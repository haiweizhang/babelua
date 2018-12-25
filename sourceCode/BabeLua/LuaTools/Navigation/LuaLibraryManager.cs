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
using System.Runtime.InteropServices;
using Babe.Lua;
using Babe.Lua.Package;
//using Microsoft.LuaTools.Analysis;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Navigation;
using Microsoft.VisualStudioTools.Project;

namespace Microsoft.LuaTools.Navigation {

    /// <summary>
    /// This interface defines the service that finds Lua files inside a hierarchy
    /// and builds the informations to expose to the class view or object browser.
    /// </summary>
    [Guid(LuaConstants.LibraryManagerServiceGuid)]
    internal interface ILuaLibraryManager : ILibraryManager {
    }

    /// <summary>
    /// Implementation of the service that builds the information to expose to the symbols
    /// navigation tools (class view or object browser) from the Lua files inside a
    /// hierarchy.
    /// </summary>
    [Guid(LuaConstants.LibraryManagerGuid)]
    internal class LuaLibraryManager : LibraryManager, ILuaLibraryManager {
        private readonly BabePackage/*!*/ _package;

        public LuaLibraryManager(BabePackage/*!*/ package)
            : base(package) {
            _package = package;
        }

        protected override LibraryNode CreateLibraryNode(LibraryNode parent, IScopeNode subItem, string namePrefix, IVsHierarchy hierarchy, uint itemid) {
            return new LuaLibraryNode(parent, subItem, namePrefix, hierarchy, itemid);            
        }

        public override LibraryNode CreateFileLibraryNode(LibraryNode parent, HierarchyNode hierarchy, string name, string filename, LibraryNodeType libraryNodeType) {
            return new LuaFileLibraryNode(parent, hierarchy, hierarchy.Caption, filename, libraryNodeType);
        }

        protected override void OnNewFile(LibraryTask task) {
            if (IsNonMemberItem(task.ModuleID.Hierarchy, task.ModuleID.ItemID)) {
                return;
            }
/*
            IProjectEntry item;
            if (task.TextBuffer != null) {
                item = task.TextBuffer.GetAnalysis();
            } else {                
                item = task.ModuleID.Hierarchy.GetProject().GetLuaProject().GetAnalyzer().AnalyzeFile(task.FileName);
            }

            ILuaProjectEntry pyCode;
            if (item != null && (pyCode = item as ILuaProjectEntry) != null) {
                // We subscribe to OnNewAnalysis here instead of OnNewParseTree so that 
                // in the future we can use the analysis to include type information in the
                // object browser (for example we could include base type information with
                // links elsewhere in the object browser).
                pyCode.OnNewAnalysis += (sender, args) => {
                    FileParsed(task, new AstScopeNode(pyCode.Tree, pyCode));
                };
            }*/
        }
    }
}
