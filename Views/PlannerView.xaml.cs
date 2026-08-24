using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Satisfactory_Universal_Tool.ViewModels;

namespace Satisfactory_Universal_Tool.Views;

public partial class PlannerView : UserControl
{
    private const double PickerWidth = 560;
    private const double PickerHeight = 440;
    private Point _rmbDown;

    public PlannerView()
    {
        InitializeComponent();

        // Pegamos os eventos mesmo se o Nodify marcar como handled (handledEventsToo: true).
        Editor.AddHandler(PreviewMouseRightButtonDownEvent,
            new MouseButtonEventHandler(Editor_PreviewMouseRightButtonDown), true);
        Editor.AddHandler(MouseRightButtonUpEvent,
            new MouseButtonEventHandler(Editor_MouseRightButtonUp), true);
        Editor.AddHandler(MouseMoveEvent,
            new MouseEventHandler(Editor_MouseMove), true);
    }

    private PlannerViewModel? Vm => DataContext as PlannerViewModel;

    // Converte pixel do editor -> coordenada do GRAFO.
    private Point ToGraph(Point pixel) => new(
        Editor.ViewportLocation.X + pixel.X / Editor.ViewportZoom,
        Editor.ViewportLocation.Y + pixel.Y / Editor.ViewportZoom);

    // Mantém a posição do cursor (no grafo) no VM — usado no QOL de arrasto pro vazio.
    private void Editor_MouseMove(object sender, MouseEventArgs e)
    {
        if (Vm is not null)
            Vm.CursorGraph = ToGraph(e.GetPosition(Editor));
    }

    // Botão "+": adiciona no CENTRO do viewport.
    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is null) return;

        var center = new Point(
            Editor.ViewportLocation.X + Editor.ViewportSize.Width / 2,
            Editor.ViewportLocation.Y + Editor.ViewportSize.Height / 2);

        ShowPicker((Editor.ActualWidth - PickerWidth) / 2,
                   (Editor.ActualHeight - PickerHeight) / 2, center);
    }

    // Botão-direito: abre a janelinha no ponto do mouse.
    private void Editor_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        => _rmbDown = e.GetPosition(Editor);

    private void Editor_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        var up = e.GetPosition(Editor);
        if ((up - _rmbDown).Length > 6) return;   // foi arrasto (pan) -> ignora

        ShowPicker(up.X, up.Y, ToGraph(up));
        e.Handled = true;
    }

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