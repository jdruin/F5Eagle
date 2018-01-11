/*
 * ResultList.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6a4ae770-2976-4753-bc9b-ee9dc47e409e")]
    public class ResultList : List<Result>, ICloneable
    {
        #region Private Constructor
        private ResultList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructor
        public ResultList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ResultList(
            IEnumerable<Result> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ResultList(
            IEnumerable<ResultList> collection
            )
            : base()
        {
            foreach (ResultList item in collection)
                this.AddRange(item); // NOTE: Flatten.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Search Methods
        public int Find(
            string result
            )
        {
            return Find(result, StringOps.UserStringComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public int Find(
            string result,
            StringComparison comparisonType
            )
        {
            for (int index = 0; index < this.Count; index++)
                if (String.Compare(this[index], result, comparisonType) == 0)
                    return index;

            return Index.Invalid;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
            //
            // HACK: The caller of this method should NOT rely upon the
            //       resulting string being a well-formed list as this
            //       is no longer guaranteed.
            //
            if (this.Count == 0)
            {
                return String.Empty;
            }
            else if (this.Count == 1)
            {
                Result result = this[0];

                if (result != null)
                    return result.ToString();
            }

            return GenericOps<Result>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString()
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (Result element in this)
                result.Append(element);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToRawString(
            ToStringFlags toStringFlags,
            string separator
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (Result element in this)
            {
                if (element != null)
                {
                    if ((separator != null) && (result.Length > 0))
                        result.Append(separator);

                    result.Append(element.ToString(toStringFlags));
                }
            }

            return result.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            ResultList list = new ResultList(this.Capacity);

            foreach (Result element in this)
            {
                list.Add((element != null) ?
                    element.Clone() as Result : null);
            }

            return list;
        }
        #endregion
    }
}
