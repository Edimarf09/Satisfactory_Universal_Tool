namespace Satisfactory_Universal_Tool.ViewModels;

public class MainViewModel
{
    public BalancerViewModel Balancer { get; } = new();
    public TrainViewModel Train { get; } = new();
    public WikiViewModel Wiki { get; } = new();
}