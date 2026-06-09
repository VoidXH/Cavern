using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cavern.Channels;
using Cavernize.Logic.Models.RenderTargets;

namespace CavernizeAvalonia;

public sealed class SpeakerLayoutView : Control {
    public static readonly StyledProperty<RenderTarget> RenderTargetProperty =
        AvaloniaProperty.Register<SpeakerLayoutView, RenderTarget>(nameof(RenderTarget));

    public RenderTarget RenderTarget {
        get => GetValue(RenderTargetProperty);
        set => SetValue(RenderTargetProperty, value);
    }

    static SpeakerLayoutView() =>
        RenderTargetProperty.Changed.AddClassHandler<SpeakerLayoutView>((view, _) => view.InvalidateVisual());

    protected override Size MeasureOverride(Size availableSize) {
        bool hasWidth = double.IsFinite(availableSize.Width) && availableSize.Width > 0,
            hasHeight = double.IsFinite(availableSize.Height) && availableSize.Height > 0;
        double width = hasWidth ? availableSize.Width : sourceWidth,
            height = hasHeight ? availableSize.Height : sourceHeight,
            scale = Math.Min(width / sourceWidth, height / sourceHeight);
        return new Size(sourceWidth * scale, sourceHeight * scale);
    }

    public override void Render(DrawingContext context) {
        base.Render(context);

        Rect area = FitSourceAspect(new Rect(Bounds.Size).Deflate(6));
        double scale = Math.Min(area.Width / sourceWidth, area.Height / sourceHeight),
            stroke = Math.Max(.75, 1.4 * scale),
            radius = Math.Max(2.5, 6.5 * scale);
        Pen white = new(Brushes.White, stroke),
            gray = new(new SolidColorBrush(Color.Parse("#8A8A8A")), stroke);

        Rect front = Rect(area, 46, 0, 76, 50);
        Rect rear = Rect(area, 14, 73, 140, 85);
        context.DrawRectangle(null, white, front);
        context.DrawRectangle(null, gray, rear);
        DrawLine(context, gray, area, 46, 0, 14, 73);
        DrawLine(context, gray, area, 122, 0, 154, 73);
        DrawLine(context, gray, area, 46, 50, 14, 158);
        DrawLine(context, gray, area, 122, 50, 154, 158);

        HashSet<ReferenceChannel> active = [];
        if (RenderTarget != null) {
            ReferenceChannel[] channels = RenderTarget.Channels;
            for (int channel = 0; channel < channels.Length; channel++) {
                if (RenderTarget.IsExported(channel) && channels[channel] != ReferenceChannel.ScreenLFE) {
                    active.Add(channels[channel]);
                }
            }
        }
        foreach ((ReferenceChannel channel, double x, double y) in speakers) {
            DrawSpeaker(context, area, x, y, radius, active.Contains(channel));
        }
    }

    static void DrawSpeaker(DrawingContext context, Rect area, double x, double y, double radius, bool active) {
        Point center = Scale(area, x + 5, y + 5);
        IBrush fill = active ? activeSpeaker : inactiveSpeaker;
        context.DrawEllipse(fill, null, center, radius, radius);
    }

    static void DrawLine(DrawingContext context, Pen pen, Rect area, double x1, double y1, double x2, double y2) =>
        context.DrawLine(pen, Scale(area, x1, y1), Scale(area, x2, y2));

    static Rect Rect(Rect area, double x, double y, double width, double height) =>
        new(Scale(area, x, y), ScaleSize(area, width, height));

    static Point Scale(Rect area, double x, double y) =>
        new(area.X + area.Width * x / sourceWidth, area.Y + area.Height * y / sourceHeight);

    static Size ScaleSize(Rect area, double width, double height) =>
        new(area.Width * width / sourceWidth, area.Height * height / sourceHeight);

    static Rect FitSourceAspect(Rect bounds) {
        double scale = Math.Min(bounds.Width / sourceWidth, bounds.Height / sourceHeight),
            width = sourceWidth * scale,
            height = sourceHeight * scale;
        return new Rect(
            bounds.X + (bounds.Width - width) * .5,
            bounds.Y + (bounds.Height - height) * .5,
            width,
            height);
    }

    static readonly IBrush activeSpeaker = new SolidColorBrush(Color.Parse("#2E91D6"));
    static readonly IBrush inactiveSpeaker = new SolidColorBrush(Color.Parse("#9A9A9A"));

    static readonly (ReferenceChannel channel, double x, double y)[] speakers = [
        (ReferenceChannel.FrontLeft, 51, 30),
        (ReferenceChannel.FrontCenter, 79, 30),
        (ReferenceChannel.FrontRight, 107, 30),
        (ReferenceChannel.TopFrontLeft, 51, 5),
        (ReferenceChannel.TopFrontCenter, 79, 5),
        (ReferenceChannel.TopFrontRight, 107, 5),
        (ReferenceChannel.WideLeft, 36, 40),
        (ReferenceChannel.WideRight, 126, 40),
        (ReferenceChannel.SideLeft, 20, 91),
        (ReferenceChannel.SideRight, 142, 91),
        (ReferenceChannel.TopSideLeft, 29, 53),
        (ReferenceChannel.TopSideRight, 129, 53),
        (ReferenceChannel.TopRearLeft, 46, 62),
        (ReferenceChannel.TopRearCenter, 79, 62),
        (ReferenceChannel.TopRearRight, 112, 62),
        (ReferenceChannel.RearLeft, 34, 118),
        (ReferenceChannel.RearCenter, 79, 118),
        (ReferenceChannel.RearRight, 124, 118)
    ];

    const double sourceWidth = 170;
    const double sourceHeight = 160;
}
