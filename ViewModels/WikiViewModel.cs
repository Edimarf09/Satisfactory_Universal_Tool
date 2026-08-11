using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Satisfactory_Universal_Tool.Core.Data;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class WikiViewModel : ObservableObject
{
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private GameItem? _selectedItem;
    [ObservableProperty] private string _status = "";

    public ObservableCollection<GameItem> Results { get; } = new();

    public WikiViewModel() => RunSearch();

    partial void OnSearchTextChanged(string value) => RunSearch();

    private void RunSearch()
    {
        Results.Clear();
        foreach (var item in App.GameData.Search(SearchText))
            Results.Add(item);

        Status = $"{Results.Count} de {App.GameData.Items.Count} itens";
    }
}