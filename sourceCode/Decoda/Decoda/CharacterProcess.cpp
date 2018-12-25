#include "stdafx.h"
#include "CharacterProcess.h"

CCharArr::CCharArr()
{
	m_pchData = NULL;
}

CCharArr::CCharArr(const CCharArr &szArr)
{
	m_pchData = NULL;
	*this = szArr;
}

CCharArr::CCharArr(LPCSTR lpsz)
{
	m_pchData = NULL;
	*this = lpsz;
}

CCharArr::CCharArr(LPCWSTR lpsz)
{
	m_pchData = NULL;
	*this = lpsz;
}

CCharArr::~CCharArr()
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
}

const CCharArr& CCharArr::operator=(const CCharArr &szArr)
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
	if(szArr.m_pchData != NULL)
	{
		m_pchData = new char[strlen(szArr.m_pchData)+1];
		strcpy(m_pchData,szArr.m_pchData);
	}
	return *this;
}

const CCharArr& CCharArr::operator=(LPCSTR lpsz)
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
	if(lpsz != NULL)
	{
		m_pchData = new char[strlen(lpsz)+1];
		strcpy(m_pchData,lpsz);
	}
	return *this;
}

const CCharArr& CCharArr::operator=(LPCWSTR lpsz)
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
	if(lpsz != NULL)
	{
		int iLen = WideCharToMultiByte(CP_ACP,0,lpsz,-1,NULL,0,NULL,NULL);
		m_pchData = new char[iLen];
		WideCharToMultiByte(CP_ACP,0,lpsz,(int)wcslen(lpsz),m_pchData,iLen,NULL,NULL);
		m_pchData[iLen-1] = '\0';
	}
	return *this;
}

CCharArr::operator char*() const
{
	return m_pchData;
}


CWCharArr::CWCharArr()
{
	m_pchData = NULL;
}

CWCharArr::CWCharArr(const CWCharArr &szArr)
{
	m_pchData = NULL;
	*this = szArr;
}

CWCharArr::CWCharArr(LPCSTR lpsz)
{
	m_pchData = NULL;
	*this = lpsz;
}

CWCharArr::CWCharArr(LPCWSTR lpsz)
{
	m_pchData = NULL;
	*this = lpsz;
}

CWCharArr::~CWCharArr()
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
}

const CWCharArr& CWCharArr::operator=(const CWCharArr &szArr)
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
	if(szArr.m_pchData != NULL)
	{
		m_pchData = new WCHAR[wcslen(szArr.m_pchData)+1];
		wcscpy(m_pchData,szArr.m_pchData);
	}
	return *this;
}

const CWCharArr& CWCharArr::operator=(LPCSTR lpsz)
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
	if(lpsz != NULL)
	{
		int iLen = MultiByteToWideChar(CP_ACP,0,lpsz,-1,NULL,0);
		m_pchData = new WCHAR[iLen];
		MultiByteToWideChar(CP_ACP,0,lpsz,(int)strlen(lpsz),m_pchData,iLen);
		m_pchData[iLen-1] = '\0';
	}
	return *this;
}

const CWCharArr& CWCharArr::operator=(LPCWSTR lpsz)
{
	if(m_pchData != NULL)
	{
		delete []m_pchData;
		m_pchData = NULL;
	}
	if(lpsz != NULL)
	{
		m_pchData = new WCHAR[wcslen(lpsz)+1];
		wcscpy(m_pchData,lpsz);
	}
	return *this;
}

CWCharArr::operator WCHAR*() const
{
	return m_pchData;
}


