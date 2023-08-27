# Cavern.Format
Audio transcoder library for Cavern with object-based audio support.
Supported codecs:
  * E-AC-3 with Joint Object Coding (Dolby Digital Plus Atmos)
  * Limitless Audio Format
  * RIFF WAVE
  * Audio Definition Model Broadcast Wave Format

Supported containers: .ac3, .eac3, .ec3, .laf, .m4a, .m4v, .mka, .mkv, .mov, .mp4, .qt, .wav, .weba, .webm

[![Build Status](https://api.travis-ci.com/VoidXH/Cavern.svg?branch=master)](https://app.travis-ci.com/VoidXH/Cavern)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/VoidXH/Cavern)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/VoidXH/Cavern/latest)
![Lines of Code](https://img.shields.io/tokei/lines/github/VoidXH/Cavern "Lines of Code")

## Quick start
This library handles reading and writing audio files. For custom rendering or
transcoding, they can be handled on a lower level than loading a `Clip`.

### Reading
To open any supported audio file for reading, use the following static function:
```
AudioReader reader = AudioReader.Open(string path);
```
After opening a file, the following workflows are available.

#### Getting all samples
The `Read()` function of an `AudioReader` returns all samples from the file in
an interlaced array with the size of `reader.ChannelCount * reader.Length`.

#### Getting the samples block by block
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

#### Rendering in an environment
The `reader.GetRenderer()` function returns a `Renderer` instance that creates
`Source`s for each channel or audio object. These can be retrieved from the
`Objects` property of the renderer. When all of them are attached to a
`Listener`, they will handle fetching the samples. Seeking the reader or the
renderer works in this use case.

### Writing
To create an audio file, use an `AudioWriter`:
```
AudioWriter writer = AudioWriter.Create(string path, int channelCount, long length, int sampleRate, BitDepth bits);
```
This will create the `AudioWriter` for the appropriate file extension if it's
supported.

Just like `AudioReader`, an `AudioWriter` can be used with a single call
(`Write(float[] samples)` or `Write(float[][] samples)`) or block by block
(`WriteHeader()` and `WriteBlock(float[] samples, long from, long to)`).

## Development documents
* [Scripting API](https://cavern.sbence.hu/cavern/doc.php?if=api/index) with descriptions of all public members for all public classes
* [Limitless Audio Format](https://cavern.sbence.hu/cavern/doc.php?p=LAF) for storing Cavern mixes in a CPU-effective spatial format
* [Cavern DCP channel order](https://cavern.sbence.hu/cavern/doc.php?p=DCP) compared to DCP standards
