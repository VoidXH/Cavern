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
this case will contain flags for which channels the PCM track encodes position
values for. Alignment of the flags is done the same way as for the active
channels at the beginning of each PCM block. Since there are only 32 bits
available, this means a single PCM track can only contain positional information
for 32 channels at most. The next such channel would contain data for the next
32 PCM channel and so on.

The bit depth determines the total data available for object movement. A single
16-bit PCM channel can provide 3D position updates for all objects every `3
coordinates * 32 bits per float coordinate / 16 bit sample rate * 32 objects =
192 samples`, which means a 250 Hz update rate at 48 kHz. When the content is
24 bit, the sample interval is 128, meaning a 375 Hz update rate. Linear
interpolation should be applied for position changes, and instant position
changes should be handled by adding an additional track, since the format can
handle practically infinite objects.
