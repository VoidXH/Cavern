#ifndef FILTER_H
#define FILTER_H

// Abstract base class for filters.
class Filter {
public:
    virtual void Process(float* samples, int len) = 0;
    virtual void Process(float* samples, int len, int channel, int channels) = 0;
};

#endif // FILTER_H
