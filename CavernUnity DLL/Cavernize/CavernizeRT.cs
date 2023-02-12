using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern {
    /// <summary>
    /// <see cref="Cavernize"/> on a single source with diverted direct audio output.
    /// </summary>
    [AddComponentMenu("Audio/Cavernize/Single-channel height processor")]
    public class CavernizeRealtime : MonoBehaviour {
        /// <summary>
        /// The channel of the source to convert.
        /// </summary>
        [Tooltip("The channel of the source to convert.")]
        public int ChannelUsed;

        /// <summary>
        /// Indicates a balanced input line.
        /// </summary>
        [Tooltip("Indicates a balanced input line.")]
        public bool Balanced;

        /// <summary>
        /// Target output for the base channel (L).
        /// </summary>
        [Tooltip("Target output for the base channel (L).")]
        public Jack Divert = Jack.Front;

        /// <summary>
        /// Target output for the height channel (R).
        /// </summary>
        [Tooltip("Target output for the height channel (R).")]
        public Jack HeightDivert = Jack.Front;

        /// <summary>
        /// Height effect strength.
        /// </summary>
        [Tooltip("Height effect strength.")]
        [Range(0, 1)] public float Effect = .75f;

        /// <summary>
        /// Output smoothing strength.
        /// </summary>
        [Tooltip("Output smoothing strength.")]
        [Range(0, 1)] public float Smoothness = .8f;

        /// <summary>
        /// Base speaker's position on the horizontal axis.
        /// </summary>
        [Tooltip("Base speaker's position on the horizontal axis.")]
        [Range(-1, 1)] public float BottomSpeakerHeight;

        /// <summary>
        /// Height speaker's position on the horizontal axis.
        /// </summary>
        [Tooltip("Height speaker's position on the horizontal axis.")]
        [Range(-1, 1)] public float TopSpeakerHeight = 1;

        /// <summary>
        /// Peak decay rate multiplier.
        /// </summary>
        [Header("Metering")]
        [Tooltip("Peak decay rate multiplier.")]
        public float PeakDecay = .5f;

        /// <summary>
        /// Channel amplitude at the last update.
        /// </summary>
        public float LastPeak { get; private set; }

        /// <summary>
        /// Channel height at the last update.
        /// </summary>
        public float Height { get; private set; }

        /// <summary>
        /// Gain modifier calculated from fader level.
        /// </summary>
        static float faderGain = 1f;

        /// <summary>
        /// The cinema processor's fader level. Required for height calculation as it is partially based on content volume.
        /// </summary>
        public static float Fader {
            get {
                float dB = 20 * Mathf.Log10(1f / faderGain);
                if (dB > -10) {
                    return dB * .3f + 7;
                }
                return dB * .05f + 4.5f;
            }
            set => faderGain = 1f / Mathf.Pow(10, 1 / 20f * (value > 4 ? (value - 7) * 3.3333333333333f : ((value - 4.5f) * 20)));
        }

        float lastSample, lowSample, highSample;
        int sampleRate;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() => sampleRate = GetComponent<AudioSource>().clip.frequency;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnAudioFilterRead(float[] data, int channels) {
            // Mono downmix
            int UpdateRate = data.Length / channels, actualSample = 0;
            float[] monoMix = new float[UpdateRate];
            if (Balanced) {
                for (int sample = ChannelUsed; sample < data.Length; sample += channels) {
                    monoMix[actualSample++] = data[sample];
                }
                actualSample = 0;
                for (int sample = ChannelUsed % 2 == 0 ? (ChannelUsed + 1) : (ChannelUsed - 1); sample < data.Length; sample += channels) {
                    monoMix[actualSample] = (monoMix[actualSample] - data[sample]) * .5f;
                }
            } else {
                for (int sample = ChannelUsed; sample < data.Length; sample += channels) {
                    monoMix[actualSample++] = data[sample];
                }
            }

            // Cavernize
            float smoothFactor = 1f - Mathf.LerpUnclamped(UpdateRate, sampleRate, Mathf.Pow(Smoothness, .1f)) / sampleRate * .999f;
            float maxDepth = .0001f, MaxHeight = .0001f, absHigh, absLow;
            for (int sample = 0; sample < UpdateRate; sample++) {
                float currentSample = monoMix[sample] * faderGain;
                highSample = .9f * (highSample + currentSample - lastSample);
                absHigh = Math.Abs(highSample);
                if (MaxHeight < absHigh) {
                    MaxHeight = absHigh;
                }
                lowSample = lowSample * .99f + highSample * .01f;
                absLow = Math.Abs(lowSample);
                if (maxDepth < absLow) {
                    maxDepth = absLow;
                }
                lastSample = currentSample;
            }
            MaxHeight = Mathf.Clamp((MaxHeight - maxDepth * 1.2f) * Effect * 15, BottomSpeakerHeight, TopSpeakerHeight);
            Height = Mathf.LerpUnclamped(Height, MaxHeight, smoothFactor);

            // Output
            float upperMix = (MaxHeight - BottomSpeakerHeight) / (TopSpeakerHeight - BottomSpeakerHeight),
                lowerMix = Mathf.Sin(Mathf.PI / 2 * (1f - upperMix));
            upperMix = Mathf.Sin(Mathf.PI / 2 * upperMix);
            int outputPos = (int)Divert % channels - channels;
            data.Clear();
            for (int sample = 0; sample < UpdateRate; sample++) { // Base channel
                data[outputPos += channels] = monoMix[sample] * lowerMix;
            }
            outputPos = ((int)HeightDivert + 1) % channels - channels;
            for (int sample = 0; sample < UpdateRate; sample++) { // Height channel
                data[outputPos += channels] = monoMix[sample] * upperMix;
            }

            // Metering
            float currentPeak = 0, abs;
            for (int sample = 0; sample < UpdateRate; sample++) {
                abs = Math.Abs(data[sample]);
                if (currentPeak < abs) {
                    currentPeak = abs;
                }
            }
            LastPeak = Mathf.Max(currentPeak, LastPeak - PeakDecay * UpdateRate / sampleRate);
        }
    }
}