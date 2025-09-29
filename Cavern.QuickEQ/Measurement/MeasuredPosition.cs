using Cavern.QuickEQ.Equalization;

namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// All channels measured at a single microphone position.
    /// </summary>
    public sealed class MeasuredPosition {
        /// <summary>
        /// Identifier of the microphone used - can be anything if there's only a single mic.
        /// </summary>
        public int Mic { get; }

        /// <summary>
        /// For quick calculations, the frequency responses for each channel, in a format it's fast to work with.
        /// </summary>
        public Equalizer[] FrequencyResponses { get; }

        /// <summary>
        /// For advanced operations, the raw measurements for each channel.
        /// </summary>
        public VerboseImpulseResponse[] ImpulseResponses { get; }

        /// <summary>
        /// All channels measured at a single microphone position.
        /// </summary>
        /// <param name="mic">Identifier of the microphone used - can be anything if there's only a single mic</param>
        /// <param name="frequencyResponses">For quick calculations, the frequency responses for each channel</param>
        /// <param name="impulseResponses">For advanced operations, the raw measurements for each channel</param>
        public MeasuredPosition(int mic, Equalizer[] frequencyResponses, VerboseImpulseResponse[] impulseResponses) {
            Mic = mic;
            FrequencyResponses = frequencyResponses;
            ImpulseResponses = impulseResponses;
        }

        /// <inheritdoc/>
        public override string ToString() {
            string channels = FrequencyResponses != null ? FrequencyResponses.Length.ToString() : "no";
            return $"Mic {Mic}, {channels} channels";
        }
    }
}
