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
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Windows.Forms;
//using Microsoft.LuaTools.Parsing;

namespace Microsoft.LuaTools.Debugger.Remote {

    /// <summary>
    /// Specifies how <see cref="LuaRemoteProcess.TryConnect"/> should handle SSL certificate errors.
    /// </summary>
    internal enum SslErrorHandling {
        /// <summary>
        /// Fail and return <see cref="ConnErrorMessages.RemoteSslError"/>.
        /// </summary>
        Fail,
        /// <summary>
        /// Ignore any errors and proceed with connection.
        /// </summary>
        Ignore,
        /// <summary>
        /// Prompt the user whether to ignore the error or to fail.
        /// </summary>
        PromptUser
    }

    internal class LuaRemoteProcess : LuaProcess {
        public const byte DebuggerProtocolVersion = 2;
        public const string DebuggerSignature = "PTVSDBG";
        public const string Accepted = "ACPT";
        public const string Rejected = "RJCT";
        public static readonly byte[] DebuggerSignatureBytes = Encoding.ASCII.GetBytes(DebuggerSignature);
        public static readonly byte[] InfoCommandBytes = Encoding.ASCII.GetBytes("INFO");
        public static readonly byte[] AttachCommandBytes = Encoding.ASCII.GetBytes("ATCH");
        public static readonly byte[] ReplCommandBytes = Encoding.ASCII.GetBytes("REPL");

        private readonly string _hostName;
        private readonly ushort _portNumber;
        private readonly string _secret;
        private readonly bool _useSsl;

        private LuaRemoteProcess(int pid, string hostName, ushort portNumber, string secret, bool useSsl/*, LuaLanguageVersion langVer*/)
            : base(pid/*, langVer*/) {
            _hostName = hostName;
            _portNumber = portNumber;
            _secret = secret;
            _useSsl = useSsl;
        }

        public string HostName {
            get { return _hostName; }
        }

        public ushort PortNumber {
            get { return _portNumber; }
        }

        public string Secret {
            get { return _secret; }
        }

        /// <summary>
        /// Performs the initial handshake with the remote debugging server, verifying signature and version number and setting up SSL,
        /// and returns the opened socket and the SSL stream for that socket.
        /// </summary>
        /// <param name="hostName">Name of the host to connect to.</param>
        /// <param name="portNumber">Port number to connect to.</param>
        /// <param name="secret">Secret to authenticate with.</param>
        /// <param name="useSsl">Whether to use SSL for this connection.</param>
        /// <param name="sslErrorHandling">If using SSL, specifies how SSL certificate errors should be handled.</param>
        /// <param name="socket">Opened socket to the remote debugging server. The returned socket is owned by <paramref name="stream"/>.</param>
        /// <param name="stream">Opened SSL network stream to the remote debugging server. This stream owns the <paramref name="socket"/>, and will automatically close it.</param>
        /// <returns>Error code.</returns>
        /// <remarks><paramref name="socket"/> should not be used to send or receive data, since it is wrapped in a stream, and is owned by that stream.
        /// It is exposed solely to enable querying it for endpoint information and connectivity status.</remarks>
/*
        public static ConnErrorMessages TryConnect(string hostName, ushort portNumber, string secret, bool useSsl, SslErrorHandling sslErrorHandling, out Socket socket, out Stream stream) {
            stream = null;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            bool connected = false;
            try {
                socket.Connect(hostName, portNumber);
                var rawStream = new NetworkStream(socket, ownsSocket: true);
                if (useSsl) {
                    var sslStream = new SslStream(rawStream, false, (sender, cert, chain, errs) => {
                        if (errs == SslPolicyErrors.None || sslErrorHandling == SslErrorHandling.Ignore) {
                            return true;
                        } else if (sslErrorHandling == SslErrorHandling.Fail) {
                            return false;
                        }

                        string errText = string.Format("Could not establish secure connection to {0}:{1} because of the following SSL issues:\n\n", hostName, portNumber);
                        if ((errs & SslPolicyErrors.RemoteCertificateNotAvailable) != 0) {
                            errText += "- no remote certificate provided\n";
                        }
                        if ((errs & SslPolicyErrors.RemoteCertificateNameMismatch) != 0) {
                            errText += "- remote certificate name does not match hostname\n";
                        }
                        if ((errs & SslPolicyErrors.RemoteCertificateChainErrors) != 0) {
                            errText += "- remote certificate is not trusted\n";
                        }

                        errText += "\nConnect anyway?";

                        var dlgRes = MessageBox.Show(errText, null, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        return dlgRes == DialogResult.Yes;
                    });
                    try {
                        sslStream.AuthenticateAsClient(hostName);
                    } catch (AuthenticationException) {
                        return ConnErrorMessages.RemoteSslError;
                    }
                    stream = sslStream;
                } else {
                    stream = rawStream;
                }

                string sig = stream.ReadAsciiString(DebuggerSignature.Length);
                if (sig != DebuggerSignature) {
                    return ConnErrorMessages.RemoteUnsupportedServer;
                }

                long ver = stream.ReadInt64();
                if (ver != DebuggerProtocolVersion) {
                    return ConnErrorMessages.RemoteUnsupportedServer;
                }

                stream.Write(DebuggerSignatureBytes);
                stream.WriteInt64(DebuggerProtocolVersion);

                stream.WriteString(secret);
                string secretResp = stream.ReadAsciiString(Accepted.Length);
                if (secretResp != Accepted) {
                    return ConnErrorMessages.RemoteSecretMismatch;
                }

                connected = true;
            } catch (IOException) {
                return ConnErrorMessages.RemoteNetworkError;
            } catch (SocketException) {
                return ConnErrorMessages.RemoteNetworkError;
            } finally {
                if (!connected) {
                    if (stream != null) {
                        stream.Close();
                    }
                    socket.Close();
                    socket = null;
                    stream = null;
                }
            }

            return ConnErrorMessages.None;
        }

        /// <summary>
        /// Same as the static version of this method, but uses the same <c>hostName</c>, <c>portNumber</c> and <c>secret</c> values
        /// that were originally used to create this instance of <see cref="LuaRemoteProcess"/>.
        /// </summary>
        public ConnErrorMessages TryConnect(SslErrorHandling sslErrorHandling, out Socket socket, out Stream stream) {
            return TryConnect(_hostName, _portNumber, _secret, _useSsl, sslErrorHandling, out socket, out stream);
        }

        public static ConnErrorMessages TryAttach(string hostName, ushort portNumber, string secret, bool useSsl, SslErrorHandling sslErrorHandling, out LuaProcess process) {
            process = null;

            Socket socket;
            Stream stream;
            ConnErrorMessages err = TryConnect(hostName, portNumber, secret, useSsl, sslErrorHandling, out socket, out stream);
            if (err == ConnErrorMessages.None) {
                bool attached = false;
                LuaLanguageVersion langVer;
                int pid;
                try {
                    stream.Write(AttachCommandBytes);

                    string attachResp = stream.ReadAsciiString(Accepted.Length);
                    if (attachResp != Accepted) {
                        return ConnErrorMessages.RemoteAttachRejected;
                    }

                    pid = stream.ReadInt32();
                    int langMajor = stream.ReadInt32();
                    int langMinor = stream.ReadInt32();
                    int langMicro = stream.ReadInt32();
                    langVer = (LuaLanguageVersion)((langMajor << 8) | langMinor);
                    if (!Enum.IsDefined(typeof(LuaLanguageVersion), langVer)) {
                        langVer = LuaLanguageVersion.None;
                    }

                    attached = true;
                } catch (IOException) {
                    return ConnErrorMessages.RemoteNetworkError;
                } finally {
                    if (!attached) {
                        if (stream != null) {
                            stream.Close();
                        }
                        socket.Close();
                        socket = null;
                        stream = null;
                    }
                }

                process = new LuaRemoteProcess(pid, hostName, portNumber, secret, useSsl, langVer);
                process.Connected(socket, stream);
            }

            return err;
        }
*/
    }
}

