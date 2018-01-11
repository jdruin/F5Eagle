/*
 * SetupOps.cs --
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
using Microsoft.Win32;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("bd1dfe4f-116f-4780-9bbf-4c8c80d94873")]
    internal static class SetupOps
    {
        #region Private Constants
#if NATIVE && WINDOWS
        private static readonly string mutexName = GlobalState.GetPackageName(
            PackageType.Default, null, "_Setup", false);

        ///////////////////////////////////////////////////////////////////////

        private static readonly string globalMutexName =
            "Global\\" + mutexName;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static readonly string libraryKeyName =
            GlobalState.GetPackageName(PackageType.Default, "Software\\",
                null, false);

        private static readonly string lowSecurityKeyNameSuffix = "\\Low";

        ///////////////////////////////////////////////////////////////////////

        private const string assemblyValueName = "Assembly";
        private const string appIdValueName = "AppId";
        private const string compileInfoValueName = "CompileInfo";
        private const string pathValueName = "Path";
        private const string checkCoreTrustedValueName = "CheckCoreTrusted";
        private const string checkCoreVerifiedValueName = "CheckCoreVerified";
        private const string checkCoreUpdatesValueName = "CheckCoreUpdates";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Settings
        //
        // HACK: These are not read-only.
        //
        private static SettingFlags DefaultReadSettingFlags =
            SettingFlags.Default;

        private static SettingFlags DefaultWriteSettingFlags =
            SettingFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are not read-only.
        //
        // NOTE: These setting flags were primarily designed to be used with
        //       the methods reading and writing "CheckCoreUpdates" value.  The
        //       intent here is that "normal users" can check for updates using
        //       the "low security" group of settings, only falling back to the
        //       "high security" group of settings if necessary (for read-only
        //       operations) and not impact users with administrator access.
        //       Furthermore, users with administrator access can always check
        //       for updates using the "high security" group of settings, only
        //       falling back to the "low security" group of settings if
        //       necessary.
        //
        // NOTE: It should be noted that "normal users" will not typically be
        //       able to actually install updates; however, that is a totally
        //       separate issue and can vary wildly between deployments.
        //
        private static SettingFlags SpecialReadSettingFlags =
            SettingFlags.LocalMachine | SettingFlags.LowSecurity |
            SettingFlags.HighSecurity;

        private static SettingFlags SpecialWriteSettingFlags =
            SettingFlags.LocalMachine | SettingFlags.LowSecurity |
            SettingFlags.HighSecurity;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are not read-only.
        //
        private static bool PathNoComplain = false;
        private static bool CheckCoreTrustedNoComplain = true;
        private static bool CheckCoreVerifiedNoComplain = true;
        private static bool CheckCoreUpdatesNoComplain = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are not read-only.
        //
        private static bool DefaultCheckCoreTrusted = true;
        private static bool DefaultCheckCoreVerified = true;
        private static bool DefaultCheckCoreUpdates = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is not read-only.
        //
        private static long CheckCoreUpdatesTicks = 5 * TimeSpan.TicksPerDay;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        //
        // NOTE: *NEVER CLOSED* Prevents our setup from installing.
        //
        private static IntPtr globalMutex = IntPtr.Zero;
        private static IntPtr mutex = IntPtr.Zero;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot)
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

#if NATIVE && WINDOWS
                if (empty || !String.IsNullOrEmpty(mutexName))
                    localList.Add("MutexName",
                        FormatOps.DisplayString(mutexName));

                if (empty || (mutex != IntPtr.Zero))
                    localList.Add("Mutex", mutex.ToString());

                if (empty || !String.IsNullOrEmpty(globalMutexName))
                    localList.Add("GlobalMutexName",
                        FormatOps.DisplayString(globalMutexName));

                if (empty || (globalMutex != IntPtr.Zero))
                    localList.Add("GlobalMutex", globalMutex.ToString());
#endif

                if (empty || !String.IsNullOrEmpty(libraryKeyName))
                    localList.Add("LibraryKeyName",
                        FormatOps.DisplayString(libraryKeyName));

                if (empty || !String.IsNullOrEmpty(lowSecurityKeyNameSuffix))
                    localList.Add("LowSecurityKeyNameSuffix",
                        FormatOps.DisplayString(lowSecurityKeyNameSuffix));

                if (empty || (DefaultReadSettingFlags != SettingFlags.None))
                    localList.Add("ReadSettingFlags",
                        DefaultReadSettingFlags.ToString());

                if (empty || (DefaultWriteSettingFlags != SettingFlags.None))
                    localList.Add("WriteSettingFlags",
                        DefaultWriteSettingFlags.ToString());

                if (empty || (SpecialReadSettingFlags != SettingFlags.None))
                    localList.Add("SpecialReadSettingFlags",
                        SpecialReadSettingFlags.ToString());

                if (empty || (SpecialWriteSettingFlags != SettingFlags.None))
                    localList.Add("SpecialWriteSettingFlags",
                        SpecialWriteSettingFlags.ToString());

                if (empty || PathNoComplain)
                    localList.Add("PathNoComplain",
                        PathNoComplain.ToString());

                if (empty || CheckCoreTrustedNoComplain)
                    localList.Add("CheckCoreTrustedNoComplain",
                        CheckCoreTrustedNoComplain.ToString());

                if (empty || CheckCoreVerifiedNoComplain)
                    localList.Add("CheckCoreVerifiedNoComplain",
                        CheckCoreVerifiedNoComplain.ToString());

                if (empty || CheckCoreUpdatesNoComplain)
                    localList.Add("CheckCoreUpdatesNoComplain",
                        CheckCoreUpdatesNoComplain.ToString());

                if (empty || DefaultCheckCoreTrusted)
                    localList.Add("DefaultCheckCoreTrusted",
                        DefaultCheckCoreTrusted.ToString());

                if (empty || DefaultCheckCoreVerified)
                    localList.Add("DefaultCheckCoreVerified",
                        DefaultCheckCoreVerified.ToString());

                if (empty || DefaultCheckCoreUpdates)
                    localList.Add("DefaultCheckCoreUpdates",
                        DefaultCheckCoreUpdates.ToString());

                if (empty || (CheckCoreUpdatesTicks > 0))
                    localList.Add("CheckCoreUpdatesTicks",
                        StringList.MakeList(CheckCoreUpdatesTicks.ToString(),
                        FormatOps.AlwaysOrNever(CheckCoreUpdatesTicks)));

                bool shouldCheckCoreTrusted = ShouldCheckCoreTrusted();

                if (empty || shouldCheckCoreTrusted)
                    localList.Add("ShouldCheckCoreTrusted",
                        shouldCheckCoreTrusted.ToString());

                bool shouldCheckCoreVerified = ShouldCheckCoreVerified();

                if (empty || shouldCheckCoreVerified)
                    localList.Add("ShouldCheckCoreVerified",
                        shouldCheckCoreVerified.ToString());

                bool shouldCheckCoreUpdates = ShouldCheckCoreUpdates();

                if (empty || shouldCheckCoreUpdates)
                    localList.Add("ShouldCheckCoreUpdates",
                        shouldCheckCoreUpdates.ToString());

                string appId = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), appIdValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(appId))
                    localList.Add("AppId", FormatOps.DisplayString(appId));

                string path = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), pathValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(path))
                    localList.Add("Path", FormatOps.DisplayString(path));

                string assembly = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), assemblyValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(assembly))
                    localList.Add("Assembly",
                        FormatOps.DisplayString(assembly));

                string compileInfo = GetSettingValue(
                    GlobalState.GetAssemblyVersion(), compileInfoValueName,
                    DefaultReadSettingFlags, true);

                if (empty || !String.IsNullOrEmpty(compileInfo))
                    localList.Add("CompileInfo",
                        FormatOps.DisplayString(compileInfo));

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Setup Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Instance Path Support Methods
        public static string GetPath()
        {
            return GetPath(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPath(
            Version version
            )
        {
            return GetSettingValue(
                version, pathValueName, DefaultReadSettingFlags,
                PathNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetPath(
            RegistryKey rootKey,
            Version version,
            ref string path,
            ref Result error
            )
        {
            return GetSettingValue(
                rootKey, version, pathValueName, DefaultReadSettingFlags,
                ref path, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Instance Enumeration Support Methods
        public static ReturnCode GetInstances(
            ref Result result
            )
        {
            return GetInstances(Registry.LocalMachine, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetInstances(
            RegistryKey rootKey,
            ref Result result
            )
        {
            if (rootKey == null)
            {
                result = "invalid root registry key";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(libraryKeyName))
            {
                result = "invalid library registry key name";
                return ReturnCode.Error;
            }

            try
            {
                using (RegistryKey key = rootKey.OpenSubKey(libraryKeyName))
                {
                    if (key == null)
                    {
                        result = "no setup instances found";
                        return ReturnCode.Error;
                    }

                    StringList list = new StringList();

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        StringList subList = new StringList("Name",
                            subKeyName);

                        try
                        {
                            using (RegistryKey subKey = key.OpenSubKey(
                                    subKeyName))
                            {
                                if (subKey != null)
                                {
                                    subList.Add(appIdValueName,
                                        subKey.GetValue(
                                            appIdValueName) as string);

                                    subList.Add(pathValueName,
                                        subKey.GetValue(
                                            pathValueName) as string);

                                    subList.Add(assemblyValueName,
                                        subKey.GetValue(
                                            assemblyValueName) as string);

                                    subList.Add(compileInfoValueName,
                                        subKey.GetValue(
                                            compileInfoValueName) as string);
                                }
                                else
                                {
                                    subList.Add("Error",
                                        String.Format("could not open sub-key " +
                                        "\"{0}\" of registry key \"{1}\"",
                                        subKeyName, key));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            subList.Add("Exception", e.ToString());

                            subList.Add("Error",
                                String.Format("could not open sub-key " +
                                "\"{0}\" of registry key \"{1}\"",
                                subKeyName, key));
                        }

                        list.Add(subList.ToString());
                    }

                    result = list;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Trusted Support Methods
        public static bool ShouldCheckCoreTrusted()
        {
            return ShouldCheckCoreTrusted(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldCheckCoreTrusted(
            Version version
            )
        {
            string value = GetSettingValue(
                version, checkCoreTrustedValueName, DefaultReadSettingFlags,
                CheckCoreTrustedNoComplain);

            if (value == null)
                return DefaultCheckCoreTrusted;

            ReturnCode code;
            Result error = null;
            bool boolValue = false;

            code = Value.GetBoolean4(
                value, ValueFlags.AnyBoolean, ref boolValue, ref error);

            if (code != ReturnCode.Ok)
            {
                if (!CheckCoreTrustedNoComplain)
                    DebugOps.Complain(code, error);

                return DefaultCheckCoreTrusted;
            }

            return boolValue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Verified Support Methods
        public static bool ShouldCheckCoreVerified()
        {
            return ShouldCheckCoreVerified(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldCheckCoreVerified(
            Version version
            )
        {
            string value = GetSettingValue(
                version, checkCoreVerifiedValueName, DefaultReadSettingFlags,
                CheckCoreVerifiedNoComplain);

            if (value == null)
                return DefaultCheckCoreVerified;

            ReturnCode code;
            Result error = null;
            bool boolValue = false;

            code = Value.GetBoolean4(
                value, ValueFlags.AnyBoolean, ref boolValue, ref error);

            if (code != ReturnCode.Ok)
            {
                if (!CheckCoreVerifiedNoComplain)
                    DebugOps.Complain(code, error);

                return DefaultCheckCoreVerified;
            }

            return boolValue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Check-For-Updates Support Methods
        private static bool ShouldCheckCoreUpdatesViaValue(
            DateTime dateTime
            )
        {
            //
            // NOTE: A negative value means "never".
            //
            if (CheckCoreUpdatesTicks < 0)
                return false;

            //
            // NOTE: A zero value means "always".
            //
            if (CheckCoreUpdatesTicks == 0)
                return true;

            //
            // NOTE: A positive value means we should check it against
            //       the elapsed number of ticks since the last check
            //       for updates.
            //
            if (TimeOps.GetUtcNow().Subtract(
                    dateTime).Ticks >= CheckCoreUpdatesTicks)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldCheckCoreUpdatesViaValue(
            string value
            )
        {
            if (value == null)
                return DefaultCheckCoreUpdates;

            bool boolValue = false;
            Result localError = null;
            ResultList errors = null;

            if (Value.GetBoolean4(
                    value, ValueFlags.AnyBoolean, ref boolValue,
                    ref localError) == ReturnCode.Ok)
            {
                //
                // NOTE: Return the stored boolean value verbatim.  Since
                //       the setup initially defaults this value to "True",
                //       we should always see a Boolean before a DateTime.
                //
                return boolValue;
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            DateTime dateTimeValue = DateTime.MinValue;

            localError = null;

            if (Value.GetDateTime3(
                    value, ValueFlags.AnyDateTime, DateTimeKind.Local,
                    ref dateTimeValue, ref localError) == ReturnCode.Ok)
            {
                //
                // NOTE: Convert the value to UTC for comparison.
                //
                dateTimeValue = dateTimeValue.ToUniversalTime();

                //
                // NOTE: Have enough ticks elapsed since the last check
                //       for updates?
                //
                if (ShouldCheckCoreUpdatesViaValue(dateTimeValue))
                {
                    //
                    // NOTE: Enough ticks have passed since the last
                    //       check for updates, return true.
                    //
                    return true;
                }
                else
                {
                    //
                    // NOTE: Not enough ticks have passed since the
                    //       last check for updates (i.e. it was too
                    //       recently), return false.
                    //
                    return false;
                }
            }
            else if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            //
            // NOTE: If we get to this point, the string value could not be
            //       converted to a Boolean or DateTime.  This is an error.
            //       Complain about this and return the default value.
            //
            DebugOps.Complain(ReturnCode.Error, errors);

            return DefaultCheckCoreUpdates;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckCoreUpdates()
        {
            return ShouldCheckCoreUpdates(GlobalState.GetAssemblyVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldCheckCoreUpdates(
            Version version
            )
        {
            return ShouldCheckCoreUpdatesViaValue(
                GetSettingValue(version, checkCoreUpdatesValueName,
                SpecialReadSettingFlags, CheckCoreUpdatesNoComplain));
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MarkCheckCoreUpdatesNow()
        {
            SetSettingValue(
                GlobalState.GetAssemblyVersion(), checkCoreUpdatesValueName,
                FormatOps.Iso8601UpdateDateTime(TimeOps.GetNow()),
                SpecialWriteSettingFlags, CheckCoreUpdatesNoComplain);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Read/Write Setting Support Methods
        private static RegistryKey[] GetRootKeys(
            SettingFlags flags
            )
        {
            return new RegistryKey[] {
                FlagOps.HasFlags(flags, SettingFlags.CurrentUser, true) ?
                    Registry.CurrentUser : null,
                FlagOps.HasFlags(flags, SettingFlags.LocalMachine, true) ?
                    Registry.LocalMachine : null
            };
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetKeyName(
            Version version,
            bool? lowSecurity
            )
        {
            if (String.IsNullOrEmpty(libraryKeyName))
                return null;

            bool localLowSecurity = (lowSecurity != null) ?
                (bool)lowSecurity : !RuntimeOps.IsAdministrator();

            string keyNameSuffix = localLowSecurity ?
                lowSecurityKeyNameSuffix : String.Empty;

            if (version != null)
            {
                return String.Format(
                    "{0}\\{1}{2}", libraryKeyName, version,
                    keyNameSuffix);
            }
            else
            {
                return String.Format(
                    "{0}{1}", libraryKeyName, keyNameSuffix);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string[] GetKeyNames(
            Version version,
            SettingFlags flags,
            bool readOnly
            )
        {
            if (FlagOps.HasFlags(flags, SettingFlags.AnySecurity, true))
            {
                //
                // NOTE: Check all groups of settings that are enabled via
                //       the flags specified by the caller, regardless of
                //       the permissions for the current user and what type
                //       of operation this is.  However, the order in which
                //       these are checked may depend on the permissions of
                //       the current user.
                //
                StringList list = new StringList();

                if (FlagOps.HasFlags(flags, SettingFlags.UserSecurity, true))
                    list.Add(GetKeyName(version, null));

                if (RuntimeOps.IsAdministrator())
                {
                    if (FlagOps.HasFlags(
                            flags, SettingFlags.HighSecurity, true))
                    {
                        list.Add(GetKeyName(version, false));
                    }

                    if (FlagOps.HasFlags(
                            flags, SettingFlags.LowSecurity, true))
                    {
                        list.Add(GetKeyName(version, true));
                    }
                }
                else
                {
                    if (FlagOps.HasFlags(
                            flags, SettingFlags.LowSecurity, true))
                    {
                        list.Add(GetKeyName(version, true));
                    }

                    if (FlagOps.HasFlags(
                            flags, SettingFlags.HighSecurity, true))
                    {
                        list.Add(GetKeyName(version, false));
                    }
                }

                return list.ToArray();
            }
            else if (RuntimeOps.IsAdministrator())
            {
                //
                // NOTE: Check the "high security" group of settings only,
                //       when it is enabled.
                //
                return new string[] {
                    FlagOps.HasFlags(flags, SettingFlags.HighSecurity, true) ?
                        GetKeyName(version, false) : null
                };
            }
            else
            {
                //
                // NOTE: Check the "low security" group of settings, when it
                //       is enabled; however, only check the "high security"
                //       group of settings when it is enabled and this is a
                //       read operation.
                //
                return new string[] {
                    FlagOps.HasFlags(flags, SettingFlags.LowSecurity, true) ?
                        GetKeyName(version, true) : null, readOnly &&
                    FlagOps.HasFlags(flags, SettingFlags.HighSecurity, true) ?
                        GetKeyName(version, false) : null
                };
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddVersionVariants(
            IDictionary<Version, Version> versions,
            Version version
            )
        {
            if (versions == null)
                return;

            if (version != null)
            {
                if (!versions.ContainsKey(version))
                    versions.Add(version, version);

                Version version3 = GlobalState.GetThreePartVersion(version);

                if (!versions.ContainsKey(version3))
                    versions.Add(version3, version3);

                Version version2 = GlobalState.GetTwoPartVersion(version);

                if (!versions.ContainsKey(version2))
                    versions.Add(version2, version2);
            }

            Version version0 = new Version();

            if (!versions.ContainsKey(version0))
                versions.Add(version0, null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Read Setting Support Methods
        private static string GetSettingValue(
            Version version,
            string name,
            SettingFlags flags,
            bool noComplain
            )
        {
            IDictionary<Version, Version> versions =
                new Dictionary<Version, Version>();

            AddVersionVariants(versions, version);

            RegistryKey[] rootKeys = GetRootKeys(flags);

            foreach (RegistryKey rootKey in rootKeys)
            {
                if (rootKey == null)
                    continue;

                foreach (Version localVersion in versions.Values)
                {
                    string value = GetSettingValue(
                        rootKey, localVersion, name, flags, noComplain);

                    if (value != null)
                        return value;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            SettingFlags flags,
            bool noComplain
            )
        {
            bool result;
            string value = null;
            Result error = null;

            result = GetSettingValue(
                rootKey, version, name, flags, ref value, ref error);

            if (!result && !noComplain)
                DebugOps.Complain(ReturnCode.Error, error);

            TraceOps.DebugTrace(String.Format(
                "GetSettingValue: rootKey = {0}, version = {1}, name = " +
                "{2}, value = {3}, flags = {4}, noComplain = {5}, " +
                "result = {6}, error = {7}", FormatOps.WrapOrNull(rootKey),
                FormatOps.WrapOrNull(version), FormatOps.WrapOrNull(name),
                FormatOps.WrapOrNull(value), FormatOps.WrapOrNull(flags),
                noComplain, result, FormatOps.WrapOrNull(true, true, error)),
                typeof(SetupOps).Name, TracePriority.StartupDebug);

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            SettingFlags flags,
            ref string value,
            ref Result error
            )
        {
            if (rootKey == null)
            {
                error = "invalid root registry key";
                return false;
            }

            try
            {
                string[] keyNames = GetKeyNames(version, flags, true);

                if (keyNames == null)
                {
                    error = "no setup instances available";
                    return false;
                }

                int count = 0;
                ResultList errors = null;

                foreach (string keyName in keyNames)
                {
                    if (keyName == null)
                        continue;

                    count++;

                    using (RegistryKey key = rootKey.OpenSubKey(keyName))
                    {
                        if (key != null)
                        {
                            //
                            // NOTE: Per MSDN, null and empty string are
                            //       allowed here for the value name and
                            //       will result in the "default" value
                            //       being returned for the associated
                            //       registry key.
                            //
                            value = key.GetValue(name) as string;
                            return true;
                        }
                        else
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "setup instance not found: {0}\\{1}",
                                rootKey, keyName));
                        }
                    }
                }

                if (count == 0)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("no setup instances checked");
                }

                if (errors != null)
                    error = errors;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Write Setting Support Methods
        private static bool SetSettingValue(
            Version version,
            string name,
            string value,
            SettingFlags flags,
            bool noComplain
            )
        {
            IDictionary<Version, Version> versions =
                new Dictionary<Version, Version>();

            AddVersionVariants(versions, version);

            RegistryKey[] rootKeys = GetRootKeys(flags);

            foreach (RegistryKey rootKey in rootKeys)
            {
                if (rootKey == null)
                    continue;

                foreach (Version localVersion in versions.Values)
                {
                    if (SetSettingValue(
                            rootKey, localVersion, name, value, flags,
                            noComplain))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            string value,
            SettingFlags flags,
            bool noComplain
            )
        {
            bool result;
            Result error = null;

            result = SetSettingValue(
                rootKey, version, name, value, flags, ref error);

            if (!result && !noComplain)
                DebugOps.Complain(ReturnCode.Error, error);

            TraceOps.DebugTrace(String.Format(
                "SetSettingValue: rootKey = {0}, version = {1}, name = " +
                "{2}, value = {3}, flags = {4}, noComplain = {5}, " +
                "result = {6}, error = {7}", FormatOps.WrapOrNull(rootKey),
                FormatOps.WrapOrNull(version), FormatOps.WrapOrNull(name),
                FormatOps.WrapOrNull(value), FormatOps.WrapOrNull(flags),
                noComplain, result, FormatOps.WrapOrNull(true, true, error)),
                typeof(SetupOps).Name, TracePriority.StartupDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetSettingValue(
            RegistryKey rootKey,
            Version version,
            string name,
            string value,
            SettingFlags flags,
            ref Result error
            )
        {
            if (rootKey == null)
            {
                error = "invalid root registry key";
                return false;
            }

            if (value == null)
            {
                error = "invalid setting value";
                return false;
            }

            try
            {
                string[] keyNames = GetKeyNames(version, flags, false);

                if (keyNames == null)
                {
                    error = "no setup instances available";
                    return false;
                }

                int count = 0;
                ResultList errors = null;

                foreach (string keyName in keyNames)
                {
                    if (keyName == null)
                        continue;

                    count++;

                    using (RegistryKey key = rootKey.OpenSubKey(keyName, true))
                    {
                        if (key != null)
                        {
                            //
                            // NOTE: Per MSDN, null and empty string are
                            //       allowed here for the value name and
                            //       will result in the "default" value
                            //       being set for the associated registry
                            //       key.
                            //
                            key.SetValue(name, value);
                            return true;
                        }
                        else
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "setup instance not found: {0}\\{1}",
                                rootKey, keyName));
                        }
                    }
                }

                if (count == 0)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("no setup instances checked");
                }

                if (errors != null)
                    error = errors;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Mutex Support Methods
#if NATIVE && WINDOWS
        private static void SetupOps_Exited(
            object sender,
            EventArgs e
            )
        {
            ReturnCode mutexCode;
            Result mutexError = null;

            mutexCode = CloseMutexes(ref mutexError);

            if (mutexCode != ReturnCode.Ok)
                DebugOps.Complain(null, mutexCode, mutexError);

            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= SetupOps_Exited;
                else
                    appDomain.ProcessExit -= SetupOps_Exited;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr CreateMutex(
            string name,
            ref Result error
            )
        {
            IntPtr handle = IntPtr.Zero;

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    handle = NativeOps.UnsafeNativeMethods.CreateMutex(
                        IntPtr.Zero, true, name);

                    if (handle == IntPtr.Zero)
                    {
                        error = String.Format(
                            "could not create mutex \"{0}\": {1}",
                            name, NativeOps.GetErrorMessage());
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return handle;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CloseMutex(
            ref IntPtr handle,
            ref Result error
            )
        {
            bool result = false;

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    if (NativeOps.UnsafeNativeMethods.CloseHandle(
                            handle)) /* throw */
                    {
                        handle = IntPtr.Zero;
                        result = true;
                    }
                    else
                    {
                        error = String.Format(
                            "could not close mutex \"{0}\": {1}",
                            handle, NativeOps.GetErrorMessage());
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CreateMutexes()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload += SetupOps_Exited;
                else
                    appDomain.ProcessExit += SetupOps_Exited;
            }

            ReturnCode mutexCode;
            Result mutexError = null;

            mutexCode = CreateMutexes(ref mutexError);

            if (mutexCode != ReturnCode.Ok)
                DebugOps.Complain(null, mutexCode, mutexError);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CreateMutexes(
            ref Result error
            )
        {
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                Result localError;
                ResultList errors = null;

                //
                // NOTE: This should prevent setup from running while
                //       the library is in use (even if the setup is
                //       running in a different user session).
                //
                if ((globalMutex == IntPtr.Zero) &&
                    !String.IsNullOrEmpty(globalMutexName))
                {
                    localError = null;

                    globalMutex = CreateMutex(
                        globalMutexName, ref localError);

                    if (globalMutex == IntPtr.Zero)
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }
                        else
                        {
                            localError = String.Format(
                                "could not create global mutex \"{0}\": {1}",
                                globalMutexName, NativeOps.GetErrorMessage());
                        }

                        DebugOps.Complain(ReturnCode.Error, localError);
                    }
                }

                //
                // NOTE: This should prevent setup from running while
                //       the library is in use in this user session.
                //
                if ((mutex == IntPtr.Zero) &&
                    !String.IsNullOrEmpty(mutexName))
                {
                    localError = null;

                    mutex = CreateMutex(mutexName, ref localError);

                    if (mutex == IntPtr.Zero)
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }
                        else
                        {
                            localError = String.Format(
                                "could not create normal mutex \"{0}\": {1}",
                                mutexName, NativeOps.GetErrorMessage());
                        }

                        DebugOps.Complain(ReturnCode.Error, localError);
                    }
                }

                //
                // NOTE: Pass any errors back to the caller.
                //
                if (errors != null)
                    error = errors;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CloseMutexes(
            ref Result error
            )
        {
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                Result localError;
                ResultList errors = null;

                if (mutex != IntPtr.Zero)
                {
                    localError = null;

                    if (CloseMutex(ref mutex, ref localError))
                    {
                        mutex = IntPtr.Zero;
                    }
                    else
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }
                        else
                        {
                            localError = String.Format(
                                "could not close normal mutex \"{0}\": {1}",
                                mutex, NativeOps.GetErrorMessage());
                        }

                        DebugOps.Complain(ReturnCode.Error, localError);
                    }
                }

                if (globalMutex != IntPtr.Zero)
                {
                    localError = null;

                    if (CloseMutex(ref globalMutex, ref localError))
                    {
                        globalMutex = IntPtr.Zero;
                    }
                    else
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }
                        else
                        {
                            localError = String.Format(
                                "could not close global mutex \"{0}\": {1}",
                                globalMutex, NativeOps.GetErrorMessage());
                        }

                        DebugOps.Complain(ReturnCode.Error, localError);
                    }
                }

                //
                // NOTE: Pass any errors back to the caller.
                //
                if (errors != null)
                    error = errors;
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion
    }
}
