/*
 * Debug.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if DEBUGGER
using Eagle._Interfaces.Private;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("8d2559ac-e4e4-41c4-8183-52c90008d25f")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("debug")]
    internal sealed class Debug : Core
    {
        public Debug(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "break", "breakpoints", "cacheconfiguration", "callback",
            "cleanup", "collect", "complaint", "enable", "eval",
            "exception", "execute", "function", "gcmemory", "halt",
            "history", "icommand", "interactive", "invoke", "iqueue",
            "iresult", "lockcmds", "lockprocs", "lockvars", "log",
            "memory", "oncancel", "onerror", "onexecute", "onexit",
            "onreturn", "ontest", "ontoken", "operator", "output",
            "paths", "pluginexecute", "pluginflags", "purge",
            "procedureflags", "resume", "restore", "ready",
            "refreshautopath", "run", "runtimeoption", "secureeval",
            "self", "setup", "shell", "stack", "status", "step",
            "steps", "subst", "suspend", "sysmemory", "test",
            "testpath", "token", "trace", "types", "undelete",
            "variable", "vout", "watch"
        });

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

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
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            //
                            // NOTE: Programmatically interact with the debugger
                            //       (breakpoint, watch, eval, etc).
                            //
                            switch (subCommand)
                            {
                                case "break":
                                    {
                                        if (arguments.Count >= 2)
                                        {
#if DEBUGGER
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue,
                                                    Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-strict", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, false,
                                                    ref argumentIndex, ref result);
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    Variant value = null;
                                                    Interpreter localInterpreter = interpreter;

                                                    if (options.IsPresent("-interpreter", ref value))
                                                        localInterpreter = (Interpreter)value.Value;

                                                    bool strict = false;

                                                    if (options.IsPresent("-strict"))
                                                        strict = true;

                                                    IDebugger debugger = null;
                                                    bool enabled = false;
                                                    HeaderFlags headerFlags = HeaderFlags.None;

                                                    if (Engine.CheckDebugger(localInterpreter, !strict,
                                                            ref debugger, ref enabled, ref headerFlags,
                                                            ref result))
                                                    {
#if PREVIOUS_RESULT
                                                        //
                                                        // NOTE: At this point, the result of the previous
                                                        //       command may still be untouched and will
                                                        //       be displayed verbatim upon entry into the
                                                        //       interactive loop, if necessary.
                                                        //
                                                        result = Result.Copy(
                                                            Interpreter.GetPreviousResult(localInterpreter),
                                                            true); /* COPY */
#endif

                                                        //
                                                        // NOTE: If the debugger is disabled, skip breaking
                                                        //       into the nested interactive loop.
                                                        //
                                                        if (enabled && FlagOps.HasFlags(debugger.Types,
                                                                BreakpointType.Demand, true))
                                                        {
                                                            //
                                                            // NOTE: Break into the debugger by starting a
                                                            //       nested interactive loop.
                                                            //
                                                            code = localInterpreter.DebuggerBreak(debugger,
                                                                new InteractiveLoopData(
                                                                (result != null) ?
                                                                    result.ReturnCode : ReturnCode.Ok,
                                                                BreakpointType.Demand, this.Name,
                                                                headerFlags | HeaderFlags.Breakpoint,
                                                                clientData, arguments), ref result);

                                                            //
                                                            // FIXME: If there were no other failures in the
                                                            //        interactive loop, perhaps we should reflect
                                                            //        the previous result?  Better logic here may
                                                            //        be needed.
                                                            //
                                                            if ((code == ReturnCode.Ok) && (result != null))
                                                                code = result.ReturnCode;
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: Return the previous return code, if any.
                                                            //
                                                            code = (result != null) ?
                                                                result.ReturnCode : ReturnCode.Ok;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Attempting to break into the debugger when
                                                        //       it is disabled or unavailable is an error
                                                        //       if "strict" mode is enabled.
                                                        //
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
                                                        result = "wrong # args: should be \"debug break ?options?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug break ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "breakpoints":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER && BREAKPOINTS
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 3)
                                                    pattern = arguments[2];

                                                IStringList list = null;

                                                code = debugger.GetBreakpointList(
                                                    interpreter, pattern, false, ref list,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = list.ToString(); /* EXEMPT */
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug breakpoints ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "callback":
                                    {
                                        //
                                        // debug callback ?{}|arg ...?
                                        //
                                        if (arguments.Count >= 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (arguments.Count >= 3)
                                                {
                                                    //
                                                    // NOTE: If there is only one argument and it's empty,
                                                    //       null out the callback arguments; otherwise,
                                                    //       use them verbatim.
                                                    //
                                                    ArgumentList list = ArgumentList.NullIfEmpty(
                                                        arguments, 2);

                                                    debugger.CallbackArguments =
                                                        (list != null) ? new StringList(list) : null;

                                                    debugger.CheckCallbacks(interpreter);

                                                    result = String.Empty;
                                                }
                                                else if (arguments.Count == 2)
                                                {
                                                    result = debugger.CallbackArguments;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug callback ?{}|arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cleanup":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                                            try
                                            {
                                                CacheFlags cacheFlags = CacheFlags.Default;

                                                if (arguments.Count == 3)
                                                {
                                                    object enumValue = EnumOps.TryParseFlagsEnum(
                                                        interpreter, typeof(CacheFlags),
                                                        cacheFlags.ToString(), arguments[2],
                                                        interpreter.CultureInfo, true, true,
                                                        true, ref result);

                                                    if (enumValue is CacheFlags)
                                                        cacheFlags = (CacheFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                ICallFrame variableFrame = interpreter.CurrentFrame;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = interpreter.GetVariableFrameViaResolvers(
                                                        LookupFlags.Default, ref variableFrame,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = StringList.MakeList(CallFrameOps.Cleanup(
                                                        interpreter.CurrentFrame, variableFrame, false),
                                                        interpreter.ClearCaches(cacheFlags, true),
                                                        GC.GetTotalMemory(true));
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug cleanup ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "collect":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            GarbageFlags flags = GarbageFlags.ForCommand;

                                            if (arguments.Count == 3)
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(GarbageFlags),
                                                    flags.ToString(), arguments[2],
                                                    interpreter.CultureInfo, true, true,
                                                    true, ref result);

                                                if (enumValue is GarbageFlags)
                                                    flags = (GarbageFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                /* NO RESULT */
                                                ObjectOps.CollectGarbage(flags); /* throw */

                                                result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug collect ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "complaint":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = DebugOps.SafeGetComplaint(interpreter);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug complaint\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "enable":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;
                                            bool enabled = false;

                                            if (Engine.CheckDebugger(interpreter, true,
                                                    ref debugger, ref enabled, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                enabled = !enabled;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.Enabled = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "debugger " +
                                                            ConversionOps.ToEnabled(debugger.Enabled));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug enable ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (arguments.Count >= 3)
                                        {
#if DEBUGGER
                                            Interpreter debugInterpreter = null;

                                            if (Engine.CheckDebuggerInterpreter(interpreter, false,
                                                    ref debugInterpreter, ref result))
                                            {
                                                string name = StringList.MakeList("debug eval");

                                                ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                    CallFrameFlags.Evaluate | CallFrameFlags.Debugger);

                                                interpreter.PushAutomaticCallFrame(frame);

                                                if (arguments.Count == 3)
                                                    code = debugInterpreter.EvaluateScript(
                                                        arguments[2], ref result);
                                                else
                                                    code = debugInterpreter.EvaluateScript(
                                                        arguments, 2, ref result);

                                                if (code == ReturnCode.Error)
                                                    Engine.AddErrorInformation(interpreter, result,
                                                        String.Format("{0}    (in debug eval script line {1})",
                                                            Environment.NewLine, Interpreter.GetErrorLine(debugInterpreter)));

                                                //
                                                // NOTE: Pop the original call frame that we pushed above and
                                                //       any intervening scope call frames that may be leftover
                                                //       (i.e. they were not explicitly closed).
                                                //
                                                /* IGNORED */
                                                interpreter.PopScopeCallFramesAndOneMore();
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug eval arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exception":
                                    {
                                        if (arguments.Count >= 2)
                                        {
#if PREVIOUS_RESULT
                                            OptionDictionary options = ObjectOps.GetExceptionOptions();

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    Type returnType;
                                                    ObjectFlags objectFlags;
                                                    string objectName;
                                                    string interpName;
                                                    bool create;
                                                    bool dispose;
                                                    bool alias;
                                                    bool aliasRaw;
                                                    bool aliasAll;
                                                    bool aliasReference;
                                                    bool toString;

                                                    ObjectOps.ProcessFixupReturnValueOptions(
                                                        options, null, out returnType, out objectFlags,
                                                        out objectName, out interpName, out create,
                                                        out dispose, out alias, out aliasRaw, out aliasAll,
                                                        out aliasReference, out toString);

                                                    //
                                                    // NOTE: We intend to modify the interpreter state,
                                                    //       make sure this is not forbidden.
                                                    //
                                                    if (interpreter.IsModifiable(true, ref result))
                                                    {
                                                        Result previousResult = Interpreter.GetPreviousResult(
                                                            interpreter);

                                                        if (previousResult != null)
                                                        {
                                                            Exception exception = previousResult.Exception;

                                                            //
                                                            // NOTE: Create an opaque object handle
                                                            //       for the exception from the
                                                            //       previous result.
                                                            //
                                                            ObjectOptionType objectOptionType = ObjectOptionType.Exception |
                                                                ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                            code = MarshalOps.FixupReturnValue(
                                                                interpreter, interpreter.Binder,
                                                                interpreter.CultureInfo, returnType, objectFlags,
                                                                ObjectOps.GetInvokeOptions(objectOptionType),
                                                                objectOptionType, objectName, interpName, exception,
                                                                create, dispose, alias, aliasReference, toString,
                                                                ref result);
                                                        }
                                                        else
                                                        {
                                                            result = "no previous result";
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
                                                    result = "wrong # args: should be \"debug exception ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug exception ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "execute":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
#if DEBUGGER
                                            string executeName = arguments[2];
                                            IExecute execute = null;

                                            code = interpreter.GetIExecuteViaResolvers(
                                                interpreter.GetResolveEngineFlags(true),
                                                executeName, null, LookupFlags.Default,
                                                ref execute, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count == 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (Engine.SetExecuteBreakpoint(
                                                                execute, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "execute \"{0}\" breakpoint is now {1}",
                                                                execute, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} execute \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), execute, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "execute \"{0}\" breakpoint is {1}",
                                                        execute, ConversionOps.ToEnabled(
                                                            Engine.HasExecuteBreakpoint(execute)));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug execute name ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "function":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
#if DEBUGGER
                                            string functionName = arguments[2];
                                            IFunction function = null;

                                            code = interpreter.GetFunction(
                                                functionName, LookupFlags.Default,
                                                ref function, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count == 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (Engine.SetExecuteArgumentBreakpoint(
                                                                function, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "function \"{0}\" breakpoint is now {1}",
                                                                function, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} function \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), function, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "function \"{0}\" breakpoint is {1}",
                                                        function, ConversionOps.ToEnabled(
                                                            Engine.HasExecuteArgumentBreakpoint(function)));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug function name ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "gcmemory":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool collect = false;

                                            if (arguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref collect, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = GC.GetTotalMemory(collect);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug gcmemory ?collect?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "halt":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            Result haltResult = null;

                                            if (arguments.Count == 3)
                                                haltResult = arguments[2];

                                            code = Engine.HaltEvaluate(
                                                interpreter, haltResult, CancelFlags.DebugHalt,
                                                ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug halt ?result?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "history":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if HISTORY
                                            if (arguments.Count == 3)
                                            {
                                                bool enabled = false;

                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                    interpreter.History = enabled;
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = interpreter.History;
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug history ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "icommand":
                                    {
                                        //
                                        // debug icommand ?command?
                                        //
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    debugger.Command = StringOps.NullIfEmpty(arguments[2]);
                                                    result = String.Empty;
                                                }
                                                else if (arguments.Count == 2)
                                                {
                                                    result = debugger.Command;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug icommand ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cacheconfiguration":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
                                            if (arguments.Count >= 3)
                                            {
                                                string text = arguments[2];

                                                if (String.IsNullOrEmpty(text))
                                                    text = null;

                                                int level = Level.Invalid;

                                                if (arguments.Count == 4)
                                                {
                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        interpreter.CultureInfo, ref level, ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        CacheConfiguration.Initialize(
                                                            interpreter, text, level, true);

                                                        /* IGNORED */
                                                        interpreter.PreSetupCaches();
                                                    }
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = CacheConfiguration.GetStateAndSettings();
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug cacheconfig ?settings? ?level?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "interactive":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                bool enabled = false;

                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                    interpreter.Interactive = enabled;
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = interpreter.Interactive;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug interactive ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "invoke":
                                    {
                                        if (arguments.Count >= 3)
                                        {
#if DEBUGGER
                                            Interpreter debugInterpreter = null;

                                            if (Engine.CheckDebuggerInterpreter(interpreter, false,
                                                    ref debugInterpreter, ref result))
                                            {
                                                int currentLevel = 0;

                                                code = interpreter.GetInfoLevel(
                                                    CallFrameOps.InfoLevelSubCommand, ref currentLevel,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    bool mark = false;
                                                    bool absolute = false;
                                                    bool super = false;
                                                    int level = 0;
                                                    ICallFrame currentFrame = null;
                                                    ICallFrame otherFrame = null;

                                                    FrameResult frameResult = debugInterpreter.GetCallFrame(
                                                        arguments[2], ref mark, ref absolute, ref super,
                                                        ref level, ref currentFrame, ref otherFrame,
                                                        ref result);

                                                    if (frameResult != FrameResult.Invalid)
                                                    {
                                                        int argumentIndex = ((int)frameResult + 2);

                                                        //
                                                        // BUGFIX: The argument count needs to be checked
                                                        //         again here.
                                                        //
                                                        if (argumentIndex < arguments.Count)
                                                        {
                                                            if (mark)
                                                            {
                                                                code = CallFrameOps.MarkMatching(
                                                                    debugInterpreter.CallStack,
                                                                    debugInterpreter.CurrentFrame,
                                                                    absolute, level,
                                                                    CallFrameFlags.Variables,
                                                                    CallFrameFlags.Invisible |
                                                                        CallFrameFlags.NoVariables,
                                                                    CallFrameFlags.Invisible, false,
                                                                    false, true, ref result);
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                try
                                                                {
                                                                    string name = StringList.MakeList("debug invoke",
                                                                        arguments[2], arguments[argumentIndex]);

                                                                    ICallFrame newFrame = debugInterpreter.NewUplevelCallFrame(
                                                                        name, currentLevel, CallFrameFlags.Debugger, mark,
                                                                        currentFrame, otherFrame);

                                                                    ICallFrame savedFrame = null;

                                                                    debugInterpreter.PushUplevelCallFrame(
                                                                        currentFrame, newFrame, true, ref savedFrame);

                                                                    code = debugInterpreter.Invoke(
                                                                        arguments[argumentIndex], clientData,
                                                                        ArgumentList.GetRange(arguments, argumentIndex),
                                                                        ref result);

                                                                    if (code == ReturnCode.Error)
                                                                        Engine.AddErrorInformation(interpreter, result,
                                                                            String.Format("{0}    (\"debug invoke\" body line {1})",
                                                                                Environment.NewLine, Interpreter.GetErrorLine(debugInterpreter)));

                                                                    //
                                                                    // NOTE: Pop the original call frame
                                                                    //       that we pushed above and any
                                                                    //       intervening scope call frames
                                                                    //       that may be leftover (i.e. they
                                                                    //       were not explicitly closed).
                                                                    //
                                                                    /* IGNORED */
                                                                    debugInterpreter.PopUplevelCallFrame(
                                                                        currentFrame, newFrame, ref savedFrame);
                                                                }
                                                                finally
                                                                {
                                                                    if (mark)
                                                                    {
                                                                        //
                                                                        // NOTE: We should not get an error at
                                                                        //       this point from unmarking the
                                                                        //       call frames; however, if we do
                                                                        //       get one, we need to complain
                                                                        //       loudly about it because that
                                                                        //       means the interpreter state
                                                                        //       has probably been corrupted
                                                                        //       somehow.
                                                                        //
                                                                        ReturnCode markCode;
                                                                        Result markResult = null;

                                                                        markCode = CallFrameOps.MarkMatching(
                                                                            debugInterpreter.CallStack,
                                                                            debugInterpreter.CurrentFrame,
                                                                            absolute, level,
                                                                            CallFrameFlags.Variables,
                                                                            CallFrameFlags.NoVariables,
                                                                            CallFrameFlags.Invisible, false,
                                                                            false, false, ref markResult);

                                                                        if (markCode != ReturnCode.Ok)
                                                                        {
                                                                            DebugOps.Complain(debugInterpreter,
                                                                                markCode, markResult);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "wrong # args: should be \"debug invoke ?level? cmd ?arg ...?\"";
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
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug invoke ?level? cmd ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "iqueue":
                                    {
                                        if (arguments.Count >= 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false, ref debugger, ref result))
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-dump", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-clear", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                                });

                                                int argumentIndex = Index.Invalid;

                                                if (arguments.Count > 2)
                                                {
                                                    code = interpreter.GetOptions(
                                                        options, arguments, 0, 2, Index.Invalid, true,
                                                        ref argumentIndex, ref result);
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Ok;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex == Index.Invalid) ||
                                                        ((argumentIndex + 1) == arguments.Count))
                                                    {
                                                        bool dump = false;

                                                        if (options.IsPresent("-dump"))
                                                            dump = true;

                                                        bool clear = false;

                                                        if (options.IsPresent("-clear"))
                                                            clear = true;

                                                        if ((code == ReturnCode.Ok) && dump)
                                                            code = debugger.DumpCommands(ref result);

                                                        if ((code == ReturnCode.Ok) && clear)
                                                            code = debugger.ClearCommands(ref result);

                                                        if ((code == ReturnCode.Ok) &&
                                                            (argumentIndex != Index.Invalid))
                                                        {
                                                            code = debugger.EnqueueCommand(
                                                                arguments[argumentIndex], ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && !dump)
                                                            result = String.Empty;
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
                                                            result = "wrong # args: should be \"debug iqueue ?options? ?command?\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug iqueue ?options? ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "iresult":
                                    {
                                        //
                                        // debug iresult ?result?
                                        //
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    debugger.Result = StringOps.NullIfEmpty(arguments[2]);
                                                    result = String.Empty;
                                                }
                                                else if (arguments.Count == 2)
                                                {
                                                    result = debugger.Result;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug iresult ?result?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lockcmds":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            bool enabled = false;

                                            code = Value.GetBoolean2(
                                                arguments[2], ValueFlags.AnyBoolean,
                                                interpreter.CultureInfo, ref enabled, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 4)
                                                    pattern = ScriptOps.MakeCommandName(arguments[3]);

                                                int count = interpreter.SetCommandsReadOnly(
                                                    pattern, false, enabled);

                                                result = String.Format(
                                                    "{0} {1} {2}", enabled ? "locked" : "unlocked",
                                                    count, (count != 1) ? "commands" : "command");
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug lockcmds enabled ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lockprocs":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            bool enabled = false;

                                            code = Value.GetBoolean2(
                                                arguments[2], ValueFlags.AnyBoolean,
                                                interpreter.CultureInfo, ref enabled, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 4)
                                                    pattern = ScriptOps.MakeCommandName(arguments[3]);

                                                int count = interpreter.SetProceduresReadOnly(
                                                    pattern, false, enabled);

                                                result = String.Format(
                                                    "{0} {1} {2}", enabled ? "locked" : "unlocked",
                                                    count, (count != 1) ? "procedures" : "procedure");
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug lockprocs enabled ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lockvars":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            bool enabled = false;

                                            code = Value.GetBoolean2(
                                                arguments[2], ValueFlags.AnyBoolean,
                                                interpreter.CultureInfo, ref enabled, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 4)
                                                    pattern = arguments[3];

                                                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                {
                                                    ICallFrame variableFrame = null;

                                                    code = interpreter.GetVariableFrameViaResolvers(
                                                        LookupFlags.Default, ref variableFrame,
                                                        ref pattern, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (variableFrame != null)
                                                        {
                                                            VariableDictionary variables = variableFrame.Variables;

                                                            if (variables != null)
                                                            {
                                                                int count = variables.SetReadOnly(
                                                                    interpreter, pattern, enabled);

                                                                result = String.Format(
                                                                    "{0} {1} {2} in call frame {3}", enabled ?
                                                                    "locked" : "unlocked", count, (count != 1) ?
                                                                    "variables" : "variable", variableFrame.Name);
                                                            }
                                                            else
                                                            {
                                                                result = "call frame does not support variables";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "invalid call frame";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug lockvars enabled ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "log":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-level", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-category", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    int level = 0;

                                                    if (options.IsPresent("-level", ref value))
                                                        level = (int)value.Value;

                                                    string category = DebugOps.DefaultCategory;

                                                    if (options.IsPresent("-category", ref value))
                                                        category = value.ToString();

                                                    DebugOps.Log(level, category, arguments[argumentIndex]);

                                                    result = String.Empty;
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
                                                        result = "wrong # args: should be \"debug log ?options? message\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug log ?options? message\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "memory":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            StringList list = new StringList(
                                                "gcTotalMemory", GC.GetTotalMemory(false).ToString());

                                            int maxGeneration = GC.MaxGeneration;

                                            list.Add("gcMaxGeneration", maxGeneration.ToString());

                                            for (int generation = 0; generation <= maxGeneration; generation++)
                                            {
                                                list.Add(String.Format(
                                                    "gcCollectionCount({0})", generation),
                                                    GC.CollectionCount(generation).ToString());
                                            }

                                            list.Add("isServerGC",
                                                GCSettings.IsServerGC.ToString());

#if NET_35 || NET_40
                                            list.Add("gcLatencyMode",
                                                GCSettings.LatencyMode.ToString());
#endif

#if NATIVE
                                            Result error = null;

                                            if (NativeOps.GetMemoryStatus(
                                                    ref list, ref error) != ReturnCode.Ok)
                                            {
                                                list.Add(error);
                                            }
#endif

                                            result = list;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug memory\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "oncancel":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnCancel;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnCancel = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on cancel " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnCancel));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug oncancel ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onerror":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnError;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnError = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on error " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnError));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onerror ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onexecute":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnExecute;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnExecute = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on execute " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnExecute));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onexecute ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onexit":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnExit;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnExit = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on exit " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnExit));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onexit ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "onreturn":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnReturn;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnReturn = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on return " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnReturn));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug onreturn ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ontest":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnTest;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnTest = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on test " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnTest));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug ontest ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ontoken":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER && BREAKPOINTS
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.BreakOnToken;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    debugger.BreakOnToken = enabled;

                                                    IInteractiveHost interactiveHost = interpreter.Host;

                                                    if (interactiveHost != null)
                                                        /* IGNORED */
                                                        interactiveHost.WriteResultLine(
                                                            ReturnCode.Ok, "break on token " +
                                                            ConversionOps.ToEnabled(debugger.BreakOnToken));

                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug ontoken ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "operator":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
#if DEBUGGER
                                            string operatorName = arguments[2];
                                            IOperator @operator = null;

                                            code = interpreter.GetOperator(
                                                operatorName, LookupFlags.Default,
                                                ref @operator, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count == 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (Engine.SetExecuteArgumentBreakpoint(
                                                                @operator, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "operator \"{0}\" breakpoint is now {1}",
                                                                @operator, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} operator \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), @operator, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "operator \"{0}\" breakpoint is {1}",
                                                        @operator, ConversionOps.ToEnabled(
                                                            Engine.HasExecuteArgumentBreakpoint(@operator)));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug operator name ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "output":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if NATIVE
                                            DebugOps.Output(arguments[2]);

                                            result = String.Empty;
                                            code = ReturnCode.Ok;
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug output message\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "paths":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool all = false;

                                            if (arguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref all, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                StringPairList list = null;

                                                GlobalState.GetPaths(interpreter, all, ref list);

                                                result = list;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug paths ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pluginexecute":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            IPlugin plugin = null;

                                            code = interpreter.GetPlugin(
                                                arguments[2], LookupFlags.Default, ref plugin,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                StringList list = null;

                                                code = Parser.SplitList(
                                                    interpreter, arguments[3], 0, Length.Invalid,
                                                    false, ref list, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    //
                                                    // HACK: Convert empty list elements to a null
                                                    //       string.
                                                    //
                                                    for (int index = 0; index < list.Count; index++)
                                                        if (String.IsNullOrEmpty(list[index]))
                                                            list[index] = null;

                                                    //
                                                    // NOTE: The IExecuteRequest.Execute method is
                                                    //       always passed a string array here, not
                                                    //       a StringList object.  Upon success,
                                                    //       the response is always converted to a
                                                    //       string and used as the command result;
                                                    //       otherwise, the error is used as the
                                                    //       command result.
                                                    //
                                                    object response = null;

                                                    code = plugin.Execute(
                                                        interpreter, clientData, list.ToArray(),
                                                        ref response, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringOps.GetStringFromObject(
                                                            response);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug pluginexecute name request\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pluginflags":
                                    {
                                        //
                                        // debug pluginflags ?flags?
                                        //
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(PluginFlags),
                                                    interpreter.PluginFlags.ToString(),
                                                    arguments[2], interpreter.CultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is PluginFlags)
                                                    interpreter.PluginFlags = (PluginFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = interpreter.PluginFlags;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug pluginflags ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "procedureflags":
                                    {
                                        //
                                        // debug procedureflags procName ?flags?
                                        //
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            IProcedure procedure = null;

                                            code = interpreter.GetProcedureViaResolvers(
                                                ScriptOps.MakeCommandName(arguments[2]),
                                                LookupFlags.Default, ref procedure,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count == 4)
                                                {
                                                    object enumValue = EnumOps.TryParseFlagsEnum(
                                                        interpreter, typeof(ProcedureFlags),
                                                        procedure.Flags.ToString(),
                                                        arguments[3], interpreter.CultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is ProcedureFlags)
                                                        procedure.Flags = (ProcedureFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = procedure.Flags;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug procedureflags procName ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "purge":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            code = CallFrameOps.Purge(interpreter, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug purge\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resume":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, true,
                                                    ref debugger, ref result))
                                            {
                                                code = debugger.Resume(ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = "debugger resumed";
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug resume\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "restore":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool strict = false;

                                            if (arguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref strict, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                code = interpreter.RestoreCorePlugin(strict, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug restore ?strict?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ready":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            bool isolated = false; /* NOTE: Require isolated interpreter? */

                                            if (arguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref isolated, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                IDebugger debugger = null;

                                                if (isolated)
                                                {
                                                    Interpreter debugInterpreter = null;

                                                    result = Engine.CheckDebugger(
                                                        interpreter, false, ref debugger,
                                                        ref debugInterpreter, ref result);
                                                }
                                                else
                                                {
                                                    result = Engine.CheckDebugger(
                                                        interpreter, false, ref debugger,
                                                        ref result);
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug ready ?isolated?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "refreshautopath":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool verbose = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref verbose,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                /* NO RESULT */
                                                GlobalState.RefreshAutoPathList(verbose);

                                                result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug refreshautopath ?verbose?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "run":
                                    {
                                        //
                                        // NOTE: Think of this as "eval without debugging"
                                        //       or "run this at full speed".
                                        //
                                        if (arguments.Count >= 3)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                code = debugger.Suspend(ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    try
                                                    {
                                                        if (arguments.Count == 3)
                                                        {
                                                            code = interpreter.EvaluateScript(
                                                                arguments[2], ref result);
                                                        }
                                                        else
                                                        {
                                                            code = interpreter.EvaluateScript(
                                                                arguments, 2, ref result);
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        //
                                                        // NOTE: At this point, if we fail to resume
                                                        //       debugging for some reason, there is
                                                        //       not really much we can do about it.
                                                        //
                                                        ReturnCode resumeCode;
                                                        Result resumeResult = null;

                                                        resumeCode = debugger.Resume(ref resumeResult);

                                                        if (resumeCode != ReturnCode.Ok)
                                                        {
                                                            DebugOps.Complain(
                                                                interpreter, resumeCode, resumeResult);
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug run arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "runtimeoption":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            object enumValue = EnumOps.TryParseEnum(
                                                typeof(RuntimeOptionOperation), arguments[2],
                                                true, true, ref result);

                                            if (enumValue is RuntimeOptionOperation)
                                            {
                                                RuntimeOptionOperation operation =
                                                    (RuntimeOptionOperation)enumValue;

                                                switch (operation)
                                                {
                                                    case RuntimeOptionOperation.Has:
                                                        {
                                                            if (arguments.Count == 4)
                                                            {
                                                                result = interpreter.HasRuntimeOption(
                                                                    arguments[3]);

                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption has name\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Get:
                                                        {
                                                            if (arguments.Count == 3)
                                                            {
                                                                result = interpreter.RuntimeOptions;
                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption get\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Clear:
                                                        {
                                                            if (arguments.Count == 3)
                                                            {
                                                                result = interpreter.ClearRuntimeOptions();
                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption clear\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Add:
                                                        {
                                                            if (arguments.Count == 4)
                                                            {
                                                                result = interpreter.AddRuntimeOption(
                                                                    arguments[3]);

                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption add name\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Remove:
                                                        {
                                                            if (arguments.Count == 4)
                                                            {
                                                                result = interpreter.RemoveRuntimeOption(
                                                                    arguments[3]);

                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption remove name\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    case RuntimeOptionOperation.Set:
                                                        {
                                                            if (arguments.Count == 4)
                                                            {
                                                                StringList list = null;

                                                                code = Parser.SplitList(
                                                                    interpreter, arguments[3], 0, Length.Invalid,
                                                                    true, ref list, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                                    {
                                                                        ClientDataDictionary runtimeOptions =
                                                                            new ClientDataDictionary();

                                                                        foreach (string element in list)
                                                                            runtimeOptions[element] = null;

                                                                        interpreter.RuntimeOptions = runtimeOptions;
                                                                        result = interpreter.RuntimeOptions;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "wrong # args: should be \"debug runtimeoption set list\"";
                                                                code = ReturnCode.Error;
                                                            }
                                                            break;
                                                        }
                                                    default:
                                                        {
                                                            result = String.Format(
                                                                "unsupported runtime option operation \"{0}\"",
                                                                operation);

                                                            code = ReturnCode.Error;
                                                            break;
                                                        }
                                                }
                                            }
                                            else
                                            {
                                                result = ScriptOps.BadValue(
                                                    null, "runtime option operation", arguments[2],
                                                    Enum.GetNames(typeof(RuntimeOptionOperation)),
                                                    null, null);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug runtimeoption operation ?arg?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "secureeval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-file", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-trusted", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-events", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    Variant value = null;
                                                    bool file = false;

                                                    if (options.IsPresent("-file", ref value))
                                                        file = (bool)value.Value;

                                                    bool trusted = false;

                                                    if (options.IsPresent("-trusted", ref value))
                                                        trusted = (bool)value.Value;

                                                    bool events = !trusted;

                                                    if (options.IsPresent("-events", ref value))
                                                        events = (bool)value.Value;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter slaveInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref slaveInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string name = StringList.MakeList("debug secure eval",
                                                            path, trusted ? "(trusted)" : "(untrusted)",
                                                            events ? "(with events)" : "(without events)");

                                                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Evaluate | CallFrameFlags.Debugger |
                                                            CallFrameFlags.Restricted);

                                                        interpreter.PushAutomaticCallFrame(frame);

                                                        bool locked = false;

                                                        try
                                                        {
                                                            slaveInterpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                                                            if (locked)
                                                            {
                                                                int savedEnabled = 0;

                                                                if (!events)
                                                                {
                                                                    /* IGNORED */
                                                                    EventOps.SaveEnabledAndForceDisabled(
                                                                        slaveInterpreter, ref savedEnabled);
                                                                }

                                                                try
                                                                {
                                                                    if (trusted)
                                                                    {
                                                                        //
                                                                        // NOTE: Use of this flag is currently limited to "safe" slave
                                                                        //       interpreters only.  Just ignore the flag if called for
                                                                        //       an "unsafe" interpreter.
                                                                        //
                                                                        if (slaveInterpreter.InternalIsSafe())
                                                                            slaveInterpreter.InternalMarkTrusted();
                                                                        else
                                                                            trusted = false;
                                                                    }

                                                                    try
                                                                    {
                                                                        //
                                                                        // HACK: If necessary, add the "IgnoreHidden" engine flag to the
                                                                        //       per-thread engine flags for this interpreter so that the
                                                                        //       script specified by the caller can run with full trust.
                                                                        //       The per-thread engine flags must be used in this case;
                                                                        //       otherwise, other scripts being evaluated on other threads
                                                                        //       in this interpreter would also gain full trust.
                                                                        //
                                                                        bool added = false;

                                                                        if (trusted &&
                                                                            !Engine.HasIgnoreHidden(slaveInterpreter.ContextEngineFlags))
                                                                        {
                                                                            added = true;
                                                                            slaveInterpreter.ContextEngineFlags |= EngineFlags.IgnoreHidden;
                                                                        }

                                                                        try
                                                                        {
                                                                            if (((argumentIndex + 2) == arguments.Count))
                                                                            {
                                                                                if (file)
                                                                                {
                                                                                    code = slaveInterpreter.EvaluateFile(
                                                                                        arguments[argumentIndex + 1], ref result);
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = slaveInterpreter.EvaluateScript(
                                                                                        arguments[argumentIndex + 1], ref result);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                if (file)
                                                                                {
                                                                                    result = String.Format(
                                                                                        "wrong # args: should be \"{0} {1} " +
                                                                                        "-file true ?options? path fileName\"",
                                                                                        this.Name, subCommand);

                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = slaveInterpreter.EvaluateScript(
                                                                                        arguments, argumentIndex + 1, ref result);
                                                                                }
                                                                            }

                                                                            if (code == ReturnCode.Error)
                                                                                Engine.AddErrorInformation(interpreter, result,
                                                                                    String.Format("{0}    (in debug secureeval \"{1}\" script line {2})",
                                                                                        Environment.NewLine, path, Interpreter.GetErrorLine(slaveInterpreter)));
                                                                        }
                                                                        finally
                                                                        {
                                                                            if (added)
                                                                            {
                                                                                slaveInterpreter.ContextEngineFlags &= ~EngineFlags.IgnoreHidden;
                                                                                added = false;
                                                                            }
                                                                        }
                                                                    }
                                                                    finally
                                                                    {
                                                                        if (trusted)
                                                                            slaveInterpreter.InternalMarkSafe();
                                                                    }
                                                                }
                                                                finally
                                                                {
                                                                    if (!events)
                                                                    {
                                                                        /* IGNORED */
                                                                        EventOps.RestoreEnabled(
                                                                            slaveInterpreter, savedEnabled);

                                                                        savedEnabled = 0;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "could not lock interpreter";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            slaveInterpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                                                        }

                                                        //
                                                        // NOTE: Pop the original call frame that we pushed above and
                                                        //       any intervening scope call frames that may be leftover
                                                        //       (i.e. they were not explicitly closed).
                                                        //
                                                        /* IGNORED */
                                                        interpreter.PopScopeCallFramesAndOneMore();
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
                                                        result = "wrong # args: should be \"debug secureeval ?options? path arg ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug secureeval ?options? path arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "self":
                                    {
                                        //
                                        // NOTE: The default is "break when debugger is attached".
                                        //
                                        bool debug = DebugOps.IsAttached();

                                        //
                                        // NOTE: Purposely relaxed argument count checking...
                                        //
                                        if (arguments.Count >= 3)
                                            code = Value.GetBoolean2(
                                                arguments[2], ValueFlags.AnyBoolean,
                                                interpreter.CultureInfo, ref debug, ref result);

                                        bool force = false; // NOTE: Break for release builds?

                                        //
                                        // NOTE: Purposely relaxed argument count checking...
                                        //
                                        if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                            code = Value.GetBoolean2(
                                                arguments[3], ValueFlags.AnyBoolean,
                                                interpreter.CultureInfo, ref force, ref result);

                                        if ((code == ReturnCode.Ok) && debug)
                                            DebugOps.Break(interpreter, null, force);

                                        break;
                                    }
                                case "setup":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 8))
                                        {
#if DEBUGGER
                                            bool setup = !Engine.CheckDebugger(interpreter, true);

                                            if (arguments.Count >= 3)
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref setup, ref result);

                                            bool isolated = false; /* TODO: Good default? */

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                                code = Value.GetBoolean2(
                                                    arguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref isolated, ref result);

                                            CreateFlags createFlags = interpreter.CreateFlags; /* TODO: Good default? */
                                            InitializeFlags initializeFlags = interpreter.InitializeFlags; /* TODO: Good default? */
                                            ScriptFlags scriptFlags = interpreter.ScriptFlags; /* TODO: Good default? */
                                            InterpreterFlags interpreterFlags = interpreter.InterpreterFlags; /* TODO: Good default? */

                                            //
                                            // NOTE: Remove flags that we are handling specially
                                            //       -OR- that we know will cause problems and
                                            //       add the ones we know are generally required.
                                            //
                                            createFlags &= ~CreateFlags.ThrowOnError;
                                            createFlags &= ~CreateFlags.DebuggerInterpreter;
                                            createFlags |= CreateFlags.Initialize;

                                            if (isolated)
                                                createFlags |= CreateFlags.DebuggerInterpreter;

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 5))
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(CreateFlags),
                                                    createFlags.ToString(),
                                                    arguments[4], interpreter.CultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is CreateFlags)
                                                    createFlags = (CreateFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 6))
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(InitializeFlags),
                                                    initializeFlags.ToString(),
                                                    arguments[5], interpreter.CultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is InitializeFlags)
                                                    initializeFlags = (InitializeFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 7))
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(ScriptFlags),
                                                    scriptFlags.ToString(),
                                                    arguments[6], interpreter.CultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is ScriptFlags)
                                                    scriptFlags = (ScriptFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if ((code == ReturnCode.Ok) && (arguments.Count >= 8))
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(InterpreterFlags),
                                                    scriptFlags.ToString(),
                                                    arguments[6], interpreter.CultureInfo,
                                                    true, true, true, ref result);

                                                if (enumValue is InterpreterFlags)
                                                    interpreterFlags = (InterpreterFlags)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // TODO: Make it possible to disable copying the library
                                                //       path and/or the auto-path here?
                                                //
                                                if (Engine.SetupDebugger(
                                                        interpreter, interpreter.CultureName,
                                                        createFlags, initializeFlags, scriptFlags,
                                                        interpreterFlags, interpreter.GetAppDomain(),
                                                        interpreter.Host,
                                                        DebuggerOps.GetLibraryPath(interpreter),
                                                        DebuggerOps.GetAutoPathList(interpreter),
                                                        false, setup, isolated, ref result))
                                                {
                                                    result = String.Empty;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug setup ?create? ?isolated? ?createFlags? ?initializeFlags? ?scriptFlags? ?interpreterFlags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "shell":
                                    {
                                        if (arguments.Count >= 2)
                                        {
#if SHELL
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveInterpreterValue, Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-initialize", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-loop", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-asynchronous", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                Variant value = null;
                                                Interpreter localInterpreter = interpreter;

                                                if (options.IsPresent("-interpreter", ref value))
                                                    localInterpreter = (Interpreter)value.Value;

                                                bool initialize = false;

                                                if (options.IsPresent("-initialize", ref value))
                                                    initialize = (bool)value.Value;

                                                bool loop = false;

                                                if (options.IsPresent("-loop", ref value))
                                                    loop = (bool)value.Value;

                                                bool asynchronous = false;

                                                if (options.IsPresent("-asynchronous", ref value))
                                                    asynchronous = (bool)value.Value;

                                                //
                                                // NOTE: Pass the remaining arguments, if any, to
                                                //       the [nested] shell.  If there are no more
                                                //       arguments, pass null.
                                                //
                                                IEnumerable<string> args = (argumentIndex != Index.Invalid) ?
                                                    ArgumentList.GetRangeAsStringList(arguments, argumentIndex) : null;

                                                if (asynchronous)
                                                {
                                                    Thread thread = ShellOps.CreateShellMainThread(args, true);

                                                    if (thread != null)
                                                    {
                                                        result = String.Empty;
                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        result = "failed to create shell thread";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    code = ResultOps.ExitCodeToReturnCode(
                                                        Interpreter.ShellMainCore(localInterpreter,
                                                        null, clientData, args, initialize, loop,
                                                        ref result));
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug shell ?options? ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "stack":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool force = false;

                                            if (arguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref force, ref result);

#if NATIVE && WINDOWS
                                            if ((code == ReturnCode.Ok) && force)
                                            {
                                                code = RuntimeOps.CheckForStackSpace(
                                                    interpreter, Engine.GetExtraStackSpace());

                                                if (code != ReturnCode.Ok)
                                                    result = "stack check failed";
                                            }
#endif

                                            if (code == ReturnCode.Ok)
                                            {
                                                UIntPtr used = UIntPtr.Zero;
                                                UIntPtr allocated = UIntPtr.Zero;
                                                UIntPtr extra = UIntPtr.Zero;
                                                UIntPtr margin = UIntPtr.Zero;
                                                UIntPtr maximum = UIntPtr.Zero;
                                                UIntPtr reserve = UIntPtr.Zero;
                                                UIntPtr commit = UIntPtr.Zero;

                                                code = RuntimeOps.GetStackSize(
                                                    ref used, ref allocated,
                                                    ref extra, ref margin,
                                                    ref maximum, ref reserve,
                                                    ref commit, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = StringList.MakeList(
                                                        "used", used,
                                                        "allocated", allocated,
                                                        "extra", extra,
                                                        "margin", margin,
                                                        "maximum", maximum,
                                                        "reserve", reserve,
                                                        "commit", commit);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug stack ?force?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "status":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;
                                            bool enabled = false;
                                            HeaderFlags headerFlags = HeaderFlags.None;
                                            Interpreter debugInterpreter = null;

                                            /* IGNORED */
                                            Engine.CheckDebugger(interpreter, true, ref debugger,
                                                ref enabled, ref headerFlags, ref debugInterpreter);

                                            StringList list = new StringList();

                                            if (debugger != null)
                                                list.Add("debugger available");
                                            else
                                                list.Add("debugger not available");

                                            if ((debugger != null) && enabled)
                                                list.Add("debugger enabled");
                                            else
                                                list.Add("debugger not enabled");

                                            list.Add(String.Format(
                                                "header flags are \"{0}\"",
                                                headerFlags));

                                            if (debugInterpreter != null)
                                                list.Add("debugger interpreter available");
                                            else
                                                list.Add("debugger interpreter not available");

                                            result = list;
                                            code = ReturnCode.Ok;
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug status\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "step":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                //
                                                // NOTE: Default is toggle of current value.
                                                //
                                                bool enabled = !debugger.SingleStep;

                                                if (arguments.Count == 3)
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (interpreter.Interactive)
                                                    {
                                                        debugger.SingleStep = enabled;

                                                        IInteractiveHost interactiveHost = interpreter.Host;

                                                        if (interactiveHost != null)
                                                            /* IGNORED */
                                                            interactiveHost.WriteResultLine(
                                                                ReturnCode.Ok, "single step " +
                                                                ConversionOps.ToEnabled(debugger.SingleStep));

                                                        result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "cannot {0} single step",
                                                            ConversionOps.ToEnable(enabled));

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug step ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "steps":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    long steps = 0;

                                                    code = Value.GetWideInteger2(
                                                        (IGetValue)arguments[2], ValueFlags.AnyWideInteger,
                                                        interpreter.CultureInfo, ref steps, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (interpreter.Interactive)
                                                        {
                                                            debugger.Steps = steps;
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "cannot break after {0} steps",
                                                                steps);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = debugger.Steps;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug steps ?integer?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "subst":
                                    {
                                        if (arguments.Count >= 3)
                                        {
#if DEBUGGER
                                            Interpreter debugInterpreter = null;

                                            if (Engine.CheckDebuggerInterpreter(interpreter, false,
                                                    ref debugInterpreter, ref result))
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nobackslashes", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocommands", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-novariables", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                                });

                                                int argumentIndex = Index.Invalid;

                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        ((argumentIndex + 1) == arguments.Count))
                                                    {
                                                        SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;

                                                        if (options.IsPresent("-nobackslashes"))
                                                            substitutionFlags &= ~SubstitutionFlags.Backslashes;

                                                        if (options.IsPresent("-nocommands"))
                                                            substitutionFlags &= ~SubstitutionFlags.Commands;

                                                        if (options.IsPresent("-novariables"))
                                                            substitutionFlags &= ~SubstitutionFlags.Variables;

                                                        string name = StringList.MakeList("debug subst");

                                                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Substitute | CallFrameFlags.Debugger);

                                                        interpreter.PushAutomaticCallFrame(frame);

                                                        code = debugInterpreter.SubstituteString(
                                                            arguments[argumentIndex], substitutionFlags, ref result);

                                                        if (code == ReturnCode.Error)
                                                            Engine.AddErrorInformation(interpreter, result,
                                                                String.Format("{0}    (in debug subst script line {1})",
                                                                    Environment.NewLine, Interpreter.GetErrorLine(debugInterpreter)));

                                                        //
                                                        // NOTE: Pop the original call frame that we pushed above and
                                                        //       any intervening scope call frames that may be leftover
                                                        //       (i.e. they were not explicitly closed).
                                                        //
                                                        /* IGNORED */
                                                        interpreter.PopScopeCallFramesAndOneMore();
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
                                                            result = "wrong # args: should be \"debug subst ?-nobackslashes? ?-nocommands? ?-novariables? string\"";
                                                        }

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug subst ?-nobackslashes? ?-nocommands? ?-novariables? string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "suspend":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, true,
                                                    ref debugger, ref result))
                                            {
                                                code = debugger.Suspend(ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = "debugger suspended";
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug suspend\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sysmemory":
                                    {
                                        if (arguments.Count == 2)
                                        {
#if NATIVE
                                            StringList list = null;

                                            if (NativeOps.GetMemoryStatus(
                                                    ref list, ref result) == ReturnCode.Ok)
                                            {
                                                result = list;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug sysmemory\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "test":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
#if DEBUGGER
                                            if (arguments.Count >= 3)
                                            {
                                                string name = arguments[2];

                                                if (arguments.Count >= 4)
                                                {
                                                    bool enabled = false;

                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref enabled, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (interpreter.SetTestBreakpoint(
                                                                name, enabled, ref result))
                                                        {
                                                            result = String.Format(
                                                                "test \"{0}\" breakpoint is now {1}",
                                                                name, ConversionOps.ToEnabled(enabled));
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "failed to {0} test \"{1}\" breakpoint: {2}",
                                                                ConversionOps.ToEnable(enabled), name, result);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "test \"{0}\" breakpoint is {1}",
                                                        name, ConversionOps.ToEnabled(
                                                            interpreter.HasTestBreakpoint(name)));

                                                    code = ReturnCode.Ok;
                                                }
                                            }
                                            else
                                            {
                                                code = interpreter.TestBreakpointsToString(ref result);
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug test ?name? ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "testpath":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                                interpreter.TestPath = arguments[2];

                                            result = interpreter.TestPath;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug testpath ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "token":
                                    {
                                        if ((arguments.Count == 5) || (arguments.Count == 6))
                                        {
#if DEBUGGER && BREAKPOINTS
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                int startLine = 0;

                                                code = Value.GetInteger2(
                                                    (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                    interpreter.CultureInfo, ref startLine, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    int endLine = 0;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[4], ValueFlags.AnyInteger,
                                                        interpreter.CultureInfo, ref endLine, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IScriptLocation location = ScriptLocation.Create(
                                                            interpreter, arguments[2], startLine, endLine, false);

                                                        if (arguments.Count == 6)
                                                        {
                                                            bool enabled = false;

                                                            code = Value.GetBoolean2(
                                                                arguments[5], ValueFlags.AnyBoolean,
                                                                interpreter.CultureInfo, ref enabled, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                bool match = false;

                                                                if (enabled)
                                                                {
                                                                    code = debugger.SetBreakpoint(
                                                                        interpreter, location, ref match, ref result);
                                                                }
                                                                else
                                                                {
                                                                    code = debugger.ClearBreakpoint(
                                                                        interpreter, location, ref match, ref result);
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                    result = String.Format(
                                                                        "token \"{0}\" breakpoint {1} {2}",
                                                                        location, match ? "was already" : "is now",
                                                                        ConversionOps.ToEnabled(enabled));
                                                            }
                                                        }
                                                        else
                                                        {
                                                            bool match = false;

                                                            code = debugger.MatchBreakpoint(
                                                                interpreter, location, ref match, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = String.Format(
                                                                    "token \"{0}\" breakpoint is {1}",
                                                                    location, ConversionOps.ToEnabled(match));
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug token fileName startLine endLine ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "trace":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-console", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-debug", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-raw", null),
                                                new Option(typeof(TracePriority), OptionFlags.MustHaveEnumValue, Index.Invalid,
                                                    Index.Invalid, "-priority", new Variant(TraceOps.GetTracePriority())),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-category", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    TracePriority priority = TraceOps.GetTracePriority();

                                                    if (options.IsPresent("-priority", ref value))
                                                        priority = (TracePriority)value.Value;

                                                    bool console = false;

                                                    if (options.IsPresent("-console"))
                                                        console = true;

                                                    bool debug = false;

                                                    if (options.IsPresent("-debug"))
                                                        debug = true;

                                                    bool raw = false;

                                                    if (options.IsPresent("-raw"))
                                                        raw = true;

                                                    string category = DebugOps.DefaultCategory;

                                                    if (options.IsPresent("-category", ref value))
                                                        category = value.ToString();

                                                    if (debug)
                                                    {
                                                        if (console)
                                                        {
                                                            code = DebugOps.EnsureTraceListener(
                                                                DebugOps.GetDebugListeners(),
                                                                DebugOps.NewDefaultTraceListener(true),
                                                                ref result);
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (raw)
                                                            {
                                                                /* EXEMPT */
                                                                DebugOps.DebugWrite(
                                                                    arguments[argumentIndex], category);
                                                            }
                                                            else
                                                            {
                                                                TraceOps.DebugWriteTo(
                                                                    interpreter, arguments[argumentIndex],
                                                                    true);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (console)
                                                        {
                                                            code = DebugOps.EnsureTraceListener(
                                                                DebugOps.GetTraceListeners(),
                                                                DebugOps.NewDefaultTraceListener(true),
                                                                ref result);
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (raw)
                                                            {
                                                                /* EXEMPT */
                                                                DebugOps.TraceWrite(
                                                                    arguments[argumentIndex], category);
                                                            }
                                                            else
                                                            {
                                                                TraceOps.DebugTrace(
                                                                    arguments[argumentIndex], category,
                                                                    priority);
                                                            }
                                                        }
                                                    }

                                                    result = String.Empty;
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
                                                        result = "wrong # args: should be \"debug trace ?options? message\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug trace ?options? message\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "types":
                                    {
                                        //
                                        // debug types ?types?
                                        //
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if DEBUGGER
                                            IDebugger debugger = null;

                                            if (Engine.CheckDebugger(interpreter, false,
                                                    ref debugger, ref result))
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    object enumValue = EnumOps.TryParseFlagsEnum(
                                                        interpreter, typeof(BreakpointType),
                                                        debugger.Types.ToString(),
                                                        arguments[2], interpreter.CultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is BreakpointType)
                                                        debugger.Types = (BreakpointType)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = debugger.Types;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug types ?types?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "undelete":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                ICallFrame variableFrame = null;

                                                code = interpreter.GetVariableFrameViaResolvers(
                                                    LookupFlags.Default, ref variableFrame,
                                                    ref pattern, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (variableFrame != null)
                                                    {
                                                        VariableDictionary variables = variableFrame.Variables;

                                                        if (variables != null)
                                                        {
                                                            int count = variables.SetUndefined(
                                                                interpreter, pattern, false);

                                                            result = String.Format(
                                                                "undeleted {0} {1} in call frame {2}",
                                                                count, (count != 1) ? "variables" :
                                                                "variable", variableFrame.Name);
                                                        }
                                                        else
                                                        {
                                                            result = "call frame does not support variables";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "invalid call frame";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug undelete ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "variable":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-searches", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-elements", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-links", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-empty", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IHost host = interpreter.Host;

                                                    if (host != null)
                                                    {
                                                        _Hosts.Default defaultHost = host as _Hosts.Default;

                                                        if (defaultHost != null)
                                                        {
                                                            DetailFlags detailFlags = DetailFlags.Default;

                                                            if (options.IsPresent("-searches"))
                                                                detailFlags |= DetailFlags.VariableSearches;

                                                            if (options.IsPresent("-elements"))
                                                                detailFlags |= DetailFlags.VariableElements;

                                                            if (options.IsPresent("-links"))
                                                                detailFlags |= DetailFlags.VariableLinks;

                                                            if (options.IsPresent("-empty"))
                                                                detailFlags |= DetailFlags.EmptyContent;

                                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                VariableFlags flags = VariableFlags.NoElement;
                                                                IVariable variable = null;

                                                                code = interpreter.GetVariableViaResolversWithSplit(
                                                                    arguments[argumentIndex], ref flags, ref variable,
                                                                    ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    StringPairList list = null;

                                                                    if (defaultHost.BuildLinkedVariableInfoList(
                                                                            interpreter, variable, detailFlags,
                                                                            ref list))
                                                                    {
                                                                        result = list;
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "could not introspect variable";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "interpreter host does not have variable support";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
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
                                                        result = "wrong # args: should be \"debug variable ?options? varName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug variable ?options? varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vout":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string channelId = StandardChannel.Output;

                                            if ((arguments.Count >= 3) &&
                                                !String.IsNullOrEmpty(arguments[2]))
                                            {
                                                channelId = arguments[2];
                                            }

                                            if (arguments.Count >= 4)
                                            {
                                                bool enabled = false;

                                                code = Value.GetBoolean2(
                                                    arguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref enabled, ref result);

                                                if (code == ReturnCode.Ok)
                                                    code = interpreter.SetChannelVirtualOutput(
                                                        channelId, enabled, ref result);
                                            }
                                            else
                                            {
                                                code = interpreter.GetChannelVirtualOutput(
                                                    channelId, ref result);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug vout ?channelId? ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "watch":
                                    {
                                        //
                                        // debug watch ?varName? ?types?
                                        //
                                        //       Also, new syntax will be either 2 or 3 arguments exactly
                                        //       (just like "debug types").
                                        //
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            //
                                            // BUGFIX: The debugger is not required to setup variable watches;
                                            //         however, they will not actually fire if the debugger is
                                            //         not available.
                                            //
                                            if (arguments.Count == 2)
                                            {
                                                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                {
                                                    //
                                                    // NOTE: Return a list of all watched variables for
                                                    //       the current call frame.
                                                    //
                                                    ICallFrame variableFrame = null;
                                                    VariableFlags flags = VariableFlags.None;

                                                    if (interpreter.GetVariableFrameViaResolvers(
                                                            LookupFlags.Default, ref variableFrame,
                                                            ref flags, ref result) == ReturnCode.Ok)
                                                    {
                                                        if (variableFrame != null)
                                                        {
                                                            VariableDictionary variables = variableFrame.Variables;

                                                            if (variables != null)
                                                                result = variables.GetWatchpoints();
                                                            else
                                                                result = String.Empty;
                                                        }
                                                        else
                                                        {
                                                            result = String.Empty;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Empty;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                {
                                                    VariableFlags flags = VariableFlags.NoElement;
                                                    IVariable variable = null;

                                                    code = interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (EntityOps.IsLink(variable))
                                                            variable = EntityOps.FollowLinks(variable, flags);

                                                        if (arguments.Count == 4)
                                                        {
                                                            object enumValue = EnumOps.TryParseFlagsEnum(
                                                                interpreter, typeof(VariableFlags),
                                                                EntityOps.GetWatchpointFlags(variable.Flags).ToString(),
                                                                arguments[3], interpreter.CultureInfo, true, true, true,
                                                                ref result);

                                                            if (enumValue is VariableFlags)
                                                            {
                                                                VariableFlags watchFlags = (VariableFlags)enumValue;

                                                                //
                                                                // NOTE: Next, reset all the watch related variable
                                                                //       flags for this variable, masking off any
                                                                //       variable flags that are not watch related
                                                                //       from the newly supplied variable flags.
                                                                //
                                                                variable.Flags = EntityOps.SetWatchpointFlags(
                                                                    variable.Flags, watchFlags);
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: Finally, return the [potentially changed]
                                                            //       watch flags on the variable.
                                                            //
                                                            StringList list = new StringList();

                                                            if (EntityOps.IsBreakOnGet(variable))
                                                                list.Add(VariableFlags.BreakOnGet.ToString());

                                                            if (EntityOps.IsBreakOnSet(variable))
                                                                list.Add(VariableFlags.BreakOnSet.ToString());

                                                            if (EntityOps.IsBreakOnUnset(variable))
                                                                list.Add(VariableFlags.BreakOnUnset.ToString());

                                                            if (EntityOps.IsMutable(variable))
                                                                list.Add(VariableFlags.Mutable.ToString());

                                                            if (list.Count == 0)
                                                                list.Add(VariableFlags.None.ToString());

                                                            result = list;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"debug watch ?varName? ?types?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"debug option ?arg ...?\"";
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
