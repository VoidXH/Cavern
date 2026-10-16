using System.Runtime.CompilerServices;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;
using Cavern.Utilities.Threading;
using Cavern.Waveforms;

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
        public Equalizer[] FrequencyResponses { get; private set; }

        /// <summary>
        /// For advanced operations, the raw measurements for each channel.
        /// </summary>
        public VerboseImpulseResponse[] ImpulseResponses { get; private set; }

        /// <summary>
        /// Number of contained channels.
        /// </summary>
        public int Channels => FrequencyResponses.Length;

        /// <summary>
        /// Parse a single positions's measurement data from <paramref name="impulseResponses"/> without an FFT optimization object.
        /// </summary>
        /// <param name="mic">Identifier of the microphone used - can be anything if there's only a single mic.</param>
        /// <param name="impulseResponses">Raw measured impulse responses on the current microphone position.</param>
        /// <param name="sampleRate">At what sample rate were the <paramref name="impulseResponses"/> recorded at.</param>
        /// <param name="multithreaded">Whether to perform the operation with multithreaded performance.</param>
        public MeasuredPosition(int mic, MultichannelWaveform impulseResponses, int sampleRate, bool multithreaded) {
            Mic = mic;
            if (multithreaded) {
                using FFTCachePool pool = new FFTCachePool(impulseResponses.Length);
                Parse(impulseResponses, sampleRate, pool);
            } else {
                using FFTCache cache = new FFTCache(impulseResponses.Length);
                Parse(impulseResponses, sampleRate, cache);
            }
        }

        /// <summary>
        /// Parse a single positions's measurement data from <paramref name="impulseResponses"/> on a single thread.
        /// </summary>
        /// <param name="mic">Identifier of the microphone used - can be anything if there's only a single mic.</param>
        /// <param name="impulseResponses">Raw measured impulse responses on the current microphone position.</param>
        /// <param name="sampleRate">At what sample rate were the <paramref name="impulseResponses"/> recorded at.</param>
        /// <param name="cache">FFT performance optimization instance.</param>
        public MeasuredPosition(int mic, MultichannelWaveform impulseResponses, int sampleRate, FFTCache cache) {
            Mic = mic;
            Parse(impulseResponses, sampleRate, cache);
        }

        /// <summary>
        /// Parse a single positions's measurement data from <paramref name="impulseResponses"/> with multithreaded performance.
        /// </summary>
        /// <param name="mic">Identifier of the microphone used - can be anything if there's only a single mic.</param>
        /// <param name="impulseResponses">Raw measured impulse responses on the current microphone position.</param>
        /// <param name="sampleRate">At what sample rate were the <paramref name="impulseResponses"/> recorded at.</param>
        /// <param name="pool">FFT performance optimization instance.</param>
        public MeasuredPosition(int mic, MultichannelWaveform impulseResponses, int sampleRate, FFTCachePool pool) {
            Mic = mic;
            Parse(impulseResponses, sampleRate, pool);
        }

        /// <summary>
        /// All channels measured at a single microphone position.
        /// </summary>
        /// <param name="mic">Identifier of the microphone used - can be anything if there's only a single mic</param>
        /// <param name="frequencyResponses">For quick calculations, the frequency responses for each channel</param>
        /// <param name="impulseResponses">For advanced operations, the raw measurements for each channel</param>
        public MeasuredPosition(int mic, Equalizer[] frequencyResponses, VerboseImpulseResponse[] impulseResponses) {
            if (frequencyResponses.Length != impulseResponses.Length) {
                throw new ChannelCountMismatchException();
            }
            Mic = mic;
            FrequencyResponses = frequencyResponses;
            ImpulseResponses = impulseResponses;
        }

        /// <inheritdoc/>
        public override string ToString() {
            string channels = FrequencyResponses != null ? FrequencyResponses.Length.ToString() : "no";
            return $"Mic {Mic}, {channels} channels";
        }

        /// <summary>
        /// Initialize the arrays of stored data based on the number of <paramref name="impulseResponses"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void InitArrays(MultichannelWaveform impulseResponses) {
            FrequencyResponses = new Equalizer[impulseResponses.Channels];
            ImpulseResponses = new VerboseImpulseResponse[impulseResponses.Channels];
        }

        /// <summary>
        /// Operation of the single-threaded parser constructors.
        /// </summary>
        void Parse(MultichannelWaveform impulseResponses, int sampleRate, FFTCache cache) {
            InitArrays(impulseResponses);
            for (int i = 0; i < impulseResponses.Channels; i++) {
                Complex[] transferFunction = impulseResponses[i].FFT(cache);
                FrequencyResponses[i] = EQGenerator.FromTransferFunctionOptimized(transferFunction, sampleRate);
                ImpulseResponses[i] = new VerboseImpulseResponse(impulseResponses[i]);
            }
        }

        /// <summary>
        /// Operation of the multithreaded parser constructors.
        /// </summary>
        void Parse(MultichannelWaveform impulseResponses, int sampleRate, FFTCachePool pool) {
            InitArrays(impulseResponses);
            Parallelizer.For(0, impulseResponses.Channels, i => {
                FFTCache cache = pool.Lease();
                Complex[] transferFunction = impulseResponses[i].FFT(cache);
                pool.Return(cache);
                FrequencyResponses[i] = EQGenerator.FromTransferFunctionOptimized(transferFunction, sampleRate);
                ImpulseResponses[i] = new VerboseImpulseResponse(impulseResponses[i]);
            });
        }
    }
}
