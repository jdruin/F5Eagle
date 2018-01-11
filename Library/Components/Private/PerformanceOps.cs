/*
 * PerformanceOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;

#if NATIVE
using System.Security;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
#if NATIVE
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("4e5f0e33-37e0-4d99-a39f-6d0a87ebc487")]
    internal static class PerformanceOps
    {
#if NATIVE
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("53eaca2a-6ad1-4373-b541-8decca686521")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryPerformanceCounter(out long count);

            [DllImport(DllName.Kernel32, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryPerformanceFrequency(out long frequency);
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static bool isWindows = PlatformOps.IsWindowsOperatingSystem();
#endif

#if !NATIVE || !WINDOWS
        private const long MillisecondsPerSecond = 1000;
#endif

        private const long MicrosecondsPerMillisecond = 1000;
        private const long MicrosecondsPerSecond = 1000000;

        private static readonly long CountsPerSecond = GetCountsPerSecond();

        #region Dead Code
#if DEAD_CODE
#if !NATIVE || !WINDOWS
        private static readonly double CountsPerMillisecond =
            (double)CountsPerSecond / (double)MillisecondsPerSecond;
#endif
#endif
        #endregion

        private static readonly double CountsPerMicrosecond =
            (double)CountsPerSecond / (double)MicrosecondsPerSecond;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method determines if the specified value is valid for
        //       a quantity that represents a given duration (e.g. seconds,
        //       milliseconds, or microseconds, etc).  Negative and/or null
        //       values are never valid.  When the "integer" flag is true,
        //       the value must also fit within a signed 32-bit integer.
        //
        private static bool IsValidValue(
            long? value, /* NOTE: Seconds, milliseconds, microseconds, etc. */
            bool integer /* NOTE: When true, value must fit into an Int32. */
            )
        {
            if (value == null)
                return false;

            long localValue = (long)value;

            if (localValue < 0) /* NOTE: Negative duration? */
                return false;

            if (integer && (localValue > int.MaxValue))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetMicroseconds(
            int milliseconds,
            int? minimumMicroseconds,
            int? maximumMicroseconds
            ) /* LOSSY */
        {
            long result = GetMicroseconds(milliseconds);

            if (IsValidValue(minimumMicroseconds, true))
            {
                int localMinimumMicroseconds = (int)minimumMicroseconds;

                if (!IsValidValue(result, true) ||
                    (result < localMinimumMicroseconds))
                {
                    result = localMinimumMicroseconds;
                }
            }

            if (IsValidValue(maximumMicroseconds, true))
            {
                int localMaximumMicroseconds = (int)maximumMicroseconds;

                if (!IsValidValue(result, true) ||
                    (result > localMaximumMicroseconds))
                {
                    result = localMaximumMicroseconds;
                }
            }

            if (!IsValidValue(result, true))
                result = 0;

            return ConversionOps.ToInt(result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetMicroseconds(
            long milliseconds
            )
        {
            return milliseconds * MicrosecondsPerMillisecond;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static double GetMicroseconds(
            long startCount,
            long stopCount,
            long iterations
            )
        {
            return GetMicroseconds((stopCount - startCount), iterations);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static double GetMicroseconds(
            long count,
            long iterations
            )
        {
            double result;

            if (iterations <= 1)
                result = count / CountsPerMicrosecond;
            else
                result = (count / (double)iterations) / CountsPerMicrosecond;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetMilliseconds(
            long microseconds
            )
        {
            return microseconds / MicrosecondsPerMillisecond;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
#if !NATIVE || !WINDOWS
        private static double GetMilliseconds(
            long startCount,
            long stopCount,
            long iterations
            )
        {
            double result;

            if (iterations <= 1)
                result = (stopCount - startCount) / CountsPerMillisecond;
            else
                result = ((stopCount - startCount) / (double)iterations) / CountsPerMillisecond;

            return result;
        }
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetCountsPerSecond()
        {
            long result;

#if NATIVE && WINDOWS
            if (isWindows)
            {
                try
                {
                    if (!UnsafeNativeMethods.QueryPerformanceFrequency(out result))
                        result = 0;
                }
                catch
                {
                    result = 0;
                }
            }
            else
#endif
            {
                result = MicrosecondsPerSecond;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetIterations(
            long requestedIterations,
            long actualIterations,
            ReturnCode returnCode,
            bool breakOk
            )
        {
            //
            // NOTE: If the number of iterations requested is less than
            //       negative one, return the absolute value of it.
            //       Effectively, this allows the caller to use a specific
            //       overall divisor, even though only one actual iteration
            //       will take place.  This differs from what Tcl does (i.e.
            //       it treats all negative numbers the same as zero).
            //
            if (requestedIterations < Count.Invalid)
                return Math.Abs(requestedIterations);

            //
            // NOTE: If the number of iterations requested is negative one
            //       (i.e. "run forever until error"), return the number of
            //       iterations actually completed instead.  This differs
            //       from what Tcl does (i.e. it treats all negative numbers
            //       the same as zero).
            //
            if (requestedIterations == Count.Invalid)
                return actualIterations;

            //
            // NOTE: If the number of iterations requested is exactly zero,
            //       just return one.  This is used to measure the overhead
            //       associated with the [time] command infrastructure
            //       (COMPAT: Tcl).
            //
            if (requestedIterations == 0)
                return 1;

            //
            // NOTE: If the return code was Ok, the requested number of
            //       iterations should match the number of iterations
            //       actually completed.
            //
            if (returnCode == ReturnCode.Ok)
                return requestedIterations;

            //
            // NOTE: If the return code was Break, the requested number of
            //       iterations should only be used if the caller requests
            //       it.
            //
            if ((returnCode == ReturnCode.Break) && breakOk)
                return requestedIterations;

            //
            // NOTE: Otherwise, return the number of iterations actually
            //       completed.
            //
            return actualIterations;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetCount()
        {
            long result;

#if NATIVE && WINDOWS
            if (isWindows)
            {
                try
                {
                    if (!UnsafeNativeMethods.QueryPerformanceCounter(out result))
                        result = 0;
                }
                catch
                {
                    result = 0;
                }
            }
            else
#endif
            {
                //
                // BUGFIX: This result must be in microseconds because the various
                //         callers of this method assume that is the case when they
                //         calculate elapsed time.
                //
                result = Environment.TickCount * MicrosecondsPerMillisecond;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static double ElapsedMicroseconds(
            long startCount
            )
        {
            return GetMicroseconds(startCount, GetCount(), 1);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasElapsed(
            long startCount,
            long waitMicroseconds,
            long slopMicroseconds
            )
        {
            return ElapsedMicroseconds(startCount) +
                (double)slopMicroseconds >= (double)waitMicroseconds;
        }
    }
}
