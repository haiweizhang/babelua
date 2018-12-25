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

#include "Config.h"
#include "MainFrame.h"
#include "DebugFrontend.h"
#include "StlUtility.h"
#include "XmlUtility.h"
#include "Project.h"
#include "DebugEvent.h"
#include "Symbol.h"
#include "Tokenizer.h"

#include <wx/txtstrm.h>
#include <wx/xml/xml.h>
#include <wx/fontdlg.h>
#include <wx/file.h>
#include <wx/dir.h>
#include <wx/wfstream.h>
#include <wx/string.h>
#include <wx/numdlg.h>
#include <wx/sstream.h>

#include <shlobj.h>

#include <algorithm>
#include <hash_map>
#include "CriticalSectionLock.h"

#include "../Decoda.h"
#include "../CrashDump.h"

HANDLE g_eventThread = NULL;
Project *m_project = new Project;
unsigned int m_vm = 0;
std::vector<unsigned int> m_vms;
unsigned int m_stackLevel = 0;
DWORD g_dwThreadId = GetCurrentThreadId();

extern bool g_bStartDebug;
extern CBreakPoints g_initBreakPoints;

extern CallbackEventAttachToProcess g_CallbackEventAttachToProcess;
extern CallbackEventInitialize g_CallbackEventInitialize;
extern CallbackEventCreateVM g_CallbackEventCreateVM;
extern CallbackEventDestroyVM g_CallbackEventDestroyVM;
extern CallbackEventLoadScript g_CallbackEventLoadScript;
extern CallbackEventBreak g_CallbackEventBreak;
extern CallbackEventSetBreakpoint g_CallbackEventSetBreakpoint;
extern CallbackEventException g_CallbackEventException;
extern CallbackEventLoadError g_CallbackEventLoadError;
extern CallbackEventMessage g_CallbackEventMessage;
extern CallbackEventSessionEnd g_CallbackEventSessionEnd;
extern CallbackEventNameVM g_CallbackEventNameVM;
extern CallbackEventLuaPrint g_CallbackEventLuaPrint;


CBreakPoint::CBreakPoint()
{
	m_line = 0;
}

CBreakPoint::CBreakPoint(wxString file,int line)
{
	m_file = file;
	m_line = line;
}

CBreakPoint::~CBreakPoint()
{
}

wxString CBreakPoint::GetFile()
{
	return m_file;
}

int CBreakPoint::GetLine()
{
	return m_line;
}


void CBreakPoints::Clear()
{
	CriticalSectionLock lock(m_criticalSection);

	m_breakpoints.clear();
}

void CBreakPoints::AddBreakPoint(wxString file,int line)
{
	CriticalSectionLock lock(m_criticalSection);

	m_breakpoints.push_back(CBreakPoint(file,line));
}

void CBreakPoints::RemoveBreakPoint(wxString file,int line)
{
	CriticalSectionLock lock(m_criticalSection);

	std::vector<CBreakPoint>::iterator iter;
	for(iter = m_breakpoints.begin(); iter != m_breakpoints.end(); )
	{
		bool bErase = false;
		if(::IsEqualFilePath(file,(*iter).GetFile()) && (line == (*iter).GetLine()))
		{
			iter = m_breakpoints.erase(iter);
			bErase = true;
		}
		if(!bErase)
		{
			iter++;
		}
	}
}

void CBreakPoints::GetLines(wxString file,std::vector<int> &lines)
{
	CriticalSectionLock lock(m_criticalSection);

	lines.clear();

	for(unsigned i=0; i<m_breakpoints.size(); i++)
	{
		if(::IsEqualFilePath(file,m_breakpoints[i].GetFile()))
		{
			lines.push_back(m_breakpoints[i].GetLine());
		}
	}
}


CWriteDebugEvent::CWriteDebugEvent()
{
	m_bWrite = FALSE;
}

CWriteDebugEvent::~CWriteDebugEvent()
{
}

void CWriteDebugEvent::WriteDebugEvent(const wxDebugEvent& event)
{
	if(!m_bWrite)
	{
		wxString log = GetDebugEvent(event);
		WriteDecodaLog(log);
	}
	m_bWrite = TRUE;
}

void CWriteDebugEvent::WriteDebugEvent(const wxDebugEvent& event,const wxString &file)
{
	if(!m_bWrite)
	{
		wxString log = GetDebugEvent(event)+" "+wxString::Format("file=%s",file.c_str());
		WriteDecodaLog(log);
	}
	m_bWrite = TRUE;
}

wxString CWriteDebugEvent::GetDebugEvent(const wxDebugEvent& event)
{
	wxString debugEvent = wxString::Format("eventId=%d vm=%d scriptIndex=%d line=%d enabled=%d message=%s messageType=%d",
		event.GetEventId(),event.GetVm(),event.GetScriptIndex(),event.GetLine(),
		event.GetEnabled(),event.GetMessage(),event.GetMessageType());
	return debugEvent;
}


class CInitDistory
{
public:
	CInitDistory()
	{
		SetUnhandledExceptionFilter(CrashFunction);
	}
	~CInitDistory()
	{
		if(m_project != NULL)
		{
			delete m_project;
			m_project = NULL;
		}

		DebugFrontend::Destroy();
	}
};
CInitDistory g_initdistory;


HINSTANCE g_hCurrentInstanceHandle = NULL;
void SetCurrentInstanceHandle(HINSTANCE hInstance)
{
	g_hCurrentInstanceHandle = hInstance;
}

//#include <afxstat_.h>
HINSTANCE GetCurrentInstanceHandle()
{
	return g_hCurrentInstanceHandle;
//	return AfxGetStaticModuleState()->m_hCurrentInstanceHandle;
}

wxString PathForwardslashToBackslash(wxString path)
{
	wxString newPath = path;
	newPath.Replace("/","\\");
	return newPath;
}

BOOL CreateDirectory(wxString directory)
{
	return CreateDirectory(directory,NULL);
}

void CreateMultipleDirectory(wxString directory)
{
	if(directory.Length() <= 0)
		return;
	if(directory.Right(1) == '\\')
	{
		directory = directory.Left(directory.Length()-1);
	}
	if(GetFileAttributes(directory) != -1)
		return;

	int iIndex = directory.rfind('\\');

	CreateMultipleDirectory(directory.Left(iIndex));
	CreateDirectory(directory,NULL);
}

BOOL IsDirectory(wxString path)
{
	DWORD dwAttributes = GetFileAttributes(path);
	if(dwAttributes == -1)
		return FALSE;
	if((dwAttributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY)
		return TRUE;
	else
		return FALSE;
}

BOOL IsDirectoryExist(wxString directory)
{
	return IsDirectory(directory);
}

#include <Shlobj.h>
wxString GetBabeLuaDirectory()
{
	TCHAR szDocumentPath[MAX_PATH] = {0};
	SHGetSpecialFolderPath(NULL,szDocumentPath,CSIDL_PERSONAL,0);
	wxString documentPath = szDocumentPath;
	documentPath += "\\BabeLua";
	return documentPath;
}

CWCharArr UTF8ToUnicode(const char* str)
{
	int iLen = MultiByteToWideChar(CP_UTF8,0,str,-1,NULL,0);
	WCHAR *pchData = new WCHAR[iLen];
	if(pchData != NULL)
	{
		MultiByteToWideChar(CP_UTF8,0,str,(int)strlen(str),pchData,iLen);
		pchData[iLen-1] = '\0';
	}

	CWCharArr wcharArr;
	if(pchData != NULL)
	{
		wcharArr = pchData;

		delete []pchData;
		pchData = NULL;
	}
	return wcharArr;
}

void OnDebugEvent(wxDebugEvent& event)
{
    char vmText[256];
    sprintf(vmText, "0x%08x: ", event.GetVm());

	CWriteDebugEvent writeDebugEvent;

    switch (event.GetEventId())
    {
    case EventId_LoadScript:
        {
            // Sync up the breakpoints for this file.
            unsigned int scriptIndex = event.GetScriptIndex();

            Project::File* file = m_project->GetFileForScript(scriptIndex);
            if (file == NULL)
            {
                // Check to see if one of the existing files' contents match this script.
                DebugFrontend::Script* script = DebugFrontend::Get().GetScript(scriptIndex);
                file = GetFileMatchingSource( wxFileName(DebugFrontend::Get().GetScript(scriptIndex)->name), script->source );
            
                if (file != NULL)
                {
                    // Map lines in case the loaded script is different than what we have on disk.
                    UpdateScriptLineMappingFromFile(file, script);
                }
            }

            if (file == NULL)
            {
                // Add the file to the project as a temporary file so that it's
                // easy for the user to add break points.
                file = m_project->AddTemporaryFile(scriptIndex);
                file->type = "Lua";
//                UpdateForNewFile(file);
            }
            else
            {
                // Check that we haven't already assigned this guy an index. If
                // we have, overwriting it will cause the previous index to no
                // longer exist in our project.
                assert(file->scriptIndex == -1);
            }

            if (file != NULL)
            {
                // The way this mechanism works, the front end sends all of the breakpoints
                // to the backend, and then the backend sends back commands to enable breakpoints
                // on the valid lines. So, we make a temporary copy of the breakpoints and clear
                // out our stored breakpoints before proceeding.

                std::vector<unsigned int> breakpoints = file->breakpoints;
                RemoveAllLocalBreakpoints(file);

                DebugFrontend::Script* script = DebugFrontend::Get().GetScript(scriptIndex);
                
                file->scriptIndex = scriptIndex;
/*
                for (unsigned int i = 0; i < breakpoints.size(); ++i)
                {
                    unsigned int newLine = breakpoints[i];
                    unsigned int oldLine = script->lineMapper.GetOldLine(newLine);
                    // If a line is changed, the breakpoint will be removed.
                    // Note, since we removed all breakpoints from the file already, if we
                    // don't add a breakpoint back it will be automatically deleted.
                    if (oldLine != LineMapper::s_invalidLine)
                    {
                        DebugFrontend::Get().ToggleBreakpoint(event.GetVm(), scriptIndex, oldLine);
                    }
                }
*/
				std::vector<int> lines;
				g_initBreakPoints.GetLines(::GetFileFullPath(file),lines);
				if(lines.size() > 0)
				{
					WriteDecodaLog(wxString::Format("InitBreakPoints:%s",file->fileName.GetFullPath().c_str()));
				}
				for(unsigned int i=0; i<lines.size(); i++)
				{
					InitSetBreakpoint(::GetFileFullPath(file),lines[i]);
				}

				if(g_CallbackEventLoadScript != NULL)
				{
					bool bRelative = false;
					wxString fullPath = ::GetFileFullPath(file,&bRelative);
					g_CallbackEventLoadScript(g_dwThreadId,fullPath,file->scriptIndex,bRelative);
				}

				writeDebugEvent.WriteDebugEvent(event,file->fileName.GetFullPath());
            }

            // Tell the backend we're done processing this script for loading.
            DebugFrontend::Get().DoneLoadingScript(event.GetVm());

        }
        break;
    case EventId_CreateVM:
		{
			if(g_CallbackEventCreateVM != NULL)
			{
				g_CallbackEventCreateVM(g_dwThreadId,event.GetVm());
			}
			OutputMessage(MessageType_Normal,wxString::Format("%sVM created", vmText));
//        m_output->OutputMessage(wxString::Format("%sVM created", vmText));
	        AddVmToList(event.GetVm());
		}
        break;
    case EventId_DestroyVM:
		{
			if(g_CallbackEventDestroyVM != NULL)
			{
				g_CallbackEventDestroyVM(g_dwThreadId,event.GetVm());
			}
			OutputMessage(MessageType_Normal,wxString::Format("%sVM destroyed", vmText));
//        m_output->OutputMessage(wxString::Format("%sVM destroyed", vmText));
	        RemoveVmFromList(event.GetVm());
		}
        break;
    case EventId_Break:
		{
			unsigned int scriptIndex = event.GetScriptIndex();
			unsigned int breakLine   = event.GetLine();
			Project::File* file = m_project->GetFileForScript(scriptIndex);
			if(file != NULL)
			{
				if(g_CallbackEventBreak != NULL)
				{
					g_CallbackEventBreak(g_dwThreadId,::GetFileFullPath(file),breakLine);
				}
				writeDebugEvent.WriteDebugEvent(event,file->fileName.GetFullPath());
			}
/*
			UpdateForNewState();

			// Bring ourself to the top of the z-order.
			BringToFront();

			ClearBreakLineMarker();

			m_breakScriptIndex  = event.GetScriptIndex();
			m_breakLine         = event.GetLine();
*/
			unsigned int stackLevel = 0;

			// Set the VM the debugger is working with to the one that this event came
			// from. Note this will update the watch values.
			SetContext(event.GetVm(), stackLevel);
		}
        break;
    case EventId_SetBreakpoint:
        {
            unsigned int scriptIndex = event.GetScriptIndex();
            Project::File* file = m_project->GetFileForScript(scriptIndex);
            unsigned int newLine = OldToNewLine(file, event.GetLine());
//            m_project->SetBreakpoint(scriptIndex, newLine, event.GetEnabled());

			if(file != NULL)
			{
				if(g_CallbackEventSetBreakpoint != NULL)
				{
					g_CallbackEventSetBreakpoint(g_dwThreadId,::GetFileFullPath(file),newLine,event.GetEnabled());
				}
				writeDebugEvent.WriteDebugEvent(event,file->fileName.GetFullPath());
			}
        }
        break;
    case EventId_Exception:
        {
			wxString target;
			unsigned int line;
			wxString error;
			if(ParseMessage(event.GetMessage(),target,line,error))
			{
				if(g_CallbackEventException != NULL)
				{
					g_CallbackEventException(g_dwThreadId,::PathToFullPath(target),line-1,error);
				}
			}
			else
			{
				if(g_CallbackEventException != NULL)
				{
					g_CallbackEventException(g_dwThreadId,"",0,event.GetMessage());
				}
			}
/*
            // Add the exception to the output window.
            m_output->OutputError(event.GetMessage());

            // Check if we're ignoring this exception.

            ExceptionDialog dialog(this, event.GetMessage(), true);
            int result = dialog.ShowModal();

            if (result == ExceptionDialog::ID_Ignore)
            {
                // Resume the backend.
                DebugFrontend::Get().Continue(m_vm);
                UpdateForNewState();
            }
            else if (result == ExceptionDialog::ID_IgnoreAlways)
            {
                DebugFrontend::Get().IgnoreException(event.GetMessage().ToAscii());
                DebugFrontend::Get().Continue(m_vm);
                UpdateForNewState();
            }
*/            
        }
        break;
    case EventId_LoadError:
		{
			wxString target;
			unsigned int line;
			wxString error;
			if(ParseMessage(event.GetMessage(),target,line,error))
			{
				if(g_CallbackEventLoadError != NULL)
				{
					g_CallbackEventLoadError(g_dwThreadId,::PathToFullPath(target),line-1,error);
				}
			}
			else
			{
				if(g_CallbackEventLoadError != NULL)
				{
					g_CallbackEventLoadError(g_dwThreadId,"",0,event.GetMessage());
				}
			}
		}
        break;
    case EventId_SessionEnd:
		{
/*
			ClearBreakLineMarker();
			ClearCurrentLineMarker();
*/
			// Check if all of the VMs have been closed.
			if (!m_vms.empty())
			{
				OutputMessage(MessageType_Warning,"Warning 1003: Not all virtual machines were destroyed");
//				m_output->OutputWarning("Warning 1003: Not all virtual machines were destroyed");
			}

			if(g_CallbackEventSessionEnd != NULL)
			{
				g_CallbackEventSessionEnd(g_dwThreadId);
			}

			m_project->CleanUpAfterSession();
			CleanUpTemporaryFiles();

			// Clean up after the debugger.
			DebugFrontend::Get().Shutdown();

//			UpdateForNewState();

			SetContext(0, 0);
			m_vms.clear();
/*			m_vmList->DeleteAllItems();

			SetMode(Mode_Editing);
			m_output->OutputMessage("Debugging session ended");
*/
			OutputMessage(MessageType_Normal,"Debugging session ended");

			g_bStartDebug = false;
		}
        break;
    case EventId_Message:
		{
			wxString message = event.GetMessage();
			if(event.GetMessageType() == MessageType_Normal)
			{
				if(message == "Debugger attached to process")
				{
					if(g_CallbackEventAttachToProcess != NULL)
					{
						g_CallbackEventAttachToProcess(g_dwThreadId);
					}
				}
			}

			if(message.Find("[LUA print]") == 0)
			{
				if(g_CallbackEventLuaPrint != NULL)
				{
					CWCharArr wcharMessage = UTF8ToUnicode(message);
					g_CallbackEventLuaPrint(g_dwThreadId,wcharMessage);
				}
			}
			else
			{
				OutputMessage(event.GetMessageType(),event.GetMessage());
			}
		}
//        OnMessage(event);
        break;
    case EventId_NameVM:
		{
			if(g_CallbackEventNameVM != NULL)
			{
				g_CallbackEventNameVM(g_dwThreadId,event.GetVm(),event.GetMessage());
			}
		}
//        SetVmName(event.GetVm(), event.GetMessage());
        break;
	default:
		break;
    }

	writeDebugEvent.WriteDebugEvent(event);
}


bool IsEqualFilePath(wxString path1,wxString path2)
{
	wxString fullPath1 = PathToFullPath(path1);
	wxString fullPath2 = PathToFullPath(path2);

	return (fullPath1.CmpNoCase(fullPath2) == 0);
}

#include <Shlwapi.h>
#pragma comment(lib,"Shlwapi.lib")
extern wxString g_scriptsDirectory;
wxString PathToFullPath(wxString path)
{
	if(::PathIsRelative(path.c_str()))
	{
		wxString workingDirectory = g_scriptsDirectory;
		if(workingDirectory.Right(1) != "\\")
			workingDirectory += "\\";

		path = workingDirectory+path;
	}

	char szFullPath[MAX_PATH];
	char* pFullPath = (char*)szFullPath;
	char **lppPart = &pFullPath;
	::GetFullPathNameA(path.c_str(),MAX_PATH,szFullPath,lppPart);
	
	wxString fullPath = szFullPath;
	return fullPath;
}

//如果是相对路径文件则在前面加上scripts目录
wxString GetFileFullPath(Project::File *file,bool *pRelative)
{
	if(pRelative != NULL)
	{
		*pRelative = false;
	}
	wxString wxFullPath;
	if(file != NULL)
	{
		if(file->fileName.IsRelative())
		{
			wxString workingDirectory = g_scriptsDirectory;
			if(workingDirectory.Right(1) != "\\")
				workingDirectory += "\\";

			wxFullPath = workingDirectory+file->fileName.GetFullPath();
			if(pRelative != NULL)
			{
				*pRelative = true;
			}
		}
		else
			wxFullPath = file->fileName.GetFullPath();
	}
	return PathToFullPath(wxFullPath);
}

CriticalSection g_breakCriticalSection;
bool IsFileSetBreakpoint(int scriptIndex,int line)
{
	CriticalSectionLock lock(g_breakCriticalSection);

	Project::File* file = m_project->GetFileForScript(scriptIndex);
	if(file == NULL)
		return false;

    std::vector<unsigned int>::iterator iterator;
    iterator = std::find(file->breakpoints.begin(), file->breakpoints.end(), line);

    if (iterator == file->breakpoints.end())
		return false;

	return true;
}

bool FileSetBreakpoint(int scriptIndex,int line,bool set)
{
	CriticalSectionLock lock(g_breakCriticalSection);

    m_project->SetBreakpoint(scriptIndex, line, set);
	DebugFrontend::Get().ToggleBreakpoint(m_vm, scriptIndex, line);

	return true;
}

//LoadScript时设置Init断点调用
void InitSetBreakpoint(const char *fullPath,int line)
{
	int scriptIndex = GetScriptIndex(fullPath);
	wxString log = wxString::Format("InitSetBreakpoint:scriptIndex=%d,line=%d fullPath=%s",scriptIndex,line,fullPath);
	WriteDecodaLog(log);
	if(scriptIndex != -1)
	{
		if(!IsFileSetBreakpoint(scriptIndex,line))
		{
			WriteDecodaLog("!IsSetBreakpoint");
            FileSetBreakpoint(scriptIndex, line, true);
//			DebugFrontend::Get().ToggleBreakpoint(m_vm, scriptIndex, line);
		}
	}
}

void OutputMessage(int msgType,const char *msg)
{
	if(g_CallbackEventMessage != NULL)
	{
		g_CallbackEventMessage(g_dwThreadId,msgType,"",0,msg);
	}
}

void OutputMessage(int msgType,const char *fullPath,int line,const char *msg)
{
	if(g_CallbackEventMessage != NULL)
	{
		g_CallbackEventMessage(g_dwThreadId,msgType,::PathToFullPath(fullPath),line,msg);
	}
}

unsigned int OldToNewLine(Project::File* file, unsigned int oldLine)
{
	if(file == NULL)
		return oldLine;

    DebugFrontend::Script* script = DebugFrontend::Get().GetScript(file->scriptIndex);
    wxASSERT(script != NULL);

    if (script == NULL)
    {
        // This file isn't being debugged, so we don't need to map lines.
        return oldLine;
    }
    else
    {
        return script->lineMapper.GetNewLine( oldLine );
    }
}

bool ParseHelpMessage(const wxString& message, wxString& topic)
{
    int topicId;
    if (sscanf(message.c_str(), "Warning %d :", &topicId) == 1)
    {
        topic.Printf("Decoda_warning_%d.html", topicId);
        return true;
    }
    else if (sscanf(message.c_str(), "Error %d :", &topicId) == 1)
    {
        topic.Printf("Decoda_error_%d.html", topicId);
        return true;
    }
    return false;
}

bool ParseLuaErrorMessage(const wxString& error, wxString& fileName, unsigned int& line, wxString& message)
{
    // Error messages have the form "filename:line: message"
    fileName = error;
    fileName.Trim(false);

    int fileNameEnd;
    if (fileName.Length() >= 3 && isalpha(fileName[0]) && fileName[1] == ':' && wxIsPathSeparator(fileName[2]))
    {
        // The form appears to have a drive letter in front of the path.
        fileNameEnd = fileName.find(':', 3);
    }
    else
    {
        fileNameEnd = fileName.find(':');
    }

    if (fileNameEnd == wxNOT_FOUND)
    {
        return false;
    }

	message.resize(error.Length()+1);
    if (sscanf(fileName.c_str() + fileNameEnd, ":%d:%[^\0]", &line, message) >= 1)
    {
        fileName = fileName.Left(fileNameEnd);
        return true;
    }
    return false;
}

bool ParseLuacErrorMessage(const wxString& error, wxString& fileName, unsigned int& line, wxString& message)
{
    // "appname: filename:line: message"
    int appNameEnd = error.Find(wxT(": "));
    if (appNameEnd == wxNOT_FOUND)
    {
        return false;
    }

    wxString temp = error.Right(error.Length() - appNameEnd - 1);
    return ParseLuaErrorMessage(temp, fileName, line, message);
}

bool ParseErrorMessage(const wxString& error, wxString& fileName, unsigned int& line, wxString& message)
{
    if (ParseLuaErrorMessage (error, fileName, line, message) ||
        ParseLuacErrorMessage(error, fileName, line, message))
    {
		// Check if the target stars with "...". Luac does this if the file name is too long.
        // In that case, we find the closest matching file in the project.
        if ( fileName.StartsWith(wxT("...")) )
        {
            bool foundMatch = false;
            wxString partialName = wxFileName(fileName.Mid(3)).GetFullPath();

            for (unsigned int fileIndex = 0; fileIndex < m_project->GetNumFiles() && !foundMatch; ++fileIndex)
            {
                Project::File* file = m_project->GetFile(fileIndex);
                wxString fullName = file->fileName.GetFullPath();

                if (fullName.EndsWith(partialName))
                {
                    fileName = fullName;
                    foundMatch = true;
                }
            }
        }
        return true;
    }
    return false;
}

bool ParseMessage(const wxString& error, wxString& target, unsigned int& line,wxString &message)
{
	if (ParseHelpMessage(error, target))
	{
//		m_helpController.DisplaySection(target);
		target = "";
		line = 0;
		message = error;

		return true;
	}
	else if (ParseErrorMessage(error, target, line, message))
	{
		return true;
	}
	return false;
}

int GetScriptIndex(const char *fullPath)
{
	if(m_project == NULL)
		return -1;

    for (unsigned int i = 0; i < m_project->GetNumFiles(); ++i)
    {
        Project::File* file = m_project->GetFile(i);
		if(file == NULL)
			continue;

		if (::IsEqualFilePath(file->fileName.GetFullPath(),fullPath))
        {
            return file->scriptIndex;
        }
		else if(file->fileName.IsRelative())
		{
			wxString wxFullPath = ::GetFileFullPath(file);
			if(::IsEqualFilePath(wxFullPath,fullPath))
			{
				return file->scriptIndex;
			}
		}
    }
	return -1;
}

void SetContext(unsigned int vm, unsigned int stackLevel)
{
    m_vm = vm;
    m_stackLevel = stackLevel;
/*
    m_watch->SetContext(m_vm, m_stackLevel);
    m_watch->UpdateItems();

    // Update the selection in the VM list.

    m_vmList->ClearAllIcons();
    unsigned int vmIndex = std::find(m_vms.begin(), m_vms.end(), vm) - m_vms.begin();

    if (vmIndex < m_vms.size())
    {
        m_vmList->SetItemIcon(vmIndex, ListWindow::Icon_YellowArrow);
    }

    // Update the icons in the call stack.

    m_callStack->ClearAllIcons();

    if (stackLevel < static_cast<unsigned int>(m_callStack->GetItemCount()))
    {
        m_callStack->SetItemIcon(0, ListWindow::Icon_YellowArrow);
        if (stackLevel != 0)
        {
            m_callStack->SetItemIcon(stackLevel, ListWindow::Icon_GreenArrow);
        }
    }
*/
}

void AddVmToList(unsigned int vm)
{
    assert(std::find(m_vms.begin(), m_vms.end(), vm) == m_vms.end());
    m_vms.push_back(vm);
/*
    char vmText[256];
    sprintf(vmText, "0x%08x", vm);

    m_vmList->Append(vmText);*/
}

void RemoveVmFromList(unsigned int vm)
{
    std::vector<unsigned int>::iterator iterator;
    iterator = std::find(m_vms.begin(), m_vms.end(), vm);

    if (iterator != m_vms.end())
    {
//        unsigned int index = iterator - m_vms.begin();
//        m_vmList->DeleteItem(index);
        m_vms.erase(iterator);
    }
}

void CleanUpTemporaryFiles()
{
    std::vector<Project::File*> files;

    for (unsigned int i = 0; i < m_project->GetNumFiles(); ++i)
    {
        Project::File* file = m_project->GetFile(i);
		if(file == NULL)
			continue;

        if (file->temporary /*&& GetOpenFileIndex(file) == -1*/)
        {
            files.push_back(file);
        }
    }
//    m_projectExplorer->RemoveFiles(files);

    for (unsigned int i = 0; i < files.size(); ++i)
    {
        m_project->RemoveFile(files[i]);
    }
//    m_breakpointsWindow->UpdateBreakpoints();
}

void RemoveAllLocalBreakpoints(Project::File* file)
{
	if(file == NULL)
		return;
/*
    unsigned int openIndex = GetOpenFileIndex(file);

    if (openIndex != -1)
    {
        for (unsigned int i = 0; i < file->breakpoints.size(); ++i)
        {
            UpdateFileBreakpoint(m_openFiles[openIndex], file->breakpoints[i], false);
        }
    }
*/
    file->breakpoints.clear();
//    m_breakpointsWindow->UpdateBreakpoints(file);
}

Project::File* GetFileMatchingSource(const wxFileName& fileName, const std::string& source)
{
	if(m_project == NULL)
		return NULL;

    for (unsigned int i = 0; i < m_project->GetNumFiles(); ++i)
    {
        Project::File* file = m_project->GetFile(i);
        if(file == NULL)
			continue;

        if (file->scriptIndex == -1 && file->fileName.GetFullName().CmpNoCase(fileName.GetFullName()) == 0)
        {
            return file;
        }
    }
    return NULL;
}

void UpdateScriptLineMappingFromFile(const Project::File* file, DebugFrontend::Script* script)
{
	if(file == NULL)
		return;

    if (file->fileName.FileExists())
    {
        // Read the file from disk.
        wxFile diskFile(file->fileName.GetFullPath());

        if (diskFile.IsOpened())
        {
            unsigned int diskFileSize = file->fileName.GetSize().GetLo();
            char* diskFileSource = new char[diskFileSize + 1];

            diskFileSize = diskFile.Read(diskFileSource, diskFileSize);
            diskFileSource[diskFileSize] = 0; 
            
            script->lineMapper.Update(script->source, diskFileSource);

            delete [] diskFileSource;
            diskFileSource = NULL;
        }
    }
}
