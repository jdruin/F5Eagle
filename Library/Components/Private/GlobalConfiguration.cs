/*
 * GlobalConfiguration.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    [ObjectId("ec7e7b01-b6c3-40fb-87a0-4a9eefc6f192")]
    internal static class GlobalConfiguration
    {
        #region Private Constants
        //
        // NOTE: This format string is used when building the package
        //       prefixed environment variable names (e.g. Eagle_Foo).
        //
        private static readonly string EnvVarFormat = "{0}_{1}";

        ///////////////////////////////////////////////////////////////////////

        //
        //
        // NOTE: This is the prefix (not including the trailing underscore)
        //       that is used when handling environment variables that are
        //       package-specific.
        //
        // WARNING: *HACK* Hard-code the package environment variable prefix
        //          here because using the package name would require using
        //          the GlobalState class, which relies upon this class.
        //
        private static readonly string EnvVarPrefix = "Eagle";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: When this value is non-zero, trace messages will be written
        //       whenever a global configuration value is read, modified, or
        //       removed.
        //
        // HACK: This is purposely not read-only.
        //
        private static bool DefaultVerbose = ShouldBeVerbose();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static bool ShouldBeVerbose()
        {
            if (!Build.Debug)
                return false;

            if (CommonOps.Environment.DoesVariableExist(EnvVars.NoVerbose))
                return false;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Configuration Value Management Methods
        public static ConfigurationFlags GetFlags(
            ConfigurationFlags flags,
            bool verbose
            ) /* THREAD-SAFE */
        {
            ConfigurationFlags result = flags;

            if (verbose)
                result |= ConfigurationFlags.Verbose;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesValueExist(
            string variable,
            ConfigurationFlags flags
            ) /* THREAD-SAFE */
        {
            string value = GetValue(variable, flags);

            return (value != null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetValue(
            string variable,
            ConfigurationFlags flags
            ) /* THREAD-SAFE */
        {
            string prefixedVariable = null;

            //
            // NOTE: The default return value is null, which means that the
            //       value is not available and/or not set.
            //
            string value = null;

            //
            // NOTE: If the variable name is null or empty, return the default
            //       value (null) instead of potentially throwing an exception
            //       later.
            //
            if (String.IsNullOrEmpty(variable))
                goto done;

            //
            // NOTE: Try to get the variable name without the package name
            //       prefix?
            //
            bool unprefixed = FlagOps.HasFlags(
                flags, ConfigurationFlags.Unprefixed, true);

            //
            // NOTE: Try to get the variable name prefixed by package name
            //       first?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Prefixed, true))
            {
                prefixedVariable = String.Format(
                    EnvVarFormat, EnvVarPrefix, variable);
            }

            //
            // NOTE: Does the caller want to check the environment variables?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Environment, true))
            {
                //
                // NOTE: Try the variable name prefixed by our package name
                //       first?
                //
                if ((prefixedVariable != null) && (value == null))
                    value = CommonOps.Environment.GetVariable(prefixedVariable);

                //
                // NOTE: Failing that, just try for the variable name?
                //
                if (unprefixed && (value == null))
                    value = CommonOps.Environment.GetVariable(variable);
            }

#if CONFIGURATION
            //
            // NOTE: Does the caller want to check the loaded AppSettings?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.AppSettings, true))
            {
                //
                // NOTE: Try the variable name prefixed by our package name
                //       first?
                //
                if ((prefixedVariable != null) && (value == null))
                    value = ConfigurationOps.GetAppSetting(prefixedVariable);

                //
                // NOTE: Failing that, just try for the variable name?
                //
                if (unprefixed && (value == null))
                    value = ConfigurationOps.GetAppSetting(variable);
            }
#endif

            //
            // NOTE: If necessary, expand any contained environment variables.
            //
            if (!String.IsNullOrEmpty(value) &&
                FlagOps.HasFlags(flags, ConfigurationFlags.Expand, true))
            {
                value = CommonOps.Environment.ExpandVariables(value);
            }

        done:

            //
            // NOTE: Output diagnostic message about the configuration value
            //       request.
            //
            if (DefaultVerbose ||
                FlagOps.HasFlags(flags, ConfigurationFlags.Verbose, true))
            {
                TraceOps.DebugTrace(String.Format(
                    "GetValue: variable = {0}, prefixedVariable = {1}, " +
                    "value = {2}, defaultVerbose = {3}, flags = {4}",
                    FormatOps.WrapOrNull(variable),
                    FormatOps.WrapOrNull(prefixedVariable),
                    FormatOps.WrapOrNull(value), DefaultVerbose,
                    FormatOps.WrapOrNull(flags)),
                    typeof(GlobalConfiguration).Name,
                    TracePriority.StartupDebug);
            }

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetValue(
            string variable,
            string value,
            ConfigurationFlags flags
            ) /* THREAD-SAFE */
        {
            string prefixedVariable = null;

            //
            // NOTE: If the variable name is null or empty, do nothing.
            //
            if (String.IsNullOrEmpty(variable))
                goto done;

            //
            // NOTE: Try to set the variable name without the package name
            //       prefix?
            //
            bool unprefixed = FlagOps.HasFlags(
                flags, ConfigurationFlags.Unprefixed, true);

            //
            // NOTE: Set the variable name prefixed by package name instead?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Prefixed, true))
            {
                prefixedVariable = String.Format(
                    EnvVarFormat, EnvVarPrefix, variable);
            }

            //
            // NOTE: If necessary, expand any contained environment variables.
            //
            if (!String.IsNullOrEmpty(value) &&
                FlagOps.HasFlags(flags, ConfigurationFlags.Expand, true))
            {
                value = CommonOps.Environment.ExpandVariables(value);
            }

#if CONFIGURATION
            //
            // NOTE: Does the caller want to modify the loaded AppSettings?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.AppSettings, true))
            {
                //
                // NOTE: Attempt to set the requested AppSettings value,
                //       also using the prefixed name if requested.
                //
                if (unprefixed)
                    ConfigurationOps.SetAppSetting(variable, value);

                if (prefixedVariable != null)
                    ConfigurationOps.SetAppSetting(prefixedVariable, value);
            }
#endif

            //
            // NOTE: Does the caller want to modify the environment variables?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Environment, true))
            {
                //
                // NOTE: Attempt to set the requested environment variable,
                //       also using the prefixed name if requested.
                //
                if (unprefixed)
                    CommonOps.Environment.SetVariable(variable, value);

                if (prefixedVariable != null)
                    CommonOps.Environment.SetVariable(prefixedVariable, value);
            }

        done:

            //
            // NOTE: Output diagnostic message about the configuration value
            //       request.
            //
            if (DefaultVerbose ||
                FlagOps.HasFlags(flags, ConfigurationFlags.Verbose, true))
            {
                TraceOps.DebugTrace(String.Format(
                    "SetValue: variable = {0}, prefixedVariable = {1}, " +
                    "value = {2}, defaultVerbose = {3}, flags = {4}",
                    FormatOps.WrapOrNull(variable),
                    FormatOps.WrapOrNull(prefixedVariable),
                    FormatOps.WrapOrNull(value), DefaultVerbose,
                    FormatOps.WrapOrNull(flags)),
                    typeof(GlobalConfiguration).Name,
                    TracePriority.StartupDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void UnsetValue(
            string variable,
            ConfigurationFlags flags
            ) /* THREAD-SAFE */
        {
            string prefixedVariable = null;

            //
            // NOTE: If the variable name is null or empty, do nothing.
            //
            if (String.IsNullOrEmpty(variable))
                goto done;

            //
            // NOTE: Try to set the variable name without the package name
            //       prefix?
            //
            bool unprefixed = FlagOps.HasFlags(
                flags, ConfigurationFlags.Unprefixed, true);

            //
            // NOTE: Set the variable name prefixed by package name instead?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Prefixed, true))
            {
                prefixedVariable = String.Format(
                    EnvVarFormat, EnvVarPrefix, variable);
            }

#if CONFIGURATION
            //
            // NOTE: Does the caller want to remove the loaded AppSettings?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.AppSettings, true))
            {
                //
                // NOTE: Try to unset the requested AppSettings value(s).
                //
                if (unprefixed)
                    ConfigurationOps.UnsetAppSetting(variable);

                if (prefixedVariable != null)
                    ConfigurationOps.UnsetAppSetting(prefixedVariable);
            }
#endif

            //
            // NOTE: Does the caller want to remove the environment variables?
            //
            if (FlagOps.HasFlags(flags, ConfigurationFlags.Environment, true))
            {
                //
                // NOTE: Try to unset the requested environment variable(s).
                //
                if (unprefixed)
                    CommonOps.Environment.UnsetVariable(variable);

                if (prefixedVariable != null)
                    CommonOps.Environment.UnsetVariable(prefixedVariable);
            }

        done:

            //
            // NOTE: Output diagnostic message about the configuration value
            //       request.
            //
            if (DefaultVerbose ||
                FlagOps.HasFlags(flags, ConfigurationFlags.Verbose, true))
            {
                TraceOps.DebugTrace(String.Format(
                    "UnsetValue: variable = {0}, prefixedVariable = {1}, " +
                    "defaultVerbose = {2}, flags = {3}",
                    FormatOps.WrapOrNull(variable),
                    FormatOps.WrapOrNull(prefixedVariable),
                    DefaultVerbose, FormatOps.WrapOrNull(flags)),
                    typeof(GlobalConfiguration).Name,
                    TracePriority.StartupDebug);
            }
        }
        #endregion
    }
}
