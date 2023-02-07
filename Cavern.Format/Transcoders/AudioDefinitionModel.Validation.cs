using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class AudioDefinitionModel {
        /// <summary>
        /// Check if timings and positions are valid for this AXML. A string for each error is returned.
        /// </summary>
        public List<string> Validate() {
            ADMTimeSpan length = GetLength();
            List<string> errors = new List<string>();
            for (int ch = 0, c = ChannelFormats.Count; ch < c; ch++) {
                ADMChannelFormat channel = ChannelFormats[ch];
                List<ADMBlockFormat> blocks = channel.Blocks;
                int lastBlock = blocks.Count - 1;
                if (lastBlock == -1) {
                    errors.Add($"Channel {channel.ID} has no blocks.");
                    continue;
                }

                if (!blocks[0].Offset.IsZero()) {
                    errors.Add($"Channel {channel.ID} does not start when the program starts.");
                }

                if (channel.Type != ADMPackType.DirectSpeakers) {
                    for (int block = 0; block <= lastBlock; block++) {
                        if (blocks[block].Duration.IsZero()) {
                            errors.Add($"Channel {channel.ID}'s block {block + 1}'s length is zero.");
                        }
                    }
                }

                for (int block = 0; block <= lastBlock; block++) {
                    if (blocks[block].Interpolation > blocks[block].Duration) {
                        errors.Add($"Channel {channel.ID}'s block {block + 1}'s interpolation is longer than its duration.");
                    }

                    if (blocks[block].Position.X < -1 || blocks[block].Position.X > 1 ||
                        blocks[block].Position.Y < -1 || blocks[block].Position.Y > 1 ||
                        blocks[block].Position.Z < -1 || blocks[block].Position.Z > 1) {
                        errors.Add($"Channel {channel.ID}'s block {block + 1}'s position is out of the allowed [-1; 1] range.");
                    }
                }

                for (int block = 1; block <= lastBlock; block++) {
                    if (blocks[block - 1].Offset > blocks[block].Offset) {
                        errors.Add($"Channel {channel.ID}'s block {block} and {block + 1} are swapped in time.");
                    }
                }

                for (int block = 1; block < lastBlock; block++) {
                    if (!blocks[block].Offset.Equals(blocks[block - 1].Offset + blocks[block - 1].Duration)) {
                        errors.Add($"Channel {channel.ID}'s block {block} does not end when the next block starts.");
                    }
                }
                if (channel.Type == ADMPackType.Objects && !length.Equals(blocks[lastBlock].Offset + blocks[lastBlock].Duration)) {
                    errors.Add($"Channel {channel.ID} does not end when the program ends.");
                }
            }
            return errors;
        }

        void ValidateChannel(XElement channel) {
            string channelName = channel.GetAttribute(ADMTags.channelFormatIDAttribute)[3..];
            IEnumerable<XElement> blocks = channel.AllDescendants(ADMTags.blockTag);
            int blockId = 0;
            foreach (XElement block in blocks) {
                string id = block.GetAttribute(ADMTags.blockIDAttribute);
                string[] ids = id.Split('_');
                if (ids.Length != 3) {
                    throw new ArgumentException($"Block ID \"{id}\" is invalid.");
                }
                if (ids[0] != "AB") {
                    throw new ArgumentException($"Block ID \"{id}\" doesn't start with \"AB\".");
                }
                if (ids[1] != channelName) {
                    throw new ArgumentException($"Block ID \"{id}\" doesn't match the channel it's assigned to $({channelName}).");
                }
                if (int.Parse(ids[2], NumberStyles.HexNumber) != ++blockId) {
                    throw new ArgumentException($"Block ID \"{id}\" has an invalid index, the valid one is $({blockId}).");
                }
            }
        }
    }
}