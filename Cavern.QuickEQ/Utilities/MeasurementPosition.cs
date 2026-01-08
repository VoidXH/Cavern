using System;
using System.Linq;

using Cavern.QuickEQ.Measurement;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Utilities {
    /// <summary>
    /// Contains the transfer functions of all channels at a single measurement position.
    /// </summary>
    public readonly struct MeasurementPosition : ICloneable {
        /// <summary>
        /// A single channel's transfer function.
        /// </summary>
        public Complex[] this[int index] => transferFunctions[index];

        /// <summary>
        /// Number of measured channels at this position.
        /// </summary>
        public int Length => transferFunctions.Length;

        /// <summary>
        /// Transfer functions of all channels at a single measurement position.
        /// </summary>
        readonly Complex[][] transferFunctions;

        /// <summary>
        /// Contains the transfer functions of all channels at a single measurement position.
        /// </summary>
        public MeasurementPosition(Complex[][] transferFunctions) => this.transferFunctions = transferFunctions;

        /// <summary>
        /// Calculate the transfer functions from the passed <paramref name="position"/>'s impulse responses.
        /// </summary>
        public MeasurementPosition(MultichannelWaveform position, bool multithreaded) {
            using FFTCachePool pool = new FFTCachePool(QMath.Base2Ceil(position.Length));
            transferFunctions = ParseMultichannel(position, multithreaded, pool);
        }

        /// <summary>
        /// Calculate the transfer functions from the passed <paramref name="position"/>'s impulse responses.
        /// </summary>
        public MeasurementPosition(MultichannelWaveform position, bool multithreaded, FFTCachePool pool) =>
            transferFunctions = ParseMultichannel(position, multithreaded, pool);

        /// <summary>
        /// Convert a <see cref="MeasuredPosition"/> to <see cref="MeasurementPosition"/>.
        /// </summary>
        public MeasurementPosition(MeasuredPosition position, bool multithreaded) {
            MultichannelWaveform source = new MultichannelWaveform(position.ImpulseResponses.Select(x => x.Response).ToArray());
            using FFTCachePool pool = new FFTCachePool(QMath.Base2Ceil(source.Length));
            transferFunctions = ParseMultichannel(source, multithreaded, pool);
        }

        /// <summary>
        /// Convert a <see cref="MeasuredPosition"/> to <see cref="MeasurementPosition"/>.
        /// </summary>
        public MeasurementPosition(MeasuredPosition position, bool multithreaded, FFTCachePool pool) {
            MultichannelWaveform source = new MultichannelWaveform(position.ImpulseResponses.Select(x => x.Response).ToArray());
            transferFunctions = ParseMultichannel(source, multithreaded, pool);
        }

        /// <summary>
        /// Parse a single <paramref name="impulseResponse"/> as a transfer function using an FFT cache <paramref name="pool"/>.
        /// </summary>
        static Complex[] ParseChannel(float[] impulseResponse, FFTCachePool pool) {
            FFTCache cache = pool.Lease();
            Complex[] result = impulseResponse.FFT(cache);
            pool.Return(cache);
            return result;
        }

        /// <summary>
        /// Parse a set of impulse responses either <paramref name="multithreaded"/> or not, using a pre-made <paramref name="pool"/>.
        /// </summary>
        static Complex[][] ParseMultichannel(MultichannelWaveform position, bool multithreaded, FFTCachePool pool) {
            Complex[][] result = new Complex[position.Channels][];
            if (multithreaded) {
                Parallelizer.ForUnchecked(0, position.Channels, i => {
                    result[i] = ParseChannel(position[i], pool);
                });
            } else {
                for (int i = 0; i < position.Channels; i++) {
                    result[i] = ParseChannel(position[i], pool);
                }
            }
            return result;
        }

        /// <summary>
        /// Knowing the channel <paramref name="layout"/>, select only the LFE channels.
        /// </summary>
        public MeasurementPosition GetLFEs(Channel[] layout) =>
            new MeasurementPosition(transferFunctions.Where((_, i) => layout[i].LFE).Select(x => x.FastClone()).ToArray());

        /// <summary>
        /// Create a simulation of all channels playing the same impulse together.
        /// </summary>
        public Complex[] Simulate() {
            Complex[] result = new Complex[transferFunctions[0].Length];
            result.Clear();
            for (int i = 0; i < transferFunctions.Length; i++) {
                result.Add(transferFunctions[i]);
            }
            return result;
        }

        /// <summary>
        /// Create a deep copy of this measurement position.
        /// </summary>
        public object Clone() => new MeasurementPosition(transferFunctions.DeepCopy2D());
    }
}