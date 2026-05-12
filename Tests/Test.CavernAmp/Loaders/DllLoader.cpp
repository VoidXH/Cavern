#include "DllLoader.h"
#include <cstdio>

DllLoader::DllLoader()
    : m_hDll(nullptr)
{
}

DllLoader::~DllLoader() {
    Unload();
}

bool DllLoader::Load(const wchar_t* dllPath) {
    if (m_hDll) {
        return true; // Already loaded
    }

    m_hDll = LoadLibraryW(dllPath);
    if (!m_hDll) {
        fprintf(stderr, "FATAL: Could not load DLL: %ls (error %lu)\n", dllPath, GetLastError());
        return false;
    }

    return true;
}

bool DllLoader::IsLoaded() const {
    return m_hDll != nullptr;
}

void DllLoader::Unload() {
    if (m_hDll) {
        FreeLibrary(m_hDll);
        m_hDll = nullptr;
    }
}

HMODULE DllLoader::GetHandle() const {
    return m_hDll;
}
