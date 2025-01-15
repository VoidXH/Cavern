using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

using Cavern.Format.Utilities;

namespace CavernPipeServer {
    /// <summary>
    /// Handles the network communication of CavernPipe. A watchdog for a self-created named pipe called &quot;CavernPipe&quot;.
    /// </summary>
    public class PipeHandler : IDisposable {
        /// <summary>
        /// A thread that keeps the named pipe active in a loop - if the pipe was closed by a client and CavernPipe is still <see cref="running"/>,
        /// the named pipe is recreated, waiting for the next application to connect to CavernPipe.
        /// </summary>
        readonly Thread thread;

        /// <summary>
        /// Cancels waiting for a player/consumer when quitting the application.
        /// </summary>
        readonly CancellationTokenSource canceler = new CancellationTokenSource();

        /// <summary>
        /// Network endpoint instance.
        /// </summary>
        NamedPipeServerStream server;

        /// <summary>
        /// The network connection shall be kept alive.
        /// </summary>
        bool running = true;

        /// <summary>
        /// Handles the network communication of CavernPipe. A watchdog for a self-created named pipe called &quot;CavernPipe&quot;.
        /// </summary>
        public PipeHandler() {
            thread = new Thread(ThreadProc);
            thread.Start();
        }

        /// <summary>
        /// Stop keeping the named pipe alive.
        /// </summary>
        public void Dispose() {
            running = false;
            lock (server) {
                if (server != null) {
                    if (server.IsConnected) {
                        server.Close();
                    } else {
                        canceler.Cancel();
                    }
                }
            }
            thread.Join();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Watchdog for the CavernPipe named pipe. Allows a single instance of this named pipe to exist.
        /// </summary>
        async void ThreadProc() {
            while (running) {
                server = new NamedPipeServerStream("CavernPipe");
                try {
                    await server.WaitForConnectionAsync(canceler.Token);
                    // TODO: fix second connections
                    using CavernPipeRenderer renderer = new CavernPipeRenderer(server);
                    byte[] inBuffer = [],
                        outBuffer = [];
                    while (running) {
                        int length = server.ReadInt32();
                        if (inBuffer.Length < length) {
                            inBuffer = new byte[length];
                        }
                        ReadAll(inBuffer, length);
                        renderer.Input.Write(inBuffer, 0, length);

                        while (renderer.Output.Length < renderer.Protocol.MandatoryBytesToSend) {
                            // Wait until mandatory frames are rendered or pipe is closed
                        }
                        length = (int)renderer.Output.Length;
                        if (outBuffer.Length < length) {
                            outBuffer = new byte[length];
                        }
                        renderer.Output.Read(outBuffer, 0, length);
                        server.Write(BitConverter.GetBytes(length));
                        server.Write(outBuffer, 0, length);
                    }
                } catch { // Content type change or server/stream closed
                    if (server.IsConnected) {
                        server.Flush();
                    }
                }
                lock (server) {
                    server.Dispose();
                    server = null;
                }
            }
        }

        /// <summary>
        /// Read a specific number of bytes from the stream or throw an <see cref="EndOfStreamException"/> if it was closed midway.
        /// </summary>
        void ReadAll(byte[] buffer, int length) {
            int read = 0;
            while (read < length) {
                if (server.IsConnected) {
                    read += server.Read(buffer, read, length - read);
                } else {
                    throw new EndOfStreamException();
                }
            }
        }
    }
}