using Cavern.Utilities;
using UnityEngine;

namespace Cavern.SpecialSources {
    /// <summary>An <see cref="AudioSource3D"/> with an intro <see cref="Clip"/> and a looping part after.</summary>
    [AddComponentMenu("Audio/Special Sources/3D Infinite Source")]
    public class InfiniteSource3D : AudioSource3D {
        /// <summary>Clip to start playback with.</summary>
        [Header("Infinite source settings")]
        [Tooltip("Clip to start playback with.")]
        public AudioClip intro;
        /// <summary>Clip to start playback with in Cavern's format. Overrides <see cref="intro"/>.</summary>
        [Tooltip("Clip to start playback with in Cavern's format. Overrides Intro.")]
        public AudioClip3D intro3D;
        /// <summary>Clip to play continuously after.</summary>
        [Tooltip("Clip to play continuously after.")]
        public AudioClip loopClip;
        /// <summary>Clip to play continuously after in Cavern's format. Overrides <see cref="loopClip"/>.</summary>
        [Tooltip("Clip to play continuously after in Cavern's format. Overrides Loop Clip.")]
        public AudioClip3D loopClip3D;

        int introClipHash;
        int loopClipHash;

        void Awake() => cavernSource = new InfiniteSource();

        void Update() {
            InfiniteSource source = (InfiniteSource)cavernSource;
            Tunneler.TunnelClips(ref source.Intro, intro, intro3D, ref introClipHash);
            Tunneler.TunnelClips(ref source.LoopClip, loopClip, loopClip3D, ref loopClipHash);
            SourceUpdate();
        }
    }
}