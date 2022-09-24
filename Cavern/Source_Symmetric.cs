using System.Runtime.CompilerServices;

namespace Cavern {
    public partial class Source {
        // ------------------------------------------------------------------
        // Helpers for the symmetric renderer
        // ------------------------------------------------------------------
        /// <summary>
        /// Inverse lerp, but returns 0 when the values are equal.
        /// </summary>
        /// <param name="a">Start position</param>
        /// <param name="b">End position</param>
        /// <param name="x">Intermediate position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Ratio(float a, float b, float x) {
            if (a == b) {
                return 0;
            }
            return (x - a) / (b - a);
        }

        /// <summary>
        /// Check and assign a channel if it's the closest left/right from a given position.
        /// </summary>
        /// <param name="channel">Checked channel ID</param>
        /// <param name="left">Closest left channel ID</param>
        /// <param name="right">Closest right channel ID</param>
        /// <param name="posX">Reference position on the X axis</param>
        /// <param name="channelX">Currently checked channel position on the X axis</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssignLR(int channel, ref int left, ref int right, float posX, float channelX) {
            if (channelX == posX) { // Exact match
                left = channel;
                right = channel;
            } else if (channelX < posX) { // Left
                if (left == -1 || Listener.Channels[left].CubicalPos.X < channelX) {
                    left = channel;
                }
            } else if (right == -1 || Listener.Channels[right].CubicalPos.X > channelX) { // Right
                right = channel;
            }
        }

        /// <summary>
        /// For a given horizontal layer, if it's over a side of the room, fill blank speakers.
        /// </summary>
        /// <param name="frontLeft">Front left ID</param>
        /// <param name="frontRight">Front right ID</param>
        /// <param name="rearLeft">Rear left ID</param>
        /// <param name="rearRight">Rear right ID</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void FixIncompleteLayer(ref int frontLeft, ref int frontRight, ref int rearLeft, ref int rearRight) {
            if (frontLeft != -1 || frontRight != -1) {
                if (frontLeft == -1) {
                    frontLeft = frontRight;
                }
                if (frontRight == -1) {
                    frontRight = frontLeft;
                }
                if (rearLeft == -1 && rearRight == -1) {
                    rearLeft = frontLeft;
                    rearRight = frontRight;
                }
            }
            if (rearLeft != -1 || rearRight != -1) {
                if (rearLeft == -1) {
                    rearLeft = rearRight;
                }
                if (rearRight == -1) {
                    rearRight = rearLeft;
                }
                if (frontLeft == -1 && frontRight == -1) {
                    frontLeft = rearLeft;
                    frontRight = rearRight;
                }
            }
        }
    }
}