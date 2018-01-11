/*
 * PackageOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("e6e6c799-cbfd-4aa3-9017-c9944322a81c")]
    internal static class PackageOps
    {
        #region Private Constants
        //
        // NOTE: These are the ScriptFlags that are *always* used when trying
        //       to fetch the "pkgIndex.eagle" file via the interpreter host.
        //
        private static readonly ScriptFlags IndexScriptFlags =
            ScriptFlags.PackageLibraryOptionalFile;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Checking Methods
        public static int VersionCompare(
            Version version1,
            Version version2
            )
        {
            if ((version1 != null) && (version2 != null))
                return version1.CompareTo(version2);
            else if ((version1 == null) && (version2 == null))
                return 0;        // x (null) is equal to y (null)
            else if (version1 == null)
                return -1;       // x (null) is less than y (non-null)
            else
                return 1;        // x (non-null) is greater than y (null)
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VersionSatisfies(
            Version version1,
            Version version2,
            bool exact
            )
        {
            if (exact)
                return (VersionCompare(version1, version2) == 0);
            else
                return (VersionCompare(version1, version2) >= 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
        public static IPackage NewCore(
            string name,
            string group,
            string description,
            IClientData clientData,
            string indexFileName,
            string provideFileName,
            PackageFlags flags,
            Version loaded,
            VersionStringDictionary ifNeeded
            )
        {
            return new _Packages.Core(new PackageData(
                name, group, description, clientData, indexFileName,
                provideFileName, flags, loaded, ifNeeded, 0));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Support Methods
        public static string GetRelativeFileName(
            Interpreter interpreter,               /* in */
            string name,                           /* in, script name */
            PathComparisonType pathComparisonType, /* in */
            bool verbose                           /* in */
            )
        {
            string fileName = null;
            Result error = null;

            if (GetRelativeFileName(
                    interpreter, name, pathComparisonType,
                    ref fileName, ref error) == ReturnCode.Ok)
            {
                return fileName;
            }
            else if (verbose)
            {
                TraceOps.DebugTrace(String.Format(
                    "GetRelativeFileName: interpreter = {0}, " +
                    "name = {1}, pathComparisonType = {2}, " +
                    "verbose = {3}, fileName = {4}, error = {5}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(pathComparisonType), verbose,
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.WrapOrNull(error)), typeof(PackageOps).Name,
                    TracePriority.PathDebug);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetRelativeFileName(
            Interpreter interpreter,               /* in */
            string name,                           /* in, script name */
            PathComparisonType pathComparisonType, /* in */
            ref string fileName,                   /* out */
            ref Result error                       /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(name))
            {
                error = "invalid script name";
                return ReturnCode.Error;
            }

            StringList packageIndexFileNames;

            PackageIndexDictionary packageIndexes =
                interpreter.CopyPackageIndexes();

            if (packageIndexes == null)
            {
                error = "package indexes not available";
                return ReturnCode.Error;
            }

            //
            // NOTE: Sort the package index file names in order so
            //       that the deepest directories are listed first.
            //
            packageIndexFileNames = new StringList(packageIndexes.Keys);

            packageIndexFileNames.Sort(_Comparers.FileName.Create(
                pathComparisonType));

            string localFileName = PathOps.ResolveFullPath(interpreter, name);

            if (localFileName == null)
            {
                error = String.Format(
                    "failed to resolve full path of \"{0}\"",
                    name);

                return ReturnCode.Error;
            }

            string directory = PathOps.GetDirectoryName(localFileName);

            if (directory == null)
            {
                error = String.Format(
                    "failed to get directory name for \"{0}\"",
                    localFileName);

                return ReturnCode.Error;
            }

            directory = PathOps.AppendSeparator(directory);

            foreach (string packageIndexFileName in packageIndexFileNames)
            {
                string packageDirectory = PathOps.GetDirectoryName(
                    packageIndexFileName);

                if (String.IsNullOrEmpty(packageDirectory))
                    continue;

#if MONO || MONO_HACKS
                //
                // HACK: *MONO* The Mono call to Path.GetDirectoryName does not
                //       appear to convert the forward slashes in the directory
                //       name to backslashes as the .NET does; therefore, force
                //       a conversion by fully resolving the directory name, but
                //       only when running on Mono.
                //
                if (CommonOps.Runtime.IsMono())
                {
                    packageDirectory = PathOps.ResolveFullPath(interpreter,
                        packageDirectory);
                }
#endif

                packageDirectory = PathOps.AppendSeparator(packageDirectory);

                if (PathOps.IsEqualFileName(
                        packageDirectory, directory, packageDirectory.Length))
                {
                    fileName = PathOps.GetUnixPath(localFileName.Substring(
                        packageDirectory.Length));

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "package index matching directory \"{0}\" not found",
                directory);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool AdjustFileName(
            IClientData clientData, /* in */
            ref string fileName     /* in, out */
            )
        {
            if (clientData == null)
                return false;

            //
            // TODO: Adjust this type check if the GetScript() method for
            //       the "Core" (i.e. Eagle._Hosts.Core) host changes what
            //       could be provided in the associated IClientData.
            //
            IAnyTriplet<IPluginData, string, string> anyTriplet1 =
                clientData.Data as IAnyTriplet<IPluginData, string, string>;

            IAnyTriplet<IAnyPair<string, ResourceManager>, string, string>
                anyTriplet2 = clientData.Data as IAnyTriplet<IAnyPair<string,
                ResourceManager>, string, string>;

            string prefixFileName;

            if ((anyTriplet1 == null) && (anyTriplet2 == null))
            {
                return false;
            }
            else if (anyTriplet1 != null)
            {
                IPluginData pluginData = anyTriplet1.X;

                if (pluginData == null)
                    return false;

                prefixFileName = pluginData.FileName;
            }
            else
            {
                IAnyPair<string, ResourceManager> anyPair = anyTriplet2.X;

                if (anyPair == null)
                    return false;

                prefixFileName = anyPair.X;
            }

            if (String.IsNullOrEmpty(prefixFileName))
                return false;

            if (!String.IsNullOrEmpty(fileName))
            {
                fileName = PathOps.GetUnixPath(PathOps.CombinePath(
                    null, prefixFileName, fileName));
            }
            else
            {
                fileName = prefixFileName;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddFileNameWithFlags(
            PackageIndexDictionary packageIndexes,
            string fileName,
            PackageIndexFlags addFlags
            )
        {
            if (packageIndexes == null)
                return;

            PackageIndexFlags flags;

            /* IGNORED */
            packageIndexes.TryGetValue(fileName, out flags);

            /* NO RESULT */
            packageIndexes[fileName] = flags | addFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode FindHost(
            Interpreter interpreter,                   /* in */
            StringList paths,                          /* in */ /* NOT USED */
            PackageIndexCallback callback,             /* in */
            PackageIndexFlags flags,                   /* in */
            ref PackageIndexDictionary packageIndexes, /* in, out */
            ref Result error                           /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.HasNoPackageIndexes())
                return ReturnCode.Ok;

            if (paths == null) /* NOT USED */
            {
                error = "invalid paths";
                return ReturnCode.Error;
            }

            //
            // NOTE: Initialize the package index collection if
            //       necessary.
            //
            if (packageIndexes == null)
                packageIndexes = new PackageIndexDictionary();

            //
            // NOTE: The file name we are interested in is the host
            //       package index, which is normally named something
            //       like "lib/Eagle1.0/pkgIndex.eagle".
            //
            string fileName = GetIndexFileName(interpreter, false);

            //
            // NOTE: The fully qualified library path and file name for
            //       the host package index, if it actually existed on
            //       the file system, which is normally named something
            //       like "<dir>/lib/Eagle1.0/pkgIndex.eagle".
            //
            string libraryFileName = GetIndexFileName(interpreter, true);

            //
            // NOTE: Initially mark the host package index as
            //       "not found".  After the main search (below), if
            //       the host package index is still marked "not found"
            //       it will be purged.
            //
            if (MarkIndex(
                    packageIndexes, fileName, PackageIndexFlags.Host,
                    PackageIndexFlags.Normal, PackageIndexFlags.Found,
                    true, false, false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Modify the package index flags so that we perform
            //       the correct type of search.
            //
            flags &= ~PackageIndexFlags.Normal;
            flags |= PackageIndexFlags.Host;

            //
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Host |
                PackageIndexFlags.Found;

            //
            // NOTE: If we are refreshing package indexes or we have
            //       never seen this package index before, notify the
            //       caller.
            //
            bool refresh = FlagOps.HasFlags(
                flags, PackageIndexFlags.Refresh, true);

            //
            // NOTE: Have we seen this package index before?
            //
            bool exists = packageIndexes.ContainsKey(fileName) ||
                packageIndexes.ContainsKey(libraryFileName); /* EXEMPT */

            //
            // NOTE: When set to non-zero, forcibly purge the package
            //       index, due to changes in the file name.
            //
            bool purge = false;

            if (refresh || !exists)
            {
                interpreter.SetNoPackageIndexes(true);

                try
                {
                    if (callback != null)
                    {
                        ReturnCode code;
                        IClientData clientData = ClientData.Empty;
                        Result result = null;

                        code = callback(
                            interpreter, fileName, ref flags,
                            ref clientData, ref result);

                        if (code != ReturnCode.Ok)
                        {
                            error = result;
                            return code;
                        }

                        if (FlagOps.HasFlags(
                                flags, PackageIndexFlags.Evaluated,
                                true))
                        {
                            string newFileName = fileName;

                            if (AdjustFileName(
                                    clientData, ref newFileName))
                            {
                                AddFileNameWithFlags(
                                    packageIndexes, newFileName,
                                    addFlags);

                                purge = true;
                            }
                        }
                        else
                        {
                            purge = true;
                        }
                    }
                }
                finally
                {
                    interpreter.SetNoPackageIndexes(false);
                }
            }
            else
            {
                purge = true;
            }

            //
            // NOTE: If we have not seen this package index before add
            //       it to the resulting collection now; otherwise,
            //       mark it as "found" so that it will not be purged.
            //
            if (!purge)
                AddFileNameWithFlags(packageIndexes, fileName, addFlags);

            //
            // NOTE: Purge the host package index from the list if it
            //       is still marked as "not found".
            //
            if (PurgeIndex(
                    packageIndexes, fileName, PackageIndexFlags.Host,
                    PackageIndexFlags.Normal | PackageIndexFlags.Found,
                    true, false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode FindFile(
            Interpreter interpreter,                   /* in */
            StringList paths,                          /* in */
            PackageIndexCallback callback,             /* in */
            PackageIndexFlags flags,                   /* in */
            ref PackageIndexDictionary packageIndexes, /* in, out */
            ref Result error                           /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreter.HasNoPackageIndexes())
                return ReturnCode.Ok;

            if (paths == null)
            {
                error = "invalid paths";
                return ReturnCode.Error;
            }

            //
            // NOTE: Initialize the package index collection if
            //       necessary.
            //
            if (packageIndexes == null)
                packageIndexes = new PackageIndexDictionary();

            //
            // NOTE: Initially mark all package indexes as "not found".
            //       After the main search loop (below), any remaining
            //       package indexes that are still marked "not found"
            //       will be purged.
            //
            if (MarkIndexes(
                    packageIndexes, PackageIndexFlags.Normal,
                    PackageIndexFlags.Host, PackageIndexFlags.Found,
                    false, false, false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Modify the package index flags so that we perform
            //       the correct type of search.
            //
            flags &= ~PackageIndexFlags.Host;
            flags |= PackageIndexFlags.Normal;

            //
            // NOTE: What are the package index flags to add when the
            //       package index is found?
            //
            PackageIndexFlags addFlags = PackageIndexFlags.Normal |
                PackageIndexFlags.Found;

            //
            // NOTE: Find all the package index files in the specified
            //       paths, optionally looking in all sub-directories.
            //
            bool recursive = FlagOps.HasFlags(
                flags, PackageIndexFlags.Recursive, true);

            bool refresh = FlagOps.HasFlags(
                flags, PackageIndexFlags.Refresh, true);

            foreach (string path in paths)
            {
                //
                // NOTE: Normalize the path prior to using it or adding
                //       it to the dictionary.
                //
                string newPath = PathOps.ResolveFullPath(interpreter,
                    path);

                //
                // NOTE: Make sure the directory exists prior to
                //       attempting to find any files in it; otherwise,
                //       we just ignore it to reduce the burden on the
                //       caller to validate that a given path actually
                //       exists (which could be especially burdensome
                //       if it is constructed dynamically from
                //       environment variables, etc).
                //
                if (!String.IsNullOrEmpty(newPath) &&
                    Directory.Exists(newPath))
                {
                    //
                    // NOTE: Find all package index files in the
                    //       specified directory.
                    //
                    StringList fileNames = new StringList(
                        Directory.GetFiles(newPath,
                            PathOps.ScriptFileNameOnly(
                                GetIndexFileName(interpreter,
                                    false)) /* PATTERN */,
                            recursive ?
                                SearchOption.AllDirectories :
                                SearchOption.TopDirectoryOnly));

                    //
                    // NOTE: For each package index file, notify the
                    //       callback if it is new and/or add it to the
                    //       resulting collection.
                    //
                    foreach (string fileName in fileNames)
                    {
                        //
                        // NOTE: Have we seen this package index before?
                        //
                        bool exists = packageIndexes.ContainsKey(
                            fileName); /* EXEMPT */

                        //
                        // NOTE: When set to non-zero, forcibly purge the
                        //       package index, due to changes in the file
                        //       name.
                        //
                        bool purge = false;

                        //
                        // NOTE: If we are refreshing package indexes or
                        //       we have never seen this package index
                        //       before, notify the caller.
                        //
                        if (refresh || !exists)
                        {
                            interpreter.SetNoPackageIndexes(true);

                            try
                            {
                                if (callback != null)
                                {
                                    ReturnCode code;
                                    IClientData clientData = ClientData.Empty;
                                    Result result = null;

                                    code = callback(
                                        interpreter, fileName, ref flags,
                                        ref clientData, ref result);

                                    if (code != ReturnCode.Ok)
                                    {
                                        error = result;
                                        return code;
                                    }

                                    if (FlagOps.HasFlags(
                                            flags, PackageIndexFlags.Evaluated,
                                            true))
                                    {
                                        string newFileName = fileName;

                                        if (AdjustFileName(
                                                clientData, ref newFileName))
                                        {
                                            AddFileNameWithFlags(
                                                packageIndexes, newFileName,
                                                addFlags);

                                            purge = true;
                                        }
                                    }
                                    else
                                    {
                                        purge = true;
                                    }
                                }
                            }
                            finally
                            {
                                interpreter.SetNoPackageIndexes(false);
                            }
                        }

                        //
                        // NOTE: If we have not seen this package index
                        //       before add it to the resulting
                        //       collection now; otherwise, mark it as
                        //       "found" so that it will not be purged.
                        //
                        if (!purge)
                        {
                            AddFileNameWithFlags(
                                packageIndexes, fileName, addFlags);
                        }
                    }
                }
            }

            //
            // NOTE: Purge any package indexes from the list that are
            //       still marked as "not found".
            //
            if (PurgeIndexes(
                    packageIndexes, PackageIndexFlags.None,
                    PackageIndexFlags.Host | PackageIndexFlags.Found,
                    false, false, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldPreferFileSystem(
            PackageIndexFlags flags
            )
        {
            if (FlagOps.HasFlags(
                    flags, PackageIndexFlags.PreferFileSystem, true))
            {
                return true;
            }

            if (FlagOps.HasFlags(
                    flags, PackageIndexFlags.PreferHost, true))
            {
                return false;
            }

            if (FlagOps.HasFlags(
                    IndexScriptFlags, ScriptFlags.PreferFileSystem, true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetIndexFileName(
            Interpreter interpreter,
            bool full
            )
        {
            string fileName = FileName.LibraryPackageIndex;

            if (full)
            {
                //
                // NOTE: First, fetch the library path for the interpreter.
                //       If this is null or an empty string, it will simply
                //       be ignored.
                //
                string[] directories = {
                    //
                    // NOTE: This is the interpreter library path, if any.
                    //
                    PathOps.GetUnixPath(
                        GlobalState.GetLibraryPath(interpreter)),

                    //
                    // NOTE: This may be used to store the directory name
                    //       portion for the library package index file
                    //       name.  It will be set below if necessary.
                    //
                    null
                };

                if (!String.IsNullOrEmpty(directories[0]))
                {
                    //
                    // NOTE: Check if the library path for the interpreter
                    //       ends with the directory name portion of the
                    //       library package index file name.
                    //
                    directories[1] = PathOps.GetUnixPath(
                        PathOps.GetDirectoryName(fileName));

                    if (directories[0].EndsWith(
                            directories[1], PathOps.ComparisonType))
                    {
                        //
                        // NOTE: Yes.  In this case, append just the file
                        //       name portion of the library package index
                        //       file name.
                        //
                        fileName = PathOps.GetUnixPath(PathOps.CombinePath(
                            null, directories[0], Path.GetFileName(fileName)));
                    }
                    else
                    {
                        //
                        // NOTE: No.  Append the library package index file
                        //       name verbatim, including the directory name
                        //       portion.
                        //
                        fileName = PathOps.GetUnixPath(PathOps.CombinePath(
                            null, directories[0], fileName));
                    }
                }
            }

            return fileName;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FindAll(
            Interpreter interpreter,                   /* in */
            StringList paths,                          /* in */
            PackageIndexFlags flags,                   /* in */
            ref PackageIndexDictionary packageIndexes, /* in, out */
            ref Result error                           /* out */
            )
        {
            bool host = FlagOps.HasFlags(
                flags, PackageIndexFlags.Host, true);

            bool normal = FlagOps.HasFlags(
                flags, PackageIndexFlags.Normal, true);

            if (ShouldPreferFileSystem(flags))
            {
                if ((!normal || (FindFile(
                        interpreter, paths, IndexCallback, flags,
                        ref packageIndexes, ref error) == ReturnCode.Ok)) &&
                    (!host || (FindHost(
                        interpreter, paths, IndexCallback, flags,
                        ref packageIndexes, ref error) == ReturnCode.Ok)))
                {
                    return ReturnCode.Ok;
                }
            }
            else
            {
                if ((!host || (FindHost(
                        interpreter, paths, IndexCallback, flags,
                        ref packageIndexes, ref error) == ReturnCode.Ok)) &&
                    (!normal || (FindFile(
                        interpreter, paths, IndexCallback, flags,
                        ref packageIndexes, ref error) == ReturnCode.Ok)))
                {
                    return ReturnCode.Ok;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MatchFlags(
            PackageIndexFlags flags,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            bool hasAll,
            bool notHasAll
            )
        {
            if (((hasFlags == PackageIndexFlags.None) ||
                    FlagOps.HasFlags(flags, hasFlags, hasAll)) &&
                ((notHasFlags == PackageIndexFlags.None) ||
                    !FlagOps.HasFlags(flags, notHasFlags, notHasAll)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode MarkIndex(
            PackageIndexDictionary packageIndexes,
            string fileName,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            PackageIndexFlags markFlags,
            bool hasAll,
            bool notHasAll,
            bool mark,
            ref Result error
            )
        {
            if (packageIndexes != null)
            {
                if (fileName != null)
                {
                    PackageIndexFlags flags;

                    if (packageIndexes.TryGetValue(fileName, out flags))
                    {
                        if (MatchFlags(
                                flags, hasFlags, notHasFlags, hasAll,
                                notHasAll))
                        {
                            if (mark)
                                flags |= markFlags;
                            else
                                flags &= ~markFlags;

                            packageIndexes[fileName] = flags;
                        }
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "invalid package indexes";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode MarkIndexes(
            PackageIndexDictionary packageIndexes,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            PackageIndexFlags markFlags,
            bool hasAll,
            bool notHasAll,
            bool mark,
            ref Result error
            )
        {
            if (packageIndexes != null)
            {
                StringList list = new StringList(packageIndexes.Keys);

                for (int index = 0; index < list.Count; index++)
                {
                    PackageIndexFlags flags = packageIndexes[list[index]];

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
                    {
                        if (mark)
                            flags |= markFlags;
                        else
                            flags &= ~markFlags;

                        packageIndexes[list[index]] = flags;
                    }
                }

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid package indexes";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode PurgeIndex(
            PackageIndexDictionary packageIndexes,
            string fileName,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref Result error
            )
        {
            if (packageIndexes != null)
            {
                if (fileName != null)
                {
                    PackageIndexFlags flags;

                    if (packageIndexes.TryGetValue(fileName, out flags))
                    {
                        if (MatchFlags(
                                flags, hasFlags, notHasFlags, hasAll,
                                notHasAll))
                        {
                            packageIndexes.Remove(fileName);
                        }
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "invalid package indexes";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode PurgeIndexes(
            PackageIndexDictionary packageIndexes,
            PackageIndexFlags hasFlags,
            PackageIndexFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            ref Result error
            )
        {
            if (packageIndexes != null)
            {
                StringList list = new StringList(packageIndexes.Keys);

                for (int index = list.Count - 1; index >= 0; index--)
                {
                    PackageIndexFlags flags = packageIndexes[list[index]];

                    if (MatchFlags(
                            flags, hasFlags, notHasFlags, hasAll,
                            notHasAll))
                    {
                        packageIndexes.Remove(list[index]);
                    }
                }

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid package indexes";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode IndexCallback( /* PackageIndexCallback */
            Interpreter interpreter,
            string fileName,
            ref PackageIndexFlags flags,
            ref IClientData clientData,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            ReturnCode code;
            bool directory = false;

            try
            {
                bool host = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Host, true);

                bool noNormal = FlagOps.HasFlags(
                    flags, PackageIndexFlags.NoNormal, true);

                bool refresh = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Refresh, true);

                bool resolve = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Resolve, true);

                bool trace = FlagOps.HasFlags(
                    flags, PackageIndexFlags.Trace, true);

                bool noComplain = FlagOps.HasFlags(
                    flags, PackageIndexFlags.NoComplain, true);

                if (!trace)
                {
                    trace = ScriptOps.HasFlags(interpreter,
                        InterpreterFlags.TracePackageIndex, true);
                }

                if (host)
                {
                    //
                    // NOTE: It is important to note here that currently
                    //       there may only be a maximum of ONE package
                    //       index file provided by the host.
                    //
                    ScriptFlags scriptFlags = ScriptOps.GetFlags(
                        interpreter, IndexScriptFlags, false);

                    //
                    // BUGFIX: Forbid "host-only" package index files
                    //         from being read from the file system.
                    //
                    if (noNormal)
                    {
                        scriptFlags &= ~ScriptFlags.PreferFileSystem;
                        scriptFlags |= ScriptFlags.NoFileSystem;
                    }

                    //
                    // BUGFIX: This should not be hard-coded to use the
                    //         "pkgIndex.eagle" file name.  Instead, it
                    //         should use the file name provided by the
                    //         caller (which is still "pkgIndex.eagle").
                    //
                    string text = interpreter.GetScript(
                        fileName, ref scriptFlags, ref clientData);

                    if (!String.IsNullOrEmpty(text))
                    {
                        if (FlagOps.HasFlags(
                                scriptFlags, ScriptFlags.File, true))
                        {
                            bool remoteUri = PathOps.IsRemoteUri(text);

                            if (remoteUri || File.Exists(text))
                            {
                                string newText = text;

                                if (resolve && !remoteUri)
                                {
                                    //
                                    // NOTE: Attempt to resolve the file
                                    //       name to a fully qualified
                                    //       one.
                                    //
                                    newText = PathOps.ResolveFullPath(
                                        interpreter, newText);

                                    //
                                    // NOTE: Failing that, fallback to
                                    //       the original file name which
                                    //       has already been "validated".
                                    //
                                    if (String.IsNullOrEmpty(newText))
                                        newText = text;
                                }

                                //
                                // NOTE: The host for the interpreter seems
                                //       to indicate we should be able to
                                //       find the package index on the native
                                //       file system?  Ok, fine.  Setup the
                                //       directory variable properly.
                                //
                                code = interpreter.SetVariableValue( /* EXEMPT */
                                    VariableFlags.None, TclVars.Directory,
                                    PathOps.GetUnixPath(PathOps.GetDirectoryName(
                                    newText)), ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    directory = true;

                                    if (!interpreter.IsSafe() && FlagOps.HasFlags(
                                            flags, PackageIndexFlags.Safe, true))
                                    {
                                        code = interpreter.EvaluateSafeFile(
                                            null, newText, ref result);
                                    }
                                    else
                                    {
                                        code = interpreter.EvaluateFile(
                                            newText, ref result);
                                    }

                                    flags |= PackageIndexFlags.Evaluated;

                                    if (trace)
                                    {
                                        TraceOps.DebugTrace(String.Format(
                                            "IndexCallback: interpreter = {0}, " +
                                            "fileName = {1}, flags = {2}, host = {3}, " +
                                            "noNormal = {4}, refresh = {5}, resolve = {6}, " +
                                            "trace = {7}, newText = {8}, code = {9}, " +
                                            "result = {10}", FormatOps.InterpreterNoThrow(
                                            interpreter), FormatOps.WrapOrNull(fileName),
                                            FormatOps.WrapOrNull(flags), host, noNormal,
                                            refresh, resolve, trace, FormatOps.WrapOrNull(
                                            newText), code, FormatOps.WrapOrNull(true,
                                            true, result)), typeof(PackageOps).Name,
                                            TracePriority.PathDebug);
                                    }

                                    if (noComplain && (code != ReturnCode.Ok))
                                        code = ReturnCode.Ok;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "the provided \"{0}\" script file \"{1}\" is not " +
                                    "a valid remote uri and does not exist locally",
                                    ScriptTypes.PackageIndex, text);

                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            code = interpreter.SetVariableValue( /* EXEMPT */
                                VariableFlags.None, TclVars.Directory,
                                PathOps.GetUnixPath(PathOps.GetDirectoryName(
                                fileName)), ref result);

                            if (code == ReturnCode.Ok)
                            {
                                directory = true;

                                //
                                // BUGFIX: Use the original script [file?]
                                //         name, exactly as specified, for
                                //         any contained [info script] calls.
                                //
                                interpreter.PushScriptLocation(fileName, true);

                                try
                                {
                                    if (!interpreter.IsSafe() && FlagOps.HasFlags(
                                            flags, PackageIndexFlags.Safe, true))
                                    {
                                        code = interpreter.EvaluateSafeScript(
                                            text, ref result); /* EXEMPT */
                                    }
                                    else
                                    {
                                        code = interpreter.EvaluateScript(
                                            text, ref result); /* EXEMPT */
                                    }
                                }
                                finally
                                {
                                    interpreter.PopScriptLocation(true);
                                }

                                flags |= PackageIndexFlags.Evaluated;

                                if (trace)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "IndexCallback: interpreter = {0}, " +
                                        "fileName = {1}, flags = {2}, host = {3}, " +
                                        "noNormal = {4}, refresh = {5}, resolve = {6}, " +
                                        "trace = {7}, text = {8}, code = {9}, " +
                                        "result = {10}", FormatOps.InterpreterNoThrow(
                                        interpreter), FormatOps.WrapOrNull(fileName),
                                        FormatOps.WrapOrNull(flags), host, noNormal,
                                        refresh, resolve, trace, FormatOps.WrapOrNull(
                                        true, true, text), code, FormatOps.WrapOrNull(
                                        true, true, result)), typeof(PackageOps).Name,
                                        TracePriority.PathDebug);
                                }

                                if (noComplain && (code != ReturnCode.Ok))
                                    code = ReturnCode.Ok;
                            }
                        }
                    }
                    else
                    {
                        //
                        // NOTE: This is optional; therefore, success.
                        //
                        code = ReturnCode.Ok;
                    }
                }
                else
                {
                    if (!String.IsNullOrEmpty(fileName))
                    {
                        bool remoteUri = PathOps.IsRemoteUri(fileName);

                        if (remoteUri || File.Exists(fileName))
                        {
                            string newFileName = fileName;

                            if (resolve && !remoteUri)
                            {
                                //
                                // NOTE: Attempt to resolve the file name
                                //       to a fully qualified one.
                                //
                                newFileName = PathOps.ResolveFullPath(
                                    interpreter, newFileName);

                                //
                                // NOTE: Failing that, fallback to the
                                //       original file name which has
                                //       already been "validated".
                                //
                                if (String.IsNullOrEmpty(newFileName))
                                    newFileName = fileName;
                            }

                            code = interpreter.SetVariableValue( /* EXEMPT */
                                VariableFlags.None, TclVars.Directory,
                                PathOps.GetUnixPath(PathOps.GetDirectoryName(
                                newFileName)), ref result);

                            if (code == ReturnCode.Ok)
                            {
                                directory = true;

                                if (!interpreter.IsSafe() && FlagOps.HasFlags(
                                        flags, PackageIndexFlags.Safe, true))
                                {
                                    code = interpreter.EvaluateSafeFile(
                                        null, newFileName, ref result);
                                }
                                else
                                {
                                    code = interpreter.EvaluateFile(
                                        newFileName, ref result);
                                }

                                flags |= PackageIndexFlags.Evaluated;

                                if (trace)
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "IndexCallback: interpreter = {0}, " +
                                        "fileName = {1}, flags = {2}, host = {3}, " +
                                        "noNormal = {4}, refresh = {5}, resolve = {6}, " +
                                        "trace = {7}, newFileName = {8}, code = {9}, " +
                                        "result = {10}", FormatOps.InterpreterNoThrow(
                                        interpreter), FormatOps.WrapOrNull(fileName),
                                        FormatOps.WrapOrNull(flags), host, noNormal,
                                        refresh, resolve, trace, FormatOps.WrapOrNull(
                                        newFileName), code, FormatOps.WrapOrNull(
                                        true, true, result)), typeof(PackageOps).Name,
                                        TracePriority.PathDebug);
                                }

                                if (noComplain && (code != ReturnCode.Ok))
                                    code = ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "the provided \"{0}\" script file \"{1}\" is not " +
                                "a valid remote uri and does not exist locally",
                                ScriptTypes.PackageIndex, fileName);

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = String.Format(
                            "the provided \"{0}\" script is invalid",
                            ScriptTypes.PackageIndex);

                        code = ReturnCode.Error;
                    }
                }
            }
            catch (Exception e)
            {
                result = String.Format(
                    "caught exception while sourcing package index: {0}",
                    e);

                code = ReturnCode.Error;
            }
            finally
            {
                //
                // NOTE: If we set the directory variable, be sure to unset
                //       it.
                //
                if (directory)
                {
                    ReturnCode unsetCode;
                    Result unsetResult = null;

                    unsetCode = interpreter.UnsetVariable( /* EXEMPT */
                        VariableFlags.None, TclVars.Directory,
                        ref unsetResult);

                    if (unsetCode != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, unsetCode, unsetResult);
                }
            }

            return code;
        }
        #endregion
    }
}
