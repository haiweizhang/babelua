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

using System.Collections.Generic;

namespace Microsoft.LuaTools.Debugger {
    class LuaThread {
        private readonly long _identity;
        private readonly LuaProcess _process;
        private readonly bool _isWorkerThread;
        private string _name;
        private IList<LuaStackFrame> _frames;

        internal LuaThread(LuaProcess process, long identity, bool isWorkerThread) {
            _process = process;
            _identity = identity;
            _isWorkerThread = isWorkerThread;
            _name = "";
        }

        public void StepInto() {
            _process.SendStepInto(_identity);
        }

        public void StepOver() {
            _process.SendStepOver(_identity);
        }

        public void StepOut() {
            _process.SendStepOut(_identity);
        }

        public void Resume() {
            _process.SendResumeThread(_identity);
        }

        public void AutoResume() {
            _process.AutoResumeThread(_identity);
        }

        public bool IsWorkerThread {
            get {
                return _isWorkerThread;
            }
        }

        internal void ClearSteppingState() {
            _process.SendClearStepping(_identity);
        }

        public IList<LuaStackFrame> Frames {
            get {
                return _frames;
            }
            set {
                _frames = value;
            }
        }

        public LuaProcess Process {
            get {
                return _process;
            }
        }

        public string Name {
            get {
                return _name;
            }
            set {
                _name = value;
            }
        }

        internal long Id {
            get {
                return _identity;
            }
        }
    }
}
