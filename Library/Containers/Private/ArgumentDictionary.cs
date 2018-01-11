/*
 * ArgumentDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !CACHE_DICTIONARY
using System.Collections.Generic;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Containers.Private
{
    [ObjectId("5a4eacd4-644d-4145-8bb7-f66e7cb08b9b")]
    internal sealed class ArgumentDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<Argument, Argument>
#else
        Dictionary<Argument, Argument>
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        internal readonly int[] cacheCounts =
            new int[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ArgumentDictionary(
            int capacity
            )
            : base(capacity, new _Comparers._Argument())
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
#if CACHE_STATISTICS
        public bool HaveCacheCounts()
        {
            if (this.Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////

        public string CacheToString()
        {
            return StringList.MakeList(
                "count", this.Count,
#if CACHE_DICTIONARY
                "maximumCount", this.MaximumCount,
                "maximumAccessCount", this.MaximumAccessCount,
#endif
                FormatOps.CacheCounts(cacheCounts));
        }
#endif
        #endregion
    }
}
