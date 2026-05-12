#ifndef DLL_LOADER_H
#define DLL_LOADER_H

#include <windows.h>

class DllLoader {
public:
    DllLoader();
    virtual ~DllLoader();

    // Load DLL from the given path. Returns true on success.
    virtual bool Load(const wchar_t* dllPath);

    // Check if DLL is loaded successfully
    bool IsLoaded() const;

    // Release loaded DLL
    virtual void Unload();

protected:
    HMODULE GetHandle() const;

    HMODULE m_hDll;
};

#endif // DLL_LOADER_H
