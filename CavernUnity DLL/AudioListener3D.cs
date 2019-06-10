using System;
using UnityEngine;

using Cavern.Utilities;
using Cavern.Virtualizer;

namespace Cavern {
    [AddComponentMenu("Audio/3D Audio Listener"), RequireComponent(typeof(AudioListener))]
    public partial class AudioListener3D : MonoBehaviour {
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
        /// <summary>Output timer.</summary>
        static int now = 0;
        /// <summary>Cached <see cref="SampleRate"/> for change detection.</summary>
        static int cachedSampleRate = 0;
        /// <summary>Cached <see cref="UpdateRate"/> for change detection.</summary>
        static int cachedUpdateRate = 0;
        /// <summary>Current time in ticks in the last frame.</summary>
        static long lastTicks = 0;

        // ------------------------------------------------------------------
        // Filter output
        // ------------------------------------------------------------------
        /// <summary>Filter buffer position, samples currently cached for output.</summary>
        static int bufferPosition = 0;
        /// <summary>Samples to play with the filter.</summary>
        static float[] filterOutput;
        /// <summary>Lock for the <see cref="bufferPosition"/>, which is set in multiple threads.</summary>
        static object bufferLock = new object();
        /// <summary>Filter normalizer gain.</summary>
        static float filterNormalizer = 1;
        /// <summary>Cached system sample rate.</summary>
        static int systemSampleRate;

        // ------------------------------------------------------------------
        // Internal functions
        // ------------------------------------------------------------------
        void MatchListener() {
            cavernListener.Volume = Volume;
            cavernListener.LFEVolume = LFEVolume;
            cavernListener.Range = Range;
            cavernListener.Paused = Paused;
            cavernListener.Normalizer = Normalizer;
            cavernListener.LimiterOnly = LimiterOnly;
            cavernListener.SampleRate = SampleRate;
            cavernListener.UpdateRate = UpdateRate;
            cavernListener.DelayTarget = DelayTarget;
            cavernListener.AudioQuality = AudioQuality;
            cavernListener.Manual = Manual;
            cavernListener.LFESeparation = LFESeparation;
            cavernListener.DirectLFE = DirectLFE;
            cavernListener.Position = CavernUtilities.VectorMatch(transform.position);
            cavernListener.Rotation = CavernUtilities.VectorMatch(transform.eulerAngles);
        }

        /// <summary>Reset the listener after any change.</summary>
        void ResetFunc() {
            ChannelCount = Channels.Length;
            cachedSampleRate = SampleRate;
            cachedUpdateRate = UpdateRate;
            bufferPosition = 0;
            lastTicks = DateTime.Now.Ticks;
            filterOutput = new float[ChannelCount * SampleRate];
        }

        /// <summary>The function to initially call when samples are available, to feed them to the filter.</summary>
        void Finalization() {
            if (!Paused) {
                float[] sourceBuffer = Output;
                int sourceBufferSize = Output.Length;
                if (HeadphoneVirtualizer)
                    VirtualizerFilter.Process(sourceBuffer);
                if (systemSampleRate != cachedSampleRate) { // Resample output for system sample rate
                    float[][] channelSplit = new float[ChannelCount][];
                    int splitSize = sourceBufferSize / ChannelCount;
                    for (int channel = 0; channel < ChannelCount; ++channel)
                        channelSplit[channel] = new float[splitSize];
                    int outputSample = 0;
                    for (int sample = 0; sample < splitSize; ++sample)
                        for (int channel = 0; channel < ChannelCount; ++channel)
                            channelSplit[channel][sample] = sourceBuffer[outputSample++];
                    for (int channel = 0; channel < ChannelCount; ++channel)
                        channelSplit[channel] = Resample.Adaptive(channelSplit[channel],
                            (int)(splitSize * systemSampleRate / (float)cachedSampleRate), AudioQuality);
                    int newUpdateRate = channelSplit[0].Length;
                    sourceBuffer = new float[sourceBufferSize = ChannelCount * newUpdateRate];
                    outputSample = 0;
                    for (int sample = 0; sample < newUpdateRate; ++sample)
                        for (int channel = 0; channel < ChannelCount; ++channel)
                            sourceBuffer[outputSample++] = channelSplit[channel][sample];
                }
                int end = filterOutput.Length;
                lock (bufferLock) {
                    int altEnd = bufferPosition + sourceBufferSize;
                    if (end > altEnd)
                        end = altEnd;
                    int outputPos = 0;
                    for (int bufferWrite = bufferPosition; bufferWrite < end; ++bufferWrite)
                        filterOutput[bufferWrite] = sourceBuffer[outputPos++];
                    bufferPosition = end;
                }
            } else
                filterOutput = new float[ChannelCount * cachedSampleRate];
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
            // Change checks
            if (HeadphoneVirtualizer) // Virtual channels
                VirtualizerFilter.SetLayout();
            MatchListener();
            if (Manual) {
                if (ChannelCount != Channels.Length || cachedSampleRate != SampleRate || cachedUpdateRate != UpdateRate)
                    ResetFunc();
                Output = cavernListener.Render();
                Manual = false;
                return;
            }
        }

        /// <summary>Output Cavern's generated audio as a filter.</summary>
        /// <param name="unityBuffer">Output buffer</param>
        /// <param name="unityChannels">Output channel count</param>
        void OnAudioFilterRead(float[] unityBuffer, int unityChannels) {
            if (Paused)
                return;
            if (ChannelCount != Channels.Length || cachedSampleRate != SampleRate || cachedUpdateRate != UpdateRate)
                ResetFunc();
            now += unityBuffer.Length / unityChannels;
            // Output buffer creation
            if (UpdateRate <= now) {
                int frames = now / UpdateRate;
                Output = cavernListener.Render(frames);
                // Finalize
                Finalization();
                now %= UpdateRate;
            }
            if (bufferPosition == 0)
                return;
            int samplesPerChannel = unityBuffer.Length / unityChannels, end = Math.Min(bufferPosition, samplesPerChannel * ChannelCount);
            // Output audio
            if (unityChannels <= 4) { // For non-surround setups, downmix properly
                for (int channel = 0; channel < ChannelCount; ++channel) {
                    int unityChannel = channel % unityChannels;
                    if (channel != 2 && channel != 3)
                        for (int sample = 0; sample < samplesPerChannel; ++sample)
                            unityBuffer[sample * unityChannels + unityChannel] += filterOutput[sample * ChannelCount + channel];
                    else {
                        for (int sample = 0; sample < samplesPerChannel; ++sample) {
                            int leftOut = sample * unityChannels;
                            float copySample = filterOutput[sample * ChannelCount + channel];
                            unityBuffer[leftOut] += copySample;
                            unityBuffer[leftOut + 1] += copySample;
                        }
                    }
                }
            } else {
                for (int channel = 0; channel < ChannelCount; ++channel) {
                    int unityChannel = channel % unityChannels;
                    for (int sample = 0; sample < samplesPerChannel; ++sample)
                        unityBuffer[sample * unityChannels + unityChannel] += filterOutput[sample * ChannelCount + channel];
                }
            }
            if (Normalizer != 0) // Normalize
                Utils.Normalize(ref unityBuffer, UpdateRate / (float)SampleRate, ref filterNormalizer, true);
            // Remove used samples
            lock (bufferLock) {
                for (int bufferPos = end; bufferPos < bufferPosition; ++bufferPos)
                    filterOutput[bufferPos - end] = filterOutput[bufferPos];
                int maxLatency = ChannelCount * cachedSampleRate / DelayTarget;
                if (bufferPosition < maxLatency)
                    bufferPosition -= end;
                else
                    bufferPosition = 0;
            }
        }
    }
}