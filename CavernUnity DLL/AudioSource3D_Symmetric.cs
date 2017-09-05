using System.Runtime.CompilerServices;
using UnityEngine;

namespace Cavern {
    public partial class AudioSource3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Helpers for the symmetric renderer
        // ------------------------------------------------------------------
        /// <summary>Width ratio of a point between two channels.</summary>
        /// <param name="Left">Left channel ID</param>
        /// <param name="Right">Right channel ID</param>
        /// <param name="Pos">Point X position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float WidthRatio(int Left, int Right, float Pos) {
            if (Left == Right)
                return .5f;
            float LeftX = AudioListener3D.Channels[Left].CubicalPos.x;
            return (Pos - LeftX) / (AudioListener3D.Channels[Right].CubicalPos.x - LeftX);
        }

        /// <summary>Length ratio of a point between two channels.</summary>
        /// <param name="Rear">Rear channel ID</param>
        /// <param name="Front">Front channel ID</param>
        /// <param name="Pos">Point Z position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float LengthRatio(int Rear, int Front, float Pos) {
            if (Rear == Front)
                return .5f;
            float RearZ = AudioListener3D.Channels[Rear].CubicalPos.z;
            return (Pos - RearZ) / (AudioListener3D.Channels[Front].CubicalPos.z - RearZ);
        }

        /// <summary>Check and assign a channel if it's the closest left/right from a given position.</summary>
        /// <param name="Channel">Checked channel ID</param>
        /// <param name="Left">Closest left channel ID</param>
        /// <param name="Right">Closest right channel ID</param>
        /// <param name="Position">Reference position</param>
        /// <param name="ChannelPos">Currently checked channel position</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssignLR(int Channel, ref int Left, ref int Right, Vector3 Position, Vector3 ChannelPos) {
            if (ChannelPos.x < Position.x) { // Left
                if (Left == -1 || AudioListener3D.Channels[Left].CubicalPos.x < ChannelPos.x) Left = Channel;
            } else if (Right == -1 || AudioListener3D.Channels[Right].CubicalPos.x > ChannelPos.x) Right = Channel; // Right
        }

        /// <summary>For a given horizontal layer, if it's over a side of the room, fill blank speakers.</summary>
        /// <param name="FL">Front left ID</param>
        /// <param name="FR">Front right ID</param>
        /// <param name="RL">Rear left ID</param>
        /// <param name="RR">Rear right ID</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void FixIncompleteLayer(ref int FL, ref int FR, ref int RL, ref int RR) {
            if (FL == -1 || FR == -1 || RL == -1 || RR == -1) {
                if (FL != -1 || FR != -1) {
                    if (FL == -1) FL = FR;
                    if (FR == -1) FR = FL;
                    if (RL == -1 && RR == -1) { RL = FL; RR = FR; }
                }
                if (RL != -1 || RR != -1) {
                    if (RL == -1) RL = RR;
                    if (RR == -1) RR = RL;
                    if (FL == -1 && FR == -1) { FL = RL; FR = RR; }
                }
            }
        }
    }
}
