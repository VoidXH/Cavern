using System;
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
        /// <summary>The response of each channel to the sweep signal.</summary>
        float[][] Results;

        /// <summary>Length of the measurement signal. Must be a power of 2.</summary>
        public int SweepLength = 32768;

        /// <summary>The measurement is done and all results are available.</summary>
        [NonSerialized] public bool ResultAvailable;
        /// <summary>Frequency responses of output channels.</summary>
        [NonSerialized] public float[][] Responses;
        /// <summary>Room correction, equalizer for each channel.</summary>
        [NonSerialized] public Equalizer[] Equalizers;

        /// <summary>Channel under measurement.</summary>
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
            Results = new float[AudioListener3D.ChannelCount][];
            Responses = new float[AudioListener3D.ChannelCount][];
            Equalizers = new Equalizer[AudioListener3D.ChannelCount];
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
            SweepResponse.GetData(Results[Channel] = new float[SweepReference.Length], 0);
            Destroy(SweepResponse);
        }

        /// <summary>Calculate the frequency response of this channel and create an equalizer preset to correct it.</summary>
        public void FinalizeChannel(int Channel) {
            Responses[Channel] = Measurements.GetFrequencyResponseAbs(SweepReference, Results[Channel]);
            EqualizeChannel(Channel);
        }

        /// <summary>Generate the required equalizer preset to flatten the frequency response of this channel.</summary>
        public void EqualizeChannel(int Channel) {
            Equalizers[Channel] = Equalizer.CorrectResponse(Responses[Channel], FreqStart, FreqEnd, Listener.SampleRate);
        }

        void Update() {
            if (ResultAvailable)
                return;
            if (!Sweeper.IsPlaying) {
                MeasurementEnd();
                if (++Channel != AudioListener3D.ChannelCount)
                    MeasurementStart();
                else
                    ResultAvailable = true;
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