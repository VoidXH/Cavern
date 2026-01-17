using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

using Cavern.Filters;
using Cavern.Virtualizer;

namespace Cavern {
    partial class Listener {
        // ------------------------------------------------------------------
        // Renderer settings
        // ------------------------------------------------------------------
        /// <summary>
        /// 3D environment type.
        /// </summary>
        /// <remarks>Set by the user and applied when a <see cref="Listener"/> is created.
        /// Don't override without user interaction.</remarks>
        public static Environments EnvironmentType {
            get => environmentType;
            set {
                environmentType = value;
                Recalculate();
            }
        }

        /// <summary>
        /// Virtual surround effect for headphones. This will replace the active <see cref="Channels"/> on the next frame.
        /// </summary>
        /// <remarks>Set by the user and applied when a <see cref="Listener"/> is created.
        /// Don't override without user interaction.</remarks>
        public static bool HeadphoneVirtualizer {
            get => headphoneVirtualizer;
            set {
                headphoneVirtualizer = value;
                Recalculate();
            }
        }

        /// <summary>
        /// Output channel layout. The default setup is the standard 5.1.
        /// </summary>
        /// <remarks>Set by the user and applied when a <see cref="Listener"/> is created.</remarks>
        public static Channel[] Channels { get; private set; } = { new Channel(0, -30), new Channel(0, 30),
            new Channel(0, 0), new Channel(15, 15, true), new Channel(0, -110), new Channel(0, 110) };

        /// <summary>
        /// Gets if the speakers are placed in a sphere according to current layout settings.
        /// </summary>
        public static bool IsSpherical {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => EnvironmentType == Environments.Studio || HeadphoneVirtualizer;
        }

        /// <summary>
        /// Is the user's speaker layout symmetrical?
        /// </summary>
        public static bool IsSymmetric { get; internal set; } = true;

        /// <summary>
        /// The single most important variable defining sound space in symmetric mode, the environment scaling.
        /// Originally set by the user and applied when a <see cref="Listener"/> is created, however, overriding
        /// it in specific applications can make a huge difference. Objects inside a box this size are positioned
        /// inside the room, and defines the range of balance between left/right, front/rear, and top/bottom speakers.
        /// Does not affect directional rendering. The user's settings should be
        /// respected, thus this vector should be scaled, not completely overridden.
        /// </summary>
        public static Vector3 EnvironmentSize {
            get => environmentSize;
            set {
                environmentSize = value;
                EnvironmentSizeInverse = Vector3.One / value;
            }
        }

        /// <summary>
        /// Relative size of the screen to the front wall's width. Used for rendering screen-anchored objects.
        /// The user's settings should be respected, thus this vector should not be overridden without good reason.
        /// </summary>
        public static Vector2 ScreenSize { get; set; } = new Vector2(.9f, .486f);

        /// <summary>
        /// How many sources can be played at the same time.
        /// </summary>
        public int MaximumSources {
            get => sourceDistances.Length;
            set => sourceDistances = new float[value];
        }

        /// <summary>
        /// Channel count on the left side of the room, but 1 if there's none, as it's used for volume division.
        /// </summary>
        internal static int leftChannels = 2;

        /// <summary>
        /// Channel count on the right side of the room, but 1 if there's none, as it's used for volume division.
        /// </summary>
        internal static int rightChannels = 2;

        /// <summary>
        /// 1 / <see cref="EnvironmentSize"/> on each axis. Cached optimization value for when a division is needed.
        /// </summary>
        internal static Vector3 EnvironmentSizeInverse { get; private set; } = new Vector3(1 / 10f, 1 / 7f, 1 / 10f);

        // ------------------------------------------------------------------
        // Listener settings
        // ------------------------------------------------------------------
        /// <summary>
        /// Absolute spatial position.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// Rotation in Euler angles (degrees).
        /// </summary>
        public Vector3 Rotation { get; set; }

        /// <summary>
        /// Global playback amplitude multiplier.
        /// </summary>
        public float Volume { get; set; } = 1;

        /// <summary>
        /// LFE channels' amplitude multiplier.
        /// </summary>
        public float LFEVolume { get; set; } = 1;

        /// <summary>
        /// Hearing distance.
        /// </summary>
        public float Range { get; set; } = 100;

        // ------------------------------------------------------------------
        // Normalizer settings
        // ------------------------------------------------------------------
        /// <summary>
        /// Adaption speed of the normalizer. 0 means disabled.
        /// </summary>
        public float Normalizer { get; set; } = 1;

        /// <summary>
        /// If active, the normalizer won't increase the volume above 100%.
        /// </summary>
        public bool LimiterOnly {
            get => normalizer.limiterOnly;
            set => normalizer.limiterOnly = value;
        }

        // ------------------------------------------------------------------
        // Advanced settings
        // ------------------------------------------------------------------
        /// <summary>
        /// Project sample rate (min. 44100). It's best to have all your audio clips in this sample rate for maximum performance.
        /// </summary>
        public int SampleRate { get; set; } = DefaultSampleRate;

        /// <summary>
        /// Update interval in audio samples (min. 16).
        /// Lower values mean better interpolation, but require more processing power.
        /// </summary>
        public int UpdateRate { get; set; } = 240;

        /// <summary>
        /// Maximum audio delay, defined in this FPS value. This is the minimum frame rate required to render continuous audio.
        /// </summary>
        public int DelayTarget { get; set; } = 12;

        /// <summary>
        /// Lower qualities increase performance for many sources.
        /// </summary>
        public QualityModes AudioQuality { get; set; } = QualityModes.High;

        /// <summary>
        /// Only mix LFE tagged sources to subwoofers.
        /// </summary>
        public bool LFESeparation { get; set; }

        /// <summary>
        /// Disable lowpass on the LFE channel.
        /// </summary>
        public bool DirectLFE { get; set; }

        // ------------------------------------------------------------------
        // Logic
        // ------------------------------------------------------------------
        /// <summary>
        /// Attached <see cref="Source"/>s.
        /// </summary>
        public IReadOnlyCollection<Source> ActiveSources => activeSources;

        /// <summary>
        /// Virtual surround effect for headphones. This will replace the active <see cref="Channels"/> on the next frame.
        /// </summary>
        static bool headphoneVirtualizer;

        /// <summary>
        /// 3D environment type.
        /// </summary>
        static Environments environmentType = Environments.Home;

        /// <summary>
        /// Value of <see cref="EnvironmentSize"/>.
        /// </summary>
        static Vector3 environmentSize = new Vector3(10, 7, 10);

        /// <summary>
        /// Position between the last and current game frame's playback position.
        /// </summary>
        internal float pulseDelta;

        /// <summary>
        /// Distances of sources from the listener.
        /// </summary>
        internal float[] sourceDistances = new float[defaultSourceLimit];

        /// <summary>
        /// Attached <see cref="Source"/>s.
        /// </summary>
        readonly LinkedList<Source> activeSources = new LinkedList<Source>();

        /// <summary>
        /// All sources from the last frame, rendered to the active <see cref="Channels"/>.
        /// </summary>
        readonly List<float[]> results = new List<float[]>();

        /// <summary>
        /// Active normalizer filter.
        /// </summary>
        readonly Normalizer normalizer = new Normalizer(true);

        /// <summary>
        /// Result of the last update. Size is [<see cref="Channels"/>.Length * <see cref="UpdateRate"/>].
        /// </summary>
        float[] renderBuffer;

        /// <summary>
        /// Same as <see cref="renderBuffer"/>, for multiple frames.
        /// </summary>
        float[] multiframeBuffer = Array.Empty<float>();

        /// <summary>
        /// Optimization variables.
        /// </summary>
        int channelCount, lastSampleRate, lastUpdateRate;

        /// <summary>
        /// Lowpass filters for each channel.
        /// </summary>
        Lowpass[] lowpasses;

        /// <summary>
        /// Active virtualization filter.
        /// </summary>
        VirtualizerFilter virtualizer;

        /// <summary>
        /// Default sample rate.
        /// </summary>
        public static int DefaultSampleRate => 48000;

        /// <summary>
        /// Version information.
        /// </summary>
        public static string Version => version;

        /// <summary>
        /// Version and creator information.
        /// </summary>
        public static string Info => info;

        /// <summary>
        /// Version number.
        /// </summary>
        /// <remarks>Hardcoded, because version reading is unsupported for .NET Standard projects</remarks>
        const string version = "2.1";

        /// <summary>
        /// Version and creator information.
        /// </summary>
        /// <remarks>Hardcoded, because version reading is unsupported for .NET Standard projects</remarks>
        const string info = "Cavern v" + version + " by VoidX (cavern.sbence.hu)";

        /// <summary>
        /// Default value of <see cref="MaximumSources"/>.
        /// </summary>
        const int defaultSourceLimit = 128;
    }
}
