/*
 * Core.cs --
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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;

namespace Eagle._Hosts
{
    [ObjectId("0969beae-3d4a-42bf-b514-c7bc18bd6071")]
    public abstract class Core : Default, IDisposable, IGetInterpreter
    {
        #region Private Constants
        private const string NoColorPreferencesSuffix = "NoColor";

        private const string ForegroundColorSuffix = "ForegroundColor";
        private const string BackgroundColorSuffix = "BackgroundColor";

        private const string PropertyNameFormat =
            "SET {{{0}}}";

        private const string PropertyNameAndValueFormat =
            "SET {{{0}}} = {{{1}}}";

        private const string PropertyNameAndErrorFormat =
            "SET {{{0}}} --> {1}";

        private const string PropertyNameValueAndErrorFormat =
            "SET {{{0}}} = {{{2}}} --> {1}";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const string DefaultLibraryResourceBaseName = "library";
        private const string DefaultPackagesResourceBaseName = "packages";
        private const string DefaultApplicationResourceBaseName = "application";

        private const string NotFoundResourceName = "empty";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        protected Core(
            IHostData hostData
            )
            : base(hostData)
        {
            if (hostData != null)
            {
                //
                // NOTE: This must be the name of the class that is deriving
                //       from us.  It is used to construct the file name for
                //       the host profile file.
                //
                typeName = hostData.TypeName;

                //
                // NOTE: Keep track of the interpreter that we are provided,
                //       if any.
                //
                interpreter = hostData.Interpreter;

                //
                // NOTE: Keep the resource manager provided by the custom
                //       IHost implementation, if any.
                //
                resourceManager = hostData.ResourceManager;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Set the default resource base names.
            //
            libraryResourceBaseName = DefaultLibraryResourceBaseName;
            packagesResourceBaseName = DefaultPackagesResourceBaseName;
            applicationResourceBaseName = DefaultApplicationResourceBaseName;

            ///////////////////////////////////////////////////////////////////

            //
            // BUGFIX: In case other host settings are loaded which affect
            //         the rest of the setup process, do this first.
            //
            /* IGNORED */
            LoadHostProfile(interpreter, this, GetType(),
                HostProfileFileEncoding, HostProfileFileName,
                NoProfile, false);

            ///////////////////////////////////////////////////////////////////

            /* IGNORED */
            SetupLibraryResourceManager();

            /* IGNORED */
            SetupPackagesResourceManager();

            /* IGNORED */
            SetupApplicationResourceManager();

            ///////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
            //
            // NOTE: If this host is running in the same application domain
            //       as the parent interpreter, store this instance in the
            //       "isolatedHost" field of the interpreter for later use.
            //
            if (interpreter != null)
            {
                if (!SafeIsIsolated(interpreter))
                {
                    lock (interpreter.SyncRoot)
                    {
                        interpreter.IsolatedHost = this;
                    }
                }
            }
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Support
        protected internal Interpreter InternalSafeGetInterpreter(
            bool trace
            )
        {
            try
            {
                return Interpreter; /* throw */
            }
            catch (Exception e)
            {
                if (trace)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Core).Name,
                        TracePriority.HostError);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected Interpreter SafeGetInterpreter()
        {
            return InternalSafeGetInterpreter(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected Interpreter UnsafeGetInterpreter()
        {
            return Interpreter; /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        protected bool SafeIsIsolated()
        {
            try
            {
                Interpreter localInterpreter = UnsafeGetInterpreter();

                if (localInterpreter == null)
                    return false;

                return SafeIsIsolated(localInterpreter);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected static bool SafeIsIsolated(
            Interpreter interpreter
            )
        {
            try
            {
                if (!AppDomainOps.IsSame(interpreter)) /* throw */
                    return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Plugin Support
        protected static bool SafeHasFlags(
            IPluginData pluginData,
            PluginFlags hasFlags,
            bool all
            )
        {
            if (pluginData == null)
                return false;

            try
            {
                return FlagOps.HasFlags(pluginData.Flags, hasFlags, all);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Prompt Support
        protected virtual string GetPromptVariableName(
            PromptType type,
            PromptFlags flags
            )
        {
            bool debug = FlagOps.HasFlags(flags, PromptFlags.Debug, true);
            bool queue = FlagOps.HasFlags(flags, PromptFlags.Queue, true);

            if (debug)
            {
                if (queue)
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Prompt8 : TclVars.Prompt7;
                }
                else
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Prompt4 : TclVars.Prompt3;
                }
            }
            else
            {
                if (queue)
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Prompt6 : TclVars.Prompt5;
                }
                else
                {
                    return (type == PromptType.Continue) ?
                        TclVars.Prompt2 : TclVars.Prompt1;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Title Support
        protected virtual string BuildCoreTitle(
            string packageName,
            Assembly assembly
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

            string release = FormatOps.ReleaseAttribute(
                SharedAttributeOps.GetAssemblyRelease(assembly));

            bool haveRelease = !String.IsNullOrEmpty(release);

            string text = SharedAttributeOps.GetAssemblyText(
                assembly);

            string configuration = AttributeOps.GetAssemblyConfiguration(
                assembly);

            string[] values = {
                packageName, FormatOps.MajorMinor(
                    AssemblyOps.GetVersion(assembly),
                    Characters.v.ToString(), null),
                haveRelease ? release :
                    SharedAttributeOps.GetAssemblyTag(assembly),
                haveRelease ? null :
                    FormatOps.PackageDateTime(
                        AttributeOps.GetAssemblyDateTime(assembly)),
                FormatOps.AssemblyTextAndConfiguration(text, configuration,
                    Characters.OpenParenthesis.ToString(),
                    Characters.CloseParenthesis.ToString())
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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Script Support
        #region Script Flags & Package Types Support Methods
        private static void ExtractResourceNameScriptFlags(
            ScriptFlags flags,            /* in */
            out bool skipQualified,       /* out */
            out bool skipNonQualified,    /* out */
            out bool skipRelative,        /* out */
            out bool skipRawName,         /* out */
            out bool skipFileName,        /* out */
            out bool skipFileNameOnly,    /* out */
            out bool skipNonFileNameOnly, /* out */
            out bool skipLibraryToLib,    /* out */
            out bool skipTestsToLib,      /* out */
            out bool libraryPackage,      /* out */
            out bool testPackage,         /* out */
            out bool automaticPackage,    /* out */
            out bool preferDeepFileNames  /* out */
            )
        {
            skipQualified = FlagOps.HasFlags(
                flags, ScriptFlags.SkipQualified, true);

            skipNonQualified = FlagOps.HasFlags(
                flags, ScriptFlags.SkipNonQualified, true);

            skipRelative = FlagOps.HasFlags(
                flags, ScriptFlags.SkipRelative, true);

            skipRawName = FlagOps.HasFlags(
                flags, ScriptFlags.SkipRawName, true);

            skipFileName = FlagOps.HasFlags(
                flags, ScriptFlags.SkipFileName, true);

            skipFileNameOnly = FlagOps.HasFlags(
                flags, ScriptFlags.SkipFileNameOnly, true);

            skipNonFileNameOnly = FlagOps.HasFlags(
                flags, ScriptFlags.SkipNonFileNameOnly, true);

            skipLibraryToLib = FlagOps.HasFlags(
                flags, ScriptFlags.SkipLibraryToLib, true);

            skipTestsToLib = FlagOps.HasFlags(
                flags, ScriptFlags.SkipTestsToLib, true);

            libraryPackage = FlagOps.HasFlags(
                flags, ScriptFlags.LibraryPackage, true);

            testPackage = FlagOps.HasFlags(
                flags, ScriptFlags.TestPackage, true);

            automaticPackage = FlagOps.HasFlags(
                flags, ScriptFlags.AutomaticPackage, true);

            preferDeepFileNames = FlagOps.HasFlags(
                flags, ScriptFlags.PreferDeepFileNames, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractResourceNameScriptFlags(
            ScriptFlags flags,               /* in */
            out bool filterOnSuffixMatch,    /* out */
            out bool preferDeepResourceNames /* out */
            )
        {
            filterOnSuffixMatch = FlagOps.HasFlags(
                flags, ScriptFlags.FilterOnSuffixMatch, true);

            preferDeepResourceNames = FlagOps.HasFlags(
                flags, ScriptFlags.PreferDeepResourceNames, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractPluginScriptFlags(
            ScriptFlags flags,             /* in */
            out bool noPluginResourceName, /* out */
            out bool noRawResourceName,    /* out */
            out bool failOnException,      /* out */
            out bool stopOnException,      /* out */
            out bool failOnError,          /* out */
            out bool stopOnError           /* out */
            )
        {
            noPluginResourceName = FlagOps.HasFlags(
                flags, ScriptFlags.NoPluginResourceName, true);

            noRawResourceName = FlagOps.HasFlags(
                flags, ScriptFlags.NoRawResourceName, true);

            failOnException = FlagOps.HasFlags(
                flags, ScriptFlags.FailOnException, true);

            stopOnException = FlagOps.HasFlags(
                flags, ScriptFlags.StopOnException, true);

            failOnError = FlagOps.HasFlags(
                flags, ScriptFlags.FailOnError, true);

            stopOnError = FlagOps.HasFlags(
                flags, ScriptFlags.StopOnError, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractErrorHandlingScriptFlags(
            ScriptFlags flags,        /* in */
            out bool failOnException, /* out */
            out bool stopOnException, /* out */
            out bool failOnError,     /* out */
            out bool stopOnError      /* out */
            )
        {
            failOnException = FlagOps.HasFlags(
                flags, ScriptFlags.FailOnException, true);

            stopOnException = FlagOps.HasFlags(
                flags, ScriptFlags.StopOnException, true);

            failOnError = FlagOps.HasFlags(
                flags, ScriptFlags.FailOnError, true);

            stopOnError = FlagOps.HasFlags(
                flags, ScriptFlags.StopOnError, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractResourceNamePackageTypes(
            PackageType packageType,      /* in */
            out bool haveLibraryPackage,  /* out */
            out bool haveTestPackage,     /* out */
            out bool haveAutomaticPackage /* out */
            )
        {
            haveLibraryPackage = FlagOps.HasFlags(
                packageType, PackageType.Library, true);

            haveTestPackage = FlagOps.HasFlags(
                packageType, PackageType.Test, true);

            haveAutomaticPackage = FlagOps.HasFlags(
                packageType, PackageType.Automatic, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ScriptFlagsToFileSearchFlags(
            ScriptFlags flags,                  /* in */
            out FileSearchFlags fileSearchFlags /* out */
            )
        {
            fileSearchFlags = FileSearchFlags.Default;

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.SpecificPath, true))
            {
                fileSearchFlags |= FileSearchFlags.SpecificPath;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.Mapped, true))
            {
                fileSearchFlags |= FileSearchFlags.Mapped;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.AutoSourcePath, true))
            {
                fileSearchFlags |= FileSearchFlags.AutoSourcePath;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.User, true))
            {
                fileSearchFlags |= FileSearchFlags.User;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.Application, true))
            {
                fileSearchFlags |= FileSearchFlags.Application;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.Vendor, true))
            {
                fileSearchFlags |= FileSearchFlags.Vendor;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.StrictGetFile, true))
            {
                fileSearchFlags |= FileSearchFlags.Strict;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.SearchDirectory, true))
            {
                fileSearchFlags |= FileSearchFlags.DirectoryLocation;
            }

            if (FlagOps.HasFlags(
                    flags, ScriptFlags.SearchFile, true))
            {
                fileSearchFlags |= FileSearchFlags.FileLocation;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tracing Support Methods
        protected virtual void GetScriptTrace(
            Interpreter interpreter, /* in */
            string prefix,           /* in */
            string name,             /* in */
            ScriptFlags flags,       /* in */
            IClientData clientData,  /* in */
            ReturnCode returnCode,   /* in */
            Result result            /* in */
            )
        {
            if (!FlagOps.HasFlags(flags, ScriptFlags.NoTrace, true))
            {
                TraceOps.DebugTrace(String.Format(
                    "GetScript: {0}, interpreter = {1}, name = {2}, " +
                    "flags = {3}, clientData = {4}, returnCode = {5}, " +
                    "result = {6}", prefix,
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name), FormatOps.WrapOrNull(flags),
                    FormatOps.WrapOrNull(clientData), FormatOps.WrapOrNull(
                    returnCode), FormatOps.WrapOrNull(true, true, result)),
                    typeof(Core).Name, TracePriority.GetScriptDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void FilterScriptResourceNamesTrace(
            Interpreter interpreter,           /* in */
            string name,                       /* in */
            IEnumerable<string> resourceNames, /* in */
            ScriptFlags flags,                 /* in */
            string message                     /* in */
            )
        {
            if (!FlagOps.HasFlags(flags, ScriptFlags.NoTrace, true))
            {
                StringList list = (resourceNames != null) ?
                    new StringList(resourceNames) : null;

                TraceOps.DebugTrace(String.Format(
                    "FilterScriptResourceNames: interpreter = {0}, " +
                    "name = {1}, resourceNames = {2}, flags = {3}, {4}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name), FormatOps.WrapOrNull(list),
                    FormatOps.WrapOrNull(flags), (message != null) ?
                    message : FormatOps.DisplayNull),
                    typeof(Core).Name, TracePriority.GetScriptDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void GetUniqueResourceNamesTrace(
            Interpreter interpreter,              /* in */
            string name,                          /* in */
            IEnumerable<string> resourceNames,    /* in */
            StringDictionary uniqueResourceNames, /* in */
            ScriptFlags flags                     /* in */
            )
        {
            if (!FlagOps.HasFlags(flags, ScriptFlags.NoTrace, true))
            {
                StringList list = (resourceNames != null) ?
                    new StringList(resourceNames) : null;

                int[] counts = {
                    Count.Invalid, Count.Invalid, Count.Invalid
                };

                if (list != null)
                    counts[0] = list.Count;

                if (uniqueResourceNames != null)
                    counts[1] = uniqueResourceNames.Count;

                if ((counts[0] != Count.Invalid) &&
                    (counts[1] != Count.Invalid))
                {
                    counts[2] = counts[0] - counts[1];
                }

                TraceOps.DebugTrace(String.Format(
                    "GetUniqueResourceNames: interpreter = {0}, " +
                    "name = {1}, resourceNames = {2}, " +
                    "uniqueResourceNames = {3}, flags = {4}, " +
                    "had {5} names, have {6} names, removed {7} names",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(list),
                    FormatOps.WrapOrNull(uniqueResourceNames),
                    FormatOps.WrapOrNull(flags), counts[0],
                    counts[1], counts[2]),
                    typeof(Core).Name, TracePriority.GetScriptDebug);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Reserved Names Support Methods
        protected virtual IDictionary<string, string> GetReservedScriptNames()
        {
            //
            // NOTE: This data comes from the base class (i.e. the "Default"
            //       host).
            //
            return wellKnownScriptNames;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified script does not contain a "reserved" name.
        //
        protected virtual bool IsReservedScriptName(
            Interpreter interpreter, /* in: NOT USED */
            string name,             /* in */
            ScriptFlags flags,       /* in: NOT USED */
            IClientData clientData   /* in: NOT USED */
            )
        {
            if (name == null)
                return false;

            IDictionary<string, string> dictionary = GetReservedScriptNames();

            if (dictionary == null)
                return false;

            return dictionary.ContainsKey(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified [file] name contains directory information as
        //       well.
        //
        protected virtual bool IsFileNameOnlyScriptName(
            string name /* in */
            )
        {
            return !PathOps.HasDirectory(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified [file] name does not contain an absolute path.
        //
        protected virtual bool IsAbsoluteFileNameScriptName(
            string name,    /* in */
            ref bool exists /* out */
            )
        {
            try
            {
                exists = File.Exists(name); /* throw */

                if (Path.IsPathRooted(name)) /* throw */
                    return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Parameter Customization Support Methods
        //
        // NOTE: If this method returns false in a derived class, it must set
        //       the error message as well.
        //
        protected virtual bool CheckScriptParameters(
            Interpreter interpreter,    /* in: NOT USED */
            ref string name,            /* in, out: NOT USED */
            ref ScriptFlags flags,      /* in, out */
            ref IClientData clientData, /* in, out: NOT USED */
            ref Result error            /* out */
            )
        {
            try
            {
                ScriptFlags newFlags = CoreScriptFlags;

                if (newFlags != ScriptFlags.None)
                    flags |= newFlags;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Allow / Deny Support Methods
        //
        // NOTE: If this method returns false in a derived class, it must set
        //       the error message as well.
        //
        protected virtual bool ShouldAllowScriptParameters(
            Interpreter interpreter,    /* in: NOT USED */
            ref string name,            /* in, out: NOT USED */
            ref ScriptFlags flags,      /* in, out: NOT USED */
            ref IClientData clientData, /* in, out: NOT USED */
            ref Result error            /* out: NOT USED */
            )
        {
            return true; /* STUB */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Resource Name Support Methods
        protected virtual PackageType GetPackageTypeForResourceName(
            Interpreter interpreter, /* in: NOT USED */
            string name,             /* in */
            ScriptFlags flags,       /* in: NOT USED */
            PackageType packageType  /* in */
            )
        {
            packageType &= ~PackageType.Mask;

            if (name != null)
            {
                string unixName = PathOps.GetUnixPath(name);

                if (unixName.IndexOf(ScriptPaths.LibraryPackage,
                        StringOps.SystemStringComparisonType) != Index.Invalid)
                {
                    packageType |= PackageType.Library;
                }

                if (unixName.IndexOf(ScriptPaths.TestPackage,
                        StringOps.SystemStringComparisonType) != Index.Invalid)
                {
                    packageType |= PackageType.Test;
                }
            }

            return packageType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual IEnumerable<string> GetScriptResourceNames(
            Interpreter interpreter, /* in */
            string name,             /* in */
            ScriptFlags flags,       /* in */
            bool verbose             /* in */
            )
        {
            //
            // NOTE: Does the caller wish to skip treating the name as the raw
            //       resource name and/or a file name?  Also, does the caller
            //       wish to skip the qualified and/or non-qualified name?
            //
            bool skipQualified;
            bool skipNonQualified;
            bool skipRelative;
            bool skipRawName;
            bool skipFileName;
            bool skipFileNameOnly;
            bool skipNonFileNameOnly;
            bool skipLibraryToLib;
            bool skipTestsToLib;
            bool libraryPackage;
            bool testPackage;
            bool automaticPackage;
            bool preferDeepFileNames;

            ExtractResourceNameScriptFlags(
                flags, out skipQualified, out skipNonQualified,
                out skipRelative, out skipRawName, out skipFileName,
                out skipFileNameOnly, out skipNonFileNameOnly,
                out skipLibraryToLib, out skipTestsToLib,
                out libraryPackage, out testPackage,
                out automaticPackage, out preferDeepFileNames);

            PackageType packageType = PackageType.None;

            if (libraryPackage)
                packageType |= PackageType.Library;

            if (testPackage)
                packageType |= PackageType.Test;

            if (automaticPackage)
                packageType |= PackageType.Automatic;

            packageType = GetPackageTypeForResourceName(interpreter,
                name, flags, packageType);

            bool haveLibraryPackage;
            bool haveTestPackage;
            bool haveAutomaticPackage;

            ExtractResourceNamePackageTypes(
                packageType, out haveLibraryPackage, out haveTestPackage,
                out haveAutomaticPackage);

            string[] fileNames = {
                null, null, null, null, null, null, null, null
            };

            if ((name != null) &&
                (!skipQualified || !skipRelative) && !skipFileName)
            {
                if (!skipNonFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        fileNames[0] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Library, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[1] = PathOps.MaybeToLib(
                                fileNames[0], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        fileNames[2] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Test, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[3] = PathOps.MaybeToLib(
                                fileNames[2], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }

                if (!skipFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        fileNames[4] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Library, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[5] = PathOps.MaybeToLib(
                                fileNames[4], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        fileNames[6] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Test, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[7] = PathOps.MaybeToLib(
                                fileNames[6], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }
            }

            string baseName = null;

            if ((!skipRawName || !skipFileName) && !skipNonQualified)
                baseName = (name != null) ? Path.GetFileName(name) : null;

            string[] baseFileNames = {
                null, null, null, null, null, null, null, null
            };

            if ((baseName != null) && !skipNonQualified && !skipFileName)
            {
                if (!skipNonFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        baseFileNames[0] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Library, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[1] = PathOps.MaybeToLib(
                                baseFileNames[0], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        baseFileNames[2] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Test, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[3] = PathOps.MaybeToLib(
                                baseFileNames[2], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }

                if (!skipFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        baseFileNames[4] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Library, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[5] = PathOps.MaybeToLib(
                                baseFileNames[4], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        baseFileNames[6] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Test, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[7] = PathOps.MaybeToLib(
                                baseFileNames[6], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }
            }

            PathComparisonType pathComparisonType = preferDeepFileNames ?
                PathComparisonType.DeepestFirst : PathComparisonType.Default;

            //
            // NOTE: Try the following ways to get the script via an embedded
            //       resource name, in order:
            //
            //       1. The provided name verbatim as a resource name, with and
            //          without a file extension.
            //
            //       2. Repeat step #1, treating the provided name as a fully
            //          qualified file name to be converted into a package
            //          relative file name, with and without a file extension.
            //
            //       3. Repeat step #1, treating the provided name as a fully
            //          qualified file name to be converted into a relative
            //          file name, with and without a file extension.
            //
            //       4. There is no step #4.
            //
            return new string[] {
                ///////////////////////////////////////////////////////////////
                // STEP #1
                ///////////////////////////////////////////////////////////////

                !skipQualified && !skipRawName ? name : null,
                !skipQualified && !skipRawName &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(name, skipLibraryToLib,
                            skipTestsToLib, false) : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly ?
                    fileNames[0] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? fileNames[1] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly ?
                    fileNames[2] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? fileNames[3] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly ?
                    fileNames[4] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? fileNames[5] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly ?
                    fileNames[6] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? fileNames[7] : null,

                ///////////////////////////////////////////////////////////////
                // STEP #2
                ///////////////////////////////////////////////////////////////

                !skipRelative && !skipRawName ?
                    PackageOps.GetRelativeFileName(interpreter,
                        name, pathComparisonType, verbose) : null,
                !skipRelative && !skipRawName &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(name, skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[0], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[0], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[1], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[1], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[2], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[2], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[3], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[3], skipLibraryToLib,
                            skipTestsToLib, true) : null,

                ///////////////////////////////////////////////////////////////
                // STEP #3
                ///////////////////////////////////////////////////////////////

                !skipNonQualified && !skipRawName ? baseName : null,
                !skipNonQualified && !skipRawName &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(baseName, skipLibraryToLib,
                            skipTestsToLib, false) : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly ?
                    baseFileNames[0] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[1] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly ?
                    baseFileNames[2] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[3] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly ?
                    baseFileNames[4] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[5] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly ?
                    baseFileNames[6] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[7] : null
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual IEnumerable<string> FilterScriptResourceNames(
            Interpreter interpreter,           /* in */
            string name,                       /* in */
            IEnumerable<string> resourceNames, /* in */
            ScriptFlags flags,                 /* in */
            bool verbose                       /* in */
            )
        {
            if (resourceNames != null)
            {
                bool filterOnSuffixMatch;
                bool preferDeepResourceNames;

                ExtractResourceNameScriptFlags(flags,
                    out filterOnSuffixMatch, out preferDeepResourceNames);

                if (filterOnSuffixMatch || preferDeepResourceNames)
                {
                    if (verbose)
                    {
                        FilterScriptResourceNamesTrace(
                            interpreter, name, resourceNames, flags,
                            "original");
                    }

                    StringList newResourceNames = new StringList();
                    StringBuilder builder = null;

                    if (filterOnSuffixMatch)
                    {
                        StringOps.AppendWithComma("filtered", ref builder);

                        foreach (string resourceName in resourceNames)
                        {
                            if (resourceName == null)
                            {
                                if (verbose)
                                {
                                    FilterScriptResourceNamesTrace(
                                        interpreter, name, null, flags,
                                        "skipped null resource name");
                                }

                                continue;
                            }

                            if (PathOps.MatchSuffix(name, resourceName))
                            {
                                newResourceNames.Add(resourceName);

                                if (verbose)
                                {
                                    FilterScriptResourceNamesTrace(
                                        interpreter, name, null, flags,
                                        String.Format(
                                            "added resource name {0}, " +
                                            "matched suffix {1}",
                                        FormatOps.WrapOrNull(resourceName),
                                        FormatOps.WrapOrNull(name)));
                                }
                            }
                            else
                            {
                                if (verbose)
                                {
                                    FilterScriptResourceNamesTrace(
                                        interpreter, name, null, flags,
                                        String.Format(
                                            "skipped resource name {0}, " +
                                            "mismatched suffix {1}",
                                        FormatOps.WrapOrNull(resourceName),
                                        FormatOps.WrapOrNull(name)));
                                }
                            }
                        }
                    }
                    else
                    {
                        newResourceNames.AddRange(resourceNames);

                        if (verbose)
                        {
                            FilterScriptResourceNamesTrace(
                                interpreter, name, null, flags,
                                "added resource names verbatim");
                        }
                    }

                    if (preferDeepResourceNames)
                    {
                        StringOps.AppendWithComma("sorted", ref builder);

                        newResourceNames.Sort(_Comparers.FileName.Create(
                            PathComparisonType.DeepestFirst));
                    }

                    FilterScriptResourceNamesTrace(
                        interpreter, name, newResourceNames, flags,
                        (builder != null) ? builder.ToString() : null);

                    return newResourceNames.ToArray();
                }
            }

            FilterScriptResourceNamesTrace(
                interpreter, name, resourceNames, flags,
                "verbatim");

            return resourceNames;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual StringDictionary GetUniqueResourceNames(
            Interpreter interpreter,           /* in */
            string name,                       /* in */
            IEnumerable<string> resourceNames, /* in */
            ScriptFlags flags,                 /* in */
            bool verbose                       /* in */
            )
        {
            //
            // NOTE: Create a string dictionary with the resource names so
            //       that we do not search needlessly for duplicates.
            //
            StringDictionary uniqueResourceNames = new StringDictionary();

            if (resourceNames != null)
            {
                foreach (string resourceName in resourceNames)
                {
                    if (resourceName == null)
                        continue;

                    if (!uniqueResourceNames.ContainsKey(resourceName))
                        uniqueResourceNames.Add(resourceName, null);
                }
            }

            GetUniqueResourceNamesTrace(
                interpreter, name, resourceNames, uniqueResourceNames,
                flags);

            return uniqueResourceNames;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void PopulateUniqueResourceNames(
            Interpreter interpreter,                 /* in */
            string name,                             /* in */
            ScriptFlags flags,                       /* in */
            bool verbose,                            /* in */
            ref StringDictionary uniqueResourceNames /* out */
            )
        {
            IEnumerable<string> resourceNames;

            resourceNames = GetScriptResourceNames(
                interpreter, name, flags, verbose);

            resourceNames = FilterScriptResourceNames(
                interpreter, name, resourceNames, flags,
                verbose);

            uniqueResourceNames = GetUniqueResourceNames(
                interpreter, name, resourceNames, flags,
                verbose);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region File System Support Methods
        protected virtual ReturnCode GetScriptViaFileSystem(
            Interpreter interpreter,    /* in */
            string name,                /* in */
            int[] counts,               /* in, out */
            ref ScriptFlags flags,      /* in, out */
            ref IClientData clientData, /* out: NOT USED */
            ref Result result,          /* out */
            ref ResultList errors       /* in, out: NOT USED */
            )
        {
            FileSearchFlags fileSearchFlags;

            ScriptFlagsToFileSearchFlags(flags, out fileSearchFlags);

            int count = 0;

            string value = PathOps.Search(
                interpreter, name, fileSearchFlags, ref count);

            if ((counts != null) && (counts.Length > 0))
                counts[0] += count;

            if (value != null)
            {
                flags |= ScriptFlags.File;
                result = value;

                return ReturnCode.Ok;
            }

            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Plugin Support Methods
        protected virtual EngineFlags GetEngineFlagsForReadScript(
            Interpreter interpreter,
            ScriptFlags flags
            )
        {
            //
            // NOTE: Grab the engine flags as we need them for the calls into
            //       the engine.
            //
            EngineFlags engineFlags = EngineFlags.None;

            if (interpreter != null)
                engineFlags |= interpreter.EngineFlags;

#if XML
            if (FlagOps.HasFlags(flags, ScriptFlags.NoXml, true))
                engineFlags |= EngineFlags.NoXml;
#endif

            if (FlagOps.HasFlags(flags, ScriptFlags.NoPolicy, true))
                engineFlags |= EngineFlags.NoPolicy;

            return engineFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode GetScriptViaPlugin(
            Interpreter interpreter,              /* in */
            string name,                          /* in */
            IPlugin plugin,                       /* in */
            StringDictionary uniqueResourceNames, /* in */
            CultureInfo cultureInfo,              /* in */
            EngineFlags engineFlags,              /* in */
            bool verbose,                         /* in */
            int[] counts,                         /* in, out */
            ref ScriptFlags flags,                /* in, out */
            ref IClientData clientData,           /* out */
            ref Result result,                    /* out */
            ref ResultList errors                 /* in, out */
            )
        {
            //
            // HACK: Skip all invalid and static system (i.e. "core") plugins.
            //       Also, skip plugins that have the "no GetString" flag set.
            //
            if ((plugin == null) ||
                SafeHasFlags(plugin, PluginFlags.System, true) ||
                SafeHasFlags(plugin, PluginFlags.NoGetString, true))
            {
                return ReturnCode.Continue;
            }

            if (uniqueResourceNames == null)
                return ReturnCode.Continue;

            bool noPluginResourceName;
            bool noRawResourceName;
            bool failOnException;
            bool stopOnException;
            bool failOnError;
            bool stopOnError;

            ExtractPluginScriptFlags(flags,
                out noPluginResourceName, out noRawResourceName,
                out failOnException, out stopOnException,
                out failOnError, out stopOnError);

            string pluginName = FormatOps.PluginSimpleName(plugin);

            if ((noPluginResourceName || (pluginName == null)) &&
                noRawResourceName)
            {
                //
                // NOTE: The loop below would do nothing, just skip it and
                //       return now.
                //
                return ReturnCode.Continue;
            }

            foreach (string uniqueResourceName in uniqueResourceNames.Keys)
            {
                string resourceValue = null;

                if (!noPluginResourceName && (pluginName != null))
                {
                    string pluginUniqueResourceName =
                        pluginName + Characters.Period + uniqueResourceName;

                    try
                    {
                        Result error = null;

                        resourceValue = plugin.GetString(
                            interpreter, pluginUniqueResourceName,
                            cultureInfo, ref error);

                        if ((counts != null) && (counts.Length > 1))
                            counts[1]++;

                        if (resourceValue == null)
                        {
                            if (verbose && (error != null))
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(error);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            /* VERBOSE */
                            errors.Add(e);
                        }

                        if (failOnException)
                            return ReturnCode.Error;
                        else if (stopOnException)
                            break;
                    }

                    if (resourceValue != null)
                    {
                        using (StringReader stringReader =
                                new StringReader(resourceValue)) /* throw */
                        {
                            string text = null;
                            Result error = null;

                            if (Engine.ReadScriptStream(
                                    interpreter, name, stringReader,
                                    0, Count.Invalid, ref engineFlags,
                                    ref text, ref error) == ReturnCode.Ok)
                            {
                                IAnyTriplet<IPluginData, string, string> anyTriplet =
                                    new AnyTriplet<IPluginData, string, string>(
                                        plugin, "GetString",
                                        pluginUniqueResourceName);

                                flags |= ScriptFlags.ClientData;
                                clientData = new ClientData(anyTriplet);
                                result = text;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }

                                if (failOnError)
                                    return ReturnCode.Error;
                                else if (stopOnError)
                                    break;
                            }
                        }
                    }
                }

                if (!noRawResourceName)
                {
                    try
                    {
                        Result error = null;

                        resourceValue = plugin.GetString(
                            interpreter, uniqueResourceName,
                            cultureInfo, ref error);

                        if ((counts != null) && (counts.Length > 1))
                            counts[1]++;

                        if (resourceValue == null)
                        {
                            if (verbose && (error != null))
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(error);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            /* VERBOSE */
                            errors.Add(e);
                        }

                        if (failOnException)
                            return ReturnCode.Error;
                        else if (stopOnException)
                            break;
                    }
                }

                if (resourceValue != null)
                {
                    using (StringReader stringReader =
                            new StringReader(resourceValue)) /* throw */
                    {
                        string text = null;
                        Result error = null;

                        if (Engine.ReadScriptStream(
                                interpreter, name, stringReader,
                                0, Count.Invalid, ref engineFlags,
                                ref text, ref error) == ReturnCode.Ok)
                        {
                            IAnyTriplet<IPluginData, string, string> anyTriplet =
                                new AnyTriplet<IPluginData, string, string>(
                                    plugin, "GetString",
                                    uniqueResourceName);

                            flags |= ScriptFlags.ClientData;
                            clientData = new ClientData(anyTriplet);
                            result = text;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            if (verbose && (error != null))
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(error);
                            }

                            if (failOnError)
                                return ReturnCode.Error;
                            else if (stopOnError)
                                break;
                        }
                    }
                }
            }

            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Resource Manager Support Methods
        protected virtual ReturnCode GetScriptViaResourceManager(
            Interpreter interpreter,                                  /* in */
            string name,                                              /* in */
            IAnyPair<string, ResourceManager> resourceManagerAnyPair, /* in */
            StringDictionary uniqueResourceNames,                     /* in */
            EngineFlags engineFlags,                                  /* in */
            bool verbose,                                             /* in */
            bool isolated,                                            /* in */
            int[] counts,                                             /* in, out */
            ref ScriptFlags flags,                                    /* in, out */
            ref IClientData clientData,                               /* out */
            ref Result result,                                        /* out */
            ref ResultList errors                                     /* in, out */
            )
        {
            //
            // HACK: Skip all invalid resource managers.
            //
            if (resourceManagerAnyPair == null)
                return ReturnCode.Continue;

            ResourceManager resourceManager = resourceManagerAnyPair.Y;

            if (resourceManager == null)
                return ReturnCode.Continue;

            if (uniqueResourceNames == null)
                return ReturnCode.Continue;

            bool failOnException;
            bool stopOnException;
            bool failOnError;
            bool stopOnError;

            ExtractErrorHandlingScriptFlags(flags,
                out failOnException, out stopOnException, out failOnError,
                out stopOnError);

            foreach (string uniqueResourceName in uniqueResourceNames.Keys)
            {
                try
                {
                    //
                    // NOTE: Attempt to fetch the script as a resource stream.
                    //
                    Stream resourceStream = resourceManager.GetStream(
                        uniqueResourceName); /* throw */

                    if ((counts != null) && (counts.Length > 2))
                        counts[2]++;

                    //
                    // NOTE: In order to continue, we must have the found the
                    //       resource stream associated with the named resource.
                    //
                    if (resourceStream != null)
                    {
                        using (StreamReader streamReader =
                                new StreamReader(resourceStream)) /* throw */
                        {
                            string text = null;
                            Result error = null;

                            if (Engine.ReadScriptStream(
                                    interpreter, name, streamReader,
                                    0, Count.Invalid, ref engineFlags,
                                    ref text, ref error) == ReturnCode.Ok)
                            {
                                IAnyTriplet<IAnyPair<string, ResourceManager>, string, string> anyTriplet =
                                    new AnyTriplet<IAnyPair<string, ResourceManager>, string, string>(
#if ISOLATED_PLUGINS
                                        !isolated ? resourceManagerAnyPair :
                                            new AnyPair<string, ResourceManager>(
                                                resourceManagerAnyPair.X, null),
#else
                                        resourceManagerAnyPair,
#endif
                                        "GetStream", uniqueResourceName);

                                flags |= ScriptFlags.ClientData;
                                clientData = new ClientData(anyTriplet);
                                result = text;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }

                                if (failOnError)
                                    return ReturnCode.Error;
                                else if (stopOnError)
                                    break;
                            }
                        }
                    }
                }
                catch (MissingManifestResourceException) /* EXPECTED */
                {
                    // do nothing.
                }
                catch (InvalidOperationException) /* EXPECTED */
                {
                    //
                    // NOTE: If we get to this point, it means that
                    //       the resource does exist; however, it
                    //       cannot be accessed via stream.  Attempt
                    //       to fetch the script as a resource string.
                    //
                    string resourceValue = null;

                    try
                    {
                        resourceValue = resourceManager.GetString(
                            uniqueResourceName); /* throw */

                        if ((counts != null) && (counts.Length > 2))
                            counts[2]++;
                    }
                    catch (MissingManifestResourceException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (InvalidOperationException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (Exception e)
                    {
                        if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            /* VERBOSE */
                            errors.Add(e);
                        }

                        if (failOnException)
                            return ReturnCode.Error;
                        else if (stopOnException)
                            break;
                    }

                    //
                    // NOTE: In order to continue, we must have
                    //       the found the resource stream
                    //       associated with the named resource.
                    //
                    if (resourceValue != null)
                    {
                        using (StringReader stringReader =
                                new StringReader(resourceValue)) /* throw */
                        {
                            string text = null;
                            Result error = null;

                            if (Engine.ReadScriptStream(
                                    interpreter, name, stringReader,
                                    0, Count.Invalid, ref engineFlags,
                                    ref text, ref error) == ReturnCode.Ok)
                            {
                                IAnyTriplet<IAnyPair<string, ResourceManager>, string, string> anyTriplet =
                                    new AnyTriplet<IAnyPair<string, ResourceManager>, string, string>(
#if ISOLATED_PLUGINS
                                        !isolated ? resourceManagerAnyPair :
                                            new AnyPair<string, ResourceManager>(
                                                resourceManagerAnyPair.X, null),
#else
                                        resourceManagerAnyPair,
#endif
                                        "GetString", uniqueResourceName);

                                flags |= ScriptFlags.ClientData;
                                clientData = new ClientData(anyTriplet);
                                result = text;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }

                                if (failOnError)
                                    return ReturnCode.Error;
                                else if (stopOnError)
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (verbose)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        /* VERBOSE */
                        errors.Add(e);
                    }

                    if (failOnException)
                        return ReturnCode.Error;
                    else if (stopOnException)
                        break;
                }
            }

            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Assembly Manifest Support Methods
        protected virtual ReturnCode GetScriptViaAssemblyManifest(
            Interpreter interpreter,                                  /* in */
            string name,                                              /* in */
            Assembly assembly,                                        /* in */
            StringDictionary uniqueResourceNames,                     /* in */
            EngineFlags engineFlags,                                  /* in */
            bool verbose,                                             /* in */
            bool isolated,                                            /* in */
            int[] counts,                                             /* in, out */
            ref ScriptFlags flags,                                    /* in, out */
            ref IClientData clientData,                               /* out */
            ref Result result,                                        /* out */
            ref ResultList errors                                     /* in, out */
            )
        {
            //
            // HACK: Skip all invalid assemblies.
            //
            if (assembly == null)
                return ReturnCode.Continue;

            if (uniqueResourceNames == null)
                return ReturnCode.Continue;

            bool failOnException;
            bool stopOnException;
            bool failOnError;
            bool stopOnError;

            ExtractErrorHandlingScriptFlags(flags,
                out failOnException, out stopOnException, out failOnError,
                out stopOnError);

            foreach (string uniqueResourceName in uniqueResourceNames.Keys)
            {
                try
                {
                    //
                    // NOTE: Attempt to fetch the script as a resource stream.
                    //
                    Stream resourceStream = assembly.GetManifestResourceStream(
                        uniqueResourceName);

                    if ((counts != null) && (counts.Length > 3))
                        counts[3]++;

                    //
                    // NOTE: In order to continue, we must have the found the
                    //       resource stream associated with the named resource.
                    //
                    if (resourceStream != null)
                    {
                        using (StreamReader streamReader =
                                new StreamReader(resourceStream)) /* throw */
                        {
                            string text = null;
                            Result error = null;

                            if (Engine.ReadScriptStream(
                                    interpreter, name, streamReader,
                                    0, Count.Invalid, ref engineFlags,
                                    ref text, ref error) == ReturnCode.Ok)
                            {
                                IAnyTriplet<Assembly, string, string> anyTriplet =
                                    new AnyTriplet<Assembly, string, string>(
#if ISOLATED_PLUGINS
                                        !isolated ? assembly : null,
#else
                                        assembly,
#endif
                                        "GetStream", uniqueResourceName);

                                flags |= ScriptFlags.ClientData;
                                clientData = new ClientData(anyTriplet);
                                result = text;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }

                                if (failOnError)
                                    return ReturnCode.Error;
                                else if (stopOnError)
                                    break;
                            }
                        }
                    }
                }
                catch (MissingManifestResourceException) /* EXPECTED */
                {
                    // do nothing.
                }
                catch (InvalidOperationException) /* EXPECTED */
                {
                    //
                    // NOTE: If we get to this point, it means that
                    //       the resource does exist; however, it
                    //       cannot be accessed via stream.  Attempt
                    //       to fetch the script as a resource string.
                    //
                    string resourceValue = null;

                    try
                    {
                        resourceValue = resourceManager.GetString(
                            uniqueResourceName); /* throw */

                        if ((counts != null) && (counts.Length > 3))
                            counts[3]++;
                    }
                    catch (MissingManifestResourceException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (InvalidOperationException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (Exception e)
                    {
                        if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            /* VERBOSE */
                            errors.Add(e);
                        }

                        if (failOnException)
                            return ReturnCode.Error;
                        else if (stopOnException)
                            break;
                    }

                    //
                    // NOTE: In order to continue, we must have
                    //       the found the resource stream
                    //       associated with the named resource.
                    //
                    if (resourceValue != null)
                    {
                        using (StringReader stringReader =
                                new StringReader(resourceValue)) /* throw */
                        {
                            string text = null;
                            Result error = null;

                            if (Engine.ReadScriptStream(
                                    interpreter, name, stringReader,
                                    0, Count.Invalid, ref engineFlags,
                                    ref text, ref error) == ReturnCode.Ok)
                            {
                                IAnyTriplet<Assembly, string, string> anyTriplet =
                                    new AnyTriplet<Assembly, string, string>(
#if ISOLATED_PLUGINS
                                        !isolated ? assembly : null,
#else
                                        assembly,
#endif
                                        "GetString", uniqueResourceName);

                                flags |= ScriptFlags.ClientData;
                                clientData = new ClientData(anyTriplet);
                                result = text;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }

                                if (failOnError)
                                    return ReturnCode.Error;
                                else if (stopOnError)
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (verbose)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        /* VERBOSE */
                        errors.Add(e);
                    }

                    if (failOnException)
                        return ReturnCode.Error;
                    else if (stopOnException)
                        break;
                }
            }

            return ReturnCode.Continue;
        }
        #endregion
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
        protected virtual void EnterReadLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref readLevels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void ExitReadLevel()
        {
            // CheckDisposed();

            Interlocked.Decrement(ref readLevels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void EnterWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref writeLevels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void ExitWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Decrement(ref writeLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Profile Support
        protected internal virtual Encoding HostProfileFileEncoding
        {
            get
            {
                return null; /* NOTE: Use default. */
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual string HostProfileFileName
        {
            get
            {
                string packageName = GlobalState.GetPackageName();

                if (!String.IsNullOrEmpty(packageName))
                {
                    string profile = Profile; // NOTE: Grab property.

                    try
                    {
                        return PathOps.Search(
                            UnsafeGetInterpreter(), packageName + typeName +
                            (!String.IsNullOrEmpty(profile) ? profile : String.Empty) +
                            (NoColor ? NoColorPreferencesSuffix : String.Empty) +
                            FileExtension.Profile, FileSearchFlags.Standard);
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(Core).Name,
                            TracePriority.HostError);
                    }
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WritePropertyNameError(
            IDebugHost debugHost,
            string name,
            object value,
            Result error
            )
        {
            try
            {
                if (debugHost != null)
                {
                    return debugHost.WriteResult(
                        ReturnCode.Error, String.Format(
                        (value != null) ?
                            PropertyNameValueAndErrorFormat :
                            PropertyNameAndErrorFormat,
                        name, error, value), true);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WritePropertyNameException(
            IDebugHost debugHost,
            string name,
            object value,
            Exception exception
            )
        {
            try
            {
                if (debugHost != null)
                {
                    return debugHost.WriteResult(
                        ReturnCode.Error, String.Format(
                        (value != null) ?
                            PropertyNameValueAndErrorFormat :
                            PropertyNameAndErrorFormat,
                        name, exception, value), true);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WritePropertyError(
            IDebugHost debugHost,
            PropertyInfo propertyInfo,
            object value,
            Result error
            )
        {
            return WritePropertyNameError(
                debugHost, (propertyInfo != null) ?
                    propertyInfo.Name : String.Empty,
                value, error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WritePropertyException(
            IDebugHost debugHost,
            PropertyInfo propertyInfo,
            object value,
            Exception exception
            )
        {
            return WritePropertyNameException(
                debugHost, (propertyInfo != null) ?
                    propertyInfo.Name : String.Empty,
                value, exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool WritePropertyNameAndValue(
            IInteractiveHost interactiveHost,
            PropertyInfo propertyInfo,
            object value
            )
        {
            try
            {
                if (interactiveHost != null)
                {
                    return interactiveHost.WriteLine(String.Format(
                        (value != null) ?
                            PropertyNameAndValueFormat :
                            PropertyNameFormat,
                        (propertyInfo != null) ?
                            propertyInfo.Name : String.Empty,
                        value));
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool LoadHostProfile(
            Interpreter interpreter,
            IDebugHost debugHost,
            Type type,
            Encoding encoding,
            string fileName,
            bool noProfile,
            bool verbose
            )
        {
            Result error = null;

            return LoadHostProfile(
                interpreter, debugHost, type, encoding, fileName,
                noProfile, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static bool LoadHostProfile(
            Interpreter interpreter,
            IDebugHost debugHost,
            Type type,
            Encoding encoding,
            string fileName,
            bool noProfile, /* NOTE: Yes, this seems dumb. */
            bool verbose,
            ref Result error
            )
        {
            //
            // NOTE: Has loading the host profile been explicitly disabled?
            //
            if (!noProfile)
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return false;
                }

                if (debugHost == null)
                {
                    error = "interpreter host not available";
                    return false;
                }

                if (type == null)
                {
                    error = "invalid type";
                    return false;
                }

                if (String.IsNullOrEmpty(fileName))
                {
                    error = "invalid file name";
                    return false;
                }

                if (!File.Exists(fileName))
                {
                    error = String.Format(
                        "couldn't read file \"{0}\": no such file or directory",
                        fileName);

                    return false;
                }

                CultureInfo cultureInfo = interpreter.CultureInfo;
                BindingFlags bindingFlags = HostPropertyBindingFlags;

                //
                // NOTE: The encoding used here cannot be null; therefore,
                //       reset it to the default encoding associated with
                //       this method.
                //
                if (encoding == null)
                    encoding = StringOps.GetEncoding(EncodingType.Default);

                foreach (string line in File.ReadAllLines(fileName, encoding))
                {
                    string trimLine = line.Trim();

                    if (!String.IsNullOrEmpty(trimLine))
                    {
                        if ((trimLine[0] != Characters.Comment) &&
                            (trimLine[0] != Characters.AltComment))
                        {
                            int index = trimLine.IndexOf(Characters.EqualSign);

                            if (index != Index.Invalid)
                            {
                                if ((index > 0) && ((index + 1) < trimLine.Length))
                                {
                                    string name = trimLine.Substring(0, index).Trim();
                                    string value = trimLine.Substring(index + 1).Trim();

                                    //
                                    // NOTE: We already know it is not null.
                                    //
                                    if (name.Length > 0)
                                    {
                                        PropertyInfo propertyInfo;

                                        try
                                        {
                                            propertyInfo = type.GetProperty(
                                                name, bindingFlags);
                                        }
                                        catch (Exception e)
                                        {
                                            if (verbose)
                                                WritePropertyNameException(
                                                    debugHost, name, null, e);

                                            //
                                            // NOTE: Skip further processing of this
                                            //       property line because we cannot
                                            //       look it up.
                                            //
                                            continue;
                                        }

                                        //
                                        // NOTE: Verify that the name is a valid
                                        //       ConsoleColor field.
                                        //
                                        if (propertyInfo != null)
                                        {
                                            Result localError = null;

                                            if (propertyInfo.PropertyType == typeof(ConsoleColor))
                                            {
                                                ResultList errors = new ResultList();

                                                //
                                                // NOTE: Verify that the value is a valid
                                                //       ConsoleColor Enum value.
                                                //
                                                object enumValue = EnumOps.TryParseEnum(
                                                    typeof(ConsoleColor), value, true,
                                                    true, ref localError);

                                                //
                                                // HACK: Support our custom color values (i.e. from
                                                //       the HostColor enumeration).  Currently, the
                                                //       only extra color we support is "None" and/or
                                                //       "Invalid", which both mean "do not change the
                                                //       color".
                                                //
                                                if (enumValue == null)
                                                {
                                                    errors.Add(localError);

                                                    enumValue = EnumOps.TryParseEnum(
                                                        typeof(HostColor), value, true,
                                                        true, ref localError);

                                                    if (enumValue is HostColor)
                                                        enumValue = (ConsoleColor)enumValue;
                                                    else
                                                        errors.Add(localError);
                                                }

                                                if (enumValue is ConsoleColor)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the ConsoleColor
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, enumValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, enumValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, enumValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, enumValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    localError = errors;

                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(OutputStyle))
                                            {
                                                string oldValue = null;

                                                try
                                                {
                                                    if (propertyInfo.CanRead)
                                                    {
                                                        OutputStyle outputStyle = (OutputStyle)
                                                            propertyInfo.GetValue(debugHost, null);

                                                        oldValue = outputStyle.ToString();
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    if (verbose)
                                                        WritePropertyException(
                                                            debugHost, propertyInfo, null, e);
                                                }

                                                //
                                                // NOTE: Verify that the value is a valid
                                                //       OutputStyle Enum value.
                                                //
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(OutputStyle), oldValue,
                                                    value, interpreter.CultureInfo, true, true,
                                                    true, ref localError);

                                                if (enumValue is OutputStyle)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the OutputStyle
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, enumValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, enumValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, enumValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, enumValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(HostStreamFlags))
                                            {
                                                string oldValue = null;

                                                try
                                                {
                                                    if (propertyInfo.CanRead)
                                                    {
                                                        HostStreamFlags hostStreamFlags = (HostStreamFlags)
                                                            propertyInfo.GetValue(debugHost, null);

                                                        oldValue = hostStreamFlags.ToString();
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    if (verbose)
                                                        WritePropertyException(
                                                            debugHost, propertyInfo, null, e);
                                                }

                                                //
                                                // NOTE: Verify that the value is a valid
                                                //       HostStreamFlags Enum value.
                                                //
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(HostStreamFlags), oldValue,
                                                    value, interpreter.CultureInfo, true, true,
                                                    true, ref localError);

                                                if (enumValue is HostStreamFlags)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the HostStreamFlags
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, enumValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, enumValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, enumValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, enumValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(HeaderFlags))
                                            {
                                                string oldValue = null;

                                                try
                                                {
                                                    if (propertyInfo.CanRead)
                                                    {
                                                        HeaderFlags headerFlags = (HeaderFlags)
                                                            propertyInfo.GetValue(debugHost, null);

                                                        oldValue = headerFlags.ToString();
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    if (verbose)
                                                        WritePropertyException(
                                                            debugHost, propertyInfo, null, e);
                                                }

                                                //
                                                // NOTE: Verify that the value is a valid
                                                //       HeaderFlags Enum value.
                                                //
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(HeaderFlags), oldValue,
                                                    value, interpreter.CultureInfo, true, true,
                                                    true, ref localError);

                                                if (enumValue is HeaderFlags)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the HeaderFlags
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, enumValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, enumValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, enumValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, enumValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(IdentifierKind))
                                            {
                                                string oldValue = null;

                                                try
                                                {
                                                    if (propertyInfo.CanRead)
                                                    {
                                                        IdentifierKind IdentifierKind = (IdentifierKind)
                                                            propertyInfo.GetValue(debugHost, null);

                                                        oldValue = IdentifierKind.ToString();
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    if (verbose)
                                                        WritePropertyException(
                                                            debugHost, propertyInfo, null, e);
                                                }

                                                //
                                                // NOTE: Verify that the value is a valid
                                                //       IdentifierKind Enum value.
                                                //
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(IdentifierKind), oldValue,
                                                    value, interpreter.CultureInfo, true, true,
                                                    true, ref localError);

                                                if (enumValue is IdentifierKind)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the IdentifierKind
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, enumValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, enumValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, enumValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, enumValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(bool))
                                            {
                                                bool boolValue = false;

                                                if (Value.GetBoolean2(
                                                        value, ValueFlags.AnyBoolean,
                                                        cultureInfo, ref boolValue,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the bool
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, boolValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, boolValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, boolValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, boolValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(int))
                                            {
                                                int intValue = 0;

                                                if (Value.GetInteger2(
                                                        value, ValueFlags.AnyInteger,
                                                        cultureInfo, ref intValue,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the int
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, intValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, intValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, intValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, intValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(char))
                                            {
                                                //
                                                // HACK: Always grab the first character of the
                                                //       value string.
                                                //
                                                char charValue = !String.IsNullOrEmpty(value) ?
                                                    value[0] : Characters.Null;

                                                try
                                                {
                                                    //
                                                    // NOTE: Try to set the character
                                                    //       property.
                                                    //
                                                    if (propertyInfo.CanWrite)
                                                    {
                                                        propertyInfo.SetValue(
                                                            debugHost, charValue, null);

                                                        if (verbose)
                                                            WritePropertyNameAndValue(
                                                                debugHost, propertyInfo, charValue);
                                                    }
                                                    else
                                                    {
                                                        WritePropertyError(
                                                            debugHost, propertyInfo, charValue,
                                                            "property cannot be written");
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    if (verbose)
                                                        WritePropertyException(
                                                            debugHost, propertyInfo, charValue, e);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(string))
                                            {
                                                try
                                                {
                                                    //
                                                    // NOTE: Try to set the string
                                                    //       property.
                                                    //
                                                    if (propertyInfo.CanWrite)
                                                    {
                                                        propertyInfo.SetValue(
                                                            debugHost, value, null);

                                                        if (verbose)
                                                            WritePropertyNameAndValue(
                                                                debugHost, propertyInfo, value);
                                                    }
                                                    else
                                                    {
                                                        WritePropertyError(
                                                            debugHost, propertyInfo, value,
                                                            "property cannot be written");
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    if (verbose)
                                                        WritePropertyException(
                                                            debugHost, propertyInfo, value, e);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(Guid))
                                            {
                                                Guid guidValue = Guid.Empty;

                                                if (Value.GetGuid(
                                                        value, cultureInfo, ref guidValue,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the GUID
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, guidValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, guidValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, guidValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, guidValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(StringList))
                                            {
                                                StringList listValue = null;

                                                //
                                                // WARNING: Cannot cache list representation
                                                //          here, the list may be modified via
                                                //          the public property in the future.
                                                //
                                                if (Parser.SplitList(
                                                        interpreter, value, 0, Length.Invalid,
                                                        false, ref listValue,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the StringList
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, listValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, listValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, listValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, listValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                            else if (propertyInfo.PropertyType == typeof(Encoding))
                                            {
                                                Encoding encodingValue = null;

                                                if (interpreter.GetEncoding(
                                                        value, LookupFlags.Default,
                                                        ref encodingValue,
                                                        ref localError) == ReturnCode.Ok)
                                                {
                                                    try
                                                    {
                                                        //
                                                        // NOTE: Try to set the Encoding
                                                        //       property.
                                                        //
                                                        if (propertyInfo.CanWrite)
                                                        {
                                                            propertyInfo.SetValue(
                                                                debugHost, encodingValue, null);

                                                            if (verbose)
                                                                WritePropertyNameAndValue(
                                                                    debugHost, propertyInfo, encodingValue);
                                                        }
                                                        else
                                                        {
                                                            WritePropertyError(
                                                                debugHost, propertyInfo, encodingValue,
                                                                "property cannot be written");
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (verbose)
                                                            WritePropertyException(
                                                                debugHost, propertyInfo, encodingValue, e);
                                                    }
                                                }
                                                else if (verbose)
                                                {
                                                    WritePropertyError(
                                                        debugHost, propertyInfo, value, localError);
                                                }
                                            }
                                        }
                                        else if (verbose)
                                        {
                                            WritePropertyNameError(
                                                debugHost, name, null, "invalid host property name");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return true; /* NOTE: Fake success. */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public virtual Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Properties
        private string typeName;
        protected internal virtual string TypeName
        {
            get { return typeName; }
            internal set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string libraryResourceBaseName;
        protected internal virtual string LibraryResourceBaseName
        {
            get { return libraryResourceBaseName; }
            set { libraryResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager libraryResourceManager;
        protected internal virtual ResourceManager LibraryResourceManager
        {
            get { return libraryResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string packagesResourceBaseName;
        protected internal virtual string PackagesResourceBaseName
        {
            get { return packagesResourceBaseName; }
            set { packagesResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager packagesResourceManager;
        protected internal virtual ResourceManager PackagesResourceManager
        {
            get { return packagesResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string applicationResourceBaseName;
        protected internal virtual string ApplicationResourceBaseName
        {
            get { return applicationResourceBaseName; }
            set { applicationResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager applicationResourceManager;
        protected internal virtual ResourceManager ApplicationResourceManager
        {
            get { return applicationResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager resourceManager;
        protected internal virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ScriptFlags coreScriptFlags;
        protected internal virtual ScriptFlags CoreScriptFlags
        {
            get { return coreScriptFlags; }
            set { coreScriptFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Script Resource Support
        private bool SetupLibraryResourceManager()
        {
            try
            {
                //
                // NOTE: Create a resource manager for the embedded core script
                //       library, if any.
                //
                libraryResourceManager = new ResourceManager(
                    LibraryResourceBaseName, GlobalState.GetAssembly());

                //
                // NOTE: Now, since creating it will pretty much always succeed,
                //       we need to test it to make sure it is really available.
                //
                /* IGNORED */
                libraryResourceManager.GetString(
                    NotFoundResourceName); /* throw */

                //
                // NOTE: If we get this far, the resource manager is created and
                //       functional.
                //
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);

                //
                // NOTE: The resource manager we created does not appear to work,
                //       null it out so that it will not be used later.
                //
                if (libraryResourceManager != null)
                    libraryResourceManager = null;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetupPackagesResourceManager()
        {
            try
            {
                //
                // NOTE: Create a resource manager for the embedded core script
                //       packages, if any.
                //
                packagesResourceManager = new ResourceManager(
                    PackagesResourceBaseName, GlobalState.GetAssembly());

                //
                // NOTE: Now, since creating it will pretty much always succeed,
                //       we need to test it to make sure it is really available.
                //
                /* IGNORED */
                packagesResourceManager.GetString(
                    NotFoundResourceName); /* throw */

                //
                // NOTE: If we get this far, the resource manager is created and
                //       functional.
                //
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);

                //
                // NOTE: The resource manager we created does not appear to work,
                //       null it out so that it will not be used later.
                //
                if (packagesResourceManager != null)
                    packagesResourceManager = null;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetupApplicationResourceManager()
        {
            try
            {
                //
                // NOTE: Create a resource manager for the embedded application
                //       scripts, if any.
                //
                applicationResourceManager = new ResourceManager(
                    ApplicationResourceBaseName, GlobalState.GetAssembly());

                //
                // NOTE: Now, since creating it will pretty much always succeed,
                //       we need to test it to make sure it is really available.
                //
                /* IGNORED */
                applicationResourceManager.GetString(
                    NotFoundResourceName); /* throw */

                //
                // NOTE: If we get this far, the resource manager is created and
                //       functional.
                //
                return true;
            }
#if DEBUG && VERBOSE
            catch (Exception e)
#else
            catch
#endif
            {
#if DEBUG && VERBOSE
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);
#endif

                //
                // NOTE: The resource manager we created does not appear to work,
                //       null it out so that it will not be used later.
                //
                if (applicationResourceManager != null)
                    applicationResourceManager = null;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        public override ReturnCode Prompt(
            PromptType type,
            ref PromptFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            flags &= ~PromptFlags.Done;

            Interpreter localInterpreter = InternalSafeGetInterpreter(
                false);

            if (localInterpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            //
            // BUGFIX: Always bypass the interpreter readiness checks here;
            //         otherwise, we can get into very nasty situations (e.g.
            //         infinite recursion for [debug oncancel], etc).
            //
            ReturnCode code;
            Result value = null;

            if ((type != PromptType.None) &&
                (localInterpreter.GetVariableValue(
                    VariableFlags.ViaPrompt, GetPromptVariableName(
                        type, flags), ref value) == ReturnCode.Ok))
            {
                Result result = null;
                int errorLine = 0;

                code = localInterpreter.EvaluatePromptScript(
                    value, ref result, ref errorLine);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: The prompt script probably displayed some kind
                    //       of prompt; therefore, we are done.
                    //
                    flags |= PromptFlags.Done;
                }
                else
                {
                    //
                    // NOTE: Attempt to show the error from the prompt script.
                    //
                    /* IGNORED */
                    WriteResultLine(code, result, errorLine);

                    //
                    // NOTE: Add error information to the interpreter.
                    //
                    Engine.AddErrorInformation(
                        localInterpreter, result, String.Format(
                            "{0}    (script that generates prompt, line {1})",
                            Environment.NewLine, errorLine));

                    //
                    // NOTE: Now, transfer the prompt script evaluation error
                    //       to the caller.
                    //
                    error = result;
                }
            }
            else
            {
                //
                // NOTE: Either our caller requested a prompt type of "None"
                //       -OR- there is no prompt script configured.  So far,
                //       this has been a complete success.
                //
                code = ReturnCode.Ok;
            }

            //
            // NOTE: If we did not evaluate a prompt script -OR- if that script
            //       failed then we attempt to output the appropriate default
            //       prompt.
            //
            if ((value == null) || (code != ReturnCode.Ok))
            {
                //
                // NOTE: Now, we need to fallback to the default
                //       prompt.
                //
                string prompt = HostOps.GetDefaultPrompt(type, flags);

                //
                // NOTE: If we got a valid default prompt for this
                //       type, attempt to write it now.
                //
                if ((prompt != null) && Write(prompt))
                {
                    //
                    // NOTE: We displayed the debug prompt for this
                    //       type.
                    //
                    flags |= PromptFlags.Done;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support the "Prompt", "CreateThread",
                //       "QueueWorkItem", "Sleep", "Yield",
                //       "GetStream", and "GetScript" methods.
                //
                hostFlags = HostFlags.Prompt | HostFlags.Thread |
                            HostFlags.WorkItem | HostFlags.Stream |
                            HostFlags.Script | HostFlags.Sleep |
                            HostFlags.Yield | base.GetHostFlags();

#if ISOLATED_PLUGINS
                //
                // NOTE: If this host is not running in the same
                //       application domain as the parent interpreter,
                //       also add the "Isolated" flag.
                //
                if (SafeIsIsolated())
                    hostFlags |= HostFlags.Isolated;
#endif
            }

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int readLevels;
        public override int ReadLevels
        {
            get
            {
                CheckDisposed();

                int localReadLevels = Interlocked.CompareExchange(
                    ref readLevels, 0, 0);

                return localReadLevels;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int writeLevels;
        public override int WriteLevels
        {
            get
            {
                CheckDisposed();

                int localWriteLevels = Interlocked.CompareExchange(
                    ref writeLevels, 0, 0);

                return localWriteLevels;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        public override ReturnCode GetStream(
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize,
            FileOptions options,
            ref HostStreamFlags flags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                return RuntimeOps.NewStream(
                    UnsafeGetInterpreter(), path, mode, access, share,
                    bufferSize, options, ref flags, ref fullPath,
                    ref stream, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode GetScript(
            string name,
            ref ScriptFlags flags,
            ref IClientData clientData,
            ref Result result
            )
        {
            CheckDisposed();

            //
            // NOTE: The purpose of this routine is to redirect requests for
            //       library scripts made by the script engine to our internal
            //       resources (i.e. so that the scripts do not have to exist
            //       elsewhere on the file system).
            //
            Interpreter localInterpreter = InternalSafeGetInterpreter(false);

            GetScriptTrace(
                localInterpreter, "entered",
                name, flags, clientData,
                ReturnCode.Ok, result);

            //
            // NOTE: Permit the key parameters to be customized by derived
            //       classes as as well with the configured core script flags,
            //       if any.
            //
            if (!CheckScriptParameters(
                    localInterpreter, ref name, ref flags, ref clientData,
                    ref result)) /* HOOK */
            {
                GetScriptTrace(
                    localInterpreter,
                    "exited, bad parameters",
                    name, flags, clientData,
                    ReturnCode.Error, result);

                return ReturnCode.Error;
            }

            //
            // NOTE: Check if the requested script name is allowed.  If not,
            //       then return an error now.
            //
            if (!ShouldAllowScriptParameters(
                    localInterpreter, ref name, ref flags, ref clientData,
                    ref result)) /* HOOK */
            {
                GetScriptTrace(
                    localInterpreter,
                    "exited, access denied",
                    name, flags, clientData,
                    ReturnCode.Error, result);

                return ReturnCode.Error;
            }
            //
            // NOTE: Otherwise, if the script name appears to be a file name
            //       with no directory information -AND- the script name is
            //       reserved by the host (e.g. "pkgIndex.eagle"), issue a
            //       warning now.
            //
            else if (IsReservedScriptName(
                    localInterpreter, name, flags, clientData)) /* HOOK */
            {
                bool exists = false;

                if (IsFileNameOnlyScriptName(name))
                {
                    GetScriptTrace(localInterpreter,
                        "WARNING: detected reserved script name without directory",
                        name, flags, clientData, ReturnCode.Ok, result);
                }
                else if (!IsAbsoluteFileNameScriptName(name, ref exists) && !exists)
                {
                    GetScriptTrace(localInterpreter,
                        "WARNING: detected reserved script name with relative path",
                        name, flags, clientData, ReturnCode.Ok, result);
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Make sure the script name is [still?] valid.
            //
            if (name == null)
            {
                result = "invalid script name";

                GetScriptTrace(
                    localInterpreter,
                    "exited, invalid script name",
                    name, flags, clientData,
                    ReturnCode.Error, result);

                return ReturnCode.Error;
            }

            //
            // NOTE: An interpreter instance is required in order to help
            //       locate the script.  If we do not have one, bail out now.
            //
            if (localInterpreter == null)
            {
                result = "invalid interpreter";

                GetScriptTrace(
                    localInterpreter,
                    "exited, invalid interpreter",
                    name, flags, clientData,
                    ReturnCode.Error, result);

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Are we operating in the "quiet" error handling
            //       mode?
            //
            bool quiet = FlagOps.HasFlags(flags, ScriptFlags.Quiet, true);

            //
            // NOTE: These are the tracking flags for which subsystems were
            //       actually checked.  The first element is for the file
            //       system.  The second element is for all the loaded
            //       plugins, excluding system plugins.  The third element
            //       is the customizable resource manager associated with
            //       this host.  The fourth element is the application
            //       resource manager for the assembly this host belongs
            //       to.  The fifth element is the library resource manager
            //       for the assembly this host belongs to.  The sixth
            //       element is the resource manager associated with the
            //       parent interpreter.  The seventh element is the core
            //       library assembly manifest.
            //
            bool[] @checked = {
                false, false, false, false, false, false, false, false
            };

            //
            // NOTE: These are the tracking counts for how many tries
            //       were performed using the file system, plugins, and
            //       resource managers.
            //
            int[] counts = { 0, 0, 0, 0 };

            //
            // NOTE: This is the list of errors encountered during the
            //       search for the requested script.
            //
            ResultList errors = null;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: First, if it has not been prohibited by the caller,
            //       try to get the requested script externally, using
            //       our standard file system search routine.
            //
            if (!FlagOps.HasFlags(
                    flags, ScriptFlags.NoFileSystem, true))
            {
                @checked[0] = true;

                ReturnCode code = GetScriptViaFileSystem(
                    localInterpreter, name, counts, ref flags,
                    ref clientData, ref result, ref errors);

                if ((code == ReturnCode.Ok) ||
                    (code == ReturnCode.Error))
                {
                    GetScriptTrace(
                        localInterpreter,
                        "exited, via file system",
                        name, flags, clientData,
                        code, result);

                    return code;
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!FlagOps.HasFlags(flags, ScriptFlags.NoResources, true))
            {
                //
                // NOTE: Are we operating in the "verbose" error handling
                //       mode?
                //
                bool verbose = FlagOps.HasFlags(flags,
                    ScriptFlags.Verbose, true);

                StringDictionary uniqueResourceNames = null;

                PopulateUniqueResourceNames(
                    localInterpreter, name, flags, verbose,
                    ref uniqueResourceNames);

                EngineFlags engineFlags = GetEngineFlagsForReadScript(
                    localInterpreter, flags);

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: See if we are allowed to search for the script via
                //       plugin resource strings.
                //
                if (!FlagOps.HasFlags(flags, ScriptFlags.NoPlugins, true))
                {
                    PluginWrapperDictionary plugins =
                        localInterpreter.CopyPlugins();

                    if (plugins != null)
                    {
                        @checked[1] = true;

                        CultureInfo cultureInfo =
                            localInterpreter.CultureInfo;

                        foreach (KeyValuePair<string, _Wrappers.Plugin>
                                pair in plugins)
                        {
                            IPlugin plugin = pair.Value;

                            //
                            // NOTE: This method *MUST* return
                            //       "ReturnCode.Continue" in
                            //       order to keep searching.
                            //
                            ReturnCode code = GetScriptViaPlugin(
                                localInterpreter, name, plugin,
                                uniqueResourceNames, cultureInfo,
                                engineFlags, verbose, counts,
                                ref flags, ref clientData,
                                ref result, ref errors);

                            if ((code == ReturnCode.Ok) ||
                                (code == ReturnCode.Error))
                            {
                                GetScriptTrace(
                                    localInterpreter,
                                    "exited, via plugin",
                                    name, flags, clientData,
                                    code, result);

                                return code;
                            }
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: When compiled with isolated plugin support,
                //       check if the current method is running in
                //       an application domain isolated from our
                //       parent interpreter.
                //
#if ISOLATED_PLUGINS
                bool isolated = SafeIsIsolated(localInterpreter);
#else
                bool isolated = false;
#endif

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Are we being forbidden from using any resource
                //       managers?
                //
                if (!FlagOps.HasFlags(
                        flags, ScriptFlags.NoResourceManager, true))
                {
                    //
                    // NOTE: In order to use the interpreter resource
                    //       manager, we must be in the same application
                    //       domain.  We should always be able to use
                    //       both our own resource manager and the one
                    //       associated with the assembly containing
                    //       this host.  Grab and check them both now.
                    //
                    ResourceManager thisResourceManager = !FlagOps.HasFlags(
                        flags, ScriptFlags.NoHostResourceManager, true) ?
                            this.ResourceManager : null;

                    if (thisResourceManager != null)
                        @checked[2] = true;

                    ResourceManager applicationResourceManager = !FlagOps.HasFlags(
                        flags, ScriptFlags.NoApplicationResourceManager, true) ?
                            this.ApplicationResourceManager : null;

                    if (applicationResourceManager != null)
                        @checked[3] = true;

                    ResourceManager libraryResourceManager = !FlagOps.HasFlags(
                        flags, ScriptFlags.NoLibraryResourceManager, true) ?
                            this.LibraryResourceManager : null;

                    if (libraryResourceManager != null)
                        @checked[4] = true;

                    ResourceManager packagesResourceManager = !FlagOps.HasFlags(
                        flags, ScriptFlags.NoPackagesResourceManager, true) ?
                            this.PackagesResourceManager : null;

                    if (packagesResourceManager != null)
                        @checked[5] = true;

                    //
                    // NOTE: If this host is running isolated (i.e. in
                    //       an isolated application domain, via a
                    //       plugin), skip using the resource manager
                    //       from the interpreter because it cannot be
                    //       marshalled from the other application
                    //       domain (it's a private field).
                    //
                    ResourceManager interpreterResourceManager =
#if ISOLATED_PLUGINS
                        !isolated ? localInterpreter.ResourceManager : null;
#else
                        localInterpreter.ResourceManager;
#endif

                    if (interpreterResourceManager != null)
                        @checked[6] = true;

                    //
                    // NOTE: We prefer to use the customizable resource
                    //       manager, then the application resource
                    //       manager, then the library resource manager,
                    //       and finally the resource manager for the
                    //       interpreter that we are associated with,
                    //       which may contain scripts.
                    //
                    IAnyPair<string, ResourceManager>[] resourceManagers =
                        new AnyPair<string, ResourceManager>[] {
                        new AnyPair<string, ResourceManager>(
                            null, thisResourceManager),
                        new AnyPair<string, ResourceManager>(
                            GlobalState.GetAssemblyLocation(),
                            applicationResourceManager),
                        new AnyPair<string, ResourceManager>(
                            GlobalState.GetAssemblyLocation(),
                            packagesResourceManager),
                        new AnyPair<string, ResourceManager>(
                            GlobalState.GetAssemblyLocation(),
                            libraryResourceManager),
                        new AnyPair<string, ResourceManager>(
                            null, interpreterResourceManager)
                    };

                    foreach (IAnyPair<string, ResourceManager>
                            anyPair in resourceManagers)
                    {
                        //
                        // NOTE: This method *MUST* return
                        //       "ReturnCode.Continue" in
                        //       order to keep searching.
                        //
                        ReturnCode code = GetScriptViaResourceManager(
                            localInterpreter, name, anyPair,
                            uniqueResourceNames, engineFlags,
                            verbose, isolated, counts, ref flags,
                            ref clientData, ref result, ref errors);

                        if ((code == ReturnCode.Ok) ||
                            (code == ReturnCode.Error))
                        {
                            GetScriptTrace(
                                localInterpreter,
                                "exited, via resource manager",
                                name, flags, clientData,
                                code, result);

                            return code;
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Are we being forbidden from using the assembly
                //       manifest?
                //
                if (!FlagOps.HasFlags(
                        flags, ScriptFlags.NoAssemblyManifest, true))
                {
                    Assembly assembly = GlobalState.GetAssembly();

                    if (assembly != null)
                        @checked[7] = true;

                    //
                    // NOTE: This method *MUST* return
                    //       "ReturnCode.Continue" in
                    //       order to keep searching.
                    //
                    ReturnCode code = GetScriptViaAssemblyManifest(
                        localInterpreter, name, assembly,
                        uniqueResourceNames, engineFlags,
                        verbose, isolated, counts, ref flags,
                        ref clientData, ref result, ref errors);

                    if ((code == ReturnCode.Ok) ||
                        (code == ReturnCode.Error))
                    {
                        GetScriptTrace(
                            localInterpreter,
                            "exited, via assembly manifest",
                            name, flags, clientData,
                            code, result);

                        return code;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (errors == null)
                errors = new ResultList();

            /* NOT VERBOSE */
            errors.Insert(0, String.Format(
                "script \"{0}\" not found",
                name));

            //
            // NOTE: In quiet mode, skip the other error information.
            //
            if (!quiet)
            {
                if (!@checked[0])
                    /* NOT VERBOSE */
                    errors.Add("skipped file system");

                if (counts[0] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no files were checked");

                if (!@checked[1])
                    /* NOT VERBOSE */
                    errors.Add("skipped plugin list");

                if (counts[1] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no plugin was queried");

                if (!@checked[2])
                    /* NOT VERBOSE */
                    errors.Add("skipped extension resource manager");

                if (!@checked[3])
                    /* NOT VERBOSE */
                    errors.Add("skipped application resource manager");

                if (!@checked[4])
                    /* NOT VERBOSE */
                    errors.Add("skipped library resource manager");

                if (!@checked[5])
                    /* NOT VERBOSE */
                    errors.Add("skipped packages resource manager");

                if (!@checked[6])
                    /* NOT VERBOSE */
                    errors.Add("skipped interpreter resource manager");

                if (counts[2] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no resource manager was queried");

                if (!@checked[7])
                    /* NOT VERBOSE */
                    errors.Add("skipped assembly manifest");

                if (counts[3] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no assembly manifest was queried");
            }

            result = errors;

            GetScriptTrace(
                localInterpreter,
                "exited, script not found",
                name, flags, clientData,
                ReturnCode.Error, result);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IProcessHost Members
        public override bool CanExit
        {
            get
            {
                CheckDisposed();

                //
                // NOTE: This configuration parameter is considered to be
                //       part of the configuration of the interpreter itself,
                //       hence those flags are used here.
                //
                if (GlobalConfiguration.DoesValueExist(EnvVars.NoExit,
                        ConfigurationFlags.Interpreter)) /* EXEMPT */
                {
                    return false;
                }

                return base.CanExit;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        public override ReturnCode CreateThread(
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                thread = Engine.CreateThread(
                    start, maxStackSize, userInterface, isBackground);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode CreateThread(
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                thread = Engine.CreateThread(
                    start, maxStackSize, userInterface, isBackground);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                if (Engine.QueueWorkItem(callback, state))
                    return ReturnCode.Ok;
                else
                    error = "could not queue work item";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            try
            {
                HostOps.ThreadSleepOrMaybeComplain(milliseconds, false);

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool Yield()
        {
            CheckDisposed();

            try
            {
                HostOps.Yield();

                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Core).Name,
                    TracePriority.HostError);

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        public override IHost Clone()
        {
            CheckDisposed();

            return Clone(UnsafeGetInterpreter());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        public override ReturnCode GetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: Only the "default" theme (i.e. using null or an empty
            //       string for the configuration) is supported for now.
            //
            if (String.IsNullOrEmpty(theme))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    try
                    {
                        ConsoleColor localForegroundColor = DefaultForegroundColor;
                        ConsoleColor localBackgroundColor = DefaultBackgroundColor;

                        //
                        // NOTE: Did they request the foreground color?
                        //
                        if ((code == ReturnCode.Ok) && foreground)
                        {
                            PropertyInfo property = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, ForegroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (property != null)
                            {
                                if (property.CanRead)
                                {
                                    localForegroundColor = (ConsoleColor)property.GetValue(
                                        this, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for foreground color \"{0}\" cannot be read",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for foreground color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Did they request the background color?
                        //
                        if ((code == ReturnCode.Ok) && background)
                        {
                            PropertyInfo property = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, BackgroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (property != null)
                            {
                                if (property.CanRead)
                                {
                                    localBackgroundColor = (ConsoleColor)property.GetValue(
                                        this, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for background color \"{0}\" cannot be read",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for background color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: If we succeeded in looking up the requested colors,
                        //       return them now.
                        //
                        if (code == ReturnCode.Ok)
                        {
                            foregroundColor = localForegroundColor;
                            backgroundColor = localBackgroundColor;
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid color name";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "unsupported theme name";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode SetColors(
            string theme, /* RESERVED */
            string name,
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor,
            ref Result error
            )
        {
            CheckDisposed();

            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: Only the "default" theme (i.e. using null or an empty
            //       string for the configuration) is supported for now.
            //
            if (String.IsNullOrEmpty(theme))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    try
                    {
                        //
                        // NOTE: Did they request the foreground color?
                        //
                        if ((code == ReturnCode.Ok) && foreground)
                        {
                            PropertyInfo property = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, ForegroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (property != null)
                            {
                                if (property.CanWrite)
                                {
                                    property.SetValue(this, foregroundColor, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for foreground color \"{0}\" cannot be written",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for foreground color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Did they request the background color?
                        //
                        if ((code == ReturnCode.Ok) && background)
                        {
                            PropertyInfo property = base.GetType().GetProperty(
                                String.Format("{0}{1}", name, BackgroundColorSuffix),
                                HostPropertyBindingFlags, null, typeof(ConsoleColor),
                                Type.EmptyTypes, null);

                            if (property != null)
                            {
                                if (property.CanWrite)
                                {
                                    property.SetValue(this, backgroundColor, null);
                                }
                                else
                                {
                                    error = String.Format(
                                        "property for background color \"{0}\" cannot be written",
                                        name);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "property for background color \"{0}\" not found",
                                    name);

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid color name";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "unsupported theme name";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override string DefaultTitle
        {
            get
            {
                CheckDisposed();

                try
                {
                    if (base.DefaultTitle == null)
                    {
                        string packageName = GlobalState.GetPackageName();

                        if (!String.IsNullOrEmpty(packageName))
                        {
                            Assembly assembly = GlobalState.GetAssembly();
                            base.DefaultTitle = BuildCoreTitle(packageName, assembly);
                        }
                    }

                    return base.DefaultTitle;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Core).Name,
                        TracePriority.HostError);
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override bool ResetFlags()
        {
            CheckDisposed();

            return PrivateResetFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if (base.Reset(ref error) == ReturnCode.Ok)
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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed &&
                Engine.IsThrowOnDisposed(interpreter /* EXEMPT */, null))
            {
                throw new InterpreterDisposedException(typeof(Core));
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
