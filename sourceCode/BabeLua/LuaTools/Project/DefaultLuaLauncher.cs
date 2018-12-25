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
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows.Forms;
using Babe.Lua;
using Babe.Lua.Package;
using Babe.Lua.ToolWindows;
using Microsoft.LuaTools.Debugger.DebugEngine;
//using Microsoft.LuaTools.Interpreter;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Project;

namespace Microsoft.LuaTools.Project {
    /// <summary>
    /// Implements functionality of starting a project or a file with or without debugging.
    /// </summary>
    sealed class DefaultLuaLauncher : IProjectLauncher {
        private readonly ILuaProject/*!*/ _project;

        public DefaultLuaLauncher(ILuaProject/*!*/ project) {
            Utilities.ArgumentNotNull("project", project);

            _project = project;
        }

        #region ILuaLauncher Members

        public int LaunchProject(bool debug) {
            string startupFile = ResolveStartupFile();
            return LaunchFile(startupFile, debug);
        }

        internal static string GetFullUrl(string url, string port) {
            if (!String.IsNullOrWhiteSpace(url)) {
                Uri relativeUri;
                if (Uri.TryCreate(url, UriKind.Relative, out relativeUri)) {
                    url = new Uri(new Uri("http://localhost"), url).ToString();
                }

                Uri uri;

                if (Uri.TryCreate(url, UriKind.Absolute, out uri)) {
                    if (String.IsNullOrEmpty(uri.GetComponents(UriComponents.Port, UriFormat.Unescaped))) {
                        int portNum;
                        if (Int32.TryParse(port, out portNum)) {
                            var builder = new UriBuilder(uri);
                            builder.Port = portNum;
                            url = builder.ToString();
                        }
                    }

                }
                return url;
            }
            return null;
        }

        private string GetFullUrl() {
            var url = GetFullUrl(
                _project.GetProperty(LuaConstants.WebBrowserUrlSetting),
                _project.GetProperty(LuaConstants.WebBrowserPortSetting)
            );
            Uri uri;
            if (!String.IsNullOrWhiteSpace(url) &&
                !Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri)) {
                MessageBox.Show(String.Format("Project is configured to launch to invalid URL: \"{0}\"\r\n\r\nYour web browser will not be opened.", url));
            }
            return url;
        }

        static private string _textExe;
        static private string _textCommand;
        static private string _textWorking;
        public static string TextExe
        {
            get { return _textExe; }
        }
        public static string TextCommand
        {
            get { return _textCommand; }
        }
        public static string TextWorking
        {
            get { return _textWorking; }
        }
        private bool InitDebugExe()
        {
            if (BabePackage.Current.CurrentSetting == null || string.IsNullOrWhiteSpace(BabePackage.Current.CurrentSetting.LuaExecutable))
            {
                System.Windows.MessageBox.Show(Babe.Lua.Properties.Resources.GuideLuaExecutablePath);
                BabePackage.WindowManager.ShowSettingWindow();
                if (SettingWndPane.Current != null)
                {
                    SettingWndPane.Current.ShowToExecutable();
                }
                return false;
            }
/*
            if (Babe.Lua.BabePackage.Current.CurrentSetting == null)
            {
                MessageBox.Show("please choose setting!\r\nselect menu: [Lua] --> [Views] --> [Settings] --> LuaFolder");
                return false;
            }
            if (!File.Exists(_textExe))
            {
                MessageBox.Show("Lua .exe path is not exist! please select Lua .exe path\r\nselect menu: [Lua] --> [Views] --> [Settings] --> LuaFolder");
                return false;
            }*/
            _textExe = BabePackage.Current.CurrentSetting.LuaExecutable;
            _textCommand = BabePackage.Current.CurrentSetting.CommandLine;
            _textWorking = BabePackage.Current.CurrentSetting.WorkingPath;
            if (string.IsNullOrWhiteSpace(_textWorking))
            {
                _textWorking = System.IO.Path.GetDirectoryName(_textExe);
            }
            return true;
/*
            Boyaa.FormSetting setting = new Boyaa.FormSetting();
            setting.textExe = _textExe;
            setting.textCommand = _textCommand;
            setting.textWorking = _textWorking;
            DialogResult dialogResult = setting.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                _textExe = setting.textExe;
                _textCommand = setting.textCommand;
                _textWorking = setting.textWorking;
                return true;
            }
            return false;
*/
        }
        public int LaunchFile(string/*!*/ file, bool debug) {
            if (!InitDebugExe())
                return VSConstants.S_FALSE;

            if (debug) {
//                LuaToolsPackage.Instance.Logger.LogEvent(Logging.LuaLogEvent.Launch, 1);
                StartWithDebugger(file);
            } else {
//                LuaToolsPackage.Instance.Logger.LogEvent(Logging.LuaLogEvent.Launch, 0);
                var process = StartWithoutDebugger(file);
/*
                var url = GetFullUrl();
                Uri uri;
                if (process != null &&
                    !String.IsNullOrWhiteSpace(url) &&
                    Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri)) {
                    OnPortOpenedHandler.CreateHandler(
                        uri.Port,
                        shortCircuitPredicate: () => process.HasExited,
                        action: () => {
                            var web = LuaToolsPackage.GetGlobalService(typeof(SVsWebBrowsingService)) as IVsWebBrowsingService;
                            if (web == null) {
                                LuaToolsPackage.OpenWebBrowser(url);
                                return;
                            }

                            ErrorHandler.ThrowOnFailure(
                                web.CreateExternalWebBrowser(
                                    (uint)__VSCREATEWEBBROWSER.VSCWB_ForceNew,
                                    VSPREVIEWRESOLUTION.PR_Default,
                                    url
                                )
                            );
                        }
                    );
                }*/
            }

            return VSConstants.S_OK;
        }

        #endregion

        private bool NoInterpretersAvailable() {
/*
            var interpreter = _project.GetInterpreterFactory();
            var interpreterService = LuaToolsPackage.ComponentModel.GetService<IInterpreterOptionsService>();
            return interpreterService == null || interpreterService.NoInterpretersValue == interpreter;
*/
            return false;
        }

        private string GetInterpreterExecutableInternal(out bool isWindows) {
            if (!Boolean.TryParse(_project.GetProperty(CommonConstants.IsWindowsApplication) ?? Boolean.FalseString, out isWindows)) {
                isWindows = false;
            }

            string result;
            result = (_project.GetProperty(CommonConstants.InterpreterPath) ?? string.Empty).Trim();
            if (!String.IsNullOrEmpty(result)) {
                result = CommonUtils.GetAbsoluteFilePath(_project.ProjectDirectory, result);

                if (!File.Exists(result)) {
                    throw new FileNotFoundException(String.Format("Interpreter specified in the project does not exist: '{0}'", result), result);
                }

                return result;
            }
/*
            if (NoInterpretersAvailable()) {
                LuaToolsPackage.OpenVsWebBrowser(LuaToolsInstallPath.GetFile("NoInterpreters.html"));
                return null;
            }

            var interpreter = _project.GetInterpreterFactory();
            return !isWindows ?
                interpreter.Configuration.InterpreterPath :
                interpreter.Configuration.WindowsInterpreterPath;
*/
            return null;
        }

        /// <summary>
        /// Creates language specific command line for starting the project without debigging.
        /// </summary>
        public string CreateCommandLineNoDebug(string startupFile) {
            string cmdLineArgs = _project.GetProperty(CommonConstants.CommandLineArguments) ?? string.Empty;
            string interpArgs = _project.GetProperty(CommonConstants.InterpreterArguments) ?? string.Empty;

            return String.Format("{0} \"{1}\" {2}", interpArgs, startupFile, cmdLineArgs);
        }

        /// <summary>
        /// Creates language specific command line for starting the project with debigging.
        /// </summary>
        public string CreateCommandLineDebug(string startupFile) {
            string cmdLineArgs = _project.GetProperty(CommonConstants.CommandLineArguments) ?? string.Empty;

            return String.Format("\"{0}\" {1}", startupFile, cmdLineArgs);
        }

        /// <summary>
        /// Default implementation of the "Start without Debugging" command.
        /// </summary>
        private Process StartWithoutDebugger(string startupFile) {
            var psi = CreateProcessStartInfoNoDebug(startupFile);
            if (psi == null) {
                if (!NoInterpretersAvailable()) {
                    MessageBox.Show(
                        "The project cannot be started because its active Lua environment does not have the interpreter executable specified.",
                        "Lua Tools for Visual Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return null;
            }
            return Process.Start(psi);
        }

        /// <summary>
        /// Default implementation of the "Start Debugging" command.
        /// </summary>
        private void StartWithDebugger(string startupFile) {
            VsDebugTargetInfo dbgInfo = new VsDebugTargetInfo();
            try {
                dbgInfo.cbSize = (uint)Marshal.SizeOf(dbgInfo);
                SetupDebugInfo(ref dbgInfo, startupFile);

                if (string.IsNullOrEmpty(dbgInfo.bstrExe)) {
                    if (!NoInterpretersAvailable()) {
                        MessageBox.Show(
                            "The project cannot be debugged because its active Lua environment does not have the interpreter executable specified.",
                            "Lua Tools for Visual Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                LaunchDebugger(BabePackage.Current, dbgInfo);
            } finally {
                if (dbgInfo.pClsidList != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(dbgInfo.pClsidList);
                }
            }
        }

        private static void LaunchDebugger(IServiceProvider provider, VsDebugTargetInfo dbgInfo) {
            if (!Directory.Exists(UnquotePath(dbgInfo.bstrCurDir))) {
                MessageBox.Show(String.Format("Working directory \"{0}\" does not exist.", dbgInfo.bstrCurDir), "Lua Tools for Visual Studio");
            } else if (!File.Exists(UnquotePath(dbgInfo.bstrExe))) {
                MessageBox.Show(String.Format("Interpreter \"{0}\" does not exist.", dbgInfo.bstrExe), "Lua Tools for Visual Studio");
            } else {
                VsShellUtilities.LaunchDebugger(provider, dbgInfo);
            }
        }

        private static string UnquotePath(string p) {
            if (p.StartsWith("\"") && p.EndsWith("\"")) {
                return p.Substring(1, p.Length - 2);
            }
            return p;
        }

        /// <summary>
        /// Sets up debugger information.
        /// </summary>
        private unsafe void SetupDebugInfo(ref VsDebugTargetInfo dbgInfo, string startupFile) {
            bool enableNativeCodeDebugging = false;
#if DEV11_OR_LATER
            bool.TryParse(_project.GetProperty(LuaConstants.EnableNativeCodeDebugging), out enableNativeCodeDebugging);
#endif

            dbgInfo.dlo = DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;
/*
            bool isWindows;
            var interpreterPath = GetInterpreterExecutableInternal(out isWindows);
            if (string.IsNullOrEmpty(interpreterPath)) {
                return;
            }
*/

            dbgInfo.bstrExe = _textExe;//interpreterPath;
            dbgInfo.bstrCurDir = _textWorking;//_project.GetWorkingDirectory();
            dbgInfo.bstrArg = _textCommand;//CreateCommandLineDebug(startupFile);
            dbgInfo.bstrRemoteMachine = null;
            dbgInfo.fSendStdoutToOutputWindow = 0;
/*
            if (!enableNativeCodeDebugging) {
                string interpArgs = _project.GetProperty(CommonConstants.InterpreterArguments);
                dbgInfo.bstrOptions = AD7Engine.VersionSetting + "=" + _project.GetInterpreterFactory().GetLanguageVersion().ToString();
                if (!isWindows) {
                    if (LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnAbnormalExit) {
                        dbgInfo.bstrOptions += ";" + AD7Engine.WaitOnAbnormalExitSetting + "=True";
                    }
                    if (LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnNormalExit) {
                        dbgInfo.bstrOptions += ";" + AD7Engine.WaitOnNormalExitSetting + "=True";
                    }
                }
                if (LuaToolsPackage.Instance.DebuggingOptionsPage.TeeStandardOutput) {
                    dbgInfo.bstrOptions += ";" + AD7Engine.RedirectOutputSetting + "=True";
                }
                if (LuaToolsPackage.Instance.DebuggingOptionsPage.BreakOnSystemExitZero) {
                    dbgInfo.bstrOptions += ";" + AD7Engine.BreakSystemExitZero + "=True";
                }
                if (LuaToolsPackage.Instance.DebuggingOptionsPage.DebugStdLib) {
                    dbgInfo.bstrOptions += ";" + AD7Engine.DebugStdLib + "=True";
                }
                if (!String.IsNullOrWhiteSpace(interpArgs)) {
                    dbgInfo.bstrOptions += ";" + AD7Engine.InterpreterOptions + "=" + interpArgs.Replace(";", ";;");
                }

                var url = GetFullUrl();
                if (!String.IsNullOrWhiteSpace(url)) {
                    dbgInfo.bstrOptions += ";" + AD7Engine.WebBrowserUrl + "=" + HttpUtility.UrlEncode(url);
                }

                var djangoDebugging = _project.GetProperty("DjangoDebugging");
                bool enableDjango;
                if (!String.IsNullOrWhiteSpace(djangoDebugging) && Boolean.TryParse(djangoDebugging, out enableDjango)) {
                    dbgInfo.bstrOptions += ";" + AD7Engine.EnableDjangoDebugging + "=True";
                }
            }
*/

            StringDictionary env = new StringDictionary();
            SetupEnvironment(env);
            if (env.Count > 0) {
                // add any inherited env vars
                var variables = Environment.GetEnvironmentVariables();
                foreach (var key in variables.Keys) {
                    string strKey = (string)key;
                    if (!env.ContainsKey(strKey)) {
                        env.Add(strKey, (string)variables[key]);
                    }
                }

                //Environemnt variables should be passed as a
                //null-terminated block of null-terminated strings. 
                //Each string is in the following form:name=value\0
                StringBuilder buf = new StringBuilder();
                foreach (DictionaryEntry entry in env) {
                    buf.AppendFormat("{0}={1}\0", entry.Key, entry.Value);
                }
                buf.Append("\0");
                dbgInfo.bstrEnv = buf.ToString();
            }

            if (enableNativeCodeDebugging) {
#if DEV11_OR_LATER
                dbgInfo.dwClsidCount = 2;
                dbgInfo.pClsidList = Marshal.AllocCoTaskMem(sizeof(Guid) * 2);
                var engineGuids = (Guid*)dbgInfo.pClsidList;
                engineGuids[0] = dbgInfo.clsidCustom = DkmEngineId.NativeEng;
                engineGuids[1] = AD7Engine.DebugEngineGuid;
#endif
            } else {
                // Set the Lua debugger
                dbgInfo.clsidCustom = new Guid(AD7Engine.DebugEngineId);
                dbgInfo.grfLaunch = (uint)__VSDBGLAUNCHFLAGS.DBGLAUNCH_StopDebuggingOnEnd;
            }
        }

        /// <summary>
        /// Sets up environment variables before starting the project.
        /// </summary>
        private void SetupEnvironment(StringDictionary environment) {
/*
            string pathEnvVar = _project.GetInterpreterFactory().Configuration.PathEnvironmentVariable;
            if (!String.IsNullOrWhiteSpace(pathEnvVar)) {
                environment[pathEnvVar] = string.Join(";", _project.GetSearchPaths());
            }
*/
        }

        /// <summary>
        /// Creates process info used to start the project with no debugging.
        /// </summary>
        private ProcessStartInfo CreateProcessStartInfoNoDebug(string startupFile) {
            string command = CreateCommandLineNoDebug(startupFile);
/*
            bool isWindows;
            string interpreter = GetInterpreterExecutableInternal(out isWindows);
            if (string.IsNullOrEmpty(interpreter)) {
                return null;
            }
*/
            ProcessStartInfo startInfo;
/*
            if (!isWindows && (LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnAbnormalExit || LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnNormalExit)) {
                command = "/c \"\"" + interpreter + "\" " + command;

                if (LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnNormalExit &&
                    LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnAbnormalExit) {
                    command += " & pause";
                } else if (LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnNormalExit) {
                    command += " & if not errorlevel 1 pause";
                } else if (LuaToolsPackage.Instance.DebuggingOptionsPage.WaitOnAbnormalExit) {
                    command += " & if errorlevel 1 pause";
                }

                command += "\"";
                startInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), command);
            } else {
                startInfo = new ProcessStartInfo(interpreter, command);
            }
*/
            startInfo = new ProcessStartInfo(_textExe,_textCommand);//add
            startInfo.WorkingDirectory = _textWorking;//_project.GetWorkingDirectory();

            //In order to update environment variables we have to set UseShellExecute to false
            startInfo.UseShellExecute = false;
            SetupEnvironment(startInfo.EnvironmentVariables);
            return startInfo;
        }

        private string ResolveStartupFile() {
            string startupFile = _project.GetStartupFile();
            if (string.IsNullOrEmpty(startupFile)) {
                //TODO: need to start active file then
//                throw new ApplicationException("No startup file is defined for the startup project.");
            }
            return startupFile;
        }
    }
}
