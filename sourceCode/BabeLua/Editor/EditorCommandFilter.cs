using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudioTools.Project;
using System;

using Babe.Lua.Helper;
using Babe.Lua.DataModel;
using Babe.Lua.Package;

namespace Babe.Lua.Editor
{
    /// <summary>
    /// Editor Command Extend.
    /// So far we support two commands : Goto Definition & CommentRegion/UnCommentRegion
    /// </summary>
    internal class EditorCommandFilter : IOleCommandTarget
    {
        public EditorCommandFilter(IWpfTextView textView)
        {
            TextView = textView;
        }
        
        public IWpfTextView TextView { get; private set; }

        public IOleCommandTarget Next { get; set; }

        int GotoDefinition()
        {
            var span = TextView.GetToken(TextView.Caret.Position.BufferPosition);

            var token = span.GetText();

            var member = FileManager.Instance.FindDefination(token);

            if (member == null)
            {
                System.Diagnostics.Debug.WriteLine("can't find definition : " + token);
            }
            else
            {
                EditorManager.GoTo(member.File.Path, member.Line, member.Column);
            }

            return VSConstants.S_OK;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VsMenus.guidStandardCommandSet97)
            {
                switch ((VSConstants.VSStd97CmdID)nCmdID)
                {
                    case VSConstants.VSStd97CmdID.GotoDefn:
                        return GotoDefinition();
                }
            }
            else if (pguidCmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                    case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
                        if (EditorExtensions.CommentOrUncommentBlock(TextView, comment: true)) {
                            return VSConstants.S_OK;
                        }
                        break;
                    case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                    case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
                        if (EditorExtensions.CommentOrUncommentBlock(TextView, comment: false))
                        {
                            return VSConstants.S_OK;
                        }
                        break;
                }
            }

            return Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch ((VSConstants.VSStd97CmdID)prgCmds[i].cmdID)
                    {
                        case VSConstants.VSStd97CmdID.GotoDefn:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                        case VSConstants.VSStd97CmdID.FindReferences:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }
            else if (pguidCmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch ((VSConstants.VSStd2KCmdID)prgCmds[i].cmdID)
                    {
                        case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
                        case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
                        case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
                        case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                            return VSConstants.S_OK;
                    }
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}