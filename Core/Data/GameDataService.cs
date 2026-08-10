using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        if (string.IsNullOrWhiteSpace(query))
            return _items.OrderBy(i => i.DisplayName).Take(200).ToList();

        return _items
            .Where(i => i.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.DisplayName)
            .Take(200)
            .ToList();
    }
}