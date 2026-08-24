using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Satisfactory_Universal_Tool.Core.Planner;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class ConnectorViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private Point _anchor;

    public bool IsOutput { get; init; }
    public string? ItemClass { get; init; }
    public string? ItemName { get; init; }
    public double Rate { get; init; }
    public string RateText => Rate > 0 ? $"{Rate:0.##}/min" : "";
}

public partial class PlannerNodeViewModel : ObservableObject
{
    public string TypeId { get; }

    public IReadOnlyList<IProductionCalculator> CalculationMethods => CalculatorCatalog.All;

    [ObservableProperty] private IProductionCalculator _selectedMethod = CalculatorCatalog.Default;
    [ObservableProperty] private string _solveStatus = "";

    [RelayCommand]
    private void Calculate()
    {
        var res = SelectedMethod.Calculate(new CalculationContext(Nodes, Connections));
        SolveStatus = res.Message;
    }

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _glyph;
    [ObservableProperty] private Point _location;
    [ObservableProperty] private bool _isSelected;      // <-- ESTA linha, nesta classe

    [ObservableProperty] private bool _isRecipe;
    [ObservableProperty] private string? _recipeClass;
    [ObservableProperty] private string? _machine;

    [ObservableProperty] private double _targetRate;                 // usado pelo método Grafo

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MachinesText))]
    private double _machines;                                        // resultado (qualquer método)

    public string MachinesText => Machines > 0.0001
        ? $"×{Machines:0.##} ({(int)System.Math.Ceiling(Machines)})"
        : "";

    public ObservableCollection<ConnectorViewModel> Input { get; } = new();
    public ObservableCollection<ConnectorViewModel> Output { get; } = new();

    public PlannerNodeViewModel(string typeId, string title, string glyph,
                                Point location, int inputs, int outputs)
    {
        TypeId = typeId;
        _title = title;
        _glyph = glyph;
        _location = location;

        for (int i = 0; i < inputs; i++)
            Input.Add(new ConnectorViewModel
            { Title = inputs > 1 ? $"in {i + 1}" : "entra", IsOutput = false });

        for (int i = 0; i < outputs; i++)
            Output.Add(new ConnectorViewModel
            { Title = outputs > 1 ? $"out {i + 1}" : "sai", IsOutput = true });
    }
}

public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty] private ConnectorViewModel _source = null!;
    [ObservableProperty] private ConnectorViewModel _target = null!;
}