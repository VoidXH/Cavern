using System.Collections.Generic;
using System.Linq;
using System.Text;

using Cavern;
using Cavern.Channels;
using Cavern.Format.Renderers;
using Cavern.Utilities;

namespace CavernizeGUI {
    partial class MainWindow {
        /// <summary>
        /// Holds the post-render report.
        /// </summary>
        string report;

        /// <summary>
        /// Extracts the <see cref="report"/> from a render.
        /// </summary>
        void UpdatePostRenderReport(RenderStats stats) {
            StringBuilder builder = new();
            int total = listener.ActiveSources.Count;
            int dynamic = total - stats.GetSemiStaticCount() - stats.GetSuperStaticCount();

            List<ReferenceChannel> channels = stats.GetStaticOrSemiStaticPositions()
                .Select(x => Renderer.ChannelFromPosition(x / Listener.EnvironmentSize)).ToList();

            bool hasLFE = listener.ActiveSources.Any(source => source.LFE);
            if (hasLFE)
                channels.Add(ReferenceChannel.ScreenLFE);
            channels.Sort();

            int unused = total - dynamic - channels.Count;
            if (unused < 0) {
                dynamic += unused;
                unused = 0;
            }

            builder.Append("Actually present bed channels: ").Append(channels.Count);
            if (channels.Count != 0)
                builder.Append(" (").Append(string.Join(", ", channels)).Append(')');
            builder.AppendLine().Append("Actually present dynamic objects: ").AppendLine(dynamic.ToString())
                .Append("Unused (fake) rendering targets: ").AppendLine(unused.ToString());
            report = builder.ToString();
        }
    }
}