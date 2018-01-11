/*
 * RuntimeOps.cs --
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

#if NATIVE && (NATIVE_UTILITY || TCL)
using System.Runtime.InteropServices;
#endif

#if NATIVE && WINDOWS
using System.Security;
#endif

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;

#if !NATIVE
using System.Security.Principal;
#endif

using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Components.Shared;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Encodings;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;

namespace Eagle._Components.Private
{
    [ObjectId("52155f4f-322b-4389-aacd-166fe334d164")]
    internal static class RuntimeOps
    {
        #region Synchronization Objects
#if NATIVE && WINDOWS
        private static readonly object syncRoot = new object();
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        #region Property Value Defaults
        internal static readonly bool DefaultThrowOnFeatureNotSupported = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Binding Flags
        internal const BindingFlags DelegateBindingFlags =
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Encoding Constants
        //
        // WARNING: Do not change this as it must be a pass-through one-byte
        //          per character encoding.
        //
        private static readonly Encoding RawEncoding = OneByteEncoding.OneByte;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hash Algorithm Constants
        //
        // NOTE: *WARNING* Change this value with great care because it may
        //       break custom script, file, and stream policies that rely on
        //       the hash result.
        //
        private static readonly string DefaultHashAlgorithmName = "SHA1";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Handling
        private const string InvalidInterpreterResourceManager =
            "invalid interpreter resource manager";

        private const string InvalidPluginResourceManager =
            "invalid plugin resource manager";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Pointer Handling
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        #region Native Stack Checking
#if NATIVE && WINDOWS
        private static LocalDataStoreSlot stackPtrSlot; /* ThreadSpecificData */
        private static LocalDataStoreSlot stackSizeSlot; /* ThreadSpecificData */

        //
        // NOTE: The number of nesting levels before we start checking
        //       native stack space.
        //
        // TODO: We really need to adjust these numbers dynamically
        //       depending on the maximum stack size of the thread.
        //
        // HACK: These are no longer read-only.
        //
        private static int NoStackLevels = 100;
        private static int NoStackParserLevels = 100;
        private static int NoStackExpressionLevels = 100;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Locking
#if DEBUG
        //
        // HACK: This is not read-only.
        //
        private static bool CheckDisposedOnExitLock = false;
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Process Support Methods
        public static int GetCurrentProcessId()
        {
            try
            {
                Process process = Process.GetCurrentProcess();

                if (process != null)
                    return process.Id;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Stack Checking Support Methods
        public static ReturnCode GetStackSize(
            ref UIntPtr used,
            ref UIntPtr allocated,
            ref UIntPtr extra,
            ref UIntPtr margin,
            ref UIntPtr maximum,
            ref UIntPtr reserve,
            ref UIntPtr commit,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
#if NATIVE && WINDOWS
            lock (syncRoot)
            {
                if (stackSizeSlot != null)
                {
                    try
                    {
                        /* THREAD-SAFE, per-thread data */
                        NativeStack.StackSize stackSize = Thread.GetData(
                            stackSizeSlot) as NativeStack.StackSize; /* throw */

                        if (stackSize != null)
                        {
                            used = stackSize.used;
                            allocated = stackSize.allocated;
                            extra = stackSize.extra;
                            margin = stackSize.margin;
                            maximum = stackSize.maximum;
                            reserve = stackSize.reserve;
                            commit = stackSize.commit;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "thread stack size is invalid";
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "thread stack size slot is invalid";
                }
            }
#else
            error = "not implemented";
#endif

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static void InitializeStackChecking()
        {
            lock (syncRoot)
            {
                #region Native Stack Checking Thread Local Storage
                //
                // NOTE: These MUST to be done prior to evaluating any scripts
                //       or runtime stack checking will not work properly
                //       (which can potentially cause scripts that use deep
                //       recursion to cause a .NET exception to be thrown from
                //       the script engine itself because the script engine
                //       depends upon runtime stack checking working properly).
                //
                if (stackPtrSlot == null)
                    stackPtrSlot = Thread.AllocateDataSlot();

                if (stackSizeSlot == null)
                    stackSizeSlot = Thread.AllocateDataSlot();
                #endregion
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void FinalizeStackChecking()
        {
            lock (syncRoot)
            {
                //
                // NOTE: Dispose the cached stack pointer and size information
                //       for this thread.  It is "mostly harmless" to do this
                //       even if is still required by another interpreter in
                //       this thread because it will automatically re-created
                //       in that case.  The alternative is to never dispose of
                //       this data.
                //
                if (stackPtrSlot != null)
                {
                    try
                    {
                        object stackPtrData = Thread.GetData(
                            stackPtrSlot); /* throw */

                        if (stackPtrData != null)
                        {
                            //
                            // NOTE: Remove our local reference to the data.
                            //
                            stackPtrData = null;

                            //
                            // NOTE: Clear out the data value for this thread.
                            //
                            Thread.SetData(
                                stackPtrSlot, stackPtrData); /* throw */
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (stackSizeSlot != null)
                {
                    try
                    {
                        object stackSizeData = Thread.GetData(
                            stackSizeSlot); /* throw */

                        if (stackSizeData != null)
                        {
                            //
                            // NOTE: Remove our local reference to the data.
                            //
                            stackSizeData = null;

                            //
                            // NOTE: Clear out the data value for this thread.
                            //
                            Thread.SetData(
                                stackSizeSlot, stackSizeData); /* throw */
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static UIntPtr CalculateUsedStackSpace(
            UIntPtr outerStackPtr,
            UIntPtr innerStackPtr
            )
        {
            //
            // NOTE: Attempt to automatically detect which way the stack is
            //       growing and then calculate the approximate amount of
            //       space that has been used so far.
            //
            if (outerStackPtr.ToUInt64() > innerStackPtr.ToUInt64())
            {
                return new UIntPtr(
                    outerStackPtr.ToUInt64() - innerStackPtr.ToUInt64());
            }
            else
            {
                return new UIntPtr(
                    innerStackPtr.ToUInt64() - outerStackPtr.ToUInt64());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static UIntPtr CalculateNeededStackSpace(
            Interpreter interpreter,
            ulong extraSpace,
            UIntPtr usedSpace,
            UIntPtr stackMargin
            )
        {
            ulong interpreterExtraSpace = (interpreter != null) ?
                interpreter.InternalExtraStackSpace : 0;

            return new UIntPtr(
                interpreterExtraSpace + extraSpace +
                usedSpace.ToUInt64() + stackMargin.ToUInt64());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckForStackSpace(
            ReadyFlags flags,
            int levels,
            int maximumLevels,
            int parserLevels,
            int maximumParserLevels,
            int expressionLevels,
            int maximumExpressionLevels
            )
        {
            //
            // NOTE: If native stack checking was not requested -OR- has
            //       been explicitly disabled, just skip it.
            //
            if (FlagOps.HasFlags(flags, ReadyFlags.NoStack, true) ||
                !FlagOps.HasFlags(flags, ReadyFlags.CheckStack, true))
            {
                return false;
            }

            //
            // NOTE: If this is a thread-pool thread, skip checking its
            //       stack if that was not requested -OR- it has been
            //       explicitly disabled.
            //
            if ((FlagOps.HasFlags(flags, ReadyFlags.NoPoolStack, true) ||
                !FlagOps.HasFlags(flags, ReadyFlags.ForcePoolStack, true)) &&
                Thread.CurrentThread.IsThreadPoolThread)
            {
                return false;
            }

            //
            // NOTE: Otherwise, if native stack checking is being forced,
            //       just do it.
            //
            if (FlagOps.HasFlags(flags, ReadyFlags.ForceStack, true))
                return true;

            //
            // NOTE: Are we supposed to check (or ignore?) the maximum
            //       levels reached thus far?
            //
            bool checkLevels = FlagOps.HasFlags(
                flags, ReadyFlags.CheckLevels, true);

            //
            // NOTE: Otherwise, if we have exceeded the number of script
            //       execution levels that require no native stack check,
            //       do it.
            //
            if ((levels > NoStackLevels) &&
                (!checkLevels || (levels >= maximumLevels)))
            {
                return true;
            }

            //
            // NOTE: Otherwise, if we have exceeded the number of script
            //       parser levels that require no native stack check,
            //       do it.
            //
            if ((parserLevels > NoStackParserLevels) &&
                (!checkLevels || (parserLevels >= maximumParserLevels)))
            {
                return true;
            }

            //
            // NOTE: Otherwise, if we have exceeded the number of script
            //       expression levels that require no native stack check,
            //       do it.
            //
            if ((expressionLevels > NoStackExpressionLevels) &&
                (!checkLevels || (expressionLevels >= maximumExpressionLevels)))
            {
                return true;
            }

            //
            // NOTE: Otherwise, skip it.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void RefreshNativeStackPointers()
        {
            UIntPtr innerStackPtr = UIntPtr.Zero;
            UIntPtr outerStackPtr = UIntPtr.Zero;

            RefreshNativeStackPointers(ref innerStackPtr, ref outerStackPtr);

            TraceOps.DebugTrace(String.Format(
                "RefreshNativeStackPointers: innerStackPtr = {0}, " +
                "outerStackPtr = {1}", innerStackPtr, outerStackPtr),
                typeof(RuntimeOps).Name, TracePriority.ThreadDebug);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void RefreshNativeStackPointers(
            ref UIntPtr innerStackPtr,
            ref UIntPtr outerStackPtr
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Make sure we have our process-wide thread data
                //       slot.
                //
                if (stackPtrSlot == null)
                    return;

                //
                // NOTE: Get the current native stack pointer (so that we
                //       know approximately where in the stack we currently
                //       are).
                //
                innerStackPtr = NativeStack.GetNativeStackPointer();

                //
                // NOTE: Get previously saved outer native stack pointer,
                //       if any.
                //
                /* THREAD-SAFE, per-thread data */
                object stackPtrData = Thread.GetData(stackPtrSlot); /* throw */

                //
                // NOTE: If we got a valid saved outer stack pointer value
                //       from the thread data slot, it should be a UIntPtr;
                //       otherwise, set it to zero (first time through) so
                //       that the current inner stack pointer will be saved
                //       into it for later use.
                //
                outerStackPtr = (stackPtrData is UIntPtr) ?
                    (UIntPtr)stackPtrData : UIntPtr.Zero;

                //
                // NOTE: If it was not previously saved, save it now.
                //
                if (outerStackPtr == UIntPtr.Zero)
                {
                    //
                    // NOTE: This must be the first time through, set the
                    //       outer stack pointer value equal to the current
                    //       stack pointer value and then save it for later
                    //       use.
                    //
                    outerStackPtr = innerStackPtr;

                    /* THREAD-SAFE, per-thread data */
                    Thread.SetData(stackPtrSlot, outerStackPtr); /* throw */
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the associated lock is held.
        //
        private static NativeStack.StackSize CreateOrUpdateStackSize(
            ulong extraSpace,
            UIntPtr usedSpace
            )
        {
            //
            // NOTE: Get the stack size object for this thread.  If it is
            //       invalid or has not been created yet, we will create
            //       or reset it now.
            //
            /* THREAD-SAFE, per-thread data */
            NativeStack.StackSize stackSize = Thread.GetData(
                stackSizeSlot) as NativeStack.StackSize; /* throw */

            //
            // NOTE: If it was not previously saved, save it now.
            //
            if (stackSize == null)
            {
                stackSize = new NativeStack.StackSize();

                /* THREAD-SAFE, per-thread data */
                Thread.SetData(stackSizeSlot, stackSize); /* throw */
            }

            //
            // NOTE: Update stack size object for this thread with the
            //       requested amount of extra space.
            //
            stackSize.extra = new UIntPtr(extraSpace);

            //
            // NOTE: First, update the stack size object for this thread
            //       with the amount of used space.
            //
            stackSize.used = usedSpace;

            //
            // NOTE: Next, update the stack size object for this thread
            //       with the amount of space allocated (because this
            //       number grows automatically within the actual stack
            //       limits, it is useless for the actual stack check
            //       and is only used for informational purposes).
            //
            stackSize.allocated = NativeStack.GetNativeStackAllocated();

            //
            // NOTE: Calculate the approximate safety margin (overhead)
            //       imposed by the CLR runtime.  This is estimated and
            //       may need to be updated for later versions of the
            //       CLR.  Since this number is currently constant for
            //       the lifetime of the process, we calculate it once
            //       and then cache it.
            //
            if (stackSize.margin == UIntPtr.Zero)
                stackSize.margin = NativeStack.GetNativeStackMargin();

            //
            // NOTE: Get the current amount of stack reserved for this
            //       thread from its Thread Environment Block (TEB).
            //       Since it is highly unlikely that this number will
            //       change during the lifetime of the thread, we cache
            //       it.
            //
            if (stackSize.maximum == UIntPtr.Zero)
                stackSize.maximum = NativeStack.GetNativeStackMaximum();

            //
            // NOTE: Return the created (or updated) stack size object
            //       to the caller.
            //
            return stackSize;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeSetStackReserveAndCommit(
            NativeStack.StackSize stackSize
            )
        {
            if (stackSize != null)
            {
                if ((stackSize.reserve == UIntPtr.Zero) ||
                    (stackSize.commit == UIntPtr.Zero))
                {
                    FileOps.CopyPeFileStackReserveAndCommit(stackSize);

                    TraceOps.DebugTrace(String.Format(
                        "MaybeSetStackReserveAndCommit: reserve = {0}, " +
                        "commit = {1}", stackSize.reserve, stackSize.commit),
                        typeof(RuntimeOps).Name, TracePriority.ThreadDebug);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryGetMaximumStackSpace(
            NativeStack.StackSize stackSize,
            ref UIntPtr maximumSpace
            )
        {
            if (stackSize != null)
            {
                //
                // NOTE: Start out with the maximum value from the stack size
                //       object.  This should be the typical case on Windows,
                //       because (most versions of) it supports the necessary
                //       native stack size checking APIs.
                //
                UIntPtr localMaximumSpace = stackSize.maximum;

                if (localMaximumSpace != UIntPtr.Zero)
                {
                    maximumSpace = localMaximumSpace;
                    return true;
                }

                //
                // NOTE: Failing that, fallback on the stack reserve from the
                //       executable (PE) file that started this process.  Do
                //       not bother with the commit as it is useless for this
                //       purpose.
                //
                MaybeSetStackReserveAndCommit(stackSize);

                localMaximumSpace = stackSize.reserve;

                if (localMaximumSpace != UIntPtr.Zero)
                {
                    maximumSpace = localMaximumSpace;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckForStackSpace(
            Interpreter interpreter,
            ulong extraSpace
            ) /* THREAD-SAFE */
        {
            try
            {
                if (!PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // NOTE: We are not running on Windows (even though this
                    //       binary was compiled for it); therefore, we have
                    //       no native stack checking available.
                    //
#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(
                        "CheckForStackSpace: platform is not Windows",
                        typeof(RuntimeOps).Name, TracePriority.ThreadError);
#endif

                    return ReturnCode.Ok;
                }

                ///////////////////////////////////////////////////////////////

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Make sure we have our process-wide thread data
                    //       slots.  We do not and cannot actually allocate
                    //       or create them here.  We are only reading these
                    //       static variables and we KNOW they are allocated
                    //       during interpreter creation; therefore, we do
                    //       not need a lock around access to them.
                    //
                    if ((stackPtrSlot == null) || (stackSizeSlot == null))
                    {
                        //
                        // NOTE: Our process-wide data slots were either not
                        //       allocated or have been freed prematurely?
                        //       Just assume that runtime stack checking was
                        //       purposely disabled and enough stack space is
                        //       available.
                        //
#if DEBUG && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: thread storage slots " +
                            "not available", typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: Attempt to get the current (inner) native stack
                    //       pointer and the (previously saved) outer native
                    //       stack pointer.
                    //
                    UIntPtr innerStackPtr = UIntPtr.Zero;
                    UIntPtr outerStackPtr = UIntPtr.Zero;

                    RefreshNativeStackPointers(
                        ref innerStackPtr, ref outerStackPtr);

                    //
                    // NOTE: Make sure we have valid values for the outer and
                    //       inner native stack pointers.
                    //
                    if (outerStackPtr == UIntPtr.Zero)
                    {
                        //
                        // NOTE: Runtime native stack checking appears to be
                        //       unavailable, just assume that enough stack
                        //       space is available.
                        //
#if DEBUG && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: outer stack pointer " +
                            "not available", typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    if (innerStackPtr == UIntPtr.Zero)
                    {
                        //
                        // NOTE: Runtime native stack checking appears to be
                        //       unavailable, just assume that enough stack
                        //       space is available.
                        //
#if DEBUG && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: inner stack pointer " +
                            "not available", typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: Calculate approximately how much native stack
                    //       space has been used.
                    //
                    UIntPtr usedSpace = CalculateUsedStackSpace(outerStackPtr,
                        innerStackPtr);

                    //
                    // NOTE: Create or update the stack size object for this
                    //       thread.
                    //
                    NativeStack.StackSize stackSize = CreateOrUpdateStackSize(
                        extraSpace, usedSpace);

                    //
                    // NOTE: Obtain the maximum stack size for this thread.
                    //
                    UIntPtr maximumSpace = UIntPtr.Zero;

                    if (!TryGetMaximumStackSpace(stackSize, ref maximumSpace))
                    {
                        //
                        // NOTE: If we made it this far and still do not have
                        //       a valid maximum native stack size, just assume
                        //       that enough stack space is available.
                        //
#if DEBUG && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: maximum space not available",
                            typeof(RuntimeOps).Name, TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: Calculate the amount of space used with the safety
                    //       margin taken into account.
                    //
                    UIntPtr neededSpace = CalculateNeededStackSpace(
                        interpreter, extraSpace, usedSpace, stackSize.margin);

                    //
                    // NOTE: Are we "out of stack space" taking the requested
                    //       extra space and our internal safety margin into
                    //       account?
                    //
                    // BUGBUG: Also, it seems that some pool threads have a
                    //         miserably low stack size (less than our internal
                    //         safety margin); therefore, evaluating scripts on
                    //         pool threads is not officially supported.
                    //
                    if (neededSpace.ToUInt64() <= maximumSpace.ToUInt64())
                    {
                        //
                        // NOTE: Normal case, enough native stack space appears
                        //       to be available.
                        //
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: Try to "fill in" some accurate stack reserve
                        //       and commit numbers, now, if needed.
                        //
                        MaybeSetStackReserveAndCommit(stackSize);

                        //
                        // NOTE: We hit a "soft" stack-overflow error.  This
                        //       error is guaranteed by the script engine to
                        //       be non-fatal to the process, the application
                        //       domain, and the script engine itself, and is
                        //       always fully recoverable.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "CheckForStackSpace: stack overflow, needed " +
                            "space {0} is greater than maximum space {1} for " +
                            "interpreter {2}: {3}", neededSpace, maximumSpace,
                            FormatOps.InterpreterNoThrow(interpreter),
                            stackSize), typeof(RuntimeOps).Name,
                            TracePriority.EngineError);

                        TraceOps.DebugTrace(String.Format(
                            "CheckForStackSpace: innerStackPtr = {0}, " +
                            "outerStackPtr = {1}", innerStackPtr, outerStackPtr),
                            typeof(RuntimeOps).Name, TracePriority.NativeDebug);

                        return ReturnCode.Error;
                    }
                }
            }
            catch (StackOverflowException)
            {
                //
                // NOTE: We hit a "hard" stack-overflow (exception) during the
                //       stack checking code?  Generally, this error should be
                //       non-fatal to the process, the application domain, and
                //       the script engine, and should be fully "recoverable";
                //       however, this is not guaranteed by the script engine
                //       as we are relying on the CLR stack unwinding semantics
                //       to function properly.
                //
                try
                {
                    //
                    // NOTE: We really want to report this condition to anybody
                    //       who might be listening; however, it is somewhat
                    //       dangerous to do so.  Therefore, wrap the necessary
                    //       method call in a try/catch block just in case we
                    //       re-trigger another stack overflow.
                    //
                    TraceOps.DebugTrace(
                        "CheckForStackSpace: stack overflow exception",
                        typeof(RuntimeOps).Name, TracePriority.EngineError);
                }
                catch (StackOverflowException)
                {
                    // do nothing.
                }

                return ReturnCode.Error;
            }
            catch (SecurityException)
            {
                //
                // NOTE: We may not be allowed to execute any native code;
                //       therefore, just assume that we always have enough
                //       stack space in that case.
                //
#if DEBUG && VERBOSE
                TraceOps.DebugTrace(
                    "CheckForStackSpace: security exception",
                    typeof(RuntimeOps).Name, TracePriority.EngineError);
#endif

                return ReturnCode.Ok;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hash Algorithm Support Methods
        public static byte[] HashArgument(
            string hashAlgorithmName,
            Argument argument,
            Encoding encoding,
            ref Result error
            )
        {
            return HashString(hashAlgorithmName,
                (argument != null) ? argument.String : null,
                encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashScript(
            string hashAlgorithmName,
            IScript script,
            Encoding encoding,
            ref Result error
            )
        {
            try
            {
                ByteList bytes = new ByteList();

                if (script != null)
                {
                    string value = script.Text;

                    if (value != null)
                    {
                        if (encoding != null)
                            bytes.AddRange(encoding.GetBytes(value));
                        else
                            bytes.AddRange(RawEncoding.GetBytes(value));
                    }
                }

                return HashBytes(
                    hashAlgorithmName, bytes.ToArray(), ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashFile(
            string hashAlgorithmName,
            string fileName,
            Encoding encoding,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return null;
            }

            if (PathOps.IsRemoteUri(fileName))
            {
                error = "remote uri not supported";
                return null;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't read file \"{0}\": " +
                    "no such file or directory",
                    fileName);

                return null;
            }

            try
            {
                ByteList bytes = new ByteList();

                if (encoding != null)
                {
                    bytes.AddRange(encoding.GetBytes(
                        File.ReadAllText(fileName, encoding)));
                }
                else
                {
                    bytes.AddRange(File.ReadAllBytes(fileName));
                }

                return HashBytes(
                    hashAlgorithmName, bytes.ToArray(), ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashScriptFile(
            Interpreter interpreter,
            string fileName,
            bool noRemote,
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return null;
                }

                Encoding encoding = Engine.GetEncoding(
                    fileName, EncodingType.Script, null);

                if (encoding == null)
                {
                    error = "script encoding not available";
                    return null;
                }

                ScriptFlags scriptFlags;
                EngineFlags engineFlags;
                SubstitutionFlags substitutionFlags;
                EventFlags eventFlags;
                ExpressionFlags expressionFlags;

                lock (interpreter) /* TRANSACTIONAL */
                {
                    scriptFlags = ScriptOps.GetFlags(
                        interpreter, interpreter.ScriptFlags, true);

                    engineFlags = interpreter.EngineFlags;
                    substitutionFlags = interpreter.SubstitutionFlags;
                    eventFlags = interpreter.EngineEventFlags;
                    expressionFlags = interpreter.ExpressionFlags;
                }

                scriptFlags |= ScriptFlags.NoPolicy;
                engineFlags |= EngineFlags.NoPolicy;

                if (noRemote)
                    engineFlags |= EngineFlags.NoRemote;

                string originalText = null;
                string text = null; /* NOT USED */

                if (Engine.ReadOrGetScriptFile(
                        interpreter, encoding, ref scriptFlags,
                        ref fileName, ref engineFlags,
                        ref substitutionFlags, ref eventFlags,
                        ref expressionFlags, ref originalText,
                        ref text, ref error) != ReturnCode.Ok)
                {
                    return null;
                }

                return HashString(
                    DefaultHashAlgorithmName, originalText, encoding,
                    ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashString(
            string hashAlgorithmName,
            string value,
            Encoding encoding,
            ref Result error
            )
        {
            try
            {
                ByteList bytes = new ByteList();

                if (value != null)
                {
                    if (encoding != null)
                        bytes.AddRange(encoding.GetBytes(value));
                    else
                        bytes.AddRange(RawEncoding.GetBytes(value));
                }

                return HashBytes(
                    hashAlgorithmName, bytes.ToArray(), ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static byte[] HashBytes(
            string hashAlgorithmName,
            byte[] bytes,
            ref Result error
            )
        {
            if (bytes != null)
            {
                try
                {
                    if (hashAlgorithmName == null)
                        hashAlgorithmName = DefaultHashAlgorithmName;

                    using (HashAlgorithm hashAlgorithm = HashAlgorithm.Create(
                            hashAlgorithmName))
                    {
                        if (hashAlgorithm == null)
                        {
                            error = String.Format(
                                "unsupported hash algorithm \"{0}\"",
                                hashAlgorithmName);

                            return null;
                        }

                        hashAlgorithm.Initialize();

                        return hashAlgorithm.ComputeHash(bytes);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Security Support Methods
        private static KeySizes GetLeastMinSize(
            KeySizes[] allKeySizes /* in */
            )
        {
            if (allKeySizes == null)
                return null;

            int bestIndex = Index.Invalid;
            int bestMinSize = _Size.Invalid;

            for (int index = 0; index < allKeySizes.Length; index++)
            {
                KeySizes keySizes = allKeySizes[index];

                if (keySizes == null)
                    continue;

                int minSize = keySizes.MaxSize;

                if ((bestIndex == Index.Invalid) || (minSize < bestMinSize))
                {
                    bestIndex = index;
                    bestMinSize = minSize;
                }
            }

            return (bestIndex != Index.Invalid) ?
                allKeySizes[bestIndex] : null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static KeySizes GetGreatestMaxSize(
            KeySizes[] allKeySizes /* in */
            )
        {
            if (allKeySizes == null)
                return null;

            int bestIndex = Index.Invalid;
            int bestMaxSize = _Size.Invalid;

            for (int index = 0; index < allKeySizes.Length; index++)
            {
                KeySizes keySizes = allKeySizes[index];

                if (keySizes == null)
                    continue;

                int maxSize = keySizes.MaxSize;

                if ((bestIndex == Index.Invalid) || (maxSize > bestMaxSize))
                {
                    bestIndex = index;
                    bestMaxSize = maxSize;
                }
            }

            return (bestIndex != Index.Invalid) ?
                allKeySizes[bestIndex] : null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool[] GetGreatestMaxKeySizeAndLeastMinBlockSize(
            SymmetricAlgorithm algorithm, /* in */
            ref int keySize,              /* in, out */
            ref int blockSize             /* in, out */
            )
        {
            bool[] found = { false, false };

            if (algorithm != null)
            {
                KeySizes keySizes; /* REUSED */

                keySizes = GetGreatestMaxSize(algorithm.LegalKeySizes);

                if (keySizes != null)
                {
                    keySize = keySizes.MaxSize;
                    found[0] = true;
                }

                keySizes = GetLeastMinSize(algorithm.LegalBlockSizes);

                if (keySizes != null)
                {
                    blockSize = keySizes.MinSize;
                    found[1] = true;
                }
            }

            return found;
        }

        ///////////////////////////////////////////////////////////////////////

#if !NATIVE
        private static ReturnCode IsAdministrator(
            ref bool administrator,
            ref Result error
            )
        {
            try
            {
                //
                // BUGBUG: This does not work properly on Mono due to their
                //         lack of support for checking the elevation status of
                //         the current process (i.e. it returns true even when
                //         running without elevation).
                //
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                administrator = (identity != null)
                    ? new WindowsPrincipal(identity).IsInRole(
                        WindowsBuiltInRole.Administrator) :
                    false;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAdministrator()
        {
#if NATIVE
            //
            // BUGBUG: This fails when running on Mono for Windows due to the
            //         bug that prevents native functions from being called by
            //         ordinal (e.g. "#680").
            //         https://bugzilla.novell.com/show_bug.cgi?id=636966
            //
            return SecurityOps.IsAdministrator();
#else
            //
            // BUGBUG: This does not work properly on Mono due to their lack of
            //         support for checking the elevation status of the current
            //         process (i.e. it returns true even when running without
            //         elevation).
            //
            bool administrator = false;
            Result error = null;

            return (IsAdministrator(ref administrator,
                ref error) == ReturnCode.Ok) && administrator;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckStrongNameVerified()
        {
            return !CommonOps.Environment.DoesVariableExist(EnvVars.NoVerified);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStrongNameVerified(
            byte[] bytes,
            bool force
            )
        {
            //
            // NOTE: *SECURITY* Failure, if no bytes were supplied we cannot
            //       verify them.
            //
            if ((bytes == null) || (bytes.Length == 0))
                return false;

            string fileName = null;

            try
            {
                fileName = Path.GetTempFileName(); /* throw */

                //
                // NOTE: *SECURITY* Failure, if we cannot obtain a temporary
                //       file name we cannot verify the assembly file.
                //
                if (String.IsNullOrEmpty(fileName))
                    return false;

                //
                // NOTE: This code requires a bit of explanation.  First,
                //       we write all the file bytes to the temporary file.
                //       Next, we [re-]open that same temporary file for
                //       reading only and hold it open while calling into
                //       the native CLR API to verify the strong name
                //       signature on it.  Furthermore, the bytes of the
                //       open temporary file are read back into a new byte
                //       array and are then compared with the previously
                //       written byte array.  If there is any discrepancy,
                //       this method returns false without calling the
                //       native CLR API to check the strong name signature.
                //
                File.WriteAllBytes(fileName, bytes); /* throw */

                using (FileStream stream = new FileStream(
                        fileName, FileMode.Open, FileAccess.Read,
                        FileShare.Read)) /* throw */ /* EXEMPT */
                {
                    //
                    // NOTE: Depending on the size of the file, this could
                    //       potentially run out of memory.
                    //
                    byte[] newBytes = new byte[bytes.Length]; /* throw */
                    stream.Read(newBytes, 0, newBytes.Length); /* throw */

                    //
                    // NOTE: *SECURITY* Failure, if the underlying bytes of
                    //       the file have changed since we wrote them then
                    //       it cannot be verified.
                    //
                    if (!ArrayOps.Equals(newBytes, bytes))
                        return false;

                    //
                    // NOTE: Ok, the newly read bytes match those we wrote
                    //       out and we are holding the underlying file open,
                    //       preventing it from being changed via any other
                    //       thread or process; therefore, perform the strong
                    //       name verification via the native CLR API now.
                    //
                    return IsStrongNameVerified(fileName, force);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.SecurityError);

                //
                // NOTE: *SECURITY* Failure, assume not verified.
                //
                return false;
            }
            finally
            {
                try
                {
                    //
                    // NOTE: If we created a temporary file, always delete it
                    //       prior to returning from this method.
                    //
                    if (fileName != null)
                        File.Delete(fileName); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStrongNameVerified(
            string fileName,
            bool force
            )
        {
#if NATIVE
            return StrongNameOps.IsStrongNameVerified(fileName, force);
#else
            //
            // FIXME: Find some pure-managed way to do this?
            //
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckForUpdates()
        {
            return !CommonOps.Environment.DoesVariableExist(EnvVars.NoUpdates);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckFileTrusted()
        {
            return !CommonOps.Environment.DoesVariableExist(EnvVars.NoTrusted);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckCoreFileTrusted()
        {
            if (!ShouldCheckFileTrusted())
                return false;

            if (!SetupOps.ShouldCheckCoreTrusted())
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE_UTILITY
        //
        // WARNING: For now, this method should be called *ONLY* from within
        //          the NativeUtility class in order to verify that the Eagle
        //          Native Utility Library (Spilornis) is trusted.
        //
        public static bool ShouldLoadNativeLibrary(
            string fileName
            )
        {
            //
            // NOTE: If the primary assembly is not "trusted", allow any
            //       native library to load.
            //
            // NOTE: For the purposes of this ShouldCheckCoreTrusted() call, the
            //       "Eagle Native Utility Library" (Spilornis) *IS* considered
            //       to be part of the "Eagle Core Library".
            //
            if (!ShouldCheckCoreFileTrusted() ||
                !IsFileTrusted(GlobalState.GetAssemblyLocation(), IntPtr.Zero))
            {
                return true;
            }

            //
            // NOTE: Otherwise, if the native library is "trusted", allow
            //       it to load.
            //
            if (!ShouldCheckFileTrusted() ||
                IsFileTrusted(fileName, IntPtr.Zero))
            {
                return true;
            }

            //
            // NOTE: Otherwise, do not allow the native library to load.
            //
            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFileTrusted(
            byte[] bytes
            )
        {
            //
            // NOTE: *SECURITY* Failure, if no bytes were supplied we cannot
            //       check trust on them.
            //
            if ((bytes == null) || (bytes.Length == 0))
                return false;

            string fileName = null;

            try
            {
                fileName = Path.GetTempFileName(); /* throw */

                //
                // NOTE: *SECURITY* Failure, if we cannot obtain a temporary
                //       file name we cannot check trust on the file.
                //
                if (String.IsNullOrEmpty(fileName))
                    return false;

                using (FileStream stream = new FileStream(
                        fileName, FileMode.Create, FileAccess.ReadWrite,
                        FileShare.None)) /* throw */ /* EXEMPT */
                {
                    stream.Write(bytes, 0, bytes.Length); /* throw */

                    return IsFileTrusted(fileName, stream.Handle);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.SecurityError);

                //
                // NOTE: *SECURITY* Failure, assume not trusted.
                //
                return false;
            }
            finally
            {
                try
                {
                    //
                    // NOTE: If we created a temporary file, always delete it
                    //       prior to returning from this method.
                    //
                    if (fileName != null)
                        File.Delete(fileName); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFileTrusted(
            string fileName,
            IntPtr fileHandle
            )
        {
            return IsFileTrusted(
                fileName, fileHandle, false, false, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsFileTrusted(
            string fileName,
            IntPtr fileHandle,
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install
            )
        {
#if NATIVE
            return WinTrustOps.IsFileTrusted(
                fileName, fileHandle, userInterface, userPrompt, revocation,
                install);
#else
            //
            // FIXME: Find some pure-managed way to do this?
            //
            // NOTE: We could use the AuthenticodeSignatureInformation
            //       class if we took a dependency on the .NET Framework
            //       3.5+; however, that would still be very unlikely to
            //       work on Mono.
            //
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetVendor()
        {
            return GetCertificateSubject(GlobalState.GetAssemblyLocation(),
                null, ShouldCheckCoreFileTrusted(), true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetCertificateSubject(
            string fileName,
            string prefix,
            bool trusted,
            bool noParenthesis
            )
        {
            if (trusted && (fileName != null))
            {
                X509Certificate2 certificate2 = null;

                if (CertificateOps.GetCertificate2(
                        fileName, ref certificate2) == ReturnCode.Ok)
                {
                    if ((certificate2 != null) &&
                        IsFileTrusted(fileName, IntPtr.Zero))
                    {
                        StringBuilder result = StringOps.NewStringBuilder();

                        if (!String.IsNullOrEmpty(prefix))
                            result.Append(prefix);

                        string simpleName = certificate2.GetNameInfo(
                            X509NameType.SimpleName, false);

                        if (noParenthesis && (simpleName != null))
                        {
                            int index = simpleName.IndexOf(
                                Characters.OpenParenthesis);

                            if (index != Index.Invalid)
                            {
                                simpleName = simpleName.Substring(
                                    0, index).Trim();
                            }
                        }

                        result.Append(simpleName);
                        return result.ToString();
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList CertificateToList(
            X509Certificate certificate,
            bool verbose
            )
        {
            StringList list = null;

            if (certificate != null)
            {
                list = new StringList();
                list.Add("subject", certificate.Subject);

                if (verbose)
                {
                    list.Add("issuer", certificate.Issuer);

                    list.Add("serialNumber",
                        certificate.GetSerialNumberString());

                    list.Add("hash",
                        certificate.GetCertHashString());
                    list.Add("effectiveDate",
                        certificate.GetEffectiveDateString());

                    list.Add("expirationDate",
                        certificate.GetExpirationDateString());

                    list.Add("algorithm",
                        certificate.GetKeyAlgorithm());

                    list.Add("algorithmParameters",
                        certificate.GetKeyAlgorithmParametersString());
                }
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Plugin Support Methods
        public static string PluginFlagsToPrefix(
            PluginFlags flags
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (FlagOps.HasFlags(flags, PluginFlags.System, true))
                result.Append(Characters.S);

            if (FlagOps.HasFlags(flags, PluginFlags.Host, true))
                result.Append(Characters.H);

            if (FlagOps.HasFlags(flags, PluginFlags.Debugger, true))
                result.Append(Characters.D);

            if (FlagOps.HasFlags(flags, PluginFlags.Commercial, true) ||
                FlagOps.HasFlags(flags, PluginFlags.Proprietary, true))
            {
                result.Append(Characters.N); /* NOTE: Non-free. */
            }

            if (FlagOps.HasFlags(flags, PluginFlags.Licensed, true))
                result.Append(Characters.L);

#if ISOLATED_PLUGINS
            if (FlagOps.HasFlags(flags, PluginFlags.Isolated, true))
                result.Append(Characters.I);
#endif

            if (FlagOps.HasFlags(flags, PluginFlags.StrongName, true) &&
                FlagOps.HasFlags(flags, PluginFlags.Verified, true))
            {
                result.Append(Characters.V);
            }

            if (FlagOps.HasFlags(flags, PluginFlags.Authenticode, true) &&
                FlagOps.HasFlags(flags, PluginFlags.Trusted, true))
            {
                result.Append(Characters.T);
            }

            if (FlagOps.HasFlags(flags, PluginFlags.Primary, true))
                result.Append(Characters.P);

            if (FlagOps.HasFlags(flags, PluginFlags.UserInterface, true))
                result.Append(Characters.U);

            //
            // NOTE: Did the plugin have any special flags?
            //
            if (result.Length > 0)
            {
                result.Insert(0, Characters.OpenBracket);
                result.Append(Characters.CloseBracket);
                result.Append(Characters.Space);
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Line Support Methods
        private static void AppendCommandLineArgument(
            StringBuilder builder,
            string arg,
            bool quoteAll
            )
        {
            if ((builder == null) || (arg == null))
                return;

            char[] special = {
                Characters.Space, Characters.QuotationMark,
                Characters.Backslash
            };

            if (builder.Length > 0)
                builder.Append(Characters.Space);

            bool wrap = quoteAll ||
                (arg.IndexOfAny(special) != Index.Invalid);

            if (wrap)
                builder.Append(Characters.QuotationMark);

            int length = arg.Length;

            for (int index = 0; index < length; index++)
            {
                if (arg[index] == Characters.QuotationMark)
                {
                    builder.Append(Characters.Backslash);
                    builder.Append(Characters.QuotationMark);
                }
                else if (arg[index] == Characters.Backslash)
                {
                    int count = 0;

                    while ((index < length) &&
                        (arg[index] == Characters.Backslash))
                    {
                        count++; index++;
                    }

                    if (index < length)
                    {
                        if (arg[index] == Characters.QuotationMark)
                        {
                            builder.Append(
                                Characters.Backslash, (count * 2) + 1);

                            builder.Append(Characters.QuotationMark);
                        }
                        else
                        {
                            builder.Append(Characters.Backslash, count);
                            builder.Append(arg[index]);
                        }
                    }
                    else
                    {
                        builder.Append(Characters.Backslash, count * 2);
                        break;
                    }
                }
                else
                {
                    builder.Append(arg[index]);
                }
            }

            if (wrap)
                builder.Append(Characters.QuotationMark);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string BuildCommandLine(
            IEnumerable<string> args,
            bool quoteAll
            )
        {
            if (args == null)
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();

            foreach (string arg in args)
                AppendCommandLineArgument(builder, arg, quoteAll);

            return builder.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entity Usage Support Methods
        public static void IncrementOperationCount(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
                interpreter.IncrementOperationCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void IncrementCommandCount(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
                interpreter.IncrementCommandCount();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Alias Support Methods
        public static ReturnCode GetInterpreterAliasArguments(
            string interpreterName,
            ObjectOptionType objectOptionType, /* NOT USED */
            ref ArgumentList arguments,
            ref Result error /* NOT USED */
            )
        {
            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Interp)),
                "eval"
            });

            if (interpreterName != null)
                arguments.Add(interpreterName);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
        public static ReturnCode GetLibraryAliasArguments(
            string delegateName,
            ObjectOptionType objectOptionType, /* NOT USED */
            ref ArgumentList arguments,
            ref Result error /* NOT USED */
            )
        {
            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Library)),
                "call"
            });

            if (delegateName != null)
                arguments.Add(delegateName);

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static string GetObjectAliasSubCommand(
            ObjectOptionType objectOptionType
            )
        {
            switch (objectOptionType & ObjectOptionType.ObjectInvokeOptionMask)
            {
                case ObjectOptionType.Invoke:
                    return "invoke";
                case ObjectOptionType.InvokeRaw:
                    return "invokeraw";
                case ObjectOptionType.InvokeAll:
                    return "invokeall";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetObjectAliasArguments(
            string objectName,
            ObjectOptionType objectOptionType,
            ref ArgumentList arguments,
            ref Result error
            )
        {
            string subCommand = GetObjectAliasSubCommand(objectOptionType);

            if (subCommand == null)
            {
                error = "invalid sub-command";
                return ReturnCode.Error;
            }

            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Object)),
                subCommand
            });

            if (objectName != null)
                arguments.Add(objectName);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static ReturnCode GetTclAliasArguments(
            string interpName,
            ObjectOptionType objectOptionType, /* NOT USED */
            ref ArgumentList arguments,
            ref Result error /* NOT USED */
            )
        {
            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Tcl)),
                "eval"
            });

            if (interpName != null)
                arguments.Add(interpName);

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is static; however, in the future that may no
        //       longer be true.  In that case, it will need to move back to
        //       the Interpreter class.
        //
        public static IAlias NewAlias(
            string name,
            CommandFlags flags,
            AliasFlags aliasFlags,
            IClientData clientData,
            string nameToken,
            Interpreter sourceInterpreter,
            Interpreter targetInterpreter,
            INamespace sourceNamespace,
            INamespace targetNamespace,
            IExecute target,
            ArgumentList arguments,
            OptionDictionary options,
            int startIndex
            )
        {
            //
            // HACK: We do not necessarily know (and do not simply want to
            //       "guess") the plugin associated with the target of the
            //       command; therefore, we use a null value for the plugin
            //       argument here.
            //
            return new _Commands.Alias(
                new CommandData(name, null, null, clientData,
                    typeof(_Commands.Alias).FullName, flags,
                    /* plugin */ null, 0),
                new AliasData(nameToken, sourceInterpreter,
                    targetInterpreter, sourceNamespace, targetNamespace,
                    target, arguments, options, aliasFlags, startIndex, 0));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Notification Support Methods
        public static IScriptEventArgs GetEventArgs(
            NotifyType type,
            NotifyFlags flags,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            Result result,
            ScriptException exception,
            InterruptType interruptType
            )
        {
            return new ScriptEventArgs(
                GlobalState.NextId(interpreter), type, flags, interpreter,
                clientData, arguments, result, exception, interruptType, null,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScriptEventArgs GetInterruptEventArgs(
            Interpreter interpreter,
            InterruptType interruptType,
            IClientData clientData
            )
        {
            NotifyType notifyType = NotifyType.Script;

            if (interruptType == InterruptType.Deleted)
                notifyType = NotifyType.Interpreter;
#if DEBUGGER
            else if (interruptType == InterruptType.Halted)
                notifyType = NotifyType.Debugger;
#endif

            return GetEventArgs(
                notifyType, NotifyFlags.Interrupted, interpreter, clientData,
                null, null, null, interruptType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Channel Support Methods
        public static string ChannelTypeToName(
            string name,
            ChannelType channelType
            )
        {
            if (FlagOps.HasFlags(channelType, ChannelType.Input, true))
                return (name != null) ? name : StandardChannel.Input;
            else if (FlagOps.HasFlags(channelType, ChannelType.Output, true))
                return (name != null) ? name : StandardChannel.Output;
            else if (FlagOps.HasFlags(channelType, ChannelType.Error, true))
                return (name != null) ? name : StandardChannel.Error;
            else
                return name;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static bool IsStandardChannelName( /* NOT USED */
            string name
            )
        {
            if (String.Compare(name, StandardChannel.Input,
                    StringOps.SystemStringComparisonType) == 0)
            {
                return true;
            }
            else if (String.Compare(name, StandardChannel.Output,
                    StringOps.SystemStringComparisonType) == 0)
            {
                return true;
            }
            else if (String.Compare(name, StandardChannel.Error,
                    StringOps.SystemStringComparisonType) == 0)
            {
                return true;
            }

            return false;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Support Methods
        public static string GetString(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (resourceManager != null)
            {
                try
                {
                    return resourceManager.GetString(name, cultureInfo);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = InvalidPluginResourceManager;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetResourceNames(
            IPluginData pluginData,          /* in */
            ResourceManager resourceManager, /* in */
            CultureInfo cultureInfo,         /* in */
            ref StringList list,             /* in, out */
            ref Result error                 /* out */
            )
        {
            ResourceManager pluginResourceManager = null;

            if ((pluginData != null) && !FlagOps.HasFlags(
                    pluginData.Flags, PluginFlags.NoResources, true))
            {
                pluginResourceManager = pluginData.ResourceManager;
            }

            StringList localList = null;

            foreach (ResourceManager localResourceManager in
                new ResourceManager[] {
                    resourceManager, pluginResourceManager
                })
            {
                if (localResourceManager == null)
                    continue;

                if (ResourceOps.GetNames(
                        localResourceManager, cultureInfo,
                        ref localList, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            if (localList != null)
            {
                if (list == null)
                    list = new StringList();

                list.AddRange(localList);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetString(
            IPluginData pluginData,
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if ((pluginData != null) && !FlagOps.HasFlags(
                    pluginData.Flags, PluginFlags.NoResources, true))
            {
                ResourceManager pluginResourceManager = pluginData.ResourceManager;

                if (pluginResourceManager != null)
                {
                    try
                    {
                        return pluginResourceManager.GetString(name, cultureInfo);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = InvalidPluginResourceManager;
                }
            }
            else
            {
                if (resourceManager != null)
                {
                    try
                    {
                        return resourceManager.GetString(name, cultureInfo);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = InvalidInterpreterResourceManager;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ResourceManager NewResourceManager(
            AssemblyName assemblyName
            )
        {
            if (assemblyName != null)
            {
                try
                {
                    return NewResourceManager(assemblyName,
                        Assembly.Load(assemblyName));
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ResourceManager NewResourceManager(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    return NewResourceManager(assembly.GetName(), assembly);
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }

            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ResourceManager NewResourceManager(
            AssemblyName assemblyName,
            Assembly assembly
            )
        {
            if ((assemblyName != null) && (assembly != null))
            {
                try
                {
                    return new ResourceManager(assemblyName.Name, assembly);
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stream Support Methods
        private static ReturnCode NewStreamFromAssembly(
            Interpreter interpreter,
            string path,
            ref HostStreamFlags flags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(path))
            {
                error = "unrecognized path";
                return ReturnCode.Error;
            }

            Dictionary<HostStreamFlags, Assembly> assemblies =
                new Dictionary<HostStreamFlags, Assembly>();

            if (FlagOps.HasFlags(flags,
                    HostStreamFlags.EntryAssembly, true))
            {
                assemblies.Add(HostStreamFlags.EntryAssembly,
                    GlobalState.GetEntryAssembly());
            }

            if (FlagOps.HasFlags(flags,
                    HostStreamFlags.ExecutingAssembly, true))
            {
                assemblies.Add(HostStreamFlags.ExecutingAssembly,
                    GlobalState.GetAssembly());
            }

            flags &= ~HostStreamFlags.AssemblyMask;

            bool resolve = FlagOps.HasFlags(
                flags, HostStreamFlags.ResolveFullPath, true);

            foreach (KeyValuePair<HostStreamFlags, Assembly> pair
                    in assemblies)
            {
                Assembly assembly = pair.Value;

                if (assembly == null)
                    continue;

                string localFullPath = resolve ?
                    PathOps.ResolveFullPath(interpreter, path) : path;

                Stream localStream = AssemblyOps.GetResourceStream(
                    assembly, localFullPath);

                if (localStream != null)
                {
                    flags |= pair.Key;

                    if (FlagOps.HasFlags(flags,
                            HostStreamFlags.AssemblyQualified, true))
                    {
                        fullPath = String.Format(
                            "{0}{1}{2}", assembly.Location,
                            PathOps.GetFirstDirectorySeparator(localFullPath),
                            PathOps.MakeRelativePath(localFullPath, true));
                    }
                    else
                    {
                        fullPath = localFullPath;
                    }

                    stream = localStream;

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "stream \"{0}\" not available via specified assemblies",
                path);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode NewStreamFromPlugins(
            Interpreter interpreter,
            string path,
            ref HostStreamFlags flags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(path))
            {
                error = "unrecognized path";
                return ReturnCode.Error;
            }

            PluginWrapperDictionary plugins = interpreter.CopyPlugins();

            if (plugins == null)
            {
                error = "plugins not available";
                return ReturnCode.Error;
            }

            flags &= ~HostStreamFlags.FoundViaPlugin;

            bool resolve = FlagOps.HasFlags(
                flags, HostStreamFlags.ResolveFullPath, true);

            foreach (KeyValuePair<string, _Wrappers.Plugin> pair in plugins)
            {
                IPlugin plugin = pair.Value;

                if (plugin == null)
                    continue;

                string localFullPath = resolve ?
                    PathOps.ResolveFullPath(interpreter, path) : path;

                Stream localStream = plugin.GetStream(
                    interpreter, localFullPath, ref error);

                if (localStream != null)
                {
                    flags |= HostStreamFlags.FoundViaPlugin;
                    fullPath = localFullPath;
                    stream = localStream;

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "stream \"{0}\" not available via loaded plugins",
                path);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode NewStream(
            Interpreter interpreter,
            string path,
            FileMode mode,
            FileAccess access,
            ref Stream stream,
            ref Result error
            )
        {
            HostStreamFlags flags = HostStreamFlags.None;
            string fullPath = null;

            return NewStream(
                interpreter, path, mode, access, FileShare.Read,
                Channel.DefaultBufferSize, FileOptions.None, ref flags,
                ref fullPath, ref stream, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode NewStream(
            Interpreter interpreter,
            string path,
            FileMode mode,
            FileAccess access,
            ref HostStreamFlags flags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            return NewStream(
                interpreter, path, mode, access, FileShare.Read,
                Channel.DefaultBufferSize, FileOptions.None, ref flags,
                ref fullPath, ref stream, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode NewStream(
            Interpreter interpreter,   /* in, OPTIONAL: May be null. */
            string path,               /* in */
            FileMode mode,             /* in */
            FileAccess access,         /* in */
            FileShare share,           /* in */
            int bufferSize,            /* in */
            FileOptions options,       /* in */
            ref HostStreamFlags flags, /* in, out */
            ref string fullPath,       /* out */
            ref Stream stream,         /* out */
            ref Result error           /* out */
            )
        {
            flags &= ~HostStreamFlags.FoundMask;

            if (String.IsNullOrEmpty(path))
            {
                error = "unrecognized path";
                return ReturnCode.Error;
            }

            if (PathOps.IsRemoteUri(path))
            {
                error = String.Format(
                    "cannot open stream for remote uri \"{0}\"",
                    path);

                return ReturnCode.Error;
            }

            ReturnCode code;
            Result localError = null;
            ResultList errors = null;

            ///////////////////////////////////////////////////////////////////

            bool usePlugins = FlagOps.HasFlags(
                flags, HostStreamFlags.LoadedPlugins, true);

            bool useAssembly = FlagOps.HasFlags(
                flags, HostStreamFlags.AssemblyMask, false);

            bool resolve = FlagOps.HasFlags(
                flags, HostStreamFlags.ResolveFullPath, true);

            bool preferFileSystem = FlagOps.HasFlags(
                flags, HostStreamFlags.PreferFileSystem, true);

            bool skipFileSystem = FlagOps.HasFlags(
                flags, HostStreamFlags.SkipFileSystem, true);

            ///////////////////////////////////////////////////////////////////

            if (usePlugins && !preferFileSystem)
            {
                code = NewStreamFromPlugins(
                    interpreter, path, ref flags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    flags |= HostStreamFlags.FoundViaPlugin;
                    return ReturnCode.Ok;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (useAssembly && !preferFileSystem)
            {
                code = NewStreamFromAssembly(
                    interpreter, path, ref flags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    flags |= HostStreamFlags.FoundViaAssembly;
                    return ReturnCode.Ok;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!skipFileSystem)
            {
                string localFullPath = resolve ?
                    PathOps.ResolveFullPath(interpreter, path) : path;

                if (!String.IsNullOrEmpty(localFullPath))
                {
                    try
                    {
                        stream = new FileStream(
                            localFullPath, mode, access, share,
                            bufferSize, options); /* throw */ /* EXEMPT */

                        flags |= HostStreamFlags.FoundViaFileSystem;
                        fullPath = localFullPath;

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "could not resolve local path \"{0}\"",
                        path));
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (usePlugins && preferFileSystem)
            {
                code = NewStreamFromPlugins(
                    interpreter, path, ref flags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    flags |= HostStreamFlags.FoundViaPlugin;
                    return ReturnCode.Ok;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (useAssembly && preferFileSystem)
            {
                code = NewStreamFromAssembly(
                    interpreter, path, ref flags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    flags |= HostStreamFlags.FoundViaAssembly;
                    return ReturnCode.Ok;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!usePlugins && !useAssembly && skipFileSystem)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "cannot open stream for \"{0}\", no search performed",
                    path));
            }

            ///////////////////////////////////////////////////////////////////

            error = errors;
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Information Methods
        private static string GetGenuine()
        {
            return ArrayOps.Equals(
                License.Hash, StringOps.HashString(null, null,
                StringOps.ForceCarriageReturns(License.Summary +
                License.Text))) ? Vars.GenuineValue : null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFileTrusted(
            string fileName
            )
        {
            if (GetCertificateSubject(
                    fileName, null, ShouldCheckFileTrusted(), true) != null)
            {
                return Vars.TrustedValue;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsOfficial()
        {
#if OFFICIAL
            return true;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStable()
        {
#if STABLE
            return true;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetUpdatePathAndQuery(
            string version,
            bool? stable,
            string suffix
            )
        {
            string format;

            if (stable != null)
            {
                format = (bool)stable ?
                    Vars.Platform.UpdateStablePathAndQueryValue :
                    Vars.Platform.UpdateUnstablePathAndQueryValue;

                if ((version != null) || (suffix != null))
                    return String.Format(format, version, suffix);
                else
                    return format;
            }
            else
            {
                format = Vars.Platform.UpdateStablePathAndQuerySuffix;

                string methodName = DebugOps.GetMethodName(
                    1, null, false, true, null);

                return String.Format("{1}{0}", String.Format(
                    format, version, suffix), methodName);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetVersion(
            ref Result result
            )
        {
            //
            // NOTE: Return the engine version information.
            //
            Assembly assembly = GlobalState.GetAssembly();
            string fileName = (assembly != null) ? assembly.Location : null;

            result = StringList.MakeList(
                GlobalState.GetPackageName(),
                GlobalState.GetAssemblyVersion(),
                GetFileTrusted(fileName),
                GetGenuine(), Vars.OfficialValue, Vars.StableValue,
                SharedAttributeOps.GetAssemblyTag(assembly),
                SharedAttributeOps.GetAssemblyRelease(assembly),
                SharedAttributeOps.GetAssemblyText(assembly),
                AttributeOps.GetAssemblyConfiguration(assembly),
                TclVars.PackageName + Characters.Space.ToString() +
                TclVars.PatchLevelValue, FormatOps.Iso8601DateTime(
                    AttributeOps.GetAssemblyDateTime(assembly), true),
                SharedAttributeOps.GetAssemblySourceId(assembly),
                SharedAttributeOps.GetAssemblySourceTimeStamp(assembly),
                CommonOps.Runtime.GetRuntimeNameAndVersion(),
                PlatformOps.GetOperatingSystemName(),
                PlatformOps.GetMachineName());

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Processor Information Methods
        public static string GetProcessorArchitecture()
        {
            string processorArchitecture;

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                processorArchitecture = CommonOps.Environment.GetVariable(
                    EnvVars.ProcessorArchitecture);
            }
            else
            {
                //
                // HACK: Technically, this may not be 100% accurate.
                //
                processorArchitecture = PlatformOps.GetMachineName();
            }

            //
            // HACK: Check for an "impossible" situation.  If the pointer size
            //       is 32-bits, the processor architecture cannot be "AMD64".
            //       In that case, we are almost certainly hitting a bug in the
            //       operating system and/or Visual Studio that causes the
            //       PROCESSOR_ARCHITECTURE environment variable to contain the
            //       wrong value in some circumstances.  There are several
            //       reports of this issue from users on StackOverflow.
            //
            if ((IntPtr.Size == sizeof(int)) && String.Equals(
                    processorArchitecture, "AMD64",
                    StringOps.SystemNoCaseStringComparisonType))
            {
                //
                // NOTE: When tracing is enabled, save the originally detected
                //       processor architecture before changing it.
                //
                string savedProcessorArchitecture = processorArchitecture;

                //
                // NOTE: We know that operating systems that return "AMD64" as
                //       the processor architecture are actually a superset of
                //       the "x86" processor architecture; therefore, return
                //       "x86" when the pointer size is 32-bits.
                //
                processorArchitecture = "x86";

                //
                // NOTE: Show that we hit a fairly unusual situation (i.e. the
                //       "wrong" processor architecture was detected).
                //
                TraceOps.DebugTrace(String.Format(
                    "Detected {0}-bit process pointer size with processor " +
                    "architecture \"{1}\", using processor architecture " +
                    "\"{2}\" instead...", PlatformOps.GetProcessBits(),
                    savedProcessorArchitecture, processorArchitecture),
                    typeof(RuntimeOps).Name, TracePriority.StartupDebug);
            }

            return processorArchitecture;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
        public static IProcedure NewCoreProcedure(
            Interpreter interpreter,
            string name,
            string group,
            string description,
            ProcedureFlags flags,
            ArgumentList arguments,
            string body,
            IScriptLocation location,
            IClientData clientData
            )
        {
            return new _Procedures.Core(new ProcedureData(name, group,
                description, flags, arguments, body, location, clientData, 0));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resolver Support Methods
        public static bool ShouldResolveHidden(
            EngineFlags engineFlags,
            bool match
            )
        {
            return Engine.HasToExecute(engineFlags) &&
                !Engine.HasUseHidden(engineFlags) &&
                (match ? Engine.HasMatchHidden(engineFlags) :
                    Engine.HasGetHidden(engineFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool AreNamespacesEnabled(
            CreateFlags createFlags
            )
        {
            return FlagOps.HasFlags(
                createFlags, CreateFlags.UseNamespaces, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IResolve NewResolver(
            Interpreter interpreter,
            ICallFrame frame,
            INamespace @namespace,
            CreateFlags createFlags
            )
        {
            if (AreNamespacesEnabled(createFlags))
            {
                return new _Resolvers.Namespace(new ResolveData(
                    null, null, null, ClientData.Empty, interpreter, 0),
                    frame, @namespace);
            }
            else
            {
                return new _Resolvers.Core(new ResolveData(
                    null, null, null, ClientData.Empty, interpreter, 0));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reflection Support Methods
        public static Type GetTypeWithMostSimilarName(
            TypeList types,
            string text,
            StringComparison comparisonType
            )
        {
            if (types == null)
                return null;

            Type typeWithMostSimilarName = null;
            int mostSimilarNameResult = 0;

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                int similarNameResult = MarshalOps.CompareSimilarTypeNames(
                    type.FullName, text, comparisonType);

                if (typeWithMostSimilarName == null)
                {
                    mostSimilarNameResult = similarNameResult;
                    typeWithMostSimilarName = type;
                    continue;
                }

                if ((mostSimilarNameResult == 0) ||
                    (similarNameResult > mostSimilarNameResult))
                {
                    mostSimilarNameResult = similarNameResult;
                    typeWithMostSimilarName = type;
                }
            }

            if (mostSimilarNameResult > 0)
                return typeWithMostSimilarName;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type GetTypeWithMostMembers(
            TypeList types,
            BindingFlags bindingFlags
            )
        {
            if (types == null)
                return null;

            Type typeWithMostMembers = null;
            MemberInfo[] mostMemberInfos = null;

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                MemberInfo[] memberInfos;

                if (bindingFlags != BindingFlags.Default)
                    memberInfos = type.GetMembers(bindingFlags);
                else
                    memberInfos = type.GetMembers();

                if (memberInfos == null)
                    continue;

                if (typeWithMostMembers == null)
                {
                    mostMemberInfos = memberInfos;
                    typeWithMostMembers = type;
                    continue;
                }

                if ((mostMemberInfos == null) ||
                    (memberInfos.Length > mostMemberInfos.Length))
                {
                    mostMemberInfos = memberInfos;
                    typeWithMostMembers = type;
                }
            }

            return typeWithMostMembers;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesClassTypeSupportInterface(
            Type type,
            Type matchType
            )
        {
            if ((type == null) || !type.IsClass)
                return false;

            if ((matchType == null) || !matchType.IsInterface)
                return false;

            //
            // HACK: Yes, this is horrible.  There must be a cleaner way of
            //       checking if a given type implements a given interface.
            //
            return (type.GetInterface(matchType.FullName) != null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsClassTypeEqualOrSubClass(
            Type type,
            Type matchType,
            bool subClass
            )
        {
            if ((type == null) || !type.IsClass)
                return false;

            if ((matchType == null) || matchType.IsInterface)
                return false;

            if (type.Equals(matchType))
                return true;

            if (subClass && type.IsSubclassOf(matchType))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesClassTypeMatch(
            Type type,
            Type matchType,
            bool subClass
            )
        {
            if ((type != null) && (matchType != null))
            {
                //
                // NOTE: Are we matching against an interface type?
                //
                if (matchType.IsInterface)
                {
                    //
                    // NOTE: Does the class implement the interface?
                    //
                    if (DoesClassTypeSupportInterface(type, matchType))
                        return true;
                }
                else
                {
                    //
                    // NOTE: Are the types equal; otherwise, [optionally]
                    //       is the type a sub-class of the type to match
                    //       against?
                    //
                    if (IsClassTypeEqualOrSubClass(
                            type, matchType, subClass))
                    {
                        return true;
                    }
                }
            }
            else if ((type == null) && (matchType == null))
            {
                //
                // NOTE: If both are null we consider that to be a match.
                //
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetSkipCheckPluginFlags()
        {
            PluginFlags result = PluginFlags.None;

            if (!ShouldCheckStrongNameVerified())
                result |= PluginFlags.SkipVerified;

            if (!ShouldCheckFileTrusted())
                result |= PluginFlags.SkipTrusted;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetAssemblyPluginFlags(
            Assembly assembly,
            byte[] assemblyBytes,
            PluginFlags pluginFlags
            )
        {
            PluginFlags result = PluginFlags.None;

            if (assembly != null)
            {
                //
                // NOTE: Check if the plugin has a StrongName signature.
                //
                StrongName strongName = null;

                if ((AssemblyOps.GetStrongName(assembly,
                        ref strongName) == ReturnCode.Ok) &&
                    (strongName != null))
                {
                    result |= PluginFlags.StrongName;

                    //
                    // NOTE: Skip checking the StrongName signature?
                    //
                    if (!FlagOps.HasFlags(
                            pluginFlags, PluginFlags.SkipVerified, true))
                    {
                        //
                        // NOTE: See if the StrongName signature has really
                        //       been verified by the CLR itself [via the CLR
                        //       native API StrongNameSignatureVerificationEx].
                        //
                        if ((assemblyBytes != null) &&
                            IsStrongNameVerified(assemblyBytes, true))
                        {
                            result |= PluginFlags.Verified;
                        }
                    }
                    else
                    {
                        result |= PluginFlags.SkipVerified;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (assemblyBytes != null)
            {
                //
                // NOTE: Check if the plugin has an Authenticode signature.
                //
                X509Certificate certificate = null;

                if ((AssemblyOps.GetCertificate(
                        assemblyBytes, ref certificate) == ReturnCode.Ok) &&
                    (certificate != null))
                {
                    result |= PluginFlags.Authenticode;

                    //
                    // NOTE: Skip checking the Authenticode signature?
                    //
                    if (!FlagOps.HasFlags(
                            pluginFlags, PluginFlags.SkipTrusted, true))
                    {
                        //
                        // NOTE: See if the Authenticode signature and
                        //       certificate are trusted by the operating
                        //       system [via the Win32 native API
                        //       WinVerifyTrust].
                        //
                        if (IsFileTrusted(assemblyBytes))
                        {
                            result |= PluginFlags.Trusted;
                        }
                    }
                    else
                    {
                        result |= PluginFlags.SkipTrusted;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetAssemblyPluginFlags(
            Assembly assembly,
            PluginFlags pluginFlags
            )
        {
            if (assembly == null)
                return PluginFlags.None;

            PluginFlags result = PluginFlags.None;

            //
            // NOTE: Check if the plugin has a StrongName signature.
            //
            StrongName strongName = null;

            if ((AssemblyOps.GetStrongName(assembly,
                    ref strongName) == ReturnCode.Ok) &&
                (strongName != null))
            {
                result |= PluginFlags.StrongName;

                //
                // NOTE: Skip checking the StrongName signature?
                //
                if (!FlagOps.HasFlags(
                        pluginFlags, PluginFlags.SkipVerified, true))
                {
                    //
                    // NOTE: See if the StrongName signature has really
                    //       been verified by the CLR itself [via the CLR
                    //       native API StrongNameSignatureVerificationEx].
                    //
                    if (IsStrongNameVerified(assembly.Location, true))
                    {
                        result |= PluginFlags.Verified;
                    }
                }
                else
                {
                    result |= PluginFlags.SkipVerified;
                }
            }

            //
            // NOTE: Check if the plugin has an Authenticode signature.
            //
            X509Certificate certificate = null;

            if ((AssemblyOps.GetCertificate(
                    assembly, ref certificate) == ReturnCode.Ok) &&
                (certificate != null))
            {
                result |= PluginFlags.Authenticode;

                //
                // NOTE: Skip checking the Authenticode signature?
                //
                if (!FlagOps.HasFlags(
                        pluginFlags, PluginFlags.SkipTrusted, true))
                {
                    //
                    // NOTE: See if the Authenticode signature and
                    //       certificate are trusted by the operating
                    //       system [via the Win32 native API
                    //       WinVerifyTrust].
                    //
                    if (IsFileTrusted(assembly.Location, IntPtr.Zero))
                    {
                        result |= PluginFlags.Trusted;
                    }
                }
                else
                {
                    result |= PluginFlags.SkipTrusted;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FindPrimaryPlugin(
            Assembly assembly,
            ref string typeName,
            ref Result error
            )
        {
            if (assembly == null)
            {
                error = "invalid assembly";
                return ReturnCode.Error;
            }

            TypeList types = null;
            ResultList errors = null;

            if (!GetMatchingClassTypes(assembly, typeof(IPlugin),
                    typeof(IWrapper), true, ref types, ref errors))
            {
                errors.Insert(0,
                    "no plugins found in assembly");

                error = errors;
                return ReturnCode.Error;
            }

            typeName = null;

            foreach (Type type in types)
            {
                //
                // NOTE: Is the plugin named "Default"?  If so, we need to
                //       skip over it because it is used as the base class
                //       for other plugins.
                //
                if (String.Compare(type.FullName,
                        typeof(_Plugins.Default).FullName,
                        StringOps.SystemStringComparisonType) != 0)
                {
                    PluginFlags flags = AttributeOps.GetPluginFlags(type);

                    if (FlagOps.HasFlags(flags, PluginFlags.Primary, true))
                    {
                        typeName = type.FullName;
                        break;
                    }
                }
            }

            if (typeName != null)
                return ReturnCode.Ok;
            else
                error = "no primary plugin found in assembly";

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetMatchingClassTypes(
            Assembly assembly,
            Type matchType,    // must match this type
            Type nonMatchType, // must not match this type
            bool subClass,     // check sub-classes also (for not match)
            ref TypeList matchingTypes,
            ref ResultList errors
            )
        {
            if (errors == null)
                errors = new ResultList();

            try
            {
                if (assembly == null)
                {
                    errors.Add("invalid assembly");
                    return false;
                }

                if (matchingTypes == null)
                    matchingTypes = new TypeList();

                foreach (Type type in assembly.GetTypes()) /* throw */
                {
                    //
                    // NOTE: Make sure we are dealing with a valid type.
                    //
                    if ((type == null) ||
                        (!type.IsClass && !type.IsValueType))
                    {
                        continue;
                    }

                    //
                    // NOTE: Check the type against the matching criteria
                    //       supplied by the caller.  If it matches, add
                    //       it to the resulting list of types.
                    //
                    if (((matchType == null) || DoesClassTypeMatch(
                            type, matchType, subClass)) &&
                        ((nonMatchType == null) || !DoesClassTypeMatch(
                            type, nonMatchType, subClass)))
                    {
                        matchingTypes.Add(type);
                    }
                }

                return true;
            }
            catch (ReflectionTypeLoadException e)
            {
                errors.Add(e);

                //
                // NOTE: Add loader exceptions, if they are available.
                //
                Exception[] exceptions = e.LoaderExceptions;

                if (exceptions != null)
                {
                    foreach (Exception exception in exceptions)
                    {
                        errors.Add(exception);
                    }
                }
            }
            catch (Exception e)
            {
                errors.Add(e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetMatchingDelegates(
            Assembly assembly,
            Type matchType,          // the delegate type we are expecting
            MethodFlags hasFlags,    // must match flag(s)
            MethodFlags notHasFlags, // must not match flag(s)
            bool hasAll,
            bool notHasAll,
            ref Dictionary<Delegate, MethodFlags> delegates,
            ref ResultList errors
            )
        {
            if (errors == null)
                errors = new ResultList();

            try
            {
                if (assembly == null)
                {
                    errors.Add("invalid assembly");
                    return false;
                }

                if (delegates == null)
                    delegates = new Dictionary<Delegate, MethodFlags>();

                foreach (Type type in assembly.GetTypes()) /* throw */
                {
                    //
                    // NOTE: Make sure we are dealing with a valid type.
                    //
                    if ((type == null) ||
                        (!type.IsClass && !type.IsValueType))
                    {
                        continue;
                    }

                    //
                    // NOTE: Check the type against the matching criteria
                    //       supplied by the caller.  If it matches, add
                    //       it to the resulting list of types.
                    //
                    MethodInfo[] methodInfo =
                        type.GetMethods(DelegateBindingFlags);

                    foreach (MethodInfo thisMethodInfo in methodInfo)
                    {
                        MethodFlags methodFlags = AttributeOps.GetMethodFlags(
                            thisMethodInfo);

                        if (!FlagOps.HasFlags(methodFlags, hasFlags, hasAll) ||
                            FlagOps.HasFlags(methodFlags, notHasFlags, notHasAll))
                        {
                            continue;
                        }

                        Delegate @delegate = Delegate.CreateDelegate(
                            matchType, null, thisMethodInfo, false);

                        if (@delegate != null)
                        {
                            delegates.Add(@delegate, methodFlags);
                        }
                        else
                        {
                            //
                            // NOTE: This is not strictly an "error"; however,
                            //       report it to the caller anyhow.
                            //
                            errors.Add(String.Format(
                                "could not convert method \"{0}\" " +
                                "to a delegate of type \"{1}\"",
                                FormatOps.MethodFullName(
                                    thisMethodInfo.DeclaringType,
                                    thisMethodInfo.Name), matchType.FullName));
                        }
                    }
                }

                return true;
            }
            catch (ReflectionTypeLoadException e)
            {
                errors.Add(e);

                //
                // NOTE: Add loader exceptions, if they are available.
                //
                Exception[] exceptions = e.LoaderExceptions;

                if (exceptions != null)
                {
                    foreach (Exception exception in exceptions)
                    {
                        errors.Add(exception);
                    }
                }
            }
            catch (Exception e)
            {
                errors.Add(e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ICommandData FindCommandData(
            IPluginData pluginData,
            Type type
            )
        {
            if (pluginData == null)
                return null;

            CommandDataList commands = pluginData.Commands;

            if (commands == null)
                return null;

            foreach (ICommandData commandData in commands)
            {
                if (commandData == null)
                    continue;

                if (String.Equals(
                        commandData.TypeName, type.FullName,
                        StringOps.SystemStringComparisonType))
                {
                    return commandData;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PopulatePluginCommands(
            IPlugin plugin,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            Assembly assembly = plugin.Assembly;

            if (assembly == null)
            {
                error = "plugin has invalid assembly";
                return ReturnCode.Error;
            }

            CommandDataList commands = plugin.Commands;

            if (commands == null)
            {
                error = "plugin has invalid command data list";
                return ReturnCode.Error;
            }

            TypeList types = null;
            ResultList errors = null;

            if (!GetMatchingClassTypes(
                    assembly, typeof(ICommand), typeof(IWrapper),
                    true, ref types, ref errors))
            {
                errors.Insert(0,
                    "could not get matching command types");

                error = errors;
                return ReturnCode.Error;
            }

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                CommandFlags flags = AttributeOps.GetCommandFlags(type);

                if (FlagOps.HasFlags(flags, CommandFlags.NoPopulate, true))
                    continue;

                string name = AttributeOps.GetObjectName(type);

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                commands.Add(new CommandData(
                    name, null, null, null, type.FullName, flags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PopulatePluginPolicies(
            IPlugin plugin,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            Assembly assembly = plugin.Assembly;

            if (assembly == null)
            {
                error = "plugin has invalid assembly";
                return ReturnCode.Error;
            }

            PolicyDataList policies = plugin.Policies;

            if (policies == null)
            {
                error = "plugin has invalid policy data list";
                return ReturnCode.Error;
            }

            Dictionary<Delegate, MethodFlags> delegates = null;
            ResultList errors = null;

            if (!GetMatchingDelegates(
                    assembly, typeof(ExecuteCallback),
                    MethodFlags.PluginPolicy | MethodFlags.CommandPolicy |
                    MethodFlags.SubCommandPolicy | MethodFlags.ScriptPolicy |
                    MethodFlags.FilePolicy | MethodFlags.StreamPolicy |
                    MethodFlags.OtherPolicy, MethodFlags.NoAdd, false, false,
                    ref delegates, ref errors))
            {
                errors.Insert(0,
                    "could not get matching policy delegates");

                error = errors;
                return ReturnCode.Error;
            }

            foreach (KeyValuePair<Delegate, MethodFlags> pair in delegates)
            {
                MethodInfo methodInfo = pair.Key.Method;

                if (methodInfo == null)
                    continue;

                Type type = methodInfo.DeclaringType;

                if (type == null)
                    continue;

                policies.Add(new PolicyData(
                    FormatOps.MethodFullName(type, methodInfo.Name),
                    null, null, null, type.FullName, methodInfo.Name,
                    DelegateBindingFlags, pair.Value, PolicyFlags.None,
                    plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PrepareStaticPlugin(
            IPlugin plugin,
            ref Result error
            )
        {
            ReturnCode code = PopulatePluginCommands(plugin, ref error);

            if (code == ReturnCode.Ok)
                code = PopulatePluginPolicies(plugin, ref error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ThrowFeatureNotSupported( /* EXTERNAL USE ONLY */
            IPluginData pluginData,
            string name
            )
        {
            Interpreter interpreter = Interpreter.GetActive();

            if (((interpreter == null) &&
                DefaultThrowOnFeatureNotSupported) ||
                ((interpreter != null) &&
                interpreter.ThrowOnFeatureNotSupported))
            {
                throw new ScriptException(String.Format(
                    "feature \"{0}\" not supported by plugin \"{1}\"",
                    name, pluginData));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Expression Operator Support Methods
        public static ReturnCode GetPluginOperators(
            IPlugin plugin,
            ref List<IOperatorData> operators,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            Assembly assembly = plugin.Assembly;

            if (assembly == null)
            {
                error = "plugin has invalid assembly";
                return ReturnCode.Error;
            }

            TypeList types = null;
            ResultList errors = null;

            if (!GetMatchingClassTypes(
                    assembly, typeof(IOperator), typeof(IWrapper),
                    true, ref types, ref errors))
            {
                errors.Insert(0,
                    "could not get matching operator types");

                error = errors;
                return ReturnCode.Error;
            }

            if (operators == null)
                operators = new List<IOperatorData>();

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                string typeName = type.FullName;

                if ((String.Compare(
                        typeName, typeof(_Operators.Default).FullName,
                        StringOps.SystemStringComparisonType) == 0) ||
                    (String.Compare(
                        typeName, typeof(_Operators.Core).FullName,
                        StringOps.SystemStringComparisonType) == 0))
                {
                    continue;
                }

                OperatorFlags flags = AttributeOps.GetOperatorFlags(type);

                if (FlagOps.HasFlags(flags, OperatorFlags.NoPopulate, true))
                    continue;

                Lexeme lexeme = AttributeOps.GetLexeme(type);
                int operands = AttributeOps.GetOperands(type);

                TypeList operandTypes = null;

                Value.GetTypes(
                    AttributeOps.GetTypeListFlags(type), ref operandTypes);

                string name = AttributeOps.GetObjectName(type);

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                operators.Add(new OperatorData(
                    name, null, null, null, typeName, lexeme, operands,
                    operandTypes, flags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateOperator(
            IOperatorData operatorData,
            ref IOperator @operator,
            ref Result error
            )
        {
            if (operatorData == null)
            {
                error = "invalid operator data";
                return ReturnCode.Error;
            }

            string typeName = operatorData.TypeName;

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return ReturnCode.Error;
            }

            Type type = Type.GetType(typeName, false, true);

            if (type == null)
            {
                error = String.Format(
                    "operator \"{0}\" not found",
                    FormatOps.OperatorTypeName(typeName));

                return ReturnCode.Error;
            }

            try
            {
                @operator = (IOperator)Activator.CreateInstance(
                    type, new object[] { operatorData });

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Expression Function Support Methods
        public static ReturnCode GetPluginFunctions(
            IPlugin plugin,
            ref List<IFunctionData> functions,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            Assembly assembly = plugin.Assembly;

            if (assembly == null)
            {
                error = "plugin has invalid assembly";
                return ReturnCode.Error;
            }

            TypeList types = null;
            ResultList errors = null;

            if (!GetMatchingClassTypes(
                    assembly, typeof(IFunction), typeof(IWrapper),
                    true, ref types, ref errors))
            {
                errors.Insert(0,
                    "could not get matching function types");

                error = errors;
                return ReturnCode.Error;
            }

            if (functions == null)
                functions = new List<IFunctionData>();

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                string typeName = type.FullName;

                if ((String.Compare(
                        typeName, typeof(_Functions.Default).FullName,
                        StringOps.SystemStringComparisonType) == 0) ||
                    (String.Compare(
                        typeName, typeof(_Functions.Core).FullName,
                        StringOps.SystemStringComparisonType) == 0))
                {
                    continue;
                }

                FunctionFlags flags = AttributeOps.GetFunctionFlags(type);

                if (FlagOps.HasFlags(flags, FunctionFlags.NoPopulate, true))
                    continue;

                int arguments = AttributeOps.GetArguments(type);

                TypeList argumentTypes = null;

                Value.GetTypes(
                    AttributeOps.GetTypeListFlags(type), ref argumentTypes);

                string name = AttributeOps.GetObjectName(type);

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                functions.Add(new FunctionData(
                    name, null, null, null, typeName, arguments,
                    argumentTypes, flags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateFunction(
            IFunctionData functionData,
            ref IFunction function,
            ref Result error
            )
        {
            if (functionData == null)
            {
                error = "invalid function data";
                return ReturnCode.Error;
            }

            string typeName = functionData.TypeName;

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return ReturnCode.Error;
            }

            Type type = Type.GetType(typeName, false, true);

            if (type == null)
            {
                error = String.Format(
                    "function \"{0}\" not found",
                    FormatOps.FunctionTypeName(typeName));

                return ReturnCode.Error;
            }

            try
            {
                function = (IFunction)Activator.CreateInstance(
                    type, new object[] { functionData });

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Delegate Support Methods
#if NATIVE && (NATIVE_UTILITY || TCL)
        public static void UnsetNativeDelegates(
            TypeDelegateDictionary delegates,
            TypeBoolDictionary optional
            )
        {
            if (delegates != null)
                delegates.Clear();

            if (optional != null)
                optional.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetNativeDelegates(
            string description,
            TypeIntPtrDictionary addresses,
            TypeDelegateDictionary delegates,
            TypeBoolDictionary optional,
            ref Result error
            )
        {
            if (addresses == null)
            {
                error = "addresses are invalid";
                return ReturnCode.Error;
            }

            if (delegates == null)
            {
                error = "delegates are invalid";
                return ReturnCode.Error;
            }

            try
            {
                TypeList types = new TypeList(delegates.Keys);

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    IntPtr address;

                    if (addresses.TryGetValue(type, out address) &&
                        (address != IntPtr.Zero))
                    {
                        delegates[type] = Marshal.GetDelegateForFunctionPointer(
                            address, type); /* throw */
                    }
                    else
                    {
                        bool value;

                        if ((optional != null) &&
                            optional.TryGetValue(type, out value) && value)
                        {
                            //
                            // NOTE: This is allowed, an optional function was
                            //       not found.
                            //
                            delegates[type] = null;
                        }
                        else
                        {
                            error = String.Format(
                                "cannot locate required {0} function \"{1}\", " +
                                "address not available", description, type.Name);

                            return ReturnCode.Error;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetNativeDelegates(
            string description,
            IntPtr module,
            TypeDelegateDictionary delegates,
            TypeBoolDictionary optional,
            ref Result error
            )
        {
            if (module == IntPtr.Zero)
            {
                error = "module is invalid";
                return ReturnCode.Error;
            }

            if (delegates == null)
            {
                error = "delegates are invalid";
                return ReturnCode.Error;
            }

            try
            {
                TypeList types = new TypeList(delegates.Keys);

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    int lastError;

                    IntPtr address = NativeOps.GetProcAddress(
                        module, type.Name, out lastError); /* throw */

                    if (address != IntPtr.Zero)
                    {
                        delegates[type] = Marshal.GetDelegateForFunctionPointer(
                            address, type); /* throw */
                    }
                    else
                    {
                        bool value;

                        if ((optional != null) &&
                            optional.TryGetValue(type, out value) && value)
                        {
                            //
                            // NOTE: This is allowed, an optional function was
                            //       not found.
                            //
                            delegates[type] = null;
                        }
                        else
                        {
                            //
                            // NOTE: Failure, a required function was not found.
                            //
                            error = String.Format(
                                "cannot locate required {1} function \"{2}\", " +
                                "GetProcAddress({3}, \"{2}\") failed with error {0}: {4}",
                                lastError, description, type.Name, module,
                                NativeOps.GetDynamicLoadingError(lastError));

                            return ReturnCode.Error;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Random Number Support Methods
        public static void GetRandomBytes(
            byte[] bytes /* in, out */
            )
        {
            RandomNumberGenerator randomNumberGenerator = null;

            try
            {
                randomNumberGenerator = RNGCryptoServiceProvider.Create();

                /* NO RESULT */
                GetRandomBytes(randomNumberGenerator, null, bytes);
            }
            finally
            {
                if (randomNumberGenerator != null)
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        randomNumberGenerator, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                        DebugOps.Complain(disposeCode, disposeError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GetRandomBytes(
            RandomNumberGenerator randomNumberGenerator, /* in: may be NULL. */
            Random random,                               /* in: may be NULL. */
            byte[] bytes                                 /* in, out */
            )
        {
            bool gotBytes = false;

            if (randomNumberGenerator != null)
            {
                /* NO RESULT */
                randomNumberGenerator.GetBytes(bytes);

                gotBytes = true;
            }
            else if (random != null)
            {
                /* NO RESULT */
                random.NextBytes(bytes);

                gotBytes = true;
            }

            if (!gotBytes && !GlobalState.GetRandomBytes(bytes))
                throw new ScriptException("could not obtain entropy");
        }

        ///////////////////////////////////////////////////////////////////////

        public static ulong GetRandomNumber()
        {
            RandomNumberGenerator randomNumberGenerator = null;

            try
            {
                randomNumberGenerator = RNGCryptoServiceProvider.Create();

                return GetRandomNumber(randomNumberGenerator, null);
            }
            finally
            {
                if (randomNumberGenerator != null)
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        randomNumberGenerator, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                        DebugOps.Complain(disposeCode, disposeError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ulong GetRandomNumber(
            RandomNumberGenerator randomNumberGenerator, /* in: may be NULL. */
            Random random                                /* in: may be NULL. */
            )
        {
            byte[] bytes = new byte[sizeof(ulong)];

            /* NO RESULT */
            GetRandomBytes(randomNumberGenerator, random, bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Locking Support Methods
        //
        // TODO: Make this method configurable via some runtime mechanism?
        //
        public static bool ShouldCheckDisposedOnExitLock(
            bool locked
            )
        {
#if DEBUG
            //
            // NOTE: When compiled in the "Debug" build configuration, check
            //       if the parent object instance is disposed prior to exiting
            //       the lock via the ISynchronize.ExitLock method if the lock
            //       is not actually held -OR- if the "CheckDisposedOnExitLock"
            //       variable is non-zero.
            //
            if (CheckDisposedOnExitLock)
                return true;

            return !locked;
#else
            //
            // NOTE: When compiled in the "Release" build configuration, check
            //       if the parent object instance is disposed prior to exiting
            //       the lock via the ISynchronize.ExitLock method only if the
            //       lock is not actually held.
            //
            return !locked;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Pointer Support Methods
        public static bool IsValidHandle(
            IntPtr handle
            )
        {
            return ((handle != IntPtr.Zero) &&
                    (handle != INVALID_HANDLE_VALUE));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsValidHandle(
            IntPtr handle,
            ref bool invalid
            )
        {
            if (handle == IntPtr.Zero)
            {
                invalid = false;
                return false;
            }

            if (handle == INVALID_HANDLE_VALUE)
            {
                invalid = true;
                return false;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Module Support Methods
#if NATIVE && LIBRARY
        public static ReturnCode UnloadNativeModule(
            IModule module,
            ref int loaded,
            ref Result error
            )
        {
            if (module == null)
            {
                error = "invalid module";
                return ReturnCode.Error;
            }

            _Wrappers._Module wrapper = module as _Wrappers._Module;

            if (wrapper != null)
                module = wrapper.Object as IModule;

            NativeModule nativeModule = module as NativeModule;

            if (nativeModule == null)
            {
                error = "module is not native";
                return ReturnCode.Error;
            }

            return nativeModule.UnloadNoThrow(ref loaded, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Cancellation Support Methods
        public static CancelFlags GetCancelEvaluateFlags(
            bool interactive,
            bool unwind,
            bool strict
            )
        {
            CancelFlags cancelFlags = CancelFlags.Default;

            if (interactive)
                cancelFlags |= CancelFlags.ForInteractive;

            if (unwind)
                cancelFlags |= CancelFlags.Unwind;

            if (strict)
                cancelFlags |= CancelFlags.StopOnError;

            return cancelFlags;
        }
        #endregion
    }
}
