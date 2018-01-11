/*
 * CertificateOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("acaf57c4-509f-4953-b43b-2222a40d6d33")]
    internal static class CertificateOps
    {
        #region Private Data
        internal static X509RevocationMode DefaultRevocationMode =
            X509RevocationMode.Online;

        internal static X509RevocationFlag DefaultRevocationFlag =
            X509RevocationFlag.ExcludeRoot;

        internal static X509VerificationFlags DefaultVerificationFlags =
            X509VerificationFlags.NoFlag;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Certificate Methods
        public static ReturnCode GetCertificate(
            string fileName,
            ref X509Certificate certificate,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(fileName))
            {
                try
                {
                    certificate = X509Certificate.CreateFromSignedFile(
                        fileName);

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid file name";
            }

#if DEBUG
            if (!PathOps.IsSameFile(
                    Interpreter.GetActive(), fileName,
                    GlobalState.GetAssemblyLocation()))
#endif
            {
                TraceOps.DebugTrace(String.Format(
                    "GetCertificate: file {0} query failure, error = {1}",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(CertificateOps).Name, TracePriority.SecurityError);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetCertificate2(
            string fileName,
            ref X509Certificate2 certificate2
            )
        {
            Result error = null;

            return GetCertificate2(fileName, ref certificate2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetCertificate2(
            string fileName,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            X509Certificate certificate = null;

            if (GetCertificate(fileName, ref certificate,
                    ref error) == ReturnCode.Ok)
            {
                if (certificate != null)
                {
                    try
                    {
                        certificate2 = new X509Certificate2(certificate);
                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "invalid certificate";
                }
            }

#if DEBUG
            if (!PathOps.IsSameFile(
                    Interpreter.GetActive(), fileName,
                    GlobalState.GetAssemblyLocation()))
#endif
            {
                TraceOps.DebugTrace(String.Format(
                    "GetCertificate2: file {0} query failure, error = {1}",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(CertificateOps).Name, TracePriority.SecurityError);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode VerifyChain(
            Assembly assembly,
            X509Certificate2 certificate2,
            X509VerificationFlags verificationFlags,
            X509RevocationMode revocationMode,
            X509RevocationFlag revocationFlag,
            ref Result error
            )
        {
            if (certificate2 != null)
            {
                try
                {
                    X509Chain chain = X509Chain.Create();

                    if (chain != null)
                    {
                        X509ChainPolicy chainPolicy = chain.ChainPolicy;

                        if (chainPolicy != null)
                        {
                            //
                            // NOTE: Setup the chain policy settings as specified
                            //       by the caller.
                            //
                            chainPolicy.VerificationFlags = verificationFlags;
                            chainPolicy.RevocationMode = revocationMode;
                            chainPolicy.RevocationFlag = revocationFlag;

                            if (chain.Build(certificate2))
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                StringList list = new StringList();

                                if (chain.ChainStatus != null)
                                {
                                    foreach (X509ChainStatus status in chain.ChainStatus)
                                    {
                                        list.Add(
                                            status.Status.ToString(),
                                            status.StatusInformation);
                                    }

                                    if (assembly != null)
                                        error = String.Format(
                                            "assembly {0}: {1}",
                                            FormatOps.WrapOrNull(assembly),
                                            list.ToString());
                                    else
                                        error = list;
                                }
                                else
                                {
                                    if (assembly != null)
                                        error = String.Format(
                                            "assembly {0}: invalid chain status",
                                            FormatOps.WrapOrNull(assembly));
                                    else
                                        error = "invalid chain status";
                                }
                            }
                        }
                        else
                        {
                            if (assembly != null)
                                error = String.Format(
                                    "assembly {0}: invalid chain policy",
                                    FormatOps.WrapOrNull(assembly));
                            else
                                error = "invalid chain policy";
                        }
                    }
                    else
                    {
                        if (assembly != null)
                            error = String.Format(
                                "assembly {0}: invalid chain",
                                FormatOps.WrapOrNull(assembly));
                        else
                            error = "invalid chain";
                    }
                }
                catch (Exception e)
                {
                    if (assembly != null)
                        error = String.Format(
                            "assembly {0}: {1}",
                            FormatOps.WrapOrNull(assembly), e);
                    else
                        error = e;
                }
            }
            else
            {
                if (assembly != null)
                    error = String.Format(
                        "assembly {0}: invalid certificate",
                        FormatOps.WrapOrNull(assembly));
                else
                    error = "invalid certificate";
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
