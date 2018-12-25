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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace Microsoft.LuaTools.Debugger.DebugEngine {
    // And implementation of IDebugCodeContext2 and IDebugMemoryContext2. 
    // IDebugMemoryContext2 represents a position in the address space of the machine running the program being debugged.
    // IDebugCodeContext2 represents the starting position of a code instruction. 
    // For most run-time architectures today, a code context can be thought of as an address in a program's execution stream.
    class AD7MemoryAddress : IDebugCodeContext2, IDebugCodeContext100 {
        private readonly AD7Engine _engine;
        private readonly uint _lineNo;
        private readonly string _filename;
        private readonly LuaStackFrame _frame;
        private IDebugDocumentContext2 _documentContext;


        public AD7MemoryAddress(AD7Engine engine, string filename, uint lineno) {
            _engine = engine;
            _lineNo = (uint)lineno;
            _filename = filename;
        }

        public AD7MemoryAddress(AD7Engine engine, string filename, uint lineno, LuaStackFrame frame)
            : this(engine, filename, lineno) {
            _frame = frame;
        }

        public void SetDocumentContext(IDebugDocumentContext2 docContext) {
            _documentContext = docContext;
        }

        #region IDebugMemoryContext2 Members

        // Adds a specified value to the current context's address to create a new context.
        public int Add(ulong dwCount, out IDebugMemoryContext2 newAddress) {
            newAddress = new AD7MemoryAddress(_engine, _filename, (uint)dwCount + _lineNo);
            return VSConstants.S_OK;
        }

        // Compares the memory context to each context in the given array in the manner indicated by compare flags, 
        // returning an index of the first context that matches.
        public int Compare(enum_CONTEXT_COMPARE uContextCompare, IDebugMemoryContext2[] compareToItems, uint compareToLength, out uint foundIndex) {
            foundIndex = uint.MaxValue;

            enum_CONTEXT_COMPARE contextCompare = (enum_CONTEXT_COMPARE)uContextCompare;

            for (uint c = 0; c < compareToLength; c++) {
                AD7MemoryAddress compareTo = compareToItems[c] as AD7MemoryAddress;
                if (compareTo == null) {
                    continue;
                }

                if (!AD7Engine.ReferenceEquals(this._engine, compareTo._engine)) {
                    continue;
                }

                bool result;

                switch (contextCompare) {
                    case enum_CONTEXT_COMPARE.CONTEXT_EQUAL:
                        result = (this._lineNo == compareTo._lineNo);
                        break;

                    case enum_CONTEXT_COMPARE.CONTEXT_LESS_THAN:
                        result = (this._lineNo < compareTo._lineNo);
                        break;

                    case enum_CONTEXT_COMPARE.CONTEXT_GREATER_THAN:
                        result = (this._lineNo > compareTo._lineNo);
                        break;

                    case enum_CONTEXT_COMPARE.CONTEXT_LESS_THAN_OR_EQUAL:
                        result = (this._lineNo <= compareTo._lineNo);
                        break;

                    case enum_CONTEXT_COMPARE.CONTEXT_GREATER_THAN_OR_EQUAL:
                        result = (this._lineNo >= compareTo._lineNo);
                        break;

                    case enum_CONTEXT_COMPARE.CONTEXT_SAME_SCOPE:
                    case enum_CONTEXT_COMPARE.CONTEXT_SAME_FUNCTION:
                        if (_frame != null) {
                            result = compareTo._filename == _filename && (compareTo._lineNo + 1) >= _frame.StartLine && (compareTo._lineNo + 1) <= _frame.EndLine;
                        } else if (compareTo._frame != null) {
                            result = compareTo._filename == _filename && (_lineNo + 1) >= compareTo._frame.StartLine && (compareTo._lineNo + 1) <= compareTo._frame.EndLine;
                        } else {
                            result = this._lineNo == compareTo._lineNo && this._filename == compareTo._filename;
                        }
                        break;

                    case enum_CONTEXT_COMPARE.CONTEXT_SAME_MODULE:
                        result = _filename == compareTo._filename;
                        break;

                    case enum_CONTEXT_COMPARE.CONTEXT_SAME_PROCESS:
                        result = true;
                        break;

                    default:
                        // A new comparison was invented that we don't support
                        return VSConstants.E_NOTIMPL;
                }

                if (result) {
                    foundIndex = c;
                    return VSConstants.S_OK;
                }
            }

            return VSConstants.S_FALSE;
        }

        public uint LineNumber {
            get {
                return _lineNo;
            }
        }

        // Gets information that describes this context.
        public int GetInfo(enum_CONTEXT_INFO_FIELDS dwFields, CONTEXT_INFO[] pinfo) {
            pinfo[0].dwFields = 0;

            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS) != 0) {
                pinfo[0].bstrAddress = _lineNo.ToString();
                pinfo[0].dwFields |= enum_CONTEXT_INFO_FIELDS.CIF_ADDRESS;
            }

            // Fields not supported by the sample
            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSOFFSET) != 0) { }
            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_ADDRESSABSOLUTE) != 0) { }
            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_MODULEURL) != 0) { }
            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_FUNCTION) != 0) { }
            if ((dwFields & enum_CONTEXT_INFO_FIELDS.CIF_FUNCTIONOFFSET) != 0) { }

            return VSConstants.S_OK;
        }

        // Gets the user-displayable name for this context
        // This is not supported by the sample engine.
        public int GetName(out string pbstrName) {
            throw new Exception("The method or operation is not implemented.");
        }

        // Subtracts a specified value from the current context's address to create a new context.
        public int Subtract(ulong dwCount, out IDebugMemoryContext2 ppMemCxt) {
            ppMemCxt = new AD7MemoryAddress(_engine, _filename, (uint)dwCount - _lineNo);
            return VSConstants.S_OK;
        }

        #endregion

        #region IDebugCodeContext2 Members

        // Gets the document context for this code-context
        public int GetDocumentContext(out IDebugDocumentContext2 ppSrcCxt) {
            ppSrcCxt = _documentContext;
            return VSConstants.S_OK;
        }

        // Gets the language information for this code context.
        public int GetLanguageInfo(ref string pbstrLanguage, ref Guid pguidLanguage) {
            if (_documentContext != null) {
                _documentContext.GetLanguageInfo(ref pbstrLanguage, ref pguidLanguage);
                return VSConstants.S_OK;
            } else {
                return VSConstants.S_FALSE;
            }
        }

        #endregion

        #region IDebugCodeContext100 Members

        // Returns the program being debugged. In the case of this sample
        // debug engine, AD7Engine implements IDebugProgram2 which represents
        // the program being debugged.
        int IDebugCodeContext100.GetProgram(out IDebugProgram2 pProgram) {
            pProgram = _engine;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
