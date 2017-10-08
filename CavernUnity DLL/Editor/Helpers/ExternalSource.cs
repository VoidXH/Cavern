using UnityEditor;
using UnityEngine;

namespace Cavern.Helpers {
    [CustomEditor(typeof(ExternalSource))]
    public class ExternalSourceInspector : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            if (Application.isPlaying)
                GUILayout.Label("Latency: " + ((ExternalSource)target).Latency);
        }

        public override bool RequiresConstantRepaint() {
            return true;
        }
    }
}