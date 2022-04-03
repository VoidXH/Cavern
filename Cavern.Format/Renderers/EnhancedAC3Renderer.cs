﻿using System;
using System.Numerics;

using Cavern.Format.Decoders;
using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Remapping;
using Cavern.SpecialSources;
using Cavern.Utilities;

namespace Cavern.Format.Renderers {
    /// <summary>
    /// Renders a decoded E-AC-3 stream with Cavern.
    /// </summary>
    public class EnhancedAC3Renderer : Renderer {
        /// <summary>TODO: TEMPORARY UNTIL THE DECODER IS IMPLEMENTED!</summary>
        public AudioReader Source { get; set; }

        /// <summary>
        /// The stream is object-based.
        /// </summary>
        readonly bool hasObjects;

        /// <summary>
        /// Samples for the rendered objects in the last update.
        /// </summary>
        readonly float[][] objectSamples;

        /// <summary>
        /// Aligned timeslot results for final output.
        /// </summary>
        readonly float[][] finalResult;

        /// <summary>
        /// Source stream.
        /// </summary>
        readonly EnhancedAC3Decoder stream;

        /// <summary>
        /// Creates the object mix from channels.
        /// </summary>
        readonly JointObjectCodingApplier applier;

        /// <summary>
        /// Sample supplier for the rendered objects.
        /// </summary>
        readonly StreamMaster reader;

        /// <summary>
        /// Last non-interpolated position of each object.
        /// </summary>
        readonly Vector3[] lastHoldPos;

        /// <summary>
        /// LFE channel samples from the last timeslot.
        /// </summary>
        float[] lfeTimeslot;

        /// <summary>
        /// LFE channel output.
        /// </summary>
        float[] lfeResult = new float[0];

        /// <summary>
        /// Object samples for the last rendered timeslot.
        /// </summary>
        float[][] timeslotResult;

        /// <summary>
        /// Sample in the last fetched <see cref="timeslotResult"/>.
        /// </summary>
        int timeslotPosition;

        /// <summary>
        /// Parse an E-AC-3 decoder to a renderer.
        /// </summary>
        public EnhancedAC3Renderer(EnhancedAC3Decoder stream) {
            this.stream = stream;
            ObjectAudioMetadata oamd = null;
            JointObjectCoding joc = null;
            if (stream.Extensions != null) {
                oamd = stream.Extensions.OAMD;
                joc = stream.Extensions.JOC;
            }
            hasObjects = oamd != null && joc != null;
            if (hasObjects) {
                objectSamples = new float[oamd.ObjectCount][];
                finalResult = new float[joc.ObjectCount][];
                lastHoldPos = new Vector3[oamd.ObjectCount];
                applier = new JointObjectCodingApplier(joc.ObjectCount, stream.FrameSize);
                reader = new StreamMaster(GetNextObjectSamples);
                for (int obj = 0; obj < oamd.ObjectCount; ++obj)
                    objects.Add(new StreamMasterSource(reader, obj));
                reader.SetupSources(objects, stream.SampleRate);
            }
        }

        /// <summary>
        /// Read the next <paramref name="samples"/> and update the objects.
        /// </summary>
        public override void Update(int samples) {
            if (hasObjects) {
                if (lfeResult.Length != samples) {
                    for (int obj = 0; obj < finalResult.Length; ++obj)
                        finalResult[obj] = new float[samples];
                    lfeResult = new float[samples];
                }
                int pointer = 0;
                while (pointer < samples) {
                    if (timeslotPosition == 0)
                        RenderNextTimeslot();
                    int next = Math.Min(samples - pointer, QuadratureMirrorFilterBank.subbands - timeslotPosition);
                    for (int obj = 0; obj < finalResult.Length; ++obj)
                        for (int i = 0; i < next; ++i)
                            finalResult[obj][pointer + i] = timeslotResult[obj][timeslotPosition + i];
                    for (int i = 0; i < next; ++i)
                        lfeResult[pointer + i] = lfeTimeslot[timeslotPosition + i];
                    pointer += next;
                    timeslotPosition += next;
                    if (timeslotPosition == QuadratureMirrorFilterBank.subbands)
                        timeslotPosition = 0;
                }

                int lfe = stream.Extensions.OAMD.GetLFEPosition();
                if (lfe != -1) {
                    for (int obj = 0; obj < lfe; ++obj)
                        objectSamples[obj] = finalResult[obj];
                    ReferenceChannel[] matrix = ChannelPrototype.StandardMatrix[stream.ChannelCount];
                    for (int i = 0; i < matrix.Length; ++i) {
                        if (matrix[i] == ReferenceChannel.ScreenLFE) {
                            objectSamples[lfe] = lfeResult;
                            break;
                        }
                    }
                    for (int obj = lfe + 1; obj < objectSamples.Length; ++obj)
                        objectSamples[obj] = finalResult[obj - 1];
                } else
                    for (int obj = 0; obj < objectSamples.Length; ++obj)
                        objectSamples[obj] = finalResult[obj];
            }
        }

        /// <summary>
        /// Update the objects and get the samples they need to render.
        /// </summary>
        public float[][] GetNextObjectSamples(int samples) {
            Update(samples);
            return objectSamples;
        }

        /// <summary>
        /// Render new object samples for the next timeslot
        /// </summary>
        void RenderNextTimeslot() {
            float[] input = new float[QuadratureMirrorFilterBank.subbands * stream.ChannelCount];
            stream.DecodeBlock(input, 0, input.LongLength);
            if (Source != null)
                Source.ReadBlock(input, 0, input.LongLength);
            float[][] grouped = WaveformUtils.InterlacedToMultichannel(input, stream.ChannelCount);

            if (hasObjects) {
                int offset = stream.Extensions.GetPayloadByID(ExtensibleMetadataDecoder.oamdPayloadID).SampleOffset;
                stream.Extensions.OAMD.UpdateSources(stream.LastFetchStart - offset, objects, lastHoldPos);

                ReferenceChannel[] matrix = ChannelPrototype.StandardMatrix[stream.ChannelCount];
                float[][] sources = new float[stream.Channels.Length][];
                for (int i = 0; i < sources.Length; ++i) {
                    for (int j = 0; j < matrix.Length; ++j) {
                        if (JointObjectCodingTables.inputMatrix[i] == matrix[j]) {
                            sources[i] = grouped[j];
                            break;
                        }
                    }
                }

                for (int i = 0; i < matrix.Length; ++i)
                    if (matrix[i] == ReferenceChannel.ScreenLFE)
                        lfeTimeslot = grouped[i];

                timeslotResult = applier.Apply(sources, stream.Extensions.JOC);
            }
        }
    }
}