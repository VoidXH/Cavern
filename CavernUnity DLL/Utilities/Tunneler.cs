using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>
    /// Provides synchronization between Unity and Cavern objects.
    /// </summary>
    public static class Tunneler {
        /// <summary>
        /// Provides the <paramref name="source"/> a clip from either Cavern or Unity.
        /// </summary>
        public static void TunnelClips(ref Clip source, AudioClip unity, Clip cavern, ref int lastClipHash) {
            if (cavern) {
                if (!source || lastClipHash != cavern.GetHashCode()) {
                    source = cavern;
                    lastClipHash = cavern.GetHashCode();
                }
            } else if (unity) {
                if (!source || lastClipHash != unity.GetHashCode()) {
                    float[] AllData = new float[unity.channels * unity.samples];
                    unity.GetData(AllData, 0);
                    source = new Clip(AllData, unity.channels, unity.frequency);
                    lastClipHash = unity.GetHashCode();
                }
            } else if (source && lastClipHash != 0) {
                source = null;
                lastClipHash = 0;
            }
        }
    }
}