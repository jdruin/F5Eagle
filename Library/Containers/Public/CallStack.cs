/*
 * CallStack.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Public
{
    [ObjectId("71f3559d-68e7-47de-9356-916c0c147f63")]
    public sealed class CallStack : StackList<ICallFrame>, IDisposable
    {
        #region Public Constructors
        public CallStack(bool canFree)
            : base()
        {
            this.canFree = canFree;
        }

        ///////////////////////////////////////////////////////////////////////

        public CallStack(
            IEnumerable<ICallFrame> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public CallStack(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        private bool canFree;
        public bool CanFree
        {
            get { /* CheckDisposed(); */ return canFree; }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Disposed
        {
            get { return disposed; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Free(
            bool global
            )
        {
            //
            // HACK: *SPECIAL CASE* We cannot dispose the global call frame
            //       unless we are [also] disposing of the interpreter itself;
            //       therefore, use the special Free method here instead of the
            //       Dispose method.  The Free method is guaranteed to do the
            //       right thing with regard to the global call frame (assuming
            //       the "global" paramter is correct).
            //
            foreach (ICallFrame frame in this)
            {
                if (frame == null)
                    continue;

                frame.Free(global);
            }

            //
            // NOTE: Finally, clear all frames from the call stack.
            //
            Clear();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            // CheckDisposed();

            return GenericOps<ICallFrame>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            // CheckDisposed();

            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(CallStack));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (canFree)
                        Free(true);
                    else
                        Clear();
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
        ~CallStack()
        {
            Dispose(false);
        }
        #endregion
    }
}
