# Convolution Box Format Structure
A file format for easy to implement DSP, only using channel copy and convolution
filters. Channels are numbered from 0 to the number of system channels. Negative
channel indices mean virtual channels, of which any number can be created.
All values are little endian.
* 4 bytes: "CBFM" marker
* 4 bytes: system sample rate, used for all convolutions
* 4 bytes: number of filter entries

For each filter entry:
* 1 byte: filter type
    * 0 - Copy (matrix mixer) filter:
        * 4 bytes: number of copy operations
        * For each copy entry:
            * 4 bytes: source channel index
            * 4 bytes: number of target channels
            * Serially: 4 byte indices of all target channels
        * Merging of channels is allowed, but it is described in two distinct
          copy entries.
    * 1 - Convolution filter:
        * 4 bytes: index of the affected channel
        * 4 bytes: length of the convolution in samples (must be a power of 2)
        * Serially: single precision floating point samples of the filter
