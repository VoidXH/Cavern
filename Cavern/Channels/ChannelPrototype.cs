using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.Channels {
    /// <summary>
    /// Light audio channel information structure.
    /// </summary>
    public partial struct ChannelPrototype : IEquatable<ChannelPrototype> {
        /// <summary>
        /// Horizontal axis angle.
        /// </summary>
        public readonly float Y;

        /// <summary>
        /// Vertical axis angle.
        /// </summary>
        public readonly float X;

        /// <summary>
        /// Channel name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// True if the channel is used for Low Frequency Effects.
        /// </summary>
        public readonly bool LFE;

        /// <summary>
        /// Mute status.
        /// </summary>
        /// <remarks>Some channels should not be played back on the spatial master,
        /// like hearing/visually impaired tracks.</remarks>
        public readonly bool Muted;

        /// <summary>
        /// Standard channel constructor.
        /// </summary>
        /// <param name="y">Horizontal axis angle</param>
        /// <param name="name">Channel name</param>
        /// <param name="LFE">True if the channel is used for Low Frequency Effects</param>
        /// <param name="muted">Mute status</param>
        ChannelPrototype(float y, string name, bool LFE = false, bool muted = false) {
            X = 0;
            Y = y;
            Name = name;
            this.LFE = LFE;
            Muted = muted;
        }

        /// <summary>
        /// Spatial channel constructor.
        /// </summary>
        /// <param name="y">Horizontal axis angle</param>
        /// <param name="x">Vertical axis angle</param>
        /// <param name="name">Channel name</param>
        ChannelPrototype(float y, float x, string name) {
            Y = y;
            X = x;
            Name = name;
            LFE = Muted = false;
        }

        /// <summary>
        /// Converts a standard channel shorthand to a <see cref="ChannelPrototype"/>.
        /// </summary>
        public static ReferenceChannel FromStandardName(string name) {
            switch (name) {
                case frontLeftMark:
                case frontLeftMarkFull:
                    return ReferenceChannel.FrontLeft;
                case frontRightMark:
                case frontRightMarkFull:
                    return ReferenceChannel.FrontRight;
                case frontCenterMark:
                case frontCenterMarkFull:
                    return ReferenceChannel.FrontCenter;
                case screenLFEMark:
                case subwooferMark:
                    return ReferenceChannel.ScreenLFE;
                case rearLeftMark:
                case rearLeftMarkDCI:
                    return ReferenceChannel.RearLeft;
                case rearRightMark:
                case rearRightMarkDCI:
                    return ReferenceChannel.RearRight;
                case sideLeftMark:
                case sideLeftMarkDCI:
                    return ReferenceChannel.SideLeft;
                case sideRightMark:
                case sideRightMarkDCI:
                    return ReferenceChannel.SideRight;
                case topSideLeftMarkDCI:
                    return ReferenceChannel.TopSideLeft;
                case topSideRightMarkDCI:
                    return ReferenceChannel.TopSideRight;
                default:
                    return ReferenceChannel.Unknown;
            }
        }

        /// <summary>
        /// Gets a home cinema standard channel matrix for a given channel count.
        /// </summary>
        /// <remarks>If the channel count is larger than the largest supported layout, it will be filled with
        /// <see cref="ReferenceChannel.Unknown"/>.</remarks>
        public static ReferenceChannel[] GetStandardMatrix(int count) => GetStandardMatrix(HomeStandardMatrix, count);

        /// <summary>
        /// Gets an industry standard channel matrix for a given channel count.
        /// </summary>
        /// <remarks>If the channel count is larger than the largest supported layout, it will be filled with
        /// <see cref="ReferenceChannel.Unknown"/>.</remarks>
        public static ReferenceChannel[] GetIndustryStandardMatrix(int count) => GetStandardMatrix(IndustryStandardMatrix, count);

        /// <summary>
        /// Get a <paramref name="channel"/>'s <see cref="ChannelPrototype"/> of the home standard layout
        /// with a given number of <paramref name="channels"/>.
        /// </summary>
        public static ChannelPrototype Get(int channel, int channels) {
            int prototypeID = (int)HomeStandardMatrix[channels][channel];
            return Mapping[prototypeID];
        }

        /// <summary>
        /// Convert a mapping of <see cref="ReferenceChannel"/>s to <see cref="ChannelPrototype"/>s.
        /// </summary>
        public static ChannelPrototype[] Get(ReferenceChannel[] source) {
            ChannelPrototype[] result = new ChannelPrototype[source.Length];
            for (int i = 0; i < source.Length; i++) {
                result[i] = Mapping[(int)source[i]];
            }
            return result;
        }

        /// <summary>
        /// Convert a mapping of <see cref="ReferenceChannel"/>s to <see cref="ChannelPrototype"/>s with <see cref="AlternativePositions"/>.
        /// </summary>
        public static ChannelPrototype[] GetAlternative(ReferenceChannel[] source) {
            ChannelPrototype[] result = new ChannelPrototype[source.Length];
            for (int i = 0; i < source.Length; i++) {
                int index = (int)source[i];
                ChannelPrototype old = Mapping[index];
                Channel moved = new Channel(AlternativePositions[index], old.LFE);
                if (old.X == 0) {
                    result[i] = new ChannelPrototype(moved.Y, old.Name, old.LFE, old.Muted);
                } else {
                    result[i] = new ChannelPrototype(moved.Y, moved.X, old.Name);
                }
            }
            return result;
        }

        /// <summary>
        /// Convert a <see cref="Channel"/> to the name of the channel, if its position is known.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetName(Channel source) => GetName(GetReference(source));

        /// <summary>
        /// Convert a <see cref="ReferenceChannel"/> to the name of the channel.
        /// </summary>
        public static string GetName(ReferenceChannel source) => Mapping[(int)source].Name;

        /// <summary>
        /// Convert a mapping of <see cref="Channel"/>s to the names of the channels.
        /// </summary>
        public static string[] GetNames(Channel[] source) {
            string[] result = new string[source.Length];
            for (int i = 0; i < source.Length; i++) {
                result[i] = GetName(source[i]);
            }
            return result;
        }

        /// <summary>
        /// Convert a mapping of <see cref="ReferenceChannel"/>s to the names of the channels.
        /// </summary>
        public static string[] GetNames(ReferenceChannel[] source) {
            string[] result = new string[source.Length];
            for (int i = 0; i < source.Length; i++) {
                result[i] = Mapping[(int)source[i]].Name;
            }
            return result;
        }

        /// <summary>
        /// Get which channel this placement represents.
        /// </summary>
        public static ReferenceChannel GetReference(Channel source) {
            if (source.LFE) {
                return ReferenceChannel.ScreenLFE;
            }
            for (int i = 0; i < Mapping.Length; i++) {
                if (Math.Abs(Mapping[i].X - source.X) < .05f && Math.Abs(Mapping[i].Y - source.Y) < .05f) {
                    return (ReferenceChannel)i;
                }
            }
            Vector3 cubicalPos = source.CubicalPos;
            for (int i = 0; i < AlternativePositions.Length; i++) {
                if (AlternativePositions[i].CloseTo(cubicalPos, .05f)) {
                    return (ReferenceChannel)i;
                }
            }
            return ReferenceChannel.Unknown;
        }

        /// <summary>
        /// Get which channels this layout represents.
        /// </summary>
        public static ReferenceChannel[] GetReferences(Channel[] source) {
            ReferenceChannel[] result = new ReferenceChannel[source.Length];
            for (int i = 0; i < source.Length; i++) {
                result[i] = GetReference(source[i]);
            }
            return result;
        }

        /// <summary>
        /// Convert a mapping of <see cref="ReferenceChannel"/>s to channel name initials.
        /// </summary>
        public static string[] GetShortNames(ReferenceChannel[] source) => source.Select(x => x.GetShortName()).ToArray();

        /// <summary>
        /// Convert a prototype array to their corresponding alternative positions in the current environment.
        /// </summary>
        public static Vector3[] ToAlternativePositions(ChannelPrototype[] source) {
            Vector3[] result = new Vector3[source.Length];
            for (int channel = 0; channel < source.Length; ++channel) {
                result[channel] = AlternativePositions[channel] * Listener.EnvironmentSize;
            }
            return result;
        }

        /// <summary>
        /// Convert a prototype array to their corresponding alternative positions in the current environment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3[] ToAlternativePositions(ReferenceChannel[] source) => ToAlternativePositions(Get(source));

        /// <summary>
        /// Convert a prototype array to their corresponding positions in the current environment.
        /// </summary>
        public static Vector3[] ToPositions(ChannelPrototype[] source) {
            Vector3[] result = new Vector3[source.Length];
            for (int channel = 0; channel < source.Length; ++channel) {
                result[channel] = new Channel(source[channel].X, source[channel].Y, source[channel].LFE).SpatialPos *
                    Listener.EnvironmentSize;
            }
            return result;
        }

        /// <summary>
        /// Convert a prototype array to their corresponding positions in the current environment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3[] ToPositions(ReferenceChannel[] source) => ToPositions(Get(source));

        /// <summary>
        /// Convert a prototype array to a <see cref="Channel"/> array that can be set in <see cref="Listener.Channels"/>.
        /// </summary>
        public static Channel[] ToLayout(ChannelPrototype[] source) {
            Channel[] result = new Channel[source.Length];
            for (int channel = 0; channel < source.Length; ++channel) {
                result[channel] = new Channel(source[channel].X, source[channel].Y, source[channel].LFE);
            }
            return result;
        }

        /// <summary>
        /// Convert a reference array to a <see cref="Channel"/> array that can be set in <see cref="Listener.Channels"/>.
        /// </summary>
        public static Channel[] ToLayout(ReferenceChannel[] source) => ToLayout(Get(source));

        /// <summary>
        /// Convert a reference array to a <see cref="Channel"/> array that can be set in <see cref="Listener.Channels"/>,
        /// using the <see cref="AlternativePositions"/>.
        /// </summary>
        public static Channel[] ToLayoutAlternative(ReferenceChannel[] source) => ToLayout(GetAlternative(source));

        /// <summary>
        /// Check if two channel prototypes are the same.
        /// </summary>
        public readonly bool Equals(ChannelPrototype other) => X == other.X && Y == other.Y && LFE == other.LFE;

        /// <summary>
        /// Human-readable channel prototype data.
        /// </summary>
        public override readonly string ToString() {
            string basic = $"{(LFE ? Name + "(LFE)" : Name)} ({X}; {Y})";
            if (Muted) {
                return basic + " (muted)";
            }
            return basic;
        }

        /// <summary>
        /// Get the standard matrix from one of the standard matrix databases.
        /// </summary>
        static ReferenceChannel[] GetStandardMatrix(ReferenceChannel[][] database, int count) {
            int subcount = Math.Min(count, database.Length - 1);
            ReferenceChannel[] matrix = database[subcount];
            if (subcount != count) {
                Array.Resize(ref matrix, count);
                for (int i = subcount; i < count; i++) {
                    matrix[i] = ReferenceChannel.Unknown;
                }
            }
            return matrix;
        }
    }
}