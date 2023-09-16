using System.Numerics;
using System.Runtime.CompilerServices;
using System;

namespace Cavern.Utilities {
    public static partial class Measurements {
        /// <summary>
        /// Actual FFT processing, somewhat in-place.
        /// </summary>
        static unsafe void ProcessFFT(Complex[] samples, FFTCache cache, int depth) {
            Complex[] even = cache.Even[depth],
                odd = cache.Odd[depth];
            fixed (Complex* pSamples = samples)
            fixed (Complex* pEven = even)
            fixed (Complex* pOdd = odd) {
                Complex* result = pSamples,
                    end = result + samples.Length,
                    evenRef = pEven,
                    oddRef = pOdd;
                while (result != end) {
                    *evenRef++ = *result++;
                    *oddRef++ = *result++;
                }
            }

            --depth;
            if (even.Length != 4) {
                ProcessFFT(even, cache, depth);
                ProcessFFT(odd, cache, depth);
            } else {
                ProcessFFT4(even);
                ProcessFFT4(odd);
            }

            fixed (Complex* pSamples = samples)
            fixed (Complex* pEven = even)
            fixed (Complex* pOdd = odd)
            fixed (float* pCosCache = FFTCache.cos[depth + 1])
            fixed (float* pSinCache = FFTCache.sin[depth + 1]) {
                int step = Vector<float>.Count >> 1;
                Vector2* result = (Vector2*)pSamples,
                    mirror = result + (samples.Length >> 1),
                    end = mirror,
                    endSimd = end - step,
                    evenRef = (Vector2*)pEven,
                    oddRef = (Vector2*)pOdd;
                float* cosCache = pCosCache,
                    sinCache = pSinCache;

                // SIMD pass
                while (result < endSimd) {
                    Vector<float> oddRight = new Vector<float>(new Span<float>((float*)oddRef - 1, Vector<float>.Count)),
                        oddVec = new Vector<float>(new Span<float>((float*)oddRef, Vector<float>.Count)),
                        oddLeft = new Vector<float>(new Span<float>((float*)oddRef + 1, Vector<float>.Count));

                    Vector<float> newOdd =
                    // At even offsets: real * cos - imag * sin
                        (oddVec * new Vector<float>(new Span<float>(cosCache, Vector<float>.Count)) -
                        oddLeft * new Vector<float>(new Span<float>(sinCache, Vector<float>.Count))) * FFTCache.evenMask
                    // At odd offsets: real * sin + imag * cos
                        + (oddRight * new Vector<float>(new Span<float>(sinCache, Vector<float>.Count)) +
                        oddVec * new Vector<float>(new Span<float>(cosCache, Vector<float>.Count))) * FFTCache.oddMask;

                    Vector<float> evenSource = new Vector<float>(new Span<float>((float*)evenRef, Vector<float>.Count));
                    *(Vector<float>*)result = evenSource + newOdd;
                    *(Vector<float>*)mirror = evenSource - newOdd;

                    result += step;
                    mirror += step;
                    evenRef += step;
                    oddRef += step;
                    cosCache += Vector<float>.Count;
                    sinCache += Vector<float>.Count;
                }

                // Slow pass
                while (result != end) {
                    float oddRealSource = oddRef->X,
                        oddImagSource = oddRef->Y,
                        cachedCos = *cosCache,
                        cachedSin = *sinCache;
                    Vector2 newOdd = new Vector2(oddRealSource * cachedCos - oddImagSource * cachedSin,
                        oddRealSource * cachedSin + oddImagSource * cachedCos);

                    *result = *evenRef + newOdd;
                    *mirror = *evenRef - newOdd;

                    result++;
                    mirror++;
                    evenRef++;
                    oddRef++;
                    cosCache += 2;
                    sinCache += 2;
                }
            }
        }

        /// <summary>
        /// Actual FFT processing without SIMD as Mono has no support for it, somewhat in-place.
        /// </summary>
        static unsafe void ProcessFFT_Mono(Complex[] samples, FFTCache cache, int depth) {
            Complex[] even = cache.Even[depth],
                odd = cache.Odd[depth];
            fixed (Complex* pSamples = samples)
            fixed (Complex* pEven = even)
            fixed (Complex* pOdd = odd) {
                Complex* result = pSamples,
                    end = result + samples.Length,
                    evenRef = pEven,
                    oddRef = pOdd;
                while (result != end) {
                    *evenRef++ = *result++;
                    *oddRef++ = *result++;
                }
            }

            --depth;
            if (even.Length != 4) {
                ProcessFFT_Mono(even, cache, depth);
                ProcessFFT_Mono(odd, cache, depth);
            } else {
                ProcessFFT4(even);
                ProcessFFT4(odd);
            }

            fixed (Complex* pSamples = samples)
            fixed (Complex* pEven = even)
            fixed (Complex* pOdd = odd)
            fixed (float* pCosCache = FFTCache.cos[depth + 1])
            fixed (float* pSinCache = FFTCache.sin[depth + 1]) {
                Complex* result = pSamples,
                    mirror = result + (samples.Length >> 1),
                    end = mirror,
                    evenRef = pEven,
                    oddRef = pOdd;
                float* cosCache = pCosCache,
                    sinCache = pSinCache;

                // Slow pass
                while (result != end) {
                    float evenReal = evenRef->Real,
                        evenImag = evenRef->Imaginary,
                        oddRealSource = oddRef->Real,
                        oddImagSource = oddRef->Imaginary,
                        cachedCos = *cosCache,
                        cachedSin = *sinCache,
                        oddReal = oddRealSource * cachedCos - oddImagSource * cachedSin,
                        oddImag = oddRealSource * cachedSin + oddImagSource * cachedCos;

                    result->Real = evenReal + oddReal;
                    result->Imaginary = evenImag + oddImag;
                    mirror->Real = evenReal - oddReal;
                    mirror->Imaginary = evenImag - oddImag;

                    result++;
                    mirror++;
                    evenRef++;
                    oddRef++;
                    cosCache += 2;
                    sinCache += 2;
                }
            }
        }

        /// <summary>
        /// Perform 4 sample FFTs in a hardcoded, most efficient way.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static unsafe void ProcessFFT4(Complex[] samples) {
            fixed (Complex* pSamples = samples) {
                Vector2 evenValue = *(Vector2*)pSamples,
                    oddValue = *(Vector2*)(pSamples + 2),
                    evenValue1 = evenValue + oddValue,
                    evenValue2 = evenValue - oddValue;

                evenValue = *(Vector2*)(pSamples + 1);
                oddValue = *(Vector2*)(pSamples + 3);
                Vector2 oddValue1 = evenValue + oddValue,
                    oddValue2 = evenValue - oddValue;
                oddValue2 = new Vector2(oddValue2.Y, -oddValue2.X);

                *(Vector2*)pSamples = evenValue1 + oddValue1;
                *(Vector2*)(pSamples + 1) = evenValue2 + oddValue2;
                *(Vector2*)(pSamples + 2) = evenValue1 - oddValue1;
                *(Vector2*)(pSamples + 3) = evenValue2 - oddValue2;
            }
        }

        /// <summary>
        /// Perform very small-size FFTs in a hardcoded, most efficient way.
        /// </summary>
        static unsafe void ProcessFFTSmall(Complex[] samples) {
            if (samples.Length == 4) {
                ProcessFFT4(samples);
            } else if (samples.Length == 2) {
                fixed (Complex* pSamples = samples) {
                    Vector2 evenValue = *(Vector2*)pSamples,
                        oddValue = *(Vector2*)(pSamples + 1);
                    *(Vector2*)pSamples = evenValue + oddValue;
                    *(Vector2*)(pSamples + 1) = evenValue - oddValue;
                }
            }
        }

        /// <summary>
        /// Fourier-transform a signal in 1D. The result is the spectral power.
        /// </summary>
        static void ProcessFFT(float[] samples, FFTCache cache) {
            int depth = QMath.Log2(samples.Length) - 1;
            if (samples.Length == 1) {
                return;
            }
            Complex[] even = cache.Even[depth], odd = cache.Odd[depth];
            for (int sample = 0, pair = 0; pair < samples.Length; sample++, pair += 2) {
                even[sample].Real = samples[pair];
                odd[sample].Real = samples[pair + 1];
            }

            if (CavernAmp.IsMono()) {
                ProcessFFT_Mono(even, cache, --depth);
                ProcessFFT_Mono(odd, cache, depth);
            } else {
                ProcessFFT(even, cache, --depth);
                ProcessFFT(odd, cache, depth);
            }

            float[] cosCache = FFTCache.cos[depth + 1],
                sinCache = FFTCache.sin[depth + 1];
            int halfLength = samples.Length >> 1;
            for (int i = 0; i < halfLength; i++) {
                float
                    cosRef = cosCache[i << 1],
                    sinRef = sinCache[i << 1],
                    oddReal = odd[i].Real * cosRef - odd[i].Imaginary * sinRef,
                    oddImag = odd[i].Real * sinRef + odd[i].Imaginary * cosRef,
                    real = even[i].Real + oddReal,
                    imaginary = even[i].Imaginary + oddImag;
                samples[i] = MathF.Sqrt(real * real + imaginary * imaginary);
                real = even[i].Real - oddReal;
                imaginary = even[i].Imaginary - oddImag;
                samples[i + halfLength] = MathF.Sqrt(real * real + imaginary * imaginary);
            }
        }
    }
}