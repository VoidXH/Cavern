using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>
    /// Creates a spatial echo effect by bouncing sound on surfaces.
    /// </summary>
    [AddComponentMenu("Audio/Filters/3D Echo")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Echo3D : Raytraced {
        /// <summary>
        /// Speed of sound in units/s.
        /// </summary>
        [Header("Echo")]
        [Tooltip("Speed of sound in units/s.")]
        public float SpeedOfSound = Source.SpeedOfSound * 10;

        /// <summary>
        /// Bounce dampening multiplier.
        /// </summary>
        [Tooltip("Bounce dampening multiplier.")]
        public float DampeningFactor = 2;

        /// <summary>
        /// Maximal echo travel time in samples, size of the convolution filter.
        /// </summary>
        [Tooltip("Maximal echo travel time in samples, size of the convolution filter.")]
        public int MaxSamples = 64;

        /// <summary>
        /// Last generated FIR filter of the echo.
        /// </summary>
        public float[] Impulse { get; private set; }

        /// <summary>
        /// The attached audio source.
        /// </summary>
        AudioSource3D source;

        /// <summary>
        /// Convolution filter to process the echo.
        /// </summary>
        Convolver filter;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            filter = new Convolver(new float[MaxSamples], 0);
            source.AddFilter(filter);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => source.RemoveFilter(filter);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!source.clip) {
                return;
            }
            Vector3 listenerPos = AudioListener3D.Current.transform.position;
            float maxDist = AudioListener3D.Current.Range,
                sourceDist = Vector3.Distance(transform.position, listenerPos),
                step = 360f / Detail;
            float[] impulse = Impulse = new float[MaxSamples];
            impulse[0] = 1;
            Vector3 direction = Vector3.zero;
            for (int horizontal = 0; horizontal < Detail; ++horizontal) {
                for (int vertical = 0; vertical < Detail; ++vertical) {
                    Vector3 lastDir = Quaternion.Euler(direction) * Vector3.forward;
                    for (int hitCount = 0; hitCount < Bounces; ++hitCount) {
                        if (Physics.Raycast(transform.position, lastDir, out RaycastHit hit, maxDist, Layers.value)) {
                            lastDir = Vector3.Reflect(lastDir, hit.normal);
                            float distance = Vector3.Distance(transform.position, hit.point) +
                                             Vector3.Distance(hit.point, listenerPos) - sourceDist,
                                gain = 1f / (distance * DampeningFactor),
                                timeOffset = distance / SpeedOfSound * source.clip.frequency;
                            if (timeOffset < MaxSamples - 1) {
                                float postMix = timeOffset % 1;
                                impulse[(int)timeOffset] += (1 - postMix) * gain;
                                impulse[(int)timeOffset + 1] -= postMix * gain;
                            }
                        } else {
                            break;
                        }
                    }
                    direction.y += step;
                }
                direction.x += step;
            }
            filter.Impulse = impulse;
        }
    }
}