using System;
using System.Text;

using Cavern.Format.Common;

namespace Cavern.Format.Networking {
    /// <summary>
    /// Represents a Session Description Protocol (SDP) message for audio streams.
    /// Parses and generates SDP bodies containing session metadata, codec info, and network details.
    /// </summary>
    public class SDPPacket {
        /// <summary>
        /// Gets or sets the session name.
        /// </summary>
        public string SessionName { get; set; } = "Cavern";

        /// <summary>
        /// Gets or sets the originator name.
        /// </summary>
        public string Originator { get; set; } = "-";

        /// <summary>
        /// Gets or sets the session unique identifier.
        /// </summary>
        public long SessionID { get; set; }

        /// <summary>
        /// Gets or sets the session version, incremented on each modification.
        /// </summary>
        public long SessionVersion { get; set; }

        /// <summary>
        /// Gets or sets the multicast IP address for the audio stream.
        /// </summary>
        public string MulticastAddress { get; set; } = "239.69.0.1";

        /// <summary>
        /// Gets or sets the UDP port for the RTP stream.
        /// </summary>
        public int Port { get; set; } = 5004;

        /// <summary>
        /// Gets or sets the RTP payload type. Defaults to 97 (dynamic).
        /// </summary>
        public int PayloadType { get; set; } = 97;

        /// <summary>
        /// Condec of the audio stream.
        /// </summary>
        public Codec Codec { get; set; } = Codec.PCM_LE;

        /// <summary>
        /// Gets or sets stream bit depth for codecs that support multiple (e.g. <see cref="Codec.PCM_LE"/>).
        /// </summary>
        public BitDepth BitDepth { get; set; } = BitDepth.Int24;

        /// <summary>
        /// Gets or sets the sample rate in Hz.
        /// </summary>
        public int SampleRate { get; set; } = 48000;

        /// <summary>
        /// Gets or sets the number of audio channels.
        /// </summary>
        public int Channels { get; set; } = 2;

        /// <summary>
        /// Gets or sets the packet time in milliseconds.
        /// </summary>
        public int PacketTime { get; set; } = 1;

        /// <summary>
        /// Gets or sets the reference clock source type (e.g., "ptp", "localmac").
        /// </summary>
        public string ClockSource { get; set; } = "ptp";

        /// <summary>
        /// Gets or sets the PTP grandmaster clock identity (GMID) in EUI-64 format.
        /// Defaults to "00-00-00-00-00-00-00-00".
        /// </summary>
        public string PTP_GMID { get; set; } = "00-00-00-00-00-00-00-00";

        /// <summary>
        /// Gets or sets the PTP domain number.
        /// </summary>
        public int PTPDomain { get; set; }

        /// <summary>
        /// Gets or sets the media clock offset relative to the reference clock.
        /// </summary>
        public long MediaClockOffset { get; set; }

        /// <summary>
        /// Parses an SDP string into an <see cref="SDPPacket"/> instance.
        /// </summary>
        /// <param name="sdp">The SDP body as a string.</param>
        /// <returns>A populated <see cref="SDPPacket"/> instance.</returns>
        public static SDPPacket Parse(string sdp) {
            SDPPacket message = new SDPPacket();
            string[] lines = sdp.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines) {
                if (line.StartsWith("s=")) {
                    message.SessionName = line[2..];
                } else if (line.StartsWith("o=")) {
                    string[] parts = line[2..].Split(' ');
                    if (parts.Length >= 3) {
                        message.Originator = parts[0];
                        if (long.TryParse(parts[1], out long sId)) {
                            message.SessionID = sId;
                        }
                        if (long.TryParse(parts[2], out long sVer)) {
                            message.SessionVersion = sVer;
                        }
                    }
                } else if (line.StartsWith("c=")) {
                    string[] parts = line[2..].Split(new[] { " ", "/" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3) {
                        message.MulticastAddress = parts[2];
                    }
                } else if (line.StartsWith("m=audio")) {
                    string[] parts = line[2..].Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int port)) {
                        message.Port = port;
                    }
                    if (parts.Length >= 4 && int.TryParse(parts[3], out int pt)) {
                        message.PayloadType = pt;
                    }
                } else if (line.StartsWith("a=rtpmap:")) {
                    string val = line[9..];
                    int spaceIdx = val.IndexOf(' ');
                    if (spaceIdx > 0) {
                        string[] codecParts = val[(spaceIdx + 1)..].Split('/');
                        if (codecParts.Length >= 1) {
                            message.Codec = SDPCodeMapper.GetCodec(codecParts[0]);
                            message.BitDepth = SDPCodeMapper.TryGetPcmBitDepth(codecParts[0], out int bitDepth) ? (BitDepth)bitDepth : BitDepth.Float32;
                        }
                        if (codecParts.Length >= 2 && int.TryParse(codecParts[1], out int sr)) {
                            message.SampleRate = sr;
                        }
                        if (codecParts.Length >= 3 && int.TryParse(codecParts[2], out int ch)) {
                            message.Channels = ch;
                        }
                    }
                } else if (line.StartsWith("a=ptime:")) {
                    if (int.TryParse(line[8..], out int ptime)) {
                        message.PacketTime = ptime;
                    }
                } else if (line.StartsWith("a=ts-refclk:")) {
                    string refclk = line[12..];
                    if (refclk.StartsWith("ptp=IEEE1588-2008:")) {
                        message.ClockSource = "ptp";
                        string ptpParams = refclk[18..];
                        string[] parts = ptpParams.Split(':');
                        if (parts.Length >= 1) {
                            message.PTP_GMID = parts[0];
                        }
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int domain)) {
                            message.PTPDomain = domain;
                        }
                    } else if (refclk.StartsWith("localmac")) {
                        message.ClockSource = "localmac";
                    }
                } else if (line.StartsWith("a=mediaclk:")) {
                    string mediaclk = line[11..];
                    if (mediaclk.StartsWith("direct=")) {
                        string offsetPart = mediaclk[7..].Split(' ')[0];
                        if (long.TryParse(offsetPart, out long offset)) {
                            message.MediaClockOffset = offset;
                        }
                    }
                }
            }
            return message;
        }

        /// <summary>
        /// Generates the SDP body string from the current property values.
        /// </summary>
        /// <returns>A formatted SDP body string.</returns>
        public override string ToString() {
            StringBuilder result = new StringBuilder();
            result.AppendLine("v=0");
            result.AppendLine($"o={Originator} {SessionID} {SessionVersion} IN IP4 {MulticastAddress}");
            result.AppendLine($"s={SessionName}");
            result.AppendLine($"c=IN IP4 {MulticastAddress}/32");
            result.AppendLine("t=0 0");
            result.AppendLine($"m=audio {Port} RTP/AVP {PayloadType}");
            result.AppendLine($"a=rtpmap:{PayloadType} {SDPCodeMapper.GetSDPName(Codec, BitDepth)}/{SampleRate}/{Channels}");
            result.AppendLine($"a=ptime:{PacketTime}");
            if (ClockSource == "ptp") {
                result.AppendLine($"a=ts-refclk:ptp=IEEE1588-2008:{PTP_GMID}:{PTPDomain}");
            } else {
                result.AppendLine($"a=ts-refclk:{ClockSource}");
            }
            result.AppendLine($"a=mediaclk:direct={MediaClockOffset}");
            return result.ToString();
        }
    }
}
