using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.FilterInterfaces {
    /// <summary>Creates an IR simulation between this object and <see cref="SimulationTarget"/>s.</summary>
    [AddComponentMenu("Audio/Filters/Advanced/Path Simulation Source")]
    public class SimulationSource : Raytraced {
        /// <summary>Speed of sound in units/s.</summary>
        [Tooltip("Speed of sound in units/s.")]
        public float SpeedOfSound = Source.SpeedOfSound;
        /// <summary>Targets which absorb the emitted rays and generate their impulse response by.</summary>
        [Tooltip("Targets which absorb the emitted rays and generate their impulse response by.")]
        [Linked("colliders")]
        public SimulationTarget[] Targets;

        /// <summary>Colliders for the <see cref="Targets"/>.</summary>
        [Linked("Targets")]
        Collider[] colliders;

        /// <summary>Hits in the last ray's path.</summary>
        int hitCount;
        /// <summary>The hit positions of the last ray's path.</summary>
        Vector3[] hits;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            colliders = new Collider[Targets.Length];
            for (int target = 0; target < Targets.Length; ++target) {
                colliders[target] = Targets[target].GetComponent<Collider>();
                if (colliders[target] == null)
                    UnityEngine.Debug.LogError(Targets[target].name + " doesn't have a collider to be used for simulation.");
            }
        }

        void MixImpulse(SimulationTarget target) {
            if (!target.HasClip)
                return;
            float distance;
            if (hitCount != 0) {
                distance = Vector3.Distance(transform.position, hits[0]);
                int lastHit = hitCount - 1;
                for (int i = 0; i < lastHit; ++i)
                    distance += Vector3.Distance(hits[i], hits[i + 1]);
                distance += Vector3.Distance(hits[lastHit], AudioListener3D.Current.transform.position);
            } else
                distance = Vector3.Distance(transform.position, AudioListener3D.Current.transform.position);
            float timeOffset = distance / SpeedOfSound * target.SampleRate;
            if (timeOffset < target.MaxSamples - 1) {
                float gain = 1f / timeOffset;
                if (gain > 1)
                    gain = 1;
                target.Impulse[(int)timeOffset] += gain;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            float maxDist = AudioListener3D.Current.Range,
                step = 360f / Detail;
            Vector3 direction = Vector3.zero;

            hits = new Vector3[Bounces];
            for (int target = 0; target < Targets.Length; ++target)
                Targets[target].Prepare();

            for (int horizontal = 0; horizontal < Detail; ++horizontal) {
                for (int vertical = 0; vertical < Detail; ++vertical) {
                    Vector3 lastDir = Quaternion.Euler(direction) * Vector3.forward;
                    hitCount = 0;
                    for (int bounce = 0; bounce < Bounces; ++bounce) {
                        if (Physics.Raycast(transform.position, lastDir, out RaycastHit hit, maxDist, Layers.value)) {
                            for (int i = 0; i < colliders.Length; ++i) {
                                if (colliders[i] == hit.collider) { // Found a path to one collider
                                    MixImpulse(Targets[i]);
                                    break;
                                }
                            }
                            lastDir = Vector3.Reflect(lastDir, hit.normal);
                            hits[hitCount++] = hit.point;
                        } else
                            break;
                    }
                    direction.y += step;
                }
                direction.x += step;
            }

            for (int target = 0; target < Targets.Length; ++target)
                Targets[target].UpdateFilter();
        }
    }
}