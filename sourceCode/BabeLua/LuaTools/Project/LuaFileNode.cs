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
using System.Text;
/*
using Microsoft.LuaTools.Analysis;
using Microsoft.LuaTools.Intellisense;
*/
using Microsoft.VisualStudio;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Project;

namespace Microsoft.LuaTools.Project {

    internal class LuaFileNode : CommonFileNode {
        internal LuaFileNode(CommonProjectNode root, ProjectElement e)
            : base(root, e) { }

        public override string Caption {
            get {
                var res = base.Caption;
                if (res == "__init__.py" && Parent != null) {
                    StringBuilder fullName = new StringBuilder(res);
                    fullName.Append(" (");

                    GetPackageName(this, fullName);

                    fullName.Append(")");
                    res = fullName.ToString();
                }
                return res;
            }
        }

        internal static void GetPackageName(HierarchyNode self, StringBuilder fullName) {
            List<HierarchyNode> nodes = new List<HierarchyNode>();
            var curNode = self.Parent;
            do {
                nodes.Add(curNode);
                curNode = curNode.Parent;
            } while (curNode != null && curNode.FindImmediateChildByName("__init__.py") != null);

            for (int i = nodes.Count - 1; i >= 0; i--) {
                fullName.Append(GetNodeNameForPackage(nodes[i]));
                if (i != 0) {
                    fullName.Append('.');
                }
            }
        }

        private static string GetNodeNameForPackage(HierarchyNode node) {
            var project = node as ProjectNode;
            if (project != null) {
                return Path.GetFileName(CommonUtils.TrimEndSeparator(project.ProjectHome));
            } else {
                return node.Caption;
            }
        }

        internal override int ExecCommandOnNode(Guid guidCmdGroup, uint cmd, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
            Debug.Assert(this.ProjectMgr != null, "The Dynamic FileNode has no project manager");
            Utilities.CheckNotNull(this.ProjectMgr);
/*
            if (guidCmdGroup == GuidList.guidLuaToolsCmdSet) {
                switch (cmd) {
                    case CommonConstants.SetAsStartupFileCmdId:
                        // Set the StartupFile project property to the Url of this node
                        ProjectMgr.SetProjectProperty(
                            CommonConstants.StartupFile,
                            CommonUtils.GetRelativeFilePath(this.ProjectMgr.ProjectHome, Url)
                        );
                        return VSConstants.S_OK;
                    case CommonConstants.StartDebuggingCmdId:
                    case CommonConstants.StartWithoutDebuggingCmdId:
                        CommonProjectPackage package = (CommonProjectPackage)ProjectMgr.Package;
                        IProjectLauncher starter = ((CommonProjectNode)ProjectMgr).GetLauncher();
                        if (starter != null) {
                            if (!Utilities.SaveDirtyFiles()) {
                                // Abort
                                return VSConstants.E_ABORT;
                            }

                            starter.LaunchFile(this.Url, cmd == CommonConstants.StartDebuggingCmdId);
                        }
                        return VSConstants.S_OK;
                }
            }
*/
            return base.ExecCommandOnNode(guidCmdGroup, cmd, nCmdexecopt, pvaIn, pvaOut);
        }

        internal override int QueryStatusOnNode(Guid guidCmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result) {
/*
            if (guidCmdGroup == GuidList.guidLuaToolsCmdSet) {
                if (this.ProjectMgr.IsCodeFile(this.Url)) {
                    switch (cmd) {
                        case CommonConstants.SetAsStartupFileCmdId:
                            //We enable "Set as StartUp File" command only on current language code files, 
                            //the file is in project home dir and if the file is not the startup file already.
                            string startupFile = ((CommonProjectNode)ProjectMgr).GetStartupFile();
                            if (IsInProjectHome() && 
                                !CommonUtils.IsSamePath(startupFile, Url) &&
                                !IsNonMemberItem) {
                                result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                            }
                            return VSConstants.S_OK;
                        case CommonConstants.StartDebuggingCmdId:
                        case CommonConstants.StartWithoutDebuggingCmdId:
                            result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                            return VSConstants.S_OK;
                    }
                }
            }
*/
            return base.QueryStatusOnNode(guidCmdGroup, cmd, pCmdText, ref result);
        }

        private bool IsInProjectHome() {
            HierarchyNode parent = this.Parent;
/*
            while (parent != null) {
                if (parent is CommonSearchPathNode) {
                    return false;
                }
                parent = parent.Parent;
            }*/
            return true;
        }

        public override void Remove(bool removeFromStorage) {
//            ((LuaProjectNode)ProjectMgr).GetAnalyzer().UnloadFile(GetAnalysis());
            base.Remove(removeFromStorage);
        }

        public override string GetEditLabel() {
            if (IsLinkFile) {
                // cannot rename link files
                return null;
            }
            // dispatch to base class which doesn't include package name, just filename.
            return base.Caption;
        }

        public override string FileName {
            get {
                return base.Caption;
            }
            set {
                base.FileName = value;
            }
        }
/*
        public IProjectEntry GetAnalysis() {
            var textBuffer = GetTextBuffer();

            IProjectEntry analysis;
            if (textBuffer != null && textBuffer.TryGetAnalysis(out analysis)) {
                return analysis;
            }

            return ((LuaProjectNode)this.ProjectMgr).GetAnalyzer().GetAnalysisFromFile(Url);
        }
*/
        internal override FileNode RenameFileNode(string oldFileName, string newFileName) {
            var res = base.RenameFileNode(oldFileName, newFileName);
/*
            if (res != null) {
                var analyzer = ((LuaProjectNode)this.ProjectMgr).GetAnalyzer();
                var analysis = GetAnalysis();
                if (analysis != null) {
                    analyzer.UnloadFile(analysis);
                }

                var textBuffer = GetTextBuffer();

                BufferParser parser;
                if (textBuffer != null && textBuffer.Properties.TryGetProperty<BufferParser>(typeof(BufferParser), out parser)) {
                    analyzer.ReAnalyzeTextBuffers(parser);
                }

            }
*/
            return res;
        }

        internal override int IncludeInProject(bool includeChildren) {
/*
            var analyzer = ((LuaProjectNode)this.ProjectMgr).GetAnalyzer();
            analyzer.AnalyzeFile(Url);
*/
            return base.IncludeInProject(includeChildren);
        }

        internal override int ExcludeFromProject() {
/*
            var analyzer = ((LuaProjectNode)this.ProjectMgr).GetAnalyzer();
            var analysis = GetAnalysis();
            if (analysis != null) {
                analyzer.UnloadFile(analysis);
            }
*/
            return base.ExcludeFromProject();
        }
    }
}
