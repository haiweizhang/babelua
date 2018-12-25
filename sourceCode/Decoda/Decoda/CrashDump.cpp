#include "CrashDump.h"
#include "Decoda.h"

#pragma comment(lib,"Version.lib")

bool GetApplicationVersion(HMODULE hModule,WORD nProdVersion[4])
{
	TCHAR szFullPath[MAX_PATH];
	DWORD dwVerInfoSize = 0;
	DWORD dwVerHnd;
	VS_FIXEDFILEINFO * pFileInfo;

	GetModuleFileName(hModule, szFullPath, sizeof(szFullPath));
	dwVerInfoSize = GetFileVersionInfoSize(szFullPath, &dwVerHnd);
	if (dwVerInfoSize)
	{
		// If we were able to get the information, process it:
		HANDLE  hMem;
		LPVOID  lpvMem;
		unsigned int uInfoSize = 0;

		hMem = GlobalAlloc(GMEM_MOVEABLE, dwVerInfoSize);
		lpvMem = GlobalLock(hMem);
		GetFileVersionInfo(szFullPath, dwVerHnd, dwVerInfoSize, lpvMem);

		::VerQueryValue(lpvMem, (LPTSTR)_T("\\"), (void**)&pFileInfo, &uInfoSize);

		// File version from the FILEVERSION of the version info resource 
		nProdVersion[0] = HIWORD(pFileInfo->dwFileVersionMS); 
		nProdVersion[1] = LOWORD(pFileInfo->dwFileVersionMS);
		nProdVersion[2] = HIWORD(pFileInfo->dwFileVersionLS);
		nProdVersion[3] = LOWORD(pFileInfo->dwFileVersionLS); 

		GlobalUnlock(hMem);
		GlobalFree(hMem);

		return true;
	}
	return false;
}

#include <dbghelp.h>
#pragma comment(lib,"dbghelp.lib")
#include <Shellapi.h>
extern bool g_bStartDebug;
LONG WINAPI CrashFunction(__in struct _EXCEPTION_POINTERS *ExceptionInfo)
{
	WORD nProdVersion[4] = {0};
	GetApplicationVersion(GetCurrentInstanceHandle(),nProdVersion);

	wxString version = wxString::Format("%d.%d.%d.%d",nProdVersion[0],nProdVersion[1],nProdVersion[2],nProdVersion[3]);

	wxString dumpFilePath = GetBabeLuaDirectory()+"\\dumpfile V"+version+".dmp";
	HANDLE hFile = ::CreateFile(dumpFilePath, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	if( hFile != INVALID_HANDLE_VALUE)
	{
		MINIDUMP_EXCEPTION_INFORMATION einfo;
		einfo.ThreadId = ::GetCurrentThreadId();
		einfo.ExceptionPointers = ExceptionInfo;
		einfo.ClientPointers = FALSE;

		::MiniDumpWriteDump(::GetCurrentProcess(), ::GetCurrentProcessId(), hFile, MiniDumpNormal, &einfo, NULL, NULL);
		::CloseHandle(hFile);

		if(g_bStartDebug)
		{
			wxString info = wxString::Format("error file: \r\n%s\r\n\r\nplease send the file to us help us improving.\r\n\r\npress ok to open the file directory.",dumpFilePath);
			MessageBox(NULL,info,"error",MB_OK);
			ShellExecute(NULL,NULL,"explorer","/select, "+dumpFilePath,NULL,SW_SHOW);
		}
	}
	return 0;
}
