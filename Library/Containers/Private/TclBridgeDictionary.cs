/*
 * TclBridgeDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Private.Tcl;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Containers.Private.Tcl
{
    [ObjectId("44c35e4c-8d85-4758-8482-5658d2555cbf")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBridgeDictionary : Dictionary<string, TclBridge>
    {
        public TclBridgeDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public TclBridgeDictionary(
            IDictionary<string, TclBridge> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(string pattern, bool noCase)
        {
            return GenericOps<string, TclBridge>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(IDictionary<string, TclBridge> dictionary)
        {
            foreach (KeyValuePair<string, TclBridge> pair in dictionary)
                this.Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
