using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.FilterInterfaces {
    /// <summary>
    /// A filter built on raytracing with ideal geometric relections.
    /// </summary>
    public abstract class Raytraced : MonoBehaviour {
        /// <summary>
        /// Number of rays per plane.
        /// </summary>
        [Header("Raycast")]
        [Tooltip("Number of rays per plane.")]
        public int Detail = 5;

        /// <summary>
        /// Maximum surface bounces.
        /// </summary>
        [Tooltip("Maximum surface bounces.")]
        public int Bounces = 3;

        /// <summary>
        /// Layers to bounce the sound off from.
        /// </summary>
        [Tooltip("Layers to bounce the sound off from.")]
        public LayerMask Layers = int.MaxValue;

        /// <summary>
        /// Draw all emitted rays as gizmos.
        /// </summary>
        protected void DrawDebugRays() {
            float maxDist = AudioListener3D.Current ? AudioListener3D.Current.Range : float.PositiveInfinity,
                step = 360f / Detail,
                colorStep = 1f / Bounces,
                alphaStep = colorStep * .25f;
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
                            lastColor.r += colorStep;
                            lastColor.b += colorStep;
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

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDrawGizmosSelected() => DrawDebugRays();
    }
}
