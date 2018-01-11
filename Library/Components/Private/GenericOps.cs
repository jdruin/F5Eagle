/*
 * GenericOps.cs --
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
using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    #region GenericCompareOps<T> Class
    [ObjectId("a7570998-6e18-4ac0-aac7-87000158c37a")]
    internal static class GenericCompareOps<T> where T : IComparable<T>
    {
        #region Private Constants
#if ARGUMENT_CACHE
        private static readonly int DefaultHashCode = 0;
        private static readonly int InvalidHashCode = Length.Invalid;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Equals(
            T[] array1,
            T[] array2,
            int length
            )
        {
            int compare = 0;

            return Equals(array1, array2, length, ref compare);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Equals(
            T[] array1,
            T[] array2,
            int length,
            ref int compare
            )
        {
            int failIndex = Index.Invalid;

            return Equals(array1, array2, length, ref compare, ref failIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Equals(
            T[] array1,
            T[] array2,
            int length,
            ref int compare,
            ref int failIndex
            )
        {
            if ((array1 == null) && (array2 == null))
                return true;

            if ((array1 == null) || (array2 == null))
                return false;

            int localLength;

            if (length < 0)
            {
                //
                // NOTE: Use "automatic" handling.  Arrays must be exactly
                //       the same size.
                //
                localLength = array1.Length;

                if (localLength != array2.Length)
                    return false;
            }
            else if (length == 0)
            {
                //
                // NOTE: Yes, I guess that zero bytes are equal.
                //
                return true;
            }
            else if ((length > array1.Length) || (length > array2.Length))
            {
                //
                // NOTE: Using prefix handling; however, both arrays must
                //       have at least the specified number of bytes to
                //       compare.
                //
                return false;
            }
            else
            {
                //
                // NOTE: Using prefix handling and both arrays do have at
                //       least the specified number of bytes to compare;
                //       therefore, use that to limit the loop.
                //
                localLength = length;
            }

            for (int index = 0; index < localLength; index++)
            {
                T element1 = array1[index];
                T element2 = array2[index];

                if ((element1 != null) && (element2 != null))
                {
                    int localCompare = element1.CompareTo(element2);

                    if (localCompare != 0)
                    {
                        compare = localCompare;
                        failIndex = index;

                        return false;
                    }
                }
                else if (element1 != null)
                {
                    compare = 1; // element1 is greater than null.
                    failIndex = index;

                    return false;
                }
                else if (element2 != null)
                {
                    compare = -1; // element2 is greater than null.
                    failIndex = index;

                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        public static int GetHashCode(
            T[] array,
            int length
            )
        {
            int result = DefaultHashCode;

            if (array == null)
                return result;

            int localLength;

            if (length < 0)
            {
                //
                // NOTE: Ok, hash all elements.
                //
                localLength = array.Length;
            }
            else if (length == 0)
            {
                //
                // NOTE: Ok, hash zero elements.
                //
                return result;
            }
            else if (length > array.Length)
            {
                //
                // NOTE: Error, not enough elements.
                //
                return InvalidHashCode;
            }
            else
            {
                //
                // NOTE: Ok, hash exactly X elements.
                //
                localLength = length;
            }

            for (int index = 0; index < length; index++)
            {
                T element = array[index];

                if (element == null)
                    continue;

                result ^= element.GetHashCode();
            }

            return result;
        }
#endif
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region GenericOps<T> Class
    [ObjectId("7f613001-c787-4e55-acde-eeb35834d5d0")]
    internal static class GenericOps<T>
    {
        #region Private Data
#if NATIVE && NATIVE_UTILITY
        internal static bool UseNativeJoinList = true;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        public static T PopFirstArgument(
            ref T[] array
            )
        {
            if (array != null)
            {
                int length = array.Length;

                if (length > 0)
                {
                    length--; /* one less element */

                    T result = array[0]; /* extract first element */

                    if (length > 0)
                    {
                        /* new length is one less */
                        T[] newArray = new T[length];

                        /* copy array, skip first element */
                        /* length has already been adjusted down */
                        Array.Copy(array, 1, newArray, 0, length);

                        /* replace original array */
                        array = newArray;
                    }
                    else
                    {
                        /* no arguments left */
                        array = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static T PopFirstArgument(
            ref IList<T> list
            )
        {
            if (list != null)
            {
                //
                // WARNING: This method assumes that the list type has a
                //          constructor with exactly one integer [capacity]
                //          argument.
                //
                Type type = list.GetType();
                int count = list.Count;

                if (count > 0)
                {
                    count--; /* one less element */

                    T result = list[0]; /* extract first element */

                    if (count > 0)
                    {
                        /* new count is one less */
                        IList<T> newList = Activator.CreateInstance(
                            type, new object[] { count }) as IList<T>;

                        /* copy list, skip first element */
                        /* count has already been adjusted down */
                        for (int index = 1; index < count + 1; index++)
                            newList.Add(list[index]);

                        /* replace original array */
                        list = newList;
                    }
                    else
                    {
                        /* no arguments left */
                        list = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static T PopLastArgument(
            ref T[] array
            )
        {
            if (array != null)
            {
                int length = array.Length;

                if (length > 0)
                {
                    length--; /* one less element */

                    T result = array[length]; /* extract last element */

                    if (length > 0)
                    {
                        /* new length is one less */
                        T[] newArray = new T[length];

                        /* copy array, skip last element */
                        /* length has already been adjusted down */
                        Array.Copy(array, 0, newArray, 0, length);

                        /* replace original array */
                        array = newArray;
                    }
                    else
                    {
                        /* no arguments left */
                        array = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static T PopLastArgument(
            ref IList<T> list
            )
        {
            if (list != null)
            {
                //
                // WARNING: This method assumes that the list type has a
                //          constructor with exactly one integer [capacity]
                //          argument.
                //
                Type type = list.GetType();
                int count = list.Count;

                if (count > 0)
                {
                    count--; /* one less element */

                    T result = list[count]; /* extract last element */

                    if (count > 0)
                    {
                        /* new count is one less */
                        IList<T> newList = Activator.CreateInstance(
                            type, new object[] { count }) as IList<T>;

                        /* copy list, skip last element */
                        /* count has already been adjusted down */
                        for (int index = 0; index < count; index++)
                            newList.Add(list[index]);

                        /* replace original array */
                        list = newList;
                    }
                    else
                    {
                        /* no arguments left */
                        list = null;
                    }

                    return result;
                }
            }

            return default(T);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool EqualityComparerEquals(
            IEqualityComparer<T> equalityComparer,
            T left,
            T right
            )
        {
            return (equalityComparer != null) ?
                equalityComparer.Equals(left, right) : Equals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int EqualityComparerGetHashCode(
            IEqualityComparer<T> equalityComparer,
            T value
            )
        {
            return (equalityComparer != null) ?
                equalityComparer.GetHashCode(value) : GetHashCode(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Equals(
            T left,
            T right
            )
        {
            if ((left != null) && (right != null))
                return left.Equals(right);
            else
                return (left == null) && (right == null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetHashCode(
            T value
            )
        {
            return (value != null) ? value.GetHashCode() : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ToString(
            T value,
            string @default
            )
        {
            return (value != null) ? value.ToString() : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method should exactly mimic the List<T>.AddRange()
        //       method.
        //
        public static void AddRange(
            IList<T> list,
            IEnumerable<T> collection
            )
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (collection == null)
                throw new ArgumentNullException("collection");

            foreach (T item in collection)
                list.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringList ToStringList(
            IList<T> list,
            bool skipNull,
            bool skipEmpty
            )
        {
            if (list == null)
                return null;

            StringList result = new StringList(list.Count);

            foreach (T element in list)
            {
                if (skipNull && (element == null))
                    continue;

                string value = (element != null) ? element.ToString() : null;

                if (skipEmpty && String.IsNullOrEmpty(value))
                    continue;

                result.Add(value);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IDictionary<T, T> dictionary,
            string separator,
            string prefix,
            string suffix
            )
        {
            IList<T> list = (dictionary != null) ? new List<T>(dictionary.Keys) : null;

            return ListToEnglish(list, separator, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IDictionary<T, T> dictionary,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            IList<T> list = (dictionary != null) ? new List<T>(dictionary.Keys) : null;

            return ListToEnglish(list, separator, prefix, suffix, valuePrefix, valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IList<T> list,
            string separator,
            string prefix,
            string suffix
            )
        {
            return ListToEnglish(list, separator, prefix, suffix, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IList<T> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();
            StringList localList = ToStringList(list, true, true);

            if (localList != null)
            {
                int count = localList.Count;

                if (count > 0)
                {
                    bool havePrefix = !String.IsNullOrEmpty(prefix);

                    bool haveSeparator = !String.IsNullOrEmpty(
                        separator);

                    bool haveSuffix = !String.IsNullOrEmpty(suffix);

                    bool haveValuePrefix = !String.IsNullOrEmpty(
                        valuePrefix);

                    bool haveValueSuffix = !String.IsNullOrEmpty(
                        valueSuffix);

                    for (int index = 0; index < count; index++)
                    {
                        string value = localList[index];
                        bool usedSeparator = false;

                        if ((index > 0) && (count > 2) &&
                            haveSeparator)
                        {
                            result.Append(separator);
                            usedSeparator = true;
                        }

                        if ((index == (count - 1)) &&
                            (count > 1) && haveSuffix)
                        {
                            if (havePrefix && !usedSeparator)
                                result.Append(prefix);

                            result.Append(suffix);
                        }

                        if (haveValuePrefix)
                            result.Append(valuePrefix);

                        result.Append(value);

                        if (haveValueSuffix)
                            result.Append(valueSuffix);
                    }
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FilterList(
            IList<T> inputList,
            IList<T> outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string regExPattern,
            RegexOptions regExOptions,
            ref Result error
            )
        {
            if (inputList != null)
            {
                if (outputList != null)
                {
                    //
                    // BUGFIX: Skip doing anything if the input list is empty.
                    //
                    if (inputList.Count > 0)
                    {
                        if (ListOps.CheckStartAndStopIndex(0, inputList.Count - 1,
                                ref startIndex, ref stopIndex, ref error))
                        {
                            Regex regEx = null;

                            try
                            {
                                if (regExPattern != null)
                                    regEx = new Regex(regExPattern, regExOptions); /* throw */
                            }
                            catch
                            {
                                // do nothing.
                            }

                            if ((regExPattern == null) || (regEx != null))
                            {
                                for (int index = startIndex; index <= stopIndex; index++)
                                {
                                    string element;

                                    if (inputList[index] != null)
                                    {
                                        if (toStringFlags != ToStringFlags.None)
                                        {
                                            IToString toString = inputList[index] as IToString;

                                            if (toString != null)
                                                element = toString.ToString(toStringFlags);
                                            else
                                                element = inputList[index].ToString();
                                        }
                                        else
                                        {
                                            element = inputList[index].ToString();
                                        }
                                    }
                                    else
                                    {
                                        element = String.Empty;
                                    }

                                    //
                                    // NOTE: Match the string representation and add the original
                                    //       element and not the string representation because
                                    //       this is a generic list, not a StringList.
                                    //
                                    if ((regEx == null) || regEx.IsMatch(element))
                                        outputList.Add(inputList[index]);
                                }

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "invalid regular expression";
                            }
                        }
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    error = "invalid output list";
                }
            }
            else
            {
                error = "invalid input list";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FilterList(
            IList<T> inputList,
            IList<T> outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string pattern,
            bool noCase,
            ref Result error
            )
        {
            if (inputList != null)
            {
                if (outputList != null)
                {
                    //
                    // BUGFIX: Skip doing anything if the input list is empty.
                    //
                    if (inputList.Count > 0)
                    {
                        if (ListOps.CheckStartAndStopIndex(0, inputList.Count - 1,
                                ref startIndex, ref stopIndex, ref error))
                        {
                            for (int index = startIndex; index <= stopIndex; index++)
                            {
                                string element;

                                if (inputList[index] != null)
                                {
                                    if (toStringFlags != ToStringFlags.None)
                                    {
                                        IToString toString = inputList[index] as IToString;

                                        if (toString != null)
                                            element = toString.ToString(toStringFlags);
                                        else
                                            element = inputList[index].ToString();
                                    }
                                    else
                                    {
                                        element = inputList[index].ToString();
                                    }
                                }
                                else
                                {
                                    element = String.Empty;
                                }

                                //
                                // NOTE: Match the string representation and add the original
                                //       element and not the string representation because
                                //       this is a generic list, not a StringList.
                                //
                                if ((pattern == null) ||
                                    StringOps.Match(null, StringOps.DefaultMatchMode, element, pattern, noCase))
                                {
                                    outputList.Add(inputList[index]);
                                }
                            }

                            return ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    error = "invalid output list";
                }
            }
            else
            {
                error = "invalid input list";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && NATIVE_UTILITY
        private static ReturnCode FilterList(
            IList<T> inputList,
            StringList outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string regExPattern,
            RegexOptions regExOptions,
            ref Result error
            )
        {
            if (inputList != null)
            {
                if (outputList != null)
                {
                    //
                    // BUGFIX: Skip doing anything if the input list is empty.
                    //
                    if (inputList.Count > 0)
                    {
                        if (ListOps.CheckStartAndStopIndex(0, inputList.Count - 1,
                                ref startIndex, ref stopIndex, ref error))
                        {
                            Regex regEx = null;

                            try
                            {
                                if (regExPattern != null)
                                    regEx = new Regex(regExPattern, regExOptions); /* throw */
                            }
                            catch
                            {
                                // do nothing.
                            }

                            if ((regExPattern == null) || (regEx != null))
                            {
                                for (int index = startIndex; index <= stopIndex; index++)
                                {
                                    string element;

                                    if (inputList[index] != null)
                                    {
                                        if (toStringFlags != ToStringFlags.None)
                                        {
                                            IToString toString = inputList[index] as IToString;

                                            if (toString != null)
                                                element = toString.ToString(toStringFlags);
                                            else
                                                element = inputList[index].ToString();
                                        }
                                        else
                                        {
                                            element = inputList[index].ToString();
                                        }
                                    }
                                    else
                                    {
                                        element = String.Empty;
                                    }

                                    //
                                    // NOTE: Match the string representation and add the original
                                    //       element and not the string representation because
                                    //       this is a generic list, not a StringList.
                                    //
                                    if ((regEx == null) || regEx.IsMatch(element))
                                        outputList.Add(element);
                                }

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "invalid regular expression";
                            }
                        }
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    error = "invalid output list";
                }
            }
            else
            {
                error = "invalid input list";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
            if (UseNativeJoinList && Parser.IsListSeparator(separator))
            {
                bool locked = false;

                try
                {
                    //
                    // BUGFIX: *DEADLOCK* Prevent deadlocks here by using
                    //         the TryLock pattern.
                    //
                    NativeUtility.TryLock(ref locked); /* TRANSACTIONAL */

                    if (locked && NativeUtility.IsAvailable(null))
                    {
                        if (list == null)
                            return String.Empty;

                        StringList localList;
                        ReturnCode code;
                        Result error = null;

                        if ((startIndex >= 0) || (stopIndex >= 0) ||
                            (toStringFlags != ToStringFlags.None) ||
                            (regExPattern != null))
                        {
                            localList = new StringList(list.Count);

                            code = FilterList(
                                list, localList, startIndex, stopIndex,
                                toStringFlags, regExPattern, regExOptions,
                                ref error);

                            if (code != ReturnCode.Ok)
                            {
                                DebugOps.Complain(code, error);
                                goto managedFallback;
                            }
                        }
#if !MONO_BUILD
                        //
                        // HACK: *MONO* The Mono C# compiler cannot handle
                        //       this block of code.  It gives the following
                        //       warnings:
                        //
                        //       warning CS0184: The given expression is
                        //       never of the provided
                        //       (`Eagle._Containers.Public.StringList')
                        //       type
                        //
                        //       warning CS0162: Unreachable code detected
                        //
                        else if (list is StringList)
                        {
                            localList = list as StringList;
                        }
#endif
                        else
                        {
                            localList = new StringList(list);
                        }

                        string text = null;

                        code = NativeUtility.JoinList(
                            localList, ref text, ref error);

                        if (code != ReturnCode.Ok)
                        {
                            DebugOps.Complain(code, error);
                            goto managedFallback;
                        }

                        return text;
                    }
                    else if (!locked)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "ListToString ({0}): unable to acquire native utility lock",
                            MatchMode.RegExp), typeof(GenericOps<T>).Name,
                            TracePriority.LockError);
                    }
                }
                finally
                {
                    NativeUtility.ExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

        managedFallback:

            return ManagedListToString(
                list, startIndex, stopIndex, toStringFlags, separator,
                regExPattern, regExOptions);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && NATIVE_UTILITY
        public static string ManagedListToString(
#else
        public static string ListToString(
#endif
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (list != null)
            {
                if (ListOps.CheckStartAndStopIndex(0, list.Count - 1, ref startIndex, ref stopIndex))
                {
                    Regex regEx = null;

                    if (regExPattern != null)
                        regEx = new Regex(regExPattern, regExOptions); /* throw */

                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        string element;

                        if (list[index] != null)
                        {
                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = list[index] as IToString;

                                if (toString != null)
                                    element = toString.ToString(toStringFlags);
                                else
                                    element = list[index].ToString();
                            }
                            else
                            {
                                element = list[index].ToString();
                            }
                        }
                        else
                        {
                            element = String.Empty;
                        }

                        if ((regEx == null) || regEx.IsMatch(element))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None : ListElementFlags.DontQuoteHash;

                            Parser.ScanElement(/* null, */ element, 0, element.Length, ref flags);

                            if ((result.Length > 0) && (!String.IsNullOrEmpty(separator)))
                                result.Append(separator);

                            Parser.ConvertElement(/* null, */ element, 0, element.Length, flags, ref result);
                        }
                    }
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && NATIVE_UTILITY
        private static ReturnCode FilterList(
            IList<T> inputList,
            StringList outputList,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string pattern,
            bool noCase,
            ref Result error
            )
        {
            if (inputList != null)
            {
                if (outputList != null)
                {
                    //
                    // BUGFIX: Skip doing anything if the input list is empty.
                    //
                    if (inputList.Count > 0)
                    {
                        if (ListOps.CheckStartAndStopIndex(0, inputList.Count - 1,
                                ref startIndex, ref stopIndex, ref error))
                        {
                            for (int index = startIndex; index <= stopIndex; index++)
                            {
                                string element;

                                if (inputList[index] != null)
                                {
                                    if (toStringFlags != ToStringFlags.None)
                                    {
                                        IToString toString = inputList[index] as IToString;

                                        if (toString != null)
                                            element = toString.ToString(toStringFlags);
                                        else
                                            element = inputList[index].ToString();
                                    }
                                    else
                                    {
                                        element = inputList[index].ToString();
                                    }
                                }
                                else
                                {
                                    element = String.Empty;
                                }

                                //
                                // NOTE: Match the string representation and add the original
                                //       element and not the string representation because
                                //       this is a generic list, not a StringList.
                                //
                                if ((pattern == null) ||
                                    StringOps.Match(null, StringOps.DefaultMatchMode, element, pattern, noCase))
                                {
                                    outputList.Add(element);
                                }
                            }

                            return ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    error = "invalid output list";
                }
            }
            else
            {
                error = "invalid input list";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            if (UseNativeJoinList && Parser.IsListSeparator(separator))
            {
                bool locked = false;

                try
                {
                    //
                    // BUGFIX: *DEADLOCK* Prevent deadlocks here by using
                    //         the TryLock pattern.
                    //
                    NativeUtility.TryLock(ref locked); /* TRANSACTIONAL */

                    if (locked && NativeUtility.IsAvailable(null))
                    {
                        if (list == null)
                            return String.Empty;

                        StringList localList;
                        ReturnCode code;
                        Result error = null;

                        if ((startIndex >= 0) || (stopIndex >= 0) ||
                            (toStringFlags != ToStringFlags.None) ||
                            (pattern != null))
                        {
                            localList = new StringList(list.Count);

                            code = FilterList(
                                list, localList, startIndex, stopIndex,
                                toStringFlags, pattern, noCase,
                                ref error);

                            if (code != ReturnCode.Ok)
                            {
                                DebugOps.Complain(code, error);
                                goto managedFallback;
                            }
                        }
#if !MONO_BUILD
                        //
                        // HACK: *MONO* The Mono C# compiler cannot handle
                        //       this block of code.  It gives the following
                        //       warnings:
                        //
                        //       warning CS0184: The given expression is
                        //       never of the provided
                        //       (`Eagle._Containers.Public.StringList')
                        //       type
                        //
                        //       warning CS0162: Unreachable code detected
                        //
                        else if (list is StringList)
                        {
                            localList = list as StringList;
                        }
#endif
                        else
                        {
                            localList = new StringList(list);
                        }

                        string text = null;

                        code = NativeUtility.JoinList(
                            localList, ref text, ref error);

                        if (code != ReturnCode.Ok)
                        {
                            DebugOps.Complain(code, error);
                            goto managedFallback;
                        }

                        return text;
                    }
                    else if (!locked)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "ListToString ({0}): unable to acquire native utility lock",
                            MatchMode.Glob), typeof(GenericOps<T>).Name,
                            TracePriority.LockError);
                    }
                }
                finally
                {
                    NativeUtility.ExitLock(ref locked);
                }
            }

        managedFallback:

            return ManagedListToString(
                list, startIndex, stopIndex, toStringFlags, separator,
                pattern, noCase);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && NATIVE_UTILITY
        public static string ManagedListToString(
#else
        public static string ListToString(
#endif
            IList<T> list,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (list != null)
            {
                if (ListOps.CheckStartAndStopIndex(0, list.Count - 1, ref startIndex, ref stopIndex))
                {
                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        string element;

                        if (list[index] != null)
                        {
                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = list[index] as IToString;

                                if (toString != null)
                                    element = toString.ToString(toStringFlags);
                                else
                                    element = list[index].ToString();
                            }
                            else
                            {
                                element = list[index].ToString();
                            }
                        }
                        else
                        {
                            element = String.Empty;
                        }

                        if ((pattern == null) ||
                            StringOps.Match(null, StringOps.DefaultMatchMode, element, pattern, noCase))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None : ListElementFlags.DontQuoteHash;

                            Parser.ScanElement(/* null, */ element, 0, element.Length, ref flags);

                            if ((result.Length > 0) && (!String.IsNullOrEmpty(separator)))
                                result.Append(separator);

                            Parser.ConvertElement(/* null, */ element, 0, element.Length, flags, ref result);
                        }
                    }
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IEnumerable<T> list,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (list != null)
            {
                bool once = false;

                foreach (T element in list)
                {
                    string value;

                    if (element != null)
                    {
                        if (toStringFlags != ToStringFlags.None)
                        {
                            IToString toString = element as IToString;

                            if (toString != null)
                                value = toString.ToString(toStringFlags);
                            else
                                value = element.ToString();
                        }
                        else
                        {
                            value = element.ToString();
                        }
                    }
                    else
                    {
                        value = String.Empty;
                    }

                    if ((pattern == null) ||
                        StringOps.Match(null, StringOps.DefaultMatchMode, value, pattern, noCase))
                    {
                        ListElementFlags flags = once ?
                            ListElementFlags.None : ListElementFlags.DontQuoteHash;

                        once = true;

                        Parser.ScanElement(/* null, */ value, 0, value.Length, ref flags);

                        if ((result.Length > 0) && (!String.IsNullOrEmpty(separator)))
                            result.Append(separator);

                        Parser.ConvertElement(/* null, */ value, 0, value.Length, flags, ref result);
                    }
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IEnumerable<T> list,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (list != null)
            {
                Regex regEx = null;

                if (regExPattern != null)
                    regEx = new Regex(regExPattern, regExOptions); /* throw */

                bool once = false;

                foreach (T element in list)
                {
                    string value;

                    if (element != null)
                    {
                        if (toStringFlags != ToStringFlags.None)
                        {
                            IToString toString = element as IToString;

                            if (toString != null)
                                value = toString.ToString(toStringFlags);
                            else
                                value = element.ToString();
                        }
                        else
                        {
                            value = element.ToString();
                        }
                    }
                    else
                    {
                        value = String.Empty;
                    }

                    if ((regEx == null) || regEx.IsMatch(value))
                    {
                        ListElementFlags flags = once ?
                            ListElementFlags.None : ListElementFlags.DontQuoteHash;

                        once = true;

                        Parser.ScanElement(/* null, */ value, 0, value.Length, ref flags);

                        if ((result.Length > 0) && (!String.IsNullOrEmpty(separator)))
                            result.Append(separator);

                        Parser.ConvertElement(/* null, */ value, 0, value.Length, flags, ref result);
                    }
                }
            }

            return result.ToString();
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region GenericOps<T1, T2> Class
    [ObjectId("58b15ba6-0517-4179-ac20-5e63efae31f3")]
    internal static class GenericOps<T1, T2>
    {
        public static IStringList Combine(
            bool pairs,
            bool keys,
            bool values,
            params IDictionary<T1, T2>[] dictionaries
            )
        {
            IStringList list = pairs ?
                (IStringList)new StringPairList() : new StringList();

            foreach (IDictionary<T1, T2> dictionary in dictionaries)
            {
                if (dictionary == null)
                    continue;

                list.Add(KeysAndValues(
                    dictionary, pairs, keys, values, MatchMode.None,
                    null, null, null, null, null, false), 0);
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TryGetKeyAtIndex(
            IDictionary<T1, T2> dictionary,
            int index,
            ref T1 key
            )
        {
            bool result = false;

            if (dictionary != null)
            {
                List<T1> keys = new List<T1>(dictionary.Keys);

                if (keys != null)
                {
                    if ((index >= 0) && (index < keys.Count))
                    {
                        key = keys[index];
                        result = true;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TryGetValueAtIndex(
            IDictionary<T1, T2> dictionary,
            int index,
            ref T2 value
            )
        {
            bool result = false;

            if (dictionary != null)
            {
                List<T1> keys = new List<T1>(dictionary.Keys);

                if (keys != null)
                {
                    if ((index >= 0) && (index < keys.Count))
                    {
                        value = dictionary[keys[index]];
                        result = true;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IStringList KeysAndValues(
            IDictionary<T1, T2> dictionary,
            bool pairs,
            bool keys,
            bool values,
            MatchMode mode,
            string keyPattern,
            string valuePattern,
            string keyFormat,
            string valueFormat,
            IFormatProvider formatProvider,
            bool noCase
            )
        {
            return KeysAndValues(
                dictionary, pairs, keys, values, mode, keyPattern,
                valuePattern, keyFormat, valueFormat,
                formatProvider, noCase, RegexOptions.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IStringList KeysAndValues(
            IDictionary<T1, T2> dictionary,
            bool pairs,
            bool keys,
            bool values,
            MatchMode mode,
            string keyPattern,
            string valuePattern,
            string keyFormat,
            string valueFormat,
            IFormatProvider formatProvider,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            if (dictionary != null)
            {
                IStringList list = pairs ?
                    (IStringList)new StringPairList() : new StringList();

                foreach (KeyValuePair<T1, T2> pair in dictionary)
                {
                    //
                    // NOTE: Assume there will be a match and then attempt to
                    //       prove otherwise.
                    //
                    bool match = true;

                    ///////////////////////////////////////////////////////////////////////////////////

                    string keyString;

                    if (match) /* REDUNDANT: In case code moves around. */
                    {
                        object key = pair.Key;

                        if (key != null)
                        {
                            //
                            // NOTE: Has a custom format been specified by the
                            //       caller for the key(s)?
                            //
                            if (keyFormat != null)
                            {
                                //
                                // NOTE: Attempt to treat the key as a formattable
                                //       object.  If we do not succeed, simply use
                                //       normal ToString.
                                //
                                IFormattable formattable = pair.Key as IFormattable;

                                if (formattable != null)
                                    keyString = formattable.ToString(
                                        keyFormat, formatProvider);
                                else
                                    keyString = key.ToString();
                            }
                            else
                            {
                                //
                                // NOTE: No custom formatting for the key, just
                                //       ToString it.
                                //
                                keyString = key.ToString();
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Nothing much we can do here, the key is null;
                            //       therefore, the key string is null.
                            //
                            keyString = null;
                        }

                        //
                        // NOTE: Do we need to match against the key pattern, if any?
                        //       If the key pattern is null, we match everything.
                        //
                        if (keyPattern != null)
                        {
                            match = StringOps.Match(
                                null, mode, keyString, keyPattern, noCase,
                                null, regExOptions);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: No need to get a key string for this key, there is
                        //       no match.
                        //
                        keyString = null;
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    string valueString;

                    if (match)
                    {
                        object value = pair.Value;

                        if (value != null)
                        {
                            //
                            // NOTE: Has a custom format been specified by the
                            //       caller for the value(s)?
                            //
                            if (valueFormat != null)
                            {
                                //
                                // NOTE: Attempt to treat the value as a formattable
                                //       object.  If we do not succeed, simply use
                                //       normal ToString.
                                //
                                IFormattable formattable = pair.Value as IFormattable;

                                if (formattable != null)
                                    valueString = formattable.ToString(
                                        valueFormat, formatProvider);
                                else
                                    valueString = value.ToString();
                            }
                            else
                            {
                                //
                                // NOTE: No custom formatting for the value, just
                                //       ToString it.
                                //
                                valueString = value.ToString();
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Nothing much we can do here, the value is null;
                            //       therefore, the value string is null.
                            //
                            valueString = null;
                        }

                        //
                        // NOTE: Do we need to match against the value pattern, if any?
                        //       If the value pattern is null, we match everything.
                        //
                        if (valuePattern != null)
                        {
                            match = StringOps.Match(
                                null, mode, valueString, valuePattern, noCase,
                                null, regExOptions);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: No need to get a value string for this value, there is
                        //       no match.
                        //
                        valueString = null;
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    //
                    // NOTE: Did we match the key and/or value strings using the
                    //       selected matching mode, pattern(s), and format(s)?
                    //
                    if (match)
                    {
                        //
                        // NOTE: Do the want the corresponding values as well
                        //       as the keys?
                        //
                        if (keys && values)
                            list.Add(keyString, valueString);
                        else if (keys)
                            list.Add(keyString);
                        else if (values)
                            list.Add(valueString);
                    }
                }

                return list;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode KeysAndValues(
            IDictionary<T1, T2> dictionary,
            bool keys,
            bool values,
            MatchMode mode,
            string pattern,
            string keyFormat,
            string valueFormat,
            IFormatProvider formatProvider,
            bool matchKey,
            bool matchValue,
            bool noCase,
            RegexOptions regExOptions,
            ref Result result
            )
        {
            if (dictionary == null)
            {
                result = "invalid dictionary";
                return ReturnCode.Error;
            }

            if (!keys && !values)
            {
                result = String.Empty;
                return ReturnCode.Ok;
            }

            StringDictionary localDictionary = (keys && values) ?
                new StringDictionary() : null;

            StringList localList = (localDictionary == null) ?
                new StringList() : null;

            foreach (KeyValuePair<T1, T2> pair in dictionary)
            {
                //
                // NOTE: Assume there will be a match and then attempt to
                //       prove otherwise.
                //
                bool match = true;

                ///////////////////////////////////////////////////////////////////////////////////

                string keyString;

                if (match) /* REDUNDANT: In case code moves around. */
                {
                    object key = pair.Key;

                    if (key != null)
                    {
                        //
                        // NOTE: Has a custom format been specified by the
                        //       caller for the key(s)?
                        //
                        if (keyFormat != null)
                        {
                            //
                            // NOTE: Attempt to treat the key as a formattable
                            //       object.  If we do not succeed, simply use
                            //       normal ToString.
                            //
                            IFormattable formattable = pair.Key as IFormattable;

                            if (formattable != null)
                                keyString = formattable.ToString(
                                    keyFormat, formatProvider);
                            else
                                keyString = key.ToString();
                        }
                        else
                        {
                            //
                            // NOTE: No custom formatting for the key, just
                            //       ToString it.
                            //
                            keyString = key.ToString();
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Nothing much we can do here, the key is null;
                        //       therefore, the key string is null.
                        //
                        keyString = null;
                    }
                }
                else
                {
                    //
                    // NOTE: No need to get a key string for this key, there is
                    //       no match.
                    //
                    keyString = null;
                }

                ///////////////////////////////////////////////////////////////////////////////////

                string valueString;

                if (match)
                {
                    object value = pair.Value;

                    if (value != null)
                    {
                        //
                        // NOTE: Has a custom format been specified by the
                        //       caller for the value(s)?
                        //
                        if (valueFormat != null)
                        {
                            //
                            // NOTE: Attempt to treat the value as a formattable
                            //       object.  If we do not succeed, simply use
                            //       normal ToString.
                            //
                            IFormattable formattable = pair.Value as IFormattable;

                            if (formattable != null)
                                valueString = formattable.ToString(
                                    valueFormat, formatProvider);
                            else
                                valueString = value.ToString();
                        }
                        else
                        {
                            //
                            // NOTE: No custom formatting for the value, just
                            //       ToString it.
                            //
                            valueString = value.ToString();
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Nothing much we can do here, the value is null;
                        //       therefore, the value string is null.
                        //
                        valueString = null;
                    }
                }
                else
                {
                    //
                    // NOTE: No need to get a value string for this value, there is
                    //       no match.
                    //
                    valueString = null;
                }

                ///////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: Do we need to match against the a pattern, if any?
                //       If the pattern is null, we match everything.
                //
                if (pattern != null)
                {
                    string matchString;

                    if (matchKey)
                    {
                        if (matchValue)
                        {
                            matchString = String.Format(
                                "{0} {1}", keyString, valueString);
                        }
                        else
                        {
                            matchString = keyString;
                        }
                    }
                    else if (matchValue)
                    {
                        matchString = valueString;
                    }
                    else
                    {
                        matchString = null;
                    }

                    if (matchString != null)
                    {
                        match = StringOps.Match(
                            null, mode, matchString, pattern, noCase, null,
                            regExOptions);
                    }
                    else
                    {
                        //
                        // NOTE: Nothing to match, just skip it.
                        //
                        match = false;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////

                //
                // NOTE: Did we match the key and/or value strings using the
                //       selected matching mode, pattern(s), and format(s)?
                //
                if (match)
                {
                    //
                    // NOTE: Do the want the corresponding values as well
                    //       as the keys?
                    //
                    if (localDictionary != null)
                    {
                        if (keyString != null)
                            localDictionary.Add(keyString, valueString);
                    }
                    else if (localList != null)
                    {
                        if (keys)
                            localList.Add(keyString);

                        if (values)
                            localList.Add(valueString);
                    }
                }
            }

            if (localDictionary != null)
                result = localDictionary;
            else
                result = localList;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IDictionary<T1, T2> dictionary,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string regExPattern,
            RegexOptions regExOptions
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (dictionary != null)
            {
                List<T1> list = new List<T1>(dictionary.Keys);

                if (ListOps.CheckStartAndStopIndex(0, list.Count - 1, ref startIndex, ref stopIndex))
                {
                    Regex regEx = null;

                    if (regExPattern != null)
                        regEx = new Regex(regExPattern, regExOptions); /* throw */

                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        string element;

                        if (list[index] != null)
                        {
                            string keyElement;

                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = list[index] as IToString;

                                if (toString != null)
                                    keyElement = toString.ToString(toStringFlags);
                                else
                                    keyElement = list[index].ToString();
                            }
                            else
                            {
                                keyElement = list[index].ToString();
                            }

                            string valueElement;

                            if (dictionary[list[index]] != null)
                            {
                                if (toStringFlags != ToStringFlags.None)
                                {
                                    IToString toString = dictionary[list[index]] as IToString;

                                    if (toString != null)
                                        valueElement = toString.ToString(toStringFlags);
                                    else
                                        valueElement = dictionary[list[index]].ToString();
                                }
                                else
                                {
                                    valueElement = dictionary[list[index]].ToString();
                                }
                            }
                            else
                            {
                                valueElement = String.Empty;
                            }

                            element = StringList.MakeList(keyElement, valueElement);
                        }
                        else
                        {
                            element = String.Empty;
                        }

                        if ((regEx == null) || regEx.IsMatch(element))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None : ListElementFlags.DontQuoteHash;

                            Parser.ScanElement(/* null, */ element, 0, element.Length, ref flags);

                            if ((result.Length > 0) && (!String.IsNullOrEmpty(separator)))
                                result.Append(separator);

                            Parser.ConvertElement(/* null, */ element, 0, element.Length, flags, ref result);
                        }
                    }
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ListToString(
            IDictionary<T1, T2> dictionary,
            int startIndex,
            int stopIndex,
            ToStringFlags toStringFlags,
            string separator,
            string pattern,
            bool noCase
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (dictionary != null)
            {
                List<T1> list = new List<T1>(dictionary.Keys);

                if (ListOps.CheckStartAndStopIndex(0, list.Count - 1, ref startIndex, ref stopIndex))
                {
                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        string element;

                        if (list[index] != null)
                        {
                            string keyElement;

                            if (toStringFlags != ToStringFlags.None)
                            {
                                IToString toString = list[index] as IToString;

                                if (toString != null)
                                    keyElement = toString.ToString(toStringFlags);
                                else
                                    keyElement = list[index].ToString();
                            }
                            else
                            {
                                keyElement = list[index].ToString();
                            }

                            string valueElement;

                            if (dictionary[list[index]] != null)
                            {
                                if (toStringFlags != ToStringFlags.None)
                                {
                                    IToString toString = dictionary[list[index]] as IToString;

                                    if (toString != null)
                                        valueElement = toString.ToString(toStringFlags);
                                    else
                                        valueElement = dictionary[list[index]].ToString();
                                }
                                else
                                {
                                    valueElement = dictionary[list[index]].ToString();
                                }
                            }
                            else
                            {
                                valueElement = String.Empty;
                            }

                            element = StringList.MakeList(keyElement, valueElement);
                        }
                        else
                        {
                            element = String.Empty;
                        }

                        if ((pattern == null) ||
                            StringOps.Match(null, StringOps.DefaultMatchMode, element, pattern, noCase))
                        {
                            ListElementFlags flags = (index == startIndex) ?
                                ListElementFlags.None : ListElementFlags.DontQuoteHash;

                            Parser.ScanElement(/* null, */ element, 0, element.Length, ref flags);

                            if ((result.Length > 0) && (!String.IsNullOrEmpty(separator)))
                                result.Append(separator);

                            Parser.ConvertElement(/* null, */ element, 0, element.Length, flags, ref result);
                        }
                    }
                }
            }

            return result.ToString();
        }
    }
    #endregion
}
