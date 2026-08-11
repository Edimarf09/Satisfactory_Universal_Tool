using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;

namespace Satisfactory_Universal_Tool.Core.Data;

public class GameDataService
{
    private List<GameItem> _items = new();
    private Dictionary<string, GameItem> _byClass = new();   // o "dicionário": busca O(1) por classe

    public IReadOnlyList<GameItem> Items => _items;

    public void Load(string docsFolder, string languageCode)
    {
        var path = Path.Combine(docsFolder, $"{languageCode}.json");   // ex.: pt-BR.json
        _items = DocsImporter.ImportItems(path);
        _byClass = _items.ToDictionary(i => i.ClassName);
    }

    // útil lá na frente pras receitas (resolver classe de ingrediente -> item)
    public GameItem? ByClass(string className) => _byClass.GetValueOrDefault(className);

    public List<GameItem> Search(string query)
    {
        var q = Normalize(query);
        var filtered = string.IsNullOrWhiteSpace(q)
            ? _items
            : _items.Where(i => Normalize(i.DisplayName).Contains(q));

        return filtered.OrderBy(i => i.DisplayName).Take(500).ToList();
    }

    // "Ácido Sulfúrico" -> "acido sulfurico" : ignora acento e caixa
    public static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}