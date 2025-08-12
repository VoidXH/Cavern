namespace Cavernize.Logic.Language {
    /// <summary>
    /// Strings for the messages of external renderers.
    /// </summary>
    public class ExternalConverterStrings {
        /// <summary>
        /// Cavernize uses {0} for {1} conversions. It will be downloaded automatically, but you need to accept its licence agreement first.
        /// </summary>
        public virtual string LicenceNeeded =>
            "Cavernize uses {0} for {1} conversions. It will be downloaded automatically, but you need to accept its licence agreement first.";

        /// <summary>
        /// Fetching licence...
        /// </summary>
        public virtual string LicenceFetch => "Fetching licence...";

        /// <summary>
        /// Failed to fetch licence.
        /// </summary>
        public virtual string LicenceFail => "Failed to fetch licence.";

        /// <summary>
        /// Waiting for user approval...
        /// </summary>
        public virtual string WaitingUserAccept => "Waiting for user approval...";

        /// <summary>
        /// The licence was not accepted.
        /// </summary>
        public virtual string UserCancelled => "The licence was not accepted.";

        /// <summary>
        /// Downloading {0}...
        /// </summary>
        public virtual string Downloading => "Downloading {0}...";

        /// <summary>
        /// Extracting {0}...
        /// </summary>
        public virtual string Extracting => "Extracting {0}...";

        /// <summary>
        /// Extracting raw bitstream...
        /// </summary>
        public virtual string ExtractingBitstream => "Extracting raw bitstream...";

        /// <summary>
        /// Converting with {0}...
        /// </summary>
        public virtual string Converting => "Converting with {0}...";

        /// <summary>
        /// Downloading failed because of a network error.
        /// </summary>
        public virtual string NetworkError => "Downloading failed because of a network error.";
    }
}
