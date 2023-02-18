using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cavern.QuickEQ {
    /// <summary>
    /// Status indicator for <see cref="MeasurementImporter"/>.
    /// </summary>
    public enum MeasurementImporterStatus {
        /// <summary>
        /// Finding measured channels in the source recording.
        /// </summary>
        Preprocessing,
        /// <summary>
        /// Getting frequency and impulse response for each channel.
        /// </summary>
        Processing,
        /// <summary>
        /// The import was finished and the related <see cref="SpeakerSweeper"/> instance was set up.
        /// </summary>
        Done
    }

    /// <summary>
    /// Imports a QuickEQ measurement in the background.
    /// </summary>
    public class MeasurementImporter : IDisposable {
        /// <summary>
        /// Import process status.
        /// </summary>
        public MeasurementImporterStatus Status { get; private set; } = MeasurementImporterStatus.Preprocessing;

        /// <summary>
        /// The currently processed channel. Available if the <see cref="Status"/> indicates processing.
        /// </summary>
        public int ProcessedChannel { get; private set; }

        /// <summary>
        /// Total channels found in the measurement. Available if the <see cref="Status"/> indicates processing.
        /// </summary>
        public int Channels { get; private set; }

        /// <summary>
        /// True if this imported measurement contained multiple microphone locations.
        /// </summary>
        public bool MultiMeasurement { get; private set; }

        /// <summary>
        /// Signature used for <see cref="OnMeasurement"/>.
        /// </summary>
        public delegate void OnMeasurementDelegate(int measurement, int measurements);

        /// <summary>
        /// Called when a single measurement was imported successfully.
        /// </summary>
        public event OnMeasurementDelegate OnMeasurement;

        /// <summary>
        /// RMS level calculation interval.
        /// </summary>
        const int blockSize = 2048;

        /// <summary>
        /// The largest length in samples not to consider noise.
        /// </summary>
        const int scrapSilence = 8192;

        /// <summary>
        /// Recording of a QuickEQ measurement. If single-channel, it's a microphone recording of a QuickEQ measurement.
        /// If multichannel, it's an exported measurement from Cavern.
        /// </summary>
        readonly float[][] data;

        /// <summary>
        /// Sweeper instance to put the results in.
        /// </summary>
        readonly SpeakerSweeper sweeper;

        /// <summary>
        /// The task processing <see cref="data"/>.
        /// </summary>
        readonly Task runner;

        /// <summary>
        /// Start importing a previous measurement. Status can be tracked in <see cref="Status"/>.
        /// </summary>
        /// <param name="samples">Single-channel microphone recording of a QuickEQ measurement</param>
        /// <param name="sampleRate">Sample rate of <paramref name="samples"/></param>
        /// <param name="sweeper">Sweeper instance to put the results in</param>
        public MeasurementImporter(float[][] samples, int sampleRate, SpeakerSweeper sweeper) {
            data = samples;
            this.sweeper = sweeper;
            sweeper.SampleRate = sampleRate;
            sweeper.ImpResponses = new VerboseImpulseResponse[0];
            runner = new Task(Process);
            runner.Start();
        }

        /// <summary>
        /// An edge in a signal.
        /// </summary>
        struct Ramp {
            /// <summary>
            /// Marks a rising edge.
            /// </summary>
            public bool rising;
            public int position;

            /// <summary>
            /// An edge in a signal.
            /// </summary>
            public Ramp(bool rising, int position) {
                this.rising = rising;
                this.position = position;
            }
        }

        /// <summary>
        /// Get RMS values in blocks the size of <see cref="blockSize"/>.
        /// </summary>
        static float[] GetRMSBlocks(float[] data) {
            int blocks = data.Length / blockSize;
            float[] RMSs = new float[blocks];
            for (int block = 0; block < blocks; ++block) {
                float RMSHere = 0;
                for (int pos = block * blockSize, end = pos + blockSize; pos < end; ++pos) {
                    RMSHere += data[pos] * data[pos];
                }
                RMSs[block] = (float)Math.Sqrt(RMSHere / blockSize);
            }
            --blocks;
            for (int block = 1; block < blocks; ++block) { // Ghetto smoothing
                RMSs[block] = (RMSs[block - 1] + RMSs[block] + RMSs[block + 1]) * .33f;
            }
            return RMSs;
        }

        /// <summary>
        /// Guess the noise level by putting it 3 decibels above the lowest non-zero RMS block or at zero if many blocks are zero.
        /// </summary>
        static float GetNoiseLevel(float[] rmsBlocks) {
            int zeroBlocks = 0;
            float average = 0, peak = float.PositiveInfinity;
            for (int block = 0; block < rmsBlocks.Length; ++block) {
                if (rmsBlocks[block] == 0) {
                    ++zeroBlocks;
                } else {
                    if (peak > rmsBlocks[block]) {
                        peak = rmsBlocks[block];
                    }
                    average += rmsBlocks[block];
                }
            }
            return zeroBlocks < rmsBlocks.Length / 5 ? (peak + average / rmsBlocks.Length) * .5f : 0;
        }

        /// <summary>
        /// Find edges (jumps between low level and high level or noise and signal).
        /// </summary>
        /// <param name="samples">Signal to find edges in</param>
        /// <param name="highLevel">Signal level considered high level</param>
        static List<Ramp> GetRamps(float[] samples, float highLevel) {
            List<Ramp> ramps = new List<Ramp>();
            bool lastRising = false;
            for (int sample = 0; sample < samples.Length; ++sample) {
                if (samples[sample] <= highLevel) {
                    if (lastRising) {
                        ramps.Add(new Ramp(lastRising = false, sample * blockSize));
                    }
                } else if (!lastRising) {
                    ramps.Add(new Ramp(lastRising = true, sample * blockSize));
                }
            }

            // Remove wrongly detected (too short) ramps
            bool[] toRemove = new bool[ramps.Count];
            for (int ramp = 1; ramp < ramps.Count; ramp += 2) {
                if (ramps[ramp].rising && ramps[ramp].position - ramps[ramp - 1].position < scrapSilence) {
                    toRemove[ramp] = toRemove[ramp - 1] = true;
                }
            }
            for (int ramp = ramps.Count - 1; ramp >= 0; --ramp) {
                if (toRemove[ramp]) {
                    ramps.RemoveAt(ramp);
                }
            }
            return ramps;
        }

        /// <summary>
        /// Based on distances between ramps, guess the FFT size of the measurement.
        /// </summary>
        static int GetFFTSize(List<Ramp> ramps) {
            int peakRampDist = 0,
                mainRampDist = 0; // The LFE measurement may be the highest distance, so we're looking for the second highest
            for (int ramp = 1; ramp < ramps.Count; ++ramp) {
                int rampDist = ramps[ramp].position - ramps[ramp - 1].position;
                if (peakRampDist < rampDist) {
                    mainRampDist = peakRampDist;
                    peakRampDist = rampDist;
                } else if (mainRampDist < rampDist) {
                    mainRampDist = rampDist;
                }
            }
            return 1 << (int)Math.Log(mainRampDist, 2); // The gap will always be larger than the FFT size as no response is perfect
        }

        /// <summary>
        /// Process a microphone recording, extract the QuickEQ measurement from it.
        /// </summary>
        void ProcessRecording(float[] data) {
            float[] RMSs = GetRMSBlocks(data);
            List<Ramp> ramps = GetRamps(RMSs, GetNoiseLevel(RMSs));
            int FFTSize = GetFFTSize(ramps),
                samplesPerCh = FFTSize * 2;
            int offset = Math.Max(ramps[0].position - FFTSize / 2 - blockSize, 0),
                end = Math.Min(ramps[^1].position + FFTSize, data.Length);
            Channels = (end - offset) / samplesPerCh;
            offset = Math.Clamp(offset, 0, data.Length - Channels * samplesPerCh);

            sweeper.OverwriteSweeper(Channels, FFTSize);
            Status = MeasurementImporterStatus.Processing;
            for (; ProcessedChannel < Channels; ++ProcessedChannel) {
                float[] samples = new float[samplesPerCh];
                Array.Copy(data, offset + ProcessedChannel * samplesPerCh, samples, 0, samplesPerCh);
                sweeper.OverwriteChannel(ProcessedChannel, samples);
            }
            sweeper.ResultAvailable = true;
        }

        /// <summary>
        /// Process a QuickEQ export, simply import the recorded data.
        /// </summary>
        void ProcessExport() {
            Channels = 0;
            int lastSample = data[0].Length - 1;
            while (data[0][lastSample] == 0 && lastSample > 0) {
                --lastSample;
            }
            for (int channel = 1; channel < data.Length; ++channel) {
                int firstSample = 0;
                while (data[channel][firstSample] == 0 && firstSample < lastSample) {
                    ++firstSample;
                }
                if (firstSample < lastSample) {
                    Channels = channel;
                    break;
                }
            }
            if (Channels == 0) {
                Channels = data.Length;
            }
            if (Channels != data.Length) {
                MultiMeasurement = true;
            }

            int measurements = data.Length / Channels,
                samplesPerCh = data[0].Length / Channels;
            Status = MeasurementImporterStatus.Processing;
            for (int measurement = 0; measurement < measurements; ++measurement) {
                sweeper.OverwriteSweeper(Channels, samplesPerCh >> 1);
                for (ProcessedChannel = 0; ProcessedChannel < Channels; ++ProcessedChannel) {
                    float[] samples = new float[samplesPerCh];
                    Array.Copy(data[ProcessedChannel + measurement * Channels],
                        samplesPerCh * ProcessedChannel, samples, 0, samplesPerCh);
                    sweeper.OverwriteChannel(ProcessedChannel, samples);
                }
                sweeper.ResultAvailable = true;
                OnMeasurement?.Invoke(measurement, measurements);
            }
        }

        /// <summary>
        /// Process the <see cref="data"/> and set up the <see cref="sweeper"/>.
        /// </summary>
        void Process() {
            if (data.Length == 1) {
                ProcessRecording(data[0]);
            } else {
                ProcessExport();
            }
            Status = MeasurementImporterStatus.Done;
        }

        /// <summary>
        /// Free the resources used by the importer.
        /// </summary>
        public void Dispose() => runner?.Dispose();
    }
}