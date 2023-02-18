using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.FilterInterfaces {
    /// <summary>
    /// Creates an IR simulation between this object and <see cref="SimulationTarget"/>s.
    /// </summary>
    [AddComponentMenu("Audio/Filters/Advanced/Path Simulation Source")]
    public class SimulationSource : Raytraced {
        /// <summary>
        /// Speed of sound in units/s.
        /// </summary>
        [Tooltip("Speed of sound in units/s.")]
        public float SpeedOfSound = Source.SpeedOfSound;

        /// <summary>
        /// Change the phase of the sound wave on reflection.
        /// </summary>
        [Tooltip("Change the phase of the sound wave on reflection.")]
        public bool ChangePhase;

        /// <summary>
        /// Targets which absorb the emitted rays and generate their impulse response by.
        /// </summary>
        [Tooltip("Targets which absorb the emitted rays and generate their impulse response by.")]
        [Linked("colliders")]
        public SimulationTarget[] Targets = new SimulationTarget[0];

        /// <summary>
        /// Show all rays, even the ones that didn't hit.
        /// </summary>
        [Header("Debug")]
        [Tooltip("Show all rays, even the ones that didn't hit.")]
        public bool ShowAllRays;

        /// <summary>
        /// Colliders for the <see cref="Targets"/>.
        /// </summary>
        [Linked("Targets")]
        Collider[] colliders = new Collider[0];

        /// <summary>
        /// Hits in the last ray's path.
        /// </summary>
        int hitCount;

        /// <summary>
        /// The hit positions of the last ray's path.
        /// </summary>
        Vector3[] hits;

        void ResetColliders() {
            colliders = new Collider[Targets.Length];
            for (int target = 0; target < Targets.Length; target++) {
                colliders[target] = Targets[target].GetComponent<Collider>();
                if (colliders[target] == null) {
                    UnityEngine.Debug.LogError(Targets[target].name + " doesn't have a collider to be used for simulation.");
                }
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() => ResetColliders();

        void MixImpulse(SimulationTarget target) {
            if (!target.HasClip) {
                return;
            }
            float distance;
            if (hitCount != 0) {
                distance = Vector3.Distance(transform.position, hits[0]);
                int lastHit = hitCount - 1;
                for (int hit = 0; hit < lastHit; hit++) {
                    distance += Vector3.Distance(hits[hit], hits[hit + 1]);
                }
                distance += Vector3.Distance(hits[lastHit], target.transform.position);
            } else {
                distance = Vector3.Distance(transform.position, target.transform.position);
            }
            float timeOffset = distance / SpeedOfSound * target.SampleRate;
            if (timeOffset < target.MaxSamples - 1) {
                float gain = 1f / timeOffset;
                if (gain > 1) {
                    gain = 1;
                }
                if (ChangePhase && (hitCount & 1) == 1) {
                    gain = -gain;
                }
                target.Impulse[(int)timeOffset] += gain;
            }
        }

        void PaintPath(SimulationTarget target) {
            Vector3 lastPos = transform.position;
            Color lastColor = new Color(0, 1, 0, .5f);
            float colorStep = 1f / Bounces,
                alphaStep = colorStep * .25f;
            for (int hit = 0; hit < hitCount; hit++) {
                lastColor.r += colorStep;
                lastColor.b += colorStep;
                lastColor.a -= alphaStep;
                Gizmos.color = lastColor;
                Gizmos.DrawLine(lastPos, hits[hit]);
                lastPos = hits[hit];
            }
        }

        void Raycast(Action<SimulationTarget> onHit) {
            float maxDist = float.PositiveInfinity,
                step = 360f / Detail;
            if (AudioListener3D.Current != null) {
                maxDist = AudioListener3D.Current.Range;
            }
            Vector3 direction = Vector3.zero;

            hits = new Vector3[Bounces + 1];
            for (int target = 0; target < Targets.Length; target++) {
                Targets[target].Prepare();
            }

            for (int horizontal = 0; horizontal < Detail; horizontal++) {
                for (int vertical = 0; vertical < Detail; vertical++) {
                    Vector3 lastPos = transform.position;
                    Vector3 lastDir = Quaternion.Euler(direction) * Vector3.forward;
                    hitCount = 0;
                    for (int bounce = 0; bounce < Bounces; bounce++) {
                        if (Physics.Raycast(lastPos, lastDir, out RaycastHit hit, maxDist, Layers.value)) {
                            for (int i = 0; i < colliders.Length; i++) {
                                if (colliders[i] == hit.collider) { // Found a path to one collider
                                    hits[hitCount++] = hit.point;
                                    onHit(Targets[i]);
                                    bounce = Bounces; // Break outer loop
                                    break;
                                }
                            }
                            lastDir = Vector3.Reflect(lastDir, hit.normal);
                            hits[hitCount++] = lastPos = hit.point;
                        } else {
                            break;
                        }
                    }
                    direction.y += step;
                }
                direction.x += step;
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDrawGizmosSelected() {
            if (ShowAllRays) {
                DrawDebugRays();
            } else {
                if (Targets.Length != colliders.Length) {
                    ResetColliders();
                }
                Raycast(PaintPath);
            }
        }

        /// <summary>
        /// Render new impulse responses by the state of the scene.
        /// </summary>
        public void Update() {
            if (Targets.Length != colliders.Length) {
                ResetColliders();
            }
            Raycast(MixImpulse);
            for (int target = 0; target < Targets.Length; target++) {
                Targets[target].UpdateFilter();
            }
        }
    }
}