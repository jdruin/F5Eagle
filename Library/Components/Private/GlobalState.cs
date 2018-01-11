/*
 * GlobalState.cs --
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
using System.Globalization;
using System.IO;
using System.Reflection;

#if CAS_POLICY
using System.Security.Policy;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;

namespace Eagle._Components.Private
{
    [ObjectId("e8491fec-2fd3-455e-92fd-cf2a84c75e8a")]
    internal static class GlobalState
    {
        #region Private Read-Only Data (Logical Constants)
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Application Domain Data
        private static readonly AppDomain appDomain = AppDomain.CurrentDomain;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string appDomainBaseDirectory =
            (appDomain != null) ? appDomain.BaseDirectory : null;

        ///////////////////////////////////////////////////////////////////////

#if USE_APPDOMAIN_FOR_ID
        //
        // NOTE: Normally, zero would be used here; however, Mono appears
        //       to use zero for the default application domain; therefore,
        //       we must use a negative value here.
        //
        // NOTE: The value used here *MUST* be manually kept in sync with
        //       the value of the AppDomainOps.InvalidId static read-only
        //       field.
        //
        private static readonly int appDomainId = (appDomain != null) ?
            appDomain.Id : -1;

        ///////////////////////////////////////////////////////////////////////

        private static readonly bool isDefaultAppDomain =
            (appDomain != null) ? appDomain.IsDefaultAppDomain() : false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Package Name & Version Data
        private static readonly string DefaultPackageName = "Eagle";
        private static readonly string DefaultPackageNameNoCase = "eagle";

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: This version information should be changed if the major or
        //       minor version of the assembly changes.
        //
        private static readonly int DefaultMajorVersion = 1;
        private static readonly int DefaultMinorVersion = 0;

        ///////////////////////////////////////////////////////////////////////

        private static readonly Version DefaultVersion = GetTwoPartVersion(
            DefaultMajorVersion, DefaultMinorVersion);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Name Data
        //
        // NOTE: This package contains the Eagle [core] script library.  Its
        //       primary contents are script files that are required during
        //       initialization of an interpreter (e.g. the "embed.eagle",
        //       "init.eagle", and "vendor.eagle" files, etc).
        //
        private static readonly string LibraryPackageName = DefaultPackageName;

        //
        // NOTE: This package contains the Eagle test [suite infrastructure].
        //       Its primary contents are script files that are required when
        //       running the Eagle test suite (e.g. the "constraints.eagle",
        //       "prologue.eagle", and "epilogue.eagle" files).  They are also
        //       designed to be used by third-party test suites.
        //
        private static readonly string TestPackageName = "Test";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Name & Version Formatting
        private static readonly string UpdateVersionFormat = "{0}.{1}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string PackageNameFormat = "{0}{1}{2}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry & Executing Assembly Data
        #region Executing Assembly Data
        private static readonly Assembly thisAssembly =
            Assembly.GetExecutingAssembly();

#if CAS_POLICY
        private static readonly Evidence thisAssemblyEvidence =
            (thisAssembly != null) ? thisAssembly.Evidence : null;
#endif

        private static readonly AssemblyName thisAssemblyName =
            (thisAssembly != null) ? thisAssembly.GetName() : null;

        private static readonly string thisAssemblyTitle =
            SharedAttributeOps.GetAssemblyTitle(thisAssembly);

        private static readonly string thisAssemblyLocation =
            (thisAssembly != null) ? thisAssembly.Location : null;

        private static readonly string thisAssemblySimpleName =
            (thisAssemblyName != null) ? thisAssemblyName.Name : null;

        private static readonly string thisAssemblyFullName =
            (thisAssemblyName != null) ? thisAssemblyName.FullName : null;

        private static readonly Version thisAssemblyVersion =
            (thisAssemblyName != null) ? thisAssemblyName.Version : null;

        private static readonly CultureInfo thisAssemblyCultureInfo =
            (thisAssemblyName != null) ? thisAssemblyName.CultureInfo : null;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string thisAssemblyPath =
            AssemblyOps.GetPath(null, thisAssembly);

        private static readonly Uri thisAssemblyUri =
            SharedAttributeOps.GetAssemblyUri(thisAssembly);

        //
        // TODO: Change this if the update URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyUpdateBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyUpdateBaseUri =
            SharedAttributeOps.GetAssemblyUpdateBaseUri(thisAssembly);

        //
        // TODO: Change this if the download URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyDownloadBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyDownloadBaseUri =
            SharedAttributeOps.GetAssemblyDownloadBaseUri(thisAssembly);

        //
        // TODO: Change this if the script URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyScriptBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyScriptBaseUri =
            SharedAttributeOps.GetAssemblyScriptBaseUri(thisAssembly);

        //
        // TODO: Change this if the auxiliary URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyAuxiliaryBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyAuxiliaryBaseUri =
            SharedAttributeOps.GetAssemblyAuxiliaryBaseUri(thisAssembly);

        //
        // TODO: Change this if the XSD schema URI changes.
        //
        private static readonly Uri thisAssemblyNamespaceUri =
            (thisAssemblyUri != null) ?
                new Uri(thisAssemblyUri, "2009/schema") : null;

        ///////////////////////////////////////////////////////////////////////

        #region Package Data
        //
        // NOTE: This is the base package name (e.g. "Eagle").
        //
        private static readonly string packageName = GetPackageName(
            PackageType.Library, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the base package name (e.g. "Eagle") in lower-case.
        //
        private static readonly string packageNameNoCase =
            GetPackageName(PackageType.Library, true);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: The package version *IS* the assembly version.
        //
        private static readonly Version packageVersion =
            thisAssemblyVersion;

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        //
        // NOTE: This is the base package name (e.g. "Eagle") in lower-case
        //       for use on Unix.
        //
        private static readonly string unixPackageName = packageNameNoCase;

        //
        // HACK: The Unix package version *IS* the assembly version.
        //
        private static readonly Version unixPackageVersion =
            thisAssemblyVersion;
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private static readonly string debuggerName =
            String.Format("{0} {1}", packageName, Debugger.Name).Trim();
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry Assembly Data
        private static readonly Assembly entryAssembly = FindEntryAssembly();

        private static readonly AssemblyName entryAssemblyName =
            (entryAssembly != null) ? entryAssembly.GetName() : null;

        private static readonly string entryAssemblyTitle =
            SharedAttributeOps.GetAssemblyTitle(entryAssembly);

        private static readonly string entryAssemblyLocation =
            (entryAssembly != null) ? entryAssembly.Location : null;

        private static readonly Version entryAssemblyVersion =
            (entryAssemblyName != null) ? entryAssemblyName.Version : null;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string entryAssemblyPath =
            AssemblyOps.GetPath(null, entryAssembly);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Binary Executable Data
        private static readonly string binaryPath = PathOps.GetBinaryPath(true);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Management Data
        //
        // BUGFIX: Non-default builds may use a file name other than the
        //         normal "Eagle.dll"; therefore, the base resource name
        //         must match that value, not the package name.
        //
        private static readonly string resourceBaseName =
            thisAssemblySimpleName;
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Read-Only Primary Thread Data (Logical Constants)
        //
        // HACK: This is done dead last (within the read-only data) so that we
        //       can output useful diagnostic messages showing that the global
        //       state has been initialized successfully.
        //
        private static readonly int primaryThreadId = GetCurrentThreadId();

        ///////////////////////////////////////////////////////////////////////

        private static readonly Thread primaryThread =
            SetupPrimaryThread(primaryThreadId);

        ///////////////////////////////////////////////////////////////////////

        private static readonly int primaryManagedThreadId =
            GetCurrentManagedThreadId();

        ///////////////////////////////////////////////////////////////////////

        private static readonly int primaryNativeThreadId =
            GetCurrentNativeThreadId();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Read-Write Data
        #region Diagnostic Data
#if POLICY_TRACE
        //
        // NOTE: When this is non-zero, policy trace diagnostics will always
        //       be written by the engine, regardless of the per-interpreter
        //       settings.
        //
        private static bool policyTrace = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Identity Data
        private static Random random = new Random();

#if RANDOMIZE_ID
        private static long nextId = Math.Abs((random != null) ?
            random.Next() : 0);

#if !SHARED_ID_POOL
        private static long nextComplaintId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        private static long nextInterpreterId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        private static long nextScriptThreadId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif
#else
        private static long nextId = 0;

#if !SHARED_ID_POOL
        private static long nextComplaintId = 0;
#endif

#if !SHARED_ID_POOL
        private static long nextInterpreterId = 0;
#endif

#if !SHARED_ID_POOL
        private static long nextScriptThreadId = 0;
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Data
#if false && NATIVE && WINDOWS
        private static bool getNativeThreadId;
#endif

        ///////////////////////////////////////////////////////////////////////

        [ThreadStatic()] /* ThreadSpecificData */
        private static int currentNativeThreadId;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "Active" / "All" Interpreter Tracking
        private static Interpreter firstInterpreter = null;

        ///////////////////////////////////////////////////////////////////////

        [ThreadStatic()] /* ThreadSpecificData */
        private static InterpreterStackList activeInterpreters;

        private static readonly InterpreterDictionary allInterpreters =
            new InterpreterDictionary();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Path Override Data
        #region Environment Variable Path Overrides
        private static string libraryPath = GlobalConfiguration.GetValue(
            EnvVars.EagleLibrary, ConfigurationFlags.GlobalStateNoPrefix);

        private static StringList autoPathList = StringList.FromString(
            GlobalConfiguration.GetValue(EnvVars.EagleLibPath,
                ConfigurationFlags.GlobalStateNoPrefix));

        private static string tclLibraryPath = GlobalConfiguration.GetValue(
            EnvVars.TclLibrary, ConfigurationFlags.GlobalState);

        private static StringList tclAutoPathList = StringList.FromString(
            GlobalConfiguration.GetValue(EnvVars.TclLibPath,
                ConfigurationFlags.GlobalState));
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Master Base / Library Path Overrides
        //
        // NOTE: Master override for the base path.
        //
        private static string masterBasePath = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Master override for the library path.
        //
        private static string masterLibraryPath = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Master Externals Path Overrides
        //
        // NOTE: Master override for the externals path.
        //
        private static string masterExternalsPath = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Path Data
        private static string assemblyPackageNamePath =
            GetAssemblyPackagePath(packageName, packageVersion);

        private static string assemblyPackageRootPath =
            GetAssemblyPackagePath(null, null);

        ///////////////////////////////////////////////////////////////////////

        private static string rawBinaryBasePackageNamePath =
            GetRawBinaryBasePackagePath(packageName, packageVersion);

        private static string rawBinaryBasePackageRootPath =
            GetRawBinaryBasePackagePath(null, null);

        ///////////////////////////////////////////////////////////////////////

        private static string rawBasePackageNamePath =
            GetRawBasePackagePath(packageName, packageVersion);

        private static string rawBasePackageRootPath =
            GetRawBasePackagePath(null, null);

        ///////////////////////////////////////////////////////////////////////

        private static string packagePeerBinaryPath = GetPackagePath(
            thisAssembly, null, null, false, false, false);

        private static string packagePeerAssemblyPath = GetPackagePath(
            thisAssembly, null, null, false, false, true);

        private static string packageRootPath = GetPackagePath(
            thisAssembly, null, null, false, true, false);

        ///////////////////////////////////////////////////////////////////////

        private static string packageNameBinaryPath = GetPackagePath(
            thisAssembly, packageName, packageVersion, false, false, false);

        private static string packageNameAssemblyPath = GetPackagePath(
            thisAssembly, packageName, packageVersion, false, false, true);

        private static string packageNameRootPath = GetPackagePath(
            thisAssembly, packageName, packageVersion, false, true, false);

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        private static string unixPackageNameLocalPath = GetUnixPackagePath(
            unixPackageName, unixPackageVersion, false, true);

        private static string unixPackageNamePath = GetUnixPackagePath(
            unixPackageName, unixPackageVersion, false, false);
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        private static string tclPackageNamePath = GetPackagePath(
            thisAssembly, TclVars.PackageName, null, false, false, false);

        private static string tclPackageNameRootPath = GetPackagePath(
            thisAssembly, TclVars.PackageName, null, false, true, false);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Master Auto-Path List
        //
        // NOTE: Master auto-path list.  This list is automatically initialized
        //       [once] when necessary; however, it may then be overridden to
        //       influence the behavior of any interpreters created after it
        //       has been changed.
        //
        private static StringList masterAutoPathList = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        private static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ExitLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Identity Methods
#if USE_APPDOMAIN_FOR_ID
        private static long MaybeCombineWithAppDomainId(
            long id,
            bool noComplain
            ) /* THREAD-SAFE, NO-LOCK */
        {
            //
            // NOTE: This handling never applies to the default application
            //       domain (COMPAT: Eagle Beta).
            //
            if (isDefaultAppDomain) /* NO-LOCK, READ-ONLY */
                return id;

            //
            // NOTE: Make sure the application domain identifier is positive
            //       and fits completely within a 32-bit signed integer (which
            //       it must, since it is declared as "int"); otherwise, just
            //       return the original identifier verbatim.
            //
            // HACK: This method knows the application domain identifier will
            //       be used for the top-half of the resulting composite long
            //       integer identifier and this class "guarantees" that no
            //       integer identifiers will be negative; therefore, the top
            //       bit of the application domain identifier cannot be set
            //       (i.e. it cannot be negative).
            //
            if (appDomainId < 0) /* NO-LOCK, READ-ONLY */
            {
                //
                // HACK: This method may not be able to use the DebugOps
                //       methods because they may call into us (e.g. the
                //       Complain method).
                //
                if (!noComplain)
                {
                    DebugOps.Complain(ReturnCode.Error,
                        "application domain identifier is negative");
                }

                return id;
            }

            //
            // NOTE: Make sure the original identifier fits completely within
            //       a 32-bit unsigned integer; otherwise, just return the
            //       original identifier verbatim.
            //
            // HACK: This method knows the original identifier will be used
            //       for the bottom-half of resulting composite long integer
            //       identifier; therefore, any value that can fit within
            //       32-bits is fair game.
            //
            if ((id < 0) || (id > uint.MaxValue))
            {
                //
                // HACK: This method may not be able to use the DebugOps
                //       methods because they may call into us (e.g. the
                //       Complain method).
                //
                if (!noComplain)
                {
                    DebugOps.Complain(ReturnCode.Error, String.Format(
                        "original identifier is negative or greater than {0}",
                        uint.MaxValue));
                }

                return id;
            }

            return ConversionOps.MakeLong(
                appDomainId /* NO-LOCK, READ-ONLY */, id);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static long NextId() /* THREAD-SAFE */
        {
            long result;

            //
            // NOTE: This is our cheap unique Id generator for
            //       the various script visible identifiers
            //       (such as channel names, etc).  This value
            //       is not per-interpreter; therefore, use with
            //       caution.
            //
            result = Interlocked.Increment(ref nextId);

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, false);
#endif

            if (result < 0)
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next global identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return (interpreter != null) ?
                interpreter.NextId() : NextId();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This is used by the DebugOps.Complain subsystem.
        //
        public static long NextComplaintId() /* THREAD-SAFE */
        {
            long result;

#if SHARED_ID_POOL
            result = NextId();
#else
            result = Interlocked.Increment(ref nextComplaintId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, true);
#endif

            //
            // HACK: This method may not be able to use the DebugOps
            //       methods because they may call into us (e.g. the
            //       Complain method).
            //
            // if (result < 0)
            // {
            //     DebugOps.Complain(ReturnCode.Error,
            //         "next complaint identifier is negative");
            // }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextInterpreterId() /* THREAD-SAFE */
        {
            long result;

            //
            // NOTE: Interpreter names should be totally unique within the
            //       application domain (or the process?); therefore, this
            //       must be global.
            //
#if SHARED_ID_POOL
            result = NextId();
#else
            result = Interlocked.Increment(ref nextInterpreterId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, false);
#endif

            if (result < 0)
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next interpreter identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextScriptThreadId() /* THREAD-SAFE */
        {
            long result;

            //
            // NOTE: ScriptThread names should be totally unique within the
            //       application domain (or the process?); therefore, this
            //       must be global.
            //
#if SHARED_ID_POOL
            result = NextId();
#else
            result = Interlocked.Increment(ref nextScriptThreadId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, false);
#endif

            if (result < 0)
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next script thread identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextEventId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: Event names must be totally unique within the application
            //       domain (or the process?); therefore, this must be global.
            //
            return NextId();
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextTypeId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: Type names must be totally unique within the application
            //       domain; therefore, this must be global.
            //
            return NextId();
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextThreadId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: Thread names should be totally unique within the process;
            //       therefore, this must be global.
            //
            return NextId();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Random Number Generation Methods
        public static bool GetRandomBytes(
            byte[] bytes
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (random != null)
                {
                    random.NextBytes(bytes);
                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Global Variable Access Methods
        public static int GetCurrentThreadId() /* THREAD-SAFE */
        {
#if NATIVE_THREAD_ID
            return GetCurrentNativeThreadId();
#else
            return GetCurrentManagedThreadId();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetCurrentSystemThreadId() /* THREAD-SAFE */
        {
#if NATIVE_THREAD_ID
            return GetCurrentNativeThreadId();
#else
            return GetCurrentManagedThreadId();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetCurrentManagedThreadId() /* THREAD-SAFE */
        {
            Thread thread = Thread.CurrentThread;

            if (thread == null)
                return 0;

            return thread.ManagedThreadId;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetCurrentNativeThreadId() /* THREAD-SAFE */
        {
#if false && NATIVE && WINDOWS
            //
            // NOTE: *PERF* Thread-local storage is really slow for storing
            //       values needed in the hot-paths of the context manager;
            //       therefore, avoid it if at all possible.
            //
            if (getNativeThreadId)
                return NativeOps.SafeNativeMethods.GetCurrentThreadId();
#endif

            //
            // HACK: This code currently assumes that a managed thread cannot
            //       have its associated native thread changed.  In order to
            //       avoid calling AppDomain.GetCurrentThreadId() repeatedly,
            //       it caches the returned native thread Id in a thread-local
            //       variable.  It further assumes that accessing this integer
            //       thread-local variable is faster than repeatedly calling
            //       the AppDomain.GetCurrentThreadId() method.  This may not
            //       actually be true, depending on the version of the CLR.
            //
            int threadId = currentNativeThreadId;

            if (threadId == 0)
            {
                threadId = AppDomain.GetCurrentThreadId();
                currentNativeThreadId = threadId;
            }

            return threadId;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Thread SetupPrimaryThread(
            int threadId
            ) /* THREAD-SAFE */
        {
#if false && NATIVE && WINDOWS
            PlatformOps.Initialize();

            getNativeThreadId = PlatformOps.IsWindowsOperatingSystem();

            TraceOps.DebugTrace(threadId, String.Format(
                "SetupPrimaryThread: getNativeThreadId feature {0}.",
                getNativeThreadId ? "enabled" : "disabled"),
                typeof(GlobalState).Name, TracePriority.StartupDebug);
#endif

            Thread thread = Thread.CurrentThread;
            string threadName = FormatOps.DisplayThread(thread);

            TraceOps.DebugTrace(threadId, String.Format(
                "SetupPrimaryThread: library initialized in {0}application " +
                "domain {1} on managed thread with [{2}], next Id {3}, next " +
                "complaint Id {4}, next interpreter Id {5}, and next script " +
                "thread Id {6}.", AppDomainOps.IsCurrentDefault() ? "default " :
                String.Empty, AppDomainOps.GetCurrentId(), threadName, nextId,
                nextComplaintId, nextInterpreterId, nextScriptThreadId),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            return thread;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetPrimaryThreadId() /* THREAD-SAFE */
        {
            return primaryThreadId;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Thread GetPrimaryThread() /* THREAD-SAFE */
        {
            return primaryThread;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetPrimaryManagedThreadId() /* THREAD-SAFE */
        {
            return primaryManagedThreadId;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetPrimaryNativeThreadId() /* THREAD-SAFE */
        {
            return primaryNativeThreadId;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryThread() /* THREAD-SAFE */
        {
            return IsPrimaryThread(GetCurrentThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryManagedThread() /* THREAD-SAFE */
        {
            return IsPrimaryManagedThread(GetCurrentManagedThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryNativeThread() /* THREAD-SAFE */
        {
            return IsPrimaryNativeThread(GetCurrentNativeThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPrimaryThread(
            int threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPrimaryManagedThread(
            int threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryManagedThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPrimaryNativeThread(
            int threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryNativeThreadId());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "Active" / "All" Interpreter Tracking Methods
        #region "First" Interpreter Tracking Methods
        public static bool IsFirstInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((interpreter == null) || (firstInterpreter == null))
                    return false;

                return Object.ReferenceEquals(interpreter, firstInterpreter);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Interpreter GetFirstInterpreter()
        {
            lock (syncRoot)
            {
                return firstInterpreter;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "All" Interpreter Tracking Methods
        public static bool AddInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (interpreter != null)
                {
                    if (firstInterpreter == null)
                        firstInterpreter = interpreter;

                    if (allInterpreters != null)
                    {
                        allInterpreters.Add(interpreter);
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool RemoveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (interpreter != null)
                {
                    if (Object.ReferenceEquals(interpreter, firstInterpreter))
                        firstInterpreter = null;

                    if (allInterpreters != null)
                        return allInterpreters.Remove(interpreter);
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "All" Interpreter Tracking Methods
        public static ReturnCode GetInterpreter( /* NOTE: GetAnyInterpreter */
            LookupFlags lookupFlags,
            ref Interpreter interpreter,
            ref Result error
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (allInterpreters != null)
                {
                    //
                    // NOTE: Grab the first available [and valid?] interpreter.
                    //
                    bool validate = FlagOps.HasFlags(
                        lookupFlags, LookupFlags.Validate, true);

                    foreach (KeyValuePair<string, Interpreter> pair
                            in allInterpreters)
                    {
                        interpreter = pair.Value;

                        if (!validate || (interpreter != null))
                            return ReturnCode.Ok;
                    }

                    error = FlagOps.HasFlags(
                        lookupFlags, LookupFlags.Verbose, true) ?
                        String.Format(
                            "no {0}interpreter found",
                            validate ? "valid " : String.Empty) :
                        "no interpreter found";
                }
                else
                {
                    error = "no interpreters available";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetInterpreter(
            string name,
            LookupFlags lookupFlags,
            ref Interpreter interpreter,
            ref Result error
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (allInterpreters != null)
                {
                    //
                    // NOTE: *WARNING* Empty interpreter names are
                    //       technically allowed, please do not
                    //       change this to "!String.IsNullOrEmpty".
                    //
                    if (name != null)
                    {
                        if (allInterpreters.TryGetValue(name, out interpreter))
                        {
                            if ((interpreter != null) ||
                                !FlagOps.HasFlags(
                                    lookupFlags, LookupFlags.Validate, true))
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = FlagOps.HasFlags(
                                    lookupFlags, LookupFlags.Verbose, true) ?
                                    String.Format(
                                        "invalid interpreter name \"{0}\"",
                                        FormatOps.DisplayName(name)) :
                                    "invalid interpreter name";
                            }
                        }
                        else
                        {
                            error = FlagOps.HasFlags(
                                lookupFlags, LookupFlags.Verbose, true) ?
                                String.Format(
                                    "interpreter \"{0}\" not found",
                                    FormatOps.DisplayName(name)) :
                                "interpreter not found";
                        }
                    }
                    else
                    {
                        error = "invalid interpreter name";
                    }
                }
                else
                {
                    error = "no interpreters available";
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string InterpretersToString(
            string pattern,
            bool noCase
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (allInterpreters != null)
                    return allInterpreters.ToString(pattern, noCase);
                else
                    return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        public static int CountInterpreters()
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                        return allInterpreters.Count;
                }
                else
                {
                    TraceOps.DebugTrace(
                        "CountInterpreters: unable to acquire static lock",
                        typeof(GlobalState).Name, TracePriority.LockError);
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return Count.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        public static InterpreterDictionary GetInterpreters() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                        return (InterpreterDictionary)allInterpreters.Clone();
                }
                else
                {
                    TraceOps.DebugTrace(
                        "GetInterpreters: unable to acquire static lock",
                        typeof(GlobalState).Name, TracePriority.LockError);
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IEnumerable<IInterpreter> FilterInterpreters(
            IEnumerable<IInterpreter> interpreters,
            bool found,
            bool nonPrimary
            ) /* THREAD-SAFE */
        {
            if (interpreters == null)
                return null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (allInterpreters != null)
                {
                    IList<IInterpreter> result = new List<IInterpreter>();

                    foreach (IInterpreter interpreter in interpreters)
                    {
                        Interpreter value = interpreter as Interpreter;

                        if (value == null)
                            continue;

                        if (allInterpreters.ContainsValue(value) == found)
                            result.Add(value);
                        else if (nonPrimary && !value.IsPrimarySystemThread())
                            result.Add(value);
                    }

                    return result;
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "Active" Interpreter Tracking Methods
        public static bool IsActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters == null)
                return false;

            return activeInterpreters.ContainsInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        private static IAnyPair<Interpreter, IClientData> GetActiveInterpreter()
        {
            return GetActiveInterpreter(null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IAnyPair<Interpreter, IClientData> GetActiveInterpreter(
            Type type
            ) /* THREAD-SAFE */
        {
            if ((activeInterpreters != null) && !activeInterpreters.IsEmpty)
            {
                if (type != null)
                {
                    for (int index = 0;
                            index < activeInterpreters.Count;
                            index++)
                    {
                        IAnyPair<Interpreter, IClientData> anyPair =
                            activeInterpreters.Peek(index);

                        if (anyPair == null)
                            continue;

                        IClientData clientData = anyPair.Y;

                        if (clientData == null)
                            continue;

                        if (clientData.GetType() == type)
                            return anyPair;
                    }
                }
                else
                {
                    return activeInterpreters.Peek();
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ActiveInterpretersToString(
            string pattern,
            bool noCase
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters != null)
                return activeInterpreters.ToString(pattern, noCase);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        private static InterpreterStackList GetActiveInterpreters()
        {
            if (activeInterpreters != null)
                return (InterpreterStackList)activeInterpreters.Clone();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IEnumerable<IInterpreter> FilterActiveInterpreters(
            IEnumerable<IInterpreter> interpreters,
            bool found,
            bool nonPrimary
            ) /* THREAD-SAFE */
        {
            if (interpreters == null)
                return null;

            if (activeInterpreters != null)
            {
                IList<IInterpreter> result = new List<IInterpreter>();

                foreach (IInterpreter interpreter in interpreters)
                {
                    Interpreter value = interpreter as Interpreter;

                    if (value == null)
                        continue;

                    if (activeInterpreters.ContainsInterpreter(value) == found)
                        result.Add(value);
                    else if (nonPrimary && !value.IsPrimarySystemThread())
                        result.Add(value);
                }

                return result;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PushActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            PushActiveInterpreter(interpreter, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData
            ) /* THREAD-SAFE */
        {
            int pushed = 0;

            PushActiveInterpreter(interpreter, clientData, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData,
            ref int pushed
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters == null)
                activeInterpreters = new InterpreterStackList();

            activeInterpreters.Push(new AnyPair<Interpreter, IClientData>(
                interpreter, clientData));

            Interlocked.Increment(ref pushed);

            ///////////////////////////////////////////////////////////////////
            // BEGIN NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_GLOBAL && NOTIFY_ACTIVE
            if ((interpreter != null) && !interpreter.Disposed &&
                interpreter.GlobalNotify)
            {
                /* IGNORED */
                Interpreter.CheckNotifications(null, false,
                    NotifyType.Interpreter, NotifyFlags.Pushed,
                    null, interpreter, clientData, null, null);
            }
#endif

            ///////////////////////////////////////////////////////////////////
            // END NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        public static IAnyPair<Interpreter, IClientData> PopActiveInterpreter()
        {
            int pushed = 1; // required, or no pop.

            return PopActiveInterpreter(ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IAnyPair<Interpreter, IClientData> PopActiveInterpreter(
            ref int pushed
            ) /* THREAD-SAFE */
        {
            IAnyPair<Interpreter, IClientData> anyPair = null;

            if (Interlocked.CompareExchange(ref pushed, 0, 0) > 0)
            {
                if ((activeInterpreters != null) &&
                    !activeInterpreters.IsEmpty)
                {
                    anyPair = activeInterpreters.Pop();
                    Interlocked.Decrement(ref pushed);
                }
            }

            ///////////////////////////////////////////////////////////////////
            // BEGIN NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_GLOBAL && NOTIFY_ACTIVE
            if (pair != null)
            {
                Interpreter interpreter = pair.X;

                if ((interpreter != null) && !interpreter.Disposed &&
                    interpreter.GlobalNotify)
                {
                    IClientData clientData = pair.Y;

                    /* IGNORED */
                    Interpreter.CheckNotifications(null, false,
                        NotifyType.Interpreter, NotifyFlags.Popped,
                        null, interpreter, clientData, null, null);
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////
            // END NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////

            return anyPair;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Context Manager Support Methods
#if THREADING
        public static IEnumerable<IInterpreter> FilterInterpretersToPurge(
            IEnumerable<IInterpreter> interpreters,
            bool nonPrimary
            )
        {
            //
            // NOTE: First, filter the specified list of interpreters to
            //       those that are not present in the list of all valid
            //       (i.e. created and not disposed) interpreters.
            //
            IEnumerable<IInterpreter> result = FilterInterpreters(
                interpreters, false, nonPrimary);

            //
            // HACK: If an interpreter is present on the active stack, we
            //       never want to purge its contexts.
            //
            result = FilterActiveInterpreters(result, false, false);

            //
            // NOTE: Finally, return the resulting list of interpreters.
            //
            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy Trace Support Properties
#if POLICY_TRACE
        public static bool PolicyTrace /* THREAD-SAFE */
        {
            get
            {
                lock (syncRoot)
                {
                    return policyTrace;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    policyTrace = value;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Support Methods
        public static Version GetTwoPartVersion( /* CANNOT RETURN NULL */
            int major,
            int minor
            )
        {
            int newMajor;

            if (major >= _Constants._Version.Minimum)
                newMajor = major;
            else
                newMajor = 0;

            int newMinor;

            if (minor >= _Constants._Version.Minimum)
                newMinor = minor;
            else
                newMinor = 0;

            return new Version(newMajor, newMinor);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetTwoPartVersion( /* MAY RETURN NULL */
            Version version
            )
        {
            if (version == null)
                return null;

            return GetTwoPartVersion(version.Major, version.Minor);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetThreePartVersion( /* CANNOT RETURN NULL */
            int major,
            int minor,
            int build
            )
        {
            int newMajor;

            if (major >= _Constants._Version.Minimum)
                newMajor = major;
            else
                newMajor = 0;

            int newMinor;

            if (minor >= _Constants._Version.Minimum)
                newMinor = minor;
            else
                newMinor = 0;

            int newBuild;

            if (build >= _Constants._Version.Minimum)
                newBuild = build;
            else
                newBuild = 0;

            return new Version(newMajor, newMinor, newBuild);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetThreePartVersion( /* MAY RETURN NULL */
            Version version
            )
        {
            if (version == null)
                return null;

            return GetThreePartVersion(
                version.Major, version.Minor, version.Build);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetFourPartVersion( /* CANNOT RETURN NULL */
            int major,
            int minor,
            int build,
            int revision
            )
        {
            int newMajor;

            if (major >= _Constants._Version.Minimum)
                newMajor = major;
            else
                newMajor = 0;

            int newMinor;

            if (minor >= _Constants._Version.Minimum)
                newMinor = minor;
            else
                newMinor = 0;

            int newBuild;

            if (build >= _Constants._Version.Minimum)
                newBuild = build;
            else
                newBuild = 0;

            int newRevision;

            if (revision >= _Constants._Version.Minimum)
                newRevision = revision;
            else
                newRevision = 0;

            return new Version(newMajor, newMinor, newBuild, newRevision);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Environment Variable Wrapper Methods
        private static string GetEnvironmentVariable(
            string variable
            ) /* THREAD-SAFE */
        {
            return CommonOps.Environment.GetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetEnvironmentVariable(
            string variable,
            string value
            ) /* THREAD-SAFE */
        {
            CommonOps.Environment.SetVariable(variable, value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Domain Variable Access Methods
        public static AppDomain GetAppDomain() /* THREAD-SAFE */
        {
            return appDomain;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAppDomainBaseDirectory() /* THREAD-SAFE */
        {
            return appDomainBaseDirectory;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Global Variable Access Methods
        #region Entry Assembly Variable Access Methods
        private static Assembly FindEntryAssembly() /* CANNOT RETURN NULL */
        {
            Assembly assembly = Assembly.GetEntryAssembly(); /* NULL? */

            if (assembly != null)
            {
                TraceOps.DebugTrace(String.Format(
                    "FindEntryAssembly: using entry assembly {0}",
                    FormatOps.WrapOrNull(assembly)),
                    typeof(GlobalState).Name, TracePriority.StartupDebug);

                return assembly;
            }

            assembly = Assembly.GetExecutingAssembly();

            TraceOps.DebugTrace(String.Format(
                "FindEntryAssembly: using executing assembly {0}",
                FormatOps.WrapOrNull(assembly)),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            return assembly; /* NOT NULL */
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsEntryAssembly(
            Assembly assembly
            ) /* THREAD-SAFE */
        {
            return Object.ReferenceEquals(assembly, entryAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly GetEntryAssembly() /* THREAD-SAFE */
        {
            return entryAssembly;
        }

        ///////////////////////////////////////////////////////////////////////

        public static AssemblyName GetEntryAssemblyName() /* THREAD-SAFE */
        {
            return entryAssemblyName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetEntryAssemblyTitle() /* THREAD-SAFE */
        {
            return entryAssemblyTitle;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetEntryAssemblyLocation() /* THREAD-SAFE */
        {
            return entryAssemblyLocation;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetEntryAssemblyVersion() /* THREAD-SAFE */
        {
            return entryAssemblyVersion;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetEntryAssemblyPath() /* THREAD-SAFE */
        {
            return entryAssemblyPath;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Executing Assembly Variable Access Methods
        public static bool IsAssembly(
            Assembly assembly
            ) /* THREAD-SAFE */
        {
            return Object.ReferenceEquals(assembly, thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly GetAssembly() /* THREAD-SAFE */
        {
            return thisAssembly;
        }

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        public static Evidence GetAssemblyEvidence() /* THREAD-SAFE */
        {
            return thisAssemblyEvidence;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static AssemblyName GetAssemblyName() /* THREAD-SAFE */
        {
            return thisAssemblyName;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTitle() /* THREAD-SAFE */
        {
            return thisAssemblyTitle;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyLocation() /* THREAD-SAFE */
        {
            return thisAssemblyLocation;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblySimpleName() /* THREAD-SAFE */
        {
            return thisAssemblySimpleName;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyFullName() /* THREAD-SAFE */
        {
            return thisAssemblyFullName;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetAssemblyVersion() /* THREAD-SAFE */
        {
            return thisAssemblyVersion;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyUpdateVersion() /* THREAD-SAFE */
        {
            if (thisAssemblyVersion == null)
                return null;

            if ((thisAssemblyVersion.Major == DefaultMajorVersion) &&
                (thisAssemblyVersion.Minor == DefaultMinorVersion))
            {
                //
                // NOTE: This has a default major and minor version, use
                //       the build and revision only.
                //
                return String.Format(
                    UpdateVersionFormat, thisAssemblyVersion.Build,
                    thisAssemblyVersion.Revision);
            }
            else
            {
                //
                // NOTE: This has a non-default major or minor version,
                //       use the full version string.
                //
                return thisAssemblyVersion.ToString();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static CultureInfo GetAssemblyCultureInfo() /* THREAD-SAFE */
        {
            return thisAssemblyCultureInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyPath() /* THREAD-SAFE */
        {
            return thisAssemblyPath;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveAssemblyPath() /* THREAD-SAFE */
        {
            return !String.IsNullOrEmpty(thisAssemblyPath);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri() /* THREAD-SAFE */
        {
            return thisAssemblyUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUpdateBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyUpdateBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyDownloadBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyDownloadBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyScriptBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyScriptBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyAuxiliaryBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyAuxiliaryBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyNamespaceUri() /* THREAD-SAFE */
        {
            return thisAssemblyNamespaceUri;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Binary Executable Variable Access Methods
        public static string GetBinaryPath() /* THREAD-SAFE */
        {
            return binaryPath;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveBinaryPath() /* THREAD-SAFE */
        {
            return !String.IsNullOrEmpty(binaryPath);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Management Variable Access Methods
        public static string GetResourceBaseName() /* THREAD-SAFE */
        {
            return resourceBaseName;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Global Variable Access Methods
#if DEBUGGER
        public static string GetDebuggerName() /* THREAD-SAFE */
        {
            return debuggerName;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        public static string GetPackageName() /* CANNOT RETURN NULL */
        {
            return (packageName != null) ?
                packageName : DefaultPackageName;
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        public static string GetPackageNameNoCase() /* CANNOT RETURN NULL */
        {
            return (packageNameNoCase != null) ?
                packageNameNoCase : DefaultPackageNameNoCase;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageTypeName( /* MAY RETURN NULL */
            PackageType packageType, /* in */
            string @default          /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            switch (packageType)
            {
                case PackageType.Library:
                    return LibraryPackageName;
                case PackageType.Test:
                    return TestPackageName;
                case PackageType.Default:
                    return DefaultPackageName;
                default:
                    return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageName( /* CANNOT RETURN NULL */
            PackageType packageType,
            bool noCase
            ) /* THREAD-SAFE */
        {
            return GetPackageName(packageType, null, null, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageName( /* CANNOT RETURN NULL */
            PackageType packageType,
            string prefix,
            string suffix,
            bool noCase
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: We do not want to return the assembly name here because
            //       that is not guaranteed to be the same as what we consider
            //       the "package name".
            //
            string result = GetAssemblyTitle();

            if (String.IsNullOrEmpty(result))
                result = GetPackageTypeName(packageType, DefaultPackageName);

            if (noCase && !String.IsNullOrEmpty(result))
                result = result.ToLowerInvariant();

            if (!String.IsNullOrEmpty(prefix) || !String.IsNullOrEmpty(suffix))
            {
                result = String.Format(
                    PackageNameFormat, prefix, result, suffix);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetPackageVersion() /* THREAD-SAFE */
        {
            //
            // NOTE: Package versions do not typically include the build
            //       and revision numbers; therefore, be sure they are
            //       omitted in our return value.
            //
            return (packageVersion != null) ?
                GetTwoPartVersion(packageVersion) :
                GetTwoPartVersion(DefaultVersion);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static string GetTclPackageNamePath() /* THREAD-SAFE */
        {
            lock (syncRoot)
            {
                return tclPackageNamePath;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetTclPackageNameRootPath() /* THREAD-SAFE */
        {
            lock (syncRoot)
            {
                return tclPackageNameRootPath;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Binary Base Path Management Methods
        public static string GetRawBinaryBasePath() /* THREAD-SAFE */
        {
            return GetRawBinaryBasePath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetRawBinaryBasePath(
            Assembly assembly /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            return GetRawBasePath(assembly, GetBinaryPath());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Base Path Management Methods
        public static string GetRawBasePath() /* THREAD-SAFE */
        {
            return GetRawBasePath(thisAssemblyPath);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetRawBasePath(
            string path
            ) /* THREAD-SAFE */
        {
            return GetRawBasePath(thisAssembly, path);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetRawBasePath(
            Assembly assembly, /* OPTIONAL: May be null. */
            string path
            ) /* THREAD-SAFE */
        {
            return PathOps.GetBasePath(assembly, path);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Base Path Global Variable Management Methods
        public static string GetBasePath() /* THREAD-SAFE */
        {
            return GetBasePath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetBasePath(
            Assembly assembly /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            string result = null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Allow manual override of the base path via the
                //       SetBasePath method.
                //
                result = masterBasePath;

                //
                // NOTE: Was the master base path set to something that
                //       looks valid?
                //
                if (String.IsNullOrEmpty(result))
                {
                    //
                    // NOTE: Allow the "EAGLE_BASE" environment variable
                    //       to override the base path.
                    //
                    result = GetEnvironmentVariable(EnvVars.EagleBase);
                }

                //
                // NOTE: Was the "EAGLE_BASE" environment variable set to
                //       something that looks valid?
                //
                if (String.IsNullOrEmpty(result))
                {
                    //
                    // NOTE: Check if the assembly specified by the caller,
                    //       if any, is present in the GAC.
                    //
                    if ((assembly != null) && assembly.GlobalAssemblyCache)
                    {
                        //
                        // NOTE: The specified assembly has been GAC'd.  We
                        //       need to use the registry to find where we
                        //       were actually installed to.
                        //
                        result = SetupOps.GetPath(packageVersion);

                        //
                        // NOTE: If we failed to get the path from the setup
                        //       registry hive (perhaps setup was not run?)
                        //       then we resort to using the current assembly
                        //       probing path for the application domain, if
                        //       possible.
                        //
                        if (String.IsNullOrEmpty(result) && HaveBinaryPath())
                            result = GetRawBinaryBasePath(assembly);
                    }
                    else
                    {
                        //
                        // NOTE: Return the base directory that this assembly
                        //       was loaded from (i.e. without the "bin"), if
                        //       possible.
                        //
                        if (HaveBinaryPath())
                            result = GetRawBinaryBasePath(assembly);
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetBasePath( /* EXTERNAL USE ONLY */
            string basePath,
            bool refresh
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "SetBasePath: entered, basePath = {0}, refresh = {1}",
                FormatOps.WrapOrNull(basePath), refresh),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                masterBasePath = basePath;

                //
                // BUGFIX: Be sure to propagate the changes down to where they
                //         are actually useful.
                //
                if (refresh)
                    RefreshBasePath();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void RefreshBasePath() /* THREAD-SAFE */
        {
            RefreshLibraryPath();

            ///////////////////////////////////////////////////////////////////

            TraceOps.DebugTrace("RefreshBasePath: complete",
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Externals Path Global Variable Management Methods
        public static string GetExternalsPath() /* THREAD-SAFE */
        {
            return GetExternalsPath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetExternalsPath(
            Assembly assembly /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            string result = null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Allow manual override of the base path via the
                //       SetExternalsPath method.
                //
                result = masterExternalsPath;

                //
                // NOTE: Was the master base path set to something that
                //       looks valid?
                //
                if (String.IsNullOrEmpty(result))
                {
                    //
                    // NOTE: Allow the "EAGLE_EXTERNALS" environment variable
                    //       to override the externals path.
                    //
                    result = GetEnvironmentVariable(EnvVars.EagleExternals);
                }

                //
                // NOTE: Was the "EAGLE_EXTERNALS" environment variable set to
                //       something that looks valid?
                //
                if (String.IsNullOrEmpty(result))
                {
                    string basePath = GetBasePath(assembly);

                    if (!String.IsNullOrEmpty(basePath))
                    {
                        result = PathOps.CombinePath(
                            null, basePath, _Path.Externals);
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetExternalsPath( /* EXTERNAL USE ONLY */
            string externalsPath,
            bool refresh
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "SetExternalsPath: entered, externalsPath = {0}, refresh = {1}",
                FormatOps.WrapOrNull(externalsPath), refresh),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                masterExternalsPath = externalsPath;

                //
                // NOTE: Be sure to propagate the changes down to where they
                //       are actually useful.
                //
                if (refresh)
                    RefreshExternalsPath();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void RefreshExternalsPath() /* THREAD-SAFE */
        {
            //
            // TODO: Currently, this method does nothing.  Eventually, we may
            //       need to notify internal or external components of this
            //       path change.
            //
            TraceOps.DebugTrace("RefreshExternalsPath: complete",
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Library Path / Auto-Path Support Methods
        private static bool FetchInterpreterPaths(
            Interpreter interpreter,
            ref string libraryPath,
            ref StringList autoPathList
            )
        {
            CreateFlags createFlags = CreateFlags.None;

            return FetchInterpreterPathsAndFlags(
                interpreter, ref libraryPath, ref autoPathList,
                ref createFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool FetchInterpreterPathsAndFlags(
            Interpreter interpreter,
            ref string libraryPath,
            ref StringList autoPathList,
            ref CreateFlags createFlags
            )
        {
            bool result = false;

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        libraryPath = interpreter.LibraryPath; /* throw */
                        autoPathList = interpreter.AutoPathList; /* throw */
                        createFlags = interpreter.CreateFlags; /* throw */

                        result = true;
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "FetchInterpreterPathsAndFlags: " +
                            "unable to acquire interpreter {0} lock",
                            FormatOps.InterpreterNoThrow(interpreter)),
                            typeof(GlobalState).Name,
                            TracePriority.LockError);
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(GlobalState).Name,
                        TracePriority.StartupError);
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Library Path Global Variable Management Methods
        //
        // WARNING: *DEADLOCK* This requires the interpreter lock.
        //
        public static string GetLibraryPath(
            Interpreter interpreter /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            string libraryPath = null;
            StringList autoPathList = null;
            CreateFlags createFlags = CreateFlags.None;

            FetchInterpreterPathsAndFlags(
                interpreter, ref libraryPath, ref autoPathList,
                ref createFlags);

            return GetLibraryPath(
                interpreter, libraryPath, autoPathList, createFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetLibraryPath(
            Interpreter interpreter, /* OPTIONAL: May be null. */
            string libraryPath,
            StringList autoPathList,
            CreateFlags createFlags
            ) /* THREAD-SAFE */
        {
            bool showAutoPath = FlagOps.HasFlags(
                createFlags, CreateFlags.ShowAutoPath, true);

            bool strictAutoPath = FlagOps.HasFlags(
                createFlags, CreateFlags.StrictAutoPath, true);

            PathDictionary<object> paths = null;

            GetInterpreterAutoPathList(
                interpreter, libraryPath, autoPathList, true,
                showAutoPath, strictAutoPath, ref paths);

            GetMasterAutoPathList(
                interpreter, true, showAutoPath, strictAutoPath,
                ref paths);

            if ((paths != null) && (paths.Count > 0))
            {
                string path = new StringList(paths.Keys)[0]; /* HACK */

                if (!String.IsNullOrEmpty(path))
                    return path;
            }

            return GetBasePath();
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetLibraryPath(
            Assembly assembly, /* OPTIONAL: May be null. */
            bool noMaster,
            bool root,
            bool noBinary
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!noMaster && !String.IsNullOrEmpty(masterLibraryPath))
                {
                    //
                    // NOTE: Allow manual override of the library path.
                    //
                    return masterLibraryPath;
                }
                else if (root)
                {
                    //
                    // NOTE: We want the root library directory.  This path
                    //       allows us to run from the build directory (e.g.
                    //       "bin\Debug\bin") and still refer to directories
                    //       that are not in the build directory (i.e. they
                    //       are a peer of the outer "bin" directory).
                    //
                    return Path.GetFullPath(PathOps.CombinePath(
                        null, GetBasePath(assembly), TclVars.LibPath));
                }
                else if (noBinary)
                {
                    //
                    // NOTE: We want the non-root (or peer) assembly library
                    //       directory.  When running in a non-build environment,
                    //       this will typically be the same as the root library
                    //       directory.  This assumes that the parent directory
                    //       of the Eagle assembly contains a directory named
                    //       "lib".
                    //
                    return Path.GetFullPath(PathOps.CombinePath(
                        null, Path.GetDirectoryName(GetAssemblyPath()),
                        TclVars.LibPath));
                }
                else
                {
                    //
                    // NOTE: We want the non-root (or peer) binary library
                    //       directory.  When running in a non-build environment,
                    //       this will typically be the same as the root library
                    //       directory.
                    //
                    // BUGBUG: This basically assumes that the directory
                    //         containing the application binary is parallel to
                    //         the Eagle library directory.  This will not work
                    //         if Eagle is running from "/usr/local/bin/Eagle"
                    //         and the library directory is "/usr/local/lib/Eagle/"
                    //         (OpenBSD).  In order for that layout to work, we
                    //         would have to go up one more level.
                    //
                    return Path.GetFullPath(PathOps.CombinePath(
                        null, Path.GetDirectoryName(GetBinaryPath()),
                        TclVars.LibPath));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetLibraryPath(
            string libraryPath,
            bool refresh
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "SetLibraryPath: entered, libraryPath = {0}, refresh = {1}",
                FormatOps.WrapOrNull(libraryPath), refresh),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                masterLibraryPath = libraryPath;

                //
                // BUGFIX: Be sure to propagate the changes down to where they
                //         are actually useful.
                //
                if (refresh)
                    RefreshLibraryPath();
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        private static string GetUnixLibraryPath(
            bool noMaster,
            bool local
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!noMaster && !String.IsNullOrEmpty(masterLibraryPath))
                {
                    //
                    // NOTE: Allow manual override of the library path.
                    //
                    return masterLibraryPath;
                }
                else if (local)
                {
                    //
                    // NOTE: We want the directory where local libraries
                    //       are installed.
                    //
                    return TclVars.UserLocalLibPath;
                }
                else
                {
                    //
                    // NOTE: We want the directory where libraries are
                    //       installed.
                    //
                    return TclVars.UserLibPath;
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static void RefreshLibraryPath() /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                packagePeerBinaryPath = GetPackagePath(
                    thisAssembly, null, null, false, false, false);

                packagePeerAssemblyPath = GetPackagePath(
                    thisAssembly, null, null, false, false, true);

                packageRootPath = GetPackagePath(
                    thisAssembly, null, null, false, true, false);

                ///////////////////////////////////////////////////////////////

                packageNameBinaryPath = GetPackagePath(
                    thisAssembly, packageName, packageVersion, false, false,
                    false);

                packageNameAssemblyPath = GetPackagePath(
                    thisAssembly, packageName, packageVersion, false, false,
                    true);

                packageNameRootPath = GetPackagePath(
                    thisAssembly, packageName, packageVersion, false, true,
                    false);

                ///////////////////////////////////////////////////////////////

#if UNIX
                unixPackageNameLocalPath = GetUnixPackagePath(
                    unixPackageName, unixPackageVersion, false, true);

                unixPackageNamePath = GetUnixPackagePath(
                    unixPackageName, unixPackageVersion, false, false);
#endif

                ///////////////////////////////////////////////////////////////

#if NATIVE && TCL
                tclPackageNamePath = GetPackagePath(
                    thisAssembly, TclVars.PackageName, null, false, false,
                    false);

                tclPackageNameRootPath = GetPackagePath(
                    thisAssembly, TclVars.PackageName, null, false, true,
                    false);
#endif

                ///////////////////////////////////////////////////////////////

                //
                // BUGFIX: Reset the master auto-path so that it will be
                //         initialized again [using our new paths] on the
                //         next call to the GetAutoPathList method.
                //
                ResetMasterAutoPathList();
            }

            ///////////////////////////////////////////////////////////////////

            TraceOps.DebugTrace("RefreshLibraryPath: complete",
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Path Support Methods
        #region Library Package Path Support Methods
        public static string GetPackagePath( /* MAY RETURN NULL */
            PackageType packageType, /* in */
            Version version,         /* OPTIONAL: May be null. */
            string @default          /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            string result = GetPackageTypeName(packageType, @default);

            if (!String.IsNullOrEmpty(result) && (version != null))
                result += FormatOps.MajorMinor(version);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackagePath(
            Assembly assembly, /* OPTIONAL: May be null. */
            string name,       /* OPTIONAL: May be null. */
            Version version,   /* OPTIONAL: May be null. */
            bool noMaster,
            bool root,
            bool noBinary
            ) /* THREAD-SAFE */
        {
            string result = GetLibraryPath(assembly, noMaster, root, noBinary);

            if (!String.IsNullOrEmpty(name))
            {
                result = PathOps.CombinePath(null, result, name);

                if (version != null)
                    result += FormatOps.MajorMinor(version);
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Unix Package Path Support Methods
#if UNIX
        private static string GetUnixPackagePath(
            string name,
            Version version,
            bool noMaster,
            bool local
            ) /* THREAD-SAFE */
        {
            string result = GetUnixLibraryPath(noMaster, local);

            if (!String.IsNullOrEmpty(name))
            {
                result = PathOps.CombinePath(null, result, name);

                if (version != null)
                    result += FormatOps.MajorMinor(version);
            }

            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Package Path Support Methods
        private static string GetAssemblyPackagePath(
            string name,
            Version version
            ) /* THREAD-SAFE */
        {
            string result = null;

            if (HaveAssemblyPath()) /* NOTE: Needed by GetAssemblyPath(). */
            {
                string basePath = GetAssemblyPath();

                if (!String.IsNullOrEmpty(basePath))
                {
                    result = PathOps.CombinePath(
                        null, basePath, TclVars.LibPath);

                    if (!String.IsNullOrEmpty(name))
                    {
                        result = PathOps.CombinePath(null, result, name);

                        if (version != null)
                            result += FormatOps.MajorMinor(version);
                    }
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Binary Base Package Path Support Methods
        private static string GetRawBinaryBasePackagePath(
            string name,
            Version version
            ) /* THREAD-SAFE */
        {
            string result = null;

            if (HaveBinaryPath()) /* NOTE: Needed by GetRawBinaryBasePath. */
            {
                string basePath = GetRawBinaryBasePath();

                if (!String.IsNullOrEmpty(basePath))
                {
                    result = PathOps.CombinePath(
                        null, basePath, TclVars.LibPath);

                    if (!String.IsNullOrEmpty(name))
                    {
                        result = PathOps.CombinePath(null, result, name);

                        if (version != null)
                            result += FormatOps.MajorMinor(version);
                    }
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Base Package Path Support Methods
        private static string GetRawBasePackagePath(
            string name,
            Version version
            ) /* THREAD-SAFE */
        {
            string result = null;

            if (HaveAssemblyPath()) /* NOTE: Needed by GetRawBasePath(). */
            {
                string basePath = GetRawBasePath();

                if (!String.IsNullOrEmpty(basePath))
                {
                    result = PathOps.CombinePath(
                        null, basePath, TclVars.LibPath);

                    if (!String.IsNullOrEmpty(name))
                    {
                        result = PathOps.CombinePath(null, result, name);

                        if (version != null)
                            result += FormatOps.MajorMinor(version);
                    }
                }
            }

            return result;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Auto-Path Support Methods
        private static void GetInterpreterAutoPathList(
            Interpreter interpreter, /* OPTIONAL: May be null. */
            string libraryPath,
            StringList autoPathList,
            bool libraryOnly,
            bool showAutoPath,
            bool strictAutoPath,
            ref PathDictionary<object> paths
            ) /* THREAD-SAFE */
        {
            if (paths == null)
                paths = new PathDictionary<object>();

            if (!String.IsNullOrEmpty(libraryPath) &&
                !paths.ContainsKey(libraryPath) &&
                (!strictAutoPath || Directory.Exists(libraryPath)))
            {
                paths.Add(libraryPath, null);

                if (showAutoPath)
                {
                    TraceOps.DebugWriteTo(interpreter, String.Format(
                        "GetInterpreterAutoPathList: Added interpreter " +
                        "library path: {0}", FormatOps.WrapOrNull(
                        libraryPath)), true);
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: In "library only" mode, only consider paths which
            //       could contain the core script library.
            //
            if (libraryOnly)
                return;

            ///////////////////////////////////////////////////////////////////

            if (autoPathList != null)
            {
                foreach (string path in autoPathList)
                {
                    if (!String.IsNullOrEmpty(path) &&
                        !paths.ContainsKey(path) &&
                        (!strictAutoPath || Directory.Exists(path)))
                    {
                        paths.Add(path, null);

                        if (showAutoPath)
                        {
                            TraceOps.DebugWriteTo(interpreter, String.Format(
                                "GetInterpreterAutoPathList: Added " +
                                "interpreter package path: {0}",
                                FormatOps.WrapOrNull(path)), true);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetMasterAutoPathList(
            Interpreter interpreter, /* OPTIONAL: May be null. */
            bool libraryOnly,
            bool showAutoPath,
            bool strictAutoPath,
            ref PathDictionary<object> paths
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (paths == null)
                    paths = new PathDictionary<object>();

                //
                // TODO: Add any extra "default" package index search paths
                //       here.
                //
                if (!String.IsNullOrEmpty(libraryPath) &&
                    !paths.ContainsKey(libraryPath) &&
                    (!strictAutoPath || Directory.Exists(libraryPath)))
                {
                    paths.Add(libraryPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added library path: {0}",
                            FormatOps.WrapOrNull(libraryPath)), true);
                    }
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: In "library only" mode, only consider paths which
                //       could contain the core script library.
                //
                if (libraryOnly)
                    return;

                ///////////////////////////////////////////////////////////////

                if (!String.IsNullOrEmpty(tclLibraryPath) &&
                    !paths.ContainsKey(tclLibraryPath) &&
                    (!strictAutoPath || Directory.Exists(tclLibraryPath)))
                {
                    paths.Add(tclLibraryPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added Tcl library path: {0}",
                            FormatOps.WrapOrNull(tclLibraryPath)), true);
                    }
                }

                if (autoPathList != null)
                {
                    foreach (string path in autoPathList)
                    {
                        if (!String.IsNullOrEmpty(path) &&
                            !paths.ContainsKey(path) &&
                            (!strictAutoPath || Directory.Exists(path)))
                        {
                            paths.Add(path, null);

                            if (showAutoPath)
                            {
                                TraceOps.DebugWriteTo(interpreter, String.Format(
                                    "GetMasterAutoPathList: Added package path: {0}",
                                    FormatOps.WrapOrNull(path)), true);
                            }
                        }
                    }
                }

                if (tclAutoPathList != null)
                {
                    foreach (string path in tclAutoPathList)
                    {
                        if (!String.IsNullOrEmpty(path) &&
                            !paths.ContainsKey(path) &&
                            (!strictAutoPath || Directory.Exists(path)))
                        {
                            paths.Add(path, null);

                            if (showAutoPath)
                            {
                                TraceOps.DebugWriteTo(interpreter, String.Format(
                                    "GetMasterAutoPathList: Added Tcl package path: {0}",
                                    FormatOps.WrapOrNull(path)), true);
                            }
                        }
                    }
                }

#if UNIX
                if (!String.IsNullOrEmpty(unixPackageNameLocalPath) &&
                    !paths.ContainsKey(unixPackageNameLocalPath) &&
                    (!strictAutoPath || Directory.Exists(unixPackageNameLocalPath)))
                {
                    paths.Add(unixPackageNameLocalPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added Unix package name local path: {0}",
                            FormatOps.WrapOrNull(unixPackageNameLocalPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(unixPackageNamePath) &&
                    !paths.ContainsKey(unixPackageNamePath) &&
                    (!strictAutoPath || Directory.Exists(unixPackageNamePath)))
                {
                    paths.Add(unixPackageNamePath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added Unix package name path: {0}",
                            FormatOps.WrapOrNull(unixPackageNamePath)), true);
                    }
                }
#endif

                if (!String.IsNullOrEmpty(packageNameBinaryPath) &&
                    !paths.ContainsKey(packageNameBinaryPath) &&
                    (!strictAutoPath || Directory.Exists(packageNameBinaryPath)))
                {
                    paths.Add(packageNameBinaryPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added package name binary path: {0}",
                            FormatOps.WrapOrNull(packageNameBinaryPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(packageNameAssemblyPath) &&
                    !paths.ContainsKey(packageNameAssemblyPath) &&
                    (!strictAutoPath || Directory.Exists(packageNameAssemblyPath)))
                {
                    paths.Add(packageNameAssemblyPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added package name assembly path: {0}",
                            FormatOps.WrapOrNull(packageNameAssemblyPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(packageNameRootPath) &&
                    !paths.ContainsKey(packageNameRootPath) &&
                    (!strictAutoPath || Directory.Exists(packageNameRootPath)))
                {
                    paths.Add(packageNameRootPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added package name root path: {0}",
                            FormatOps.WrapOrNull(packageNameRootPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(packagePeerBinaryPath) &&
                    !paths.ContainsKey(packagePeerBinaryPath) &&
                    (!strictAutoPath || Directory.Exists(packagePeerBinaryPath)))
                {
                    paths.Add(packagePeerBinaryPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added package peer binary path: {0}",
                            FormatOps.WrapOrNull(packagePeerBinaryPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(packagePeerAssemblyPath) &&
                    !paths.ContainsKey(packagePeerAssemblyPath) &&
                    (!strictAutoPath || Directory.Exists(packagePeerAssemblyPath)))
                {
                    paths.Add(packagePeerAssemblyPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added package peer assembly path: {0}",
                            FormatOps.WrapOrNull(packagePeerAssemblyPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(packageRootPath) &&
                    !paths.ContainsKey(packageRootPath) &&
                    (!strictAutoPath || Directory.Exists(packageRootPath)))
                {
                    paths.Add(packageRootPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added package root path: {0}",
                            FormatOps.WrapOrNull(packageRootPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(assemblyPackageNamePath) &&
                    !paths.ContainsKey(assemblyPackageNamePath) &&
                    (!strictAutoPath || Directory.Exists(assemblyPackageNamePath)))
                {
                    paths.Add(assemblyPackageNamePath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added assembly package name path: {0}",
                            FormatOps.WrapOrNull(assemblyPackageNamePath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(assemblyPackageRootPath) &&
                    !paths.ContainsKey(assemblyPackageRootPath) &&
                    (!strictAutoPath || Directory.Exists(assemblyPackageRootPath)))
                {
                    paths.Add(assemblyPackageRootPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added assembly package root path: {0}",
                            FormatOps.WrapOrNull(assemblyPackageRootPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(rawBinaryBasePackageNamePath) &&
                    !paths.ContainsKey(rawBinaryBasePackageNamePath) &&
                    (!strictAutoPath || Directory.Exists(rawBinaryBasePackageNamePath)))
                {
                    paths.Add(rawBinaryBasePackageNamePath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added raw binary base package name path: {0}",
                            FormatOps.WrapOrNull(rawBinaryBasePackageNamePath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(rawBinaryBasePackageRootPath) &&
                    !paths.ContainsKey(rawBinaryBasePackageRootPath) &&
                    (!strictAutoPath || Directory.Exists(rawBinaryBasePackageRootPath)))
                {
                    paths.Add(rawBinaryBasePackageRootPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added raw binary base package root path: {0}",
                            FormatOps.WrapOrNull(rawBinaryBasePackageRootPath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(rawBasePackageNamePath) &&
                    !paths.ContainsKey(rawBasePackageNamePath) &&
                    (!strictAutoPath || Directory.Exists(rawBasePackageNamePath)))
                {
                    paths.Add(rawBasePackageNamePath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added raw base package name path: {0}",
                            FormatOps.WrapOrNull(rawBasePackageNamePath)), true);
                    }
                }

                if (!String.IsNullOrEmpty(rawBasePackageRootPath) &&
                    !paths.ContainsKey(rawBasePackageRootPath) &&
                    (!strictAutoPath || Directory.Exists(rawBasePackageRootPath)))
                {
                    paths.Add(rawBasePackageRootPath, null);

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetMasterAutoPathList: Added raw base package root path: {0}",
                            FormatOps.WrapOrNull(rawBasePackageRootPath)), true);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetMasterAutoPathList()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (masterAutoPathList == null)
                    return;

                masterAutoPathList = null;
            }

            ///////////////////////////////////////////////////////////////////

            TraceOps.DebugTrace("ResetMasterAutoPathList: complete",
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Auto-Path Global Variable Management Methods
        public static void BeginWithAutoPath( /* EXTERNAL USE ONLY */
            string path,            /* in */
            bool verbose,           /* in */
            ref string savedLibPath /* out */
            )
        {
            savedLibPath = GetEnvironmentVariable(EnvVars.EagleLibPath);

            StringList list = null;

            if (savedLibPath != null)
                list = StringList.FromString(savedLibPath);

            if (list == null)
                list = new StringList();

            if (path != null)
                list.Insert(0, path);

            /* NO RESULT */
            SetEnvironmentVariable(EnvVars.EagleLibPath, list.ToString());

            /* NO RESULT */
            RefreshAutoPathList(verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void EndWithAutoPath( /* EXTERNAL USE ONLY */
            bool verbose,           /* in */
            ref string savedLibPath /* in, out */
            )
        {
            /* NO RESULT */
            SetEnvironmentVariable(EnvVars.EagleLibPath, savedLibPath);

            savedLibPath = null;

            /* NO RESULT */
            RefreshAutoPathList(verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void RefreshAutoPathList( /* EXTERNAL USE ONLY */
            bool verbose
            ) /* THREAD-SAFE */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Re-query all of our auto-path related environment
                //       variables now.
                //
                libraryPath = GlobalConfiguration.GetValue(
                    EnvVars.EagleLibrary, GlobalConfiguration.GetFlags(
                    ConfigurationFlags.GlobalStateNoPrefix, verbose));

                autoPathList = StringList.FromString(
                    GlobalConfiguration.GetValue(EnvVars.EagleLibPath,
                    GlobalConfiguration.GetFlags(
                        ConfigurationFlags.GlobalStateNoPrefix, verbose)));

                ///////////////////////////////////////////////////////////////

                tclLibraryPath = GlobalConfiguration.GetValue(
                    EnvVars.TclLibrary, GlobalConfiguration.GetFlags(
                    ConfigurationFlags.GlobalState, verbose));

                tclAutoPathList = StringList.FromString(
                    GlobalConfiguration.GetValue(EnvVars.TclLibPath,
                    GlobalConfiguration.GetFlags(
                        ConfigurationFlags.GlobalState, verbose)));

                ///////////////////////////////////////////////////////////////

                //
                // BUGFIX: Reset the master auto-path so that it will be
                //         initialized again [using our new paths] on the
                //         next call to the GetAutoPathList method.
                //
                ResetMasterAutoPathList();
            }

            ///////////////////////////////////////////////////////////////////

            TraceOps.DebugTrace("RefreshAutoPathList: complete",
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetAutoPathList( /* CANNOT RETURN NULL */
            Interpreter interpreter, /* OPTIONAL: May be null. */
            bool refresh
            ) /* THREAD-SAFE */
        {
            string libraryPath = null;
            StringList autoPathList = null;
            CreateFlags createFlags = CreateFlags.None;

            FetchInterpreterPathsAndFlags(
                interpreter, ref libraryPath, ref autoPathList,
                ref createFlags);

            return GetAutoPathList(
                interpreter, libraryPath, autoPathList, createFlags,
                refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetAutoPathList( /* CANNOT RETURN NULL */
            Interpreter interpreter, /* OPTIONAL: May be null. */
            string libraryPath,
            StringList autoPathList,
            CreateFlags createFlags,
            bool refresh
            ) /* THREAD-SAFE */
        {
            bool showAutoPath = FlagOps.HasFlags(
                createFlags, CreateFlags.ShowAutoPath, true);

            bool strictAutoPath = FlagOps.HasFlags(
                createFlags, CreateFlags.StrictAutoPath, true);

            if (showAutoPath)
            {
                TraceOps.DebugWriteTo(interpreter, String.Format(
                    "GetAutoPathList: entered, interpreter = {0}, " +
                    "createFlags = {1}, refresh = {2}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(createFlags), refresh), true);
            }

            PathDictionary<object> allPaths = null;

            GetInterpreterAutoPathList(
                interpreter, libraryPath, autoPathList, false,
                showAutoPath, strictAutoPath, ref allPaths);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (refresh || (masterAutoPathList == null))
                {
                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetAutoPathList: Master path list {0}",
                            (masterAutoPathList != null) ?
                                "was initialized" : "was not initialized"),
                            true);
                    }

                    PathDictionary<object> masterPaths = null;

                    GetMasterAutoPathList(
                        interpreter, false, showAutoPath, strictAutoPath,
                        ref masterPaths);

                    masterAutoPathList = (masterPaths != null) ?
                        new StringList(masterPaths.Keys) : new StringList();

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetAutoPathList: Master path list initialized to: {0}",
                            FormatOps.WrapOrNull(masterAutoPathList)), true);
                    }
                }
                else
                {
                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter,
                            "GetAutoPathList: Master path list already initialized",
                            true);
                    }
                }

                if (showAutoPath)
                {
                    TraceOps.DebugWriteTo(interpreter, String.Format(
                        "GetAutoPathList: exited, interpreter = {0}, " +
                        "createFlags = {1}, refresh = {2}",
                        FormatOps.InterpreterNoThrow(interpreter),
                        FormatOps.WrapOrNull(createFlags), refresh), true);
                }

                //
                // NOTE: Merge in master path list into the overall list.
                //
                allPaths.Add(masterAutoPathList, true);
            }

            //
            // NOTE: Create a simple string list beased on the path list and
            //       return it.
            //
            return new StringList(allPaths.Keys);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Path Debugging Support Methods
        //
        // WARNING: *DEADLOCK* This requires the interpreter lock.
        //
        public static void GetPaths(
            Interpreter interpreter,
            bool all,
            ref StringPairList list
            ) /* THREAD-SAFE */
        {
            if (list == null)
                list = new StringPairList();

            ///////////////////////////////////////////////////////////////////
            //
            // PHASE 0: Interpreter path variables.
            //
            ///////////////////////////////////////////////////////////////////

            string libraryPath = null;
            StringList autoPathList = null;

            FetchInterpreterPaths(
                interpreter, ref libraryPath, ref autoPathList);

            list.Add("interpreter library path", libraryPath);

            list.Add("interpreter auto_path list",
                (autoPathList != null) ? autoPathList.ToString() : null);

            ///////////////////////////////////////////////////////////////////
            //
            // PHASE 1: Basic path variables.
            //
            ///////////////////////////////////////////////////////////////////

            lock (syncRoot) /* TRANSACTIONAL */
            {
                list.Add("binary path", binaryPath);
                list.Add("this assembly path", thisAssemblyPath);
                list.Add("entry assembly path", entryAssemblyPath);

                ///////////////////////////////////////////////////////////////

                list.Add("master base path", masterBasePath);
                list.Add("master library path", masterLibraryPath);

                ///////////////////////////////////////////////////////////////

                list.Add("library path", libraryPath);
                list.Add("Tcl library path", tclLibraryPath);

                ///////////////////////////////////////////////////////////////

                list.Add("package name binary path", packageNameBinaryPath);
                list.Add("package name assembly path", packageNameAssemblyPath);
                list.Add("package name root path", packageNameRootPath);

                ///////////////////////////////////////////////////////////////

#if NATIVE && TCL
                list.Add("Tcl package name path", tclPackageNamePath);
                list.Add("Tcl package name root path", tclPackageNameRootPath);
#endif

                ///////////////////////////////////////////////////////////////

#if UNIX
                list.Add("Unix package name local path",
                    unixPackageNameLocalPath);

                list.Add("Unix package name path", unixPackageNamePath);
#endif

                ///////////////////////////////////////////////////////////////

                list.Add("package peer binary path", packagePeerBinaryPath);
                list.Add("package peer assembly path", packagePeerAssemblyPath);
                list.Add("package root path", packageRootPath);

                ///////////////////////////////////////////////////////////////

                list.Add("assembly package name path", assemblyPackageNamePath);
                list.Add("assembly package root path", assemblyPackageRootPath);

                ///////////////////////////////////////////////////////////////

                list.Add("raw binary base package name path",
                    rawBinaryBasePackageNamePath);

                list.Add("raw binary base package root path",
                    rawBinaryBasePackageRootPath);

                ///////////////////////////////////////////////////////////////

                list.Add("raw base package name path", rawBasePackageNamePath);
                list.Add("raw base package root path", rawBasePackageRootPath);

                ///////////////////////////////////////////////////////////////
                //
                // PHASE 2: Path lists.
                //
                ///////////////////////////////////////////////////////////////

                list.Add("auto_path list", (autoPathList != null) ?
                    autoPathList.ToString() : null);

                list.Add("Tcl auto_path list", (tclAutoPathList != null) ?
                    tclAutoPathList.ToString() : null);

                list.Add("master auto_path list", (masterAutoPathList != null) ?
                    masterAutoPathList.ToString() : null);

                ///////////////////////////////////////////////////////////////

                if (!all)
                    return;

                ///////////////////////////////////////////////////////////////

                string prefix;

                ///////////////////////////////////////////////////////////////
                //
                // PHASE 3: Path resolution routines.
                //
                ///////////////////////////////////////////////////////////////

                prefix = "GetBasePath()";

                try
                {
                    list.Add(prefix, GetBasePath());
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "GetRawBinaryBasePath()";

                try
                {
                    list.Add(prefix, GetRawBinaryBasePath());
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "GetRawBasePath()";

                try
                {
                    list.Add(prefix, GetRawBasePath());
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "GetExternalsPath()";

                try
                {
                    list.Add(prefix, GetExternalsPath());
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                string binaryParentPath = Path.GetDirectoryName(
                    GetBinaryPath());

                ///////////////////////////////////////////////////////////////

                prefix = "Path.GetDirectoryName(GetBinaryPath())";

                try
                {
                    list.Add(prefix, binaryParentPath);
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = String.Format("Path.GetFullPath({0})", prefix);

                try
                {
                    list.Add(prefix, (binaryParentPath != null) ?
                        Path.GetFullPath(binaryParentPath) : null);
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "Path.GetFullPath(GetBasePath())";

                try
                {
                    string basePath = GetBasePath();

                    list.Add(prefix, (basePath != null) ?
                        Path.GetFullPath(basePath) : null);
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "Path.GetFullPath(GetRawBinaryBasePath())";

                try
                {
                    string basePath = GetRawBinaryBasePath();

                    list.Add(prefix, (basePath != null) ?
                        Path.GetFullPath(basePath) : null);
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "Path.GetFullPath(GetRawBasePath())";

                try
                {
                    string basePath = GetRawBasePath();

                    list.Add(prefix, (basePath != null) ?
                        Path.GetFullPath(basePath) : null);
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "AssemblyOps.GetCurrentPath(GetAssembly())";

                try
                {
                    list.Add(prefix,
                        AssemblyOps.GetCurrentPath(GetAssembly()));
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "AssemblyOps.GetOriginalPath(GetAssembly())";

                try
                {
                    list.Add(prefix,
                        AssemblyOps.GetOriginalPath(GetAssembly()));
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "AssemblyOps.GetCurrentPath(GetEntryAssembly())";

                try
                {
                    list.Add(prefix,
                        AssemblyOps.GetCurrentPath(GetEntryAssembly()));
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }

                ///////////////////////////////////////////////////////////////

                prefix = "AssemblyOps.GetOriginalPath(GetEntryAssembly())";

                try
                {
                    list.Add(prefix,
                        AssemblyOps.GetOriginalPath(GetEntryAssembly()));
                }
                catch (Exception e)
                {
                    list.Add(prefix, e.ToString());
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DisplayPaths(
            Interpreter interpreter,
            bool all
            ) /* THREAD-SAFE */
        {
            StringPairList list = null;

            GetPaths(interpreter, all, ref list);

            if (list != null)
            {
                foreach (IPair<string> element in list)
                {
                    if (element != null)
                    {
                        /* EXEMPT */
                        DebugOps.WriteTo(
                            interpreter, String.Format("{0} = {1}",
                            element.X, FormatOps.DisplayValue(
                            element.Y)), true);
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Embedding Support Methods
        private static bool DetectPackageFileViaAssembly(
            Assembly assembly,
            IClientData clientData, /* NOT USED */
            string packageName,
            Version packageVersion,
            string fileName,
            ref string path
            )
        {
            string assemblyPath = GetPackagePath(
                assembly, null, null, false, true, false);

            if (String.IsNullOrEmpty(assemblyPath))
                return false;

            if (!Directory.Exists(assemblyPath))
                return false;

            if (!PathOps.IsEqualFileName(Path.GetFileName(
                    assemblyPath), TclVars.LibPath))
            {
                assemblyPath = PathOps.CombinePath(
                    null, assemblyPath, TclVars.LibPath);

                if (String.IsNullOrEmpty(assemblyPath))
                    return false;

                if (!Directory.Exists(assemblyPath))
                    return false;
            }

            string localPath = assemblyPath;

            if (!String.IsNullOrEmpty(packageName) && (packageVersion != null))
            {
                localPath = PathOps.CombinePath(
                    null, localPath, FormatOps.PackageDirectory(
                    packageName, packageVersion, false));

                if (String.IsNullOrEmpty(localPath))
                    return false;

                if (!Directory.Exists(localPath))
                    return false;
            }

            if (!String.IsNullOrEmpty(fileName) &&
                !File.Exists(PathOps.CombinePath(null, localPath, fileName)))
            {
                return false;
            }

            path = assemblyPath;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool DetectPackageFileViaEnvironment(
            string variable,
            IClientData clientData, /* NOT USED */
            string packageName,
            Version packageVersion,
            string fileName,
            ref string path
            )
        {
            string variablePath = GetEnvironmentVariable(variable);

            if (String.IsNullOrEmpty(variablePath))
                return false;

            if (!Directory.Exists(variablePath))
                return false;

            if (!PathOps.IsEqualFileName(Path.GetFileName(
                    variablePath), TclVars.LibPath))
            {
                variablePath = PathOps.CombinePath(
                    null, variablePath, TclVars.LibPath);

                if (String.IsNullOrEmpty(variablePath))
                    return false;

                if (!Directory.Exists(variablePath))
                    return false;
            }

            string localPath = variablePath;

            if (!String.IsNullOrEmpty(packageName) && (packageVersion != null))
            {
                localPath = PathOps.CombinePath(
                    null, localPath, FormatOps.PackageDirectory(
                    packageName, packageVersion, false));

                if (String.IsNullOrEmpty(localPath))
                    return false;

                if (!Directory.Exists(localPath))
                    return false;
            }

            if (!String.IsNullOrEmpty(fileName) &&
                !File.Exists(PathOps.CombinePath(null, localPath, fileName)))
            {
                return false;
            }

            path = variablePath;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool DetectPackageFileViaSetup(
            Version version,
            IClientData clientData, /* NOT USED */
            string packageName,
            Version packageVersion,
            string fileName,
            ref string path
            )
        {
            string setupPath = SetupOps.GetPath(version);

            if (String.IsNullOrEmpty(setupPath))
                return false;

            if (!Directory.Exists(setupPath))
                return false;

            if (!PathOps.IsEqualFileName(Path.GetFileName(
                    setupPath), TclVars.LibPath))
            {
                setupPath = PathOps.CombinePath(
                    null, setupPath, TclVars.LibPath);

                if (String.IsNullOrEmpty(setupPath))
                    return false;

                if (!Directory.Exists(setupPath))
                    return false;
            }

            string localPath = setupPath;

            if (!String.IsNullOrEmpty(packageName) && (packageVersion != null))
            {
                localPath = PathOps.CombinePath(
                    null, localPath, FormatOps.PackageDirectory(
                    packageName, packageVersion, false));

                if (String.IsNullOrEmpty(localPath))
                    return false;

                if (!Directory.Exists(localPath))
                    return false;
            }

            if (!String.IsNullOrEmpty(fileName) &&
                !File.Exists(PathOps.CombinePath(null, localPath, fileName)))
            {
                return false;
            }

            path = setupPath;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DetectLibraryPath( /* EXTERNAL USE ONLY */
            Assembly assembly,
            IClientData clientData,
            DetectFlags detectFlags
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "DetectLibraryPath: entered, assembly = {0}, " +
                "clientData = {1}, detectFlags = {2}",
                FormatOps.AssemblyName(assembly, 0, false, true),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(detectFlags)),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Attempt to obtain the version of the assembly.
                //       If this value cannot be obtained (i.e. because
                //       the assembly or assembly name is null), it will
                //       not be used.
                //
                Version assemblyVersion = null;

                if (assembly != null)
                {
                    AssemblyName assemblyName = assembly.GetName();

                    if (assemblyName != null)
                        assemblyVersion = assemblyName.Version;
                }

                //
                // NOTE: Fetch the configured script library package
                //       name and version for the core library.
                //
                string packageName = GetPackageName(); /* "Eagle" */
                Version packageVersion = GetPackageVersion(); /* "1.0" */

                //
                // NOTE: What is the name of the file we are looking
                //       for?
                //
                string fileName = PathOps.ScriptFileNameOnly(
                    FileName.Initialization); /* "init.eagle" */

                //
                // NOTE: Attempt to find a suitable library path.
                //
                string path = null;

                if ((!FlagOps.HasFlags(
                        detectFlags, DetectFlags.Assembly, true) ||
                     !DetectPackageFileViaAssembly(
                        assembly, clientData, packageName,
                        packageVersion, fileName, ref path)) &&
                    (!FlagOps.HasFlags(
                        detectFlags, DetectFlags.Environment |
                        DetectFlags.BaseDirectory, true) ||
                     !DetectPackageFileViaEnvironment(
                        EnvVars.EagleBase, clientData, packageName,
                        packageVersion, fileName, ref path)) &&
                    (!FlagOps.HasFlags(
                        detectFlags, DetectFlags.Environment |
                        DetectFlags.Directory, true) ||
                     !DetectPackageFileViaEnvironment(
                        EnvVars.Eagle, clientData, packageName,
                        packageVersion, fileName, ref path)) &&
                    ((assemblyVersion == null) || !FlagOps.HasFlags(
                        detectFlags, DetectFlags.Setup |
                        DetectFlags.AssemblyVersion, true) ||
                     !DetectPackageFileViaSetup(
                        assemblyVersion, clientData, packageName,
                        packageVersion, fileName, ref path)) &&
                    (!FlagOps.HasFlags(
                        detectFlags, DetectFlags.Setup |
                        DetectFlags.PackageVersion, true) ||
                     !DetectPackageFileViaSetup(
                        packageVersion, clientData, packageName,
                        packageVersion, fileName, ref path)) &&
                    (!FlagOps.HasFlags(
                        detectFlags, DetectFlags.Setup |
                        DetectFlags.NoVersion, true) ||
                     !DetectPackageFileViaSetup(
                        null, clientData, packageName,
                        packageVersion, fileName, ref path)))
                {
                    //
                    // NOTE: Do nothing.
                    //
                }
                else
                {
                    SetLibraryPath(path, true);

                    TraceOps.DebugTrace(String.Format(
                        "DetectLibraryPath: exited (success), assembly = {0}, " +
                        "clientData = {1}, detectFlags = {2}, path = {3}, " +
                        "result = {4}",
                        FormatOps.AssemblyName(assembly, 0, false, true),
                        FormatOps.WrapOrNull(clientData),
                        FormatOps.WrapOrNull(detectFlags),
                        FormatOps.WrapOrNull(path), true),
                        typeof(GlobalState).Name, TracePriority.StartupDebug);

                    return true;
                }
            }

            TraceOps.DebugTrace(String.Format(
                "DetectLibraryPath: exited (failure), assembly = {0}, " +
                "clientData = {1}, detectFlags = {2}, result = {3}",
                FormatOps.AssemblyName(assembly, 0, false, true),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(detectFlags), false),
                typeof(GlobalState).Name, TracePriority.StartupError);

            return false;
        }
        #endregion
    }
}
