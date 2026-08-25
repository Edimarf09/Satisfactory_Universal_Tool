using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class ConnectorViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private Point _anchor;

    public bool IsOutput { get; init; }
    public string? ItemClass { get; init; }
    public string? ItemName { get; init; }
    public double Rate { get; init; }                       // base /min por máquina

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RateText))]
    private double _actualRate;                             // dinâmico: base * máquinas

    public string RateText
    {
        get { var r = ActualRate > 0 ? ActualRate : Rate; return r > 0 ? $"{r:0.##}/min" : ""; }
    }

    // PRONTO PRA IMAGENS (implementar depois): images/<ItemClass>.png
    public string? ItemImagePath => ItemClass is null ? null : $"images/{ItemClass}.png";
}

public partial class PlannerNodeViewModel : ObservableObject
{
    public string TypeId { get; }
    public event Action? RequestRecalc;                    // dispara recálculo dinâmico

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _glyph;
    [ObservableProperty] private Point _location;
    [ObservableProperty] private bool _isSelected;

    [ObservableProperty] private bool _isRecipe;
    [ObservableProperty] private string? _recipeClass;
    [ObservableProperty] private string? _machine;

    // DEFINIDO (o que você fixa; a UI mostra o certo conforme o modo)
    [ObservableProperty] private double _targetMachines;
    [ObservableProperty] private double _targetRate;
    partial void OnTargetMachinesChanged(double value) => RequestRecalc?.Invoke();
    partial void OnTargetRateChanged(double value) => RequestRecalc?.Invoke();

    // USADO (resultado do cálculo)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MachinesText))]
    [NotifyPropertyChangedFor(nameof(UsedRate))]
    [NotifyPropertyChangedFor(nameof(UsedRateText))]
    private double _machines;

    public string MachinesText => Machines > 0.0001
        ? $"×{Machines:0.##} ({(int)Math.Ceiling(Machines)})" : "";

    private double PrimaryOutputRate =>
        Output.Where(c => c.Rate > 0).Select(c => c.Rate).DefaultIfEmpty(0).Max();
    public double UsedRate => PrimaryOutputRate * Machines;
    public string UsedRateText => UsedRate > 0 ? $"{UsedRate:0.##}/min" : "";

    // MODO espelhado do planner (0 = máquinas, 1 = itens/min)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMachinesMode))]
    [NotifyPropertyChangedFor(nameof(IsRateMode))]
    private int _displayModeIndex;
    public bool IsMachinesMode => DisplayModeIndex == 0;
    public bool IsRateMode => DisplayModeIndex == 1;

    public ObservableCollection<ConnectorViewModel> Input { get; } = new();
    public ObservableCollection<ConnectorViewModel> Output { get; } = new();

    public PlannerNodeViewModel(string typeId, string title, string glyph,
                                Point location, int inputs, int outputs)
    {
        TypeId = typeId; _title = title; _glyph = glyph; _location = location;
        for (int i = 0; i < inputs; i++)
            Input.Add(new ConnectorViewModel { Title = inputs > 1 ? $"in {i + 1}" : "entra", IsOutput = false });
        for (int i = 0; i < outputs; i++)
            Output.Add(new ConnectorViewModel { Title = outputs > 1 ? $"out {i + 1}" : "sai", IsOutput = true });
    }

    // Atualiza os números dinâmicos depois do cálculo.
    public void RefreshAfterSolve()
    {
        foreach (var c in Input) c.ActualRate = c.Rate * Machines;
        foreach (var c in Output) c.ActualRate = c.Rate * Machines;
        OnPropertyChanged(nameof(UsedRate));
        OnPropertyChanged(nameof(UsedRateText));
        OnPropertyChanged(nameof(MachinesText));
    }
}

public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty] private ConnectorViewModel _source = null!;
    [ObservableProperty] private ConnectorViewModel _target = null!;
}