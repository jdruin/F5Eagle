/*
 * ProcessOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("41b56439-03b9-4a8d-932a-aca836c99823")]
    internal static class ProcessOps
    {
        #region Private Data
        private static readonly object syncRoot = new object();

        private static IntStringBuilderDictionary processOutput = null;
        private static IntStringBuilderDictionary processError = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (processOutput != null))
                    localList.Add("ProcessOutput", (processOutput != null) ?
                        FormatOps.WrapOrNull(processOutput) :
                        FormatOps.DisplayNull);

                if (empty || (processError != null))
                    localList.Add("ProcessError", (processError != null) ?
                        FormatOps.WrapOrNull(processError) :
                        FormatOps.DisplayNull);

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Process Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int ClearOutputCache()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (processOutput != null)
                {
                    result += processOutput.Count;

                    processOutput.Clear();
                    processOutput = null;
                }

                if (processError != null)
                {
                    result += processError.Count;

                    processError.Clear();
                    processError = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void Initialize()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (processOutput == null)
                    processOutput = new IntStringBuilderDictionary();

                if (processError == null)
                    processError = new IntStringBuilderDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetHandle()
        {
            Process process = Process.GetCurrentProcess();

            if (process == null)
                return IntPtr.Zero;

            return process.Handle;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetId()
        {
            Process process = Process.GetCurrentProcess();

            if (process == null)
                return 0;

            return process.Id;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long GetParentId()
        {
#if NATIVE
            return NativeOps.GetParentProcessId().ToInt64();
#else
            return 0;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode KillProcess(
            Interpreter interpreter,
            string idOrPattern,
            bool all,
            bool force,
            ref Result result
            )
        {
            if (interpreter != null)
            {
                int id = 0;

                if (Value.GetInteger2(idOrPattern, ValueFlags.AnyInteger,
                        interpreter.CultureInfo, ref id) == ReturnCode.Ok)
                {
                    if (!all)
                    {
                        Process process = null;

                        try
                        {
                            //
                            // NOTE: Attempt to get a specific process by Id.
                            //
                            process = Process.GetProcessById(id); /* throw */

                            if (force)
                            {
                                //
                                // NOTE: Attempt to termiante the process immediately.
                                //
                                process.Kill(); /* throw */

                                //
                                // NOTE: If we get here, it should be dead now.
                                //
                                result = StringList.MakeList(
                                    "killed", FormatOps.ProcessName(process, false));

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                //
                                // NOTE: Attempt to gracefully close the process.
                                //
                                if (process.CloseMainWindow()) /* throw */
                                {
                                    //
                                    // NOTE: We report that it was closed; however, this may not
                                    //       actually be the case if the application cancels the
                                    //       close (which we have no nice way of detecting).
                                    //
                                    result = StringList.MakeList(
                                        "closed", FormatOps.ProcessName(process, false));

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    result = String.Format(
                                        "could not close process {0}",
                                        FormatOps.ProcessName(process, true));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            result = FormatOps.ErrorWithException(String.Format(
                                "could not kill process {0}", FormatOps.ProcessName(
                                process, true)), e);
                        }
                    }
                    else
                    {
                        result = "option \"-all\" cannot be used with a pid";
                    }
                }
                else
                {
                    //
                    // NOTE: List used to keep track of which processes have been killed.
                    //
                    StringList killed = new StringList();

                    //
                    // NOTE: Reset result here because we append to it when we are trying
                    //       to kill multiple processes and encounter an error.
                    //
                    result = null;

                    //
                    // NOTE: Iterate over all processes running on this computer and attempt
                    //       to match them against the supplied pattern (which may simply be
                    //       a literal process name).
                    //
                    foreach (Process process in Process.GetProcesses())
                    {
                        if (process == null)
                            continue;

                        string fileName = PathOps.GetProcessMainModuleFileName(
                            process, false);

                        //
                        // NOTE: Now that we have the file name (or null), attempt to match the
                        //       process based on the name, the fully qualified file name, or
                        //       the file name without the path.
                        //
                        MatchMode mode = StringOps.DefaultMatchMode;
                        bool match = false;

                        if (StringOps.Match(
                                interpreter, mode, process.ProcessName, idOrPattern, true))
                        {
                            //
                            // NOTE: Matched process name.
                            //
                            match = true;
                        }
                        else if (!String.IsNullOrEmpty(fileName))
                        {
                            if (StringOps.Match(interpreter, mode, fileName, idOrPattern, true) ||
                                StringOps.Match(interpreter, mode, Path.GetFileName(fileName), idOrPattern, true))
                            {
                                //
                                // NOTE: Matched fully qualified file name or
                                //       file name without the path.
                                //
                                match = true;
                            }
                        }

                        if (match)
                        {
                            try
                            {
                                if (force)
                                {
                                    //
                                    // NOTE: Attempt to termiante the process immediately.
                                    //
                                    process.Kill(); /* throw */

                                    //
                                    // NOTE: If we get here, it should be dead now.
                                    //
                                    killed.Add(StringList.MakeList(
                                        "killed", FormatOps.ProcessName(process, false)));
                                }
                                else
                                {
                                    //
                                    // NOTE: Attempt to gracefully close the process.
                                    //
                                    if (process.CloseMainWindow()) /* throw */
                                    {
                                        //
                                        // NOTE: We report that it was closed; however, this may not
                                        //       actually be the case if the application cancels the
                                        //       close (which we have no nice way of detecting).
                                        //
                                        killed.Add(StringList.MakeList(
                                            "closed", FormatOps.ProcessName(process, false)));
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: If the error message is null, we have not failed
                                        //       previously; otherwise we need to append a newline
                                        //       to the error message.
                                        //
                                        if (result != null)
                                            result += Environment.NewLine;
                                        else
                                            result = String.Empty;

                                        //
                                        // NOTE: Append the error message for this process.
                                        //
                                        result += String.Format(
                                            "could not close process {0}",
                                            FormatOps.ProcessName(process, true));

                                        //
                                        // NOTE: If they do not want to kill all matching processes,
                                        //       abort now (since we were going to stop anyhow if we
                                        //       successfully killed this process).
                                        //
                                        if (!all)
                                            return ReturnCode.Error;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                //
                                // NOTE: If the error message is null, we have not failed
                                //       previously; otherwise we need to append a newline
                                //       to the error message.
                                //
                                if (result != null)
                                    result += Environment.NewLine;
                                else
                                    result = String.Empty;

                                //
                                // NOTE: Append the error message for this process.
                                //
                                result += FormatOps.ErrorWithException(String.Format(
                                    "could not kill process {0}", FormatOps.ProcessName(
                                    process, true)), e);

                                //
                                // NOTE: If they do not want to kill all matching processes,
                                //       abort now (since we were going to stop anyhow if we
                                //       successfully killed this process).
                                //
                                if (!all)
                                    return ReturnCode.Error;
                            }

                            //
                            // NOTE: If they do not want to kill all matching
                            //       processes, stop now.
                            //
                            if (!all)
                            {
                                result = killed;

                                return ReturnCode.Ok;
                            }
                        }
                    }

                    //
                    // NOTE: If the error message is null, we succeeded in "all" mode.
                    //
                    if (result == null)
                    {
                        if (killed.Count > 0)
                        {
                            //
                            // NOTE: We succeeded, return the list of processes that
                            //       we killed.
                            //
                            result = killed;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            //
                            // NOTE: No matching processes were found.  This is an
                            //       error.
                            //
                            result = String.Format(
                                "no such process \"{0}\"",
                                idOrPattern);
                        }
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveProcess(
            Process process,
            ref int id
            )
        {
            if (process != null)
            {
                try
                {
                    //
                    // NOTE: Did we start the process or was it already running?
                    //       If the process was already running, this will throw
                    //       an exception.
                    //
                    id = process.Id; /* throw */

                    return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteProcess(
            Interpreter interpreter, /* the interpreter context to use, if any */
            string domainName,       /* the domain name for the logon, if any */
            string userName,         /* the user name for the logon, if any */
            SecureString password,   /* the password for the logon, if any */
            string fileName,         /* the executable file for the new process */
            string arguments,        /* the command line arguments for the new process, if any */
            string directory,        /* the working directory for the new process, if any */
            string input,            /* the simulated input for the new process, if any */
            EventFlags eventFlags,   /* event flags to use while waiting for new process to exit */
            bool useShellExecute,    /* use ShellExecute instead of CreateProcess? */
            bool captureExitCode,    /* populate the exitCode var? */
            bool captureOutput,      /* populate the result, and error vars? */
            bool useUnicode,         /* captured output from process will be Unicode? */
            bool ignoreStdErr,       /* true to not capture output to stderr (COMPAT: Tcl). */
            bool killOnError,        /* true to kill process on interpreter error (e.g. exit). */
            bool keepNewLine,        /* false to remove final cr/lf pair from output */
            bool background,         /* prevent waiting on child process to exit */
            bool events,             /* process events while waiting (non-background only) */
            ref int processId,       /* upon returning, the Id of the started process, if any */
            ref ExitCode exitCode,   /* upon success, ExitCode from child process */
            ref Result result,       /* upon success, output from StdOut */
            ref Result error         /* upon success, output from StdErr; otherwise, error information */
            )
        {
            if (!String.IsNullOrEmpty(fileName))
            {
                //
                // NOTE: Check if the file name is really a remote URI.  If not, the file
                //       name will be made absolute.
                //
                bool remoteUri = false;

                fileName = PathOps.SubstituteOrResolvePath(interpreter, fileName, true,
                    ref remoteUri);

                //
                // NOTE: The file name may have changed.  Make sure
                //       it is still not null or an empty string.
                //
                if (!String.IsNullOrEmpty(fileName))
                {
                    //
                    // NOTE: If this is an absolute [local] path, verify that it exists;
                    //       otherwise, it could be anything, including shell commands
                    //       (i.e. "things we cannot verify").
                    //
                    if (remoteUri || !Path.IsPathRooted(fileName) || File.Exists(fileName))
                    {
                        //
                        // NOTE: If they supplied a working directory for the child
                        //       process, normalize it and then use it; otherwise, use
                        //       the current directory for this process.
                        //
                        string workingDirectory;

                        if (directory != null)
                            workingDirectory = PathOps.ResolveFullPath(interpreter, directory);
                        else
                            workingDirectory = Directory.GetCurrentDirectory();

                        //
                        // NOTE: At this point, we must have some valid working directory.
                        //
                        if (workingDirectory != null)
                        {
                            //
                            // NOTE: Create an object to place the child process creation
                            //       parameters into and start populating it.
                            //
                            ProcessStartInfo startInfo = new ProcessStartInfo();

                            //
                            // NOTE: If requested (and applicable), set the domain name, user
                            //       name, and password.
                            //
                            if (!useShellExecute)
                            {
                                if (domainName != null)
                                    startInfo.Domain = domainName;

                                if (userName != null)
                                    startInfo.UserName = userName;

                                if (password != null)
                                    startInfo.Password = password;
                            }

                            //
                            // NOTE: Set the file name and working directory.  At this point,
                            //       these values should be normalized and reasonably well
                            //       verified.
                            //
                            startInfo.FileName = fileName;
                            startInfo.WorkingDirectory = workingDirectory;

                            //
                            // NOTE: If requested, reset the encodings for the standard output
                            //       and error streams to Unicode (i.e. UTF-16).
                            //
                            if (useUnicode)
                            {
                                startInfo.StandardOutputEncoding = Encoding.Unicode;
                                startInfo.StandardErrorEncoding = Encoding.Unicode;
                            }

                            //
                            // NOTE: If they supplied arguments, use them.
                            //
                            if (!String.IsNullOrEmpty(arguments))
                                startInfo.Arguments = arguments;

                            //
                            // NOTE: Do they want to exec the new process via ShellExecute?
                            //       That prevents them from doing other things, like capturing
                            //       the output from the child process.
                            //
                            startInfo.UseShellExecute = useShellExecute;

                            //
                            // NOTE: Setup the necessary input/output redirection based on the
                            //       other options they specified.
                            //
                            //       We do not want background processes using our StdIn (not
                            //       applicable if we use ShellExecute).
                            //
                            //       We want to be able to capture both the StdOut and StdErr
                            //       channels from the child process for non-background processes.
                            //
                            startInfo.RedirectStandardInput =
                                (!useShellExecute && (background || (input != null)));

                            //
                            // NOTE: Only capture output if we plan on using it later.
                            //
                            if (captureOutput)
                            {
                                startInfo.RedirectStandardOutput =
                                    (!useShellExecute && !background);

                                startInfo.RedirectStandardError =
                                    (!ignoreStdErr && !useShellExecute && !background);
                            }

                            try
                            {
                                //
                                // NOTE: If necessary, initialize the static data used by this
                                //       class.
                                //
                                Initialize();

                                //
                                // NOTE: Create a child process OBJECT.  This does not actually
                                //       start the process.
                                //
                                Process process = new Process();

                                //
                                // NOTE: Set the child process creation parameters to the ones
                                //       we populated above.
                                //
                                process.StartInfo = startInfo;

                                //
                                // NOTE: If necessary, setup asynchronous output capture events
                                //       for the newly created process.
                                //
                                if (startInfo.RedirectStandardOutput)
                                    process.OutputDataReceived +=
                                        new DataReceivedEventHandler(OutputDataReceived);

                                if (startInfo.RedirectStandardError)
                                    process.ErrorDataReceived +=
                                        new DataReceivedEventHandler(ErrorDataReceived);

                                //
                                // NOTE: Start the process.  We may or may not wait for it to
                                //       complete before returning (see below).
                                //
                                process.Start(); /* throw */

                                //
                                // NOTE: The Id of the newly started process, if any, will go
                                //       here.  We will never access the Id property of the
                                //       process directly because it can throw an exception.
                                //       Therefore, a helper method is used to "safely" query
                                //       it and place the value in this local variable.
                                //
                                int id = 0;

                                //
                                // NOTE: Did we actually start a new process?  If not, several
                                //       things later on will not work and we need to know that.
                                //
                                bool haveProcess = HaveProcess(process, ref id);

                                //
                                // NOTE: Give the caller the Id of the process that we started.
                                //       This value may be zero if we did not actually start a
                                //       new process.
                                //
                                processId = id;

                                //
                                // NOTE: If necessary, setup the process normal output buffer
                                //       (we waited until now because we needed the process Id
                                //       to create it) and start capturing normal output from
                                //       the process asynchronously.
                                //
                                if (haveProcess && startInfo.RedirectStandardOutput)
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        if (processOutput != null)
                                            processOutput.Add(id, StringOps.NewStringBuilder());
                                    }

                                    process.BeginOutputReadLine(); /* throw */
                                }

                                //
                                // NOTE: If necessary, setup the process error output buffer
                                //       (we waited until now because we needed the process Id
                                //       to create it) and start capturing error output from
                                //       the process asynchronously.
                                //
                                if (haveProcess && startInfo.RedirectStandardError)
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        if (processError != null)
                                            processError.Add(id, StringOps.NewStringBuilder());
                                    }

                                    process.BeginErrorReadLine(); /* throw */
                                }

                                //
                                // NOTE: If requested (and possible), write the provided input
                                //       string to the standard input stream for the started
                                //       process.
                                //
                                if (haveProcess && startInfo.RedirectStandardInput &&
                                    (input != null))
                                {
                                    StreamWriter processInput = process.StandardInput;

                                    if (processInput != null)
                                    {
                                        processInput.Write(input);
                                        processInput.Flush();
                                    }
                                }

                                //
                                // NOTE: Set the Id of the last process to be executed for this
                                //       interpreter unless we do not have an interpreter or we
                                //       did not actually start a new process.  In either of
                                //       those cases, just skip it.
                                //
                                if (haveProcess && (interpreter != null))
                                {
                                    lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                    {
                                        if (!Interpreter.IsDeletedOrDisposed(interpreter))
                                            interpreter.PreviousProcessId = id;
                                    }
                                }

                                //
                                // NOTE: Are we running the process in the background and/or were
                                //       we unable to start a new process (i.e. we used the shell
                                //       and it reused an existing browser process, etc)?
                                //
                                if (background || !haveProcess)
                                {
                                    //
                                    // NOTE: For background child processes, we do not wait and we
                                    //       return the PID of the child process.  This value may
                                    //       be zero if we did not actually start a new process.
                                    //
                                    result = id;
                                }
                                else
                                {
                                    //
                                    // NOTE: Here, we wait for the child process to exit and then
                                    //       record the results.
                                    //
                                    if (events && (interpreter != null))
                                    {
                                        //
                                        // NOTE: Get the minimum sleep time for the interpreter.
                                        //
                                        int sleepMilliseconds = interpreter.GetMinimumSleepTime();

                                        //
                                        // NOTE: Keep going until the child process has exited.
                                        //
                                        while (!process.HasExited) /* throw */
                                        {
                                            //
                                            // NOTE: We need a local result because we do not want to change
                                            //       the caller's result based on random async events that
                                            //       get processed while waiting for their variable to become
                                            //       "signaled".  However, we will change the caller's result
                                            //       if an error is encountered.
                                            //
                                            Result localResult = null;

                                            //
                                            // NOTE: Attempt to process all pending events stopping if an error
                                            //       is encountered.
                                            //
                                            if (Engine.CheckEvents(
                                                    interpreter, eventFlags, ref localResult) != ReturnCode.Ok)
                                            {
                                                try
                                                {
                                                    if (killOnError)
                                                        process.Kill(); /* throw */
                                                }
                                                catch (Exception e)
                                                {
                                                    TraceOps.DebugTrace(
                                                        e, typeof(ProcessOps).Name,
                                                        TracePriority.PlatformError);
                                                }

                                                error = localResult;
                                                return ReturnCode.Error;
                                            }

                                            //
                                            // NOTE: Prevent this loop from needlessly spinning while waiting for
                                            //       the child process to exit.
                                            //
                                            if (process.WaitForExit(sleepMilliseconds)) /* throw */
                                            {
                                                //
                                                // NOTE: The child process has now exited, bail out of loop now.
                                                //
                                                break;
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: We always try to yield to other running threads while
                                                //       the child process is still running.  This also gives
                                                //       them an opportunity to cancel waiting on the child
                                                //       process and then optionally terminate it.
                                                //
                                                try
                                                {
                                                    EventOps.Sleep(interpreter, true); /* throw */
                                                }
                                                catch (Exception e)
                                                {
                                                    try
                                                    {
                                                        if (killOnError)
                                                            process.Kill(); /* throw */
                                                    }
                                                    catch (Exception e2)
                                                    {
                                                        TraceOps.DebugTrace(
                                                            e2, typeof(ProcessOps).Name,
                                                            TracePriority.PlatformError);
                                                    }

                                                    error = e;
                                                    return ReturnCode.Error;
                                                }
                                            }

                                            //
                                            // NOTE: Force the "cached" state of the process to be refreshed
                                            //       so that the HasExited property has a better chance of
                                            //       actually being accurate.
                                            //
                                            process.Refresh();
                                        }
                                    }

                                    //
                                    // NOTE: Now, block until we are sure that we have received all pending
                                    //       output from the process and just to make sure that the process
                                    //       has really exited (apparently, the HasExited property cannot
                                    //       always be trusted).  Also, if the caller did not choose to
                                    //       process events while waiting, this should keep us in sync.
                                    //
                                    process.WaitForExit(); /* throw */

                                    //
                                    // NOTE: Save the exit code for later use by the caller?  We do NOT try
                                    //       to actually interpret the meaning of it here.
                                    //
                                    if (captureExitCode)
                                        exitCode = (ExitCode)process.ExitCode; /* throw */

                                    //
                                    // NOTE: Only populate the caller's variables if we are requested to do
                                    //       so.
                                    //
                                    if (captureOutput)
                                    {
                                        //
                                        // NOTE: If we used ShellExecute, the output of the child process is
                                        //       unavailable; otherwise, save it for later use by the caller.
                                        //
                                        if (useShellExecute)
                                        {
                                            result = null;
                                            error = null;
                                        }
                                        else
                                        {
                                            string localResult = null;
                                            string localError = null;

                                            //
                                            // NOTE: If we were capturing normal output from the process,
                                            //       attempt to safely fetch it from the dictionary now.
                                            //
                                            if (haveProcess && startInfo.RedirectStandardOutput)
                                            {
                                                lock (syncRoot) /* TRANSACTIONAL */
                                                {
                                                    StringBuilder builder;

                                                    if ((processOutput != null) &&
                                                        processOutput.TryGetValue(id, out builder))
                                                    {
                                                        localResult = (builder != null) ?
                                                            builder.ToString() : null;
                                                    }
                                                }
                                            }

                                            //
                                            // NOTE: If we were capturing error output from the process,
                                            //       attempt to safely fetch it from the dictionary now.
                                            //
                                            if (haveProcess && startInfo.RedirectStandardError)
                                            {
                                                lock (syncRoot) /* TRANSACTIONAL */
                                                {
                                                    StringBuilder builder;

                                                    if ((processError != null) &&
                                                        processError.TryGetValue(id, out builder))
                                                    {
                                                        localError = (builder != null) ?
                                                            builder.ToString() : null;
                                                    }
                                                }
                                            }

                                            //
                                            // NOTE: Do they want to retain the final trailing newline
                                            //       character sequence?
                                            //
                                            // COMPAT: Tcl.
                                            //
                                            if (keepNewLine)
                                            {
                                                //
                                                // NOTE: They want to keep the trailing newline sequence,
                                                //       give them the output verbatim.
                                                //
                                                result = localResult;
                                                error = localError;
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: They want to strip the trailing newline sequence,
                                                //       if any, from the normal output (for StdOut).
                                                //
                                                if (!String.IsNullOrEmpty(localResult) &&
                                                    localResult.EndsWith(Environment.NewLine,
                                                        StringOps.SystemStringComparisonType))
                                                {
                                                    result = localResult.Substring(0,
                                                        localResult.Length - Environment.NewLine.Length);
                                                }
                                                else
                                                {
                                                    result = localResult;
                                                }

                                                //
                                                // NOTE: They also want to strip the trailing newline sequence,
                                                //       if any, from the error output (for StdErr).
                                                //
                                                if (!String.IsNullOrEmpty(localError) &&
                                                    localError.EndsWith(Environment.NewLine,
                                                        StringOps.SystemStringComparisonType))
                                                {
                                                    error = localError.Substring(0,
                                                        localError.Length - Environment.NewLine.Length);
                                                }
                                                else
                                                {
                                                    error = localError;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = null;
                                        error = null;
                                    }
                                }

                                //
                                // NOTE: Destroy the normal output buffer we created earlier, if any.
                                //
                                if (haveProcess && startInfo.RedirectStandardOutput)
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        StringBuilder builder;

                                        if ((processOutput != null) &&
                                            processOutput.TryGetValue(id, out builder))
                                        {
                                            if (builder != null)
                                                builder.Length = 0;

                                            processOutput.Remove(id);
                                        }
                                    }
                                }

                                //
                                // NOTE: Destroy the error output buffer we created earlier, if any.
                                //
                                if (haveProcess && startInfo.RedirectStandardError)
                                {
                                    lock (syncRoot) /* TRANSACTIONAL */
                                    {
                                        StringBuilder builder;

                                        if ((processError != null) &&
                                            processError.TryGetValue(id, out builder))
                                        {
                                            if (builder != null)
                                                builder.Length = 0;

                                            processError.Remove(id);
                                        }
                                    }
                                }

                                //
                                // NOTE: We succeeded, even if the child process itself returned "failure"
                                //       because we know it was created at this point.  It is the
                                //       responsibility of the caller of this function to interpret the
                                //       exit code and output from the process in some meaningful way.
                                //
                                return ReturnCode.Ok;
                            }
                            catch (Exception e)
                            {
                                //
                                // NOTE: We failed to create the child process for some reason.
                                //
                                error = e;
                            }
                        }
                        else
                        {
                            error = String.Format(
                                "invalid working directory \"{0}\"",
                                workingDirectory);
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "couldn't execute \"{0}\": no such file or directory",
                            fileName);
                    }
                }
                else
                {
                    //
                    // NOTE: Really, the file name is just plain invalid at this point
                    //       (it could not be normalized for some reason);  however,
                    //       the difference is really academic.
                    //
                    error = String.Format(
                        "couldn't execute \"{0}\": no such file or directory",
                        fileName);
                }
            }
            else
            {
                //
                // NOTE: Yes, we know that the file name is null or an empty string;
                //       however, this is still the right error message.
                //
                error = String.Format(
                    "couldn't execute \"{0}\": no such file or directory",
                    fileName); /* COMPAT: Tcl. */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteProcess(
            Interpreter interpreter,
            string fileName,
            string arguments,
            string directory,
            EventFlags eventFlags,
            bool useUnicode,
            ref int processId,
            ref ExitCode exitCode,
            ref Result result,
            ref Result error
            )
        {
            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments,
                directory, null, eventFlags, false, true, true,
                useUnicode, false, false, true, false, true,
                ref processId, ref exitCode, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteProcess(
            Interpreter interpreter,
            string fileName,
            string arguments,
            EventFlags eventFlags,
            ref ExitCode exitCode,
            ref Result result,
            ref Result error
            )
        {
            int processId = 0;

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments, null,
                null, eventFlags, false, true, true, false, false, true,
                false, false, true, ref processId, ref exitCode, ref result,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        public static ReturnCode ShellExecuteProcess(
            Interpreter interpreter,
            string fileName,
            string arguments,
            string directory,
            EventFlags eventFlags,
            ref Result error
            )
        {
            int processId = 0;
            ExitCode exitCode = ResultOps.SuccessExitCode();
            Result result = null;

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments, directory,
                null, eventFlags, true, false, false, false, false, false,
                false, false, true, ref processId, ref exitCode, ref result,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteProcess(
            Interpreter interpreter,
            string fileName,
            string arguments,
            string directory,
            EventFlags eventFlags,
            ref Result error
            )
        {
            int processId = 0;
            ExitCode exitCode = ResultOps.SuccessExitCode();
            Result result = null;

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments, directory,
                null, eventFlags, false, false, false, false, false, false,
                false, false, true, ref processId, ref exitCode, ref result,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExecuteProcess(
            Interpreter interpreter,
            string fileName,
            string arguments,
            string directory,
            EventFlags eventFlags,
            bool background,
            ref Result error
            )
        {
            int processId = 0;
            ExitCode exitCode = ResultOps.SuccessExitCode();
            Result result = null;

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments, directory,
                null, eventFlags, false, false, false, false, false, false,
                false, background, true, ref processId, ref exitCode, ref result,
                ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void OutputDataReceived(
            object sender,
            DataReceivedEventArgs e
            )
        {
            Process process = sender as Process;

            if ((process != null) && (e != null))
            {
                int id = 0;

                if (HaveProcess(process, ref id))
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        StringBuilder builder;

                        if ((processOutput != null) &&
                            processOutput.TryGetValue(id, out builder) &&
                            (builder != null))
                        {
                            builder.AppendLine(e.Data);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ErrorDataReceived(
            object sender,
            DataReceivedEventArgs e
            )
        {
            Process process = sender as Process;

            if ((process != null) && (e != null))
            {
                int id = 0;

                if (HaveProcess(process, ref id))
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        StringBuilder builder;

                        if ((processError != null) &&
                            processError.TryGetValue(id, out builder) &&
                            (builder != null))
                        {
                            builder.AppendLine(e.Data);
                        }
                    }
                }
            }
        }
    }
}
