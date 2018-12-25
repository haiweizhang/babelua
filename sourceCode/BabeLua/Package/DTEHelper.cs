using Babe.Lua.DataModel;
using Babe.Lua.Editor;
using Babe.Lua.Helper;
using Babe.Lua.ToolWindows;
using EnvDTE;
using Microsoft.LuaTools.Navigation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Diagnostics;

namespace Babe.Lua.Package
{
    /// <summary>
    /// 提供对DTE对象的功能支持
    /// </summary>
    class DTEHelper
    {
        public DTE DTE { get; private set; }

        DocumentEvents docEvents;
        CommandEvents cmdEvents;
        SolutionEvents solEvents;
        WindowEvents wndEvents;
        DTEEvents dteEvents;
		
        public DTEHelper(DTE dte)
        {
            this.DTE = dte;

            //docEvents = DTE.Events.DocumentEvents;
            //docEvents.DocumentOpening += DocumentEvents_DocumentOpening;
            //docEvents.DocumentClosing += VSPackage1Package_DocumentClosing;

            //VS关闭命令事件
            //cmdEvents = DTE.Events.CommandEvents["{5EFC7975-14BC-11CF-9B2B-00AA00573819}", 229];
            //cmdEvents.BeforeExecute += cmdEvents_BeforeVSExit;
            
            //solEvents = DTE.Events.SolutionEvents;
            //solEvents.Opened += solEvents_Opened;
            
            wndEvents = DTE.Events.WindowEvents;
            wndEvents.WindowActivated += wndEvents_WindowActivated;

            dteEvents = DTE.Events.DTEEvents;
            dteEvents.OnStartupComplete += dteEvents_OnStartupComplete;
            dteEvents.OnBeginShutdown += dteEvents_OnBeginShutdown;
        }

        void dteEvents_OnStartupComplete()
        {
            if (BabePackage.Setting.IsFirstInstall)
            {
                BabePackage.WindowManager.ShowSettingWindow();
                //BabePackage.Current.ShowFolderWindow(null, null);
                //BabePackage.Current.ShowOutlineWindow(null, null);
                //var props = DTE.Properties["TextEditor", "Lua"];
                //props.Item("IndentStyle").Value = EnvDTE.vsIndentStyle.vsIndentStyleDefault;
            }
            else if (!string.IsNullOrWhiteSpace(BabePackage.Setting.CurrentSetting))
            {
                IntellisenseHelper.Scan();
            }

            if (BabePackage.Setting.HideUselessViews)
            {
                HiddenVSWindows();
            }

            //打开上次关闭时打开的文件

            //启动完成。开始初始化。

            //如果是此版本第一次运行，更改缩进设置项为“块”
            if (Properties.Settings.Default.IsFirstRun)
            {
                IVsTextManager textMgr = (IVsTextManager)BabePackage.Current.GetService(typeof(SVsTextManager));
                var langPrefs = new LANGPREFERENCES[1];
                langPrefs[0].guidLang = typeof(LuaLanguageInfo).GUID;
                ErrorHandler.ThrowOnFailure(textMgr.GetUserPreferences(null, null, langPrefs, null));
                langPrefs[0].IndentStyle = Microsoft.VisualStudio.TextManager.Interop.vsIndentStyle.vsIndentStyleDefault;
                textMgr.SetUserPreferences(null, null, langPrefs, null);
                Properties.Settings.Default.IsFirstRun = false;
                Properties.Settings.Default.Save();
            }

            //注册编辑器内容变化通知
            TextViewCreationListener.FileContentChanged += TextViewCreationListener_FileContentChanged;
            Logger.UploadLog();
            Updater.CheckVersion();
        }

        void dteEvents_OnBeginShutdown()
        {
            IntellisenseHelper.Stop();
        }

		void TextViewCreationListener_FileContentChanged(object sender, FileContentChangedEventArgs e)
		{
            if (e.Tree.Root == null) return;
			IntellisenseHelper.Refresh(e.Tree);
		}

        void wndEvents_WindowActivated(Window GotFocus, Window LostFocus)
        {
            BabePackage.WindowManager.WindowActivate(GotFocus, LostFocus);
        }

        public void SetStatusBarText(string text)
        {
            DTE.StatusBar.Text = text;
        }

		public void AddErrorToErrorListWindow(string error)
		{
			ErrorListProvider errorProvider = new ErrorListProvider(BabePackage.Current);
			Microsoft.VisualStudio.Shell.Task newError = new Microsoft.VisualStudio.Shell.Task();
			newError.Category = TaskCategory.BuildCompile;
			newError.Text = error;
			errorProvider.Tasks.Add(newError);
		}

        public void OutputWindowWriteLine(string text)
        {
            const string DEBUG_OUTPUT_PANE_GUID = "{FC076020-078A-11D1-A7DF-00A0C9110051}";
            EnvDTE.Window window = (EnvDTE.Window)BabePackage.Current.DTE.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            window.Visible = true;
            EnvDTE.OutputWindow outputWindow = (EnvDTE.OutputWindow)window.Object;
            foreach (EnvDTE.OutputWindowPane outputWindowPane in outputWindow.OutputWindowPanes)
            {
                if (outputWindowPane.Guid.ToUpper() == DEBUG_OUTPUT_PANE_GUID)
                {
                    outputWindowPane.OutputString(text + "\r\n");
                }
            }
        }

        public StatusBar GetStatusBar()
        {
            return DTE.StatusBar;
        }

        public void HiddenVSWindow(string vsWindowsKind)
        {
            Window window = DTE.Windows.Item(vsWindowsKind);
            if (window != null)
            {
                window.Visible = false;
            }
        }

        public void HiddenVSWindows()
        {
            HiddenVSWindow(EnvDTE.Constants.vsext_wk_SProjectWindow);       //显示解决方案及其项目的“项目”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindFindResults1);      //“查找结果 1”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindFindResults2);      //“查找结果 2”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindClassView);         //“类视图”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindCommandWindow);     //“命令”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindDocumentOutline);   //“文档大纲”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindDynamicHelp);       //“动态帮助”窗口
            //            HiddenVSWindow(EnvDTE.Constants.vsWindowKindMacroExplorer);     //“Macro 资源管理器”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindObjectBrowser);     //“对象浏览器”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindOutput);            //“输出”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindProperties);        //“属性”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindResourceView);      //“资源编辑器”
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindServerExplorer);    //“服务器资源管理器”
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindSolutionExplorer);  //“解决方案资源管理器”
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindTaskList);          //“任务列表”窗口
            HiddenVSWindow(EnvDTE.Constants.vsWindowKindToolbox);           //“工具箱”
        }

        #region not use now
        void solEvents_Opened()
        {
            var projects = DTE.Solution.Projects;
        }

        void cmdEvents_BeforeVSExit(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            IntellisenseHelper.Stop();
            //var Files = new System.Xml.Linq.XElement(BabePackage.Setting.OpenFiles.Name);

            //foreach (Document doc in DTE.Documents)
            //{
            //    if (doc != DTE.ActiveDocument)
            //    {
            //        var file = new System.Xml.Linq.XElement("File");
            //        file.Value = doc.FullName;
            //        Files.Add(file);
            //    }
            //}

            //if (DTE.ActiveDocument != null)
            //{
            //    var file = new System.Xml.Linq.XElement("Active");
            //    file.Value = DTE.ActiveDocument.FullName;
            //}
        }

        void SolutionEvents_Opened()
        {
            Debug.Print("project opened");
        }

        void VSPackage1Package_DocumentClosing(Document Document)
        {
            Debug.Print("document closed");
        }

        void DocumentEvents_DocumentOpening(string DocumentPath, bool ReadOnly)
        {
            //LuaLanguage.Helper.IntellisenseHelper.SetFile(DocumentPath);
        }

        public void ExecuteCmd(string cmd, string args = null)
        {
            if (DTE == null) return;
            DTE.ExecuteCommand(cmd, args);
        }
        #endregion
    }
}
