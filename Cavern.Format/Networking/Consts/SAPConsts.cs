namespace Cavern.Format.Networking.Consts {
    /// <summary>
    /// Constant values for the Session Announcement Protocol.
    /// </summary>
    public static class SAPConsts {
        /// <summary>
        /// The multicast address for SAP announcements (RFC 2974).
        /// </summary>
        public static readonly string multicastAddress = "239.255.255.255";

        /// <summary>
        /// The UDP port for SAP announcements.
        /// </summary>
        public static readonly int port = 9875;
    }
}
