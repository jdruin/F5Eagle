/*
 * DebugOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("1d388444-db3b-41b5-a23e-b25084d1c94b")]
    internal static class DebugOps
    {
        #region Public Constants
        public static readonly string DefaultCategory =
            System.Diagnostics.Debugger.DefaultCategory;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        private static readonly string TextWriteExceptionFormat =
            "write of text failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string HostWriteExceptionFormat =
            "write to host failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string TestWriteExceptionFormat =
            "write via test failed ({0}): {1}{2}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly int ComplainRetryLimit = 3;
        private static readonly int ComplainRetryMilliseconds = 750;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: The current number of calls to Complain() that are active
        //       on this thread.  This number should always be zero or one.
        //
        // BUGFIX: Previously, this was a global value, not per thread, and
        //         that was wrong.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static int complainLevels = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called.  It is
        //       per-thread and never reset.
        //
        [ThreadStatic()] /* ThreadSpecificData */
        private static long complainCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The number of times that Complain() has been called.  It is
        //       global (AppDomain) and never reset.
        //
        private static long globalComplainCount = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The most recent complaint message seen by this subsystem.
        //
        private static string globalComplaint = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        // NOTE: If this value is non-zero, exceptions thrown by the complain
        //       callback are simply ignored; otherwise, the default complain
        //       mechanism will be used after an exception is caught from the
        //       callback.
        //
        private static bool IgnoreOnCallbackThrow = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TESTING* This is purposely not marked as read-only.
        //
        private static bool SkipCurrentForComplainViaTest = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *TUNING* This is purposely not marked as read-only.
        //
        private static bool IgnoreQuietForComplainViaTest = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Stack Trace Methods
        private static bool ContainsMethodName(
            StringList skipNames,
            string name
            )
        {
            if (skipNames == null)
                return false;

            //
            // TODO: *PERF* Should this take into account case?  If not,
            //       the alternative Contains method overload could be
            //       used; however, it will not perform as well.
            //
            return skipNames.Contains(name);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static StackTrace GetStackTrace(
            int skipFrames
            )
        {
            //
            // NOTE: Always skip this method.
            //
            return new StackTrace(skipFrames + 1, true);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetStackTraceString()
        {
            StackTrace stackTrace = GetStackTrace(1);

            if (stackTrace != null)
                return stackTrace.ToString();

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetMethodName(
            int skipFrames,
            StringList skipNames,
            bool skipDebug,
            bool nameOnly,
            string defaultName
            )
        {
            string result = defaultName;

            try
            {
                //
                // NOTE: Create a new stack trace based on the current
                //       execution stack.
                //
                StackTrace stackTrace = GetStackTrace(0);

                if (stackTrace != null)
                {
                    //
                    // NOTE: Always skip this method (i.e. we start with at
                    //       least one, not zero).
                    //
                    int count = stackTrace.FrameCount;

                    for (int index = skipFrames + 1; index < count; index++)
                    {
                        //
                        // NOTE: Get the stack frame for the current index.
                        //
                        StackFrame stackFrame = stackTrace.GetFrame(index);

                        if (stackFrame == null)
                            continue;

                        //
                        // NOTE: Get the method for this stack frame.
                        //
                        MethodBase methodBase = stackFrame.GetMethod();

                        if (methodBase == null)
                            continue;

                        //
                        // NOTE: Get the type for this method.
                        //
                        Type type = methodBase.DeclaringType;

                        if (type == null)
                            continue;

                        //
                        // NOTE: If requested, skip over all methods from this
                        //       class, the FormatOps class, and the TraceOps
                        //       class.  None of these classes, apart from one
                        //       place in the FormatOps class, need to resolve
                        //       a method name that belongs to them.
                        //
                        if (skipDebug)
                        {
                            if (type == typeof(DebugOps))
                                continue;

                            if (type == typeof(FormatOps))
                                continue;

                            if (type == typeof(TraceOps))
                                continue;

                            if (type == typeof(Utility))
                                continue;
                        }

                        //
                        // NOTE: Grab the name of the MethodBase object.
                        //
                        string methodBaseName = methodBase.Name;

                        //
                        // NOTE: Format the method name with its full type
                        //       name.
                        //
                        string methodFullName = FormatOps.MethodQualifiedFullName(
                            type, methodBaseName);

                        //
                        // NOTE: Format the method name with its type name.
                        //
                        string methodName = FormatOps.MethodQualifiedName(
                            type, methodBaseName);

                        //
                        // NOTE: Do we need to skip this method (based on
                        //       the name and/or the type qualified method
                        //       name)?
                        //
                        if ((skipNames == null) ||
                            (!ContainsMethodName(skipNames, methodBaseName) &&
                             !ContainsMethodName(skipNames, methodFullName) &&
                             !ContainsMethodName(skipNames, methodName)))
                        {
                            //
                            // NOTE: Return only the bare method name
                            //       -OR- the method name formatted
                            //       with its declaring type.
                            //
                            if (nameOnly)
                                result = methodBaseName;
                            else
                                result = methodFullName;

                            break;
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetMethodName(
            MethodBase skipMethod,
            string defaultName
            )
        {
            string skipMethodName = null;

            if (skipMethod != null)
            {
                skipMethodName = FormatOps.MethodName(
                    skipMethod.DeclaringType, skipMethod.Name);
            }

            //
            // NOTE: We are doing this on behalf of the direct caller;
            //       therefore, skip this method AND the calling method.
            //
            return GetMethodName(2,
                new StringList(skipMethodName), true, false, defaultName);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Stack Trace Methods
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MethodBase GetMethod(
            int skipFrames
            )
        {
            try
            {
                //
                // NOTE: Create a new stack trace based on the current
                //       execution stack.
                //
                StackTrace stackTrace = GetStackTrace(0);

                if (stackTrace != null)
                {
                    //
                    // NOTE: Always skip this method (i.e. we start with at
                    //       least one, not zero).
                    //
                    StackFrame stackFrame = stackTrace.GetFrame(
                        skipFrames + 1);

                    //
                    // NOTE: Get the method for this stack frame.
                    //
                    return stackFrame.GetMethod();
                }
            }
            catch
            {
                // do nothing.
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Interpreter Helper Methods
        private static long GetComplaintId()
        {
            return GlobalState.NextComplaintId();
        }

        ///////////////////////////////////////////////////////////////////////

        private static ComplainCallback SafeGetComplainCallback(
            Interpreter interpreter /* NOT USED */
            )
        {
            bool locked = false;

            try
            {
                Interpreter.InternalTryStaticLock(ref locked);

                if (locked)
                    return Interpreter.ComplainCallback;
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                Interpreter.InternalExitStaticLock(ref locked);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SafeGetDefaultTraceStack(
            Interpreter interpreter /* NOT USED */
            )
        {
            bool locked = false;

            try
            {
                Interpreter.InternalTryStaticLock(ref locked);

                if (locked)
                    return Interpreter.DefaultTraceStack;
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                Interpreter.InternalExitStaticLock(ref locked);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SafeGetDefaultQuiet(
            Interpreter interpreter /* NOT USED */
            )
        {
            bool locked = false;

            try
            {
                Interpreter.InternalTryStaticLock(ref locked);

                if (locked)
                    return Interpreter.DefaultQuiet;
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                Interpreter.InternalExitStaticLock(ref locked);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SafeGetQuiet(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                        return interpreter.Quiet; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            //
            // HOOK: Allow the test suite (and others components) to override
            //       the quietness setting even if the interpreter is not
            //       available (or has already been disposed).
            //
            if (CommonOps.Environment.DoesVariableExist(EnvVars.Quiet))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SafeGetTraceToHost(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        return ScriptOps.HasFlags(interpreter,
                            InterpreterFlags.TraceToHost, true);
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(EnvVars.TraceToHost))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SafeGetComplainViaTrace(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        return ScriptOps.HasFlags(interpreter,
                            InterpreterFlags.ComplainViaTrace, true);
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.ComplainViaTrace))
            {
                return true;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SafeGetComplainViaTest(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        return ScriptOps.HasFlags(interpreter,
                            InterpreterFlags.ComplainViaTest, true);
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.ComplainViaTest))
            {
                return true;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SafeGetTraceStack(
            Interpreter interpreter,
            bool @default
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        return ScriptOps.HasFlags(interpreter,
                            InterpreterFlags.TraceStack, true);
                    }
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            if (CommonOps.Environment.DoesVariableExist(EnvVars.TraceStack))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        private static TextWriter SafeGetDebugTextWriter(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                        return interpreter.DebugTextWriter; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IDebugHost SafeGetHost(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                        return interpreter.Host; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string SafeGetComplaint(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.Complaint; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SafeSetComplaint(
            Interpreter interpreter,
            string complaint
            )
        {
            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                        interpreter.Complaint = complaint; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string SafeGetGlobalComplaint()
        {
            return Interlocked.CompareExchange(
                ref globalComplaint, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SafeSetGlobalComplaint(
            string complaint
            )
        {
            /* IGNORED */
            Interlocked.Exchange(ref globalComplaint, complaint);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Output Support Methods
#if NATIVE
        public static void Output(
            string message
            )
        {
            NativeOps.OutputDebugMessage(String.Format(
                "{0}{1}", message, Environment.NewLine));
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool IsUsableForComplainViaTest(
            Interpreter interpreter,
            bool ignoreLevels
            )
        {
            //
            // NOTE: The interpreter cannot be deleted or disposed.
            //
            if (!EntityOps.IsUsable(interpreter))
                return false;

            //
            // NOTE: Ignore the interpreter if its primary thread is not
            //       the current thread.  This helps to avoid deadlocks
            //       during the test suite in some situations.
            //
            if ((interpreter == null) || !interpreter.IsPrimarySystemThread())
                return false;

            //
            // NOTE: If the interpreter appears to be missing the needed
            //       command or channel, there isn't much point in trying
            //       to use it for Complain() output.
            //
            if (!TestOps.CanMaybeTryWriteViaPuts(interpreter))
                return false;

            //
            // NOTE: The interpreter cannot be in use by the script engine,
            //       the expression engine, or the script parser.  This is
            //       not a hard requirement; however, it's a failsafe that
            //       will hopefully prevented unwanted recursion back into
            //       the Complain() pipeline.  The caller can specify that
            //       these levels should be ignored.
            //
            if (!ignoreLevels &&
                ((interpreter == null) || interpreter.HasReadyLevels()))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Interpreter GetInterpreterForComplainViaTest(
            Interpreter interpreter
            )
        {
            if (!SkipCurrentForComplainViaTest &&
                IsUsableForComplainViaTest(interpreter, false))
            {
                return interpreter;
            }

            Interpreter localInterpreter = EntityOps.FollowTest(
                interpreter, true);

            if (IsUsableForComplainViaTest(localInterpreter, false))
                return localInterpreter;

            localInterpreter = EntityOps.FollowMaster(interpreter, true);

            if (IsUsableForComplainViaTest(localInterpreter, false))
                return localInterpreter;

            localInterpreter = GlobalState.GetFirstInterpreter();

            if (IsUsableForComplainViaTest(localInterpreter, true))
                return localInterpreter;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ComplainViaTest(
            Interpreter interpreter,
            long id,
            string value
            )
        {
            try
            {
                return TestOps.TryWriteViaPuts(
                    GetInterpreterForComplainViaTest(interpreter),
                    String.Format("{0}{1}", value, Environment.NewLine),
                    IgnoreQuietForComplainViaTest, /* noComplain */ true);
            }
            catch (Exception e)
            {
                TestWriteException(id, e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void WriteViaDebugAndOrTrace(
            string value
            )
        {
#if DEBUG
            //
            // BUGFIX: Use a try/catch here to prevent exceptions thrown
            //         by Debug.WriteLine from ever escaping this method.
            //
            try
            {
                DebugWriteLine(value, null); /* throw */
            }
            catch
            {
                // do nothing.
            }
#endif

#if TRACE
            //
            // BUGFIX: Use a try/catch here to prevent exceptions thrown
            //         by Trace.WriteLine from ever escaping this method.
            //
            try
            {
                TraceWriteLine(value, null); /* throw */
            }
            catch
            {
                // do nothing.
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteWithoutFail(
            string value
            )
        {
            WriteWithoutFail(value, Build.Debug, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void WriteWithoutFail(
            string value,
            bool viaOutput,
            bool viaTrace
            )
        {
#if NATIVE
            if (viaOutput)
                Output(value);
#endif

            if (viaTrace)
                WriteViaDebugAndOrTrace(value);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void TextWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                TextWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void HostWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                HostWriteExceptionFormat, id, e, Environment.NewLine));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void TestWriteException(
            long id,
            Exception e
            )
        {
            WriteWithoutFail(String.Format(
                TestWriteExceptionFormat, id, e, Environment.NewLine));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Complaint Reporting Methods
        public static bool GetDefaultQuiet(
            bool @default
            )
        {
            //
            // HOOK: Allow the test suite (and others components) to override
            //       the quietness setting during interpreter creation and to
            //       be able to specify the default fallback value.
            //
            if (CommonOps.Environment.DoesVariableExist(EnvVars.DefaultQuiet))
                return true;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetDefaultTraceStack(
            bool @default
            )
        {
            //
            // HOOK: Allow the test suite (and others components) to override
            //       the stack trace setting during interpreter creation and
            //       to be able to specify the default fallback value.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.DefaultTraceStack))
            {
                return true;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GetComplainCounts(
            bool thread,          /* in */
            bool global,          /* in */
            ref long threadCount, /* out */
            ref long globalCount  /* out */
            )
        {
            if (thread)
            {
                threadCount = Interlocked.CompareExchange(
                    ref complainCount, 0, 0);
            }

            if (global)
            {
                globalCount = Interlocked.CompareExchange(
                    ref globalComplainCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsComplainPending()
        {
            return Interlocked.CompareExchange(ref complainLevels, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method must *NOT* throw any exceptions.
        //
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Complain(
            ReturnCode code,
            Result result
            )
        {
            if (!IsComplainPossible())
                return;

            Complain(Interpreter.GetActive(), code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method must *NOT* throw any exceptions.
        //
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Complain(
            Interpreter interpreter,
            ReturnCode code,
            Result result
            )
        {
            if (!IsComplainPossible())
                return;

            ComplainCallback callback = SafeGetComplainCallback(interpreter);

            long id = GetComplaintId();

            bool stack = SafeGetTraceStack(interpreter,
                GetDefaultTraceStack(SafeGetDefaultTraceStack(interpreter)));

            string stackTrace = stack ? GetStackTraceString() : null;

            bool viaTrace = SafeGetComplainViaTrace(interpreter, false);
            bool viaTest = SafeGetComplainViaTest(interpreter, false);

            bool quiet = SafeGetQuiet(interpreter,
                GetDefaultQuiet(SafeGetDefaultQuiet(interpreter)));

            Complain(
                callback, interpreter, SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), id, code, result, stackTrace,
                viaTrace, viaTest, quiet);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Complaint Reporting Methods
        private static bool IsComplainPossible()
        {
            return !AppDomainOps.IsCurrentFinalizing();
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsHostUsable(
            IDebugHost debugHost,
            HostFlags hasFlags
            ) /* throw */
        {
            if (debugHost == null)
                return false;

            //
            // NOTE: Grab the flags for this debug host.
            //
            HostFlags flags = debugHost.GetHostFlags(); /* throw */

            //
            // NOTE: The debug host is not usable if it failed a call to read
            //       or write with an exception.
            //
            if (FlagOps.HasFlags(flags, HostFlags.ExceptionMask, false))
                return false;

            //
            // NOTE: The debug host is not usable if it does not support the
            //       selected features.
            //
            if (!FlagOps.HasFlags(flags, hasFlags, true))
                return false;

            //
            // HACK: Currently, all debug host method calls within this class
            //       are write operations; therefore, if the host is not open
            //       it cannot be used.
            //
            if (!debugHost.IsOpen())
                return false;

            //
            // NOTE: If we get to this point, the debug host should be usable
            //       for the selected features (e.g. writing a complaint to
            //       the console).
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Complain(
            ComplainCallback callback,
            Interpreter interpreter,
            TextWriter textWriter,
            IDebugHost debugHost,
            long id,
            ReturnCode code,
            Result result,
            string stackTrace,
            bool viaTrace,
            bool viaTest,
            bool quiet // NOTE: Inhibit use of IDebugHost and the Console?
            )
        {
            /* IGNORED */
            Interlocked.Increment(ref complainCount);

            /* IGNORED */
            Interlocked.Increment(ref globalComplainCount);

            int retry = 0;

        retryLevels:

            int levels = Interlocked.Increment(ref complainLevels);

            try
            {
                if (callback != null)
                {
                    //
                    // NOTE: Invoke the callback now.  If this ends up throwing
                    //       an exception, it will be caught by this method and
                    //       the remaining complaint handling will be skipped.
                    //
                    callback(interpreter, id, code, result, stackTrace, quiet,
                        retry, levels); /* throw */
                }

                if (levels == 1)
                {
                    string formatted = FormatOps.Complaint(
                        id, code, result, stackTrace);

                    SafeSetComplaint(interpreter, formatted);
                    SafeSetGlobalComplaint(formatted);

                    WriteWithoutFail(formatted, true, viaTrace);

                    if (viaTest)
                    {
                        /* IGNORED */
                        ComplainViaTest(interpreter, id, formatted);
                    }

                    if (!quiet)
                    {
                        if (textWriter != null)
                        {
                            try
                            {
                                textWriter.WriteLine(formatted); /* throw */
                                textWriter.Flush(); /* throw */
                            }
                            catch (Exception e)
                            {
                                TextWriteException(id, e);
                            }
                        }

                    retryHost:

                        if (debugHost != null)
                        {
                            //
                            // BUGFIX: The host may have been disposed at this
                            //         point and we do NOT want to throw an
                            //         exception; therefore, wrap the host
                            //         access in a try block.  If the host does
                            //         throw an exception for any reason, we
                            //         will simply null out the host and retry
                            //         using our default handling.
                            //
                            try
                            {
                                if (IsHostUsable(
                                        debugHost, HostFlags.Complain))
                                {
                                    debugHost.WriteErrorLine(
                                        formatted); /* throw */
                                }
                            }
                            catch (Exception e)
                            {
                                HostWriteException(id, e);

                                debugHost = null;

                                goto retryHost;
                            }
                        }
#if WINFORMS
                        else
                        {
                            WindowOps.Complain(formatted);
                        }
#elif CONSOLE
                        else
                        {
                            try
                            {
                                TextWriter localTextWriter = Console.Error;

                                if (localTextWriter == null)
                                    localTextWriter = Console.Out;

                                if (localTextWriter != null)
                                {
                                    localTextWriter.WriteLine(
                                        formatted); /* throw */

                                    localTextWriter.Flush(); /* throw */
                                }
                            }
                            catch (Exception e)
                            {
                                TextWriteException(id, e);
                            }
                        }
#endif
                    }
                }
                else
                {
                    //
                    // NOTE: Have we reached the limit on the number of times
                    //       we should retry the complaint?
                    //
                    if (Interlocked.Increment(ref retry) < ComplainRetryLimit)
                    {
                        //
                        // NOTE: *IMPORTANT* The second parameter (noComplain)
                        //       must be true here to avoid possible infinite
                        //       recursion.
                        //
                        HostOps.ThreadSleepOrMaybeComplain(
                            ComplainRetryMilliseconds, true);

                        //
                        // NOTE: After waiting a bit, try again to escape the
                        //       nested complaint level (i.e. one from another
                        //       thread).
                        //
                        goto retryLevels;
                    }

                    //
                    // NOTE: This method has been called recursively -AND- we
                    //       are out of retries.  That is not a good sign.
                    //       Allow the attached debugger to see this.
                    //
                    MaybeBreak();
                }
            }
            catch
            {
                //
                // NOTE: If there is a valid callback, we might want to do
                //       nothing, as it may have simple wanted to abort the
                //       complaint processing; however, if necessary, reset
                //       the callback to null and retry.
                //
                if (callback == null)
                {
                    throw;
                }
                else if (!IgnoreOnCallbackThrow)
                {
                    //
                    // HACK: Change the callback to null (only locally) and
                    //       then try to handle this complaint using only
                    //       the default handling.  This code may look bad;
                    //       however, apparently, jumping out of the middle
                    //       of a catch block is perfectly fine and still
                    //       executes the finally block correctly.
                    //
                    callback = null;
                    goto retryLevels;
                }
                else
                {
                    //
                    // NOTE: Really do nothing.  There is a valid callback
                    //       and the "ignoreOnCallbackThrow" flag is set.
                    //
                }
            }
            finally
            {
                Interlocked.Decrement(ref complainLevels);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Host Output Methods
        public static void WriteTo(
            Interpreter interpreter,
            string value,
            bool force
            )
        {
            WriteTo(SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), value, force);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Host Output Methods
        private static void WriteTo(
            TextWriter textWriter,
            IDebugHost debugHost,
            string value,
            bool force
            )
        {
#if DEBUG
            if (textWriter != null)
#else
            if (force && (textWriter != null))
#endif
            {
                try
                {
                    textWriter.WriteLine(value); /* throw */
                    textWriter.Flush(); /* throw */
                }
                catch (Exception e)
                {
                    TextWriteException(0, e);
                }
            }

#if DEBUG
            if (debugHost != null)
#else
            if (force && (debugHost != null))
#endif
            {
                //
                // BUGFIX: The host may have been disposed at this point and we
                //         do NOT want to throw an exception; therefore, wrap
                //         the host access in a try block.
                //
                try
                {
                    if (IsHostUsable(
                            debugHost, HostFlags.Debug))
                    {
                        debugHost.WriteDebugLine(value);
                    }
                }
                catch (Exception e)
                {
                    HostWriteException(0, e);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Debug "Break" Methods
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Break(
            Interpreter interpreter,
            MethodBase skipMethod,
            bool force
            )
        {
            Break(SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), skipMethod, force);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Break(
            TextWriter textWriter,
            IDebugHost debugHost,
            MethodBase skipMethod,
            bool force
            )
        {
            ComplainCallback callback = SafeGetComplainCallback(null);

            Result result = FormatOps.BreakOrFail(
                GetMethodName(skipMethod, null), "debug break invoked");

            //
            // NOTE: There is no need for a full stack trace here.
            //
            Complain(
                callback, null, textWriter, debugHost, GetComplaintId(),
                ReturnCode.Error, result, null, true, false, false);

#if !DEBUG
            if (force)
#endif
                Break();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Debug "Fail" Methods
        #region Dead Code
#if DEAD_CODE
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fail(
            Interpreter interpreter,
            MethodBase skipMethod,
            string message,
            string detailMessage,
            bool force
            )
        {
            Fail(SafeGetDebugTextWriter(interpreter),
                SafeGetHost(interpreter), skipMethod,
                message, detailMessage, force);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fail(
            TextWriter textWriter,
            IDebugHost debugHost,
            MethodBase skipMethod,
            string message,
            string detailMessage,
            bool force
            )
        {
            ComplainCallback callback = SafeGetComplainCallback(null);

            Result result = FormatOps.BreakOrFail(
                GetMethodName(skipMethod, null), "debug fail invoked",
                message, detailMessage);

            Complain(
                callback, null, textWriter, debugHost, GetComplaintId(),
                ReturnCode.Error, result, GetStackTraceString(), true,
                false, false);

#if !DEBUG
            if (force)
#endif
                Fail(message, detailMessage);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Listener Handling Methods
        public static TraceListener NewDefaultTraceListener(
            bool console
            )
        {
#if CONSOLE
            return console ? (TraceListener)
                new ConsoleTraceListener() :
                new DefaultTraceListener();
#else
            return new DefaultTraceListener();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsSameTraceListener(
            TraceListener listener1,
            TraceListener listener2,
            bool typeOnly
            )
        {
            //
            // NOTE: If either trace listener is null, both must
            //       be null for this method to return true.
            //
            if ((listener1 == null) || (listener2 == null))
                return (listener1 == null) && (listener2 == null);

            //
            // NOTE: First, compare the types.  If they are not a
            //       match, we are done.  If they are a match, we
            //       might be done.
            //
            Type type1 = listener1.GetType();
            Type type2 = listener2.GetType();

            if (!Object.ReferenceEquals(type1, type2))
                return false;

            //
            // NOTE: At least one listener of this type is present
            //       in the list.  If the caller only cares about
            //       type, just return now.
            //
            if (typeOnly)
                return true;

            //
            // NOTE: If these trace listener are the same object,
            //       return true; otherwise, return false.
            //
            return Object.ReferenceEquals(listener1, listener2);
        }

        ///////////////////////////////////////////////////////////////////////

        private static int FindTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            bool typeOnly
            )
        {
            if (listeners != null)
            {
                if (listener != null)
                {
                    int count = listeners.Count;
                    Type type = listener.GetType();

                    for (int index = 0; index < count; index++)
                    {
                        TraceListener localListener = listeners[index];

                        if (localListener == null)
                            continue;

                        if (IsSameTraceListener(
                                localListener, listener, typeOnly))
                        {
                            return index;
                        }
                    }
                }
            }

            return Index.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode EnsureTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            ref Result error
            )
        {
            return EnsureTraceListener(listeners, listener, true, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode EnsureTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            bool typeOnly,
            ref Result error
            )
        {
            if (listeners != null)
            {
                if (listener != null)
                {
                    //
                    // NOTE: We succeeded.  At least one listener of this
                    //       type is already present in the list.
                    //
                    if (FindTraceListener(
                            listeners, listener, typeOnly) != Index.Invalid)
                    {
                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: No listeners of this type are present in the
                    //       list, add one now (i.e. the one provided by
                    //       the caller).
                    //
                    /* IGNORED */
                    listeners.Add(listener);

                    //
                    // NOTE: We succeeded (the listener has been added).
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid trace listener";
                }
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ReplaceTraceListener(
            TraceListenerCollection listeners,
            TraceListener oldListener,
            TraceListener newListener,
            bool typeOnly,
            bool dispose,
            ref Result error
            )
        {
            if (listeners == null)
            {
                error = "invalid trace listener collection";
                return ReturnCode.Error;
            }

            if (oldListener != null)
            {
                int index = FindTraceListener(
                    listeners, oldListener, typeOnly);

                if (index != Index.Invalid)
                {
                    /* NO RESULT */
                    listeners.RemoveAt(index);
                }

                if (dispose)
                    oldListener.Dispose(); /* throw */
            }

            if (newListener != null)
            {
                /* IGNORED */
                listeners.Add(newListener);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ClearTraceListeners(
            bool trace,
            bool debug,
            bool console,
            bool verbose
            )
        {
            Result error = null;

            return ClearTraceListeners(
                trace, debug, console, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ClearTraceListeners(
            bool trace,
            bool debug,
            bool console,
            bool verbose,
            ref Result error
            )
        {
            try
            {
                int count = (trace ? 1 : 0) + (debug ? 1 : 0);

                //
                // NOTE: Do they want to clear normal trace listeners?
                //
                if (trace)
                {
                    if (ClearTraceListeners(
                            GetTraceListeners(), false, console,
                            verbose, ref error) == ReturnCode.Ok)
                    {
                        count--;
                    }
                }

                //
                // NOTE: Do they want to clear debug trace listeners
                //       as well?
                //
                if (debug)
                {
                    if (ClearTraceListeners(
                            GetDebugListeners(), true, console,
                            verbose, ref error) == ReturnCode.Ok)
                    {
                        count--;
                    }
                }

                if (count == 0)
                    return ReturnCode.Ok;
                else
                    error = "one or more trace listeners could not be cleared";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ClearTraceListeners(
            TraceListenerCollection listeners,
            bool debug,
            bool console,
            bool verbose,
            ref Result error
            )
        {
            if (listeners != null)
            {
                /* NO RESULT */
                listeners.Clear();

#if CONSOLE
                if (console && verbose)
                {
                    ConsoleOps.WritePrompt(debug ?
                        _Constants.Prompt.NoDebugTrace :
                        _Constants.Prompt.NoTrace);
                }
#endif

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            Result error = null;

            return AddTraceListener(listener, debug, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug,
            ref Result error
            )
        {
            TraceListenerCollection listeners = debug ?
                GetDebugListeners() : GetTraceListeners();

            return EnsureTraceListener(listeners, listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            Result error = null;

            return RemoveTraceListener(listener, debug, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug,
            ref Result error
            )
        {
            TraceListenerCollection listeners = debug ?
                GetDebugListeners() : GetTraceListeners();

            return RemoveTraceListener(listeners, listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode RemoveTraceListener(
            TraceListenerCollection listeners,
            TraceListener listener,
            ref Result error
            )
        {
            if (listeners != null)
            {
                if (listener != null)
                {
                    /* NO RESULT */
                    listeners.Remove(listener);

                    //
                    // NOTE: We succeeded (the listener has been removed)?
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid trace listener";
                }
            }
            else
            {
                error = "invalid trace listener collection";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetupTraceListeners(
            bool trace,
            bool debug,
            bool console,
            bool verbose
            )
        {
            Result error = null;

            return SetupTraceListeners(
                trace, debug, console, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SetupTraceListeners(
            bool trace,
            bool debug,
            bool console,
            bool verbose,
            ref Result error
            )
        {
            try
            {
                int count = (trace ? 1 : 0) + (debug ? 1 : 0);

                TraceListener listener = (trace || debug) ?
                    NewDefaultTraceListener(console) : null;

                //
                // NOTE: Do they want to add a normal trace listener?
                //
                if (trace)
                {
                    Result localError = null;

                    if (AddTraceListener(listener, false,
                            ref localError) == ReturnCode.Ok)
                    {
                        count--;

#if CONSOLE
                        if (console && verbose)
                        {
                            ConsoleOps.WritePrompt(
                                _Constants.Prompt.Trace);
                        }
#endif
                    }
#if CONSOLE
                    else
                    {
                        //
                        // TODO: Can this actually happen?
                        //
                        if (console && verbose)
                        {
                            ConsoleOps.WritePrompt(String.Format(
                                _Constants.Prompt.TraceError,
                                localError));
                        }
                    }
#endif
                }

                //
                // NOTE: Do they want to add a debug trace listener as well?
                //
                if (debug)
                {
                    Result localError = null;

                    if (AddTraceListener(listener, true,
                            ref localError) == ReturnCode.Ok)
                    {
                        count--;

#if CONSOLE
                        if (console && verbose)
                        {
                            ConsoleOps.WritePrompt(
                                _Constants.Prompt.DebugTrace);
                        }
#endif
                    }
#if CONSOLE
                    else
                    {
                        //
                        // TODO: Can this actually happen?
                        //
                        if (console && verbose)
                        {
                            ConsoleOps.WritePrompt(String.Format(
                                _Constants.Prompt.DebugTraceError,
                                localError));
                        }
                    }
#endif
                }

                if (count == 0)
                    return ReturnCode.Ok;
                else
                    error = "one or more trace listeners could not be added";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Framework Wrapper Methods
        public static void Break()
        {
            System.Diagnostics.Debugger.Break();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAttached()
        {
            return System.Diagnostics.Debugger.IsAttached;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Log(
            string message
            )
        {
            System.Diagnostics.Debugger.Log(0, DefaultCategory, message);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Fail(string message, string detailMessage)
        {
            Debug.Fail(message, detailMessage);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TraceListenerCollection GetDebugListeners()
        {
            return Debug.Listeners;
        }

        ///////////////////////////////////////////////////////////////////////

        public static TraceListenerCollection GetTraceListeners()
        {
            return Trace.Listeners;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static void Log(
            int level,
            string message
            )
        {
            System.Diagnostics.Debugger.Log(level, DefaultCategory, message);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static void Log(
            int level,
            string category,
            string message
            )
        {
            System.Diagnostics.Debugger.Log(level, category, message);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWrite(
            object value
            )
        {
            Debug.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWrite(
            string message
            )
        {
            Debug.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWrite(
            string message,
            string category
            )
        {
            if (category != null)
                Debug.Write(message, category); /* throw */
            else
                Debug.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWriteLine(
            object value
            )
        {
            Debug.WriteLine(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DebugWriteLine(
            string message
            )
        {
            Debug.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DebugWriteLine(
            string message,
            string category
            )
        {
            if (category != null)
                Debug.WriteLine(message, category); /* throw */
            else
                Debug.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static void DebugFlush()
        {
            Debug.Flush();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWrite(
            object value
            )
        {
            Trace.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWrite(
            string message
            )
        {
            Trace.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWrite(
            string message,
            string category
            )
        {
            if (category != null)
                Trace.Write(message, category); /* throw */
            else
                Trace.Write(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWriteLine(
            object value
            )
        {
            Trace.WriteLine(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWriteLine(
            string message
            )
        {
            Trace.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWriteLine(
            string message,
            string category
            )
        {
            if (category != null)
                Trace.WriteLine(message, category); /* throw */
            else
                Trace.WriteLine(message); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceWriteLineFormatted(
            string message,
            string category
            )
        {
            string formatted = String.Format(
                "{0}: {1}", GlobalState.GetCurrentSystemThreadId(),
                message);

            TraceWriteLine(formatted, category);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TraceFlush()
        {
            Trace.Flush();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Break-Into-Debugger Methods
        public static void MaybeBreak()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeBreak(
            string message
            )
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Log(0, DefaultCategory, message);
                System.Diagnostics.Debugger.Break();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Miscellaneous Debugging Methods
        public static void DumpAppDomain(
            AppDomain appDomain
            )
        {
            if (appDomain != null)
            {
                TraceWriteLineFormatted(String.Format(
                    "DumpAppDomain: Id = {0}, FriendlyName = {1}, " +
                    "BaseDirectory = {2}, RelativeSearchPath = {3}, " +
                    "DynamicDirectory = {4}, ShadowCopyFiles = {5}",
                    AppDomainOps.GetId(appDomain),
                    FormatOps.WrapOrNull(appDomain.FriendlyName),
                    FormatOps.WrapOrNull(appDomain.BaseDirectory),
                    FormatOps.WrapOrNull(appDomain.RelativeSearchPath),
                    FormatOps.WrapOrNull(appDomain.DynamicDirectory),
                    appDomain.ShadowCopyFiles),
                    typeof(DebugOps).Name);

                foreach (Assembly assembly in appDomain.GetAssemblies())
                {
                    string name = null;
                    string location = null;

                    if (assembly != null)
                    {
                        AssemblyName assemblyName = assembly.GetName();

                        if (assemblyName != null)
                            name = assemblyName.ToString();

                        try
                        {
                            location = assembly.Location;
                        }
                        catch (NotSupportedException)
                        {
                            // do nothing.
                        }
                    }

                    TraceWriteLineFormatted(String.Format(
                        "DumpAppDomain: assemblyName = {0}, " +
                        "location = {1}", FormatOps.WrapOrNull(name),
                        FormatOps.WrapOrNull(location)),
                        typeof(DebugOps).Name);
                }
            }
            else
            {
                TraceWriteLineFormatted(
                    "DumpAppDomain: invalid application domain",
                    typeof(DebugOps).Name);
            }
        }
        #endregion
    }
}
