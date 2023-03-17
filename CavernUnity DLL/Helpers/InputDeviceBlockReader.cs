using Cavern.Format;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Helpers {
    /// <summary>
    /// Reads an audio input device and delivers blocks of a given <see cref="blockSize"/> of audio samples in a callback
    /// when new samples are available.
    /// </summary>
    [AddComponentMenu("Audio/Helpers/Input Device Block-by-block Reader")]
    public class InputDeviceBlockReader : MonoBehaviour {
        /// <summary>
        /// Passes a block of audio samples.
        /// </summary>
        public delegate void AudioBlockDelegate(float[] samples);

        /// <summary>
        /// Called when a new block of samples have arrived.
        /// </summary>
        public event AudioBlockDelegate Callback;

        /// <summary>
        /// Target device sample rate. Will be overridden if the device doesn't support it.
        /// Only updated when the component is enabled.
        /// </summary>
        [Tooltip("Target device sample rate. Will be overridden if the device doesn't support it." +
            " Only updated when the component is enabled.")]
        public int sampleRate = 48000;

        /// <summary>
        /// Name of the target device or empty string for the default device.
        /// </summary>
        [Tooltip("Name of the target device or empty string for the default device.")]
        public string deviceName = string.Empty;

        /// <summary>
        /// Amount of audio samples to be delivered per <see cref="Callback"/>.
        /// </summary>
        [Tooltip("Amount of audio samples to be delivered per callback.")]
        public int blockSize = 16384;

        /// <summary>
        /// Clip to record to from the device.
        /// </summary>
        AudioClip buffer;

        /// <summary>
        /// The last block to be processed.
        /// </summary>
        float[] frame;

        /// <summary>
        /// The position in the <see cref="buffer"/> until it's processed.
        /// </summary>
        int lastPosition;

        /// <summary>
        /// Returns the most recent samples recorded, whatever the inner state of recording is.
        /// </summary>
        public float[] ForceRead() {
            int pos = Microphone.GetPosition(deviceName);
            buffer.GetData(frame, pos > blockSize ? pos - blockSize : (sampleRate + pos - blockSize));
            return frame;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity lifecycle")]
        void OnEnable() {
            if (Application.platform != RuntimePlatform.Android) {
                Microphone.GetDeviceCaps(deviceName, out int minFreq, out int maxFreq);
                sampleRate = Math.Clamp(sampleRate, minFreq, maxFreq);
                buffer = Microphone.Start(deviceName, true, 1, sampleRate);
            } else { // Fix for a Unity bug that's a feature: on Android, GetDeviceCaps always reports 16 kHz
                buffer = Microphone.Start(deviceName, true, 1, sampleRate);
                if (buffer == null) {
                    int[] androidSampleRates = new[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 192000 };
                    int index = Array.BinarySearch(androidSampleRates, sampleRate);
                    if (index < 0) {
                        index = ~index;
                    }
                    for (int i = index; i < androidSampleRates.Length; i++) {
                        if (buffer = Microphone.Start(deviceName, true, 1, sampleRate = androidSampleRates[i])) {
                            return;
                        }
                    }
                    for (int i = 0; i < index; i++) {
                        if (buffer = Microphone.Start(deviceName, true, 1, sampleRate = androidSampleRates[i])) {
                            return;
                        }
                    }
                }
            }
            frame = new float[blockSize];
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity lifecycle")]
        void Update() {
            int pos = Microphone.GetPosition(deviceName);
            if (lastPosition > pos) {
                lastPosition -= buffer.samples;
            }
            while (lastPosition + blockSize < pos) {
                buffer.GetData(frame, lastPosition < 0 ? lastPosition + buffer.samples : lastPosition);
                Callback?.Invoke(frame);
                lastPosition += blockSize;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity lifecycle")]
        void OnDisable() {
            if (Microphone.IsRecording(deviceName)) {
                Microphone.End(deviceName);
                Destroy(buffer);
            }
        }
    }
}