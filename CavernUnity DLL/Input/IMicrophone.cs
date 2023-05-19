using UnityEngine;

namespace Cavern.Input {
    /// <summary>
    /// Required functions to handle a microphone.
    /// </summary>
    public interface IMicrophone {
        /// <summary>
        /// Get the list of available audio input devices, separated by a single \n.
        /// </summary>
        string[] GetDevices();

        /// <summary>
        /// Get the sample rate limits of a device.
        /// </summary>
        void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq);

        /// <summary>
        /// Start recording by device name.
        /// </summary>
        AudioClip StartDevice(string deviceName, bool loop, int lengthSec, int frequency);

        /// <summary>
        /// Returns the index of the last read sample of the input device in its <see cref="AudioClip"/>.
        /// </summary>
        int GetPosition(string deviceName);

        /// <summary>
        /// Returns if the device has successfully started recording.
        /// </summary>
        bool IsRecording(string deviceName);

        /// <summary>
        /// Stop recording by device name.
        /// </summary>
        void EndDevice(string deviceName);
    }
}