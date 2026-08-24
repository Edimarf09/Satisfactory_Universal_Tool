using System.Collections.Generic;
using Satisfactory_Universal_Tool.ViewModels;

namespace Satisfactory_Universal_Tool.Core.Planner;

// O grafo que qualquer método recebe.
public sealed record CalculationContext(
    IReadOnlyList<PlannerNodeViewModel> Nodes,
    IReadOnlyList<ConnectionViewModel> Connections);

public sealed record CalculationResult(bool Ok, string Message);

// Um método de cálculo. Para criar um novo, implemente isto e registre no catálogo.
public interface IProductionCalculator
{
    string Id { get; }             // estável, ex.: "graph"
    string DisplayName { get; }    // rótulo no seletor
    string Description { get; }     // ajuda/tooltip
    CalculationResult Calculate(CalculationContext ctx);
}