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
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudioTools.Project;

namespace Microsoft.LuaTools.Project {
    [Export(typeof(ILuaLauncherProvider))]
    class DefaultLauncherProvider : ILuaLauncherProvider {
        internal const string DefaultLauncherDescription = "Standard Lua launcher";

        public ILuaLauncherOptions GetLauncherOptions(ILuaProject properties) {
            return new DefaultLuaLauncherOptions(properties);
        }

        public string Name {
            get {
                return DefaultLauncherDescription;
            }
        }

        public string Description {
            get {
                return "Launches and debugs Lua programs.  This is the default.";
            }
        }

        public IProjectLauncher CreateLauncher(ILuaProject project) {
            return new DefaultLuaLauncher(project);
        }
    }
}
