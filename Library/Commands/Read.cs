/*
 * Read.cs --
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
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("8bde05f7-44aa-4d1c-a350-15c02319305a")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Read : Core
    {
        public Read(
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
                    if (arguments.Count >= 2)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonewline", null)
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) >= arguments.Count))
                            {
                                bool newLine = true;

                                if (options.IsPresent("-nonewline"))
                                    newLine = false;

                                int count = Count.Invalid;

                                if ((argumentIndex + 1) < arguments.Count)
                                {
                                    code = Value.GetInteger2(
                                        (IGetValue)arguments[argumentIndex + 1], ValueFlags.AnyInteger,
                                        interpreter.CultureInfo, ref count, ref result);
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    string channelId = arguments[argumentIndex];
                                    Channel channel = interpreter.GetChannel(channelId, ref result);

                                    if (channel != null)
                                    {
                                        CharList endOfLine = channel.GetInputEndOfLine();
                                        Encoding encoding = null;

                                        if (interpreter.GetChannelEncoding(channel, ref encoding) == ReturnCode.Ok)
                                        {
                                            //
                                            // NOTE: If they do not specify a count we simply read
                                            //       until end-of-file.
                                            //
                                            try
                                            {
                                                ByteList buffer = null;

                                                code = channel.Read(count, null, false, ref buffer, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    //
                                                    // BUGFIX: Remove trailing end-of-line character even
                                                    //         when reading the entire stream.
                                                    //
                                                    if (!newLine)
                                                        channel.RemoveTrailingEndOfLine(buffer, endOfLine);

                                                    string stringValue = null;

                                                    code = StringOps.GetString(
                                                        encoding, buffer.ToArray(), EncodingType.Binary,
                                                        ref stringValue, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = stringValue;
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
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                }
                                else
                                {
                                    result = "wrong # args: should be \"read channelId ?numChars?\" or \"read ?-nonewline? channelId\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"read channelId ?numChars?\" or \"read ?-nonewline? channelId\"";
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
