using System;
using System.Collections.Generic;

namespace Cavern.Channels {
    /// <summary>
    /// Utility class for calculating speaker layout symmetry.
    /// </summary>
    public static class SymmetryCalculator {
        /// <summary>
        /// Calculates if the given channel layout is symmetric.
        /// </summary>
        /// <param name="channels">Channels to check for symmetry.</param>
        /// <returns>True if the layout is symmetric, false otherwise.</returns>
        public static bool CalculateSymmetry(Channel[] channels) {
            if (channels == null || channels.Length == 0) {
                return true;
            }

            int channelCount = channels.Length;

            // Handle odd channel count: the unpaired channel must be centerline or LFE
            if ((channelCount & 1) == 1) {
                channelCount--;
                Channel channel = channels[channelCount];
                if (channel.Y % 180 != 0 && !channel.LFE) {
                    return false;
                }
            }

            // Track non-LFE, non-centerline channels, and keep only the unpaired ones
            Dictionary<(int X, int Y), int> unpaired = new Dictionary<(int X, int Y), int>();
            for (int i = 0; i < channelCount; i++) {
                Channel current = channels[i];
                if (current == null) {
                    continue;
                }

                if (!current.LFE) {
                    if (current.Y % 180 == 0) {
                        continue; // Skip centerline
                    }

                    int y = (int)MathF.Round(current.Y) % 360;
                    if (y < 0) {
                        y += 360;
                    }
                    int x = (int)MathF.Round(current.X);
                    (int, int) mirrorKey = (x, (360 - y) % 360);
                    if (unpaired.TryGetValue(mirrorKey, out int count)) {
                        if (count == 1) {
                            unpaired.Remove(mirrorKey);
                        } else {
                            unpaired[mirrorKey]--;
                        }
                    } else {
                        unpaired[(x, y)] = 1;
                    }
                }
            }

            return unpaired.Count == 0; // Layout is symmetric if all speakers found their mirror pairs
        }
    }
}
