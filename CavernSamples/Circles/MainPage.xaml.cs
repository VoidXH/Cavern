using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Numerics;

using Cavern.Numerics;
using Cavern.Utilities;

using Circles.Models;

using Rectangle = Microsoft.Maui.Controls.Shapes.Rectangle;

namespace Circles;

/// <summary>
/// Main window layout for Circles.
/// </summary>
public partial class MainPage : ContentPage {
    /// <summary>
    /// MVVM wrapper for editing <see cref="Circle"/>s.
    /// </summary>
    public ObservableCollection<EditableCircle> Circles { get; set; } = [];

    /// <summary>
    /// How large should be the displayed iterations for geometric median and intersection.
    /// </summary>
    public int IterationDisplaySize { get; set; } = 10;

    /// <summary>
    /// Each <see cref="Circles"/> entry's UI control pair.
    /// </summary>
    readonly Dictionary<EditableCircle, Ellipse> representations = [];

    /// <summary>
    /// Visualized geometric median iterations.
    /// </summary>
    readonly Rectangle[] geomed;

    /// <summary>
    /// Visualized intersection iterations.
    /// </summary>
    readonly Rectangle[] intersection;

    /// <summary>
    /// Main window layout for Circles.
    /// </summary>
    public MainPage() {
        InitializeComponent();
        BindingContext = this;
        geomed = SetupIterations(10, Colors.Green);
        intersection = SetupIterations(10, Colors.Red);
    }

    /// <summary>
    /// Change if the geometric medians or intersections are visible.
    /// </summary>
    /// <param name="set"></param>
    /// <param name="value"></param>
    static void ChangeVisibility(Rectangle[] set, bool value) {
        for (int i = 0; i < set.Length; i++) {
            set[i].IsVisible = value;
        }
    }

    /// <summary>
    /// Visualize an iteration of an approximation.
    /// </summary>
    static void IterationChanged(Rectangle display, Vector2 position, double offset) => display.Margin = new(position.X - offset, position.Y - offset, 0, 0);

    /// <summary>
    /// Append a new circle at the end of the list.
    /// </summary>
    void AddCircle(object _, EventArgs e) {
        EditableCircle circle = new() {
            circle = new(new(100, 100), 50)
        };
        circle.PropertyChanged += CircleChanged;
        Circles.Add(circle);
    }

    /// <summary>
    /// Update the visual representation corresponding to the changed circle.
    /// </summary>
    void CircleChanged(object sender, PropertyChangedEventArgs _) {
        EditableCircle circle = (EditableCircle)sender;
        if (!representations.TryGetValue(circle, out Ellipse representation)) {
            representation = new Ellipse {
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Stroke = Brush.Blue,
                StrokeThickness = 2
            };
            canvas.Add(representation);
            representations[circle] = representation;
        }

        representation.Margin = new(circle.X - circle.Radius, circle.Y - circle.Radius, 0, 0);
        representation.WidthRequest = circle.Radius * 2;
        representation.HeightRequest = circle.Radius * 2;

        Circle[] circles = representations.SelectArray(x => x.Key.circle);
        double iterationOffset = IterationDisplaySize * .5;
        for (int i = 0; i < geomed.Length; i++) {
            IterationChanged(geomed[i], Circle.GeometricMedian(circles, i + 1), iterationOffset);
        }
        for (int i = 0; i < intersection.Length; i++) {
            IterationChanged(intersection[i], Circle.Intersect(circles, i + 1), iterationOffset);
        }
    }

    /// <summary>
    /// Show geometric median markers.
    /// </summary>
    void ShowGeomeds(object _, EventArgs e) => ChangeVisibility(geomed, true);

    /// <summary>
    /// Hide geometric median markers.
    /// </summary>
    void HideGeomeds(object _, EventArgs e) => ChangeVisibility(geomed, false);

    /// <summary>
    /// Show intersection markers.
    /// </summary>
    void ShowIntersections(object _, EventArgs e) => ChangeVisibility(intersection, true);

    /// <summary>
    /// Hide intersection markers.
    /// </summary>
    void HideIntersections(object _, EventArgs e) => ChangeVisibility(intersection, false);

    /// <summary>
    /// Create darkening iteration visualizations.
    /// </summary>
    Rectangle[] SetupIterations(int iterations, Color color) {
        Rectangle[] result = new Rectangle[iterations];
        for (int i = 0; i < result.Length; i++) {
            float colorMul = (i + 1) / (float)result.Length;
            result[i] = new Rectangle {
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                WidthRequest = IterationDisplaySize,
                HeightRequest = IterationDisplaySize,
                Stroke = new SolidColorBrush(new Color(color.Red * colorMul, color.Green * colorMul, color.Blue * colorMul)),
                StrokeThickness = 2
            };
            canvas.Add(result[i]);
        }
        return result;
    }
}
