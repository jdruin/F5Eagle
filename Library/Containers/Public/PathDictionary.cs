/*
 * PathDictionary.cs --
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
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("0a2942ed-d174-4549-8cdf-8e8cf003d9b9")]
    public class PathDictionary<T> :
            Dictionary<string, T>, IDictionary<string, T> where T : new()
    {
        #region Public Constructors
        public PathDictionary()
            : this(PathTranslationType.Default)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PathDictionary(
            IDictionary<string, T> dictionary
            )
            : this(dictionary, PathTranslationType.Default)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public PathDictionary(
            IEqualityComparer<string> comparer
            )
            : this(comparer, PathTranslationType.Default)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        protected PathDictionary(
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

        #region Internal Constructors
        internal PathDictionary(
            IEnumerable<string> collection
            )
            : this(PathTranslationType.Default)
        {
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private PathDictionary(
            PathTranslationType translationType
            )
            : base(new _Comparers.Custom(PathOps.ComparisonType))
        {
            this.translationType = translationType;
        }

        ///////////////////////////////////////////////////////////////////////

        private PathDictionary(
            IDictionary<string, T> dictionary,
            PathTranslationType translationType
            )
            : base(dictionary, new _Comparers.Custom(PathOps.ComparisonType))
        {
            this.translationType = translationType;
        }

        ///////////////////////////////////////////////////////////////////////

        private PathDictionary(
            IEqualityComparer<string> comparer,
            PathTranslationType translationType
            )
            : base(comparer)
        {
            this.translationType = translationType;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Collections.Generic.IDictionary<string, TValue> Overrides
        T IDictionary<string, T>.this[string key]
        {
            get { return base[PathOps.TranslatePath(key, translationType)]; }
            set { base[PathOps.TranslatePath(key, translationType)] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        void IDictionary<string, T>.Add(
            string key,
            T value
            )
        {
            base.Add(PathOps.TranslatePath(key, translationType), value);
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<string, T>.ContainsKey(
            string key
            )
        {
            return base.ContainsKey(
                PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<string, T>.Remove(
            string key
            )
        {
            return base.Remove(PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        bool IDictionary<string, T>.TryGetValue(
            string key,
            out T value
            )
        {
            return base.TryGetValue(
                PathOps.TranslatePath(key, translationType), out value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Collections.Generic.Dictionary<string, TValue> Overrides
        public new T this[string key]
        {
            get { return base[PathOps.TranslatePath(key, translationType)]; }
            set { base[PathOps.TranslatePath(key, translationType)] = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public new void Add(
            string key,
            T value
            )
        {
            base.Add(PathOps.TranslatePath(key, translationType), value);
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool ContainsKey(
            string key
            )
        {
            return base.ContainsKey(
                PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool Remove(
            string key
            )
        {
            return base.Remove(PathOps.TranslatePath(key, translationType));
        }

        ///////////////////////////////////////////////////////////////////////

        public new bool TryGetValue(
            string key,
            out T value
            )
        {
            return base.TryGetValue(
                PathOps.TranslatePath(key, translationType), out value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private PathTranslationType translationType;
        public PathTranslationType TranslationType
        {
            get { return translationType; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual void Add(
            IEnumerable<string> collection
            )
        {
            Add(collection, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual void Add(
            string key
            )
        {
            this.Add(key, new T());
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool Contains(
            string key
            )
        {
            return this.ContainsKey(key);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual T Add(
            string key,
            T value,
            bool reserved
            )
        {
            Add(key, value);

            return this[key];
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
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

        #region Private Methods
        protected internal virtual void Add(
            IEnumerable<string> collection,
            bool merge
            )
        {
            foreach (string item in collection)
                if (!merge || !this.ContainsKey(item))
                    this.Add(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
            )
        {
            info.AddValue("translationType", translationType);

            base.GetObjectData(info, context);
        }
#endif
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
