using UnityEngine;

namespace Cavern {
    /// <summary>The center of the listening space. <see cref="AudioSource3D"/>s will be rendered relative to this GameObject's position.</summary>
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
        /// <summary>Manually ask for one update period.</summary>
        [Tooltip("Manually ask for one update period.")]
        public bool Manual = false;
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
        public float volume {
            get => Volume;
            set => Volume = value;
        }
#pragma warning restore IDE1006 // Naming Styles

        // ------------------------------------------------------------------
        // Global settings
        // ------------------------------------------------------------------
        /// <summary>Virtual surround effect for headphones. This will replace the active <see cref="Listener.Channels"/>.</summary>
        /// <remarks>Set by the user and applied when a <see cref="Listener"/> is created. Don't override without user interaction.</remarks>
        public static bool HeadphoneVirtualizer {
            get => Listener.HeadphoneVirtualizer;
            set => Listener.HeadphoneVirtualizer = value;
        }

        // ------------------------------------------------------------------
        // Read-only properties
        // ------------------------------------------------------------------
        /// <summary>True if the layout is symmetric.</summary>
        public static bool IsSymmetric => Listener.IsSymmetric;

        // ------------------------------------------------------------------
        // Global vars
        // ------------------------------------------------------------------
        /// <summary>The active <see cref="AudioListener3D"/> instance.</summary>
        public static AudioListener3D Current;

        /// <summary>Result of the last update. Size is [<see cref="Listener.Channels"/>.Length * <see cref="UpdateRate"/>].</summary>
        public static float[] Output = new float[0];

        // ------------------------------------------------------------------
        // Public static functions
        // ------------------------------------------------------------------
        /// <summary>Current speaker layout name in the format of &lt;main&gt;.&lt;LFE&gt;.&lt;height&gt;.&lt;floor&gt;, or simply "Virtualization".</summary>
        public static string GetLayoutName() => Listener.GetLayoutName();

        // ------------------------------------------------------------------
        // Public functions
        // ------------------------------------------------------------------
        /// <summary>Runs the frame update function.</summary>
        public void ForcedUpdate() => Update();
    }
}