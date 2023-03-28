using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Filters;
using Cavern.Remapping;
using Cavern.Utilities;
using Cavern.Virtualizer;

namespace Cavern {
    /// <summary>
    /// The center of the listening space. <see cref="AudioSource3D"/>s will be rendered relative to this GameObject's position.
    /// </summary>
    [AddComponentMenu("Audio/3D Audio Listener"), RequireComponent(typeof(AudioListener))]
    public partial class AudioListener3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Active listener settings
        // ------------------------------------------------------------------
        /// <summary>
        /// Global playback volume.
        /// </summary>
        [Header("Listener")]
        [Tooltip("Global playback volume.")]
        [Range(0, 1)] public float Volume = 1;

        /// <summary>
        /// LFE channels' volume.
        /// </summary>
        [Tooltip("LFE channels' volume.")]
        [Range(0, 1)] public float LFEVolume = 1;

        /// <summary>
        /// Hearing distance.
        /// </summary>
        [Tooltip("Hearing distance.")]
        public float Range = 100;

        /// <summary>
        /// Disables any audio. Use this instead of enabling/disabling the script.
        /// </summary>
        [Tooltip("Disables any audio. Use this instead of enabling/disabling the script.")]
        public bool Paused;

        /// <summary>
        /// Adaption speed of the normalizer. 0 means disabled.
        /// </summary>
        [Header("Normalizer")]
        [Tooltip("Adaption speed of the normalizer. 0 means disabled.")]
        [Range(0, 1)] public float Normalizer = 1;

        /// <summary>
        /// If active, the normalizer won't increase the volume above 100%.
        /// </summary>
        [Tooltip("If active, the normalizer won't increase the volume above 100%.")]
        public bool LimiterOnly = true;

        /// <summary>
        /// Project sample rate (min. 44100). It's best to have all your audio clips in this sample rate for maximum performance.
        /// </summary>
        [Header("Advanced")]
        [Tooltip("Project sample rate (min. 44100). " +
            "It's best to have all your audio clips in this sample rate for maximum performance.")]
        public int SampleRate = 48000;

        /// <summary>
        /// Update interval in audio samples (min. 16). Lower values mean better interpolation, but require more processing power.
        /// </summary>
        [Tooltip("Update interval in audio samples (min. 16). " +
            "Lower values mean better interpolation, but require more processing power.")]
        public int UpdateRate = 240;

        /// <summary>
        /// Maximum audio delay, defined in this FPS value. This is the minimum frame rate required to render continuous audio.
        /// </summary>
        [Tooltip("Maximum audio delay in 1/s. This is half the minimum frame rate required to render continuous audio.")]
        public int DelayTarget = 12;

        /// <summary>
        /// Lower qualities increase performance for many sources.
        /// </summary>
        [Tooltip("Lower qualities increase performance for many sources.")]
        public QualityModes AudioQuality = QualityModes.High;

        /// <summary>
        /// Only mix LFE tagged sources to subwoofers.
        /// </summary>
        [Tooltip("Only mix LFE tagged sources to subwoofers.")]
        public bool LFESeparation;

        /// <summary>
        /// Disable lowpass on the LFE channel.
        /// </summary>
        [Tooltip("Disable lowpass on the LFE channel.")]
        public bool DirectLFE;

        /// <summary>
        /// Save performance by not remapping Unity's output to the user layout and only rendering Cavern sources.
        /// </summary>
        /// <remarks>You should still use Unity's render engine
        /// for non-decompressed clips like background music to save memory.</remarks>
        [Tooltip("Save performance by not remapping Unity's output to the user layout and only rendering Cavern sources.")]
        public bool DisableUnityAudio;

        // ------------------------------------------------------------------
        // Compatibility
        // ------------------------------------------------------------------
#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// Alias for <see cref="Volume"/>.
        /// </summary>
        public static float volume {
            get => Current.Volume;
            set => Current.Volume = value;
        }

        /// <summary>
        /// Disables any audio. Use this instead of enabling/disabling the script.
        /// </summary>
        public static bool paused {
            get => Current.Paused;
            set {
                if (Current.Paused = value) {
                    bufferPosition = 0;
                }
            }
        }
#pragma warning restore IDE1006 // Naming Styles

        // ------------------------------------------------------------------
        // Read-only properties
        // ------------------------------------------------------------------
        /// <summary>
        /// True if the layout is symmetric.
        /// </summary>
        public static bool IsSymmetric => Listener.IsSymmetric;

        // ------------------------------------------------------------------
        // Global vars
        // ------------------------------------------------------------------
        /// <summary>
        /// The active <see cref="AudioListener3D"/> instance.
        /// </summary>
        public static AudioListener3D Current { get; private set; }

        /// <summary>
        /// Result of the last update. Size is [<see cref="Listener.Channels"/>.Length * <see cref="UpdateRate"/>].
        /// </summary>
        public static float[] Output { get; private set; } = new float[0];

        // ------------------------------------------------------------------
        // Internal vars
        // ------------------------------------------------------------------
        /// <summary>
        /// Actual listener handled by this interface.
        /// </summary>
        /// <remarks>Normalization and limiting happens in this object's <see cref="normalizer"/>.</remarks>
        internal static readonly Listener cavernListener = new Listener {
            LimiterOnly = true
        };

        /// <summary>
        /// Cached system sample rate.
        /// </summary>
        internal static int SystemSampleRate { get; private set; }

        // ------------------------------------------------------------------
        // Private vars
        // ------------------------------------------------------------------
        /// <summary>
        /// Cached <see cref="SampleRate"/> for change detection.
        /// </summary>
        static int cachedSampleRate = -1;

        /// <summary>
        /// Renders Unity's output to the layout set in Cavern.
        /// </summary>
        static Remapper remapper;

        /// <summary>
        /// Used to prevent sample generation before the first frame.
        /// </summary>
        bool startSkip = true;

        // ------------------------------------------------------------------
        // Public functions
        // ------------------------------------------------------------------
        /// <summary>
        /// Current speaker layout name in the format of &lt;main&gt;.&lt;LFE&gt;.&lt;height&gt;.&lt;floor&gt;,
        /// or simply "Virtualization".
        /// </summary>
        public static string GetLayoutName() => Listener.GetLayoutName();

        /// <summary>
        /// Invoke an action when rendering is not in progress, making non-thread-safe Cavern calls (like seeking)
        /// safe from another thread.
        /// </summary>
        public static void PerformSafely(Action action) {
            lock (cavernListener) {
                action.Invoke();
            }
        }

        /// <summary>
        /// Manually generate one frame.
        /// </summary>
        public void ManualUpdate() {
            lock (cavernListener) {
                Output = cavernListener.Render();
            }
        }

        // ------------------------------------------------------------------
        // Filter output
        // ------------------------------------------------------------------
        /// <summary>
        /// Filter buffer position, samples currently cached for output.
        /// </summary>
        static int bufferPosition;

        /// <summary>
        /// Samples to play with the filter.
        /// </summary>
        static float[] filterOutput;

        /// <summary>
        /// As the output does not match the <see cref="Listener"/>'s because of Unity's optional sound output and
        /// to prepare for a channel count mismatch, normalization happens here.
        /// </summary>
        static readonly Normalizer normalizer = new Normalizer(true);

        /// <summary>
        /// Active virtualization filter.
        /// </summary>
        VirtualizerFilter virtualizer;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Awake() {
            if (Current) {
                UnityEngine.Debug.LogError("There can be only one 3D audio listener per scene.");
                Destroy(Current);
            }
            Current = this;
            SystemSampleRate = AudioSettings.GetConfiguration().sampleRate;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            cavernListener.Volume = Volume;
            cavernListener.LFEVolume = LFEVolume;
            cavernListener.Range = Range;
            cavernListener.SampleRate = SampleRate;
            cavernListener.UpdateRate = UpdateRate;
            cavernListener.DelayTarget = DelayTarget;
            cavernListener.AudioQuality = AudioQuality;
            cavernListener.LFESeparation = LFESeparation;
            cavernListener.DirectLFE = DirectLFE;
            normalizer.decayFactor = Normalizer * UpdateRate / SampleRate;
            normalizer.limiterOnly = LimiterOnly;
            cavernListener.LimiterOnly = LimiterOnly;
            cavernListener.Position = VectorUtils.VectorMatch(transform.position);
            cavernListener.Rotation = VectorUtils.VectorMatch(transform.eulerAngles);
            startSkip = false;
        }

        /// <summary>
        /// Output Cavern's generated audio as a filter.
        /// </summary>
        /// <param name="unityBuffer">Output buffer</param>
        /// <param name="unityChannels">Output channel count</param>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnAudioFilterRead(float[] unityBuffer, int unityChannels) {
            if (startSkip || Paused || SystemSampleRate == 0) {
                return;
            }
            if (cachedSampleRate != SampleRate) {
                cachedSampleRate = SampleRate;
                bufferPosition = 0;
                filterOutput = new float[unityChannels * SampleRate];
            }

            // Append new samples to the filter output buffer
            int channels = Listener.Channels.Length;
            lock (cavernListener) {
                Output = cavernListener.Render((unityBuffer.Length - bufferPosition) / unityChannels *
                    cachedSampleRate / SystemSampleRate / UpdateRate + 1);
            }
            float[] renderBuffer = Output;

            // Virtualizer pipeline: resample -> filter -> downmix
            if (Listener.HeadphoneVirtualizer) {
                if (SystemSampleRate != cachedSampleRate) { // Resample output for system sample rate
                    renderBuffer = Resample.Adaptive(renderBuffer,
                        renderBuffer.Length / channels * SystemSampleRate / cachedSampleRate, channels, AudioQuality);
                    // If the listener didn't perform virtualization for sample rate mismatch, do it here
                    virtualizer ??= new VirtualizerFilter();
                    virtualizer.Process(renderBuffer, SystemSampleRate);
                }
                int end = filterOutput.Length,
                    altEnd = bufferPosition + renderBuffer.Length / channels * unityChannels;
                if (end > altEnd) {
                    end = altEnd;
                }
                for (int renderPos = 0; bufferPosition < end; bufferPosition += unityChannels, renderPos += channels) {
                    filterOutput[bufferPosition] = renderBuffer[renderPos];
                    filterOutput[bufferPosition + 1] = renderBuffer[renderPos + 1];
                }
            }

            // Default pipeline: downmix -> resample (faster for many virtual channels)
            else {
                float[] downmix = renderBuffer;
                if (channels != unityChannels) {
                    downmix = new float[renderBuffer.Length / channels * unityChannels];
                    if (renderBuffer.Length * unityChannels == downmix.Length * channels) {
                        WaveformUtils.Downmix(renderBuffer, downmix, unityChannels);
                    } else {
                        downmix.Clear();
                    }
                    if (SystemSampleRate != cachedSampleRate) { // Resample output for system sample rate
                        downmix = Resample.Adaptive(downmix,
                            downmix.Length / unityChannels * SystemSampleRate / cachedSampleRate, unityChannels, AudioQuality);
                    }
                }
                int end = filterOutput.Length;
                if (end > bufferPosition + downmix.Length) {
                    end = bufferPosition + downmix.Length;
                }
                Array.Copy(downmix, 0, filterOutput, bufferPosition, end - bufferPosition);
                bufferPosition = end;
            }

            // If Unity has audio output and it's rendering is enabled, mix it for the user's layout
            if (!DisableUnityAudio) {
                if (remapper == null || remapper.channels != unityChannels) {
                    remapper?.Dispose();
                    remapper = new Remapper(unityChannels, unityBuffer.Length / unityChannels);
                }
                float[] remapped = remapper.Update(unityBuffer, unityChannels);
                unityBuffer.Clear();
                Array.Copy(filterOutput, unityBuffer, unityBuffer.Length);
                WaveformUtils.Downmix(remapped, unityBuffer, unityChannels); // Output remapped Unity audio
            } else {
                Array.Copy(filterOutput, unityBuffer, unityBuffer.Length);
            }

            // Apply normalizer
            if (Normalizer != 0) {
                normalizer.Process(unityBuffer);
            }

            // Generate output from buffer
            int written = unityBuffer.Length;
            if (written > bufferPosition) {
                written = bufferPosition;
            }
            for (int bufferPos = written; bufferPos < bufferPosition; ++bufferPos) {
                filterOutput[bufferPos - written] = filterOutput[bufferPos];
            }
            bufferPosition -= written;
        }
    }
}