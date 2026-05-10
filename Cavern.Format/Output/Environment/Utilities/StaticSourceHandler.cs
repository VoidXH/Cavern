using System;
using System.Collections.Generic;

using Cavern.Channels;
using Cavern.Format.Renderers;
using Cavern.Format.Renderers.BaseClasses;
using Cavern.Utilities;

namespace Cavern.Format.Environment.Utilities {
    /// <summary>
    /// Handles bed channels for <see cref="EnvironmentWriter"/>s.
    /// </summary>
    internal class StaticSourceHandler {
        /// <summary>
        /// Get which <see cref="Source"/>s represent static objects and their corresponding <see cref="ReferenceChannel"/>s.
        /// </summary>
        public static (ReferenceChannel, Source)[] GetStaticObjects(Renderer source) {
            (ReferenceChannel, Source)[] result = null;
            if (source != null) {
                if (source.HasObjects) {
                    if (source is IMixedBedObjectRenderer mixed) {
                        ReferenceChannel[] staticChannels = mixed.GetStaticChannels();
                        IReadOnlyList<Source> allObjects = source.Objects;

                        result = new (ReferenceChannel, Source)[staticChannels.Length];
                        for (int i = 0; i < staticChannels.Length; i++) {
                            result[i] = (staticChannels[i], allObjects[i]);
                        }
                    } else {
                        // Being object-based, the location of the LFE track is likely the first, but not certainly, so we count it as unknown
                    }
                } else {
                    IReadOnlyList<Source> allObjects = source.Objects;
                    return source.GetChannels().SelectArray((x, i) => (x, allObjects[i]));
                }
            }

            return result ?? Array.Empty<(ReferenceChannel, Source)>();
        }
    }
}
