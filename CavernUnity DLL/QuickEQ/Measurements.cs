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

        /// <summary>Outputs IFFT(X) * N.</summary>
        static Complex[] ProcessIFFT(Complex[] Samples) {
            int Length = Samples.Length, HalfLength = Length / 2;
            if (Length == 1)
                return Samples;
            Complex[] Output = new Complex[Length], Even = new Complex[HalfLength], Odd = new Complex[HalfLength];
            for (int Sample = 0; Sample < HalfLength; ++Sample) {
                int Pair = Sample + Sample;
                Even[Sample] = Samples[Pair];
                Odd[Sample] = Samples[Pair + 1];
            }
            Complex[] EvenFFT = ProcessIFFT(Even), OddFFT = ProcessIFFT(Odd);
            for (int i = 0; i < HalfLength; ++i) {
                float Angle = Pix2 * i / Length;
                OddFFT[i] *= new Complex(Mathf.Cos(Angle), Mathf.Sin(Angle));
            }
            for (int i = 0; i < Length / 2; ++i) {
                Output[i] = EvenFFT[i] + OddFFT[i];
                Output[i + HalfLength] = EvenFFT[i] - OddFFT[i];
            }
            return Output;
        }

        /// <summary>Inverse Fast Fourier Transform of a transformed signal.</summary>
        public static Complex[] IFFT(Complex[] Samples) {
            Samples = ProcessIFFT(Samples);
            int N = Samples.Length;
            float Multiplier = 1f / N;
            for (int i = 0; i < N; ++i)
                Samples[i] *= Multiplier;
            return Samples;
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
                Output[Sample] = Mathf.Sin(Pix2 * (StartFreq * Position + Chirpyness * Position * Position));
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
                LogChirpyness = Mathf.Log(Chirpyness), SinConst = Pix2 * StartFreq;
            for (int Sample = 0; Sample < Samples; ++Sample)
                Output[Sample] =
                    Mathf.Sin(SinConst * (Mathf.Pow(Chirpyness, Sample / (float)SampleRate) - 1) / LogChirpyness);
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

        /// <summary>Get the complex frequency response using the original sweep signal as reference.</summary>
        public static Complex[] GetFrequencyResponse(float[] Reference, float[] Response) {
            Complex[] ReferenceFFT = FFT(Reference), ResponseFFT = FFT(Response);
            for (int Sample = 0, Length = ResponseFFT.Length; Sample < Length; ++Sample)
                ResponseFFT[Sample] /= ReferenceFFT[Sample];
            return ResponseFFT;
        }

        /// <summary>Get the absolute frequency response using the original sweep signal as reference.</summary>
        public static float[] GetFrequencyResponseAbs(float[] Reference, float[] Response) {
            float[] SourceResponse = GetSpectrum(FFT(Reference));
            float[] ResultResponse = GetSpectrum(FFT(Response));
            for (int Sample = 0, Length = SourceResponse.Length; Sample < Length; ++Sample)
                if (SourceResponse[Sample] != 0)
                    ResultResponse[Sample] /= SourceResponse[Sample];
                else
                    ResultResponse[Sample] = 1;
            return ResultResponse;
        }

        /// <summary>Get the complex impulse response using a precalculated frequency response.</summary>
        public static Complex[] GetImpulseResponse(Complex[] FrequencyResponse) {
            return IFFT(FrequencyResponse);
        }

        /// <summary>Get the complex impulse response using the original sweep signal as a reference.</summary>
        public static Complex[] GetImpulseResponse(float[] Reference, float[] Response) {
            return IFFT(GetFrequencyResponse(Reference, Response));
        }

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
        public static float[] ConvertToDecibels(float[] Curve) {
            Curve = (float[])Curve.Clone();
            for (int i = 0, End = Curve.Length; i < End; ++i)
                Curve[i] = 20 * Mathf.Log10(Curve[i]);
            return Curve;
        }

        /// <summary>Convert a response to logarithmically scaled cut frequency range.</summary>
        /// <param name="Samples">Source response</param>
        /// <param name="StartFreq">Frequency at the first position of the output</param>
        /// <param name="EndFreq">Frequency at the last position of the output</param>
        /// <param name="SampleRate">Sample rate of the measurement that generated the curve</param>
        /// <param name="ResultSize">Length of the resulting array</param>
        public static float[] ConvertToGraph(float[] Samples, float StartFreq, float EndFreq, int SampleRate, int ResultSize) {
            Samples = (float[])Samples.Clone();
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
            int WindowSize = (int)(Length * Octave / OctaveRange * .5f);
            float[] Smoothed = new float[Length--];
            for (int Sample = 0; Sample <= Length; ++Sample) {
                int Start = Sample - WindowSize, End = Sample + WindowSize;
                if (Start < 0)
                    Start = 0;
                if (End > Length)
                    End = Length;
                float Average = 0;
                for (int WindowSample = Start; WindowSample <= End; ++WindowSample)
                    Average += Samples[WindowSample];
                Smoothed[Sample] = Average / (End - Start);
            }
            return Smoothed;
        }

        /// <summary>Apply smoothing (in octaves) on a linear frequency response.</summary>
        public static float[] SmoothResponse(float[] Samples, float StartFreq, float EndFreq, float Octave = 1 / 3f) {
            if (Octave == 0)
                return (float[])Samples.Clone();
            int Length = Samples.Length;
            float[] Smoothed = new float[Length--];
            float Span = EndFreq - StartFreq, Offset = Mathf.Pow(2, Octave), Positioner = Length / Span, FreqAtSample = Span / Length;
            for (int Sample = 0; Sample <= Length; ++Sample) {
                float Freq = StartFreq + Sample * FreqAtSample, WindowStart = Freq / Offset, WindowEnd = Freq * Offset;
                int Start = (int)((WindowStart - StartFreq) * Positioner), End = (int)((WindowEnd - StartFreq) * Positioner);
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