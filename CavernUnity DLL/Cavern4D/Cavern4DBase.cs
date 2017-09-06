using System;
using UnityEngine;

namespace Cavern.Cavern4D {
    /// <summary>Seat movement generation for <see cref="Cavernize"/>.</summary>
    [AddComponentMenu("Audio/4D Processor")]
    public class Cavern4DBase : MonoBehaviour {
        /// <summary>The Cavernized audio object to be converted.</summary>
        [Tooltip("The Cavernized audio object to be converted.")]
        public Cavernize CavernSource;
        /// <summary>Rotation aggressiveness.</summary>
        [Tooltip("Rotation aggressiveness.")]
        public float RotationConstant = 90;
        /// <summary>Maximum forward and backward seat rotation.</summary>
        [Tooltip("Maximum forward and backward seat rotation.")]
        [Range(0, 45)] public float MaxRotationFace = 20;
        /// <summary>Maximum sideways seat rotation.</summary>
        [Tooltip("Maximum sideways seat rotation.")]
        [Range(0, 45)] public float MaxRotationSide = 10;
        /// <summary>Number of seat rows.</summary>
        [Tooltip("Number of seat rows.")]
        public int Rows;
        /// <summary>Number of seats in a row.</summary>
        [Tooltip("Number of seats in a row.")]
        public int Columns;

        /// <summary>Seat movement description.</summary>
        public struct SeatData {
            /// <summary>Seat elevation in the range of <see cref="Cavernize.ChannelHeights"/>.</summary>
            [Tooltip("Seat elevation in Cavernize's bounds.")]
            public float Height;
            /// <summary>Seat rotation Euler angles.</summary>
            public Vector3 Rotation;
        }

        /// <summary>Seat movement descriptions. The first dimension is the row, the second is the column.</summary>
        public SeatData[][] SeatMovements;

        void Start() {
            SeatMovements = new SeatData[Rows][];
            for (int i = 0; i < Rows; ++i)
                SeatMovements[i] = new SeatData[Columns];
        }

        void OnDisable() {
            for (int i = 0; i < Rows; i++)
                Array.Clear(SeatMovements[i], 0, Columns);
        }

        void Update() {
            if (!CavernSource) {
                OnDisable();
                return;
            }
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    SeatMovements[i][j].Height = 200;
            int LastRow = Rows - 1, LastColumn = Columns - 1;
            if (CavernSource.WrittenOutput[0]) // Front left
                SeatMovements[0][0].Height = CavernSource.ChannelHeights[0];
            if (CavernSource.WrittenOutput[1]) // Front right
                SeatMovements[0][LastColumn].Height = CavernSource.ChannelHeights[1];
            if (CavernSource.WrittenOutput[2] && CavernSource.ChannelHeights[2] != -2) // Center
                SeatMovements[0][LastColumn / 2].Height = CavernSource.ChannelHeights[2];
            if (CavernSource.WrittenOutput[4]) // Side left
                SeatMovements[LastRow][0].Height = CavernSource.ChannelHeights[4];
            if (CavernSource.WrittenOutput[5]) // Side right
                SeatMovements[LastRow][LastColumn].Height = CavernSource.ChannelHeights[5];
            // Addition is okay, and should be used, as the rears are near the sides in the back corners.
            if (CavernSource.WrittenOutput[6]) // Rear left
                SeatMovements[LastRow][0].Height += CavernSource.ChannelHeights[6];
            if (CavernSource.WrittenOutput[7]) // Rear right
                SeatMovements[LastRow][LastColumn].Height += CavernSource.ChannelHeights[7];
            if (CavernSource.WrittenOutput[8]) // Rear center
                SeatMovements[LastRow][LastColumn / 2].Height = CavernSource.ChannelHeights[8];
            // Use the front channels for moving all seats if nothing else is available for the rear sides
            if (SeatMovements[LastRow][0].Height == 200)
                SeatMovements[LastRow][0].Height = CavernSource.ChannelHeights[0];
            if (SeatMovements[LastRow][LastColumn].Height == 200)
                SeatMovements[LastRow][LastColumn].Height = CavernSource.ChannelHeights[1];
            // Seat position interpolation
            for (int Row = 0; Row < Rows; Row++) {
                int Prev = 0;
                for (int Column = 0; Column < Columns; ++Column) {
                    if (SeatMovements[Row][Column].Height != 200) {
                        float LerpDiv = Column - Prev;
                        for (int OldColumn = Prev; OldColumn < Column; ++OldColumn)
                            SeatMovements[Row][OldColumn].Height =
                                CavernUtilities.FastLerp(SeatMovements[Row][Prev].Height, SeatMovements[Row][Column].Height, (OldColumn - Prev) / LerpDiv);
                        Prev = Column;
                    }
                }
                if (Prev != LastColumn) {
                    float LerpDiv = LastColumn - Prev;
                    for (int OldColumn = Prev; OldColumn < LastColumn; ++OldColumn)
                        SeatMovements[Row][OldColumn].Height =
                            CavernUtilities.FastLerp(SeatMovements[Row][Prev].Height, SeatMovements[Row][LastColumn].Height, (OldColumn - Prev) / LerpDiv);
                }
            }
            for (int Column = 0; Column < Columns; ++Column) {
                int Prev = 0;
                for (int Row = 0; Row < Rows; Row++) {
                    if (SeatMovements[Row][Column].Height != 200) {
                        float LerpDiv = Row - Prev;
                        for (int OldRow = Prev; OldRow < Row; ++OldRow)
                            SeatMovements[OldRow][Column].Height =
                                CavernUtilities.FastLerp(SeatMovements[Prev][Column].Height, SeatMovements[Row][Column].Height, (OldRow - Prev) / LerpDiv);
                        Prev = Row;
                    }
                }
                if (Prev != LastRow) {
                    float LerpDiv = LastRow - Prev;
                    for (int OldRow = Prev; OldRow < LastRow; ++OldRow)
                        SeatMovements[OldRow][Column].Height = CavernUtilities.FastLerp(SeatMovements[Prev][Column].Height, SeatMovements[LastRow][Column].Height, (OldRow - Prev) / LerpDiv);
                }
            }
            // Seat rotation interpolation
            for (int Row = 0; Row < Rows; ++Row) {
                SeatMovements[Row][0].Rotation.z = Mathf.Clamp((SeatMovements[Row][1].Height - SeatMovements[Row][0].Height) * RotationConstant * 2, -MaxRotationSide, MaxRotationSide);
                for (int Column = 1; Column < LastColumn; Column++)
                    SeatMovements[Row][Column].Rotation.z = Mathf.Clamp((SeatMovements[Row][Column + 1].Height - SeatMovements[Row][Column - 1].Height) * RotationConstant, -MaxRotationSide, MaxRotationSide);
                SeatMovements[Row][LastColumn].Rotation.z = Mathf.Clamp((SeatMovements[Row][LastColumn].Height - SeatMovements[Row][LastColumn - 1].Height) * RotationConstant * 2,
                    -MaxRotationSide, MaxRotationSide);
            }
            for (int Column = 0; Column < Columns; ++Column) {
                SeatMovements[0][Column].Rotation.x = Mathf.Clamp((SeatMovements[1][Column].Height - SeatMovements[0][Column].Height) * RotationConstant * 2, -20, 20);
                for (int Row = 1; Row < LastRow; Row++)
                    SeatMovements[Row][Column].Rotation.x = Mathf.Clamp((SeatMovements[Row + 1][Column].Height - SeatMovements[Row - 1][Column].Height) * RotationConstant, -MaxRotationFace, MaxRotationFace);
                SeatMovements[LastRow][Column].Rotation.x = Mathf.Clamp((SeatMovements[LastRow][Column].Height - SeatMovements[LastRow - 1][Column].Height) * RotationConstant * 2,
                    -MaxRotationFace, MaxRotationFace);
            }
        }
    }
}