using System;
using System.Numerics;

using Cavern.Channels;
using Cavern.Format.Decoders;
using Cavern.Format.Renderers.CoreAudioFormat;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded Core Audio Format stream.
    /// </summary>
    public class CoreAudioFormatRenderer : PCMToObjectsRenderer {
        /// <summary>
        /// The first PCM tracks are these specific channels.
        /// </summary>
        readonly ReferenceChannel[] channels;

        /// <summary>
        /// Movement data for each dynamic object (bed channel object indices are null).
        /// </summary>
        readonly MovementTimeframe[][] movement;

        /// <summary>
        /// Which index of an object's corresponding <see cref="movement"/> array was last read.
        /// </summary>
        readonly int[] movementFrames;

        /// <summary>
        /// Renders a channel-based Core Audio Format stream.
        /// </summary>
        public CoreAudioFormatRenderer(Decoder stream) : base(stream) {
            SetupChannels();
            objectSamples[0] = Array.Empty<float>();
        }

        /// <summary>
        /// Renders an object-based Core Audio Format stream (Dolby Atmos Master Format).
        /// </summary>
        internal CoreAudioFormatRenderer(Decoder stream, YAML rootSource, YAML metadataSource) : base(stream) {
            SetupObjects(Channels);
            objectSamples[0] = Array.Empty<float>();

            DolbyAtmosMasterRootFile root = new DolbyAtmosMasterRootFile(rootSource, Channels);
            channels = root.Channels;
            Vector3[] positions = ChannelPrototype.ToAlternativePositions(channels);
            for (int i = 0; i < channels.Length; i++) {
                objects[i].Position = positions[i] * Listener.EnvironmentSize;
                objects[i].LFE = ChannelPrototype.Mapping[(int)channels[i]].LFE;
            }

            DolbyAtmosMasterMetadataFile metadata = new DolbyAtmosMasterMetadataFile(metadataSource, root.ObjectMapping);
            movement = metadata.Movement;
            movementFrames = new int[movement.Length];
        }

        /// <inheritdoc/>
        public override void Update(int samples) {
            base.Update(samples);
            if (movement == null) {
                return;
            }

            for (int i = 0, c = movement.Length; i < c; i++) {
                MovementTimeframe[] movementData = movement[i];
                if (movementData == null) {
                    continue;
                }

                while (movementFrames[i] > 0 && movementData[movementFrames[i] - 1].offset > stream.Position) {
                    movementFrames[i]--;
                }
                while (movementFrames[i] < movementData.Length - 1 && movementData[movementFrames[i] + 1].offset < stream.Position) {
                    movementFrames[i]++;
                }

                MovementTimeframe current = movementData[movementFrames[i]];
                MovementTimeframe previous = movementFrames[i] != 0 ? movementData[movementFrames[i] - 1] : current;
                float fade = QMath.LerpInverse(current.offset - current.fade, current.offset, stream.Position);
                objects[i].Position = Vector3.Lerp(previous.position, current.position, QMath.Clamp01(fade)) * Listener.EnvironmentSize;
            }
        }

        /// <inheritdoc/>
        public override ReferenceChannel[] GetChannels() => channels ?? base.GetChannels();
    }
}
