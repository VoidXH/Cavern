using Microsoft.Maui.Graphics.Platform;
using System.Runtime.CompilerServices;

using Cavern.Format.Image;
using Cavern.QuickEQ.Graphing;

using IImage = Microsoft.Maui.Graphics.IImage;
using Image = Microsoft.Maui.Controls.Image;

namespace Cavern.MAUI.Utilities;

/// <summary>
/// Extension functions for interoperability between Cavern's raw ARGB pixel arrays and WPF's <see cref="Bitmap"/>.
/// </summary>
public static partial class IImageUtils {
    /// <summary>
    /// Make an image <paramref name="holder"/> have a fixed <paramref name="maxEdgeLength"/> while keeping the <paramref name="image"/>'s aspect ratio.
    /// </summary>
    public static void MaxEdgeSizing(this Image holder, IImage image, double maxEdgeLength) => MaxEdgeSizing(holder, image.Width, image.Height, maxEdgeLength);

    /// <summary>
    /// Make an image <paramref name="holder"/> have a fixed <paramref name="maxEdgeLength"/> while keeping the image's aspect ratio.
    /// </summary>
    public static void MaxEdgeSizing(this Image holder, double imageWidth, double imageHeight, double maxEdgeLength) {
        double maxEdge = Math.Max(imageWidth, imageHeight);
        double scaling = maxEdgeLength / maxEdge;
        holder.WidthRequest = imageWidth * scaling;
        holder.HeightRequest = imageHeight * scaling;
    }

    /// <summary>
    /// Convert an array of ARGB pixels to a <see cref="IImage"/> that can be used in MAUI.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IImage ToIImage(this GraphRenderer renderer) => renderer.Pixels.ToIImage(renderer.Width, renderer.Height);

    /// <summary>
    /// Convert an array of ARGB pixels to an <see cref="IImage"/> that can be used in MAUI.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IImage ToIImage(this uint[] argb, int width, int height) => PlatformImage.FromStream(ToBGRA(argb, width, height));

    /// <summary>
    /// Convert an array of ARGB pixels to an <see cref="ImageSource"/> that can be displayed in MAUI.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImageSource ToImageSource(this GraphRenderer renderer) => renderer.Pixels.ToImageSource(renderer.Width, renderer.Height);

    /// <summary>
    /// Convert an array of ARGB pixels to an <see cref="ImageSource"/> that can be displayed in MAUI.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImageSource ToImageSource(this uint[] argb, int width, int height) => ImageSource.FromStream(() => ToBGRA(argb, width, height));

    /// <summary>
    /// Convert an array of ARGB pixels to a <see cref="MemoryStream"/> in BGRA format that can be used in MAUI.
    /// </summary>
    static MemoryStream ToBGRA(uint[] argb, int width, int height) {
        MemoryStream result = new();
        new Bitmap(width, height, argb).Write(result);
        result.Position = 0;
        return result;
    }
}
