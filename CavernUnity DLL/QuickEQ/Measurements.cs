using UnityEngine;

using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Tools for measuring frequency response.</summary>
    public static class Measurements {
        /// <summary>Actual FFT processing, somewhat in-place.</summary>
        static void ProcessFFT(Complex[] Samples, FFTCache Cache, int Depth) {
            int Length = Samples.Length, HalfLength = Length / 2;
            if (Length == 1)
                return;
            Complex[] Even = FFTCache.Even[Depth], Odd = FFTCache.Odd[Depth];
            for (int Sample = 0, Pair = 0; Sample < HalfLength; ++Sample, Pair += 2) {
                Even[Sample] = Samples[Pair];
                Odd[Sample] = Samples[Pair + 1];
            }
            ProcessFFT(Even, Cache, --Depth);
            ProcessFFT(Odd, Cache, Depth);
            int StepMul = Cache.Cos.Length / HalfLength;
            for (int i = 0; i < HalfLength; ++i) {
                int CachePos = i * StepMul;
                float OddReal = Odd[i].Real * Cache.Cos[CachePos] - Odd[i].Imaginary * Cache.Sin[CachePos],
                    OddImag = Odd[i].Real * Cache.Sin[CachePos] + Odd[i].Imaginary * Cache.Cos[CachePos];
                Samples[i].Real = Even[i].Real + OddReal;
                Samples[i].Imaginary = Even[i].Imaginary + OddImag;
                int o = i + HalfLength;
                Samples[o].Real = Even[i].Real - OddReal;
                Samples[o].Imaginary = Even[i].Imaginary - OddImag;
            }
        }

        /// <summary>Fourier-transform a signal in 1D. The result is the spectral power.</summary>
        static void ProcessFFT(float[] Samples, FFTCache Cache) {
            int Length = Samples.Length, HalfLength = Length / 2, Depth = CavernUtilities.Log2(Samples.Length) - 1;
            if (Length == 1)
                return;
            Complex[] Even = FFTCache.Even[Depth], Odd = FFTCache.Odd[Depth];
            for (int Sample = 0, Pair = 0; Sample < HalfLength; ++Sample, Pair += 2) {
                Even[Sample].Real = Samples[Pair];
                Odd[Sample].Real = Samples[Pair + 1];
            }
            ProcessFFT(Even, Cache, --Depth);
            ProcessFFT(Odd, Cache, Depth);
            int StepMul = Cache.Cos.Length / HalfLength;
            for (int i = 0; i < HalfLength; ++i) {
                int CachePos = i * StepMul;
                float OddReal = Odd[i].Real * Cache.Cos[CachePos] - Odd[i].Imaginary * Cache.Sin[CachePos],
                    OddImag = Odd[i].Real * Cache.Sin[CachePos] + Odd[i].Imaginary * Cache.Cos[CachePos];
                float Real = Even[i].Real + OddReal, Imaginary = Even[i].Imaginary + OddImag;
                Samples[i] = Mathf.Sqrt(Real * Real + Imaginary * Imaginary);
                Real = Even[i].Real - OddReal;
                Imaginary = Even[i].Imaginary - OddImag;
                Samples[i + HalfLength] = Mathf.Sqrt(Real * Real + Imaginary * Imaginary);
            }
        }

        /// <summary>Fast Fourier transform a 2D signal.</summary>
        public static Complex[] FFT(Complex[] Samples, FFTCache Cache = null) {
            Samples = (Complex[])Samples.Clone();
            ProcessFFT(Samples, Cache ?? new FFTCache(Samples.Length), CavernUtilities.Log2(Samples.Length) - 1);
            return Samples;
        }

        /// <summary>Fast Fourier transform a 1D signal.</summary>
        public static Complex[] FFT(float[] Samples, FFTCache Cache = null) {
            int Length = Samples.Length;
            Complex[] ComplexSignal = new Complex[Length];
            for (int i = 0; i < Length; ++i)
                ComplexSignal[i].Real = Samples[i];
            ProcessFFT(ComplexSignal, Cache ?? new FFTCache(Samples.Length), CavernUtilities.Log2(Length) - 1);
            return ComplexSignal;
        }

        /// <summary>Fast Fourier transform a 2D signal while keeping the source array allocation.</summary>
        public static void InPlaceFFT(Complex[] Samples, FFTCache Cache = null) =>
            ProcessFFT(Samples, Cache ?? new FFTCache(Samples.Length), CavernUtilities.Log2(Samples.Length) - 1);

        /// <summary>Spectrum of a signal's FFT.</summary>
        public static float[] FFT1D(float[] Samples, FFTCache Cache = null) {
            Samples = (float[])Samples.Clone();
            ProcessFFT(Samples, Cache ?? new FFTCache(Samples.Length));
            return Samples;
        }

        /// <summary>Spectrum of a signal's FFT while keeping the source array allocation.</summary>
        public static void InPlaceFFT(float[] Samples, FFTCache Cache = null) => ProcessFFT(Samples, Cache ?? new FFTCache(Samples.Length));

        /// <summary>Outputs IFFT(X) * N.</summary>
        static void ProcessIFFT(Complex[] Samples, FFTCache Cache, int Depth) {
            int Length = Samples.Length, HalfLength = Length / 2;
            if (Length == 1)
                return;
            Complex[] Even = FFTCache.Even[Depth], Odd = FFTCache.Odd[Depth];
            for (int Sample = 0, Pair = 0; Sample < HalfLength; ++Sample, Pair += 2) {
                Even[Sample] = Samples[Pair];
                Odd[Sample] = Samples[Pair + 1];
            }
            ProcessIFFT(Even, Cache, --Depth);
            ProcessIFFT(Odd, Cache, Depth);
            int StepMul = Cache.Cos.Length / HalfLength;
            for (int i = 0; i < HalfLength; ++i) {
                int CachePos = i * StepMul;
                float OddReal = Odd[i].Real * Cache.Cos[CachePos] - Odd[i].Imaginary * -Cache.Sin[CachePos],
                    OddImag = Odd[i].Real * -Cache.Sin[CachePos] + Odd[i].Imaginary * Cache.Cos[CachePos];
                Samples[i].Real = Even[i].Real + OddReal;
                Samples[i].Imaginary = Even[i].Imaginary + OddImag;
                int o = i + HalfLength;
                Samples[o].Real = Even[i].Real - OddReal;
                Samples[o].Imaginary = Even[i].Imaginary - OddImag;
            }
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal.</summary>
        public static Complex[] IFFT(Complex[] Samples, FFTCache Cache = null) {
            Samples = (Complex[])Samples.Clone();
            InPlaceIFFT(Samples, Cache ?? new FFTCache(Samples.Length));
            return Samples;
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.</summary>
        public static void InPlaceIFFT(Complex[] Samples, FFTCache Cache) {
            int Length = Samples.Length;
            ProcessIFFT(Samples, Cache, CavernUtilities.Log2(Length) - 1);
            float Multiplier = 1f / Length;
            for (int i = 0; i < Length; ++i) {
                Samples[i].Real *= Multiplier;
                Samples[i].Imaginary *= Multiplier;
            }
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
        public static Complex[] GetFrequencyResponse(Complex[] ReferenceFFT, Complex[] ResponseFFT) {
            for (int Sample = 0, Length = ResponseFFT.Length; Sample < Length; ++Sample)
                ResponseFFT[Sample].Divide(ref ReferenceFFT[Sample]);
            return ResponseFFT;
        }

        /// <summary>Get the frequency response using the original sweep signal's FFT as reference.</summary>
        public static Complex[] GetFrequencyResponse(Complex[] ReferenceFFT, float[] Response, FFTCache Cache = null) =>
            GetFrequencyResponse(ReferenceFFT, FFT(Response, Cache));

        /// <summary>Get the frequency response using the original sweep signal as reference.</summary>
        public static Complex[] GetFrequencyResponse(float[] Reference, float[] Response, FFTCache Cache = null) =>
            GetFrequencyResponse(FFT(Reference, Cache), Response);

        /// <summary>Get the complex impulse response using a precalculated frequency response.</summary>
        public static Complex[] GetImpulseResponse(Complex[] FrequencyResponse, FFTCache Cache = null) => IFFT(FrequencyResponse, Cache);

        /// <summary>Get the complex impulse response using the original sweep signal as a reference.</summary>
        public static Complex[] GetImpulseResponse(float[] Reference, float[] Response, FFTCache Cache = null) =>
            IFFT(GetFrequencyResponse(Reference, Response), Cache);

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
            int Length = Samples.Length, WindowSize = (int)(Length * Octave / OctaveRange), LastBlock = Length - WindowSize;
            float[] Smoothed = new float[Length--];
            float Average = 0;
            for (int Sample = 0; Sample < WindowSize; ++Sample)
                Average += Samples[Sample];
            for (int Sample = 0; Sample < WindowSize; ++Sample) {
                int NewSample = Sample + WindowSize;
                Average += Samples[NewSample];
                Smoothed[Sample] = Average / NewSample;
            }
            for (int Sample = WindowSize; Sample < LastBlock; ++Sample) {
                int OldSample = Sample - WindowSize, NewSample = Sample + WindowSize;
                Average += Samples[NewSample] - Samples[OldSample];
                Smoothed[Sample] = Average / (NewSample - OldSample);
            }
            for (int Sample = LastBlock; Sample <= Length; ++Sample) {
                int OldSample = Sample - WindowSize;
                Average -= Samples[OldSample];
                Smoothed[Sample] = Average / (Length - OldSample);
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
    }
}