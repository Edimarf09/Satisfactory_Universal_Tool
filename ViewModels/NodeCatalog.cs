using System.Collections.Generic;
using System.Windows;
using Satisfactory_Universal_Tool.Core.Data;

namespace Satisfactory_Universal_Tool.ViewModels;

public sealed record ToolDescriptor(
    string Id, string DisplayName, string Glyph, string Description, int Inputs, int Outputs);

public static class NodeCatalog
{
    public static IReadOnlyList<ToolDescriptor> Tools { get; } = new[]
    {
        new ToolDescriptor("outpost",       "Outpost",            "\u2302", "Ponto de coleta / base avançada.",        0, 1),
        new ToolDescriptor("blueprint",     "Blueprint",          "\u25A6", "Blueprint (grupo de máquinas).",          1, 1),
        new ToolDescriptor("splurger",      "Splurger",           "\u2726", "Ferramenta Splurger.",                    1, 1),
        new ToolDescriptor("prio_splitter", "Priority Splitter",  "\u25C0", "Divisor por prioridade.",                 1, 3),
        new ToolDescriptor("prio_merger",   "Priority Merger",    "\u25B6", "Combinador por prioridade.",              3, 1),
        new ToolDescriptor("prio_splurger", "Priority Splurger",  "\u2727", "Splurger por prioridade.",                1, 1),
        new ToolDescriptor("sink",          "AWESOME Sink",       "\u267B", "Consome itens por pontos. Só entrada.",   1, 0),
        new ToolDescriptor("storage",       "Storage Container",  "\u25A9", "Armazém / buffer.",                       1, 1),
        new ToolDescriptor("dim_depot",     "Dimensional Depot",  "\u2601", "Depósito dimensional.",                   1, 0),
    };

    public static PlannerNodeViewModel FromTool(ToolDescriptor t, Point location)
        => new(typeId: t.Id, title: t.DisplayName, glyph: t.Glyph,
               location: location, inputs: t.Inputs, outputs: t.Outputs);

    public static PlannerNodeViewModel FromRecipe(GameRecipe r, Point location)
    {
        var node = new PlannerNodeViewModel(
            typeId: "recipe", title: r.DisplayName, glyph: r.IsExtraction ? "\u26CF" : "\u2699",
            location: location, inputs: 0, outputs: 0)
        {
            IsRecipe = true,
            RecipeClass = r.ClassName,
            Machine = r.Machine
        };

        foreach (var i in r.Inputs)
            node.Input.Add(new ConnectorViewModel
            { Title = i.ItemName, IsOutput = false, ItemClass = i.ItemClass, ItemName = i.ItemName, Rate = i.Rate });

        foreach (var o in r.Outputs)
            node.Output.Add(new ConnectorViewModel
            { Title = o.ItemName, IsOutput = true, ItemClass = o.ItemClass, ItemName = o.ItemName, Rate = o.Rate });

        return node;
    }
}