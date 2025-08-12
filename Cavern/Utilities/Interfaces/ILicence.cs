namespace Cavern.Utilities {
    /// <summary>
    /// Provides a way to accept a licence before using a feature that requires it.
    /// </summary>
    public interface ILicence {
        /// <summary>
        /// Display a text before the licence text, explaining why this software is needed.
        /// </summary>
        public void SetDescription(string description);

        /// <summary>
        /// Provide the agreement text the user must accept.
        /// </summary>
        public void SetLicenceText(string licence);

        /// <summary>
        /// Show the licence and prompt the user to accept it.
        /// </summary>
        /// <returns>If the licence was accepted.</returns>
        public bool Prompt();
    }
}
