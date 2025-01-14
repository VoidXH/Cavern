using System;
using System.IO.Pipes;
using System.Threading;

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
                    CavernPipeRenderer renderer = new CavernPipeRenderer(server);
                    while (running) {
                        byte[] data = renderer.Render();
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
    }
}