using System.Collections.Generic;
using System.Linq;

namespace Satisfactory_Universal_Tool.Core.Planner;

public static class CalculatorCatalog
{
    public static IReadOnlyList<IProductionCalculator> All { get; } = new IProductionCalculator[]
    {
        new OffCalculator(),
        new GraphCalculator(),
        // new ManualCalculator(),   // <- futuro: cria a classe e registra aqui
    };

    public static IProductionCalculator Default => All.First(c => c.Id == "graph");
}