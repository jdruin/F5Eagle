/*
 * Library.cs --
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
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("a4d151e8-05d7-4051-9dc3-80665197ccd5")]
    [CommandFlags(CommandFlags.NativeCode | CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("nativeEnvironment")]
    internal sealed class Library : Core
    {
        public Library(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "call", "certificate", "checkload", "declare", "handle",
            "info", "load", "resolve", "test", "undeclare", "unload",
            "unresolve"
        });

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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
                                case "call":
                                    {
                                        //
                                        // NOTE: Example: library call ?options? delegate ?arg ...?
                                        //
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetCallOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    BindingFlags bindingFlags;
                                                    MarshalFlags marshalFlags;
                                                    ReorderFlags reorderFlags;
                                                    ByRefArgumentFlags byRefArgumentFlags;
                                                    int limit;
                                                    int index;
                                                    bool noByRef;
                                                    bool strictMember;
                                                    bool strictArgs;
                                                    bool invoke;
                                                    bool noArgs;
                                                    bool arrayAsValue;
                                                    bool arrayAsLink;
                                                    bool trace;

                                                    ObjectOps.ProcessFindMethodsAndFixupArgumentsOptions(
                                                        interpreter, options, null, null, null, null,
                                                        out bindingFlags, out marshalFlags, out reorderFlags,
                                                        out byRefArgumentFlags, out limit, out index,
                                                        out noByRef, out strictMember, out strictArgs,
                                                        out invoke, out noArgs, out arrayAsValue,
                                                        out arrayAsLink, out trace);

                                                    Type returnType;
                                                    ObjectFlags objectFlags;
                                                    ObjectFlags byRefObjectFlags;
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
                                                        options, null, null, out returnType, out objectFlags,
                                                        out byRefObjectFlags, out objectName, out interpName,
                                                        out create, out dispose, out alias, out aliasRaw,
                                                        out aliasAll, out aliasReference, out toString);

                                                    string delegateName = arguments[argumentIndex];
                                                    IDelegate @delegate = null;

                                                    code = interpreter.GetDelegate(
                                                        delegateName, LookupFlags.Default,
                                                        ref @delegate, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        object[] args = null;
                                                        int argumentCount = 0;

                                                        if ((argumentIndex + 1) < arguments.Count)
                                                        {
                                                            //
                                                            // NOTE: How many arguments were supplied?
                                                            //
                                                            argumentCount = (arguments.Count - (argumentIndex + 1));

                                                            //
                                                            // NOTE: Create and populate the array of arguments for the
                                                            //       invocation.
                                                            //
                                                            args = new object[argumentCount];

                                                            for (int index2 = (argumentIndex + 1); index2 < arguments.Count; index2++)
                                                                /* need String, not Argument */
                                                                args[index2 - (argumentIndex + 1)] = arguments[index2].String;
                                                        }
                                                        else if (invoke || !noArgs)
                                                        {
                                                            //
                                                            // FIXME: When no arguments are specified, we actually need an array
                                                            //        of zero arguments for the parameter to argument matching
                                                            //        code to work correctly.
                                                            //
                                                            args = new object[0];
                                                        }

                                                        //
                                                        // HACK: We want to use the existing marshalling code; therefore,
                                                        //       we pre-bake some of the required arguments here (i.e. we
                                                        //       KNOW what method we are going to call, however we want
                                                        //       magical bi-directional type coercion, etc).
                                                        //
                                                        MethodInfo delegateMethodInfo = @delegate.MethodInfo;

                                                        if (delegateMethodInfo != null)
                                                        {
                                                            string newObjectName = @delegate.FunctionName;
                                                            string newMemberName = delegateMethodInfo.Name;
                                                            MethodInfo[] methodInfo = new MethodInfo[] { delegateMethodInfo };

                                                            if (methodInfo != null) // NOTE: Redundant [for now].
                                                            {
                                                                IntList methodIndexList = null;
                                                                ObjectArrayList argsList = null;
                                                                IntArgumentInfoListDictionary argumentInfoListDictionary = null;
                                                                ResultList errors = null;

                                                                //
                                                                // NOTE: Attempt to convert the argument strings to something
                                                                //       potentially more meaningful and find the corresponding
                                                                //       method.
                                                                //
                                                                code = MarshalOps.FindMethodsAndFixupArguments(
                                                                    interpreter, interpreter.Binder, options,
                                                                    interpreter.CultureInfo, @delegate.GetType(),
                                                                    newObjectName, newObjectName, newMemberName,
                                                                    newMemberName, MemberTypes.Method, bindingFlags,
                                                                    methodInfo, null, null, null, args, limit,
                                                                    marshalFlags, ref methodIndexList, ref argsList,
                                                                    ref argumentInfoListDictionary, ref errors);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if ((methodIndexList != null) && (methodIndexList.Count > 0) &&
                                                                        (argsList != null) && (argsList.Count > 0))
                                                                    {
                                                                        if ((index == Index.Invalid) ||
                                                                            ((index >= 0) && (index < methodIndexList.Count) &&
                                                                            (index < argsList.Count)))
                                                                        {
                                                                            if (FlagOps.HasFlags(
                                                                                    marshalFlags, MarshalFlags.ReorderMatches, true))
                                                                            {
                                                                                IntList savedMethodIndexList = new IntList(
                                                                                    methodIndexList);

                                                                                code = MarshalOps.ReorderMethodIndexes(
                                                                                    interpreter.Binder, interpreter.CultureInfo,
                                                                                    methodInfo, marshalFlags, reorderFlags,
                                                                                    ref methodIndexList, ref argsList, ref errors);

                                                                                if (code == ReturnCode.Ok)
                                                                                {
                                                                                    if (trace)
                                                                                    {
                                                                                        TraceOps.DebugTrace(String.Format(
                                                                                            "Execute: savedMethodIndexList = {0}, " +
                                                                                            "methodIndexList = {1}",
                                                                                            savedMethodIndexList, methodIndexList),
                                                                                            typeof(Library).Name, TracePriority.Command);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = errors;
                                                                                }
                                                                            }

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                ObjectOptionType objectOptionType = ObjectOptionType.Call |
                                                                                    ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                                                if (invoke)
                                                                                {
                                                                                    if (!strictMember || (methodIndexList.Count == 1))
                                                                                    {
                                                                                        //
                                                                                        // FIXME: By default, select the first method that matches.
                                                                                        //        However, the configured script binder can override
                                                                                        //        this behavior via the SelectMethodIndex method.
                                                                                        //        More sophisticated logic may need to be added here
                                                                                        //        later.
                                                                                        //
                                                                                        int methodIndex;

                                                                                        if (index != Index.Invalid)
                                                                                        {
                                                                                            methodIndex = methodIndexList[index];
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            methodIndex = MarshalOps.SelectMethodIndex(
                                                                                                interpreter, interpreter.Binder,
                                                                                                interpreter.CultureInfo, @delegate.GetType(),
                                                                                                methodInfo, null, null, args, methodIndexList,
                                                                                                argsList, marshalFlags, reorderFlags);
                                                                                        }

                                                                                        if (methodIndex != Index.Invalid)
                                                                                        {
                                                                                            try
                                                                                            {
                                                                                                //
                                                                                                // NOTE: Get the arguments we are going to use to perform
                                                                                                //       the actual method call.
                                                                                                //
                                                                                                args = (index != Index.Invalid) ? argsList[index] : argsList[0];

                                                                                                ArgumentInfoList argumentInfoList;

                                                                                                /* IGNORED */
                                                                                                MarshalOps.TryGetArgumentInfoList(argumentInfoListDictionary,
                                                                                                    methodIndex, out argumentInfoList);

                                                                                                if (trace)
                                                                                                {
                                                                                                    TraceOps.DebugTrace(String.Format(
                                                                                                        "Execute: methodIndex = {0}, delegate = {1}, " +
                                                                                                        "args = {2}, argumentInfoList = {3}",
                                                                                                        methodIndex, FormatOps.WrapOrNull(@delegate),
                                                                                                        FormatOps.WrapOrNull(new StringList(args)),
                                                                                                        FormatOps.WrapOrNull(argumentInfoList)),
                                                                                                        typeof(Library).Name, TracePriority.Command);
                                                                                                }

                                                                                                object returnValue = null;

                                                                                                code = @delegate.Invoke(args, ref returnValue, ref result);

                                                                                                if ((code == ReturnCode.Ok) &&
                                                                                                    !noByRef && (argumentInfoList != null))
                                                                                                {
                                                                                                    code = MarshalOps.FixupByRefArguments(
                                                                                                        interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                                        argumentInfoList, objectFlags | byRefObjectFlags,
                                                                                                        options, objectOptionType, interpName, args,
                                                                                                        marshalFlags, byRefArgumentFlags, strictArgs, create,
                                                                                                        dispose, alias, aliasReference, toString, arrayAsValue,
                                                                                                        arrayAsLink, ref result);
                                                                                                }

                                                                                                if (code == ReturnCode.Ok)
                                                                                                {
                                                                                                    code = MarshalOps.FixupReturnValue(
                                                                                                        interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                                        returnType, objectFlags, options, objectOptionType,
                                                                                                        objectName, interpName, returnValue, create, dispose,
                                                                                                        alias, aliasReference, toString, ref result);
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
                                                                                                "method \"{0}\" of delegate \"{1}\" not found",
                                                                                                newMemberName, newObjectName);

                                                                                            code = ReturnCode.Error;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Format(
                                                                                            "matched {0} method overloads of \"{1}\" on delegate \"{2}\", need exactly 1",
                                                                                             methodIndexList.Count, newMemberName, newObjectName);

                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    MethodInfoList methodInfoList = new MethodInfoList();

                                                                                    if (index != Index.Invalid)
                                                                                        methodInfoList.Add(methodInfo[methodIndexList[index]]);
                                                                                    else
                                                                                        foreach (int methodIndex in methodIndexList)
                                                                                            methodInfoList.Add(methodInfo[methodIndex]);

                                                                                    code = MarshalOps.FixupReturnValue(
                                                                                        interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                        returnType, objectFlags, options, objectOptionType,
                                                                                        objectName, interpName, methodInfoList, create, dispose,
                                                                                        alias, aliasReference, toString, ref result);
                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "method \"{0}\" of delegate \"{1}\" not found, " +
                                                                                "invalid method index {2}, must be {3}",
                                                                                newMemberName, newObjectName, index,
                                                                                FormatOps.BetweenOrExact(0, methodIndexList.Count - 1));

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        result = String.Format(
                                                                            "method \"{0}\" of delegate \"{1}\" not found",
                                                                            newMemberName, newObjectName);

                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = errors;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "delegate \"{0}\" has no methods matching \"{1}\"",
                                                                    newObjectName, bindingFlags);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "cannot call delegate \"{0}\", it is unresolved",
                                                                delegateName);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"library call ?options? delegate ?arg ...?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library call ?options? delegate ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "certificate":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetCertificateOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    X509VerificationFlags x509VerificationFlags;
                                                    X509RevocationMode x509RevocationMode;
                                                    X509RevocationFlag x509RevocationFlag;
                                                    bool chain;

                                                    ObjectOps.ProcessObjectCertificateOptions(
                                                        options, null, null, null, out x509VerificationFlags,
                                                        out x509RevocationMode, out x509RevocationFlag,
                                                        out chain);

                                                    if (chain)
                                                    {
                                                        X509Certificate2 certificate2 = null;

                                                        code = CertificateOps.GetCertificate2(
                                                            arguments[argumentIndex], ref certificate2, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            code = CertificateOps.VerifyChain(
                                                                null, certificate2, x509VerificationFlags,
                                                                x509RevocationMode, x509RevocationFlag,
                                                                ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = FormatOps.Certificate(
                                                                arguments[argumentIndex], certificate2, true, true, false);
                                                    }
                                                    else
                                                    {
                                                        X509Certificate certificate = null;

                                                        code = CertificateOps.GetCertificate(
                                                            arguments[argumentIndex], ref certificate, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = FormatOps.Certificate(
                                                                arguments[argumentIndex], certificate, true, true, false);
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
                                                        result = "wrong # args: should be \"library certificate ?options? fileName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library certificate ?options? fileName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "declare":
                                    {
                                        //
                                        // NOTE: Example: library declare -returnType type -parameterTypes typeList
                                        //
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-alias", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-module", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-functionname", null),
                                                new Option(null, OptionFlags.MustHaveWideIntegerValue, Index.Invalid, Index.Invalid, "-address", null),
                                                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid, Index.Invalid, "-returntype", null),
                                                new Option(null, OptionFlags.MustHaveTypeListValue, Index.Invalid, Index.Invalid, "-parametertypes", null),
                                                new Option(typeof(CallingConvention), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-callingconvention", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-assemblyname", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-modulename", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-typename", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-bestfitmapping", null),
                                                new Option(typeof(CharSet), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-charset", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-setlasterror", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-throwonunmappablechar", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-delegatename", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    bool alias = false;

                                                    if (options.IsPresent("-alias"))
                                                        alias = true;

                                                    Variant value = null;
                                                    Type returnType = typeof(void);

                                                    if (options.IsPresent("-returntype", ref value))
                                                        returnType = (Type)value.Value;

                                                    TypeList parameterTypes = null;

                                                    if (options.IsPresent("-parametertypes", ref value))
                                                        parameterTypes = (TypeList)value.Value;

                                                    CallingConvention callingConvention = CallingConvention.Winapi; // TODO: Good default?

                                                    if (options.IsPresent("-callingconvention", ref value))
                                                        callingConvention = (CallingConvention)value.Value;

                                                    string functionName = null;

                                                    if (options.IsPresent("-functionname", ref value))
                                                        functionName = value.ToString();

                                                    IntPtr address = IntPtr.Zero;

                                                    if (options.IsPresent("-address", ref value))
                                                        address = new IntPtr((long)value.Value); /* NOTE: DANGEROUS. */

                                                    string assemblyName = null;

                                                    if (options.IsPresent("-assemblyname", ref value))
                                                        assemblyName = value.ToString();

                                                    string moduleName = null;

                                                    if (options.IsPresent("-modulename", ref value))
                                                        moduleName = value.ToString();

                                                    string typeName = null;

                                                    if (options.IsPresent("-typename", ref value))
                                                        typeName = value.ToString();

                                                    bool bestFitMapping = true;

                                                    if (options.IsPresent("-bestfitmapping", ref value))
                                                        bestFitMapping = (bool)value.Value;

                                                    CharSet charSet = (CharSet)0; /* NOTE: .NET Framework default, per MSDN. */

                                                    if (options.IsPresent("-charset", ref value))
                                                        charSet = (CharSet)value.Value;

                                                    bool setLastError = false;

                                                    if (options.IsPresent("-setlasterror", ref value))
                                                        setLastError = (bool)value.Value;

                                                    bool throwOnUnmappableChar = false;

                                                    if (options.IsPresent("-throwonunmappablechar", ref value))
                                                        throwOnUnmappableChar = (bool)value.Value;

                                                    string delegateName = null;

                                                    if (options.IsPresent("-delegatename", ref value))
                                                        delegateName = value.ToString();

                                                    IModule module = null;

                                                    if (options.IsPresent("-module", ref value))
                                                    {
                                                        code = interpreter.GetModule(
                                                            value.ToString(), LookupFlags.Default,
                                                            ref module, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IDelegate @delegate = null;

                                                        code = DelegateOps.CreateNativeDelegateType(
                                                            interpreter, null, (assemblyName != null) ? new AssemblyName(assemblyName) : null,
                                                            moduleName, typeName, callingConvention, bestFitMapping, charSet, setLastError,
                                                            throwOnUnmappableChar, returnType, parameterTypes, delegateName, module,
                                                            functionName, address, ref @delegate, ref result);

                                                        if ((code == ReturnCode.Ok) && (module != null) && (functionName != null))
                                                            code = @delegate.Resolve(null, null, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = interpreter.AddDelegate(@delegate, clientData, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                //
                                                                // NOTE: Grab the name of the newly added delegate.
                                                                //
                                                                string newDelegateName = @delegate.Name;

                                                                //
                                                                // NOTE: Create an alias for the new Tcl interpreter handle?
                                                                //
                                                                if ((code == ReturnCode.Ok) && alias)
                                                                {
                                                                    code = interpreter.AddLibraryAlias(
                                                                        newDelegateName, ObjectOps.GetCallOptions(),
                                                                        ObjectOptionType.Call, ref result);
                                                                }

                                                                //
                                                                // NOTE: Return the name of the new delegate upon success.
                                                                //
                                                                if (code == ReturnCode.Ok)
                                                                    result = newDelegateName;
                                                            }
                                                            else
                                                            {
                                                                ReturnCode disposeCode;
                                                                Result disposeError = null;

                                                                disposeCode = ObjectOps.TryDispose(@delegate, ref disposeError);

                                                                if (disposeCode != ReturnCode.Ok)
                                                                    DebugOps.Complain(interpreter, disposeCode, disposeError);
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"library declare ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library declare ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "handle":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IntPtr handle = NativeOps.GetModuleHandle(arguments[2]);

                                            result = PlatformOps.Is64BitProcess() ?
                                                handle.ToInt64() : handle.ToInt32();

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library handle fileName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "info":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            switch (arguments[2])
                                            {
                                                case "delegate":
                                                    {
                                                        string delegateName = arguments[3];
                                                        IDelegate @delegate = null;

                                                        code = interpreter.GetDelegate(
                                                            delegateName, LookupFlags.Default,
                                                            ref @delegate, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            Guid id = AttributeOps.GetObjectId(@delegate);

                                                            result = StringList.MakeList(
                                                                "kind", @delegate.Kind,
                                                                "id", @delegate.Id.Equals(Guid.Empty) ? id : @delegate.Id,
                                                                "name", @delegate.Name,
                                                                "description", @delegate.Description,
                                                                "callingConvention", @delegate.CallingConvention,
                                                                "returnType", (@delegate.ReturnType != null) ? @delegate.ReturnType : null,
                                                                "parameterTypes", (@delegate.ParameterTypes != null) ? @delegate.ParameterTypes : null,
                                                                "typeId", (@delegate.Type != null) ? (object)@delegate.Type.GUID : null,
                                                                "typeName", (@delegate.Type != null) ? @delegate.Type : null,
                                                                "moduleFlags", (@delegate.Module != null) ? (object)@delegate.Module.Flags : null,
                                                                "moduleName", (@delegate.Module != null) ? @delegate.Module.Name : null,
                                                                "moduleFileName", (@delegate.Module != null) ? @delegate.Module.FileName : null,
                                                                "moduleReferenceCount", (@delegate.Module != null) ? (object)@delegate.Module.ReferenceCount : null,
                                                                "functionName", @delegate.FunctionName,
                                                                "address", @delegate.Address);
                                                        }
                                                        break;
                                                    }
                                                case "module":
                                                    {
                                                        string moduleName = arguments[3];
                                                        IModule module = null;

                                                        code = interpreter.GetModule(
                                                            moduleName, LookupFlags.Default,
                                                            ref module, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            Guid id = AttributeOps.GetObjectId(module);

                                                            result = StringList.MakeList(
                                                                "kind", module.Kind,
                                                                "id", module.Id.Equals(Guid.Empty) ? id : module.Id,
                                                                "name", module.Name,
                                                                "description", module.Description,
                                                                "flags", module.Flags,
                                                                "fileName", module.FileName,
                                                                "module", module.Module,
                                                                "referenceCount", module.ReferenceCount);
                                                        }
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        result = String.Format(
                                                            "unsupported library object type \"{0}\"",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                        break;
                                                    }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library info type object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "checkload":
                                case "load":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            bool check = (String.Compare(subCommand, "checkload", StringOps.SystemStringComparisonType) == 0);

                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-modulename", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-locked", null),
                                                new Option(typeof(ModuleFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-flags",
                                                    new Variant(ModuleFlags.None)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, true,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    string fileName = arguments[argumentIndex];
                                                    string moduleName = null;
                                                    bool load = true;

                                                    if (check)
                                                    {
                                                        //
                                                        // NOTE: If the native module has already been
                                                        //       loaded, just skip it.
                                                        //
                                                        if (interpreter.GetModuleByFileName(
                                                                fileName, LookupFlags.NoVerbose,
                                                                ref moduleName) == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: Yes, it was already loaded.  Make
                                                            //       sure to disable further loading.
                                                            //
                                                            load = false;

                                                            //
                                                            // NOTE: Just like a real "load", return
                                                            //       the module handle name.
                                                            //
                                                            result = moduleName;
                                                        }
                                                    }

                                                    if (load)
                                                    {
                                                        bool locked = false;

                                                        if (options.IsPresent("-locked"))
                                                            locked = true;

                                                        Variant value = null;
                                                        ModuleFlags flags = ModuleFlags.None;

                                                        if (options.IsPresent("-flags", ref value))
                                                            flags = (ModuleFlags)value.Value;

                                                        //
                                                        // NOTE: Lock the module in place, thereby preventing it from
                                                        //       being unloaded (at least, by us)?
                                                        //
                                                        if (locked)
                                                            flags |= ModuleFlags.NoUnload;

                                                        if (options.IsPresent("-modulename", ref value))
                                                            moduleName = value.ToString();
                                                        else
                                                            moduleName = DelegateOps.MakeIModuleName(interpreter);

                                                        IModule module = null;

                                                        code = DelegateOps.LoadNativeModule(
                                                            interpreter, flags, fileName, moduleName, ref module,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = interpreter.AddModule(module, clientData, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                //
                                                                // NOTE: Return the name of the new module upon success.
                                                                //
                                                                result = moduleName;
                                                            }
                                                            else
                                                            {
                                                                ReturnCode disposeCode;
                                                                Result disposeError = null;

                                                                disposeCode = ObjectOps.TryDispose(module, ref disposeError);

                                                                if (disposeCode != ReturnCode.Ok)
                                                                    DebugOps.Complain(interpreter, disposeCode, disposeError);
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
                                                        result = String.Format(
                                                            "wrong # args: should be \"library {0} ?options? fileName\"",
                                                            subCommand);
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"library {0} ?options? fileName\"",
                                                subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resolve":
                                    {
                                        //
                                        // NOTE: Example: library resolve ?options? delegate
                                        //
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-module", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-functionname", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    string delegateName = arguments[argumentIndex];

                                                    //
                                                    // NOTE: Check for and process the optional module name
                                                    //       argument.
                                                    //
                                                    Variant value = null;
                                                    string moduleName = null;

                                                    if (options.IsPresent("-module", ref value))
                                                        moduleName = value.ToString();

                                                    //
                                                    // NOTE: Check for and process the optional function name
                                                    //       argument.
                                                    //
                                                    string functionName = null;

                                                    if (options.IsPresent("-functionname", ref value))
                                                        functionName = value.ToString();

                                                    IModule module = null;

                                                    if (moduleName != null)
                                                    {
                                                        code = interpreter.GetModule(
                                                            moduleName, LookupFlags.Default,
                                                            ref module, ref result);
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Ok;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IDelegate @delegate = null;

                                                        code = interpreter.GetDelegate(
                                                            delegateName, LookupFlags.Default,
                                                            ref @delegate, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: We are going to modify the interpreter state, make
                                                            //       sure it is not set to read-only.  In this particular
                                                            //       case we are modifying the interpreter state indirectly,
                                                            //       by resolving a native delegate previously added.  This
                                                            //       restriction on resolving native delegates in read-only
                                                            //       interpreters may need to be relaxed or removed later.
                                                            //
                                                            if (interpreter.IsModifiable(false, ref result))
                                                            {
                                                                //
                                                                // NOTE: Using previously created NativeModule and NativeDelegate
                                                                //       objects, hookup the unmanaged function pointer to it.
                                                                //
                                                                code = @delegate.Resolve(module, functionName, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = String.Empty;
                                                            }
                                                            else
                                                            {
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
                                                        result = "wrong # args: should be \"library resolve ?options? delegate\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library resolve ?options? delegate\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "test":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string fileName = (arguments.Count == 3) ? arguments[2] : null;

                                            code = NativeOps.TestLoadLibrary(fileName, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library test ?fileName?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "undeclare":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string delegateName = arguments[2];

                                            if ((interpreter.DoesAliasExist(
                                                    delegateName) != ReturnCode.Ok) ||
                                                (interpreter.RemoveAliasAndCommand(
                                                    delegateName, clientData, false,
                                                    ref result) == ReturnCode.Ok))
                                            {
                                                code = interpreter.RemoveDelegate(
                                                    delegateName, clientData,
                                                    ObjectOps.GetDefaultSynchronous(),
                                                    ref result);

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
                                            result = "wrong # args: should be \"library undeclare delegate\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unload":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string moduleName = arguments[2];

                                            code = interpreter.RemoveModule(
                                                moduleName, clientData, ObjectOps.GetDefaultSynchronous(),
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library unload module\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unresolve":
                                    {
                                        //
                                        // NOTE: Example: library unresolve ?options? delegate
                                        //
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    string delegateName = arguments[argumentIndex];
                                                    IDelegate @delegate = null;

                                                    code = interpreter.GetDelegate(
                                                        delegateName, LookupFlags.Default,
                                                        ref @delegate, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // NOTE: We are going to modify the interpreter state, make
                                                        //       sure it is not set to read-only.  In this particular
                                                        //       case we are modifying the interpreter state indirectly,
                                                        //       by unresolving a native delegate previously added.  This
                                                        //       restriction on unresolving native delegates in read-only
                                                        //       interpreters may need to be relaxed or removed later.
                                                        //
                                                        if (interpreter.IsModifiable(false, ref result))
                                                        {
                                                            //
                                                            // NOTE: Using previously created NativeModule and NativeDelegate
                                                            //       objects, hookup the unmanaged function pointer to it.
                                                            //
                                                            code = @delegate.Unresolve(ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = String.Empty;
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
                                                        result = "wrong # args: should be \"library unresolve ?options? delegate\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"library unresolve ?options? delegate\"";
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
                        result = "wrong # args: should be \"library option ?arg ...?\"";
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
