/*
 * SocketOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    [ObjectId("71b14766-48a0-45d5-9254-640fde03509d")]
    internal static class SocketOps
    {
        #region Private Static Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static PropertyInfo networkStreamSocket;
        private static PropertyInfo tcpListenerActive;
        private static PropertyInfo socketCleanedUp;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Diagnostic Methods
        public static ReturnCode Ping(
            string hostNameOrAddress,
            int timeout,
            ref IPStatus status,
            ref long roundtripTime,
            ref Result error
            )
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(hostNameOrAddress, timeout);

                    //
                    // NOTE: Populate reply information for the caller.
                    //
                    status = reply.Status;
                    roundtripTime = reply.RoundtripTime;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                //
                // NOTE: Populate error information for the caller.
                //
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Network Address Methods
        public static IPAddress GetIpAddress(
            string hostNameOrAddress,
            bool strict,
            ref Result error
            )
        {
            IPAddress result = null;

            if (!String.IsNullOrEmpty(hostNameOrAddress))
            {
                if (!IPAddress.TryParse(hostNameOrAddress, out result))
                {
                    try
                    {
                        //
                        // NOTE: Attempt to resolve the host name to one or
                        //       more IP addresses. This is required even for
                        //       things like "localhost", etc.
                        //
                        IPAddress[] addresses = Dns.GetHostAddresses(hostNameOrAddress);

                        if (addresses != null)
                        {
                            //
                            // NOTE: We require an IPv4 address.
                            //
                            for (int index = 0; index < addresses.Length; index++)
                            {
                                if (addresses[index].AddressFamily == AddressFamily.InterNetwork)
                                {
                                    result = addresses[index];
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
            }
            else
            {
                if (strict)
                    error = "invalid host name or address";
                else
                    result = IPAddress.Any;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetPortNumber(
            string port,
            CultureInfo cultureInfo,
            bool strict,
            ref Result error
            )
        {
            int result = Port.Invalid;

            if (!String.IsNullOrEmpty(port))
            {
                if (Value.GetInteger2(
                        port, ValueFlags.AnyInteger, cultureInfo,
                        ref result, ref error) == ReturnCode.Ok)
                {
                    return result;
                }

                //
                // FIXME: Else we should try to lookup the service name using
                //        getservbyname() API; however, the .NET Framework does
                //        not expose this functionality.  A possible workaround
                //        would be to see if the necessary DLL is already loaded
                //        (e.g. WS2_32.DLL) and simply P/Invoke the function
                //        ourselves.
                //
            }
            else if (!strict)
            {
                result = Port.Automatic;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Network Client Methods
        public static Socket GetSocket(
            NetworkStream networkStream
            )
        {
            try
            {
                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked as "protected";
                    //       however, we need to know this information and we do not want
                    //       to derive a custom class to get it; therefore, just use
                    //       Reflection.  We cache the PropertyInfo object so that we do
                    //       not need to look it up more than once.
                    //
                    if (networkStreamSocket == null)
                    {
                        networkStreamSocket = typeof(NetworkStream).GetProperty(
                            "Socket", MarshalOps.PrivateInstanceGetFieldBindingFlags);
                    }

                    if ((networkStreamSocket != null) && (networkStream != null))
                        return networkStreamSocket.GetValue(networkStream, null) as Socket;
                }
                #endregion
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static TcpClient NewTcpClient(
            string localAddress,
            string localPort,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            IPAddress address = GetIpAddress(localAddress, false, ref error);

            if (address != null)
            {
                int port = GetPortNumber(
                    localPort, cultureInfo, false, ref error);

                if (port != Port.Invalid)
                    return new TcpClient(new IPEndPoint(address, (int)port));
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Connect(
            TcpClient client,
            string remoteHost,
            string remotePort,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            IPAddress address = GetIpAddress(remoteHost, true, ref error);

            if (address != null)
            {
                int port = GetPortNumber(
                    remotePort, cultureInfo, true, ref error);

                if (port != Port.Invalid)
                {
                    try
                    {
                        client.Connect(new IPEndPoint(address, (int)port));

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Network Server Methods
        public static ReturnCode GetServerScript(
            TcpClient client,
            string channelId,
            string oldScript,
            ref string newScript,
            ref Result error
            )
        {
            try
            {
                if (client != null)
                {
                    Socket socket = client.Client;

                    if (socket != null)
                    {
                        IPEndPoint endPoint = socket.RemoteEndPoint as IPEndPoint;

                        if (endPoint != null)
                        {
                            StringBuilder builder = StringOps.NewStringBuilder(oldScript);

                            builder.Append(Characters.Space);
                            builder.Append(channelId);
                            builder.Append(Characters.Space);
                            builder.Append(endPoint.Address);
                            builder.Append(Characters.Space);
                            builder.Append(endPoint.Port);

                            newScript = builder.ToString();

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "invalid remote endpoint";
                        }
                    }
                    else
                    {
                        error = "invalid client socket";
                    }
                }
                else
                {
                    error = "invalid client";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsListenerActive(
            TcpListener listener,
            bool @default
            )
        {
            try
            {
                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked as "protected";
                    //       however, we need to know this information and we do not want
                    //       to derive a custom class to get it; therefore, just use
                    //       Reflection.  We cache the PropertyInfo object so that we do
                    //       not need to look it up more than once.
                    //
                    if (tcpListenerActive == null)
                    {
                        tcpListenerActive = typeof(TcpListener).GetProperty(
                            "Active", MarshalOps.PrivateInstanceGetFieldBindingFlags);
                    }

                    if ((tcpListenerActive != null) && (listener != null))
                        return (bool)tcpListenerActive.GetValue(listener, null);
                }
                #endregion
            }
            catch
            {
                // do nothing.
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsCleanedUp(
            Socket socket,
            bool @default
            )
        {
            try
            {
                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked as "internal";
                    //       however, we need to know this information.  Therefore, just
                    //       use Reflection.  We cache the PropertyInfo object so that
                    //       we do not need to look it up more than once.
                    //
                    if (socketCleanedUp == null)
                    {
                        socketCleanedUp = typeof(Socket).GetProperty(
                            "CleanedUp", MarshalOps.PrivateInstanceGetFieldBindingFlags);
                    }

                    if ((socketCleanedUp != null) && (socket != null))
                        return (bool)socketCleanedUp.GetValue(socket, null);
                }
                #endregion
            }
            catch
            {
                // do nothing.
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static TcpListener NewTcpListener(
            string localAddress,
            string localPort,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            try
            {
                IPAddress address = null;

                if (localAddress != null)
                    address = GetIpAddress(localAddress, true, ref error);

                if ((localAddress == null) || (address != null))
                {
                    int port = GetPortNumber(
                        localPort, cultureInfo, true, ref error);

                    if (port != Port.Invalid)
                    {
                        return (address != null) ?
                            new TcpListener(address, port) :
                            new TcpListener(port);
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
        #endregion
    }
}
