using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Satisfactory_Universal_Tool.Core.Data;

public sealed record RecipeIO(string ItemClass, string ItemName, int Amount, double Rate);

public sealed record GameRecipe(
    string ClassName, string DisplayName, bool IsAlternate, bool IsExtraction,
    string Machine, IReadOnlyList<RecipeIO> Inputs, IReadOnlyList<RecipeIO> Outputs);

public static partial class RecipeImporter
{
    private static readonly Dictionary<string, string> MachineFallback = new()
    {
        ["Build_SmelterMk1_C"] = "Fornalha",
        ["Build_ConstructorMk1_C"] = "Construtor",
        ["Build_AssemblerMk1_C"] = "Montadora",
        ["Build_ManufacturerMk1_C"] = "Fabricante",
        ["Build_OilRefinery_C"] = "Refinaria",
        ["Build_FoundryMk1_C"] = "Fundição",
        ["Build_Packager_C"] = "Envasadora",
        ["Build_Blender_C"] = "Misturador",
        ["Build_HadronCollider_C"] = "Acelerador de Partículas",
        ["Build_Converter_C"] = "Conversor",
        ["Build_QuantumEncoder_C"] = "Codificador Quântico",
    };

    [GeneratedRegex(@"ItemClass="".*?([A-Za-z0-9_]+_C)'?"",Amount=(\d+)")]
    private static partial Regex StackRx();

    public static List<GameRecipe> ImportRecipes(string docsJsonPath)
    {
        string json = File.ReadAllText(docsJsonPath);
        using var doc = JsonDocument.Parse(json);

        var nameByClass = new Dictionary<string, string>(StringComparer.Ordinal);
        var formByClass = new Dictionary<string, string>(StringComparer.Ordinal);
        JsonElement recipeGroup = default, resourceGroup = default;
        bool hasRecipes = false, hasResources = false;

        foreach (var group in doc.RootElement.EnumerateArray())
        {
            if (!group.TryGetProperty("Classes", out var classes)) continue;

            var nc = NativeClassShort(group);
            if (nc == "FGRecipe") { recipeGroup = group; hasRecipes = true; }
            if (nc == "FGResourceDescriptor") { resourceGroup = group; hasResources = true; }

            foreach (var c in classes.EnumerateArray())
            {
                var cn = GetStr(c, "ClassName");
                if (cn.Length == 0) continue;
                var dn = GetStr(c, "mDisplayName");
                if (dn.Length > 0) nameByClass.TryAdd(cn, dn);
                var fm = GetStr(c, "mForm");
                if (fm.Length > 0) formByClass.TryAdd(cn, fm);
            }
        }

        var result = new List<GameRecipe>();

        if (hasRecipes)
        {
            foreach (var c in recipeGroup.GetProperty("Classes").EnumerateArray())
            {
                var producedIn = GetStr(c, "mProducedIn");
                var machineClass = MachineFallback.Keys.FirstOrDefault(producedIn.Contains);
                if (machineClass is null) continue;

                var name = GetStr(c, "mDisplayName").Trim();
                if (name.Length == 0) continue;

                var duration = ParseDouble(GetStr(c, "mManufactoringDuration"));  // typo é do jogo
                var className = GetStr(c, "ClassName");
                var inputs = ParseStacks(GetStr(c, "mIngredients"), nameByClass, formByClass, duration);
                var outputs = ParseStacks(GetStr(c, "mProduct"), nameByClass, formByClass, duration);
                if (outputs.Count == 0) continue;

                var machine = nameByClass.TryGetValue(machineClass, out var mn)
                    ? mn : MachineFallback[machineClass];

                result.Add(new GameRecipe(className, name,
                    className.Contains("Alternate", StringComparison.Ordinal),
                    false, machine, inputs, outputs));
            }
        }

        if (hasResources)
        {
            foreach (var c in resourceGroup.GetProperty("Classes").EnumerateArray())
            {
                var cls = GetStr(c, "ClassName");
                var name = GetStr(c, "mDisplayName").Trim();
                if (cls.Length == 0 || name.Length == 0) continue;

                var (machine, baseRate) = ExtractorFor(cls, GetStr(c, "mForm"));
                result.Add(new GameRecipe("Extract_" + cls, name, false, true, machine,
                    Array.Empty<RecipeIO>(),
                    new[] { new RecipeIO(cls, name, baseRate, baseRate) }));
            }
        }

        return result;
    }

    private static List<RecipeIO> ParseStacks(string raw,
        Dictionary<string, string> nameByClass, Dictionary<string, string> formByClass, double durationSec)
    {
        var list = new List<RecipeIO>();
        foreach (Match m in StackRx().Matches(raw))
        {
            var cls = m.Groups[1].Value;
            var amount = int.TryParse(m.Groups[2].Value, out var a) ? a : 0;
            var name = nameByClass.TryGetValue(cls, out var n) ? n : cls;
            var form = formByClass.TryGetValue(cls, out var f) ? f : "RF_SOLID";
            bool fluid = form.Contains("RF_LIQUID") || form.Contains("RF_GAS");
            double units = fluid ? amount / 1000.0 : amount;
            double rate = durationSec > 0 ? units * 60.0 / durationSec : 0;
            list.Add(new RecipeIO(cls, name, amount, rate));
        }
        return list;
    }

    private static (string machine, int baseRate) ExtractorFor(string resourceClass, string form)
        => resourceClass switch
        {
            "Desc_Water_C" => ("Bomba d'água", 120),
            "Desc_LiquidOil_C" => ("Extrator de Petróleo", 120),
            "Desc_NitrogenGas_C" => ("Extrator de Poço", 60),
            _ when form.Contains("RF_LIQUID") || form.Contains("RF_GAS") => ("Extrator de Poço", 60),
            _ => ("Mineradora", 60),
        };

    private static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;

    private static string NativeClassShort(JsonElement group)
    {
        var nc = group.TryGetProperty("NativeClass", out var v) ? v.GetString() ?? "" : "";
        int dot = nc.LastIndexOf('.');
        return (dot >= 0 ? nc[(dot + 1)..] : nc).TrimEnd('\'');
    }

    private static string GetStr(JsonElement e, string p) =>
        e.TryGetProperty(p, out var v) ? v.GetString() ?? "" : "";
}