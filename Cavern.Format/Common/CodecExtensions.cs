using System;

namespace Cavern.Format.Common {
    /// <summary>
    /// Extra functions for codecs.
    /// </summary>
    public static class CodecExtensions {
        /// <summary>
        /// List of known (not neccessarily supported) audio codecs.
        /// </summary>
        static readonly Codec[] audioCodecs = { Codec.LimitlessAudio, Codec.ADM_BWF, Codec.ADM_BWF_Atmos, Codec.DAMF, Codec.TrueHD,
            Codec.EnhancedAC3, Codec.PCM_Float, Codec.PCM_LE, Codec.DTS_HD, Codec.FLAC, Codec.Opus, Codec.DTS, Codec.AC3 };

        /// <summary>
        /// List of known video codecs.
        /// </summary>
        static readonly Codec[] videoCodecs = { Codec.HEVC, Codec.HEVC_DolbyVision, Codec.AVC };

        /// <summary>
        /// List of supported audio codecs.
        /// </summary>
        static readonly Codec[] supportedAudioCodecs = { Codec.LimitlessAudio, Codec.ADM_BWF, Codec.ADM_BWF_Atmos, Codec.DAMF, Codec.EnhancedAC3,
            Codec.PCM_Float, Codec.PCM_LE, Codec.AC3 };

        /// <summary>
        /// List of audio codecs that can export rendered audio environments.
        /// </summary>
        static readonly Codec[] environmentalAudioCodecs = { Codec.LimitlessAudio, Codec.ADM_BWF, Codec.ADM_BWF_Atmos, Codec.DAMF };

        /// <summary>
        /// For a given <paramref name="codec"/>, returns the supported native outputs in save file dialog format.
        /// This does not include container formats.
        /// </summary>
        public static string GetSaveDialogFilter(this Codec codec) {
            const string admResult = "ADM Broadcast Wave Format|*.wav|ADM BWF + Audio XML|*.xml";
            const string pcmResult = "RIFF WAVE|*.wav|Limitless Audio Format|*.laf|Core Audio Format|*.caf";
            return codec switch {
                Codec.LimitlessAudio => "Limitless Audio Format|*.laf",
                Codec.ADM_BWF => admResult,
                Codec.ADM_BWF_Atmos => admResult,
                Codec.DAMF => "Dolby Atmos Master Format|*.atmos",
                Codec.EnhancedAC3 => "Enhanced AC-3|*.ec3;*.eac3",
                Codec.PCM_Float => pcmResult,
                Codec.PCM_LE => pcmResult,
                Codec.Opus => "Opus|*.ogg;*.opus",
                Codec.AC3 => "AC-3|*.ac3",
                _ => null
            };
        }

        /// <summary>
        /// Checks if a codec transports audio.
        /// </summary>
        public static bool IsAudio(this Codec codec) => Array.BinarySearch(audioCodecs, codec) >= 0;

        /// <summary>
        /// Checks if a codec transports video.
        /// </summary>
        public static bool IsVideo(this Codec codec) => Array.BinarySearch(videoCodecs, codec) >= 0;

        /// <summary>
        /// Checks if a codec is a supported audio codec.
        /// </summary>
        public static bool IsSupportedAudio(this Codec codec) => Array.BinarySearch(supportedAudioCodecs, codec) >= 0;

        /// <summary>
        /// Checks if a codec is able to export a rendered audio environment.
        /// </summary>
        public static bool IsEnvironmental(this Codec codec) => Array.BinarySearch(environmentalAudioCodecs, codec) >= 0;

        /// <summary>
        /// Checks if an extension (including the dot) belongs to a codec's native audio-only type.
        /// </summary>
        public static bool IsNative(this string extension) {
            switch (extension.ToLower()) {
                case ".ac3":
                case ".atmos":
                case ".caf":
                case ".eac3":
                case ".ec3":
                case ".laf":
                case ".ogg":
                case ".opus":
                case ".wav":
                    return true;
                default:
                    return false;
            }
        }
    }
}
