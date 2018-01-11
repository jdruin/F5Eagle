/*
 * Array.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("fe8bac16-d7a1-4b29-b4a1-7948ee4d9611")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Array : Core
    {
        public Array(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "anymore", "copy", "donesearch", "exists", "for",
            "foreach", "get", "lmap", "names", "nextelement",
            "random", "set", "size", "startsearch", "unset",
            "values"
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
                            switch (subCommand)
                            {
                                case "anymore":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        ArraySearchDictionary arraySearches = interpreter.ArraySearches;

                                                        if (arraySearches != null)
                                                        {
                                                            ArraySearch arraySearch;

                                                            if (arraySearches.TryGetValue(arguments[3], out arraySearch))
                                                            {
                                                                if (System.Object.ReferenceEquals(
                                                                        arraySearch.Variable, variable))
                                                                {
                                                                    result = arraySearch.AnyMore;
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "search identifier \"{0}\" isn't for variable \"{1}\"",
                                                                        arguments[3], arguments[2]);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "couldn't find search \"{0}\"",
                                                                    arguments[3]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "array searches not available";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array anymore arrayName searchId\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "copy":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-deep", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nosignal", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    bool deep = false;

                                                    if (options.IsPresent("-deep"))
                                                        deep = true;

                                                    bool noSignal = false;

                                                    if (options.IsPresent("-nosignal"))
                                                        noSignal = true;

                                                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                        IVariable variable = null;
                                                        Result localError = null;

                                                        if (interpreter.GetVariableViaResolversWithSplit(
                                                                arguments[argumentIndex], ref flags, ref variable,
                                                                ref localError) == ReturnCode.Ok)
                                                        {
                                                            if (EntityOps.IsLink(variable))
                                                                variable = EntityOps.FollowLinks(variable, flags);

                                                            if (!interpreter.IsEnvironmentVariable(variable) &&
                                                                !interpreter.IsTestsVariable(variable)
#if DATA
                                                                && !interpreter.IsDatabaseVariable(variable)
#endif
                                                                )
                                                            {
                                                                if (!EntityOps.IsUndefined(variable) &&
                                                                    EntityOps.IsArray(variable))
                                                                {
                                                                    flags = VariableFlags.ArrayCommandMask;
                                                                    IVariable variable2 = null;

                                                                    if (interpreter.GetVariableViaResolversWithSplit(
                                                                            arguments[argumentIndex + 1], ref flags,
                                                                            ref variable2) != ReturnCode.Ok)
                                                                    {
                                                                        //
                                                                        // NOTE: Grab the call frame for the variable, we'll
                                                                        //       need it several times.
                                                                        //
                                                                        ICallFrame frame = variable.Frame;

                                                                        if (frame != null)
                                                                        {
                                                                            VariableDictionary variables = frame.Variables;

                                                                            if (variables != null)
                                                                            {
                                                                                //
                                                                                // NOTE: Ok, the destination variable does not exist.
                                                                                //
                                                                                variable2 = new Variable(
                                                                                    frame, arguments[argumentIndex + 1], variable.Flags,
                                                                                    null, variable.Traces, interpreter.VariableEvent);

                                                                                interpreter.MaybeSetQualifiedName(variable2);

                                                                                object oldValue = variable.Value;
                                                                                ElementDictionary oldArrayValue = variable.ArrayValue;

                                                                                bool isSystemArray = interpreter.IsSystemArrayVariable(
                                                                                    variable);

                                                                                if (deep)
                                                                                {
                                                                                    //
                                                                                    // NOTE: Deep copy, by default, this will use
                                                                                    //       the existing non-array value, if any.
                                                                                    //       For System.Array backed variables, it
                                                                                    //       will be a new System.Array instance
                                                                                    //       containing the old data.
                                                                                    //
                                                                                    object newValue = null;

                                                                                    //
                                                                                    // NOTE: Deep copy, use a new array value with
                                                                                    //       the same elements (if available).  In
                                                                                    //       this case, we may need to reference
                                                                                    //       any contained opaque object handles.
                                                                                    //
                                                                                    ElementDictionary newArrayValue;

                                                                                    if (oldArrayValue != null)
                                                                                    {
                                                                                        newArrayValue = new ElementDictionary(
                                                                                            interpreter.VariableEvent, oldArrayValue);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        newArrayValue = new ElementDictionary(
                                                                                            interpreter.VariableEvent);
                                                                                    }

                                                                                    //
                                                                                    // NOTE: If the source as a system array, we
                                                                                    //       we need to use special handling here.
                                                                                    //
                                                                                    if (isSystemArray)
                                                                                    {
                                                                                        newValue = ArrayOps.DeepCopy(
                                                                                            oldValue as System.Array, ref result);

                                                                                        if (newValue == null)
                                                                                            code = ReturnCode.Error;
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        //
                                                                                        // HACK: We do not really know how to deal
                                                                                        //       with making a "deep" copy of this;
                                                                                        //       therefore, just use it verbatim.
                                                                                        //
                                                                                        newValue = variable.Value;
                                                                                    }

                                                                                    if ((code == ReturnCode.Ok) && !isSystemArray)
                                                                                    {
                                                                                        //
                                                                                        // BUGFIX: We *MUST* fire traces here; otherwise,
                                                                                        //         new opaque object handles could have
                                                                                        //         incorrect reference counts.
                                                                                        //
                                                                                        code = interpreter.FireTraces(
                                                                                            BreakpointType.BeforeVariableSet, flags, frame,
                                                                                            arguments[argumentIndex + 1], null, null, null,
                                                                                            newArrayValue, variable2, ref result);
                                                                                    }

                                                                                    if (code == ReturnCode.Ok)
                                                                                    {
                                                                                        //
                                                                                        // NOTE: Deep copy, use the new array value.
                                                                                        //
                                                                                        variable2.Value = newValue;
                                                                                        variable2.ArrayValue = newArrayValue;
                                                                                        variables[variable2.Name] = variable2;

                                                                                        if (!noSignal)
                                                                                            EntityOps.SignalDirty(variable, null);

                                                                                        result = String.Empty;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    //
                                                                                    // NOTE: Shallow copy, use the same array value.
                                                                                    //
                                                                                    variable2.Value = oldValue;
                                                                                    variable2.ArrayValue = oldArrayValue;
                                                                                    variables[variable2.Name] = variable2;

                                                                                    if (!noSignal)
                                                                                        EntityOps.SignalDirty(variable, null);

                                                                                    result = String.Empty;
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
                                                                    else
                                                                    {
                                                                        result = String.Format(
                                                                            "cannot copy array to \"{0}\" variable already exists",
                                                                            arguments[argumentIndex + 1]);

                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "\"{0}\" isn't an array",
                                                                        arguments[argumentIndex]);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "\"{0}\" is system array",
                                                                    arguments[2]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                            {
                                                                result = String.Format(
                                                                    "\"{0}\" isn't an array",
                                                                    arguments[argumentIndex]);

                                                                code = ReturnCode.Error;
                                                            }
                                                            else
                                                            {
                                                                result = localError;
                                                                code = ReturnCode.Error;
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
                                                        result = "wrong # args: should be \"array copy ?options? source destination\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array copy ?options? source destination\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "donesearch":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        ArraySearchDictionary arraySearches = interpreter.ArraySearches;

                                                        if (arraySearches != null)
                                                        {
                                                            ArraySearch arraySearch;

                                                            if (arraySearches.TryGetValue(arguments[3], out arraySearch))
                                                            {
                                                                if (System.Object.ReferenceEquals(arraySearch.Variable, variable))
                                                                {
                                                                    arraySearches.Remove(arguments[3]);

                                                                    result = String.Empty;
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "search identifier \"{0}\" isn't for variable \"{1}\"",
                                                                        arguments[3], arguments[2]);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "couldn't find search \"{0}\"",
                                                                    arguments[3]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "array searches not available";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array donesearch arrayName searchId\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    result = !EntityOps.IsUndefined(variable) &&
                                                        EntityOps.IsArray(variable);
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = false; // variable does not exist.
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array exists arrayName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "for":
                                    {
                                        code = ScriptOps.ArrayNamesAndValuesLoopCommand(
                                            this, subCommand, false, interpreter,
                                            clientData, arguments, ref result);

                                        break;
                                    }
                                case "foreach":
                                    {
                                        code = ScriptOps.ArrayNamesLoopCommand(
                                            this, subCommand, false, interpreter,
                                            clientData, arguments, ref result);

                                        break;
                                    }
                                case "get":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        string pattern = null;

                                                        if (arguments.Count >= 4)
                                                            pattern = arguments[3];

                                                        //
                                                        // HACK: Handle the global "env" array specially.  We must do this because
                                                        //       our global "env" array has no backing storage (unlike Tcl's) and
                                                        //       we do not have a trace operation for "get names" or "get names
                                                        //       and values".
                                                        //
                                                        if (interpreter.IsEnvironmentVariable(variable))
                                                        {
                                                            StringDictionary environment =
                                                                CommonOps.Environment.GetVariables();

                                                            if (environment != null)
                                                            {
                                                                result = environment.KeysAndValuesToString(pattern, false);
                                                            }
                                                            else
                                                            {
                                                                result = "environment variables unavailable";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else if (interpreter.IsTestsVariable(variable))
                                                        {
                                                            StringDictionary tests = interpreter.GetAllTestInformation(
                                                                true, ref result);

                                                            if (tests != null)
                                                                result = tests.KeysAndValuesToString(pattern, false);
                                                            else
                                                                code = ReturnCode.Error;
                                                        }
                                                        else if (interpreter.IsSystemArrayVariable(variable))
                                                        {
                                                            StringDictionary keysAndValues = null;

                                                            code = MarshalOps.GetArrayElementKeysAndValues(
                                                                interpreter, EntityOps.GetSystemArray(variable),
                                                                StringOps.DefaultMatchMode, pattern, null, false,
                                                                ref keysAndValues, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = keysAndValues;
                                                        }
                                                        else
                                                        {
#if DATA
                                                            DatabaseVariable databaseVariable = null;

                                                            if (interpreter.IsDatabaseVariable(variable, ref databaseVariable))
                                                            {
                                                                string value = databaseVariable.KeysAndValuesToString(
                                                                    interpreter, pattern, false, ref result);

                                                                if (value != null)
                                                                    result = value;
                                                                else
                                                                    code = ReturnCode.Error;
                                                            }
                                                            else
#endif
                                                            {
                                                                //
                                                                // FIXME: PRI 4: Variable traces will not be fired here because we are
                                                                //        accessing the array elements via the ArrayValues property and
                                                                //        not through the GetVariableValue method.
                                                                //
                                                                result = variable.ArrayValue.KeysAndValuesToString(pattern, false);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array get arrayName ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lmap":
                                    {
                                        code = ScriptOps.ArrayNamesLoopCommand(
                                            this, subCommand, true, interpreter,
                                            clientData, arguments, ref result);

                                        break;
                                    }
                                case "names":
                                    {
                                        if ((arguments.Count >= 3) && (arguments.Count <= 5))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        MatchMode mode = StringOps.DefaultMatchMode;
                                                        string pattern = null;

                                                        switch (arguments.Count)
                                                        {
                                                            case 4:
                                                                {
                                                                    pattern = arguments[3];
                                                                    break;
                                                                }
                                                            case 5:
                                                                {
                                                                    object enumValue = EnumOps.TryParseEnum(
                                                                        typeof(MatchMode), arguments[3], true,
                                                                        true);

                                                                    if (enumValue is MatchMode)
                                                                        mode = (MatchMode)enumValue;
                                                                    else
                                                                        mode = MatchMode.None;

                                                                    pattern = arguments[4];
                                                                    break;
                                                                }
                                                        }

                                                        if (mode != MatchMode.None)
                                                        {
                                                            //
                                                            // HACK: Handle the global "env" array specially.  We must do this because
                                                            //       our global "env" array has no backing storage (unlike Tcl's) and
                                                            //       we do not have a trace operation for "get names" or "get names
                                                            //       and values".
                                                            //
                                                            if (interpreter.IsEnvironmentVariable(variable))
                                                            {
                                                                StringDictionary environment =
                                                                    CommonOps.Environment.GetVariables();

                                                                if (environment != null)
                                                                {
                                                                    result = environment.KeysToString(
                                                                        mode, pattern, false, RegexOptions.None);
                                                                }
                                                                else
                                                                {
                                                                    result = "environment variables unavailable";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else if (interpreter.IsTestsVariable(variable))
                                                            {
                                                                StringDictionary tests = interpreter.GetAllTestInformation(
                                                                    false, ref result);

                                                                if (tests != null)
                                                                    result = tests.KeysToString(
                                                                        mode, pattern, false, RegexOptions.None);
                                                                else
                                                                    code = ReturnCode.Error;
                                                            }
                                                            else if (interpreter.IsSystemArrayVariable(variable))
                                                            {
                                                                StringList keys = null;

                                                                code = MarshalOps.GetArrayElementKeys(
                                                                    interpreter, EntityOps.GetSystemArray(variable),
                                                                    StringOps.DefaultMatchMode, pattern, false,
                                                                    ref keys, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = keys;
                                                            }
                                                            else
                                                            {
#if DATA
                                                                DatabaseVariable databaseVariable = null;

                                                                if (interpreter.IsDatabaseVariable(variable, ref databaseVariable))
                                                                {
                                                                    string value = databaseVariable.KeysToString(
                                                                        interpreter, mode, pattern, false, RegexOptions.None,
                                                                        ref result);

                                                                    if (value != null)
                                                                        result = value;
                                                                    else
                                                                        code = ReturnCode.Error;
                                                                }
                                                                else
#endif
                                                                {
                                                                    //
                                                                    // FIXME: PRI 4: Variable traces will not be fired here because we are
                                                                    //        accessing the array elements via the ArrayValues property and
                                                                    //        not through the GetVariableValue method.
                                                                    //
                                                                    result = variable.ArrayValue.KeysToString(
                                                                        mode, pattern, false, RegexOptions.None);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "bad option \"{0}\": must be -exact, -substring, -glob, or -regexp",
                                                                arguments[3]);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array names arrayName ?mode? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "nextelement":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        ArraySearchDictionary arraySearches = interpreter.ArraySearches;

                                                        if (arraySearches != null)
                                                        {
                                                            ArraySearch arraySearch;

                                                            if (arraySearches.TryGetValue(arguments[3], out arraySearch))
                                                            {
                                                                if (System.Object.ReferenceEquals(arraySearch.Variable, variable))
                                                                {
                                                                    result = arraySearch.GetNextElement();
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "search identifier \"{0}\" isn't for variable \"{1}\"",
                                                                        arguments[3], arguments[2]);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "couldn't find search \"{0}\"",
                                                                    arguments[3]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "array searches not available";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array nextelement arrayName searchId\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "random":
                                    {
                                        //
                                        // FIXME: This sub-command does not fire read traces, ever.
                                        //
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-strict", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-pair", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-valueonly", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-matchname", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-matchvalue", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) <= arguments.Count) && ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    bool strict = false;

                                                    if (options.IsPresent("-strict"))
                                                        strict = true;

                                                    bool pair = false;

                                                    if (options.IsPresent("-pair"))
                                                        pair = true;

                                                    bool valueOnly = false;

                                                    if (options.IsPresent("-valueonly"))
                                                        valueOnly = true;

                                                    bool matchName = false;

                                                    if (options.IsPresent("-matchname"))
                                                        matchName = true;

                                                    bool matchValue = false;

                                                    if (options.IsPresent("-matchvalue"))
                                                        matchValue = true;

                                                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                    {
                                                        VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                        IVariable variable = null;
                                                        Result localError = null;

                                                        if (interpreter.GetVariableViaResolversWithSplit(
                                                                arguments[argumentIndex], ref flags, ref variable,
                                                                ref localError) == ReturnCode.Ok)
                                                        {
                                                            string pattern = null;

                                                            if ((argumentIndex + 2) == arguments.Count)
                                                                pattern = arguments[argumentIndex + 1];

                                                            //
                                                            // HACK: If there is a pattern, make sure that at
                                                            //       least name matching is enabled.
                                                            //
                                                            if ((pattern != null) && !matchName && !matchValue)
                                                                matchName = true;

                                                            if (EntityOps.IsLink(variable))
                                                                variable = EntityOps.FollowLinks(variable, flags);

                                                            if (!EntityOps.IsUndefined(variable) &&
                                                                EntityOps.IsArray(variable))
                                                            {
                                                                ElementDictionary arrayValue = null;

                                                                if (interpreter.IsEnvironmentVariable(variable))
                                                                {
                                                                    IDictionary environment =
                                                                        Environment.GetEnvironmentVariables();

                                                                    if (environment != null)
                                                                    {
                                                                        arrayValue = new ElementDictionary(
                                                                            interpreter.VariableEvent, environment,
                                                                            StringOps.DefaultMatchMode, pattern,
                                                                            false, matchName, matchValue);
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "environment variables unavailable";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else if (interpreter.IsTestsVariable(variable))
                                                                {
                                                                    StringDictionary tests = interpreter.GetAllTestInformation(
                                                                        true, ref result);

                                                                    if (tests != null)
                                                                    {
                                                                        arrayValue = new ElementDictionary(
                                                                            interpreter.VariableEvent, tests,
                                                                            StringOps.DefaultMatchMode, pattern,
                                                                            false, matchName, matchValue);
                                                                    }
                                                                    else
                                                                    {
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else if (interpreter.IsSystemArrayVariable(variable))
                                                                {
                                                                    StringDictionary keysAndValues = null;

                                                                    if (pattern != null)
                                                                    {
                                                                        code = MarshalOps.GetArrayElementKeysAndValues(
                                                                            interpreter, EntityOps.GetSystemArray(variable),
                                                                            StringOps.DefaultMatchMode, pattern, false,
                                                                            matchName, matchValue, ref keysAndValues,
                                                                            ref result);
                                                                    }
                                                                    else
                                                                    {
                                                                        code = MarshalOps.GetArrayElementKeysAndValues(
                                                                            interpreter, EntityOps.GetSystemArray(variable),
                                                                            StringOps.DefaultMatchMode, null, null, false,
                                                                            ref keysAndValues, ref result);
                                                                    }

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        arrayValue = new ElementDictionary(
                                                                            interpreter.VariableEvent, keysAndValues);
                                                                    }
                                                                }
                                                                else
                                                                {
#if DATA
                                                                    DatabaseVariable databaseVariable = null;

                                                                    if (interpreter.IsDatabaseVariable(
                                                                            variable, ref databaseVariable))
                                                                    {
                                                                        ObjectDictionary database = databaseVariable.GetList(
                                                                            interpreter, true, true, ref result);

                                                                        if (database != null)
                                                                        {
                                                                            arrayValue = new ElementDictionary(
                                                                                interpreter.VariableEvent, database,
                                                                                StringOps.DefaultMatchMode, pattern,
                                                                                false, matchName, matchValue);
                                                                        }
                                                                        else
                                                                        {
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
#endif
                                                                    {
                                                                        if (pattern != null)
                                                                        {
                                                                            Result localResult = null;

                                                                            code = GenericOps<string, object>.KeysAndValues(
                                                                                variable.ArrayValue, true, true,
                                                                                StringOps.DefaultMatchMode, pattern, null,
                                                                                null, null, matchName, matchValue, false,
                                                                                StringOps.DefaultRegExOptions, ref localResult);

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                StringDictionary keysAndValues =
                                                                                    localResult.Value as StringDictionary;

                                                                                if (keysAndValues != null)
                                                                                {
                                                                                    arrayValue = new ElementDictionary(
                                                                                        interpreter.VariableEvent, keysAndValues);
                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            arrayValue = variable.ArrayValue;
                                                                        }
                                                                    }
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (arrayValue != null)
                                                                    {
                                                                        if (strict || (arrayValue.Count > 0))
                                                                        {
                                                                            string name = arrayValue.GetRandom(
                                                                                interpreter.RandomNumberGenerator, ref result);

                                                                            if (name != null)
                                                                            {
                                                                                string value = null;

                                                                                if (pair || valueOnly)
                                                                                {
                                                                                    object objectValue;

                                                                                    if (arrayValue.TryGetValue(name, out objectValue))
                                                                                    {
                                                                                        value = StringOps.GetStringFromObject(objectValue);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Format(
                                                                                            "can't read {0}: no such element in array",
                                                                                            FormatOps.ErrorVariableName(variable,
                                                                                                null, arguments[argumentIndex], name));

                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }

                                                                                if (code == ReturnCode.Ok)
                                                                                {
                                                                                    if (pair)
                                                                                        result = StringList.MakeList(name, value);
                                                                                    else if (valueOnly)
                                                                                        result = value;
                                                                                    else
                                                                                        result = name;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Empty;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        result = String.Format(
                                                                            "\"{0}\" isn't an array",
                                                                            arguments[argumentIndex]);

                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "\"{0}\" isn't an array",
                                                                    arguments[argumentIndex]);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                            {
                                                                result = String.Format(
                                                                    "\"{0}\" isn't an array",
                                                                    arguments[argumentIndex]);

                                                                code = ReturnCode.Error;
                                                            }
                                                            else
                                                            {
                                                                result = localError;
                                                                code = ReturnCode.Error;
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
                                                        result = "wrong # args: should be \"array random ?options? arrayName ?pattern?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array random ?options? arrayName ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "set":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                //
                                                // BUGFIX: If we are not going to add a new array variable, the least
                                                //         we need to do is validate that the argument refers to an
                                                //         existing array and that it does not refer to an element of
                                                //         that array.
                                                //
                                                VariableFlags flags = VariableFlags.ArrayCommandMask |
                                                    VariableFlags.Array | VariableFlags.NoGetArray;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if ((interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok) ||
                                                    (interpreter.AddVariable2(
                                                        flags & ~VariableFlags.Defined, arguments[2],
                                                        null, true, ref variable,
                                                        ref localError) == ReturnCode.Ok))
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!interpreter.IsEnvironmentVariable(variable) &&
                                                        !interpreter.IsTestsVariable(variable) &&
                                                        !interpreter.IsSystemArrayVariable(variable)
#if DATA
                                                        && !interpreter.IsDatabaseVariable(variable)
#endif
                                                        && !interpreter.IsLinkedVariable(variable)
                                                        && !interpreter.IsEnumerableVariable(variable)
                                                        )
                                                    {
                                                        bool isUndefined = EntityOps.IsUndefined(
                                                            variable);

                                                        if (isUndefined || EntityOps.IsArray2(variable))
                                                        {
                                                            StringList list = null;

                                                            if (Parser.SplitList(
                                                                    interpreter, arguments[3], 0,
                                                                    Length.Invalid, true, ref list,
                                                                    ref result) == ReturnCode.Ok)
                                                            {
                                                                if ((list.Count & 1) == 0)
                                                                {
                                                                    ElementDictionary oldArrayValue = isUndefined ?
                                                                        null : variable.ArrayValue;

                                                                    ElementDictionary newArrayValue = new ElementDictionary(
                                                                        interpreter.VariableEvent, list.Count / 2);

                                                                    //
                                                                    // NOTE: Copy the names and values for the new/modified
                                                                    //       array elements from the provided arguments into
                                                                    //       the new array.
                                                                    //
                                                                    for (int index = 0; index < list.Count; index += 2)
                                                                        newArrayValue[list[index]] = list[index + 1];

                                                                    //
                                                                    // BUGFIX: We *MUST* fire traces here; otherwise, both
                                                                    //         old and new opaque object handles could have
                                                                    //         incorrect reference counts.
                                                                    //
                                                                    code = interpreter.FireArraySetTraces(
                                                                        BreakpointType.BeforeVariableSet, flags, variable.Frame,
                                                                        arguments[2], null, null, oldArrayValue, newArrayValue,
                                                                        variable, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        //
                                                                        // HACK: Augment (or replace) the array value for this
                                                                        //       variable to the one we just created, bypassing
                                                                        //       the normal path through the SetVariableValue*
                                                                        //       subsystem (for speed).
                                                                        //
                                                                        if (oldArrayValue != null)
                                                                        {
                                                                            //
                                                                            // BUGFIX: Some array elements may already exist;
                                                                            //         therefore, do not replace the existing
                                                                            //         array, modify it instead.
                                                                            //
                                                                            foreach (KeyValuePair<string, object> pair
                                                                                    in newArrayValue)
                                                                            {
                                                                                oldArrayValue[pair.Key] = pair.Value;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            //
                                                                            // NOTE: There is no existing array "value" for
                                                                            //       this variable; therefore, set the array
                                                                            //       value to the new values.
                                                                            //
                                                                            variable.ArrayValue = newArrayValue;
                                                                        }

                                                                        //
                                                                        // BUGFIX: The variable is now defined.  Mark the
                                                                        //         variable as "dirty" AFTER all the actual
                                                                        //         modifications have been completed.
                                                                        //
                                                                        EntityOps.SetArray(variable, true);
                                                                        EntityOps.SetUndefined(variable, false);
                                                                        EntityOps.SignalDirty(variable, null);

                                                                        //
                                                                        // NOTE: This command returns an empty result on
                                                                        //       success.
                                                                        //
                                                                        result = String.Empty;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = "list must have an even number of elements";
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
                                                            result = String.Format(
                                                                "\"{0}\" isn't an array",
                                                                arguments[2]);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" is system array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // TODO: Is this check right?  If so, needs test cases.
                                                    //
                                                    if (!FlagOps.HasFlags(flags, VariableFlags.NotFound, true) ||
                                                        FlagOps.HasFlags(flags, VariableFlags.HasLinkIndex, true))
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array set arrayName list\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "size":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        //
                                                        // HACK: Handle the global "env" array specially.  We must do this because
                                                        //       our global "env" array has no backing storage (unlike Tcl's) and
                                                        //       we do not have a trace operation for "get names" or "get names
                                                        //       and values".
                                                        //
                                                        if (interpreter.IsEnvironmentVariable(variable))
                                                        {
                                                            StringDictionary environment =
                                                                CommonOps.Environment.GetVariables();

                                                            if (environment != null)
                                                            {
                                                                result = environment.Count;
                                                            }
                                                            else
                                                            {
                                                                result = "environment variables unavailable";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else if (interpreter.IsTestsVariable(variable))
                                                        {
                                                            StringDictionary tests = interpreter.GetAllTestInformation(
                                                                false, ref result);

                                                            if (tests != null)
                                                                result = tests.Count;
                                                            else
                                                                code = ReturnCode.Error;
                                                        }
                                                        else if (interpreter.IsSystemArrayVariable(variable))
                                                        {
                                                            System.Array array = EntityOps.GetSystemArray(variable);

                                                            if (array != null)
                                                            {
                                                                result = array.Length;
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "invalid {0} variable",
                                                                    FormatOps.TypeName(typeof(System.Array)));

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
#if DATA
                                                            DatabaseVariable databaseVariable = null;

                                                            if (interpreter.IsDatabaseVariable(variable, ref databaseVariable))
                                                            {
                                                                long? count = databaseVariable.GetCount(interpreter, ref result);

                                                                if (count != null)
                                                                    result = (long)count;
                                                                else
                                                                    code = ReturnCode.Error;
                                                            }
                                                            else
#endif
                                                            {
                                                                //
                                                                // FIXME: PRI 4: Variable traces will not be fired here because we are
                                                                //        accessing the array elements via the ArrayValues property and
                                                                //        not through the GetVariableValue method.
                                                                //
                                                                result = variable.ArrayValue.Count;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = 0; // variable is not array.
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = 0; // variable does not exist.
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array size arrayName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "startsearch":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        ArraySearchDictionary arraySearches = interpreter.ArraySearches;

                                                        if (arraySearches != null)
                                                        {
                                                            ArraySearch arraySearch = new ArraySearch(interpreter, variable);
                                                            string name = FormatOps.Id("arraySearch", null, interpreter.NextId());

                                                            //
                                                            // NOTE: Add the new array search to the interpreter.
                                                            //
                                                            arraySearches.Add(name, arraySearch);

                                                            //
                                                            // NOTE: Return the Id of the newly created array search
                                                            //       to the caller.
                                                            //
                                                            result = name;
                                                        }
                                                        else
                                                        {
                                                            result = "array searches not available";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Format(
                                                            "\"{0}\" isn't an array",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array startsearch arrayName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unset":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        if (arguments.Count == 4)
                                                        {
                                                            StringList list = null;

                                                            //
                                                            // HACK: Handle the global "env" array specially.  We must do this because
                                                            //       our global "env" array has no backing storage (unlike Tcl's) and
                                                            //       we do not have a trace operation for "get names" or "get names
                                                            //       and values".
                                                            //
                                                            if (interpreter.IsEnvironmentVariable(variable))
                                                            {
                                                                StringDictionary environment =
                                                                    CommonOps.Environment.GetVariables();

                                                                if (environment != null)
                                                                {
                                                                    list = new StringList(environment.Keys);
                                                                }
                                                                else
                                                                {
                                                                    result = "environment variables unavailable";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else if (interpreter.IsTestsVariable(variable))
                                                            {
                                                                StringDictionary tests = interpreter.GetAllTestInformation(
                                                                    false, ref result);

                                                                if (tests != null)
                                                                    list = new StringList(tests.Keys);
                                                                else
                                                                    code = ReturnCode.Error;
                                                            }
                                                            else if (interpreter.IsSystemArrayVariable(variable))
                                                            {
                                                                result = String.Format(
                                                                    "can't unset matching {0} elements: " +
                                                                    "operation not supported for {1}",
                                                                    FormatOps.ErrorVariableName(
                                                                        variable, null, arguments[2], null),
                                                                        FormatOps.TypeName(typeof(System.Array)));

                                                                code = ReturnCode.Error;
                                                            }
                                                            else
                                                            {
#if DATA
                                                                DatabaseVariable databaseVariable = null;

                                                                if (interpreter.IsDatabaseVariable(variable, ref databaseVariable))
                                                                {
                                                                    ObjectDictionary database = databaseVariable.GetList(
                                                                        interpreter, true, false, ref result);

                                                                    if (database != null)
                                                                        list = new StringList(database.Keys);
                                                                    else
                                                                        code = ReturnCode.Error;
                                                                }
                                                                else
#endif
                                                                {
                                                                    //
                                                                    // FIXME: PRI 4: Variable traces will not be fired here because we are
                                                                    //        accessing the array elements via the ArrayValues property and
                                                                    //        not through the GetVariableValue method.
                                                                    //
                                                                    list = new StringList(variable.ArrayValue.Keys);
                                                                }
                                                            }

                                                            //
                                                            // NOTE: Is there anything to do (i.e. elements to unset)?
                                                            //
                                                            if (list != null)
                                                            {
                                                                foreach (string element in list)
                                                                {
                                                                    if (StringOps.Match(
                                                                            interpreter, StringOps.DefaultMatchMode,
                                                                            element, arguments[3], false))
                                                                    {
                                                                        code = interpreter.UnsetVariable2(
                                                                            VariableFlags.None, arguments[2],
                                                                            element, variable, ref result);

                                                                        if (code != ReturnCode.Ok)
                                                                            break;
                                                                    }
                                                                }

                                                                if (code == ReturnCode.Ok)
                                                                    result = String.Empty;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: Remove the entire array.
                                                            //
                                                            code = interpreter.UnsetVariable2(VariableFlags.None, arguments[2],
                                                                null, variable, ref result);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array unset arrayName ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "values":
                                    {
                                        if ((arguments.Count >= 3) && (arguments.Count <= 5))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                VariableFlags flags = VariableFlags.ArrayCommandMask;
                                                IVariable variable = null;
                                                Result localError = null;

                                                if (interpreter.GetVariableViaResolversWithSplit(
                                                        arguments[2], ref flags, ref variable,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    if (EntityOps.IsLink(variable))
                                                        variable = EntityOps.FollowLinks(variable, flags);

                                                    if (!EntityOps.IsUndefined(variable) && EntityOps.IsArray(variable))
                                                    {
                                                        MatchMode mode = StringOps.DefaultMatchMode;
                                                        string pattern = null;

                                                        switch (arguments.Count)
                                                        {
                                                            case 4:
                                                                {
                                                                    pattern = arguments[3];
                                                                    break;
                                                                }
                                                            case 5:
                                                                {
                                                                    object enumValue = EnumOps.TryParseEnum(
                                                                        typeof(MatchMode), arguments[3], true,
                                                                        true);

                                                                    if (enumValue is MatchMode)
                                                                        mode = (MatchMode)enumValue;
                                                                    else
                                                                        mode = MatchMode.None;

                                                                    pattern = arguments[4];
                                                                    break;
                                                                }
                                                        }

                                                        if (mode != MatchMode.None)
                                                        {
                                                            //
                                                            // HACK: Handle the global "env" array specially.  We must do this because
                                                            //       our global "env" array has no backing storage (unlike Tcl's) and
                                                            //       we do not have a trace operation for "get names" or "get names
                                                            //       and values".
                                                            //
                                                            if (interpreter.IsEnvironmentVariable(variable))
                                                            {
                                                                StringDictionary environment =
                                                                    CommonOps.Environment.GetVariables();

                                                                if (environment != null)
                                                                {
                                                                    result = environment.ValuesToString(
                                                                        mode, pattern, false, RegexOptions.None);
                                                                }
                                                                else
                                                                {
                                                                    result = "environment variables unavailable";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else if (interpreter.IsTestsVariable(variable))
                                                            {
                                                                StringDictionary tests = interpreter.GetAllTestInformation(
                                                                    true, ref result);

                                                                if (tests != null)
                                                                    result = tests.ValuesToString(
                                                                        mode, pattern, false, RegexOptions.None);
                                                                else
                                                                    code = ReturnCode.Error;
                                                            }
                                                            else if (interpreter.IsSystemArrayVariable(variable))
                                                            {
                                                                StringList values = null;

                                                                code = MarshalOps.GetArrayElementValues(
                                                                    interpreter, EntityOps.GetSystemArray(variable),
                                                                    StringOps.DefaultMatchMode, pattern, false,
                                                                    ref values, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = values;
                                                            }
                                                            else
                                                            {
#if DATA
                                                                DatabaseVariable databaseVariable = null;

                                                                if (interpreter.IsDatabaseVariable(variable, ref databaseVariable))
                                                                {
                                                                    ObjectDictionary database = databaseVariable.GetList(
                                                                        interpreter, true, true, ref result);

                                                                    if (database != null)
                                                                        result = database.ValuesToString(
                                                                            mode, pattern, false, RegexOptions.None);
                                                                    else
                                                                        code = ReturnCode.Error;
                                                                }
                                                                else
#endif
                                                                {
                                                                    //
                                                                    // FIXME: PRI 4: Variable traces will not be fired here because we are
                                                                    //        accessing the array elements via the ArrayValues property and
                                                                    //        not through the GetVariableValue method.
                                                                    //
                                                                    result = variable.ArrayValue.ValuesToString(
                                                                        mode, pattern, false, RegexOptions.None);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "bad option \"{0}\": must be -exact, -substring, -glob, or -regexp",
                                                                arguments[3]);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, VariableFlags.ArrayErrorMask, false))
                                                    {
                                                        result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = localError;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"array values arrayName ?mode? ?pattern?\"";
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
                        result = "wrong # args: should be \"array option ?arg ...?\"";
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
