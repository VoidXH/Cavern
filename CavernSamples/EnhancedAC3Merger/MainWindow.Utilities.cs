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
        static void Error(string message) => MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

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
            if (prepend != null) {
                mustBeGrouped.AddRange(prepend);
            }

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
        /// Channels that can only be in pairs with the next <see cref="ReferenceChannel"/> in order.
        /// </summary>
        static readonly ReferenceChannel[] mustBePaired = {
            ReferenceChannel.RearLeft, ReferenceChannel.FrontLeftCenter, ReferenceChannel.TopFrontLeft,
            ReferenceChannel.TopSideLeft, ReferenceChannel.WideLeft
        };
    }
}