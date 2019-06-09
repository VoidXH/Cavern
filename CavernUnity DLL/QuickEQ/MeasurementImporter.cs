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
        const int blockSize = 2048;
        /// <summary>The largest length in samples not to consider noise.</summary>
        const int scrapSilence = 8192;

        /// <summary>Single-channel microphone recording of a QuickEQ measurement.</summary>
        readonly float[] data;
        /// <summary>Sweeper instance to put the results in.</summary>
        readonly SpeakerSweeper sweeper;
        /// <summary>The task processing <see cref="data"/>.</summary>
        readonly Task runner;

        /// <summary>Start importing a previous measurement. Status can be tracked in <see cref="Status"/>.</summary>
        /// <param name="samples">Single-channel microphone recording of a QuickEQ measurement</param>
        /// <param name="sweeper">Sweeper instance to put the results in</param>
        public MeasurementImporter(float[] samples, SpeakerSweeper sweeper) {
            data = samples;
            this.sweeper = sweeper;
            sweeper.ImpResponses = new VerboseImpulseResponse[0];
            sweeper.ResultAvailable = false;
            runner = new Task(Process);
            runner.Start();
        }

        /// <summary>An edge in a signal.</summary>
        struct Ramp {
            /// <summary>Marks a rising edge.</summary>
            public bool Rising;
            public int Position;

            public Ramp(bool rising, int position) {
                Rising = rising;
                Position = position;
            }
        }

        /// <summary>Get RMS values in blocks the size of <see cref="blockSize"/>.</summary>
        static float[] GetRMSBlocks(float[] data) {
            int blocks = data.Length / blockSize;
            float[] RMSs = new float[blocks];
            for (int block = 0; block < blocks; ++block) {
                float RMSHere = 0;
                for (int pos = block * blockSize, end = pos + blockSize; pos < end; ++pos)
                    RMSHere += data[pos] * data[pos];
                RMSs[block] = Mathf.Sqrt(RMSHere / blockSize);
            }
            return RMSs;
        }

        /// <summary>Guess the noise level by putting it 3 decibels above the lowest non-zero RMS block or at zero if many blocks are zero.</summary>
        static float GetNoiseLevel(float[] RMSBlocks) {
            int zeroBlocks = 0;
            float peakNoise = float.PositiveInfinity;
            for (int block = 0, blocks = RMSBlocks.Length; block < blocks; ++block)
                if (RMSBlocks[block] == 0)
                    ++zeroBlocks;
                else if (peakNoise > RMSBlocks[block])
                    peakNoise = RMSBlocks[block];
            return zeroBlocks < RMSBlocks.Length / 5 ? peakNoise * 10 : 0;
        }

        /// <summary>Find edges (jumps between low level and high level or noise and signal).</summary>
        /// <param name="samples">Signal to find edges in</param>
        /// <param name="highLevel">Signal level considered high level</param>
        static List<Ramp> GetRamps(float[] samples, float highLevel) {
            List<Ramp> ramps = new List<Ramp>();
            bool lastRising = false;
            for (int sample = 0, totalSamples = samples.Length; sample < totalSamples; ++sample) {
                if (samples[sample] <= highLevel) {
                    if (lastRising)
                        ramps.Add(new Ramp(lastRising = false, sample * blockSize));
                } else if (!lastRising)
                    ramps.Add(new Ramp(lastRising = true, sample * blockSize));
            }
            // Remove wrongly detected (too short) ramps
            bool[] toRemove = new bool[ramps.Count];
            for (int ramp = 1, end = ramps.Count; ramp < end; ramp += 2)
                if (ramps[ramp].Rising && ramps[ramp].Position - ramps[ramp - 1].Position < scrapSilence)
                    toRemove[ramp] = toRemove[ramp - 1] = true;
            for (int ramp = ramps.Count - 1; ramp >= 0; --ramp)
                if (toRemove[ramp])
                    ramps.RemoveAt(ramp);
            return ramps;
        }

        /// <summary>Based on distances between ramps, guess the FFT size of the measurement.</summary>
        static int GetFFTSize(List<Ramp> ramps) {
            int peakRampDist = 0, mainRampDist = 0; // The LFE measurement may be the highest distance, so we're looking for the second highest
            for (int ramp = 1, end = ramps.Count; ramp < end; ++ramp) {
                int rampDist = ramps[ramp].Position - ramps[ramp - 1].Position;
                if (peakRampDist < rampDist) {
                    mainRampDist = peakRampDist;
                    peakRampDist = rampDist;
                } else if (mainRampDist < rampDist)
                    mainRampDist = rampDist;
            }
            return 1 << Mathf.FloorToInt(Mathf.Log(mainRampDist, 2)); // The gap will always be larger than the FFT size as no response is perfect
        }

        /// <summary>Process the <see cref="data"/> and set up the <see cref="sweeper"/>.</summary>
        void Process() {
            float[] RMSs = GetRMSBlocks(data);
            List<Ramp> ramps = GetRamps(RMSs, GetNoiseLevel(RMSs));
            int FFTSize = GetFFTSize(ramps);
            int samplesPerCh = FFTSize << 1;
            int offset = Math.Max(ramps[0].Position - FFTSize / 2 - blockSize, 0);
            Channels = (data.Length - offset) / samplesPerCh;
            int length = Channels * samplesPerCh; // TODO: find the end of the signal
            offset = Utils.Clamp(offset, 0, data.Length - length);
            sweeper.SweepLength = FFTSize;
            sweeper.RegenerateSweep();
            sweeper.ExcitementResponses = new float[Channels][];
            sweeper.FreqResponses = new float[Channels][];
            sweeper.ImpResponses = new VerboseImpulseResponse[Channels];
            Status = MeasurementImporterStatus.Processing;
            for (; ProcessedChannel < Channels; ++ProcessedChannel) {
                float[] samples = new float[samplesPerCh];
                int channelStart = offset + ProcessedChannel * samplesPerCh;
                for (int sample = 0; sample < samplesPerCh; ++sample)
                    samples[sample] = data[channelStart + sample];
                sweeper.ExcitementResponses[ProcessedChannel] = samples;
                Complex[] RawResponse = sweeper.GetFrequencyResponse(samples);
                sweeper.FreqResponses[ProcessedChannel] = Measurements.GetSpectrum(RawResponse);
                sweeper.ImpResponses[ProcessedChannel] = sweeper.GetImpulseResponse(RawResponse);
            }
            // Finalize
            sweeper.ResultAvailable = true;
            Status = MeasurementImporterStatus.Done;
        }
    }
}
