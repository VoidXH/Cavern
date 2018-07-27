using System.Text;
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
        public int SampleRate = 44100;
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

        /// <summary>Virtual surround effect for headphones. Will override any echo settings.</summary>
        [Header("Experimental")]
        [Tooltip("Virtual surround effect for headphones. Will override any echo settings.")]
        public bool HeadphoneVirtualizer = false;
        /// <summary>Tries to fix standing waves in real-time, but may cause artifacts.</summary>
        [Tooltip("Tries to fix standing waves in real-time, but may cause artifacts.")]
        public bool StandingWaveFix = false;

        // ------------------------------------------------------------------
        // Compatibility
        // ------------------------------------------------------------------
        /// <summary>Alias for <see cref="Volume"/>.</summary>
        public float volume {
            get { return Volume; }
            set { Volume = value; }
        }

        // ------------------------------------------------------------------
        // Global settings
        // ------------------------------------------------------------------
        /// <summary>Output channel data. Set by the user and applied when an <see cref="AudioListener3D"/> is created. The default setup is the standard 5.1 layout.</summary>
        public static Channel[] Channels = { new Channel(0, -45), new Channel(0, 45),
                                             new Channel(0, 0), new Channel(15, 15, true),
                                             new Channel(0, -110), new Channel(0, 110) };

        /// <summary>3D environment type. Set by the user and applied when an <see cref="AudioListener3D"/> is created.</summary>
        public static Environments EnvironmentType {
            get { return _EnvironmentType; }
            set {
                _EnvironmentType = value;
                for (int Channel = 0; Channel < ChannelCount; ++Channel)
                    Channels[Channel].Recalculate();
            }
        }
        internal static Environments _EnvironmentType = Environments.Home;

        /// <summary>
        /// The single most important variable defining sound space in
        /// symmetric mode, the environment scaling. Originally set by the
        /// user and applied when an <see cref="AudioListener3D"/> is
        /// created, however, overriding it in specific applications can make
        /// a huge difference. Objects inside a box this size are positioned
        /// inside the room, and defines the range of balance between
        /// left/right, front/rear, and top/bottom speakers. On asymmetric
        /// systems, this setting only affects channel volumes if environment
        /// compensation is enabled. The user's settings should be respected,
        /// thus this vector should be scaled, not completely overridden.
        /// </summary>
        public static Vector3 EnvironmentSize = new Vector3(10, 7, 10);

        /// <summary>
        /// Automatically set channel volumes based on
        /// <see cref="EnvironmentSize"/> and <see cref="EnvironmentType"/>.
        /// Not recommended for calibrated systems. Set by the user and
        /// applied when an AudioListener3D is created.
        /// </summary>
        public static bool EnvironmentCompensation = false;

        /// <summary>How many sources can be played at the same time.</summary>
        public static int MaximumSources {
            get { return SourceLimit; }
            set { SourceLimit = value; SourceDistances = new float[value]; }
        }

        // ------------------------------------------------------------------
        // Read-only properties
        // ------------------------------------------------------------------
        /// <summary>True if the layout is symmetric.</summary>
        public static bool IsSymmetric {
            get { return AudioSource3D.Symmetric; }
        }

        /// <summary>Samples currently cached for output.</summary>
        public static int FilterBufferPosition {
            get { return BufferPosition; }
        }

        // ------------------------------------------------------------------
        // Global vars
        // ------------------------------------------------------------------
        /// <summary>The active <see cref="AudioListener3D"/> instance.</summary>
        public static AudioListener3D Current;

        /// <summary>Result of the last update. Size is [<see cref="Channels"/>.Length * <see cref="UpdateRate"/>].</summary>
        public static float[] Output = new float[0];

        // ------------------------------------------------------------------
        // Delegates
        // ------------------------------------------------------------------
        /// <summary>Handle new outputted samples.</summary>
        public delegate void OutputAvailable();

        /// <summary>Called when new samples were generated.</summary>
        public event OutputAvailable OnOutputAvailable;

        // ------------------------------------------------------------------
        // Public static functions
        // ------------------------------------------------------------------
        /// <summary>Current speaker layout name in the format of &lt;main&gt;.&lt;LFE&gt;.&lt;height&gt;.&lt;floor&gt;, or simply "Virtualization".</summary>
        public static string GetLayoutName() {
            if (Current.HeadphoneVirtualizer)
                return "Virtualization";
            else {
                int Regular = 0, LFE = 0, Ceiling = 0, Floor = 0;
                for (int i = 0, ChannelCount = Channels.Length; i < ChannelCount; ++i)
                    if (Channels[i].LFE) ++LFE;
                    else if (Channels[i].x == 0) ++Regular;
                    else if (Channels[i].x < 0) ++Ceiling;
                    else if (Channels[i].x > 0) ++Floor;
                StringBuilder LayOut = new StringBuilder(Regular.ToString()).Append('.').Append(LFE);
                if (Ceiling > 0 || Floor > 0) LayOut.Append('.').Append(Ceiling);
                if (Floor > 0) LayOut.Append('.').Append(Floor);
                return LayOut.ToString();
            }
        }

        // ------------------------------------------------------------------
        // Public functions
        // ------------------------------------------------------------------
        /// <summary>Restarts the <see cref="AudioListener3D"/>.</summary>
        public void ForceReset() {
            ChannelCount = -1;
            ResetFunc();
        }

        /// <summary>Runs the frame update function.</summary>
        public void ForcedUpdate() {
            Update();
        }
    }
}