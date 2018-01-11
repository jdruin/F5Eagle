/*
 * Scope.cs --
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
    [ObjectId("39023a46-960b-48bc-9139-55d6a2416f50")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("variable")]
    internal sealed class Scope : Core
    {
        #region Public Constructors
        public Scope(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "close", "create", "current", "destroy", "eval",
            "exists", "global", "list", "open", "set",
            "unset", "vars"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
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
                                case "close":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool all = false;

                                                    if (options.IsPresent("-all"))
                                                        all = true;

                                                    string name = null;

                                                    if (argumentIndex != Index.Invalid)
                                                        name = arguments[argumentIndex];

                                                    if (interpreter.HasScopes(ref result))
                                                    {
                                                        ICallFrame frame = null;

                                                        if (all)
                                                        {
                                                            /* IGNORED */
                                                            interpreter.PopScopeCallFrames(ref frame);

                                                            if (frame == null)
                                                            {
                                                                result = "no scopes are open";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = interpreter.GetScopeCallFrame(
                                                                name, LookupFlags.Default, true, false,
                                                                ref frame, ref result);
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                            result = frame.Name;
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
                                                        result = "wrong # args: should be \"scope close ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope close ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "create":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-args", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-clone", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-global", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-open", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-procedure", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-shared", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-strict", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-fast", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool args = false;

                                                    if (options.IsPresent("-args"))
                                                        args = true;

                                                    bool clone = false;

                                                    if (options.IsPresent("-clone"))
                                                        clone = true;

                                                    bool global = false;

                                                    if (options.IsPresent("-global"))
                                                        global = true;

                                                    bool open = false;

                                                    if (options.IsPresent("-open"))
                                                        open = true;

                                                    bool procedure = false;

                                                    if (options.IsPresent("-procedure"))
                                                        procedure = true;

                                                    bool shared = false;

                                                    if (options.IsPresent("-shared"))
                                                        shared = true;

                                                    bool strict = false;

                                                    if (options.IsPresent("-strict"))
                                                        strict = true;

                                                    bool fast = false;

                                                    if (options.IsPresent("-fast"))
                                                        fast = true;

                                                    string name = null;

                                                    if (procedure)
                                                    {
                                                        if (argumentIndex != Index.Invalid)
                                                        {
                                                            result = "cannot specify scope name with -procedure";
                                                            code = ReturnCode.Error;
                                                        }
                                                        else
                                                        {
                                                            ICallFrame variableFrame = null;

                                                            code = interpreter.GetVariableFrameViaResolvers(
                                                                LookupFlags.Default, ref variableFrame, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if ((variableFrame != null) &&
                                                                    FlagOps.HasFlags(variableFrame.Flags,
                                                                        CallFrameFlags.Procedure, true))
                                                                {
                                                                    name = CallFrameOps.GetAutomaticScopeName(
                                                                        variableFrame, shared);
                                                                }
                                                                else
                                                                {
                                                                    result = "no procedure frame available";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (argumentIndex != Index.Invalid)
                                                    {
                                                        //
                                                        // NOTE: Use the name specified by the caller.
                                                        //
                                                        name = arguments[argumentIndex];
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Use an automatically generated name.
                                                        //
                                                        name = FormatOps.Id("scope", null, interpreter.NextId());
                                                    }

                                                    if ((code == ReturnCode.Ok) && !String.IsNullOrEmpty(name))
                                                    {
                                                        bool created = false;
                                                        ICallFrame frame = null;

                                                        if (interpreter.GetScope(
                                                                name, LookupFlags.NoVerbose, ref frame) != ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: We intend to modify the interpreter state,
                                                            //       make sure this is not forbidden.
                                                            //
                                                            if (interpreter.IsModifiable(true, ref result))
                                                            {
                                                                //
                                                                // NOTE: Make sure that the scopes collection is available.
                                                                //
                                                                if (interpreter.HasScopes(ref result))
                                                                {
                                                                    VariableDictionary newVariables = null;

                                                                    //
                                                                    // NOTE: Clone the variables from the current call frame
                                                                    //       or start with no variables?
                                                                    //
                                                                    if (clone)
                                                                    {
                                                                        //
                                                                        // BUGFIX: Grab the actual current variable frame.
                                                                        //
                                                                        ICallFrame variableFrame = global ?
                                                                            interpreter.CurrentGlobalFrame : interpreter.CurrentFrame;

                                                                        code = interpreter.GetVariableFrameViaResolvers(
                                                                            LookupFlags.Default, ref variableFrame, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            if (variableFrame != null)
                                                                            {
                                                                                VariableDictionary variables = variableFrame.Variables;

                                                                                if (variables != null)
                                                                                {
                                                                                    //
                                                                                    // NOTE: Create a new call frame with the variables from
                                                                                    //       the current variable call frame.
                                                                                    //
                                                                                    newVariables = new VariableDictionary(variables);

                                                                                    //
                                                                                    // NOTE: We do not want to mess with the parent frames of
                                                                                    //       global variables, mostly because of the global
                                                                                    //       traces that we use there.
                                                                                    //
                                                                                    if (!interpreter.IsGlobalCallFrame(variableFrame))
                                                                                    {
                                                                                        //
                                                                                        // BUGFIX: *HACK* Re-parent all the variables to be in
                                                                                        //         the scope call frame.
                                                                                        //
                                                                                        foreach (IVariable variable in newVariables.Values)
                                                                                        {
                                                                                            //
                                                                                            // NOTE: Double check that the variable is valid and
                                                                                            //       is really not a global variable (i.e. through
                                                                                            //       a frame that transparently links to the variables
                                                                                            //       of the parent frame, like [source]).
                                                                                            //
                                                                                            if ((variable != null) &&
                                                                                                !interpreter.IsGlobalCallFrame(variable.Frame))
                                                                                            {
                                                                                                variable.Frame = frame;
                                                                                            }
                                                                                        }
                                                                                    }
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
                                                                    else
                                                                    {
                                                                        newVariables = new VariableDictionary();
                                                                    }

                                                                    //
                                                                    // NOTE: Make sure the frame resolution and/or cloing above was
                                                                    //       successful.
                                                                    //
                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        //
                                                                        // NOTE: Create the new scope call frame with the variables
                                                                        //       and an empty list of arguments (which are provided
                                                                        //       separately).
                                                                        //
                                                                        frame = interpreter.NewScopeCallFrame(
                                                                            name, CallFrameFlags.Scope, newVariables,
                                                                            new ArgumentList());

                                                                        created = true;

                                                                        //
                                                                        // NOTE: Setup the arguments in this scope call frame based
                                                                        //       on those in the enclosing procedure frame, if any.
                                                                        //
                                                                        if (args)
                                                                            code = interpreter.CopyProcedureArgumentsToFrame(
                                                                                frame, true, ref result);

                                                                        //
                                                                        // NOTE: If we fully created the new call frame then
                                                                        //       persist it.
                                                                        //
                                                                        if (code == ReturnCode.Ok)
                                                                            code = interpreter.AddScope(frame, clientData,
                                                                                ref result);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else if (strict)
                                                        {
                                                            result = String.Format(
                                                                "scope named \"{0}\" already exists",
                                                                name);

                                                            code = ReturnCode.Error;
                                                        }
                                                        else if (args)
                                                        {
                                                            //
                                                            // NOTE: Sync up the arguments in this scope call frame
                                                            //       with those in the enclosing procedure frame,
                                                            //       if any.
                                                            //
                                                            code = interpreter.CopyProcedureArgumentsToFrame(
                                                                frame, true, ref result);
                                                        }

                                                        //
                                                        // NOTE: If we succeeded at creating or fetching the scope,
                                                        //       continue.
                                                        //
                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: If required, make the named [possibly new] scope
                                                            //       the current scope.
                                                            //
                                                            if (open)
                                                                interpreter.PushCallFrame(frame);

                                                            //
                                                            // NOTE: Enable or disable "fast" local variable access
                                                            //       for the new scope.
                                                            //
                                                            if (created)
                                                                CallFrameOps.SetFast(frame, fast);

                                                            //
                                                            // NOTE: Return the newly created scope name.
                                                            //
                                                            result = frame.Name;
                                                        }
                                                    }
                                                    else if (code == ReturnCode.Ok)
                                                    {
                                                        result = "invalid scope name";
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
                                                        result = "wrong # args: should be \"scope create ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope create ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "current":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                ICallFrame frame = null;

                                                code = interpreter.GetScopeCallFrame(
                                                    null, LookupFlags.Default, false, false,
                                                    ref frame, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    result = frame.Name;
                                                }
                                                else
                                                {
                                                    result = String.Empty;
                                                    code = ReturnCode.Ok;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope current\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "destroy":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string name = arguments[2];

                                            code = interpreter.RemoveScope(name, clientData, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope destroy name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "eval":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2]; /* REUSED */

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    ICallFrame frame = null; /* REUSED */

                                                    code = interpreter.GetScope(
                                                        name, LookupFlags.Default, ref frame, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        interpreter.PushAutomaticCallFrame(frame); /* scope <name> */

                                                        name = StringList.MakeList("scope eval", name);

                                                        frame = interpreter.NewTrackingCallFrame(name,
                                                            CallFrameFlags.Scope | CallFrameFlags.Evaluate);

                                                        interpreter.PushAutomaticCallFrame(frame); /* scope eval */

                                                        if (arguments.Count == 4)
                                                            code = interpreter.EvaluateScript(arguments[3], ref result);
                                                        else
                                                            code = interpreter.EvaluateScript(arguments, 3, ref result);

                                                        if (code == ReturnCode.Error)
                                                            Engine.AddErrorInformation(interpreter, result,
                                                                String.Format("{0}    (in scope eval \"{1}\" script line {2})",
                                                                    Environment.NewLine, arguments[2], Interpreter.GetErrorLine(interpreter)));

                                                        //
                                                        // NOTE: Pop the original call frame that we pushed
                                                        //       above and any intervening scope call frames
                                                        //       that may be leftover (i.e. they were not
                                                        //       explicitly closed).
                                                        //
                                                        // BUGFIX: *SPECIAL* In this particular case [only],
                                                        //         the original call frame WAS ALSO a scope
                                                        //         call frame; therefore, only pop (all the)
                                                        //         scope call frames.
                                                        //
                                                        /* IGNORED */
                                                        interpreter.PopScopeCallFrames();
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope eval name arg ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    ICallFrame frame = null;

                                                    code = interpreter.GetScope(
                                                        name, LookupFlags.NoVerbose, ref frame);

                                                    if (code == ReturnCode.Ok)
                                                        result = true;
                                                    else
                                                        result = false;

                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope exists name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "global":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-unset", null),
                                                new Option(null, OptionFlags.None, Index.Invalid,
                                                    Index.Invalid, "-force", null),
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
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool force = false;

                                                    if (options.IsPresent("-force"))
                                                        force = true;

                                                    bool unset = false;

                                                    if (options.IsPresent("-unset"))
                                                        unset = true;

                                                    if (!unset || (argumentIndex == Index.Invalid))
                                                    {
                                                        if (interpreter.HasScopes(ref result))
                                                        {
                                                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                                            {
                                                                if (unset)
                                                                {
                                                                    code = interpreter.UnsetGlobalScopeCallFrame(
                                                                        !force, ref result);
                                                                }
                                                                else if (argumentIndex != Index.Invalid)
                                                                {
                                                                    ICallFrame globalScopeFrame =
                                                                        interpreter.GlobalScopeFrame;

                                                                    if (force || (globalScopeFrame == null))
                                                                    {
                                                                        code = interpreter.SetGlobalScopeCallFrame(
                                                                            arguments[argumentIndex], ref result);
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "global scope call frame already set";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    result = StringOps.GetStringFromObject(
                                                                        interpreter.GlobalScopeFrame);
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
                                                        result = "cannot specify scope name with -unset option";
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
                                                        result = "wrong # args: should be \"scope global ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope global ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "list":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.ScopesToString(pattern, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope list ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "open":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-procedure", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-shared", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-args", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) || ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool args = false;

                                                    if (options.IsPresent("-args"))
                                                        args = true;

                                                    bool procedure = false;

                                                    if (options.IsPresent("-procedure"))
                                                        procedure = true;

                                                    bool shared = false;

                                                    if (options.IsPresent("-shared"))
                                                        shared = true;

                                                    string name = null;

                                                    if (procedure)
                                                    {
                                                        if (argumentIndex != Index.Invalid)
                                                        {
                                                            result = "cannot specify scope name with -procedure";
                                                            code = ReturnCode.Error;
                                                        }
                                                        else
                                                        {
                                                            ICallFrame variableFrame = null;

                                                            code = interpreter.GetVariableFrameViaResolvers(
                                                                LookupFlags.Default, ref variableFrame, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if ((variableFrame != null) &&
                                                                    FlagOps.HasFlags(variableFrame.Flags,
                                                                        CallFrameFlags.Procedure, true))
                                                                {
                                                                    name = CallFrameOps.GetAutomaticScopeName(
                                                                        variableFrame, shared);
                                                                }
                                                                else
                                                                {
                                                                    result = "no procedure frame available";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (argumentIndex != Index.Invalid)
                                                    {
                                                        //
                                                        // NOTE: Use the name specified by the caller.
                                                        //
                                                        name = arguments[argumentIndex];
                                                    }
                                                    else
                                                    {
                                                        result = "must specify scope name or -procedure";
                                                        code = ReturnCode.Error;
                                                    }

                                                    if ((code == ReturnCode.Ok) && !String.IsNullOrEmpty(name))
                                                    {
                                                        ICallFrame frame = null;

                                                        code = interpreter.GetScope(
                                                            name, LookupFlags.Default, ref frame, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (args)
                                                                code = interpreter.CopyProcedureArgumentsToFrame(
                                                                    frame, true, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                interpreter.PushCallFrame(frame);

                                                                result = String.Empty;
                                                            }
                                                        }
                                                    }
                                                    else if (code == ReturnCode.Ok)
                                                    {
                                                        result = "invalid scope name";
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
                                                        result = "wrong # args: should be \"scope open ?options? ?name?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"scope open ?options? ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "set":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    ICallFrame frame = null;

                                                    code = interpreter.GetScope(
                                                        name, LookupFlags.Default, ref frame, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // TRICKY: Need to get/set variables from a specific
                                                        //         call frame that is not the current one.
                                                        //
                                                        if (arguments.Count == 4)
                                                        {
                                                            code = interpreter.GetVariableValue2(VariableFlags.None,
                                                                frame, arguments[3], null, ref result, ref result);
                                                        }
                                                        else
                                                        {
                                                            code = interpreter.SetVariableValue2(VariableFlags.None,
                                                                frame, arguments[3], null, arguments[4], null, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = arguments[4];
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope set name varName ?value?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unset":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    ICallFrame frame = null;

                                                    code = interpreter.GetScope(
                                                        name, LookupFlags.Default, ref frame, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // TRICKY: Need to unset variables from a specific
                                                        //         call frame that is not the current one.
                                                        //
                                                        code = interpreter.UnsetVariable2(VariableFlags.Purge,
                                                            frame, arguments[3], null, null, ref result);
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope unset name varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vars":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            if (interpreter.HasScopes(ref result))
                                            {
                                                string name = arguments[2];

                                                if (!String.IsNullOrEmpty(name))
                                                {
                                                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        ICallFrame frame = null;

                                                        code = interpreter.GetScope(
                                                            name, LookupFlags.Default, ref frame, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            string pattern = null;

                                                            if (arguments.Count == 4)
                                                            {
                                                                //
                                                                // NOTE: *OK* Cannot have namespaces within a scope.
                                                                //
                                                                pattern = ScriptOps.MakeVariableName(arguments[3]);
                                                            }

                                                            if (frame != null)
                                                            {
                                                                VariableDictionary variables = frame.Variables;

                                                                if (variables != null)
                                                                    result = variables.GetDefined(interpreter, pattern);
                                                                else
                                                                    result = String.Empty;
                                                            }
                                                            else
                                                            {
                                                                result = String.Empty;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid scope name";
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
                                            result = "wrong # args: should be \"scope vars name ?pattern?\"";
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
                        result = "wrong # args: should be \"scope option ?arg ...?\"";
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
