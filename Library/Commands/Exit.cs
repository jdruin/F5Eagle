/*
 * Exit.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("c950492b-f20d-41b8-8a6f-b4c2ce7cbba6")]
    /*
     * POLICY: We disallow certain "unsafe" options.  In a "safe" interpreter,
     *         the only thing this command will do is cause the interpreter
     *         to be marked as exited (i.e. no interpreter state information
     *         will be lost and the host application will not exit).
     */
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Exit : Core
    {
        public Exit(
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
                    if (arguments.Count >= 1)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-force", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-fail", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-message", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-current", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                        });

                        int argumentIndex = Index.Invalid;

                        if (arguments.Count > 1)
                            code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);
                        else
                            code = ReturnCode.Ok;

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex == Index.Invalid) ||
                                ((argumentIndex + 1) == arguments.Count))
                            {
                                bool force = false;

                                if (options.IsPresent("-force"))
                                    force = true;

                                bool fail = false;

                                if (options.IsPresent("-fail"))
                                    fail = true;

                                Variant value = null;
                                string message = null;

                                if (options.IsPresent("-message", ref value))
                                    message = value.ToString();

                                //
                                // NOTE: The default exit code is "success" (i.e. zero).
                                //
                                ExitCode exitCode = ResultOps.SuccessExitCode();

                                if (options.IsPresent("-current"))
                                    exitCode = interpreter.ExitCode;

                                //
                                // NOTE: Was an exit code specified in the command?
                                //
                                if (argumentIndex != Index.Invalid)
                                {
                                    object enumValue = EnumOps.TryParseEnum(
                                        typeof(ExitCode), arguments[argumentIndex],
                                        true, true, ref result);

                                    if (enumValue is ExitCode)
                                    {
                                        exitCode = (ExitCode)enumValue;
                                    }
                                    else
                                    {
                                        result = ScriptOps.BadValue(
                                            null, "exit code", arguments[argumentIndex],
                                            Enum.GetNames(typeof(ExitCode)), null, ", or an integer");

                                        code = ReturnCode.Error;
                                    }
                                }

                                //
                                // NOTE: Make sure we succeeded at coverting the exit code to an integer.
                                //
                                if (code == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Make sure the interpreter host, if any, agrees to exit (i.e. it may deny the
                                    //       request if the application is doing something that should not be interrupted).
                                    //
                                    code = interpreter.CanExit(exitCode, force, fail, message, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        //
                                        // NOTE: Exit the application (either by marking the current interpreter as "exited"
                                        //       or physically exiting the containing process).
                                        //
                                        TraceOps.DebugTrace(String.Format(
                                            "Execute: {0}, interpreter = {1}, message = {2}",
                                            force && fail ?
                                            "forcibly failing" : force ? "forcibly exiting" : "exiting",
                                            FormatOps.InterpreterNoThrow(interpreter), FormatOps.WrapOrNull(message)),
                                            typeof(Exit).Name, TracePriority.Command);

                                        if (force)
                                        {
#if !MONO
                                            if (fail && !CommonOps.Runtime.IsMono())
                                            {
                                                try
                                                {
                                                    //
                                                    // NOTE: Using this method to exit a script is NOT recommended unless
                                                    //       you are trying to prevent damaging another part of the system.
                                                    //
                                                    // MONO: This method is not supported by the Mono runtime.
                                                    //
                                                    Environment.FailFast(message);

                                                    /* NOT REACHED */
                                                    result = "failed to exit process";
                                                    code = ReturnCode.Error;
                                                }
                                                catch (Exception e)
                                                {
                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
#endif
                                            {
                                                //
                                                // BUGFIX: Try to dispose our containing interpreter now.  We must do
                                                //         this to prevent it from being disposed on a random GC thread.
                                                //
                                                try
                                                {
                                                    interpreter.Dispose();
                                                    interpreter = null;
                                                }
                                                catch (Exception e)
                                                {
                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }

                                                //
                                                // NOTE: If we could not dispose the interpreter properly, complain;
                                                //       however, keep exiting anyway.
                                                //
                                                if (code != ReturnCode.Ok)
                                                    DebugOps.Complain(interpreter, code, result);

                                                try
                                                {
                                                    //
                                                    // NOTE: Using this method to exit a script is NOT recommended unless
                                                    //       you are running a standalone script in the Eagle Shell (i.e.
                                                    //       you are not hosted within another application).
                                                    //
                                                    Environment.Exit((int)exitCode);

                                                    /* NOT REACHED */
                                                    result = "failed to exit process";
                                                    code = ReturnCode.Error;
                                                }
                                                catch (Exception e)
                                                {
                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            interpreter.ExitCode = exitCode;
                                            interpreter.Exit = true;

                                            result = String.Empty;
                                            code = ReturnCode.Ok;
                                        }
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
                                    result = "wrong # args: should be \"exit ?options? ?returnCode?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"exit ?options? ?returnCode?\"";
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
