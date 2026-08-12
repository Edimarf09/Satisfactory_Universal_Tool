using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Satisfactory_Universal_Tool.Views;

public partial class MapView : UserControl
{
    private const double MapSize = 8192;      // 2x2 de fatias 4096
    private bool _initialized;
    private bool _isPanning;
    private bool _syncing;                     // evita loop slider <-> zoom
    private Point _panStart;
    private Matrix _matrix = Matrix.Identity;
    // Limites do mundo (unidades do mapa): canto sup-esq da 0-0 até canto inf-dir da 1-1
    private const double WorldMinX = -3246, WorldMaxX = 4253;
    private const double WorldMinY = -3750, WorldMaxY = 3750;

    public MapView() => InitializeComponent();

    private void Viewport_Loaded(object sender, RoutedEventArgs e) => CenterIfNeeded();
    private void Viewport_SizeChanged(object sender, SizeChangedEventArgs e) => CenterIfNeeded();

    // Abre em 100% com o encontro das 4 fatias (centro do mapa) no meio da viewport.
    private void CenterIfNeeded()
    {
        if (_initialized || Viewport.ActualWidth <= 0) return;
        _matrix = Matrix.Identity;             // escala 1 = 100%
        _matrix.OffsetX = Viewport.ActualWidth  / 2 - MapSize / 2;
        _matrix.OffsetY = Viewport.ActualHeight / 2 - MapSize / 2;
        Apply();
        _initialized = true;
    }

    private void Apply()
    {
        MapMatrix.Matrix = _matrix;
        if (ZoomLabel != null) ZoomLabel.Text = $"{_matrix.M11 * 100:0}%";
    }

    // Zoom ABSOLUTO (escala alvo) ancorado num ponto da viewport.
    private void ZoomTo(double targetScale, Point anchor)
    {
        double min = Math.Pow(2, ZoomSlider.Minimum);
        double max = Math.Pow(2, ZoomSlider.Maximum);
        targetScale = Math.Clamp(targetScale, min, max);

        double factor = targetScale / _matrix.M11;
        _matrix.ScaleAt(factor, factor, anchor.X, anchor.Y);
        Apply();

        _syncing = true;                       // reposiciona o slider sem re-disparar zoom
        ZoomSlider.Value = Math.Log2(targetScale);
        _syncing = false;
    }

    // Slider -> zoom ancorado no CENTRO da viewport.
    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded || _syncing || Viewport == null) return;
        ZoomTo(Math.Pow(2, ZoomSlider.Value),
               new Point(Viewport.ActualWidth / 2, Viewport.ActualHeight / 2));
    }

    // Ctrl+scroll -> zoom ancorado no PONTEIRO do mouse.
    private void Viewport_Zoom(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        double step = e.Delta > 0 ? 1.25 : 1 / 1.25;
        ZoomTo(_matrix.M11 * step, e.GetPosition(Viewport));
        e.Handled = true;
    }

    // ---- Pan: segura o botão esquerdo e arrasta (funciona em qualquer zoom) ----
    private void Map_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _panStart = e.GetPosition(Viewport);
        Viewport.CaptureMouse();
        Viewport.Cursor = Cursors.SizeAll;        // setas pra todos os lados
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(Viewport);

        // pan (só enquanto segura o botão esquerdo)
        if (_isPanning)
        {
            _matrix.Translate(p.X - _panStart.X, p.Y - _panStart.Y);
            _panStart = p;
            Apply();
        }

        // ponteiro (viewport) -> pixel do mapa -> coordenada de mundo
        // usa a matriz invertida pra achar o pixel sob o cursor
        var inv = _matrix;
        if (!inv.HasInverse) return;
        inv.Invert();
        Point px = inv.Transform(p);

        // clamp no pixel => quando sai do mapa, o número congela no limite
        double cx = Math.Clamp(px.X, 0, MapSize);
        double cy = Math.Clamp(px.Y, 0, MapSize);

        double worldX = WorldMinX + (cx / MapSize) * (WorldMaxX - WorldMinX);
        double worldY = WorldMinY + (cy / MapSize) * (WorldMaxY - WorldMinY);

        if (CoordLabel != null)
            CoordLabel.Text = $"X: {worldX,7:0}   Y: {worldY,7:0}";
    }

    private void Map_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning) return;
        _isPanning = false;
        Viewport.ReleaseMouseCapture();
        Viewport.Cursor = Cursors.Arrow;
    }
}