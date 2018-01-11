/*
 * NativeStack.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !NATIVE || !WINDOWS
#error "This file cannot be compiled or used properly with native Windows code disabled."
#endif

using System;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

/////////////////////////////////////////////////////////////////////////////////////////
// NATIVE STACK HANDLING
/////////////////////////////////////////////////////////////////////////////////////////

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("0c681582-7e68-4e41-88c8-0891f10cd484")]
    internal static class NativeStack
    {
        /////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        /////////////////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("92cecfd9-3ef3-42c1-83e2-055cd6f9dbfe")]
        private static class UnsafeNativeMethods
        {
            [ObjectId("643773c8-cf43-4d74-9559-35ce377d86f5")]
            internal enum THREADINFOCLASS
            {
                ThreadBasicInformation
                // ...
            }

            /////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("d5537405-4611-48b8-a2d4-f588f4001fcb")]
            internal struct CLIENT_ID
            {
                public /* PVOID */ IntPtr UniqueProcess;
                public /* PVOID */ IntPtr UniqueThread;
            }

            /////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("e9663a89-68cd-420e-a37e-4058e1b8e6ea")]
            internal struct THREAD_BASIC_INFORMATION
            {
                public /* NTSTATUS */ int ExitStatus;
                public /* PVOID */ IntPtr TebBaseAddress;
                public CLIENT_ID ClientId;
                public /* KAFFINITY */ IntPtr AffinityMask;
                public /* KPRIORITY */ int Priority;
                public /* KPRIORITY */ int BasePriority;
            }

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetThreadContext(
                IntPtr thread,
                IntPtr context
            );

            /////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.NtDll,
                CallingConvention = CallingConvention.StdCall)]
            internal static extern int NtQueryInformationThread(
                /* HANDLE */ IntPtr thread,
                THREADINFOCLASS threadInformationClass,
                /* PVOID */ ref THREAD_BASIC_INFORMATION threadInformation,
                /* ULONG */ uint threadInformationLength,
                /* PULONG */ ref uint returnLength
            );
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Safe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("89610a3c-5b4d-4c75-b779-426e72615f9c")]
        private static class SafeNativeMethods
        {
            [DllImport(DllName.NtDll,
                CallingConvention = CallingConvention.StdCall)]
            internal static extern IntPtr NtCurrentTeb();
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Stack Size Support Class
        [ObjectId("32de459b-ba4d-492b-8e5e-731f418e9ca1")]
        internal sealed class StackSize
        {
            #region Public Constructors
            public StackSize()
            {
                used = UIntPtr.Zero;
                allocated = UIntPtr.Zero;
                extra = UIntPtr.Zero;
                margin = UIntPtr.Zero;
                maximum = UIntPtr.Zero;

                reserve = UIntPtr.Zero;
                commit = UIntPtr.Zero;
            }
            #endregion

            /////////////////////////////////////////////////////////////////////////////

            #region Public Data
            public UIntPtr used;
            public UIntPtr allocated;
            public UIntPtr extra;
            public UIntPtr margin;
            public UIntPtr maximum;

            public UIntPtr reserve;
            public UIntPtr commit;
            #endregion

            /////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                return StringList.MakeList(
                    "used", used, "allocated", allocated, "extra", extra,
                    "margin", margin, "maximum", maximum, "reserve", reserve,
                    "commit", commit);
            }
            #endregion
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // NOTE: This is the successful value for the NTSTATUS data type.
        //
        private const int STATUS_SUCCESS = 0;

        //
        // NOTE: The number of memory pages reserved by the script engine for
        //       our safety margin (i.e. "buffer zone").  This includes enough
        //       space to cause our stack overflow logic to always trigger prior
        //       to the .NET Framework itself throwing a StackOverflowException.
        //       This value may need fine tuning and is subject to change for
        //       every new release of the .NET Framework.
        //
        private const uint StackMarginPages = 96; /* 384K on x86, 768K on x64 */

        //
        // NOTE: The script engine fallback stack reserve for all threads created
        //       via Engine.CreateThread if a larger stack reserve is not specified
        //       in the PE file header.  This value must be kept in sync with the
        //       "EagleStackSize" value in the "Eagle.Settings.targets" file.
        //
        private const ulong DefaultStackSize = 0x1000000; // 16MB

        /////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Magic offsets (in bytes) into the "undocumented" NT TEB (Thread
        //       Environment Block) structure.  We need these because that is the
        //       only reliable way to get access to the currently available stack
        //       size for the current thread.  To get these values from WinDbg use:
        //
        //          dt ntdll!_TEB TebAddr StackBase
        //          dt ntdll!_TEB TebAddr StackLimit
        //          dt ntdll!_TEB TebAddr DeallocationStack
        //
        private const uint TebStackBaseOffset32Bit = 0x04;     /* VERIFIED */
        private const uint TebStackLimitOffset32Bit = 0x08;    /* VERIFIED */
        private const uint TebDeallocationStack32Bit = 0xE0C;  /* VERIFIED */

        private const uint TebStackBaseOffset64Bit = 0x08;     /* VERIFIED */
        private const uint TebStackLimitOffset64Bit = 0x10;    /* VERIFIED */
        private const uint TebDeallocationStack64Bit = 0x1478; /* VERIFIED */

        /////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These constants are from the Platform SDK header file "WinNT.h" and
        //       are for use with the Win32 GetThreadContext API.
        //
        private const uint CONTEXT_i386 = 0x00010000;
        private const uint CONTEXT_IA64 = 0x00080000;
        private const uint CONTEXT_AMD64 = 0x00100000;
        private const uint CONTEXT_ARM = 0x00200000;
        private const uint CONTEXT_ARM64 = 0x00400000;

        private const uint CONTEXT_CONTROL_i386 = (CONTEXT_i386 | 0x00000001);
        private const uint CONTEXT_CONTROL_IA64 = (CONTEXT_IA64 | 0x00000001);
        private const uint CONTEXT_CONTROL_AMD64 = (CONTEXT_AMD64 | 0x00000001);
        private const uint CONTEXT_CONTROL_ARM = (CONTEXT_ARM | 0x00000001);
        private const uint CONTEXT_CONTROL_ARM64 = (CONTEXT_ARM64 | 0x00000001);

        /////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are offsets into the architecture specific _CONTEXT structure
        //       from the Platform SDK header file "WinNT.h".  The "VERIFIED" comment
        //       indicates that the calculation has been double-checked on an actual
        //       running system via WinDbg.
        //
        private const uint CONTEXT_FLAGS_OFFSET_i386 = 0;   /* VERIFIED */
        private const uint CONTEXT_SIZE_i386 = 716;         /* VERIFIED */
        private const uint CONTEXT_ESP_OFFSET_i386 = 196;   /* VERIFIED */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_IA64 = 0;   /* VERIFIED */
        private const uint CONTEXT_SIZE_IA64 = 2672;        /* ???????? */
        private const uint CONTEXT_ESP_OFFSET_IA64 = 2248;  /* ???????? */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_AMD64 = 48; /* VERIFIED */
        private const uint CONTEXT_SIZE_AMD64 = 1232;       /* VERIFIED */
        private const uint CONTEXT_ESP_OFFSET_AMD64 = 152;  /* VERIFIED */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_ARM = 0;    /* VERIFIED */
        private const uint CONTEXT_SIZE_ARM = 228;          /* ???????? */
        private const uint CONTEXT_ESP_OFFSET_ARM = 56;     /* ???????? */

        /////////////////////////////////////////////////////////////////////////////////

        private const uint CONTEXT_FLAGS_OFFSET_ARM64 = 0;  /* ???????? */
        private const uint CONTEXT_SIZE_ARM64 = 912;        /* ???????? */
        private const uint CONTEXT_ESP_OFFSET_ARM64 = 256;  /* ???????? */
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Thread Context Data
        //
        // NOTE: This object is only used to synchronize access to the
        //       ThreadContextBuffer static field.
        //
        private static readonly object syncRoot = new object();

        /////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: When not zero, this will point to memory that must be freed
        //       via the Marshal.FreeCoTaskMem method.  This will be handled
        //       automatically by this class using an event handler for the
        //       AppDomain.DomainUnload -OR- AppDomain.ProcessExit event,
        //       depending on whether or not this is the default AppDomain.
        //
        private static UIntPtr ThreadContextBuffer = UIntPtr.Zero;
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Thread Context Metadata
        private static int CannotQueryThread;
        private static int InvalidTeb;
        private static int TebException;
        private static int ContextException;

        private static bool CanQueryThread;

        private static uint TebStackBaseOffset;
        private static uint TebStackLimitOffset;
        private static uint TebDeallocationStack;

        private static uint CONTEXT_FLAGS_OFFSET;
        private static uint CONTEXT_CONTROL;
        private static uint CONTEXT_SIZE;
        private static uint CONTEXT_ESP_OFFSET;

        /////////////////////////////////////////////////////////////////////////////////

        private static NtCurrentTeb NtCurrentTeb = null; /* delegate */
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Thread Stack Size Data
        private static ulong NewThreadStackSize;
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static NativeStack()
        {
            SetupThreadContextMetadata();
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region AppDomain EventHandler (ProcessExit / DomainUnload)
        private static void NativeStack_Exited(
            object sender,
            EventArgs e
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (ThreadContextBuffer != UIntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(ConversionOps.ToIntPtr(
                        ThreadContextBuffer));

                    ThreadContextBuffer = UIntPtr.Zero;

                    AppDomain appDomain = AppDomainOps.GetCurrent();

                    if (appDomain != null)
                    {
                        if (!AppDomainOps.IsDefault(appDomain))
                            appDomain.DomainUnload -= NativeStack_Exited;
                        else
                            appDomain.ProcessExit -= NativeStack_Exited;
                    }
                }
            }
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Thread Context Support Methods
        private static void SetupThreadContextMetadata()
        {
            CanQueryThread = false;

            OperatingSystemId operatingSystemId = PlatformOps.GetOperatingSystemId();

            switch (operatingSystemId)
            {
                case OperatingSystemId.WindowsNT:
                    {
                        ProcessorArchitecture processorArchitecture =
                            PlatformOps.GetProcessorArchitecture();

                        switch (processorArchitecture)
                        {
                            case ProcessorArchitecture.Intel:
                            case ProcessorArchitecture.IA32_on_Win64:
                                {
                                    TebStackBaseOffset = TebStackBaseOffset32Bit;
                                    TebStackLimitOffset = TebStackLimitOffset32Bit;
                                    TebDeallocationStack = TebDeallocationStack32Bit;

                                    CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_i386;
                                    CONTEXT_CONTROL = CONTEXT_CONTROL_i386;
                                    CONTEXT_SIZE = CONTEXT_SIZE_i386;
                                    CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_i386;

                                    //
                                    // NOTE: Support is present in NTDLL, use the direct
                                    //       (fast) method.
                                    //
                                    NtCurrentTeb = null;

                                    TraceOps.DebugTrace(
                                        "selected x86 architecture",
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    CanQueryThread = true;
                                    break;
                                }
                            case ProcessorArchitecture.ARM:
                                {
                                    TebStackBaseOffset = TebStackBaseOffset32Bit;
                                    TebStackLimitOffset = TebStackLimitOffset32Bit;
                                    TebDeallocationStack = TebDeallocationStack32Bit;

                                    CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_ARM;
                                    CONTEXT_CONTROL = CONTEXT_CONTROL_ARM;
                                    CONTEXT_SIZE = CONTEXT_SIZE_ARM;
                                    CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_ARM;

                                    //
                                    // NOTE: Native stack checking is not "officially"
                                    //       supported on this architecture; however,
                                    //       it may work.
                                    //
                                    NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);

                                    TraceOps.DebugTrace(
                                        "selected ARM architecture",
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    CanQueryThread = true;
                                    break;
                                }
                            case ProcessorArchitecture.IA64:
                                {
                                    TebStackBaseOffset = TebStackBaseOffset64Bit;
                                    TebStackLimitOffset = TebStackLimitOffset64Bit;
                                    TebDeallocationStack = TebDeallocationStack64Bit;

                                    CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_IA64;
                                    CONTEXT_CONTROL = CONTEXT_CONTROL_IA64;
                                    CONTEXT_SIZE = CONTEXT_SIZE_IA64;
                                    CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_IA64;

                                    //
                                    // NOTE: Native stack checking is not "officially"
                                    //       supported on this architecture; however,
                                    //       it may work.
                                    //
                                    NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);

                                    TraceOps.DebugTrace(
                                        "selected ia64 architecture",
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    CanQueryThread = true;
                                    break;
                                }
                            case ProcessorArchitecture.AMD64:
                                {
                                    TebStackBaseOffset = TebStackBaseOffset64Bit;
                                    TebStackLimitOffset = TebStackLimitOffset64Bit;
                                    TebDeallocationStack = TebDeallocationStack64Bit;

                                    CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_AMD64;
                                    CONTEXT_CONTROL = CONTEXT_CONTROL_AMD64;
                                    CONTEXT_SIZE = CONTEXT_SIZE_AMD64;
                                    CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_AMD64;

                                    //
                                    // HACK: Thanks for not exporting this function from
                                    //       NTDLL on x64 (you know who you are).  Since
                                    //       support is not present in NTDLL, use the
                                    //       slow method.
                                    //
                                    NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);

                                    TraceOps.DebugTrace(
                                        "selected x64 architecture",
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    CanQueryThread = true;
                                    break;
                                }
                            case ProcessorArchitecture.ARM64:
                                {
                                    TebStackBaseOffset = TebStackBaseOffset64Bit;
                                    TebStackLimitOffset = TebStackLimitOffset64Bit;
                                    TebDeallocationStack = TebDeallocationStack64Bit;

                                    CONTEXT_FLAGS_OFFSET = CONTEXT_FLAGS_OFFSET_ARM64;
                                    CONTEXT_CONTROL = CONTEXT_CONTROL_ARM64;
                                    CONTEXT_SIZE = CONTEXT_SIZE_ARM64;
                                    CONTEXT_ESP_OFFSET = CONTEXT_ESP_OFFSET_ARM64;

                                    //
                                    // NOTE: Native stack checking is not "officially"
                                    //       supported on this architecture; however,
                                    //       it may work.
                                    //
                                    NtCurrentTeb = new NtCurrentTeb(NtCurrentTebSlow);

                                    TraceOps.DebugTrace(
                                        "selected ARM64 architecture",
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeDebug);

                                    CanQueryThread = true;
                                    break;
                                }
                            default:
                                {
                                    //
                                    // NOTE: We have no idea what processor architecture
                                    //       this is.  Native stack checking is disabled.
                                    //
                                    TraceOps.DebugTrace(String.Format(
                                        "unknown architecture {0}",
                                        processorArchitecture),
                                        typeof(NativeStack).Name,
                                        TracePriority.NativeError);

                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        //
                        // NOTE: We have no idea what operating system this is.
                        //       Native stack checking is disabled.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "unknown operating system {0}",
                            operatingSystemId),
                            typeof(NativeStack).Name,
                            TracePriority.NativeError);

                        break;
                    }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static IntPtr NtCurrentTebSlow()
        {
            UnsafeNativeMethods.THREAD_BASIC_INFORMATION threadInformation =
                new UnsafeNativeMethods.THREAD_BASIC_INFORMATION();

            uint returnLength = 0;

            if (UnsafeNativeMethods.NtQueryInformationThread(
                    NativeOps.SafeNativeMethods.GetCurrentThread(),
                    UnsafeNativeMethods.THREADINFOCLASS.ThreadBasicInformation,
                    ref threadInformation,
                    (uint)Marshal.SizeOf(threadInformation),
                    ref returnLength) == STATUS_SUCCESS)
            {
                return threadInformation.TebBaseAddress;
            }

            return IntPtr.Zero;
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Runtime Stack Checking Support Methods
        public static UIntPtr GetNativeStackAllocated()
        {
            UIntPtr result = UIntPtr.Zero;

            //
            // NOTE: Are we able to query the thread environment block (i.e. we
            //       know what platform we are on and the appropriate constants
            //       have been setup)?
            //
            if (CanQueryThread)
            {
                try
                {
                    IntPtr teb = IntPtr.Zero;

                    if (NtCurrentTeb != null)
                        teb = NtCurrentTeb();
                    else
                        teb = SafeNativeMethods.NtCurrentTeb();

                    if (teb != IntPtr.Zero)
                    {
                        IntPtr stackBase = Marshal.ReadIntPtr(
                            teb, (int)TebStackBaseOffset);

                        IntPtr stackLimit = Marshal.ReadIntPtr(
                            teb, (int)TebStackLimitOffset);

                        if (stackBase.ToInt64() > stackLimit.ToInt64())
                        {
                            result = new UIntPtr(ConversionOps.ToULong(
                                stackBase.ToInt64() - stackLimit.ToInt64()));
                        }
                    }
                    else
                    {
                        if (Interlocked.Increment(ref InvalidTeb) == 1)
                        {
                            TraceOps.DebugTrace(
                                "GetNativeStackAllocated: invalid TEB",
                                typeof(NativeStack).Name,
                                TracePriority.NativeError);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Interlocked.Increment(ref TebException) == 1)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeStack).Name,
                            TracePriority.NativeError);
                    }
                }
            }
            else
            {
                if (Interlocked.Increment(ref CannotQueryThread) == 1)
                {
                    TraceOps.DebugTrace(
                        "GetNativeStackAllocated: cannot query thread",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }
            }

            return result;
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr GetNativeStackMargin()
        {
            return new UIntPtr(
                (ulong)PlatformOps.GetPageSize() * (ulong)StackMarginPages);
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr GetNativeStackMaximum()
        {
            UIntPtr result = UIntPtr.Zero;

            //
            // NOTE: Are we able to query the thread environment block (i.e. we
            //       know what platform we are on and the appropriate constants
            //       have been setup)?
            //
            if (CanQueryThread)
            {
                try
                {
                    IntPtr teb = IntPtr.Zero;

                    if (NtCurrentTeb != null)
                        teb = NtCurrentTeb();
                    else
                        teb = SafeNativeMethods.NtCurrentTeb();

                    if (teb != IntPtr.Zero)
                    {
                        IntPtr stackBase = Marshal.ReadIntPtr(
                            teb, (int)TebStackBaseOffset);

                        IntPtr deallocationStack = Marshal.ReadIntPtr(
                            teb, (int)TebDeallocationStack);

                        if (stackBase.ToInt64() > deallocationStack.ToInt64())
                        {
                            result = new UIntPtr(ConversionOps.ToULong(
                                stackBase.ToInt64() - deallocationStack.ToInt64()));
                        }
                    }
                    else
                    {
                        if (Interlocked.Increment(ref InvalidTeb) == 1)
                        {
                            TraceOps.DebugTrace(
                                "GetNativeStackMaximum: invalid TEB",
                                typeof(NativeStack).Name,
                                TracePriority.NativeError);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Interlocked.Increment(ref TebException) == 1)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(NativeStack).Name,
                            TracePriority.NativeError);
                    }
                }
            }
            else
            {
                if (Interlocked.Increment(ref CannotQueryThread) == 1)
                {
                    TraceOps.DebugTrace(
                        "GetNativeStackMaximum: cannot query thread",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }
            }

            return result;
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region PE File Stack Size Support Methods
        //
        // NOTE: For use by the Engine.GetNewThreadStackSize method only.
        //
        public static ulong GetNewThreadNativeStackSize()
        {
            if (NewThreadStackSize == 0)
                NewThreadStackSize = QueryNewThreadNativeStackSize();

            return NewThreadStackSize;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static ulong QueryNewThreadNativeStackSize()
        {
            ulong result = FileOps.GetPeFileStackReserve();

            if (result < DefaultStackSize)
                result = DefaultStackSize;

            return result;
        }
        #endregion

        /////////////////////////////////////////////////////////////////////////////////

        #region Native Register Support Methods
        public static UIntPtr GetNativeStackPointer()
        {
            return GetNativeRegister(
                NativeOps.SafeNativeMethods.GetCurrentThread(),
                CONTEXT_CONTROL, (int)CONTEXT_ESP_OFFSET, IntPtr.Size);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static UIntPtr GetNativeRegister(
            IntPtr thread,
            uint flags,
            int offset,
            int size
            )
        {
            //
            // NOTE: Are we able to query the thread context (i.e. we know
            //       what platform we are on and the appropriate constants
            //       have been setup)?
            //
            if (!CanQueryThread)
            {
                if (Interlocked.Increment(ref CannotQueryThread) == 1)
                {
                    TraceOps.DebugTrace(
                        "GetNativeRegister: cannot query thread",
                        typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }

                return UIntPtr.Zero;
            }

            //
            // NOTE: We do not allow anybody to attempt to read outside what
            //       we think the bounds of the CONTEXT structure are.
            //
            if ((offset < 0) || (offset > (CONTEXT_SIZE - IntPtr.Size)))
                return UIntPtr.Zero;

            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Perform one-time allocation of the fixed-size
                    //       thread context buffer, on demand and schedule
                    //       to have it freed prior to process exit.
                    //
                    if (ThreadContextBuffer == UIntPtr.Zero)
                    {
                        //
                        // NOTE: Schedule the fixed-size thread context
                        //       buffer to be freed either upon the
                        //       AppDomain being unloaded (if we are not
                        //       in the default AppDomain) or when the
                        //       process exits.  This should gracefully
                        //       handle both embedding and stand-alone
                        //       scenarios.
                        //
                        AppDomain appDomain = AppDomainOps.GetCurrent();

                        if (appDomain != null)
                        {
                            if (!AppDomainOps.IsDefault(appDomain))
                                appDomain.DomainUnload += NativeStack_Exited;
                            else
                                appDomain.ProcessExit += NativeStack_Exited;
                        }

                        //
                        // NOTE: Now that we are sure that we have
                        //       succeeded in scheduling the cleanup for
                        //       this buffer, allocate it.
                        //
                        // NOTE: For safety, we now allocate at least a
                        //       whole page for this buffer.
                        //
                        // ThreadContextBuffer = ConversionOps.ToUIntPtr(
                        //     Marshal.AllocCoTaskMem((int)CONTEXT_SIZE));
                        //
                        ThreadContextBuffer = ConversionOps.ToUIntPtr(
                            Marshal.AllocCoTaskMem((int)Math.Max(
                                CONTEXT_SIZE, PlatformOps.GetPageSize())));
                    }

                    //
                    // NOTE: Make sure we were able to allocate the
                    //       thread context buffer.
                    //
                    if (ThreadContextBuffer == UIntPtr.Zero)
                        return UIntPtr.Zero;

                    //
                    // NOTE: Internally convert our buffer UIntPtr to
                    //       IntPtr, as required by Marshal class.
                    //       This is absolutely required because
                    //       otherwise we end up calling the generic
                    //       version of the WriteInt32 and ReadInt32
                    //       methods (that take an object instead of
                    //       an IntPtr) and getting the wrong results.
                    //
                    IntPtr threadContext = ConversionOps.ToIntPtr(
                        ThreadContextBuffer);

                    //
                    // NOTE: Write flags that tell GetThreadContext
                    //       which fields of the thread context buffer
                    //       we would like it to populate.  For now,
                    //       we mainly want to support the control
                    //       registers (primarily for ESP and EBP).
                    //
                    Marshal.WriteInt32(
                        threadContext, (int)CONTEXT_FLAGS_OFFSET,
                        (int)flags);

                    //
                    // NOTE: Query the Win32 API to obtain the
                    //       requested thread context.  In theory,
                    //       this could fail or throw an exception
                    //       at this point.  In that case, we would
                    //       return zero from this function and the
                    //       stack checking code would assume that
                    //       native stack checking is unavailable
                    //       and should not be relied upon.
                    //
                    if (UnsafeNativeMethods.GetThreadContext(
                            thread, threadContext))
                    {
                        if (size == IntPtr.Size)
                        {
                            return ConversionOps.ToUIntPtr(
                                Marshal.ReadIntPtr(threadContext,
                                offset));
                        }
                        else
                        {
                            switch (size)
                            {
                                case sizeof(long):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadInt64(threadContext,
                                            offset));
                                    }
                                case sizeof(int):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadInt32(threadContext,
                                            offset));
                                    }
                                case sizeof(short):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadInt16(threadContext,
                                            offset));
                                    }
                                case sizeof(byte):
                                    {
                                        return ConversionOps.ToUIntPtr(
                                            Marshal.ReadByte(threadContext,
                                            offset));
                                    }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Interlocked.Increment(ref ContextException) == 1)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeStack).Name,
                        TracePriority.NativeError);
                }
            }

            return UIntPtr.Zero;
        }
        #endregion
    }
}
