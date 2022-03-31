using System;
using System.Threading.Tasks;

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
        /// Number of time division blocks for processing. A timeslot's length is the same as
        /// the number of <see cref="QuadratureMirrorFilterBank.subbands"/>.
        /// </summary>
        readonly int timeslots;

        /// <summary>
        /// Preallocated output sample arrays.
        /// </summary>
        readonly float[][] outCache;

        /// <summary>
        /// Preallocated forward transformation result holder.
        /// </summary>
        readonly Complex[][] results;

        /// <summary>
        /// Preallocated QMFB operation arrays.
        /// </summary>
        readonly Complex[][] qmfbCache;

        /// <summary>
        /// Preallocated QMFB transform objects.
        /// </summary>
        readonly QuadratureMirrorFilterBank[] converters;

        /// <summary>
        /// Creates a converter from a channel-based audio stream and JOC to object output samples.
        /// </summary>
        public JointObjectCodingApplier(int channels, int objects, int frameSize) {
            this.channels = channels;
            this.objects = objects;
            timeslots = frameSize / QuadratureMirrorFilterBank.subbands;

            outCache = new float[objects][];
            results = new Complex[channels][];
            qmfbCache = new Complex[objects][];
            for (int obj = 0; obj < objects; ++obj) {
                outCache[obj] = new float[frameSize];
                qmfbCache[obj] = new Complex[QuadratureMirrorFilterBank.subbands];
            }

            int converterCount = Math.Max(channels, objects);
            converters = new QuadratureMirrorFilterBank[converterCount];
            for (int i = 0; i < converterCount; ++i)
                converters[i] = new QuadratureMirrorFilterBank();
        }

        /// <summary>
        /// Gets the audio samples of each object for an audio frame.
        /// </summary>
        public float[][] Apply(float[][] input, float[][][][] mixMatrix) {
            for (int ts = 0; ts < timeslots; ++ts) {
                int firstSample = ts * QuadratureMirrorFilterBank.subbands;
                Parallel.For(0, channels, ch => results[ch] = converters[ch].ProcessForward(input[ch], firstSample));
                Parallel.For(0, objects, obj => ProcessObject(obj, mixMatrix[obj][ts], firstSample));
            }
            return outCache;
        }

        /// <summary>
        /// Mixes channel-based samples by a matrix to the objects.
        /// </summary>
        void ProcessObject(int obj, float[][] mixMatrix, int firstSample) {
            Array.Clear(qmfbCache[obj], 0, QuadratureMirrorFilterBank.subbands);
            for (int ch = 0; ch < channels; ++ch) {
                for (int sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb)
                    qmfbCache[obj][sb] += results[ch][sb] * mixMatrix[ch][sb];
            }
            converters[obj].ProcessInverse(qmfbCache[obj], outCache[obj], firstSample);
        }
    }
}