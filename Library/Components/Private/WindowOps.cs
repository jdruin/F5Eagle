/*
 * WindowOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NATIVE && WINDOWS
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#endif

using System.Runtime.InteropServices;

#if NATIVE && WINDOWS
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;
using System.Threading;
#endif

#if WINFORMS
using System.Windows.Forms;
#endif

#if NATIVE && WINDOWS
using Microsoft.Win32.SafeHandles;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

#if NATIVE && WINDOWS
using Eagle._Components.Private.Delegates;
#endif

using Eagle._Constants;

namespace Eagle._Components.Private
{
#if NATIVE && WINDOWS
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("9e185cdc-bb2e-42bf-8d66-a176a18df7f1")]
    internal static class WindowOps
    {
        #region Private Static Data
#if NATIVE && WINDOWS
        private static bool traceWait = false;
#endif

#if (NATIVE && WINDOWS) || WINFORMS
        private static bool traceException = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("b8dd6936-cd78-4a1f-b51e-e34b254e66bd")]
        internal static class UnsafeNativeMethods
        {
            internal const uint WM_NULL = 0x0000;

            internal const uint WM_CLOSE = 0x0010;

            internal const uint WM_GETICON = 0x007F;
            internal const uint WM_SETICON = 0x0080;

            internal const uint ICON_SMALL = 0;
            internal const uint ICON_BIG = 1;

            internal const uint VK_RETURN = 0x0D;

            internal const uint WM_KEYDOWN = 0x100;
            internal const uint WM_KEYUP = 0x101;

            internal const uint QS_NONE = 0x0000;
            internal const uint QS_KEY = 0x0001;
            internal const uint QS_MOUSEMOVE = 0x0002;
            internal const uint QS_MOUSEBUTTON = 0x0004;
            internal const uint QS_POSTMESSAGE = 0x0008;
            internal const uint QS_TIMER = 0x0010;
            internal const uint QS_PAINT = 0x0020;
            internal const uint QS_SENDMESSAGE = 0x0040;
            internal const uint QS_HOTKEY = 0x0080;
            internal const uint QS_ALLPOSTMESSAGE = 0x0100;
            internal const uint QS_RAWINPUT = 0x0400;

            internal const uint QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON);
            internal const uint QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT);

            internal const uint QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE |
                                                QS_TIMER | QS_PAINT |
                                                QS_HOTKEY);

            internal const uint QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE |
                                               QS_TIMER | QS_PAINT |
                                               QS_HOTKEY | QS_SENDMESSAGE);

            internal const uint MWMO_NONE = 0x0;
            internal const uint MWMO_WAITALL = 0x1;
            internal const uint MWMO_ALERTABLE = 0x2;
            internal const uint MWMO_INPUTAVAILABLE = 0x4;

            internal const uint MWMO_DEFAULT = MWMO_ALERTABLE |
                                               MWMO_INPUTAVAILABLE;

            internal const int ERROR_INVALID_THREAD_ID = 1444;

            internal const int MAX_CLASS_NAME = 257; // 256 + NUL (per MSDN, "The maximum length for lpszClassName is 256")

            ///////////////////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("b01f5772-a193-4cac-9a2c-6c73fd452e6e")]
            internal struct LASTINPUTINFO
            {
                public uint cbSize;
                public uint dwTime;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern uint MsgWaitForMultipleObjectsEx(uint count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] handles, uint milliseconds, uint wakeMask, uint flags);

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern uint WaitForMultipleObjectsEx(uint count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] handles, [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint milliseconds, [MarshalAs(UnmanagedType.Bool)] bool alertable);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
            internal static extern IntPtr SendMessage(IntPtr hWnd, uint message, UIntPtr wParam, IntPtr lParam);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PostMessage(IntPtr hWnd, uint message, UIntPtr wParam, IntPtr lParam);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PostThreadMessage(int threadId, uint message, UIntPtr wParam, IntPtr lParam);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi)]
            internal static extern uint GetQueueStatus(uint flags);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi)]
            internal static extern int GetWindowThreadProcessId(IntPtr hWnd, ref int processId);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool EnumWindows(EnumWindowCallback callback, IntPtr lParam);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int GetClassName(IntPtr hWnd, StringBuilder buffer, int count);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern int GetWindowText(IntPtr hWnd, StringBuilder buffer, int count);

            [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetLastInputInfo(ref LASTINPUTINFO pLastInputInfo);

            // [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr GetFocus();
            //
            // [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr SetFocus(IntPtr hWnd);
            //
            // [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr GetParent(IntPtr hWnd);
            //
            // [DllImport(DllName.User32, CallingConvention = CallingConvention.Winapi)]
            // internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool YesOrNo(
            string text,
            string caption,
            bool @default
            )
        {
#if WINFORMS
            if (IsInteractive())
            {
                return MessageBox.Show(
                    text, caption, MessageBoxButtons.YesNo) == DialogResult.Yes;
            }
#endif

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        public static DialogResult YesOrNoOrCancel(
            string text,
            string caption,
            DialogResult @default
            )
        {
            if (IsInteractive())
            {
                return MessageBox.Show(
                    text, caption, MessageBoxButtons.YesNoCancel);
            }

            return @default;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetInteractiveHandle()
        {
            return IsInteractive() ? IntPtr.Zero : INVALID_HANDLE_VALUE;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsInteractive()
        {
#if MONO || MONO_HACKS
            //
            // HACK: On Mono, the "*.UserInteractive" properties always
            //       return false; therefore, just use the "Interactive"
            //       property of the active interpreter in that case.
            //
            if (CommonOps.Runtime.IsMono())
            {
                Interpreter interpreter = Interpreter.GetActive();

                if (interpreter != null)
                    return interpreter.Interactive;
            }
#endif

#if WINFORMS
            return SystemInformation.UserInteractive;
#else
            return Environment.UserInteractive;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        public static ReturnCode GetHandle(
            Control control,
            ref IntPtr handle,
            ref Result error
            )
        {
            if (control != null)
            {
                try
                {
                    //
                    // HACK: This should not be necessary.  However, it does
                    //       appear that a control (including a Form) will not
                    //       allow you to simply query the handle [to check it
                    //       against null] without attempting to automatically
                    //       create it first (which requires thread affinity).
                    //
                    Type type = control.GetType();

                    handle = (IntPtr)type.InvokeMember("HandleInternal",
                        MarshalOps.PrivateInstanceGetPropertyBindingFlags,
                        null, control, null);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid control";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetHandle(
            Menu menu,
            ref IntPtr handle,
            ref Result error
            )
        {
            if (menu != null)
            {
                try
                {
                    //
                    // HACK: This should not be necessary.  However, it does
                    //       appear that a menu will not allow you to simply
                    //       query the handle [to check it against null]
                    //       without attempting to automatically create it
                    //       first (which requires thread affinity).
                    //
                    Type type = menu.GetType();

                    handle = (IntPtr)type.InvokeMember("handle",
                        MarshalOps.PrivateInstanceGetFieldBindingFlags,
                        null, menu, null);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid menu";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static DialogResult Complain( /* NOT USED */
            ReturnCode code,
            Result result
            )
        {
            return Complain(ResultOps.Format(code, result));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static DialogResult Complain(
            string message
            )
        {
            if (IsInteractive() && GlobalState.IsPrimaryThread())
            {
                return MessageBox.Show(
                    message, Application.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                DebugOps.Log(0, DebugOps.DefaultCategory, String.Format(
                    "{0}{1}", message, Environment.NewLine));

                return DialogResult.OK;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && WINFORMS
        private static bool HasMessageQueue(
            int threadId,
            ref Result error
            )
        {
            try
            {
                if (UnsafeNativeMethods.PostThreadMessage(threadId,
                        UnsafeNativeMethods.WM_NULL, UIntPtr.Zero, IntPtr.Zero))
                {
                    return true;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    if (lastError == UnsafeNativeMethods.ERROR_INVALID_THREAD_ID)
                        return false;

                    error = NativeOps.GetErrorMessage(lastError);
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WINFORMS
        private static void DoEvents()
        {
            try
            {
                Application.DoEvents();
            }
            catch
            {
                if (PlatformOps.IsWindowsOperatingSystem())
                    throw;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ProcessEvents(
            Interpreter interpreter, /* NOT USED */
            ref Result error
            )
        {
            try
            {
#if NATIVE && WINDOWS
                //
                // NOTE: If this thread has a message queue and there
                //       appears to be anything in it, process it now.
                //
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    if (HasMessageQueue(
                            GlobalState.GetCurrentNativeThreadId(),
                            ref error))
                    {
                        uint flags = UnsafeNativeMethods.QS_ALLINPUT;

                        if (UnsafeNativeMethods.GetQueueStatus(flags) != 0)
#endif
                            DoEvents();
#if NATIVE && WINDOWS
                    }
                }
                else
                {
                    DoEvents();
                }
#endif

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static ReturnCode GetLastInputTickCount(
            ref Result result
            )
        {
            try
            {
                UnsafeNativeMethods.LASTINPUTINFO lastInputInfo =
                    new UnsafeNativeMethods.LASTINPUTINFO();

                lastInputInfo.cbSize = (uint)Marshal.SizeOf(
                    typeof(UnsafeNativeMethods.LASTINPUTINFO));

                if (UnsafeNativeMethods.GetLastInputInfo(
                        ref lastInputInfo))
                {
                    result = lastInputInfo.dwTime;
                    return ReturnCode.Ok;
                }
                else
                {
                    result = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Enumerator Class
#if NET_40
        [SecurityCritical()]
#else
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
        [ObjectId("12dd831f-79c8-4e34-a7e7-16eaf46bcbd2")]
        internal sealed class WindowEnumerator
        {
            #region Private Data
            private StringBuilder buffer;
            private Dictionary<IntPtr, Pair<string>> windows;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public WindowEnumerator()
            {
                windows = new Dictionary<IntPtr, Pair<string>>();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private bool EnumWindowCallback(
                IntPtr hWnd,
                IntPtr lParam
                )
            {
                try
                {
                    string text = null;
                    int length = UnsafeNativeMethods.GetWindowTextLength(hWnd);

                    if (length > 0)
                    {
                        length++; /* NUL terminator */

                        buffer = StringOps.NewStringBuilder(buffer, length);

                        if (UnsafeNativeMethods.GetWindowText(
                                hWnd, buffer, length) > 0)
                        {
                            text = buffer.ToString();
                        }
                    }

                    string @class = null;
                    length = UnsafeNativeMethods.MAX_CLASS_NAME;

                    buffer = StringOps.NewStringBuilder(buffer, length);

                    if (UnsafeNativeMethods.GetClassName(
                            hWnd, buffer, length) > 0)
                    {
                        @class = buffer.ToString();
                    }

                    windows[hWnd] = new Pair<string>(@class, text);
                    return true;
                }
                catch (Exception e)
                {
                    if (traceException)
                    {
                        //
                        // NOTE: Nothing much we can do here except log the
                        //       failure.
                        //
                        TraceOps.DebugTrace(
                            e, typeof(EnumWindowCallback).Name,
                            TracePriority.NativeError);
                    }
                }

                return false;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Methods
            public ReturnCode Populate(
                ref bool returnValue,
                ref Result error
                )
            {
                try
                {
                    returnValue = UnsafeNativeMethods.EnumWindows(
                        EnumWindowCallback, IntPtr.Zero);

                    if (!returnValue)
                        error = NativeOps.GetErrorMessage();

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    if (traceException)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(WindowOps).Name,
                            TracePriority.NativeError);
                    }

                    error = e;
                }

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public Dictionary<IntPtr, Pair<string>> GetWindows()
            {
                return new Dictionary<IntPtr, Pair<string>>(windows);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CloseWindow(
            IntPtr handle,
            ref bool returnValue,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    IntPtr result = UnsafeNativeMethods.SendMessage(
                        handle, UnsafeNativeMethods.WM_CLOSE, UIntPtr.Zero,
                        IntPtr.Zero);

                    returnValue = (result == IntPtr.Zero);

                    if (returnValue)
                        return ReturnCode.Ok;
                    else
                        error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid window handle";
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetWindowText(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                int length = UnsafeNativeMethods.GetWindowTextLength(handle);

                if (length > 0)
                {
                    length++; /* NUL terminator */

                    StringBuilder buffer = StringOps.NewStringBuilder(length);

                    if (UnsafeNativeMethods.GetWindowText(
                            handle, buffer, length) > 0)
                    {
                        return buffer.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetWindowThreadProcessId(
            IntPtr handle,
            ref int processId,
            ref int threadId,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    int localProcessId = 0;
                    int localThreadId = UnsafeNativeMethods.GetWindowThreadProcessId(
                            handle, ref localProcessId);

                    if (localThreadId != 0)
                    {
                        processId = localProcessId;
                        threadId = localThreadId;

                        return ReturnCode.Ok;
                    }

                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid window handle";
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SimulateReturnKey(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                if (handle != IntPtr.Zero)
                {
                    UIntPtr virtualKey = new UIntPtr(UnsafeNativeMethods.VK_RETURN);

                    if (UnsafeNativeMethods.PostMessage(
                            handle, UnsafeNativeMethods.WM_KEYDOWN,
                            virtualKey, IntPtr.Zero))
                    {
                        if (UnsafeNativeMethods.PostMessage(
                                handle, UnsafeNativeMethods.WM_KEYUP,
                                virtualKey, IntPtr.Zero))
                        {
                            return ReturnCode.Ok;
                        }
                    }

                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid window handle";
                }
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static ReturnCode WaitForSingleHandle(
            WaitHandle waitHandle,
            int milliseconds,
            bool userInterface
            )
        {
            uint returnValue = 0;

            return WaitForSingleHandle(
                waitHandle, milliseconds, userInterface, ref returnValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode WaitForSingleHandle(
            WaitHandle waitHandle,
            int milliseconds,
            bool userInterface,
            ref uint returnValue
            )
        {
            ReturnCode code;
            Result error = null;

            code = WaitForSingleHandle(
                waitHandle, milliseconds, userInterface, ref returnValue, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(code, error);

            if (traceWait)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitForSingleHandle: exited, waitHandle = {0}, " +
                    "milliseconds = {1}, userInterface = {2}, " +
                    "returnValue = {3}, code = {4}, error = {5}",
                    FormatOps.DisplayWaitHandle(waitHandle), milliseconds,
                    userInterface, returnValue, code, FormatOps.WrapOrNull(
                    true, true, error)), typeof(WindowOps).Name,
                    TracePriority.NativeDebug);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: Contains a "Constrained Execution Region", modify carefully.
        //
        private static ReturnCode WaitForSingleHandle(
            WaitHandle waitHandle,
            int milliseconds,
            bool userInterface,
            ref uint returnValue,
            ref Result error
            )
        {
            SafeWaitHandle safeWaitHandle = null;
            bool success = false;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                if (waitHandle == null)
                {
                    error = "invalid wait handle";
                    return ReturnCode.Error;
                }

                safeWaitHandle = waitHandle.SafeWaitHandle;

                if (safeWaitHandle == null)
                {
                    error = "invalid safe wait handle";
                    return ReturnCode.Error;
                }

                safeWaitHandle.DangerousAddRef(ref success);

                if (!success)
                {
                    error = "failed to add reference to safe wait handle";
                    return ReturnCode.Error;
                }

                IntPtr[] handles = { safeWaitHandle.DangerousGetHandle() };

                if (handles[0] == IntPtr.Zero)
                {
                    error = "failed to get native handle from safe wait handle";
                    return ReturnCode.Error;
                }

                if (userInterface)
                    returnValue = UnsafeNativeMethods.MsgWaitForMultipleObjectsEx(
                        1, handles, (uint)milliseconds, UnsafeNativeMethods.QS_ALLINPUT,
                        UnsafeNativeMethods.MWMO_DEFAULT);
                else
                    returnValue = UnsafeNativeMethods.WaitForMultipleObjectsEx(
                        1, handles, false, (uint)milliseconds, true);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }
            finally
            {
                if (success)
                {
                    safeWaitHandle.DangerousRelease();
                    success = false;
                }
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode WaitForMultipleHandles(
            WaitHandle[] waitHandles,
            int milliseconds,
            bool userInterface
            )
        {
            uint returnValue = 0;

            return WaitForMultipleHandles(
                waitHandles, milliseconds, userInterface, ref returnValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode WaitForMultipleHandles(
            WaitHandle[] waitHandles,
            int milliseconds,
            bool userInterface,
            ref uint returnValue
            )
        {
            ReturnCode code;
            Result error = null;

            code = WaitForMultipleHandles(
                waitHandles, milliseconds, userInterface, ref returnValue,
                ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(code, error);

            if (traceWait)
            {
                TraceOps.DebugTrace(String.Format(
                    "WaitForMultipleHandles: exited, waitHandles = {0}, " +
                    "milliseconds = {1}, userInterface = {2}, " +
                    "returnValue = {3}, code = {4}, error = {5}",
                    FormatOps.DisplayWaitHandles(waitHandles), milliseconds,
                    userInterface, returnValue, code, FormatOps.WrapOrNull(
                    true, true, error)), typeof(WindowOps).Name,
                    TracePriority.NativeDebug);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: Contains a "Constrained Execution Region", modify carefully.
        //
        private static ReturnCode WaitForMultipleHandles(
            WaitHandle[] waitHandles,
            int milliseconds,
            bool userInterface,
            ref uint returnValue,
            ref Result error
            )
        {
            SafeWaitHandle[] safeWaitHandles = null;
            bool[] success = null;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                if (waitHandles == null)
                {
                    error = "invalid wait handles";
                    return ReturnCode.Error;
                }

                int length = waitHandles.Length;

                if (length <= 0)
                {
                    error = "no wait handles";
                    return ReturnCode.Error;
                }

                safeWaitHandles = new SafeWaitHandle[length];
                success = new bool[length];

                IntPtr[] handles = new IntPtr[length];

                for (int index = 0; index < length; index++)
                {
                    if (waitHandles[index] == null)
                    {
                        error = String.Format(
                            "invalid wait handle {0}", index);

                        return ReturnCode.Error;
                    }

                    safeWaitHandles[index] = waitHandles[index].SafeWaitHandle;

                    if (safeWaitHandles[index] == null)
                    {
                        error = String.Format(
                            "invalid safe wait handle {0}", index);

                        return ReturnCode.Error;
                    }

                    safeWaitHandles[index].DangerousAddRef(ref success[index]);

                    if (!success[index])
                    {
                        error = String.Format(
                            "failed to add reference to safe wait handle {0}",
                            index);

                        return ReturnCode.Error;
                    }

                    handles[index] = safeWaitHandles[index].DangerousGetHandle();

                    if (handles[index] == IntPtr.Zero)
                    {
                        error = String.Format(
                            "failed to get native handle from safe wait handle {0}",
                            index);

                        return ReturnCode.Error;
                    }
                }

                if (userInterface)
                {
                    returnValue = UnsafeNativeMethods.MsgWaitForMultipleObjectsEx(
                        (uint)length, handles, (uint)milliseconds,
                        UnsafeNativeMethods.QS_ALLINPUT,
                        UnsafeNativeMethods.MWMO_DEFAULT);
                }
                else
                {
                    returnValue = UnsafeNativeMethods.WaitForMultipleObjectsEx(
                        (uint)length, handles, false, (uint)milliseconds, true);
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (traceException)
                {
                    TraceOps.DebugTrace(
                        e, typeof(WindowOps).Name,
                        TracePriority.NativeError);
                }

                error = e;
            }
            finally
            {
                if ((safeWaitHandles != null) && (success != null))
                {
                    int length = safeWaitHandles.Length;

                    for (int index = 0; index < length; index++)
                    {
                        if (success[index])
                        {
                            safeWaitHandles[index].DangerousRelease();
                            success[index] = false;
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }
#endif
    }
}
