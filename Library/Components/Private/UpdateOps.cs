/*
 * UpdateOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using _PublicKey = Eagle._Components.Shared.PublicKey;

namespace Eagle._Components.Private
{
    [ObjectId("711b1e60-8516-4f41-ba61-89c48f904d0a")]
    internal static class UpdateOps
    {
        #region Private Static Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Public Key Data
        //
        // NOTE: This public key is one specifically reserved for updates to
        //       the core library.  It is logically constant and should not be
        //       changed (except to null, which will disable its use).  This
        //       is a "legacy" key (2048 bits).  It is trusted by the vast
        //       majority of published Eagle builds when checking for updates.
        //       In the future, newer builds of Eagle may start refusing to
        //       trust this key.
        //
        private static byte[] PublicKey1 = _PublicKey.SoftwareUpdate1;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        private static byte[] PublicKey2 = _PublicKey.SoftwareUpdate2;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        private static byte[] PublicKey3 = _PublicKey.SoftwareUpdate3;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is another specifically reserved for updates
        //       to the core library.  It is logically constant and should not
        //       be changed (except to null, which will disable its use).  This
        //       key is only recognized by builds of Eagle that are Beta 32 or
        //       later.
        //
        private static byte[] PublicKey4 = _PublicKey.SoftwareUpdate4;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This public key is RESERVED for use by third-party plugins
        //       and applications; however, it is not public because it is
        //       not intended to be used lightly.
        //
        private static byte[] PublicKey5 = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICertificatePolicy Support Data
        private static bool exclusive = false;

        ///////////////////////////////////////////////////////////////////////

#if !MONO
        private static bool useLegacyCertificatePolicy = false;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static ICertificatePolicy savedCertificatePolicy;
        private static bool haveSavedCertificatePolicy;

        ///////////////////////////////////////////////////////////////////////

        private static readonly ICertificatePolicy certificatePolicy =
            new CertificatePolicy();
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        public static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ExitLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Exclusive Mode Support Methods
        public static bool IsExclusive()
        {
            lock (syncRoot)
            {
                return exclusive;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetExclusive(
            bool exclusive,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool wasExclusive = IsExclusive();

                if (exclusive != wasExclusive)
                {
                    UpdateOps.exclusive = exclusive;

                    TraceOps.DebugTrace(String.Format(
                        "SetExclusive: exclusive mode {0}",
                        exclusive ? "enabled" : "disabled"),
                        typeof(UpdateOps).Name,
                        TracePriority.SecurityDebug);

                    return ReturnCode.Ok;
                }
                else
                {
                    error = String.Format(
                        "already {0}", exclusive ?
                            "exclusive" : "non-exclusive");
                }
            }

            TraceOps.DebugTrace(String.Format(
                "SetExclusive: exclusive = {0}, error = {1}",
                exclusive, FormatOps.WrapOrNull(error)),
                typeof(UpdateOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Trust Support Methods
        public static bool IsTrusted()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
#if MONO
                return IsLegacyCertificatePolicyActive();
#else
                if (!ShouldUseLegacyCertificatePolicy())
                    return IsServerCertificateValidationCallbackActive();
                else
                    return IsLegacyCertificatePolicyActive();
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetTrusted(
            bool trusted,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool wasTrusted = IsTrusted();

                if (trusted != wasTrusted)
                {
                    try
                    {
#if !MONO
                        if (!ShouldUseLegacyCertificatePolicy())
                        {
                            //
                            // NOTE: When using the .NET Framework, use the
                            //       newer certification validation callback
                            //       interface.
                            //
                            if (trusted)
                                AddServerCertificateValidationCallback();
                            else
                                RemoveServerCertificateValidationCallback();

                            TraceOps.DebugTrace(String.Format(
                                "SetTrusted: {0} " +
                                "RemoteCertificateValidationCallback",
                                trusted ? "added" : "removed"),
                                typeof(UpdateOps).Name,
                                TracePriority.SecurityDebug);
                        }
                        else
#endif
                        {
                            //
                            // NOTE: When running on Mono, fallback to the
                            //       "obsolete" CertificatePolicy property.
                            //
                            if (trusted)
                                EnableLegacyCertificatePolicy();
                            else
                                DisableLegacyCertificatePolicy();

                            TraceOps.DebugTrace(String.Format(
                                "SetTrusted: {0} CertificatePolicy",
                                trusted ? "overridden" : "restored"),
                                typeof(UpdateOps).Name,
                                TracePriority.SecurityDebug);
                        }

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = String.Format(
                        "already {0}", trusted ?
                            "trusted" : "untrusted");
                }
            }

            TraceOps.DebugTrace(String.Format(
                "SetTrusted: trusted = {0}, error = {1}",
                trusted, FormatOps.WrapOrNull(error)),
                typeof(UpdateOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region RemoteCertificateValidationCallback Support Methods
#if !MONO
        private static bool RemoteCertificateValidationCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Permit all X.509 certificates that are considered to
                //       to be valid by the platform itself (i.e. they do not
                //       have an error status).  If exclusive mode is enabled,
                //       this will be skipped.
                //
                if (!exclusive && (sslPolicyErrors == SslPolicyErrors.None))
                    return true;

                //
                // NOTE: If this ServerCertificateValidationCallback is being
                //       called when it should not be active, then it's not
                //       supposed to be "always trusted" right now; therefore,
                //       just return false.
                //
                if (!IsServerCertificateValidationCallbackActive())
                    return false;

                return IsTrustedCertificate(certificate);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsServerCertificateValidationCallbackActive()
        {
            return (ServicePointManager.ServerCertificateValidationCallback ==
                RemoteCertificateValidationCallback);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddServerCertificateValidationCallback()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                RemoteCertificateValidationCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void RemoveServerCertificateValidationCallback()
        {
            ServicePointManager.ServerCertificateValidationCallback -=
                RemoteCertificateValidationCallback;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICertificatePolicy Support Class & Methods
        #region ICertificatePolicy Support Class
        [ObjectId("4062e197-ed96-4db3-87e8-f463e5fb818b")]
        private sealed class CertificatePolicy : ICertificatePolicy
        {
            #region ICertificatePolicy Members
            public bool CheckValidationResult(
                ServicePoint srvPoint,
                X509Certificate certificate,
                WebRequest request,
                int certificateProblem
                )
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Unless exclusive mode is enabled, permit all
                    //       X.509 certificates that are considered to to
                    //       be valid by the platform itself (i.e. they do
                    //       not have an error status).
                    //
                    if (!exclusive && (certificateProblem == 0))
                        return true;

                    //
                    // NOTE: If this ICertificatePolicy is being called when
                    //       it should not be active, then it's not supposed
                    //       to be "always trusted" right now; therefore,
                    //       just return false.
                    //
                    if (!IsLegacyCertificatePolicyActive())
                        return false;

                    return IsTrustedCertificate(certificate);
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICertificatePolicy Support Methods
        private static bool IsLegacyCertificatePolicyActive()
        {
            return Object.ReferenceEquals(
                ServicePointManager.CertificatePolicy, certificatePolicy);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void EnableLegacyCertificatePolicy()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: First, save the current certificate policy for
                //       possible later restoration.
                //
                savedCertificatePolicy = ServicePointManager.CertificatePolicy;
                haveSavedCertificatePolicy = true;

                //
                // NOTE: Next, set the certificate policy to the one we
                //       use for software updates.
                //
                ServicePointManager.CertificatePolicy = certificatePolicy;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void DisableLegacyCertificatePolicy()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Restore the previously saved certificate policy,
                //       if any.
                //
                if (!haveSavedCertificatePolicy)
                    return;

                //
                // NOTE: Restore the saved ICertificatePolicy.
                //
                ServicePointManager.CertificatePolicy = savedCertificatePolicy;

                //
                // NOTE: Clear the saved ICertificatePolicy.
                //
                haveSavedCertificatePolicy = false;
                savedCertificatePolicy = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if !MONO
        private static bool ShouldUseLegacyCertificatePolicy()
        {
            lock (syncRoot)
            {
                return useLegacyCertificatePolicy ||
                    CommonOps.Runtime.IsMono();
            }
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Trusted Certificate Support Methods
        private static bool IsTrustedCertificate(
            X509Certificate certificate
            )
        {
            bool result = false;
            string name = null;

            //
            // NOTE: Make sure the certificate public key matches what
            //       we expect it to be for our own software updates.
            //
            if (certificate != null)
            {
                //
                // NOTE: Grab the public key of the certificate.
                //
                byte[] certificatePublicKey = certificate.GetPublicKey();

                if ((certificatePublicKey != null) &&
                    (certificatePublicKey.Length > 0))
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        //
                        // NOTE: Compare the public key of the certificate to
                        //       one(s) that we trust for our software updates.
                        //
                        if (!result &&
                            (PublicKey1 != null) && (PublicKey1.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey1))
                        {
                            name = "PublicKey1";
                            result = true;
                        }

                        if (!result &&
                            (PublicKey2 != null) && (PublicKey2.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey2))
                        {
                            name = "PublicKey2";
                            result = true;
                        }

                        if (!result &&
                            (PublicKey3 != null) && (PublicKey3.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey3))
                        {
                            name = "PublicKey3";
                            result = true;
                        }

                        if (!result &&
                            (PublicKey4 != null) && (PublicKey4.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey4))
                        {
                            name = "PublicKey4";
                            result = true;
                        }

                        //
                        // NOTE: Compare the public key of the certificate to
                        //       the auxiliary one that we trust for use by
                        //       third-party applications and plugins.
                        //
                        if (!result &&
                            (PublicKey5 != null) && (PublicKey5.Length > 0) &&
                            ArrayOps.Equals(certificatePublicKey, PublicKey5))
                        {
                            name = "PublicKey5";
                            result = true;
                        }
                    }
                }
            }

            //
            // NOTE: Report this trust result to any trace listeners.
            //
            TraceOps.DebugTrace(String.Format(
                "IsTrustedCertificate: certificate = {0}, name = {1}, " +
                "result = {2}", FormatOps.Certificate(certificate, false,
                true), FormatOps.WrapOrNull(name), result),
                typeof(UpdateOps).Name, TracePriority.SecurityDebug);

            return result;
        }
        #endregion
    }
}
