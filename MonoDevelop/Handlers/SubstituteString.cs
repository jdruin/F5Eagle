/*
 * SubstituteString.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////
//    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this code, it is a proof-of-concept only.  It is not
// production ready.
//
//    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Handlers
{
    [ObjectId("eb1b3980-d45d-4e96-a243-8051878d2254")]
    internal sealed class SubstituteString : Script
    {
        #region Public Constructors
        public SubstituteString()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region MonoDevelop.Components.Commands.CommandHandler Overrides
        protected override void Run()
        {
            IEditableTextBuffer buffer = GetEditableTextBuffer();

            if (buffer == null)
                return;

            string text = buffer.SelectedText;

            if (String.IsNullOrEmpty(text))
                return;

            ReturnCode code;
            Result result = null;

            code = SubstituteString(text, ref result);
            result = Utility.FormatResult(code, result);

            HandleResult(GetType(), buffer, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void Update(
            CommandInfo info
            )
        {
            UpdateText(info);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(SubstituteString).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected override void Dispose(
            bool disposing
            )
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~SubstituteString()
        {
            Dispose(false);
        }
        #endregion
    }
}
