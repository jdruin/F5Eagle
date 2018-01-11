/*
 * InteractiveContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Private;

#if HISTORY
using Eagle._Interfaces.Public;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("aa7d8954-f29a-48f4-8fe2-5a20bc61846d")]
    internal sealed class InteractiveContext :
            IInteractiveContext, IDisposable /* optional */
    {
        #region Public Constructors
        public InteractiveContext(
            Interpreter interpreter,
            int threadId
            )
        {
            this.interpreter = interpreter;
            this.threadId = threadId;

            interactive = false;
            interactiveInput = null;
            previousInteractiveInput = null;
            interactiveMode = null;
            activeInteractiveLoops = 0;
            totalInteractiveLoops = 0;

            interactiveLoopData = null;
            interactiveCommandCallback = null;

#if HISTORY
            historyLoadData = null;
            historySaveData = null;

            historyInfoFilter = null;
            historyLoadFilter = null;
            historySaveFilter = null;

            historyFileName = null;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        //
        // WARNING: This field/property is for debugging use only and may be
        //          removed in the future.
        //
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadContext Members
        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int threadId;
        public int ThreadId
        {
            get
            {
                //
                // NOTE: *EXEMPT* Hot path.
                //
                // CheckDisposed();

                return threadId;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveContext Members
        private bool interactive;
        public bool Interactive
        {
            get { CheckDisposed(); return interactive; }
            set { CheckDisposed(); interactive = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string interactiveInput;
        public string InteractiveInput
        {
            get { CheckDisposed(); return interactiveInput; }
            set { CheckDisposed(); interactiveInput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string previousInteractiveInput;
        public string PreviousInteractiveInput
        {
            get { CheckDisposed(); return previousInteractiveInput; }
            set { CheckDisposed(); previousInteractiveInput = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string interactiveMode;
        public string InteractiveMode
        {
            get { CheckDisposed(); return interactiveMode; }
            set { CheckDisposed(); interactiveMode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int activeInteractiveLoops;
        public int ActiveInteractiveLoops
        {
            get { CheckDisposed(); return activeInteractiveLoops; }
            set { CheckDisposed(); activeInteractiveLoops = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int totalInteractiveLoops;
        public int TotalInteractiveLoops
        {
            get { CheckDisposed(); return totalInteractiveLoops; }
            set { CheckDisposed(); totalInteractiveLoops = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private InteractiveLoopData interactiveLoopData;
        public InteractiveLoopData InteractiveLoopData
        {
            get { CheckDisposed(); return interactiveLoopData; }
            set { CheckDisposed(); interactiveLoopData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringTransformCallback interactiveCommandCallback;
        public StringTransformCallback InteractiveCommandCallback
        {
            get { CheckDisposed(); return interactiveCommandCallback; }
            set { CheckDisposed(); interactiveCommandCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        private IHistoryData historyLoadData;
        public IHistoryData HistoryLoadData
        {
            get { CheckDisposed(); return historyLoadData; }
            set { CheckDisposed(); historyLoadData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryData historySaveData;
        public IHistoryData HistorySaveData
        {
            get { CheckDisposed(); return historySaveData; }
            set { CheckDisposed(); historySaveData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryFilter historyInfoFilter;
        public IHistoryFilter HistoryInfoFilter
        {
            get { CheckDisposed(); return historyInfoFilter; }
            set { CheckDisposed(); historyInfoFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryFilter historyLoadFilter;
        public IHistoryFilter HistoryLoadFilter
        {
            get { CheckDisposed(); return historyLoadFilter; }
            set { CheckDisposed(); historyLoadFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IHistoryFilter historySaveFilter;
        public IHistoryFilter HistorySaveFilter
        {
            get { CheckDisposed(); return historySaveFilter; }
            set { CheckDisposed(); historySaveFilter = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string historyFileName;
        public string HistoryFileName
        {
            get { CheckDisposed(); return historyFileName; }
            set { CheckDisposed(); historyFileName = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(InteractiveContext));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: disposing = {0}, interpreter = {1}, disposed = {2}",
                disposing, FormatOps.InterpreterNoThrow(interpreter), disposed),
                typeof(InteractiveContext).Name, TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    interpreter = null; /* NOT OWNED: Do not dispose. */
                    threadId = 0;

                    ///////////////////////////////////////////////////////////

                    interactive = false;
                    interactiveInput = null;
                    previousInteractiveInput = null;
                    interactiveMode = null;
                    activeInteractiveLoops = 0;
                    totalInteractiveLoops = 0;

                    interactiveLoopData = null;
                    interactiveCommandCallback = null;

#if HISTORY
                    historyLoadData = null;
                    historySaveData = null;

                    historyInfoFilter = null;
                    historyLoadFilter = null;
                    historySaveFilter = null;

                    historyFileName = null;
#endif
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
        ~InteractiveContext()
        {
            Dispose(false);
        }
        #endregion
    }
}
