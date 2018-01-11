/*
 * IntDictionary.cs --
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
using System.Globalization;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d0f3e89e-8835-471a-aaff-81a0b97c49ef")]
    internal sealed class IntDictionary : Dictionary<string, int>
    {
        #region Public Constructors
        public IntDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            IEnumerable<string> collection,
            CultureInfo cultureInfo
            )
            : this()
        {
            Add(collection, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public IntDictionary(
            IEnumerable<string> collection
            )
            : this()
        {
            AddKeys(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private IntDictionary(
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
        public void Add(
            IEnumerable<string> collection,
            CultureInfo cultureInfo
            )
        {
            foreach (string item in collection)
            {
                //
                // NOTE: We require a list of lists to process into name/value
                //       pairs.  The name is always a string and the value must
                //       parse as a valid integer.
                //
                StringList list = null;

                if (Parser.SplitList(
                        null, item, 0, Length.Invalid, true,
                        ref list) == ReturnCode.Ok)
                {
                    //
                    // NOTE: We require at least a name and a value, extra
                    //       elements are silently ignored.
                    //
                    if (list.Count >= 2)
                    {
                        string key = list[0];

                        //
                        // NOTE: *WARNING* Empty array element names are
                        //       allowed, please do not change this to
                        //       "!String.IsNullOrEmpty".
                        //
                        if (key != null)
                        {
                            //
                            // NOTE: Attempt to parse the list element as a
                            //       valid integer; if not, it will be silently
                            //       ignored.
                            //
                            int value = 0;

                            if (Value.GetInteger2(list[1], ValueFlags.AnyInteger,
                                    cultureInfo, ref value) == ReturnCode.Ok)
                            {
                                if (this.ContainsKey(key))
                                    this[key] += value;
                                else
                                    this.Add(key, value);
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddKeys(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
            {
                if (item == null)
                    continue;

                int value;

                if (TryGetValue(item, out value))
                    value += 1;
                else
                    value = 1;

                this[item] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, int>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return GenericOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return GenericOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
