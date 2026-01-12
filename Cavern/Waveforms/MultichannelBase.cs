using System;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.Waveforms {
    /// <summary>
    /// Contains multiple arrays of the same length.
    /// </summary>
    public abstract class MultichannelBase<T> : ICloneable {
        /// <summary>
        /// Get a <paramref name="channel"/>'s <see cref="data"/>.
        /// </summary>
        public T[] this[int channel] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data[channel];
        }

        /// <summary>
        /// The number of channels contained in this data.
        /// </summary>
        public int Channels {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data.Length;
        }

        /// <summary>
        /// The length of a single channel's <see cref="data"/>.
        /// </summary>
        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => data[0].Length;
        }

        /// <summary>
        /// Each channel's samples or other stored data.
        /// </summary>
        protected readonly T[][] data;

        /// <summary>
        /// Encapsulate multichannel data.
        /// </summary>
        public MultichannelBase(params T[][] source) {
            for (int i = 1; i < source.Length; i++) {
                if (source[0].LongLength != source[i].LongLength) {
                    throw new DifferentSignalLengthsException();
                }
            }
            data = source;
        }

        /// <summary>
        /// Construct an empty multichannel data of a given size.
        /// </summary>
        public MultichannelBase(int channels, int itemsPerChannel) {
            data = new T[channels][];
            for (int channel = 0; channel < channels; channel++) {
                data[channel] = new T[itemsPerChannel];
            }
        }

        /// <inheritdoc/>
        public abstract object Clone();

        /// <summary>
        /// Get an array referencing the contained channel entries.
        /// </summary>
        public T[][] ToArray() => data.FastClone();
    }
}
