#include "FastConvolver.h"
#include <cstdio>
#include <cstring>

FastConvolverLoader::FastConvolverLoader()
    : m_pCreate(nullptr)
    , m_pGetLength(nullptr)
    , m_pGetFilter(nullptr)
    , m_pProcess(nullptr)
    , m_pDispose(nullptr)
{
}

FastConvolverLoader::~FastConvolverLoader() {
}

bool FastConvolverLoader::Load(const wchar_t* dllPath) {
    if (!DllLoader::Load(dllPath)) {
        return false;
    }

    m_pCreate = reinterpret_cast<CreateFn>(GetProcAddress(GetHandle(), "FastConvolver_Create"));
    m_pGetLength = reinterpret_cast<GetLengthFn>(GetProcAddress(GetHandle(), "FastConvolver_GetLength"));
    m_pGetFilter = reinterpret_cast<GetFilterFn>(GetProcAddress(GetHandle(), "FastConvolver_GetFilter"));
    m_pProcess = reinterpret_cast<ProcessFn>(GetProcAddress(GetHandle(), "FastConvolver_Process"));
    m_pDispose = reinterpret_cast<DisposeFn>(GetProcAddress(GetHandle(), "FastConvolver_Dispose"));

    if (!m_pCreate || !m_pGetLength || !m_pGetFilter || !m_pProcess || !m_pDispose) {
        fprintf(stderr, "FATAL: Could not resolve DLL exports (error %lu)\n", GetLastError());
        Unload();
        return false;
    }

    return true;
}

void* FastConvolverLoader::Create(const float* filter, int filterLength, int delay) {
    if (!m_pCreate) return nullptr;
    return m_pCreate(filter, filterLength, delay);
}

int FastConvolverLoader::GetLength(void* convolver) {
    if (!m_pGetLength) return 0;
    return m_pGetLength(convolver);
}

void FastConvolverLoader::GetFilter(void* convolver, float* outFilter) {
    if (!m_pGetFilter) return;
    m_pGetFilter(convolver, outFilter);
}

void FastConvolverLoader::Process(void* convolver, float* output, int sampleCount, int delay, int numChannels) {
    if (!m_pProcess) return;
    m_pProcess(convolver, output, sampleCount, delay, numChannels);
}

void FastConvolverLoader::Dispose(void* convolver) {
    if (!m_pDispose) return;
    m_pDispose(convolver);
}
}
