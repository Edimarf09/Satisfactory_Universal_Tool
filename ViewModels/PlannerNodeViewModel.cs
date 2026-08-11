using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Satisfactory_Universal_Tool.ViewModels;

// Um conector (bolinha) de entrada ou saída num nó
public partial class ConnectorViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private bool _isConnected;
    public PlannerNodeViewModel Node { get; set; } = null!;
}

// Um nó no canvas (ex.: "Constructor", "Iron Plate")
public partial class PlannerNodeViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Novo nó";
    [ObservableProperty] private Point _location;

    public ConnectorViewModel Input { get; }
    public ConnectorViewModel Output { get; }

    public PlannerNodeViewModel()
    {
        Input  = new ConnectorViewModel { Title = "entra", Node = this };
        Output = new ConnectorViewModel { Title = "sai",   Node = this };
    }
}

// Uma ligação (seta) entre a saída de um nó e a entrada de outro
public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty] private ConnectorViewModel _source = null!;
    [ObservableProperty] private ConnectorViewModel _target = null!;
}