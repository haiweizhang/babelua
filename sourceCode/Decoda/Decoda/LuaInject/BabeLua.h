#pragma once

#include <string>
#include <algorithm>

#include <Shlwapi.h>
#pragma comment(lib,"Shlwapi.lib")

extern "C"
{
#include <lua.h>
#include <lauxlib.h>
}

std::string GetPackagePath(lua_State* L,unsigned long api);
bool BabeFindFile(const char *name,const char *path,std::string &filePath);
bool IsOutputPackagePath();
void SetOutputPackagePath(bool bOutput);
void WriteLog(const char* log);
