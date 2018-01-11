/*
 * ArrayOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

#if NETWORK
using Eagle._Containers.Public;
#endif

using Eagle._Encodings;

#if NETWORK
using Eagle._Interfaces.Public;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("e6709468-95a4-405e-8c9c-e0dbd1aa3a88")]
    internal static class ArrayOps
    {
        #region Private Constants
        private static char[] byteSeparators = {
            Characters.HorizontalTab, Characters.LineFeed,
            Characters.VerticalTab, Characters.FormFeed,
            Characters.CarriageReturn, Characters.Space,
            Characters.Comma
        };

        ///////////////////////////////////////////////////////////////////////

        private static Encoding oneByteEncoding = OneByteEncoding.OneByte;
        private static Encoding twoByteEncoding = TwoByteEncoding.TwoByte;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static bool GetBounds(
            Array array,           /* in */
            ref int rank,          /* in, out */
            ref int[] lowerBounds, /* in, out */
            ref int[] lengths,     /* in, out */
            ref int[] indexes      /* in, out */
            )
        {
            if (array == null)
                return false;

            if (rank == 0)
                rank = array.Rank;

            if (rank <= 0)
                return false;

            if (lowerBounds != null)
                Array.Resize(ref lowerBounds, rank);
            else
                lowerBounds = new int[rank];

            if (lengths != null)
                Array.Resize(ref lengths, rank);
            else
                lengths = new int[rank];

            if (indexes != null)
                Array.Resize(ref indexes, rank);
            else
                indexes = new int[rank];

            //
            // NOTE: Setup all the lower bounds, lengths, and indexes to
            //       their initial states.
            //
            for (int rankIndex = 0; rankIndex < rank; rankIndex++)
            {
                //
                // NOTE: Get the bounds for each rank because we must
                //       iterate over all the elements in the array.
                //
                lowerBounds[rankIndex] = array.GetLowerBound(rankIndex);
                lengths[rankIndex] = array.GetLength(rankIndex);

                //
                // NOTE: Always set initial indexes to the lower bound.
                //
                indexes[rankIndex] = lowerBounds[rankIndex];
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IncrementIndexes(
            int rank,          /* in */
            int[] lowerBounds, /* in */
            int[] lengths,     /* in */
            int[] indexes      /* in, out */
            )
        {
#if false
            if ((lowerBounds == null) || (lengths == null) ||
                (indexes == null))
            {
                return false;
            }

            if ((lowerBounds.Length != lengths.Length) ||
                (lowerBounds.Length != indexes.Length))
            {
                return false;
            }

            if ((rank <= 0) || (rank > lowerBounds.Length))
                return false;
#endif

            //
            // NOTE: Determine the index of the "least significant" rank.
            //
            int rankIndex = rank - 1;

            //
            // NOTE: Keep going forever (i.e. until the loop is terminated
            //       from within).
            //
            while (true)
            {
                //
                // NOTE: Can the index of the current rank NOT be advanced
                //       without overflowing its bounds?
                //
                if (indexes[rankIndex] >=
                        (lowerBounds[rankIndex] + lengths[rankIndex] - 1))
                {
                    //
                    // NOTE: Ok, there would be an overflow; therefore, reset
                    //       the index of the current rank to its lower bound
                    //       and then advance to the next rank.
                    //
                    if (rankIndex > 0)
                    {
                        indexes[rankIndex] = lowerBounds[rankIndex];
                        rankIndex--;
                    }
                    else
                    {
                        //
                        // NOTE: No more ranks.  This condition is expected to
                        //       occur during the last iteration of loops in
                        //       the caller therefore, this is not technically
                        //       a "failure", per se.
                        //
                        return false;
                    }
                }

                //
                // NOTE: Increment the index for the current rank and return
                //       success.
                //
                indexes[rankIndex]++;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Array DeepCopy(
            Array array,     /* in */
            ref Result error /* out */
            )
        {
            if (array == null)
            {
                error = "invalid existing array";
                return null;
            }

            Type type = array.GetType();

            if (type == null)
            {
                error = "invalid array type";
                return null;
            }

            Type elementType = type.GetElementType();

            if (elementType == null)
            {
                error = "invalid array element type";
                return null;
            }

            int rank = 0;
            int[] lowerBounds = null;
            int[] lengths = null;
            int[] indexes = null;

            if (!GetBounds(
                    array, ref rank, ref lowerBounds,
                    ref lengths, ref indexes))
            {
                error = String.Format(
                    "could not get bounds for rank {0} array",
                    rank);

                return null;
            }

            try
            {
                Array localArray = Array.CreateInstance(
                    elementType, lengths, lowerBounds);

                int length = array.Length;

                for (int unused = 0; unused < length; unused++)
                {
                    localArray.SetValue(
                        array.GetValue(indexes), indexes);

                    IncrementIndexes(
                        rank, lowerBounds, lengths, indexes);
                }

                return localArray;
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetBytesFromString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (value == null)
            {
                error = "invalid string";
                return ReturnCode.Error;
            }

            string[] values = value.Split(
                byteSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (values == null)
            {
                error = "could not split string";
                return ReturnCode.Error;
            }

            int length = values.Length;
            byte[] localBytes = new byte[length];

            for (int index = 0; index < length; index++)
            {
                byte byteValue = 0;
                Result localError = null;

                if (Value.GetByte2(
                        values[index], ValueFlags.AnyByte, cultureInfo,
                        ref byteValue, ref localError) == ReturnCode.Ok)
                {
                    localBytes[index] = byteValue;
                }
                else
                {
                    error = localError;
                    return ReturnCode.Error;
                }
            }

            bytes = localBytes;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static ReturnCode GetBytesFromList(
            Interpreter interpreter,
            StringList list,
            Encoding encoding,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (list == null)
            {
                error = "invalid list";
                return ReturnCode.Error;
            }

            if (list.Count == 0)
            {
                bytes = new byte[0];
                return ReturnCode.Ok;
            }

            if ((list.Count == 1) && (interpreter != null))
            {
                IObject @object = null;

                if (interpreter.GetObject(
                        list[0], LookupFlags.NoVerbose,
                        ref @object) == ReturnCode.Ok)
                {
                    object value = @object.Value;

                    if (value == null)
                    {
                        bytes = null;
                        return ReturnCode.Ok;
                    }
                    else if (value is byte[])
                    {
                        bytes = (byte[])value;
                        return ReturnCode.Ok;
                    }
                    else if (value is string)
                    {
                        if (encoding != null)
                        {
                            bytes = encoding.GetBytes((string)value);
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "invalid encoding";
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "object \"{0}\" type mismatch, have {1}, want {2}",
                            list[0], FormatOps.TypeName(value.GetType()),
                            FormatOps.TypeName(typeof(byte[])));

                        return ReturnCode.Error;
                    }
                }
            }

            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.CultureInfo;

            byte[] localBytes = new byte[list.Count];

            for (int index = 0; index < list.Count; index++)
            {
                if (Value.GetByte2(
                        list[index], ValueFlags.AnyByte,
                        cultureInfo, ref localBytes[index],
                        ref error) != ReturnCode.Ok)
                {
                    error = String.Format(
                        "bad byte value at index {0}: {1}",
                        index, error);

                    return ReturnCode.Error;
                }
            }

            bytes = localBytes;
            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SelectRandomValue(
            Interpreter interpreter, /* in: may be NULL. */
            Array array,             /* in */
            ref object value,        /* out */
            ref Result error         /* out */
            )
        {
            if (array == null)
            {
                error = "invalid array";
                return ReturnCode.Error;
            }

            if (array.Rank != 1)
            {
                error = "array must be one-dimensional";
                return ReturnCode.Error;
            }

            if (array.Length == 0)
            {
                error = "array cannot be empty";
                return ReturnCode.Error;
            }

            try
            {
                ulong randomNumber;

                if (interpreter != null)
                    randomNumber = interpreter.GetRandomNumber(); /* throw */
                else
                    randomNumber = RuntimeOps.GetRandomNumber(); /* throw */

                int index = ConversionOps.ToInt(randomNumber %
                    ConversionOps.ToULong(array.LongLength));

                value = array.GetValue(index); /* throw */
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static T[] ToNonNullable<T>(
            T?[] array,
            T @default
            ) where T : struct
        {
            if (array == null)
                return null;

            Array result = Array.CreateInstance(
                typeof(T), array.Length);

            for (int index = array.GetLowerBound(0);
                    index <= array.GetUpperBound(0); index++)
            {
                if (array[index] != null)
                    result.SetValue(array[index], index);
                else
                    result.SetValue(@default, index);
            }

            return (T[])result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Equals(
            byte[] array1,
            byte[] array2
            )
        {
            return Equals(array1, array2, Length.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Equals(
            byte[] array1,
            byte[] array2,
            int length
            )
        {
            return GenericCompareOps<byte>.Equals(array1, array2, length);
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        public static int GetHashCode(
            byte[] array
            )
        {
            return GenericCompareOps<byte>.GetHashCode(array, Length.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetHashCode(
            byte[] array,
            int length
            )
        {
            return GenericCompareOps<byte>.GetHashCode(array, length);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool HasTwoByteCharacter(
            string value,
            ref byte[] bytes
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            if (twoByteEncoding == null)
                return false;

            bytes = twoByteEncoding.GetBytes(value);

            if (bytes == null)
                return false;

            int length = bytes.Length;

            if (length == 0)
                return false;

            if ((length % 2) != 0)
                return false;

            int zeroOffset = (bytes[0] != 0) ? 1 : 0;

            for (int index = 0; index < length; index += 2)
                if (bytes[index + zeroOffset] != 0)
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToHexadecimalString(
            string value
            )
        {
            byte[] bytes = null;

            if (HasTwoByteCharacter(value, ref bytes))
                return ToHexadecimalString(bytes);
            else if (oneByteEncoding != null)
                return ToHexadecimalString(oneByteEncoding.GetBytes(value));
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToHexadecimalString(
            byte[] array
            )
        {
            if (array == null)
                return null;

            StringBuilder result = StringOps.NewStringBuilder();

            int length = array.Length;

            for (int index = 0; index < length; index++)
                result.Append(FormatOps.Hexadecimal(array[index], false));

            return result.ToString();
        }
    }
}
