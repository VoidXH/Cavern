using System;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Tools for measuring frequency response.</summary>
    public static class Measurements {
        internal const float Pix2 = Mathf.PI * 2f, NegPix2 = -Pix2;

        /// <summary>Fast Fourier transform a real signal.</summary>
        public static Complex[] FFT(float[] Samples) {
            int Length = Samples.Length;
            Complex[] ComplexSignal = new Complex[Length];
            for (int i = 0; i < Length; ++i)
                ComplexSignal[i] = new Complex(Samples[i]);
            return FFT(ComplexSignal);
        }

        /// <summary>Fast Fourier transform a signal.</summary>
        public static Complex[] FFT(Complex[] Samples) {
            int Length = Samples.Length, HalfLength = Length / 2;
            if (Length == 1)
                return Samples;
            Complex[] Output = new Complex[Length], Even = new Complex[HalfLength], Odd = new Complex[HalfLength];
            for (int Sample = 0; Sample < HalfLength; ++Sample) {
                int Pair = Sample + Sample;
                Even[Sample] = Samples[Pair];
                Odd[Sample] = Samples[Pair + 1];
            }
            Complex[] EvenFFT = FFT(Even), OddFFT = FFT(Odd);
            for (int i = 0; i < HalfLength; ++i) {
                float Angle = NegPix2 * i / Length;
                OddFFT[i] *= new Complex(Mathf.Cos(Angle), Mathf.Sin(Angle));
            }
            for (int i = 0; i < Length / 2; ++i) {
                Output[i] = EvenFFT[i] + OddFFT[i];
                Output[i + HalfLength] = EvenFFT[i] - OddFFT[i];
            }
            return Output;
        }

        /// <summary>Get the gains of frequencies in a signal after FFT.</summary>
        public static float[] GetSpectrum(Complex[] Samples) {
            int End = Samples.Length / 2;
            float[] Output = new float[End];
            for (int Sample = 0; Sample < End; ++Sample)
                Output[Sample] = (float)Samples[Sample].Magnitude;
            return Output;
        }

        /// <summary>Get the gains of frequencies in a signal after FFT.</summary>
        public static float[] GetPhase(Complex[] Samples) {
            int End = Samples.Length / 2;
            float[] Output = new float[End];
            for (int Sample = 0; Sample < End; ++Sample)
                Output[Sample] = (float)Samples[Sample].Phase;
            return Output;
        }

        /// <summary>Generate a linear frequency sweep with a flat frequency response.</summary>
        public static float[] LinearSweep(float FreqStart, float FreqEnd, int Samples, int SampleRate) {
            float[] Output = new float[Samples];
            float Chirpyness = (FreqEnd - FreqStart) / (2 * Samples / (float)SampleRate);
            for (int Sample = 0; Sample < Samples; ++Sample) {
                float Position = Sample / (float)SampleRate;
                Output[Sample] = Mathf.Sin(Pix2 * (FreqStart * Position + Chirpyness * Position * Position));
            }
            return Output;
        }

        /// <summary>Generate the frequencies at each sample's position in a linear frequency sweep.</summary>
        public static float[] LinearSweepFreqs(float FreqStart, float FreqEnd, int Samples, int SampleRate) {
            float[] Freqs = new float[Samples];
            float Chirpyness = FreqEnd - FreqStart / (Samples / (float)SampleRate);
            for (int Sample = 0; Sample < Samples; ++Sample)
                Freqs[Sample] = FreqStart + Chirpyness * Sample / SampleRate;
            return Freqs;
        }

        /// <summary>Generate an exponential frequency sweep.</summary>
        public static float[] ExponentialSweep(float FreqStart, float FreqEnd, int Samples, int SampleRate) {
            float[] Output = new float[Samples];
            float Chirpyness = Mathf.Pow(FreqEnd / FreqStart, SampleRate / (float)Samples),
                LogChirpyness = Mathf.Log(Chirpyness), SinConst = Pix2 * FreqStart;
            for (int Sample = 0; Sample < Samples; ++Sample)
                Output[Sample] =
                    Mathf.Sin(SinConst * (Mathf.Pow(Chirpyness, Sample / (float)SampleRate) - 1) / LogChirpyness);
            return Output;
        }

        /// <summary>Generate the frequencies at each sample's position in an exponential frequency sweep.</summary>
        public static float[] ExponentialSweepFreqs(float FreqStart, float FreqEnd, int Samples, int SampleRate) {
            float[] Freqs = new float[Samples];
            float Chirpyness = Mathf.Pow(FreqEnd / FreqStart, SampleRate / (float)Samples);
            for (int Sample = 0; Sample < Samples; ++Sample)
                Freqs[Sample] = FreqStart + Mathf.Pow(Chirpyness, Sample / (float)SampleRate);
            return Freqs;
        }

        /// <summary>Get the actual frequency response using the original sweep signal as reference.</summary>
        public static float[] GetFrequencyResponse(float[] Reference, float[] Response, float FreqStart, float FreqEnd, int SampleRate) {
            float[] SourceResponse = GetSpectrum(FFT(Reference));
            float[] ResultResponse = GetSpectrum(FFT(Response));
            float PosMult = SourceResponse.Length / (SampleRate * .5f);
            int Start = (int)(FreqStart * PosMult), End = (int)(FreqEnd * PosMult), ResultSize = End - Start;
            float[] Result = new float[ResultSize];
            for (int Sample = Start; Sample < End; ++Sample)
                Result[Sample - Start] = SourceResponse[Sample] != 0 ? ResultResponse[Sample] / SourceResponse[Sample] : 0;
            return Result;
        }

        /// <summary>Convert a response curve to decibel scale.</summary>
        public static float[] ConvertToDecibels(float[] Curve) {
            Curve = (float[])Curve.Clone();
            for (int i = 0, End = Curve.Length; i < End; ++i)
                Curve[i] = 20 * Mathf.Log10(Curve[i]);
            return Curve;
        }

        /// <summary>Apply smoothing (in octaves) on a frequency response curve.</summary>
        public static float[] SmoothResponse(float[] Samples, float FreqStart, float FreqEnd, float Octave = 1 / 3f) {
            if (Octave == 0)
                return (float[])Samples.Clone();
            int Length = Samples.Length;
            float[] Smoothed = new float[Length];
            float Span = FreqEnd - FreqStart;
            for (int Sample = 0; Sample < Length; ++Sample) {
                float Freq = FreqStart + Span * Sample / Length, Offset = Mathf.Pow(2, Octave),
                    WindowStart = Freq / Offset, WindowEnd = Freq * Offset, Positioner = Length / Span;
                int Start = Math.Max((int)((WindowStart - FreqStart) * Positioner), 0),
                    End = Math.Min((int)((WindowEnd - FreqStart) * Positioner), Length - 1);
                float Average = 0;
                for (int WindowSample = Start; WindowSample < End; ++WindowSample)
                    Average += Samples[WindowSample];
                Smoothed[Sample] = Average / (End - Start);
            }
            return Smoothed;
        }
    }
}