/*
 * Default.cs --
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
using System.Diagnostics;
using System.Globalization;
using System.IO;

#if NETWORK
using System.Net;
#endif

using System.Reflection;
using System.Runtime.InteropServices;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

#if REMOTING
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
#endif

#if CAS_POLICY
using System.Security.Policy;
#endif

#if NETWORK && REMOTING
using System.Security.Principal;
#endif

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

using _Public = Eagle._Components.Public;

namespace Eagle._Tests
{
    [ObjectId("e1257294-a012-4164-b0ea-3763dd06eec2")]
    public class Default : IDisposable
    {
        #region Private Constants
#if REMOTING
        private static readonly string RemotingChannelName = String.Empty;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string TestCustomInfoBoxName = "TestCustomInfo";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Delegates
        [ObjectId("13daa524-57d2-470e-b67d-23490b21e0d0")]
        private delegate void VoidWithStringCallback(string value);

        [ObjectId("ad67563c-a83c-4cb4-a91b-aaed767f57a0")]
        private delegate long LongWithDateTimeCallback(DateTime dateTime);

        [ObjectId("c3cb55e9-3e8f-4e0d-95ec-d2b324f36715")]
        private delegate IEnumerable IEnumerableWithICommandCallback(ICommand command);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Delegates
        [ObjectId("d75fb203-a5eb-4688-83aa-487dff0119c7")]
        public delegate int TwoArgsDelegate(string param1, string param2);

        [ObjectId("55a64fc1-79e0-4236-a5af-d3a31b261591")]
        public delegate void ThreeArgsDelegate(object[] args, int value, ref object data);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Default()
        {
            @event = ThreadOps.CreateEvent(false);
            intArrayField = new int[10];
            privateField = this.ToString();
            intPtrArrayField = Array.CreateInstance(typeof(IntPtr), new int[] { 2, 3 });
            objectArrayField = Array.CreateInstance(typeof(Default), new int[] { 1 });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default(sbyte value)
            : this()
        {
            internalField = value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default(sbyte value1, bool value2)
            : this(value1)
        {
            uniqueToString = value2;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static Type staticTypeField;
        private static object staticObjectField;
        private static StringPairList customInfoList;
        private static ObjectWrapperDictionary savedObjects;
        private static DateTime now;
        private static DateTimeNowCallback nowCallback;
        private static long nowIncrement;
        private static bool staticDynamicInvoke;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Data
        public int[] intArrayField;
        public bool boolField;
        public byte byteField;
        public short shortField;
        public int intField;
        public long longField;
        public decimal decimalField;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        internal sbyte internalField;
        private EventWaitHandle @event;
        private Result asyncResult;
        private string privateField;
        private Type typeField;
        private object objectField;
        private Array intPtrArrayField;
        private Array objectArrayField;
        private Interpreter callbackInterpreter;
        private string newInterpreterText;
        private string complainCommandName;
        private bool complainWithThrow;
        private ReturnCode complainCode;
        private Result complainResult;
        private int complainErrorLine;
        private bool dynamicInvoke;
        private string packageFallbackText;
        private bool uniqueToString;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Methods for StringOps.MatchCore
        public static void TestSetMatchCallback(
            Interpreter interpreter,
            bool setup
            )
        {
            if (interpreter == null)
                return;

            if (setup)
                interpreter.MatchCallback = TestMatchCallback;
            else
                interpreter.MatchCallback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        public static void TestSetBreakOrFailOnComplain(
            Interpreter interpreter, /* NOT USED */
            bool setup,
            bool useFail
            )
        {
            ComplainCallback callback = useFail ?
                (ComplainCallback)TestComplainCallbackFail :
                TestComplainCallbackBreak;

            Interpreter.ComplainCallback = setup ? callback : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetComplainCallback(
            Interpreter interpreter, /* NOT USED */
            bool setup,
            bool withThrow
            )
        {
            if (setup)
            {
                Interpreter.ComplainCallback = withThrow ?
                    (ComplainCallback)TestComplainCallbackThrow :
                    TestComplainCallbackNoThrow;
            }
            else
            {
                Interpreter.ComplainCallback = null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ScriptWebClient
#if NETWORK
        public static bool TestHasScriptNewWebClientCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return (interpreter.NewWebClientCallback == TestScriptNewWebClientCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestHasErrorNewWebClientCallback(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return (interpreter.NewWebClientCallback == TestErrorNewWebClientCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetScriptNewWebClientCallback(
            Interpreter interpreter,
            bool enable,
            bool success,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (enable)
            {
                interpreter.NewWebClientCallback = success ?
                    (NewWebClientCallback)TestScriptNewWebClientCallback :
                    TestErrorNewWebClientCallback;
            }
            else
            {
                interpreter.NewWebClientCallback = null;
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for AddExecuteCallback
        public static ReturnCode TestExecuteCallback1(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                //
                // HACK: Just default the command name to "eval" here
                //       when there are zero arguments.
                //
                result = String.Format(
                    "wrong # args: should be \"{0} arg ?arg ...?\"",
                    (arguments.Count > 0) ? (string)arguments[0] : "command");

                return ReturnCode.Error;
            }

            return interpreter.EvaluateScript(arguments, 1, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestExecuteCallback2(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 3)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} {1} arg ?arg ...?\"",
                    (arguments.Count > 0) ? (string)arguments[0] : "command",
                    (arguments.Count > 1) ? (string)arguments[1] : "subcommand");

                return ReturnCode.Error;
            }

            return interpreter.EvaluateScript(arguments, 2, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddExecuteCallback(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddExecuteCallback(
                name, TestExecuteCallback1, clientData, ref token,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddExecuteCallback(
            Interpreter interpreter,
            string name,
            ICommand command,
            IClientData clientData,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddExecuteCallback(
                name, command, TestExecuteCallback2, clientData,
                ref token, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ValueType TestStaticValueType(
            ValueType value
            )
        {
            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ValueType TestStaticByRefValueType(
            ref ValueType value
            )
        {
            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestStaticObjectIdentity(
            object value
            )
        {
            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestGetResourceString(
            Interpreter interpreter,
            string name,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return null;
            }

            if (name == null)
            {
                error = "invalid name";
                return null;
            }

            try
            {
                IFileSystemHost fileSystemHost = interpreter.Host;

                if (fileSystemHost == null)
                {
                    error = "interpreter host not available";
                    return null;
                }

                scriptFlags |= ScriptFlags.CoreAssemblyOnly;
                Result result = null;

                if (fileSystemHost.GetScript(
                        name, ref scriptFlags, ref clientData,
                        ref result) == ReturnCode.Ok)
                {
                    return result;
                }
                else
                {
                    error = result;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestArePluginsIsolated(
            Interpreter interpreter
            )
        {
#if ISOLATED_PLUGINS
            if (interpreter != null)
            {
                lock (interpreter.SyncRoot)
                {
                    return FlagOps.HasFlags(
                        interpreter.PluginFlags, PluginFlags.Isolated, true);
                }
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestMethod(
            string argument /* This is the value of the "pwzArgument" argument
                             * as it was passed to native CLR API method
                             * ICLRRuntimeHost.ExecuteInDefaultAppDomain. */
            )
        {
            int value = 0;

            if (Value.GetInteger2(
                    argument, ValueFlags.AnyInteger, null,
                    ref value) == ReturnCode.Ok)
            {
                return value;
            }

            return -1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetQuiet(
            Interpreter interpreter,
            bool quiet
            )
        {
            if (interpreter == null)
                return;

            interpreter.Quiet = quiet;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetNoBackgroundError(
            Interpreter interpreter,
            bool noBackgroundError
            )
        {
            if (interpreter == null)
                return;

            interpreter.SetNoBackgroundError(noBackgroundError);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestSetComplain(
            Interpreter interpreter,
            bool complain
            )
        {
            if (interpreter == null)
                return false;

            IInteractiveHost interactiveHost = interpreter.Host;

            if (interactiveHost == null)
                return false;

            FieldInfo fieldInfo = interactiveHost.GetType().GetField(
                "hostFlags", MarshalOps.PrivateInstanceGetFieldBindingFlags);

            if (fieldInfo == null)
                return false;

            HostFlags hostFlags = (HostFlags)fieldInfo.GetValue(
                interactiveHost);

            if (complain)
                hostFlags |= HostFlags.Complain;
            else
                hostFlags &= ~HostFlags.Complain;

            fieldInfo.SetValue(interactiveHost, hostFlags);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
#if INTERACTIVE_COMMANDS
        public static bool TestDisposedWriteHeader(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot)
            {
                IInteractiveHost interactiveHost = interpreter.Host;

                interpreter.SetDisposed(true);

                try
                {
                    InteractiveLoopData loopData = new InteractiveLoopData(
                        null, ReturnCode.Ok, null, null, HeaderFlags.All |
                        HeaderFlags.AllEmptyFlags);

                    bool show = false;

                    InteractiveOps.Commands.show(
                        interpreter, interactiveHost, new ArgumentList(),
                        loopData, null, loopData.HeaderFlags, ReturnCode.Ok,
                        null, ref show);

                    return true;
                }
                catch (Exception e)
                {
                    DebugOps.Complain(interpreter, ReturnCode.Error, e);

                    return false;
                }
                finally
                {
                    interpreter.SetDisposed(false);
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ExitCode TestShellMainCore(
            Interpreter interpreter,
            IEnumerable<string> args,
            bool initialize,
            bool loop,
            ref Result result
            )
        {
            ShellCallbackData callbackData = ShellCallbackData.Create();

            if (callbackData == null)
            {
                result = "could not create shell callback data";
                return ExitCode.Failure;
            }

            callbackData.ArgumentCallback = TestShellArgumentCallback;

            return Interpreter.ShellMainCore(
                interpreter, callbackData, null, args, initialize, loop,
                ref result);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddLoadPluginPolicy(
            Interpreter interpreter,
            IClientData clientData,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddPolicy(
                TestLoadPluginPolicy, null, clientData, ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestLoadPluginViaBytes(
            Interpreter interpreter,
            string assemblyFileName,
#if CAS_POLICY
            Evidence evidence,
#endif
            string typeName,
            IClientData clientData,
            PluginFlags pluginFlags,
            ref IPlugin plugin,
            ref long token,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(assemblyFileName))
            {
                result = "invalid assembly file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(assemblyFileName))
            {
                result = String.Format(
                    "cannot load plugin: assembly file {0} does not exist",
                    FormatOps.WrapOrNull(assemblyFileName));

                return ReturnCode.Error;
            }

            if (plugin != null)
            {
                result = "cannot overwrite valid plugin";
                return ReturnCode.Error;
            }

            if (token != 0)
            {
                result = "cannot overwrite valid plugin token";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;

            try
            {
                //
                // NOTE: Figure out what the debug symbols file for the
                //       assembly would be if it actually existed.
                //
                string symbolFileName = PathOps.GetNativePath(
                    PathOps.CombinePath(null, Path.GetDirectoryName(
                    assemblyFileName), Path.GetFileNameWithoutExtension(
                    assemblyFileName) + FileExtension.Symbols)); /* throw */

                byte[] assemblyBytes = File.ReadAllBytes(
                    assemblyFileName); /* throw */

                byte[] symbolBytes = File.Exists(symbolFileName) ?
                    File.ReadAllBytes(symbolFileName) : null; /* throw */

                code = interpreter.LoadPlugin(assemblyBytes, symbolBytes,
#if CAS_POLICY
                    evidence,
#endif
                    typeName, clientData, pluginFlags, ref plugin, ref result);

                if (code == ReturnCode.Ok)
                {
                    code = interpreter.AddPlugin(
                        plugin, clientData, ref token, ref result);
                }

                return code;
            }
            catch (Exception e)
            {
                result = e;
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (plugin != null))
                {
                    ReturnCode unloadCode;
                    Result unloadResult = null;

                    unloadCode = interpreter.UnloadPlugin(
                        plugin, clientData, pluginFlags, ref unloadResult);

                    if (unloadCode != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, unloadCode, unloadResult);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRenameNamespace(
            Interpreter interpreter,
            string oldName,
            string newName,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(newName))
            {
                error = "invalid or empty new namespace name";
                return ReturnCode.Error;
            }

            if (NamespaceOps.IsQualifiedName(newName))
            {
                error = "new namespace name must not be qualified";
                return ReturnCode.Error;
            }

            INamespace @namespace = NamespaceOps.Lookup(
                interpreter, oldName, false, false, ref error);

            if (@namespace == null)
                return ReturnCode.Error;

            try
            {
                @namespace.Name = newName;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSaveObjects(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            interpreter.SaveObjects(ref savedObjects);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestRestoreObjects(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            interpreter.RestoreObjects(ref savedObjects);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestExecuteDelegateCommands(
            Interpreter interpreter,
            ArgumentList arguments,
            bool dynamic,
            ref ReturnCodeList returnCodes,
            ref ResultList results
            )
        {
            if (returnCodes == null)
                returnCodes = new ReturnCodeList();

            if (results == null)
                results = new ResultList();

            long[] tokens = { 0, 0, 0 };

            try
            {
                ReturnCode code;
                Result result = null;

                if (interpreter == null)
                {
                    returnCodes.Add(ReturnCode.Error);
                    results.Add("invalid interpreter");

                    return;
                }

                if (arguments == null)
                {
                    returnCodes.Add(ReturnCode.Error);
                    results.Add("invalid argument list");

                    return;
                }

                IPlugin plugin =
#if DEBUG
                    interpreter.GetTestPlugin(ref result);
#else
                    interpreter.GetCorePlugin(ref result);
#endif

                if (plugin == null)
                {
                    returnCodes.Add(ReturnCode.Error);
                    results.Add(result);

                    return;
                }

                Delegate[] delegates = { null, null, null };

                if (dynamic)
                {
                    MethodInfo[] methodInfo = { null, null, null };

                    methodInfo[0] = typeof(Default).GetMethod(
                        "TestVoidMethod",
                        MarshalOps.PrivateStaticBindingFlags);

                    if (methodInfo[0] == null)
                    {
                        returnCodes.Add(ReturnCode.Error);
                        results.Add("method TestVoidMethod not found");

                        return;
                    }

                    methodInfo[1] = typeof(Default).GetMethod(
                        "TestLongMethod",
                        MarshalOps.PrivateStaticBindingFlags);

                    if (methodInfo[1] == null)
                    {
                        returnCodes.Add(ReturnCode.Error);
                        results.Add("method TestLongMethod not found");

                        return;
                    }

                    methodInfo[2] = typeof(Default).GetMethod(
                        "TestIEnumerableMethod",
                        MarshalOps.PrivateStaticBindingFlags);

                    if (methodInfo[2] == null)
                    {
                        returnCodes.Add(ReturnCode.Error);
                        results.Add("method TestIEnumerableMethod not found");

                        return;
                    }

                    Type[] types = { null, null, null };

                    code = DelegateOps.CreateManagedDelegateType(
                        interpreter, null, null, null, null,
                        methodInfo[0].ReturnType,
                        GetParameterTypeList(methodInfo[0]), ref types[0],
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        returnCodes.Add(code);
                        results.Add(result);

                        return;
                    }

                    code = DelegateOps.CreateManagedDelegateType(
                        interpreter, null, null, null, null,
                        methodInfo[1].ReturnType,
                        GetParameterTypeList(methodInfo[1]), ref types[1],
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        returnCodes.Add(code);
                        results.Add(result);

                        return;
                    }

                    code = DelegateOps.CreateManagedDelegateType(
                        interpreter, null, null, null, null,
                        methodInfo[2].ReturnType,
                        GetParameterTypeList(methodInfo[2]), ref types[2],
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        returnCodes.Add(code);
                        results.Add(result);

                        return;
                    }

                    delegates[0] = Delegate.CreateDelegate(types[0],
                        methodInfo[0], false);

                    delegates[1] = Delegate.CreateDelegate(types[1],
                        methodInfo[1], false);

                    delegates[2] = Delegate.CreateDelegate(types[2],
                        methodInfo[2], false);
                }
                else
                {
                    delegates[0] = new VoidWithStringCallback(
                        TestVoidMethod);

                    delegates[1] = new LongWithDateTimeCallback(
                        TestLongMethod);

                    delegates[2] = new IEnumerableWithICommandCallback(
                        TestIEnumerableMethod);
                }

                ICommand[] commands = { null, null, null };

                commands[0] = new _Commands._Delegate(new CommandData(
                    "voidDelegate", null, null, ClientData.Empty,
                    typeof(_Commands._Delegate).FullName, CommandFlags.None,
                    plugin, 0), new DelegateData(delegates[0], false, 0));

                commands[1] = new _Commands._Delegate(new CommandData(
                    "longDelegate", null, null, ClientData.Empty,
                    typeof(_Commands._Delegate).FullName, CommandFlags.None,
                    plugin, 0), new DelegateData(delegates[1], false, 0));

                commands[2] = new _Commands._Delegate(new CommandData(
                    "enumerableDelegate", null, null, ClientData.Empty,
                    typeof(_Commands._Delegate).FullName, CommandFlags.None,
                    plugin, 0), new DelegateData(delegates[2], false, 0));

                code = interpreter.AddCommand(
                    commands[0], ClientData.Empty, ref tokens[0],
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    returnCodes.Add(code);
                    results.Add(result);

                    return;
                }

                code = interpreter.AddCommand(
                    commands[1], ClientData.Empty, ref tokens[1],
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    returnCodes.Add(code);
                    results.Add(result);

                    return;
                }

                code = interpreter.AddCommand(
                    commands[2], ClientData.Empty, ref tokens[2],
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    returnCodes.Add(code);
                    results.Add(result);

                    return;
                }

                arguments.Insert(0, commands[0].Name);

                code = Engine.EvaluateScript(interpreter, arguments.ToString(),
                    ref result);

                returnCodes.Add(code);
                results.Add(result);

                arguments[0] = commands[1].Name;

                code = Engine.EvaluateScript(interpreter, arguments.ToString(),
                    ref result);

                returnCodes.Add(code);
                results.Add(result);

                arguments[0] = commands[2].Name;

                code = Engine.EvaluateScript(interpreter, arguments.ToString(),
                    ref result);

                returnCodes.Add(code);
                results.Add(result);
            }
            finally
            {
                if (interpreter != null)
                {
                    ReturnCode removeCode;
                    Result removeError = null;

                    if (tokens[2] != 0)
                    {
                        removeCode = interpreter.RemoveCommand(
                            tokens[2], null, ref removeError);

                        if (removeCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeError);
                        }
                    }

                    if (tokens[1] != 0)
                    {
                        removeCode = interpreter.RemoveCommand(
                            tokens[1], null, ref removeError);

                        if (removeCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeError);
                        }
                    }

                    if (tokens[0] != 0)
                    {
                        removeCode = interpreter.RemoveCommand(
                            tokens[0], null, ref removeError);

                        if (removeCode != ReturnCode.Ok)
                        {
                            DebugOps.Complain(
                                interpreter, removeCode, removeError);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestExecuteStaticDelegates(
            ArgumentList arguments,
            ref ReturnCodeList returnCodes,
            ref ResultList results
            )
        {
            if (returnCodes == null)
                returnCodes = new ReturnCodeList();

            if (results == null)
                results = new ResultList();

            ReturnCode code;
            Result result = null;

            code = Engine.ExecuteDelegate(
                new VoidWithStringCallback(TestVoidMethod),
                arguments, ref result);

            returnCodes.Add(code);
            results.Add(result);

            code = Engine.ExecuteDelegate(
                new LongWithDateTimeCallback(TestLongMethod),
                arguments, ref result);

            returnCodes.Add(code);
            results.Add(result);

            code = Engine.ExecuteDelegate(
                new IEnumerableWithICommandCallback(TestIEnumerableMethod),
                arguments, ref result);

            returnCodes.Add(code);
            results.Add(result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestToIntPtr(
            Interpreter interpreter, /* NOT USED */
            Type type, /* NOT USED */
            string text,
            OptionDictionary options, /* NOT USED */
            CultureInfo cultureInfo,
            IClientData clientData, /* NOT USED */
            ref MarshalFlags marshalFlags, /* NOT USED */
            ref object value,
            ref Result error
            )
        {
            long longValue = 0;

            if (Value.GetWideInteger2(
                    text, ValueFlags.AnyWideInteger, cultureInfo,
                    ref longValue, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // HACK: Maybe truncate 64-bit pointer value to 32-bit.
            //
            if (PlatformOps.Is64BitProcess())
                value = new IntPtr(longValue);
            else
                value = new IntPtr(ConversionOps.ToInt(longValue));

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestIntPtrChangeTypeCallback(
            Interpreter interpreter,
            bool install,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            IScriptBinder scriptBinder = interpreter.Binder as IScriptBinder;

            if (scriptBinder == null)
            {
                error = "invalid script binder";
                return ReturnCode.Error;
            }

            if (install)
            {
                if (scriptBinder.AddChangeTypeCallback(
                        typeof(IntPtr), TestToIntPtr,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (scriptBinder.RemoveChangeTypeCallback(
                        typeof(IntPtr), ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestCallStaticDynamicCallback0(
            Delegate callback,
            params object[] args
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (TestHasOnlyObjectArrayParameter(callback))
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback.DynamicInvoke(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestCallStaticDynamicCallback1(
            DynamicInvokeCallback callback,
            params object[] args
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (staticDynamicInvoke)
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestCallStaticDynamicCallback2(
            TwoArgsDelegate callback,
            string param1,
            string param2
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (staticDynamicInvoke)
                return (int)callback.DynamicInvoke(new object[] { param1, param2 });
            else
                return callback(param1, param2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestCallStaticDynamicCallback3(
            ThreeArgsDelegate callback,
            object[] args,
            int value,
            ref object data
            )
        {
            if (callback == null)
                throw new ArgumentNullException("callback");

            if (staticDynamicInvoke)
            {
                object[] newArgs = new object[] { args, value, data };

                callback.DynamicInvoke(newArgs);
                data = newArgs[newArgs.Length - 1];
            }
            else
            {
                callback(args, value, ref data);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IEnumerable<Delegate> TestGetStaticDynamicCallbacks()
        {
            return new Delegate[] {
                new DynamicInvokeCallback(TestDynamicStaticCallback0),
                new DynamicInvokeCallback(TestDynamicStaticCallback1),
                new TwoArgsDelegate(TestDynamicStaticCallback2),
                new ThreeArgsDelegate(TestDynamicStaticCallback3)
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestDynamicStaticCallback0(
            params object[] args
            )
        {
            return String.Format("static, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestDynamicStaticCallback1(
            object[] args
            )
        {
            return String.Format("static, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestDynamicStaticCallback2(
            string param1,
            string param2
            )
        {
            return String.Compare(param1, param2,
                StringOps.BinaryNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestDynamicStaticCallback3(
            object[] args,
            int value,
            ref object data
            )
        {
            data = String.Format("static, {0}, {1}, {2}",
                TestFormatArgs(args), value, FormatOps.WrapOrNull(data));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestToHexadecimalString(
            byte[] array
            )
        {
            return ArrayOps.ToHexadecimalString(array);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static MethodInfo TestMethodInfo()
        {
            return typeof(Default).GetMethod("TestMethodInfo");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ChangeTypeCallback TestReturnChangeTypeCallback()
        {
            return TestChangeTypeCallback;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ToStringCallback TestReturnToStringCallback()
        {
            return TestToStringCallback;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestChangeTypeCallback(
            Interpreter interpreter,
            Type type,
            string text,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            ref MarshalFlags marshalFlags,
            ref object value,
            ref Result error
            )
        {
            value = text;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestToStringCallback(
            Interpreter interpreter,
            Type type,
            object value,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            ref MarshalFlags marshalFlags,
            ref string text,
            ref Result error
            )
        {
            text = (value != null) ? value.ToString() : String.Empty;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void StaticMethodWithCallback(
            GetTypeCallback callback
            )
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TestIdentity(
            object arg
            )
        {
            return HandleOps.Identity(arg);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestStringArray(
            string [] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestArray(
            int[] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestNullArray(
            int[] array
            )
        {
            if (array == null)
                return -1;

            int count = 0;

            foreach (int element in array)
                count += element;

            return count;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestObjectAsArray(
            object input,
            ref object output
            )
        {
            output = new object[] { input, output };
            return (input != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefArray(
            bool create,
            ref int[] array
            )
        {
            if (create)
                array = new int[] { 1, 2, 3 };

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefArray(
            bool create,
            ref string[] array
            )
        {
            if (create)
                array = new string[] { "one", "two", "three" };

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefArray(
            Interpreter interpreter,
            VariableFlags variableFlags,
            string varName,
            string varIndex,
            object varValue,
            bool create,
            ref string[] array
            )
        {
            if ((interpreter != null) && (varName != null))
            {
                ReturnCode code;
                IVariable variable = null; /* NOT USED */
                Result error = null;

                code = interpreter.SetVariableValue2(
                    variableFlags, null, varName, varIndex,
                    varValue, null, ref variable, ref error);

                if (code != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, code, error);
            }

            return TestByRefArray(create, ref array);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestOutArray(
            bool create,
            out int[] array
            )
        {
            if (create)
                array = new int[] { 1, 2, 3 };
            else
                array = null;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestOutArray(
            bool create,
            out string[] array
            )
        {
            if (create)
                array = new string[] { "one", "two", "three" };
            else
                array = null;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestMulti2Array(
            int[,] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestMulti3Array(
            int[, ,] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestNestedArray(
            int[][] array
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static sbyte TestSByte(
            byte X
            )
        {
            return ConversionOps.ToSByte(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int TestInt(
            uint X
            )
        {
            return ConversionOps.ToInt(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long TestLong(
            ulong X
            )
        {
            return ConversionOps.ToLong(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ulong TestULong(
            long X
            )
        {
            return ConversionOps.ToULong(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestIntParams(
            params int[] args
            )
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestEnum(
            ReturnCode x
            )
        {
            return (x == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefEnum(
            ref ReturnCode x
            )
        {
            if (x == ReturnCode.Error)
                x = ReturnCode.Break;

            return (x == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestNullableEnum(
            ReturnCode? x
            )
        {
            return (x != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestByRefNullableEnum(
            ref ReturnCode? x
            )
        {
            if ((x != null) && (x == ReturnCode.Error))
                x = ReturnCode.Break;

            return (x != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ByteList TestByteList()
        {
            return new ByteList(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntList TestIntList()
        {
            return new IntList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static LongList TestLongList()
        {
            return new LongList(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DerivedList TestDerivedList()
        {
            return new DerivedList();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringDictionary TestStringDictionary(
            bool enumerable,
            bool keys,
            bool values,
            params string[] args
            )
        {
            if (enumerable)
                return new StringDictionary((IEnumerable<string>)args, keys, values);
            else
                return new StringDictionary(new StringList(args), keys, values);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringPairList TestStringPairList(
            Interpreter interpreter
            )
        {
            Result error = null;

            return AttributeOps.GetObjectIds(
                (interpreter != null) ? interpreter.GetAppDomain() : null, true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringPairList[] TestArrayOfStringPairList()
        {
            return new StringPairList[] {
                new StringPairList(
                    new StringPair(),
                    new StringPair("this is a test #1."),
                    new StringPair("1", "2"),
                    new StringPair("3", "4"),
                    new StringPair("5", "6"),
                    new StringPair("7", "8"),
                    new StringPair("9", "10")),
                new StringPairList(
                    new StringPair(),
                    new StringPair("this is a test #2."),
                    new StringPair("11", "12"),
                    new StringPair("13", "14"),
                    new StringPair("15", "16"),
                    new StringPair("17", "18"),
                    new StringPair("19", "20"))
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Guid TestObjectId(
            Interpreter interpreter
            )
        {
            // ICommand command = null;
            // Result error = null;

            // interpreter.GetCommand("apply", false, true, ref command, ref error);
            // interpreter.GetCommand("apply", true, true, ref command, ref error);

            Guid id = AttributeOps.GetObjectId(typeof(ObjectIdAttribute));

            IInteractiveHost interactiveHost =
                (interpreter != null) ? interpreter.Host : null;

            if (interactiveHost != null)
                interactiveHost.Write(id.ToString());

            return id;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReplaceTraceListener(
            TraceListenerCollection listeners,
            TraceListener oldListener,
            TraceListener newListener,
            bool typeOnly,
            bool dispose,
            ref Result error
            )
        {
            return DebugOps.ReplaceTraceListener(
                listeners, oldListener, newListener, typeOnly,
                dispose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestSetDateTimeNowCallback(
            Interpreter interpreter,
            DateTime dateTime,
            long increment,
            bool enable
            )
        {
            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (eventManager == null)
                return false;

            if (enable)
            {
                now = dateTime;
                nowCallback = eventManager.NowCallback; /* save */
                nowIncrement = increment;
                eventManager.NowCallback = TestDateTimeNow; /* set */
            }
            else
            {
                now = DateTime.MinValue;
                eventManager.NowCallback = nowCallback; /* restore */
                nowCallback = null; /* clear */
                nowIncrement = 0;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList TestStringListFromString(
            string value
            )
        {
            return StringList.FromString(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool TestPeInformation(
            string fileName,
            ref uint timeStamp,
            ref UIntPtr reserve,
            ref UIntPtr commit
            )
        {
            return FileOps.GetPeFileTimeStamp(fileName, ref timeStamp) &&
                FileOps.GetPeFileStackReserveAndCommit(fileName, ref reserve, ref commit);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestSetPluginIsolation(
            Interpreter interpreter,
            bool enable
            )
        {
            ReturnCode code;
            Result result = null;

            code = TestSetPluginIsolation(interpreter, enable, ref result);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSetPluginIsolation(
            Interpreter interpreter,
            bool enable,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

#if ISOLATED_PLUGINS
            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
            {
                ResultList results = new ResultList();
                PluginFlags pluginFlags = interpreter.PluginFlags;

                results.Add(String.Format(
                    "plugin isolation was {0}", FlagOps.HasFlags(
                    pluginFlags, PluginFlags.Isolated, true) ?
                    "enabled" : "disabled"));

                if (enable)
                    pluginFlags |= PluginFlags.Isolated;
                else
                    pluginFlags &= ~PluginFlags.Isolated;

                interpreter.PluginFlags = pluginFlags;

                results.Add(String.Format(
                    "plugin isolation is {0}", FlagOps.HasFlags(
                    pluginFlags, PluginFlags.Isolated, true) ?
                    "enabled" : "disabled"));

                result = results;
                return ReturnCode.Ok;
            }
#else
            result = "plugin isolation is not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TestResource(
            Interpreter interpreter
            )
        {
            return ResourceOps.GetString(
                interpreter, ResourceId.Test, 1, TimeOps.GetUtcNow(), "test");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void TestScriptStream(
            Interpreter interpreter,
            string name,
            string value,
            int length,
            ref Result result
            )
        {
            if (interpreter == null)
                return;

#if false
            IInteractiveHost interactiveHost = interpreter.Host;
#endif

            if (value == null) /* NOTE: Default? */
            {
                if (name == null) name = "TestScriptStream";

                value = "set x 1" + Characters.DosNewLine +
                    Characters.EndOfFile + "abc";
            }

            ReturnCode[] code = { ReturnCode.Ok, ReturnCode.Ok };
            Result[] localResult = { null, null };
            string[] extra = { null, null };

            using (StringReader stringReader = new StringReader(value))
            {
                EngineFlags engineFlags = EngineFlags.ForceSoftEof;
                string text = null;

                code[0] = Engine.ReadScriptStream(
                    interpreter, name, stringReader, 0,
                    length, ref engineFlags, ref text,
                    ref localResult[0]);

                if (code[0] == ReturnCode.Ok)
                    localResult[0] = text;

                extra[0] = stringReader.ReadToEnd();
            }

#if false
            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[0], localResult[0]);

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[0], extra[0]);

            using (StringReader stringReader = new StringReader(value))
            {
                EngineFlags engineFlags = EngineFlags.ForceSoftEof;
                string text = null;

                code[1] = Engine.ReadScriptStream(
                    interpreter, name, stringReader, 0,
                    Count.Invalid, ref engineFlags, ref text,
                    ref localResult[1]);

                if (code[1] == ReturnCode.Ok)
                    localResult[1] = text;

                extra[1] = stringReader.ReadToEnd();
            }

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[1], localResult[1]);

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code[1], extra[1]);
#endif

            result = StringList.MakeList(code[0], code[1], localResult[0],
                localResult[1], extra[0], extra[1]);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadScriptFile(
            Interpreter interpreter,                 /* in */
            Encoding encoding,                       /* in */
            string fileName,                         /* in */
            EngineFlags engineFlags,                 /* in */
            ref IClientData clientData,              /* out */
            ref Result error                         /* out */
            )
        {
            SubstitutionFlags substitutionFlags = SubstitutionFlags.Default;
            EventFlags eventFlags = EventFlags.Default;
            ExpressionFlags expressionFlags = ExpressionFlags.Default;

            if (interpreter != null)
            {
                engineFlags |= interpreter.EngineFlags;
                substitutionFlags = interpreter.SubstitutionFlags;
                eventFlags = interpreter.EngineEventFlags;
                expressionFlags = interpreter.ExpressionFlags;
            }

            return TestReadScriptFile(
                interpreter, encoding, fileName, ref engineFlags,
                ref substitutionFlags, ref eventFlags, ref expressionFlags,
                ref clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadScriptFile(
            Interpreter interpreter,                 /* in */
            Encoding encoding,                       /* in */
            string fileName,                         /* in */
            ref EngineFlags engineFlags,             /* in, out */
            ref SubstitutionFlags substitutionFlags, /* in, out */
            ref EventFlags eventFlags,               /* in, out */
            ref ExpressionFlags expressionFlags,     /* in, out */
            ref IClientData clientData,              /* out */
            ref Result error                         /* out */
            )
        {
            ReadScriptClientData readScriptClientData = null;

            if (Engine.ReadScriptFile(
                    interpreter, encoding, fileName, ref engineFlags,
                    ref substitutionFlags, ref eventFlags,
                    ref expressionFlags, ref readScriptClientData,
                    ref error) == ReturnCode.Ok)
            {
                clientData = readScriptClientData;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestReadPostScriptBytes(
            Interpreter interpreter, /* NOT USED */
            Stream stream,
            long streamLength,
            bool seekSoftEof,
            ref ByteList bytes,
            ref Result error
            )
        {
            try
            {
                Engine.ReadPostScriptBytes(
                    stream.ReadByte, stream.Read, streamLength,
                    seekSoftEof, ref bytes);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestSubstituteFile(
            Interpreter interpreter,
            string fileName,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref Result result
            )
        {
            ReturnCode code = Engine.SubstituteFile(
                interpreter, fileName, engineFlags,
                substitutionFlags, eventFlags, expressionFlags,
                ref result);

            IInteractiveHost interactiveHost = interpreter.Host;

            if (interactiveHost != null)
                interactiveHost.WriteResultLine(code, result);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestMatchComparer(
            MatchMode mode,
            bool noCase,
            RegexOptions regExOptions,
            StringList list,
            string value,
            ref bool match,
            ref Result error
            )
        {
            if (list != null)
            {
                if (value != null)
                {
                    try
                    {
                        PathDictionary<int> paths = new PathDictionary<int>(
                            new _Comparers.Match(mode, noCase, regExOptions));

                        paths.Add(list);
                        match = paths.ContainsKey(value);

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid value";
                }
            }
            else
            {
                error = "invalid list";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestExpr( /* LOCAL-ONLY */
            Interpreter interpreter,
            string text,
            ref bool usable,
            ref bool exception,
            ref Result result,
            out Result result2
            )
        {
            ReturnCode code;

            IParseState parseState = new ParseState(
                interpreter.EngineFlags, interpreter.SubstitutionFlags);

            result2 = null;

            code = ExpressionParser.ParseExpression(
                interpreter, text, 0,
                (text != null) ? text.Length : 0,
                parseState, true, ref result2);

            if (code != ReturnCode.Ok)
            {
                result.Value = result2;
                return code;
            }

            Argument value = null;
            Result error = null;

            code = ExpressionEvaluator.EvaluateSubExpression(
                interpreter, parseState, 0, interpreter.EngineFlags,
                interpreter.SubstitutionFlags, interpreter.EngineEventFlags,
                interpreter.ExpressionFlags,
#if RESULT_LIMITS
                interpreter.ExecuteResultLimit,
                interpreter.NestedResultLimit,
#endif
                true, ref usable, ref exception, ref value, ref error);

            if (code == ReturnCode.Ok)
            {
                result.Value = value;
                result2 = value;
            }
            else
            {
                result.Value = error;
                result2 = error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList TestParseInteger(
            string text,
            int startIndex,
            int characters,
            byte radix,
            bool whiteSpace,
            bool greedy,
            bool unsigned,
            bool legacyOctal
            )
        {
            int endIndex = 0;

            int intValue = Parser.ParseInteger(
                text, startIndex, characters, radix,
                whiteSpace, greedy, unsigned, legacyOctal,
                ref endIndex);

            return new StringList(intValue.ToString(), endIndex.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ResultList TestAddFunction(
            Interpreter interpreter
            )
        {
            ResultList results = new ResultList();
            long token = 0;

            ReturnCode code;
            Result result = null;

            code = interpreter.AddFunction(
                typeof(_Tests.Default), "foo", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.AddFunction(
                typeof(_Functions.Min), "bar", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.AddFunction(
                typeof(_Functions.Min), "eq", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.AddFunction(
                typeof(_Functions.Min), "eqq", (int)Arity.None, null,
                FunctionFlags.ForTestUse, null, null, true, ref token,
                ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            return results;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddNamedFunction(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref Result result
            )
        {
            long token = 0;

            return interpreter.AddFunction(
                typeof(_Tests.Default.Function), name, (int)Arity.None,
                null, FunctionFlags.ForTestUse, null, clientData, true,
                ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddNamedFunction2(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref Result result
            )
        {
            long token = 0;

            return interpreter.AddFunction(
                typeof(_Tests.Default.Function2), name, (int)Arity.Automatic,
                null, FunctionFlags.ForTestUse, null, clientData, true,
                ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestAddNamedFunction3(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            ref Result result
            )
        {
            long token = 0;

            return interpreter.AddFunction(
                typeof(_Tests.Default.Function3), name, (int)Arity.Automatic,
                null, FunctionFlags.ForTestUse, null, clientData, true,
                ref token, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ResultList TestRemoveFunction(
            Interpreter interpreter
            )
        {
            ResultList results = new ResultList();

            ReturnCode code;
            Result result = null;

            code = interpreter.RemoveFunction("foo", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.RemoveFunction("bar", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.RemoveFunction("eq", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            code = interpreter.RemoveFunction("eqq", null, ref result);

            if (code == ReturnCode.Ok)
                results.Add(code);
            else
                results.Add(result);

            return results;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRemoveNamedFunction(
            Interpreter interpreter,
            string name,
            IClientData clientData,
            Result result
            )
        {
            return interpreter.RemoveFunction(name, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestWriteBox(
            Interpreter interpreter,
            string value,
            bool multiple,
            bool newLine,
            bool restore,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                IDisplayHost displayHost = interpreter.Host;

                if (displayHost != null)
                {
                    try
                    {
                        bool positioning = FlagOps.HasFlags(
                            displayHost.GetHostFlags(), HostFlags.Positioning,
                            true);

                        int left = 0;
                        int top = 0;

                        if (!positioning ||
                            displayHost.GetPosition(ref left, ref top))
                        {
                            ConsoleColor foregroundColor = _ConsoleColor.None;
                            ConsoleColor backgroundColor = _ConsoleColor.None;

                            if (displayHost.GetColors(
                                    null, "TestInfo", true, true, ref foregroundColor,
                                    ref backgroundColor, ref error) == ReturnCode.Ok)
                            {
                                if (multiple)
                                {
                                    StringList list = null;

                                    if (Parser.SplitList(
                                            interpreter, value, 0, Length.Invalid, true,
                                            ref list, ref error) == ReturnCode.Ok)
                                    {
                                        if (displayHost.WriteBox(
                                                null, new StringPairList(list), null,
                                                newLine, restore, ref left, ref top,
                                                foregroundColor, backgroundColor))
                                        {
                                            return true;
                                        }
                                        else
                                        {
                                            error = "could not write box to interpreter host";
                                        }
                                    }
                                }
                                else
                                {
                                    if (displayHost.WriteBox(
                                            null, value, null, newLine, restore,
                                            ref left, ref top, foregroundColor,
                                            backgroundColor))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        error = "could not write box to interpreter host";
                                    }
                                }
                            }
                        }
                        else
                        {
                            error = "could not get interpreter host position";
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "interpreter host not available";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool EnableWriteCustomInfo(
            Interpreter interpreter,
            bool enable,
            StringPairList list
            )
        {
            if (interpreter == null)
                return false;

#if CONSOLE
            _Hosts.Console consoleHost = interpreter.Host as _Hosts.Console;

            if (consoleHost == null)
                return false;

            consoleHost.EnableTests(enable);
#endif

            if (enable)
            {
                if (list != null)
                {
                    customInfoList = new StringPairList(list);
                }
                else
                {
                    customInfoList = new StringPairList(
                        new StringPair("Custom"), null,
                        new StringPair("name0", null),
                        new StringPair("name1", String.Empty),
                        new StringPair("name2", "value1"),
                        new StringPair("name3", TimeOps.GetUtcNow().ToString()),
                        new StringPair("name4", (interpreter.Random != null) ?
                            interpreter.Random.Next().ToString() : "0"));
                }
            }
            else
            {
                customInfoList = null;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestWriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            if (interpreter == null)
                return false;

            _Hosts.Default defaultHost = interpreter.Host as _Hosts.Default;

            if (defaultHost == null)
                return false;

            int hostLeft = 0;
            int hostTop = 0;

            if (!defaultHost.GetDefaultPosition(ref hostLeft, ref hostTop))
                return false;

            try
            {
                OutputStyle outputStyle = defaultHost.OutputStyle;
                StringPairList list = new StringPairList(customInfoList);

                if (defaultHost.IsBoxedOutputStyle(outputStyle))
                {
                    return defaultHost.WriteBox(
                        TestCustomInfoBoxName, list, null, false, true,
                        ref hostLeft, ref hostTop, foregroundColor,
                        backgroundColor);
                }
                else if (defaultHost.IsFormattedOutputStyle(outputStyle))
                {
                    return defaultHost.WriteFormat(
                        list, newLine, foregroundColor, backgroundColor);
                }
                else if (defaultHost.IsNoneOutputStyle(outputStyle))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                defaultHost.SetDefaultPosition(hostLeft, hostTop);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Requires C# 4.0
#if NET_40
        public ReturnCode TestOptionalParameter0(
            ref Result result,
            string one = null
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter0_1", one);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter0(
            ref Result result,
            string one = null,
            int two = 0
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter0_2", one, two);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter1(
            ref Result result,
            string one,
            string two = null
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter1_1", one, two);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter1(
            ref Result result,
            string one,
            string two = null,
            int three = 0
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter1_2", one, two, three);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter2(
            ref Result result,
            string one,
            string two = "two1"
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter2_1", one, two);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter2(
            ref Result result,
            string one,
            string two = "two2",
            int three = int.MaxValue
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameter2_2", one, two, three);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameter2(
            ref Result result,
            string one,
            string two = "two3",
            int three = int.MaxValue - 1,
            params string[] more
            )
        {
            CheckDisposed();

            StringList list = new StringList(
                "TestOptionalParameter2_3", one, two,
                three.ToString());

            if (more != null)
                foreach (string item in more)
                    list.Add(item);

            result = list;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestOptionalParameterZ(
            ref Result result,
            Guid guid = default(Guid)
            )
        {
            CheckDisposed();

            result = StringList.MakeList(
                "TestOptionalParameterZ", guid);

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        public void TestSetScriptComplainCallback(
            Interpreter interpreter,
            string commandName,
            bool setup,
            bool withThrow
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            complainCommandName = commandName;
            complainWithThrow = withThrow;

            Interpreter.ComplainCallback = setup ?
                (ComplainCallback)TestScriptComplainCallback : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewInterpreterCallback
        public void TestSetNewInterpreterCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            callbackInterpreter = interpreter;
            newInterpreterText = text;

            Interpreter.NewInterpreterCallback = setup ?
                (EventCallback)TestNewInterpreterCallback : null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for PackageCallback
        public void TestSetPackageFallbackCallback(
            Interpreter interpreter,
            string text,
            bool setup
            )
        {
            CheckDisposed();

            if (interpreter == null)
                return;

            packageFallbackText = text;

            if (setup)
                interpreter.PackageFallback = TestPackageFallbackCallback;
            else
                interpreter.PackageFallback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            if (uniqueToString)
                return GlobalState.NextId().ToString();

            return base.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestObjectIdentity(
            object value
            )
        {
            CheckDisposed();

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default TestReturnOfSelf(
            bool useNull
            )
        {
            CheckDisposed();

            return useNull ? null : this;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void MethodWithCallback(
            GetTypeCallback callback
            )
        {
            CheckDisposed();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int[] TestIntArrayReturnValue()
        {
            CheckDisposed();

            return new int[] { 1, 2, 3, 4, 5 };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string[] TestStringArrayReturnValue()
        {
            CheckDisposed();

            return new string[] { "1", "2", "joe", "jim", "tom" };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringList TestStringListReturnValue()
        {
            CheckDisposed();

            return new StringList(TestStringArrayReturnValue());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IList<string> TestStringIListReturnValue(
            bool useCustom,
            params string[] strings
            )
        {
            CheckDisposed();

            return useCustom ?
                new StringList(strings) : new List<string>(strings);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IList<IList<IList<string>>> TestStringIListIListIListReturnValue(
            bool useCustom,
            params string[] strings
            )
        {
            CheckDisposed();

            if (useCustom)
            {
                IList<string> list = new StringList(strings);

                IList<IList<string>> list2 = (IList<IList<string>>)
                    new GenericList<IList<string>>(list, list, list);

                IList<IList<IList<string>>> list3 = (IList<IList<IList<string>>>)
                    new GenericList<IList<IList<string>>>(list2, list2, list2);

                return list3;
            }
            else
            {
                IList<string> list = new List<string>(strings);

                IList<IList<string>> list2 = (IList<IList<string>>)
                    new List<IList<string>>(new IList<string>[] { list, list, list });

                IList<IList<IList<string>>> list3 = (IList<IList<IList<string>>>)
                    new List<IList<IList<string>>>(new IList<IList<string>>[] { list2, list2, list2 });

                return list3;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringList[] TestStringListArrayReturnValue()
        {
            CheckDisposed();

            return new StringList[] {
                TestStringListReturnValue(), new StringList("hello world"),
                new StringList(";"), new StringList("\\"), new StringList("{")
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IDictionary<string, string> TestStringIDictionaryReturnValue(
            bool useCustom,
            params string[] strings
            )
        {
            CheckDisposed();

            if (useCustom)
            {
                IList<string> list = new StringList(strings);

                return new GenericDictionary<string, string>(list);
            }
            else
            {
                IList<string> list = new List<string>(strings);

                return new Dictionary<string, string>(
                    new GenericDictionary<string, string>(list));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefByteArray(
            int size,
            ref byte[] byteArray
            )
        {
            CheckDisposed();

            byteArray = new byte[size];

            for (int index = 0; index < size; index++)
                byteArray[index] = (byte)(index & byte.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestTwoByteArrays(
            Interpreter interpreter,
            bool randomize,
            byte[] inByteArray,
            ref byte[] outByteArray,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (inByteArray == null)
            {
                error = "invalid input byte array";
                return ReturnCode.Error;
            }

            if (randomize)
            {
                byte[] randomByteArray = new byte[inByteArray.Length];
                Random random = interpreter.Random;

                if (random != null)
                    random.NextBytes(randomByteArray);

                outByteArray = new byte[inByteArray.Length];

                for (int index = 0; index < outByteArray.Length; index++)
                {
                    outByteArray[index] = ConversionOps.ToByte(
                        inByteArray[index] ^ randomByteArray[index]);
                }
            }
            else
            {
                outByteArray = new byte[inByteArray.Length];

                for (int index = 0; index < outByteArray.Length; index++)
                    outByteArray[index] = inByteArray[index];
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public byte[] TestReturnByteArray(
            byte[] array
            )
        {
            CheckDisposed();

            //
            // WARNING: DO NOT REMOVE.  This is used by the unit tests to
            //          convert a Tcl array into a Tcl list via the Eagle
            //          marshaller.
            //
            return array;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefStringListArray(
            ref StringList[] list
            )
        {
            CheckDisposed();

            list = TestStringListArrayReturnValue();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default TestComplexMethod(
            sbyte w,
            int x,
            bool y,
            double z,
            ref int[] t1,
            ref int[,] t2,
            ref string[] t3,
            out string t4,
            out string[] t5
            )
        {
            CheckDisposed();

            int[][] t6 = null;

            return TestComplexMethod(
                w, x, y, z, ref t1, ref t2, ref t3, out t4, out t5, out t6);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefNullableValueTypeMethod(
            ref int? x
            )
        {
            CheckDisposed();

            if (x != null) x++;
            else x = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestNullableValueTypeMethod(
            int? x
            )
        {
            CheckDisposed();

            return x;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefValueTypeMethod(
            ref int x
            )
        {
            CheckDisposed();

            x++;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int TestRanges(
            bool a,
            sbyte b,
            byte c,
            char d,
            short e,
            ushort f,
            int g,
            uint h,
            long i,
            ulong j,
            decimal k,
            float l,
            double m
            )
        {
            CheckDisposed();

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public char TestCharacterMethod(
            char x
            )
        {
            CheckDisposed();

            return x;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestByRefCharacterArrayMethod(
            ref char[] x
            )
        {
            CheckDisposed();

            x = new char[] { 'f', 'o', 'o' };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string TestGetPrivateField()
        {
            CheckDisposed();

            return privateField;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Default TestComplexMethod(
            sbyte w,
            int x,
            bool y,
            double z,
            ref int[] t1,
            ref int[,] t2,
            ref string[] t3,
            out string t4,
            out string[] t5,
            out int[][] t6
            )
        {
            CheckDisposed();

            if (y)
                t1[0]++;

            if (z > 1)
            {
#if !MONO_BUILD
                t2[-1, -2] += 20;
#endif

                t2[0, 0]++;
                t2[0, 1] *= 2;
                t2[1, 0]--;
                t2[1, 1] /= 2;
                t2[2, 1] += 21;
            }

            if (x > 0)
                //
                // BUGFIX: We do not want to complicate the test case to account
                //         for negative numbers here; therefore, just use the
                //         absolute value.
                //
                t3[0] = Math.Abs(Environment.TickCount).ToString();

            //
            // BUGFIX: Cannot be locale-specific here.
            //
            t4 = FormatOps.Iso8601DateTime(TimeOps.GetUtcNow(), true);

            t5 = new string[] {
                w.ToString(), x.ToString(), y.ToString(), z.ToString()
            };

            t6 = new int[][] {
                new int[] { 0, 1, 2 }, new int[] { 2, 4, 6 }, new int[] { 8, 16, 32 }
            };

            return new Default(w);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !MONO_BUILD
        public int[,] TestMoreComplexMethod(
            bool y,
            ref int[,] t7
            )
        {
            CheckDisposed();

            if (y)
            {
                t7 = (int[,])Array.CreateInstance(
                    typeof(int), new int[] { 4, 5 }, new int[] { -6, 6 });
            }
            else
            {
                t7[-6, 6] += -6 * 6;
                t7[-6, 7] += -6 * 7;
                t7[-6, 8] += -6 * 8;
                t7[-6, 9] += -6 * 9;
                t7[-6, 10] += -6 * 10;

                t7[-5, 6] += -5 * 6;
                t7[-5, 7] += -5 * 7;
                t7[-5, 8] += -5 * 8;
                t7[-5, 9] += -5 * 9;
                t7[-5, 10] += -5 * 10;

                t7[-4, 6] += -4 * 6;
                t7[-4, 7] += -4 * 7;
                t7[-4, 8] += -4 * 8;
                t7[-4, 9] += -4 * 9;
                t7[-4, 10] += -4 * 10;

                t7[-3, 6] += -3 * 6;
                t7[-3, 7] += -3 * 7;
                t7[-3, 8] += -3 * 8;
                t7[-3, 9] += -3 * 9;
                t7[-3, 10] += -3 * 10;
            }

            return t7;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestSetupIntArray(
            bool reverse
            )
        {
            CheckDisposed();

            if (intArrayField == null)
                throw new InvalidOperationException();

            int length = intArrayField.Length;

            for (int index = 0; index < length; index++)
            {
                intArrayField[index] = reverse ?
                    ((length - 1) - index) : index;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestSetVariableEnumerable(
            Interpreter interpreter,
            string name,
            bool autoReset,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;
                ReturnCode setCode;
                Result setError = null;

                setCode = interpreter.SetVariableEnumerable(
                    VariableFlags.None, name, intArrayField, autoReset,
                    ref setError);

                if (setCode != ReturnCode.Ok)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(setError);

                    if (!quiet)
                        DebugOps.Complain(interpreter, setCode, setError);
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestUnsetVariableEnumerable(
            Interpreter interpreter,
            string name,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;
                ReturnCode unsetCode;
                Result unsetError = null;

                unsetCode = interpreter.UnsetVariable(
                    VariableFlags.None, name, ref unsetError);

                if (unsetCode != ReturnCode.Ok)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(unsetError);

                    if (!quiet)
                        DebugOps.Complain(interpreter, unsetCode, unsetError);
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestSetVariableLinks(
            Interpreter interpreter,
            string stringName,
            string objectName,
            string integerName,
            string propertyName,
            bool useString,
            bool useObject,
            bool useInteger,
            bool useProperty,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;

                if (useString)
                {
                    FieldInfo fieldInfo = null;

                    try
                    {
                        fieldInfo = GetType().GetField("privateField",
                            MarshalOps.PrivateInstanceGetFieldBindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (fieldInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, stringName, fieldInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (useObject)
                {
                    FieldInfo fieldInfo = null;

                    try
                    {
                        fieldInfo = GetType().GetField("objectField",
                            MarshalOps.PrivateInstanceGetFieldBindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (fieldInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, objectName, fieldInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (useInteger)
                {
                    FieldInfo fieldInfo = null;

                    try
                    {
                        fieldInfo = GetType().GetField("intField",
                            MarshalOps.PublicInstanceGetFieldBindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (fieldInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, integerName, fieldInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (useProperty)
                {
                    PropertyInfo propertyInfo = null;

                    try
                    {
                        propertyInfo = GetType().GetProperty("SimpleIntProperty",
                            MarshalOps.PublicInstanceGetPropertyBindingFlags);
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);

                        if (!quiet)
                            DebugOps.Complain(interpreter, ReturnCode.Error, e);
                    }

                    if (propertyInfo != null)
                    {
                        ReturnCode setCode;
                        Result setError = null;

                        setCode = interpreter.SetVariableLink(
                            VariableFlags.None, propertyName, propertyInfo,
                            this, ref setError);

                        if (setCode != ReturnCode.Ok)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(setError);

                            if (!quiet)
                                DebugOps.Complain(interpreter, setCode, setError);
                        }
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestUnsetVariableLinks(
            Interpreter interpreter,
            string stringName,
            string objectName,
            string integerName,
            string propertyName,
            bool useString,
            bool useObject,
            bool useInteger,
            bool useProperty,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;

                if (useString)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, stringName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useObject)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, objectName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useInteger)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, integerName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useProperty)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, propertyName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestSetVariableSystemArray(
            Interpreter interpreter,
            string intPtrName,
            string objectName,
            bool useIntPtr,
            bool useObject,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;

                if (useIntPtr)
                {
                    ReturnCode setCode;
                    Result setError = null;

                    setCode = interpreter.SetVariableSystemArray(
                        VariableFlags.None, intPtrName, intPtrArrayField,
                        ref error);

                    if (setCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(setError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, setCode, setError);
                    }
                }

                if (useObject)
                {
                    ReturnCode setCode;
                    Result setError = null;

                    setCode = interpreter.SetVariableSystemArray(
                        VariableFlags.None, objectName, objectArrayField,
                        ref error);

                    if (setCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(setError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, setCode, setError);
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestUnsetVariableSystemArray(
            Interpreter interpreter,
            string intPtrName,
            string objectName,
            bool useIntPtr,
            bool useObject,
            bool quiet,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                ResultList errors = null;

                if (useIntPtr)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, intPtrName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (useObject)
                {
                    ReturnCode unsetCode;
                    Result unsetError = null;

                    unsetCode = interpreter.UnsetVariable(
                        VariableFlags.None, objectName, ref unsetError);

                    if (unsetCode != ReturnCode.Ok)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(unsetError);

                        if (!quiet)
                            DebugOps.Complain(interpreter, unsetCode, unsetError);
                    }
                }

                if (errors == null)
                    return ReturnCode.Ok;

                error = errors;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestTakeEventHandler(
            EventHandler eventHandler,
            object sender,
            EventArgs e
            )
        {
            CheckDisposed();

            if (eventHandler != null)
                eventHandler(sender, e);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestTakeGenericEventHandler<TEventArgs>(
            EventHandler<TEventArgs> eventHandler,
            object sender,
            TEventArgs e
            ) where TEventArgs : EventArgs
        {
            CheckDisposed();

            if (eventHandler != null)
                eventHandler(sender, e);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestTakeResolveEventHandler(
            EventHandler<ResolveEventArgs> eventHandler,
            object sender,
            ResolveEventArgs e
            )
        {
            CheckDisposed();

            if (eventHandler != null)
                eventHandler(sender, e);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestEvaluateAsyncCallback(IAsynchronousContext context)
        {
            CheckDisposed();

            if (context != null)
                //
                // NOTE: Capture async result.
                //
                asyncResult = ResultOps.Format(
                    context.ReturnCode, context.Result, context.ErrorLine);

            if (@event != null)
                ThreadOps.SetEvent(@event);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestEvaluateAsync(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            int timeout,
            ref Result result
            )
        {
            CheckDisposed();

            int preDisposeContextCount = 0;
            int postDisposeContextCount = 0;

            return TestEvaluateAsync(
                interpreter, text, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, timeout, ref preDisposeContextCount,
                ref postDisposeContextCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public ReturnCode TestEvaluateAsync(
            Interpreter interpreter,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            int timeout,
            ref int preDisposeContextCount,
            ref int postDisposeContextCount,
            ref Result result
            )
        {
            CheckDisposed();

            if (interpreter != null)
            {
                if (Engine.EvaluateScript(
                        interpreter, text, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, TestEvaluateAsyncCallback,
                        null, ref result) == ReturnCode.Ok)
                {
                    if (@event != null)
                    {
                        if (timeout == _Timeout.Infinite)
                            timeout = interpreter.Timeout;

                        if (ThreadOps.WaitEvent(@event, timeout))
                        {
#if THREADING
                            //
                            // NOTE: Return the context counts that should
                            //       have been last updated when the context
                            //       manager was purged from the thread-pool
                            //       thread.
                            //
                            preDisposeContextCount =
                                interpreter.InternalPreDisposeContextCount;

                            postDisposeContextCount =
                                interpreter.InternalPostDisposeContextCount;
#endif

                            //
                            // NOTE: Return async result.
                            //
                            result = asyncResult;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            result = String.Format(
                                "waited {0} milliseconds for script to complete",
                                timeout);
                        }

                        //
                        // NOTE: Reset event for next time.
                        //
                        ThreadOps.ResetEvent(@event);
                    }
                    else
                    {
                        //
                        // NOTE: No event is setup, skip waiting.
                        //
                        return ReturnCode.Ok;
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IAnyPair<int, long> TestPermute(
            IList<string> list,
            ListTransformCallback callback
            )
        {
            CheckDisposed();

            if (callback == null)
                callback = TestListTransformCallback;

            intField = 0;
            longField = 0;

            ListOps.Permute(list, callback);

            return new AnyPair<int, long>(intField, longField);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestCallDynamicCallback0(
            Delegate callback,
            params object[] args
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (TestHasOnlyObjectArrayParameter(callback))
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback.DynamicInvoke(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestCallDynamicCallback1(
            DynamicInvokeCallback callback,
            params object[] args
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (dynamicInvoke)
                return callback.DynamicInvoke(new object[] { args });
            else
                return callback(args);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int TestCallDynamicCallback2(
            TwoArgsDelegate callback,
            string param1,
            string param2
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (dynamicInvoke)
                return (int)callback.DynamicInvoke(new object[] { param1, param2 });
            else
                return callback(param1, param2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestCallDynamicCallback3(
            ThreeArgsDelegate callback,
            object[] args,
            int value,
            ref object data
            )
        {
            CheckDisposed();

            if (callback == null)
                throw new ArgumentNullException("callback");

            if (dynamicInvoke)
            {
                object[] newArgs = new object[] { args, value, data };

                callback.DynamicInvoke(newArgs);
                data = newArgs[newArgs.Length - 1];
            }
            else
            {
                callback(args, value, ref data);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IEnumerable<Delegate> TestGetDynamicCallbacks()
        {
            CheckDisposed();

            return new Delegate[] {
                new DynamicInvokeCallback(TestDynamicCallback0),
                new DynamicInvokeCallback(TestDynamicCallback1),
                new TwoArgsDelegate(TestDynamicCallback2),
                new ThreeArgsDelegate(TestDynamicCallback3)
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestDynamicCallback0(
            params object[] args
            )
        {
            CheckDisposed();

            return String.Format("instance, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object TestDynamicCallback1(
            object[] args
            )
        {
            CheckDisposed();

            return String.Format("instance, {0}", TestFormatArgs(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int TestDynamicCallback2(
            string param1,
            string param2
            )
        {
            CheckDisposed();

            return String.Compare(param1, param2,
                StringOps.BinaryNoCaseComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void TestDynamicCallback3(
            object[] args,  /* in */
            int value,      /* in */
            ref object data /* in, out */
            )
        {
            CheckDisposed();

            data = String.Format("instance, {0}, {1}, {2}",
                TestFormatArgs(args), value, FormatOps.WrapOrNull(data));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool TestListTransformCallback(
            IList<string> list
            )
        {
            // CheckDisposed();

            if (list == null)
                return true;

            string value = list.ToString();

            longField ^= value.GetHashCode();
            intField++;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        private void TestScriptComplainCallback(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            // CheckDisposed();

            if ((callbackInterpreter != null) &&
                !String.IsNullOrEmpty(complainCommandName))
            {
                StringList list = new StringList(complainCommandName,
                    FormatOps.Complaint(id, code, result, stackTrace));

                complainCode = callbackInterpreter.EvaluateScript(
                    list.ToString(), ref complainResult,
                    ref complainErrorLine);
            }

            if (complainWithThrow)
                throw new ScriptException(code, result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for NewInterpreterCallback
        private ReturnCode TestNewInterpreterCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            // CheckDisposed();

            if ((callbackInterpreter != null) &&
                !String.IsNullOrEmpty(newInterpreterText))
            {
                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("interpreter", interpreter);
                objects.Add("clientData", clientData);

                return Helpers.EvaluateScript(
                    callbackInterpreter, newInterpreterText, objects,
                    ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                    ObjectOps.GetDefaultDispose(), ref result);
            }

            result = null;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for PackageCallback
        private ReturnCode TestPackageFallbackCallback(
            Interpreter interpreter,
            string name,
            Version version,
            string text,
            PackageFlags flags,
            bool exact,
            ref Result result
            )
        {
            // CheckDisposed();

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (name == null)
            {
                result = "invalid package name";
                return ReturnCode.Error;
            }

            if (version == null)
            {
                result = "invalid package version";
                return ReturnCode.Error;
            }

            if (packageFallbackText != null)
            {
                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("methodName", "PackageFallback");
                objects.Add("name", name);
                objects.Add("version", version);
                objects.Add("text", text);
                objects.Add("flags", flags);
                objects.Add("exact", exact);

                Result localResult = null;

                if (!ResultOps.IsOkOrReturn(Helpers.EvaluateScript(
                        interpreter, packageFallbackText, objects,
                        ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                        ObjectOps.GetDefaultDispose(), ref localResult)))
                {
                    result = localResult;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        interpreter, localResult, ref value,
                        ref localResult) != ReturnCode.Ok)
                {
                    result = localResult;
                    return ReturnCode.Error;
                }

                if (value)
                {
                    localResult = null;

                    if (interpreter.PkgProvide(
                            name, version, flags,
                            ref localResult) == ReturnCode.Ok)
                    {
                        result = localResult;
                    }
                    else
                    {
                        result = localResult;
                        return ReturnCode.Error;
                    }
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

            return ReturnCode.Ok;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        #region Methods for Ad-Hoc Commands
        private static void TestVoidMethod(
            string value
            )
        {
            //
            // NOTE: Write a string to the console as long as it is not null.
            //
            if (value == null)
                throw new ArgumentNullException("value");

            Interpreter interpreter = Interpreter.GetActive();

            if (interpreter == null)
                throw new ScriptException("invalid interpreter");

            ReturnCode code;
            Result result = null;

            code = ScriptOps.WriteViaIExecute(
                interpreter, null, null, value, ref result);

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long TestLongMethod(
            DateTime dateTime
            )
        {
            //
            // NOTE: Just return the number of ticks.
            //
            return dateTime.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IEnumerable TestIEnumerableMethod(
            ICommand command
            )
        {
            //
            // NOTE: Return the sub-commands for the command.
            //
            if (command != null)
            {
                EnsembleDictionary subCommands = PolicyOps.GetSubCommandsUnsafe(
                    command); /* TEST NAME LIST USE ONLY */

                if (subCommands != null)
                {
                    StringList result = new StringList(subCommands.Keys);
                    result.Sort(); return result;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TestHasOnlyObjectArrayParameter(
            Delegate callback
            )
        {
            if (callback == null)
                return false;

            MethodInfo methodInfo = callback.Method;

            if (methodInfo == null)
                return false;

            ParameterInfo[] parameterInfo = methodInfo.GetParameters();

            if ((parameterInfo == null) || (parameterInfo.Length != 1))
                return false;

            ParameterInfo firstParameterInfo = parameterInfo[0];

            if (firstParameterInfo == null)
                return false;

            return (firstParameterInfo.ParameterType == typeof(object[]));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for ScriptWebClient
#if NETWORK
        private static WebClient TestScriptNewWebClientCallback(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            string text = null;

            if (clientData != null)
            {
                object data = null;

                clientData = ClientData.UnwrapOrReturn(
                    clientData, ref data);

                text = data as string;
            }

            if (text != null)
            {
                return ScriptWebClient.Create(
                    interpreter, text, argument, ref error);
            }
            else
            {
                return WebOps.CreateClient();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static WebClient TestErrorNewWebClientCallback(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
            )
        {
            string text = null;

            if (clientData != null)
            {
                object data = null;

                clientData = ClientData.UnwrapOrReturn(
                    clientData, ref data);

                text = data as string;
            }

            if (text != null)
                error = text;
            else
                error = "creation of web client forbidden";

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for StringOps.MatchCore
        private static ReturnCode TestMatchCallback(
            Interpreter interpreter,
            MatchMode mode,
            string text,
            string pattern,
            IClientData clientData,
            ref bool match,
            ref Result error
            )
        {
            bool noCase = false;
            IComparer<string> comparer = null;
            RegexOptions regExOptions = RegexOptions.None;

            if (clientData != null)
            {
                ObjectList objectList = clientData.Data as ObjectList;

                if ((objectList != null) && (objectList.Count >= 3))
                {
                    if (objectList[0] is bool)
                        noCase = (bool)objectList[0];

                    if (objectList[1] is IComparer<string>)
                        comparer = (IComparer<string>)objectList[1];

                    if (objectList[2] is RegexOptions)
                        regExOptions = (RegexOptions)objectList[2];
                }
            }

            if (FlagOps.HasFlags(mode, MatchMode.Callback, true))
            {
                MatchMode[] modes = (MatchMode[])Enum.GetValues(
                    typeof(MatchMode));

                foreach (MatchMode localMode in modes)
                {
                    if ((localMode == MatchMode.None) ||
                        (localMode == MatchMode.Invalid) ||
                        (localMode == MatchMode.Callback) ||
                        !FlagOps.HasFlags(localMode,
                            MatchMode.ModeMask, false) ||
                        FlagOps.HasFlags(localMode,
                            MatchMode.ModeMask, true))
                    {
                        continue;
                    }

                    ReturnCode code;
                    bool localMatch = false;
                    Result localError = null;

                    code = StringOps.Match(
                        interpreter, localMode, text,
                        pattern, noCase, comparer,
                        regExOptions, ref localMatch,
                        ref localError);

                    if (code != ReturnCode.Ok)
                    {
                        error = localError;
                        return code;
                    }

                    if (localMatch)
                    {
                        match = localMatch;
                        return ReturnCode.Ok;
                    }
                }

                match = false;
                return ReturnCode.Ok;
            }
            else
            {
                return StringOps.Match(
                    interpreter, mode, text, pattern,
                    noCase, comparer, regExOptions,
                    ref match, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Methods for DebugOps.Complain
        private static void TestComplainCallbackFail(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            string formatted = FormatOps.Complaint(
                id, code, result, stackTrace);

#if CONSOLE
            ConsoleOps.WriteComplaint(formatted);
#endif

            DebugOps.Fail(typeof(Default).FullName, formatted);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackBreak(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
#if CONSOLE
            ConsoleOps.WriteComplaint(FormatOps.Complaint(
                id, code, result, stackTrace));
#endif

            DebugOps.Break();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackThrow(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            TestComplainCallbackSetVariable(
                "test_complain_throw", interpreter, id, code, result,
                stackTrace, quiet, retry, levels);

            throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackNoThrow(
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            TestComplainCallbackSetVariable(
                "test_complain_no_throw", interpreter, id, code, result,
                stackTrace, quiet, retry, levels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TestComplainCallbackSetVariable(
            string varName,
            Interpreter interpreter,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool quiet,
            int retry,
            int levels
            )
        {
            if (interpreter != null)
            {
                ReturnCode setCode;
                Result setError = null;

                setCode = interpreter.SetVariableValue(
                    VariableFlags.None, varName, StringList.MakeList(
                    "retry", retry, "levels", levels, "formatted",
                    FormatOps.Complaint(id, code, result, stackTrace)),
                    ref setError);

                if ((setCode != ReturnCode.Ok) && (levels == 1))
                {
                    DebugOps.Complain(
                        interpreter, setCode, setError); /* RECURSIVE */
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Policy Callback Methods
        [MethodFlags(MethodFlags.PluginPolicy | MethodFlags.NoAdd)]
        private static ReturnCode TestLoadPluginPolicy( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments, /* NOT USED */
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                result = "invalid policy clientData";
                return ReturnCode.Error;
            }

            IPolicyContext policyContext = clientData.Data as IPolicyContext;

            if (policyContext == null)
            {
                result = "policy clientData is not a policyContext object";
                return ReturnCode.Error;
            }

            string fileName = policyContext.FileName;

            if (String.IsNullOrEmpty(fileName))
            {
                policyContext.Denied("no plugin file name was supplied");
                return ReturnCode.Ok;
            }

            string typeName = policyContext.TypeName;

            if (StringOps.Match(
                    interpreter, MatchMode.Glob, typeName, "*.Class4", true))
            {
                policyContext.Denied("access to plugin Class4 is denied");
                return ReturnCode.Ok;
            }

            if (StringOps.Match(
                    interpreter, MatchMode.Glob, typeName, "*.Class3", true))
            {
                policyContext.Approved("access to plugin Class3 is granted");
                return ReturnCode.Ok;
            }

            policyContext.Undecided("plugin type name is unknown");
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TestFormatArgs(
            object[] args
            )
        {
            if (args == null)
                return FormatOps.DisplayNull;

            int length = args.Length;

            if (length == 0)
                return FormatOps.DisplayEmpty;

            return String.Format("object[{0}]", length);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode TestShellArgumentCallback(
            Interpreter interpreter,
            IInteractiveHost interactiveHost,
            IClientData clientData,
            int count,
            string arg,
            ref IList<string> argv,
            ref Result result
            )
        {
            int argc = (argv != null) ? argv.Count : 0;

            if ((count > 0) &&
                StringOps.MatchSwitch(arg, "one"))
            {
                GenericOps<string>.PopFirstArgument(ref argv);

                result += String.Format(
                    "argument one OK{0}",
                    Characters.Pipe);

                return ReturnCode.Ok;
            }
            else if ((count > 0) &&
                StringOps.MatchSwitch(arg, "two"))
            {
                if (argc >= 2)
                {
                    string value = argv[1];

                    GenericOps<string>.PopFirstArgument(ref argv);
                    GenericOps<string>.PopFirstArgument(ref argv);

                    result += String.Format(
                        "argument two {0} OK{1}",
                        FormatOps.WrapOrNull(value),
                        Characters.Pipe);

                    return ReturnCode.Ok;
                }
                else
                {
                    result += String.Format(
                        "wrong # args: should be \"-two <value>\"{0}",
                        Characters.Pipe);
                }
            }
            else if ((count > 0) &&
                StringOps.MatchSwitch(arg, "three"))
            {
                GenericOps<string>.PopFirstArgument(ref argv);

                result += String.Format(
                    "argument three ERROR{0}",
                    Characters.Pipe);
            }
            else
            {
                result += String.Format(
                    "invalid test argument {0}{1}",
                    FormatOps.WrapOrNull(arg),
                    Characters.Pipe);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static DateTime TestDateTimeNow()
        {
            if (nowIncrement != 0)
                now = now.AddTicks(nowIncrement);

            return now;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static TypeList GetParameterTypeList(
            MethodInfo methodInfo
            )
        {
            TypeList result = new TypeList();

            if (methodInfo != null)
                foreach (ParameterInfo parameterInfo in methodInfo.GetParameters())
                    result.Add(parameterInfo.ParameterType);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        private static IntPtrList TestIntPtrList()
        {
            return new IntPtrList(new IntPtr[] {
                new IntPtr(-1), IntPtr.Zero, new IntPtr(1)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static CharList TestCharList()
        {
            return new CharList(Characters.ListReservedCharList);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Static Properties
        public static Type StaticTypeProperty
        {
            get { return staticTypeField; }
            set { staticTypeField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object StaticObjectProperty
        {
            get { return staticObjectField; }
            set { staticObjectField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool StaticDynamicInvoke
        {
            get { return staticDynamicInvoke; }
            set { staticDynamicInvoke = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Properties
        public Type TypeProperty
        {
            get { CheckDisposed(); return typeField; }
            set { CheckDisposed(); typeField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public object ObjectProperty
        {
            get { CheckDisposed(); return objectField; }
            set { CheckDisposed(); objectField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool SimpleBoolProperty
        {
            get { CheckDisposed(); return boolField; }
            set { CheckDisposed(); boolField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public byte SimpleByteProperty
        {
            get { CheckDisposed(); return byteField; }
            set { CheckDisposed(); byteField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public short SimpleShortProperty
        {
            get { CheckDisposed(); return shortField; }
            set { CheckDisposed(); shortField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int SimpleIntProperty
        {
            get { CheckDisposed(); return intField; }
            set { CheckDisposed(); intField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public long SimpleLongProperty
        {
            get { CheckDisposed(); return longField; }
            set { CheckDisposed(); longField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public decimal SimpleDecimalProperty
        {
            get { CheckDisposed(); return decimalField; }
            set { CheckDisposed(); decimalField = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int ReadOnlyProperty
        {
            get { CheckDisposed(); return intField; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool DynamicInvoke
        {
            get { CheckDisposed(); return dynamicInvoke; }
            set { CheckDisposed(); dynamicInvoke = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Indexer Properties
        public int this[int index]
        {
            get { CheckDisposed(); return intArrayField[index]; }
            set { CheckDisposed(); intArrayField[index] = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int this[int index, string more]
        {
            get { CheckDisposed(); return intArrayField[index]; }
            set { CheckDisposed(); intArrayField[index] = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Complain Properties
        public ReturnCode ComplainCode
        {
            get { CheckDisposed(); return complainCode; }
            set { CheckDisposed(); complainCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Result ComplainResult
        {
            get { CheckDisposed(); return complainResult; }
            set { CheckDisposed(); complainResult = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int ComplainErrorLine
        {
            get { CheckDisposed(); return complainErrorLine; }
            set { CheckDisposed(); complainErrorLine = value; }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Helpers Class
        [ObjectId("0ea7f7bb-bf64-4a11-b11c-20ff8be94024")]
        public static class Helpers
        {
            #region Private Constants
            //
            // NOTE: The object flags to use when calling FixupReturnValue on the
            //       various method parameters passed required by the script being
            //       evaluated to handle formal interface methods.
            //
            private static readonly ObjectFlags DefaultObjectFlags =
                ObjectFlags.Default | ObjectFlags.NoBinder |
                ObjectFlags.NoDispose | ObjectFlags.AddReference;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The object option type to use when calling FixupReturnValue
            //       on the various method parameters passed required by the
            //       script being evaluated to handle formal interface methods.
            //
            private static readonly ObjectOptionType DefaultObjectOptionType =
                ObjectOptionType.Default;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private static ReturnCode FixupReturnValue(
                Interpreter interpreter,
                string objectName,
                object value,
                ObjectFlags objectFlags,
                ref Result result
                )
            {
                return MarshalOps.FixupReturnValue(
                    interpreter, null, objectFlags | DefaultObjectFlags,
                    null, DefaultObjectOptionType, objectName, value, true,
                    true, false, ref result);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private static ReturnCode RemoveObject(
                Interpreter interpreter,
                string objectName,
                bool synchronous,
                bool dispose,
                ref Result result
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                return interpreter.RemoveObject(
                    objectName, _Public.ClientData.Empty, synchronous,
                    ref dispose, ref result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public static ReturnCode FixupReturnValues(
                Interpreter interpreter,
                ObjectDictionary objects,
                ObjectFlags objectFlags,
                ref StringList objectNames,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (objects != null)
                {
                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                    {
                        foreach (KeyValuePair<string, object> pair in objects)
                        {
                            string objectName = pair.Key;

                            if (String.IsNullOrEmpty(objectName))
                                objectName = null; /* AUTOMATIC */

                            Result localResult = null;

                            if (FixupReturnValue(interpreter,
                                    objectName, pair.Value, objectFlags,
                                    ref localResult) != ReturnCode.Ok)
                            {
                                error = localResult;
                                return ReturnCode.Error;
                            }

                            if (localResult != null)
                            {
                                if (objectNames == null)
                                    objectNames = new StringList();

                                objectNames.Add(localResult);
                            }
                        }
                    }
                }

                return ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static void RemoveObjects(
                Interpreter interpreter,
                StringList objectNames,
                bool synchronous,
                bool dispose
                )
            {
                if ((interpreter != null) && (objectNames != null))
                {
                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                    {
                        foreach (string objectName in objectNames)
                        {
                            if (objectName == null)
                                continue;

                            ReturnCode removeCode;
                            Result removeResult = null;

                            removeCode = RemoveObject(
                                interpreter, objectName, synchronous,
                                dispose, ref removeResult);

                            if (removeCode != ReturnCode.Ok)
                            {
                                DebugOps.Complain(
                                    interpreter, removeCode, removeResult);
                            }
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode EvaluateScript(
                Interpreter interpreter,
                string text,
                ObjectDictionary objects,
                ObjectFlags objectFlags,
                bool synchronous,
                bool dispose,
                ref Result result
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                StringList objectNames = null;

                try
                {
                    Result error = null;

                    if (FixupReturnValues(
                            interpreter, objects, objectFlags,
                            ref objectNames, ref error) != ReturnCode.Ok)
                    {
                        result = error;
                        return ReturnCode.Error;
                    }

                    return interpreter.EvaluateScript(text, ref result);
                }
                finally
                {
                    /* NO RESULT */
                    RemoveObjects(
                        interpreter, objectNames, synchronous, dispose);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static ReturnCode ToBoolean(
                Interpreter interpreter,
                Result result,
                ref bool value,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                return Value.GetBoolean3(
                    result, ValueFlags.AnyBoolean, interpreter.CultureInfo,
                    ref value, ref error);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region WebClient Test Class
#if NETWORK
        [ObjectId("a772f475-0016-4016-a444-310b1be9ba58")]
        public class ScriptWebClient : WebClient, IHaveInterpreter /* NOT SEALED */
        {
            #region Private Constructors
            private ScriptWebClient(
                Interpreter interpreter,
                string text,
                string argument
                )
            {
                this.interpreter = interpreter;
                this.text = text;
                this.argument = argument;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static WebClient Create(
                Interpreter interpreter,
                string text,
                string argument,
                ref Result error /* NOT USED */
                )
            {
                return new ScriptWebClient(interpreter, text, argument);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            //
            // NOTE: This is the script to evaluate in response to the methods
            //       overridden methods from the base class (WebClient).
            //
            private string text;
            public virtual string Text
            {
                get { CheckDisposed(); return text; }
                set { CheckDisposed(); text = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the string argument (e.g. method name) passed to
            //       the web client creation callback that was responsible for
            //       creating this web client instance.
            //
            private string argument;
            public virtual string Argument
            {
                get { CheckDisposed(); return argument; }
                set { CheckDisposed(); argument = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter / ISetInterpreter Members
            //
            // NOTE: This is the interpreter context that the script will be
            //       evaluated in.
            //
            private Interpreter interpreter;
            public virtual Interpreter Interpreter
            {
                get { CheckDisposed(); return interpreter; }
                set { CheckDisposed(); interpreter = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            protected virtual void Complain(
                Interpreter interpreter,
                ReturnCode code,
                Result result
                )
            {
                DebugOps.Complain(interpreter, code, result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Net.WebClient Overrides
            protected override WebRequest GetWebRequest(
                Uri address
                )
            {
                WebRequest webRequest = base.GetWebRequest(address);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("argument", this.Argument);
                objects.Add("methodName", "GetWebRequest");
                objects.Add("address", address);
                objects.Add("webRequest", webRequest);

                ReturnCode localCode;
                Result localResult = null;

                localCode = Helpers.EvaluateScript(
                    this.Interpreter, this.Text, objects,
                    ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                    ObjectOps.GetDefaultDispose(), ref localResult);

                if (localCode != ReturnCode.Ok)
                    Complain(this.Interpreter, localCode, localResult);

                return webRequest;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override WebResponse GetWebResponse(
                WebRequest request
                )
            {
                WebResponse webResponse = base.GetWebResponse(request);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("argument", this.Argument);
                objects.Add("methodName", "GetWebResponse");
                objects.Add("webRequest", request);
                objects.Add("webResponse", webResponse);

                ReturnCode localCode;
                Result localResult = null;

                localCode = Helpers.EvaluateScript(
                    this.Interpreter, this.Text, objects,
                    ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                    ObjectOps.GetDefaultDispose(), ref localResult);

                if (localCode != ReturnCode.Ok)
                    Complain(this.Interpreter, localCode, localResult);

                return webResponse;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override WebResponse GetWebResponse(
                WebRequest request,
                IAsyncResult result
                )
            {
                WebResponse webResponse = base.GetWebResponse(request);

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("argument", this.Argument);
                objects.Add("methodName", "GetWebResponse");
                objects.Add("webRequest", request);
                objects.Add("asyncResult", result);
                objects.Add("webResponse", webResponse);

                ReturnCode localCode;
                Result localResult = null;

                localCode = Helpers.EvaluateScript(
                    this.Interpreter, this.Text, objects,
                    ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                    ObjectOps.GetDefaultDispose(), ref localResult);

                if (localCode != ReturnCode.Ok)
                    Complain(this.Interpreter, localCode, localResult);

                return webResponse;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptWebClient).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            interpreter = null; /* NOT OWNED */
                            text = null;
                            argument = null;
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////
                    }
                }
                finally
                {
                    base.Dispose(disposing);

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptWebClient()
            {
                Dispose(false);
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IResolve Test Class
        [ObjectId("c420fccf-69f8-463a-b97b-629d7f7fcd9f")]
        public sealed class Resolve :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
            ScriptMarshalByRefObject,
#endif
            IResolve
        {
            #region Private Data
            //
            // NOTE: The interpreter where the script should be evaluated in.
            //
            private Interpreter sourceInterpreter;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The interpreter where the variable frame, current namespace,
            //           execute, or variable is being resolved.
            //
            private Interpreter targetInterpreter;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The script to evaluate when this resolver instance is called.
            //
            private string text;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The call frame to return from the GetVariableFrame method
            //       if the script returns non-zero.
            //
            private ICallFrame frame;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The namespace to return from the GetCurrentNamespace method
            //       if the script returns non-zero.
            //
            private INamespace @namespace;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The command to return from the GetIExecute method if the
            //       script returns non-zero.
            //
            private IExecute execute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The variable to return from the GetVariable method if the
            //       script returns non-zero.
            //
            private IVariable variable;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: These are flags that control the behavior of the various
            //       IResolve methods of this class.
            //
            private TestResolveFlags testResolveFlags;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Keeps track of the number of times each IResolve method
            //       has been called.
            //
            private readonly int[] methodInvokeCounts = {
                0, /* GetVariableFrame */
                0, /* GetCurrentNamespace */
                0, /* GetIExecute */
                0  /* GetVariable */
            };
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public Resolve(
                Interpreter sourceInterpreter,
                Interpreter targetInterpreter,
                string text,
                ICallFrame frame,
                INamespace @namespace,
                IExecute execute,
                IVariable variable,
                TestResolveFlags testResolveFlags
                )
            {
                this.sourceInterpreter = sourceInterpreter;
                this.targetInterpreter = targetInterpreter;
                this.text = text;
                this.frame = frame;
                this.@namespace = @namespace;
                this.execute = execute;
                this.variable = variable;
                this.testResolveFlags = testResolveFlags;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierBase Members
            public IdentifierKind Kind
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Guid Id
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public string Name
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifier Members
            public string Group
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public string Description
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter / ISetInterpreter Members
            public Interpreter Interpreter
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetClientData / ISetClientData Members
            public IClientData ClientData
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IWrapperData Members
#if MONO_BUILD
#pragma warning disable 414
#endif
            private long token;
            public long Token
            {
                get { throw new NotImplementedException(); }
                set { token = value; }
            }
#if MONO_BUILD
#pragma warning restore 414
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IResolve Members
            public ReturnCode GetVariableFrame(
                ref ICallFrame frame,
                ref string varName,
                ref VariableFlags flags,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[0]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetVariableFrame: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, varName = {4}, flags = {5}, error = {6}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(varName), FormatOps.WrapOrNull(flags),
                        FormatOps.WrapOrNull(true, true, error)), typeof(Resolve).Name,
                        TracePriority.TestDebug);
                }

                if (!FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.HandleGlobalOnly, true) &&
                    FlagOps.HasFlags(flags, VariableFlags.GlobalOnly, true))
                {
                    if (targetInterpreter != null)
                    {
                        frame = targetInterpreter.CurrentGlobalFrame;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "invalid target interpreter";
                        return ReturnCode.Error;
                    }
                }

                if (!FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.HandleAbsolute, true) &&
                    NamespaceOps.IsAbsoluteName(varName))
                {
                    return NamespaceOps.GetVariableFrame(
                        targetInterpreter, ref frame, ref varName, ref flags,
                        ref error);
                }

                if (!FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.HandleQualified, true) &&
                    NamespaceOps.IsQualifiedName(varName))
                {
                    return NamespaceOps.GetVariableFrame(
                        targetInterpreter, ref frame, ref varName, ref flags,
                        ref error);
                }

                ICallFrame variableFrame = GetVariableFrame();

                if (variableFrame == null)
                {
                    error = "variable frame not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetVariableFrame");
                objects.Add("frame", frame);
                objects.Add("varName", varName);
                objects.Add("flags", flags);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ObjectFlags.None,
                        ObjectOps.GetDefaultSynchronous(),
                        ObjectOps.GetDefaultDispose(),
                        ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    frame = variableFrame;
                    return ReturnCode.Ok;
                }

                error = "variable frame not found";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode GetCurrentNamespace(
                ICallFrame frame,
                ref INamespace @namespace,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[1]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetCurrentNamespace: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, namespace = {4}, error = {5}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(@namespace), FormatOps.WrapOrNull(true, true, error)),
                        typeof(Resolve).Name, TracePriority.TestDebug);
                }

                INamespace currentNamespace = GetCurrentNamespace();

                if (currentNamespace == null)
                {
                    error = "current namespace not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetCurrentNamespace");
                objects.Add("frame", frame);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ObjectFlags.None,
                        ObjectOps.GetDefaultSynchronous(),
                        ObjectOps.GetDefaultDispose(),
                        ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    @namespace = currentNamespace;
                    testResolveFlags |= TestResolveFlags.NextUseNamespaceFrame;

                    return ReturnCode.Ok;
                }

                error = "current namespace not found";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode GetIExecute(
                ICallFrame frame,
                EngineFlags engineFlags,
                string name,
                ArgumentList arguments,
                LookupFlags lookupFlags,
                ref bool ambiguous,
                ref long token,
                ref IExecute execute,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[2]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetIExecute: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, engineFlags = {4}, name = {5}, " +
                        "arguments = {6}, lookupFlags = {7}, ambiguous = {8}, token = {9}, " +
                        "execute = {10}, error = {11}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(engineFlags), FormatOps.WrapOrNull(name),
                        FormatOps.WrapOrNull(arguments), FormatOps.WrapOrNull(lookupFlags),
                        FormatOps.WrapOrNull(ambiguous), FormatOps.WrapOrNull(token),
                        FormatOps.WrapOrNull(execute), FormatOps.WrapOrNull(true, true, error)),
                        typeof(Resolve).Name, TracePriority.TestDebug);
                }

                if (this.execute == null)
                {
                    error = "execute not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetIExecute");
                objects.Add("frame", frame);
                objects.Add("engineFlags", engineFlags);
                objects.Add("name", name);
                objects.Add("arguments", arguments);
                objects.Add("lookupFlags", lookupFlags);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ObjectFlags.None,
                        ObjectOps.GetDefaultSynchronous(),
                        ObjectOps.GetDefaultDispose(),
                        ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    execute = this.execute;
                    return ReturnCode.Ok;
                }

                error = "execute not found";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode GetVariable(
                ICallFrame frame,
                string varName,
                string varIndex,
                ref VariableFlags flags,
                ref IVariable variable,
                ref Result error
                )
            {
                Interlocked.Increment(ref methodInvokeCounts[3]);

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.EnableLogging, true))
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetVariable: sourceInterpreter = {0}, targetInterpreter = {1}, " +
                        "text = {2}, frame = {3}, varName = {4}, varIndex = {5}, flags = {6}, " +
                        "variable = {7}, error = {8}",
                        FormatOps.InterpreterNoThrow(sourceInterpreter),
                        FormatOps.InterpreterNoThrow(targetInterpreter),
                        FormatOps.WrapOrNull(true, true, text), FormatOps.WrapOrNull(frame),
                        FormatOps.WrapOrNull(varName), FormatOps.WrapOrNull(varIndex),
                        FormatOps.WrapOrNull(flags), FormatOps.WrapOrNull(variable),
                        FormatOps.WrapOrNull(true, true, error)), typeof(Resolve).Name,
                        TracePriority.TestDebug);
                }

                if (this.variable == null)
                {
                    error = "variable not configured";
                    return ReturnCode.Continue;
                }

                ObjectDictionary objects = new ObjectDictionary();

                objects.Add("targetInterpreter", targetInterpreter);
                objects.Add("methodName", "GetVariable");
                objects.Add("frame", frame);
                objects.Add("varName", varName);
                objects.Add("varIndex", varIndex);

                Result result = null;

                if (Helpers.EvaluateScript(
                        sourceInterpreter, text,
                        objects, ObjectFlags.None,
                        ObjectOps.GetDefaultSynchronous(),
                        ObjectOps.GetDefaultDispose(),
                        ref result) != ReturnCode.Ok)
                {
                    error = result;
                    return ReturnCode.Error;
                }

                bool value = false;

                if (Helpers.ToBoolean(
                        sourceInterpreter, result, ref value,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (value)
                {
                    variable = this.variable;
                    return ReturnCode.Ok;
                }

                error = "variable not found";
                return ReturnCode.Error;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private ICallFrame GetVariableFrame()
            {
                if (frame != null)
                    return frame;

                if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.AlwaysUseNamespaceFrame,
                        true))
                {
                    if (@namespace != null)
                        return @namespace.VariableFrame;
                }
                else if (FlagOps.HasFlags(
                        testResolveFlags, TestResolveFlags.NextUseNamespaceFrame,
                        true))
                {
                    /* ONE SHOT */
                    testResolveFlags &= ~TestResolveFlags.NextUseNamespaceFrame;

                    if (@namespace != null)
                        return @namespace.VariableFrame;
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: For use by the IResolve.GetCurrentNamespace method only.
            //
            private INamespace GetCurrentNamespace()
            {
                testResolveFlags &= ~TestResolveFlags.NextUseNamespaceFrame;

                return @namespace;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                StringList list = new StringList();

                for (int index = 0; index < methodInvokeCounts.Length; index++)
                    list.Add(methodInvokeCounts[index].ToString());

                return list.ToString();
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISubCommand Test Class
        [ObjectId("b9d6a0c9-8bca-4558-8b55-68334118978f")]
        public sealed class SubCommand : ISubCommand
        {
            #region Private Data
            //
            // NOTE: The script command to evaluate when this sub-command
            //       instance is executed (this only applies if the
            //       "useIExecute" flag is false).
            //
            private StringList scriptCommand;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: The IExecute to execute when this sub-command instance
            //       is executed (this only applies if the "useIExecute" flag
            //       is true).
            //
            private IExecute execute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the IExecute will be used instead
            //       of the script command.
            //
            private bool useIExecute;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: When this is non-zero, the arguments to the sub-command
            //       will be appended to the script command to be evaluated
            //       (this only applies if the "useIExecute" flag is false).
            //
            private bool useExecuteArguments;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This keeps track of the number of times the Execute
            //       method of this class handles a sub-command.  This value
            //       starts at zero, is always incremented, and never reset.
            //
            private int executeCount;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public SubCommand(
                string name,
                ICommand command,
                ExecuteCallback callback,
                IClientData clientData,
                CommandFlags commandFlags,
                StringList scriptCommand,
                IExecute execute,
                bool useIExecute,
                bool useExecuteArguments
                )
            {
                this.name = name;
                this.command = command;
                this.callback = callback;
                this.clientData = clientData;
                this.commandFlags = commandFlags;
                this.scriptCommand = scriptCommand;
                this.execute = execute;
                this.useIExecute = useIExecute;
                this.useExecuteArguments = useExecuteArguments;
                this.executeCount = 0;

                ///////////////////////////////////////////////////////////////////////////////////////

                SetupSubCommands();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void SetupSubCommands()
            {
                //
                // NOTE: We only handle one sub-command at a time and it is
                //       always handled locally (i.e. using null ISubCommand
                //       instance).
                //
                subCommands = new EnsembleDictionary();
                subCommands.Add(this.Name, null);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private ArgumentList GetArgumentsForExecute(
                ArgumentList arguments
                )
            {
                return useExecuteArguments ? arguments : null;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetClientData / ISetClientData Members
            private IClientData clientData;
            public IClientData ClientData
            {
                get { return clientData; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifierBase Members
            public IdentifierKind Kind
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Guid Id
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string name;
            public string Name
            {
                get { return name; }
                set { name = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IIdentifier Members
            public string Group
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public string Description
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ICommandBaseData Members
            public string TypeName
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private CommandFlags commandFlags;
            public CommandFlags CommandFlags
            {
                get { return commandFlags; }
                set { commandFlags = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IWrapperData Members
            public long Token
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IHaveCommand Members
            private ICommand command;
            public ICommand Command
            {
                get { return command; }
                set { command = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ISubCommandData Members
            public SubCommandFlags Flags
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDynamicExecute Members
            private ExecuteCallback callback;
            public ExecuteCallback Callback
            {
                get { return callback; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IEnsemble Members
            private EnsembleDictionary subCommands;
            public EnsembleDictionary SubCommands
            {
                get { return subCommands; }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IPolicyEnsemble Members
            public EnsembleDictionary AllowedSubCommands
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public EnsembleDictionary DisallowedSubCommands
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ISyntax Members
            public string Syntax
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IUsageData Members
            public bool ResetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool GetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool SetUsage(
                UsageType type,
                ref long value
                )
            {
                throw new NotImplementedException();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool AddUsage(
                UsageType type,
                long value
                )
            {
                //
                // NOTE: This is a stub required by the Engine class.
                //       Do nothing.
                //
                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecute Members
            public ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Result result
                )
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    result = "invalid argument list";
                    return ReturnCode.Error;
                }

                if (arguments.Count < 2)
                {
                    result = String.Format(
                        "wrong # args: should be \"{0} option ?arg ...?\"",
                        this.Name);

                    return ReturnCode.Error;
                }

                string subCommand = arguments[1];

                if (!String.Equals(
                        subCommand, this.Name,
                        StringOps.SystemStringComparisonType))
                {
                    result = ScriptOps.BadSubCommand(
                        interpreter, null, null, subCommand, this, null, null);

                    return ReturnCode.Error;
                }

                Interlocked.Increment(ref executeCount);

                if (useIExecute)
                {
                    //
                    // NOTE: Re-dispatch to the configured IExecute instance
                    //       and return its results verbatim.
                    //
                    string commandName = EntityOps.GetName(
                        execute as IIdentifierBase);

                    if (commandName == null)
                        commandName = arguments[0];

                    return interpreter.Execute(
                        commandName, execute, clientData, arguments,
                        ref result);
                }
                else
                {
                    //
                    // NOTE: Evaluate the configured script command, maybe
                    //       adding all the local arguments, and return the
                    //       results verbatim.
                    //
                    string name = StringList.MakeList(this.Name);

                    ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                        CallFrameFlags.Evaluate | CallFrameFlags.Test |
                        CallFrameFlags.SubCommand);

                    interpreter.PushAutomaticCallFrame(frame);

                    ReturnCode code = interpreter.EvaluateScript(
                        ScriptOps.GetArgumentsForExecute(this, scriptCommand,
                        GetArgumentsForExecute(arguments), 0), 0, ref result);

                    if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format("{0}    (\"{1}\" body line {2})",
                                Environment.NewLine, ScriptOps.GetNameForExecute(
                                arguments[0], this), Interpreter.GetErrorLine(
                                interpreter)));
                    }

                    //
                    // NOTE: Pop the original call frame that we pushed above and
                    //       any intervening scope call frames that may be leftover
                    //       (i.e. they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFramesAndOneMore();
                    return code;
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ScriptTraceListener Test Class
        [ObjectId("91730fa7-dbe4-42cd-b175-9ccb66cae405")]
        public class ScriptTraceListener : TraceListener
        {
            #region Private Data
            private static int traceLevels = 0;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Constructors
            private ScriptTraceListener(
                Interpreter interpreter,
                string text,
                string argument
                )
            {
                this.interpreter = interpreter;
                this.text = text;
                this.argument = argument;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static TraceListener Create(
                Interpreter interpreter,
                string text,
                string argument,
                ref Result error /* NOT USED */
                )
            {
                return new ScriptTraceListener(interpreter, text, argument);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            //
            // NOTE: This is the script to evaluate in response to the methods
            //       overridden methods from the base class (TraceListener).
            //
            private string text;
            public virtual string Text
            {
                get { CheckDisposed(); return text; }
                set { CheckDisposed(); text = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the string argument (e.g. method name) passed to
            //       the trace listener creation callback that was responsible
            //       for creating this trace listener instance.
            //
            private string argument;
            public virtual string Argument
            {
                get { CheckDisposed(); return argument; }
                set { CheckDisposed(); argument = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IGetInterpreter / ISetInterpreter Members
            //
            // NOTE: This is the interpreter context that the script will be
            //       evaluated in.
            //
            private Interpreter interpreter;
            public virtual Interpreter Interpreter
            {
                get { CheckDisposed(); return interpreter; }
                set { CheckDisposed(); interpreter = value; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Methods
            protected virtual bool IsUsable()
            {
                //
                // NOTE: Since this class uses the complaint subsystem,
                //       both directly and indirectly, it is not usable
                //       if there is a complaint pending.
                //
                return !DebugOps.IsComplainPending();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected virtual void Complain(
                Interpreter interpreter,
                ReturnCode code,
                Result result
                )
            {
                //
                // NOTE: This should be legal even though the complaint
                //       subsystem (potentially) calls into this trace
                //       listener (e.g. via the Trace.Write, which uses
                //       the Trace.Listeners collection) because all of
                //       the TraceListener method overrides avoid doing
                //       any processing when a complaint is pending.
                //
                DebugOps.Complain(interpreter, code, result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "Close");

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                            ObjectOps.GetDefaultDispose(), ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "Flush");

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                            ObjectOps.GetDefaultDispose(), ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message
                )
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "Write");
                        objects.Add("message", message);

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                            ObjectOps.GetDefaultDispose(), ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message
                )
            {
                CheckDisposed();

                if (!IsUsable())
                    return;

                //
                // NOTE: Avoid doing any processing when any of the
                //       TraceListener method overrides are already
                //       pending because this class is not designed
                //       to handle reentrancy.  This has the effect
                //       of suppressing any trace messages arising
                //       out of the contained script evaluation.
                //
                int levels = Interlocked.Increment(ref traceLevels);

                try
                {
                    if (levels == 1)
                    {
                        ObjectDictionary objects = new ObjectDictionary();

                        objects.Add("argument", this.Argument);
                        objects.Add("methodName", "WriteLine");
                        objects.Add("message", message);

                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = Helpers.EvaluateScript(
                            this.Interpreter, this.Text, objects,
                            ObjectFlags.None, ObjectOps.GetDefaultSynchronous(),
                            ObjectOps.GetDefaultDispose(), ref localResult);

                        if (localCode != ReturnCode.Ok)
                            Complain(this.Interpreter, localCode, localResult);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref traceLevels);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                {
                    throw new ObjectDisposedException(
                        typeof(ScriptTraceListener).Name);
                }
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            interpreter = null; /* NOT OWNED */
                            text = null;
                            argument = null;
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////
                    }
                }
                finally
                {
                    base.Dispose(disposing);

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~ScriptTraceListener()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region TraceListener Test Class
        [ObjectId("73634227-a942-4f0f-be47-1d395e1a9750")]
        public sealed class Listener : TraceListener
        {
            #region Private Constants
            //
            // HACK: These are purposely not marked as read-only.
            //
            private static Encoding DefaultEncoding = Encoding.UTF8;
            private static int DefaultBufferSize = 1024;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Data
            private Stream stream;
            private Encoding encoding;

            ///////////////////////////////////////////////////////////////////////////////////////////

            private byte[] buffer;
            private bool expandBuffer;
            private bool zeroBuffer;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public Listener(
                string name,       /* in: may be NULL. */
                string path,       /* in */
                Encoding encoding, /* in: may be NULL. */
                int bufferSize,    /* in: may be zero. */
                bool expandBuffer, /* in */
                bool zeroBuffer    /* in */
                )
                : base(name)
            {
                SetupStream(path); /* throw */
                SetupEncoding(encoding);

                /* IGNORED */
                SetupOrExpandBuffer(bufferSize);

                ///////////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: Should the allocated buffer automatically expand
                //       to fit a request size?  If this is false, a brand
                //       new buffer will be allocated each time a request
                //       size cannot be satisfied by the existing buffer.
                //
                this.expandBuffer = expandBuffer;

                //
                // NOTE: Should the existing buffer always be zeroed before
                //       being returned?
                //
                this.zeroBuffer = zeroBuffer;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void SetupStream(
                string path
                )
            {
                CloseStream();

                stream = new FileStream(
                    path, FileMode.Append, FileAccess.Write,
                    FileShare.ReadWrite); /* throw */
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void FlushStream()
            {
                if (stream != null)
                    stream.Flush();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void CloseStream()
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void SetupEncoding(
                Encoding encoding
                )
            {
                if (encoding == null)
                    encoding = DefaultEncoding;

                this.encoding = encoding;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This method uses "strategy #1", use the existing buffer
            //       verbatim if it's already large enough; otherwise, replace
            //       the existing buffer with one of the larger size and then
            //       return it.
            //
            private byte[] SetupOrExpandBuffer(
                int bufferSize
                )
            {
                if (bufferSize <= 0)
                    bufferSize = DefaultBufferSize;

                if ((buffer == null) || (buffer.Length < bufferSize))
                    buffer = new byte[bufferSize];
                else if (zeroBuffer)
                    Array.Clear(buffer, 0, buffer.Length);

                return buffer;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: This method uses "strategy #2", use the existing buffer
            //       verbatim if it's already large enough; otherwise, create
            //       a new buffer of the requested size and return it.
            //
            private byte[] GetOrCreateBuffer(
                int bufferSize
                )
            {
                if (bufferSize <= 0)
                    bufferSize = DefaultBufferSize;

                if ((buffer != null) && (bufferSize <= buffer.Length))
                {
                    if (zeroBuffer)
                        Array.Clear(buffer, 0, buffer.Length);

                    return buffer;
                }

                return new byte[bufferSize];
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: Figure out which buffer management strategy to use and
            //       then return the appropriate buffer.
            //
            private byte[] GetBuffer(
                int bufferSize
                )
            {
                return expandBuffer ?
                    SetupOrExpandBuffer(bufferSize) :
                    GetOrCreateBuffer(bufferSize);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void GetWriteParameters(
                string message,
                ref byte[] buffer,
                ref int offset,
                ref int count
                )
            {
                if (encoding == null)
                    throw new InvalidOperationException();

                if (message == null)
                {
                    buffer = null; offset = 0; count = 0;
                    return;
                }

                int byteCount = encoding.GetByteCount(message);
                byte[] localBuffer = GetBuffer(byteCount);

                encoding.GetBytes(
                    message, 0, message.Length, localBuffer, 0);

                buffer = localBuffer; offset = 0; count = byteCount;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region TraceListener Overrides
            public override void Close()
            {
                CheckDisposed();

                CloseStream();
                base.Close();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public override void Flush()
            {
                CheckDisposed();

                FlushStream();
                base.Flush();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void Write(
                string message
                )
            {
                CheckDisposed();

                if (stream == null)
                    throw new InvalidOperationException();

                byte[] buffer = null; int offset = 0; int count = 0;

                GetWriteParameters(
                    message, ref buffer, ref offset, ref count);

                if ((buffer == null) || (count == 0))
                    return;

                stream.Write(buffer, offset, count);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public /* abstract */ override void WriteLine(
                string message
                )
            {
                CheckDisposed();

                Write(message);
                Write(Environment.NewLine);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(Listener).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            protected override void Dispose(
                bool disposing
                )
            {
                try
                {
                    if (!disposed)
                    {
                        if (disposing)
                        {
                            ////////////////////////////////////
                            // dispose managed resources here...
                            ////////////////////////////////////

                            //
                            // HACK: Remove this object instance from the
                            //       collections of debug listeners to prevent
                            //       ObjectDisposedException from being thrown
                            //       (i.e. during later calls to Debug.Write,
                            //       etc).
                            //
                            try
                            {
                                Debug.Listeners.Remove(this);
                            }
                            catch
                            {
                                //
                                // NOTE: There is nothing much we can do here.
                                //       We cannot even call DebugOps.Complain
                                //       because it could use Debug.WriteLine,
                                //       and that may end up calling into this
                                //       object instance.
                                //
                            }

                            ///////////////////////////////////////////////////////////////////////////

                            //
                            // HACK: Remove this object instance from the
                            //       collections of trace listeners to prevent
                            //       ObjectDisposedException from being thrown
                            //       (i.e. during later calls to Trace.Write,
                            //       etc).
                            //
                            try
                            {
                                Trace.Listeners.Remove(this);
                            }
                            catch
                            {
                                //
                                // NOTE: There is nothing much we can do here.
                                //       We cannot even call DebugOps.Complain
                                //       because it could use Trace.WriteLine,
                                //       and that may end up calling into this
                                //       object instance.
                                //
                            }

                            ///////////////////////////////////////////////////////////////////////////

                            Close();
                        }

                        //////////////////////////////////////
                        // release unmanaged resources here...
                        //////////////////////////////////////
                    }
                }
                finally
                {
                    base.Dispose(disposing);

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~Listener()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Test Class
        [ObjectId("4b0d4629-b42f-4cfa-adb6-5943e098961c")]
        public sealed class Disposable : IDisposable
        {
            #region Public Constructors
            public Disposable()
            {
                id = GlobalState.NextId(); /* EXEMPT */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Properties
            private long id;
            public long Id
            {
                get { CheckDisposed(); return id; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private string name;
            public string Name
            {
                get { CheckDisposed(); return name; }
                set { CheckDisposed(); name = value; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool Disposed
            {
                //
                // NOTE: *WARNING* Do not uncomment the CheckDisposed call here
                //       as that would defeat the purpose of this method and
                //       may interfere with the associated test cases.
                //
                get { /* CheckDisposed(); */ return disposed; }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public bool Disposing
            {
                //
                // NOTE: *WARNING* Do not uncomment the CheckDisposed call here
                //       as that would defeat the purpose of this method and
                //       may interfere with the associated test cases.
                //
                get { /* CheckDisposed(); */ return disposing; }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                //
                // NOTE: *WARNING* Do not uncomment the CheckDisposed call here
                //       as that would defeat the purpose of this method and
                //       may interfere with the associated test cases.
                //
                // CheckDisposed();

                return String.Format(
                    "id = {0}, disposing = {1}, disposed = {2}",
                    id, disposing, disposed);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(Disposable).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private bool disposing;
            private /* protected virtual */ void Dispose(
                bool disposing
                )
            {
                if (!disposed)
                {
                    //
                    // NOTE: Keep track of whether we were disposed via the
                    //       destructor (i.e. most likely via the GC) or
                    //       explicitly via the public Dispose method.
                    //
                    this.disposing = disposing;

                    //
                    // NOTE: This object is now disposed.  The test cases may
                    //       query the property associated with this field to
                    //       discover this fact.
                    //
                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~Disposable()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFunction Test Classes
        #region IFunction Test Class #1
        [ObjectId("a61370f5-215a-41a3-af8f-196fcf8f3cc4")]
        [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NoPopulate)]
        [Arguments(Arity.Unary)]
        [ObjectGroup("test")]
        public sealed class Function : _Functions.Default
        {
            #region Public Constructors
            public Function(
                IFunctionData functionData
                )
                : base(functionData)
            {
                this.Flags |= AttributeOps.GetFunctionFlags(GetType().BaseType) |
                    AttributeOps.GetFunctionFlags(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecuteArgument Members
            public override ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Argument value,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    error = "invalid argument list";
                    return ReturnCode.Error;
                }

                ReturnCode code;

                if (arguments.Count == 2)
                {
                    string text = arguments[1];
                    Result result = null;

                    code = Engine.EvaluateExpression(
                        interpreter, text, ref result);

                    if (code == ReturnCode.Ok)
                        value = StringList.MakeList(text, result);
                    else
                        error = result;
                }
                else
                {
                    error = ScriptOps.WrongNumberOfArguments(
                        this, 1, arguments, "expr");

                    code = ReturnCode.Error;
                }

                return code;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFunction Test Class #2
        [ObjectId("f7051cc9-57b1-4307-b4b6-1216811b9d39")]
        [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.NoPopulate)]
        [Arguments(Arity.Binary)]
        [ObjectGroup("test")]
        public sealed class Function2 : _Functions.Default
        {
            #region Public Constructors
            public Function2(
                IFunctionData functionData
                )
                : base(functionData)
            {
                this.Flags |= AttributeOps.GetFunctionFlags(GetType().BaseType) |
                    AttributeOps.GetFunctionFlags(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecuteArgument Members
            public override ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Argument value,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    error = "invalid argument list";
                    return ReturnCode.Error;
                }

                if (arguments.Count != (this.Arguments + 1)) /* 3 */
                {
                    error = ScriptOps.WrongNumberOfArguments(
                        this, 1, arguments, "bool expr");

                    return ReturnCode.Error;
                }

                ReturnCode code = ReturnCode.Ok;

                bool boolValue = false;

                if (Engine.ToBoolean(
                        arguments[1], interpreter.CultureInfo,
                        ref boolValue, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (boolValue)
                {
                    try
                    {
                        //
                        // NOTE: This is being done purely for expression
                        //       engine testing purposes only.  Normally,
                        //       the containing interpreter should NOT be
                        //       disposed from inside of a custom command
                        //       or function.
                        //
                        if (interpreter != null)
                        {
                            interpreter.Dispose();
                            interpreter = null;
                        }

                        code = ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }

                string text = arguments[2];

                if ((code == ReturnCode.Ok) && !String.IsNullOrEmpty(text))
                {
                    Result result = null;

                    code = Engine.EvaluateExpression(
                        interpreter, text, ref result);

                    if (code == ReturnCode.Ok)
                        value = StringList.MakeList(text, result);
                    else
                        error = result;
                }

                return code;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFunction Test Class #3
        [ObjectId("c06da159-fe1f-4482-85ec-1b4c3bf4d0d0")]
        [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.NoPopulate)]
        [Arguments(Arity.None)]
        [ObjectGroup("test")]
        public sealed class Function3 : _Functions.Default
        {
            #region Public Constructors
            public Function3(
                IFunctionData functionData
                )
                : base(functionData)
            {
                this.Flags |= AttributeOps.GetFunctionFlags(GetType().BaseType) |
                    AttributeOps.GetFunctionFlags(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IExecuteArgument Members
            public override ReturnCode Execute(
                Interpreter interpreter,
                IClientData clientData,
                ArgumentList arguments,
                ref Argument value,
                ref Result error
                )
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }

                if (arguments == null)
                {
                    error = "invalid argument list";
                    return ReturnCode.Error;
                }

                if (arguments.Count < 3)
                {
                    error = ScriptOps.WrongNumberOfArguments(
                        this, 1, arguments, "bool arg ?arg ...?");

                    return ReturnCode.Error;
                }

                bool boolValue = false;

                if (Engine.ToBoolean(
                        arguments[1], interpreter.CultureInfo,
                        ref boolValue, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                ReturnCode code;

                if (boolValue)
                {
                    Result result = null;

                    code = interpreter.Invoke(arguments[2], clientData,
                        ArgumentList.GetRange(arguments, 2), ref result);

                    if (code == ReturnCode.Ok)
                        value = result;
                    else
                        error = result;
                }
                else
                {
                    value = ArgumentList.GetRange(
                        arguments, 1, Index.Invalid, false).ToString();

                    code = ReturnCode.Ok;
                }

                return code;
            }
            #endregion
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Generic List Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("df724c7b-bf7a-4292-8223-c2350b6f3dc2")]
        public sealed class GenericList<T> : List<T>
        {
            #region Public Constructors
            public GenericList()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericList(
                IEnumerable<T> collection
                )
                : base(collection)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericList(
                params T[] elements
                )
                : base(elements)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ToString Methods
            public string ToString(
                string pattern,
                bool noCase
                )
            {
                return GenericOps<T>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), pattern, noCase);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return ToString(null, false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Generic Dictionary Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("51e22e50-4079-48f6-83eb-e48cb49c7ea3")]
        public sealed class GenericDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
            #region Public Constructors
            public GenericDictionary()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericDictionary(
                IEnumerable<TKey> collection
                )
                : this(collection, null)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericDictionary(
                params TKey[] elements
                )
                : this((IEnumerable<TKey>)elements)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public GenericDictionary(
                IEnumerable<TKey> keys,
                IEnumerable<TValue> values
                )
                : this()
            {
                Add(keys, values);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Protected Constructors
#if SERIALIZATION
            private GenericDictionary(
                SerializationInfo info,
                StreamingContext context
                )
                : base(info, context)
            {
                // do nothing.
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Add Methods
            public void Add(
                IEnumerable<TKey> keys,
                IEnumerable<TValue> values
                )
            {
                if (keys == null)
                    return;

                IEnumerator<TKey> keyEnumerator = keys.GetEnumerator();

                IEnumerator<TValue> valueEnumerator = (values != null) ?
                    values.GetEnumerator() : null;

                bool moreValues = (valueEnumerator != null);

                while (keyEnumerator.MoveNext())
                {
                    TKey key = keyEnumerator.Current;
                    TValue value = default(TValue);

                    if (moreValues)
                    {
                        if (valueEnumerator.MoveNext())
                            value = valueEnumerator.Current;
                        else
                            moreValues = false;
                    }

                    this.Add(key, value);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ToString Methods
            public string ToString(
                string pattern,
                bool noCase
                )
            {
                return GenericOps<TKey, TValue>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), pattern, noCase);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return ToString(null, false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Derived List Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("99cda455-3f92-4ec7-bf6e-a406c76ddbf5")]
        public sealed class DerivedList : List<DerivedList>
        {
            #region Public Constructors
            public DerivedList()
                : base()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public DerivedList(
                IEnumerable<DerivedList> collection
                )
                : base(collection)
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public DerivedList(
                params DerivedList[] elements
                )
                : base(elements)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region ToString Methods
            public string ToString(
                string pattern,
                bool noCase
                )
            {
                return GenericOps<DerivedList>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), pattern, noCase);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return ToString(null, false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Remote Authorizer Class (Conditional)
#if NETWORK && REMOTING
        [ObjectId("008a9889-79c4-4a75-af68-1bbb56a349b3")]
        public sealed class RemoteAuthorizer : IAuthorizeRemotingConnection
        {
            #region IAuthorizeRemotingConnection Members
            public bool IsConnectingEndPointAuthorized(EndPoint endPoint)
            {
                IPEndPoint ipEndPoint = endPoint as IPEndPoint;

                if ((ipEndPoint != null) &&
                    (ipEndPoint.Address != null) &&
                    (ipEndPoint.Address.Equals(IPAddress.Loopback)))
                {
                    return true;
                }

                return false;
            }
            ///////////////////////////////////////////////////////////////////////////////////////////////

            public bool IsConnectingIdentityAuthorized(
                IIdentity identity
                )
            {
                return true;
            }
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Remote Object Class
        [ObjectId("2d6c5daa-884a-4ee5-a654-20b70cee463f")]
        public sealed class RemoteObject : MarshalByRefObject, IDisposable
        {
            #region Public Methods (Remotely Accessible)
            public DateTime Now()
            {
                CheckDisposed();

                return TimeOps.GetUtcNow();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////

            public bool Exit()
            {
                CheckDisposed();

#if REMOTING
                try
                {
                    TcpServerChannel channel = ChannelServices.GetChannel(
                        RemotingChannelName) as TcpServerChannel;

                    if (channel != null)
                    {
                        if (RemotingServices.Disconnect(this))
                        {
                            channel.StopListening(null);
                            ChannelServices.UnregisterChannel(channel);

                            return true;
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }
#endif

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////

            public ReturnCode Evaluate(
                string text,
                EngineFlags engineFlags,
                SubstitutionFlags substitutionFlags,
                EventFlags eventFlags,
                ExpressionFlags expressionFlags,
                ref Result result
                )
            {
                CheckDisposed();

                return Engine.EvaluateScript(
                    Interpreter.GetAny(), text,
                    engineFlags, substitutionFlags,
                    eventFlags, expressionFlags,
                    ref result);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable Members
            //
            // HACK: We can never unset the variable that contains the transparent
            //       proxy reference unless this class implements IDisposable because
            //       the transparent proxy "pretends" to implement it, thereby fooling
            //       our TryDisposeObject function.
            //
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////////

            #region IDisposable "Pattern" Members
            private bool disposed;
            private void CheckDisposed() /* throw */
            {
#if THROW_ON_DISPOSED
                if (disposed && Engine.IsThrowOnDisposed(null, false))
                    throw new ObjectDisposedException(typeof(RemoteObject).Name);
#endif
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////

            private /* protected virtual */ void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////////

            #region Destructor
            ~RemoteObject()
            {
                Dispose(false);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Conditional Public Static Methods
#if XML
        public static void TestScriptStreamXml(
            Interpreter interpreter
            )
        {
            string value = String.Format(
                "<?xml version=\"1.0\" encoding=\"utf-8\" ?>{0}" +
                "<blocks xmlns=\"https://eagle.to/2009/schema\">" +
                "  <block id=\"11b0bd2a-bc99-4639-9727-dc38efacaca1\" type=\"automatic\" name=\"block1\">{0}" +
                "    <![CDATA[{0}" +
                "      set seconds [clock seconds]{0}" +
                "      lappend seconds \"clock time = $seconds\"{0}" +
                "      lappend seconds [expr {1}$seconds > 0 ? $seconds : \"negative\"{2}]{0}" +
                "    ]]>{0}" +
                "  </block>{0}" +
                "  <block id=\"07a3570a-49ab-45d7-9e1a-893b2c61edff\" type=\"automatic\" name=\"block2\">{0}" +
                "    return [list {1}this is a test.{2} $seconds [clock seconds]]{0}" +
                "  </block>{0}" +
                "</blocks>{0}" + Characters.EndOfFile + "this is not valid XML",
                Environment.NewLine, Characters.OpenBrace, Characters.CloseBrace);

            ReturnCode code;
            Result result = null;
            string extra = null;

            using (StringReader stringReader = new StringReader(value))
            {
                string text = null;

                code = Engine.ReadScriptStream(
                    interpreter, null, stringReader, 0,
                    Count.Invalid, ref text, ref result);

                if (code == ReturnCode.Ok)
                    result = text;

                extra = stringReader.ReadToEnd();
            }

            IInteractiveHost interactiveHost = interpreter.Host;

            if (interactiveHost != null)
            {
                interactiveHost.WriteResultLine(code, result);
                interactiveHost.WriteResultLine(code, extra);

                code = Engine.EvaluateScript(interpreter, result, ref result);

                interactiveHost.WriteResultLine(code, result);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This will *NOT* build using Mono/XBuild.  Additionally,
        //       this method will not execute correctly on Mono due to a
        //       missing constructor for TcpServerChannel and incomplete
        //       support for .NET Remoting in general.
        //
#if !MONO && !MONO_BUILD && NETWORK && REMOTING
        public static bool TestRemotingHaveChannel()
        {
            TcpServerChannel channel;
            Result error;

            if (TestRemotingTryGetChannel(out channel, out error))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TestRemotingTryGetChannel(
            out TcpServerChannel channel,
            out Result error
            )
        {
            channel = null;
            error = null;

            try
            {
                channel = ChannelServices.GetChannel(
                    RemotingChannelName) as TcpServerChannel;

                return (channel != null);
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestRemoting(
            Interpreter interpreter,
            int port,
            string uri,
            WellKnownObjectMode mode,
            ref Result error
            )
        {
            try
            {
                IDictionary sinkProperties = new Hashtable();

                //
                // NOTE: Allow all types to be used.
                //
                sinkProperties.Add("typeFilterLevel", "Full");

                BinaryServerFormatterSinkProvider sinkProvider =
                    new BinaryServerFormatterSinkProvider(
                        sinkProperties, null);

                IDictionary channelProperties = new Hashtable();

                channelProperties.Add("name", RemotingChannelName);
                channelProperties.Add("port", port);

                //
                // NOTE: Value of "true" causes client hang when
                //       no config is used to setup the channel.
                //
                channelProperties.Add("secure", false);

                TcpServerChannel channel = new TcpServerChannel(
                    channelProperties, sinkProvider, new RemoteAuthorizer());

                ChannelServices.RegisterChannel(channel, false);

                RemotingConfiguration.RegisterWellKnownServiceType(
                    typeof(RemoteObject), uri, mode);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestManagedDelegate(
            Interpreter interpreter,
            ref Type type,
            ref Result error
            )
        {
            return DelegateOps.CreateManagedDelegateType(
                interpreter, null, null, null, null, typeof(void),
                new TypeList(new Type[] { typeof(string) }), ref type,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        private static ReturnCode TestNativeDelegate(
            Interpreter interpreter,
            ref IDelegate @delegate,
            ref Result error
            )
        {
            return DelegateOps.CreateNativeDelegateType(
                interpreter, null, null, null, null, CallingConvention.Cdecl,
                true, (CharSet)0, false, false, typeof(ReturnCode),
                new TypeList(new Type[] {
                    typeof(int), typeof(IntPtr).MakeByRefType(),
                    typeof(Result).MakeByRefType(), typeof(bool)
                }), null, null, null, IntPtr.Zero, ref @delegate, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            #region Dead Code
#if DEAD_CODE
            if (intField == 999)
                throw new ScriptException(ReturnCode.Error, "dispose failure test");
#endif
            #endregion

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(Default).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: Not owned by us.  Do not
                    //       dispose.
                    //
                    TestSetScriptComplainCallback(
                        null, null, false, false);

                    ////////////////////////////////////

                    ThreadOps.CloseEvent(ref @event);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Default()
        {
            Dispose(false);
        }
        #endregion
    }
}
