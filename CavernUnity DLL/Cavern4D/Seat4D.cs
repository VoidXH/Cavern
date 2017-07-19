using UnityEngine;

namespace Cavern.Cavern4D {
    [AddComponentMenu("Audio/4D Seat")]
    public class Seat4D : MonoBehaviour {
        public Cavern4DBase Base;
        public int Row, Column;

        float LastHeight;

        void LateUpdate() {
            float Height = Base.SeatMovements[Row][Column].Height;
            transform.position += new Vector3(0, Height - LastHeight, 0);
            LastHeight = Height;
            transform.rotation = Quaternion.Euler(Base.SeatMovements[Row][Column].Rotation);
        }
    }
}