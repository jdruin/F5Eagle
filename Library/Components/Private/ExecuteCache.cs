/*
 * ExecuteCache.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if CACHE_STATISTICS
using System.Threading;
#endif

using Eagle._Attributes;

#if CACHE_STATISTICS
using Eagle._Components.Public;
#endif

using Eagle._Containers.Private;

#if CACHE_STATISTICS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("5fcf4ba1-d84c-46fc-84a5-14d0f98e014a")]
    internal sealed class ExecuteCache
    {
        #region Private Data
#if CACHE_STATISTICS
        internal readonly int[] cacheCounts =
            new int[(int)CacheCountType.SizeOf]; // WARNING: CACHE USE ONLY.
#endif

        private ExecuteDictionary cache;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ExecuteCache()
        {
            cache = new ExecuteDictionary();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Properties
        public int Count
        {
            get { return (cache != null) ? cache.Count : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Clear()
        {
            if (cache != null)
            {
                cache.Clear();

#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Clear]);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool TryGet(
            string name,
            bool validate,
            ref IExecute execute
            )
        {
            if ((cache != null) && (name != null))
            {
                if (cache.TryGetValue(name, out execute))
                {
                    if (!validate || (execute != null))
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.Found]);
#endif

                        return true;
                    }
                }
            }

#if CACHE_STATISTICS
            Interlocked.Increment(
                ref cacheCounts[(int)CacheCountType.NotFound]);
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool AddOrUpdate(
            string name,
            IExecute execute,
            bool invalidate
            )
        {
            if ((cache != null) && (name != null))
            {
                if (invalidate)
                {
                    cache.Clear();

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Clear]);
#endif
                }
                else if (cache.ContainsKey(name))
                {
                    cache[name] = execute;

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Change]);
#endif

                    return true;
                }

                cache.Add(name, execute);

#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Add]);
#endif

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Rename(
            string oldName,
            string newName,
            IExecute execute,
            bool invalidate
            )
        {
            if ((cache != null) && (oldName != null) && (newName != null))
            {
                if (invalidate)
                {
                    cache.Clear();

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Clear]);
#endif
                }
                else if (cache.ContainsKey(oldName))
                {
                    if (cache.Remove(oldName))
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.Remove]);
#endif
                    }
                }

                cache.Add(newName, execute);

#if CACHE_STATISTICS
                Interlocked.Increment(
                    ref cacheCounts[(int)CacheCountType.Add]);
#endif

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool Remove(
            string name,
            bool invalidate
            )
        {
            if ((cache != null) && (name != null))
            {
                if (invalidate)
                {
                    cache.Clear();

#if CACHE_STATISTICS
                    Interlocked.Increment(
                        ref cacheCounts[(int)CacheCountType.Clear]);
#endif

                    return true;
                }
                else if (cache.ContainsKey(name))
                {
                    if (cache.Remove(name))
                    {
#if CACHE_STATISTICS
                        Interlocked.Increment(
                            ref cacheCounts[(int)CacheCountType.Remove]);
#endif

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        public bool HaveCacheCounts()
        {
            if (Count > 0)
                return true;

            return FormatOps.HaveCacheCounts(cacheCounts);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string CacheToString()
        {
            return StringList.MakeList(
                "count", Count, FormatOps.CacheCounts(cacheCounts));
        }
#endif
        #endregion
    }
}
