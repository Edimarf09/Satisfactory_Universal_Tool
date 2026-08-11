using System.Windows.Controls;
using System.Windows.Input;

namespace Satisfactory_Universal_Tool.Views;

public partial class MapView : UserControl
{
    public MapView() => InitializeComponent();

    private void Scroll_Zoom(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return; // Ctrl+scroll = zoom
        double factor = e.Delta > 0 ? 1.1 : 1 / 1.1;
        MapScale.ScaleX = System.Math.Clamp(MapScale.ScaleX * factor, 0.1, 10);
        MapScale.ScaleY = MapScale.ScaleX;
        e.Handled = true;
    }
}