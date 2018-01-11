/*
 * ScriptThread.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("f3bd8b05-282c-4ec8-8c46-c02790fbbb7d")]
    // [ObjectFlags(ObjectFlags.AutoDispose)]
    public sealed class ScriptThread : IScriptThread, IDisposable
    {
        #region Event Input Pair Class (Input-Only)
        [ObjectId("72a69e6c-515a-4567-9d07-874e05b2cf6b")]
        private sealed class EventInputPair :
            AnyPair<string, string>
        {
            public EventInputPair(
                string x,
                string y
                )
                : base(x, y)
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Output Pair Class (Input-Output)
        [ObjectId("25757691-b599-40c1-a485-90b1a3ab87a7")]
        private sealed class EventOutputPair :
            MutableAnyPair<ReturnCode, Result>
        {
            public EventOutputPair(
                bool mutable
                )
                : base(mutable)
            {
                // do nothing.
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        private const string scriptThreadObjectName = "thread";

        private const string scriptThreadSendEventPrefix = "threadSend";

        private static readonly ObjectOptionType DefaultObjectOptionType =
            ObjectOps.GetDefaultObjectOptionType();

        private static readonly ObjectFlags DefaultObjectFlags =
            ObjectOps.GetDefaultObjectFlags();

        private static readonly ThreadFlags DefaultThreadFlags =
            ThreadFlags.Default;

        private static readonly CreateFlags DefaultCreateFlags =
            CreateFlags.ScriptThreadUse;

        private static readonly InitializeFlags DefaultInitializeFlags =
            InitializeFlags.Default;

        private static readonly ScriptFlags DefaultScriptFlags =
            ScriptFlags.Default;

        private static readonly InterpreterFlags DefaultInterpreterFlags =
            InterpreterFlags.Default;

        private static readonly int DefaultStackSize = 0;

        private static readonly int DefaultTimeout = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static int activeCount = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private object syncRoot = new object();
        private EventWaitHandle startEvent;
        private EventWaitHandle wakeUpEvent;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private ScriptThread()
        {
            id = GlobalState.NextScriptThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptThread(
            string name,
            ThreadFlags threadFlags,
            CreateFlags createFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            EventWaitFlags eventWaitFlags,
            IEnumerable<string> args,
            IHost host,
            IScript script,
            string varName,
            int maxStackSize,
            int timeout,
            bool userInterface,
            bool isBackground,
            bool quiet,
            bool noBackgroundError,
            bool useSelf,
            bool noComplain,
            bool verbose,
            bool debug,
            bool usePool
            )
            : this()
        {
            this.name = name;
            this.threadFlags = threadFlags;
            this.createFlags = createFlags;
            this.initializeFlags = initializeFlags;
            this.scriptFlags = scriptFlags;
            this.interpreterFlags = interpreterFlags;
            this.args = args;
            this.host = host;
            this.script = script;
            this.varName = varName;
            this.maxStackSize = maxStackSize;
            this.timeout = timeout;
            this.userInterface = userInterface;
            this.isBackground = isBackground;
            this.quiet = quiet;
            this.noBackgroundError = noBackgroundError;
            this.useSelf = useSelf;
            this.eventWaitFlags = eventWaitFlags;
            this.noComplain = noComplain;
            this.verbose = verbose;
            this.debug = debug;
            this.usePool = usePool;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static IScriptThread Create(
            string varName,
            ref Result error
            )
        {
            //
            // NOTE: Create a ScriptThread object using the default stack size
            //       for the process.
            //
            return Create(varName, DefaultStackSize, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScriptThread Create(
            string varName,
            int maxStackSize,
            ref Result error
            )
        {
            return Create(
                null, null, null, null, null, null, varName, maxStackSize,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScriptThread Create(
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Create(
                null, null, null, null, null, null, varName, maxStackSize,
                timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScriptThread Create(
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            string varName,
            int maxStackSize,
            ref Result error
            )
        {
            return Create(
                name, threadFlags, createFlags, initializeFlags, scriptFlags,
                interpreterFlags, varName, maxStackSize, DefaultTimeout,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScriptThread Create(
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            return Create(
                name, threadFlags, createFlags, initializeFlags, scriptFlags,
                interpreterFlags, null, null, null, varName, maxStackSize,
                timeout, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScriptThread Create(
            string name,
            ThreadFlags? threadFlags,
            CreateFlags? createFlags,
            InitializeFlags? initializeFlags,
            ScriptFlags? scriptFlags,
            InterpreterFlags? interpreterFlags,
            IEnumerable<string> args,
            IHost host,
            IScript script,
            string varName,
            int maxStackSize,
            int timeout,
            ref Result error
            )
        {
            try
            {
                //
                // NOTE: *WARNING* If the "usePool" option is specified, the
                //       "maxStackSize", "userInterface", "isBackground", and
                //       "start" parameters are effectively ignored.
                //
                bool throwOnDisposed;
                bool safe;
                bool userInterface;
                bool isBackground;
                bool quiet;
                bool noBackgroundError;
                bool useSelf;
                bool noComplain;
                bool verbose;
                bool debug;
                bool usePool;
                bool start;
                bool noAbort;

                ThreadFlags localThreadFlags = GetThreadFlags(
                    threadFlags, out throwOnDisposed, out safe,
                    out userInterface, out isBackground, out quiet,
                    out noBackgroundError, out useSelf, out noComplain,
                    out verbose, out debug, out usePool, out start,
                    out noAbort);

                CreateFlags localCreateFlags = GetCreateFlags(
                    createFlags, throwOnDisposed, safe);

                InitializeFlags localInitializeFlags = GetInitializeFlags(
                    initializeFlags);

                ScriptFlags localScriptFlags = GetScriptFlags(scriptFlags);

                InterpreterFlags localInterpreterFlags = GetInterpreterFlags(
                    interpreterFlags);

                EventWaitFlags localEventWaitFlags = GetEventWaitFlags(
                    null, localThreadFlags);

                if (timeout == DefaultTimeout)
                    timeout = ThreadOps.DefaultJoinTimeout;

                ScriptThread scriptThread = new ScriptThread(
                    name, localThreadFlags, localCreateFlags,
                    localInitializeFlags, localScriptFlags,
                    localInterpreterFlags, localEventWaitFlags,
                    args, host, script, varName, maxStackSize,
                    timeout, userInterface, isBackground, quiet,
                    noBackgroundError, useSelf, noComplain,
                    verbose, debug, usePool);

                EventWaitHandle startEvent = scriptThread.startEvent =
                    ThreadOps.CreateEvent(false);

                scriptThread.wakeUpEvent = ThreadOps.CreateEvent(true);

                //
                // NOTE: Grab the timeout value from the created script thread,
                //       in case it was changed during the creation process.
                //       When this was written (2013-12-18), it was impossible
                //       for this value to change; however, this may change in
                //       the future.
                //
                timeout = scriptThread.PrivateTimeout;

                //
                // NOTE: Determine if an actual, physical thread needs to be
                //       created now (i.e. otherwise, the thread pool will be
                //       used).
                //
                if (usePool)
                {
                    ReturnCode code = ReturnCode.Ok;

                    try
                    {
                        if (Engine.QueueWorkItem(scriptThread.ThreadStart, null))
                        {
                            if (ThreadOps.WaitEvent(startEvent, timeout))
                            {
                                return scriptThread;
                            }
                            else
                            {
                                /* NO RESULT */
                                scriptThread.PrivateSignalAndSleep(timeout);

                                error = String.Format(
                                    "script thread startup timeout of {0} milliseconds",
                                    timeout);

                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "could not queue work item";
                            code = ReturnCode.Error;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                    finally
                    {
                        if ((code != ReturnCode.Ok) &&
                            (scriptThread != null))
                        {
                            try
                            {
                                scriptThread.Dispose(); /* throw */
                                scriptThread = null;
                            }
                            catch (Exception e)
                            {
                                PrivateComplain(noComplain, String.Format(
                                    "could not dispose script thread: {0}", e));
                            }
                        }
                    }
                }
                else
                {
                    Thread thread = scriptThread.thread = Engine.CreateThread(
                        scriptThread.ThreadStart, maxStackSize, userInterface,
                        isBackground);

                    if (thread != null)
                    {
                        thread.Name = GetThreadName(scriptThread);

                        if (start)
                        {
                            ReturnCode code = ReturnCode.Ok;

                            try
                            {
                                thread.Start();

                                if (ThreadOps.WaitEvent(startEvent, timeout))
                                {
                                    return scriptThread;
                                }
                                else
                                {
                                    /* NO RESULT */
                                    scriptThread.PrivateSignalAndSleep(timeout);

                                    /* NO RESULT */
                                    InterruptOrAbortThread(
                                        thread, timeout, verbose, noAbort);

                                    error = String.Format(
                                        "script thread startup timeout of {0} milliseconds",
                                        timeout);

                                    code = ReturnCode.Error;
                                }
                            }
                            catch (Exception e)
                            {
                                error = e;
                                code = ReturnCode.Error;
                            }
                            finally
                            {
                                if ((code != ReturnCode.Ok) &&
                                    (scriptThread != null))
                                {
                                    try
                                    {
                                        scriptThread.Dispose(); /* throw */
                                        scriptThread = null;
                                    }
                                    catch (Exception e)
                                    {
                                        PrivateComplain(noComplain, String.Format(
                                            "could not dispose script thread: {0}", e));
                                    }
                                }
                            }
                        }
                        else
                        {
                            return scriptThread;
                        }
                    }
                    else
                    {
                        error = "could not create script thread";
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScriptThread Members
        #region Public Properties
        #region Owned Resource Properties
        private Thread thread;
        public Thread Thread
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return thread;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return interpreter;
                }
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Creation & Startup Properties
        private long id;
        public long Id
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateId;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This defines an instance property in order to read a static
        //       field.
        //
        public int ActiveCount
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return Interlocked.CompareExchange(ref activeCount, 0, 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private IEnumerable<string> args;
        public IEnumerable<string> Args
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return args;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHost host;
        public IHost Host
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return host;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ThreadFlags threadFlags;
        public ThreadFlags ThreadFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return threadFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private CreateFlags createFlags;
        public CreateFlags CreateFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return createFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private InitializeFlags initializeFlags;
        public InitializeFlags InitializeFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return initializeFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptFlags scriptFlags;
        public ScriptFlags ScriptFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return scriptFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private InterpreterFlags interpreterFlags;
        public InterpreterFlags InterpreterFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return interpreterFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool quiet;
        public bool Quiet
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return quiet;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noBackgroundError;
        public bool NoBackgroundError
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return noBackgroundError;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private IScript script;
        public IScript Script // NOTE: For no startup script, use null.
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return script;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private int timeout;
        public int Timeout
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateTimeout;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Processing Properties (part 1)
        private string varName;
        public string VarName // NOTE: For no wait, use null.
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return varName;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Thread Creation Properties
        private int maxStackSize;
        public int MaxStackSize
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return maxStackSize;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool userInterface;
        public bool UserInterface
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return userInterface;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool isBackground;
        public bool IsBackground
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return isBackground;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Processing Properties (part 2)
        private bool useSelf;
        public bool UseSelf
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return useSelf;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventWaitFlags eventWaitFlags;
        public EventWaitFlags EventWaitFlags
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return eventWaitFlags;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noComplain;
        public bool NoComplain
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return noComplain;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool verbose;
        public bool Verbose
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return verbose;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    verbose = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool debug;
        public bool Debug
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return debug;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    debug = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool usePool;
        public bool UsePool
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return usePool;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    usePool = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode returnCode;
        public ReturnCode ReturnCode
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return returnCode;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    returnCode = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private Result result;
        public Result Result
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    return result;
                }
            }
            set
            {
                CheckDisposed();
                CheckRestricted();

                lock (syncRoot)
                {
                    result = value;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Thread State Properties
        public bool IsAlive
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateIsAlive;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsBusy
        {
            get
            {
                CheckDisposed();
                CheckRestricted();

                return PrivateIsBusy;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsDisposed
        {
            get
            {
                // CheckDisposed(); /* EXEMPT */
                // CheckRestricted(); /* EXEMPT */

                return PrivateIsDisposed;
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Thread State Methods
        public bool Start()
        {
            CheckDisposed();
            CheckRestricted();

            try
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                }

                if (thread == null)
                    return false;
                else if (thread.IsAlive)
                    return true;

                thread.Start(); /* throw */
                return true;
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not start thread: {0}", e));
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Stop()
        {
            CheckDisposed();
            CheckRestricted();

            return Stop(false);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Stop(
            bool force
            )
        {
            CheckDisposed();
            CheckRestricted();

            bool noAbort = PrivateIsNoAbort(); /* REDUNDANT */

            try
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                    noAbort = PrivateIsNoAbort(); /* REFRESH */
                }

                if (thread == null)
                    return false;
                else if (!thread.IsAlive)
                    return true;

                //
                // NOTE: If the NoAbort thread flag is set, we NEVER
                //       call the Abort() method.
                //
                if (force && !noAbort)
                    thread.Abort(); /* BUGBUG: Leaks? */
                else
                    thread.Interrupt(); /* throw */

                return true;
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not {0} thread: {1}", force && !noAbort ?
                    "abort" : "interrupt", e));
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region CLR Object Integration Methods
        public ReturnCode AddObject(
            object value
            )
        {
            CheckDisposed();
            CheckRestricted();

            ReturnCode code;
            Result result = null;

            code = AddObject(value, ref result);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, result);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode AddObject(
            object value,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(value, true, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode AddObject(
            object value,
            bool alias,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                DefaultObjectOptionType, value, alias, false, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            object value,
            bool alias,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                objectOptionType, DefaultObjectFlags, value, alias, false,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                objectOptionType, DefaultObjectFlags, value, alias,
                aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return AddObject(
                objectOptionType, null, objectFlags, value, alias,
                aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode AddObject(
            ObjectOptionType objectOptionType,
            string objectName,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            CheckDisposed();
            CheckRestricted();

            return FixupReturnValue(
                null, objectOptionType, objectName, objectFlags, value, alias,
                aliasReference, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Synchronous Wait Methods
        public bool WaitForStart()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForStart(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForStart(
            int milliseconds
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForStart(milliseconds, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForStart(
            int milliseconds,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForStart(milliseconds, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEnd()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEnd(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEnd(
            int milliseconds
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEnd(milliseconds, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEnd(
            int milliseconds,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForEnd(milliseconds, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEmpty()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEmpty(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEmpty(
            int milliseconds
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEmpty(milliseconds, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEmpty(
            int milliseconds,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEmpty(milliseconds, false, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEmpty(
            int milliseconds,
            bool idle,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForEmpty(milliseconds, idle, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEvent()
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEvent(_Timeout.Infinite);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEvent(
            int milliseconds
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEvent(milliseconds, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEvent(
            int milliseconds,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return WaitForEvent(milliseconds, false, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WaitForEvent(
            int milliseconds,
            bool idle,
            bool strict
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWaitForEvent(milliseconds, idle, strict);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Callback Methods
        public bool Queue(
            EventCallback callback,
            IClientData clientData
            )
        {
            CheckDisposed();

            return Queue(TimeOps.GetUtcNow(), callback, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Queue(
            DateTime dateTime,
            EventCallback callback,
            IClientData clientData
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (eventManager == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = eventManager.QueueEvent(null, dateTime, callback,
                clientData, GetEventFlags(), EventPriority.ScriptThread,
                ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Evaluation Methods
        public bool Queue(
            string text
            )
        {
            CheckDisposed();

            return Queue(TimeOps.GetUtcNow(), text);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Queue(
            DateTime dateTime,
            string text
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = interpreter.QueueScript(dateTime, text, GetEventFlags(),
                ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Queue(
            string text,
            AsynchronousCallback callback,
            IClientData clientData
            )
        {
            CheckDisposed();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = interpreter.EvaluateScript(text, callback, clientData,
                ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Synchronous Evaluation Methods
        public ReturnCode Send(
            string text,
            ref Result result
            )
        {
            CheckDisposed();

            return Send(text, false, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Send(
            string text,
            bool useEngine,
            ref Result result
            )
        {
            CheckDisposed();

            return Send(text, _Timeout.Infinite, useEngine, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Send(
            string text,
            int milliseconds,
            bool useEngine,
            ref Result result
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            string eventName = FormatOps.EventName(interpreter,
                scriptThreadSendEventPrefix, null, GlobalState.NextEventId(
                interpreter));

            using (EventWaitHandle @event = ThreadOps.CreateEvent(eventName))
            {
                if (useEngine)
                {
                    Result localResult = null;

                    if (Queue(text, new AsynchronousCallback(
                            delegate(IAsynchronousContext context)
                            {
                                using (EventWaitHandle localEvent =
                                        ThreadOps.OpenEvent(eventName))
                                {
                                    //
                                    // NOTE: Grab the return code and result
                                    //       from the asynchronous context.
                                    //
                                    if (context != null)
                                    {
                                        localResult = context.Result;
                                        code = context.ReturnCode;
                                    }
                                    else
                                    {
                                        localResult = "invalid asynchronous context";
                                        code = ReturnCode.Error;
                                    }

                                    //
                                    // NOTE: Signal that we are done.
                                    //
                                    if (localEvent != null)
                                        ThreadOps.SetEvent(localEvent);
                                }
                            }),
                        null))
                    {
                        if (ThreadOps.WaitEvent(@event, milliseconds))
                        {
                            result = localResult;

                            //
                            // NOTE: The return code is already set.
                            //
                        }
                        else
                        {
                            result = String.Format(
                                "engine script timeout of {0} milliseconds",
                                milliseconds);

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "could not queue script to engine thread";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    EventInputPair eventInputPair =
                        new EventInputPair(text, eventName);

                    EventOutputPair eventOutputPair =
                        new EventOutputPair(true);

                    IClientData clientData = new ClientData(
                        new AnyPair<EventInputPair, EventOutputPair>(
                            eventInputPair, eventOutputPair));

                    if (Queue(TimeOps.GetUtcNow(), ScriptEventCallback,
                            clientData))
                    {
                        if (ThreadOps.WaitEvent(@event, milliseconds))
                        {
                            result = eventOutputPair.Y;
                            code = eventOutputPair.X;
                        }
                        else
                        {
                            result = String.Format(
                                "event script timeout of {0} milliseconds",
                                milliseconds);

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "could not queue script to event thread";
                        code = ReturnCode.Error;
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Signaling Methods
        public bool Signal(
            string value
            )
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateSignal(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool WakeUp()
        {
            CheckDisposed();
            CheckRestricted();

            return PrivateWakeUp();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Asynchronous Cancellation Methods
        public bool Cancel(
            CancelFlags cancelFlags
            )
        {
            CheckDisposed();
            CheckRestricted();

            Result error = null;

            return Cancel(cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Cancel(
            CancelFlags cancelFlags,
            ref Result error
            )
        {
            CheckDisposed();
            CheckRestricted();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;

            code = interpreter.CancelAnyEvaluate(
                null, cancelFlags, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetCancel(
            CancelFlags cancelFlags
            )
        {
            CheckDisposed();
            CheckRestricted();

            Result error = null;

            return ResetCancel(cancelFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ResetCancel(
            CancelFlags cancelFlags,
            ref Result error
            )
        {
            CheckDisposed();
            CheckRestricted();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;

            code = Engine.ResetCancel(interpreter, cancelFlags, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Cleanup()
        {
            CheckDisposed();
            CheckRestricted();

            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            return interpreter.MaybeDisposeThread();
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            long id;
            ThreadFlags threadFlags;

            lock (syncRoot)
            {
                id = this.id;
                threadFlags = this.threadFlags;
            }

            return StringList.MakeList("id", id, "threadFlags", threadFlags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static ThreadFlags GetThreadFlags(
            ThreadFlags? threadFlags,
            out bool throwOnDisposed,
            out bool safe,
            out bool userInterface,
            out bool isBackground,
            out bool quiet,
            out bool noBackgroundError,
            out bool useSelf,
            out bool noComplain,
            out bool verbose,
            out bool debug,
            out bool usePool,
            out bool start,
            out bool noAbort
            )
        {
            ThreadFlags result;

            if (threadFlags != null)
                result = (ThreadFlags)threadFlags;
            else
                result = DefaultThreadFlags;

            throwOnDisposed = FlagOps.HasFlags(
                result, ThreadFlags.ThrowOnDisposed, true);

            safe = FlagOps.HasFlags(result, ThreadFlags.Safe, true);

            userInterface = FlagOps.HasFlags(
                result, ThreadFlags.UserInterface, true);

            isBackground = FlagOps.HasFlags(
                result, ThreadFlags.IsBackground, true);

            quiet = FlagOps.HasFlags(result, ThreadFlags.Quiet, true);

            noBackgroundError = FlagOps.HasFlags(
                result, ThreadFlags.NoBackgroundError, true);

            useSelf = FlagOps.HasFlags(result, ThreadFlags.UseSelf, true);

            noComplain = FlagOps.HasFlags(
                result, ThreadFlags.NoComplain, true);

            verbose = FlagOps.HasFlags(result, ThreadFlags.Verbose, true);
            debug = FlagOps.HasFlags(result, ThreadFlags.Debug, true);

            ///////////////////////////////////////////////////////////////////
            // NOTE: The following are used during Create() only.
            ///////////////////////////////////////////////////////////////////

            usePool = FlagOps.HasFlags(result, ThreadFlags.UsePool, true);
            start = FlagOps.HasFlags(result, ThreadFlags.Start, true);
            noAbort = FlagOps.HasFlags(result, ThreadFlags.NoAbort, true);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static CreateFlags GetCreateFlags(
            CreateFlags? createFlags,
            bool throwOnDisposed,
            bool safe
            )
        {
            CreateFlags result;

            if (createFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (CreateFlags)createFlags;
            }
            else
            {
                //
                // NOTE: Start with the default flags and adjust them according
                //       to the boolean switches specified by the caller.
                //
                result = DefaultCreateFlags;

                if (throwOnDisposed)
                    result |= CreateFlags.ThrowOnDisposed;
                else
                    result &= ~CreateFlags.ThrowOnDisposed;

                if (safe)
                    result |= CreateFlags.SafeAndHideUnsafe;
                else
                    result &= ~CreateFlags.SafeAndHideUnsafe;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static InitializeFlags GetInitializeFlags(
            InitializeFlags? initializeFlags
            )
        {
            InitializeFlags result;

            if (initializeFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (InitializeFlags)initializeFlags;
            }
            else
            {
                //
                // NOTE: Use the default flags.
                //
                result = DefaultInitializeFlags;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ScriptFlags GetScriptFlags(
            ScriptFlags? scriptFlags
            )
        {
            ScriptFlags result;

            if (scriptFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (ScriptFlags)scriptFlags;
            }
            else
            {
                //
                // NOTE: Use the default flags.
                //
                result = DefaultScriptFlags;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static InterpreterFlags GetInterpreterFlags(
            InterpreterFlags? interpreterFlags
            )
        {
            InterpreterFlags result;

            if (interpreterFlags != null)
            {
                //
                // NOTE: Always use the supplied flags verbatim.
                //
                result = (InterpreterFlags)interpreterFlags;
            }
            else
            {
                //
                // NOTE: Use the default flags.
                //
                result = DefaultInterpreterFlags;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static EventWaitFlags GetEventWaitFlags(
            EventWaitFlags? eventWaitFlags,
            ThreadFlags threadFlags
            )
        {
            EventWaitFlags result = (eventWaitFlags != null) ?
                (EventWaitFlags)eventWaitFlags : EventWaitFlags.None;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.NoCancel, true))
                result |= EventWaitFlags.NoCancel;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.StopOnError, true))
                result |= EventWaitFlags.StopOnError;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.ErrorOnEmpty, true))
                result |= EventWaitFlags.ErrorOnEmpty;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.NoComplain, true))
                result |= EventWaitFlags.NoComplain;

#if NATIVE && TCL
            if (FlagOps.HasFlags(threadFlags, ThreadFlags.TclThread, true))
                result |= EventWaitFlags.TclDoOneEvent;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.TclWaitEvent, true))
                result |= EventWaitFlags.TclWaitEvent;

            if (FlagOps.HasFlags(threadFlags, ThreadFlags.TclAllEvents, true))
                result |= EventWaitFlags.TclAllEvents;
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetThreadName(
            ScriptThread scriptThread
            )
        {
            if (scriptThread == null)
                return null;

            string name = scriptThread.PrivateName;

            if (name != null)
                return name;

            return String.Format("scriptThread#{0}", scriptThread.PrivateId);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void PrivateComplain( /* NOTE: For Create() only. */
            bool noComplain,
            Result result
            )
        {
            PrivateComplain(null, noComplain, ReturnCode.Error, result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void PrivateComplain(
            Interpreter interpreter,
            bool noComplain,
            ReturnCode code,
            Result result
            )
        {
            if (noComplain)
            {
                TraceOps.DebugTrace(String.Format(
                    "Complain: {0}", ResultOps.Format(code, result)),
                    typeof(ScriptThread).Name,
                    TracePriority.ScriptThreadError);

                return;
            }

            DebugOps.Complain(interpreter, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ScriptEventCallback(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (clientData == null)
            {
                result = "invalid clientData";
                return ReturnCode.Error;
            }

            IAnyPair<EventInputPair, EventOutputPair> anyPair =
                clientData.Data as IAnyPair<EventInputPair, EventOutputPair>;

            if (anyPair == null)
            {
                result = "clientData is not a pair";
                return ReturnCode.Error;
            }

            EventInputPair eventInputPair = anyPair.X;

            if (eventInputPair == null)
            {
                result = "invalid event input pair";
                return ReturnCode.Error;
            }

            EventOutputPair eventOutputPair = anyPair.Y;

            if (eventOutputPair == null)
            {
                result = "invalid event output pair";
                return ReturnCode.Error;
            }

            string eventName = eventInputPair.Y;

            using (EventWaitHandle @event = ThreadOps.OpenEvent(eventName))
            {
                try
                {
                    result = eventOutputPair.Y;

                    eventOutputPair.X = interpreter.EvaluateScript(
                        eventInputPair.X, ref result); /* EXEMPT */

                    eventOutputPair.Y = result;

                    return eventOutputPair.X;
                }
                finally
                {
                    if (@event != null)
                        ThreadOps.SetEvent(@event);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            string objectName,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                result = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return MarshalOps.FixupReturnValue(
                interpreter, interpreter.Binder, interpreter.CultureInfo,
                null, objectFlags, options, objectOptionType, objectName,
                null, value, true, false, alias, aliasReference, false,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode EvaluateScript(
            Interpreter interpreter,
            IScript script,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                result = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return interpreter.EvaluateScript(script, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void InterruptOrAbortThread(
            Thread thread,
            int timeout,
            bool verbose,
            bool noAbort
            )
        {
            //
            // NOTE: Finally, check if the physical thread is still alive.
            //       If so, try to interrupt and/or abort it.
            //
            if (thread == null)
                return;

            string threadName = FormatOps.DisplayThread(thread);

            if (!thread.IsAlive)
            {
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "InterruptOrAbortThread: script thread " +
                        "with [{0}] is already dead", threadName),
                        typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }

                thread = null;
                return;
            }

            try
            {
                //
                // NOTE: The thread is still alive and it must die now.
                //       Wait for a bit (now that the interpreter has
                //       been disposed) and then forcibly abort it if
                //       necessary.
                //
                thread.Interrupt(); /* throw */

                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "InterruptOrAbortThread: interrupted " +
                        "script thread with [{0}]", threadName),
                        typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }

                if (thread.Join(timeout))
                {
                    if (verbose)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "InterruptOrAbortThread: joined script thread " +
                            "with [{0}] and with timeout {1}", threadName,
                            timeout), typeof(ScriptThread).Name,
                            TracePriority.ScriptThreadDebug);
                    }
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "InterruptOrAbortThread: failed to join " +
                        "script thread with [{0}] and with timeout {1}",
                        threadName, timeout), typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadError);

                    //
                    // NOTE: If the NoAbort thread flag is set, we NEVER call
                    //       the Abort() method.
                    //
                    if (!noAbort)
                    {
                        thread.Abort(); /* BUGBUG: Leaks? */

                        TraceOps.DebugTrace(String.Format(
                            "InterruptOrAbortThread: aborted script " +
                            "thread with [{0}]", threadName),
                            typeof(ScriptThread).Name,
                            TracePriority.ScriptThreadDebug);
                    }
                    else
                    {
                        if (verbose)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "InterruptOrAbortThread: skipped aborting " +
                                "script thread with [{0}]", threadName),
                                typeof(ScriptThread).Name,
                                TracePriority.ScriptThreadDebug);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(ScriptThread).Name,
                    TracePriority.ScriptThreadError);
            }

            thread = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool IsRestricted()
        {
            ThreadFlags threadFlags;

            lock (syncRoot)
            {
                threadFlags = this.threadFlags;
            }

            return FlagOps.HasFlags(threadFlags,
                ThreadFlags.Restricted, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private void CheckRestricted() /* throw */
        {
            if (IsRestricted())
                throw new ScriptException("method access denied");
        }

        ///////////////////////////////////////////////////////////////////////

        private void MarkRestricted(
            bool restricted
            )
        {
            lock (syncRoot)
            {
                if (restricted)
                    threadFlags |= ThreadFlags.Restricted;
                else
                    threadFlags &= ~ThreadFlags.Restricted;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventFlags GetEventFlags()
        {
            Interpreter interpreter;
            bool debug;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
                debug = this.debug;
            }

            EventFlags result = (interpreter != null) ?
                interpreter.QueueEventFlags : EventFlags.None;

            result &= ~EventFlags.DequeueMask;
            result |= EventFlags.After | EventFlags.Interpreter;

            if (debug)
                result |= EventFlags.Debug;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private long PrivateId /* NOTE: For Create(). */
        {
            get { lock (syncRoot) { return id; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private string PrivateName /* NOTE: For Create(). */
        {
            get { lock (syncRoot) { return name; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private int PrivateTimeout /* NOTE: For Create(). */
        {
            get { lock (syncRoot) { return timeout; } }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateIsAlive
        {
            get
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                }

                return (thread != null) && thread.IsAlive;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal bool PrivateIsBusy /* NOTE: For Interpreter.IsOwnerBusy(). */
        {
            get
            {
                Interpreter interpreter;

                lock (syncRoot)
                {
                    interpreter = this.interpreter;
                }

                return (interpreter != null) && interpreter.InternalIsBusy;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateIsDisposed
        {
            get
            {
                return disposed;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by PrivateSignalAndSleep() and Shutdown() only.
        //
        private bool PrivateSignal(
            string value
            )
        {
            Interpreter interpreter;
            string varName;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
                varName = this.varName;
            }

            if (interpreter == null)
                return false;

            ReturnCode code;
            Result error = null;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (interpreter.Disposed)
                    return false;

                code = interpreter.SetVariableValue(
                    VariableFlags.None, varName, value, null, ref error);
            }

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateWakeUp() /* NOTE: For Shutdown(). */
        {
            EventWaitHandle wakeUpEvent;

            lock (syncRoot)
            {
                wakeUpEvent = this.wakeUpEvent;
            }

            if (wakeUpEvent == null)
                return false;

            return ThreadOps.SetEvent(wakeUpEvent);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateWaitForStart( /* NOTE: For WaitForStart(). */
            int milliseconds,
            bool strict
            )
        {
            try
            {
                EventWaitHandle startEvent;

                lock (syncRoot)
                {
                    startEvent = this.startEvent;
                }

                if (startEvent == null)
                    return !strict;

                return ThreadOps.WaitEvent(startEvent, milliseconds);
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not wait for thread start: {0}", e));
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateWaitForEnd( /* NOTE: For Shutdown(). */
            int milliseconds,
            bool strict
            )
        {
            try
            {
                Thread thread;

                lock (syncRoot)
                {
                    thread = this.thread;
                }

                if (thread == null)
                    return !strict;
                else if (!thread.IsAlive)
                    return true;

                return thread.Join(milliseconds); /* throw */
            }
            catch (Exception e)
            {
                PrivateComplain(ReturnCode.Error, String.Format(
                    "could not wait for thread join: {0}", e));
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateWaitForEmpty( /* NOTE: For WaitForEmpty(). */
            int milliseconds,
            bool idle,
            bool strict
            )
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (eventManager == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = eventManager.WaitForEmptyQueue(
                milliseconds, idle, strict, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateWaitForEvent( /* NOTE: For WaitForEvent(). */
            int milliseconds,
            bool idle,
            bool strict
            )
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
                return false;

            IEventManager eventManager = interpreter.EventManager;

            if (eventManager == null)
                return false;

            ReturnCode code;
            Result error = null;

            code = eventManager.WaitForEventEnqueued(
                milliseconds, idle, strict, ref error);

            if (code != ReturnCode.Ok)
                PrivateComplain(code, error);

            return (code == ReturnCode.Ok);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by Create() only.
        //
        private void PrivateSignalAndSleep(
            int timeout
            )
        {
            //
            // HACK: Attempt to get the newly created script thread to exit
            //       by signaling its variable (if possible).
            //
            /* IGNORED */
            PrivateSignal(null);

            /* NO RESULT */
            HostOps.ThreadSleepOrMaybeComplain(timeout, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private void PrivateComplain(
            ReturnCode code,
            Result result
            )
        {
            Interpreter interpreter;
            bool noComplain;

            lock (syncRoot)
            {
                noComplain = this.noComplain;
                interpreter = this.interpreter;
            }

            PrivateComplain(interpreter, noComplain, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateIsNoAbort() /* NO-LOCK */
        {
            return FlagOps.HasFlags(
                threadFlags, ThreadFlags.NoAbort, true);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode FixupReturnValue(
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            string objectName,
            ObjectFlags objectFlags,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            )
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                result = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return FixupReturnValue(
                interpreter, options, objectOptionType, objectName,
                objectFlags, value, alias, aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode WaitVariable(
            Interpreter interpreter,
            string varName,
            int limit,
            ref Result error
            )
        {
            EventWaitHandle wakeUpEvent;
            EventWaitFlags eventWaitFlags;

            lock (syncRoot)
            {
                wakeUpEvent = this.wakeUpEvent;
                eventWaitFlags = this.eventWaitFlags;
            }

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.Disposed) /* RACE */
            {
                error = "interpreter is disposed";
                return ReturnCode.Error;
            }

            return interpreter.WaitVariable(
                eventWaitFlags, VariableFlags.WaitVariableMask, varName,
                limit, wakeUpEvent, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private void ThreadStart(
            object obj
            )
        {
            Interlocked.Increment(ref activeCount);

            try
            {
                IEnumerable<string> args;
                IHost host;
                CreateFlags createFlags;
                InitializeFlags initializeFlags;
                ScriptFlags scriptFlags;
                InterpreterFlags interpreterFlags;
                bool quiet;
                bool noBackgroundError;
                bool useSelf;

#if THREADING
                bool usePool;
#endif

                EventWaitHandle startEvent;
                IScript script;
                string varName;

                lock (syncRoot)
                {
                    args = this.args;
                    host = this.host;
                    createFlags = this.createFlags;
                    initializeFlags = this.initializeFlags;
                    scriptFlags = this.scriptFlags;
                    interpreterFlags = this.interpreterFlags;
                    quiet = this.quiet;
                    noBackgroundError = this.noBackgroundError;
                    useSelf = this.useSelf;

#if THREADING
                    usePool = this.usePool;
#endif

                    startEvent = this.startEvent;
                    script = this.script;
                    varName = this.varName;
                }

#if THREADING
                Interpreter purgeInterpreter = null;
#endif

                bool setEvent = false;
                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    //
                    // BUGBUG: If this interpreter creation takes too long, the
                    //         thread that started this thread will timeout and
                    //         dispose of this object.  Depending on the timing
                    //         of that disposal, the newly created interpreter
                    //         may end up being disposed as well (i.e. if the
                    //         "interpreter" field is updated before our parent
                    //         thread calls Dispose).  Technically, this thread
                    //         creation process contains several potential race
                    //         conditions; however, they are well-known and can
                    //         now be easily detected by either thread.
                    //
                    using (Interpreter interpreter = Interpreter.Create(
                            args, createFlags, initializeFlags, scriptFlags,
                            interpreterFlags, host, ref result))
                    {
                        if (interpreter != null)
                        {
#if THREADING
                            purgeInterpreter = interpreter;
#endif

                            interpreter.Owner = this;
                            interpreter.Quiet = quiet;

                            interpreter.SetNoBackgroundError(
                                noBackgroundError);

                            lock (syncRoot)
                            {
                                this.interpreter = interpreter;
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                            goto done;
                        }

                        if (startEvent != null)
                        {
                            setEvent = ThreadOps.SetEvent(startEvent);

                            if (!setEvent)
                            {
                                result = "failed to set start event";
                                code = ReturnCode.Error;

                                goto done;
                            }
                        }

                        if (useSelf)
                        {
                            code = FixupReturnValue(
                                interpreter, null,
                                DefaultObjectOptionType,
                                scriptThreadObjectName,
                                ObjectFlags.NoDispose, this,
                                true, false, ref result);

                            if (code != ReturnCode.Ok)
                                goto done;
                        }

                        if (script != null)
                        {
                            code = EvaluateScript(
                                interpreter, script,
                                ref result);

                            if (code != ReturnCode.Ok)
                                goto done;
                        }

                        if (varName != null)
                        {
                            code = WaitVariable(
                                interpreter, varName, 0,
                                ref result);

                            if (code != ReturnCode.Ok)
                                goto done;
                        }

                        //
                        // NOTE: At this point, if we successfully created an
                        //       interpreter in this method it will be disposed
                        //       when the containing using block was exited;
                        //       therefore, null out the interpreter contained
                        //       in the class field so that external callers do
                        //       not try to use it or dispose it [again].
                        //
                        lock (syncRoot)
                        {
                            this.interpreter = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    result = e;
                    code = ReturnCode.Error;
                }

            done:

                //
                // NOTE: Make sure that when we fail to create an interpreter
                //       we set the "start event" for this thread so that
                //       any threads waiting are released (i.e. we don't want
                //       them to wait forever).
                //
                if (!setEvent && (startEvent != null))
                    /* IGNORED */
                    ThreadOps.SetEvent(startEvent);

                //
                // NOTE: If we failed for any reason, complain about it now
                //       since we have no means of returning a direct result
                //       to our creator.
                //
                if (code != ReturnCode.Ok)
                    PrivateComplain(code, result);

#if THREADING
                //
                // NOTE: Finally, be sure to re-purge all context information
                //       about this thread from the context manager since the
                //       thread is now exiting.
                //
                ContextManager.Purge(purgeInterpreter, !usePool, true);
#endif
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Disposal Helper Methods
        private bool IsDead(
            ref string threadName
            ) /* NO-LOCK */
        {
            threadName = FormatOps.DisplayThread(thread);
            return (thread == null) || !thread.IsAlive;
        }

        ///////////////////////////////////////////////////////////////////////

        private void Shutdown(
            string value,
            int timeout,
            bool verbose,
            bool strict
            ) /* NO-LOCK */
        {
            //
            // NOTE: If the thread is already dead, there is nothing that
            //       we need to do.
            //
            string threadName = null;

            if (IsDead(ref threadName))
            {
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "Shutdown: script thread with [{0}] is already dead",
                        threadName), typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }

                return;
            }

            //
            // BUGFIX: First, try to nicely shutdown the thread by setting
            //         its associated variable and then waiting a bit for
            //         the thread to exit.
            //
            if (PrivateSignal(value) &&
                PrivateWakeUp() && PrivateWaitForEnd(timeout, strict))
            {
                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "Shutdown: signal {0}, wake up, and join script " +
                        "thread with [{1}] and with timeout {2} success",
                        FormatOps.WrapOrNull(value), threadName, timeout),
                        typeof(ScriptThread).Name,
                        TracePriority.ScriptThreadDebug);
                }
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "Shutdown: signal {0}, wake up, and join script " +
                    "thread with [{1}] and with timeout {2} failure",
                    FormatOps.WrapOrNull(value), threadName, timeout),
                    typeof(ScriptThread).Name,
                    TracePriority.ScriptThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void DisposeInterpreter()
        {
            Interpreter interpreter;

            lock (syncRoot)
            {
                interpreter = this.interpreter;
            }

            if (interpreter != null)
            {
                interpreter.Dispose(); /* throw */
                interpreter = null;
            }

            lock (syncRoot)
            {
                this.interpreter = interpreter;
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(ScriptThread).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(ScriptThread).Name,
                TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: First, try graceful thread shutdown.
                    //
                    Shutdown(null, timeout, verbose, false);

                    //
                    // NOTE: Next, dispose of the interpreter.  This should
                    //       cause the ThreadStart method to exit if it has
                    //       not done so already.
                    //
                    DisposeInterpreter();

                    //
                    // NOTE: Finally, try to interrupt or abort the running
                    //       thread.
                    //
                    /* NO RESULT */
                    InterruptOrAbortThread(
                        thread, timeout, verbose, PrivateIsNoAbort());

                    //
                    // NOTE: Close the event handles that we created.
                    //
                    ThreadOps.CloseEvent(ref wakeUpEvent);
                    ThreadOps.CloseEvent(ref startEvent);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~ScriptThread()
        {
            Dispose(false);
        }
        #endregion
    }
}
