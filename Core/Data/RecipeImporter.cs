using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Satisfactory_Universal_Tool.Core.Data;

// Um ingrediente ou produto já com o nome resolvido.
public sealed record RecipeIO(string ItemClass, string ItemName, int Amount);

public sealed record GameRecipe(
    string ClassName,
    string DisplayName,
    bool IsAlternate,
    string Machine,
    IReadOnlyList<RecipeIO> Inputs,
    IReadOnlyList<RecipeIO> Outputs);

public static partial class RecipeImporter
{
    // Prédios que contam como "máquina de produção". Só receitas feitas
    // nesses entram na lista da direita (tira receitas de construção/manual).
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

    // Regex: extrai cada (ClassName do item, quantidade) de mIngredients/mProduct.
    [GeneratedRegex(@"ItemClass="".*?([A-Za-z0-9_]+_C)'?"",Amount=(\d+)")]
    private static partial Regex StackRx();

    public static List<GameRecipe> ImportRecipes(string docsJsonPath)
    {
        string json = File.ReadAllText(docsJsonPath);   // UTF-16 detectado pelo BOM
        using var doc = JsonDocument.Parse(json);

        // 1) mapa completo ClassName -> nome (itens, recursos, prédios...)
        var nameByClass = new Dictionary<string, string>(StringComparer.Ordinal);
        JsonElement recipeGroup = default;
        bool hasRecipes = false;

        foreach (var group in doc.RootElement.EnumerateArray())
        {
            if (!group.TryGetProperty("Classes", out var classes)) continue;

            if (NativeClassShort(group) == "FGRecipe")
            {
                recipeGroup = group;
                hasRecipes = true;
            }

            foreach (var c in classes.EnumerateArray())
            {
                var cn = GetStr(c, "ClassName");
                var dn = GetStr(c, "mDisplayName");
                if (cn.Length > 0 && dn.Length > 0)
                    nameByClass.TryAdd(cn, dn);
            }
        }

        var result = new List<GameRecipe>();
        if (!hasRecipes) return result;

        // 2) percorre as receitas
        foreach (var c in recipeGroup.GetProperty("Classes").EnumerateArray())
        {
            var producedIn = GetStr(c, "mProducedIn");
            var machineClass = MachineFallback.Keys.FirstOrDefault(producedIn.Contains);
            if (machineClass is null) continue;   // não é receita de máquina -> ignora

            var name = GetStr(c, "mDisplayName").Trim();
            if (name.Length == 0) continue;

            var className = GetStr(c, "ClassName");
            var inputs = ParseStacks(GetStr(c, "mIngredients"), nameByClass);
            var outputs = ParseStacks(GetStr(c, "mProduct"), nameByClass);
            if (outputs.Count == 0) continue;     // segurança

            var machine = nameByClass.TryGetValue(machineClass, out var mn)
                ? mn
                : MachineFallback[machineClass];

            result.Add(new GameRecipe(
                ClassName: className,
                DisplayName: name,
                IsAlternate: className.Contains("Alternate", StringComparison.Ordinal),
                Machine: machine,
                Inputs: inputs,
                Outputs: outputs));
        }

        return result;
    }

    private static List<RecipeIO> ParseStacks(string raw, Dictionary<string, string> nameByClass)
    {
        var list = new List<RecipeIO>();
        foreach (Match m in StackRx().Matches(raw))
        {
            var cls = m.Groups[1].Value;
            var amount = int.TryParse(m.Groups[2].Value, out var a) ? a : 0;
            var itemName = nameByClass.TryGetValue(cls, out var n) ? n : cls;
            list.Add(new RecipeIO(cls, itemName, amount));
        }
        return list;
    }

    private static string NativeClassShort(JsonElement group)
    {
        var nc = group.TryGetProperty("NativeClass", out var v) ? v.GetString() ?? "" : "";
        int dot = nc.LastIndexOf('.');
        return (dot >= 0 ? nc[(dot + 1)..] : nc).TrimEnd('\'');
    }

    private static string GetStr(JsonElement e, string p) =>
        e.TryGetProperty(p, out var v) ? v.GetString() ?? "" : "";
}