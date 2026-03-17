using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
    /// Displays the iterations of inter-circle point finder algorithms.
    /// </summary>
    readonly CircleAlgorithmVisualizer visualizer;

    /// <summary>
    /// Each <see cref="Circles"/> entry's UI control pair.
    /// </summary>
    readonly Dictionary<EditableCircle, Ellipse> representations = [];

    /// <summary>
    /// Main window layout for Circles.
    /// </summary>
    public MainPage() {
        InitializeComponent();
        visualizer = new(canvas);
        BindingContext = this;
    }

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
        visualizer.Update(representations.SelectArray(x => x.Key.circle));
    }

    /// <summary>
    /// Show equidistant markers.
    /// </summary>
    void ShowEquidistants(object _, EventArgs e) => visualizer.ChangeEquidistantVisibility(true);

    /// <summary>
    /// Hide equidistant markers.
    /// </summary>
    void HideEquidistants(object _, EventArgs e) => visualizer.ChangeEquidistantVisibility(false);

    /// <summary>
    /// Show geometric median markers.
    /// </summary>
    void ShowGeomeds(object _, EventArgs e) => visualizer.ChangeGeomedVisibility(true);

    /// <summary>
    /// Hide geometric median markers.
    /// </summary>
    void HideGeomeds(object _, EventArgs e) => visualizer.ChangeGeomedVisibility(false);

    /// <summary>
    /// Show intersection markers.
    /// </summary>
    void ShowIntersections(object _, EventArgs e) => visualizer.ChangeIntersectionVisibility(true);

    /// <summary>
    /// Hide intersection markers.
    /// </summary>
    void HideIntersections(object _, EventArgs e) => visualizer.ChangeIntersectionVisibility(false);
}
