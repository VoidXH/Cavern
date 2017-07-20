using UnityEngine;

namespace Cavern.Cavern4D {
    /// <summary>Applies a generated seat motion to an object.</summary>
    [AddComponentMenu("Audio/4D Seat")]
    public class Seat4D : MonoBehaviour {
        /// <summary>The related 4D converter to fetch seat data from.</summary>
        [Tooltip("The related 4D converter to fetch seat data from.")]
        public Cavern4DBase Base;
        /// <summary>Seat position in the column from the front.</summary>
        [Tooltip("Seat position in the column from the front.")]
        public int Row;
        /// <summary>Seat position in the row from the left.</summary>
        [Tooltip("Seat position in the row from the left.")]
        public int Column;

        /// <summary>Keep the last height for delta movement if the object is moved by something else.</summary>
        float LastHeight;

        void LateUpdate() {
            float Height = Base.SeatMovements[Row][Column].Height;
            transform.localPosition += new Vector3(0, Height - LastHeight, 0);
            LastHeight = Height;
            transform.localRotation = Quaternion.Euler(Base.SeatMovements[Row][Column].Rotation);
        }
    }
}