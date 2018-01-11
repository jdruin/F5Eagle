/*
 * AppDomainOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if ISOLATED_PLUGINS
using System.Collections.Generic;
#endif

#if ISOLATED_PLUGINS || REMOTING
using System.Reflection;
#endif

#if REMOTING
using System.Runtime.Remoting;
using System.Runtime.Remoting.Proxies;
#endif

#if CAS_POLICY
using System.Security.Policy;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

#if ISOLATED_PLUGINS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("592a1298-b491-48ca-b66d-0a5ef3c7a1ae")]
    internal static class AppDomainOps
    {
        #region Private Constants
        //
        // NOTE: Normally, zero would be used here; however, Mono appears
        //       to use zero for the default application domain; therefore,
        //       we must use a negative value here.
        //
        private static readonly int InvalidId = -1;

        ///////////////////////////////////////////////////////////////////////

#if REMOTING
        private const string domainIdFieldName = "_domainID";

        ///////////////////////////////////////////////////////////////////////

        private const BindingFlags domainIdBindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if REMOTING
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static FieldInfo domainIdFieldInfo = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppDomain / Remoting Support Methods
        public static bool IsPrimary()
        {
            return IsPrimary(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPrimary(
            AppDomain appDomain
            ) /* GLOBAL */
        {
            if (appDomain == null)
                return false;

            return IsSame(appDomain, GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTransparentProxy(
            object proxy
            )
        {
#if REMOTING
            return RemotingServices.IsTransparentProxy(proxy);
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static AppDomain GetFrom(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            bool locked = false;

            try
            {
                interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (!interpreter.Disposed)
                        return interpreter.GetAppDomain();
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                interpreter.ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static AppDomain GetCurrent()
        {
            return AppDomain.CurrentDomain;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetCurrentId()
        {
            return GetId(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetPrimaryId() /* GLOBAL */
        {
            return GetId(GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetId(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return InvalidId;

            return appDomain.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetId(
            object @object
            )
        {
#if REMOTING
            if (CommonOps.Runtime.IsMono())
                return InvalidId;

            try
            {
                FieldInfo fieldInfo = null;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (domainIdFieldInfo == null)
                    {
                        domainIdFieldInfo = typeof(RealProxy).GetField(
                            domainIdFieldName, domainIdBindingFlags);
                    }

                    fieldInfo = domainIdFieldInfo;
                }

                if (fieldInfo != null)
                {
                    RealProxy realProxy = RemotingServices.GetRealProxy(
                        @object);

                    if (realProxy != null)
                        return (int)fieldInfo.GetValue(realProxy);
                }
            }
            catch
            {
                // do nothing.
            }
#endif

            return InvalidId;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this just use the IsTransparentProxy method instead?
        //
        public static bool IsCross(
            Interpreter interpreter1,
            Interpreter interpreter2
            )
        {
            AppDomain interpreterAppDomain1 = (interpreter1 != null) ?
                GetFrom(interpreter1) : null;

            AppDomain interpreterAppDomain2 = (interpreter2 != null) ?
                GetFrom(interpreter2) : null;

            if (!IsSame(interpreterAppDomain1, interpreterAppDomain2))
                return true;

            if (!IsSameId(interpreter1, interpreter2))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCross(
            IPluginData pluginData
            )
        {
#if ISOLATED_PLUGINS
            if (IsIsolated(pluginData))
                return true;
#endif

            return IsCrossNoIsolated(pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this just use the IsTransparentProxy method instead?
        //
        public static bool IsCrossNoIsolated(
            IPluginData pluginData
            )
        {
            AppDomain pluginAppDomain = (pluginData != null) ?
                pluginData.AppDomain : null;

            AppDomain currentAppDomain = GetCurrent();

            if (!IsSame(pluginAppDomain, currentAppDomain))
                return true;

            if (!IsSameId(pluginData, currentAppDomain))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCross(
            Interpreter interpreter,
            IPluginData pluginData
            )
        {
#if ISOLATED_PLUGINS
            if (IsIsolated(pluginData))
                return true;
#endif

            return IsCrossNoIsolated(interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this just use the IsTransparentProxy method instead?
        //
        public static bool IsCrossNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData
            )
        {
            AppDomain interpreterAppDomain;

            if (interpreter != null)
            {
                //
                // NOTE: If the interpreter is not a master interpreter
                //       and it is running in a non-default application
                //       domain, it MUST be considered as cross-domain.
                //       The master interpreter may call [interp eval]
                //       on it, and that result could be of a type from
                //       an assembly that has not been (and cannot be)
                //       loaded into the master interpreter application
                //       domain.
                //
                if (!interpreter.IsMasterInterpreter() &&
                    !IsCurrentDefault())
                {
                    return true;
                }

                interpreterAppDomain = GetFrom(interpreter);
            }
            else
            {
                interpreterAppDomain = null;
            }

            AppDomain pluginAppDomain = (pluginData != null) ?
                pluginData.AppDomain : null;

            if (!IsSame(interpreterAppDomain, pluginAppDomain))
                return true;

            if (!IsSameId(interpreter, pluginData))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryDefault() /* GLOBAL */
        {
            return IsDefault(GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCurrentDefault()
        {
            return IsDefault(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDefault(
            AppDomain appDomain
            )
        {
            return ((appDomain != null) && appDomain.IsDefaultAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCurrent(
            AppDomain appDomain
            )
        {
            return IsSame(appDomain, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSame(
            AppDomain appDomain1,
            AppDomain appDomain2
            )
        {
            if ((appDomain1 == null) && (appDomain2 == null))
                return true;
            else if ((appDomain1 == null) || (appDomain2 == null))
                return false;
            else
                return appDomain1.Id == appDomain2.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSame(
            Interpreter interpreter
            )
        {
            return IsSame(interpreter, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSame(
            Interpreter interpreter,
            AppDomain appDomain
            )
        {
            AppDomain localAppDomain = GetFrom(interpreter);

            if (!IsSame(localAppDomain, appDomain))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSame(
            IPluginData pluginData1,
            IPluginData pluginData2
            )
        {
            return IsSameId(pluginData1, pluginData2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameId(
            object @object,
            AppDomain appDomain
            )
        {
            //
            // NOTE: Grab the application domain ID for the object, if
            //       it is a proxy; otherwise, this will be "invalid",
            //       which is fine.
            //
            int id = GetId(@object);

            //
            // NOTE: If the object is NOT a proxy and the application
            //       domain is the current one, then the application
            //       domain IDs are considered to be "matching".
            //
            if ((id == InvalidId) && IsCurrent(appDomain))
                return true;

            //
            // NOTE: Otherwise, the application domain may be invalid
            //       -OR- not the current one -OR- the plugin may be
            //       a proxy.  Fallback to default handling.
            //
            return (id == GetId(appDomain));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameId(
            object object1,
            object object2
            )
        {
            //
            // NOTE: Grab the application domain IDs for the objects, if
            //       they are proxies; otherwise, they will be "invalid",
            //       which is fine.
            //
            return (GetId(object1) == GetId(object2));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCurrentFinalizing()
        {
#if NATIVE_PACKAGE
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.EagleClrStopping))
            {
                return true;
            }
#endif

            return IsFinalizing(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsFinalizing(
            AppDomain appDomain
            )
        {
            return ((appDomain != null) && appDomain.IsFinalizingForUnload());
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        public static bool IsIsolated(
            IPluginData pluginData
            )
        {
            return (pluginData != null) &&
                FlagOps.HasFlags(pluginData.Flags, PluginFlags.Isolated, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionFlags GetEnumOptionFlags(
            TypeCode typeCode,
            bool strict
            )
        {
            switch (typeCode)
            {
                case TypeCode.SByte:
                    {
                        return OptionFlags.MustBeSignedByte;
                    }
                case TypeCode.Byte:
                    {
                        return OptionFlags.MustBeByte;
                    }
                case TypeCode.Int16:
                    {
                        return OptionFlags.MustBeNarrowInteger;
                    }
                case TypeCode.UInt16:
                    {
                        return OptionFlags.MustBeUnsignedNarrowInteger;
                    }
                case TypeCode.Int32:
                    {
                        return OptionFlags.MustBeInteger;
                    }
                case TypeCode.UInt32:
                    {
                        return OptionFlags.MustBeUnsignedInteger;
                    }
                case TypeCode.Int64:
                    {
                        return OptionFlags.MustBeWideInteger;
                    }
                case TypeCode.UInt64:
                    {
                        return OptionFlags.MustBeUnsignedWideInteger;
                    }
                default:
                    {
                        return strict ? OptionFlags.None :
                            OptionFlags.MustBeUnsignedWideInteger;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is a horrible hack to workaround the issue with not being
        //       able to use plugin enumerated types from the "primary"
        //       application domain when the plugin has been loaded in isolated
        //       mode.
        //
        public static ReturnCode FixupOptions(
            IPluginData pluginData,
            OptionDictionary options,
            bool strict,
            ref Result error
            )
        {
            if (pluginData == null)
            {
                if (strict)
                {
                    error = "invalid plugin data";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            if (options == null)
            {
                if (strict)
                {
                    error = "invalid options";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            if (!IsIsolated(pluginData))
                return ReturnCode.Ok;

            Assembly assembly = pluginData.Assembly;

            foreach (KeyValuePair<string, IOption> pair in options)
            {
                IOption option = pair.Value;

                if (option == null)
                    continue;

                //
                // HACK: Skip options that do not have enumerated types.
                //       For now, these are the only options we really have to
                //       worry about because they are the only ones that can
                //       directly refer to user-defined types [of any kind].
                //
                if (!option.HasFlags(OptionFlags.MustBeEnum, true))
                    continue;

                //
                // NOTE: Grab the enumerated (?) type and figure out if it
                //       came from the plugin assembly.  If not, ignore it and
                //       continue.
                //
                Type type = option.Type;

                if ((type == null) || !type.IsEnum ||
                    !Object.ReferenceEquals(type.Assembly, assembly))
                {
                    continue;
                }

                //
                // NOTE: Get the current value of the option.
                //
                object oldValue = option.InnerValue;
                TypeCode typeCode = TypeCode.Empty;

                //
                // NOTE: Attempt to get the new value for the integral type for
                //       the enumeration value of this option, if any.  We must
                //       do this even if the original value is null because we
                //       must have the type code to properly reset the option
                //       flags.
                //
                object newValue = EnumOps.ConvertToTypeCodeValue(
                    type, (oldValue != null) ? oldValue : 0, ref typeCode,
                    ref error);

                if (newValue == null)
                    return ReturnCode.Error;

                //
                // NOTE: Get the option flags required for the integral type.
                //
                OptionFlags flags = GetEnumOptionFlags(typeCode, strict);

                if (flags == OptionFlags.None)
                {
                    error = String.Format(
                        "unsupported type code for enumerated type \"{0}\"",
                        type);

                    return ReturnCode.Error;
                }

                //
                // NOTE: Special handling for "flags" enumerations here.
                //
                if (EnumOps.IsFlagsEnum(type))
                {
                    //
                    // HACK: Substitute our placeholder flags enumerated type.
                    //       It does not know about the textual values provided
                    //       by the actual enumerated type; however, at least
                    //       they can use the custom flags enumeration handling
                    //       (i.e. the "+" and "-" operators, etc).
                    //
                    option.Type = typeof(StubFlagsEnum);
                }
                else
                {
                    //
                    // NOTE: Remove the MustBeEnum flag for this option and add
                    //       the flag(s) needed for its integral type.
                    //
                    option.Flags &= ~OptionFlags.MustBeEnum;
                    option.Flags |= flags;

                    //
                    // NOTE: Clear the type for the option.  The type property
                    //       is only meaningful for enumeration-based options
                    //       and we are converting this option to use some kind
                    //       of integral type.
                    //
                    option.Type = null;
                }

                //
                // NOTE: If necessary, set the new [default] value for this
                //       option to the one we converted to an integral type
                //       value above.  If the old (original) value was null, we
                //       just discard the new value which will be zero anyhow.
                //
                option.Value = (oldValue != null) ? new Variant(newValue) : null;
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetOrCreate(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
#if CAS_POLICY
            Evidence evidence,
#endif
            IClientData clientData,
            bool isolated,
            bool useBasePath,
            ref AppDomain appDomain,
            ref Result error
            )
        {
            //
            // NOTE: Use an isolated application domain or the current one?
            //
            if (isolated)
            {
#if ISOLATED_PLUGINS
                //
                // BUGBUG: This feature does not currently work due to
                //         cross-domain marshalling issues.
                //
                return Create(
                    interpreter, friendlyName, baseDirectory, packagePath,
#if CAS_POLICY
                    evidence,
#endif
                    clientData, useBasePath, ref appDomain, ref error);
#else
                error = "not implemented";
#endif
            }
            else if (interpreter != null)
            {
                //
                // NOTE: Get the application domain configured for this
                //       interpreter.
                //
                appDomain = GetFrom(interpreter);

                if (appDomain != null)
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid application domain";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_INTERPRETERS
        public static ReturnCode CreateForTest(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
            IClientData clientData, /* NOT USED */
            ref AppDomain appDomain,
            ref Result error
            )
        {
            return Create(
                interpreter, friendlyName, baseDirectory, packagePath,
#if CAS_POLICY
                null,
#endif
                clientData, true, ref appDomain, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        private static string GetBasePath(
            Interpreter interpreter, /* OPTIONAL */
            string packagePath,
            ref Result error
            )
        {
            //
            // NOTE: Fetch the raw base directory for the currently executing
            //       application binary.  It is now possible to override the
            //       value used here via the environment.
            //
            string path0 = AssemblyOps.GetAnchorPath();

            if (path0 == null)
                path0 = GlobalState.GetRawBinaryBasePath();

            //
            // NOTE: First, try to use the effective path to the core library
            //       assembly.  This is used to verify that this candidate
            //       application domain base path contains the core library
            //       assembly somewhere underneath it.
            //
            string path1 = GetAssemblyPath();

            if (PathOps.IsUnderPath(interpreter, path1, path0))
            {
                if ((packagePath == null) ||
                    PathOps.IsUnderPath(interpreter, packagePath, path0))
                {
                    return path0;
                }
            }

            //
            // NOTE: Second, try to use the raw base path for the assembly.
            //       This is used to verify that this candidate application
            //       domain base path contains the core library assembly
            //       somewhere underneath it.
            //
            string path2 = GlobalState.GetRawBasePath();

            if (PathOps.IsUnderPath(interpreter, path1, path2))
            {
                if ((packagePath == null) ||
                    PathOps.IsUnderPath(interpreter, packagePath, path2))
                {
                    return path2;
                }
            }

            //
            // NOTE: At this point, we have failed to figure out a base path
            //       for the application domain to be created that actually
            //       contains the core library assembly.
            //
            error = String.Format(
                "cannot determine usable base path for the new application " +
                "domain for interpreter {0}, with the raw binary base path " +
                "{1}, assembly path {2}, and raw base path {3}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.DisplayPath(path0), FormatOps.DisplayPath(path1),
                FormatOps.DisplayPath(path2));

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetAssemblyPath()
        {
            return GlobalState.GetAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddInfo(
            AppDomainSetup appDomainSetup,
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            if (appDomainSetup != null)
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);

                if (empty || (appDomainSetup.ApplicationBase != null))
                {
                    list.Add("ApplicationBase",
                        appDomainSetup.ApplicationBase);
                }

                if (empty || (appDomainSetup.PrivateBinPath != null))
                {
                    list.Add("PrivateBinPath",
                        appDomainSetup.PrivateBinPath);
                }
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DumpSetup(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
            bool useBasePath,
            AppDomainSetup appDomainSetup
            )
        {
            StringPairList list = new StringPairList();

            AddInfo(appDomainSetup, list, DetailFlags.DebugTrace);

            TraceOps.DebugTrace(String.Format(
                "DumpSetup: interpreter = {0}, friendlyName = {1}, " +
                "baseDirectory = {2}, packagePath = {3}, " +
                "useBasePath = {4}, appDomainSetup = {5}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(friendlyName),
                FormatOps.WrapOrNull(baseDirectory),
                FormatOps.WrapOrNull(packagePath),
                useBasePath, list), typeof(AppDomainOps).Name,
                TracePriority.SecurityDebug);
        }

        ///////////////////////////////////////////////////////////////////////

        private static AppDomainSetup CreateSetup(
            Interpreter interpreter, /* OPTIONAL */
            string baseDirectory,
            string packagePath,
            bool useBasePath,
            ref Result error
            )
        {
            string basePath = baseDirectory;

            if (useBasePath && (basePath == null) && (interpreter != null))
                basePath = interpreter.PluginBaseDirectory;

            Result localError = null;

            if (useBasePath && (basePath == null))
            {
                basePath = GetBasePath(
                    interpreter, packagePath, ref localError);
            }

            if (!useBasePath || (basePath != null))
            {
                //
                // NOTE: Check if the package path is located under the base
                //       path.
                //
                bool packageUnderBasePath = (packagePath != null) ?
                    PathOps.IsUnderPath(interpreter, packagePath,
                        basePath) : false;

                //
                // NOTE: Verify that the package path is either usable or
                //       superfluous.
                //
                if ((packagePath == null) ||
                    !useBasePath || packageUnderBasePath)
                {
                    //
                    // NOTE: Grab the full path for the Eagle core library
                    //       assembly.
                    //
                    string assemblyPath = GetAssemblyPath();

                    //
                    // NOTE: Check if the assembly path is located under
                    //       the base path.
                    //
                    bool assemblyUnderBasePath = (assemblyPath != null) ?
                        PathOps.IsUnderPath(interpreter, assemblyPath,
                            basePath) : false;

                    //
                    // NOTE: Verify that the assembly path is either usable
                    //       or superfluous.
                    //
                    if ((assemblyPath == null) ||
                        !useBasePath || assemblyUnderBasePath)
                    {
                        AppDomainSetup appDomainSetup = new AppDomainSetup();

                        //
                        // NOTE: Use the base directory of the Eagle install
                        //       as the base directory for the new isolated
                        //       application domain.
                        //
                        appDomainSetup.ApplicationBase = useBasePath ?
                            basePath : (packagePath != null) ?
                                packagePath : assemblyPath;

                        //
                        // NOTE: If we are using the base path of the Eagle
                        //       core library assembly, then we need to modify
                        //       the private binary path so that it includes
                        //       both the directory containing that assembly
                        //       and the directory containing the package;
                        //       otherwise, we can simply skip this step.
                        //
                        if (useBasePath)
                        {
                            //
                            // TODO: May need to add more options here.
                            //
                            string relativeAssemblyPath =
                                (assemblyPath != null) && assemblyUnderBasePath ?
                                    assemblyPath.Remove(0, basePath.Length).Trim(
                                        PathOps.DirectoryChars) : null;

                            string privateBinPath = relativeAssemblyPath;

                            string relativePackagePath =
                                (packagePath != null) && packageUnderBasePath ?
                                    packagePath.Remove(0, basePath.Length).Trim(
                                        PathOps.DirectoryChars) : null;

                            if (!String.IsNullOrEmpty(relativePackagePath))
                            {
                                if (!String.IsNullOrEmpty(privateBinPath))
                                    privateBinPath += Characters.SemiColon;

                                privateBinPath += relativePackagePath;
                            }

                            appDomainSetup.PrivateBinPath = privateBinPath;
                        }

                        return appDomainSetup;
                    }
                    else
                    {
                        error = "assembly path is not under base path";
                    }
                }
                else
                {
                    error = "package path is not under base path";
                }
            }
            else if (localError == null)
            {
                error = "invalid base path";
            }
            else
            {
                error = localError;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Create(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
#if CAS_POLICY
            Evidence evidence,
#endif
            IClientData clientData, /* NOT USED */
            bool useBasePath,
            ref AppDomain appDomain,
            ref Result error
            )
        {
            //
            // NOTE: *WARNING* Empty application domain names are allowed,
            //       please do not change this to "!String.IsNullOrEmpty".
            //
            if (friendlyName != null)
            {
                try
                {
                    AppDomainSetup appDomainSetup = CreateSetup(
                        interpreter, baseDirectory, packagePath,
                        useBasePath, ref error);

                    if (appDomainSetup != null)
                    {
                        DumpSetup(
                            interpreter, friendlyName, baseDirectory,
                            packagePath, useBasePath, appDomainSetup);

                        appDomain = AppDomain.CreateDomain(
                            friendlyName,
#if CAS_POLICY
                            evidence,
#else
                            null,
#endif
                            appDomainSetup);

                        return ReturnCode.Ok;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid friendly name";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Unload(
            AppDomain appDomain,
            IClientData clientData, /* NOT USED */
            ref Result error
            )
        {
            if (appDomain != null)
            {
                try
                {
                    AppDomain.Unload(appDomain); /* throw */

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid application domain";
            }

            //
            // NOTE: We do not really expect this method to fail;
            //       therefore, output a complaint about it.
            //
            DebugOps.Complain(ReturnCode.Error, error);

            return ReturnCode.Error;
        }
#endif
        #endregion
    }
}
