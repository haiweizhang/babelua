using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Babe.Lua.Package;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Babe.Lua.Helper;

namespace Babe.Lua.Editor
{
    class EditorManager
    {
        /// <summary>
        /// Get a IVsTextView interface that the ActiveDocument use
        /// </summary>
        /// <returns></returns>
        public static IVsTextView GetCurrentTextView()
        {
            var monitorSelection = (IVsMonitorSelection)BabePackage.GetGlobalService(typeof(SVsShellMonitorSelection));
            if (monitorSelection == null)
            {
                return null;
            }
            object curDocument;
            if (ErrorHandler.Failed(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out curDocument)))
            {
                // TODO: Report error
                return null;
            }

            IVsWindowFrame frame = curDocument as IVsWindowFrame;
            if (frame == null)
            {
                // TODO: Report error
                return null;
            }

            object docView = null;
            if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out docView)))
            {
                // TODO: Report error
                return null;
            }

            if (docView is IVsCodeWindow)
            {
                IVsTextView textView;
                if (ErrorHandler.Failed(((IVsCodeWindow)docView).GetPrimaryView(out textView)))
                {
                    // TODO: Report error
                    return null;
                }
                return textView;
            }
            return null;
        }

        /// <summary>
        /// Open file , set cursor position, select tokens and highlight tokens
        /// </summary>
        /// <param name="file">file to open.null means current document.</param>
        /// <param name="line">cursor line</param>
        /// <param name="column">cursor column</param>
        /// <param name="length">selection length</param>
        /// <param name="highlight">highlight the token cursor at or not</param>
        public static void GoTo(string file, int line = 0, int column = 0, int length = 0, bool highlight = false)
        {
            IVsTextView view;
            if (file == null)
            {
                if (BabePackage.DTEHelper.DTE.ActiveDocument == null)
                {
                    Logger.LogMessage("EditorManager.Goto Fail : file is null.");
                    return;
                }
                else
                {
                    view = GetCurrentTextView();
                }
            }
            else
            {
                if (BabePackage.DTEHelper.DTE.ActiveDocument == null)
                {
                    view = PreviewDocument(file);
                }
                else if(BabePackage.DTEHelper.DTE.ActiveDocument.FullName != file)
                {
                    view = PreviewDocument(file);
                }
                else
                {
                    view = GetCurrentTextView();
                }
            }

            if (line != 0 || column != 0)
            {
                view.SetCaretPos(line, column);
                view.SetSelection(line, column, line, column + length);
                view.CenterLines(line, 1);
            }

            MarkLine(line);

            if (highlight) HighlightPosition(line, column, length);
        }

        public static IVsTextView OpenDocument(string file, bool linkToProject = true, bool focus = true)
        {
            if (linkToProject)
            {
                LuaProject.AddFileLinkToCurrentLuaProjectOrCreate(BabePackage.DTEHelper.DTE, file);
            }
            BabePackage.DTEHelper.DTE.ItemOperations.OpenFile(file, EnvDTE.Constants.vsViewKindPrimary);
            //IVsTextManager textMgr = (IVsTextManager)BabePackage.Current.GetService(typeof(SVsTextManager));

            //IVsUIShellOpenDocument uiShellOpenDocument = BabePackage.Current.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            //IVsUIHierarchy hierarchy;
            //uint itemid;
            //IVsTextView viewAdapter;
            //IVsWindowFrame pWindowFrame;

            //VsShellUtilities.OpenDocument(
            //    BabePackage.Current,
            //    file,
            //    Guid.Empty,
            //    out hierarchy,
            //    out itemid,
            //    out pWindowFrame,
            //    out viewAdapter);

            //if (focus)
            //{
            //    ErrorHandler.ThrowOnFailure(pWindowFrame.Show());
            //}
            //else
            //{
            //    ErrorHandler.ThrowOnFailure(pWindowFrame.ShowNoActivate());
            //}

            var viewAdapter = GetCurrentTextView();
            return viewAdapter;
        }

        public static IVsTextView PreviewDocument(string file)
        {
            IVsTextView view;
            using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, Microsoft.VisualStudio.VSConstants.NewDocumentStateReason.Navigation))
            {
                //BabePackage.DTEHelper.DTE.ItemOperations.OpenFile(file, EnvDTE.Constants.vsViewKindPrimary);
                view = OpenDocument(file, true, false);
            }
            return view;
        }

        public static void MarkLine(int line)
        {
            Editor.MarkPosTaggerProvider.CurrentTagger.ShowTag(line);
        }

        public static void HighlightCurrentPosition()
        {
            FindWordTaggerProvider.CurrentTagger.UpdateAtCaretPosition(TextViewCreationListener.TextView.Caret.Position);
        }

        public static void HighlightPosition(int position, int length)
        {
            FindWordTaggerProvider.CurrentTagger.UpdateAtPosition(position, length);
        }

        public static void HighlightPosition(int line, int column, int length)
        {
            FindWordTaggerProvider.CurrentTagger.UpdateAtPosition(line, column, length);
        }

        public static void ShowEditorOutlineMarginLeft()
        {
            EditorMarginProvider.CurrentMargin.OpenLeftOutline();
        }

        public static void ShowEditorOutlineMarginRight()
        {
            EditorMarginProvider.CurrentMargin.OpenRightOutline();
        }

        public static void SearchSelect(bool AllFiles, bool WholeWordMatch, bool CaseSensitive)
        {
            if (TextViewCreationListener.TextView == null) return;
            if (TextViewCreationListener.TextView.Selection == null) return;
            var spans = TextViewCreationListener.TextView.Selection.SelectedSpans;
            if (spans.Count > 1) return;
            var txt = TextViewCreationListener.TextView.TextSnapshot.GetText(spans[0]);
            if (string.IsNullOrWhiteSpace(txt)) return;

            if (BabePackage.Setting.ContainsSearchFilter(txt)) return;

            if (WholeWordMatch)
            {
                if (!txt.All(ch => { return ch.IsWord(); })) return;
                HighlightPosition(spans[0].Start, spans[0].Length);
            }

            BabePackage.WindowManager.RefreshSearchWnd(txt, AllFiles, CaseSensitive, WholeWordMatch);
        }
    }
}
