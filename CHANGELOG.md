# Cavern version history
## Cavern 2.0 - Long-term Support update
**2023. April 25th**

#### Library additions
* Added MKV container writing
* Added MP4 container reading
* Added LAF environment export
* Added crossover export for Equalizer APO, supporting all of Cavern's basic crossovers
* Added FIR/IIR filter set export for supported software/hardware, including CamillaDSP
* Merging of multiple E-AC-3 files for channel-based spatial mix storage
* Microphone support for WebGL

#### Cavernize additions
* Added a metadata viewer
* Added 4.1.1 channel layout
* Content grading
* New render target selector
* Option to swap side/rear channels
* Support opening webm/weba files

## Cavern 1.6 - Happiest Halloween update
**2022. October 31st**

#### Additions
* Decoding of Matroska (.mkv/.mka) streams
* Decoding of E-AC-3 (.ac3/.ec3) streams, including JOC (Dolby Digital+ Atmos) into objects
* Decoding of ADM BWF (.wav) streams into objects
* CavernAmp accelerates some features with native code on 64-bit Windows for an average of 2x speedup
* Ear canal simulations by angle and distance with the Distancer filter
* Optimized convolution for basic impulse responses called Spike convolver
* Screen-locked source flag
* Many new waveform and audio file handling functions

#### Changes
* File reading is separated to reading, decoding, and rendering
* Linearized virtualizer frequency responses
* Moved API to .NET 6 and made NuGet-ready

## Cavern 1.5 - Nearly 5 years update
**2021. January 5th**

#### Additions
* Valley correction in QuickEQ
* Gain and Cavernize filters
* Phase, RT60, and imulse likelyness for impulse responses
* 24-bit support for known formats, WAV reader
* FRD calibration support

#### Changes
* Impulse responses have way less noise
* Versatile convolution generation
* Headphone virtualization is available outside Unity
* Many refactors

## Cavern 1.4 - Standalone update
**2020. May 23rd**

#### Additions
* Cavern is no longer bound to Unity, separated to module DLLs, CavernUnity.dll is only an adapter
* Completely separated rendering to Unity's audio thread
* New, abstract, and complex filters
* Sources can be filtered individually
* EQ curves
* Zero delay (minimum phase) or linear phase convolution EQ generation

#### Changes
* The center channel now has an echo in virtualization

## Cavern 1.3 - HRIR and Cavernize Lite update
**2019. March 16th**

#### Additions
* HRIR-based headphone virtualizer
* Cavernize Lite is now open source and included in this repo
* Variable band smoothing
* Implemented more Unity functions
* Support for virtual, even streaming audio sources with forced playback
* Noise generator audio source

#### Changes
* Greatly optimized code
* Virtualization does not override echo settings for audio sources
* The default sample rate is now 48 kHz (which is the sample rate of the HRIR)
* New set of supported DCPs

## Cavern 1.2 - QuickEQ update
**2018. September 30th**

#### Additions
* QuickEQ: advanced room correction toolkit with extremely quick and accurate measurements
* Audio spoofer: converts Unity's listeners and sources to Cavern components
* Log display debug window

#### Changes
* Rewoked resampling
* All C# code is now managed

## Cavern 1.1 - DCP update
**2018. May 10th**

* Multi-threaded and more optimized renderer
* Auto 7.1 upmix in Cavernize if rear channels are present
* Support for Cavern DCPs, Barco Auro, and 12-Track

## Cavern 1.0
### v1.0.3
**2018. February 5th**

#### Additions
* Size property for audio sources
* Audio file writers for RIFF Wave and Limitless Audio Format
* Documentation files
* Audio input handling
* Unity Editor scripts
* Various small features

#### Changes
* Panning is now constant power above high quality or in real time
* Utilities moved to their own namespace
* Various optimizations

### v1.0.2
**2017. September 27th**

* Added Debug Monitor, which displays all the objects and room bounds around an object
* Added Array Levels window, the standard volume display for object-based audio mixing
* Atmospheres are now visualizable
* Various fixes and optimizations

### v1.0.1
**2017. September 9th**

* Added cinema processor fader matching for CavernizeRealtime, which is an important setting in cinema environments, simplifying the control process
* Improved Levels debug window with automatic channel grouping and coloring (as seen in the Cavern driver)
* Various cleanups and optimizations

### v1.0
**2017. August 7th**

Initial release.

### Beta
**2016. February 19th**

### Alpha
**2016. January 17th**
