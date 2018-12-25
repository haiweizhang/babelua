#include "BabeLua.h"
#include "LuaDll.h"
#include "DebugBackend.h"

std::string GetPackagePath(lua_State* L,unsigned long api)
{
	std::string path = ".";
    lua_getglobal_dll(api, L, "package");
    if (!lua_isnil_dll(api, L, -1))
	{
		lua_getfield_dll(api, L, -1, "path");
		if (lua_type_dll(api, L, -1) == LUA_TSTRING)//lua_isstring(L, -1))
		{
			const char* cur_path = lua_tostring_dll(api, L, -1);
			if ( 0 == cur_path || '\0' == cur_path[0] )
			{
			}
			else
			{
				path = cur_path;
			}
		}
		lua_pop_dll(api, L, 1);
	}
	lua_pop_dll(api, L, 1);

	return path;
}
/*
std::string GetPackagePath(lua_State* L)
{
	std::string path = ".";
    lua_getglobal(L, "package");
    if (!lua_isnil(L, -1))
	{
		lua_getfield(L, -1, "path");
		if (lua_isstring(L, -1))
		{
			const char* cur_path = lua_tostring(L, -1);
			if ( 0 == cur_path || '\0' == cur_path[0] )
			{
			}
			else
			{
				path = cur_path;
			}
		}
		lua_pop(L, 1);
	}
	lua_pop(L, 1);

	return path;
}

std::string GetPackagePath(lua_State *L)
{
	std::string strPackagePath;

	if(luaL_dostring(L,"__babePackagePath = _G.package.path;") == 0)
	{
		lua_getglobal(L,"__babePackagePath");
		const char *packagePath = lua_tostring(L,-1);
		if(packagePath != NULL)
		{
			strPackagePath = packagePath;
		}
		lua_pop(L,1);
	}

	return strPackagePath;
}

class CLuaState
{
public:
	CLuaState()
	{
		L = luaL_newstate();
		if(L != NULL)
		{
			luaL_openlibs(L);
		}
	}
	~CLuaState()
	{
		if(L != NULL)
		{
			lua_close(L);
			L = NULL;
		}
	}
	operator lua_State*() const
	{
		return L;
	}
	bool IsLuaStateNull() const
	{
		return (L == NULL);
	}
protected:
	lua_State *L;
};
CLuaState g_tempLuaState;
*/

static int readable (const char *filename) {
	FILE *f = fopen(filename, "r");  /* try to open file */
	if (f == NULL) return 0;  /* open failed */
	fclose(f);
	return 1;
}

static const char *pushnexttemplate (const char *path,std::string &filename)
{
	filename.clear();

	const char *l;
	while (*path == *LUA_PATHSEP) path++;  /* skip separators */
	if (*path == '\0') return NULL;  /* no more templates */
	l = strchr(path, *LUA_PATHSEP);  /* find next separator */
	if (l == NULL) l = path + strlen(path);

	std::string str(path,l-path);
	filename = str;

	return l;
}

void StringReplace(std::string &strBase, std::string strSrc, std::string strDes)  
{  
	std::string::size_type pos = 0;  
	std::string::size_type srcLen = strSrc.size();  
	std::string::size_type desLen = strDes.size();  
	pos = strBase.find(strSrc, pos);   
	while ((pos != std::string::npos))  
	{  
		strBase.replace(pos, srcLen, strDes);  
		pos=strBase.find(strSrc, (pos+desLen));  
	}  
}

std::string findfile (const char *name, const char *path)//const char *pname)
{
	std::string strName = name;
	StringReplace(strName,".", LUA_DIRSEP);

	std::string filename;
	while ((path = pushnexttemplate(path, filename)) != NULL) {
		StringReplace(filename, LUA_PATH_MARK, name);

		if (readable(filename.c_str()))  /* does file exist and is readable? */
			return filename;  /* return that file name */
	}
	return "";  /* not found */
}

bool BabeFindFile(const char *name,const char *path,std::string &filePath)
{
	//为调用findfile函数去除最后面的.lua后缀 如test.lua则变成test
	std::string strName = name;
	if(strName.length() >= 4)
	{
		std::string right = strName.substr(strName.length()-4,4);
		
		transform(right.begin(), right.end(), right.begin(), tolower);

		if(_stricmp(right.c_str(),".lua") == 0)
		{
			strName = strName.substr(0,strName.length()-4);
		}
	}

	filePath = findfile(strName.c_str(), path);

	return !filePath.empty();
}

bool g_bOutputPackagePath = true;
bool IsOutputPackagePath()
{
	return g_bOutputPackagePath;
}

void SetOutputPackagePath(bool bOutput)
{
	g_bOutputPackagePath = bOutput;
}

void WriteLog(const char* log)
{
	FILE *fp = fopen("D:\\log.txt","a+");
	if(fp != NULL)
	{
		fwrite(log,strlen(log),1,fp);
		fwrite("\r\n",2,1,fp);
		fclose(fp);
	}
}
