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
    double EnergyValue);

public static class DocsImporter
{
    private static readonly Dictionary<string, int> StackSizes = new()
    {
        ["SS_ONE"] = 1, ["SS_SMALL"] = 50, ["SS_MEDIUM"] = 100,
        ["SS_BIG"] = 200, ["SS_HUGE"] = 500, ["SS_FLUID"] = 0
    };

    public static List<GameItem> ImportItems(string docsJsonPath)
    {
        string json = File.ReadAllText(docsJsonPath); // UTF-16 c/ BOM detectado sozinho
        var items = new List<GameItem>();
        using var doc = JsonDocument.Parse(json);

        foreach (var group in doc.RootElement.EnumerateArray())
        {
            if (!group.TryGetProperty("Classes", out var classes)) continue;

            foreach (var c in classes.EnumerateArray())
            {
                if (!c.TryGetProperty("mStackSize", out var stackProp)) continue;   // filtro de item
                if (!c.TryGetProperty("mDisplayName", out var nameProp)) continue;

                items.Add(new GameItem(
                    ClassName:   GetStr(c, "ClassName"),
                    DisplayName: nameProp.GetString() ?? "",
                    Description: GetStr(c, "mDescription"),
                    StackSize:   StackSizes.GetValueOrDefault(stackProp.GetString() ?? "SS_MEDIUM", 100),
                    Form:        GetStr(c, "mForm"),
                    SinkPoints:  ParseInt(GetStr(c, "mResourceSinkPoints")),
                    EnergyValue: ParseDouble(GetStr(c, "mEnergyValue"))));
            }
        }
        return items;
    }

    private static string GetStr(JsonElement e, string p) =>
        e.TryGetProperty(p, out var v) ? v.GetString() ?? "" : "";
    private static int ParseInt(string s) => int.TryParse(s, out var v) ? v : 0;
    private static double ParseDouble(string s) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
}