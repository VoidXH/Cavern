using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Converts a channel-based audio stream and JOC to object output samples.
    /// </summary>
    class JointObjectCodingApplier {
        /// <summary>
        /// Length of an AC-3 frame.
        /// </summary>
        readonly int frameSize;

        /// <summary>
        /// Recycled timeslot object output arrays.
        /// </summary>
        readonly float[][] timeslotCache;

        /// <summary>
        /// Recycled forward transformation result holder. Vectors are used for their SIMD properties.
        /// </summary>
        readonly Vector2[][] results;

        /// <summary>
        /// Recycled QMFB operation arrays. Vectors are used for their SIMD properties.
        /// </summary>
        readonly Vector2[][] qmfbCache;

        /// <summary>
        /// Recycled QMFB transform objects.
        /// </summary>
        readonly QuadratureMirrorFilterBank[] converters;

        /// <summary>
        /// Channels to objects matrix.
        /// </summary>
        float[][][][] mixMatrix;

        /// <summary>
        /// Next timeslot to read in the current JOC.
        /// </summary>
        int timeslot;

        /// <summary>
        /// Creates a converter from a channel-based audio stream and JOC to object output samples.
        /// </summary>
        public JointObjectCodingApplier(JointObjectCoding joc, int frameSize) {
            int maxChannels = JointObjectCodingTables.inputMatrix.Length;
            int objects = joc.ObjectCount;
            this.frameSize = frameSize;

            timeslotCache = new float[objects][];
            results = new Vector2[maxChannels][];
            qmfbCache = new Vector2[objects][];
            for (int obj = 0; obj < objects; ++obj) {
                timeslotCache[obj] = new float[QuadratureMirrorFilterBank.subbands];
                qmfbCache[obj] = new Vector2[QuadratureMirrorFilterBank.subbands];
            }

            int converterCount = Math.Max(maxChannels, objects);
            converters = new QuadratureMirrorFilterBank[converterCount];
            for (int i = 0; i < converterCount; ++i) {
                converters[i] = new QuadratureMirrorFilterBank();
            }
        }

        /// <summary>
        /// Gets the audio samples of each object for the next timeslot.
        /// </summary>
        public float[][] Apply(float[][] input, JointObjectCoding joc) {
            if (timeslot == 0) {
                mixMatrix = joc.GetMixingMatrices(frameSize);
            }

            // Forward transformations
            int runs = joc.ChannelCount;
            using (ManualResetEvent reset = new ManualResetEvent(false)) {
                for (int ch = 0; ch < joc.ChannelCount; ++ch) {
                    ThreadPool.QueueUserWorkItem(
                       new WaitCallback(channel => {
                           int ch = (int)channel;
                           results[ch] = converters[ch].ProcessForward(input[ch]);
                           if (Interlocked.Decrement(ref runs) == 0) {
                               reset.Set();
                           }
                       }), ch);
                }
                reset.WaitOne();
            }

            // Inverse transformations
            int objects = joc.ObjectCount;
            runs = objects;
            using (ManualResetEvent reset = new ManualResetEvent(false)) {
                for (int obj = 0; obj < objects; ++obj) {
                    ThreadPool.QueueUserWorkItem(
                       new WaitCallback(objectId => {
                           int obj = (int)objectId;
                           ProcessObject(joc, obj, mixMatrix[obj][timeslot], joc.Gain);
                           if (Interlocked.Decrement(ref runs) == 0) {
                               reset.Set();
                           }
                       }), obj);
                }
                reset.WaitOne();
            }

            if (++timeslot == input.Length) {
                timeslot = 0;
            }
            return timeslotCache;
        }

        /// <summary>
        /// Mixes channel-based samples by a matrix to the objects.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProcessObject(JointObjectCoding joc, int obj, float[][] mixMatrix, float gain) {
            Array.Clear(qmfbCache[obj], 0, QuadratureMirrorFilterBank.subbands);
            Vector2[] objCache = qmfbCache[obj];
            for (int ch = 0; ch < joc.ChannelCount; ++ch) {
                Vector2[] channelResult = results[ch];
                float[] channelMatrix = mixMatrix[ch];
                for (int sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb) {
                    objCache[sb] += channelResult[sb] * channelMatrix[sb];
                }
            }
            converters[obj].ProcessInverse(qmfbCache[obj], timeslotCache[obj]);
            if (gain != 1) {
                WaveformUtils.Gain(timeslotCache[obj], gain);
            }
        }
    }
}