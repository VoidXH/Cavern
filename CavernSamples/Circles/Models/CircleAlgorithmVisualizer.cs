using System.Numerics;

using Cavern.Numerics;

using Rectangle = Microsoft.Maui.Controls.Shapes.Rectangle;

namespace Circles.Models;

/// <summary>
/// Displays each iteration of iterative inter-circle point finder algorithms.
/// </summary>
public sealed class CircleAlgorithmVisualizer {
    /// <summary>
    /// How large should be the displayed iterations for geometric median and intersection.
    /// </summary>
    public int IterationDisplaySize {
        get => iterationDisplaySize;
        set {
            iterationDisplaySize = value;
            Reset();
        }
    }
    int iterationDisplaySize = 10;

    /// <summary>
    /// Number of iterations.
    /// </summary>
    public int IterationCount {
        get => iterationCount;
        set {
            iterationCount = value;
            Reset();
        }
    }
    int iterationCount = 10;

    /// <summary>
    /// The parent to which the indicators are attached.
    /// </summary>
    readonly Grid canvas;

    /// <summary>
    /// Visualized equidistant point iterations.
    /// </summary>
    Rectangle[] equidistant;

    /// <summary>
    /// Visualized geometric median iterations.
    /// </summary>
    Rectangle[] geomed;

    /// <summary>
    /// Visualized intersection iterations.
    /// </summary>
    Rectangle[] intersection;

    /// <summary>
    /// Displays each iteration of iterative inter-circle point finder algorithms.
    /// </summary>
    public CircleAlgorithmVisualizer(Grid canvas) {
        this.canvas = canvas;
        Reset();
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
    /// Show or hide <see cref="equidistant"/> iterations.
    /// </summary>
    public void ChangeEquidistantVisibility(bool value) => ChangeVisibility(equidistant, value);

    /// <summary>
    /// Show or hide <see cref="geomed"/> iterations.
    /// </summary>
    public void ChangeGeomedVisibility(bool value) => ChangeVisibility(geomed, value);

    /// <summary>
    /// Show or hide <see cref="intersection"/> iterations.
    /// </summary>
    public void ChangeIntersectionVisibility(bool value) => ChangeVisibility(intersection, value);

    /// <summary>
    /// Calculate new values from new <paramref name="circles"/>.
    /// </summary>
    public void Update(Circle[] circles) {
        double iterationOffset = IterationDisplaySize * .5;
        for (int i = 0; i < equidistant.Length; i++) {
            IterationChanged(equidistant[i], Circle.EquidistantPoint(circles, i + 1), iterationOffset);
        }
        for (int i = 0; i < geomed.Length; i++) {
            IterationChanged(geomed[i], Circle.GeometricMedian(circles, i + 1), iterationOffset);
        }
        for (int i = 0; i < intersection.Length; i++) {
            IterationChanged(intersection[i], Circle.Intersect(circles, i + 1), iterationOffset);
        }
    }

    /// <summary>
    /// Recreate the visualizations on the <see cref="canvas"/>.
    /// </summary>
    void Reset() {
        if (equidistant != null) {
            TearDownIterations(equidistant);
            TearDownIterations(geomed);
            TearDownIterations(intersection);
        }

        equidistant = SetupIterations(IterationCount, Colors.Blue);
        geomed = SetupIterations(IterationCount, Colors.Green);
        intersection = SetupIterations(IterationCount, Colors.Red);
    }

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

    /// <summary>
    /// Remove the visible iterations from the <see cref="canvas"/>.
    /// </summary>
    void TearDownIterations(Rectangle[] items) {
        for (int i = 0; i < items.Length; i++) {
            canvas.Remove(items[i]);
        }
    }
}
