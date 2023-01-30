using System.Collections.Generic;
using System.Numerics;

using Cavern.Channels;
using Cavern.Format.Decoders;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded RIFF WAVE stream.
    /// </summary>
    public class RIFFWaveRenderer : Renderer {
        /// <summary>
        /// Index of the last passed ADM block.
        /// </summary>
        readonly int[] admBlocks;

        /// <summary>
        /// Reused array for output rendering.
        /// </summary>
        float[] render;

        /// <summary>
        /// Renders a decoded RIFF WAVE stream.
        /// </summary>
        public RIFFWaveRenderer(RIFFWaveDecoder stream) : base(stream) {
            if (stream.ADM == null) {
                SetupChannels();
            } else {
                SetupObjects(stream.ChannelCount);
                admBlocks = new int[stream.ChannelCount];
                IReadOnlyList<ADMChannelFormat> movements = stream.ADM.Movements;
                for (int i = 0, c = movements.Count; i < c; i++) {
                    if (movements[i].Blocks.Count == 1 &&
                        ChannelFromPosition(movements[i].Blocks[0].Position) == ReferenceChannel.ScreenLFE) {
                        objects[i].LFE = true;
                    }
                }
            }
            objectSamples[0] = new float[0];
        }

        /// <summary>
        /// Get the bed channels.
        /// </summary>
        public override ReferenceChannel[] GetChannels() {
            RIFFWaveDecoder decoder = (RIFFWaveDecoder)stream;
            AudioDefinitionModel adm = decoder.ADM;
            if (adm == null) {
                return decoder.GetChannels();
            } else {
                List<ReferenceChannel> channels = new List<ReferenceChannel>();
                for (int i = 0, c = adm.Movements.Count; i < c; i++) {
                    if (adm.Movements[i].Blocks.Count == 1 && adm.Movements[i].Blocks[0].Duration.IsZero()) {
                        channels.Add(ChannelFromPosition(adm.Movements[i].Blocks[0].Position));
                    }
                }
                return channels.ToArray();
            }
        }

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the objects.
        /// </summary>
        /// <param name="samples">Samples per channel</param>
        public override void Update(int samples) {
            AudioDefinitionModel adm = ((RIFFWaveDecoder)stream).ADM;

            if (objectSamples[0].Length != samples) {
                for (int i = 0; i < objectSamples.Length; i++) {
                    objectSamples[i] = new float[samples];
                }
                render = new float[objectSamples.Length * samples];
            }

            stream.DecodeBlock(render, 0, render.LongLength);
            WaveformUtils.InterlacedToMultichannel(render, objectSamples);

            if (adm != null) {
                for (int i = 0; i < objectSamples.Length; i++) {
                    List<ADMBlockFormat> blocks = adm.Movements[i].Blocks;

                    while (admBlocks[i] > 0 &&
                        blocks[admBlocks[i] - 1].Offset.TotalSeconds * stream.SampleRate > stream.Position) {
                        --admBlocks[i];
                    }
                    while (admBlocks[i] < blocks.Count - 1 &&
                        blocks[admBlocks[i] + 1].Offset.TotalSeconds * stream.SampleRate < stream.Position) {
                        ++admBlocks[i];
                    }

                    ADMBlockFormat current = blocks[admBlocks[i]],
                        previous = admBlocks[i] != 0 ? blocks[admBlocks[i] - 1] : current;
                    float fade = 1;
                    if (!current.Offset.IsZero()) {
                        fade = (float)QMath.LerpInverse(current.Offset.TotalSeconds * stream.SampleRate,
                            (current.Offset + current.Interpolation).TotalSeconds * stream.SampleRate, stream.Position);
                    }
                    objects[i].Position =
                        Vector3.Lerp(previous.Position, current.Position, QMath.Clamp01(fade)) * Listener.EnvironmentSize;
                }
            }
        }
    }
}