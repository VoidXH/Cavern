using System.IO;

namespace Cavern.Format.Input {
    /// <summary>
    /// Reads the stems from Spleeter output folders.
    /// </summary>
    public class SpleeterReader {
        /// <summary>
        /// Separated bass instruments.
        /// </summary>
        public RIFFWaveReader Bass { get; }

        /// <summary>
        /// Separated instrument stem for drums.
        /// </summary>
        public RIFFWaveReader Drums { get; }

        /// <summary>
        /// All sounds that were not separated into any other stem. This includes everything for vocal separation (2-stem export).
        /// </summary>
        public RIFFWaveReader Other { get; }

        /// <summary>
        /// Separated instrument stem for piano.
        /// </summary>
        public RIFFWaveReader Piano { get; }

        /// <summary>
        /// Separated vocal track.
        /// </summary>
        public RIFFWaveReader Vocals { get; }

        /// <summary>
        /// Reads the stems from a Spleeter output <paramref name="folder"/>.
        /// </summary>
        public SpleeterReader(string folder) {
            Bass = GetReader(Path.Combine(folder, bass));
            Drums = GetReader(Path.Combine(folder, drums));
            Other = GetReader(Path.Combine(folder, other));
            Piano = GetReader(Path.Combine(folder, piano));
            Vocals = GetReader(Path.Combine(folder, vocals));

            Other ??= GetReader(Path.Combine(folder, accompaniment));
            if (Other == null) {
                throw new FileNotFoundException("No Spleeter content was found in the folder.");
            }
        }

        /// <summary>
        /// Seek all stems to a <paramref name="sample"/> offset (for a single channel).
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        /// <remarks>Seeking is not thread-safe.</remarks>
        public void Seek(long sample) {
            Bass?.Seek(sample);
            Drums?.Seek(sample);
            Other.Seek(sample);
            Piano?.Seek(sample);
            Vocals?.Seek(sample);
        }

        /// <summary>
        /// Load a stem by <paramref name="path"/> if it exists. Nonexistent stems return null.
        /// </summary>
        /// <param name="path">Full path to the stem file</param>
        RIFFWaveReader GetReader(string path) {
            if (!File.Exists(path)) {
                return null;
            }

            RIFFWaveReader result = new RIFFWaveReader(path);
            result.ReadHeader();
            return result;
        }

        const string bass = "bass.wav";
        const string drums = "drums.wav";
        const string other = "other.wav";
        const string piano = "piano.wav";
        const string vocals = "vocals.wav";
        const string accompaniment = "accompaniment.wav";
    }
}