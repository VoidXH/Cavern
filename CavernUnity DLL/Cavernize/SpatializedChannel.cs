using UnityEngine;

using Cavern.Channels;
using Cavern.Helpers;
using Cavern.Utilities;

using Vector3D = System.Numerics.Vector3;

namespace Cavern.Cavernize {
    /// <summary>
    /// All the data <see cref="Cavernizer"/> needs for a single channel.
    /// </summary>
    class SpatializedChannel {
        /// <summary>
        /// Channel position and type information.
        /// </summary>
        public readonly ChannelPrototype Channel;

        /// <summary>
        /// Samples to split between <see cref="MovingSource"/> and <see cref="GroundSource"/>.
        /// </summary>
        public readonly float[] Output;

        /// <summary>
        /// There is available output data, and the channel should be rendered.
        /// </summary>
        public bool WrittenOutput;

        /// <summary>
        /// High frequency data with height information.
        /// </summary>
        public CavernizeOutput MovingSource { get; private set; }

        /// <summary>
        /// Low frequency data that stays on the ground.
        /// </summary>
        public CavernizeOutput GroundSource { get; private set; }

        /// <summary>
        /// The moving part's normalized height from the ground.
        /// </summary>
        public float Height { get; private set; }

        /// <summary>
        /// Times the sources called for a set of samples. Numbers >= 2 indicate that the next frame should be mixed.
        /// </summary>
        public int TicksTook;

        /// <summary>
        /// Renderer for <see cref="MovingSource"/>.
        /// </summary>
        public Renderer MovingRenderer { get; private set; }

        /// <summary>
        /// Renderer for <see cref="GroundSource"/>.
        /// </summary>
        public Renderer GroundRenderer { get; private set; }

        /// <summary>
        /// Enable visualization of this channel in the next frame.
        /// </summary>
        bool visualize;

        readonly Filters.Cavernize filter;

        void CreateSource(Cavernizer master, bool groundLevel) {
            GameObject newObject;
            if (!Channel.Equals(ChannelPrototype.ScreenLFE)) {
                newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            } else {
                newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            newObject.name = Channel.Name;
            CavernizeOutput newSource = newObject.AddComponent<CavernizeOutput>();
            newSource.Master = master;
            newSource.Channel = this;
            if (newSource.GroundLevel = groundLevel) {
                GroundSource = newSource;
                GroundRenderer = newObject.GetComponent<Renderer>();
            } else {
                MovingSource = newSource;
                MovingRenderer = newObject.GetComponent<Renderer>();
            }
            newSource.Loop = true;
            newSource.VolumeRolloff = Rolloffs.Disabled;
            newSource.LFE = Channel.LFE;
            newObject.AddComponent<ScaleByGain>().source = newSource;
            if (Channel.Muted) {
                newSource.Volume = 0;
            }
            newObject.transform.SetParent(master.transform);
            Vector3D position = new Vector3D(0, Channel.Y, 0).PlaceInCube();
            position *= Listener.EnvironmentSize;
            newObject.transform.localPosition = VectorUtils.VectorMatch(position);
        }

        public SpatializedChannel(ReferenceChannel source, Cavernizer master, int updateRate) {
            Channel = ChannelPrototype.Mapping[(int)source];
            filter = new Filters.Cavernize(AudioListener3D.Current.SampleRate, 250);
            filter.PresetOutput(updateRate);
            Output = new float[updateRate];
            CreateSource(master, true);
            CreateSource(master, false);
        }

        public void Tick(float effect, float smoothFactor, float crossoverFreq, bool visualize) {
            if (!WrittenOutput) {
                Output.Clear();
            }
            if (filter.GroundCrossover != crossoverFreq) {
                filter.GroundCrossover = crossoverFreq;
            }
            this.visualize = visualize && WrittenOutput;
            if (WrittenOutput) {
                filter.Effect = effect;
                filter.SmoothFactor = smoothFactor;
                filter.Process(Output);
                if (Channel.Y != 0 || !MovingSource.Master.CenterStays || Channel.X != 0) {
                    Height = filter.Height;
                } else {
                    Height = Cavernizer.unsetHeight;
                }
            }
        }

        public float[] GetOutput(bool groundLevel) {
            if (groundLevel) {
                return filter.GroundLevel;
            }
            return filter.HeightLevel;
        }

        public void Update() {
            MovingRenderer.enabled = GroundRenderer.enabled = visualize;
            Vector3D position = new Vector3D(0, Channel.Y, 0).PlaceInCube();
            position *= Listener.EnvironmentSize;
            if (Height != Cavernizer.unsetHeight) {
                position.Y = Height * Listener.EnvironmentSize.Y;
            } else {
                position.Y = 0;
            }
            MovingSource.transform.localPosition = VectorUtils.VectorMatch(position);
        }

        public void Destroy() {
            if (MovingSource) {
                Object.Destroy(MovingSource.gameObject);
            }
            if (GroundSource) {
                Object.Destroy(GroundSource.gameObject);
            }
        }
    }
}