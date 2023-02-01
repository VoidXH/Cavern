namespace EnhancedAC3Merger {
    partial class MainWindow {
        /// <summary>
        /// All possible input tracks, even if they're not assigned.
        /// </summary>
        InputChannel[] inputs;

        /// <summary>
        /// E-AC-3 override streams contain 4 channels, but only add 2 new channels. The first 2 channels should be already existing
        /// channels, which were previously merged channels. The override streams separate those 2 channels to 2-2 new ones, like
        /// SL and SR to SL-RL and SR-RR. These are the channels each optional is downmixed to.
        /// </summary>
        /// <remarks>Each pair of entries create one override stream.</remarks>
        (InputChannel newChannel, InputChannel overriddenChannel)[] downmixPairs;

        /// <summary>
        /// Assign the arrays that are semantically constant.
        /// </summary>
        void CreateConsts() {
            inputs = new InputChannel[] {
                fl, fr, fc, lfe, sl, sr, // Bed order doesn't matter, it's handled by FFmpeg
                flc, frc, rl, rr, wl, wr, tfl, tfr, tsl, tsr // Others are in E-AC-3 channel assignment order
            };
            downmixPairs = new (InputChannel, InputChannel)[] {
                (flc, fl), (frc, fr), (rl, sl), (rr, sr), (wl, fl), (wr, fr), (tfl, fl), (tfr, fr), (tsl, sl), (tsr, sr)
            };
        }
    }
}