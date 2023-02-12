using UnityEngine;

namespace Cavern {
    /// <summary>
    /// Wrapper for Cavern's audio content format to match Unity's <see cref="AudioClip"/> signature.
    /// </summary>
    public sealed class AudioClip3D : Clip {
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// The length of the clip in seconds.
        /// </summary>
        public float length { get; private set; }

        /// <summary>
        /// Sampling rate of the clip.
        /// </summary>
        public int frequency { get; private set; }

        /// <summary>
        /// Channel count for the clip.
        /// </summary>
        public int channels { get; private set; }

        /// <summary>
        /// The length of the clip in samples, for a single channel.
        /// </summary>
        public int samples { get; private set; }

        /// <summary>
        /// Returns true if this clip is ambisonic. Cavern is always ambisonic.
        /// </summary>
        public bool ambisonic => true;

#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Audio content.
        /// </summary>
        /// <param name="data">Audio data, with the size of [channels][samples for given channel]</param>
        /// <param name="sampleRate">Sample rate</param>
        public AudioClip3D(MultichannelWaveform data, int sampleRate) : base(data, sampleRate) => Fill();

        /// <summary>
        /// Audio content.
        /// </summary>
        /// <param name="data">Audio data, with interlaced channels</param>
        /// <param name="channels">Channel count</param>
        /// <param name="sampleRate">Sample rate</param>
        public AudioClip3D(float[] data, int channels, int sampleRate) : base(data, channels, sampleRate) => Fill();

        /// <summary>
        /// Create a Cavern clip from a Unity clip.
        /// </summary>
        public static AudioClip3D FromUnityClip(AudioClip source) {
            float[] data = new float[source.samples * source.channels];
            source.GetData(data, 0);
            return new AudioClip3D(data, source.channels, source.frequency);
        }

        /// <summary>
        /// Copy settings from the parent.
        /// </summary>
        void Fill() {
            length = Length;
            frequency = SampleRate;
            channels = Channels;
            samples = Samples;
        }
    }
}