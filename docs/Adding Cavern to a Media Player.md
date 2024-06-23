# Adding Cavern to a Media Player
Integrating Cavern into an application to replace the channel-based downmixes
with an actual spatial mix is easy. It requires wrapping the audio bitstream in
a .NET `Stream`, initializing the Cavern renderer, and attaching the audio
track.

## Stream Wrapping
Cavern supports two sources for encoded audio data: loading from a file or
reading from a `Stream`. For real-time applications, `Stream`s are preferred.
The easiest way to pass this from your own container is to write the bitstream
to a `MemoryStream`, which can then be opened by Cavern with the following:
```
AudioReader track = AudioReader.Open(stream);
```
Cavern will detect the codec automatically if it's supported. The stream should
always contain at least one frame in advance, as Cavern reads ahead for the next
header to check for modifying subframes or changed bitrate.

## Creating a Listener Environment
Initialize a listening environment with an update rate of the content, which is
1536 samples for an E-AC-3 track:
```
Listener listener = new() {
    SampleRate = track.SampleRate,
    UpdateRate = 1536
};
```
The listening environment shall be set up by the user in Cavern Driver, and this
is to be respected. If you plan on creating your own channel setup screen, refer
to the main README.

## Attach the Track to the Listener
Each track has a `Renderer` that matches in-track objects or channels to Cavern
`Source`s. These `Source`s are self-updating when attached to a `Listener` and
that `Listener` is updated. First, attach all `Source`s:
```
listener.AttachSources(track.GetRenderer().Objects);
```

## Output
With this setup, every call to `listener.Render();` will process the next frame
in the provided `Stream`. It returns interlaced samples for all the user's
channels in the order as it's set up in `Listener.Channels`. Each channel will
contain as many samples as the `UpdateRate` of the listener.

To support seeking, many formats support if the frame sought to is provided to
the `Stream` as all frames contain a valid header. This is also the case for
E-AC-3. When switching tracks of different types, the `Listener` doesn't have to
be recreated, just use `listener.DetachAllSources();` to remove the previous
track and attach the new.
