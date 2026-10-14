using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Format.Exceptions;
using Cavern.Format.JSON;

namespace Cavern.Format.FilterSet.Consts {
    /// <summary>
    /// Helper functions MQX-related classes can use.
    /// </summary>
    public static partial class MQXHelpers {
        /// <summary>
        /// Apply the channel data map, channel list, and target curves to an existing MQX document.
        /// </summary>
        /// <param name="mqx">Parsed MQX document</param>
        /// <param name="channelDataMap">Per-channel data map element</param>
        /// <param name="guids">In-file channel GUIDs in export order</param>
        /// <param name="targetCurves">Target curve elements</param>
        public static void ApplyToDocument(JsonFile mqx, JsonFile channelDataMap, string[] guids, object[] targetCurves) {
            mqx[MQXConsts.channelMappingKey] = channelDataMap;
            mqx[channelListKey] = ToObjectArray(guids);
            mqx["TargetCurveSet"] = targetCurves;
        }

        /// <summary>
        /// Build the per-channel data map element of an MQX file.
        /// </summary>
        /// <param name="guids">In-file channel GUIDs</param>
        /// <param name="channels">Export data of each channel</param>
        /// <param name="polarities">Polarity inversion of each channel</param>
        /// <param name="gains">Trim gain of each channel in decibels</param>
        /// <param name="delays">Delay of each channel in milliseconds</param>
        /// <exception cref="IOException">A channel that's part of the exported configuration is unsupported by MultEQ-X.</exception>
        public static JsonFile CreateChannelDataMap(string[] guids, ChannelData[] channels, bool[] polarities, double[] gains, double[] delays) {
            JsonFile result = new JsonFile();
            for (int channel = 0; channel < guids.Length; channel++) {
                (ReferenceChannel channel, string designation, string name, string pairDesignation, string pair, string location) label;
                if (!string.IsNullOrEmpty(channels[channel].name)) {
                    label = MQXConsts.labeling.FirstOrDefault(x => x.name == channels[channel].name);
                    if (label.designation == null) {
                        label = MQXConsts.labeling.FirstOrDefault(x => x.channel == channels[channel].reference);
                    }
                }
                else {
                    label = MQXConsts.labeling.FirstOrDefault(x => x.channel == channels[channel].reference);
                }
                if (label.designation == null) {
                    throw new IOException("A channel that's part of the exported configuration is unsupported by MultEQ-X.");
                }

                result[guids[channel]] = new JsonFile {
                    {
                        metadataKey, new JsonFile {
                            { designationKey, label.designation },
                            { displayNameKey, label.name },
                            { "PairDesignation", label.pairDesignation },
                            { "PairDisplayName", label.pair },
                            { "Location", label.location }
                        }
                    },
                    {
                        "Calibration", new JsonFile {
                            { "IsEnabled", true },
                            { "Trim", gains[channel] },
                            { "DistanceMilliseconds", delays[channel] },
                            { "PolarityError", polarities[channel] },
                            { "SpeakerSize", "Small" },
                            { "CrossoverFrequency", channels[channel].crossoverFrequency ?? 80 }
                        }
                    },
                    {
                        "TargetCurveCutoff", new JsonFile {
                            { "Mode", channels[channel].reference == ReferenceChannel.ScreenLFE ? "Disabled" : "Auto" }
                        }
                    }
                };
            }
            return result;
        }

        /// <summary>
        /// Gets the <see cref="ReferenceChannel"/> for a given <paramref name="designation"/>.
        /// </summary>
        public static ReferenceChannel DesignationToChannel(string designation) {
            for (int i = 0; i < MQXConsts.labeling.Length; i++) {
                if (MQXConsts.labeling[i].designation == designation) {
                    return MQXConsts.labeling[i].channel;
                }
            }
            return ReferenceChannel.Unknown;
        }

        /// <summary>
        /// Gets the <see cref="ReferenceChannel"/> for a given fully spelled out <paramref name="displayName"/>.
        /// </summary>
        public static ReferenceChannel DisplayNameToChannel(string displayName) {
            for (int i = 0; i < MQXConsts.labeling.Length; i++) {
                if (MQXConsts.labeling[i].name == displayName) {
                    return MQXConsts.labeling[i].channel;
                }
            }
            return ReferenceChannel.Unknown;
        }

        /// <summary>
        /// Generate a set of new channel GUIDs for an MQX file.
        /// </summary>
        /// <param name="count">Number of GUIDs to generate</param>
        public static string[] GenerateGuids(int count) {
            string[] result = new string[count];
            for (int i = 0; i < count; i++) {
                result[i] = Guid.NewGuid().ToString();
            }
            return result;
        }

        /// <summary>
        /// Get what GUID maps to which actual channel in the MQX file or null if no such mapping is present.
        /// </summary>
        public static Dictionary<string, ReferenceChannel> GetChannelMapping(JsonFile mqx) {
            if (!mqx.ContainsKey(MQXConsts.channelMappingKey)) {
                return null;
            }

            JsonFile mapping = (JsonFile)mqx[MQXConsts.channelMappingKey];

            Dictionary<string, ReferenceChannel> result = new Dictionary<string, ReferenceChannel>();
            foreach (KeyValuePair<string, object> channel in mapping) {
                JsonFile metadata = (JsonFile)((JsonFile)channel.Value)[metadataKey];
                ReferenceChannel reference = DesignationToChannel((string)metadata[designationKey]);
                result[channel.Key] = reference;
            }
            return result;
        }

        /// <summary>
        /// Get the display name and <see cref="ReferenceChannel"/> for each channel GUID in the MQX file,
        /// or null if no channel mapping is present.
        /// </summary>
        public static Dictionary<string, (string displayName, ReferenceChannel reference)> GetChannelMappingWithNames(JsonFile mqx) {
            if (!mqx.ContainsKey(MQXConsts.channelMappingKey)) {
                return null;
            }

            JsonFile mapping = (JsonFile)mqx[MQXConsts.channelMappingKey];

            Dictionary<string, (string displayName, ReferenceChannel reference)> result = new Dictionary<string, (string, ReferenceChannel)>();
            foreach (KeyValuePair<string, object> channel in mapping) {
                JsonFile metadata = (JsonFile)((JsonFile)channel.Value)[metadataKey];
                string displayName = (string)metadata[displayNameKey];
                ReferenceChannel reference = DisplayNameToChannel(displayName);
                if (reference == ReferenceChannel.Unknown) {
                    reference = DesignationToChannel((string)metadata[designationKey]);
                }
                result[channel.Key] = (displayName, reference);
            }
            return result;
        }

        /// <summary>
        /// Convert an array of fully spelled out display names to <see cref="ReferenceChannel"/>s using the library database.
        /// </summary>
        public static ReferenceChannel[] GetReferenceChannels(string[] displayNames) {
            if (displayNames == null) {
                return new ReferenceChannel[0];
            }
            ReferenceChannel[] result = new ReferenceChannel[displayNames.Length];
            for (int i = 0; i < displayNames.Length; i++) {
                result[i] = DisplayNameToChannel(displayNames[i]);
            }
            return result;
        }

        /// <summary>
        /// Get the ordered channel GUIDs, display names, and reference channels from a parsed MQX document.
        /// </summary>
        /// <param name="mqx">Parsed MQX data</param>
        /// <exception cref="CorruptionException">The document has no channel list or channel mapping.</exception>
        public static (string guid, string name, ReferenceChannel reference)[] GetOrderedChannels(JsonFile mqx) {
            Dictionary<string, (string displayName, ReferenceChannel reference)> channelData = GetChannelMappingWithNames(mqx);
            if (channelData == null || !mqx.ContainsKey(channelListKey)) {
                throw new CorruptionException("channel list");
            }

            object[] orderedGuids = (object[])mqx[channelListKey];
            (string guid, string name, ReferenceChannel reference)[] result = new (string guid, string name, ReferenceChannel channel)[orderedGuids.Length];
            for (int i = 0; i < orderedGuids.Length; i++) {
                string guid = (string)orderedGuids[i];
                result[i].guid = guid;
                if (channelData.TryGetValue(guid, out (string displayName, ReferenceChannel channel) entry)) {
                    result[i].name = entry.displayName;
                    result[i].reference = entry.channel;
                } else {
                    result[i].reference = ReferenceChannel.Unknown;
                }
            }
            return result;
        }

        /// <summary>
        /// Build a new MQX file root around the given channel data map and target curves.
        /// </summary>
        /// <param name="channelDataMap">Per-channel data map element</param>
        /// <param name="guids">In-file channel GUIDs in export order</param>
        /// <param name="targetCurves">Target curve elements</param>
        public static JsonFile CreateConfiguration(JsonFile channelDataMap, string[] guids, object[] targetCurves) {
            JsonFile result = new JsonFile {
                { "_measurements", Array.Empty<object>() },
                { MQXConsts.channelMappingKey, channelDataMap },
                { channelListKey, ToObjectArray(guids) },
                { "CalibrationSettings", new JsonFile {
                    { "AutoTrims", false },
                    { "AutoDistance", false },
                    { "AutoEnable", false },
                    { "AutoBassManagement", false }
                } },
                { "TargetCurveSet", targetCurves },
                { "PositionNames", new JsonFile() },
                { "UsedLocalMicrophones", new JsonFile() }
            };
            return result;
        }

        /// <summary>
        /// Convert channel GUIDs to an object array for JSON serialization.
        /// </summary>
        static object[] ToObjectArray(string[] guids) {
            object[] result = new object[guids.Length];
            for (int i = 0; i < guids.Length; i++) {
                result[i] = guids[i];
            }
            return result;
        }
    }
}
