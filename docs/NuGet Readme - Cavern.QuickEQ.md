# Cavern.QuickEQ
QuickEQ is a high performance, versatile audio measurement library, providing
the foundations for an automated, leading precision, heavily configurable
room correction software called Cavern QuickEQ.

[![Build Status](https://api.travis-ci.com/VoidXH/Cavern.svg?branch=master)](https://app.travis-ci.com/VoidXH/Cavern)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/VoidXH/Cavern)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/VoidXH/Cavern/latest)
![Lines of Code](https://img.shields.io/tokei/lines/github/VoidXH/Cavern "Lines of Code")

## Quick start
### Fast Fourier-Transform
The basis of most measurements, the FFT is in the main Cavern library, QuickEQ
is not even required. The `Measurements` class in the `Cavern.Utilities`
namespace has extension methods for `float` and `Complex` arrays. The following
FFT operations are available on both of them:
* `Complex[] FFT(samples)` - Get the FFT of the input.
  * To get the spectrum of a real signal, the `FFT1D` method is the fastest.
* `void InPlaceFFT(samples)` - Transform samples in-place (to spectrum for `float`s).
* `IFFT` and `InPlaceIFFT` are only available for `Complex` numbers by definition.

For optimal performance, the second parameter for each function can be an
`FFTCache` instance. These have to be constructed to the exact FFT size, and
will improve runtimes for multiple sequential transforms to the fraction of the
time without them. For software that run FFTs in multiple threads, using
`ThreadSafeFFTCache`s for each thread is mandatory.

### Equalization
EQ generation including FIR, zero-phase, and peaking filter versions is found in
the `Cavern.QuickEQ.Equalization` namespace. Target curves are available in the
`Cavern.QuickEQ.EQCurves` namespace.

### Signal generation
Noise and sweep generation with multichannel timing are available in the
`Cavern.QuickEQ.SignalGeneration` namespace.

### Graphing utilities
Displaying the spectrum of a measurement takes 3 steps, all of which are found
in `Cavern.QuickEQ.GraphUtils`:
* `ConvertToGraph` - Linear spectra have to be cut and converted to logarithmic scale.
* `ConvertToDecibels` - The Y axis also requires a logarithmic transformation.
* `SmoothGraph` - To mitigate heavy spectrum fluctuations by the nature of sound, smoothing helps a lot.

### CavernAmp
For massive performance improvements, compile CavernAmp and place its build next
to `Cavern.QuickEQ.dll`. It doesn't require any code changes, but it only runs
on Windows.

## Development documents
* [Scripting API](https://cavern.sbence.hu/cavern/doc.php?if=api/index) with descriptions of all public members for all public classes
