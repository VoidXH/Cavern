using Cavern.MAUI.Models;
using Cavern.Numerics;

namespace Circles.Models;

/// <summary>
/// A user-editable <see cref="Circle"/> instance.
/// </summary>
public partial class EditableCircle : NotifyPropertyChanged {
    /// <summary>
    /// Center point of the circle on the Y axis.
    /// </summary>
    public float X {
        get => circle.Center.X;
        set {
            circle.Center = new(value, circle.Center.Y);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Center point of the circle on the Y axis.
    /// </summary>
    public float Y {
        get => circle.Center.Y;
        set {
            circle.Center = new(circle.Center.X, value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Radius of the circle.
    /// </summary>
    public float Radius {
        get => circle.Radius;
        set {
            circle.Radius = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// The underlying editable instance.
    /// </summary>
    public Circle circle;
}
