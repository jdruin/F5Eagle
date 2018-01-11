/*
 * Eagle.cs --
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;
using System.Web.Services;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Services
{
    [WebService(
        Name = Eagle.Name,
        Description = Eagle.Description,
        Namespace = Eagle.Namespace)]
    [ObjectId("36ce542c-dc1c-4a9a-affa-ce22aebb1173")]
    public sealed class Eagle : IEagle
    {
        #region Private Constants
        private const string Name = "Eagle Web Service";

        private const string Description =
            "This service is used to handle dynamic content (i.e. expressions, " +
            "scripts, and/or text blocks) for the Tcl and/or Eagle languages.";

        private const string Namespace = "https://eagle.to/";

        ///////////////////////////////////////////////////////////////////////

        private static readonly Assembly assembly =
            Assembly.GetExecutingAssembly();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Settings
        //
        // NOTE: By default, no console.
        //
        private static readonly bool DefaultConsole = false;

        //
        // NOTE: By default:
        //
        //       1. We want to initialize the script library.
        //       2. We want to throw an exception if disposed objects are
        //          accessed.
        //       3. We do not want to change the console title.
        //       4. We do not want to change the console icon.
        //       5. We do not want to intercept the Ctrl-C keypress.
        //       6. We want to have only directories that actually exist in
        //          the auto-path.
        //       7. We want to provide a "safe" subset of commands.
        //
        private static readonly CreateFlags DefaultCreateFlags =
            CreateFlags.SafeEmbeddedUse & ~CreateFlags.ThrowOnError;

        //
        // NOTE: By default:
        //
        //       1. We want no special engine flags.
        //
        private static readonly EngineFlags DefaultEngineFlags =
            EngineFlags.None;

        //
        // NOTE: By default:
        //
        //       1. We want all substitution types to be performed.
        //
        private static readonly SubstitutionFlags DefaultSubstitutionFlags =
            SubstitutionFlags.Default;

        //
        // NOTE: By default:
        //
        //       1. We want to handle events targeted to the engine.
        //
        private static readonly EventFlags DefaultEventFlags =
            EventFlags.Default;

        //
        // NOTE: By default:
        //
        //       1. We want all expression types to be performed.
        //
        private static readonly ExpressionFlags DefaultExpressionFlags =
            ExpressionFlags.Default;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Eagle()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Settings Management Class
        [ObjectId("1fca91ae-46dd-4e13-98d6-c21815c8131c")]
        private static class Settings
        {
            #region Setting Names
            public static readonly string EagleLibrary = EnvVars.EagleLibrary;
            public static readonly string SetupScript = "SetupScript";
            public static readonly string LibraryPath = "LibraryPath";
            public static readonly string CreateFlags = "CreateFlags";
            public static readonly string EngineFlags = "EngineFlags";
            public static readonly string SubstitutionFlags = "SubstitutionFlags";
            public static readonly string EventFlags = "EventFlags";
            public static readonly string ExpressionFlags = "ExpressionFlags";
            public static readonly string TrustedSetup = "TrustedSetup";
            public static readonly string NeedConsole = "NeedConsole";
            #endregion

            ///////////////////////////////////////////////////////////////////

            public static NameValueCollection AppSettings
            {
                get { return ConfigurationManager.AppSettings; }
            }

            ///////////////////////////////////////////////////////////////////

            public static bool GetTrustedSetup(
                NameValueCollection appSettings
                )
            {
                try
                {
                    if (Utility.GetEnvironmentVariable(
                            TrustedSetup, true, false) != null)
                    {
                        return true;
                    }
                    else if (appSettings != null)
                    {
                        string value = appSettings[TrustedSetup];

                        if (!String.IsNullOrEmpty(value))
                        {
                            bool result = false;

                            return bool.TryParse(value, out result) && result;
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            public static bool GetNeedConsole(
                NameValueCollection appSettings
                )
            {
                return GetNeedConsole(appSettings, DefaultConsole);
            }

            ///////////////////////////////////////////////////////////////////

            private static bool GetNeedConsole(
                NameValueCollection appSettings,
                bool @default
                )
            {
                //
                // HACK: By default, assume that a console-based host is not
                //       available.  Then, attempt to check and see if the
                //       user believes that one is available.  We use this
                //       very clumsy method because ASP.NET does not seem to
                //       expose an easy way for us to determine if we have a
                //       console-like host available to output diagnostic
                //       [and other] information to.
                //
                try
                {
                    if (@default)
                    {
                        if (Utility.GetEnvironmentVariable(
                                EnvVars.NoConsole, true, false) != null)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if ((Utility.GetEnvironmentVariable(
                                NeedConsole, true, false) != null) ||
                            (Utility.GetEnvironmentVariable(
                                EnvVars.Console, true, false) != null))
                        {
                            return true;
                        }
                    }

                    if (appSettings != null)
                    {
                        string value = appSettings[NeedConsole];

                        if (!String.IsNullOrEmpty(value))
                        {
                            bool result = false;

                            return bool.TryParse(value, out result) && result;
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static CreateFlags GetCreateFlags(
                NameValueCollection appSettings
                )
            {
                return GetCreateFlags(appSettings, DefaultCreateFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static CreateFlags GetCreateFlags(
                NameValueCollection appSettings,
                CreateFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        CreateFlags, true, true);

                    if (String.IsNullOrEmpty(value) && (appSettings != null))
                        value = appSettings[CreateFlags];

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(CreateFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is CreateFlags)
                            return (CreateFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static EngineFlags GetEngineFlags(
                NameValueCollection appSettings
                )
            {
                return GetEngineFlags(appSettings, DefaultEngineFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static EngineFlags GetEngineFlags(
                NameValueCollection appSettings,
                EngineFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        EngineFlags, true, true);

                    if (String.IsNullOrEmpty(value) && (appSettings != null))
                        value = appSettings[EngineFlags];

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(EngineFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is EngineFlags)
                            return (EngineFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static SubstitutionFlags GetSubstitutionFlags(
                NameValueCollection appSettings
                )
            {
                return GetSubstitutionFlags(
                    appSettings, DefaultSubstitutionFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static SubstitutionFlags GetSubstitutionFlags(
                NameValueCollection appSettings,
                SubstitutionFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        SubstitutionFlags, true, true);

                    if (String.IsNullOrEmpty(value) && (appSettings != null))
                        value = appSettings[SubstitutionFlags];

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(SubstitutionFlags),
                            @default.ToString(), value, null, true,
                            true, true, ref error);

                        if (enumValue is SubstitutionFlags)
                            return (SubstitutionFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static EventFlags GetEventFlags(
                NameValueCollection appSettings
                )
            {
                return GetEventFlags(appSettings, DefaultEventFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static EventFlags GetEventFlags(
                NameValueCollection appSettings,
                EventFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        EventFlags, true, true);

                    if (String.IsNullOrEmpty(value) && (appSettings != null))
                        value = appSettings[EventFlags];

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(EventFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is EventFlags)
                            return (EventFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static ExpressionFlags GetExpressionFlags(
                NameValueCollection appSettings
                )
            {
                return GetExpressionFlags(appSettings, DefaultExpressionFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static ExpressionFlags GetExpressionFlags(
                NameValueCollection appSettings,
                ExpressionFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        ExpressionFlags, true, true);

                    if (String.IsNullOrEmpty(value) && (appSettings != null))
                        value = appSettings[ExpressionFlags];

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(ExpressionFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is ExpressionFlags)
                            return (ExpressionFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Interpreter Creation & Setup Methods
        private static bool SetupLibraryPath(
            NameValueCollection appSettings,
            ref Result error
            )
        {
            try
            {
                if (appSettings != null)
                {
                    //
                    // HACK: We must make sure that Eagle can find the script
                    //       library to initialize the created interpreter(s).
                    //
                    string directory = appSettings[Settings.LibraryPath];

                    if (String.IsNullOrEmpty(directory))
                        directory = appSettings[Settings.EagleLibrary];

                    if (!String.IsNullOrEmpty(directory))
                    {
                        //
                        // NOTE: Expand any environment variable references
                        //       that may be present in the path.
                        //
                        directory = Utility.ExpandEnvironmentVariables(directory);

#if false
                        //
                        // NOTE: Set the library path to the location from our
                        //       application configuration.  This will only work
                        //       if the Interpreter type has not yet been loaded
                        //       from the Eagle assembly.
                        //
                        Utility.SetEnvironmentVariable(EnvVars.EagleLibrary, directory);
#else
                        //
                        // NOTE: This is the "preferred" way of setting the
                        //       library path as it does not depend on the
                        //       Interpreter type not having been loaded from
                        //       the Eagle assembly yet.
                        //
                        Utility.SetLibraryPath(directory, true);
#endif

                        return true;
                    }
#if true
                    else
                    {
                        //
                        // NOTE: This is the "preferred" way to have Eagle
                        //       automatically detect the library path to use.
                        //       The assembly location is used along with the
                        //       various Eagle-related environment variables
                        //       and/or registry settings.
                        //
                        return Utility.DetectLibraryPath(
                            assembly, null, DetectFlags.Default);
                    }
#endif
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SetupInterpreter(
            NameValueCollection appSettings,
            Interpreter interpreter,
            ref Result result
            )
        {
            try
            {
                string value = Utility.GetEnvironmentVariable(
                    Settings.SetupScript, true, true);

                if ((value == null) && (appSettings != null))
                    value = appSettings[Settings.SetupScript];

                //
                // NOTE: Were we able to get the value from somewhere?
                //
                if (value != null)
                {
                    //
                    // NOTE: Get the normal engine flags for script
                    //       evaluation.
                    //
                    EngineFlags engineFlags = Settings.GetEngineFlags(
                        appSettings);

                    //
                    // NOTE: If the setup script is considered "trusted"
                    //       add the IgnoreHidden flag to override the
                    //       normal safe interpreter behavior.
                    //
                    if (Settings.GetTrustedSetup(appSettings))
                        engineFlags |= EngineFlags.IgnoreHidden;

                    //
                    // NOTE: Evaluate the setup script and return the
                    //       results to the caller verbatim.
                    //
                    return Engine.EvaluateScript(
                        interpreter, value, engineFlags,
                        Settings.GetSubstitutionFlags(appSettings),
                        Settings.GetEventFlags(appSettings),
                        Settings.GetExpressionFlags(appSettings),
                        ref result);
                }

                //
                // NOTE: No setup script to evaluate, this is fine.
                //
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Interpreter CreateInterpreter(
            NameValueCollection appSettings,
            IEnumerable<string> args,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;
            Interpreter interpreter = null;

            try
            {
                if (SetupLibraryPath(appSettings, ref result))
                {
                    bool console = Settings.GetNeedConsole(appSettings);

                    CreateFlags createFlags =
                        Interpreter.GetStartupCreateFlags(
                        args, Settings.GetCreateFlags(appSettings),
                        OptionOriginFlags.Any, console, true);

                    string text = null;

                    code = Interpreter.GetStartupPreInitializeText(
                        args, createFlags, OptionOriginFlags.Standard,
                        console, true, ref text, ref result);

                    string libraryPath = null;

                    if (code == ReturnCode.Ok)
                    {
                        code = Interpreter.GetStartupLibraryPath(
                            args, createFlags, OptionOriginFlags.Standard,
                            console, true, ref libraryPath, ref result);
                    }

                    if (code == ReturnCode.Ok)
                    {
                        interpreter = Interpreter.Create(
                            args, createFlags, libraryPath, ref result);

                        if (interpreter != null)
                        {
                            code = Interpreter.ProcessStartupOptions(
                                interpreter, args, createFlags,
                                OptionOriginFlags.Standard, console,
                                true, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                code = SetupInterpreter(
                                    appSettings, interpreter, ref result);
                            }
                        }
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (interpreter != null))
                {
                    interpreter.Dispose();
                    interpreter = null;
                }
            }

            return interpreter;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEagle Members
        public MethodResult EvaluateExpression(
            string text
            )
        {
            return EvaluateExpressionWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateExpressionWithArgs(
            string text,
            Collection<string> args
            )
        {
            NameValueCollection appSettings = Settings.AppSettings;
            ReturnCode code;
            Result result = null;

            using (Interpreter interpreter = CreateInterpreter(
                    appSettings, args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.EvaluateExpression(interpreter, text,
                        Settings.GetEngineFlags(appSettings),
                        Settings.GetSubstitutionFlags(appSettings),
                        Settings.GetEventFlags(appSettings),
                        Settings.GetExpressionFlags(appSettings),
                        ref result);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateScript(
            string text
            )
        {
            return EvaluateScriptWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateScriptWithArgs(
            string text,
            Collection<string> args
            )
        {
            NameValueCollection appSettings = Settings.AppSettings;
            ReturnCode code;
            Result result = null;
            int errorLine = 0;

            using (Interpreter interpreter = CreateInterpreter(
                    appSettings, args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.EvaluateScript(interpreter, text,
                        Settings.GetEngineFlags(appSettings),
                        Settings.GetSubstitutionFlags(appSettings),
                        Settings.GetEventFlags(appSettings),
                        Settings.GetExpressionFlags(appSettings),
                        ref result, ref errorLine);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateFile(
            string fileName
            )
        {
            return EvaluateFileWithArgs(fileName, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateFileWithArgs(
            string fileName,
            Collection<string> args
            )
        {
            NameValueCollection appSettings = Settings.AppSettings;
            ReturnCode code;
            Result result = null;
            int errorLine = 0;

            using (Interpreter interpreter = CreateInterpreter(
                    appSettings, args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.EvaluateFile(interpreter, fileName,
                        Settings.GetEngineFlags(appSettings),
                        Settings.GetSubstitutionFlags(appSettings),
                        Settings.GetEventFlags(appSettings),
                        Settings.GetExpressionFlags(appSettings),
                        ref result, ref errorLine);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteString(
            string text
            )
        {
            return SubstituteStringWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteStringWithArgs(
            string text,
            Collection<string> args
            )
        {
            NameValueCollection appSettings = Settings.AppSettings;
            ReturnCode code;
            Result result = null;

            using (Interpreter interpreter = CreateInterpreter(
                    appSettings, args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.SubstituteString(interpreter, text,
                        Settings.GetEngineFlags(appSettings),
                        Settings.GetSubstitutionFlags(appSettings),
                        Settings.GetEventFlags(appSettings),
                        Settings.GetExpressionFlags(appSettings),
                        ref result);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteFile(
            string fileName
            )
        {
            return SubstituteFileWithArgs(fileName, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteFileWithArgs(
            string fileName,
            Collection<string> args
            )
        {
            NameValueCollection appSettings = Settings.AppSettings;
            ReturnCode code;
            Result result = null;

            using (Interpreter interpreter = CreateInterpreter(
                    appSettings, args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.SubstituteFile(interpreter, fileName,
                        Settings.GetEngineFlags(appSettings),
                        Settings.GetSubstitutionFlags(appSettings),
                        Settings.GetEventFlags(appSettings),
                        Settings.GetExpressionFlags(appSettings),
                        ref result);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSuccess(
            ReturnCode code,
            bool exceptions
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public string FormatResult(
            ReturnCode code,
            string result,
            int errorLine
            )
        {
            return Utility.FormatResult(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public string FormatMethodResult(MethodResult result)
        {
            return (result != null) ?
                FormatResult(result.ReturnCode, result.Result, result.ErrorLine) :
                null;
        }
        #endregion
    }
}
