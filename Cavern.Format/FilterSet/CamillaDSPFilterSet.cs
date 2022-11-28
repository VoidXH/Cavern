namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Convolution filter set for CamillaDSP.
    /// </summary>
    public class CamillaDSPFilterSet : FIRFilterSet {
        /// <summary>
        /// Convolution filter set for CamillaDSP.
        /// </summary>
        public CamillaDSPFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            throw new System.NotImplementedException();
        }
    }
}