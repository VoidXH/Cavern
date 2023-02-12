using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.SpecialSources {
    /// <summary>
    /// An <see cref="AudioSource3D"/> with an intro <see cref="Clip"/> and a looping part after.
    /// </summary>
    [AddComponentMenu("Audio/Special Sources/3D Infinite Source")]
    public class InfiniteSource3D : AudioSource3D {
        /// <summary>
        /// Clip to start playback with.
        /// </summary>
        [Header("Infinite source settings")]
        [Tooltip("Clip to start playback with.")]
        public AudioClip intro;

        /// <summary>
        /// Clip to start playback with in Cavern's format. Overrides <see cref="intro"/>.
        /// </summary>
        [Tooltip("Clip to start playback with in Cavern's format. Overrides Intro.")]
        public Clip intro3D;

        /// <summary>
        /// Clip to play continuously after.
        /// </summary>
        [Tooltip("Clip to play continuously after.")]
        public AudioClip loopClip;

        /// <summary>
        /// Clip to play continuously after in Cavern's format. Overrides <see cref="loopClip"/>.
        /// </summary>
        [Tooltip("Clip to play continuously after in Cavern's format. Overrides Loop Clip.")]
        public Clip loopClip3D;

        int introClipHash;

        int loopClipHash;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Awake() => cavernSource = new InfiniteSource();

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            InfiniteSource source = (InfiniteSource)cavernSource;
            Tunneler.TunnelClips(ref source.intro, intro, intro3D, ref introClipHash);
            Tunneler.TunnelClips(ref source.loopClip, loopClip, loopClip3D, ref loopClipHash);
            SourceUpdate();
        }
    }
}