#include <cstdlib>
#include <cstring>
#include <ctime>
#include <iostream>
#include <list>
#include <string>
#include "AudioChannel.h"
#include "CavernizeLite.h"
#include "Limitless.h"
#include "OBAE.h"
#include "Waveform.h"
using namespace std;

#define UPDATE_RATE 240

inline bool fexists(const char* path) { return ifstream(path).is_open(); }
int to_int(const char* str);

inline bool ends_with(const std::string &value, const std::string &ending) {
    if (ending.size() > value.size()) return false;
    return std::equal(ending.rbegin(), ending.rend(), value.rbegin());
}

string file_type(char* path) {
    for (; *path && *path != '.'; ++path);
    if (*path != '.') return string("no file type");
    return string(path + 1);
}

int bool_to_int(const char* str) {
    if (*str == 't' || *str == 'T' || *str == '1' || *str == 'y' || str[1] == 'n') return 1;
    if (*str == 'f' || *str == 'F' || *str == '0' || *str == 'n' || str[1] == 'f') return 0;
    return -1;
}

#define CLEANUP(X) while (!sources.empty()) { delete sources.back(); sources.pop_back(); } if (target) delete target; return X;
#define ERROR(X) { cout << X << endl; CLEANUP(1) }

int main(int argc, char* argv[]) {
    /**********************************************************
    * Argument reading
    ***********************************************************/
    cout << "-- Cavernize Lite by VoidX (www.voidx.tk) --" << endl;
    list<Format*> sources;
    Format* target = NULL;
    SpatialTarget cavernize = user;
    float cavEffect = .75f, cavSmoothness = .8f, lfeVolume = 1;
    int targetQuality = 0; // No quality specified
    int centerStays = 1, matrixUpmix = 1, lfeSeparation = 1;
    for (int i = 1; i < argc;) {
        if (!strcmp(argv[i], "-h") || !strcmp(argv[i], "-help")) { // -h: help
            cout << "Cavernize Lite v1.1.5 help" << endl <<
                    "==========================" << endl <<
                    "-i/input <path>: input file path" << endl <<
                    "-br/bitrate <8/16/32>: bit rate" << endl <<
                    "-cav/cavernize <0/1/301/312/402/404/512>: preset layout - 0=copy, 1=auto, others=presets" << endl <<
                    "-cc/count <count>: channel count override" << endl <<
                    "-co/channel/override <channel> <x> <y> <lfe>: channel override" << endl <<
                    "-cs/center <on/off>: center stays in place" << endl <<
                    "-ef <percent>: Cavernize effect (0+%)" << endl <<
                    "-lfe <on/off>: LFE separation - keep source LFE" << endl <<
                    "-lfev <percent>: LFE channel volume (0+%)" << endl <<
                    "-mx <on/off>: matrix upmix" << endl <<
                    "-sm <percent>: Cavernize smoothness (0-100%)" << endl <<
                    "last argument: output file path" << endl;
            ++i;
        } else if (!strcmp(argv[i], "-i") || !strcmp(argv[i], "-input")) { // -i <path>: input file path
            if (i + 1 == argc)
                ERROR("Nothing found after \"" << argv[i] << "\".")
            if (!fexists(argv[i + 1]))
                ERROR(argv[i + 1] << " doesn't exist.")
            string ft = file_type(argv[i + 1]);
            if (ft.c_str() == string("wav"))
                sources.push_back(new Waveform(string(argv[i + 1])));
            else if (ft.c_str() == string("laf"))
                sources.push_back(new Limitless(string(argv[i + 1])));
            else if (ft.c_str() == string("mxf"))
                sources.push_back(new OBAE(string(argv[i + 1])));
            else
                ERROR("Unknown input format: \"" << ft << "\".")
            sources.back()->ReadHeader(); // Read header to be able to override channel order in the next arguments
            i += 2;
        } else if (!strcmp(argv[i], "-br") || !strcmp(argv[i], "-bitrate")) { // -br <8/16/32>: bit rate
            if (i + 1 == argc)
                ERROR("Nothing found after \"" << argv[i] << "\".")
            targetQuality = to_int(argv[i + 1]);
            if (targetQuality % 8 || targetQuality == 24 || targetQuality > 32)
                ERROR("Bit rate can only be 8, 16, or 32 bits per sample. \"" << argv[i + 1] << "\" is invalid.")
            i += 2;
        } else if (!strcmp(argv[i], "-cav") || !strcmp(argv[i], "-cavernize")) { // -cav/cavernize <SpatialTarget>: preset layout
            if (i + 1 == argc)
                ERROR("Nothing found after \"" << argv[i] << "\".")
            cavernize = (SpatialTarget)to_int(argv[i + 1]);
            i += 2;
        } else if (!strcmp(argv[i], "-cc") || !strcmp(argv[i], "-count")) { // -cc <count>: channel count override
            if (sources.empty())
                ERROR("\"" << argv[i] << "\" must come after the first input file.")
            if (i + 1 == argc)
                ERROR("Nothing found after \"" << argv[i] << "\".")
            int channels = to_int(argv[i + 1]);
            if (channels == -1)
                ERROR("Channel count must be an integer.\"" << argv[i + 1] << "\" is invalid.")
            AudioChannel** modified = new AudioChannel*[channels];
            for (int c = 0; c < channels && c < sources.front()->channelCount; ++c)
                modified[c] = sources.front()->channels[c];
            for (int c = sources.front()->channelCount; c < channels; ++c)
                modified[c] = new AudioChannel(0, 0);
            for (int c = channels; c < sources.front()->channelCount; ++c)
                delete sources.front()->channels[c];
            delete[] sources.front()->channels;
            sources.front()->channels = modified;
            sources.front()->channelCount = channels;
            i += 2;
        } else if (!strcmp(argv[i], "-co") || !strcmp(argv[i], "-channel") || !strcmp(argv[i], "-override")) { // -co <channel> <x> <y> <lfe>: channel override
            if (sources.empty())
                ERROR("\"" << argv[i] << "\" must come after the first input file.")
            if (i + 4 >= argc)
                ERROR("Not enough arguments for \"" << argv[i] << "\".")
            int channel = to_int(argv[i + 1]), x = to_int(argv[i + 2]), y = to_int(argv[i + 3]), lfe = to_int(argv[i + 4]);
            if (lfe < 0 || lfe > 1)
                lfe = bool_to_int(argv[i + 4]);
            if (channel == -1 || x == -1 || y == -1 || lfe == -1)
                ERROR("Invalid format for \"" << argv[i] << "\" arguments. Syntax: -co <channel (integer)> <x (integer)> <y (integer)> <lfe (boolean)>.")
            if (channel > sources.front()->channelCount)
                ERROR("Channel ID out of range. Channel count could be overridden with \"-cc\".")
            AudioChannel* modified = sources.front()->channels[channel];
            modified->setX(x);
            modified->setY(y);
            modified->LFE = lfe;
            i += 5;
        } else if (!strcmp(argv[i], "-cs") || !strcmp(argv[i], "-center")) { // -cs/center <on/off>: center stays in place
            centerStays = bool_to_int(argv[i + 1]);
            if (centerStays == -1)
                ERROR("Invalid format for \"" << argv[i] << "\" arguments. Syntax: -cs/center <on/off>.")
            i += 2;
        } else if (!strcmp(argv[i], "-ef")) { // -ef <percent>: Cavernize effect (0+%)
            if (i + 1 == argc)
                ERROR("Nothing found after \"" << argv[i] << "\".")
            cavEffect = to_int(argv[i + 1]) / 100.f;
            if (cavEffect < 0)
                ERROR("Cavernize effect can't be negative.");
            i += 2;
        } else if (!strcmp(argv[i], "-lfe")) { // -lfe <on/off>: LFE separation - keep source LFE
            lfeSeparation = bool_to_int(argv[i + 1]);
            if (lfeSeparation == -1)
                ERROR("Invalid format for \"" << argv[i] << "\" arguments. Syntax: -lfe <on/off>.")
            i += 2;
        } else if (!strcmp(argv[i], "-lfev")) { // -lfev <percent>: LFE channel volume (0+%)
            if (i + 1 == argc)
                ERROR("Nothing found after \"" << argv[i] << "\".")
            lfeVolume = to_int(argv[i + 1]) / 100.f;
            if (lfeVolume < 0)
                ERROR("LFE volume can't be negative.");
            i += 2;
        } else if (!strcmp(argv[i], "-mx")) { // -mx <on/off>: matrix upmix
            matrixUpmix = bool_to_int(argv[i + 1]);
            if (matrixUpmix == -1)
                ERROR("Invalid format for \"" << argv[i] << "\" arguments. Syntax: -mx <on/off>.")
            i += 2;
        } else if (!strcmp(argv[i], "-sm")) { // -sm <percent>: Cavernize smoothness (0-100%)
            if (i + 1 == argc)
                ERROR("Nothing found after \"" << argv[i] << "\".")
            cavSmoothness = to_int(argv[i + 1]);
            if (cavEffect < 0 || cavSmoothness > 100)
                ERROR("Cavernize smoothness must be between 0 and 100%.");
            cavSmoothness /= 100.f;
            i += 2;
        } else if (i + 1 != argc) {
            ERROR("Invalid argument: \"" << argv[i] << "\". Use the -h argument to list all arguments.")
        } else {
            string ft = file_type(argv[i]);
            if (ft.c_str() == string("wav"))
                target = new Waveform(string(argv[i]), true);
            else if (ft.c_str() == string("laf"))
                target = new Limitless(string(argv[i]), true);
            else
                ERROR("Unknown output format: \"" << ft << "\".")
            ++i;
        }
    }

    /**********************************************************
    * Metadata output
    ***********************************************************/
    if (sources.empty())
        ERROR("No input files were given.")
    if (target == NULL)
        ERROR("No output name was given.")
    CavernizeLite* cavernizer = NULL;
    float* cavernCache = NULL;
    Format* first = sources.front();
    target->quality = targetQuality == 0 ? first->quality : (Quality)targetQuality;
    target->format = first->format;
    target->sampleRate = first->sampleRate;
    target->totalSamples = 0;

    int totalSources = 0;
    for (list<Format*>::iterator i = sources.begin(); i != sources.end(); ++i) {
        Format* source = *i;
        if (first->quality != source->quality)
            ERROR("Bit rate of the sources differ.")
        else if (first->channelCount != source->channelCount)
            ERROR("Channel count of the sources differ.")
        else if (first->sampleRate != source->sampleRate)
            ERROR("Sample rate of the sources differ.")
        target->totalSamples += source->totalSamples;
        ++totalSources;
    }

    if (cavernize != disabled) {
        CavernizeLite::Setup(target, cavernize);
        cavernizer = new CavernizeLite(cavEffect, cavSmoothness, lfeVolume, centerStays, target->sampleRate, first->channelCount);
        cavernCache = new float[target->channelCount * UPDATE_RATE];
    } else {
        target->channels = new AudioChannel*[first->channelCount];
        for (int i = 0; i < first->channelCount; ++i)
            target->channels[i] = first->channels[i];
        target->channelCount = first->channelCount;
        target->totalSamples = target->totalSamples / first->channelCount * target->channelCount;
    }

    target->WriteHeader();

    /**********************************************************
    * Content copy
    ***********************************************************/
    int sourceId = 0;
    int64_t writtenSamples = 0;
    cout.setf(ios::fixed, ios::floatfield);
    cout.precision(2);
    time_t lastResult = time(NULL) + 1;
    for (list<Format*>::iterator i = sources.begin(); i != sources.end(); ++i) {
        ++sourceId;
        Format* source = *i;
        int64_t readRate = UPDATE_RATE * source->channelCount, writeRate = target->sampleRate * target->channelCount;
        float* samples = new float[max(readRate, writeRate)];
        for (int64_t p = 0; p < source->totalSamples;) {
            int64_t sourceReadRate = (p + UPDATE_RATE <= source->totalSamples) ? readRate : ((source->totalSamples - p) * source->channelCount);
            source->Read(samples, sourceReadRate);
            if (cavernizer) {
                cavernizer->Upconvert(samples, source, cavernCache, UPDATE_RATE, lfeSeparation, matrixUpmix);
                target->Write(cavernCache, UPDATE_RATE * target->channelCount);
            } else
                target->Write(samples, sourceReadRate / source->channelCount * target->channelCount);
            writtenSamples += sourceReadRate / source->channelCount;
            p += UPDATE_RATE;
            if (p > source->totalSamples)
                p = source->totalSamples;
            if (time(NULL) - lastResult || p == source->totalSamples) {
                cout << "Source " << sourceId << '/' << totalSources << " - Current: " << 100.0f * p / source->totalSamples
                     << "% - Total: " << 100.0f * writtenSamples / target->totalSamples << '%' << '\r' << flush;
                lastResult = time(NULL);
            }
        }
        delete[] samples;
    }

    /**********************************************************
    * Cleanup
    ***********************************************************/
    cout << endl;
    if (cavernizer) {
        delete cavernizer;
        delete cavernCache;
    }
    CLEANUP(0)
}

int to_int(const char* str) {
    int r = 0;
    bool neg = false;
    if (*str == '-') {
        neg = true;
        ++str;
    }
    while (*str) {
        if (*str < '0' || *str > '9')
            return -1;
        r = r * 10 + *str - '0';
        ++str;
    }
    return !neg ? r : -r;
}
