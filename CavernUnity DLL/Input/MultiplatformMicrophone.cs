using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Input {
    /// <summary>
    /// A box for Unity's <see cref="Microphone"/> class that also works for WebGL,
    /// which wouldn't even compile if <see cref="Microphone"/> functions were used.
    /// </summary>
    public static class MultiplatformMicrophone {
        /// <summary>
        /// To use an unsupported platform's microphone, initialize an accessor class to this property and it will be used.
        /// </summary>
        public static IMicrophone Override { get; set; }

        /// <summary>
        /// Names of connected audio input devices.
        /// </summary>
        /// <remarks>Since JS gets the microphones asynchronously, this might have to be polled.</remarks>
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "API matching")]
        public static string[] devices => Override == null ? Microphone.devices : Override.GetDevices();

        /// <summary>
        /// Get the sample rate limits of a device.
        /// </summary>
        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq) {
            if (Override == null) {
                Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
            } else {
                Override.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
            }
        }

        /// <summary>
        /// Connect to an input device and start recording.
        /// </summary>
        /// <returns>Returns null if the device is unavailable</returns>
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency) => Override == null
            ? Microphone.Start(deviceName, loop, lengthSec, frequency)
            : Override.StartDevice(deviceName, loop, lengthSec, frequency);

        /// <summary>
        /// Returns the index of the last read sample of the input device in its <see cref="AudioClip"/>.
        /// </summary>
        public static int GetPosition(string deviceName) =>
            Override == null ? Microphone.GetPosition(deviceName) : Override.GetPosition(deviceName);

        /// <summary>
        /// Returns if the device has successfully started recording.
        /// </summary>
        public static bool IsRecording(string deviceName) =>
            Override == null ? Microphone.IsRecording(deviceName) : Override.IsRecording(deviceName);

        /// <summary>
        /// Close an input device.
        /// </summary>
        public static void End(string deviceName) {
            if (Override == null) {
                Microphone.End(deviceName);
            } else {
                Override.EndDevice(deviceName);
            }
        }
    }
}