using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;

namespace Test.Cavern.QuickEQ.Measurement;

/// <summary>
/// Tests the <see cref="DelayCalculation"/> class.
/// </summary>
[TestClass]
public class DelayCalculation_Tests {
    /// <summary>
    /// Creates a Dirac delta impulse response with the impulse at <paramref name="offset"/>.
    /// </summary>
    static float[] DiracDeltaAtOffset(int length, int offset) {
        float[] result = new float[length];
        result[offset] = 1;
        return result;
    }

    /// <summary>
    /// Test a specific <paramref name="method"/> of delay calculation on a Dirac-delta with an actual delay set by an <paramref name="offset"/> in samples.
    /// </summary>
    static void PerformWithImpulseResponse(int signalLength, int offset, DelayDeterminationType method) {
        float[] impulseResponse = DiracDeltaAtOffset(signalLength, offset);
        float delay = DelayCalculation.Get(impulseResponse, method);
        Assert.AreEqual(offset, delay, .5f, $"Delay with {offset} returned a calculated delay of {delay}");
    }

    /// <summary>
    /// Test a specific <paramref name="method"/> of delay calculation on a Dirac-delta's FFT with an actual delay set by an <paramref name="offset"/> in samples.
    /// </summary>
    static void PerformWithTransferFunction(int signalLength, int offset, DelayDeterminationType method) {
        float[] impulseResponse = DiracDeltaAtOffset(signalLength, offset);
        Complex[] transferFunction = Measurements.FFT(impulseResponse);
        float delay = DelayCalculation.Get(transferFunction, method);
        Assert.AreEqual(offset, delay, .5f, $"Delay with {offset} returned a calculated delay of {delay}");
    }

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(float[])"/> returns the correct delay for a value of 0.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_ImpulseResponse_DiracDeltaAt0() => PerformWithImpulseResponse(128, 0, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(float[])"/> returns the correct delay for a value of 16.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_ImpulseResponse_DiracDeltaAt16() => PerformWithImpulseResponse(128, 16, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(float[])"/> returns the correct delay for a value of 47.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_ImpulseResponse_DiracDeltaAt47() => PerformWithImpulseResponse(128, 47, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(float[])"/> returns the correct delay for a value of 0.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_ImpulseResponse_DiracDeltaAt69() => PerformWithImpulseResponse(128, 69, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(float[])"/> returns the correct delay for a value of 111.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_ImpulseResponse_DiracDeltaAt111() => PerformWithImpulseResponse(128, 111, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(Complex[])"/> returns the correct delay for a value of 0.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_TransferFunction_DiracDeltaAt0() => PerformWithTransferFunction(128, 0, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(Complex[])"/> returns the correct delay for a value of 16.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_TransferFunction_DiracDeltaAt16() => PerformWithTransferFunction(128, 16, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(Complex[])"/> returns the correct delay for a value of 47.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_TransferFunction_DiracDeltaAt47() => PerformWithTransferFunction(128, 47, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(Complex[])"/> returns the correct delay for a value of 0.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_TransferFunction_DiracDeltaAt69() => PerformWithTransferFunction(128, 69, DelayDeterminationType.Slope);

    /// <summary>
    /// Tests if <see cref="DelayCalculation.GetSlopeDelay(Complex[])"/> returns the correct delay for a value of 111.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetSlopeDelay_TransferFunction_DiracDeltaAt111() => PerformWithTransferFunction(128, 111, DelayDeterminationType.Slope);
}
