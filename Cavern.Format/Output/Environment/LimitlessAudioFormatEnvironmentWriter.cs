using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

using Cavern.SpecialSources;

namespace Cavern.Format.Environment {
    public class LimitlessAudioFormatEnvironmentWriter : EnvironmentWriter {
        /// <summary>
        /// The file to write the environment to.
        /// </summary>
        readonly LimitlessAudioFormatWriter output;

        /// <summary>
        /// Number of objects to write to the file.
        /// </summary>
        readonly int objects;

        /// <summary>
        /// The last position update that is already under exporting. It's only refreshed when the
        /// previous state is flushed to the file. One byte array for the next frame of each track.
        /// </summary>
        byte[][] positionalBlock;

        /// <summary>
        /// Total samples written to the export file.
        /// </summary>
        long samplesWritten;

        /// <summary>
        /// How many bytes of the <see cref="objectStreamRate"/> are currently written.
        /// </summary>
        long objectStreamPosition;

        public LimitlessAudioFormatEnvironmentWriter(BinaryWriter writer, Listener source, long length, BitDepth bits) :
            base(writer, source) {
            IReadOnlyCollection<Source> sources = source.ActiveSources;
            objects = sources.Count;
            positionalBlock = new byte[(objects >> 4) + ((objects & 0b1111) != 0 ? 1 : 0)][];
            for (int track = 0; track < positionalBlock.Length; track++) {
                positionalBlock[track] = new byte[objectStreamRate];
            }
            Channel[] channels = new Channel[objects + positionalBlock.Length];

            IEnumerator<Source> enumerator = sources.GetEnumerator();
            int i = 0;
            while (enumerator.MoveNext()) {
                channels[i++] = new Channel(enumerator.Current.Position, enumerator.Current.LFE);
            }
            while (i < channels.Length) {
                channels[i++] = new Channel(float.NaN, 0);
                Source mute = new MuteSource(source);
                source.AttachSource(mute);
            }
            output = new LimitlessAudioFormatWriter(writer, length, source.SampleRate, bits, channels);
        }

        /// <summary>
        /// Export the next frame of the <see cref="Source"/>.
        /// </summary>
        public override void WriteNextFrame() {
            float[] result = GetInterlacedPCMOutput();
            long writable = output.Length - samplesWritten;
            if (writable > 0) {
                long samplesPerChannel = Math.Min(Source.UpdateRate, writable),
                    bytesForMovement = samplesPerChannel * (long)output.Bits;
                while (bytesForMovement > 0) {
                    if (objectStreamPosition == 0) {
                        IEnumerator<Source> enumerator = Source.ActiveSources.GetEnumerator();
                        // TODO: update positionalBlocks
                    }

                    // TODO: flush what remains of the positionalBlocks and update objectStreamPosition
                }
                output.WriteBlock(result, 0, samplesPerChannel * output.ChannelCount);
            }
            samplesWritten += Source.UpdateRate;
        }

        /// <summary>
        /// Free the written file.
        /// </summary>
        public override void Dispose() {
            base.Dispose();
            output.Dispose();
        }

        /// <summary>
        /// The number of bytes for position updates per position track. Position tracks always contain data for 16
        /// channels, even if those values are unused.
        /// </summary>
        const long objectStreamRate = 16 * 3 * sizeof(float);
    }
}