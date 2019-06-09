using UnityEngine;

namespace Cavern {
    /// <summary>Wrapper for Cavern's audio content format to match Unity's <see cref="AudioClip"/> signature.</summary>
    public class AudioClip3D : Clip {
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>The length of the clip in seconds.</summary>
        public float length { get; private set; }
        /// <summary>Sampling rate of the clip.</summary>
        public float frequency { get; private set; }
        /// <summary>Channel count for the clip.</summary>
        public int channels { get; private set; }
        /// <summary>The length of the clip in samples, for a single channel.</summary>
        public int samples { get; private set; }
        /// <summary>Returns true if this clip is ambisonic. Cavern is always ambisonic.</summary>
        public bool ambisonic => true;
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>Audio content.</summary>
        /// <param name="data">Audio data, with the size of [channels][samples for given channel]</param>
        /// <param name="sampleRate">Sample rate</param>
        public AudioClip3D(float[][] data, int sampleRate) : base(data, sampleRate) => Fill();

        /// <summary>Audio content.</summary>
        /// <param name="data">Audio data, with interlaced channels</param>
        /// <param name="channels">Channel count</param>
        /// <param name="sampleRate">Sample rate</param>
        public AudioClip3D(float[] data, int channels, int sampleRate) : base(data, channels, sampleRate) => Fill();

        void Fill() {
            length = Length;
            frequency = SampleRate;
            channels = Channels;
            samples = Samples;
        }
    }
}