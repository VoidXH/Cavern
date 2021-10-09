using System;
using System.Numerics;

using Cavern.Utilities;

namespace Cavern.Remapping {
    /// <summary>Convert any standard multichannel audio stream to the channel layout set for Cavern.</summary>
    public sealed class Remapper : IDisposable {
        /// <summary>Channels to remap.</summary>
        public readonly int channels;
        /// <summary>Remapping update rate.</summary>
        public int UpdateRate {
            get => updateRate;
            set {
                for (int clip = 0; clip < clips.Length; ++clip)
                    clips[clip].Remake(value);
                updateRate = value;
            }
        }

        int updateRate;

        readonly Listener listener = new Listener(false);
        readonly RemappedChannel[] clips;
        readonly Source[] sources;

        /// <summary>Convert any standard multichannel audio stream to the channel layout set for Cavern.</summary>
        /// <param name="channels">Channels to remap</param>
        /// <param name="updateRate">Remapping update rate</param>
        public Remapper(int channels, int updateRate) {
            listener.UpdateRate = this.updateRate = updateRate;
            clips = new RemappedChannel[listener.MaximumSources = this.channels = channels];
            sources = new Source[channels];
            for (int channel = 0; channel < channels; ++channel) {
                Source source = sources[channel] = new Source();
                listener.AttachSource(source);
                ChannelPrototype prototype = ChannelPrototype.Mapping[(int)ChannelPrototype.StandardMatrix[channels][channel]];
                source.Clip = clips[channel] = new RemappedChannel(updateRate);
                source.Loop = true;
                source.LFE = prototype.LFE;
                source.Position = new Vector3(prototype.X, prototype.Y, 0).PlaceInCube();
                source.Position *= Listener.EnvironmentSize;
                source.VolumeRolloff = Rolloffs.Disabled;
            }
        }

        /// <summary>Remap a multichannel audio frame.</summary>
        /// <param name="stream">Source audio stream</param>
        /// <param name="channels">Source channel count</param>
        public float[] Update(float[] stream, int channels) {
            int actualRate = stream.Length / channels;
            if (updateRate != actualRate) {
                for (int channel = 0; channel < channels; ++channel)
                    clips[channel].Remake(actualRate);
                listener.UpdateRate = updateRate = actualRate;
            }
            for (int channel = 0; channel < channels; ++channel)
                clips[channel].Update(stream, channel, channels);
            return listener.Render();
        }

        /// <summary>Remove the created sources from the listener.</summary>
        public void Dispose() {
            for (int channel = 0; channel < channels; ++channel)
                listener.DetachSource(sources[channel]);
        }
    }
}