using System;
using UnityEngine;

namespace Cavern {
    /// <summary>Spatially positioned audio output channel.</summary>
    public class Channel {
        float X, Y; // Spherical position in degrees

        /// <summary>True for channels carrying only Low Frequency Effects.</summary>
        public bool LFE;

        /// <summary>Rotation around the X axis: height.</summary>
        public float x {
            get => X;
            set { X = value; Recalculate(); }
        }

        /// <summary>Rotation around the Y axis.</summary>
        public float y {
            get => Y;
            set { Y = value; Recalculate(); }
        }

        /// <summary>Position on a sphere with the radius of 1.</summary>
        public Vector3 SphericalPos { get; private set; }
        /// <summary>Position on a cube with a side length of 2.</summary>
        public Vector3 CubicalPos { get; private set; }
        /// <summary>Position in space. <see cref="SphericalPos"/> in Studio environments, <see cref="CubicalPos"/> otherwise.</summary>
        public Vector3 SpatialPos { get; private set; }
        /// <summary>Rotation direction of the channel.</summary>
        public Quaternion Rotation { get; private set; }
        /// <summary>The channel's forward direction.</summary>
        public Vector3 Direction { get; private set; }

        /// <summary>An identical channel.</summary>
        internal Channel Copy { get => new Channel(X, Y, LFE); }

        /// <summary>Constructor for a channel with given rotation values.</summary>
        /// <param name="X">Rotation around the X axis: height</param>
        /// <param name="Y">Rotation around the Y axis</param>
        public Channel(float X, float Y) {
            this.X = X;
            this.Y = Y;
            LFE = false;
            Recalculate();
        }

        /// <summary>Constructor for a channel with given rotation values and LFE status.</summary>
        /// <param name="X">Rotation around the X axis: height</param>
        /// <param name="Y">Rotation around the Y axis</param>
        /// <param name="LFE">True for channels carrying only Low Frequency Effects</param>
        public Channel(float X, float Y, bool LFE) {
            this.X = X;
            this.Y = Y;
            this.LFE = LFE;
            Recalculate();
        }

        /// <summary>Returns if this channel is part of the screen channels.</summary>
        public bool IsScreenChannel() => !LFE && Math.Abs(X) < 25 && Math.Abs(Y) <= 45;

        /// <summary>Recalculates properties and symmetry when a channel's position is changed.</summary>
        internal void Recalculate() {
            // Helper calculation
            Rotation = Quaternion.Euler(x, y, 0);
            Direction = Rotation * Vector3.forward;
            float XRad = X * Mathf.Deg2Rad, YRad = Y * Mathf.Deg2Rad, SinX = (float)Math.Sin(XRad), CosX = (float)Math.Cos(XRad),
                SinY = (float)Math.Sin(YRad), CosY = (float)Math.Cos(YRad);
            SphericalPos = new Vector3(SinY * CosX, -SinX, CosY * CosX);
            if (Math.Abs(SinY) > Math.Abs(CosY))
                SinY = SinY > 0 ? CavernUtilities.Sqrt2p2 : CavernUtilities.Sqrt2pm2;
            else
                CosY = CosY > 0 ? CavernUtilities.Sqrt2p2 : CavernUtilities.Sqrt2pm2;
            SinY /= CavernUtilities.Sqrt2p2;
            CosY /= CavernUtilities.Sqrt2p2;
            if (Math.Abs(SinX) >= CavernUtilities.Sqrt2p2) {
                SinX = SinX > 0 ? CavernUtilities.Sqrt2p2 : CavernUtilities.Sqrt2pm2;
                CosX /= CavernUtilities.Sqrt2p2;
                SinY *= CosX;
                CosY *= CosX;
            }
            SinX /= CavernUtilities.Sqrt2p2;
            CubicalPos = new Vector3(SinY, -SinX, CosY);
            if (AudioListener3D._EnvironmentType != Environments.Studio)
                SpatialPos = CubicalPos;
            else
                SpatialPos = SphericalPos;
            // Symmetry check
            if (AudioListener3D.Channels != null) {
                AudioSource3D.Symmetric = true;
                int ChannelCount = AudioListener3D.Channels.Length;
                if (ChannelCount % 2 == 1) { // If there is an unpaired channel, it must be on the center circle
                    --ChannelCount;
                    AudioSource3D.Symmetric = AudioListener3D.Channels[ChannelCount].Y % 180 == 0;
                }
                for (int i = 0; i < ChannelCount; i += 2) {
                    int Next = i + 1;
                    if (Next != ChannelCount && AudioListener3D.Channels[i] != null && AudioListener3D.Channels[Next] != null)
                        AudioSource3D.Symmetric &=
                            AudioListener3D.Channels[i].LFE ? AudioListener3D.Channels[Next].Y % 180 == 0 || AudioListener3D.Channels[Next].LFE :
                            AudioListener3D.Channels[Next].LFE ? AudioListener3D.Channels[i].Y % 180 == 0 || AudioListener3D.Channels[i].LFE :
                            (AudioListener3D.Channels[i].X == AudioListener3D.Channels[Next].X ? AudioListener3D.Channels[i].Y % 180 == -AudioListener3D.Channels[Next].Y % 180 :
                            AudioListener3D.Channels[i].Y % 180 == 0 && AudioListener3D.Channels[Next].Y % 180 == 0);
                }
            }
        }
    }
}