/*
 * Gets.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("bfc8553d-5fb7-4f5c-9eba-4957473258ef")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Gets : Core
    {        
        public Gets(
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
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if ((arguments.Count == 2) || (arguments.Count == 3))
                    {
                        string channelId = arguments[1];
                        Channel channel = interpreter.GetChannel(channelId, ref result);

                        if (channel != null)
                        {
                            Encoding encoding = null;

                            if (interpreter.GetChannelEncoding(channel, ref encoding) == ReturnCode.Ok)
                            {
                                try
                                {
                                    ByteList buffer = null;

                                    code = channel.Read(ref buffer, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        string stringValue = null;

                                        code = StringOps.GetString(
                                            encoding, buffer.ToArray(), EncodingType.Binary,
                                            ref stringValue, ref result);

                                        if (code == ReturnCode.Ok)
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                code = interpreter.SetVariableValue(
                                                    VariableFlags.None, arguments[2], stringValue,
                                                    null, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    int length = stringValue.Length;

                                                    if (length > 0)
                                                    {
                                                        result = length;
                                                    }
                                                    else
                                                    {
                                                        bool canSeek = channel.CanSeek;

                                                        if ((canSeek && channel.EndOfStream) ||
                                                            (!canSeek && channel.HitEndOfStream))
                                                        {
                                                            result = Channel.EndOfFile;
                                                        }
                                                        else
                                                        {
                                                            result = length; /* ZERO */
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = stringValue;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Engine.SetExceptionErrorCode(interpreter, e);

                                    result = e;
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "failed to get encoding for channel \"{0}\"", 
                                    channelId);

                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"gets channelId ?varName?\"";
                        code = ReturnCode.Error;
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
        }
        #endregion
    }
}
