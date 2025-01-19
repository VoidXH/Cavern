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
pipeHeader[1] = 6; // Mandatory frames
BitConverter.GetBytes((ushort)target.ChannelCount).CopyTo(pipeHeader, 2);
BitConverter.GetBytes(listener.UpdateRate).CopyTo(pipeHeader, 4);
pipe.Write(pipeHeader, 0, pipeHeader.Length);

// Sending the file or part to the pipe
using FileStream reader = File.OpenRead(args[0]);
long sent = 0,
    received = 0;
float[] writeBuffer = [];
byte[] sendBuffer = new byte[1024 * 1024],
    receiveBuffer = [];
while (received < target.Length) {
    int toSend = reader.Read(sendBuffer, 0, sendBuffer.Length);
    pipe.Write(BitConverter.GetBytes(toSend));
    pipe.Write(sendBuffer, 0, toSend);
    sent += toSend;

    // If there is incoming data, write it to file
    int toReceive = pipe.ReadInt32(),
        samples = toReceive / sizeof(float);
    if (receiveBuffer.Length < toReceive) {
        receiveBuffer = new byte[toReceive];
        writeBuffer = new float[samples];
    }
    pipe.ReadAll(receiveBuffer, 0, toReceive);
    Buffer.BlockCopy(receiveBuffer, 0, writeBuffer, 0, toReceive);
    target.WriteBlock(writeBuffer, 0, samples);
    received += samples / target.ChannelCount;
}