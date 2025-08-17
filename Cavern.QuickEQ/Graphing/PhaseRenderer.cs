using System;
using System.Linq;

using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Displays phase responses of multiple channels. Works in degrees.
    /// </summary>
    public class PhaseRenderer : GraphRenderer {
        /// <summary>
        /// Number of logarithmically spaced sampling points on the parsed phase curves. This is only used when parsing new curves.
        /// </summary>
        public int Resolution { get; set; } = 512;

        /// <summary>
        /// Displays phase responses of multiple channels. Works in degrees.
        /// </summary>
        public PhaseRenderer(int width, int height) : base(width, height) {
            DynamicRange = 360;
            Peak = 180;
        }

        /// <summary>
        /// Parse phase curves from previously calculated impulse responses.
        /// </summary>
        /// <param name="sampleRate">Measurement sampling rate</param>
        /// <param name="parallel">Multithreaded parsing</param>
        /// <param name="entries">Impluse responses and the colors used to draw their phase on the graph</param>
        public void AddPhases(int sampleRate, bool parallel, params (float[] impulseResponse, uint color)[] entries) {
            if (entries.Length == 0) {
                return;
            }

            (Complex[] transferFunction, uint color)[] parsed = new (Complex[], uint)[entries.Length];
            int fftSize = entries.Max(x => x.impulseResponse.Length);
            if (parallel) {
                using FFTCachePool pool = new FFTCachePool(fftSize);
                Parallelizer.For(0, entries.Length, i => {
                    FFTCache cache = pool.Lease();
                    Complex[] transferFunction = Measurements.FFT(entries[i].impulseResponse, cache);
                    parsed[i] = (transferFunction, entries[i].color);
                    pool.Return(cache);
                });
            } else {
                using FFTCache cache = new FFTCache(fftSize);
                for (int i = 0; i < entries.Length; i++) {
                    Complex[] transferFunction = Measurements.FFT(entries[i].impulseResponse, cache);
                    parsed[i] = (transferFunction, entries[i].color);
                }
            }
            AddPhases(sampleRate, parsed);
        }

        /// <summary>
        /// Parse phase curves from previously calculated transfer functions.
        /// </summary>
        public void AddPhases(int sampleRate, params (Complex[] transferFunction, uint color)[] entries) {
            (Equalizer, uint)[] additions = new (Equalizer, uint)[entries.Length];
            for (int i = 0; i < entries.Length; i++) {
                additions[i] = (TransferFunctionToPhaseEqualizer(entries[i].transferFunction, sampleRate), entries[i].color);
            }
            AddCurves(additions, false);
        }

        /// <summary>
        /// With the rendering settings, parse phase curves for display.
        /// </summary>
        /// <remarks>Downsampling must not happen externally as that might create unwrapping errors.</remarks>
        Equalizer TransferFunctionToPhaseEqualizer(Complex[] source, int sampleRate) {
            float[] phase = Measurements.GetPhase(source);
            WaveformUtils.Gain(phase, 180 / MathF.PI);
            Equalizer result = EQGenerator.FromCurve(phase, sampleRate);
            result.DownsampleLogarithmically(Resolution, StartFrequency, EndFrequency);
            return result;
        }
    }
}
