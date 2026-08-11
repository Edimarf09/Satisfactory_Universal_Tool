using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Satisfactory_Universal_Tool.Core.Data;

public record GameItem(
    string ClassName,
    string DisplayName,
    string Description,
    int StackSize,
    string Form,
    int SinkPoints,
    double EnergyValue,
    string Category);   // "Item" ou "Building"

public static class DocsImporter
{
    private static readonly Dictionary<string, int> StackSizes = new()
    {
        ["SS_ONE"] = 1, ["SS_SMALL"] = 50, ["SS_MEDIUM"] = 100,
        ["SS_BIG"] = 200, ["SS_HUGE"] = 500, ["SS_FLUID"] = 0
    };

    public static List<GameItem> ImportItems(string docsJsonPath)
    {
        string json = File.ReadAllText(docsJsonPath);
        var result = new List<GameItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // dedupe por nome

        using var doc = JsonDocument.Parse(json);
        foreach (var group in doc.RootElement.EnumerateArray())
        {
            var native = NativeClassShort(group);
            if (!group.TryGetProperty("Classes", out var classes)) continue;

            foreach (var c in classes.EnumerateArray())
            {
                var name = GetStr(c, "mDisplayName").Trim();
                if (name.Length == 0) continue;      // única regra de exclusão: sem nome
                if (!seen.Add(name)) continue;        // sem repetir nome

                bool isItem = c.TryGetProperty("mStackSize", out _);

                result.Add(new GameItem(
                    ClassName:   GetStr(c, "ClassName"),
                    DisplayName: name,
                    Description: GetStr(c, "mDescription"),
                    StackSize:   isItem ? StackSizes.GetValueOrDefault(GetStr(c, "mStackSize"), 100) : 0,
                    Form:        GetStr(c, "mForm"),
                    SinkPoints:  ParseInt(GetStr(c, "mResourceSinkPoints")),
                    EnergyValue: ParseDouble(GetStr(c, "mEnergyValue")),
                    Category:    Categorize(native, isItem)));
            }
        }
        return result;
    }

    private static string Categorize(string native, bool isItem)
    {
        if (isItem) return "Item";
        if (native.StartsWith("FGBuildable")) return "Building";
        return native switch
        {
            "FGRecipe" => "Recipe",
            "FGSchematic" => "Schematic",
            "FGCustomizationRecipe" => "Customization",
            _ => "Other"
        };
    }

    private static string NativeClassShort(JsonElement group)
    {
        var nc = group.TryGetProperty("NativeClass", out var v) ? v.GetString() ?? "" : "";
        int dot = nc.LastIndexOf('.');
        return (dot >= 0 ? nc[(dot + 1)..] : nc).TrimEnd('\'');
    }

    private static string GetStr(JsonElement e, string p) =>
        e.TryGetProperty(p, out var v) ? v.GetString() ?? "" : "";
    private static int ParseInt(string s) => int.TryParse(s, out var v) ? v : 0;
    private static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
}