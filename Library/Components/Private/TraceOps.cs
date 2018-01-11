/*
 * TraceOps.cs --
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
using System.Diagnostics;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("6dd365ef-005a-4d33-8042-bf5b7d17153e")]
    internal static class TraceOps
    {
        #region Private Constants
        //
        // NOTE: The various trace formats shared between this class and the
        //       FormatOps class.  The passed format arguments are always the
        //       same; therefore, we just omit the ones we do not need for a
        //       particular format.
        //
        private static readonly string MinimumTraceFormat = "{6}{7}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string MediumTraceFormat = "{4}: {6}{7}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string MaximumTraceFormat =
            "[d:{0}] [p:{1}] [a:{2}] [i:{3}] [t:{4}] [m:{5}]: {6}{7}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string DefaultTraceFormat = MediumTraceFormat;

        ///////////////////////////////////////////////////////////////////////

        private static readonly string[] TraceFormats = {
            DefaultTraceFormat,
            MinimumTraceFormat,
            MediumTraceFormat,
            MaximumTraceFormat
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only; however, they should not
        //       need to be changed.  Instead, the associated methods in this
        //       class can be called.
        //
        private static TracePriority DefaultTracePriority =
            TracePriority.Default;

        private static TracePriority DefaultTracePriorities =
            TracePriority.DefaultMask;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data (MUST BE DONE PRIOR TO TOUCHING GlobalState)
        #region Synchronization Objects
        //
        // BUGFIX: This is used for synchronization inside the IsTraceEnabled
        //         method, which is used by the DebugTrace method, which is
        //         used during the initialization of the static GlobalState
        //         class; therefore, it must be initialized before anything
        //         that touches the GlobalState class.
        //
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tracing Support Data
        //
        // NOTE: This field helps determine what the IsTracePossible method
        //       will return.  If this field is zero, no trace handling of
        //       any kind will be performed, including the normal formatting
        //       and category checks, etc.
        //
        private static bool isTracePossible = true;

        //
        // NOTE: This field helps determine what the IsTracePossible method
        //       will return.  It is used to check if the specified trace
        //       priority matches this mask of enabled trace priorities.
        //
        private static TracePriority tracePriorities = DefaultTracePriorities;

        //
        // NOTE: This is the default trace priority value used when a method
        //       overload that lacks such a parameter is used.
        //
        private static TracePriority defaultTracePriority = DefaultTracePriority;

        //
        // NOTE: This field determines if core library tracing is enabled or
        //       disabled by default.  The value of this field is only used
        //       when initializing this subsystem and then only if both the
        //       NoTrace and Trace environment variables are not set [to
        //       anything].
        //
        // TODO: Good default?
        //
        private static bool isTraceEnabledByDefault = true;

        //
        // HACK: This is part of a hack that solves a chicken-and-egg problem
        //       with the diagnostic tracing method used by this library.  We
        //       allow tracing to be disabled via an environment variable
        //       and/or the shell command line.  Unfortunately, by the time we
        //       disable tracing, many messages will have typically already
        //       been written to the trace listeners.  To prevent this noise
        //       (that the user wishes to suppress), we internalize the check
        //       (i.e. we do it from inside the core trace method itself) and
        //       initialize this variable [once] with the result of checking
        //       the environment variable.
        //
        private static bool? isTraceEnabled = null;

        //
        // NOTE: This is the current trace format.  Normally, this is set to
        //       one of the constants defined above; however, it can be set
        //       to any valid format string as long as out-of-bounds argument
        //       indexes are not used.  If this is set to null, trace writes
        //       will be disabled.
        //
        private static string traceFormat = DefaultTraceFormat;

        //
        // NOTE: When this value is non-zero, the formatted DateTime (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<null>" or similar.
        //
        private static bool traceDateTime = false;

        //
        // NOTE: When this value is non-zero, the trace priority value will be
        //       included in the trace output; otherwise, it will be replaced
        //       with the string "<null>" or similar.
        //
        private static bool tracePriority = true;

        //
        // NOTE: When this value is non-zero, the active application domain (if
        //       any) will be included in the trace output; otherwise, it will
        //       be replaced with the string "<null>" or similar.
        //
        private static bool traceAppDomain = false;

        //
        // NOTE: When this value is non-zero, the active interpreter (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        private static bool traceInterpreter = false;

        //
        // NOTE: When this value is non-zero, the active thread (if any) will
        //       be included in the trace output; otherwise, it will be
        //       replaced with the string "<null>" or similar.
        //
        private static bool traceThreadId = true;

        //
        // NOTE: When this value is non-zero, the active method name (if any)
        //       will be included in the trace output; otherwise, it will be
        //       replaced with the string "<unknown>" or similar.
        //
        private static bool traceMethod = false;

        //
        // NOTE: This is the dictionary of trace categories that are currently
        //       "allowed".  If this dictionary is empty, all categories are
        //       considered to be "allowed"; otherwise, only those present in
        //       the dictionary with a non-zero associated value are "allowed".
        //       Any trace messages that are not "allowed" will be silently
        //       dropped.
        //
        private static IntDictionary traceCategories = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal State Introspection Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || isTracePossible)
                {
                    localList.Add("IsTracePossible",
                        isTracePossible.ToString());
                }

                if (empty || (tracePriorities != TracePriority.None))
                {
                    localList.Add("TracePriorities",
                        tracePriorities.ToString());
                }

                if (empty || (defaultTracePriority != TracePriority.None))
                {
                    localList.Add("DefaultTracePriority",
                        defaultTracePriority.ToString());
                }

                if (empty || isTraceEnabledByDefault)
                {
                    localList.Add("IsTraceEnabledByDefault",
                        isTraceEnabledByDefault.ToString());
                }

                if (empty || (isTraceEnabled != null))
                {
                    localList.Add("IsTraceEnabled", (isTraceEnabled != null) ?
                        isTraceEnabled.ToString() : FormatOps.DisplayNull);
                }

                if (empty || (traceFormat != null))
                {
                    localList.Add("TraceFormat",
                        FormatOps.DisplayString(traceFormat));
                }

                if (empty || traceDateTime)
                {
                    localList.Add("TraceDateTime",
                        traceDateTime.ToString());
                }

                if (empty || tracePriority)
                {
                    localList.Add("TracePriority",
                        tracePriority.ToString());
                }

                if (empty || traceAppDomain)
                {
                    localList.Add("TraceAppDomain",
                        traceAppDomain.ToString());
                }

                if (empty || traceInterpreter)
                {
                    localList.Add("TraceInterpreter",
                        traceInterpreter.ToString());
                }

                if (empty || traceThreadId)
                {
                    localList.Add("TraceThreadId",
                        traceThreadId.ToString());
                }

                if (empty || traceMethod)
                {
                    localList.Add("TraceMethod",
                        traceMethod.ToString());
                }

                if (empty || (traceCategories != null))
                {
                    localList.Add("TraceCategories", (traceCategories != null) ?
                        traceCategories.KeysAndValuesToString(null, false) :
                        FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Trace Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tracing Support Methods
        private static bool IsTracePossible()
        {
            /* NO-LOCK */
            return isTracePossible && !AppDomainOps.IsCurrentFinalizing();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is part of a hack that solves a chicken-and-egg problem
        //       with the diagnostic tracing method used by this library.  We
        //       allow tracing to be disabled via an environment variable
        //       and/or the shell command line.  Unfortunately, by the time we
        //       disable tracing, many messages will have typically already
        //       been written to the trace listeners.  To prevent this noise
        //       (that the user wishes to suppress), we internalize the check
        //       (i.e. we do it from inside the core trace method itself) and
        //       initialize this variable [once] with the result of checking
        //       the environment variable.
        //
        private static bool IsTraceEnabled(
            TracePriority priority,
            params string[] categories
            )
        {
            bool result;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (isTraceEnabled == null)
                {
                    //
                    // NOTE: Cannot use the GlobalConfiguration.GetValue
                    //       method at this point because that method may
                    //       call into the DebugTrace method (below), which
                    //       calls this method.
                    //
                    if (CommonOps.Environment.DoesVariableExist(
                            EnvVars.NoTrace))
                    {
                        isTraceEnabled = false;
                    }
                    else if (CommonOps.Environment.DoesVariableExist(
                            EnvVars.Trace))
                    {
                        isTraceEnabled = true;
                    }
                    else
                    {
                        isTraceEnabled = isTraceEnabledByDefault;
                    }
                }

                //
                // NOTE: Determine if tracing is globally enabled or disabled.
                //
                result = (bool)isTraceEnabled;

                //
                // NOTE: If tracing has been globally disabled, do not bother
                //       checking any categories.
                //
                if (result)
                {
                    //
                    // NOTE: The priority flags specified by the caller must
                    //       all be present in the configured trace priority
                    //       flags.
                    //
                    if (!FlagOps.HasFlags(tracePriorities, priority, true))
                    {
                        result = false;
                    }
                    else
                    {
                        //
                        // NOTE: If the caller specified a null category -OR-
                        //       there are no trace categories specifically
                        //       enabled (i.e. all trace categories are
                        //       allowed), always allow the message through.
                        //
                        if ((categories != null) &&
                            (categories.Length > 0) &&
                            (traceCategories != null) &&
                            (traceCategories.Count > 0))
                        {
                            //
                            // NOTE: At this point, at least one of the
                            //       specified trace categories, if any,
                            //       must exist in the dictionary and its
                            //       associated value must be non-zero;
                            //       otherwise, the trace message is not
                            //       allowed through.
                            //
                            bool found = false;

                            foreach (string category in categories)
                            {
                                if (category == null)
                                    continue;

                                int value;

                                if (traceCategories.TryGetValue(
                                        category, out value) &&
                                    (value != 0))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                                result = false;
                        }
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetTraceEnabled(
            bool enabled
            )
        {
            lock (syncRoot)
            {
                isTraceEnabled = enabled;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetTraceEnabled()
        {
            lock (syncRoot)
            {
                isTraceEnabled = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static TracePriority GetTracePriorities()
        {
            lock (syncRoot)
            {
                return tracePriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AdjustTracePriorities(
            TracePriority priority,
            bool enabled
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (enabled)
                    tracePriorities |= priority;
                else
                    tracePriorities &= ~priority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetTracePriorities()
        {
            lock (syncRoot)
            {
                tracePriorities = DefaultTracePriorities;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static TracePriority GetTracePriority()
        {
            lock (syncRoot)
            {
                return defaultTracePriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetTracePriority(
            TracePriority priority
            )
        {
            lock (syncRoot)
            {
                defaultTracePriority = priority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetTracePriority()
        {
            lock (syncRoot)
            {
                defaultTracePriority = DefaultTracePriority;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static IEnumerable<string> ListTraceCategories()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (traceCategories == null)
                    return null;

                StringList categories = new StringList();

                foreach (KeyValuePair<string, int> pair in traceCategories)
                    categories.Add(pair.Key, pair.Value.ToString());

                return categories;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetTraceCategories(
            IEnumerable<string> categories,
            int value
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the dictionary of "allowed" trace categories has
                //       not been created yet, do so now.
                //
                if (traceCategories == null)
                    traceCategories = new IntDictionary();

                //
                // NOTE: If there are no trace categories specified, the trace
                //       category dictionary may be created; however, it will
                //       not be added to.
                //
                if (categories != null)
                {
                    foreach (string category in categories)
                    {
                        //
                        // NOTE: Skip null categories.
                        //
                        if (category == null)
                            continue;

                        //
                        // NOTE: Add or modify the trace category.
                        //
                        traceCategories[category] = value;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void UnsetTraceCategories(
            IEnumerable<string> categories
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the dictionary of "allowed" trace categories has
                //       not been created yet, do so now.
                //
                if (traceCategories == null)
                    traceCategories = new IntDictionary();

                //
                // NOTE: If there are no trace categories specified, the trace
                //       category dictionary may be created; however, it will
                //       not be removed from.
                //
                if (categories != null)
                {
                    foreach (string category in categories)
                    {
                        //
                        // NOTE: Skip null categories.
                        //
                        if (category == null)
                            continue;

                        //
                        // NOTE: Remove the trace category.
                        //
                        traceCategories.Remove(category);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ClearTraceCategories()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (traceCategories != null)
                    traceCategories.Clear();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetTraceCategories()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (traceCategories != null)
                {
                    traceCategories.Clear();
                    traceCategories = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetTraceFormat()
        {
            lock (syncRoot)
            {
                return traceFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetTraceFormat(
            int index
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (index < 0)
                {
                    traceFormat = null; /* DISABLE */
                    return true;
                }

                if ((TraceFormats != null) &&
                    (index >= 0) && (index < TraceFormats.Length))
                {
                    traceFormat = TraceFormats[index];
                    return true;
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetMinimumTraceFormat()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (TraceFormats != null)
                {
                    int length = TraceFormats.Length;

                    if (length > 0)
                        return SetTraceFormat(0);
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetMaximumTraceFormat()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (TraceFormats != null)
                {
                    int length = TraceFormats.Length;

                    if (length > 0)
                        return SetTraceFormat(length - 1);
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetTraceFormatFlags(
            out bool zero,
            out bool one,
            out bool two,
            out bool three,
            out bool four,
            out bool five
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                zero = traceDateTime;
                one = tracePriority;
                two = traceAppDomain;
                three = traceInterpreter;
                four = traceThreadId;
                five = traceMethod;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetTraceFormatFlags(
            bool zero,
            bool one,
            bool two,
            bool three,
            bool four,
            bool five
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                traceDateTime = zero;
                tracePriority = one;
                traceAppDomain = two;
                traceInterpreter = three;
                traceThreadId = four;
                traceMethod = five;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [Conditional("DEBUG_WRITE")]
        public static void DebugWriteTo(
            Interpreter interpreter,
            string value,
            bool force
            )
        {
            if (!IsTracePossible())
                return;

            string traceFormat = GetTraceFormat();

            if (traceFormat == null)
                return;

            bool traceDateTime;
            bool tracePriority;
            bool traceAppDomain;
            bool traceInterpreter;
            bool traceThreadId;
            bool traceMethod;

            GetTraceFormatFlags(
                out traceDateTime, out tracePriority, out traceAppDomain,
                out traceInterpreter, out traceThreadId, out traceMethod);

            DebugOps.WriteTo(interpreter, FormatOps.TraceOutput(traceFormat,
                traceDateTime ? (DateTime?)TimeOps.GetNow() : null, null,
                traceAppDomain ? AppDomainOps.GetCurrent() : null,
                traceInterpreter ? interpreter : null, traceThreadId ?
                (int?)GlobalState.GetCurrentSystemThreadId() : null,
                value, traceMethod), force);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DebugTraceRaw(
            string message,
            string category,
            string methodName,
            TracePriority priority
            )
        {
            //
            // TODO: Redirect these writes to the active IHost, if any?
            //
            if (IsTraceEnabled(priority, category, methodName))
            {
                try
                {
                    DebugOps.TraceWrite(message, category); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            Exception exception,
            string category,
            TracePriority priority
            )
        {
            if (!IsTracePossible())
                return;

            if (!IsTraceEnabled(priority)) /* HACK: *PERF* Bail. */
                return;

            DebugTrace(
                GlobalState.GetCurrentSystemThreadId(),
                FormatOps.TraceException(exception), category, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            Exception exception,
            string category,
            string prefix,
            TracePriority priority
            )
        {
            if (!IsTracePossible())
                return;

            if (!IsTraceEnabled(priority)) /* HACK: *PERF* Bail. */
                return;

            DebugTrace(
                GlobalState.GetCurrentSystemThreadId(),
                String.Format("{0}{1}", prefix, FormatOps.TraceException(
                exception)), category, priority);
        }

        ///////////////////////////////////////////////////////////////////////

        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            string message,
            string category,
            TracePriority priority
            )
        {
            if (!IsTracePossible())
                return;

            if (!IsTraceEnabled(priority)) /* HACK: *PERF* Bail. */
                return;

            DebugTrace(
                GlobalState.GetCurrentSystemThreadId(), message, category,
                priority);
        }

        ///////////////////////////////////////////////////////////////////////

        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            int? threadId,
            Exception exception,
            string category,
            TracePriority priority
            )
        {
            if (!IsTracePossible())
                return;

            if (!IsTraceEnabled(priority)) /* HACK: *PERF* Bail. */
                return;

            DebugTrace(
                threadId, FormatOps.TraceException(exception), category,
                priority);
        }

        ///////////////////////////////////////////////////////////////////////

        [Conditional("DEBUG_TRACE")]
        public static void DebugTrace(
            int? threadId,
            string message,
            string category,
            TracePriority priority
            )
        {
            if (!IsTracePossible())
                return;

            if (!IsTraceEnabled(priority)) /* HACK: *PERF* Bail. */
                return;

            string traceFormat = GetTraceFormat();

            if (traceFormat == null)
                return;

            bool traceDateTime;
            bool tracePriority;
            bool traceAppDomain;
            bool traceInterpreter;
            bool traceThreadId;
            bool traceMethod;

            GetTraceFormatFlags(
                out traceDateTime, out tracePriority, out traceAppDomain,
                out traceInterpreter, out traceThreadId, out traceMethod);

            string methodName = null;

            DebugTraceRaw(FormatOps.TraceOutput(traceFormat,
                traceDateTime ? (DateTime?)TimeOps.GetNow() : null,
                tracePriority ? (TracePriority?)priority : null,
                traceAppDomain ? AppDomainOps.GetCurrent() : null,
                traceInterpreter ? Interpreter.GetActive() : null,
                traceThreadId ? threadId : null, message, traceMethod,
                ref methodName), category, methodName, priority);
        }
        #endregion
    }
}
