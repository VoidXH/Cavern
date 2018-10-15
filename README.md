# Cavern
Cavern is a fully adaptive object-based audio engine and upmixer without limitations for home, cinema, and stage use. This repository features a Unity plugin and a standalone converter in C++.

## Features
* Unlimited objects and output channels without position restrictions
* Custom audio format for storing spatial mixes
* Upconverter for regular surround mixes
* Full cinema and stage support with realtime conversion
* Cavern QuickEQ corrects the room's frequency response in seconds
* Mix repositioning based on occupied seats
* Seat movement generation
* Unity-like listener and source functionality
* Ultra low latency, even the upconverter can work from as low as one sample per frame

## Helpful documents
* [Calibration process](./docs/Calibration%20process.md) for traditional or Cavern-only rooms
* [Cavern DCP channel order](./docs/Cavern%20DCP%20channel%20order.md) compared to DCP standards
* [Limitless Audio Format](./docs/Limitless%20Audio%20Format.md) for storing Cavern mixes in a CPU-effective spatial format

## Driver disclaimer
While Cavern itself is open-source, the setup utility and most converter interfaces are not, because they are built on licences not allowing it. However, their functionality is almost entirely using this plugin. Builds can be downloaded from the [Cavern website](http://cavern.cf).

## Licence
By downloading, using, copying, modifying, or compiling the source code or a build, you are accepting these terms. The source code, just like the compiled software, is given to you for free, but without any warranty. It is not guaranteed to work, and the developer is not responsible for any damages from the use of the software. You are allowed to make any modifications, and release them for free under this licence. If you release a modified version, you have to link this repository as its source. You are not allowed to sell any part of the original or the modified version. You are also not allowed to show advertisements in the modified software. The software must be named with a link to the creator (http://voidx.tk) when used in public (e.g. for screenings) or commercially (e.g. as an API in another software), also, the original creator's permission is required for public use. If you include these code or any part of the original version in any other project, these terms still apply.
