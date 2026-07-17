using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;

namespace Test.Cavern.QuickEQ.Measurement {
    /// <summary>
    /// Tests for the <see cref="PhaseAnalyzer"/> class, specifically the <see cref="float[]"/> overload of
    /// <see cref="PhaseAnalyzer.GetExcessPhase(float[], DelayDeterminationType)"/> that creates its own <see cref="FFTCache"/>.
    /// </summary>
    [TestClass]
    public class PhaseAnalyzer_Tests {
        /// <summary>
        /// Creates a Dirac delta impulse response with the impulse at <paramref name="offset"/> samples.
        /// </summary>
        static float[] DiracDelta(int length, int offset) {
            float[] result = new float[length];
            result[offset] = 1;
            return result;
        }

        /// <summary>
        /// Compares two phase values modulo 2*pi and asserts they are within <paramref name="delta"/>.
        /// </summary>
        static void AssertPhaseClose(float expected, float actual, float delta, string message) {
            float diff = (actual - expected) % (2 * MathF.PI);
            if (diff < -MathF.PI) {
                diff += 2 * MathF.PI;
            }
            if (diff > MathF.PI) {
                diff -= 2 * MathF.PI;
            }
            Assert.AreEqual(0, diff, delta, message);
        }

        /// <summary>
        /// A Dirac delta is a pure delay, which is already minimum phase. With delay compensation,
        /// the excess phase should be ~0 at every bin.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_DelayedDiracDelta_CompensatedIsZero() {
            int length = 256;
            int delay = 10;
            float[] impulseResponse = DiracDelta(length, delay);

            float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.Slope);

            Assert.AreEqual(length / 2, excessPhase.Length, "Excess phase should contain half the bins (the unique half-spectrum).");
            for (int i = 0; i < excessPhase.Length; i++) {
                Assert.AreEqual(0, excessPhase[i], 1e-3f, $"Compensated excess phase at bin {i} was not close to 0: {excessPhase[i]}");
            }
        }

        /// <summary>
        /// With <see cref="DelayDeterminationType.None"/>, the excess phase is computed against the
        /// uncompensated (linear) phase of the delay, which equals -2*pi*i*delay/length.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_DelayedDiracDelta_UncompensatedMatchesLinearPhase() {
            int length = 256;
            int delay = 10;
            float[] impulseResponse = DiracDelta(length, delay);

            float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.None);

            Assert.AreEqual(length / 2, excessPhase.Length);
            for (int i = 0; i < excessPhase.Length; i++) {
                float expectedPhase = -2 * MathF.PI * i * delay / length;
                AssertPhaseClose(expectedPhase, excessPhase[i], 1e-3f,
                    $"Uncompensated excess phase at bin {i} was {excessPhase[i]}, expected {expectedPhase}");
            }
        }

        /// <summary>
        /// A flat (all-pass, zero-phase) impulse response located at sample 0 has no excess phase,
        /// regardless of delay compensation type, since it is its own minimum phase.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_DiracDeltaAtZero_IsZero() {
            int length = 256;
            float[] impulseResponse = DiracDelta(length, 0);

            float[] compensated = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.Slope);
            float[] uncompensated = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.None);

            Assert.AreEqual(length / 2, compensated.Length);
            Assert.AreEqual(length / 2, uncompensated.Length);
            for (int i = 0; i < compensated.Length; i++) {
                Assert.AreEqual(0, compensated[i], 1e-3f, $"Compensated excess phase at bin {i} was not close to 0.");
                Assert.AreEqual(0, uncompensated[i], 1e-3f, $"Uncompensated excess phase at bin {i} was not close to 0.");
            }
        }

        /// <summary>
        /// The excess phase must be invariant to the absolute gain of the impulse response,
        /// because both the actual and minimum phase are normalized internally.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_IsGainInvariant() {
            int length = 256;
            int delay = 8;
            float[] baseImpulse = DiracDelta(length, delay);

            float[] reference = PhaseAnalyzer.GetExcessPhase(baseImpulse, DelayDeterminationType.Slope);

            float[] scaled = new float[length];
            for (int i = 0; i < length; i++) {
                scaled[i] = baseImpulse[i] * 3.7f;
            }
            float[] scaledExcess = PhaseAnalyzer.GetExcessPhase(scaled, DelayDeterminationType.Slope);

            Assert.AreEqual(reference.Length, scaledExcess.Length);
            for (int i = 0; i < reference.Length; i++) {
                Assert.AreEqual(reference[i], scaledExcess[i], 1e-4f, $"Excess phase at bin {i} changed with gain.");
            }
        }

        /// <summary>
        /// A non-minimum-phase (mixed-phase) impulse response should produce a non-zero excess phase,
        /// proving the algorithm detects phase deviations rather than always returning 0.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_MixedPhaseImpulse_IsNonZero() {
            int length = 256;
            float[] impulseResponse = new float[length];
            // Two separated Dirac deltas of opposite sign -> a non-minimum-phase (mixed) response.
            impulseResponse[20] = 1;
            impulseResponse[60] = -0.5f;

            float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.Slope);

            Assert.AreEqual(length / 2, excessPhase.Length);
            float maxAbs = 0;
            for (int i = 0; i < excessPhase.Length; i++) {
                maxAbs = MathF.Max(maxAbs, MathF.Abs(excessPhase[i]));
            }
            Assert.IsTrue(maxAbs > 0.01f, $"Mixed-phase impulse produced an all-zero excess phase (max abs = {maxAbs}), algorithm likely broken.");
        }

        /// <summary>
        /// Every delay-compensation mode (<see cref="DelayDeterminationType.Slope"/>,
        /// <see cref="DelayDeterminationType.SlopeWindowed"/>, <see cref="DelayDeterminationType.ImpulsePeak"/>,
        /// and <see cref="DelayDeterminationType.ImpulseEnvelopePeak"/>) must recover the exact delay of a pure
        /// Dirac delta, leaving an excess phase of ~0. This confirms the float[] overload routes the impulse
        /// response through FFT and delay compensation correctly for all supported modes.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_DelayedDiracDelta_AllCompensationModesNearZero() {
            int length = 256;
            int delay = 10;
            float[] impulseResponse = DiracDelta(length, delay);

            DelayDeterminationType[] compensationModes = [
                DelayDeterminationType.Slope,
                DelayDeterminationType.SlopeWindowed,
                DelayDeterminationType.ImpulsePeak,
                DelayDeterminationType.ImpulseEnvelopePeak
            ];

            foreach (DelayDeterminationType mode in compensationModes) {
                float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, mode);
                Assert.AreEqual(length / 2, excessPhase.Length, $"Length mismatch for mode {mode}.");
                for (int i = 0; i < excessPhase.Length; i++) {
                    Assert.AreEqual(0, excessPhase[i], 1e-3f,
                        $"Excess phase at bin {i} was not close to 0 for mode {mode}: {excessPhase[i]}");
                }
            }
        }

        /// <summary>
        /// Stress test: Destructive interference causes absolute zero magnitude at specific bins.
        /// This forces the algorithm's internal log-magnitude operation to evaluate ln(0) -> -infinity.
        /// Validates that internal stabilization routines (like a tiny epsilon floor) prevent NaN propagation.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_PerfectSpectralNull_DoesNotReturnNaNOrInfinity() {
            int length = 256;
            float[] impulseResponse = new float[length];
            // [1, 0, 0, ..., 1] creates a severe comb filter with mathematical zeros on the unit circle
            impulseResponse[0] = 1.0f;
            impulseResponse[128] = 1.0f;

            float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.None);

            Assert.AreEqual(length / 2, excessPhase.Length);
            for (int i = 0; i < excessPhase.Length; i++) {
                Assert.IsFalse(float.IsNaN(excessPhase[i]), $"NaN encountered at bin {i} due to a spectral null.");
                Assert.IsFalse(float.IsInfinity(excessPhase[i]), $"Infinity encountered at bin {i} due to an unhandled ln(0) singularity.");
            }
        }

        /// <summary>
        /// Edge case: Absolute dead air / muted buffer.
        /// Compels log(0) across the entire spectrum and forces Atan2(0,0) conditions.
        /// Tests if the API safe-guards against empty signals gracefully or throws a targeted exception.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_AbsoluteSilence_DoesNotCrash() {
            int length = 256;
            float[] impulseResponse = new float[length]; // Entirely 0.0f

            try {
                float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.None);

                Assert.AreEqual(length / 2, excessPhase.Length);
                for (int i = 0; i < excessPhase.Length; i++) {
                    Assert.IsFalse(float.IsNaN(excessPhase[i]), $"NaN leaked into the silence response array at bin {i}.");
                }
            } catch (ArgumentException) {
                // If your system architecture explicitly rejects dead signals, an ArgumentException is a passing state.
            }
        }

        /// <summary>
        /// Math stress: High group delay through a true Maximum-Phase structure.
        /// An inverted minimum-phase filter flips the roots outside the unit circle.
        /// This forces rapid phase wrapping and verifies that phase unwrapping/subtraction logic does not produce jagged discontinuities.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_TrueMaximumPhase_ReturnsSmoothUnwrappedCurve() {
            int length = 256;
            float[] impulseResponse = new float[length];

            // A classic maximum-phase two-tap system (zero outside the unit circle)
            impulseResponse[0] = 0.5f;
            impulseResponse[1] = 1.0f;

            float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.None);

            // Verify the phase sequence behaves as a continuous, stable curve without erratic step jumps.
            for (int i = 1; i < excessPhase.Length; i++) {
                float delta = MathF.Abs(excessPhase[i] - excessPhase[i - 1]);
                // A massive jump greater than Pi indicates a structural phase unwrapping failure
                // or unintended modulo artifacts under high phase slope conditions.
                Assert.IsTrue(delta < MathF.PI, $"Discontinuity or raw wrapping error detected between bin {i - 1} and {i} (Delta: {delta}).");
            }
        }

        /// <summary>
        /// Edge case: Tiny, near-zero signals (quantization/noise floor simulation).
        /// Tests whether the numeric precision limits underflow down to zero or maintain normalization stability.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void GetExcessPhase_MicroscopicSignal_IsNumericStable() {
            int length = 256;
            float[] impulseResponse = DiracDelta(length, 4);
            for (int i = 0; i < length; i++) {
                impulseResponse[i] *= 1e-25f; // Extremely low gain near IEEE 754 single-precision boundaries
            }

            float[] excessPhase = PhaseAnalyzer.GetExcessPhase(impulseResponse, DelayDeterminationType.Slope);

            for (int i = 0; i < excessPhase.Length; i++) {
                Assert.IsFalse(float.IsNaN(excessPhase[i]), $"Numeric underflow triggered a NaN artifact at bin {i}.");
            }
        }
    }
}
