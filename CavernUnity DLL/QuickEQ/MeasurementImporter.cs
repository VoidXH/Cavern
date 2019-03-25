using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Status indicator for <see cref="MeasurementImporter"/>.</summary>
    public enum MeasurementImporterStatus {
        /// <summary>Finding measured channels in the source recording.</summary>
        Preprocessing,
        /// <summary>Getting frequency and impulse response for each channel.</summary>
        Processing,
        /// <summary>The import was finished and the related <see cref="SpeakerSweeper"/> instance was set up.</summary>
        Done
    }

    /// <summary>Imports a QuickEQ measurement in the background.</summary>
    public class MeasurementImporter {
        /// <summary>Import process status.</summary>
        public MeasurementImporterStatus Status { get; private set; } = MeasurementImporterStatus.Preprocessing;
        /// <summary>The currently processed channel. Available if the <see cref="Status"/> indicates processing.</summary>
        public int ProcessedChannel { get; private set; }
        /// <summary>Total channels found in the measurement. Available if the <see cref="Status"/> indicates processing.</summary>
        public int Channels { get; private set; }

        /// <summary>RMS level calculation interval.</summary>
        const int BlockSize = 2048;
        /// <summary>The largest length in samples not to consider noise.</summary>
        const int ScrapSilence = 8192;

        /// <summary>Single-channel microphone recording of a QuickEQ measurement.</summary>
        readonly float[] Data;
        /// <summary>Sweeper instance to put the results in.</summary>
        readonly SpeakerSweeper Sweeper;
        /// <summary>The task processing <see cref="Data"/>.</summary>
        readonly Task Runner;

        /// <summary>Start importing a previous measurement. Status can be tracked in <see cref="Status"/>.</summary>
        /// <param name="Samples">Single-channel microphone recording of a QuickEQ measurement</param>
        /// <param name="Sweeper">Sweeper instance to put the results in</param>
        public MeasurementImporter(float[] Samples, SpeakerSweeper Sweeper) {
            Data = Samples;
            this.Sweeper = Sweeper;
            Sweeper.ImpResponses = new VerboseImpulseResponse[0];
            Sweeper.ResultAvailable = false;
            Runner = new Task(Process);
            Runner.Start();
        }

        /// <summary>An edge in a signal.</summary>
        struct Ramp {
            /// <summary>Marks a rising edge.</summary>
            public bool Rising;
            public int Position;

            public Ramp(bool Rising, int Position) {
                this.Rising = Rising;
                this.Position = Position;
            }
        }

        /// <summary>Get RMS values in blocks the size of <see cref="BlockSize"/>.</summary>
        static float[] GetRMSBlocks(float[] Data) {
            int Blocks = Data.Length / BlockSize;
            float[] RMSs = new float[Blocks];
            for (int Block = 0; Block < Blocks; ++Block) {
                float RMSHere = 0;
                for (int Pos = Block * BlockSize, End = Pos + BlockSize; Pos < End; ++Pos)
                    RMSHere += Data[Pos] * Data[Pos];
                RMSs[Block] = Mathf.Sqrt(RMSHere / BlockSize);
            }
            return RMSs;
        }

        /// <summary>Guess the noise level by putting it 3 decibels above the lowest non-zero RMS block or at zero if many blocks are zero.</summary>
        static float GetNoiseLevel(float[] RMSBlocks) {
            int ZeroBlocks = 0;
            float PeakNoise = float.PositiveInfinity;
            for (int Block = 0, Blocks = RMSBlocks.Length; Block < Blocks; ++Block)
                if (RMSBlocks[Block] == 0)
                    ++ZeroBlocks;
                else if (PeakNoise > RMSBlocks[Block])
                    PeakNoise = RMSBlocks[Block];
            return ZeroBlocks < RMSBlocks.Length / 5 ? PeakNoise * 10 : 0;
        }

        /// <summary>Find edges (jumps between low level and high level or noise and signal).</summary>
        /// <param name="Samples">Signal to find edges in</param>
        /// <param name="HighLevel">Signal level considered high level</param>
        static List<Ramp> GetRamps(float[] Samples, float HighLevel) {
            List<Ramp> Ramps = new List<Ramp>();
            bool LastRising = false;
            for (int Sample = 0, TotalSamples = Samples.Length; Sample < TotalSamples; ++Sample) {
                if (Samples[Sample] <= HighLevel) {
                    if (LastRising)
                        Ramps.Add(new Ramp(LastRising = false, Sample * BlockSize));
                } else if (!LastRising)
                    Ramps.Add(new Ramp(LastRising = true, Sample * BlockSize));
            }
            // Remove wrongly detected (too short) ramps
            bool[] ToRemove = new bool[Ramps.Count];
            for (int i = 1, c = Ramps.Count; i < c; i += 2)
                if (Ramps[i].Rising && Ramps[i].Position - Ramps[i - 1].Position < ScrapSilence)
                    ToRemove[i] = ToRemove[i - 1] = true;
            for (int i = Ramps.Count - 1; i >= 0; --i)
                if (ToRemove[i])
                    Ramps.RemoveAt(i);
            return Ramps;
        }

        /// <summary>Based on distances between ramps, guess the FFT size of the measurement.</summary>
        static int GetFFTSize(List<Ramp> Ramps) {
            int PeakRampDist = 0, MainRampDist = 0; // The LFE measurement may be the highest distance, so we're looking for the second highest
            for (int Channel = 1, c = Ramps.Count; Channel < c; ++Channel) {
                int RampDist = Ramps[Channel].Position - Ramps[Channel - 1].Position;
                if (PeakRampDist < RampDist) {
                    MainRampDist = PeakRampDist;
                    PeakRampDist = RampDist;
                } else if (MainRampDist < RampDist)
                    MainRampDist = RampDist;
            }
            return 1 << Mathf.FloorToInt(Mathf.Log(MainRampDist, 2)); // The gap will always be larger than the FFT size as no response is perfect
        }

        /// <summary>Process the <see cref="Data"/> and set up the <see cref="Sweeper"/>.</summary>
        void Process() {
            float[] RMSs = GetRMSBlocks(Data);
            List<Ramp> Ramps = GetRamps(RMSs, GetNoiseLevel(RMSs));
            int FFTSize = GetFFTSize(Ramps);
            int SamplesPerCh = FFTSize << 1;
            int Offset = Math.Max(Ramps[0].Position - FFTSize / 2 - BlockSize, 0);
            Channels = (Data.Length - Offset) / SamplesPerCh;
            int Length = Channels * SamplesPerCh; // TODO: find the end of the signal
            Offset = CavernUtilities.Clamp(Offset, 0, Data.Length - Length);
            Sweeper.SweepLength = FFTSize;
            Sweeper.RegenerateSweep();
            Sweeper.ExcitementResponses = new float[Channels][];
            Sweeper.FreqResponses = new float[Channels][];
            Sweeper.ImpResponses = new VerboseImpulseResponse[Channels];
            Status = MeasurementImporterStatus.Processing;
            for (; ProcessedChannel < Channels; ++ProcessedChannel) {
                float[] Samples = new float[SamplesPerCh];
                int ChannelStart = Offset + ProcessedChannel * SamplesPerCh;
                for (int Sample = 0; Sample < SamplesPerCh; ++Sample)
                    Samples[Sample] = Data[ChannelStart + Sample];
                Sweeper.ExcitementResponses[ProcessedChannel] = Samples;
                Complex[] RawResponse = Sweeper.GetFrequencyResponse(Samples);
                Sweeper.FreqResponses[ProcessedChannel] = Measurements.GetSpectrum(RawResponse);
                Sweeper.ImpResponses[ProcessedChannel] = Sweeper.GetImpulseResponse(RawResponse);
            }
            // Finalize
            Sweeper.ResultAvailable = true;
            Status = MeasurementImporterStatus.Done;
        }
    }
}
