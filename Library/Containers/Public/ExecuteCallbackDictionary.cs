/*
 * ExecuteCallbackDictionary.cs --
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

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("a2e8fe45-318f-454f-982e-b483c53ef3d7")]
    public sealed class ExecuteCallbackDictionary : Dictionary<string, ExecuteCallback>
    {
        #region Public Constructors
        public ExecuteCallbackDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ExecuteCallbackDictionary(
            IDictionary<string, ExecuteCallback> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ExecuteCallbackDictionary(
            IEnumerable<ExecuteCallback> collection
            )
            : this()
        {
            AddRange(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private ExecuteCallbackDictionary(
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

        public void AddRange(
            IEnumerable<ExecuteCallback> collection
            )
        {
            foreach (ExecuteCallback item in collection)
                if (item != null)
                    this.Add(FormatOps.DelegateName(item), item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void AddRange(
            IDictionary<string, ExecuteCallback> dictionary
            )
        {
            foreach (KeyValuePair<string, ExecuteCallback> pair in dictionary)
                this.Add(pair.Key, pair.Value);
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

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
