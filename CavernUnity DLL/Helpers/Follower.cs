using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Helpers {
    /// <summary>
    /// Creates a sphere that matches the position of a <see cref="Source"/>.
    /// This is useful for visualizing non-Unity-based Cavern features.
    /// </summary>
    [AddComponentMenu("Audio/Helpers/Follower")]
    public class Follower : MonoBehaviour {
        /// <summary>
        /// The followed source.
        /// </summary>
        [Tooltip("The followed source.")]
        public Source target;

        /// <summary>
        /// Attach the visualized source to the active listener.
        /// </summary>
        [Tooltip("Attach the visualized source to the active listener.")]
        public bool attach;

        /// <summary>
        /// Mute the source of the displayed object.
        /// </summary>
        [Tooltip("Mute the source of the displayed object.")]
        public bool mute;

        /// <summary>
        /// Sphere color for unmuted objects.
        /// </summary>
        [Header("Display")]
        [Tooltip("Sphere color for unmuted objects.")]
        public Color normalColor = Color.cyan;

        /// <summary>
        /// Sphere color for unmuted and screen locked objects.
        /// </summary>
        [Tooltip("Sphere color for unmuted and screen locked objects.")]
        public Color screenLockedColor = Color.magenta;

        /// <summary>
        /// Sphere color when the source is muted.
        /// </summary>
        [Tooltip("Sphere color when the source is muted.")]
        public Color muteColor = Color.red;

        /// <summary>
        /// Show the object's trail for this many seconds.
        /// </summary>
        public float trailTime;

        /// <summary>
        /// Access to material properties.
        /// </summary>
        new Renderer renderer;

        /// <summary>
        /// Displayed path of the object.
        /// </summary>
        TrailRenderer trail;

        /// <summary>
        /// Create a follower for a target source.
        /// </summary>
        /// <param name="target">Follow this <see cref="Source"/></param>
        /// <param name="attach">Attach the visualized source to the active listener</param>
        /// <returns>The follower component on a created sphere</returns>
        public static Follower CreateFollower(Source target, bool attach) {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Follower follower = obj.AddComponent<Follower>();
            follower.target = target;
            follower.attach = attach;
            follower.renderer = obj.GetComponent<Renderer>();
            MakeTransparent(follower.renderer.material);

            follower.trail = obj.AddComponent<TrailRenderer>();
            follower.trail.material = follower.renderer.material;
            follower.trail.endWidth = 0;
            return follower;
        }

        /// <summary>
        /// Applies the Transparent rendering mode on a material which has the Standard Shader.
        /// </summary>
        static void MakeTransparent(Material mat) {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() => OnEnable();

        void OnEnable() {
            if (attach) {
                AudioListener3D.cavernListener.AttachSource(target);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() {
            if (attach) {
                AudioListener3D.cavernListener.DetachSource(target);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (target != null && target.Rendered != null) {
                Transform listenerTransform = AudioListener3D.Current.transform;
                transform.position = listenerTransform.position + listenerTransform.rotation * target.Position.VectorMatch();

                Color newColor = normalColor;
                if (target.Mute = mute) {
                    newColor = muteColor;
                } else if (target.screenLocked) {
                    newColor = screenLockedColor;
                }

                newColor.a = .125f;
                float[] samples = target.Rendered[0];
                for (int i = 0; i < samples.Length; i++) {
                    if (samples[i] != 0) {
                        newColor.a = 1;
                    }
                }

                if (renderer.material.color != newColor) {
                    renderer.material.color = newColor;
                }

                if (trail.time != trailTime) {
                    trail.time = trailTime;
                }
            }
        }
    }
}