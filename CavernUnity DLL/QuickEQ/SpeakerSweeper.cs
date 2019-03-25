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
        /// <summary>The sweep playback object.</summary>
        AudioSource3D Sweeper;
        /// <summary>A hack to fix lost playback from the initial hanging caused by sweep generation in <see cref="OnEnable"/>.</summary>
        bool MeasurementStarted;
        /// <summary>Environment compensation before the measurement. Environment compensation is off while measuring.</summary>
        bool OldCompensation;
        /// <summary>LFE pass-through before the measurement. LFE pass-through is on while measuring.</summary>
        bool OldDirectLFE;
        /// <summary>LFE separation before the measurement. LFE separation is on while measuring.</summary>
        bool OldLFESeparation;
        /// <summary>Virtualizer before the measurement. Virtualizer is off while measuring.</summary>
        bool OldVirtualizer;
        /// <summary>Measurement signal's Fourier transform for response calculation optimizations.</summary>
        Complex[] SweepFFT;
        /// <summary>FFT constant cache for the sweep FFT size.</summary>
        FFTCache SweepFFTCache;
        /// <summary>Quality mode before the measurement. Quality is set to Low while measuring for constant gain panning.</summary>
        QualityModes OldQuality;
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
                return Channel / (float)AudioListener3D.ChannelCount + (Sweeper.time / Sweep.length) / AudioListener3D.ChannelCount;
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
            OldLFESeparation = Listener.LFESeparation;
            OldVirtualizer = Listener.HeadphoneVirtualizer;
            OldQuality = Listener.AudioQuality;
            AudioListener3D.EnvironmentCompensation = false;
            Listener.DirectLFE = true;
            Listener.LFESeparation = true;
            Listener.HeadphoneVirtualizer = false;
            Listener.AudioQuality = QualityModes.Low;
            MeasurementStarted = false;
        }

        void MeasurementStart() {
            GameObject SweeperObj = new GameObject();
            if (AudioListener3D._EnvironmentType != Environments.Studio)
                SweeperObj.transform.position = Vector3.Scale(AudioListener3D.Channels[Channel].CubicalPos, AudioListener3D.EnvironmentSize);
            else
                SweeperObj.transform.position = Vector3.Scale(AudioListener3D.Channels[Channel].SphericalPos, AudioListener3D.EnvironmentSize);
            Sweeper = SweeperObj.AddComponent<AudioSource3D>();
            Sweeper.Clip = Sweep;
            Sweeper.IsPlaying = true;
            Sweeper.LFE = AudioListener3D.Channels[Channel].LFE;
            Sweeper.Loop = false;
            Sweeper.VolumeRolloff = Rolloffs.Disabled;
            SweepResponse = Microphone.Start(InputDevice, false, Mathf.CeilToInt(SweepLength * 2 / Listener.SampleRate) + 1, Listener.SampleRate);
        }

        void MeasurementEnd() {
            Destroy(Sweeper.gameObject);
            Microphone.End(InputDevice);
            float[] Result = new float[SweepReference.Length];
            SweepResponse.GetData(Result, 0);
            Destroy(SweepResponse);
            ExcitementResponses[Channel] = Result;
            (Workers[Channel] = new Task<WorkerResult>(() => new WorkerResult(SweepFFT, SweepFFTCache, Result))).Start();
        }

        void Update() {
            if (!MeasurementStarted) {
                MeasurementStarted = true;
                MeasurementStart();
            }
            if (ResultAvailable)
                return;
            if (Channel == AudioListener3D.ChannelCount) {
                for (int i = 0, c = Workers.Length; i < c; ++i)
                    if (Workers[i].Result.IsNull())
                        return;
                for (int i = 0, c = Workers.Length; i < c; ++i) {
                    FreqResponses[i] = Workers[i].Result.FreqResponse;
                    ImpResponses[i] = Workers[i].Result.ImpResponse;
                }
                ResultAvailable = true;
                return;
            }
            if (!Sweeper.IsPlaying) {
                MeasurementEnd();
                if (++Channel != AudioListener3D.ChannelCount)
                    MeasurementStart();
            }
        }

        void OnDisable() {
            Destroy(Sweep);
            if (Sweeper)
                Destroy(Sweeper.gameObject);
            if (SweepResponse)
                Destroy(SweepResponse);
            AudioListener3D.EnvironmentCompensation = OldCompensation;
            Listener.DirectLFE = OldDirectLFE;
            Listener.LFESeparation = OldLFESeparation;
            Listener.HeadphoneVirtualizer = OldVirtualizer;
            Listener.AudioQuality = OldQuality;
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