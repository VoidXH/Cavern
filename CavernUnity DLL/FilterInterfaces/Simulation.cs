using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Filters;

namespace Cavern.FilterInterfaces {
    /// <summary>Simulates a microphone recording at the active <see cref="AudioListener3D"/>.
    /// Requires a <see cref="Collider"/> on the listener.</summary>
    [AddComponentMenu("Audio/Filters/Advanced/Path simulation")]
    [RequireComponent(typeof(AudioSource3D))]
    public class Simulation : Raytraced {
        /// <summary>Speed of sound in units/s.</summary>
        [Header("Simulation")]
        [Tooltip("Speed of sound in units/s.")]
        public float SpeedOfSound = Source.SpeedOfSound;
        /// <summary>The impulse response of the simulated material. A single channel is required with the system sample rate.</summary>
        [Tooltip("The impulse response of the simulated material. A single channel is required with the system sample rate.")]
        public AudioClip Characteristics;
        /// <summary>Maximal echo travel time in samples, size of the convolution filter.</summary>
        [Tooltip("Maximal echo travel time in samples, size of the convolution filter.")]
        public int MaxSamples = 1024;

        /// <summary>Last generated FIR filter of the echo.</summary>
        public float[] Impulse { get; private set; }

        /// <summary>The attached audio source.</summary>
        AudioSource3D source;
        /// <summary>The collider that collects rays to be considered for the simulation.</summary>
        new Collider collider;
        /// <summary>Convolution filter to process the echo.</summary>
        Convolver filter;
        /// <summary>Extracted samples from <see cref="Characteristics"/>.</summary>
        float[] character;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            if (!(collider = AudioListener3D.Current.GetComponent<Collider>()))
                UnityEngine.Debug.LogError("No Collider is attached to the Listener.");
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnEnable() {
            source = GetComponent<AudioSource3D>();
            filter = new Convolver(new float[MaxSamples], 0);
            source.AddFilter(filter);
            if (Characteristics) {
                character = new float[Characteristics.samples];
                Characteristics.GetData(character, 0);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() => source.RemoveFilter(filter);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            if (!source.clip)
                return;
            float maxDist = AudioListener3D.Current.Range,
                step = 360f / Detail;
            List<Vector3> hits = new List<Vector3>();
            float[] impulse = Impulse = new float[MaxSamples];
            Vector3 direction = Vector3.zero,
                listenerPos = AudioListener3D.Current.transform.position;

            for (int horizontal = 0; horizontal < Detail; ++horizontal) {
                for (int vertical = 0; vertical < Detail; ++vertical) {
                    Vector3 lastDir = Quaternion.Euler(direction) * Vector3.forward;
                    hits.Clear();
                    for (int hitCount = 0; hitCount < Bounces; ++hitCount) {
                        if (Physics.Raycast(transform.position, lastDir, out RaycastHit hit, maxDist, Layers.value)) {
                            if (hit.collider == collider) { // Found a path from the source to the listener
                                float distance;
                                if (hits.Count != 0) {
                                    distance = Vector3.Distance(transform.position, hits[0]);
                                    int lastHit = hits.Count - 1;
                                    for (int i = 0; i < lastHit; ++i)
                                        distance += Vector3.Distance(hits[i], hits[i + 1]);
                                    distance += Vector3.Distance(hits[lastHit], listenerPos);
                                } else
                                    distance = Vector3.Distance(transform.position, listenerPos);
                                float timeOffset = distance / SpeedOfSound * source.clip.frequency;
                                if (timeOffset < MaxSamples - 1) {
                                    float gain = 1f / timeOffset;
                                    if (gain > 1)
                                        gain = 1;
                                    impulse[(int)timeOffset] += gain;
                                }
                                break;
                            }
                            lastDir = Vector3.Reflect(lastDir, hit.normal);
                            hits.Add(hit.point);
                        } else
                            break;
                    }
                    direction.y += step;
                }
                direction.x += step;
            }

            if (character != null)
                new Convolver(character, 0).Process(impulse);
            filter.Impulse = impulse;
        }
    }
}