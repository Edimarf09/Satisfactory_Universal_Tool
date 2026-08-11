using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class ConnectorViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private Point _anchor;   // posição na tela, o Nodify preenche
}

public partial class PlannerNodeViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Novo nó";
    [ObservableProperty] private Point _location;

    // Input/Output são COLEÇÕES no Nodify
    public ObservableCollection<ConnectorViewModel> Input { get; } = new();
    public ObservableCollection<ConnectorViewModel> Output { get; } = new();

    public PlannerNodeViewModel()
    {
        Input.Add(new ConnectorViewModel { Title = "entra" });
        Output.Add(new ConnectorViewModel { Title = "sai" });
    }
}

public partial class ConnectionViewModel : ObservableObject
{
    [ObservableProperty] private ConnectorViewModel _source = null!;
    [ObservableProperty] private ConnectorViewModel _target = null!;
}