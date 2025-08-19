using CavernPipeServer.Logic;

Console.WriteLine("Launching CavernPipe Server...");

bool running = true;
while (running) {
    using PipeHandler handler = new();
    handler.OnException += e => {
        if (!e.StackTrace.Contains("QueueStream")) { // That would be a normal disconnection
            Console.Error.WriteLine(e.Message);
        }
        running = false;
    };

    Console.WriteLine("CavernPipe Server is waiting for client connection.");
    bool canShutDown = false;
    handler.StatusChanged += () => {
        if (handler.IsConnected) {
            Console.WriteLine("Client connected.");
        }
        canShutDown = !handler.Running;
    };

    while (!canShutDown) {
        Thread.Sleep(1000);
    }
}
