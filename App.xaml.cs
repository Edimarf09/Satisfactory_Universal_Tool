using System.IO;
using System.Windows;
using Satisfactory_Universal_Tool.Core.Data;
using Satisfactory_Universal_Tool.Localization;

namespace Satisfactory_Universal_Tool;

public partial class App : Application
{
    // Uma instância só, acessível de qualquer lugar do app
    public static GameDataService GameData { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var baseDir       = System.AppContext.BaseDirectory;
        var docsFolder    = Path.Combine(baseDir, "CommunityResources", "Docs");
        var stringsFolder = Path.Combine(baseDir, "Strings");

        var lang = "en-US";   // idioma inicial; o seletor troca isso depois

        Loc.SetLanguage(lang, stringsFolder);   // carrega os textos da interface
        GameData.Load(docsFolder, lang);         // carrega os itens do jogo

        new MainWindow().Show();                 // abre a janela DEPOIS de carregar
    }
}