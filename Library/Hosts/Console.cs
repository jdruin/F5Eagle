/*
 * Console.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !CONSOLE
#error "This file cannot be compiled or used properly with console support disabled."
#endif

using System;
using System.IO;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if DRAWING
using System.Drawing;
#endif

namespace Eagle._Hosts
{
    [ObjectId("e15283cf-00b4-44f2-a16e-48cf061e53d1")]
    public class Console : Core, IDisposable
    {
        #region Private Static Data
#if NATIVE && WINDOWS
        private static int closeCount = 0;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int referenceCount = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int mustBeOpenCount = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Setting this value to non-zero will force this class to treat
        //       non-default application domains [more-or-less] like the default
        //       application domain one (e.g. the Ctrl-C keypress handler will
        //       be added/removed).
        //
        private static bool forceNonDefaultAppDomain = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int certificateCount = 0;
        private string certificateSubject = null; /* CACHED */

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && DRAWING
        private Icon icon;
        private IntPtr oldBigIcon;
        private IntPtr oldSmallIcon;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string savedTitle;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor savedForegroundColor = _ConsoleColor.None;
        private ConsoleColor savedBackgroundColor = _ConsoleColor.None;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int savedWindowWidth = _Size.Invalid;
        private int savedWindowHeight = _Size.Invalid;
        private int savedBufferWidth = _Size.Invalid;
        private int savedBufferHeight = _Size.Invalid;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        #region Output Size Constants
        //
        // NOTE: Apparently, the underlying WriteConsoleW call used by the
        //       System.Console class has an internal limit of *somewhere*
        //       between 26000 and 32768 characters (i.e. about 65536 bytes,
        //       give or take?).  This limit is not exact and cannot be
        //       readily predicted in advance.  Several sources on the web
        //       seem to indicate that <=26000 characters should be a safe
        //       write size.  Please refer to the following links for more
        //       information:
        //
        //       https://msdn.microsoft.com/en-us/library/ms687401.aspx
        //
        //       https://mail-archives.apache.org/mod_mbox/logging-log4net
        //           -dev/200501.mbox/%3CD44F10C7974F5D4BAFAC9D37A127D5600
        //           1B7B05F@raven.tdsway.com%3E
        //
        //       https://bit.ly/1Akk2YI (shortened version of above)
        //
        //       https://www.mail-archive.com/log4net-dev@logging.apache.
        //           org/msg00645.html
        //
        //       https://bit.ly/2d3EniG (shortened version of above)
        //
        internal static readonly int SafeWriteSize = 25000; /* NOTE: <=26000 */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Buffer Size Constants
        private static readonly int MaximumBufferWidthMargin = 8;
        private static readonly int MaximumBufferHeight = 9999;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Width Constants
        //
        // HACK: These are considered to be "best guess" values.
        //       Please adjust them to suit your taste as necessary.
        //
        private static readonly int MinimumWindowWidth = 40;
        private static readonly int CompactWindowWidth = 80;
        private static readonly int FullWindowWidth = 120;
        private static readonly int SuperFullWindowWidth = 160;
        private static readonly int JumboWindowWidth = 200;
        private static readonly int SuperJumboWindowWidth = 230;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Height Constants
        private static readonly int MinimumWindowHeight = 10;
        private static readonly int CompactWindowHeight = 25;
        private static readonly int FullWindowHeight = 40;
        private static readonly int SuperFullWindowHeight = 60;
        private static readonly int JumboWindowHeight = 75;
        private static readonly int SuperJumboWindowHeight = 90;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Margin Constants
        private static readonly int MaximumWindowWidthMargin = MaximumBufferWidthMargin;
        private static readonly int MaximumWindowHeightMargin = 6;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Title Constants
        private static readonly string AdministratorTitlePrefix = "Administrator:";
        private static readonly string CertificateSubjectPrefix = "- ";
        private static readonly string CertificateSubjectPending = "checking certificate...";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Console(
            IHostData hostData
            )
            : base(hostData)
        {
            //
            // NOTE: Enable throwing exceptions when the various
            //       SystemConsole*MustBeOpen() methods are called.
            //
            EnableThrowOnMustBeOpen();

            //
            // NOTE: Save the original buffer and window sizes.
            //
            /* IGNORED */
            SaveSize();

            //
            // NOTE: Save the original colors.
            //
            /* IGNORED */
            SaveColors();

            /* IGNORED */
            Setup(this, true, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        private bool PrivateResetFlags()
        {
            hostFlags = HostFlags.Invalid;

            return base.ResetFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetWriteException(
            bool exception
            )
        {
            base.SetWriteException(exception);
            hostFlags = HostFlags.Invalid;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Read/Write Levels Support
        protected override void EnterReadLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref sharedReadLevels);
            base.EnterReadLevel();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void ExitReadLevel()
        {
            // CheckDisposed();

            base.ExitReadLevel();
            Interlocked.Decrement(ref sharedReadLevels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void EnterWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref sharedWriteLevels);
            base.EnterWriteLevel();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void ExitWriteLevel()
        {
            // CheckDisposed();

            base.ExitWriteLevel();
            Interlocked.Decrement(ref sharedWriteLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Handling
        #region Native Console Stream Handling
        private static bool SystemConsoleIsRedirected(
            Interpreter interpreter,
            ChannelType channelType,
            bool @default
            )
        {
#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                IntPtr handle;
                Result error = null;

                handle = NativeConsole.GetHandle(channelType, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    bool redirected = false;

                    if (NativeConsole.IsHandleRedirected(handle,
                            ref redirected, ref error) == ReturnCode.Ok)
                    {
                        return redirected;
                    }
                }

                //
                // NOTE: Either we failed to get the handle or we could
                //       not determine if it has been redirected.  This
                //       condition should be relatively rare, complain.
                //
                DebugOps.Complain(interpreter, ReturnCode.Error, error);
            }

            return false;
#else
            return @default;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SystemConsoleInputIsRedirected()
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false),
                ChannelType.Input, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SystemConsoleOutputIsRedirected()
        {
            Interpreter interpreter = InternalSafeGetInterpreter(
                false);

            if (SystemConsoleIsRedirected(
                    interpreter, ChannelType.Output, false) ||
                SystemConsoleIsRedirected(
                    interpreter, ChannelType.Error, false))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SystemConsoleErrorIsRedirected()
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false),
                ChannelType.Error, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Open/Close Handling
        private static void EnableThrowOnMustBeOpen()
        {
            //
            // NOTE: If necessary, enable throwing exceptions from
            //       within the SystemConsole*MustBeOpen() methods.
            //
            if (!ThrowOnMustBeOpen) ThrowOnMustBeOpen = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static bool ThrowOnMustBeOpen
        {
            get
            {
                try
                {
                    return Interlocked.Increment(ref mustBeOpenCount) > 1;
                }
                finally
                {
                    Interlocked.Decrement(ref mustBeOpenCount);
                }
            }
            set
            {
                if (value)
                    Interlocked.Increment(ref mustBeOpenCount);
                else
                    Interlocked.Decrement(ref mustBeOpenCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool SystemConsoleIsOpen(
            bool window
            )
        {
#if NATIVE && WINDOWS
            //
            // NOTE: Are there any outstanding calls to the NativeConsole.Close
            //       method (i.e. those that have not been matched by calls to
            //       the NativeConsole.Open method)?
            //
            if (Interlocked.CompareExchange(ref closeCount, 0, 0) > 0)
                return false;

            if (window &&
                PlatformOps.IsWindowsOperatingSystem() &&
                !NativeConsole.IsOpen())
            {
                return false;
            }

            return SystemConsoleInputIsOpen(); /* COMPAT: Eagle beta. */
#else
            return true;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool SystemConsoleInputIsOpen()
        {
            try
            {
#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                bool value = System.Console.TreatControlCAsInput; /* EXEMPT */
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool SystemConsoleOutputIsOpen()
        {
            try
            {
#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                bool value = System.Console.CursorVisible; /* EXEMPT */
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected static void SystemConsoleMustBeOpen(
            bool window
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (!SystemConsoleIsOpen(window))
            {
                throw new ScriptException(String.Format(
                    "system console {0}is not available",
                    window ? "window " : String.Empty));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SystemConsoleInputMustBeOpen(
            IInteractiveHost interactiveHost
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (((interactiveHost == null) || !interactiveHost.IsInputRedirected()) &&
                !SystemConsoleInputIsOpen() &&
                !SystemConsoleIsRedirected(null, ChannelType.Input, true))
            {
                throw new ScriptException(
                    "system console input channel is not available");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SystemConsoleOutputMustBeOpen(
            IStreamHost streamHost
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (((streamHost == null) || !streamHost.IsOutputRedirected()) &&
                !SystemConsoleOutputIsOpen() &&
                !SystemConsoleIsRedirected(null, ChannelType.Output, true))
            {
                throw new ScriptException(
                    "system console output channel is not available");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void SystemConsoleErrorMustBeOpen(
            IStreamHost streamHost
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (((streamHost == null) || !streamHost.IsErrorRedirected()) &&
                !SystemConsoleOutputIsOpen() &&
                !SystemConsoleIsRedirected(null, ChannelType.Error, true))
            {
                throw new ScriptException(
                    "system console error channel is not available");
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Size Handling
        protected virtual bool FallbackGetLargestWindowSize(
            ref int width,
            ref int height
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                width = System.Console.LargestWindowWidth;
                height = System.Console.LargestWindowHeight;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override int WindowWidth
        {
            get
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */
                    return System.Console.WindowWidth;
                }
                catch
                {
                    return base.WindowWidth;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override int WindowHeight
        {
            get
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */
                    return System.Console.WindowHeight;
                }
                catch
                {
                    return base.WindowHeight;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SaveSize()
        {
            lock (syncRoot)
            {
                return SaveSize(
                    ref savedBufferWidth, ref savedBufferHeight,
                    ref savedWindowWidth, ref savedWindowHeight);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SaveSize(
            ref int bufferWidth,
            ref int bufferHeight,
            ref int windowWidth,
            ref int windowHeight
            )
        {
            //
            // NOTE: Save original console dimensions in case we need
            //       to restore from the later (e.g. ResetSize).
            //
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                bufferWidth = System.Console.BufferWidth;
                bufferHeight = System.Console.BufferHeight;

                windowWidth = System.Console.WindowWidth;
                windowHeight = System.Console.WindowHeight;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SetSize(
            int bufferWidth,
            int bufferHeight,
            int windowWidth,
            int windowHeight
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                //
                // NOTE: Set the window size to the minimum possible so that
                //       any buffer size we set (within bounds) is valid.
                //
                System.Console.SetWindowSize(1, 1);

                //
                // NOTE: Set the new buffer size.
                //
                System.Console.SetBufferSize(bufferWidth, bufferHeight);

                //
                // NOTE: Set the new window size.
                //
                System.Console.SetWindowSize(windowWidth, windowHeight);

                //
                // NOTE: If we get this far, we've succeeded.
                //
                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool CalculateSize(
            int width,
            int height,
            bool maximum,
            ref int bufferWidth,
            ref int bufferHeight,
            ref int windowWidth,
            ref int windowHeight
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                //
                // NOTE: If the caller does not want to set the width (i.e. it
                //       is invalid) then use the current window width;
                //       otherwise, if setting up for the maximum console size,
                //       subtract the necessary width margin from the provided
                //       width value.
                //
                int newWindowWidth = width;

                if (newWindowWidth == _Size.Invalid)
                    newWindowWidth = System.Console.WindowWidth;
                else if (maximum)
                    newWindowWidth -= MaximumWindowWidthMargin;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: If the caller does not want to set the height (i.e. it
                //       is invalid) then use the current window height;
                //       otherwise, if setting up for the maximum console size,
                //       subtract the necessary height margin from the provided
                //       height value.
                //
                int newWindowHeight = height;

                if (newWindowHeight == _Size.Invalid)
                    newWindowHeight = System.Console.WindowHeight;
                else if (maximum)
                    newWindowHeight -= MaximumWindowHeightMargin;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: If the caller does not want to set the width (i.e. it
                //       is invalid) then use the current buffer width;
                //       otherwise, if setting up for the maximum console size,
                //       subtract the necessary width margin from the provided
                //       width value.
                //
                int newBufferWidth = width;

                if (newBufferWidth == _Size.Invalid)
                    newBufferWidth = System.Console.BufferWidth;
                else if (maximum)
                    newBufferWidth -= MaximumBufferWidthMargin;

                ///////////////////////////////////////////////////////////////

                //
                // HACK: *SPECIAL CASE* If the caller does not want to set the
                //       height (i.e. it is invalid) then use the current
                //       buffer height; otherwise, if setting up for the
                //       maximum console size, we always want to set the buffer
                //       height to the maximum "reasonable" value (i.e. for use
                //       as a scrollback buffer).  The maximum "reasonable"
                //       value is typically 9999 because that is what modern
                //       (all?) versions of Windows recognize for console-based
                //       applications via the shell properties dialog.
                //
                int newBufferHeight = height;

                if (newBufferHeight == _Size.Invalid)
                    newBufferHeight = System.Console.BufferHeight;
                else if (maximum)
                    newBufferHeight = MaximumBufferHeight;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Try to make sure that we do not attempt to set an
                //       unreasonable window width.
                //
                if ((newWindowWidth > newBufferWidth) ||
                    (newWindowWidth > System.Console.LargestWindowWidth))
                {
                    newWindowWidth = Math.Min(newBufferWidth,
                        System.Console.LargestWindowWidth);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Try to make sure that we do not attempt to set an
                //       unreasonable window height.
                //
                if ((newWindowHeight > newBufferHeight) ||
                    (newWindowHeight > System.Console.LargestWindowHeight))
                {
                    newWindowHeight = Math.Min(newBufferHeight,
                        System.Console.LargestWindowHeight);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Commit all changes to the output parameters provided
                //       by the caller.
                //
                bufferWidth = newBufferWidth;
                bufferHeight = newBufferHeight;
                windowWidth = newWindowWidth;
                windowHeight = newWindowHeight;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: If we get this far, we succeeded.
                //
                return true;
            }
            catch
            {
                //
                // NOTE: Something failed, just return false.
                //
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SetSize(
            int width,
            int height,
            bool maximum
            )
        {
            try
            {
                int newBufferWidth = _Size.Invalid;
                int newBufferHeight = _Size.Invalid;
                int newWindowWidth = _Size.Invalid;
                int newWindowHeight = _Size.Invalid;

                if (!CalculateSize(
                        width, height, maximum, ref newBufferWidth,
                        ref newBufferHeight, ref newWindowWidth,
                        ref newWindowHeight))
                {
                    return false;
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Reset the console buffer and window sizes.
                //
                return SetSize(
                    newBufferWidth, newBufferHeight,
                    newWindowWidth, newWindowHeight);
            }
            catch
            {
                //
                // NOTE: Something failed, just return false.
                //
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Color Save/Restore
        protected virtual bool SaveColors()
        {
            //
            // NOTE: Save original console colors in case we need to restore
            //       from the later.
            //
            try
            {
                lock (syncRoot)
                {
                    return GetColors(
                        ref savedForegroundColor, ref savedBackgroundColor);
                }
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool RestoreColors()
        {
            //
            // NOTE: Restore the originally saved console colors.
            //
            try
            {
                lock (syncRoot)
                {
                    return SetColors(true, true,
                        savedForegroundColor, savedBackgroundColor);
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Title Handling
        protected virtual string GetCertificateSubject()
        {
            lock (syncRoot)
            {
                if (Interlocked.Increment(ref certificateCount) == 1)
                {
                    //
                    // NOTE: Is trust checking enabled for executable files that are
                    //       signed with X509 certificates?  If not, no work will be
                    //       done by the RuntimeOps.GetCertificateSubject method.
                    //
                    // NOTE: Technically, this should [probably] not be using the
                    //       SetupOps.ShouldCheckCoreTrusted method here (i.e. since
                    //       the entry assembly could be a third-party executable;
                    //       however, in this particular context, "core" is intended
                    //       to include the shell as well, just not plugins).  Also,
                    //       the certificate subjects are checked (just below here)
                    //       for equality, prior to being displayed to the user.
                    //
                    bool trusted = RuntimeOps.ShouldCheckCoreFileTrusted();

                    //
                    // BUGFIX: Verify that the certificate subjects are the same for
                    //         this assembly (i.e. the Eagle core library) and the
                    //         entry assembly (e.g. the Eagle shell).
                    //
                    string thisCertificateSubject = RuntimeOps.GetCertificateSubject(
                        GlobalState.GetAssemblyLocation(), CertificateSubjectPrefix,
                        trusted, true);

                    if (thisCertificateSubject != null)
                    {
                        string entryCertificateSubject = RuntimeOps.GetCertificateSubject(
                            GlobalState.GetEntryAssemblyLocation(), CertificateSubjectPrefix,
                            trusted, true);

                        if (entryCertificateSubject != null)
                        {
                            if (String.Equals(
                                    thisCertificateSubject, entryCertificateSubject,
                                    StringOps.SystemStringComparisonType))
                            {
                                //
                                // NOTE: If we get to this point, the core assembly
                                //       (i.e. this one) and the entry assembly have
                                //       the same certificate subject.  Most likely,
                                //       this is because the entry assembly is the
                                //       standard Eagle shell assembly.
                                //
                                certificateSubject = thisCertificateSubject;
                            }
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "GetCertificateSubject: certificate subject " +
                                    "mismatch, core = {0}, entry = {1}",
                                    FormatOps.WrapOrNull(thisCertificateSubject),
                                    FormatOps.WrapOrNull(entryCertificateSubject)),
                                    typeof(Console).Name, TracePriority.HostDebug);

                                certificateSubject = null;
                            }
                        }
                        else
                        {
                            TraceOps.DebugTrace(
                                "GetCertificateSubject: no certificate subject for entry assembly",
                                typeof(Console).Name, TracePriority.HostDebug);

                            certificateSubject = null;
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "GetCertificateSubject: no certificate subject for core assembly",
                            typeof(Console).Name, TracePriority.HostDebug);

                        certificateSubject = null;
                    }
                }
                else
                {
                    Interlocked.Decrement(ref certificateCount);
                }

                return certificateSubject;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SaveTitle(
            ref Result error
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                lock (syncRoot)
                {
                    if (savedTitle == null)
                        savedTitle = System.Console.Title;
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool RestoreTitle(
            ref Result error
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                lock (syncRoot)
                {
                    if (savedTitle != null)
                    {
                        System.Console.Title = savedTitle;
                        savedTitle = null;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual string BuildTitle(
            Interpreter interpreter,
            bool certificate
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            string[] values = {
                RuntimeOps.IsAdministrator() ?
                    AdministratorTitlePrefix : String.Empty,
                DefaultTitle, base.Title, certificate ?
                    GetCertificateSubject() :
                    CertificateSubjectPending,
                HostOps.GetInteractiveMode(interpreter)
            };

            foreach (string value in values)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (result.Length > 0)
                        result.Append(Characters.Space);

                    result.Append(value);
                }
            }

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool SetupTitle(
            bool setup
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                //
                // NOTE: Has changing the title been explicitly disabled?
                //
                if (!NoTitle && IsOpen())
                {
                    Interpreter interpreter = InternalSafeGetInterpreter(
                        false);

                    Result error = null;

                    if (setup)
                    {
                        if (SaveTitle(ref error))
                        {
                            //
                            // HACK: Permit the user to see the original title
                            //       while the certificate is being checked,
                            //       if applicable.
                            //
                            foreach (bool certificate in new bool[] {
                                    false, true
                                })
                            {
                                System.Console.Title = BuildTitle(
                                    interpreter, certificate);
                            }

                            return true;
                        }
                        else
                        {
                            DebugOps.Complain(
                                interpreter, ReturnCode.Error, error);
                        }
                    }
                    else
                    {
                        if (RestoreTitle(ref error))
                        {
                            return true;
                        }
                        else
                        {
                            DebugOps.Complain(
                                interpreter, ReturnCode.Error, error);
                        }
                    }
                }
                else
                {
                    return true; /* BUGFIX: Fake success. */
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Stream Handling
        protected virtual bool IsChannelRedirected(
            ChannelType channelType
            )
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false), channelType, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Stream Handling
        private static Stream GetInputStream(
            IInteractiveHost interactiveHost
            )
        {
            Stream stream = null;
            Result error = null;

            if (GetInputStream(
                    interactiveHost, ref stream,
                    ref error) == ReturnCode.Ok)
            {
                return stream;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetInputStream(
            IInteractiveHost interactiveHost,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                SystemConsoleInputMustBeOpen(interactiveHost); /* throw */

                if (ConsoleOps.GetInputStream(
                        ref stream, ref error) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Stream GetOutputStream(
            IStreamHost streamHost
            )
        {
            Stream stream = null;
            Result error = null;

            if (GetOutputStream(
                    streamHost, ref stream,
                    ref error) == ReturnCode.Ok)
            {
                return stream;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetOutputStream(
            IStreamHost streamHost,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                SystemConsoleOutputMustBeOpen(streamHost); /* throw */

                if (ConsoleOps.GetOutputStream(
                        ref stream, ref error) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Stream GetErrorStream(
            IStreamHost streamHost
            )
        {
            Stream stream = null;
            Result error = null;

            if (GetErrorStream(
                    streamHost, ref stream,
                    ref error) == ReturnCode.Ok)
            {
                return stream;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetErrorStream(
            IStreamHost streamHost,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                SystemConsoleErrorMustBeOpen(streamHost); /* throw */

                if (ConsoleOps.GetErrorStream(
                        ref stream, ref error) == ReturnCode.Ok)
                {
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Stream "Factory" Methods
        private static StreamReader NewStreamReader(
            Stream stream,
            Encoding encoding
            )
        {
            if ((stream != null) && (encoding != null))
                return new StreamReader(stream, encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StreamWriter NewStreamWriter(
            Stream stream,
            Encoding encoding,
            bool autoFlush
            )
        {
            if ((stream != null) && (encoding != null))
            {
                StreamWriter streamWriter =
                    new StreamWriter(stream, encoding);

                streamWriter.AutoFlush = autoFlush;

                return streamWriter;
            }

            return null;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Setup Handling
        #region Console CancelKeyPress Handling
        #region Native Console CancelKeyPress Handling
#if NATIVE && WINDOWS
        private ReturnCode UnhookSystemConsoleControlHandler(
            bool strict,
            ref Result error
            )
        {
            try
            {
                if (!NoCancel)
                    return ConsoleOps.UnhookControlHandler(strict, ref error);
                else
                    return ReturnCode.Ok; // NOTE: Fake success.
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console CancelKeyPress Handling
        protected virtual bool SetupCancelKeyPressHandler(
            bool setup,
            bool force
            )
        {
            try
            {
                SystemConsoleMustBeOpen(false); /* throw */

                //
                // NOTE: Has setting up the script cancellation keypress been
                //       explicitly disabled?
                //
                if (!NoCancel && (force || AppDomainOps.IsCurrentDefault()))
                {
                    if (setup)
                        System.Console.CancelKeyPress +=
                            Interpreter.ConsoleCancelEventHandler;
                    else
                        System.Console.CancelKeyPress -=
                            Interpreter.ConsoleCancelEventHandler;

                    return true; // success.
                }
                else
                {
                    return true; // fake success.
                }
            }
            catch
            {
                return false; // failure.
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Icon Handling
#if NATIVE && WINDOWS && DRAWING
        private bool SetupIcon()
        {
            try
            {
                //
                // NOTE: Has changing the icon been explicitly disabled?
                //
                if (!NoIcon)
                {
                    if (PlatformOps.IsWindowsOperatingSystem())
                    {
                        string packageName = GlobalState.GetPackageName();

                        if (!String.IsNullOrEmpty(packageName))
                        {
                            Stream stream = AssemblyOps.GetResourceStream(
                                GlobalState.GetAssembly(),
                                packageName + FileExtension.Icon);

                            return SetupIcon(true, stream);
                        }
                    }
                    else
                    {
                        return true; /* BUGFIX: Fake success. */
                    }
                }
                else
                {
                    return true; /* BUGFIX: Fake success. */
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetupIcon(
            bool setup,
            Stream stream
            )
        {
            try
            {
                //
                // NOTE: Has changing the icon been explicitly disabled?
                //
                if (!NoIcon && IsOpen())
                {
                    if (PlatformOps.IsWindowsOperatingSystem())
                    {
                        if (setup)
                        {
                            if (stream != null)
                            {
                                lock (syncRoot)
                                {
                                    if (icon != null)
                                    {
                                        icon.Dispose();
                                        icon = null;
                                    }

                                    icon = new Icon(stream);

                                    oldSmallIcon = WindowOps.UnsafeNativeMethods.SendMessage(
                                        GetConsoleWindow(),
                                        WindowOps.UnsafeNativeMethods.WM_GETICON,
                                        new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_SMALL),
                                        IntPtr.Zero);

                                    oldBigIcon = WindowOps.UnsafeNativeMethods.SendMessage(
                                        GetConsoleWindow(),
                                        WindowOps.UnsafeNativeMethods.WM_GETICON,
                                        new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_BIG),
                                        IntPtr.Zero);

                                    WindowOps.UnsafeNativeMethods.SendMessage(
                                        GetConsoleWindow(),
                                        WindowOps.UnsafeNativeMethods.WM_SETICON,
                                        new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_SMALL),
                                        icon.Handle);

                                    WindowOps.UnsafeNativeMethods.SendMessage(
                                        GetConsoleWindow(),
                                        WindowOps.UnsafeNativeMethods.WM_SETICON,
                                        new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_BIG),
                                        icon.Handle);
                                }

                                return true;
                            }
                        }
                        else
                        {
                            lock (syncRoot)
                            {
                                WindowOps.UnsafeNativeMethods.SendMessage(
                                    GetConsoleWindow(),
                                    WindowOps.UnsafeNativeMethods.WM_SETICON,
                                    new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_BIG),
                                    oldBigIcon);

                                oldBigIcon = IntPtr.Zero;

                                WindowOps.UnsafeNativeMethods.SendMessage(
                                    GetConsoleWindow(),
                                    WindowOps.UnsafeNativeMethods.WM_SETICON,
                                    new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_SMALL),
                                    oldSmallIcon);

                                oldSmallIcon = IntPtr.Zero;

                                if (icon != null)
                                {
                                    icon.Dispose();
                                    icon = null;
                                }
                            }

                            return true;
                        }
                    }
                    else
                    {
                        return true; /* NOTE: Fake success. */
                    }
                }
                else
                {
                    return true; /* BUGFIX: Fake success. */
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Mode Handling
#if NATIVE && WINDOWS
        private bool SetupMode(
            bool setup
            )
        {
            if (IsOpen())
            {
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: Disable this flag so that right-click works properly in
                    //       the shell (i.e. it brings up the context menu, just like
                    //       it does by default in cmd.exe).
                    //
                    uint mode = NativeConsole.UnsafeNativeMethods.ENABLE_MOUSE_INPUT;

                    if (PrivateChangeMode(
                            ChannelType.Input, !setup, mode) != ReturnCode.Ok)
                    {
                        return false;
                    }
                }
            }

            return true; // NOTE: Fake success.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Master Setup Methods
        protected virtual bool ShouldSetup(
            int newReferenceCount,
            bool setup,
            bool force
            )
        {
            bool result = false;
            bool isSetup = false; /* TRACE ONLY */
            bool markSetup = false;

            try
            {
                if (!CommonOps.Environment.DoesVariableExist(
                        EnvVars.NoConsoleSetup))
                {
                    isSetup = ConsoleOps.IsSetup(); /* TRACE ONLY */

                    if (setup)
                    {
                        if (force || (newReferenceCount == 1))
                        {
                            markSetup = ConsoleOps.MarkSetup(setup);

                            if (markSetup)
                                result = true;
                        }
                    }
                    else
                    {
                        if (force || (newReferenceCount <= 0))
                        {
                            markSetup = ConsoleOps.MarkSetup(setup);

                            if (markSetup)
                                result = true;
                        }
                    }
                }
                else
                {
                    result = false;
                }

                return result;
            }
            finally
            {
                TraceOps.DebugTrace(String.Format(
                    "ShouldSetup: newReferenceCount = {0}, setup = {1}, " +
                    "force = {2}, isSetup = {3}, markSetup = {4}, result = {5}",
                    newReferenceCount, setup, force, isSetup, markSetup, result),
                    typeof(Console).Name, TracePriority.HostDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool Setup(
            Console host,
            bool setup,
            bool force
            )
        {
            if (setup)
            {
                if (host != null)
                {
                    int newReferenceCount = Interlocked.Increment(
                        ref referenceCount);

                    if (host.ShouldSetup(newReferenceCount, setup, force))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Setup: INITIALIZING, newReferenceCount = {0}, " +
                            "setup = {1}, force = {2}", newReferenceCount, setup,
                            force), typeof(Console).Name, TracePriority.HostDebug);

                        bool result = true;

                        if (!host.SetupTitle(true))
                            result = false;

#if NATIVE && WINDOWS && DRAWING
                        if (!host.SetupIcon())
                            result = false;
#endif

#if NATIVE && WINDOWS
                        if (!host.SetupMode(true))
                            result = false;
#endif

                        if (!host.SetupCancelKeyPressHandler(
                                true, forceNonDefaultAppDomain))
                        {
                            result = false;
                        }

                        return result;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (host != null)
                {
                    int newReferenceCount = Interlocked.Decrement(
                        ref referenceCount);

                    if (host.ShouldSetup(newReferenceCount, setup, force))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Setup: UNINITIALIZING, newReferenceCount = {0}, " +
                            "setup = {1}, force = {2}", newReferenceCount, setup,
                            force), typeof(Console).Name, TracePriority.HostDebug);

                        bool result = true;

                        if (!host.SetupCancelKeyPressHandler(
                                false, forceNonDefaultAppDomain))
                        {
                            result = false;
                        }

#if NATIVE && WINDOWS
                        if (!host.SetupMode(false))
                            result = false;
#endif

#if NATIVE && WINDOWS && DRAWING
                        if (!host.SetupIcon(false, null))
                            result = false;
#endif

                        if (!host.SetupTitle(false))
                            result = false;

                        return result;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Test Mode Handling
        internal void EnableTests(
            bool enable
            )
        {
            hostFlags = GetHostFlags();
            hostTestFlags = GetTestFlags();

            if (enable)
            {
                //
                // NOTE: Enable test mode.
                //
                hostFlags |= HostFlags.Test;

                //
                // NOTE: Enable each of the individual tests.
                //
                hostTestFlags |= HostTestFlags.CustomInfo;
            }
            else
            {
                //
                // NOTE: Disable test mode.
                //
                hostFlags &= ~HostFlags.Test;

                //
                // NOTE: Disable each of the individual tests.
                //
                hostTestFlags &= ~HostTestFlags.CustomInfo;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Read Cancellation Handling
        #region Read Cancellation Properties
        private int cancelReadLevels;
        protected internal virtual int CancelReadLevels
        {
            get
            {
                // CheckDisposed();

                return cancelReadLevels;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Check Read Cancellation
        protected virtual bool WasReadCanceled()
        {
            return Interlocked.CompareExchange(
                ref cancelReadLevels, 0, 0) > 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Reset Read Cancellation
        protected virtual void ResetCancelRead()
        {
            // CheckDisposed();

            //
            // HACK: Assumes that setting a 32-bit integer is atomic.
            //
            cancelReadLevels = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Initiate Read Cancellation
        protected virtual void CancelRead()
        {
            // CheckDisposed();

            Interlocked.Increment(ref cancelReadLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Read / ReadLine Mutators
        protected virtual void GetValueForRead(
            ref string value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void GetValueForRead(
            ref int value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void GetValueForRead(
            ref IClientData value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Obsolete()]
        protected virtual void GetValueForRead(
            ref ConsoleKeyInfo value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = default(ConsoleKeyInfo);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Pending Reads/Writes Handling
        private static int sharedReadLevels;
        protected internal virtual int SharedReadLevels
        {
            get
            {
                // CheckDisposed();

                int localReadLevels = Interlocked.CompareExchange(
                    ref sharedReadLevels, 0, 0);

                return localReadLevels;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int sharedWriteLevels;
        protected internal virtual int SharedWriteLevels
        {
            get
            {
                // CheckDisposed();

                int localWriteLevels = Interlocked.CompareExchange(
                    ref sharedWriteLevels, 0, 0);

                return localWriteLevels;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode CheckActiveReadsAndWrites(
            ref Result error
            )
        {
            // CheckDisposed();

            int localReadLevels = ReadLevels;

            if (localReadLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} local reads pending",
                    localReadLevels);

                return ReturnCode.Error;
            }

            localReadLevels = SharedReadLevels;

            if (localReadLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} shared reads pending",
                    localReadLevels);

                return ReturnCode.Error;
            }

            int localWriteLevels = WriteLevels;

            if (localWriteLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} local writes pending",
                    localWriteLevels);

                return ReturnCode.Error;
            }

            localWriteLevels = SharedWriteLevels;

            if (localWriteLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} shared writes pending",
                    localWriteLevels);

                return ReturnCode.Error;
            }

            if (ConsoleOps.IsShared())
            {
                error = "cannot close console, it may be in use by other application domains";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Open/Close Handling
#if NATIVE && WINDOWS
        private ReturnCode PrivateAttachOrOpen(
            bool attach,
            ref Result error
            )
        {
            ReturnCode code;

            code = NativeConsole.AttachOrOpen(false, attach, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode PrivateCloseStandardInput(
            ref Result error
            )
        {
            ReturnCode code;

            code = NativeConsole.CloseStandardInput(ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode PrivateClose(
            ref Result error
            )
        {
            ReturnCode code;

            code = NativeConsole.Close(ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Size Handling
#if NATIVE && WINDOWS
        private ReturnCode PrivateGetLargestWindowSize(
            ref int width,
            ref int height
            )
        {
            ReturnCode code;
            Result error = null;

            code = NativeConsole.GetLargestWindowSize(
                ref width, ref height, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Mode Handling
#if NATIVE && WINDOWS
        private ReturnCode PrivateGetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            ReturnCode code = NativeConsole.GetMode(
                channelType, ref mode, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode PrivateSetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            ReturnCode code = NativeConsole.SetMode(
                channelType, mode, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ReturnCode PrivateChangeMode(
            ChannelType channelType,
            bool enable,
            uint mode
            )
        {
            ReturnCode code;
            Result error = null;

            code = NativeConsole.ChangeMode(
                ChannelType.Input, enable, mode, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual bool ChangeMode( /* NOT USED? */
            ChannelType channelType,
            bool enable,
            uint mode
            )
        {
            if (IsOpen() &&
                PlatformOps.IsWindowsOperatingSystem() &&
                (PrivateChangeMode(channelType, enable, mode) == ReturnCode.Ok))
            {
                return true;
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Window Handling
#if NATIVE && WINDOWS
        private static IntPtr GetConsoleWindow()
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return NativeConsole.GetConsoleWindow();

            return IntPtr.Zero;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Input Handling
#if NATIVE && WINDOWS
        private ReturnCode PrivateFlushInputBuffer(
            ref Result error
            )
        {
            ReturnCode code;

            code = NativeConsole.FlushInputBuffer(ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console CancelKeyPress Handling
#if NATIVE && WINDOWS
        private ReturnCode PrivateCancel(
            bool force,
            ref Result error
            )
        {
            //
            // NOTE: This general idea behind simulating a Ctrl-C event before
            //       simulating the return key (below) is that it will prevent
            //       any existing text that happens to be on the console from
            //       being evaluated.  Experiments indicate that this method
            //       is not 100% reliable; however, a more reliable method
            //       (that will work properly from any thread) is not known.
            //       That being said, when this call is combined with the new
            //       read cancellation handling (see above), it should be very
            //       reliable.
            //
            ReturnCode code = force ? NativeConsole.SendControlEvent(
                ControlEvent.CTRL_C_EVENT, ref error) : ReturnCode.Ok;

            if (code == ReturnCode.Ok)
            {
                IntPtr handle = GetConsoleWindow();

                if (handle != IntPtr.Zero)
                {
                    //
                    // NOTE: This is an attempt to "nicely" break out of the
                    //       synchronous Console.ReadLine call so that the
                    //       interactive loop can realize any changes in the
                    //       interpreter state (i.e. has the interpreter been
                    //       marked as "exited"?).
                    //
                    code = WindowOps.SimulateReturnKey(handle, ref error);
                }
                else
                {
                    error = "invalid console window";
                    code = ReturnCode.Error;
                }
            }

            //
            // NOTE: If we encountered an error calling the Win32 API, report
            //       that now.
            //
            if (code != ReturnCode.Ok)
                DebugOps.Complain(SafeGetInterpreter(), code, error);

            return code;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Content Section Methods
        protected override bool DoesSupportColor()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportColor();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override bool DoesAdjustColor()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesAdjustColor();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override bool DoesSupportSizing()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportSizing();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override bool DoesSupportPositioning()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportPositioning();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        public override string Title
        {
            set
            {
                CheckDisposed();

                base.Title = value;
                RefreshTitle();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool RefreshTitle()
        {
            CheckDisposed();

            return SetupTitle(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsInputRedirected()
        {
            CheckDisposed();

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            return System.Console.IsInputRedirected;
#else
            try
            {
#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                bool value = System.Console.KeyAvailable; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                //
                // NOTE: If we got this far, input has not been
                //       redirected (i.e. there was no exception
                //       thrown by KeyAvailable).
                //
                return false;
            }
            catch (InvalidOperationException)
            {
                //
                // NOTE: Per MSDN, input is being redirected from
                //       a "file".
                //
                return true;
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsOpen()
        {
            CheckDisposed();

            return SystemConsoleIsOpen(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Pause()
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */
                System.Console.ReadKey(true);

                return true;
            }
            catch (InvalidOperationException)
            {
                SetReadException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Flush()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                int count = 0;

                ///////////////////////////////////////////////////////////////

                try
                {
                    SystemConsoleOutputMustBeOpen(this); /* throw */
                    System.Console.Out.Flush(); /* throw */

                    count++;
                }
                catch (IOException)
                {
                    SetWriteException(true);
                }
                catch
                {
                    // do nothing.
                }

                ///////////////////////////////////////////////////////////////

                try
                {
                    SystemConsoleErrorMustBeOpen(this); /* throw */
                    System.Console.Error.Flush(); /* throw */

                    count++;
                }
                catch (IOException)
                {
                    SetWriteException(true);
                }
                catch
                {
                    // do nothing.
                }

                ///////////////////////////////////////////////////////////////

                return (count > 0);
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support text output, colors, positioning,
                //       sizing, and resizing.
                //
                hostFlags = HostFlags.Resizable | HostFlags.Color |
                            HostFlags.ReversedColor | HostFlags.Text |
                            HostFlags.Sizing | HostFlags.Positioning |
                            HostFlags.QueryState | HostFlags.NoColorNewLine |
                            base.GetHostFlags();

                if ((WindowWidth >= SuperJumboWindowWidth) &&
                    (WindowHeight >= SuperJumboWindowHeight))
                {
                    hostFlags |= HostFlags.SuperJumboSize;
                }
                else if ((WindowWidth >= JumboWindowWidth) &&
                    (WindowHeight >= JumboWindowHeight))
                {
                    hostFlags |= HostFlags.JumboSize;
                }
                else if ((WindowWidth >= SuperFullWindowWidth) &&
                    (WindowHeight >= SuperFullWindowHeight))
                {
                    hostFlags |= HostFlags.SuperFullSize;
                }
                else if ((WindowWidth >= FullWindowWidth) &&
                    (WindowHeight >= FullWindowHeight))
                {
                    hostFlags |= HostFlags.FullSize;
                }
                else if ((WindowWidth >= CompactWindowWidth) &&
                    (WindowHeight >= CompactWindowHeight))
                {
                    hostFlags |= HostFlags.CompactSize;
                }
                else if ((WindowWidth >= MinimumWindowWidth) &&
                    (WindowHeight >= MinimumWindowHeight))
                {
                    hostFlags |= HostFlags.MinimumSize;
                }
                else if (!PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // NOTE: No idea on this platform as Mono does not support
                    //       console window width and height on Unix (?).  Fake
                    //       it.
                    //
                    hostFlags |= HostFlags.CompactSize;
                }
                else
                {
                    //
                    // NOTE: We should not get here.
                    //
                    hostFlags |= HostFlags.ZeroSize;
                }
            }

            //
            // WARNING: Do not use the InTestMode method here, it calls
            //          this method.
            //
            if (FlagOps.HasFlags(hostFlags, HostFlags.Test, true))
                hostFlags |= HostFlags.CustomInfo;
            else
                hostFlags &= ~HostFlags.CustomInfo;

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                value = System.Console.ReadLine();

                GetValueForRead(ref value);

                if (Echo)
                    System.Console.WriteLine(value);

                return true;
            }
            catch (IOException)
            {
                SetReadException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteLine()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */
                System.Console.WriteLine();

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        public override Stream DefaultIn
        {
            get
            {
                CheckDisposed();
                SystemConsoleMustBeOpen(true); /* throw */

                return System.Console.OpenStandardInput();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override Stream DefaultOut
        {
            get
            {
                CheckDisposed();
                SystemConsoleMustBeOpen(true); /* throw */

                return System.Console.OpenStandardOutput();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override Stream DefaultError
        {
            get
            {
                CheckDisposed();
                SystemConsoleMustBeOpen(true); /* throw */

                return System.Console.OpenStandardError();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override Stream In
        {
            get { CheckDisposed(); return GetInputStream(this); }
            set
            {
                CheckDisposed();
                SystemConsoleInputMustBeOpen(this); /* throw */

                System.Console.SetIn(NewStreamReader(
                    value, InputEncoding));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override Stream Out
        {
            get { CheckDisposed(); return GetOutputStream(this); }
            set
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                System.Console.SetOut(NewStreamWriter(
                    value, OutputEncoding, DoesAutoFlushWriter()));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override Stream Error
        {
            get { CheckDisposed(); return GetErrorStream(this); }
            set
            {
                CheckDisposed();
                SystemConsoleErrorMustBeOpen(this); /* throw */

                System.Console.SetError(NewStreamWriter(
                    value, ErrorEncoding, DoesAutoFlushWriter()));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override Encoding InputEncoding
        {
            get
            {
                CheckDisposed();
                SystemConsoleInputMustBeOpen(this); /* throw */

                return System.Console.InputEncoding;
            }
            set
            {
                CheckDisposed();
                SystemConsoleInputMustBeOpen(this); /* throw */

                System.Console.InputEncoding = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override Encoding OutputEncoding
        {
            get
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                return System.Console.OutputEncoding;
            }
            set
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                System.Console.OutputEncoding = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This uses OutputEncoding since there is no ErrorEncoding
        //       property of the System.Console class.
        //
        public override Encoding ErrorEncoding
        {
            get
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                return System.Console.OutputEncoding;
            }
            set
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                System.Console.OutputEncoding = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetIn()
        {
            CheckDisposed();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                Encoding encoding = System.Console.InputEncoding;
                System.Console.InputEncoding = encoding;

#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                TextReader value = System.Console.In; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetOut()
        {
            CheckDisposed();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */

                Encoding encoding = System.Console.OutputEncoding;
                System.Console.OutputEncoding = encoding;

#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                TextWriter value = System.Console.Out; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetError()
        {
            CheckDisposed();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */

                Encoding encoding = System.Console.OutputEncoding;
                System.Console.OutputEncoding = encoding;

#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                TextWriter value = System.Console.Error; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsOutputRedirected()
        {
            CheckDisposed();

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            return System.Console.IsOutputRedirected;
#elif NATIVE && WINDOWS
            return IsChannelRedirected(ChannelType.Output);
#else
            return false; /* NOT YET IMPLEMENTED */
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsErrorRedirected()
        {
            CheckDisposed();

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            return System.Console.IsErrorRedirected;
#elif NATIVE && WINDOWS
            return IsChannelRedirected(ChannelType.Error);
#else
            return false; /* NOT YET IMPLEMENTED */
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public override IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Console(new HostData(
                Name, Group, Description, ClientData, typeof(Console).Name, interpreter,
                ResourceManager, Profile, UseAttach, NoColor, NoTitle, NoIcon, NoProfile,
                NoCancel));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostTestFlags hostTestFlags = HostTestFlags.Invalid;
        public override HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            if (hostTestFlags == HostTestFlags.Invalid)
                hostTestFlags = HostTestFlags.None;

            return hostTestFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Platform Neutral
            //
            // NOTE: Prior to doing anything else, attempt to make sure that any
            //       pending input is discarded by the current calls into Read()
            //       and/or ReadLine(), if any.  This is designed to work on all
            //       supported platforms.
            //
            CancelRead();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Specific
#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                return PrivateCancel(force, ref error);
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    Interpreter interpreter = InternalSafeGetInterpreter(
                        false);

                    if (interpreter != null)
                    {
                        //
                        // NOTE: Stop any further activity in the interpreter.
                        //
                        if (force)
                            interpreter.Exit = true;

                        //
                        // NOTE: Bail out of Console.ReadLine, etc.
                        //
                        return PrivateCloseStandardInput(ref error);
                    }
                    else
                    {
                        error = "invalid interpreter";
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                return ReturnCode.Error;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteDebugLine()
        {
            CheckDisposed();

            //
            // TODO: We have no dedicated place for debug output;
            //       therefore, just forward it as normal output.
            //
            return WriteLine();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            //
            // TODO: We have no dedicated place for debug output;
            //       therefore, just forward it as normal output
            //       [with the correct colors].
            //
            return Write(value, 1, newLine, DebugForegroundColor, DebugBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            //
            // TODO: We have no dedicated place for debug output;
            //       therefore, just forward it as normal output
            //       [with the correct colors].
            //
            return Write(value, newLine, DebugForegroundColor, DebugBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteErrorLine()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */
                System.Console.Error.WriteLine();

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */

                return WriteCore(
                    System.Console.Error.Write, System.Console.Error.WriteLine,
                    value, 1, newLine, ErrorForegroundColor, ErrorBackgroundColor);
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */

                return WriteCore(
                    System.Console.Error.Write, System.Console.Error.WriteLine,
                    value, newLine, ErrorForegroundColor, ErrorBackgroundColor);
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        public override bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

#if TEST
            if (InTestMode() && HasTestFlags(HostTestFlags.CustomInfo, true))
            {
                return _Tests.Default.TestWriteCustomInfo(
                    interpreter, detailFlags, newLine,
                    foregroundColor, backgroundColor);
            }
#endif

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        public override bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        public override bool ResetColors()
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */
                System.Console.ResetColor();

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                foregroundColor = System.Console.ForegroundColor;
                backgroundColor = System.Console.BackgroundColor;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            //
            // NOTE: This is implemented as "reverse video".
            //
            ConsoleColor color = foregroundColor;
            foregroundColor = backgroundColor;
            backgroundColor = color;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                if (foregroundColor != _ConsoleColor.None)
                    System.Console.ForegroundColor = foregroundColor;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                if (backgroundColor != _ConsoleColor.None)
                    System.Console.BackgroundColor = backgroundColor;

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        public override bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                left = System.Console.CursorLeft;
                top = System.Console.CursorTop;

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                if ((left != _Position.Invalid) && (top != _Position.Invalid))
                    System.Console.SetCursorPosition(left, top);
                else if (left != _Position.Invalid)
                    System.Console.CursorLeft = left;
                else if (top != _Position.Invalid)
                    System.Console.CursorTop = top;

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        public override bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            if ((hostSizeType != HostSizeType.Any) &&
                (hostSizeType != HostSizeType.WindowCurrent))
            {
                return false;
            }

            int currentBufferWidth = _Size.Invalid;
            int currentBufferHeight = _Size.Invalid;
            int currentWindowWidth = _Size.Invalid;
            int currentWindowHeight = _Size.Invalid;

            if (!SaveSize(
                    ref currentBufferWidth, ref currentBufferHeight,
                    ref currentWindowWidth, ref currentWindowHeight))
            {
                return false;
            }

            bool result = false;

            try
            {
                //
                // NOTE: Make sure we successfully saved the original buffer
                //       and window sizes earlier.
                //
                lock (syncRoot)
                {
                    if ((savedBufferWidth != _Size.Invalid) &&
                        (savedBufferHeight != _Size.Invalid) &&
                        (savedWindowWidth != _Size.Invalid) &&
                        (savedWindowHeight != _Size.Invalid))
                    {
                        result = SetSize(
                            savedBufferWidth, savedBufferHeight,
                            savedWindowWidth, savedWindowHeight);
                    }
                }
            }
            catch /* NOTE: Superfluous? */
            {
                // do nothing.
            }
            finally
            {
                //
                // NOTE: *FAIL* Restore the previous buffer and window sizes
                //       (i.e. those that were current at the start of this
                //       method).
                //
                if (!result)
                {
                    if ((currentBufferWidth != _Size.Invalid) &&
                        (currentBufferHeight != _Size.Invalid) &&
                        (currentWindowWidth != _Size.Invalid) &&
                        (currentWindowHeight != _Size.Invalid))
                    {
                        /* IGNORED */
                        SetSize(
                            currentBufferWidth, currentBufferHeight,
                            currentWindowWidth, currentWindowHeight);
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            )
        {
            CheckDisposed();

            if ((hostSizeType == HostSizeType.BufferCurrent) ||
                (hostSizeType == HostSizeType.BufferMaximum))
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */

                    width = System.Console.BufferWidth;
                    height = System.Console.BufferHeight;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else if ((hostSizeType == HostSizeType.Any) ||
                (hostSizeType == HostSizeType.WindowCurrent))
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */

                    width = System.Console.WindowWidth;
                    height = System.Console.WindowHeight;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else if (hostSizeType == HostSizeType.WindowMaximum)
            {
#if NATIVE && WINDOWS
                if (PlatformOps.IsWindowsOperatingSystem())
                {
                    ReturnCode code = PrivateGetLargestWindowSize(
                        ref width, ref height);

                    return (code == ReturnCode.Ok);
                }
                else
                {
                    return FallbackGetLargestWindowSize(ref width, ref height);
                }
#else
                return FallbackGetLargestWindowSize(ref width, ref height);
#endif
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool SetSize(
            HostSizeType hostSizeType,
            int width,
            int height
            )
        {
            CheckDisposed();

            if ((hostSizeType == HostSizeType.BufferCurrent) ||
                (hostSizeType == HostSizeType.BufferMaximum))
            {
                //
                // TODO: Figure out a clean way to support this.
                //
                return false;
            }
            else if ((hostSizeType == HostSizeType.Any) ||
                (hostSizeType == HostSizeType.WindowCurrent))
            {
                return SetSize(width, height, false);
            }
            else if (hostSizeType == HostSizeType.WindowMaximum)
            {
                return SetSize(width, height, true);
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        public override bool Read(
            ref int value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                value = System.Console.Read();

                GetValueForRead(ref value);

                if (Echo)
                    System.Console.Write(Convert.ToChar(value));

                return true;
            }
            catch (IOException)
            {
                SetReadException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                value = new ClientData(System.Console.ReadKey(
                    intercept)); /* throw */

                GetValueForRead(ref value);

                return true;
            }
            catch (InvalidOperationException)
            {
                SetReadException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public override bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                value = System.Console.ReadKey(intercept);

                GetValueForRead(ref value);

                return true;
            }
            catch (InvalidOperationException)
            {
                SetReadException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        public override bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */

                if (newLine)
                    System.Console.WriteLine(value);
                else
                    System.Console.Write(value);

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */

                if (newLine)
                    System.Console.WriteLine(value);
                else
                    System.Console.Write(value);

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            StringList result = new StringList();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                result.Add("HeaderFlags", GetHeaderFlags().ToString());
                result.Add("HostFlags", GetHostFlags().ToString());
                result.Add("StaticReadLevels", SharedReadLevels.ToString());
                result.Add("StaticWriteLevels", SharedWriteLevels.ToString());
                result.Add("ReadLevels", ReadLevels.ToString());
                result.Add("WriteLevels", WriteLevels.ToString());
                result.Add("CancelReadLevels", CancelReadLevels.ToString());
                result.Add("IsOpen", SystemConsoleIsOpen(false).ToString());
                result.Add("WindowIsOpen", SystemConsoleIsOpen(true).ToString());
                result.Add("InputIsOpen", SystemConsoleInputIsOpen().ToString());
                result.Add("OutputIsOpen", SystemConsoleOutputIsOpen().ToString());
                result.Add("InputIsRedirected", SystemConsoleInputIsRedirected().ToString());
                result.Add("OutputIsRedirected", SystemConsoleOutputIsRedirected().ToString());
                result.Add("ErrorIsRedirected", SystemConsoleErrorIsRedirected().ToString());

#if NATIVE && WINDOWS
                result.Add("CloseCount", closeCount.ToString());
#endif

                result.Add("ReferenceCount", referenceCount.ToString());
                result.Add("MustBeOpenCount", mustBeOpenCount.ToString());
                result.Add("CertificateCount", certificateCount.ToString());
                result.Add("CertificateSubject", certificateSubject);
                result.Add("SavedTitle", savedTitle);
                result.Add("SavedForegroundColor", savedForegroundColor.ToString());
                result.Add("SavedBackgroundColor", savedBackgroundColor.ToString());
                result.Add("SavedWindowWidth", savedWindowWidth.ToString());
                result.Add("SavedWindowHeight", savedWindowHeight.ToString());
                result.Add("SavedBufferWidth", savedBufferWidth.ToString());
                result.Add("SavedBufferHeight", savedBufferHeight.ToString());

#if NATIVE && WINDOWS && DRAWING
                result.Add("OldBigIcon", oldBigIcon.ToString());
                result.Add("OldSmallIcon", oldSmallIcon.ToString());
#endif

#if NATIVE && WINDOWS
                StringPairList list = new StringPairList();

                NativeConsole.AddInfo(list, detailFlags);

                foreach (IPair<string> item in list)
                {
                    if ((item == null) || (item.X == null) || (item.Y == null))
                        continue;

                    result.Add(item.X, item.Y);
                }
#endif
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(false); /* throw */
                System.Console.Beep(frequency, duration);

                return true;
            }
            catch
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool IsIdle()
        {
            CheckDisposed();

            //
            // STUB: We have no better idle detection.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Clear()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */
                System.Console.Clear(); /* throw */

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetFlags()
        {
            CheckDisposed();

            return PrivateResetFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                return PrivateGetMode(channelType, ref mode, ref error);
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                return PrivateSetMode(channelType, mode, ref error);
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                ReturnCode code = PrivateAttachOrOpen(UseAttach, ref error);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: The call to NativeConsole.Open succeeded,
                    //       decrease the "close" count by one.  Do not
                    //       let the count fall [and stay] below zero.
                    //
                    if (Interlocked.Decrement(ref closeCount) < 0)
                        Interlocked.Increment(ref closeCount);

                    //
                    // NOTE: Now, re-setup our console customizations.
                    //
                    if (!Setup(this, true, true))
                    {
                        error = "failed to re-setup console";
                        code = ReturnCode.Error;
                    }
                }

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                ReturnCode code = CheckActiveReadsAndWrites(ref error);

                if (code == ReturnCode.Ok)
                {
                    if (Setup(this, false, true))
                    {
                        code = UnhookSystemConsoleControlHandler(
                            false, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Prior to actually closing the console,
                            //       prevent other threads from attempting
                            //       to use it by adding a "lock" to the
                            //       close count.  Then, if the call to the
                            //       NativeConsole.Close method succeeds, add
                            //       another "lock" on the close count.
                            //       Finally, remove the outer "lock" prior
                            //       to returning from this method, leaving
                            //       the inner one in place.
                            //
                            Interlocked.Increment(ref closeCount);

                            try
                            {
                                code = PrivateClose(ref error);

                                if (code == ReturnCode.Ok)
                                    Interlocked.Increment(ref closeCount);
                            }
                            finally
                            {
                                Interlocked.Decrement(ref closeCount);
                            }
                        }
                    }
                    else
                    {
                        error = "failed to un-setup console";
                        code = ReturnCode.Error;
                    }
                }

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                if (ConsoleOps.ResetCachedInputRecord(
                        ref error) == ReturnCode.Ok)
                {
                    return PrivateFlushInputBuffer(ref error);
                }
                else
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if ((ConsoleOps.ResetStreams(
                    ChannelType.StandardChannels,
                    ref error) == ReturnCode.Ok) &&
                (base.Reset(ref error) == ReturnCode.Ok))
            {
                if (!PrivateResetFlags()) /* NON-VIRTUAL */
                {
                    error = "failed to reset flags";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(
                    InternalSafeGetInterpreter(false), null))
            {
                throw new InterpreterDisposedException(typeof(Console));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////

                    Setup(this, false, false);
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
