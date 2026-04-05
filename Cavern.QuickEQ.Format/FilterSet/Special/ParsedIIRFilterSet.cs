using Cavern.Format.Utilities;

namespace Cavern.Format.FilterSet.Special {
    /// <summary>
    /// Loads files created with different versions of <see cref="IIRFilterSet.Export"/>.
    /// </summary>
    public class ParsedIIRFilterSet : IIRFilterSet {
        /// <summary>
        /// Loads a single-file export created with <see cref="IIRFilterSet.Export()"/>.
        /// </summary>
        public ParsedIIRFilterSet(string contents) : base(CountChannels(contents), Listener.DefaultSampleRate) {
            // TODO: parse each channel (hold last line, store it as channel name on equal sign line, parse filters after)
        }

        /// <summary>
        /// Because the base constructor needs the channel count, a channel-counter pass is required.
        /// </summary>
        static int CountChannels(string contents) {
            int result = 0;
            foreach (string line in contents.ReadLines()) {
                if (line.IsTheSameCharacter() == '=') {
                    result++;
                }
            }
            return result;
        }
    }
}
