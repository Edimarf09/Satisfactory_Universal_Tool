using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class MapViewModel : ObservableObject
{
    // As 4 fatias originais do jogo, em resolução nativa (sem downscale).
    // Nomes seguem Map_X-Y (X = coluna esq->dir, Y = linha cima->baixo):
    //   Slice00 = Map_0-0 (topo-esq)   Slice10 = Map_1-0 (topo-dir)
    //   Slice01 = Map_0-1 (baixo-esq)  Slice11 = Map_1-1 (baixo-dir)
    [ObservableProperty] private BitmapImage? _slice00;
    [ObservableProperty] private BitmapImage? _slice10;
    [ObservableProperty] private BitmapImage? _slice01;
    [ObservableProperty] private BitmapImage? _slice11;

    [ObservableProperty] private bool _hasMap;
    [ObservableProperty] private string _status = "";

    public MapViewModel()
    {
        // Coloque as 4 fatias extraídas em  <app>/Map/SlicedMap/
        var dir = Path.Combine(System.AppContext.BaseDirectory, "Map", "SlicedMap");

        var f00 = Path.Combine(dir, "Map_0-0.png");
        var f10 = Path.Combine(dir, "Map_1-0.png");
        var f01 = Path.Combine(dir, "Map_0-1.png");
        var f11 = Path.Combine(dir, "Map_1-1.png");

        if (File.Exists(f00) && File.Exists(f10) && File.Exists(f01) && File.Exists(f11))
        {
            Slice00 = Load(f00);
            Slice10 = Load(f10);
            Slice01 = Load(f01);
            Slice11 = Load(f11);
            HasMap = true;
        }
        else
        {
            HasMap = false;
            Status = "Mapa não encontrado.\n" +
                     "Coloque as 4 fatias em Map/SlicedMap/:\n" +
                     "Map_0-0.png, Map_1-0.png, Map_0-1.png, Map_1-1.png";
        }
    }

    // Carrega o PNG inteiro na memória (OnLoad) e libera o arquivo — não trava o
    // .png e não perde resolução. Freeze() deixa a imagem imutável/thread-safe.
    private static BitmapImage Load(string path)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new System.Uri(path, System.UriKind.Absolute);
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
}