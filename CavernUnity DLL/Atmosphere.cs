using System.Diagnostics.CodeAnalysis;
using UnityEngine;

using Cavern.Utilities;

using Random = UnityEngine.Random;
using Vector3D = System.Numerics.Vector3;

namespace Cavern {
    /// <summary>
    /// Creates an atmosphere of the given <see cref="Clips"/>.
    /// </summary>
    [AddComponentMenu("Audio/3D Atmosphere")]
    public class Atmosphere : MonoBehaviour {
        /// <summary>
        /// The possible clips that will be played at random positions.
        /// </summary>
        [Tooltip("The possible clips that will be played at random positions.")]
        public AudioClip[] Clips;

        /// <summary>
        /// The amount of audio sources in the atmosphere.
        /// </summary>
        [Tooltip("The amount of audio sources in the atmosphere.")]
        public int Sources;

        /// <summary>
        /// Create a spherical environment instead of cubic.
        /// </summary>
        [Tooltip("Create a spherical environment instead of cubic.")]
        public bool Spherical;

        /// <summary>
        /// Minimal distance to spawn sources from the object's position.
        /// </summary>
        [Tooltip("Minimal distance to spawn sources from the object's position.")]
        public float MinDistance = 5;

        /// <summary>
        /// Maximum distance to spawn sources from the object's position.
        /// </summary>
        [Tooltip("Maximum distance to spawn sources from the object's position.")]
        public float MaxDistance = 20;

        /// <summary>
        /// Atmosphere volume.
        /// </summary>
        [Tooltip("Atmosphere volume.")]
        [Range(0, 1)] public float Volume = .25f;

        /// <summary>
        /// Show created objects.
        /// </summary>
        [Tooltip("Show created objects.")]
        public bool Visualize;

        struct AtmosphereObject {
            public GameObject Object;
            public AudioSource3D Source;
        }

        AtmosphereObject[] objects;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() => objects = new AtmosphereObject[Sources];

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDrawGizmosSelected() {
            if (!gameObject.activeInHierarchy) {
                return;
            }
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

        delegate Vector3D PlacerFunc(Vector3D angles);

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            PlacerFunc directionFunc = Spherical ? (PlacerFunc)VectorExtensions.PlaceInSphere : VectorExtensions.PlaceInCube;
            float targetVolume = Volume / Sources;
            for (int source = 0; source < Sources; ++source) {
                if (!objects[source].Object) {
                    if (Visualize) {
                        objects[source].Object = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    } else {
                        objects[source].Object = new GameObject();
                    }
                    GameObject creation = objects[source].Object;

                    // Position source
                    Vector3D direction = directionFunc(new Vector3D(Random.value * 360, Random.value * 360, 0));
                    float distance = (MaxDistance - MinDistance) * Random.value + MinDistance;
                    creation.transform.position = transform.position + VectorUtils.VectorMatch(direction) * distance;

                    // Add audio
                    AudioSource3D newSource = objects[source].Source = creation.AddComponent<AudioSource3D>();
                    newSource.Clip = Clips[(int)(Clips.Length * Random.value)];
                    newSource.Volume = targetVolume;
                } else if (!objects[source].Source.IsPlaying) {
                    Destroy(objects[source].Object);
                }
            }
        }
    }
}