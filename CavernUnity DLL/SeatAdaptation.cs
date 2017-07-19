using UnityEngine;

namespace Cavern {
    /// <summary>Modifies listener position based on seat occupation data.</summary>
    [AddComponentMenu("Audio/Seat Adaptation")]
    public class SeatAdaptation : MonoBehaviour {
        /// <summary>The number of rows in the room.</summary>
        public int Rows;
        /// <summary>Seats in each row.</summary>
        public int Columns;
        /// <summary>The center position of the room.</summary>
        public Transform Origin;

        /// <summary>A [Rows, Columns] sized array containing if a seat is occupied.</summary>
        public bool[,] SeatsOccupied;

        void Start() {
            SeatsOccupied = new bool[Rows, Columns];
        }

        void Update() {
            float RRows = Rows - 1, RColumns = Columns - 1;
            float CenterRow = RRows * .5f, CenterColumn = RColumns * .5f;
            int SeatsFound = 0;
            float SoundPositionX = 0, SoundPositionZ = 0;
            for (int Z = 0; Z < Rows; ++Z) {
                for (int X = 0; X < Columns; ++X) {
                    if (SeatsOccupied[Z, X]) {
                        SoundPositionX += X;
                        SoundPositionZ += Z;
                        ++SeatsFound;
                    }
                }
            }
            float SeatRecip = 1f / SeatsFound;
            SoundPositionX *= SeatRecip;
            SoundPositionZ *= SeatRecip;
            AudioListener3D.Current.transform.position = SeatsFound == 0 ? Origin.position :
                Origin.position + Origin.rotation * new Vector3(AudioListener3D.EnvironmentSize.x * .5f - SoundPositionX / RColumns * AudioListener3D.EnvironmentSize.x, 0,
                                                                AudioListener3D.EnvironmentSize.z * .5f - SoundPositionZ / RRows * AudioListener3D.EnvironmentSize.z);
        }

        void OnDisable() {
            if (AudioListener3D.Current != null && Origin != null)
                AudioListener3D.Current.transform.position = Origin.position;
        }
    }
}