using UnityEngine;

namespace Cavern {
    /// <summary>Creates a spatial echo effect by bouncing sound on surfaces.</summary>
    [AddComponentMenu("Audio/3D Echo")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Echo3D : MonoBehaviour {
        /// <summary>Speed of sound in units/s.</summary>
        [Tooltip("Speed of sound in units/s.")]
        public float SpeedOfSound = 340;
        /// <summary>Number of directions to check.</summary>
        [Tooltip("Number of directions to check.")]
        public int Detail = 5;
        /// <summary>Maximum surface bounces.</summary>
        [Tooltip("Maximum surface bounces.")]
        public int Bounces = 3;
        /// <summary>Bounce dampening multiplier.</summary>
        [Tooltip("Bounce dampening multiplier.")]
        public float DampeningFactor = 50;
        /// <summary>Layers to bounce the sound off from.</summary>
        [Tooltip("Layers to bounce the sound off from.")]
        public LayerMask Layers = int.MaxValue;

        /// <summary>THe attached audio source.</summary>
        AudioSource3D Source;
        /// <summary>Generated audio reflections.</summary>
        AudioSource3D[] BouncePoints;
        /// <summary>Last value of <see cref="Detail"/>.</summary>
        int CachedDetail = 0;
        /// <summary>Last value of <see cref="Bounces"/>.</summary>
        int CachedBounces = 0;

        void OnDrawGizmosSelected() {
            float MaxDist = AudioListener3D.Current ? AudioListener3D.Current.Range : float.PositiveInfinity;
            float Step = 360f / Detail, ColorStep = 1f / Bounces, AlphaStep = ColorStep * .25f;
            Vector3 Direction = Vector3.zero;
            for (int Horizontal = 0; Horizontal < Detail; ++Horizontal) {
                for (int Vertical = 0; Vertical < Detail; ++Vertical) {
                    Vector3 LastPos = transform.position;
                    Vector3 LastDir = Quaternion.Euler(Direction) * Vector3.forward;
                    Color LastColor = new Color(0, 1, 0, .5f);
                    for (int BounceCount = 0; BounceCount < Bounces; ++BounceCount) {
                        if (Physics.Raycast(LastPos, LastDir, out RaycastHit hit, MaxDist, Layers.value)) {
                            Gizmos.color = LastColor;
                            Gizmos.DrawLine(LastPos, hit.point);
                            LastPos = hit.point;
                            LastDir = Vector3.Reflect(LastDir, hit.normal);
                            LastColor.r += ColorStep;
                            LastColor.b += ColorStep;
                            LastColor.a -= AlphaStep;
                        } else {
                            Gizmos.color = new Color(1, 0, 0, LastColor.a);
                            Gizmos.DrawRay(LastPos, LastDir);
                            break;
                        }
                    }
                    Direction.y += Step;
                }
                Direction.x += Step;
            }
        }

        void Start() => Source = GetComponent<AudioSource3D>();

        void Update() {
            int CurrentBounce = 0, BounceCount = Detail * Detail * Bounces;
            if (CachedDetail != Detail || CachedBounces != Bounces) {
                CachedDetail = Detail;
                CachedBounces = Bounces;
                BouncePoints = new AudioSource3D[BounceCount];
                for (int i = 0; i < BounceCount; ++i)
                    (BouncePoints[i] = new GameObject().AddComponent<AudioSource3D>()).Mute = true;
            }

            float MaxDist = AudioListener3D.Current.Range;
            float Step = 360f / Detail, ColorStep = 1f / Bounces, MaxError = SpeedOfSound / 1000f;
            Vector3 Direction = Vector3.zero;
            for (int Horizontal = 0; Horizontal < Detail; ++Horizontal) {
                for (int Vertical = 0; Vertical < Detail; ++Vertical) {
                    Vector3 LastPos = transform.position;
                    Vector3 LastDir = Quaternion.Euler(Direction) * Vector3.forward;
                    Color LastColor = new Color(0, 1, 0, .5f);
                    for (int HitCount = 0; HitCount < Bounces; ++HitCount) {
                        if (Physics.Raycast(LastPos, LastDir, out RaycastHit hit, MaxDist, Layers.value)) {
                            AudioSource3D Target = BouncePoints[CurrentBounce++];
                            if (Target.Mute)
                                Target.CopySettings(Source);
                            LastPos = hit.point;
                            LastDir = Vector3.Reflect(LastDir, hit.normal);
                            if ((Target.transform.position - LastPos).sqrMagnitude > MaxError) { // Correcting the clip position all the time causes artifacts
                                Target.transform.position = LastPos;
                                float Distance = Vector3.Distance(transform.position, hit.point);
                                if (DampeningFactor > Distance)
                                    Target.volume *= DampeningFactor / Distance;
                                Target.timeSamples += (int)(Distance / SpeedOfSound * Target.clip.frequency);
                            }
                        } else
                            break;
                    }
                    Direction.y += Step;
                }
                Direction.x += Step;
            }

            while (CurrentBounce < BounceCount) {
                BouncePoints[CurrentBounce].Mute = true;
                ++CurrentBounce;
            }
        }
    }
}