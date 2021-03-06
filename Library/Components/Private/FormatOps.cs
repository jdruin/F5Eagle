/*
 * FormatOps.cs --
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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

#if !MONO
using System.Security.AccessControl;
#endif

using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;

#if NATIVE && TCL
using Eagle._Components.Private.Tcl;
#endif

using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;

namespace Eagle._Components.Private
{
    [ObjectId("62feeba0-3df8-4395-b850-c4d307d021a7")]
    internal static class FormatOps
    {
        #region Private Constants
        private static readonly string TracePriorityFormat = "X2";

        private const string ConfigurationSeparator = " - ";

        private static readonly string UnknownTypeName = "unknown";

        internal static readonly string DisplayNone = "<none>";
        internal static readonly string DisplayNull = "<null>";
        private static readonly string DisplayNullString = "<nullString>";
        internal static readonly string DisplayEmpty = "<empty>";
        private static readonly string DisplaySpace = "<space>";
        internal static readonly string DisplayDisposed = "<disposed>";
        internal static readonly string DisplayBusy = "<busy>";
        private static readonly string DisplayError = "<error:{0}>";
        internal static readonly string DisplayUnknown = "<unknown>";
        internal static readonly string DisplayPresent = "<present>";
        internal static readonly string DisplayFormat = "<{0}>";
        private static readonly string DisplayUnavailable = "<unavailable>";

        private static readonly string QuotationMark =
            Characters.QuotationMark.ToString();

        private const int ResultEllipsisLimit = 78;

#if HISTORY
        private const int HistoryEllipsisLimit = 78;
#endif

        private const int DefaultEllipsisLimit = 60;
        private const int WrapEllipsisLimit = 200;

        private const string ResultEllipsis = " ...";

#if HISTORY
        private const string HistoryEllipsis = " ...";
#endif

        private const string DefaultEllipsis = "...";

        private const string ByteOutputFormat = "x2";
        private const string ULongOutputFormat = "x16";

        private const string HexadecimalPrefix = "0x";
        private const string HexadecimalFormat = "{0}{1:X}";

        // private const string HexavigesimalAlphabet = "0123456789ABCDEFGHIJKLMNOP";
        private const string HexavigesimalAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Regex releaseShortNameRegEx = new Regex(
            "(?:Pre-|Post-)?(?:Alpha|Beta|RC|Final|Release) \\d+(?:\\.\\d+)?",
            RegexOptions.IgnoreCase);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Clock Constants
        private const string GmtTimeZoneName = "GMT";
        private const string UtcTimeZoneName = "UTC";

        private const int Roddenberry = 1946; // Another epoch (Hi, Jeff!)

        private const string DefaultFullDateTimeFormat = "dddd, dd MMMM yyyy HH:mm:ss";

        private const string DayOfYearFormat = "000"; // COMPAT: Tcl
        private const string WeekOfYearFormat = "00"; // COMPAT: Tcl
        private const string Iso8601YearFormat = "00"; // COMPAT: Tcl

        private const string Iso8601FullDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";
        private const string Iso8601TraceDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";
        private const string Iso8601UpdateDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";

        private const string Iso8601DateTimeOutputFormat = "yyyy.MM.ddTHH:mm:ss.fff";
        private const string PackageDateTimeOutputFormat = "yyyy.MM.dd";

        private const string StardateInputFormat = "%Q"; // COMPAT: Tcl
        private const string StardateOutputFormat = "Stardate {0:D2}{1:D3}{2}{3:D1}";

#if UNIX
        internal static readonly string StringInputFormat = "%s";
#endif

        private static readonly StringPairList tclClockFormats = new StringPairList(
            new StringPair(GmtTimeZoneName, "\\G\\M\\T"),
            new StringPair("%a", "ddd"), new StringPair("%A", "dddd"),
            new StringPair("%b", "MMM"), new StringPair("%B", "MMMM"),
            new StringPair("%c", GetFullDateTimeFormat()), new StringPair("%C", null),
            new StringPair("%d", "dd"), new StringPair("%D", "MM/dd/yy"),
            new StringPair("%e", "%d"), new StringPair("%g", null),
            new StringPair("%G", null), new StringPair("%h", "MMM"),
            new StringPair("%H", "HH"), new StringPair("%i", Iso8601DateTimeOutputFormat),
            new StringPair("%I", "hh"), new StringPair("%j", null),
            new StringPair("%k", "%H"), new StringPair("%l", "%h"),
            new StringPair("%m", "MM"), new StringPair("%M", "mm"),
            new StringPair("%n", Characters.NewLine.ToString()), new StringPair("%p", "tt"),
            new StringPair("%Q", null), new StringPair("%r", "hh:mm:ss tt"),
            new StringPair("%R", "HH:mm"), new StringPair("%s", null),
            new StringPair("%S", "ss"), new StringPair("%t", Characters.HorizontalTab.ToString()),
            new StringPair("%T", "HH:mm:ss"), new StringPair("%u", null),
            new StringPair("%U", null), new StringPair("%V", null),
            new StringPair("%w", null), new StringPair("%W", null),
            new StringPair("%x", "M/d/yyyy"), new StringPair("%X", "h:mm:ss tt"),
            new StringPair("%y", "yy"), new StringPair("%Y", "yyyy"),
            new StringPair("%Z", null), new StringPair("%%", "\\%"));

        private static readonly DelegateDictionary tclClockDelegates = new DelegateDictionary(
            new ObjectPair("%C", new ClockTransformCallback(TclClockDelegates.GetCentury)),
            new ObjectPair("%g", new ClockTransformCallback(TclClockDelegates.GetTwoDigitYearIso8601)),
            new ObjectPair("%G", new ClockTransformCallback(TclClockDelegates.GetFourDigitYearIso8601)),
            new ObjectPair("%j", new ClockTransformCallback(TclClockDelegates.GetDayOfYear)),
            new ObjectPair("%Q", new ClockTransformCallback(TclClockDelegates.GetStardate)),
            new ObjectPair("%s", new ClockTransformCallback(TclClockDelegates.GetSecondsSinceEpoch)),
            new ObjectPair("%u", new ClockTransformCallback(TclClockDelegates.GetWeekdayNumberOneToSeven)),
            new ObjectPair("%U", new ClockTransformCallback(TclClockDelegates.GetWeekOfYearSundayIsFirstDay)),
            new ObjectPair("%V", new ClockTransformCallback(TclClockDelegates.GetWeekOfYearIso8601)),
            new ObjectPair("%w", new ClockTransformCallback(TclClockDelegates.GetWeekdayNumberZeroToSix)),
            new ObjectPair("%W", new ClockTransformCallback(TclClockDelegates.GetWeekOfYearMondayIsFirstDay)),
            new ObjectPair("%Z", new ClockTransformCallback(TclClockDelegates.GetTimeZoneName)));
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the next unique event "serial number" within this
        //       application domain.  It is only ever accessed by this class
        //       (in one place) using an interlocked increment operation in
        //       order to assist in constructing event names that are unique
        //       within the entire application domain (i.e. there are other
        //       aspects of the final event name that ensure it is unique on
        //       this system).
        //
        private static long nextEventId;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the list of method names to skip when figuring out
        //       the "correct" method name to use for trace output.
        //
        private static StringList skipNames = new StringList(
            "DebugTrace", "DebugWrite", "DebugWriteTo", "DebugWriteOrTrace");
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tcl Clock Delegates
        [ObjectId("706d94c0-f87f-4562-abbd-a1917ed99e8c")]
        private static class TclClockDelegates
        {
            public static string GetCentury(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.Year / 100) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetTwoDigitYearIso8601(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, (TimeOps.ThisThursday(
                        clockData.DateTime).Year % 100).ToString(Iso8601YearFormat)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetFourDigitYearIso8601(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, TimeOps.ThisThursday(clockData.DateTime).Year) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetDayOfYear(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, clockData.DateTime.DayOfYear.ToString(DayOfYearFormat)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetStardate(
                IClockData clockData
                )
            {
                return (clockData != null) ? WrapOrNull(true, Stardate(clockData.DateTime)) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetSecondsSinceEpoch(
                IClockData clockData
                )
            {
                long seconds = 0;

                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime.ToUniversalTime();
                    DateTime epoch = clockData.Epoch;

                    if (TimeOps.DateTimeToSeconds(ref seconds, dateTime, epoch))
                        return WrapOrNull(true, seconds);
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekdayNumberOneToSeven(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime;
                    DayOfWeek dayOfWeek = dateTime.DayOfWeek;

                    //
                    // HACK: Make Sunday have the value of seven (Saturday + 1).
                    //
                    if (dayOfWeek == DayOfWeek.Sunday)
                        dayOfWeek = DayOfWeek.Saturday + 1;

                    return WrapOrNull(true, (int)dayOfWeek);
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekOfYearSundayIsFirstDay(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime;

                    return WrapOrNull(true, ((dateTime.DayOfYear + 7 -
                        (int)dateTime.DayOfWeek) / 7).ToString(WeekOfYearFormat));
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekOfYearIso8601(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    CultureInfo cultureInfo = clockData.CultureInfo;

                    if (cultureInfo != null)
                    {
                        Calendar calendar = cultureInfo.Calendar;

                        if (calendar != null)
                        {
                            DateTime dateTime = clockData.DateTime;

                            return WrapOrNull(true, calendar.GetWeekOfYear(
                                dateTime, CalendarWeekRule.FirstFourDayWeek,
                                DayOfWeek.Monday).ToString(WeekOfYearFormat));
                        }
                    }
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekdayNumberZeroToSix(
                IClockData clockData
                )
            {
                return (clockData != null) ?
                    WrapOrNull(true, (int)clockData.DateTime.DayOfWeek) : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetWeekOfYearMondayIsFirstDay(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    DateTime dateTime = clockData.DateTime;

                    return WrapOrNull(true, ((dateTime.DayOfYear + 7 -
                        ((dateTime.DayOfWeek != DayOfWeek.Sunday) ?
                            (int)dateTime.DayOfWeek : 6)) / 7).ToString(WeekOfYearFormat));
                }

                return null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            public static string GetTimeZoneName(
                IClockData clockData
                )
            {
                if (clockData != null)
                {
                    TimeZone timeZone = clockData.TimeZone;
                    DateTime dateTime = clockData.DateTime;

                    if (timeZone != null)
                    {
                        return WrapOrNull(true, dateTime.IsDaylightSavingTime() ?
                            timeZone.DaylightName : timeZone.StandardName);
                    }
                    else if (dateTime.Kind == DateTimeKind.Utc)
                    {
                        return UtcTimeZoneName;
                    }
                }

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetFullDateTimeFormat()
        {
            DateTimeFormatInfo dateTimeFormatInfo =
                Value.GetDateTimeFormatProvider() as DateTimeFormatInfo;

            if (dateTimeFormatInfo != null)
            {
                string format = dateTimeFormatInfo.FullDateTimePattern;

                if (format != null)
                    return format;
            }

            return DefaultFullDateTimeFormat;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !MONO
        #region Dead Code
#if DEAD_CODE
        public static StringList FlagsEnum(
            Enum enumValue,
            bool noCase,
            bool skipNameless,
            bool skipBadName,
            bool skipBadValue,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            if (enumValue == null)
            {
                error = "invalid value";
                return null;
            }

            Type enumType = enumValue.GetType();

            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    TypeName(enumType));

                return null;
            }

            string[] names = Enum.GetNames(enumType);

            if (names == null)
            {
                error = "invalid enumeration names";
                return null;
            }

            ulong currentUlongValue;

            try
            {
                //
                // NOTE: Get the underlying unsigned long integer
                //       value for the overall enumerated value.
                //       This may throw an exception.
                //
                currentUlongValue = EnumOps.ToUIntOrULong(
                    enumValue); /* throw */
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }

            StringList list = new StringList();

            //
            // NOTE: If the enumerated value is zero, just
            //       return the (empty) result list now.
            //
            if (currentUlongValue == 0)
                return list;

            ulong previousUlongValue = 0;

            foreach (string name in names)
            {
                if (String.IsNullOrEmpty(name))
                {
                    //
                    // TODO: This block should never be hit?
                    //
                    if (!skipNameless)
                    {
                        error = "invalid enumeration name";
                        return null;
                    }
                    else
                    {
                        //
                        // NOTE: No point in calling TryParse
                        //       on something we *know* is not
                        //       valid.
                        //
                        continue;
                    }
                }

                object localEnumValue;
                Result localError = null;

                localEnumValue = EnumOps.TryParseEnum(
                    enumType, name, false, noCase, ref localError);

                if (localEnumValue == null)
                {
                    if (!skipBadName)
                    {
                        error = localError;
                        return null;
                    }
                    else
                    {
                        continue;
                    }
                }

                try
                {
                    //
                    // NOTE: Get the underlying unsigned long integer
                    //       value for the current enumerated value.
                    //       This may throw an exception.
                    //
                    ulong localUlongValue = EnumOps.ToUIntOrULong(
                        (Enum)localEnumValue); /* throw */

                    //
                    // NOTE: If the value for the current enumerated
                    //       value is zero, skip it.  The associated
                    //       names will never be added to the result
                    //       unless the "keepZeros" flag is set.
                    //
                    if (localUlongValue == 0)
                    {
                        if (keepZeros)
                            list.Add(name);
                        else
                            continue;
                    }

                    //
                    // NOTE: Check if the overall enumerated value
                    //       has all the bits set from the current
                    //       enumerated value.
                    //
                    if (FlagOps.HasFlags(
                            currentUlongValue, localUlongValue, true) ||
                        (!uniqueValues && FlagOps.HasFlags(
                            previousUlongValue, localUlongValue, true)))
                    {
                        //
                        // NOTE: The current enumerated value has
                        //       now been handled; remove it from
                        //       the overall enumerated value and
                        //       add the name to the result list.
                        //
                        currentUlongValue &= ~localUlongValue;
                        previousUlongValue |= localUlongValue;

                        list.Add(name);

                        //
                        // NOTE: If the value is now zero, then we
                        //       are done.
                        //
                        if (currentUlongValue == 0)
                            break;
                    }
                }
                catch (Exception e)
                {
                    if (!skipBadValue)
                    {
                        error = e;
                        return null;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            //
            // NOTE: If there are any residual bit values within the
            //       overall enumerated value, add them verbatim to
            //       the result list.
            //
            if (currentUlongValue != 0)
                list.Add(currentUlongValue.ToString());

            return list;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList FlagsEnumV2(
            Enum enumValue,
            StringList enumNames,
            UlongList enumValues,
            bool skipNameless,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            if (enumValue == null)
            {
                error = "invalid value";
                return null;
            }

            Type enumType = enumValue.GetType();

            if (enumType == null)
            {
                error = "invalid type";
                return null;
            }

            if (!enumType.IsEnum)
            {
                error = String.Format(
                    "type {0} is not an enumeration",
                    TypeName(enumType));

                return null;
            }

            StringList localEnumNames = (enumNames != null) ?
                new StringList(enumNames) : null;

            UlongList localEnumValues = (enumValues != null) ?
                new UlongList(enumValues) : null;

            if (EnumOps.GetEnumNamesAndValues(enumType,
                    ref localEnumNames, ref localEnumValues,
                    ref error) != ReturnCode.Ok)
            {
                return null;
            }

            return FlagsEnumCore(
                enumValue, localEnumNames, localEnumValues,
                skipNameless, keepZeros, uniqueValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringList FlagsEnumCore(
            Enum enumValue,
            StringList enumNames,
            UlongList enumValues,
            bool skipNameless,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            if (enumValue == null)
            {
                error = "invalid value";
                return null;
            }

            if (enumNames == null)
            {
                error = "invalid enumeration names";
                return null;
            }

            if (enumValues == null)
            {
                error = "invalid enumeration values";
                return null;
            }

            if (enumNames.Count != enumValues.Count)
            {
                error = "mismatched names and values counts";
                return null;
            }

            ulong currentUlongValue;

            try
            {
                //
                // NOTE: Get the underlying unsigned long integer
                //       value for the overall enumerated value.
                //       This may throw an exception.
                //
                currentUlongValue = EnumOps.ToUIntOrULong(
                    enumValue); /* throw */
            }
            catch (Exception e)
            {
                error = e;
                return null;
            }

            StringList list = new StringList();

            //
            // NOTE: If the enumerated value is zero, just return
            //       the (empty) result list now.
            //
            if (currentUlongValue == 0)
                return list;

            int count = enumNames.Count;
            ulong previousUlongValue = 0;

            for (int index = 0; index < count; index++)
            {
                string localEnumName = enumNames[index];

                if (String.IsNullOrEmpty(localEnumName))
                {
                    //
                    // TODO: This block should never be hit?
                    //
                    if (!skipNameless)
                    {
                        error = "invalid enumeration name";
                        return null;
                    }
                    else
                    {
                        continue;
                    }
                }

                ulong localEnumValue = enumValues[index];

                //
                // NOTE: If the value for the current enumerated
                //       value is zero, skip it.  The associated
                //       names will never be added to the result
                //       unless the "keepZeros" flag is set.
                //
                if (localEnumValue == 0)
                {
                    if (keepZeros)
                        list.Add(localEnumName);
                    else
                        continue;
                }

                //
                // NOTE: Check if the overall enumerated value
                //       has all the bits set from the current
                //       enumerated value.
                //
                if (FlagOps.HasFlags(
                        currentUlongValue, localEnumValue, true) ||
                    (!uniqueValues && FlagOps.HasFlags(
                        previousUlongValue, localEnumValue, true)))
                {
                    //
                    // NOTE: The current enumerated value has
                    //       now been handled; remove it from
                    //       the overall enumerated value and
                    //       add the name to the result list.
                    //
                    currentUlongValue &= ~localEnumValue;
                    previousUlongValue |= localEnumValue;

                    list.Add(localEnumName);

                    //
                    // NOTE: If the value is now zero, then we
                    //       are done.
                    //
                    if (currentUlongValue == 0)
                        break;
                }
            }

            //
            // NOTE: If there are any residual bit values within the
            //       overall enumerated value, add them verbatim to
            //       the result list.
            //
            if (currentUlongValue != 0)
                list.Add(currentUlongValue.ToString());

            return list;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayRegExMatch(
            Match match
            )
        {
            if (match == null)
                return DisplayNull;

            if (!match.Success)
                return "<notSuccess>";

            GroupCollection groups = match.Groups;

            if (groups == null)
                return "<nullGroups>";

            if (groups.Count == 0)
                return "<groupZeroMissing>";

            Group group = groups[0];

            if (group == null)
                return "<groupZeroNull>";

            return String.Format(
                "index {0}, length {1}, value {2}", group.Index,
                group.Length, WrapOrNull(group.Value));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static string TclBuildFileName(
            TclBuild build
            )
        {
            if (build == null)
                return DisplayNull;

            return WrapOrNull(build.FileName);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayCallFrame(
            ICallFrame frame
            )
        {
            if (frame == null)
                return DisplayNull;

            return String.Format(
                "{0} ({1})", frame.Name, frame.FrameId).Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayColor(
            ConsoleColor color
            )
        {
            return (color != _ConsoleColor.None) ? color.ToString() : "None";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        public static StringList ConsoleKeyInfo(
            ConsoleKeyInfo consoleKeyInfo
            )
        {
            return new StringList(
                "Modifiers", consoleKeyInfo.Modifiers.ToString(),
                "Key", consoleKeyInfo.Key.ToString(),
                "KeyChar", consoleKeyInfo.KeyChar.ToString());
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static string DisplayWaitHandle(
            WaitHandle waitHandle
            )
        {
            if (waitHandle != null)
                return String.Format("{0}", waitHandle.Handle);

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayWaitHandles(
            WaitHandle[] waitHandles
            )
        {
            if (waitHandles != null)
            {
                StringBuilder result = StringOps.NewStringBuilder();

                for (int index = 0; index < waitHandles.Length; index++)
                {
                    WaitHandle waitHandle = waitHandles[index];

                    if (waitHandle != null)
                    {
                        if (result.Length > 0)
                            result.Append(Characters.Space);

                        result.Append(DisplayWaitHandle(waitHandle));
                    }
                }

                return result.ToString();
            }

            return DisplayNull;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_ARGUMENTS
        public static string WrapTraceOrNull(
            bool normalize,
            bool ellipsis,
            bool quote,
            bool display,
            object value
            )
        {
            string result = (value != null) ? value.ToString() : null;

            if (result != null)
            {
                if (result.Length > 0)
                {
                    try
                    {
                        if (normalize)
                        {
                            result = StringOps.NormalizeWhiteSpace(
                                result, Characters.Space,
                                WhiteSpaceFlags.FormattedUse);
                        }

                        if (ellipsis)
                        {
                            result = Ellipsis(result, GetEllipsisLimit(
                                WrapEllipsisLimit), false);
                        }

                        return quote ? StringList.MakeList(result) : result;
                    }
                    catch (Exception e)
                    {
                        Type type = (e != null) ? e.GetType() : null;

                        return String.Format(DisplayError,
                            (type != null) ? type.Name : UnknownTypeName);
                    }
                }

                return display ? DisplayEmpty : String.Empty;
            }

            return display ? DisplayNull : null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ToString(
            IBinder binder,
            CultureInfo cultureInfo,
            object value,
            string @default
            )
        {
            IScriptBinder scriptBinder = binder as IScriptBinder;

            if (scriptBinder == null)
                goto fallback;

            Type type = (value != null) ? value.GetType() : typeof(object);

            if (!scriptBinder.HasToStringCallback(type, false))
                goto fallback;

            IChangeTypeData changeTypeData = new ChangeTypeData(
                "FormatOps.ToString", type, value, null, cultureInfo, null,
                MarshalFlags.None);

            ReturnCode code;
            Result error = null;

            code = scriptBinder.ToString(changeTypeData, ref error);

            if (code == ReturnCode.Ok)
            {
                string stringValue = changeTypeData.NewValue as string;

                if (stringValue == null)
                    goto fallback;

                return stringValue;
            }
            else
            {
                DebugOps.Complain(code, error);
            }

        fallback:

            return (value != null) ? value.ToString() : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodArguments(
            IBinder binder,
            CultureInfo cultureInfo,
            IEnumerable<object> args,
            bool display
            )
        {
            string @default = display ? DisplayNull : null;

            if (args == null)
                return @default;

            StringList list = new StringList();
            int index = 0;

            foreach (object arg in args)
            {
                Type type = (arg != null) ? arg.GetType() : null;

                list.Add(StringList.MakeList(
                    index, (type != null) ? type.FullName : @default,
                    ToString(binder, cultureInfo, arg, @default)));

                index++;
            }

            if (list.Count == 0)
                return display ? DisplayEmpty : null;

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapArgumentsOrNull(
            bool normalize,
            bool ellipsis,
            IEnumerable<string> args
            )
        {
            if (args == null)
                return DisplayNull;

            return WrapOrNull(normalize, ellipsis, new StringList(args));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScriptForLog(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            if (value == null)
                return DisplayNull;

            string text = value.ToString();

            if (text == null) /* NOTE: Impossible? */
                return DisplayNullString;

            if (text.Length == 0)
                return DisplayEmpty;

            if (text.Trim().Length == 0)
                return DisplaySpace;

            if (normalize)
            {
                text = StringOps.NormalizeWhiteSpace(
                    text, Characters.Space, WhiteSpaceFlags.FormattedUse);
            }

            if (ellipsis)
            {
                text = Ellipsis(text, GetEllipsisLimit(WrapEllipsisLimit),
                    false);
            }

            return StringList.MakeList(text);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return WrapOrNull(normalize, ellipsis, false, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            object value
            )
        {
            return WrapOrNull(
                (value != null), normalize, ellipsis, display, QuotationMark, value,
                QuotationMark);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string StripOuter(
            string value,
            char character
            )
        {
            return StripOuter(value, character, character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string StripOuter(
            string value,
            char prefix,
            char suffix
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            int length = value.Length;

            if (length < 2) /* i.e. prefix + suffix */
                return value;

            int prefixIndex = value.IndexOf(prefix);

            if (prefixIndex != 0)
                return value;

            int suffixIndex = value.LastIndexOf(suffix);

            if (suffixIndex != (length - 1))
                return value;

            return value.Substring(prefixIndex + 1, length - 2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ReleaseAttribute(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            //
            // NOTE: Skip special handling if the regular expression
            //       pattern that we need is not available.
            //
            if (releaseShortNameRegEx != null)
            {
                //
                // NOTE: The convention used here is that the release
                //       attribute contains a string of the format:
                //
                //       "<Short_Description>(\n|.|,) <Type> XY"
                //
                //       Where "Short_Description" is something like
                //       "Namespaces Edition", "Type" is one of
                //       ["Alpha", "Beta", "Final", "Release"] and
                //       "XY" is a number.  Together, the "Type" and
                //       "XY" portion are considered to really be the
                //       "Short_Name".
                //
                int index = value.LastIndexOf(Characters.LineFeed);

                if (index == Index.Invalid)
                    index = value.LastIndexOf(Characters.Comma);

                if (index == Index.Invalid)
                    index = value.LastIndexOf(Characters.Period);

                //
                // NOTE: Extract the "Short_Name" portion of the value.
                //
                if (index != Index.Invalid)
                {
                    string partOne = value.Substring(0, index).Trim();
                    string partTwo = value.Substring(index + 1).Trim();

                    if (releaseShortNameRegEx.IsMatch(partTwo))
                        return partTwo;
                    else if (releaseShortNameRegEx.IsMatch(partOne))
                        return partOne;
                }
            }

            //
            // NOTE: Return the whole original string, with extra
            //       spaces removed, possibly wrapped in quotes.
            //
            return WrapOrNull(value.Trim());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NETWORK
        public static string WrapOrNull(
            byte[] bytes
            )
        {
            if (bytes == null)
                return DisplayNull;

            return Parser.Quote(StringList.MakeList(
                "Length", bytes.Length, "Base64", (bytes.Length > 0) ?
                Convert.ToBase64String(bytes) : DisplayEmpty));
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string WrapOrNull(
            object value
            )
        {
            return WrapOrNull((value != null), value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WrapOrNull(
            bool wrap,
            object value
            )
        {
            return WrapOrNull(
                wrap, false, false, true, QuotationMark, value, QuotationMark);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string WrapOrNull(
            bool wrap,
            bool normalize,
            bool ellipsis,
            bool display,
            string prefix,
            object value,
            string suffix
            )
        {
            if (wrap)
            {
                try
                {
                    string result = (value != null) ? value.ToString() : null;

                    if (normalize)
                    {
                        result = StringOps.NormalizeWhiteSpace(
                            result, Characters.Space,
                            WhiteSpaceFlags.FormattedUse);
                    }

                    if (ellipsis)
                    {
                        result = Ellipsis(result, GetEllipsisLimit(
                            WrapEllipsisLimit), false);
                    }

                    if (display)
                    {
                        if (result == null)
                            return DisplayNull;

                        if (result.Length == 0)
                            return DisplayEmpty;
                    }

                    return String.Format("{0}{1}{2}", prefix, result, suffix);
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayError,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
        public static bool HaveCacheCounts(
            int[] counts
            )
        {
            if (counts == null)
                return false;

            if (counts.Length < (int)CacheCountType.SizeOf)
                return false;

            for (int index = 0; index < (int)CacheCountType.SizeOf; index++)
                if (counts[index] > 0) return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CacheCounts(
            int[] counts
            )
        {
            if ((counts != null) &&
                (counts.Length >= (int)CacheCountType.SizeOf))
            {
                int hit = counts[(int)CacheCountType.Hit];
                int miss = counts[(int)CacheCountType.Miss];
                int skip = counts[(int)CacheCountType.Skip];
                int collide = counts[(int)CacheCountType.Collide];
                int found = counts[(int)CacheCountType.Found];
                int notFound = counts[(int)CacheCountType.NotFound];
                int add = counts[(int)CacheCountType.Add];
                int change = counts[(int)CacheCountType.Change];
                int remove = counts[(int)CacheCountType.Remove];
                int clear = counts[(int)CacheCountType.Clear];
                int trim = counts[(int)CacheCountType.Trim];
                int total = hit + miss;

                double percent = (total != 0) ?
                    ((double)hit / (double)total) * 100 : 0;

                return StringList.MakeList(
                    "hit%", String.Format("{0:0.####}%", percent),
                    "hit", hit, "miss", miss, "skip", skip, "collide",
                    collide, "found", found, "notFound", notFound,
                    "add", add, "change", change, "remove", remove,
                    "clear", clear, "trim", trim);
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string KeysAndValues(
            _Containers.Public.StringDictionary dictionary,
            bool display,
            bool normalize,
            bool ellipsis
            )
        {
            string result = (dictionary != null) ?
                dictionary.KeysAndValuesToString(null, false) : null;

            return display ? WrapOrNull(normalize, ellipsis, result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NameValueCollection(
            NameValueCollection collection,
            bool display
            )
        {
            if (collection == null)
                return display ? DisplayNull : null;

            StringList list = null;
            int count = collection.Count;

            if (count > 0)
            {
                list = new StringList();

                for (int index = 0; index < count; index++)
                {
                    list.Add(collection.GetKey(index));
                    list.Add(collection.Get(index));
                }
            }

            if (list == null)
                return display ? DisplayEmpty : null;

            return WrapOrNull(list);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Complaint(
            long id,
            ReturnCode code,
            Result result,
            string stackTrace
            )
        {
            return ThreadMessage(
                GlobalState.GetCurrentSystemThreadId(), id,
                ResultOps.Format(code, result), stackTrace);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ThreadIdOrNull(
            Thread thread
            )
        {
            if (thread == null)
                return DisplayNull;

            return thread.ManagedThreadId.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayThread(
            Thread thread
            )
        {
            if (thread != null)
            {
                try
                {
                    StringBuilder result = StringOps.NewStringBuilder();

                    result.AppendFormat(
                        "{0}: {1}, ", "Name", WrapOrNull(thread.Name));

                    result.AppendFormat(
                        "{0}: {1}, ", "ManagedThreadId", thread.ManagedThreadId);

                    result.AppendFormat(
                        "{0}: {1}, ", "Priority", thread.Priority);

                    result.AppendFormat(
                        "{0}: {1}, ", "ApartmentState", thread.ApartmentState);

                    result.AppendFormat(
                        "{0}: {1}, ", "ThreadState", thread.ThreadState);

                    result.AppendFormat(
                        "{0}: {1}, ", "IsAlive", thread.IsAlive);

                    result.AppendFormat(
                        "{0}: {1}, ", "IsBackground", thread.IsBackground);

                    result.AppendFormat(
                        "{0}: {1}, ", "IsThreadPoolThread", thread.IsThreadPoolThread);

                    result.AppendFormat(
                        "{0}: {1}, ", "CurrentCulture", WrapOrNull(thread.CurrentCulture));

                    result.AppendFormat(
                        "{0}: {1}", "CurrentUICulture", WrapOrNull(thread.CurrentUICulture));

                    return result.ToString();
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayError,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ProcedureBody(
            string body,
            int startLine,
            bool showLines
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(body))
            {
                if (showLines)
                {
                    int line = (startLine != Parser.NoLine)
                        ? startLine : Parser.StartLine;

                    int count = Parser.CountLines(body);

                    string format = "{0," + (MathOps.Log10(line +
                        count) + ((count >= 10) ? 1 : 0)).ToString() + "}: ";

                    result.AppendFormat(format, line++);

                    for (int index = 0; index < body.Length; index++)
                    {
                        char character = body[index];

                        result.Append(character);

                        if (Parser.IsLineTerminator(character))
                            result.AppendFormat(format, line++);
                    }
                }
                else
                {
                    result.Append(body);
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ThreadMessage(
            int threadId,
            long id,
            string message,
            string stackTrace
            )
        {
            return String.Format("{0} ({1}): {2}{3}{3}{4}{3}",
                threadId, id, message, Environment.NewLine, stackTrace).Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexadecimal(
            byte value,
            bool prefix
            )
        {
            return String.Format("{0}{1}",
                prefix ? HexadecimalPrefix : String.Empty,
                value.ToString(ByteOutputFormat));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexadecimal(
            ulong value,
            bool prefix
            )
        {
            return String.Format("{0}{1}",
                prefix ? HexadecimalPrefix : String.Empty,
                value.ToString(ULongOutputFormat));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexadecimal(
            ValueType value,
            bool prefix
            )
        {
            return String.Format(
                HexadecimalFormat,
                prefix ? HexadecimalPrefix : String.Empty, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hexavigesimal(
            ulong value,
            byte width
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (value > 0)
            {
                do
                {
                    //
                    // NOTE: Get the current digit.
                    //
                    ulong digit = value % (ulong)HexavigesimalAlphabet.Length;

                    //
                    // NOTE: Append it to the result.
                    //
                    result.Append(HexavigesimalAlphabet[(int)digit]);

                    //
                    // NOTE: Advance to the next digit.
                    //
                    value /= (ulong)HexavigesimalAlphabet.Length;

                    //
                    // NOTE: Continue until we no longer need more digits.
                    //
                } while (value > 0);

                //
                // NOTE: Finally, reverse the string to put the digits in
                //       the correct order.
                //
                result = StringOps.NewStringBuilder(
                    StringOps.StrReverse(result.ToString()));
            }
            else
            {
                //
                // NOTE: The value is exactly zero.
                //
                result.Append(HexavigesimalAlphabet[0]);
            }

            //
            // NOTE: If requested, 'zero' pad to the requested width.
            //
            if (width > result.Length)
                result.Insert(0, StringOps.StrRepeat(width - result.Length, HexavigesimalAlphabet[0]));

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PackageDateTime(
            DateTime value
            )
        {
            return value.ToString(PackageDateTimeOutputFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TranslateDateTimeFormats(
            CultureInfo cultureInfo,
            TimeZone timeZone,
            string format,
            DateTime dateTime,
            DateTime epoch,
            bool useFormats,
            bool useDelegates
            )
        {
            if (!String.IsNullOrEmpty(format))
            {
                if (useFormats && (tclClockFormats != null))
                {
                    foreach (IPair<string> element in tclClockFormats)
                    {
                        if ((element != null) &&
                            !String.IsNullOrEmpty(element.X))
                        {
                            if (element.Y != null)
                                format = format.Replace(element.X, element.Y);
                        }
                    }
                }

                if (useDelegates && (tclClockDelegates != null))
                {
                    IClockData clockData = new ClockData(null, cultureInfo, timeZone,
                        format, dateTime, epoch, ClientData.Empty);

                    foreach (KeyValuePair<string, Delegate> pair in tclClockDelegates)
                    {
                        if ((pair.Value != null) && (format.IndexOf(pair.Key,
                                StringOps.SystemStringComparisonType) != Index.Invalid))
                        {
                            string newValue = pair.Value.DynamicInvoke(clockData) as string;

                            if (newValue != null)
                                format = format.Replace(pair.Key, newValue);
                        }
                    }
                }
            }

            return format;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TclClockDateTime(
            CultureInfo cultureInfo,
            TimeZone timeZone,
            string format,
            DateTime dateTime,
            DateTime epoch
            )
        {
            if (!String.IsNullOrEmpty(format))
            {
                format = TranslateDateTimeFormats(
                    cultureInfo, timeZone, format, dateTime, epoch, true, true);

                if (format.Trim().Length > 0)
                {
                    return (cultureInfo != null) ?
                        dateTime.ToString(format, cultureInfo) :
                        dateTime.ToString(format);
                }
            }

            return format;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The reason to retain this wrapper method is to keep
        //       its intent clear as "mainly for cosmetic purposes"
        //       (i.e. it is only used when displaying strings).
        //
        public static string NormalizeNewLines(
            string value
            )
        {
            return StringOps.NormalizeLineEndings(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ReplaceNewLines(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            StringBuilder builder = StringOps.NewStringBuilder(value);

            StringOps.FixupDisplayLineEndings(builder, false);

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayEngineResult(
            Result value
            )
        {
            if (value != null)
            {
                StringBuilder result = StringOps.NewStringBuilder();

                result.AppendFormat(
                    "{0}: {1}, ", "ReturnCode", value.ReturnCode);

                result.AppendFormat(
                    "{0}: {1}, ", "Result",
                    WrapOrNull(true, true, value));

                result.AppendFormat(
                    "{0}: {1}, ", "ErrorLine", value.ErrorLine);

                result.AppendFormat(
                    "{0}: {1}, ", "ErrorCode", value.ErrorCode);

                result.AppendFormat(
                    "{0}: {1}, ", "ErrorInfo",
                    WrapOrNull(true, true, value.ErrorInfo));

                result.AppendFormat(
                    "{0}: {1}, ", "PreviousReturnCode", value.PreviousReturnCode);

                result.AppendFormat(
                    "{0}: {1}", "Exception",
                    WrapOrNull(true, true, value.Exception));

                return result.ToString();
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayResult(
            string value,
            bool ellipsis,
            bool replaceNewLines
            )
        {
            if (value != null)
            {
                if (value.Length > 0)
                    return Result(value, ellipsis, replaceNewLines);
                else
                    return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayValue(
            string value
            )
        {
            if (value == null)
                return DisplayNull;

            if (value.Length == 0)
                return DisplayEmpty;

            string trimmed = value.Trim();

            if (trimmed.Length == 0)
                return DisplaySpace;

            return value;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Result(
            string value,
            bool ellipsis,
            bool replaceNewLines
            )
        {
            string result = value;

            if (ellipsis)
            {
                result = Ellipsis(
                    result, 0, (result != null) ? result.Length : 0,
                    GetEllipsisLimit(ResultEllipsisLimit), false,
                    ResultEllipsis);
            }

            if (replaceNewLines)
                result = ReplaceNewLines(NormalizeNewLines(result));

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Performance(
            IClientData clientData
            )
        {
            PerformanceClientData performanceClientData =
                clientData as PerformanceClientData;

            return (performanceClientData != null) ?
                Performance(performanceClientData.Microseconds) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Performance(
            double microseconds
            )
        {
            return Performance(microseconds, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Performance(
            double microseconds,
            string suffix
            )
        {
            return String.Format(
                "{0:0.####} {1}microseconds per iteration",
                Interpreter.FixIntermediatePrecision(microseconds), suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PerformanceWithStatistics(
            long requestedIterations,
            long actualIterations,
            long resultIterations,
            ReturnCode code,
            Result result,
            long startCount,
            long stopCount,
            double averageMicroseconds,
            double minimumMicroseconds,
            double maximumMicroseconds
            )
        {
            StringList localResult = new StringList();

            localResult.Add(String.Format("{0} requested iterations", requestedIterations));
            localResult.Add(String.Format("{0} actual iterations", actualIterations));
            localResult.Add(String.Format("{0} result iterations", resultIterations));
            localResult.Add(new StringPair("code", code.ToString()).ToString());

            if (result != null)
                localResult.Add(new StringPair("result", result).ToString());

            localResult.Add(String.Format("{0} raw start count", startCount));
            localResult.Add(String.Format("{0} raw stop count", stopCount));

            localResult.Add(String.Format("{0} count per second",
                PerformanceOps.GetCountsPerSecond()));

            localResult.Add(Performance(averageMicroseconds, "average "));
            localResult.Add(Performance(minimumMicroseconds, "minimum "));
            localResult.Add(Performance(maximumMicroseconds, "maximum "));

            return localResult.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if HISTORY
        public static StringPair HistoryItem(
            int count,
            IClientData clientData,
            bool ellipsis,
            bool replaceNewLines
            )
        {
            HistoryClientData historyClientData = clientData as HistoryClientData;

            if (historyClientData != null)
            {
                ArgumentList arguments = historyClientData.Arguments;

                string value = (arguments != null) ?
                    arguments.ToString() : DisplayNull;

                if (ellipsis)
                {
                    value = Ellipsis(
                        value, 0, (value != null) ? value.Length : 0,
                        GetEllipsisLimit(HistoryEllipsisLimit), false,
                        HistoryEllipsis);
                }

                if (replaceNewLines)
                    value = ReplaceNewLines(NormalizeNewLines(value));

                return new StringPair(String.Format(
                    "#{0}, Level {1}, {2}", count, historyClientData.Levels,
                    historyClientData.Flags), value);
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int GetEllipsisLimit(
            int @default
            )
        {
            string value = CommonOps.Environment.GetVariable(
                EnvVars.EllipsisLimit);

            if (!String.IsNullOrEmpty(value))
            {
                int intValue = 0;

                if (Value.GetInteger2(
                        value, ValueFlags.AnyInteger, null,
                        ref intValue) == ReturnCode.Ok)
                {
                    return intValue;
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Ellipsis(
            string value
            )
        {
            return Ellipsis(
                value, 0, (value != null) ? value.Length : 0,
                GetEllipsisLimit(DefaultEllipsisLimit), false,
                DefaultEllipsis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Ellipsis(
            string value,
            int limit,
            bool strict
            )
        {
            return Ellipsis(value, 0, (value != null) ?
                value.Length : 0, limit, strict, DefaultEllipsis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Ellipsis(
            string value,
            int startIndex,
            int length,
            int limit,
            bool strict
            )
        {
            return Ellipsis(value, startIndex, length, limit, strict, DefaultEllipsis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string Ellipsis(
            string value,
            int startIndex,
            int length,
            int limit,
            bool strict,
            string ellipsis
            )
        {
            string result = value;

            if (!String.IsNullOrEmpty(result) && (limit >= 0))
            {
                if ((startIndex >= 0) && (startIndex < result.Length))
                {
                    //
                    // NOTE: Are we going to actually truncate anything?
                    //
                    if (length > limit)
                    {
                        //
                        // NOTE: Prevent going past the end of the string.
                        //
                        if ((startIndex + limit) > result.Length)
                            limit = result.Length - startIndex;

                        //
                        // NOTE: Was a valid ellipsis string provided and will
                        //       it fit within the limit?
                        //
                        if (!String.IsNullOrEmpty(ellipsis) &&
                            (limit >= ellipsis.Length))
                        {
                            int newLimit = limit;

                            if (strict)
                                newLimit -= ellipsis.Length;

                            result = String.Format("{0}{1}",
                                result.Substring(startIndex, newLimit), ellipsis);
                        }
                        else
                        {
                            //
                            // BUGFIX: If the ellipsis is invalid or the limit is
                            //         less than the length of the it, just use
                            //         the initial substring of the value.
                            //
                            result = result.Substring(startIndex, limit);
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Prevent going past the end of the string.
                        //
                        if ((startIndex + length) > result.Length)
                            length = result.Length - startIndex;

                        result = result.Substring(startIndex, length);
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hash(
            Hash hash
            )
        {
            return (hash != null) ?
                StringList.MakeList(
                    "md5", Hash(hash.MD5),
                    "sha1", Hash(hash.SHA1)) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SourceId(
            Assembly assembly,
            string @default
            )
        {
            string result = SharedAttributeOps.GetAssemblySourceId(assembly);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SourceTimeStamp(
            Assembly assembly,
            string @default
            )
        {
            string result = SharedAttributeOps.GetAssemblySourceTimeStamp(assembly);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string UpdateUri(
            Assembly assembly,
            string @default
            )
        {
            Uri uri = SharedAttributeOps.GetAssemblyUpdateBaseUri(assembly);
            return (uri != null) ? uri.ToString() : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DownloadUri(
            Assembly assembly,
            string @default
            )
        {
            Uri uri = SharedAttributeOps.GetAssemblyDownloadBaseUri(assembly);
            return (uri != null) ? uri.ToString() : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PublicKeyToken(
            AssemblyName assemblyName,
            string @default
            )
        {
            string result = AssemblyOps.GetPublicKeyToken(assemblyName);
            return (result != null) ? result : @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string StrongName(
            Assembly assembly,
            StrongName strongName,
            bool verified
            )
        {
            if ((assembly != null) && (strongName != null))
            {
                AssemblyName assemblyName = assembly.GetName();

                if (assemblyName != null)
                {
                    byte[] assemblyNamePublicKey = assemblyName.GetPublicKey();

                    try
                    {
                        bool isMono = CommonOps.Runtime.IsMono();

                        //
                        // HACK: Is there no other way to get the public key byte array
                        //       from a StrongName object?
                        //
                        byte[] strongNamePublicKey = (byte[])
                            typeof(StrongNamePublicKeyBlob).InvokeMember(isMono ? "pubkey" : "PublicKey",
                            MarshalOps.PrivateInstanceGetFieldBindingFlags, null, strongName.PublicKey,
                            null);

                        //
                        // NOTE: Make sure the caller gave us a "matching set" of objects.
                        //
                        if (ArrayOps.Equals(assemblyNamePublicKey, strongNamePublicKey))
                        {
                            string strongNameName = strongName.Name;
                            Version strongNameVersion = strongName.Version;
                            string strongNameTag = SharedAttributeOps.GetAssemblyStrongNameTag(assembly);
                            byte[] assemblyNamePublicKeyToken = assemblyName.GetPublicKeyToken();

                            StringList list = new StringList();

                            if (strongNameName != null)
                                list.Add("name", strongNameName);

                            if (strongNameVersion != null)
                                list.Add("version", strongNameVersion.ToString());

                            if (assemblyNamePublicKeyToken != null)
                                list.Add("publicKeyToken", ArrayOps.ToHexadecimalString(
                                    assemblyNamePublicKeyToken));

                            list.Add("verified",
                                (verified && RuntimeOps.IsStrongNameVerified(
                                    assembly.Location, true)).ToString());

                            if (strongNameTag != null)
                                list.Add("tag", strongNameTag);

                            return list.ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        //
                        // NOTE: Nothing we can do here except log the failure.
                        //       The method name reported in the trace output
                        //       here may be wrong due to skipping of built-in
                        //       classes by the DebugOps class.
                        //
                        TraceOps.DebugTrace(
                            e, typeof(FormatOps).Name,
                            TracePriority.SecurityError);
                    }
                }
            }

            return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Certificate(
            Assembly assembly,
            X509Certificate certificate,
            bool trusted,
            bool verbose,
            bool wrap
            )
        {
            return Certificate(
                (assembly != null) ? assembly.Location : null,
                certificate, trusted, verbose, wrap);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Certificate(
            X509Certificate certificate,
            bool verbose,
            bool wrap
            )
        {
            StringList list = RuntimeOps.CertificateToList(
                certificate, verbose);

            string result = (list != null) ?
                list.ToString() : null;

            return wrap ? WrapOrNull(result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Certificate(
            string fileName,
            X509Certificate certificate,
            bool trusted,
            bool verbose,
            bool wrap
            )
        {
            StringList list = RuntimeOps.CertificateToList(
                certificate, verbose);

            string result = null;

            if (list != null)
            {
                if (fileName != null)
                {
                    list.Add("trusted", trusted ?
                        RuntimeOps.IsFileTrusted(
                            fileName, IntPtr.Zero).ToString() :
                        false.ToString());
                }

                result = list.ToString();
            }

            return wrap ? WrapOrNull(result) : result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool NeedScriptExtension(
            string value,
            ref string extension
            )
        {
            //
            // NOTE: If the [path] value is null or empty, there would be
            //       no need to add a file extension.
            //
            if (String.IsNullOrEmpty(value))
                return false;

            //
            // NOTE: Grab the script file extension.  This should normally
            //       be ".eagle".
            //
            string scriptExtension = FileExtension.Script;

            //
            // NOTE: If the script file extension is null (or empty), there
            //       is no point in ever appending it [to anything].
            //
            if (String.IsNullOrEmpty(scriptExtension))
                return false;

            //
            // NOTE: If the file name already ends with the script file
            //       extension, there is no point in appending it.
            //
            if (value.EndsWith(scriptExtension, PathOps.ComparisonType))
                return false;

            //
            // NOTE: If the file name already ends with any "well-known"
            //       file extension, skip appending an extension.
            //
            if (PathOps.HasKnownExtension(value))
                return false;

            extension = scriptExtension;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScriptTypeToFileName(
            string scriptType,
            PackageType packageType,
            bool fileNameOnly,
            bool strict
            )
        {
            string result = scriptType;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: If the "script type" (which might really be a file
                //       name) specified by the caller already has the file
                //       extension, skip appending it; otherwise, make sure
                //       that it ends with the file extension now.
                //
                string extension = null;

                if (NeedScriptExtension(result, ref extension))
                {
                    //
                    // NOTE: Append the script file extension to the base
                    //       name (i.e. "script type").
                    //
                    result = String.Format("{0}{1}", result, extension);
                }

                //
                // NOTE: If the result already has some kind of directory,
                //       skip adding the library path fragment; otherwise,
                //       make sure it has the library path fragment as a
                //       prefix.
                //
                if (!fileNameOnly && !PathOps.HasDirectory(result))
                {
                    //
                    // HACK: In the [missing] default case here, we simply
                    //       do nothing.
                    //
                    switch (packageType)
                    {
                        case PackageType.Library:
                            {
                                result = PathOps.GetUnixPath(
                                    PathOps.CombinePath(null,
                                    ScriptPaths.LibraryPackage,
                                    result));

                                break;
                            }
                        case PackageType.Test:
                            {
                                result = PathOps.GetUnixPath(
                                    PathOps.CombinePath(null,
                                    ScriptPaths.TestPackage,
                                    result));

                                break;
                            }
                    }
                }

                return result;
            }
            else if (!strict)
            {
                return result; /* NOTE: Either "null" or "String.Empty". */
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CultureName(
            CultureInfo cultureInfo,
            bool display
            )
        {
            if (cultureInfo != null)
            {
                //
                // NOTE: For some reason, the invariant culture has an empty
                //       string as the result of its ToString() method.  In
                //       that case, use the string "invariant" if the caller
                //       has not requested the display name.
                //
                string result = cultureInfo.ToString();

                if ((result != null) && (result.Length == 0))
                {
                    result = display ?
                        cultureInfo.DisplayName : "invariant";
                }
                else if (display && (result == null))
                {
                    result = cultureInfo.DisplayName;
                }

                return result;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string BreakOrFail(
            string methodName,
            params string[] strings
            )
        {
            return String.Format("{0}: {1}", methodName,
                (strings != null) ? StringList.MakeList(strings) : DisplayNull);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NumericTimeZone(
            long totalSeconds
            )
        {
            //
            // NOTE: This code was cut and pasted from
            //       ::tcl::clock::FormatNumericTimeZone
            //       (Tcl 8.5+) and translated from Tcl
            //       to C#.
            //
            StringBuilder result = StringOps.NewStringBuilder();

            if (totalSeconds < 0)
            {
                result.Append(Characters.MinusSign);
                totalSeconds = -totalSeconds; /* normalize */
            }
            else
            {
                result.Append(Characters.PlusSign);
            }

            result.AppendFormat("{0:00}", totalSeconds / 3600);
            totalSeconds = totalSeconds % 3600;

            result.AppendFormat("{0:00}", totalSeconds / 60);
            totalSeconds = totalSeconds % 60;

            if (totalSeconds != 0)
                result.AppendFormat("{0:00}", totalSeconds);

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AlwaysOrNever(
            long value
            )
        {
            if (value < 0)
                return "never";
            else if (value == 0)
                return "always";
            else
                return "sometimes";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Iso8601UpdateDateTime(
            DateTime value
            )
        {
            return value.ToString(Iso8601UpdateDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Iso8601TraceDateTime(
            DateTime value
            )
        {
            return value.ToString(Iso8601TraceDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Iso8601FullDateTime(
            DateTime value
            )
        {
            return value.ToString(Iso8601FullDateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY && NATIVE
        public static string Iso8601DateTime(
            DateTime value
            )
        {
            return Iso8601DateTime(value, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Iso8601DateTime(
            DateTime value,
            bool timeZone
            )
        {
            string offset = null;

            if (timeZone)
            {
                //
                // NOTE: Argument "value" cannot be null, it is a ValueType.
                //
                if ((value.Kind == DateTimeKind.Utc) ||
                    (value.Kind == DateTimeKind.Local))
                {
                    TimeSpan span =
                        TimeZone.CurrentTimeZone.GetUtcOffset(value);

                    offset = NumericTimeZone((long)span.TotalSeconds);
                }
            }

            return String.Format(
                "{0} {1}",
                value.ToString(Iso8601DateTimeOutputFormat), offset).Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SettingKey(
            IIdentifierBase identifierBase,
            ElementDictionary arrayValue,
            string varIndex
            ) /* CANNOT RETURN NULL */
        {
            if (identifierBase != null)
            {
                string varName = identifierBase.Name;

                if (varName != null)
                {
                    if (varIndex != null)
                    {
                        return String.Format(
                            "{0}{1}{2}{3}", varName,
                            Characters.OpenParenthesis, varIndex,
                            Characters.CloseParenthesis);
                    }
                    else
                    {
                        return varName;
                    }
                }
            }

            return varIndex;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorVariableName(
            IVariable variable,
            string linkIndex,
            string varName,
            string varIndex
            )
        {
            return ErrorVariableName(varName, varIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorVariableName(
            string varName,
            string varIndex
            )
        {
            return String.Format(
                "{0}{1}{0}", Characters.QuotationMark,
                VariableName(varName, varIndex));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string VariableName(
            string varName,
            string varIndex
            )
        {
            if (varIndex == null)
                return varName;

            return String.Format(
                "{0}{1}{2}{3}", varName, Characters.OpenParenthesis,
                varIndex, Characters.CloseParenthesis);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Breakpoint(
            BreakpointType breakpointType
            )
        {
            string result = String.Empty;

            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableGet:
                    result = "read";
                    break;
                case BreakpointType.BeforeVariableSet:
                case BreakpointType.BeforeVariableAdd:
                    result = "set";
                    break;
                case BreakpointType.BeforeVariableUnset:
                    result = "unset";
                    break;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string FunctionTypeName(
            string name
            )
        {
            return String.Format("{0}{1}{2}",
                typeof(_Functions.Default).Namespace,
                Type.Delimiter, name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string OperatorTypeName(
            string name
            )
        {
            return String.Format("{0}{1}{2}",
                typeof(_Operators.Default).Namespace,
                Type.Delimiter, name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayByteArray(
            byte[] bytes
            )
        {
            if (bytes == null)
                return DisplayNull;

            if (bytes.Length == 0)
                return DisplayEmpty;

            return BitConverter.ToString(bytes);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Hash(
            byte[] bytes
            )
        {
            return BitConverter.ToString(bytes).Replace(
                Characters.MinusSign.ToString(), String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Level(
            bool absolute,
            int level
            )
        {
            return String.Format(
                "{0}{1}",
                absolute ? Characters.NumberSign.ToString() : String.Empty,
                level);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
#if CACHE_DICTIONARY
        public static string MaybeEnableOrDisable(
            Dictionary<CacheFlags, int> dictionary,
            bool display
            )
        {
            IStringList list = GenericOps<CacheFlags, int>.KeysAndValues(
                dictionary, false, true, true, MatchMode.None, null, null,
                null, null, null, false);

            if (list == null)
                return display ? DisplayNull : null;

            if (display && (list.Count == 0))
                return DisplayEmpty;

            return list.ToString();
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayNamespace(
            INamespace @namespace
            )
        {
            if (@namespace == null)
                return DisplayNull;

            return DisplayValue(@namespace.ToString());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayScriptLocationList(
            ScriptLocationList scriptLocations
            )
        {
            if (scriptLocations == null)
                return DisplayUnavailable;

            if (scriptLocations.IsEmpty)
                return DisplayEmpty;

            IScriptLocation scriptLocation = scriptLocations.Peek();

            if (scriptLocation == null)
                return DisplayNull;

            return scriptLocation.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayString(
            string value
            )
        {
            return DisplayString(value, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayString(
            string value,
            bool wrap
            )
        {
            if (value != null)
            {
                if (value.Length > 0)
                    return wrap ? WrapOrNull(value) : value;

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayWidthAndHeight(
            int width,
            int height
            )
        {
            return String.Format(
                "Width={0}, Height={1}", width, height);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayException(
            Exception exception,
            bool innermost
            )
        {
            if (exception != null)
            {
                if (innermost)
                    while (exception.InnerException != null)
                        exception = exception.InnerException;

                return String.Format(DisplayFormat, exception.GetType());
            }

            return String.Format(DisplayFormat, typeof(Exception).Name.ToLower());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayKeys(
            IDictionary dictionary
            )
        {
            if (dictionary != null)
            {
                if (dictionary.Count > 0)
                    return new StringList(dictionary.Keys).ToString();

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayList(
            IList list
            )
        {
            if (list != null)
            {
                if (list.Count > 0)
                    return list.ToString();

                return DisplayEmpty;
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NameAndVersion(
            string name,
            Version version,
            string extra
            )
        {
            return NameAndVersion(name, (version != null) ? version.ToString() : null, extra);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NameAndVersion(
            string name,
            string version,
            string extra
            )
        {
            return String.Format("{0} {1} {2}", name, version, extra).Trim();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MajorMinor(
            Version version
            )
        {
            return MajorMinor(version, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MajorMinor(
            Version version,
            string prefix,
            string suffix
            )
        {
            return (version != null) ? String.Format("{0}{1}{2}", prefix,
                version.ToString(2), suffix) : String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AssemblyTextAndConfiguration(
            string text,
            string configuration,
            string prefix,
            string suffix
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(text))
                builder.Append(text);

            if (!String.IsNullOrEmpty(configuration))
            {
                if (builder.Length > 0)
                    builder.Append(ConfigurationSeparator);

                builder.Append(configuration);
            }

            if (builder.Length == 0)
                return String.Empty;

            return String.Format("{0}{1}{2}", prefix, builder, suffix);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ErrorWithException(
            Result error,
            Exception exception
            )
        {
            string result;

            if (error != null)
            {
                if (exception != null)
                    result = String.Format("{0}{1}{2}{3}", error, Environment.NewLine,
                        Environment.NewLine, exception);
                else
                    result = error;
            }
            else
            {
                if (exception != null)
                    result = exception.ToString();
                else
                    result = String.Empty;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string NestedArrayName(
            string name,
            string index
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(name))
            {
                result.Append(name);

                if (!String.IsNullOrEmpty(index))
                {
                    result.Append(Characters.Underscore);
                    result.Append(index.Replace(Characters.Comma, Characters.Underscore));
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeName(
            object @object
            )
        {
            return (@object != null) ? TypeName(@object.GetType()) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeName(
            Type type
            )
        {
            return (type != null) ? type.FullName : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string InvokeRawTypeName(
            Type type
            )
        {
            if (type == null)
                return DisplayNull;

            return InvokeRawTypeName(type, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string InvokeRawTypeName(
            Type type,
            bool full
            )
        {
            if (type == null)
                return DisplayNull;

            return WrapOrNull(QualifiedAndOrFullName(type, full,
                !IsSystemAssembly(type) && !IsSameAssembly(type),
                true));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeNameOrFullName(
            Type type
            )
        {
            return (type != null) ?
                TypeNameOrFullName(type, !IsSameAssembly(type)) :
                DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TypeNameOrFullName(
            Type type,
            bool full
            )
        {
            return (type != null) ?
                (full ? type.FullName : type.Name) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static string TclBridgeName(
            string interpName,
            string commandName
            )
        {
            return StringList.MakeList(interpName, commandName);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PackageName(
            string name,
            Version version
            )
        {
            if (version != null)
                return String.Format("{0} {1}",
                    (name != null) ? name : DisplayNull, version);
            else
                return name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PackageDirectory(
            string name,
            Version version,
            bool full
            )
        {
            return String.Format("{0}{1}{2}",
                full ? TclVars.LibPath + Path.DirectorySeparatorChar : String.Empty, name, version);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ProcessName(
            Process process,
            bool display
            )
        {
            if (process != null)
            {
                string fileName = PathOps.GetProcessMainModuleFileName(
                    process, false);

                int id = process.Id;

                if (!String.IsNullOrEmpty(fileName))
                    return StringList.MakeList(id, fileName);
                else
                    return id.ToString();
            }

            return display ? DisplayUnknown : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string BetweenOrExact(
            int lowerBound,
            int upperBound
            )
        {
            return String.Format(
                (lowerBound != upperBound) ?
                    "between {0} and {1}" : "{0}",
                lowerBound, upperBound);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayName(
            IIdentifierBase identifierBase,
            Interpreter interpreter,
            ArgumentList arguments
            )
        {
            string commandName = DisplayName(identifierBase);

            if ((interpreter != null) &&
                (arguments != null) && (arguments.Count >= 2))
            {
                IEnsemble ensemble = identifierBase as IEnsemble;

                if (ensemble != null)
                {
                    string subCommandName = arguments[1];

                    if (subCommandName != null)
                    {
                        if (ScriptOps.SubCommandFromEnsemble(
                                interpreter, ensemble, null, true, false,
                                ref subCommandName) == ReturnCode.Ok)
                        {
                            return StringList.MakeList(
                                commandName, subCommandName);
                        }
                    }
                }
            }

            return commandName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayName(
            IIdentifierBase identifierBase
            )
        {
            return (identifierBase != null) ?
                DisplayName(identifierBase.Name) : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayName(
            string name
            )
        {
            return (name != null) ? name : DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayPath(
            string path
            )
        {
            if (path == null)
                return DisplayNull;

            return WrapOrNull(path.Trim(PathOps.DirectoryChars));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayAssemblyName(
            Assembly assembly
            )
        {
            if (assembly == null)
                return DisplayNull;

            string location = null;

            try
            {
                location = assembly.Location;
            }
            catch (NotSupportedException)
            {
                // do nothing.
            }

            if (location == null)
                location = DisplayNull;

            string codeBase = assembly.CodeBase;

            if (codeBase == null)
                codeBase = DisplayNull;

            return String.Format(
                "[{0}, {1}, {2}, {3}]", assembly.FullName,
                AssemblyOps.GetModuleVersionId(assembly),
                location, codeBase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AssemblyName(
            AssemblyName assemblyName,
            long id,
            bool paths,
            bool wrap
            )
        {
            StringList list = null;

            if (assemblyName != null)
            {
                list = new StringList();
                list.Add(assemblyName.FullName);

                if (id != 0)
                    list.Add(id.ToString());

                if (paths)
                    list.Add(assemblyName.CodeBase);
            }

            if (wrap)
                return WrapOrNull(list);
            else if (list != null)
                return list.ToString();
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AssemblyName(
            Assembly assembly,
            long id,
            bool paths,
            bool wrap
            )
        {
            StringList list = null;

            if (assembly != null)
            {
                list = new StringList();
                list.Add(assembly.FullName);

                list.Add(AssemblyOps.GetModuleVersionId(
                    assembly).ToString());

                if (id != 0)
                    list.Add(id.ToString());

                if (paths)
                {
                    try
                    {
                        list.Add(assembly.Location);
                    }
                    catch (NotSupportedException)
                    {
                        list.Add((string)null);
                    }

                    list.Add(assembly.CodeBase);
                }
            }

            if (wrap)
                return WrapOrNull(list);
            else if (list != null)
                return list.ToString();
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string EventName(
            Interpreter interpreter,
            string prefix,
            string name,
            long id
            )
        {
            //
            // BUGFIX: We need to make 100% sure that event names are unique
            //         throughout the entire system.  Therefore, format them
            //         with some information that uniquely identifies this
            //         process, thread, application domain, and an ever
            //         increasing value (i.e. "event serial number") that is
            //         unique within this application domain (i.e. regardless
            //         of how many interpreters exist).
            //
            return Id(
                prefix, name, ProcessOps.GetId().ToString(),
                GlobalState.GetCurrentSystemThreadId().ToString(),
                AppDomainOps.GetCurrentId().ToString(),
                (interpreter != null) ?
                    interpreter.IdNoThrow.ToString() : null,
                Interlocked.Increment(ref nextEventId).ToString(),
                (id != 0) ? id.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeRemoveMethodName(
            string methodName, /* in */
            bool method,       /* in */
            ref string message /* in, out */
            )
        {
            //
            // HACK: Remove the duplicate "MethodName: " prefix from the
            //       message if using the real class and method names are
            //       enabled.  It will only be removed if it is at the
            //       start of the message.
            //
            if (!String.IsNullOrEmpty(message) &&
                method && !String.IsNullOrEmpty(methodName))
            {
                string[] parts = methodName.Split(Type.Delimiter);

                if (parts != null)
                {
                    int length = parts.Length;

                    if (length >= 1)
                    {
                        string part = parts[length - 1];

                        if (!String.IsNullOrEmpty(part))
                        {
                            //
                            // HACK: This takes advantage of the consistent
                            //       formatting of trace messages throughout
                            //       the core library and may not work with
                            //       external code.
                            //
                            if (message.StartsWith(part + ": ",
                                    StringOps.SystemStringComparisonType))
                            {
                                message = message.Substring(part.Length + 2);
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Great care must be taken in this method because it is directly
        //       called by DebugTrace, which is used everywhere.  Accessing the
        //       interpreter requires a lock and a try/catch block.
        //
        public static string TraceInterpreter(
            Interpreter interpreter
            )
        {
            //
            // NOTE: If there is no interpreter, just return a value suitable
            //       for displaying "null".
            //
            if (interpreter == null)
                return DisplayNull;

            //
            // NOTE: The interpreter may have been disposed and we do not want
            //       to throw an exception; therefore, wrap all the interpreter
            //       property access in a try block.
            //
            bool locked = false;

            try
            {
                interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                if (locked) /* TRANSACTIONAL */
                {
                    if (interpreter.Disposed)
                        return DisplayDisposed;

                    return interpreter.Id.ToString();
                }
                else
                {
                    return DisplayBusy;
                }
            }
            catch (Exception e)
            {
                Type type = (e != null) ? e.GetType() : null;

                return String.Format(DisplayError,
                    (type != null) ? type.Name : UnknownTypeName);
            }
            finally
            {
                interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceOutput(
            string format,
            DateTime? dateTime,
            TracePriority? priority,
            AppDomain appDomain,
            Interpreter interpreter,
            int? threadId,
            string message,
            bool method
            )
        {
            string methodName = null;

            return TraceOutput(
                format, dateTime, priority, appDomain, interpreter,
                threadId, message, method, ref methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceOutput(
            string format,
            DateTime? dateTime,
            TracePriority? priority,
            AppDomain appDomain,
            Interpreter interpreter,
            int? threadId,
            string message,
            bool method,
            ref string methodName
            )
        {
            string displayMethodName;

            if (method)
            {
                methodName = DebugOps.GetMethodName(
                    0, skipNames, true, false, null);

                if (methodName != null)
                    displayMethodName = methodName;
                else
                    displayMethodName = DisplayUnknown;
            }
            else
            {
                displayMethodName = DisplayNull;
            }

            MaybeRemoveMethodName(methodName, method, ref message);

            return String.Format(format, (dateTime != null) ?
                Iso8601TraceDateTime((DateTime)dateTime) : DisplayNull,
                (priority != null) ? HexadecimalPrefix +
                EnumOps.ToUIntOrULong(priority.Value).ToString(
                TracePriorityFormat) : DisplayNull, (appDomain != null) ?
                AppDomainOps.GetId(appDomain).ToString() : DisplayNull,
                TraceInterpreter(interpreter), (threadId != null) ?
                threadId.ToString() : DisplayNull, displayMethodName,
                message, Environment.NewLine);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceException(
            Exception exception
            )
        {
            return String.Format("{0}", exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ExceptionMethod(
            Exception exception,
            bool display
            )
        {
            if (exception == null)
                return display ? DisplayNull : null;

            try
            {
                MethodBase methodBase = exception.TargetSite;

                if (methodBase == null)
                    return display ? DisplayNull : null;

                return String.Format("{0}{1}{2}",
                    methodBase.ReflectedType, Type.Delimiter, methodBase.Name);
            }
            catch /* NOTE: Type from different AppDomain, perhaps? */
            {
                return display ? DisplayError : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Id(
            string prefix,
            string name,
            long id
            )
        {
            return Id(prefix, name, (id != 0) ? id.ToString() : null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Id(
            string prefix,
            string name,
            string id
            )
        {
            return Id(prefix, name, id, null, null, null, null, null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Id(
            string prefix,
            string name,
            string id1,
            string id2,
            string id3,
            string id4,
            string id5,
            string id6
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(prefix))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(prefix);
            }

            if (!String.IsNullOrEmpty(name))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(name);
            }

            if (!String.IsNullOrEmpty(id1))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id1);
            }

            if (!String.IsNullOrEmpty(id2))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id2);
            }

            if (!String.IsNullOrEmpty(id3))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id3);
            }

            if (!String.IsNullOrEmpty(id4))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id4);
            }

            if (!String.IsNullOrEmpty(id5))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id5);
            }

            if (!String.IsNullOrEmpty(id6))
            {
                if (result.Length > 0)
                    result.Append(Characters.NumberSign);

                result.Append(id6);
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string InteractiveLoopData(
            InteractiveLoopData loopData
            )
        {
            if (loopData == null)
                return DisplayNull;

            return loopData.ToTraceString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL
        public static string ShellCallbackData(
            ShellCallbackData callbackData
            )
        {
            if (callbackData == null)
                return DisplayNull;

            return callbackData.ToTraceString();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string InterpreterNoThrow(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return DisplayNull;

            return String.Format(
                "{0}{1}{0}", Characters.QuotationMark,
                interpreter.IdNoThrow);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string EnabledAndValue(
            bool enabled,
            string value
            )
        {
            return String.Format("{0} ({1})", enabled, value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && TCL_THREADS
        public static string WaitResult(
            int count,
            int index
            )
        {
            string result;

            if ((index >= _Constants.WaitResult.Object0) &&
                (index <= _Constants.WaitResult.Object0 + count - 1))
            {
                int offset = index - _Constants.WaitResult.Object0;

                if ((offset >= (int)TclThreadEvent.DoneEvent) &&
                    (offset <= (int)TclThreadEvent.QueueEvent))
                {
                    return String.Format(
                        "Object({0})", (TclThreadEvent)offset);
                }

                return String.Format("Object(#{0})", offset);
            }
            else if ((index >= _Constants.WaitResult.Abandoned0) &&
                (index <= _Constants.WaitResult.Abandoned0 + count - 1))
            {
                int offset = index - _Constants.WaitResult.Abandoned0;

                if ((offset >= (int)TclThreadEvent.DoneEvent) &&
                    (offset <= (int)TclThreadEvent.QueueEvent))
                {
                    return String.Format(
                        "Abandoned({0})", (TclThreadEvent)offset);
                }

                return String.Format("Abandoned(#{0})", offset);
            }
            else if (index == _Constants.WaitResult.IoCompletion)
            {
                result = "IoCompletion";
            }
            else if (index == _Constants.WaitResult.Timeout)
            {
                result = "Timeout";
            }
            else if (index == _Constants.WaitResult.Failed)
            {
                result = "Failed";
            }
#if MONO || MONO_HACKS
            else if (index == _Constants.WaitResult.MonoFailed)
            {
                result = "MonoFailed";
            }
#endif
            else
            {
                result = String.Format("Unknown({0})", index);
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string OperatorName(
            string name
            )
        {
            return DisplayString(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string OperatorName(
            string name,
            Lexeme lexeme
            )
        {
            return String.Format(
                "{0} ({1})", DisplayString(name, true), lexeme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
        public static string DatabaseObjectName(
            object @object,
            string @default,
            long id
            )
        {
            if (@object != null)
            {
                Type type = @object.GetType();

                if (type != null)
                {
                    return Id(type.ToString().Replace(
                        Type.Delimiter, Characters.NumberSign), null, id);
                }
            }

            return Id(@default, null, id);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSystemAssembly(Type type)
        {
            if (type == null)
                return false;

            Assembly assembly = type.Assembly;

            //
            // NOTE: Check if the type is in the assembly "mscorlib.dll".
            //
            if (Object.ReferenceEquals(assembly, typeof(object).Assembly))
                return true;

            //
            // NOTE: Check if the type is in the assembly "System.dll".
            //
            if (Object.ReferenceEquals(assembly, typeof(Uri).Assembly))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSameAssembly(Type type)
        {
            return (type != null) && GlobalState.IsAssembly(type.Assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ObjectHandleTypeName(
            Type type,
            bool full
            )
        {
            return (type != null) ? (full ? type.FullName : type.Name) : UnknownTypeName;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ObjectHandle(
            string prefix,
            string name,
            long id
            )
        {
            return Id(prefix, (name != null) ?
                name.Replace(Type.Delimiter, Characters.NumberSign) : null, id);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedAndOrFullName(
            Type type,
            bool fullName,
            bool qualified,
            bool display
            )
        {
            if (type == null)
                return display ? DisplayNull : String.Empty;

            if (fullName && qualified)
            {
                if (type.AssemblyQualifiedName != null)
                    return type.AssemblyQualifiedName;
                if (type.Assembly != null)
                    return String.Format("{0}, {1}", type.FullName, type.Assembly);
                else
                    return type.FullName;
            }
            else if (fullName)
            {
                return type.FullName;
            }
            else if (qualified && (type.Assembly != null))
            {
                return String.Format("{0}, {1}", type.Name, type.Assembly);
            }
            else
            {
                return type.Name;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedName(
            Type type
            )
        {
            if ((type != null) && (type.AssemblyQualifiedName != null))
                return type.AssemblyQualifiedName;
            if ((type != null) && (type.Assembly != null))
                return String.Format("{0}, {1}", type, type.Assembly);
            else if (type != null)
                return type.ToString();
            else
                return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedName(
            AssemblyName assemblyName,
            string typeName,
            bool full
            )
        {
            if ((assemblyName != null) && !String.IsNullOrEmpty(typeName))
                return String.Format("{0}, {1}", typeName,
                    full ? assemblyName.FullName : assemblyName.Name);
            else if (assemblyName != null)
                return full ? assemblyName.FullName : assemblyName.Name;
            else if (!String.IsNullOrEmpty(typeName))
                return typeName;
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string QualifiedName(
            string parentName,
            string childName
            )
        {
            if (!String.IsNullOrEmpty(parentName) && !String.IsNullOrEmpty(childName))
                return String.Format("{0}{1}{2}", parentName, Type.Delimiter, childName);
            else if (!String.IsNullOrEmpty(parentName))
                return parentName;
            else if (!String.IsNullOrEmpty(childName))
                return childName;
            else
                return String.Empty;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateMethodName(
            string typeName,
            string methodName
            )
        {
            return QualifiedName(typeName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateMethodName(
            Delegate @delegate,
            bool assembly,
            bool display
            )
        {
            if (@delegate == null)
                return display ? DisplayNull : null;

            MethodBase methodBase = @delegate.Method;

            return DelegateMethodName(methodBase, assembly, display);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateMethodName(
            MethodBase methodBase,
            bool assembly,
            bool display
            )
        {
            if (methodBase == null)
                return display ? DisplayNull : null;

            return DelegateMethodName(
                methodBase.DeclaringType, methodBase.Name, assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string DelegateMethodName(
            Type type,
            string methodName,
            bool assembly
            )
        {
            if ((type == null) ||
                (type == typeof(Interpreter)))
            {
                return QualifiedName((string)null, methodName);
            }
#if DATA
            else if (type == typeof(DatabaseVariable))
            {
                return QualifiedName(type.Name, methodName);
            }
#endif
            else if (!assembly && IsSameAssembly(type))
            {
                return QualifiedName(type.FullName, methodName);
            }
            else
            {
                return StringList.MakeList(
                    (type.Assembly != null) ? type.Assembly.FullName : null,
                    type.FullName, methodName);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ArgumentName(
            int position,
            string name
            )
        {
            return String.Format(
                "{0}{1} {2}{3}{2}", Characters.NumberSign,
                position, Characters.QuotationMark, name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MaybeEmitPolicyResults(
            PolicyWrapperDictionary allPolicies,
            PolicyWrapperDictionary failedPolicies,
            MethodFlags methodFlags,
            PolicyFlags policyFlags,
            string fileName,
            ReturnCode code,
            PolicyDecision decision,
            Result result
            )
        {
            bool success = PolicyOps.IsSuccess(code, decision);

            return String.Format(
                "MaybeEmitPolicyResults: {0} --> {1}, methodFlags = {2}, " +
                "policyFlags = {3}, fileName = {4}, decision = {5}, " +
                "code = {6}, result = {7}", success ? "SUCCESS" : "FAILURE",
                success ? FormatOps.WrapOrNull(allPolicies) : FormatOps.WrapOrNull(failedPolicies),
                FormatOps.WrapOrNull(methodFlags), FormatOps.WrapOrNull(policyFlags),
                FormatOps.WrapOrNull(fileName), decision, code, FormatOps.WrapOrNull(result));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PolicyDelegateName(
            Delegate @delegate
            )
        {
            if (@delegate == null)
                return null;

            IScriptPolicy policy = @delegate.Target as IScriptPolicy;

            if (policy != null)
            {
                return String.Format(
                    "{0} {1}{1}{2} {3}", TypeName(policy.GetType()),
                    Characters.MinusSign, Characters.GreaterThanSign,
                    TypeName(policy.CommandType));
            }

            MethodBase methodInfo = @delegate.Method;

            if (methodInfo == null)
                return null;

            return MethodName(
                TypeName(methodInfo.DeclaringType), methodInfo.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TraceDelegateName(
            Delegate @delegate
            )
        {
            if (@delegate == null)
                return null;

            MethodBase methodInfo = @delegate.Method;

            if (methodInfo == null)
                return null;

            return MethodName(
                TypeName(methodInfo.DeclaringType), methodInfo.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DelegateName(
            Delegate @delegate
            )
        {
            if (@delegate == null)
                return null;

            MethodInfo methodInfo = @delegate.Method;

            if (methodInfo == null)
                return null;

            return MethodName(
                TypeName(methodInfo.DeclaringType), methodInfo.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodQualifiedName(
            Type type,
            string methodName
            )
        {
            string typeName = null;

            if (!IsSameAssembly(type))
                typeName = (type != null) ? type.Name : null;

            return MethodName(typeName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodQualifiedFullName(
            Type type,
            string methodName
            )
        {
            string typeName = null;

            if (!IsSameAssembly(type))
                typeName = (type != null) ? type.FullName : null;

            return MethodName(typeName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodFullName(
            Type type,
            string methodName
            )
        {
            return MethodName((type != null) ? type.FullName : null, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodName(
            Type type,
            string methodName
            )
        {
            return MethodName((type != null) ? type.Name : null, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MethodName(
            string objectName,
            string methodName
            )
        {
            return QualifiedName(objectName, methodName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MethodOverload(
            int index,
            string objectName,
            string methodName
            )
        {
            if (index == Index.Invalid)
            {
                return String.Format(
                    "{0}{1}", Characters.QuotationMark,
                    QualifiedName(objectName, methodName));
            }

            return String.Format(
                "{0}{1} {2}{3}{2}", Characters.NumberSign, index,
                Characters.QuotationMark, QualifiedName(objectName,
                methodName));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayAppDomain()
        {
            return DisplayAppDomain(AppDomainOps.GetCurrent());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string DisplayAppDomain(
            AppDomain appDomain
            )
        {
            if (appDomain != null)
            {
                try
                {
                    StringBuilder result = StringOps.NewStringBuilder();

                    result.AppendFormat(
                        "[id = {0}, default = {1}]",
                        AppDomainOps.GetId(appDomain),
                        AppDomainOps.IsDefault(appDomain));

                    return result.ToString();
                }
                catch (Exception e)
                {
                    Type type = (e != null) ? e.GetType() : null;

                    return String.Format(DisplayError,
                        (type != null) ? type.Name : UnknownTypeName);
                }
            }

            return DisplayNull;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AppDomainFriendlyName(
            string fileName,
            string typeName
            )
        {
            return StringList.MakeList(fileName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AppDomainFriendlyName(
            AssemblyName assemblyName,
            string typeName
            )
        {
            string result;

            if (assemblyName != null)
            {
                if (typeName != null)
                {
                    result = Assembly.CreateQualifiedName(
                        assemblyName.ToString(), typeName);
                }
                else
                {
                    result = assemblyName.ToString();
                }
            }
            else
            {
                if (typeName != null)
                {
                    result = typeName;
                }
                else
                {
                    result = String.Empty;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginName(
            string assemblyName,
            string typeName
            )
        {
            // return QualifiedName(assemblyName, typeName);
            return Assembly.CreateQualifiedName(assemblyName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginSimpleName(
            IPluginData pluginData
            )
        {
            if (pluginData == null)
                return null;

            AssemblyName assemblyName;

#if ISOLATED_PLUGINS
            if (AppDomainOps.IsIsolated(pluginData))
            {
                assemblyName = pluginData.AssemblyName;
            }
            else
#endif
            {
                Assembly assembly = pluginData.Assembly;

                if (assembly == null)
                    return null;

                assemblyName = assembly.GetName();
            }

            if (assemblyName == null)
                return null;

            return assemblyName.Name;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginCommand(
            Assembly assembly,
            string pluginName,
            Type type,
            string typeName
            )
        {
            AssemblyName assemblyName = (assembly != null) ? assembly.GetName() : null;

            return String.Format(
                "{0}{1}{2}",
                (assemblyName != null) ? assemblyName.Name : pluginName,
                Characters.Underscore,
                (type != null) ? type.Name : typeName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string PluginAbout(
            IPluginData pluginData,
            bool full
            )
        {
            if (pluginData != null)
            {
                Type type;

                try
                {
                    type = pluginData.GetType(); /* throw */
                }
                catch
                {
                    type = null;
                }

                string appDomainId;

                try
                {
                    AppDomain appDomain = pluginData.AppDomain; /* throw */

                    if (appDomain != null)
                        appDomainId = AppDomainOps.GetId(appDomain).ToString();
                    else
                        appDomainId = DisplayNull;
                }
                catch
                {
                    appDomainId = DisplayUnknown;
                }

                return String.Format(
                    "{0}{1}{2}{3}{4} v{5} ({6})", Characters.HorizontalTab,
                    RuntimeOps.PluginFlagsToPrefix(pluginData.Flags),
                    Path.GetFileNameWithoutExtension(pluginData.FileName),
                    Type.Delimiter, TypeNameOrFullName(type, full),
                    pluginData.Version, appDomainId);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This algorithm was shamelessly stolen from
        //       Kevin B. Kenny's [clock] command implementation
        //       in Tcl 8.5.
        //
        private static string Stardate(
            DateTime value
            ) // COMPAT: Tcl
        {
            return String.Format(StardateOutputFormat,
                value.Year - Roddenberry,
                ((value.DayOfYear - 1) * 1000) / TimeOps.DaysInYear(value.Year),
                Characters.Period,
                (TimeOps.WholeSeconds(value) %
                    TimeOps.SecondsInNormalDay) / (TimeOps.SecondsInNormalDay / 10));
        }
    }
}
