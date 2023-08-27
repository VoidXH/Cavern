# Cavern
Cavern is a fully adaptive object-based audio rendering engine and (up)mixer
without limitations for home, cinema, and stage use. Audio transcoding and
self-calibration libraries built on the Cavern engine are also available.
This repository also features a Unity plugin and a standalone converter called
Cavernize.

[![Build Status](https://api.travis-ci.com/VoidXH/Cavern.svg?branch=master)](https://app.travis-ci.com/VoidXH/Cavern)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/VoidXH/Cavern)](https://github.com/VoidXH/Cavern/releases/latest)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/VoidXH/Cavern/latest)
![Lines of Code](https://img.shields.io/tokei/lines/github/VoidXH/Cavern "Lines of Code")
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/087eefd2734a48c08e6b4b1006f84021)](https://app.codacy.com/gh/VoidXH/Cavern)

[![NuGet - Cavern](https://img.shields.io/nuget/v/Cavern?label=NuGet%3A%20Cavern)](https://www.nuget.org/packages/Cavern/)
[![NuGet - Cavern.Format](https://img.shields.io/nuget/v/Cavern.Format?label=NuGet%3A%20Cavern.Format)](https://www.nuget.org/packages/Cavern.Format/)
[![NuGet - Cavern.QuickEQ](https://img.shields.io/nuget/v/Cavern.QuickEQ?label=NuGet%3A%20Cavern.QuickEQ)](https://www.nuget.org/packages/Cavern.QuickEQ/)

## Features
* Unlimited objects and output channels without position restrictions
* Audio transcoder library with a custom spatial format
* Supported codecs:
  * E-AC-3 with Joint Object Coding (Dolby Digital Plus Atmos)
  * Limitless Audio Format
  * RIFF WAVE
  * Audio Definition Model Broadcast Wave Format
  * Supported containers: .ac3, .eac3, .ec3, .laf, .m4a, .m4v, .mka, .mkv, .mov, .mp4, .qt, .wav, .weba, .webm
* Advanced self-calibration with a microphone
  * Results in close to perfectly flat frequency response, <0.01 dB and <0.01 ms of uniformity
  * Uniformity can be achieved without a calibration file
  * Supported software/hardware for EQ/filter set export:
    * PC: Equalizer APO, CamillaDSP
    * DSP: MiniDSP 2x4 Advanced, MiniDSP 2x4 HD, MiniDSP DDRC-88A
    * Processors: Emotiva, StormAudio
    * Amplifiers: Behringer NX series
    * Others: Audyssey MultEQ-X, Dirac Live, YPAO
* Direction and distance virtualization for headphones
* Real-time upconversion of regular surround sound mixes to 3D
* Mix repositioning based on occupied seats
* Seat movement generation
* Ultra low latency, even the upconverter can work from as low as one sample per frame
* Unity-like listener and source functionality
* Fixes for Unity's Microphone API
  * Works in WebGL too

## User documentation
User documentation can be found at the [Cavern documentation webpage](http://cavern.sbence.hu/cavern/doc.php).
Please go to this page for basic setup, in-depth QuickEQ tutorials, and
command-line arguments.

The full list of changes for each version can be found in [CHANGELOG.md](./CHANGELOG.md).

## How to build
### Cavern
Cavern is a .NET Standard project with no dependencies. Open the `Cavern.sln`
solution with Microsoft Visual Studio 2022 or later and all projects should
build.

### Sample projects
These examples use the Cavern library to show how it works. The solution
containing all sample projects is found at `CavernSamples/CavernSamples.sln`.
The same build instructions apply as to the base project.

Single-purpose sample codes are found under `docs/Code`.

### Cavern for Unity
Open the `CavernUnity DLL.sln` solution with Microsoft Visual Studio 2022.
Remove the references from the CavernUnity DLL project to UnityEngine and
UnityEditor. Add these files from your own Unity installation as references.
They are found in `Editor\Data\Managed` under Unity's installation folder.

### CavernAmp
This is a Code::Blocks project, set up for the MingW compiler. No additional
libraries were used, this is standard C++ code, so importing just the .cpp and
.h files into any IDE will work perfectly.

## Library quick start
### Clip
Cavern is using audio clips to render the audio scene. A `Clip` is basically a
single audio file, which can be an effect or music. The easiest method of
loading from a file is through the `Cavern.Format` library, which will
auto-detect the format:
```
Clip clip = AudioReader.ReadClip(pathToFile);
```
Refer to the [scripting API](http://cavern.sbence.hu/cavern/doc.php?if=api/Cavern/Clip/index)
for the complete description of this object.

### Listener
The `Listener` is the center of the sound stage, which will render the audio
sources attached to it. The listener has a `Position` and `Rotation` (Euler
angles, degrees) field for spatial placement. All sources will be rendered
relative to it. Here's its creation:
```
Listener listener = new Listener() {
    SampleRate = 48000, // Match this with your output
    UpdateRate = 256 // Match this with your buffer size
};
```
The `Listener` will set up itself automatically with the user's saved
configuration. The used audio channels can be queried through
`Listener.Channels`, which should be respected, and the output audio channel
count should be set to its length. If this is not possible, the layout could be
set to a standard by the number of channels, for example, this line will set up
all listeners to 5.1:
```
Listener.ReplaceChannels(6);
```
Refer to the [scripting API](http://cavern.sbence.hu/cavern/doc.php?if=api/Cavern/Listener/index)
for the complete description of this object.

### Source
This is an audio placed in the sound space, renders a `Clip` at where it's
positioned relative to the `Listener`. Here's how to create a new source at a
given position and attach it to the listener:
```
Source source = new Source() {
    Clip = clip,
    Position = new Vector3(10, 0, 0)
};
listener.AttachSource(source);
```
Sources that are no longer used should be detached from the listener using
`DetachSource`. Refer to the [scripting API](http://cavern.sbence.hu/cavern/doc.php?if=api/Cavern/Source/index)
for the complete description of this object.

### Rendering
To generate the output of the audio space and get the audio samples which should
be output to the system, use the following line:
```
float[] output = listener.Render();
```
The length of this array is `listener.UpdateRate * Listener.Channels.Length`.

### Working with audio files
The `Cavern.Format` library handles reading and writing audio files. For custom
rendering or transcoding, they can be handled on a lower level than loading a
`Clip`.

#### Reading
To open any supported audio file for reading, use the following static function:
```
AudioReader reader = AudioReader.Open(string path);
```
After opening a file, the following workflows are available.

##### Getting all samples
The `Read()` function of an `AudioReader` returns all samples from the file in
an interlaced array with the size of `reader.ChannelCount * reader.Length`.

##### Getting the samples block by block
For real-time use or cases where progress should be displayed, an audio file can
be read block-by-block. First, the header must be read, this is not done
automatically. Until the header is not read, metadata like length or channel
count are unavailable. Header reading is accomplished by calling
`reader.ReadHeader()`.

The `ReadBlock(float[] samples, long from, long to)` function of an
`AudioReader` reads the next interlaced sample block to the specified array in
the specified index range. Samples are counted for all channels. A version of
`ReadBlock` for multichannel arrays (`float[channel][sample]`) is also
available, but in this case, the index range is given for a single channel.

Seeking in local files are supported by calling `reader.Seek(long sample)`. The
time in `sample`s is relative to `reader.Length`, which means it's per a single
channel.

##### Rendering in an environment
The `reader.GetRenderer()` function returns a `Renderer` instance that creates
`Source`s for each channel or audio object. These can be retrieved from the
`Objects` property of the renderer. When all of them are attached to a
`Listener`, they will handle fetching the samples. Seeking the reader or the
renderer works in this use case.

#### Writing
To create an audio file, use an `AudioWriter`:
```
AudioWriter writer = AudioWriter.Create(string path, int channelCount, long length, int sampleRate, BitDepth bits);
```
This will create the `AudioWriter` for the appropriate file extension if it's
supported.

Just like `AudioReader`, an `AudioWriter` can be used with a single call
(`Write(float[] samples)` or `Write(float[][] samples)`) or block by block
(`WriteHeader()` and `WriteBlock(float[] samples, long from, long to)`).

## Unity quick start
Cavern works exactly the same way as Unity's audio engine, only the names are
different. For `AudioSource`, there's `AudioSource3D`, and for `AudioListener`,
there's `AudioListener3D`, and so on. You will find all Cavern components in the
component browser, under audio, and they will automatically add all their Unity
dependencies.

## Development documents
* [Scripting API](http://cavern.sbence.hu/cavern/doc.php?if=api/index) with descriptions of all public members for all public classes
* [Virtualizer repository](https://github.com/VoidXH/HRTF) which contains the raw IR measurements and detailed information about their use
* [Limitless Audio Format](./docs/Limitless%20Audio%20Format.md) for storing Cavern mixes in a CPU-effective spatial format
* [Cavern DCP channel order](./docs/Cavern%20DCP%20channel%20order.md) compared to DCP standards

## Disclaimers
### Code
Cavern is a performance software written in an environment that wasn't made for
it. This means that clean code policies like DRY are broken many times if the
code is faster this way, usually by orders of magnitude. Most changes should be
benchmarked in the target environment, and the fastest code should be chosen,
regardless of how bad it looks. This, however, can't result in inconsistent
interfaces. In that case, wrappers should be used with the least possible method
calls.

### Driver
While Cavern itself is open-source, the setup utility and most converter
interfaces are not, because they are built on licences not allowing it. However,
their functionality is almost entirely using this plugin. Builds can be
downloaded from the [Cavern website](http://cavern.sbence.hu).

## Licence
By downloading, using, copying, modifying, or compiling the source code or a
build, you are accepting these terms. The source code, just like the compiled
software, is given to you for free, but without any warranty. It is not
guaranteed to work, and the developer is not responsible for any damages from
the use of the software. You are allowed to make any modifications, and release
them for free under this licence. If you release a modified version, you have to
link this repository as its source. You are not allowed to sell any part of the
original or the modified version. You are also not allowed to show
advertisements in the modified software. The software must be named with a link
to the creator (http://en.sbence.hu) when used in public (e.g. for screenings)
or commercially (e.g. as an API in another software), also, the original
creator's permission is required for public use (e.g. screening). If you include
these code or any part of the original version in any other project, these terms
still apply.
