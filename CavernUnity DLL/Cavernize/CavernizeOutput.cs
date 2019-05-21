using UnityEngine;

namespace Cavern.Cavernize {
    /// <summary>Outputs audio from a <see cref="Cavernizer"/> at a given channel.</summary>
    [AddComponentMenu("Audio/Internal (do not use)/Cavernize Output Source")]
    internal class CavernizeOutput : AudioSource3D {
        /// <summary>The Cavernizer to output audio from.</summary>
        [Header("Cavernized source settings")]
        [Tooltip("The Cavernizer to output audio from.")]
        public Cavernizer Master;
        /// <summary>Output the ground level (lowpassed) or moving (highpassed) audio.</summary>
        [Tooltip("Output the ground level (lowpassed) or moving (highpassed) audio.")]
        public bool GroundLevel;
        /// <summary>Target channel to render.</summary>
        public SpatializedChannel Channel;

        /// <summary>Force the source to be played.</summary>
        internal override bool Precollect() {
            base.Precollect();
            return true;
        }

        /// <summary>Indicates that the source meets rendering requirements, and <see cref="GetSamples"/> won't fail.</summary>
        internal override bool Renderable => IsPlaying;

        /// <summary>Get the next sample block from <see cref="Master"/>.</summary>
        internal override float[] GetSamples() => Master.Tick(Channel, GroundLevel);
    }
}