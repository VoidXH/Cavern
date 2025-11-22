using System;

using Cavern.Channels;

namespace Cavern.Remapping {
    /// <summary>
    /// Operations on <see cref="SpatialRemappingSource"/> values.
    /// </summary>
    public static class SpatialRemappingSourceExtensions {
        /// <summary>
        /// Convert the content's spatial layout to Cavern objects that support layout operations.
        /// </summary>
        public static ChannelPrototype[] ToPrototypes(this SpatialRemappingSource source) => source switch {
            SpatialRemappingSource.Off => null,
            SpatialRemappingSource.ITU_7_1 => ChannelPrototype.Get(ChannelPrototype.ref710),
            SpatialRemappingSource.ITU_5_1_2 => ChannelPrototype.Get(ChannelPrototype.ref512),
            SpatialRemappingSource.Allocentric_7_1 => ChannelPrototype.GetAlternative(ChannelPrototype.ref710),
            SpatialRemappingSource.Allocentric_5_1_2 => ChannelPrototype.GetAlternative(ChannelPrototype.ref512),
            _ => throw new NotImplementedException()
        };

        /// <summary>
        /// Convert the content's spatial layout to <see cref="Channel"/>s.
        /// </summary>
        public static Channel[] ToLayout(this SpatialRemappingSource source) => source != SpatialRemappingSource.Off ?
            ChannelPrototype.ToLayout(source.ToPrototypes()) :
            null;
    }
}
