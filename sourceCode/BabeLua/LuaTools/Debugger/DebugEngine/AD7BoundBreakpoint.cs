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
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.LuaTools.Debugger.DebugEngine {
    // This class represents a breakpoint that has been bound to a location in the debuggee. It is a child of the pending breakpoint
    // that creates it. Unless the pending breakpoint only has one bound breakpoint, each bound breakpoint is displayed as a child of the
    // pending breakpoint in the breakpoints window. Otherwise, only one is displayed.
    class AD7BoundBreakpoint : IDebugBoundBreakpoint2 {
        private readonly AD7PendingBreakpoint _pendingBreakpoint;
        private readonly AD7BreakpointResolution _breakpointResolution;
        private readonly AD7Engine _engine;
        private readonly LuaBreakpoint _breakpoint;

        private bool _enabled;
        private bool _deleted;

        public AD7BoundBreakpoint(AD7Engine engine, LuaBreakpoint address, AD7PendingBreakpoint pendingBreakpoint, AD7BreakpointResolution breakpointResolution, bool enabled) {
            _engine = engine;
            _breakpoint = address;
            _pendingBreakpoint = pendingBreakpoint;
            _breakpointResolution = breakpointResolution;
            _enabled = enabled;
            _deleted = false;
        }

        #region IDebugBoundBreakpoint2 Members

        // Called when the breakpoint is being deleted by the user.
        int IDebugBoundBreakpoint2.Delete() {
            AssertMainThread();

            if (!_deleted) {
                _deleted = true;
                _breakpoint.Remove();
                _pendingBreakpoint.OnBoundBreakpointDeleted(this);
                _engine.BreakpointManager.RemoveBoundBreakpoint(_breakpoint);
            }

            return VSConstants.S_OK;
        }

        [Conditional("DEBUG")]
        private static void AssertMainThread() {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
        }

        // Called by the debugger UI when the user is enabling or disabling a breakpoint.
        int IDebugBoundBreakpoint2.Enable(int fEnable) {
            AssertMainThread();

            bool enabled = fEnable == 0 ? false : true;
            if (_enabled != enabled) {
                if (!enabled) {
                    _breakpoint.Disable();
                } else {
                    _breakpoint.Add();
                }
            }
            _enabled = enabled;
            return VSConstants.S_OK;
        }

        // Return the breakpoint resolution which describes how the breakpoint bound in the debuggee.
        int IDebugBoundBreakpoint2.GetBreakpointResolution(out IDebugBreakpointResolution2 ppBPResolution) {
            ppBPResolution = _breakpointResolution;
            return VSConstants.S_OK;
        }

        // Return the pending breakpoint for this bound breakpoint.
        int IDebugBoundBreakpoint2.GetPendingBreakpoint(out IDebugPendingBreakpoint2 ppPendingBreakpoint) {
            ppPendingBreakpoint = _pendingBreakpoint;
            return VSConstants.S_OK;
        }

        // 
        int IDebugBoundBreakpoint2.GetState(enum_BP_STATE[] pState) {
            pState[0] = 0;

            if (_deleted) {
                pState[0] = enum_BP_STATE.BPS_DELETED;
            } else if (_enabled) {
                pState[0] = enum_BP_STATE.BPS_ENABLED;
            } else if (!_enabled) {
                pState[0] = enum_BP_STATE.BPS_DISABLED;
            }

            return VSConstants.S_OK;
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit and return it here.
        int IDebugBoundBreakpoint2.GetHitCount(out uint pdwHitCount) {
            pdwHitCount = 0;
            return VSConstants.E_NOTIMPL;
        }

        // The sample engine does not support conditions on breakpoints.
        // A real-world debugger will use this to specify when a breakpoint will be hit
        // and when it should be ignored.
        int IDebugBoundBreakpoint2.SetCondition(BP_CONDITION bpCondition) {
            _breakpoint.SetCondition(bpCondition.bstrCondition, bpCondition.styleCondition == enum_BP_COND_STYLE.BP_COND_WHEN_CHANGED ? true : false);
            return VSConstants.S_OK;
        }

        // The sample engine does not support hit counts on breakpoints. A real-world debugger will want to keep track 
        // of how many times a particular bound breakpoint has been hit. The debugger calls SetHitCount when the user 
        // resets a breakpoint's hit count.
        int IDebugBoundBreakpoint2.SetHitCount(uint dwHitCount) {
            throw new NotImplementedException();
        }

        // The sample engine does not support pass counts on breakpoints.
        // This is used to specify the breakpoint hit count condition.
        int IDebugBoundBreakpoint2.SetPassCount(BP_PASSCOUNT bpPassCount) {
            throw new NotImplementedException();
        }

        #endregion
    }
}
