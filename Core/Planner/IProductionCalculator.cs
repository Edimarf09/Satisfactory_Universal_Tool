using System.Collections.Generic;
using Satisfactory_Universal_Tool.ViewModels;

namespace Satisfactory_Universal_Tool.Core.Planner;

public enum PlannerDisplayMode { Machines, Rate }

public sealed record CalculationContext(
    IReadOnlyList<PlannerNodeViewModel> Nodes,
    IReadOnlyList<ConnectionViewModel> Connections,
    PlannerDisplayMode Mode);

public sealed record CalculationResult(bool Ok, string Message);

public interface IProductionCalculator
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    CalculationResult Calculate(CalculationContext ctx);
}