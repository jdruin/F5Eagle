/*
 * ListOps.cs --
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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;

namespace Eagle._Components.Private
{
    [ObjectId("41713d5d-1147-4395-9863-92e45a9f28dc")]
    internal static class ListOps
    {
        public static bool CheckStartAndStopIndex(
            int lowerBound,
            int upperBound,
            ref int startIndex,
            ref int stopIndex
            )
        {
            Result error = null;

            return CheckStartAndStopIndex(lowerBound, upperBound,
                ref startIndex, ref stopIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CheckStartAndStopIndex(
            int lowerBound,
            int upperBound,
            ref int startIndex,
            ref int stopIndex,
            ref Result error
            )
        {
            bool result = false;

            if (startIndex < 0)
                startIndex = lowerBound;

            if (stopIndex < 0)
                stopIndex = upperBound;

            if ((startIndex >= lowerBound) && (startIndex <= upperBound))
            {
                if ((stopIndex >= lowerBound) && (stopIndex <= upperBound))
                {
                    if (startIndex <= stopIndex)
                    {
                        result = true;
                    }
                    else
                    {
                        error = "start index is greater than stop index";
                    }
                }
                else
                {
                    error = "stop index is out of bounds";
                }
            }
            else
            {
                error = "start index is out of bounds";
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(params string[] strings)
        {
            return (strings != null) ? Concat(new StringList(strings)) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list)
        {
            return Concat(list, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list, int startIndex)
        {
            return (list != null) ? Concat(list, startIndex, list.Count - 1) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list, int startIndex, int stopIndex)
        {
            return Concat(list, startIndex, stopIndex, Characters.Space.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list, int startIndex, string separator)
        {
            return (list != null) ? Concat(list, startIndex, list.Count - 1,
                (separator != null) ? separator : Characters.Space.ToString()) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Concat(IList list, int startIndex, int stopIndex, string separator)
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (list != null)
            {
                if (CheckStartAndStopIndex(
                        0, list.Count - 1, ref startIndex, ref stopIndex))
                {
                    //
                    // NOTE: This function joins each of its arguments together
                    //       with spaces after trimming leading and trailing
                    //       white-space from each of them. If all the arguments
                    //       are lists, this has the same effect as concatenating
                    //       them into a single list. It permits any number of
                    //       arguments; if no args are supplied, the result is an
                    //       empty string.
                    //
                    for (int index = startIndex; index <= stopIndex; index++)
                    {
                        object element = list[index];

                        if (element == null)
                            continue;

                        string value = element.ToString();

                        if (String.IsNullOrEmpty(value))
                            continue;

                        value = value.Trim();

                        if (String.IsNullOrEmpty(value))
                            continue;

                        if (result.Length > 0)
                            result.Append(separator);

                        result.Append(value);
                    }
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetMaximumLength(
            IEnumerable<string> collection
            )
        {
            int result = Length.Invalid;

            if (collection != null)
            {
                foreach (string item in collection)
                {
                    if (item == null)
                        continue;

                    if (item.Length > result)
                        result = item.Length;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetMaximumLength(
            IList list,
            string format,
            int limit
            )
        {
            int result = Length.Invalid;

            if (list != null)
            {
                foreach (object element in list)
                {
                    if (element != null)
                    {
                        IToString toString = element as IToString;
                        string value;

                        if (toString != null)
                            value = toString.ToString(format);
                        else
                            value = element.ToString();

                        if (!String.IsNullOrEmpty(value))
                        {
                            if ((result == Length.Invalid) ||
                                (value.Length > result))
                            {
                                result = value.Length;
                            }
                        }
                    }
                }

                //
                // NOTE: Reduce to the maximum limit allowed by the caller.
                //
                if ((result != Length.Invalid) &&
                    (limit != Length.Invalid) &&
                    (result > limit))
                {
                    result = limit;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SelectFromSubList(
            Interpreter interpreter,
            string text,
            string indexText,
            bool clear,
            CultureInfo cultureInfo,
            ref IntList indexList,
            ref Result error
            )
        {
            string value = null;

            return SelectFromSubList(interpreter, text, indexText, clear, cultureInfo,
                ref value, ref indexList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SelectFromSubList(
            Interpreter interpreter,
            string text,
            string indexText,
            bool clear,
            CultureInfo cultureInfo,
            ref string value,
            ref Result error
            )
        {
            IntList indexList = null;

            return SelectFromSubList(interpreter, text, indexText, clear, cultureInfo,
                ref value, ref indexList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SelectFromSubList(
            Interpreter interpreter,
            string text,
            string indexText,
            bool clear,
            CultureInfo cultureInfo,
            ref string value,
            ref IntList indexList,
            ref Result error
            )
        {
            ReturnCode code;

            if (!String.IsNullOrEmpty(indexText))
            {
                StringList indexTextList = null;

                code = Parser.SplitList(
                    interpreter, indexText, 0, Length.Invalid, true,
                    ref indexTextList, ref error);

                if (code == ReturnCode.Ok)
                {
                    if (indexTextList.Count > 0)
                    {
                        StringList list = null;

                        code = Parser.SplitList(
                            interpreter, text, 0, Length.Invalid, true,
                            ref list, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            string localValue = null;
                            IntList localIndexList = new IntList();

                            for (int index = 0; index < indexTextList.Count; index++)
                            {
                                int listIndex = Index.Invalid;

                                code = Value.GetIndex(
                                    indexTextList[index], list.Count,
                                    ValueFlags.AnyIndex, cultureInfo,
                                    ref listIndex, ref error);

                                if (code != ReturnCode.Ok)
                                    break;

                                if ((listIndex < 0) ||
                                    (listIndex >= list.Count) ||
                                    (list[listIndex] == null))
                                {
                                    error = String.Format(
                                        "element {0} missing from sublist \"{1}\"",
                                        listIndex, list.ToString());

                                    code = ReturnCode.Error;
                                    break;
                                }

                                localValue = list[listIndex];
                                localIndexList.Add(listIndex);

                                StringList subList = null;

                                code = Parser.SplitList(
                                    interpreter, list[listIndex], 0,
                                    Length.Invalid, true, ref subList,
                                    ref error);

                                if (code == ReturnCode.Ok)
                                    list = subList;
                                else
                                    break;
                            }

                            if (code == ReturnCode.Ok)
                            {
                                value = localValue;

                                if (clear || (indexList == null))
                                    indexList = localIndexList;
                                else
                                    indexList.AddRange(localIndexList);
                            }
                        }
                    }
                    else
                    {
                        value = text;

                        if (clear || (indexList == null))
                            indexList = new IntList();
                    }
                }
            }
            else
            {
                value = text;

                if (clear || (indexList == null))
                    indexList = new IntList();

                code = ReturnCode.Ok;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void GetElementsToCompare(
            Interpreter interpreter,
            bool ascending,
            string indexText,
            bool leftOnly,
            bool pattern,
            CultureInfo cultureInfo,
            ref string left,
            ref string right
            )
        {
            if (indexText != null)
            {
                string leftValue = null;
                Result error = null;

                if (SelectFromSubList(interpreter, left, indexText, false,
                        cultureInfo, ref leftValue, ref error) == ReturnCode.Ok)
                {
                    if (leftOnly)
                    {
                        left = leftValue;
                    }
                    else
                    {
                        string rightValue = null;

                        if (SelectFromSubList(interpreter, right, indexText, false,
                                cultureInfo, ref rightValue, ref error) == ReturnCode.Ok)
                        {
                            left = leftValue;
                            right = rightValue;
                        }
                    }
                }

                if (error != null)
                    throw new ScriptException(error);
            }

            if (!ascending && !pattern)
            {
                string swap = left;
                left = right;
                right = swap;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ComparerEquals(
            IComparer<string> comparer,
            string left,
            string right
            )
        {
            if (comparer != null)
                return (comparer.Compare(left, right) == 0 /* EQUAL */);
            else
                return Comparer<string>.Default.Compare(left, right) == 0 /* EQUAL */;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int ComparerGetHashCode(
            IComparer<string> comparer,
            string value,
            bool noCase
            )
        {
            //
            // NOTE: The only thing that we must guarantee here, according
            //       to the MSDN documentation for IEqualityComparer, is
            //       that for two given strings, if Equals return true then
            //       the two strings must hash to the same value.
            //
            if (value != null)
                return noCase ? value.ToLower().GetHashCode() : value.GetHashCode();
            else
                throw new ArgumentNullException("value");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetDuplicateCount( /* O(N) */
            IComparer<string> comparer,
            IntDictionary duplicates,
            string value
            )
        {
            //
            // HACK: Since the ContainsKey method of the Dictionary object
            //       insists on using both the Equals and GetHashCode methods
            //       of the custom IEqualityComparer interface we provide
            //       to find the key, we must resort to a linear search
            //       because we cannot reasonably implement the GetHashCode
            //       method in terms of the Compare method in a semantically
            //       compatible way.
            //
            int result = 0;

            if ((comparer != null) && (duplicates != null) && (value != null))
            {
                foreach (string element in duplicates.Keys)
                {
                    if (comparer.Compare(element, value) == 0 /* EQUAL */)
                    {
                        //
                        // NOTE: Found the key value, get the count.
                        //
                        result = duplicates[element];
                        break;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool SetDuplicateCount( /* O(N) */
            IComparer<string> comparer,
            IntDictionary duplicates,
            string value,
            int count
            )
        {
            //
            // HACK: Since the ContainsKey method of the Dictionary object
            //       insists on using both the Equals and GetHashCode methods
            //       of the custom IEqualityComparer interface we provide
            //       to find the key, we must resort to a linear search
            //       because we cannot reasonably implement the GetHashCode
            //       method in terms of the Compare method in a semantically
            //       compatible way.
            //
            if ((comparer != null) && (duplicates != null) && (value != null))
            {
                foreach (string element in duplicates.Keys)
                {
                    if (comparer.Compare(element, value) == 0 /* EQUAL */)
                    {
                        //
                        // NOTE: Found the key value, set the count.
                        //
                        duplicates[element] = count;
                        return true;
                    }
                }

                //
                // NOTE: The value was not found in the dictionary,
                //       add it now.
                //
                duplicates.Add(value, count);
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void UpdateDuplicateCount( /* 2 * O(N) */
            IComparer<string> comparer,
            IntDictionary duplicates,
            string left,
            string right,
            bool unique,
            int result,
            ref int levels
            ) /* throw */
        {
            if (unique && (result == 0 /* EQUAL */))
            {
                if ((duplicates != null) && (left != null) && (right != null))
                {
                    //
                    // NOTE: Skip instances where the sort algorithm is actually
                    //       having us compare the exact same string.
                    //
                    if (!Object.ReferenceEquals(left, right))
                    {
                        //
                        // NOTE: Only continue if we are not already processing
                        //       duplicate counts already.
                        //
                        if (Interlocked.Increment(ref levels) == 1)
                        {
                            try
                            {
                                //
                                // NOTE: Search for all the list elements that are duplicates
                                //       of the left element.  This is an O(N) operation in
                                //       the worst case (i.e. if every element in the list is
                                //       a duplicate of the provided left element).
                                //
                                int count = GetDuplicateCount(comparer, duplicates, left);

                                if (count != Count.Invalid)
                                    //
                                    // NOTE: Set the duplicate count of the first list element
                                    //       that is a duplicate of the provided left element.
                                    //       This is an O(N) operation in the worst case (i.e.
                                    //       if the last element in the list is the first
                                    //       duplicate of the provided left element).
                                    //
                                    if (!SetDuplicateCount(comparer, duplicates, left, ++count))
                                        throw new ScriptException(String.Format(
                                            "failed to update duplicate count for element \"{0}\"",
                                            left));
                            }
                            finally
                            {
                                //
                                // NOTE: Even if we are throwing an exception, we want
                                //       to keep the number of active levels at the
                                //       correct value.
                                //
                                Interlocked.Decrement(ref levels);
                            }
                        }
                        else
                        {
                            //
                            // NOTE: When we incremented the number of active levels it
                            //       resulted in a value higher than one; notwithstanding
                            //       that state of affairs, we still need to decremenet
                            //       the number of active levels because we did successfully
                            //       increment it.
                            //
                            Interlocked.Decrement(ref levels);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IList FromNameValueCollection(
            NameValueCollection collection,
            IList @default
            )
        {
            IList result = (@default != null) ?
                new StringList(@default) : null;

            if (collection != null)
            {
                if (result == null)
                    result = new StringList();

                int count = collection.Count;

                for (int index = 0; index < count; index++)
                {
                    result.Add(collection.GetKey(index));
                    result.Add(collection.Get(index));
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static NameValueCollection ToNameValueCollection(
            IList list,
            NameValueCollection @default
            )
        {
            NameValueCollection result = @default;

            if (list != null)
            {
                if (result == null)
                    result = new NameValueCollection();

                int count = list.Count;

                for (int index = 0; index < count; index += 2)
                {
                    object element = null;
                    string name = null;
                    string value = null;

                    element = list[index];

                    name = (element != null) ?
                        element.ToString() : null;

                    if ((index + 1) < count)
                    {
                        element = list[index + 1];

                        value = (element != null) ?
                            element.ToString() : null;
                    }

                    result.Add(name, value);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Combine(
            IList<IList<StringBuilder>> lists, /* in */
            ref IList<StringBuilder> list,     /* in, out */
            ref Result error                   /* out */
            )
        {
            if (lists == null)
            {
                error = "invalid list of lists";
                return ReturnCode.Error;
            }

            if (lists.Count == 0)
            {
                error = "no lists in list";
                return ReturnCode.Error;
            }

            IList<StringBuilder> list1 = lists[0];

            if (lists.Count > 1)
            {
                for (int index = 1; index < lists.Count; index++)
                {
                    IList<StringBuilder> list2 = lists[index];
                    IList<StringBuilder> list3 = null;

                    if (Combine(
                            list1, list2, ref list3, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    list1 = list3;
                }
            }

            if (list != null)
                GenericOps<StringBuilder>.AddRange(list, list1);
            else
                list = new List<StringBuilder>(list1);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode Combine(
            IList<StringBuilder> list1,     /* in */
            IList<StringBuilder> list2,     /* in */
            ref IList<StringBuilder> list3, /* in, out */
            ref Result error                /* out */
            )
        {
            if (list1 == null)
            {
                if (list2 == null)
                {
                    error = "cannot combine, neither list is valid";
                    return ReturnCode.Error;
                }

                if (list3 != null)
                    GenericOps<StringBuilder>.AddRange(list3, list2);
                else
                    list3 = new List<StringBuilder>(list2);
            }
            else if (list2 == null)
            {
                if (list3 != null)
                    GenericOps<StringBuilder>.AddRange(list3, list1);
                else
                    list3 = new List<StringBuilder>(list1);
            }
            else
            {
                if ((list1.Count > 0) || (list2.Count > 0))
                {
                    if (list3 == null)
                        list3 = new List<StringBuilder>();
                }

                foreach (StringBuilder element1 in list1)
                {
                    foreach (StringBuilder element2 in list2)
                    {
                        int capacity = 0;

                        if (element1 != null)
                            capacity += element1.Length;

                        if (element2 != null)
                            capacity += element2.Length;

                        StringBuilder element3 = StringOps.NewStringBuilder(
                            capacity);

                        element3.Append(element1);
                        element3.Append(element2);

                        list3.Add(element3);
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList Flatten(
            IList<StringBuilder> list
            )
        {
            if (list == null)
                return null;

            StringList result = new StringList();

            foreach (StringBuilder element in list)
                result.Add(element);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void HandlePermuteResult(
            ListTransformCallback callback, /* in */
            IList<string> list,             /* in */
            ref IList<IList<string>> result /* in, out */
            )
        {
            if (list == null)
                return;

            if ((callback == null) || callback(list))
            {
                if (result == null)
                    result = new List<IList<string>>();

                result.Add(new StringList(list)); /* COPY */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IList<IList<string>> Permute(
            IList<string> list,
            ListTransformCallback callback
            )
        {
            IList<IList<string>> result = null;

            if (list != null)
            {
                IList<string> localList = new StringList(list); /* COPY */

                HandlePermuteResult(callback, localList, ref result);

                int count = localList.Count;
                int[] indexes = new int[count + 1];
                int index1 = 1;

                while (index1 < count)
                {
                    if (indexes[index1] < index1)
                    {
                        int index2 = index1 % 2 * indexes[index1];
                        string temporary = localList[index2];

                        localList[index2] = localList[index1];
                        localList[index1] = temporary;

                        HandlePermuteResult(callback, localList, ref result);

                        indexes[index1]++;
                        index1 = 1;
                    }
                    else
                    {
                        indexes[index1] = 0;
                        index1++;
                    }
                }
            }

            return result;
        }
    }
}
