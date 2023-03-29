using Cavern.Channels;
using Cavern.Utilities;

namespace CavernizeGUI.Elements {
    /// <summary>
    /// A render target that renders for a different layout than what's written to the file.
    /// This layout is achieved through merging some of the rendered channels together.
    /// Mainly used for X.X.2 front exports, in those cases, the rears are mixed to the ground to provide elevation only at the screen.
    /// </summary>
    /// <remarks>Only merge the last channels to any other, as those can be efficiently left out while exporting, and only the
    /// first <see cref="OutputChannels"/> will be kept.</remarks>
    class DownmixedRenderTarget : RenderTarget {
        /// <summary>
        /// The exact <see cref="OutputChannels"/>, what will have an output.
        /// These are the channels that are not just virtual.
        /// </summary>
        public override ReferenceChannel[] WiredChannels {
            get {
                ReferenceChannel[] result = new ReferenceChannel[OutputChannels];
                int resultIndex = 0;
                for (int i = 0; i < Channels.Length; i++) {
                    bool written = true;
                    for (int j = 0; j < merge.Length; j++) {
                        if (merge[j].source == i) {
                            written = false;
                            break;
                        }
                    }
                    if (written) {
                        result[resultIndex++] = Channels[i];
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Some channels can be wired such that their terminals will be connected to a different channel's + terminal.
        /// If they broadcast the same signal, but in different phase, they'll get added together
        /// </summary>
        public (ReferenceChannel source, ReferenceChannel posPhase, ReferenceChannel negPhase)[] MatrixWirings {
            get {
                int count = 0;
                for (int i = 0; i < merge.Length; i++) {
                    if (merge[i].source < 0) {
                        count++;
                    }
                }

                (ReferenceChannel, ReferenceChannel, ReferenceChannel)[] result =
                    new (ReferenceChannel, ReferenceChannel, ReferenceChannel)[count];
                count = 0;
                for (int i = 0; i < merge.Length; i++) {
                    if (merge[i].source < 0) {
                        result[count++] = (Channels[~merge[i].source], Channels[merge[i - 1].target], Channels[merge[i].target]);
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Channels pairs in the form of (a, b) where a is mixed into b and then thrown away. When the source is 2's complement, a
        /// phase-inverted downmix will be performed.
        /// </summary>
        /// <remarks>Positive and negative matrix wiring terminals must follow each other.</remarks>
        readonly (int source, int target)[] merge;

        /// <summary>
        /// A render target that renders for a different layout than what's written to the file.
        /// This layout is achieved through merging some of the rendered channels together.
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="channels">Channels that are used for rendering</param>
        /// <param name="merge">Channels pairs in the form of (a, b) where a is mixed into b and then thrown away</param>
        /// <remarks>Only merge the last channels to any other, as those can be efficiently left out while exporting, and only the
        /// first <see cref="OutputChannels"/> will be kept.</remarks>
        public DownmixedRenderTarget(string name, ReferenceChannel[] channels, params (int, int)[] merge) :
            base(name, channels) {
            this.merge = merge;
            int merged = merge.Length;
            for (int i = 0; i < merge.Length; i++) {
                if (merge[i].Item1 < 0) {
                    --merged; // Don't count L - R mixes twice
                }
            }
            OutputChannels = channels.Length - merged;
        }

        /// <summary>
        /// Gets if a channel is actually present in the final file or just used for downmixing.
        /// </summary>
        public override bool IsExported(int index) {
            for (int i = 0; i < merge.Length; i++) {
                if (merge[i].source == index || ~merge[i].source == index) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Perform the channel mixing by the <see cref="merge"/> mapping.
        /// </summary>
        public void PerformMerge(float[] samples) {
            for (int i = 0; i < merge.Length; i++) {
                if (i + 1 < merge.Length && merge[i].source == ~merge[i + 1].source) { // Matrix channel, + phase
                    WaveformUtils.Mix(samples, merge[i].source, merge[i].target, Channels.Length, .5f);
                } else if (merge[i].source >= 0) { // Regular channel
                    WaveformUtils.Mix(samples, merge[i].source, merge[i].target, Channels.Length);
                } else { // Matrix channel, - phase
                    WaveformUtils.Mix(samples, ~merge[i].source, merge[i].target, Channels.Length, -.5f);
                }
            }
        }
    }
}