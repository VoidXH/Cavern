using Cavern.Format.Utilities;

namespace Cavern.Format.InOut {
    partial class EnhancedAC3Header {
        bool infomdate;
        bool copyrightb;
        bool origbs;
        int dsurmod;
        int dheadphonmod;
        int dsurexmod;
        bool audprodie;
        int mixlevel;
        int roomtyp;
        bool adconvtyp;
        bool audprodie2;
        int mixlevel2;
        int roomtyp2;
        bool adconvtyp2;
        bool sourcefscod;

        /// <summary>
        /// Parse informational metadata.
        /// </summary>
        void ReadInfoMetadata(BitExtractor extractor) {
            if (!(infomdate = extractor.ReadBit()))
                return;

            bsmod = extractor.Read(3);
            copyrightb = extractor.ReadBit();
            origbs = extractor.ReadBit();
            if (ChannelMode == 2) {
                dsurmod = extractor.Read(2);
                dheadphonmod = extractor.Read(2);
            }
            if (ChannelMode >= 6)
                dsurexmod = extractor.Read(2);
            if (audprodie = extractor.ReadBit()) {
                mixlevel = extractor.Read(5);
                roomtyp = extractor.Read(2);
                adconvtyp = extractor.ReadBit();
            }
            if (ChannelMode == 0) {
                if (audprodie2 = extractor.ReadBit()) {
                    mixlevel2 = extractor.Read(5);
                    roomtyp2 = extractor.Read(2);
                    adconvtyp2 = extractor.ReadBit();
                }
            }
            if (SampleRateCode < 3)
                sourcefscod = extractor.ReadBit();
        }
    }
}