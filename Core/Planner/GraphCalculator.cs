using System.Collections.Generic;
using System.Linq;
using Google.OrTools.LinearSolver;
using Satisfactory_Universal_Tool.ViewModels;

namespace Satisfactory_Universal_Tool.Core.Planner;

// Fluxo dirigido sobre o grafo desenhado. Alvos vêm de node.TargetRate>0.
// m_n = máquinas por nó, f_e = fluxo por conexão. Minimiza Σ m.
public sealed class GraphCalculator : IProductionCalculator
{
    public string Id => "graph";
    public string DisplayName => "Grafo direcionado";
    public string Description => "Usa as conexões e um alvo/min; calcula as máquinas de todo o grafo.";

    public CalculationResult Calculate(CalculationContext ctx)
    {
        var nodes = ctx.Nodes;
        var connections = ctx.Connections;

        var solver = Solver.CreateSolver("GLOP");
        if (solver is null) return new(false, "OrTools (GLOP) indisponível.");
        const double INF = double.PositiveInfinity;

        var prod = nodes.Where(n => n.Input.Any(c => c.Rate > 0) || n.Output.Any(c => c.Rate > 0)).ToList();
        if (prod.Count == 0) return new(false, "Nenhum nó com receita no grafo.");

        var m = new Dictionary<PlannerNodeViewModel, Variable>();
        int i = 0;
        foreach (var n in prod) m[n] = solver.MakeNumVar(0, INF, $"m{i++}");

        var f = new Dictionary<ConnectionViewModel, Variable>();
        int j = 0;
        foreach (var e in connections) f[e] = solver.MakeNumVar(0, INF, $"f{j++}");

        var outEdges = connections.GroupBy(e => e.Source).ToDictionary(g => g.Key, g => g.ToList());
        var inEdges = connections.GroupBy(e => e.Target).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var n in prod)
        {
            foreach (var oc in n.Output.Where(c => c.Rate > 0))
            {
                if (!outEdges.TryGetValue(oc, out var es)) continue;
                var ct = solver.MakeConstraint(-INF, 0);              // Σf - rate*m ≤ 0
                foreach (var e in es) ct.SetCoefficient(f[e], 1);
                ct.SetCoefficient(m[n], -oc.Rate);
            }
            foreach (var ic in n.Input.Where(c => c.Rate > 0))
            {
                if (!inEdges.TryGetValue(ic, out var es)) continue;   // input sem aresta = externo
                var ct = solver.MakeConstraint(0, 0);                // Σf - rate*m = 0
                foreach (var e in es) ct.SetCoefficient(f[e], 1);
                ct.SetCoefficient(m[n], -ic.Rate);
            }
        }

        bool anyTarget = false;
        foreach (var n in prod.Where(x => x.TargetRate > 0))
        {
            var oc = n.Output.Where(c => c.Rate > 0).OrderByDescending(c => c.Rate).FirstOrDefault();
            if (oc is null) continue;
            anyTarget = true;
            var ct = solver.MakeConstraint(n.TargetRate, INF);       // rate*m ≥ alvo
            ct.SetCoefficient(m[n], oc.Rate);
        }
        if (!anyTarget) return new(false, "Defina um alvo/min em algum nó de saída.");

        var obj = solver.Objective();
        foreach (var n in prod) obj.SetCoefficient(m[n], 1);
        obj.SetMinimization();

        var status = solver.Solve();
        if (status != Solver.ResultStatus.OPTIMAL && status != Solver.ResultStatus.FEASIBLE)
            return new(false, "Sem solução viável para os alvos.");

        foreach (var n in nodes) n.Machines = m.TryGetValue(n, out var v) ? v.SolutionValue() : 0;
        return new(true, $"Grafo: {prod.Count} nós calculados.");
    }
}