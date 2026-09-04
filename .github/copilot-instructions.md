## Libraries
- Cavern/ (ns Cavern.*): Listener, Source, Clip, Channel classes. Folders: Filters/, Remapping/ (upmixers, remapping), Virtualizer/, Channels/ (ChannelPrototype, ReferenceChannel), Utilities/ (QMath, Complex, FFTCache, Resample, WaveformUtils, CavernAmp).
- Cavern.Format/ (ns Cavern.Format.*): Audio file handling: AudioReader.Open(...), AudioWriter.Create(...). Codec/container I/O, networking.
- Cavern.QuickEQ/ (ns Cavern.QuickEQ.*): Equalization, EQCurves, Crossover, Graphing, SignalGeneration. Measurement (FFT, etc) class is under Cavern/Utilities.
- Cavern.QuickEQ.Format/ (ns Cavern.Format.*): EQ export to devices (ConfigurationFile, FilterSet).
- CavernAmp/ - g++ DLL, accessed with Cavern/Utilities/CavernAmp*.
- CavernUnity DLL/ (ns Cavern.*): Unity versions of some classes, multichannel measurement orchestration.
