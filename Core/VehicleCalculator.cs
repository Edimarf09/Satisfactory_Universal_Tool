using System;
using System.Collections.Generic;

namespace Satisfactory_Universal_Tool.Core;

public record Vehicle(string Name, int Slots);

public static class VehicleCatalog
{
    public static readonly Vehicle FreightCar = new("Vagão de carga", 32);
    public static readonly Vehicle Truck      = new("Caminhão", 48);
    public static readonly Vehicle Tractor    = new("Tractor", 25);
    public static readonly Vehicle Explorer   = new("Explorer", 12);

    public static IReadOnlyList<Vehicle> All { get; } =
        new[] { FreightCar, Truck, Tractor, Explorer };
}

public record VehicleCalcRequest(
    double DemandPerMinute,
    int StackSize,
    Vehicle Vehicle,
    double RoundTripSeconds);

public record VehicleCalcResult(
    int VehiclesNeeded,
    double CapacityPerTrip,
    double ThroughputPerVehicle,
    double TotalThroughput,
    IReadOnlyList<string> Notes);

public static class VehicleCalculator
{
    public static VehicleCalcResult Solve(VehicleCalcRequest req)
    {
        var notes = new List<string>();
        double rtdMin = req.RoundTripSeconds / 60.0;

        double capacityPerTrip = req.Vehicle.Slots * (double)req.StackSize;
        double perVehicle = rtdMin > 0 ? capacityPerTrip / rtdMin : 0;
        int needed = perVehicle > 0 ? (int)Math.Ceiling(req.DemandPerMinute / perVehicle) : 0;
        double total = needed * perVehicle;

        notes.Add($"Cada {req.Vehicle.Name} leva {capacityPerTrip:0} itens por viagem ({req.Vehicle.Slots} slots × {req.StackSize}/pilha).");
        notes.Add($"Com essa rota, cada um entrega ~{perVehicle:0.#}/min.");
        if (needed > 0)
            notes.Add($"{needed} deles cobrem {total:0.#}/min ({total - req.DemandPerMinute:0.#}/min de folga).");
        if (req.Vehicle == VehicleCatalog.FreightCar)
            notes.Add("São vagões — você pode pendurar vários numa mesma locomotiva.");
        notes.Add("Valor teórico. A trava de ~27s por carga/descarga e as esteiras que abastecem a estação reduzem isso na prática — deixe uma folga.");

        return new VehicleCalcResult(needed, capacityPerTrip, perVehicle, total, notes);
    }
}