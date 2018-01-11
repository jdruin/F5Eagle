/*
 * Interp.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("f83b2063-cf1f-428f-9cb9-7a1862a69960")]
    //
    // TODO: Make this command "safe".  The main thing that needs to be done is
    //       to audit the code for security and make sure any state changes are
    //       isolated to the current interpreter or one of its slaves.
    //
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Interp : Core
    {
        public Interp(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "alias", "aliases", "bgerror", "cancel", "create",
            "delete", "enabled", "eval", "exists", "expose", "expr",
            "finallytimeout", "hide", "hidden", "immutable", "invokehidden",
            "issafe", "isstandard", "makesafe", "makestandard", "marktrusted",
            "nopolicy", "policy", "queue", "readonly", "readorgetscriptfile",
            "readylimit", "recursionlimit", "resetcancel", "resultlimit",
            "service", "set", "shareinterp", "shareobject", "slaves",
            "sleeptime", "subcommand", "subst", "target", "timeout", "unset",
            "watchdog"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private readonly EnsembleDictionary allowedSubCommands = new EnsembleDictionary(
            PolicyOps.DefaultAllowedInterpSubCommandNames);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                            switch (subCommand)
                            {
                                case "alias":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            Interpreter sourceInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                arguments[2], LookupFlags.Interpreter, false,
                                                ref sourceInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string sourceName = arguments[3];
                                                string sourceCommandName = ScriptOps.MakeCommandName(sourceName);

                                                if (arguments.Count == 4)
                                                {
                                                    //
                                                    // NOTE: Return the alias definition.
                                                    //
                                                    IAlias alias = null;

                                                    code = sourceInterpreter.GetAlias(
                                                        sourceName, LookupFlags.Default,
                                                        ref alias, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = alias.ToString();
                                                }
                                                else if ((arguments.Count == 5) &&
                                                    String.IsNullOrEmpty(arguments[4]))
                                                {
                                                    //
                                                    // NOTE: Delete the alias definition.
                                                    //
                                                    code = sourceInterpreter.RemoveAliasAndCommand(
                                                        sourceName, clientData, false, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else if (arguments.Count > 5)
                                                {
                                                    Interpreter targetInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        arguments[4], LookupFlags.Interpreter, false,
                                                        ref targetInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string targetName = arguments[5];

                                                        if ((arguments.Count == 6) &&
                                                            String.IsNullOrEmpty(targetName))
                                                        {
                                                            //
                                                            // NOTE: Delete the alias definition.
                                                            //
                                                            code = sourceInterpreter.RemoveAliasAndCommand(
                                                                sourceName, clientData, false, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = String.Empty;
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: Create the alias definition.
                                                            //
                                                            if ((sourceInterpreter.DoesIExecuteExist(sourceCommandName) != ReturnCode.Ok) ||
                                                                (sourceInterpreter.RemoveIExecute(sourceCommandName, clientData, ref result) == ReturnCode.Ok))
                                                            {
                                                                if ((sourceInterpreter.DoesProcedureExist(sourceCommandName) != ReturnCode.Ok) ||
                                                                    (sourceInterpreter.RemoveProcedure(sourceCommandName, clientData, ref result) == ReturnCode.Ok))
                                                                {
                                                                    if ((sourceInterpreter.DoesCommandExist(sourceCommandName) != ReturnCode.Ok) ||
                                                                        (sourceInterpreter.RemoveCommand(sourceCommandName, clientData, ref result) == ReturnCode.Ok))
                                                                    {
                                                                        ArgumentList targetArguments = new ArgumentList(targetName);

                                                                        if (arguments.Count > 6)
                                                                            targetArguments.AddRange(ArgumentList.GetRange(arguments, 6));

                                                                        IAlias alias = null;

                                                                        code = sourceInterpreter.AddAlias(
                                                                            sourceName, CommandFlags.None, AliasFlags.SkipSourceName | AliasFlags.CrossCommandAlias,
                                                                            clientData, targetInterpreter, null, targetArguments, null, 0, ref alias, ref result);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    goto aliasArgs;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            goto aliasArgs;
                                        }
                                        break;

                                    aliasArgs:
                                        result = "wrong # args: should be \"interp alias slavePath slaveCmd ?masterPath masterCmd? ?arg ...?\"";
                                        code = ReturnCode.Error;
                                        break;
                                    }
                                case "aliases":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 5))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            string pattern = (arguments.Count >= 4) ?
                                                (string)arguments[3] : null;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool all = false; /* TODO: Good default? */

                                                if (arguments.Count >= 5)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[4], ValueFlags.AnyBoolean,
                                                        slaveInterpreter.CultureInfo, ref all,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    StringList list = null;

                                                    code = slaveInterpreter.ListAliases(
                                                        pattern, false, all, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp aliases ?path? ?pattern? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "bgerror":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (slaveInterpreter.SyncRoot) /* TRANSACTIONAL */
                                                {
                                                    if (arguments.Count == 4)
                                                    {
                                                        StringList list = null;

                                                        code = Parser.SplitList(
                                                            slaveInterpreter, arguments[3], 0,
                                                            Length.Invalid, true, ref list,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            slaveInterpreter.BackgroundError = list.ToString();
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                        result = slaveInterpreter.BackgroundError;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp bgerror path ?cmdPrefix?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cancel":
                                    {
                                        OptionDictionary options = new OptionDictionary(
                                            new IOption[] {
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-unwind", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                        });

                                        int argumentIndex = Index.Invalid;

                                        if (arguments.Count > 2)
                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                        else
                                            code = ReturnCode.Ok;

                                        if (code == ReturnCode.Ok)
                                        {
                                            string path = null;
                                            Result cancelResult = null;

                                            if (argumentIndex != Index.Invalid)
                                            {
                                                if ((argumentIndex + 2) >= arguments.Count)
                                                {
                                                    //
                                                    // NOTE: Grab the name of the interpreter.
                                                    //
                                                    path = arguments[argumentIndex];

                                                    //
                                                    // NOTE: The cancel result is just after the interpreter.
                                                    //
                                                    if ((argumentIndex + 1) < arguments.Count)
                                                        cancelResult = arguments[argumentIndex + 1];
                                                }
                                                else
                                                {
                                                    if (Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp cancel ?-unwind? ?--? ?path? ?result?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                path = String.Empty;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                Interpreter slaveInterpreter = null;

                                                code = interpreter.GetNestedSlaveInterpreter(
                                                    path, LookupFlags.Interpreter, false,
                                                    ref slaveInterpreter, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    CancelFlags cancelFlags = CancelFlags.InterpCancel;

                                                    if (options.IsPresent("-unwind"))
                                                        cancelFlags |= CancelFlags.Unwind;

                                                    code = Engine.CancelEvaluate(
                                                        slaveInterpreter, cancelResult, cancelFlags,
                                                        ref result);
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case "create":
                                    {
                                        OptionDictionary options = new OptionDictionary(
                                            new IOption[] {
                                            new Option(typeof(CreationFlagTypes), OptionFlags.Unsafe | OptionFlags.MustHaveEnumValue,
                                                Index.Invalid, Index.Invalid, "-creationflagtypes", new Variant(CreationFlagTypes.Default)),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-namespaces", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonamespaces", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-novariables", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noinitialize", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-alias", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-safe", null),
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-standard", null),
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-unsafeinitialize", null),
#if APPDOMAINS && ISOLATED_INTERPRETERS
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-isolated", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-isolated", null),
#endif
#if DEBUGGER
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-debug", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-debug", null),
#endif
#if NOTIFY && NOTIFY_ARGUMENTS
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-trace", null),
#else
                                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-trace", null),
#endif
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-security", null),
                                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nosecurity", null),
                                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                        });

                                        int argumentIndex = Index.Invalid;

                                        if (arguments.Count > 2)
                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                        else
                                            code = ReturnCode.Ok;

                                        if (code == ReturnCode.Ok)
                                        {
                                            if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                            {
                                                if (interpreter.HasSlaveInterpreters(ref result))
                                                {
                                                    string path = null;
                                                    string name = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        path = arguments[argumentIndex];

                                                    if ((path != null) &&
                                                        (interpreter.DoesSlaveInterpreterExist(path, true, ref name) == ReturnCode.Ok))
                                                    {
                                                        result = String.Format(
                                                            "interpreter named \"{0}\" already exists, cannot create",
                                                            name);

                                                        code = ReturnCode.Error;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Variant value = null;
                                                        CreationFlagTypes creationFlagTypes = CreationFlagTypes.Default;

                                                        if (options.IsPresent("-creationflagtypes", ref value))
                                                            creationFlagTypes = (CreationFlagTypes)value.Value;

                                                        //
                                                        // HACK: Inherit the default "use namespaces" setting
                                                        //       from the master interpreter.
                                                        //
                                                        bool namespaces = interpreter.AreNamespacesEnabled();

                                                        if (options.IsPresent("-namespaces"))
                                                            namespaces = true;

                                                        if (options.IsPresent("-nonamespaces"))
                                                            namespaces = false;

                                                        bool initialize = true;

                                                        if (options.IsPresent("-noinitialize"))
                                                            initialize = false;

                                                        bool variables = true;

                                                        if (options.IsPresent("-novariables"))
                                                            variables = false;

                                                        bool safe = interpreter.IsSafe();

                                                        if (options.IsPresent("-safe"))
                                                            safe = true;

                                                        bool alias = false;

                                                        if (options.IsPresent("-alias"))
                                                            alias = true;

                                                        bool standard = false;

                                                        if (options.IsPresent("-standard"))
                                                            standard = true;

                                                        bool unsafeInitialize = false;

                                                        if (options.IsPresent("-unsafeinitialize"))
                                                            unsafeInitialize = true;

                                                        bool isolated = false;

#if APPDOMAINS && ISOLATED_INTERPRETERS
                                                        if (options.IsPresent("-isolated"))
                                                            isolated = true;
#endif

#if DEBUGGER
                                                        bool debug = false;

                                                        if (options.IsPresent("-debug"))
                                                            debug = true;
#endif

#if NOTIFY && NOTIFY_ARGUMENTS
                                                        bool trace = false;

                                                        if (options.IsPresent("-trace"))
                                                            trace = true;
#endif

                                                        bool security = interpreter.HasSecurity();

                                                        if (options.IsPresent("-security"))
                                                            security = true;

                                                        if (options.IsPresent("-nosecurity"))
                                                            security = false;

                                                        CreateFlags createFlags;

                                                        if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.CurrentCreateFlags, true))
                                                            createFlags = interpreter.CreateFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.DefaultCreateFlags, true))
                                                            createFlags = interpreter.DefaultCreateFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.FallbackCreateFlags, true))
                                                            createFlags = CreateFlags.NestedUse;
                                                        else
                                                            createFlags = CreateFlags.None;

                                                        InitializeFlags initializeFlags;

                                                        if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.CurrentInitializeFlags, true))
                                                            initializeFlags = interpreter.InitializeFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.DefaultInitializeFlags, true))
                                                            initializeFlags = interpreter.DefaultInitializeFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.FallbackInitializeFlags, true))
                                                            initializeFlags = InitializeFlags.Default;
                                                        else
                                                            initializeFlags = InitializeFlags.None;

                                                        ScriptFlags scriptFlags;

                                                        if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.CurrentScriptFlags, true))
                                                            scriptFlags = interpreter.ScriptFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.DefaultScriptFlags, true))
                                                            scriptFlags = interpreter.DefaultScriptFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.FallbackScriptFlags, true))
                                                            scriptFlags = ScriptFlags.Default;
                                                        else
                                                            scriptFlags = ScriptFlags.None;

                                                        InterpreterFlags interpreterFlags;

                                                        if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.CurrentInterpreterFlags, true))
                                                            interpreterFlags = interpreter.InterpreterFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.DefaultInterpreterFlags, true))
                                                            interpreterFlags = interpreter.DefaultInterpreterFlags;
                                                        else if (FlagOps.HasFlags(creationFlagTypes, CreationFlagTypes.FallbackInterpreterFlags, true))
                                                            interpreterFlags = InterpreterFlags.Default;
                                                        else
                                                            interpreterFlags = InterpreterFlags.None;

                                                        //
                                                        // NOTE: Enable full namespace support?
                                                        //
                                                        if (namespaces)
                                                            createFlags |= CreateFlags.UseNamespaces;
                                                        else
                                                            createFlags &= ~CreateFlags.UseNamespaces;

                                                        //
                                                        // NOTE: Set the built-in variables?
                                                        //
                                                        if (variables)
                                                            createFlags &= ~CreateFlags.NoVariables;
                                                        else
                                                            createFlags |= CreateFlags.NoVariables;

                                                        //
                                                        // NOTE: Initialize the script library?
                                                        //
                                                        if (initialize)
                                                            createFlags |= CreateFlags.Initialize;
                                                        else
                                                            createFlags &= ~CreateFlags.Initialize;

                                                        //
                                                        // NOTE: Are we creating a safe interpreter?  If so, make
                                                        //       sure the "full initialize" option is not present,
                                                        //       then disable evaluating "init.eagle" and evaluate
                                                        //       "safe.eagle" instead.
                                                        //
                                                        if (safe)
                                                        {
                                                            createFlags |= CreateFlags.SafeAndHideUnsafe;

                                                            if (!unsafeInitialize)
                                                            {
                                                                initializeFlags &= ~InitializeFlags.Initialization;
                                                                initializeFlags |= InitializeFlags.Safe;
                                                            }
                                                        }

                                                        if (standard)
                                                            createFlags |= CreateFlags.StandardAndHideNonStandard;

#if DEBUGGER
                                                        //
                                                        // NOTE: Do we want a script debugger?
                                                        //
                                                        if (debug)
                                                            createFlags |= CreateFlags.DebuggerUse;
#endif

#if NOTIFY && NOTIFY_ARGUMENTS
                                                        //
                                                        // NOTE: Do we want to enable the trace plugin?
                                                        //
                                                        if (trace)
                                                            createFlags &= ~CreateFlags.NoTracePlugin;
#endif

                                                        code = interpreter.CreateSlaveInterpreter(
                                                            path, clientData, createFlags, initializeFlags,
                                                            scriptFlags, interpreterFlags, isolated, security,
                                                            alias, ref result);
                                                    }
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "wrong # args: should be \"interp create ?-safe? ?--? ?path?\"";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        break;
                                    }
                                case "delete":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            if (interpreter.HasSlaveInterpreters(ref result))
                                            {
                                                for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                                {
                                                    code = interpreter.DeleteSlaveInterpreter(
                                                        arguments[argumentIndex], clientData,
                                                        ObjectOps.GetDefaultSynchronous(),
                                                        ref result);

                                                    if (code != ReturnCode.Ok)
                                                        break;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp delete ?path ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "enabled":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool enabled = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        slaveInterpreter.CultureInfo, ref enabled,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.Enabled = enabled;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.Enabled;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp enabled ?path? ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string name = StringList.MakeList("interp eval", path);

                                                ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                    CallFrameFlags.Evaluate | CallFrameFlags.Restricted);

                                                interpreter.PushAutomaticCallFrame(frame);

                                                if (arguments.Count == 4)
                                                    code = slaveInterpreter.EvaluateScript(arguments[3], ref result);
                                                else
                                                    code = slaveInterpreter.EvaluateScript(arguments, 3, ref result);

                                                if (code == ReturnCode.Error)
                                                    Engine.AddErrorInformation(interpreter, result,
                                                        String.Format("{0}    (in interp eval \"{1}\" script line {2})",
                                                            Environment.NewLine, path, Interpreter.GetErrorLine(slaveInterpreter)));

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
                                            result = "wrong # args: should be \"interp eval path arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            result = (interpreter.DoesSlaveInterpreterExist(path) == ReturnCode.Ok);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp exists ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "expose":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    code = slaveInterpreter.ExposeCommand(arguments[3], ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot expose commands";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp expose path hiddenCmdName ?cmdName?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "expr":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string name = StringList.MakeList("interp expr", path);

                                                ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                    CallFrameFlags.Expression | CallFrameFlags.Restricted);

                                                interpreter.PushAutomaticCallFrame(frame);

                                                //
                                                // FIXME: The expression parser does not know the line where
                                                //        the error happened unless it evaluates a command
                                                //        contained within the expression.
                                                //
                                                Interpreter.SetErrorLine(slaveInterpreter, 0);

                                                if (arguments.Count == 4)
                                                    code = slaveInterpreter.EvaluateExpression(arguments[3], ref result);
                                                else
                                                    code = slaveInterpreter.EvaluateExpression(arguments, 3, ref result);

                                                if (code == ReturnCode.Error)
                                                    Engine.AddErrorInformation(interpreter, result,
                                                        String.Format("{0}    (in interp expr \"{1}\" script line {2})",
                                                            Environment.NewLine, path, Interpreter.GetErrorLine(slaveInterpreter)));

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
                                            result = "wrong # args: should be \"interp expr path arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "finallytimeout":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count >= 4)
                                                {
                                                    int timeout = _Timeout.None;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        slaveInterpreter.CultureInfo, ref timeout,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.FinallyTimeout = timeout;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.FinallyTimeout;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp finallytimeout ?path? ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "hide":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    code = slaveInterpreter.HideCommand(arguments[3], ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot hide commands";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp hide path cmdName ?hiddenCmdName?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "hidden":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                StringList list = null;

                                                code = slaveInterpreter.HiddenCommandsToList(
                                                    CommandFlags.Hidden, CommandFlags.None, false,
                                                    false, null, false, ref list, ref result);

                                                if (code == ReturnCode.Ok)
                                                    code = slaveInterpreter.HiddenProceduresToList(
                                                        ProcedureFlags.Hidden, ProcedureFlags.None, false,
                                                        false, null, false, ref list, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = list;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp hidden path\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "immutable":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool immutable = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        slaveInterpreter.CultureInfo, ref immutable, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.Immutable = immutable;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.Immutable;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp immutable ?path? ?immutable?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "invokehidden":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid,
                                                        Index.Invalid, "-global", null),
                                                    new Option(null, OptionFlags.MustHaveAbsoluteNamespaceValue,
                                                        Index.Invalid, Index.Invalid, "-namespace", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid,
                                                        Index.Invalid, Option.EndOfOptions, null)
                                                });

                                                int argumentIndex = Index.Invalid;

                                                code = slaveInterpreter.GetOptions(
                                                    options, arguments, 0, 3, Index.Invalid, true,
                                                    ref argumentIndex, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        bool global = false;

                                                        if (options.IsPresent("-global"))
                                                            global = true;

                                                        Variant value = null;
                                                        INamespace @namespace = null;

                                                        if (options.IsPresent("-namespace", ref value))
                                                            @namespace = (INamespace)value.Value;

                                                        string executeName = arguments[argumentIndex];
                                                        IExecute execute = null;

                                                        code = slaveInterpreter.GetIExecuteViaResolvers(
                                                            slaveInterpreter.GetResolveEngineFlags(true) |
                                                            EngineFlags.UseHidden, executeName, null,
                                                            LookupFlags.Default, ref execute, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: Figure out the arguments for the command to
                                                            //       be executed.
                                                            //
                                                            ArgumentList executeArguments =
                                                                ArgumentList.GetRange(arguments, argumentIndex);

                                                            //
                                                            // NOTE: Create and push a new call frame to track the
                                                            //       activation of this alias.
                                                            //
                                                            string name = StringList.MakeList(
                                                                "interp invokehidden", executeName);

                                                            ICallFrame frame = interpreter.NewTrackingCallFrame(
                                                                name, CallFrameFlags.Interpreter);

                                                            interpreter.PushAutomaticCallFrame(frame);

                                                            ICallFrame nestedFrame = null;

                                                            if (!global && (@namespace != null))
                                                            {
                                                                string nestedName = StringList.MakeList(
                                                                    "namespace eval", @namespace.QualifiedName);

                                                                nestedFrame = slaveInterpreter.NewNamespaceCallFrame(
                                                                    nestedName, CallFrameFlags.Evaluate |
                                                                    CallFrameFlags.UseNamespace, null, @namespace,
                                                                    false);

                                                                slaveInterpreter.PushNamespaceCallFrame(nestedFrame);
                                                            }

                                                            //
                                                            // NOTE: Execute the command in the global scope?
                                                            //
                                                            if (global)
                                                                slaveInterpreter.PushGlobalCallFrame(true);
                                                            else if (nestedFrame != null)
                                                                slaveInterpreter.PushNamespaceCallFrame(nestedFrame);

                                                            try
                                                            {
                                                                //
                                                                // NOTE: Save the current engine flags and then enable
                                                                //       the external execution flags.
                                                                //
                                                                EngineFlags savedEngineFlags =
                                                                    slaveInterpreter.BeginExternalExecution();

                                                                try
                                                                {
                                                                    //
                                                                    // NOTE: Execute the hidden command now.
                                                                    //
                                                                    code = slaveInterpreter.ExecuteHidden(
                                                                        executeName, execute, clientData, executeArguments,
                                                                        ref result);
                                                                }
                                                                finally
                                                                {
                                                                    //
                                                                    // NOTE: Restore the saved engine flags, masking off the
                                                                    //       external execution flags as necessary.
                                                                    //
                                                                    /* IGNORED */
                                                                    slaveInterpreter.EndAndCleanupExternalExecution(
                                                                        savedEngineFlags);
                                                                }
                                                            }
                                                            finally
                                                            {
                                                                //
                                                                // NOTE: If we previously pushed the global call frame
                                                                //       (above), we also need to pop any leftover scope
                                                                //       call frames now; otherwise, the call stack will
                                                                //       be imbalanced.
                                                                //
                                                                if (global)
                                                                    slaveInterpreter.PopGlobalCallFrame(true);
                                                                else if (nestedFrame != null)
                                                                    /* IGNORED */
                                                                    slaveInterpreter.PopNamespaceCallFrame(nestedFrame);
                                                            }

                                                            //
                                                            // NOTE: Pop the original call frame that we pushed above
                                                            //       and any intervening scope call frames that may be
                                                            //       leftover (i.e. they were not explicitly closed).
                                                            //
                                                            /* IGNORED */
                                                            interpreter.PopScopeCallFramesAndOneMore();
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp invokehidden path ?options? cmd ?arg ..?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp invokehidden path ?options? cmd ?arg ..?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "issafe":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = slaveInterpreter.IsSafe();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp issafe ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isstandard":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = slaveInterpreter.IsStandard();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp isstandard ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "makesafe":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 5))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    bool safe = true;

                                                    if (arguments.Count >= 4)
                                                    {
                                                        code = Value.GetBoolean2(
                                                            arguments[3], ValueFlags.AnyBoolean,
                                                            slaveInterpreter.CultureInfo,
                                                            ref safe, ref result);
                                                    }

                                                    MakeFlags makeFlags = MakeFlags.SafeLibrary;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (arguments.Count >= 5)
                                                        {
                                                            object enumValue = EnumOps.TryParseFlagsEnum(
                                                                slaveInterpreter, typeof(MakeFlags),
                                                                makeFlags.ToString(), arguments[4],
                                                                slaveInterpreter.CultureInfo, true, true,
                                                                true, ref result);

                                                            if (enumValue is MakeFlags)
                                                                makeFlags = (MakeFlags)enumValue;
                                                            else
                                                                code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (safe != slaveInterpreter.IsSafe())
                                                        {
                                                            code = slaveInterpreter.MakeSafe(
                                                                makeFlags, safe, ref result);
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "interpreter is already marked as \"{0}\"",
                                                                slaveInterpreter.IsSafe() ?
                                                                    "safe" : "unsafe");

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot modify safety";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp makesafe ?path? ?safe? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "makestandard":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 5))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    bool standard = true;

                                                    if (arguments.Count >= 4)
                                                    {
                                                        code = Value.GetBoolean2(
                                                            arguments[3], ValueFlags.AnyBoolean,
                                                            slaveInterpreter.CultureInfo,
                                                            ref standard, ref result);
                                                    }

                                                    MakeFlags makeFlags = MakeFlags.StandardLibrary;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (arguments.Count >= 5)
                                                        {
                                                            object enumValue = EnumOps.TryParseFlagsEnum(
                                                                slaveInterpreter, typeof(MakeFlags),
                                                                makeFlags.ToString(), arguments[4],
                                                                slaveInterpreter.CultureInfo, true, true,
                                                                true, ref result);

                                                            if (enumValue is MakeFlags)
                                                                makeFlags = (MakeFlags)enumValue;
                                                            else
                                                                code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (standard != slaveInterpreter.IsStandard())
                                                        {
                                                            code = slaveInterpreter.MakeStandard(
                                                                makeFlags, standard, ref result);
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "interpreter is already marked as \"{0}\"",
                                                                slaveInterpreter.IsStandard() ?
                                                                    "standard" : "non-standard");

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot modify standardization";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp makestandard ?path? ?standard? ?all?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "marktrusted":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    code = slaveInterpreter.MarkTrusted(ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot mark trusted";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp marktrusted path\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "nopolicy":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    code = slaveInterpreter.RemovePolicy(
                                                        arguments[3], clientData, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot remove policy";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp nopolicy path name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "policy":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid, Index.Invalid, "-type", null),
                                                new Option(null, OptionFlags.MustHaveWideIntegerValue, Index.Invalid, Index.Invalid, "-token", null),
                                                new Option(typeof(PolicyFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(PolicyFlags.Script)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    Type type = null;

                                                    if (options.IsPresent("-type", ref value))
                                                        type = (Type)value.Value;

                                                    long token = 0;

                                                    if (options.IsPresent("-token", ref value))
                                                        token = (long)value.Value;

                                                    PolicyFlags flags = PolicyFlags.Script;

                                                    if (options.IsPresent("-flags", ref value))
                                                        flags = (PolicyFlags)value.Value;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter slaveInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref slaveInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!interpreter.IsSafe())
                                                        {
                                                            if ((type != null) || (token != 0))
                                                            {
                                                                IPlugin plugin = slaveInterpreter.GetCorePlugin(ref result);

                                                                if (plugin != null)
                                                                {
                                                                    code = slaveInterpreter.AddScriptPolicy(
                                                                        flags, type, token, interpreter,
                                                                        arguments[argumentIndex + 1],
                                                                        plugin, clientData, ref result);
                                                                }
                                                                else
                                                                {
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "option \"-type\" or \"-token\" must be specified";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "permission denied: safe interpreter cannot add policy";
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
                                                        result = "wrong # args: should be \"interp policy ?options? path script\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp policy ?options? path script\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "queue":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveDateTimeValue, Index.Invalid, Index.Invalid, "-when", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    Variant value = null;
                                                    DateTime dateTime = TimeOps.GetUtcNow();

                                                    if (options.IsPresent("-when", ref value))
                                                        dateTime = (DateTime)value.Value;

                                                    string path = arguments[2];
                                                    Interpreter slaveInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref slaveInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string text = null;

                                                        if ((argumentIndex + 1) == arguments.Count)
                                                            text = arguments[argumentIndex];
                                                        else
                                                            text = ListOps.Concat(arguments, argumentIndex);

                                                        code = slaveInterpreter.QueueScript(dateTime, text, ref result);
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"interp queue path ?options? arg ?arg ...?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp queue path ?options? arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readonly":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool readOnly = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        slaveInterpreter.CultureInfo, ref readOnly, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.ReadOnly = readOnly;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.ReadOnly;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp readonly ?path? ?readonly?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readorgetscriptfile":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            ScriptFlags oldScriptFlags = ScriptOps.GetFlags(
                                                interpreter, interpreter.ScriptFlags, true);

                                            EngineFlags oldEngineFlags = interpreter.EngineFlags;

                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] { 
                                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                                    Index.Invalid, Index.Invalid, "-encoding", null),
                                                new Option(typeof(ScriptFlags),
                                                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-scriptflags",
                                                    new Variant(oldScriptFlags)),
                                                new Option(typeof(EngineFlags),
                                                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                                                    Index.Invalid, Index.Invalid, "-engineflags",
                                                    new Variant(oldEngineFlags)),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    Encoding encoding = null;

                                                    if (options.IsPresent("-encoding", ref value))
                                                        encoding = (Encoding)value.Value;

                                                    ScriptFlags newScriptFlags = oldScriptFlags;

                                                    if (options.IsPresent("-scriptflags", ref value))
                                                        newScriptFlags = (ScriptFlags)value.Value;

                                                    EngineFlags newEngineFlags = oldEngineFlags;

                                                    if (options.IsPresent("-engineflags", ref value))
                                                        newEngineFlags = (EngineFlags)value.Value;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter slaveInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref slaveInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string fileName = arguments[argumentIndex + 1];

                                                        if (!String.IsNullOrEmpty(fileName))
                                                        {
                                                            EngineFlags engineFlags = Engine.CombineFlagsWithMasks(
                                                                oldEngineFlags, newEngineFlags,
                                                                EngineFlags.ReadOrGetScriptFileMask,
                                                                EngineFlags.ReadOrGetScriptFileMask
                                                            );

                                                            SubstitutionFlags substitutionFlags = interpreter.SubstitutionFlags;
                                                            EventFlags eventFlags = interpreter.EngineEventFlags;
                                                            ExpressionFlags expressionFlags = interpreter.ExpressionFlags;
                                                            string text = null;

                                                            code = Engine.ReadOrGetScriptFile(
                                                                interpreter, encoding, ref newScriptFlags, ref fileName,
                                                                ref engineFlags, ref substitutionFlags, ref eventFlags,
                                                                ref expressionFlags, ref text, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = text;
                                                        }
                                                        else
                                                        {
                                                            result = "invalid file name";
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
                                                        result = "wrong # args: should be \"interp readorgetscriptfile ?options? path fileName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp readorgetscriptfile ?options? path fileName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readylimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                int readyLimit = 0;

                                                if (arguments.Count == 4)
                                                {
                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        slaveInterpreter.CultureInfo, ref readyLimit,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.ReadyLimit = readyLimit;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.ReadyLimit;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp readylimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "recursionlimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                int recursionLimit = 0;

                                                if (arguments.Count == 4)
                                                {
                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        slaveInterpreter.CultureInfo, ref recursionLimit,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.RecursionLimit = recursionLimit;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.RecursionLimit;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp recursionlimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resetcancel":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 3)
                                                code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    CancelFlags cancelFlags = CancelFlags.InterpResetCancel;

                                                    if (options.IsPresent("-force"))
                                                        cancelFlags |= CancelFlags.IgnorePending;

                                                    string path = arguments[2];
                                                    Interpreter slaveInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref slaveInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // NOTE: This really only works for the current interpreter
                                                        //       if the cancel was not specified with the unwind
                                                        //       flag; otherwise, this command will never actually
                                                        //       get a chance to execute.  For interpreters other
                                                        //       than the current interpreter, this will always
                                                        //       "just work".
                                                        //
                                                        bool reset = false;

                                                        code = Engine.ResetCancel(
                                                            slaveInterpreter, cancelFlags, ref reset, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = reset;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"interp resetcancel path ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp resetcancel path ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resultlimit":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
#if RESULT_LIMITS
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                {
                                                    int resultLimit = 0;

                                                    if (arguments.Count == 4)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            slaveInterpreter.CultureInfo, ref resultLimit,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            slaveInterpreter.InternalExecuteResultLimit = resultLimit;
                                                            slaveInterpreter.InternalNestedResultLimit = resultLimit;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = StringList.MakeList(
                                                            "execute", slaveInterpreter.InternalExecuteResultLimit,
                                                            "nested", slaveInterpreter.InternalNestedResultLimit);
                                                    }
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp resultlimit path ?limit?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "service":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                OptionDictionary options = new OptionDictionary(
                                                    new IOption[] {
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-dedicated", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocancel", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-erroronempty", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-userinterface", null),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocomplain", null),
                                                    new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-limit", null),
                                                    new Option(typeof(EventFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-eventflags",
                                                        new Variant(slaveInterpreter.ServiceEventFlags)),
                                                    new Option(typeof(EventPriority), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-priority",
                                                        new Variant(EventPriority.Service)),
                                                    new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                                });

                                                int argumentIndex = Index.Invalid;

                                                if (arguments.Count > 3)
                                                    code = interpreter.GetOptions(options, arguments, 0, 3, Index.Invalid, true, ref argumentIndex, ref result);
                                                else
                                                    code = ReturnCode.Ok;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (argumentIndex == Index.Invalid)
                                                    {
                                                        bool dedicated = false;

                                                        if (options.IsPresent("-dedicated"))
                                                            dedicated = true;

                                                        bool noCancel = false;

                                                        if (options.IsPresent("-nocancel"))
                                                            noCancel = true;

                                                        bool stopOnError = true;

                                                        if (options.IsPresent("-nocomplain"))
                                                            stopOnError = false;

                                                        bool errorOnEmpty = false;

                                                        if (options.IsPresent("-erroronempty"))
                                                            errorOnEmpty = true;

                                                        bool userInterface = false;

                                                        if (options.IsPresent("-userinterface"))
                                                            userInterface = true;

                                                        Variant value = null;
                                                        int limit = 0;

                                                        if (options.IsPresent("-limit", ref value))
                                                            limit = (int)value.Value;

                                                        EventFlags eventFlags = slaveInterpreter.ServiceEventFlags;

                                                        if (options.IsPresent("-eventflags", ref value))
                                                            eventFlags = (EventFlags)value.Value;

                                                        EventPriority priority = EventPriority.Service;

                                                        if (options.IsPresent("-priority", ref value))
                                                            priority = (EventPriority)value.Value;

                                                        if (dedicated)
                                                        {
                                                            try
                                                            {
                                                                if (Engine.QueueWorkItem(slaveInterpreter,
                                                                        EventManager.ServiceEventsThreadStart, slaveInterpreter))
                                                                {
                                                                    result = String.Empty;
                                                                    code = ReturnCode.Ok;
                                                                }
                                                                else
                                                                {
                                                                    result = "failed to queue event servicing work item";
                                                                    code = ReturnCode.Error;
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
                                                            IEventManager eventManager = slaveInterpreter.EventManager;

                                                            if (EventOps.ManagerIsOk(eventManager))
                                                            {
                                                                code = eventManager.ServiceEvents(
                                                                    eventFlags, priority, limit, noCancel, stopOnError,
                                                                    errorOnEmpty, userInterface, ref result);
                                                            }
                                                            else
                                                            {
                                                                result = "event manager not available";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp service path ?options?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp service path ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "set":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Result value;

                                                if (arguments.Count == 4)
                                                {
                                                    value = null;

                                                    code = slaveInterpreter.GetVariableValue(
                                                        VariableFlags.None, arguments[3], ref value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = value;
                                                }
                                                else if (arguments.Count == 5)
                                                {
                                                    value = arguments[4];

                                                    code = slaveInterpreter.SetVariableValue(
                                                        VariableFlags.None, arguments[3], value, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = value;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp set interp varName ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "shareinterp":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    IObject @object = null;

                                                    code = interpreter.GetObject(
                                                        arguments[3], LookupFlags.Default, ref @object,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Interpreter otherInterpreter = (@object != null) ?
                                                            @object.Value as Interpreter : null;

                                                        if (otherInterpreter != null)
                                                        {
                                                            //
                                                            // NOTE: The interpreter is not owned by us and
                                                            //       the opaque object handle should really
                                                            //       be marked as NoDispose, if it has not
                                                            //       been already.
                                                            //
                                                            @object.ObjectFlags |= ObjectFlags.NoDispose;

                                                            //
                                                            // NOTE: Also, mark the interpreter itself as
                                                            //       shared, to prevent its eventual disposal
                                                            //       in the DisposeSlaveInterpreters method.
                                                            //       This flag will NOT prevent any other
                                                            //       code from disposing of this interpreter,
                                                            //       including from within the interpreter
                                                            //       itself.
                                                            //
                                                            otherInterpreter.SetShared();

                                                            //
                                                            // NOTE: Add the other (now shared) interpreter
                                                            //       to the specified interpreter as a slave.
                                                            //
                                                            string otherId =
                                                                GlobalState.NextInterpreterId().ToString();

                                                            code = slaveInterpreter.AddSlaveInterpreter(
                                                                otherId, otherInterpreter, clientData,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = otherId;
                                                        }
                                                        else
                                                        {
                                                            result = "invalid interpreter";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot share interpreters";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp shareinterp interp objectName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "shareobject":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (!interpreter.IsSafe())
                                                {
                                                    IObject @object = null;

                                                    code = interpreter.GetObject(
                                                        arguments[3], LookupFlags.Default, ref @object,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // HACK: Add the object handle verbatim to the "safe"
                                                        //       slave interpreter.  It should be noted that
                                                        //       the object flags are NOT changed by this
                                                        //       sub-command; therefore, it will be unusable
                                                        //       by the "safe" interpreter until its flags are
                                                        //       manually adjusted in the master interpreter.
                                                        //
                                                        long token = 0;

                                                        code = slaveInterpreter.AddSharedObject(
                                                            ObjectData.CreateForSharing(interpreter,
                                                            slaveInterpreter, @object
#if DEBUGGER && DEBUGGER_ARGUMENTS
                                                            , new ArgumentList(arguments)
#endif
                                                            ), clientData, null, @object.Value,
                                                            ref token, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = token;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "permission denied: safe interpreter cannot share objects";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp shareobject interp objectName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "slaves":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (slaveInterpreter.HasSlaveInterpreters(ref result))
                                                {
                                                    result = slaveInterpreter.SlaveInterpretersToString(null, false);
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp slaves ?path?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sleeptime":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count >= 4)
                                                {
                                                    int sleepTime = 0;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        slaveInterpreter.CultureInfo, ref sleepTime,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.SleepTime = sleepTime;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.SleepTime;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp sleeptime ?path? ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "subcommand":
                                    {
                                        if (arguments.Count >= 5)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(SubCommandFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(SubCommandFlags.Default)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 3) <= arguments.Count) &&
                                                    ((argumentIndex + 4) >= arguments.Count))
                                                {
                                                    Variant value = null;
                                                    SubCommandFlags subCommandFlags = SubCommandFlags.Default;

                                                    if (options.IsPresent("-flags", ref value))
                                                        subCommandFlags = (SubCommandFlags)value.Value;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter slaveInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref slaveInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (!interpreter.IsSafe())
                                                        {
                                                            lock (slaveInterpreter.SyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                StringList list = null;

                                                                if ((argumentIndex + 4) == arguments.Count)
                                                                {
                                                                    code = Parser.SplitList(
                                                                        slaveInterpreter, arguments[argumentIndex + 3],
                                                                        0, Length.Invalid, true, ref list, ref result);
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    string commandName = ScriptOps.MakeCommandName(
                                                                        arguments[argumentIndex + 1]);

                                                                    ICommand command = null;

                                                                    code = slaveInterpreter.GetCommand(
                                                                        commandName, LookupFlags.Default,
                                                                        ref command, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        EnsembleDictionary subCommands = PolicyOps.GetSubCommandsUnsafe(
                                                                            command); /* ALREADY UNSAFE */

                                                                        if (subCommands != null)
                                                                        {
                                                                            string subCommandName = arguments[argumentIndex + 2];
                                                                            ISubCommand localSubCommand;

                                                                            if (!FlagOps.HasFlags(subCommandFlags,
                                                                                    SubCommandFlags.ForceQuery, true) &&
                                                                                (arguments.Count >= 6))
                                                                            {
                                                                                bool exists = subCommands.ContainsKey(subCommandName); /* EXEMPT */

                                                                                if ((list != null) && (list.Count > 0))
                                                                                {
                                                                                    if (!exists || !FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.ForceNew, true))
                                                                                    {
                                                                                        localSubCommand = ScriptOps.NewCommandSubCommand(
                                                                                            subCommandName, null, command, list, subCommandFlags);

                                                                                        subCommands[subCommandName] = localSubCommand;
                                                                                        result = localSubCommand.ToString();
                                                                                    }
                                                                                    else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.NoComplain, true))
                                                                                    {
                                                                                        result = "can't add new sub-command: already exists";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                }
                                                                                else if (FlagOps.HasFlags(subCommandFlags,
                                                                                        SubCommandFlags.ForceDelete, true))
                                                                                {
                                                                                    if (exists && subCommands.Remove(subCommandName))
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                    else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.NoComplain, true))
                                                                                    {
                                                                                        result = "can't remove sub-command: doesn't exist";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (exists || FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.ForceReset, true))
                                                                                    {
                                                                                        subCommands[subCommandName] = null;
                                                                                        result = String.Empty;
                                                                                    }
                                                                                    else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                            SubCommandFlags.NoComplain, true))
                                                                                    {
                                                                                        result = "can't reset sub-command: doesn't exist";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Empty;
                                                                                    }
                                                                                }
                                                                            }
                                                                            else if (subCommands.TryGetValue(
                                                                                    subCommandName, out localSubCommand))
                                                                            {
                                                                                result = (localSubCommand != null) ?
                                                                                    localSubCommand.ToString() :
                                                                                    String.Empty;
                                                                            }
                                                                            else if (!FlagOps.HasFlags(subCommandFlags,
                                                                                    SubCommandFlags.NoComplain, true))
                                                                            {
                                                                                result = "can't query sub-command: doesn't exist";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                            else
                                                                            {
                                                                                result = String.Empty;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "sub-commands not available";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "permission denied: safe interpreter cannot manage sub-commands";
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
                                                        result = "wrong # args: should be \"interp subcommand ?options? path cmdName subCmdName ?command?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp subcommand ?options? path cmdName subCmdName ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "subst":
                                    {
                                        if (arguments.Count >= 4)
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
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;

                                                    if (options.IsPresent("-nobackslashes"))
                                                        substitutionFlags &= ~SubstitutionFlags.Backslashes;

                                                    if (options.IsPresent("-nocommands"))
                                                        substitutionFlags &= ~SubstitutionFlags.Commands;

                                                    if (options.IsPresent("-novariables"))
                                                        substitutionFlags &= ~SubstitutionFlags.Variables;

                                                    string path = arguments[argumentIndex];
                                                    Interpreter slaveInterpreter = null;

                                                    code = interpreter.GetNestedSlaveInterpreter(
                                                        path, LookupFlags.Interpreter, false,
                                                        ref slaveInterpreter, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string name = StringList.MakeList("interp subst", path);

                                                        ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Substitute | CallFrameFlags.Restricted);

                                                        interpreter.PushAutomaticCallFrame(frame);

                                                        code = slaveInterpreter.SubstituteString(
                                                            arguments[argumentIndex + 1], substitutionFlags, ref result);

                                                        if (code == ReturnCode.Error)
                                                            Engine.AddErrorInformation(interpreter, result,
                                                                String.Format("{0}    (in interp subst \"{1}\" script line {2})",
                                                                    Environment.NewLine, path, Interpreter.GetErrorLine(slaveInterpreter)));

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
                                                        result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"interp subst ?options? path string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp subst ?options? path string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "target":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string sourceName = arguments[3];
                                                IAlias alias = null;

                                                code = slaveInterpreter.GetAlias(
                                                    sourceName, LookupFlags.Default,
                                                    ref alias, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = interpreter.GetInterpreterPath(
                                                        alias.TargetInterpreter, ref result);

                                                    if (code != ReturnCode.Ok)
                                                        result = String.Format(
                                                            "target interpreter for alias \"{0}\" " +
                                                            "in path \"{1}\" is not my descendant",
                                                            sourceName, path);
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: Modify the error message (COMPAT: Tcl).
                                                    //
                                                    result = String.Format(
                                                        "alias \"{0}\" in path \"{1}\" not found",
                                                        sourceName, path);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp target path alias\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "timeout":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count >= 4)
                                                {
                                                    int timeout = _Timeout.None;

                                                    code = Value.GetInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                        slaveInterpreter.CultureInfo, ref timeout,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        slaveInterpreter.Timeout = timeout;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = slaveInterpreter.Timeout;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp timeout ?path? ?newValue?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unset":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            string path = arguments[2];
                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                code = interpreter.UnsetVariable(
                                                    VariableFlags.NoRemove, arguments[3], ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp unset interp varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "watchdog":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string path = (arguments.Count >= 3) ?
                                                (string)arguments[2] : String.Empty;

                                            Interpreter slaveInterpreter = null;

                                            code = interpreter.GetNestedSlaveInterpreter(
                                                path, LookupFlags.Interpreter, false,
                                                ref slaveInterpreter, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool boolValue = false;

                                                if (arguments.Count >= 4)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        slaveInterpreter.CultureInfo, ref boolValue, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (boolValue)
                                                        {
                                                            code = Interpreter.StartTimeoutThread(
                                                                slaveInterpreter, null, false, true, true,
                                                                ref result);
                                                        }
                                                        else
                                                        {
                                                            code = Interpreter.InterruptTimeoutThread(
                                                                slaveInterpreter, null, true, ref result);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    boolValue = Interpreter.HasTimeoutThread(slaveInterpreter);
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = boolValue;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"interp watchdog ?path? ?enabled?\"";
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
                        result = "wrong # args: should be \"interp cmd ?arg ...?\"";
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
