/*
 * ConsoleOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !CONSOLE
#error "This file cannot be compiled or used properly with console support disabled."
#endif

using System;
using System.IO;
using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    [ObjectId("69b75e27-9fd6-4cbe-844d-9c00002d0088")]
    internal static class ConsoleOps
    {
        #region Private Constants
        //
        // NOTE: The type for the public System.IO.MonoIO type.  This is
        //       used, via reflection, by various methods of this class.
        //
        private static readonly Type MonoIoType = CommonOps.Runtime.IsMono() ?
            Type.GetType("System.IO.MonoIO") : null;

        //
        // NOTE: The type for the System.Console class.  This is used, via
        //       reflection, by various methods of this class.
        //
        private static readonly Type ConsoleType = typeof(System.Console);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Console Support Methods (.NET Framework 2.0 - 4.7.1)
        #region Per-Process Console (Setup) Reference Count Support
        public static string GetEnvironmentVariable(
            long processId
            )
        {
            return String.Format(
                EnvVars.EagleLibraryHostsConsole, processId);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetEnvironmentVariableAndValue(
            ref string variable,
            ref string value
            )
        {
            long currentProcessId = ProcessOps.GetId();

            foreach (long processId in new long[] {
                    ProcessOps.GetParentId(), currentProcessId })
            {
                if (processId == 0)
                    continue;

                string localVariable = GetEnvironmentVariable(processId);

                if (String.IsNullOrEmpty(localVariable))
                    continue;

                string localValue = null;

                if (CommonOps.Environment.DoesVariableExist(
                        localVariable, ref localValue))
                {
                    variable = localVariable;
                    value = localValue;

                    return;
                }
            }

            //
            // NOTE: Always fallback to the console reference count
            //       environment variable for the current process.
            //
            //       This is the common case as there is not normally
            //       a parent process that is also using the console
            //       reference counting mechanism (which is specific
            //       to the Eagle core library).
            //
            variable = GetEnvironmentVariable(currentProcessId);
            value = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CheckAndMaybeModifyReferenceCount(
            bool? increment,
            ref int referenceCount
            )
        {
            try
            {
                string variable = null;
                string value = null;

                GetEnvironmentVariableAndValue(ref variable, ref value);

                int localReferenceCount = 0;

                if (!String.IsNullOrEmpty(variable) &&
                    ((value == null) || (Value.GetInteger2(
                        value, ValueFlags.AnyInteger, null,
                        ref localReferenceCount) == ReturnCode.Ok)))
                {
                    if (increment != null)
                    {
                        if ((bool)increment)
                            localReferenceCount++;
                        else
                            localReferenceCount--;

                        if (localReferenceCount > 0)
                        {
                            CommonOps.Environment.SetVariable(
                                variable, localReferenceCount.ToString());
                        }
                        else
                        {
                            CommonOps.Environment.UnsetVariable(variable);
                        }
                    }

                    referenceCount = localReferenceCount;
                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ConsoleOps).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSetup()
        {
            int referenceCount = 0;

            return CheckAndMaybeModifyReferenceCount(
                null, ref referenceCount) && (referenceCount > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsShared()
        {
            int referenceCount = 0;

            return CheckAndMaybeModifyReferenceCount(
                null, ref referenceCount) && (referenceCount > 1);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MarkSetup(
            bool setup
            )
        {
            int referenceCount = 0;

            if (!CheckAndMaybeModifyReferenceCount(
                    setup, ref referenceCount))
            {
                return false;
            }

            if (setup)
                return (referenceCount == 1);
            else
                return (referenceCount <= 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ResetStreams(
            ChannelType channelType,
            ref Result error
            )
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
            {
                if (ConsoleType == null)
                {
                    error = "invalid system console type";
                    return ReturnCode.Error;
                }

                //
                // HACK: Because the System.Console object in the .NET Framework
                //       provides no means to reset the underlying input/output
                //       streams, we must do it here by force.
                //
                try
                {
                    //
                    // NOTE: Which standard channels do we want to reset?
                    //
                    bool resetInput = FlagOps.HasFlags(
                        channelType, ChannelType.Input, true);

                    bool resetOutput = FlagOps.HasFlags(
                        channelType, ChannelType.Output, true);

                    bool resetError = FlagOps.HasFlags(
                        channelType, ChannelType.Error, true);

                    if (resetInput)
                    {
                        ConsoleType.InvokeMember("_consoleInputHandle",
                            MarshalOps.PrivateStaticSetFieldBindingFlags,
                            null, null, new object[] { IntPtr.Zero });
                    }

                    if (resetOutput)
                    {
                        ConsoleType.InvokeMember("_consoleOutputHandle",
                            MarshalOps.PrivateStaticSetFieldBindingFlags,
                            null, null, new object[] { IntPtr.Zero });
                    }

                    if (resetInput)
                    {
                        ConsoleType.InvokeMember("_in",
                            MarshalOps.PrivateStaticSetFieldBindingFlags,
                            null, null, new object[] { null });
                    }

                    if (resetOutput)
                    {
                        ConsoleType.InvokeMember("_out",
                            MarshalOps.PrivateStaticSetFieldBindingFlags,
                            null, null, new object[] { null });
                    }

                    if (resetError)
                    {
                        ConsoleType.InvokeMember("_error",
                            MarshalOps.PrivateStaticSetFieldBindingFlags,
                            null, null, new object[] { null });
                    }

#if NET_40
                    if (CommonOps.Runtime.IsFramework45OrHigher())
                    {
                        if (resetInput)
                        {
                            ConsoleType.InvokeMember("_stdInRedirectQueried",
                                MarshalOps.PrivateStaticSetFieldBindingFlags,
                                null, null, new object[] { false });
                        }

                        if (resetOutput)
                        {
                            ConsoleType.InvokeMember("_stdOutRedirectQueried",
                                MarshalOps.PrivateStaticSetFieldBindingFlags,
                                null, null, new object[] { false });
                        }

                        if (resetError)
                        {
                            ConsoleType.InvokeMember("_stdErrRedirectQueried",
                                MarshalOps.PrivateStaticSetFieldBindingFlags,
                                null, null, new object[] { false });
                        }
                    }
#endif

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
#endif
            {
                //
                // NOTE: This is not supported (or necessary) on Mono;
                //       therefore, just fake success.
                //
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ResetCachedInputRecord(
            ref Result error
            )
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
            {
                if (ConsoleType == null)
                {
                    error = "invalid system console type";
                    return ReturnCode.Error;
                }

                try
                {
                    object cachedInputRecord = ConsoleType.InvokeMember(
                       "_cachedInputRecord",
                       MarshalOps.PrivateStaticGetFieldBindingFlags, null,
                       null, null);

                    if (cachedInputRecord != null)
                    {
                        Marshal.WriteInt16(cachedInputRecord, 0, 0);

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "invalid internal system console cached " +
                            "input record";
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
#endif
            {
                //
                // NOTE: This is not supported (or necessary) on Mono;
                //       therefore, just fake success.
                //
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static IntPtr GetInputHandle(
            ref Result error
            )
        {
            bool isMono = CommonOps.Runtime.IsMono();
            Type type = isMono ? MonoIoType : ConsoleType;

            if (type == null)
            {
                error = "invalid system console type";
                return IntPtr.Zero;
            }

            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       handles, we must do it here by force.
            //
            try
            {
                IntPtr handle = (IntPtr)type.InvokeMember(
                    isMono ? "ConsoleInput" : "ConsoleInputHandle",
                    isMono ? MarshalOps.PublicStaticGetPropertyBindingFlags :
                    MarshalOps.PrivateStaticGetPropertyBindingFlags, null,
                    null, null);

                if (!RuntimeOps.IsValidHandle(handle))
                    error = "invalid console input handle";

                return handle;
            }
            catch (Exception e)
            {
                error = e;
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IntPtr GetOutputHandle(
            ref Result error
            )
        {
            bool isMono = CommonOps.Runtime.IsMono();
            Type type = isMono ? MonoIoType : ConsoleType;

            if (type == null)
            {
                error = "invalid system console type";
                return IntPtr.Zero;
            }

            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       handles, we must do it here by force.
            //
            try
            {
                IntPtr handle = (IntPtr)type.InvokeMember(
                    isMono ? "ConsoleOutput" : "ConsoleOutputHandle",
                    isMono ? MarshalOps.PublicStaticGetPropertyBindingFlags :
                    MarshalOps.PrivateStaticGetPropertyBindingFlags, null,
                    null, null);

                if (!RuntimeOps.IsValidHandle(handle))
                    error = "invalid console output handle";

                return handle;
            }
            catch (Exception e)
            {
                error = e;
            }

            return IntPtr.Zero;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetInputStream(
            ref Stream stream,
            ref Result error
            )
        {
            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       streams, we must do it here by force.
            //
            try
            {
                TextReader textReader = System.Console.In; /* throw */

                if (textReader == null)
                {
                    error = "invalid system console input text reader";
                    return ReturnCode.Error;
                }

                Type type = textReader.GetType();

                if (type == null)
                {
                    error = "invalid system console input text reader type";
                    return ReturnCode.Error;
                }

                StreamReader streamReader = type.InvokeMember(
                    CommonOps.Runtime.IsMono() ? "reader" : "_in",
                    MarshalOps.PrivateInstanceGetFieldBindingFlags,
                    null, textReader, null) as StreamReader; /* throw */

                if (streamReader != null)
                {
                    stream = streamReader.BaseStream; /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid internal system console input " +
                        "stream reader";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetOutputStream(
            ref Stream stream,
            ref Result error
            )
        {
            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       streams, we must do it here by force.
            //
            try
            {
                TextWriter textWriter = System.Console.Out; /* throw */

                if (textWriter == null)
                {
                    error = "invalid system console output text writer";
                    return ReturnCode.Error;
                }

                Type type = textWriter.GetType();

                if (type == null)
                {
                    error = "invalid system console output text writer type";
                    return ReturnCode.Error;
                }

                StreamWriter streamWriter = type.InvokeMember(
                    CommonOps.Runtime.IsMono() ? "writer" : "_out",
                    MarshalOps.PrivateInstanceGetFieldBindingFlags,
                    null, textWriter, null) as StreamWriter; /* throw */

                if (streamWriter != null)
                {
                    stream = streamWriter.BaseStream; /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid internal system console output " +
                        "stream writer";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetErrorStream(
            ref Stream stream,
            ref Result error
            )
        {
            //
            // HACK: Because the System.Console object in the .NET Framework
            //       provides no means to query the underlying input/output
            //       streams, we must do it here by force.
            //
            try
            {
                TextWriter textWriter = System.Console.Error; /* throw */

                if (textWriter == null)
                {
                    error = "invalid system console error text writer";
                    return ReturnCode.Error;
                }

                Type type = textWriter.GetType();

                if (type == null)
                {
                    error = "invalid system console error text writer type";
                    return ReturnCode.Error;
                }

                StreamWriter streamWriter = type.InvokeMember(
                    CommonOps.Runtime.IsMono() ? "writer" : "_out",
                    MarshalOps.PrivateInstanceGetFieldBindingFlags,
                    null, textWriter, null) as StreamWriter; /* throw */

                if (streamWriter != null)
                {
                    stream = streamWriter.BaseStream; /* throw */

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid internal system console error " +
                        "stream writer";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UnhookControlHandler(
            bool strict,
            ref Result error
            )
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
            {
                if (ConsoleType == null)
                {
                    error = "invalid system console type";
                    return ReturnCode.Error;
                }

                //
                // HACK: Because the System.Console object in the .NET Framework
                //       provides no means to unhook it from its native console
                //       callbacks, we must do it here by force.
                //
                try
                {
                    //
                    // NOTE: First, grab the private static ControlCHooker
                    //       field from the static System.Console object.
                    //
                    object hooker = ConsoleType.InvokeMember("_hooker",
                        MarshalOps.PrivateStaticGetFieldBindingFlags, null,
                        null, null);

                    if (hooker != null)
                    {
                        //
                        // NOTE: Next, grab and validate the type for the
                        //       ControlCHooker field.
                        //
                        Type type = hooker.GetType();

                        if (type == null)
                        {
                            error = "invalid internal system console hook type";
                            return ReturnCode.Error;
                        }

                        //
                        // NOTE: Next, call the Unhook method of the returned
                        //       ControlCHooker object so that it will unhook
                        //       itself from its native callbacks.
                        //
                        type.InvokeMember("Unhook",
                            MarshalOps.PrivateInstanceMethodBindingFlags,
                            null, hooker, null);

                        //
                        // NOTE: Finally, null out the private static (cached)
                        //       ControlCHooker field inside the System.Console
                        //       object so that it will know when it needs to
                        //       be re-hooked later.
                        //
                        ConsoleType.InvokeMember("_hooker",
                            MarshalOps.PrivateStaticSetFieldBindingFlags,
                            null, null, new object[] { null });

                        return ReturnCode.Ok;
                    }
                    else if (strict)
                    {
                        error = "invalid internal system console hook";
                    }
                    else
                    {
                        //
                        // NOTE: There is no console hook present.
                        //
                        return ReturnCode.Ok;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
#endif
            {
                //
                // NOTE: This is not supported (or necessary) on Mono;
                //       therefore, just fake success.
                //
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteCore(
            string value
            )
        {
            ConsoleColor savedForegroundColor;

            savedForegroundColor = Console.ForegroundColor; /* throw */

            //
            // TODO: Maybe change the background color here as well?
            //
            Console.ForegroundColor = HostOps.GetHighContrastColor(
                Console.BackgroundColor); /* throw */

            try
            {
                Console.WriteLine(value); /* throw */
            }
            finally
            {
                Console.ForegroundColor = savedForegroundColor; /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void WriteCoreNoThrow(
            string value
            )
        {
            try
            {
                WriteCore(value); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ConsoleOps).Name,
                    TracePriority.HostError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WritePrompt(
            string value
            )
        {
            WriteCoreNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteError(
            string value
            )
        {
            WriteCoreNoThrow(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteComplaint(
            string value
            )
        {
            WriteCoreNoThrow(value);
        }
        #endregion
    }
}
