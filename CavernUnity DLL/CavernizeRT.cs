using System;
using UnityEngine;

namespace Cavern {
    /// <summary><see cref="Cavernize"/> on a single source with diverted direct audio output.</summary>
    [AddComponentMenu("Audio/Single-channel height processor")]
    public class CavernizeRealtime : MonoBehaviour {
        /// <summary>The channel of the source to convert.</summary>
        [Tooltip("The channel of the source to convert.")]
        public int ChannelUsed = 0;
        /// <summary>Indicates a balanced input line.</summary>
        [Tooltip("Indicates a balanced input line.")]
        public bool Balanced = false;

        /// <summary>Target output for the base channel (L).</summary>
        [Tooltip("Target output for the base channel (L).")]
        public Jack Divert = Jack.Front;
        /// <summary>Target output for the height channel (R).</summary>
        [Tooltip("Target output for the height channel (R).")]
        public Jack HeightDivert = Jack.Front;

        /// <summary>Height effect strength.</summary>
        [Tooltip("Height effect strength.")]
        [Range(0, 1)] public float Effect = .75f;
        /// <summary>Output smoothing strength.</summary>
        [Tooltip("Output smoothing strength.")]
        [Range(0, 1)] public float Smoothness = .8f;

        /// <summary>Base speaker's position on the Y axis.</summary>
        [Tooltip("Base speaker's position on the Y axis.")]
        [Range(-1, 1)] public float BottomSpeakerHeight = 0;
        /// <summary>Height speaker's position on the Y axis.</summary>
        [Tooltip("Height speaker's position on the Y axis.")]
        [Range(-1, 1)] public float TopSpeakerHeight = 1;

        /// <summary>Peak decay rate multiplier.</summary>
        [Header("Metering")]
        [Tooltip("Peak decay rate multiplier.")]
        public float PeakDecay = .5f;

        /// <summary>Channel amplitude at the last update.</summary>
        public float LastPeak { get; private set; }
        /// <summary>Channel height at the last update.</summary>
        public float Height { get; private set; }

        /// <summary>Gain modifier calculated from fader level.</summary>
        static float FaderGain = 1f;

        /// <summary>The cinema processor's fader level. Required for height calculation as it is partially based on content volume.</summary>
        public static float Fader {
            get {
                float dB = CavernUtilities.SignalToDb(1f / FaderGain);
                return dB > -10 ? dB * .3f + 7 : (dB * .05f + 4.5f);
            }
            set {
                FaderGain = 1f / CavernUtilities.DbToSignal(value > 4 ? (value - 7) * 3.3333333333333f : ((value - 4.5f) * 20));
            }
        }

        float LastSample = 0, LowSample = 0, HighSample = 0;
        int SampleRate;

        void Start() {
            SampleRate = GetComponent<AudioSource>().clip.frequency;
        }

        void OnAudioFilterRead(float[] data, int channels) {
            // Mono downmix
            int Samples = data.Length, UpdateRate = Samples / channels, ActualSample = 0;
            float[] MonoMix = new float[UpdateRate];
            if (Balanced) {
                for (int Sample = ChannelUsed; Sample < Samples; Sample += channels)
                    MonoMix[ActualSample++] = data[Sample];
                ActualSample = 0;
                for (int Sample = ChannelUsed % 2 == 0 ? (ChannelUsed + 1) : (ChannelUsed - 1); Sample < Samples; Sample += channels)
                    MonoMix[ActualSample] = (MonoMix[ActualSample] - data[Sample]) * .5f;
            } else {
                for (int Sample = ChannelUsed; Sample < Samples; Sample += channels)
                    MonoMix[ActualSample++] = data[Sample];
            }
            // Cavernize
            float SmoothFactor = 1f - Mathf.LerpUnclamped(UpdateRate, SampleRate, Mathf.Pow(Smoothness, .1f)) / SampleRate * .999f;
            float MaxDepth = .0001f, MaxHeight = .0001f;
            for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                float CurrentSample = MonoMix[Sample] * FaderGain;
                HighSample = .9f * (HighSample + CurrentSample - LastSample);
                float AbsHigh = CavernUtilities.Abs(HighSample);
                if (MaxHeight < AbsHigh)
                    MaxHeight = AbsHigh;
                LowSample = LowSample * .99f + HighSample * .01f;
                float AbsLow = CavernUtilities.Abs(LowSample);
                if (MaxDepth < AbsLow)
                    MaxDepth = AbsLow;
                LastSample = CurrentSample;
            }
            MaxHeight = Mathf.Clamp((MaxHeight - MaxDepth * 1.2f) * Effect * 15, BottomSpeakerHeight, TopSpeakerHeight);
            Height = Mathf.LerpUnclamped(Height, MaxHeight, SmoothFactor);
            // Output
            float UpperMix = (MaxHeight - BottomSpeakerHeight) / (TopSpeakerHeight - BottomSpeakerHeight), LowerMix = 1f - UpperMix;
            int OutputPos = (int)Divert % channels - channels;
            Array.Clear(data, 0, Samples);
            for (int Sample = 0; Sample < UpdateRate; ++Sample) // Base channel
                data[OutputPos += channels] = MonoMix[Sample] * LowerMix;
            OutputPos = ((int)HeightDivert + 1) % channels - channels;
            for (int Sample = 0; Sample < UpdateRate; ++Sample) // Height channel
                data[OutputPos += channels] = MonoMix[Sample] * UpperMix;
            // Metering
            float CurrentPeak = 0;
            for (int Sample = 0; Sample < UpdateRate; ++Sample) {
                float Abs = CavernUtilities.Abs(data[Sample]);
                if (CurrentPeak < Abs)
                    CurrentPeak = Abs;
            }
            LastPeak = Mathf.Max(CurrentPeak, LastPeak - PeakDecay * UpdateRate / SampleRate);
        }
    }
}