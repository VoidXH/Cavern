using Cavern.Channels;
using Cavern.Format;

using Cavernize.Logic.Language;

namespace Cavernize.Logic.Models;

/// <summary>
/// Operations that can be performed on all <see cref="ICavernizeApp"/> instances.
/// </summary>
public static class ICavernizeAppExtensions {
    /// <summary>
    /// Loads a previous Cavern FIR filter set export for <see cref="RoomCorrection"/>, or sets it to null
    /// if the <paramref name="path"/> (to a root file, the txt next to the WAVs) is invalid.
    /// In those cases, an exception will be thrown.
    /// </summary>
    public static void LoadRoomCorrection(this ICavernizeApp app, string path, ConversionStrings language) {
        int cutoff = path.IndexOf('.');
        if (cutoff == -1) {
            app.RenderingSettings.RoomCorrection = null;
            throw new IOException(language.InvalidRootFile);
        }

        string pathStart = path[..cutoff] + ' ';
        ReferenceChannel[] channels = app.RenderTarget.GetNameMappedChannels();
        float[][] filter = new float[channels.Length][];
        int sampleRate = 0;
        for (int i = 0; i < channels.Length; i++) {
            string file = $"{pathStart}{channels[i].GetShortName()}.wav";
            if (!File.Exists(file)) {
                file = $"{pathStart}{i + 1}.wav";
            }
            if (File.Exists(file)) {
                using RIFFWaveReader reader = new RIFFWaveReader(file);
                filter[i] = reader.Read();
                sampleRate = reader.SampleRate;
            } else {
                app.RenderingSettings.RoomCorrection = null;
                throw new IOException(string.Format(language.ChannelFilterNotFound, ChannelPrototype.Mapping[(int)channels[i]].Name, Path.GetFileName(path)));
            }
        }
        app.RenderingSettings.RoomCorrection = new(new(filter), sampleRate);
    }
}
