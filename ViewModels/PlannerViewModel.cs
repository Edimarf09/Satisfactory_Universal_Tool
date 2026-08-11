using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class PlannerViewModel : ObservableObject
{
    public ObservableCollection<PlannerNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    // conector "pendente" enquanto o usuário arrasta uma linha da bolinha
    [ObservableProperty] private PendingConnectionViewModel _pendingConnection;

    public PlannerViewModel()
    {
        PendingConnection = new PendingConnectionViewModel(this);

        // dois nós de exemplo pra você ver algo na tela
        Nodes.Add(new PlannerNodeViewModel { Title = "Minério de Ferro", Location = new Point(60, 80) });
        Nodes.Add(new PlannerNodeViewModel { Title = "Constructor",       Location = new Point(360, 160) });
    }

    [RelayCommand]
    private void AddNode() =>
        Nodes.Add(new PlannerNodeViewModel { Title = "Novo nó", Location = new Point(120, 120) });

    public void Connect(ConnectorViewModel source, ConnectorViewModel target)
    {
        Connections.Add(new ConnectionViewModel { Source = source, Target = target });
        source.IsConnected = true;
        target.IsConnected = true;
    }
}

// Gerencia a linha que nasce quando você arrasta de um conector
public partial class PendingConnectionViewModel : ObservableObject
{
    private readonly PlannerViewModel _editor;
    [ObservableProperty] private ConnectorViewModel? _source;

    public PendingConnectionViewModel(PlannerViewModel editor) => _editor = editor;

    [RelayCommand]
    private void Start(ConnectorViewModel source) => Source = source;

    [RelayCommand]
    private void Finish(ConnectorViewModel? target)
    {
        if (Source != null && target != null && Source != target)
            _editor.Connect(Source, target);
        Source = null;
    }
}