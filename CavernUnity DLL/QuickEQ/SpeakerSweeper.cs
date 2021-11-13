using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UnityEngine;

using Cavern.Utilities;
using Cavern.QuickEQ.Equalization;
using Cavern.QuickEQ.SignalGeneration;

namespace Cavern.QuickEQ {
    /// <summary>Measures the frequency response of all output channels.</summary>
    [AddComponentMenu("Audio/QuickEQ/Speaker Sweeper")]
    public class SpeakerSweeper : MonoBehaviour, IDisposable {
        /// <summary>Frequency at the beginning of the sweep.</summary>
        [Tooltip("Frequency at the beginning of the sweep.")]
        [Range(1, 24000)] public float StartFreq = 20;
        /// <summary>Frequency at the end of the sweep.</summary>
        [Tooltip("Frequency at the end of the sweep.")]
        [Range(1, 24000)] public float EndFreq = 20000;
        /// <summary>Maximum checked frequency for LFE channels. Other frequencites will be suppressed.</summary>
        [Tooltip("Maximum checked frequency for LFE channels. Other frequencites will be suppressed.")]
        [Range(1, 24000)] public float EndFreqLFE = 200;
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

        /// <summary>Change the measurement signal to this tone.</summary>
        public float[] Bypass {
            get => bypass;
            set {
                bypass = value;
                RegenerateSweep();
            }
        }
        float[] bypass;
        /// <summary>Measurement signal samples.</summary>
        public float[] SweepReference { get; private set; }
        /// <summary>Channel under measurement. If <see cref="ResultAvailable"/> is false, but this equals the channel count,
        /// <see cref="FreqResponses"/> are still being processed.</summary>
        public int Channel { get; private set; }

        /// <summary>Measurement sample rate. Set after an <see cref="InputDevice"/> was selected.</summary>
        public int SampleRate {
            get {
                if (sampleRate != 0)
                    return sampleRate;
                return sampleRate = AudioListener3D.Current.SampleRate;
            }
            internal set => sampleRate = value;
        }
        int sampleRate;

        /// <summary>Progress of the measurement process from 0 to 1.</summary>
        public float Progress {
            get {
                if (ResultAvailable)
                    return 1;
                else if (!enabled || sweepers == null)
                    return 0;
                return (float)sweepers[Listener.Channels.Length - 1].timeSamples / SweepReference.Length / Listener.Channels.Length;
            }
        }

        /// <summary>Name of the recording device. If empty, the system default will be used.</summary>
        public string InputDevice {
            get {
                if (inputDevice != null)
                    return inputDevice;
                return InputDevice = string.Empty;
            }
            set {
                int oldSampleRate = SampleRate;
                sweepResponse = Microphone.Start(value, false, 1, AudioListener3D.Current.SampleRate);
                if (sweepResponse != null) {
                    Microphone.End(value);
                    Destroy(sweepResponse);
                    SampleRate = AudioListener3D.Current.SampleRate;
                } else {
                    Microphone.GetDeviceCaps(value, out int min, out _);
                    if (min == 0)
                        SampleRate = 48000;
                    else
                        SampleRate = min;
                }
                if (SampleRate != oldSampleRate)
                    RegenerateSweep();
                inputDevice = value;
            }
        }
        string inputDevice = null;

        /// <summary>Microphone input.</summary>
        AudioClip sweepResponse;
        /// <summary>A hack to fix lost playback from the initial hanging caused by sweep generation in <see cref="OnEnable"/>.</summary>
        bool measurementStarted;
        /// <summary>LFE pass-through before the measurement. LFE pass-through is on while measuring.</summary>
        bool oldDirectLFE;
        /// <summary>Virtualizer before the measurement. Virtualizer is off while measuring.</summary>
        bool oldVirtualizer;
        /// <summary>Measurement signal's Fourier transform for response calculation optimizations.</summary>
        Complex[] sweepFFT;
        /// <summary>Measurement signal's Fourier transform for response calculation optimizations
        /// and removed high frequencies for LFE noise suppression.</summary>
        Complex[] sweepFFTlow;
        /// <summary>FFT constant cache for the sweep FFT size.</summary>
        FFTCache sweepFFTCache;
        /// <summary>Sweep playback objects.</summary>
        SweepChannel[] sweepers;
        /// <summary>Response evaluator tasks.</summary>
        Task<WorkerResult>[] workers;

        /// <summary>Generate <see cref="SweepReference"/> and the related optimization values.</summary>
        public void RegenerateSweep() {
            if (bypass == null)
                SweepReference = SweepGenerator.Frame(SweepGenerator.Exponential(StartFreq, EndFreq, SweepLength, SampleRate));
            else
                SweepReference = (float[])bypass.Clone();
            float gainMult = Mathf.Pow(10, SweepGain / 20);
            WaveformUtils.Gain(SweepReference, gainMult);
            if (sweepFFTCache != null)
                sweepFFTCache.Dispose();
            sweepFFT = Measurements.FFT(SweepReference, sweepFFTCache = new FFTCache(SweepReference.Length));
            sweepFFTlow = sweepFFT.FastClone();
            Measurements.OffbandGain(sweepFFT, StartFreq, EndFreq, SampleRate, 100);
            Measurements.OffbandGain(sweepFFTlow, StartFreq, EndFreqLFE, sampleRate, 100);
        }

        /// <summary>Get the frequency response of an external measurement that was performed with the current <see cref="sweepFFT"/>.</summary>
        public Complex[] GetFrequencyResponse(float[] samples, bool LFE) =>
            Measurements.GetFrequencyResponse(LFE ? sweepFFTlow : sweepFFT, Measurements.FFT(samples, sweepFFTCache));

        /// <summary>Get the impulse response of a frequency response generated with <see cref="GetFrequencyResponse(float[], bool)"/>.</summary>
        public VerboseImpulseResponse GetImpulseResponse(Complex[] frequencyResponse) =>
            new VerboseImpulseResponse(Measurements.GetImpulseResponse(frequencyResponse, sweepFFTCache));

        [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            ResultAvailable = false;
            RegenerateSweep();
            Channel = 0;
            int channels = Listener.Channels.Length;
            FreqResponses = new float[channels][];
            ImpResponses = new VerboseImpulseResponse[channels];
            ExcitementResponses = new float[channels][];
            Equalizers = new Equalizer[channels];
            workers = new Task<WorkerResult>[channels];
            oldDirectLFE = AudioListener3D.Current.DirectLFE;
            oldVirtualizer = Listener.HeadphoneVirtualizer;
            AudioListener3D.Current.DirectLFE = true;
            Listener.HeadphoneVirtualizer = false;
            measurementStarted = false;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!measurementStarted) {
                measurementStarted = true;
                sweepers = new SweepChannel[Listener.Channels.Length];
                for (int i = 0; i < Listener.Channels.Length; ++i) {
                    sweepers[i] = gameObject.AddComponent<SweepChannel>();
                    sweepers[i].Channel = i;
                    sweepers[i].Sweeper = this;
                }
                if (Microphone.devices.Length != 0)
                    sweepResponse = Microphone.Start(InputDevice, false, SweepReference.Length * Listener.Channels.Length / SampleRate + 1,
                        SampleRate);
            }
            if (ResultAvailable)
                return;
            if (!sweepers[Channel].IsPlaying) {
                float[] result = new float[SweepReference.Length];
                if (sweepResponse)
                    sweepResponse.GetData(result, Channel * SweepReference.Length);
                else
                    result = result.FastClone();
                ExcitementResponses[Channel] = result;
                Complex[] fft = Cavern.Channel.IsLFE(Channel) ? sweepFFTlow : sweepFFT;
                (workers[Channel] = new Task<WorkerResult>(() =>
                    new WorkerResult(fft, sweepFFTCache, result))).Start();
                if (++Channel == Listener.Channels.Length) {
                    for (int channel = 0; channel < Listener.Channels.Length; ++channel)
                        if (workers[channel].Result.IsNull())
                            return;
                    for (int channel = 0; channel < Listener.Channels.Length; ++channel) {
                        FreqResponses[channel] = workers[channel].Result.FreqResponse;
                        ImpResponses[channel] = workers[channel].Result.ImpResponse;
                        Destroy(sweepers[channel]);
                    }
                    sweepers = null;
                    if (Microphone.IsRecording(InputDevice))
                        Microphone.End(InputDevice);
                    if (sweepResponse)
                        Destroy(sweepResponse);
                    ResultAvailable = true;
                }
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() {
            if (sweepers != null && sweepers[0])
                for (int channel = 0; channel < Listener.Channels.Length; ++channel)
                    Destroy(sweepers[channel]);
            sweepers = null;
            if (sweepResponse)
                Destroy(sweepResponse);
            AudioListener3D.Current.DirectLFE = oldDirectLFE;
            Listener.HeadphoneVirtualizer = oldVirtualizer;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDestroy() => Dispose();
        
        /// <summary>Free the resources used by this object.</summary>
        public void Dispose() {
            if (sweepFFTCache != null)
                sweepFFTCache.Dispose();
        }

        struct WorkerResult {
            public float[] FreqResponse;
            public VerboseImpulseResponse ImpResponse;

            public WorkerResult(Complex[] sweepFFT, FFTCache sweepFFTCache, float[] response) {
                Complex[] rawResponse = Measurements.GetFrequencyResponse(sweepFFT, Measurements.FFT(response, sweepFFTCache));
                FreqResponse = Measurements.GetSpectrum(rawResponse);
                ImpResponse = new VerboseImpulseResponse(Measurements.GetImpulseResponse(rawResponse, sweepFFTCache));
            }

            public bool IsNull() => FreqResponse == null || ImpResponse == null;
        }
    }
}