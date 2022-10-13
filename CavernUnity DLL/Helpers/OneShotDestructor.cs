using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Helpers {
    /// <summary>
    /// Destructs a one-shot <see cref="AudioSource3D"/> after it finishes playback.
    /// </summary>
    [AddComponentMenu("Audio/Helpers/Internal/One Shot Destructor")]
    internal class OneShotDestructor : MonoBehaviour {
        /// <summary>
        /// Source to destruct.
        /// </summary>
        AudioSource3D source;

        /// <summary>
        /// Destroy the parent GameObject after playback.
        /// </summary>
        bool destroyGameObject;

        /// <summary>
        /// Constructs a new destructor.
        /// </summary>
        /// <param name="attachTo">Object containing a source to destruct</param>
        /// <param name="targetSource">Source to destruct</param>
        /// <param name="destroyAfter">Destroy the parent after playback</param>
        internal static OneShotDestructor Constructor(GameObject attachTo, AudioSource3D targetSource, bool destroyAfter) {
            OneShotDestructor destructor = attachTo.AddComponent<OneShotDestructor>();
            destructor.source = targetSource;
            destructor.destroyGameObject = destroyAfter;
            return destructor;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!source.IsPlaying) {
                Destroy(source);
                Destroy(this);
                if (destroyGameObject) {
                    Destroy(gameObject);
                }
            }
        }
    }
}