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
    public sealed partial class Listener {
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
                    for (int i = 0; i < Channels.Length; i++) {
                        Channels[i] = new Channel(QMath.ParseFloat(save[savePos++]), QMath.ParseFloat(save[savePos++]),
                            Convert.ToBoolean(save[savePos++]));
                    }
                    EnvironmentType = (Environments)Convert.ToInt32(save[savePos++]);
                    EnvironmentSize = new Vector3(QMath.ParseFloat(save[savePos++]), QMath.ParseFloat(save[savePos++]),
                        QMath.ParseFloat(save[savePos++]));
                    HeadphoneVirtualizer = save.Length > savePos && Convert.ToBoolean(save[savePos++]); // Added: 2016.04.24.
                    savePos++; // Environment compensation (bool), added: 2017.06.18, removed: 2019.06.06.
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
                for (int channel = 0; channel < Channels.Length; channel++) {
                    if (Channels[channel].LFE) {
                        sub++;
                    } else if (Channels[channel].X == 0) {
                        regular++;
                    } else if (Channels[channel].X < 0) {
                        ceiling++;
                    } else if (Channels[channel].X > 0) {
                        floor++;
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
            for (int channel = 0; channel < Channels.Length; channel++) {
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
        /// Attach multiple sources to this listener.
        /// </summary>
        public void AttachSources(IEnumerable<Source> sources) {
            foreach (Source source in sources) {
                AttachSource(source);
            }
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
            for (int i = 0, c = activeSources.Count; i < c; i++) {
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
        /// Ask for update ticks for a single frame.
        /// </summary>
        /// <remarks>The output size is <see cref="UpdateRate"/> * <see cref="Channels"/>.Length.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float[] Render() => Render(1);

        /// <summary>
        /// Ask for update ticks for multiple frames.
        /// </summary>
        /// <remarks>The output size is <paramref name="frames"/> * <see cref="UpdateRate"/> * <see cref="Channels"/>.Length.</remarks>
        public float[] Render(int frames) {
            if (SampleRate < 44100 || UpdateRate < 16) { // Don't work with wrong settings
                return null;
            }
            for (int source = 0; source < sourceDistances.Length; source++) {
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
                for (int frame = 0; frame < frames; frame++) {
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
            for (int i = 0; i < channelCount; i++) {
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
            for (int result = 0; result < results.Count; result++) {
                WaveformUtils.Mix(results[result], renderBuffer);
            }

            // Volume and subwoofers' lowpass
            for (int channel = 0; channel < channelCount; channel++) {
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
    }
}