#include <iostream> // int32_t without errors
#include "AudioChannel.h"
#include "Enums.h"

AudioChannel* CreateChannel(float x, float y, bool lfe) {
    return new AudioChannel(x, y, lfe);
}

AudioChannel** DefaultChannelSet(int32_t channelCount) {
    AudioChannel** channels = new AudioChannel*[channelCount];
    switch(channelCount) {
    case 1: // 1.0: mono
        channels[0] = CreateChannel(0, 0, false);
        break;
    case 2: // 2.0: stereo
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        break;
    case 3: // 3.0: stereo + center
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        break;
    case 4: // 4.0: quadro
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, -110, false);
        channels[3] = CreateChannel(0, 110, false);
        break;
    case 5: // 5.0 surround
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, -110, false);
        channels[4] = CreateChannel(0, 110, false);
        break;
    case 6: // 5.1 surround
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -110, false);
        channels[5] = CreateChannel(0, 110, false);
        break;
    case 7: // 6.1 surround
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -110, false);
        channels[5] = CreateChannel(0, 110, false);
        channels[6] = CreateChannel(0, 180, false);
        break;
    case 8: // 7.1 surround
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -150, false);
        channels[5] = CreateChannel(0, 150, false);
        channels[6] = CreateChannel(0, -110, false);
        channels[7] = CreateChannel(0, 110, false);
        break;
    case 9: // 8.1 surround (non-standard)
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -150, false);
        channels[5] = CreateChannel(0, 150, false);
        channels[6] = CreateChannel(0, -110, false);
        channels[7] = CreateChannel(0, 110, false);
        channels[8] = CreateChannel(0, 180, false);
        break;
    case 10: // 7.1.2 surround (Cavern DCP, Atmos bed)
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -150, false);
        channels[5] = CreateChannel(0, 150, false);
        channels[6] = CreateChannel(0, -110, false);
        channels[7] = CreateChannel(0, 110, false);
        channels[8] = CreateChannel(-45, -70, false);
        channels[9] = CreateChannel(-45, 70, false);
        break;
    case 11: // 7.1.2.1 surround (Cavern XL DCP)
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -150, false);
        channels[5] = CreateChannel(0, 150, false);
        channels[6] = CreateChannel(0, -110, false);
        channels[7] = CreateChannel(0, 110, false);
        channels[8] = CreateChannel(-45, -70, false);
        channels[9] = CreateChannel(-45, 70, false);
        channels[10] = CreateChannel(90, 0, false);
        break;
    case 12: // Barco Auro 11.1
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -110, false);
        channels[5] = CreateChannel(0, 110, false);
        channels[6] = CreateChannel(-45, -30, false);
        channels[7] = CreateChannel(-45, 30, false);
        channels[8] = CreateChannel(-45, 0, false);
        channels[9] = CreateChannel(-90, 0, false);
        channels[10] = CreateChannel(-45, -110, false);
        channels[11] = CreateChannel(-45, 110, false);
        break;
    case 13: // 12-Track
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(-45, 0, false);
        channels[4] = CreateChannel(0, -150, false);
        channels[5] = CreateChannel(0, 150, false);
        channels[6] = CreateChannel(0, -110, false);
        channels[7] = CreateChannel(0, 110, false);
        channels[8] = CreateChannel(-45, -30, false);
        channels[9] = CreateChannel(-45, 30, false);
        channels[10] = CreateChannel(-45, -110, false);
        channels[11] = CreateChannel(-45, 110, false);
        channels[12] = CreateChannel(0, 0, true);
        break;
    case 14: // Barco Auro 13.1
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -150, false);
        channels[5] = CreateChannel(0, 150, false);
        channels[6] = CreateChannel(0, -110, false);
        channels[7] = CreateChannel(0, 110, false);
        channels[8] = CreateChannel(-45, -30, false);
        channels[9] = CreateChannel(-45, 30, false);
        channels[10] = CreateChannel(-45, 0, false);
        channels[11] = CreateChannel(-90, 0, false);
        channels[12] = CreateChannel(-45, -110, false);
        channels[13] = CreateChannel(-45, 110, false);
        break;
    case 16: // Full DCP
        channels[0] = CreateChannel(0, -30, false);
        channels[1] = CreateChannel(0, 30, false);
        channels[2] = CreateChannel(0, 0, false);
        channels[3] = CreateChannel(0, 0, true);
        channels[4] = CreateChannel(0, -110, false);
        channels[5] = CreateChannel(0, 110, false);
        channels[6] = new AudioChannel(true);
        channels[7] = new AudioChannel(true);
        channels[8] = CreateChannel(0, -15, false);
        channels[9] = CreateChannel(0, 15, false);
        channels[10] = CreateChannel(0, -150, false);
        channels[11] = CreateChannel(0, 150, false);
        channels[12] = new AudioChannel(true);
        channels[13] = new AudioChannel(true);
        channels[14] = new AudioChannel(true);
        channels[15] = new AudioChannel(true);
        break;
    default:
        for (int c = 0; c < channelCount; ++c)
            channels[c] = CreateChannel(0, 0, false);
        break;
    }
    return channels;
}
