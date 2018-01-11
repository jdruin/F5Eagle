/*
 * CacheConfiguration.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;

#if CACHE_DICTIONARY
using System.Collections.Generic;
#endif

using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("245051cd-4bae-45e2-ab31-adc247aa046e")]
    internal static class CacheConfiguration
    {
        #region Private Constants
        //
        // NOTE: These are the delimters used to split the settings text into
        //       its parts, which are then (currently) converted to integers.
        //
        private static readonly char[] Separators = {
            Characters.Space, Characters.Comma
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: The default settings are hard-coded into the following
        //       strings, one per caching "level".  Each (integer) setting
        //       within its string must be delimited by spaces or commas.
        //       The available settings are defined as follows:
        //
        //       item[0x00] == Read Enabled                 (bool, enabled)
        //       item[0x01] == Write Enabled                (bool, enabled)
        //       item[0x02] == Delete Enabled               (bool, enabled)
        //       item[0x03] == Last Accessed Enabled        (bool, enabled)
        //       item[0x04] == Maximum Clear Memory Load    (uint, percent)
        //       item[0x05] == Maximum Write Memory Load    (uint, percent)
        //       item[0x06] == Maximum Trim Memory Load     (uint, percent)
        //       item[0x07] == Maximum Bump Memory Load     (uint, percent)
        //       item[0x08] == Maximum Compact Memory Load  (uint, percent)
        //       item[0x09] == Maximum Read Text Item Size  (int, length)
        //       item[0x0A] == Minimum Read Text Item Size  (int, length)
        //       item[0x0B] == Maximum Write Text Item Size (int, length)
        //       item[0x0C] == Minimum Write Text Item Size (int, length)
        //       item[0x0D] == Maximum Read List Item Size  (int, count)
        //       item[0x0E] == Minimum Read List Item Size  (int, count)
        //       item[0x0F] == Maximum Write List Item Size (int, count)
        //       item[0x10] == Minimum Write List Item Size (int, count)
        //       item[0x11] == Maximum Size                 (int, items)
        //       item[0x12] == Minimum Size                 (int, items)
        //       item[0x13] == Maximum Trim Item Count      (int, items)
        //       item[0x14] == Minimum Trim Item Count      (int, items)
        //       item[0x15] == Minimum Trim Milliseconds    (int, milliseconds)
        //       item[0x16] == Maximum Change Count         (int, occurrences)
        //       item[0x17] == Minimum Change Count         (int, occurrences)
        //       item[0x18] == Maximum Change Milliseconds  (int, milliseconds)
        //       item[0x19] == Minimum Change Milliseconds  (int, milliseconds)
        //       item[0x1A] == Maximum Usage Count          (int, occurrences)
        //       item[0x1B] == Minimum Usage Count          (int, occurrences)
        //       item[0x1C] == Maximum Usage Milliseconds   (int, milliseconds)
        //       item[0x1D] == Maximum Maybe Disable Count  (int, occurrences)
        //       item[0x1E] == Maximum Maybe Enable Count   (int, occurrences)
        //
        // TODO: *PERF* Good defaults?
        //
        // HACK: *PERF* This is not read-only.
        //
        private static string[] DefaultSettings = {
            /* Level 0: UNUSED */
            null,

            /* Level 1: 32-bit, low */
            "1 1 1 1 75 70 65 60 55 10485760 0 10485760 0  2621440 0 " +
            "2621440 0 6000 200 5000 500 10000 20000 0 30000 60000 " +
            "100 2 10000 5 0",

            /* Level 2: 32-bit, high */
            "1 1 1 1 90 85 70 75 65 10485760 0 10485760 0  2621440 0 " +
            "2621440 0 6000 200 5000 500 10000 20000 0 30000 60000 " +
            "100 2 10000 5 0",

            /* Level 3: 64-bit, low */
            "1 1 1 1 75 70 65 60 55        0 0        0 0        0 0 " +
            "      0 0 6000 200 5000 500 10000 20000 0 30000 60000 " +
            "100 2 10000 5 0",

            /* Level 4: 64-bit, high */
            "1 1 1 1 90 85 70 75 65        0 0        0 0        0 0 " +
            "      0 0 6000 200 5000 500 10000 20000 0 30000 60000 " +
            "100 2 10000 5 0"
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access (within this class only)
        //       to the static cache settings, primarily the boolean that is
        //       used to determine if initialization was already completed.
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Have these (shared) cache settings already been initialized
        //       in the context of some created interpreter?
        //
        private static bool initialized;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: This constant declares how much total physical memory, in
        //       bytes, is considered to be "bare bones" for machines that
        //       are running the Eagle core library.  It will only be used
        //       to set the default cache level (i.e. its aggressiveness).
        //
        private static ulong badMemoryThreshold = 1073741824; /* bytes */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constant declares how much total physical memory, in
        //       bytes, is considered to be "quite a bit" for machines that
        //       are running the Eagle core library.  It will only be used
        //       to set the default cache level (i.e. its aggressiveness).
        //
        private static ulong goodMemoryThreshold = 3221225472; /* bytes */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the lowest and highest memory loads seen so far.
        //
        private static uint[] memoryLoadBounds = { 0, 0 };

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Has a warning been traced since the last low-memory condition
        //       was hit?  The cache operations associated with these are (in
        //       the order they are used):
        //
        //       [0]: Unknown
        //       [1]: Clear
        //       [2]: Write
        //       [3]: Trim
        //       [4]: Bump
        //       [5]: Compact
        //
        private static bool[] lowMemoryWarning = {
            false, false, false, false, false, false
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Has a warning been traced since the last low-memory condition
        //       was cleared?  The cache operations associated with these are
        //       (in the order they are used):
        //
        //       [0]: Unknown
        //       [1]: Clear
        //       [2]: Write
        //       [3]: Trim
        //       [4]: Bump
        //       [5]: Compact
        //
        private static bool[] okMemoryWarning = {
            false, false, false, false, false, false
        };
#endif

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        //
        // NOTE: Keep track of the number of times a cache is disabled via the
        //       MaybeEnableOrDisable method, on a per-cache (type) basis.
        //
        private static Dictionary<CacheFlags, int> maybeDisableCounts =
            new Dictionary<CacheFlags, int>();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Keep track of the number of times a cache is enabled via the
        //       MaybeEnableOrDisable method, on a per-cache (type) basis.
        //
        private static Dictionary<CacheFlags, int> maybeEnableCounts =
            new Dictionary<CacheFlags, int>();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Keep track of the total number of microseconds spent during
        //       cache trimming operations.
        //
        private static PerformanceClientData trimPerformanceClientData =
            new PerformanceClientData("trim", true);
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is the cache enabled for reading (i.e. can items be used)?
        //
        private static bool readEnabled;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is the cache enabled for writing (i.e. can items be added)?
        //
        private static bool writeEnabled;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Is the cache enabled for deleting (i.e. can items be removed)?
        //
        private static bool deleteEnabled;

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        //
        // NOTE: Should the cache track the last-accessed information on a
        //       per-entry basis?
        //
        private static bool accessedEnabled;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: Zero means there is no maximum.
        //
        private static uint maximumClearMemoryLoad; /* percent */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Zero means there is no maximum.
        //
        private static uint maximumWriteMemoryLoad; /* percent */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Zero means there is no maximum.
        //
        private static uint maximumTrimMemoryLoad; /* percent */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Zero means there is no maximum.
        //
        private static uint maximumBumpMemoryLoad; /* percent */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Zero means there is no maximum.
        //
        private static uint maximumCompactMemoryLoad; /* percent */
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no maximum.
        //
        private static int maximumReadTextItemSize; /* characters */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no minimum.
        //
        private static int minimumReadTextItemSize; /* characters */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no maximum.
        //
        private static int maximumWriteTextItemSize; /* characters */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no minimum.
        //
        private static int minimumWriteTextItemSize; /* characters */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no maximum.
        //
        private static int maximumReadListItemSize; /* items */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no minimum.
        //
        private static int minimumReadListItemSize; /* items */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no maximum.
        //
        private static int maximumWriteListItemSize; /* items */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no minimum.
        //
        private static int minimumWriteListItemSize; /* items */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than or equal to zero means there is no maximum.
        //
        private static int maximumSize; /* items */

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int minimumSize; /* items */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no maximum.
        //
        private static int maximumTrimItemCount; /* items */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int minimumTrimItemCount; /* items */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int minimumTrimMilliseconds; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no maximum.
        //
        private static int maximumChangeCount; /* occurrences */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int minimumChangeCount; /* occurrences */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no maximum.
        //
        private static int maximumChangeMilliseconds; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int minimumChangeMilliseconds; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int maximumUsageCount; /* occurrences */

        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int minimumUsageCount; /* occurrences */

        //
        // NOTE: What was the originally configured value for the
        //       "minimumUsageCount" setting?
        //
        private static int savedMinimumUsageCount; /* occurrences */

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: Less than zero means there is no minimum.
        //
        private static int minimumUsageMilliseconds; /* milliseconds */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When was the configured minimum usage count bumped last?
        //       Null means never.
        //
        private static DateTime? lastUsageCount;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Less than zero means there is no maximum.
        //
        private static int maximumMaybeDisableCount; /* occurrences */

        //
        // NOTE: Less than zero means there is no maximum.
        //
        private static int maximumMaybeEnableCount; /* occurrences */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        //
        // NOTE: This method returns the default cache settings that are
        //       associated with the specified zero-based level.  First,
        //       one is added to the level value, so the initial (null)
        //       entry will be skipped.  The value null is returned when
        //       the specified zero-based level is unavailable.
        //
        private static string GetDefaultSettings(
            int level
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int index = (level <= Level.Invalid) ? 0 : (level + 1);

                if (DefaultSettings != null)
                {
                    int length = DefaultSettings.Length;

                    if ((index >= 0) && (index < length))
                        return DefaultSettings[index];
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method returns a zero-based maximum cache level.  Prior
        //       to it being used, one will be added to it *IF* that value is
        //       already zero or higher.  Therefore, using negative one as
        //       the default method return value will effectively disable all
        //       caching.
        //
        private static int GetMaximumLevel()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int level = Level.Invalid; /* Level 0: none */

                if (DefaultSettings != null)
                {
                    int length = DefaultSettings.Length;

                    if (length > 1)
                        level = (length - 2);
                }

                return level;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        //
        // TODO: Add future CacheFlags values here if they refer to a new type
        //       of cached object.
        //
        private static CacheFlags GetExtraTrimCacheFlags(
            CacheFlags cacheFlags,
            CacheFlags allCacheFlags
            )
        {
            switch (cacheFlags)
            {
                case CacheFlags.Argument:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ForceTrimArgument,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.ForceTrim;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.StringList:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ForceTrimStringList,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.ForceTrim;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.IParseState:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ForceTrimIParseState,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.ForceTrim;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.IExecute:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ForceTrimIExecute,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.ForceTrim;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.Type:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ForceTrimType,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.ForceTrim;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.ComTypeList:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ForceTrimComTypeList,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.ForceTrim;
                        }

                        return extraCacheFlags;
                    }
                default:
                    {
                        return CacheFlags.None;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Add future CacheFlags values here if they refer to a new type
        //       of cached object.
        //
        private static CacheFlags GetExtraEnableCacheFlags(
            CacheFlags cacheFlags,
            CacheFlags allCacheFlags,
            bool enable
            )
        {
            switch (cacheFlags)
            {
                case CacheFlags.Argument:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ResetArgument,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Reset;
                            extraCacheFlags |= CacheFlags.SetProperties;
                        }

                        if (!enable && FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ClearArgument,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Clear;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.StringList:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ResetStringList,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Reset;
                            extraCacheFlags |= CacheFlags.SetProperties;
                        }

                        if (!enable && FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ClearStringList,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Clear;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.IParseState:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ResetIParseState,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Reset;
                            extraCacheFlags |= CacheFlags.SetProperties;
                        }

                        if (!enable && FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ClearIParseState,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Clear;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.IExecute:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ResetIExecute,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Reset;
                            extraCacheFlags |= CacheFlags.SetProperties;
                        }

                        if (!enable && FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ClearIExecute,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Clear;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.Type:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ResetType,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Reset;
                            extraCacheFlags |= CacheFlags.SetProperties;
                        }

                        if (!enable && FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ClearType,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Clear;
                        }

                        return extraCacheFlags;
                    }
                case CacheFlags.ComTypeList:
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ResetComTypeList,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Reset;
                            extraCacheFlags |= CacheFlags.SetProperties;
                        }

                        if (!enable && FlagOps.HasFlags(
                                allCacheFlags, CacheFlags.ClearComTypeList,
                                true))
                        {
                            extraCacheFlags |= CacheFlags.Clear;
                        }

                        return extraCacheFlags;
                    }
                default:
                    {
                        return CacheFlags.None;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool RecordMaybeEnableOrDisable(
            CacheFlags cacheFlags,
            bool enable
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Dictionary<CacheFlags, int> dictionary;
                int maximumCount;

                if (enable)
                {
                    dictionary = maybeEnableCounts;
                    maximumCount = maximumMaybeEnableCount;
                }
                else
                {
                    dictionary = maybeDisableCounts;
                    maximumCount = maximumMaybeDisableCount;
                }

                if (dictionary == null)
                    return false;

                int count;

                if (dictionary.TryGetValue(cacheFlags, out count))
                {
                    dictionary[cacheFlags] = ++count;
                }
                else
                {
                    count = 1; dictionary.Add(cacheFlags, count);
                }

                if (maximumCount <= 0)
                    return false;

                //
                // NOTE: Have there been too many calls to disable/enable a
                //       particular cache?
                //
                return count >= maximumCount;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // NOTE: This method is a bit confusing.  It returns the minimum of
        //       the three "maximum memory load" values; however, returning
        //       zero is avoided unless all the "maximum memory load" values
        //       are zero.
        //
        private static uint GetMinimumMemoryLoad()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                uint minimumMemoryLoad = 0;

                foreach (uint maximumMemoryLoad in
                    new uint[] {
                        maximumClearMemoryLoad,
                        maximumWriteMemoryLoad,
                        maximumTrimMemoryLoad,
                        maximumBumpMemoryLoad,
                        maximumCompactMemoryLoad
                    })
                {
                    if (maximumMemoryLoad > 0)
                    {
                        if ((minimumMemoryLoad == 0) ||
                            (maximumMemoryLoad < minimumMemoryLoad))
                        {
                            minimumMemoryLoad = maximumMemoryLoad;
                        }
                    }
                }

                return minimumMemoryLoad;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not remove this method, it is used by the test suite.
        //
        private static bool IsMemoryLoadOk()
        {
            return IsMemoryLoadOk(
                null, CacheFlags.None, 0, GetMinimumMemoryLoad());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsClearMemoryLoadOk(
            CacheFlags cacheFlags
            )
        {
            return IsMemoryLoadOk(
                "clear", cacheFlags, 1, maximumClearMemoryLoad);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsWriteMemoryLoadOk(
            CacheFlags cacheFlags
            )
        {
            return IsMemoryLoadOk(
                "write", cacheFlags, 2, maximumWriteMemoryLoad);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsTrimMemoryLoadOk(
            CacheFlags cacheFlags
            )
        {
            return IsMemoryLoadOk(
                "trim", cacheFlags, 3, maximumTrimMemoryLoad);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsBumpMemoryLoadOk(
            CacheFlags cacheFlags
            )
        {
            return IsMemoryLoadOk(
                "bump", cacheFlags, 4, maximumBumpMemoryLoad);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Make this more cross-platform and possibly more flexible.
        //
        private static bool IsMemoryLoadOk(
            string operation,
            CacheFlags cacheFlags,
            int warningIndex,
            uint maximumMemoryLoad
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (maximumMemoryLoad > 0)
                {
                    bool gotMemoryLoad;
                    uint memoryLoad = 0;

                    gotMemoryLoad = NativeOps.GetMemoryLoad(ref memoryLoad);

                    if (gotMemoryLoad)
                    {
                        if ((memoryLoadBounds[0] == 0) ||
                            (memoryLoad < memoryLoadBounds[0]))
                        {
                            if (memoryLoadBounds[0] != 0)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "IsMemoryLoadOk: memory load {0} for " +
                                    "{1} [{2}] operation on {3} cache is " +
                                    "below previous lower bound {4}",
                                    memoryLoad,
                                    FormatOps.WrapOrNull(operation),
                                    warningIndex,
                                    FormatOps.WrapOrNull(cacheFlags),
                                    memoryLoadBounds[0]),
                                    typeof(CacheConfiguration).Name,
                                    TracePriority.EngineDebug);
                            }

                            memoryLoadBounds[0] = memoryLoad;
                        }

                        if ((memoryLoadBounds[1] == 0) ||
                            (memoryLoad > memoryLoadBounds[1]))
                        {
                            if (memoryLoadBounds[1] != 0)
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "IsMemoryLoadOk: memory load {0} for " +
                                    "{1} [{2}] operation on {3} cache is " +
                                    "above previous upper bound {4}",
                                    memoryLoad,
                                    FormatOps.WrapOrNull(operation),
                                    warningIndex,
                                    FormatOps.WrapOrNull(cacheFlags),
                                    memoryLoadBounds[1]),
                                    typeof(CacheConfiguration).Name,
                                    TracePriority.EngineDebug);
                            }

                            memoryLoadBounds[1] = memoryLoad;
                        }

                        if (memoryLoad >= maximumMemoryLoad)
                        {
                            okMemoryWarning[warningIndex] = false;

                            if (!lowMemoryWarning[warningIndex])
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "IsMemoryLoadOk: memory load {0} for " +
                                    "{1} [{2}] operation on {3} cache hit " +
                                    "or exceeded maximum {4}",
                                    memoryLoad,
                                    FormatOps.WrapOrNull(operation),
                                    warningIndex,
                                    FormatOps.WrapOrNull(cacheFlags),
                                    maximumMemoryLoad),
                                    typeof(CacheConfiguration).Name,
                                    TracePriority.EngineDebug);

                                lowMemoryWarning[warningIndex] = true;
                            }

                            return false;
                        }

                        lowMemoryWarning[warningIndex] = false;

                        if (!okMemoryWarning[warningIndex])
                        {
                            TraceOps.DebugTrace(String.Format(
                                "IsMemoryLoadOk: memory load {0} for " +
                                "{1} [{2}] operation on {3} cache now " +
                                "lower than maximum {4}",
                                memoryLoad,
                                FormatOps.WrapOrNull(operation),
                                warningIndex,
                                FormatOps.WrapOrNull(cacheFlags),
                                maximumMemoryLoad),
                                typeof(CacheConfiguration).Name,
                                TracePriority.EngineDebug);

                            okMemoryWarning[warningIndex] = true;
                        }
                    }
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        //
        // NOTE: This method assumes the lock is already held.
        //
        private static void MaybeChangeMinimumUsageCount<TKey, TValue>(
            CacheDictionary<TKey, TValue> cacheDictionary
            )
        {
            if (cacheDictionary == null)
                return;

            int cacheMaximumAccessCount = cacheDictionary.MaximumAccessCount;

            if (cacheMaximumAccessCount != 0)
            {
                if ((minimumUsageCount > cacheMaximumAccessCount) &&
                    (minimumUsageCount > savedMinimumUsageCount))
                {
                    minimumUsageCount = Math.Max(
                        savedMinimumUsageCount, cacheMaximumAccessCount);
                }
                else
                {
                    minimumUsageCount++;
                }
            }
            else
            {
                minimumUsageCount++;
            }
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE || PARSE_CACHE || TYPE_CACHE
        //
        // TODO: In the future, make this use settings other than the minimum
        //       and maximum write sizes.
        //
        private static bool IsItemSizeOk(
            string text,
            bool nullOk,
            bool reading
            )
        {
            if (text != null)
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    int length = text.Length;

                    if (reading)
                    {
                        if ((minimumReadTextItemSize > 0) &&
                            (length <= minimumReadTextItemSize))
                        {
                            return false;
                        }

                        if ((maximumReadTextItemSize > 0) &&
                            (length >= maximumReadTextItemSize))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if ((minimumWriteTextItemSize > 0) &&
                            (length <= minimumWriteTextItemSize))
                        {
                            return false;
                        }

                        if ((maximumWriteTextItemSize > 0) &&
                            (length >= maximumWriteTextItemSize))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                return nullOk;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        //
        // TODO: In the future, make this use settings other than the minimum
        //       and maximum write sizes.
        //
        private static bool IsItemSizeOk(
            Argument argument,
            bool nullOk,
            bool reading
            )
        {
            if (argument != null)
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    int length = argument.Length;

                    if (reading)
                    {
                        if ((minimumReadTextItemSize > 0) &&
                            (length <= minimumReadTextItemSize))
                        {
                            return false;
                        }

                        if ((maximumReadTextItemSize > 0) &&
                            (length >= maximumReadTextItemSize))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if ((minimumWriteTextItemSize > 0) &&
                            (length <= minimumWriteTextItemSize))
                        {
                            return false;
                        }

                        if ((maximumWriteTextItemSize > 0) &&
                            (length >= maximumWriteTextItemSize))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                return nullOk;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE || PARSE_CACHE || COM_TYPE_CACHE
        private static bool IsItemSizeOk(
            ICollection collection,
            bool nullOk,
            bool reading
            )
        {
            if (collection != null)
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    int count = collection.Count;

                    if (reading)
                    {
                        if ((minimumReadListItemSize > 0) &&
                            (count <= minimumReadListItemSize))
                        {
                            return false;
                        }

                        if ((maximumReadListItemSize > 0) &&
                            (count >= maximumReadListItemSize))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if ((minimumWriteListItemSize > 0) &&
                            (count <= minimumWriteListItemSize))
                        {
                            return false;
                        }

                        if ((maximumWriteListItemSize > 0) &&
                            (count >= maximumWriteListItemSize))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                return nullOk;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY && NATIVE
        private static double GetUsageCountMilliseconds()
        {
            if ((minimumUsageMilliseconds < 0.0) || (lastUsageCount == null))
                return Milliseconds.Never;

            return TimeOps.GetUtcNow().Subtract(
                (DateTime)lastUsageCount).TotalMilliseconds;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void TouchUsageCount()
        {
            lastUsageCount = TimeOps.GetUtcNow();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static StringList GetStateAndSettings()
        {
            StringList list = new StringList();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringList subList1 = new StringList();
                int defaultLevel = GetDefaultLevel();

                subList1.Add("default");
                subList1.Add(defaultLevel.ToString());
                subList1.Add(GetDefaultSettings(defaultLevel));

                int maximumLevel = GetMaximumLevel();

                subList1.Add("maximum");
                subList1.Add(maximumLevel.ToString());
                subList1.Add(GetDefaultSettings(maximumLevel));

#if NATIVE
                StringList subList2 = new StringList();

                subList2.Add(badMemoryThreshold.ToString());
                subList2.Add(goodMemoryThreshold.ToString());

                subList2.Add(memoryLoadBounds[0].ToString());
                subList2.Add(memoryLoadBounds[1].ToString());

                subList2.Add(lowMemoryWarning[0].ToString());
                subList2.Add(lowMemoryWarning[1].ToString());
                subList2.Add(lowMemoryWarning[2].ToString());
                subList2.Add(lowMemoryWarning[3].ToString());
                subList2.Add(lowMemoryWarning[4].ToString());

                subList2.Add(okMemoryWarning[0].ToString());
                subList2.Add(okMemoryWarning[1].ToString());
                subList2.Add(okMemoryWarning[2].ToString());
                subList2.Add(okMemoryWarning[3].ToString());
                subList2.Add(okMemoryWarning[4].ToString());
#endif

#if CACHE_DICTIONARY
                StringList subList3 = new StringList();

                subList3.Add((maybeDisableCounts != null) ?
                    FormatOps.MaybeEnableOrDisable(maybeDisableCounts,
                        false) : null);

                subList3.Add((maybeEnableCounts != null) ?
                    FormatOps.MaybeEnableOrDisable(maybeEnableCounts,
                        false) : null);

                subList3.Add((trimPerformanceClientData != null) ?
                    trimPerformanceClientData.Microseconds.ToString() :
                    null);

                subList3.Add(savedMinimumUsageCount.ToString());

#if NATIVE
                subList3.Add((lastUsageCount != null) ?
                    FormatOps.Iso8601DateTime((DateTime)lastUsageCount) :
                    null);
#endif
#endif

                StringList subList4 = new StringList();

                subList4.Add(initialized.ToString());
                subList4.Add(readEnabled.ToString());
                subList4.Add(writeEnabled.ToString());
                subList4.Add(deleteEnabled.ToString());

#if CACHE_DICTIONARY
                subList4.Add(accessedEnabled.ToString());
#endif

#if NATIVE
                subList4.Add(maximumClearMemoryLoad.ToString());
                subList4.Add(maximumWriteMemoryLoad.ToString());
                subList4.Add(maximumTrimMemoryLoad.ToString());
                subList4.Add(maximumBumpMemoryLoad.ToString());
                subList4.Add(maximumCompactMemoryLoad.ToString());
#endif

                subList4.Add(maximumReadTextItemSize.ToString());
                subList4.Add(minimumReadTextItemSize.ToString());
                subList4.Add(maximumWriteTextItemSize.ToString());
                subList4.Add(minimumWriteTextItemSize.ToString());
                subList4.Add(maximumReadListItemSize.ToString());
                subList4.Add(minimumReadListItemSize.ToString());
                subList4.Add(maximumWriteListItemSize.ToString());
                subList4.Add(minimumWriteListItemSize.ToString());
                subList4.Add(maximumSize.ToString());

#if CACHE_DICTIONARY
                subList4.Add(minimumSize.ToString());
                subList4.Add(maximumTrimItemCount.ToString());
                subList4.Add(minimumTrimItemCount.ToString());
                subList4.Add(minimumTrimMilliseconds.ToString());
                subList4.Add(maximumChangeCount.ToString());
                subList4.Add(minimumChangeCount.ToString());
                subList4.Add(maximumChangeMilliseconds.ToString());
                subList4.Add(minimumChangeMilliseconds.ToString());
                subList4.Add(maximumUsageCount.ToString());
                subList4.Add(minimumUsageCount.ToString());

#if NATIVE
                subList4.Add(minimumUsageMilliseconds.ToString());
#endif

                subList4.Add(maximumMaybeDisableCount.ToString());
                subList4.Add(maximumMaybeEnableCount.ToString());
#endif

                list.Add("settings");
                list.Add(subList1.ToString());

#if NATIVE
                list.Add("memory");
                list.Add(subList2.ToString());
#endif

#if CACHE_DICTIONARY
                list.Add("statistics");
                list.Add(subList3.ToString());
#endif

                list.Add("state");
                list.Add(subList4.ToString());
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method returns a zero-based default cache level.  Prior
        //       to it being used, one will be added to it *IF* that value is
        //       already zero or higher.  Therefore, using zero as the default
        //       method return value actually results in the first level being
        //       used.
        //
        public static int GetDefaultLevel()
        {
            int level = 0; /* Level 1: 32-bit, low */

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: If the appropriate configuration variable is set, force
            //       the default cache level up by one.
            //
            if (GlobalConfiguration.DoesValueExist(
                    EnvVars.BumpCacheLevel,
                    ConfigurationFlags.CacheConfiguration))
            {
                level++; /* Level 2: 32-bit, high */
            }

            ///////////////////////////////////////////////////////////////////

#if NATIVE
            //
            // NOTE: Attempt to query the total physical memory for this
            //       machine.  If this fails, just continue.
            //
            ulong totalPhysical = 0;

            if (NativeOps.GetTotalMemory(ref totalPhysical))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Does this machine appear to have a "bad" amount
                    //       of total physical memory?  If so, the lowest
                    //       level is returned immediately.
                    //
                    if (totalPhysical <= badMemoryThreshold)
                        return level; /* Level 1 or 2 */

                    //
                    // NOTE: Does this machine appear to have a "good" amount
                    //       of total physical memory?  If so, bump the next
                    //       level.
                    //
                    if (totalPhysical >= goodMemoryThreshold)
                        level++; /* Level 2 or 3 */
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: For 64-bit processes, the cache settings are much more
            //       aggressive.  In the future, this may need more thought
            //       and adjustment(s).
            //
            if (PlatformOps.Is64BitProcess())
                level += 2; /* Level 4 or 5 */

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Do not allow the returned default cache level to exceed
            //       the maximum cache level; in that case, just return the
            //       maximum cache level.
            //
            int maximumLevel = GetMaximumLevel();

            if (level > maximumLevel)
                level = maximumLevel; /* Level 4: 64-bit, high */

            ///////////////////////////////////////////////////////////////////

            return level;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method initializes the cache management subsystem, if
        //       necessary, based on the settings that are associated with
        //       the specified zero-based level.  If the cache management
        //       subsystem is already initialized, nothing is done unless
        //       the refresh parameter is non-zero.
        //
        public static void Initialize(
            Interpreter interpreter, /* NOTE: No script evaluation. */
            string text,
            int level,
            bool refresh
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If we are not manually refreshing the cache settings
                //       and we have already initialized at some point, just
                //       return now.
                //
                if (!refresh && initialized)
                    return;

                ///////////////////////////////////////////////////////////////

                //
                // HACK: Normally, an application (or plugin) would just use
                //       something like script evaluation here.  We cannot,
                //       because the interpreter is not yet fully created.
                //
                if (text == null)
                    text = GetDefaultSettings(level);

                ///////////////////////////////////////////////////////////////

                if (!String.IsNullOrEmpty(text))
                {
                    string[] parts = text.Split(
                        Separators, StringSplitOptions.RemoveEmptyEntries);

                    ///////////////////////////////////////////////////////////

                    CultureInfo cultureInfo = null;

                    if (interpreter != null)
                        cultureInfo = interpreter.CultureInfo;

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 1))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetBoolean2(
                            parts[0], ValueFlags.AnyInteger, cultureInfo,
                            ref readEnabled, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 2))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetBoolean2(
                            parts[1], ValueFlags.AnyInteger, cultureInfo,
                            ref writeEnabled, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 3))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetBoolean2(
                            parts[2], ValueFlags.AnyInteger, cultureInfo,
                            ref deleteEnabled, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
                    if ((parts != null) && (parts.Length >= 4))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetBoolean2(
                            parts[3], ValueFlags.AnyInteger, cultureInfo,
                            ref accessedEnabled, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }
#endif

                    ///////////////////////////////////////////////////////////

#if NATIVE
                    if ((parts != null) && (parts.Length >= 5))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetUnsignedInteger2(
                            parts[4], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumClearMemoryLoad, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 6))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetUnsignedInteger2(
                            parts[5], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumWriteMemoryLoad, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 7))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetUnsignedInteger2(
                            parts[6], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumTrimMemoryLoad, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 8))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetUnsignedInteger2(
                            parts[7], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumBumpMemoryLoad, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 9))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetUnsignedInteger2(
                            parts[8], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumCompactMemoryLoad, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }
#endif

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 10))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[9], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumReadTextItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 11))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[10], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumReadTextItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 12))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[11], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumWriteTextItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 13))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[12], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumWriteTextItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 14))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[13], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumReadListItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 15))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[14], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumReadListItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 16))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[15], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumWriteListItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 17))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[16], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumWriteListItemSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 18))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[17], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
                    if ((parts != null) && (parts.Length >= 19))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[18], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumSize, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 20))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[19], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumTrimItemCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 21))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[20], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumTrimItemCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 22))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[21], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumTrimMilliseconds, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 23))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[22], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumChangeCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 24))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[23], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumChangeCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 25))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[24], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumChangeMilliseconds, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 26))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[25], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumChangeMilliseconds, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 27))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[26], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumUsageCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 28))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[27], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumUsageCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);

                        savedMinimumUsageCount = minimumUsageCount;
                    }

                    ///////////////////////////////////////////////////////////

#if NATIVE
                    if ((parts != null) && (parts.Length >= 29))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[28], ValueFlags.AnyInteger, cultureInfo,
                            ref minimumUsageMilliseconds, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }
#endif

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 30))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[29], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumMaybeDisableCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }

                    ///////////////////////////////////////////////////////////

                    if ((parts != null) && (parts.Length >= 31))
                    {
                        ReturnCode code;
                        Result error = null;

                        code = Value.GetInteger2(
                            parts[30], ValueFlags.AnyInteger, cultureInfo,
                            ref maximumMaybeEnableCount, ref error);

                        if (code != ReturnCode.Ok)
                            DebugOps.Complain(interpreter, code, error);
                    }
#endif

                    //
                    // NOTE: At this point, initialization is complete.  It
                    //       does not matter how many of the cache settings
                    //       were read or converted correctly.
                    //
                    initialized = true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetCapacity()
        {
            lock (syncRoot) { return maximumSize; }
        }

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        public static void SetProperties<TKey, TValue>(
            CacheDictionary<TKey, TValue> cacheDictionary,
            bool enable
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (cacheDictionary == null)
                    return;

                cacheDictionary.TrimMilliseconds = minimumTrimMilliseconds;

                cacheDictionary.ChangeMilliseconds = enable ?
                    maximumChangeMilliseconds : minimumChangeMilliseconds;

                if (accessedEnabled != cacheDictionary.IsAccessedEnabled())
                    cacheDictionary.SetAccessedEnabled(accessedEnabled);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if (NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471) && NATIVE
        public static bool IsCompactMemoryLoadOk(
            CacheFlags cacheFlags
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return IsMemoryLoadOk(
                    "compact", cacheFlags, 5, maximumCompactMemoryLoad);
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool CanRead()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!readEnabled)
                    return false;

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanWrite(
            CacheFlags cacheFlags,
            ref bool full
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!writeEnabled)
                {
                    full = false;
                    return false;
                }

#if NATIVE
                //
                // NOTE: Stop adding to ALL caches if native system memory
                //       is starting to get "too full".
                //
                if (!IsWriteMemoryLoadOk(cacheFlags))
                {
                    full = true;
                    return false;
                }
#endif

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanDelete()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!deleteEnabled)
                    return false;

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSizeOk(
            ICollection collection,
            CacheFlags cacheFlags,
            bool nullOk,
            bool emptyOk
            )
        {
            if (collection != null)
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: First, grab the number of items currently in the
                    //       cache.  If the count is zero, always return true
                    //       because there is nothing else we can do.
                    //
                    int count = collection.Count;

                    if (count == 0)
                        return emptyOk;

                    //
                    // NOTE: Currently, this method is only used to determine
                    //       if the cache should be trimmed, so checking the
                    //       minimum does not really make sense because that
                    //       value is used as part of the TrimExcess operation
                    //       itself.  Additionally, the "MinimumSize" field is
                    //       only available when this class is compiled with
                    //       the "CACHE_DICTIONARY" compile-time option.
                    //
                    // if ((minimumSize > 0) && (count <= minimumSize))
                    //     return false;

                    if ((maximumSize > 0) && (count >= maximumSize))
                        return false;

#if NATIVE
                    //
                    // NOTE: Start trimming ALL caches if native system memory
                    //       is starting to get "too full".
                    //
                    if (!IsTrimMemoryLoadOk(cacheFlags))
                        return false;
#endif

                    return true;
                }
            }
            else
            {
                return nullOk;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE || PARSE_CACHE || TYPE_CACHE
        public static bool IsItemReadSizeOk(
            string text,
            bool nullOk
            )
        {
            return IsItemSizeOk(text, nullOk, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        public static bool IsItemReadSizeOk(
            Argument argument,
            bool nullOk
            )
        {
            return IsItemSizeOk(argument, nullOk, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE || PARSE_CACHE || TYPE_CACHE
        public static bool IsItemWriteSizeOk(
            string text,
            bool nullOk
            )
        {
            return IsItemSizeOk(text, nullOk, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if LIST_CACHE || PARSE_CACHE || COM_TYPE_CACHE
        public static bool IsItemWriteSizeOk(
            ICollection collection,
            bool nullOk
            )
        {
            return IsItemSizeOk(collection, nullOk, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        public static bool IsItemWriteSizeOk(
            Argument argument,
            bool nullOk
            )
        {
            return IsItemSizeOk(argument, nullOk, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        public static bool MaybeEnableOrDisable<TKey, TValue>(
            Interpreter interpreter,                       /* in */
            CacheDictionary<TKey, TValue> cacheDictionary, /* in */
            CacheFlags cacheFlags,                         /* in */
            CacheFlags allCacheFlags                       /* in */
            )
        {
            bool disabled = false;

            return MaybeEnableOrDisable<TKey, TValue>(
                interpreter, cacheDictionary, cacheFlags, allCacheFlags,
                ref disabled);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeEnableOrDisable<TKey, TValue>(
            Interpreter interpreter,                       /* in */
            CacheDictionary<TKey, TValue> cacheDictionary, /* in */
            CacheFlags cacheFlags,                         /* in */
            CacheFlags allCacheFlags,                      /* in */
            ref bool disabled                              /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (cacheDictionary == null)
                    return false;

                bool? maybeEnable = null;

                cacheDictionary.CheckForNoOrExcessChanges(
                    minimumChangeCount, maximumChangeCount, ref maybeEnable);

                if (maybeEnable != null)
                {
                    bool enable = (bool)maybeEnable;

                    bool areEnabled = interpreter.AreCachesEnabled(
                        cacheFlags);

                    if (enable != areEnabled)
                    {
                        CacheFlags extraCacheFlags = CacheFlags.None;

                        if (RecordMaybeEnableOrDisable(cacheFlags, enable))
                        {
                            //
                            // NOTE: *HACK* By design, there is no way to
                            //       automatically unlock the cache after
                            //       locking it.  That is because locking
                            //       the cache is being used as a way to
                            //       prevent the cache management itself
                            //       from consuming way too many system
                            //       resources.
                            //
                            extraCacheFlags |= CacheFlags.Lock;

                            //
                            // NOTE: When locking a particular cache, do
                            //       we want to disable it as well?  For
                            //       now, this behavior is disabled by
                            //       default.  If the cache in question
                            //       is already in the process of being
                            //       disabled, do nothing.
                            //
                            // TODO: Further investigate the performance
                            //       ramifications of this.
                            //
                            if (enable && FlagOps.HasFlags(
                                    allCacheFlags, CacheFlags.DisableOnLock,
                                    true))
                            {
                                enable = false;
                            }
                        }

                        extraCacheFlags |= GetExtraEnableCacheFlags(
                            cacheFlags, allCacheFlags, enable);

                        CacheFlags newCacheFlags = interpreter.EnableCaches(
                            cacheFlags | extraCacheFlags, enable);

                        TraceOps.DebugTrace(String.Format(
                            "MaybeEnableOrDisable: cacheFlags = {0}, " +
                            "allCacheFlags = {1}, extraCacheFlags = {2}, " +
                            "maybeEnable = {3}, enable = {4}, " +
                            "areEnabled = {5}, newCacheFlags = {6}",
                            FormatOps.WrapOrNull(cacheFlags),
                            FormatOps.WrapOrNull(allCacheFlags),
                            FormatOps.WrapOrNull(extraCacheFlags),
                            FormatOps.WrapOrNull(maybeEnable),
                            enable, areEnabled,
                            FormatOps.WrapOrNull(newCacheFlags)),
                            typeof(CacheConfiguration).Name,
                            TracePriority.EngineDebug);

                        if (!enable)
                            disabled = true;

                        if (FlagOps.HasFlags(newCacheFlags, cacheFlags, true))
                            return true;
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void TrimExcess<TKey, TValue>(
            CacheDictionary<TKey, TValue> cacheDictionary, /* in */
            CacheFlags cacheFlags,                         /* in */
            CacheFlags allCacheFlags,                      /* in */
            ref int trimCount,                             /* out */
            ref bool? maybeClear                           /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (cacheDictionary == null)
                {
                    maybeClear = null;
                    return;
                }

                int count = cacheDictionary.Count;

                if (count == 0)
                {
                    maybeClear = null;
                    return;
                }

#if NATIVE
                //
                // NOTE: Start clearing (i.e. instead of trimming) ALL
                //       caches if native system memory is starting to
                //       get "too full".  Also, reset the minimum usage
                //       count to the orignally configured value.
                //
                if (!IsClearMemoryLoadOk(cacheFlags))
                {
                    cacheDictionary.Clear();

                    minimumUsageCount = savedMinimumUsageCount;

                    maybeClear = true;
                    return;
                }

                //
                // NOTE: Figure out how many milliseconds it has been since
                //       we last bumped the minimum usage count, if ever.
                //
                double milliseconds = GetUsageCountMilliseconds();

                //
                // NOTE: If we have never bumped the minimum usage count or
                //       it has been longer than the configured time span,
                //       maybe do it again now.
                //
                if ((milliseconds == Milliseconds.Never) ||
                    (milliseconds > minimumUsageMilliseconds))
                {
                    //
                    // NOTE: Has the current memory load hit the point where
                    //       bumping the minimum usage count (i.e. for the
                    //       cached items to retain) needs to be considered?
                    //
                    if (!IsBumpMemoryLoadOk(cacheFlags))
                    {
                        //
                        // NOTE: Maybe start increasing the minimum usage
                        //       count [that is required to retain a cached
                        //       item].
                        //
                        MaybeChangeMinimumUsageCount<TKey, TValue>(
                            cacheDictionary);

                        //
                        // NOTE: The minimum usage count may have just been
                        //       changed; make sure it is not changed again
                        //       for at least X milliseconds.
                        //
                        TouchUsageCount();
                    }
                }
#endif

                if (trimPerformanceClientData != null)
                    trimPerformanceClientData.Start();

                try
                {
                    int possibleRemoveCount = 0;

                    cacheDictionary.TrimExcess(
                        minimumSize, maximumSize, minimumTrimItemCount,
                        maximumTrimItemCount, minimumUsageCount,
                        maximumUsageCount, ref possibleRemoveCount);

                    trimCount++;

                    CacheFlags extraCacheFlags = GetExtraTrimCacheFlags(
                        cacheFlags, allCacheFlags);

                    if (FlagOps.HasFlags(
                            extraCacheFlags, CacheFlags.ForceTrim, true))
                    {
                        if ((possibleRemoveCount > 0) &&
                            ((minimumTrimItemCount < 0) ||
                                (possibleRemoveCount > minimumTrimItemCount))
#if NATIVE
                            && !IsTrimMemoryLoadOk(cacheFlags)
#endif
                            )
                        {
                            possibleRemoveCount = 0;

                            cacheDictionary.TrimExcess(
                                minimumSize, maximumSize, Count.Invalid,
                                Count.Invalid, Count.Invalid, Count.Invalid,
                                ref possibleRemoveCount);

                            trimCount++;
                        }
                    }

                    maybeClear = false;
                    return;
                }
                finally
                {
                    if (trimPerformanceClientData != null)
                        trimPerformanceClientData.Stop();
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            bool empty = HostOps.HasEmptyContent(detailFlags);
            StringPairList localList = new StringPairList();

            ///////////////////////////////////////////////////////////////////

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (empty || initialized)
                {
                    localList.Add("Initialized",
                        initialized.ToString());
                }

                ///////////////////////////////////////////////////////////////

                int defaultLevel = GetDefaultLevel();

                if (empty || (defaultLevel >= 0))
                {
                    localList.Add("DefaultLevel",
                        defaultLevel.ToString());
                }

                ///////////////////////////////////////////////////////////////

                int maximumLevel = GetMaximumLevel();

                if (empty || (maximumLevel >= 0))
                {
                    localList.Add("MaximumLevel",
                        maximumLevel.ToString());
                }

                ///////////////////////////////////////////////////////////////

                string defaultText = GetDefaultSettings(defaultLevel);

                if (empty || (defaultText != null))
                {
                    localList.Add(String.Format(
                        "DefaultSettings[{0}]", defaultLevel),
                        FormatOps.DisplayString(defaultText));
                }

                ///////////////////////////////////////////////////////////////

                //
                // HACK: Skip adding the maximum settings if the maximum level
                //       is the same as the default level.  This may be fairly
                //       common for 64-bit machines.
                //
                string maximumText = GetDefaultSettings(maximumLevel);

                if (maximumLevel != defaultLevel)
                {
                    if (empty || (maximumText != null))
                    {
                        localList.Add(String.Format(
                            "DefaultSettings[{0}]", maximumLevel),
                            FormatOps.DisplayString(maximumText));
                    }
                }

                ///////////////////////////////////////////////////////////////

#if NATIVE
                if (empty || (badMemoryThreshold != 0))
                {
                    localList.Add("BadMemoryThreshold",
                        badMemoryThreshold.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (goodMemoryThreshold != 0))
                {
                    localList.Add("GoodMemoryThreshold",
                        goodMemoryThreshold.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (memoryLoadBounds[0] != 0))
                {
                    localList.Add("MemoryLoadBounds[0]",
                        memoryLoadBounds[0].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (memoryLoadBounds[1] != 0))
                {
                    localList.Add("MemoryLoadBounds[1]",
                        memoryLoadBounds[1].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || lowMemoryWarning[0])
                {
                    localList.Add("LowMemoryWarning[0]",
                        lowMemoryWarning[0].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || lowMemoryWarning[1])
                {
                    localList.Add("LowMemoryWarning[1]",
                        lowMemoryWarning[1].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || lowMemoryWarning[2])
                {
                    localList.Add("LowMemoryWarning[2]",
                        lowMemoryWarning[2].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || lowMemoryWarning[3])
                {
                    localList.Add("LowMemoryWarning[3]",
                        lowMemoryWarning[3].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || lowMemoryWarning[4])
                {
                    localList.Add("LowMemoryWarning[4]",
                        lowMemoryWarning[4].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || okMemoryWarning[0])
                {
                    localList.Add("OkMemoryWarning[0]",
                        okMemoryWarning[0].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || okMemoryWarning[1])
                {
                    localList.Add("OkMemoryWarning[1]",
                        okMemoryWarning[1].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || okMemoryWarning[2])
                {
                    localList.Add("OkMemoryWarning[2]",
                        okMemoryWarning[2].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || okMemoryWarning[3])
                {
                    localList.Add("OkMemoryWarning[3]",
                        okMemoryWarning[3].ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || okMemoryWarning[4])
                {
                    localList.Add("OkMemoryWarning[4]",
                        okMemoryWarning[4].ToString());
                }
#endif

                ///////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
                if (empty || (maybeDisableCounts != null))
                {
                    localList.Add("MaybeDisableCounts",
                        (maybeDisableCounts != null) ?
                        FormatOps.MaybeEnableOrDisable(
                            maybeDisableCounts, true) : null);
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maybeEnableCounts != null))
                {
                    localList.Add("MaybeEnableCounts",
                        (maybeEnableCounts != null) ?
                        FormatOps.MaybeEnableOrDisable(
                            maybeEnableCounts, true) : null);
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (trimPerformanceClientData != null))
                {
                    localList.Add("TrimPerformanceClientData",
                        (trimPerformanceClientData != null) ?
                            trimPerformanceClientData.ToString() : null);
                }
#endif

                ///////////////////////////////////////////////////////////////

                if (empty || readEnabled)
                {
                    localList.Add("ReadEnabled",
                        readEnabled.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || writeEnabled)
                {
                    localList.Add("WriteEnabled",
                        writeEnabled.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || deleteEnabled)
                {
                    localList.Add("DeleteEnabled",
                        deleteEnabled.ToString());
                }

                ///////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
                if (empty || accessedEnabled)
                {
                    localList.Add("AccessedEnabled",
                        accessedEnabled.ToString());
                }
#endif

                ///////////////////////////////////////////////////////////////

#if NATIVE
                if (empty || (maximumClearMemoryLoad > 0))
                {
                    localList.Add("MaximumClearMemoryLoad",
                        maximumClearMemoryLoad.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumWriteMemoryLoad > 0))
                {
                    localList.Add("MaximumWriteMemoryLoad",
                        maximumWriteMemoryLoad.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumTrimMemoryLoad > 0))
                {
                    localList.Add("MaximumTrimMemoryLoad",
                        maximumTrimMemoryLoad.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumBumpMemoryLoad > 0))
                {
                    localList.Add("MaximumBumpMemoryLoad",
                        maximumBumpMemoryLoad.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumCompactMemoryLoad > 0))
                {
                    localList.Add("MaximumCompactMemoryLoad",
                        maximumCompactMemoryLoad.ToString());
                }
#endif

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumReadTextItemSize > 0))
                {
                    localList.Add("MaximumReadTextItemSize",
                        maximumReadTextItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumReadTextItemSize > 0))
                {
                    localList.Add("MinimumReadTextItemSize",
                        minimumReadTextItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumWriteTextItemSize > 0))
                {
                    localList.Add("MaximumWriteTextItemSize",
                        maximumWriteTextItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumWriteTextItemSize > 0))
                {
                    localList.Add("MinimumWriteTextItemSize",
                        minimumWriteTextItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumReadListItemSize > 0))
                {
                    localList.Add("MaximumReadListItemSize",
                        maximumReadListItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumReadListItemSize > 0))
                {
                    localList.Add("MinimumReadListItemSize",
                        minimumReadListItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumWriteListItemSize > 0))
                {
                    localList.Add("MaximumWriteListItemSize",
                        maximumWriteListItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumWriteListItemSize > 0))
                {
                    localList.Add("MinimumWriteListItemSize",
                        minimumWriteListItemSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumSize > 0))
                {
                    localList.Add("MaximumSize",
                        maximumSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
                if (empty || (minimumSize > 0))
                {
                    localList.Add("MinimumSize",
                        minimumSize.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumTrimItemCount > 0))
                {
                    localList.Add("MaximumTrimItemCount",
                        maximumTrimItemCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumTrimItemCount > 0))
                {
                    localList.Add("MinimumTrimItemCount",
                        minimumTrimItemCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumTrimMilliseconds > 0))
                {
                    localList.Add("MinimumTrimMilliseconds",
                        minimumTrimMilliseconds.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumChangeCount > 0))
                {
                    localList.Add("MaximumChangeCount",
                        maximumChangeCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumChangeCount > 0))
                {
                    localList.Add("MinimumChangeCount",
                        minimumChangeCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumChangeMilliseconds > 0))
                {
                    localList.Add("MaximumChangeMilliseconds",
                        maximumChangeMilliseconds.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumChangeMilliseconds > 0))
                {
                    localList.Add("MinimumChangeMilliseconds",
                        minimumChangeMilliseconds.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumUsageCount > 0))
                {
                    localList.Add("MaximumUsageCount",
                        maximumUsageCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (minimumUsageCount > 0))
                {
                    localList.Add("MinimumUsageCount",
                        minimumUsageCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (savedMinimumUsageCount > 0))
                {
                    localList.Add("SavedMinimumUsageCount",
                        savedMinimumUsageCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

#if NATIVE
                if (empty || (minimumUsageMilliseconds > 0))
                {
                    localList.Add("MinimumUsageMilliseconds",
                        minimumUsageMilliseconds.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (lastUsageCount != null))
                {
                    localList.Add("LastUsageCount", (lastUsageCount != null) ?
                        FormatOps.Iso8601DateTime((DateTime)lastUsageCount) :
                        FormatOps.DisplayNull);
                }
#endif

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumMaybeDisableCount > 0))
                {
                    localList.Add("MaximumMaybeDisableCount",
                        maximumMaybeDisableCount.ToString());
                }

                ///////////////////////////////////////////////////////////////

                if (empty || (maximumMaybeEnableCount > 0))
                {
                    localList.Add("MaximumMaybeEnableCount",
                        maximumMaybeEnableCount.ToString());
                }
#endif

                ///////////////////////////////////////////////////////////////

                if (localList.Count > 0)
                {
                    list.MaybeAddNull();
                    list.Add("Cache Settings");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion
    }
}
