using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

using Cavern.Common;

namespace Cavern.Input {
    /// <summary>
    /// Microphone accessor function references for the accompanying native library.
    /// </summary>
    public class JSMicrophone : IMicrophone {
        /// <summary>
        /// Holds the recording progress for each device by name, including the allocated <see cref="Task"/>, the samples recorded so far
        /// (which are overridden for looping recordings), and which was the last recorded sample.
        /// </summary>
        static readonly Dictionary<string, (Task thread, float[] samples, int index)> recorders =
            new Dictionary<string, (Task, float[], int)>();

        /// <summary>
        /// Get the list of available audio input devices, separated by a single \n.
        /// </summary>
        public string[] GetDevices() {
            Initialize();
            IntPtr read = getDevices();
            string[] result = Marshal.PtrToStringAuto(read).Split('\n');
            dispose(read);
            if (result[0].Length != 0) {
                if (result[0].Equals(permissionDeniedReturn)) {
                    throw new PermissionDeniedException();
                }
                if (result[0].Equals(errorReturn)) {
                    throw new JSlibException();
                }

                return result;
            }
            return new string[0];
        }

        /// <summary>
        /// Get the sample rate limits of a device.
        /// </summary>
        public void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq) {
            minFreq = 48000;
            maxFreq = 48000;
        }

        /// <summary>
        /// Start recording by device name.
        /// </summary>
        public AudioClip StartDevice(string deviceName, bool loop, int lengthSec, int frequency) {
            if (startDevice(deviceName)) {
                recorders[deviceName] = (null, null, 0);
                return null;
            }
            return null;
        }

        /// <summary>
        /// Returns the index of the last read sample of the input device in its <see cref="AudioClip"/>.
        /// </summary>
        public int GetPosition(string deviceName) => recorders.ContainsKey(deviceName) ? recorders[deviceName].index : 0;

        /// <summary>
        /// Returns if the device has successfully started recording.
        /// </summary>
        public bool IsRecording(string deviceName) => recorders.ContainsKey(deviceName);

        /// <summary>
        /// Stop recording by device name.
        /// </summary>
        public void EndDevice(string deviceName) {
            if (recorders.ContainsKey(deviceName)) {
                endDevice(deviceName);
                recorders.Remove(deviceName);
            }
        }

        /// <summary>
        /// Throws an exception if the required microphone.jslib file wasn't compiled.
        /// </summary>
        [DllImport("__Internal")]
        static extern void getMicrophoneJslib();

        /// <summary>
        /// Get the list of available audio input devices, separated by a single \n.
        /// </summary>
        [DllImport("__Internal")]
        static extern IntPtr getDevices();

        /// <summary>
        /// Start recording by device name.
        /// </summary>
        [DllImport("__Internal")]
        static extern bool startDevice(string deviceName);

        /// <summary>
        /// Stop recording by device name.
        /// </summary>
        [DllImport("__Internal")]
        static extern void endDevice(string deviceName);

        /// <summary>
        /// Frees some allocated memory.
        /// </summary>
        [DllImport("__Internal")]
        static extern void dispose(IntPtr pointer);

        /// <summary>
        /// Throws an exception if the required library wasn't compiled.
        /// </summary>
        void Initialize() {
            if (!JSlibAvailable) {
                throw new JSlibNotFoundException("microphone.jslib");
            }
        }

        /// <summary>
        /// Gets if the required microphone.jslib file was compiled.
        /// </summary>
        static bool JSlibAvailable {
            get {
                if (jslibAvailable.HasValue) {
                    return jslibAvailable.Value;
                }
                try {
                    getMicrophoneJslib();
                    return true;
                } catch {
                    jslibAvailable = false;
                    return false;
                }
            }
        }
        static bool? jslibAvailable;

        /// <summary>
        /// String returned from <see cref="getDevices"/> when the user rejected the microphone access.
        /// </summary>
        const string permissionDeniedReturn = "DENIED";

        /// <summary>
        /// String returned from <see cref="getDevices"/> in case of a general error.
        /// </summary>
        const string errorReturn = "ERROR";
    }
}