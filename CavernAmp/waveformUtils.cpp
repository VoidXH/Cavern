#include "waveformUtils.h"

void Mix(float* from, float* to, int length) {
    for (int i = 0; i < length; ++i)
        to[i] += from[i];
}
