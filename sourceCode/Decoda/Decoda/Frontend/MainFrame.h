/*

Decoda
Copyright (C) 2007-2013 Unknown Worlds Entertainment, Inc. 

This file is part of Decoda.

Decoda is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Decoda is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Decoda.  If not, see <http://www.gnu.org/licenses/>.

*/

#ifndef MAIN_FRAME_H
#define MAIN_FRAME_H

#include <wx/wx.h>
#include <wx/filename.h>
#include <wx/listctrl.h>
#include <wx/aui/aui.h>
#include <wx/process.h>
#include <wx/fdrepdlg.h>
#include <wx/treectrl.h>
#include <wx/docview.h>
#include <wx/help.h>
#include <wx/cshelp.h>

#include "Project.h"
#include "DebugFrontend.h"
#include "DebugEvent.h"

#include <vector>
#include <string>

#include "../CharacterProcess.h"


class CBreakPoint
{
public:
	wxString GetFile();
	int GetLine();
public:
	CBreakPoint();
	CBreakPoint(wxString file,int line);
	~CBreakPoint();
protected:
	wxString m_file;
	int m_line;
};

class CBreakPoints
{
public:
	void Clear();
	void AddBreakPoint(wxString file,int line);
	void RemoveBreakPoint(wxString file,int line);
	void GetLines(wxString file,std::vector<int> &lines);
public:
	CBreakPoints(){}
	~CBreakPoints(){}
protected:
	std::vector<CBreakPoint> m_breakpoints;

	mutable CriticalSection m_criticalSection;
};

class CWriteDebugEvent
{
public:
	void WriteDebugEvent(const wxDebugEvent& event);
	void WriteDebugEvent(const wxDebugEvent& event,const wxString &file);
private:
	wxString GetDebugEvent(const wxDebugEvent& event);
public:
	CWriteDebugEvent();
	~CWriteDebugEvent();
protected:
	BOOL m_bWrite;
};


void SetCurrentInstanceHandle(HINSTANCE hInstance);
HINSTANCE GetCurrentInstanceHandle();

BOOL CreateDirectory(wxString directory);
void CreateMultipleDirectory(wxString directory);
BOOL IsDirectory(wxString path);
BOOL IsDirectoryExist(wxString directory);
wxString GetBabeLuaDirectory();
CWCharArr UTF8ToUnicode(const char* str);

void OnDebugEvent(wxDebugEvent& event);

bool IsEqualFilePath(wxString path1,wxString path2);
wxString PathToFullPath(wxString path);
wxString GetFileFullPath(Project::File *file,bool *pRelative = NULL);	//如果是相对路径文件则在前面加上scripts目录
bool IsFileSetBreakpoint(int scriptIndex,int line);
bool FileSetBreakpoint(int scriptIndex,int line,bool set);
void InitSetBreakpoint(const char *fullPath,int line);
void OutputMessage(int msgType,const char *msg);
void OutputMessage(int msgType,const char *fullPath,int line,const char *msg);

/**
 * Maps a line number from the backend script to the currently open page.
 */
unsigned int OldToNewLine(Project::File* file, unsigned int oldLine);

/**
 * Extracts the topic from an error message which is formatted to correspond
 * to a help section. The messages are in the form "Warning ####: " or
 * "Error ####: ". If the format matches one of these, the topic will be
 * "warning_####" or "error_####".
 */
bool ParseHelpMessage(const wxString& message, wxString& topic);

/**
 * Extracts the file name and line number from an error message. If the
 * error message doesn't have a recognizable form, the method returns false.
 */
bool ParseErrorMessage(const wxString& error, wxString& fileName, unsigned int& line);

/**
 * Extracts the file name and line number from a Lua-style error message. A Lua-style error
 * message has the form "filename:line: message". If the error message doesn't have
 * that form, the method returns false.
 */
bool ParseLuaErrorMessage(const wxString& error, wxString& fileName, unsigned int& line);

/**
 * Extracts the file name and line number from a Luac-style error message. A Luac-style error
 * message has the form "appname: filename:line: message". If the error message doesn't have
 * that form, the method returns false.
 */
bool ParseLuacErrorMessage(const wxString& error, wxString& fileName, unsigned int& line);

bool ParseMessage(const wxString& error, wxString& target, unsigned int& line,wxString &message);

/**
 * Gets the index of the script associated with an edit control. If there's no script
 * associated with it the method returns -1.
 */
int GetScriptIndex(const char *fullPath);

/**
 * Sets the vm that the UI is controlling/inspecting.
 */
void SetContext(unsigned int vm, unsigned int stackLevel);

/**
 * Adds the virtual machine to the list of virtual machines handled by the
 * debugger.
 */
void AddVmToList(unsigned int vm);

/**
 * Removes the specified virtual machine from the list of virtual machines
 * handled by the debugger.
 */
void RemoveVmFromList(unsigned int vm);

/**
 * Removes any temporary files from the project that aren't open in the
 * editor. This is useful after a debugging session has ended and temporary
 * files that were automatically opened during debugging need to be removed.
 */
void CleanUpTemporaryFiles();

/**
 * Removes all of the breakpoints from the specified file. This will also remove
 * the markers if the file is open in the editor. Note this won't remove the
 * breakpoints from the backend.
 */
void RemoveAllLocalBreakpoints(Project::File* file);

/**
 * Matches up a project file to a script file.
 */
Project::File* GetFileMatchingSource(const wxFileName& fileName, const std::string& source);

/**
 * Updates the line mapping for script file source based on the a diff with a disk file.
 */
void UpdateScriptLineMappingFromFile(const Project::File* file, DebugFrontend::Script* script);


#endif
