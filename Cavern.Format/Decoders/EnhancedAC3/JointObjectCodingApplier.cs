using System;
using System.Threading;

using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Converts a channel-based audio stream and JOC to object output samples.
    /// </summary>
    class JointObjectCodingApplier {
        /// <summary>
        /// Output object count.
        /// </summary>
        readonly int objects;

        /// <summary>
        /// Length of an AC-3 frame.
        /// </summary>
        readonly int frameSize;

        /// <summary>
        /// Recycled timeslot object output arrays.
        /// </summary>
        readonly float[][] timeslotCache;

        /// <summary>
        /// Recycled forward transformation result holder.
        /// </summary>
        readonly Complex[][] results;

        /// <summary>
        /// Recycled QMFB operation arrays.
        /// </summary>
        readonly Complex[][] qmfbCache;

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
        public JointObjectCodingApplier(int objects, int frameSize) {
            int maxChannels = JointObjectCodingTables.inputMatrix.Length;
            this.objects = objects;
            this.frameSize = frameSize;

            timeslotCache = new float[objects][];
            results = new Complex[maxChannels][];
            qmfbCache = new Complex[objects][];
            for (int obj = 0; obj < objects; ++obj) {
                timeslotCache[obj] = new float[QuadratureMirrorFilterBank.subbands];
                qmfbCache[obj] = new Complex[QuadratureMirrorFilterBank.subbands];
            }

            int converterCount = Math.Max(maxChannels, objects);
            converters = new QuadratureMirrorFilterBank[converterCount];
            for (int i = 0; i < converterCount; ++i)
                converters[i] = new QuadratureMirrorFilterBank();
        }

        /// <summary>
        /// Gets the audio samples of each object for the next timeslot.
        /// </summary>
        public float[][] Apply(float[][] input, JointObjectCoding actual) {
            if (timeslot == 0)
                mixMatrix = actual.GetMixingMatrices(frameSize);

            // Forward transformations
            int runs = actual.ChannelCount;
            using (ManualResetEvent reset = new ManualResetEvent(false)) {
                for (int ch = 0; ch < actual.ChannelCount; ++ch) {
                    ThreadPool.QueueUserWorkItem(
                       new WaitCallback(channel => {
                           int ch = (int)channel;
                           results[ch] = converters[ch].ProcessForward(input[ch]);
                           if (Interlocked.Decrement(ref runs) == 0)
                               reset.Set();
                       }), ch);
                }
                reset.WaitOne();
            }

            // Inverse transformations
            runs = objects;
            using (ManualResetEvent reset = new ManualResetEvent(false)) {
                for (int obj = 0; obj < objects; ++obj) {
                    ThreadPool.QueueUserWorkItem(
                       new WaitCallback(objectId => {
                           int obj = (int)objectId;
                           ProcessObject(actual, obj, mixMatrix[obj][timeslot]);
                           if (Interlocked.Decrement(ref runs) == 0)
                               reset.Set();
                       }), obj);
                }
                reset.WaitOne();
            }

            if (++timeslot == input.Length)
                timeslot = 0;
            return timeslotCache;
        }

        /// <summary>
        /// Mixes channel-based samples by a matrix to the objects.
        /// </summary>
        void ProcessObject(JointObjectCoding joc, int obj, float[][] mixMatrix) {
            if (joc.ObjectActive[obj]) {
                Array.Clear(qmfbCache[obj], 0, QuadratureMirrorFilterBank.subbands);
                for (int ch = 0; ch < joc.ChannelCount; ++ch)
                    for (int sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb)
                        qmfbCache[obj][sb] += results[ch][sb] * mixMatrix[ch][sb];
                for (int sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb) {
                    if (!qmfbCache[obj][sb].IsZero()) { // Only render when there's anything to render
                        converters[obj].ProcessInverse(qmfbCache[obj], timeslotCache[obj]);
                        return;
                    }
                }
            }
            Array.Clear(timeslotCache[obj], 0, QuadratureMirrorFilterBank.subbands);
        }
    }
}