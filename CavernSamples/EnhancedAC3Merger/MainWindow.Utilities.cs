using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using Cavern.Format;
using Cavern.Remapping;

namespace EnhancedAC3Merger {
    // Utility functions for the main functionality that are unlikely to be modified
    partial class MainWindow {
        /// <summary>
        /// Display an error message popup.
        /// </summary>
        public static void Error(string message) => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Create caches for each input file's each channel.
        /// </summary>
        /// <param name="files">Result of <see cref="GetFiles"/></param>
        /// <remarks>The header of the files must be read beforehand.</remarks>
        static float[][][] CreateChannelCache(AudioReader[] files) {
            float[][][] result = new float[files.Length][][];
            for (int i = 0; i < files.Length; i++) {
                float[][] fileCache = result[i] = new float[files[i].ChannelCount][];
                for (int j = 0; j < fileCache.Length; j++) {
                    fileCache[j] = new float[bufferSize];
                }
            }
            return result;
        }

        /// <summary>
        /// Get the complete target layout in E-AC-3's channel mapping order
        /// </summary>
        /// <param name="streams">Result of <see cref="GetSubstreams(InputChannel[][])"/></param>
        static ReferenceChannel[] GetLayout(InputChannel[][] streams) {
            ReferenceChannel[] layout = streams.SelectMany(x => x).Select(x => x.TargetChannel).ToArray();
            if (streams[0].Length > 4) { // E-AC-3 has the non-standard LCR order
                layout[1] = ReferenceChannel.FrontCenter;
                layout[2] = ReferenceChannel.FrontRight;
                if (streams[0].Length == 6) { // LFE location is also different
                    layout[3] = ReferenceChannel.SideLeft;
                    layout[4] = ReferenceChannel.SideRight;
                    layout[5] = ReferenceChannel.ScreenLFE;
                }
            }
            return layout;
        }

        /// <summary>
        /// Read the headers of the opened files. Get their length and sample rate if they match, and return -1 if they don't.
        /// </summary>
        /// <param name="files">Result of <see cref="GetFiles"/></param>
        static (long length, int sampleRate) PrepareFiles(AudioReader[] files) {
            files[0].ReadHeader();
            long length = files[0].Length;
            int sampleRate = files[0].SampleRate;
            bool error = false;
            for (int i = 1; i < files.Length; i++) {
                files[i].ReadHeader();
                if (files[i].SampleRate != sampleRate) {
                    Error("The sample rate of the input files does not match.");
                    error = true;
                }
                if (!error && files[i].Length != length) {
                    Error("The length of the input files does not match.");
                    error = true;
                }
                if (error) {
                    for (int j = 0; j < files.Length; j++) {
                        files[j].Dispose();
                    }
                    return (-1, -1);
                }
            }
            return (length, sampleRate);
        }

        /// <summary>
        /// Get the channels that are part of the bed (2.0, 4.0, 5.0, or 5.1) or null if the bed layout is invalid.
        /// </summary>
        InputChannel[] GetBed() {
            if (fl.Active && fr.Active) {
                if (sl.Active && sr.Active) {
                    if (fc.Active) {
                        if (lfe.Active) {
                            return new InputChannel[] { fl, fr, fc, lfe, sl, sr };
                        } else {
                            return new InputChannel[] { fl, fr, fc, sl, sr };
                        }
                    } else if (!lfe.Active) {
                        return new InputChannel[] { fl, fr, sl, sr };
                    }
                } else if (!fc.Active && !lfe.Active && !sl.Active && !sr.Active) {
                    return new InputChannel[] { fl, fr };
                }
            }
            return null;
        }

        /// <summary>
        /// Assign the channels as E-AC-3 override streams.
        /// </summary>
        /// <param name="prepend">Append substreams after these entries</param>
        /// <returns>Stream groups or null if a channel misses its mandatory pair.</returns>
        InputChannel[][] GetSubstreams(params InputChannel[][] prepend) {
            List<InputChannel[]> mustBeGrouped = new List<InputChannel[]>();

            for (int i = 6 /* after bed */; i < inputs.Length; i++) {
                if (Array.BinarySearch(mustBePaired, inputs[i].TargetChannel) >= 0) {
                    if (inputs[i].SelectedFile != null && inputs[i + 1].SelectedFile != null) {
                        mustBeGrouped.Add(new InputChannel[] { inputs[i], inputs[++i] });
                    } else if (inputs[i].SelectedFile != null || inputs[++i].SelectedFile != null) {
                        return null;
                    }
                } else if (inputs[i].SelectedFile != null) {
                    mustBeGrouped.Add(new InputChannel[] { inputs[i] });
                }
            }

            List<InputChannel[]> result = new List<InputChannel[]>();
            result.AddRange(prepend);
            const int maxPerBuild = 4;
            InputChannel[] build = new InputChannel[maxPerBuild];
            int buildIndex = 0;
            for (int i = 0, c = mustBeGrouped.Count; i < c; i++) {
                if (buildIndex + mustBeGrouped[i].Length > maxPerBuild) {
                    result.Add(build[..buildIndex]);
                    build = new InputChannel[maxPerBuild];
                    buildIndex = 0;
                }
                for (int j = 0; j < mustBeGrouped[i].Length; j++) {
                    build[buildIndex++] = mustBeGrouped[i][j];
                }
            }
            if (buildIndex != 0) {
                result.Add(build[..buildIndex]);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Get files that have at least one of their tracks set as an input.
        /// </summary>
        AudioReader[] GetFiles() =>
            inputs.Where(x => x.Active).Select(x => x.SelectedFile).Distinct().Select(x => AudioReader.Open(x)).ToArray();

        /// <summary>
        /// For each of the <see cref="inputs"/>, get which index of the <paramref name="files"/> has the handle for that input.
        /// </summary>
        /// <param name="files">Result of <see cref="GetFiles"/></param>
        /// <remarks>For unsed inputs, the index is -1.</remarks>
        Dictionary<InputChannel, int> Associate(AudioReader[] files) {
            Dictionary<InputChannel, int> result = new Dictionary<InputChannel, int>();
            for (int i = 0; i < inputs.Length; i++) {
                if (inputs[i].Active) {
                    for (int j = 0; j < files.Length; j++) {
                        if (inputs[i].SelectedFile.Equals(files[j].Path)) {
                            result[inputs[i]] = j;
                        }
                    }
                } else {
                    result[inputs[i]] = -1;
                }
            }
            return result;
        }

        /// <summary>
        /// Number of samples per channel to read and write in each frame.
        /// </summary>
        const long bufferSize = 16384;

        /// <summary>
        /// Channels that can only be in pairs with the next <see cref="ReferenceChannel"/> in order.
        /// </summary>
        static readonly ReferenceChannel[] mustBePaired = {
            ReferenceChannel.RearLeft, ReferenceChannel.FrontLeftCenter, ReferenceChannel.TopFrontLeft,
            ReferenceChannel.TopSideLeft, ReferenceChannel.WideLeft
        };
    }
}