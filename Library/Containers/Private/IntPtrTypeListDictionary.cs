/*
 * IntPtrTypeListDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if !CACHE_DICTIONARY
using System.Collections.Generic;
#endif

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;

#if CACHE_STATISTICS
using Eagle._Components.Private;
using Eagle._Components.Public;
#endif

using Eagle._Containers.Public;

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("5bdee0d3-1eb8-47c2-bac8-81b9d178fcd2")]
    internal sealed class IntPtrTypeListDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<IntPtr, TypeList>
#else
        Dictionary<IntPtr, TypeList>
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
        public IntPtrTypeListDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private IntPtrTypeListDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
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
