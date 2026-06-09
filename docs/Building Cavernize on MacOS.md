# Building Cavernize on MacOS

This document covers the macOS Cavernize desktop port. It does not cover the Unity project or the original Windows-only WPF application.

The Windows desktop build uses `CavernSamples/CavernizeGUI`, which is a WPF app and only runs on Windows. The macOS build uses `CavernSamples/CavernizeAvalonia`, a cross-platform [Avalonia](https://avaloniaui.net/gettingstarted) app that shares the Cavernize rendering logic but replaces the Windows-only UI layer.

There are two ways to get the app running on macOS:

1. Build it from source.
2. Download a prebuilt binary from the Cavernize releases page.

## 1. Building From Source

### Requirements

- macOS 11 or newer.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- [FFmpeg](https://ffmpeg.org/download.html) available in `PATH`, or selected from the app when prompted.

The Cavernize macOS port targets `net8.0`. .NET 10 is not required for this app.

Check the installed SDKs:

```sh
dotnet --list-sdks
```

### Build

Run commands from the Cavern repository root:

```sh
dotnet build CavernSamples/CavernizeAvalonia/CavernizeAvalonia.csproj
```

The debug executable is generated under:

```text
CavernSamples/CavernizeAvalonia/bin/Debug/net8.0/
```

### Publish an App Bundle

For Apple Silicon Macs:

```sh
dotnet publish CavernSamples/CavernizeAvalonia/CavernizeAvalonia.csproj -c Release -r osx-arm64 --self-contained false
```

For Intel Macs:

```sh
dotnet publish CavernSamples/CavernizeAvalonia/CavernizeAvalonia.csproj -c Release -r osx-x64 --self-contained false
```

The publish target creates a macOS app bundle:

```text
CavernSamples/CavernizeAvalonia/bin/Release/net8.0/osx-arm64/publish/Cavernize.app
```

or, for Intel:

```text
CavernSamples/CavernizeAvalonia/bin/Release/net8.0/osx-x64/publish/Cavernize.app
```

Launch it with:

```sh
open CavernSamples/CavernizeAvalonia/bin/Release/net8.0/osx-arm64/publish/Cavernize.app
```

Use the `osx-x64` path instead when building for Intel.

### Cleanup

To remove generated build output for the Avalonia app:

```sh
dotnet clean CavernSamples/CavernizeAvalonia/CavernizeAvalonia.csproj
rm -rf CavernSamples/CavernizeAvalonia/bin CavernSamples/CavernizeAvalonia/obj
```

## 2. Downloading From Releases

Download the macOS build from the [Cavernize releases page](https://github.com/VoidXH/Cavern/releases/latest). Choose the archive that matches your Mac:

- `osx-arm64` for Apple Silicon Macs.
- `osx-x64` for Intel Macs.

After extracting the archive, move `Cavernize.app` to `/Applications`:

```sh
mv ~/Downloads/Cavernize.app /Applications/Cavernize.app
```

If macOS blocks the app because it was downloaded from the internet, remove the quarantine attribute and apply a local ad-hoc signature:

```sh
xattr -dr com.apple.quarantine /Applications/Cavernize.app
codesign --force --deep --sign - /Applications/Cavernize.app
```

Then verify the local signature:

```sh
codesign --verify --deep --strict --verbose=2 /Applications/Cavernize.app
```

Launch the app:

```sh
open /Applications/Cavernize.app
```

Only remove quarantine and sign the app after downloading it from a trusted release page. Ad-hoc signing is a local workaround for unsigned builds; it does not notarize the app or identify the developer to Gatekeeper.

## Notes

- The app bundle is not signed or notarized. 
- The original WPF UI cannot run on macOS. macOS support is provided by the Avalonia project in `CavernSamples/CavernizeAvalonia`.
- The original Windows Cavernize app remains in `CavernSamples/CavernizeGUI`.
- The Unity version is not part of this build path.
