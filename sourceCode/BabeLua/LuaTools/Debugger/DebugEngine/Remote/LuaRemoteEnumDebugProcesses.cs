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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.IO;
using System.Net.Security;
using System.Text.RegularExpressions;

namespace Microsoft.LuaTools.Debugger.Remote {
    internal class LuaRemoteEnumDebugProcesses : LuaRemoteEnumDebug<IDebugProcess2>, IEnumDebugProcesses2 {
        private readonly LuaRemoteDebugPort _port;

        private static LuaRemoteDebugProcess Connect(LuaRemoteDebugPort port) {
            LuaRemoteDebugProcess process = null;
/*
            // Connect to the remote debugging server and obtain process information. If any errors occur, display an error dialog, and keep
            // trying for as long as user clicks "Retry".
            while (true) {
                Socket socket;
                Stream stream;
                // Process information is not sensitive, so ignore any SSL certificate errors, rather than bugging the user with warning dialogs.
                var err = LuaRemoteProcess.TryConnect(port.HostName, port.PortNumber, port.Secret, port.UseSsl, SslErrorHandling.Ignore, out socket, out stream);
                using (stream) {
                    if (err == ConnErrorMessages.None) {
                        try {
                            stream.Write(LuaRemoteProcess.InfoCommandBytes);
                            int pid = stream.ReadInt32();
                            string exe = stream.ReadString();
                            string username = stream.ReadString();
                            string version = stream.ReadString();
                            process = new LuaRemoteDebugProcess(port, pid, exe, username, version);
                            break;
                        } catch (IOException) {
                            err = ConnErrorMessages.RemoteNetworkError;
                        }
                    }

                    if (err != ConnErrorMessages.None) {
                        string errText;
                        switch (err) {
                            case ConnErrorMessages.RemoteUnsupportedServer:
                                errText = string.Format("Remote server at '{0}:{1}' is not a Lua Tools for Visual Studio remote debugging server, or its version is not supported.", port.HostName, port.PortNumber);
                                break;
                            case ConnErrorMessages.RemoteSecretMismatch:
                                errText = string.Format("Secret '{0}' did not match the server secret at '{1}:{2}'. Make sure that the secret is specified correctly in the Qualifier textbox, e.g. 'secret@localhost:5678'.", port.Secret, port.HostName, port.PortNumber);
                                break;
                            case ConnErrorMessages.RemoteSslError:
                                // User has already got a warning dialog and clicked "Cancel" on that, so no further prompts are needed.
                                return null;
                            default:
                                errText = string.Format("Could not connect to remote Lua process at '{0}:{1}'. Make sure that the process is running, and has called ptvsd.enable_attach()", port.HostName, port.PortNumber);
                                break;
                        }

                        DialogResult dlgRes = MessageBox.Show(errText, null, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                        if (dlgRes != DialogResult.Retry) {
                            break;
                        }
                    }
                }
            }

*/
            return process;
        }

        public LuaRemoteEnumDebugProcesses(LuaRemoteDebugPort port)
            : base(Connect(port)) {
            this._port = port;
        }

        public int Clone(out IEnumDebugProcesses2 ppEnum) {
            ppEnum = new LuaRemoteEnumDebugProcesses(_port);
            return 0;
        }
    }
}
