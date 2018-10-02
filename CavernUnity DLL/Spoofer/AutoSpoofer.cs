using System.Collections.Generic;
using UnityEngine;

namespace Cavern.Spoofer {
    /// <summary>Automatically replaces Unity Audio with Cavern on the fly.</summary>
    [AddComponentMenu("Audio/Spoofer/Auto Audio Engine Spoofer")]
    public sealed class AutoSpoofer : MonoBehaviour {
        /// <summary>Use Unity's audio engine for clips that are not transferrable to Cavern (anything that is not decompressed on load).</summary>
        [Tooltip("Use Unity's audio engine for clips that are not transferrable to Cavern (anything that is not decompressed on load).")]
        public bool Duality = true;

        static AutoSpoofer Instance;

        AudioListener ListenerInstance;
        List<AudioSource> Sources = new List<AudioSource>();

        /// <summary>Create an <see cref="AutoSpoofer"/> through the application if it doesn't exist.</summary>
        /// <param name="Debug">Display <see cref="Debug.Levels"/> in the application.</param>
        public static void CreateSpoofer(bool Debug = false) {
            if (!Instance)
                DontDestroyOnLoad(new GameObject("Auto Audio Engine Spoofer").AddComponent<AutoSpoofer>());
            Debug.Levels LevelsWindow = Instance.GetComponent<Debug.Levels>();
            if (Debug && !LevelsWindow)
                Instance.gameObject.AddComponent<Debug.Levels>();
            else if (!Debug && LevelsWindow)
                Destroy(LevelsWindow);
        }

        void Awake() => Instance = this;

        void Update() {
            if (!ListenerInstance && (ListenerInstance = FindObjectOfType<AudioListener>())) {
                AudioListenerSpoofer Spoofer = ListenerInstance.gameObject.AddComponent<AudioListenerSpoofer>();
                Spoofer.Source = ListenerInstance;
                Spoofer.Duality = Duality;
            }
            Sources.RemoveAll(x => !x);
            AudioSource[] All = FindObjectsOfType<AudioSource>();
            for (int i = 0, Count = All.Length; i < Count; ++i) {
                if (!Sources.Contains(All[i])) {
                    Sources.Add(All[i]);
                    AudioSourceSpoofer Spoofer = All[i].gameObject.AddComponent<AudioSourceSpoofer>();
                    Spoofer.Source = All[i];
                    Spoofer.Duality = Duality;
                }
            }
        }
    }
}