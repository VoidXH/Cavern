using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

using Cavern.Filters;
using Cavern.Virtualizer;

namespace Cavern {
    [AddComponentMenu("Audio/3D Audio Listener"), RequireComponent(typeof(AudioListener))]
    public partial class AudioListener3D : MonoBehaviour {
        // ------------------------------------------------------------------
        // Internal vars
        // ------------------------------------------------------------------
        /// <summary>Position between the last and current game frame's playback position.</summary>
        internal static float PulseDelta { get; private set; }
        /// <summary>Required output array size for each <see cref="AudioSource3D.Collect"/> function.</summary>
        internal static int RenderBufferSize { get; private set; }
        /// <summary>The cached length of the <see cref="SourceDistances"/> array.</summary>
        internal static int SourceLimit = 128;
        /// <summary>Distances of sources from the listener.</summary>
        internal static float[] SourceDistances = new float[SourceLimit];
        /// <summary>Cached number of output channels.</summary>
        internal static int ChannelCount { get; private set; }
        /// <summary>Last position of the active listener.</summary>
        internal static Vector3 LastPosition { get; private set; }
        /// <summary>Last rotation of the active listener.</summary>
        internal static Quaternion LastRotation { get; private set; }
        /// <summary>Inverse of the rotation of the active listener.</summary>
        internal static Quaternion LastRotationInverse { get; private set; }

        // ------------------------------------------------------------------
        // Private vars
        // ------------------------------------------------------------------
        /// <summary>List of enabled <see cref="AudioSource3D"/>'s.</summary>
        internal static LinkedList<AudioSource3D> ActiveSources = new LinkedList<AudioSource3D>();

        /// <summary>Listener normalizer gain.</summary>
        static float Normalization = 1;

        /// <summary>Output timer.</summary>
        static int Now = 0;
        /// <summary>Cached <see cref="SampleRate"/> for change detection.</summary>
        static int CachedSampleRate = 0;
        /// <summary>Cached <see cref="UpdateRate"/> for change detection.</summary>
        static int CachedUpdateRate = 0;
        /// <summary>Current time in ticks in the last frame.</summary>
        static long LastTicks = 0;
        /// <summary>Ticks missed by integer division in the last frame. Required for perfect timing.</summary>
        static long AdditionMiss = 0;

        /// <summary>Cached <see cref="Channels"/> for change detection.</summary>
        static Channel3D[] ChannelCache;
        /// <summary>Lowpass filters for each channel.</summary>
        static Lowpass[] Lowpasses;

        // ------------------------------------------------------------------
        // Internal functions
        // ------------------------------------------------------------------
        /// <summary>Reset the listener after any change.</summary>
        void ResetFunc() {
            ChannelCount = Channels.Length;
            CachedSampleRate = SampleRate;
            CachedUpdateRate = UpdateRate;
            BufferPosition = 0;
            LastTicks = DateTime.Now.Ticks;
            Lowpasses = new Lowpass[ChannelCount];
            FilterOutput = new float[ChannelCount * SampleRate];
            // Optimization arrays
            ChannelCache = new Channel3D[ChannelCount];
            for (int i = 0; i < ChannelCount; ++i) {
                ChannelCache[i] = Channels[i].Copy;
                Lowpasses[i] = new Lowpass(SampleRate, 120);
            }
        }

        /// <summary>Normalize an array of samples.</summary>
        /// <param name="Target">Samples to normalize</param>
        /// <param name="TargetLength">Target array size</param>
        /// <param name="LastGain">Last normalizer gain (a reserved float with a default of 1 to always pass to this function)</param>
        void Normalize(ref float[] Target, int TargetLength, ref float LastGain) {
            float Max = Math.Abs(Target[0]), AbsSample;
            for (int Sample = 1; Sample < TargetLength; ++Sample) {
                AbsSample = Math.Abs(Target[Sample]);
                if (Max < AbsSample)
                    Max = AbsSample;
            }
            if (Max * LastGain > 1) // Kick in
                LastGain = .9f / Max;
            CavernUtilities.Gain(Target, TargetLength, LastGain); // Normalize last samples
            // Release
            LastGain += Normalizer * UpdateRate / SampleRate;
            if (LimiterOnly && LastGain > 1)
                LastGain = 1;
        }

        /// <summary>The function to initially call when samples are available, to feed them to the filter.</summary>
        void Finalization() {
            if (!Paused) {
                float[] SourceBuffer = Output;
                int SourceBufferSize = Output.Length;
                if (HeadphoneVirtualizer)
                    VirtualizerFilter.Process(SourceBuffer);
                if (SystemSampleRate != CachedSampleRate) { // Resample output for system sample rate
                    float[][] ChannelSplit = new float[ChannelCount][];
                    for (int Channel = 0; Channel < ChannelCount; ++Channel)
                        ChannelSplit[Channel] = new float[UpdateRate];
                    int OutputSample = 0;
                    for (int Sample = 0; Sample < UpdateRate; ++Sample)
                        for (int Channel = 0; Channel < ChannelCount; ++Channel)
                            ChannelSplit[Channel][Sample] = SourceBuffer[OutputSample++];
                    for (int Channel = 0; Channel < ChannelCount; ++Channel)
                        ChannelSplit[Channel] = AudioSource3D.Resample(ChannelSplit[Channel], UpdateRate,
                            (int)(UpdateRate * SystemSampleRate / (float)CachedSampleRate));
                    int NewUpdateRate = ChannelSplit[0].Length;
                    SourceBuffer = new float[SourceBufferSize = ChannelCount * NewUpdateRate];
                    OutputSample = 0;
                    for (int Sample = 0; Sample < NewUpdateRate; ++Sample)
                        for (int Channel = 0; Channel < ChannelCount; ++Channel)
                            SourceBuffer[OutputSample++] = ChannelSplit[Channel][Sample];
                }
                int End = FilterOutput.Length;
                lock (BufferLock) {
                    int AltEnd = BufferPosition + SourceBufferSize;
                    if (End > AltEnd)
                        End = AltEnd;
                    int OutputPos = 0;
                    for (int BufferWrite = BufferPosition; BufferWrite < End; ++BufferWrite)
                        FilterOutput[BufferWrite] = SourceBuffer[OutputPos++];
                    BufferPosition = End;
                }
            } else
                FilterOutput = new float[ChannelCount * CachedSampleRate];
        }

        void Awake() {
            if (Current) {
                UnityEngine.Debug.LogError("There can be only one 3D audio listener per scene.");
                Destroy(Current);
            }
            Current = this;
            OnOutputAvailable = Finalization; // Call finalization when samples are available
            SystemSampleRate = AudioSettings.GetConfiguration().sampleRate;
            ChannelCount = 0;
            string FileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Cavern\\Save.dat";
            if (File.Exists(FileName)) {
                string[] Save = File.ReadAllLines(FileName);
                int SavePos = 1;
                int ChannelLength = Convert.ToInt32(Save[0]);
                Channels = new Channel3D[ChannelLength];
                NumberFormatInfo Format = new NumberFormatInfo {
                    NumberDecimalSeparator = ","
                };
                for (int i = 0; i < ChannelLength; ++i)
                    Channels[i] = new Channel3D(Convert.ToSingle(Save[SavePos++], Format), Convert.ToSingle(Save[SavePos++], Format),
                        Convert.ToBoolean(Save[SavePos++]));
                _EnvironmentType = (Environments)Convert.ToInt32(Save[SavePos++], Format);
                EnvironmentSize = new Vector3(Convert.ToSingle(Save[SavePos++], Format), Convert.ToSingle(Save[SavePos++], Format),
                    Convert.ToSingle(Save[SavePos++], Format));
                HeadphoneVirtualizer = Save.Length > SavePos ? Convert.ToBoolean(Save[SavePos++]) : false; // Added: 2016.04.24.
                EnvironmentCompensation = Save.Length > SavePos ? Convert.ToBoolean(Save[SavePos++]) : false; // Added: 2017.06.18.
            }
            ResetFunc();
        }

        /// <summary>Single update tick rendering.</summary>
        void Frame(int OutputLength) {
            // Collect audio data from sources
            RenderBufferSize = UpdateRate * ChannelCount;
            LinkedListNode<AudioSource3D> Node = ActiveSources.First;
            List<float[]> Results = new List<float[]>();
            while (Node != null) {
                if (Node.Value.Precollect())
                    Results.Add(Node.Value.Collect());
                Node = Node.Next;
            }
            // Mix sources to output
            Array.Clear(Output, 0, OutputLength);
            for (int Result = 0, ResultCount = Results.Count; Result < ResultCount; ++Result)
                CavernUtilities.Mix(Results[Result], Output, OutputLength);
            // Volume, distance compensation, and subwoofers' lowpass
            for (int Channel = 0; Channel < ChannelCount; ++Channel) {
                if (Channels[Channel].LFE) {
                    if (!DirectLFE)
                        Lowpasses[Channel].Process(Output, Channel, ChannelCount);
                    CavernUtilities.Gain(Output, UpdateRate, LFEVolume * Volume, Channel, ChannelCount); // LFE Volume
                } else
                    CavernUtilities.Gain(Output, UpdateRate, !EnvironmentCompensation ? Volume :
                        (Volume * Channels[Channel].Distance * CavernUtilities.Sqrt2p2), Channel, ChannelCount);
            }
            if (Normalizer != 0) // Normalize
                Normalize(ref Output, OutputLength, ref Normalization);
        }

        void Update() {
            // Change checks
            if (HeadphoneVirtualizer) // Virtual channels
                VirtualizerFilter.SetLayout();
            if (ChannelCount != Channels.Length || CachedSampleRate != SampleRate || CachedUpdateRate != UpdateRate)
                ResetFunc();
            LastPosition = transform.position;
            LastRotationInverse = Quaternion.Inverse(LastRotation = transform.rotation);
            // Timing
            if (!Manual) {
                long TicksNow = DateTime.Now.Ticks;
                long TimePassed = (TicksNow - LastTicks) * SampleRate + AdditionMiss;
                long Addition = TimePassed / TimeSpan.TicksPerSecond;
                AdditionMiss = TimePassed % TimeSpan.TicksPerSecond;
                Now += (int)Addition;
                LastTicks = TicksNow;
                if (Now > SampleRate >> 2) // Lag compensation: don't process more than 1/4 of a second
                    Now = SampleRate >> 2;
            } else
                Now = UpdateRate;
            // Don't work with wrong settings
            if (SampleRate < 44100 || UpdateRate < 16)
                return;
            // Output buffer creation
            int OutputLength = ChannelCount * UpdateRate;
            if (Output.Length == OutputLength)
                Array.Clear(Output, 0, OutputLength);
            else
                Output = new float[OutputLength];
            // Choose processing functions
            AudioSource3D.UsedAngleMatchFunc = AudioQuality >= QualityModes.High ? // Only calculate accurate arc cosine above high quality
                (AudioSource3D.AngleMatchFunc)AudioSource3D.CalculateAngleMatches : AudioSource3D.LinearizeAngleMatches;
            if (UpdateRate <= Now) {
                // Set up sound collection environment
                for (int Source = 0; Source < MaximumSources; ++Source)
                    SourceDistances[Source] = Range;
                PulseDelta = Now /(float)SampleRate;
                LinkedListNode<AudioSource3D> Node = ActiveSources.First;
                while (Node != null) {
                    Node.Value.Precalculate();
                    Node = Node.Next;
                }
                while (UpdateRate <= Now) {
                    if (!Paused || Manual)
                        Frame(OutputLength);
                    // Finalize
                    OnOutputAvailable();
                    Now -= UpdateRate;
                }
                Manual = false;
            }
        }

        // ------------------------------------------------------------------
        // Filter output
        // ------------------------------------------------------------------
        /// <summary>Filter buffer position, samples currently cached for output.</summary>
        static int BufferPosition = 0;
        /// <summary>Samples to play with the filter.</summary>
        static float[] FilterOutput;
        /// <summary>Lock for the <see cref="BufferPosition"/>, which is set in multiple threads.</summary>
        static object BufferLock = new object();
        /// <summary>Filter normalizer gain.</summary>
        static float FilterNormalizer = 1;
        /// <summary>Cached system sample rate.</summary>
        static int SystemSampleRate;

        /// <summary>Output Cavern's generated audio as a filter.</summary>
        /// <param name="UnityBuffer">Output buffer</param>
        /// <param name="UnityChannels">Output channel count</param>
        void OnAudioFilterRead(float[] UnityBuffer, int UnityChannels) {
            if (BufferPosition == 0)
                return;
            int SamplesPerChannel = UnityBuffer.Length / UnityChannels;
            int End = Math.Min(BufferPosition, SamplesPerChannel * ChannelCount);
            // Output audio
            if (UnityChannels <= 4) { // For non-surround setups, downmix properly
                for (int Channel = 0; Channel < ChannelCount; ++Channel) {
                    int UnityChannel = Channel % UnityChannels;
                    if (Channel != 2 && Channel != 3)
                        for (int Sample = 0; Sample < SamplesPerChannel; ++Sample)
                            UnityBuffer[Sample * UnityChannels + UnityChannel] += FilterOutput[Sample * ChannelCount + Channel];
                    else {
                        for (int Sample = 0; Sample < SamplesPerChannel; ++Sample) {
                            int LeftOut = Sample * UnityChannels;
                            float CopySample = FilterOutput[Sample * ChannelCount + Channel];
                            UnityBuffer[LeftOut] += CopySample;
                            UnityBuffer[LeftOut + 1] += CopySample;
                        }
                    }
                }
            } else {
                for (int Channel = 0; Channel < ChannelCount; ++Channel) {
                    int UnityChannel = Channel % UnityChannels;
                    for (int Sample = 0; Sample < SamplesPerChannel; ++Sample)
                        UnityBuffer[Sample * UnityChannels + UnityChannel] += FilterOutput[Sample * ChannelCount + Channel];
                }
            }
            if (Normalizer != 0) // Normalize
                Normalize(ref UnityBuffer, UnityBuffer.Length, ref FilterNormalizer);
            // Remove used samples
            lock (BufferLock) {
                for (int BufferPos = End; BufferPos < BufferPosition; ++BufferPos)
                    FilterOutput[BufferPos - End] = FilterOutput[BufferPos];
                int MaxLatency = ChannelCount * CachedSampleRate / DelayTarget;
                if (BufferPosition < MaxLatency)
                    BufferPosition -= End;
                else
                    BufferPosition = 0;
            }
        }
    }
}