#ifndef _CHARACTER_PROCESS_H
#define _CHARACTER_PROCESS_H


class CCharArr
{
public:
	CCharArr();
	CCharArr(const CCharArr &szArr);
	CCharArr(LPCSTR lpsz);
	CCharArr(LPCWSTR lpsz);
	~CCharArr();
	const CCharArr& operator=(const CCharArr &szArr);
	const CCharArr& operator=(LPCSTR lpsz);
	const CCharArr& operator=(LPCWSTR lpsz);
	operator char*() const;
private:
	char *m_pchData;
};

class CWCharArr
{
public:
	CWCharArr();
	CWCharArr(const CWCharArr &szArr);
	CWCharArr(LPCSTR lpsz);
	CWCharArr(LPCWSTR lpsz);
	~CWCharArr();
	const CWCharArr& operator=(const CWCharArr &szArr);
	const CWCharArr& operator=(LPCSTR lpsz);
	const CWCharArr& operator=(LPCWSTR lpsz);
	operator WCHAR*() const;
private:
	WCHAR *m_pchData;
};

#endif