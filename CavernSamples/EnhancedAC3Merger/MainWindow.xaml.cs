using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

using Cavern.Channels;
using Cavern.Format;

using VoidX.WPF;

namespace EnhancedAC3Merger {
    /// <summary>
    /// Main application window.
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// Handles FFmpeg location and launch.
        /// </summary>
        readonly FFmpeg ffmpeg;

        /// <summary>
        /// All possible input tracks, even if they're not assigned.
        /// </summary>
        readonly InputChannel[] inputs;

        /// <summary>
        /// Main application window.
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            ffmpeg = new FFmpeg(null, Settings.Default.ffmpeg);
            inputs = new InputChannel[] {
                fl, fr, fc, lfe, sl, sr, // Bed order doesn't matter, it's handled by FFmpeg
                flc, frc, rl, rr, rc, gv, wl, wr, tfl, tfr, tfc, tsl, tsr // Others are in E-AC-3 channel assignment order
            };
        }

        /// <summary>
        /// Search for FFmpeg's executable.
        /// </summary>
        void LocateFFmpeg(object _, RoutedEventArgs e) {
            ffmpeg.Locate();
            Settings.Default.ffmpeg = ffmpeg.Location;
        }

        /// <summary>
        /// Start merging the selected tracks.
        /// </summary>
        void Merge(object _, RoutedEventArgs e) {
            if (!ffmpeg.Found) {
                Error("FFmpeg wasn't found, please locate.");
                return;
            }
            if (inputs.Count(x => x.Active) > 15) {
                Error("E-AC-3 can only contain 15 full bandwidth channels.");
                return;
            }
            InputChannel[] bedChannels = GetBed();
            if (bedChannels == null) {
                Error("Invalid bed layout. Only 2.0, 4.0, 5.0, and 5.1 are allowed.");
                return;
            }

            SaveFileDialog saver = new SaveFileDialog() {
                Filter = "E-AC-3 files|*.ec3"
            };
            if (!saver.ShowDialog().Value) {
                return;
            }

            AudioReader[] files = GetFiles();
            Dictionary<InputChannel, int> fileMap = Associate(files);
            InputChannel[][] streams = GetSubstreams(bedChannels);
            (long length, int sampleRate) = PrepareFiles(files);
            if (length == -1) {
                return;
            }
            float[][][] channelCache = CreateChannelCache(files);

            // Create outputs
            string baseOutputName = saver.FileName[..saver.FileName.LastIndexOf('.')];
            AudioWriter[] outputs = new AudioWriter[streams.Length];
            for (int i = 0; i < outputs.Length; i++) {
                outputs[i] = AudioWriter.Create($"{baseOutputName} {i}.wav", streams[i].Length, length, sampleRate, BitDepth.Int24);
                outputs[i].WriteHeader();
            }

            long position = 0;
            while (position < length) {
                long samplesThisFrame = length - position;
                if (samplesThisFrame > bufferSize) {
                    samplesThisFrame = bufferSize;
                }

                // Read the last samples from each stream
                for (int i = 0; i < files.Length; i++) {
                    files[i].ReadBlock(channelCache[i], 0, samplesThisFrame);
                }

                // Remix the channels and write them to the output files
                for (int i = 0; i < streams.Length; i++) {
                    float[][] output = new float[streams[i].Length][];
                    for (int j = 0; j < output.Length; j++) {
                        InputChannel stream = streams[i][j];
                        output[j] = channelCache[fileMap[stream]][stream.SelectedChannel];
                    }
                    outputs[i].WriteBlock(output, 0, samplesThisFrame);
                }

                position += bufferSize;
            }

            Stream[] finalSources = new Stream[outputs.Length];
            for (int i = 0; i < outputs.Length; i++) {
                string tempOutputName = $"{baseOutputName} {i}.wav",
                    finalTempName = $"{baseOutputName} {i}.ac3";
                ffmpeg.Launch($"-i \"{tempOutputName}\" -c:a eac3 -y \"{finalTempName}\"");
                outputs[i].Dispose();
                File.Delete(tempOutputName);
                finalSources[i] = File.OpenRead(finalTempName);
            }

            ReferenceChannel[] layout = GetLayout(streams);
            Cavern.Format.Transcoders.EnhancedAC3Merger merger =
                new Cavern.Format.Transcoders.EnhancedAC3Merger(finalSources, layout, saver.FileName);
            position = 0;
            while (!merger.ProcessFrame()) {
                position += 1536;
            }

            for (int i = 0; i < outputs.Length; i++) {
                finalSources[i].Close();
                File.Delete($"{baseOutputName} {i}.ac3");
            }
            for (int i = 0; i < files.Length; i++) {
                files[i].Dispose();
            }
        }

        /// <summary>
        /// Save the settings on exiting.
        /// </summary>
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            Settings.Default.Save();
        }
    }
}