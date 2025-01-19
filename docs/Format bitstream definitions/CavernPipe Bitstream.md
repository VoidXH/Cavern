# CavernPipe Bitstream Structure
CavernPipe is a solution for inter-process rendering of any supported bitstream
with named pipes. When the user has CavernPipe installed and it's running, the
CavernPipe named pipe is available for a single consuming process. The protocol
of CavernPipe is very minimal and easy to implement.

## Initial setup
The user can configure the system layout in the Cavern Driver, downloadable from
the [Cavern website](https://cavern.sbence.hu). The playback software only needs
to know the channel count of the user's layout to be able to provide correct
output, everything else will be handled by CavernPipe. The channel count is the
first line in `%appdata%\Cavern\Save.dat`. If this file doesn't exist, it's 6.

## Handshake
After connection, the client has to define the format in which it expects the
rendered data. These are the 8 bytes required before rendering can begin.
|------|-------|------|
| Byte | Type  | Data |
|------|-------|
| 0    | Byte  | [Bit depth](https://cavern.sbence.hu/cavern/doc.php?if=api/Cavern/Format/BitDepth/index) |
| 1    | Byte  | Mandatory frames to process. Before CavernPipe replies, it will render at least `mandatory frames * update rate * channel count` samples. If there aren't enough data sent to the server to render that much, a deadlock happens. |
| 2-3  | Int16 | Output channel count, the number of available system output channels. If it doesn't match with the user's actual channel count, no problem, CavernPipe will handle it, but _no rendering or mapping shall happen in any media player with Cavern-rendered samples_. |
| 4-7  | Int32 | Update rate: samples per channel for each rendered frame. |
|------|-------|------|

### Proper E-AC-3 handshake
For optimal rendering of Enhanced AC-3, the update rate shall be 64 samples. A
single frame of E-AC-3 data is always 1536 samples, so if you'd like to render
E-AC-3 + JOC with Cavern, set the mandatory frames to 24, and the update rate to
64. This is the recommended rendering detail in the JOC specification. If you
know the frame border, just send a single frame and wait for the reply just
before that frame should be played.

CavernPipe also supports caching, multiple frames can be sent in advance, which
will be rendered in the background, and replies will always contain at least the
mandatory frames asked for. Excess samples have to be cached client-side. On
seek, break the connection and reconnect to CavernPipe, that clears the cache.

## Rendering
Communication after the handshake is very straightforward: send a single 32-bit
integer, which is the number of bytes in the available bitstream data, then send
the available bitstream. It can be more than what's initially needed as defined
in the mandatory frames, but in this case, it's very likely that they will only
get rendered in the next reply. If data is cached, sending a single 0 integer
will send back every currently processed data, at least the mandatory frames.
This is how to flush the CavernPipe. Warning: if there isn't enough data for the
mandatory frames in the cache, a deadlock happens.

The result of CavernPipe will follow the same format: a 32-bit integer arrives
with the data length, and the bytes of the rendered interlaced PCM stream will
follow. If the bit depth matches the system output, a simple buffer copy is
enough to provide the correct output.
