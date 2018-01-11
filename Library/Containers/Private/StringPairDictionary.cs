/*
 * StringPairDictionary.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
    [ObjectId("a22cdd6d-d3b5-4336-a4f0-c54cd618004f")]
    internal sealed class StringPairDictionary : Dictionary<string, IPair<string>>
    {
        public StringPairDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringPairDictionary(
            IDictionary<string, IPair<string>> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private StringPairDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private StringPairDictionary(
            IDictionary<string, IPair<string>> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }
#endif
        #endregion
    }
}
