using System;
using System.Numerics;

using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    class ObjectInfoBlock {
        /// <summary>
        /// This block contained position information.
        /// </summary>
        bool validPosition;

        /// <summary>
        /// This frame contains the difference from the last object position instead of an exact position.
        /// </summary>
        bool differentialPosition;

        /// <summary>
        /// Object volume multiplier. Any negative value means reusing the last gain.
        /// </summary>
        float gain = -1;

        /// <summary>
        /// Object distance from the center of the room. Distant objects are mapped closer to the listening position.
        /// </summary>
        float distance;

        /// <summary>
        /// Object size. Any negative value means reusing the last size.
        /// </summary>
        float size = -1;

        /// <summary>
        /// A multiplier in screen-anchored rendering.
        /// </summary>
        float depthFactor, screenFactor;

        /// <summary>
        /// The object's anchor position.
        /// </summary>
        ObjectAnchor anchor = ObjectAnchor.Room;

        /// <summary>
        /// The coded position information, either exact or differential.
        /// </summary>
        Vector3 position;

        public ObjectInfoBlock(BitExtractor extractor, int blk, bool bedOrISFObject) {
            bool inactive = extractor.ReadBit();
            int basicInfoStatus = inactive ? 0 : (blk == 0 ? 1 : extractor.Read(2));
            if ((basicInfoStatus == 1) || (basicInfoStatus == 3))
                ObjectBasicInfo(extractor, basicInfoStatus == 1);

            int renderInfoStatus = 0;
            if (!inactive && !bedOrISFObject)
                renderInfoStatus = blk == 0 ? 1 : extractor.Read(2);
            if ((renderInfoStatus == 1) || (renderInfoStatus == 3))
                ObjectRenderInfo(extractor, blk, renderInfoStatus == 1);

            if (extractor.ReadBit()) // Additional table data
                extractor.Skip((extractor.Read(4) + 1) * 8);

            if (bedOrISFObject)
                anchor = ObjectAnchor.Speaker;
        }

        public void UpdateSource(Source source, ref Vector3 lastPrecise) {
            if (gain >= 0)
                source.Volume = gain;
            if (size >= 0)
                source.Size = size;

            if (validPosition && anchor != ObjectAnchor.Speaker) {
                if (differentialPosition)
                    source.Position = new Vector3(
                        QMath.Clamp(lastPrecise.X + position.X, 0, 1),
                        QMath.Clamp(lastPrecise.Y + position.Y, 0, 1),
                        QMath.Clamp(lastPrecise.Z + position.Z, 0, 1)
                    );
                else
                    source.Position = position;
                lastPrecise = source.Position;

                switch (anchor) {
                    case ObjectAnchor.Room:
                        if (!float.IsNaN(distance)) {
                            Vector3 intersect = source.Position.MapToCube();
                            float distanceFactor = intersect.Length() / distance;
                            source.Position = distanceFactor * intersect + (1 - distanceFactor) * roomCenter;
                        }
                        break;
                    case ObjectAnchor.Screen:
                        Vector3 reference =
                            new Vector3((source.Position.X - .5f) * Listener.ScreenSize.X + .5f,
                            source.Position.Y,
                            (source.Position.Z + 1) * Listener.ScreenSize.Y
                        );
                        Vector3 screenFactorMultiplier = new Vector3(screenFactor, 1, screenFactor);
                        float depth = MathF.Pow(source.Position.Y, depthFactor);
                        Vector3 depthFactorMultiplier = new Vector3(depth, 1, depth);
                        source.Position = depthFactorMultiplier * (screenFactorMultiplier * source.Position + reference -
                            screenFactorMultiplier * reference) + reference - depthFactorMultiplier * reference;
                        break;
                }

                // Convert to Cavern coordinate space
                source.Position = Listener.EnvironmentSize * new Vector3(
                    source.Position.X * 2 - 1,
                    source.Position.Z,
                    source.Position.Y * -2 + 1
                );
            }
        }

        void ObjectBasicInfo(BitExtractor extractor, bool readAllBlocks) {
            int blocks = readAllBlocks ? 3 : extractor.Read(2);

            // Gain
            if ((blocks & 2) != 0) {
                int gainHelper = extractor.Read(2);
                gain = gainHelper switch {
                    0 => 1,
                    1 => 0,
                    2 => (gainHelper = extractor.Read(6)) < 15 ? QMath.DbToGain(15 - gainHelper) : QMath.DbToGain(14 - gainHelper),
                    _ => -1,
                };
            }

            // Priority - unneccessary, everything's rendered
            if ((blocks & 1) != 0 && !extractor.ReadBit())
                extractor.Skip(5);
        }

        void ObjectRenderInfo(BitExtractor extractor, int blk, bool readAllBlocks) {
            int blocks = readAllBlocks ? 15 : extractor.Read(4);

            // Spatial position
            if (validPosition = (blocks & 8) != 0) {
                differentialPosition = blk != 0 && extractor.ReadBit();
                if (differentialPosition)
                    position = new Vector3(
                        extractor.ReadSigned(3) * xyScale,
                        extractor.ReadSigned(3) * xyScale,
                        extractor.ReadSigned(3) * zScale
                    );
                else {
                    int posX = extractor.Read(6);
                    int posY = extractor.Read(6);
                    bool signZ = extractor.ReadBit();
                    int posZ = extractor.Read(4);
                    position = new Vector3(
                        Math.Min(1, posX * xyScale),
                        Math.Min(1, posY * xyScale),
                        Math.Min(1, (signZ ? 1 : -1) * posZ * zScale)
                    );
                }
                if (extractor.ReadBit()) { // Distance specified
                    if (extractor.ReadBit()) // Infinite distance
                        distance = 100; // Close enough
                    else
                        distance = distanceFactors[extractor.Read(4)];
                } else
                    distance = float.NaN;
            }

            // Zone constraints - the renderer is not prepared for zoning
            if ((blocks & 4) != 0)
                extractor.Skip(4);

            // Scaling
            if ((blocks & 2) != 0)
                size = extractor.Read(2) switch {
                    0 => 0,
                    1 => extractor.Read(5) * sizeScale,
                    2 => (new Vector3(extractor.Read(5), extractor.Read(5), extractor.Read(5)) * sizeScale).Length(),
                    _ => -1,
                };

            // Screen anchoring
            if ((blocks & 1) != 0 && extractor.ReadBit()) {
                anchor = ObjectAnchor.Screen;
                screenFactor = (extractor.Read(3) + 1) * .125f;
                depthFactor = depthFactors[extractor.Read(2)];
            }

            extractor.Skip(1); // Snap to the nearest channel - unused
        }

        const float xyScale = 1 / 62f;
        const float zScale = 1 / 15f;
        const float sizeScale = 1 / 31f;
        static readonly Vector3 roomCenter = new Vector3(.5f, .5f, 0);
        static readonly float[] distanceFactors =
            { 1.1f, 1.3f, 1.6f, 2.0f, 2.5f, 3.2f, 4.0f, 5.0f, 6.3f, 7.9f, 10.0f, 12.6f, 15.8f, 20.0f, 25.1f, 50.1f };
        static readonly float[] depthFactors = { .25f, .5f, 1, 2 };
    }
}