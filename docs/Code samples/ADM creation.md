```cs
// Create a rendering environment with a single audio source
// that's playing a second of sine wave
Listener listener = new();
float[] sine = new float[listener.SampleRate];
float mul = 2 * MathF.PI * 1000 / listener.SampleRate;
for (int i = 0; i < sine.Length; i++) {
    sine[i] = MathF.Sin(mul * i);
}
Source source = new() {
    Clip = new Clip(sine, 1, listener.SampleRate),
    Loop = true
};
listener.AttachSource(source);

// Create a 10 second ADM BWF file
long length = 10 * listener.SampleRate;
BroadcastWaveFormatWriter writer =
    new BroadcastWaveFormatWriter("test.wav", listener, length, BitDepth.Int16);

// To contain object movement in the file, it has to be written frame-by-frame
long progress = 0;
while (progress < length) {
    // Example circular object motion
    float t = 2 * MathF.PI * progress / listener.SampleRate;
    Vector3 referencePos = new Vector3(MathF.Sin(t), MathF.Sin(t * .1f), MathF.Cos(t));
    // All positions will be written relative to the environment size
    source.Position = referencePos * Listener.EnvironmentSize;
    // WriteNextFrame will update the Listener, you don't have to
    writer.WriteNextFrame();

    progress += listener.UpdateRate;
}
writer.Dispose(); // Disposal will append the object movement to the WAV file
```
