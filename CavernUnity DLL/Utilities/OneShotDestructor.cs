using UnityEngine;

namespace Cavern.Utilities {
    /// <summary>Destructs a one-shot <see cref="AudioSource3D"/> after it finishes playback.</summary>
    internal class OneShotDestructor : MonoBehaviour {
        /// <summary>Source to destruct</summary>
        AudioSource3D Source;
        /// <summary>Destroy the parent GameObject after playback.</summary>
        bool DestroyGameObject;

        /// <summary>Constructs a new destructor.</summary>
        /// <param name="AttachTo">Object containing a source to destruct</param>
        /// <param name="TargetSource">Source to destruct</param>
        /// <param name="DestroyAfter">Destroy the parent after playback</param>
        internal static OneShotDestructor Constructor(GameObject AttachTo, AudioSource3D TargetSource, bool DestroyAfter) {
            OneShotDestructor Destructor = AttachTo.AddComponent<OneShotDestructor>();
            Destructor.Source = TargetSource;
            Destructor.DestroyGameObject = DestroyAfter;
            return Destructor;
        }

        void Update() {
            if (!Source.IsPlaying) {
                Destroy(Source);
                Destroy(this);
                if (DestroyGameObject)
                    Destroy(gameObject);
            }
        }
    }
}