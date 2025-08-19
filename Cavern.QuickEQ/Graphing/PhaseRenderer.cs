using System;
using System.Linq;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Measurement;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Displays phase responses of multiple channels. Works in degrees.
    /// </summary>
    public class PhaseRenderer : GraphRenderer {
        /// <inheritdoc/>
        public override DrawableMeasurementType Type => DrawableMeasurementType.Phase;

        /// <summary>
        /// Number of logarithmically spaced sampling points on the parsed phase curves. This is only used when parsing new curves.
        /// </summary>
        public int Resolution { get; set; } = 512;

        /// <summary>
        /// Try to recover the actual phase curve from the results that are confined to the unit circle.
        /// </summary>
        public bool Unwrap { get; set; } = true;

        /// <summary>
        /// Method to remove the delay's effect from the displayed phase measurements.
        /// </summary>
        public PhaseDelayCompensationType DelayCompensation = PhaseDelayCompensationType.ImpulsePeak;

        /// <summary>
        /// Detect phase properties until this band and no further, as high resolution artifacts might break linearization.
        /// </summary>
        public double DetectionFrequency { get; set; } = 2000;

        /// <summary>
        /// Displays phase responses of multiple channels. Works in degrees.
        /// </summary>
        public PhaseRenderer(int width, int height) : base(width, height) {
            DynamicRange = 7200;
            Peak = 3600;
        }

        /// <summary>
        /// Parse phase curves from previously calculated impulse responses.
        /// </summary>
        /// <param name="sampleRate">Measurement sampling rate</param>
        /// <param name="parallel">Multithreaded parsing</param>
        /// <param name="entries">Impluse responses and the colors used to draw their phase on the graph</param>
        public void AddPhases(int sampleRate, bool parallel, params PhaseRendererImpulseEntry[] entries) {
            if (entries.Length == 0) {
                return;
            }

            (Complex[] transferFunction, float speakerEndFrequency, uint color)[] parsed = new (Complex[], float, uint)[entries.Length];
            int fftSize = entries.Max(x => x.impulseResponse.Length);
            if (parallel) {
                using FFTCachePool pool = new FFTCachePool(fftSize);
                Parallelizer.For(0, entries.Length, i => {
                    FFTCache cache = pool.Lease();
                    Complex[] transferFunction = Measurements.FFT(entries[i].impulseResponse, cache);
                    parsed[i] = (transferFunction, entries[i].endFrequency, entries[i].color);
                    pool.Return(cache);
                });
            } else {
                using FFTCache cache = new FFTCache(fftSize);
                for (int i = 0; i < entries.Length; i++) {
                    Complex[] transferFunction = Measurements.FFT(entries[i].impulseResponse, cache);
                    parsed[i] = (transferFunction, entries[i].endFrequency, entries[i].color);
                }
            }
            AddPhases(sampleRate, parsed);
        }

        /// <summary>
        /// Parse phase curves from previously calculated transfer functions.
        /// </summary>
        public void AddPhases(int sampleRate, params (Complex[] transferFunction, float endFrequency, uint color)[] entries) {
            (Equalizer, uint)[] additions = new (Equalizer, uint)[entries.Length];
            for (int i = 0; i < entries.Length; i++) {
                additions[i] = (TransferFunctionToPhaseEqualizer(entries[i].transferFunction, entries[i].endFrequency, sampleRate), entries[i].color);
            }
            AddCurves(additions, false);
        }

        /// <summary>
        /// With the rendering settings, parse phase curves for display.
        /// </summary>
        /// <remarks>Downsampling must not happen externally as that might create unwrapping errors.</remarks>
        Equalizer TransferFunctionToPhaseEqualizer(Complex[] source, float endFrequency, int sampleRate) {
            float[] phase = PhaseDelayCompensation.CorrectDelay(source, sampleRate, DelayCompensation, StartFrequency, DetectionFrequency, endFrequency, Unwrap);
            WaveformUtils.Gain(phase, 180 / MathF.PI);
            Equalizer result = EQGenerator.FromCurve(phase, sampleRate);
            result.DownsampleLogarithmically(Resolution, StartFrequency, EndFrequency);
            return result;
        }
    }
}
