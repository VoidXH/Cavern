﻿using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Tools for measuring frequency response.</summary>
    public static class Measurements {
        /// <summary>Actual FFT processing, somewhat in-place.</summary>
        static void ProcessFFT(Complex[] Samples) {
            int Length = Samples.Length, HalfLength = Length / 2;
            if (Length == 1)
                return;
            Complex[] Even = new Complex[HalfLength], Odd = new Complex[HalfLength];
            for (int Sample = 0, Pair = 0; Sample < HalfLength; ++Sample, Pair += 2) {
                Even[Sample] = Samples[Pair];
                Odd[Sample] = Samples[Pair + 1];
            }
            ProcessFFT(Even);
            ProcessFFT(Odd);
            float Step = -2 * Mathf.PI / Length;
            for (int i = 0; i < HalfLength; ++i)
                Odd[i].Rotate(Step * i);
            for (int i = 0; i < HalfLength; ++i) {
                Samples[i].Real = Even[i].Real + Odd[i].Real;
                Samples[i].Imaginary = Even[i].Imaginary + Odd[i].Imaginary;
                int o = i + HalfLength;
                Samples[o].Real = Even[i].Real - Odd[i].Real;
                Samples[o].Imaginary = Even[i].Imaginary - Odd[i].Imaginary;
            }
        }

        /// <summary>Fast Fourier transform a signal.</summary>
        public static Complex[] FFT(Complex[] Samples) {
            Samples = (Complex[])Samples.Clone();
            ProcessFFT(Samples);
            return Samples;
        }

        /// <summary>Fast Fourier transform a real signal.</summary>
        public static Complex[] FFT(float[] Samples) {
            int Length = Samples.Length;
            Complex[] ComplexSignal = new Complex[Length];
            for (int i = 0; i < Length; ++i)
                ComplexSignal[i].Real = Samples[i];
            ProcessFFT(ComplexSignal);
            return ComplexSignal;
        }

        /// <summary>Outputs IFFT(X) * N.</summary>
        static void ProcessIFFT(Complex[] Samples) {
            int Length = Samples.Length, HalfLength = Length / 2;
            if (Length == 1)
                return;
            Complex[] Even = new Complex[HalfLength], Odd = new Complex[HalfLength];
            for (int Sample = 0, Pair = 0; Sample < HalfLength; ++Sample, Pair += 2) {
                Even[Sample] = Samples[Pair];
                Odd[Sample] = Samples[Pair + 1];
            }
            ProcessIFFT(Even);
            ProcessIFFT(Odd);
            float Step = 2 * Mathf.PI / Length;
            for (int i = 0; i < HalfLength; ++i)
                Odd[i].Rotate(Step * i);
            for (int i = 0; i < HalfLength; ++i) {
                Samples[i].Real = Even[i].Real + Odd[i].Real;
                Samples[i].Imaginary = Even[i].Imaginary + Odd[i].Imaginary;
                int o = i + HalfLength;
                Samples[o].Real = Even[i].Real - Odd[i].Real;
                Samples[o].Imaginary = Even[i].Imaginary - Odd[i].Imaginary;
            }
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal.</summary>
        public static Complex[] IFFT(Complex[] Samples) {
            Samples = (Complex[])Samples.Clone();
            ProcessIFFT(Samples);
            int N = Samples.Length;
            float Multiplier = 1f / N;
            for (int i = 0; i < N; ++i) {
                Samples[i].Real *= Multiplier;
                Samples[i].Imaginary *= Multiplier;
            }
            return Samples;
        }

        /// <summary>Get the real part of a signal's FFT.</summary>
        public static float[] GetRealPart(Complex[] Samples) {
            int End = Samples.Length;
            float[] Output = new float[End];
            for (int Sample = 0; Sample < End; ++Sample)
                Output[Sample] = Samples[Sample].Real;
            return Output;
        }

        /// <summary>Get the imaginary part of a signal's FFT.</summary>
        public static float[] GetImaginaryPart(Complex[] Samples) {
            int End = Samples.Length;
            float[] Output = new float[End];
            for (int Sample = 0; Sample < End; ++Sample)
                Output[Sample] = Samples[Sample].Imaginary;
            return Output;
        }

        /// <summary>Get the gains of frequencies in a signal after FFT.</summary>
        public static float[] GetSpectrum(Complex[] Samples) {
            int End = Samples.Length / 2;
            float[] Output = new float[End];
            for (int Sample = 0; Sample < End; ++Sample)
                Output[Sample] = Samples[Sample].Magnitude;
            return Output;
        }

        /// <summary>Get the gains of frequencies in a signal after FFT.</summary>
        public static float[] GetPhase(Complex[] Samples) {
            int End = Samples.Length / 2;
            float[] Output = new float[End];
            for (int Sample = 0; Sample < End; ++Sample)
                Output[Sample] = Samples[Sample].Phase;
            return Output;
        }

        /// <summary>Generate a linear frequency sweep with a flat frequency response.</summary>
        public static float[] LinearSweep(float StartFreq, float EndFreq, int Samples, int SampleRate) {
            float[] Output = new float[Samples];
            float Chirpyness = (EndFreq - StartFreq) / (2 * Samples / (float)SampleRate);
            for (int Sample = 0; Sample < Samples; ++Sample) {
                float Position = Sample / (float)SampleRate;
                Output[Sample] = Mathf.Sin(2 * Mathf.PI * (StartFreq * Position + Chirpyness * Position * Position));
            }
            return Output;
        }

        /// <summary>Generate the frequencies at each sample's position in a linear frequency sweep.</summary>
        public static float[] LinearSweepFreqs(float StartFreq, float EndFreq, int Samples, int SampleRate) {
            float[] Freqs = new float[Samples];
            float Chirpyness = EndFreq - StartFreq / (Samples / (float)SampleRate);
            for (int Sample = 0; Sample < Samples; ++Sample)
                Freqs[Sample] = StartFreq + Chirpyness * Sample / SampleRate;
            return Freqs;
        }

        /// <summary>Generate an exponential frequency sweep.</summary>
        public static float[] ExponentialSweep(float StartFreq, float EndFreq, int Samples, int SampleRate) {
            float[] Output = new float[Samples];
            float Chirpyness = Mathf.Pow(EndFreq / StartFreq, SampleRate / (float)Samples),
                LogChirpyness = Mathf.Log(Chirpyness), SinConst = 2 * Mathf.PI * StartFreq;
            for (int Sample = 0; Sample < Samples; ++Sample)
                Output[Sample] = Mathf.Sin(SinConst * (Mathf.Pow(Chirpyness, Sample / (float)SampleRate) - 1) / LogChirpyness);
            return Output;
        }

        /// <summary>Generate the frequencies at each sample's position in an exponential frequency sweep.</summary>
        public static float[] ExponentialSweepFreqs(float StartFreq, float EndFreq, int Samples, int SampleRate) {
            float[] Freqs = new float[Samples];
            float Chirpyness = Mathf.Pow(EndFreq / StartFreq, SampleRate / (float)Samples);
            for (int Sample = 0; Sample < Samples; ++Sample)
                Freqs[Sample] = StartFreq + Mathf.Pow(Chirpyness, Sample / (float)SampleRate);
            return Freqs;
        }

        /// <summary>Add silence to the beginning and the end of a sweep for a larger response window.</summary>
        public static float[] SweepFraming(float[] Sweep) {
            int Length = Sweep.Length, InitialSilence = Length / 4;
            float[] Result = new float[Length * 2];
            for (int Sample = InitialSilence, End = Length + InitialSilence; Sample < End; ++Sample)
                Result[Sample] = Sweep[Sample - InitialSilence];
            return Result;
        }

        /// <summary>Get the frequency response using the original sweep signal's FFT as reference.</summary>
        public static Complex[] GetFrequencyResponse(Complex[] ReferenceFFT, float[] Response) {
            Complex[] ResponseFFT = FFT(Response);
            for (int Sample = 0, Length = ResponseFFT.Length; Sample < Length; ++Sample)
                ResponseFFT[Sample].Divide(ref ReferenceFFT[Sample]);
            return ResponseFFT;
        }

        /// <summary>Get the frequency response using the original sweep signal as reference.</summary>
        public static Complex[] GetFrequencyResponse(float[] Reference, float[] Response) => GetFrequencyResponse(FFT(Reference), Response);

        /// <summary>Get the complex impulse response using a precalculated frequency response.</summary>
        public static Complex[] GetImpulseResponse(Complex[] FrequencyResponse) => IFFT(FrequencyResponse);

        /// <summary>Get the complex impulse response using the original sweep signal as a reference.</summary>
        public static Complex[] GetImpulseResponse(float[] Reference, float[] Response) => IFFT(GetFrequencyResponse(Reference, Response));

        /// <summary>Get the complex impulse response faster using the original sweep signal as a reference.</summary>
        public static Complex[] GetImpulseResponse(float[] Reference, float[] Response, int SpeedMultiplier) {
            int OldSize = Reference.Length, NewSize = OldSize >> SpeedMultiplier, Step = OldSize / NewSize;
            float AvgDiv = NewSize / (float)OldSize;
            float[] NewReference = new float[NewSize], NewResponse = new float[NewSize];
            for (int OldSample = 0, NewSample = 0; OldSample < OldSize; ++NewSample) {
                float AverageRef = 0, AverageResp = 0;
                for (int NextStep = OldSample + Step; OldSample < NextStep; ++OldSample) {
                    AverageRef += Reference[OldSample];
                    AverageResp += Response[OldSample];
                }
                NewReference[NewSample] = AverageRef * AvgDiv;
                NewResponse[NewSample] = AverageResp * AvgDiv;
            }
            return IFFT(GetFrequencyResponse(NewReference, NewResponse));
        }

        /// <summary>Convert a response curve to decibel scale.</summary>
        public static void ConvertToDecibels(float[] Curve, float Minimum = -100) {
            for (int i = 0, End = Curve.Length; i < End; ++i) {
                Curve[i] = 20 * Mathf.Log10(Curve[i]);
                if (Curve[i] < Minimum)
                    Curve[i] = Minimum;
            }
        }

        /// <summary>Convert a response to logarithmically scaled cut frequency range.</summary>
        /// <param name="Samples">Source response</param>
        /// <param name="StartFreq">Frequency at the first position of the output</param>
        /// <param name="EndFreq">Frequency at the last position of the output</param>
        /// <param name="SampleRate">Sample rate of the measurement that generated the curve</param>
        /// <param name="ResultSize">Length of the resulting array</param>
        public static float[] ConvertToGraph(float[] Samples, float StartFreq, float EndFreq, int SampleRate, int ResultSize) {
            float SourceSize = Samples.Length - 1, Positioner = SourceSize * 2 / SampleRate, PowerMin = Mathf.Log10(StartFreq),
                PowerRange = (Mathf.Log10(EndFreq) - PowerMin) / ResultSize; // Divide 'i' here, not ResultScale times
            float[] Graph = new float[ResultSize];
            for (int i = 0; i < ResultSize; ++i) {
                float FreqHere = Mathf.Pow(10, PowerMin + PowerRange * i);
                Graph[i] = Samples[(int)(FreqHere * Positioner)];
            }
            return Graph;
        }

        /// <summary>Apply smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], float, float, int, int)"/>.</summary>
        public static float[] SmoothGraph(float[] Samples, float StartFreq, float EndFreq, float Octave = 1 / 3f) {
            if (Octave == 0)
                return (float[])Samples.Clone();
            float OctaveRange = Mathf.Log(EndFreq, 2) - Mathf.Log(StartFreq, 2);
            int Length = Samples.Length;
            int WindowSize = (int)(Length * Octave / OctaveRange);
            float[] Smoothed = new float[Length--];
            for (int Sample = 0; Sample < WindowSize; ++Sample) {
                int Start = 0, End = Sample + WindowSize;
                if (End > Length)
                    End = Length;
                float Average = 0;
                for (int WindowSample = Start; WindowSample <= End; ++WindowSample)
                    Average += Samples[WindowSample];
                Smoothed[Sample] = Average / (End - Start);
            }
            for (int Sample = WindowSize; Sample <= Length; ++Sample) {
                int Start = Sample - WindowSize, End = Sample + WindowSize;
                if (End > Length)
                    End = Length;
                float Average = 0;
                for (int WindowSample = Start; WindowSample <= End; ++WindowSample)
                    Average += Samples[WindowSample];
                Smoothed[Sample] = Average / (End - Start);
            }
            return Smoothed;
        }

        /// <summary>Apply variable smoothing (in octaves) on a graph drawn with <see cref="ConvertToGraph(float[], float, float, int, int)"/>.</summary>
        public static float[] SmoothGraph(float[] Samples, float StartFreq, float EndFreq, float StartOctave, float EndOctave) {
            float[] StartGraph = SmoothGraph(Samples, StartFreq, EndFreq, StartOctave), EndGraph = SmoothGraph(Samples, StartFreq, EndFreq, EndOctave),
                Output = new float[Samples.Length];
            float Positioner = 1f / Samples.Length;
            for (int i = 0, Length = Samples.Length; i < Length; ++i)
                Output[i] = CavernUtilities.FastLerp(StartGraph[i], EndGraph[i], i * Positioner);
            return Output;
        }

        /// <summary>Apply smoothing (in octaves) on a linear frequency response.</summary>
        public static float[] SmoothResponse(float[] Samples, int SampleRate, float Octave = 1 / 3f) {
            if (Octave == 0)
                return (float[])Samples.Clone();
            int Length = Samples.Length;
            float[] Smoothed = new float[Length--];
            float Nyquist = SampleRate * .5f, Offset = Mathf.Pow(2, Octave), Positioner = Length / Nyquist, FreqAtSample = Nyquist / Length;
            for (int Sample = 0; Sample <= Length; ++Sample) {
                float Freq = Sample * FreqAtSample, WindowStart = Freq / Offset, WindowEnd = Freq * Offset;
                int Start = (int)(WindowStart * Positioner), End = (int)(WindowEnd * Positioner);
                if (Start < 0)
                    Start = 0;
                if (End > Length)
                    End = Length;
                float Average = 0;
                for (int WindowSample = Start; WindowSample <= End; ++WindowSample)
                    Average += Samples[WindowSample];
                if (End != Start)
                    Smoothed[Sample] = Average / (End - Start);
                else
                    Smoothed[Sample] = Average;
            }
            return Smoothed;
        }
    }
}