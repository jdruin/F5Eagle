/*
 * StrongNameOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !NATIVE
#error "This file cannot be compiled or used properly with native code disabled."
#endif

using System;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("fc99d220-d288-43c5-b065-d7d8ac39303e")]
    internal static class StrongNameOps
    {
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("c02693d7-a96b-42e5-983e-b98c3d973771")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
#if !MONO
            [DllImport(DllName.MsCorEe,
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.U1)]
            internal static extern bool StrongNameSignatureVerificationEx(
                [MarshalAs(UnmanagedType.LPWStr)] string filePath,
                [MarshalAs(UnmanagedType.U1)] bool forceVerification,
                [MarshalAs(UnmanagedType.U1)] ref bool wasVerified
            );
#endif
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static bool IsStrongNameVerified(
            string fileName,
            bool force
            )
        {
            bool returnValue = false;
            bool verified = false;
            Result localError = null; /* NOT USED */

            if ((IsStrongNameVerified(
                    fileName, force, ref returnValue, ref verified,
                    ref localError) == ReturnCode.Ok) &&
                returnValue && verified)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ReturnCode IsStrongNameVerified(
            string fileName,
            bool force,
            ref bool returnValue,
            ref bool verified,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

#if WINDOWS && !MONO
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return ReturnCode.Error;
            }

            try
            {
                returnValue =
                    UnsafeNativeMethods.StrongNameSignatureVerificationEx(
                        fileName, force, ref verified);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }
#else
            error = "not implemented";
#endif

            TraceOps.DebugTrace(String.Format(
                "IsStrongNameVerified: file {0} verification " +
                "failure, force = {1}, returnValue = {2}, " +
                "verified = {3}, error = {4}",
                FormatOps.WrapOrNull(fileName), force,
                returnValue, verified, FormatOps.WrapOrNull(error)),
                typeof(SecurityOps).Name, TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion
    }
}
