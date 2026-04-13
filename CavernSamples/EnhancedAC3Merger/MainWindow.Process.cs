using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Format;

using VoidX.WPF;
using VoidX.WPF.FFmpeg;

namespace EnhancedAC3Merger;

partial class MainWindow {
    /// <summary>
    /// Perform the merging process.
    /// </summary>
    void Process(TaskEngine engine, InputChannel[][] streams, string fileName) {
        engine.Progress = 0;
        AudioReader[] files = GetFiles();
        Dictionary<InputChannel, int> fileMap = Associate(files);
        (long length, int sampleRate) = PrepareFiles(files);
        if (length == -1) {
            return;
        }
        float[][][] channelCache = CreateChannelCache(files);

        // Create outputs
        string baseOutputName = fileName[..fileName.LastIndexOf('.')];
        string[] outputFiles = new string[streams.Length];
        AudioWriter[] outputs = new AudioWriter[streams.Length];
        int[][] selectedChannels = new int[streams.Length][];
        for (int i = 0; i < outputs.Length; i++) {
            outputFiles[i] = $"{baseOutputName} {i}.wav";
            outputs[i] = AudioWriter.Create(outputFiles[i], streams[i].Length, length, sampleRate, BitDepth.Int24);
            outputs[i].WriteHeader();
            int[] channelSource = selectedChannels[i] = new int[streams[i].Length];
            for (int j = 0; j < channelSource.Length; j++) {
                channelSource[j] = streams[i][j].SelectedChannel;
            }
        }

        long position = 0;
        double progressScale = .25 / length;
        while (position < length) {
            if ((position & 0xFFFF) == 0) {
                engine.UpdateProgressBar(position * progressScale);
            }
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
                InputChannel[] streamSource = streams[i];
                int[] channelSource = selectedChannels[i];
                for (int j = 0; j < output.Length; j++) {
                    output[j] = channelCache[fileMap[streamSource[j]]][channelSource[j]];
                }
                outputs[i].WriteBlock(output, 0, samplesThisFrame);
            }

            position += bufferSize;
        }

        engine.UpdateProgressBar(.25);
        Stream[] finalSources = new Stream[outputs.Length];
        for (int i = 0; i < outputs.Length; i++) {
            outputs[i].Dispose();

            FFmpegConfigurator config = new() {
                OutputFile = $"{baseOutputName} {i}.ec3"
            };
            config.AddInputFile(outputFiles[i]);
            config.SetCodec(FFmpegStream.Audio, "eac3");
            config.SetOverwrite(true);

            ffmpeg.Launch(config.ToString());
            File.Delete(outputFiles[i]);
            finalSources[i] = File.OpenRead(config.OutputFile);
        }

        ReferenceChannel[] layout = GetLayout(streams);
        Cavern.Format.Transcoders.EnhancedAC3Merger merger =
            new Cavern.Format.Transcoders.EnhancedAC3Merger(finalSources, layout, fileName);
        try { // Only slow down for checking consistency in the first frame - exceptions that would happen will be here too
            merger.ProcessFrame();
        } catch (Exception e) {
            Error(e.Message);
            return;
        }

        position = 1536;
        progressScale = .75 / length;
        while (!merger.ProcessFrame()) {
            if ((position & 0xFFFF) == 0) {
                engine.UpdateProgressBar(.25 + position * progressScale);
            }
            position += 1536;
        }

        for (int i = 0; i < outputs.Length; i++) {
            finalSources[i].Close();
            File.Delete($"{baseOutputName} {i}.ac3");
        }
        for (int i = 0; i < files.Length; i++) {
            files[i].Dispose();
        }
        engine.Progress = 1;
    }
}
