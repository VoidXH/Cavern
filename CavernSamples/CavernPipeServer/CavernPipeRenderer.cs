using System;
using System.IO;
using System.Threading.Tasks;

using Cavern;
using Cavern.Format;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;

namespace CavernPipeServer {
    /// <summary>
    /// Handles rendering of incoming audio content and special protocol additions/transformations.
    /// </summary>
    public class CavernPipeRenderer : IDisposable {
        /// <summary>
        /// Protocol message decoder.
        /// </summary>
        public CavernPipeProtocol Protocol { get; private set; }

        /// <summary>
        /// Bytes to be processed.
        /// </summary>
        public QueueStream Input { get; private set; } = new();

        /// <summary>
        /// Bytes to send to the client.
        /// </summary>
        public QueueStream Output { get; private set; } = new();

        /// <summary>
        /// Handles rendering of incoming audio content and special protocol additions/transformations.
        /// </summary>
        public CavernPipeRenderer(Stream stream) {
            Protocol = new CavernPipeProtocol(stream);
            Task.Run(RenderThread);
        }

        /// <inheritdoc/>
        public void Dispose() {
            lock (Protocol) {
                Input?.Dispose();
                Output?.Dispose();
            }
            Input = null;
            Output = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wait for enough input stream data and render the next set of samples, of which the count will be <see cref="Listener.UpdateRate"/> per channel.
        /// </summary>
        void RenderThread() {
            try {
                EnhancedAC3Reader reader = new EnhancedAC3Reader(Input); // Currently E-AC-3 only, AudioReader.Open doesn't work without seek
                Renderer renderer = reader.GetRenderer();
                Listener listener = new Listener {
                    SampleRate = reader.SampleRate,
                    UpdateRate = Protocol.UpdateRate
                };
                listener.AttachSources(renderer.Objects);

                // When this writer is used without writing a header, it's a BitDepth converter from float to anything, and can dump to streams.
                RIFFWaveWriter streamDumper = new RIFFWaveWriter(Output, Protocol.OutputChannels, long.MaxValue, reader.SampleRate, Protocol.OutputFormat);

                while (Input != null) {
                    float[] render = listener.Render();
                    streamDumper.WriteBlock(render, 0, render.LongLength);
                }
            } catch {
                Dispose();
            }
        }
    }
}