#ifndef FASTCONVOLVER_LOADER_H
#define FASTCONVOLVER_LOADER_H

#include "../DllLoader.h"

class FastConvolverLoader : public DllLoader {
public:
    FastConvolverLoader();
    ~FastConvolverLoader();

    // Load DLL and resolve FastConvolver-specific function pointers
    bool Load(const wchar_t* dllPath) override;

    // --- Exported API (proxied to DLL) ---
    void* Create(const float* filter, int filterLength, int delay);
    int   GetLength(void* convolver);
    void  GetFilter(void* convolver, float* outFilter);
    void  Process(void* convolver, float* output, int sampleCount, int delay, int numChannels);
    void  Dispose(void* convolver);

protected:
    // Function pointer types
    typedef void* (*CreateFn)(const float*, int, int);
    typedef int   (*GetLengthFn)(void*);
    typedef void  (*GetFilterFn)(void*, float*);
    typedef void  (*ProcessFn)(void*, float*, int, int, int);
    typedef void  (*DisposeFn)(void*);

    // Function pointers
    CreateFn   m_pCreate;
    GetLengthFn   m_pGetLength;
    GetFilterFn   m_pGetFilter;
    ProcessFn   m_pProcess;
    DisposeFn   m_pDispose;
};

#endif // FASTCONVOLVER_LOADER_H