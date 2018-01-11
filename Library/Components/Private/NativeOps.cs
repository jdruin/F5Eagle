/*
 * NativeOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !NATIVE
#error "This file cannot be compiled or used properly with native code disabled."
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;

#if WINDOWS || UNIX
using Eagle._Components.Private.Delegates;
#endif

using Eagle._Containers.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("c8e735d9-c893-4da3-9845-51c8479f4d53")]
    internal static class NativeOps
    {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Safe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("317a3846-f0c5-4da6-89bc-185a51bb7015")]
        internal static class SafeNativeMethods
        {
#if WINDOWS
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetCurrentProcess();

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetCurrentThread();

#if false
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern int GetCurrentThreadId();
#endif

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsDebuggerPresent();
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6dc268be-697f-41a1-98a2-be2ce602bdfe")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS
            #region Windows Dynamic Loading Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern uint GetModuleFileName(IntPtr module, IntPtr fileName, uint size);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr GetModuleHandle(string fileName);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetDllDirectory(string directory);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr LoadLibrary(string fileName);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeLibrary(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi, even on Unicode OS. */
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr GetProcAddress(IntPtr module, string name);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Error Message Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern uint FormatMessage(
                FormatMessageFlags flags, IntPtr source, uint messageId, uint languageId,
                ref IntPtr buffer, uint size, IntPtr arguments);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr LocalFree(IntPtr handle);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Mutex Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true,
                SetLastError = true)]
            internal static extern IntPtr CreateMutex(IntPtr securityAttributes,
                [MarshalAs(UnmanagedType.Bool)] bool initialOwner, string name);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Logging Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void OutputDebugString(string outputString);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Event Methods
            #region Dead Code
#if DEAD_CODE
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetEvent(IntPtr handle);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ResetEvent(IntPtr handle);
#endif
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Handle Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr handle);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Process Methods
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetProcessAffinityMask(IntPtr process,
                ref IntPtr processAffinityMask, ref IntPtr systemAffinityMask);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [ObjectId("07983ce3-bac0-48af-b98b-b88eac97cc08")]
            internal enum PROCESSINFOCLASS
            {
                ProcessBasicInformation
                // ...
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("bc739fdb-8d62-482d-a529-ba64bbfa388d")]
            internal struct PROCESS_BASIC_INFORMATION
            {
                public /* NTSTATUS */ int ExitStatus;
                public /* PPEB */ IntPtr PebBaseAddress;
                public /* KAFFINITY */ IntPtr AffinityMask;
                public /* KPRIORITY */ int BasePriority;
                public /* HANDLE */ IntPtr UniqueProcessId;
                public /* HANDLE */ IntPtr InheritedFromUniqueProcessId;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.NtDll,
                CallingConvention = CallingConvention.StdCall)]
            internal static extern int NtQueryInformationProcess(
                /* HANDLE */ IntPtr process,
                PROCESSINFOCLASS processInformationClass,
                /* PVOID */ ref PROCESS_BASIC_INFORMATION processInformation,
                /* ULONG */ uint processInformationLength,
                /* PULONG */ ref uint returnLength
            );
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Threading Methods
            /* NOTE: Needed for use with QueueUserAPC. */
            internal const uint THREAD_SET_CONTEXT = 0x10;

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr OpenThread(uint desiredAccess,
                [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint threadId);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint QueueUserAPC(ApcCallback proc, IntPtr thread, IntPtr data);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Memory Methods
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("ad38df98-9796-41ee-99b0-8543b52ca6a7")]
            internal struct MEMORYSTATUSEX
            {
                public uint dwLength;
                public uint dwMemoryLoad;
                public ulong ullTotalPhys;
                public ulong ullAvailPhys;
                public ulong ullTotalPageFile;
                public ulong ullAvailPageFile;
                public ulong ullTotalVirtual;
                public ulong ullAvailVirtual;
                public ulong ullAvailExtendedVirtual;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern void ZeroMemory(IntPtr pMemory, uint size);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX memoryStatus);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows String Formatting Methods
#if MONO || MONO_HACKS
            [DllImport(DllName.MsVcRt, EntryPoint = "_snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int msvc_snprintf_double(StringBuilder buffer, UIntPtr count,
                string format, double value);
#endif

            [DllImport(DllName.MsVcRt, EntryPoint = "_snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int msvc_snprintf(StringBuilder buffer, UIntPtr count,
                string format, __arglist);
            #endregion
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
            #region Unix Dynamic Loading Constants
            //
            // BUGBUG: These values are probably only portable to Linux.
            //
            internal const int RTLD_LAZY = 0x1;     /* Bind function calls lazily */
            internal const int RTLD_NOW = 0x2;      /* Bind function calls immediately */
            internal const int RTLD_GLOBAL = 0x100; /* Make symbols globally available */
            internal const int RTLD_LOCAL = 0x000;  /* Opposite of RTLD_GLOBAL, and the default */
            internal const int RTLD_DEFMODE = RTLD_NOW | RTLD_GLOBAL;

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // BUGBUG: These values are probably only portable to Linux.
            //
            internal static readonly IntPtr RTLD_DEFAULT = IntPtr.Zero; /* Global symbol search */
            internal static readonly IntPtr RTLD_NEXT = new IntPtr(-1); /* Get Next symbol */
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Methods (libc)
            //
            // NOTE: Some systems, such as FreeBSD and OpenBSD, seem to have these in "libc";
            //       however, you cannot actually try to declare them from there because you
            //       get the "stub" versions.  Therefore, we assume they are already globally
            //       available in the process.
            //
            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.Internal, EntryPoint = "dlopen",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libc_dlopen(string fileName, int mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal, EntryPoint = "dlclose",
                CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            internal static extern int libc_dlclose(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.Internal, EntryPoint = "dlsym",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libc_dlsym(IntPtr module, string name);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Internal, EntryPoint = "dlerror",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr libc_dlerror();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Methods (libdl)
            //
            // NOTE: Some systems, such as Linux, seem to have these in "libdl".
            //
            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.LibDL, EntryPoint = "dlopen",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libdl_dlopen(string fileName, int mode);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibDL, EntryPoint = "dlclose",
                CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
            internal static extern int libdl_dlclose(IntPtr module);

            ///////////////////////////////////////////////////////////////////////////////////////////

            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.LibDL, EntryPoint = "dlsym",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr libdl_dlsym(IntPtr module, string name);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibDL, EntryPoint = "dlerror",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr libdl_dlerror();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Process Methods
            [DllImport(DllName.Internal, EntryPoint = "getppid")]
            internal static extern int getppid();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix String Formatting Methods
#if MONO || MONO_HACKS
            [DllImport(DllName.LibC, EntryPoint = "snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int ansi_snprintf_double(StringBuilder buffer, UIntPtr count,
                string format, double value);
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibC, EntryPoint = "snprintf",
                CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int ansi_snprintf(StringBuilder buffer, UIntPtr count,
                string format, __arglist);
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Dynamic Loading Delegates (Private Static Data)
            internal static readonly object syncRoot = new object();

            internal static dlopen dlopen = null;
            internal static dlclose dlclose = null;
            internal static dlsym dlsym = null;
            internal static dlerror dlerror = null;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Logging Constants
            internal const int LOG_EMERG = 0;          /* system is unusable */
            internal const int LOG_ALERT = 1;          /* action must be taken immediately */
            internal const int LOG_CRIT = 2;           /* critical conditions */
            internal const int LOG_ERR = 3;            /* error conditions */
            internal const int LOG_WARNING = 4;        /* warning conditions */
            internal const int LOG_NOTICE = 5;         /* normal but significant condition */
            internal const int LOG_INFO = 6;           /* informational */
            internal const int LOG_DEBUG = 7;          /* debug-level messages */

            ///////////////////////////////////////////////////////////////////////////////////////////

            internal const int LOG_USER = (1 << 3);    /* random user-level messages */
            internal const int LOG_LOCAL0 = (16 << 3); /* reserved for local use */
            internal const int LOG_LOCAL1 = (17 << 3); /* reserved for local use */
            internal const int LOG_LOCAL2 = (18 << 3); /* reserved for local use */
            internal const int LOG_LOCAL3 = (19 << 3); /* reserved for local use */
            internal const int LOG_LOCAL4 = (20 << 3); /* reserved for local use */
            internal const int LOG_LOCAL5 = (21 << 3); /* reserved for local use */
            internal const int LOG_LOCAL6 = (22 << 3); /* reserved for local use */
            internal const int LOG_LOCAL7 = (23 << 3); /* reserved for local use */
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix System Logging Methods
            /* NOTE: Always Ansi on Unix. */
            [DllImport(DllName.LibC, CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void syslog(int priority, string outputString, string argument);

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.LibC, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr strerror(int error);
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
#if WINDOWS
        //
        // NOTE: This is the successful value for the NTSTATUS data type.
        //
        private const int STATUS_SUCCESS = 0;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly IntPtr IntPtrOne = new IntPtr(1);

        ///////////////////////////////////////////////////////////////////////////////////////////////

#pragma warning disable 649 // NOTE: Yes, this is by design.
        private static readonly GCHandle invalidGCHandle;
#pragma warning restore 649
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are not read-only.
        //
        private static string MonoMemoryCategoryName = "Mono Memory";
        private static string MonoTotalPhysicalMemoryCounterName = "Total Physical Memory";
        private static string MonoAvailablePhysicalMemoryCounterName = "Available Physical Memory";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the static performance
        //       counters (below) that are [only] used to track physical memory
        //       on Mono.
        //
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool once = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These performance counters are only used on Mono for tracking
        //       available and total physical memory.
        //
        private static PerformanceCounter totalPhysicalMemoryCounter = null;
        private static PerformanceCounter availablePhysicalMemoryCounter = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Windows-Only Abstraction Methods (ALWAYS FAIL ON NON-WINDOWS)
#if WINDOWS
        public static StringList GetProcessorAffinityMasks()
        {
            ReturnCode code;
            StringList list = null;
            Result error = null;

            code = WindowsGetProcessorAffinityMasks(ref list, ref error);

            return (code == ReturnCode.Ok) ? list : new StringList(error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr OpenThread(
            uint desiredAccess,
            bool inheritHandle,
            uint threadId,
            ref Result error
            )
        {
            //
            // NOTE: We must double-check the platform because this is a public
            //       API (i.e. it is called from outside the platform abstraction
            //       methods in this class).
            //
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    IntPtr thread = UnsafeNativeMethods.OpenThread(
                        desiredAccess, inheritHandle, threadId);

                    if (IsValidHandle(thread))
                    {
                        return thread;
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();

                        error = String.Format(
                            "OpenThread({1}) failed with error {0}: {2}",
                            lastError, threadId, GetDynamicLoadingError(
                            lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool QueueUserApc(
            ApcCallback proc,
            IntPtr thread,
            IntPtr data,
            ref Result error
            )
        {
            //
            // NOTE: We must double-check the platform because this is a public
            //       API (i.e. it is called from outside the platform abstraction
            //       methods in this class).
            //
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    uint result = UnsafeNativeMethods.QueueUserAPC(
                        proc, thread, data);

                    return ConversionOps.ToBool(result);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [MethodImpl(
            MethodImplOptions.NoInlining
#if (NET_20_SP2 || NET_40) && !MONO_LEGACY
            | MethodImplOptions.NoOptimization
#endif
        )]
        public static ReturnCode ZeroMemory(
            IntPtr pMemory,
            uint size,
            ref Result error
            )
        {
            //
            // NOTE: We must double-check the platform because this is a public
            //       API (i.e. it is called from outside the platform abstraction
            //       methods in this class).
            //
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    UnsafeNativeMethods.ZeroMemory(pMemory, size);
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Windows Specific Methods (DO NOT CALL)
#if WINDOWS
        private static void WindowsInitializeMemoryStatus(
            out UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus
            )
        {
            memoryStatus = new UnsafeNativeMethods.MEMORYSTATUSEX();

            memoryStatus.dwLength = (uint)Marshal.SizeOf(
                typeof(UnsafeNativeMethods.MEMORYSTATUSEX));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsGetMemoryStatus(
            ref UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return UnsafeNativeMethods.GlobalMemoryStatusEx(ref memoryStatus);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode WindowsGetProcessorAffinityMasks(
            ref StringList list,
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    IntPtr processAffinityMask = IntPtr.Zero;
                    IntPtr systemAffinityMask = IntPtr.Zero;

                    if (UnsafeNativeMethods.GetProcessAffinityMask(
                            ProcessOps.GetHandle(), ref processAffinityMask,
                            ref systemAffinityMask))
                    {
                        list = new StringList(
                            "process", FormatOps.Hexadecimal(processAffinityMask, true),
                            "system", FormatOps.Hexadecimal(systemAffinityMask, true));

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();

                        error = String.Format(
                            "GetProcessAffinityMask() failed with error {0}: {1}",
                            lastError, GetErrorMessage(lastError));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr WindowsGetParentProcessId()
        {
            try
            {
                UnsafeNativeMethods.PROCESS_BASIC_INFORMATION processInformation =
                    new UnsafeNativeMethods.PROCESS_BASIC_INFORMATION();

                uint returnLength = 0;

                if (UnsafeNativeMethods.NtQueryInformationProcess(
                        SafeNativeMethods.GetCurrentProcess(),
                        UnsafeNativeMethods.PROCESSINFOCLASS.ProcessBasicInformation,
                        ref processInformation, (uint)Marshal.SizeOf(processInformation),
                        ref returnLength) == STATUS_SUCCESS)
                {
                    return processInformation.InheritedFromUniqueProcessId;
                }
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WindowsGetErrorMessage(
            int error
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                FormatMessageFlags flags =
                    FormatMessageFlags.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                    FormatMessageFlags.FORMAT_MESSAGE_IGNORE_INSERTS |
                    FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM;

                if (UnsafeNativeMethods.FormatMessage(
                        flags, IntPtr.Zero, ConversionOps.ToUInt(error),
                        0, ref handle, 0, IntPtr.Zero) != 0)
                {
                    return Marshal.PtrToStringAuto(handle);
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    UnsafeNativeMethods.LocalFree(handle);
                    handle = IntPtr.Zero;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WindowsGetDynamicLoadingError(
            int error
            )
        {
            if (InitializeDynamicLoading())
                return GetErrorMessage(error);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsInitializeDynamicLoading()
        {
            //
            // NOTE: Nothing really to do in here for Windows.
            //
            if (!once)
            {
                TraceOps.DebugTrace(String.Format(
                    "WindowsInitializeDynamicLoading: {0}running on Windows",
                    PlatformOps.IsWindowsOperatingSystem() ? String.Empty : "not "),
                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                once = true;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static uint WindowsGetModuleFileName(
            IntPtr module,
            IntPtr fileName,
            uint size
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.GetModuleFileName(module, fileName, size);

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr WindowsGetModuleHandle(
            string fileName
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.GetModuleHandle(fileName);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsSetDllDirectory(
            string directory
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.SetDllDirectory(directory);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr WindowsLoadLibrary(
            string fileName
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.LoadLibrary(fileName);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsFreeLibrary(
            IntPtr module
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.FreeLibrary(module);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr WindowsGetProcAddress(
            IntPtr module,
            string name
            )
        {
            if (InitializeDynamicLoading())
                return UnsafeNativeMethods.GetProcAddress(module, name);

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WindowsOutputDebugMessage(
            string message
            )
        {
            if (message != null)
            {
                try
                {
                    UnsafeNativeMethods.OutputDebugString(message);

                    return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Unix Specific Methods (DO NOT CALL)
#if UNIX
        private static IntPtr UnixGetParentProcessId()
        {
            try
            {
                return new IntPtr(UnsafeNativeMethods.getppid());
            }
            catch
            {
                // do nothing.
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string UnixGetErrorMessage(
            int error
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                handle = UnsafeNativeMethods.strerror(error);

                if (handle != IntPtr.Zero)
                    return Marshal.PtrToStringAnsi(handle);
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string UnixGetDynamicLoadingError(
            int error /* NOT USED */
            )
        {
            if (InitializeDynamicLoading())
            {
                dlerror dlerror;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlerror = UnsafeNativeMethods.dlerror;
                }

                if (dlerror != null)
                {
                    IntPtr handle = dlerror();

                    if (handle != IntPtr.Zero)
                        return Marshal.PtrToStringAnsi(handle);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool UnixInitializeDynamicLoading()
        {
            lock (UnsafeNativeMethods.syncRoot)
            {
                if ((UnsafeNativeMethods.dlopen == null) ||
                    (UnsafeNativeMethods.dlclose == null) ||
                    (UnsafeNativeMethods.dlsym == null) ||
                    (UnsafeNativeMethods.dlerror == null))
                {
                    IntPtr module = IntPtr.Zero;

                    try
                    {
                        try
                        {
                            //
                            // HACK: Attempt to determine if we should be using the "libdl"
                            //       version of dlopen or the ones from "libc".
                            //
                            module = UnsafeNativeMethods.libdl_dlopen(
                                null, UnsafeNativeMethods.RTLD_DEFMODE); /* throw */
                        }
                        catch
                        {
                            // do nothing.
                        }

                        //
                        // NOTE: Did we manage to get a valid module handle (i.e. we did not
                        //       throw an exception and the function returned something that
                        //       looks valid)?
                        //
                        if (module != IntPtr.Zero)
                        {
                            if (!once)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "UnixInitializeDynamicLoading: {0}running on Unix, " +
                                    "using dlopen from libdl",
                                    PlatformOps.IsUnixOperatingSystem() ? String.Empty : "not "),
                                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                                once = true;
                            }

                            //
                            // NOTE: Setup our delegates to use the "libdl" functions.
                            //
                            if (UnsafeNativeMethods.dlopen == null)
                                UnsafeNativeMethods.dlopen = UnsafeNativeMethods.libdl_dlopen;

                            if (UnsafeNativeMethods.dlclose == null)
                                UnsafeNativeMethods.dlclose = UnsafeNativeMethods.libdl_dlclose;

                            if (UnsafeNativeMethods.dlsym == null)
                                UnsafeNativeMethods.dlsym = UnsafeNativeMethods.libdl_dlsym;

                            if (UnsafeNativeMethods.dlerror == null)
                                UnsafeNativeMethods.dlerror = UnsafeNativeMethods.libdl_dlerror;
                        }
                        else
                        {
                            if (!once)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "UnixInitializeDynamicLoading: {0}running on Unix, " +
                                    "using dlopen from __Internal",
                                    PlatformOps.IsUnixOperatingSystem() ? String.Empty : "not "),
                                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                                once = true;
                            }

                            //
                            // NOTE: Using the "libdl" dlopen() did not work.  Either this
                            //       platform does not put the dlopen() function there or it
                            //       does not work at all.  Either way, setup our delegates
                            //       to use the "libc" functions.  If these [later] fail, we
                            //       will assume that dynamic loading is somehow broken on
                            //       this platform (at least from inside Mono).
                            //
                            if (UnsafeNativeMethods.dlopen == null)
                                UnsafeNativeMethods.dlopen = UnsafeNativeMethods.libc_dlopen;

                            if (UnsafeNativeMethods.dlclose == null)
                                UnsafeNativeMethods.dlclose = UnsafeNativeMethods.libc_dlclose;

                            if (UnsafeNativeMethods.dlsym == null)
                                UnsafeNativeMethods.dlsym = UnsafeNativeMethods.libc_dlsym;

                            if (UnsafeNativeMethods.dlerror == null)
                                UnsafeNativeMethods.dlerror = UnsafeNativeMethods.libc_dlerror;
                        }
                    }
                    finally
                    {
                        //
                        // NOTE: Ok, the test "libdl" dlopen() worked, close
                        //       the module handle we got now.
                        //
                        if (module != IntPtr.Zero)
                        {
                            if (UnsafeNativeMethods.libdl_dlclose(module) == 0)
                            {
                                module = IntPtr.Zero;
                            }
                            else
                            {
                                //
                                // NOTE: This could be bad.  Report it to the user.
                                //
                                int lastError = Marshal.GetLastWin32Error();

                                DebugOps.Complain(ReturnCode.Error, String.Format(
                                    "libdl_dlclose(0x{1:X}) failed with error {0}",
                                    lastError, module));
                            }
                        }
                    }
                }
            }

            //
            // NOTE: Currently, this method cannot "fail"; however, that does
            //       not necessarily mean that subsequent calls into the Unix
            //       dynamic loading subsystem will actually succeed.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static uint UnixGetModuleFileName(
            IntPtr module,
            IntPtr fileName,
            uint size
            )
        {
            //
            // NOTE: There is no standard implementation of this function on
            //       Unix.
            //
            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr UnixGetModuleHandle(
            string fileName
            )
        {
            //
            // NOTE: There is no standard implementation of this function on
            //       Unix.
            //
            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool UnixSetDllDirectory(
            string directory
            )
        {
            //
            // NOTE: There is no standard implementation of this function on
            //       Unix.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr UnixLoadLibrary(
            string fileName
            )
        {
            if (InitializeDynamicLoading())
            {
                dlopen dlopen;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlopen = UnsafeNativeMethods.dlopen;
                }

                if (dlopen != null)
                    return dlopen(fileName, UnsafeNativeMethods.RTLD_DEFMODE);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool UnixFreeLibrary(
            IntPtr module
            )
        {
            if (InitializeDynamicLoading())
            {
                dlclose dlclose;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlclose = UnsafeNativeMethods.dlclose;
                }

                if (dlclose != null)
                {
                    //
                    // NOTE: The "dlclose" function is supposed to
                    //       return zero upon success.
                    //
                    return (dlclose(module) == 0);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr UnixGetProcAddress(
            IntPtr module,
            string name
            )
        {
            if (InitializeDynamicLoading())
            {
                dlsym dlsym;

                lock (UnsafeNativeMethods.syncRoot)
                {
                    dlsym = UnsafeNativeMethods.dlsym;
                }

                if (dlsym != null)
                    return dlsym(module, name);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool UnixOutputDebugMessage(
            string message
            )
        {
            if (message != null)
            {
                try
                {
                    UnsafeNativeMethods.syslog(
                        UnsafeNativeMethods.LOG_DEBUG,
                        FormatOps.StringInputFormat, message);

                    return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Mono Specific Methods (DO NOT CALL)
#if MONO || MONO_HACKS
        private static ReturnCode MonoPrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref int returnValue,  /* in, out */
            ref Result error      /* out */
            )
        {
            try
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UnsafeNativeMethods.msvc_snprintf_double(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, value);

                    return ReturnCode.Ok;
                }
#endif

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UnsafeNativeMethods.ansi_snprintf_double(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, value);

                    return ReturnCode.Ok;
                }
#endif

                error = "unknown operating system";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MonoGetMemoryStatus(
            ref uint memoryLoad,
            ref ulong totalPhysical,
            ref ulong availablePhysical,
            ref Result error
            )
        {
            if (CommonOps.Runtime.IsMono())
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    try
                    {
                        if (totalPhysicalMemoryCounter == null)
                        {
                            totalPhysicalMemoryCounter = new PerformanceCounter(
                                MonoMemoryCategoryName,
                                MonoTotalPhysicalMemoryCounterName);
                        }

                        if (availablePhysicalMemoryCounter == null)
                        {
                            availablePhysicalMemoryCounter = new PerformanceCounter(
                                MonoMemoryCategoryName,
                                MonoAvailablePhysicalMemoryCounterName);
                        }

                        totalPhysical = ConversionOps.ToULong(
                            totalPhysicalMemoryCounter.RawValue);

                        availablePhysical = ConversionOps.ToULong(
                            availablePhysicalMemoryCounter.RawValue);

                        ulong usedPhysical = totalPhysical - availablePhysical;

                        double percent = (totalPhysical != 0) ?
                            ((double)usedPhysical /
                                (double)totalPhysical) * 100 : 0;

                        if (percent < 0.0)
                            percent = 0.0;
                        else if (percent > 100.0)
                            percent = 100.0;

                        memoryLoad = (uint)percent;
                        return true;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
            }
            else
            {
                error = "not supported on this platform";
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Platform Abstraction Methods (DO NOT CALL)
        private static bool InitializeDynamicLoading()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsInitializeDynamicLoading();
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixInitializeDynamicLoading();
#endif

            //
            // NOTE: If we get to this point, we are running on neither
            //       Windows nor Unix; this is currently considered a
            //       failure.
            //
            if (!once)
            {
                TraceOps.DebugTrace(String.Format(
                    "InitializeDynamicLoading: running on unknown operating " +
                    "system {0} with Id {1}",
                    FormatOps.WrapOrNull(PlatformOps.GetOperatingSystemName()),
                    PlatformOps.GetOperatingSystemId()),
                    typeof(NativeOps).Name, TracePriority.NativeDebug);

                once = true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode NormalPrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref int returnValue,  /* in, out */
            ref Result error      /* out */
            )
        {
            try
            {
#if WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UnsafeNativeMethods.msvc_snprintf(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, __arglist(value));

                    return ReturnCode.Ok;
                }
#endif

#if UNIX
                if (PlatformOps.IsUnixOperatingSystem())
                {
                    //
                    // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
                    //       does not support using the C# "__arglist" keyword.
                    //       https://bugzilla.novell.com/show_bug.cgi?id=472845
                    //
                    returnValue += UnsafeNativeMethods.ansi_snprintf(buffer,
                        new UIntPtr((uint)buffer.Capacity), format, __arglist(value));

                    return ReturnCode.Ok;
                }
#endif

                error = "unknown operating system";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTotalMemory(
            ref ulong totalPhysical,
            ref Result error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus;

                WindowsInitializeMemoryStatus(out memoryStatus);

                if (WindowsGetMemoryStatus(ref memoryStatus))
                {
                    totalPhysical = memoryStatus.ullTotalPhys;
                    return ReturnCode.Ok;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "GlobalMemoryStatusEx() failed with error {0}: {1}",
                        lastError, GetErrorMessage(lastError));

                    return ReturnCode.Error;
                }
            }
#endif

            if (CommonOps.Runtime.IsMono())
            {
                uint memoryLoad = 0;
                ulong availablePhysical = 0;

                if (MonoGetMemoryStatus(
                        ref memoryLoad, ref totalPhysical,
                        ref availablePhysical, ref error))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            error = "not supported on this operating system";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetMemoryLoad(
            ref uint memoryLoad,
            ref Result error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus;

                WindowsInitializeMemoryStatus(out memoryStatus);

                if (WindowsGetMemoryStatus(ref memoryStatus))
                {
                    memoryLoad = memoryStatus.dwMemoryLoad;
                    return ReturnCode.Ok;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "GlobalMemoryStatusEx() failed with error {0}: {1}",
                        lastError, GetErrorMessage(lastError));

                    return ReturnCode.Error;
                }
            }
#endif

            if (CommonOps.Runtime.IsMono())
            {
                ulong totalPhysical = 0;
                ulong availablePhysical = 0;

                if (MonoGetMemoryStatus(
                        ref memoryLoad, ref totalPhysical,
                        ref availablePhysical, ref error))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            error = "not supported on this operating system";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Platform Abstraction Methods
        public static GCHandle GetInvalidGCHandle()
        {
            return invalidGCHandle;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode PrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref Result error      /* out */
            )
        {
            int returnValue = 0;

            return PrintDouble(buffer, format, value, ref returnValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode PrintDouble(
            StringBuilder buffer, /* in */
            string format,        /* in */
            double value,         /* in */
            ref int returnValue,  /* in, out */
            ref Result error      /* out */
            )
        {
#if MONO || MONO_HACKS
            //
            // HACK: *MONO* As of Mono 2.10.3, it seems that Mono still
            //       does not support using the C# "__arglist" keyword.
            //       https://bugzilla.novell.com/show_bug.cgi?id=472845
            //
            if (CommonOps.Runtime.IsMono())
                return MonoPrintDouble(buffer, format, value, ref returnValue, ref error);
            else
#endif
                return NormalPrintDouble(buffer, format, value, ref returnValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsValidHandle(
            IntPtr handle
            )
        {
            return RuntimeOps.IsValidHandle(handle);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsValidHandle(
            IntPtr handle,
            ref bool invalid
            )
        {
            return RuntimeOps.IsValidHandle(handle, ref invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetParentProcessId()
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetParentProcessId();
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetParentProcessId();
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode TestLoadLibrary(
            string fileName,
            ref Result error
            )
        {
            IntPtr module = IntPtr.Zero;

            try
            {
                //
                // NOTE: Attempt to dynamically load the module.  This module
                //       handle will be cleaned up in the finally block, if
                //       necessary (i.e. if it was successfully opened).
                //
                int lastError;

                module = LoadLibrary(fileName, out lastError); /* throw */

                if (IsValidHandle(module))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "LoadLibrary({1}) failed with error {0}: {2}",
                        lastError, FormatOps.WrapOrNull(fileName),
                        GetDynamicLoadingError(lastError));
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                try
                {
                    if (IsValidHandle(module))
                    {
                        int lastError;

                        if (FreeLibrary(module, out lastError)) /* throw */
                        {
                            module = IntPtr.Zero;
                        }
                        else
                        {
                            DebugOps.Complain(ReturnCode.Error, String.Format(
                                "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                                lastError, module, GetDynamicLoadingError(
                                lastError)));
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, String.Format(
                        "FreeLibrary(0x{1:X}) failed with exception: {0}",
                        e, module));
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetErrorMessage()
        {
            return GetErrorMessage(Marshal.GetLastWin32Error());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetErrorMessage(
            int error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetErrorMessage(error);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetErrorMessage(error);
#endif

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetDynamicLoadingError(
            int error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetDynamicLoadingError(error);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetDynamicLoadingError(error);
#endif

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static uint GetModuleFileName(
            IntPtr module,
            IntPtr fileName,
            uint size
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetModuleFileName(module, fileName, size);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetModuleFileName(module, fileName, size);
#endif

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetModuleHandle(
            string fileName
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsGetModuleHandle(fileName);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixGetModuleHandle(fileName);
#endif

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool SetDllDirectory(
            string directory,
            out int lastError
            )
        {
            lastError = 0;

            bool result = false;

#if WINDOWS
            if (!result && PlatformOps.IsWindowsOperatingSystem())
                result = WindowsSetDllDirectory(directory);
#endif

#if UNIX
            if (!result && PlatformOps.IsUnixOperatingSystem())
                result = UnixSetDllDirectory(directory);
#endif

            if (!result)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "SetDllDirectory: directory = {0}, result = {1}, lastError = {2}",
                FormatOps.WrapOrNull(directory), result, lastError),
                typeof(NativeOps).Name, TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr LoadLibrary(
            string fileName,
            out int lastError
            )
        {
            lastError = 0;

            IntPtr result = IntPtr.Zero;

#if WINDOWS
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsWindowsOperatingSystem())
            {
                result = WindowsLoadLibrary(fileName);
            }
#endif

#if UNIX
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsUnixOperatingSystem())
            {
                result = UnixLoadLibrary(fileName);
            }
#endif

            if (result == IntPtr.Zero)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "LoadLibrary: fileName = {0}, result = {1}, lastError = {2}",
                FormatOps.WrapOrNull(fileName), result, lastError),
                typeof(NativeOps).Name, TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool FreeLibrary(
            IntPtr module,
            out int lastError
            )
        {
            lastError = 0;

            bool result = false;

#if WINDOWS
            if (!result && PlatformOps.IsWindowsOperatingSystem())
                result = WindowsFreeLibrary(module);
#endif

#if UNIX
            if (!result && PlatformOps.IsUnixOperatingSystem())
                result = UnixFreeLibrary(module);
#endif

            if (!result)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "FreeLibrary: module = {0}, result = {1}, lastError = {2}",
                module, result, lastError), typeof(NativeOps).Name,
                TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetProcAddress(
            IntPtr module,
            string name,
            out int lastError
            )
        {
            lastError = 0;

            IntPtr result = IntPtr.Zero;

#if WINDOWS
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsWindowsOperatingSystem())
            {
                result = WindowsGetProcAddress(module, name);
            }
#endif

#if UNIX
            if ((result == IntPtr.Zero) &&
                PlatformOps.IsUnixOperatingSystem())
            {
                result = UnixGetProcAddress(module, name);
            }
#endif

            if (result == IntPtr.Zero)
                lastError = Marshal.GetLastWin32Error();

            TraceOps.DebugTrace(String.Format(
                "GetProcAddress: module = {0}, name = {1}, " +
                "result = {2}, lastError = {3}",
                module, FormatOps.WrapOrNull(name), result, lastError),
                typeof(NativeOps).Name, (result != IntPtr.Zero) ?
                    TracePriority.NativeDebug2 :
                    TracePriority.NativeDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool OutputDebugMessage(
            string message
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
                return WindowsOutputDebugMessage(message);
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
                return UnixOutputDebugMessage(message);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetTotalMemory(
            ref ulong totalPhysical
            )
        {
            ReturnCode code;
            Result error = null;

            code = GetTotalMemory(ref totalPhysical, ref error);

            if (code == ReturnCode.Ok)
                return true;

#if DEBUG && VERBOSE
            DebugOps.Complain(code, error);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetMemoryLoad(
            ref uint memoryLoad
            )
        {
            ReturnCode code;
            Result error = null;

            code = GetMemoryLoad(ref memoryLoad, ref error);

            if (code == ReturnCode.Ok)
                return true;

#if DEBUG && VERBOSE
            DebugOps.Complain(code, error);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMemoryStatus(
            ref StringList list,
            ref Result error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.MEMORYSTATUSEX memoryStatus;

                WindowsInitializeMemoryStatus(out memoryStatus);

                if (WindowsGetMemoryStatus(ref memoryStatus))
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "memoryLoad",
                        memoryStatus.dwMemoryLoad.ToString(),
                        "totalPhysical",
                        memoryStatus.ullTotalPhys.ToString(),
                        "availablePhysical",
                        memoryStatus.ullAvailPhys.ToString(),
                        "totalPageFile",
                        memoryStatus.ullTotalPageFile.ToString(),
                        "availablePageFile",
                        memoryStatus.ullAvailPageFile.ToString(),
                        "totalVirtual",
                        memoryStatus.ullTotalVirtual.ToString(),
                        "availableVirtual",
                        memoryStatus.ullAvailVirtual.ToString(),
                        "availableExtendedVirtual",
                        memoryStatus.ullAvailExtendedVirtual.ToString());

                    return ReturnCode.Ok;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "GlobalMemoryStatusEx() failed with error {0}: {1}",
                        lastError, GetErrorMessage(lastError));

                    return ReturnCode.Error;
                }
            }
#endif

            if (CommonOps.Runtime.IsMono())
            {
                uint memoryLoad = 0;
                ulong totalPhysical = 0;
                ulong availablePhysical = 0;

                if (MonoGetMemoryStatus(
                        ref memoryLoad, ref totalPhysical,
                        ref availablePhysical, ref error))
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "memoryLoad", memoryLoad.ToString(),
                        "totalPhysical", totalPhysical.ToString(),
                        "availablePhysical", availablePhysical.ToString());

                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            error = "not supported on this operating system";
            return ReturnCode.Error;
        }
        #endregion
    }
}
