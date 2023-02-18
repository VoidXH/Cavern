using System;
using System.Numerics;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Remapping {
    /// <summary>
    /// Convert any standard multichannel audio stream to the channel layout set for Cavern.
    /// </summary>
    public sealed class Remapper : IDisposable {
        /// <summary>
        /// Channels to remap.
        /// </summary>
        public readonly int channels;

        /// <summary>
        /// Remapping update rate.
        /// </summary>
        public int UpdateRate {
            get => updateRate;
            set {
                for (int clip = 0; clip < clips.Length; clip++) {
                    clips[clip].Remake(value);
                }
                updateRate = value;
            }
        }

        /// <summary>
        /// Cavern rendering environment to upscale to.
        /// </summary>
        readonly Listener listener;

        /// <summary>
        /// Easily editable <see cref="Clip"/>s for channel spoofing.
        /// </summary>
        readonly RemappedChannel[] clips;

        /// <summary>
        /// Sources representing each source channel.
        /// </summary>
        readonly Source[] sources;

        /// <summary>
        /// Source update rate.
        /// </summary>
        int updateRate;

        /// <summary>
        /// Convert any standard multichannel audio stream to the channel layout set for Cavern while using a standard layout
        /// for the given number of <paramref name="channels"/>.
        /// </summary>
        /// <param name="channels">Channels to remap</param>
        /// <param name="updateRate">Remapping update rate</param>
        public Remapper(int channels, int updateRate) : this(channels, updateRate, false) { }

        /// <summary>
        /// Convert any standard multichannel audio stream to the channel layout set for Cavern.
        /// </summary>
        /// <param name="channels">Channels to remap</param>
        /// <param name="updateRate">Remapping update rate</param>
        /// <param name="loadCavernSettings">Load user settings including the Cavern channel layout</param>
        public Remapper(int channels, int updateRate, bool loadCavernSettings) {
            listener = new Listener(loadCavernSettings) {
                UpdateRate = this.updateRate = updateRate
            };
            clips = new RemappedChannel[listener.MaximumSources = this.channels = channels];
            sources = new Source[channels];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(channels);
            for (int channel = 0; channel < channels; ++channel) {
                Source source = sources[channel] = new Source();
                listener.AttachSource(source);
                ChannelPrototype prototype = ChannelPrototype.Mapping[(int)matrix[channel]];
                source.Clip = clips[channel] = new RemappedChannel(updateRate);
                source.Mute = prototype.Muted;
                source.Loop = true;
                source.LFE = prototype.LFE;
                source.Position = new Vector3(prototype.X, prototype.Y, 0).PlaceInCube();
                source.Position *= Listener.EnvironmentSize;
                source.VolumeRolloff = Rolloffs.Disabled;
            }
        }

        /// <summary>
        /// Remap a multichannel audio frame.
        /// </summary>
        /// <param name="stream">Source audio stream</param>
        /// <param name="channels">Source channel count</param>
        public float[] Update(float[] stream, int channels) {
            int actualRate = stream.Length / channels;
            if (updateRate != actualRate) {
                for (int channel = 0; channel < channels; channel++) {
                    clips[channel].Remake(actualRate);
                }
                listener.UpdateRate = updateRate = actualRate;
            }
            for (int channel = 0; channel < channels; channel++) {
                clips[channel].Update(stream, channel, channels);
            }
            return listener.Render();
        }

        /// <summary>
        /// Remove the created sources from the listener.
        /// </summary>
        public void Dispose() {
            for (int channel = 0; channel < channels; channel++) {
                listener.DetachSource(sources[channel]);
            }
        }
    }
}