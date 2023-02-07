using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

namespace Cavern.Debug {
    /// <summary>
    /// Visualizer for all objects.
    /// </summary>
    [AddComponentMenu("Audio/Debug/Monitor")]
    public class Monitor : MonoBehaviour {
        /// <summary>
        /// Snap objects out of the room to walls.
        /// </summary>
        [Tooltip("Snap objects out of the room to walls.")]
        public bool limitBounds = true;

        /// <summary>
        /// Normalize size to this by local scale if not 0.
        /// </summary>
        [Tooltip("Normalize size to this by local scale if not 0.")]
        public float autoScale;

        /// <summary>
        /// Use a different listener other than the scene's <see cref="AudioListener3D"/>.
        /// </summary>
        public Listener listenerOverride;

        /// <summary>
        /// Alias for <see cref="limitBounds"/> to be used with Unity Events.
        /// </summary>
        public bool LimitBounds {
            get => limitBounds;
            set => limitBounds = value;
        }

        /// <summary>
        /// Displayed room edges.
        /// </summary>
        readonly GameObject[] edges = new GameObject[12];

        /// <summary>
        /// List of visualized objects.
        /// </summary>
        readonly List<Visualized> objects = new List<Visualized>();

        /// <summary>
        /// Last environment scale.
        /// </summary>
        Vector3 roomScale;

        /// <summary>
        /// A visualized object.
        /// </summary>
        class Visualized {
            /// <summary>
            /// Created object for visualization.
            /// </summary>
            public GameObject Object;

            /// <summary>
            /// The visualized source.
            /// </summary>
            public Source Target;

            public Visualized(GameObject obj, Source target) {
                Object = obj;
                Target = target;
            }
        }

        void SetRoomScale() {
            roomScale = VectorUtils.VectorMatch(Listener.EnvironmentSize);
            for (int vertical = 0; vertical < 4; vertical++) {
                float modMult = vertical % 2 == 0 ? -.5f : .5f, divMult = vertical / 2 == 0 ? -.5f : .5f;
                edges[vertical].transform.localPosition = new Vector3(modMult * roomScale.x, 0, divMult * roomScale.z);
                edges[vertical].transform.localScale = new Vector3(.1f, roomScale.y, .1f);
                int horizontal = vertical + 4;
                edges[horizontal].transform.localPosition = new Vector3(0, modMult * roomScale.y, divMult * roomScale.z);
                edges[horizontal].transform.localScale = new Vector3(roomScale.x, .1f, .1f);
                horizontal += 4;
                edges[horizontal].transform.localPosition = new Vector3(modMult * roomScale.x, divMult * roomScale.y, 0);
                edges[horizontal].transform.localScale = new Vector3(.1f, .1f, roomScale.z);
            }
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() {
            for (int i = 0; i < 12; i++) {
                (edges[i] = GameObject.CreatePrimitive(PrimitiveType.Cube)).transform.SetParent(transform, false);
            }
            SetRoomScale();
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            // Reset scale
            if (!VectorUtils.VectorCompare(Listener.EnvironmentSize, roomScale)) {
                SetRoomScale();
            }
            Vector3 objScale = new Vector3(.5f, .5f, .5f);
            if (autoScale != 0) {
                float scale = Mathf.Max(roomScale.x, roomScale.y, roomScale.z);
                float invScale = autoScale / scale;
                scale *= .05f;
                objScale = new Vector3(scale, scale, scale);
                transform.localScale = new Vector3(invScale, invScale, invScale);
            }

            // Remove destroyed sources
            objects.RemoveAll(new System.Predicate<Visualized>((Obj) => {
                if (!Obj.Target || !Obj.Target.IsPlaying || Obj.Target.Mute) {
                    Destroy(Obj.Object);
                    return true;
                }
                return false;
            }));

            // Add not visualized sources, update visualized sources
            Quaternion inverseListenerRot = Quaternion.Inverse(AudioListener3D.Current.transform.rotation);
            Listener parent = listenerOverride ?? AudioListener3D.cavernListener;
            IEnumerator<Source> source = parent.ActiveSources.GetEnumerator();

            while (source.MoveNext()) {
                Source current = source.Current;
                if (!current.IsPlaying || current.Mute) {
                    continue;
                }
                Visualized target = null;
                IEnumerator<Visualized> vis = objects.GetEnumerator();
                while (vis.MoveNext()) {
                    if (vis.Current.Target == current) {
                        target = vis.Current;
                        break;
                    }
                }
                if (target == null) {
                    GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    obj.transform.SetParent(transform, false);
                    obj.transform.localScale = objScale;
                    objects.Add(target = new Visualized(obj, current));
                }
                Vector3 pos = inverseListenerRot *
                    (VectorUtils.VectorMatch(target.Target.Position) - AudioListener3D.Current.transform.position);
                if (LimitBounds) {
                    pos = new Vector3(Mathf.Clamp(pos.x, -roomScale.x, roomScale.x), Mathf.Clamp(pos.y, -roomScale.y, roomScale.y),
                        Mathf.Clamp(pos.z, -roomScale.z, roomScale.z));
                }
                target.Object.transform.localPosition = pos * .5f;
            }
        }
    }
}