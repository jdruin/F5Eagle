/*
 * Utility.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this class from within the Eagle core library itself.
// Instead, the various internal methods used by this class should be called
// directly.  This class is intended only for use by third-party plugins and
// applications.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

#if NETWORK
using System.Collections.Specialized;
#endif

#if DATA
using System.Data;
#endif

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

#if SHELL
using System.Threading;
#endif

#if XML
using System.Xml;
#endif

#if XML && SERIALIZATION
using System.Xml.Serialization;
#endif

#if WINFORMS
using System.Windows.Forms;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using _StringDictionary = Eagle._Containers.Public.StringDictionary;

namespace Eagle._Components.Public
{
    [ObjectId("702cb2b3-5e60-4f90-b5af-df09c236ef51")]
    public static class Utility /* FOR EXTERNAL USE ONLY */
    {
        #region External Use Only Helper Methods
        public static bool[] GetGreatestMaxKeySizeAndLeastMinBlockSize(
            SymmetricAlgorithm algorithm,
            ref int keySize,
            ref int blockSize
            )
        {
            return RuntimeOps.GetGreatestMaxKeySizeAndLeastMinBlockSize(
                algorithm, ref keySize, ref blockSize);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: The returned DateTime value may be virtualized (i.e. it may
        //          not reflect the actual current date and time).
        //
        public static DateTime GetNow()
        {
            return TimeOps.GetNow();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: The returned DateTime value may be virtualized (i.e. it may
        //          not reflect the actual current date and time).
        //
        public static DateTime GetUtcNow()
        {
            return TimeOps.GetUtcNow();
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        public static bool LooksLikeXmlDocument(
            string text
            )
        {
            return XmlOps.LooksLikeDocument(text);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SelectRandomArrayValue(
            Interpreter interpreter,
            Array array,
            ref object value,
            ref Result error
            )
        {
            return ArrayOps.SelectRandomValue(
                interpreter, array, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractDataFromComments(
            ref string value,
            ref Result error
            )
        {
            return StringOps.ExtractDataFromComments(ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetScriptPath(
            Interpreter interpreter,
            bool directoryOnly,
            ref string path,
            ref Result error
            )
        {
            return ScriptOps.GetScriptPath(
                interpreter, directoryOnly, ref path, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        public static StringList GetInteractiveCommandNames(
            Interpreter interpreter,
            string pattern,
            bool noCase
            )
        {
            return HelpOps.GetInteractiveCommandNames(
                interpreter, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string SearchForPath(
            Interpreter interpreter,
            string path,
            FileSearchFlags flags
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.Search(interpreter, path, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringPair GetInteractiveCommandHelpItem(
            Interpreter interpreter,
            string name
            )
        {
            return HelpOps.GetInteractiveCommandHelpItem(interpreter, name);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static Thread CreateShellMainThread(
            IEnumerable<string> args,
            bool start
            )
        {
            return ShellOps.CreateShellMainThread(args, start);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Thread CreateInteractiveLoopThread(
            Interpreter interpreter,
            InteractiveLoopData loopData,
            bool start,
            ref Result error
            )
        {
            return ShellOps.CreateInteractiveLoopThread(
                interpreter, loopData, start, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode StopInteractiveLoopThread(
            Thread thread,
            Interpreter interpreter,
            bool force,
            ref Result error
            )
        {
            return ShellOps.StopInteractiveLoopThread(
                thread, interpreter, force, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string BuildCommandLine(
            IEnumerable<string> args,
            bool quoteAll
            )
        {
            return RuntimeOps.BuildCommandLine(args, quoteAll);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string PopFirstArgument(
            ref IList<string> args
            )
        {
            return GenericOps<string>.PopFirstArgument(ref args);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string PopLastArgument(
            ref IList<string> args
            )
        {
            return GenericOps<string>.PopLastArgument(ref args);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MatchSwitch(
            string text,
            string @switch
            )
        {
            return StringOps.MatchSwitch(text, @switch);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            BreakpointType flags,
            BreakpointType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CommandFlags flags,
            CommandFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CreateFlags flags,
            CreateFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ExecutionPolicy flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HostFlags flags,
            HostFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        public static bool HasFlags(
            NotifyFlags flags,
            NotifyFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            NotifyType flags,
            NotifyType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            OperatorFlags flags,
            OperatorFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PluginFlags flags,
            PluginFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PolicyFlags flags,
            PolicyFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ScriptFlags flags,
            ScriptFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            UriFlags flags,
            UriFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashScriptFile(
            Interpreter interpreter,
            string fileName,
            bool noRemote,
            ref Result error
            )
        {
            return RuntimeOps.HashScriptFile(
                interpreter, fileName, noRemote, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldTraceToHost(
            Interpreter interpreter
            ) /* SAFE-ON-DISPOSE */
        {
            return DebugOps.SafeGetTraceToHost(interpreter, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string WrapHandle(
            Interpreter interpreter,
            object value
            )
        {
            return HandleOps.Wrap(interpreter, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object Identity(
            object arg
            )
        {
            return HandleOps.Identity(arg);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type TypeIdentity(
            Type arg
            )
        {
            return HandleOps.TypeIdentity(arg);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly FindAssemblyInAppDomain(
            AppDomain appDomain,
            string name,
            Version version,
            byte[] publicKeyToken,
            ref Result error
            )
        {
            return AssemblyOps.FindInAppDomain(
                appDomain, name, version, publicKeyToken, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameAssemblyName(
            AssemblyName assemblyName1,
            AssemblyName assemblyName2
            )
        {
            return AssemblyOps.IsSameAssemblyName(
                assemblyName1, assemblyName2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static InterpreterDictionary GetInterpreters()
        {
            return GlobalState.GetInterpreters();
        }

        ///////////////////////////////////////////////////////////////////////

        public static AssemblyName GetPackageAssemblyName()
        {
            return GlobalState.GetAssemblyName();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageName(
            PackageType packageType,
            bool noCase
            )
        {
            return GlobalState.GetPackageName(packageType, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageName(
            PackageType packageType,
            string prefix,
            string suffix,
            bool noCase
            )
        {
            return GlobalState.GetPackageName(
                packageType, prefix, suffix, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetPackageVersion()
        {
            return GlobalState.GetPackageVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackagePath(
            Assembly assembly,
            string name,
            Version version,
            bool noMaster,
            bool root,
            bool noBinary
            )
        {
            return GlobalState.GetPackagePath(
                assembly, name, version, noMaster, root, noBinary);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetBasePath(
            string basePath,
            bool refresh
            )
        {
            GlobalState.SetBasePath(basePath, refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetLibraryPath(
            string libraryPath,
            bool refresh
            )
        {
            GlobalState.SetLibraryPath(libraryPath, refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ScriptTypeToFileName(
            string type,
            PackageType packageType,
            bool fileNameOnly,
            bool strict
            )
        {
            return FormatOps.ScriptTypeToFileName(
                type, packageType, fileNameOnly, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetVariableUndefined(
            IVariable variable,
            bool undefined
            )
        {
            return EntityOps.SetUndefined(variable, undefined);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetVariableDirty(
            IVariable variable,
            bool dirty
            )
        {
            return EntityOps.SetDirty(variable, dirty);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SignalVariableDirty(
            IVariable variable,
            string index
            )
        {
            return EntityOps.SignalDirty(variable, index);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatBreakpoint(
            BreakpointType breakpointType
            )
        {
            return FormatOps.Breakpoint(breakpointType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatComplaint(
            long id,
            ReturnCode code,
            Result result,
            string stackTrace
            )
        {
            return FormatOps.Complaint(id, code, result, stackTrace);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatErrorVariableName(
            string varName,
            string varIndex
            )
        {
            return FormatOps.ErrorVariableName(varName, varIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetBasePath()
        {
            return GlobalState.GetBasePath();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetExternalsPath()
        {
            return GlobalState.GetExternalsPath();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetBinaryPath()
        {
            return GlobalState.GetBinaryPath();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetExecutableName()
        {
            return PathOps.GetExecutableName();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyPublicKeyToken(
            AssemblyName assemblyName
            )
        {
            return AssemblyOps.GetPublicKeyToken(assemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetAssemblyCertificate(
            Assembly assembly,
            ref X509Certificate certificate,
            ref Result error
            )
        {
            return AssemblyOps.GetCertificate(
                assembly, ref certificate, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetAssemblyCertificate2(
            Assembly assembly,
            bool strict,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            return AssemblyOps.GetCertificate2(
                assembly, strict, ref certificate2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetObjectId(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            object @object
            )
        {
            return AttributeOps.GetObjectId(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static CommandFlags GetCommandFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetCommandFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static CommandFlags GetCommandFlags(
            object @object
            )
        {
            return AttributeOps.GetCommandFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static FunctionFlags GetFunctionFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetFunctionFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static FunctionFlags GetFunctionFlags(
            object @object
            )
        {
            return AttributeOps.GetFunctionFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ProcessorArchitecture GetProcessorArchitecture()
        {
            return PlatformOps.GetProcessorArchitecture();
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetPluginFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetPluginFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetPluginFlags(
            object @object
            )
        {
            return AttributeOps.GetPluginFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static NotifyFlags GetNotifyFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetNotifyFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static NotifyType GetNotifyTypes(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetNotifyTypes(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyConfiguration(
            Assembly assembly
            )
        {
            return AttributeOps.GetAssemblyConfiguration(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTag(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyTag(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyText(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyText(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTitle(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyTitle(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyDescription(
            Assembly assembly
            )
        {
            return AttributeOps.GetAssemblyDescription(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyUri(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri(
            Assembly assembly,
            string name
            )
        {
            return SharedAttributeOps.GetAssemblyUri(assembly, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetAssemblyVersion(
            Assembly assembly
            )
        {
            return AssemblyOps.GetVersion(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetEagleVersion()
        {
            return GlobalState.GetAssemblyVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetEnvironmentVariable(
            string variable
            )
        {
            return CommonOps.Environment.GetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetEnvironmentVariable(
            string variable,
            bool prefixed,
            bool expand
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            if (expand)
                flags |= ConfigurationFlags.Expand;

            return GlobalConfiguration.GetValue(variable, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetEnvironmentVariable(
            string variable,
            string value
            )
        {
            CommonOps.Environment.SetVariable(variable, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetEnvironmentVariable(
            string variable,
            string value,
            bool prefixed
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            GlobalConfiguration.SetValue(variable, value, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void UnsetEnvironmentVariable(
            string variable
            )
        {
            CommonOps.Environment.UnsetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void UnsetEnvironmentVariable(
            string variable,
            bool prefixed
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            GlobalConfiguration.UnsetValue(variable, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ExpandEnvironmentVariables(
            string name
            )
        {
            return CommonOps.Environment.ExpandVariables(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetStringFromObject(
            object @object
            )
        {
            return StringOps.GetStringFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Argument GetArgumentFromObject(
            object @object
            )
        {
            return StringOps.GetArgumentFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result GetResultFromObject(
            object @object
            )
        {
            return StringOps.GetResultFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TraceListener NewDefaultTraceListener(
            bool console
            )
        {
            return DebugOps.NewDefaultTraceListener(console);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            return DebugOps.AddTraceListener(listener, debug);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            return DebugOps.RemoveTraceListener(listener, debug);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWebUri(
            Uri uri,
            ref UriFlags flags,
            ref string host,
            ref Result error
            )
        {
            return PathOps.IsWebUri(uri, ref flags, ref host, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value
            )
        {
            return PathOps.IsRemoteUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value,
            ref Uri uri
            )
        {
            return PathOps.IsRemoteUri(value, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase
            )
        {
            return EnumOps.TryParseEnum(enumType, value, allowInteger, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase,
            ref Result error
            )
        {
            return EnumOps.TryParseEnum(
                enumType, value, allowInteger, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseFlagsEnum(
            Interpreter interpreter,
            Type enumType,
            string oldValue,
            string newValue,
            CultureInfo cultureInfo,
            bool allowInteger,
            bool strict,
            bool noCase,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return EnumOps.TryParseFlagsEnum(
                interpreter, enumType, oldValue, newValue, cultureInfo,
                allowInteger, strict, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFlagsEnum(Type enumType)
        {
            return EnumOps.IsFlagsEnum(enumType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IList<string> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            return GenericOps<string>.ListToEnglish(
                list, separator, prefix, suffix, valuePrefix, valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IList<Uri> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            return GenericOps<Uri>.ListToEnglish(
                list, separator, prefix, suffix, valuePrefix, valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryDisposeObject(
            object @object,
            ref Result error
            )
        {
            return ObjectOps.TryDispose(@object, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryDisposeObject(
            object @object,
            ref bool dispose,
            ref Result error,
            ref Exception exception
            )
        {
            return ObjectOps.TryDispose(
                @object, ref dispose, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeStringId(
            string prefix,
            long id
            )
        {
            return FormatOps.Id(prefix, null, id);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeRelativePath(
            string path,
            bool separator
            )
        {
            return PathOps.MakeRelativePath(path, separator);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ParseHexavigesimal(
            string text,
            ref long value,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(text) &&
                (Parser.ParseHexavigesimal(text, 0, text.Length,
                    ref value) == text.Length))
            {
                return ReturnCode.Ok;
            }

            error = String.Format(
                "expected hexavigesimal wide integer but got \"{0}\"",
                text);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatHexavigesimal(
            ulong value,
            byte width
            )
        {
            return FormatOps.Hexavigesimal(value, width);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBase26(
            string text
            )
        {
            return StringOps.IsBase26(text);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] FromBase26String(
            string value
            )
        {
            return StringOps.FromBase26String(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToBase26String(
            byte[] array,
            Base26FormattingOption options
            )
        {
            return StringOps.ToBase26String(array, options);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBase64(
            string text
            )
        {
            return StringOps.IsBase64(text);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatDelegateMethodName(
            Delegate @delegate,
            bool assembly,
            bool display
            )
        {
            return FormatOps.DelegateMethodName(@delegate, assembly, display);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPackageDirectory(
            string name,
            Version version,
            bool full
            )
        {
            return FormatOps.PackageDirectory(name, version, full);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPluginName(
            string assemblyName,
            string typeName
            )
        {
            return FormatOps.PluginName(assemblyName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPluginAbout(
            IPluginData pluginData,
            bool full
            )
        {
            return FormatOps.PluginAbout(pluginData, full);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatId(
            string prefix,
            string name,
            long id
            )
        {
            return FormatOps.Id(prefix, name, id);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatResult(
            ReturnCode code,
            Result result
            )
        {
            return ResultOps.Format(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatResult(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            return ResultOps.Format(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSuccess(
            ReturnCode code,
            bool exceptions
            )
        {
            return ResultOps.IsSuccess(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CustomOkCode(
            uint value
            )
        {
            return ResultOps.CustomOkCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CustomErrorCode(
            uint value
            )
        {
            return ResultOps.CustomErrorCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode SuccessExitCode()
        {
            return ResultOps.SuccessExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode FailureExitCode()
        {
            return ResultOps.FailureExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode ExceptionExitCode()
        {
            return ResultOps.ExceptionExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExitCodeToReturnCode(
            ExitCode exitCode
            )
        {
            return ResultOps.ExitCodeToReturnCode(exitCode);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode ReturnCodeToExitCode(
            ReturnCode code
            )
        {
            return ResultOps.ReturnCodeToExitCode(code, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode ReturnCodeToExitCode(
            ReturnCode code,
            bool exceptions
            )
        {
            return ResultOps.ReturnCodeToExitCode(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public static uint FlipEndian(
            uint X
            )
        {
            return ConversionOps.FlipEndian(X);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ulong FlipEndian(
            ulong X
            )
        {
            return ConversionOps.FlipEndian(X);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetBytesFromString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            return ArrayOps.GetBytesFromString(
                value, cultureInfo, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToHexadecimalString(
            byte[] array
            )
        {
            return ArrayOps.ToHexadecimalString(array);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        public static ReturnCode Serialize(
            object @object,
            Type type,
            XmlWriter writer,
            XmlSerializerNamespaces serializerNamespaces,
            ref Result error
            )
        {
            return XmlOps.Serialize(
                @object, type, writer, serializerNamespaces, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Deserialize(
            Type type,
            XmlReader reader,
            ref object @object,
            ref Result error
            )
        {
            return XmlOps.Deserialize(type, reader, ref @object, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML
        public static ReturnCode Validate(
            Assembly assembly,
            string resourceName,
            XmlDocument document,
            ref Result error
            )
        {
            return XmlOps.Validate(
                assembly, resourceName, document, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static ObjectOptionType GetOptionType(
            bool raw,
            bool all
            )
        {
            return ObjectOps.GetOptionType(raw, all);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static OptionDictionary GetInvokeOptions(
            ObjectOptionType objectOptionType
            )
        {
            return ObjectOps.GetInvokeOptions(objectOptionType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly LoadAssemblyFromStream(
            Stream stream,
            ref Result error
            )
        {
            return AssemblyOps.LoadFromStream(stream, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadSettingsViaScriptFile(
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in */
            string fileName,                /* in */
            ScriptDataFlags flags,          /* in */
            ref _StringDictionary settings, /* in, out */
            ref Result error                /* out */
            )
        {
            return ScriptOps.LoadSettingsViaFile(
                interpreter, clientData, fileName, flags, ref settings,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateTemporaryScriptFile(
            string text,         /* in */
            Encoding encoding,   /* in: OPTIONAL */
            ref string fileName, /* out */
            ref Result error     /* out */
            )
        {
            return ScriptOps.CreateTemporaryFile(
                text, encoding, ref fileName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,        /* in */
            ObjectFlags? defaultObjectFlags, /* in */
            out ObjectFlags objectFlags,     /* out */
            out string objectName,           /* out */
            out string interpName,           /* out */
            out bool alias,                  /* out */
            out bool aliasRaw,               /* out */
            out bool aliasAll,               /* out */
            out bool aliasReference          /* out */
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, out objectFlags, out objectName,
                out interpName, out alias, out aliasRaw, out aliasAll,
                out aliasReference);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,        /* in */
            ObjectFlags? defaultObjectFlags, /* in */
            out Type returnType,             /* out */
            out ObjectFlags objectFlags,     /* out */
            out string objectName,           /* out */
            out string interpName,           /* out */
            out bool create,                 /* out */
            out bool dispose,                /* out */
            out bool alias,                  /* out */
            out bool aliasRaw,               /* out */
            out bool aliasAll,               /* out */
            out bool aliasReference,         /* out */
            out bool toString                /* out */
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, out returnType, out objectFlags,
                out objectName, out interpName, out create, out dispose,
                out alias, out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,             /* in */
            ObjectFlags? defaultObjectFlags,      /* in */
            ObjectFlags? defaultByRefObjectFlags, /* in */
            out Type returnType,                  /* out */
            out ObjectFlags objectFlags,          /* out */
            out ObjectFlags byRefObjectFlags,     /* out */
            out string objectName,                /* out */
            out string interpName,                /* out */
            out bool create,                      /* out */
            out bool dispose,                     /* out */
            out bool alias,                       /* out */
            out bool aliasRaw,                    /* out */
            out bool aliasAll,                    /* out */
            out bool aliasReference,              /* out */
            out bool toString                     /* out */
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, defaultByRefObjectFlags,
                out returnType, out objectFlags, out byRefObjectFlags,
                out objectName, out interpName, out create, out dispose,
                out alias, out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static OptionDictionary GetFixupReturnValueOptions()
        {
            return ObjectOps.GetFixupReturnValueOptions();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,           /* in */
            Type type,                         /* in */
            ObjectFlags flags,                 /* in */
            OptionDictionary options,          /* in */
            ObjectOptionType objectOptionType, /* in */
            string objectName,                 /* in */
            object value,                      /* in */
            bool alias,                        /* in */
            bool aliasReference,               /* in */
            ref Result result                  /* out */
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, type, flags, options, objectOptionType,
                objectName, value, true, alias, aliasReference,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,           /* in */
            IBinder binder,                    /* in */
            CultureInfo cultureInfo,           /* in */
            Type type,                         /* in */
            ObjectFlags flags,                 /* in */
            OptionDictionary options,          /* in */
            ObjectOptionType objectOptionType, /* in */
            string objectName,                 /* in */
            string interpName,                 /* in */
            object value,                      /* in */
            bool create,                       /* in */
            bool dispose,                      /* in */
            bool alias,                        /* in */
            bool aliasReference,               /* in */
            bool toString,                     /* in */
            ref Result result                  /* out */
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, binder, cultureInfo, type, flags, options,
                objectOptionType, objectName, interpName, value, create,
                dispose, alias, aliasReference, toString, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ThrowFeatureNotSupported(
            IPluginData pluginData,
            string name
            )
        {
            RuntimeOps.ThrowFeatureNotSupported(pluginData, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            _StringDictionary subCommandNames,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.SubCommandPolicy(
                flags, commandType, commandToken, subCommandNames,
                allowed, interpreter, clientData, arguments,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DirectoryPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            string fileName,
            PathDictionary<object> directories,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.DirectoryPolicy(
                flags, commandType, commandToken, fileName,
                directories, allowed, interpreter, clientData,
                arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UriPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Uri uri,
            UriDictionary<object> uris,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.UriPolicy(
                flags, commandType, commandToken, uri, uris,
                allowed, interpreter, clientData, arguments,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CallbackPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            ICallback callback,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CallbackPolicy(
                flags, commandType, commandToken, callback,
                interpreter, clientData, arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ScriptPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Interpreter policyInterpreter,
            string text,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ScriptPolicy(
                flags, commandType, commandToken, policyInterpreter,
                text, interpreter, clientData, arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TypePolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Type objectType,
            TypeList types,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.TypePolicy(
                flags, commandType, commandToken, objectType,
                types, allowed, interpreter, clientData,
                arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            StringList paths,
            string path2
            )
        {
            return IsSameFile(null, paths, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            Interpreter interpreter,
            StringList paths,
            string path2
            )
        {
            foreach (string path1 in paths)
                if (IsSameFile(interpreter, path1, path2))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            string path1,
            string path2
            )
        {
            return IsSameFile(null, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            Interpreter interpreter,
            string path1,
            string path2
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.IsSameFile(interpreter, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NormalizePath(
            string path
            )
        {
            return NormalizePath(null, path);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NormalizePath(
            Interpreter interpreter,
            string path
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.ResolvePath(interpreter, path);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAdministrator()
        {
            return RuntimeOps.IsAdministrator();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsMono()
        {
            return CommonOps.Runtime.IsMono();
        }

        ///////////////////////////////////////////////////////////////////////

#if !MONO
        public static ReturnCode VerifyPath(
            string path,
            FilePermission permissions,
            ref Result error
            )
        {
            return FileOps.VerifyPath(path, permissions, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static int CompareFileNames(
            string fileNameA,
            string fileNameB
            )
        {
            return PathOps.CompareFileNames(fileNameA, fileNameB);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int CompareUserStrings(
            string strA,
            string strB,
            bool noCase
            )
        {
            return String.Compare(strA, strB, noCase ?
                StringOps.UserNoCaseStringComparisonType :
                StringOps.UserStringComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int CompareSystemStrings(
            string strA,
            string strB,
            bool noCase
            )
        {
            return String.Compare(strA, strB, noCase ?
                StringOps.SystemNoCaseStringComparisonType :
                StringOps.SystemStringComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NormalizeLineEndings(
            string text
            )
        {
            return StringOps.NormalizeLineEndings(text);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long DefaultAttributeFlagsKey()
        {
            return AttributeFlags.DefaultKey;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IDictionary<long, string> ParseAttributeFlags(
            string text,
            bool complex,
            bool space,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Parse(
                text, complex, space, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatAttributeFlags(
            IDictionary<long, string> flags,
            bool legacy,
            bool compact,
            bool space,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Format(
                flags, legacy, compact, space, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VerifyAttributeFlags(
            string text,
            bool complex,
            bool space,
            ref Result error
            )
        {
            return AttributeFlags.Verify(text, complex, space, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveAttributeFlags(
            IDictionary<long, string> flags,
            long key,
            string haveFlags,
            bool all,
            bool strict
            )
        {
            return AttributeFlags.Have(flags, key, haveFlags, all, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IDictionary<long, string> ChangeAttributeFlags(
            IDictionary<long, string> flags,
            long key,
            string changeFlags,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Change(
                flags, key, changeFlags, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadValue(
            string adjective,
            string type,
            string value,
            IEnumerable<string> values,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadValue(
                adjective, type, value, values, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadSubCommand(
            Interpreter interpreter,
            string adjective,
            string type,
            string subCommand,
            IEnsemble ensemble,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadSubCommand(
                interpreter, adjective, type, subCommand, ensemble,
                prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result WrongNumberOfArguments(
            IIdentifierBase identifierBase,
            int count,
            ArgumentList arguments,
            string suffix
            )
        {
            return ScriptOps.WrongNumberOfArguments(
                identifierBase, count, arguments, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter,
            IEnsemble ensemble,
            IClientData clientData,
            ArgumentList arguments,
            bool strict,
            bool noCase,
            ref string name,
            ref bool tried,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, ensemble, clientData, arguments, strict, noCase,
                ref name, ref tried, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PrepareStaticPlugin(
            IPlugin plugin,
            ref Result error
            )
        {
            return RuntimeOps.PrepareStaticPlugin(plugin, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndCommand(
            Interpreter interpreter,
            IClientData clientData,
            Type commandType,
            long commandToken,
            ref IPolicyContext policyContext,
            ref bool match,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractPolicyContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndPlugin(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref IPlugin plugin,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractPolicyContextAndPlugin(
                interpreter, clientData, ref policyContext, ref plugin,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndScript(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref IScript script,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractPolicyContextAndScript(
                interpreter, clientData, ref policyContext, ref script,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndFileName(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string fileName,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractPolicyContextAndFileName(
                interpreter, clientData, ref policyContext, ref fileName,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndText(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string text,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractPolicyContextAndText(
                interpreter, clientData, ref policyContext, ref text,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndTextAndBytes(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string text,
            ref ByteList bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractPolicyContextAndTextAndBytes(
                interpreter, clientData, ref policyContext, ref text,
                ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DetectLibraryPath(
            Assembly assembly,
            IClientData clientData,
            DetectFlags detectFlags
            )
        {
            return GlobalState.DetectLibraryPath(
                assembly, clientData, detectFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result CreateSynchronizedResult(
            string name
            )
        {
            return ResultOps.CreateSynchronized(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CleanupSynchronizedResult(
            Result synchronizedResult
            )
        {
            ResultOps.CleanupSynchronized(synchronizedResult);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Wait(
            Interpreter interpreter,
            long microseconds,
            bool timeout,
            bool strict,
            ref Result error
            ) /* SAFE-ON-DISPOSE */
        {
            return EventOps.Wait(
                interpreter, microseconds, timeout, strict, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WaitSynchronizedResult(
            Result synchronizedResult
            )
        {
            return ResultOps.WaitSynchronized(synchronizedResult);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WaitSynchronizedResult(
            Result synchronizedResult,
            int milliseconds
            )
        {
            return ResultOps.WaitSynchronized(
                synchronizedResult, milliseconds);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetSynchronizedResult(
            Result synchronizedResult,
            ReturnCode code,
            Result result
            )
        {
            ResultOps.SetSynchronized(synchronizedResult, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSynchronizedResult(
            Result synchronizedResult,
            ref ReturnCode code,
            ref Result result,
            ref Result error
            )
        {
            return ResultOps.GetSynchronized(
                synchronizedResult, ref code, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static bool IsSoftwareUpdateExclusive()
        {
            return UpdateOps.IsExclusive();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetSoftwareUpdateExclusive(
            bool exclusive,
            ref Result error
            )
        {
            return UpdateOps.SetExclusive(exclusive, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSoftwareUpdateTrusted()
        {
            return UpdateOps.IsTrusted();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetSoftwareUpdateTrusted(
            bool trusted,
            ref Result error
            )
        {
            return UpdateOps.SetTrusted(trusted, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDefaultAppDomain()
        {
            return AppDomainOps.IsCurrentDefault();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomain(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsCross(pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomainNoIsolated(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsCrossNoIsolated(pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomain(
            Interpreter interpreter,
            IPluginData pluginData
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCross(interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomainNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCrossNoIsolated(interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameAppDomain(
            Interpreter interpreter
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsSame(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameAppDomain(
            IPluginData pluginData1,
            IPluginData pluginData2
            )
        {
            return AppDomainOps.IsSame(pluginData1, pluginData2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOriginalLocalPath(
            Assembly assembly
            )
        {
            return AssemblyOps.GetOriginalLocalPath(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOriginalLocalPath(
            AssemblyName assemblyName
            )
        {
            return AssemblyOps.GetOriginalLocalPath(assemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetCurrentThreadId()
        {
            return GlobalState.GetCurrentThreadId(); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode InvokeDelegate(
            Interpreter interpreter,
            Delegate @delegate,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ObjectOps.InvokeDelegate(
                interpreter, @delegate, arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode WriteViaIExecute(
            Interpreter interpreter,
            string commandName, /* NOTE: Almost always null, for [puts]. */
            string channelId,   /* NOTE: Almost always null, for "stdout". */
            string value,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.WriteViaIExecute(
                interpreter, commandName, channelId, value, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatScriptForLog(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return FormatOps.ScriptForLog(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public static string FormatTraceException(
            Exception exception
            )
        {
            return FormatOps.TraceException(exception);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            object value
            )
        {
            return FormatOps.WrapOrNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            object value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, display, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ArrayEquals(
            byte[] array1,
            byte[] array2
            )
        {
            return ArrayOps.Equals(array1, array2);
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public static void DebugTrace(
            string message,
            string category
            )
        {
            TraceOps.DebugTrace(message, category,
                TraceOps.GetTracePriority() | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public static void DebugTrace(
            int threadId,
            string message,
            string category
            )
        {
            TraceOps.DebugTrace(threadId, message, category,
                TraceOps.GetTracePriority() | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugTrace(
            string message,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTrace(message, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugTrace(
            Exception exception,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTrace(exception, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugTrace(
            int threadId,
            string message,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTrace(threadId, message, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Complain(
            Interpreter interpreter,
            ReturnCode code,
            Result result
            ) /* SAFE-ON-DISPOSE */
        {
            DebugOps.Complain(interpreter, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextId()
        {
            return GlobalState.NextId();
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        public static string GetErrorMessage()
        {
            return NativeOps.GetErrorMessage();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetErrorMessage(
            int error
            )
        {
            return NativeOps.GetErrorMessage(error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string GetString(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return RuntimeOps.GetString(
                resourceManager, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetResourceNames(
            IPluginData pluginData,
            ResourceManager resourceManager,
            CultureInfo cultureInfo,
            ref StringList list,
            ref Result error
            )
        {
            return RuntimeOps.GetResourceNames(
                pluginData, resourceManager, cultureInfo, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetTclPatchLevel()
        {
            return TclVars.PatchLevelValue;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetTclVersion()
        {
            return TclVars.VersionValue;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTransparentProxy(
            object proxy
            )
        {
            return AppDomainOps.IsTransparentProxy(proxy);
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        [Obsolete()]
        public static bool IsPluginIsolated(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsIsolated(pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FixupOptions(
            IPluginData pluginData,
            OptionDictionary options,
            bool strict,
            ref Result error
            )
        {
            return AppDomainOps.FixupOptions(
                pluginData, options, strict, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static void BeginWithAutoPath(
            string path,
            bool verbose,
            ref string savedLibPath
            )
        {
            GlobalState.BeginWithAutoPath(path, verbose, ref savedLibPath);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void EndWithAutoPath(
            bool verbose,
            ref string savedLibPath
            )
        {
            GlobalState.EndWithAutoPath(verbose, ref savedLibPath);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method needs to be public because external applications
        //       and plugins may set the environment variables we care about;
        //       however, there is no other way to notify this library about
        //       those changes (other than this method, that is).
        //
        public static void RefreshAutoPathList(
            bool verbose
            )
        {
            GlobalState.RefreshAutoPathList(verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri TryCombineUris(
            Uri baseUri,              /* in */
            string relativeUri,       /* in */
            Encoding encoding,        /* in */
            UriComponents components, /* in */
            UriFormat format,         /* in */
            UriFlags flags,           /* in */
            ref Result error          /* out */
            )
        {
            return PathOps.TryCombineUris(
                baseUri, relativeUri, encoding, components, format,
                flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static ReturnCode DownloadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            return WebOps.DownloadData(
                interpreter, clientData, uri, trusted, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadFile(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string fileName,
            bool trusted,
            ref Result error
            )
        {
            return WebOps.DownloadFile(
                interpreter, clientData, uri, fileName, trusted, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadValues(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            NameValueCollection collection,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            return WebOps.UploadValues(
                interpreter, clientData, uri, method, collection, trusted,
                ref bytes, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ICallback CreateCommandCallback(
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
            return CommandCallback.Create(
                marshalFlags, callbackFlags | CallbackFlags.External,
                objectFlags, byRefArgumentFlags, interpreter,
                clientData, name, arguments, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,
            DbConnectionType dbConnectionType,
            string connectionString,
            string typeFullName,
            string typeName,
            ValueFlags valueFlags,
            ref IDbConnection connection,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return DataOps.CreateDbConnection(
                interpreter, dbConnectionType, connectionString,
                typeFullName, typeName, valueFlags, ref connection,
                ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool IsDebuggerPresent()
        {
            return NativeOps.SafeNativeMethods.IsDebuggerPresent();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CopyAndWrapHost(
            Interpreter interpreter,
            Type type,
            ref IHost host,
            ref Result error
            )
        {
            return HostOps.CopyAndWrap(
                interpreter, type, ref host, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UnwrapAndDisposeHost(
            Interpreter interpreter,
            ref Result error
            )
        {
            return HostOps.UnwrapAndDispose(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUnixOperatingSystem()
        {
            return PlatformOps.IsUnixOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsOperatingSystem()
        {
            return PlatformOps.IsWindowsOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

#if WINFORMS
        public static ReturnCode GetControlHandle( /* hWnd */
            Control control,
            ref IntPtr handle,
            ref Result error
            )
        {
            return WindowOps.GetHandle(control, ref handle, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMenuHandle( /* hMenu */
            Menu menu,
            ref IntPtr handle,
            ref Result error
            )
        {
            return WindowOps.GetHandle(menu, ref handle, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static ReturnCode ZeroMemory(
            IntPtr pMemory,
            uint size,
            ref Result error
            )
        {
            return NativeOps.ZeroMemory(pMemory, size, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        public static int GetHashCode(
            byte[] array
            )
        {
            return ArrayOps.GetHashCode(array);
        }
#endif
        #endregion
    }
}
