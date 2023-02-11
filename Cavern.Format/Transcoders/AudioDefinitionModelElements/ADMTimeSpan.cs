using System;

namespace Cavern.Format.Transcoders.AudioDefinitionModelElements {
    /// <summary>
    /// Representation of a length of time, with digits cut at 1/100000th of a second.
    /// </summary>
    public readonly struct ADMTimeSpan : IEquatable<ADMTimeSpan> {
        /// <summary>
        /// A time span of 0 seconds and 0 fractions.
        /// </summary>
        public static readonly ADMTimeSpan Zero = new ADMTimeSpan(0);

        /// <summary>
        /// The total time of the time span in seconds.
        /// </summary>
        public double TotalSeconds => seconds + fraction / 100000.0;

        /// <summary>
        /// Seconds of time.
        /// </summary>
        readonly int seconds;

        /// <summary>
        /// 5 decimal places of fractions of a second as ADM needs this many digits.
        /// </summary>
        readonly int fraction;

        /// <summary>
        /// Representation of a length of time, with digits cut at 1/100000th of a second.
        /// </summary>
        public ADMTimeSpan(double seconds) {
            this.seconds = (int)seconds;
            fraction = (int)(seconds % 1 * 100000 + 0.5);
        }

        /// <summary>
        /// Representation of a length of time, with digits cut at 1/100000th of a second.
        /// </summary>
        public ADMTimeSpan(string source) {
            int split = source.IndexOf('.');
            if (split != -1) {
                seconds = int.Parse(source[0..2]) * 3600 + int.Parse(source[3..5]) * 60 + int.Parse(source[6..8]);
                fraction = int.Parse(source[(split + 1)..]);
            } else {
                seconds = int.Parse(source);
                fraction = 0;
            }
        }

        /// <summary>
        /// Representation of a length of time, with digits cut at 1/100000th of a second.
        /// </summary>
        internal ADMTimeSpan(int seconds, int fraction) {
            this.seconds = seconds;
            this.fraction = fraction;
        }

        /// <summary>
        /// Add two time spans together.
        /// </summary>
        public static ADMTimeSpan operator +(ADMTimeSpan lhs, ADMTimeSpan rhs) {
            int totalFraction = lhs.fraction + rhs.fraction;
            return new ADMTimeSpan(lhs.seconds + rhs.seconds + totalFraction / 100000, totalFraction % 100000);
        }

        /// <summary>
        /// Get the difference of two time spans.
        /// </summary>
        /// <remarks>Negative time spans are not supported for performance.</remarks>
        public static ADMTimeSpan operator -(ADMTimeSpan lhs, ADMTimeSpan rhs) {
            if (lhs.fraction >= rhs.fraction) {
                return new ADMTimeSpan(lhs.seconds - rhs.seconds, lhs.fraction - rhs.fraction);
            }
            return new ADMTimeSpan(lhs.seconds - rhs.seconds - 1, lhs.fraction - rhs.fraction + 100000);
        }

        /// <summary>
        /// Checks if a time span is smaller than another.
        /// </summary>
        public static bool operator <(ADMTimeSpan lhs, ADMTimeSpan rhs) =>
            lhs.seconds < rhs.seconds || (lhs.seconds == rhs.seconds && lhs.fraction < rhs.fraction);

        /// <summary>
        /// Checks if a time span is larget than another.
        /// </summary>
        public static bool operator >(ADMTimeSpan lhs, ADMTimeSpan rhs) =>
            lhs.seconds > rhs.seconds || (lhs.seconds == rhs.seconds && lhs.fraction > rhs.fraction);

        /// <summary>
        /// Checks if the two time spans are equal.
        /// </summary>
        public bool Equals(ADMTimeSpan other) => seconds == other.seconds && fraction == other.fraction;

        /// <summary>
        /// Gets if this time span represents no time.
        /// </summary>
        public bool IsZero() => seconds == 0 && fraction == 0;

        /// <summary>
        /// Display the timestamp as a floating-point number with a dot.
        /// </summary>
        public string ToInvariantFloatString() => $"{seconds}.{fraction:00000}";

        /// <summary>
        /// Checks if this time span is equal to another object.
        /// </summary>
        public override bool Equals(object obj) {
            if (obj is ADMTimeSpan rhs) {
                return seconds == rhs.seconds && fraction == rhs.fraction;
            }
            return false;
        }

        /// <summary>
        /// Get a basic hash for the time span.
        /// </summary>
        public override int GetHashCode() => seconds.GetHashCode() ^ fraction.GetHashCode();

        /// <summary>
        /// Get the AXML-compliant string format of the time span.
        /// </summary>
        public override string ToString() =>
            $"{seconds / 3600:00}:{seconds % 3600 / 60:00}:{seconds % 60:00}.{fraction:00000}";
    }
}
