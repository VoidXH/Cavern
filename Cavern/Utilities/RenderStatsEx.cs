using System;

namespace Cavern.Utilities {
    /// <summary>
    /// Rendering environment statistics evaluator extended with audio quality metrics.
    /// </summary>
    public sealed class RenderStatsEx : RenderStats {
        /// <summary>
        /// Peak signal level of a frame across all audio frames in relative signal level.
        /// </summary>
        public float FrameLevelPeak { get; private set; }

        /// <summary>
        /// RMS level of the entire measured audio signal in relative signal level.
        /// </summary>
        public float FrameLevelRMS => MathF.Sqrt(rms.Sum / frames);

        /// <summary>
        /// The dynamic range of the entire measured audio signal in relative signal level.
        /// </summary>
        public float Macrodynamics => FrameLevelPeak / FrameLevelRMS;

        /// <summary>
        /// The maximum dynamic range that happens in a second, in relative signal level.
        /// </summary>
        public float Microdynamics { get; private set; }

        /// <summary>
        /// Peak signal level of a frame across all audio frames on the LFE channel in relative signal level.
        /// </summary>
        public float LFELevelPeak { get; private set; }

        /// <summary>
        /// RMS level of the entire measured audio signal on the LFE channel in relative signal level.
        /// </summary>
        public float LFELevelRMS => MathF.Sqrt(lfeRms.Sum / frames);

        /// <summary>
        /// The dynamic range of the entire measured audio signal on the LFE channel in relative signal level.
        /// </summary>
        public float LFEMacrodynamics => LFELevelPeak / LFELevelRMS;

        /// <summary>
        /// The maximum dynamic range that happens in a second on the LFE channel, in relative signal level.
        /// </summary>
        public float LFEMicrodynamics { get; private set; }

        /// <summary>
        /// RMS level of the entire measured audio signal on the surround channels in relative signal level.
        /// </summary>
        public float SurroundLevelRMS => MathF.Sqrt(surroundRms.Sum / frames);

        /// <summary>
        /// RMS level of the entire measured audio signal on the height channels in relative signal level.
        /// </summary>
        public float HeightLevelRMS => MathF.Sqrt(heightRms.Sum / frames);

        /// <summary>
        /// Surrounds to all channels usage ratio.
        /// </summary>
        public float RelativeSurroundLevel => SurroundLevelRMS / FrameLevelRMS;

        /// <summary>
        /// Heights to all channels usage ratio.
        /// </summary>
        public float RelativeHeightLevel => HeightLevelRMS / FrameLevelRMS;

        /// <summary>
        /// Used for calculating the global RMS by RMS(each frame's RMS). This holds the sum of squares and
        /// has to be evaluated with <see cref="FrameLevelRMS"/>.
        /// </summary>
        readonly AccurateSum rms = new AccurateSum();

        /// <summary>
        /// Used for the same purpose as <see cref="rms"/>, but for the LFE channel.
        /// </summary>
        readonly AccurateSum lfeRms = new AccurateSum();

        /// <summary>
        /// Used for the same purpose as <see cref="rms"/>, but for the surround channels.
        /// </summary>
        readonly AccurateSum surroundRms = new AccurateSum();

        /// <summary>
        /// Used for the same purpose as <see cref="rms"/>, but for the height channels.
        /// </summary>
        readonly AccurateSum heightRms = new AccurateSum();

        /// <summary>
        /// Number of measured audio frames.
        /// </summary>
        int frames;

        /// <summary>
        /// Averages the required values for calculating <see cref="Microdynamics"/>.
        /// </summary>
        MovingAverage averager;

        /// <summary>
        /// Process this many frames before the values of <see cref="averager"/> become valid.
        /// </summary>
        int skip;

        /// <summary>
        /// Rendering environment statistics evaluator extended with audio quality metrics.
        /// </summary>
        public RenderStatsEx(Listener listener) : base(listener) { }

        /// <summary>
        /// Update the stats according to the last <paramref name="frame"/>.
        /// </summary>
        /// <remarks>The <paramref name="frame"/> size must be constant across calls to get accurate results.</remarks>
        public override void Update(float[] frame) {
            base.Update(frame);

            float currentRMS = WaveformUtils.GetRMS(frame),
                lfeRMS = 0,
                surroundRMS = 0,
                heightRMS = 0;
            int lfeChannels = 0,
                surroundChannels = 0,
                heightChannels = 0;
            for (int i = 0; i < Listener.Channels.Length; i++) {
                if (Listener.Channels[i].LFE) {
                    lfeRMS += WaveformUtils.GetRMS(frame, i, Listener.Channels.Length);
                    lfeChannels++;
                } else if (!Listener.Channels[i].IsScreenChannel) {
                    float channelRms = WaveformUtils.GetRMS(frame, i, Listener.Channels.Length);
                    surroundRMS += channelRms;
                    surroundChannels++;
                    if (Listener.Channels[i].X != 0) {
                        heightRMS += channelRms;
                        heightChannels++;
                    }
                }
            }
            lfeRMS /= lfeChannels;
            surroundRMS /= surroundChannels;
            heightRMS /= heightChannels;

            if (FrameLevelPeak < currentRMS) {
                FrameLevelPeak = currentRMS;
            }
            if (LFELevelPeak < lfeRMS) {
                LFELevelPeak = lfeRMS;
            }
            currentRMS *= currentRMS;
            lfeRMS *= lfeRMS;
            rms.Add(currentRMS);
            lfeRms.Add(lfeRMS);
            surroundRms.Add(surroundRMS);
            heightRms.Add(heightRMS);
            ++frames;

            if (averager == null) {
                skip = listener.SampleRate / frame.Length;
                averager = new MovingAverage(skip);
                return;
            }
            averager.AddFrame(new[] { currentRMS, lfeRMS });
            if (skip-- > 0) {
                return;
            }
            float microdynamics = currentRMS / averager.Average[0];
            float lfeMicrodynamics = lfeRMS / averager.Average[1];
            if (Microdynamics < microdynamics) {
                Microdynamics = microdynamics;
            }
            if (LFEMicrodynamics < lfeMicrodynamics) {
                LFEMicrodynamics = lfeMicrodynamics;
            }
        }
    }
}