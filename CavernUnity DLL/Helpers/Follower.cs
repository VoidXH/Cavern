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
        /// Access to material properties.
        /// </summary>
        new Renderer renderer;

        /// <summary>
        /// Create a follower for a target source.
        /// </summary>
        /// <returns>The follower component on a created sphere</returns>
        public static Follower CreateFollower(Source target, bool attach) {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Follower follower = obj.AddComponent<Follower>();
            follower.target = target;
            follower.attach = attach;
            follower.renderer = obj.GetComponent<Renderer>();
            return follower;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() => OnEnable();

        void OnEnable() {
            if (attach)
                AudioListener3D.cavernListener.AttachSource(target);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() {
            if (attach)
                AudioListener3D.cavernListener.DetachSource(target);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (target != null) {
                Transform listenerTransform = AudioListener3D.Current.transform;
                transform.position = listenerTransform.position + listenerTransform.rotation * target.Position.VectorMatch();
                target.Mute = mute;
                Color newColor = mute ? Color.red : Color.white;
                if (renderer.material.color != newColor)
                renderer.material.color = newColor;
            }
        }
    }
}