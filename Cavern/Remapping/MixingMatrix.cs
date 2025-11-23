using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Cavern.Remapping {
    /// <summary>
    /// Contains a mixing matrix that maps a content (input) to a layout (output). A mixing matrix is a jagged array of multipliers
    /// for each output (playback) channel, with which the input (content) channels should be multiplied and mixed to that specific
    /// channel. The dimensions are [output channels][input channels]. The values are not decibels, but linear gains.
    /// </summary>
    public sealed class MixingMatrix : IList<float[]> {
        /// <inheritdoc/>
        public int Count => matrix.Length;

        /// <inheritdoc/>
        public bool IsReadOnly => true;

        /// <inheritdoc/>
        float[] IList<float[]>.this[int index] {
            get => matrix[index];
            set => throw new ReadOnlyException();
        }

        /// <summary>
        /// Get the input mixing levels for a given output channel.
        /// </summary>
        public float[] this[int outputChannel] => matrix[outputChannel];

        /// <summary>
        /// Mixing matrix values. The dimensions are [output channels][input channels], the values are linear gains.
        /// </summary>
        public readonly float[][] matrix;

        /// <summary>
        /// Constructs a mixing matrix for a given number of output and input channels.
        /// </summary>
        public MixingMatrix(int outputChannels, int inputChannels) {
            matrix = new float[outputChannels][];
            for (int i = 0; i < outputChannels; i++) {
                matrix[i] = new float[inputChannels];
            }
        }

        /// <summary>
        /// Encapsulates an already created mixing matrix.
        /// </summary>
        public MixingMatrix(params float[][] matrix) {
            for (int i = 1; i < matrix.Length; i++) {
                if (matrix[0].Length != matrix[i].Length) {
                    throw new DifferentInputChannelCountsException();
                }
            }
            this.matrix = matrix;
        }

        /// <inheritdoc/>
        public void Add(float[] item) => throw new InvalidOperationException();

        /// <inheritdoc/>
        public void Clear() => throw new InvalidOperationException();

        /// <inheritdoc/>
        public bool Contains(float[] item) => throw new InvalidOperationException();

        /// <inheritdoc/>
        public void CopyTo(float[][] array, int arrayIndex) => throw new InvalidOperationException();

        /// <inheritdoc/>
        public int IndexOf(float[] item) => throw new InvalidOperationException();

        /// <inheritdoc/>
        public void Insert(int index, float[] item) => throw new InvalidOperationException();

        /// <inheritdoc/>
        public bool Remove(float[] item) => throw new InvalidOperationException();

        /// <inheritdoc/>
        public void RemoveAt(int index) => throw new InvalidOperationException();

        /// <inheritdoc/>
        public IEnumerator<float[]> GetEnumerator() => ((IList<float[]>)matrix).GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => matrix.GetEnumerator();
    }
}
