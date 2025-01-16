using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

using Cavern.Format.Utilities;

using CavernPipeServer.Consts;

namespace CavernPipeServer {
    /// <summary>
    /// Handles the network communication of CavernPipe. A watchdog for a self-created named pipe called &quot;CavernPipe&quot;.
    /// </summary>
    public class PipeHandler : IDisposable {
        /// <summary>
        /// The network connection is kept alive.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// Used for providing thread safety.
        /// </summary>
        readonly object locker = new object();

        /// <summary>
        /// A thread that keeps the named pipe active in a loop - if the pipe was closed by a client and CavernPipe is still <see cref="Running"/>,
        /// the named pipe is recreated, waiting for the next application to connect to CavernPipe.
        /// </summary>
        Thread thread;

        /// <summary>
        /// Cancels waiting for a player/consumer when quitting the application.
        /// </summary>
        CancellationTokenSource canceler;

        /// <summary>
        /// Network endpoint instance.
        /// </summary>
        NamedPipeServerStream server;

        /// <summary>
        /// Handles the network communication of CavernPipe. A watchdog for a self-created named pipe called &quot;CavernPipe&quot;.
        /// </summary>
        public PipeHandler() => Start();

        /// <summary>
        /// Start the named pipe watchdog. If it's already running, an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        public void Start() {
            lock (locker) {
                if (Running) {
                    throw new InvalidOperationException((string)Language.GetMainWindowStrings()[server == null ? "EServ" : "ERest"]);
                }
                canceler = new CancellationTokenSource();
                thread = new Thread(ThreadProc);
                thread.Start();
                Running = true;
            }
        }

        /// <summary>
        /// Stop keeping the named pipe alive.
        /// </summary>
        public void Dispose() {
            lock (locker) {
                Running = false;
                if (server != null) {
                    if (server.IsConnected) {
                        server.Close();
                    } else {
                        canceler.Cancel();
                    }
                }
            }
            thread.Join();
            canceler.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Watchdog for the CavernPipe named pipe. Allows a single instance of this named pipe to exist.
        /// </summary>
        async void ThreadProc() {
            while (Running) {
                try {
                    TryStartServer();
                    await server.WaitForConnectionAsync(canceler.Token);
                    using CavernPipeRenderer renderer = new CavernPipeRenderer(server);
                    byte[] inBuffer = [],
                        outBuffer = [];
                    while (Running) {
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
                } catch (TimeoutException) {
                    Language.ShowError((string)Language.GetMainWindowStrings()["EPipe"]);
                    return;
                } catch { // Content type change or server/stream closed
                    if (server.IsConnected) {
                        server.Flush();
                    }
                }
                lock (locker) {
                    server.Dispose();
                    server = null;
                }
            }
        }

        /// <summary>
        /// Try to open the CavernPipe and assign the <see cref="server"/> variable if it was successful. If not, the thread stops,
        /// and the user gets a message that it should be restarted.
        /// </summary>
        void TryStartServer() {
            DateTime tryUntil = DateTime.Now + TimeSpan.FromSeconds(3);
            while (DateTime.Now < tryUntil) {
                try {
                    server = new NamedPipeServerStream("CavernPipe");
                    return;
                } catch {
                    server = null;
                }
            }
            Running = false;
            throw new TimeoutException();
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