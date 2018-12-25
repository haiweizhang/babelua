#ifndef _CRASH_DUMP
#define _CRASH_DUMP

#include <wx/wx.h>

LONG WINAPI CrashFunction(__in struct _EXCEPTION_POINTERS *ExceptionInfo);

#endif
