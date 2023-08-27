using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>
    /// Automatically replaces Unity Audio with Cavern on the fly.
    /// </summary>
    [AddComponentMenu("Audio/Spoofer/Auto Audio Engine Spoofer")]
    public sealed class AutoSpoofer : MonoBehaviour {
        /// <summary>
        /// Use Unity's audio engine for clips that are not transferrable to Cavern (anything that is not decompressed on load).
        /// </summary>
        [Tooltip("Use Unity's audio engine for clips that are not transferrable to Cavern (anything that is not decompressed on load).")]
        public bool Duality = true;

        static AutoSpoofer instance;

        AudioListener listenerInstance;

        readonly List<AudioSource> sources = new List<AudioSource>();

        /// <summary>
        /// Create an <see cref="AutoSpoofer"/> through the application if it doesn't exist.
        /// </summary>
        public static void CreateSpoofer() => CreateSpoofer(false);

        /// <summary>
        /// Create an <see cref="AutoSpoofer"/> through the application if it doesn't exist.
        /// </summary>
        /// <param name="debug">Display <see cref="Debug.Levels"/> in the application.</param>
        public static void CreateSpoofer(bool debug) {
            if (!instance) {
                DontDestroyOnLoad(new GameObject("Auto Audio Engine Spoofer").AddComponent<AutoSpoofer>());
            }
            Debug.Levels LevelsWindow = instance.GetComponent<Debug.Levels>();
            if (debug && !LevelsWindow) {
                instance.gameObject.AddComponent<Debug.Levels>();
            } else if (!debug && LevelsWindow) {
                Destroy(LevelsWindow);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Awake() => instance = this;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!listenerInstance && (listenerInstance = FindObjectOfType<AudioListener>())) {
                AudioListenerSpoofer spoofer = listenerInstance.gameObject.AddComponent<AudioListenerSpoofer>();
                spoofer.Source = listenerInstance;
                spoofer.duality = Duality;
            }
            sources.RemoveAll(x => !x);
            AudioSource[] toAdd = FindObjectsOfType<AudioSource>();
            for (int source = 0, end = toAdd.Length; source < end; ++source) {
                if (!sources.Contains(toAdd[source])) {
                    sources.Add(toAdd[source]);
                    AudioSourceSpoofer spoofer = toAdd[source].gameObject.AddComponent<AudioSourceSpoofer>();
                    spoofer.Source = toAdd[source];
                    spoofer.duality = Duality;
                }
            }
        }
    }
}