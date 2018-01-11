/*
 * HostOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("1b0d1e7d-957b-4151-b31f-598393251442")]
    internal static class HostOps
    {
        #region Private Constants
        #region Interactive Prompt Defaults
        private const string PrimaryPrompt = "% ";
        private const string ContinuePrompt = ">\t";

        private const string DebugPrefix = "(debug) ";
        private const string QueuePrefix = "^ ";

        private static readonly StringList DefaultPrompts = new StringList(
            new string[] { null, PrimaryPrompt, ContinuePrompt }
        );
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Host Timeouts
        //
        // HACK: This is not read-only.
        //
        private static int GetTimeout = 2000; /* TODO: Good default? */

        //
        // HACK: This is not read-only.
        //
        private static int InteractiveGetTimeout = 2000; /* TODO: Good default? */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Mode Formatting
        private const string InteractiveModeFormat = "- [{0}]";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constants
        public static readonly string NoFeatureError =
            "interpreter host lacks support for the \"{0}\" feature";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Support Methods
        private static IHost TryGet(
            Interpreter interpreter
            )
        {
            IHost host = null;

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (!locked)
                    {
                        TraceOps.DebugTrace(
                            "TryGet: could not lock interpreter",
                            typeof(HostOps).Name, TracePriority.LockError);

                        int timeout = GetTimeout; /* NO-LOCK */

                        if (timeout >= 0)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TryGet: retry in {0} milliseconds",
                                timeout), typeof(HostOps).Name,
                                TracePriority.HostDebug);

                            interpreter.InternalTryLock(
                                timeout, ref locked); /* TRANSACTIONAL */
                        }
                    }

                    if (locked)
                    {
                        //
                        // BUGFIX: Prevent a race condition between grabbing
                        //         the host and the interpreter being disposed.
                        //         This is necessary because we are called in
                        //         the critical code path of both the Wait and
                        //         WaitVariable methods.
                        //
                        if (!interpreter.Disposed)
                            host = interpreter.Host;
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return host;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IInteractiveHost TryGetInteractive(
            Interpreter interpreter
            )
        {
            IInteractiveHost interactiveHost = null;

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (!locked)
                    {
                        TraceOps.DebugTrace(
                            "TryGetInteractive: could not lock interpreter",
                            typeof(HostOps).Name, TracePriority.LockError);

                        int timeout = InteractiveGetTimeout; /* NO-LOCK */

                        if (timeout >= 0)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "TryGetInteractive: retry in {0} milliseconds",
                                timeout), typeof(HostOps).Name,
                                TracePriority.HostDebug);

                            interpreter.InternalTryLock(
                                timeout, ref locked); /* TRANSACTIONAL */
                        }
                    }

                    if (locked)
                    {
                        //
                        // BUGFIX: Prevent a race condition between grabbing
                        //         the host and the interpreter being disposed.
                        //         This is necessary because we are called in
                        //         the critical code path of both the Wait and
                        //         WaitVariable methods.
                        //
                        if (!interpreter.Disposed)
                            interactiveHost = interpreter.GetInteractiveHost();
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return interactiveHost;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetDefaultPrompt(
            PromptType type,
            PromptFlags flags
            )
        {
            string result = null;

            if (((int)type >= 0) && ((int)type < DefaultPrompts.Count))
            {
                result = DefaultPrompts[(int)type];

                if ((result != null) &&
                    FlagOps.HasFlags(flags, PromptFlags.Queue, true))
                {
                    result = QueuePrefix + result;
                }

                if ((result != null) &&
                    FlagOps.HasFlags(flags, PromptFlags.Debug, true))
                {
                    result = DebugPrefix + result;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetInteractiveMode(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            string interactiveMode = interpreter.InteractiveMode;

            if (!String.IsNullOrEmpty(interactiveMode))
                return String.Format(InteractiveModeFormat, interactiveMode);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasEmptyContent(
            DetailFlags detailFlags
            )
        {
            return FlagOps.HasFlags(
                detailFlags, DetailFlags.EmptyContent, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void HeaderFlagsToDetailFlags(
            HeaderFlags headerFlags,
            ref DetailFlags detailFlags
            )
        {
            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.EmptySection, true))
            {
                detailFlags |= DetailFlags.EmptySection;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.EmptyContent, true))
            {
                detailFlags |= DetailFlags.EmptyContent;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.CallStackAllFrames, true))
            {
                detailFlags |= DetailFlags.CallStackAllFrames;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.DebuggerBreakpoints, true))
            {
                detailFlags |= DetailFlags.DebuggerBreakpoints;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.EngineNative, true))
            {
                detailFlags |= DetailFlags.EngineNative;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostDimensions, true))
            {
                detailFlags |= DetailFlags.HostDimensions;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostFormatting, true))
            {
                detailFlags |= DetailFlags.HostFormatting;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostColors, true))
            {
                detailFlags |= DetailFlags.HostColors;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.HostNames, true))
            {
                detailFlags |= DetailFlags.HostNames;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.TraceCached, true))
            {
                detailFlags |= DetailFlags.TraceCached;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.VariableLinks, true))
            {
                detailFlags |= DetailFlags.VariableLinks;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.VariableSearches, true))
            {
                detailFlags |= DetailFlags.VariableSearches;
            }

            if (FlagOps.HasFlags(
                    headerFlags, HeaderFlags.VariableElements, true))
            {
                detailFlags |= DetailFlags.VariableElements;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: All interpreter members used by this method MUST be safe
        //          to use after the interpreter has been disposed.
        //
        public static bool BuildInterpreterInfoList(
            Interpreter interpreter,
            string name,
            DetailFlags detailFlags,
            ref StringPairList list
            )
        {
            if (list == null)
                list = new StringPairList();

            bool empty = HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

            try
            {
                if (interpreter == null)
                {
                    if (empty)
                        localList.Add("Id", FormatOps.DisplayNull);

                    return true;
                }

                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    localList.Add("Id", interpreter.InternalToString());

                    if (empty || interpreter.Disposed)
                    {
                        localList.Add("Disposed",
                            interpreter.Disposed.ToString());
                    }

                    if (empty || interpreter.Deleted)
                    {
                        localList.Add("Deleted",
                            interpreter.Deleted.ToString());
                    }

                    if (empty || interpreter.InternalExit)
                    {
                        localList.Add("Exit",
                            interpreter.InternalExit.ToString());
                    }

                    if (empty ||
                        (interpreter.InternalExitCode != ResultOps.SuccessExitCode()))
                    {
                        localList.Add("ExitCode",
                            interpreter.InternalExitCode.ToString());
                    }
                }

                return true;
            }
            finally
            {
                if (localList.Count > 0)
                {
                    if (name != null)
                    {
                        list.Add((IPair<string>)null);
                        list.Add((name.Length > 0) ? name : "Interpreter");
                        list.Add((IPair<string>)null);
                    }

                    list.Add(localList);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Sleep Support Methods
        public static void ThreadSleepOrMaybeComplain(
            int milliseconds,
            bool noComplain
            ) /* THREAD-SAFE */
        {
#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning disable 219
#endif
            ReturnCode code; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning restore 219
#endif

            Result error = null;

            code = ThreadSleep(milliseconds, ref error);

#if DEBUG && VERBOSE
            if (!noComplain && (code != ReturnCode.Ok))
                DebugOps.Complain(code, error);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ThreadSleep(
            int milliseconds,
            ref Result error
            ) /* THREAD-SAFE */
        {
            Exception exception = null;

            return ThreadSleep(milliseconds, ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ThreadSleep(
            int milliseconds,
            ref Exception exception,
            ref Result error
            ) /* THREAD-SAFE */
        {
            try
            {
                Thread.Sleep(milliseconds);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                exception = e;
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Yield Support Methods
        public static void Yield() /* THREAD-SAFE */
        {
#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning disable 219
#endif
            ReturnCode code; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning restore 219
#endif

            Result error = null;

            code = Yield(ref error);

#if DEBUG && VERBOSE
            if (code != ReturnCode.Ok)
                DebugOps.Complain(code, error);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Yield(
            ref Result error
            ) /* THREAD-SAFE */
        {
            Exception exception = null;

            return Yield(ref exception, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Yield(
            ref Exception exception,
            ref Result error
            ) /* THREAD-SAFE */
        {
            try
            {
#if NET_40
                Thread.Yield(); /* NOTE: .NET Framework 4.0+ only. */
#else
                Thread.Sleep(0);
#endif

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                exception = e;
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Wrapper Methods
        #region Exit Support Methods
        public static void SetExiting(
            Interpreter interpreter,
            bool exiting
            )
        {
            if (interpreter == null)
                return;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter is already fully disposed, just
                //       do nothing.
                //
                if (interpreter.Disposed)
                    return;

                ///////////////////////////////////////////////////////////////

                SetExiting(
                    interpreter, interpreter.InternalHost, null, false,
                    exiting);

                ///////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
                SetExiting(
                    interpreter, interpreter.IsolatedHost, null, true,
                    exiting);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetExiting(
            Interpreter interpreter,
            IProcessHost processHost,
            string hostName,
            bool isolated,
            bool exiting
            )
        {
            //
            // BUGFIX: Disposal ordering issue.  There is no need to set (or
            //         unset) the host "exiting" flag if it has been disposed.
            //
            try
            {
                if ((processHost != null) && !IsDisposed(processHost) &&
                    FlagOps.HasFlags(
                        processHost.GetHostFlags(), HostFlags.Exit, true))
                {
                    processHost.Exiting = exiting;
                }
            }
            catch (Exception e)
            {
                DebugOps.Complain(
                    interpreter, ReturnCode.Error, String.Format(
                    "caught exception while {0} {1}host {2}: {3}", exiting ?
                    "exiting" : "unexiting", isolated ? "isolated " :
                    String.Empty, FormatOps.WrapOrNull(hostName), e));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sleep Support Methods
        public static void SleepOrMaybeComplain(
            Interpreter interpreter,
            int milliseconds
            ) /* THREAD-SAFE */
        {
            IThreadHost threadHost = TryGet(interpreter);

#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning disable 219
#endif
            ReturnCode code; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning restore 219
#endif

            Result error = null;

            code = Sleep(threadHost, milliseconds, false, ref error);

#if DEBUG && VERBOSE
            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Sleep(
            IThreadHost threadHost,
            int milliseconds,
            bool strict,
            ref Result error
            )
        {
            if (threadHost != null)
            {
                try
                {
                    if (FlagOps.HasFlags(
                            threadHost.GetHostFlags(), HostFlags.Sleep, true))
                    {
                        if (threadHost.Sleep(milliseconds))
                            return ReturnCode.Ok;
                        else
                            error = "host sleep failed";
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            NoFeatureError, HostFlags.Sleep);
                    }
                    else
                    {
                        return ThreadSleep(milliseconds, ref error);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else if (strict)
            {
                error = "interpreter host not available";
            }
            else
            {
                return ThreadSleep(milliseconds, ref error);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Yield Support Methods
        public static void Yield(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            IThreadHost threadHost = TryGet(interpreter);

#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning disable 219
#endif
            ReturnCode code; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD && (!DEBUG || !VERBOSE)
#pragma warning restore 219
#endif

            Result error = null;

            code = Yield(threadHost, false, ref error);

#if DEBUG && VERBOSE
            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Yield(
            IThreadHost threadHost,
            bool strict,
            ref Result error
            )
        {
            if (threadHost != null)
            {
                try
                {
                    if (FlagOps.HasFlags(
                            threadHost.GetHostFlags(), HostFlags.Yield, true))
                    {
                        if (threadHost.Yield())
                            return ReturnCode.Ok;
                        else
                            error = "host yield failed";
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            NoFeatureError, HostFlags.Yield);
                    }
                    else
                    {
                        return Yield(ref error);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else if (strict)
            {
                error = "interpreter host not available";
            }
            else
            {
                return Yield(ref error);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Output Support Methods
        public static bool WriteLine(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return (value != null) ?
                        interactiveHost.WriteLine(value) :
                        interactiveHost.WriteLine();
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WriteResultLine(
            IInteractiveHost interactiveHost,
            ReturnCode code,
            Result result
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.WriteResultLine(code, result);
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteOrConsole(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
                interactiveHost.Write(value);
#if CONSOLE
            else
                Console.Write(value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteLineOrConsole(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
                interactiveHost.WriteLine(value);
#if CONSOLE
            else
                Console.WriteLine(value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteConsoleOrComplain(
            ReturnCode code,
            Result result
            )
        {
            WriteConsoleOrComplain(code, result, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteConsoleOrComplain(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
#if CONSOLE
            try
            {
                Console.WriteLine(ResultOps.Format(code, result, errorLine));
            }
            catch
#endif
            {
                //
                // NOTE: Either there is no System.Console support available
                //       -OR- it somehow failed to produce output.  Complain
                //       about the original issue.
                //
                DebugOps.Complain(code, result);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        private static bool IsNullType(
            Type type
            )
        {
            return (type != null) &&
                ((type == typeof(_Hosts.Null)) ||
                type.IsSubclassOf(typeof(_Hosts.Null)));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisposed(
            IInteractiveHost interactiveHost /* in */
            )
        {
            if (interactiveHost == null)
                return false;

            try
            {
                /* IGNORED */
                interactiveHost.IsOpen(); /* throw */

                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsOpen(
            Interpreter interpreter,
            HostFlags hostFlags,
            ref IInteractiveHost interactiveHost
            )
        {
            interactiveHost = TryGetInteractive(interpreter);

            try
            {
                if (interactiveHost != null)
                {
                    //
                    // HACK: Is the interactive host in an "error state"
                    //       due to being unable to read or write?  This
                    //       is mostly used to help detect the lack of a
                    //       real, usable console.
                    //
                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.ExceptionMask, false))
                    {
                        TraceOps.DebugTrace(
                            "IsOpen: interactive host in error state",
                            typeof(HostOps).Name, TracePriority.HostError);

                        return false;
                    }

                    if (interactiveHost.IsOpen()) /* throw */
                        return true;

                    if (interactiveHost.IsInputRedirected()) /* throw */
                        return true;
                }
                else
                {
                    TraceOps.DebugTrace(
                        "IsOpen: interactive host not available",
                        typeof(HostOps).Name, TracePriority.HostDebug);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(HostOps).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static HeaderFlags GetHeaderFlags(
            IInteractiveHost interactiveHost,
            HeaderFlags @default
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.GetHeaderFlags(); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static HostFlags GetHostFlags(
            IInteractiveHost interactiveHost
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.GetHostFlags(); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return HostFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsInputRedirected(
            IInteractiveHost interactiveHost
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.IsInputRedirected(); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetReadLevels(
            IInteractiveHost interactiveHost
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    return interactiveHost.ReadLevels; /* NON-SHARED ONLY */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ConsoleColor GetHighContrastColor(
            ConsoleColor color
            )
        {
            switch (color)
            {
                case ConsoleColor.Black:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.DarkBlue:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.DarkGreen:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.DarkCyan:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.DarkRed:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.DarkMagenta:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.DarkYellow:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.Gray:
                    {
                        return ConsoleColor.Black;
                    }
                case ConsoleColor.DarkGray:
                    {
                        return ConsoleColor.Black;
                    }
                case ConsoleColor.Blue:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.Green:
                    {
                        return ConsoleColor.Black;
                    }
                case ConsoleColor.Cyan:
                    {
                        return ConsoleColor.Black;
                    }
                case ConsoleColor.Red:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.Magenta:
                    {
                        return ConsoleColor.White;
                    }
                case ConsoleColor.Yellow:
                    {
                        return ConsoleColor.Black;
                    }
                case ConsoleColor.White:
                    {
                        return ConsoleColor.Black;
                    }
                default:
                    {
                        return _ConsoleColor.None;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetColors(
            IColorHost colorHost,
            string name,
            bool foreground,
            bool background,
            bool strict,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
            )
        {
            ReturnCode code;

            //
            // NOTE: Is the interpreter host available (to make color
            //       decisions)?
            //
            if (colorHost != null)
            {
                try
                {
                    //
                    // NOTE: If a "Null"-typed interpreter host is being used
                    //       or the host does not support colors, just skip
                    //       this step.
                    //
                    if (!IsNullType(colorHost.GetType()) && FlagOps.HasFlags(
                            colorHost.GetHostFlags(), HostFlags.AllColors, false))
                    {
                        code = colorHost.GetColors(
                            null, name, foreground, background,
                            ref foregroundColor, ref backgroundColor,
                            ref error);
                    }
                    else if (strict)
                    {
                        error = String.Format(
                            NoFeatureError, HostFlags.AllColors);

                        code = ReturnCode.Error;
                    }
                    else
                    {
                        code = ReturnCode.Ok;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    code = ReturnCode.Error;
                }
            }
            else if (strict)
            {
                error = "interpreter host not available";
                code = ReturnCode.Error;
            }
            else
            {
                code = ReturnCode.Ok;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shell Support Methods
#if SHELL
        public static bool SetTitle(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
            {
                try
                {
                    interactiveHost.Title = value;
                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(HostOps).Name,
                        TracePriority.HostError);
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Library Support Methods
        public static bool HasNoHost(
            ScriptFlags flags,
            ref Result error
            )
        {
            if (FlagOps.HasFlags(flags, ScriptFlags.NoHost, true))
            {
                error = "forbidden from getting script from host";
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetScript(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string name,
            ref ScriptFlags flags,
            ref IClientData clientData,
            ref Result result
            )
        {
            if (HasNoHost(flags, ref result))
                return ReturnCode.Error;

            if (fileSystemHost != null)
            {
                try
                {
                    HostFlags hostFlags = fileSystemHost.GetHostFlags();

#if ISOLATED_PLUGINS
                    //
                    // HACK: If the current interpreter host is running
                    //       in an isolated application domain, use the
                    //       "backup" core host instead.
                    //
                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.Isolated, true))
                    {
                        IFileSystemHost isolatedFileSystemHost = interpreter.IsolatedHost;

                        if (isolatedFileSystemHost != null)
                        {
                            HostFlags isolatedHostFlags = isolatedFileSystemHost.GetHostFlags();

                            if (FlagOps.HasFlags(
                                    isolatedHostFlags, HostFlags.Script, true))
                            {
                                return isolatedFileSystemHost.GetScript(
                                    name, ref flags, ref clientData,
                                    ref result); /* throw */
                            }
                        }
                    }
#endif

                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.Script, true))
                    {
                        return fileSystemHost.GetScript(
                            name, ref flags, ref clientData,
                            ref result); /* throw */
                    }
                    else
                    {
                        result = "interpreter host does not have script support";
                    }
                }
                catch (Exception e)
                {
                    result = e;
                }
            }
            else
            {
                result = "interpreter host not available";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Channel Support Methods
        public static ReturnCode GetStream(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize,
            FileOptions options,
            bool strict,
            ref HostStreamFlags flags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            if (fileSystemHost != null)
            {
                try
                {
                    HostFlags hostFlags = fileSystemHost.GetHostFlags();

#if ISOLATED_PLUGINS
                    //
                    // HACK: If the current interpreter host is running
                    //       in an isolated application domain, use the
                    //       "backup" core host instead.
                    //
                    if (FlagOps.HasFlags(
                            hostFlags, HostFlags.Isolated, true))
                    {
                        IFileSystemHost isolatedFileSystemHost = interpreter.IsolatedHost;

                        if (isolatedFileSystemHost != null)
                        {
                            HostFlags isolatedHostFlags = isolatedFileSystemHost.GetHostFlags();

                            if (FlagOps.HasFlags(
                                    isolatedHostFlags, HostFlags.Stream, true))
                            {
                                HostStreamFlags hostStreamFlags =
                                    flags | isolatedFileSystemHost.StreamFlags;

                                ReturnCode code = isolatedFileSystemHost.GetStream(
                                    path, mode, access, share, bufferSize,
                                    options, ref hostStreamFlags, ref fullPath,
                                    ref stream, ref error);

                                if (code == ReturnCode.Ok)
                                    flags = hostStreamFlags;

                                return code;
                            }
                        }
                    }
#endif

                    if (FlagOps.HasFlags(hostFlags, HostFlags.Stream, true))
                    {
                        HostStreamFlags hostStreamFlags =
                            flags | fileSystemHost.StreamFlags;

                        ReturnCode code = fileSystemHost.GetStream(
                            path, mode, access, share, bufferSize, options,
                            ref hostStreamFlags, ref fullPath, ref stream,
                            ref error);

                        if (code == ReturnCode.Ok)
                            flags = hostStreamFlags;

                        return code;
                    }
                    else if (strict)
                    {
                        error = "interpreter host does not have stream support";
                    }
                    else
                    {
                        return RuntimeOps.NewStream(
                            interpreter, path, mode, access, share, bufferSize,
                            options, ref flags, ref fullPath, ref stream,
                            ref error);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "interpreter host not available";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
        private static void GetProperties(
            Interpreter interpreter,
            ref CreateFlags createFlags,
            ref IHost host,
            ref string profile,
            ref CultureInfo cultureInfo,
            ref ResourceManager resourceManager,
            ref IBinder binder
            )
        {
            createFlags = interpreter.CreateFlags; /* throw */
            host = interpreter.Host; /* throw */
            profile = (host != null) ? host.Profile : null; /* throw */
            cultureInfo = interpreter.CultureInfo; /* throw */
            resourceManager = interpreter.ResourceManager; /* throw */
            binder = interpreter.Binder; /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private static IHostData NewData(
            string typeName,
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            CreateFlags createFlags
            )
        {
            return new HostData(
                null, null, null, ClientData.Empty, typeName, interpreter,
                resourceManager, profile,
                FlagOps.HasFlags(createFlags, CreateFlags.UseAttach, true),
                FlagOps.HasFlags(createFlags, CreateFlags.NoColor, true),
                FlagOps.HasFlags(createFlags, CreateFlags.NoTitle, true),
                FlagOps.HasFlags(createFlags, CreateFlags.NoIcon, true),
                FlagOps.HasFlags(createFlags, CreateFlags.NoProfile, true),
                FlagOps.HasFlags(createFlags, CreateFlags.NoCancel, true));
        }

        ///////////////////////////////////////////////////////////////////////

        public static IHost NewCustom(
            NewHostCallback callback,
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            CreateFlags createFlags
            )
        {
            if (callback == null)
                return null;

            IHost host = callback(NewData(
                null, interpreter, resourceManager, profile, createFlags));

            if (host != null)
            {
                //
                // NOTE: Dynamic fixup.  Since this host was created via the
                //       new host callback delegate, it will [probably] not
                //       have a valid type name; therefore, attempt to see if
                //       this host derives from the core host and then check
                //       the type name and fill it in now, if necessary.
                //
                _Hosts.Core coreHost = host as _Hosts.Core;

                if ((coreHost != null) && (coreHost.TypeName == null))
                    coreHost.TypeName = coreHost.GetType().Name;
            }

            return host;
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        public static IHost NewConsole(
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            CreateFlags createFlags
            )
        {
            return new _Hosts.Console(NewData(
                typeof(_Hosts.Console).Name, interpreter, resourceManager,
                profile, createFlags));
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static IHost NewDiagnostic(
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            CreateFlags createFlags
            )
        {
            return new _Hosts.Diagnostic(NewData(
                typeof(_Hosts.Diagnostic).Name, interpreter, resourceManager,
                profile, createFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CopyAndWrap(
            Interpreter interpreter,
            Type type,
            ref IHost host,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            IHost newHost = null;
            object newObject = null;

            try
            {
                CreateFlags createFlags = CreateFlags.None;
                IHost oldHost = null;
                string profile = null;
                CultureInfo cultureInfo = null;
                ResourceManager resourceManager = null;
                IBinder binder = null;

                GetProperties(
                    interpreter, ref createFlags, ref oldHost, ref profile,
                    ref cultureInfo, ref resourceManager, ref binder);

                if (oldHost == null)
                {
                    error = "interpreter host not available";
                    return ReturnCode.Error;
                }

                BindingFlags bindingFlags = ObjectOps.GetDefaultBindingFlags();

                TypeList types = new TypeList(new Type[] {
                    typeof(IHostData), typeof(IHost), typeof(bool)
                });

                ConstructorInfo constructorInfo = type.GetConstructor(
                    bindingFlags, binder as Binder, types.ToArray(), null); /* throw */

                if (constructorInfo == null)
                {
                    error = String.Format(
                        "type \"{0}\" has no constructors matching " +
                        "parameter types \"{1}\" and binding flags \"{2}\"",
                        type.FullName, types, bindingFlags);

                    return ReturnCode.Error;
                }

                IHostData hostData = NewData(
                    type.Name, interpreter, resourceManager, profile,
                    createFlags);

                newObject = constructorInfo.Invoke(
                    bindingFlags, binder as Binder,
                    new object[] { hostData, oldHost, false }, cultureInfo);

                if (newObject != null)
                {
                    newHost = newObject as IHost;
                }
                else
                {
                    error = String.Format(
                        "could not create an instance of type \"{0}\"",
                        type.FullName);
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                //
                // NOTE: If we created an instance of the specified type and
                //       it cannot be used as an IHost, dispose of it now.
                //
                if ((newObject != null) && (newHost == null))
                {
                    ReturnCode disposeCode;
                    Result disposeError = null;

                    disposeCode = ObjectOps.TryDispose(
                        newObject, ref disposeError);

                    if (disposeCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, disposeCode, disposeError);
                    }
                }
            }

            if (newHost != null)
            {
                host = newHost;
                return ReturnCode.Ok;
            }
            else
            {
                error = String.Format(
                    "type \"{0}\" mismatch, cannot convert to type \"{1}\"",
                    type.FullName, typeof(IHost));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UnwrapAndDispose(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                IHost host = interpreter.Host; /* throw */

                if (host == null)
                {
                    error = "interpreter host not available";
                    return ReturnCode.Error;
                }

                _Hosts.Wrapper wrapperHost = host as _Hosts.Wrapper;

                if (wrapperHost == null)
                {
                    error = String.Format(
                        NoFeatureError, typeof(_Hosts.Wrapper).Name);

                    return ReturnCode.Error;
                }

                IHost baseHost = wrapperHost.BaseHost; /* throw */
                bool baseHostOwned = wrapperHost.BaseHostOwned; /* throw */

                wrapperHost.Dispose(); /* throw */
                wrapperHost = null;

                interpreter.Host = baseHostOwned ? null : baseHost; /* throw */
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
    }
}
