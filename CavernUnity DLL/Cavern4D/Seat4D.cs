using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Cavern.Cavern4D {
    /// <summary>
    /// Applies a generated seat motion to an object.
    /// </summary>
    [AddComponentMenu("Audio/4D Seat")]
    public class Seat4D : MonoBehaviour {
        /// <summary>
        /// The related 4D converter to fetch seat data from.
        /// </summary>
        [Tooltip("The related 4D converter to fetch seat data from.")]
        public Cavern4DBase Base;

        /// <summary>
        /// Seat position in the column from the front.
        /// </summary>
        [Tooltip("Seat position in the column from the front.")]
        public int Row;

        /// <summary>
        /// Seat position in the row from the left.
        /// </summary>
        [Tooltip("Seat position in the row from the left.")]
        public int Column;

        /// <summary>
        /// Keep the last height for delta movement if the object is moved by something else.
        /// </summary>
        float lastHeight;

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by Unity lifecycle")]
        void LateUpdate() {
            float height = Base.SeatMovements[Row][Column].Height;
            transform.localPosition += new Vector3(0, height - lastHeight, 0);
            lastHeight = height;
            transform.localRotation = Quaternion.Euler(Base.SeatMovements[Row][Column].Rotation);
        }
    }
}