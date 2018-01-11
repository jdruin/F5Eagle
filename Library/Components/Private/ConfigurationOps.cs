/*
 * ConfigurationOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;

#if DEAD_CODE
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("df98c383-ae1f-46b5-a3ab-a3902d186498")]
    internal static class ConfigurationOps
    {
        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static bool? noComplainGet;
        private static bool? noComplainSet;
        private static bool? noComplainUnset;

        ///////////////////////////////////////////////////////////////////////

        private static PropertyInfo isReadOnly;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static void Initialize()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool isMono = CommonOps.Runtime.IsMono();

                ///////////////////////////////////////////////////////////////

                //
                // HACK: It is expected that attempting to read application
                //       settings will fail a large percentage of the time
                //       because they have not been set; therefore, disable
                //       those complaints by default.
                //
                if (noComplainGet == null)
                    noComplainGet = true;

                //
                // HACK: *MONO* There seems to be a subtle incompatibility
                //       on Mono that results in the AppSettings collection
                //       returned by the ConfigurationManager.AppSettings
                //       property being read-only (e.g. perhaps this only
                //       happens in non-default application domains?).  In
                //       order to facilitate better Mono support, we do not
                //       want to complain about these errors.
                //
                if (noComplainSet == null)
                    noComplainSet = isMono;

                if (noComplainUnset == null)
                    noComplainUnset = isMono;

                ///////////////////////////////////////////////////////////////

                if (isReadOnly == null)
                {
                    //
                    // HACK: Why must we do this?  This member is marked as
                    //       "protected"; however, we really need to know
                    //       this information (e.g. on Mono where it seems
                    //       that the collection may actually be read-only).
                    //       Therefore, just use Reflection.  We cache the
                    //       PropertyInfo object so that we do not need to
                    //       look it up more than once.
                    //
                    isReadOnly = typeof(NameValueCollection).GetProperty(
                        "IsReadOnly", MarshalOps.PrivateInstanceBindingFlags);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static NameValueCollection GetAppSettings()
        {
            //
            // WARNING: Do not use the ConfigurationManager class directly
            //          from anywhere else.
            //
            return ConfigurationManager.AppSettings;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsReadOnly(
            NameValueCollection appSettings /* in */
            )
        {
            if (appSettings == null)
                return false;

            try
            {
                lock (syncRoot)
                {
                    if (isReadOnly == null)
                        return false;

                    return (bool)isReadOnly.GetValue(appSettings, null);
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetNoComplain(
            ConfigurationOperation operation /* in */
            )
        {
            switch (operation)
            {
                case ConfigurationOperation.Get:
                    {
                        lock (syncRoot)
                        {
                            if (noComplainGet != null)
                                return (bool)noComplainGet;
                        }

                        break;
                    }
                case ConfigurationOperation.Set:
                    {
                        lock (syncRoot)
                        {
                            if (noComplainSet != null)
                                return (bool)noComplainSet;
                        }

                        break;
                    }
                case ConfigurationOperation.Unset:
                    {
                        lock (syncRoot)
                        {
                            if (noComplainUnset != null)
                                return (bool)noComplainUnset;
                        }

                        break;
                    }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Getting (Read) Values
        public static bool HaveAppSettings(
            bool moreThanZero /* in */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = GetAppSettings();

                if (appSettings == null)
                    return false;

                return !moreThanZero || (appSettings.Count > 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAppSetting(
            string name /* in */
            )
        {
            return GetAppSetting(name, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAppSetting(
            string name,    /* in */
            string @default /* in */
            )
        {
            string value = null;
            Result error = null;

            if (!TryGetAppSetting(name, out value, ref error))
            {
                bool noComplain = GetNoComplain(ConfigurationOperation.Get);

                if (!noComplain)
                    DebugOps.Complain(ReturnCode.Error, error);

                return @default;
            }

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryGetAppSetting(
            string name,      /* in */
            out string value, /* out */
            ref Result error  /* out */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = GetAppSettings();

                if (appSettings == null)
                {
                    value = null;

                    error = "invalid application settings";

                    return false;
                }

                string stringValue = appSettings.Get(name);

                if (stringValue == null)
                {
                    value = null;

                    error = String.Format(
                        "setting {0} not found", FormatOps.WrapOrNull(name));

                    return false;
                }

                value = stringValue;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Strongly Typed Setting Values
        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        public static bool TryGetIntegerAppSetting(
            string name,  /* in */
            out int value /* out */
            )
        {
            Result error = null;

            return TryGetIntegerAppSetting(name, out value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        public static bool TryGetIntegerAppSetting(
            string name,     /* in */
            out int value,   /* out */
            ref Result error /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = default(int);
                return false;
            }

            int intValue = default(int);

            if (Value.GetInteger2(
                    stringValue, ValueFlags.AnyInteger, null,
                    ref intValue, ref error) != ReturnCode.Ok)
            {
                value = default(int);
                return false;
            }

            value = intValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        public static bool TryGetListAppSetting(
            string name,          /* in */
            out StringList value, /* out */
            ref Result error      /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = null;
                return false;
            }

            StringList listValue = null;

            if (Parser.SplitList(
                    null, stringValue, 0, Length.Invalid, false,
                    ref listValue, ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = listValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        private static bool TryGetBooleanAppSetting(
            string name,     /* in */
            out bool value,  /* out */
            ref Result error /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = default(bool);
                return false;
            }

            bool boolValue = default(bool);

            if (Value.GetBoolean2(
                    stringValue, ValueFlags.AnyBoolean, null,
                    ref boolValue, ref error) != ReturnCode.Ok)
            {
                value = default(bool);
                return false;
            }

            value = boolValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        public static bool TryGetEnumAppSetting(
            string name,      /* in */
            Type enumType,    /* in */
            string oldValue,  /* in */
            out object value, /* out */
            ref Result error  /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = null;
                return false;
            }

            object enumValue = null;

            if (EnumOps.IsFlagsEnum(enumType))
            {
                enumValue = Utility.TryParseFlagsEnum(
                    null, enumType, oldValue, stringValue,
                    null, true, true, true, ref error);
            }
            else
            {
                enumValue = Utility.TryParseEnum(
                    enumType, stringValue, true, true,
                    ref error);
            }

            if (!(enumValue is Enum))
            {
                value = null;
                return false;
            }

            value = enumValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        public static bool TryGetObjectAppSetting(
            Interpreter interpreter, /* in */
            string name,             /* in */
            LookupFlags lookupFlags, /* in */
            out object value,        /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                value = null;
                error = "invalid interpreter";

                return false;
            }

            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = null;
                return false;
            }

            IObject @object = null;

            if (interpreter.GetObject(
                    stringValue, lookupFlags, ref @object,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = (@object != null) ? @object.Value : null;
            return true;
        }
#endif
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Setting (Write) Values
        public static void SetAppSetting(
            string name, /* in */
            string value /* in */
            )
        {
            Result error = null;

            if (!TrySetAppSetting(name, value, ref error))
            {
                bool noComplain = GetNoComplain(ConfigurationOperation.Set);

                if (!noComplain)
                    DebugOps.Complain(ReturnCode.Error, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TrySetAppSetting(
            string name,     /* in */
            string value,    /* in */
            ref Result error /* out */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = GetAppSettings();

                if (appSettings == null)
                {
                    error = "invalid application settings";
                    return false;
                }

                if (IsReadOnly(appSettings))
                {
                    error = "application settings are read-only";
                    return false;
                }

                appSettings.Set(name, value);
                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Unsetting (Write) Values
        public static void UnsetAppSetting(
            string name /* in */
            )
        {
            Result error = null;

            if (!TryUnsetAppSetting(name, ref error))
            {
                bool noComplain = GetNoComplain(ConfigurationOperation.Unset);

                if (!noComplain)
                    DebugOps.Complain(ReturnCode.Error, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryUnsetAppSetting(
            string name,     /* in */
            ref Result error /* out */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = GetAppSettings();

                if (appSettings == null)
                {
                    error = "invalid application settings";
                    return false;
                }

                if (IsReadOnly(appSettings))
                {
                    error = "application settings are read-only";
                    return false;
                }

                appSettings.Remove(name);
                return true;
            }
        }
        #endregion
        #endregion
    }
}
