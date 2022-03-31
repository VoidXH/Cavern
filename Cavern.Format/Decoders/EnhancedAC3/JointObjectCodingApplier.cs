using System;
using System.Threading.Tasks;

using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Converts a channel-based audio stream and JOC to object output samples.
    /// </summary>
    class JointObjectCodingApplier {
        /// <summary>
        /// Input channel count.
        /// </summary>
        readonly int channels;

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
        /// Previous JOC mixing matrix values.
        /// </summary>
        readonly float[][][] prevMatrix;

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
        /// Next timeslot to read in the <see cref="current"/> JOC.
        /// </summary>
        int timeslot;

        /// <summary>
        /// Creates a converter from a channel-based audio stream and JOC to object output samples.
        /// </summary>
        public JointObjectCodingApplier(int channels, int objects, int frameSize) {
            this.channels = channels;
            this.objects = objects;
            this.frameSize = frameSize;

            timeslotCache = new float[objects][];
            results = new Complex[channels][];
            qmfbCache = new Complex[objects][];
            for (int obj = 0; obj < objects; ++obj) {
                timeslotCache[obj] = new float[QuadratureMirrorFilterBank.subbands];
                qmfbCache[obj] = new Complex[QuadratureMirrorFilterBank.subbands];
            }

            int converterCount = Math.Max(channels, objects);
            converters = new QuadratureMirrorFilterBank[converterCount];
            for (int i = 0; i < converterCount; ++i)
                converters[i] = new QuadratureMirrorFilterBank();

            prevMatrix = new float[objects][][];
            for (int obj = 0; obj < objects; ++obj) {
                prevMatrix[obj] = new float[channels][];
                for (int ch = 0; ch < channels; ++ch)
                    prevMatrix[obj][ch] = new float[QuadratureMirrorFilterBank.subbands];
            }
        }

        /// <summary>
        /// Gets the audio samples of each object for the next timeslot.
        /// </summary>
        public float[][] Apply(float[][] input, JointObjectCoding actual) {
            if (timeslot == 0)
                mixMatrix = actual.GetMixingMatrices(frameSize, prevMatrix);
            Parallel.For(0, channels, ch => results[ch] = converters[ch].ProcessForward(input[ch]));
            Parallel.For(0, objects, obj => ProcessObject(obj, mixMatrix[obj][timeslot]));
            if (++timeslot == channels)
                timeslot = 0;
            return timeslotCache;
        }

        /// <summary>
        /// Mixes channel-based samples by a matrix to the objects.
        /// </summary>
        void ProcessObject(int obj, float[][] mixMatrix) {
            Array.Clear(qmfbCache[obj], 0, QuadratureMirrorFilterBank.subbands);
            for (int ch = 0; ch < channels; ++ch) {
                for (int sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb)
                    qmfbCache[obj][sb] += results[ch][sb] * mixMatrix[ch][sb];
            }
            converters[obj].ProcessInverse(qmfbCache[obj], timeslotCache[obj]);
        }
    }
}