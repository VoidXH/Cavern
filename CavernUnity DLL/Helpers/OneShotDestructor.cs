using UnityEngine;

namespace Cavern.Helpers {
    /// <summary>Destructs a one-shot <see cref="AudioSource3D"/> after it finishes playback.</summary>
    internal class OneShotDestructor : MonoBehaviour {
        /// <summary>Source to destruct</summary>
        AudioSource3D Source;

        /// <summary>Constructs a new destructor.</summary>
        /// <param name="AttachTo">Object containing a source to destruct</param>
        /// <param name="TargetSource">Source to destruct</param>
        internal static OneShotDestructor Constructor(GameObject AttachTo, AudioSource3D TargetSource) {
            OneShotDestructor Destructor = AttachTo.AddComponent<OneShotDestructor>();
            Destructor.Source = TargetSource;
            return Destructor;
        }

        void Update() {
            if (!Source.IsPlaying) {
                Destroy(Source);
                Destroy(this);
            }
        }
    }
}