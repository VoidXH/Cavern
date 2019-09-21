using UnityEngine;

using Cavern.Utilities;
using Cavern.Virtualizer;

namespace Cavern {
    /// <summary>The center of the listening space. <see cref="AudioSource3D"/>s will be rendered relative to this GameObject's position.</summary>
    [AddComponentMenu("Audio/3D Audio Listener"), RequireComponent(typeof(AudioListener))]
    public partial class AudioListener3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Active listener settings
        // ------------------------------------------------------------------
        /// <summary>Global playback volume.</summary>
        [Header("Listener")]
        [Tooltip("Global playback volume.")]
        [Range(0, 1)] public float Volume = 1;
        /// <summary>LFE channels' volume.</summary>
        [Tooltip("LFE channels' volume.")]
        [Range(0, 1)] public float LFEVolume = 1;
        /// <summary>Hearing distance.</summary>
        [Tooltip("Hearing distance.")]
        public float Range = 100;
        /// <summary>Disables any audio. Use this instead of enabling/disabling the script.</summary>
        [Tooltip("Disables any audio. Use this instead of enabling/disabling the script.")]
        public bool Paused = false;

        /// <summary>Adaption speed of the normalizer. 0 means disabled.</summary>
        [Header("Normalizer")]
        [Tooltip("Adaption speed of the normalizer. 0 means disabled.")]
        [Range(0, 1)] public float Normalizer = 1;
        /// <summary>If active, the normalizer won't increase the volume above 100%.</summary>
        [Tooltip("If active, the normalizer won't increase the volume above 100%.")]
        public bool LimiterOnly = true;

        /// <summary>Project sample rate (min. 44100). It's best to have all your audio clips in this sample rate for maximum performance.</summary>
        [Header("Advanced")]
        [Tooltip("Project sample rate (min. 44100). It's best to have all your audio clips in this sample rate for maximum performance.")]
        public int SampleRate = 48000;
        /// <summary>Update interval in audio samples (min. 16). Lower values mean better interpolation, but require more processing power.</summary>
        [Tooltip("Update interval in audio samples (min. 16). Lower values mean better interpolation, but require more processing power.")]
        public int UpdateRate = 240;
        /// <summary>Maximum audio delay, defined in this FPS value. This is the minimum frame rate required to render continuous audio.</summary>
        [Tooltip("Maximum audio delay in 1/s. This is half the minimum frame rate required to render continuous audio.")]
        public int DelayTarget = 12;
        /// <summary>Lower qualities increase performance for many sources.</summary>
        [Tooltip("Lower qualities increase performance for many sources.")]
        public QualityModes AudioQuality = QualityModes.High;
        /// <summary>Only mix LFE tagged sources to subwoofers.</summary>
        [Tooltip("Only mix LFE tagged sources to subwoofers.")]
        public bool LFESeparation = false;
        /// <summary>Disable lowpass on the LFE channel.</summary>
        [Tooltip("Disable lowpass on the LFE channel.")]
        public bool DirectLFE = false;


        // ------------------------------------------------------------------
        // Compatibility
        // ------------------------------------------------------------------
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>Alias for <see cref="Volume"/>.</summary>
        public static float volume {
            get => Current.Volume;
            set => Current.Volume = value;
        }

        /// <summary>Disables any audio. Use this instead of enabling/disabling the script.</summary>
        public static bool paused {
            get => Current.Paused;
            set => Current.Paused = value;
        }
#pragma warning restore IDE1006 // Naming Styles

        // ------------------------------------------------------------------
        // Read-only properties
        // ------------------------------------------------------------------
        /// <summary>True if the layout is symmetric.</summary>
        public static bool IsSymmetric => Listener.IsSymmetric;

        // ------------------------------------------------------------------
        // Global vars
        // ------------------------------------------------------------------
        /// <summary>The active <see cref="AudioListener3D"/> instance.</summary>
        public static AudioListener3D Current { get; private set; }

        /// <summary>Result of the last update. Size is [<see cref="Listener.Channels"/>.Length * <see cref="UpdateRate"/>].</summary>
        public static float[] Output = new float[0];

        // ------------------------------------------------------------------
        // Public functions
        // ------------------------------------------------------------------
        /// <summary>Manually generate one frame.</summary>
        public void ManualUpdate() {
            if (ChannelCount != Listener.Channels.Length || cachedSampleRate != SampleRate)
                ResetFunc();
            Output = cavernListener.Render();
        }

        /// <summary>Current speaker layout name in the format of &lt;main&gt;.&lt;LFE&gt;.&lt;height&gt;.&lt;floor&gt;, or simply "Virtualization".</summary>
        public static string GetLayoutName() => Listener.GetLayoutName();

        // ------------------------------------------------------------------
        // Internal vars
        // ------------------------------------------------------------------
        /// <summary>Actual listener handled by this interface.</summary>
        internal static Listener cavernListener = new Listener();
        /// <summary>Cached number of output channels.</summary>
        internal static int ChannelCount { get; private set; }

        // ------------------------------------------------------------------
        // Private vars
        // ------------------------------------------------------------------
        /// <summary>Cached <see cref="SampleRate"/> for change detection.</summary>
        static int cachedSampleRate = 0;

        // ------------------------------------------------------------------
        // Filter output
        // ------------------------------------------------------------------
        /// <summary>Filter buffer position, samples currently cached for output.</summary>
        static int bufferPosition = 0;
        /// <summary>Samples to play with the filter.</summary>
        static float[] filterOutput;
        /// <summary>Filter normalizer gain.</summary>
        static float filterNormalizer = 1;
        /// <summary>Cached system sample rate.</summary>
        static int systemSampleRate;

        /// <summary>Reset the listener after any change.</summary>
        void ResetFunc() {
            ChannelCount = Listener.Channels.Length;
            bufferPosition = 0;
            cachedSampleRate = SampleRate;
            filterOutput = new float[ChannelCount * SampleRate];
        }

        void Awake() {
            if (Current) {
                UnityEngine.Debug.LogError("There can be only one 3D audio listener per scene.");
                Destroy(Current);
            }
            Current = this;
            systemSampleRate = AudioSettings.GetConfiguration().sampleRate;
            ResetFunc();
        }

        void Update() {
            if (Listener.HeadphoneVirtualizer)
                VirtualizerFilter.SetLayout();
            cavernListener.Volume = Volume;
            cavernListener.LFEVolume = LFEVolume;
            cavernListener.Range = Range;
            cavernListener.Normalizer = Normalizer;
            cavernListener.LimiterOnly = LimiterOnly;
            cavernListener.SampleRate = SampleRate;
            cavernListener.UpdateRate = UpdateRate;
            cavernListener.DelayTarget = DelayTarget;
            cavernListener.AudioQuality = AudioQuality;
            cavernListener.LFESeparation = LFESeparation;
            cavernListener.DirectLFE = DirectLFE;
            cavernListener.Position = CavernUtilities.VectorMatch(transform.position);
            cavernListener.Rotation = CavernUtilities.VectorMatch(transform.eulerAngles);
        }

        /// <summary>Output Cavern's generated audio as a filter.</summary>
        /// <param name="unityBuffer">Output buffer</param>
        /// <param name="unityChannels">Output channel count</param>
        void OnAudioFilterRead(float[] unityBuffer, int unityChannels) {
            if (Paused)
                return;
            if (ChannelCount != Listener.Channels.Length || cachedSampleRate != SampleRate)
                ResetFunc();
            // Append new samples to the filter output buffer
            int needed = unityBuffer.Length / unityChannels - bufferPosition / ChannelCount,
                frames = needed * cachedSampleRate / systemSampleRate / UpdateRate + 1;
            float[] renderBuffer = cavernListener.Render(frames);
            int renderSize = renderBuffer.Length;
            if (systemSampleRate != cachedSampleRate) { // Resample output for system sample rate
                renderBuffer = Resample.Adaptive(renderBuffer, renderSize / ChannelCount * systemSampleRate / cachedSampleRate,
                    ChannelCount, AudioQuality);
                renderSize = renderBuffer.Length;
            }
            if (Listener.HeadphoneVirtualizer)
                VirtualizerFilter.Process(renderBuffer);
            Output = (float[])renderBuffer.Clone(); // Has to be cloned, as this might be a cached array
            int end = filterOutput.Length, altEnd = bufferPosition + renderSize;
            if (end > altEnd)
                end = altEnd;
            for (int bufferWrite = bufferPosition, renderPos = 0; bufferWrite < end; ++bufferWrite, ++renderPos)
                filterOutput[bufferWrite] = Output[renderPos];
            bufferPosition = end;
            WaveformUtils.Downmix(filterOutput, ChannelCount, unityBuffer, unityChannels); // Output audio
            if (Normalizer != 0) // Normalize
                WaveformUtils.Normalize(ref unityBuffer, UpdateRate / (float)SampleRate, ref filterNormalizer, true);
            // Remove used samples
            int written = unityBuffer.Length / unityChannels * ChannelCount;
            if (written > bufferPosition)
                written = bufferPosition;
            for (int bufferPos = written; bufferPos < bufferPosition; ++bufferPos)
                filterOutput[bufferPos - written] = filterOutput[bufferPos];
            bufferPosition -= written;
        }
    }
}