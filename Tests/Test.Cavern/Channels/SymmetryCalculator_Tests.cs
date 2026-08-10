using Cavern;
using Cavern.Channels;

namespace Test.Cavern.Channels;

/// <summary>
/// Tests for the <see cref="SymmetryCalculator"/> utility.
/// </summary>
[TestClass]
public class SymmetryCalculator_Tests {
    /// <summary>
    /// Tests that a layout with two channels at 0° and 180° (both on the center line: front center and rear center)
    /// is considered symmetric.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void TwoChannelsOnCenterLine_IsSymmetric() {
        // Arrange: Create two channels at 0° (front center) and 180° (rear center)
        // Both have X = 0 (on the horizontal plane), Y = 0 and Y = 180 respectively
        Channel[] channels = [
            new Channel(0, 0),   // Front center
            new Channel(0, 180)  // Rear center (equivalent to -180°)
        ];

        // Act: Calculate symmetry directly
        bool isSymmetric = SymmetryCalculator.CalculateSymmetry(channels);

        // Assert: The layout should be symmetric since both channels are on the center line (Y % 180 == 0)
        Assert.IsTrue(isSymmetric, "Layout with channels at 0° and 180° should be symmetric");
    }

    /// <summary>
    /// Tests that a layout with a single channel at 0° (center) is considered symmetric.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SingleChannelAtCenter_IsSymmetric() {
        // Arrange: Create a single channel at 0° (center)
        Channel[] channels = [
            new Channel(0, 0)   // Center
        ];

        // Act: Calculate symmetry directly
        bool isSymmetric = SymmetryCalculator.CalculateSymmetry(channels);

        // Assert: The layout should be symmetric since the single channel is at center (Y % 180 == 0)
        Assert.IsTrue(isSymmetric, "Layout with single channel at center (0°) should be symmetric");
    }

    /// <summary>
    /// Tests that a layout with a single channel at 90° (side) is NOT considered symmetric.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SingleChannelAtSide_IsNotSymmetric() {
        // Arrange: Create a single channel at 90° (right side)
        Channel[] channels = [
            new Channel(0, 90)   // Right side
        ];

        // Act: Calculate symmetry directly
        bool isSymmetric = SymmetryCalculator.CalculateSymmetry(channels);

        // Assert: The layout should NOT be symmetric since the single channel is not at center
        Assert.IsFalse(isSymmetric, "Layout with single channel at side (90°) should not be symmetric");
    }

    /// <summary>
    /// Tests that a standard stereo pair (-30°, +30°) is symmetric.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void StereoPair_IsSymmetric() {
        // Arrange: Standard stereo pair
        Channel[] channels = ChannelPrototype.ToLayout(ChannelPrototype.ref200);

        // Act: Calculate symmetry directly
        bool isSymmetric = SymmetryCalculator.CalculateSymmetry(channels);

        // Assert: Standard stereo pair should be symmetric
        Assert.IsTrue(isSymmetric, "Standard stereo pair (-30°, +30°) should be symmetric");
    }

    /// <summary>
    /// Tests that 5.1 layout is symmetric.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void Standard51Layout_IsSymmetric() {
        // Arrange: Standard 5.1 layout
        Channel[] channels = ChannelPrototype.ToLayout(ChannelPrototype.ref510);

        // Act: Calculate symmetry directly
        bool isSymmetric = SymmetryCalculator.CalculateSymmetry(channels);

        // Assert: Standard 5.1 should be symmetric
        Assert.IsTrue(isSymmetric, "Standard 5.1 layout should be symmetric");
    }

    /// <summary>
    /// Tests that a single vertical stack at offset is NOT symmetric (no mirror pair).
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void SingleVerticalStackAtOffset_IsNotSymmetric() {
        // Arrange: Two channels at same azimuth but different elevations - no mirror
        Channel[] channels = [
            new Channel(15, 45),  // Top Front Right
            new Channel(30, 45)   // Higher Front Right - no mirror on left side
        ];

        // Act: Calculate symmetry directly
        bool isSymmetric = SymmetryCalculator.CalculateSymmetry(channels);

        // Assert: Should NOT be symmetric - no mirror pairs found
        Assert.IsFalse(isSymmetric, "Single vertical stack at offset without mirror should not be symmetric");
    }

    /// <summary>
    /// Tests that a vertical stack with mirrors IS symmetric.
    /// </summary>
    [TestMethod, Timeout(1000)]
    public void VerticalStackWithMirrors_IsSymmetric() {
        // Arrange: Vertical stack on both left and right sides
        Channel[] channels = [
            new Channel(15, -45),  // Top Front Left
            new Channel(30, -45),  // Higher Front Left
            new Channel(15, 45),   // Top Front Right
            new Channel(30, 45)    // Higher Front Right
        ];

        // Act: Calculate symmetry directly
        bool isSymmetric = SymmetryCalculator.CalculateSymmetry(channels);

        // Assert: Should be symmetric - all speakers have mirrors
        Assert.IsTrue(isSymmetric, "Vertical stack with mirrors on both sides should be symmetric");
    }
}
