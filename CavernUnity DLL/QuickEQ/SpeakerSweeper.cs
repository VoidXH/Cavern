using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Cavern.QuickEQ {
    /// <summary>Measures the frequency response of all output channels.</summary>
    public class SpeakerSweeper : MonoBehaviour {
        const float FreqStart = 20, FreqEnd = 20000;
        /// <summary>Playable measurement signal.</summary>
        AudioClip Sweep;
        /// <summary>Microphone input.</summary>
        AudioClip SweepResponse;
        /// <summary>Cached listener.</summary>
        AudioListener3D Listener;
        /// <summary>The sweep playback object.</summary>
        AudioSource3D Sweeper;
        /// <summary>LFE separation before the measurement. LFE separation is on while measuring.</summary>
        bool OldLFESeparation;
        /// <summary>Virtualizer before the measurement. Virtualizer is off while measuring.</summary>
        bool OldVirtualizer;
        /// <summary>Measurement signal samples.</summary>
        float[] SweepReference;
        /// <summary>Response evaluator tasks.</summary>
        Task<float[]>[] Workers;

        /// <summary>Length of the measurement signal. Must be a power of 2.</summary>
        public int SweepLength = 32768;

        /// <summary>The measurement is done and responses are available.</summary>
        [NonSerialized] public bool ResultAvailable;
        /// <summary>Frequency responses of output channels.</summary>
        [NonSerialized] public float[][] Responses;
        /// <summary>Room correction, equalizer for each channel.</summary>
        [NonSerialized] public Equalizer[] Equalizers;

        /// <summary>Channel under measurement. If results are not available, but this equals the channel count, some tasks are not yet finished.</summary>
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
            SweepReference = Measurements.SweepFraming(Measurements.ExponentialSweep(FreqStart, FreqEnd, SweepLength, SampleRate));
            Sweep = AudioClip.Create("Sweep", SweepReference.Length, 1, SampleRate, false);
            Sweep.SetData(SweepReference, 0);
            Channel = 0;
            Responses = new float[AudioListener3D.ChannelCount][];
            Equalizers = new Equalizer[AudioListener3D.ChannelCount];
            Workers = new Task<float[]>[AudioListener3D.ChannelCount];
            OldLFESeparation = Listener.LFESeparation;
            OldVirtualizer = Listener.HeadphoneVirtualizer;
            Listener.LFESeparation = true;
            Listener.HeadphoneVirtualizer = false;
            MeasurementStart();
        }

        void MeasurementStart() {
            GameObject SweeperObj = new GameObject();
            SweeperObj.transform.position = CavernUtilities.VectorScale(AudioListener3D.Channels[Channel].SphericalPos, AudioListener3D.EnvironmentSize * 2);
            Sweeper = SweeperObj.AddComponent<AudioSource3D>();
            Sweeper.IsPlaying = true;
            Sweeper.Loop = false;
            Sweeper.Clip = Sweep;
            Sweeper.LFE = AudioListener3D.Channels[Channel].LFE;
            // TODO: ability to choose input device and adapt to its sample rate on perfect quality
            SweepResponse = Microphone.Start(string.Empty, false, Mathf.CeilToInt(SweepLength * 2 / Listener.SampleRate) + 1, Listener.SampleRate);
        }

        void MeasurementEnd() {
            Destroy(Sweeper.gameObject);
            Microphone.End(string.Empty);
            float[] Result = new float[SweepReference.Length];
            SweepResponse.GetData(Result, 0);
            Destroy(SweepResponse);
            (Workers[Channel] = new Task<float[]>(() => Measurements.GetFrequencyResponseAbs(SweepReference, Result))).Start();
        }

        /// <summary>Generate the required equalizer preset to flatten the frequency response of this channel.</summary>
        public void EqualizeChannel(int Channel) {
            Equalizers[Channel] = Equalizer.CorrectResponse(Responses[Channel], FreqStart, FreqEnd, Listener.SampleRate);
        }

        void Update() {
            if (ResultAvailable)
                return;
            if (Channel == AudioListener3D.ChannelCount) {
                for (int i = 0, c = Workers.Length; i < c; ++i)
                    if (Workers[i].Result == null)
                        return;
                for (int i = 0, c = Workers.Length; i < c; ++i)
                    Responses[i] = Workers[i].Result;
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
            Listener.LFESeparation = OldLFESeparation;
            Listener.HeadphoneVirtualizer = OldVirtualizer;
        }
    }
}