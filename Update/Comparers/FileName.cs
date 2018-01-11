/*
 * FileName.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;
using System.Text;
using Eagle._Components.Private;
using Eagle._Interfaces.Private;

namespace Eagle._Comparers
{
    [Guid("a86b864a-2087-4856-8918-c7abffdeeb47")]
    internal sealed class FileName : IAnyComparer<string>
    {
        #region Private Constants
        private const StringComparison comparisonType =
#if WINDOWS
            StringComparison.OrdinalIgnoreCase;
#else
            StringComparison.Ordinal;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private Encoding encoding = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public FileName()
            : this(Encoding.UTF8)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public FileName(
            Encoding encoding
            )
        {
            this.encoding = encoding;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        public int Compare(
            string x,
            string y
            )
        {
            return String.Compare(x, y, comparisonType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static bool EndsWith(
            string x,
            string y
            )
        {
            if (String.IsNullOrEmpty(x) || (y == null))
                return false;

            if (y.Length > x.Length)
                return false;

            return x.EndsWith(y, comparisonType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        public bool Equals(
            string x,
            string y
            )
        {
            return Compare(x, y) == 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(string obj)
        {
            int result = 0;

            if ((obj != null) && (encoding != null))
            {
                result ^= unchecked((int)HashOps.HashFnv1UInt(
                    encoding.GetBytes(obj), true));
            }

            return result;
        }
        #endregion
    }
}
