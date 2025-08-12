using System.Diagnostics;
using System.IO.Compression;

using Cavern.Format;
using Cavern.Format.Common;
using Cavern.Format.Operations;

using Cavernize.Logic.Language;
using Cavernize.Logic.Models;
using VoidX.WPF;

namespace Cavernize.Logic.External;

/// <summary>
/// Perform MLP conversions with truehdd by first extracting the MLP track, then using truehdd to create a DAMF, then loading that DAMF to Cavern for render.
/// </summary>
public class Truehdd(ExternalConverterStrings language) : ExternalConverter(language) {
    /// <summary>
    /// The DAMF track after truehdd's run.
    /// </summary>
    CavernizeTrack track;

    /// <summary>
    /// Path to the extracted MLP track.
    /// </summary>
    string tempTrack;

    /// <inheritdoc/>
    public override void PrepareOnUI() {
        if (File.Exists(versionFile)) {
            return;
        }

        UpdateStatusMessage(language.LicenceFetch);
        string licence = HTTP.GET(licenceUrl) ?? throw new NetworkException(language.LicenceFail);
        UpdateStatusMessage(language.WaitingUserAccept);
        LicenceDisplay.SetDescription(string.Format(language.LicenceNeeded, unpackDir, "Meridian Lossless Packing"));
        LicenceDisplay.SetLicenceText(licence);
        if (!LicenceDisplay.Prompt()) {
            throw new OperationCanceledException(language.UserCancelled);
        }
    }

    /// <summary>
    /// Convert the <paramref name="source"/> track with truehdd to DAMF and load it for rendering.
    /// </summary>
    public override CavernizeTrack Convert(CavernizeTrack source) {
        Download(); // Convert can only be called after the licence was accepted

        if (source.Codec != Codec.TrueHD) {
            throw new CodecNotFoundException(Codec.TrueHD);
        }

        UpdateStatusMessage(language.ExtractingBitstream);
        string folder = Path.GetDirectoryName(source.Path);
        tempTrack = Path.Combine(folder, tempFile);
        using (ExtractTrackFromContainer extractor = new(source.Track, File.OpenWrite(tempTrack))) {
            while (extractor.Process()) {
                // Extraction in progress
            }
        }

        UpdateStatusMessage(string.Format(language.Converting, unpackDir));
        ProcessStartInfo truehdd = new() {
            FileName = Path.Combine(unpackDir, "truehdd.exe"),
            Arguments = $"decode --progress \"{tempTrack}\" --output-path \"{tempTrack}\""
        };
        using (Process runner = Process.Start(truehdd)) {
            runner.WaitForExit();
        }
        File.Delete(tempTrack);

        track = new CavernizeTrack(AudioReader.Open(tempTrack + ".atmos"), Codec.DAMF, 0, new TrackStrings());
        return track;
    }

    /// <inheritdoc/>
    public override void Cleanup() {
        track.Dispose();
        File.Delete(tempTrack + ".atmos");
        File.Delete(tempTrack + ".atmos.audio");
        File.Delete(tempTrack + ".atmos.metadata");
    }

    /// <summary>
    /// Download the latest truehdd release from GitHub and unpack it to the <see cref="unpackDir"/>.
    /// </summary>
    void Download() {
        if (File.Exists(versionFile)) {
            return; // Already downloaded
        }

        if (File.Exists(cacheZip)) {
            File.Delete(cacheZip);
        }
        if (Directory.Exists(unpackDir)) {
            Directory.Delete(unpackDir, true);
        }

        string downloading = string.Format(language.Downloading, unpackDir);
        UpdateStatusMessage(downloading);
        string zip;
        try {
            zip = Downloader.GetLatestGitHubVersion("truehdd/truehdd", version);
            Task downloader = Downloader.Download(zip, cacheZip,
                progress => UpdateStatusMessage($"{downloading} {progress:0.00%}"));
            downloader.Wait();
        } catch (Exception e) {
            throw new NetworkException($"{language.NetworkError}{Environment.NewLine}{e.Message}");
        }

        UpdateStatusMessage(string.Format(language.Extracting, unpackDir));
        try {
            ZipFile.ExtractToDirectory(cacheZip, unpackDir);
            File.Delete(cacheZip);
        } catch (Exception e) {
            throw new NetworkException($"{language.NetworkError}{Environment.NewLine}{e.Message}");
        }

        File.WriteAllText(versionFile, zip);
    }

    /// <summary>
    /// Where to save the truehdd zip file.
    /// </summary>
    const string cacheZip = "truehdd.zip";

    /// <summary>
    /// If this folder exists, truehdd was properly downloaded and unpacked.
    /// </summary>
    const string unpackDir = "truehdd";

    /// <summary>
    /// Save the downloaded truehdd version to this file so it can be checked later.
    /// </summary>
    const string versionFile = "truehdd.version";

    /// <summary>
    /// Temporary file used to store the extracted MLP track before truehdd processes it.
    /// </summary>
    const string tempFile = "_extracted.thd";

    /// <summary>
    /// Where to fetch the truehdd licence from.
    /// </summary>
    const string licenceUrl = "https://raw.githubusercontent.com/truehdd/truehdd/refs/heads/main/LICENSE";

    /// <summary>
    /// Which release to download on this platform.
    /// </summary>
    const string version = "x86_64-pc-windows-msvc.zip";
}
