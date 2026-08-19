using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Satisfactory_Universal_Tool.Core.Data;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class PlannerViewModel : ObservableObject
{
    public ObservableCollection<PlannerNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    // ===== Janela de seleção =====
    // Esquerda: ferramentas fixas. Direita: receitas (busca dinâmica).
    public IReadOnlyList<ToolDescriptor> Tools => NodeCatalog.Tools;
    public ObservableCollection<GameRecipe> RecipeResults { get; } = new();

    [ObservableProperty] private bool _isPickerOpen;
    [ObservableProperty] private string _pickerSearch = "";

    // Toggles do topo: em qual campo a busca (da direita) casa.
    [ObservableProperty] private bool _matchName = true;
    [ObservableProperty] private bool _matchInputs;
    [ObservableProperty] private bool _matchOutputs;

    // Onde o próximo nó nasce (coords do GRAFO).
    private Point _pendingLocation;

    public PlannerViewModel() => RunRecipeSearch();

    partial void OnPickerSearchChanged(string value) => RunRecipeSearch();
    partial void OnMatchNameChanged(bool value) => RunRecipeSearch();
    partial void OnMatchInputsChanged(bool value) => RunRecipeSearch();
    partial void OnMatchOutputsChanged(bool value) => RunRecipeSearch();

    private void RunRecipeSearch()
    {
        RecipeResults.Clear();
        foreach (var r in App.GameData.SearchRecipes(PickerSearch, MatchName, MatchInputs, MatchOutputs))
            RecipeResults.Add(r);
    }

    // Chamado pela View (botão "+" -> centro; botão-direito -> mouse).
    public void OpenPickerAt(Point graphLocation)
    {
        _pendingLocation = graphLocation;
        PickerSearch = "";     // dispara re-filtro
        RunRecipeSearch();
        IsPickerOpen = true;
    }

    [RelayCommand]
    private void PickTool(ToolDescriptor? t)
    {
        if (t is null) return;
        Nodes.Add(NodeCatalog.FromTool(t, _pendingLocation));
        IsPickerOpen = false;
    }

    [RelayCommand]
    private void PickRecipe(GameRecipe? r)
    {
        if (r is null) return;
        Nodes.Add(NodeCatalog.FromRecipe(r, _pendingLocation));
        IsPickerOpen = false;
    }

    [RelayCommand]
    private void ClosePicker() => IsPickerOpen = false;

    // ===== Conexões (mantido) =====
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