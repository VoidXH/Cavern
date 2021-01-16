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

list<Format*> sources;
bool forceDCP = false;
float cavEffect = .75f, cavSmoothness = .8f, lfeVolume = 1;
int targetQuality = 0; // No quality specified
int centerStays = 1, matrixUpmix = 1, lfeSeparation = 1;
SpatialTarget cavernize = user;

void argHelp(int& arg) { // -h: help
    cout << "Cavernize Lite v1.1.5 help" << endl <<
        "==========================" << endl <<
        "-i/input <path>: input file path" << endl <<
        "-br/bitrate <8/16/32>: bit rate" << endl <<
        "-cav/cavernize <0/1/301/312/402/404/512>: preset layout - 0=copy, 1=auto, others=presets" << endl <<
        "-cc/count <count>: channel count override" << endl <<
        "-co/channel/override <channel> <x> <y> <lfe>: channel override" << endl <<
        "-cs/center <on/off>: center stays in place" << endl <<
        "-dcp: force standard DCP channel order (WAV only)" << endl <<
        "-ef/effect <percent>: Cavernize effect (0+%)" << endl <<
        "-lfe/separation <on/off>: LFE separation - keep source LFE" << endl <<
        "-lfev/lfevolume <percent>: LFE channel volume (0+%)" << endl <<
        "-mx/matrix <on/off>: matrix upmix" << endl <<
        "-sm/smoothness <percent>: Cavernize smoothness (0-100%)" << endl <<
        "last argument: output file path" << endl;
    ++arg;
}

string argInput(int& arg, int argc, char** argv) { // -i <path>: input file path
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + "\".";
    if (!fexists(argv[arg + 1]))
        return string(argv[arg + 1]) + " doesn't exist.";
    string ft = file_type(argv[arg + 1]);
    if (ft.c_str() == string("wav"))
        sources.push_back(new Waveform(string(argv[arg + 1])));
    else if (ft.c_str() == string("laf"))
        sources.push_back(new Limitless(string(argv[arg + 1])));
    else if (ft.c_str() == string("mxf"))
        sources.push_back(new OBAE(string(argv[arg + 1])));
    else
        return string("Unknown input format: \"") + string(ft) + "\".";
    sources.back()->ReadHeader(); // Read header to be able to override channel order in the next arguments
    arg += 2;
    return string();
}

string argBitrate(int& arg, int argc, char** argv) { // -br <8/16/32>: bit rate
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + "\".";
    targetQuality = to_int(argv[arg + 1]);
    if (targetQuality % 8 || targetQuality == 24 || targetQuality > 32)
        return string("Bit rate can only be 8, 16, or 32 bits per sample. \"") + argv[arg + 1] + "\" is invalid.";
    arg += 2;
    return string();
}

string argCavernize(int& arg, int argc, char** argv) { // -cav/cavernize <SpatialTarget>: preset layout
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + "\".";
    cavernize = (SpatialTarget)to_int(argv[arg + 1]);
    arg += 2;
    return string();
}

string argCount(int& arg, int argc, char** argv) { // -cc <count>: channel count override
    if (sources.empty())
        return string("\"") + argv[arg] + "\" must come after the first input file.";
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + "\".";
    int channels = to_int(argv[arg + 1]);
    if (channels == -1)
        return string("Channel count must be an integer.\"") + argv[arg + 1] + "\" is invalid.";
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
    arg += 2;
    return string();
}

string argChannel(int& arg, int argc, char** argv) { // -co <channel> <x> <y> <lfe>: channel override
    if (sources.empty())
        return string("\"") + argv[arg] + "\" must come after the first input file.";
    if (arg + 4 >= argc)
        return string("Not enough arguments for \"") + argv[arg] + "\".";
    int channel = to_int(argv[arg + 1]), x = to_int(argv[arg + 2]), y = to_int(argv[arg + 3]), lfe = to_int(argv[arg + 4]);
    if (lfe < 0 || lfe > 1)
        lfe = bool_to_int(argv[arg + 4]);
    if (channel == -1 || x == -1 || y == -1 || lfe == -1)
        return string("Invalid format for \"") + argv[arg] + "\" arguments. Syntax: -co <channel (integer)> <x (integer)> <y (integer)> <lfe (boolean)>.";
    if (channel > sources.front()->channelCount)
        return string("Channel ID out of range. Channel count could be overridden with \"-cc\".");
    AudioChannel* modified = sources.front()->channels[channel];
    modified->setX(x);
    modified->setY(y);
    modified->LFE = lfe;
    arg += 5;
    return string();
}

string argCenter(int& arg, int argc, char** argv) { // -cs/center <on/off>: center stays in place
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + string("\".");
    centerStays = bool_to_int(argv[arg + 1]);
    if (centerStays == -1)
        return string("Invalid format for \"") + argv[arg] + "\" arguments. Syntax: -cs/center <on/off>.";
    arg += 2;
    return string();
}

void argDCP(int& arg) { // -dcp: force standard DCP channel order (WAV only)
    forceDCP = true;
    ++arg;
}

string argEffect(int& arg, int argc, char** argv) { // -ef/effect <percent>: Cavernize effect (0+%)
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + string("\".");
    cavEffect = to_int(argv[arg + 1]) / 100.f;
    if (cavEffect < 0)
        return string("Cavernize effect can't be negative.");
    arg += 2;
    return string();
}

string argLFESeparation(int& arg, int argc, char** argv) { // -lfe/separation <on/off>: LFE separation - keep source LFE
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + string("\".");
    lfeSeparation = bool_to_int(argv[arg + 1]);
    if (lfeSeparation == -1)
        return string("Invalid format for \"") + argv[arg] + "\" arguments. Syntax: -lfe/separation <on/off>.";
    arg += 2;
    return string();
}

string argLFEVolume(int& arg, int argc, char** argv) { // -lfev/lfevolume <percent>: LFE channel volume (0+%)
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + "\".";
    lfeVolume = to_int(argv[arg + 1]) / 100.f;
    if (lfeVolume < 0)
        return string("LFE volume can't be negative.");
    arg += 2;
    return string();
}

string argMatrix(int& arg, int argc, char** argv) { // -mx/matrix <on/off>: matrix upmix
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + "\".";
    matrixUpmix = bool_to_int(argv[arg + 1]);
    if (matrixUpmix == -1)
        return string("Invalid format for \"") + argv[arg] + "\" arguments. Syntax: -mx/matrix <on/off>.";
    arg += 2;
    return string();
}

string argSmoothness(int& arg, int argc, char** argv) { // -sm/smoothness <percent>: Cavernize smoothness (0-100%)
    if (arg + 1 == argc)
        return string("Nothing found after \"") + argv[arg] + "\".";
    cavSmoothness = to_int(argv[arg + 1]);
    if (cavEffect < 0 || cavSmoothness > 100)
        return string("Cavernize smoothness must be between 0 and 100%.");
    cavSmoothness /= 100.f;
    arg += 2;
    return string();
}

#define CLEANUP(X) while (!sources.empty()) { delete sources.back(); sources.pop_back(); } if (target) delete target; return X;
#define ERROR(X) { cout << X << endl; CLEANUP(1) }
#define ARGUMENT(X) { string ret = X(i, argc, argv); if (!ret.empty()) ERROR(ret) }
#define ARGSL(SHORT, LONG, X) else if (!strcmp(argv[i], SHORT) || !strcmp(argv[i], LONG)) ARGUMENT(X)
#define ARGSLL(SHORT, LONG1, LONG2, X) else if (!strcmp(argv[i], SHORT) || !strcmp(argv[i], LONG1) || !strcmp(argv[i], LONG2)) ARGUMENT(X)

int main(int argc, char* argv[]) {
    /**********************************************************
    * Argument reading
    ***********************************************************/
    cout << "-- Cavernize Lite by VoidX (www.voidx.tk) --" << endl;
    Format* target = NULL;
    for (int i = 1; i < argc;) {
        if (!strcmp(argv[i], "-h") || !strcmp(argv[i], "-help")) argHelp(i); // -h: help
        ARGSL("-i", "-input", argInput) // -i <path>: input file path
        ARGSL("-br", "-bitrate", argBitrate) // -br <8/16/32>: bit rate
        ARGSL("-cav", "-cavernize", argCavernize) // -cav/cavernize <SpatialTarget>: preset layout
        ARGSL("-cc", "-count", argCount) // -cc <count>: channel count override
        ARGSLL("-co", "-channel", "-override", argChannel) // -co <channel> <x> <y> <lfe>: channel override
        ARGSL("-cs", "-center", argCenter) // -cs/center <on/off>: center stays in place
        else if (!strcmp(argv[i], "-dcp")) argDCP(i); // -dcp: force standard DCP channel order (WAV only)
        ARGSL("-ef", "-effect", argEffect) // -ef/effect <percent>: Cavernize effect (0+%)
        ARGSL("-lfe", "-separation", argLFESeparation) // -lfe/separation <on/off>: LFE separation - keep source LFE
        ARGSL("-lfev", "-lfevolume", argLFEVolume) // -lfev/lfevolume <percent>: LFE channel volume (0+%)
        ARGSL("-mx", "-matrix", argMatrix) // -mx/matrix <on/off>: matrix upmix
        ARGSL("-sm", "-smoothness", argSmoothness) // -sm/smoothness <percent>: Cavernize smoothness (0-100%)
        else if (i + 1 != argc) {
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
        if (forceDCP)
            source->ForceDCPStandardOrder();
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
