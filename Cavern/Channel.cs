using System;
using System.Numerics;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern {
    /// <summary>
    /// Spatially positioned audio output channel.
    /// </summary>
    public sealed class Channel : IEquatable<Channel> {
        /// <summary>
        /// Rotation around the vertical axis in degrees: elevation.
        /// </summary>
        public float X { get; private set; }

        /// <summary>
        /// Rotation around the horizontal axis in degrees, clockwise: azimuth.
        /// </summary>
        public float Y { get; private set; }

        /// <summary>
        /// True for channels carrying only Low Frequency Effects.
        /// </summary>
        public bool LFE {
            get => lfe;
            set {
                lfe = value;
                Listener.ReplaceChannels(Listener.Channels);
            }
        }
        bool lfe;

        /// <summary>
        /// This channel is part of the screen channels, and should be behind the screen in a theatre, or on the front wall in a home.
        /// </summary>
        public bool IsScreenChannel => !lfe && Math.Abs(X) < 25 && Math.Abs(Y) <= 45;

        /// <summary>
        /// Position on a sphere with the radius of 1.
        /// </summary>
        public Vector3 SphericalPos { get; private set; }

        /// <summary>
        /// Position on a cube with a side length of 2.
        /// </summary>
        public Vector3 CubicalPos { get; private set; }

        /// <summary>
        /// Position in space. <see cref="SphericalPos"/> in Studio environments, <see cref="CubicalPos"/> otherwise.
        /// </summary>
        public Vector3 SpatialPos { get; private set; }

        /// <summary>
        /// The distance from the listener, relative to the center channel. Magnitude of <see cref="SpatialPos"/>.
        /// </summary>
        public float Distance { get; private set; }

        /// <summary>
        /// Constructs a channel with given rotation values.
        /// </summary>
        /// <param name="x">Rotation around the vertical axis in degrees: elevation</param>
        /// <param name="y">Rotation around the horizontal axis in degrees: azimuth</param>
        public Channel(float x, float y) => SetPosition(x, y);

        /// <summary>
        /// Constructs a channel with given rotation values and LFE status.
        /// </summary>
        /// <param name="x">Rotation around the vertical axis in degrees: elevation</param>
        /// <param name="y">Rotation around the horizontal axis in degrees: azimuth</param>
        /// <param name="LFE">True for channels carrying only Low Frequency Effects</param>
        public Channel(float x, float y, bool LFE) {
            lfe = LFE;
            SetPosition(x, y);
        }

        /// <summary>
        /// Constructs a channel that is placed in the same direction as the given position.
        /// </summary>
        /// <remarks>The <paramref name="location"/> is not scaled with <see cref="Listener.EnvironmentSize"/>.</remarks>
        /// <param name="location">Spatial position of the channel</param>
        /// <param name="LFE">True for channels carrying only Low Frequency Effects</param>
        public Channel(Vector3 location, bool LFE) {
            lfe = LFE;
            if (location.Y != 0) {
                if (location.X == 0) {
                    SetPosition(-MathF.Abs(MathF.Atan(location.Z / location.Y)) * VectorExtensions.Rad2Deg, 0);
                    return;
                } else if (location.Z == 0) {
                    SetPosition(-MathF.Abs(MathF.Atan(location.X / location.Y)) * VectorExtensions.Rad2Deg, location.X < 0 ? -90 : 90);
                    return;
                }
            }
            float y = MathF.Atan(location.X / location.Z) * VectorExtensions.Rad2Deg;
            if (location.Z < 0) {
                y += 180;
            }
            SetPosition(location.Y == 0 ? 0 : (-MathF.Abs(MathF.Atan(location.X / location.Y)) * VectorExtensions.Rad2Deg), y);
        }

        /// <summary>
        /// Get if a channel is LFE in the current layout.
        /// </summary>
        public static bool IsLFE(int channel) => Listener.Channels[channel].lfe;

        /// <summary>
        /// Get if a channel is LFE for a given channel count.
        /// </summary>
        public static bool IsLFE(int channel, int channels) => channels == Listener.Channels.Length ?
            Listener.Channels[channel].LFE :
            channels >= 6 && channel == 3;

        /// <summary>
        /// Get if a channel is on the front wall and not a subwoofer.
        /// </summary>
        public static bool IsAScreenChannel(int channel, int channels) => channels == Listener.Channels.Length ?
            Listener.Channels[channel].IsScreenChannel :
            channels > 4 ? channel < 3 : channel < 2;

        /// <summary>
        /// Move this channel to a new position.
        /// </summary>
        /// <param name="x">Rotation around the vertical axis in degrees: elevation</param>
        /// <param name="y">Rotation around the horizontal axis in degrees: azimuth</param>
        public void Move(float x, float y) {
            SetPosition(x, y);
            Listener.ReplaceChannels(Listener.Channels);
        }

        /// <summary>
        /// Rotate this channel.
        /// </summary>
        /// <param name="x">Rotation around the vertical axis in degrees: height</param>
        /// <param name="y">Rotation around the horizontal axis in degrees</param>
        public void Rotate(float x, float y) => Move(X + x, Y + y);

        /// <summary>
        /// Check if two channels are the same.
        /// </summary>
        public bool Equals(Channel other) => X == other.X && Y == other.Y && lfe == other.lfe;

        /// <summary>
        /// Check if the other object is also a <see cref="Channel"/> and equal to this.
        /// </summary>
        public override bool Equals(object obj) => obj is Channel other && Equals(other);

        /// <summary>
        /// Get a hash value representing this channel.
        /// </summary>
        public override int GetHashCode() => (Y % 360 * 720 + X + (LFE ? 1048576 : 0)).GetHashCode();

        /// <summary>
        /// Display channel position when converted to string.
        /// </summary>
        public override string ToString() => $"X: {X}, Y: {Y}, {SpatialPos}";

        /// <summary>
        /// Recalculates properties.
        /// </summary>
        internal void Recalculate() {
            float xRad = X * VectorExtensions.Deg2Rad,
                yRad = Y * VectorExtensions.Deg2Rad,
                sinX = MathF.Sin(xRad),
                cosX = MathF.Cos(xRad),
                sinY = MathF.Sin(yRad),
                cosY = MathF.Cos(yRad);
            SphericalPos = new Vector3(sinY * cosX, -sinX, cosY * cosX);
            if (Math.Abs(sinY) > Math.Abs(cosY)) {
                sinY = Math.Sign(sinY) * VectorExtensions.Sqrt2p2;
            } else {
                cosY = Math.Sign(cosY) * VectorExtensions.Sqrt2p2;
            }
            sinY /= VectorExtensions.Sqrt2p2;
            cosY /= VectorExtensions.Sqrt2p2;
            if (Math.Abs(sinX) >= VectorExtensions.Sqrt2p2) {
                sinX = Math.Sign(sinX) * VectorExtensions.Sqrt2p2;
                cosX /= VectorExtensions.Sqrt2p2;
                sinY *= cosX;
                cosY *= cosX;
            }
            sinX /= VectorExtensions.Sqrt2p2;
            CubicalPos = new Vector3(sinY, -sinX, cosY);
            SpatialPos = Listener.IsSpherical ? SphericalPos : CubicalPos;
            Distance = SpatialPos.Length();
        }

        /// <summary>
        /// Set the position of the channel and do all neccessary processing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetPosition(float x, float y) {
            X = x;
            Y = y;
            Recalculate();
        }
    }
}
