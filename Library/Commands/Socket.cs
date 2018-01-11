/*
 * Socket.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Net.Sockets;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("2cb67080-894d-4232-a2d9-ae2a65da012e")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("network")]
    internal sealed class Socket : Core
    {
        public Socket(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 3)
                    {
                        if (interpreter.HasChannels(ref result))
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] { 
                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-server", null), // server only
                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-buffer", null),  // client & server
                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-timeout", null), // client & server
                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-sendtimeout", null), // client & server
                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-receivetimeout", null), // client & server
                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-myaddr", null), // client & server
                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-myport", null), // client only
                                new Option(null, OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-async", null), // client only
                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-channelid", null), // client & server
                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nodelay", null), // client only
                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noexclusive", null), // server only
                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                            });

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, true, ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: There must be at least one argument after the options 
                                //       and there can never be more than two.
                                //
                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) >= arguments.Count))
                                {
                                    Variant value = null;
                                    string command = null;

                                    if (options.IsPresent("-server", ref value))
                                        command = value.ToString();

                                    string myAddress = null;

                                    if (options.IsPresent("-myaddr", ref value))
                                        myAddress = value.ToString();

                                    string myPort = null;

                                    if (options.IsPresent("-myport", ref value))
                                        myPort = value.ToString();

                                    int buffer = 0;

                                    if (options.IsPresent("-buffer", ref value))
                                        buffer = (int)value.Value;

                                    int sendTimeout = _Timeout.None;

                                    if (options.IsPresent("-sendtimeout", ref value))
                                        sendTimeout = (int)value.Value;

                                    int receiveTimeout = _Timeout.None;

                                    if (options.IsPresent("-receivetimeout", ref value))
                                        receiveTimeout = (int)value.Value;

                                    int timeout = _Timeout.None;

                                    if (options.IsPresent("-timeout", ref value))
                                        timeout = (int)value.Value;

                                    bool asynchronous = false;

                                    if (options.IsPresent("-async"))
                                        asynchronous = true; /* NOT YET IMPLEMENTED */

                                    bool noDelay = false;

                                    if (options.IsPresent("-nodelay"))
                                        noDelay = true;

                                    bool exclusive = true; /* TODO: Good default? */

                                    if (options.IsPresent("-noexclusive"))
                                        exclusive = false;

                                    string channelId = null;

                                    if (options.IsPresent("-channelid", ref value))
                                        channelId = value.ToString();

                                    if ((channelId == null) ||
                                        (interpreter.DoesChannelExist(channelId) != ReturnCode.Ok))
                                    {
                                        if (command != null)
                                        {
                                            if ((argumentIndex + 1) == arguments.Count)
                                            {
                                                if (myPort == null)
                                                    code = interpreter.StartServerSocket(
                                                        options, timeout, myAddress,
                                                        arguments[argumentIndex], exclusive,
                                                        command, ref result);
                                                else
                                                    goto wrongNumArgs;
                                            }
                                            else
                                            {
                                                goto wrongNumArgs;
                                            }
                                        }
                                        else
                                        {
                                            if ((argumentIndex + 2) == arguments.Count)
                                            {
                                                if (!asynchronous)
                                                {
                                                    TcpClient client = SocketOps.NewTcpClient(myAddress, myPort,
                                                        interpreter.CultureInfo, ref result);

                                                    if (client != null)
                                                    {
                                                        try
                                                        {
                                                            client.NoDelay = noDelay;

                                                            if (timeout != _Timeout.None)
                                                            {
                                                                client.SendTimeout = timeout;
                                                                client.ReceiveTimeout = timeout;
                                                            }

                                                            if (sendTimeout != _Timeout.None)
                                                                client.SendTimeout = sendTimeout;

                                                            if (receiveTimeout != _Timeout.None)
                                                                client.ReceiveTimeout = receiveTimeout;

                                                            if (buffer != 0)
                                                            {
                                                                client.SendBufferSize = buffer;
                                                                client.ReceiveBufferSize = buffer;
                                                            }

                                                            code = ReturnCode.Ok;
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                            result = e;
                                                            code = ReturnCode.Error;
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = SocketOps.Connect(client, arguments[argumentIndex],
                                                                arguments[argumentIndex + 1], interpreter.CultureInfo,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                StreamFlags flags = StreamFlags.PreventClose |
                                                                    StreamFlags.Socket | StreamFlags.Client;

                                                                if (channelId == null)
                                                                    channelId = FormatOps.Id("clientSocket", null, interpreter.NextId());

                                                                code = interpreter.AddFileOrSocketChannel(
                                                                    channelId, client.GetStream(), options, flags, false,
                                                                    false, new ClientData(client), ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = channelId;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "asynchronous sockets are not implemented";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                goto wrongNumArgs;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "can't add \"{0}\": channel already exists",
                                            channelId);

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                    {
                                        result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                        code = ReturnCode.Error;
                                    }
                                    else
                                    {
                                        goto wrongNumArgs;
                                    }
                                }
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        goto wrongNumArgs;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;

        wrongNumArgs:
            result = "wrong # args: should be \"socket ?-myaddr addr? ?-myport myport? ?-async? host port\" " +
                "or \"socket -server command ?-myaddr addr? port\"";

            return ReturnCode.Error;
        }
        #endregion
    }
}