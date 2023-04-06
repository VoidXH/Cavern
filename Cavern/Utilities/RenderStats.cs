using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Cavern.Utilities {
    /// <summary>
    /// Records rendering environment statistics for each <see cref="Source"/>.
    /// </summary>
    public class RenderStats {
        /// <summary>
        /// Recorded stats about a <see cref="Source"/>.
        /// </summary>
        class StatHolder {
            /// <summary>
            /// The source has moved some positions, but less times than the <see cref="SemiStaticLimit"/>.
            /// </summary>
            public bool SemiStatic => Positions.Count < SemiStaticLimit;

            /// <summary>
            /// False if the source was ever moved from its position.
            /// </summary>
            public bool Static { get; private set; } = true;

            /// <summary>
            /// False if the source was ever set to a position.
            /// </summary>
            public bool SuperStatic { get; private set; } = true;

            /// <summary>
            /// Positions this source was placed on. The size of this set is limited to <see cref="SemiStaticLimit"/>.
            /// </summary>
            public HashSet<Vector3> Positions = new HashSet<Vector3>();

            /// <summary>
            /// Last location of the source.
            /// </summary>
            public Vector3 LastPosition {
                get => lastPosition;
                set {
                    if (value != Vector3.Zero) {
                        if (value != lastPosition && lastPosition != Vector3.Zero) {
                            Static = false;
                        } else {
                            SuperStatic = false;
                        }
                        lastPosition = value;
                        if (Positions.Count <= SemiStaticLimit) {
                            Positions.Add(value);
                        }
                    }
                }
            }
            Vector3 lastPosition;
        }

        /// <summary>
        /// Number of new positions required to consider a source dynamic.
        /// </summary>
        public static int SemiStaticLimit { get; set; } = 16;

        /// <summary>
        /// Target rendering environment.
        /// </summary>
        protected readonly Listener listener;

        /// <summary>
        /// Recorded stats for each source.
        /// </summary>
        readonly Dictionary<Source, StatHolder> stats = new Dictionary<Source, StatHolder>();

        /// <summary>
        /// Records rendering environment statistics for each <see cref="Source"/>.
        /// </summary>
        public RenderStats(Listener listener) => this.listener = listener;

        /// <summary>
        /// Gets positions where any static or semi-static source was located.
        /// </summary>
        public Vector3[] GetStaticOrSemiStaticPositions() =>
            stats.Where(x => x.Value.Static || x.Value.SemiStatic).Select(x => x.Value.LastPosition).Distinct().ToArray();

        /// <summary>
        /// Get how many sources weren't moved more than <see cref="SemiStaticLimit"/> times through all updates.
        /// </summary>
        public int GetSemiStaticCount() => stats.Sum(x => x.Value.Static || x.Value.SemiStatic ? 1 : 0);

        /// <summary>
        /// Get how many sources weren't even set to position through all updates.
        /// </summary>
        public int GetSuperStaticCount() => stats.Sum(x => x.Value.SuperStatic ? 1 : 0);

        /// <summary>
        /// Update the stats according to the last <paramref name="frame"/> that's rendered by the <see cref="listener"/>.
        /// </summary>
        /// <remarks>The base class doesn't use the <paramref name="frame"/> data, but <see cref="RenderStatsEx"/> does.</remarks>
        public virtual void Update(float[] frame) {
            foreach (Source source in listener.ActiveSources) {
                StatHolder holder;
                if (!stats.ContainsKey(source)) {
                    stats.Add(source, holder = new StatHolder());
                } else {
                    holder = stats[source];
                }

                holder.LastPosition = source.Position;
            }
        }
    }
}