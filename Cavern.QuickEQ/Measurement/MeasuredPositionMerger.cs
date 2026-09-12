using System;
using System.Collections.Generic;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// Calculate the average spectrum of each channel across multiple measurement points, which is a prerequisite of EQing.
    /// </summary>
    public sealed class MeasuredPositionMerger {
        /// <summary>
        /// Number of the measured channels.
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// The imported set of measurements.
        /// </summary>
        public IReadOnlyList<MeasuredPosition> MeasurementPoints { get; }

        /// <summary>
        /// Calibration file of the microphone to account for microphone spectral errors.
        /// </summary>
        public Equalizer MicCalibration { get; set; }

        /// <summary>
        /// Number of logarithmically spaced bands to have in the result between <see cref="MinFreq"/> and <see cref="MaxFreq"/>.
        /// </summary>
        public int BandCount { get; set; } = 512;

        /// <summary>
        /// Minimum frequency to have in the averaged results.
        /// </summary>
        public double MinFreq { get; set; } = 20;

        /// <summary>
        /// Maximum frequency to have in the averaged results.
        /// </summary>
        public double MaxFreq { get; set; } = 20000;

        /// <summary>
        /// Calculate the average spectrum of each channel across multiple measurement points, which is a prerequisite of EQing.
        /// </summary>
        /// <param name="results">Measurements made at all measured microphone position</param>
        public MeasuredPositionMerger(IReadOnlyList<MeasuredPosition> results) {
            Channels = results[0].FrequencyResponses.Length;
            for (int i = 1, c = results.Count; i < c; i++) {
                if (results[i].FrequencyResponses.Length != Channels) {
                    throw new ChannelCountMismatchException();
                }
            }
            MeasurementPoints = results;
        }

        /// <summary>
        /// Using the settings, calculate the average spectrum for each channel.
        /// </summary>
        /// <param name="averagingMode">How to average the measurements.</param>
        public Equalizer[] Merge(AveragingMode averagingMode = AveragingMode.FrequencyDomain) {
            Equalizer calibration = MicCalibration;
            if (calibration != null) {
                calibration = (Equalizer)calibration.Clone();
                calibration.DownsampleLogarithmically(BandCount, MinFreq, MaxFreq);
            }

            Equalizer[] result = new Equalizer[Channels];
            for (int i = 0; i < Channels; i++) {
                Equalizer[] channelEQs = MeasurementPoints.SelectArray(x => {
                    Equalizer y = (Equalizer)x.FrequencyResponses[i].Clone();
                    y.DownsampleLogarithmically(BandCount, MinFreq, MaxFreq);
                    if (calibration != null) {
                        y.AlignTo(calibration);
                    }
                    return y;
                });

                result[i] = averagingMode switch {
                    AveragingMode.FrequencyDomain => EQGenerator.Average(channelEQs),
                    AveragingMode.FrequencyDomainRMS => EQGenerator.AverageRMS(channelEQs),
                    _ => throw new NotImplementedException()
                };
            }
            return result;
        }
    }
}
