using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaPath = Avalonia.Controls.Shapes.Path;

namespace RBWStack.Avalonia.Views;

public class SvgIcon : Viewbox
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<SvgIcon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<SvgIcon, IBrush?>(nameof(Stroke), Brushes.Gray);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<SvgIcon, double>(nameof(StrokeThickness), 1.8);

    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<SvgIcon, double>(nameof(Size), 16.0);

    public Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    private readonly AvaloniaPath _path;

    static SvgIcon()
    {
        DataProperty.Changed.AddClassHandler<SvgIcon>((s, e) => s._path.Data = e.NewValue as Geometry);
        StrokeProperty.Changed.AddClassHandler<SvgIcon>((s, e) => s._path.Stroke = e.NewValue as IBrush);
        StrokeThicknessProperty.Changed.AddClassHandler<SvgIcon>((s, e) => s._path.StrokeThickness = (double)(e.NewValue ?? 1.8));
        SizeProperty.Changed.AddClassHandler<SvgIcon>((s, e) =>
        {
            double sz = (double)(e.NewValue ?? 16.0);
            s.Width = sz;
            s.Height = sz;
        });
    }

    public SvgIcon()
    {
        Width = Size;
        Height = Size;
        Stretch = Stretch.Uniform;
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _path = new AvaloniaPath
        {
            StrokeThickness = StrokeThickness,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round
        };

        var canvas = new Canvas
        {
            Width = 24,
            Height = 24
        };
        canvas.Children.Add(_path);
        Child = canvas;
    }
}
