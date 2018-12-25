/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
/*
using Microsoft.LuaTools.Parsing;
using Microsoft.LuaTools.Parsing.Ast;
*/
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

using System.Text;
using System.Windows.Forms;
using Babe.Lua;
using Babe.Lua.Package;

namespace Microsoft.LuaTools.Debugger {
    /// <summary>
    /// Handles all interactions with a Lua process which is being debugged.
    /// </summary>
    class LuaProcess : IDisposable {
        private static Random _portGenerator = new Random();

        private /*readonly*/ Process _process;
        private readonly Dictionary<long, LuaThread> _threads = new Dictionary<long, LuaThread>();
        private readonly Dictionary<int, LuaBreakpoint> _breakpoints = new Dictionary<int, LuaBreakpoint>();
        private readonly IdDispenser _ids = new IdDispenser();
        private readonly AutoResetEvent _lineEvent = new AutoResetEvent(false);         // set when result of setting current line returns
        private readonly Dictionary<int, CompletionInfo> _pendingExecutes = new Dictionary<int, CompletionInfo>();
        private readonly Dictionary<int, ChildrenInfo> _pendingChildEnums = new Dictionary<int, ChildrenInfo>();
//        private readonly LuaLanguageVersion _langVersion;
        private readonly Guid _processGuid = Guid.NewGuid();
        private readonly List<string[]> _dirMapping;
//        private readonly bool _delayUnregister;
        private readonly object _socketLock = new object();

        private int _pid;
        private bool _sentExited;//, _startedProcess;
//        private Socket _socket;
//        private Stream _stream;
        private int _breakpointCounter;
        private bool _setLineResult;                    // contains result of attempting to set the current line of a frame
        private bool _createdFirstThread;
        private bool _stoppedForException;
//        private int _defaultBreakMode;
//        private ICollection<KeyValuePair<string, int>> _breakOn;
        private bool _handleEntryPointHit = true;
        private bool _handleEntryPointBreakpoint = true;
/*
        protected LuaProcess(int pid) {//, LuaLanguageVersion languageVersion
            _pid = pid;
//            _langVersion = languageVersion;
        }
*/
        public LuaProcess()
        {
        }
        public void InitProcess(int pid)
        {
            _pid = pid;
            _process = Process.GetProcessById(pid);
            _process.EnableRaisingEvents = true;
            _process.Exited += new EventHandler(_process_Exited);
        }
        public LuaProcess(int pid) {
            _pid = pid;
            _process = Process.GetProcessById(pid);
            _process.EnableRaisingEvents = true;
            _process.Exited += new EventHandler(_process_Exited);
/*
            ListenForConnection();

            using (var result = DebugAttach.AttachAD7(pid, DebugConnectionListener.ListenerPort, _processGuid)) {
                if (result.Error != ConnErrorMessages.None) {
                    throw new AttachException(result.Error);
                }

                _langVersion = (LuaLanguageVersion)result.LanguageVersion;
                if (!result.AttachDone.WaitOne(20000)) {
                    throw new AttachException(ConnErrorMessages.TimeOut);
                }
            }
*/
        }
/*
        public LuaProcess(Stream stream, int pid, LuaLanguageVersion version) {
            _pid = pid;
            _process = Process.GetProcessById(pid);
            _process.EnableRaisingEvents = true;
            _process.Exited += new EventHandler(_process_Exited);
            
            _delayUnregister = true;
            
            ListenForConnection();

            stream.WriteInt32(DebugConnectionListener.ListenerPort);
            stream.WriteString(_processGuid.ToString());
        }
*/
        public LuaProcess(/*LuaLanguageVersion languageVersion,*/ string exe, string args, string dir, string env, string interpreterOptions, LuaDebugOptions options = LuaDebugOptions.None, List<string[]> dirMapping = null)
            : this(0/*, languageVersion*/) {

            ListenForConnection();

            if (dir.EndsWith("\\")) {
                dir = dir.Substring(0, dir.Length - 1);
            }
            _dirMapping = dirMapping;
            var processInfo = new ProcessStartInfo(exe);

            //add code
            processInfo.WorkingDirectory = dir;

            processInfo.CreateNoWindow = (options & LuaDebugOptions.CreateNoWindow) != 0;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = false;
            processInfo.RedirectStandardInput = (options & LuaDebugOptions.RedirectInput) != 0;
/*
            processInfo.Arguments = 
                (String.IsNullOrWhiteSpace(interpreterOptions) ? "" : (interpreterOptions + " ")) +
                "\"" + LuaToolsInstallPath.GetFile("visualstudio_py_launcher.py") + "\" " +
                "\"" + dir + "\" " +
                " " + DebugConnectionListener.ListenerPort + " " +
                " " + _processGuid + " " +
                (((options & LuaDebugOptions.WaitOnAbnormalExit) != 0) ? " --wait-on-exception " : "") +
                (((options & LuaDebugOptions.WaitOnNormalExit) != 0) ? " --wait-on-exit " : "") +
                (((options & LuaDebugOptions.RedirectOutput) != 0) ? " --redirect-output " : "") +
                (((options & LuaDebugOptions.BreakOnSystemExitZero) != 0) ? " --break-on-systemexit-zero " : "") +
                (((options & LuaDebugOptions.DebugStdLib) != 0) ? " --debug-stdlib " : "") +
                (((options & LuaDebugOptions.DjangoDebugging) != 0) ? " --django-debugging " : "") +
                args;
*/
            if (env != null) {
                string[] envValues = env.Split('\0');
                foreach (var curValue in envValues) {
                    string[] nameValue = curValue.Split(new[] { '=' }, 2);
                    if (nameValue.Length == 2 && !String.IsNullOrWhiteSpace(nameValue[0])) {
                        processInfo.EnvironmentVariables[nameValue[0]] = nameValue[1];
                    }
                }
            }

            Debug.WriteLine(String.Format("Launching: {0} {1}", processInfo.FileName, processInfo.Arguments));
            _process = new Process();
            _process.StartInfo = processInfo;
            _process.EnableRaisingEvents = true;
            _process.Exited += new EventHandler(_process_Exited);
        }
/*
        public static ConnErrorMessages TryAttach(int pid, out LuaProcess process) {
            try {
                process = new LuaProcess(pid);
                return ConnErrorMessages.None;
            } catch (AttachException ex) {
                process = null;
                return ex.Error;
            }
        }

        public static LuaProcess AttachRepl(Stream stream, int pid, LuaLanguageVersion version) {
            return new LuaProcess(stream, pid, version);
        }

        class AttachException : Exception {
            private readonly ConnErrorMessages _error;

            public AttachException(ConnErrorMessages error) {
                _error = error;
            }

            public ConnErrorMessages Error {
                get {
                    return _error;
                }
            }
        }
*/
        #region Public Process API

        public int Id {
            get {
                return _pid;
            }
        }

        public Guid ProcessGuid {
            get {
                return _processGuid;
            }
        }

        public void Start(bool startListening = true) {
            _process.Start();
//            _startedProcess = true;
            _pid = _process.Id;
            if (startListening) {
                StartListening();
            }
        }

        private void ListenForConnection() {
            DebugConnectionListener.RegisterProcess(_processGuid, this);
        }

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            DebugConnectionListener.UnregisterProcess(_processGuid);
/*
            if (disposing) {
                lock (_socketLock) {
                    if (_stream != null) {
                        _stream.Dispose();
                    }
                    if (_socket != null) {
                        try {
                            _socket.Disconnect(false);
                        } catch (ObjectDisposedException) {
                        } catch (SocketException) {
                        }
                        _socket.Dispose();
                        _socket = null;
                    }
                }
            }
*/
            GC.SuppressFinalize(this);
        }

        ~LuaProcess() {
            Dispose(false);
        }

        void _process_Exited(object sender, EventArgs e) {
            if (!_sentExited) {
                _sentExited = true;
                var exited = ProcessExited;
                if (exited != null) {
                    int exitCode;
                    try {
                        exitCode = (_process != null && _process.HasExited) ? _process.ExitCode : -1;
                    } catch (InvalidOperationException) {
                        // debug attach, we didn't start the process...
                        exitCode = -1;
                    }
                    exited(this, new ProcessExitedEventArgs(exitCode));
                }
            }
        }

        public void WaitForExit() {
            if (_process == null) {
                throw new InvalidOperationException();
            }
            _process.WaitForExit();
        }

        public bool WaitForExit(int milliseconds) {
            if (_process == null) {
                throw new InvalidOperationException();
            }
            return _process.WaitForExit(milliseconds);
        }

        public void Terminate() {
            if (_process != null && !_process.HasExited) {
                _process.Kill();
            }
/*
            if (_stream != null) {
                _stream.Dispose();
            }
            if (_socket != null) {
                _socket.Dispose();
                _socket = null;
            }
*/
        }

        public bool HasExited {
            get {
                return _process != null && _process.HasExited;
            }
        }

        /// <summary>
        /// Breaks into the process.
        /// </summary>
        public void Break() {
            DebugWriteCommand("BreakAll");
/*
            lock(_socketLock) {
                _stream.Write(BreakAllCommandBytes);
            }
*/
        }

        [Conditional("DEBUG")]
        private void DebugWriteCommand(string commandName) {
            Debug.WriteLine("LuaDebugger " + _processGuid + " Sending Command " + commandName);
        }

        public void Resume() {
            // Resume must be from entry point or past
            _handleEntryPointHit = false;

            _stoppedForException = false;
            DebugWriteCommand("ResumeAll");

            Boyaa.LuaDebug.WritePackageLog("DebugStart()");
            OutputSend(0, "DEBUG_START");
            Boyaa.LuaDebug.DebugStart();
            OutputDone(0, "DEBUG_START");
/*
            lock (_socketLock) {
                if (_stream != null) {
                    _stream.Write(ResumeAllCommandBytes);
                }
            }
*/
        }

        public void AutoResumeThread(long threadId) {
            if (_handleEntryPointHit) {
                // Handle entrypoint breakpoint/tracepoint
                var thread = _threads[threadId];
                if (_handleEntryPointBreakpoint) {
                    _handleEntryPointBreakpoint = false;
                    var frames = thread.Frames;
                    if (frames != null && frames.Count() > 0) {
                        var frame = frames[0];
                        if (frame != null) {
                            foreach (var breakpoint in _breakpoints.Values) {
                                // UNDONE Fuzzy filename matching
                                if (breakpoint.LineNo == frame.StartLine && breakpoint.Filename.Equals(frame.FileName, StringComparison.OrdinalIgnoreCase)) {
                                    // UNDONE: Conditional breakpoint/tracepoint
                                    var breakpointHit = BreakpointHit;
                                    if (breakpointHit != null) {
                                        breakpointHit(this, new BreakpointHitEventArgs(breakpoint, thread));
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }

                _handleEntryPointHit = false;
                var entryPointHit = EntryPointHit;
                if (entryPointHit != null) {
                    entryPointHit(this, new ThreadEventArgs(thread));
                    return;
                }
            }

            SendAutoResumeThread(threadId);
        }

        public bool StoppedForException {
            get {
                return _stoppedForException;
            }
        }

        public void Continue() {
            Resume();
        }

        public LuaBreakpoint AddBreakPoint(string filename, int lineNo, string condition = "", bool breakWhenChanged = false) {
            int id = _breakpointCounter++;
            var res = new LuaBreakpoint(this, filename, lineNo, condition, breakWhenChanged, id);
            _breakpoints[id] = res;
            return res;
        }

        public LuaBreakpoint AddDjangoBreakPoint(string filename, int lineNo) {
            int id = _breakpointCounter++;
            var res = new LuaBreakpoint(this, filename, lineNo, null, false, id, true);
            _breakpoints[id] = res;
            return res;
        }
/*
        public LuaLanguageVersion LanguageVersion {
            get {
                return _langVersion;
            }
        }
*/
        public void SetExceptionInfo(int defaultBreakOnMode, IEnumerable<KeyValuePair<string, int>> breakOn) {
/*
            lock (this) {
                if (_stream != null) {
                    SendExceptionInfo(defaultBreakOnMode, breakOn);
                } else {
                    _breakOn = breakOn.ToArray();
                    _defaultBreakMode = defaultBreakOnMode;
                }
            }
*/
        }

        private void SendExceptionInfo(int defaultBreakOnMode, IEnumerable<KeyValuePair<string, int>> breakOn) {
/*
            lock (_socketLock) {
                _stream.Write(SetExceptionInfoCommandBytes);
                _stream.WriteInt32(defaultBreakOnMode);
                _stream.WriteInt32(breakOn.Count());
                foreach (var item in breakOn) {
                    _stream.WriteInt32(item.Value);
                    _stream.WriteString(item.Key);
                }
            }
*/
        }

        #endregion

        #region Debuggee Communcation

        internal void Connected(Socket socket, Stream stream) {
            Debug.WriteLine("Process Connected: " + _processGuid);
/*
            lock (this) {
                _socket = socket;
                _stream = stream;
                if (_breakOn != null) {
                    SendExceptionInfo(_defaultBreakMode, _breakOn);
                }
            }

            if (!_delayUnregister) {
                Unregister();
            }*/
        }

        internal void Unregister() {
            DebugConnectionListener.UnregisterProcess(_processGuid);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Starts listening for debugger communication.  Can be called after Start
        /// to give time to attach to debugger events.
        /// </summary>
        public void StartListening() {
            var debuggerThread = new Thread(DebugEventThread);
            debuggerThread.Name = "Lua Debugger Thread " + _processGuid;
            debuggerThread.Start();
        }
        private Boyaa.CallbackEventInitialize _CallbackEventInitialize;
        private Boyaa.CallbackEventCreateVM _CallbackEventCreateVM;
        private Boyaa.CallbackEventDestroyVM _CallbackEventDestroyVM;
        private Boyaa.CallbackEventLoadScript _CallbackEventLoadScript;
        private Boyaa.CallbackEventBreak _CallbackEventBreak;
        private Boyaa.CallbackEventSetBreakpoint _CallbackEventSetBreakpoint;
        private Boyaa.CallbackEventException _CallbackEventException;
        private Boyaa.CallbackEventLoadError _CallbackEventLoadError;
        private Boyaa.CallbackEventMessage _CallbackEventMessage;
        private Boyaa.CallbackEventSessionEnd _CallbackEventSessionEnd;
        private Boyaa.CallbackEventNameVM _CallbackEventNameVM;
        private const int MaxStringBuilderLen = 1024;
        EnvDTE.Project _startupProject;
        public void LuaDebugSetCallback()
        {
            Boyaa.LuaDebug.WritePackageLog("LuaDebugSetCallback");

            _CallbackEventInitialize = new Boyaa.CallbackEventInitialize(CallbackEventInitialize);
            _CallbackEventCreateVM = new Boyaa.CallbackEventCreateVM(CallbackEventCreateVM);
            _CallbackEventDestroyVM = new Boyaa.CallbackEventDestroyVM(CallbackEventDestroyVM);
            _CallbackEventLoadScript = new Boyaa.CallbackEventLoadScript(CallbackEventLoadScript);
            _CallbackEventBreak = new Boyaa.CallbackEventBreak(CallbackEventBreak);
            _CallbackEventSetBreakpoint = new Boyaa.CallbackEventSetBreakpoint(CallbackEventSetBreakpoint);
            _CallbackEventException = new Boyaa.CallbackEventException(CallbackEventException);
            _CallbackEventLoadError = new Boyaa.CallbackEventLoadError(CallbackEventLoadError);
            _CallbackEventMessage = new Boyaa.CallbackEventMessage(CallbackEventMessage);
            _CallbackEventSessionEnd = new Boyaa.CallbackEventSessionEnd(CallbackEventSessionEnd);
            _CallbackEventNameVM = new Boyaa.CallbackEventNameVM(CallbackEventNameVM);

            Boyaa.LuaDebug.SetCallbackEventInitialize(_CallbackEventInitialize);
            Boyaa.LuaDebug.SetCallbackEventCreateVM(_CallbackEventCreateVM);
            Boyaa.LuaDebug.SetCallbackEventDestroyVM(_CallbackEventDestroyVM);
            Boyaa.LuaDebug.SetCallbackEventLoadScript(_CallbackEventLoadScript);
            Boyaa.LuaDebug.SetCallbackEventBreak(_CallbackEventBreak);
            Boyaa.LuaDebug.SetCallbackEventSetBreakpoint(_CallbackEventSetBreakpoint);
            Boyaa.LuaDebug.SetCallbackEventException(_CallbackEventException);
            Boyaa.LuaDebug.SetCallbackEventLoadError(_CallbackEventLoadError);
            Boyaa.LuaDebug.SetCallbackEventMessage(_CallbackEventMessage);
            Boyaa.LuaDebug.SetCallbackEventSessionEnd(_CallbackEventSessionEnd);
            Boyaa.LuaDebug.SetCallbackEventNameVM(_CallbackEventNameVM);

            _startupProject = Babe.Lua.LuaProject.GetStartupProject(BabePackage.Current.DTE);
        }
        private void CallbackEventInitialize(int iThreadId)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventInitialize:" + "iThread=" + iThreadId.ToString());
        }
        private void CallbackEventCreateVM(int iThreadId, int vm)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventCreateVM:" + "iThread=" + iThreadId.ToString() + " vm=" + vm.ToString());

            long threadId = iThreadId;// stream.ReadInt64();
            var thread = _threads[threadId] = new LuaThread(this, threadId, _createdFirstThread);
            _createdFirstThread = true;

            var created = ThreadCreated;
            if (created != null)
            {
                created(this, new ThreadEventArgs(thread));
            }


            var loaded = ProcessLoaded;
            if (loaded != null)
            {
                loaded(this, new ThreadEventArgs(thread));
            }

            Boyaa.LuaDebug.ClearInitBreakpoints();
            EnvDTE.Debugger debugger = BabePackage.Current.DTE.Debugger;
            foreach (EnvDTE.Breakpoint breakpoint in debugger.Breakpoints)
            {
                if (breakpoint.Enabled)
                {
                    OutputSend(iThreadId, "ADD_INIT_BREAKPOINT" + " " + breakpoint.File + "(" + breakpoint.FileLine.ToString() + ")");
                    Boyaa.LuaDebug.AddInitBreakpoint(breakpoint.File, breakpoint.FileLine - 1);
                    OutputDone(iThreadId, "ADD_INIT_BREAKPOINT");
                }
            }
        }
        private void CallbackEventDestroyVM(int iThreadId, int vm)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventDestroyVM:" + "iThread=" + iThreadId.ToString() + " vm=" + vm.ToString());
        }
        private void CallbackEventLoadScript(int iThreadId, string file, int scriptIndex, int iRelative)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventLoadScript:" + "iThread=" + iThreadId.ToString() + " file=" + file + " scriptIndex=" + scriptIndex.ToString());
            string relative = (iRelative == 0) ? "" : "  relative";
            string fileExist = File.Exists(file) ? "" : "  (file not exist)";
            string msg = "Load script(" + scriptIndex.ToString() + "): " + file + relative + fileExist;
            OutputEvent(iThreadId,msg);
/*
            //如果file是相对路径则转换为绝对路径（根据Working目录生成绝对路径）
            if(!Path.IsPathRooted(file))
            {
                file = Path.Combine(Microsoft.LuaTools.Project.DefaultLuaLauncher.TextWorking, file);
            }
*/
            if (!File.Exists(file))
                return;

            // module load
            int moduleId = scriptIndex;//stream.ReadInt32();
            string filename = file;//stream.ReadString();
            if (filename != null)
            {
                Debug.WriteLine(String.Format("Module Loaded ({0}): {1}", moduleId, filename));
                var module = new LuaModule(moduleId, filename);

                var loaded = ModuleLoaded;
                if (loaded != null)
                {
                    loaded(this, new ModuleLoadedEventArgs(module));
                }
            }

            //add file to startup project
            EnvDTE.Project startupProject = _startupProject;
            if (startupProject != null)
            {
                EnvDTE.ProjectItem projectItem = Babe.Lua.LuaProject.IsFileInProject(startupProject, file);
                if (projectItem == null)
                {
                    startupProject.ProjectItems.AddFromFile(file);
                }
            }
        }
        private int GetBreakpointId(string file, int line)
        {
            foreach (var breakpoint in _breakpoints.Values)
            {
                if (breakpoint.LineNo == line && breakpoint.Filename.Equals(file, StringComparison.OrdinalIgnoreCase))
                {
                    return breakpoint.Id;
                }
            }
            return -1;
        }
        private void CallbackEventBreak(int iThreadId, string file, int line)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventBreak:" + "iThread=" + iThreadId.ToString() + " file=" + file + " line=" + line.ToString());

            int breakId = GetBreakpointId(file, line + 1);// stream.ReadInt32();
            long threadId = iThreadId;// stream.ReadInt64();

            var frames = new List<LuaStackFrame>();
            LuaThread thread;
            _threads.TryGetValue(threadId, out thread);
            int numStackFrames = Boyaa.LuaDebug.GetNumStackFrames();
            for (int i = 0; i < numStackFrames; i++)
            {
                StringBuilder fullPath = new StringBuilder(MaxStringBuilderLen);
                StringBuilder fun = new StringBuilder(MaxStringBuilderLen);
                int stackLine = 0;
                Boyaa.LuaDebug.GetStackFrame(i, fullPath, MaxStringBuilderLen, fun, MaxStringBuilderLen, ref stackLine);

                int startLine = 1;
                int endLine = stackLine;
                int lineNo = stackLine;
                string frameName = fun.ToString();
                string filename = fullPath.ToString();
                int argCount = 0;
                var frameKind = FrameKind.Lua;
                LuaStackFrame frame = null;
                if (thread != null)
                {
                    frame = new LuaStackFrame(thread, frameName, filename, startLine, endLine, lineNo, argCount, i, frameKind);
                }

                int varCount = 0;// stream.ReadInt32();
                LuaEvaluationResult[] variables = new LuaEvaluationResult[varCount];
                /*
                for (int j = 0; j < variables.Length; j++)
                {
                    string name = stream.ReadString();
                    if (frame != null)
                    {
                        variables[j] = ReadLuaObject(stream, name, "", false, false, frame);
                    }
                }*/
                if (frame != null)
                {
                    frame.SetVariables(variables);
                    frames.Add(frame);
                }
            }

            Debug.WriteLine("Received frames for thread {0}", threadId);
            if (thread != null)
            {
                thread.Frames = frames;
                thread.Name = "LuaThread";// threadName;
            }

            if (breakId >= 0)
            {
                var brkEvent = BreakpointHit;
                LuaBreakpoint unboundBreakpoint;
                if (brkEvent != null)
                {
                    if (_breakpoints.TryGetValue(breakId, out unboundBreakpoint))
                    {
                        brkEvent(this, new BreakpointHitEventArgs(unboundBreakpoint, _threads[threadId]));
                    }
                    else
                    {
                        SendResumeThread(threadId);
                    }
                }
            }
            else
            {
                var stepComp = StepComplete;
                if (stepComp != null)
                {
                    stepComp(this, new ThreadEventArgs(_threads[threadId]));
                }
            }
        }
        private void CallbackEventSetBreakpoint(int iThreadId, string file, int line, int enabled)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventSetBreakpoint:" + "iThread=" + iThreadId.ToString() + " file=" + file + " line=" + line.ToString() + " enabled=" + enabled.ToString());

            //
            if (enabled != 1)
                return;

            // break point successfully set
            int id = GetBreakpointId(file, line + 1);// stream.ReadInt32();
            LuaBreakpoint unbound;
            if (_breakpoints.TryGetValue(id, out unbound))
            {
                var brkEvent = BreakpointBindSucceeded;
                if (brkEvent != null)
                {
                    brkEvent(this, new BreakpointEventArgs(unbound));
                }
            }
        }
        private void CallbackEventException(int iThreadId, string fullPath, int line, string msg)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventException:" + "iThread=" + iThreadId.ToString() + " msg=" + msg);

            OutputMessage(iThreadId, fullPath, line, msg);
            if (File.Exists(fullPath))
            {
//                CallbackEventBreak(iThreadId, fullPath, line + 1);
                GotoFileLineNumber(fullPath, line + 1);
            }
        }
        public void GotoFileLineNumber(string fileName, int lineNumber)
        {
            EnvDTE.DTE dte = BabePackage.Current.DTE;
            EnvDTE.ProjectItem document = dte.Solution.FindProjectItem(fileName);
            if (document != null)
            {
                document.Open().Activate();
                EnvDTE.TextSelection selection = dte.ActiveDocument.Selection as EnvDTE.TextSelection;
                if(selection != null)
                {
                    selection.GotoLine(lineNumber,true);
                }
            }
        }
        private void CallbackEventLoadError(int iThreadId, string fullPath, int line, string error)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventLoadError:" + "iThread=" + iThreadId.ToString() + " file=" + fullPath + " line=" + line.ToString() + " error=" + error);

            OutputMessage(iThreadId, fullPath, line + 1, error);
            if (File.Exists(fullPath))
            {
//                CallbackEventBreak(iThreadId, fullPath, line + 1);
                GotoFileLineNumber(fullPath, line + 1);
            }
        }
        private void OutputMessage(int iThreadId, string fullPath, int line, string msg)
        {
            if (string.IsNullOrEmpty(fullPath))
                OutputEvent(iThreadId, msg);
            else
                OutputEvent(iThreadId, fullPath + "(" + line.ToString() + "):" + msg);
        }
        private void OutputSend(int iThreadId,string cmd)
        {
            OutputEvent(iThreadId, "Debug cmd: " + cmd, false, false);
        }
        private void OutputDone(int iThreadId,string cmd)
        {
            OutputEvent(iThreadId, "  success", true, false);
        }
        private void OutputEvent(int iThreadId, string msg,bool enter = true,bool activate = true)
        {
            long tid = iThreadId;// stream.ReadInt64();
            msg = msg.Replace("\n", "");  //删除\n符号，避免msg中包含文件路径（从Decoda返回的含/的文件路径）显示到下一行，双击那条信息会多次打开文件
            string output = enter ? msg+"\r\n" : msg;// stream.ReadString();
/*
            LuaThread thread;
            if (_threads.TryGetValue(tid, out thread))
            {
                var outputEvent = DebuggerOutput;
                if (outputEvent != null)
                {
                    outputEvent(this, new OutputEventArgs(thread, output));
                }
            }
            else
            {*/
                const string DEBUG_OUTPUT_PANE_GUID = "{FC076020-078A-11D1-A7DF-00A0C9110051}";
                EnvDTE.Window window = (EnvDTE.Window)BabePackage.Current.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                if (activate)
                {
                    window.Visible = true;
                }
                EnvDTE.OutputWindow outputWindow = (EnvDTE.OutputWindow)window.Object;
                foreach (EnvDTE.OutputWindowPane outputWindowPane in outputWindow.OutputWindowPanes)
                {
                    if (outputWindowPane.Guid.ToUpper() == DEBUG_OUTPUT_PANE_GUID)
                    {
                        outputWindowPane.OutputString(output);
                        if (activate)
                        {
                            outputWindowPane.Activate();
                        }
                    }
                }
//            }
        }
        private void CallbackEventMessage(int iThreadId, int msgType, string fullPath, int line, string msg)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventMessage:" + "iThread=" + iThreadId.ToString() + " msgType=" + msgType.ToString() + " msg=" + msg);

//            const int MessageType_Normal = 0;
            const int MessageType_Warning = 1;
            const int MessageType_Error = 2;

            OutputMessage(iThreadId, fullPath, line, msg);
            if((msgType == MessageType_Warning) || (msgType == MessageType_Error))
            {
                if (File.Exists(fullPath))
                {
//                    CallbackEventBreak(iThreadId, fullPath, line + 1);
                    GotoFileLineNumber(fullPath, line + 1);
                }
            }
        }
        private void CallbackEventSessionEnd(int iThreadId)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventSessionEnd:" + "iThread=" + iThreadId.ToString());
/*
            EnvDTE.Project startupProject = _startupProject;
            if (startupProject != null)
            {
                int projectNumFiles = Boyaa.LuaDebug.GetProjectNumFiles();
                for (int i = 0; i < projectNumFiles; i++)
                {
                    StringBuilder fullPath = new StringBuilder(MaxStringBuilderLen);
                    Boyaa.LuaDebug.GetProjectFile(i, fullPath, MaxStringBuilderLen);

                    //remove file from startup project
                    EnvDTE.ProjectItem projectItem = IsFileInProject(startupProject, fullPath.ToString());
                    if (projectItem != null && projectItem.Document == null)
                    {
                        projectItem.Remove();
                    }
                }
            }*/
        }
        private void CallbackEventNameVM(int iThreadId, int vm, string vmName)
        {
            Boyaa.LuaDebug.WritePackageLog("CallbackEventNameVM:" + "iThread=" + iThreadId.ToString() + " vm=" + vm.ToString() + " vmName=" + vmName);
        }

        private void DebugEventThread()
        {
            Debug.WriteLine("DebugEvent Thread Started " + _processGuid);
/*
            while ((_process == null || !_process.HasExited) && _stream == null) {
                // wait for connection...
                System.Threading.Thread.Sleep(10);
            }

            try {
                while (true) {
                    Stream stream = _stream;
                    if (stream == null) {
                        break;
                    }

                    string cmd = stream.ReadAsciiString(4);
                    Debug.WriteLine(String.Format("Received Debugger command: {0} ({1})", cmd, _processGuid));

                    switch (cmd) {
                        case "EXCP": HandleException(stream); break;
                        case "BRKH": HandleBreakPointHit(stream); break;
                        case "NEWT": HandleThreadCreate(stream); break;
                        case "EXTT": HandleThreadExit(stream); break;
                        case "MODL": HandleModuleLoad(stream); break;
                        case "STPD": HandleStepDone(stream); break;
                        case "BRKS": HandleBreakPointSet(stream); break;
                        case "BRKF": HandleBreakPointFailed(stream); break;
                        case "LOAD": HandleProcessLoad(stream); break;
                        case "THRF": HandleThreadFrameList(stream); break;
                        case "EXCR": HandleExecutionResult(stream); break;
                        case "EXCE": HandleExecutionException(stream); break;
                        case "ASBR": HandleAsyncBreak(stream); break;
                        case "SETL": HandleSetLineResult(stream); break;
                        case "CHLD": HandleEnumChildren(stream); break;
                        case "OUTP": HandleDebuggerOutput(stream); break;
                        case "REQH": HandleRequestHandlers(stream); break;
                        case "DETC": _process_Exited(this, EventArgs.Empty); break; // detach, report process exit
                        case "LAST": HandleLast(stream); break;
                    }
                }
            } catch (IOException ioExc) {
                var sockExc = ioExc.InnerException as SocketException;
                if (sockExc != null) {
                    // Treat non-recoverable socket errors as an indication that the debuggee process has been terminated.
                    switch (sockExc.SocketErrorCode) {
                        case SocketError.ConnectionAborted:
                        case SocketError.ConnectionReset:
                            if (!_startedProcess) { // if we started the process wait until we receive the process exited event
                                _process_Exited(this, EventArgs.Empty);
                            }
                            break;
                    }
                }
            } catch (ObjectDisposedException ex) {
                // Socket or stream have been disposed
                Debug.Assert(
                    ex.ObjectName == typeof(NetworkStream).FullName ||
                    ex.ObjectName == typeof(Socket).FullName,
                    "Accidentally handled ObjectDisposedException(" + ex.ObjectName + ")"
                );
            }
*/
        }
/*
        private static string ToDottedNameString(Expression expr, LuaAst ast) {
            NameExpression name;
            MemberExpression member;
            ParenthesisExpression paren;
            if ((name = expr as NameExpression) != null) {
                return name.Name;
            } else if ((member = expr as MemberExpression) != null) {
                while (member.Target is MemberExpression) {
                    member = (MemberExpression)member.Target;
                }
                if (member.Target is NameExpression) {
                    return expr.ToCodeString(ast);
                }
            } else if ((paren = expr as ParenthesisExpression) != null) {
                return ToDottedNameString(paren.Expression, ast);
            }
            return null;
        }
*/
        internal IList<LuaThread> GetThreads() {
            List<LuaThread> threads = new List<LuaThread>();
            foreach (var thread in _threads.Values) {
                threads.Add(thread);
            }
            return threads;
        }
/*
        internal IList<Tuple<int, int, IList<string>>> GetHandledExceptionRanges(string filename) {
            LuaAst ast;
            TryHandlerWalker walker = new TryHandlerWalker();
            var statements = new List<Tuple<int, int, IList<string>>>();

            try {
                using (var source = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    ast = Parser.CreateParser(source, LanguageVersion).ParseFile();
                    ast.Walk(walker);
                }
            } catch (Exception ex) {
                Debug.WriteLine("Exception in GetHandledExceptionRanges:");
                Debug.WriteLine(string.Format("Filename: {0}", filename));
                Debug.WriteLine(ex);
                return statements;
            }

            foreach (var statement in walker.Statements) {
                int start = statement.GetStart(ast).Line;
                int end = statement.Body.GetEnd(ast).Line + 1;
                var expressions = new List<string>();

                if (statement.Handlers == null) {
                    expressions.Add("*");
                } else {
                    foreach (var handler in statement.Handlers) {
                        Expression expr = handler.Test;
                        TupleExpression tuple;
                        if (expr == null) {
                            expressions.Clear();
                            expressions.Add("*");
                            break;
                        } else if ((tuple = handler.Test as TupleExpression) != null) {
                            foreach (var e in tuple.Items) {
                                var text = ToDottedNameString(e, ast);
                                if (text != null) {
                                    expressions.Add(text);
                                }
                            }
                        
                        } else {
                            var text = ToDottedNameString(expr, ast);
                            if (text != null) {
                                expressions.Add(text);
                            }
                        }
                    }
                }

                if (expressions.Count > 0) {
                    statements.Add(new Tuple<int, int, IList<string>>(start, end, expressions));
                }
            }


            return statements;
        }
*/
        private void HandleRequestHandlers(Stream stream) {
/*
            string filename = stream.ReadString();

            Debug.WriteLine("Exception handlers requested for: " + filename);
            var statements = GetHandledExceptionRanges(filename);

            lock (_socketLock) {
                stream.Write(SetExceptionHandlerInfoCommandBytes);
                stream.WriteString(filename);

                stream.WriteInt32(statements.Count);

                foreach (var t in statements) {
                    stream.WriteInt32(t.Item1);
                    stream.WriteInt32(t.Item2);

                    foreach (var expr in t.Item3) {
                        stream.WriteString(expr);
                    }
                    stream.WriteString("-");
                }
            }
*/
        }

        private void HandleDebuggerOutput(Stream stream) {
            long tid = stream.ReadInt64();
            string output = stream.ReadString();

            LuaThread thread;
            if (_threads.TryGetValue(tid, out thread)) {
                var outputEvent = DebuggerOutput;
                if (outputEvent != null) {
                    outputEvent(this, new OutputEventArgs(thread, output));
                }
            }
        }

        private void HandleSetLineResult(Stream stream) {
            int res = stream.ReadInt32();
            long tid = stream.ReadInt64();
            int newLine = stream.ReadInt32();
            _setLineResult = res != 0;
            if (_setLineResult) {
                var frame = _threads[tid].Frames.FirstOrDefault();
                if (frame != null) {
                    frame.LineNo = newLine;
                } else {
                    Debug.Fail("SETL result received, but there is no frame to update");
                }
            }
            _lineEvent.Set();
        }

        private void HandleAsyncBreak(Stream stream) {
            long tid = stream.ReadInt64();
            var thread = _threads[tid];
            var asyncBreak = AsyncBreakComplete;
            Debug.WriteLine("Received async break command from thread {0}", tid);
            if (asyncBreak != null) {
                asyncBreak(this, new ThreadEventArgs(thread));
            }
        }

        private void HandleExecutionException(Stream stream) {
            int execId = stream.ReadInt32();
            CompletionInfo completion;

            lock (_pendingExecutes) {
                completion = _pendingExecutes[execId];
                _pendingExecutes.Remove(execId);
            }

            string exceptionText = stream.ReadString();
            completion.Completion(new LuaEvaluationResult(this, exceptionText, completion.Text, completion.Frame));
        }

        private void HandleExecutionResult(Stream stream) {
            int execId = stream.ReadInt32();
            CompletionInfo completion;
            lock (_pendingExecutes) {
                completion = _pendingExecutes[execId];

                _pendingExecutes.Remove(execId);
                _ids.Free(execId);
            }
            Debug.WriteLine("Received execution request {0}", execId);
            completion.Completion(ReadLuaObject(stream, completion.Text, "", false, false, completion.Frame));
        }

        private void HandleEnumChildren(Stream stream) {
            int execId = stream.ReadInt32();
            ChildrenInfo completion;

            lock (_pendingChildEnums) {
                completion = _pendingChildEnums[execId];
                _pendingChildEnums.Remove(execId);
            }

            int attributesCount = stream.ReadInt32();
            int indicesCount = stream.ReadInt32();
            bool indicesAreIndex = stream.ReadInt32() == 1;
            bool indicesAreEnumerate = stream.ReadInt32() == 1;
            LuaEvaluationResult[] res = new LuaEvaluationResult[attributesCount + indicesCount];
            for (int i = 0; i < attributesCount; i++) {
                string expr = stream.ReadString();
                res[i] = ReadLuaObject(stream, completion.Text, expr, false, false, completion.Frame);
            }
            for (int i = attributesCount; i < res.Length; i++) {
                string expr = stream.ReadString();
                res[i] = ReadLuaObject(stream, completion.Text, expr, indicesAreIndex, indicesAreEnumerate, completion.Frame);
            }
            completion.Completion(res);
        }

        private LuaEvaluationResult ReadLuaObject(Stream stream, string text, string childText, bool childIsIndex, bool childIsEnumerate, LuaStackFrame frame) {
            string objRepr = stream.ReadString();
            string hexRepr = stream.ReadString();
            string typeName = stream.ReadString();
            bool isExpandable = stream.ReadInt32() == 1;
/*
            if ((typeName == "unicode" && LanguageVersion.Is2x()) ||
                (typeName == "str" && LanguageVersion.Is3x())) {
                objRepr = objRepr.FixupEscapedUnicodeChars();
            }
*/
            if (typeName == "bool") {
                hexRepr = null;
            }

            return new LuaEvaluationResult(this, objRepr, hexRepr, typeName, text, childText, childIsIndex, childIsEnumerate, frame, isExpandable);
        }

        private void HandleThreadFrameList(Stream stream) {
            // list of thread frames
            var frames = new List<LuaStackFrame>();
            long tid = stream.ReadInt64();
            LuaThread thread;
            _threads.TryGetValue(tid, out thread);
            var threadName = stream.ReadString();

            int frameCount = stream.ReadInt32();
            for (int i = 0; i < frameCount; i++) {
                int startLine = stream.ReadInt32();
                int endLine = stream.ReadInt32();
                int lineNo = stream.ReadInt32();
                string frameName = stream.ReadString();
                string filename = stream.ReadString();
                int argCount = stream.ReadInt32();
                var frameKind = (FrameKind)stream.ReadInt32();
                LuaStackFrame frame = null; 
                if (thread != null) {
                    switch (frameKind) {
                        case FrameKind.Django:
                            string sourceFile = stream.ReadString();
                            var sourceLine = stream.ReadInt32();
                            frame = new DjangoStackFrame(thread, frameName, filename, startLine, endLine, lineNo, argCount, i, sourceFile, sourceLine);
                            break;
                        default:
                            frame = new LuaStackFrame(thread, frameName, filename, startLine, endLine, lineNo, argCount, i, frameKind);
                            break;
                    }
                    
                }

                int varCount = stream.ReadInt32();
                LuaEvaluationResult[] variables = new LuaEvaluationResult[varCount];
                for (int j = 0; j < variables.Length; j++) {
                    string name = stream.ReadString();
                    if (frame != null) {
                        variables[j] = ReadLuaObject(stream, name, "", false, false, frame);
                    }
                }
                if (frame != null) {
                    frame.SetVariables(variables);
                    frames.Add(frame);
                }
            }

            Debug.WriteLine("Received frames for thread {0}", tid);
            if (thread != null) {
                thread.Frames = frames;
                if (threadName != null) {
                    thread.Name = threadName;
                }
            }
        }

        private void HandleProcessLoad(Stream stream) {
            Debug.WriteLine("Process loaded " + _processGuid);

            // process is loaded, no user code has run
            long threadId = stream.ReadInt64();
            var thread = _threads[threadId];

            var loaded = ProcessLoaded;
            if (loaded != null) {
                loaded(this, new ThreadEventArgs(thread));
            }
        }

        private void HandleBreakPointFailed(Stream stream) {
            // break point failed to set
            int id = stream.ReadInt32();
            var brkEvent = BreakpointBindFailed;
            LuaBreakpoint breakpoint;
            if (brkEvent != null && _breakpoints.TryGetValue(id, out breakpoint)) {
                brkEvent(this, new BreakpointEventArgs(breakpoint));
            }
        }

        private void HandleBreakPointSet(Stream stream) {
            // break point successfully set
            int id = stream.ReadInt32();
            LuaBreakpoint unbound;
            if (_breakpoints.TryGetValue(id, out unbound)) {
                var brkEvent = BreakpointBindSucceeded;
                if (brkEvent != null) {
                    brkEvent(this, new BreakpointEventArgs(unbound));
                }
            }
        }

        private void HandleStepDone(Stream stream) {
            // stepping done
            long threadId = stream.ReadInt64();
            var stepComp = StepComplete;
            if (stepComp != null) {
                stepComp(this, new ThreadEventArgs(_threads[threadId]));
            }
        }

        private void HandleModuleLoad(Stream stream) {
            // module load
            int moduleId = stream.ReadInt32();
            string filename = stream.ReadString();
            if (filename != null) {
                Debug.WriteLine(String.Format("Module Loaded ({0}): {1}", moduleId, filename));
                var module = new LuaModule(moduleId, filename);

                var loaded = ModuleLoaded;
                if (loaded != null) {
                    loaded(this, new ModuleLoadedEventArgs(module));
                }
            }
        }

        private void HandleThreadExit(Stream stream) {
            // thread exit
            long threadId = stream.ReadInt64();
            LuaThread thread;
            if (_threads.TryGetValue(threadId, out thread)) {
                var exited = ThreadExited;
                if (exited != null) {
                    exited(this, new ThreadEventArgs(thread));
                }

                _threads.Remove(threadId);
                Debug.WriteLine("Thread exited, {0} active threads", _threads.Count);
            }

        }

        private void HandleThreadCreate(Stream stream) {
            // new thread
            long threadId = stream.ReadInt64();
            var thread = _threads[threadId] = new LuaThread(this, threadId, _createdFirstThread);
            _createdFirstThread = true;

            var created = ThreadCreated;
            if (created != null) {
                created(this, new ThreadEventArgs(thread));
            }
        }

        private void HandleBreakPointHit(Stream stream) {
            int breakId = stream.ReadInt32();
            long threadId = stream.ReadInt64();
            var brkEvent = BreakpointHit;
            LuaBreakpoint unboundBreakpoint;
            if (brkEvent != null) {
                if (_breakpoints.TryGetValue(breakId, out unboundBreakpoint)) {
                    brkEvent(this, new BreakpointHitEventArgs(unboundBreakpoint, _threads[threadId]));
                } else {
                    SendResumeThread(threadId);
                }
            }
        }

        private void HandleException(Stream stream) {
            string typeName = stream.ReadString();
            long tid = stream.ReadInt64();
            int breakType = stream.ReadInt32();
            string desc = stream.ReadString();
            if (typeName != null && desc != null) {
                Debug.WriteLine("Exception: " + desc);
                var excepRaised = ExceptionRaised;
                if (excepRaised != null) {
                    excepRaised(this, new ExceptionRaisedEventArgs(_threads[tid], new LuaException(typeName, desc), breakType == 1 /* BREAK_TYPE_UNHANLDED */));
                }
            }
            _stoppedForException = true;
        }

        private static string CommandtoString(byte[] cmd_buffer) {
            return new string(new char[] { (char)cmd_buffer[0], (char)cmd_buffer[1], (char)cmd_buffer[2], (char)cmd_buffer[3] });
        }

        private void HandleLast(Stream stream) {
            DebugWriteCommand("LAST ack");
            lock (_socketLock) {
                stream.Write(LastAckCommandBytes);
            }
        }

        internal void SendStepOut(long threadId) {
            DebugWriteCommand("StepOut");

            Boyaa.LuaDebug.WritePackageLog("StepOver()");
            Boyaa.LuaDebug.StepOver();

/*
            lock (_socketLock) {
                _stream.Write(StepOutCommandBytes);
                _stream.WriteInt64(threadId);
            }
*/
        }

        internal void SendStepOver(long threadId) {
            DebugWriteCommand("StepOver");

            Boyaa.LuaDebug.WritePackageLog("StepOver()");
            OutputSend((int)threadId, "STEP_OVER");
            Boyaa.LuaDebug.StepOver();
            OutputDone((int)threadId, "STEP_OVER");
/*
            lock (_socketLock) {
                _stream.Write(StepOverCommandBytes);
                _stream.WriteInt64(threadId);
            }
*/
        }

        internal void SendStepInto(long threadId) {
            DebugWriteCommand("StepInto");

            Boyaa.LuaDebug.WritePackageLog("StepInto()");
            OutputSend((int)threadId, "STEP_INTO");
            Boyaa.LuaDebug.StepInto();
            OutputDone((int)threadId, "STEP_INTO");
/*
            lock (_socketLock) {
                _stream.Write(StepIntoCommandBytes);
                _stream.WriteInt64(threadId);
            }
*/
        }

        public void SendResumeThread(long threadId) {
            _stoppedForException = false;
            DebugWriteCommand("ResumeThread");
/*
            lock (_socketLock) {
                // race w/ removing the breakpoint, let the thread continue
                _stream.Write(ResumeThreadCommandBytes);
                _stream.WriteInt64(threadId);
            }
*/
        }

        public void SendAutoResumeThread(long threadId) {
            _stoppedForException = false;
            DebugWriteCommand("AutoResumeThread");
/*
            lock (_socketLock) {
                _stream.Write(AutoResumeThreadCommandBytes);
                _stream.WriteInt64(threadId);
            }
*/
        }

    public void SendClearStepping(long threadId) {
            DebugWriteCommand("ClearStepping");
/*
            lock (_socketLock) {
                // race w/ removing the breakpoint, let the thread continue
                _stream.Write(ClearSteppingCommandBytes);
                _stream.WriteInt64(threadId);
            }
*/
        }

        public void Detach() {
            DebugWriteCommand("Detach");
/*
            try {
                lock (_socketLock) {
                    _stream.Write(DetachCommandBytes);
                }
            } catch (IOException) {
                // socket is closed after we send detach
            }
*/
        }

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        public static extern Int32 WaitForSingleObject(SafeWaitHandle handle, Int32 milliseconds);

        internal void BindBreakpoint(LuaBreakpoint breakpoint) {
            DebugWriteCommand(String.Format("Bind Breakpoint IsDjango: {0}", breakpoint.IsDjangoBreakpoint));

            Boyaa.LuaDebug.WritePackageLog("SetBreakpoint():" + "Filename=" + breakpoint.Filename + " LineNo=" + breakpoint.LineNo.ToString());
            OutputSend(0, "SET_BREAKPOINT" + " " + breakpoint.Filename + "(" + breakpoint.LineNo.ToString() + ")");
            Boyaa.LuaDebug.SetBreakpoint(breakpoint.Filename, breakpoint.LineNo-1);
            OutputDone(0, "SET_BREAKPOINT");

/*
            lock (_socketLock) {
                if (breakpoint.IsDjangoBreakpoint) {
                    _stream.Write(AddDjangoBreakPointCommandBytes);
                } else {
                    _stream.Write(SetBreakPointCommandBytes);
                }
                _stream.WriteInt32(breakpoint.Id);
                _stream.WriteInt32(breakpoint.LineNo);
                _stream.WriteString(MapFile(breakpoint.Filename));
                if (!breakpoint.IsDjangoBreakpoint) {
                    SendCondition(breakpoint);
                }
            }
*/
        }

        /// <summary>
        /// Maps a filename from the debugger machine to the debugge machine to vice versa.
        /// 
        /// The file mapping information is provided by our options when the debugger is started.  
        /// 
        /// This is used so that we can use the files local on the developers machine which have
        /// for setting breakpoints and viewing source code even though the files have been
        /// deployed to a remote machine.  For example the user may have:
        /// 
        /// C:\Users\Me\Documents\MyProject\Fob.py
        /// 
        /// which is deployed to
        /// 
        /// \\mycluster\deploydir\MyProject\Fob.py
        /// 
        /// We want the user to be working w/ the local project files during development but
        /// want to set break points in the cluster deployment share.
        /// </summary>
        internal string MapFile(string file, bool toDebuggee = true) {
            if (_dirMapping != null) {
                foreach (var mappingInfo in _dirMapping) {
                    string mapFrom = mappingInfo[toDebuggee ? 0 : 1];
                    string mapTo = mappingInfo[toDebuggee ? 1 : 0];

                    if (file.StartsWith(mapFrom, StringComparison.OrdinalIgnoreCase)) {
                        if (file.StartsWith(mapFrom, StringComparison.OrdinalIgnoreCase)) {
                            int len = mapFrom.Length;
                            if (!mappingInfo[0].EndsWith("\\")) {
                                len++;
                            }

                            string newFile = Path.Combine(mapTo, file.Substring(len));
                            Debug.WriteLine(String.Format("Filename mapped from {0} to {1}", file, newFile));
                            return newFile;
                        }
                    }
                }
            }
            return file;
        }

        private void SendCondition(LuaBreakpoint breakpoint) {
            DebugWriteCommand("Send BP Condition");
/*
            _stream.WriteString(breakpoint.Condition ?? "");
            _stream.WriteInt32(breakpoint.BreakWhenChanged ? 1 : 0);
*/
        }

        internal void SetBreakPointCondition(LuaBreakpoint breakpoint) {
            DebugWriteCommand("Set BP Condition");
/*
            lock (_socketLock) {
                _stream.Write(SetBreakPointConditionCommandBytes);
                _stream.WriteInt32(breakpoint.Id);
                SendCondition(breakpoint);
            }
*/
        }
        private LuaEvaluationResult ReadLuaObject(string text, string childText, int iExpandable, string type, string value, bool childIsIndex, bool childIsEnumerate, LuaStackFrame frame)
        {
            string objRepr = value;
            string hexRepr = "";
            string typeName = type;
            bool isExpandable = (iExpandable == 1);

            if (typeName == "bool")
            {
                hexRepr = null;
            }

            return new LuaEvaluationResult(this, objRepr, hexRepr, typeName, text, childText, childIsIndex, childIsEnumerate, frame, isExpandable);
        }

        private void ExecutionResult(bool bExecuteText,int executeId,string text,string type,string value,int iExpandable)
        {
            int execId = executeId;//stream.ReadInt32();
            CompletionInfo completion;
            lock (_pendingExecutes)
            {
                completion = _pendingExecutes[execId];

                _pendingExecutes.Remove(execId);
                _ids.Free(execId);
            }
            Debug.WriteLine("Received execution request {0}", execId);

            LuaEvaluationResult evaluationResult = ReadLuaObject(text, "", iExpandable, type, value, false, false, completion.Frame);
            evaluationResult.ExecuteText = bExecuteText;
            completion.Completion(evaluationResult);
        }
        void UpdateStackLevel()
        {
            EnvDTE.StackFrame currentStackFrame = BabePackage.Current.DTE.Debugger.CurrentStackFrame;
            EnvDTE.StackFrames stackFrames = BabePackage.Current.DTE.Debugger.CurrentThread.StackFrames;
            int stackLevel = 0;
            int iStackFrameIndex = 0;
            foreach (EnvDTE.StackFrame stackFrame in stackFrames)
            {
                if (stackFrame == currentStackFrame)
                {
                    stackLevel = iStackFrameIndex;
                    break;
                }
                iStackFrameIndex++;
            }
            Boyaa.LuaDebug.SetStackLevel(stackLevel);
        }
        internal void ExecuteText(string text, LuaStackFrame luaStackFrame, Action<LuaEvaluationResult> completion)
        {
            int executeId = _ids.Allocate();
            DebugWriteCommand("ExecuteText to thread " + luaStackFrame.Thread.Id + " " + executeId);
            lock (_pendingExecutes) {
                _pendingExecutes[executeId] = new CompletionInfo(completion, text, luaStackFrame);
            }

            UpdateStackLevel();

            StringBuilder type = new StringBuilder(MaxStringBuilderLen);
            StringBuilder value = new StringBuilder(MaxStringBuilderLen);
            int iExpandable = 0;
            bool bExecuteText = Boyaa.LuaDebug.ExecuteText(executeId, text, type, MaxStringBuilderLen, value, MaxStringBuilderLen, ref iExpandable);

            ExecutionResult(bExecuteText,executeId, text, type.ToString(), value.ToString(), iExpandable);

/*
            lock (_socketLock) {
                _stream.Write(ExecuteTextCommandBytes);
                _stream.WriteString(text);
                _stream.WriteInt64(luaStackFrame.Thread.Id);
                _stream.WriteInt32(luaStackFrame.FrameId);
                _stream.WriteInt32(executeId);
                _stream.WriteInt32((int)luaStackFrame.Kind);
            }
*/
        }
        private void EnumChildren(int executeId, string text)
        {
            int execId = executeId;//stream.ReadInt32();
            ChildrenInfo completion;

            lock (_pendingChildEnums)
            {
                completion = _pendingChildEnums[execId];
                _pendingChildEnums.Remove(execId);
            }

            UpdateStackLevel();

            int iChildrenNum = Boyaa.LuaDebug.EnumChildrenNum(executeId, text);
            LuaEvaluationResult[] res = new LuaEvaluationResult[iChildrenNum];
            for (int i = 0; i < iChildrenNum; i++)
            {
                StringBuilder subText = new StringBuilder(MaxStringBuilderLen);
                Boyaa.LuaDebug.EnumChildren(executeId, text, i, subText, MaxStringBuilderLen);

                StringBuilder type = new StringBuilder(MaxStringBuilderLen);
                StringBuilder value = new StringBuilder(MaxStringBuilderLen);
                int iExpandable = 0;
                Boyaa.LuaDebug.ExecuteText(executeId, completion.Text + '.' + subText.ToString(), type, MaxStringBuilderLen, value, MaxStringBuilderLen, ref iExpandable);

                res[i] = ReadLuaObject(completion.Text, subText.ToString(), iExpandable, type.ToString(), value.ToString(), false, false, completion.Frame);
            }
            completion.Completion(res);
        }

        internal void EnumChildren(string text, LuaStackFrame luaStackFrame, bool childIsEnumerate, Action<LuaEvaluationResult[]> completion)
        {
            DebugWriteCommand("Enum Children");
            int executeId = _ids.Allocate();
            lock (_pendingChildEnums) {
                _pendingChildEnums[executeId] = new ChildrenInfo(completion, text, luaStackFrame);
            }

            EnumChildren(executeId, text);
/*
            lock (_socketLock) {
                _stream.Write(GetChildrenCommandBytes);
                _stream.WriteString(text);
                _stream.WriteInt64(luaStackFrame.Thread.Id);
                _stream.WriteInt32(luaStackFrame.FrameId);
                _stream.WriteInt32(executeId);
                _stream.WriteInt32((int)luaStackFrame.Kind);
                _stream.WriteInt32(childIsEnumerate ? 1 : 0);
            }
*/
        }

        internal void RemoveBreakPoint(LuaBreakpoint unboundBreakpoint) {
            DebugWriteCommand("Remove Breakpoint");
            _breakpoints.Remove(unboundBreakpoint.Id);

            DisableBreakPoint(unboundBreakpoint);
        }

        internal void DisableBreakPoint(LuaBreakpoint unboundBreakpoint) {
            DebugWriteCommand("Disable Breakpoint");

            Boyaa.LuaDebug.WritePackageLog("DisableBreakpoint():" + "Filename=" + unboundBreakpoint.Filename + " LineNo=" + unboundBreakpoint.LineNo.ToString());
            OutputSend(0, "DISABLE_BREAKPOINT" + " " + unboundBreakpoint.Filename + "(" + unboundBreakpoint.LineNo.ToString() + ")");
            Boyaa.LuaDebug.DisableBreakpoint(unboundBreakpoint.Filename, unboundBreakpoint.LineNo - 1);
            OutputDone(0, "DISABLE_BREAKPOINT");
/*
            if (_stream != null && _socket.Connected) {
                lock (_socketLock) {
                    if (unboundBreakpoint.IsDjangoBreakpoint) {
                        _stream.Write(RemoveDjangoBreakPointCommandBytes);
                    } else {
                        _stream.Write(RemoveBreakPointCommandBytes);
                    }
                    _stream.WriteInt32(unboundBreakpoint.LineNo);
                    _stream.WriteInt32(unboundBreakpoint.Id);
                    if (unboundBreakpoint.IsDjangoBreakpoint) {
                        _stream.WriteString(unboundBreakpoint.Filename);
                    }
                }
            }
*/
        }

        internal void ConnectRepl(int portNum) {
            DebugWriteCommand("Connect Repl");
/*
            lock (_socketLock) {
                _stream.Write(ConnectReplCommandBytes);
                _stream.WriteInt32(portNum);
            }
*/
        }

        internal void DisconnectRepl() {
            DebugWriteCommand("Disconnect Repl");
/*
            lock (_socketLock) {
                if (_stream == null) {
                    return;
                }

                try {
                    _stream.Write(DisconnectReplCommandBytes);
                } catch (IOException) {
                } catch (ObjectDisposedException) {
                    // If the process has terminated, we expect an exception
                }
            }
*/
        }

        internal bool SetLineNumber(LuaStackFrame luaStackFrame, int lineNo) {
            if (_stoppedForException) {
                return false;
            }

            DebugWriteCommand("Set Line Number");
/*
            lock (_socketLock) {
                _setLineResult = false;
                _stream.Write(SetLineNumberCommand);
                _stream.WriteInt64(luaStackFrame.Thread.Id);
                _stream.WriteInt32(luaStackFrame.FrameId);
                _stream.WriteInt32(lineNo);
            }

            // wait up to 2 seconds for line event...
            for (int i = 0; i < 20 && _socket.Connected && WaitForSingleObject(_lineEvent.SafeWaitHandle, 100) != 0; i++) {
            }
*/
            return _setLineResult;
        }

        private static byte[] ExitCommandBytes = MakeCommand("exit");
        private static byte[] StepIntoCommandBytes = MakeCommand("stpi");
        private static byte[] StepOutCommandBytes = MakeCommand("stpo");
        private static byte[] StepOverCommandBytes = MakeCommand("stpv");
        private static byte[] BreakAllCommandBytes = MakeCommand("brka");
        private static byte[] SetBreakPointCommandBytes = MakeCommand("brkp");
        private static byte[] SetBreakPointConditionCommandBytes = MakeCommand("brkc");
        private static byte[] RemoveBreakPointCommandBytes = MakeCommand("brkr");
        private static byte[] ResumeAllCommandBytes = MakeCommand("resa");
        private static byte[] GetThreadFramesCommandBytes = MakeCommand("thrf");
        private static byte[] ExecuteTextCommandBytes = MakeCommand("exec");
        private static byte[] ResumeThreadCommandBytes = MakeCommand("rest");
        private static byte[] AutoResumeThreadCommandBytes = MakeCommand("ares");
        private static byte[] ClearSteppingCommandBytes = MakeCommand("clst");
        private static byte[] SetLineNumberCommand = MakeCommand("setl");
        private static byte[] GetChildrenCommandBytes = MakeCommand("chld");
        private static byte[] DetachCommandBytes = MakeCommand("detc");
        private static byte[] SetExceptionInfoCommandBytes = MakeCommand("sexi");
        private static byte[] SetExceptionHandlerInfoCommandBytes = MakeCommand("sehi");
        private static byte[] RemoveDjangoBreakPointCommandBytes = MakeCommand("bkdr");
        private static byte[] AddDjangoBreakPointCommandBytes = MakeCommand("bkda");
        private static byte[] ConnectReplCommandBytes = MakeCommand("crep");
        private static byte[] DisconnectReplCommandBytes = MakeCommand("drep");
        private static byte[] LastAckCommandBytes = MakeCommand("lack");

        private static byte[] MakeCommand(string command) {
            return new byte[] { (byte)command[0], (byte)command[1], (byte)command[2], (byte)command[3] };
        }

        internal void SendStringToStdInput(string text) {
            if (_process == null) {
                throw new InvalidOperationException();
            }
            _process.StandardInput.Write(text);
        }

        #endregion

        #region Debugging Events

        /// <summary>
        /// Fired when the process has started and is broken into the debugger, but before any user code is run.
        /// </summary>
        public event EventHandler<ThreadEventArgs> ProcessLoaded;
        public event EventHandler<ThreadEventArgs> ThreadCreated;
        public event EventHandler<ThreadEventArgs> ThreadExited;
        public event EventHandler<ThreadEventArgs> StepComplete;
        public event EventHandler<ThreadEventArgs> AsyncBreakComplete;
        public event EventHandler<ProcessExitedEventArgs> ProcessExited;
        public event EventHandler<ModuleLoadedEventArgs> ModuleLoaded;
        public event EventHandler<ExceptionRaisedEventArgs> ExceptionRaised;
        public event EventHandler<ThreadEventArgs> EntryPointHit;
        public event EventHandler<BreakpointHitEventArgs> BreakpointHit;
        public event EventHandler<BreakpointEventArgs> BreakpointBindSucceeded;
        public event EventHandler<BreakpointEventArgs> BreakpointBindFailed;
        public event EventHandler<OutputEventArgs> DebuggerOutput;

        #endregion

        class CompletionInfo {
            public readonly Action<LuaEvaluationResult> Completion;
            public readonly string Text;
            public readonly LuaStackFrame Frame;

            public CompletionInfo(Action<LuaEvaluationResult> completion, string text, LuaStackFrame frame) {
                Completion = completion;
                Text = text;
                Frame = frame;
            }
        }

        class ChildrenInfo {
            public readonly Action<LuaEvaluationResult[]> Completion;
            public readonly string Text;
            public readonly LuaStackFrame Frame;

            public ChildrenInfo(Action<LuaEvaluationResult[]> completion, string text, LuaStackFrame frame) {
                Completion = completion;
                Text = text;
                Frame = frame;
            }
        }
    }
}
