/*
 * Vars.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    [ObjectId("e8dd6520-63db-4d7a-9ab3-f3bbe0f00d82")]
    internal static class Vars
    {
        #region Basic Naming "Constants"
        //
        // NOTE: The name used by the managed Eagle core "package".
        //
        public static readonly string PackageName = GlobalState.GetPackageName();

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

        #region For Core Marshaller Use Only
        //
        // NOTE: Used by the CommandCallback class for temporary storage of
        //       ByRef parameter values.
        //
        public static readonly string VarsPrefix = Prefix + "vars_";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used by [object] to represent a null object.  Do NOT use it
        //       for any other purpose.
        //
        public static readonly string Null = _String.Null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For (Local & Remote) Debugger Use Only
        //
        // NOTE: Used by the debugger subsystem (DebuggerOps, etc).  Do NOT
        //       use it for any other purpose.
        //
        public static readonly string Debugger = Prefix + "debugger"; // Eagle only.
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For File Name Resolution Use Only
        //
        // NOTE: Used by the file name resolution subsystem (PathOps).  This
        //       is used within the PathOps static class to mutate the fully
        //       qualified path of a particular script file.  Do NOT use it
        //       for any other purpose.
        //
        public static readonly string Paths = Prefix + "paths"; // Eagle only.
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "Safe" Interpreter Use Only
        //
        // NOTE: Used by the "safe" interpreter path scrubber.  Do NOT use it
        //       for any other purpose.
        //
        public static readonly string BaseDirectory = "{BaseDirectory}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Test Suite Use Only
        //
        // NOTE: Used by the unit testing functionality.  Do NOT use it for
        //       any other purpose.
        //
        public static readonly string Tests = Prefix + "tests"; // Eagle only.

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is a transient (temporary) variable for use during test
        //       file evaluation only.  Do NOT use it for any other purpose.
        //
        public static readonly string TestFile = "test_file"; // Eagle only.

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is an array variable used by the test suite and its
        //       related procedures to prevent various default actions from
        //       being taken (e.g. constraint checks, warnings, etc).
        //
        public static readonly string No = "no"; // Eagle only.
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Shell Support Use Only
        //
        // NOTE: Reserved for use by the interactive shell (this variable may
        //       -OR- may not actually be defined).  Do NOT use it for any
        //       other purpose.
        //
        public static readonly string Shell = Prefix + "shell"; // Eagle only.

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        //
        // NOTE: Used for the "about" banner.
        //
        public static readonly string PackageDescription =
            "A Tcl {0} compatible interpreter for the Common Language Runtime.";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Also used for the "about" banner.
        //
        public static readonly string OfficialDescription =
            "Core: This is an official build.";

        public static readonly string UnofficialDescription =
            "Core: This is an unofficial build.";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used for the "about" banner.  These phrases are hard-coded
        //       because the wording is a bit different in English when using
        //       the words "trusted" and "untrusted".
        //
        public static readonly string TrustedDescription =
            "Core: This is a trusted build.";

        public static readonly string UntrustedDescription =
            "Core: This is an untrusted build.";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used for the "about" banner.  These phrases are hard-coded
        //       because the wording is a bit different in English when using
        //       the words "stable" and "unstable".
        //
        public static readonly string StableDescription =
            "Core: This is a stable build.";

        public static readonly string UnstableDescription =
            "Core: This is an unstable build.";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used for the "about" banner.
        //
        public static readonly string SafeDescription =
            "Core: Interpreter {0} thinks it is \"{1}\".";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used for the "about" banner.
        //
        public static readonly string SecurityDescription =
            "Core: Interpreter {0} thinks script security is {1}.";

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        //
        // NOTE: Used for the "about" banner.
        //
        public static readonly string IsolatedDescription =
            "Core: Interpreter {0} thinks plugin isolation is {1}.";
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Shell & Version Support
        //
        // NOTE: This is used to show the release as "trusted".  It should
        //       only be used if the primary assembly file has been signed
        //       with an Authenticode (X.509) certificate and the certificate
        //       is trusted on this machine.
        //
        public static readonly string TrustedValue = "trusted";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to mark the release as "genuine"...  :P~
        //
        public static readonly string GenuineValue = "genuine";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to mark builds as official or unofficial
        //       releases.
        //
        public static readonly string OfficialValue = RuntimeOps.IsOfficial() ?
            "official" : "unofficial";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to mark builds as stable or unsable releases.
        //
        public static readonly string StableValue = RuntimeOps.IsStable() ?
            "stable" : "unstable";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "eagle_platform" Array Use Only
        //
        // NOTE: These names are referred to directly from scripts and where
        //       applicable are named identically to their Tcl counterparts,
        //       please do not change.
        //
        [ObjectId("f8ff1ea7-dfce-4b34-8bd0-6bb395759605")]
        internal static class Platform
        {
            //
            // NOTE: The name of the script array that contains the
            //       platform specific information.
            //
            public static readonly string Name = Prefix + "platform";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Script engine version information.
            //
            public static readonly string Administrator = "administrator";
            public static readonly string ApplicationAddressRange = "applicationAddressRange";
            public static readonly string Certificate = "certificate";
            public static readonly string UpdateBaseUri = "updateBaseUri";
            public static readonly string UpdatePathAndQueryName = "updatePathAndQuery";
            public static readonly string DownloadBaseUri = "downloadBaseUri";
            public static readonly string ScriptBaseUri = "scriptBaseUri";
            public static readonly string AuxiliaryBaseUri = "auxiliaryBaseUri";
            public static readonly string CompileOptions = "compileOptions";
            public static readonly string CSharpOptionsName = "csharpOptions";
            public static readonly string StrongName = "strongName";
            public static readonly string StrongNameTag = "strongNameTag";
            public static readonly string Hash = "hash";
            public static readonly string Engine = "engine"; // COMPAT: What engine are we really using
                                                             //         (mirrored from tcl_platform)?
            public static readonly string Epoch = "epoch";
            public static readonly string InterpreterTimeStamp = "interpreterTimeStamp";
            public static readonly string Vendor = "vendor";
            public static readonly string GlobalAssemblyCache = "globalAssemblyCache";
            public static readonly string MinimumDate = "minimumDate";
            public static readonly string MaximumDate = "maximumDate";
            public static readonly string Culture = "culture";
            public static readonly string FrameworkVersion = "frameworkVersion";
            public static readonly string FrameworkExtraVersion = "frameworkExtraVersion";
            public static readonly string ObjectIds = "objectIds";
            public static readonly string OsName = "os";
            public static readonly string Wow64 = "wow64";

#if CAS_POLICY
            public static readonly string PermissionSet = "permissionSet";
#endif

            public static readonly string ProcessorAffinityMasks = "processorAffinityMasks";
            public static readonly string RuntimeName = "runtime";
            public static readonly string ImageRuntimeVersion = "imageRuntimeVersion";
            public static readonly string TargetFramework = "targetFramework";
            public static readonly string RuntimeVersion = "runtimeVersion";
            public static readonly string RuntimeOptions = "runtimeOptions";
            public static readonly string Configuration = "configuration";
            public static readonly string TimeStamp = "timeStamp";
            public static readonly string PatchLevel = "patchLevel";
            public static readonly string Release = "release";
            public static readonly string SourceId = "sourceId";
            public static readonly string SourceTimeStamp = "sourceTimeStamp";
            public static readonly string Tag = "tag";
            public static readonly string Text = "text";
            public static readonly string Uri = "uri";
            public static readonly string PublicKey = "publicKey";
            public static readonly string PublicKeyToken = "publicKeyToken";
            public static readonly string ModuleVersionId = "moduleVersionId";
            public static readonly string Version = "version";
            public static readonly string ShellPatchLevel = "shellPatchLevel";
            public static readonly string ShellVersion = "shellVersion";
            public static readonly string NativeUtility = "nativeUtility";

            ///////////////////////////////////////////////////////////////////

            public static readonly string CSharpOptionsValue = null; /* TODO: Good default? */

            ///////////////////////////////////////////////////////////////////

            public static readonly string UpdateStablePathAndQuerySuffix =
                ".txt{1}?v={0}";

            public static readonly string UpdateStablePathAndQueryValue =
                "stable" + UpdateStablePathAndQuerySuffix;

            public static readonly string UpdateUnstablePathAndQueryValue =
                "latest" + UpdateStablePathAndQuerySuffix;

            ///////////////////////////////////////////////////////////////////

            public static readonly string UpdatePathAndQueryValue =
                RuntimeOps.GetUpdatePathAndQuery(null, RuntimeOps.IsStable(), null);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For [parse options] Sub-Command Use Only
        [ObjectId("3facc96f-c6ae-495e-813d-3b906382377e")]
        internal static class OptionSet
        {
            public static readonly string Value = "value";
            public static readonly string Options = "options";
            public static readonly string NextIndex = "nextIndex";
            public static readonly string EndIndex = "endIndex";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For [sql execute] Sub-Command Use Only
        [ObjectId("5bcb3c76-9061-40fc-b728-9cd2b1c052a2")]
        internal static class ResultSet
        {
            public static readonly string Names = "names";
            public static readonly string Count = "count";
            public static readonly string Rows = "rows";

            public static readonly string Prepare = "prepare";
            public static readonly string Execute = "execute";
            public static readonly string Time = "time";
        }
        #endregion
    }
}
