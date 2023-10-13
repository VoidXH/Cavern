using System.Collections.Generic;

using Cavern.Format.Common;
using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class ObjectInfoBlock : IMetadataSupplier {
        /// <inheritdoc/>
        public ReadableMetadata GetMetadata() => new ReadableMetadata(new List<ReadableMetadataHeader> {
            new ReadableMetadataHeader("Object Info Block", new[] {
                new ReadableMetadataField("object_gain", "Object gain", gain < 0 ? "reuse" : QMath.GainToDb(gain).ToString("0 dB")),
                new ReadableMetadataField("obj_render_info[0]", "Position update information was present in this block", ValidPosition),
                new ReadableMetadataField("b_differential_position_specified", "The block contains relative movement", differentialPosition),
                new ReadableMetadataField("pos", "Last decoded object position in absolute space", position),
                new ReadableMetadataField("distance_factor", "Object distance", float.IsNaN(distance) ? "not present" : distance.ToString()),
                new ReadableMetadataField("object_size", "Sides of the cube that represents the scale of this object", size),
                new ReadableMetadataField("anchor", "Transformation origin point", anchor),
                new ReadableMetadataField("screen_factor", "Screen/room anchoring transition helper variable", screenFactor),
                new ReadableMetadataField("depth_factor", "Screen/room anchoring transition helper variable", depthFactor),
            })
        });
    }
}