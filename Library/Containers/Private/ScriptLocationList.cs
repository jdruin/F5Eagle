/*
 * ScriptLocationList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using Eagle._Attributes;
using Eagle._Constants;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e1168217-1a48-49fb-8c0d-a4bffaf632a7")]
    internal sealed class ScriptLocationList : StackList<IScriptLocation>
    {
        #region Public Constructors
        public ScriptLocationList()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return GenericOps<IScriptLocation>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
