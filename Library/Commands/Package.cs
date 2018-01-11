/*
 * Package.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("c8fd57c0-20b3-4594-a5a7-919d6f9a8272")]
    /* 
     * POLICY: We allow certain "safe" sub-commands.
     */
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Package : Core
    {
        public Package(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] { 
            "forget", "ifneeded", "indexes", "info", "loaded",
            "names", "present", "provide", "relativefilename", "require",
            "reset", "scan", "unknown", "vcompare", "versions",
            "vloaded", "vsatisfies", "withdraw"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private readonly EnsembleDictionary disallowedSubCommands = new EnsembleDictionary(
            PolicyOps.DefaultDisallowedPackageSubCommandNames);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary DisallowedSubCommands
        {
            get { return disallowedSubCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "forget":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            code = interpreter.PkgForget(
                                                new StringList(arguments, 2), ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package forget ?package package ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ifneeded":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            Version version = null;

                                            code = Value.GetVersion(
                                                arguments[3], interpreter.CultureInfo,
                                                ref version, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                string text = null;

                                                if (arguments.Count == 5)
                                                    text = arguments[4];

                                                code = interpreter.PkgIfNeeded(
                                                    arguments[2], version, text, interpreter.PackageFlags,
                                                    ref result);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package ifneeded package version ?script?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "indexes":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgIndexes(
                                                pattern, false, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package indexes ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "info":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IPackage package = null;

                                            code = interpreter.GetPackage(
                                                arguments[2], LookupFlags.Default,
                                                ref package, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                bool scrub = interpreter.IsSafe();
                                                PackageFlags flags = package.Flags;
                                                Guid id = AttributeOps.GetObjectId(package);

                                                result = StringList.MakeList(
                                                    "kind", package.Kind,
                                                    "id", package.Id.Equals(Guid.Empty) ? id : package.Id,
                                                    "name", package.Name,
                                                    "description", package.Description,
                                                    "indexFileName", scrub ? PathOps.ScrubPath(
                                                        GlobalState.GetBasePath(), package.IndexFileName) :
                                                        package.IndexFileName,
                                                    "provideFileName", scrub ? PathOps.ScrubPath(
                                                        GlobalState.GetBasePath(), package.ProvideFileName) :
                                                        package.ProvideFileName,
                                                    "flags", flags,
                                                    "loaded", (package.Loaded != null) ? package.Loaded : null,
                                                    "ifNeeded", (!scrub && (package.IfNeeded != null)) ?
                                                        package.IfNeeded.KeysAndValuesToString(null, false) :
                                                        null);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package info name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "loaded":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgLoaded(
                                                pattern, false, false, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package loaded ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "names":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgNames(
                                                pattern, false, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package names ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "present":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] { 
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-exact", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    bool exact = false;

                                                    if (options.IsPresent("-exact"))
                                                        exact = true;

                                                    Version version = null;

                                                    if ((argumentIndex + 1) < arguments.Count)
                                                        code = Value.GetVersion(
                                                            arguments[argumentIndex + 1], interpreter.CultureInfo,
                                                            ref version, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        code = interpreter.PresentPackage(
                                                            arguments[argumentIndex], version, exact, ref result);
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"package present ?-exact? package ?version?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package present ?-exact? package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "provide":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            PackageFlags flags = interpreter.PackageFlags;

                                            if (!FlagOps.HasFlags(flags, PackageFlags.NoProvide, true))
                                            {
                                                Version version = null;

                                                if (arguments.Count == 4)
                                                    code = Value.GetVersion(arguments[3], interpreter.CultureInfo, ref version, ref result);

                                                if (code == ReturnCode.Ok)
                                                    code = interpreter.PkgProvide(arguments[2], version, flags, ref result);
                                            }
                                            else
                                            {
                                                //
                                                // HACK: Do nothing, provide no package, and return nothing.
                                                //
                                                result = String.Empty;
                                                code = ReturnCode.Ok;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package provide package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "relativefilename":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            PathComparisonType pathComparisonType = PathComparisonType.Default;

                                            if (arguments.Count == 4)
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(PathComparisonType),
                                                    pathComparisonType.ToString(), arguments[3],
                                                    interpreter.CultureInfo, true, true, true,
                                                    ref result);

                                                if (enumValue is EventFlags)
                                                    pathComparisonType = (PathComparisonType)enumValue;
                                                else
                                                    code = ReturnCode.Error;
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                string fileName = null;

                                                code = PackageOps.GetRelativeFileName(
                                                    interpreter, arguments[2], pathComparisonType,
                                                    ref fileName, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = fileName;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package relativefilename fileName ?type?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "require":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] { 
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-exact", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    bool exact = false;

                                                    if (options.IsPresent("-exact"))
                                                        exact = true;

                                                    Version version = null;

                                                    if ((argumentIndex + 1) < arguments.Count)
                                                    {
                                                        code = Value.GetVersion(
                                                            arguments[argumentIndex + 1], interpreter.CultureInfo,
                                                            ref version, ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = interpreter.RequirePackage(
                                                            arguments[argumentIndex], version, exact, ref result);
                                                    }

                                                    //
                                                    // NOTE: This is a new feature.  If the initial attempt to
                                                    //       require a package fails, call the package fallback
                                                    //       delegate for the interpreter and then try requiring
                                                    //       the package again.
                                                    //
                                                    if ((code != ReturnCode.Ok) && !ScriptOps.HasFlags(
                                                            interpreter, InterpreterFlags.NoPackageFallback, true))
                                                    {
                                                        PackageCallback packageFallback = interpreter.PackageFallback;

                                                        if (packageFallback != null)
                                                        {
                                                            code = packageFallback(
                                                                interpreter, arguments[argumentIndex], version, null,
                                                                interpreter.PackageFlags, exact, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                code = interpreter.RequirePackage(
                                                                    arguments[argumentIndex], version, exact, ref result);
                                                            }
                                                        }
                                                    }

                                                    //
                                                    // BUGFIX: This is really a new feature.  In the event of a failure
                                                    //         here, we now fallback to the "unknown package handler",
                                                    //         just like Tcl does.
                                                    //
                                                    if ((code != ReturnCode.Ok) && !ScriptOps.HasFlags(
                                                            interpreter, InterpreterFlags.NoPackageUnknown, true))
                                                    {
                                                        string text = interpreter.PackageUnknown + Characters.Space +
                                                            Parser.Quote(arguments[argumentIndex]);

                                                        if (version != null)
                                                            text += Characters.Space + Parser.Quote(version.ToString());

                                                        code = interpreter.EvaluateScript(text, ref result); /* EXEMPT */

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = interpreter.RequirePackage(
                                                                arguments[argumentIndex], version, exact, ref result);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"package require ?-exact? package ?version?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package require ?-exact? package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "reset":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            code = interpreter.ResetPkgIndexes(ref result);

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package reset\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "scan":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-interpreter", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-preferfilesystem", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-preferhost", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-host", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-normal", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nonormal", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-recursive", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-resolve", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-refresh", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-autopath", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                                {
                                                    PackageIndexFlags flags;

                                                    if (options.IsPresent("-interpreter"))
                                                        flags = interpreter.PackageIndexFlags;
                                                    else
                                                        flags = PackageIndexFlags.Default;

                                                    if (options.IsPresent("-preferfilesystem"))
                                                        flags |= PackageIndexFlags.PreferFileSystem;

                                                    if (options.IsPresent("-preferhost"))
                                                        flags |= PackageIndexFlags.PreferHost;

                                                    if (options.IsPresent("-host"))
                                                        flags |= PackageIndexFlags.Host;

                                                    if (options.IsPresent("-normal"))
                                                        flags |= PackageIndexFlags.Normal;

                                                    if (options.IsPresent("-nonormal"))
                                                        flags |= PackageIndexFlags.NoNormal;

                                                    if (options.IsPresent("-recursive"))
                                                        flags |= PackageIndexFlags.Recursive;

                                                    if (options.IsPresent("-refresh"))
                                                        flags |= PackageIndexFlags.Refresh;

                                                    if (options.IsPresent("-resolve"))
                                                        flags |= PackageIndexFlags.Resolve;

                                                    bool autoPath = false;

                                                    if (options.IsPresent("-autopath"))
                                                        autoPath = true;

                                                    StringList paths;

                                                    if (argumentIndex != Index.Invalid)
                                                    {
                                                        //
                                                        // NOTE: Refresh the specified path list.
                                                        //
                                                        paths = new StringList(arguments, argumentIndex);
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Refresh the default path list.
                                                        //
                                                        paths = GlobalState.GetAutoPathList(interpreter, autoPath);

                                                        //
                                                        // NOTE: Did they request the auto-path be rebuilt?
                                                        //
                                                        if (autoPath)
                                                        {
                                                            //
                                                            // NOTE: Since the actual auto-path may have changed,
                                                            //       update the variable now.  We disable traces
                                                            //       here because we manually rescan, if necessary,
                                                            //       below.
                                                            //
                                                            code = interpreter.SetLibraryVariableValue(
                                                                VariableFlags.SkipTrace, TclVars.AutoPath,
                                                                (paths != null) ? paths.ToString() : null,
                                                                ref result);
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        PackageIndexDictionary packageIndexes = interpreter.CopyPackageIndexes();

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            code = PackageOps.FindAll(
                                                                interpreter, paths, flags, ref packageIndexes,
                                                                ref result);
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            interpreter.PackageIndexes = packageIndexes;
                                                            result = String.Empty;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package scan ?options? ?dir dir ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unknown":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            if (arguments.Count == 3)
                                            {
                                                interpreter.PackageUnknown = arguments[2];
                                                result = String.Empty;
                                            }
                                            else
                                            {
                                                result = interpreter.PackageUnknown;
                                            }

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package unknown ?command?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vcompare":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            Version version1 = null;

                                            code = Value.GetVersion(
                                                arguments[2], interpreter.CultureInfo,
                                                ref version1, ref result);

                                            Version version2 = null;

                                            if (code == ReturnCode.Ok)
                                                code = Value.GetVersion(
                                                    arguments[3], interpreter.CultureInfo,
                                                    ref version2, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = PackageOps.VersionCompare(version1, version2);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package vcompare version1 version2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "versions":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            code = interpreter.PkgVersions(
                                                arguments[2], ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package versions package\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vloaded":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            code = interpreter.PkgLoaded(
                                                pattern, false, true, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package vloaded ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "vsatisfies":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            PackageFlags flags = interpreter.PackageFlags;

                                            if (!FlagOps.HasFlags(flags, PackageFlags.AlwaysSatisfy, true))
                                            {
                                                Version version1 = null;

                                                code = Value.GetVersion(arguments[2], interpreter.CultureInfo, ref version1, ref result);

                                                Version version2 = null;

                                                if (code == ReturnCode.Ok)
                                                    code = Value.GetVersion(
                                                        arguments[3], interpreter.CultureInfo,
                                                        ref version2, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = PackageOps.VersionSatisfies(
                                                        version1, version2, false);
                                            }
                                            else
                                            {
                                                //
                                                // HACK: Always fake that this was a satisfied package request.
                                                //
                                                result = true;
                                                code = ReturnCode.Ok;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package vsatisfies version1 version2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "withdraw":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            Version version = null;

                                            if (arguments.Count == 4)
                                                code = Value.GetVersion(
                                                    arguments[3], interpreter.CultureInfo,
                                                    ref version, ref result);

                                            if (code == ReturnCode.Ok)
                                                code = interpreter.WithdrawPackage(
                                                    arguments[2], version, ref result);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"package withdraw package ?version?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"package arg ?arg ...?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
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
    }
}
