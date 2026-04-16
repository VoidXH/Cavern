using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cavern.MAUI.Models;

/// <summary>
/// Boilerplate implementation of <see cref="INotifyPropertyChanged"/>, only <see cref="OnPropertyChanged"/> has to be called when a property was changed.
/// </summary>
public abstract class NotifyPropertyChanged : INotifyPropertyChanged {
    /// <inheritdoc/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Call without setting the <paramref name="propertyName"/> when any property was changed to notify the <see cref="PropertyChanged"/> event.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
