using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Satisfactory_Universal_Tool.Core.Data;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class PlannerViewModel : ObservableObject
{
    public ObservableCollection<PlannerNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    public IReadOnlyList<ToolDescriptor> Tools => NodeCatalog.Tools;
    public ObservableCollection<GameRecipe> RecipeResults { get; } = new();

    [ObservableProperty] private bool _isPickerOpen;
    [ObservableProperty] private string _pickerSearch = "";

    [ObservableProperty] private bool _matchName = true;
    [ObservableProperty] private bool _matchInputs;
    [ObservableProperty] private bool _matchOutputs;

    // Posição do cursor no GRAFO (o editor empurra isso via MouseLocation OneWayToSource).
    [ObservableProperty] private Point _cursorGraph;

    private Point _pendingLocation;
    private ConnectorViewModel? _pendingSourceConnector;   // de onde o arrasto começou

    public PlannerViewModel() => RunRecipeSearch();

    partial void OnPickerSearchChanged(string value) => RunRecipeSearch();
    partial void OnMatchNameChanged(bool value) => RunRecipeSearch();
    partial void OnMatchInputsChanged(bool value) => RunRecipeSearch();
    partial void OnMatchOutputsChanged(bool value) => RunRecipeSearch();
    partial void OnIsPickerOpenChanged(bool value)
    {
        if (!value) _pendingSourceConnector = null;
    }

    private void RunRecipeSearch()
    {
        RecipeResults.Clear();
        foreach (var r in App.GameData.SearchRecipes(PickerSearch, MatchName, MatchInputs, MatchOutputs))
            RecipeResults.Add(r);
    }

    // Botão "+" e clique-direito -> add "do zero" (reseta filtros e origem).
    public void OpenPickerAt(Point graphLocation)
    {
        _pendingSourceConnector = null;
        _pendingLocation = graphLocation;
        MatchName = true; MatchInputs = false; MatchOutputs = false;
        PickerSearch = "";
        RunRecipeSearch();
        IsPickerOpen = true;
    }

    // QOL: arrastou de um conector e soltou no vazio.
    private void OpenPickerFromConnector(ConnectorViewModel c)
    {
        _pendingSourceConnector = c;
        _pendingLocation = CursorGraph;

        if (!string.IsNullOrEmpty(c.ItemName))
        {
            // puxei de uma SAÍDA (item saindo)  -> quero quem CONSOME -> casa em Entradas
            // puxei de uma ENTRADA (falta item) -> quero quem PRODUZ  -> casa em Saídas
            MatchName = false;
            MatchInputs = c.IsOutput;
            MatchOutputs = !c.IsOutput;
            PickerSearch = c.ItemName;
        }
        else
        {
            // conector curinga (ferramenta) -> abre sem filtro
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
        Nodes.Add(node);

        // --- auto-conexão (opcional): se veio de um arrasto, já liga no novo nó ---
        if (_pendingSourceConnector is { } origin)
        {
            var pool = origin.IsOutput ? node.Input : node.Output;
            var match = pool.FirstOrDefault(k => ItemsCompatible(origin.ItemClass, k.ItemClass))
                        ?? pool.FirstOrDefault();
            if (match is not null) TryConnect(origin, match);
        }
        // -----------------------------------------------------------------------

        IsPickerOpen = false;   // OnIsPickerOpenChanged limpa _pendingSourceConnector
    }

    [RelayCommand]
    private void ClosePicker() => IsPickerOpen = false;

    [RelayCommand]
    private void DeleteSelected()
    {
        var doomed = Nodes.Where(n => n.IsSelected).ToList();
        if (doomed.Count == 0) return;

        // conectores que pertencem aos nós que vão sair
        var dead = new HashSet<ConnectorViewModel>();
        foreach (var n in doomed)
        {
            foreach (var c in n.Input)  dead.Add(c);
            foreach (var c in n.Output) dead.Add(c);
        }

        // remove as conexões ligadas a esses nós
        foreach (var link in Connections
                    .Where(l => dead.Contains(l.Source) || dead.Contains(l.Target))
                    .ToList())
            Connections.Remove(link);

        // remove os nós
        foreach (var n in doomed)
            Nodes.Remove(n);

        // recalcula "conectado" nos conectores que sobraram
        // (uma saída pode alimentar vários; só desmarca se não sobrou nenhuma ligação)
        var live = new HashSet<ConnectorViewModel>();
        foreach (var l in Connections) { live.Add(l.Source); live.Add(l.Target); }
        foreach (var n in Nodes)
        {
            foreach (var c in n.Input)  c.IsConnected = live.Contains(c);
            foreach (var c in n.Output) c.IsConnected = live.Contains(c);
        }
    }

    // Vem do Nodify: soltou no conector (Target != null) OU no vazio (Target == null).
    [RelayCommand]
    private void CreateConnection((object? Source, object? Target) p)
    {
        if (p.Source is not ConnectorViewModel source) return;

        if (p.Target is null)              // soltou no vazio -> QOL
        {
            OpenPickerFromConnector(source);
            return;
        }

        if (p.Target is ConnectorViewModel target)
            TryConnect(source, target);
    }

    // Núcleo das REGRAS. Retorna false (e não conecta) se violar alguma.
    private bool TryConnect(ConnectorViewModel x, ConnectorViewModel y)
    {
        if (ReferenceEquals(x, y)) return false;
        if (x.IsOutput == y.IsOutput) return false;              // precisa ser saída -> entrada

        var output = x.IsOutput ? x : y;
        var input = x.IsOutput ? y : x;

        if (Connections.Any(c => ReferenceEquals(c.Target, input))) return false;  // 1 esteira por entrada
        if (!ItemsCompatible(output.ItemClass, input.ItemClass)) return false;     // tipos precisam bater

        Connections.Add(new ConnectionViewModel { Source = output, Target = input });
        output.IsConnected = true;
        input.IsConnected = true;
        return true;
    }

    // Curinga (null/"") aceita qualquer item; senão os ItemClass precisam ser iguais.
    private static bool ItemsCompatible(string? a, string? b)
        => string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b;
}