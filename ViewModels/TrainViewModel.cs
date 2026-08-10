using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Satisfactory_Universal_Tool.Core;

namespace Satisfactory_Universal_Tool.ViewModels;

public partial class TrainViewModel : ObservableObject
{
    [ObservableProperty] private string _demandText = "780";
    [ObservableProperty] private string _stackSizeText = "100";
    [ObservableProperty] private Vehicle _selectedVehicle = VehicleCatalog.FreightCar;
    [ObservableProperty] private string _roundTripText = "120";
    [ObservableProperty] private string _resultText = "";

    public IReadOnlyList<Vehicle> Vehicles { get; } = VehicleCatalog.All;

    [RelayCommand]
    private void Calculate()
    {
        try
        {
            double demand = ParseNum(DemandText);
            int stack = (int)ParseNum(StackSizeText);
            double rtd = ParseNum(RoundTripText);

            var r = VehicleCalculator.Solve(
                new VehicleCalcRequest(demand, stack, SelectedVehicle, rtd));

            var sb = new StringBuilder();
            sb.AppendLine($"Necessário: {r.VehiclesNeeded} × {SelectedVehicle.Name}");
            sb.AppendLine();
            foreach (var n in r.Notes) sb.AppendLine("•  " + n);
            ResultText = sb.ToString();
        }
        catch (FormatException)
        {
            ResultText = "Valores inválidos. Use só números.";
        }
    }

    private static double ParseNum(string s) =>
        double.Parse(s.Trim().Replace(',', '.'), CultureInfo.InvariantCulture);
}