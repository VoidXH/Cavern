using System.Numerics;

using Cavern.Remapping;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Constants required for rendering.
    /// </summary>
    partial class Renderer {
        /// <summary>
        /// Rendering positions of standard channels, indexed by <see cref="ReferenceChannel"/>s.
        /// </summary>
        public static readonly Vector3[] channelPositions = {
            new Vector3(-1, 0, 1), // FrontLeft
            new Vector3(1, 0, 1), // FrontRight
            new Vector3(0, 0, 1), // FrontCenter
            new Vector3(0, 0, 1), // ScreenLFE
            new Vector3(-1, 0, -1), // RearLeft
            new Vector3(1, 0, -1), // RearRight
            new Vector3(-1, 0, 0), // SideLeft
            new Vector3(1, 0, 0), // SideRight
            new Vector3(-.5f, 0, 1), // FrontLeftCenter
            new Vector3(.5f, 0, 1), // FrontRightCenter
            new Vector3(0, 0, 1), // HearingImpaired
            new Vector3(0, 0, 1), // VisuallyImpaired
            new Vector3(0, 0, 1), // Unknown
            new Vector3(0, 0, 1), // MotionData
            new Vector3(0, 0, 1), // ExternalData
            new Vector3(-1, 1, 1), // TopFrontLeft
            new Vector3(1, 1, 1), // TopFrontRight
            new Vector3(-1, 1, 0), // TopSideLeft
            new Vector3(1, 1, 0), // TopSideRight
            new Vector3(0, 0, 1), // SignLanguage
            new Vector3(0, -1, 0), // BottomSurround
            new Vector3(0, 1, 1), // TopFrontCenter
            new Vector3(0, 1, 0), // GodsVoice
            new Vector3(0, 0, -1), // RearCenter
            new Vector3(-1, 0, .677419f), // WideLeft
            new Vector3(1, 0, .677419f), // WideRight
            new Vector3(-1, 1, -1), // TopRearLeft
            new Vector3(-1, 1, 1), // TopRearRight
            new Vector3(-1, 1, 0) // TopRearCenter
        };
    }
}