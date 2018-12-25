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
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Project;
#if DEV11_OR_LATER
using Microsoft.VisualStudio.Shell.Interop;
#endif

namespace Microsoft.LuaTools.Project {
    //Set the projectsTemplatesDirectory to a non-existant path to prevent VS from including the working directory as a valid template path
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Description("Lua Project Package")]
    [ProvideProjectFactory(typeof(LuaProjectFactory), LuaConstants.LanguageName, LuaFileFilter, "luaproj", "luaproj", @"Templates\Projects\LuaProject"/*".\\NullPath"*/, LanguageVsTemplate = LuaConstants.LanguageName)]

    [ProvideObject(typeof(LuaGeneralPropertyPage))]
//    [ProvideObject(typeof(LuaDebugPropertyPage))]
//    [ProvideObject(typeof(PublishPropertyPage))]

    [ProvideEditorExtension(typeof(LuaEditorFactory), LuaConstants.FileExtension, 50, ProjectGuid = VSConstants.CLSID.MiscellaneousFilesProject_string, NameResourceID = 3004, DefaultName = "module", TemplateDir = "Templates\\NewItem")]
    #if DEV11_OR_LATER
    [ProvideEditorExtension2(typeof(LuaEditorFactory), LuaConstants.FileExtension, 50, __VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, "*:1", ProjectGuid = LuaConstants.ProjectFactoryGuid, NameResourceID = 3004, DefaultName = "module", TemplateDir = @"Templates\Files\LuaFile")]//".\\NullPath"
    [ProvideEditorExtension2(typeof(LuaEditorFactory), LuaConstants.WindowsFileExtension, 60, __VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, null, ProjectGuid = LuaConstants.ProjectFactoryGuid, NameResourceID = 3004, DefaultName = "module", TemplateDir = @"Templates\Files\LuaFile")]//".\\NullPath"
    [ProvideEditorExtension2(typeof(LuaEditorFactoryPromptForEncoding), LuaConstants.FileExtension, 50, __VSPHYSICALVIEWATTRIBUTES.PVA_SupportsPreview, ProjectGuid = LuaConstants.ProjectFactoryGuid, NameResourceID = 3015, LinkedEditorGuid = LuaConstants.EditorFactoryGuid, TemplateDir = @"Templates\Files\LuaFile")]//".\\NullPath"
#else
    [ProvideEditorExtension2(typeof(LuaEditorFactory), LuaConstants.FileExtension, 50, "*:1", ProjectGuid = LuaConstants.ProjectFactoryGuid, NameResourceID = 3004, DefaultName = "module", TemplateDir =  @"Templates\Files\LuaFile")]//".\\NullPath"
    [ProvideEditorExtension2(typeof(LuaEditorFactory), LuaConstants.WindowsFileExtension, 60, ProjectGuid = LuaConstants.ProjectFactoryGuid, NameResourceID = 3004, DefaultName = "module", TemplateDir =  @"Templates\Files\LuaFile")]//".\\NullPath"
    [ProvideEditorExtension2(typeof(LuaEditorFactoryPromptForEncoding), LuaConstants.FileExtension, 50, ProjectGuid = LuaConstants.ProjectFactoryGuid, NameResourceID = 3015, LinkedEditorGuid = LuaConstants.EditorFactoryGuid, TemplateDir =  @"Templates\Files\LuaFile")]//".\\NullPath"
#endif
/*
    [ProvideFileFilter(LuaConstants.ProjectFactoryGuid, "/1", "Lua Files;*.lua,*.luaw", 100)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactory), VSConstants.LOGVIEWID.TextView_string)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactory), VSConstants.LOGVIEWID.Designer_string)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactory), VSConstants.LOGVIEWID.Code_string)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactory), VSConstants.LOGVIEWID.Debugging_string)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactoryPromptForEncoding), VSConstants.LOGVIEWID.TextView_string)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactoryPromptForEncoding), VSConstants.LOGVIEWID.Designer_string)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactoryPromptForEncoding), VSConstants.LOGVIEWID.Code_string)]
    [ProvideEditorLogicalView(typeof(LuaEditorFactoryPromptForEncoding), VSConstants.LOGVIEWID.Debugging_string)]
*/
    [Guid(LuaConstants.ProjectSystemPackageGuid)]
    [DeveloperActivity("Lua", typeof(LuaProjectPackage))]
    public class LuaProjectPackage : CommonProjectPackage {
        internal const string LuaFileFilter = "Lua Project Files (*.luaproj);*.luaproj";

        public override ProjectFactory CreateProjectFactory() {
            return new LuaProjectFactory(this);
        }

        public override CommonEditorFactory CreateEditorFactory() {
            return null;
//            return new LuaEditorFactory(this);
        }

        public override CommonEditorFactory CreateEditorFactoryPromptForEncoding() {
            return null;
//            return new LuaEditorFactoryPromptForEncoding(this);
        }

        /// <summary>
        /// This method is called to get the icon that will be displayed in the
        /// Help About dialog when this package is selected.
        /// </summary>
        /// <returns>The resource id corresponding to the icon to display on the Help About dialog</returns>
        public override uint GetIconIdForAboutBox() {
            return LuaConstants.IconIdForAboutBox;
        }
        /// <summary>
        /// This method is called during Devenv /Setup to get the bitmap to
        /// display on the splash screen for this package.
        /// </summary>
        /// <returns>The resource id corresponding to the bitmap to display on the splash screen</returns>
        public override uint GetIconIdForSplashScreen() {
            return LuaConstants.IconIfForSplashScreen;
        }
        /// <summary>
        /// This methods provides the product official name, it will be
        /// displayed in the help about dialog.
        /// </summary>
        public override string GetProductName() {
            return LuaConstants.LanguageName;
        }

        /// <summary>
        /// This methods provides the product description, it will be
        /// displayed in the help about dialog.
        /// </summary>
        public override string GetProductDescription() {
            return LuaConstants.LanguageName;
            //return Resources.ProductDescription;
        }
        /// <summary>
        /// This methods provides the product version, it will be
        /// displayed in the help about dialog.
        /// </summary>
        public override string GetProductVersion() {
            return this.GetType().Assembly.GetName().Version.ToString();
        }
    }
}
