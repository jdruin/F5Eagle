/*
 * MutableAnyTriplet.cs --
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
    [ObjectId("b70c6b5f-6507-44ef-bca7-abd1b14efff8")]
    public class MutableAnyTriplet<T1, T2, T3> :
        IMutableAnyTriplet<T1, T2, T3>,
        IComparer<IMutableAnyTriplet<T1, T2, T3>>,
        IComparable<IMutableAnyTriplet<T1, T2, T3>>,
        IEquatable<IMutableAnyTriplet<T1, T2, T3>>,
        IComparable,
        IToString
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null triplet object.
        //
        public MutableAnyTriplet()
            : this(false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyTriplet(
            T1 x
            )
            : this(false, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyTriplet(
            T1 x,
            T2 y
            )
            : this(false, x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyTriplet(
            T1 x,
            T2 y,
            T3 z
            )
            : this(false, x, y, z)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyTriplet(
            bool mutable
            )
            : base()
        {
            this.mutable = mutable;
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyTriplet(
            bool mutable,
            T1 x
            )
            : this(mutable)
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyTriplet(
            bool mutable,
            T1 x,
            T2 y
            )
            : this(mutable, x)
        {
            this.y = y;
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyTriplet(
            bool mutable,
            T1 x,
            T2 y,
            T3 z
            )
            : this(mutable, x, y)
        {
            this.z = z;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void CheckMutable()
        {
            if (!mutable)
                throw new InvalidOperationException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet Members
        private T1 x;
        public virtual T1 X
        {
            get { return x; }
            set { CheckMutable(); x = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T2 y;
        public virtual T2 Y
        {
            get { return y; }
            set { CheckMutable(); y = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T3 z;
        public virtual T3 Z
        {
            get { return z; }
            set { CheckMutable(); z = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMutableAnyTriplet Members
        private bool mutable;
        public virtual bool Mutable
        {
            get { return mutable; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Extractor Class
        [ObjectId("7d6c5223-c54e-425b-b916-0bdbe4acdd81")]
        public static class Extractor<T4, T5, T6>
        {
            public static bool Extract(
                IMutableAnyTriplet<T1, T2, T3> triplet,
                ref T4 x,
                ref T5 y,
                ref T6 z
                )
            {
                try
                {
                    //
                    // HACK: This is not ideal; however, we cannot use the "as"
                    //       operator unless we place a restrictions on all the
                    //       types (i.e. they must be classes).
                    //
                    T4 x2 = (T4)(object)triplet.X; /* throw */
                    T5 y2 = (T5)(object)triplet.Y; /* throw */
                    T6 z2 = (T6)(object)triplet.Z; /* throw */

                    //
                    // NOTE: Assign to the variables provided by the caller now
                    //       that we know the casts were all successful.  These
                    //       statements cannot throw.
                    //
                    x = x2; y = y2; z = z2;

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
        public static MutableAnyTriplet<T1, T2, T3> FromType1(
            T1 value
            )
        {
            return new MutableAnyTriplet<T1, T2, T3>(
                value, default(T2), default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

        public static MutableAnyTriplet<T1, T2, T3> FromType2(
            T2 value
            )
        {
            return new MutableAnyTriplet<T1, T2, T3>(
                default(T1), value, default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

        public static MutableAnyTriplet<T1, T2, T3> FromType3(
            T3 value
            )
        {
            return new MutableAnyTriplet<T1, T2, T3>(
                default(T1), default(T2), value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        public static implicit operator MutableAnyTriplet<T1, T2, T3>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator MutableAnyTriplet<T1, T2, T3>(
            T2 value
            )
        {
            return FromType2(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator MutableAnyTriplet<T1, T2, T3>(
            T3 value
            )
        {
            return FromType3(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override bool Equals(
            object obj
            )
        {
            IMutableAnyTriplet<T1, T2, T3> triplet =
                obj as IMutableAnyTriplet<T1, T2, T3>;

            if (triplet != null)
                return GenericOps<T1>.Equals(this.X, triplet.X) &&
                       GenericOps<T2>.Equals(this.Y, triplet.Y) &&
                       GenericOps<T3>.Equals(this.Z, triplet.Z);
            else
                return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                GenericOps<T1>.GetHashCode(this.X),
                GenericOps<T2>.GetHashCode(this.Y),
                GenericOps<T3>.GetHashCode(this.Z));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IMutableAnyTriplet<T1,T2,T3>> Members
        public int Compare(
            IMutableAnyTriplet<T1, T2, T3> x,
            IMutableAnyTriplet<T1, T2, T3> y
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

                result = Comparer<T2>.Default.Compare(x.Y, y.Y);

                if (result != 0)
                    return result;

                return Comparer<T3>.Default.Compare(x.Z, y.Z);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable<IMutableAnyTriplet<T1,T2,T3>> Members
        public int CompareTo(
            IMutableAnyTriplet<T1, T2, T3> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IMutableAnyTriplet<T1,T2,T3>> Members
        public bool Equals(
            IMutableAnyTriplet<T1, T2, T3> other
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
            IMutableAnyTriplet<T1, T2, T3> other =
                obj as IMutableAnyTriplet<T1, T2, T3>;

            if (other == null)
                throw new ArgumentException();

            return CompareTo(other);
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
            return String.Format(format, this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            string format,
            int limit,
            bool strict
            )
        {
            return FormatOps.Ellipsis(
                String.Format(format, this.X, this.Y, this.Z), limit, strict);
        }
        #endregion
    }
}
