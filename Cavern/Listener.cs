using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;
using Cavern.Virtualizer;

namespace Cavern {
    /// <summary>
    /// Center of a listening space. Attached <see cref="Source"/>s will be rendered relative to this object's position.
    /// </summary>
    public sealed class Listener {
        /// <summary>
        /// Version and creator information.
        /// </summary>
        public static string Info => info;

        /// <summary>
        /// Default sample rate.
        /// </summary>
        public static int DefaultSampleRate => 48000;

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
        float[] multiframeBuffer = new float[0];

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
        /// Center of a listening space. Attached <see cref="Source"/>s will be rendered relative to this object's position.
        /// The layout set up by the user will be used.
        /// </summary>
        public Listener() : this(true) { }

        /// <summary>
        /// Center of a listening space. Attached <see cref="Source"/>s will be rendered relative to this object's position.
        /// </summary>
        /// <param name="loadGlobals">Load the global settings for all listeners. This should be false for listeners created
        /// on the fly, as this overwrites previous application settings that might have been modified.</param>
        public Listener(bool loadGlobals) {
            if (!loadGlobals) {
                return;
            }
            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Cavern\\Save.dat";
            if (File.Exists(fileName)) {
                string[] save = File.ReadAllLines(fileName);
                try {
                    int savePos = 1;
                    Channels = new Channel[Convert.ToInt32(save[0])];
                    for (int i = 0; i < Channels.Length; ++i) {
                        Channels[i] = new Channel(QMath.ParseFloat(save[savePos++]), QMath.ParseFloat(save[savePos++]),
                            Convert.ToBoolean(save[savePos++]));
                    }
                    EnvironmentType = (Environments)Convert.ToInt32(save[savePos++]);
                    EnvironmentSize = new Vector3(QMath.ParseFloat(save[savePos++]), QMath.ParseFloat(save[savePos++]),
                        QMath.ParseFloat(save[savePos++]));
                    HeadphoneVirtualizer = save.Length > savePos && Convert.ToBoolean(save[savePos++]); // Added: 2016.04.24.
                    ++savePos; // Environment compensation (bool), added: 2017.06.18, removed: 2019.06.06.
                } catch {
                    Channels = ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(6));
                    EnvironmentType = Environments.Home;
                    EnvironmentSize = new Vector3(10, 7, 10);
                }
            }
        }

        /// <summary>
        /// Current speaker layout name in the format of &lt;main&gt;.&lt;LFE&gt;.&lt;height&gt;.&lt;floor&gt;,
        /// or simply "Virtualization".
        /// </summary>
        public static string GetLayoutName() {
            if (headphoneVirtualizer) {
                return "Virtualization";
            } else {
                int regular = 0, sub = 0, ceiling = 0, floor = 0;
                for (int channel = 0; channel < Channels.Length; ++channel) {
                    if (Channels[channel].LFE) {
                        ++sub;
                    } else if (Channels[channel].X == 0) {
                        ++regular;
                    } else if (Channels[channel].X < 0) {
                        ++ceiling;
                    } else if (Channels[channel].X > 0) {
                        ++floor;
                    }
                }
                StringBuilder layout = new StringBuilder(regular.ToString()).Append('.').Append(sub);
                if (ceiling > 0 || floor > 0) {
                    layout.Append('.').Append(ceiling);
                }
                if (floor > 0) {
                    layout.Append('.').Append(floor);
                }
                return layout.ToString();
            }
        }

        /// <summary>
        /// Replace the channel layout.
        /// </summary>
        /// <remarks>If you're making your own configurator, don't forget to overwrite the Cavern configuration file.</remarks>
        public static void ReplaceChannels(Channel[] channels) {
            Channels = channels;
            Channel.SymmetryCheck();
        }

        /// <summary>
        /// Replace the channel layout with a standard of a given channel count.
        /// The <see cref="Listener"/> will set up itself automatically with the user's saved configuration.
        /// The used audio channels can be queried through <see cref="Channels"/>, which should be respected,
        /// and the output audio channel count should be set to its length. If this is not possible,
        /// the layout could be set to a standard by the number of channels with this function.
        /// </summary>
        public static void ReplaceChannels(int channelCount) =>
            ReplaceChannels(ChannelPrototype.ToLayout(ChannelPrototype.GetStandardMatrix(channelCount)));

        /// <summary>
        /// Implicit null check.
        /// </summary>
        public static implicit operator bool(Listener listener) => listener != null;

        /// <summary>
        /// Recalculate the rendering environment.
        /// </summary>
        static void Recalculate() {
            for (int channel = 0; channel < Channels.Length; ++channel) {
                Channels[channel].Recalculate();
            }
        }

        /// <summary>
        /// Attach a source to this listener.
        /// </summary>
        public void AttachSource(Source source) {
            if (source.listener) {
                source.listener.DetachSource(source);
            }
            source.listenerNode = activeSources.AddLast(source);
            source.listener = this;
        }

        /// <summary>
        /// Attach a source to this listener, to the first place of the processing queue.
        /// </summary>
        public void AttachPrioritySource(Source source) {
            if (source.listener) {
                source.listener.DetachSource(source);
            }
            source.listenerNode = activeSources.AddFirst(source);
            source.listener = this;
        }

        /// <summary>
        /// Detach a source from this listener.
        /// </summary>
        public void DetachSource(Source source) {
            if (source == this) {
                activeSources.Remove(source.listenerNode);
                source.listener = null;
            }
        }

        /// <summary>
        /// Detach all sources from this listener.
        /// </summary>
        public void DetachAllSources() {
            for (int i = 0, c = activeSources.Count; i < c; ++i) {
                activeSources.First.Value.listener = null;
                activeSources.RemoveFirst();
            }
        }

        /// <summary>
        /// Perform an update on all objects without rendering anything to the listener's output.
        /// </summary>
        public void Ping() {
            LinkedListNode<Source> node = activeSources.First;
            while (node != null) {
                sourceDistances[0] = Range;
                node.Value.Precalculate();
                node.Value.Precollect();
                node = node.Next;
            }
        }

        /// <summary>
        /// Ask for update ticks.
        /// </summary>
        public float[] Render(int frames = 1) {
            if (SampleRate < 44100 || UpdateRate < 16) { // Don't work with wrong settings
                return null;
            }
            for (int source = 0; source < sourceDistances.Length; ++source) {
                sourceDistances[source] = Range;
            }
            pulseDelta = frames * UpdateRate / (float)SampleRate;

            // Choose the sources to play
            LinkedListNode<Source> node = activeSources.First;
            while (node != null) {
                node.Value.Precalculate();
                node = node.Next;
            }

            // Render the required number of frames
            if (frames == 1) {
                float[] result = Frame();
                if (headphoneVirtualizer) {
                    virtualizer.Process(result, SampleRate);
                }
                return result;
            } else {
                int sampleCount = frames * Channels.Length * UpdateRate;
                if (multiframeBuffer.Length != sampleCount) {
                    multiframeBuffer = new float[sampleCount];
                }
                for (int frame = 0; frame < frames; ++frame) {
                    float[] frameBuffer = Frame();
                    Array.Copy(frameBuffer, 0, multiframeBuffer, frame * frameBuffer.Length, frameBuffer.Length);
                }
                if (headphoneVirtualizer) {
                    virtualizer.Process(multiframeBuffer, SampleRate);
                }
                return multiframeBuffer;
            }
        }

        /// <summary>
        /// Recreate optimization arrays.
        /// </summary>
        void Reoptimize() {
            channelCount = Channels.Length;
            lastSampleRate = SampleRate;
            lastUpdateRate = UpdateRate;
            renderBuffer = new float[channelCount * UpdateRate];
            lowpasses = new Lowpass[channelCount];
            for (int i = 0; i < channelCount; ++i) {
                lowpasses[i] = new Lowpass(SampleRate, 120);
            }
        }

        /// <summary>
        /// A single update.
        /// </summary>
        float[] Frame() {
            if (headphoneVirtualizer) {
                virtualizer ??= new VirtualizerFilter();
                virtualizer.SetLayout();
            }
            if (channelCount != Channels.Length || lastSampleRate != SampleRate || lastUpdateRate != UpdateRate) {
                Reoptimize();
            }

            // Collect audio data from sources
            LinkedListNode<Source> node = activeSources.First;
            results.Clear();
            while (node != null) {
                if (node.Value.Precollect()) {
                    results.Add(node.Value.Collect());
                }
                node = node.Next;
            }

            // Mix sources to output
            Array.Clear(renderBuffer, 0, renderBuffer.Length);
            for (int result = 0; result < results.Count; ++result) {
                WaveformUtils.Mix(results[result], renderBuffer);
            }

            // Volume and subwoofers' lowpass
            for (int channel = 0; channel < channelCount; ++channel) {
                if (Channels[channel].LFE) {
                    if (!DirectLFE) {
                        lowpasses[channel].Process(renderBuffer, channel, channelCount);
                    }
                    WaveformUtils.Gain(renderBuffer, LFEVolume * Volume, channel, channelCount); // LFE Volume
                } else {
                    WaveformUtils.Gain(renderBuffer, Volume, channel, channelCount);
                }
            }
            if (Normalizer != 0) { // Normalize
                normalizer.decayFactor = Normalizer * UpdateRate / SampleRate;
                normalizer.Process(renderBuffer);
            }
            return renderBuffer;
        }

        /// <summary>
        /// Version and creator information.
        /// </summary>
        /// <remarks>Hardcoded, because version reading is unsupported for .NET Standard projects</remarks>
        const string info = "Cavern v2.0 by VoidX (cavern.sbence.hu)";

        /// <summary>
        /// Default value of <see cref="MaximumSources"/>.
        /// </summary>
        const int defaultSourceLimit = 128;
    }
}