using System.Windows.Controls;

namespace CavernPipeServer.Windows;

/// <summary>
/// Multithreaded version of <see cref="ChannelMeters"/>, for use outside dispatchers.
/// </summary>
/// <param name="canvas">Add meters as children of this control</param>
/// <param name="labelProto">Reference channel name display</param>
/// <param name="barProto">Reference meter display</param>
public class ThreadSafeChannelMeters(Panel canvas, TextBlock labelProto, ProgressBar barProto) : ChannelMeters(canvas, labelProto, barProto) {
    /// <inheritdoc/>
    public override void Enable() {
        lock (canvas) {
            canvas.Dispatcher.Invoke(base.Enable);
        }
    }

    /// <inheritdoc/>
    public override void Disable() {
        lock (canvas) {
            canvas.Dispatcher.Invoke(base.Disable);
        }
    }

    /// <inheritdoc/>
    protected override void UpdateUI(float[] meters) {
        lock (canvas) {
            canvas.Dispatcher.Invoke(base.UpdateUI, meters);
        }
    }
}
