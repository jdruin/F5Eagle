/*
 * PlatformOps.cs --
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

#if NATIVE && WINDOWS
using System.Diagnostics;
#endif

using System.IO;
using System.Runtime.InteropServices;

#if NATIVE
using System.Security;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

#if NATIVE && UNIX
using System.Text;
#endif

using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
#if NATIVE
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("751e3aab-6f53-4a0d-bc13-f1ab217ef7dc")]
    internal static class PlatformOps
    {
        #region Private Constants
        #region Windows 10 Update Constants
        //
        // TODO: Windows 10 build numbers for various "updates".
        //
        private const int Windows10NovemberUpdateBuildNumber = 10586;
        private const string Windows10NovemberUpdateName = "November Update";

        private const int Windows10AnniversaryUpdateBuildNumber = 14393;
        private const string Windows10AnniversaryUpdateName = "Anniversary Update";

        private const int Windows10CreatorsUpdateBuildNumber = 15063;
        private const string Windows10CreatorsUpdateName = "Creators Update";

        private const int Windows10FallCreatorsUpdateBuildNumber = 16299;
        private const string Windows10FallCreatorsUpdateName = "Fall Creators Update";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Windows Version Registry Constants
        private const string OsVersionSubKeyName =
            "Software\\Microsoft\\Windows NT\\CurrentVersion";

        private const string ProductNameValueName = "ProductName";
        private const string CurrentTypeValueName = "CurrentType";
        private const string InstallationTypeValueName = "InstallationType";
        private const string BuildLabExValueName = "BuildLabEx";
        private const string InstallDateValueName = "InstallDate";
        private const string UpdateNamesValueName = "UpdateNames";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WMI Query (for Windows Update) Constants
        //
        // HACK: These are purposely not read-only.
        //
        private static string WmiQfeGetUpdatesCommandFileName =
            "%SystemRoot%\\System32\\wbem\\wmic.exe"; // BUGBUG: Constant?

        private static string WmiQfePropertyName = "HotFixID";

        private static string WmiQfeGetUpdatesCommandArguments =
            "QFE GET HotFixID";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        private const uint defaultPageSize = 4096; /* COMPAT: x86. */

        ///////////////////////////////////////////////////////////////////////

        private static readonly string UnknownName = "unknown";

        ///////////////////////////////////////////////////////////////////////

        private static StringList processorNames = null;
        private static StringList platformNames = null;
        private static StringList operatingSystemNames = null;

        ///////////////////////////////////////////////////////////////////////

        private static IDictionary<string, string> machineNames = null;

        ///////////////////////////////////////////////////////////////////////

        private static Regex majorMinorRegEx = new Regex("^\\d+\\.\\d+");

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        private static IDictionary<string, string> alternateProcessorNames = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();
        private static bool initialized = false;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static UnsafeNativeMethods.SYSTEM_INFO systemInfo;
        private static UnsafeNativeMethods.OSVERSIONINFOEX versionInfo;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
        private static readonly char[] utsNameSeparators = {
            Characters.Null
        };

        private static UnsafeNativeMethods.utsname utsName;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static ProcessorArchitecture processorArchitecture =
            ProcessorArchitecture.Unknown;

        private static OperatingSystemId operatingSystemId =
            OperatingSystemId.Unknown;

        private static uint pageSize = defaultPageSize;

        private static IntPtr minimumApplicationAddress = IntPtr.Zero;
        private static IntPtr maximumApplicationAddress = IntPtr.Zero;

        ///////////////////////////////////////////////////////////////////////

        private static OperatingSystem operatingSystem = null;

        ///////////////////////////////////////////////////////////////////////

        private static string processorName = null;
        private static string machineName = null;
        private static string platformName = null;
        private static string operatingSystemName = null;
        private static string operatingSystemVersion = null;
        private static string operatingSystemServicePack = null;

        ///////////////////////////////////////////////////////////////////////

        private static bool isWin32onWin64 = false;

        ///////////////////////////////////////////////////////////////////////

        private static StringList installedUpdates = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("259417ba-e318-4982-b2c0-9f6fd4196b74")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("a858d038-1313-43d2-a9b0-7a00b2975933")]
            internal struct SYSTEM_INFO
            {
                public ProcessorArchitecture wProcessorArchitecture;
                public ushort wReserved;
                public uint dwPageSize;
                public IntPtr lpMinimumApplicationAddress;
                public IntPtr lpMaximumApplicationAddress;
                public UIntPtr dwActiveProcessorMask;
                public uint dwNumberOfProcessors;
                public uint dwProcessorType;
                public uint dwAllocationGranularity;
                public ushort wProcessorLevel;
                public ushort wProcessorRevision;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Yes, this has been tested and the size must be exactly
            //       148 bytes.
            //
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            [ObjectId("f33f3aa7-8ddc-48a0-8ac9-e950dcbaaac1")]
            internal struct OSVERSIONINFOEX
            {
                public uint dwOSVersionInfoSize;
                public uint dwMajorVersion;
                public uint dwMinorVersion;
                public uint dwBuildNumber;
                public OperatingSystemId dwPlatformId;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string szCSDVersion;
                public ushort wServicePackMajor;
                public ushort wServicePackMinor;
                public short wSuiteMask;
                public byte wProductType;
                public byte wReserved;
            }

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern void GetSystemInfo(
                ref SYSTEM_INFO systemInfo
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetVersionEx(
                ref OSVERSIONINFOEX versionInfo
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsWow64Process(
                IntPtr hProcess,
                [MarshalAs(UnmanagedType.Bool)]
                ref bool wow64Process
            );
#endif

            ///////////////////////////////////////////////////////////////////

#if UNIX
            [ObjectId("4c41ee57-ee1d-4db6-8735-a4d78dd810b9")]
            internal struct utsname
            {
                public string sysname;  /* Name of this implementation of
                                         * the operating system. */
                public string nodename; /* Name of this node within the
                                         * communications network to which
                                         * this node is attached, if any. */
                public string release;  /* Current release level of this
                                         * implementation. */
                public string version;  /* Current version level of this
                                         * release. */
                public string machine;  /* Name of the hardware type on
                                         * which the system is running. */
            }

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("86669ff8-3031-46e1-a51b-6a0b837c0c14")]
            internal struct utsname_interop
            {
                //
                // NOTE: The following string fields should be present in
                //       this buffer, all of which will be zero-terminated:
                //
                //                      sysname
                //                      nodename
                //                      release
                //                      version
                //                      machine
                //
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
                public byte[] buffer;
            }

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal,
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern int uname(out utsname_interop name);
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static PlatformOps()
        {
            Initialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Initialization Methods
        public static void Initialize()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (initialized)
                    return;

                ///////////////////////////////////////////////////////////////

                if (processorNames == null)
                {
                    processorNames = new StringList(new string[] {
                        "intel", "mips", "alpha", "ppc", "shx", "arm",
                        "ia64", "alpha64", "msil", "amd64", "ia32_on_win64",
                        "neutral", "arm64"
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (platformNames == null)
                {
                    platformNames = new StringList(new string[] {
                        "windows", "windows", "windows", "windows", "unix",
                        "windows", "unix"
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (operatingSystemNames == null)
                {
                    operatingSystemNames = new StringList(new string[] {
                        "Win32s", "Windows 9x", "Windows NT", "Windows CE",
                        "Unix", "Xbox", "Darwin"
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (machineNames == null)
                {
                    machineNames = new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

                    machineNames.Add("i386", "intel");
                    machineNames.Add("i486", "intel");
                    machineNames.Add("i586", "intel");
                    machineNames.Add("i686", "intel");
                    machineNames.Add("Win32", "intel");
                    machineNames.Add("x86", "intel");
                    machineNames.Add("Win64", "amd64"); /* HACK */
                    machineNames.Add("x86_64", "amd64");
                    machineNames.Add("Itanium", "ia64");

                    if (processorNames != null)
                    {
                        foreach (string name in processorNames)
                        {
                            if (name == null)
                                continue;

                            machineNames[name] = name; /* IDENTITY */
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////

#if NATIVE
                if (alternateProcessorNames == null)
                {
                    alternateProcessorNames = new Dictionary<string, string>(
                        StringComparer.OrdinalIgnoreCase);

                    alternateProcessorNames.Add("Intel", "x86");
                    alternateProcessorNames.Add("Win32", "x86");
                    alternateProcessorNames.Add("x86", "x86");
                    alternateProcessorNames.Add("ia32_on_win64", "x86");

                    ///////////////////////////////////////////////////////////

                    alternateProcessorNames.Add("ARM", "arm");
                    alternateProcessorNames.Add("ARM64", "arm64");

                    ///////////////////////////////////////////////////////////

                    alternateProcessorNames.Add("Win64", "x64"); /* HACK */
                    alternateProcessorNames.Add("AMD64", "x64");
                    alternateProcessorNames.Add("x64", "x64");
                    alternateProcessorNames.Add("x86_64", "x64");

                    ///////////////////////////////////////////////////////////

                    alternateProcessorNames.Add("Itanium", "IA64");
                    alternateProcessorNames.Add("IA64", "IA64");
                }
#endif

                ///////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
                if (GetSystemInfo(ref systemInfo))
                {
                    //
                    // NOTE: What is the processor architecture that we are
                    //       executing on?
                    //
                    processorArchitecture = systemInfo.wProcessorArchitecture;

                    //
                    // NOTE: What is the native memory page size?
                    //
                    pageSize = systemInfo.dwPageSize;

                    //
                    // NOTE: What is the range of memory addresses that can
                    //       be used for applications?
                    //
                    minimumApplicationAddress =
                        systemInfo.lpMinimumApplicationAddress;

                    maximumApplicationAddress =
                        systemInfo.lpMaximumApplicationAddress;
                }
#endif

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What is the name of the processor that we are
                //       executing on?
                //
                processorName = GetProcessorName(processorArchitecture, false);

                //
                // NOTE: What is the name of the "machine" that we are
                //       executing on?  This is based on the processor
                //       name; however, it may or may not be the same
                //       value as the processor name.
                //
                machineName = GetMachineName(processorName, false, true);

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What is the operating system that we are executing on?
                //
                operatingSystem = Environment.OSVersion;

                ///////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
                if (GetOsVersionInfo(ref versionInfo))
                {
                    //
                    // NOTE: What is the platform we are executing on?
                    //
                    operatingSystemId = versionInfo.dwPlatformId;
                }
                else
#endif
                {
                    operatingSystemId = (OperatingSystemId)
                        GetOperatingSystemPlatformId();
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What is the name of the platform that we are executing
                //       on?
                //
                if (operatingSystemId != OperatingSystemId.Unknown)
                {
                    platformName = GetPlatformName(operatingSystemId, false);
                }
                else
                {
                    platformName = GetPlatformName(
                        (OperatingSystemId)GetOperatingSystemPlatformId(),
                        false);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: What is the name of the operating system that we are
                //       executing on?
                //
                operatingSystemName = GetOperatingSystemName(
                    operatingSystemId, false);

                operatingSystemVersion = null;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Check if this process is running as Win32-on-Win64
                //       (WoW64).
                //
#if NATIVE && WINDOWS
                isWin32onWin64 = IsWin32onWin64();
#else
                isWin32onWin64 = false;
#endif

                ///////////////////////////////////////////////////////////////

#if NATIVE && UNIX
                if (GetOsVersionInfo(ref utsName))
                {
                    //
                    // NOTE: What is the name of the processor that we are
                    //       executing on?
                    //
                    processorName = utsName.machine;

                    //
                    // NOTE: What is the name of the "machine" that we are
                    //       executing on?  This is based on the processor
                    //       name; however, it may or may not be the same
                    //       value as the processor name.
                    //
                    machineName = GetMachineName(processorName, false, true);

                    //
                    // NOTE: What is the name of the platform that we are
                    //       executing on?
                    //
                    platformName = TclVars.Platform.UnixValue;

                    //
                    // NOTE: What is the name of the operating system that
                    //       we are executing on?
                    //
                    operatingSystemName = utsName.sysname;

                    //
                    // NOTE: What is the version of the operating system
                    //       that we are executing on?
                    //
                    operatingSystemVersion = utsName.release;

                    //
                    // NOTE: What is the extra version information for the
                    //       operating system that we are executing on?
                    //
                    operatingSystemServicePack = utsName.version;
                }
#endif

                ///////////////////////////////////////////////////////////////

                initialized = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Querying Methods
        public static ProcessorArchitecture GetProcessorArchitecture()
        {
            lock (syncRoot)
            {
                return processorArchitecture;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static uint GetPageSize()
        {
            lock (syncRoot)
            {
                return pageSize;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetApplicationAddressRange()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return String.Format("{0}-{1}",
                    FormatOps.Hexadecimal(
                        minimumApplicationAddress.ToInt64(), true),
                    FormatOps.Hexadecimal(
                        maximumApplicationAddress.ToInt64(), true));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetMachineName()
        {
            lock (syncRoot)
            {
                return machineName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetProcessorName()
        {
            lock (syncRoot)
            {
                return processorName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPlatformName()
        {
            lock (syncRoot)
            {
                return platformName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static OperatingSystem GetOperatingSystem()
        {
            lock (syncRoot)
            {
                return operatingSystem;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static PlatformID GetOperatingSystemPlatformId()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return (operatingSystem != null) ? operatingSystem.Platform :
                    (PlatformID)(int)OperatingSystemId.Unknown;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static Version GetOperatingSystemVersion()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return (operatingSystem != null) ?
                    operatingSystem.Version : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemPatchLevel()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystemVersion != null)
                    return operatingSystemVersion;

                Version osVersion = GetOperatingSystemVersion();

                return (osVersion != null) ? osVersion.ToString() : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemMajorMinor()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystemVersion != null)
                {
                    if (majorMinorRegEx != null)
                    {
                        Match match = majorMinorRegEx.Match(
                            operatingSystemVersion);

                        if ((match != null) && match.Success)
                            return match.Value;
                    }

                    return null;
                }

                Version osVersion = GetOperatingSystemVersion();

                return FormatOps.MajorMinor(osVersion);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemServicePack()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (operatingSystemServicePack != null)
                    return operatingSystemServicePack;

#if NATIVE && WINDOWS
                return FormatOps.MajorMinor(
                    GlobalState.GetTwoPartVersion(
                        versionInfo.wServicePackMajor,
                        versionInfo.wServicePackMinor));
#else
                return (operatingSystem != null) ?
                    operatingSystem.ServicePack : null;
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemExtra(
            Interpreter interpreter, /* in */
            bool asynchronous        /* in: WARNING, Non-zero is expensive. */
            )
        {
            string productName = null;
            string currentType = null;
            string installationType = null;
            string buildLabEx = null;
            int installDate = 0;

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        OsVersionSubKeyName)) /* throw */
                {
                    try
                    {
                        productName = key.GetValue(
                            ProductNameValueName) as string; /* throw */
                    }
                    catch (Exception e2)
                    {
                        TraceOps.DebugTrace(
                            e2, typeof(PlatformOps).Name,
                            TracePriority.PlatformError);
                    }

                    ///////////////////////////////////////////////////////////

                    try
                    {
                        currentType = key.GetValue(
                            CurrentTypeValueName) as string; /* throw */
                    }
                    catch (Exception e2)
                    {
                        TraceOps.DebugTrace(
                            e2, typeof(PlatformOps).Name,
                            TracePriority.PlatformError);
                    }

                    ///////////////////////////////////////////////////////////

                    try
                    {
                        installationType = key.GetValue(
                            InstallationTypeValueName) as string; /* throw */
                    }
                    catch (Exception e2)
                    {
                        TraceOps.DebugTrace(
                            e2, typeof(PlatformOps).Name,
                            TracePriority.PlatformError);
                    }

                    ///////////////////////////////////////////////////////////

                    try
                    {
                        buildLabEx = key.GetValue(
                            BuildLabExValueName) as string; /* throw */
                    }
                    catch (Exception e2)
                    {
                        TraceOps.DebugTrace(
                            e2, typeof(PlatformOps).Name,
                            TracePriority.PlatformError);
                    }

                    ///////////////////////////////////////////////////////////

                    try
                    {
                        installDate = (int)key.GetValue(
                            InstallDateValueName); /* throw */
                    }
                    catch (Exception e2)
                    {
                        TraceOps.DebugTrace(
                            e2, typeof(PlatformOps).Name,
                            TracePriority.PlatformError);
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PlatformOps).Name,
                    TracePriority.PlatformError);
            }

            ///////////////////////////////////////////////////////////////////

            StringList list = new StringList();

            list.Add(ProductNameValueName);
            list.Add(productName);
            list.Add(CurrentTypeValueName);
            list.Add(currentType);
            list.Add(InstallationTypeValueName);
            list.Add(installationType);
            list.Add(BuildLabExValueName);
            list.Add(buildLabEx);

            ///////////////////////////////////////////////////////////////////

            StringList updateNames = new StringList();
            Version osVersion = null;

            if (IsWindows10OrHigher(ref osVersion))
            {
                string updateName = GetWindows10UpdateName(osVersion);

                if (updateName != null)
                    updateNames.Add(updateName);
            }

            if (asynchronous)
            {
                StringList installedUpdates = GetInstalledUpdates(
                    interpreter);

                if (installedUpdates != null)
                    updateNames.AddRange(installedUpdates);
            }

            list.Add(UpdateNamesValueName);
            list.Add(updateNames.ToString());

            ///////////////////////////////////////////////////////////////////

            DateTime installDateTime = DateTime.MinValue;

            list.Add(InstallDateValueName);

            if ((installDate != 0) && TimeOps.UnixSecondsToDateTime(
                    installDate, ref installDateTime))
            {
                list.Add(FormatOps.Iso8601DateTime(
                    installDateTime, true));
            }
            else
            {
                list.Add((string)null);
            }

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PopulateOperatingSystemExtra(
            Interpreter interpreter,
            string varName,
            string varIndex
            )
        {
            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                if (interpreter != null)
                {
                    ReturnCode code;
                    Result result = null;

                    try
                    {
                        string varValue = GetOperatingSystemExtra(
                            interpreter, true); /* WARNING: ~10 secs... */

                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            if (!Interpreter.IsDeletedOrDisposed(interpreter))
                            {
                                code = interpreter.SetLibraryVariableValue2(
                                    VariableFlags.None, varName, varIndex,
                                    varValue, ref result);
                            }
                            else
                            {
                                result = Engine.InterpreterUnusableError;
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        result = e;
                        code = ReturnCode.Error;
                    }

                    if (code != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, code, result);
                }
            });
        }

        ///////////////////////////////////////////////////////////////////////

        public static OperatingSystemId GetOperatingSystemId()
        {
            lock (syncRoot)
            {
                return operatingSystemId;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemName()
        {
            lock (syncRoot)
            {
                return operatingSystemName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetWin32onWin64()
        {
            lock (syncRoot)
            {
                return isWin32onWin64;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetUserName(
            bool domain
            )
        {
            string result = Environment.UserName;

            if (!domain)
                return result;

            return Environment.UserDomainName +
                Path.DirectorySeparatorChar + result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Operating System Detection Support Methods
        public static bool IsUnixOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                return ((operatingSystemId == OperatingSystemId.Unix) ||
                    (operatingSystemId == OperatingSystemId.Darwin) ||
                    (operatingSystemId == OperatingSystemId.Mono_on_Unix));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsMacintoshOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                return (operatingSystemId == OperatingSystemId.Darwin);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsOperatingSystem()
        {
            // lock (syncRoot) /* EXEMPT: Possible hot-path (read-only). */
            {
                return ((operatingSystemId == OperatingSystemId.Windows9x) ||
                    (operatingSystemId == OperatingSystemId.WindowsNT));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows81()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows81(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10OrHigher()
        {
            Version osVersion = null; /* NOT USED */

            return IsWindows10OrHigher(ref osVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10NovemberUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10NovemberUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10AnniversaryUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10AnniversaryUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10CreatorsUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            //
            // BUGBUG: The language in MSDN seems to strongly imply that
            //         the build number must be an exact match for the
            //         associated .NET Framework version to be inclued
            //         with the operating system; therefore, use the
            //         "equal to" operator here, not the "greater than
            //         or equal to" operator.
            //
            if ((osVersion != null) &&
                (osVersion.Build == Windows10CreatorsUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindows10FallCreatorsUpdate()
        {
            Version osVersion = null;

            if (!IsWindows10OrHigher(ref osVersion))
                return false;

            if ((osVersion != null) &&
                (osVersion.Build == Windows10FallCreatorsUpdateBuildNumber))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool CheckVersion(
            PlatformID platformId,
            int major,
            int minor,
            short servicePackMajor,
            short servicePackMinor
            )
        {
            if (GetOperatingSystemPlatformId() == platformId)
            {
                Version osVersion = GetOperatingSystemVersion();

                if (osVersion != null)
                {
                    if (osVersion.Major > major)
                    {
                        return true;
                    }
                    else if ((osVersion.Major == major) &&
                        (osVersion.Minor > minor))
                    {
                        return true;
                    }
                    else if ((osVersion.Major == major) &&
                        (osVersion.Minor == minor))
                    {
                        ushort osServicePackMajor = versionInfo.wServicePackMajor;
                        ushort osServicePackMinor = versionInfo.wServicePackMinor;

                        if (osServicePackMajor > servicePackMajor)
                        {
                            return true;
                        }
                        else if ((osServicePackMajor == servicePackMajor) &&
                            (osServicePackMinor >= servicePackMinor))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Process Bits Querying Methods
        public static int GetProcessBits() // (e.g. 32, 64, etc)
        {
            return (IntPtr.Size * ConversionOps.ByteBits);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Is32BitProcess()
        {
            return (IntPtr.Size == sizeof(uint));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Is64BitProcess()
        {
            return (IntPtr.Size == sizeof(ulong));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Name Lookup Methods
#if NATIVE
        public static string GetAlternateProcessorName(
            string platformOrProcessorName,
            bool nullIfNotFound,
            bool unknownIfNotFound
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                string processorName;

                if ((platformOrProcessorName != null) &&
                    (alternateProcessorNames != null) &&
                    alternateProcessorNames.TryGetValue(
                        platformOrProcessorName, out processorName))
                {
                    return processorName;
                }
            }

            if (nullIfNotFound)
                return null;

            if (unknownIfNotFound)
                return UnknownName;

            return platformOrProcessorName;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Operating System Detection Support Methods
        private static bool IsWindows81(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if ((osVersion.Major == 6) && (osVersion.Minor == 3))
                        return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWindows10OrHigher(
            ref Version osVersion
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((operatingSystem != null) &&
                    (operatingSystem.Platform == PlatformID.Win32NT))
                {
                    osVersion = operatingSystem.Version;

                    if (osVersion.Major >= 10) /* Windows 10 = 10.0 */
                        return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringList GetInstalledUpdates(
            Interpreter interpreter
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (installedUpdates != null)
                    return new StringList(installedUpdates);
            }

            //
            // HACK: For now, full introspection of installed updates is
            //       only supported on Windows.
            //
            if (!IsWindowsOperatingSystem())
                return null;

            try
            {
                EventFlags eventFlags = (interpreter != null) ?
                    interpreter.EngineEventFlags : EventFlags.None;

                ExitCode exitCode = ResultOps.SuccessExitCode();
                ReturnCode code;
                Result result = null;
                Result error = null;

                code = ProcessOps.ExecuteProcess(
                    interpreter, CommonOps.Environment.ExpandVariables(
                        WmiQfeGetUpdatesCommandFileName),
                    WmiQfeGetUpdatesCommandArguments, eventFlags,
                    ref exitCode, ref result, ref error);

                if ((code == ReturnCode.Ok) &&
                    (exitCode == ResultOps.SuccessExitCode()))
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        installedUpdates = StringList.FromString(result);

                        if (installedUpdates != null)
                        {
                            if ((installedUpdates.Count > 0) && String.Equals(
                                    installedUpdates[0], WmiQfePropertyName,
                                    StringOps.SystemNoCaseStringComparisonType))
                            {
                                installedUpdates.RemoveAt(0);
                            }

                            if (installedUpdates.Count == 0)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "GetInstalledUpdates: missing, code = {0}, " +
                                    "exitCode = {1}, result = {2}, error = {3}",
                                    code, exitCode, FormatOps.WrapOrNull(true,
                                    true, result), FormatOps.WrapOrNull(true,
                                    true, error)), typeof(PlatformOps).Name,
                                    TracePriority.PlatformDebug);
                            }

                            return new StringList(installedUpdates);
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "GetInstalledUpdates: invalid, code = {0}, " +
                                "exitCode = {1}, result = {2}, error = {3}",
                                code, exitCode, FormatOps.WrapOrNull(true,
                                true, result), FormatOps.WrapOrNull(true,
                                true, error)), typeof(PlatformOps).Name,
                                TracePriority.PlatformDebug);
                        }
                    }
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetInstalledUpdates: failure, code = {0}, " +
                        "exitCode = {1}, result = {2}, error = {3}",
                        code, exitCode, FormatOps.WrapOrNull(true,
                        true, result), FormatOps.WrapOrNull(true,
                        true, error)), typeof(PlatformOps).Name,
                        TracePriority.PlatformError);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PlatformOps).Name,
                    TracePriority.PlatformError);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: This method and its caller assume that there can be only
        //         one "named update" installed at a time.
        //
        private static string GetWindows10UpdateName(
            Version osVersion
            )
        {
            if (osVersion != null)
            {
                switch (osVersion.Build)
                {
                    case Windows10NovemberUpdateBuildNumber:
                        return Windows10NovemberUpdateName;
                    case Windows10AnniversaryUpdateBuildNumber:
                        return Windows10AnniversaryUpdateName;
                    case Windows10CreatorsUpdateBuildNumber:
                        return Windows10CreatorsUpdateName;
                    case Windows10FallCreatorsUpdateBuildNumber:
                        return Windows10FallCreatorsUpdateName;
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Name Lookup Methods
        private static string GetMachineName(
            string platformOrProcessorName,
            bool nullIfNotFound,
            bool unknownIfNotFound
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                string machineName;

                if ((platformOrProcessorName != null) &&
                    (machineNames != null) &&
                    machineNames.TryGetValue(
                        platformOrProcessorName, out machineName))
                {
                    return machineName;
                }
            }

            if (nullIfNotFound)
                return null;

            if (unknownIfNotFound)
                return UnknownName;

            return platformOrProcessorName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetProcessorName(
            ProcessorArchitecture processorArchitecture,
            bool nullIfNotFound
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                ProcessorArchitecture count = (ProcessorArchitecture)
                    processorNames.Count;

                if ((processorArchitecture >= 0) &&
                    (processorArchitecture < count))
                {
                    return processorNames[(int)processorArchitecture];
                }
            }

            return nullIfNotFound ? null : UnknownName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetOperatingSystemName(
            OperatingSystemId platformId,
            bool nullIfNotFound
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                OperatingSystemId count = (OperatingSystemId)
                    operatingSystemNames.Count;

                if ((platformId >= 0) && (platformId < count))
                {
                    return operatingSystemNames[(int)platformId];
                }
                else if (platformId == OperatingSystemId.Mono_on_Unix)
                {
                    return platformId.ToString().Replace(
                        Characters.Underscore, Characters.Space);
                }
            }

            return nullIfNotFound ? null : UnknownName;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetPlatformName(
            OperatingSystemId platformId,
            bool nullIfNotFound
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                OperatingSystemId count = (OperatingSystemId)
                    platformNames.Count;

                if ((platformId >= 0) && (platformId < count))
                    return platformNames[(int)platformId];
                else if (platformId == OperatingSystemId.Mono_on_Unix)
                    return OperatingSystemId.Unix.ToString();
            }

            return nullIfNotFound ? null : UnknownName;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Win32 Support Methods
#if NATIVE && WINDOWS
        private static bool GetSystemInfo(
            ref UnsafeNativeMethods.SYSTEM_INFO systemInfo
            )
        {
            try
            {
                /* CANNOT FAIL? */
                UnsafeNativeMethods.GetSystemInfo(ref systemInfo);

                return true;
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetOsVersionInfo(
            ref UnsafeNativeMethods.OSVERSIONINFOEX versionInfo
            )
        {
            try
            {
                versionInfo.dwOSVersionInfoSize = (uint)Marshal.SizeOf(
                    versionInfo);

                return UnsafeNativeMethods.GetVersionEx(ref versionInfo);
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWin32onWin64()
        {
            try
            {
                Process process = Process.GetCurrentProcess();

                if (process != null)
                {
                    bool wow64Process = false;

                    if (UnsafeNativeMethods.IsWow64Process(
                            process.Handle, ref wow64Process))
                    {
                        return wow64Process;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Unix Support Methods
#if NATIVE && UNIX
        private static bool GetOsVersionInfo(
            ref UnsafeNativeMethods.utsname utsName
            )
        {
            try
            {
                UnsafeNativeMethods.utsname_interop utfNameInterop;

                if (UnsafeNativeMethods.uname(out utfNameInterop) < 0)
                    return false;

                if (utfNameInterop.buffer == null)
                    return false;

                string bufferAsString = Encoding.UTF8.GetString(
                    utfNameInterop.buffer);

                if ((bufferAsString == null) || (utsNameSeparators == null))
                    return false;

                bufferAsString = bufferAsString.Trim(utsNameSeparators);

                string[] parts = bufferAsString.Split(
                    utsNameSeparators, StringSplitOptions.RemoveEmptyEntries);

                if (parts == null)
                    return false;

                UnsafeNativeMethods.utsname localUtsName =
                    new UnsafeNativeMethods.utsname();

                if (parts.Length >= 1)
                    localUtsName.sysname = parts[0];

                if (parts.Length >= 2)
                    localUtsName.nodename = parts[1];

                if (parts.Length >= 3)
                    localUtsName.release = parts[2];

                if (parts.Length >= 4)
                    localUtsName.version = parts[3];

                if (parts.Length >= 5)
                    localUtsName.machine = parts[4];

                utsName = localUtsName;
                return true;
            }
            catch
            {
                // do nothing.
            }

            return false;
        }
#endif
        #endregion
    }
}
