using UnityEngine;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.FilterInterfaces {
    /// <summary>Creates a spatial echo effect by bouncing sound on surfaces.</summary>
    [AddComponentMenu("Audio/Filters/3D Echo")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Echo3D : MonoBehaviour {
        /// <summary>Speed of sound in units/s.</summary>
        [Tooltip("Speed of sound in units/s.")]
        public float SpeedOfSound = Source.SpeedOfSound * 10;
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

        /// <summary>The attached audio source.</summary>
        AudioSource3D source;
        /// <summary>Convolution filter to process the echo.</summary>
        Convolver filter;

        void OnDrawGizmosSelected() {
            float maxDist = AudioListener3D.Current ? AudioListener3D.Current.Range : float.PositiveInfinity;
            float step = 360f / Detail, ColorStep = 1f / Bounces, alphaStep = ColorStep * .25f;
            Vector3 direction = Vector3.zero;
            for (int horizontal = 0; horizontal < Detail; ++horizontal) {
                for (int vertical = 0; vertical < Detail; ++vertical) {
                    Vector3 lastPos = transform.position;
                    Vector3 lastDir = Quaternion.Euler(direction) * Vector3.forward;
                    Color lastColor = new Color(0, 1, 0, .5f);
                    for (int bounceCount = 0; bounceCount < Bounces; ++bounceCount) {
                        if (Physics.Raycast(lastPos, lastDir, out RaycastHit hit, maxDist, Layers.value)) {
                            Gizmos.color = lastColor;
                            Gizmos.DrawLine(lastPos, hit.point);
                            lastPos = hit.point;
                            lastDir = Vector3.Reflect(lastDir, hit.normal);
                            lastColor.r += ColorStep;
                            lastColor.b += ColorStep;
                            lastColor.a -= alphaStep;
                        } else {
                            Gizmos.color = new Color(1, 0, 0, lastColor.a);
                            Gizmos.DrawRay(lastPos, lastDir);
                            break;
                        }
                    }
                    direction.y += step;
                }
                direction.x += step;
            }
        }

        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            filter = new Convolver(new float[MaxSamples], 0);
            source.AddFilter(filter);
        }

        void OnDisable() => source.RemoveFilter(filter);

        void Update() {
            if (!source.clip)
                return;
            float maxDist = AudioListener3D.Current.Range, step = 360f / Detail, colorStep = 1f / Bounces;
            float[] impulse = new float[MaxSamples];
            impulse[0] = 1;
            Vector3 direction = Vector3.zero;
            for (int horizontal = 0; horizontal < Detail; ++horizontal) {
                for (int vertical = 0; vertical < Detail; ++vertical) {
                    Vector3 lastPos = transform.position;
                    Vector3 lastDir = Quaternion.Euler(direction) * Vector3.forward;
                    for (int hitCount = 0; hitCount < Bounces; ++hitCount) {
                        if (Physics.Raycast(lastPos, lastDir, out RaycastHit hit, maxDist, Layers.value)) {
                            lastPos = hit.point;
                            lastDir = Vector3.Reflect(lastDir, hit.normal);
                            float distance = Vector3.Distance(transform.position, hit.point), volume = 1f / (distance * DampeningFactor),
                                timeOffset = distance / SpeedOfSound * source.clip.frequency;
                            if (timeOffset < MaxSamples - 1) {
                                float postMix = timeOffset % 1, preMix = 1 - postMix;
                                impulse[(int)timeOffset] += preMix * volume;
                                impulse[(int)timeOffset + 1] -= postMix * volume;
                            }
                        } else
                            break;
                    }
                    direction.y += step;
                }
                direction.x += step;
            }
            filter.Impulse = impulse;
        }
    }
}