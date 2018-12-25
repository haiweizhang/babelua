#include "stdafx.h"
#include "Decoda.h"
#include "CriticalSectionLock.h"

#include <algorithm>

extern unsigned int m_vm;
extern Project *m_project;
extern unsigned int m_stackLevel;


CriticalSection g_writeLogCriticalSection;
//CLog g_log(GetBabeLuaDirectory());
bool g_bWriteLog = false;
void SetWriteLog(int iEnable)
{
	g_bWriteLog = (iEnable != 0);
}

void SetDecodaLogMaxFileSize(int maxFileSize)
{
//	g_log.SetDecodaLogMaxFileSize(maxFileSize);
}

void SetVsLogMaxFileSize(int maxFileSize)
{
//	g_log.SetVsLogMaxFileSize(maxFileSize);
}

int WriteDecodaLog(const char *log)
{
	CriticalSectionLock lock(g_writeLogCriticalSection);
	if(g_bWriteLog)
	{
		wxString directory = GetBabeLuaDirectory();
		if(!IsDirectoryExist(directory))
			CreateMultipleDirectory(directory);

//		g_log.DecodaLog(log);
		return 0;
	}
	return -1;
}

int WritePackageLog(const char *log)
{
	CriticalSectionLock lock(g_writeLogCriticalSection);
	if(g_bWriteLog)
	{
		wxString directory = GetBabeLuaDirectory();
		if(!IsDirectoryExist(directory))
			CreateMultipleDirectory(directory);

//		g_log.VsLog(log);
		return 0;
	}
	return -1;
}

#include <fstream>
int WriteLog(const char *filePath,const char *log)
{
	SYSTEMTIME systemTime;
	GetLocalTime(&systemTime);

	wxString logString = wxString::Format("[%04d-%02d-%02d %02d:%02d:%02d] %s\r\n",systemTime.wYear,systemTime.wMonth,systemTime.wDay,systemTime.wHour,systemTime.wMinute, systemTime.wSecond,log);

	wxString file = filePath;
	std::ofstream output;
    output.open(file, std::ios::ate | std::ios::app | std::ios::binary);
	if(output.is_open())
	{
		output.seekp(0, std::ios::end);
		output.write(logString.c_str(),logString.Length());
		output.close();
	}

	return 0;
}



CallbackEventAttachToProcess g_CallbackEventAttachToProcess = NULL;
CallbackEventInitialize g_CallbackEventInitialize = NULL;
CallbackEventCreateVM g_CallbackEventCreateVM = NULL;
CallbackEventDestroyVM g_CallbackEventDestroyVM = NULL;
CallbackEventLoadScript g_CallbackEventLoadScript = NULL;
CallbackEventBreak g_CallbackEventBreak = NULL;
CallbackEventSetBreakpoint g_CallbackEventSetBreakpoint = NULL;
CallbackEventException g_CallbackEventException = NULL;
CallbackEventLoadError g_CallbackEventLoadError = NULL;
CallbackEventMessage g_CallbackEventMessage = NULL;
CallbackEventSessionEnd g_CallbackEventSessionEnd = NULL;
CallbackEventNameVM g_CallbackEventNameVM = NULL;
CallbackEventLuaPrint g_CallbackEventLuaPrint = NULL;

void SetCallbackEventAttachToProcess(CallbackEventAttachToProcess callbackFunction)
{
	g_CallbackEventAttachToProcess = callbackFunction;
}

void SetCallbackEventInitialize(CallbackEventInitialize callbackFunction)
{
	g_CallbackEventInitialize = callbackFunction;
}

void SetCallbackEventCreateVM(CallbackEventCreateVM callbackFunction)
{
	g_CallbackEventCreateVM = callbackFunction;
}

void SetCallbackEventDestroyVM(CallbackEventDestroyVM callbackFunction)
{
	g_CallbackEventDestroyVM = callbackFunction;
}

void SetCallbackEventLoadScript(CallbackEventLoadScript callbackFunction)
{
	g_CallbackEventLoadScript = callbackFunction;
}

void SetCallbackEventBreak(CallbackEventBreak callbackFunction)
{
	g_CallbackEventBreak = callbackFunction;
}

void SetCallbackEventSetBreakpoint(CallbackEventSetBreakpoint callbackFunction)
{
	g_CallbackEventSetBreakpoint = callbackFunction;
}

void SetCallbackEventException(CallbackEventException callbackFunction)
{
	g_CallbackEventException = callbackFunction;
}

void SetCallbackEventLoadError(CallbackEventLoadError callbackFunction)
{
	g_CallbackEventLoadError = callbackFunction;
}

void SetCallbackEventMessage(CallbackEventMessage callbackFunction)
{
	g_CallbackEventMessage = callbackFunction;
}

void SetCallbackEventSessionEnd(CallbackEventSessionEnd callbackFunction)
{
	g_CallbackEventSessionEnd = callbackFunction;
}

void SetCallbackEventNameVM(CallbackEventNameVM callbackFunction)
{
	g_CallbackEventNameVM = callbackFunction;
}

void SetCallbackEventLuaPrint(CallbackEventLuaPrint callbackFunction)
{
	g_CallbackEventLuaPrint = callbackFunction;
}


void StartProcess(const wxString& command, const wxString& commandArguments, const wxString& workingDirectory, const wxString& symbolsDirectory, bool debug, bool startBroken)
{
    if (!DebugFrontend::Get().Start(command, commandArguments, workingDirectory, symbolsDirectory, debug, startBroken))
    {
		OutputMessage(MessageType_Error,"Error starting process");
//        wxMessageBox("Error starting process", s_applicationName, wxOK | wxICON_ERROR, this);
    }
    else if (debug)
    {
		OutputMessage(MessageType_Normal,"Debugging session started");
/*        SetMode(Mode_Debugging);

        m_output->OutputMessage("Debugging session started");
        if (m_attachToHost)
        {
            DebugFrontend::Get().AttachDebuggerToHost();
        }*/
    }
}

wxString g_command;
wxString g_commandArguments;
wxString g_workingDirectory;
wxString g_symbolsDirectory;
wxString g_scriptsDirectory;

void StartProcess(bool debug, bool startBroken)
{
    wxString command = g_command;
    wxString commandArguments = g_commandArguments;
    wxString workingDirectory = g_workingDirectory;
    wxString symbolsDirectory = g_symbolsDirectory;

    if (!command.IsEmpty())
    {
        StartProcess(command, commandArguments, workingDirectory, symbolsDirectory, debug, startBroken);
    }
}

bool g_bStartDebug = false;
unsigned int StartProcess(const char *command,const char *commandArguments,const char *workingDirectory,const char *symbolsDirectory,const char *scriptsDirectory)
{
	g_bStartDebug = true;

	g_command = command;
	g_commandArguments = commandArguments;
	g_workingDirectory = workingDirectory;
	g_symbolsDirectory = symbolsDirectory;
	g_scriptsDirectory = scriptsDirectory;

	DebugStart();

	unsigned int processId = DebugFrontend::Get().GetProcessId();
	return processId;
}

void DebugStart()
{
	WriteDecodaLog("DebugStart");
    if (DebugFrontend::Get().GetState() == DebugFrontend::State_Inactive)
    {
        StartProcess(true, false);
    }
    else
    {
        // The user wants to continue since we're already running.
        DebugFrontend::Get().Continue(m_vm);
    }
}

void DebugStop()
{
	WriteDecodaLog("DebugStop");
    DebugFrontend::Get().Stop(false);
}

void StepInto()
{
	WriteDecodaLog("StepInto");
    if (DebugFrontend::Get().GetState() == DebugFrontend::State_Inactive)
    {
        StartProcess(true, true);
    }
    else
    {
        DebugFrontend::Get().StepInto(m_vm);
    }
//    UpdateForNewState();
}

void StepOver()
{
	WriteDecodaLog("StepOver");
    DebugFrontend::Get().StepOver(m_vm);
//    UpdateForNewState();
}

void SetBreakpoint(const char *fullPath,int line)
{
	int scriptIndex = GetScriptIndex(fullPath);
	wxString log = wxString::Format("SetBreakpoint:scriptIndex=%d,line=%d fullPath=%s",scriptIndex,line,fullPath);
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
	else
	{
		//如果在Project中没有找到文件，则可能是该文件尚未require，则先添加到InitBreakpoint中，待LoadScript时设置断点
		AddInitBreakpoint(fullPath,line);
	}
}

void RemoveInitBreakpoint(const char *fullPath,int line);

extern CriticalSection g_breakCriticalSection;
void DisableBreakpoint(const char *fullPath,int line)
{
	CriticalSectionLock lock(g_breakCriticalSection);

	int scriptIndex = GetScriptIndex(fullPath);
	wxString log = wxString::Format("DisableBreakpoint:scriptIndex=%d,line=%d fullPath=%s",scriptIndex,line,fullPath);
	WriteDecodaLog(log);
	if(scriptIndex != -1)
	{
		if(IsFileSetBreakpoint(scriptIndex,line))
		{
			WriteDecodaLog("IsSetBreakpoint");
            FileSetBreakpoint(scriptIndex, line, false);
//			DebugFrontend::Get().ToggleBreakpoint(m_vm, scriptIndex, line);
		}
	}
	else
	{
		//如果在Project中没有找到文件，则可能是该文件尚未require，则先从InitBreakpoint中移除
		RemoveInitBreakpoint(fullPath,line);
	}
}

int GetNumStackFrames()
{
    DebugFrontend& frontend = DebugFrontend::Get();
    unsigned int numStackFrames = frontend.GetNumStackFrames();
	return numStackFrames;
}

char* CopyString(char *dest,int destLen,const char *source)
{
	memset(dest,0,destLen);
	memcpy(dest,source,min(destLen-1,(int)strlen(source)));
	return dest;
}

wchar_t* CopyString(wchar_t *dest,int destLen,const wchar_t *source)
{
	memset(dest,0,sizeof(wchar_t)*destLen);
	memcpy(dest,source,min(destLen-1,(int)(sizeof(wchar_t)*wcslen(source))));
	return dest;
}

void GetStackFrame(int stackFrameIndex,char *fullPath,int fullPathLen,char *fun,int funLen,int *line)
{
    DebugFrontend& frontend = DebugFrontend::Get();
    unsigned int numStackFrames = frontend.GetNumStackFrames();
	if(stackFrameIndex >= 0 && stackFrameIndex < (int)numStackFrames)
	{
        const DebugFrontend::StackFrame& stackFrame = frontend.GetStackFrame(stackFrameIndex);
        
        if (stackFrame.scriptIndex != -1)
        {
            Project::File* file = m_project->GetFileForScript(stackFrame.scriptIndex);
            unsigned int lineNumber = OldToNewLine(file, stackFrame.line);

            const DebugFrontend::Script* script = frontend.GetScript(stackFrame.scriptIndex);

			if(script != NULL)
			{
				wxString name = ::PathToFullPath(script->name);

				CopyString(fullPath,fullPathLen,name);//script->name.c_str());
				CopyString(fun,funLen,stackFrame.function.c_str());
				*line = lineNumber + 1;
			}
			else
			{
				CopyString(fun,funLen,stackFrame.function.c_str());
				*line = -1;
			}
        }
        else
        {
			CopyString(fun,funLen,stackFrame.function.c_str());
			*line = -1;
        }
    }
}

#include <wx/sstream.h>
#include <wx/xml/xml.h>
#include ".\Frontend\XmlUtility.h"

wxString GetTableAsText(wxXmlNode* root);
wxString GetNodeAsText(wxXmlNode* node, wxString& type);

wxString GetTableAsText(wxXmlNode* root)
{
    assert(root->GetName() == "table");

    int maxElements = 20;//4;
    // Add the elements of the table as tree children.
    wxString result = "{";
    wxString type;

    wxXmlNode* node = root->GetChildren();
    int numElements = 0;
    while (node != NULL)
    {
        if (node->GetName() == "element")
        {
            wxXmlNode* keyNode  = FindChildNode(node, "key");
            wxXmlNode* dataNode = FindChildNode(node, "data");

            if (keyNode != NULL && dataNode != NULL)
            {
                wxString key  = GetNodeAsText(keyNode->GetChildren(), type);
                wxString data = GetNodeAsText(dataNode->GetChildren(), type);
                if (numElements >= maxElements)
                {
                    result += "...";
                    break;
                }
                result += key + "=" + data + " ";
                ++numElements;
            }
        }
        node = node->GetNext();
    }
    result += "}";
    return result;
}

wxString GetNodeAsText(wxXmlNode* node, wxString& type)
{
    wxString text;
    if (node != NULL)
    {
        if (node->GetName() == "error")
        {
            ReadXmlNode(node, "error", text);
        }
        else if (node->GetName() == "table")
        {
            text = GetTableAsText(node);
        }
        else if (node->GetName() == "values")
        {
            wxXmlNode* child = node->GetChildren();
            while (child != NULL)
            {
                if (!text.IsEmpty())
                {
                    text += ", ";
                }
                wxString dummy;
                text += GetNodeAsText(child, dummy);
                child = child->GetNext();
            }
        }
        else if (node->GetName() == "value")
        {
            wxXmlNode* child = node->GetChildren();
            while (child != NULL)
            {
                ReadXmlNode(child, "type", type) ||
                ReadXmlNode(child, "data", text);
                child = child->GetNext();
            }
        }
        else if (node->GetName() == "function")
        {
            unsigned int scriptIndex = -1;
            unsigned int lineNumber  = -1;

            wxXmlNode* child = node->GetChildren();
            while (child != NULL)
            {
                ReadXmlNode(child, "script", scriptIndex);
                ReadXmlNode(child, "line",   lineNumber);
                child = child->GetNext();
            }

			text = "function";
            DebugFrontend::Script* script = DebugFrontend::Get().GetScript(scriptIndex);
            if (script != NULL)
            {
                text += " defined at ";
                text += script->name;
                text += ":";
                text += wxString::Format("%d", lineNumber + 1);
            }

            type = "function";
        }
    }
    return text;
}

bool GetExpression(wxXmlNode* root,wxString &dataType,wxString &dataValue,int *expandable)
{
    wxString type;
    wxString text = GetNodeAsText(root, type);
/*    
    // Remove any embedded zeros in the text. This happens if we're displaying a wide
    // string. Since we aren't using wide character wxWidgets, we cant' display that
    // properly, so we just hack it for roman text.

    bool englishWideCharacter = true;
    for (unsigned int i = 0; i < text.Length(); i += 2)
    {
        if (text[i] != 0)
        {
            englishWideCharacter = false;
        }
    }

    if (englishWideCharacter)
    {
        size_t convertedLength = WideCharToMultiByte(CP_UTF8, 0, (const wchar_t*)text.c_str(), text.Length() / sizeof(wchar_t), NULL, 0, 0, 0);

        char* result = new char[convertedLength + 1]; 
        convertedLength = WideCharToMultiByte(CP_UTF8, 0, (const wchar_t*)text.c_str(), text.Length() / sizeof(wchar_t), result, convertedLength, 0, 0);

        text = wxString(result, convertedLength);
    }
*/
	dataType = type;
	dataValue = text;

    if (root != NULL)
    {
        if (root->GetName() == "table")
        {
            wxXmlNode* node = root->GetChildren();
            while (node != NULL)
            {
                wxString typeName;
                if (ReadXmlNode(node, "type", typeName))
                {
                }
                else if (node->GetName() == "element")
                {
                    wxXmlNode* keyNode  = FindChildNode(node, "key");
                    wxXmlNode* dataNode = FindChildNode(node, "data");

                    if (keyNode != NULL && dataNode != NULL)
                    {
						*expandable = 1;
						break;
                    }
                }
                node = node->GetNext();
            }
        }
        else if (root->GetName() == "values")
        {
            wxXmlNode* node = root->GetChildren();
            while (node != NULL)
            {
				*expandable = 1;
				break;

                node = node->GetNext();
            }

        }
    }
    return true;
}

bool Evaluate(const wxString &expression,wxString &result)
{
    if (m_vm != 0)
    {
        if (!expression.empty())
        {
            std::string temp;
            bool bEvaluate = DebugFrontend::Get().Evaluate(m_vm, expression, m_stackLevel, temp);
            result = temp.c_str();
			return bEvaluate;
        }
	}
	return false;
}

bool IsValidVaralible(char ch)
{
	if(ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'))
		return true;
	else
		return false;
}

//修改从VS传入的expression名称 VS传入表达式中以.为分隔符
wxString ChangeExpressionName(const char *text)
{
	wxString expression = text;
	
//	expression.Replace(".[","[");	//例如：a.[1]转为a[1]

	bool bFind = false;
	wxString changeExpression;
	unsigned int i;
	for(i=0; i<expression.Length(); i++)
	{
		if(expression[i] == '.')
		{
			if(i < expression.Length()-1)
			{
				if(bFind)
				{
					changeExpression += "']";

					bFind = false;
				}

				if(::IsValidVaralible(expression[i+1]) || expression[i+1] == '[')	//例如 a.[1]不转换，在后面进行转换
				{
					changeExpression += expression[i];
				}
				else
				{
//					changeExpression += expression[i];
					changeExpression += "['";

					bFind = true;
				}
			}
			else
			{
				changeExpression += expression[i];
			}
		}
		else
		{
			changeExpression += expression[i];
		}
	}
	if(bFind)
	{
		changeExpression += "']";
	}

	changeExpression.Replace(".[","[");	//例如：a.[1]转为a[1]

	return changeExpression;
}

bool ExecuteText(int executeId,const wchar_t *wtext,wchar_t *wtype,int typeLen,wchar_t *wvalue,int valueLen,int *expandable)
{
	CCharArr text = wtext;

	wxString result;
	bool bEvaluate = Evaluate(ChangeExpressionName(text),result);
    if (!result.IsEmpty())
	{
        wxStringInputStream stream(result);
        wxXmlDocument document;

        if (document.Load(stream))
        {
			wxString dataType;
			wxString dataValue;
            GetExpression(document.GetRoot(),dataType,dataValue,expandable);

			CWCharArr wcharType = UTF8ToUnicode(dataType);
			CWCharArr wcharValue = UTF8ToUnicode(dataValue);
			CopyString(wtype,typeLen,wcharType);
			CopyString(wvalue,valueLen,wcharValue);
        }
    }
	return bEvaluate;
}

std::vector<wxString> g_expressions;

void GetExpressions(wxXmlNode* root,std::vector<wxString> &expressions)
{
    if (root != NULL)
    {
        if (root->GetName() == "table")
        {
            // Add the elements of the table as tree children.
            wxXmlNode* node = root->GetChildren();
            while (node != NULL)
            {
                wxString typeName;
                if (ReadXmlNode(node, "type", typeName))
                {
//                    SetItemText(item, 2, typeName);
                }
                else if (node->GetName() == "element")
                {
                    wxXmlNode* keyNode  = FindChildNode(node, "key");
                    wxXmlNode* dataNode = FindChildNode(node, "data");
                    if (keyNode != NULL && dataNode != NULL)
                    {
                        wxString type;
                        wxString key  = GetNodeAsText(keyNode->GetChildren(), type);
						expressions.push_back(key);
//                        wxTreeItemId child = AppendItem(item, key);
//                        AddCompoundExpression(dataNode->GetChildren());
                    }
                }
                node = node->GetNext();
            }
        }
        else if (root->GetName() == "values")
        {
            wxXmlNode* node = root->GetChildren();
            unsigned int i = 1;
            while (node != NULL)
            {
				expressions.push_back(wxString::Format("%d", i));
//                wxTreeItemId child = AppendItem(item, wxString::Format("%d", i));
//                AddCompoundExpression(node);

                node = node->GetNext();
                ++i;
            }
        }
    }
}

int EnumChildrenNum(int executeId,const char *text)
{
	g_expressions.clear();

	wxString result;
	Evaluate(ChangeExpressionName(text),result);
    if (!result.IsEmpty())
	{
        wxStringInputStream stream(result);
        wxXmlDocument document;

        if (document.Load(stream))
        {
            GetExpressions(document.GetRoot(),g_expressions);
		}
	}

	return g_expressions.size();
}

void EnumChildren(int executeId,const char *text,int subIndex,char *subText,int subTextLen)
{
	if(subIndex >= 0 && subIndex < (int)g_expressions.size())
	{
		CopyString(subText,subTextLen,g_expressions[subIndex].c_str());
	}
}

int GetProjectNumFiles()
{
	return m_project->GetNumFiles();
}

void GetProjectFile(int index,char *fullPath,int fullPathLen)
{
	if(index >= 0 && index < (int)m_project->GetNumFiles())
	{
        Project::File* file = m_project->GetFile(index);
		if(file != NULL)
		{
			CopyString(fullPath,fullPathLen,::GetFileFullPath(file));
		}
	}
}

CBreakPoints g_initBreakPoints;

void ClearInitBreakpoints()
{
	WriteDecodaLog("ClearInitBreakpoints");
	g_initBreakPoints.Clear();
}

void AddInitBreakpoint(const char *fullPath,int line)
{
	WriteDecodaLog(wxString::Format("AddInitBreakpoint line=%d fullPath=%s",line,fullPath));
	g_initBreakPoints.AddBreakPoint(fullPath,line);
}

void RemoveInitBreakpoint(const char *fullPath,int line)
{
	WriteDecodaLog(wxString::Format("RemoveInitBreakpoint line=%d fullPath=%s",line,fullPath));
	g_initBreakPoints.RemoveBreakPoint(fullPath,line);
}

void SetStackLevel(int stackLevel)
{
	SetContext(m_vm,stackLevel);
}
