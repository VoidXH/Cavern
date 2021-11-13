using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.QuickEQ.SignalGeneration;

namespace Cavern.QuickEQ {
    /// <summary>Runs the sweep of a <see cref="SpeakerSweeper"/> with a correct delay for the given channel.</summary>
    [AddComponentMenu("Audio/QuickEQ/Sweep Channel")]
    internal class SweepChannel : AudioSource3D {
        /// <summary>Target output channel.</summary>
        [Header("Sweep channel")]
        [Tooltip("Target output channel.")]
        public int Channel = 0;
        /// <summary>Sweeper to use the sweep reference of.</summary>
        [Tooltip("Sweeper to use the sweep reference of.")]
        public SpeakerSweeper Sweeper;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            AudioListener3D.cavernListener.DetachSource(cavernSource); // preattached in OnCreate
            cavernSource = new TimedTestTone(Channel, Sweeper.SweepReference);
            AudioListener3D.cavernListener.AttachSource(cavernSource);
        }
    }
}