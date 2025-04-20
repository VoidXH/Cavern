﻿using Cavern.Format;
using Cavern.Format.Decoders;
using Cavern.Format.Utilities;

namespace CavernizeGUI.CommandLine.HiddenCommands {
    /// <summary>
    /// Converts an Enhanced AC-3 bitstream to raw samples, replaces the PCM data with external.
    /// </summary>
    /// <param name="reader">Accesses the linear E-AC-3 bitstream</param>
    /// <param name="fileSize">Length of the E-AC-3 bitstream</param>
    /// <param name="overrider">Stream to override the PCM data with - only applies to the source PCM data,
    /// not the JOC-decoded objects</param>
    class OverrideBedDecoder(BlockBuffer<byte> reader, long fileSize, AudioReader overrider) : EnhancedAC3Decoder(reader, fileSize) {
        /// <inheritdoc/>
        protected override float[] DecodeFrame() {
            float[] result = base.DecodeFrame();
            overrider?.ReadBlock(result, 0, result.Length);
            return result;
        }
    }
}