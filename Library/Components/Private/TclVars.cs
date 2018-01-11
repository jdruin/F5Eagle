/*
 * TclVars.cs --
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

namespace Eagle._Components.Private
{
    [ObjectId("f48e798e-7e3a-4a21-b27b-3d20d30c91bf")]
    internal static class TclVars
    {
        #region Basic Naming "Constants"
        //
        // NOTE: The name used by the native Tcl core "package".
        //
        public static readonly string PackageName = "Tcl";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used to set the variables in the platform array(s), etc.
        //
        public static readonly string PackageNameNoCase = PackageName.ToLower();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The typical prefix used for reserved variable names.
        //
        public static readonly string Prefix = PackageNameNoCase + "_";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version & Patch-Level "Constants"
        //
        // NOTE: This is the version of Tcl we are "emulating".
        //
        public static readonly string VersionValue = "8.4";
        public static readonly string VersionName = Prefix + "version";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the patch level of Tcl we are "emulating".
        //
        public static readonly string PatchLevelValue = "8.4.21";
        public static readonly string PatchLevelName = Prefix + "patchLevel";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is only used when providing the "Tcl" package to
        //       the interpreter.
        //
        public static readonly Version PackageVersion = new Version(
            PatchLevelValue);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Package Management Use Only
        public static readonly string AutoIndex = "auto_index";
        public static readonly string AutoNoLoad = "auto_noload";
        public static readonly string AutoOldPath = "auto_oldpath";
        public static readonly string AutoPath = "auto_path";
        public static readonly string AutoSourcePath = "auto_source_path";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is a transient (temporary) variable only for use when
        //       evaluating package index files.  Do NOT use it for any other
        //       purpose.
        //
        public static readonly string Directory = "dir"; // NOTE: Tcl / Eagle.

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These two variables are used by Tcl only; however, they are
        //       not intended to be used directly by (most?) scripts.
        //
        public static readonly string LibraryPath = Prefix + "libPath";
        public static readonly string PackagePath = Prefix + "pkgPath";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string PackageUnknown =
            "::tcl::tm::UnknownHandler ::tclPkgUnknown";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string BinPath = "bin";
        public static readonly string LibPath = "lib";

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        public static readonly string LibDataPath = "libdata";

        public static readonly string UserLocalPath = "/usr/local";
        public static readonly string UserLibPath = "/usr/lib";
        public static readonly string LinuxGnuSuffix = "-linux-gnu";

        public static readonly string UserLocalLibPath =
            UserLocalPath + "/" + LibPath;

        public static readonly string UserLocalLibDataPath =
            UserLocalPath + "/" + LibDataPath;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Script Library Use Only
        //
        // NOTE: This variable contains the location of the core script
        //       library.  This is used by Tcl and Eagle.
        //
        public static readonly string Library = Prefix + "library";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This variable contains the location of the shell script
        //       library.  This is only used by Eagle.
        //
        public static readonly string ShellLibrary = Prefix + "shellLibrary";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Shell Support Use Only
        //
        // NOTE: These are used by the native Tcl auto-execution mechanism.
        //       They are not used by Eagle.
        //
        public static readonly string AutoExecutables = "auto_execs";
        public static readonly string AutoNoExecute = "auto_noexec";

        ///////////////////////////////////////////////////////////////////////

        public static readonly string Interactive = Prefix + "interactive";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are for the normal interactive prompt (without debug,
        //       without queue).
        //
        public static readonly string Prompt1 = Prefix + "prompt1";
        public static readonly string Prompt2 = Prefix + "prompt2";

        //
        // NOTE: These are for the debug interactive prompt (without queue).
        //       These do not exist in Tcl (Eagle only).
        //
        public static readonly string Prompt3 = Prefix + "prompt3";
        public static readonly string Prompt4 = Prefix + "prompt4";

        //
        // NOTE: These are for the queue interactive prompt (without debug).
        //       These do not exist in Tcl (Eagle only).
        //
        public static readonly string Prompt5 = Prefix + "prompt5";
        public static readonly string Prompt6 = Prefix + "prompt6";

        //
        // NOTE: These are for the debug, queue interactive prompt. These do
        //       not exist in Tcl (Eagle only).
        //
        public static readonly string Prompt7 = Prefix + "prompt7";
        public static readonly string Prompt8 = Prefix + "prompt8";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This does not exist in Tcl (Eagle only).
        //
        public static readonly string InteractiveLoops = Prefix +
            "interactiveLoops";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are supported (and used) by the Tcl and Eagle shells.
        //       The script file, if it exists, will be evaluated after the
        //       interpreter has been fully initialized and the interactive
        //       loop is about to be entered.
        //
        public static readonly string RunCommandsFileName = Prefix +
            "rcFileName";

        public static readonly string RunCommandsFileValue = "~/tclshrc.tcl";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used by native Tcl on Mac OS Classic only.
        //
        public static readonly string RunCommandsResourceName = Prefix +
            "rcRsrcName";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Script Error Handling Use Only
        public static readonly string ErrorCode = "errorCode";
        public static readonly string ErrorInfo = "errorInfo";
        public static readonly string BackgroundError = "bgerror";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Shell Argument Handling Use Only
        public static readonly string ShellArgumentCount = "argc";
        public static readonly string ShellArguments = "argv";
        public static readonly string ShellArgument0 = "argv0";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Expression Processing Use Only
        //
        // NOTE: This variable is used by Tcl and Eagle to set the precision
        //       to be used for double result values in expressions.
        //
        public static readonly string PrecisionName = Prefix + "precision";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default precision value for doubles.
        //
        public static readonly int PrecisionValue = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These strings are recognized as doubles by the Tcl expression
        //       parser.
        //
        public static readonly string Infinity = "Inf";
        public static readonly string NaN = "NaN";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "word.tcl" Use Only
        public static readonly string NonWordCharacters = Prefix +
            "nonwordchars";

        public static readonly string WordCharacters = Prefix + "wordchars";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Namespace Use Only
        public static readonly string NamespaceSeparator = "::";
        public static readonly string GlobalNamespace = NamespaceSeparator;
        public static readonly string GlobalNamespaceName = String.Empty;
        public static readonly string Unknown = GlobalNamespace + "unknown";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is part of an ugly hack to add "tcl::mathfunc::*" and
        //       "tcl::mathop::*" support for [expr] functions and operators
        //       to Eagle, respectively.
        //
        public static readonly string MathFunctionNamespaceName = "tcl::mathfunc";
        public static readonly string MathOperatorNamespaceName = "tcl::mathop";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For [proc] Use Only
        //
        // NOTE: This is used by (and with) the [proc] command to indicate a
        //       procedure that accepts a variable number of arguments (i.e.
        //       it may only be used as the last argument).
        //
        public static readonly string Arguments = "args";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "env" Array Use Only
        //
        // NOTE: This variable can be used to access the environment variables
        //       applicable to the current process.  Do NOT use it for any
        //       other purpose.
        //
        public static readonly string Environment = "env";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Tcl Byte-Code Compiler Use Only
        //
        // NOTE: These are used by (special debugging builds of) Tcl only in
        //       order to emit extra information pertaining to the byte-code
        //       compilation and execution of commands.
        //
        public static readonly string TraceCompile = Prefix + "traceCompile";
        public static readonly string TraceExecute = Prefix + "traceExec";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "tcl_platform" Array Use Only
        //
        // NOTE: These names are referred to directly from scripts and where
        //       applicable are named identically to their Tcl counterparts,
        //       please do not change.
        //
        [ObjectId("17c544ad-f0f2-415c-af5c-ec7cb6bd88f0")]
        internal static class Platform
        {
            //
            // NOTE: The name of the script array that contains the
            //       Tcl compatible platform specific information.
            //
            public static readonly string Name = Prefix + "platform";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The various "well known" elements of the array.
            //
            public static readonly string ByteOrder = "byteOrder";
            public static readonly string CharacterSize = "characterSize"; // NOTE: In format ("min-max"). Not in Tcl.
            public static readonly string Debug = "debug"; // NOTE: ActiveTcl only?
            public static readonly string Version = "version";
            public static readonly string PatchLevel = "patchLevel";
            public static readonly string Engine = "engine"; // COMPAT: What engine are we really using? Not in Tcl.
            public static readonly string Host = "host"; // NOTE: Not in Tcl.
            public static readonly string Machine = "machine";
            public static readonly string OsName = "os";
            public static readonly string OsVersion = "osVersion";
            public static readonly string OsPatchLevel = "osPatchLevel";
            public static readonly string OsServicePack = "osServicePack";
            public static readonly string OsExtra = "osExtra";
            public static readonly string ProcessBits = "processBits"; // NOTE: Not in Tcl (32-bit or 64-bit, etc).
            public static readonly string PlatformName = "platform"; // COMPAT: Tcl.
            public static readonly string PointerSize = "pointerSize";
            public static readonly string Processors = "processors"; // NOTE: Not in Tcl.
            public static readonly string Threaded = "threaded";
            public static readonly string Unicode = "unicode"; // NOTE: Not in Tcl.
            public static readonly string User = "user";
            public static readonly string WordSize = "wordSize";
            public static readonly string DirectorySeparator = "dirSeparator"; // NOTE: Not in Tcl.
            public static readonly string PathSeparator = "pathSeparator"; // NOTE: Not in Tcl, proposed by TIP #315.

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The various "well known" values...
            //
            public static readonly string LittleEndianValue = "littleEndian"; // byteOrder
            public static readonly string BigEndianValue = "bigEndian";       // byteOrder

            ///////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
            public static readonly string UnixValue = "unix";
#endif
        }
        #endregion
    }
}
