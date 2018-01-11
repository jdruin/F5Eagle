/*
 * TypeDictionary.cs --
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
    [ObjectId("24757152-2455-4f25-8f9e-94d510722fce")]
    internal sealed class TypeDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<string, Type>
#else
        Dictionary<string, Type>
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
        public TypeDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private TypeDictionary(
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
