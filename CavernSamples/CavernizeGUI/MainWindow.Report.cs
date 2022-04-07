using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Cavern;
using Cavern.Format.Renderers;
using Cavern.Remapping;
using Cavern.Utilities;

namespace CavernizeGUI {
    partial class MainWindow {
        void UpdatePostRenderReport(RenderStats stats) {
            StringBuilder builder = new();
            int total = listener.ActiveSources.Count;
            int dynamic = total - stats.GetSemiStaticCount() - stats.GetSuperStaticCount();

            List<ReferenceChannel> channels = stats.GetStaticOrSemiStaticPositions().Select(x => {
                Vector3 scaled = x / Listener.EnvironmentSize;
                for (int i = 0; i < Renderer.channelPositions.Length; ++i)
                    if (scaled == Renderer.channelPositions[i])
                        return (ReferenceChannel)i;
                return ReferenceChannel.Unknown;
            }).ToList();

            bool hasLFE = listener.ActiveSources.Any(source => source.LFE);
            if (hasLFE)
                channels.Add(ReferenceChannel.ScreenLFE);
            channels.Sort();

            int unused = total - dynamic - channels.Count;
            if (unused < 0) {
                if (hasLFE)
                    channels.Remove(ReferenceChannel.FrontCenter);
                unused = 0;
            }

            builder.Append("Actually present bed channels: ").Append(channels.Count);
            if (channels.Count != 0)
                builder.Append(" (").Append(string.Join(", ", channels)).AppendLine(")");
            builder.Append("Actually present dynamic objects: ").AppendLine(dynamic.ToString())
                .Append("Unused (fake) rendering targets: ").AppendLine(unused.ToString());
            report.Dispatcher.Invoke(() => report.Text = builder.ToString());
        } 
    }
}