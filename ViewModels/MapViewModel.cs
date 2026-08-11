using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class MapViewModel : ObservableObject
{
    [ObservableProperty] private string? _mapImagePath;
    [ObservableProperty] private bool _hasMap;
    [ObservableProperty] private string _status = "";

    public MapViewModel()
    {
        // procura um mapa em <app>/Map/map.png (por enquanto um arquivo só; tiles vêm depois)
        var path = Path.Combine(System.AppContext.BaseDirectory, "Map", "map.png");
        if (File.Exists(path))
        {
            MapImagePath = path;
            HasMap = true;
        }
        else
        {
            HasMap = false;
            Status = "Nenhum mapa carregado.\nColoque um 'map.png' na pasta Map/ do aplicativo.";
        }
    }
}