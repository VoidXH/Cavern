using System.Windows;
using System.Windows.Media.Animation;

namespace VoidX.WPF {
    /// <summary>
    /// Common animations for WPF projects.
    /// </summary>
    public static class Animator {
        /// <summary>
        /// Make a control roll down from the top.
        /// </summary>
        public static void RollDown(FrameworkElement control, int milliseconds) {
            DoubleAnimation animation = CreateDoubleAnimation(0, control.Height, milliseconds);
            control.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        /// <summary>
        /// Make a control roll up to the top, performing an animation after rolled up.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="milliseconds"></param>
        /// <param name="afterAnimation"></param>
        public static void RollUp(FrameworkElement control, int milliseconds, Action afterAnimation) {
            DoubleAnimation animation = CreateDoubleAnimation(control.Height, 0, milliseconds);
            animation.Completed += (_, __) => afterAnimation?.Invoke();
            control.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        /// <summary>
        /// Shorthand for setting up a <see cref="DoubleAnimation"/> with the given parameters.
        /// </summary>
        static DoubleAnimation CreateDoubleAnimation(double from, double to, int durationMS) => new DoubleAnimation {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(durationMS),
            AccelerationRatio = 0.3,
            DecelerationRatio = 0.7
        };
    }
}
