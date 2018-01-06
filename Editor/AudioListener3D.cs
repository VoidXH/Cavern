using UnityEditor;
using UnityEngine;

namespace Cavern {
    [CustomEditor(typeof(AudioListener3D))]
    public class AudioListener3DInspector : Editor {
        float LastDelay = 0;

        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            if (Application.isPlaying) {
                AudioListener3D Instance = AudioListener3D.Current;
                GUILayout.Space(16);
                EditorGUILayout.LabelField("Performance stats", EditorStyles.boldLabel);
                GUILayout.Label("Buffer position: " + AudioListener3D.FilterBufferPosition);
                GUILayout.Label("Delay: " +
                    (LastDelay = Mathf.LerpUnclamped(LastDelay, (float)AudioListener3D.FilterBufferPosition /
                    (AudioListener3D.Channels.Length * Instance.SampleRate), 20 * Time.deltaTime)) + " s");
                GUILayout.Label("Required FPS: " + (AudioListener3D.FilterBufferPosition != 0 ? 2 / LastDelay : 1).ToString("0"));
            }
        }

        public override bool RequiresConstantRepaint() {
            return true;
        }
    }
}