/*
 * Object.cs --
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
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("95dc42f9-d1f9-467b-acdc-3312d4bcdfee")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Object : Core
    {
        public Object(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "addref", "alias", "aliasnamespaces", "assemblies",
            "callbackflags", "certificate", "cleanup",
            "create", "declare", "dispose", "exists",
            "flags", "foreach", "get", "hash", "import",
            "interfaces", "invoke", "invokeall", "invokeraw",
            "isoftype", "list", "lmap", "load", "members",
            "namespaces", "refcount", "removecallback",
            "removeref", "resolve", "search", "strongname",
            "type", "types", "unalias", "unaliasnamespace",
            "undeclare", "unimport", "untype", "verifyall"
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
            PolicyOps.DefaultAllowedObjectSubCommandNames);

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
                                case "addref":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = interpreter.AddObjectReference(
                                                code, arguments[2], ObjectReferenceType.Demand,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object addref object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "alias":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetAliasOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    TypeList objectTypes;
                                                    bool verbose;
                                                    bool strictType;
                                                    bool noCase;
                                                    bool aliasRaw;
                                                    bool aliasAll;
                                                    bool aliasReference;

                                                    ObjectOps.ProcessObjectAliasOptions(
                                                        options, out objectTypes, out verbose,
                                                        out strictType, out noCase, out aliasRaw,
                                                        out aliasAll, out aliasReference);

                                                    IObject @object = null;
                                                    Type type = null;

                                                    if (interpreter.GetObject(
                                                            arguments[argumentIndex], LookupFlags.NoVerbose,
                                                            ref @object) == ReturnCode.Ok)
                                                    {
                                                        if (@object.Value != null)
                                                        {
                                                            type = @object.Value.GetType();
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "invalid value for object {0}",
                                                                FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ResultList errors = null;

                                                        code = Value.GetType(
                                                            interpreter, arguments[argumentIndex], objectTypes,
                                                            interpreter.GetAppDomain(), Value.GetTypeValueFlags(
                                                            strictType, verbose, noCase), interpreter.CultureInfo,
                                                            ref type, ref errors);

                                                        if (code != ReturnCode.Ok)
                                                        {
                                                            errors.Insert(0, String.Format(
                                                                "object or type {0} not found",
                                                                FormatOps.WrapOrNull(arguments[argumentIndex])));

                                                            result = errors;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IAlias alias = null;

                                                        ObjectOptionType objectOptionType = ObjectOptionType.Alias |
                                                            ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                        code = interpreter.AddObjectAlias(
                                                            arguments[argumentIndex], null,
                                                            ObjectOps.GetInvokeOptions(objectOptionType),
                                                            objectOptionType, aliasReference, ref alias,
                                                            ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = alias.ToString();
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
                                                        result = "wrong # args: should be \"object alias ?options? object\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object alias ?options? object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "aliasnamespaces":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 3)
                                                    pattern = arguments[2];

                                                StringDictionary dictionary = interpreter.ObjectAliasNamespaces;

                                                if (dictionary != null)
                                                    result = dictionary.KeysAndValuesToString(pattern, false);
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object aliasnamespaces ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "assemblies":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            AppDomain appDomain = interpreter.GetAppDomain();

                                            if (appDomain != null)
                                            {
                                                StringList list = new StringList(
                                                    appDomain.GetAssemblies());

                                                string pattern = null;

                                                if (arguments.Count == 3)
                                                    pattern = arguments[2];

                                                result = list.ToString(pattern, false);
                                            }
                                            else
                                            {
                                                result = "invalid application domain";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object assemblies ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "callbackflags":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            ICallback callback = null;

                                            code = interpreter.GetCallback(
                                                arguments[2], LookupFlags.Default,
                                                ref callback, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // HACK: This following causes a compiler error with the C#
                                                //       v2.0 compiler (i.e. the one included with the .NET
                                                //       Framework 2.0 SP2) because the CallbackFlags enum
                                                //       has a value named "ToString":
                                                //
                                                //       callback.Flags.ToString()
                                                //
                                                //       Therefore, use a temporary object as a workaround.
                                                //
                                                object flags = callback.CallbackFlags;

                                                if (arguments.Count == 4)
                                                {
                                                    object enumValue = EnumOps.TryParseFlagsEnum(
                                                        interpreter, typeof(CallbackFlags),
                                                        flags.ToString(),
                                                        arguments[3], interpreter.CultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is CallbackFlags)
                                                        callback.CallbackFlags = (CallbackFlags)enumValue;
                                                    else
                                                        code = ReturnCode.Error;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    //
                                                    // HACK: See above comment for details on this.
                                                    //
                                                    flags = callback.CallbackFlags;
                                                    result = flags.ToString();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object callbackflags name ?flags?\"";
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

                                                    IObject @object = null;

                                                    code = interpreter.GetObject(
                                                        arguments[argumentIndex], LookupFlags.Default,
                                                        ref @object, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Assembly assembly = (@object != null) ?
                                                            @object.Value as Assembly : null;

                                                        if (chain)
                                                        {
                                                            if (assembly != null)
                                                            {
                                                                X509Certificate2 certificate2 = null;

                                                                code = AssemblyOps.GetCertificate2(
                                                                    assembly, true, ref certificate2, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    code = CertificateOps.VerifyChain(
                                                                        assembly, certificate2, x509VerificationFlags,
                                                                        x509RevocationMode, x509RevocationFlag,
                                                                        ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = FormatOps.Certificate(
                                                                        assembly, certificate2, true, true, false);
                                                            }
                                                            else
                                                            {
                                                                result = "invalid assembly";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            X509Certificate certificate = null;

                                                            code = AssemblyOps.GetCertificate(
                                                                assembly, ref certificate, ref result);

                                                            if (code == ReturnCode.Ok)
                                                                result = FormatOps.Certificate(
                                                                    assembly, certificate, true, true, false);
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
                                                        result = "wrong # args: should be \"object certificate ?options? assembly\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object certificate ?options? assembly\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cleanup":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetCleanupOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                            {
                                                code = interpreter.GetOptions(
                                                    options, arguments, 0, 2, Index.Invalid, true,
                                                    ref argumentIndex, ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    bool references = false;

                                                    if (options.IsPresent("-references"))
                                                        references = true;

                                                    bool remove = true;

                                                    if (options.IsPresent("-noremove"))
                                                        remove = false;

                                                    bool synchronous = false;

                                                    if (options.IsPresent("-synchronous"))
                                                        synchronous = true;

                                                    bool dispose = true; /* EXEMPT */

                                                    if (options.IsPresent("-nodispose"))
                                                        dispose = false;

                                                    bool stopOnError = true;

                                                    if (options.IsPresent("-nocomplain"))
                                                        stopOnError = false;

                                                    if (references)
                                                    {
                                                        code = interpreter.CleanupObjectReferences(
                                                            true, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (remove)
                                                        {
                                                            code = interpreter.RemoveObjects(
                                                                null, stopOnError, synchronous, ref dispose,
                                                                ref result);
                                                        }
                                                        else
                                                        {
                                                            result = StringList.MakeList(
                                                                "disposed", 0, "removed", 0);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object cleanup ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object cleanup ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "create":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetCreateOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    TypeList objectTypes;
                                                    TypeList methodTypes;
                                                    TypeList parameterTypes;
                                                    MarshalFlagsList parameterMarshalFlags;
                                                    BindingFlags bindingFlags;
                                                    MarshalFlags marshalFlags;
                                                    ReorderFlags reorderFlags;
                                                    ByRefArgumentFlags byRefArgumentFlags;
                                                    int limit;
                                                    int index;
                                                    bool noByRef;
                                                    bool strictType;
                                                    bool strictMember;
                                                    bool strictArgs;
                                                    bool noCase;
                                                    bool invoke;
                                                    bool noArgs;
                                                    bool arrayAsValue;
                                                    bool arrayAsLink;
                                                    bool trace;

                                                    ObjectOps.ProcessFindMethodsAndFixupArgumentsOptions(
                                                        interpreter, options, null, null, null, null,
                                                        out objectTypes, out methodTypes, out parameterTypes,
                                                        out parameterMarshalFlags, out bindingFlags,
                                                        out marshalFlags, out reorderFlags,
                                                        out byRefArgumentFlags, out limit, out index,
                                                        out noByRef, out strictType, out strictMember,
                                                        out strictArgs, out noCase, out invoke, out noArgs,
                                                        out arrayAsValue, out arrayAsLink, out trace);

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

                                                    if (noCase)
                                                        objectFlags |= ObjectFlags.NoCase;

                                                    //
                                                    // NOTE: We intend to modify the interpreter state,
                                                    //       make sure this is not forbidden.
                                                    //
                                                    if (interpreter.IsModifiable(true, ref result))
                                                    {
                                                        Type objectType = null;
                                                        ResultList errors = null;

                                                        code = Value.GetType(
                                                            interpreter, arguments[argumentIndex], objectTypes,
                                                            interpreter.GetAppDomain(), Value.GetTypeValueFlags(
                                                            strictType, FlagOps.HasFlags(marshalFlags,
                                                            MarshalFlags.Verbose, true), noCase),
                                                            interpreter.CultureInfo, ref objectType, ref errors);

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

                                                            ObjectOptionType objectOptionType = ObjectOptionType.Create |
                                                                ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                            ConstructorInfo[] constructorInfo = objectType.GetConstructors(bindingFlags);

                                                            if ((constructorInfo != null) && (constructorInfo.Length > 0))
                                                            {
                                                                IntList methodIndexList = null;
                                                                ObjectArrayList argsList = null;
                                                                IntArgumentInfoListDictionary argumentInfoListDictionary = null;

                                                                //
                                                                // NOTE: Attempt to convert the argument strings to something
                                                                //       potentially more meaningful and find the corresponding
                                                                //       constructor.
                                                                //
                                                                errors = null;

                                                                code = MarshalOps.FindMethodsAndFixupArguments(
                                                                    interpreter, interpreter.Binder, options, interpreter.CultureInfo,
                                                                    objectType, arguments[argumentIndex], arguments[argumentIndex],
                                                                    null, null, MemberTypes.Constructor, bindingFlags, constructorInfo,
                                                                    methodTypes, parameterTypes, parameterMarshalFlags, args, limit,
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
                                                                                    constructorInfo, marshalFlags, reorderFlags,
                                                                                    ref methodIndexList, ref argsList,
                                                                                    ref errors);

                                                                                if (code == ReturnCode.Ok)
                                                                                {
                                                                                    if (trace)
                                                                                    {
                                                                                        TraceOps.DebugTrace(String.Format(
                                                                                            "Execute: savedMethodIndexList = {0}, " +
                                                                                            "methodIndexList = {1}",
                                                                                            savedMethodIndexList, methodIndexList),
                                                                                            typeof(Object).Name, TracePriority.Command);
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = errors;
                                                                                }
                                                                            }

                                                                            if (code == ReturnCode.Ok)
                                                                            {
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
                                                                                            methodIndex = MarshalOps.SelectMethodIndex(interpreter,
                                                                                                interpreter.Binder, interpreter.CultureInfo, objectType,
                                                                                                constructorInfo, parameterTypes, parameterMarshalFlags,
                                                                                                args, methodIndexList, argsList, marshalFlags,
                                                                                                reorderFlags);
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
                                                                                                        "Execute: methodIndex = {0}, constructorInfo = {1}, " +
                                                                                                        "args = {2}, argumentInfoList = {3}",
                                                                                                        methodIndex,
                                                                                                        FormatOps.WrapOrNull(constructorInfo[methodIndex]),
                                                                                                        FormatOps.WrapOrNull(new StringList(args)),
                                                                                                        FormatOps.WrapOrNull(argumentInfoList)),
                                                                                                        typeof(Object).Name, TracePriority.Command);
                                                                                                }

                                                                                                object @object = constructorInfo[methodIndex].Invoke(
                                                                                                    bindingFlags, interpreter.Binder as Binder, args, interpreter.CultureInfo);

                                                                                                if (@object != null)
                                                                                                {
                                                                                                    if (!noByRef && (argumentInfoList != null))
                                                                                                    {
                                                                                                        code = MarshalOps.FixupByRefArguments(
                                                                                                            interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                                            argumentInfoList, objectFlags | byRefObjectFlags,
                                                                                                            ObjectOps.GetInvokeOptions(objectOptionType),
                                                                                                            objectOptionType, interpName, args, marshalFlags,
                                                                                                            byRefArgumentFlags, strictArgs, create, dispose,
                                                                                                            alias, aliasReference, toString, arrayAsValue,
                                                                                                            arrayAsLink, ref result);
                                                                                                    }

                                                                                                    if (code == ReturnCode.Ok)
                                                                                                    {
                                                                                                        code = MarshalOps.FixupReturnValue(
                                                                                                            interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                                            returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                                                                            objectOptionType), objectOptionType, objectName, interpName,
                                                                                                            @object, create, dispose, alias, aliasReference, toString,
                                                                                                            ref result);
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    result = String.Format(
                                                                                                        "could not create an instance of type {0}",
                                                                                                        MarshalOps.GetErrorTypeName(objectType));

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
                                                                                            result = String.Format(
                                                                                                "type {0} constructor not found",
                                                                                                MarshalOps.GetErrorTypeName(objectType));

                                                                                            code = ReturnCode.Error;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Format(
                                                                                            "matched {0} constructor overloads on type {1}, need exactly 1",
                                                                                             methodIndexList.Count, MarshalOps.GetErrorTypeName(objectType));

                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    ConstructorInfoList constructorInfoList = new ConstructorInfoList();

                                                                                    if (index != Index.Invalid)
                                                                                    {
                                                                                        constructorInfoList.Add(constructorInfo[methodIndexList[index]]);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        foreach (int methodIndex in methodIndexList)
                                                                                            constructorInfoList.Add(constructorInfo[methodIndex]);
                                                                                    }

                                                                                    code = MarshalOps.FixupReturnValue(
                                                                                        interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                        returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                                                        objectOptionType), objectOptionType, objectName, interpName,
                                                                                        constructorInfoList, create, dispose, alias, aliasReference,
                                                                                        toString, ref result);
                                                                                }
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "type {0} constructor not found, " +
                                                                                "invalid method index {1}, must be {2}",
                                                                                MarshalOps.GetErrorTypeName(objectType), index,
                                                                                FormatOps.BetweenOrExact(0, methodIndexList.Count - 1));

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        result = String.Format(
                                                                            "type {0} constructor not found",
                                                                            MarshalOps.GetErrorTypeName(objectType));

                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = errors;
                                                                }
                                                            }
                                                            //
                                                            // NOTE: These (primitive and value types) may not have any
                                                            //       constructors (e.g. System.Int32).
                                                            //
                                                            else if (invoke && (objectType.IsPrimitive || objectType.IsValueType))
                                                            {
                                                                if (index == Index.Invalid)
                                                                {
                                                                    try
                                                                    {
                                                                        object @object = Activator.CreateInstance(
                                                                            objectType, bindingFlags, interpreter.Binder as Binder, args,
                                                                            interpreter.CultureInfo);

                                                                        if (@object != null)
                                                                        {
                                                                            code = MarshalOps.FixupReturnValue(
                                                                                interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                                                objectOptionType), objectOptionType, objectName, interpName,
                                                                                @object, create, dispose, alias, aliasReference, toString,
                                                                                ref result);
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "could not create an instance of primitive type {0}",
                                                                                MarshalOps.GetErrorTypeName(objectType));

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
                                                                    result = String.Format(
                                                                        "type {0} constructor not found, " +
                                                                        "cannot specify method index {1} for primitive type",
                                                                        MarshalOps.GetErrorTypeName(objectType), index);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "type {0} has no constructors matching {1}",
                                                                    MarshalOps.GetErrorTypeName(objectType),
                                                                    FormatOps.WrapOrNull(bindingFlags));

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            errors.Insert(0, String.Format(
                                                                "type {0} not found",
                                                                FormatOps.WrapOrNull(arguments[argumentIndex])));

                                                            result = errors;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object create ?options? typeName ?arg ...?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object create ?options? typeName ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "declare":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetDeclareOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                MatchMode matchMode;
                                                string pattern;
                                                bool verbose;
                                                bool strictType;
                                                bool nonPublic;
                                                bool noCase;

                                                ObjectOps.ProcessObjectDeclareOptions(
                                                    options, null, out matchMode, out pattern,
                                                    out verbose, out strictType, out nonPublic,
                                                    out noCase);

                                                //
                                                // NOTE: Check for a pattern without a mode (change to
                                                //       default, which is Glob).
                                                //
                                                if ((pattern != null) && (matchMode == MatchMode.None))
                                                    matchMode = StringOps.DefaultObjectMatchMode;

                                                //
                                                // NOTE: Figure out which interfaces they want to declare (add).
                                                //
                                                TypeList types = new TypeList();

                                                //
                                                // NOTE: Any specific interfaces they want to declare (add)?
                                                //
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    StringList list = new StringList(ArgumentList.GetRange(
                                                        arguments, argumentIndex, Index.Invalid));

                                                    ResultList errors = null;

                                                    code = Value.GetTypeList(interpreter,
                                                        list.ToString(), interpreter.GetAppDomain(),
                                                        Value.GetTypeValueFlags(strictType, verbose, noCase),
                                                        interpreter.CultureInfo, ref types, ref errors);

                                                    if (code != ReturnCode.Ok)
                                                        result = errors;
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    //
                                                    // NOTE: Ok, add the specified interfaces now.
                                                    //
                                                    code = interpreter.AddObjectInterfaces(
                                                        types, nonPublic, matchMode, pattern,
                                                        noCase, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object declare ?options? ?name name ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "dispose":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetDisposeOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    bool synchronous = false;

                                                    if (options.IsPresent("-synchronous"))
                                                        synchronous = true;

                                                    bool dispose = true; /* EXEMPT */

                                                    if (options.IsPresent("-nodispose"))
                                                        dispose = false;

                                                    bool stopOnError = true;

                                                    if (options.IsPresent("-nocomplain"))
                                                        stopOnError = false;

                                                    int removed = 0;
                                                    int disposed = 0;

                                                    for (; argumentIndex < arguments.Count; argumentIndex++)
                                                    {
                                                        bool localDispose = dispose;
                                                        Result localResult = null;

                                                        code = interpreter.RemoveObject(
                                                            arguments[argumentIndex], null, synchronous,
                                                            ref localDispose, ref localResult);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            removed++;

                                                            if (localDispose)
                                                                disposed++;
                                                        }
                                                        else if (stopOnError)
                                                        {
                                                            result = localResult;
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: The "-nocomplain" option is enabled;
                                                            //       therefore, reset the return code to
                                                            //       success just in case we exit the loop.
                                                            //
                                                            code = ReturnCode.Ok;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (removed > 1)
                                                            result = StringList.MakeList(
                                                                "disposed", disposed, "removed", removed);
                                                        else if (disposed > 0)
                                                            result = "disposed";
                                                        else if (removed > 0)
                                                            result = "removed";
                                                        else
                                                            result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object dispose ?options? ?object ...?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object dispose ?options? ?object ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = (interpreter.DoesObjectExist(arguments[2]) == ReturnCode.Ok);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object exists object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "flags":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            IObject @object = null;

                                            code = interpreter.GetObject(
                                                arguments[2], LookupFlags.Default,
                                                ref @object, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (arguments.Count == 4)
                                                {
                                                    object enumValue = EnumOps.TryParseFlagsEnum(
                                                        interpreter, typeof(ObjectFlags),
                                                        @object.ObjectFlags.ToString(),
                                                        arguments[3], interpreter.CultureInfo,
                                                        true, true, true, ref result);

                                                    if (enumValue is ObjectFlags)
                                                    {
                                                        //
                                                        // NOTE: We intend to modify the interpreter state,
                                                        //       make sure this is not forbidden.
                                                        //
                                                        if (interpreter.IsModifiable(false, ref result))
                                                            @object.ObjectFlags = (ObjectFlags)enumValue;
                                                        else
                                                            code = ReturnCode.Error;
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = @object.ObjectFlags;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object flags object ?flags?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "foreach":
                                case "lmap":
                                    {
                                        if (arguments.Count >= 5)
                                        {
                                            OptionDictionary options = ObjectOps.GetForEachOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 3) == arguments.Count))
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

                                                    Variant value = null;
                                                    bool collect = (String.Compare(subCommand, "lmap",
                                                        StringOps.SystemStringComparisonType) == 0);

                                                    if (options.IsPresent("-collect", ref value))
                                                        collect = (bool)value.Value;

                                                    bool synchronous = false;

                                                    if (options.IsPresent("-synchronous"))
                                                        synchronous = true;

                                                    IObject @object = null;

                                                    code = interpreter.GetObject(
                                                        arguments[argumentIndex + 1], LookupFlags.Default,
                                                        ref @object, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IEnumerable enumerable = (@object != null) ?
                                                            @object.Value as IEnumerable : null;

                                                        if (enumerable != null)
                                                        {
                                                            IEnumerator enumerator = null;
                                                            Result localError = null;

                                                            try
                                                            {
                                                                enumerator = enumerable.GetEnumerator(); /* throw */
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                localError = e;
                                                            }

                                                            if (enumerator != null)
                                                            {
                                                                string varName = arguments[argumentIndex];
                                                                string body = arguments[argumentIndex + 2];
                                                                IScriptLocation location = arguments[argumentIndex + 2];
                                                                StringList resultList = collect ? new StringList() : null;

                                                                //
                                                                // NOTE: Move to the next item.  If this fails, there are
                                                                //       no more items and we cannot move any farther.
                                                                //
                                                                while (code == ReturnCode.Ok)
                                                                {
                                                                    object newValue = null;

                                                                    try
                                                                    {
                                                                        //
                                                                        // NOTE: See if there are any more items.
                                                                        //
                                                                        if (!enumerator.MoveNext())
                                                                            break;

                                                                        //
                                                                        // NOTE: Get the value of the current item.
                                                                        //
                                                                        newValue = enumerator.Current; /* may be null */
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        result = e;
                                                                        code = ReturnCode.Error;
                                                                    }

                                                                    //
                                                                    // NOTE: If we caught an exception while getting the
                                                                    //       current item, record the error and stop now.
                                                                    //
                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        Engine.AddErrorInformation(interpreter, result,
                                                                            String.Format("{0}    (advancing object {1} enumerator \"{2}\"",
                                                                                Environment.NewLine, subCommand, FormatOps.Ellipsis(varName)));

                                                                        break;
                                                                    }

                                                                    MarshalOps.CheckForStickyAlias(
                                                                        @object, ref objectFlags, ref alias);

                                                                    ObjectOptionType objectOptionType = ObjectOptionType.ForEach |
                                                                        ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                                    //
                                                                    // NOTE: Now create an object handle for it.
                                                                    //
                                                                    code = MarshalOps.FixupReturnValue(
                                                                        interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                        returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                                        objectOptionType), objectOptionType, objectName, interpName,
                                                                        newValue, create, dispose, alias, aliasReference, toString,
                                                                        ref result);

                                                                    //
                                                                    // NOTE: If we fail at adding the object handle, bail
                                                                    //       out now.
                                                                    //
                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        Engine.AddErrorInformation(interpreter, result,
                                                                            String.Format("{0}    (adding object {1} object handle \"{2}\"",
                                                                                Environment.NewLine, subCommand, FormatOps.Ellipsis(varName)));

                                                                        break;
                                                                    }

                                                                    //
                                                                    // NOTE: Save the object name just in case we need to
                                                                    //       dispose it (below).
                                                                    //
                                                                    string newObjectName = result;

                                                                    //
                                                                    // NOTE: Set the value of the variable to the created
                                                                    //       object handle.
                                                                    //
                                                                    code = interpreter.SetVariableValue(
                                                                        VariableFlags.None, varName, newObjectName, null, ref result);

                                                                    //
                                                                    // NOTE: If we fail at setting the loop variable, bail
                                                                    //       out now.
                                                                    //
                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        ReturnCode removeCode;
                                                                        Result removeResult = null;

                                                                        removeCode = interpreter.RemoveObject(
                                                                            newObjectName, null, synchronous, ref removeResult);

                                                                        if (removeCode != ReturnCode.Ok)
                                                                            DebugOps.Complain(interpreter, removeCode, removeResult);

                                                                        Engine.AddErrorInformation(interpreter, result,
                                                                            String.Format("{0}    (setting object {1} loop variable \"{2}\"",
                                                                                Environment.NewLine, subCommand, FormatOps.Ellipsis(varName)));

                                                                        break;
                                                                    }

                                                                    //
                                                                    // NOTE: Evaluate the loop body.  If this fails, we bail
                                                                    //       out, leaving the loop variable and associated
                                                                    //       object handle untouched.
                                                                    //
                                                                    code = interpreter.EvaluateScript(body, location, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        if (collect && (resultList != null))
                                                                            resultList.Add(result);
                                                                    }
                                                                    else
                                                                    {
                                                                        if (code == ReturnCode.Continue)
                                                                        {
                                                                            code = ReturnCode.Ok;
                                                                        }
                                                                        else if (code == ReturnCode.Break)
                                                                        {
                                                                            code = ReturnCode.Ok;
                                                                            break;
                                                                        }
                                                                        else if (code == ReturnCode.Error)
                                                                        {
                                                                            Engine.AddErrorInformation(interpreter, result,
                                                                                String.Format("{0}    (\"object {1}\" body line {2})",
                                                                                    Environment.NewLine, subCommand, Interpreter.GetErrorLine(interpreter)));

                                                                            break;
                                                                        }
                                                                        else
                                                                        {
                                                                            break;
                                                                        }
                                                                    }
                                                                }

                                                                //
                                                                // NOTE: Upon success, clear result.
                                                                //
                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (collect && (resultList != null))
                                                                        result = resultList;
                                                                    else
                                                                        Engine.ResetResult(interpreter, ref result);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (localError != null)
                                                                    result = localError;
                                                                else
                                                                    result = "invalid object enumerator";

                                                                Engine.AddErrorInformation(interpreter, result,
                                                                    String.Format("{0}    (getting object {1} enumerator \"{2}\"",
                                                                        Environment.NewLine, subCommand, FormatOps.Ellipsis(arguments[argumentIndex + 1])));

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "object does not support IEnumerable";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "wrong # args: should be \"object {0} varName object body\"",
                                                        subCommand);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "wrong # args: should be \"object {0} varName object body\"",
                                                subCommand);

                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "get":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetGetOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) <= arguments.Count) &&
                                                    ((argumentIndex + 3) >= arguments.Count))
                                                {
                                                    TypeList objectTypes;
                                                    bool verbose;
                                                    bool strictType;
                                                    bool noCase;

                                                    ObjectOps.ProcessGetTypeOptions(
                                                        options, out objectTypes, out verbose,
                                                        out strictType, out noCase);

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

                                                    if (noCase)
                                                        objectFlags |= ObjectFlags.NoCase;

                                                    //
                                                    // NOTE: Which Tcl interpreter do we want a command alias created
                                                    //       in, if any?
                                                    //
                                                    Type objectType = null;
                                                    ResultList errors = null;

                                                    code = Value.GetType(
                                                        interpreter, arguments[argumentIndex], objectTypes,
                                                        interpreter.GetAppDomain(), Value.GetTypeValueFlags(
                                                        strictType, verbose, noCase), interpreter.CultureInfo,
                                                        ref objectType, ref errors);

                                                    if (code != ReturnCode.Ok)
                                                    {
                                                        errors.Insert(0, String.Format(
                                                            "object or type {0} not found",
                                                            FormatOps.WrapOrNull(arguments[argumentIndex])));

                                                        result = errors;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        IObject @object = null;

                                                        if ((argumentIndex + 3) == arguments.Count)
                                                        {
                                                            code = interpreter.GetObject(
                                                                arguments[argumentIndex + 2], LookupFlags.Default,
                                                                ref @object, ref result);
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            try
                                                            {
                                                                object returnValue = Activator.GetObject(
                                                                    objectType, arguments[argumentIndex + 1],
                                                                    (@object != null) ? @object.Value : null);

                                                                MarshalOps.CheckForStickyAlias(
                                                                    @object, ref objectFlags, ref alias);

                                                                ObjectOptionType objectOptionType = ObjectOptionType.Get |
                                                                    ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                                code = MarshalOps.FixupReturnValue(
                                                                    interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                    returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                                    objectOptionType), objectOptionType, objectName, interpName,
                                                                    returnValue, create, dispose, alias, aliasReference, toString,
                                                                    ref result);
                                                            }
                                                            catch (Exception e)
                                                            {
                                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                                result = e;
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
                                                        result = "wrong # args: should be \"object get ?options? type url ?state?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object get ?options? type url ?state?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "hash":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IObject @object = null;

                                            code = interpreter.GetObject(
                                                arguments[2], LookupFlags.Default,
                                                ref @object, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Assembly assembly = (@object != null) ? @object.Value as Assembly : null;
                                                Hash hash = null;

                                                code = AssemblyOps.GetHash(assembly, ref hash, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = FormatOps.Hash(hash);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object hash assembly\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "import":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetImportOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                MatchMode matchMode;
                                                string container;
                                                string pattern;
                                                bool assembly;
                                                bool system;
                                                bool noCase;

                                                ObjectOps.ProcessObjectImportOptions(
                                                    options, null, out matchMode, out container,
                                                    out pattern, out assembly, out system,
                                                    out noCase);

                                                //
                                                // NOTE: Check for a pattern without a mode (change to
                                                //       default, which is Glob).
                                                //
                                                if ((pattern != null) && (matchMode == MatchMode.None))
                                                    matchMode = StringOps.DefaultObjectMatchMode;

                                                //
                                                // NOTE: Figure out which namespaces they want to import (add).
                                                //
                                                StringDictionary dictionary = new StringDictionary();

                                                if ((code == ReturnCode.Ok) && assembly)
                                                    code = ObjectOps.AddAssemblyObjectNamespaces(ref dictionary, ref result);

                                                if ((code == ReturnCode.Ok) && system)
                                                    code = ObjectOps.AddSystemObjectNamespaces(ref dictionary, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    //
                                                    // NOTE: Any specific namespaces they want to import (add)?
                                                    //
                                                    if (argumentIndex != Index.Invalid)
                                                        dictionary.AddKeys(ArgumentList.GetRange(
                                                            arguments, argumentIndex, Index.Invalid), container);

                                                    //
                                                    // NOTE: Ok, add the specified namespaces now.
                                                    //
                                                    code = interpreter.AddObjectNamespaces(
                                                        dictionary, matchMode, pattern, noCase,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object import ?options? ?name name ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "interfaces":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 3)
                                                    pattern = arguments[2];

                                                TypePairDictionary<string, long> dictionary = interpreter.ObjectInterfaces;

                                                if (dictionary != null)
                                                    result = dictionary.KeysAndValuesToString(pattern, false);
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object interfaces ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "invoke":
                                    {
                                        //
                                        // FIXME: There are a variety of things we could do to make invoking
                                        //        non-static objects via a simplified syntax possible (e.g.
                                        //        $x ToString):
                                        //
                                        //        1. The engine could be modified to look for objects in the
                                        //           interpreter (via GetObject on the first word of the
                                        //           command) and automatically consider them eligible to
                                        //           be executed as "commands".  They would then vector to
                                        //           the [object invoke] code (either by the engine supporting
                                        //           dynamic command re-writing, prefixing "object invoke " in
                                        //           this case, or by simply wrapping the actual [object invoke]
                                        //           code in a SubCommand object that can easily be called both
                                        //           by the engine and for actual [object invoke] calls).
                                        //
                                        //        2. A command could be added to the interpreter upon
                                        //           [object create] that calls the above mentioned
                                        //           [object invoke] SubCommand object passing only the supplied
                                        //           object and arguments (no dynamic command re-writing).
                                        //
                                        // NOTE: The above issue has been "fixed" (use the -alias option).
                                        //
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetInvokeOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // NOTE: We require at least the opaque object handle and member name
                                                //       after the options.
                                                //
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool invokeAllCmd = false;

                                                    if (options.IsPresent("-invokeall"))
                                                        invokeAllCmd = true;

                                                    bool invokeRawCmd = false;

                                                    if (options.IsPresent("-invokeraw"))
                                                        invokeRawCmd = true;

                                                    if (invokeAllCmd)
                                                    {
                                                        //
                                                        // NOTE: Copy our current argument list verbatim.
                                                        //
                                                        ArgumentList newArguments = new ArgumentList(arguments);

                                                        //
                                                        // NOTE: Replace the sub-command name with "invokeall".
                                                        //
                                                        newArguments[1] = "invokeall";

                                                        //
                                                        // NOTE: Invoke this method recursively with the modified
                                                        //       argument list.
                                                        //
                                                        return Execute(interpreter, clientData, newArguments, ref result);
                                                    }
                                                    else if (invokeRawCmd)
                                                    {
                                                        //
                                                        // NOTE: Copy our current argument list verbatim.
                                                        //
                                                        ArgumentList newArguments = new ArgumentList(arguments);

                                                        //
                                                        // NOTE: Replace the sub-command name with "invokeraw".
                                                        //
                                                        newArguments[1] = "invokeraw";

                                                        //
                                                        // NOTE: Invoke this method recursively with the modified
                                                        //       argument list.
                                                        //
                                                        return Execute(interpreter, clientData, newArguments, ref result);
                                                    }

                                                    TypeList objectTypes;
                                                    TypeList methodTypes;
                                                    TypeList parameterTypes;
                                                    MarshalFlagsList parameterMarshalFlags;
                                                    ValueFlags objectValueFlags;
                                                    ValueFlags memberValueFlags;
                                                    MemberTypes memberTypes;
                                                    BindingFlags bindingFlags;
                                                    MarshalFlags marshalFlags;
                                                    ReorderFlags reorderFlags;
                                                    ByRefArgumentFlags byRefArgumentFlags;
                                                    int limit;
                                                    int index;
                                                    bool noByRef;
                                                    bool strictType;
                                                    bool strictMember;
                                                    bool strictArgs;
                                                    bool identity;
                                                    bool typeIdentity;
                                                    bool noNestedObject;
                                                    bool noNestedMember;
                                                    bool noCase;
                                                    bool invoke;
                                                    bool noArgs;
                                                    bool arrayAsValue;
                                                    bool arrayAsLink;
                                                    bool trace;

                                                    ObjectOps.ProcessFindMethodsAndFixupArgumentsOptions(
                                                        interpreter, options, null, null, null, null, null,
                                                        null, null, out objectTypes, out methodTypes,
                                                        out parameterTypes, out parameterMarshalFlags,
                                                        out objectValueFlags, out memberValueFlags,
                                                        out memberTypes, out bindingFlags, out marshalFlags,
                                                        out reorderFlags, out byRefArgumentFlags, out limit,
                                                        out index, out noByRef, out strictType,
                                                        out strictMember, out strictArgs, out identity,
                                                        out typeIdentity, out noNestedObject,
                                                        out noNestedMember, out noCase, out invoke,
                                                        out noArgs, out arrayAsValue, out arrayAsLink,
                                                        out trace);

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

                                                    if (noCase)
                                                        objectFlags |= ObjectFlags.NoCase;

                                                    Variant value = null;
                                                    Type type = null;

                                                    if (options.IsPresent("-type", ref value))
                                                        type = (Type)value.Value;

                                                    objectValueFlags = Value.GetObjectValueFlags(
                                                        objectValueFlags, strictType, FlagOps.HasFlags(
                                                        marshalFlags, MarshalFlags.Verbose, true), noCase,
                                                        noNestedObject, FlagOps.HasFlags(objectFlags,
                                                        ObjectFlags.NoComObjectLookup, true));

                                                    TypedInstance typedInstance = null;

                                                    code = Value.GetNestedObject(
                                                        interpreter, arguments[argumentIndex], objectTypes,
                                                        interpreter.GetAppDomain(), bindingFlags, type,
                                                        objectValueFlags, interpreter.CultureInfo,
                                                        ref typedInstance, ref result);

                                                    Type objectType = null;
                                                    object @object = null;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        objectType = typedInstance.Type;

                                                        if (objectType != null)
                                                        {
                                                            @object = typedInstance.Object;
                                                        }
                                                        else if (FlagOps.HasFlags(objectValueFlags,
                                                                ValueFlags.StopOnNullObject, true))
                                                        {
                                                            //
                                                            // NOTE: Nested object/type resolution failed;
                                                            //       however, the error should be ignored.
                                                            //
                                                            code = ReturnCode.Continue;
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "invalid object or type {0}",
                                                                FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did the type lookup above succeed?
                                                    //
                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        object[] args = null;
                                                        int argumentCount = 0;

                                                        if (identity || typeIdentity)
                                                        {
                                                            //
                                                            // NOTE: In "self" mode, the object itself is always the one
                                                            //       and only argument.
                                                            //
                                                            args = new object[] { @object };

                                                            //
                                                            // NOTE: There should only be one argument; however, query
                                                            //       the create array anyhow just in case we change it.
                                                            //
                                                            argumentCount = args.Length;
                                                        }
                                                        else if ((argumentIndex + 2) < arguments.Count)
                                                        {
                                                            //
                                                            // NOTE: How many arguments were supplied?
                                                            //
                                                            argumentCount = (arguments.Count - (argumentIndex + 2));

                                                            //
                                                            // NOTE: Create and populate the array of arguments for the
                                                            //       invocation.
                                                            //
                                                            args = new object[argumentCount];

                                                            for (int index2 = (argumentIndex + 2); index2 < arguments.Count; index2++)
                                                                /* need String, not Argument */
                                                                args[index2 - (argumentIndex + 2)] = arguments[index2].String;
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
                                                        // FIXME: This is not quite right.  We are filtering based on member
                                                        //        type and binding flags and then selecting the first match
                                                        //        to proceed with processing on.  It would be nicer if we
                                                        //        could skip this step altogether; however, we have to know
                                                        //        what kind of member we are dealing with before we can do
                                                        //        anything meaningful with it (and preferably, without having
                                                        //        to get this information from the script writer).
                                                        //
                                                        TypedMember typedMemberInfo = null;

                                                        if (identity)
                                                        {
                                                            //
                                                            // NOTE: While the member name must be present in the object
                                                            //       invoke call (primarily due to the argument count
                                                            //       checking and the [normally totally valid] hard-wired
                                                            //       assumptions about argument indexing), it is ignored
                                                            //       when the -identity option is used.
                                                            //
                                                            typedMemberInfo = new TypedMember(
                                                                typeof(HandleOps), ObjectFlags.None, @object,
                                                                HandleOps.IdentityMemberName,
                                                                HandleOps.IdentityMemberName,
                                                                HandleOps.IdentityMemberInfo);
                                                        }
                                                        else if (typeIdentity)
                                                        {
                                                            //
                                                            // NOTE: While the member name must be present in the object
                                                            //       invoke call (primarily due to the argument count
                                                            //       checking and the [normally totally valid] hard-wired
                                                            //       assumptions about argument indexing), it is ignored
                                                            //       when the -typeidentity option is used.
                                                            //
                                                            typedMemberInfo = new TypedMember(
                                                                typeof(HandleOps), ObjectFlags.None, @object,
                                                                HandleOps.TypeIdentityMemberName,
                                                                HandleOps.TypeIdentityMemberName,
                                                                HandleOps.TypeIdentityMemberInfo);
                                                        }
                                                        else if (!String.IsNullOrEmpty(arguments[argumentIndex + 1]))
                                                        {
                                                            //
                                                            // NOTE: Attempt to resolve the final member information based on
                                                            //       the provided [possibly composite] member name and the
                                                            //       specified member types and binding flags.
                                                            //
                                                            memberValueFlags = Value.GetMemberValueFlags(
                                                                memberValueFlags, noNestedMember, FlagOps.HasFlags(
                                                                objectFlags, ObjectFlags.NoComObjectLookup, true));

                                                            code = Value.GetNestedMember(
                                                                interpreter, arguments[argumentIndex + 1],
                                                                typedInstance, memberTypes, bindingFlags,
                                                                memberValueFlags, interpreter.CultureInfo,
                                                                ref typedMemberInfo, ref result);
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: A null or empty string was provided, skip the standard
                                                            //       member name resolution and simply use the default
                                                            //       member(s).
                                                            //
                                                            typedMemberInfo = new TypedMember(
                                                                objectType, ObjectFlags.None, @object,
                                                                arguments[argumentIndex + 1],
                                                                arguments[argumentIndex + 1],
                                                                objectType.GetDefaultMembers());
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: The type and object instance could have changed during
                                                            //       the member resolution process (above); therefore,
                                                            //       refresh them from that result now.
                                                            //
                                                            objectType = typedMemberInfo.Type;
                                                            @object = typedMemberInfo.Object;

                                                            //
                                                            // NOTE: Get the object and method names to pass to the method
                                                            //       overload resolution engine.  Also, get the array of
                                                            //       methods to actually perform overload resolution over.
                                                            //
                                                            string newObjectName = typedInstance.ObjectName; /* COSMETIC */
                                                            string newFullObjectName = typedInstance.FullObjectName; /* COSMETIC */
                                                            string newMemberName = typedMemberInfo.MemberName;
                                                            string newFullMemberName = typedMemberInfo.FullMemberName; /* COSMETIC */
                                                            MemberInfo[] memberInfo = typedMemberInfo.MemberInfo;

                                                            //
                                                            // NOTE: Figure out which type of options are needed for created
                                                            //       aliases.
                                                            //
                                                            MarshalOps.CheckForStickyAlias(
                                                                typedInstance, ref objectFlags, ref alias);

                                                            ObjectOptionType objectOptionType = /* ObjectOptionType.Invoke | */
                                                                ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                            //
                                                            // NOTE: Make sure we found some members to perform overload
                                                            //       resolution over.
                                                            //
                                                            if ((memberInfo != null) && (memberInfo.Length > 0))
                                                            {
                                                                switch (memberInfo[0].MemberType)
                                                                {
                                                                    case MemberTypes.Field:
                                                                        {
                                                                            FieldInfo fieldInfo = objectType.GetField(
                                                                                newMemberName, bindingFlags);

                                                                            if (fieldInfo != null)
                                                                            {
                                                                                if (invoke)
                                                                                {
                                                                                    if (argumentCount == 0)
                                                                                    {
                                                                                        try
                                                                                        {
                                                                                            object returnValue = fieldInfo.GetValue(@object);

                                                                                            code = MarshalOps.FixupReturnValue(
                                                                                                interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                                returnType, objectFlags, options, objectOptionType,
                                                                                                objectName, interpName, returnValue, create, dispose,
                                                                                                alias, aliasReference, toString, ref result);
                                                                                        }
                                                                                        catch (Exception e)
                                                                                        {
                                                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                                                            result = e;
                                                                                            code = ReturnCode.Error;
                                                                                        }
                                                                                    }
                                                                                    else if (argumentCount == 1)
                                                                                    {
                                                                                        try
                                                                                        {
                                                                                            object newFieldValue = args[0];

                                                                                            if (FlagOps.HasFlags(objectFlags,
                                                                                                    ObjectFlags.AutoFlagsEnum, true) &&
                                                                                                EnumOps.IsFlagsEnum(fieldInfo.FieldType))
                                                                                            {
                                                                                                object oldFieldValue = fieldInfo.GetValue(
                                                                                                    @object);

                                                                                                newFieldValue = EnumOps.TryParseFlagsEnum(
                                                                                                    interpreter, fieldInfo.FieldType,
                                                                                                    (oldFieldValue != null) ?
                                                                                                        oldFieldValue.ToString() : null,
                                                                                                    (newFieldValue != null) ?
                                                                                                        newFieldValue.ToString() : null,
                                                                                                    interpreter.CultureInfo, true, true,
                                                                                                    true, ref result);

                                                                                                if (newFieldValue == null)
                                                                                                    code = ReturnCode.Error;
                                                                                            }

                                                                                            if (code == ReturnCode.Ok)
                                                                                            {
                                                                                                code = MarshalOps.FixupArgument(
                                                                                                    interpreter, interpreter.Binder, options,
                                                                                                    interpreter.CultureInfo, fieldInfo.FieldType,
                                                                                                    ArgumentInfo.Create(
                                                                                                        0, fieldInfo.FieldType, newMemberName,
                                                                                                        true, false),
                                                                                                    MarshalFlags.None, true, false,
                                                                                                    ref newFieldValue, ref result);

                                                                                                if (code == ReturnCode.Ok)
                                                                                                {
                                                                                                    fieldInfo.SetValue(
                                                                                                        @object, newFieldValue, bindingFlags,
                                                                                                        interpreter.Binder as Binder,
                                                                                                        interpreter.CultureInfo);

                                                                                                    result = String.Empty;
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
                                                                                            "wrong # args for field: " +
                                                                                            "should be \"object invoke ?options? \"{0}\" \"{1}\" ?newValue?\"",
                                                                                            arguments[argumentIndex], arguments[argumentIndex + 1]);

                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    code = MarshalOps.FixupReturnValue(
                                                                                        interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                                        returnType, objectFlags, options, objectOptionType,
                                                                                        objectName, interpName, fieldInfo, create, dispose,
                                                                                        alias, aliasReference, toString, ref result);
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                result = String.Format(
                                                                                    "field {0} of object {1} not found",
                                                                                    FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                    FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                code = ReturnCode.Error;
                                                                            }
                                                                            break;
                                                                        }
                                                                    case MemberTypes.Method:
                                                                        {
                                                                            //
                                                                            // HACK: Cannot filter on the name here (via Type.GetMethods)?
                                                                            //
                                                                            MethodInfo[] methodInfo = objectType.GetMethods(bindingFlags);

                                                                            if (methodInfo != null)
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
                                                                                    interpreter.CultureInfo, objectType, newObjectName,
                                                                                    newFullObjectName, newMemberName, newFullMemberName,
                                                                                    MemberTypes.Method, bindingFlags, methodInfo,
                                                                                    methodTypes, parameterTypes, parameterMarshalFlags,
                                                                                    args, limit, marshalFlags, ref methodIndexList,
                                                                                    ref argsList, ref argumentInfoListDictionary,
                                                                                    ref errors);

                                                                                if (code == ReturnCode.Ok)
                                                                                {
                                                                                    //
                                                                                    // NOTE: Make sure we got a valid list of method indexes.
                                                                                    //
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
                                                                                                    ref methodIndexList, ref argsList,
                                                                                                    ref errors);

                                                                                                if (code == ReturnCode.Ok)
                                                                                                {
                                                                                                    if (trace)
                                                                                                    {
                                                                                                        TraceOps.DebugTrace(String.Format(
                                                                                                            "Execute: savedMethodIndexList = {0}, " +
                                                                                                            "methodIndexList = {1}",
                                                                                                            savedMethodIndexList, methodIndexList),
                                                                                                            typeof(Object).Name, TracePriority.Command);
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    result = errors;
                                                                                                }
                                                                                            }

                                                                                            if (code == ReturnCode.Ok)
                                                                                            {
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
                                                                                                            methodIndex = MarshalOps.SelectMethodIndex(interpreter,
                                                                                                                interpreter.Binder, interpreter.CultureInfo, objectType,
                                                                                                                methodInfo, parameterTypes, parameterMarshalFlags, args,
                                                                                                                methodIndexList, argsList, marshalFlags, reorderFlags);
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
                                                                                                                        "Execute: methodIndex = {0}, methodInfo = {1}, " +
                                                                                                                        "args = {2}, argumentInfoList = {3}",
                                                                                                                        methodIndex,
                                                                                                                        FormatOps.WrapOrNull(methodInfo[methodIndex]),
                                                                                                                        FormatOps.WrapOrNull(new StringList(args)),
                                                                                                                        FormatOps.WrapOrNull(argumentInfoList)),
                                                                                                                        typeof(Object).Name, TracePriority.Command);
                                                                                                                }

                                                                                                                object returnValue = methodInfo[methodIndex].Invoke(
                                                                                                                    @object, bindingFlags, interpreter.Binder as Binder, args,
                                                                                                                    interpreter.CultureInfo);

                                                                                                                if (!noByRef && (argumentInfoList != null))
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
                                                                                                                "method {0} of object {1} not found",
                                                                                                                FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                                                FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                                            code = ReturnCode.Error;
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        result = String.Format(
                                                                                                            "matched {0} method overloads of {1} on type {2}, need exactly 1",
                                                                                                             methodIndexList.Count,
                                                                                                             FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                                             FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                                        code = ReturnCode.Error;
                                                                                                    }
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    MethodInfoList methodInfoList = new MethodInfoList();

                                                                                                    if (index != Index.Invalid)
                                                                                                    {
                                                                                                        methodInfoList.Add(methodInfo[methodIndexList[index]]);
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        foreach (int methodIndex in methodIndexList)
                                                                                                            methodInfoList.Add(methodInfo[methodIndex]);
                                                                                                    }

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
                                                                                                "method {0} of object {1} not found, " +
                                                                                                "invalid method index {2}, must be {3}",
                                                                                                FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                                FormatOps.WrapOrNull(arguments[argumentIndex]),
                                                                                                index, FormatOps.BetweenOrExact(0, methodIndexList.Count - 1));

                                                                                            code = ReturnCode.Error;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Format(
                                                                                            "method {0} of object {1} not found",
                                                                                            FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                            FormatOps.WrapOrNull(arguments[argumentIndex]));

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
                                                                                    "type {0} has no methods matching member " +
                                                                                    "types {1} and flags {2}",
                                                                                    MarshalOps.GetErrorTypeName(objectType),
                                                                                    FormatOps.WrapOrNull(memberTypes),
                                                                                    FormatOps.WrapOrNull(bindingFlags));

                                                                                code = ReturnCode.Error;
                                                                            }
                                                                            break;
                                                                        }
                                                                    case MemberTypes.Property:
                                                                        {
                                                                            //
                                                                            // HACK: Cannot filter on the name here (via Type.GetProperties)?
                                                                            //
                                                                            PropertyInfo[] propertyInfo = objectType.GetProperties(bindingFlags);

                                                                            if (propertyInfo != null)
                                                                            {
                                                                                MethodInfo[] methodInfo = null;

                                                                                //
                                                                                // NOTE: Ok, now we need to get all the methods that access
                                                                                //       the properties (get and set).
                                                                                //
                                                                                code = MarshalOps.GetMethodInfoFromPropertyInfo(
                                                                                    propertyInfo, bindingFlags, ref methodInfo, ref result);

                                                                                if (code == ReturnCode.Ok)
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
                                                                                        interpreter.CultureInfo, objectType, newObjectName,
                                                                                        newFullObjectName, newMemberName, newFullMemberName,
                                                                                        MemberTypes.Property, bindingFlags, methodInfo,
                                                                                        methodTypes, parameterTypes, parameterMarshalFlags,
                                                                                        args, limit, marshalFlags, ref methodIndexList,
                                                                                        ref argsList, ref argumentInfoListDictionary,
                                                                                        ref errors);

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
                                                                                                        ref methodIndexList, ref argsList,
                                                                                                        ref errors);

                                                                                                    if (code == ReturnCode.Ok)
                                                                                                    {
                                                                                                        if (trace)
                                                                                                        {
                                                                                                            TraceOps.DebugTrace(String.Format(
                                                                                                                "Execute: savedMethodIndexList = {0}, " +
                                                                                                                "methodIndexList = {1}",
                                                                                                                savedMethodIndexList, methodIndexList),
                                                                                                                typeof(Object).Name, TracePriority.Command);
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        result = errors;
                                                                                                    }
                                                                                                }

                                                                                                if (code == ReturnCode.Ok)
                                                                                                {
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
                                                                                                                methodIndex = MarshalOps.SelectMethodIndex(interpreter,
                                                                                                                    interpreter.Binder, interpreter.CultureInfo, objectType,
                                                                                                                    methodInfo, parameterTypes, parameterMarshalFlags, args,
                                                                                                                    methodIndexList, argsList, marshalFlags, reorderFlags);
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
                                                                                                                            "Execute: methodIndex = {0}, methodInfo = {1}, " +
                                                                                                                            "args = {2}, argumentInfoList = {3}",
                                                                                                                            methodIndex,
                                                                                                                            FormatOps.WrapOrNull(methodInfo[methodIndex]),
                                                                                                                            FormatOps.WrapOrNull(new StringList(args)),
                                                                                                                            FormatOps.WrapOrNull(argumentInfoList)),
                                                                                                                            typeof(Object).Name, TracePriority.Command);
                                                                                                                    }

                                                                                                                    object returnValue = methodInfo[methodIndex].Invoke(
                                                                                                                        @object, bindingFlags, interpreter.Binder as Binder, args,
                                                                                                                        interpreter.CultureInfo);

                                                                                                                    if (!noByRef && (argumentInfoList != null))
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
                                                                                                                    "property {0} of object {1} not found",
                                                                                                                    FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                                                    FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                                                code = ReturnCode.Error;
                                                                                                            }
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            result = String.Format(
                                                                                                                "matched {0} property overloads of {1} on type {2}, need exactly 1",
                                                                                                                methodIndexList.Count, FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                                                FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                                            code = ReturnCode.Error;
                                                                                                        }
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        MethodInfoList methodInfoList = new MethodInfoList();

                                                                                                        if (index != Index.Invalid)
                                                                                                        {
                                                                                                            methodInfoList.Add(methodInfo[methodIndexList[index]]);
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            foreach (int methodIndex in methodIndexList)
                                                                                                                methodInfoList.Add(methodInfo[methodIndex]);
                                                                                                        }

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
                                                                                                    "property {0} of object {1} not found, " +
                                                                                                    "invalid method index {2}, must be {3}",
                                                                                                    FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                                    FormatOps.WrapOrNull(arguments[argumentIndex]),
                                                                                                    index, FormatOps.BetweenOrExact(0, methodIndexList.Count - 1));

                                                                                                code = ReturnCode.Error;
                                                                                            }
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            result = String.Format(
                                                                                                "property {0} of object {1} not found",
                                                                                                FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                                                FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                            code = ReturnCode.Error;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = errors;
                                                                                    }
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                result = String.Format(
                                                                                    "type {0} has no properties matching {1}",
                                                                                    MarshalOps.GetErrorTypeName(objectType),
                                                                                    FormatOps.WrapOrNull(bindingFlags));

                                                                                code = ReturnCode.Error;
                                                                            }
                                                                            break;
                                                                        }
                                                                    default:
                                                                        {
                                                                            result = String.Format(
                                                                                "unsupported member type {0}",
                                                                                FormatOps.WrapOrNull(memberInfo[0].MemberType));

                                                                            code = ReturnCode.Error;
                                                                            break;
                                                                        }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "type {0} member {1} matching member " +
                                                                    "types {2} and flags {3} not found",
                                                                    MarshalOps.GetErrorTypeName(objectType),
                                                                    FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                    FormatOps.WrapOrNull(memberTypes),
                                                                    FormatOps.WrapOrNull(bindingFlags));

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else if (code == ReturnCode.Continue)
                                                        {
                                                            //
                                                            // NOTE: Nested type member resolution failed;
                                                            //       however, the error should be ignored.
                                                            //
                                                            code = ReturnCode.Ok;
                                                        }
                                                    }
                                                    else if (code == ReturnCode.Continue)
                                                    {
                                                        //
                                                        // NOTE: Nested object/type resolution failed;
                                                        //       however, the error should be ignored.
                                                        //
                                                        code = ReturnCode.Ok;
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
                                                        result = "wrong # args: should be \"object invoke ?options? object member ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object invoke ?options? object member ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "invokeall":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetInvokeAllOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool invokeCmd = false;

                                                    if (options.IsPresent("-invoke"))
                                                        invokeCmd = true;

                                                    bool invokeRawCmd = false;

                                                    if (options.IsPresent("-invokeraw"))
                                                        invokeRawCmd = true;

                                                    //
                                                    // NOTE: If we are supposed to simply use [object invoke] for this
                                                    //       call instead, do that now.
                                                    //
                                                    if (invokeCmd)
                                                    {
                                                        //
                                                        // NOTE: Copy our current argument list verbatim.
                                                        //
                                                        ArgumentList newArguments = new ArgumentList(arguments);

                                                        //
                                                        // NOTE: Replace the sub-command name with "invoke".
                                                        //
                                                        newArguments[1] = "invoke";

                                                        //
                                                        // NOTE: Invoke this method recursively with the modified
                                                        //       argument list.
                                                        //
                                                        return Execute(interpreter, clientData, newArguments, ref result);
                                                    }
                                                    else if (invokeRawCmd)
                                                    {
                                                        //
                                                        // NOTE: Copy our current argument list verbatim.
                                                        //
                                                        ArgumentList newArguments = new ArgumentList(arguments);

                                                        //
                                                        // NOTE: Replace the sub-command name with "invokeraw".
                                                        //
                                                        newArguments[1] = "invokeraw";

                                                        //
                                                        // NOTE: Invoke this method recursively with the modified
                                                        //       argument list.
                                                        //
                                                        return Execute(interpreter, clientData, newArguments, ref result);
                                                    }

                                                    bool chained = false;

                                                    if (options.IsPresent("-chained"))
                                                        chained = true;

                                                    bool lastResult = false;

                                                    if (options.IsPresent("-lastresult"))
                                                        lastResult = true;

                                                    bool keepResults = false;

                                                    if (options.IsPresent("-keepresults"))
                                                        keepResults = true;

                                                    bool noComplain = false;

                                                    if (options.IsPresent("-nocomplain"))
                                                        noComplain = true;

                                                    //
                                                    // NOTE: If necessary, mask-off the options that are not supported by
                                                    //       [object invoke].
                                                    //
                                                    if (chained)
                                                        options.SetPresent("-chained", false, Index.Invalid, null);

                                                    if (lastResult)
                                                        options.SetPresent("-lastresult", false, Index.Invalid, null);

                                                    if (keepResults)
                                                        options.SetPresent("-keepresults", false, Index.Invalid, null);

                                                    if (noComplain)
                                                        options.SetPresent("-nocomplain", false, Index.Invalid, null);

                                                    //
                                                    // NOTE: Save the object name because we need it every time through
                                                    //       the loop.
                                                    //
                                                    string objectName = arguments[argumentIndex];

                                                    //
                                                    // NOTE: This will contain the number of invocation errors.
                                                    //
                                                    int errorCount = 0;

                                                    //
                                                    // NOTE: If we are keeping track of all the results, create the
                                                    //       necessary list now.
                                                    //
                                                    ResultList newResults = keepResults ? new ResultList() : null;

                                                    //
                                                    // NOTE: Advance to the first invocation list element and keep
                                                    //       going until there are no more to be processed (unless
                                                    //       we break out early).
                                                    //
                                                    for (argumentIndex++; argumentIndex < arguments.Count; argumentIndex++)
                                                    {
                                                        //
                                                        // NOTE: First, parse the invocation list element.  It must
                                                        //       be a list consisting of a member name followed by
                                                        //       the arguments to that member, if any.
                                                        //
                                                        StringList list = null;
                                                        Result newResult = null;

                                                        code = Parser.SplitList(
                                                            interpreter, arguments[argumentIndex], 0,
                                                            Length.Invalid, true, ref list, ref newResult);

                                                        if ((code != ReturnCode.Ok) || (list.Count == 0))
                                                        {
                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                newResult = "invocation list element missing member name";
                                                                code = ReturnCode.Error;
                                                            }

                                                            if (noComplain)
                                                                noComplain = false; /* NOTE: Not an invocation error. */

                                                            goto error;
                                                        }

                                                        //
                                                        // NOTE: Next, grab the first two arguments, which are currently
                                                        //       always the same (i.e. [object invoke]) and append them to
                                                        //       the new argument list.
                                                        //
                                                        ArgumentList newArguments = null;

                                                        code = RuntimeOps.GetObjectAliasArguments(
                                                            null, ObjectOptionType.Invoke, ref newArguments,
                                                            ref newResult);

                                                        if (code != ReturnCode.Ok)
                                                        {
                                                            if (noComplain)
                                                                noComplain = false; /* NOTE: Not an invocation error. */

                                                            goto error;
                                                        }

                                                        //
                                                        // NOTE: Next, add all the options used to invoke this command with
                                                        //       the exception of the ones we [may] have masked off earlier
                                                        //       (i.e. the ones supported by us and not [object invoke]).
                                                        //
                                                        code = options.ToArgumentList(ref newArguments, ref newResult);

                                                        if (code != ReturnCode.Ok)
                                                        {
                                                            if (noComplain)
                                                                noComplain = false; /* NOTE: Not an invocation error. */

                                                            goto error;
                                                        }

                                                        //
                                                        // NOTE: Next, add the object name, which is currently always the
                                                        //       same, to the new argument list.
                                                        //
                                                        newArguments.Add(objectName);

                                                        //
                                                        // NOTE: Next, add the member name and its associated arguments, if
                                                        //       any.
                                                        //
                                                        newArguments.AddRange(list);

                                                        //
                                                        // NOTE: Finally, invoke this method again using the new arguments
                                                        //       and a new result.
                                                        //
                                                        /* RECURSIVE */
                                                        code = Execute(interpreter, clientData, newArguments, ref newResult);

                                                        //
                                                        // NOTE: In "chained" mode, the result at this point must be an
                                                        //       opaque object handle.
                                                        //
                                                        if ((code == ReturnCode.Ok) &&
                                                            chained && ((argumentIndex + 1) < arguments.Count))
                                                        {
                                                            Result localResult = null;

                                                            if (interpreter.DoesObjectExist(
                                                                    newResult, ref localResult) == ReturnCode.Ok)
                                                            {
                                                                objectName = newResult;
                                                            }
                                                            else
                                                            {
                                                                newResult = localResult;
                                                                code = ReturnCode.Error;
                                                            }
                                                        }

                                                    error:
                                                        //
                                                        // NOTE: If we are keeping track of results, add the return code and
                                                        //       result to the result list now.  We need to do this whether
                                                        //       or not we are ignoring invocation errors.
                                                        //
                                                        if (keepResults && (newResults != null))
                                                        {
                                                            newResults.Add(code);
                                                            newResults.Add(newResult);
                                                        }

                                                        //
                                                        // NOTE: Did the object invocation raise some kind of error?  If so,
                                                        //       we need to determine how to handle it.
                                                        //
                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // NOTE: In single result mode, set the overall result to the
                                                            //       one from the most recent (and last) [object invoke].
                                                            //
                                                            if (lastResult && ((argumentIndex + 1) >= arguments.Count))
                                                            {
                                                                result = newResult;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // NOTE: If we are ignoring invocation errors, just increase the
                                                            //       error count and then pretend like nothing bad happened;
                                                            //       otherwise, we will bail out after setting the overall
                                                            //       result.
                                                            //
                                                            if (!chained && noComplain)
                                                            {
                                                                errorCount++;
                                                                code = ReturnCode.Ok;
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: If we have been keeping track of results the whole
                                                                //       time, do not discard them now; otherwise, just return
                                                                //       the error that is causing us to abort our processing.
                                                                //
                                                                if (keepResults && (newResults != null))
                                                                    result = newResults;
                                                                else
                                                                    result = newResult;

                                                                break;
                                                            }
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Was the overall result success?
                                                    //
                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // NOTE: If we were keeping track of results the whole time,
                                                        //       return them all as one big list -OR- if the single
                                                        //       result mode is enabled, do nothing as the result
                                                        //       has already been set; otherwise, return the literal
                                                        //       string "Ok" if no errors were encountered -OR- the
                                                        //       literal string "Error" if there was at least one
                                                        //       error.
                                                        //
                                                        if (keepResults && (newResults != null))
                                                        {
                                                            result = newResults;
                                                        }
                                                        else if (!lastResult)
                                                        {
                                                            result = StringList.MakeList((errorCount == 0) ?
                                                                ReturnCode.Ok : ReturnCode.Error, errorCount);
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
                                                        result = "wrong # args: should be \"object invokeall ?options? object memberAndArgs ?memberAndArgs ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object invokeall ?options? object memberAndArgs ?memberAndArgs ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "invokeraw":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetInvokeRawOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                //
                                                // NOTE: We require at least two arguments after the
                                                //       options.
                                                //
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool invokeCmd = false;

                                                    if (options.IsPresent("-invoke"))
                                                        invokeCmd = true;

                                                    bool invokeAllCmd = false;

                                                    if (options.IsPresent("-invokeall"))
                                                        invokeAllCmd = true;

                                                    if (invokeCmd)
                                                    {
                                                        //
                                                        // NOTE: Copy our current argument list verbatim.
                                                        //
                                                        ArgumentList newArguments = new ArgumentList(arguments);

                                                        //
                                                        // NOTE: Replace the sub-command name with "invoke".
                                                        //
                                                        newArguments[1] = "invoke";

                                                        //
                                                        // NOTE: Invoke this method recursively with the modified
                                                        //       argument list.
                                                        //
                                                        return Execute(interpreter, clientData, newArguments, ref result);
                                                    }
                                                    else if (invokeAllCmd)
                                                    {
                                                        //
                                                        // NOTE: Copy our current argument list verbatim.
                                                        //
                                                        ArgumentList newArguments = new ArgumentList(arguments);

                                                        //
                                                        // NOTE: Replace the sub-command name with "invokeall".
                                                        //
                                                        newArguments[1] = "invokeall";

                                                        //
                                                        // NOTE: Invoke this method recursively with the modified
                                                        //       argument list.
                                                        //
                                                        return Execute(interpreter, clientData, newArguments, ref result);
                                                    }

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

                                                    Type type;
                                                    TypeList objectTypes;
                                                    TypeList methodTypes;
                                                    TypeList parameterTypes;
                                                    MarshalFlagsList parameterMarshalFlags;
                                                    ValueFlags objectValueFlags;
                                                    BindingFlags bindingFlags;
                                                    MarshalFlags marshalFlags;
                                                    ByRefArgumentFlags byRefArgumentFlags;
                                                    bool noByRef;
                                                    bool noCase;
                                                    bool strictType;
                                                    bool strictArgs;
                                                    bool noNestedObject;
                                                    bool invoke;
                                                    bool noArgs;
                                                    bool arrayAsValue;
                                                    bool arrayAsLink;
                                                    bool trace;

                                                    ObjectOps.ProcessObjectInvokeRawOptions(
                                                        options, null, ObjectOps.GetInvokeRawBindingFlags(),
                                                        null, null, out type, out objectTypes, out methodTypes,
                                                        out parameterTypes, out parameterMarshalFlags,
                                                        out objectValueFlags, out bindingFlags, out marshalFlags,
                                                        out byRefArgumentFlags, out noByRef, out strictType,
                                                        out strictArgs, out noNestedObject, out noCase,
                                                        out invoke, out noArgs, out arrayAsValue,
                                                        out arrayAsLink, out trace);

                                                    objectValueFlags = Value.GetObjectValueFlags(
                                                        objectValueFlags, strictType, FlagOps.HasFlags(
                                                        marshalFlags, MarshalFlags.Verbose, true), noCase,
                                                        noNestedObject, FlagOps.HasFlags(objectFlags,
                                                        ObjectFlags.NoComObjectLookup, true));

                                                    TypedInstance typedInstance = null;

                                                    code = Value.GetNestedObject(
                                                        interpreter, arguments[argumentIndex], objectTypes,
                                                        interpreter.GetAppDomain(), bindingFlags, type,
                                                        objectValueFlags, interpreter.CultureInfo,
                                                        ref typedInstance, ref result);

                                                    if (noCase)
                                                        objectFlags |= ObjectFlags.NoCase;

                                                    //
                                                    // NOTE: Did the object/type lookup succeed?
                                                    //
                                                    Type objectType = null;
                                                    object @object = null;
                                                    string newObjectName = null;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        objectType = typedInstance.Type;

                                                        if (objectType != null)
                                                        {
                                                            @object = typedInstance.Object;
                                                            newObjectName = typedInstance.ObjectName; /* COSMETIC */
                                                        }
                                                        else if (FlagOps.HasFlags(objectValueFlags,
                                                                ValueFlags.StopOnNullObject, true))
                                                        {
                                                            //
                                                            // NOTE: Nested object/type resolution failed;
                                                            //       however, the error should be ignored.
                                                            //
                                                            code = ReturnCode.Continue;
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "invalid object or type {0}",
                                                                FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did the type lookup above succeed?
                                                    //
                                                    object[] args = null;
                                                    ArgumentInfoList argumentInfoList = null;

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        int argumentCount = 0;

                                                        if ((argumentIndex + 2) < arguments.Count)
                                                        {
                                                            argumentCount = (arguments.Count - (argumentIndex + 2));
                                                            args = new object[argumentCount];

                                                            for (int index2 = (argumentIndex + 2);
                                                                    index2 < arguments.Count;
                                                                    index2++)
                                                            {
                                                                /* need String, not Argument */
                                                                args[index2 - (argumentIndex + 2)] =
                                                                    arguments[index2].String;
                                                            }
                                                        }
                                                        else if (invoke || !noArgs)
                                                        {
                                                            args = new object[0];
                                                        }

                                                        if (parameterTypes != null)
                                                        {
                                                            code = MarshalOps.FixupArguments(
                                                                interpreter, interpreter.Binder, options,
                                                                interpreter.CultureInfo, newObjectName,
                                                                arguments[argumentIndex + 1], parameterTypes,
                                                                parameterMarshalFlags, marshalFlags, args,
                                                                false, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                code = MarshalOps.GetByRefArgumentInfo(
                                                                    parameterTypes, parameterMarshalFlags,
                                                                    marshalFlags, ref argumentInfoList,
                                                                    ref result);
                                                            }
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did the parameter type translation above succeed?
                                                    //
                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        try
                                                        {
                                                            if (trace)
                                                            {
                                                                TraceOps.DebugTrace(String.Format(
                                                                    "Execute: objectType = {0}, objectName = {1}, " +
                                                                    "memberName = {2}, valueFlags = {3}, " +
                                                                    "bindingFlags = {4}, binder = {5}, target = {6}, " +
                                                                    "culture = {7}, args = {8}, argumentInfoList = {9}",
                                                                    FormatOps.InvokeRawTypeName(objectType),
                                                                    FormatOps.WrapOrNull(newObjectName),
                                                                    FormatOps.WrapOrNull(arguments[argumentIndex + 1]),
                                                                    FormatOps.WrapOrNull(objectValueFlags),
                                                                    FormatOps.WrapOrNull(bindingFlags),
                                                                    FormatOps.WrapOrNull(interpreter.Binder),
                                                                    FormatOps.WrapOrNull(@object),
                                                                    FormatOps.WrapOrNull(interpreter.CultureInfo),
                                                                    FormatOps.WrapOrNull(new StringList(args)),
                                                                    FormatOps.WrapOrNull(argumentInfoList)),
                                                                    typeof(Object).Name, TracePriority.Command);
                                                            }

                                                            object returnValue = invoke ? objectType.InvokeMember(
                                                                arguments[argumentIndex + 1], bindingFlags,
                                                                interpreter.Binder as Binder, @object, args,
                                                                interpreter.CultureInfo) : null;

                                                            MarshalOps.CheckForStickyAlias(
                                                                typedInstance, ref objectFlags, ref alias);

                                                            ObjectOptionType objectOptionType = /* ObjectOptionType.InvokeRaw | */
                                                                ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                            if (!noByRef && (argumentInfoList != null))
                                                            {
                                                                code = MarshalOps.FixupByRefArguments(
                                                                    interpreter, interpreter.Binder,
                                                                    interpreter.CultureInfo,
                                                                    argumentInfoList,
                                                                    objectFlags | byRefObjectFlags,
                                                                    options, objectOptionType, interpName,
                                                                    args, marshalFlags, byRefArgumentFlags,
                                                                    strictArgs, create, dispose, alias,
                                                                    aliasReference, toString, arrayAsValue,
                                                                    arrayAsLink, ref result);
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                code = MarshalOps.FixupReturnValue(
                                                                    interpreter, interpreter.Binder,
                                                                    interpreter.CultureInfo, returnType,
                                                                    objectFlags, options, objectOptionType,
                                                                    objectName, interpName, returnValue,
                                                                    create, dispose, alias, aliasReference,
                                                                    toString, ref result);
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                            result = e;
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else if (code == ReturnCode.Continue)
                                                    {
                                                        //
                                                        // NOTE: Nested object/type resolution failed;
                                                        //       however, the error should be ignored.
                                                        //
                                                        code = ReturnCode.Ok;
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
                                                        result = "wrong # args: should be \"object invokeraw ?options? object member ?arg ...?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object invokeraw ?options? object member ?arg ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isoftype":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = ObjectOps.GetIsOfTypeOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(
                                                options, arguments, 0, 2, Index.Invalid, false,
                                                ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    TypeList objectTypes;
                                                    MarshalFlags marshalFlags;
                                                    bool verbose;
                                                    bool strictType;
                                                    bool noCase;
                                                    bool noComplain;
                                                    bool assignable;

                                                    ObjectOps.ProcessObjectIsOfTypeOptions(
                                                        options, null, out objectTypes, out marshalFlags,
                                                        out verbose, out strictType, out noCase,
                                                        out noComplain, out assignable);

                                                    IObject @object = null;
                                                    Result error = null;

                                                    code = interpreter.GetObject(
                                                        arguments[argumentIndex], LookupFlags.Default,
                                                        ref @object, ref error);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Type type = null;
                                                        ResultList errors = null;

                                                        code = Value.GetType(
                                                            interpreter, arguments[argumentIndex + 1], objectTypes,
                                                            interpreter.GetAppDomain(), Value.GetTypeValueFlags(
                                                            strictType, verbose, noCase), interpreter.CultureInfo,
                                                            ref type, ref errors);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            result = MarshalOps.IsOfType(
                                                                @object, type, marshalFlags, assignable);
                                                        }
                                                        else if (noComplain)
                                                        {
                                                            result = false;
                                                            code = ReturnCode.Ok;
                                                        }
                                                        else
                                                        {
                                                            errors.Insert(0, String.Format(
                                                                "object or type {0} not found",
                                                                FormatOps.WrapOrNull(arguments[argumentIndex + 1])));

                                                            result = errors;
                                                        }
                                                    }
                                                    else if (noComplain)
                                                    {
                                                        result = false;
                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        result = error;
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
                                                        result = "wrong # args: should be \"object isoftype ?options? object type\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object isoftype ?options? object type\"";
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

                                            result = interpreter.ObjectsToString(pattern, false);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object list ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "load":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetLoadOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    //
                                                    // NOTE: Create defaults to false because we know that Assembly
                                                    //       is not a primitive type and hence that it will not
                                                    //       be automatically converted by FixupReturnValue.
                                                    //
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

                                                    ObjectOps.ProcessFixupReturnValueOptions(options,
                                                        ObjectOps.GetDefaultObjectFlags() | ObjectFlags.Assembly,
                                                        out returnType, out objectFlags, out objectName,
                                                        out interpName, out create, out dispose, out alias,
                                                        out aliasRaw, out aliasAll, out aliasReference,
                                                        out toString);

                                                    INamespace @namespace;
                                                    LoadType loadType;
                                                    MatchMode declareMatchMode;
                                                    MatchMode importMatchMode;
                                                    string declarePattern;
                                                    string importPattern;
                                                    bool declare;
                                                    bool import;
                                                    bool declareNonPublic;
                                                    bool declareNoCase;
                                                    bool importNonPublic;
                                                    bool importNoCase;

                                                    ObjectOps.ProcessObjectLoadOptions(
                                                        options, null, null, out @namespace, out loadType,
                                                        out declareMatchMode, out importMatchMode,
                                                        out declarePattern, out importPattern, out declare,
                                                        out import, out declareNonPublic, out declareNoCase,
                                                        out importNonPublic, out importNoCase);

                                                    //
                                                    // NOTE: Check for a pattern without a mode (change to
                                                    //       default, which is Glob).
                                                    //
                                                    if ((importPattern != null) && (importMatchMode == MatchMode.None))
                                                        importMatchMode = StringOps.DefaultObjectMatchMode;

                                                    if ((declarePattern != null) && (declareMatchMode == MatchMode.None))
                                                        declareMatchMode = StringOps.DefaultObjectMatchMode;

                                                    //
                                                    // NOTE: We intend to modify the interpreter state,
                                                    //       make sure this is not forbidden.
                                                    //
                                                    if (interpreter.IsModifiable(true, ref result))
                                                    {
                                                        Assembly assembly = null;

                                                        try
                                                        {
                                                            switch (loadType)
                                                            {
                                                                case LoadType.PartialName:
                                                                    {
                                                                        assembly = Assembly.LoadWithPartialName(
                                                                            arguments[argumentIndex]);

                                                                        if (assembly == null)
                                                                        {
                                                                            result = String.Format(
                                                                                "could not load assembly based on partial name {0}",
                                                                                FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                        break;
                                                                    }
                                                                case LoadType.FullName:
                                                                    {
                                                                        assembly = Assembly.Load(
                                                                            arguments[argumentIndex]);
                                                                        break;
                                                                    }
                                                                case LoadType.File:
                                                                    {
                                                                        assembly = Assembly.LoadFrom(
                                                                            arguments[argumentIndex]);
                                                                        break;
                                                                    }
                                                                case LoadType.Bytes:
                                                                    {
                                                                        assembly = Assembly.Load(
                                                                            Convert.FromBase64String(
                                                                                arguments[argumentIndex]));
                                                                        break;
                                                                    }
                                                                case LoadType.Stream:
                                                                    {
                                                                        IObject @object = null;

                                                                        code = interpreter.GetObject(
                                                                            arguments[argumentIndex], LookupFlags.Default,
                                                                            ref @object, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            Stream stream = @object.Value as Stream;

                                                                            if (stream != null)
                                                                            {
                                                                                if (stream.CanRead && stream.CanSeek)
                                                                                {
                                                                                    long length = stream.Length;

                                                                                    if (length <= int.MaxValue)
                                                                                    {
                                                                                        int intLength = (int)length;
                                                                                        byte[] bytes = new byte[intLength];

                                                                                        stream.Read(bytes, 0, intLength);
                                                                                        assembly = Assembly.Load(bytes);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = String.Format(
                                                                                            "stream for object {0} too large",
                                                                                            FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = String.Format(
                                                                                        "stream for object {0} must " +
                                                                                        "support seeking and reading",
                                                                                        FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                result = String.Format(
                                                                                    "invalid stream for object {0}",
                                                                                    FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        break;
                                                                    }
                                                                default:
                                                                    {
                                                                        result = String.Format(
                                                                            "unsupported assembly load type {0}",
                                                                            FormatOps.WrapOrNull(loadType));

                                                                        code = ReturnCode.Error;
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            Engine.SetExceptionErrorCode(interpreter, e);

                                                            result = e;
                                                            code = ReturnCode.Error;
                                                        }

                                                        if ((code == ReturnCode.Ok) && (@namespace != null))
                                                            code = interpreter.AddObjectAliasNamespace(assembly,
                                                                @namespace, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            ObjectOptionType objectOptionType = ObjectOptionType.Load |
                                                                ObjectOps.GetOptionType(aliasRaw, aliasAll);

                                                            code = MarshalOps.FixupReturnValue(
                                                                interpreter, interpreter.Binder, interpreter.CultureInfo,
                                                                returnType, objectFlags, ObjectOps.GetInvokeOptions(
                                                                objectOptionType), objectOptionType, objectName,
                                                                interpName, assembly, create, dispose, alias,
                                                                aliasReference, toString, ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) && import)
                                                            code = interpreter.AddObjectNamespaces(assembly, importNonPublic,
                                                                importMatchMode, importPattern, importNoCase, ref result);

                                                        if ((code == ReturnCode.Ok) && declare)
                                                            code = interpreter.AddObjectInterfaces(assembly, declareNonPublic,
                                                                declareMatchMode, declarePattern, declareNoCase,
                                                                ref result);
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
                                                        result = "wrong # args: should be \"object load ?options? assembly\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object load ?options? assembly\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "members":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetMembersOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    TypeList objectTypes;
                                                    MemberTypes memberTypes;
                                                    BindingFlags bindingFlags;
                                                    MatchMode matchMode;
                                                    string pattern;
                                                    bool verbose;
                                                    bool strictType;
                                                    bool noCase;
                                                    bool attributes;
                                                    bool matchNameOnly;
                                                    bool nameOnly;
                                                    bool signatures;
                                                    bool qualified;

                                                    ObjectOps.ProcessObjectMembersOptions(
                                                        options, null, null, null, out objectTypes,
                                                        out memberTypes, out bindingFlags, out matchMode,
                                                        out pattern, out verbose, out strictType,
                                                        out noCase, out attributes, out matchNameOnly,
                                                        out nameOnly, out signatures, out qualified);

                                                    //
                                                    // NOTE: Check for a pattern without a mode (change to
                                                    //       default, which is Glob).
                                                    //
                                                    if ((pattern != null) && (matchMode == MatchMode.None))
                                                        matchMode = StringOps.DefaultObjectMatchMode;

                                                    Variant value = null;
                                                    Type type = null;

                                                    if (options.IsPresent("-type", ref value))
                                                        type = (Type)value.Value;

                                                    if (type == null)
                                                    {
                                                        IObject @object = null;

                                                        if (interpreter.GetObject(
                                                                arguments[argumentIndex], LookupFlags.NoVerbose,
                                                                ref @object) == ReturnCode.Ok)
                                                        {
                                                            if (@object.Value != null)
                                                            {
                                                                type = @object.Value.GetType();
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "invalid value for object {0}",
                                                                    FormatOps.WrapOrNull(arguments[argumentIndex]));

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            ResultList errors = null;

                                                            code = Value.GetType(
                                                                interpreter, arguments[argumentIndex], objectTypes,
                                                                interpreter.GetAppDomain(), Value.GetTypeValueFlags(
                                                                strictType, verbose, noCase), interpreter.CultureInfo,
                                                                ref type, ref errors);

                                                            if (code != ReturnCode.Ok)
                                                            {
                                                                errors.Insert(0, String.Format(
                                                                    "object or type {0} not found",
                                                                    FormatOps.WrapOrNull(arguments[argumentIndex])));

                                                                result = errors;
                                                            }
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did the type lookup above succeed?
                                                    //
                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        MemberInfo[] memberInfo = type.GetMembers(bindingFlags);

                                                        if ((memberInfo != null) && (memberInfo.Length > 0))
                                                        {
                                                            StringDictionary dictionary = new StringDictionary();

                                                            for (int index = 0; index < memberInfo.Length; index++)
                                                            {
                                                                MemberInfo thisMemberInfo = memberInfo[index];
                                                                MemberTypes thisMemberType = thisMemberInfo.MemberType;

                                                                if (!FlagOps.HasFlags(memberTypes, thisMemberType, true))
                                                                    continue;

                                                                string thisMemberName = thisMemberInfo.Name;

                                                                StringList list = nameOnly ?
                                                                    new StringList(thisMemberName) :
                                                                    new StringList(
                                                                        "memberType", thisMemberType.ToString(),
                                                                        "memberName", thisMemberName);

                                                                if (attributes)
                                                                {
                                                                    EventAttributes? eventAttributes =
                                                                        MarshalOps.GetEventAttributes(thisMemberInfo);

                                                                    if (eventAttributes != null)
                                                                    {
                                                                        list.Add("eventAttributes");
                                                                        list.Add(eventAttributes.ToString());
                                                                    }

                                                                    FieldAttributes? fieldAttributes =
                                                                        MarshalOps.GetFieldAttributes(thisMemberInfo);

                                                                    if (fieldAttributes != null)
                                                                    {
                                                                        list.Add("fieldAttributes");
                                                                        list.Add(fieldAttributes.ToString());
                                                                    }

                                                                    MethodAttributes? methodAttributes =
                                                                        MarshalOps.GetMethodAttributes(thisMemberInfo);

                                                                    if (methodAttributes != null)
                                                                    {
                                                                        list.Add("methodAttributes");
                                                                        list.Add(methodAttributes.ToString());
                                                                    }

                                                                    PropertyAttributes? propertyAttributes =
                                                                        MarshalOps.GetPropertyAttributes(thisMemberInfo);

                                                                    if (propertyAttributes != null)
                                                                    {
                                                                        list.Add("propertyAttributes");
                                                                        list.Add(propertyAttributes.ToString());
                                                                    }

                                                                    TypeAttributes? typeAttributes =
                                                                        MarshalOps.GetTypeAttributes(thisMemberInfo);

                                                                    if (typeAttributes != null)
                                                                    {
                                                                        list.Add("typeAttributes");
                                                                        list.Add(typeAttributes.ToString());
                                                                    }
                                                                }

                                                                //
                                                                // NOTE: Check if we want all the method signatures.  This
                                                                //       option does NOT apply to fields or types because
                                                                //       they never have a MethodInfo associated with them.
                                                                //
                                                                if (signatures)
                                                                {
                                                                    if (!(thisMemberInfo is FieldInfo) &&
                                                                        !(thisMemberInfo is Type))
                                                                    {
                                                                        MethodBase[] methodBase = null;

                                                                        code = MarshalOps.GetMethodBaseFromMemberInfo(
                                                                            new MemberInfo[] { thisMemberInfo },
                                                                            bindingFlags, ref methodBase, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            for (int index2 = 0; index2 < methodBase.Length; index2++)
                                                                            {
                                                                                TypeList returnType = null;

                                                                                code = MarshalOps.GetTypeListFromParameterInfo(
                                                                                    new ParameterInfo[] {
                                                                                        MarshalOps.GetReturnParameterInfo(methodBase[index2]) },
                                                                                    true, ref returnType, ref result);

                                                                                if (code != ReturnCode.Ok)
                                                                                    break;

                                                                                TypeList parameterTypes = null;

                                                                                code = MarshalOps.GetTypeListFromParameterInfo(
                                                                                    methodBase[index2].GetParameters(), false,
                                                                                    ref parameterTypes, ref result);

                                                                                if (code != ReturnCode.Ok)
                                                                                    break;

                                                                                list.AddRange(new StringList(
                                                                                    "methodType",
                                                                                    //
                                                                                    // NOTE: This should always be "Method"; however, we
                                                                                    //       do need to include it for script comparison
                                                                                    //       purposes and we do not want to hard-code it.
                                                                                    //
                                                                                    methodBase[index2].MemberType.ToString(),
                                                                                    "methodName",
                                                                                    methodBase[index2].Name,
                                                                                    "callingConvention",
                                                                                    methodBase[index2].CallingConvention.ToString(),
                                                                                    "returnType",
                                                                                    returnType.ToString(null, false, qualified),
                                                                                    "parameterTypes",
                                                                                    parameterTypes.ToString(null, false, qualified)));
                                                                            }
                                                                        }
                                                                    }
                                                                    else if (thisMemberInfo is FieldInfo)
                                                                    {
                                                                        FieldInfo fieldInfo = (FieldInfo)thisMemberInfo;

                                                                        TypeList fieldType = new TypeList(
                                                                            new Type[] { fieldInfo.FieldType });

                                                                        list.AddRange(new StringList(
                                                                            "fieldType",
                                                                            fieldType.ToString(null, false, qualified)));
                                                                    }
                                                                }

                                                                if (code != ReturnCode.Ok)
                                                                    break;

                                                                if ((matchMode == MatchMode.None) ||
                                                                    (matchNameOnly &&
                                                                        StringOps.Match(interpreter, matchMode, thisMemberName, pattern, noCase)) ||
                                                                    (!matchNameOnly && (list != null) &&
                                                                        StringOps.Match(interpreter, matchMode, list.ToString(), pattern, noCase)))
                                                                {
                                                                    //
                                                                    // BUGFIX: Avoid adding duplicate members (this could
                                                                    //         previously happen if a method had multiple
                                                                    //         overloads and the "-signatures" option was not
                                                                    //         specified).
                                                                    //
                                                                    string key = list.ToString();

                                                                    if (!dictionary.ContainsKey(key))
                                                                        dictionary.Add(key, null);
                                                                }
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                                result = dictionary.ToString();
                                                        }
                                                        else
                                                        {
                                                            result = String.Empty;
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
                                                        result = "wrong # args: should be \"object members ?options? object\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object members ?options? object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "namespaces":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 3)
                                                    pattern = arguments[2];

                                                StringDictionary dictionary = interpreter.ObjectNamespaces;

                                                if (dictionary != null)
                                                    result = dictionary.KeysAndValuesToString(pattern, false);
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object namespaces ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "refcount":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IObject @object = null;

                                            code = interpreter.GetObject(
                                                arguments[2], LookupFlags.Default,
                                                ref @object, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = @object.ReferenceCount;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object refcount object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "removecallback":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = interpreter.RemoveCallback(arguments[2], clientData, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object removecallback name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "removeref":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = interpreter.RemoveObjectReference(
                                                code, arguments[2], ObjectReferenceType.Demand,
                                                true, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object removeref object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "resolve":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = MarshalOps.ResolveAssembly(arguments[2], ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object resolve assembly\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "search":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = ObjectOps.GetSearchOptions();
                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    TypeList objectTypes;
                                                    bool verbose;
                                                    bool strictType;
                                                    bool noCase;

                                                    ObjectOps.ProcessGetTypeOptions(
                                                        options, out objectTypes, out verbose,
                                                        out strictType, out noCase);

                                                    ValueFlags valueFlags = ValueFlags.ObjectSearch;

                                                    if (options.IsPresent("-noshowname"))
                                                        valueFlags &= ~ValueFlags.ShowName;

                                                    if (options.IsPresent("-nonamespace"))
                                                        valueFlags |= ValueFlags.NoNamespace;

                                                    if (options.IsPresent("-noassembly"))
                                                        valueFlags |= ValueFlags.NoAssembly;

                                                    if (options.IsPresent("-noexception"))
                                                        valueFlags |= ValueFlags.NoException;

                                                    if (options.IsPresent("-fullname"))
                                                        valueFlags |= ValueFlags.FullName;

                                                    Type objectType = null;
                                                    ResultList errors = null;

                                                    code = Value.GetType(
                                                        interpreter, arguments[argumentIndex], objectTypes,
                                                        interpreter.GetAppDomain(), Value.GetTypeValueFlags(
                                                        strictType, verbose, noCase) | valueFlags,
                                                        interpreter.CultureInfo, ref objectType, ref errors);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        result = (objectType != null) ?
                                                            objectType.ToString() : typeof(object).ToString();
                                                    }
                                                    else
                                                    {
                                                        result = errors;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object search ?options? typeName\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object search ?options? typeName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "strongname":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IObject @object = null;

                                            code = interpreter.GetObject(
                                                arguments[2], LookupFlags.Default,
                                                ref @object, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Assembly assembly = (@object != null) ? @object.Value as Assembly : null;
                                                StrongName strongName = null;

                                                code = AssemblyOps.GetStrongName(assembly, ref strongName, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = FormatOps.StrongName(assembly, strongName, true);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object strongname assembly\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "type":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetTypeOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    MatchMode matchMode;
                                                    string pattern;
                                                    bool noCase;

                                                    ObjectOps.ProcessObjectTypeOptions(
                                                        options, null, out matchMode, out pattern,
                                                        out noCase);

                                                    //
                                                    // NOTE: Check for a pattern without a mode (change to
                                                    //       default, which is Glob).
                                                    //
                                                    if ((pattern != null) && (matchMode == MatchMode.None))
                                                        matchMode = StringOps.DefaultObjectMatchMode;

                                                    //
                                                    // NOTE: Figure out which ones they want to add.
                                                    //
                                                    StringList list = new StringList();

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // NOTE: Any specific names they want to add?
                                                        //
                                                        if (argumentIndex != Index.Invalid)
                                                            list.Add(ArgumentList.GetRange(
                                                                arguments, argumentIndex, Index.Invalid));

                                                        //
                                                        // NOTE: Ok, add the specified names now.
                                                        //
                                                        code = interpreter.AddObjectTypes(
                                                            list, matchMode, pattern, noCase, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = String.Empty;
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
                                                        result = "wrong # args: should be \"object type ?options? ?fromName toName...? fromName toName\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object type ?options? ?fromName toName...? fromName toName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "types":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                            {
                                                string pattern = null;

                                                if (arguments.Count == 3)
                                                    pattern = arguments[2];

                                                StringDictionary list = interpreter.ObjectTypes;

                                                if (list != null)
                                                    result = list.KeysAndValuesToString(pattern, false);
                                                else
                                                    result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object types ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unalias":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = interpreter.RemoveAliasAndCommand(
                                                arguments[2], clientData, false, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object unalias object\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unaliasnamespace":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetUnaliasNamespaceOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    MatchMode matchMode;
                                                    string pattern;
                                                    bool noCase;
                                                    bool values;

                                                    ObjectOps.ProcessObjectUnaliasNamespaceOptions(
                                                        options, null, out matchMode, out pattern,
                                                        out noCase, out values);

                                                    //
                                                    // NOTE: Check for a pattern without a mode (change to
                                                    //       default, which is Glob).
                                                    //
                                                    if ((pattern != null) && (matchMode == MatchMode.None))
                                                        matchMode = StringOps.DefaultObjectMatchMode;

                                                    //
                                                    // NOTE: Ok, remove the specified alias namespaces now.
                                                    //
                                                    code = interpreter.RemoveObjectAliasNamespaces(
                                                        matchMode, pattern, noCase, values, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object unaliasnamespace ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object unaliasnamespace ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "undeclare":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetUndeclareOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    MatchMode matchMode;
                                                    string pattern;
                                                    bool noCase;
                                                    bool values;

                                                    ObjectOps.ProcessObjectUndeclareOptions(
                                                        options, null, out matchMode, out pattern,
                                                        out noCase, out values);

                                                    //
                                                    // NOTE: Check for a pattern without a mode (change to
                                                    //       default, which is Glob).
                                                    //
                                                    if ((pattern != null) && (matchMode == MatchMode.None))
                                                        matchMode = StringOps.DefaultObjectMatchMode;

                                                    //
                                                    // NOTE: Ok, remove the specified interfaces now.
                                                    //
                                                    code = interpreter.RemoveObjectInterfaces(
                                                        matchMode, pattern, noCase, values, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object undeclare ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object undeclare ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unimport":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetUnimportOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    MatchMode matchMode;
                                                    string pattern;
                                                    bool noCase;
                                                    bool values;

                                                    ObjectOps.ProcessObjectUnimportOptions(
                                                        options, null, out matchMode, out pattern,
                                                        out noCase, out values);

                                                    //
                                                    // NOTE: Check for a pattern without a mode (change to
                                                    //       default, which is Glob).
                                                    //
                                                    if ((pattern != null) && (matchMode == MatchMode.None))
                                                        matchMode = StringOps.DefaultObjectMatchMode;

                                                    //
                                                    // NOTE: Ok, remove the specified namespaces now.
                                                    //
                                                    code = interpreter.RemoveObjectNamespaces(
                                                        matchMode, pattern, noCase, values, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object unimport ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object unimport ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "untype":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = ObjectOps.GetTypeOptions();
                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    MatchMode matchMode;
                                                    string pattern;
                                                    bool noCase;

                                                    ObjectOps.ProcessObjectUntypeOptions(
                                                        options, null, out matchMode, out pattern,
                                                        out noCase);

                                                    //
                                                    // NOTE: Check for a pattern without a mode (change to
                                                    //       default, which is Glob).
                                                    //
                                                    if ((pattern != null) && (matchMode == MatchMode.None))
                                                        matchMode = StringOps.DefaultObjectMatchMode;

                                                    //
                                                    // NOTE: Ok, remove the specified names now.
                                                    //
                                                    code = interpreter.RemoveObjectTypes(matchMode, pattern, noCase, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"object untype ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object untype ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "verifyall":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool strict = false;

                                            if (arguments.Count == 3)
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref strict, ref result);

                                            X509VerificationFlags x509VerificationFlags =
                                                CertificateOps.DefaultVerificationFlags;

                                            X509RevocationMode x509RevocationMode =
                                                CertificateOps.DefaultRevocationMode;

                                            X509RevocationFlag x509RevocationFlag =
                                                CertificateOps.DefaultRevocationFlag;

                                            if (code == ReturnCode.Ok)
                                            {
                                                AppDomain appDomain = interpreter.GetAppDomain();

                                                if (appDomain != null)
                                                {
                                                    Assembly[] assemblies = appDomain.GetAssemblies();

                                                    if (assemblies != null)
                                                    {
                                                        foreach (Assembly assembly in assemblies)
                                                        {
                                                            if (assembly != null)
                                                            {
                                                                if (strict || !assembly.GlobalAssemblyCache)
                                                                {
                                                                    StrongName strongName = null;

                                                                    code = AssemblyOps.GetStrongName(
                                                                        assembly, ref strongName, ref result);

                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        result = String.Format(
                                                                            "assembly {0}: {1}",
                                                                            FormatOps.WrapOrNull(assembly),
                                                                            result);

                                                                        break;
                                                                    }

                                                                    if (!RuntimeOps.IsStrongNameVerified(
                                                                            assembly.Location, true))
                                                                    {
                                                                        result = String.Format(
                                                                            "assembly {0}: not fully-signed",
                                                                            FormatOps.WrapOrNull(assembly));

                                                                        code = ReturnCode.Error;
                                                                        break;
                                                                    }

                                                                    X509Certificate2 certificate2 = null;

                                                                    code = AssemblyOps.GetCertificate2(
                                                                        assembly, true, ref certificate2, ref result);

                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        result = String.Format(
                                                                            "assembly {0}: {1}",
                                                                            FormatOps.WrapOrNull(assembly),
                                                                            result);

                                                                        break;
                                                                    }

                                                                    code = CertificateOps.VerifyChain(
                                                                        assembly, certificate2, x509VerificationFlags,
                                                                        x509RevocationMode, x509RevocationFlag,
                                                                        ref result);

                                                                    if (code != ReturnCode.Ok)
                                                                        break;

                                                                    if (!RuntimeOps.IsFileTrusted(
                                                                            assembly.Location, IntPtr.Zero))
                                                                    {
                                                                        result = String.Format(
                                                                            "assembly {0}: is not trusted",
                                                                            FormatOps.WrapOrNull(assembly));

                                                                        code = ReturnCode.Error;
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "invalid assembly";
                                                                code = ReturnCode.Error;
                                                                break;
                                                            }
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                            result = String.Empty;
                                                    }
                                                    else
                                                    {
                                                        result = "invalid assemblies";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "invalid application domain";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"object verifyall ?strict?\"";
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
                        result = "wrong # args: should be \"object option ?arg ...?\"";
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
