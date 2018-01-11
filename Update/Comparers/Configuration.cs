/*
 * Configuration.cs --
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
using System.Runtime.InteropServices;
using System.Text;
using Eagle._Components.Private;

namespace Eagle._Comparers
{
    //
    // WARNING: For the purposes of this IEqualityComparer implementation, only
    //          the lookup fields of the configuration are actually considered
    //          in the comparisons (i.e. publicKeyToken, name, and culture).
    //
    [Guid("330569ee-973a-459a-8dfc-665aabba72f6")]
    internal sealed class _Configuration : IEqualityComparer<Configuration>
    {
        #region Private Data
        private StringComparison comparisonType = StringComparison.Ordinal;
        private Encoding encoding = null;

        ///////////////////////////////////////////////////////////////////////

        private ByteArray publicKeyTokenComparer = null;

        ///////////////////////////////////////////////////////////////////////

        private _CultureInfo cultureInfoComparer = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private _Configuration()
        {
            publicKeyTokenComparer = new ByteArray();
            cultureInfoComparer = new _CultureInfo();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public _Configuration(
            StringComparison comparisonType,
            Encoding encoding
            )
            : this()
        {
            this.comparisonType = comparisonType;
            this.encoding = encoding;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<byte[]> Members
        public bool Equals(
            Configuration x,
            Configuration y
            )
        {
            if ((x == null) && (y == null))
            {
                return true;
            }
            else if ((x == null) || (y == null))
            {
                return false;
            }
            else
            {
                //
                // HACK: An "Id" of less than zero means "any".
                //
                if ((x.Id >= 0) && (y.Id >= 0) && (x.Id != y.Id))
                    return false;

                if (!String.Equals(
                        x.ProtocolId, y.ProtocolId, comparisonType))
                {
                    return false;
                }

                if ((publicKeyTokenComparer != null) &&
                    !publicKeyTokenComparer.Equals(
                        x.PublicKeyToken, y.PublicKeyToken))
                {
                    return false;
                }

                if (!String.Equals(x.Name, y.Name, comparisonType))
                    return false;

                if ((cultureInfoComparer != null) &&
                    !cultureInfoComparer.Equals(x.Culture, y.Culture))
                {
                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            Configuration obj
            )
        {
            int result = 0;

            if ((obj != null) && (obj.ProtocolId != null) &&
                (encoding != null))
            {
                result ^= unchecked((int)HashOps.HashFnv1UInt(
                    encoding.GetBytes(obj.ProtocolId), true));
            }

            if ((obj != null) && (obj.PublicKeyToken != null) &&
                (publicKeyTokenComparer != null))
            {
                result ^= publicKeyTokenComparer.GetHashCode(
                    obj.PublicKeyToken);
            }

            if ((obj != null) && (obj.Name != null) &&
                (encoding != null))
            {
                result ^= unchecked((int)HashOps.HashFnv1UInt(
                    encoding.GetBytes(obj.Name), true));
            }

            if ((obj != null) && (obj.Culture != null) &&
                (cultureInfoComparer != null))
            {
                result ^= cultureInfoComparer.GetHashCode(
                    obj.Culture);
            }

            return result;
        }
        #endregion
    }
}
