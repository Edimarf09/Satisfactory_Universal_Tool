using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Satisfactory_Universal_Tool.Core.Data;
using Satisfactory_Universal_Tool.Core.Planner;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class PlannerViewModel : ObservableObject
{
    public ObservableCollection<PlannerNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    // ===== Config (topo-direito) =====
    public IReadOnlyList<IProductionCalculator> CalculationMethods => CalculatorCatalog.All;

    [ObservableProperty] private IProductionCalculator _selectedMethod = CalculatorCatalog.Default;
    partial void OnSelectedMethodChanged(IProductionCalculator value) => Recalculate();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayMode))]
    private int _displayModeIndex;                        // 0 = Máquinas, 1 = Itens/min
    public PlannerDisplayMode DisplayMode => (PlannerDisplayMode)DisplayModeIndex;
    partial void OnDisplayModeIndexChanged(int value)
    {
        foreach (var n in Nodes) n.DisplayModeIndex = value;
        Recalculate();
    }

    [ObservableProperty] private string _solveStatus = "";

    public void Recalculate()
    {
        var res = SelectedMethod.Calculate(new CalculationContext(Nodes, Connections, DisplayMode));
        foreach (var n in Nodes) n.RefreshAfterSolve();
        SolveStatus = res.Message;
    }

    // ===== Janela de seleção =====
    public IReadOnlyList<ToolDescriptor> Tools => NodeCatalog.Tools;
    public ObservableCollection<GameRecipe> RecipeResults { get; } = new();

    [ObservableProperty] private bool _isPickerOpen;
    [ObservableProperty] private string _pickerSearch = "";
    [ObservableProperty] private bool _matchName = true;
    [ObservableProperty] private bool _matchInputs;
    [ObservableProperty] private bool _matchOutputs;

    [ObservableProperty] private Point _cursorGraph;

    private Point _pendingLocation;
    private ConnectorViewModel? _pendingSourceConnector;

    public PlannerViewModel() => RunRecipeSearch();

    partial void OnPickerSearchChanged(string value) => RunRecipeSearch();
    partial void OnMatchNameChanged(bool value) => RunRecipeSearch();
    partial void OnMatchInputsChanged(bool value) => RunRecipeSearch();
    partial void OnMatchOutputsChanged(bool value) => RunRecipeSearch();
    partial void OnIsPickerOpenChanged(bool value) { if (!value) _pendingSourceConnector = null; }

    private void RunRecipeSearch()
    {
        RecipeResults.Clear();
        foreach (var r in App.GameData.SearchRecipes(PickerSearch, MatchName, MatchInputs, MatchOutputs))
            RecipeResults.Add(r);
    }

    public void OpenPickerAt(Point graphLocation)
    {
        _pendingSourceConnector = null;
        _pendingLocation = graphLocation;
        MatchName = true; MatchInputs = false; MatchOutputs = false;
        PickerSearch = "";
        RunRecipeSearch();
        IsPickerOpen = true;
    }

    private void OpenPickerFromConnector(ConnectorViewModel c)
    {
        _pendingSourceConnector = c;
        _pendingLocation = CursorGraph;

        if (!string.IsNullOrEmpty(c.ItemName))
        {
            MatchName = false;
            MatchInputs = c.IsOutput;
            MatchOutputs = !c.IsOutput;
            PickerSearch = c.ItemName;
        }
        else
        {
            MatchName = true; MatchInputs = false; MatchOutputs = false;
            PickerSearch = "";
        }
        RunRecipeSearch();
        IsPickerOpen = true;
    }

    [RelayCommand]
    private void PickTool(ToolDescriptor? t)
    {
        if (t is null) return;
        FinishPick(NodeCatalog.FromTool(t, _pendingLocation));
    }

    [RelayCommand]
    private void PickRecipe(GameRecipe? r)
    {
        if (r is null) return;
        FinishPick(NodeCatalog.FromRecipe(r, _pendingLocation));
    }

    private void FinishPick(PlannerNodeViewModel node)
    {
        node.RequestRecalc += Recalculate;               // recálculo dinâmico
        node.DisplayModeIndex = DisplayModeIndex;
        Nodes.Add(node);

        if (_pendingSourceConnector is { } origin)
        {
            var pool = origin.IsOutput ? node.Input : node.Output;
            var match = pool.FirstOrDefault(k => ItemsCompatible(origin.ItemClass, k.ItemClass))
                        ?? pool.FirstOrDefault();
            if (match is not null) TryConnect(origin, match);
        }

        IsPickerOpen = false;
        Recalculate();
    }

    [RelayCommand]
    private void ClosePicker() => IsPickerOpen = false;

    [RelayCommand]
    private void CreateConnection((object? Source, object? Target) p)
    {
        if (p.Source is not ConnectorViewModel source) return;

        if (p.Target is null) { OpenPickerFromConnector(source); return; }

        if (p.Target is ConnectorViewModel target)
        {
            TryConnect(source, target);
            Recalculate();
        }
    }

    private bool TryConnect(ConnectorViewModel x, ConnectorViewModel y)
    {
        if (ReferenceEquals(x, y)) return false;
        if (x.IsOutput == y.IsOutput) return false;

        var output = x.IsOutput ? x : y;
        var input = x.IsOutput ? y : x;

        if (Connections.Any(c => ReferenceEquals(c.Target, input))) return false;
        if (!ItemsCompatible(output.ItemClass, input.ItemClass)) return false;

        Connections.Add(new ConnectionViewModel { Source = output, Target = input });
        output.IsConnected = true;
        input.IsConnected = true;
        return true;
    }

    private static bool ItemsCompatible(string? a, string? b)
        => string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b;

    [RelayCommand]
    private void DeleteSelected()
    {
        var doomed = Nodes.Where(n => n.IsSelected).ToList();
        if (doomed.Count == 0) return;

        var dead = new HashSet<ConnectorViewModel>();
        foreach (var n in doomed)
        {
            foreach (var c in n.Input) dead.Add(c);
            foreach (var c in n.Output) dead.Add(c);
        }

        foreach (var link in Connections
                     .Where(l => dead.Contains(l.Source) || dead.Contains(l.Target)).ToList())
            Connections.Remove(link);

        foreach (var n in doomed) { n.RequestRecalc -= Recalculate; Nodes.Remove(n); }

        var live = new HashSet<ConnectorViewModel>();
        foreach (var l in Connections) { live.Add(l.Source); live.Add(l.Target); }
        foreach (var n in Nodes)
        {
            foreach (var c in n.Input) c.IsConnected = live.Contains(c);
            foreach (var c in n.Output) c.IsConnected = live.Contains(c);
        }

        Recalculate();
    }
}