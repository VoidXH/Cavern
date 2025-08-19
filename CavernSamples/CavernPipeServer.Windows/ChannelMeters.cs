using System;
using System.Windows;
using System.Windows.Controls;

using Cavern;
using Cavern.Channels;
using Cavern.WPF.Consts;

namespace CavernPipeServer.Windows;

/// <summary>
/// Display channel output meters on a <see cref="Panel"/>.
/// </summary>
/// <param name="canvas">Add meters as children of this control</param>
/// <param name="labelProto">Reference channel name display</param>
/// <param name="barProto">Reference meter display</param>
public class ChannelMeters(Panel canvas, TextBlock labelProto, ProgressBar barProto) {
    /// <summary>
    /// Meters are displayed on this control.
    /// </summary>
    protected Panel canvas = canvas;

    /// <summary>
    /// Display the last gain updates of the channels on these controls.
    /// </summary>
    (TextBlock, ProgressBar)[] displays;

    /// <summary>
    /// To prevent slowdowns caused by too many UI updates, collect the peaks over longer time intervals and update with said peaks.
    /// </summary>
    protected float[] movingPeaks;

    /// <summary>
    /// When to update the meters.
    /// </summary>
    DateTime updateAt;

    /// <summary>
    /// Create the UI elements for displaying meter values later. The first call to this function is always after the <see cref="Listener"/>'s creation,
    /// so the <see cref="Listener.Channels"/> are what will be rendered.
    /// </summary>
    public virtual void Enable() {
        ReferenceChannel[] channels = ChannelPrototype.GetReferences(Listener.Channels);
        displays = new (TextBlock, ProgressBar)[channels.Length];
        movingPeaks = new float[channels.Length];
        for (int i = 0; i < channels.Length; i++) {
            double marginTop = labelProto.Margin.Top + (labelProto.Height + 5) * i;
            TextBlock channelName = new TextBlock {
                Margin = new Thickness(labelProto.Margin.Left, marginTop, labelProto.Margin.Right, 0),
                HorizontalAlignment = labelProto.HorizontalAlignment,
                VerticalAlignment = labelProto.VerticalAlignment,
                Text = channels[i].Translate()
            };
            ProgressBar progressBar = new ProgressBar {
                Margin = new Thickness(barProto.Margin.Left, marginTop, barProto.Margin.Right, 0),
                HorizontalAlignment = barProto.HorizontalAlignment,
                VerticalAlignment = barProto.VerticalAlignment,
                Width = barProto.Width,
                Height = barProto.Height,
                Maximum = 1
            };
            displays[i] = (channelName, progressBar);
            canvas.Children.Add(channelName);
            canvas.Children.Add(progressBar);
        }
    }

    /// <summary>
    /// Update the displayed meter values of each channel if they exist.
    /// </summary>
    public void Update(float[] meters) {
        if (displays == null) {
            return;
        }

        for (int i = 0; i < movingPeaks.Length; i++) {
            if (movingPeaks[i] < meters[i]) {
                movingPeaks[i] = meters[i];
            }
        }

        if (updateAt < DateTime.Now) {
            UpdateUI(movingPeaks);
            Array.Clear(movingPeaks);
            updateAt = DateTime.Now + updateInterval;
        }
    }

    /// <summary>
    /// Remove the channel output meters from the UI.
    /// </summary>
    public virtual void Disable() {
        if (displays == null) {
            return;
        }

        for (int i = 0; i < displays.Length; i++) {
            canvas.Children.Remove(displays[i].Item1);
            canvas.Children.Remove(displays[i].Item2);
        }
        displays = null;
    }

    /// <summary>
    /// The part of <see cref="Update(float[])"/> that requires a dispatcher.
    /// </summary>
    /// <param name="meters"></param>
    protected virtual void UpdateUI(float[] meters) {
        if (displays == null) {
            return;
        }

        for (int i = 0, c = Math.Min(displays.Length, meters.Length); i < c; i++) {
            displays[i].Item2.Value = meters[i];
        }
    }

    /// <summary>
    /// How often to update the meters.
    /// </summary>
    static readonly TimeSpan updateInterval = TimeSpan.FromSeconds(.05);
}
