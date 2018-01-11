/*
 * ScriptOps.cs --
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("81c28526-dd5a-4ba8-b056-b62bbd3b8d90")]
    internal static class ScriptOps
    {
        #region Private Constants
        private static readonly string DefaultVariableValue = null;
        private static readonly string DefaultGetVariableValue = null;
        private static readonly string DefaultSetVariableValue = null;
        private static readonly string DefaultUnsetVariableValue = null;

        ///////////////////////////////////////////////////////////////////////

        #region Security Integration Support Constants
        private static readonly string HarpyPackageIndexPattern =
            String.Format("*/Harpy*{0}/*", GlobalState.GetPackageVersion());

        private static readonly string BadgePackageIndexPattern =
            String.Format("*/Badge*{0}/*", GlobalState.GetPackageVersion());

        ///////////////////////////////////////////////////////////////////////

        private const string EnableSecurityScriptName = "enableSecurity";
        private const string DisableSecurityScriptName = "disableSecurity";
        private const string RemoveCommandsScriptName = "removeCommands";

        ///////////////////////////////////////////////////////////////////////

        #region Security Package Loader Warning Message
#if !DEBUG
        private static readonly string DefaultShellFileName =
            "EagleShell" + FileExtension.Executable;

        private static readonly string DefaultShell32FileName =
            DefaultShellFileName + "32" + FileExtension.Executable;

        private const string SecurityErrorMessage =
            "It is likely that the security plugins will fail to load in this configuration,\n" +
            "please use one of the following supported workarounds:\n\n" +
            "{0}1. Force this process to run as 32-bit, e.g. using \"{1}\",\n"+
            "{0}   etc.\n\n" +
            "{0}2. Modify \"{2}{3}\", setting its \"supportedRuntime\"\n" +
            "{0}   version to \"v4.0.30319\" (or higher).\n\n" +
            "{0}3. Set the \"{4}\" environment variable (to anything); however,\n" +
            "{0}   while this will bypass this error message, it will do nothing to\n" +
            "{0}   address the underlying issue, should it still exist.\n";
#endif
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Variable Name Lists
        private static StringDictionary defaultVariableNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable "Safe" Name / Element Lists
        private static StringDictionary safeVariableNames;
        private static StringDictionary safeTclPlatformElementNames;
        private static StringDictionary safeEaglePlatformElementNames;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Security Integration Support Methods
        private static void GetSecurityPackageIndexPaths(
            Interpreter interpreter,
            ref StringList paths
            )
        {
            StringList autoPathList = GlobalState.GetAutoPathList(
                interpreter, false);

            if (autoPathList == null)
                return;

            string indexFileName = FormatOps.ScriptTypeToFileName(
                ScriptTypes.PackageIndex, PackageType.None, true, false);

            if (String.IsNullOrEmpty(indexFileName))
                return;

            foreach (string path in autoPathList)
            {
                string[] fileNames = Directory.GetFiles(
                    PathOps.GetNativePath(path), indexFileName,
                    SearchOption.AllDirectories);

                if ((fileNames == null) || (fileNames.Length == 0))
                    continue;

                foreach (string fileName in fileNames)
                {
                    if (String.IsNullOrEmpty(fileName))
                        continue;

                    if (Parser.StringMatch(
                            interpreter, PathOps.GetUnixPath(fileName), 0,
                            HarpyPackageIndexPattern, 0, PathOps.NoCase) ||
                        Parser.StringMatch(
                            interpreter, PathOps.GetUnixPath(fileName), 0,
                            BadgePackageIndexPattern, 0, PathOps.NoCase))
                    {
                        if (paths == null)
                            paths = new StringList();

                        paths.Add(Path.GetDirectoryName(fileName));
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveSecurityPackageIndexes(
            StringList paths,
            PackageIndexDictionary packageIndexes
            )
        {
            if ((paths == null) || (packageIndexes == null))
                return false;

            string indexFileName = FormatOps.ScriptTypeToFileName(
                ScriptTypes.PackageIndex, PackageType.None, true, false);

            if (String.IsNullOrEmpty(indexFileName))
                return false;

            foreach (string path in paths)
            {
                if (String.IsNullOrEmpty(path))
                    continue;

                if (!packageIndexes.ContainsKey(Path.Combine(
                        path, indexFileName)))
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode MaybeFindSecurityPackageIndexes(
            Interpreter interpreter,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // HACK: If the interpreter was already initialized, skip finding
            //       the security package indexes because they should already
            //       be loaded.
            //
            if (interpreter.InternalInitialized && !interpreter.IsSafe())
                return ReturnCode.Ok;

            StringList paths = null;

            /* NO RESULT */
            GetSecurityPackageIndexPaths(interpreter, ref paths);

            if (paths == null)
                return ReturnCode.Ok;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                PackageIndexDictionary packageIndexes =
                    interpreter.CopyPackageIndexes();

                //
                // HACK: If all the security package indexes are present in
                //       the interpreter, skip trying to find and load them
                //       again.
                //
                if (HaveSecurityPackageIndexes(paths, packageIndexes))
                    return ReturnCode.Ok;

                PackageIndexFlags savedPackageIndexFlags =
                    interpreter.PackageIndexFlags;

                try
                {
                    interpreter.PackageIndexFlags =
                        PackageIndexFlags.SecurityPackage;

                    PackageFlags savedPackageFlags = interpreter.PackageFlags;

                    try
                    {
                        interpreter.PackageFlags |=
                            PackageFlags.SecurityPackageMask;

                        if (PackageOps.FindAll(
                                interpreter, paths,
                                interpreter.PackageIndexFlags,
                                ref packageIndexes,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }
                    }
                    finally
                    {
                        interpreter.PackageFlags = savedPackageFlags;
                    }
                }
                finally
                {
                    interpreter.PackageIndexFlags = savedPackageIndexFlags;
                }

                interpreter.PackageIndexes = packageIndexes;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if !DEBUG
        private static string GetShellFileName(
            bool wow64
            )
        {
            string fileNameOnly = null;

            try
            {
                fileNameOnly = Path.GetFileName(PathOps.GetExecutableName());
            }
            catch
            {
                // do nothing.
            }

            if (String.IsNullOrEmpty(fileNameOnly))
            {
                return wow64 ?
                    DefaultShell32FileName : DefaultShellFileName;
            }

            if (!wow64)
                return fileNameOnly;

            return String.Format(
                "{0}32{1}", Path.GetFileNameWithoutExtension(fileNameOnly),
                FileExtension.Executable);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool AreSecurityPackagesLikelyBroken(
            ref Result error
            )
        {
            //
            // HACK: When running as a 64-bit process on the .NET Framework
            //       2.0 runtime, the security plugins will not load due to
            //       broken obfuscation provided by LogicNP Software, which
            //       they refuse to fix.  Sorry guys, please fix your code,
            //       which is apparently broken on 64-bit .NET 2.0.  It is
            //       possible to skip this error by setting the environment
            //       variable "ForceSecurity" [to anything]; however, that
            //       will only enable this class to *attempt* to loading of
            //       the security plugins, which will (most likely) still
            //       fail due to the aforementioned reasons.
            //
            // NOTE: Eagle Enterprise Edition licensees may request, and are
            //       fully entitled to receive, non-obfuscated binaries for
            //       all Eagle Enterprise Edition plugins.  Additionally,
            //       Eagle Enterprise Edition source code licensees are
            //       permitted to customize the plugins and/or rebuild them
            //       without any obfuscation.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.ForceSecurity))
            {
                return false;
            }

            ///////////////////////////////////////////////////////////////////

            #region Release Builds Only
#if !DEBUG
            //
            // BUGBUG: Technically, the (release build) security plugins may
            //         not load right on Mono either.
            //
            if (PlatformOps.Is64BitProcess() &&
                CommonOps.Runtime.IsRuntime20() && !CommonOps.Runtime.IsMono())
            {
                error = String.Format(SecurityErrorMessage,
                    Characters.HorizontalTab, GetShellFileName(true),
                    GetShellFileName(false), FileExtension.Configuration,
                    EnvVars.ForceSecurity);

                return true;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode EnableOrDisableSecurity(
            Interpreter interpreter,
            bool enable,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (AreSecurityPackagesLikelyBroken(ref error))
                return ReturnCode.Error;

            if (MaybeFindSecurityPackageIndexes(
                    interpreter, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: It should be noted that the "enableSecurity" and/or
            //       "disableSecurity" script must be signed and trusted
            //       if the interpreter used is configured with security
            //       enabled.  The very first time the "enableSecurity"
            //       is evaluated here, its signature will generally not
            //       be checked (i.e. because it is the script used to
            //       enable signed script policy enforcement); however,
            //       any subsequent attempts to evaluate it in the same
            //       interpreter may cause its signature to be checked,
            //       (i.e. unless signed script policy enforcement has
            //       been disabled in the meantime).  Since the script
            //       flags used here should force the designated script
            //       to be loaded only from within the compiled core
            //       library assembly itself (i.e. which is typically
            //       strong name and/or Authenticode signed), we should
            //       be OK security-wise.  It should be noted that this
            //       assumption requires the core library to be built
            //       with the embedded library option enabled in order
            //       for it to be valid.
            //
            string name = enable ?
                EnableSecurityScriptName : DisableSecurityScriptName;

            ScriptFlags scriptFlags =
                ScriptFlags.CoreLibrarySecurityRequiredFile;

            IClientData clientData = ClientData.Empty;
            Result localResult = null;

            if (interpreter.GetScript(
                    name, ref scriptFlags, ref clientData,
                    ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            //
            // NOTE: This script should use several "unsafe" commands
            //       (i.e. within Harpy); therefore, evaluate it as
            //       an "unsafe" one.
            //
            string text = localResult;

            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.File, true))
            {
                if (interpreter.EvaluateTrustedFile(
                        null, text, TrustFlags.SecurityPackage,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (interpreter.EvaluateTrustedScript(
                        text, TrustFlags.SecurityPackage,
                        ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Location Support Methods
        public static ReturnCode GetAndCheckProcedureLocation(
            Interpreter interpreter,
            IProcedure procedure,
            ref IScriptLocation location,
            ref Result error
            )
        {
            ReturnCode code = GetProcedureLocation(
                interpreter, procedure, ref location, ref error);

            if (code != ReturnCode.Ok)
                return code;

            ProcedureFlags procedureFlags = procedure.Flags;

            if (!FlagOps.HasFlags(
                    procedureFlags, ProcedureFlags.Private, true))
            {
                return ReturnCode.Ok;
            }

            IScriptLocation scriptLocation = null;
            ICallFrame frame = interpreter.ProcedureFrame;

            if (frame != null)
            {
                //
                // NOTE: There is an active procedure, attempt to grab
                //       the location from it.
                //
                IProcedure scriptProcedure = frame.Execute as IProcedure;

                if (scriptProcedure == null)
                {
                    error = "invalid procedure in procedure frame";
                    return ReturnCode.Error;
                }

                scriptLocation = scriptProcedure.Location;
            }
            else
            {
                //
                // NOTE: No active procedure, use script scope.
                //
                code = GetLocation(
                    interpreter, true, ref scriptLocation, ref error);

                if (code != ReturnCode.Ok)
                    return code;
            }

            IScriptLocation procedureLocation = (location != null) &&
                (location.FileName != null) ? location : null;

            if (!ScriptLocation.MatchFileName(
                    interpreter, procedureLocation, scriptLocation, true))
            {
                error = "cannot execute private procedure, " +
                    "script location mismatch";

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetProcedureLocation(
            Interpreter interpreter,
            IProcedure procedure,
            ref IScriptLocation location,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (procedure == null)
            {
                error = "invalid procedure";
                return ReturnCode.Error;
            }

            if (FlagOps.HasFlags(
                    procedure.Flags, ProcedureFlags.ScriptLocation, true))
            {
                return GetLocation(
                    interpreter, false, ref location, ref error);
            }

            location = procedure.Location;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLocation(
            Interpreter interpreter,
            ArgumentList arguments,
            int startIndex,
            ref IScriptLocation location,
            ref Result error
            )
        {
            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            if ((startIndex < 0) || (startIndex >= arguments.Count))
            {
                error = "argument index out of range";
                return ReturnCode.Error;
            }

            Argument firstArgument = arguments[startIndex];
            Argument lastArgument = arguments[arguments.Count - 1];

            if ((firstArgument == null) && (lastArgument == null))
            {
                location = ScriptLocation.Create((IScriptLocation)null);
                return ReturnCode.Ok;
            }

            if (firstArgument != null)
            {
                location = ScriptLocation.Create(interpreter,
                    firstArgument.FileName, firstArgument.StartLine,
                    (lastArgument != null) ? lastArgument.EndLine :
                        firstArgument.EndLine,
                    firstArgument.ViaSource);
            }
            else
            {
                location = ScriptLocation.Create(interpreter,
                    lastArgument.FileName, lastArgument.StartLine,
                    lastArgument.EndLine, lastArgument.ViaSource);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLocation(
            Interpreter interpreter,
            bool viaSource,
            bool scrub,
            ref string fileName,
            ref Result error
            )
        {
            int currentLine = Parser.UnknownLine;

            return GetLocation(
                interpreter, viaSource, scrub, ref fileName,
                ref currentLine, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLocation(
            Interpreter interpreter,
            bool viaSource,
            bool scrub,
            ref string fileName,
            ref int currentLine,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                ReturnCode code;
                IScriptLocation location = null;

                code = GetLocation(
                    interpreter, viaSource, ref location, ref error);

                if (code == ReturnCode.Ok)
                {
                    string scriptFileName = (location != null) ?
                        location.FileName : null;

                    if (scrub && (scriptFileName != null))
                        fileName = PathOps.ScrubPath(
                            GlobalState.GetBasePath(), scriptFileName);
                    else
                        fileName = scriptFileName; /* NOTE: May be null. */

                    currentLine = (location != null) ?
                        location.StartLine : Parser.UnknownLine;

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetLocation(
            Interpreter interpreter,
            bool viaSource,
            ref IScriptLocation location,
            ref Result error
            )
        {
            if (interpreter != null)
            {
#if !THREADING
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
#endif
                {
                    //
                    // NOTE: Grab whatever the caller previously manually
                    //       set the current script file name to, if any.
                    //
                    location = interpreter.ScriptLocation;

                    if (location == null)
                    {
                        ScriptLocationList locations =
                            interpreter.ScriptLocations;

                        if (locations != null)
                        {
                            int count = locations.Count;

                            if (count > 0)
                            {
                                for (int index = count - 1; index >= 0; index--)
                                {
                                    IScriptLocation thisLocation = locations[index];

                                    if (thisLocation == null)
                                        continue;

                                    if (!viaSource || thisLocation.ViaSource)
                                    {
                                        //
                                        // NOTE: Grab the last (most recent) script
                                        //       location from the stack of active
                                        //       script locations that matches the
                                        //       via [source] flag set by the caller.
                                        //
                                        location = thisLocation;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetScriptPath(
            Interpreter interpreter, /* in */
            bool directoryOnly,      /* in */
            ref string path,         /* out */
            ref Result error         /* out */
            )
        {
            try
            {
                string fileName = null;

                if (GetLocation(
                        interpreter, true, false, ref fileName,
                        ref error) == ReturnCode.Ok)
                {
                    if (directoryOnly)
                        path = Path.GetDirectoryName(fileName);
                    else
                        path = fileName;

                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Settings Support Methods
        //
        // WARNING: This method is designed to check the *COMPLETE* list of
        //          system variables that may be set in created interpreters.
        //          If additional variables need to be set during interpreter
        //          creation, they will need to be added here as well.
        //
        private static bool IsDefaultVariableName(
            string name,
            bool anyReserved
            )
        {
            lock (syncRoot)
            {
                if (defaultVariableNames == null)
                {
                    defaultVariableNames = new StringDictionary(new string[] {
                        TclVars.ShellArgumentCount,
                        TclVars.ShellArguments,
                        TclVars.ShellArgument0,
                        TclVars.AutoExecutables,
                        TclVars.AutoIndex,
                        TclVars.AutoNoExecute,
                        TclVars.AutoNoLoad,
                        TclVars.AutoOldPath,
                        TclVars.AutoPath,
                        TclVars.AutoSourcePath,
                        Vars.Platform.Name,
                        Vars.Debugger,
                        Vars.Paths,
                        Vars.Shell,
                        Vars.Tests,
                        TclVars.Environment,
                        TclVars.ErrorCode,
                        TclVars.ErrorInfo,
                        Vars.Null,
                        TclVars.Interactive,
                        TclVars.InteractiveLoops,
                        TclVars.Library,
                        TclVars.LibraryPath,
                        TclVars.NonWordCharacters,
                        TclVars.PatchLevelName,
                        TclVars.PackagePath,
                        TclVars.Platform.Name,
                        TclVars.PrecisionName,
                        TclVars.Prompt1,
                        TclVars.Prompt2,
                        TclVars.RunCommandsFileName,
                        TclVars.RunCommandsResourceName,
                        TclVars.ShellLibrary,
                        TclVars.TraceCompile,
                        TclVars.TraceExecute,
                        TclVars.VersionName,
                        TclVars.WordCharacters
                    }, true, false);
                }

                if (name != null)
                {
                    if (defaultVariableNames.ContainsKey(name))
                        return true;

                    if (anyReserved)
                    {
                        //
                        // NOTE: Check if the name starts with "tcl_".
                        //
                        if (name.StartsWith(TclVars.Prefix,
                                StringOps.SystemStringComparisonType))
                        {
                            return true;
                        }

                        //
                        // NOTE: Check if the name starts with "eagle_".
                        //
                        if (name.StartsWith(Vars.Prefix,
                                StringOps.SystemStringComparisonType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode PrepareForStaticData(
            Interpreter interpreter, /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // NOTE: It should be noted that the "removeCommands" script
            //       must be signed and trusted if the interpreter used
            //       is configured with security enabled.
            //
            ScriptFlags scriptFlags =
                ScriptFlags.CoreLibrarySecurityRequiredFile;

            IClientData clientData = ClientData.Empty;
            Result localResult = null;

            if (interpreter.GetScript(
                    RemoveCommandsScriptName, ref scriptFlags,
                    ref clientData, ref localResult) != ReturnCode.Ok)
            {
                error = localResult;
                return ReturnCode.Error;
            }

            //
            // NOTE: This script should not use any "unsafe" commands;
            //       therefore, do not evaluate it as an "unsafe" one.
            //
            string text = localResult;

            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.File, true))
            {
                if (interpreter.EvaluateFile(
                        text, ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }
            else
            {
                if (interpreter.EvaluateScript(
                        text, ref localResult) != ReturnCode.Ok)
                {
                    error = localResult;
                    return ReturnCode.Error;
                }
            }

            interpreter.RemoveNonBaseObjects(true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadSettingsViaFile(
            Interpreter interpreter,       /* in */
            IClientData clientData,        /* in */
            string fileName,               /* in */
            ScriptDataFlags flags,         /* in */
            ref StringDictionary settings, /* in, out */
            ref Result error               /* out */
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);

            try
            {
                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    bool safe = FlagOps.HasFlags(flags,
                        ScriptDataFlags.UseSafeInterpreter, true);

                    CreateFlags createFlags = safe ?
                        CreateFlags.SafeSettingsUse : CreateFlags.SettingsUse;

                    bool disableSecurity = FlagOps.HasFlags(flags,
                        ScriptDataFlags.DisableSecurity, true);

                    InitializeFlags initializeFlags;

                    if (interpreter != null)
                        initializeFlags = interpreter.DefaultInitializeFlags;
                    else
                        initializeFlags = InitializeFlags.Default;

                    if (disableSecurity)
                        initializeFlags &= ~InitializeFlags.Security;

                    using (Interpreter localInterpreter = Interpreter.Create(
                            clientData, null, createFlags, initializeFlags,
                            ref result))
                    {
                        if (localInterpreter == null)
                        {
                            error = result;
                            code = ReturnCode.Error;

                            return code;
                        }

                        bool staticData = FlagOps.HasFlags(flags,
                            ScriptDataFlags.UseStaticOnly, true);

                        if (staticData)
                        {
                            code = PrepareForStaticData(
                                localInterpreter, ref error);

                            if (code != ReturnCode.Ok)
                                return code;
                        }

#if NETWORK
                        bool trusted = FlagOps.HasFlags(flags,
                            ScriptDataFlags.UseTrustedUri, true);

                        bool locked = false;
                        bool? wasTrusted = null;

                        try
                        {
                            if (trusted)
                            {
                                UpdateOps.TryLock(ref locked);

                                if (!locked)
                                {
                                    error = "unable to acquire update lock";
                                    return ReturnCode.Error;
                                }

                                wasTrusted = UpdateOps.IsTrusted();
                            }

                            if (wasTrusted != null)
                            {
                                code = UpdateOps.SetTrusted(true, ref error);

                                if (code != ReturnCode.Ok)
                                    return code;
                            }
#endif

                            code = localInterpreter.EvaluateFile(
                                fileName, ref result);

                            if (code != ReturnCode.Ok)
                            {
                                error = result;
                                return code;
                            }
#if NETWORK
                        }
                        finally
                        {
                            if (wasTrusted != null)
                            {
                                ReturnCode trustedCode;
                                Result trustedError = null;

                                trustedCode = UpdateOps.SetTrusted(
                                    (bool)wasTrusted, ref trustedError);

                                if (trustedCode != ReturnCode.Ok)
                                {
                                    DebugOps.Complain(
                                        localInterpreter, trustedCode,
                                        trustedError);
                                }
                            }

                            UpdateOps.ExitLock(ref locked);
                        }
#endif

                        lock (localInterpreter.SyncRoot) /* TRANSACTIONAL */
                        {
                            ICallFrame frame = localInterpreter.CurrentGlobalFrame;

                            if (frame == null)
                            {
                                error = "invalid call frame";
                                code = ReturnCode.Error;

                                return code;
                            }

                            VariableDictionary variables = frame.Variables;

                            if (variables == null)
                            {
                                error = "call frame does not support variables";
                                code = ReturnCode.Error;

                                return code;
                            }

                            //
                            // NOTE: Figure out which kind(s) of variables that
                            //       the caller wants saved to the resulting
                            //       settings dictionary.
                            //
                            bool copyScalars = FlagOps.HasFlags(flags,
                                ScriptDataFlags.CopyScalars, true);

                            bool copyArrays = FlagOps.HasFlags(flags,
                                ScriptDataFlags.CopyArrays, true);

                            bool errorOnScalar = FlagOps.HasFlags(flags,
                                ScriptDataFlags.ErrorOnScalar, true);

                            bool errorOnArray = FlagOps.HasFlags(flags,
                                ScriptDataFlags.ErrorOnArray, true);

                            //
                            // NOTE: If the caller specified some settings to be
                            //       loaded, use that list verbatim; otherwise,
                            //       add settings based on the global variables
                            //       now present in the interpreter that were NOT
                            //       put there during the interpreter creation
                            //       process.
                            //
                            StringDictionary localSettings = null;

                            if (settings != null)
                            {
                                localSettings = new StringDictionary(
                                    settings as IDictionary<string, string>);
                            }
                            else
                            {
                                localSettings = new StringDictionary();
                            }

                            bool existingOnly = FlagOps.HasFlags(flags,
                                ScriptDataFlags.ExistingOnly, true);

                            if (existingOnly && (localSettings.Count > 0))
                            {
                                //
                                // NOTE: Since a dictionary cannot be changed while it
                                //       is in use (by the foreach statement), we need
                                //       to create a copy of the variable names (only)
                                //       for the foreach statement to use.
                                //
                                StringList varNames = new StringList(localSettings.Keys);

                                foreach (string varName in varNames)
                                {
                                    IVariable variable;

                                    if (variables.TryGetValue(varName, out variable) &&
                                        (variable != null))
                                    {
                                        //
                                        // NOTE: A setting with this name may or may
                                        //       not already exist in the dictionary
                                        //       provided by the caller; therefore,
                                        //       add or update the setting.
                                        //
                                        ElementDictionary arrayValue = null;

                                        if (EntityOps.IsArray(variable, ref arrayValue))
                                        {
                                            if (copyArrays)
                                            {
                                                foreach (KeyValuePair<string, object> pair2 in arrayValue)
                                                {
                                                    string key = FormatOps.SettingKey(
                                                        variable, arrayValue, pair2.Key);

                                                    localSettings[key] = StringOps.GetStringFromObject(
                                                        pair2.Value);
                                                }
                                            }
                                            else if (errorOnArray)
                                            {
                                                error = String.Format(
                                                    "array variable \"{0}\" is not allowed",
                                                    varName);

                                                code = ReturnCode.Error;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (copyScalars)
                                            {
                                                localSettings[varName] = StringOps.GetStringFromObject(
                                                    variable.Value);
                                            }
                                            else if (errorOnScalar)
                                            {
                                                error = String.Format(
                                                    "scalar variable \"{0}\" is not allowed",
                                                    varName);

                                                code = ReturnCode.Error;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (KeyValuePair<string, IVariable> pair in variables)
                                {
                                    if (!IsDefaultVariableName(pair.Key, true))
                                    {
                                        IVariable variable = pair.Value;

                                        if (variable != null)
                                        {
                                            //
                                            // NOTE: A setting with this name may or may
                                            //       not already exist in the dictionary
                                            //       provided by the caller; therefore,
                                            //       add or update the setting.
                                            //
                                            ElementDictionary arrayValue = null;

                                            if (EntityOps.IsArray(variable, ref arrayValue))
                                            {
                                                if (copyArrays)
                                                {
                                                    foreach (KeyValuePair<string, object> pair2 in arrayValue)
                                                    {
                                                        string key = FormatOps.SettingKey(
                                                            variable, arrayValue, pair2.Key);

                                                        localSettings[key] = StringOps.GetStringFromObject(
                                                            pair2.Value);
                                                    }
                                                }
                                                else if (errorOnArray)
                                                {
                                                    error = String.Format(
                                                        "array variable \"{0}\" is not allowed",
                                                        pair.Key);

                                                    code = ReturnCode.Error;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                if (copyScalars)
                                                {
                                                    localSettings[pair.Key] = StringOps.GetStringFromObject(
                                                        variable.Value);
                                                }
                                                else if (errorOnScalar)
                                                {
                                                    error = String.Format(
                                                        "scalar variable \"{0}\" is not allowed",
                                                        pair.Key);

                                                    code = ReturnCode.Error;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (code == ReturnCode.Ok)
                                settings = localSettings;
                        }
                    }
                }
                catch (Exception e)
                {
                    error = e;
                    code = ReturnCode.Error;
                }
                finally
                {
                    TraceOps.DebugTrace(String.Format(
                        "LoadSettingsViaFile: interpreter = {0}, " +
                        "fileName = {1}, flags = {2}, settings = {3}, " +
                        "code = {4}, result = {5}, error = {6}",
                        FormatOps.InterpreterNoThrow(interpreter),
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.WrapOrNull(flags),
                        FormatOps.KeysAndValues(settings, true, true, true),
                        code, FormatOps.WrapOrNull(true, true, result),
                        FormatOps.WrapOrNull(true, true, error)),
                        typeof(ScriptOps).Name, TracePriority.EngineDebug);
                }

                return code;
            }
            finally
            {
                GlobalState.PopActiveInterpreter();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTemporaryFileName(
            ref string fileName, /* out */
            ref Result error     /* out */
            )
        {
            ReturnCode code = ReturnCode.Error;
            string[] fileNames = { null, null };

            try
            {
                //
                // NOTE: First, just obtain a temporary file name from the
                //       operating system.
                //
                fileNames[0] = Path.GetTempFileName(); /* throw */

                if (!String.IsNullOrEmpty(fileNames[0]))
                {
                    //
                    // NOTE: Next, append the script file extension (i.e.
                    //       ".eagle") to it.
                    //
                    fileNames[1] = String.Format(
                        "{0}{1}", fileNames[0], FileExtension.Script);

                    //
                    // NOTE: Finally, move the temporary file, atomically,
                    //       to the new name.
                    //
                    File.Move(fileNames[0], fileNames[1]); /* throw */

                    //
                    // NOTE: If we got this far, everything should be
                    //       completely OK.
                    //
                    fileName = fileNames[1];
                    code = ReturnCode.Ok;
                }
                else
                {
                    error = "invalid temporary file name";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                //
                // NOTE: If we created a temporary file, always delete it
                //       prior to returning from this method.
                //
                if (code != ReturnCode.Ok)
                {
                    if (fileNames[1] != null)
                    {
                        try
                        {
                            File.Delete(fileNames[1]); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptOps).Name,
                                TracePriority.FileSystemError);
                        }

                        fileNames[1] = null;
                    }

                    if (fileNames[0] != null)
                    {
                        try
                        {
                            File.Delete(fileNames[0]); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptOps).Name,
                                TracePriority.FileSystemError);
                        }

                        fileNames[0] = null;
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateTemporaryFile(
            string text,         /* in */
            Encoding encoding,   /* in: OPTIONAL */
            ref string fileName, /* out */
            ref Result error     /* out */
            )
        {
            if (text == null)
            {
                error = "invalid script";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Error;
            string localFileName = null;

            try
            {
                //
                // NOTE: First, attempt to obtain a temporary script file
                //       name (i.e. with an ".eagle" extension).
                //
                code = GetTemporaryFileName(ref localFileName, ref error);

                if (code != ReturnCode.Ok)
                    return code;

                //
                // NOTE: Next, attempt to write the specified script text
                //       into the temporary file, maybe using an encoding
                //       specified by the caller.
                //
                if (encoding != null)
                {
                    File.WriteAllText(
                        localFileName, text, encoding); /* throw */
                }
                else
                {
                    File.WriteAllText(localFileName, text); /* throw */
                }

                //
                // NOTE: If we got this far, everything should have
                //       succeeded.  Make sure the caller has the
                //       script file name containing their specified
                //       content.
                //
                fileName = localFileName;
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                //
                // NOTE: If we created a temporary file, always delete it
                //       prior to returning from this method.
                //
                if (code != ReturnCode.Ok)
                {
                    if (localFileName != null)
                    {
                        try
                        {
                            File.Delete(localFileName); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ScriptOps).Name,
                                TracePriority.FileSystemError);
                        }

                        localFileName = null;
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Library Support Methods
        public static ScriptFlags GetFlags(
            Interpreter interpreter, /* in */
            ScriptFlags scriptFlags, /* in */
            bool getScriptFile       /* in */
            )
        {
            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
            {
                if (getScriptFile && FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.UseDefault, true))
                {
                    scriptFlags |= ScriptFlags.UseDefaultGetScriptFile;
                }

                CreateFlags createFlags = interpreter.CreateFlags;

                if (FlagOps.HasFlags(
                        createFlags, CreateFlags.UseHostLibrary, true))
                {
                    scriptFlags &= ~ScriptFlags.PreferFileSystem;
                }

                return scriptFlags;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetFile(
            Interpreter interpreter,
            string directory,
            string name,
            ref ScriptFlags flags,
            ref IClientData clientData, /* NOT USED */
            ref Result result
            )
        {
            if (FlagOps.HasFlags(flags, ScriptFlags.NoLibraryFile, true))
            {
                result = String.Format(
                    "cannot find a suitable \"{0}\" script, file system " +
                    "disallowed", name);

                return ReturnCode.Error;
            }

            //
            // NOTE: Check for the script in the specified directory.
            //
            string fileName = PathOps.GetNativePath(PathOps.CombinePath(
                null, directory, name));

            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                flags |= ScriptFlags.File;
                result = fileName;

                return ReturnCode.Ok;
            }
            else if (!FlagOps.HasFlags(
                    flags, ScriptFlags.NoAutoPath, true))
            {
                //
                // NOTE: Check for the script on disk in the directories
                //       listed in the auto-path.
                //
                StringList autoPathList = GlobalState.GetAutoPathList(
                    interpreter, false);

                foreach (string path in autoPathList)
                {
                    fileName = PathOps.GetNativePath(PathOps.CombinePath(
                        null, path, name));

                    if (!String.IsNullOrEmpty(fileName) &&
                        File.Exists(fileName))
                    {
                        flags |= ScriptFlags.File;
                        result = fileName;

                        return ReturnCode.Ok;
                    }
                }

                result = String.Format(
                    "cannot find a suitable \"{0}\" script in \"{1}\"",
                    name, autoPathList);
            }
            else
            {
                result = String.Format(
                    "cannot find a suitable \"{0}\" script in \"{1}\"",
                    name, fileName);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetLibraryFile(
            Interpreter interpreter,
            string directory,
            string name,
            ref ScriptFlags flags,
            ref IClientData clientData,
            ref Result result,
            ref ResultList errors
            )
        {
            Result localResult = null;

            if (GetFile(
                    interpreter, directory, name, ref flags, ref clientData,
                    ref localResult) == ReturnCode.Ok)
            {
                result = localResult;

                return ReturnCode.Ok;
            }
            else if (localResult != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localResult);
            }

            //
            // TODO: Under what conditions should the following block of code
            //       be necessary?
            //
            if (!FlagOps.HasFlags(
                    flags, ScriptFlags.NoLibraryFileNameOnly, true))
            {
                localResult = null;

                if (PathOps.HasDirectory(name) && (GetFile(interpreter,
                        directory, PathOps.ScriptFileNameOnly(name), ref flags,
                        ref clientData, ref localResult) == ReturnCode.Ok))
                {
                    result = localResult;

                    return ReturnCode.Ok;
                }
                else if (localResult != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localResult);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLibrary(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string name,
            ref ScriptFlags flags,
            ref IClientData clientData,
            ref Result result
            )
        {
            ResultList errors = null;
            Result localResult = null;

            //
            // NOTE: Query the primary root directory where the Eagle core
            //       script library files should be found (e.g. something
            //       like "<dir>\lib\Eagle1.0\init.eagle", where "<dir>" is
            //       the value we are looking for)?
            //
            string directory = GlobalState.GetLibraryPath(interpreter);

            if (FlagOps.HasFlags(flags, ScriptFlags.PreferFileSystem, true))
            {
                localResult = null;

                if (GetLibraryFile(interpreter,
                        directory, name, ref flags, ref clientData,
                        ref localResult, ref errors) == ReturnCode.Ok)
                {
                    result = localResult;

                    return ReturnCode.Ok;
                }
                else
                {
                    localResult = null;

                    if (HostOps.GetScript(interpreter,
                            fileSystemHost, name, ref flags, ref clientData,
                            ref localResult) == ReturnCode.Ok)
                    {
                        result = localResult;

                        return ReturnCode.Ok;
                    }
                    else if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }
                }
            }
            else
            {
                localResult = null;

                if (HostOps.GetScript(interpreter,
                        fileSystemHost, name, ref flags, ref clientData,
                        ref localResult) == ReturnCode.Ok)
                {
                    result = localResult;

                    return ReturnCode.Ok;
                }
                else
                {
                    if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }

                    localResult = null;

                    if (GetLibraryFile(interpreter,
                            directory, name, ref flags, ref clientData,
                            ref localResult, ref errors) == ReturnCode.Ok)
                    {
                        result = localResult;

                        return ReturnCode.Ok;
                    }
                }
            }

            result = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetStartup(
            Interpreter interpreter,
            IFileSystemHost fileSystemHost,
            string name,
            ref ScriptFlags flags,
            ref IClientData clientData,
            ref Result result,
            ref ResultList errors
            )
        {
            Result localResult = null;

            if (HostOps.GetScript(
                    interpreter, fileSystemHost, name, ref flags,
                    ref clientData, ref localResult) == ReturnCode.Ok)
            {
                result = localResult;

                return ReturnCode.Ok;
            }
            else
            {
                if (localResult != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localResult);
                }

                localResult = null;

                if ((interpreter != null) && interpreter.GetVariableValue(
                        VariableFlags.GlobalOnly | VariableFlags.ViaShell,
                        name, ref localResult) == ReturnCode.Ok)
                {
                    string localName = localResult;

                    localResult = null;

                    if (HostOps.GetScript(
                            interpreter, fileSystemHost, localName, ref flags,
                            ref clientData, ref localResult) == ReturnCode.Ok)
                    {
                        if (FlagOps.HasFlags(flags, ScriptFlags.File, true) &&
                            !PathOps.IsRemoteUri(localResult))
                        {
                            result = PathOps.ResolveFullPath(
                                interpreter, localResult);
                        }
                        else
                        {
                            result = localResult;
                        }

                        return ReturnCode.Ok;
                    }
                    else if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }
                }
                else if (localResult != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localResult);
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Support Methods
        public static ISubCommand NewCommandSubCommand(
            string name,
            IClientData clientData,
            ICommand command,
            StringList scriptCommand,
            SubCommandFlags subCommandFlags
            )
        {
            return new _SubCommands.Command(new SubCommandData(
                name, null, null, ClientData.WrapOrReplace(clientData,
                scriptCommand), null, CommandFlags.None, subCommandFlags,
                command, 0));
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetNameForExecute(
            string name,
            ISubCommand subCommand
            ) /* MAY RETURN NULL */
        {
            if (subCommand == null)
                return name; /* NULL? */

            string commandName = null;
            ICommand command = subCommand.Command;

            if (command != null)
                commandName = command.Name;

            string subCommandName = subCommand.Name;

            if (commandName != null)
            {
                if (name != null)
                {
                    return StringList.MakeList(
                        commandName, name); /* NOT NULL */
                }
                else if (subCommandName != null)
                {
                    return StringList.MakeList(
                        commandName, subCommandName); /* NOT NULL */
                }
                else
                {
                    return commandName; /* NOT NULL */
                }
            }
            else if (name != null)
            {
                return name; /* NOT NULL */
            }
            else if (subCommandName != null)
            {
                return subCommandName; /* NOT NULL */
            }
            else
            {
                return null; /* NULL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ArgumentList GetArgumentsForExecute(
            ISubCommand subCommand, /* NOT USED */
            StringList scriptCommand,
            ArgumentList oldArguments,
            int oldStartIndex
            ) /* CANNOT RETURN NULL */
        {
            ArgumentList newArguments = new ArgumentList();

            if (scriptCommand != null)
                newArguments.AddRange(scriptCommand);

            if (oldArguments != null)
            {
                for (int index = oldStartIndex;
                        index < oldArguments.Count; index++)
                {
                    Argument oldArgument = oldArguments[index];

                    if (oldArgument == null)
                    {
                        newArguments.Add(null);
                        continue;
                    }

                    Argument newArgument = (Argument)oldArgument.Clone();

                    newArguments.Add(newArgument);
                }
            }

            return newArguments;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Ensemble Support Methods
        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in */
            SubCommandFilterCallback callback, /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name                    /* in, out */
            )
        {
            Result error = null;

            return SubCommandFromEnsemble(
                interpreter, ensemble, callback, strict, noCase,
                ref name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in */
            SubCommandFilterCallback callback, /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref Result error                   /* out */
            )
        {
            ISubCommand subCommand = null;

            return SubCommandFromEnsemble(
                interpreter, ensemble, callback, strict, noCase,
                ref name, ref subCommand, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in */
            SubCommandFilterCallback callback, /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref ISubCommand subCommand,        /* out */
            ref Result error                   /* out */
            )
        {
            return SubCommandFromEnsemble(interpreter,
                ensemble, PolicyOps.GetSubCommandsUnsafe(ensemble),
                callback, strict, noCase, ref name, ref subCommand,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in */
            EnsembleDictionary subCommands,    /* in */
            SubCommandFilterCallback callback, /* in */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref Result error                   /* out */
            )
        {
            ISubCommand subCommand = null;

            return SubCommandFromEnsemble(
                interpreter, ensemble, subCommands, callback, strict,
                noCase, ref name, ref subCommand, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,           /* in */
            IEnsemble ensemble,                /* in */
            EnsembleDictionary subCommands,    /* in */
            SubCommandFilterCallback callback, /* in: OPTIONAL */
            bool strict,                       /* in */
            bool noCase,                       /* in */
            ref string name,                   /* in, out */
            ref ISubCommand subCommand,        /* out */
            ref Result error                   /* out */
            )
        {
            //
            // NOTE: *WARNING* Empty sub-command names are allowed, please
            //       do not change this to "!String.IsNullOrEmpty".
            //
            if (name == null)
            {
                error = "invalid sub-command name";
                return ReturnCode.Error;
            }

            if (ensemble == null)
            {
                error = "invalid ensemble";
                return ReturnCode.Error;
            }

            if (subCommands == null)
            {
                error = "invalid sub-commands";
                return ReturnCode.Error;
            }

            if (subCommands.Count == 0)
            {
                if (strict)
                {
                    error = BadSubCommand(interpreter,
                        null, "option", name, (EnsembleDictionary)null,
                        null, null);

                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            //
            // NOTE: Always try for an exact match first.  Some callers
            //       of this method may require this behavior, e.g. the
            //       built-in sub-command policy implementation.  Upon
            //       a successful match here, skip setting the name
            //       output parameter because it already contains the
            //       correct value.
            //
            ISubCommand localSubCommand;

            if (subCommands.TryGetValue(name, out localSubCommand))
            {
                subCommand = localSubCommand;
                return ReturnCode.Ok;
            }

            bool exact = false;

            IList<KeyValuePair<string, ISubCommand>> matches =
                new List<KeyValuePair<string, ISubCommand>>();

            int nameLength = name.Length;

            StringComparison comparisonType = noCase ?
                StringOps.SystemNoCaseStringComparisonType :
                StringOps.SystemStringComparisonType;

            foreach (KeyValuePair<string, ISubCommand> pair in subCommands)
            {
                string key = pair.Key;

                if ((key == null) || (String.Compare(
                        key, 0, name, 0, nameLength, comparisonType) != 0))
                {
                    continue;
                }

                //
                // NOTE: Did we match the whole string, regardless of
                //       case?
                //
                bool whole = (key.Length == nameLength);

                //
                // NOTE: Was it an exact match or did we match at least
                //       one character in a partial match?
                //
                if (whole || (nameLength > 0))
                {
                    //
                    // NOTE: Store the exact or partial match in the
                    //       result list.
                    //
                    matches.Add(pair);

                    //
                    // NOTE: It was a match; however, was it exact?
                    //       This condition cannot be hit now unless
                    //       the noCase flag is set because the exact
                    //       matches are now short-circuited before
                    //       this loop.
                    //
                    if (whole)
                    {
                        //
                        // NOTE: For the purposes of this method, an
                        //       "exact" match requires a comparison
                        //       type of case-sensitive.
                        //
                        exact = !noCase;

                        //
                        // NOTE: Always stop on the first exact match.
                        //
                        break;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (callback != null)
            {
                //
                // NOTE: Use the callback to filter the list of matched
                //       sub-commands.  This is (always?) necessary just
                //       in case the caller specified an unfiltered list
                //       of sub-commands to match against.
                //
                Result localError = null;

                matches = callback(
                    interpreter, ensemble, matches, ref localError)
                    as IList<KeyValuePair<string, ISubCommand>>;

                //
                // NOTE: If the callback returns null, that indicates an
                //       unexpected failure and we cannot continue.
                //
                if (matches == null)
                {
                    if (localError != null)
                    {
                        error = localError;
                    }
                    else
                    {
                        //
                        // TODO: Good fallback error message?
                        //
                        error = "sub-command filter failed (matched)";
                    }

                    return ReturnCode.Error;
                }

                //
                // NOTE: If there are now no matches, use the callback to
                //       filter the list of available sub-commands, which
                //       will be used to build the error message (below).
                //
                if (matches.Count == 0)
                {
                    IList<KeyValuePair<string, ISubCommand>> localSubCommands;

                    localError = null;

                    localSubCommands = callback(
                        interpreter, ensemble, subCommands, ref localError)
                        as IList<KeyValuePair<string, ISubCommand>>;

                    //
                    // NOTE: If the callback returns null, that indicates an
                    //       unexpected failure and we cannot continue.
                    //
                    if (localSubCommands == null)
                    {
                        if (localError != null)
                        {
                            error = localError;
                        }
                        else
                        {
                            //
                            // TODO: Good fallback error message?
                            //
                            error = "sub-command filter failed (all)";
                        }

                        return ReturnCode.Error;
                    }

                    //
                    // TODO: At this point, the list of sub-commands is only
                    //       going to be used when building the error message;
                    //       therefore, make sure it is set to the (possibly
                    //       filtered) new list of sub-commands first.  This
                    //       list is ONLY used when the number of matches is
                    //       exactly zero.  If this method ever changes that
                    //       assumption, the containing "if" statement will
                    //       need to be updated as well.
                    //
                    subCommands = new EnsembleDictionary(localSubCommands);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (matches.Count == 1)
            {
                //
                // NOTE: Normal "success" case, exactly one sub-command
                //       matched.  If this was an exact match, including
                //       case, skip setting the name output parameter
                //       because it already contains the correct value.
                //
                if (!exact)
                    name = matches[0].Key;

                subCommand = matches[0].Value;

                return ReturnCode.Ok;
            }
            else if (matches.Count > 1)
            {
                error = BadSubCommand(
                    interpreter, "ambiguous", "option", name, matches,
                    null, null);

                return ReturnCode.Error;
            }
            else if (strict)
            {
                error = BadSubCommand(
                    interpreter, null, "option", name, subCommands,
                    null, null);

                return ReturnCode.Error;
            }
            else
            {
                //
                // NOTE: Non-strict mode, leave the original sub-command
                //       unchanged and let the caller deal with it.
                //
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter, /* in */
            IEnsemble ensemble,      /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            bool strict,             /* in */
            bool noCase,             /* in */
            ref string name,         /* in, out */
            ref bool tried,          /* out */
            ref Result result        /* out */
            )
        {
            ISubCommand subCommand = null;

            return TryExecuteSubCommandFromEnsemble(
                interpreter, ensemble, clientData, arguments, strict,
                noCase, ref name, ref subCommand, ref tried, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter,    /* in */
            IEnsemble ensemble,         /* in */
            IClientData clientData,     /* in */
            ArgumentList arguments,     /* in */
            bool strict,                /* in */
            bool noCase,                /* in */
            ref string name,            /* in, out */
            ref ISubCommand subCommand, /* out */
            ref bool tried,             /* out */
            ref Result result           /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                //
                // NOTE: Attempt to lookup the sub-command based on the name
                //       and the parent ensemble.
                //
                code = SubCommandFromEnsemble(
                    interpreter, ensemble, PolicyOps.OnlyAllowedSubCommands,
                    strict, noCase, ref name, ref subCommand, ref result);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: If the sub-command was found and is null, treat
                    //       that as "handled by the caller" and just return
                    //       success; otherwise, attempt to redispatch it.
                    //
                    if (subCommand != null)
                    {
                        //
                        // NOTE: Do not allow arbitrary nesting levels for
                        //       sub-commands as we could easily run out of
                        //       native stack space.
                        //
                        if (interpreter.EnterSubCommandLevel() < 2)
                        {
                            try
                            {
                                //
                                // NOTE: Indicate that the sub-command has been
                                //       dispatched (i.e. there is no need for
                                //       the caller to handle this sub-command).
                                //       Even if the execution fails, we still
                                //       tried to execute it and the caller
                                //       should not try to handle it.
                                //
                                tried = true;

                                code = interpreter.Execute(
                                    GetNameForExecute(name, subCommand),
                                    subCommand, clientData, arguments,
                                    ref result);
                            }
                            finally
                            {
                                //
                                // NOTE: Remove the sub-command level added by
                                //       the if statement above.
                                //
                                interpreter.ExitSubCommandLevel();
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Remove the "trial" sub-command level added
                            //       by the if statement above.
                            //
                            interpreter.ExitSubCommandLevel();
                        }
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Message Support Methods
        public static Exception GetBaseException(
            Exception exception
            )
        {
            if (exception == null)
                return null;

            Exception baseException = exception.GetBaseException();

            if (baseException != null)
                return baseException;

            return exception;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static Exception GetInnerException(
            Exception exception
            )
        {
            if (exception == null)
                return null;

            Exception innerException = exception.InnerException;

            if (innerException != null)
                return innerException;

            return exception;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static Result BadValue(
            string adjective,
            string type,
            string value,
            StringSortedList values,
            string prefix,
            string suffix
            )
        {
            if ((values != null) && (values.Count > 0))
            {
                return String.Format("{0} {1} \"{2}\": must be {3}{4}",
                    !String.IsNullOrEmpty(adjective) ? adjective : "bad",
                    !String.IsNullOrEmpty(type) ? type : "value", value,
                    GenericOps<string>.ListToEnglish(
                        values, ", ", Characters.Space.ToString(),
                        !String.IsNullOrEmpty(suffix) ? null : "or ",
                        prefix, null),
                    suffix);
            }

            //
            // FIXME: Fallback here?
            //
            return String.Format(
                "{0} {1} \"{2}\"",
                !String.IsNullOrEmpty(adjective) ? adjective : "bad",
                !String.IsNullOrEmpty(type) ? type : "value", value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadValue(
            string adjective,
            string type,
            string value,
            IEnumerable<string> values,
            string prefix,
            string suffix
            )
        {
            return BadValue(
                adjective, type, value, (values != null) ?
                new StringSortedList(values) : null, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadSubCommand(
            Interpreter interpreter,
            string adjective,
            string type,
            string subCommand,
            IEnsemble ensemble,
            string prefix,
            string suffix
            )
        {
            return BadSubCommand(
                interpreter, adjective, type, subCommand,
                PolicyOps.GetSubCommandsSafe(interpreter, ensemble),
                prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        private static Result BadSubCommand(
            Interpreter interpreter,
            string adjective,
            string type,
            string subCommand,
            EnsembleDictionary subCommands,
            string prefix,
            string suffix
            )
        {
            if ((subCommands != null) && (subCommands.Count > 0))
            {
                bool exists = (subCommand != null) ?
                    subCommands.ContainsKey(subCommand) /* EXEMPT */ :
                    false;

                //
                // BUGFIX: If the sub-command exists in the dictionary,
                //         it must simply be "unsupported" (i.e. not
                //         really implemented) by the parent command.
                //         In that case, construct a good error message.
                //
                EnsembleDictionary localSubCommands;

                if (exists)
                {
                    //
                    // NOTE: Clone the dictionary and then remove the
                    //       "unsupported" sub-command so that it will
                    //       NOT appear in the error message.
                    //
                    localSubCommands = new EnsembleDictionary(
                        subCommands);

                    /* IGNORED */
                    localSubCommands.Remove(subCommand);
                }
                else
                {
                    localSubCommands = subCommands;
                }

                return BadValue(!String.IsNullOrEmpty(adjective) ?
                    adjective : (exists ? "unsupported" : "bad"),
                    !String.IsNullOrEmpty(type) ? type : "option",
                    subCommand, localSubCommands.Keys, prefix, suffix);
            }

            //
            // FIXME: Fallback here?
            //
            return String.Format("{0} {1} \"{2}\"",
                !String.IsNullOrEmpty(adjective) ? adjective : "bad",
                !String.IsNullOrEmpty(type) ? type : "option", subCommand);
        }

        ///////////////////////////////////////////////////////////////////////

        private static Result BadSubCommand(
            Interpreter interpreter,
            string adjective,
            string type,
            string subCommand,
            IEnumerable<KeyValuePair<string, ISubCommand>> subCommands,
            string prefix,
            string suffix
            )
        {
            return BadValue(
                adjective, type, subCommand, (subCommands != null) ?
                new StringSortedList(subCommands) : null, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result WrongNumberOfArguments(
            IIdentifierBase identifierBase, /* NOT USED */
            int count,
            ArgumentList arguments,
            string suffix
            )
        {
            if ((count > 0) &&
                (arguments != null) &&
                (arguments.Count > 0))
            {
                return String.Format(
                    "wrong # args: should be \"{0}{1}{2}\"",
                    ArgumentList.GetRange(arguments, 0, Math.Min(count - 1,
                        arguments.Count - 1)), !String.IsNullOrEmpty(
                    suffix) ? Characters.Space.ToString() : null, suffix);
            }

            //
            // FIXME: Fallback here?
            //
            return "wrong # args";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Support Methods
        public static ReturnCode GetOptionValue(
            Interpreter interpreter,
            StringList list,
            Type type,
            OptionFlags optionFlags,
            bool force,
            bool allowInteger,
            bool strict,
            bool noCase,
            CultureInfo cultureInfo,
            ref Variant value,
            ref int nextIndex,
            ref Result error
            )
        {
            if ((nextIndex < list.Count) && (force || FlagOps.HasFlags(
                    optionFlags, OptionFlags.MustHaveValue, true)))
            {
                if (FlagOps.HasFlags(
                        optionFlags, OptionFlags.MatchOldValueType, true))
                {
                    OptionFlags notHasFlags = OptionFlags.MustBeMask;

                    if ((type != null) && type.IsEnum)
                        notHasFlags &= ~OptionFlags.MustBeEnumMask;

                    if (FlagOps.HasFlags(optionFlags, notHasFlags, false))
                    {
                        error = String.Format(
                            "cannot convert old value for option with flags {0}",
                            FormatOps.WrapOrNull(optionFlags));

                        return ReturnCode.Error;
                    }

                    if ((type != null) && type.IsEnum)
                    {
                        object enumValue;

                        if (EnumOps.IsFlagsEnum(type))
                        {
                            enumValue = EnumOps.TryParseFlagsEnum(
                                interpreter, type, null, list[nextIndex],
                                cultureInfo, allowInteger, strict, noCase,
                                ref error);
                        }
                        else
                        {
                            enumValue = EnumOps.TryParseEnum(
                                type, list[nextIndex], allowInteger, noCase,
                                ref error);
                        }

                        if (enumValue == null)
                            return ReturnCode.Error;

                        value = new Variant(enumValue);
                    }
                    else
                    {
                        value = new Variant(list[nextIndex]);
                    }
                }
                else
                {
                    value = new Variant(list[nextIndex]);
                }

                nextIndex++;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Output Support Methods
        public static ReturnCode WriteViaIExecute(
            Interpreter interpreter,
            string commandName, /* NOTE: Almost always null, for [puts]. */
            string channelId,   /* NOTE: Almost always null, for "stdout". */
            string value,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (commandName == null)
            {
                commandName = ScriptOps.TypeNameToEntityName(
                    typeof(_Commands.Puts));
            }

            if (channelId == null)
                channelId = StandardChannel.Output;

            ReturnCode code;
            IExecute execute = null;

            code = interpreter.GetIExecuteViaResolvers(
                interpreter.GetResolveEngineFlags(true), commandName,
                null, LookupFlags.Default, ref execute, ref result);

            if (code != ReturnCode.Ok)
                return code;

            code = Engine.ExternalExecuteWithFrame(
                commandName, execute, interpreter, null, new ArgumentList(
                    commandName, channelId, value), interpreter.EngineFlags,
                interpreter.SubstitutionFlags, interpreter.EngineEventFlags,
                interpreter.ExpressionFlags, ref result);

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Support Methods
        #region Variable Tracing Support Methods
        public static object GetDefaultValue(
            BreakpointType breakpointType
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableGet:
                    return DefaultGetVariableValue;
                case BreakpointType.BeforeVariableSet:
                    return DefaultSetVariableValue;
                case BreakpointType.BeforeVariableUnset:
                    return DefaultUnsetVariableValue;
            }

            return DefaultVariableValue;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWriteValueTrace(
            BreakpointType breakpointType
            )
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableSet:
                case BreakpointType.BeforeVariableReset:
                case BreakpointType.BeforeVariableUnset:
                    {
                        return true;
                    }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool NeedValueForTrace(
            BreakpointType breakpointType,
            bool old
            )
        {
            if (old)
            {
                switch (breakpointType)
                {
                    case BreakpointType.BeforeVariableReset:
                    case BreakpointType.BeforeVariableUnset:
                        {
                            return true;
                        }
                }
            }
            else if (breakpointType == BreakpointType.BeforeVariableSet)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GatherTraceValues(
            string varName,
            string varIndex,
            object value,
            ElementDictionary arrayValue,
            ref StringList values
            )
        {
            if ((varName == null) && (varIndex == null) &&
                (value == null) && (arrayValue == null))
            {
                return;
            }

            if (varName != null)
            {
                if (values == null)
                    values = new StringList();

                values.Add(varName);
            }

            if (varIndex != null)
            {
                if (values == null)
                    values = new StringList();

                values.Add(varIndex);
            }

            if (value != null)
            {
                if (values == null)
                    values = new StringList();

                values.Add(StringOps.GetStringFromObject(value));
            }

            if ((arrayValue != null) && (arrayValue.Count > 0))
            {
                if (values == null)
                    values = new StringList();

                values.Add(arrayValue.Keys);

                foreach (KeyValuePair<string, object> pair in arrayValue)
                {
                    if (pair.Value != null)
                        values.Add(StringOps.GetStringFromObject(pair.Value));
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessOldObjectsForTrace(
            Interpreter interpreter,             /* in */
            IList<_Wrappers._Object> oldObjects, /* in */
            ref ReturnCode code,                 /* out */
            ref ResultList errors                /* out */
            )
        {
            //
            // If there are no old objects, skip this block.
            //
            if (oldObjects == null)
                return;

            //
            // NOTE: For each old value (i.e. there are potentially multiple
            //       values, maybe even duplicate values, when handling an
            //       array).
            //
            foreach (_Wrappers._Object oldWrapper in oldObjects)
            {
                //
                // NOTE: If the old wrapper object is valid, release a single
                //       reference from it.
                //
                if (oldWrapper == null)
                    continue;

                //
                // NOTE: Grab the object flags now, we may need to use them
                //       multiple times.
                //
                ObjectFlags flags = oldWrapper.ObjectFlags;

                //
                // NOTE: Do not attempt to manage reference counts for locked
                //       objects.
                //
                if (FlagOps.HasFlags(flags, ObjectFlags.Locked, true))
                    continue;

                //
                // NOTE: If there are no more outstanding references to the
                //       underlying object, dipose and remove it now.
                //
                if (--oldWrapper.ReferenceCount > 0)
                    continue;

                //
                // NOTE: If there is no interpreter, we cannot remove opaque
                //       object handles.
                //
                if (interpreter == null)
                    continue;

                //
                // NOTE: We know the opaque object handle must be removed;
                //       however, if the opaque object handle is flagged
                //       as "no automatic disposal", we must honor that and
                //       not dispose the actual underlying object instance.
                //
                if (FlagOps.HasFlags(
                        flags, ObjectFlags.NoAutoDispose, true))
                {
                    //
                    // HACK: Prevent the RemoveObject method from actually
                    //       disposing of the object.
                    //
                    flags |= ObjectFlags.NoDispose;
                    oldWrapper.ObjectFlags = flags;
                }

                //
                // NOTE: Attempt to remove the opaque object handle from the
                //       interpreter now.
                //
                ReturnCode removeCode;
                Result removeResult = null;

                removeCode = interpreter.RemoveObject(
                    EntityOps.GetToken(oldWrapper), null,
                    ObjectOps.GetDefaultSynchronous(), ref removeResult);

                if (removeCode != ReturnCode.Ok)
                {
                    //
                    // NOTE: Complain loudly if we could not remove the object
                    //       because this indicates an error probably occurred
                    //       during the disposal of the object?
                    //
                    if (!FlagOps.HasFlags(
                            flags, ObjectFlags.NoRemoveComplain, true))
                    {
                        DebugOps.Complain(
                            interpreter, removeCode, removeResult);
                    }

                    //
                    // NOTE: Keep track of all errors that occur when removing
                    //       any of the opaque object handles.
                    //
                    if (removeResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(removeResult);
                    }

                    //
                    // NOTE: If any of the objects cannot be removed, then the
                    //       overall result will be an error (even if some of
                    //       the objects are successfully removed).
                    //
                    code = ReturnCode.Error;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessNewObjectsForTrace(
            IList<_Wrappers._Object> newObjects /* in */
            )
        {
            //
            // If there are no new objects, skip this block.
            //
            if (newObjects == null)
                return;

            //
            // NOTE: For each new value (i.e. there are potentially multiple
            //       values, maybe even duplicate values, when handling an
            //       array).
            //
            foreach (_Wrappers._Object newWrapper in newObjects)
            {
                //
                // NOTE: If the new wrapper object is valid, add a single
                //       reference to it.
                //
                if (newWrapper == null)
                    continue;

                //
                // NOTE: Grab the object flags now, we may need to use them
                //       multiple times.
                //
                ObjectFlags flags = newWrapper.ObjectFlags;

                //
                // NOTE: Do not attempt to manage reference counts for locked
                //       objects.
                //
                if (!FlagOps.HasFlags(flags, ObjectFlags.Locked, true))
                    newWrapper.ReferenceCount++;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void AddOldValuesToTraceInfo(
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ElementDictionary oldValues
            )
        {
            if ((traceInfo == null) || (oldValues == null))
                return;

            ElementDictionary localOldValues = traceInfo.OldValues;

            if (localOldValues != null)
            {
                localOldValues.Add(oldValues);
            }
            else
            {
                EventWaitHandle variableEvent = (interpreter != null) ?
                    interpreter.VariableEvent : null;

                localOldValues = new ElementDictionary(variableEvent);
                localOldValues.Add(oldValues);

                traceInfo.OldValues = localOldValues;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ITrace NewCoreTrace(
            TraceCallback callback,
            IClientData clientData,
            TraceFlags traceFlags,
            IPlugin plugin,
            ref Result error
            )
        {
            if (callback != null)
            {
                MethodInfo methodInfo = callback.Method;

                if (methodInfo != null)
                {
                    Type type = methodInfo.DeclaringType;

                    if (type != null)
                    {
                        _Traces.Core trace = new _Traces.Core(new TraceData(
                            FormatOps.TraceDelegateName(callback), null, null,
                            clientData, type.FullName, methodInfo.Name,
                            RuntimeOps.DelegateBindingFlags,
                            AttributeOps.GetMethodFlags(methodInfo),
                            traceFlags, plugin, 0));

                        trace.Callback = callback;
                        return trace;
                    }
                    else
                    {
                        error = "invalid trace callback method type";
                    }
                }
                else
                {
                    error = "invalid trace callback method";
                }
            }
            else
            {
                error = "invalid trace callback";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ITraceInfo NewTraceInfo(
            Interpreter interpreter,
            ITrace trace,
            BreakpointType breakpointType,
            ICallFrame frame,
            IVariable variable,
            string name,
            string index,
            VariableFlags flags,
            object oldValue,
            object newValue,
            ElementDictionary oldValues,
            ElementDictionary newValues,
            StringList list,
            bool force,
            bool cancel,
            bool postProcess,
            ReturnCode returnCode
            )
        {
            //
            // HACK: This method is used to prevent creating a ton of redundant
            //       TraceInfo objects on the heap (i.e. whenever a variable is
            //       read, set, or unset).  Now, there is one TraceInfo object
            //       per-thread and it will be re-used as necessary.
            //
            ITraceInfo traceInfo;

            if (!force && (interpreter != null))
            {
                traceInfo = interpreter.TraceInfo;

                if (traceInfo != null)
                {
                    traceInfo = traceInfo.Update(
                       trace, breakpointType, frame, variable, name, index,
                       flags, oldValue, newValue, oldValues, newValues, list,
                       cancel, postProcess, returnCode);

                    if (traceInfo != null)
                        return traceInfo;
                }
            }

            traceInfo = new TraceInfo(
                trace, breakpointType, frame, variable, name, index,
                flags, oldValue, newValue, oldValues, newValues, list,
                cancel, postProcess, returnCode);

            if (!force && (interpreter != null))
                interpreter.TraceInfo = traceInfo;

            return traceInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do NOT call this method from "get" operation traces.  This
        //          method is ONLY for use by variable operations that cannot
        //          return a value (e.g. "set", "unset", "reset", "add").
        //
        public static ReturnCode FireTraces(
            IVariable variable,
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result error
            )
        {
            Result value = null;

            return FireTraces(variable, breakpointType, interpreter,
                traceInfo, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FireTraces(
            IVariable variable,
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result value,
            ref Result error
            )
        {
            if (variable == null)
            {
                error = "invalid variable";
                return ReturnCode.Error;
            }

            if (traceInfo == null)
            {
                error = "invalid trace";
                return ReturnCode.Error;
            }

            //
            // NOTE: Save the original return code.  We will need it
            //       later to figure out how to process the trace
            //       callback results.
            //
            ReturnCode localCode = traceInfo.ReturnCode;

            //
            // NOTE: Start off with the original old value from the trace
            //       information object as the local result.  This value
            //       may be overwritten via the fired traces if necessary.
            //
            Result localResult = StringOps.GetResultFromObject(
                traceInfo.OldValue);

            //
            // NOTE: Attempt to fire the traces for the variable, if any.
            //
            if (variable.FireTraces(
                    breakpointType, interpreter, traceInfo,
                    ref localResult) == ReturnCode.Ok)
            {
                //
                // HACK: For "get" traces, we need a little bit more magic
                //       here.
                //
                if (breakpointType == BreakpointType.BeforeVariableGet)
                {
                    //
                    // NOTE: Did a trace callback cancel processing of a
                    //       variable operation that was previously regarded
                    //       as unsuccessful?
                    //
                    if ((localCode != ReturnCode.Ok) && traceInfo.Cancel)
                    {
                        //
                        // NOTE: This was a failed "get" operation; however,
                        //       it has been canceled by a trace callback
                        //       (presumably after taking some more meaningful
                        //       action) and is now considered to be successful;
                        //       therefore, place the trace result into the
                        //       OldValue property of the trace object itself,
                        //       if necessary (i.e. it is still null).  Also,
                        //       this relies upon the old value being an actual
                        //       string, not a Result object.
                        //
                        if (traceInfo.OldValue == null)
                        {
                            traceInfo.OldValue = (localResult != null) ?
                                localResult.Value : null;
                        }
                    }
                    else if ((localCode == ReturnCode.Ok) && traceInfo.Cancel)
                    {
                        //
                        // NOTE: This was a successful "get" operation; however,
                        //       it has now been canceled and the OldValue
                        //       property of the trace object will not be used.
                        //       Set the trace result for the caller to grab.
                        //
                        value = localResult;
                    }
                }

                return ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: Give the caller the error from the trace callback.
                //
                error = localResult;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Enumerable Variable Support Methods
        public static void GetEnumerableVariableItemValue(
            Interpreter interpreter,
            object value,
            ref object itemValue
            )
        {
            ReturnCode code;
            Result error = null;

            code = GetEnumerableVariableItemValue(
                BreakpointType.None, null, null, null, value,
                ref itemValue, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetEnumerableVariableItemValue(
            BreakpointType breakpointType,
            IVariable variable,
            string name,
            string index,
            object value,
            ref object itemValue,
            ref Result error
            )
        {
            IMutableAnyTriplet<IEnumerable, IEnumerator, bool> anyTriplet =
                value as IMutableAnyTriplet<IEnumerable, IEnumerator, bool>;

            if (anyTriplet == null)
            {
                error = String.Format(
                    "can't {0} {1}: broken enumerable",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            IEnumerable collection = anyTriplet.X;

            if (collection == null)
            {
                error = String.Format(
                    "can't {0} {1}: missing enumerable",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            IEnumerator enumerator = anyTriplet.Y;

            if (enumerator == null)
            {
                try
                {
                    //
                    // NOTE: Initially, there is no enumerator for the
                    //       variable.  It is created automatically.
                    //
                    enumerator = anyTriplet.Y = collection.GetEnumerator();
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }

            bool autoReset = anyTriplet.Z;

            try
            {
                if (!enumerator.MoveNext()) /* throw */
                {
                    if (autoReset)
                        enumerator.Reset(); /* throw */

                    error = "no more items";
                    return ReturnCode.Error;
                }

                itemValue = enumerator.Current; /* throw */
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Linked Variable Support Methods
        public static void GetLinkedVariableMemberValue(
            Interpreter interpreter,
            object value,
            ref object memberValue
            )
        {
            ReturnCode code;
            MemberInfo memberInfo = null;
            Type type = null;
            object @object = null;
            Result error = null;

            code = GetLinkedVariableMemberAndValue(
                BreakpointType.None, null, null, null, value,
                ref memberInfo, ref type, ref @object,
                ref memberValue, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetLinkedVariableMemberAndValue(
            BreakpointType breakpointType,
            IVariable variable,
            string name,
            string index,
            object value,
            ref MemberInfo memberInfo,
            ref Type type,
            ref object @object,
            ref object memberValue,
            ref Result error
            )
        {
            IAnyPair<MemberInfo, object> anyPair =
                value as IAnyPair<MemberInfo, object>;

            if (anyPair == null)
            {
                error = String.Format(
                    "can't {0} {1}: broken link",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            memberInfo = anyPair.X;

            if (memberInfo == null)
            {
                error = String.Format(
                    "can't {0} {1}: missing member",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;

                type = fieldInfo.FieldType;

                if (type == null)
                {
                    error = String.Format(
                        "can't {0} {1}: missing field type",
                        FormatOps.Breakpoint(breakpointType),
                        FormatOps.ErrorVariableName(
                            variable, null, name, index));

                    return ReturnCode.Error;
                }

                @object = anyPair.Y;

                try
                {
                    memberValue = fieldInfo.GetValue(@object); /* throw */
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;

                type = propertyInfo.PropertyType;

                if (type == null)
                {
                    error = String.Format(
                        "can't {0} {1}: missing property type",
                        FormatOps.Breakpoint(breakpointType),
                        FormatOps.ErrorVariableName(
                            variable, null, name, index));

                    return ReturnCode.Error;
                }

                @object = anyPair.Y;

                try
                {
                    //
                    // BUGBUG: Only non-indexed properties are currently
                    //         supported.
                    //
                    memberValue = propertyInfo.GetValue(@object, null); /* throw */
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else
            {
                error = String.Format(
                    "can't {0} {1}: member must be field or property",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GetLinkedVariableArrayValues(
            Interpreter interpreter,
            ElementDictionary arrayValue,
            ref ElementDictionary values
            )
        {
            if (arrayValue == null)
                return;

            EventWaitHandle variableEvent = (interpreter != null) ?
                interpreter.VariableEvent : null;

            ElementDictionary localValues = new ElementDictionary(
                variableEvent);

            foreach (KeyValuePair<string, object> pair in arrayValue)
            {
                object memberValue = null;

                GetLinkedVariableMemberValue(
                    interpreter, pair.Value, ref memberValue);

                if (memberValue == null)
                    continue;

                localValues.Add(pair.Key, memberValue);
            }

            if (localValues.Count > 0)
            {
                if (values == null)
                    values = new ElementDictionary(variableEvent);

                values.Add(localValues);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetLinkedVariableMemberValue(
            BreakpointType breakpointType,
            IVariable variable,
            string name,
            string index,
            MemberInfo memberInfo,
            object @object,
            object memberValue,
            ref Result error
            )
        {
            if (memberInfo == null)
            {
                error = String.Format(
                    "can't {0} {1}: missing member",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }

            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;

                try
                {
                    fieldInfo.SetValue(@object, memberValue); /* throw */
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else if (memberInfo is PropertyInfo)
            {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;

                try
                {
                    //
                    // BUGBUG: Only non-indexed properties are currently supported.
                    //
                    propertyInfo.SetValue(@object, memberValue, null); /* throw */
                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }
            else
            {
                error = String.Format(
                    "can't {0} {1}: member must be field or property",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(
                        variable, null, name, index));

                return ReturnCode.Error;
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Name Support Methods
        public static ReturnCode SplitVariableName(
            Interpreter interpreter,
            VariableFlags flags,
            string name,
            ref string varName,
            ref string varIndex,
            ref Result error
            )
        {
            if (name != null)
            {
                if (name.Length > 0)
                {
                    if (FlagOps.HasFlags(flags, VariableFlags.NoSplit, true))
                    {
                        //
                        // HACK: Skip parsing, use the supplied name verbatim.
                        //
                        varName = name;
                        varIndex = null;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        string localVarName = null;
                        string localVarIndex = null;

                        if (Parser.SplitVariableName(name, ref localVarName,
                                ref localVarIndex, ref error) == ReturnCode.Ok)
                        {
                            if (localVarIndex != null)
                            {
                                if (!FlagOps.HasFlags(flags,
                                        VariableFlags.NoElement, true))
                                {
                                    varName = localVarName;
                                    varIndex = localVarIndex;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    error = "name refers to an element in an array";
                                }
                            }
                            else
                            {
                                //
                                // BUGFIX: Use the supplied name verbatim.
                                //
                                varName = name;
                                varIndex = null;

                                return ReturnCode.Ok;
                            }
                        }
                    }
                }
                else
                {
                    varName = String.Empty;
                    varIndex = null;

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "invalid variable name";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Frame Support Methods
        public static ReturnCode LinkVariable(
            Interpreter interpreter, /* in */
            ICallFrame localFrame,   /* in */
            string localName,        /* in */
            ICallFrame otherFrame,   /* in */
            string otherName,        /* in */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
            {
                if (localFrame == null)
                {
                    error = "invalid \"local\" call frame";
                    return ReturnCode.Error;
                }

                if (otherFrame == null)
                {
                    error = "invalid \"other\" call frame";
                    return ReturnCode.Error;
                }

                //
                // NOTE: *WARNING* Empty variable names are allowed, please
                //       do not change these to "!String.IsNullOrEmpty".
                //
                if (localName == null)
                {
                    error = "invalid \"local\" variable name";
                    return ReturnCode.Error;
                }

                if (otherName == null)
                {
                    error = "invalid \"other\" variable name";
                    return ReturnCode.Error;
                }

                string localVarName = null;
                string localVarIndex = null;

                if (SplitVariableName(
                        interpreter, VariableFlags.NoElement, localName,
                        ref localVarName, ref localVarIndex,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                string otherVarName = null;
                string otherVarIndex = null;

                //
                // BUGFIX: Allow the other side of the link to be an array
                //         element.
                //
                if (SplitVariableName(
                        interpreter, VariableFlags.None, otherName,
                        ref otherVarName, ref otherVarIndex,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                //
                // NOTE: Is the interpreter running with namespaces enabled?
                //       If so, extra steps must be taken later.
                //
                bool useNamespaces = interpreter.AreNamespacesEnabled();

                //
                // NOTE: *NAMESPACES* Need to make sure the correct frame is
                //       being used if the local frame is marked to use the
                //       associated namespace.
                //
                localFrame = CallFrameOps.FollowNext(localFrame);

                if (useNamespaces && CallFrameOps.IsUseNamespace(localFrame))
                {
                    INamespace localNamespace = NamespaceOps.GetCurrent(
                        interpreter, localFrame);

                    if (localNamespace != null)
                    {
                        if (NamespaceOps.IsGlobal(
                                interpreter, localNamespace))
                        {
                            localFrame = interpreter.CurrentGlobalFrame;
                        }
                        else
                        {
                            localFrame = localNamespace.VariableFrame;
                        }
                    }
                }

                string newLocalVarName = useNamespaces ?
                    NamespaceOps.MakeRelativeName(
                        interpreter, localFrame, localVarName) :
                    MakeVariableName(localVarName);

                //
                // NOTE: *NAMESPACES* Need to make sure the correct frame is
                //       being used if the other frame is marked to use the
                //       associated namespace.
                //
                otherFrame = CallFrameOps.FollowNext(otherFrame);

                if (useNamespaces && CallFrameOps.IsUseNamespace(otherFrame))
                {
                    INamespace otherNamespace = NamespaceOps.GetCurrent(
                        interpreter, otherFrame);

                    if (otherNamespace != null)
                    {
                        if (NamespaceOps.IsGlobal(
                                interpreter, otherNamespace))
                        {
                            otherFrame = interpreter.CurrentGlobalFrame;
                        }
                        else
                        {
                            otherFrame = otherNamespace.VariableFrame;
                        }
                    }
                }

                string newOtherVarName = useNamespaces ?
                    NamespaceOps.MakeRelativeName(
                        interpreter, otherFrame, otherVarName) :
                    MakeVariableName(otherVarName);

                if (CallFrameOps.IsSame(
                        interpreter, localFrame, otherFrame, newLocalVarName,
                        newOtherVarName))
                {
                    error = "can't upvar from variable to itself";
                    return ReturnCode.Error;
                }

                //
                // NOTE: After this point, both the local and other variable
                //       names must be stripped of their qualifiers (i.e. if
                //       they were qualified to begin with).
                //
                if (useNamespaces)
                {
                    newLocalVarName = NamespaceOps.TailOnly(newLocalVarName);
                    newOtherVarName = NamespaceOps.TailOnly(newOtherVarName);
                }

                VariableDictionary localVariables = localFrame.Variables;

                if (localVariables == null)
                {
                    error = "local call frame does not support variables";
                    return ReturnCode.Error;
                }

                IVariable localVariable = null;

                if (interpreter.GetVariableViaResolversWithSplit(
                        localFrame, localVarName /* FULL NAME*/,
                        ref localVariable) == ReturnCode.Ok)
                {
                    //
                    // NOTE: If the local variable has been flagged as undefined
                    //       then go ahead and allow them to use it (it was not
                    //       purged?).
                    //
                    if (!EntityOps.IsUndefined(localVariable))
                    {
                        //
                        // BUGFIX: If the local variable is a link then go
                        //         ahead and allow them to use it.  We do
                        //         this for Tcl compatibility, which allows
                        //         for this "re-targeting" of variable links
                        //         to a different variable.
                        //
                        if (!EntityOps.IsLink(localVariable))
                        {
                            error = String.Format(
                                "variable \"{0}\" already exists",
                                localVarName /* FULL NAME */);

                            return ReturnCode.Error;
                        }
                    }
                }

                EventWaitHandle variableEvent = interpreter.VariableEvent;
                VariableDictionary otherVariables = otherFrame.Variables;
                IVariable otherVariable = null;

                if (interpreter.GetVariableViaResolversWithSplit(
                        otherFrame, otherVarName /* FULL NAME*/,
                        ref otherVariable) == ReturnCode.Ok)
                {
                    IVariable targetVariable = otherVariable;

                    if (EntityOps.IsLink(targetVariable))
                    {
                        targetVariable = EntityOps.FollowLinks(
                            otherVariable, VariableFlags.None);
                    }

                    //
                    // NOTE: Make double sure now that we are not trying to
                    //       create a link to ourselves.
                    //
                    if ((localVariable != null) &&
                        Object.ReferenceEquals(targetVariable, localVariable))
                    {
                        error = "can't upvar from variable to itself";
                        return ReturnCode.Error;
                    }

                    //
                    // BUGFIX: If the other variable is currently undefined,
                    //         make sure all of its state is reset prior to
                    //         being used; otherwise, issues can arise like
                    //         "leftover" array elements.  For an example,
                    //         see test "array-1.26".
                    //
                    if ((otherVariable != null) &&
                        EntityOps.IsUndefined(otherVariable))
                    {
                        bool isGlobalCallFrame = interpreter.IsGlobalCallFrame(
                            otherFrame);

                        otherVariable.Reset(variableEvent);

                        otherVariable.Flags =
                            CallFrameOps.GetNewVariableFlags(otherFrame) |
                            interpreter.GetNewVariableFlags(isGlobalCallFrame);

                        if (isGlobalCallFrame)
                            EntityOps.SetGlobal(otherVariable, true);
                        else
                            EntityOps.SetLocal(otherVariable, true);

                        EntityOps.SetUndefined(otherVariable, true);
                    }
                }
                else if (otherVariables != null)
                {
                    if (otherVariables.ContainsKey(newOtherVarName))
                    {
                        //
                        // BUGBUG: Really, this can only happen if the variable
                        //         resolver lies to us (i.e. it does not return
                        //         the variable when asked yet if appears to be
                        //         present in the target call frame).
                        //
                        error = String.Format(
                            "other variable \"{0}\" already exists",
                            otherVarName /* FULL NAME */);

                        return ReturnCode.Error;
                    }

                    bool isGlobalCallFrame = interpreter.IsGlobalCallFrame(
                        otherFrame);

                    otherVariable = new Variable(
                        otherFrame, newOtherVarName,
                        CallFrameOps.GetNewVariableFlags(otherFrame) |
                        interpreter.GetNewVariableFlags(isGlobalCallFrame),
                        null, interpreter.GetTraces(null),
                        interpreter.VariableEvent);

                    interpreter.MaybeSetQualifiedName(otherVariable);

                    if (isGlobalCallFrame)
                        EntityOps.SetGlobal(otherVariable, true);
                    else
                        EntityOps.SetLocal(otherVariable, true);

                    EntityOps.SetUndefined(otherVariable, true);

                    otherVariables.Add(newOtherVarName, otherVariable);
                }
                else
                {
                    error = "other call frame does not support variables";
                    return ReturnCode.Error;
                }

                if (localVariable != null)
                {
                    localVariable.Reset(variableEvent);
                    localVariable.Link = otherVariable;
                    localVariable.LinkIndex = otherVarIndex;
                }
                else
                {
                    localVariable = new Variable( /* EXEMPT */
                        localFrame, newLocalVarName, null, otherVariable,
                        otherVarIndex, variableEvent);

                    interpreter.MaybeSetQualifiedName(localVariable);
                }

                //
                // NOTE: Make sure to flag the local variable as a link to the
                //       real one.
                //
                EntityOps.SetLink(localVariable, true);

                //
                // NOTE: If we get to this point and the local variable exists
                //       in the call frame, it should be replaced; otherwise,
                //       it should be added.
                //
                localVariables[newLocalVarName] = localVariable;

                //
                // BUGFIX: Mark the variable as "dirty" AFTER the actual
                //         modifications have been completed.
                //
                EntityOps.SetDirty(localVariable, true);

                //
                // NOTE: If we get this far, we have succeeded.
                //
                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable "Safe" Support Methods
        public static bool IsSafeVariableName(
            string name
            )
        {
            lock (syncRoot)
            {
                //
                // WARNING: This list MUST be kept synchronized with the
                //          variable setup code in the Interpreter.Setup
                //          and Interpreter.SetupPlatform methods.
                //
                if (safeVariableNames == null) /* ONCE */
                {
                    safeVariableNames = new StringDictionary(new string[] {
                        Vars.Null,
                        Vars.Platform.Name,
                        TclVars.Interactive,
                        TclVars.PatchLevelName,
                        TclVars.Platform.Name,
                        TclVars.VersionName
                    }, true, false);
                }

                return (name != null) &&
                    safeVariableNames.ContainsKey(name);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSafeTclPlatformElementName(
            string name
            )
        {
            lock (syncRoot)
            {
                //
                // WARNING: This list MUST be kept synchronized with the
                //          variable setup code in the Interpreter.Setup
                //          and Interpreter.SetupPlatform methods.
                //
                if (safeTclPlatformElementNames == null) /* ONCE */
                {
                    safeTclPlatformElementNames = new StringDictionary(
                        new string[] {
                        TclVars.Platform.ByteOrder,
                        TclVars.Platform.CharacterSize,
                        TclVars.Platform.Debug,
                        TclVars.Platform.DirectorySeparator,
                        TclVars.Platform.Engine,
                        TclVars.Platform.PatchLevel,
                        TclVars.Platform.PathSeparator,
                        TclVars.Platform.PlatformName,
                        TclVars.Platform.PointerSize,
                        TclVars.Platform.Threaded,
                        TclVars.Platform.Unicode,
                        TclVars.Platform.Version,
                        TclVars.Platform.WordSize
                    }, true, false);
                }

                return (name != null) &&
                    safeTclPlatformElementNames.ContainsKey(name);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSafeEaglePlatformElementName(
            string name
            )
        {
            lock (syncRoot)
            {
                //
                // WARNING: This list MUST be kept synchronized with the
                //          variable setup code in the Interpreter.Setup
                //          and Interpreter.SetupPlatform methods.
                //
                if (safeEaglePlatformElementNames == null) /* ONCE */
                {
                    safeEaglePlatformElementNames = new StringDictionary(
                        new string[] {
                        Vars.Platform.Configuration,
                        Vars.Platform.Engine,
                        Vars.Platform.PatchLevel,
                        Vars.Platform.Version
                    }, true, false);
                }

                return (name != null) &&
                    safeEaglePlatformElementNames.ContainsKey(name);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Entity Naming Support Methods
        public static string TypeNameToEntityName(
            Type type
            )
        {
            if (type == null)
                return null;

            return TypeNameToEntityName(type.Name);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string TypeNameToEntityName(
            string typeName
            )
        {
            string result = typeName;

            if (result != null)
            {
                //
                // HACK: All core entity names are lowercase; culture is
                //       invariant because these are considered to be
                //       "system" identifiers.
                //
                result = result.ToLowerInvariant();

                //
                // HACK: Remove leading underscore from core entity names
                //       to accommodate the special circumstance where we
                //       were using a leading underscore in order to get
                //       around .NET Framework "reserved" type names (e.g.
                //       Decimal, Double, File, String, Object, etc).
                //
                if ((result.Length > 0) &&
                    (result[0] == Characters.Underscore))
                {
                    result = result.Substring(1);
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute/IVariable Naming Support Methods
        public static string MakeCommandPrefix(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeCommandName(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeCommandPattern(
            string name
            )
        {
            return NamespaceOps.TrimAll(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeVariableName(
            string name
            )
        {
            return NamespaceOps.TrimLeading(name);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Command Support Methods
        public static ReturnCode EachLoopCommand(
            IIdentifierBase identifierBase, /* in */
            bool collect,                   /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in */
            ArgumentList arguments,         /* in */
            ref Result result               /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            string commandName = (identifierBase != null) ?
                identifierBase.Name : "foreach";

            if ((arguments.Count < 4) || ((arguments.Count % 2) != 0))
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varList list ?varList list ...? script\"",
                    commandName);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            int numLists = ((arguments.Count - 2) / 2);
            List<StringList> variableLists = new List<StringList>();
            List<StringList> valueLists = new List<StringList>();
            IntList valueIndexes = new IntList();
            int maximumIterations = 0;

            for (int listIndex = 0; listIndex < numLists; listIndex++)
            {
                int argumentIndex = 1 + (listIndex * 2);
                StringList variableList = null;

                code = Parser.SplitList(
                    interpreter, arguments[argumentIndex], 0,
                    Length.Invalid, true, ref variableList,
                    ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                if (variableList.Count < 1)
                {
                    result = String.Format(
                        "{0} varlist is empty",
                        commandName);

                    code = ReturnCode.Error;
                    goto done;
                }

                variableLists.Add(variableList);
                argumentIndex = 2 + (listIndex * 2);

                StringList valueList = null;

                code = Parser.SplitList(
                    interpreter, arguments[argumentIndex], 0,
                    Length.Invalid, true, ref valueList,
                    ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                valueLists.Add(valueList);
                valueIndexes.Add(0);

                int iterations = valueList.Count / variableList.Count;

                if ((valueList.Count % variableList.Count) != 0)
                    iterations++;

                if (iterations > maximumIterations)
                    maximumIterations = iterations;
            }

            string body = arguments[arguments.Count - 1];
            IScriptLocation location = arguments[arguments.Count - 1];
            StringList resultList = collect ? new StringList() : null;

            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                for (int listIndex = 0; listIndex < numLists; listIndex++)
                {
                    for (int variableIndex = 0;
                            variableIndex < variableLists[listIndex].Count;
                            variableIndex++)
                    {
                        int valueIndex = valueIndexes[listIndex]++;
                        string value = String.Empty;

                        if (valueIndex < valueLists[listIndex].Count)
                            value = valueLists[listIndex][valueIndex];

                        string variableName =
                            variableLists[listIndex][variableIndex];

                        code = interpreter.SetVariableValue(
                            VariableFlags.None, variableName, value, null,
                            ref result);

                        if (code != ReturnCode.Ok)
                        {
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format(
                                    "{0}    (setting {1} loop variable \"{2}\"",
                                    Environment.NewLine, commandName,
                                    FormatOps.Ellipsis(variableName)));

                            goto done;
                        }
                    }
                }

                Result localResult = null;

                code = interpreter.EvaluateScript(
                    body, location, ref localResult);

                if (code == ReturnCode.Ok)
                {
                    if (collect && (resultList != null))
                        resultList.Add(localResult);
                }
                else
                {
                    if (code == ReturnCode.Continue)
                    {
                        code = ReturnCode.Ok;
                    }
                    else if (code == ReturnCode.Break)
                    {
                        result = localResult;
                        code = ReturnCode.Ok;

                        break;
                    }
                    else if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, localResult,
                            String.Format(
                                "{0}    (\"{1}\" body line {2})",
                                Environment.NewLine, commandName,
                                Interpreter.GetErrorLine(interpreter)));

                        result = localResult;
                        break;
                    }
                    else
                    {
                        //
                        // TODO: Can we actually get to this point?
                        //
                        result = localResult;

                        break;
                    }
                }
            }

            //
            // NOTE: Upon success, either set the result to the collected list
            //       elements or clear the result.
            //
            if (code == ReturnCode.Ok)
            {
                if (collect && (resultList != null))
                    result = resultList;
                else
                    Engine.ResetResult(interpreter, ref result);
            }

        done:
            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ArrayNamesLoopCommand(
            IIdentifierBase identifierBase, /* in */
            string subCommandName,          /* in */
            bool collect,                   /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in */
            ArgumentList arguments,         /* in */
            ref Result result               /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            string commandName = (identifierBase != null) ?
                identifierBase.Name : "array";

            if (subCommandName == null)
                subCommandName = "foreach";

            if ((arguments.Count < 5) || (((arguments.Count - 1) % 2) != 0))
            {
                result = String.Format(
                    "wrong # args: should be \"{0} {1} varList arrayName " +
                    "?varList arrayName ...? script\"",
                    commandName, subCommandName);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            int numLists = ((arguments.Count - 3) / 2);
            List<StringList> variableLists = new List<StringList>();
            List<IEnumerator> valueLists = new List<IEnumerator>();
            int maximumIterations = 0;

            for (int listIndex = 0; listIndex < numLists; listIndex++)
            {
                int argumentIndex = 2 + (listIndex * 2);
                StringList variableList = null;

                code = Parser.SplitList(
                    interpreter, arguments[argumentIndex], 0,
                    Length.Invalid, true, ref variableList,
                    ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                if (variableList.Count < 1)
                {
                    result = String.Format(
                        "{0} {1} varlist is empty",
                        commandName, subCommandName);

                    code = ReturnCode.Error;
                    goto done;
                }

                variableLists.Add(variableList);
                argumentIndex = 3 + (listIndex * 2);

                VariableFlags variableFlags = VariableFlags.NoElement |
                    VariableFlags.NoLinkIndex | VariableFlags.Defined |
                    VariableFlags.NonVirtual;

                IVariable variable = null;

                code = interpreter.GetVariableViaResolversWithSplit(
                    arguments[argumentIndex], ref variableFlags,
                    ref variable, ref result);

                if (code != ReturnCode.Ok)
                    goto done;

                if (EntityOps.IsLink(variable))
                    variable = EntityOps.FollowLinks(variable, variableFlags);

                if (EntityOps.IsUndefined(variable) ||
                    !EntityOps.IsArray(variable))
                {
                    result = String.Format(
                        "\"{0}\" isn't an array",
                        arguments[argumentIndex]);

                    code = ReturnCode.Error;
                    goto done;
                }

                ICollection valueList;

                if (interpreter.IsEnvironmentVariable(variable))
                {
                    IDictionary environment =
                        Environment.GetEnvironmentVariables();

                    if (environment == null)
                    {
                        result = "environment variables unavailable";
                        code = ReturnCode.Error;
                        goto done;
                    }

                    valueList = environment.Keys;
                }
                else if (interpreter.IsTestsVariable(variable))
                {
                    StringDictionary tests = interpreter.GetAllTestInformation(
                        false, ref result);

                    if (tests == null)
                    {
                        code = ReturnCode.Error;
                        goto done;
                    }

                    valueList = tests.Keys;
                }
                else
                {
#if DATA
                    DatabaseVariable databaseVariable = null;

                    if (interpreter.IsDatabaseVariable(
                            variable, ref databaseVariable))
                    {
                        ObjectDictionary database =
                            databaseVariable.GetList(
                                interpreter, true, false, ref result);

                        if (database == null)
                        {
                            code = ReturnCode.Error;
                            goto done;
                        }

                        valueList = database.Keys;
                    }
                    else
#endif
                    {
                        valueList = variable.ArrayValue.Keys;
                    }
                }

                valueLists.Add(valueList.GetEnumerator());

                int iterations = valueList.Count / variableList.Count;

                if ((valueList.Count % variableList.Count) != 0)
                    iterations++;

                if (iterations > maximumIterations)
                    maximumIterations = iterations;
            }

            string body = arguments[arguments.Count - 1];
            IScriptLocation location = arguments[arguments.Count - 1];
            StringList resultList = collect ? new StringList() : null;

            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                for (int listIndex = 0; listIndex < numLists; listIndex++)
                {
                    for (int variableIndex = 0;
                            variableIndex < variableLists[listIndex].Count;
                            variableIndex++)
                    {
                        IEnumerator valueList = valueLists[listIndex];
                        object value = null;

                        if (valueList != null)
                        {
                            if (valueList.MoveNext())
                                value = valueList.Current;
                            else
                                valueLists[listIndex] = null;
                        }

                        string variableName =
                            variableLists[listIndex][variableIndex];

                        code = interpreter.SetVariableValue(
                            VariableFlags.None, variableName,
                            StringOps.GetStringFromObject(value),
                            null, ref result);

                        if (code != ReturnCode.Ok)
                        {
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format(
                                    "{0}    (setting {1} {2} loop variable \"{3}\"",
                                    Environment.NewLine, commandName, subCommandName,
                                    FormatOps.Ellipsis(variableName)));

                            goto done;
                        }
                    }
                }

                Result localResult = null;

                code = interpreter.EvaluateScript(
                    body, location, ref localResult);

                if (code == ReturnCode.Ok)
                {
                    if (collect && (resultList != null))
                        resultList.Add(localResult);
                }
                else
                {
                    if (code == ReturnCode.Continue)
                    {
                        code = ReturnCode.Ok;
                    }
                    else if (code == ReturnCode.Break)
                    {
                        result = localResult;
                        code = ReturnCode.Ok;

                        break;
                    }
                    else if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, localResult,
                            String.Format(
                                "{0}    (\"{1} {2}\" body line {3})",
                                Environment.NewLine, commandName,
                                subCommandName,
                                Interpreter.GetErrorLine(interpreter)));

                        result = localResult;
                        break;
                    }
                    else
                    {
                        //
                        // TODO: Can we actually get to this point?
                        //
                        result = localResult;

                        break;
                    }
                }
            }

            //
            // NOTE: Upon success, either set the result to the collected list
            //       elements or clear the result.
            //
            if (code == ReturnCode.Ok)
            {
                if (collect && (resultList != null))
                    result = resultList;
                else
                    Engine.ResetResult(interpreter, ref result);
            }

        done:
            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ArrayNamesAndValuesLoopCommand(
            IIdentifierBase identifierBase, /* in */
            string subCommandName,          /* in */
            bool collect,                   /* in */
            Interpreter interpreter,        /* in */
            IClientData clientData,         /* in */
            ArgumentList arguments,         /* in */
            ref Result result               /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            string commandName = (identifierBase != null) ?
                identifierBase.Name : "array";

            if (subCommandName == null)
                subCommandName = "for";

            if (arguments.Count != 5)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} {1} " +
                    "{2}keyVarName valueVarName{3} arrayName script\"",
                    commandName, subCommandName, Characters.OpenBrace,
                    Characters.CloseBrace);

                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;
            StringList variableList = null;

            code = Parser.SplitList(
                interpreter, arguments[2], 0, Length.Invalid, true,
                ref variableList, ref result);

            if (code != ReturnCode.Ok)
                goto done;

            if ((variableList.Count != 1) &&
                (variableList.Count != 2))
            {
                result = "must have one or two variable names";

                code = ReturnCode.Error;
                goto done;
            }

            VariableFlags variableFlags = VariableFlags.NoElement |
                VariableFlags.NoLinkIndex | VariableFlags.Defined |
                VariableFlags.NonVirtual;

            string varName = arguments[3];
            IVariable variable = null;

            code = interpreter.GetVariableViaResolversWithSplit(
                varName, ref variableFlags, ref variable, ref result);

            if (code != ReturnCode.Ok)
                goto done;

            if (EntityOps.IsLink(variable))
                variable = EntityOps.FollowLinks(variable, variableFlags);

            if (EntityOps.IsUndefined(variable) ||
                !EntityOps.IsArray(variable))
            {
                result = String.Format(
                    "\"{0}\" isn't an array",
                    varName);

                code = ReturnCode.Error;
                goto done;
            }

            ICollection valueList;

            if (interpreter.IsEnvironmentVariable(variable))
            {
                IDictionary environment =
                    Environment.GetEnvironmentVariables();

                if (environment == null)
                {
                    result = "environment variables unavailable";
                    code = ReturnCode.Error;
                    goto done;
                }

                valueList = environment.Keys;
            }
            else if (interpreter.IsTestsVariable(variable))
            {
                StringDictionary tests = interpreter.GetAllTestInformation(
                    false, ref result);

                if (tests == null)
                {
                    code = ReturnCode.Error;
                    goto done;
                }

                valueList = tests.Keys;
            }
            else
            {
#if DATA
                DatabaseVariable databaseVariable = null;

                if (interpreter.IsDatabaseVariable(
                        variable, ref databaseVariable))
                {
                    ObjectDictionary database =
                        databaseVariable.GetList(
                            interpreter, true, false, ref result);

                    if (database == null)
                    {
                        code = ReturnCode.Error;
                        goto done;
                    }

                    valueList = database.Keys;
                }
                else
#endif
                {
                    valueList = variable.ArrayValue.Keys;
                }
            }

            string body = arguments[arguments.Count - 1];
            IScriptLocation location = arguments[arguments.Count - 1];
            StringList resultList = collect ? new StringList() : null;
            IEnumerator valueEnumerator = valueList.GetEnumerator();

            while (true)
            {
                if (!valueEnumerator.MoveNext())
                    break;

                string varIndex = StringOps.GetStringFromObject(
                    valueEnumerator.Current);

                Result varValue = null;

                code = interpreter.GetVariableValue2(
                    VariableFlags.None, varName, varIndex, ref varValue,
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    Engine.AddErrorInformation(interpreter, result,
                        String.Format(
                            "{0}    (getting {1} {2} loop variable \"{3}\"",
                            Environment.NewLine, commandName, subCommandName,
                            FormatOps.VariableName(varName, varIndex)));

                    goto done;
                }

                code = interpreter.SetVariableValue(
                    VariableFlags.None, variableList[0], varIndex, null,
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    Engine.AddErrorInformation(interpreter, result,
                        String.Format(
                            "{0}    (setting {1} {2} loop name variable \"{3}\"",
                            Environment.NewLine, commandName, subCommandName,
                            FormatOps.Ellipsis(variableList[0])));

                    goto done;
                }

                if (variableList.Count >= 2)
                {
                    code = interpreter.SetVariableValue(
                        VariableFlags.None, variableList[1],
                        StringOps.GetStringFromObject(varValue), null,
                        ref result);

                    if (code != ReturnCode.Ok)
                    {
                        Engine.AddErrorInformation(interpreter, result,
                            String.Format(
                                "{0}    (setting {1} {2} loop value variable \"{3}\"",
                                Environment.NewLine, commandName, subCommandName,
                                FormatOps.Ellipsis(variableList[1])));

                        goto done;
                    }
                }

                Result localResult = null;

                code = interpreter.EvaluateScript(
                    body, location, ref localResult);

                if (code == ReturnCode.Ok)
                {
                    if (collect && (resultList != null))
                        resultList.Add(localResult);
                }
                else
                {
                    if (code == ReturnCode.Continue)
                    {
                        code = ReturnCode.Ok;
                    }
                    else if (code == ReturnCode.Break)
                    {
                        result = localResult;
                        code = ReturnCode.Ok;

                        break;
                    }
                    else if (code == ReturnCode.Error)
                    {
                        Engine.AddErrorInformation(interpreter, localResult,
                            String.Format(
                                "{0}    (\"{1} {2}\" body line {3})",
                                Environment.NewLine, commandName,
                                subCommandName,
                                Interpreter.GetErrorLine(interpreter)));

                        result = localResult;
                        break;
                    }
                    else
                    {
                        //
                        // TODO: Can we actually get to this point?
                        //
                        result = localResult;

                        break;
                    }
                }
            }

            //
            // NOTE: Upon success, either set the result to the collected list
            //       elements or clear the result.
            //
            if (code == ReturnCode.Ok)
            {
                if (collect && (resultList != null))
                    result = resultList;
                else
                    Engine.ResetResult(interpreter, ref result);
            }

        done:
            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Behavior Support Methods
        public static bool HasFlags(
            Interpreter interpreter,
            InterpreterFlags hasFlags,
            bool all
            )
        {
            if (interpreter == null)
                return false;

            return FlagOps.HasFlags( /* EXEMPT */
                interpreter.InterpreterFlags, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Evaluation Support Methods
        public static void ExtractTrustFlags(
            TrustFlags trustFlags,
            out bool exclusive,
            out bool withEvents,
            out bool markTrusted,
            out bool allowUnsafe,
            out bool ignoreHidden
            )
        {
            exclusive = !FlagOps.HasFlags(
                trustFlags, TrustFlags.Shared, true);

            withEvents = FlagOps.HasFlags(
                trustFlags, TrustFlags.WithEvents, true);

            markTrusted = FlagOps.HasFlags(
                trustFlags, TrustFlags.MarkTrusted, true);

            allowUnsafe = FlagOps.HasFlags(
                trustFlags, TrustFlags.AllowUnsafe, true);

            ignoreHidden = !FlagOps.HasFlags(
                trustFlags, TrustFlags.NoIgnoreHidden, true);
        }
        #endregion
    }
}
