namespace Satisfactory_Universal_Tool.Core;

// A velocidade de cada tier (itens/min) fica embutida no próprio enum
public enum BeltTier
{
    Mk1 = 60,
    Mk2 = 120,
    Mk3 = 270,
    Mk4 = 480,
    Mk5 = 780,
    Mk6 = 1200
}

public enum BalanceStatus { Balanced, Overflow, Starved }

public record StreamPlan(double Rate, int BeltsNeeded);

public record BalancerResult(
    double TotalIn,
    double TotalOut,
    double Difference,               // TotalIn - TotalOut
    BalanceStatus Status,
    IReadOnlyList<StreamPlan> InputPlans,
    IReadOnlyList<StreamPlan> OutputPlans,
    IReadOnlyList<string> Warnings);

public record BalancerRequest(
    IReadOnlyList<double> Inputs,
    IReadOnlyList<double> Outputs,
    BeltTier Tier);

public static class BeltBalancer
{
    private const double Tol = 0.001;

    public static BalancerResult Solve(BalancerRequest req)
    {
        var tier = req.Tier;
        double maxPerBelt = (int)tier;
        var warnings = new List<string>();

        StreamPlan PlanFor(double rate)
        {
            if (rate <= 0) return new StreamPlan(0, 0);
            int belts = Math.Max(1, (int)Math.Ceiling(rate / maxPerBelt - Tol));
            double perBelt = rate / belts;

            if (belts > 1)
                warnings.Add($"{rate:0.##}/min passa do teto da {tier} ({maxPerBelt:0}/min) — precisa de {belts} esteiras em paralelo ({perBelt:0.##}/min cada).");
            if (perBelt > maxPerBelt * 0.95)
                warnings.Add($"{perBelt:0.##}/min está quase no teto da {tier} — na prática a esteira engasga por timing. Considere um tier acima ou uma esteira a mais.");

            return new StreamPlan(rate, belts);
        }

        var inputPlans = req.Inputs.Select(PlanFor).ToList();
        var outputPlans = req.Outputs.Select(PlanFor).ToList();

        double totalIn = req.Inputs.Sum();
        double totalOut = req.Outputs.Sum();
        double diff = totalIn - totalOut;

        var status = Math.Abs(diff) < Tol ? BalanceStatus.Balanced
                   : diff > 0            ? BalanceStatus.Overflow
                                         : BalanceStatus.Starved;

        if (status == BalanceStatus.Overflow)
            warnings.Add($"Sobra de {diff:0.##}/min entrando — sem uma saída de overflow, isso entope a linha.");
        else if (status == BalanceStatus.Starved)
            warnings.Add($"Faltam {-diff:0.##}/min — as saídas não atingem o alvo com essas entradas.");

        return new BalancerResult(totalIn, totalOut, diff, status, inputPlans, outputPlans, warnings);
    }

    // Atalho pro modo "divisão igual": N entradas → M saídas iguais
    public static BalancerResult SolveEvenSplit(IReadOnlyList<double> inputs, int outputCount, BeltTier tier)
    {
        double each = inputs.Sum() / outputCount;
        var outputs = Enumerable.Repeat(each, outputCount).ToList();
        return Solve(new BalancerRequest(inputs, outputs, tier));
    }
}