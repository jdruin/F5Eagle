/*
 * StringPairList.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("fa3c3c95-bcc7-4c71-ab7f-94e534f9aad2")]
    public sealed class StringPairList : List<IPair<string>>,
            IStringList, IGetValue
    {
        #region Public Constants
        public static readonly bool DefaultEmpty = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringPairList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IEnumerable<IPair<string>> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            params IPair<string>[] pairs
            )
            : base(pairs)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            string[] array,
            int startIndex
            )
        {
            Add(array, startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            params string[] strings
            )
        {
            Add(strings);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IEnumerable<string> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IEnumerable<StringBuilder> collection
            )
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList(
            IDictionary<string, string> dictionary
            )
        {
            Add(dictionary);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue Members
        //
        // NOTE: This must call ToString to provide a "flattened" value
        //       because this is a mutable class.
        //
        public object Value
        {
            get { return ToString(); }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            get
            {
                string stringValue = ToString();

                return (stringValue != null) ?
                    stringValue.Length : _Constants.Length.Invalid;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            get { return ToString(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new StringPairList(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IStringList Members
        #region Properties
#if LIST_CACHE
        private string cacheKey;
        public string CacheKey
        {
            get { return cacheKey; }
            set { cacheKey = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Get Methods
        public string GetItem(
            int index
            )
        {
            return this[index].ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public IPair<string> GetPair(
            int index
            )
        {
            return this[index];
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Methods
        public void Add(
            string item
            )
        {
            if (item != null)
                this.Add(new StringPair(item));
            else
                this.Add((IPair<string>)null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string key,
            string value
            )
        {
            this.Add(new StringPair(key, value));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringBuilder item
            )
        {
            this.Add((item != null) ? item.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            string[] array,
            int startIndex
            )
        {
            for (int index = startIndex; index < array.Length; index++)
                Add(array[index]);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                object item = list[index];

                if (item != null)
                    Add(item.ToString());
                else
                    Add((string)null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IStringList list,
            int startIndex
            )
        {
            for (int index = startIndex; index < list.Count; index++)
            {
                IPair<string> item = list.GetPair(index);

                if (item != null)
                    Add(item);
                else
                    Add((IPair<string>)null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<StringBuilder> collection
            )
        {
            foreach (StringBuilder item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IDictionary<string, string> dictionary
            )
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
                Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Argument> collection
            )
        {
            foreach (Argument item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Result> collection
            )
        {
            foreach (Result item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<IPair<string>> collection
            )
        {
            foreach (IPair<string> item in collection)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<Argument> collection
            )
        {
            foreach (Argument item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            StringTransformCallback callback,
            IEnumerable<Result> collection
            )
        {
            foreach (Result item in collection)
                Add(callback(item));
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method adds a null item if the final item currently in
        //       the list is not null -OR- the list is empty.  It returns true
        //       if an item was actually added.
        //
        public bool MaybeAddNull()
        {
            int count = base.Count;

            if (count == 0)
            {
                base.Add(null);
                return true;
            }

            IPair<string> item = base[count - 1];

            if (item == null)
                return false;

            base.Add(null);
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            bool empty
            )
        {
            return ToString(null, empty, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            return ToString(
                Characters.Space.ToString(), pattern, empty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
            return ToString(separator, pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string separator,
            string pattern,
            bool empty,
            bool noCase
            )
        {
            if (empty)
            {
                return GenericOps<IPair<string>>.ListToString(
                    this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
            else
            {
                StringPairList result = new StringPairList();

                foreach (IPair<string> element in this)
                {
                    if (element == null)
                        continue;

                    if (String.IsNullOrEmpty(element.X) &&
                        String.IsNullOrEmpty(element.Y))
                    {
                        continue;
                    }

                    result.Add(element);
                }

                return GenericOps<IPair<string>>.ListToString(
                    result, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    separator, pattern, noCase);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString()
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (IPair<string> element in this)
            {
                if (element != null)
                {
                    result.Append(element.X);
                    result.Append(element.Y);
                }
                else
                {
                    result.Append((string)null);
                    result.Append((string)null);
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString(
            string separator
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (IPair<string> element in this)
            {
                if (result.Length > 0)
                    result.Append(separator);

                if (element != null)
                {
                    result.Append(element.X);
                    result.Append(element.Y);
                }
                else
                {
                    result.Append((string)null);
                    result.Append((string)null);
                }
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToList Methods
        public IStringList ToList()
        {
            return new StringList(this);
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList(
            string pattern,
            bool noCase
            )
        {
            return ToList(pattern, DefaultEmpty, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public IStringList ToList(
            string pattern,
            bool empty,
            bool noCase
            )
        {
            StringPairList inputList;
            StringPairList outputList = new StringPairList();

            if (empty)
            {
                inputList = this;
            }
            else
            {
                inputList = new StringPairList();

                foreach (IPair<string> element in this)
                {
                    if (element == null)
                        continue;

                    if (String.IsNullOrEmpty(element.X) &&
                        String.IsNullOrEmpty(element.Y))
                    {
                        continue;
                    }

                    inputList.Add(element);
                }
            }

            ReturnCode code;
            Result error = null;

            code = GenericOps<IPair<string>>.FilterList(
                inputList, outputList, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);

            if (code != ReturnCode.Ok)
            {
                DebugOps.Complain(code, error);

                //
                // TODO: Return null in the error case here?
                //
                outputList = null;
            }

            return outputList;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(DefaultEmpty);
        }
        #endregion
    }
}
