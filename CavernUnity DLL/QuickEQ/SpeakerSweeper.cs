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
        /// <summary>The sweep in the middle of the measurement was run.</summary>
        bool SweepStarted;
        /// <summary>Measurement signal samples.</summary>
        float[] SweepReference;
        /// <summary>Time passed while measuring the current channel.</summary>
        float ChannelMeasTime;
        /// <summary>Time required to measure a single channel. This includes the <see cref="SweepLength"/>
        /// and the half second in and out delays for all delay uncertainty.</summary>
        float ChannelTime;
        /// <summary>Channel under measurement.</summary>
        int Channel;

        /// <summary>Length of the measurement signal. Must be a power of 2.</summary>
        public int SweepLength = 8192;

        /// <summary>The measurement is done and all results are available.</summary>
        [NonSerialized] public bool ResultAvailable = false;
        /// <summary>Frequency responses of output channels.</summary>
        [NonSerialized] public float[][] Responses;
        /// <summary>Room correction, equalizer for each channel.</summary>
        [NonSerialized] public Equalizer[] Equalizers;

        void OnEnable() {
            Listener = AudioListener3D.Current;
            int SampleRate = Listener.SampleRate;
            Sweep = AudioClip.Create("Sweep", SweepLength, 1, SampleRate, false);
            SweepReference = Measurements.ExponentialSweep(FreqStart, FreqEnd, SweepLength, SampleRate);
            Sweep.SetData(SweepReference, 0);
            Channel = -1;
            ChannelTime = ChannelMeasTime = 1f + Sweep.length;
            Responses = new float[AudioListener3D.ChannelCount][];
            Equalizers = new Equalizer[AudioListener3D.ChannelCount];
            OldLFESeparation = Listener.LFESeparation;
            OldVirtualizer = Listener.HeadphoneVirtualizer;
            Listener.LFESeparation = true;
            Listener.HeadphoneVirtualizer = false;
        }

        void MeasurementStart() {
            GameObject SweeperObj = new GameObject();
            SweeperObj.transform.position = CavernUtilities.VectorScale(AudioListener3D.Channels[Channel].SphericalPos, AudioListener3D.EnvironmentSize * 2);
            Sweeper = SweeperObj.AddComponent<AudioSource3D>();
            Sweeper.IsPlaying = Sweeper.Loop = false;
            Sweeper.Clip = Sweep;
            Sweeper.LFE = AudioListener3D.Channels[Channel].LFE;
            SweepStarted = false;
            // TODO: ability to choose input device and adapt to its sample rate on perfect quality
            SweepResponse = Microphone.Start(string.Empty, false, 2, Listener.SampleRate);
        }

        void MeasurementEnd() {
            if (Channel != -1) {
                Destroy(Sweeper.gameObject);
                Microphone.End(string.Empty);
                float[] Result = new float[SweepResponse.samples];
                SweepResponse.GetData(Result, 0);
                Responses[Channel] = new float[SweepReference.Length];
                // TODO: copy the relevant part from the result to the response array
                Equalizers[Channel] = Equalizer.CorrectResponse(SweepReference, Responses[Channel], FreqStart, FreqEnd, Listener.SampleRate);
            }
        }

        void Update() {
            if (ResultAvailable)
                return;
            ChannelMeasTime += Time.deltaTime;
            if (ChannelMeasTime >= ChannelTime) {
                MeasurementEnd();
                if (++Channel == AudioListener3D.ChannelCount) {
                    ResultAvailable = true;
                    return;
                }
                MeasurementStart();
                ChannelMeasTime = 0;
            }
            if (!SweepStarted && ChannelMeasTime >= .5f) { // Start measurement at 0.5 seconds into microphone data collection - as no delay is known
                SweepStarted = true;
                Sweeper.IsPlaying = true;
            }
        }

        void OnDisable() {
            Destroy(Sweep);
            if (SweepResponse)
                Destroy(SweepResponse);
            Listener.LFESeparation = OldLFESeparation;
            Listener.HeadphoneVirtualizer = OldVirtualizer;
        }
    }
}