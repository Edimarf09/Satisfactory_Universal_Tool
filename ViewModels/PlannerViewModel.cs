using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class PlannerViewModel : ObservableObject
{
    public ObservableCollection<PlannerNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    [ObservableProperty] private ConnectorViewModel? _pendingSource;

    public PlannerViewModel()
    {
        Nodes.Add(new PlannerNodeViewModel { Title = "Minério de Ferro", Location = new Point(60, 80) });
        Nodes.Add(new PlannerNodeViewModel { Title = "Constructor",       Location = new Point(360, 160) });
    }

    [RelayCommand]
    private void AddNode() =>
        Nodes.Add(new PlannerNodeViewModel { Title = "Novo nó", Location = new Point(120, 120) });

    // chamado quando você começa a arrastar de um conector
    [RelayCommand]
    private void StartConnection(ConnectorViewModel source) => PendingSource = source;

    // chamado quando você solta em cima de outro conector
    // O Nodify entrega (origem, alvo) como uma tupla (object, object)
    [RelayCommand]
    private void CreateConnection((object? Source, object? Target) p)
    {
        if (p.Source is ConnectorViewModel source &&
            p.Target is ConnectorViewModel target &&
            source != target)
        {
            Connections.Add(new ConnectionViewModel { Source = source, Target = target });
            source.IsConnected = true;
            target.IsConnected = true;
        }
    }
}