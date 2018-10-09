#include <iostream>
#include "Format.h"

enum SpatialTarget {
    disabled = 0,
    user = 1,
    f3_0_1 = 301,
    f3_1_2 = 312,
    f4_0_2 = 402,
    f4_0_4 = 404,
    f5_1_2 = 512
};

class CavernizeLite {
    bool centerStays;
    float effectScale, smoothness, lfeVolume, normalizerGain;
    float *lastLows, *lastNormals, *lastHighs, *heights;
    int32_t sampleRate, channelCount;
    static Format* renderTarget;

    void Normalize(float* target, int64_t sampleCount);
    void ChannelHeightCheck(float* source, int64_t samplesPerChannel, int32_t channels, int32_t channel, int32_t arrayPosition, float smoothFactor);

public:
    CavernizeLite(float effect, float smooth, float lfev, bool keepCenter, int32_t sampling, int32_t channels);
    void Upconvert(float* source, Format* sourceFormat, float* target, int64_t samplesPerChannel, bool lfeSeparation, bool matrixUpmix);
    static void Setup(Format* target, SpatialTarget upmix);
    ~CavernizeLite();
};
