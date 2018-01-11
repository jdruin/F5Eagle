/*
 * Result.cs --
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
using System.Diagnostics;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("9b092a26-fb6f-4487-ad6e-560ce24f249b")]
    public sealed class Result : IResult, IToString, IString, ICloneable
    {
        #region Public Constants
        public static readonly string NoValue = null;
        public static readonly IClientData NoClientData = null;

        ///////////////////////////////////////////////////////////////////////

        public static readonly Result Null = new Result((object)null);
        public static readonly Result Empty = FromString(String.Empty);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        [DebuggerStepThrough()]
        private Result()
        {
            Clear(); /* set error info to well-known state. */
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support for 
        //       conversions from other data types.
        //
        [DebuggerStepThrough()]
        private Result(
            object value
            )
            : this(ReturnCode.Ok, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        [DebuggerStepThrough()]
        private Result(
            Number value
            )
            : this(ReturnCode.Ok, (value != null) ? value.Value : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private Result(
            Variant value
            )
            : this(ReturnCode.Ok, (value != null) ? value.Value : null)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support
        //       for conversions from the Interpreter data type.
        //
        [DebuggerStepThrough()]
        private Result(
            Interpreter value
            )
            : this(ReturnCode.Ok, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support
        //       for conversions from the Argument data type.
        //
        [DebuggerStepThrough()]
        private Result(
            Argument value
            )
            : this(ReturnCode.Ok, (value != null) ? value.Value : null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is only intended to provide internal support
        //       for the Copy static factory method.
        //
        [DebuggerStepThrough()]
        private Result(
            Result result
            )
            : this()
        {
            if (result != null)
            {
                if (result.value is string)
                {
                    //
                    // NOTE: We know how to make a deep copy of a string.
                    //
                    this.value = result.value; /* Immutable, Deep Copy */
                }
                else
                {
                    //
                    // NOTE: No idea what this is; however, we do not know 
                    //       how to make a deep copy of it; therefore, just 
                    //       refer to it.
                    //
                    this.value = result.value; /* Shallow Copy */
                }

#if CACHE_RESULT_TOSTRING
                this.@string = result.@string; /* Immutable, Deep Copy */
#endif

                this.flags = result.flags; /* ValueType, Deep Copy */
                this.returnCode = result.returnCode; /* ValueType, Deep Copy */
                this.previousReturnCode = result.previousReturnCode; /* ValueType, Deep Copy */
                this.errorLine = result.errorLine; /* ValueType, Deep Copy */
                this.errorCode = result.errorCode; /* Immutable, Deep Copy */
                this.errorInfo = result.errorInfo; /* Immutable, Deep Copy */
                this.exception = result.exception;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is primarily intended for success (Ok)
        //       results.
        //
        [DebuggerStepThrough()]
        private Result(
            ReturnCode returnCode,
            object value
            )
            : this()
        {
            this.returnCode = returnCode;
            this.previousReturnCode = this.returnCode;
            this.value = value;

            if (this.value is string)
            {
                //
                // NOTE: We now have a string result.
                //
                this.flags |= ResultFlags.String;

#if CACHE_RESULT_TOSTRING
                //
                // NOTE: We now have a cached string representation.
                //
                this.@string = (string)this.value;
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This constructor is primarily intended for failure (Error)
        //       results.
        //
        [DebuggerStepThrough()]
        private Result( /* NOT USED */
            ReturnCode returnCode,
            object value,
            int errorLine,
            string errorCode,
            string errorInfo,
            Exception exception
            )
            : this(returnCode, value)
        {
            this.errorLine = errorLine;
            this.errorCode = errorCode;
            this.errorInfo = errorInfo;
            this.exception = exception;

            //
            // NOTE: We now have error info.
            //
            this.flags |= ResultFlags.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        internal void ResetValue(
            Interpreter interpreter,
            bool zero
            )
        {
#if !MONO && NATIVE && WINDOWS
            if (zero && (value is string) && (interpreter != null) &&
                interpreter.HasZeroString())
            {
                ReturnCode zeroCode;
                Result zeroError = null;

                zeroCode = StringOps.ZeroString(
                    (string)value, ref zeroError);

                if (zeroCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, zeroCode, zeroError);
            }
#endif

            value = null;

#if CACHE_RESULT_TOSTRING
#if !MONO && NATIVE && WINDOWS
            if (zero && (@string != null) && (interpreter != null) &&
                interpreter.HasZeroString())
            {
                ReturnCode zeroCode;
                Result zeroError = null;

                zeroCode = StringOps.ZeroString(
                    @string, ref zeroError);

                if (zeroCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, zeroCode, zeroError);
            }
#endif

            @string = null;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        [DebuggerStepThrough()]
        public static object GetValue(
            Result result
            )
        {
            if (result == null)
                return null;

            return result.Value;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static bool SetValue(
            ref Result result,
            object value,
            bool create
            )
        {
            if (result == null)
            {
                if (!create)
                    return false;

                result = new Result(); /* EXEMPT */
            }

            result.Value = value;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        [DebuggerStepThrough()]
        public static Result Copy(
            Result result,
            bool full
            )
        {
            return Copy(null, result, full);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result Copy(
            ReturnCode? returnCode,
            Result result,
            bool full
            )
        {
            Result localResult = null;

            if (result != null) /* garbage in, garbage out */
            {
                localResult = (Result)result.Copy(full);

                if (returnCode != null)
                    localResult.returnCode = (ReturnCode)returnCode;
            }

            return localResult;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Equals Helpers
        [DebuggerStepThrough()]
        internal static bool Equals(
            Result left,
            Result right
            )
        {
            if (Object.ReferenceEquals(left, right))
                return true;

            if ((left == null) || (right == null))
                return false;

            if (!ValueEquals(left.value, right.value))
                return false;

            if (left.flags != right.flags)
                return false;

            if (left.returnCode != right.returnCode)
                return false;

            if (left.previousReturnCode != right.previousReturnCode)
                return false;

            if (left.errorLine != right.errorLine)
                return false;

            if (!String.Equals(left.errorCode, right.errorCode))
                return false;

            if (!String.Equals(left.errorInfo, right.errorInfo))
                return false;

            if (!Object.ReferenceEquals(left.exception, right.exception))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static bool ValueEquals(
            object left,
            object right
            )
        {
            //
            // BUGBUG: This method should probably just use Object.Equals
            //         and nothing else.
            //
            if ((left is string) && (right is string))
                return String.Equals((string)left, (string)right);
            else
                return Object.Equals(left, right);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static String Helpers
        #region Dead Code
#if DEAD_CODE
        [DebuggerStepThrough()]
        private static int Compare(
            Result result1,
            Result result2,
            StringComparison comparisonType
            )
        {
            return String.Compare(ToString(result1, null),
                ToString(result2, null), comparisonType);
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        [DebuggerStepThrough()]
        private static int GetLength(
            Result result,
            object value,
            int @default
            )
        {
            if (value is string)
            {
                return ((string)value).Length;
            }
            else if (value != null)
            {
#if CACHE_RESULT_TOSTRING
                if (result != null)
                {
                    string @string = result.@string;

                    if (@string != null)
                        return @string.Length;

                    @string = value.ToString();
                    result.@string = @string;

                    if (@string != null)
                        return @string.Length;
                    else
                        return @default;
                }
                else
#endif
                {
                    return value.ToString().Length;
                }
            }
            else
            {
                return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static string ToString(
            Result result,
            object value,
            string @default
            )
        {
            if (value is string)
            {
                return (string)value;
            }
            else if (value != null)
            {
#if CACHE_RESULT_TOSTRING
                if (result != null)
                {
                    string @string = result.@string;

                    if (@string != null)
                        return @string;

                    @string = value.ToString();
                    result.@string = @string;

                    return @string;
                }
                else
#endif
                {
                    return value.ToString();
                }
            }
            else
            {
                return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static bool IsSupported(
            Type type
            )
        {
            if (type == null)
                return false;

            if (type == typeof(bool))
            {
                return true;
            }
            else if (type == typeof(byte))
            {
                return true;
            }
            else if (type == typeof(char))
            {
                return true;
            }
            else if (type == typeof(int))
            {
                return true;
            }
            else if (type == typeof(long))
            {
                return true;
            }
            else if (type == typeof(double))
            {
                return true;
            }
            else if (type == typeof(decimal))
            {
                return true;
            }
            else if (type == typeof(string))
            {
                return true;
            }
            else if (type == typeof(DateTime))
            {
                return true;
            }
            else if (type == typeof(TimeSpan))
            {
                return true;
            }
            else if (type == typeof(Guid))
            {
                return true;
            }
            else if (type == typeof(Uri))
            {
                return true;
            }
            else if (type == typeof(Version))
            {
                return true;
            }
            else if (type == typeof(StringBuilder))
            {
                return true;
            }
            else if (type == typeof(CommandBuilder))
            {
                return true;
            }
            else if (type == typeof(Interpreter))
            {
                return true;
            }
            else if (type == typeof(Argument))
            {
                return true;
            }
            else if (type == typeof(ByteList))
            {
                return true;
            }
            else if (type == typeof(ResultList))
            {
                return true;
            }
            else if (type.IsEnum)
            {
                return true;
            }
            else if (RuntimeOps.DoesClassTypeSupportInterface(
                    type, typeof(IStringList)))
            {
                return true;
            }
            else if (RuntimeOps.IsClassTypeEqualOrSubClass(
                    type, typeof(Exception), true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromObject(
            object value,
            bool forceCopy,
            bool supportedOnly,
            bool toString
            )
        {
            if (value == null)
                return null;

            Result result = value as Result;

            if (result != null)
            {
                //
                // NOTE: Otherwise, use the existing reference.
                //
                if (forceCopy)
                    result = new Result(result); /* COPY */
            }
            else if (!supportedOnly || IsSupported(value.GetType()))
            {
                result = new Result(value); /* WRAP */
            }
            else if (toString)
            {
                result = StringOps.GetResultFromObject(value); /* String */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromInterpreter(
            Interpreter value
            )
        {
            return new Result(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromArgument(
            Argument value
            )
        {
            return new Result(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDouble(
            double value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDecimal(
            decimal value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromEnum(
            Enum value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromException(
            Exception value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromVersion(
            Version value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromResultList(
            ResultList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromStringBuilder(
            StringBuilder value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromWideInteger(
            long value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromInteger(
            int value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromBoolean(
            bool value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromCharacter(
            char value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromCharacters(
            char? value1,
            char? value2
            )
        {
            return new Result((object)String.Format("{0}{1}",
                (value1 != null) ? value1.ToString() : null,
                (value2 != null) ? value2.ToString() : null));
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDateTime(
            DateTime value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromTimeSpan(
            TimeSpan value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromGuid(
            Guid value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromUri(
            Uri value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromString(
            string value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromByte(
            byte value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromByteList(
            ByteList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromList(
            IStringList value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static Result FromDictionary(
            IDictionary value
            )
        {
            return new Result((object)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        [DebuggerStepThrough()]
        internal static Result FromCommandBuilder(
            CommandBuilder value
            )
        {
            if (value == null)
                return null;

            return new Result(value.GetResult());
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        private static string ToString(
            Result result,
            string @default
            )
        {
            if (result == null)
                return @default;

            return ToString(result, result.Value, @default);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        [DebuggerStepThrough()]
        public static implicit operator string(
            Result result
            )
        {
            return ToString(result, null);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Interpreter value
            )
        {
            if (value != null)
                return FromInterpreter(value);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Argument value
            )
        {
            if (value != null)
                return FromArgument(value);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringPairList value
            )
        {
            return FromList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            ClientDataDictionary value
            )
        {
            return FromDictionary(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            DateTime value
            )
        {
            return FromDateTime(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            TimeSpan value
            )
        {
            return FromTimeSpan(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Guid value
            )
        {
            return FromGuid(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Uri value
            )
        {
            return FromUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            string value
            )
        {
            return FromString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            byte value
            )
        {
            return FromByte(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            ByteList value
            )
        {
            return FromByteList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            char value
            )
        {
            return FromCharacter(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            double value
            )
        {
            return FromDouble(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            decimal value
            )
        {
            return FromDecimal(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Enum value
            )
        {
            return FromEnum(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Exception value
            )
        {
            return FromException(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            Version value
            )
        {
            return FromVersion(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            ResultList value
            )
        {
            return FromResultList(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            StringBuilder value
            )
        {
            return FromStringBuilder(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            long value
            )
        {
            return FromWideInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            int value
            )
        {
            return FromInteger(value);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public static implicit operator Result(
            bool value
            )
        {
            return FromBoolean(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IString Members
        [DebuggerStepThrough()]
        public int IndexOf(
            string value,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).IndexOf(value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public int IndexOf(
            string value,
            int startIndex,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).IndexOf(
                value, startIndex, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public int LastIndexOf(
            string value,
            StringComparison comparisonType
            )
        {
            return StringOps.LastIndexOf(
                ToString(this, String.Empty), value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public int LastIndexOf(
            string value,
            int startIndex,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).LastIndexOf(
                value, startIndex, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public bool StartsWith(
            string value,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).StartsWith(
                value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public bool EndsWith(
            string value,
            StringComparison comparisonType
            )
        {
            return ToString(this, String.Empty).EndsWith(
                value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Substring(
            int startIndex
            )
        {
            return ToString(this, String.Empty).Substring(startIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Substring(
            int startIndex,
            int length
            )
        {
            return ToString(this, String.Empty).Substring(startIndex, length);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public int Compare(
            string value,
            StringComparison comparisonType
            )
        {
            return String.Compare(ToString(this, null), value, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public int Compare(
            Result value,
            StringComparison comparisonType
            )
        {
            return String.Compare(ToString(this, null),
                ToString(value, null), comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public bool Contains(
            string value,
            StringComparison comparisonType
            )
        {
            return (ToString(this, String.Empty).IndexOf(
                value, comparisonType) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Replace(
            string oldValue,
            string newValue
            )
        {
            return ToString(this, String.Empty).Replace(oldValue, newValue);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Trim()
        {
            return ToString(this, String.Empty).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string Trim(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).Trim(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string TrimStart(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).TrimStart(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string TrimEnd(
            char[] trimChars
            )
        {
            return ToString(this, String.Empty).TrimEnd(trimChars);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public char[] ToCharArray()
        {
            return ToString(this, String.Empty).ToCharArray();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        [DebuggerStepThrough()]
        public string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(
            ToStringFlags flags,
            string @default /* NOT USED */
            )
        {
            return ToString("{0}");
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(
            string format
            )
        {
            return String.Format(format, ToString(this, null));
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public string ToString(string format, int limit, bool strict)
        {
            return FormatOps.Ellipsis(
                String.Format(format, ToString(this, null)), limit, strict);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        [DebuggerStepThrough()]
        public override string ToString()
        {
            return ToString(this, String.Empty);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetValue / ISetValue Members
        private object value;
        public object Value
        {
            [DebuggerStepThrough()]
            get { return value; }
            [DebuggerStepThrough()]
            set
            {
                this.value = value;

                if (this.value is string)
                {
                    //
                    // NOTE: We now have a string result.
                    //
                    this.flags |= ResultFlags.String;

#if CACHE_RESULT_TOSTRING
                    //
                    // NOTE: We now have a cached string representation.
                    //
                    this.@string = (string)this.value;
#endif
                }
                else
                {
                    //
                    // NOTE: We no longer have a string result.
                    //
                    this.flags &= ~ResultFlags.String;

#if CACHE_RESULT_TOSTRING
                    //
                    // NOTE: We no longer have a cached string representation.
                    //
                    this.@string = null;
#endif
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string String
        {
            [DebuggerStepThrough()]
            get { return ToString(this, value, null); }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Length
        {
            [DebuggerStepThrough()]
            get { return GetLength(this, value, 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Private
#if CACHE_RESULT_TOSTRING
        private string @string; /* CACHE */
        internal string CachedString
        {
            [DebuggerStepThrough()]
            get { return @string; }
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            [DebuggerStepThrough()]
            get { return clientData; }
            [DebuggerStepThrough()]
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IValueData Members
        private IClientData valueData;
        public IClientData ValueData
        {
            [DebuggerStepThrough()]
            get { return valueData; }
            [DebuggerStepThrough()]
            set { valueData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData extraData;
        public IClientData ExtraData
        {
            [DebuggerStepThrough()]
            get { return extraData; }
            [DebuggerStepThrough()]
            set { extraData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResult Members
        private ResultFlags flags;
        public ResultFlags Flags
        {
            [DebuggerStepThrough()]
            get { return flags; }
            [DebuggerStepThrough()]
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public void Reset(
            bool full
            )
        {
            //
            // NOTE: If we are doing a "full" reset then clear 
            //       all the error information as well.
            //
            if (full)
                Clear();

            //
            // NOTE: For this object, we always null out the fields (i.e. the
            //       NoValue and NoClientData constants are defined to be null) 
            //       because:
            //
            //       1. Typical usage of this method would be to recycle this 
            //          object for use in an object pool, which really requires
            //          totally cleaned out (null) field values.
            //
            //       2. The existing semantics of this object do not offer any
            //          kind of guarantee that an uninitialized instance will
            //          convert to an empty string (unlike the Argument object).
            //
            value = NoValue;
            clientData = NoClientData;

            //
            // NOTE: We no longer have a string result.
            //
            flags &= ~ResultFlags.String;

#if CACHE_RESULT_TOSTRING
            //
            // NOTE: We no longer have a cached string representation.
            //
            @string = null;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public IResult Copy(
            bool full
            )
        {
            Result result = new Result(this);

            if (!full)
                result.Clear(); /* remove error info */

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public bool HasFlags(
            ResultFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public ResultFlags SetFlags(
            ResultFlags flags,
            bool set
            )
        {
            if (set)
                return (this.flags |= flags);
            else
                return (this.flags &= ~flags);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IError Members
        private ReturnCode returnCode;
        public ReturnCode ReturnCode
        {
            [DebuggerStepThrough()]
            get { return returnCode; }
            [DebuggerStepThrough()]
            set
            {
                //
                // NOTE: Is the return code actually changing?
                //
                if (returnCode != value)
                {
                    //
                    // NOTE: Save the previous return code.
                    //
                    previousReturnCode = returnCode;

                    //
                    // NOTE: Set the new return code.
                    //
                    returnCode = value;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode previousReturnCode;
        public ReturnCode PreviousReturnCode
        {
            [DebuggerStepThrough()]
            get { return previousReturnCode; }
            [DebuggerStepThrough()]
            set { previousReturnCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int errorLine;
        public int ErrorLine
        {
            [DebuggerStepThrough()]
            get { return errorLine; }
            [DebuggerStepThrough()]
            set { errorLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string errorCode;
        public string ErrorCode
        {
            [DebuggerStepThrough()]
            get { return errorCode; }
            [DebuggerStepThrough()]
            set { errorCode = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string errorInfo;
        public string ErrorInfo
        {
            [DebuggerStepThrough()]
            get { return errorInfo; }
            [DebuggerStepThrough()]
            set { errorInfo = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Exception exception;
        public Exception Exception
        {
            [DebuggerStepThrough()]
            get { return exception; }
            [DebuggerStepThrough()]
            set { exception = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public void Clear()
        {
            //
            // NOTE: Clear the error information only.
            //
            returnCode = ReturnCode.Ok;
            previousReturnCode = ReturnCode.Ok;

            errorLine = 0;
            errorCode = null;
            errorInfo = null;

            exception = null;

            flags &= ~ResultFlags.Error; // we cleared error info.
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public bool Save(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                returnCode = interpreter.ReturnCode;
                previousReturnCode = returnCode;

                errorLine = interpreter.ErrorLine; /* EXEMPT */
                errorCode = interpreter.ErrorCode;
                errorInfo = interpreter.ErrorInfo;

                flags |= ResultFlags.Error; // we now have error info.

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        [DebuggerStepThrough()]
        public bool Restore(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                interpreter.ReturnCode = returnCode;

                interpreter.ErrorLine = errorLine;
                interpreter.ErrorCode = errorCode;
                interpreter.ErrorInfo = errorInfo;

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new Result(this);
        }
        #endregion
    }
}
