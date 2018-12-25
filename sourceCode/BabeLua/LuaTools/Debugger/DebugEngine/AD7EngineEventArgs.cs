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

namespace Microsoft.LuaTools.Debugger.DebugEngine {
    /// <summary>
    /// Event args for start/stop of engines.
    /// </summary>
    class AD7EngineEventArgs : EventArgs {
        private readonly AD7Engine _engine;

        public AD7EngineEventArgs(AD7Engine engine) {
            _engine = engine;
        }

        public AD7Engine Engine {
            get {
                return _engine;
            }
        }
    }
}
