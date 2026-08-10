using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Markup;

namespace Satisfactory_Universal_Tool.Localization;

// Núcleo: guarda o idioma atual e o dicionário carregado
public static class Loc
{
    public static string CurrentLanguage { get; private set; } = "en-US";
    private static Dictionary<string, string> _ui = new();

    public static void SetLanguage(string code, string stringsFolder)
    {
        CurrentLanguage = code;
        var path = Path.Combine(stringsFolder, $"{code}-dictionary.json");
        _ui = File.Exists(path)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? new()
            : new();
        LocProvider.Instance.Refresh();   // avisa a interface pra reler os textos
    }

    public static string T(string key) => _ui.TryGetValue(key, out var v) ? v : key;
}

// Ponte pro binding do WPF: indexador que a tela consulta por chave
public class LocProvider : INotifyPropertyChanged
{
    public static LocProvider Instance { get; } = new();
    public string this[string key] => Loc.T(key);
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
}

// Deixa o XAML escrever {loc:Tr chave} de forma limpa
public class TrExtension : MarkupExtension
{
    public string Key { get; set; } = "";
    public TrExtension() { }
    public TrExtension(string key) { Key = key; }

    public override object ProvideValue(IServiceProvider sp)
    {
        var binding = new Binding($"[{Key}]") { Source = LocProvider.Instance, Mode = BindingMode.OneWay };
        return binding.ProvideValue(sp);
    }
}