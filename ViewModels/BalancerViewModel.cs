using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Satisfactory_Universal_Tool.Core;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class BalancerViewModel : ObservableObject
{
    // [ObservableProperty] num campo _inputsText gera a propriedade "InputsText"
    // que a tela consegue enxergar e que avisa a UI sozinha quando muda.
    [ObservableProperty] private string _inputsText = "600\n600";
    [ObservableProperty] private string _outputsText = "500\n500";
    [ObservableProperty] private BeltTier _selectedTier = BeltTier.Mk3;
    [ObservableProperty] private string _resultText = "";

    // Alimenta o dropdown de tiers com todos os valores do enum
    public IReadOnlyList<BeltTier> Tiers { get; } = Enum.GetValues<BeltTier>();

    // [RelayCommand] no método Calculate() gera "CalculateCommand",
    // que a gente liga no botão.
    [RelayCommand]
    private void Calculate()
    {
        try
        {
            var inputs = ParseRates(InputsText);
            var outputs = ParseRates(OutputsText);
            var r = BeltBalancer.Solve(new BalancerRequest(inputs, outputs, SelectedTier));

            var sb = new StringBuilder();
            sb.AppendLine($"Entrada total: {r.TotalIn:0.##}/min");
            sb.AppendLine($"Saída total:   {r.TotalOut:0.##}/min");
            sb.AppendLine($"Diferença:     {r.Difference:0.##}/min");
            sb.AppendLine($"Status:        {r.Status}");
            sb.AppendLine();
            if (r.Warnings.Count == 0)
                sb.AppendLine("Sem avisos — tudo certo!");
            else
                foreach (var w in r.Warnings) sb.AppendLine("•  " + w);

            ResultText = sb.ToString();
        }
        catch (FormatException)
        {
            ResultText = "Valor inválido. Use só números (um por linha ou separados por espaço).";
        }
    }

    // Separe os valores por linha ou espaço. Vírgula conta como casa decimal (78,75 = 78.75).
    private static IReadOnlyList<double> ParseRates(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<double>();
        return text
            .Split(new[] { '\n', '\r', ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => double.Parse(t.Replace(',', '.'), CultureInfo.InvariantCulture))
            .ToList();
    }
}