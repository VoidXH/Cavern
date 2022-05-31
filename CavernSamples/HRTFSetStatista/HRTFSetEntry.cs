using Cavern;
using Cavern.Format;

namespace HRTFSetStatista {
    public class HRTFSetEntry {
        public double Azimuth { get; private set; }
        public double Elevation { get; private set; }
        public double Distance { get; private set; }

        public float[][] Data { get; private set; }

        public HRTFSetEntry(double hAngle, double wAngle, double distance, string path) {
            Azimuth = hAngle;
            Elevation = wAngle;
            Distance = distance;

            Clip clip = AudioReader.ReadClip(path);
            Data = new float[clip.Channels][];
            for (int channel = 0; channel < clip.Channels; ++channel)
                Data[channel] = new float[clip.Samples];
            clip.GetData(Data, 0);
        }
    }
}