/*
 * CommonOps.cs --
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
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Eagle._Attributes;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("c385e1b9-95b0-4cd5-b0d9-a5fe582d7162")]
    internal static class CommonOps
    {
        #region Runtime Detection Support Class
        [ObjectId("e9622641-301b-4208-a5cc-3801edf4854e")]
        internal static class Runtime
        {
            #region Private Constants
            private static readonly string MonoRuntimeType = "Mono.Runtime";
            private static readonly string MonoDisplayNameMember = "GetDisplayName";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Create a list of regular expression patterns to check the
            //       Mono runtime version against.
            //
            private static readonly RegExList MonoVersionRegExList =
                new RegExList(new Regex[] {
                new Regex(" (\\d+(?:\\.\\d+)+)$", /* NOTE: Pre-2.6.0? */
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                new Regex("^(\\d+(?:\\.\\d+)+) ", /* NOTE: Post-2.6.0? */
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            });

            ///////////////////////////////////////////////////////////////////

            private static readonly string MonoRuntimeName = "Mono";
            private static readonly string MicrosoftRuntimeName = "Microsoft.NET";

            ///////////////////////////////////////////////////////////////////

            private static readonly string FrameworkSetup20KeyName =
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\NET Framework Setup\\NDP\\v2.0.50727";

            private static readonly string FrameworkSetup20ValueName = "Increment";

            private static readonly string FrameworkSetup40KeyName =
                "HKEY_LOCAL_MACHINE\\Software\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full";

            private static readonly string FrameworkSetup40ValueName = "Release";

            ///////////////////////////////////////////////////////////////////

#if NET_40
            ///////////////////////////////////////////////////////////////////
            //
            // NOTE: These values were verified against those listed on the
            //       MSDN page:
            //
            //       https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
            //
            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This value indicates the .NET Framework 4.5.  It was
            //       obtained from MSDN.
            //
            private static readonly int FrameworkSetup45Value = 378389; // >= indicates 4.5

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.5.1.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 8.1 value only applies to that
            //         exact version, not any higher versions.  This class
            //         obeys this assumption.
            //
            private static readonly int FrameworkSetup451Value = 378758; // >= indicates 4.5.1
            private static readonly int FrameworkSetup451OnWindows81Value = 378675; // >= indicates 4.5.1

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This value indicates the .NET Framework 4.5.2.  It was
            //       obtained from MSDN.
            //
            private static readonly int FrameworkSetup452Value = 379893; // >= indicates 4.5.2

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.6.  They were
            //       obtained from MSDN.
            //
            private static readonly int FrameworkSetup46Value = 393297; // >= indicates 4.6
            private static readonly int FrameworkSetup46OnWindows10Value = 393295; // >= indicates 4.6

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.6.1.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup461Value = 394271; // >= indicates 4.6.1
            private static readonly int FrameworkSetup461OnWindows10Value = 394254; // >= indicates 4.6.1

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.6.2.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup462Value = 394806; // >= indicates 4.6.2
            private static readonly int FrameworkSetup462OnWindows10Value = 394802; // >= indicates 4.6.2

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.7.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup47Value = 460805; // >= indicates 4.7
            private static readonly int FrameworkSetup47OnWindows10Value = 460798; // >= indicates 4.7

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These values indicate the .NET Framework 4.7.1.  They were
            //       obtained from MSDN.
            //
            // BUGBUG: Apparently, the Windows 10 value only applies to certain
            //         "updates" of the Windows 10 operating system, not RTM+.
            //         This class obeys this assumption.
            //
            private static readonly int FrameworkSetup471Value = 461310; // >= indicates 4.7.1
            private static readonly int FrameworkSetup471OnWindows10Value = 461308; // >= indicates 4.7.1
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            private static readonly object syncRoot = new object();

            ///////////////////////////////////////////////////////////////////

            private static bool? isMono = null;
            private static string frameworkExtraVersion = null;

            ///////////////////////////////////////////////////////////////////

            private static Version FrameworkVersion = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Runtime Detection Methods
            public static bool IsMono()
            {
                try
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (isMono == null)
                            isMono = (Type.GetType(MonoRuntimeType) != null);

                        return (bool)isMono;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            private static Version GetMonoVersion()
            {
                try
                {
                    Type type = Type.GetType(MonoRuntimeType);

                    if (type != null)
                    {
                        string displayName = type.InvokeMember(
                            MonoDisplayNameMember,
                            MarshalOps.PrivateStaticMethodBindingFlags,
                            null, null, null) as string;

                        if (!String.IsNullOrEmpty(displayName))
                        {
                            if (MonoVersionRegExList != null)
                            {
                                foreach (Regex regEx in MonoVersionRegExList)
                                {
                                    Match match = regEx.Match(displayName);

                                    if ((match != null) && match.Success)
                                        return new Version(match.Value);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Runtime Information Methods
            public static string GetRuntimeName()
            {
                return IsMono() ? MonoRuntimeName : MicrosoftRuntimeName;
            }

            ///////////////////////////////////////////////////////////////////

            public static Version GetRuntimeVersion()
            {
                //
                // HACK: Currently, the runtime version is the same as
                //       the framework version when not running on Mono.
                //
                return IsMono() ? GetMonoVersion() : GetFrameworkVersion();
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetRuntimeExtraVersion()
            {
                //
                // HACK: Currently, the runtime version is the same as
                //       the framework version when not running on Mono.
                //
                return IsMono() ? null : GetFrameworkExtraVersion();
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetRuntimeNameAndVersion()
            {
                return FormatOps.NameAndVersion(
                    GetRuntimeName(), GetRuntimeVersion(),
                    GetRuntimeExtraVersion());
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono also.
            //
            public static bool IsRuntime20()
            {
                Version version = GetRuntimeVersion();

                return (version != null) && (version.Major == 2);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Framework Information Methods
#if NET_40
            private static int GetFrameworkSetup451Value()
            {
                return PlatformOps.IsWindows81() ?
                    FrameworkSetup451OnWindows81Value : FrameworkSetup451Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup46Value()
            {
                return PlatformOps.IsWindows10OrHigher() ?
                    FrameworkSetup46OnWindows10Value : FrameworkSetup46Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup461Value()
            {
                return PlatformOps.IsWindows10NovemberUpdate() ?
                    FrameworkSetup461OnWindows10Value : FrameworkSetup461Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup462Value()
            {
                return PlatformOps.IsWindows10AnniversaryUpdate() ?
                    FrameworkSetup462OnWindows10Value : FrameworkSetup462Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup47Value()
            {
                return PlatformOps.IsWindows10CreatorsUpdate() ?
                    FrameworkSetup47OnWindows10Value : FrameworkSetup47Value;
            }

            ///////////////////////////////////////////////////////////////////

            private static int GetFrameworkSetup471Value()
            {
                return PlatformOps.IsWindows10FallCreatorsUpdate() ?
                    FrameworkSetup471OnWindows10Value : FrameworkSetup471Value;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            public static Version GetFrameworkVersion()
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (FrameworkVersion == null)
                        FrameworkVersion = System.Environment.Version;

                    return FrameworkVersion;
                }
            }

            ///////////////////////////////////////////////////////////////////

            public static string GetFrameworkExtraVersion()
            {
                try
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (frameworkExtraVersion == null)
                        {
                            object value = null;

                            if (IsFramework40())
                            {
                                value = Registry.GetValue(
                                    FrameworkSetup40KeyName,
                                    FrameworkSetup40ValueName, null);
                            }
                            else if (IsFramework20())
                            {
                                value = Registry.GetValue(
                                    FrameworkSetup20KeyName,
                                    FrameworkSetup20ValueName, null);
                            }

                            //
                            // NOTE: The value may still be null at this point
                            //       and that means this code may be executed
                            //       again the next time this method is called
                            //       (i.e. we have no way of caching the null
                            //       value).
                            //
                            frameworkExtraVersion = (value != null) ?
                                value.ToString() : null;
                        }

                        return frameworkExtraVersion;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return null;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Framework Version Detection Methods
            //
            // NOTE: Be sure to use !IsMono also.
            //
            public static bool IsFramework20()
            {
                Version version = GetFrameworkVersion();

                return (version != null) && (version.Major == 2);
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Be sure to use !IsMono also.
            //
            public static bool IsFramework40()
            {
                Version version = GetFrameworkVersion();

                return (version != null) && (version.Major == 4);
            }

            ///////////////////////////////////////////////////////////////////

#if NET_40
            public static bool IsFramework45OrHigher()
            {
                Version version = GetFrameworkVersion();
                int extraValue;

                if (!int.TryParse(
                        GetFrameworkExtraVersion(), out extraValue))
                {
                    return false;
                }

                if (IsFramework45(version, extraValue))
                    return true;

                if (IsFramework451(version, extraValue))
                    return true;

                if (IsFramework452(version, extraValue))
                    return true;

                if (IsFramework46(version, extraValue))
                    return true;

                if (IsFramework461(version, extraValue))
                    return true;

                if (IsFramework462(version, extraValue))
                    return true;

                if (IsFramework47(version, extraValue))
                    return true;

                if (IsFramework471(version, extraValue))
                    return true;

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            #region .NET Framework 4.5+ "Extra Version" Methods
            private static bool IsFramework45(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= FrameworkSetup45Value);
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsFramework451(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup451Value());
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsFramework452(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= FrameworkSetup452Value);
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsFramework46(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup46Value());
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsFramework461(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup461Value());
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsFramework462(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup462Value());
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsFramework47(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup47Value());
            }

            ///////////////////////////////////////////////////////////////////

            private static bool IsFramework471(
                Version version,
                int extraValue
                )
            {
                if ((version == null) || (version.Major != 4))
                    return false;

                return (extraValue >= GetFrameworkSetup471Value());
            }
            #endregion
#endif
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Environment Variable Support Class
        [ObjectId("24328505-60ed-4a79-89dd-41014d024f6d")]
        internal static class Environment
        {
            public static string GetVariable(
                string variable
                )
            {
                return System.Environment.GetEnvironmentVariable(variable);
            }

            ///////////////////////////////////////////////////////////////////

            public static void SetVariable(
                string variable,
                string value
                )
            {
                System.Environment.SetEnvironmentVariable(variable, value);
            }

            ///////////////////////////////////////////////////////////////////

            public static void UnsetVariable(
                string variable
                )
            {
                System.Environment.SetEnvironmentVariable(variable, null);
            }

            ///////////////////////////////////////////////////////////////////

            public static IDictionary GetRawVariables()
            {
                return System.Environment.GetEnvironmentVariables();
            }

            ///////////////////////////////////////////////////////////////////

            public static StringDictionary GetVariables()
            {
                IDictionary dictionary = GetRawVariables();

                if (dictionary == null)
                    return null;

                return new StringDictionary(dictionary);
            }

            ///////////////////////////////////////////////////////////////////

            public static string ExpandVariables(
                string name
                )
            {
                return System.Environment.ExpandEnvironmentVariables(name);
            }

            ///////////////////////////////////////////////////////////////////

            public static bool DoesVariableExist(
                string name
                )
            {
                string value = null;

                return DoesVariableExist(name, ref value);
            }

            ///////////////////////////////////////////////////////////////////

            public static bool DoesVariableExist(
                string name,
                ref string value
                )
            {
                value = GetVariable(name);
                return value != null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hash Code Support Class
        [ObjectId("b1bf9f59-a8a5-45b3-b54b-02c3d7107b85")]
        internal static class HashCodes
        {
            public static int Combine(
                int X,
                int Y
                )
            {
                byte[] bytes = new byte[sizeof(int) * 2];

                Array.Copy(BitConverter.GetBytes(X),
                    0, bytes, 0, sizeof(int));

                Array.Copy(BitConverter.GetBytes(Y),
                    0, bytes, sizeof(int), sizeof(int));

                return ConversionOps.ToInt(MathOps.HashFnv1UInt(bytes, true));
            }

            ///////////////////////////////////////////////////////////////////

            public static int Combine(
                int X,
                int Y,
                int Z
                )
            {
                byte[] bytes = new byte[sizeof(int) * 3];

                Array.Copy(BitConverter.GetBytes(X),
                    0, bytes, 0, sizeof(int));

                Array.Copy(BitConverter.GetBytes(Y),
                    0, bytes, sizeof(int), sizeof(int));

                Array.Copy(BitConverter.GetBytes(Z),
                    0, bytes, sizeof(int) * 2, sizeof(int));

                return ConversionOps.ToInt(MathOps.HashFnv1UInt(bytes, true));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Bi-directional Looping Support Methods
        public static bool ForCondition(
            bool increment,
            int index,
            int lowerBound,
            int upperBound
            )
        {
            if (increment)
                return (index <= upperBound);
            else
                return (index >= lowerBound);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ForLoop(
            bool increment,
            ref int index
            )
        {
            if (increment)
                index++;
            else
                index--;
        }
        #endregion
    }
}
