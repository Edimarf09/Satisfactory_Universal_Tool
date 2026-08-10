using System.Windows;
using Satisfactory_Universal_Tool.ViewModels;

namespace Satisfactory_Universal_Tool;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();   // conecta a tela ao ViewModel
    }
}