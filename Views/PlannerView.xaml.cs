using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Satisfactory_Universal_Tool.ViewModels;

namespace Satisfactory_Universal_Tool.Views;

public partial class PlannerView : UserControl
{
    // era 300; a janela de 2 painéis é mais larga:
    private const double PickerWidth = 560;
    private const double PickerHeight = 440;

    // onde o botão-direito desceu; usado pra distinguir clique de arrasto (pan)
    private Point _rmbDown;

    public PlannerView()
    {
        InitializeComponent();

        // O Nodify usa o botão-direito pra PAN, então pegamos os eventos
        // mesmo que ele os marque como handled (handledEventsToo: true).
        Editor.AddHandler(PreviewMouseRightButtonDownEvent,
            new MouseButtonEventHandler(Editor_PreviewMouseRightButtonDown), true);
        Editor.AddHandler(MouseRightButtonUpEvent,
            new MouseButtonEventHandler(Editor_MouseRightButtonUp), true);
    }

    private PlannerViewModel? Vm => DataContext as PlannerViewModel;

    // ---- Botão "+": adiciona no CENTRO do viewport ----
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;

        // Centro do viewport em coordenadas do GRAFO.
        // ViewportSize já vem em unidades do grafo (ActualWidth / zoom).
        var center = new Point(
            Editor.ViewportLocation.X + Editor.ViewportSize.Width / 2,
            Editor.ViewportLocation.Y + Editor.ViewportSize.Height / 2);

        ShowPicker(
            offsetX: (Editor.ActualWidth - PickerWidth) / 2,
            offsetY: (Editor.ActualHeight - PickerHeight) / 2,
            graphLocation: center);
    }

    // ---- Botão-direito: abre a janelinha no ponto do mouse ----
    private void Editor_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        => _rmbDown = e.GetPosition(Editor);

    private void Editor_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var up = e.GetPosition(Editor);

        // Se arrastou, foi PAN -> não abre o menu.
        if ((up - _rmbDown).Length > 6) return;

        // MouseLocation do Nodify já está em coordenadas do GRAFO.
        ShowPicker(offsetX: up.X, offsetY: up.Y, graphLocation: Editor.MouseLocation);
        e.Handled = true;
    }

    // Posiciona a janelinha (em pixels, relativa ao editor) e abre o picker
    // pedindo pro VM criar o próximo nó em 'graphLocation'.
    private void ShowPicker(double offsetX, double offsetY, Point graphLocation)
    {
        if (Vm is null) return;

        PickerPopup.Placement = PlacementMode.Relative;
        PickerPopup.PlacementTarget = Editor;
        PickerPopup.HorizontalOffset = offsetX;
        PickerPopup.VerticalOffset = offsetY;

        Vm.OpenPickerAt(graphLocation);
    }

    private void PickerPopup_Opened(object sender, EventArgs e) => SearchBox.Focus();
}
