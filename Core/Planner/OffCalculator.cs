namespace Satisfactory_Universal_Tool.Core.Planner;

public sealed class OffCalculator : IProductionCalculator
{
    public string Id => "off";
    public string DisplayName => "Desligado";
    public string Description => "Não faz cálculo. Zera as máquinas.";

    public CalculationResult Calculate(CalculationContext ctx)
    {
        foreach (var n in ctx.Nodes) n.Machines = 0;
        return new(true, "Cálculo desligado.");
    }
}