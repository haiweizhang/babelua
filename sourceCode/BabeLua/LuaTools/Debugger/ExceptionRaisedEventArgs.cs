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

namespace Microsoft.LuaTools.Debugger {
    class ExceptionRaisedEventArgs : EventArgs {
        private readonly LuaException _exception;
        private readonly LuaThread _thread;
        private readonly bool _isUnhandled;

        public ExceptionRaisedEventArgs(LuaThread thread, LuaException exception, bool isUnhandled) {
            _thread = thread;
            _exception = exception;
            _isUnhandled = isUnhandled;
        }

        public LuaException Exception {
            get {
                return _exception;
            }
        }

        public LuaThread Thread {
            get {
                return _thread;
            }
        }

        public bool IsUnhandled {
            get {
                return _isUnhandled;
            }
        }
    }
}
