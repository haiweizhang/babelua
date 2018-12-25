#pragma once

#include <iostream>

class CLog
{
public:
	void DecodaLog(const char* message);
	void VsLog(const char* message);
	void SetDecodaLogMaxFileSize(size_t maxFileSize);
	void SetVsLogMaxFileSize(size_t maxFileSize);
public:
	CLog(const char* logDirectory);
	~CLog();
};
