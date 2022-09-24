# Cavern
Cavern is a fully adaptive object-based audio rendering engine and (up)mixer
without limitations for home, cinema, and stage use. Audio transcoding and
self-calibration libraries built on the Cavern engine are also available.
This repository features a Unity plugin and a standalone converter in C++.

[![Build Status](https://api.travis-ci.com/VoidXH/Cavern.svg?branch=master)](https://app.travis-ci.com/VoidXH/Cavern)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/VoidXH/Cavern)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/VoidXH/Cavern/latest)
![Lines of Code](https://img.shields.io/tokei/lines/github/VoidXH/Cavern "Lines of Code")

## Features
* Unlimited objects and output channels without position restrictions
* Advanced self-calibration with a microphone
	* Results in close to perfectly flat frequency response, <0.01 dB and <0.01 ms of uniformity
	* Uniformity can be achieved without a calibration file
* Audio transcoder library with a custom spatial format
	* Supported codecs:
	  * E-AC-3 with Joint Object Coding (Dolby Digital Plus Atmos)
	  * Limitless Audio Format
	  * RIFF WAVE
	  * Audio Definition Model Broadcast Wave Format
	* Supported containers: .ac3, .ec3, .laf, .mka, .mkv, .wav
* Direction and distance virtualization for headphones
* Real-time upconversion of regular surround sound mixes to 3D
* Mix repositioning based on occupied seats
* Seat movement generation
* Unity-like listener and source functionality
* Ultra low latency, even the upconverter can work from as low as one sample per frame

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

### Cavern for Unity
Open the `CavernUnity DLL.sln` solution with Microsoft Visual Studio 2022. Remove
the references from the CavernUnity DLL project to UnityEngine and UnityEditor.
Add these files from your own Unity installation as references. They are found in
`Editor\Data\Managed` under Unity's installation folder.

### CavernAmp and Cavernize Lite
These are Code::Blocks projects, set up for the MingW compiler. No additional
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

## Unity quick start
Cavern works exactly the same way as Unity's audio engine, only the names are
different. For `AudioSource`, there's `AudioSource3D`, and for `AudioListener`,
there's `AudioListener3D`, and so on. You will find all Cavern components in the
component browser, under audio, and they will automatically add all their Unity
dependencies.

## Development documents
* [Scripting API](http://cavern.sbence.hu/cavern/doc.php?if=api/index) with descriptions of all public members for all public classes.
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
