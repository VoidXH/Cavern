using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

using Cavern.SpecialSources;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Exports the rendered environment in an object-based Limitless Audio Format file.
    /// </summary>
    public sealed class LimitlessAudioFormatEnvironmentWriter : EnvironmentWriter {
        /// <summary>
        /// The file to write the environment to.
        /// </summary>
        LimitlessAudioFormatWriter output;

        /// <summary>
        /// Number of objects to write to the file.
        /// </summary>
        int objects;

        /// <summary>
        /// The last position update that is already under exporting. It's only refreshed when the
        /// previous state is flushed to the file. One position collection for the next frame of each track.
        /// </summary>
        float[][] positionalBlock;

        /// <summary>
        /// Total samples written to the export file.
        /// </summary>
        int samplesWritten;

        /// <summary>
        /// How many bytes of the <see cref="objectStreamRate"/> are currently written.
        /// </summary>
        int objectStreamPosition = objectStreamRate;

        /// <summary>
        /// Exports the rendered environment in an object-based Limitless Audio Format file.
        /// </summary>
        public LimitlessAudioFormatEnvironmentWriter(Stream writer, Listener source, long length, BitDepth bits) :
            base(writer, source, length, bits) { }

        /// <summary>
        /// Exports the rendered environment in an object-based Limitless Audio Format file.
        /// </summary>
        public LimitlessAudioFormatEnvironmentWriter(string path, Listener source, long length, BitDepth bits) :
            base(path, source, length, bits) { }

        /// <summary>
        /// Export the next frame of the <see cref="Source"/>.
        /// </summary>
        public override void WriteNextFrame() {
            if (output == null) {
                CreateFile();
            }

            Vector3 scale = Vector3.One / Listener.EnvironmentSize;
            float[] result = GetInterlacedPCMOutput();
            long writable = output.Length - samplesWritten;
            if (writable > 0) {
                long samplesPerChannel = Math.Min(Source.UpdateRate, writable);
                int valuesForMovement = (int)samplesPerChannel,
                    movementOffset = 0;
                while (valuesForMovement > 0) {
                    if (objectStreamPosition == objectStreamRate) {
                        IEnumerator<Source> enumerator = Source.ActiveSources.GetEnumerator();
                        int i = 0;
                        while (i < objects && enumerator.MoveNext()) {
                            (enumerator.Current.Position * scale).CopyTo(positionalBlock[i >> 4], 3 * i % objectStreamRate);
                            i++;
                        }
                        objectStreamPosition = 0;
                    }

                    int valuesFittingFrame = objectStreamRate - objectStreamPosition;
                    if (valuesFittingFrame > valuesForMovement) {
                        valuesFittingFrame = valuesForMovement;
                    }
                    valuesForMovement -= valuesFittingFrame;

                    for (int track = 0; track < positionalBlock.Length; track++) {
                        float[] source = positionalBlock[track];
                        int index = movementOffset * output.ChannelCount + // Time alignment
                            output.ChannelCount - positionalBlock.Length + track; // Track alignment
                        for (int sample = 0; sample < valuesFittingFrame; sample++) {
                            result[index] = source[objectStreamPosition + sample];
                            index += output.ChannelCount;
                        }
                    }
                    objectStreamPosition += valuesFittingFrame;
                    movementOffset += valuesFittingFrame;
                }
                output.WriteBlock(result, 0, samplesPerChannel * output.ChannelCount);
            }
            samplesWritten += Source.UpdateRate;
        }

        /// <inheritdoc/>
        public override void Dispose() {
            if (output == null) {
                CreateFile(); // Create an empty file if no data was written
            }

            base.Dispose();
            output.Dispose();
        }

        /// <summary>
        /// Lazy create the output file. The <see cref="Listener"/> might get initialized after the constructor.
        /// </summary>
        void CreateFile() {
            int objectTracksNeeded = (Source.ActiveSources.Count >> 4) + ((Source.ActiveSources.Count & 0b1111) != 0 ? 1 : 0);
            while (objectTracksNeeded-- != 0) {
                Source.AttachSource(new MuteSource(Source));
            }

            IReadOnlyCollection<Source> sources = Source.ActiveSources;
            Channel[] channels = new Channel[sources.Count];
            IEnumerator<Source> enumerator = sources.GetEnumerator();
            while (enumerator.MoveNext()) {
                if (enumerator.Current is MuteSource) {
                    break;
                }
                channels[objects++] = new Channel(enumerator.Current.Position != Vector3.Zero ?
                    enumerator.Current.Position : new Vector3(0, -1, 0), enumerator.Current.LFE);
            }
            for (int i = objects; i < channels.Length; i++) {
                channels[i] = new Channel(0, 0);
            }

            positionalBlock = new float[channels.Length - objects][];
            for (int track = 0; track < positionalBlock.Length; track++) {
                positionalBlock[track] = new float[objectStreamRate];
            }

            output = new LimitlessAudioFormatWriter(writer, length, Source.SampleRate, bits, channels);
            output.WriteHeader(true, objects);
        }

        /// <summary>
        /// The number of values required for position updates per position track. Position tracks always contain data for 16
        /// channels, even if those values are unused. Their precision depends on the bit depth, but the range is always [0;1].
        /// </summary>
        internal const int objectStreamRate = 16 * 3;
    }
}