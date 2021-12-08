using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Cavernize {
    /// <summary>
    /// Outputs audio from a <see cref="Cavernizer"/> at a given channel.
    /// </summary>
    [AddComponentMenu("Audio/Internal (do not use)/Cavernize Output Source")]
    internal class CavernizeOutput : AudioSource3D {
        /// <summary>
        /// The Cavernizer to output audio from.
        /// </summary>
        [Header("Cavernized source settings")]
        [Tooltip("The Cavernizer to output audio from.")]
        public Cavernizer Master;

        /// <summary>
        /// Output the ground level (lowpassed) or moving (highpassed) audio.
        /// </summary>
        [Tooltip("Output the ground level (lowpassed) or moving (highpassed) audio.")]
        public bool GroundLevel;

        /// <summary>
        /// Target channel to render.
        /// </summary>
        public SpatializedChannel Channel;

        /// <summary>
        /// Custom Cavern <see cref="Source"/> for this component.
        /// </summary>
        class CavernizeOutputSource : Source {
            /// <summary>
            /// The Cavernizer to output audio from.
            /// </summary>
            public Cavernizer Master;

            /// <summary>
            /// Output the ground level (lowpassed) or moving (highpassed) audio.
            /// </summary>
            public bool GroundLevel;

            /// <summary>
            /// Target channel to render.
            /// </summary>
            public SpatializedChannel Channel;

            /// <summary>
            /// Force the source to be played.
            /// </summary>
            protected override bool Precollect() {
                if (!Master)
                    return false;
                ForcePrecollect();
                bool state = base.Precollect();
                return state;
            }

            /// <summary>
            /// Indicates that the source meets rendering requirements, and <see cref="GetSamples"/> won't fail.
            /// </summary>
            protected override bool Renderable => IsPlaying;

            /// <summary>
            /// Get the next sample block from <see cref="Master"/>.
            /// </summary>
            protected override float[][] GetSamples() => Master.Tick(Channel, GroundLevel);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Awake() => cavernSource = new CavernizeOutputSource() {
            Clip = new Clip(new float[1][] { new float[1] }, Listener.defaultSampleRate)
        };

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            SourceUpdate();
            CavernizeOutputSource source = (CavernizeOutputSource)cavernSource;
            if (Master.Clip != null)
                source.Clip = new Clip(new float[1][] { new float[1] }, Master.Clip.frequency);
            else
                source.Clip = new Clip(new float[1][] { new float[1] }, Master.Clip3D.SampleRate);
            source.Master = Master;
            source.GroundLevel = GroundLevel;
            source.Channel = Channel;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            Channel.Update();
            SourceUpdate();
        }
    }
}