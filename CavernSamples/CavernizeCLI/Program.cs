using System;

using Cavernize.Logic.CommandLine;
using Cavernize.Logic.External;
using Cavernize.Logic.Rendering;
using VoidX.WPF.FFmpeg;

namespace CavernizeCLI;

/// <summary>
/// Cavernize in the command line to support all platforms that run .NET.
/// </summary>
public static class Program {
    /// <summary>
    /// Application entry point.
    /// </summary>
    public static void Main(string[] args) {
        FFmpeg.ConsoleMode = true;
        using CavernizeSession app = new() {
            LicencePrompt = new ConsoleLicence()
        };

        int lastPercent = -1;
        app.StatusChanged += Console.WriteLine;
        app.WarningRaised += message => Console.Error.WriteLine(message);
        app.ProgressChanged += progress => {
            if (progress < 0) {
                Console.WriteLine("Progress: running");
                return;
            }

            int percent = (int)Math.Round(progress * 100);
            if (percent != lastPercent) {
                lastPercent = percent;
                Console.WriteLine($"Progress: {percent}%");
            }
        };

        CommandLineProcessor.Initialize(args, app);
    }
}
