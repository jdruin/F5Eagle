/*
 * CommandCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _ClientData = Eagle._Components.Public.ClientData;

namespace Eagle._Components.Private
{
    [ObjectId("ee26fe9d-bbe4-42af-be2d-d52952acc9a6")]
    internal sealed class CommandCallback :
            IGetInterpreter, ICallback, IExecute, IDisposable
    {
        #region Private Constants
        #region Dynamic Delegate Support
        //
        // NOTE: This is for use by GetDynamicDelegate() only.
        //
        private static readonly MethodInfo dynamicInvokeMethodInfo =
            typeof(CommandCallback).GetMethod("StaticFireDynamicInvokeCallback",
            MarshalOps.PublicStaticMethodBindingFlags);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private CommandCallback(
            string name,
            string group,
            string description,
            IClientData clientData,
            MarshalFlags marshalFlags,
            CallbackFlags callbackFlags,
            ObjectFlags objectFlags,
            ByRefArgumentFlags byRefArgumentFlags,
            Interpreter interpreter,
            StringList arguments
            )
        {
            this.kind = IdentifierKind.Callback;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.marshalFlags = marshalFlags;
            this.callbackFlags = callbackFlags;
            this.byRefArgumentFlags = byRefArgumentFlags;
            this.objectFlags = objectFlags;
            this.interpreter = interpreter;
            this.arguments = arguments;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ICallback Create(
            MarshalFlags marshalFlags,
            CallbackFlags callbackFlags,
            ObjectFlags objectFlags,
            ByRefArgumentFlags byRefArgumentFlags,
            Interpreter interpreter,
            IClientData clientData,
            string name,
            StringList arguments,
            ref Result error
            )
        {
            ICallback callback = null;

            //
            // NOTE: If the interpreter cannot currently be used for script
            //       evaluation, bail out now.
            //
            if (CheckInterpreter(interpreter, ref error))
            {
                //
                // NOTE: The "name" of the callback is the full string
                //       representation of the argument list.  We normalize
                //       null to empty string here because the underlying
                //       dictionary of callbacks in the interpreter cannot
                //       handle a null key.
                //
                if (name == null)
                {
                    name = (arguments != null) ?
                        arguments.ToString() : String.Empty;
                }

                //
                // NOTE: Attempt to locate the matching callback object in
                //       the interpreter.  If it is not found, create a new
                //       one based on the specified arguments, add it to the
                //       interpreter, and return it.
                //
                if (interpreter.GetCallback(name,
                        LookupFlags.Exists, ref callback) != ReturnCode.Ok)
                {
                    callback = new CommandCallback(
                        name, null, null, clientData, marshalFlags,
                        callbackFlags, objectFlags, byRefArgumentFlags,
                        interpreter, arguments);

                    if (interpreter.AddCallback(callback, clientData,
                            ref error) != ReturnCode.Ok)
                    {
                        callback = null;
                    }
                }
            }

            return callback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /* [static --> this] System.Delegate.DynamicInvoke */
        public static object StaticFireDynamicInvokeCallback(
            ICallback callback,
            object[] args
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            return callback.FireDynamicInvokeCallback(args);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static bool CheckInterpreter(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            if (Interpreter.IsDeletedOrDisposed(interpreter))
            {
                error = "interpreter is deleted or disposed";
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CheckScriptThread(
            IScriptThread scriptThread,
            ref Result error
            )
        {
            if (scriptThread == null)
            {
                error = "invalid script thread";
                return false;
            }

            if (scriptThread.IsDisposed)
            {
                error = "script thread is disposed";
                return false;
            }

            return CheckInterpreter(scriptThread.Interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out bool useOwner,
            out bool resetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy
            )
        {
            useOwner = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.UseOwner, true);

            resetCancel = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ResetCancel, true);

            asynchronous = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Asynchronous, true);

            asynchronousIfBusy = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AsynchronousIfBusy, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out bool useOwner,
            out bool resetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy,
            out bool fireAndForget,
            out bool complain,
            out bool disposeThread,
            out bool throwOnError
            )
        {
            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out asynchronous, out asynchronousIfBusy);

            ///////////////////////////////////////////////////////////////////

            fireAndForget = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.FireAndForget, true);

            complain = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Complain, true);

            disposeThread = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.DisposeThread, true);

            throwOnError = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ThrowOnError, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out ObjectOptionType objectOptionType,
            out bool needArguments,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString,
            out bool useOwner,
            out bool resetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy,
            out bool fireAndForget,
            out bool complain,
            out bool disposeThread,
            out bool throwOnError
            )
        {
            bool byRefStrict;
            bool returnValue;
            bool defaultValue;
            bool addReference;
            bool removeReference;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out asynchronous,
                out asynchronousIfBusy, out byRefStrict, out returnValue,
                out defaultValue, out addReference, out removeReference,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessCallbackFlags(
            CallbackFlags callbackFlags,
            out ObjectOptionType objectOptionType,
            out bool needArguments,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString,
            out bool useOwner,
            out bool resetCancel,
            out bool asynchronous,
            out bool asynchronousIfBusy,
            out bool byRefStrict,
            out bool returnValue,
            out bool defaultValue,
            out bool addReference,
            out bool removeReference,
            out bool fireAndForget,
            out bool complain,
            out bool disposeThread,
            out bool throwOnError
            )
        {
            needArguments = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Arguments, true);

            create = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Create, true);

            dispose = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Dispose, true);

            alias = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Alias, true);

            aliasRaw = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AliasRaw, true);

            aliasAll = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AliasAll, true);

            aliasReference = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AliasReference, true);

            toString = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ToString, true);

            ///////////////////////////////////////////////////////////////////

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out asynchronous, out asynchronousIfBusy);

            ///////////////////////////////////////////////////////////////////

            byRefStrict = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ByRefStrict, true);

            returnValue = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ReturnValue, true);

            defaultValue = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.DefaultValue, true);

            addReference = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.AddReference, true);

            removeReference = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.RemoveReference, true);

            fireAndForget = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.FireAndForget, true);

            complain = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.Complain, true);

            disposeThread = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.DisposeThread, true);

            throwOnError = FlagOps.HasFlags(
                callbackFlags, CallbackFlags.ThrowOnError, true);

            ///////////////////////////////////////////////////////////////////

            objectOptionType = ObjectOptionType.FireCallback |
                ObjectOps.GetOptionType(aliasRaw, aliasAll);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessMarshalFlags(
            MarshalFlags marshalFlags,
            out bool useDelegateCallback,
            out bool useGenericCallback,
            out bool useDynamicCallback
            )
        {
            useDelegateCallback = !FlagOps.HasFlags(
                marshalFlags, MarshalFlags.NoDelegateCallback, true);

            useGenericCallback = !FlagOps.HasFlags(
                marshalFlags, MarshalFlags.NoGenericCallback, true);

            useDynamicCallback = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.DynamicCallback, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldCatchInterrupt(
            CallbackFlags callbackFlags
            )
        {
            return FlagOps.HasFlags(
                callbackFlags, CallbackFlags.CatchInterrupt, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddArgument(
            string argument,
            ref StringList localArguments
            )
        {
            if (localArguments == null)
                localArguments = new StringList();

            localArguments.Add(argument);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddArguments(
            StringList localArguments,
            StringList arguments
            )
        {
            if ((localArguments == null) || (arguments == null))
                return;

            foreach (string argument in arguments)
                localArguments.Add(argument);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddArguments(
            StringList arguments,
            ref StringList localArguments
            )
        {
            if (arguments != null)
            {
                if (localArguments == null)
                    localArguments = new StringList();

                AddArguments(localArguments, arguments);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static object GetOwner(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.GetOwner();
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsScriptThreadOwnerBusy(
            Interpreter interpreter,
            IScriptThread scriptThread
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.InternalIsOwnerBusy(scriptThread);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsInterpreterBusy(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return interpreter.InternalIsBusy;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode InvokeUsingScriptThread(
            Interpreter interpreter,
            IScriptThread scriptThread,
            StringList arguments,
            bool resetCancel,
            bool asynchronous,
            bool asynchronousIfBusy,
            ref Result result,
            ref int errorLine
            )
        {
            //
            // NOTE: If the interpreter cannot currently be used for
            //       script evaluation, bail out now.
            //
            if (!CheckInterpreter(interpreter, ref result))
                return ReturnCode.Error;

            //
            // NOTE: If the script thread cannot currently be used
            //       for script evaluation, bail out now.
            //
            if (!CheckScriptThread(scriptThread, ref result))
                return ReturnCode.Error;

            //
            // NOTE: If the appropriate flag is set, reset the script
            //       cancellation flag for the target interpreter now.
            //
            if (resetCancel && !scriptThread.ResetCancel(
                    CancelFlags.CommandCallback, ref result))
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: No calls into the IScriptThread will supply
            //       error line information; therefore, reset it
            //       to a well-defined value (zero) now.
            //
            errorLine = 0;

            //
            // NOTE: Should the callback script be asynchronously
            //       queued to the IScriptThread rather than being
            //       sent synchronously?
            //
            if (asynchronous || (asynchronousIfBusy &&
                IsScriptThreadOwnerBusy(interpreter, scriptThread)))
            {
                //
                // NOTE: Queue the resulting command as a script
                //       (with proper list quoting), to the owner
                //       of the target interpreter, for evaluation
                //       asynchronously.
                //
                if (scriptThread.Queue(arguments.ToString()))
                {
                    result = null;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = "could not queue script to thread";
                    return ReturnCode.Error;
                }
            }
            else
            {
                //
                // NOTE: Send the resulting command as a script
                //       (with proper list quoting), to the owner
                //       of the target interpreter, for evaluation
                //       synchronously.
                //
                return scriptThread.Send(
                    arguments.ToString(), ref result);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode InvokeUsingInterpreter(
            Interpreter interpreter,
            StringList arguments,
            bool resetCancel,
            bool asynchronous,
            bool asynchronousIfBusy,
            ref Result result,
            ref int errorLine
            )
        {
            //
            // NOTE: If the interpreter cannot currently be used for
            //       script evaluation, bail out now.
            //
            if (!CheckInterpreter(interpreter, ref result))
                return ReturnCode.Error;

            //
            // NOTE: If the appropriate flag is set, reset the script
            //       cancellation flag for the target interpreter now.
            //
            if (resetCancel && Engine.ResetCancel(
                    interpreter, CancelFlags.CommandCallback,
                    ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (asynchronous ||
                (asynchronousIfBusy && IsInterpreterBusy(interpreter)))
            {
                //
                // NOTE: Asynchronous script evaluation means the
                //       error line information will be unavailable;
                //       therefore, reset it to a well-defined value
                //       (zero) now.
                //
                errorLine = 0;

                //
                // NOTE: Evaluate the resulting command as a script
                //       (with proper list quoting), asynchronously.
                //
                Result error = null;

                if (interpreter.EvaluateScript(
                        arguments.ToString(), null, null,
                        ref error) == ReturnCode.Ok)
                {
                    result = null;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = error;
                    return ReturnCode.Error;
                }
            }
            else
            {
                //
                // NOTE: Evaluate the resulting command as a script
                //       (with proper list quoting).
                //
                return interpreter.EvaluateScript(
                    arguments.ToString(), ref result,
                    ref errorLine); /* EXEMPT */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void AddArguments(
            StringList localArguments
            )
        {
            AddArguments(localArguments, this.arguments);
        }

        ///////////////////////////////////////////////////////////////////////

        private Delegate GetDynamicDelegate(
            string name,                            /* in */
            Type returnType,                        /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in: OPTIONAL */
            bool throwOnBindFailure,                /* in */
            ref Type delegateType,                  /* in, out */
            ref Result error                        /* out */
            )
        {
            if (returnType == null)
            {
                error = "invalid return type";
                return null;
            }

            if (parameterTypes == null)
            {
                error = "invalid parameter types";
                return null;
            }

            Type newDelegateType = delegateType;
            MethodInfo methodInfo;

            if (newDelegateType != null)
            {
                //
                // NOTE: Use the StaticFireDynamicInvokeCallback static method
                //       from this class as the method to act as a "trampoline"
                //       for the generated IL method body.  This works because
                //       the created DynamicMethod is logically also part of
                //       this class.
                //        
                methodInfo = dynamicInvokeMethodInfo;

                if (methodInfo == null)
                {
                    error = String.Format(
                        "missing \"{0}.StaticFireDynamicInvokeCallback\" method.",
                        typeof(CommandCallback).Name);

                    return null;
                }
            }
            else
            {
                //
                // NOTE: Use the StaticFireDynamicInvokeCallback static method
                //       from the CommandCallbackWrapper class as the method
                //       to act as a "trampoline" for the generated IL method
                //       body.  This is necessary because the new Delegate type
                //       logically resides outside of this assembly and can only
                //       access public members in this assembly.  Furthermore,
                //       in order for the Delegate and MethodInfo signatures to
                //       match up correctly (i.e. not throw runtime exceptions),
                //       this cannot be an instance method (why not?).
                //        
                methodInfo = CommandCallbackWrapper.dynamicInvokeMethodInfo;

                if (methodInfo == null)
                {
                    error = String.Format(
                        "missing \"{0}.StaticFireDynamicInvokeCallback\" method.",
                        typeof(CommandCallbackWrapper).Name);

                    return null;
                }
            }

            try
            {
                Interpreter interpreter = this.Interpreter;

                if (interpreter != null)
                {
                    if (newDelegateType != null)
                    {
                        TypeList newParameterTypes = new TypeList();

                        newParameterTypes.Add(typeof(ICallback));
                        newParameterTypes.AddRange(parameterTypes);

                        if (name == null)
                            name = DelegateOps.MakeDelegateName(interpreter);

                        DynamicMethod dynamicMethod = new DynamicMethod(
                            name, returnType, newParameterTypes.ToArray(),
                            GetType(), true);

                        ILGenerator generator = dynamicMethod.GetILGenerator();

                        DelegateOps.EmitDelegateWrapperMethodBody(
                            generator, methodInfo, returnType, parameterTypes,
                            true);

                        Delegate newDelegate = dynamicMethod.CreateDelegate(
                            newDelegateType, this);

                        if (newDelegate != null)
                        {
                            if (delegateType == null)
                                delegateType = newDelegateType;

                            return newDelegate;
                        }
                        else
                        {
                            error = String.Format(
                                "bad delegate of type {0} for dynamic method {1}",
                                newDelegateType, dynamicMethod);
                        }
                    }
                    else
                    {
                        TypeList newParameterTypes = new TypeList();

                        newParameterTypes.Add(typeof(object));
                        newParameterTypes.AddRange(parameterTypes);

                        Type newWrapperType = null;

                        if (DelegateOps.CreateManagedDelegateType(interpreter,
                                null, null, null, null, returnType, parameterTypes,
                                ref newDelegateType, ref error) == ReturnCode.Ok &&
                            DelegateOps.CreateDelegateWrapperMethod(
                                interpreter, null, null, null, null, methodInfo,
                                returnType, parameterTypes, ref newWrapperType,
                                ref error) == ReturnCode.Ok)
                        {
                            object newObject = Activator.CreateInstance(
                                newWrapperType);

                            if (CommandCallbackWrapper.Create(
                                    newObject, this, ref error) == ReturnCode.Ok)
                            {
                                MethodInfo newMethodInfo = newWrapperType.GetMethod(
                                    DelegateOps.InvokeMethodName);

                                Delegate newDelegate = Delegate.CreateDelegate(
                                    newDelegateType, newObject, newMethodInfo,
                                    throwOnBindFailure);

                                if (newDelegate != null)
                                {
                                    if (delegateType == null)
                                        delegateType = newDelegateType;

                                    return newDelegate;
                                }
                                else
                                {
                                    error = String.Format(
                                        "bad delegate of type {0} for method {1}",
                                        newDelegateType, newMethodInfo);
                                }
                            }
                        }
                    }
                }
                else
                {
                    error = "invalid interpreter";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode PrepareByRefArguments(
            Interpreter interpreter,
            ref ArgumentInfoList argumentInfoList,
            ref IntArgumentInfoDictionary argumentInfoDictionary,
            ref Result error
            )
        {
            if (parameterTypes == null)
                return ReturnCode.Ok;

            ReturnCode code = MarshalOps.GetByRefArgumentInfo(
                new TypeList(parameterTypes), parameterMarshalFlags,
                marshalFlags, ref argumentInfoList, ref error);

            if (code != ReturnCode.Ok)
                return code;

            if (argumentInfoList == null)
                return ReturnCode.Ok; /* NOTE: None are ByRef. */

            MarshalOps.SetupTemporaryByRefVariableNames(
                interpreter, argumentInfoList);

            foreach (ArgumentInfo argumentInfo in argumentInfoList)
            {
                if (argumentInfo == null)
                    continue;

                if (argumentInfoDictionary == null)
                    argumentInfoDictionary = new IntArgumentInfoDictionary();

                argumentInfoDictionary[argumentInfo.Index] = argumentInfo;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            ref Result result
            )
        {
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out asynchronous, out asynchronousIfBusy);

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                asynchronous, asynchronousIfBusy, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            ref Result result,
            ref int errorLine
            )
        {
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out asynchronous, out asynchronousIfBusy);

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                asynchronous, asynchronousIfBusy, ref result,
                ref errorLine);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            bool useOwner,
            bool resetCancel,
            bool asynchronous,
            bool asynchronousIfBusy,
            ref Result result
            )
        {
            int errorLine = 0;

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                asynchronous, asynchronousIfBusy, ref result,
                ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode Invoke(
            Interpreter interpreter,
            StringList arguments,
            bool useOwner,
            bool resetCancel,
            bool asynchronous,
            bool asynchronousIfBusy,
            ref Result result,
            ref int errorLine
            )
        {
            //
            // NOTE: Construct a new local argument list.
            //
            StringList localArguments = new StringList();

            //
            // NOTE: Start with the original (instance) arguments, if any.
            //
            AddArguments(localArguments);

            //
            // NOTE: Append the arguments for this invocation, if any.
            //
            AddArguments(localArguments, arguments);

            //
            // NOTE: Does the creator of this callback want the owner of the
            //       target interpreter to handle it, instead of simply using
            //       the target interpreter directly?
            //
            if (useOwner)
            {
                //
                // NOTE: Attempt to fetch the owner of the target interpreter.
                //       The owner must be an IScriptThread or an interpreter.
                //       If that is not the case (i.e. a null value or a value
                //       of an unsupported type is returned), the invocation
                //       of this callback will be considered a failure.
                //
                object owner = GetOwner(interpreter);

                if (owner is IScriptThread)
                {
                    return InvokeUsingScriptThread(
                        interpreter, (IScriptThread)owner, localArguments,
                        resetCancel, asynchronous, asynchronousIfBusy,
                        ref result, ref errorLine);
                }
                else if (owner is Interpreter)
                {
                    return InvokeUsingInterpreter(
                        (Interpreter)owner, localArguments, resetCancel,
                        asynchronous, asynchronousIfBusy, ref result,
                        ref errorLine);
                }
                else
                {
                    //
                    // NOTE: If the owner of the target interpreter is not
                    //       an IScriptThread, we do not (currently) know
                    //       how to deal with it.
                    //
                    result = String.Format(
                        "owner of callback interpreter not an {0} or {1}",
                        typeof(IScriptThread).Name, typeof(Interpreter).Name);

                    return ReturnCode.Error;
                }
            }
            else
            {
                return InvokeUsingInterpreter(
                    interpreter, localArguments, resetCancel, asynchronous,
                    asynchronousIfBusy, ref result, ref errorLine);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode CheckByRefArgumentType(
            int parameterIndex,
            string parameterName,
            Type parameterType,
            Type argType,
            bool byRefStrict,
            ref Result error
            )
        {
            if (byRefStrict || FlagOps.HasFlags(
                    byRefArgumentFlags, ByRefArgumentFlags.Strict, true))
            {
                if (!MarshalOps.IsSameValueType(
                        parameterType, argType) &&
                    !MarshalOps.IsSameReferenceType(
                        parameterType, argType, marshalFlags))
                {
                    error = String.Format(
                        "output parameter {0} type \"{1}\" does not match " +
                        "argument value type \"{2}\"", FormatOps.ArgumentName(
                        parameterIndex, parameterName), parameterType, argType);

                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode FixupByRefArguments(
            Interpreter interpreter,
            ArgumentInfoList argumentInfoList,
            object[] args,
            bool byRefStrict,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (argumentInfoList == null)
            {
                error = "invalid argument info";
                return ReturnCode.Error;
            }

            if (args == null)
            {
                error = "invalid argument array";
                return ReturnCode.Error;
            }

            int argumentLength = args.Length;

            foreach (ArgumentInfo argumentInfo in argumentInfoList)
            {
                if (argumentInfo == null)
                    continue;

                int parameterIndex = argumentInfo.Index;
                string parameterName = argumentInfo.Name;

                if ((parameterIndex < 0) || (parameterIndex >= args.Length))
                {
                    error = String.Format(
                        "output parameter {0} out-of-bounds, index {1} " +
                        "must be between 0 and {2}", FormatOps.ArgumentName(
                        parameterIndex, parameterName), parameterIndex,
                        argumentLength);

                    return ReturnCode.Error;
                }

                Result variableValue = null;

                if (interpreter.GetVariableValue(
                        VariableFlags.None, parameterName,
                        ref variableValue, ref error) == ReturnCode.Ok)
                {
                    //
                    // BUGBUG: This call uses 'true' for the 'addReference'
                    //         parameter to DoesObjectExist because the call
                    //         to unset the variable (just below) will remove
                    //         a reference and we do not want to automatically
                    //         dispose the underlying object(s), if any.  The
                    //         "correct" solution here would be to somehow
                    //         modify the UnsetVariable code to be capable of
                    //         skipping object reference removal and/or object
                    //         disposal; however, that would be complicated by
                    //         the fact that the UnsetVariable code (purposely)
                    //         has no knowledge of any specific variable traces
                    //         (including the object reference counting trace)
                    //         that may be present on a particular variable.
                    //
                    object arg = null;

                    if (interpreter.DoesObjectExist(variableValue,
                            true, false, ref arg) != ReturnCode.Ok)
                    {
                        arg = variableValue.Value;
                    }

                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, parameterName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, unsetCode, unsetError);

                    Type parameterType = argumentInfo.Type;

                    Type argType = (arg != null) ?
                        arg.GetType() : typeof(object);

                    if (CheckByRefArgumentType(
                            parameterIndex, parameterName, parameterType,
                            argType, byRefStrict, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    args[parameterIndex] = arg;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter; /* NOT OWNED */
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { CheckDisposed(); return kind; }
            set { CheckDisposed(); kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { CheckDisposed(); return id; }
            set { CheckDisposed(); id = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { CheckDisposed(); return name; }
            set { CheckDisposed(); name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { CheckDisposed(); return group; }
            set { CheckDisposed(); group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { CheckDisposed(); return description; }
            set { CheckDisposed(); description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { CheckDisposed(); return clientData; }
            set { CheckDisposed(); clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        private ObjectFlags objectFlags;
        public ObjectFlags ObjectFlags
        {
            get { CheckDisposed(); return objectFlags; }
            set { CheckDisposed(); objectFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICallback Members
        private MarshalFlags marshalFlags;
        public MarshalFlags MarshalFlags
        {
            get { CheckDisposed(); return marshalFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private CallbackFlags callbackFlags;
        public CallbackFlags CallbackFlags
        {
            get { CheckDisposed(); return callbackFlags; }
            set { CheckDisposed(); callbackFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ByRefArgumentFlags byRefArgumentFlags;
        public ByRefArgumentFlags ByRefArgumentFlags
        {
            get { CheckDisposed(); return byRefArgumentFlags; }
            set { CheckDisposed(); byRefArgumentFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringList arguments;
        public StringList Arguments
        {
            get { CheckDisposed(); return arguments; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Delegate @delegate;
        public Delegate Delegate
        {
            get { CheckDisposed(); return @delegate; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type delegateType;
        public Type DelegateType
        {
            get { CheckDisposed(); return delegateType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type returnType;
        public Type ReturnType
        {
            get { CheckDisposed(); return returnType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TypeList parameterTypes;
        public TypeList ParameterTypes
        {
            get { CheckDisposed(); return parameterTypes; }
        }

        ///////////////////////////////////////////////////////////////////////

        private MarshalFlagsList parameterMarshalFlags;
        public MarshalFlagsList ParameterMarshalFlags
        {
            get { CheckDisposed(); return parameterMarshalFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private AsyncCallback asyncCallback;
        public AsyncCallback AsyncCallback
        {
            get { CheckDisposed(); return asyncCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventHandler eventHandler;
        public EventHandler EventHandler
        {
            get { CheckDisposed(); return eventHandler; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ThreadStart threadStart;
        public ThreadStart ThreadStart
        {
            get { CheckDisposed(); return threadStart; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ParameterizedThreadStart parameterizedThreadStart;
        public ParameterizedThreadStart ParameterizedThreadStart
        {
            get { CheckDisposed(); return parameterizedThreadStart; }
        }

        ///////////////////////////////////////////////////////////////////////

        private GenericCallback genericCallback;
        public GenericCallback GenericCallback
        {
            get { CheckDisposed(); return genericCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DynamicInvokeCallback dynamicInvokeCallback;
        public DynamicInvokeCallback DynamicInvokeCallback
        {
            get { CheckDisposed(); return dynamicInvokeCallback; }
        }

        ///////////////////////////////////////////////////////////////////////

        public AsyncCallback GetAsyncCallback()
        {
            CheckDisposed();

            if (asyncCallback == null)
                asyncCallback = new AsyncCallback(FireAsyncCallback);

            return asyncCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        public EventHandler GetEventHandler()
        {
            CheckDisposed();

            if (eventHandler == null)
                eventHandler = new EventHandler(FireEventHandler);

            return eventHandler;
        }

        ///////////////////////////////////////////////////////////////////////

        public ThreadStart GetThreadStart()
        {
            CheckDisposed();

            if (threadStart == null)
                threadStart = new ThreadStart(FireThreadStart);

            return threadStart;
        }

        ///////////////////////////////////////////////////////////////////////

        public ParameterizedThreadStart GetParameterizedThreadStart()
        {
            CheckDisposed();

            if (parameterizedThreadStart == null)
                parameterizedThreadStart =
                    new ParameterizedThreadStart(FireParameterizedThreadStart);

            return parameterizedThreadStart;
        }

        ///////////////////////////////////////////////////////////////////////

        public GenericCallback GetGenericCallback()
        {
            CheckDisposed();

            if (genericCallback == null)
                genericCallback = new GenericCallback(FireGenericCallback);

            return genericCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        public DynamicInvokeCallback GetDynamicInvokeCallback()
        {
            CheckDisposed();

            if (dynamicInvokeCallback == null)
            {
                dynamicInvokeCallback = new DynamicInvokeCallback(
                    FireDynamicInvokeCallback);
            }

            return dynamicInvokeCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        public Delegate GetDelegate(
            Type delegateType,                      /* in */
            Type returnType,                        /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in: OPTIONAL */
            bool throwOnBindFailure,                /* in */
            ref Result error                        /* out */
            )
        {
            CheckDisposed();

            if ((@delegate == null) ||
                (this.delegateType != delegateType) ||
                (this.returnType != returnType) ||
                TypeList.Equals(
                    this.parameterTypes, parameterTypes))
            {
                //
                // NOTE: This is the method metadata that will contain the type
                //       signature for the method to invoke.
                //
                MethodInfo methodInfo = null;

                //
                // NOTE: Process the configured marshal flags into the
                //       various boolean flags.
                //
                bool useDelegateCallback;
                bool useGenericCallback;
                bool useDynamicCallback;

                ProcessMarshalFlags(
                    marshalFlags, out useDelegateCallback, out useGenericCallback,
                    out useDynamicCallback);

                //
                // NOTE: Determine if the specified delegate type is just the
                //       System.Delegate type itself (this requires some special
                //       handling).
                //
                bool isDelegate = ConversionOps.IsDelegate(delegateType);

                //
                // WARNING: This method is tricky because of the ThreadStart and
                //          GenericCallback delegate types.  They will both match
                //          any delegate method signature that has no parameters
                //          and no return value.
                //
                if (useDynamicCallback ||
                    ConversionOps.IsDynamicInvokeCallback(delegateType))
                {
                    if (dynamicInvokeCallback == null)
                    {
                        dynamicInvokeCallback = new DynamicInvokeCallback(
                            FireDynamicInvokeCallback);
                    }

                    methodInfo = DelegateOps.GetInvokeMethod(
                        isDelegate ? typeof(DynamicInvokeCallback) :
                        delegateType);
                }
                else if (ConversionOps.IsAsyncCallback(delegateType))
                {
                    if (asyncCallback == null)
                        asyncCallback = new AsyncCallback(FireAsyncCallback);

                    methodInfo = asyncCallback.Method;
                }
                else if (ConversionOps.IsEventHandler(delegateType))
                {
                    if (eventHandler == null)
                        eventHandler = new EventHandler(FireEventHandler);

                    methodInfo = eventHandler.Method;
                }
                else if (!isDelegate && ConversionOps.IsThreadStart(delegateType))
                {
                    if (threadStart == null)
                        threadStart = new ThreadStart(FireThreadStart);

                    methodInfo = threadStart.Method;
                }
                else if (ConversionOps.IsParameterizedThreadStart(delegateType))
                {
                    if (parameterizedThreadStart == null)
                    {
                        parameterizedThreadStart = new ParameterizedThreadStart(
                            FireParameterizedThreadStart);
                    }

                    methodInfo = parameterizedThreadStart.Method;
                }
                //
                // HACK: Fake that "System.Delegate" really means they want a
                //       delegate with no parameters and no return value (i.e.
                //       the same signature as ThreadStart and GenericCallback).
                //
                else if ((useDelegateCallback && isDelegate) ||
                    (useGenericCallback &&
                        ConversionOps.IsGenericCallback(delegateType)))
                {
                    if (genericCallback == null)
                    {
                        genericCallback = new GenericCallback(
                            FireGenericCallback);
                    }

                    methodInfo = genericCallback.Method;
                }

                if (methodInfo != null)
                {
                    if (useDynamicCallback)
                    {
                        Type newReturnType;
                        TypeList newParameterTypes;

                        DelegateOps.GetReturnAndParameterTypes(
                            methodInfo, out newReturnType,
                            out newParameterTypes);

                        //
                        // BUGFIX: Do not re-create a new dynamic delegate
                        //         if one of the correct type information
                        //         has already been created.
                        //
                        if ((@delegate != null) &&
                            (this.delegateType == delegateType) &&
                            (this.returnType == newReturnType) &&
                            TypeList.Equals(
                                this.parameterTypes, newParameterTypes))
                        {
                            return @delegate;
                        }

                        if (returnType != null)
                            newReturnType = returnType;

                        if (parameterTypes != null)
                            newParameterTypes = parameterTypes;

                        Type newDelegateType;

                        if (isDelegate)
                            newDelegateType = null;
                        else
                            newDelegateType = delegateType;

                        @delegate = GetDynamicDelegate(
                            null, newReturnType, newParameterTypes,
                            parameterMarshalFlags, throwOnBindFailure,
                            ref newDelegateType, ref error);

                        if (@delegate != null)
                        {
                            this.delegateType = newDelegateType;
                            this.returnType = newReturnType;
                            this.parameterTypes = newParameterTypes;
                            this.parameterMarshalFlags = parameterMarshalFlags;
                        }
                    }
                    else
                    {
                        Type newDelegateType;

                        if (isDelegate)
                            newDelegateType = typeof(GenericCallback);
                        else
                            newDelegateType = delegateType;

                        @delegate = Delegate.CreateDelegate(
                            newDelegateType, this, methodInfo,
                            throwOnBindFailure); /* throw */

                        if (@delegate != null)
                        {
                            Type newReturnType;
                            TypeList newParameterTypes;

                            DelegateOps.GetReturnAndParameterTypes(
                                methodInfo, out newReturnType,
                                out newParameterTypes);

                            if (returnType != null)
                                newReturnType = returnType;

                            if (parameterTypes != null)
                                newParameterTypes = parameterTypes;

                            this.delegateType = newDelegateType;
                            this.returnType = newReturnType;
                            this.parameterTypes = newParameterTypes;
                            this.parameterMarshalFlags = parameterMarshalFlags;
                        }
                    }
                }
                else
                {
                    error = ScriptOps.BadValue(
                        "unsupported", "delegate type", delegateType.FullName,
                        new string[] {
                            useDelegateCallback ?
                                typeof(Delegate).FullName : null,
                            typeof(AsyncCallback).FullName,
                            typeof(EventHandler).FullName,
                            typeof(ThreadStart).FullName,
                            typeof(ParameterizedThreadStart).FullName,
                            useGenericCallback ?
                                typeof(GenericCallback).FullName : null,
                            useDynamicCallback ?
                                typeof(DynamicInvokeCallback).FullName : null
                        }, null, null);
                }
            }

            return @delegate;
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireAsyncCallback(
            IAsyncResult ar
            ) /* System.AsyncCallback */
        {
            CheckDisposed();

            FireAsyncCallback(ar, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireAsyncCallback(
            IAsyncResult ar,
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out asynchronous,
                out asynchronousIfBusy, out fireAndForget, out complain,
                out disposeThread, out throwOnError);

            Interpreter interpreter = this.Interpreter;
            ReturnCode code;
            Result result = null;

            //
            // NOTE: If the interpreter cannot currently be used for script
            //       evaluation, bail out now.
            //
            if (!CheckInterpreter(interpreter, ref result))
            {
                code = ReturnCode.Error;
                goto done;
            }

            StringList localArguments = null;

            //
            // NOTE: Do we want to create opaque object handles for the
            //       event arguments?
            //
            if (needArguments)
            {
                //
                // NOTE: Add an opaque object handle to the interpreter
                //       for the sender of the event.
                //
                code = MarshalOps.FixupReturnValue(
                    interpreter, interpreter.Binder,
                    interpreter.CultureInfo, null, objectFlags,
                    ObjectOps.GetInvokeOptions(objectOptionType),
                    objectOptionType, null, null, ar, create,
                    dispose, alias, aliasReference, toString,
                    ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                //
                // NOTE: Add the "ar" argument to the list.
                //
                AddArgument(result, ref localArguments);
            }

            //
            // NOTE: Were any extra arguments supplied by the caller?
            //       If so, add them now.
            //
            AddArguments(arguments, ref localArguments);

            //
            // NOTE: Invoke the callback (i.e. evaluate the script).
            //
            code = Invoke(
                interpreter, localArguments, useOwner, resetCancel,
                asynchronous, asynchronousIfBusy, ref result);

        done:

            try
            {
                if (code != ReturnCode.Ok)
                {
                    if (complain)
                        DebugOps.Complain(interpreter, code, result);

                    if (throwOnError)
                        throw new ScriptException(code, result);
                }
            }
            finally
            {
                if (fireAndForget)
                {
                    ReturnCode removeCode;
                    Result removeResult = null;

                    removeCode = interpreter.RemoveCallback(
                        name, _ClientData.Empty, ref removeResult);

                    if (complain && (removeCode != ReturnCode.Ok))
                    {
                        DebugOps.Complain(
                            interpreter, removeCode, removeResult);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireEventHandler(
            object sender,
            EventArgs e
            ) /* System.EventHandler */
        {
            CheckDisposed();

            FireEventHandler(sender, e, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireEventHandler(
            object sender,
            EventArgs e,
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out asynchronous,
                out asynchronousIfBusy, out fireAndForget, out complain,
                out disposeThread, out throwOnError);

            Interpreter interpreter = this.Interpreter;
            ReturnCode code;
            Result result = null;

            //
            // NOTE: If the interpreter cannot currently be used for script
            //       evaluation, bail out now.
            //
            if (!CheckInterpreter(interpreter, ref result))
            {
                code = ReturnCode.Error;
                goto done;
            }

            StringList localArguments = null;

            //
            // NOTE: Do we want to create opaque object handles for the
            //       event arguments?
            //
            if (needArguments)
            {
                //
                // NOTE: Add an opaque object handle to the interpreter
                //       for the sender of the event.
                //
                code = MarshalOps.FixupReturnValue(
                    interpreter, interpreter.Binder,
                    interpreter.CultureInfo, null, objectFlags,
                    ObjectOps.GetInvokeOptions(objectOptionType),
                    objectOptionType, null, null, sender, create,
                    dispose, alias, aliasReference, toString,
                    ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                //
                // NOTE: Add the "sender" argument to the list.
                //
                AddArgument(result, ref localArguments);

                //
                // NOTE: Add an opaque object handle to the interpreter
                //       for the data of the event.
                //
                code = MarshalOps.FixupReturnValue(
                    interpreter, interpreter.Binder,
                    interpreter.CultureInfo, null, objectFlags,
                    ObjectOps.GetInvokeOptions(objectOptionType),
                    objectOptionType, null, null, e, create,
                    dispose, alias, aliasReference, toString,
                    ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                //
                // NOTE: Add the "e" argument to the list.
                //
                localArguments.Add(result);
            }

            //
            // NOTE: Were any extra arguments supplied by the caller?
            //       If so, add them now.
            //
            AddArguments(arguments, ref localArguments);

            //
            // NOTE: Invoke the callback (i.e. evaluate the script).
            //
            code = Invoke(
                interpreter, localArguments, useOwner, resetCancel,
                asynchronous, asynchronousIfBusy, ref result);

        done:

            try
            {
                if (code != ReturnCode.Ok)
                {
                    if (complain)
                        DebugOps.Complain(interpreter, code, result);

                    if (throwOnError)
                        throw new ScriptException(code, result);
                }
            }
            finally
            {
                if (fireAndForget)
                {
                    ReturnCode removeCode;
                    Result removeResult = null;

                    removeCode = interpreter.RemoveCallback(
                        name, _ClientData.Empty, ref removeResult);

                    if (complain && (removeCode != ReturnCode.Ok))
                    {
                        DebugOps.Complain(
                            interpreter, removeCode, removeResult);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireThreadStart() /* System.Threading.ThreadStart */
        {
            CheckDisposed();

            bool shouldCatchInterrupt = ShouldCatchInterrupt(callbackFlags);

            try
            {
                FireThreadStart(null);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError);

                if (!shouldCatchInterrupt)
                    throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireThreadStart(
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags to figure out if
            //       we should complain about failures that would otherwise
            //       be unreportable -AND- if we should attempt to dispose
            //       thread-specific data after completion of the specified
            //       script.
            //
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out asynchronous, out asynchronousIfBusy,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError);

            Interpreter interpreter = this.Interpreter;

            try
            {
                ReturnCode code;
                Result result = null;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                StringList localArguments = null;

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    asynchronous, asynchronousIfBusy, ref result);

            done:

                try
                {
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                //
                // NOTE: Cleanup all the thread-specific data for the current
                //       interpreter (for this thread, which is now exiting)
                //       unless this is the primary thread for the interpreter
                //       -OR- it is actively in use elsewhere in this thread.
                //
                if (disposeThread && (interpreter != null))
                {
                    /* IGNORED */
                    interpreter.MaybeDisposeThread();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireParameterizedThreadStart(
            object obj
            ) /* System.Threading.ParameterizedThreadStart */
        {
            CheckDisposed();

            bool shouldCatchInterrupt = ShouldCatchInterrupt(callbackFlags);

            try
            {
                FireParameterizedThreadStart(obj, null);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(CommandCallback).Name,
                    TracePriority.ThreadError);

                if (!shouldCatchInterrupt)
                    throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireParameterizedThreadStart(
            object obj,
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out asynchronous,
                out asynchronousIfBusy, out fireAndForget, out complain,
                out disposeThread, out throwOnError);

            Interpreter interpreter = this.Interpreter;

            try
            {
                ReturnCode code;
                Result result = null;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                StringList localArguments = null;

                //
                // NOTE: Do we want to create opaque object handles for the
                //       event arguments?
                //
                if (needArguments)
                {
                    //
                    // NOTE: Add an opaque object handle to the interpreter
                    //       for the object parameter.
                    //
                    code = MarshalOps.FixupReturnValue(
                        interpreter, interpreter.Binder,
                        interpreter.CultureInfo, null, objectFlags,
                        ObjectOps.GetInvokeOptions(objectOptionType),
                        objectOptionType, null, null, obj, create,
                        dispose, alias, aliasReference, toString,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;

                    //
                    // NOTE: Add the "obj" argument to the list.
                    //
                    AddArgument(result, ref localArguments);
                }

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    asynchronous, asynchronousIfBusy, ref result);

            done:

                try
                {
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                //
                // NOTE: Cleanup all the thread-specific data for the current
                //       interpreter (for this thread, which is now exiting)
                //       unless this is the primary thread for the interpreter
                //       -OR- it is actively in use elsewhere in this thread.
                //
                if (disposeThread && (interpreter != null))
                {
                    /* IGNORED */
                    interpreter.MaybeDisposeThread();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /* Eagle._Components.Public.Delegates.GenericCallback */
        public void FireGenericCallback()
        {
            CheckDisposed();

            FireGenericCallback(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void FireGenericCallback(
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags to figure out if
            //       we should complain about failures that would otherwise
            //       be unreportable -AND- if we should attempt to dispose
            //       thread-specific data after completion of the specified
            //       script.
            //
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out asynchronous, out asynchronousIfBusy,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError);

            Interpreter interpreter = this.Interpreter;

            try
            {
                ReturnCode code;
                Result result = null;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                StringList localArguments = null;

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    asynchronous, asynchronousIfBusy, ref result);

            done:

                try
                {
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                //
                // NOTE: Cleanup all the thread-specific data for the current
                //       interpreter (for this thread, which is now exiting)
                //       unless this is the primary thread for the interpreter
                //       -OR- it is actively in use elsewhere in this thread.
                //
                if (disposeThread && (interpreter != null))
                {
                    /* IGNORED */
                    interpreter.MaybeDisposeThread();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /* System.Delegate.DynamicInvoke */
        public object FireDynamicInvokeCallback(
            params object[] args
            )
        {
            CheckDisposed();

            return FireDynamicInvokeCallback(args, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public object FireDynamicInvokeCallback(
            object[] args,
            StringList arguments
            )
        {
            CheckDisposed();

            //
            // NOTE: Process the configured callback flags into the various
            //       boolean flags.
            //
            ObjectOptionType objectOptionType;
            bool needArguments;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;
            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;
            bool byRefStrict;
            bool returnValue;
            bool defaultValue;
            bool addReference;
            bool removeReference;
            bool fireAndForget;
            bool complain;
            bool disposeThread;
            bool throwOnError;

            ProcessCallbackFlags(
                callbackFlags, out objectOptionType, out needArguments,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString,
                out useOwner, out resetCancel, out asynchronous,
                out asynchronousIfBusy, out byRefStrict, out returnValue,
                out defaultValue, out addReference, out removeReference,
                out fireAndForget, out complain, out disposeThread,
                out throwOnError);

            Interpreter interpreter = this.Interpreter;

            try
            {
                ReturnCode code;
                Result result = null;
                int errorLine = 0;

                //
                // NOTE: If the interpreter cannot currently be used for
                //       script evaluation, bail out now.
                //
                if (!CheckInterpreter(interpreter, ref result))
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                ArgumentInfoList argumentInfoList = null;
                StringList localArguments = null;

                OptionDictionary options = ObjectOps.GetInvokeOptions(
                    objectOptionType);

                //
                // NOTE: Do we want to create opaque object handles for the
                //       callback arguments?
                //
                if (needArguments && (args != null) && (args.Length > 0))
                {
                    IntArgumentInfoDictionary argumentInfoDictionary = null;

                    code = PrepareByRefArguments(interpreter,
                        ref argumentInfoList, ref argumentInfoDictionary,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;

                    for (int index = 0; index < args.Length; index++)
                    {
                        object arg = args[index];

                        code = MarshalOps.FixupReturnValue(
                            interpreter, interpreter.Binder,
                            interpreter.CultureInfo, null, objectFlags,
                            options, objectOptionType, null, null, arg,
                            create, dispose, alias, aliasReference,
                            toString, ref result);

                        if (code != ReturnCode.Ok)
                            goto done;

                        string argString = result;

                        if (argumentInfoDictionary != null)
                        {
                            ArgumentInfo argumentInfo;

                            if (argumentInfoDictionary.TryGetValue(
                                    index, out argumentInfo) &&
                                (argumentInfo != null))
                            {
                                string argName = argumentInfo.Name;

                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, argName, argString,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    goto done;

                                argString = argName;
                            }
                        }

                        AddArgument(argString, ref localArguments);
                    }
                }

                //
                // NOTE: Were any extra arguments supplied by the caller?
                //       If so, add them now.
                //
                AddArguments(arguments, ref localArguments);

                //
                // NOTE: Invoke the callback (i.e. evaluate the script).
                //
                code = Invoke(
                    interpreter, localArguments, useOwner, resetCancel,
                    asynchronous, asynchronousIfBusy, ref result,
                    ref errorLine);

                if (code != ReturnCode.Ok)
                    goto done;

                //
                // NOTE: Handle any ByRef arguments that may be present.
                //
                if (argumentInfoList != null)
                {
                    code = FixupByRefArguments(
                        interpreter, argumentInfoList, args, byRefStrict,
                        ref result);

                    if (code != ReturnCode.Ok)
                        goto done;
                }

            done:

                try
                {
                    //
                    // NOTE: If necessary, complain prior to the next block,
                    //       because it needs to return (i.e. more than one
                    //       possible value).
                    //
                    if (code != ReturnCode.Ok)
                    {
                        if (complain)
                            DebugOps.Complain(interpreter, code, result);

                        if (throwOnError)
                            throw new ScriptException(code, result);
                    }

                    //
                    // NOTE: This does not apply for successful return codes
                    //       when the string result is a valid opaque object
                    //       handle.
                    //
                    if (returnValue && (code == ReturnCode.Ok))
                    {
                        if (defaultValue)
                        {
                            return MarshalOps.GetDefaultValue(returnType);
                        }
                        else
                        {
                            object value = null;

                            if (interpreter.DoesObjectExist(
                                    result, addReference, removeReference,
                                    ref value) == ReturnCode.Ok)
                            {
                                return value;
                            }
                        }
                    }

                    //
                    // HACK: Just use the formatted result as the return value,
                    //       which will be just the script result itself when
                    //       successful.
                    //
                    return ResultOps.Format(code, result, errorLine);
                }
                finally
                {
                    if (fireAndForget)
                    {
                        ReturnCode removeCode;
                        Result removeResult = null;

                        removeCode = interpreter.RemoveCallback(
                            name, _ClientData.Empty, ref removeResult);

                        if (complain && (removeCode != ReturnCode.Ok))
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeResult);
                        }
                    }
                }
            }
            finally
            {
                //
                // NOTE: Cleanup all the thread-specific data for the current
                //       interpreter (for this thread, which is now exiting)
                //       unless this is the primary thread for the interpreter
                //       -OR- it is actively in use elsewhere in this thread.
                //
                // HACK: This is necessary for dynamic delegate usage due to
                //       the (remote?) possibility that a dynamic delegate is
                //       used in place of a thread-start delegate.
                //
                if (disposeThread && (interpreter != null))
                {
                    /* IGNORED */
                    interpreter.MaybeDisposeThread();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Invoke(
            StringList arguments,
            ref Result result
            )
        {
            CheckDisposed();

            int errorLine = 0;

            return Invoke(arguments, ref result, ref errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Invoke(
            StringList arguments,
            ref Result result,
            ref int errorLine
            )
        {
            CheckDisposed();

            bool useOwner;
            bool resetCancel;
            bool asynchronous;
            bool asynchronousIfBusy;

            ProcessCallbackFlags(
                callbackFlags, out useOwner, out resetCancel,
                out asynchronous, out asynchronousIfBusy);

            return Invoke(
                interpreter, arguments, useOwner, resetCancel,
                asynchronous, asynchronousIfBusy, ref result,
                ref errorLine);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData, /* NOT USED */
            ArgumentList arguments,
            ref Result result
            )
        {
            CheckDisposed();

            return Invoke(interpreter, (arguments != null) ?
                new StringList(arguments) : null, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            StringList localArguments = this.arguments;

            if (localArguments == null)
                return String.Empty;

            return localArguments.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
            {
                throw new ObjectDisposedException(
                    typeof(CommandCallback).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: Make sure to remove any references to this
                    //       instance from the public callback wrapper.
                    //
                    /* IGNORED */
                    CommandCallbackWrapper.Cleanup(this);

                    //
                    // NOTE: The contained interpreter is NOT OWNED by
                    //       this object; therefore, DO NOT dispose it.
                    //
                    if (interpreter != null)
                        interpreter = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~CommandCallback()
        {
            Dispose(false);
        }
        #endregion
    }
}
