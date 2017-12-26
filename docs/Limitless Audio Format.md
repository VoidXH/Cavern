# Limitless Audio Format Structure
* 9 bytes: "LIMITLESS" marker
* Custom headers with their own marker. The "TAGS" marker is reserved for tags in a later specification.
* 4 bytes: "HEAD" marker
* 1 byte: quality
    * 0 - Low (8-bit integer)
    * 1 - Medium (16-bit integer)
    * 2 - High (32-bit floating point)
    * *Cavern does not support 24-bit integer exports for performance and distortion elimination.*
* 1 byte: mode
    * 0 - Channel mode: individual spatial channels are exported, playback requires the same specific setup, but no processing is needed.
    * 1 - Object mode: individual objects are exported to be mixed later to any speaker setup.
    * *Can be ignored for symmetric layouts.*
    * *The Cavern driver exports in channel mode.*
* 4 bytes: channel/object count
* 9 bytes for each channel/object
    * Rotation on X axis (32-bit floating point)
    * Rotation on Y axis (32-bit floating point)
    * Low frequency (1 byte boolean, 0 if false)
* 4 bytes: sample rate
* 8 bytes: total samples
* Blocks for each second
    * Each block begins with channels/objects / 8 (rounded up) bytes: each bit in this byte array shows if a channel has samples or not in the current block. Bits are high-ordered and right-aligned, while bytes are low-ordered (for 12 channels: 8-7-6-5-4-3-2-1, ----12-11-10-9). After these, the samples follow for each channel which was declared active in the previous byte array, frame by frame, each frame containing a sample for each active channel, in ascending channel order.