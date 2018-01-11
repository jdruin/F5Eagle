/*
 * StringListDictionary.cs --
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

using System.Collections.Generic;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d4e35240-1862-4fc6-9a6a-56fa059031b5")]
    internal sealed class StringListDictionary :
#if CACHE_DICTIONARY
        CacheDictionary<string, StringList>
#else
        Dictionary<string, StringList>
#endif
    {
        #region Private Data
#if CACHE_STATISTICS
        internal readonly int[] cacheCounts =
            new int[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringListDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringListDictionary(
            int capacity,
            bool cache
            )
            : base(capacity, cache ? new _Comparers.StringObject() : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        public StringListDictionary(
            IDictionary<string, StringList> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public StringListDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringListDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringListDictionary(
            int capacity,
            IEqualityComparer<string> comparer
            )
            : base(capacity, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public StringListDictionary(
            IDictionary<string, StringList> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private StringListDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Dead Code
#if DEAD_CODE
        public StringList Add(
            string key,
            StringList value,
            bool reserved
            )
        {
            Add(key, value);

            return this[key];
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void Merge(
            string key,
            StringList value
            )
        {
            StringList oldValue;

            if (TryGetValue(key, out oldValue))
            {
                if (value != null)
                {
                    if (oldValue != null)
                        oldValue.AddRange(value);
                    else
                        this[key] = new StringList(value);
                }
            }
            else
            {
                Add(key, (value != null) ?
                    new StringList(value) : null);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        public bool HaveCacheCounts()
        {
            if (this.Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, StringList>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return GenericOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
