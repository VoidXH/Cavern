using System.IO;

using Cavern.Format.Utilities;

namespace Cavern.Format.Container.MXF {
    /// <summary>
    /// Contains partition data in a KLV.
    /// </summary>
    internal class PackRegistry : KeyLengthValueSMPTE {
        /// <summary>
        /// If the <see cref="KeyLengthValueSMPTE.Key"/> represents a partition (the registry is a pack), gets its status.
        /// </summary>
        public bool PartitionOpen => (Key.item & 0x100) != 0;

        /// <summary>
        /// If the <see cref="KeyLengthValueSMPTE.Key"/> represents a partition (the registry is a pack), gets its openness.
        /// </summary>
        public bool PartitionComplete => (Key.item & 0x600) == 0x300 || (Key.item & 0x600) == 0x400;

        /// <summary>
        /// This is a header partition pack.
        /// </summary>
        public bool IsHeader => (Key.item & 0xFF0000) == 0x020000;

        /// <summary>
        /// ULs of the essences contained in this file (track codec identifiers).
        /// </summary>
        readonly (int registry, ulong item)[] essences;

        /// <summary>
        /// Contains partition data in a KLV.
        /// </summary>
        public PackRegistry((int, int, ulong) key, Stream reader) : base(key, reader) {
            if (IsHeader) {
                reader.Position += 80;
                int essenceCount = reader.ReadInt32BE();
                reader.Position += 8; // Unknown
                essences = new (int registry, ulong item)[essenceCount];
                for (int i = 0; i < essenceCount; i++) {
                    reader.Position += 8; // UL marker
                    essences[i] = (reader.ReadInt32BE(), reader.ReadUInt64BE());
                }
            }
        }

        /// <inheritdoc/>
        protected override string ItemToString() {
            string state = $"{(PartitionOpen ? "open" : "closed")} and {(PartitionComplete ? "" : "in")}complete)";
            if (IsHeader) {
                return "(header, " + state;
            } else {
                return "(" + state;
            }
        }
    }
}