/*
 * EngineThread.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("e1a3509f-1b6b-4940-8cfc-7d21c2d81c93")]
    internal sealed class EngineThread : IDisposable
    {
        #region Private Data
        //
        // NOTE: This is the primary interpreter associated with this thread,
        //       which is set only during its creation.  It is NOT owned by
        //       this object and will not be disposed.
        //
        private Interpreter interpreter;

        //
        // NOTE: This is the parameterless start delegate for this thread.  It
        //       is only used when handling the ThreadStart delegate.
        //
        private ThreadStart threadStart;

        //
        // NOTE: This is the parameterized start delegate for this thread.  It
        //       is used when handling the ParameterizedThreadStart delegate
        //       and/or the ThreadStart delegate if the parameterless start
        //       delegate is not available.
        //
        private ParameterizedThreadStart parameterizedThreadStart;

        //
        // NOTE: This is the framework thread associated with this thread.  It
        //       is NOT owned by this object and will not be disposed.
        //
#if MONO_BUILD
#pragma warning disable 414
#endif
        private Thread thread;
#if MONO_BUILD
#pragma warning restore 414
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private EngineThread(
            Interpreter interpreter,
            ThreadStart start
            )
        {
            this.interpreter = interpreter;
            this.threadStart = start;
        }

        ///////////////////////////////////////////////////////////////////////

        private EngineThread(
            Interpreter interpreter,
            ParameterizedThreadStart start
            )
        {
            this.interpreter = interpreter;
            this.parameterizedThreadStart = start;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static EngineThread Create(
            Interpreter interpreter,
            ThreadStart start
            )
        {
            return new EngineThread(interpreter, start);
        }

        ///////////////////////////////////////////////////////////////////////

        public static EngineThread Create(
            Interpreter interpreter,
            ParameterizedThreadStart start
            )
        {
            return new EngineThread(interpreter, start);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public Thread GetThread()
        {
            CheckDisposed();

            return thread;
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetThread(
            Thread thread
            )
        {
            CheckDisposed();

            this.thread = thread;
        }

        ///////////////////////////////////////////////////////////////////////

        /* System.Threading.ThreadStart */
        public void ThreadStart()
        {
            CheckDisposed();

            try
            {
#if NATIVE && WINDOWS
                RuntimeOps.RefreshNativeStackPointers();
#endif

                if (threadStart != null)
                {
                    threadStart();
                }
                else if (parameterizedThreadStart != null)
                {
                    parameterizedThreadStart(null);
                }
                else
                {
                    TraceOps.DebugTrace(
                        "ThreadStart: no delegates available",
                        typeof(EngineThread).Name,
                        TracePriority.ThreadError);
                }
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(EngineThread).Name,
                    TracePriority.ThreadError);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EngineThread).Name,
                    TracePriority.ThreadError);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EngineThread).Name,
                    TracePriority.ThreadError);
            }
            finally
            {
                if (interpreter != null)
                    interpreter.MaybeDisposeThread();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /* System.Threading.ParameterizedThreadStart */
        public void ParameterizedThreadStart(
            object obj
            )
        {
            CheckDisposed();

            try
            {
#if NATIVE && WINDOWS
                RuntimeOps.RefreshNativeStackPointers();
#endif

                if (parameterizedThreadStart != null)
                {
                    parameterizedThreadStart(obj);
                }
                else
                {
                    TraceOps.DebugTrace(
                        "ParameterizedThreadStart: no delegate available",
                        typeof(EngineThread).Name,
                        TracePriority.ThreadError);
                }
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(EngineThread).Name,
                    TracePriority.ThreadError);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EngineThread).Name,
                    TracePriority.ThreadError);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EngineThread).Name,
                    TracePriority.ThreadError);
            }
            finally
            {
                if (interpreter != null)
                    interpreter.MaybeDisposeThread();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(EngineThread).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(EngineThread).Name,
                TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    interpreter = null; /* NOT OWNED, DO NOT DISPOSE. */
                    threadStart = null;
                    parameterizedThreadStart = null;
                    thread = null; /* NOT OWNED, DO NOT DISPOSE. */
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
        ~EngineThread()
        {
            Dispose(false);
        }
        #endregion
    }
}
