# Cavern calibration process
Cavern's exact calibration process depends on the system it's used on. However,
using pink noise for frequency analysis is strongly discouraged, as the error
margin is too large. A full range frequency sweep for measurement, and peaking
filters instead of a multiband EQ should be used for calibration.

## Regular theatres and studios
These are the recommended settings for Cavern DCP creation and playback. The
center of the microphone placement should be at 2/3 of the room from the screen,
in the center. The room should be equalized to the X-curve (-3 dB/octave below
63 Hz and above 2 kHz), at the following peak sound pressure levels.

### Peak channel sound pressure levels (-20 dB FS)
In regular theatres, Cavern-ready systems, even with custom layouts, must match
the standard levels.

| Channel                                             | Level       |
|-----------------------------------------------------|:-----------:|
| Screen channels (max. 45° from center on both axes) | 85 dB       |
| Low frequency effect channels                       | 95 dB       |
| All other channels                                  | 82 dB       |

## Cavern theatres and studios
For Cavern-only content creation and playback, the channels can only be
calibrated by an at least 1/12 octave EQ or peaking filters, based off a
frequency sweep reading with at least 7 microphones positioned around the
reference listening position, to a flat response with the error margin of 0.2 dB
between 100 Hz and 16 kHz (25 to 120 Hz for LFE channels). The use of Cavern
QuickEQ for measurement and configuration file generation is hardly recommended.

### Peak channel sound pressure levels (-20 dB FS)
For a Cavern theatre, each individual channel must be capable of outputting 105
dB SPL at the center of calibration. The channel array peak is also the same, it
must be enforced in the content creation process by the authoring tools.

| Channel                                             | Level       |
|-----------------------------------------------------|:-----------:|
| Full range channels                                 | 85 dB       |
| Low frequency effect channels                       | 95 dB       |