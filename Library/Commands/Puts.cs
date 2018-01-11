/*
 * Puts.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("646d87e5-b37f-46e4-a8d7-1b8e70234d93")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("channel")]
    internal sealed class Puts : Core
    {
        public Puts(
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

                                string channelId = Channel.StdOut;

                                if ((argumentIndex + 1) < arguments.Count)
                                    channelId = arguments[argumentIndex];

                                Channel channel = interpreter.GetChannel(channelId, ref result);

                                if (channel != null)
                                {
                                    Encoding encoding = null;

                                    if (interpreter.GetChannelEncoding(channel, ref encoding) == ReturnCode.Ok)
                                    {
                                        string output = arguments[arguments.Count - 1];

                                        try
                                        {
                                            if (channel.IsVirtualOutput)
                                            {
                                                //
                                                // NOTE: The encoding is ignored, because this is directly 
                                                //       from the input string, which is already Unicode.
                                                //
                                                channel.AppendVirtualOutput(output);

                                                if (newLine)
                                                    channel.AppendVirtualOutput(Channel.NewLine);

                                                result = String.Empty;
                                            }
                                            else
                                            {
                                                BinaryWriter binaryWriter = interpreter.GetChannelBinaryWriter(channel);

                                                if (binaryWriter != null)
                                                {
                                                    byte[] bytes = null;

                                                    code = StringOps.GetBytes(
                                                        encoding, output, EncodingType.Binary, ref bytes,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        channel.CheckAppend();

#if CONSOLE
                                                        if (channel.IsConsoleStream)
                                                        {
                                                            int offset = 0;
                                                            int count = bytes.Length;

                                                            while (count > 0)
                                                            {
                                                                int writeCount = Math.Min(
                                                                    count, _Hosts.Console.SafeWriteSize);

                                                                binaryWriter.Write(bytes, offset, writeCount);

                                                                offset += writeCount;
                                                                count -= writeCount;
                                                            }
                                                        }
                                                        else
#endif
                                                        {
                                                            binaryWriter.Write(bytes);
                                                        }

                                                        if (newLine)
                                                            binaryWriter.Write(Channel.NewLine);

#if MONO || MONO_HACKS
                                                        //
                                                        // HACK: *MONO* As of Mono 2.8.0, it seems that Mono "loses"
                                                        //       output unless a flush is performed right after a
                                                        //       write.  So far, this has only been observed for the
                                                        //       console channels; however, always using flush here
                                                        //       on Mono shouldn't cause too many problems, except a
                                                        //       slight loss in performance.
                                                        //       https://bugzilla.novell.com/show_bug.cgi?id=645193
                                                        //
                                                        if (CommonOps.Runtime.IsMono())
                                                        {
                                                            binaryWriter.Flush(); /* throw */
                                                        }
                                                        else
#endif
                                                        {
                                                            //
                                                            // NOTE: Check if we should automatically flush the channel
                                                            //       after each "logical" write done by this command.
                                                            //
                                                            /* IGNORED */
                                                            channel.CheckAutoFlush();
                                                        }

                                                        result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "failed to get binary writer for channel \"{0}\"", 
                                                        channelId);

                                                    code = ReturnCode.Error;
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
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                }
                                else
                                {
                                    result = "wrong # args: should be \"puts ?-nonewline? ?channelId? string\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"puts ?-nonewline? ?channelId? string\"";
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
