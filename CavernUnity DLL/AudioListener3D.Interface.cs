using System;
using UnityEngine;

using Cavern.Remapping;
using Cavern.Listeners;

namespace Cavern {
    partial class AudioListener3D {
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

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// Alias for <see cref="Volume"/> for drop-in Unity compatibility.
        /// </summary>
        public static float volume {
            get => Current.Volume;
            set => Current.Volume = value;
        }

        /// <summary>
        /// Disables any audio. Use this instead of enabling/disabling the script. Alias for drop-in Unity compatibility.
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

        /// <summary>
        /// True if the layout is symmetric.
        /// </summary>
        public static bool IsSymmetric => Listener.IsSymmetric;

        /// <summary>
        /// The active <see cref="AudioListener3D"/> instance.
        /// </summary>
        public static AudioListener3D Current { get; private set; }

        /// <summary>
        /// Result of the last update. Size is [<see cref="Listener.Channels"/>.Length * <see cref="UpdateRate"/>].
        /// </summary>
        public static float[] Output { get; private set; } = new float[0];

        /// <summary>
        /// Actual listener handled by this interface. Derived 3D listeners (e.g. <see cref="ConvolvedListener3D"/>)
        /// replace this with their own <see cref="Listener"/> implementation.
        /// </summary>
        /// <remarks>Normalization and limiting happens in this object's <see cref="normalizer"/>.</remarks>
        internal static Listener CavernListener { get; set; } = new Listener {
            LimiterOnly = true
        };

        /// <summary>
        /// Cached system sample rate.
        /// </summary>
        internal static int SystemSampleRate { get; private set; }

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
            lock (CavernListener) {
                action.Invoke();
            }
        }

        /// <summary>
        /// Manually generate one frame.
        /// </summary>
        public void ManualUpdate() {
            lock (CavernListener) {
                Output = CavernListener.Render();
            }
        }
    }
}
