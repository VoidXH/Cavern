using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using UnityEngine;

using Cavern.Common;

namespace Cavern.Input {
    // TODO: Working WebGL support
    /// <summary>
    /// A box for Unity's <see cref="Microphone"/> class that also works for WebGL,
    /// which wouldn't even compile if <see cref="Microphone"/> functions were used.
    /// </summary>
    public static class MultiplatformMicrophone {
        /// <summary>
        /// Names of connected audio input devices.
        /// </summary>
        /// <remarks>Since JS gets the microphones asynchronously, this might have to be polled.</remarks>
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "API matching")]
        public static string[] devices {
            get {
                if (Application.platform == RuntimePlatform.WebGLPlayer) {
                    if (!JSlibAvailable) {
                        throw new JSlibNotFoundException("microphone.jslib");
                    }

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
                } else {
                    return Microphone.devices;
                }
            }
        }

        /// <summary>
        /// Get the sample rate limits of a device.
        /// </summary>
        public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq) {
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                minFreq = 48000;
                maxFreq = 48000;
            } else {
                Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
            }
        }

        /// <summary>
        /// Connect to an input device and start recording.
        /// </summary>
        /// <returns>Returns null if the device is unavailable</returns>
        public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency) {
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                return null;
            } else {
                return Microphone.Start(deviceName, loop, lengthSec, frequency);
            }
        }

        /// <summary>
        /// Returns the index of the last read sample of the input device in its <see cref="AudioClip"/>.
        /// </summary>
        public static int GetPosition(string deviceName) {
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                return (int)(Time.time % 1 * 48000);
            } else {
                return Microphone.GetPosition(deviceName);
            }
        }

        /// <summary>
        /// Returns if the device has successfully started recording.
        /// </summary>
        public static bool IsRecording(string deviceName) {
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                return true;
            } else {
                return Microphone.IsRecording(deviceName);
            }
        }

        /// <summary>
        /// Close an input device.
        /// </summary>
        public static void End(string deviceName) {
            if (Application.platform == RuntimePlatform.WebGLPlayer) {
            } else {
                Microphone.End(deviceName);
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
        /// Zhrows an exception if the required microphone.jslib file wasn't compiled.
        /// </summary>
        [DllImport("__Internal")]
        static extern void getMicrophoneJslib();

        /// <summary>
        /// Get the list of available audio input devices, separated by a single \n.
        /// </summary>
        [DllImport("__Internal")]
        static extern IntPtr getDevices();

        /// <summary>
        /// Frees some allocated memory.
        /// </summary>
        [DllImport("__Internal")]
        static extern void dispose(IntPtr pointer);

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