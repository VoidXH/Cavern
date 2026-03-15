using System.Collections.Concurrent;
using System.IO.Pipes;

using Cavern;
using Cavern.Format;
using Cavern.Format.Utilities;

if (args.Length < 2) {
    Console.WriteLine("Usage: CavernPipeClient.exe <input file name> <output file name>");
    return;
}

Listener listener = new();
AudioReader source = AudioReader.Open(args[0]);
source.ReadHeader();
using AudioWriter target = AudioWriter.Create(args[1], Listener.Channels.Length, source.Length, source.SampleRate, BitDepth.Float32);
source.Dispose();
target.WriteHeader();

// Connection
using NamedPipeClientStream pipe = new("CavernPipe");
pipe.Connect();
byte[] pipeHeader = new byte[8]; // Assembled CavernPipe control header
pipeHeader[0] = (byte)target.Bits;
pipeHeader[1] = 1; // Mandatory frames
BitConverter.GetBytes((ushort)target.ChannelCount).CopyTo(pipeHeader, 2);
BitConverter.GetBytes(listener.UpdateRate).CopyTo(pipeHeader, 4);
pipe.Write(pipeHeader, 0, pipeHeader.Length);

ConcurrentQueue<byte[]> toProcess = new();
CancellationTokenSource canceller = new();
Task writer = new(WriterThread);
Task reader = new(ReadingThread, canceller.Token);
writer.Start();
reader.Start();
writer.Wait();
canceller.Cancel();
reader.Wait();

void WriterThread() {
    // Sending the file or part to the pipe
    using FileStream reader = File.OpenRead(args[0]);
    long sent = 0,
        received = 0;
    bool eofSent = false;
    byte[] sendBuffer = new byte[1024 * 1024];
    while (received < target.Length) {
        int toSend = reader.Read(sendBuffer, 0, sendBuffer.Length);
        if (toSend > 0) {
            pipe.Write(BitConverter.GetBytes(toSend));
            pipe.Write(sendBuffer, 0, toSend);
            sent += toSend;
        } else if (!eofSent) { // Disconnect on EOF
            pipe.Write(BitConverter.GetBytes(-1));
            eofSent = true;
        }

        // If there is incoming data, add to the processing buffer
        int toReceive = pipe.ReadInt32();
        if (toReceive == -1) { // Pipe closed, because EOS was processed
            break;
        }

        byte[] receiveBuffer = new byte[toReceive];
        pipe.ReadAll(receiveBuffer, 0, toReceive);
        received += toReceive / (sizeof(float) * target.ChannelCount);
    }
    pipe.Close();
}

void ReadingThread(object param) {
    CancellationToken token = (CancellationToken)param;
    while (!toProcess.IsEmpty || !token.IsCancellationRequested) {
        if (!toProcess.TryDequeue(out byte[] block)) {
            continue;
        }
        int samples = block.Length / sizeof(float);
        float[] writeBuffer = new float[samples];
        Buffer.BlockCopy(block, 0, writeBuffer, 0, block.Length);
        target.WriteBlock(writeBuffer, 0, samples);
    }
    target.Dispose();
}
