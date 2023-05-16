using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

using Cavern.SpecialSources;

namespace Cavern.Format.Environment {
    /// <summary>
    /// Exports the rendered environment in an object-based Limitless Audio Format file.
    /// </summary>
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
        /// previous state is flushed to the file. One position collection for the next frame of each track.
        /// </summary>
        readonly float[][] positionalBlock;

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
            base(writer, ExtendWithMuteTarget(source, (source.ActiveSources.Count >> 4) +
                ((source.ActiveSources.Count & 0b1111) != 0 ? 1 : 0))) {
            IReadOnlyCollection<Source> sources = source.ActiveSources;
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

            output = new LimitlessAudioFormatWriter(writer, length, source.SampleRate, bits, channels);
            output.WriteHeader(true, objects);
        }

        /// <summary>
        /// Exports the rendered environment in an object-based Limitless Audio Format file.
        /// </summary>
        public LimitlessAudioFormatEnvironmentWriter(string path, Listener source, long length, BitDepth bits) :
            this(AudioWriter.Open(path), source, length, bits) { }

        /// <summary>
        /// Calling this for the base constructor is a shortcut to adding extra tracks which are wired as the object position tracks.
        /// </summary>
        static Listener ExtendWithMuteTarget(Listener source, int count) {
            while (count-- != 0) {
                source.AttachSource(new MuteSource(source));
            }
            return source;
        }

        /// <summary>
        /// Export the next frame of the <see cref="Source"/>.
        /// </summary>
        public override void WriteNextFrame() {
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

        /// <summary>
        /// Free the written file.
        /// </summary>
        public override void Dispose() {
            base.Dispose();
            output.Dispose();
        }

        /// <summary>
        /// The number of values required for position updates per position track. Position tracks always contain data for 16
        /// channels, even if those values are unused. Their precision depends on the bit depth, but the range is always [0;1].
        /// </summary>
        internal const int objectStreamRate = 16 * 3;
    }
}