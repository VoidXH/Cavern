using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Measures the frequency response of all output channels.</summary>
    public class SpeakerSweeper : MonoBehaviour {
        /// <summary>Playable measurement signal.</summary>
        AudioClip Sweep;
        /// <summary>Microphone input.</summary>
        AudioClip SweepResponse;
        /// <summary>Cached listener.</summary>
        AudioListener3D Listener;
        /// <summary>The sweep playback object.</summary>
        AudioSource3D Sweeper;
        /// <summary>LFE pass-through before the measurement. LFE pass-through is on while measuring.</summary>
        bool OldDirectLFE;
        /// <summary>LFE separation before the measurement. LFE separation is on while measuring.</summary>
        bool OldLFESeparation;
        /// <summary>Virtualizer before the measurement. Virtualizer is off while measuring.</summary>
        bool OldVirtualizer;
        /// <summary>Response evaluator tasks.</summary>
        Task<float[]>[] Workers;

        /// <summary>Frequency at the beginning of the sweep.</summary>
        [Tooltip("Frequency at the beginning of the sweep.")]
        [Range(1, 24000)] public float StartFreq = 20;
        /// <summary>Frequency at the end of the sweep.</summary>
        [Tooltip("Frequency at the end of the sweep.")]
        [Range(1, 24000)] public float EndFreq = 20000;
        /// <summary>Measurement signal gain relative to full scale.</summary>
        [Tooltip("Measurement signal gain relative to full scale.")]
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

        void OnEnable() {
            ResultAvailable = false;
            Listener = AudioListener3D.Current;
            int SampleRate = Listener.SampleRate;
            SweepReference = Measurements.SweepFraming(Measurements.ExponentialSweep(StartFreq, EndFreq, SweepLength, SampleRate));
            float GainMult = Mathf.Pow(10, SweepGain / 20);
            int SweepSignalLength = SweepReference.Length;
            for (int Sample = 0; Sample < SweepSignalLength; ++Sample)
                SweepReference[Sample] *= GainMult;
            Sweep = AudioClip.Create("Sweep", SweepSignalLength, 1, SampleRate, false);
            Sweep.SetData(SweepReference, 0);
            Channel = 0;
            FreqResponses = new float[AudioListener3D.ChannelCount][];
            ExcitementResponses = new float[AudioListener3D.ChannelCount][];
            Equalizers = new Equalizer[AudioListener3D.ChannelCount];
            Workers = new Task<float[]>[AudioListener3D.ChannelCount];
            OldDirectLFE = Listener.DirectLFE;
            OldLFESeparation = Listener.LFESeparation;
            OldVirtualizer = Listener.HeadphoneVirtualizer;
            Listener.LFESeparation = true;
            Listener.HeadphoneVirtualizer = false;
            MeasurementStart();
        }

        void MeasurementStart() {
            GameObject SweeperObj = new GameObject();
            if (AudioListener3D._EnvironmentType != Environments.Studio)
                SweeperObj.transform.position = CavernUtilities.VectorScale(AudioListener3D.Channels[Channel].CubicalPos, AudioListener3D.EnvironmentSize);
            else
                SweeperObj.transform.position = CavernUtilities.VectorScale(AudioListener3D.Channels[Channel].SphericalPos, AudioListener3D.EnvironmentSize);
            Sweeper = SweeperObj.AddComponent<AudioSource3D>();
            Sweeper.Clip = Sweep;
            Sweeper.IsPlaying = true;
            Sweeper.LFE = AudioListener3D.Channels[Channel].LFE;
            Sweeper.Loop = false;
            Sweeper.VolumeRolloff = Rolloffs.Disabled;
            // TODO: ability to choose input device and adapt to its sample rate on perfect quality
            SweepResponse = Microphone.Start(string.Empty, false, Mathf.CeilToInt(SweepLength * 2 / Listener.SampleRate) + 1, Listener.SampleRate);
        }

        void MeasurementEnd() {
            Destroy(Sweeper.gameObject);
            Microphone.End(string.Empty);
            float[] Result = new float[SweepReference.Length];
            SweepResponse.GetData(Result, 0);
            Destroy(SweepResponse);
            ExcitementResponses[Channel] = Result;
            (Workers[Channel] = new Task<float[]>(() => Measurements.GetFrequencyResponseAbs(SweepReference, Result))).Start();
        }

        void Update() {
            if (ResultAvailable)
                return;
            if (Channel == AudioListener3D.ChannelCount) {
                for (int i = 0, c = Workers.Length; i < c; ++i)
                    if (Workers[i].Result == null)
                        return;
                for (int i = 0, c = Workers.Length; i < c; ++i)
                    FreqResponses[i] = Workers[i].Result;
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
            Listener.DirectLFE = OldDirectLFE;
            Listener.LFESeparation = OldLFESeparation;
            Listener.HeadphoneVirtualizer = OldVirtualizer;
        }
    }
}