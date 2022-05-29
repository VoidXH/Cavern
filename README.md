# Cavern
Cavern is a fully adaptive object-based audio engine and upmixer without limitations for home, cinema, and stage use. This repository features a Unity plugin and a standalone converter in C++.

[![Build Status](https://app.travis-ci.com/VoidXH/Cavern.svg?branch=master)](https://app.travis-ci.com/VoidXH/Cavern)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/VoidXH/Cavern)
![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/VoidXH/Cavern/latest)
![Lines of Code](https://img.shields.io/tokei/lines/github/VoidXH/Cavern "Lines of Code")

## Features
* Unlimited objects and output channels without position restrictions
* Custom audio format for storing spatial mixes
* Upconverter for regular surround mixes
* Full cinema and stage support with realtime conversion
* Cavern QuickEQ corrects the room's frequency response in seconds
* HRIR headphone virtualizer
* Mix repositioning based on occupied seats
* Seat movement generation
* Unity-like listener and source functionality
* Ultra low latency, even the upconverter can work from as low as one sample per frame

## User documentation
User documentation can be found at the [Cavern documentation webpage](http://cavern.sbence.hu/cavern/doc.php).
Please go to this page for basic setup, in-depth QuickEQ tutorials, and
command-line arguments.

## How to build
### Cavern
Cavern is a .NET Framework project with no dependencies other than NuGet ones.
Open the `Cavern.sln` solution with Microsoft Visual Studio 2019 or later and all
projects should build.

### Cavern for Unity
Open the `CavernUnity DLL.sln` solution with Microsoft Visual Studio 2019. Remove
the references from the CavernUnity DLL project to UnityEngine and UnityEditor.
Add these files from your own Unity installation as references. They are found in
`Editor\Data\Managed` under Unity's installation folder.

### Cavernize Lite
This is a Code::Blocks project, set up for the MingW compiler. No additional
libraries were used, this is standard C++ code, so importing just the .cpp and
.h files into any IDE will work perfectly.

## Development documents
* [Cavern DCP channel order](./docs/Cavern%20DCP%20channel%20order.md) compared to DCP standards
* [Limitless Audio Format](./docs/Limitless%20Audio%20Format.md) for storing Cavern mixes in a CPU-effective spatial format
* [Virtualizer repository](https://github.com/VoidXH/HRTF) which contains the raw IR measurements and detailed information about their use

## Disclaimers
### Code
Cavern is a performance software written in an environment that's not ready for
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
See the [LICENCE](LICENCE.md) file for licence rights and limitations.
