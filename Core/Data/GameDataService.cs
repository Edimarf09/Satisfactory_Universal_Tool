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
    private Dictionary<string, GameItem> _byClass = new();   // busca O(1) por classe
    private List<GameRecipe> _recipes = new();

    public IReadOnlyList<GameItem> Items => _items;
    public IReadOnlyList<GameRecipe> Recipes => _recipes;

    public void Load(string docsFolder, string languageCode)
    {
        var path = Path.Combine(docsFolder, $"{languageCode}.json");   // ex.: pt-BR.json
        _items = DocsImporter.ImportItems(path);
        _byClass = _items.ToDictionary(i => i.ClassName);
        _recipes = RecipeImporter.ImportRecipes(path);                 // <-- novo
    }

    public GameItem? ByClass(string className) => _byClass.GetValueOrDefault(className);

    public List<GameItem> Search(string query)
    {
        var q = Normalize(query);
        var filtered = string.IsNullOrWhiteSpace(q)
            ? _items
            : _items.Where(i => Normalize(i.DisplayName).Contains(q));

        return filtered.OrderBy(i => i.DisplayName).Take(500).ToList();
    }

    // Busca de receitas com os três filtros da janelinha.
    // Alternativas vão pro fim da lista; sem texto, lista tudo.
    public List<GameRecipe> SearchRecipes(string query, bool byName, bool byInputs, bool byOutputs)
    {
        var q = Normalize(query);
        IEnumerable<GameRecipe> src = _recipes;

        if (q.Length > 0)
        {
            // se o usuário desligar os três, ninguém casa (comportamento esperado)
            src = src.Where(r =>
                (byName && Normalize(r.DisplayName).Contains(q)) ||
                (byInputs && r.Inputs.Any(i => Normalize(i.ItemName).Contains(q))) ||
                (byOutputs && r.Outputs.Any(o => Normalize(o.ItemName).Contains(q))));
        }

        return src
            .OrderByDescending(r => r.IsExtraction)   // extratores no topo
            .ThenBy(r => r.IsAlternate)               // depois normais; alternativas por último
            .ThenBy(r => r.DisplayName)
            .Take(500)
            .ToList();
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