/*
 * ShellOps.cs --
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
using System.IO;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Shared;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("d9374375-f3bb-402f-8c43-354168741995")]
    internal static class ShellOps
    {
        #region Private Constants
        #region Interactive Command Prefix
        internal static readonly string InteractiveCommandPrefix =
            Characters.NumberSign.ToString();

        internal static readonly string InteractiveSystemCommandPrefix =
            StringOps.StrRepeat(2, InteractiveCommandPrefix);

        private static readonly string[] InteractiveVerbatimCommandPrefixes = {
            StringOps.StrRepeat(4, InteractiveCommandPrefix),
            InteractiveSystemCommandPrefix,
            StringOps.StrRepeat(3, InteractiveCommandPrefix),
            InteractiveCommandPrefix
        };

        internal static readonly string DefaultInteractiveCommandPrefix =
            InteractiveCommandPrefix;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Update Checking
        //
        // NOTE: These procedure names are all RESERVED; however, they may
        //       legally be redefined to do nothing.
        //
        private const string CheckForUpdateScript = "checkForUpdate";
        private const string FetchUpdateScript = "fetchUpdate";
        private const string RunUpdateAndExitScript = "runUpdateAndExit";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppSettings Support
#if CONFIGURATION
        private static string ArgumentSettingPrefix = typeof(ShellOps).Name;
        private const string ArgumentCountSettingFormat = "{0}ArgumentCount";
        private const string ArgumentStringSettingFormat = "{0}Argument{1}String";
        private const string ArgumentListSettingFormat = "{0}Argument{1}List";
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shell Support Methods
        public static ReturnCode GetArgumentValue(
            StringList argv,  /* in, out */
            string name,      /* in */
            bool remove,      /* in */
            ref string value, /* out */
            ref Result error  /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (argv != null)
            {
                //
                // NOTE: Scan all the command line arguments, searching for the
                //       ones we are interested in (i.e. mainly those that must
                //       be processed prior to interpreter creation).
                //
                int argIndex = Index.Invalid;

                for (int index = 0; index < argv.Count; index++)
                {
                    //
                    // NOTE: Grab the current command line argument.
                    //
                    string arg = argv[index];

                    //
                    // NOTE: This is the number of switch chars in front of the
                    //       current argument.
                    //
                    int count = 0;

                    //
                    // NOTE: Trims any leading switch chars from the current
                    //       command line argument and sets the count to the
                    //       number of switch chars actually removed.
                    //
                    arg = StringOps.TrimSwitchChars(arg, ref count);

                    //
                    // NOTE: Check for the named command line option.  This
                    //       option is special because it must be done prior
                    //       to the interpreter being created in order for it
                    //       to take full effect.
                    //
                    if ((count > 0) && StringOps.MatchSwitch(arg, name))
                    {
                        //
                        // NOTE: There must be a value after the option name.
                        //
                        if ((index + 1) >= argv.Count)
                        {
                            error = String.Format(
                                "wrong # args: should be \"-{0} <value>\"",
                                name);

                            code = ReturnCode.Error;
                            break;
                        }

                        string localValue = argv[index + 1];

                        if (String.IsNullOrEmpty(localValue))
                            localValue = null;

                        argIndex = index;
                        value = localValue;

                        break;
                    }
                }

                if ((code == ReturnCode.Ok) && remove &&
                    (argIndex >= 0) && (argIndex < argv.Count))
                {
                    argv.RemoveRange(argIndex, 2);
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveArgumentValue(
            IList<string> argv,
            string name
            )
        {
            if (argv == null)
                return true;

            for (int index = 0; index < argv.Count; index++)
            {
                string arg = argv[index];
                int count = 0;

                arg = StringOps.TrimSwitchChars(arg, ref count);

                if ((count > 0) && StringOps.MatchSwitch(arg, name))
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldUseArgumentsFileNames(
            IList<string> argv
            )
        {
            return !HaveArgumentValue(
                argv, CommandLineOption.NoArgumentsFileNames);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldLockHostArguments(
            IList<string> argv
            )
        {
            return HaveArgumentValue(
                argv, CommandLineOption.LockHostArguments);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SomeFileExists(
            StringList fileNames,
            ref string fileName
            )
        {
            if (fileNames == null)
                return false;

            foreach (string localFileName in fileNames)
            {
                if (String.IsNullOrEmpty(localFileName))
                    continue;

                if (File.Exists(localFileName))
                {
                    fileName = localFileName;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetArgumentsFileName(
            string fileName
            )
        {
            StringList list = GetArgumentsFileNames(fileName);

            if (list == null)
                return null;

            int count = list.Count;

            if (count == 0)
                return null;

            return list[count - 1];
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetArgumentsFileNames(
            string fileName
            )
        {
            if (String.IsNullOrEmpty(fileName))
                return null;

            StringList list = new StringList();
            string userName = Environment.UserName;
            string domainName = Environment.UserDomainName;
            string machineName = Environment.MachineName;

            if (!String.Equals(machineName, domainName,
                    StringOps.UserNoCaseStringComparisonType))
            {
                list.Add(String.Format("{0}.{1}.{2}.{3}{4}",
                    fileName, userName, machineName, domainName,
                    FileExtension.Arguments));
            }

            list.Add(String.Format("{0}.{1}.{2}{3}", fileName,
                userName, machineName, FileExtension.Arguments));

            list.Add(String.Format("{0}.{1}{2}", fileName,
                userName, FileExtension.Arguments));

            if (!String.Equals(machineName, domainName,
                    StringOps.UserNoCaseStringComparisonType))
            {
                list.Add(String.Format("{0}.{1}.{2}{3}",
                    fileName, machineName, domainName,
                    FileExtension.Arguments));
            }

            list.Add(String.Format("{0}.{1}{2}", fileName,
                machineName, FileExtension.Arguments));

            if (!String.Equals(machineName, domainName,
                    StringOps.UserNoCaseStringComparisonType))
            {
                list.Add(String.Format("{0}.{1}{2}", fileName,
                    domainName, FileExtension.Arguments));
            }

            //
            // NOTE: The default argument file name MUST be present in the
            //       returned list -AND- MUST be last in the returned list.
            //       The only other alternative that this method has is to
            //       return a list value of null.
            //
            list.Add(String.Format(
                "{0}{1}", fileName, FileExtension.Arguments));

            return list;
        }

        ///////////////////////////////////////////////////////////////////////

        private static TextReader GetTextReaderForString(
            string text,
            ref bool dispose,
            ref Result error
            )
        {
            try
            {
                dispose = true; /* NOTE: Do close all streams. */

                return new StringReader(text); /* throw */
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static TextReader GetTextReaderForFile(
            Encoding encoding,
            string fileName,
            bool console,
            ref bool dispose,
            ref Result error
            )
        {
            try
            {
#if CONSOLE
                if (console && (String.Equals(fileName,
                        CommandLineArgument.StandardInput,
                        StringOps.SystemNoCaseStringComparisonType) ||
                    String.Equals(fileName, StandardChannel.Input,
                        StringOps.SystemNoCaseStringComparisonType)))
                {
                    //
                    // TODO: Allow the interpreter host (if available) to be
                    //       used here instead?
                    //
                    dispose = false; /* NOTE: Do not close standard input. */

                    return Console.In;
                }
                else
#endif
                {
                    dispose = true; /* NOTE: Do close all other files. */

                    return (encoding != null) ?
                        new StreamReader(fileName, encoding) :
                        new StreamReader(fileName); /* throw */
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CommitToArguments(
            IList<string> newArgv,
            int count,
            bool append,
            ref IList<string> argv
            )
        {
            //
            // NOTE: *WARNING* This assumes that the arguments that need
            //       to be removed are at the start of the list provided by
            //       the caller; the ShellMainCore method can guarantee that
            //       will be the case and other callers should do so as well.
            //
            while ((argv != null) && (count-- > 0))
                GenericOps<string>.PopFirstArgument(ref argv);

            //
            // NOTE: If we used up all the arguments (i.e. there were only
            //       "count" arguments in the list), the original argument
            //       list (i.e. "argv") will now be null.  If that is the
            //       case, use a new list.
            //
            if (argv == null)
                argv = new StringList();

            //
            // NOTE: The count may already be zero at this point (if the
            //       above loop was actually fully executed); however, we
            //       must be 100% sure that it is zero beyond this point
            //       (for the loop below).
            //
            count = 0;

            //
            // NOTE: If there are no new arguments then there is nothing
            //       left to do.
            //
            if (newArgv == null)
                return;

            //
            // NOTE: Insert each argument read from the file, in order,
            //       where the original argument(s) was/were removed.
            //
            foreach (string arg in newArgv)
            {
                if (append)
                    argv.Add(arg);
                else
                    argv.Insert(count++, arg);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadArgumentsFromTextReader(
            Interpreter interpreter,
            TextReader textReader,
            int count,
            bool append,
            ref IList<string> argv,
            ref Result error
            )
        {
            if (textReader == null)
            {
                error = "invalid stream";
                return ReturnCode.Error;
            }

            StringList newArgv = new StringList();

            while (true)
            {
                string line = textReader.ReadLine();

                if (line == null) // NOTE: End-of-file?
                    break;

                string trimLine = line.Trim();

                if (!String.IsNullOrEmpty(trimLine))
                {
                    if ((trimLine[0] != Characters.Comment) &&
                        (trimLine[0] != Characters.AltComment))
                    {
                        StringList list = null;

                        if (Parser.SplitList(
                                interpreter, trimLine, 0, Length.Invalid,
                                true, ref list, ref error) == ReturnCode.Ok)
                        {
                            newArgv.Add(list);
                        }
                        else
                        {
                            //
                            // NOTE: The line read from the file cannot be
                            //       parsed as a list, fail now.
                            //
                            return ReturnCode.Error;
                        }
                    }
                }
            }

            CommitToArguments(newArgv, count, append, ref argv);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadArgumentsFromHost(
            Interpreter interpreter,
            StringList argvFileNames,
            Encoding encoding,
            int count,
            bool append,
            bool strict,
            ref string argvFileName,
            ref IList<string> argv,
            ref bool readArgv,
            ref ResultList errors
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            if (argvFileNames == null)
                return ReturnCode.Ok;

            foreach (string localArgvFileName in argvFileNames)
            {
                ScriptFlags scriptFlags = ScriptOps.GetFlags(
                    interpreter, ScriptFlags.ApplicationOptionalFile |
                    ScriptFlags.Data, false);

                ReturnCode localCode;
                Result localResult = null;

                localCode = interpreter.GetScript(
                    localArgvFileName, ref scriptFlags, ref localResult);

                if (localCode != ReturnCode.Ok)
                {
                    if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }

                    continue;
                }

                if (localResult == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "invalid host script for {0}",
                        FormatOps.WrapOrNull(localArgvFileName)));

                    return ReturnCode.Error;
                }

                bool dispose = true; /* EXEMPT */
                TextReader textReader = null;

                try
                {
                    string localFileName = null;

                    if (FlagOps.HasFlags(
                            scriptFlags, ScriptFlags.File, true))
                    {
                        localFileName = localResult;

                        textReader = GetTextReaderForFile(
                            encoding, localFileName, false, ref dispose,
                            ref localResult);
                    }
                    else
                    {
                        string text = localResult;

                        textReader = GetTextReaderForString(
                            text, ref dispose, ref localResult);
                    }

                    if (textReader == null)
                    {
                        if (localResult != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localResult);
                        }

                        return ReturnCode.Error;
                    }

                    localCode = ReadArgumentsFromTextReader(
                        interpreter, textReader, count, append, ref argv,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        //
                        // NOTE: If the interpreter host returned a file
                        //       name (even if it is different), use it;
                        //       otherwise, use the one originally given
                        //       to us by the caller.
                        //
                        if (localFileName != null)
                            argvFileName = localFileName;
                        else
                            argvFileName = localArgvFileName;

                        //
                        // NOTE: At this point (and only this point), we
                        //       know that the command line arguemnts,
                        //       if any, were read from the text reader.
                        //
                        readArgv = true;
                    }
                    else
                    {
                        if (localResult != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localResult);
                        }
                    }

                    return localCode;
                }
                catch (Exception e)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(e);
                    return ReturnCode.Error;
                }
                finally
                {
                    if (textReader != null)
                    {
                        if (dispose)
                            textReader.Dispose();

                        textReader = null;
                    }
                }
            }

            if (strict)
            {
                if (errors == null)
                    errors = new ResultList();

                if (errors.Count == 0)
                    errors.Add("no arguments found via host");

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadArgumentsFromFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            int count,
            bool console,
            bool append,
            ref IList<string> argv,
            ref Result error
            )
        {
            //
            // NOTE: Get the stream reader for the file containing the
            //       arguments to process.  If the file name is "-" or
            //       "stdin", we will end up reading arguments from the
            //       standard input stream.  Currently, this is always
            //       done via the Console; however, in the future it
            //       may use the interpreter host.
            //
            bool dispose = true; /* EXEMPT */
            TextReader textReader = null;

            try
            {
                textReader = GetTextReaderForFile(
                    encoding, fileName, console, ref dispose, ref error);

                if (textReader == null)
                    return ReturnCode.Error;

                return ReadArgumentsFromTextReader(
                    interpreter, textReader, count, append, ref argv,
                    ref error);
            }
            finally
            {
                if (textReader != null)
                {
                    if (dispose)
                        textReader.Dispose();

                    textReader = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldUseArgumentsAppSettings(
            IList<string> argv
            )
        {
#if CONFIGURATION
            //
            // NOTE: This configuration parameter is considered to be
            //       part of the configuration of the interpreter itself,
            //       hence those flags are used here.
            //
            if (GlobalConfiguration.DoesValueExist(EnvVars.NoAppSettings,
                    ConfigurationFlags.InterpreterVerbose)) /* EXEMPT */
            {
                return false;
            }

            if (!ConfigurationOps.HaveAppSettings(true))
                return false;

            if (argv == null)
                return true;

            return !HaveArgumentValue(
                argv, CommandLineOption.NoAppSettings);
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if CONFIGURATION
        public static ReturnCode ReadArgumentsFromAppSettings(
            Interpreter interpreter,
            int count,
            bool append,
            ref IList<string> argv,
            ref Result error
            )
        {
            int newArgc;

            if (!ConfigurationOps.TryGetIntegerAppSetting(String.Format(
                    ArgumentCountSettingFormat, ArgumentSettingPrefix),
                    out newArgc))
            {
                return ReturnCode.Ok;
            }

            if (newArgc < 0)
            {
                error = "argument count cannot be negative";
                return ReturnCode.Error;
            }

            if (newArgc == 0)
            {
                if (argv == null)
                    argv = new StringList();

                return ReturnCode.Ok;
            }

            StringList newArgv = new StringList();

            for (int index = 0; index < newArgc; index++)
            {
                string value;
                Result localError = null;
                ResultList errors = null;

                if (ConfigurationOps.TryGetAppSetting(String.Format(
                        ArgumentStringSettingFormat, ArgumentSettingPrefix,
                        index), out value, ref localError))
                {
                    newArgv.Add(value);
                    continue;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                localError = null;

                if (ConfigurationOps.TryGetAppSetting(String.Format(
                        ArgumentListSettingFormat, ArgumentSettingPrefix,
                        index), out value, ref localError))
                {
                    StringList list = null;

                    localError = null;

                    if (Parser.SplitList(
                            interpreter, value, 0, Length.Invalid, true,
                            ref list, ref localError) != ReturnCode.Ok)
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        error = errors;
                        return ReturnCode.Error;
                    }

                    newArgv.Add(list);
                    continue;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                error = errors;
                return ReturnCode.Error;
            }

            CommitToArguments(newArgv, count, append, ref argv);
            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool BeginHighContrastColors(
            IColorHost colorHost,
            ref ConsoleColor savedForegroundColor,
            ref ConsoleColor savedBackgroundColor
            )
        {
            if (colorHost == null)
                return false;

            if (!colorHost.GetColors(
                    ref savedForegroundColor, ref savedBackgroundColor))
            {
                return false;
            }

            //
            // TODO: Maybe change the background color here as well?
            //
            if (!colorHost.SetColors(
                    true, true, HostOps.GetHighContrastColor(
                    savedBackgroundColor), savedBackgroundColor))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool EndHighContrastColors(
            IColorHost colorHost,
            ref ConsoleColor savedForegroundColor,
            ref ConsoleColor savedBackgroundColor
            )
        {
            if (colorHost == null)
                return false;

            if (!colorHost.SetColors(
                    true, true, savedForegroundColor, savedBackgroundColor))
            {
                return false;
            }

            savedForegroundColor = _ConsoleColor.None;
            savedBackgroundColor = _ConsoleColor.None;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WritePrompt(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
            {
                IColorHost colorHost = interactiveHost as IColorHost;

                ConsoleColor savedForegroundColor = _ConsoleColor.None;
                ConsoleColor savedBackgroundColor = _ConsoleColor.None;

                BeginHighContrastColors(colorHost, ref savedForegroundColor,
                    ref savedBackgroundColor);

                try
                {
                    try
                    {
                        interactiveHost.WriteLine(value);
                        return;
                    }
                    catch
                    {
                        // do nothing.
                    }
                }
                finally
                {
                    EndHighContrastColors(colorHost, ref savedForegroundColor,
                        ref savedBackgroundColor);
                }
            }

#if CONSOLE
            try
            {
                ConsoleOps.WriteCore(value); /* throw */
                return;
            }
            catch
            {
                // do nothing.
            }
#endif

            DebugOps.WriteWithoutFail(value);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These methods are private because it currently seems unlikely
        //       that they will be useful to any external callers (i.e. those
        //       other than ShellMainCore).
        //
        public static void ShellMainCoreError( /* FOR ShellMain USE ONLY. */
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode code,
            Result result
            )
        {
            IInteractiveHost interactiveHost = null;
            bool quiet = false;

            if (interpreter != null)
            {
                interactiveHost = interpreter.Host;
                quiet = interpreter.ShouldBeQuiet();
            }

            ShellMainCoreError(
                interpreter, savedArg, arg, code, result,
                ref interactiveHost, ref quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode code,
            Result result,
            ref IInteractiveHost interactiveHost,
            ref bool quiet
            )
        {
            //
            // NOTE: This method overload is for non-API generated errors only.
            //       No error line info, script stack trace, or return code is
            //       needed.
            //
            IList<string> argv = null;

            ShellMainCoreError(interpreter, savedArg, arg, code, result,
                ref argv, ref interactiveHost, ref quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            Result result,
            ref IList<string> argv,
            ref IInteractiveHost interactiveHost,
            ref bool quiet
            )
        {
            //
            // NOTE: This method overload is for non-API generated errors only.
            //       No error line info, script stack trace, or return code is
            //       needed.
            //
            ShellMainCoreError(interpreter, savedArg, arg, ReturnCode.Error,
                result, ref argv, ref interactiveHost, ref quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode code,
            Result result,
            ref IList<string> argv,
            ref IInteractiveHost interactiveHost,
            ref bool quiet
            )
        {
            //
            // NOTE: This method overload is for [non-script] API generated
            //       errors only.  No error line info or script stack trace is
            //       required.
            //
            ShellMainCoreError(interpreter, savedArg, arg, code, result, 0,
                false, false, ref argv, ref interactiveHost, ref quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode code,
            Result result,
            int errorLine,
            bool errorInfo,
            bool strict,
            ref IList<string> argv,
            ref IInteractiveHost interactiveHost,
            ref bool quiet
            )
        {
            TraceOps.DebugTrace(String.Format(
                "ShellMainCoreError: interpreter = {0}, " +
                "savedArg = {1}, arg = {2}, code = {3}, result = {4}, " +
                "errorLine = {5}, errorInfo = {6}, strict = {7}, " +
                "argv = {8}, interactiveHost = {9}, quiet = {10}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(true, true, savedArg),
                FormatOps.WrapOrNull(true, true, arg), code,
                FormatOps.WrapOrNull(true, true, result), errorLine,
                errorInfo, strict, FormatOps.WrapOrNull(true, true, argv),
                FormatOps.WrapOrNull(interactiveHost), quiet),
                typeof(ShellOps).Name, TracePriority.ShellError);

            if (interpreter == null)
            {
                if (strict)
                {
                    //
                    // NOTE: Interpreter is invalid; therefore, no interpreter
                    //       host is available.
                    //
                    HostOps.WriteConsoleOrComplain(code, result, errorLine);
                }

                //
                // NOTE: Nothing else to do, return now.
                //
                return;
            }

            //
            // NOTE: Always grab the interpreter host fresh as it can change
            //       after any user code has been evaluated or executed.
            //
            interactiveHost = interpreter.Host;

            //
            // NOTE: See if quiet mode has been enabled for the interpreter.
            //       If so, skip doing any output because MSBuild may be
            //       watching us (i.e. it can cause the build to fail).
            //
            quiet = interpreter.ShouldBeQuiet();

            if (quiet)
                return;

            //
            // NOTE: Is the interpreter host unavailable now?
            //
            if (interactiveHost == null)
            {
                if (strict)
                {
                    //
                    // NOTE: No interpreter host is available.
                    //
                    HostOps.WriteConsoleOrComplain(code, result, errorLine);
                }

                //
                // NOTE: Nothing else to do, return now.
                //
                return;
            }

            //
            // NOTE: Write the result to the interpreter host.  If the error
            //       line is zero, it will not actually be output.
            //
            interactiveHost.WriteResultLine(code, result, errorLine);

            //
            // NOTE: Do we want to report the script stack trace as well?
            //       First, see if debug mode has been enabled for the
            //       interpreter.
            //
            if (errorInfo && interpreter.Debug)
            {
                Result error = null;

                if (interpreter.CopyErrorInformation(
                        VariableFlags.None, ref error) == ReturnCode.Ok)
                {
                    interactiveHost.WriteResultLine(code, error.ErrorInfo);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Loop Support Methods
        public static bool LooksLikeAnyInteractiveCommand(
            string text
            )
        {
            int nextIndex = 0;

            return LooksLikeAnyInteractiveCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeAnyInteractiveCommand(
            string text,
            ref int nextIndex
            )
        {
            return LooksLikeInteractiveCommand(text, ref nextIndex) ||
                LooksLikeInteractiveSystemCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeInteractiveCommand(
            string text
            )
        {
            int nextIndex = 0;

            return LooksLikeInteractiveCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool LooksLikeInteractiveCommand(
            string text,
            ref int nextIndex
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string prefix = InteractiveCommandPrefix;

                if (!String.IsNullOrEmpty(prefix))
                {
                    int prefixLength = prefix.Length;
                    string localText = text.Trim();

                    if (localText.StartsWith(prefix,
                            StringOps.SystemNoCaseStringComparisonType))
                    {
                        int localIndex = text.IndexOf(prefix,
                            StringOps.SystemNoCaseStringComparisonType);

                        if (localIndex != Index.Invalid)
                            nextIndex = localIndex + prefixLength;

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeInteractiveSystemCommand(
            string text
            )
        {
            int nextIndex = 0;

            return LooksLikeInteractiveSystemCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool LooksLikeInteractiveSystemCommand(
            string text,
            ref int nextIndex
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string prefix = InteractiveSystemCommandPrefix;

                if (!String.IsNullOrEmpty(prefix))
                {
                    int prefixLength = prefix.Length;
                    string localText = text.Trim();

                    if (localText.StartsWith(prefix,
                            StringOps.SystemNoCaseStringComparisonType))
                    {
                        int localIndex = text.IndexOf(prefix,
                            StringOps.SystemNoCaseStringComparisonType);

                        if (localIndex != Index.Invalid)
                            nextIndex = localIndex + prefixLength;

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeInteractiveVerbatimCommand(
            string text,
            ref string newPrefix,
            ref int nextIndex
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string[] prefixes = InteractiveVerbatimCommandPrefixes;

                if (prefixes == null)
                    return false;

                int prefixesLength = prefixes.Length;

                if ((prefixesLength % 2) != 0)
                    return false;

                for (int index = 0; index < prefixesLength; index += 2)
                {
                    string oldPrefix = prefixes[index];

                    if (String.IsNullOrEmpty(oldPrefix))
                        continue;

                    int prefixLength = oldPrefix.Length;
                    string localText = text.Trim();

                    if (localText.StartsWith(oldPrefix,
                            StringOps.SystemNoCaseStringComparisonType))
                    {
                        newPrefix = prefixes[index + 1];

                        int localIndex = text.IndexOf(oldPrefix,
                            StringOps.SystemNoCaseStringComparisonType);

                        if (localIndex != Index.Invalid)
                            nextIndex = localIndex + prefixLength;
                        else
                            nextIndex = Index.Invalid;

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string StripInteractiveCommand(
            string text
            )
        {
            if (String.IsNullOrEmpty(text))
                return text;

            int nextIndex = 0;

            if (!LooksLikeInteractiveSystemCommand(text, ref nextIndex))
                return text;
            else if (!LooksLikeInteractiveCommand(text, ref nextIndex))
                return text;

            int index = text.IndexOfAny(Characters.WhiteSpaceChars, nextIndex);

            if (index == Index.Invalid)
                return text;

            return text.Substring(index + 1).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        public static CancelFlags GetResetCancelFlags(
            bool force
            )
        {
            CancelFlags cancelFlags = CancelFlags.Default;

            if (force)
                cancelFlags |= CancelFlags.IgnorePending;

            return cancelFlags | CancelFlags.ForInteractive;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckForUpdate(
            Interpreter interpreter,
            ActionType actionType,
            ReleaseType releaseType,
            bool debug,
            bool wantScripts,
            bool quiet,
            bool prompt,
            bool automatic,
            ref int errorLine,
            ref Result result
            )
        {
            EngineFlags engineFlags;
            SubstitutionFlags substitutionFlags;
            EventFlags eventFlags;
            ExpressionFlags expressionFlags;

            Interpreter.QueryFlagsNoThrow(
                interpreter, debug, out engineFlags, out substitutionFlags,
                out eventFlags, out expressionFlags);

            return CheckForUpdate(
                interpreter, actionType, releaseType, wantScripts, quiet,
                prompt, automatic, engineFlags, substitutionFlags, eventFlags,
                expressionFlags, ref errorLine, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckForUpdate(
            Interpreter interpreter,
            ActionType actionType,
            ReleaseType releaseType,
            bool wantScripts,
            bool quiet,
            bool prompt,
            bool automatic,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref int errorLine,
            ref Result result
            )
        {
            ReturnCode code;

            //
            // NOTE: Evaluate the script we use to check for updates to the
            //       script engine.  If the proc has been redefined, this may
            //       not actually do anything.
            //
            errorLine = 0;

            code = Engine.EvaluateScript(interpreter, StringList.MakeList(
                CheckForUpdateScript, wantScripts, quiet, prompt, automatic),
                engineFlags, substitutionFlags, eventFlags, expressionFlags,
                ref result, ref errorLine);

            //
            // NOTE: Evaluate the script we use to fetch an update to the
            //       script engine, if necessary.  If the proc has been
            //       redefined, this may not actually do anything.
            //
            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: Attempt to parse the result of the check-for-update
                //       script as a list.
                //
                StringList list = null;

                code = Parser.SplitList(
                    interpreter, result, 0, Length.Invalid, true, ref list,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: If the specified action is greater than zero, we
                    //       need to actively interpret the result and then
                    //       fetch the update if necessary; otherwise, we do
                    //       nothing but simply reporting the result of the
                    //       check-for-update script.
                    //
                    if ((actionType != ActionType.None) &&
                        (actionType != ActionType.CheckForUpdate))
                    {
                        //
                        // NOTE: If the result from the check-for-update script
                        //       is a list with more than one element, then a
                        //       download must be necessary.
                        //
                        if (list.Count > 1)
                        {
                            //
                            // NOTE: Parse the second element of the list as a
                            //       nested list containing [most of] the
                            //       arguments to pass to the fetch-an-update
                            //       script.
                            //
                            StringList list2 = null;

                            code = Parser.SplitList(
                                interpreter, list[1], 0, Length.Invalid, true,
                                ref list2, ref result);

                            if ((code == ReturnCode.Ok) && (list2.Count == 2))
                            {
                                string actionScript = null;

                                if (actionType == ActionType.RunUpdateAndExit)
                                {
                                    actionScript = StringList.MakeList(
                                        RunUpdateAndExitScript, automatic);
                                }
                                else if (actionType == ActionType.FetchUpdate)
                                {
                                    actionScript = StringList.MakeList(
                                        FetchUpdateScript, list2[0], list2[1],
                                        releaseType, PathOps.GetUnixPath(
                                        Path.GetTempPath()));
                                }
                                else
                                {
                                    result = String.Format(
                                        "unsupported action type {0}",
                                        actionType);

                                    code = ReturnCode.Error;
                                }

                                if ((code == ReturnCode.Ok) &&
                                    (actionScript != null))
                                {
                                    errorLine = 0;

                                    code = Engine.EvaluateScript(
                                        interpreter, actionScript, engineFlags,
                                        substitutionFlags, eventFlags,
                                        expressionFlags, ref result,
                                        ref errorLine);

                                    //
                                    // NOTE: Return just the message itself as
                                    //       the result.
                                    //
                                    result = String.Format("{0}{1}{2}",
                                        list[0], Environment.NewLine, result);
                                }
                            }
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Return just the informational message itself
                        //       as the result or an error if none was provided.
                        //
                        result = (list.Count > 0) ? list[0] :
                            "invalid result from the check-for-update script";
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Loop Thread Support
        private static void InteractiveLoopThreadStart(
            object obj
            )
        {
            try
            {
                IAnyPair<Interpreter, InteractiveLoopData> anyPair =
                    obj as IAnyPair<Interpreter, InteractiveLoopData>;

                if (anyPair == null)
                {
                    DebugOps.Complain(ReturnCode.Error,
                        "thread argument is not a pair");

                    return;
                }

                TraceOps.DebugTrace(String.Format(
                    "InteractiveLoopThreadStart: entered, " +
                    "interpreter = {0}, loopData = {1}",
                    FormatOps.InterpreterNoThrow(anyPair.X),
                    FormatOps.InteractiveLoopData(anyPair.Y)),
                    typeof(ShellOps).Name,
                    TracePriority.ThreadDebug);

                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    code = Interpreter.InteractiveLoop(
                        anyPair.X, anyPair.Y, ref result);
                }
                catch (Exception e)
                {
                    result = e;
                    code = ReturnCode.Error;
                }
                finally
                {
                    TraceOps.DebugTrace(String.Format(
                        "InteractiveLoopThreadStart: exited, " +
                        "interpreter = {0}, loopData = {1}, " +
                        "code = {2}, result = {3}",
                        FormatOps.InterpreterNoThrow(anyPair.X),
                        FormatOps.InteractiveLoopData(anyPair.Y),
                        code, FormatOps.WrapOrNull(true, true, result)),
                        typeof(ShellOps).Name,
                        TracePriority.ThreadDebug);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Thread CreateInteractiveLoopThread(
            Interpreter interpreter,
            InteractiveLoopData loopData,
            bool start,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            try
            {
                Thread thread = Engine.CreateThread(
                    InteractiveLoopThreadStart, 0, true, false);

                if (thread != null)
                {
                    thread.Name = String.Format(
                        "interactiveLoopThread: {0}", interpreter);

                    if (start)
                    {
                        IAnyPair<Interpreter, InteractiveLoopData> anyPair =
                            new AnyPair<Interpreter, InteractiveLoopData>(
                                interpreter, loopData);

                        thread.Start(anyPair); /* throw */
                    }

                    return thread;
                }
                else
                {
                    error = "failed to create engine thread";
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);

                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode StopInteractiveLoopThread(
            Thread thread,
            Interpreter interpreter,
            bool force,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            ///////////////////////////////////////////////////////////////////
            // PHASE 0: Parameter validation.
            ///////////////////////////////////////////////////////////////////

            if ((thread == null) || !thread.IsAlive)
            {
                error = String.Format(
                    "interactive loop thread {0} is not alive",
                    FormatOps.ThreadIdOrNull(thread));

                return ReturnCode.Error;
            }

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 1: Grab event and host.
            ///////////////////////////////////////////////////////////////////

            EventWaitHandle @event;
            IDebugHost debugHost;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (interpreter.Disposed)
                {
                    error = "interpreter is disposed";
                    return ReturnCode.Error;
                }

                @event = interpreter.InteractiveLoopEvent;

                debugHost = interpreter.GetInteractiveHost(
                    typeof(IDebugHost)) as IDebugHost;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 2: Signal the interactive loop.
            ///////////////////////////////////////////////////////////////////

            if (@event == null)
            {
                error = "interactive loop event not available";
                return ReturnCode.Error;
            }

            if (!ThreadOps.SetEvent(@event))
            {
                error = "failed to signal interactive loop";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 3: Cancel interpreter host input.
            ///////////////////////////////////////////////////////////////////

            if (debugHost == null)
            {
                error = "interpreter host not available";
                return ReturnCode.Error;
            }

            try
            {
                if (debugHost.Cancel(force, ref error) != ReturnCode.Ok)
                    return ReturnCode.Error;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 4: Wait for interactive loop thread to exit.
            ///////////////////////////////////////////////////////////////////

            try
            {
                if (!thread.Join(ThreadOps.DefaultJoinTimeout))
                {
                    error = "timeout waiting for interactive loop thread";
                    return ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shell Thread Support
        private static void ShellMainThreadStart(
            object obj
            )
        {
            try
            {
                IEnumerable<string> args = obj as IEnumerable<string>;

                TraceOps.DebugTrace(String.Format(
                    "ShellMainThreadStart: entered, args = {0}",
                    FormatOps.WrapArgumentsOrNull(true, true, args)),
                    typeof(ShellOps).Name,
                    TracePriority.ThreadDebug);

                ExitCode exitCode = Interpreter.ShellMain(args);

                TraceOps.DebugTrace(String.Format(
                    "ShellMainThreadStart: exited, args = {0}, " +
                    "exitCode = {1}",
                    FormatOps.WrapArgumentsOrNull(true, true, args),
                    exitCode),
                    typeof(ShellOps).Name,
                    TracePriority.ThreadDebug);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Thread CreateShellMainThread(
            IEnumerable<string> args,
            bool start
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            try
            {
                Thread shellMainThread = Engine.CreateThread(
                    ShellMainThreadStart, 0, true, false);

                if (shellMainThread != null)
                {
                    shellMainThread.Name = "shellMainThread";

                    if (start)
                        shellMainThread.Start(args);

                    return shellMainThread;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);
            }

            return null;
        }
        #endregion
    }
}
