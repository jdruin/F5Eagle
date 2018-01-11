/*
 * EnumOps.cs --
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
using System.Globalization;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("32db1eb0-d7c8-4a31-82bf-215ae3d9086d")]
    internal static class EnumOps
    {
        #region Private Constants
        //
        // HACK: This is purposely not read-only to allow for ad-hoc
        //       "customization" (i.e. via a script using something
        //       like [object invoke -flags +NonPublic]).
        //
        // NOTE: The default value here is designed to be compatible
        //       with the .NET Framework (internal) semantics for the
        //       treatment of enumerated values as integer values.
        //
        private static bool TreatNullAsWideInteger = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        private static readonly string TryParseMethodName = "TryParse";
#endif

        internal const char AddFlagOperator = Characters.PlusSign;
        internal const char RemoveFlagOperator = Characters.MinusSign;
        internal const char SetFlagOperator = Characters.EqualSign;
        internal const char SetAddFlagOperator = Characters.Colon;
        internal const char KeepFlagOperator = Characters.Ampersand;

        internal static readonly char DefaultFlagOperator = SetAddFlagOperator;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
#if NET_40
        private static object enumTryParseSyncRoot = new object();
        private static MethodInfo enumTryParse = null;
        private static Dictionary<Type, MethodInfo> enumTryParseCache;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Enum Cache Data
        private static object enumCacheSyncRoot = new object();
        private static TypePairDictionary<StringList, UlongList> enumCache = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Enum Cache Methods
        public static int ClearEnumCache()
        {
            int result = ClearEnumNamesAndValuesCache();

#if NET_40
            result += ClearEnumTryParseCache();
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int ClearEnumNamesAndValuesCache()
        {
            lock (enumCacheSyncRoot) /* TRANSACTIONAL */
            {
                if (enumCache == null)
                    return Count.Invalid;

                int result = enumCache.Count;

                enumCache.Clear();
                enumCache = null;

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        private static int ClearEnumTryParseCache()
        {
            lock (enumTryParseSyncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (enumTryParse != null)
                {
                    result++;

                    enumTryParse = null;
                }

                if (enumTryParseCache != null)
                {
                    result += enumTryParseCache.Count;

                    enumTryParseCache.Clear();
                    enumTryParseCache = null;
                }

                return result;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static bool IsFlagsEnum(
            Type enumType
            )
        {
            if ((enumType == null) || !enumType.IsEnum)
                return false;

            return enumType.IsDefined(typeof(FlagsAttribute), false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        public static object ConvertToTypeCodeValue(
            Type enumType,
            object value,
            ref TypeCode typeCode,
            ref Result error
            )
        {
            if (enumType != null)
            {
                object enumValue = TryGetEnum(enumType, value, ref error);

                if (enumValue != null)
                {
                    IConvertible convertible = enumValue as IConvertible;

                    if (convertible != null)
                    {
                        try
                        {
                            typeCode = Convert.GetTypeCode(enumValue);

                            switch (typeCode)
                            {
                                case TypeCode.Byte:
                                    return convertible.ToByte(null);
                                case TypeCode.SByte:
                                    return convertible.ToSByte(null);
                                case TypeCode.Int16:
                                    return convertible.ToInt16(null);
                                case TypeCode.UInt16:
                                    return convertible.ToUInt16(null);
                                case TypeCode.Int32:
                                    return convertible.ToInt32(null);
                                case TypeCode.UInt32:
                                    return convertible.ToUInt32(null);
                                case TypeCode.Int64:
                                    return convertible.ToInt64(null);
                                case TypeCode.UInt64:
                                    return convertible.ToUInt64(null);
                                default:
                                    {
                                        error = String.Format(
                                            "enumerated type \"{0}\" value " +
                                            "\"{1}\" has unsupported type " +
                                            "code \"{2}\"", enumType, enumValue,
                                            typeCode);

                                        break;
                                    }
                            }
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "enumerated type \"{0}\" value " +
                            "is not convertible", enumType);
                    }
                }
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TryGetEnum(
            Type enumType,
            object value,
            ref Result error
            )
        {
            if (enumType != null)
            {
                if (enumType.IsEnum)
                {
                    try
                    {
                        //
                        // NOTE: Try to get the value as the specified enum
                        //       type.
                        //
                        return Enum.ToObject(enumType, value);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = String.Format(
                        "type \"{0}\" is not an enumeration",
                        enumType.FullName);
                }
            }
            else
            {
                error = "invalid type";
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Support Methods
        private static bool ShouldIgnoreLeading(
            string value,
            bool ignoreLeading
            )
        {
            if ((value == null) || !ignoreLeading)
                return false;

            int length = value.Length;

            if (length < 2)
                return false;

            char firstCharacter = value[0];
            char secondCharacter = value[1];

            if (Parser.IsIdentifier(firstCharacter))
                return false;

            if (!Parser.IsIdentifier(secondCharacter))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldTreatAsWideInteger(
            Enum value
            )
        {
            if (value == null)
                return TreatNullAsWideInteger; /* NOTE: Per framework classes. */

            Type type = value.GetType();

            if ((type == null) || !type.IsEnum)
                return false;

            return ShouldTreatAsWideInteger(Enum.GetUnderlyingType(type));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ShouldTreatAsWideInteger(
            Type type
            )
        {
            if (type == null)
                return TreatNullAsWideInteger; /* NOTE: Per framework classes. */

            return (type == typeof(long)) || (type == typeof(ulong));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string FixupFlagsEnumString(
            string value
            )
        {
            string result = value;

            if (!String.IsNullOrEmpty(result))
            {
                char[] characters = {
                    Characters.Comma, Characters.Pipe, Characters.SemiColon
                };

                if (result.IndexOfAny(characters) != Index.Invalid)
                {
                    StringBuilder builder = StringOps.NewStringBuilder(result);

                    for (int index = 0; index < characters.Length; index++)
                    {
                        builder = builder.Replace(
                            characters[index], Characters.Space);
                    }

                    result = builder.ToString();
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void CommitEnumNamesAndValues(
            ref StringList enumNames,
            ref UlongList enumValues,
            IEnumerable<string> newEnumNames,
            IEnumerable<ulong> newEnumValues,
            bool forceCopy
            )
        {
            if (newEnumNames != null)
            {
                if (enumNames != null)
                    enumNames.AddRange(newEnumNames);
                else if (!forceCopy && (newEnumNames is StringList))
                    enumNames = (StringList)newEnumNames;
                else
                    enumNames = new StringList(newEnumNames);
            }

            if (newEnumValues != null)
            {
                if (enumValues != null)
                    enumValues.AddRange(newEnumValues);
                else if (!forceCopy && (newEnumValues is UlongList))
                    enumValues = (UlongList)newEnumValues;
                else
                    enumValues = new UlongList(newEnumValues);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private TryParse*Enum Methods
        private static object TryParseIntegerEnum(
            Type enumType,
            string value,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            try
            {
                if (cultureInfo == null)
                    cultureInfo = Value.GetDefaultCulture();

                uint[] uintValue = { 0, 0 };

                if (Value.GetUnsignedInteger2(value,
                        ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                        cultureInfo, ref uintValue[0],
                        ref error) == ReturnCode.Ok)
                {
                    object enumValue = TryGetEnum(
                        enumType, uintValue[0], ref error);

                    if (enumValue != null)
                    {
                        //
                        // BUGFIX: Verify that the returned Enum value
                        //         is numerically identical to the parsed
                        //         unsigned integer.  This makes things a
                        //         bit slower; however, it is necessary.
                        //
                        uintValue[1] = ToUInt(
                            enumType, enumValue as Enum, cultureInfo);

                        if ((enumValue != null) &&
                            (enumValue.GetType() == enumType) &&
                            (uintValue[0] == uintValue[1]))
                        {
                            return enumValue;
                        }
                        else
                        {
                            error = String.Format(
                                "bad \"{0}\", unsigned integer value " +
                                "{1} (parsed from \"{2}\"), does not match " +
                                "converted unsigned integer value {3}",
                                enumType.FullName, uintValue[0], value,
                                uintValue[1]);
                        }
                    }
                }
                else
                {
                    int[] intValue = { 0, 0 };

                    if (Value.GetInteger2(
                            value, ValueFlags.AnyWideInteger,
                            cultureInfo, ref intValue[0],
                            ref error) == ReturnCode.Ok)
                    {
                        object enumValue = TryGetEnum(
                            enumType, intValue[0], ref error);

                        if (enumValue != null)
                        {
                            //
                            // BUGFIX: Verify that the returned Enum value
                            //         is numerically identical to the parsed
                            //         signed integer.  This makes things a
                            //         bit slower; however, it is necessary.
                            //
                            intValue[1] = ToInt(
                                enumType, enumValue as Enum, cultureInfo);

                            if ((enumValue != null) &&
                                (enumValue.GetType() == enumType) &&
                                (intValue[0] == intValue[1]))
                            {
                                return enumValue;
                            }
                            else
                            {
                                error = String.Format(
                                    "bad \"{0}\", integer value {1} " +
                                    "(parsed from \"{2}\"), does not match " +
                                    "converted integer value {3}",
                                    enumType.FullName, intValue[0], value,
                                    intValue[1]);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object TryParseWideIntegerEnum(
            Type enumType,
            string value,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            try
            {
                if (cultureInfo == null)
                    cultureInfo = Value.GetDefaultCulture();

                ulong[] ulongValue = { 0, 0 };

                if (Value.GetUnsignedWideInteger2(value,
                        ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                        cultureInfo, ref ulongValue[0],
                        ref error) == ReturnCode.Ok)
                {
                    object enumValue = TryGetEnum(
                        enumType, ulongValue[0], ref error);

                    if (enumValue != null)
                    {
                        //
                        // BUGFIX: Verify that the returned Enum value
                        //         is numerically identical to the parsed
                        //         unsigned long integer.  This makes
                        //         things a bit slower; however, it is
                        //         necessary.
                        //
                        ulongValue[1] = ToULong(
                            enumType, enumValue as Enum, cultureInfo);

                        if ((enumValue != null) &&
                            (enumValue.GetType() == enumType) &&
                            (ulongValue[0] == ulongValue[1]))
                        {
                            return enumValue;
                        }
                        else
                        {
                            error = String.Format(
                                "bad \"{0}\", unsigned wide integer value " +
                                "{1} (parsed from \"{2}\"), does not match " +
                                "converted unsigned wide integer value {3}",
                                enumType.FullName, ulongValue[0], value,
                                ulongValue[1]);
                        }
                    }
                }
                else
                {
                    long[] longValue = { 0, 0 };

                    if (Value.GetWideInteger2(
                            value, ValueFlags.AnyWideInteger,
                            cultureInfo, ref longValue[0],
                            ref error) == ReturnCode.Ok)
                    {
                        object enumValue = TryGetEnum(
                            enumType, longValue[0], ref error);

                        if (enumValue != null)
                        {
                            //
                            // BUGFIX: Verify that the returned Enum value
                            //         is numerically identical to the parsed
                            //         signed long integer.  This makes things
                            //         a bit slower; however, it is necessary.
                            //
                            longValue[1] = ToLong(
                                enumType, enumValue as Enum, cultureInfo);

                            if ((enumValue != null) &&
                                (enumValue.GetType() == enumType) &&
                                (longValue[0] == longValue[1]))
                            {
                                return enumValue;
                            }
                            else
                            {
                                error = String.Format(
                                    "bad \"{0}\", wide integer value {1} " +
                                    "(parsed from \"{2}\"), does not match " +
                                    "converted wide integer value {3}",
                                    enumType.FullName, longValue[0], value,
                                    longValue[1]);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object TryParseSomeKindOfIntegerEnum(
            Type enumType,
            string value,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            Type elementType = null;

            if (!MarshalOps.IsEnumType(
                    enumType, true, true, ref elementType))
            {
                error = String.Format(
                    "type \"{0}\" is not an enumeration",
                    enumType.FullName);

                return null;
            }

            if (ShouldTreatAsWideInteger(elementType))
            {
                return TryParseWideIntegerEnum(
                    enumType, value, cultureInfo, ref error);
            }
            else
            {
                return TryParseIntegerEnum(
                    enumType, value, cultureInfo, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public TryParseEnum Methods
        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase
            )
        {
            Result error = null;

            return TryParseEnum(enumType, value, allowInteger, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase,
            ref Result error
            )
        {
#if NET_40
            return TryParseEnumBuiltIn(enumType, value, allowInteger, true, true, noCase, ref error);
#else
            return TryParseEnumFast(enumType, value, allowInteger, true, true, noCase, ref error);
            // return TryParseEnumSlow(enumType, value, allowInteger, true, true, noCase, ref error);
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private TryParseEnumBuiltIn Methods (.NET Framework 4.0)
#if NET_40
        private static MethodInfo GetEnumTryParseMethodInfo(
            ref Result error
            )
        {
            try
            {
                lock (enumTryParseSyncRoot) /* TRANSACTIONAL */
                {
                    if (enumTryParse != null)
                    {
                        //
                        // NOTE: Return the existing (cached) method object.
                        //
                        return enumTryParse;
                    }
                    else
                    {
                        MethodInfo[] methodInfo = typeof(Enum).GetMethods(
                            MarshalOps.PublicStaticMethodBindingFlags);

                        if (methodInfo != null)
                        {
                            for (int index = 0; index < methodInfo.Length; index++)
                            {
                                if (methodInfo[index] != null)
                                {
                                    if (String.Compare(methodInfo[index].Name,
                                            TryParseMethodName,
                                            StringOps.SystemStringComparisonType) == 0)
                                    {
                                        ParameterInfo[] parameterInfo =
                                            methodInfo[index].GetParameters();

                                        if (parameterInfo != null)
                                        {
                                            //
                                            // NOTE: We expect there to be exactly 3 parameters.
                                            //       The first is a string, the second is a boolean,
                                            //       and the final one is a reference to a generic
                                            //       ValueType constrainted type.
                                            //
                                            if (parameterInfo.Length == 3)
                                            {
                                                if ((parameterInfo[0].ParameterType == typeof(string)) &&
                                                    (parameterInfo[1].ParameterType == typeof(bool)) &&
                                                    parameterInfo[2].ParameterType.IsByRef &&
                                                    parameterInfo[2].ParameterType.ContainsGenericParameters)
                                                {
                                                    enumTryParse = methodInfo[index];

                                                    return enumTryParse;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        error = String.Format(
                            "cannot find \"{0}\" method of \"{1}\" type",
                            TryParseMethodName, typeof(Enum));
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static MethodInfo GetEnumTryParseMethodInfo(
            Type enumType,
            ref Result error
            )
        {
            if (enumType != null)
            {
                if (enumType.IsEnum)
                {
                    try
                    {
                        lock (enumTryParseSyncRoot) /* TRANSACTIONAL */
                        {
                            if (enumTryParseCache == null)
                                enumTryParseCache = new Dictionary<Type, MethodInfo>();

                            MethodInfo methodInfo;

                            if (enumTryParseCache.TryGetValue(enumType, out methodInfo) &&
                                (methodInfo != null))
                            {
                                //
                                // NOTE: Return the cached TryParse method for this enumeration.
                                //
                                return methodInfo;
                            }
                            else
                            {
                                methodInfo = GetEnumTryParseMethodInfo(ref error);

                                if (methodInfo != null)
                                {
                                    //
                                    // NOTE: Construct the TryParse method with the enumeration
                                    //       type we have been passed.
                                    //
                                    methodInfo = methodInfo.MakeGenericMethod(enumType);

                                    if (methodInfo != null)
                                    {
                                        //
                                        // NOTE: Cache this TryParse method for later and then
                                        //       return it.
                                        //
                                        enumTryParseCache.Add(enumType, methodInfo);

                                        return methodInfo;
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "cannot use type \"{0}\" to make generic \"{1}\" method",
                                            enumType.FullName, TryParseMethodName);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = String.Format(
                        "type \"{0}\" is not an enumeration",
                        enumType.FullName);
                }
            }
            else
            {
                error = "invalid type";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object TryParseEnumBuiltIn(
            Type enumType,
            string value,
            bool allowInteger,
            bool ignoreLeading,
            bool strict, /* NOT USED */
            bool noCase,
            ref Result error
            )
        {
            object result = null;

            if (enumType != null)
            {
                if (enumType.IsEnum)
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        //
                        // HACK: This call assumes that no "field" of an enumerated type
                        //       can begin with a digit, plus sign, or minus sign.  This
                        //       is always true in C# because the fields are identifiers
                        //       and no identifier in C# may begin with anything except
                        //       a letter or underscore.  However, this may not be true
                        //       for other languages built on the CLR.
                        //
                        Result integerError = null;

                        if (allowInteger && Parser.IsInteger(value[0], true))
                        {
                            result = TryParseSomeKindOfIntegerEnum(
                                enumType, value, null, ref integerError);
                        }

                        if (result == null)
                        {
                            MethodInfo methodInfo = GetEnumTryParseMethodInfo(
                                enumType, ref error);

                            if (methodInfo != null)
                            {
                                try
                                {
                                    //
                                    // NOTE: Try to parse the name without the leading
                                    //       character if it is not an identifier
                                    //       character.
                                    //
                                    if (ShouldIgnoreLeading(value, ignoreLeading))
                                        value = value.Substring(1);

                                    //
                                    // NOTE: We expect the [generic] Enum.TryParse method to
                                    //       accept exactly 3 arguments.  The first argument
                                    //       must be a string.  The second argument must be
                                    //       a boolean.  The third argument is supposed to
                                    //       be a ByRef generic enumerated type.
                                    //
                                    object[] args = { value, noCase, null };

                                    //
                                    // NOTE: Attempt to invoke the TryParse method that has
                                    //       been fully constructed for the proper enumeration
                                    //       type.
                                    //
                                    bool? success = methodInfo.Invoke(null, args) as bool?;

                                    //
                                    // NOTE: Make sure the method returned a boolean and that it
                                    //       has a non-zero value.
                                    //
                                    if ((success != null) && (bool)success)
                                    {
                                        //
                                        // NOTE: Success, extract the third argument as the
                                        //       enumeration valued parsed result.
                                        //
                                        result = args[2]; /* cannot fail */
                                    }
                                    else
                                    {
                                        error = ScriptOps.BadValue(
                                            null, String.Format("\"{0}\" value",
                                            enumType.FullName), value,
                                            Enum.GetNames(enumType), null, null);
                                    }
                                }
                                catch (Exception e)
                                {
                                    error = e;
                                }
                            }
                        }

                        if (result == null)
                            error = ResultOps.MaybeCombine(error, integerError);
                    }
                    else
                    {
                        error = String.Format(
                            "invalid \"{0}\" value",
                            enumType.FullName);
                    }
                }
                else
                {
                    error = String.Format(
                        "type \"{0}\" is not an enumeration",
                        enumType.FullName);
                }
            }
            else
            {
                error = "invalid type";
            }

            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region GetEnumNamesAndValues Methods (.NET Framework 2.0, 3.5, 4.0, and Mono)
        public static ReturnCode GetEnumNamesAndValues(
            Type enumType,
            ref StringList enumNames,
            ref UlongList enumValues,
            ref Result error
            )
        {
#if !NET_40 && !MONO
            if (!CommonOps.Runtime.IsMono())
                return GetEnumNamesAndValuesFast(enumType, ref enumNames, ref enumValues, ref error);
            else
#endif
                return GetEnumNamesAndValuesSlow(enumType, ref enumNames, ref enumValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_40 && !MONO
        private static ReturnCode GetEnumNamesAndValuesFast(
            Type enumType,
            ref StringList enumNames,
            ref UlongList enumValues,
            ref Result error
            )
        {
            if (enumType != null)
            {
                if (enumType.IsEnum)
                {
                    #region Disabled Code
#if NET_20_FAST_ENUM
                    int retry = 0;

                fallback:

                    if (!CommonOps.Runtime.IsMono() &&
                        (retry == 0) &&
                        (enumType.BaseType == typeof(Enum)))
                    {
                        try
                        {

                        retry:

                            object[] args = { enumType, null, null };

                            //
                            // HACK: This method is private in the .NET Framework and we
                            //       need to use it; therefore, we use reflection to
                            //       invoke it.
                            //
                            // BUGBUG: Something inside this method occasionally throws
                            //         System.ExecutionEngineException.  It appears to be
                            //         some kind of race condition with the GC.
                            //
                            typeof(Enum).InvokeMember(MarshalOps.GetEnumValuesMethodName,
                                MarshalOps.PrivateStaticMethodBindingFlags, null,
                                null, args); /* throw */

                            //
                            // BUGBUG: Why is this required here?  The above call, seems to
                            //         intermittently fail on our custom "Boolean" enumeration
                            //         type; however, the operation succeeds upon being retried?
                            //
                            if ((retry < 1) && ((args == null) || (args[1] == null) ||
                                (args[2] == null)))
                            {
                                retry++;

                                goto retry;
                            }

                            //
                            // HACK: If we still fail, fallback on the known-good method.
                            //
                            if ((args == null) || (args[1] == null) || (args[2] == null))
                            {
                                retry++;

                                goto fallback;
                            }

                            CommitEnumNamesAndValues(
                                ref enumNames, ref enumValues, (string[])args[2],
                                (ulong[])args[1], false);

                            return ReturnCode.Ok;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                    }
                    else
#endif
                    #endregion
                    {
                        //
                        // NOTE: *FALLBACK* In case the fast method does not
                        //       work.  Even though the InternalGetEnumValues
                        //       does fail on occasion, it does not seem to
                        //       do so repeatedly; therefore, this code path
                        //       should be rarely used when not running on
                        //       Mono.
                        //
                        return GetEnumNamesAndValuesSlow(
                            enumType, ref enumNames, ref enumValues,
                            ref error);
                    }
                }
                else
                {
                    error = String.Format(
                        "type \"{0}\" is not an enumeration",
                        enumType.FullName);
                }
            }
            else
            {
                error = "invalid type";
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetEnumNamesAndValuesSlow(
            Type enumType,
            ref StringList enumNames,
            ref UlongList enumValues,
            ref Result error
            )
        {
            try
            {
                lock (enumCacheSyncRoot) /* TRANSACTIONAL */
                {
                    if (enumCache == null)
                        enumCache = new TypePairDictionary<StringList, UlongList>();

                retry:

                    IAnyPair<StringList, UlongList> anyPair;

                    if (enumCache.TryGetValue(enumType, out anyPair))
                    {
                        if (anyPair == null)
                        {
                            //
                            // NOTE: This cache entry is missing?  Try to remove it
                            //       from the cache and just fetch the names/values
                            //       for this enum type again.
                            //
                            enumCache.Remove(enumType);

                            goto retry;
                        }

                        StringList names = anyPair.X;
                        UlongList values = anyPair.Y;

                        if ((names == null) || (values == null))
                        {
                            //
                            // NOTE: This cache entry is corrupted?  Try to remove it
                            //       from the cache and just fetch the names/values
                            //       for this enum type again.
                            //
                            enumCache.Remove(enumType);

                            goto retry;
                        }

                        CommitEnumNamesAndValues(
                            ref enumNames, ref enumValues, names, values,
                            false);
                    }
                    else
                    {
                        PairList<object> pairs = new PairList<object>();

                        //
                        // NOTE: Get all the static public fields, these are
                        //       the values.
                        //
                        FieldInfo[] fieldInfo = enumType.GetFields(
                            MarshalOps.EnumFieldBindingFlags);

                        if (fieldInfo != null)
                        {
                            foreach (FieldInfo thisFieldInfo in fieldInfo)
                            {
                                //
                                // NOTE: Add the name and the value itself to the
                                //       list (via our Pair object).
                                //
                                pairs.Add(new ObjectPair(thisFieldInfo.Name,
                                    thisFieldInfo.GetValue(null)));
                            }
                        }

                        //
                        // NOTE: Sort the list based on the underlying integral
                        //       values.
                        //
                        pairs.Sort(new _Comparers.Pair<object>(
                            PairComparison.LYRY, true)); /* throw */

                        //
                        // NOTE: Populate the result lists.
                        //
                        StringList names = new StringList();
                        UlongList values = new UlongList();

                        foreach (Pair<object> pair in pairs)
                        {
                            names.Add((string)pair.X); /* throw */

                            values.Add(ToULong(
                                enumType, (Enum)pair.Y, null)); /* throw */
                        }

                        //
                        // NOTE: Commit changes to the variables provided by
                        //       the caller.
                        //
                        CommitEnumNamesAndValues(
                            ref enumNames, ref enumValues, names, values,
                            false);

                        //
                        // NOTE: Save in the cache for the next usage (this is
                        //       especially important for commonly used enum
                        //       types like Boolean).
                        //
                        enumCache.Add(enumType,
                            new AnyPair<StringList, UlongList>(names, values));
                    }

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private TryParseEnumFast Methods (.NET Framework 2.0, 3.5, and Mono)
        private static object TryParseEnumFast(
            Type enumType,
            string value,
            bool allowInteger,
            bool ignoreLeading,
            bool strict,
            bool noCase,
            ref Result error
            )
        {
            object result = null;

            if (enumType != null)
            {
                if (enumType.IsEnum)
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        //
                        // HACK: This call assumes that no "field" of an enumerated type
                        //       can begin with a digit, plus sign, or minus sign.  This
                        //       is always true in C# because the fields are identifiers
                        //       and no identifier in C# may begin with anything except
                        //       a letter or underscore.  However, this may not be true
                        //       for other languages built on the CLR.
                        //
                        Result integerError = null;

                        if (allowInteger && Parser.IsInteger(value[0], true))
                        {
                            result = TryParseSomeKindOfIntegerEnum(
                                enumType, value, null, ref integerError);
                        }

                        if (result == null)
                        {
                            //
                            // NOTE: Get the list of possible values for this enumeration
                            //       type.
                            //
                            StringList enumNames = null;
                            UlongList enumValues = null;

                            if (GetEnumNamesAndValues(
                                    enumType, ref enumNames, ref enumValues,
                                    ref error) == ReturnCode.Ok)
                            {
                                ulong newValue = 0;

                                //
                                // NOTE: Break the string into multiple values, if
                                //       necessary.
                                //
                                string[] values = value.Split(Characters.Comma);

                                //
                                // NOTE: Figure out how we would like to compare name
                                //       strings.
                                //
                                if (values != null)
                                {
                                    StringComparison comparisonType = noCase ?
                                        StringOps.SystemNoCaseStringComparisonType :
                                        StringOps.SystemStringComparisonType;

                                    for (int index = 0; index < values.Length; index++)
                                    {
                                        //
                                        // NOTE: Grab the current item and clean it.
                                        //
                                        string item = values[index].Trim();

                                        //
                                        // NOTE: Skip over empty entries.
                                        //
                                        if (!String.IsNullOrEmpty(item))
                                        {
                                            //
                                            // NOTE: Try to find the name in the list of valid
                                            //       ones for this enumerated type.
                                            //
                                            int index2 = enumNames.IndexOf(item, 0, comparisonType);

                                            //
                                            // NOTE: Try to find the name without the leading
                                            //       character if it is not an identifier
                                            //       character.
                                            //
                                            if ((index2 == Index.Invalid) &&
                                                ShouldIgnoreLeading(item, ignoreLeading))
                                            {
                                                index2 = enumNames.IndexOf(
                                                    item.Substring(1), 0, comparisonType);
                                            }

                                            //
                                            // NOTE: Did we find the name in the list of valid
                                            //       ones for this enumerated type?
                                            //
                                            if (index2 != Index.Invalid)
                                            {
                                                //
                                                // NOTE: Found it.  Combine the underlying value
                                                //       with our result so far.
                                                //
                                                newValue |= enumValues[index2];
                                            }
                                            else if (strict)
                                            {
                                                error = ScriptOps.BadValue(
                                                    null, String.Format("\"{0}\" value",
                                                    enumType.FullName), item,
                                                    Enum.GetNames(enumType), null, null);

                                                goto error;
                                            }
                                        }
                                    }
                                }

                                result = TryGetEnum(enumType, newValue, ref error);
                            }
                        }

                    error:

                        if (result == null)
                            error = ResultOps.MaybeCombine(error, integerError);
                    }
                    else
                    {
                        error = String.Format(
                            "invalid \"{0}\" value",
                            enumType.FullName);
                    }
                }
                else
                {
                    error = String.Format(
                        "type \"{0}\" is not an enumeration",
                        enumType.FullName);
                }
            }
            else
            {
                error = "invalid type";
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
        #region Private TryParseEnumSlow Methods (Obsolete)
#if DEAD_CODE
        [Obsolete()]
        private static object TryParseEnumSlow( /* NOT USED */
            Type enumType,
            string value,
            bool allowInteger, /* NOT USED */
            bool ignoreLeading,
            bool strict, /* NOT USED */
            bool noCase,
            ref Result error
            )
        {
            if (enumType != null)
            {
                if (enumType.IsEnum)
                {
                    if (!String.IsNullOrEmpty(value))
                    {
                        try
                        {
                            //
                            // NOTE: First, try for an exact match.
                            //
                            // NOTE: No TryParse for enumerations, eh?
                            //
                            return Enum.Parse(
                                enumType, value, noCase); /* throw */
                        }
                        catch
                        {
                            if (ShouldIgnoreLeading(value, ignoreLeading))
                            {
                                try
                                {
                                    //
                                    // NOTE: Ok, now try to remove a leading
                                    //       non-identifier character.
                                    //
                                    // NOTE: No TryParse for enumerations, eh?
                                    //
                                    return Enum.Parse(
                                        enumType, value.Substring(1),
                                        noCase); /* throw */
                                }
                                catch
                                {
                                    // do nothing.
                                }
                            }
                        }
                    }

                    error = ScriptOps.BadValue(
                        null, null, value,
                        Enum.GetNames(enumType), null, null);
                }
                else
                {
                    error = String.Format(
                        "type \"{0}\" is not an enumeration",
                        enumType.FullName);
                }
            }
            else
            {
                error = "invalid type";
            }

            return null;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private ToInt / ToUInt Methods
        private static int ToInt(
            Type enumType,
            Enum value,
            CultureInfo cultureInfo
            ) /* LOSSY */
        {
            return ConversionOps.ToInt(ToLong(enumType, value, cultureInfo));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static uint ToUInt(
            Type enumType,
            Enum value,
            CultureInfo cultureInfo
            ) /* LOSSY */
        {
            return ConversionOps.ToUInt(ToULong(enumType, value, cultureInfo));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static uint ToUInt(
            Enum value
            ) /* SAFE */
        {
            return ToUInt(
                (value != null) ? value.GetType() : null, value, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private ToLong / ToULong Methods
#if !MONO
        private static long ToLongFast(
            Type enumType,
            Enum value,
            CultureInfo cultureInfo
            ) /* SAFE */
        {
            return ConversionOps.ToLong(ToULongFast(
                enumType, value, cultureInfo)); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ulong ToULongFast(
            Type enumType, /* NOT USED */
            Enum value,
            CultureInfo cultureInfo /* NOT USED */
            ) /* SAFE */
        {
            //
            // HACK: This method is private in the .NET Framework and we
            //       need to use it; therefore, we use reflection to
            //       invoke it.
            //
            return (ulong)typeof(Enum).InvokeMember("ToUInt64",
                MarshalOps.PrivateStaticMethodBindingFlags,
                null, null, new object[] { value }); /* throw */
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long ToLongSlow(
            Type enumType,
            Enum value,
            CultureInfo cultureInfo
            ) /* SAFE */
        {
            //
            // NOTE: Use the routine for converting to an unsigned long integer
            //       and then safely convert it to a signed long integer.
            //
            return ConversionOps.ToLong(ToULongSlow(enumType, value, cultureInfo));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ulong ToULongSlow(
            Type enumType, /* NOT USED */
            Enum value,
            CultureInfo cultureInfo
            ) /* SAFE */
        {
            if (cultureInfo == null)
                cultureInfo = Value.GetDefaultCulture();

            TypeCode typeCode = Convert.GetTypeCode(value);

            switch (typeCode)
            {
                case TypeCode.Boolean: /* signed, based on int */
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    {
                        //
                        // NOTE: Signed value, convert via int64.
                        //
                        return ConversionOps.ToULong(Convert.ToInt64(
                            value, cultureInfo));
                    }
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        //
                        // NOTE: Unsigned value, convert directly to uint64.
                        //
                        return Convert.ToUInt64(value, cultureInfo);
                    }
                default:
                    {
                        //
                        // NOTE: We have no idea what this is, punt.
                        //
                        throw new ScriptException(String.Format(
                            "enum type mismatch, type code \"{0}\" is not supported",
                            typeCode));
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static long ToLong(
            Type enumType,
            Enum value,
            CultureInfo cultureInfo
            ) /* SAFE */
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
                return ToLongFast(enumType, value, cultureInfo);
            else
#endif
                return ToLongSlow(enumType, value, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ulong ToULong(
            Type enumType,
            Enum value,
            CultureInfo cultureInfo
            ) /* SAFE */
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
                return ToULongFast(enumType, value, cultureInfo);
            else
#endif
                return ToULongSlow(enumType, value, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ulong ToULong(
            Enum value
            ) /* SAFE */
        {
            return ToULong(
                (value != null) ? value.GetType() : null, value, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public ToLong / ToULong Methods
        public static long ToLong(
            Enum value
            ) /* SAFE */
        {
            return ToLong(
                (value != null) ? value.GetType() : null, value, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public ToUIntOrULong Methods
        public static ulong ToUIntOrULong(
            Enum value
            ) /* SAFE */
        {
            return ShouldTreatAsWideInteger(value) ?
                ToULong(value) : ConversionOps.ToUInt(ToUInt(value));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public TryParseFlagsEnum Methods
        public static object TryParseFlagsEnum(
            Interpreter interpreter,
            Type enumType,
            string oldValue,
            string newValue,
            CultureInfo cultureInfo,
            bool allowInteger,
            bool strict,
            bool noCase
            )
        {
            Result error = null;

            return TryParseFlagsEnum(
                interpreter, enumType, oldValue, newValue, cultureInfo,
                allowInteger, strict, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object TryParseFlagsEnum(
            Interpreter interpreter,
            Type enumType,
            string oldValue,
            string newValue,
            CultureInfo cultureInfo,
            bool allowInteger,
            bool strict,
            bool noCase,
            ref Result error
            )
        {
            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type \"{0}\" is not an enumeration",
                    enumType.FullName);

                return null;
            }

            if (!String.IsNullOrEmpty(newValue))
            {
                //
                // HACK: If necessary, transform common flag delimiters to spaces
                //       so that we can try to parse the value as a list.  This
                //       treatment of delimiters may be too liberal; however, it
                //       does make enumeration values easier to use from scripts.
                //
                newValue = FixupFlagsEnumString(newValue);

                //
                // NOTE: Parse value into a list of enum values and try to parse
                //       each one.
                //
                StringList list = null;

                if (Parser.SplitList(
                        interpreter, newValue, 0, Length.Invalid, true,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    bool haveOldValue = false;
                    Result localError = null;
                    object enumValue;

                    if (!String.IsNullOrEmpty(oldValue))
                    {
                        haveOldValue = true;

                        enumValue = TryParseEnum(
                            enumType, oldValue, allowInteger, noCase,
                            ref localError);
                    }
                    else
                    {
                        enumValue = TryGetEnum(enumType, 0, ref localError);
                    }

                    if (enumValue != null)
                    {
                        //
                        // NOTE: Some of the Enum related operations within may throw
                        //       exceptions.  We need to catch them and return an
                        //       appropriate error.
                        //
                        try
                        {
                            //
                            // NOTE: The initial default operator is ":"; however, this
                            //       may be changed from within the loop for subsequent
                            //       flagging operations.
                            //
                            char @operator = DefaultFlagOperator;

                            for (int index = 0; index < list.Count; index++)
                            {
                                string item = list[index];

                                //
                                // NOTE: We simply ignore invalid and empty list items.
                                //
                                if (!String.IsNullOrEmpty(item))
                                {
                                    //
                                    // NOTE: This used to check if the character
                                    //       was a letter or an underscore (i.e.
                                    //       but not a digit) because it was
                                    //       assumed that numeric values would
                                    //       not be used for flag values; however,
                                    //       this behavior has now been changed.
                                    //
                                    char character = item[0];

                                    if (!Parser.IsIdentifier(character))
                                    {
                                        //
                                        // NOTE: Must be some kind of operator
                                        //       (this will be validated below).
                                        //
                                        @operator = character;

                                        //
                                        // NOTE: Skip over the leading operator
                                        //       character and get the rest of
                                        //       the name.
                                        //
                                        item = item.Substring(1);
                                    }

                                    object itemEnumValue = TryParseEnum(
                                        enumType, item, allowInteger, noCase,
                                        ref localError);

                                    if (itemEnumValue != null)
                                    {
                                        switch (@operator)
                                        {
                                            case AddFlagOperator:
                                                {
                                                    //
                                                    // NOTE: Add the specified flag bits.
                                                    //
                                                    enumValue = TryGetEnum(enumType,
                                                        ToULong(enumType,
                                                            (Enum)enumValue, cultureInfo) |
                                                        ToULong(enumType,
                                                            (Enum)itemEnumValue, cultureInfo),
                                                        ref localError);

                                                    break;
                                                }
                                            case RemoveFlagOperator:
                                                {
                                                    //
                                                    // NOTE: Remove the specified flag
                                                    //       bits.
                                                    //
                                                    enumValue = TryGetEnum(enumType,
                                                        ToULong(enumType,
                                                            (Enum)enumValue, cultureInfo) &
                                                        ~ToULong(enumType,
                                                            (Enum)itemEnumValue, cultureInfo),
                                                        ref localError);

                                                    break;
                                                }
                                            case SetFlagOperator:
                                                {
                                                    //
                                                    // NOTE: Set the overall value equal
                                                    //       to the current value.  This
                                                    //       should be used only very
                                                    //       rarely and is supported
                                                    //       primarily for completeness.
                                                    //
                                                    enumValue = itemEnumValue;

                                                    break;
                                                }
                                            case SetAddFlagOperator:
                                                {
                                                    //
                                                    // NOTE: Set the overall value equal
                                                    //       to the current value and then
                                                    //       reset the operator to add.
                                                    //
                                                    @operator = AddFlagOperator;
                                                    enumValue = itemEnumValue;

                                                    break;
                                                }
                                            case KeepFlagOperator:
                                                {
                                                    //
                                                    // NOTE: Bitwise 'and' the specified
                                                    //       flag bits.
                                                    //
                                                    enumValue = TryGetEnum(enumType,
                                                        ToULong(enumType,
                                                            (Enum)enumValue, cultureInfo) &
                                                        ToULong(enumType,
                                                            (Enum)itemEnumValue, cultureInfo),
                                                        ref localError);

                                                    break;
                                                }
                                            default:
                                                {
                                                    //
                                                    // NOTE: Any other operator character
                                                    //       is invalid.
                                                    //
                                                    localError = String.Format(
                                                        "bad flags operator \"{0}\", must " +
                                                        "be \"{1}\", \"{2}\", \"{3}\", " +
                                                        "\"{4}\", or \"{5}\"", @operator,
                                                        AddFlagOperator, RemoveFlagOperator,
                                                        SetFlagOperator, SetAddFlagOperator,
                                                        KeepFlagOperator);

                                                    enumValue = null;
                                                    break;
                                                }
                                        }
                                    }
                                    else
                                    {
                                        enumValue = null;
                                    }
                                }

                                //
                                // NOTE: If we failed anything above, break out of the
                                //       processing loop now.
                                //
                                if (enumValue == null)
                                {
                                    error = localError;
                                    break;
                                }
                            }

                            //
                            // NOTE: Return the calculated enum value (this may be null if
                            //       we failed an operation inside the processing loop).
                            //
                            return enumValue;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                    }
                    else
                    {
                        error = ResultOps.MaybeCombine(
                            haveOldValue ? String.Format(
                            "invalid \"{0}\" old value \"{1}\"",
                            enumType.FullName, oldValue) : null,
                            localError);
                    }
                }
            }
            else if (strict)
            {
                error = String.Format(
                    "invalid \"{0}\" new value",
                    enumType.FullName);
            }
            else
            {
                bool haveOldValue = false;
                Result localError = null;
                object enumValue;

                if (!String.IsNullOrEmpty(oldValue))
                {
                    haveOldValue = true;

                    enumValue = TryParseEnum(
                        enumType, oldValue, allowInteger,
                        noCase, ref localError);
                }
                else
                {
                    enumValue = TryGetEnum(
                        enumType, 0, ref localError);
                }

                if (enumValue != null)
                {
                    return enumValue;
                }
                else
                {
                    error = ResultOps.MaybeCombine(
                        haveOldValue ? String.Format(
                        "invalid \"{0}\" old value \"{1}\"",
                        enumType.FullName, oldValue) : null,
                        localError);
                }
            }

            return null;
        }
        #endregion
    }
}
