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
            // Change checks
            if (HeadphoneVirtualizer) // Virtual channels
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
            if (Manual) {
                if (ChannelCount != Listener.Channels.Length || cachedSampleRate != SampleRate)
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
            if (Paused || Manual)
                return;
            if (ChannelCount != Listener.Channels.Length || cachedSampleRate != SampleRate)
                ResetFunc();
            int needed = unityBuffer.Length / unityChannels - bufferPosition / ChannelCount;
            int frames = Math.Min(needed * cachedSampleRate / systemSampleRate / UpdateRate + 1, systemSampleRate / (UpdateRate * 4));
            float[] renderBuffer = cavernListener.Render(frames);
            int renderSize = renderBuffer.Length;
            if (systemSampleRate != cachedSampleRate) { // Resample output for system sample rate
                renderBuffer = Resample.Adaptive(renderBuffer, renderSize / ChannelCount * systemSampleRate / cachedSampleRate,
                    ChannelCount, AudioQuality);
                renderSize = renderBuffer.Length;
            }
            if (HeadphoneVirtualizer)
                VirtualizerFilter.Process(renderBuffer);
            Output = (float[])renderBuffer.Clone(); // Has to be cloned, as this might be a cached array
            int end = filterOutput.Length, altEnd = bufferPosition + renderSize, outputPos = 0;
            if (end > altEnd)
                end = altEnd;
            for (int bufferWrite = bufferPosition; bufferWrite < end; ++bufferWrite)
                filterOutput[bufferWrite] = Output[outputPos++];
            bufferPosition = end;
            // Output audio
            int samplesPerChannel = unityBuffer.Length / unityChannels, written = Math.Min(bufferPosition, samplesPerChannel * ChannelCount);
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
            for (int bufferPos = written; bufferPos < bufferPosition; ++bufferPos)
                filterOutput[bufferPos - written] = filterOutput[bufferPos];
            bufferPosition -= written;
        }
    }
}