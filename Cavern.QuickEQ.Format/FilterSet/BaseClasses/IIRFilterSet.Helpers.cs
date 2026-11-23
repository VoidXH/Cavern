using System;
using System.IO;
using System.Text;

using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    // Base64 serialization handlers
    partial class IIRFilterSet {
        /// <inheritdoc/>
        public void FromBase64(string source) {
            byte[] data = Convert.FromBase64String(source);
            using BinaryReader reader = new BinaryReader(new MemoryStream(data));
            BinaryDeserialize(reader);
        }

        /// <inheritdoc/>
        public string ToBase64() {
            using MemoryStream ms = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(ms)) {
                BinarySerialize(writer);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Deserialize this filter set from a binary stream written with <see cref="BinarySerialize"/>.
        /// </summary>
        /// <param name="reader">Input binary reader.</param>
        void BinaryDeserialize(BinaryReader reader) {
            SampleRate = reader.ReadInt32();
            int channelCount = reader.ReadInt32();
            Initialize<IIRChannelData>(channelCount);

            for (int i = 0; i < channelCount; i++) {
                IIRChannelData channelRef = (IIRChannelData)Channels[i];
                channelRef.gain = reader.ReadDouble();
                channelRef.delaySamples = reader.ReadInt32();
                channelRef.switchPolarity = reader.ReadBoolean();
                int nameLength = reader.ReadInt32();
                byte[] nameBytes = reader.ReadBytes(nameLength);
                channelRef.name = Encoding.UTF8.GetString(nameBytes);

                int filterCount = reader.ReadInt32();
                BiquadFilter[] filters = new BiquadFilter[filterCount];
                for (int j = 0; j < filterCount; j++) {
                    string filterBase64 = reader.ReadString();
                    filters[j] = BiquadFilter.FromBase64String(filterBase64);
                }
                channelRef.filters = filters;
            }
        }

        /// <summary>
        /// Serialize this filter set to a binary stream for later deserialization.
        /// Format: sampleRate, channelCount, [gain, delaySamples, switchPolarity, nameLength, name, filterCount, filterBase64...]
        /// </summary>
        /// <param name="writer">Output binary writer.</param>
        void BinarySerialize(BinaryWriter writer) {
            writer.Write(SampleRate);
            writer.Write(Channels.Length);

            for (int i = 0; i < Channels.Length; i++) {
                IIRChannelData channelRef = (IIRChannelData)Channels[i];
                writer.Write(channelRef.gain);
                writer.Write(channelRef.delaySamples);
                writer.Write(channelRef.switchPolarity);
                string name = channelRef.name ?? string.Empty;
                writer.Write(name.Length);
                writer.Write(Encoding.UTF8.GetBytes(name));

                BiquadFilter[] filters = channelRef.filters;
                writer.Write(filters.Length);
                for (int j = 0; j < filters.Length; j++) {
                    writer.Write(filters[j].ToBase64());
                }
            }
        }
    }
}
