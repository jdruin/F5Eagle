/*
 * NativeUtility.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("4e7b9ec6-8474-49ec-9f5f-59c3f21e7046")]
    internal static class NativeUtility
    {
        #region Private Constants
        private static readonly Type itemsType = typeof(StringList).BaseType;

        ///////////////////////////////////////////////////////////////////////

        private const string itemsFieldName = "_items"; /* NOTE: Also Mono. */

        ///////////////////////////////////////////////////////////////////////

        private const string optionUse32BitSizeT = " USE_32BIT_SIZE_T=1";
        private const string optionUseSysStringLen = " USE_SYSSTRINGLEN=1";

        ///////////////////////////////////////////////////////////////////////

        private const BindingFlags itemsBindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr nativeModule = IntPtr.Zero;
        private static string nativeFileName = null;
        private static TypeDelegateDictionary nativeDelegates;

        ///////////////////////////////////////////////////////////////////////

        private static Eagle_GetVersion nativeGetVersion;
        private static Eagle_AllocateMemory nativeAllocateMemory;
        private static Eagle_FreeMemory nativeFreeMemory;
        private static Eagle_FreeElements nativeFreeElements;
        private static Eagle_SplitList nativeSplitList;
        private static Eagle_JoinList nativeJoinList;

        ///////////////////////////////////////////////////////////////////////

        private static bool strictPath = false;

        ///////////////////////////////////////////////////////////////////////

        private static bool locked = false;
        private static bool disabled = false; /* INFORMATIONAL */
        private static bool? isAvailable = null;
        private static string version = null;

        ///////////////////////////////////////////////////////////////////////

        private static FieldInfo itemsFieldInfo = null;
        private static bool noReflection = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static bool IsUsable(
            string version
            )
        {
            if (version == null)
            {
                TraceOps.DebugTrace(
                    "IsUsable: invalid version string",
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }

            if (version.IndexOf(
                    optionUse32BitSizeT,
                    StringOps.SystemStringComparisonType) == Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: missing option {0}",
                    FormatOps.WrapOrNull(optionUse32BitSizeT)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }

#if NATIVE_UTILITY_BSTR
            if (version.IndexOf(
                    optionUseSysStringLen,
                    StringOps.SystemStringComparisonType) == Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: missing option {0}",
                    FormatOps.WrapOrNull(optionUseSysStringLen)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }
#else
            if (version.IndexOf(
                    optionUseSysStringLen,
                    StringOps.SystemStringComparisonType) != Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: mismatched option {0}",
                    FormatOps.WrapOrNull(optionUseSysStringLen)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetNativeLibraryFileName(
            Interpreter interpreter /* NOT USED */
            )
        {
            string path = CommonOps.Environment.GetVariable(
                EnvVars.UtilityPath);

            if (!String.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                    return path;

                if (Directory.Exists(path))
                {
                    string fileName = PathOps.CombinePath(
                        null, path, DllName.Utility);

                    if (File.Exists(fileName))
                        return fileName;

                    //
                    // TODO: Is this strictly necessary here?  It is known
                    //       at this point that this file does not exist.
                    //       Setting the path here only controls the result
                    //       returned in non-strict mode (below).
                    //
                    path = fileName;
                }

                //
                // NOTE: If the environment variable was set and the utility
                //       library could not be found, force an invalid result
                //       to be returned.  This ends up skipping the standard
                //       automatic utility library detection logic.
                //
                lock (syncRoot)
                {
                    return strictPath ? null : path;
                }
            }

            //
            // HACK: If the processor architecture ends up being "AMD64", we
            //       want it to be "x64" instead, to match the platform name
            //       used by the native utility library project itself.
            //
            string processorName = PlatformOps.GetAlternateProcessorName(
                RuntimeOps.GetProcessorArchitecture(), true, false);

            if (processorName != null)
            {
                path = PathOps.CombinePath(
                    null, GlobalState.GetAssemblyPath(), processorName,
                    DllName.Utility);

                if (File.Exists(path))
                    return path;
            }

            path = PathOps.CombinePath(
                null, GlobalState.GetAssemblyPath(), DllName.Utility);

            if (File.Exists(path))
                return path;

            lock (syncRoot)
            {
                return strictPath ? null : path;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if !NATIVE_UTILITY_BSTR
        private static int[] ToLengthArray(
            StringList list
            )
        {
            if (list == null)
                return null;

            int count = list.Count;
            int[] result = new int[count];

            for (int index = 0; index < count; index++)
            {
                string element = list[index];

                if (element == null)
                    continue;

                result[index] = element.Length;
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeEnableReflection(
            bool reset
            )
        {
            if (reset)
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    itemsFieldInfo = null;
                    noReflection = false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string[] ToStringArray(
            StringList list
            )
        {
            if (list == null)
                return null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!noReflection)
                {
                    try
                    {
                        if (itemsFieldInfo == null)
                        {
                            itemsFieldInfo = itemsType.GetField(
                                itemsFieldName, itemsBindingFlags);
                        }

                        if (itemsFieldInfo != null)
                        {
                            object value = itemsFieldInfo.GetValue(list);

                            if (value is string[])
                                return (string[])value;
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeUtility).Name,
                            TracePriority.NativeError);
                    }
                }
            }

            return list.ToArray();
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool InitializeNativeDelegates(
            bool clear
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (nativeDelegates == null)
                    nativeDelegates = new TypeDelegateDictionary();
                else if (clear)
                    nativeDelegates.Clear();

                nativeDelegates.Add(typeof(Eagle_GetVersion), null);
                nativeDelegates.Add(typeof(Eagle_AllocateMemory), null);
                nativeDelegates.Add(typeof(Eagle_FreeMemory), null);
                nativeDelegates.Add(typeof(Eagle_FreeElements), null);
                nativeDelegates.Add(typeof(Eagle_SplitList), null);
                nativeDelegates.Add(typeof(Eagle_JoinList), null);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void UnsetNativeDelegates()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                nativeGetVersion = null;
                nativeAllocateMemory = null;
                nativeFreeMemory = null;
                nativeFreeElements = null;
                nativeSplitList = null;
                nativeJoinList = null;

                RuntimeOps.UnsetNativeDelegates(nativeDelegates, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetNativeDelegates(
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((RuntimeOps.SetNativeDelegates(
                        "utility API", nativeModule, nativeDelegates,
                        null, ref error) == ReturnCode.Ok) &&
                    (nativeDelegates != null))
                {
                    try
                    {
                        nativeGetVersion = (Eagle_GetVersion)
                            nativeDelegates[typeof(Eagle_GetVersion)];

                        nativeAllocateMemory = (Eagle_AllocateMemory)
                            nativeDelegates[typeof(Eagle_AllocateMemory)];

                        nativeFreeMemory = (Eagle_FreeMemory)
                            nativeDelegates[typeof(Eagle_FreeMemory)];

                        nativeFreeElements = (Eagle_FreeElements)
                            nativeDelegates[typeof(Eagle_FreeElements)];

                        nativeSplitList = (Eagle_SplitList)
                            nativeDelegates[typeof(Eagle_SplitList)];

                        nativeJoinList = (Eagle_JoinList)
                            nativeDelegates[typeof(Eagle_JoinList)];

                        return true;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool LoadNativeLibrary(
            Interpreter interpreter
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (nativeModule != IntPtr.Zero)
                    return true;

                try
                {
                    string fileName = GetNativeLibraryFileName(interpreter);

                    if (!String.IsNullOrEmpty(fileName))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: using file name {0}",
                            FormatOps.WrapOrNull(fileName)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeDebug);
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: file name {0} is invalid",
                            FormatOps.WrapOrNull(fileName)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);

                        return false;
                    }

                    //
                    // NOTE: Check if the native library file name actually
                    //       exists.  If not, do nothing and return failure
                    //       after tracing the issue.
                    //
                    if (!File.Exists(fileName))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: file name {0} does not exist",
                            FormatOps.WrapOrNull(fileName)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);

                        return false;
                    }

                    //
                    // BUGFIX: Stop loading "untrusted" native libraries
                    //         when running with a "trusted" core library.
                    //
                    if (!RuntimeOps.ShouldLoadNativeLibrary(fileName))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: file name {0} is untrusted",
                            FormatOps.WrapOrNull(fileName)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);

                        return false;
                    }

                    int lastError;

                    nativeModule = NativeOps.LoadLibrary(
                        fileName, out lastError); /* throw */

                    if (nativeModule != IntPtr.Zero)
                    {
                        InitializeNativeDelegates(true);

                        Result error = null;

                        if (SetNativeDelegates(ref error))
                        {
                            nativeFileName = fileName;

                            TraceOps.DebugTrace(
                                "LoadNativeLibrary: successfully loaded",
                                typeof(NativeUtility).Name,
                                TracePriority.NativeDebug);

                            return true;
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "LoadNativeLibrary: file name {0} delegate " +
                                "setup error: {1}",
                                FormatOps.WrapOrNull(fileName), error),
                                typeof(NativeUtility).Name,
                                TracePriority.NativeError);

                            /* IGNORED */
                            UnloadNativeLibrary(interpreter);
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadLibrary({1}) failed with error {0}: {2}",
                            lastError, FormatOps.WrapOrNull(fileName),
                            NativeOps.GetDynamicLoadingError(lastError).Trim()),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeUtility).Name,
                        TracePriority.NativeError);
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool UnloadNativeLibrary(
            Interpreter interpreter /* NOT USED */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (nativeModule == IntPtr.Zero)
                    return true;

                try
                {
                    UnsetNativeDelegates();

                    int lastError;

                    if (NativeOps.FreeLibrary(
                            nativeModule, out lastError)) /* throw */
                    {
                        nativeModule = IntPtr.Zero;
                        nativeFileName = null;

                        TraceOps.DebugTrace(
                            "UnloadNativeLibrary: successfully unloaded",
                            typeof(NativeUtility).Name,
                            TracePriority.NativeDebug);

                        return true;
                    }
                    else
                    {
                        TraceOps.DebugTrace(String.Format(
                            "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                            lastError, nativeModule,
                            NativeOps.GetDynamicLoadingError(lastError).Trim()),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeUtility).Name,
                        TracePriority.NativeError);
                }

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ExitLock(
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

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        // NOTE: Used by the _Hosts.Default.WriteEngineInfo method.
        //
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    bool empty = HostOps.HasEmptyContent(detailFlags);
                    StringPairList localList = new StringPairList();

                    if (empty || (isAvailable != null))
                        localList.Add("IsAvailable", (isAvailable != null) ?
                            isAvailable.ToString() : FormatOps.DisplayNull);

                    if (empty || locked)
                        localList.Add("Locked", locked.ToString());

                    if (empty || disabled)
                        localList.Add("Disabled", disabled.ToString());

                    if (empty || strictPath)
                        localList.Add("StrictPath", strictPath.ToString());

                    if (empty || noReflection)
                        localList.Add("NoReflection", noReflection.ToString());

                    if (empty || (nativeModule != IntPtr.Zero))
                        localList.Add("NativeModule", nativeModule.ToString());

                    if (empty || (nativeFileName != null))
                        localList.Add("NativeFileName", (nativeFileName != null) ?
                            nativeFileName : FormatOps.DisplayNull);

                    if (empty || ((nativeDelegates != null) && (nativeDelegates.Count > 0)))
                        localList.Add("NativeDelegates", (nativeDelegates != null) ?
                            nativeDelegates.Count.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeGetVersion != null))
                        localList.Add("NativeGetVersion", (nativeGetVersion != null) ?
                            nativeGetVersion.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeAllocateMemory != null))
                        localList.Add("NativeAllocateMemory", (nativeAllocateMemory != null) ?
                            nativeAllocateMemory.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeFreeMemory != null))
                        localList.Add("NativeFreeMemory", (nativeFreeMemory != null) ?
                            nativeFreeMemory.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeFreeElements != null))
                        localList.Add("NativeFreeElements", (nativeFreeElements != null) ?
                            nativeFreeElements.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeSplitList != null))
                        localList.Add("NativeSplitList", (nativeSplitList != null) ?
                            nativeSplitList.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeJoinList != null))
                        localList.Add("NativeJoinList", (nativeJoinList != null) ?
                            nativeJoinList.ToString() : FormatOps.DisplayNull);

                    if (empty || (version != null))
                        localList.Add("Version", (version != null) ?
                            version : FormatOps.DisplayNull);

                    if (empty || (itemsFieldInfo != null))
                        localList.Add("ItemsFieldInfo", (itemsFieldInfo != null) ?
                            itemsFieldInfo.ToString() : FormatOps.DisplayNull);

                    if (empty || Parser.UseNativeSplitList)
                        localList.Add("UseNativeSplitList",
                            Parser.UseNativeSplitList.ToString());

                    if (empty || GenericOps<string>.UseNativeJoinList)
                        localList.Add("UseNativeJoinList",
                            GenericOps<string>.UseNativeJoinList.ToString());

                    if (localList.Count > 0)
                    {
                        list.Add((IPair<string>)null);
                        list.Add("Native Utility");
                        list.Add((IPair<string>)null);
                        list.Add(localList);
                    }
                }
                else
                {
                    StringPairList localList = new StringPairList();

                    localList.Add(FormatOps.DisplayBusy);

                    if (localList.Count > 0)
                    {
                        list.Add((IPair<string>)null);
                        list.Add("Native Utility");
                        list.Add((IPair<string>)null);
                        list.Add(localList);
                    }
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAvailable(
            Interpreter interpreter
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    if (isAvailable == null)
                    {
                        //
                        // NOTE: If loading the native utility library has
                        //       been temporarily locked out, return false to
                        //       indicate that it is temporarily unavailable.
                        //       Do nothing else.  That way, it may become
                        //       available later after being unlocked.
                        //
                        if (locked)
                            return false;

                        //
                        // NOTE: If loading the native utility library has
                        //       been prohibited, mark it as "permanently"
                        //       unavailable and return now.
                        //
                        bool verbose = Interpreter.IsVerbose(interpreter);

                        if (((interpreter != null) &&
                            FlagOps.HasFlags(
                                interpreter.CreateFlagsNoLock,
                                CreateFlags.NoUtility, true)) ||
                            GlobalConfiguration.DoesValueExist(
                                EnvVars.NoUtility, GlobalConfiguration.GetFlags(
                                ConfigurationFlags.NativeUtility, verbose)))
                        {
                            disabled = true; /* INFORMATIONAL */
                            return (bool)(isAvailable = false);
                        }

                        //
                        // NOTE: If loading the native utility library fails,
                        //       mark it as "permanently" unavailable.  This
                        //       must be done; otherwise, we will try to load
                        //       it everytime a list needs to be joined or
                        //       split, potentially slowing things down rather
                        //       significantly.
                        //
                        if (!LoadNativeLibrary(interpreter))
                            return (bool)(isAvailable = false);

                        IntPtr pVersion = IntPtr.Zero;

                        if ((nativeFreeMemory != null) &&
                            (nativeGetVersion != null))
                        {
                            try
                            {
                                pVersion = nativeGetVersion();

                                if (pVersion != IntPtr.Zero)
                                {
                                    version = Marshal.PtrToStringUni(
                                        pVersion);

                                    if (IsUsable(version))
                                    {
                                        MaybeEnableReflection(false);
                                        isAvailable = true;
                                    }
                                    else
                                    {
                                        version = null;
                                        isAvailable = false;
                                    }
                                }
                                else
                                {
                                    version = null;
                                    isAvailable = false;
                                }
                            }
                            catch
                            {
                                //
                                // NOTE: Prevent an exception during the native
                                //       function call from causing this check
                                //       to be repeated [forever] in the future.
                                //
                                version = null;
                                isAvailable = false;

                                //
                                // NOTE: Next, re-throw the exception (i.e. to
                                //       be caught by the outer catch block).
                                //
                                throw;
                            }
                            finally
                            {
                                if (pVersion != IntPtr.Zero)
                                {
                                    nativeFreeMemory(pVersion);
                                    pVersion = IntPtr.Zero;
                                }
                            }
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "IsAvailable: one or more required " +
                                "functions are unavailable: {0} or {1}",
                                typeof(Eagle_FreeMemory).Name,
                                typeof(Eagle_GetVersion).Name),
                                typeof(NativeUtility).Name,
                                TracePriority.NativeError);

                            version = null;
                            isAvailable = false;
                        }
                    }

                    return (bool)isAvailable;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeUtility).Name,
                        TracePriority.NativeError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ResetAvailable(
            Interpreter interpreter,
            bool unload,
            bool unlock
            )
        {
            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (unload && !UnloadNativeLibrary(interpreter))
                        return false;

                    if (unlock)
                        locked = false;

                    disabled = false; /* INFORMATIONAL */
                    isAvailable = null;
                    version = null;

                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(NativeUtility).Name,
                    TracePriority.NativeError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        public static string GetVersion(
            Interpreter interpreter
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (IsAvailable(interpreter))
                        return version;
                    else if (disabled)
                        return "disabled";
                    else
                        return "unavailable";
                }
                else
                {
                    return "locked";
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SplitList(
            string text,
            ref StringList list,
            ref Result error
            )
        {
            if (text == null)
            {
                error = "invalid text";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((nativeFreeMemory != null) &&
                    (nativeFreeElements != null) &&
                    (nativeSplitList != null))
                {
                    int elementCount = 0;
                    IntPtr pElementLengths = IntPtr.Zero;
                    IntPtr ppElements = IntPtr.Zero;
                    IntPtr pError = IntPtr.Zero;

                    try
                    {
                        ReturnCode code = nativeSplitList(
                            text.Length, text, ref elementCount,
                            ref pElementLengths, ref ppElements,
                            ref pError);

                        if (code != ReturnCode.Ok)
                        {
                            error = Marshal.PtrToStringUni(pError);
                            return code;
                        }

                        if (elementCount < 0)
                        {
                            error = String.Format(
                                "bad number of elements in list: {0}",
                                elementCount);

                            return ReturnCode.Error;
                        }

                        if (list != null)
                            list.Capacity += elementCount;
                        else
                            list = new StringList(elementCount);

                        for (int index = 0; index < elementCount; index++)
                        {
                            int elementOffset = index * IntPtr.Size;

                            if (elementOffset < 0)
                            {
                                error = String.Format(
                                    "bad list element {0} offset: {1}",
                                    index, elementOffset);

                                return ReturnCode.Error;
                            }

                            IntPtr pElement = Marshal.ReadIntPtr(
                                ppElements, elementOffset);

                            if (pElement == IntPtr.Zero)
                            {
                                list.Add(String.Empty);
                                continue;
                            }

                            int lengthOffset = index * sizeof(int);

                            if (lengthOffset < 0)
                            {
                                error = String.Format(
                                    "bad list element length {0} offset: {1}",
                                    index, lengthOffset);

                                return ReturnCode.Error;
                            }

                            int elementLength = Marshal.ReadInt32(
                                pElementLengths, lengthOffset);

                            if (elementLength < 0)
                            {
                                error = String.Format(
                                    "bad number of characters in list element: {0}",
                                    elementLength);

                                return ReturnCode.Error;
                            }

                            list.Add(Marshal.PtrToStringUni(pElement,
                                elementLength));
                        }

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        #region Free Error String
                        if (pError != IntPtr.Zero)
                        {
                            nativeFreeMemory(pError);
                            pError = IntPtr.Zero;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Free Element Array
                        if (ppElements != IntPtr.Zero)
                        {
                            nativeFreeElements(elementCount, ppElements);
                            ppElements = IntPtr.Zero;
                            elementCount = 0;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Free Element Lengths Array
                        if (pElementLengths != IntPtr.Zero)
                        {
                            nativeFreeMemory(pElementLengths);
                            pElementLengths = IntPtr.Zero;
                        }
                        #endregion
                    }
                }
                else
                {
                    error = String.Format(
                        "one or more required functions are unavailable: " +
                        "{0}, {1}, or {2}", typeof(Eagle_FreeMemory).Name,
                        typeof(Eagle_FreeElements).Name,
                        typeof(Eagle_SplitList).Name);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode JoinList(
            StringList list,
            ref string text,
            ref Result error
            )
        {
            if (list == null)
            {
                error = "invalid list";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((nativeFreeMemory != null) && (nativeJoinList != null))
                {
                    IntPtr pText = IntPtr.Zero;
                    IntPtr pError = IntPtr.Zero;

                    try
                    {
                        int length = 0;

#if NATIVE_UTILITY_BSTR
                        ReturnCode code = nativeJoinList(
                            list.Count, null, ToStringArray(list),
                            ref length, ref pText, ref pError);
#else
                        ReturnCode code = nativeJoinList(
                            list.Count, ToLengthArray(list),
                            ToStringArray(list), ref length,
                            ref pText, ref pError);
#endif

                        if (code != ReturnCode.Ok)
                        {
                            error = Marshal.PtrToStringUni(pError);
                            return code;
                        }

                        if (length < 0)
                        {
                            error = String.Format(
                                "bad number of characters in string: {0}",
                                length);

                            return ReturnCode.Error;
                        }

                        text = Marshal.PtrToStringUni(pText, length);
                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        #region Free Error String
                        if (pError != IntPtr.Zero)
                        {
                            nativeFreeMemory(pError);
                            pError = IntPtr.Zero;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Free Text String
                        if (pText != IntPtr.Zero)
                        {
                            nativeFreeMemory(pText);
                            pText = IntPtr.Zero;
                        }
                        #endregion
                    }
                }
                else
                {
                    error = String.Format(
                        "one or more required functions are unavailable: " +
                        "{0} or {1}", typeof(Eagle_FreeMemory).Name,
                        typeof(Eagle_JoinList).Name);
                }
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
