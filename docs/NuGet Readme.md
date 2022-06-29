# Cavern
Cavern is a fully adaptive object-based audio rendering engine and (up)mixer
without limitations for home, cinema, and stage use. Audio transcoding and
self-calibration libraries built on the Cavern engine are also available.

[![Build Status](https://api.travis-ci.com/VoidXH/Cavern.svg?branch=master)](https://app.travis-ci.com/VoidXH/Cavern)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/VoidXH/Cavern)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/VoidXH/Cavern/latest)
![Lines of Code](https://img.shields.io/tokei/lines/github/VoidXH/Cavern "Lines of Code")

## Features
* Unlimited objects and output channels without position restrictions
* Advanced self-calibration with a microphone (Cavern.QuickEQ package)
	* Results in close to perfectly flat frequency response, <0.01 dB and <0.01 ms of uniformity
	* Uniformity can be achieved without a calibration file
* Real-time upconversion of regular surround sound mixes to 3D
* Direction and distance virtualization for headphones
* Audio transcoder library with a custom spatial format (Cavern.Format package)
	* Supported containers: .wav, .laf, .mkv, .mka
	* Fully supported codecs: RIFF WAVE, Limitless Audio Format
	* Partially supported codecs: E-AC-3 with Joint Object Coding (Dolby Digital Plus Atmos)
* Mix repositioning based on occupied seats
* Seat movement generation
* Unity-like listener and source functionality
* Ultra low latency, even the upconverter can work from as low as one sample per frame

## Quick start
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

## Development documents
* [Scripting API](http://cavern.sbence.hu/cavern/doc.php?if=api/index) with descriptions of all public members for all public classes.
* [Virtualizer repository](https://github.com/VoidXH/HRTF) which contains the raw IR measurements and detailed information about their use
* [Limitless Audio Format](./docs/Limitless%20Audio%20Format.md) for storing Cavern mixes in a CPU-effective spatial format