using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern {
    /// <summary>
    /// Modifies listener position based on seat occupation data.
    /// </summary>
    [AddComponentMenu("Audio/Seat Adaptation")]
    public class SeatAdaptation : MonoBehaviour {
        /// <summary>
        /// The number of rows in the room.
        /// </summary>
        public int Rows;

        /// <summary>
        /// Seats in each row.
        /// </summary>
        public int Columns;

        /// <summary>
        /// The center position of the room.
        /// </summary>
        public Transform Origin;

        /// <summary>
        /// A [Rows, Columns] sized array containing if a seat is occupied.
        /// </summary>
        public bool[,] SeatsOccupied;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Start() => SeatsOccupied = new bool[Rows, Columns];

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void Update() {
            float rRows = Rows - 1, rColumns = Columns - 1;
            int seatsFound = 0;
            float soundPositionX = 0, soundPositionZ = 0;
            for (int z = 0; z < Rows; ++z) {
                for (int x = 0; x < Columns; ++x) {
                    if (SeatsOccupied[z, x]) {
                        soundPositionX += x;
                        soundPositionZ += z;
                        ++seatsFound;
                    }
                }
            }
            float seatRecip = 1f / seatsFound;
            soundPositionX *= seatRecip;
            soundPositionZ *= seatRecip;
            AudioListener3D.Current.transform.position = seatsFound == 0 ? Origin.position :
                Origin.position + Origin.rotation *
                new Vector3(Listener.EnvironmentSize.X * .5f - soundPositionX / rColumns * Listener.EnvironmentSize.X, 0,
                            Listener.EnvironmentSize.Z * .5f - soundPositionZ / rRows * Listener.EnvironmentSize.Z);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void OnDisable() {
            if (AudioListener3D.Current != null && Origin != null) {
                AudioListener3D.Current.transform.position = Origin.position;
            }
        }
    }
}