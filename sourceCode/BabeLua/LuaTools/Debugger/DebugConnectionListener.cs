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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.LuaTools.Debugger {
    /// <summary>
    /// Handles connections from all debuggers.
    /// </summary>
    static class DebugConnectionListener {
        private static int _listenerPort = -1;
        private static readonly Dictionary<Guid, WeakReference> _targets = new Dictionary<Guid, WeakReference>();

        public static void RegisterProcess(Guid id, LuaProcess process) {
            lock (_targets) {
                EnsureListenerSocket();

                _targets[id] = new WeakReference(process);
            }
        }

        public static int ListenerPort {
            get {
                lock (_targets) {
                    EnsureListenerSocket();
                }

                return _listenerPort;
            }
        }

        private static void EnsureListenerSocket() {
            if (_listenerPort < 0) {
                var socketSource = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                socketSource.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                socketSource.Listen(0);
                _listenerPort = ((IPEndPoint)socketSource.LocalEndPoint).Port;
                Debug.WriteLine("Listening for debug connections on port {0}", _listenerPort);
                socketSource.BeginAccept(AcceptConnection, socketSource);
            }
        }

        public static void UnregisterProcess(Guid id) {
            lock (_targets) {
                _targets.Remove(id);
            }
        }

        public static LuaProcess GetProcess(Guid id) {
            WeakReference value;
            lock (_targets) {
                if (_targets.TryGetValue(id, out value)) {
                    return (LuaProcess)value.Target;
                }
            }
            return null;
        }

        private static void AcceptConnection(IAsyncResult iar) {
            Socket socket;
            var socketSource = ((Socket)iar.AsyncState);
            try {
                socket = socketSource.EndAccept(iar);
            } catch (SocketException ex) {
                Debug.WriteLine("DebugConnectionListener socket failed");
                Debug.WriteLine(ex);
                return;
            } catch (ObjectDisposedException) {
                Debug.WriteLine("DebugConnectionListener socket closed");
                return;
            }
/*
            var stream = new NetworkStream(socket, ownsSocket: false);
            try {
                socket.Blocking = true;
                string debugId = stream.ReadString();
                var result = (ConnErrorMessages)stream.ReadInt32();

                lock (_targets) {
                    Guid debugGuid;
                    WeakReference weakProcess;
                    LuaProcess targetProcess;

                    if (Guid.TryParse(debugId, out debugGuid) &&
                        _targets.TryGetValue(debugGuid, out weakProcess) &&
                        (targetProcess = weakProcess.Target as LuaProcess) != null) {

                        if (result == ConnErrorMessages.None) {
                            targetProcess.Connected(socket, stream);
                            stream = null;
                            socket = null;
                        } else {
                            WriteErrorToOutputWindow(result, targetProcess);
                            targetProcess.Unregister();
                        }
                    } else {
                        Debug.WriteLine("Unknown debug target: {0}", debugId);
                    }
                }
            } catch (IOException) {
            } catch (SocketException) {
            } finally {
                if (stream != null) {
                    stream.Dispose();
                }
                if (socket != null) {
                    socket.Dispose();
                }
            }

            socketSource.BeginAccept(AcceptConnection, socketSource);
*/
        }
/*
        private static void WriteErrorToOutputWindow(ConnErrorMessages result, LuaProcess targetProcess) {
            var outWin = (IVsOutputWindow)Package.GetGlobalService(typeof(IVsOutputWindow));

            IVsOutputWindowPane pane;
            if (outWin != null && ErrorHandler.Succeeded(outWin.GetPane(VSConstants.GUID_OutWindowDebugPane, out pane))) {
                pane.Activate();
                string moduleName;
                try {
                    moduleName = Process.GetProcessById(targetProcess.Id).MainModule.ModuleName;
                } catch {
                    // either the process is no longer around, or it's a 64-bit process
                    // and we can't get the EXE name.
                    moduleName = null;
                }

                if (moduleName != null) {
                    pane.OutputString(String.Format("Failed to connect to process {0} ({1}): {2}",
                        targetProcess.Id,
                        moduleName,
                        result.GetErrorMessage())
                    );
                } else {
                    pane.OutputString(String.Format("Failed to connect to process {0}: {1}",
                        targetProcess.Id,
                        result.GetErrorMessage())
                    );
                }
            }
        }
*/
    }
}
