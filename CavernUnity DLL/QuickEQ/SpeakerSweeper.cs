using System;
using System.Threading.Tasks;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.QuickEQ {
    /// <summary>Measures the frequency response of all output channels.</summary>
    [AddComponentMenu("Audio/QuickEQ/Speaker Sweeper")]
    public class SpeakerSweeper : MonoBehaviour {
        /// <summary>Playable measurement signal.</summary>
        AudioClip Sweep;
        /// <summary>Microphone input.</summary>
        AudioClip SweepResponse;
        /// <summary>Cached listener.</summary>
        AudioListener3D Listener;
        /// <summary>A hack to fix lost playback from the initial hanging caused by sweep generation in <see cref="OnEnable"/>.</summary>
        bool MeasurementStarted;
        /// <summary>Environment compensation before the measurement. Environment compensation is off while measuring.</summary>
        bool OldCompensation;
        /// <summary>LFE pass-through before the measurement. LFE pass-through is on while measuring.</summary>
        bool OldDirectLFE;
        /// <summary>Virtualizer before the measurement. Virtualizer is off while measuring.</summary>
        bool OldVirtualizer;
        /// <summary>Measurement signal's Fourier transform for response calculation optimizations.</summary>
        Complex[] SweepFFT;
        /// <summary>FFT constant cache for the sweep FFT size.</summary>
        FFTCache SweepFFTCache;
        /// <summary>Sweep playback objects.</summary>
        SweepChannel[] Sweepers;
        /// <summary>Response evaluator tasks.</summary>
        Task<WorkerResult>[] Workers;

        /// <summary>Name of the recording device. If empty, de system default will be used.</summary>
        [Tooltip("Name of the recording device. If empty, de system default will be used.")]
        public string InputDevice = string.Empty;
        /// <summary>Frequency at the beginning of the sweep.</summary>
        [Tooltip("Frequency at the beginning of the sweep.")]
        [Range(1, 24000)] public float StartFreq = 20;
        /// <summary>Frequency at the end of the sweep.</summary>
        [Tooltip("Frequency at the end of the sweep.")]
        [Range(1, 24000)] public float EndFreq = 20000;
        /// <summary>Measurement signal gain in decibels relative to full scale.</summary>
        [Tooltip("Measurement signal gain in decibels relative to full scale.")]
        [Range(-50, 0)] public float SweepGain = -20;
        /// <summary>Length of the measurement signal. Must be a power of 2.</summary>
        [Tooltip("Length of the measurement signal. Must be a power of 2.")]
        public int SweepLength = 32768;

        /// <summary>The measurement is done and responses are available.</summary>
        [NonSerialized] public bool ResultAvailable;
        /// <summary>Raw recorded signals of output channels.</summary>
        [NonSerialized] public float[][] ExcitementResponses;
        /// <summary>Frequency responses of output channels.</summary>
        [NonSerialized] public float[][] FreqResponses;
        /// <summary>Impulse responses of output channels.</summary>
        [NonSerialized] public VerboseImpulseResponse[] ImpResponses;
        /// <summary>Room correction, equalizer for each channel.</summary>
        [NonSerialized] public Equalizer[] Equalizers;

        /// <summary>Measurement signal samples.</summary>
        public float[] SweepReference { get; private set; }
        /// <summary>Channel under measurement. If <see cref="ResultAvailable"/> is false, but this equals the channel count,
        /// <see cref="FreqResponses"/> are still being processed.</summary>
        public int Channel { get; private set; }

        /// <summary>Progress of the measurement process from 0 to 1.</summary>
        public float Progress {
            get {
                if (ResultAvailable)
                    return 1;
                else if (!enabled)
                    return 0;
                return (float)Sweepers[AudioListener3D.ChannelCount - 1].timeSamples / SweepReference.Length / AudioListener3D.ChannelCount;
            }
        }

        /// <summary>Generate <see cref="SweepReference"/> and the related optimization values.</summary>
        public void RegenerateSweep() {
            SweepReference = Measurements.SweepFraming(Measurements.ExponentialSweep(StartFreq, EndFreq, SweepLength, AudioListener3D.Current.SampleRate));
            float GainMult = Mathf.Pow(10, SweepGain / 20);
            for (int Sample = 0, Length = SweepReference.Length; Sample < Length; ++Sample)
                SweepReference[Sample] *= GainMult;
            SweepFFT = Measurements.FFT(SweepReference, SweepFFTCache = new FFTCache(SweepReference.Length));
        }

        /// <summary>Get the frequency response of an external measurement that was performed with the current <see cref="SweepFFT"/>.</summary>
        public Complex[] GetFrequencyResponse(float[] Samples) => Measurements.GetFrequencyResponse(SweepFFT, Measurements.FFT(Samples, SweepFFTCache));

        /// <summary>Get the impulse response of a frequency response generated with <see cref="GetFrequencyResponse(float[])"/>.</summary>
        public VerboseImpulseResponse GetImpulseResponse(Complex[] FrequencyResponse) =>
            new VerboseImpulseResponse(Measurements.GetImpulseResponse(FrequencyResponse, SweepFFTCache));

        void OnEnable() {
            ResultAvailable = false;
            Listener = AudioListener3D.Current;
            RegenerateSweep();
            Sweep = AudioClip.Create("Sweep", SweepReference.Length, 1, Listener.SampleRate, false);
            Sweep.SetData(SweepReference, 0);
            Channel = 0;
            FreqResponses = new float[AudioListener3D.ChannelCount][];
            ImpResponses = new VerboseImpulseResponse[AudioListener3D.ChannelCount];
            ExcitementResponses = new float[AudioListener3D.ChannelCount][];
            Equalizers = new Equalizer[AudioListener3D.ChannelCount];
            Workers = new Task<WorkerResult>[AudioListener3D.ChannelCount];
            OldCompensation = AudioListener3D.EnvironmentCompensation;
            OldDirectLFE = Listener.DirectLFE;
            OldVirtualizer = Listener.HeadphoneVirtualizer;
            AudioListener3D.EnvironmentCompensation = false;
            Listener.DirectLFE = true;
            Listener.HeadphoneVirtualizer = false;
            MeasurementStarted = false;
        }

        void Update() {
            int Channels = AudioListener3D.ChannelCount;
            if (!MeasurementStarted) {
                MeasurementStarted = true;
                SweepResponse = Microphone.Start(InputDevice, false, SweepReference.Length * Channels / Listener.SampleRate + 1, Listener.SampleRate);
                Sweepers = new SweepChannel[Channels];
                for (int i = 0; i < Channels; ++i) {
                    Sweepers[i] = gameObject.AddComponent<SweepChannel>();
                    Sweepers[i].Channel = i;
                    Sweepers[i].Sweeper = this;
                }
            }
            if (ResultAvailable)
                return;
            if (!Sweepers[Channel].IsPlaying) {
                float[] Result = new float[SweepReference.Length];
                SweepResponse.GetData(Result, Channel * SweepReference.Length);
                ExcitementResponses[Channel] = Result;
                (Workers[Channel] = new Task<WorkerResult>(() => new WorkerResult(SweepFFT, SweepFFTCache, Result))).Start();
                if (++Channel == Channels) {
                    for (int i = 0; i < Channels; ++i)
                        if (Workers[i].Result.IsNull())
                            return;
                    for (int i = 0; i < Channels; ++i) {
                        FreqResponses[i] = Workers[i].Result.FreqResponse;
                        ImpResponses[i] = Workers[i].Result.ImpResponse;
                        Destroy(Sweepers[i]);
                    }
                    if (Microphone.IsRecording(InputDevice))
                        Microphone.End(InputDevice);

                    Destroy(SweepResponse);
                    ResultAvailable = true;
                }
            }
        }

        void OnDisable() {
            Destroy(Sweep);
            if (Sweepers[0])
                for (int i = 0, c = AudioListener3D.ChannelCount; i < c; ++i)
                    Destroy(Sweepers[i]);
            if (SweepResponse)
                Destroy(SweepResponse);
            AudioListener3D.EnvironmentCompensation = OldCompensation;
            Listener.DirectLFE = OldDirectLFE;
            Listener.HeadphoneVirtualizer = OldVirtualizer;
        }

        struct WorkerResult {
            public float[] FreqResponse;
            public VerboseImpulseResponse ImpResponse;

            public WorkerResult(Complex[] SweepFFT, FFTCache SweepFFTCache, float[] Response) {
                Complex[] RawResponse = Measurements.GetFrequencyResponse(SweepFFT, Measurements.FFT(Response, SweepFFTCache));
                FreqResponse = Measurements.GetSpectrum(RawResponse);
                ImpResponse = new VerboseImpulseResponse(Measurements.GetImpulseResponse(RawResponse, SweepFFTCache));
            }

            public bool IsNull() => FreqResponse == null || ImpResponse == null;
        }
    }
}