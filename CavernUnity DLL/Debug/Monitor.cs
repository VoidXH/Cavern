using System.Collections.Generic;
using UnityEngine;

namespace Cavern.Debug {
    /// <summary>Visualizer for all objects.</summary>
    [AddComponentMenu("Audio/Debug/Monitor")]
    public class Monitor : MonoBehaviour {
        /// <summary>Snap objects out of the room to walls.</summary>
        public bool LimitBounds = true;
        /// <summary>Normalize size to this by local scale if not 0.</summary>
        [Tooltip("Normalize size to this by local scale if not 0.")]
        public float AutoScale = 0;

        /// <summary>Displayed room edges.</summary>
        GameObject[] Edges = new GameObject[12];
        /// <summary>Last environment scale.</summary>
        Vector3 RoomScale;

        /// <summary>A visualized object.</summary>
        class Visualized {
            /// <summary>Created object for visualization.</summary>
            public GameObject Object;
            /// <summary>The visualized source.</summary>
            public AudioSource3D Target;

            public Visualized(GameObject Object, AudioSource3D Target) {
                this.Object = Object;
                this.Target = Target;
            }
        }

        /// <summary>List of visualized objects.</summary>
        List<Visualized> Objects = new List<Visualized>();

        void SetRoomScale() {
            RoomScale = AudioListener3D.EnvironmentSize;
            for (int Vertical = 0; Vertical < 4; ++Vertical) {
                float ModMult = Vertical % 2 == 0 ? -.5f : .5f, DivMult = Vertical / 2 == 0 ? -.5f : .5f;
                Edges[Vertical].transform.localPosition = new Vector3(ModMult * RoomScale.x, 0, DivMult * RoomScale.z);
                Edges[Vertical].transform.localScale = new Vector3(.1f, RoomScale.y, .1f);
                int Horizontal = Vertical + 4;
                Edges[Horizontal].transform.localPosition = new Vector3(0, ModMult * RoomScale.y, DivMult * RoomScale.z);
                Edges[Horizontal].transform.localScale = new Vector3(RoomScale.x, .1f, .1f);
                Horizontal += 4;
                Edges[Horizontal].transform.localPosition = new Vector3(ModMult * RoomScale.x, DivMult * RoomScale.y, 0);
                Edges[Horizontal].transform.localScale = new Vector3(.1f, .1f, RoomScale.z);
            }
        }

        void Start() {
            for (int i = 0; i < 12; ++i)
                (Edges[i] = GameObject.CreatePrimitive(PrimitiveType.Cube)).transform.SetParent(transform, false);
            SetRoomScale();
        }

        void Update() {
            // Reset scale
            if (RoomScale != AudioListener3D.EnvironmentSize)
                SetRoomScale();
            Vector3 ObjScale = new Vector3(.5f, .5f, .5f);
            if (AutoScale != 0) {
                float Scale = Mathf.Max(RoomScale.x, RoomScale.y, RoomScale.z);
                float InvScale = AutoScale / Scale;
                Scale *= .05f;
                ObjScale = new Vector3(Scale, Scale, Scale);
                transform.localScale = new Vector3(InvScale, InvScale, InvScale);
            }
            // Remove destroyed sources
            Objects.RemoveAll(new System.Predicate<Visualized>((Obj) => {
                if (!Obj.Target || !Obj.Target.IsPlaying || Obj.Target.Mute) {
                    Destroy(Obj.Object);
                    return true;
                }
                return false;
            }));
            // Add not visualized sources, update visualized sources
            IEnumerator<Visualized> Vis;
            Quaternion InverseListenerRot = Quaternion.Inverse(AudioListener3D.Current.transform.rotation);
            IEnumerator<AudioSource3D> Source = AudioListener3D.ActiveSources.GetEnumerator();
            while (Source.MoveNext()) {
                AudioSource3D Current = Source.Current;
                if (!Current.IsPlaying || Current.Mute)
                    continue;
                Visualized Target = null;
                Vis = Objects.GetEnumerator();
                while (Vis.MoveNext()) {
                    if (Vis.Current.Target == Current) {
                        Target = Vis.Current;
                        break;
                    }
                }
                if (Target == null) {
                    GameObject Obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Obj.transform.SetParent(transform, false);
                    Obj.transform.localScale = ObjScale;
                    Objects.Add(Target = new Visualized(Obj, Current));
                }
                Vector3 Pos = InverseListenerRot * (Target.Target.gameObject.transform.position - AudioListener3D.LastPosition);
                if (LimitBounds)
                    Pos = new Vector3(Mathf.Clamp(Pos.x, -RoomScale.x, RoomScale.x), Mathf.Clamp(Pos.y, -RoomScale.y, RoomScale.y),
                        Mathf.Clamp(Pos.z, -RoomScale.z, RoomScale.z));
                Target.Object.transform.localPosition = Pos * .5f;
            }
        }
    }
}