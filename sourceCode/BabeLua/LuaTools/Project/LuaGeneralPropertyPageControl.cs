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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
//using Microsoft.LuaTools.Interpreter;
//using Microsoft.LuaTools.Options;
using Microsoft.VisualStudio.ComponentModelHost;

namespace Microsoft.LuaTools.Project
{
    public partial class LuaGeneralPropertyPageControl : UserControl {
/*
        static readonly ILuaInterpreterFactory Separator =
            new InterpreterPlaceholder(Guid.Empty, " -- Other installed interpreters --");
        static readonly ILuaInterpreterFactory GlobalDefault =
            new InterpreterPlaceholder(Guid.Empty, "(Use global default)");

        private readonly IInterpreterOptionsService _service;
*/
        private readonly LuaGeneralPropertyPage _propPage;

        internal LuaGeneralPropertyPageControl(LuaGeneralPropertyPage newLuaGeneralPropertyPage)
        {
            InitializeComponent();

            _propPage = newLuaGeneralPropertyPage;
//            _service = LuaToolsPackage.ComponentModel.GetService<IInterpreterOptionsService>();
        }

        private void InitializeInterpreters() {
            _defaultInterpreter.BeginUpdate();
/*            
            try {
                var selection = _defaultInterpreter.SelectedItem;
                _defaultInterpreter.Items.Clear();
                var provider = _propPage.LuaProject != null ? _propPage.LuaProject.Interpreters : null;
                var available = provider != null ? provider.GetInterpreterFactories().ToArray() : null;
                if (available != null && available.Length > 0) {
                    var others = _service.Interpreters.ToList();

                    foreach (var interpreter in available) {
                        _defaultInterpreter.Items.Add(interpreter);
                        others.Remove(interpreter);
                    }

                    if (others.Count > 0) {
                        _defaultInterpreter.Items.Add(Separator);
                        foreach (var interpreter in others) {
                            _defaultInterpreter.Items.Add(interpreter);
                        }
                    }
                } else {
                    _defaultInterpreter.Items.Add(GlobalDefault);

                    foreach (var interpreter in _service.Interpreters) {
                        _defaultInterpreter.Items.Add(interpreter);
                    }
                }

                if (provider == null || provider.IsActiveInterpreterGlobalDefault) {
                    // ActiveInterpreter will never be null, so we need to check
                    // the property to find out if it's following the global
                    // default.
                    SetDefaultInterpreter(null);
                } else if (selection != null) {
                    SetDefaultInterpreter((ILuaInterpreterFactory)selection);
                } else { 
                    SetDefaultInterpreter(provider.ActiveInterpreter);
                }
            } finally {
                _defaultInterpreter.EndUpdate();
            }
*/
        }

        internal void OnInterpretersChanged() {
            _defaultInterpreter.SelectedIndexChanged -= Changed;
            
            InitializeInterpreters();

            _defaultInterpreter.SelectedIndexChanged += Changed;
        }

        public string StartupFile {
            get { return _startupFile.Text; }
            set { _startupFile.Text = value; }
        }

        public string WorkingDirectory {
            get { return _workingDirectory.Text; }
            set { _workingDirectory.Text = value; }
        }

        public bool IsWindowsApplication {
            get { return _windowsApplication.Checked; }
            set { _windowsApplication.Checked = value; }
        }
/*
        public ILuaInterpreterFactory DefaultInterpreter
        {
            get {
                if (_defaultInterpreter.SelectedItem == GlobalDefault) {
                    return null;
                }
                return _defaultInterpreter.SelectedItem as ILuaInterpreterFactory;
            }
        }

        public void SetDefaultInterpreter(ILuaInterpreterFactory interpreter)
        {
            if (interpreter == null) {
                _defaultInterpreter.SelectedIndex = 0;
            } else {
                try {
                    _defaultInterpreter.SelectedItem = interpreter;
                } catch (IndexOutOfRangeException) {
                    _defaultInterpreter.SelectedIndex = 0;
                }
            }
        }
*/
        private void Changed(object sender, EventArgs e) {
/*
            if (_defaultInterpreter.SelectedItem == Separator) {
                _defaultInterpreter.SelectedItem = _propPage.LuaProject.Interpreters.ActiveInterpreter;
            }
*/
            _propPage.IsDirty = true;
        }

        private void Interpreter_Format(object sender, ListControlConvertEventArgs e) {
/*
            var factory = e.ListItem as ILuaInterpreterFactory;
            if (factory != null) {
                e.Value = factory.Description;
            } else {
                e.Value = e.ListItem.ToString();
            }
*/
        }
    }
}
