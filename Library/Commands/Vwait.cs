/*
 * Vwait.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("9a58d5a9-85b8-43e1-9136-5667eb2e87bf")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("event")]
    internal sealed class Vwait : Core
    {
        public Vwait(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                    if (arguments.Count >= 2)
                    {
                        //
                        // NOTE: Grab the variable and event wait flags from the
                        //       interpreter and use them as the defaults for the
                        //       associated options.
                        //
                        EventWaitFlags eventWaitFlags = interpreter.EventWaitFlags;
                        VariableFlags variableFlags = interpreter.EventVariableFlags;

                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null,
                                OptionFlags.MustHaveObjectValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-handle", null),
                            new Option(typeof(EventWaitFlags),
                                OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-eventwaitflags",
                                new Variant(eventWaitFlags)),
                            new Option(typeof(VariableFlags),
                                OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-variableflags",
                                new Variant(variableFlags)),
                            new Option(null,
                                OptionFlags.MustHaveIntegerValue | OptionFlags.Unsafe,
                                Index.Invalid, Index.Invalid, "-limit", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-force", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-nocomplain", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid,
                                Index.Invalid, "-leaveresult", null),
                            new Option(null, OptionFlags.None, Index.Invalid,
                                Index.Invalid, Option.EndOfOptions, null)
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(
                            options, arguments, 0, 1, Index.Invalid, false,
                            ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) &&
                                ((argumentIndex + 1) == arguments.Count))
                            {
                                Variant value = null;
                                EventWaitHandle @event = null;

                                if (options.IsPresent("-handle", ref value))
                                {
                                    IObject @object = (IObject)value.Value;

                                    if ((@object.Value == null) ||
                                        (@object.Value is EventWaitHandle))
                                    {
                                        @event = (EventWaitHandle)@object.Value;
                                    }
                                    else
                                    {
                                        result = "option value has invalid EventWaitHandle";
                                        code = ReturnCode.Error;
                                    }
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    int limit = 0;

                                    if (options.IsPresent("-limit", ref value))
                                        limit = (int)value.Value;

                                    if (options.IsPresent("-eventwaitflags", ref value))
                                        eventWaitFlags = (EventWaitFlags)value.Value;

                                    if (options.IsPresent("-variableflags", ref value))
                                        variableFlags = (VariableFlags)value.Value;

                                    bool force = false;

                                    if (options.IsPresent("-force"))
                                        force = true;

                                    bool noComplain = false;

                                    if (options.IsPresent("-nocomplain"))
                                        noComplain = true;

                                    bool leaveResult = false;

                                    if (options.IsPresent("-leaveresult"))
                                        leaveResult = true;

                                    //
                                    // NOTE: Typically, we do not want to enter a wait state if
                                    //       there are no events queued because there would be
                                    //       no possible way to ever (gracefully) exit the wait;
                                    //       however, there are exceptions to this.
                                    //
                                    if (force || interpreter.ShouldWaitVariable())
                                    {
                                        //
                                        // HACK: The call to ThreadOps.IsStaThread here is made
                                        //       under the assumption that no user-interface
                                        //       thread can exist without also being an STA
                                        //       thread.  This may eventually prove to be false;
                                        //       however, currently WinForms, WPF, et al require
                                        //       this (i.e. an STA thread).
                                        //
                                        if (!FlagOps.HasFlags(
                                                eventWaitFlags, EventWaitFlags.UserInterface,
                                                true) && ThreadOps.IsStaThread())
                                        {
                                            eventWaitFlags |= EventWaitFlags.UserInterface;
                                        }

                                        code = interpreter.WaitVariable(eventWaitFlags,
                                            variableFlags, arguments[argumentIndex], limit,
                                            @event, ref result);

                                        if ((code != ReturnCode.Ok) && noComplain)
                                            code = ReturnCode.Ok;

                                        if ((code == ReturnCode.Ok) && !leaveResult)
                                            Engine.ResetResult(interpreter, ref result);
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "can't wait for variable \"{0}\": would wait forever",
                                            arguments[1]);

                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(arguments[argumentIndex]))
                                {
                                    result = OptionDictionary.BadOption(
                                        options, arguments[argumentIndex]);
                                }
                                else
                                {
                                    result = "wrong # args: should be \"vwait ?options? varName\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"vwait ?options? varName\"";
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
