using UnityEngine;

using Cavern.Filters;

namespace Cavern {
    /// <summary>Creates a spatial echo effect by bouncing sound on surfaces.</summary>
    [AddComponentMenu("Audio/3D Echo")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Echo3D : MonoBehaviour {
        /// <summary>Speed of sound in units/s.</summary>
        [Tooltip("Speed of sound in units/s.")]
        public float SpeedOfSound = 3400;
        /// <summary>Number of directions to check.</summary>
        [Tooltip("Number of directions to check.")]
        public int Detail = 5;
        /// <summary>Maximum surface bounces.</summary>
        [Tooltip("Maximum surface bounces.")]
        public int Bounces = 3;
        /// <summary>Bounce dampening multiplier.</summary>
        [Tooltip("Bounce dampening multiplier.")]
        public float DampeningFactor = 1;
        /// <summary>Maximal echo travel time in samples, size of the convolution filter.</summary>
        [Tooltip("Maximal echo travel time in samples, size of the convolution filter.")]
        public int MaxSamples = 64;
        /// <summary>Layers to bounce the sound off from.</summary>
        [Tooltip("Layers to bounce the sound off from.")]
        public LayerMask Layers = int.MaxValue;

        /// <summary>THe attached audio source.</summary>
        AudioSource3D Source;
        /// <summary>Convolution filter to process the echo.</summary>
        Convolver Filter;

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

        void Start() {
            Source = GetComponent<AudioSource3D>();
            Filter = new Convolver(new float[MaxSamples], 0);
            Source.AddFilter(Filter);
        }

        void OnDestroy() => Source.RemoveFilter(Filter);

        void Update() {
            if (!Source.clip)
                return;
            float MaxDist = AudioListener3D.Current.Range, Step = 360f / Detail, ColorStep = 1f / Bounces;
            float[] Impulse = new float[MaxSamples];
            Impulse[0] = 1;
            Vector3 Direction = Vector3.zero;
            for (int Horizontal = 0; Horizontal < Detail; ++Horizontal) {
                for (int Vertical = 0; Vertical < Detail; ++Vertical) {
                    Vector3 LastPos = transform.position;
                    Vector3 LastDir = Quaternion.Euler(Direction) * Vector3.forward;
                    for (int HitCount = 0; HitCount < Bounces; ++HitCount) {
                        if (Physics.Raycast(LastPos, LastDir, out RaycastHit hit, MaxDist, Layers.value)) {
                            LastPos = hit.point;
                            LastDir = Vector3.Reflect(LastDir, hit.normal);
                            float Distance = Vector3.Distance(transform.position, hit.point), Volume = 1f / (Distance * DampeningFactor),
                                TimeOffset = Distance / SpeedOfSound * Source.clip.frequency;
                            if (TimeOffset < MaxSamples - 1) {
                                float PostMix = TimeOffset % 1, PreMix = 1 - PostMix;
                                Impulse[(int)TimeOffset] += PreMix * Volume;
                                Impulse[(int)TimeOffset + 1] -= PostMix * Volume;
                            }
                        } else
                            break;
                    }
                    Direction.y += Step;
                }
                Direction.x += Step;
            }
            Filter.Impulse = Impulse;
        }
    }
}