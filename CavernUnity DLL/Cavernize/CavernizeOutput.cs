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

        /// <summary>Custom Cavern <see cref="Source"/> for this component.</summary>
        class CavernizeOutputSource : Source {
            /// <summary>The Cavernizer to output audio from.</summary>
            public Cavernizer Master;
            /// <summary>Output the ground level (lowpassed) or moving (highpassed) audio.</summary>
            public bool GroundLevel;
            /// <summary>Target channel to render.</summary>
            public SpatializedChannel Channel;

            /// <summary>Force the source to be played.</summary>
            protected override bool Precollect() {
                base.Precollect();
                return true;
            }

            /// <summary>Indicates that the source meets rendering requirements, and <see cref="GetSamples"/> won't fail.</summary>
            protected override bool Renderable => IsPlaying;

            /// <summary>Get the next sample block from <see cref="Master"/>.</summary>
            protected override float[][] GetSamples() => Master.Tick(Channel, GroundLevel);
        }

        void Awake() => cavernSource = new CavernizeOutputSource() {
            Clip = new Clip(new float[1][] { new float[1] }, 48000)
        };

        void Start() {
            CavernizeOutputSource source = (CavernizeOutputSource)cavernSource;
            source.Clip = new Clip(new float[1][] { new float[1] }, Master.Clip.frequency);
            source.Master = Master;
            source.GroundLevel = GroundLevel;
            source.Channel = Channel;
            SourceUpdate();
        }

        void Update() {
            Channel.Update();
            SourceUpdate();
        }
    }
}