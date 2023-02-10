using UnityEditor;
using UnityEngine;

namespace Cavern.Helpers {
    [CustomEditor(typeof(ExternalSource))]
    public class ExternalSourceInspector : Editor {
        float lastDelay, lastLatency;

        public override void OnInspectorGUI() {
            float lerpValue = 20 * Time.deltaTime;

            DrawDefaultInspector();
            if (Application.isPlaying) {
                GUILayout.Space(16);
                GUILayout.Label("Listener delay: " +
                    (lastDelay = Mathf.LerpUnclamped(lastDelay, (float)AudioListener3D.FilterBufferPosition /
                    (AudioListener3D.Channels.Length * AudioListener3D.Current.SampleRate), lerpValue)) + " s");
                GUILayout.Label("Input latency: " +
                    (lastLatency = Mathf.LerpUnclamped(lastLatency, ((ExternalSource)target).Latency, lerpValue)) + " s");
                GUILayout.Label("Actual latency: " + (lastDelay + lastLatency) + " s");
            }
        }

        public override bool RequiresConstantRepaint() => true;
    }
}