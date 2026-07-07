using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.Measurement;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;
using Cavern.Utilities.Threading;

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
        /// Window the impulse to +/- this many samples around the found peak before getting its phase curve. A 0 value disables this feature.
        /// </summary>
        public int Windowing { get; set; } = 64;

        /// <summary>
        /// Method to remove the delay's effect from the displayed phase measurements.
        /// </summary>
        public DelayDeterminationType DelayCompensation = DelayDeterminationType.ImpulsePeak;

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

            PhaseRendererTransferEntry[] parsed = new PhaseRendererTransferEntry[entries.Length];
            int fftSize = entries.Max(x => x.impulseResponse.Length);
            using FFTCachePool pool = new FFTCachePool(fftSize);
            Parallelizer.For(0, entries.Length, i => {
                FFTCache cache = pool.Lease();
                parsed[i] = new PhaseRendererTransferEntry(entries[i], cache);
                pool.Return(cache);
            }, parallel);
            AddPhases(sampleRate, parsed);
        }

        /// <summary>
        /// Parse phase curves from previously calculated transfer functions.
        /// </summary>
        public void AddPhases(int sampleRate, params PhaseRendererTransferEntry[] entries) {
            (Equalizer, uint)[] additions = new (Equalizer, uint)[entries.Length];
            for (int i = 0; i < entries.Length; i++) {
                additions[i] = (TransferFunctionToPhaseEqualizer(entries[i].transferFunction, entries[i].endFrequency, sampleRate), entries[i].color);
            }
            AddCurves(additions, false);
        }

        /// <summary>
        /// Add regular phase curves for display.
        /// </summary>
        /// <param name="sampleRate">Measurement sampling rate</param>
        /// <param name="entries">Phase curves (in degrees) and their end frequencies and colors</param>
        public void AddPhases(int sampleRate, params (float[] phase, uint color)[] entries) {
            (Equalizer, uint)[] additions = new (Equalizer, uint)[entries.Length];
            for (int i = 0; i < entries.Length; i++) {
                Equalizer eq = EQGenerator.FromCurve(entries[i].phase, sampleRate);
                eq.DownsampleLogarithmically(Resolution, StartFrequency, EndFrequency);
                additions[i] = (eq, entries[i].color);
            }
            AddCurves(additions, false);
        }

        /// <summary>
        /// Set up the graph so the phase curves fill the Y axis to at least on of its edges.
        /// </summary>
        public override void Normalize() {
            double newPeak = 0;
            for (int i = 0, c = Curves.Count; i < c; i++) {
                IReadOnlyList<Band> curve = Curves[i].Curve.Bands;
                for (int f = 0, c2 = curve.Count; f < c2; f++) {
                    double absPeak = Math.Abs(curve[f].Gain);
                    if (newPeak < absPeak) {
                        newPeak = absPeak;
                    }
                }
            }
            Peak = (float)newPeak;
            DynamicRange = 2 * Peak;
            ReRenderFull();
        }

        /// <summary>
        /// With the rendering settings, parse phase curves for display.
        /// </summary>
        /// <remarks>Downsampling must not happen externally as that might create unwrapping errors.</remarks>
        Equalizer TransferFunctionToPhaseEqualizer(Complex[] source, float endFrequency, int sampleRate) {
            float[] phase = PhaseDelayCompensation.GetUndelayedPhase(source, DelayCompensation, Unwrap, Windowing);
            WaveformUtils.Gain(phase, 180 / MathF.PI);
            Equalizer result = EQGenerator.FromCurve(phase, sampleRate);
            result.DownsampleLogarithmically(Resolution, StartFrequency, endFrequency);
            return result;
        }
    }
}
