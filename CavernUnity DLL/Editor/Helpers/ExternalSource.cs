using UnityEditor;
using UnityEngine;

namespace Cavern.Helpers {
    [CustomEditor(typeof(ExternalSource))]
    public class ExternalSourceInspector : Editor {
        float LastLatency;

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            if (Application.isPlaying)
                GUILayout.Label("Latency: " + ((ExternalSource)target).Latency);
        }

        void OnSceneGUI() {
            float Latency = ((ExternalSource)target).Latency;
            if (LastLatency != Latency) {
                LastLatency = Latency;
                Repaint();
            }
        }
    }
}