/*
 * EntityOps.cs --
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
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("c0e69f4b-fe35-44aa-8798-3080a85e6614")]
    internal static class EntityOps
    {
        #region Callback Checking Methods
        public static bool IsReadOnly(
            ICallback callback
            )
        {
            return (callback != null) ?
                FlagOps.HasFlags(callback.CallbackFlags,
                    CallbackFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Support Methods
        public static ObjectFlags GetFlagsNoThrow(
            IObject @object
            )
        {
            if (@object != null)
            {
                try
                {
                    return @object.ObjectFlags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return ObjectFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Support Methods
        public static EventFlags GetFlagsNoThrow(
            IEvent @event
            )
        {
            if (@event != null)
            {
                try
                {
                    return @event.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return EventFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Plugin Support Methods
        public static PluginFlags GetFlagsNoThrow(
            IPluginData pluginData
            )
        {
            if (pluginData != null)
            {
                try
                {
                    return pluginData.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return PluginFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static AppDomain GetAppDomainNoThrow(
            IPluginData pluginData
            )
        {
            if (pluginData != null)
            {
                try
                {
                    return pluginData.AppDomain; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Support Methods
        public static PackageFlags GetFlagsNoThrow(
            IPackageData packageData
            )
        {
            if (packageData != null)
            {
                try
                {
                    return packageData.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return PackageFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Support Methods
        public static CommandFlags GetFlagsNoThrow(
            ICommand command
            )
        {
            if (command != null)
            {
                try
                {
                    return command.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return CommandFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Checking Methods
        public static bool HasBreakpoint(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSafe(
            ICommand command
            )
        {
            if (command != null)
            {
                CommandFlags flags = command.Flags;

                if (FlagOps.HasFlags(flags, CommandFlags.Safe, true) &&
                    !FlagOps.HasFlags(flags, CommandFlags.Unsafe, true))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsHidden(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Hidden, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoRename(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoRename, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Mutator Methods
        public static bool SetBreakpoint(
            ICommand command,
            bool breakpoint
            )
        {
            if (command != null)
            {
                if (breakpoint)
                    command.Flags |= CommandFlags.Breakpoint;
                else
                    command.Flags &= ~CommandFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetReadOnly(
            ICommand command,
            bool readOnly
            )
        {
            if (command != null)
            {
                if (readOnly)
                    command.Flags |= CommandFlags.ReadOnly;
                else
                    command.Flags &= ~CommandFlags.ReadOnly;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Function Checking Methods
        public static bool HasBreakpoint(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.ReadOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSafe(
            IFunction function
            )
        {
            if (function != null)
            {
                FunctionFlags flags = function.Flags;

                if (FlagOps.HasFlags(flags, FunctionFlags.Safe, true) &&
                    !FlagOps.HasFlags(flags, FunctionFlags.Unsafe, true))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Function Mutator Methods
        public static bool SetBreakpoint(
            IFunction function,
            bool breakpoint
            )
        {
            if (function != null)
            {
                if (breakpoint)
                    function.Flags |= FunctionFlags.Breakpoint;
                else
                    function.Flags &= ~FunctionFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy Checking Methods
        public static bool HasFlags(
            IPolicy policy,
            MethodFlags methodFlags,
            bool all
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.MethodFlags,
                    methodFlags, all) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Checking Methods
        public static bool IsDisabled(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Procedure Checking Methods
        public static bool HasBreakpoint(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAtomic(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Atomic, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || PARSE_CACHE
        public static bool IsNonCaching(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NonCaching, true) : false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsHidden(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Hidden, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.ReadOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoReplace(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoReplace, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoRename(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoRename, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoRemove(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoRemove, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Procedure Mutator Methods
        public static bool SetBreakpoint(
            IProcedure procedure,
            bool breakpoint
            )
        {
            if (procedure != null)
            {
                if (breakpoint)
                    procedure.Flags |= ProcedureFlags.Breakpoint;
                else
                    procedure.Flags &= ~ProcedureFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetReadOnly(
            IProcedure procedure,
            bool readOnly
            )
        {
            if (procedure != null)
            {
                if (readOnly)
                    procedure.Flags |= ProcedureFlags.ReadOnly;
                else
                    procedure.Flags &= ~ProcedureFlags.ReadOnly;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Operator Checking Methods
        public static bool HasBreakpoint(
            IOperator @operator
            )
        {
            return (@operator != null) ?
                FlagOps.HasFlags(@operator.Flags,
                    OperatorFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            IOperator @operator
            )
        {
            return (@operator != null) ?
                FlagOps.HasFlags(@operator.Flags,
                    OperatorFlags.Disabled, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Operator Mutator Methods
        public static bool SetBreakpoint(
            IOperator @operator,
            bool breakpoint
            )
        {
            if (@operator != null)
            {
                if (breakpoint)
                    @operator.Flags |= OperatorFlags.Breakpoint;
                else
                    @operator.Flags &= ~OperatorFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Checking Methods
        public static bool IsDisabled(
            ISubCommand subCommand
            )
        {
            return (subCommand != null) ?
                FlagOps.HasFlags(subCommand.CommandFlags,
                    CommandFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSafe(
            ISubCommand subCommand
            )
        {
            if (subCommand != null)
            {
                SubCommandFlags flags = subCommand.Flags;

                if (FlagOps.HasFlags(flags, SubCommandFlags.Safe, true) &&
                    !FlagOps.HasFlags(flags, SubCommandFlags.Unsafe, true))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsHidden(
            ISubCommand subCommand
            )
        {
            return (subCommand != null) ?
                FlagOps.HasFlags(subCommand.CommandFlags,
                    CommandFlags.Hidden, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Support Methods
        public static void GetFlags(
            ref string varName,      /* in, out */
            ref VariableFlags flags  /* in, out */
            )
        {
            bool absolute = false;

            varName = NamespaceOps.TrimLeading(varName, ref absolute);

            if (absolute)
            {
                //
                // NOTE: Set the caller's flags to force them to use the
                //       global call frame for this variable from now on.
                //
                flags |= VariableFlags.GlobalOnly;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static object GetOldValue(
            VariableFlags flags, /* NOT USED */
            IVariable variable,
            string index,
            object @default
            )
        {
            if (variable != null)
            {
                if (index != null)
                {
                    ElementDictionary arrayValue = variable.ArrayValue;

                    if (arrayValue != null)
                    {
                        object value;

                        if (arrayValue.TryGetValue(index, out value) &&
                            (value != null))
                        {
                            if (FlagOps.HasFlags(flags,
                                    VariableFlags.ForceToString, true))
                            {
                                return StringOps.GetStringFromObject(value);
                            }

                            return value;
                        }
                    }
                }
                else
                {
                    object value = variable.Value;

                    if (value != null)
                    {
                        if (FlagOps.HasFlags(flags,
                                VariableFlags.ForceToString, true))
                        {
                            return StringOps.GetStringFromObject(value);
                        }

                        return value;
                    }
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static object GetNewValue(
            VariableFlags flags,
            string name,
            string index,
            object oldValue,
            object newValue
            )
        {
            if (FlagOps.HasFlags(flags, VariableFlags.AppendValue, true))
            {
                StringBuilder value = oldValue as StringBuilder;

                if (value == null)
                    //
                    // BUGBUG: Would discard any non-string "internal rep"
                    //         the old variable value may have had.
                    //
                    value = StringOps.NewStringBuilder(oldValue as string);

                return value.Append(newValue);
            }
            else if (FlagOps.HasFlags(flags, VariableFlags.AppendElement, true))
            {
                StringList value = oldValue as StringList;

                if (value == null)
                {
                    if (oldValue != null)
                        //
                        // BUGBUG: Would discard any non-string "internal rep"
                        //         the old variable value may have had.
                        //
                        value = new StringList(oldValue as string);
                    else
                        value = new StringList();
                }

                value.Add(StringOps.GetStringFromObject(newValue));

                return value;
            }
            else
            {
                return newValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Array GetSystemArray(
            IVariable variable
            )
        {
            if (variable == null)
                return null;

            return variable.Value as Array;
        }

        ///////////////////////////////////////////////////////////////////////

        public static VariableFlags GetWatchpointFlags(
            VariableFlags flags
            )
        {
            return flags & VariableFlags.WatchpointMask;
        }

        ///////////////////////////////////////////////////////////////////////

        public static VariableFlags SetWatchpointFlags(
            VariableFlags flags,
            VariableFlags newFlags
            )
        {
            VariableFlags result = flags;

            result &= ~VariableFlags.WatchpointMask; /* remove old flags */
            result |= GetWatchpointFlags(newFlags);  /* add new flags */

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Checking Methods
        public static IVariable FollowLinks(
            IVariable variable,
            VariableFlags flags
            )
        {
            return FollowLinks(variable, flags, Count.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IVariable FollowLinks(
            IVariable variable,
            VariableFlags flags,
            int limit
            )
        {
            if (FlagOps.HasFlags(flags, VariableFlags.NoFollowLink, true))
                return variable;

            if (variable != null)
            {
                int count = 0;

                while (variable.Link != null)
                {
                    if ((limit > 0) && (count++ >= limit))
                        break;

                    variable = variable.Link;
                }
            }

            return variable;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasTraces(
            IVariable variable
            )
        {
            return (variable != null) && variable.HasTraces();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasValidLink(
            IVariable variable,
            bool force
            )
        {
            bool result = false;

            if (variable != null)
            {
                if (force || IsLink(variable))
                {
                    variable = FollowLinks(variable, VariableFlags.None);

                    if (variable != null)
                    {
                        ICallFrame frame = variable.Frame;

                        if ((frame != null) &&
                            !CallFrameOps.IsDisposedOrUndefined(frame))
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsArray(
            IVariable variable
            )
        {
            return (variable != null) ?
                IsArray2(variable) && (variable.ArrayValue != null) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsArray(
            IVariable variable,
            ref ElementDictionary arrayValue
            )
        {
            if ((variable != null) && IsArray2(variable))
            {
                arrayValue = variable.ArrayValue;
                return (arrayValue != null);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsArray2(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Array, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsVirtual(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Virtual, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBreakOnGet(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.BreakOnGet, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBreakOnSet(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.BreakOnSet, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBreakOnUnset(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.BreakOnUnset, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDirty(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Dirty, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsEvaluate(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Evaluate, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsLink(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Link, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoTrace(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.NoTrace, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoWatchpoint(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.NoWatchpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoPostProcess(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.NoPostProcess, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoNotify(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.NoNotify, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.ReadOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSubstitute(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Substitute, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSystem(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.System, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsMutable(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Mutable, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsInvariant(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Invariant, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUndefined(
            IVariable variable
            )
        {
            Result error = null;

            return IsUndefined(variable, null, null, null, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUndefined(
            IVariable variable,
            string operation,
            string name,
            string index,
            ref Result error
            )
        {
            //
            // HACK: Also check if the call frame is undefined.  Technically,
            //       this is now always required and so we do this here rather
            //       than propogate this check all throughout the code.
            //
            bool result = (variable != null) ?
                CallFrameOps.IsDisposedOrUndefined(variable.Frame) ||
                IsUndefined2(variable) : false;

            if (result && !String.IsNullOrEmpty(operation) && (name != null))
            {
                error = String.Format("can't {0} {1}: no such variable",
                    operation, FormatOps.ErrorVariableName(name, index));
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUndefined2(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.Undefined, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWriteOnly(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.WriteOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsVirtualOrSystem(
            IVariable variable
            )
        {
            return (variable != null) ?
                FlagOps.HasFlags(variable.Flags,
                    VariableFlags.VirtualOrSystemMask, false) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Variable Dirty Flag Methods
        public static bool IsNowClean(
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            return FlagOps.HasFlags(oldFlags, VariableFlags.Dirty, true) &&
                !FlagOps.HasFlags(newFlags, VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNowDirty(
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            return !FlagOps.HasFlags(oldFlags, VariableFlags.Dirty, true) &&
                FlagOps.HasFlags(newFlags, VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool OnFlagsChanged(
            EventWaitHandle variableEvent,
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            //
            // NOTE: If the wait flag is not set, we do not
            //       care about the flags changing.
            //
            if (FlagOps.HasFlags(newFlags, VariableFlags.Wait, true))
            {
                //
                // NOTE: If the variable is now clean [and
                //       it was dirty before], reset the
                //       event.
                //
                if (IsNowClean(oldFlags, newFlags))
                    return ThreadOps.ResetEvent(variableEvent);
                //
                // NOTE: Otherwise, if the variable is now
                //       dirty [and it was clean before],
                //       clear the wait flag and set the
                //       event.
                //
                else if (IsNowDirty(oldFlags, newFlags))
                    return ThreadOps.SetEvent(variableEvent);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Element Checking Methods
        public static bool IsDirty(
            IVariable variable,
            string index,
            bool wasUndefined
            )
        {
            if (variable == null)
                return false;

            ElementDictionary arrayValue = variable.ArrayValue;

            if (arrayValue == null)
                return false;

            if (index != null)
            {
                if (FlagOps.HasFlags(
                        arrayValue.GetFlags(index, VariableFlags.None),
                        VariableFlags.Dirty, true))
                {
                    return true;
                }

                //
                // BUGFIX: If the variable itself is now undefined, it was
                //         almost certainly [unset] during the [vwait] for
                //         the element; therefore, consider the element as
                //         "changed" now in that case.
                //
                // BUGFIX: *UPDATE* Unless the variable was undefined prior
                //         to any [vwait] taking place (this time).
                //
                return !wasUndefined && IsUndefined(variable);
            }
            else
            {
                foreach (KeyValuePair<string, object> pair in arrayValue)
                {
                    if (FlagOps.HasFlags(
                            arrayValue.GetFlags(pair.Key, VariableFlags.None),
                            VariableFlags.Dirty, true))
                    {
                        return true;
                    }
                }

                return IsDirty(variable);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Signaling Methods
        public static bool SignalClean(
            IVariable variable /* in */
            )
        {
            bool result = true;

            if (!SetWait(variable, true))
                result = false;

            if (!SetDirty(variable, false))
                result = false;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SignalClean(
            IVariable variable, /* in */
            string index        /* in, optional */
            )
        {
            bool result = true;

            if (!SetElementWait(variable, index, true))
                result = false;

            if (!SetElementDirty(variable, index, false))
                result = false;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SignalDirty(
            IVariable variable, /* in */
            string index        /* in, optional */
            )
        {
            if (variable == null)
                return false;

            bool result = true;
            ElementDictionary arrayValue = variable.ArrayValue;

            if (arrayValue != null)
            {
                //
                // TODO: To support waiting (and being notified) on array
                //       elements that have never been waited on nor flagged
                //       as dirty before, the value of the "initialFlags"
                //       parameter to the "ChangeElementFlags" method would
                //       need to be "VariableFlags.Wait"; however, this will
                //       have a negative impact on array element performance
                //       and is not necessary to obtain compliance with the
                //       semantics of the native Tcl [vwait] command.
                //
                if (!ChangeElementFlags(
                        variable, index, VariableFlags.None,
                        VariableFlags.Dirty, (index != null),
                        true))
                {
                    result = false;
                }
            }

            variable.Flags |= VariableFlags.Dirty;
            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Mutator Methods
        public static bool SetArray(
            IVariable variable,
            bool array
            )
        {
            if (variable != null)
            {
                if (array)
                    variable.Flags |= VariableFlags.Array;
                else
                    variable.Flags &= ~VariableFlags.Array;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetDirty(
            IVariable variable,
            bool dirty
            )
        {
            if (variable != null)
            {
                if (dirty)
                    variable.Flags |= VariableFlags.Dirty;
                else
                    variable.Flags &= ~VariableFlags.Dirty;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetGlobal(
            IVariable variable,
            bool global
            )
        {
            if (variable != null)
            {
                if (global)
                {
                    //
                    // NOTE: Mutually exclusive with the local flag.
                    //
                    variable.Flags &= ~VariableFlags.Local;
                    variable.Flags |= VariableFlags.Global;
                }
                else
                {
                    variable.Flags &= ~VariableFlags.Global;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetLink(
            IVariable variable,
            bool link
            )
        {
            if (variable != null)
            {
                if (link)
                    variable.Flags |= VariableFlags.Link;
                else
                    variable.Flags &= ~VariableFlags.Link;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetLocal(
            IVariable variable,
            bool local
            )
        {
            if (variable != null)
            {
                if (local)
                {
                    //
                    // NOTE: Mutually exclusive with the global flag.
                    //
                    variable.Flags &= ~VariableFlags.Global;
                    variable.Flags |= VariableFlags.Local;
                }
                else
                {
                    variable.Flags &= ~VariableFlags.Local;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static bool SetNoTrace(
            IVariable variable,
            bool noTrace
            )
        {
            if (variable != null)
            {
                if (noTrace)
                    variable.Flags |= VariableFlags.NoTrace;
                else
                    variable.Flags &= ~VariableFlags.NoTrace;

                return true;
            }
            else
            {
                return false;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static bool SetReadOnly(
            IVariable variable,
            bool readOnly
            )
        {
            if (variable != null)
            {
                if (readOnly)
                    variable.Flags |= VariableFlags.ReadOnly;
                else
                    variable.Flags &= ~VariableFlags.ReadOnly;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetUndefined(
            IVariable variable,
            bool undefined
            )
        {
            if (variable != null)
            {
                if (undefined)
                    variable.Flags |= VariableFlags.Undefined;
                else
                    variable.Flags &= ~VariableFlags.Undefined;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetWait(
            IVariable variable,
            bool wait
            )
        {
            if (variable != null)
            {
                if (wait)
                    variable.Flags |= VariableFlags.Wait;
                else
                    variable.Flags &= ~VariableFlags.Wait;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Array Element Mutator Methods
        private static bool ChangeElementFlags(
            IVariable variable,
            string index,
            VariableFlags initialFlags,
            VariableFlags changeFlags,
            bool create,
            bool add
            )
        {
            if (variable == null)
                return false;

            ElementDictionary arrayValue = variable.ArrayValue;

            if (arrayValue == null)
                return false;

            bool notify = true;

            if (index != null)
            {
                return arrayValue.ChangeFlags(
                    index, initialFlags, changeFlags, create, add,
                    ref notify);
            }
            else
            {
                bool result = true;

                foreach (KeyValuePair<string, object> pair in arrayValue)
                {
                    if (!arrayValue.ChangeFlags(
                            pair.Key, initialFlags, changeFlags,
                            create, add, ref notify))
                    {
                        result = false;
                    }
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetElementDirty(
            IVariable variable,
            string index,
            bool dirty
            )
        {
            return ChangeElementFlags(
                variable, index, VariableFlags.None,
                VariableFlags.Dirty, false, dirty);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetElementWait(
            IVariable variable,
            string index,
            bool wait
            )
        {
            return ChangeElementFlags(
                variable, index, VariableFlags.None,
                VariableFlags.Wait, true, wait);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Wrapper Support Methods
        public static long GetToken(
            IWrapperData wrapper
            )
        {
            if (wrapper != null)
                return wrapper.Token;

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetTokenNoThrow(
            IWrapperData wrapper
            )
        {
            if (wrapper != null)
            {
                try
                {
                    return wrapper.Token; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetToken(
            IWrapperData wrapper,
            long token
            )
        {
            if (wrapper != null)
                wrapper.Token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Identifier Support Methods
        public static Guid GetId(
            IIdentifierBase identifierBase
            )
        {
            if (identifierBase != null)
                return identifierBase.Id;

            return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetName(
            IIdentifierBase identifierBase
            )
        {
            if (identifierBase != null)
                return identifierBase.Name;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetNameNoThrow(
            IIdentifierBase identifierBase
            )
        {
            if (identifierBase != null)
            {
                try
                {
                    return identifierBase.Name; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetNameNoThrow(
            object @object
            )
        {
            if (@object != null)
            {
                IIdentifierBase identifierBase = @object as IIdentifierBase;

                if (identifierBase != null)
                    return GetNameNoThrow(identifierBase);

                try
                {
                    return @object.ToString(); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetNameNoThrow(
            AppDomain appDomain
            )
        {
            if (appDomain != null)
            {
                try
                {
                    return appDomain.FriendlyName; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetNameNoThrow(
            Encoding encoding
            )
        {
            if (encoding != null)
            {
                try
                {
                    return encoding.WebName; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeSetGroup(
            IIdentifier identifier,
            string group
            )
        {
            if ((identifier == null) || (group == null))
                return;

            identifier.Group = group;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Location Support Methods
        public static bool IsViaSource(
            IScriptLocation location
            )
        {
            return ((location != null) && location.ViaSource);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Support Methods
        public static bool IsUsable(
            Interpreter interpreter
            )
        {
            return ((interpreter != null) &&
                !Interpreter.IsDeletedOrDisposed(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        public static Interpreter FollowMaster(
            Interpreter interpreter,
            bool usable
            )
        {
            while (interpreter != null)
            {
                Interpreter masterInterpreter = null;

                try
                {
                    masterInterpreter = interpreter.MasterInterpreter;
                }
                catch (InterpreterDisposedException)
                {
                    // do nothing.
                }

                if (masterInterpreter == null)
                    break;

                interpreter = masterInterpreter;

                if (usable && IsUsable(interpreter))
                    return interpreter;
            }

            return interpreter;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Interpreter FollowTest(
            Interpreter interpreter,
            bool usable
            )
        {
            while (interpreter != null)
            {
                //
                // NOTE: This method requires access to the current test
                //       context; therefore, the interpreter *CANNOT* be
                //       disposed.
                //
                if (interpreter.Disposed)
                    break;

                Interpreter testTargetInterpreter = null;

                try
                {
                    testTargetInterpreter = interpreter.TestTargetInterpreter;
                }
                catch (InterpreterDisposedException)
                {
                    // do nothing.
                }

                if (testTargetInterpreter == null)
                    break;

                interpreter = testTargetInterpreter;

                if (usable && IsUsable(interpreter))
                    return interpreter;
            }

            return interpreter;
        }
        #endregion
    }
}
