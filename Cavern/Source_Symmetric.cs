using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern {
    public partial class Source {
        // ------------------------------------------------------------------
        // Helpers for the symmetric renderer
        // ------------------------------------------------------------------
        /// <summary>Width ratio of a point between two channels.</summary>
        /// <param name="left">Left channel ID</param>
        /// <param name="right">Right channel ID</param>
        /// <param name="pos">Point X position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float WidthRatio(int left, int right, float pos) {
            if (left == right)
                return .5f;
            float leftX = Listener.Channels[left].CubicalPos.x;
            return (pos - leftX) / (Listener.Channels[right].CubicalPos.x - leftX);
        }

        /// <summary>Length ratio of a point between two channels.</summary>
        /// <param name="rear">Rear channel ID</param>
        /// <param name="front">Front channel ID</param>
        /// <param name="pos">Point Z position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float LengthRatio(int rear, int front, float pos) {
            if (rear == front)
                return .5f;
            float rearZ = Listener.Channels[rear].CubicalPos.z;
            return (pos - rearZ) / (Listener.Channels[front].CubicalPos.z - rearZ);
        }

        /// <summary>Check and assign a channel if it's the closest left/right from a given position.</summary>
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
                if (left == -1 || Listener.Channels[left].CubicalPos.x < channelX)
                    left = channel;
            } else if (right == -1 || Listener.Channels[right].CubicalPos.x > channelX) // Right
                right = channel;
        }

        /// <summary>Get the closest channels to a source in each direction.</summary>
        /// <param name="channel">Checked channel ID</param>
        /// <param name="frontLeft">Closest front left channel ID</param>
        /// <param name="frontRight">Closest front right channel ID</param>
        /// <param name="rearLeft">Closest rear left channel ID</param>
        /// <param name="rearRight">Closest rear right channel ID</param>
        /// <param name="closestFront">Closest front layer z position</param>
        /// <param name="closestRear">Closest rear layer z position</param>
        /// <param name="position">Reference position</param>
        /// <param name="channelPos">Currently checked channel position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssignHorizontalLayer(int channel, ref int frontLeft, ref int frontRight, ref int rearLeft, ref int rearRight,
            ref float closestFront, ref float closestRear, Vector position, Vector channelPos) {
            if (channelPos.z > position.z) { // Front
                if (channelPos.z < closestFront) { // Front layer selection
                    closestFront = channelPos.z;
                    frontLeft = frontRight = -1;
                }
                if (channelPos.z == closestFront)
                    AssignLR(channel, ref frontLeft, ref frontRight, position.x, channelPos.x);
            } else { // Rear
                if (channelPos.z > closestRear) { // Rear layer selection
                    closestRear = channelPos.z; rearLeft = rearRight = -1;
                }
                if (channelPos.z == closestRear)
                    AssignLR(channel, ref rearLeft, ref rearRight, position.x, channelPos.x);
            }
        }

        /// <summary>For a given horizontal layer, if it's over a side of the room, fill blank speakers.</summary>
        /// <param name="frontLeft">Front left ID</param>
        /// <param name="frontRight">Front right ID</param>
        /// <param name="rearLeft">Rear left ID</param>
        /// <param name="rearRight">Rear right ID</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void FixIncompleteLayer(ref int frontLeft, ref int frontRight, ref int rearLeft, ref int rearRight) {
            if (frontLeft == -1 || frontRight == -1 || rearLeft == -1 || rearRight == -1) {
                if (frontLeft != -1 || frontRight != -1) {
                    if (frontLeft == -1)
                        frontLeft = frontRight;
                    if (frontRight == -1)
                        frontRight = frontLeft;
                    if (rearLeft == -1 && rearRight == -1) {
                        rearLeft = frontLeft;
                        rearRight = frontRight;
                    }
                }
                if (rearLeft != -1 || rearRight != -1) {
                    if (rearLeft == -1)
                        rearLeft = rearRight;
                    if (rearRight == -1)
                        rearRight = rearLeft;
                    if (frontLeft == -1 && frontRight == -1) {
                        frontLeft = rearLeft;
                        frontRight = rearRight;
                    }
                }
            }
        }
    }
}