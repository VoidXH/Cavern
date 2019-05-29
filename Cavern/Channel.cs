using System;

namespace Cavern {
    /// <summary>Spatially positioned audio output channel.</summary>
    public class Channel {
        /// <summary>True for channels carrying only Low Frequency Effects.</summary>
        public bool LFE;

        /// <summary>Rotation around the X axis in degrees: height.</summary>
        public float X { get; protected set; }

        /// <summary>Rotation around the Y axis in degrees.</summary>
        public float Y { get; protected set; }

        /// <summary>This channel is part of the screen channels, and should be behind the screen in a theatre.</summary>
        public bool IsScreenChannel => !LFE && Math.Abs(X) < 25 && Math.Abs(Y) <= 45;

        /// <summary>Constructor for a channel with given rotation values.</summary>
        /// <param name="x">Rotation around the X axis in degrees: height</param>
        /// <param name="y">Rotation around the Y axis in degrees</param>
        public Channel(float x, float y) {
            Move(x, y);
            LFE = false;
        }

        /// <summary>Constructor for a channel with given rotation values and LFE status.</summary>
        /// <param name="x">Rotation around the X axis in degrees: height</param>
        /// <param name="y">Rotation around the Y axis in degrees</param>
        /// <param name="LFE">True for channels carrying only Low Frequency Effects</param>
        public Channel(float x, float y, bool LFE) {
            Move(x, y);
            this.LFE = LFE;
        }

        /// <summary>Move this channel to a new position.</summary>
        /// <param name="x">Rotation around the X axis in degrees: height</param>
        /// <param name="y">Rotation around the Y axis in degrees</param>
        public virtual void Move(float x, float y) {
            X = x;
            Y = y;
        }

        /// <summary>Rotate this channel.</summary>
        /// <param name="x">Rotation around the X axis in degrees: height</param>
        /// <param name="y">Rotation around the Y axis in degrees</param>
        public void Rotate(float x, float y) => Move(X + x, Y + y);
    }
}