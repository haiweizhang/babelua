using Babe.Lua.Editor;
using Babe.Lua.Package;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babe.Lua.ToolWindows
{
    class WindowManager
    {
        ISearchWnd CurrentSearchWnd;
        public void WindowActivate(Window GotFocus, Window LostFocus)
        {
            if (GotFocus.ObjectKind.Contains(GuidList.SearchWindowString1))
            {
                CurrentSearchWnd = SearchWndPane1.Current;
            }
            else if (GotFocus.ObjectKind.Contains(GuidList.SearchWindowString2))
            {
                CurrentSearchWnd = SearchWndPane2.Current;
            }
            if (LostFocus != null)
            {
                if (LostFocus.ObjectKind.Contains(GuidList.OutlineWindowString))
                {
                    OutlineWndPane.Current.LostFocus();
                }
                else if (LostFocus.ObjectKind.Contains(GuidList.FolderWindowString))
                {
                    FolderWndPane.Current.LostFocus();
                }
            }
        }

        public void SearchWndClosed(ISearchWnd wnd)
        {
            if (wnd != CurrentSearchWnd) return;

            if (wnd == SearchWndPane1.Current)
            {
                CurrentSearchWnd = SearchWndPane2.Current;
            }
            else if (wnd == SearchWndPane2.Current)
            {
                CurrentSearchWnd = SearchWndPane1.Current;
            }
        }

        public void RefreshSearchWnd(string txt, bool AllFile, bool CaseSensitive, bool WholeWordMatch = true)
        {
            if (CurrentSearchWnd == null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                     ShowSearchWindow1()
                    );
            }
            if (CurrentSearchWnd != null)
            {
                if (CurrentSearchWnd == SearchWndPane1.Current)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(
                     () => ShowSearchWindow1()
                    );
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(
                     () => ShowSearchWindow2()
                    );
                }
                CurrentSearchWnd.Search(txt, AllFile, WholeWordMatch, CaseSensitive);
            }
        }

        public void SetSearchWndRelativePathEnable(bool enable)
        {
            if(SearchWndPane1.Current != null)
            {
                SearchWndPane1.Current.SetRelativePathEnable(enable);
            }
            if(SearchWndPane2.Current != null)
            {
                SearchWndPane2.Current.SetRelativePathEnable(enable);
            }
        }

        public void RefreshOutlineWnd()
        {
            if (OutlineWndPane.Current != null) OutlineWndPane.Current.Refresh();
        }

        public void RefreshFolderWnd()
        {
            if (FolderWndPane.Current != null) FolderWndPane.Current.Refresh();
        }

        public void RefreshEditorOutline()
        {
            if (EditorMarginProvider.CurrentMargin != null)
            {
                EditorMarginProvider.CurrentMargin.Refresh();
            }
        }

        public void UpdateUI()
        {
            RefreshFolderWnd();
            RefreshOutlineWnd();
            RefreshEditorOutline();
        }

        public void ShowSearchWindow1()
        {
            ToolWindowPane window = BabePackage.Current.FindToolWindow(typeof(SearchWndPane1), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, new WindowFrameSink(SearchWndPane1.Current)));

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowSearchWindow2()
        {
            ToolWindowPane window = BabePackage.Current.FindToolWindow(typeof(SearchWndPane2), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, new WindowFrameSink(SearchWndPane2.Current)));

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowOutlineWindow()
        {
            ToolWindowPane window = BabePackage.Current.FindToolWindow(typeof(OutlineWndPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            //windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_Dock);

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowFolderWindow()
        {
            ToolWindowPane window = BabePackage.Current.FindToolWindow(typeof(FolderWndPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void ShowSettingWindow()
        {
            ToolWindowPane window = BabePackage.Current.FindToolWindow(typeof(SettingWndPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Properties.Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;

            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
