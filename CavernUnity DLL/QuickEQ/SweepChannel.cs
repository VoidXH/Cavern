using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.QuickEQ.SignalGeneration;

namespace Cavern.QuickEQ {
    /// <summary>
    /// Runs the sweep of a <see cref="SpeakerSweeper"/> with a correct delay for the given channel.
    /// </summary>
    [AddComponentMenu("Audio/QuickEQ/Sweep Channel")]
    internal class SweepChannel : AudioSource3D {
        /// <summary>
        /// Target output channel.
        /// </summary>
        [Header("Sweep channel")]
        [Tooltip("Target output channel.")]
        public int Channel;

        /// <summary>
        /// Sweeper to use the sweep reference of.
        /// </summary>
        [Tooltip("Sweeper to use the sweep reference of.")]
        public SpeakerSweeper Sweeper;

        /// <summary>
        /// Waits a sweep's time before the actual measurement. This helps for measuring with microphones that click when the system turns them on.
        /// </summary>
        [Tooltip("Waits a sweep's time before the actual measurement. " +
            "This helps for measurement with microphones that click when the system turns them on.")]
        public bool WarmUpMode;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            AudioListener3D.cavernListener.DetachSource(cavernSource); // preattached in OnCreate
            cavernSource = new TimedTestTone(Channel, Sweeper.SweepReference, WarmUpMode);
            AudioListener3D.cavernListener.AttachSource(cavernSource);
        }
    }
}