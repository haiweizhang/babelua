using Babe.Lua.Editor;
using Babe.Lua.ToolWindows;
using EnvDTE;
using Microsoft.LuaTools;
using Microsoft.LuaTools.Debugger.DebugEngine;
using Microsoft.LuaTools.Editor;
using Microsoft.LuaTools.Navigation;
using Microsoft.LuaTools.Project;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Navigation;
using Microsoft.VisualStudioTools.Project;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using NativeMethods = Microsoft.VisualStudioTools.Project.NativeMethods;

namespace Babe.Lua.Package
{
	#region head
	/// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.5.7.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.NoSolution)]
    [ProvideAutoLoad(Microsoft.VisualStudio.Shell.Interop.UIContextGuids.SolutionExists)]
    [Description("Lua Tools Package")]
    [ProvideAutomationObject("VsLua")]
    
    ////提供补全代码段功能
    //[ProvideLanguageCodeExpansion(
    //     typeof(TestLanguageService),
    //     "Test",          // Name of language used as registry key
    //     0,                               // Resource ID of localized name of language service
    //     LuaConstants.LanguageName,        // Name of Language attribute in snippet template
    //     @"%InstallRoot%\Lua\Snippets\%LCID%\SnippetsIndex.xml",  // Path to snippets index
    //     SearchPaths = @"%InstallRoot%\Lua\Snippets\%LCID%\")]    // Path to snippets

	[Guid(GuidList.PkgString)]

    [ProvideLanguageService(typeof(LuaLanguageInfo), LuaConstants.LanguageName, 106, RequestStockColors = true, ShowSmartIndent = false, ShowCompletion = true, DefaultToInsertSpaces = true, HideAdvancedMembersByDefault = true, EnableAdvancedMembersOption = true, ShowDropDownOptions = true)]
    [ProvideLanguageExtension(typeof(LuaLanguageInfo), LuaConstants.FileExtension)]
    [ProvideDebugEngine(AD7Engine.DebugEngineName, typeof(AD7ProgramProvider), typeof(AD7Engine), AD7Engine.DebugEngineId)]
    [ProvideDebugLanguage("Lua", "{D65C7900-783A-4C13-8312-41B178AF29FF}"/*"{DA3C7D59-F9E4-4697-BEE7-3A0703AF6BFF}"*/, LuaExpressionEvaluatorGuid, AD7Engine.DebugEngineId)]

    [ProvideToolWindow(typeof(SearchWndPane1),
        Style = VsDockStyle.Linked,
        Orientation = ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.Outputwindow
        )]
    [ProvideToolWindow(typeof(SearchWndPane2),
        Style = VsDockStyle.Linked,
        Orientation = ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.Outputwindow
        )]
    [ProvideToolWindow(typeof(OutlineWndPane),
        Style = VsDockStyle.Linked,
        Orientation = ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.SolutionExplorer
        )]
    [ProvideToolWindow(typeof(FolderWndPane),
        Style=VsDockStyle.Linked,
        Orientation=ToolWindowOrientation.Left,
        Window = ToolWindowGuids80.StartPage
        )]
    [ProvideToolWindow(typeof(SettingWndPane),
        Style = VsDockStyle.Tabbed,
        Orientation = ToolWindowOrientation.none,
        Window = ToolWindowGuids80.StartPage
        )]

    [ProvideAutoLoad(UIContextGuids.NoSolution)]
	#endregion
	public sealed class BabePackage : CommonPackage, IVsComponentSelectorProvider
    {
        private LanguagePreferences _langPrefs;

        private IContentType _contentType;

        internal const string LuaExpressionEvaluatorGuid = "CF8C7A55-31E3-4B6D-AF29-37CE17330AB0";//"{D67D5DB8-3D44-4105-B4B8-47AB1BA66180}";
        private SolutionEventsListener _solutionEventListener;
        private string _surveyNewsUrl;
        private object _surveyNewsUrlLock = new object();


        OleComponent OleCom;
        public static BabePackage Current { get; private set; }
        internal static DTEHelper DTEHelper { get; private set; }
        internal static Setting Setting = Setting.Instance;
        internal static WindowManager WindowManager { get; private set; }

        public BabePackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            Current = this;
        }

		protected override void Initialize()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
			
			base.Initialize();

            //捕获全局异常
			UnhandledExceptionCatcher uec = new UnhandledExceptionCatcher();

			//初始化DTEHelper
            DTEHelper = new DTEHelper(GetService(typeof(DTE)) as DTE);

            //初始化WindowManager
            WindowManager = new WindowManager();
			
			RegisterOleComponent();

			AddCommandBars();

			HideToolBars();

			// register our language service so that we can support features like the navigation bar
			var langService = new LuaLanguageInfo(this);
			((IServiceContainer)this).AddService(langService.GetType(), langService, true);

			_solutionEventListener = new SolutionEventsListener(this);
			_solutionEventListener.StartListeningForChanges();

            //_langPrefs = new LanguagePreferences(langPrefs[0]);
			//Guid guid = typeof(IVsTextManagerEvents2).GUID;
			//IConnectionPoint connectionPoint;
			//((IConnectionPointContainer)textMgr).FindConnectionPoint(ref guid, out connectionPoint);
			//uint cookie;
			//connectionPoint.Advise(_langPrefs, out cookie);
			
            Boyaa.LuaDebug.SetWriteLog(Convert.ToInt32(Setting.AllowDebugLog));
		}

        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "LuaProjectPackage",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        internal static bool IsIpyToolsInstalled() {
            // the component guid which IpyTools is installed under from IronLua 2.7
            const string ipyToolsComponentGuid = "{2DF41B37-FAEF-4FD8-A2F5-46B57FF9E951}";

            // Check if the IpyTools component is known...
            StringBuilder productBuffer = new StringBuilder(39);
            if (NativeMethods.MsiGetProductCode(ipyToolsComponentGuid, productBuffer) == 0) {
                // If it is then make sure that it's installed locally...
                StringBuilder buffer = new StringBuilder(1024);
                uint charsReceived = (uint)buffer.Capacity;
                var res = NativeMethods.MsiGetComponentPath(productBuffer.ToString(), ipyToolsComponentGuid, buffer, ref charsReceived);
                switch (res) {
                    case NativeMethods.MsiInstallState.Source:
                    case NativeMethods.MsiInstallState.Local:
                        return true;
                }
            }
            return false;
        }

        internal static void NavigateTo(string filename, Guid docViewGuidType, int line, int col) {
            IVsTextView viewAdapter;
            IVsWindowFrame pWindowFrame;
            OpenDocument(filename, out viewAdapter, out pWindowFrame);

            ErrorHandler.ThrowOnFailure(pWindowFrame.Show());

            // Set the cursor at the beginning of the declaration.            
            ErrorHandler.ThrowOnFailure(viewAdapter.SetCaretPos(line, col));
            
            // Make sure that the text is visible.
            viewAdapter.CenterLines(line, 1);
        }

        internal static void NavigateTo(string filename, Guid docViewGuidType, int pos) {
            IVsTextView viewAdapter;
            IVsWindowFrame pWindowFrame;
            OpenDocument(filename, out viewAdapter, out pWindowFrame);

            ErrorHandler.ThrowOnFailure(pWindowFrame.Show());

            // Set the cursor at the beginning of the declaration.          
            int line, col;
            ErrorHandler.ThrowOnFailure(viewAdapter.GetLineAndColumn(pos, out line, out col));
            ErrorHandler.ThrowOnFailure(viewAdapter.SetCaretPos(line, col));
            // Make sure that the text is visible.
            viewAdapter.CenterLines(line, 1);
        }

        internal static ITextBuffer GetBufferForDocument(string filename) {
            IVsTextView viewAdapter;
            IVsWindowFrame frame;
            OpenDocument(filename, out viewAdapter, out frame);

            IVsTextLines lines;
            ErrorHandler.ThrowOnFailure(viewAdapter.GetBuffer(out lines));

            var adapter = ComponentModel.GetService<IVsEditorAdaptersFactoryService>();

            return adapter.GetDocumentBuffer(lines);
        }

        internal static IProjectLauncher GetLauncher(ILuaProject project)
        {
            return new DefaultLuaLauncher(project);
        }

        private static void OpenDocument(string filename, out IVsTextView viewAdapter, out IVsWindowFrame pWindowFrame) {
            IVsTextManager textMgr = (IVsTextManager)Current.GetService(typeof(SVsTextManager));

            IVsUIShellOpenDocument uiShellOpenDocument = Current.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            IVsUIHierarchy hierarchy;
            uint itemid;

            VsShellUtilities.OpenDocument(
                Current,
                filename,
                Guid.Empty,
                out hierarchy,
                out itemid,
                out pWindowFrame,
                out viewAdapter);
        }

        public static string InterpreterHelpUrl {
            get {
                return string.Format("http://go.microsoft.com/fwlink/?LinkId=299429&clcid=0x{0:X}",
                    CultureInfo.CurrentCulture.LCID);
            }
        }

        protected override object GetAutomationObject(string name) {
            return base.GetAutomationObject(name);
        }

        public override bool IsRecognizedFile(string filename)
        {
            return LuaProjectNode.IsLuaFile(filename);
        }

        public override Type GetLibraryManagerType()
        {
            return typeof(ILuaLibraryManager);
        }

        public string InteractiveOptions {
            get {
                return "";
            }
        }

        internal override LibraryManager CreateLibraryManager(CommonPackage package)
        {
            return new LuaLibraryManager((BabePackage)package);
        }

        public IVsSolution Solution {
            get {
                return GetService(typeof(SVsSolution)) as IVsSolution;
            }
        }

        internal SolutionEventsListener SolutionEvents {
            get {
                return _solutionEventListener;
            }
        }

        internal void GlobalInvoke(CommandID cmdID) {
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            mcs.GlobalInvoke(cmdID);
        }

        internal void GlobalInvoke(CommandID cmdID, object arg) {
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            mcs.GlobalInvoke(cmdID, arg);
        }

        public bool AutoListMembers {
            get {
                return _langPrefs.AutoListMembers;
            }
        }

        internal LanguagePreferences LangPrefs {
            get {
                return _langPrefs;
            }
        }
        
		public EnvDTE.DTE DTE {
            get {
                return DTEHelper.DTE;
            }
        }

        public IContentType ContentType {
            get {
                if (_contentType == null) {
                    _contentType = ComponentModel.GetService<IContentTypeRegistryService>().GetContentType(LuaCoreConstants.ContentType);
                }
                return _contentType;
            }
        }

        #region Open/Save File
        public string BrowseForFileOpen(IntPtr owner, string filter, string initialPath = null) {
            if (string.IsNullOrEmpty(initialPath)) {
                initialPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar;
            }

            IVsUIShell uiShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (null == uiShell) {
                using (var sfd = new System.Windows.Forms.OpenFileDialog()) {
                    sfd.AutoUpgradeEnabled = true;
                    sfd.Filter = filter;
                    sfd.FileName = Path.GetFileName(initialPath);
                    sfd.InitialDirectory = Path.GetDirectoryName(initialPath);
                    DialogResult result;
                    if (owner == IntPtr.Zero) {
                        result = sfd.ShowDialog();
                    } else {
                        result = sfd.ShowDialog(NativeWindow.FromHandle(owner));
                    }
                    if (result == DialogResult.OK) {
                        return sfd.FileName;
                    } else {
                        return null;
                    }
                }
            }

            if (owner == IntPtr.Zero) {
                ErrorHandler.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out owner));
            }

            VSOPENFILENAMEW[] openInfo = new VSOPENFILENAMEW[1];
            openInfo[0].lStructSize = (uint)Marshal.SizeOf(typeof(VSOPENFILENAMEW));
            openInfo[0].pwzFilter = filter.Replace('|', '\0') + "\0";
            openInfo[0].hwndOwner = owner;
            openInfo[0].nMaxFileName = 260;
            var pFileName = Marshal.AllocCoTaskMem(520);
            openInfo[0].pwzFileName = pFileName;
            openInfo[0].pwzInitialDir = Path.GetDirectoryName(initialPath);
            var nameArray = (Path.GetFileName(initialPath) + "\0").ToCharArray();
            Marshal.Copy(nameArray, 0, pFileName, nameArray.Length);
            try {
                int hr = uiShell.GetOpenFileNameViaDlg(openInfo);
                if (hr == VSConstants.OLE_E_PROMPTSAVECANCELLED) {
                    return null;
                }
                ErrorHandler.ThrowOnFailure(hr);
                return Marshal.PtrToStringAuto(openInfo[0].pwzFileName);
            } finally {
                if (pFileName != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(pFileName);
                }
            }
        }

        public string BrowseForFileSave(IntPtr owner, string filter, string initialPath = null) {
            if (string.IsNullOrEmpty(initialPath)) {
                initialPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar;
            }

            IVsUIShell uiShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (null == uiShell) {
                using (var sfd = new System.Windows.Forms.SaveFileDialog()) {
                    sfd.AutoUpgradeEnabled = true;
                    sfd.Filter = filter;
                    sfd.FileName = Path.GetFileName(initialPath);
                    sfd.InitialDirectory = Path.GetDirectoryName(initialPath);
                    DialogResult result;
                    if (owner == IntPtr.Zero) {
                        result = sfd.ShowDialog();
                    } else {
                        result = sfd.ShowDialog(NativeWindow.FromHandle(owner));
                    }
                    if (result == DialogResult.OK) {
                        return sfd.FileName;
                    } else {
                        return null;
                    }
                }
            }

            if (owner == IntPtr.Zero) {
                ErrorHandler.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out owner));
            }

            VSSAVEFILENAMEW[] saveInfo = new VSSAVEFILENAMEW[1];
            saveInfo[0].lStructSize = (uint)Marshal.SizeOf(typeof(VSSAVEFILENAMEW));
            saveInfo[0].pwzFilter = filter.Replace('|', '\0') + "\0";
            saveInfo[0].hwndOwner = owner;
            saveInfo[0].nMaxFileName = 260;
            var pFileName = Marshal.AllocCoTaskMem(520);
            saveInfo[0].pwzFileName = pFileName;
            saveInfo[0].pwzInitialDir = Path.GetDirectoryName(initialPath);
            var nameArray = (Path.GetFileName(initialPath) + "\0").ToCharArray();
            Marshal.Copy(nameArray, 0, pFileName, nameArray.Length);
            try {
                int hr = uiShell.GetSaveFileNameViaDlg(saveInfo);
                if (hr == VSConstants.OLE_E_PROMPTSAVECANCELLED) {
                    return null;
                }
                ErrorHandler.ThrowOnFailure(hr);
                return Marshal.PtrToStringAuto(saveInfo[0].pwzFileName);
            } finally {
                if (pFileName != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(pFileName);
                }
            }
        }

        public string BrowseForDirectory(IntPtr owner, string initialDirectory = null) {
            IVsUIShell uiShell = GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (null == uiShell) {
                using (var ofd = new FolderBrowserDialog()) {
                    ofd.RootFolder = Environment.SpecialFolder.Desktop;
                    ofd.ShowNewFolderButton = false;
                    DialogResult result;
                    if (owner == IntPtr.Zero) {
                        result = ofd.ShowDialog();
                    } else {
                        result = ofd.ShowDialog(NativeWindow.FromHandle(owner));
                    }
                    if (result == DialogResult.OK) {
                        return ofd.SelectedPath;
                    } else {
                        return null;
                    }
                }
            }

            if (owner == IntPtr.Zero) {
                ErrorHandler.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out owner));
            }

            VSBROWSEINFOW[] browseInfo = new VSBROWSEINFOW[1];
            browseInfo[0].lStructSize = (uint)Marshal.SizeOf(typeof(VSBROWSEINFOW));
            browseInfo[0].pwzInitialDir = initialDirectory;
            browseInfo[0].hwndOwner = owner;
            browseInfo[0].nMaxDirName = 260;
            IntPtr pDirName = Marshal.AllocCoTaskMem(520);
            browseInfo[0].pwzDirName = pDirName;
            try {
                int hr = uiShell.GetDirectoryViaBrowseDlg(browseInfo);
                if (hr == VSConstants.OLE_E_PROMPTSAVECANCELLED) {
                    return null;
                }
                ErrorHandler.ThrowOnFailure(hr);
                return Marshal.PtrToStringAuto(browseInfo[0].pwzDirName);
            } finally {
                if (pDirName != IntPtr.Zero) {
                    Marshal.FreeCoTaskMem(pDirName);
                }
            }
        }
        #endregion

        #region SurverNews
//        private void BrowseSurveyNewsOnIdle(object sender, ComponentManagerEventArgs e) {
//            this.OnIdle -= BrowseSurveyNewsOnIdle;

//            lock (_surveyNewsUrlLock) {
//                if (!string.IsNullOrEmpty(_surveyNewsUrl)) {
//                    OpenVsWebBrowser(_surveyNewsUrl);
//                    _surveyNewsUrl = null;
//                }
//            }
//        }

//        internal void BrowseSurveyNews(string url) {
//            lock (_surveyNewsUrlLock) {
//                _surveyNewsUrl = url;
//            }

//            this.OnIdle += BrowseSurveyNewsOnIdle;
//        }

//        private void CheckSurveyNewsThread(Uri url, bool warnIfNoneAvailable) {
//            // We can't use a simple WebRequest, because that doesn't have access
//            // to the browser's session cookies.  Cookies are used to remember
//            // which survey/news item the user has submitted/accepted.  The server 
//            // checks the cookies and returns the survey/news urls that are 
//            // currently available (availability is determined via the survey/news 
//            // item start and end date).
//            var th = new System.Threading.Thread(() =>
//            {
//                var br = new WebBrowser();
//                br.Tag = warnIfNoneAvailable;
//                br.DocumentCompleted += OnSurveyNewsDocumentCompleted;
//                br.Navigate(url);
//                Application.Run();
//            });
//            th.SetApartmentState(ApartmentState.STA);
//            th.Start();
//        }

//        private void OnSurveyNewsDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {
//            var br = (WebBrowser)sender;
//            var warnIfNoneAvailable = (bool)br.Tag;
//            if (br.Url == e.Url) {
//                List<string> available = null;

//                string json = br.DocumentText;
//                if (!string.IsNullOrEmpty(json)) {
//                    int startIndex = json.IndexOf("<PRE>");
//                    if (startIndex > 0) {
//                        int endIndex = json.IndexOf("</PRE>", startIndex);
//                        if (endIndex > 0) {
//                            json = json.Substring(startIndex + 5, endIndex - startIndex - 5);

//                            try {
//                                // Example JSON data returned by the server:
//                                //{
//                                // "cannotvoteagain": [], 
//                                // "notvoted": [
//                                //  "http://ptvs.azurewebsites.net/news/141", 
//                                //  "http://ptvs.azurewebsites.net/news/41", 
//                                // ], 
//                                // "canvoteagain": [
//                                //  "http://ptvs.azurewebsites.net/news/51"
//                                // ]
//                                //}

//                                // Description of each list:
//                                // voted: cookie found
//                                // notvoted: cookie not found
//                                // canvoteagain: cookie found, but multiple votes are allowed
//                                JavaScriptSerializer serializer = new JavaScriptSerializer();
//                                var results = serializer.Deserialize<Dictionary<string, List<string>>>(json);
//                                available = results["notvoted"];
//                            } catch (ArgumentException) {
//                            } catch (InvalidOperationException) {
//                            }
//                        }
//                    }
//                }
///*
//                if (available != null && available.Count > 0) {
//                    BrowseSurveyNews(available[0]);
//                } else if (warnIfNoneAvailable) {
//                        if (available != null) {
//                        BrowseSurveyNews(GeneralOptionsPage.SurveyNewsIndexUrl);
//                        } else {
//                        BrowseSurveyNews(LuaToolsInstallPath.GetFile("NoSurveyNewsFeed.html"));
//                    }
//                }
//*/
//                Application.ExitThread();
//            }
//        }

//        internal void CheckSurveyNews(bool forceCheckAndWarnIfNoneAvailable) {
//            bool shouldQueryServer = false;
//            if (forceCheckAndWarnIfNoneAvailable) {
//                shouldQueryServer = true;
//            } else {
//                var options = GeneralOptionsPage;
//                // Ensure that we don't prompt the user on their very first project creation.
//                // Delay by 3 days by pretending we checked 4 days ago (the default of check
//                // once a week ensures we'll check again in 3 days).
//                if (options.SurveyNewsLastCheck == DateTime.MinValue) {
//                    options.SurveyNewsLastCheck = DateTime.Now - TimeSpan.FromDays(4);
//                    options.SaveSettingsToStorage();
//                }

//                var elapsedTime = DateTime.Now - options.SurveyNewsLastCheck;
//                switch (options.SurveyNewsCheck) {
//                    case SurveyNewsPolicy.Disabled:
//                        break;
//                    case SurveyNewsPolicy.CheckOnceDay:
//                        shouldQueryServer = elapsedTime.TotalDays >= 1;
//                        break;
//                    case SurveyNewsPolicy.CheckOnceWeek:
//                        shouldQueryServer = elapsedTime.TotalDays >= 7;
//                        break;
//                    case SurveyNewsPolicy.CheckOnceMonth:
//                        shouldQueryServer = elapsedTime.TotalDays >= 30;
//                        break;
//                    default:
//                        Debug.Assert(false, String.Format("Unexpected SurveyNewsPolicy: {0}.", options.SurveyNewsCheck));
//                        break;
//                }
//            }

//            if (shouldQueryServer) {
//                var options = GeneralOptionsPage;
//                options.SurveyNewsLastCheck = DateTime.Now;
//                options.SaveSettingsToStorage();
//                CheckSurveyNewsThread(new Uri(options.SurveyNewsFeedUrl), forceCheckAndWarnIfNoneAvailable);
//            }
//        }

        #endregion

        #region IVsComponentSelectorProvider Members

        public int GetComponentSelectorPage(ref Guid rguidPage, VSPROPSHEETPAGE[] ppage) {
/*
            if (rguidPage == typeof(WebPiComponentPickerControl).GUID) {
                var page = new VSPROPSHEETPAGE();
                page.dwSize = (uint)Marshal.SizeOf(typeof(VSPROPSHEETPAGE));
                var pickerPage = new WebPiComponentPickerControl();
                if (_packageContainer == null) {
                    _packageContainer = new PackageContainer(this);
                }
                _packageContainer.Add(pickerPage);
                //IWin32Window window = pickerPage;
                page.hwndDlg = pickerPage.Handle;
                ppage[0] = page;
                return VSConstants.S_OK;
            }*/
            return VSConstants.E_FAIL;
        }

        /// <devdoc>
        ///     This class derives from container to provide a service provider
        ///     connection to the package.
        /// </devdoc>
        private sealed class PackageContainer : Container {
            private IUIService _uis;
            private AmbientProperties _ambientProperties;

            private System.IServiceProvider _provider;

            /// <devdoc>
            ///     Creates a new container using the given service provider.
            /// </devdoc>
            internal PackageContainer(System.IServiceProvider provider) {
                _provider = provider;
            }

            /// <devdoc>
            ///     Override to GetService so we can route requests
            ///     to the package's service provider.
            /// </devdoc>
            protected override object GetService(Type serviceType) {
                if (serviceType == null) {
                    throw new ArgumentNullException("serviceType");
                }
                if (_provider != null) {
                    if (serviceType.IsEquivalentTo(typeof(AmbientProperties))) {
                        if (_uis == null) {
                            _uis = (IUIService)_provider.GetService(typeof(IUIService));
                        }
                        if (_ambientProperties == null) {
                            _ambientProperties = new AmbientProperties();
                        }
                        if (_uis != null) {
                            // update the _ambientProperties in case the styles have changed
                            // since last time.
                            _ambientProperties.Font = (Font)_uis.Styles["DialogFont"];
                        }
                        return _ambientProperties;
                    }
                    object service = _provider.GetService(serviceType);

                    if (service != null) {
                        return service;
                    }
                }
                return base.GetService(serviceType);
            }
        }

        #endregion

        #region Menu Events Handler
        private void ShowSearchWindow1(object sender, EventArgs e)
        {
            WindowManager.ShowSearchWindow1();
        }

        private void ShowSearchWindow2(object sender, EventArgs e)
        {
            WindowManager.ShowSearchWindow2();
        }

        private void ShowOutlineWindow(object sender, EventArgs e)
        {
            WindowManager.ShowOutlineWindow();
        }

        private void ShowFolderWindow(object sender, EventArgs e)
        {
            WindowManager.ShowFolderWindow();
        }

        private void ShowSettingWindow(object sender, EventArgs e)
        {
            WindowManager.ShowSettingWindow();
        }

        public void RunLuaExecutable(object sender, EventArgs e)
        {
            var set = CurrentSetting;

            if (CurrentSetting == null)
            {
                System.Windows.MessageBox.Show(Properties.Resources.LoseCurrentSetting);
                WindowManager.ShowSettingWindow();
            }
            else if (string.IsNullOrWhiteSpace(CurrentSetting.LuaExecutable))
            {
                System.Windows.MessageBox.Show(Properties.Resources.GuideLuaExecutablePath);
                WindowManager.ShowSettingWindow();
                if (SettingWndPane.Current != null)
                {
                    SettingWndPane.Current.ShowToExecutable();
                }
            }
            else
            {
                try
                {
                    var pro = new System.Diagnostics.Process();

					var WorkingPath = CurrentSetting.WorkingPath;
					if (string.IsNullOrWhiteSpace(WorkingPath)) WorkingPath = System.IO.Path.GetDirectoryName(CurrentSetting.LuaExecutable);

                    pro.StartInfo.FileName = CurrentSetting.LuaExecutable;
                    pro.StartInfo.Arguments = CurrentSetting.CommandLine;
					pro.StartInfo.WorkingDirectory = WorkingPath;
                    pro.Start();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(string.Format(Properties.Resources.LuaExecutableError, ex.Message));
                }
            }
        }

        public void ShotKeyHandler(object sender, EventArgs e)
        {
            var cmd = sender as MenuCommand;
            if (cmd != null)
            {
                switch ((uint)cmd.CommandID.ID)
                {
                    case PkgCmdIDList.ShotKey0:
                    case PkgCmdIDList.ShotKey1:
                    case PkgCmdIDList.ShotKey2:
                    case PkgCmdIDList.ShotKey3:
                    case PkgCmdIDList.ShotKey4:
                    case PkgCmdIDList.ShotKey5:
                    case PkgCmdIDList.ShotKey6:
                    case PkgCmdIDList.ShotKey7:
                    case PkgCmdIDList.ShotKey8:
                    case PkgCmdIDList.ShotKey9:
                        {
                            int index = cmd.CommandID.ID-(int)PkgCmdIDList.ShotKey0;
                            string keyName = Setting.GetKeyName(index);
                            if (keyName == Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindFolder))
                            {
                                WindowManager.ShowFolderWindow();
                                //if (FolderWndPane.Current != null)
                                //{
                                //    FolderWndPane.Current.SetFocus();
                                //}
                            }
                            else if (keyName == Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindOutline))
                            {
                                WindowManager.ShowOutlineWindow();
                                //if (OutlineWndPane.Current != null)
                                //{
                                //    OutlineWndPane.Current.SetFocus();
                                //}
                            }
                            else if (keyName == Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindEditorOutlineLeft))
                            {
                                if (TextViewCreationListener.TextView != null)
                                {
									EditorManager.ShowEditorOutlineMarginLeft();
                                }
                            }
                            else if (keyName == Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindEditorOutlineRight))
                            {
								if (TextViewCreationListener.TextView != null)
                                {
									EditorManager.ShowEditorOutlineMarginRight();
                                }
                            }
                            else if (keyName == Setting.GetKeyBindingName(SettingConstants.SettingKeys.KeyBindRunExec))
                            {
                                RunLuaExecutable(null, null);
                            }
                        }
                        break;
/*                    case PkgCmdIDList.ShotKey1:
                        System.Diagnostics.Debug.Print("ShotKey1");
                        ShowFolderWindow(this, null);
                        if (FolderWndPane.Current != null)
                        {
                            FolderWndPane.Current.SetFocus();
                        }
                        break;
                    case PkgCmdIDList.ShotKey2:
                        System.Diagnostics.Debug.Print("ShotKey2");
                        ShowOutlineWindow(this, null);
                        if (OutlineWndPane.Current != null)
                        {
                            OutlineWndPane.Current.SetFocus();
                        }
                        break;
                    case PkgCmdIDList.ShotKey3:
                        System.Diagnostics.Debug.Print("ShotKey3");
                        if (DTEHelper.Current.SelectionPage != null)
                        {
                            DTEHelper.Current.ShowEditorOutlineMarginLeft();
                        }
                        break;
                    case PkgCmdIDList.ShotKey4:
                        System.Diagnostics.Debug.Print("ShotKey4");
                        if (DTEHelper.Current.SelectionPage != null)
                        {
                            DTEHelper.Current.ShowEditorOutlineMarginRight();
                        }
                        break;
                    case PkgCmdIDList.ShotKey5:
                        System.Diagnostics.Debug.Print("ShotKey5");

                        break;
                    case PkgCmdIDList.ShotKey6:
                        System.Diagnostics.Debug.Print("ShotKey6");

                        break;*/
                    default:
                        break;
/*                    case PkgCmdIDList.ShotKey7:
                        System.Diagnostics.Debug.Print("ShotKey7");

                        break;
                    case PkgCmdIDList.ShotKey8:
                        System.Diagnostics.Debug.Print("ShotKey8");

                        break;
                    case PkgCmdIDList.ShotKey9:
                        System.Diagnostics.Debug.Print("ShotKey9");

                        break;
                    case PkgCmdIDList.ShotKey0:
                        System.Diagnostics.Debug.Print("ShotKey0");

                        break;*/
                }
            }
        }
        #endregion

        private void RegisterOleComponent()
        {
            OleCom = new OleComponent();

            var ocm = this.GetService(typeof(SOleComponentManager)) as IOleComponentManager;

            if (ocm != null)
            {
                uint pwdID;
                OLECRINFO[] crinfo = new OLECRINFO[1];
                crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));

                crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedAllActiveNotifs;
                ocm.FRegisterComponent(OleCom, crinfo, out pwdID);
            }
        }

        /// <summary>
        /// 隐藏一些无关菜单项
        /// </summary>
        void HideToolBars()
        {
			if (Setting.HideUselessViews)
			{
                var cmdBars = (CommandBars)DTEHelper.DTE.CommandBars;
                var menu = cmdBars.ActiveMenuBar;
                
				foreach (CommandBarControl bar in menu.Controls)
				{
					bar.Visible = false;
				}

				menu.Controls["File"].Visible = true;
				menu.Controls["Edit"].Visible = true;
				menu.Controls["Lua"].Visible = true;
                menu.Controls["Debug"].Visible = true;
				menu.Controls["Window"].Visible = true;
				menu.Controls["Help"].Visible = true;
				menu.Controls["Tools"].Visible = true;

				var file = menu.Controls["File"] as CommandBarPopup;
				foreach (CommandBarControl btn in file.Controls)
				{
					btn.Visible = false;
				}
				file.Controls["Close"].Visible = true;
				file.Controls["Recent Files"].Visible = true;
				file.Controls["Exit"].Visible = true;
			}

//			var lua = menu.Controls["Lua"] as CommandBarPopup;
//			lua.Controls["ShotKeys"].Visible = false;
        }

        /// <summary>
        /// 添加Lua菜单项
        /// </summary>
        void AddCommandBars()
        {
            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the tool window
                CommandID Search1CmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.SearchWindow1);
                MenuCommand menuSearch1Win = new MenuCommand(ShowSearchWindow1, Search1CmdID);
                mcs.AddCommand(menuSearch1Win);

                CommandID Search2CmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.SearchWindow2);
                MenuCommand menuSearch2Win = new MenuCommand(ShowSearchWindow2, Search2CmdID);
                mcs.AddCommand(menuSearch2Win);

                CommandID outlineCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.OutlineWindow);
                MenuCommand menuOutlineWin = new MenuCommand(ShowOutlineWindow, outlineCmdID);
                mcs.AddCommand(menuOutlineWin);

                CommandID folderCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.FolderWindow);
                MenuCommand menuFolderWin = new MenuCommand(ShowFolderWindow, folderCmdID);
                mcs.AddCommand(menuFolderWin);

                CommandID settingCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.SettingWindow);
                MenuCommand menuSettingWin = new MenuCommand(ShowSettingWindow, settingCmdID);
                mcs.AddCommand(menuSettingWin);

                CommandID runCmdID = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.RunLuaExecutable);
                MenuCommand menuRun = new MenuCommand(RunLuaExecutable, runCmdID);
                mcs.AddCommand(menuRun);

                CommandID ShotKey1 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey1);
                MenuCommand MenuShotKey1 = new MenuCommand(ShotKeyHandler, ShotKey1);
                mcs.AddCommand(MenuShotKey1);

                CommandID ShotKey2 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey2);
                MenuCommand MenuShotKey2 = new MenuCommand(ShotKeyHandler, ShotKey2);
                mcs.AddCommand(MenuShotKey2);

                CommandID ShotKey3 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey3);
                MenuCommand MenuShotKey3 = new MenuCommand(ShotKeyHandler, ShotKey3);
                mcs.AddCommand(MenuShotKey3);

                CommandID ShotKey4 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey4);
                MenuCommand MenuShotKey4 = new MenuCommand(ShotKeyHandler, ShotKey4);
                mcs.AddCommand(MenuShotKey4);

                CommandID ShotKey5 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey5);
                MenuCommand MenuShotKey5 = new MenuCommand(ShotKeyHandler, ShotKey5);
                mcs.AddCommand(MenuShotKey5);

                CommandID ShotKey6 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey6);
                MenuCommand MenuShotKey6 = new MenuCommand(ShotKeyHandler, ShotKey6);
                mcs.AddCommand(MenuShotKey6);

                CommandID ShotKey7 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey7);
                MenuCommand MenuShotKey7 = new MenuCommand(ShotKeyHandler, ShotKey7);
                mcs.AddCommand(MenuShotKey7);

                CommandID ShotKey8 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey8);
                MenuCommand MenuShotKey8 = new MenuCommand(ShotKeyHandler, ShotKey8);
                mcs.AddCommand(MenuShotKey8);

                CommandID ShotKey9 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey9);
                MenuCommand MenuShotKey9 = new MenuCommand(ShotKeyHandler, ShotKey9);
                mcs.AddCommand(MenuShotKey9);

                CommandID ShotKey0 = new CommandID(GuidList.CmdSetString, (int)PkgCmdIDList.ShotKey0);
                MenuCommand MenuShotKey0 = new MenuCommand(ShotKeyHandler, ShotKey0);
                mcs.AddCommand(MenuShotKey0);
            }
        }

        public int AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
        {
            throw new NotImplementedException();
        }

        public int Close()
        {
            throw new NotImplementedException();

        }

        public new object GetService(Type service)
        {
            return base.GetService(service);
        }

        /// <summary>
        /// 获取当前选中的Lua设置项。可能为空。
        /// </summary>
        public LuaSet CurrentSetting
        {
            get
            {
                return Setting.GetSetting(Setting.CurrentSetting);
            }
        }
    }
}
