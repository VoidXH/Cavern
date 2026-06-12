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
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDestroy() {
            if (Current == this) {
                Current = null;
            }
        }

        /// <summary>
        /// Output Cavern's generated audio as a filter.
        /// </summary>
        /// <param name="unityBuffer">Output buffer</param>
        /// <param name="unityChannels">Output channel count</param>
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnAudioFilterRead(float[] unityBuffer, int unityChannels) {
            if (startSkip || Paused || SystemSampleRate == 0) {
                startSkip = false;
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
