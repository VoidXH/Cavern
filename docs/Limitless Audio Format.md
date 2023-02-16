# Limitless Audio Format Structure
* 9 bytes: "LIMITLESS" marker
* Custom headers with their own markers. The "TAGS" marker is reserved for tags in a later specification.
* 4 bytes: "HEAD" marker
* 1 byte: quality
    * 0 - Low (8-bit integer)
    * 1 - Medium (16-bit integer)
    * 2 - High float (32-bit floating point)
    * 3 - High int (24-bit integer)
* 1 byte: mode
    * 0 - Channel mode: individual spatial channels are exported with no object movement.
    * 1 - Object mode: individual objects are exported with movement data embedded in the PCM blocks.
* 4 bytes: channel/object count
* 9 bytes for each channel/object
    * Rotation on X axis (32-bit floating point)
    * Rotation on Y axis (32-bit floating point)
    * Low frequency (1 byte boolean, 0 if false)
* 4 bytes: sample rate
* 8 bytes: total samples
* PCM audio blocks for each second

## PCM blocks
A PCM block is created for each second of audio. Each block begins with
`channels/objects / 8` (rounded up) bytes: each bit in this byte array shows if
a channel has samples or not in the current block. Bits are high-ordered and
right-aligned, while bytes are low-ordered (for 12 channels: 8-7-6-5-4-3-2-1,
----12-11-10-9). After these, the samples follow for each channel which was
declared active in the previous byte array, channel by channel for each sample
(interlaced, the same way as it's done for RIFF WAVE), in ascending channel
order, but only for active channels.

## Modes
### Channel mode
In Channel mode, no checks are required: all channel positions are valid if the
transport is valid, and no additional tracks are inserted, everything can be
played back.

### Object mode
For object-based exports, extra PCM tracks are used for object movement data.
These tracks are marked with the X axis rotation set to NaN. The Y rotation in
this case is reserved and shall be 0. One object position track contains
movement data for exactly 16 channels. If there are more channels, more object
position tracks are added, but if there are less channels left for the last
track, it will fill the remaining channels with skipped data, but the alignment
must match with a 16-channel position track. The positions follow each other by
their index, and the order of the dimensions is width, height, depth.

Position tracks contain the values dimension by dimension, object by object
(object 1 X, object 1 Y, object 1 Z, object 2 X...). LAF is using a right-handed
Cartesian coordinate system. The bit depth of the content sets the precision of
object positions, these values are encoded the exact same way PCM samples are:
from -1 to 1 (these limits are the walls of the virtual room), multiplied by the
positive integer limit in integer mode. In floating point mode, placing objects
over the room's walls is permitted.
