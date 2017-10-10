#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Cavern.Helpers {
    [CustomEditor(typeof(ExternalSource))]
    public class ExternalSourceInspector : Editor {
        float LastDelay = 0, LastLatency = 0;

        public override void OnInspectorGUI() {
            float LerpValue = 20 * Time.deltaTime;

            DrawDefaultInspector();
            if (Application.isPlaying) {
                GUILayout.Space(16);
                GUILayout.Label("Listener delay: " +
                    (LastDelay = CavernUtilities.FastLerp(LastDelay, (float)AudioListener3D.BufferPosition /
                    (AudioListener3D.ChannelCount * AudioListener3D.Current.SampleRate), LerpValue)) + " s");
                GUILayout.Label("Input latency: " +
                    (LastLatency = CavernUtilities.FastLerp(LastLatency, ((ExternalSource)target).Latency, LerpValue)) + " s");
                GUILayout.Label("Actual latency: " + (LastDelay + LastLatency) + " s");
            }
        }

        public override bool RequiresConstantRepaint() {
            return true;
        }
    }
}
#endif