#include "Format.h"

Format::Format(std::string filename, bool write) : write(write) {
    fileHandler.open(filename.c_str(), (write ? std::ios_base::out : std::ios_base::in) | std::ios_base::binary);
    channels = 0;
}

Format::~Format() {
    fileHandler.close();
    if (channels) {
        for (int32_t i = 0; i < channelCount; ++i)
            delete channels[i];
        delete[] channels;
    }
}
