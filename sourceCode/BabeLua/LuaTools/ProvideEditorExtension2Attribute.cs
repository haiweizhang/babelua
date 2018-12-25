﻿/* ****************************************************************************
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
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudioTools;
#if DEV11_OR_LATER
using Microsoft.VisualStudio.Shell.Interop;
#endif

namespace Microsoft.LuaTools {

    /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute"]' />
    /// <devdoc>
    ///     This attribute associates a file extension to a given editor factory.  
    ///     The editor factory may be specified as either a GUID or a type and 
    ///     is placed on a package.
    ///     
    /// This differs from the normal one in that more than one extension can be supplied and
    /// a linked editor GUID can be supplied.
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class ProvideEditorExtension2Attribute : RegistrationAttribute {
        private Guid _factory;
        private string _extension;
        private int _priority;
        private Guid _project;
        private string _templateDir;
        private int _resId;
        private bool _editorFactoryNotify;
        private string _editorName;
        private Guid _linkedEditorGuid;
        private readonly string[] _extensions;
#if DEV11_OR_LATER
        private __VSPHYSICALVIEWATTRIBUTES _commonViewAttrs;
#endif

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.ProvideEditorExtensionAttribute"]' />
        /// <devdoc>
        ///     Creates a new attribute.
        /// </devdoc>
        public ProvideEditorExtension2Attribute(object factoryType, string extension, int priority, params string[] extensions) {
            // figure out what type of object they passed in and get the GUID from it
            if (factoryType is string)
                this._factory = new Guid((string)factoryType);
            else if (factoryType is Type)
                this._factory = ((Type)factoryType).GUID;
            else if (factoryType is Guid)
                this._factory = (Guid)factoryType;
            else
                throw new ArgumentException(string.Format(Babe.Lua.Properties.Resources.Culture, "invalid factory type", factoryType));

            _extension = extension;
            _priority = priority;
            _project = Guid.Empty;
            _templateDir = "";
            _resId = 0;
            _editorFactoryNotify = false;
            _extensions = extensions;
        }

#if DEV11_OR_LATER
        public ProvideEditorExtension2Attribute(object factoryType, string extension, int priority, __VSPHYSICALVIEWATTRIBUTES commonViewAttributes, params string[] extensions) :
            this(factoryType, extension, priority, extensions) {
            _commonViewAttrs = commonViewAttributes;
        }
#endif

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.Extension"]' />
        /// <devdoc>
        ///     The file extension of the file.
        /// </devdoc>
        public string Extension {
            get {
                return _extension;
            }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.Factory"]' />
        /// <devdoc>
        ///     The editor factory guid.
        /// </devdoc>
        public Guid Factory {
            get {
                return _factory;
            }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.Priority"]' />
        /// <devdoc>
        ///     The priority of this extension registration.
        /// </devdoc>
        public int Priority {
            get {
                return _priority;
            }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.ProjectGuid"]/*' />
        public string ProjectGuid {
            set { _project = new System.Guid(value); }
            get { return _project.ToString(); }
        }

        public string LinkedEditorGuid {
            get { return _linkedEditorGuid.ToString(); }
            set { _linkedEditorGuid = new System.Guid(value); }
        }

#if DEV11_OR_LATER
        public __VSPHYSICALVIEWATTRIBUTES CommonPhysicalViewAttributes {
            get {
                return _commonViewAttrs;
            }
        }
#endif

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.EditorFactoryNotify"]/*' />
        public bool EditorFactoryNotify {
            get { return this._editorFactoryNotify; }
            set { this._editorFactoryNotify = value; }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.TemplateDir"]/*' />
        public string TemplateDir {
            get { return _templateDir; }
            set { _templateDir = value; }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.NameResourceID"]/*' />
        public int NameResourceID {
            get { return _resId; }
            set { _resId = value; }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="ProvideEditorExtensionAttribute.DefaultName"]/*' />
        public string DefaultName {
            get { return _editorName; }
            set { _editorName = value; }
        }

        /// <summary>
        ///        The reg key name of this extension.
        /// </summary>
        private string RegKeyName {
            get {
                return string.Format(CultureInfo.InvariantCulture, "Editors\\{0}", Factory.ToString("B"));
            }
        }

        /// <summary>
        ///        The reg key name of the project.
        /// </summary>
        private string ProjectRegKeyName(RegistrationContext context) {
            return string.Format(CultureInfo.InvariantCulture,
                                 "Projects\\{0}\\AddItemTemplates\\TemplateDirs\\{1}",
                                 _project.ToString("B"),
                                 context.ComponentType.GUID.ToString("B"));
        }

        private string EditorFactoryNotifyKey {
            get {
                return string.Format(CultureInfo.InvariantCulture, "Projects\\{0}\\FileExtensions\\{1}",
                                     _project.ToString("B"),
                                     Extension);
            }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="Register"]' />
        /// <devdoc>
        ///     Called to register this attribute with the given context.  The context
        ///     contains the location where the registration inforomation should be placed.
        ///     it also contains such as the type being registered, and path information.
        ///
        ///     This method is called both for registration and unregistration.  The difference is
        ///     that unregistering just uses a hive that reverses the changes applied to it.
        /// </devdoc>
        public override void Register(RegistrationContext context) {
            using (Key editorKey = context.CreateKey(RegKeyName)) {
                if (!string.IsNullOrEmpty(DefaultName)) {
                    editorKey.SetValue(null, DefaultName);
                }
                if (0 != _resId)
                    editorKey.SetValue("DisplayName", "#" + _resId.ToString(CultureInfo.InvariantCulture));
                if (_linkedEditorGuid != Guid.Empty) {
                    editorKey.SetValue("LinkedEditorGuid", _linkedEditorGuid.ToString("B"));
                }
#if DEV11_OR_LATER
                if (_commonViewAttrs != 0) {
                    editorKey.SetValue("CommonPhysicalViewAttributes", (int)_commonViewAttrs);
                }
#endif
                editorKey.SetValue("Package", context.ComponentType.GUID.ToString("B"));
            }

            using (Key extensionKey = context.CreateKey(RegKeyName + "\\Extensions")) {
                extensionKey.SetValue(Extension.Substring(1), Priority);

                if (_extensions != null && _extensions.Length > 0) {
                    foreach (var extension in _extensions) {
                        var extensionAndPri = extension.Split(':');
                        int pri;
                        if (extensionAndPri.Length != 2 || !Int32.TryParse(extensionAndPri[1], out pri)) {
                            throw new InvalidOperationException("Expected extension:priority");
                        }

                        extensionKey.SetValue(extensionAndPri[0], pri);
                    }
                }
            }

            // Build the path of the registry key for the "Add file to project" entry
            if (_project != Guid.Empty) {
                string prjRegKey = ProjectRegKeyName(context) + "\\/1";
                using (Key projectKey = context.CreateKey(prjRegKey)) {
                    if (0 != _resId)
                        projectKey.SetValue("", "#" + _resId.ToString(CultureInfo.InvariantCulture));
                    if (_templateDir.Length != 0) {
                        Uri url = new Uri(context.ComponentType.Assembly.CodeBase);
                        string templates = url.LocalPath;
                        templates = CommonUtils.GetAbsoluteDirectoryPath(Path.GetDirectoryName(templates), _templateDir);
                        templates = context.EscapePath(templates);
                        projectKey.SetValue("TemplatesDir", templates);
                    }
                    projectKey.SetValue("SortPriority", Priority);
                }
            }

            // Register the EditorFactoryNotify
            if (EditorFactoryNotify) {
                // The IVsEditorFactoryNotify interface is called by the project system, so it doesn't make sense to
                // register it if there is no project associated to this editor.
                if (_project == Guid.Empty)
                    throw new ArgumentException("project");

                // Create the registry key
                using (Key edtFactoryNotifyKey = context.CreateKey(EditorFactoryNotifyKey)) {
                    edtFactoryNotifyKey.SetValue("EditorFactoryNotify", Factory.ToString("B"));
                }
            }
        }

        /// <include file='doc\ProvideEditorExtensionAttribute.uex' path='docs/doc[@for="Unregister"]' />
        /// <devdoc>
        /// Unregister this editor.
        /// </devdoc>
        /// <param name="context"></param>
        public override void Unregister(RegistrationContext context) {
            context.RemoveKey(RegKeyName);
            if (_project != Guid.Empty) {
                context.RemoveKey(ProjectRegKeyName(context));
                if (EditorFactoryNotify)
                    context.RemoveKey(EditorFactoryNotifyKey);
            }
        }
    }

}