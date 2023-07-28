#include <algorithm>

#include "complexArray.h"
#include "fastConvolver.h"
#include "measurements.h"
#include "qmath.h"

using namespace std;

FastConvolver::FastConvolver(const float *impulse, const int len) {
    Initialize(impulse, len, 0);
}

FastConvolver::FastConvolver(const float *impulse, const int len, const int delay) {
    Initialize(impulse, len, delay);
}

void FastConvolver::Initialize(const float *impulse, const int len, const int delay) {
    filterLength = 2 << log2Ceil(len); // Zero padding for the falloff to have space
    cache = FFTCache_Create(filterLength);
    filter = (Complex*)malloc(sizeof(Complex) * filterLength);
    for (int sample = 0; sample < len; sample++) {
        filter[sample].real = impulse[sample];
    }
    ProcessFFT(filter, filterLength, cache, log2(filterLength) - 1);
    present = (Complex*)malloc(sizeof(Complex) * filterLength);
    future = (float*)malloc(sizeof(float) * (filterLength + delay));
    this->delay = delay;
}

void FastConvolver::Process(float *samples, int len) {
    int start = 0;
    while (start < len) {
        int nextBlock = start + (filterLength >> 1);
        ProcessTimeslot(samples, 0, 1, start, min(len, nextBlock));
        start = nextBlock;
    }
}

void FastConvolver::Process(float *samples, int len, int channel, int channels) {
    int start = 0,
        end = len / channels;
    while (start < end) {
        int nextBlock = start + (filterLength >> 1);
        ProcessTimeslot(samples, channel, channels, start, min(end, nextBlock));
        start = nextBlock;
    }
}

void FastConvolver::ProcessTimeslot(float *samples, int channel, int channels, int from, int to) {
    int sourceLength = to - from;
    float *sample = samples + from * channels + channel,
        *lastSample = sample + sourceLength * channels;
    Complex* timeslot = present;
    while (sample != lastSample) {
        timeslot->real = *sample;
        timeslot->imaginary = 0;
        timeslot++;
        sample += channels;
    }
    for (int i = sourceLength; i < filterLength; i++) {
        present[i].real = 0;
        present[i].imaginary = 0;
    }

    ProcessCache(sourceLength + (filterLength >> 1));

    float *source = future;
    sample = samples + from * channels + channel;
    lastSample = sample + sourceLength * channels;
    while (sample != lastSample) {
        *sample = *source++;
        sample += channels;
    }
    to -= from;

    int futureLength = filterLength + delay;
    for (int i = 0; i < futureLength - to; i++) {
        future[i] = future[i + to];
    }
    for (int i = futureLength - to; i < futureLength; i++) {
        future[i] = 0;
    }
}

void FastConvolver::ProcessCache(const int maxResultLength) {
    // Perform the convolution
    ProcessFFT(present, filterLength, cache, log2(filterLength) - 1);
    Convolve(present, filter, filterLength);
    InPlaceIFFT(present, filterLength, cache);

    // Append the result to the future
    Complex *source = present;
    float *destination = future + delay,
        *end = destination + maxResultLength;
    while (destination != end) {
        *destination++ += (*source++).real;
    }
}

FastConvolver::~FastConvolver() {
    free(filter);
    free(present);
    free(future);
    free(cache);
}

FastConvolver* DLL_EXPORT FastConvolver_Create(const float *impulse, const int len, const int delay) {
    FastConvolver *instance = (FastConvolver*)malloc(sizeof(FastConvolver));
    new(instance) FastConvolver(impulse, len, delay);
    return instance;
}

void DLL_EXPORT FastConvolver_Process(FastConvolver *instance, float *samples, int len, int channel, int channels) {
    instance->Process(samples, len, channel, channels);
}

void DLL_EXPORT FastConvolver_Dispose(FastConvolver *instance) {
    instance->~FastConvolver();
    free(instance);
}
