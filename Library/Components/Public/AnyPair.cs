/*
 * AnyPair.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2dacac40-7b18-4fc6-841a-35ffa7550dc9")]
    public class AnyPair<T1, T2> :
        IAnyPair<T1, T2>,
        IComparer<IAnyPair<T1, T2>>,
        IComparable<IAnyPair<T1, T2>>,
        IEquatable<IAnyPair<T1, T2>>,
        IComparable,
        IToString
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        public AnyPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyPair(
            T1 x
            )
            : this()
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyPair(
            T1 x,
            T2 y
            )
            : this(x)
        {
            this.y = y;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyPair Members
        private T1 x;
        public virtual T1 X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T2 y;
        public virtual T2 Y
        {
            get { return y; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Extractor Class
        [ObjectId("0c69fe4d-75ee-40cb-b924-b3bc5225790b")]
        public static class Extractor<T3, T4>
        {
            public static bool Extract(
                IAnyPair<T1, T2> pair,
                ref T3 x,
                ref T4 y
                )
            {
                try
                {
                    //
                    // HACK: This is not ideal; however, we cannot use the "as"
                    //       operator unless we place a restrictions on all the
                    //       types (i.e. they must be classes).
                    //
                    T3 x2 = (T3)(object)pair.X; /* throw */
                    T4 y2 = (T4)(object)pair.Y; /* throw */

                    //
                    // NOTE: Assign to the variables provided by the caller now
                    //       that we know the casts were all successful.  These
                    //       statements cannot throw.
                    //
                    x = x2; y = y2;

                    //
                    // NOTE: All of the casts succeeded.
                    //
                    return true;
                }
                catch
                {
                    // do nothing.
                }

                //
                // NOTE: One of the casts failed.
                //
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        public static AnyPair<T1, T2> FromType1(
            T1 value
            )
        {
            return new AnyPair<T1, T2>(value, default(T2));
        }

        ///////////////////////////////////////////////////////////////////////

        public static AnyPair<T1, T2> FromType2(
            T2 value
            )
        {
            return new AnyPair<T1, T2>(default(T1), value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        public static implicit operator AnyPair<T1, T2>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator AnyPair<T1, T2>(
            T2 value
            )
        {
            return FromType2(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(
            object obj
            )
        {
            IAnyPair<T1, T2> anyPair = obj as IAnyPair<T1, T2>;

            if (anyPair != null)
            {
                return GenericOps<T1>.Equals(this.X, anyPair.X) &&
                       GenericOps<T2>.Equals(this.Y, anyPair.Y);
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                GenericOps<T1>.GetHashCode(this.X),
                GenericOps<T2>.GetHashCode(this.Y));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IAnyPair<T1,T2>> Members
        public int Compare(
            IAnyPair<T1, T2> x,
            IAnyPair<T1, T2> y
            )
        {
            if ((x == null) && (y == null))
            {
                return 0;
            }
            else if (x == null)
            {
                return -1;
            }
            else if (y == null)
            {
                return 1;
            }
            else
            {
                int result = Comparer<T1>.Default.Compare(x.X, y.X);

                if (result != 0)
                    return result;

                return Comparer<T2>.Default.Compare(x.Y, y.Y);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable<IAnyPair<T1,T2>> Members
        public int CompareTo(
            IAnyPair<T1, T2> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IAnyPair<T1,T2>> Members
        public bool Equals(
            IAnyPair<T1, T2> other
            )
        {
            return CompareTo(other) == 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable Members
        public int CompareTo(
            object obj
            )
        {
            IAnyPair<T1, T2> anyPair = obj as IAnyPair<T1, T2>;

            if (anyPair == null)
                throw new ArgumentException();

            return CompareTo(anyPair);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        public virtual string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            ToStringFlags flags,
            string @default /* NOT USED */
            )
        {
            return ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            string format
            )
        {
            return String.Format(format, this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            string format,
            int limit,
            bool strict
            )
        {
            return FormatOps.Ellipsis(
                String.Format(format, this.X, this.Y), limit, strict);
        }
        #endregion
    }
}
