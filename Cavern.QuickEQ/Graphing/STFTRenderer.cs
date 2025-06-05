using System;
using System.Linq;

using Cavern.Utilities;

// TODO: move ReRender to abstract and support it in this renderer too, not all steps have to be recalculated every time
namespace Cavern.QuickEQ.Graphing {
    /// <summary>
    /// Perform moving window measurements and display the results as a spectrogram.
    /// When multiple impulse responses are added, the sum is measured.
    /// </summary>
    public class STFTRenderer : DrawableMeasurement {
        /// <summary>
        /// The difference between the highest and lowest displayed values in decibels.
        /// </summary>
        public float DynamicRange {
            get => dynamicRange;
            set {
                dynamicRange = value;
                ReRenderFull();
            }
        }
        float dynamicRange = 40;

        /// <summary>
        /// FFT band value to show as peak. If smaller than the actual peak in the data, it will be overridden.
        /// </summary>
        public float Peak {
            get => peak;
            set {
                peak = value;
                ReRenderFull();
            }
        }
        float peak = 0;

        /// <summary>
        /// Sample rate of the impulse responses to be added.
        /// </summary>
        public int SampleRate {
            get => sampleRate;
            set {
                sampleRate = value;
                ReRenderFull();
            }
        }
        int sampleRate = Listener.DefaultSampleRate;

        /// <summary>
        /// Size of the rolling FFT window.
        /// </summary>
        public int FFTSize {
            get => fftSize;
            set {
                fftSize = value;
                ReRenderFull();
            }
        }
        int fftSize = 4096;

        /// <summary>
        /// Mipmapping is reducing the FFT size when a lesser resolution is sufficient. This results in better temporal accuracy.
        /// </summary>
        public int FFTMipmaps {
            get => fftMipmaps;
            set {
                fftMipmaps = value;
                ReRenderFull();
            }
        }
        int fftMipmaps = 1;

        /// <summary>
        /// Window is moving this many samples between distinct lines on the image.
        /// </summary>
        public int Precision {
            get => precision;
            set {
                precision = value;
                ReRenderFull();
            }
        }
        int precision = 128;

        /// <summary>
        /// Cut off this many samples from the beginning of the measurement.
        /// </summary>
        public int Offset {
            get => offset;
            set {
                offset = value;
                ReRenderFull();
            }
        }
        int offset;

        /// <summary>
        /// Evaluate and display this many seconds.
        /// </summary>
        public float TimeSpan {
            get => timeSpan;
            set {
                timeSpan = value;
                ReRenderFull();
            }
        }
        float timeSpan = 1;

        /// <summary>
        /// Windowing used for temporal smoothing.
        /// </summary>
        public Window WindowFunction {
            get => windowFunction;
            set {
                windowFunction = value;
                ReRenderFull();
            }
        }
        Window windowFunction = Window.BlackmanHarris;

        /// <summary>
        /// Sum of impulse responses added to the measurement.
        /// </summary>
        float[] system;

        /// <summary>
        /// Perform moving window measurements and display the results as a spectrogram.
        /// </summary>
        public STFTRenderer(int width, int height) : base(width, height) { }

        /// <summary>
        /// Remove all data sources from the image, while keeping the peak value.
        /// To reset the peak and make the next render use the full band of colors, use <see cref="Clear(bool)"/> instead.
        /// </summary>
        public override void Clear() => Clear(false);

        /// <summary>
        /// Remove all data sources from the image, optionally keeping the peak value.
        /// </summary>
        public void Clear(bool keepPeak) {
            system = null;
            Array.Clear(Pixels, 0, Pixels.Length);
            if (!keepPeak) {
                peak = 0;
            }
        }

        /// <summary>
        /// Append a new impulse response to the measurement.
        /// </summary>
        public void AddImpulseResponse(float[] impulse) => AddImpulseResponse(impulse, true);

        /// <summary>
        /// Append multiple new impulse responses to the measurement.
        /// </summary>
        public void AddImpulseResponses(params float[][] impulses) {
            for (int i = 0; i < impulses.Length; i++) {
                AddImpulseResponse(impulses[i], false);
            }
            ReRenderFull();
        }

        /// <inheritdoc/>
        protected override void ReRenderFull() {
            if (EndFrequency > SampleRate / 2) {
                throw new ArgumentOutOfRangeException(nameof(EndFrequency), "The end frequency is more than the Nyquist frequency.");
            }

            if (system == null) {
                return;
            }

            Overlay?.DrawBehind(this);

            FFTCachePool[] pools = new FFTCachePool[fftMipmaps];
            for (int i = 0; i < fftMipmaps; i++) {
                pools[i] = new FFTCachePool(FFTSize >> i);
            }

            int rows = (int)(timeSpan * sampleRate / precision);
            float[][] stfts = new float[rows][];
            Parallelizer.For(0, rows, row => {
                float[][] ffts = pools.Select(x => CalculateRow(x, row)).ToArray();
                float[] result = MergeRow(ffts);
                float localMax = WaveformUtils.GetPeak(result);
                lock (pools[0]) {
                    if (peak < localMax) {
                        peak = localMax;
                    }
                }
                stfts[row] = result;
            });

            for (int i = 0; i < fftMipmaps; i++) {
                pools[i].Dispose();
            }

            NormalizeSTFTs(stfts, rows, 1 / peak);
            DrawSpectogram(stfts);
            Overlay?.DrawOn(this);
        }

        /// <summary>
        /// Append a new impulse response to the measurement.
        /// </summary>
        void AddImpulseResponse(float[] impulse, bool redraw) {
            if (system == null) {
                system = new float[impulse.Length];
            } else if (system.Length != impulse.Length) {
                throw new ArgumentException("Impulse responses added to an STFT renderer must be of the same length.", nameof(impulse));
            }

            WaveformUtils.Mix(impulse, system, impulse.Length);
            if (redraw) {
                ReRenderFull();
            }
        }

        /// <summary>
        /// Perform a single size FFT pass on a time slice of the <see cref="system"/>.
        /// </summary>
        float[] CalculateRow(FFTCachePool fftPool, int row) {
            float[] result = new float[fftPool.Size];
            int systemFrom = row * precision + offset;
            int copySize = Math.Min(fftPool.Size, system.Length - systemFrom);
            if (copySize <= 0) {
                return result;
            }

            Array.Copy(system, systemFrom, result, 0, copySize);
            Windowing.ApplyWindow(result, WindowFunction);
            FFTCache cache = fftPool.Lease();
            Measurements.InPlaceFFT(result, cache);
            fftPool.Return(cache);
            return result;
        }

        /// <summary>
        /// Use the results of multiple FFTs made by <see cref="CalculateRow"/> to create a time-domain optimized spectogram row.
        /// </summary>
        float[] MergeRow(float[][] ffts) {
            float[] result = new float[ffts[0].Length];
            if (Logarithmic) {
                int nyquist = result.Length / 2;
                float mul = 1f / nyquist;
                for (int i = 0; i < nyquist; i++) {
                    float fractionalDepth = ffts.Length * MathF.Pow(i * mul, .33f);
                    int depth = (int)fractionalDepth;
                    result[i] = ffts[depth][i / (1 << depth)];

                    // Fading to remove the stairstep effect of instant cuts to smaller FFTs
                    if (depth > 0) {
                        int prevDepth = depth - 1;
                        float prevValue = ffts[prevDepth][i / (1 << prevDepth)];
                        result[i] = QMath.Lerp(prevValue, result[i], fractionalDepth % 1);
                    }
                }
            } else {
                // TODO: just choose the smallest resolution that fills every pixel
                throw new NotImplementedException("Linear frequency scale is not implemented for STFT rendering.");
            }
            return result;
        }

        /// <summary>
        /// Using 1 / maximum value as <paramref name="gain"/>, normalize the <paramref name="stfts"/> to the [0;1] range for easy color mapping.
        /// </summary>
        void NormalizeSTFTs(float[][] stfts, int rows, float gain) => Parallelizer.ForUnchecked(0, rows, row => {
            WaveformUtils.Gain(stfts[row], gain);
        });

        /// <summary>
        /// Produce the spectrogram image from the previously calculated <paramref name="stfts"/>.
        /// </summary>
        void DrawSpectogram(float[][] stfts) {
            float dynamicRangeScaler = 1f / dynamicRange;
            Parallelizer.For(0, Height, row => {
                float[] pixels = stfts[stfts.Length * row / Height];
                int offset = row * Width;
                if (Logarithmic) {
                    double mul = Math.Pow(10, (Math.Log10(EndFrequency) - Math.Log10(StartFrequency)) / (Width - 1));
                    double pixelIndex = FFTSize * StartFrequency / sampleRate;
                    for (int column = 0; column < Height; column++) {
                        int currentIndex = (int)pixelIndex;
                        float currentValue = QMath.Lerp(pixels[currentIndex], pixels[currentIndex + 1], (float)pixelIndex % 1);
                        pixelIndex *= mul;
                        int nextIndex = (int)pixelIndex;
                        if (currentIndex + 1 < nextIndex) {
                            currentValue = QMath.Average(pixels, currentIndex, nextIndex);
                        }
                        currentValue = Math.Max((20 * MathF.Log10(currentValue) + dynamicRange) * dynamicRangeScaler, 0);
                        Pixels[offset + column] = GetColorForValue(currentValue);
                    }
                } else {
                    throw new NotImplementedException("Linear frequency scale is not implemented for STFT rendering.");
                }
            });
        }

        /// <summary>
        /// Get the corresponding color for a given band gain.
        /// </summary>
        uint GetColorForValue(float value) {
            if (value < .25f) { // From black to cyan
                byte gb = (byte)(value * 1023);
                return 0xFF000000 | (uint)(gb << 8) | gb;
            } else if (value < .5f) { // From cyan to green
                byte b = (byte)((value - .25f) * 1023);
                return 0xFF00FFFF - b;
            } else if (value < .75f) {
                // From green to yellow
                byte r = (byte)((value - .5f) * 1023);
                return 0xFF00FF00 | (uint)(r << 16);
            } else { // From yellow to red
                byte r = (byte)((value - .75f) * 1023);
                return 0xFFFFFF00 - r << 8;
            }
        }
    }
}
