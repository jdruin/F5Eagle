/*
 * File.cs --
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
using System.Diagnostics;
using System.IO;

#if !MONO
using System.Security.AccessControl;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("b2d52593-b6b5-4382-8992-af0ecee078bb")]
    /*
     * POLICY: We allow certain "safe" sub-commands.
     */
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard
#if NATIVE && WINDOWS
        //
        // NOTE: Uses native code indirectly for querying various pieces of
        //       file system information (on Windows only).
        //
        | CommandFlags.NativeCode
#endif
        )]
    [ObjectGroup("fileSystem")]
    internal sealed class _File : Core
    {
        private static Dictionary<string, GetFileDateTimeCallback> GetTimeCallbacks = null;
        private static Dictionary<string, SetFileDateTimeCallback> SetTimeCallbacks = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public _File(
            ICommandData commandData
            )
            : base(commandData)
        {
            //
            // NOTE: One-time initialization, this is not a per-interpreter
            //       datum and it never changes.
            //
            if (GetTimeCallbacks == null)
            {
                GetTimeCallbacks = new Dictionary<string, GetFileDateTimeCallback>();

                GetTimeCallbacks.Add("file.ctime", File.GetCreationTimeUtc);
                GetTimeCallbacks.Add("file.mtime", File.GetLastWriteTimeUtc);
                GetTimeCallbacks.Add("file.atime", File.GetLastAccessTimeUtc);

                GetTimeCallbacks.Add("directory.ctime", Directory.GetCreationTimeUtc);
                GetTimeCallbacks.Add("directory.mtime", Directory.GetLastWriteTimeUtc);
                GetTimeCallbacks.Add("directory.atime", Directory.GetLastAccessTimeUtc);
            }

            //
            // NOTE: One-time initialization, this is not a per-interpreter
            //       datum and it never changes.
            //
            if (SetTimeCallbacks == null)
            {
                SetTimeCallbacks = new Dictionary<string, SetFileDateTimeCallback>();

                SetTimeCallbacks.Add("file.ctime", File.SetCreationTimeUtc);
                SetTimeCallbacks.Add("file.mtime", File.SetLastWriteTimeUtc);
                SetTimeCallbacks.Add("file.atime", File.SetLastAccessTimeUtc);

                SetTimeCallbacks.Add("directory.ctime", Directory.SetCreationTimeUtc);
                SetTimeCallbacks.Add("directory.mtime", Directory.SetLastWriteTimeUtc);
                SetTimeCallbacks.Add("directory.atime", Directory.SetLastAccessTimeUtc);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "atime", "attributes", "channels", "copy", "ctime",
            "delete", "dirname", "drive", "executable", "exists",
            "extension", "glob", "information", "isdirectory", "isfile",
            "join", "list", "lstat", "magic", "mkdir", "mtime",
            "nativename", "normalize", "objectid", "owned", "pathtype",
            "readable", "rename", "rights", "rmdir", "rootname",
            "rootpath", "same", "sddl", "separator", "size",
            "split", "stat", "system", "tail", "tempname", "type",
            "version", "volumes", "writable"
        });

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        private readonly EnsembleDictionary allowedSubCommands = new EnsembleDictionary(
            PolicyOps.DefaultAllowedFileSubCommandNames);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
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
                                case "atime":
                                case "ctime":
                                case "mtime":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            if (PathOps.PathExists(arguments[2]))
                                            {
                                                string callbackName = FormatOps.QualifiedName(
                                                    FileOps.GetFileType(arguments[2]), subCommand);
                                                long clockValue = 0;
                                                DateTime dateTime = DateTime.MinValue;

                                                if (arguments.Count == 4)
                                                {
                                                    code = Value.GetWideInteger2(
                                                        (IGetValue)arguments[3], ValueFlags.AnyWideInteger,
                                                        interpreter.CultureInfo, ref clockValue, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (TimeOps.SecondsToDateTime(clockValue, ref dateTime, TimeOps.UnixEpoch))
                                                        {
                                                            SetFileDateTimeCallback callback;

                                                            if (SetTimeCallbacks.TryGetValue(callbackName, out callback))
                                                            {
                                                                code = FileOps.SetFileTime(arguments[2], callback, dateTime, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                    result = clockValue;
                                                            }
                                                            else
                                                            {
                                                                result = ScriptOps.BadSubCommand(
                                                                    interpreter, null, null, subCommand, this, null, null);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = String.Format(
                                                                "could not get time {0} seconds since the epoch {1}",
                                                                clockValue, TimeOps.UnixEpoch);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    GetFileDateTimeCallback callback;

                                                    if (GetTimeCallbacks.TryGetValue(callbackName, out callback))
                                                    {
                                                        code = FileOps.GetFileTime(arguments[2], callback, ref dateTime, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (TimeOps.DateTimeToSeconds(ref clockValue, dateTime, TimeOps.UnixEpoch))
                                                            {
                                                                result = clockValue;
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "could not get seconds since the epoch {0} for time {1}",
                                                                    TimeOps.UnixEpoch, dateTime);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = ScriptOps.BadSubCommand(
                                                            interpreter, null, null, subCommand, this, null, null);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "could not read \"{0}\": no such file or directory",
                                                    arguments[2]);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file atime name ?time?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "attributes":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            if (PathOps.PathExists(arguments[2]))
                                            {
                                                FileAttributes attributes = FileAttributes.Normal;

                                                code = FileOps.GetFileAttributes(arguments[2], ref attributes, ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (arguments.Count > 4)
                                                    {
                                                        for (int argumentIndex = 3; argumentIndex < arguments.Count; argumentIndex += 2)
                                                        {
                                                            string name = arguments[argumentIndex];

                                                            object enumValue = EnumOps.TryParseEnum(
                                                                typeof(FileAttributes), name.Substring(1),
                                                                true, true);

                                                            if (enumValue is FileAttributes)
                                                            {
                                                                FileAttributes attribute = (FileAttributes)enumValue;

                                                                if ((argumentIndex + 1) < arguments.Count)
                                                                {
                                                                    string value = arguments[argumentIndex + 1];
                                                                    bool boolValue = false;

                                                                    code = Value.GetBoolean2(value, ValueFlags.AnyBoolean,
                                                                        interpreter.CultureInfo, ref boolValue, ref result);

                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        if (boolValue)
                                                                            attributes |= attribute;
                                                                        else
                                                                            attributes &= ~attribute;
                                                                    }
                                                                    else
                                                                    {
                                                                        break;
                                                                    }
                                                                }
                                                                else if (arguments.Count == 4)
                                                                {
                                                                    result = StringList.MakeList(name,
                                                                        FileOps.HaveFileAttributes(attributes, attribute));

                                                                    break;
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "value for \"{0}\" missing",
                                                                        name);

                                                                    code = ReturnCode.Error;
                                                                    break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = ScriptOps.BadValue(
                                                                    null, "attribute", name,
                                                                    Enum.GetNames(typeof(FileAttributes)),
                                                                    Characters.MinusSign.ToString(), null);

                                                                code = ReturnCode.Error;
                                                                break;
                                                            }
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                            code = FileOps.SetFileAttributes(arguments[2], attributes, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = String.Empty;
                                                    }
                                                    else if (arguments.Count == 4)
                                                    {
                                                        //
                                                        // NOTE: Return the value of a single attribute.
                                                        //
                                                        string name = arguments[3];

                                                        object enumValue = EnumOps.TryParseEnum(
                                                            typeof(FileAttributes), name.Substring(1),
                                                            true, true);

                                                        if (enumValue is FileAttributes)
                                                        {
                                                            result = FileOps.HaveFileAttributes(
                                                                attributes, (FileAttributes)enumValue);
                                                        }
                                                        else
                                                        {
                                                            result = ScriptOps.BadValue(
                                                                null, "attribute", name,
                                                                Enum.GetNames(typeof(FileAttributes)),
                                                                Characters.MinusSign.ToString(), null);

                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else if (arguments.Count == 3)
                                                    {
                                                        //
                                                        // FIXME: PRI 4: Support the -shortname and -longname attributes
                                                        //        via P/Invoke (for getting only).
                                                        //
                                                        StringList list = new StringList();

                                                        foreach (string name in Enum.GetNames(typeof(FileAttributes)))
                                                        {
                                                            FileAttributes value =
                                                                (FileAttributes)Enum.Parse(typeof(FileAttributes), name);

                                                            list.Add(Characters.MinusSign + name.ToLower());
                                                            list.Add(((attributes & value) == value).ToString());
                                                        }

                                                        result = list;
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"file attributes name ?option? ?value? ?option value ...?\"";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "could not read \"{0}\": no such file or directory",
                                                    arguments[2]);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file attributes name ?option? ?value? ?option value ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "channels":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string pattern = null;

                                            if (arguments.Count == 3)
                                                pattern = arguments[2];

                                            StringList list = null;

                                            code = interpreter.ListChannels(
                                                pattern, false, false, ref list, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = list;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file channels ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "copy":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool force = false;

                                                    if (options.IsPresent("-force"))
                                                        force = true;

                                                    code = FileOps.FileCopy(
                                                        ArgumentList.GetRange(arguments, argumentIndex, arguments.Count - 2),
                                                        arguments[arguments.Count - 1], false, force, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
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
                                                        result = "wrong # args: should be \"file copy ?options? source ?source ...? target\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file copy ?options? source ?source ...? target\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "delete":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-recursive", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex != Index.Invalid)
                                                {
                                                    bool recursive = false;

                                                    if (options.IsPresent("-recursive"))
                                                        recursive = true;

                                                    bool force = false;

                                                    if (options.IsPresent("-force"))
                                                        force = true;

                                                    code = FileOps.FileDelete(
                                                        ArgumentList.GetRange(arguments, argumentIndex),
                                                        recursive, force, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"file delete ?options? file ?file ...?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file delete ?options? file ?file ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "dirname":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = PathOps.GetNativePath(arguments[2]);

                                            if (!String.IsNullOrEmpty(path))
                                            {
                                                try
                                                {
                                                    if (!PathOps.IsEqualFileName(
                                                            path, Path.GetPathRoot(path)))
                                                    {
                                                        if (!PathOps.IsJustTilde(path))
                                                        {
                                                            result = PathOps.GetUnixPath(
                                                                Path.GetDirectoryName(path));

                                                            if (String.IsNullOrEmpty(result))
                                                                result = PathOps.CurrentDirectory;
                                                        }
                                                        else
                                                        {
                                                            path = PathOps.ResolveFullPath(interpreter, path);

                                                            if (!String.IsNullOrEmpty(path))
                                                            {
                                                                result = PathOps.GetUnixPath(
                                                                    Path.GetDirectoryName(path));

                                                                if (String.IsNullOrEmpty(result))
                                                                    result = PathOps.CurrentDirectory;
                                                            }
                                                            else
                                                            {
                                                                result = "unrecognized path";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // BUGFIX: They passed the root of that particular drive,
                                                        //         just return the normalized form of what they
                                                        //         originally passed us.
                                                        //
                                                        result = PathOps.GetUnixPath(path); // COMPAT: Tcl.
                                                    }
                                                }
                                                catch
                                                {
                                                    //
                                                    // BUGFIX: They passed some kind of invalid path, return
                                                    //         the path that represents the current directory.
                                                    //
                                                    result = PathOps.CurrentDirectory; // COMPAT: Tcl.
                                                }
                                            }
                                            else
                                            {
                                                result = PathOps.CurrentDirectory;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file dirname name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "drive":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
#if !MONO
                                            if (!CommonOps.Runtime.IsMono())
                                            {
                                                string path = (arguments.Count == 3) ?
                                                    PathOps.ResolveFullPath(interpreter, arguments[2]) :
                                                    Environment.SystemDirectory;

                                                if (!String.IsNullOrEmpty(path))
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Get the drive information object for this path.
                                                        //
                                                        DriveInfo driveInfo = new DriveInfo(path);

                                                        //
                                                        // NOTE: Only attempt to access the drive information if
                                                        //       the device is ready (i.e. the CD-ROM disc is
                                                        //       inserted).
                                                        //
                                                        if (driveInfo.IsReady)
                                                        {
                                                            result = StringList.MakeList(
                                                                "name", driveInfo.Name,
                                                                "volumeLabel", driveInfo.VolumeLabel,
                                                                "driveType", driveInfo.DriveType,
                                                                "driveFormat", driveInfo.DriveFormat,
                                                                "totalFreeSpace", driveInfo.TotalFreeSpace,
                                                                "availableFreeSpace", driveInfo.AvailableFreeSpace,
                                                                "totalSize", driveInfo.TotalSize);
                                                        }
                                                        else
                                                        {
                                                            result = "drive is not ready";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                        result = e;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "unrecognized path";
                                                }
                                            }
                                            else
                                            {
                                                result = "not implemented";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file space ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exists":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                result = PathOps.PathExists(arguments[2]);
                                            }
                                            catch
                                            {
                                                result = false;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file exists name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "executable":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if !MONO
                                            if (!CommonOps.Runtime.IsMono())
                                            {
                                                try
                                                {
                                                    FileSystemRights grantedRights = FileOps.NoFileSystemRights;
                                                    bool accessStatus = false;

                                                    code = FileOps.AccessCheck(arguments[2], FileSystemRights.ExecuteFile,
                                                        ref grantedRights, ref accessStatus, ref result);

                                                    result = (code == ReturnCode.Ok) ? accessStatus : false;
                                                    code = ReturnCode.Ok;
                                                }
                                                catch
                                                {
                                                    result = false;
                                                }
                                            }
                                            else
                                            {
                                                //
                                                // HACK: Assume anything readable can be executed.
                                                //
                                                result = FileOps.VerifyFileAccess(
                                                    interpreter, arguments[2], FileAccess.Read);
                                            }
#else
                                            //
                                            // HACK: Assume anything readable can be executed.
                                            //
                                            result = FileOps.VerifyFileAccess(
                                                interpreter, arguments[2], FileAccess.Read);
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file executable name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "extension":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                result = Path.GetExtension(arguments[2]);
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file extension name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "glob":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocomplain", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noresolve", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-novalidate", null),
                                                new Option(null, OptionFlags.MustHaveMatchModeValue, Index.Invalid, Index.Invalid, "-match",
                                                    new Variant(StringOps.DefaultMatchMode)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-directory", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-searchpattern", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex == Index.Invalid) ||
                                                    ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool noComplain = false;

                                                    if (options.IsPresent("-nocomplain"))
                                                        noComplain = true;

                                                    bool noResolve = false;

                                                    if (options.IsPresent("-noresolve"))
                                                        noResolve = true;

                                                    bool noValidate = false;

                                                    if (options.IsPresent("-novalidate"))
                                                        noValidate = true;

                                                    bool noCase = false;

                                                    if (options.IsPresent("-nocase"))
                                                        noCase = true;

                                                    Variant value = null;
                                                    MatchMode mode = StringOps.DefaultMatchMode;

                                                    if (options.IsPresent("-match", ref value))
                                                        mode = (MatchMode)value.Value;

                                                    string searchPattern = null;

                                                    if (options.IsPresent("-searchpattern", ref value))
                                                        searchPattern = value.ToString();

                                                    string directory = null;

                                                    if (options.IsPresent("-directory", ref value))
                                                    {
                                                        directory = value.ToString();

                                                        if (!noResolve)
                                                            directory = PathOps.ResolveFullPath(interpreter, directory);
                                                    }

                                                    if (String.IsNullOrEmpty(directory))
                                                        directory = Directory.GetCurrentDirectory();

                                                    if (noValidate ||
                                                        PathOps.ValidatePathAsDirectory(directory, false, true))
                                                    {
                                                        string pattern = null;

                                                        if (argumentIndex != Index.Invalid)
                                                            pattern = arguments[argumentIndex];

                                                        StringList list = null;

                                                        code = FileOps.GetFileSystemEntries(
                                                            directory, searchPattern, ref list, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (noComplain || (list.Count > 0))
                                                            {
                                                                if (pattern != null)
                                                                {
                                                                    StringList list2 = new StringList();

                                                                    foreach (string element in list)
                                                                    {
                                                                        bool match = false;

                                                                        code = StringOps.Match(
                                                                            interpreter, mode, element, pattern, noCase,
                                                                            ref match, ref result);

                                                                        if (code != ReturnCode.Ok)
                                                                            break;

                                                                        if (match)
                                                                            list2.Add(element);
                                                                    }

                                                                    //
                                                                    // NOTE: Make sure the matching did not raise any errors.
                                                                    //
                                                                    if (code == ReturnCode.Ok)
                                                                    {
                                                                        if (noComplain || (list2.Count > 0))
                                                                        {
                                                                            //
                                                                            // NOTE: Return the filtered list of results.
                                                                            //
                                                                            result = list2;
                                                                        }
                                                                        else
                                                                        {
                                                                            result = String.Format(
                                                                                "no files matched {0} pattern \"{1}\"",
                                                                                mode.ToString().ToLower(), pattern);

                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    //
                                                                    // NOTE: No secondary filtering, return the whole list.
                                                                    //
                                                                    result = list;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = String.Format(
                                                                    "no files matched search pattern \"{0}\"",
                                                                    searchPattern);

                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "can't glob files \"{0}\": not a valid directory",
                                                            directory);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"file glob ?options? ?pattern?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file glob ?options? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "information":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-directory", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
#if NATIVE && WINDOWS
                                                    Variant value = null;
                                                    bool directory = false;

                                                    if (options.IsPresent("-directory", ref value))
                                                        directory = (bool)value.Value;
                                                    else
                                                        directory = Directory.Exists(arguments[argumentIndex]);

                                                    StringList list = null;

                                                    code = PathOps.GetPathInformation(
                                                        arguments[argumentIndex], directory, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
#else
                                                    result = "not implemented";
                                                    code = ReturnCode.Error;
#endif
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
                                                        result = "wrong # args: should be \"file information ?-directory boolean? ?--? name\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file information ?-directory boolean? ?--? name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isdirectory":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                result = PathOps.ValidatePathAsDirectory(
                                                    arguments[2], false, true);
                                            }
                                            catch
                                            {
                                                result = false;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file isdirectory name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isfile":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                result = PathOps.ValidatePathAsFile(
                                                    arguments[2], false, true);
                                            }
                                            catch
                                            {
                                                result = false;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file isfile name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "join":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            result = PathOps.CombinePath(
                                                true, arguments.GetRange(2, arguments.Count - 2));
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file join name ?name ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "list":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
                                            string directory = null;

                                            if (arguments.Count >= 3)
                                                directory = PathOps.ResolveFullPath(interpreter, arguments[2]);

                                            if (String.IsNullOrEmpty(directory))
                                                directory = Directory.GetCurrentDirectory();

                                            if (PathOps.ValidatePathAsDirectory(directory, true, true))
                                            {
                                                string pattern = null;

                                                if (arguments.Count >= 4)
                                                    pattern = arguments[3];

                                                StringList list = null;

                                                code = FileOps.GetFileSystemEntries(directory, pattern, ref list, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = list;
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "can't list files \"{0}\": not a valid directory",
                                                    directory);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file list ?directory? ?pattern?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "lstat":
                                    {
                                        if (arguments.Count == 4)
                                        {
#if NATIVE && WINDOWS
                                            StringList list = null;

                                            code = PathOps.GetStatus(
                                                arguments[2], true, ref list, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((list.Count % 2) == 0)
                                                {
                                                    for (int index = 0; index < list.Count; index += 2)
                                                    {
                                                        code = interpreter.SetVariableValue2(
                                                            VariableFlags.None, arguments[3],
                                                            list[index], list[index + 1],
                                                            ref result);

                                                        if (code != ReturnCode.Ok)
                                                            break;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "lstat result list must have an even number of elements";
                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file lstat name varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "magic":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            ushort magic = FileOps.IMAGE_NT_OPTIONAL_BAD_MAGIC;

                                            if (FileOps.GetPeFileMagic(arguments[2], ref magic, ref result))
                                            {
                                                result = StringList.MakeList(
                                                    FormatOps.Hexadecimal(magic, true),
                                                    FileOps.GetPeFileMagicName(magic));
                                            }
                                            else
                                            {
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file magic fileName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "mkdir":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                            {
                                                if (!String.IsNullOrEmpty(arguments[argumentIndex]))
                                                {
                                                    try
                                                    {
                                                        Directory.CreateDirectory(arguments[argumentIndex]);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                        result = String.Format(
                                                            "can't create directory \"{0}\": {1}",
                                                            arguments[argumentIndex], e.Message);

                                                        code = ReturnCode.Error;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "can't create directory \"{0}\": no such file or directory",
                                                        arguments[argumentIndex]);

                                                    code = ReturnCode.Error;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file mkdir dir ?dir ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "nativename":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = PathOps.GetNativePath(arguments[2]);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file nativename name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "normalize":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            if (!String.IsNullOrEmpty(arguments[2]))
                                            {
                                                string path = null;

                                                code = PathOps.NormalizePath(
                                                    interpreter, null, arguments[2], true, true,
                                                    true, true, false, ref path, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = path;
                                            }
                                            else
                                            {
                                                result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file normalize name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "objectid":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-directory", null),
                                                new Option(null, OptionFlags.MustHaveBooleanValue, Index.Invalid, Index.Invalid, "-create", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
#if NATIVE && WINDOWS
                                                    Variant value = null;
                                                    bool directory = false;

                                                    if (options.IsPresent("-directory", ref value))
                                                        directory = (bool)value.Value;
                                                    else
                                                        directory = Directory.Exists(arguments[argumentIndex]);

                                                    bool create = false;

                                                    if (options.IsPresent("-create", ref value))
                                                        create = (bool)value.Value;

                                                    StringList list = null;

                                                    code = PathOps.GetObjectId(
                                                        arguments[argumentIndex], directory, create, ref list, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = list;
#else
                                                    result = "not implemented";
                                                    code = ReturnCode.Error;
#endif
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
                                                        result = "wrong # args: should be \"file objectid ?-directory boolean? ?--? name\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file objectid ?-directory boolean? ?--? name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "owned":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if !MONO
                                            if (!CommonOps.Runtime.IsMono())
                                            {
                                                try
                                                {
                                                    bool ownerStatus = false;

                                                    code = FileOps.IsOwner(arguments[2], ref ownerStatus, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = ownerStatus;
                                                }
                                                catch
                                                {
                                                    result = false;
                                                }
                                            }
                                            else
                                            {
                                                result = "not implemented";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file owned name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pathtype":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];

                                            if (!String.IsNullOrEmpty(path))
                                            {
                                                //
                                                // NOTE: Must check for volume relative first because
                                                //       Path.IsPathRooted thinks that paths starting
                                                //       with "/" and "\" are rooted.
                                                //
                                                if (PlatformOps.IsWindowsOperatingSystem() &&
                                                    PathOps.IsDirectoryChar(path[0]))
                                                {
                                                    result = "volumerelative";
                                                }
                                                else if (Path.IsPathRooted(path))
                                                {
                                                    result = "absolute";
                                                }
                                                else
                                                {
                                                    result = "relative";
                                                }
                                            }
                                            else
                                            {
                                                result = "relative";
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file pathtype name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readable":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if !MONO
                                            if (!CommonOps.Runtime.IsMono())
                                            {
                                                try
                                                {
                                                    FileSystemRights grantedRights = FileOps.NoFileSystemRights;
                                                    bool accessStatus = false;

                                                    code = FileOps.AccessCheck(arguments[2], FileSystemRights.Read,
                                                        ref grantedRights, ref accessStatus, ref result);

                                                    result = (code == ReturnCode.Ok) ? accessStatus : false;
                                                    code = ReturnCode.Ok;
                                                }
                                                catch
                                                {
                                                    result = false;
                                                }
                                            }
                                            else
                                            {
                                                result = FileOps.VerifyFileAccess(
                                                    interpreter, arguments[2], FileAccess.Read);
                                            }
#else
                                            result = FileOps.VerifyFileAccess(
                                                interpreter, arguments[2], FileAccess.Read);
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file readable name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "rename":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-force", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                                                {
                                                    bool force = false;

                                                    if (options.IsPresent("-force"))
                                                        force = true;

                                                    code = FileOps.FileCopy(
                                                        ArgumentList.GetRange(arguments, argumentIndex, arguments.Count - 2),
                                                        arguments[arguments.Count - 1], true, force, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
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
                                                        result = "wrong # args: should be \"file rename ?options? source ?source ...? target\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file rename ?options? source ?source ...? target\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "rights":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if !MONO
                                            if (!CommonOps.Runtime.IsMono())
                                            {
                                                try
                                                {
                                                    FileSystemRights grantedRights = FileOps.NoFileSystemRights;
                                                    bool accessStatus = false; /* NOT USED */

                                                    code = FileOps.AccessCheck(
                                                        arguments[2], FileSystemRights.FullControl,
                                                        ref grantedRights, ref accessStatus, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        StringList enumNames = new StringList(new string[] {
                                                            "GenericRead", "GenericWrite", "GenericExecute",
                                                            "GenericAll"
                                                        });

                                                        UlongList enumValues = new UlongList(new ulong[] {
                                                            0x80000000, 0x40000000, 0x20000000,
                                                            0x10000000
                                                        });

                                                        StringList list = FormatOps.FlagsEnumV2(
                                                            grantedRights, enumNames, enumValues,
                                                            true, true, false, ref result);

                                                        if (list != null)
                                                            result = list;
                                                        else
                                                            code = ReturnCode.Error;
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Engine.SetExceptionErrorCode(interpreter, e);

                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "not implemented";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file rights name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "rmdir":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                            {
                                                if (!String.IsNullOrEmpty(arguments[argumentIndex]))
                                                {
                                                    try
                                                    {
                                                        Directory.Delete(arguments[argumentIndex], false);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                        result = String.Format(
                                                            "can't delete directory \"{0}\": {1}",
                                                            arguments[argumentIndex], e.Message);

                                                        code = ReturnCode.Error;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "can't delete directory \"{0}\": no such file or directory",
                                                        arguments[argumentIndex]);

                                                    code = ReturnCode.Error;
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                                result = String.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file rmdir dir ?dir ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "rootname":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                if (Path.HasExtension(arguments[2]))
                                                {
                                                    result = PathOps.CombinePath(
                                                        null, PathOps.GetUnixPath(
                                                            Path.GetDirectoryName(arguments[2])),
                                                        Path.GetFileNameWithoutExtension(arguments[2]));
                                                }
                                                else
                                                {
                                                    result = arguments[2];
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file rootname name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "rootpath":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                result = Path.GetPathRoot(arguments[2]);
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file rootpath name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "same":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            result = PathOps.IsSameFile(
                                                interpreter, arguments[2], arguments[3]);

                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file same name1 name2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sddl":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
#if !MONO
                                            if (!CommonOps.Runtime.IsMono())
                                            {
                                                try
                                                {
                                                    if (PathOps.PathExists(arguments[2]))
                                                    {
                                                        bool isDirectory = Directory.Exists(arguments[2]);
                                                        FileSystemSecurity security;

                                                        if (isDirectory)
                                                            security = Directory.GetAccessControl(arguments[2]);
                                                        else
                                                            security = File.GetAccessControl(arguments[2]);

                                                        if (arguments.Count == 4)
                                                        {
                                                            security.SetSecurityDescriptorSddlForm(
                                                                arguments[3], AccessControlSections.Access);

                                                            //
                                                            // NOTE: Commit changes to file access control and refresh
                                                            //       our file security object to reflect the changes.
                                                            //
                                                            if (isDirectory)
                                                            {
                                                                Directory.SetAccessControl(arguments[2], (DirectorySecurity)security);
                                                                security = Directory.GetAccessControl(arguments[2]);
                                                            }
                                                            else
                                                            {
                                                                File.SetAccessControl(arguments[2], (FileSecurity)security);
                                                                security = File.GetAccessControl(arguments[2]);
                                                            }
                                                        }

                                                        //
                                                        // NOTE: Get the SDDL string for the specified path and
                                                        //       return it.
                                                        //
                                                        result = security.GetSecurityDescriptorSddlForm(
                                                            AccessControlSections.Access);
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "could not read \"{0}\": no such file or directory",
                                                            arguments[2]);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    Engine.SetExceptionErrorCode(interpreter, e);

                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "not implemented";
                                                code = ReturnCode.Error;
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file sddl name ?sddl?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "separator":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            //
                                            // NOTE: We have no VFS support; hence our path separator is
                                            //       always the same.
                                            //
                                            result = Path.DirectorySeparatorChar;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file separator ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "size":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];

                                            try
                                            {
                                                if (PathOps.ValidatePathAsFile(path, false, true))
                                                {
                                                    FileInfo fileInfo = new FileInfo(path);

                                                    result = fileInfo.Length;
                                                }
                                                else if (PathOps.ValidatePathAsDirectory(path, false, true))
                                                {
#if NATIVE && WINDOWS
                                                    code = PathOps.GetSize(path, true, ref result);
#else
                                                    //
                                                    // BUGBUG: We have no way in pure managed code to
                                                    //         obtain the directory size value that
                                                    //         Tcl uses.
                                                    //
                                                    result = 0;
#endif
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "could not read \"{0}\": no such file or directory",
                                                        path);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = String.Format(
                                                    "could not read \"{0}\": no such file or directory",
                                                    path);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file size name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "split":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = PathOps.SplitPath(null, arguments[2]);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file split name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "stat":
                                    {
                                        if (arguments.Count == 4)
                                        {
#if NATIVE && WINDOWS
                                            StringList list = null;

                                            code = PathOps.GetStatus(
                                                arguments[2], false, ref list, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((list.Count % 2) == 0)
                                                {
                                                    for (int index = 0; index < list.Count; index += 2)
                                                    {
                                                        code = interpreter.SetVariableValue2(
                                                            VariableFlags.None, arguments[3],
                                                            list[index], list[index + 1],
                                                            ref result);

                                                        if (code != ReturnCode.Ok)
                                                            break;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "stat result list must have an even number of elements";
                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file stat name varName\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "system":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            string path = (arguments.Count == 3) ?
                                                PathOps.ResolveFullPath(interpreter, arguments[2]) :
                                                Environment.SystemDirectory;

                                            if (!String.IsNullOrEmpty(path))
                                            {
                                                try
                                                {
                                                    //
                                                    // NOTE: We have no VFS support; hence, we are always
                                                    //       "native".
                                                    //
                                                    StringList list = new StringList("native");

                                                    //
                                                    // NOTE: Get the drive information object for this path.
                                                    //
                                                    DriveInfo driveInfo = new DriveInfo(path);

                                                    //
                                                    // NOTE: Only attempt to access the drive information if
                                                    //       the device is ready (i.e. the CD-ROM disc is
                                                    //       inserted).
                                                    //
                                                    if (driveInfo.IsReady)
                                                        list.Add(driveInfo.DriveFormat);

                                                    result = list;
                                                }
                                                catch (Exception e)
                                                {
                                                    Engine.SetExceptionErrorCode(interpreter, e);

                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "unrecognized path";
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file system ?name?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "tail":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string path = arguments[2];

                                            path = path.TrimEnd(PathOps.DirectoryChars);

                                            if (!String.IsNullOrEmpty(path))
                                            {
                                                if (!PathOps.IsEqualFileName(
                                                        path, Path.GetPathRoot(path)))
                                                {
                                                    result = Path.GetFileName(path);
                                                }
                                                else
                                                {
                                                    result = String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                result = String.Empty;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file tail name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "tempname":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            try
                                            {
                                                result = Path.GetTempFileName();
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file tempname\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "type":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            string type = FileOps.GetFileType(arguments[2]);

                                            if (String.Compare(type, "unknown",
                                                    StringOps.SystemStringComparisonType) != 0)
                                            {
                                                result = type;
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "could not read \"{0}\": no such file or directory",
                                                    arguments[2]);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file type name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "version":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-full", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-fixed", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    bool full = false;

                                                    if (options.IsPresent("-full"))
                                                        full = true;

                                                    bool @fixed = false;

                                                    if (options.IsPresent("-fixed"))
                                                        @fixed = true;

                                                    FileVersionInfo version = null;

                                                    code = FileOps.GetFileVersion(
                                                        arguments[argumentIndex], ref version, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (full)
                                                        {
                                                            result = version.ToString();
                                                        }
                                                        else if (@fixed)
                                                        {
                                                            result = String.Format(
                                                                "{0}.{1}.{2}.{3}", version.FileMajorPart,
                                                                version.FileMinorPart, version.FileBuildPart,
                                                                version.FilePrivatePart);
                                                        }
                                                        else
                                                        {
                                                            result = version.FileVersion;
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
                                                        result = "wrong # args: should be \"file version ?options? name\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file version ?options? name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "volumes":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            try
                                            {
                                                StringList list = new StringList();

                                                foreach (string drive in Directory.GetLogicalDrives())
                                                    list.Add(PathOps.GetUnixPath(drive));

                                                result = list;
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file volumes\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "writable":
                                    {
                                        if (arguments.Count == 3)
                                        {
#if !MONO
                                            if (!CommonOps.Runtime.IsMono())
                                            {
                                                try
                                                {
                                                    FileSystemRights grantedRights = FileOps.NoFileSystemRights;
                                                    bool accessStatus = false;

                                                    code = FileOps.AccessCheck(arguments[2], FileSystemRights.Write,
                                                        ref grantedRights, ref accessStatus, ref result);

                                                    result = (code == ReturnCode.Ok) ? accessStatus : false;
                                                    code = ReturnCode.Ok;
                                                }
                                                catch
                                                {
                                                    result = false;
                                                }
                                            }
                                            else
                                            {
                                                result = FileOps.VerifyFileAccess(
                                                    interpreter, arguments[2], FileAccess.Write);
                                            }
#else
                                            result = FileOps.VerifyFileAccess(
                                                interpreter, arguments[2], FileAccess.Write);
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"file writable name\"";
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
                        result = "wrong # args: should be \"file option ?arg ...?\"";
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
