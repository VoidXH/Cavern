using System;
using UnityEngine;

namespace Cavern {
    /// <summary><see cref="Cavernize"/> on a single source with diverted direct audio output.</summary>
    public class CavernizeRealtime : MonoBehaviour {
        public int ChannelUsed = 0;
        public bool Balanced = false;

        public Jack Divert = Jack.Front;
        public Jack HeightDivert = Jack.Front;

        [Range(0, 1)] public float Effect = .75f;
        [Range(0, 1)] public float Smoothness = .8f;

        [Range(-1, 1)] public float BottomSpeakerHeight = 0;
        [Range(-1, 1)] public float TopSpeakerHeight = 1;

        [Header("Metering")]
        [Tooltip("Peak decay rate multiplier.")]
        public float PeakDecay = .5f;

        public float LastPeak { get; private set; }
        public float Height { get; private set; }

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
                HighSample = .9f * (HighSample + MonoMix[Sample] - LastSample);
                float AbsHigh = Mathf.Abs(HighSample);
                if (MaxHeight < AbsHigh)
                    MaxHeight = AbsHigh;
                LowSample = LowSample * .99f + HighSample * .01f;
                float AbsLow = Mathf.Abs(LowSample);
                if (MaxDepth < AbsLow)
                    MaxDepth = AbsLow;
                LastSample = MonoMix[Sample];
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
                float Abs = Mathf.Abs(data[Sample]);
                if (CurrentPeak < Abs)
                    CurrentPeak = Abs;
            }
            LastPeak = Mathf.Max(CurrentPeak, LastPeak - PeakDecay * UpdateRate / SampleRate);
        }
    }
}