using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern {
    public partial class Listener {
        /// <summary>Default value of <see cref="sourceLimit"/> and <see cref="MaximumSources"/>.</summary>
        const int defaultSourceLimit = 128;
        /// <summary>Position between the last and current game frame's playback position.</summary>
        internal float pulseDelta;
        /// <summary>Distances of sources from the listener.</summary>
        internal float[] sourceDistances = new float[defaultSourceLimit];
        /// <summary>The cached length of the <see cref="sourceDistances"/> array.</summary>
        internal int sourceLimit = defaultSourceLimit;

        /// <summary>Listener normalizer gain.</summary>
        float normalization = 1;
        /// <summary>Result of the last update. Size is [<see cref="Channels"/>.Length * <see cref="UpdateRate"/>].</summary>
        float[] renderBuffer;
        /// <summary>Same as <see cref="renderBuffer"/>, for multiple frames.</summary>
        float[] multiframeBuffer = new float[0];
        /// <summary>Optimization variables.</summary>
        int channelCount, lastSampleRate, lastUpdateRate;
        /// <summary>Attached <see cref="Source"/>s.</summary>
        LinkedList<Source> activeSources = new LinkedList<Source>();
        /// <summary>Lowpass filters for each channel.</summary>
        Lowpass[] lowpasses;

        /// <summary>Center of a listening space.</summary>
        public Listener() {
            string fileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Cavern\\Save.dat";
            if (File.Exists(fileName)) {
                string[] save = File.ReadAllLines(fileName);
                int savePos = 1;
                int channelCount = Convert.ToInt32(save[0]);
                Channels = new Channel[channelCount];
                NumberFormatInfo format = new NumberFormatInfo {
                    NumberDecimalSeparator = ","
                };
                for (int i = 0; i < channelCount; ++i)
                    Channels[i] = new Channel(Convert.ToSingle(save[savePos++], format), Convert.ToSingle(save[savePos++], format),
                        Convert.ToBoolean(save[savePos++]));
                EnvironmentType = (Environments)Convert.ToInt32(save[savePos++], format);
                EnvironmentSize = new Vector(Convert.ToSingle(save[savePos++], format), Convert.ToSingle(save[savePos++], format),
                    Convert.ToSingle(save[savePos++], format));
                HeadphoneVirtualizer = save.Length > savePos ? Convert.ToBoolean(save[savePos++]) : false; // Added: 2016.04.24.
                ++savePos; // Environment compensation (bool), added: 2017.06.18, removed: 2019.06.06.
            }
        }

        /// <summary>Recreate optimization arrays.</summary>
        void Reoptimize() {
            channelCount = Channels.Length;
            lastSampleRate = SampleRate;
            lastUpdateRate = UpdateRate;
            int outputLength = channelCount * UpdateRate;
            renderBuffer = new float[outputLength];
            lowpasses = new Lowpass[channelCount];
            for (int i = 0; i < channelCount; ++i)
                lowpasses[i] = new Lowpass(SampleRate, 120);
        }

        /// <summary>A single update.</summary>
        float[] Frame() {
            if (channelCount != Channels.Length || lastSampleRate != SampleRate || lastUpdateRate != UpdateRate)
                Reoptimize();
            // Collect audio data from sources
            LinkedListNode<Source> node = activeSources.First;
            List<float[]> results = new List<float[]>();
            while (node != null) {
                if (node.Value.Precollect())
                    results.Add(node.Value.Collect());
                node = node.Next;
            }
            // Mix sources to output
            Array.Clear(renderBuffer, 0, renderBuffer.Length);
            for (int result = 0, resultCount = results.Count; result < resultCount; ++result)
                Utils.Mix(results[result], renderBuffer);
            // Volume, distance compensation, and subwoofers' lowpass
            for (int channel = 0; channel < channelCount; ++channel) {
                if (Channels[channel].LFE) {
                    if (!DirectLFE)
                        lowpasses[channel].Process(renderBuffer, channel, channelCount);
                    Utils.Gain(renderBuffer, LFEVolume * Volume, channel, channelCount); // LFE Volume
                } else
                    Utils.Gain(renderBuffer, Volume, channel, channelCount);
            }
            if (Normalizer != 0) // Normalize
                Utils.Normalize(ref renderBuffer, Normalizer * UpdateRate / SampleRate, ref normalization, LimiterOnly);
            return renderBuffer;
        }

        /// <summary>Ask for update ticks.</summary>
        public float[] Render(int frames = 1) {
            for (int source = 0; source < sourceLimit; ++source)
                sourceDistances[source] = Range;
            pulseDelta = (frames * UpdateRate) / (float)SampleRate;
            LinkedListNode<Source> node = activeSources.First;
            while (node != null) {
                node.Value.Precalculate();
                node = node.Next;
            }
            if (frames == 1) return Frame();
            else {
                int sampleCount = frames * Channels.Length * UpdateRate;
                if (multiframeBuffer.Length != sampleCount)
                    multiframeBuffer = new float[sampleCount];
                for (int frame = 0; frame < frames; ++frame) {
                    float[] frameBuffer = Frame();
                    for (int sample = 0, samples = frameBuffer.Length, offset = frame * samples; sample < samples; ++sample)
                        multiframeBuffer[sample + offset] = frameBuffer[sample];
                }
                return multiframeBuffer;
            }
        }
    }
}