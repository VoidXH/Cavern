using System;
using System.IO;
using System.Threading.Tasks;

using Cavern;
using Cavern.Format;
using Cavern.Format.Renderers;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace CavernPipeServer {
    /// <summary>
    /// Handles rendering of incoming audio content and special protocol additions/transformations.
    /// </summary>
    public class CavernPipeRenderer : IDisposable {
        /// <summary>
        /// Rendering of new content has started, the <see cref="Listener.Channels"/> are updated from the latest Cavern user files.
        /// </summary>
        public event Action OnRenderingStarted;

        /// <summary>
        /// Provides per-channel metering data. Channel gains are ratios between -50 and 0 dB FS.
        /// </summary>
        public delegate void OnMetersAvailable(float[] meters);

        /// <summary>
        /// New output data was rendered, audio meters can be updated. Channel gains are ratios between -50 and 0 dB FS.
        /// </summary>
        public event OnMetersAvailable MetersAvailable;

        /// <summary>
        /// Exceptions coming from the rendering thread are passed down from this event.
        /// </summary>
        public event Action<Exception> OnException;

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
                AudioReader reader = AudioReader.Open(Input);
                Renderer renderer = reader.GetRenderer();
                Listener listener = new Listener {
                    SampleRate = reader.SampleRate,
                    UpdateRate = Protocol.UpdateRate,
                    AudioQuality = QualityModes.Perfect,
                };
                OnRenderingStarted?.Invoke();

                float[] reRender = null;
                if (Listener.Channels.Length != Protocol.OutputChannels) {
                    reRender = new float[Protocol.OutputChannels * Protocol.UpdateRate];
                }
                listener.AttachSources(renderer.Objects);

                // When this writer is used without writing a header, it's a BitDepth converter from float to anything, and can dump to streams.
                RIFFWaveWriter streamDumper = new RIFFWaveWriter(Output, Protocol.OutputChannels, long.MaxValue, reader.SampleRate, Protocol.OutputFormat);

                while (Input != null) {
                    float[] render = listener.Render();
                    UpdateMeters(render);
                    if (reRender == null) {
                        streamDumper.WriteBlock(render, 0, render.LongLength);
                    } else {
                        Array.Clear(reRender);
                        WaveformUtils.Downmix(render, reRender, Protocol.OutputChannels);
                        streamDumper.WriteBlock(reRender, 0, reRender.LongLength);
                    }
                }
            } catch (Exception e) {
                Dispose();
                OnException?.Invoke(e);
            }
        }

        /// <summary>
        /// Send the <see cref="MetersAvailable"/> event with the rendered channel names and their last rendered gains.
        /// </summary>
        /// <remarks>Channel gains are ratios between -50 and 0 dB FS.</remarks>
        void UpdateMeters(float[] audioOut) {
            float[] result = new float[Listener.Channels.Length];
            for (int i = 0; i < result.Length; i++) {
                float channelGain = QMath.GainToDb(WaveformUtils.GetRMS(audioOut, i, result.Length));
                result[i] = QMath.Clamp01(QMath.LerpInverse(-50, 0, channelGain));
            }
            MetersAvailable?.Invoke(result);
        }
    }
}