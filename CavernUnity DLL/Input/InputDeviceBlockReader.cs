using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Input {
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
        /// Recording has successfully started.
        /// </summary>
        public bool Active => buffer != null;

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
        /// The value of <see cref="deviceName"/> when the last device was activated. If its value has changed, the device
        /// should be changed.
        /// </summary>
        string activeDevice;

        /// <summary>
        /// Returns the most recent samples recorded, whatever the inner state of recording is.
        /// </summary>
        public float[] ForceRead() {
            int pos = MultiplatformMicrophone.GetPosition(deviceName);
            if (buffer != null) {
                buffer.GetData(frame, pos > blockSize ? pos - blockSize : (sampleRate + pos - blockSize));
            } else {
                Array.Clear(frame, 0, frame.Length);
            }
            return frame;
        }

        void OnEnable() {
            if (frame == null || frame.Length != blockSize) {
                frame = new float[blockSize];
            }

            activeDevice = deviceName;
            if (Application.platform != RuntimePlatform.Android) {
                MultiplatformMicrophone.GetDeviceCaps(deviceName, out int minFreq, out int maxFreq);
                sampleRate = Math.Clamp(sampleRate, minFreq, maxFreq);
                buffer = MultiplatformMicrophone.Start(deviceName, true, 1, sampleRate);
            } else { // Fix for a Unity bug that's a feature: on Android, GetDeviceCaps always reports 16 kHz
                buffer = MultiplatformMicrophone.Start(deviceName, true, 1, sampleRate);
                if (buffer == null) {
                    int[] androidSampleRates = new[] { 8000, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 192000 };
                    int index = Array.BinarySearch(androidSampleRates, sampleRate);
                    if (index < 0) {
                        index = ~index;
                    }
                    for (int i = index; i < androidSampleRates.Length; i++) {
                        if (buffer = MultiplatformMicrophone.Start(deviceName, true, 1, sampleRate = androidSampleRates[i])) {
                            return;
                        }
                    }
                    for (int i = 0; i < index; i++) {
                        if (buffer = MultiplatformMicrophone.Start(deviceName, true, 1, sampleRate = androidSampleRates[i])) {
                            return;
                        }
                    }
                }
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity lifecycle")]
        void Update() {
            if (!deviceName.Equals(activeDevice)) {
                OnDisable();
                OnEnable();
            }

            if (buffer == null) {
                OnEnable(); // In case the microphone couldn't be started, try again continuously.
            }
            if (buffer == null) {
                return;
            }

            int pos = MultiplatformMicrophone.GetPosition(deviceName);
            if (lastPosition > pos) {
                lastPosition -= sampleRate;
            }
            while (lastPosition + blockSize < pos) {
                buffer.GetData(frame, lastPosition < 0 ? lastPosition + buffer.samples : lastPosition);
                Callback?.Invoke(frame);
                lastPosition += blockSize;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Unity lifecycle")]
        void OnDisable() {
            if (MultiplatformMicrophone.IsRecording(deviceName)) {
                MultiplatformMicrophone.End(deviceName);
                Destroy(buffer);
            }
        }
    }
}