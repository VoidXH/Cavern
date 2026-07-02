using Cavern.Format.Networking;

namespace Test.Cavern.Format.Networking;

/// <summary>
/// Tests the <see cref="SystemPTPClock"/> class.
/// </summary>
[TestClass]
public class SystemWrapperPtpClock_Tests {
    /// <summary>
    /// Tests if GetCurrentTimeNanoseconds returns a non-negative value.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetCurrentTimeNanoseconds_ReturnsNonNegativeValue() {
        SystemPTPClock clock = new();
        long time = clock.GetCurrentTimeNanoseconds();
        Assert.IsTrue(time >= 0);
    }

    /// <summary>
    /// Tests if GetCurrentTimeNanoseconds increases over time.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetCurrentTimeNanoseconds_IncreasesOverTime() {
        SystemPTPClock clock = new();
        long time1 = clock.GetCurrentTimeNanoseconds();
        Thread.Sleep(50);
        long time2 = clock.GetCurrentTimeNanoseconds();
        Assert.IsTrue(time2 > time1);
    }

    /// <summary>
    /// Tests if GetCurrentTimeNanoseconds returns monotonically increasing values.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void GetCurrentTimeNanoseconds_ReturnsMonotonicallyIncreasingValues() {
        SystemPTPClock clock = new();
        List<long> times = [];
        for (int i = 0; i < 10; i++) {
            times.Add(clock.GetCurrentTimeNanoseconds());
        }
        for (int i = 1; i < times.Count; i++) {
            Assert.IsTrue(times[i] >= times[i - 1], $"Time should not decrease: times[{i}] = {times[i]} < times[{i - 1}] = {times[i - 1]}");
        }
    }

    /// <summary>
    /// Tests if WaitUntil does not return before the target time.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void WaitUntil_DoesNotReturnBeforeTarget() {
        SystemPTPClock clock = new();
        long target = clock.GetCurrentTimeNanoseconds() + 10_000_000L; // 10 ms in nanoseconds
        clock.WaitUntil(target);
        long actual = clock.GetCurrentTimeNanoseconds();
        Assert.IsTrue(actual >= target, $"WaitUntil returned early: actual = {actual}, target = {target}");
    }

    /// <summary>
    /// Tests if WaitUntil returns quickly when the target is in the past.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void WaitUntil_ReturnsQuicklyWhenPastTarget() {
        SystemPTPClock clock = new();
        long target = clock.GetCurrentTimeNanoseconds() - 1_000_000L; // 1 ms in the past
        DateTime start = DateTime.UtcNow;
        clock.WaitUntil(target);
        TimeSpan elapsed = DateTime.UtcNow - start;
        Assert.IsTrue(elapsed.TotalMilliseconds < 50, "WaitUntil should return quickly when target is in the past");
    }

    /// <summary>
    /// Tests if multiple consecutive calls to GetCurrentTimeNanoseconds return consistent values.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void MultipleCallsToGetCurrentTimeNanoseconds_ReturnsConsistentValues() {
        SystemPTPClock clock = new();
        long time1 = clock.GetCurrentTimeNanoseconds();
        long time2 = clock.GetCurrentTimeNanoseconds();

        // Two consecutive calls should be very close (within 1 ms)
        long diff = Math.Abs(time2 - time1);
        Assert.IsTrue(diff < 1_000_000L, $"Consecutive calls differ by more than 1 ms: {diff} ns");
    }
}
