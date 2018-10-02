using System.Runtime.CompilerServices;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Cavern {
    /// <summary>Creates an atmosphere of the given <see cref="Clips"/>.</summary>
    [AddComponentMenu("Audio/3D Atmosphere")]
    public class Atmosphere : MonoBehaviour {
        /// <summary>The possible clips that will be played at random positions.</summary>
        [Tooltip("The possible clips that will be played at random positions.")]
        public AudioClip[] Clips;
        /// <summary>The amount of audio sources in the atmosphere.</summary>
        [Tooltip("The amount of audio sources in the atmosphere.")]
        public int Sources;
        /// <summary>Create a spherical environment instead of cubic.</summary>
        [Tooltip("Create a spherical environment instead of cubic.")]
        public bool Spherical = false;
        /// <summary>Minimal distance to spawn sources from the object's position.</summary>
        [Tooltip("Minimal distance to spawn sources from the object's position.")]
        public float MinDistance = 5;
        /// <summary>Maximum distance to spawn sources from the object's position.</summary>
        [Tooltip("Maximum distance to spawn sources from the object's position.")]
        public float MaxDistance = 20;
        /// <summary>Atmosphere volume.</summary>
        [Tooltip("Atmosphere volume.")]
        [Range(0, 1)] public float Volume = .25f;
        /// <summary>Show created objects.</summary>
        [Tooltip("Show created objects.")]
        public bool Visualize = false;

        struct AtmosphereObject {
            public GameObject Object;
            public AudioSource3D Source;
        }

        AtmosphereObject[] Objects;

        void Start() => Objects = new AtmosphereObject[Sources];

        void OnDrawGizmosSelected() {
            if (!gameObject.activeInHierarchy)
                return;
            if (Spherical) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, MinDistance);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, MaxDistance);
            } else {
                float CorrectMinDist = MinDistance * 2, CorrectMaxDist = MaxDistance * 2;
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, new Vector3(CorrectMinDist, CorrectMinDist, CorrectMinDist));
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, new Vector3(CorrectMaxDist, CorrectMaxDist, CorrectMaxDist));
            }
        }

        delegate Vector3 PlacerFunc(Vector3 Angles);

        void Update() {
            PlacerFunc DirectionFunc = Spherical ? (PlacerFunc)CavernUtilities.PlaceInSphere : CavernUtilities.PlaceInCube;
            float TargetVolume = Volume / Sources;
            for (int Source = 0, ClipCount = Clips.Length; Source < Sources; ++Source) {
                if (!Objects[Source].Object) {
                    if (Visualize)
                        Objects[Source].Object = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    else
                        Objects[Source].Object = new GameObject();
                    GameObject Creation = Objects[Source].Object;
                    // Position source
                    Vector3 Direction = DirectionFunc(new Vector3(Random.value * 360, Random.value * 360));
                    float Distance = (MaxDistance - MinDistance) * Random.value + MinDistance;
                    Creation.transform.position = transform.position + Direction * Distance;
                    // Add audio
                    AudioSource3D NewSource = Objects[Source].Source = Creation.AddComponent<AudioSource3D>();
                    NewSource.Clip = Clips[(int)(ClipCount * Random.value)];
                    NewSource.Volume = TargetVolume;
                } else if (!Objects[Source].Source.IsPlaying)
                    Destroy(Objects[Source].Object);
            }
        }
    }
}