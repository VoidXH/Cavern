using Cavern.Utilities;
using System;

namespace Cavern.QuickEQ.Graphing.Overlays {
    /// <summary>
    /// Draws a grid over the graph with a gridline at every increment of the highest local value.
    /// </summary>
    public class LogScaleGrid : Frame {
        /// <summary>
        /// Inner line stroke width.
        /// </summary>
        readonly int gridWidth;

        /// <summary>
        /// Number of rows drawn, including the frame lines.
        /// </summary>
        readonly int ySteps;

        /// <summary>
        /// Draws a grid over the graph with a gridline at every increment of the highest local value.
        /// </summary>
        /// <param name="borderWidth">Border line stroke width</param>
        /// <param name="gridWidth">Inner line stroke width</param>
        /// <param name="color">RGBA color of the line</param>
        /// <param name="ySteps">Number of rows drawn, including the frame lines</param>
        public LogScaleGrid(int borderWidth, int gridWidth, uint color, int ySteps) : base(borderWidth, color) {
            this.gridWidth = gridWidth;
            this.ySteps = ySteps;
        }

        /// <summary>
        /// Adds the overlay to a graph.
        /// </summary>
        public override void DrawBehind(GraphRenderer target) {
            int gap = target.Height / ySteps;
            for (int y = 1; y < ySteps; y++) {
                DrawRow(target, y * gap, gridWidth, color);
            }

            float logStart = MathF.Log10(target.StartFrequency),
                logEnd = MathF.Log10(target.EndFrequency);
            for (int digit = (int)logStart, last = (int)logEnd; digit <= last; digit++) {
                for (int value = 1; value < 10; value++) {
                    float freq = value * MathF.Pow(10, digit);
                    if (freq > target.StartFrequency && freq < target.EndFrequency) {
                        DrawColumn(target, (int)(target.Width * QMath.LerpInverse(logStart, logEnd, MathF.Log10(freq))), gridWidth, color);
                    }
                }
            }
        }
    }
}