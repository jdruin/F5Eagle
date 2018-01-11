/*
 * NativeConsole.cs --
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

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("7b199e66-8290-4cdc-8312-d02de62683c5")]
    internal static class NativeConsole
    {
        #region Private Constants
        private static readonly bool DefaultNative = true; /* IsMono(); */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the native console
        //       input and output handles managed by this class (below).
        //
        private static readonly object syncRoot = new object();

        //
        // NOTE: This is either zero or the native console input handle
        //       returned via CreateFile for "CONIN$".
        //
        private static IntPtr inputHandle = IntPtr.Zero;

        //
        // NOTE: This is either zero or the native console output handle
        //       returned via CreateFile for "CONOUT$".
        //
        private static IntPtr outputHandle = IntPtr.Zero;
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("6c20c88a-dd55-4e35-a5a5-ec288041c8f4")]
        internal static class UnsafeNativeMethods
        {
            //
            // NOTE: Console input modes.
            //
            internal const uint ENABLE_MOUSE_INPUT = 0x10;

            //
            // NOTE: Console output modes.
            //
            //internal const uint ENABLE_PROCESSED_OUTPUT = 0x01;

            //
            // NOTE: Win32 error numbers.
            //
            internal const int NO_ERROR = 0;
            internal const int ERROR_INVALID_HANDLE = 6;

            //
            // NOTE: Values returned by GetFileType.
            //
            internal const uint FILE_TYPE_UNKNOWN = 0x0;
            internal const uint FILE_TYPE_DISK = 0x1;
            internal const uint FILE_TYPE_CHAR = 0x2;
            internal const uint FILE_TYPE_PIPE = 0x3;
            internal const uint FILE_TYPE_REMOTE = 0x8000;

            //
            // NOTE: Console handles.
            //
            internal const int STD_INPUT_HANDLE = -10;
            internal const int STD_OUTPUT_HANDLE = -11;
            internal const int STD_ERROR_HANDLE = -12;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Special console file names.
            //
            internal const string ConsoleInputFileName = "CONIN$";
            internal const string ConsoleOutputFileName = "CONOUT$";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Special process id.
            //
            internal const int ATTACH_PARENT_PROCESS = -1;

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("cfd3c6be-0c16-4599-8ae8-e2e513daa5f4")]
            internal struct COORD
            {
                public short X;
                public short Y;
            }

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("16757437-8f5f-4550-b986-6406b5954705")]
            internal struct SMALL_RECT
            {
                public short Left;
                public short Top;
                public short Right;
                public short Bottom;
            }

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("9b96c63e-606d-4b1e-8be7-0945aa7da03a")]
            internal struct CONSOLE_SCREEN_BUFFER_INFO
            {
                public COORD size;
                public COORD cursorPosition;
                public short attributes;
                public SMALL_RECT window;
                public COORD maximumWindowSize;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern uint GetFileType(IntPtr handle);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleMode(
                IntPtr handle, ref uint mode
            );

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
             /* UNDOCUMENTED */
             [DllImport(DllName.Kernel32,
                 CallingConvention = CallingConvention.Winapi,
                 SetLastError = true)]
             internal static extern IntPtr GetConsoleInputWaitHandle();
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleMode(
                IntPtr handle,
                uint mode
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GenerateConsoleCtrlEvent(
                ControlEvent controlEvent,
                uint processGroupId
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern IntPtr GetStdHandle(int nStdHandle);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            internal static extern COORD GetLargestConsoleWindowSize(
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            #region Dead Code
#if DEAD_CODE
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetConsoleScreenBufferInfo(
                IntPtr handle,
                ref CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetConsoleActiveScreenBuffer(
                IntPtr handle
            );
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetStdHandle(
                int nStdHandle,
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi)]
            internal static extern IntPtr GetConsoleWindow();

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FlushConsoleInputBuffer(
                IntPtr handle
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AttachConsole(int processId);

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool AllocConsole();

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FreeConsole();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static bool SetInputWaitHandle(
            bool @set,
            ref Result error
            )
        {
            bool result = false;

            try
            {
                IntPtr handle = UnsafeNativeMethods.GetConsoleInputWaitHandle();

                if (!NativeOps.IsValidHandle(handle))
                {
                    error = "invalid console input wait handle";
                    return false;
                }

                if (@set)
                    result = NativeOps.UnsafeNativeMethods.SetEvent(handle);
                else
                    result = NativeOps.UnsafeNativeMethods.ResetEvent(handle);

                if (!result)
                    error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static IntPtr GetHandle(
            ChannelType channelType,
            ref Result error
            )
        {
            return GetHandle(channelType, DefaultNative, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr GetHandle(
            ChannelType channelType,
            bool native,
            ref Result error
            )
        {
            switch (channelType)
            {
                case ChannelType.Input:
                    return GetInputHandle(native, ref error);
                case ChannelType.Output:
                    return GetOutputHandle(native, ref error);
                case ChannelType.Error:
                    return GetErrorHandle(native, ref error);
                default:
                    error = "unsupported console channel";
                    return IntPtr.Zero;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr GetInputHandle(
            bool native,
            ref Result error
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (native)
                {
                    handle = UnsafeNativeMethods.GetStdHandle(
                        UnsafeNativeMethods.STD_INPUT_HANDLE);

                    bool invalid = false;

                    if (!NativeOps.IsValidHandle(handle, ref invalid))
                    {
                        if (invalid)
                            error = NativeOps.GetErrorMessage();
                        else
                            error = "invalid native input handle";
                    }
                }
                else
                {
#if CONSOLE
                    handle = ConsoleOps.GetInputHandle(ref error);
#else
                    error = "not implemented";
#endif
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr GetOutputHandle(
            bool native,
            ref Result error
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (native)
                {
                    handle = UnsafeNativeMethods.GetStdHandle(
                        UnsafeNativeMethods.STD_OUTPUT_HANDLE);

                    bool invalid = false;

                    if (!NativeOps.IsValidHandle(handle, ref invalid))
                    {
                        if (invalid)
                            error = NativeOps.GetErrorMessage();
                        else
                            error = "invalid native output handle";
                    }
                }
                else
                {
#if CONSOLE
                    handle = ConsoleOps.GetOutputHandle(ref error);
#else
                    error = "not implemented";
#endif
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr GetErrorHandle(
            bool native,
            ref Result error
            )
        {
            IntPtr handle = IntPtr.Zero;

            try
            {
                if (native)
                {
                    //
                    // NOTE: This is always done natively.  The System.Console
                    //       class does not keep track of the standard error
                    //       channel.
                    //
                    handle = UnsafeNativeMethods.GetStdHandle(
                        UnsafeNativeMethods.STD_ERROR_HANDLE);

                    bool invalid = false;

                    if (!NativeOps.IsValidHandle(handle, ref invalid))
                    {
                        if (invalid)
                            error = NativeOps.GetErrorMessage();
                        else
                            error = "invalid native error handle";
                    }
                }
                else
                {
                    error = "not implemented";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ResetHandles(
            IntPtr inputHandle,
            IntPtr outputHandle,
            ref Result error
            )
        {
            if (!SetHandle(
                    ChannelType.Input, inputHandle, ref error))
            {
                return ReturnCode.Error;
            }

            if (!SetHandle(
                    ChannelType.Output, outputHandle, ref error))
            {
                return ReturnCode.Error;
            }

            if (!SetHandle(
                    ChannelType.Error, outputHandle, ref error))
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetHandle(
            ChannelType channelType,
            IntPtr handle,
            ref Result error
            )
        {
            switch (channelType)
            {
                case ChannelType.Input:
                    return SetInputHandle(handle, ref error);
                case ChannelType.Output:
                    return SetOutputHandle(handle, ref error);
                case ChannelType.Error:
                    return SetErrorHandle(handle, ref error);
                default:
                    error = "unsupported console channel";
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetInputHandle(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                if (UnsafeNativeMethods.SetStdHandle(
                        UnsafeNativeMethods.STD_INPUT_HANDLE, handle))
                {
#if CONSOLE
                    if (ConsoleOps.ResetStreams(
                            ChannelType.Input, ref error) == ReturnCode.Ok)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
#else
                    return true;
#endif
                }
                else
                {
                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetOutputHandle(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                if (UnsafeNativeMethods.SetStdHandle(
                        UnsafeNativeMethods.STD_OUTPUT_HANDLE, handle))
                {
#if CONSOLE
                    if (ConsoleOps.ResetStreams(
                            ChannelType.Output, ref error) == ReturnCode.Ok)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
#else
                    return true;
#endif
                }
                else
                {
                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetErrorHandle(
            IntPtr handle,
            ref Result error
            )
        {
            try
            {
                if (UnsafeNativeMethods.SetStdHandle(
                        UnsafeNativeMethods.STD_ERROR_HANDLE, handle))
                {
#if CONSOLE
                    if (ConsoleOps.ResetStreams(
                            ChannelType.Error, ref error) == ReturnCode.Ok)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
#else
                    return true;
#endif
                }
                else
                {
                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode IsHandleRedirected(
            IntPtr handle,
            ref bool redirected,
            ref Result error
            )
        {
            if (!NativeOps.IsValidHandle(handle))
            {
                error = "invalid handle";
                return ReturnCode.Error;
            }

            try
            {
                uint type = UnsafeNativeMethods.GetFileType(handle);

                if ((type != UnsafeNativeMethods.FILE_TYPE_UNKNOWN) ||
                    (Marshal.GetLastWin32Error() == UnsafeNativeMethods.NO_ERROR))
                {
                    type &= ~UnsafeNativeMethods.FILE_TYPE_REMOTE;

                    if (type == UnsafeNativeMethods.FILE_TYPE_CHAR)
                    {
                        uint mode = 0;

                        if (UnsafeNativeMethods.GetConsoleMode(
                                handle, ref mode))
                        {
                            //
                            // NOTE: We do not care about the mode, this is a
                            //       console simply because GetConsoleMode
                            //       succeeded.
                            //
                            redirected = false;
                        }
                        else if (Marshal.GetLastWin32Error() ==
                                UnsafeNativeMethods.ERROR_INVALID_HANDLE)
                        {
                            //
                            // NOTE: The handle appears to be valid (see above)
                            //       and it does not appear to be a console
                            //       because GetConsoleMode set the error to
                            //       ERROR_INVALID_HANDLE; therefore, it has
                            //       probably been redirected to something that
                            //       is not a console.
                            //
                            redirected = true;
                        }
                        else
                        {
                            //
                            // NOTE: The handle appears to be valid (see above)
                            //       and it is most likely a console because
                            //       GetConsoleMode did not set the error to
                            //       ERROR_INVALID_HANDLE.
                            //
                            redirected = false;
                        }
                    }
                    else
                    {
                        //
                        // NOTE: The handle appears to be valid (see above); It
                        //       cannot be a console because it is not being
                        //       reported as a character device; therefore, it
                        //       must have been redirected.
                        //
                        redirected = true;
                    }
                }
                else
                {
                    //
                    // NOTE: The handle appears to be valid; however, we cannot
                    //       determine the file type.  We must assume that it
                    //       has not been redirected.
                    //
                    redirected = false;
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

        public static bool IsOpen()
        {
            return GetConsoleWindow() != IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IntPtr GetConsoleWindow()
        {
            try
            {
                return UnsafeNativeMethods.GetConsoleWindow();
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(NativeConsole).Name,
                    TracePriority.NativeError);
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLargestWindowSize(
            ref int width,
            ref int height,
            ref Result error
            )
        {
            try
            {
                IntPtr handle;
                bool invalid = false;

                handle = UnsafeNativeMethods.GetStdHandle(
                    UnsafeNativeMethods.STD_OUTPUT_HANDLE);

                if (NativeOps.IsValidHandle(handle, ref invalid))
                {
                    UnsafeNativeMethods.COORD coordinates =
                        UnsafeNativeMethods.GetLargestConsoleWindowSize(
                            handle);

                    if ((coordinates.X != 0) || (coordinates.Y != 0))
                    {
                        width = coordinates.X;
                        height = coordinates.Y;

                        return ReturnCode.Ok;
                    }

                    error = NativeOps.GetErrorMessage();
                }
                else if (invalid)
                {
                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid native output handle";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FlushInputBuffer(
            ref Result error
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    ChannelType.Input, DefaultNative, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    if (UnsafeNativeMethods.FlushConsoleInputBuffer(handle))
                        return ReturnCode.Ok;

                    error = NativeOps.GetErrorMessage();
                }
                else
                {
                    error = "invalid handle";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsInputHandle(
            IntPtr handle
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return (handle == inputHandle);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsOutputHandle(
            IntPtr handle
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return (handle == outputHandle);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the _Hosts.Default.BuildHostInfoList method.
        //
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot)
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (inputHandle != IntPtr.Zero))
                    localList.Add("InputHandle", inputHandle.ToString());

                if (empty || (outputHandle != IntPtr.Zero))
                    localList.Add("OutputHandle", outputHandle.ToString());

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Native Console");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CloseHandles(
            ref Result error
            )
        {
            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (NativeOps.IsValidHandle(inputHandle))
                    {
                        if (!NativeOps.UnsafeNativeMethods.CloseHandle(
                                inputHandle))
                        {
                            error = NativeOps.GetErrorMessage();

                            return ReturnCode.Error;
                        }

                        inputHandle = IntPtr.Zero;
                    }

                    if (NativeOps.IsValidHandle(outputHandle))
                    {
                        if (!NativeOps.UnsafeNativeMethods.CloseHandle(
                                outputHandle))
                        {
                            error = NativeOps.GetErrorMessage();

                            return ReturnCode.Error;
                        }

                        outputHandle = IntPtr.Zero;
                    }
                }

                return ResetHandles(IntPtr.Zero, IntPtr.Zero, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode FixupHandles(
            ref Result error
            )
        {
            if (CloseHandles(ref error) != ReturnCode.Ok)
                return ReturnCode.Error;

            IntPtr localInputHandle = IntPtr.Zero;
            IntPtr localOutputHandle = IntPtr.Zero;

            try
            {
                localInputHandle = PathOps.UnsafeNativeMethods.CreateFile(
                    UnsafeNativeMethods.ConsoleInputFileName,
                    FileAccessMask.GENERIC_READ | FileAccessMask.GENERIC_WRITE,
                    FileShareMode.FILE_SHARE_READ, IntPtr.Zero,
                    FileCreationDisposition.OPEN_EXISTING,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE, IntPtr.Zero);

                if (!NativeOps.IsValidHandle(localInputHandle))
                {
                    error = NativeOps.GetErrorMessage();

                    return ReturnCode.Error;
                }

                localOutputHandle = PathOps.UnsafeNativeMethods.CreateFile(
                    UnsafeNativeMethods.ConsoleOutputFileName,
                    FileAccessMask.GENERIC_READ | FileAccessMask.GENERIC_WRITE,
                    FileShareMode.FILE_SHARE_WRITE, IntPtr.Zero,
                    FileCreationDisposition.OPEN_EXISTING,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE, IntPtr.Zero);

                if (!NativeOps.IsValidHandle(localOutputHandle))
                {
                    error = NativeOps.GetErrorMessage();

                    return ReturnCode.Error;
                }

                if (ResetHandles(localInputHandle,
                        localOutputHandle, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    inputHandle = localInputHandle;
                    outputHandle = localOutputHandle;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                if ((localOutputHandle != IntPtr.Zero) &&
                    !IsOutputHandle(localOutputHandle))
                {
                    try
                    {
                        if (NativeOps.UnsafeNativeMethods.CloseHandle(
                                localOutputHandle))
                        {
                            localOutputHandle = IntPtr.Zero;
                        }
                        else
                        {
                            //
                            // HACK: At this point, the local handle may be
                            //       "leaked"; however, the call to CloseHandle
                            //       failed so there is nothing else we can do.
                            //
                            string closeError = NativeOps.GetErrorMessage();

                            TraceOps.DebugTrace(String.Format(
                                "FixupHandles: could not close output handle: {0}",
                                closeError), typeof(NativeConsole).Name,
                                TracePriority.NativeError);
                        }
                    }
                    catch (Exception e)
                    {
                        //
                        // HACK: At this point, the local handle may be
                        //       "leaked"; however, the call to CloseHandle
                        //       failed so there is nothing else we can do.
                        //
                        TraceOps.DebugTrace(
                            e, typeof(NativeConsole).Name,
                            TracePriority.NativeError);
                    }
                }

                ///////////////////////////////////////////////////////////////

                if ((localInputHandle != IntPtr.Zero) &&
                    !IsInputHandle(localInputHandle))
                {
                    try
                    {
                        if (NativeOps.UnsafeNativeMethods.CloseHandle(
                                localInputHandle))
                        {
                            localInputHandle = IntPtr.Zero;
                        }
                        else
                        {
                            //
                            // HACK: At this point, the local handle may be
                            //       "leaked"; however, the call to CloseHandle
                            //       failed so there is nothing else we can do.
                            //
                            string closeError = NativeOps.GetErrorMessage();

                            TraceOps.DebugTrace(String.Format(
                                "FixupHandles: could not close input handle: {0}",
                                closeError), typeof(NativeConsole).Name,
                                TracePriority.NativeError);
                        }
                    }
                    catch (Exception e)
                    {
                        //
                        // HACK: At this point, the local handle may be
                        //       "leaked"; however, the call to CloseHandle
                        //       failed so there is nothing else we can do.
                        //
                        TraceOps.DebugTrace(
                            e, typeof(NativeConsole).Name,
                            TracePriority.NativeError);
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static ReturnCode Attach(
            bool force,
            ref Result error
            )
        {
            try
            {
                if (!force && IsOpen())
                {
                    TraceOps.DebugTrace(
                        "Attach: console already open",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }

                if (UnsafeNativeMethods.AttachConsole(
                        UnsafeNativeMethods.ATTACH_PARENT_PROCESS))
                {
                    TraceOps.DebugTrace(
                        "Attach: attached parent console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return FixupHandles(ref error);
                }

                error = NativeOps.GetErrorMessage();
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

        public static ReturnCode Open(
            bool force,
            ref Result error
            )
        {
            try
            {
                if (!force && IsOpen())
                {
                    TraceOps.DebugTrace(
                        "Open: console already open",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }

                if (UnsafeNativeMethods.AllocConsole())
                {
                    TraceOps.DebugTrace(
                        "Open: allocated new console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return FixupHandles(ref error);
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AttachOrOpen(
            bool force,
            bool attach,
            ref Result error
            )
        {
            try
            {
                if (!force && IsOpen())
                {
                    TraceOps.DebugTrace(
                        "AttachOrOpen: console already open",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return ReturnCode.Ok;
                }

                if (attach && UnsafeNativeMethods.AttachConsole(
                        UnsafeNativeMethods.ATTACH_PARENT_PROCESS))
                {
                    TraceOps.DebugTrace(
                        "AttachOrOpen: attached parent console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return FixupHandles(ref error);
                }

                if (UnsafeNativeMethods.AllocConsole())
                {
                    TraceOps.DebugTrace(
                        "AttachOrOpen: allocated new console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    return FixupHandles(ref error);
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Close(
            ref Result error
            )
        {
            try
            {
                if (UnsafeNativeMethods.FreeConsole())
                {
                    TraceOps.DebugTrace(
                        "Close: freed existing console",
                        typeof(NativeConsole).Name,
                        TracePriority.NativeDebug);

                    lock (syncRoot)
                    {
                        inputHandle = IntPtr.Zero;
                        outputHandle = IntPtr.Zero;
                    }

                    if (ResetHandles(IntPtr.Zero,
                            IntPtr.Zero, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    channelType, DefaultNative, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    if (UnsafeNativeMethods.GetConsoleMode(handle, ref mode))
                        return ReturnCode.Ok;

                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    channelType, DefaultNative, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    if (UnsafeNativeMethods.SetConsoleMode(handle, mode))
                        return ReturnCode.Ok;

                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ChangeMode(
            ChannelType channelType,
            bool enable,
            uint mode,
            ref Result error
            )
        {
            try
            {
                IntPtr handle = GetHandle(
                    channelType, DefaultNative, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    uint currentMode = 0;

                    if (UnsafeNativeMethods.GetConsoleMode(
                            handle, ref currentMode))
                    {
                        if (enable)
                            currentMode |= mode;  /* NOTE: Add mode(s). */
                        else
                            currentMode &= ~mode; /* NOTE: Remove mode(s). */

                        if (UnsafeNativeMethods.SetConsoleMode(
                                handle, currentMode))
                        {
                            return ReturnCode.Ok;
                        }
                    }

                    error = NativeOps.GetErrorMessage();
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SendControlEvent(
            ControlEvent @event,
            ref Result error
            )
        {
            try
            {
                if (UnsafeNativeMethods.GenerateConsoleCtrlEvent(@event, 0))
                    return ReturnCode.Ok;

                error = NativeOps.GetErrorMessage();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CloseStandardInput(
            ref Result error
            )
        {
            ResultList errors = null;

            try
            {
                Result localError; /* REUSED */

                //
                // TODO: Huh, output?  Why?
                //
                localError = null;

                IntPtr outputHandle = GetHandle(
                    ChannelType.Output, ref localError);

                if ((outputHandle == IntPtr.Zero) && (localError != null))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                //
                // TODO: Huh, output?  Why?
                //
                localError = null;

                IntPtr errorHandle = GetHandle(
                    ChannelType.Error, ref localError);

                if ((errorHandle == IntPtr.Zero) && (localError != null))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                //
                // NOTE: Does one of the [console] output handles need to be
                //       closed (i.e. the one used for the screen buffer).
                //
                if (NativeOps.IsValidHandle(outputHandle) ||
                    NativeOps.IsValidHandle(errorHandle))
                {
                    //
                    // NOTE: Does the [console] output handle look like it
                    //       needs to be closed?
                    //
                    if (NativeOps.IsValidHandle(outputHandle) &&
                        NativeOps.UnsafeNativeMethods.CloseHandle(outputHandle))
                    {
                        //
                        // NOTE: Notify other native and managed code that the
                        //       [console] output handle is no longer valid.
                        //
                        localError = null;

                        if (!SetHandle(
                                ChannelType.Output, IntPtr.Zero, ref localError))
                        {
                            if (localError != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localError);
                            }

                            if (errors != null)
                                error = errors;

                            return ReturnCode.Error;
                        }

                        //
                        // NOTE: If the [console] output and error handles are
                        //       the same, notify other native and managed code
                        //       that the [console] error handle is [also] no
                        //       longer valid.
                        //
                        localError = null;

                        if ((errorHandle == outputHandle) && !SetHandle(
                                ChannelType.Error, IntPtr.Zero, ref localError))
                        {
                            if (localError != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(localError);
                            }

                            if (errors != null)
                                error = errors;

                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: If the [console] output and error handles are the
                    //       same, we are already done; otherwise, we need to
                    //       [re-]check and possibly close the error handle.
                    //
                    if (errorHandle == outputHandle)
                    {
                        //
                        // NOTE: All handles cleaned up, success.
                        //
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: Does the [console] error handle look like it
                        //       needs to be closed?
                        //
                        if (NativeOps.IsValidHandle(errorHandle) &&
                            NativeOps.UnsafeNativeMethods.CloseHandle(errorHandle))
                        {
                            localError = null;

                            if (!SetHandle(
                                    ChannelType.Error, IntPtr.Zero, ref localError))
                            {
                                if (localError != null)
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Add(localError);
                                }

                                if (errors != null)
                                    error = errors;

                                return ReturnCode.Error;
                            }

                            //
                            // NOTE: All handles cleaned up, success.
                            //
                            return ReturnCode.Ok;
                        }
                    }

                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(NativeOps.GetErrorMessage());
                }
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
            }

            if (errors != null)
                error = errors;

            return ReturnCode.Error;
        }
    }
}
