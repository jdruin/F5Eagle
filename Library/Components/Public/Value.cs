/*
 * Value.cs --
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
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /* INTERNAL STATIC OK */
    [ObjectId("cd8749dc-8483-45e9-ab43-a8daef79df64")]
    public static class Value
    {
        #region Private Constants
        internal static readonly string ZeroString = 0.ToString();
        internal static readonly string OneString = 1.ToString();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly StringComparison NoCaseStringComparisonType =
            StringOps.SystemNoCaseStringComparisonType;

        private static readonly StringComparison StringComparisonType =
            StringOps.SystemStringComparisonType;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string DefaultDecimalSeparator = Characters.Period.ToString();

        //
        // HACK: These characters may be present in floating point values;
        //       however, that (clearly?) does not apply to hexadecimal in
        //       the case of 'E' / 'e'.  Also, these do not appear to vary
        //       based on the current culture.
        //
        private static readonly char[] MaybeFloatingPointChars = {
            Characters.E, Characters.e
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const string badIndexBoundsError = "bad index bounds";
        private const string badIndexOperatorError = "bad index operator {0}, must be one of: +-*/%";

        private const string badIndexError1 =
            "bad index {0}: must be start|end|count|integer";

        private const string badIndexError2 =
            "bad index {0}: must be start|end|count|integer?[+-*/%]start|end|count|integer?";

        private const string startName = "start";
        private const string endName = "end";
        private const string countName = "count";

        private static Regex startEndPlusMinusIndexRegEx = new Regex(
                "^(" + startName + "|" + endName + "|" + countName +
                "|\\d+){1}([\\+\\-\\*\\/\\%]{1})(" + startName + "|" +
                endName + "|" + countName + "|\\d+)$",
                RegexOptions.CultureInvariant);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const NumberStyles byteStyles = NumberStyles.Integer;

        private const NumberStyles narrowIntegerStyles = NumberStyles.Integer;

        private const NumberStyles integerStyles = NumberStyles.Integer;

        private const NumberStyles wideIntegerStyles = NumberStyles.Integer;

        private const NumberStyles decimalStyles = NumberStyles.Number;

        private const NumberStyles singleStyles =
            NumberStyles.Float | NumberStyles.AllowThousands;

        private const NumberStyles doubleStyles =
            NumberStyles.Float | NumberStyles.AllowThousands;

        private const DateTimeStyles dateTimeStyles = DateTimeStyles.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region CultureInfo / IFormatProvider Data
        private static CultureInfo DefaultCulture = null;
        private static IFormatProvider NumberFormatProvider = null;
        private static IFormatProvider DateTimeFormatProvider = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region CLR Integration
        private static StringDictionary DefaultObjectNamespaces = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Named Numeric Values
        private static SingleDictionary namedSingles = null; // Inf, NaN, etc (float)
        private static DoubleDictionary namedDoubles = null; // Inf, NaN, etc (double)
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Supported "Value" Types
        private static TypeList integerTypes = null;
        private static TypeList floatTypes = null;
        private static TypeList stringTypes = null;
        private static TypeList numberTypes = null;
        private static TypeList integralTypes = null;
        private static TypeList nonIntegralTypes = null;
        private static TypeList otherTypes = null;
        private static TypeList allTypes = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        static Value()
        {
            InitializeCulture();
            InitializeTypes();
            InitializeNamedNumerics();
            InitializeObjectNamespaces();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializeCulture()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (DefaultCulture == null)
                    DefaultCulture = CultureInfo.InvariantCulture;

                if (DefaultCulture != null)
                {
                    if (NumberFormatProvider == null)
                        NumberFormatProvider = DefaultCulture.NumberFormat;

                    if (DateTimeFormatProvider == null)
                        DateTimeFormatProvider = DefaultCulture.DateTimeFormat;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializeTypes()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Initialize the static type lists used when adding
                //       operators and functions.
                //
                if (integerTypes == null)
                    integerTypes = new TypeList(new Type[] { typeof(int) });

                ///////////////////////////////////////////////////////////////

                if (floatTypes == null)
                    floatTypes = new TypeList(new Type[] { typeof(double) });

                ///////////////////////////////////////////////////////////////

                if (stringTypes == null)
                    stringTypes = new TypeList(new Type[] { typeof(string) });

                ///////////////////////////////////////////////////////////////

                if (numberTypes == null)
                    numberTypes = new TypeList(Number.Types(null));

                ///////////////////////////////////////////////////////////////

                if (integralTypes == null)
                {
                    integralTypes = new TypeList(Number.Types(null));

                    integralTypes.Remove(typeof(decimal));
                    integralTypes.Remove(typeof(float));
                    integralTypes.Remove(typeof(double));
                }

                ///////////////////////////////////////////////////////////////

                if (nonIntegralTypes == null)
                {
                    nonIntegralTypes = new TypeList(Number.Types(null));

                    nonIntegralTypes.Remove(typeof(bool));
                    nonIntegralTypes.Remove(typeof(sbyte));
                    nonIntegralTypes.Remove(typeof(byte));
                    nonIntegralTypes.Remove(typeof(short));
                    nonIntegralTypes.Remove(typeof(ushort));
                    nonIntegralTypes.Remove(typeof(char));
                    nonIntegralTypes.Remove(typeof(int));
                    nonIntegralTypes.Remove(typeof(uint));
                    nonIntegralTypes.Remove(typeof(long));
                    nonIntegralTypes.Remove(typeof(ulong));
                    nonIntegralTypes.Remove(typeof(Enum));
                    nonIntegralTypes.Remove(typeof(ReturnCode));
                    nonIntegralTypes.Remove(typeof(MatchMode));
                    nonIntegralTypes.Remove(typeof(MidpointRounding));
                }

                ///////////////////////////////////////////////////////////////

                if (otherTypes == null)
                {
                    otherTypes = new TypeList(Variant.Types(null));

                    otherTypes.Remove(typeof(Number));
                }

                ///////////////////////////////////////////////////////////////

                if (allTypes == null)
                {
                    allTypes = new TypeList(numberTypes);

                    if (otherTypes != null)
                        allTypes.AddRange(otherTypes);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializeNamedNumerics()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Initialize the lookup tables of named single and
                //       double values understood by the expression parser
                //       ("+Inf", "-Inf", "NaN", etc).
                //
                if (namedSingles == null)
                {
                    namedSingles = new SingleDictionary(
                        new _Comparers.Custom(NoCaseStringComparisonType));

                    namedSingles.Add(TclVars.Infinity, float.PositiveInfinity);

                    namedSingles.Add(Characters.PlusSign + TclVars.Infinity,
                        float.PositiveInfinity);

                    namedSingles.Add(Characters.MinusSign + TclVars.Infinity,
                        float.NegativeInfinity);

                    namedSingles.Add(float.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        float.PositiveInfinity);

                    namedSingles.Add(float.NegativeInfinity.ToString(
                            GetDefaultCulture()),
                        float.NegativeInfinity);

                    namedSingles.Add(Characters.PlusSign +
                        float.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        float.PositiveInfinity);

                    namedSingles.Add(TclVars.NaN, float.NaN);
                }

                ///////////////////////////////////////////////////////////////

                if (namedDoubles == null)
                {
                    namedDoubles = new DoubleDictionary(
                        new _Comparers.Custom(NoCaseStringComparisonType));

                    namedDoubles.Add(TclVars.Infinity, double.PositiveInfinity);

                    namedDoubles.Add(Characters.PlusSign + TclVars.Infinity,
                        double.PositiveInfinity);

                    namedDoubles.Add(Characters.MinusSign + TclVars.Infinity,
                        double.NegativeInfinity);

                    namedDoubles.Add(double.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        double.PositiveInfinity);

                    namedDoubles.Add(double.NegativeInfinity.ToString(
                            GetDefaultCulture()),
                        double.NegativeInfinity);

                    namedDoubles.Add(Characters.PlusSign +
                        double.PositiveInfinity.ToString(
                            GetDefaultCulture()),
                        double.PositiveInfinity);

                    namedDoubles.Add(TclVars.NaN, double.NaN);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializeObjectNamespaces()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Initialize our list of well-known type prefixes for
                //       use by our overloaded GetType method.
                //
                DefaultObjectNamespaces = new StringDictionary();

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: First, try to add the ones that this library "knows"
                //       about.
                //
                ReturnCode namespaceCode;
                Result namespaceError = null;

                namespaceCode = ObjectOps.AddAssemblyObjectNamespaces(
                    ref DefaultObjectNamespaces, ref namespaceError);

                if (namespaceCode != ReturnCode.Ok)
                    DebugOps.Complain(namespaceCode, namespaceError);

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Finally, add the system default namespace(s) (e.g.
                //       "System", etc).
                //
                namespaceCode = ObjectOps.AddSystemObjectNamespaces(
                    ref DefaultObjectNamespaces, ref namespaceError);

                if (namespaceCode != ReturnCode.Ok)
                    DebugOps.Complain(namespaceCode, namespaceError);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static CultureInfo GetDefaultCulture() /* CANNOT RETURN NULL */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeCulture();

                if (DefaultCulture != null)
                    return DefaultCulture;

                return CultureInfo.InvariantCulture;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IFormatProvider GetNumberFormatProvider() /* MAY RETURN NULL */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeCulture();

                return NumberFormatProvider;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static IFormatProvider GetNumberFormatProvider( /* MAY RETURN NULL */
            CultureInfo cultureInfo
            )
        {
            return (cultureInfo != null) ?
                cultureInfo.NumberFormat : GetNumberFormatProvider();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static IFormatProvider GetDateTimeFormatProvider() /* MAY RETURN NULL */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                InitializeCulture();

                return DateTimeFormatProvider;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static IFormatProvider GetDateTimeFormatProvider( /* MAY RETURN NULL */
            CultureInfo cultureInfo
            )
        {
            return (cultureInfo != null) ?
                cultureInfo.DateTimeFormat : GetDateTimeFormatProvider();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetNumberDecimalSeparator( /* MAY RETURN NULL */
            IFormatProvider formatProvider
            )
        {
            NumberFormatInfo numberFormatInfo =
                formatProvider as NumberFormatInfo;

            if (numberFormatInfo != null)
                return numberFormatInfo.NumberDecimalSeparator;

            return DefaultDecimalSeparator;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void CopyTypes(
            TypeList list,
            ref Dictionary<Type, object> dictionary
            )
        {
            if (list == null)
                return;

            if (dictionary == null)
                dictionary = new Dictionary<Type, object>();

            foreach (Type type in list)
            {
                if (type == null)
                    continue;

                dictionary[type] = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static void GetTypes(
            TypeListFlags flags,
            ref TypeList types
            )
        {
            Dictionary<Type, object> dictionary = null;

            if (FlagOps.HasFlags(flags, TypeListFlags.IntegerTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(integerTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(flags, TypeListFlags.FloatTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(floatTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(flags, TypeListFlags.StringTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(stringTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(flags, TypeListFlags.NumberTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(numberTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(flags, TypeListFlags.IntegralTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(integralTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(flags, TypeListFlags.NonIntegralTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(nonIntegralTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(flags, TypeListFlags.OtherTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(otherTypes, ref dictionary);
                }
            }

            if (FlagOps.HasFlags(flags, TypeListFlags.AllTypes, true))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    CopyTypes(allTypes, ref dictionary);
                }
            }

            if (dictionary != null)
            {
                if (types == null)
                    types = new TypeList();

                types.AddRange(dictionary.Keys);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static void GetObjectNamespaces(
            out StringDictionary objectNamespaces
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (DefaultObjectNamespaces != null)
                {
                    objectNamespaces = new StringDictionary(
                        (IDictionary<string, string>)DefaultObjectNamespaces);
                }
                else
                {
                    objectNamespaces = new StringDictionary();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryLookupNamedSingle(
            string text,
            ref float value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((text != null) && (namedSingles != null) &&
                    namedSingles.TryGetValue(text, out value))
                {
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryLookupNamedDouble(
            string text,
            ref double value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((text != null) && (namedDoubles != null) &&
                    namedDoubles.TryGetValue(text, out value))
                {
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void CheckRadixPrefix(
            string text,
            CultureInfo cultureInfo, /* NOT USED */
            ref ValueFlags flags,
            ref int length
            )
        {
            if ((text == null) || (text.Length < 2))
                return;

            if (text[0] != Characters.Zero) // all prefixes start with zero.
                return;

            if ((text[1] == Characters.B) ||
                (text[1] == Characters.b)) // binary?
            {
                flags |= ValueFlags.BinaryRadix;
                length += 2;
            }
            else if ((text[1] == Characters.O) ||
                (text[1] == Characters.o)) // octal?
            {
                flags |= ValueFlags.OctalRadix;
                length += 2;
            }
            else if ((text[1] == Characters.D) ||
                (text[1] == Characters.d)) // decimal?
            {
                flags |= ValueFlags.DecimalRadix;
                length += 2;
            }
            else if ((text[1] == Characters.X) ||
                (text[1] == Characters.x)) // hexadecimal?
            {
                flags |= ValueFlags.HexadecimalRadix;
                length += 2;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object GetIntegerOrWideInteger(
            long value
            )
        {
            if ((value >= int.MinValue) && (value <= int.MaxValue))
                return ConversionOps.ToInt(value);
            else
                return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNumeric(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref object value,
            ref Result error
            )
        {
            if (text == null)
            {
                error = String.Format(
                    "expected numeric value but got {0}",
                    FormatOps.WrapOrNull(text));

                return ReturnCode.Error;
            }

            int length = text.Length;

            if (length == 0)
            {
                error = String.Format(
                    "expected numeric value but got {0}",
                    FormatOps.WrapOrNull(text));

                return ReturnCode.Error;
            }

            bool boolValue = false;
            bool wasInteger = false;

            if (TryParseBooleanOnly( /* FAST */
                    text, NoCaseStringComparisonType, ref boolValue,
                    ref wasInteger))
            {
                if (wasInteger)
                    value = boolValue ? 1 : 0;
                else
                    value = boolValue;

                return ReturnCode.Ok;
            }

            double doubleValue = 0.0;

            if (TryLookupNamedDouble(text, ref doubleValue))
            {
                value = doubleValue;
                return ReturnCode.Ok;
            }

            IFormatProvider formatProvider = GetNumberFormatProvider(
                cultureInfo);

            string decimalSeparator = GetNumberDecimalSeparator(
                formatProvider);

            decimal decimalValue = Decimal.Zero;

            if ((decimalSeparator != null) &&
                (text.IndexOf(decimalSeparator) != Index.Invalid))
            {
                if (decimal.TryParse(
                        text, decimalStyles, formatProvider,
                        out decimalValue))
                {
                    value = decimalValue;
                    return ReturnCode.Ok;
                }
                else
                {
                    doubleValue = 0.0;

                    if (double.TryParse(
                            text, doubleStyles, formatProvider,
                            out doubleValue))
                    {
                        value = doubleValue;
                        return ReturnCode.Ok;
                    }
                }

                error = String.Format(
                    "expected fixed/floating point value but got {0}",
                    FormatOps.WrapOrNull(text));

                return ReturnCode.Error;
            }

            bool triedDouble = false;

            if ((MaybeFloatingPointChars != null) &&
                (text.IndexOfAny(MaybeFloatingPointChars) != Index.Invalid))
            {
                doubleValue = 0.0;

                if (double.TryParse(
                        text, doubleStyles, formatProvider,
                        out doubleValue))
                {
                    value = doubleValue;
                    return ReturnCode.Ok;
                }

                triedDouble = true;
            }

            long longValue = 0;
            bool done = false;

            if (ParseWideIntegerWithRadixPrefix(
                    text, flags, cultureInfo, ref done, ref longValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (done)
            {
                value = GetIntegerOrWideInteger(longValue);
                return ReturnCode.Ok;
            }

            char firstCharacter = text[0];

            if ((firstCharacter == Characters.PlusSign) ||
                (firstCharacter == Characters.MinusSign))
            {
                longValue = 0;

                if (long.TryParse(
                        text, wideIntegerStyles, formatProvider,
                        out longValue))
                {
                    value = GetIntegerOrWideInteger(longValue);
                    return ReturnCode.Ok;
                }
            }

            ulong ulongValue = 0;

            if (ulong.TryParse(
                    text, wideIntegerStyles, formatProvider,
                    out ulongValue))
            {
                value = GetIntegerOrWideInteger(ConversionOps.ToLong(
                    ulongValue));

                return ReturnCode.Ok;
            }

            decimalValue = Decimal.Zero;

            if (decimal.TryParse(
                    text, decimalStyles, formatProvider,
                    out decimalValue))
            {
                value = decimalValue;
                return ReturnCode.Ok;
            }

            if (!triedDouble)
            {
                doubleValue = 0.0;

                if (double.TryParse(
                        text, doubleStyles, formatProvider,
                        out doubleValue))
                {
                    value = doubleValue;
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected numeric value but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringComparison GetComparisonType(
            ValueFlags flags
            )
        {
            if (FlagOps.HasFlags(flags, ValueFlags.NoCase, true))
                return NoCaseStringComparisonType;

            return StringComparisonType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool IsInteger( /* NOT USED */
            string text,
            bool wide,
            CultureInfo cultureInfo
            )
        {
            if (wide)
            {
                long longValue = 0;

                return (GetWideInteger2(text, ValueFlags.AnyWideInteger,
                    cultureInfo, ref longValue) == ReturnCode.Ok);
            }
            else
            {
                int intValue = 0;

                return (GetInteger2(text, ValueFlags.AnyInteger,
                    cultureInfo, ref intValue) == ReturnCode.Ok);
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Boolean Options To ValueFlags Methods
        internal static ValueFlags GetTypeValueFlags(
            bool strict,
            bool verbose,
            bool noCase
            )
        {
            return GetTypeValueFlags(false, strict, verbose, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ValueFlags GetTypeValueFlags(
            bool allowInteger,
            bool strict,
            bool verbose,
            bool noCase
            )
        {
            ValueFlags result = ValueFlags.None;

            if (allowInteger)
                result |= ValueFlags.AllowInteger;

            if (strict)
                result |= ValueFlags.Strict;

            if (verbose)
                result |= ValueFlags.Verbose;

            if (noCase)
                result |= ValueFlags.NoCase;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ValueFlags GetTypeValueFlags(
            OptionFlags flags
            )
        {
            ValueFlags result = ValueFlags.None;

            if (FlagOps.HasFlags(flags, OptionFlags.AllowInteger, true))
                result |= ValueFlags.AllowInteger;

            if (FlagOps.HasFlags(flags, OptionFlags.Strict, true))
                result |= ValueFlags.Strict;

            if (FlagOps.HasFlags(flags, OptionFlags.Verbose, true))
                result |= ValueFlags.Verbose;

            if (FlagOps.HasFlags(flags, OptionFlags.NoCase, true))
                result |= ValueFlags.NoCase;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ValueFlags GetObjectValueFlags(
            ValueFlags flags,
            bool strict,
            bool verbose,
            bool noCase,
            bool noNested,
            bool noComObject
            )
        {
            ValueFlags result = flags;

            if (strict)
                result |= ValueFlags.Strict;

            if (verbose)
                result |= ValueFlags.Verbose;

            if (noCase)
                result |= ValueFlags.NoCase;

            if (noNested)
                result |= ValueFlags.NoNested;

            if (noComObject)
                result |= ValueFlags.NoComObject;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ValueFlags GetMemberValueFlags(
            ValueFlags flags,
            bool noNested,
            bool noComObject
            )
        {
            ValueFlags result = flags;

            if (noNested)
                result |= ValueFlags.NoNested;

            if (noComObject)
                result |= ValueFlags.NoComObject;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ValueFlags GetCallFrameValueFlags(
            bool strict
            )
        {
            ValueFlags result = ValueFlags.None;

            if (strict)
                result |= ValueFlags.Strict;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static void ExtractTypeValueFlags(
            ValueFlags flags,
            out bool allowInteger,
            out bool strict,
            out bool verbose,
            out bool noCase
            )
        {
            allowInteger = FlagOps.HasFlags(
                flags, ValueFlags.AllowInteger, true);

            strict = FlagOps.HasFlags(
                flags, ValueFlags.Strict, true);

            verbose = FlagOps.HasFlags(
                flags, ValueFlags.Verbose, true);

            noCase = FlagOps.HasFlags(
                flags, ValueFlags.NoCase, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetVersion(
            string text,
            CultureInfo cultureInfo,
            ref Version value
            )
        {
            Result error = null;

            return GetVersion(text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetVersion(
            string text,
            CultureInfo cultureInfo,
            ref Version value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetVersion(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetVersion(
            string text,
            CultureInfo cultureInfo,
            ref Version value,
            ref Result error,
            ref Exception exception
            )
        {
            try
            {
                if (!String.IsNullOrEmpty(text))
                {
                    //
                    // FIXME: *COMPAT* This is not 100% Tcl
                    //        compatible.
                    //
                    // TODO: No TryParse, eh?
                    //
                    value = new Version(text);

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

            error = String.Format(
                "expected version value but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetUri(
            string text,
            UriKind uriKind,
            CultureInfo cultureInfo,
            ref Uri value
            )
        {
            Result error = null;

            return GetUri(text, uriKind, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetUri(
            string text,
            UriKind uriKind,
            CultureInfo cultureInfo,
            ref Uri value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUri(
                text, uriKind, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetUri(
            string text,
            UriKind uriKind,
            CultureInfo cultureInfo,
            ref Uri value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
                if (Uri.TryCreate(text, uriKind, out value))
                    return ReturnCode.Ok;

            error = String.Format(
                "expected uri value but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid value
            )
        {
            Result error = null;

            return GetGuid(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetGuid(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetGuid(
            string text,
            CultureInfo cultureInfo,
            ref Guid value,
            ref Result error,
            ref Exception exception
            )
        {
            try
            {
                if (!String.IsNullOrEmpty(text))
                {
                    //
                    // TODO: No TryParse, eh?
                    //
                    value = new Guid(text);

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

            error = String.Format(
                "expected guid value but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetType(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value
            )
        {
            ResultList errors = null;

            return GetType(
                interpreter, text, types, appDomain, flags, cultureInfo,
                ref value, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetType(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value,
            ref ResultList errors
            )
        {
            Exception exception = null;

            return GetType(
                interpreter, text, types, appDomain, flags, cultureInfo,
                ref value, ref errors, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetType(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            //
            // NOTE: This static method (function) is used to fetch a System.Type
            //       object based on a type string that may be qualified with a
            //       namespace and/or an assembly name.  If the type string is not
            //       qualified with a namespace, we may try prepending various type
            //       prefixes to it during the resolution process.  If the type
            //       string is not qualified with an assembly name, we may try
            //       searching through all the assemblies loaded into the current
            //       AppDomain during the resolution process.  All errors that we
            //       encounter during the type string resolution process will be
            //       recorded in the list of errors provided by the caller, even
            //       if the overall result of the entire operation turns out to be
            //       successful.
            //
            if (errors == null)
                errors = new ResultList();

            if (GetType(
                    interpreter, text, types, null, (GetTypeCallback)null,
                    flags, cultureInfo, ref value, ref errors,
                    ref exception) == ReturnCode.Ok)
            {
                return ReturnCode.Ok;
            }
            else if ((appDomain != null) &&
                !FlagOps.HasFlags(flags, ValueFlags.Strict, true) &&
                !FlagOps.HasFlags(flags, ValueFlags.NoAssembly, true) &&
                !MarshalOps.IsAssemblyQualifiedTypeName(text))
            {
                Assembly[] assemblies = appDomain.GetAssemblies();

                if ((assemblies != null) && (assemblies.Length > 0))
                {
                    foreach (Assembly assembly in assemblies)
                    {
                        if (GetType(
                                interpreter, text, types, assembly,
                                assembly.GetType, flags, cultureInfo,
                                ref value, ref errors,
                                ref exception) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetType( /* NOT USED */
            Interpreter interpreter,
            string text,
            TypeList types,
            Assembly assembly,
            GetTypeCallback callback,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Type value,
            ref ResultList errors
            )
        {
            Exception exception = null;

            return GetType(
                interpreter, text, types, assembly, callback, flags,
                cultureInfo, ref value, ref errors, ref exception);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetType(
            Interpreter interpreter,
            string text,
            TypeList types,
            Assembly assembly,
            GetTypeCallback callback,
            ValueFlags flags,
            CultureInfo cultureInfo, /* NOT USED */
            ref Type value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if (errors == null)
                errors = new ResultList();

            if (interpreter != null)
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    int errorCount = errors.Count;
                    AssemblyName assemblyName = null;

                    if (assembly != null)
                    {
                        try
                        {
                            assemblyName = assembly.GetName();
                        }
                        catch (Exception e)
                        {
                            if (!FlagOps.HasFlags(flags, ValueFlags.NoException, true))
                                errors.Add(e);

                            exception = e;
                        }
                    }

                    //
                    // NOTE: *WARNING* Empty opaque object handle names are allowed,
                    //       please do not change this to "!String.IsNullOrEmpty".
                    //
                    if (text != null)
                    {
                        //
                        // NOTE: Try to obtain a Type object from the interpreter
                        //       based on an opaque object handle.
                        //
                        object @object = null;

                        if (GetObject(interpreter, text, ref @object) == ReturnCode.Ok)
                        {
                            //
                            // NOTE: The null value may be used here to indicate
                            //       that the caller does not care about a particular
                            //       [parameter] type.
                            //
                            if ((@object == null) || (@object is Type))
                            {
                                value = MarshalOps.MaybeGenericType(
                                    (Type)@object, types, flags, ref errors);

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                Result error = String.Format(
                                    "object {0} type mismatch, type {1} is " +
                                    "not compatible with type {2}",
                                    FormatOps.WrapOrNull(text),
                                    MarshalOps.GetErrorTypeName(@object),
                                    MarshalOps.GetErrorTypeName(typeof(Type)));

                                if (errors.Find(error) == Index.Invalid)
                                    errors.Add(error);
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Check the type name in the type name lookup
                            //       table for the interpreter (i.e. typeDefs).
                            //
                            StringDictionary objectTypes = interpreter.ObjectTypes;

                            if (objectTypes != null)
                            {
                                string newText;

                                if (objectTypes.TryGetValue(text, out newText))
                                    text = newText;
                            }

#if TYPE_CACHE
                            //
                            // NOTE: Check the type name in the type name lookup
                            //       cache for the interpreter.
                            //
                            Type localValue = null;

                            if (interpreter.GetCachedType(text, ref localValue))
                            {
                                value = MarshalOps.MaybeGenericType(
                                    localValue, types, flags, ref errors);

                                return ReturnCode.Ok;
                            }
#endif

                            //
                            // NOTE: If they did not specify a type resolution
                            //       callback, we use the default.
                            //
                            if (callback == null)
                                callback = Type.GetType;

                            Type type = null;

                            try
                            {
                                type = callback(text,
                                    FlagOps.HasFlags(flags, ValueFlags.Verbose, true),
                                    FlagOps.HasFlags(flags, ValueFlags.NoCase, true)); /* throw */
                            }
                            catch (Exception e)
                            {
                                if (!FlagOps.HasFlags(flags, ValueFlags.NoException, true))
                                    errors.Add(e);

                                exception = e;
                            }

                            //
                            // NOTE: Did we find the type they specified (which
                            //       may or may not have been qualified)?
                            //
                            if (type != null)
                            {
#if TYPE_CACHE
                                interpreter.AddCachedType(text, type);
#endif

                                value = MarshalOps.MaybeGenericType(
                                    type, types, flags, ref errors);

                                return ReturnCode.Ok;
                            }
                            else if (!FlagOps.HasFlags(flags, ValueFlags.Strict, true) &&
                                !FlagOps.HasFlags(flags, ValueFlags.NoNamespace, true) &&
                                !MarshalOps.IsNamespaceQualifiedTypeName(text))
                            {
                                if (FlagOps.HasFlags(flags, ValueFlags.ShowName, true))
                                {
                                    errors.Add(String.Format("type {0} not found",
                                        FormatOps.WrapOrNull(FormatOps.QualifiedName(
                                            assemblyName, text, FlagOps.HasFlags(flags,
                                                ValueFlags.FullName, true)))));
                                }

                                StringDictionary namespaces = interpreter.ObjectNamespaces;

                                if (namespaces != null)
                                {
                                    //
                                    // HACK: Allow them to specify a partially-
                                    //       qualified type name as long as it
                                    //       resides in what we consider to be
                                    //       one of our well-known namespaces
                                    //       (i.e. "System", etc).
                                    //
                                    foreach (KeyValuePair<string, string> pair in namespaces)
                                    {
                                        string namespaceName = pair.Key;

                                        string newText = FormatOps.QualifiedName(
                                            namespaceName, text);

#if TYPE_CACHE
                                        localValue = null;

                                        if (interpreter.GetCachedType(newText, ref localValue))
                                        {
                                            value = MarshalOps.MaybeGenericType(
                                                localValue, types, flags, ref errors);

                                            return ReturnCode.Ok;
                                        }
#endif

                                        try
                                        {
                                            type = callback(newText,
                                                FlagOps.HasFlags(flags, ValueFlags.Verbose, true),
                                                FlagOps.HasFlags(flags, ValueFlags.NoCase, true)); /* throw */
                                        }
                                        catch (Exception e)
                                        {
                                            if (!FlagOps.HasFlags(flags, ValueFlags.NoException, true))
                                                errors.Add(e);

                                            exception = e;
                                        }

                                        //
                                        // NOTE: Did we find the type they specified
                                        //       (which may or may not have been
                                        //       qualified) modified with the current
                                        //       type prefix?
                                        //
                                        if (type != null)
                                        {
#if TYPE_CACHE
                                            interpreter.AddCachedType(newText, type);
#endif

                                            value = MarshalOps.MaybeGenericType(
                                                type, types, flags, ref errors);

                                            return ReturnCode.Ok;
                                        }
                                        else if (FlagOps.HasFlags(flags, ValueFlags.ShowName, true))
                                        {
                                            errors.Add(String.Format("type {0} not found",
                                                FormatOps.WrapOrNull(FormatOps.QualifiedName(
                                                    assemblyName, newText, FlagOps.HasFlags(
                                                        flags, ValueFlags.FullName, true)))));
                                        }
                                    }
                                }
                            }
                            else if (FlagOps.HasFlags(flags, ValueFlags.ShowName, true))
                            {
                                errors.Add(String.Format("type {0} not found",
                                    FormatOps.WrapOrNull(FormatOps.QualifiedName(
                                        assemblyName, text, FlagOps.HasFlags(
                                            flags, ValueFlags.FullName, true)))));
                            }
                        }
                    }

                    //
                    // NOTE: If this method did not add any error messages
                    //       before this point, add one now.
                    //
                    if (errors.Count == errorCount)
                    {
                        Result error = String.Format(
                            "expected type value but got {0}",
                            FormatOps.WrapOrNull(text));

                        if (errors.Find(error) == Index.Invalid)
                            errors.Insert(0, error);
                    }
                }
            }
            else
            {
                errors.Add("invalid interpreter");
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors
            )
        {
            Exception exception = null;

            return GetTypeList(
                interpreter, text, appDomain, flags, cultureInfo, ref value,
                ref errors, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if (errors == null)
                errors = new ResultList();

            if (text == null)
                goto error;

            StringList list = null;
            Result error = null;

            if (Parser.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                TypeList typeList = new TypeList();

                foreach (string element in list)
                {
                    Type type = null;

                    if (GetType(
                            interpreter, element, null, appDomain,
                            flags, cultureInfo, ref type, ref errors,
                            ref exception) == ReturnCode.Ok)
                    {
                        typeList.Add(type);
                    }
                    else
                    {
                        goto error;
                    }
                }

                //
                // NOTE: If we get this far, all elements in the list were
                //       successfully interpreted as a System.Type object.
                //
                value = typeList;

                return ReturnCode.Ok;
            }
            else
            {
                errors.Add(error);
            }

        error:

            errors.Insert(0, String.Format(
                "expected type list value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            Assembly assembly,
            GetTypeCallback callback,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors
            )
        {
            Exception exception = null;

            return GetTypeList(
                interpreter, text, assembly, callback, flags, cultureInfo,
                ref value, ref errors, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTypeList(
            Interpreter interpreter,
            string text,
            Assembly assembly,
            GetTypeCallback callback,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TypeList value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if (errors == null)
                errors = new ResultList();

            if (text == null)
                goto error;

            StringList list = null;
            Result error = null;

            if (Parser.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                TypeList typeList = new TypeList();

                foreach (string element in list)
                {
                    Type type = null;

                    if (GetType(
                            interpreter, element, null, assembly,
                            callback, flags, cultureInfo, ref type,
                            ref errors, ref exception) == ReturnCode.Ok)
                    {
                        typeList.Add(type);
                    }
                    else
                    {
                        goto error;
                    }
                }

                //
                // NOTE: If we get this far, all elements in the list were
                //       successfully interpreted as a System.Type object.
                //
                value = typeList;

                return ReturnCode.Ok;
            }
            else
            {
                errors.Add(error);
            }

        error:

            errors.Insert(0, String.Format(
                "expected type list value but got {0}",
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetReturnCodeList(
            string text,
            CultureInfo cultureInfo,
            ref ReturnCodeList value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetReturnCodeList(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetReturnCodeList(
            string text,
            CultureInfo cultureInfo,
            ref ReturnCodeList value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringList list = null;

                if (Parser.SplitList(
                        null, text, 0, Length.Invalid, true,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    ReturnCodeList returnCodeList = new ReturnCodeList();

                    foreach (string element in list)
                    {
                        ReturnCode returnCode = ReturnCode.Ok;

                        if (GetReturnCode2(element, ValueFlags.AnyReturnCode,
                                cultureInfo, ref returnCode, ref error,
                                ref exception) == ReturnCode.Ok)
                        {
                            returnCodeList.Add(returnCode);
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: If we get this far, all elements in the list were
                    //       successfully interpreted as a ReturnCode object.
                    //
                    value = returnCodeList;

                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            error = String.Format(
                "expected return code list value but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetEnumList(
            Interpreter interpreter,
            string text,
            Type enumType,
            string oldValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref EnumList value,
            ref ResultList errors
            )
        {
            Exception exception = null;

            return GetEnumList(
                interpreter, text, enumType, oldValue, flags, cultureInfo,
                ref value, ref errors, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetEnumList(
            Interpreter interpreter,
            string text,
            Type enumType,
            string oldValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref EnumList value,
            ref ResultList errors,
            ref Exception exception
            )
        {
            if (errors == null)
                errors = new ResultList();

            if (text == null)
                goto error;

            if (enumType == null)
            {
                errors.Add("invalid type");
                goto error;
            }

            if (!enumType.IsEnum)
            {
                errors.Add(String.Format(
                    "type {0} is not an enumeration",
                    MarshalOps.GetErrorTypeName(enumType)));

                goto error;
            }

            bool allowInteger = FlagOps.HasFlags(
                flags, ValueFlags.AllowInteger, true);

            bool strict = FlagOps.HasFlags(
                flags, ValueFlags.Strict, true);

            bool noCase = FlagOps.HasFlags(
                flags, ValueFlags.NoCase, true);

            StringList list = null;
            Result error = null;

            if (Parser.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                EnumList enumList = new EnumList();
                bool isFlags = EnumOps.IsFlagsEnum(enumType);

                foreach (string element in list)
                {
                    object enumValue;

                    if (isFlags)
                    {
                        error = null;

                        enumValue = EnumOps.TryParseFlagsEnum(
                            interpreter, enumType, oldValue,
                            element, cultureInfo, allowInteger,
                            strict, noCase, ref error);
                    }
                    else
                    {
                        error = null;

                        enumValue = EnumOps.TryParseEnum(
                            enumType, element, allowInteger,
                            noCase, ref error);
                    }

                    if (enumValue != null)
                    {
                        enumList.Add((Enum)enumValue);
                    }
                    else
                    {
                        errors.Add(error);
                        goto error;
                    }
                }

                //
                // NOTE: If we get this far, all elements in the list were
                //       successfully interpreted as a System.Enum object.
                //
                value = enumList;

                return ReturnCode.Ok;
            }
            else
            {
                errors.Add(error);
            }

        error:

            errors.Insert(0, String.Format(
                "expected {0} enumeration list value but got {1}",
                MarshalOps.GetErrorTypeName(enumType),
                FormatOps.WrapOrNull(text)));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static bool TryParseDateTime( /* NOTE: FOR USE BY THE Variant CLASS ONLY */
            string value,
            bool useKind,
            out DateTime dateTime
            )
        {
            DateTime dateTimeValue = DateTime.MinValue;

            if (DateTime.TryParse(
                    value, GetDateTimeFormatProvider(),
                    dateTimeStyles, out dateTimeValue))
            {
                dateTime = useKind ?
                    DateTime.SpecifyKind(dateTimeValue,
                        ObjectOps.GetDefaultDateTimeKind()) :
                    dateTimeValue;

                return true;
            }

            dateTime = DateTime.MinValue;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetDateTime(
            string text,
            string format,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref DateTime value
            )
        {
            Result error = null;

            return GetDateTime(
                text, format, kind, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDateTime(
            string text,
            string format,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDateTime(
                text, format, kind, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDateTime(
            string text,
            string format,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                DateTime dateTime;

                if (format != null)
                {
                    if (DateTime.TryParseExact(text, format,
                            GetDateTimeFormatProvider(cultureInfo),
                            dateTimeStyles, out dateTime))
                    {
                        value = DateTime.SpecifyKind(dateTime, kind);
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    if (DateTime.TryParse(text,
                            GetDateTimeFormatProvider(cultureInfo),
                            dateTimeStyles, out dateTime))
                    {
                        value = DateTime.SpecifyKind(dateTime, kind);
                        return ReturnCode.Ok;
                    }
                }
            }

            if (format != null)
                error = String.Format(
                    "unable to convert date-time string {0} using format {1}",
                    FormatOps.WrapOrNull(text), FormatOps.WrapOrNull(format));
            else
                error = String.Format(
                    "unable to convert date-time string {0}",
                    FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref DateTime value
            )
        {
            Result error = null;

            return GetDateTime2(
                text, format, flags, kind, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDateTime2(
                text, format, flags, kind, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDateTime2(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref DateTime value,
            ref Result error,
            ref Exception exception
            )
        {
            //
            // NOTE: Do we want to translate the DateTime formats recognized by
            //       Tcl to those recognized by the .NET Framework?
            //
            if (FlagOps.HasFlags(flags, ValueFlags.DateTimeFormat, true))
            {
                format = FormatOps.TranslateDateTimeFormats(
                    cultureInfo, TimeZone.CurrentTimeZone, format,
                    DateTime.MinValue, TimeOps.UnixEpoch, true, false);
            }

            //
            // NOTE: First, try to parse the text as a DateTime value, possibly
            //       using the format supplied by the caller.
            //
            if (FlagOps.HasFlags(flags, ValueFlags.DateTime, true) &&
                (GetDateTime(text, format, kind, cultureInfo,
                    ref value) == ReturnCode.Ok))
            {
                return ReturnCode.Ok;
            }
            else
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.Strict, true))
                {
                    //
                    // NOTE: Fallback to attempting to parse the text as an
                    //       integer and then using that to build the DateTime
                    //       value.
                    //
                    long longValue = 0;

                    if (FlagOps.HasFlags(flags, ValueFlags.WideInteger, true) &&
                        (GetWideInteger2(text, flags, cultureInfo,
                            ref longValue) == ReturnCode.Ok))
                    {
                        try
                        {
                            value = DateTime.SpecifyKind(new DateTime(longValue), kind);
                            return ReturnCode.Ok;
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                    }
                }
            }

            if (format != null)
                error = String.Format(
                    "unable to convert date-time string {0} using format {1}",
                    FormatOps.WrapOrNull(text), FormatOps.WrapOrNull(format));
            else
                error = String.Format(
                    "unable to convert date-time string {0}",
                    FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by SetupOps ONLY.
        //
        internal static ReturnCode GetDateTime3(
            string text,
            ValueFlags flags,
            DateTimeKind kind,
            ref DateTime value,
            ref Result error
            )
        {
            return GetDateTime2(
                text, ObjectOps.GetDefaultDateTimeFormat(), flags,
                kind, GetDefaultCulture(), ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetTimeSpan(
            string text,
            CultureInfo cultureInfo,
            ref TimeSpan value
            )
        {
            Result error = null;

            return GetTimeSpan(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetTimeSpan(
            string text,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetTimeSpan(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTimeSpan(
            string text,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
                if (TimeSpan.TryParse(text, out value))
                    return ReturnCode.Ok;

            error = String.Format(
                "unable to convert time-span string {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan value
            )
        {
            Result error = null;

            return GetTimeSpan2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetTimeSpan2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTimeSpan2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref TimeSpan value,
            ref Result error,
            ref Exception exception
            )
        {
            if (FlagOps.HasFlags(flags, ValueFlags.TimeSpan, true) &&
                (GetTimeSpan(text, cultureInfo, ref value) == ReturnCode.Ok))
            {
                return ReturnCode.Ok;
            }
            else
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.Strict, true))
                {
                    long longValue = 0;

                    if (FlagOps.HasFlags(flags, ValueFlags.WideInteger, true) &&
                        (GetWideInteger2(text, flags, cultureInfo,
                            ref longValue) == ReturnCode.Ok))
                    {
                        try
                        {
                            value = new TimeSpan(longValue);

                            return ReturnCode.Ok;
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                    }
                }
            }

            error = String.Format(
                "unable to convert time-span string {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsStartName(
            string text
            )
        {
            return String.Compare(
                text, startName, StringComparisonType) == 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsEndName(
            string text
            )
        {
            return String.Compare(
                text, endName, StringComparisonType) == 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsCountName(
            string text
            )
        {
            return String.Compare(
                text, countName, StringComparisonType) == 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode CheckIndex(
            int index,
            int firstIndex,
            int lastIndex,
            bool strict,
            ref Result error
            )
        {
            if (!strict)
                return ReturnCode.Ok;

            if ((firstIndex == Index.Invalid) || (lastIndex == Index.Invalid))
            {
                error = badIndexBoundsError;
                return ReturnCode.Error;
            }

            if ((index < firstIndex) || (index > lastIndex))
            {
                error = String.Format(
                    "index {0} out-of-bounds, must be between {1} and {2}",
                    index, firstIndex, lastIndex);

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Result GetIndexError(
            string text,
            ResultList errors,
            ValueFlags flags
            )
        {
            if ((errors == null) ||
                FlagOps.HasFlags(flags, ValueFlags.Verbose, true))
            {
                return errors;
            }

            if (errors.Count > 0)
                return errors[errors.Count - 1];

            return String.Format(
                badIndexError2, FormatOps.WrapOrNull(text));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddIndexError(
            int partIndex,
            Result error,
            ref ResultList errors
            )
        {
            if (error != null)
            {
                if (errors == null)
                    errors = new ResultList();

                if (partIndex > 0)
                {
                    errors.Add(String.Format(
                        "while processing index string part #{0}",
                        partIndex));
                }
                else
                {
                    errors.Add("while processing index string");
                }

                errors.Add(error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetIndex(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            int count,
            int partIndex,
            int firstIndex,
            int lastIndex,
            bool strict,
            ref int value,
            ref ResultList errors
            )
        {
            if (FlagOps.HasFlags(flags, ValueFlags.NamedIndex, true))
            {
                if (IsStartName(text))
                {
                    if (!strict || (firstIndex != Index.Invalid))
                    {
                        value = firstIndex;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, badIndexBoundsError,
                            ref errors);
                    }
                }

                if (IsEndName(text))
                {
                    if (!strict || (lastIndex != Index.Invalid))
                    {
                        value = lastIndex;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, badIndexBoundsError,
                            ref errors);
                    }
                }

                if (IsCountName(text))
                {
                    value = count;
                    return ReturnCode.Ok;
                }
            }

            int intValue;
            Result localError;

            if (partIndex > 0)
            {
                if (FlagOps.HasFlags(flags, ValueFlags.WithOffset, true))
                {
                    intValue = 0;
                    localError = null;

                    if (GetInteger2(
                            text, flags, cultureInfo, ref intValue,
                            ref localError) == ReturnCode.Ok)
                    {
                        localError = null;

                        if (CheckIndex(
                                intValue, firstIndex, lastIndex, strict,
                                ref localError) == ReturnCode.Ok)
                        {
                            value = intValue;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            AddIndexError(
                                partIndex, localError, ref errors);
                        }
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(
                        partIndex, "unsupported index offset",
                        ref errors);
                }
            }
            else
            {
                intValue = 0;
                localError = null;

                if (FlagOps.HasFlags(flags, ValueFlags.Integer, true) &&
                    GetInteger2(
                        text, flags, cultureInfo, ref intValue,
                        ref localError) == ReturnCode.Ok)
                {
                    localError = null;

                    if (CheckIndex(
                            intValue, firstIndex, lastIndex, strict,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = intValue;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(
                        partIndex, localError, ref errors);

                    bool boolValue = false;
                    localError = null;

                    if (FlagOps.HasFlags(flags, ValueFlags.Boolean, true) &&
                        GetBoolean2(
                            text, flags, cultureInfo, ref boolValue,
                            ref localError) == ReturnCode.Ok)
                    {
                        intValue = ConversionOps.ToInt(boolValue);
                        localError = null;

                        if (CheckIndex(
                                intValue, firstIndex, lastIndex, strict,
                                ref localError) == ReturnCode.Ok)
                        {
                            value = intValue;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            AddIndexError(
                                partIndex, localError, ref errors);
                        }
                    }
                    else
                    {
                        AddIndexError(
                            partIndex, localError, ref errors);
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetIndex(
            string text,
            int count,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error
            )
        {
            //
            // NOTE: This method [later] assumes that this string is not null;
            //       therefore, disallow it up-front.
            //
            if (String.IsNullOrEmpty(text))
            {
                error = "invalid index string";
                return ReturnCode.Error;
            }

            //
            // NOTE: Before doing anything else, figure out the first and last
            //       valid indexes [for the list].
            //
            int firstIndex;
            int lastIndex;

            if (count > 0)
            {
                firstIndex = 0;
                lastIndex = (count - 1);
            }
            else
            {
                firstIndex = Index.Invalid;
                lastIndex = Index.Invalid;
            }

            //
            // NOTE: Is strict bounds checking enabled for all index
            //       values?
            //
            bool strict = FlagOps.HasFlags(flags, ValueFlags.Strict, true);

            //
            // NOTE: First, try to interpret the entire string as one
            //       index value.
            //
            int intValue0 = 0;
            ResultList errors = null;

            if (GetIndex(
                    text, flags, cultureInfo, count, 0,
                    firstIndex, lastIndex, strict,
                    ref intValue0, ref errors) == ReturnCode.Ok)
            {
                value = intValue0;
                return ReturnCode.Ok;
            }

            //
            // NOTE: Next, try to match the regular expression pattern
            //       used for the special "index[+-]offset" syntax.
            //
            if (startEndPlusMinusIndexRegEx == null)
            {
                AddIndexError(
                    0, String.Format(badIndexError1,
                    FormatOps.WrapOrNull(text)),
                    ref errors);

                error = GetIndexError(text, errors, flags);
                return ReturnCode.Error;
            }

            Match match = startEndPlusMinusIndexRegEx.Match(text);

            if ((match == null) || !match.Success)
            {
                AddIndexError(
                    0, String.Format(badIndexError2,
                    FormatOps.WrapOrNull(text)),
                    ref errors);

                error = GetIndexError(text, errors, flags);
                return ReturnCode.Error;
            }

            //
            // NOTE: Attempt to figure out the first value.
            //
            string matchValue1 = RegExOps.GetMatchValue(match, 1);
            int intValue1 = 0;

            if (GetIndex(
                    matchValue1, flags, cultureInfo, count, 1,
                    firstIndex, lastIndex, strict, ref intValue1,
                    ref errors) != ReturnCode.Ok)
            {
                error = GetIndexError(matchValue1, errors, flags);
                return ReturnCode.Error;
            }

            //
            // NOTE: Attempt to figure out the second value.
            //
            string matchValue2 = RegExOps.GetMatchValue(match, 3);
            int intValue2 = 0;

            if (GetIndex(
                    matchValue2, flags, cultureInfo, count, 2,
                    firstIndex, lastIndex, strict, ref intValue2,
                    ref errors) != ReturnCode.Ok)
            {
                error = GetIndexError(matchValue2, errors, flags);
                return ReturnCode.Error;
            }

            //
            // NOTE: Do we need to add or subtract the first and second
            //       values?
            //
            string matchValue3 = RegExOps.GetMatchValue(match, 2);

            if (String.IsNullOrEmpty(matchValue3))
            {
                AddIndexError(
                    3, String.Format(badIndexError2,
                    FormatOps.WrapOrNull(text)),
                    ref errors);

                error = GetIndexError(matchValue3, errors, flags);
                return ReturnCode.Error;
            }

            char @operator = matchValue3[0];
            int intValue3;
            Result localError;

            if (@operator == Characters.PlusSign)
            {
                intValue3 = intValue1 + intValue2;
                localError = null;

                if (CheckIndex(
                        intValue3, firstIndex, lastIndex, strict,
                        ref localError) == ReturnCode.Ok)
                {
                    value = intValue3;
                    return ReturnCode.Ok;
                }
                else
                {
                    AddIndexError(3, localError, ref errors);
                }
            }
            else if (@operator == Characters.MinusSign)
            {
                intValue3 = intValue1 - intValue2;
                localError = null;

                if (CheckIndex(
                        intValue3, firstIndex, lastIndex, strict,
                        ref localError) == ReturnCode.Ok)
                {
                    value = intValue3;
                    return ReturnCode.Ok;
                }
                else
                {
                    AddIndexError(3, localError, ref errors);
                }
            }
            else if (@operator == Characters.Asterisk)
            {
                intValue3 = intValue1 * intValue2;
                localError = null;

                if (CheckIndex(
                        intValue3, firstIndex, lastIndex, strict,
                        ref localError) == ReturnCode.Ok)
                {
                    value = intValue3;
                    return ReturnCode.Ok;
                }
                else
                {
                    AddIndexError(3, localError, ref errors);
                }
            }
            else if (@operator == Characters.Slash)
            {
                if (intValue2 != 0)
                {
                    intValue3 = intValue1 / intValue2;
                    localError = null;

                    if (CheckIndex(
                            intValue3, firstIndex, lastIndex, strict,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = intValue3;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(3, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(3, String.Format(
                        "cannot divide {1} by zero (via {0}) for index",
                        FormatOps.WrapOrNull(Characters.Slash),
                        intValue1), ref errors);
                }
            }
            else if (@operator == Characters.PercentSign)
            {
                if (intValue2 != 0)
                {
                    intValue3 = intValue1 % intValue2;
                    localError = null;

                    if (CheckIndex(
                            intValue3, firstIndex, lastIndex, strict,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = intValue3;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        AddIndexError(3, localError, ref errors);
                    }
                }
                else
                {
                    AddIndexError(3, String.Format(
                        "cannot divide {1} by zero (via {0}) for index",
                        FormatOps.WrapOrNull(Characters.PercentSign),
                        intValue1), ref errors);
                }
            }
            else
            {
                AddIndexError(3,
                    String.Format(badIndexOperatorError,
                    FormatOps.WrapOrNull(matchValue3)),
                    ref errors);
            }

            error = GetIndexError(text, errors, flags);
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetIndexList(
            Interpreter interpreter,
            string text,
            int count,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IntList value
            )
        {
            Result error = null;

            return GetIndexList(
                interpreter, text, count, flags, cultureInfo,
                ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetIndexList(
            Interpreter interpreter,
            string text,
            int count,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref IntList value,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                StringList list = null;

                if (Parser.SplitList(
                        interpreter, text, 0, Length.Invalid, true,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    IntList intList = new IntList();

                    foreach (string element in list)
                    {
                        int index = Index.Invalid;

                        if (GetIndex(
                                element, count, flags, cultureInfo,
                                ref index, ref error) == ReturnCode.Ok)
                        {
                            intList.Add(index);
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: If we get this far, all elements in the list were
                    //       successfully interpreted as indexes.
                    //
                    value = intList;

                    return ReturnCode.Ok;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            error = String.Format(
                "expected index list value but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static FrameResult GetCallFrame(
            string text,
            LevelFlags levelFlags,
            CallStack callStack,
            ICallFrame globalFrame,
            ICallFrame currentGlobalFrame,
            ICallFrame currentFrame,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref ICallFrame frame,
            ref Result error
            )
        {
            bool mark = false;
            bool absolute = false;
            bool super = false;
            int level = 0;

            return GetCallFrame(
                text, levelFlags, callStack, globalFrame,
                currentGlobalFrame, currentFrame, hasFlags,
                notHasFlags, hasAll, notHasAll, valueFlags,
                cultureInfo, ref mark, ref absolute,
                ref super, ref level, ref frame, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static FrameResult GetCallFrame(
            string text,
            LevelFlags levelFlags,
            CallStack callStack,
            ICallFrame globalFrame,
            ICallFrame currentGlobalFrame,
            ICallFrame currentFrame,
            CallFrameFlags hasFlags,
            CallFrameFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref bool mark,
            ref bool absolute,
            ref bool super,
            ref int level,
            ref ICallFrame frame,
            ref Result error
            )
        {
            FrameResult frameResult = FrameResult.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                string localText = text;
                bool localMark = false;
                bool localAbsolute = false;
                bool localSuper = false;
                int localLevel = 0;

                if (localText[0] == Characters.NumberSign)
                {
                    //
                    // HACK: Allow, through the usage of "##<integer>" notation,
                    //       traversal of all call frames, including invisible
                    //       ones.  This feature is not supported by Tcl.
                    //
                    if ((localText.Length >= 2) &&
                        (localText[1] == Characters.NumberSign))
                    {
                        if (FlagOps.HasFlags(levelFlags,
                                LevelFlags.Absolute | LevelFlags.Invisible, true))
                        {
                            //
                            // NOTE: Skip the leading "##" in the level
                            //       specification.
                            //
                            localText = localText.Substring(2);

                            //
                            // NOTE: No, do not mark any call frames.
                            //
                            localMark = false;

                            //
                            // NOTE: We are going to perform an "absolute"
                            //       call frame search.
                            //
                            localAbsolute = true;

                            //
                            // NOTE: Skip using the current global frame
                            //       and use the outer global frame.
                            //
                            localSuper = true;

                            //
                            // NOTE: Make sure we include invisible call
                            //       frames (this is not Tcl compatible).
                            //
                            notHasFlags &= ~CallFrameFlags.Invisible;

                            //
                            // NOTE: Indicate to the caller that a level
                            //       specification was parsed.
                            //
                            frameResult = FrameResult.Specific;
                        }
                        else
                        {
                            goto badLevel;
                        }
                    }
                    else
                    {
                        if (FlagOps.HasFlags(levelFlags, LevelFlags.Absolute, true))
                        {
                            //
                            // NOTE: Skip the leading "#" in the level
                            //       specification.
                            //
                            localText = localText.Substring(1);

                            //
                            // NOTE: Yes, we need to mark the call
                            //       frames we use.
                            //
                            localMark = true;

                            //
                            // NOTE: We are going to perform an "absolute"
                            //       call frame search.
                            //
                            localAbsolute = true;

                            //
                            // NOTE: Make use of the current global frame.
                            //
                            localSuper = false;

                            //
                            // NOTE: Indicate to the caller that a level
                            //       specification was parsed.
                            //
                            frameResult = FrameResult.Specific;
                        }
                        else
                        {
                            goto badLevel;
                        }
                    }
                }
                else if (Parser.IsInteger(localText[0], false))
                {
                    if (FlagOps.HasFlags(levelFlags, LevelFlags.Relative, true))
                    {
                        //
                        // NOTE: Yes, we need to mark the call frames
                        //       we use.
                        //
                        localMark = true;

                        //
                        // NOTE: We are going to perform an "relative"
                        //       call frame search.
                        //
                        localAbsolute = false;

                        //
                        // NOTE: Make use of the current global frame.
                        //
                        localSuper = false;

                        //
                        // NOTE: Indicate to the caller that a level
                        //       specification was parsed.
                        //
                        frameResult = FrameResult.Specific;
                    }
                    else
                    {
                        goto badLevel;
                    }
                }
                else if (localText[0] == Characters.MinusSign)
                {
                    //
                    // NOTE: Indicate to the caller that a level
                    //       specification was parsed.
                    //
                    frameResult = FrameResult.Specific;

                    //
                    // NOTE: Negative levels are not supported.
                    //
                    goto badLevel;
                }
                else if (!FlagOps.HasFlags(valueFlags, ValueFlags.Strict, true))
                {
                    //
                    // NOTE: Yes, we need to mark the call frames
                    //       we use.
                    //
                    localMark = true;

                    //
                    // NOTE: We are going to perform an "relative"
                    //       call frame search.
                    //
                    localAbsolute = false;

                    //
                    // NOTE: Make use of the current global frame.
                    //
                    localSuper = false;

                    //
                    // NOTE: Use the call frame of the caller if
                    //       one is not specified.
                    //
                    localLevel = 1; // upvar OR uplevel <default>

                    //
                    // NOTE: Indicate to the caller that we are
                    //       using the default level specification.
                    //
                    frameResult = FrameResult.Default;
                }
                else
                {
                    goto badLevel;
                }

                //
                // NOTE: Do we need to parse a level specification
                //       integer?
                //
                if ((frameResult != FrameResult.Default) &&
                    (GetInteger2(localText, ValueFlags.AnyInteger,
                        cultureInfo, ref localLevel,
                        ref error) != ReturnCode.Ok))
                {
                    return FrameResult.Invalid;
                }

                //
                // NOTE: Now perform the actual call frame search.
                //
                if (CallFrameOps.GetOrFind(
                        callStack, globalFrame, currentGlobalFrame,
                        currentFrame, localAbsolute, localSuper,
                        localLevel, hasFlags, notHasFlags, hasAll,
                        notHasAll, ref frame) == ReturnCode.Ok)
                {
                    //
                    // NOTE: Let the caller know what kind of call
                    //       frame search we just performed.
                    //
                    mark = localMark;
                    absolute = localAbsolute;
                    super = localSuper;
                    level = localLevel;
                }
                else
                {
                    goto badLevel;
                }
            }
            else
            {
                error = "invalid level";
            }

            return frameResult;

        badLevel:

            error = String.Format(
                "bad level {0}",
                (frameResult == FrameResult.Default) ?
                    FormatOps.WrapOrNull(FormatOps.Level(absolute, 1)) :
                    FormatOps.WrapOrNull(text));

            return FrameResult.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value
            )
        {
            Result error = null;

            return GetBoolean2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetBoolean2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the [string is boolean] sub-command only.
        //
        internal static ReturnCode GetBoolean5(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value
            )
        {
            Result error = null;
            Exception exception = null;

            return GetBoolean5(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the [string is boolean] sub-command only.
        //
        private static ReturnCode GetBoolean5(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo, /* NOT USED */
            ref bool value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (TryParseBooleanOnly(text, flags, ref value))
            {
                return ReturnCode.Ok;
            }
            else
            {
                error = ScriptOps.BadValue(
                    null, "boolean", text,
                    Enum.GetNames(typeof(Boolean)), null, null);

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryParseBooleanOnly(
            string text,
            ValueFlags flags,
            ref bool value
            )
        {
            bool wasInteger = false;

            return TryParseBooleanOnly(
                text, GetComparisonType(flags), ref value, ref wasInteger);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryParseBooleanOnly(
            string text,
            StringComparison comparisonType,
            ref bool value
            )
        {
            bool wasInteger = false;

            return TryParseBooleanOnly(
                text, comparisonType, ref value, ref wasInteger);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool TryParseBooleanOnly(
            string text,
            StringComparison comparisonType,
            ref bool value,
            ref bool wasInteger
            )
        {
            int length;

            if (!StringOps.IsNullOrEmpty(text, out length))
            {
                #region Tcl and Eagle (Part 1)
                if (String.Compare(text, ZeroString, comparisonType) == 0)
                {
                    value = false;
                    wasInteger = true;

                    return true;
                }

                if (String.Compare(text, OneString, comparisonType) == 0)
                {
                    value = true;
                    wasInteger = true;

                    return true;
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                /* MINIMUM: "true" */
                /* MAXIMUM: "false" */
                if ((length >= 4) && (length <= 5))
                {
                    if (String.Compare(text, "true", comparisonType) == 0)
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (String.Compare(text, "false", comparisonType) == 0)
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////////////

                /* MINIMUM: "no" */
                /* MAXIMUM: "yes" */
                if ((length >= 2) && (length <= 3))
                {
                    if (String.Compare(text, "yes", comparisonType) == 0)
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (String.Compare(text, "no", comparisonType) == 0)
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Eagle Only (Part 1)
                /* MINIMUM: "enable" */
                /* MAXIMUM: "disabled" */
                if ((length >= 6) && (length <= 8))
                {
                    if (String.Compare(text, "enable", comparisonType) == 0)
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (String.Compare(text, "disable", comparisonType) == 0)
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }

                    ///////////////////////////////////////////////////////////////////////////////////

                    if (String.Compare(text, "enabled", comparisonType) == 0)
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (String.Compare(text, "disabled", comparisonType) == 0)
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }
                #endregion

                ///////////////////////////////////////////////////////////////////////////////////////

                #region Tcl and Eagle (Part 2)
                /* MINIMUM: "on" (less would be ambiguous) */
                /* MAXIMUM: "off" */
                if ((length >= 2) && (length <= 3))
                {
                    if (String.Compare(text, "on", comparisonType) == 0)
                    {
                        value = true;
                        wasInteger = false;

                        return true;
                    }

                    if (String.Compare(text, "off", comparisonType) == 0)
                    {
                        value = false;
                        wasInteger = false;

                        return true;
                    }
                }
                #endregion
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetBoolean2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            bool allowBooleanString = FlagOps.HasFlags(
                flags, ValueFlags.AllowBooleanString, true);

            if (allowBooleanString)
            {
                //
                // NOTE *HACK* The case-insensitive comparison type is used
                //      here for Tcl compatibility.
                //
                if (TryParseBooleanOnly( /* FAST */
                        text, NoCaseStringComparisonType, ref value))
                {
                    return ReturnCode.Ok;
                }

                if (!FlagOps.HasFlags(flags, ValueFlags.Fast, true))
                {
                    object enumValue = EnumOps.TryParseEnum(
                        typeof(Boolean), text, true, true);

                    if (enumValue is Boolean)
                    {
                        value = ConversionOps.ToBool((Boolean)enumValue);

                        return ReturnCode.Ok;
                    }
                }

                if (FlagOps.HasFlags(flags, ValueFlags.Strict, true))
                {
                    error = ScriptOps.BadValue(
                        null, "boolean", text,
                        Enum.GetNames(typeof(Boolean)), null, null);

                    return ReturnCode.Error;
                }
            }

            long longValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok))
            {
                value = ConversionOps.ToBool(longValue);

                return ReturnCode.Ok;
            }
            else if (allowBooleanString)
            {
                error = ScriptOps.BadValue(
                    null, "boolean", text,
                    Enum.GetNames(typeof(Boolean)), null,
                    ", or an integer");
            }
            else
            {
                error = String.Format(
                    "expected wide integer but got {0}",
                    FormatOps.WrapOrNull(text));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by Engine.StringToBoolean ONLY.
        //
        internal static ReturnCode GetBoolean3(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool value,
            ref Result error
            )
        {
            if (getValue == null)
            {
                error = ScriptOps.BadValue(
                    null, "boolean", null,
                    Enum.GetNames(typeof(Boolean)), null, null);

                return ReturnCode.Error;
            }

            object innerValue = getValue.Value;

            if (innerValue is ValueType)
            {
                try
                {
                    value = ConversionOps.ToBool(innerValue); /* throw */

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }

            string text = getValue.String;

            //
            // NOTE *HACK* The case-insensitive comparison type is used
            //      here for Tcl compatibility.
            //
            if (TryParseBooleanOnly( /* FAST */
                    text, NoCaseStringComparisonType, ref value))
            {
                return ReturnCode.Ok;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.Fast, true))
            {
                object enumValue = EnumOps.TryParseEnum(
                    typeof(Boolean), text, true, true);

                if (enumValue is Boolean)
                {
                    value = ConversionOps.ToBool((Boolean)enumValue);

                    return ReturnCode.Ok;
                }
            }

            if (FlagOps.HasFlags(flags, ValueFlags.Strict, true))
            {
                error = ScriptOps.BadValue(
                    null, "boolean", text,
                    Enum.GetNames(typeof(Boolean)), null, null);

                return ReturnCode.Error;
            }

            long longValue = 0;

            if (FlagOps.HasFlags(
                    flags, ValueFlags.WideInteger, true) &&
                (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok))
            {
                value = ConversionOps.ToBool(longValue);

                return ReturnCode.Ok;
            }
            else
            {
                double doubleValue = 0.0;

                if (FlagOps.HasFlags(
                        flags, ValueFlags.Double, true) &&
                    (GetDouble(text, cultureInfo,
                        ref doubleValue) == ReturnCode.Ok))
                {
                    value = ConversionOps.ToBool(doubleValue);

                    return ReturnCode.Ok;
                }
                else
                {
                    error = ScriptOps.BadValue(
                        null, "boolean", text,
                        Enum.GetNames(typeof(Boolean)), null,
                        ", or a number");

                    return ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by SetupOps ONLY.
        //
        internal static ReturnCode GetBoolean4(
            string text,
            ValueFlags flags,
            ref bool value,
            ref Result error
            )
        {
            return GetBoolean2(
                text, flags, GetDefaultCulture(), ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetSingle2(
            string text,
            CultureInfo cultureInfo,
            ref float value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            ReturnCode code = ReturnCode.Error;

            stopIndex = Index.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;
                Result localError = null;

                while (length > 0)
                {
                    code = GetSingle(
                        text.Substring(0, length), cultureInfo,
                        ref value, ref localError, ref exception);

                    if (code == ReturnCode.Ok)
                        break;
                    else
                        length--;
                }

                if (length > 0)
                    //
                    // NOTE: One beyond the character we actually
                    //       succeeded at.
                    //
                    stopIndex = length;
                else
                    error = localError;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSingle(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref float value
            )
        {
            Result error = null;

            return GetSingle(getValue, cultureInfo, ref value, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSingle(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSingle(
                getValue, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSingle(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is float)
                {
                    value = (float)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToSingle(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to single",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetSingle(
                        getValue.String, cultureInfo, ref value,
                        ref error, ref exception);
                }
            }
            else
            {
                error = "expected single value but got null";

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetSingle(
            string text,
            CultureInfo cultureInfo,
            ref float value
            )
        {
            Result error = null;

            return GetSingle(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSingle(
            string text,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSingle(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSingle(
            string text,
            CultureInfo cultureInfo,
            ref float value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (TryLookupNamedSingle(text, ref value))
                    return ReturnCode.Ok;

                if (float.TryParse(text, singleStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected floating-point number but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetDouble2(
            string text,
            CultureInfo cultureInfo,
            ref double value,
            ref int stopIndex
            )
        {
            Result error = null;

            return GetDouble2(
                text, cultureInfo, ref value, ref stopIndex, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by the Expression parser.
        //
        internal static ReturnCode GetDouble2(
            string text,
            CultureInfo cultureInfo,
            ref double value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble2(
                text, cultureInfo, ref value, ref stopIndex, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDouble2(
            string text,
            CultureInfo cultureInfo,
            ref double value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            ReturnCode code = ReturnCode.Error;

            stopIndex = Index.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;
                Result localError = null;

                while (length > 0)
                {
                    code = GetDouble(
                        text.Substring(0, length), cultureInfo,
                        ref value, ref localError, ref exception);

                    if (code == ReturnCode.Ok)
                        break;
                    else
                        length--;
                }

                if (length > 0)
                    //
                    // NOTE: One beyond the character we actually
                    //       succeeded at.
                    //
                    stopIndex = length;
                else
                    error = localError;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetDouble(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble(
                getValue, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDouble(
            IGetValue getValue,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is double)
                {
                    value = (double)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToDouble(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to double",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetDouble(
                        getValue.String, cultureInfo, ref value,
                        ref error, ref exception);
                }
            }
            else
            {
                error = "expected double value but got null";

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetDouble(
            string text,
            CultureInfo cultureInfo,
            ref double value
            )
        {
            Result error = null;

            return GetDouble(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetDouble(
            string text,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDouble(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDouble(
            string text,
            CultureInfo cultureInfo,
            ref double value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (TryLookupNamedDouble(text, ref value))
                    return ReturnCode.Ok;

                if (double.TryParse(text, doubleStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected floating-point number but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetDecimal2(
            string text,
            CultureInfo cultureInfo,
            ref decimal value,
            ref int stopIndex
            )
        {
            Result error = null;

            return GetDecimal2(
                text, cultureInfo, ref value, ref stopIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDecimal2(
            string text,
            CultureInfo cultureInfo,
            ref decimal value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDecimal2(
                text, cultureInfo, ref value, ref stopIndex, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDecimal2(
            string text,
            CultureInfo cultureInfo,
            ref decimal value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            ReturnCode code = ReturnCode.Error;

            stopIndex = Index.Invalid;

            if (!String.IsNullOrEmpty(text))
            {
                int length = text.Length;
                Result localError = null;

                while (length > 0)
                {
                    code = GetDecimal(
                        text.Substring(0, length), cultureInfo,
                        ref value, ref localError, ref exception);

                    if (code == ReturnCode.Ok)
                        break;
                    else
                        length--;
                }

                if (length > 0)
                    //
                    // NOTE: One beyond the character we actually
                    //       succeeded at.
                    //
                    stopIndex = length;
                else
                    error = localError;
            }

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetDecimal(
            string text,
            CultureInfo cultureInfo,
            ref decimal value
            )
        {
            Result error = null;

            return GetDecimal(
                text, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetDecimal(
            string text,
            CultureInfo cultureInfo,
            ref decimal value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetDecimal(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetDecimal(
            string text,
            CultureInfo cultureInfo,
            ref decimal value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (decimal.TryParse(text, decimalStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected fixed-point number but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetUnsignedWideInteger(
            string text,
            CultureInfo cultureInfo,
            ref ulong value
            )
        {
            Result error = null;

            return GetUnsignedWideInteger(
                text, cultureInfo, ref value, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetUnsignedWideInteger( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref ulong value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUnsignedWideInteger(
                text, cultureInfo, ref value, ref error, ref exception);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetUnsignedWideInteger(
            string text,
            CultureInfo cultureInfo,
            ref ulong value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (ulong.TryParse(text, wideIntegerStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected unsigned wide integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetWideInteger( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetWideInteger(
                text, cultureInfo, ref value, ref error, ref exception);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetWideInteger(
            string text,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (long.TryParse(text, wideIntegerStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected wide integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetWideInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetWideInteger2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetWideInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is int)
                {
                    value = ConversionOps.ToLong((int)innerValue);

                    return ReturnCode.Ok;
                }
                else if (innerValue is long)
                {
                    value = (long)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToWideInteger(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to wide integer",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetWideInteger2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = "expected wide integer value but got null";

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value
            )
        {
            Result error = null;

            return GetWideInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetWideInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetUnsignedWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ulong value
            )
        {
            Result error = null;

            return GetUnsignedWideInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetUnsignedWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ulong value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUnsignedWideInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ParseWideIntegerWithRadixPrefix(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref bool done,
            ref long value,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(text))
            {
                error = String.Format(
                    "expected wide integer but got {0}",
                    FormatOps.WrapOrNull(text));

                return ReturnCode.Error;
            }

            ValueFlags prefixFlags = ValueFlags.None;
            int prefixLength = 0;

            CheckRadixPrefix(text, cultureInfo, ref prefixFlags, ref prefixLength);

            int valueLength = text.Length - prefixLength;

            if (FlagOps.HasFlags(prefixFlags, ValueFlags.HexadecimalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.HexadecimalRadix, true))
                {
                    error = "hexadecimal wide integer format not supported";
                    return ReturnCode.Error;
                }

                if (Parser.ParseHexadecimal(text.Substring(prefixLength), 0,
                        valueLength, ref value) == valueLength)
                {
                    done = true;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "expected hexadecimal wide integer but got {0}",
                        FormatOps.WrapOrNull(text));
                }
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.DecimalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.DecimalRadix, true))
                {
                    error = "decimal wide integer format not supported";
                    return ReturnCode.Error;
                }

                if (Parser.ParseDecimal(text.Substring(prefixLength), 0,
                        valueLength, ref value) == valueLength)
                {
                    done = true;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "expected decimal wide integer but got {0}",
                        FormatOps.WrapOrNull(text));
                }
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.OctalRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.OctalRadix, true))
                {
                    error = "octal wide integer format not supported";
                    return ReturnCode.Error;
                }

                if (Parser.ParseOctal(text.Substring(prefixLength), 0,
                        valueLength, ref value) == valueLength)
                {
                    done = true;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "expected octal wide integer but got {0}",
                        FormatOps.WrapOrNull(text));
                }
            }
            else if (FlagOps.HasFlags(prefixFlags, ValueFlags.BinaryRadix, true))
            {
                if (!FlagOps.HasFlags(flags, ValueFlags.BinaryRadix, true))
                {
                    error = "binary wide integer format not supported";
                    return ReturnCode.Error;
                }

                if (Parser.ParseBinary(text.Substring(prefixLength), 0,
                        valueLength, ref value) == valueLength)
                {
                    done = true;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "expected binary wide integer but got {0}",
                        FormatOps.WrapOrNull(text));
                }
            }
            else
            {
                done = false;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref long value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            bool done = false;

            if (ParseWideIntegerWithRadixPrefix(
                    text, flags, cultureInfo, ref done, ref value,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (done)
                return ReturnCode.Ok;

            if (!FlagOps.HasFlags(flags, ValueFlags.DecimalRadix, true))
            {
                error = "decimal wide integer format not supported";
                return ReturnCode.Error;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.SignednessMask, false))
            {
                error = "no signedness is supported";
                return ReturnCode.Error;
            }

            ReturnCode code;
            Result localError = null;
            ResultList errors = null;

            if (FlagOps.HasFlags(flags, ValueFlags.DefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowSigned, true))
            {
                code = GetWideInteger(
                    text, cultureInfo, ref value, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                    return code;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (FlagOps.HasFlags(flags, ValueFlags.NonDefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowUnsigned, true))
            {
                ulong ulongValue = 0;
                localError = null;

                code = GetUnsignedWideInteger(
                    text, cultureInfo, ref ulongValue, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                {
                    value = ConversionOps.ToLong(ulongValue);
                    return code;
                }

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (errors != null)
            {
                error = errors;
            }
            else
            {
                error = String.Format(
                    "expected wide integer but got {0}",
                    FormatOps.WrapOrNull(text));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetUnsignedWideInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ulong value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            bool done = false;
            long longValue = 0;

            if (ParseWideIntegerWithRadixPrefix(
                    text, flags, cultureInfo, ref done, ref longValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (done)
            {
                value = ConversionOps.ToULong(longValue);
                return ReturnCode.Ok;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.DecimalRadix, true))
            {
                error = "decimal wide integer format not supported";
                return ReturnCode.Error;
            }

            if (!FlagOps.HasFlags(flags, ValueFlags.SignednessMask, false))
            {
                error = "no signedness is supported";
                return ReturnCode.Error;
            }

            ReturnCode code;
            Result localError = null;
            ResultList errors = null;

            if (FlagOps.HasFlags(flags, ValueFlags.DefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowUnsigned, true))
            {
                code = GetUnsignedWideInteger(
                    text, cultureInfo, ref value, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                    return code;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (FlagOps.HasFlags(flags, ValueFlags.NonDefaultSignedness, true) ||
                FlagOps.HasFlags(flags, ValueFlags.AllowSigned, true))
            {
                longValue = 0;
                localError = null;

                code = GetWideInteger(
                    text, cultureInfo, ref longValue, ref localError,
                    ref exception);

                if (code == ReturnCode.Ok)
                {
                    value = ConversionOps.ToULong(longValue);
                    return code;
                }

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            if (errors != null)
            {
                error = errors;
            }
            else
            {
                error = String.Format(
                    "expected unsigned wide integer but got {0}",
                    FormatOps.WrapOrNull(text));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetIntegerOrWideInteger(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number number
            )
        {
            Result error = null;

            return GetIntegerOrWideInteger(
                text, flags, cultureInfo, ref number, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetIntegerOrWideInteger(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number value,
            ref Result error
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue, ref error) == ReturnCode.Ok)
            {
                if ((longValue >= int.MinValue) &&
                    (longValue <= int.MaxValue))
                {
                    value = new Number(ConversionOps.ToInt(longValue));
                }
                else
                {
                    value = new Number(longValue);
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMatchMode2(
            Interpreter interpreter,
            string oldText,
            string newText,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref MatchMode value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetMatchMode2(
                interpreter, oldText, newText, flags, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetMatchMode2(
            Interpreter interpreter,
            string oldText,
            string newText,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref MatchMode value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            object enumValue = EnumOps.TryParseFlagsEnum(interpreter,
                typeof(MatchMode), oldText, newText, cultureInfo, true,
                FlagOps.HasFlags(flags, ValueFlags.Strict, true), true,
                ref error);

            if (enumValue is MatchMode)
            {
                value = (MatchMode)enumValue;

                return ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: We cannot nicely fallback to using long (wide) integer
                //       value processing here for a couple reasons:
                //
                //       1.  We would have to duplicate a lot of code from
                //           the EnumOps.TryParseFlagsEnum method that deals
                //           with combining the old and new values in one of
                //           several ways.
                //
                //       2.  Long (wide) integers are already handled by the
                //           EnumOps.TryParseFlagsEnum method itself.
                //
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetReturnCode2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ReturnCode value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetReturnCode2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetReturnCode2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ReturnCode value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            object enumValue = EnumOps.TryParseEnum(
                typeof(ReturnCode), text, true, true);

            if (enumValue is ReturnCode)
            {
                value = (ReturnCode)enumValue;

                return ReturnCode.Ok;
            }
            else
            {
                long longValue = 0;

                if (FlagOps.HasFlags(flags, ValueFlags.WideInteger, true) &&
                    (GetWideInteger2(text, flags, cultureInfo,
                        ref longValue) == ReturnCode.Ok))
                {
                    value = (ReturnCode)longValue;

                    return ReturnCode.Ok;
                }
                else
                {
                    error = ScriptOps.BadValue(
                        null, "completion code", text,
                        Enum.GetNames(typeof(ReturnCode)), null, ", or an integer");

                    return ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetByte( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetByte(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetByte(
            string text,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (byte.TryParse(text, byteStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected byte but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSignedByte( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSignedByte(
                text, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSignedByte(
            string text,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (sbyte.TryParse(text, byteStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected signed byte but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value
            )
        {
            Result error = null;

            return GetByte2(
                getValue, flags, cultureInfo, ref value, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetByte2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is byte)
                {
                    value = (byte)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToByte(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to byte",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetByte2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = "expected byte value but got null";

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value
            )
        {
            Result error = null;

            return GetByte2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetByte2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref byte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= byte.MinValue) &&
                    (longValue <= byte.MaxValue))
                {
                    value = ConversionOps.ToByte(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected byte but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetSignedByte2( /* NOT USED */
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSignedByte2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSignedByte2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is sbyte)
                {
                    value = (sbyte)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToSignedByte(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to signed byte",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetSignedByte2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = "expected signed byte value but got null";

                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSignedByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetSignedByte2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSignedByte2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref sbyte value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= sbyte.MinValue) &&
                    (longValue <= sbyte.MaxValue))
                {
                    value = ConversionOps.ToSByte(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected signed byte but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetNarrowInteger( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (short.TryParse(text, narrowIntegerStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected narrow integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNarrowInteger2( /* NOT USED */
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNarrowInteger2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNarrowInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is short)
                {
                    value = (short)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToNarrowInteger(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to narrow integer",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetNarrowInteger2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = "expected narrow integer value but got null";

                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value
            )
        {
            Result error = null;

            return GetNarrowInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNarrowInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref short value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= short.MinValue) &&
                    (longValue <= short.MaxValue))
                {
                    value = ConversionOps.ToShort(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected narrow integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetUnsignedNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ushort value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUnsignedNarrowInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetUnsignedNarrowInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref ushort value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= ushort.MinValue) &&
                    (longValue <= ushort.MaxValue))
                {
                    value = ConversionOps.ToUShort(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected unsigned narrow integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetCharacter( /* NOT USED */
            string text,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                //
                // NOTE: *SPECIAL CASE* Try to convert a string
                //       containing a numeric value into a single
                //       character.
                //
                long longValue = 0;

                if (long.TryParse(text, wideIntegerStyles,
                        GetNumberFormatProvider(cultureInfo), out longValue))
                {
                    if ((longValue >= char.MinValue) &&
                        (longValue <= char.MaxValue))
                    {
                        value = ConversionOps.ToChar(longValue);

                        return ReturnCode.Ok;
                    }
                }
            }

            error = String.Format(
                "expected character but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetCharacter2( /* NOT USED */
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetCharacter2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetCharacter2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is char)
                {
                    value = (char)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToCharacter(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to character",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetCharacter2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = "expected character value but got null";

                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetCharacter2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value
            )
        {
            Result error = null;

            return GetCharacter2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetCharacter2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetCharacter2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetCharacter2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref char value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= char.MinValue) &&
                    (longValue <= char.MaxValue))
                {
                    value = ConversionOps.ToChar(longValue);

                    return ReturnCode.Ok;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetInteger(
            string text,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                if (int.TryParse(text, integerStyles,
                        GetNumberFormatProvider(cultureInfo), out value))
                {
                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetInteger2(
                getValue, flags, cultureInfo, ref value, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetInteger2(
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (getValue != null)
            {
                object innerValue = getValue.Value;

                if (innerValue is int)
                {
                    value = (int)innerValue;

                    return ReturnCode.Ok;
                }
                else if (Number.IsSupported(innerValue))
                {
                    Number number = new Number(innerValue);

                    if (number.ToInteger(ref value))
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "could not convert {0} to integer",
                            FormatOps.WrapOrNull(innerValue));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: Defer to normal string processing.
                    //
                    return GetInteger2(
                        getValue.String, flags, cultureInfo,
                        ref value, ref error, ref exception);
                }
            }
            else
            {
                error = "expected integer value but got null";

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value
            )
        {
            Result error = null;

            return GetInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref int value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= int.MinValue) &&
                    (longValue <= int.MaxValue))
                {
                    value = ConversionOps.ToInt(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetUnsignedInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref uint value
            )
        {
            Result error = null;

            return GetUnsignedInteger2(
                text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetUnsignedInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref uint value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetUnsignedInteger2(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetUnsignedInteger2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref uint value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            long longValue = 0;

            if (GetWideInteger2(text, flags, cultureInfo,
                    ref longValue) == ReturnCode.Ok)
            {
                if ((longValue >= uint.MinValue) &&
                    (longValue <= uint.MaxValue))
                {
                    value = ConversionOps.ToUInt(longValue);

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "expected unsigned integer but got {0}",
                FormatOps.WrapOrNull(text));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetInterpreter(
            Interpreter interpreter,
            string text,
            InterpreterType type,
            ref Interpreter value
            )
        {
            Result error = null;

            return GetInterpreter(
                interpreter, text, type, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetInterpreter(
            Interpreter interpreter,
            string text,
            InterpreterType type,
            ref Interpreter value,
            ref Result error
            )
        {
            if (text != null)
            {
                if (text.Length == 0)
                {
                    value = interpreter;
                    return ReturnCode.Ok;
                }

                ResultList errors = new ResultList();

                if (FlagOps.HasFlags(type,
                        InterpreterType.Eagle | InterpreterType.Master, true))
                {
                    Result localError = null;

                    if (GlobalState.GetInterpreter(
                            text, LookupFlags.Interpreter, ref value,
                            ref localError) == ReturnCode.Ok)
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        errors.Add(localError);
                    }
                }

                if (FlagOps.HasFlags(type,
                        InterpreterType.Eagle | InterpreterType.Slave, true))
                {
                    if (interpreter != null)
                    {
                        Result localError = null;

                        if (interpreter.GetSlaveInterpreter(
                                text, LookupFlags.Interpreter,
                                FlagOps.HasFlags(type,
                                    InterpreterType.Nested, true), false,
                                ref value, ref localError) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            errors.Add(localError);
                        }
                    }
                    else
                    {
                        errors.Add("invalid interpreter");
                    }
                }

                error = errors;
            }
            else
            {
                error = "invalid interpreter name";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNestedMemberViaBinder(
            Interpreter interpreter,
            string text,
            TypedInstance typedInstance,
            MemberTypes memberTypes,
            BindingFlags bindingFlags,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref TypedMember value,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                IScriptBinder scriptBinder =
                    interpreter.Binder as IScriptBinder;

                if (scriptBinder == null)
                {
                    error = "invalid script binder";
                    return ReturnCode.Error;
                }

                return scriptBinder.GetMember(
                    text, typedInstance, memberTypes, bindingFlags,
                    valueFlags, cultureInfo, ref value, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNestedMember(
            Interpreter interpreter,
            string text,
            TypedInstance typedInstance,
            MemberTypes memberTypes,
            BindingFlags bindingFlags,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref TypedMember value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNestedMember(
                interpreter, text, typedInstance, memberTypes, bindingFlags,
                valueFlags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNestedMember(
            Interpreter interpreter,
            string text,
            TypedInstance typedInstance,
            MemberTypes memberTypes,
            BindingFlags bindingFlags,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref TypedMember value,
            ref Result error,
            ref Exception exception
            )
        {
            ReturnCode code = GetNestedMemberViaBinder(
                interpreter, text, typedInstance, memberTypes,
                bindingFlags, valueFlags, cultureInfo, ref value,
                ref error);

            if (code == ReturnCode.Break)
                code = ReturnCode.Ok; // NOTE: For our caller.

            if ((code == ReturnCode.Ok) || (code == ReturnCode.Error))
                return code;

            if (interpreter != null)
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    if (!String.IsNullOrEmpty(text))
                    {
                        //
                        // NOTE: We require a valid typed object instance.
                        //
                        if (typedInstance != null)
                        {
                            //
                            // NOTE: Grab the initial type from the typed instance.  This
                            //       must be valid to continue; however, the contained
                            //       object instance may be null because we do not need it
                            //       if the initial member is static.
                            //
                            Type type = typedInstance.Type;
                            object @object = typedInstance.Object;
                            ObjectFlags objectFlags = typedInstance.ObjectFlags;

                            if (type != null)
                            {
                                if (FlagOps.HasFlags(valueFlags, ValueFlags.NoNested, true))
                                {
                                    //
                                    // NOTE: Nested member resolution has been forbidden by the
                                    //       caller.  Perform simple member name resolution.
                                    //
                                    try
                                    {
                                        //
                                        // NOTE: Construct the typed member object for use by the
                                        //       caller.
                                        //
                                        value = new TypedMember(
                                            type, ObjectFlags.None, @object, text, text,
                                            type.GetMember(text, memberTypes, bindingFlags));

                                        return ReturnCode.Ok;
                                    }
                                    catch (Exception e)
                                    {
                                        error = e;

                                        exception = e;
                                    }
                                }
                                else
                                {
                                    //
                                    // NOTE: Finally, split apart their object or type reference
                                    //       into pieces and attempt to traverse down the actual
                                    //       object they want.
                                    //
                                    string[] parts = MarshalOps.SplitTypeName(text);

                                    if (parts != null)
                                    {
                                        //
                                        // NOTE: How many parts are there?  There must be at least
                                        //       one.
                                        //
                                        int length = parts.Length;

                                        if (length > 1)
                                        {
                                            //
                                            // NOTE: Grab the binder for the interpreter now, as we will
                                            //       need it multiple times (in the loop, etc).
                                            //
                                            IBinder binder = interpreter.Binder;

                                            //
                                            // NOTE: This is a little tricky.  We traverse the parts in
                                            //       the array and attempt to use each one to lookup the
                                            //       next one via Type.InvokeMember; however, the last
                                            //       time through the loop is special because we use it
                                            //       to lookup the final list of members matching the
                                            //       specified name, member types, and binding flags.
                                            //
                                            for (int index = 0; index < length; index++)
                                            {
                                                //
                                                // NOTE: Grab the parts that we may need in the body.
                                                //
                                                string lastPart = (index > 0) ? parts[index - 1] : null;
                                                string part = parts[index];

                                                //
                                                // NOTE: Are we processing anything other than the last
                                                //       part?
                                                //
                                                if ((index + 1) < length)
                                                {
                                                    //
                                                    // NOTE: At this point, the part must be valid; otherwise,
                                                    //       we cannot lookup the remaining parts that were
                                                    //       specified.
                                                    //
                                                    if (type != null)
                                                    {
                                                        try
                                                        {
                                                            //
                                                            // NOTE: Try fetching the next part as a method with zero
                                                            //       arguments, a field, or a property of the current
                                                            //       part.  If this does not work (i.e. it throws an
                                                            //       exception), we are done.  This is allowed to
                                                            //       return null unless more parts remain to be looked
                                                            //       up.
                                                            //
                                                            @object = type.InvokeMember(part,
                                                                bindingFlags | MarshalOps.NestedObjectBindingFlags,
                                                                binder as Binder, @object, null, cultureInfo);

                                                            //
                                                            // NOTE: Now, get the type of the object we just fetched.
                                                            //       If the object instance is invalid here then so is
                                                            //       the type.
                                                            //
                                                            type = (@object != null) ? @object.GetType() : null;

                                                            //
                                                            // HACK: Make COM Interop objects work [slightly better].
                                                            //
                                                            if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                                                MarshalOps.IsSystemComObjectType(type))
                                                            {
                                                                type = MarshalOps.GetTypeFromComObject(
                                                                    interpreter, text, part, @object,
                                                                    interpreter.ObjectInterfaces, binder,
                                                                    cultureInfo, objectFlags, ref error);

                                                                if (type == null)
                                                                    return ReturnCode.Error;
                                                            }
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            //
                                                            // NOTE: Failure, we are done.  Give the caller our error
                                                            //       information.
                                                            //
                                                            error = e;

                                                            exception = e;

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: We cannot lookup the next part because the current
                                                        //       part is null.  This is considered a failure unless
                                                        //       the right flag is set.
                                                        //
                                                        if (FlagOps.HasFlags(
                                                                valueFlags, ValueFlags.StopOnNullMember, true))
                                                        {
                                                            value = new TypedMember(
                                                                null, ObjectFlags.None, null, lastPart,
                                                                MarshalOps.JoinTypeName(parts, index - 1),
                                                                null);

                                                            return ReturnCode.Continue;
                                                        }
                                                        else
                                                        {
                                                            error = String.Format(
                                                                "cannot process member part {0}, " +
                                                                "previous part {1}was null",
                                                                FormatOps.WrapOrNull(part),
                                                                (lastPart != null) ? String.Format(
                                                                "part {0} ", FormatOps.WrapOrNull(
                                                                lastPart)) : String.Empty);

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: At this point, the part must be valid; otherwise,
                                                    //       we cannot lookup the remaining part that was
                                                    //       specified.
                                                    //
                                                    if (type != null)
                                                    {
                                                        //
                                                        // NOTE: This is the last pass through the loop.  The caller
                                                        //       expects an array of zero or more member info objects
                                                        //       matching the specified name, member types, and binding
                                                        //       flags to perform overload resolution on.
                                                        //
                                                        try
                                                        {
                                                            //
                                                            // NOTE: Construct the typed member object for use by the
                                                            //       caller.
                                                            //
                                                            value = new TypedMember(
                                                                type, ObjectFlags.None, @object, part,
                                                                MarshalOps.JoinTypeName(parts, index),
                                                                type.GetMember(part, memberTypes,
                                                                bindingFlags));

                                                            return ReturnCode.Ok;
                                                        }
                                                        catch (Exception e)
                                                        {
                                                            error = e;

                                                            exception = e;

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: We cannot lookup the next part because the current
                                                        //       part is null.  This is considered a failure unless
                                                        //       the right flag is set.
                                                        //
                                                        if (FlagOps.HasFlags(
                                                                valueFlags, ValueFlags.StopOnNullMember, true))
                                                        {
                                                            value = new TypedMember(
                                                                null, ObjectFlags.None, null, lastPart,
                                                                MarshalOps.JoinTypeName(parts, index - 1),
                                                                null);

                                                            return ReturnCode.Continue;
                                                        }
                                                        else
                                                        {
                                                            error = String.Format(
                                                                "cannot process member part {0}, " +
                                                                "previous part {1}was null",
                                                                FormatOps.WrapOrNull(part),
                                                                (lastPart != null) ? String.Format(
                                                                "part {0} ", FormatOps.WrapOrNull(
                                                                lastPart)) : String.Empty);

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: There is only one member part.  Perform simple member
                                            //       name resolution.
                                            //
                                            try
                                            {
                                                //
                                                // NOTE: Construct the typed member object for use by the
                                                //       caller.
                                                //
                                                value = new TypedMember(
                                                    type, ObjectFlags.None, @object, text, text,
                                                    type.GetMember(text, memberTypes, bindingFlags));

                                                return ReturnCode.Ok;
                                            }
                                            catch (Exception e)
                                            {
                                                error = e;

                                                exception = e;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        error = "could not parse member name";
                                    }
                                }
                            }
                            else
                            {
                                error = "invalid type";
                            }
                        }
                        else
                        {
                            error = "invalid typed instance";
                        }
                    }
                    else
                    {
                        error = "invalid member name";
                    }
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNestedObjectViaBinder(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            BindingFlags bindingFlags,
            Type type,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref TypedInstance value,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                IScriptBinder scriptBinder =
                    interpreter.Binder as IScriptBinder;

                if (scriptBinder == null)
                {
                    error = "invalid script binder";
                    return ReturnCode.Error;
                }

                return scriptBinder.GetObject(
                    text, types, appDomain, bindingFlags, type, valueFlags,
                    cultureInfo, ref value, ref error);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNestedObject(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            BindingFlags bindingFlags,
            Type type,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref TypedInstance value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNestedObject(
                interpreter, text, types, appDomain, bindingFlags, type,
                valueFlags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNestedObject(
            Interpreter interpreter,
            string text,
            TypeList types,
            AppDomain appDomain,
            BindingFlags bindingFlags,
            Type type,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref TypedInstance value,
            ref Result error,
            ref Exception exception
            )
        {
            ReturnCode code = GetNestedObjectViaBinder(
                interpreter, text, types, appDomain, bindingFlags,
                type, valueFlags, cultureInfo, ref value, ref error);

            if (code == ReturnCode.Break)
                code = ReturnCode.Ok; // NOTE: For our caller.

            if ((code == ReturnCode.Ok) || (code == ReturnCode.Error))
                return code;

            if (interpreter != null)
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Grab the binder for the interpreter now, as we will
                    //       need it multiple times (in the loop, etc).
                    //
                    IBinder binder = interpreter.Binder;

                    //
                    // NOTE: *WARNING* Empty opaque object handle names are allowed,
                    //       please do not change this to "!String.IsNullOrEmpty".
                    //
                    if (text != null)
                    {
                        Type objectType = null;
                        ObjectFlags objectFlags = ObjectFlags.None;
                        object @object = null;

                        //
                        // NOTE: First, check for a verbatim object handle.
                        //
                        if (GetObject(
                                interpreter, text, ref objectType, ref objectFlags,
                                ref @object) == ReturnCode.Ok)
                        {
                            //
                            // HACK: Now, if applicable, check if this object can be used
                            //       in a "safe" interpreter.
                            //
                            if (interpreter.IsSafe() &&
                                !PolicyOps.IsTrustedObject(interpreter, text, objectFlags,
                                    @object, ref error))
                            {
                                return ReturnCode.Error;
                            }

                            //
                            // NOTE: Get the type of the underlying object instance.  If the
                            //       object instance is invalid here then so is the type.
                            //
                            if (objectType == null)
                                objectType = (@object != null) ? @object.GetType() : null;

                            //
                            // HACK: Make COM Interop objects work [slightly better].
                            //
                            if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                MarshalOps.IsSystemComObjectType(objectType))
                            {
                                objectType = MarshalOps.GetTypeFromComObject(
                                    interpreter, text, null, @object,
                                    interpreter.ObjectInterfaces, binder,
                                    cultureInfo, objectFlags, ref error);

                                if (objectType == null)
                                    return ReturnCode.Error;
                            }

                            //
                            // NOTE: Construct the typed instance object for the caller.
                            //
                            value = new TypedInstance((type != null) ?
                                type : objectType, objectFlags, text, text, @object);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            //
                            // NOTE: Next, check for a verbatim [qualified] type name.
                            //
                            ResultList errors = null;

                            if (GetType(
                                    interpreter, text, types, appDomain, valueFlags,
                                    cultureInfo, ref objectType, ref errors,
                                    ref exception) == ReturnCode.Ok)
                            {
                                //
                                // HACK: Now, if applicable, check if this type can be used
                                //       in a "safe" interpreter.
                                //
                                if (interpreter.IsSafe() &&
                                    !PolicyOps.IsTrustedType(interpreter, text,
                                        (type != null) ? type : objectType, ref error))
                                {
                                    return ReturnCode.Error;
                                }

                                //
                                // NOTE: Construct the typed instance object for the caller.
                                //
                                value = new TypedInstance((type != null) ?
                                    type : objectType, ObjectFlags.None, text, text, null);

                                return ReturnCode.Ok;
                            }
                            else if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoNested, true))
                            {
                                //
                                // NOTE: Finally, split apart their object or type reference
                                //       into pieces and attempt to traverse down the actual
                                //       object they want.
                                //
                                string[] parts = MarshalOps.SplitTypeName(text);

                                if (parts != null)
                                {
                                    //
                                    // NOTE: How many parts are there?  There must be at least
                                    //       one.
                                    //
                                    int length = parts.Length;

                                    if (length > 0)
                                    {
                                        //
                                        // NOTE: This is a little tricky.  We traverse the parts in
                                        //       the array and attempt to use each one to lookup the
                                        //       next one via Type.InvokeMember; however, the first
                                        //       time through the loop is special because we use it
                                        //       to setup the anchor point for our search and we must
                                        //       consider the possibility that the caller specified a
                                        //       fully qualified type name (which we would have just
                                        //       split into pieces above).  Therefore, if the first
                                        //       part is not an opaque object handle, this forces us
                                        //       to use longest match semantics when searching for a
                                        //       type name associated with the first "logical" part.
                                        //       In summary, we cannot assume that the first logical
                                        //       part is the same as the first physical part, due to
                                        //       fully qualified type names (please refer to the
                                        //       comments below for more details).
                                        //
                                        Result localError;

                                        for (int index = 0; index < length; index++)
                                        {
                                            //
                                            // NOTE: Grab the parts that we may need in the body.
                                            //
                                            string lastPart = (index > 0) ? parts[index - 1] : null;
                                            string part = parts[index];

                                            //
                                            // NOTE: Have we already tried processing the first part?
                                            //
                                            if (index > 0)
                                            {
                                                //
                                                // NOTE: At this point, the part must be valid; otherwise,
                                                //       we cannot lookup the remaining parts that were
                                                //       specified.
                                                //
                                                if (objectType != null)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try fetching the next part as a method with zero
                                                        //       arguments, a field, or a property of the current
                                                        //       part.  If this does not work (i.e. it throws an
                                                        //       exception), we are done.  This is allowed to
                                                        //       return null unless more parts remain to be looked
                                                        //       up.
                                                        //
                                                        @object = objectType.InvokeMember(part,
                                                            bindingFlags | MarshalOps.NestedObjectBindingFlags,
                                                            binder as Binder, @object, null, cultureInfo);

                                                        //
                                                        // NOTE: Now, get the type of the object we just fetched.
                                                        //       If the object instance is invalid here then so is
                                                        //       the type.
                                                        //
                                                        objectType = (@object != null) ? @object.GetType() : null;

                                                        //
                                                        // NOTE: Reset the object flags because nested objects
                                                        //       cannot have object flags (i.e. there may be no
                                                        //       wrapper).
                                                        //
                                                        objectFlags = ObjectFlags.None;

                                                        //
                                                        // HACK: Make COM Interop objects work [slightly better].
                                                        //
                                                        if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                                            MarshalOps.IsSystemComObjectType(objectType))
                                                        {
                                                            localError = null;

                                                            objectType = MarshalOps.GetTypeFromComObject(
                                                                interpreter, text, part, @object,
                                                                interpreter.ObjectInterfaces, binder,
                                                                cultureInfo, objectFlags, ref localError);

                                                            if (objectType == null)
                                                            {
                                                                errors.Insert(0, localError);

                                                                error = errors;

                                                                return ReturnCode.Error;
                                                            }
                                                        }

                                                        //
                                                        // HACK: Now, if applicable, check if this type can be used
                                                        //       in a "safe" interpreter.
                                                        //
                                                        localError = null;

                                                        if (interpreter.IsSafe() &&
                                                            !PolicyOps.IsTrustedType(interpreter, text, objectType,
                                                                ref localError))
                                                        {
                                                            errors.Insert(0, localError);

                                                            error = errors;

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        //
                                                        // NOTE: Failure, we are done.  Give the caller our error
                                                        //       information.
                                                        //
                                                        errors.Insert(0, e);

                                                        error = errors;

                                                        exception = e;

                                                        return ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: We cannot lookup the next part because the current
                                                    //       part is null.  This is considered a failure unless
                                                    //       the right flag is set.
                                                    //
                                                    if (FlagOps.HasFlags(
                                                            valueFlags, ValueFlags.StopOnNullObject, true))
                                                    {
                                                        value = new TypedInstance(
                                                            null, ObjectFlags.None, lastPart, text, null);

                                                        return ReturnCode.Continue;
                                                    }
                                                    else
                                                    {
                                                        errors.Insert(0, String.Format(
                                                            "cannot process object part {0}, " +
                                                            "previous part {1}was null",
                                                            FormatOps.WrapOrNull(part),
                                                            (lastPart != null) ? String.Format(
                                                            "part {0} ", FormatOps.WrapOrNull(
                                                            lastPart)) : String.Empty));

                                                        error = errors;

                                                        return ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: First, try to lookup an object reference in the supplied
                                                //       interpreter.  We know that we do not have to perform
                                                //       longest match semantics on this value when looking it up
                                                //       as an object handle because we cheat in the marshalling
                                                //       code (i.e. we do not permit the type delimiter in opaque
                                                //       object handle values).
                                                //
                                                if (GetObject(
                                                        interpreter, part, ref objectType,
                                                        ref objectFlags, ref @object) == ReturnCode.Ok)
                                                {
                                                    //
                                                    // HACK: Now, if applicable, check if this object can be used
                                                    //       in a "safe" interpreter.
                                                    //
                                                    localError = null;

                                                    if (interpreter.IsSafe() &&
                                                        !PolicyOps.IsTrustedObject(interpreter, text, objectFlags,
                                                            @object, ref localError))
                                                    {
                                                        errors.Insert(0, localError);

                                                        error = errors;

                                                        return ReturnCode.Error;
                                                    }

                                                    //
                                                    // NOTE: Now, get the type of the object we just fetched.
                                                    //       If the object instance is invalid here then so is
                                                    //       the type.
                                                    //
                                                    if (objectType == null)
                                                        objectType = (@object != null) ? @object.GetType() : null;

                                                    //
                                                    // HACK: Make COM Interop objects work [slightly better].
                                                    //
                                                    if (!FlagOps.HasFlags(valueFlags, ValueFlags.NoComObject, true) &&
                                                        MarshalOps.IsSystemComObjectType(objectType))
                                                    {
                                                        localError = null;

                                                        objectType = MarshalOps.GetTypeFromComObject(
                                                            interpreter, text, part, @object,
                                                            interpreter.ObjectInterfaces, binder,
                                                            cultureInfo, objectFlags, ref localError);

                                                        if (objectType == null)
                                                        {
                                                            errors.Insert(0, localError);

                                                            error = errors;

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: Next, try to lookup a type based on one or more of
                                                    //       the parts of the object reference.  We always try
                                                    //       for the longest possible match here.
                                                    //
                                                    bool found = false;

                                                    //
                                                    // NOTE: Longest match semantics.  Start the last index for
                                                    //       this type search at the last part index and work
                                                    //       our way backwards until we find a match.  If no
                                                    //       match is found at this point, the whole operation
                                                    //       is considered a failure and we are done.
                                                    //
                                                    for (int lastIndex = length - 1; lastIndex >= index; lastIndex--)
                                                    {
                                                        string typeName = MarshalOps.JoinTypeName(
                                                            parts, index, (lastIndex - index));

                                                        if (GetType(
                                                                interpreter, typeName, types, appDomain, valueFlags,
                                                                cultureInfo, ref objectType, ref errors,
                                                                ref exception) == ReturnCode.Ok)
                                                        {
                                                            //
                                                            // HACK: Now, if applicable, check if this type can be used
                                                            //       in a "safe" interpreter.
                                                            //
                                                            localError = null;

                                                            if (interpreter.IsSafe() &&
                                                                !PolicyOps.IsTrustedType(interpreter, text, objectType,
                                                                    ref localError))
                                                            {
                                                                errors.Insert(0, localError);

                                                                error = errors;

                                                                return ReturnCode.Error;
                                                            }

                                                            //
                                                            // NOTE: Reset the object flags because types cannot have
                                                            //       object flags.
                                                            //
                                                            objectFlags = ObjectFlags.None;

                                                            //
                                                            // NOTE: Advance the index to the index we are currently on.
                                                            //       This value will be incremented at the top of the
                                                            //       outer loop prior to the next part lookup, which is
                                                            //       the desired outcome.
                                                            //
                                                            index = lastIndex;

                                                            //
                                                            // NOTE: Indicate to the block below that we found a match in
                                                            //       this loop.
                                                            //
                                                            found = true;
                                                            break;
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did we find a type to use as the current part?
                                                    //
                                                    if (!found)
                                                    {
                                                        //
                                                        // NOTE: We failed to lookup any matching type based on any of
                                                        //       the parts (within the range).  This is considered a
                                                        //       failure unless the right flag is set.
                                                        //
                                                        if (FlagOps.HasFlags(
                                                                valueFlags, ValueFlags.StopOnNullType, true))
                                                        {
                                                            value = new TypedInstance(
                                                                null, ObjectFlags.None, part, text, null);

                                                            return ReturnCode.Continue;
                                                        }

                                                        //
                                                        // HACK: The error message here is somewhat of a "best guess"
                                                        //       because it does not really reflect all the type names
                                                        //       we tried; however, detailed errors occur after this
                                                        //       one in the list.
                                                        //
                                                        errors.Insert(0, String.Format(
                                                            "object or type {0} not found",
                                                            FormatOps.WrapOrNull(part)));

                                                        error = errors;

                                                        return ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }

                                        //
                                        // NOTE: If we get to this point, we know that we have succeeded;
                                        //       therefore, set the caller's object value to the final
                                        //       object reference we fetched above.
                                        //
                                        value = new TypedInstance((type != null) ?
                                            type : objectType, objectFlags, text, text, @object);

                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        errors.Insert(0, String.Format(
                                            "no object parts in {0} to process",
                                            FormatOps.WrapOrNull(text)));

                                        error = errors;
                                    }
                                }
                                else
                                {
                                    errors.Insert(0, String.Format(
                                        "could not parse object parts from {0}",
                                        FormatOps.WrapOrNull(text)));

                                    error = errors;
                                }
                            }
                            else
                            {
                                errors.Insert(0, String.Format(
                                    "object or type {0} not found",
                                    FormatOps.WrapOrNull(text)));

                                error = errors;
                            }
                        }
                    }
                    else
                    {
                        error = "invalid object name";
                    }
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetObject( /* For use by GetVariant ONLY. */
            Interpreter interpreter,
            string text,
            LookupFlags lookupFlags,
            ref object value
            )
        {
            Result error = null; /* NOT USED */

            return GetObject(
                interpreter, text, lookupFlags, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            LookupFlags lookupFlags,
            ref object value,
            ref Result error
            )
        {
            bool verbose = FlagOps.HasFlags(
                lookupFlags, LookupFlags.Verbose, true);

            if (interpreter == null)
            {
                if (verbose)
                    error = "invalid interpreter";

                return ReturnCode.Error;
            }

            if (text == null)
            {
                if (verbose)
                    error = "invalid object name";

                return ReturnCode.Error;
            }

            IObject @object = null;

            if (interpreter.GetObject(
                    text, lookupFlags, ref @object,
                    ref error) == ReturnCode.Ok)
            {
                if (@object != null)
                {
                    value = @object.Value;
                    return ReturnCode.Ok;
                }
                else if (verbose)
                {
                    error = String.Format(
                        "invalid wrapper for object {0}",
                        FormatOps.WrapOrNull(text));
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            ref object value
            )
        {
            Type type = null;
            ObjectFlags objectFlags = ObjectFlags.None;

            return GetObject(
                interpreter, text, ref type, ref objectFlags, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            ref Type type,
            ref ObjectFlags objectFlags,
            ref object value
            )
        {
            Result error = null;

            return GetObject(
                interpreter, text, LookupFlags.NoVerbose, ref type,
                ref objectFlags, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            LookupFlags lookupFlags,
            ref Type type,
            ref ObjectFlags objectFlags,
            ref object value,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                //
                // NOTE: *WARNING* Empty opaque object handle names are allowed,
                //       please do not change this to "!String.IsNullOrEmpty".
                //
                if (text != null)
                {
                    IObject @object = null;

                    if (interpreter.GetObject(
                            text, lookupFlags, ref @object,
                            ref error) == ReturnCode.Ok)
                    {
                        if (@object != null)
                        {
                            type = @object.Type;
                            objectFlags = @object.ObjectFlags;
                            value = @object.Value;

                            return ReturnCode.Ok;
                        }
                        else if (FlagOps.HasFlags(
                                lookupFlags, LookupFlags.Verbose, true))
                        {
                            error = String.Format(
                                "invalid wrapper for object {0}",
                                FormatOps.WrapOrNull(text));
                        }
                    }
                }
                else if (FlagOps.HasFlags(
                        lookupFlags, LookupFlags.Verbose, true))
                {
                    error = "invalid object name";
                }
            }
            else if (FlagOps.HasFlags(
                    lookupFlags, LookupFlags.Verbose, true))
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetValue(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref object value
            )
        {
            Result error = null;

            return GetValue(
                text, format, flags, kind, cultureInfo, ref value,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetValue(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref object value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetValue(
                text, format, flags, kind, cultureInfo, ref value,
                ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetValue(
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref object value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            byte byteValue = 0;

            if (FlagOps.HasFlags(flags, ValueFlags.Byte, true) &&
                (GetByte2(text, flags, cultureInfo,
                    ref byteValue) == ReturnCode.Ok))
            {
                value = byteValue;

                return ReturnCode.Ok;
            }
            else
            {
                short shortValue = 0;

                if (FlagOps.HasFlags(flags, ValueFlags.NarrowInteger, true) &&
                    (GetNarrowInteger2(text, flags, cultureInfo,
                        ref shortValue) == ReturnCode.Ok))
                {
                    value = shortValue;

                    return ReturnCode.Ok;
                }
                else
                {
                    char charValue = Characters.Null;

                    if (FlagOps.HasFlags(flags, ValueFlags.Character, true) &&
                        (GetCharacter2(text, flags, cultureInfo,
                            ref charValue) == ReturnCode.Ok))
                    {
                        value = charValue;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        int intValue = 0;

                        if (FlagOps.HasFlags(flags, ValueFlags.Integer, true) &&
                            (GetInteger2(text, flags, cultureInfo,
                                ref intValue) == ReturnCode.Ok))
                        {
                            value = intValue;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            long longValue = 0;

                            if (FlagOps.HasFlags(flags, ValueFlags.WideInteger, true) &&
                                (GetWideInteger2(text, flags, cultureInfo,
                                    ref longValue) == ReturnCode.Ok))
                            {
                                value = longValue;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                decimal decimalValue = Decimal.Zero;

                                if (FlagOps.HasFlags(flags, ValueFlags.Decimal, true) &&
                                    (GetDecimal(text, cultureInfo,
                                        ref decimalValue) == ReturnCode.Ok))
                                {
                                    value = decimalValue;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    float floatValue = 0.0f;

                                    if (FlagOps.HasFlags(flags, ValueFlags.Single, true) &&
                                        (GetSingle(text, cultureInfo,
                                            ref floatValue) == ReturnCode.Ok))
                                    {
                                        value = floatValue;

                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        double doubleValue = 0.0;

                                        if (FlagOps.HasFlags(flags, ValueFlags.Double, true) &&
                                            (GetDouble(text, cultureInfo,
                                                ref doubleValue) == ReturnCode.Ok))
                                        {
                                            value = doubleValue;

                                            return ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: *SPECIAL CASE*: This converts everything that looks numeric in addition
                                            //       to the special boolean strings (such as "true", "false", "yes", "no",
                                            //       etc).  Also, since the .NET Framework will never perform a widening
                                            //       conversion from bool, this must be last among the pure numeric conversion
                                            //       attempts.
                                            //
                                            bool boolValue = false;

                                            if (FlagOps.HasFlags(flags, ValueFlags.Boolean, true) &&
                                                (GetBoolean2(text, flags, cultureInfo, ref boolValue) == ReturnCode.Ok))
                                            {
                                                value = boolValue;

                                                return ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                DateTime dateTime = DateTime.MinValue;

                                                if (FlagOps.HasFlags(flags, ValueFlags.DateTime, true) &&
                                                    (GetDateTime2(text, format, flags, kind, cultureInfo,
                                                        ref dateTime) == ReturnCode.Ok))
                                                {
                                                    value = dateTime;

                                                    return ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    TimeSpan timeSpan = TimeSpan.Zero;

                                                    if (FlagOps.HasFlags(flags, ValueFlags.TimeSpan, true) &&
                                                        (GetTimeSpan2(text, flags, cultureInfo,
                                                            ref timeSpan) == ReturnCode.Ok))
                                                    {
                                                        value = timeSpan;

                                                        return ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        Guid guid = Guid.Empty;

                                                        if (FlagOps.HasFlags(flags, ValueFlags.Guid, true) &&
                                                            (GetGuid(text, cultureInfo, ref guid) == ReturnCode.Ok))
                                                        {
                                                            value = guid;

                                                            return ReturnCode.Ok;
                                                        }
                                                        else
                                                        {
                                                            error = String.Format(
                                                                "expected value but got {0}",
                                                                FormatOps.WrapOrNull(text));

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNumber(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number value
            )
        {
            Result error = null;

            return GetNumber(text, flags, cultureInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNumber(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number value,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNumber(
                text, flags, cultureInfo, ref value, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNumber(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number value,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            if (FlagOps.HasFlags(flags, ValueFlags.IntegerOrWideInteger, true) &&
                (GetIntegerOrWideInteger(text, flags, cultureInfo,
                    ref value) == ReturnCode.Ok))
            {
                return ReturnCode.Ok;
            }
            else
            {
                uint uintValue = 0;

                if (FlagOps.HasFlags(flags, ValueFlags.Integer, true) &&
                    (GetUnsignedInteger2(text, flags, cultureInfo,
                        ref uintValue) == ReturnCode.Ok))
                {
                    value = new Number(ConversionOps.ToInt(uintValue));

                    return ReturnCode.Ok;
                }
                else
                {
                    ulong ulongValue = 0;

                    if (FlagOps.HasFlags(flags, ValueFlags.WideInteger, true) &&
                        (GetUnsignedWideInteger2(text, flags, cultureInfo,
                            ref ulongValue) == ReturnCode.Ok))
                    {
                        value = new Number(ConversionOps.ToLong(ulongValue));

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        decimal decimalValue = Decimal.Zero;

                        if (FlagOps.HasFlags(flags, ValueFlags.Decimal, true) &&
                            (GetDecimal(text, cultureInfo,
                                ref decimalValue) == ReturnCode.Ok))
                        {
                            value = new Number(decimalValue);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            double doubleValue = 0.0;

                            if (FlagOps.HasFlags(flags, ValueFlags.Double, true) &&
                                (GetDouble(text, cultureInfo,
                                    ref doubleValue) == ReturnCode.Ok))
                            {
                                value = new Number(doubleValue);

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                //
                                // NOTE: *SPECIAL CASE*: This converts everything that looks
                                //       numeric in addition to the special boolean strings
                                //       (such as "true", "false", "yes", "no", etc).  Also,
                                //       since the .NET Framework will never perform a widening
                                //       conversion from bool, this must be last among the pure
                                //       numeric conversion attempts.
                                //
                                bool boolValue = false;

                                if (FlagOps.HasFlags(flags, ValueFlags.Boolean, true) &&
                                    (GetBoolean2(text, flags, cultureInfo, ref boolValue) == ReturnCode.Ok))
                                {
                                    value = new Number(boolValue);

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "expected number but got {0}",
                                        FormatOps.WrapOrNull(text));

                                    return ReturnCode.Error;
                                }
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetNumber2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number value,
            ref int stopIndex
            )
        {
            Result error = null;

            return GetNumber2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNumber2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetNumber2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNumber2(
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Number value,
            ref int stopIndex,
            ref Result error,
            ref Exception exception /* NOT USED */
            )
        {
            int intValue = 0;

            if (FlagOps.HasFlags(flags, ValueFlags.Integer, true) &&
                (GetInteger2(text, flags, cultureInfo,
                    ref intValue) == ReturnCode.Ok))
            {
                value = new Number(intValue);

                stopIndex = Index.Invalid;

                return ReturnCode.Ok;
            }
            else
            {
                long longValue = 0;

                if (FlagOps.HasFlags(flags, ValueFlags.WideInteger, true) &&
                    (GetWideInteger2(text, flags, cultureInfo,
                        ref longValue) == ReturnCode.Ok))
                {
                    value = new Number(longValue);

                    stopIndex = Index.Invalid;

                    return ReturnCode.Ok;
                }
                else
                {
                    decimal decimalValue = Decimal.Zero;

                    if (FlagOps.HasFlags(flags, ValueFlags.Decimal, true) &&
                        (GetDecimal2(text, cultureInfo, ref decimalValue,
                            ref stopIndex) == ReturnCode.Ok))
                    {
                        value = new Number(decimalValue);

                        stopIndex = Index.Invalid;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        double doubleValue = 0.0;

                        if (FlagOps.HasFlags(flags, ValueFlags.Double, true) &&
                            (GetDouble2(text, cultureInfo, ref doubleValue,
                                ref stopIndex) == ReturnCode.Ok))
                        {
                            value = new Number(doubleValue);

                            stopIndex = Index.Invalid;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            //
                            // NOTE: *SPECIAL CASE*: This converts everything that looks
                            //       numeric in addition to the special boolean strings
                            //       (such as "true", "false", "yes", "no", etc).  Also,
                            //       since the .NET Framework will never perform a widening
                            //       conversion from bool, this must be last among the pure
                            //       numeric conversion attempts.
                            //
                            bool boolValue = false;

                            if (FlagOps.HasFlags(flags, ValueFlags.Boolean, true) &&
                                (GetBoolean2(text, flags, cultureInfo,
                                    ref boolValue) == ReturnCode.Ok))
                            {
                                value = new Number(boolValue);

                                stopIndex = Index.Invalid;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = String.Format(
                                    "expected number but got {0}",
                                    FormatOps.WrapOrNull(text));

                                return ReturnCode.Error;
                            }
                        }
                    }
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeGetDateTimeParameters(
            Interpreter interpreter,
            out string format,
            out DateTimeKind kind
            )
        {
            format = ObjectOps.GetDefaultDateTimeFormat();
            kind = ObjectOps.GetDefaultDateTimeKind();

            if (interpreter != null)
            {
                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    try
                    {
                        format = interpreter.DateTimeFormat;
                        kind = interpreter.DateTimeKind;
                    }
                    catch (Exception e)
                    {
                        DebugOps.Complain(
                            interpreter, ReturnCode.Error, e);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetVariant(
            Interpreter interpreter,
            IGetValue getValue,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Variant value,
            ref Result error
            )
        {
            if (getValue == null)
            {
                error = "expected variant value but got null";
                return ReturnCode.Error;
            }

            object innerValue = getValue.Value;

            if (Number.IsSupported(innerValue))
            {
                value = new Variant(innerValue);

                return ReturnCode.Ok;
            }
            else if (innerValue is DateTime)
            {
                value = new Variant((DateTime)innerValue);

                return ReturnCode.Ok;
            }
            else if (innerValue is TimeSpan)
            {
                value = new Variant((TimeSpan)innerValue);

                return ReturnCode.Ok;
            }
            else
            {
                //
                // BUGFIX: Only use the StringList internal value if the
                //         element count is NOT equal to one; Otherwise we
                //         would never attempt to convert a valid number
                //         that just so happens to be "contained" within a
                //         list (e.g. "65756") to the actual numeric type.
                //         We are [ab]using the knowledge that *NO* valid
                //         number of any kind may contain a space (or be an
                //         empty string).
                //
                StringList list = innerValue as StringList;

                if ((list != null) && (list.Count != 1))
                {
                    value = new Variant(list);

                    return ReturnCode.Ok;
                }
                else
                {
                    //
                    // NOTE: Fallback to normal string-based processing.
                    //
                    ResultList errors = null;
                    string stringValue = getValue.String;
                    object objectValue = null;
                    Result localError = null; /* REUSED */

                    if (GetNumeric(
                            stringValue, flags, cultureInfo, ref objectValue,
                            ref localError) == ReturnCode.Ok)
                    {
                        value = new Variant(objectValue);

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: Record the error message provided by the
                        //       GetNumeric method, if any.
                        //
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        //
                        // NOTE: Mask off the types that should have already
                        //       been handled by the GetNumeric call (above).
                        //
                        flags &= ~ValueFlags.NumericMask;

                        //
                        // NOTE: Reset the error message so we can determine
                        //       if *this* method actually set it.
                        //
                        localError = null;

                        //
                        // NOTE: Attempt to obtain DateTime related settings
                        //       from the interpreter.
                        //
                        string format;
                        DateTimeKind kind;

                        MaybeGetDateTimeParameters(
                            interpreter, out format, out kind);

                        if (GetVariant(
                                interpreter, stringValue, format,
                                flags, kind, cultureInfo, ref value,
                                ref localError) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                        else if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        error = errors;
                        return ReturnCode.Error;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For ConversionOps.Dynamic.ChangeType.ToVariant USE ONLY.
        //
        internal static ReturnCode GetVariant(
            Interpreter interpreter,
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Variant value,
            ref Result error
            )
        {
            string format;
            DateTimeKind kind;

            MaybeGetDateTimeParameters(
                interpreter, out format, out kind);

            return GetVariant(
                interpreter, text, format, flags, kind, cultureInfo,
                ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetVariant( /* FOR [string] USE ONLY. */
            Interpreter interpreter,
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref Variant value
            )
        {
            Result error = null;

            return GetVariant(
                interpreter, text, format, flags, kind, cultureInfo,
                ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetVariant(
            Interpreter interpreter,
            string text,
            string format,
            ValueFlags flags,
            DateTimeKind kind,
            CultureInfo cultureInfo,
            ref Variant value,
            ref Result error
            )
        {
            object @object = null;

            if (FlagOps.HasFlags(flags, ValueFlags.Object, true) &&
                (GetObject(
                    interpreter, text, LookupFlags.NoVerbose,
                    ref @object) == ReturnCode.Ok) &&
                Number.IsSupported(@object))
            {
                try
                {
                    value = new Variant(@object); /* throw */
                }
                catch (Exception e)
                {
                    //
                    // HACK: It should not be possible to get into this
                    //       catch block.
                    //
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else
            {
                Number number = null;

                if (FlagOps.HasFlags(flags, ValueFlags.Number, true) &&
                    (GetNumber(
                        text, flags, cultureInfo,
                        ref number) == ReturnCode.Ok))
                {
                    value = new Variant(number);
                }
                else
                {
                    DateTime dateTime = DateTime.MinValue;

                    if (FlagOps.HasFlags(flags, ValueFlags.DateTime, true) &&
                        (GetDateTime2(
                            text, format, flags, kind, cultureInfo,
                            ref dateTime) == ReturnCode.Ok))
                    {
                        value = new Variant(dateTime);
                    }
                    else
                    {
                        TimeSpan timeSpan = TimeSpan.Zero;

                        if (FlagOps.HasFlags(flags, ValueFlags.TimeSpan, true) &&
                            (GetTimeSpan2(
                                text, flags, cultureInfo,
                                ref timeSpan) == ReturnCode.Ok))
                        {
                            value = new Variant(timeSpan);
                        }
                        else
                        {
                            //
                            // NOTE: Cannot parse as list, use string.
                            //
                            value = new Variant(text);
                        }
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ReturnCode GetVariant2( /* NOT USED */
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Variant value,
            ref int stopIndex,
            ref Result error
            )
        {
            Exception exception = null;

            return GetVariant2(
                text, flags, cultureInfo, ref value, ref stopIndex, ref error,
                ref exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetVariant2( /* NOT USED */
            string text,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref Variant value,
            ref int stopIndex,
            ref Result error,       /* NOT USED */
            ref Exception exception /* NOT USED */
            )
        {
            Number number = null;

            if (GetNumber2(text, flags, cultureInfo,
                    ref number, ref stopIndex) == ReturnCode.Ok)
            {
                value = new Variant(number);

                stopIndex = Index.Invalid;
            }
            else
            {
                value = new Variant(text);

                stopIndex = Index.Invalid;
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode FixupStringVariants(
            IIdentifierBase identifierBase,
            Variant variant1,
            Variant variant2,
            ref Result error
            )
        {
            //
            // NOTE: Perform type-promotion/coercion on one or both operands based on
            //       the allowed types for this operator or function...
            //
            if ((variant1 != null) && (variant2 != null))
            {
                if (variant1.ConvertTo(typeof(string)) &&
                    variant2.ConvertTo(typeof(string)))
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "failed to convert operand to type {0}",
                        MarshalOps.GetErrorTypeName(typeof(string)));

                    return ReturnCode.Error;
                }
            }
            else
            {
                error = String.Format(
                    "one or more operands for operator {0} are invalid",
                    FormatOps.WrapOrNull(identifierBase.Name));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode GetOperandsFromArguments(
            Interpreter interpreter,
            IOperator @operator,
            ArgumentList arguments,
            ValueFlags flags,
            CultureInfo cultureInfo,
            ref string name,
            ref Variant operand1,
            ref Variant operand2,
            ref Result error
            )
        {
            ReturnCode code;

            if (@operator != null)
            {
                if (arguments != null)
                {
                    string localName = (arguments.Count > 0) ?
                        (string)arguments[0] : @operator.Name;

                    //
                    // NOTE: Must be "operator arg ?arg?" unless the number of operands if
                    //       less than zero (i.e. those values are reserved for special
                    //       cases).
                    //
                    if ((@operator.Operands < 0) ||
                        (arguments.Count == (@operator.Operands + 1)))
                    {
                        code = ReturnCode.Ok;

                        Variant localOperand1 = null;

                        if ((code == ReturnCode.Ok) && (arguments.Count >= 2))
                        {
                            if (flags == ValueFlags.String)
                            {
                                localOperand1 = new Variant(arguments[1]);
                            }
                            else
                            {
                                code = GetVariant(
                                    interpreter, (IGetValue)arguments[1],
                                    flags, cultureInfo, ref localOperand1,
                                    ref error);

                                if (code != ReturnCode.Ok)
                                    error = String.Format("operand1: {0}", error);
                            }
                        }

                        Variant localOperand2 = null;

                        if ((code == ReturnCode.Ok) && (arguments.Count >= 3))
                        {
                            if (flags == ValueFlags.String)
                            {
                                localOperand2 = new Variant(arguments[2]);
                            }
                            else
                            {
                                code = GetVariant(
                                    interpreter, (IGetValue)arguments[2],
                                    flags, cultureInfo, ref localOperand2,
                                    ref error);

                                if (code != ReturnCode.Ok)
                                    error = String.Format("operand2: {0}", error);
                            }
                        }

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Commit changes to the variables provided by
                            //       the caller.
                            //
                            name = localName;

                            operand1 = localOperand1;
                            operand2 = localOperand2;
                        }
                    }
                    else
                    {
                        if (@operator.Operands == 2)
                        {
                            if (ExpressionParser.IsOperatorNameOnly(localName))
                            {
                                error = String.Format(
                                    "wrong # args: should be \"operand1 {0} operand2\"",
                                    FormatOps.OperatorName(localName));
                            }
                            else
                            {
                                error = String.Format(
                                    "wrong # args: should be \"{0} operand1 operand2\"",
                                    FormatOps.OperatorName(localName));
                            }
                        }
                        else if (@operator.Operands == 1)
                        {
                            error = String.Format(
                                "wrong # args: should be \"{0} operand\"",
                                FormatOps.OperatorName(localName));
                        }
                        else
                        {
                            error = String.Format(
                                "unsupported number of operands for operator {0}",
                                FormatOps.OperatorName(localName, @operator.Lexeme));
                        }

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "invalid operator";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static ReturnCode FixupVariants(
            IIdentifierBase identifierBase,
            Variant variant1,
            Variant variant2,
            Type variant1Type,
            Type variant2Type,
            bool variant1NoConvertTo,
            bool variant2NoConvertTo,
            ref Result error
            )
        {
            ReturnCode code;

            //
            // NOTE: Perform type-promotion/coercion on one or both
            //       variants based on the allowed types for this
            //       operator or function...
            //
            if ((variant1 != null) && (variant2 != null))
            {
                code = ReturnCode.Ok;

                if (code == ReturnCode.Ok)
                {
                    if (variant1Type != null)
                    {
                        if (!variant1.ConvertTo(variant1Type))
                        {
                            error = String.Format(
                                "failed to convert variant1 to type {0}",
                                MarshalOps.GetErrorTypeName(variant1Type));

                            code = ReturnCode.Error;
                        }
                    }
                }

                if (code == ReturnCode.Ok)
                {
                    if (variant2Type != null)
                    {
                        if (!variant2.ConvertTo(variant2Type))
                        {
                            error = String.Format(
                                "failed to convert variant2 to type {0}",
                                MarshalOps.GetErrorTypeName(variant2Type));

                            code = ReturnCode.Error;
                        }
                    }
                }

                if (code == ReturnCode.Ok)
                {
                    if (variant1.IsNumber() &&
                        variant2.IsNumber())
                    {
                        bool variant1SkipConvertTo = variant1NoConvertTo ||
                            (variant1Type != null);

                        bool variant2SkipConvertTo = variant2NoConvertTo ||
                            (variant2Type != null);

                        if (variant1.IsIntegral() &&
                            variant2.IsIntegral())
                        {
                            //
                            // TODO: Add more of the supported Variant sub-types
                            //       here (e.g. unsigned wide integer, etc).
                            //
                            if (variant1.IsWideInteger() ||
                                variant2.IsWideInteger())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsWideInteger() ||
                                     variant1.ConvertTo(typeof(long))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsWideInteger() ||
                                     variant2.ConvertTo(typeof(long))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(long)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else if (variant1.IsInteger() ||
                                variant2.IsInteger())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsInteger() ||
                                     variant1.ConvertTo(typeof(int))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsInteger() ||
                                     variant2.ConvertTo(typeof(int))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(int)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else if (variant1.IsNarrowInteger() ||
                                variant2.IsNarrowInteger())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsNarrowInteger() ||
                                     variant1.ConvertTo(typeof(short))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsNarrowInteger() ||
                                     variant2.ConvertTo(typeof(short))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(short)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else if (variant1.IsByte() || variant2.IsByte())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsByte() ||
                                     variant1.ConvertTo(typeof(byte))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsByte() ||
                                     variant2.ConvertTo(typeof(byte))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(byte)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else if (variant1.IsBoolean() || variant2.IsBoolean())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsBoolean() ||
                                     variant1.ConvertTo(typeof(bool))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsBoolean() ||
                                     variant2.ConvertTo(typeof(bool))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(bool)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "unsupported integral variant type";
                                code = ReturnCode.Error;
                            }
                        }
                        else if (variant1.IsFloatingPoint() ||
                            variant2.IsFloatingPoint())
                        {
                            if (variant1.IsDouble() ||
                                variant2.IsDouble())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsDouble() ||
                                     variant1.ConvertTo(typeof(double))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsDouble() ||
                                     variant2.ConvertTo(typeof(double))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(double)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else if (variant1.IsSingle() ||
                                variant2.IsSingle())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsSingle() ||
                                     variant1.ConvertTo(typeof(float))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsSingle() ||
                                     variant2.ConvertTo(typeof(float))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(float)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "unsupported floating-point variant type";
                                code = ReturnCode.Error;
                            }
                        }
                        else if (variant1.IsFixedPoint() ||
                            variant2.IsFixedPoint())
                        {
                            if (variant1.IsDecimal() ||
                                variant2.IsDecimal())
                            {
                                if ((variant1SkipConvertTo ||
                                     variant1.IsDecimal() ||
                                     variant1.ConvertTo(typeof(decimal))) &&
                                    (variant2SkipConvertTo ||
                                     variant2.IsDecimal() ||
                                     variant2.ConvertTo(typeof(decimal))))
                                {
                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert variant to type {0}",
                                        MarshalOps.GetErrorTypeName(typeof(decimal)));

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "unsupported fixed-point variant type";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "unsupported variant type";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if ((variant1Type == null) ||
                            (variant2Type == null))
                        {
                            error = String.Format(
                                "can't use non-numeric string as operand of {0}",
                                FormatOps.WrapOrNull(identifierBase.Name));

                            code = ReturnCode.Error;
                        }
                    }
                }
            }
            else
            {
                error = String.Format(
                    "one or more operands for {0} are invalid",
                    FormatOps.WrapOrNull(identifierBase.Name));

                code = ReturnCode.Error;
            }

            return code;
        }
    }
}
