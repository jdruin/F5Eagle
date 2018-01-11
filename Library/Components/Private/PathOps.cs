/*
 * PathOps.cs --
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

#if WEB
using System.Collections.Specialized;
#endif

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

#if NATIVE
using System.Security;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

using System.Text;

#if WEB
using System.Web;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
#if NATIVE
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("1e358ef6-1b8f-49ac-a152-0ffece56f5af")]
    internal static class PathOps
    {
#if NATIVE
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("73db358b-e0ef-42b5-9de5-46362ad86e91")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("41367b38-86f2-41c3-b7ee-4b3374372039")]
            internal struct FILETIME
            {
                public uint dwLowDateTime;
                public uint dwHighDateTime;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("bb894e4a-17d1-4f0e-aae1-6f878ad05f2c")]
            internal struct BY_HANDLE_FILE_INFORMATION
            {
                public FileFlagsAndAttributes dwFileAttributes;
                public FILETIME ftCreationTime;
                public FILETIME ftLastAccessTime;
                public FILETIME ftLastWriteTime;
                public uint dwVolumeSerialNumber;
                public uint nFileSizeHigh;
                public uint nFileSizeLow;
                public uint nNumberOfLinks;
                public uint nFileIndexHigh;
                public uint nFileIndexLow;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            internal const uint FSCTL_GET_OBJECT_ID = 0x9009c;
            internal const uint FSCTL_CREATE_OR_GET_OBJECT_ID = 0x900c0;

            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("71d21cdf-5626-4197-9c5c-428e4717dc80")]
            internal struct FILE_OBJECTID_BUFFER
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] ObjectId;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] BirthVolumeId;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] BirthObjectId;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] DomainId;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Auto, BestFitMapping = false,
                ThrowOnUnmappableChar = true, SetLastError = true)]
            internal static extern IntPtr CreateFile(
                string fileName,
                FileAccessMask desiredAccess,
                FileShareMode shareMode,
                IntPtr securityAttributes,
                FileCreationDisposition creationDisposition,
                FileFlagsAndAttributes flagsAndAttributes,
                IntPtr templateFile
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DeviceIoControl(
                IntPtr device, uint ioControlCode, IntPtr inBuffer,
                uint inBufferSize, IntPtr outBuffer, uint outBufferSize,
                ref uint bytesReturned, IntPtr overlapped
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetFileInformationByHandle(
                IntPtr file,
                ref BY_HANDLE_FILE_INFORMATION fileInformation
            );

            ///////////////////////////////////////////////////////////////////////////////////////////

            [DllImport(DllName.Shell32,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Unicode, BestFitMapping = false,
                ThrowOnUnmappableChar = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PathIsExe(string path);
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
#if NATIVE
        //
        // NOTE: The maximum length for a module file name.
        //
        private static readonly uint UNICODE_STRING_MAX_CHARS = 32767;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly bool NoCase =
            PlatformOps.IsWindowsOperatingSystem() ?
                true : PlatformOps.IsUnixOperatingSystem() ? false : true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This list of "well-known" file extensions is used by the
        //       FileExtension class.  Any changes made here will need to
        //       be double-checked there.
        //
        internal static readonly string[] KnownExtensions = {
            ".args",   /* Arguments */
            ".bat",    /* Batch */
            ".com",    /* Command */
            ".config", /* Configuration */
            ".dll",    /* Library */
            ".exe",    /* Executable */
            ".harpy",  /* Signature */
            ".ico",    /* Icon */
            ".ini",    /* Profile */
            ".pdb",    /* Symbols */
            ".pvk",    /* PrivateKey */
            ".snk",    /* StrongNameKey */
            ".txt",    /* Text */
            ".xml"     /* Markup */
        };

        //
        // HACK: This is really a dictionary with a name suffix of "List".
        //       The rationale behind that is that it is logically a list
        //       of "well-known" file extensions that is contained inside
        //       of a physical dictionary for the sole purpose of making
        //       lookups faster.
        //
        internal static readonly PathDictionary<object> KnownExtensionList =
            new PathDictionary<object>(KnownExtensions);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WINDOWS: File names are not case-sensitive.
        //
        // UNIX: File names are case-sensitive.
        //
        internal static readonly StringComparison ComparisonType =
            PlatformOps.IsWindowsOperatingSystem() ?
                StringOps.UserNoCaseStringComparisonType :
                PlatformOps.IsUnixOperatingSystem() ?
                    StringOps.UserStringComparisonType :
                    StringOps.SystemStringComparisonType;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly char[] PathWildcardChars = {
            Characters.Asterisk,
            Characters.QuestionMark
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly char[] DirectoryChars = {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        private static readonly CharList DirectoryCharsList =
            new CharList(DirectoryChars);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static readonly string CurrentDirectory = _Path.Current;
        internal static readonly string ParentDirectory = _Path.Parent;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly char NonNativeDirectorySeparatorChar =
            PlatformOps.IsWindowsOperatingSystem() ?
                Path.AltDirectorySeparatorChar :
                Path.DirectorySeparatorChar;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly char NativeDirectorySeparatorChar =
            PlatformOps.IsWindowsOperatingSystem() ?
                Path.DirectorySeparatorChar :
                Path.AltDirectorySeparatorChar;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /*
         * The number of 100-ns intervals between the Windows system epoch
         * (1601-01-01 on the proleptic Gregorian calendar) and the Posix
         * epoch (1970-01-01).
         *
         * This value was stolen directly from the Tcl 8.6 source code.
         */

        private const ulong POSIX_EPOCH_AS_FILETIME = (ulong)116444736 * (ulong)1000000000;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This URI is only used to temporarily build an absolute
        //       URI from a relative one so the GetComponents method may
        //       be used to grab portions of the relative URI.
        //
        private static readonly Uri DefaultBaseUri = new Uri("https://www.example.com/");
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: These are the URI components to be used from the baseUri in
        //       the TryCombineUris method.
        //
        // HACK: These are purposely not read-only.
        //
        private static UriComponents BaseUriComponents = UriComponents.Scheme |
            UriComponents.UserInfo | UriComponents.Host | UriComponents.Port;

        private static UriFormat DefaultUriFormat = UriFormat.SafeUnescaped;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static void InitializeFileInformation(
            out UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation
            )
        {
            fileInformation.dwFileAttributes =
                FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE;

            fileInformation.ftCreationTime = new UnsafeNativeMethods.FILETIME();
            fileInformation.ftLastAccessTime = new UnsafeNativeMethods.FILETIME();
            fileInformation.ftLastWriteTime = new UnsafeNativeMethods.FILETIME();

            fileInformation.dwVolumeSerialNumber = 0;

            fileInformation.nFileSizeHigh = 0;
            fileInformation.nFileSizeLow = 0;

            fileInformation.nNumberOfLinks = 0;

            fileInformation.nFileIndexHigh = 0;
            fileInformation.nFileIndexLow = 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetPathInformation(
            string fileName,
            bool directory,
            ref UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation
            )
        {
            Result error = null;

            return GetPathInformation(
                fileName, directory, ref fileInformation, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetPathInformation(
            string fileName,
            bool directory,
            ref UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation,
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                IntPtr handle = IntPtr.Zero;

                if (!String.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        FileFlagsAndAttributes fileFlagsAndAttributes =
                            FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE;

                        if (directory)
                            fileFlagsAndAttributes |=
                                FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS;

                        handle = UnsafeNativeMethods.CreateFile(
                            fileName, FileAccessMask.FILE_NONE,
                            FileShareMode.FILE_SHARE_NONE, IntPtr.Zero,
                            FileCreationDisposition.OPEN_EXISTING,
                            fileFlagsAndAttributes, IntPtr.Zero);

                        if (NativeOps.IsValidHandle(handle))
                        {
                            if (UnsafeNativeMethods.GetFileInformationByHandle(
                                    handle, ref fileInformation))
                            {
                                return ReturnCode.Ok;
                            }
                        }

                        error = NativeOps.GetErrorMessage();
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        if (NativeOps.IsValidHandle(handle))
                        {
                            NativeOps.UnsafeNativeMethods.CloseHandle(handle);
                            handle = IntPtr.Zero;
                        }
                    }
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetPathInformation(
            string path,
            bool directory,
            ref StringList list,
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, directory, ref fileInformation,
                        ref error) == ReturnCode.Ok)
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "name", path,
                        "directory", directory.ToString(),
                        "attributes",
                        fileInformation.dwFileAttributes.ToString(),
                        "created",
                        ConversionOps.ToULong(
                            fileInformation.ftCreationTime.dwLowDateTime,
                            fileInformation.ftCreationTime.dwHighDateTime).ToString(),
                        "accessed",
                        ConversionOps.ToULong(
                            fileInformation.ftLastAccessTime.dwLowDateTime,
                            fileInformation.ftLastAccessTime.dwHighDateTime).ToString(),
                        "modified",
                        ConversionOps.ToULong(
                            fileInformation.ftLastWriteTime.dwLowDateTime,
                            fileInformation.ftLastWriteTime.dwHighDateTime).ToString(),
                        "vsn",
                        fileInformation.dwVolumeSerialNumber.ToString(),
                        "size",
                        ConversionOps.ToULong(
                            fileInformation.nFileSizeLow,
                            fileInformation.nFileSizeHigh).ToString(),
                        "index",
                        ConversionOps.ToULong(
                            fileInformation.nFileIndexLow,
                            fileInformation.nFileIndexHigh).ToString(),
                        "links",
                        fileInformation.nNumberOfLinks.ToString());

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSize(
            string path,
            bool directory,
            ref Result result
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, directory, ref fileInformation,
                        ref result) == ReturnCode.Ok)
                {
                    result = ConversionOps.ToULong(
                        fileInformation.nFileSizeLow,
                        fileInformation.nFileSizeHigh).ToString();

                    return ReturnCode.Ok;
                }
            }
            else
            {
                result = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This algorithm was stolen directly from the Tcl 8.6
        //       source code and modified to work in C#.
        //
        private static ulong ToTimeT(
            UnsafeNativeMethods.FILETIME fileTime
            )
        {
            ulong converted = ConversionOps.ToULong(
                fileTime.dwLowDateTime, fileTime.dwHighDateTime);

            return (converted - POSIX_EPOCH_AS_FILETIME) / 10000000;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This algorithm was stolen directly from the Tcl 8.6
        //       source code and modified to work in C#.
        //
        private static FileStatusModes GetMode(
            FileFlagsAndAttributes flagsAndAttributes,
            bool checkLinks,
            bool isExecutable,
            bool userOnly
            )
        {
            FileStatusModes mode = FileStatusModes.S_INONE;

            if (checkLinks && FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_REPARSE_POINT, true))
            {
                mode |= FileStatusModes.S_IFLNK;
            }
            else if (FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY, true))
            {
                mode |= FileStatusModes.S_IFDIR | FileStatusModes.S_IEXEC;
            }
            else
            {
                mode |= FileStatusModes.S_IFREG;
            }

            if (FlagOps.HasFlags(flagsAndAttributes,
                    FileFlagsAndAttributes.FILE_ATTRIBUTE_READONLY, true))
            {
                mode |= FileStatusModes.S_IREAD;
            }
            else
            {
                mode |= FileStatusModes.S_IREAD | FileStatusModes.S_IWRITE;
            }

            if (isExecutable)
                mode |= FileStatusModes.S_IEXEC;

            if (!userOnly)
            {
                mode |= (FileStatusModes)((int)(mode & FileStatusModes.S_IRWX) >> 3); /* group */
                mode |= (FileStatusModes)((int)(mode & FileStatusModes.S_IRWX) >> 6); /* other */
            }

            return mode;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MightBeExecutable(
            string path
            )
        {
            try
            {
                if (String.IsNullOrEmpty(path))
                    return false;

                if (PlatformOps.IsWindowsOperatingSystem() &&
                    UnsafeNativeMethods.PathIsExe(path)) /* throw */
                {
                    return true;
                }

                string extension = GetExtension(path);

                if (String.IsNullOrEmpty(extension))
                    return false;

                if (String.Equals(extension,
                        FileExtension.Command, ComparisonType) ||
                    String.Equals(extension,
                        FileExtension.Executable, ComparisonType) ||
                    String.Equals(extension,
                        FileExtension.Batch, ComparisonType))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.PathError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used directly by the [file lstat] and [file stat]
        //       sub-commands.
        //
        public static ReturnCode GetStatus(
            string path,
            bool checkLinks,
            ref StringList list,
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation;

                InitializeFileInformation(out fileInformation);

                if (GetPathInformation(
                        path, Directory.Exists(path), ref fileInformation,
                        ref error) == ReturnCode.Ok)
                {
                    int device = 0;

                    if (!String.IsNullOrEmpty(path) && Char.IsLetter(path[0]))
                        device = Char.ToLower(path[0]) - Characters.a;

                    int mode = (int)GetMode(
                        fileInformation.dwFileAttributes, checkLinks,
                        MightBeExecutable(path), false);

                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "dev",
                        device.ToString(),
                        "ino",
                        ConversionOps.ToULong(
                            fileInformation.nFileIndexLow,
                            fileInformation.nFileIndexHigh).ToString(),
                        "mode",
                        mode.ToString(),
                        "nlink",
                        fileInformation.nNumberOfLinks.ToString(),
                        "uid",
                        Value.ZeroString,
                        "gid",
                        Value.ZeroString,
                        "rdev",
                        fileInformation.dwVolumeSerialNumber.ToString(),
                        "size",
                        ConversionOps.ToULong(
                            fileInformation.nFileSizeLow,
                            fileInformation.nFileSizeHigh).ToString(),
                        "atime",
                        ToTimeT(
                            fileInformation.ftLastAccessTime).ToString(),
                        "mtime",
                        ToTimeT(
                            fileInformation.ftLastWriteTime).ToString(),
                        "ctime",
                        ToTimeT(
                            fileInformation.ftCreationTime).ToString(),
                        "type",
                        FileOps.GetFileType(path));

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetObjectId(
            string fileName,
            bool directory,
            bool create,
            ref UnsafeNativeMethods.FILE_OBJECTID_BUFFER fileObjectId,
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                IntPtr handle = IntPtr.Zero;

                if (!String.IsNullOrEmpty(fileName))
                {
                    try
                    {
                        FileFlagsAndAttributes fileFlagsAndAttributes =
                            FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE;

                        if (directory)
                            fileFlagsAndAttributes |=
                                FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS;

                        handle = UnsafeNativeMethods.CreateFile(fileName,
                            FileAccessMask.FILE_NONE,
                            FileShareMode.FILE_SHARE_READ_WRITE, IntPtr.Zero,
                            FileCreationDisposition.OPEN_EXISTING,
                            fileFlagsAndAttributes, IntPtr.Zero);

                        if (NativeOps.IsValidHandle(handle))
                        {
                            IntPtr outBuffer = IntPtr.Zero;

                            try
                            {
                                int outBufferSize = Marshal.SizeOf(typeof(
                                    UnsafeNativeMethods.FILE_OBJECTID_BUFFER));

                                outBuffer = Marshal.AllocCoTaskMem(
                                    outBufferSize);

                                if (outBuffer != IntPtr.Zero)
                                {
                                    uint bytesReturned = 0;

                                    if (UnsafeNativeMethods.DeviceIoControl(
                                            handle, create ?
                                                UnsafeNativeMethods.FSCTL_CREATE_OR_GET_OBJECT_ID :
                                                UnsafeNativeMethods.FSCTL_GET_OBJECT_ID,
                                            IntPtr.Zero, 0, outBuffer, (uint)outBufferSize,
                                            ref bytesReturned, IntPtr.Zero))
                                    {
                                        fileObjectId = (UnsafeNativeMethods.FILE_OBJECTID_BUFFER)
                                            Marshal.PtrToStructure(outBuffer,
                                                typeof(UnsafeNativeMethods.FILE_OBJECTID_BUFFER));

                                        return ReturnCode.Ok;
                                    }
                                }
                                else
                                {
                                    error = "out of memory";
                                }
                            }
                            finally
                            {
                                if (outBuffer != IntPtr.Zero)
                                {
                                    Marshal.FreeCoTaskMem(outBuffer);
                                    outBuffer = IntPtr.Zero;
                                }
                            }
                        }

                        error = NativeOps.GetErrorMessage();
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        if (NativeOps.IsValidHandle(handle))
                        {
                            NativeOps.UnsafeNativeMethods.CloseHandle(handle);
                            handle = IntPtr.Zero;
                        }
                    }
                }
                else
                {
                    error = "invalid file name";
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetObjectId(
            string path,
            bool directory,
            bool create,
            ref StringList list,
            ref Result error
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                UnsafeNativeMethods.FILE_OBJECTID_BUFFER fileObjectId =
                    new UnsafeNativeMethods.FILE_OBJECTID_BUFFER();

                if (GetObjectId(
                        path, directory, create,
                        ref fileObjectId,
                        ref error) == ReturnCode.Ok)
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(
                        "name", path,
                        "directory",
                        directory.ToString(),
                        "create",
                        create.ToString(),
                        "objectId",
                        ArrayOps.ToHexadecimalString(fileObjectId.ObjectId),
                        "birthVolumeId",
                        ArrayOps.ToHexadecimalString(fileObjectId.BirthVolumeId),
                        "birthObjectId",
                        ArrayOps.ToHexadecimalString(fileObjectId.BirthObjectId),
                        "domainId",
                        ArrayOps.ToHexadecimalString(fileObjectId.DomainId));

                    return ReturnCode.Ok;
                }
            }
            else
            {
                error = "not supported on this operating system";
            }

            return ReturnCode.Error;
        }
#endif
        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasPathWildcard(
            string value
            )
        {
            return (value != null) && (PathWildcardChars != null) &&
                (value.IndexOfAny(PathWildcardChars) != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CleanPath(
            string path,
            bool full,
            string invalidChar
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: Just remove any surrounding double quotes.
                //
                result = result.Trim(Characters.QuotationMark);

                if (full)
                {
                    //
                    // NOTE: Full cleaning required, remove all
                    //       invalid path characters.
                    //
                    StringBuilder builder =
                        StringOps.NewStringBuilder(result);

                    foreach (char character in Path.GetInvalidPathChars())
                    {
                        if (!String.IsNullOrEmpty(invalidChar))
                            builder.Replace(character, invalidChar[0]);
                        else
                            builder.Replace(character.ToString(), null);
                    }

                    result = builder.ToString();
                }

            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WEB && !MONO && (NET_20_SP2 || NET_40)
        //
        // HACK: Using the "HttpRuntime.UsingIntegratedPipeline" property when
        //       running on Mono seems to cause serious problems (I guess they
        //       cannot just return false).  Apparently, even referring to this
        //       method causes Mono to crash; therefore, it has been moved
        //       to a method by itself (which seems to get around the problem).
        //
        private static bool HttpRuntimeUsingIntegratedPipeline()
        {
            return HttpRuntime.UsingIntegratedPipeline;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetBinaryPath(
            bool full
            )
        {
            string result = null;

            try
            {
#if WEB
                //
                // NOTE: Are we running in a web application?
                //
                HttpContext current = HttpContext.Current;

                if (current != null)
                {
                    TraceOps.DebugTrace(
                        "GetBinaryPath: found HTTP context",
                        typeof(PathOps).Name,
                        TracePriority.StartupDebug);

                    HttpServerUtility server = current.Server;

                    if (server != null)
                    {
                        TraceOps.DebugTrace(
                            "GetBinaryPath: found HTTP server utility",
                            typeof(PathOps).Name,
                            TracePriority.StartupDebug);

                        string path = null;

#if !MONO && (NET_20_SP2 || NET_40)
                        //
                        // HACK: Using "HttpRuntime.UsingIntegratedPipeline" on
                        //       Mono seems to cause serious problems (I guess
                        //       they cannot just return false).  Apparently,
                        //       even referring to this method causes Mono to
                        //       crash; therefore, the check has been moved to
                        //       a method by itself (which seems to get around
                        //       the problem).
                        //
                        if (!CommonOps.Runtime.IsMono() &&
                            HttpRuntimeUsingIntegratedPipeline())
                        {
                            TraceOps.DebugTrace(
                                "GetBinaryPath: detected IIS integrated-pipeline mode",
                                typeof(PathOps).Name, TracePriority.StartupDebug);

                            //
                            // NOTE: Get the root of the web application (for
                            //       use in IIS7+ integrated mode).
                            //
                            path = HttpRuntime.AppDomainAppVirtualPath;
                        }
                        else
#endif
                        {
                            TraceOps.DebugTrace(
                                "GetBinaryPath: detected IIS classic mode",
                                typeof(PathOps).Name, TracePriority.StartupDebug);

                            //
                            // NOTE: Grab and verify the HTTP request object.
                            //
                            HttpRequest request = current.Request;

                            if (request != null)
                            {
                                TraceOps.DebugTrace(
                                    "GetBinaryPath: found HTTP request",
                                    typeof(PathOps).Name, TracePriority.StartupDebug);

                                //
                                // NOTE: Get the root of the web application.
                                //
                                path = request.ApplicationPath;
                            }
                            else
                            {
                                TraceOps.DebugTrace(
                                    "GetBinaryPath: no HTTP request",
                                    typeof(PathOps).Name, TracePriority.StartupError);
                            }
                        }

                        //
                        // NOTE: Map the application path to the local file
                        //       system path and append the "bin" folder, which
                        //       should always be there according to MSDN.
                        //
                        if (path != null)
                        {
                            result = CombinePath(
                                null, server.MapPath(path), TclVars.BinPath);
                        }
                        else
                        {
                            TraceOps.DebugTrace(
                                "GetBinaryPath: no path from HTTP context",
                                typeof(PathOps).Name, TracePriority.StartupError);
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "GetBinaryPath: no HTTP server utility",
                            typeof(PathOps).Name, TracePriority.StartupError);
                    }
                }
                else
#endif
                {
                    TraceOps.DebugTrace(
                        "GetBinaryPath: no HTTP context",
                        typeof(PathOps).Name, TracePriority.StartupDebug);

                    //
                    // NOTE: Use the base directory of the current application
                    //       domain.
                    //
                    AppDomain appDomain = AppDomainOps.GetCurrent();

                    if (AppDomainOps.IsDefault(appDomain))
                    {
                        TraceOps.DebugTrace(
                            "GetBinaryPath: default application domain",
                            typeof(PathOps).Name, TracePriority.StartupDebug);

                        result = appDomain.BaseDirectory;
                    }
                    else
                    {
                        //
                        // HACK: This is an isolated AppDomain.  There is
                        //       [probably?] no entry assembly available and
                        //       the AppDomain base directory is not reliable
                        //       for the purpose of loading packages;
                        //       therefore, just use the directory of the
                        //       current (Eagle) assembly.
                        //
                        TraceOps.DebugTrace(
                            "GetBinaryPath: non-default application domain",
                            typeof(PathOps).Name, TracePriority.StartupDebug);

                        result = GlobalState.GetAssemblyPath();
                    }
                }

                //
                // NOTE: Remove trailing directory separator characters, if
                //       necessary.
                //
                if (result != null)
                    result = TrimEndOfPath(result, null);

                //
                // NOTE: Finally, if requested, fully resolve to an absolute
                //       path if we were requested to do so.
                //
                if (full && (result != null))
                    result = Path.GetFullPath(result);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PathOps).Name,
                    TracePriority.StartupError);
            }

            TraceOps.DebugTrace(String.Format(
                "GetBinaryPath: result = {0}",
                FormatOps.WrapOrNull(result)),
                typeof(PathOps).Name,
                TracePriority.StartupDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetProcessMainModuleFileName(
            bool full
            )
        {
            return GetProcessMainModuleFileName(
                Process.GetCurrentProcess(), full);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetProcessMainModuleFileName(
            Process process,
            bool full
            )
        {
            try
            {
                if (process != null)
                {
                    ProcessModule module = process.MainModule;

                    if (module != null)
                    {
                        return full ?
                            Path.GetFullPath(module.FileName) :
                            module.FileName;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        public static string GetNativeModuleFileName(
            IntPtr module,
            ref Result error
            )
        {
            IntPtr outBuffer = IntPtr.Zero;

            try
            {
                uint outBufferSize = UNICODE_STRING_MAX_CHARS;

                outBuffer = Marshal.AllocCoTaskMem(
                    (int)(outBufferSize + 1) * sizeof(char));

                uint result = NativeOps.GetModuleFileName(
                    module, outBuffer, outBufferSize);

                //
                // NOTE: If the result is zero, the function
                //       failed.
                //
                if (result > 0)
                {
                    //
                    // NOTE: Set the module file name to the
                    //       contents of the output buffer, up
                    //       to the returned length (which may
                    //       have been truncated).
                    //
                    return Marshal.PtrToStringAuto(
                        outBuffer, (int)result);
                }
                else
                {
                    //
                    // NOTE: Failure, cannot resolve the module
                    //       file name.
                    //
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "cannot resolve module file name, " +
                        "GetModuleFileName({1}) failed with " +
                        "error {0}: {2}", lastError, module,
                        NativeOps.GetDynamicLoadingError(lastError));
                }

                //
                // NOTE: If we reach this point, fail.
                //
                return null;
            }
            finally
            {
                if (outBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(outBuffer);
                    outBuffer = IntPtr.Zero;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetNativeExecutableName()
        {
            Result error = null;

            return GetNativeModuleFileName(IntPtr.Zero, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetExecutableName()
        {
            return GetExecutableName(true, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetExecutableName(
            bool fallback,
            bool full
            )
        {
            return GetExecutableName(
                Process.GetCurrentProcess(), fallback, full);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetExecutableName(
            Process process,
            bool fallback,
            bool full
            )
        {
            //
            // NOTE: We need the name of the Eagle executable, not the Mono
            //       executable; however, if we are in a non-default AppDomain,
            //       the entry assembly may be totally meaningless.
            //
            if (!CommonOps.Runtime.IsMono())
            {
                return GetProcessMainModuleFileName(process, full);
            }
            else
            {
                string location = GlobalState.GetEntryAssemblyLocation();

                if (location != null)
                    return location;

                if (fallback)
                    return GetProcessMainModuleFileName(process, full);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: At some point, possibly provide an environment variable that,
        //       when set [to anything], causes this method to always return
        //       null.
        //
        private static string GetBuildConfiguration( /* MAY RETURN NULL */
            Assembly assembly /* OPTIONAL: May be null. */
            )
        {
            return AttributeOps.GetAssemblyConfiguration(assembly);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: At some point, it might be nice to support customizing (or
        //       skipping) the various behaviors of this method.
        //
        public static string GetBasePath( /* MAY RETURN NULL */
            Assembly assembly, /* OPTIONAL: May be null. */
            string path
            )
        {
            //
            // NOTE: Start with their entire path, verbatim.
            //
            string result = path;

            //
            // NOTE: Garbage in, garbage out.
            //
            if (String.IsNullOrEmpty(result))
                return result;

            //
            // HACK: If we are running in the special "build tasks" output
            //       directory (i.e. during the build process), go up one
            //       level now.  This is necessary to support loading the
            //       build tasks from a directory other than the primary
            //       output directory, because that would prevent us from
            //       modifying the built binaries, due to assembly locking
            //       by MSBuild.
            //
            if (IsEqualFileName(
                    Path.GetFileName(result), _Path.BuildTasks))
            {
                result = Path.GetDirectoryName(result);
            }

            //
            // BUGBUG: This will not always do the right thing because it is
            //         unconditional.  Need to make this check smarter.
            //         *UPDATE* Actually, now this is conditional; however,
            //         it still may not be smart enough.
            //
            // HACK: Go up one level to get to the parent directory of the
            //       inner "bin" and "lib" directories.  We never want to do
            //       this if the path is the root directory of the drive we
            //       are on.  This is not really optimal because it assumes
            //       the specified path must end with a "bin" directory and
            //       thus also [typically] assumes that the assembly for the
            //       core library itself must always reside within a "bin"
            //       directory to function properly when deployed.
            //
            if (!IsEqualFileName(Path.GetPathRoot(result), result))
            {
                result = Path.GetDirectoryName(result);
            }

            //
            // NOTE: Get the name of the directory at this level.
            //
            string directory = Path.GetFileName(result);

            //
            // NOTE: Get the current build configuration for this assembly.
            //
            string configuration = GetBuildConfiguration(assembly);

            //
            // HACK: If it looks like we are running from the build directory
            //       for this configuration, go up another level to compensate.
            //       If the assembly configuration is null or empty, skip this
            //       step.  This is not optimal because it assumes a directory
            //       name starting with "Debug" or "Release" cannot be the base
            //       directory.
            //
            if (/* DebugOps.IsAttached() || */ ((directory != null) &&
                !String.IsNullOrEmpty(configuration) &&
                directory.StartsWith(configuration, ComparisonType)))
            {
                result = Path.GetDirectoryName(result);
            }

            //
            // HACK: We want the parent directory of the outer "bin" directory
            //       (which will only be in the result string at this point if
            //       we are running from the build output directory), if any.
            //       This is not optimal because it assumes a directory named
            //       "bin" cannot be the base directory.
            //
            if (/* DebugOps.IsAttached() || */
                IsEqualFileName(Path.GetFileName(result), TclVars.BinPath))
            {
                result = Path.GetDirectoryName(result);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TrimEndOfPath(
            string path,
            char? separator
            )
        {
            //
            // NOTE: If the original path string is null or empty, just return
            //       it as we cannot do anything else meaningful with it.
            //
            if (String.IsNullOrEmpty(path))
                return path;

            //
            // BUGFIX: Whatever the only character may be, we cannot reasonably
            //         be expected to remove it (i.e. even if it is a directory
            //         separator).
            //
            int length = path.Length;

            if (length == 1)
                return path;

            //
            // NOTE: If the last character is not a directory separator then
            //       there is no trimming to be done.
            //
            if (!IsDirectoryChar(path[length - 1]))
                return path;

            //
            // NOTE: Figure out the suffix, if any, we may need to append to
            //       the result.
            //
            string suffix = String.Empty;

            if (separator != null)
                suffix = separator.ToString();

            //
            // NOTE: Trim all trailing directory separator characters from the
            //       end of the path string and append the separator character
            //       provided by the caller.
            //
            return path.TrimEnd(DirectoryChars) + suffix;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringList SplitPath(
            bool? unix,
            string path
            )
        {
            if (path == null)
                return null;

            path = path.Trim();

            if (path.Length == 0)
                return new StringList();

            char separator;

            if (unix != null)
            {
                separator = (bool)unix ?
                    Path.AltDirectorySeparatorChar :
                    Path.DirectorySeparatorChar;
            }
            else
            {
                separator = NativeDirectorySeparatorChar;
                GetFirstDirectorySeparator(path, ref separator);
            }

            StringList result = new StringList();
            string[] parts = path.Split(DirectoryChars);

            if ((parts != null) && (parts.Length > 0))
            {
                for (int index = 0; index < parts.Length; index++)
                {
                    string part = parts[index];

                    if (part == null)
                        continue;

                    part = part.Trim();

                    if (part.Length == 0)
                    {
                        if (result.Count == 0)
                            result.Add(separator.ToString());

                        continue;
                    }

                    result.Add(part);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CombinePath(
            bool? unix,
            IList list
            )
        {
            StringBuilder builder = StringOps.NewStringBuilder();

            if (list != null)
            {
                char separator;

                if (unix != null)
                {
                    separator = (bool)unix ?
                        Path.AltDirectorySeparatorChar :
                        Path.DirectorySeparatorChar;
                }
                else
                {
                    separator = NativeDirectorySeparatorChar;
                    GetFirstDirectorySeparator(list, ref separator);
                }

                for (int index = 0; index < list.Count; index++)
                {
                    string path = StringOps.GetStringFromObject(list[index]);

                    //
                    // NOTE: Skip all null/empty path parts.
                    //
                    if (String.IsNullOrEmpty(path))
                        continue;

                    //
                    // HACK: Remove surrounding whitespace.
                    //
                    string trimPath = path.Trim();

                    if (trimPath.Length > 0)
                    {
                        //
                        // NOTE: Have we already handled the first part of
                        //       the path?
                        //
                        if (builder.Length > 0)
                        {
                            if (!IsDirectoryChar(builder[builder.Length - 1]))
                                builder.Append(separator);

                            builder.Append(trimPath.Trim(DirectoryChars));
                        }
                        else if ((trimPath.Length == 1) &&
                            IsDirectoryChar(trimPath[0]))
                        {
                            //
                            // BUGFIX: If the first part of the path is just
                            //         one separator character, append the
                            //         selected separator character instead.
                            //
                            builder.Append(separator);
                        }
                        else
                        {
                            string trimPath2 = TrimEndOfPath(trimPath, null);

                            if (trimPath2.Length > 0)
                            {
                                //
                                // BUGFIX: *MONO* Do not trim any separator
                                //         characters from the start of the
                                //         string.
                                //
                                builder.Append(trimPath2);
                            }
                            else
                            {
                                //
                                // BUGFIX: *MONO* If trimming the [first]
                                //         non-empty part of the path ends
                                //         removing all of its characters,
                                //         append the selected separator
                                //         character instead.
                                //
                                builder.Append(separator);
                            }
                        }
                    }
                }
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static StringPairList GetPathList(
            IEnumerable<string> names
            )
        {
            StringPairList list = new StringPairList();

            if (names != null)
            {
                foreach (string name in names)
                {
                    if (String.IsNullOrEmpty(name))
                        continue;

                    string path = CommonOps.Environment.GetVariable(name);

                    if (!String.IsNullOrEmpty(path))
                    {
                        string[] values = path.Split(Path.PathSeparator);

                        if (values == null)
                            continue;

                        foreach (string value in values)
                        {
                            if (String.IsNullOrEmpty(value))
                                continue;

                            list.Add(name, value);
                        }
                    }
                }
            }

            return list;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string CombinePath(
            bool? unix,
            params string[] paths
            )
        {
            return CombinePath(unix, (IList)paths);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsDirectoryChar(
            char character
            )
        {
            if (DirectoryCharsList == null)
                return false;

            return DirectoryCharsList.Contains(character);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetHashCode(
            string path
            )
        {
            if (path != null)
            {
                string newPath = GetNativePath(path);

                if (NoCase)
                    newPath = newPath.ToLower();

                return newPath.GetHashCode();
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasDirectory(
            string path
            )
        {
            int index = Index.Invalid;

            return StartsWithDirectory(path, ref index);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetExtension(
            string path
            )
        {
            try
            {
                return Path.GetExtension(path);
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasExtension(
            string path
            )
        {
            string extension = null;

            return HasExtension(path, ref extension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HasExtension(
            string path,
            ref string extension
            )
        {
            try
            {
                extension = GetExtension(path);

                return !String.IsNullOrEmpty(extension);
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool HasKnownExtension(
            string path
            )
        {
            string extension = null;

            if (!HasExtension(path, ref extension))
                return false;

            if (extension == null)
                return false;

            if (KnownExtensionList == null)
                return false;

            return KnownExtensionList.ContainsKey(extension);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool StartsWithDirectory(
            string path,
            ref int index
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            if (DirectoryChars == null)
                return false;

            index = path.IndexOfAny(DirectoryChars);
            return (index != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool EndsWithDirectory(
            string path,
            ref int index
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            if (DirectoryChars == null)
                return false;

            index = path.LastIndexOfAny(DirectoryChars);
            return (index != Index.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddPathToDictionary(
            string path,
            ref PathDictionary<object> dictionary
            )
        {
            if (String.IsNullOrEmpty(path))
                return;

            if (dictionary == null)
                dictionary = new PathDictionary<object>();

            if (dictionary.ContainsKey(path))
                return;

            dictionary.Add(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddPathsToDictionary(
            IEnumerable<string> paths,
            ref PathDictionary<object> dictionary
            )
        {
            if (paths == null)
                return;

            foreach (string path in paths)
                AddPathToDictionary(path, ref dictionary);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetMappedPaths(
            Interpreter interpreter,
            string path,
            ref PathDictionary<object> dictionary
            )
        {
            //
            // NOTE: This method requires a valid interpreter context.
            //
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter is either disposed or it has not [yet]
                //       fully completed the PreSetup() method, we cannot use it.
                //       In that case, just return null.
                //
                if (interpreter.Disposed || !interpreter.InternalPreSetup)
                    return false;

                //
                // NOTE: *SECURITY* Currently, all path mappings are always ignored
                //       for safe interpreters.
                //
                if (interpreter.IsSafe())
                    return false;

                //
                // NOTE: Forbid any attempt to use a null or empty path string.
                //
                if (String.IsNullOrEmpty(path))
                    return false;

                try
                {
                    StringList list = new StringList();

                    foreach (string index in new string[] {
                        path, Path.GetDirectoryName(path),
                        Path.GetFileName(path) })
                    {
                        if (index == null)
                            continue;

                        Result value = null;

                        if (interpreter.GetVariableValue2(
                                VariableFlags.GlobalOnly, Vars.Paths,
                                index, ref value) == ReturnCode.Ok)
                        {
                            if (String.IsNullOrEmpty(value))
                                continue;

                            list.Add(value);
                        }
                    }

                    if (list.Count > 0)
                        AddPathsToDictionary(list, ref dictionary);

                    return true;
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetAutoSourcePaths(
            Interpreter interpreter,
            ref PathDictionary<object> dictionary
            )
        {
            //
            // NOTE: This method requires a valid interpreter context.
            //
            if (interpreter == null)
                return false;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If the interpreter is either disposed or it has not [yet]
                //       fully completed the PreSetup() method, we cannot use it.
                //       In that case, just return null.
                //
                if (interpreter.Disposed || !interpreter.InternalPreSetup)
                    return false;

                //
                // NOTE: *SECURITY* Currently, the auto-source-path is always
                //       ignored for safe interpreters.
                //
                if (interpreter.IsSafe())
                    return false;

                try
                {
                    Result value = null;

                    if (interpreter.GetVariableValue(
                            VariableFlags.GlobalOnly, TclVars.AutoSourcePath,
                            ref value) == ReturnCode.Ok)
                    {
                        StringList list = null;

                        if (!String.IsNullOrEmpty(value) && Parser.SplitList(
                                interpreter, value, 0, Length.Invalid,
                                false, ref list) == ReturnCode.Ok)
                        {
                            if (list.Count > 0)
                                AddPathsToDictionary(list, ref dictionary);

                            return true;
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractFileSearchFlags(
            FileSearchFlags flags,
            out bool specificPath,
            out bool mapped,
            out bool autoSourcePath,
            out bool current,
            out bool user,
            out bool externals,
            out bool application,
            out bool applicationBase,
            out bool vendor,
            out bool strict,
            out bool unix,
            out bool directoryLocation,
            out bool fileLocation
            )
        {
            specificPath = FlagOps.HasFlags(
                flags, FileSearchFlags.SpecificPath, true);

            mapped = FlagOps.HasFlags(
                flags, FileSearchFlags.Mapped, true);

            autoSourcePath = FlagOps.HasFlags(
                flags, FileSearchFlags.AutoSourcePath, true);

            current = FlagOps.HasFlags(
                flags, FileSearchFlags.Current, true);

            user = FlagOps.HasFlags(
                flags, FileSearchFlags.User, true);

            externals = FlagOps.HasFlags(
                flags, FileSearchFlags.Externals, true);

            application = FlagOps.HasFlags(
                flags, FileSearchFlags.Application, true);

            applicationBase = FlagOps.HasFlags(
                flags, FileSearchFlags.ApplicationBase, true);

            vendor = FlagOps.HasFlags(
                flags, FileSearchFlags.Vendor, true);

            strict = FlagOps.HasFlags(
                flags, FileSearchFlags.Strict, true);

            unix = FlagOps.HasFlags(
                flags, FileSearchFlags.Unix, true);

            directoryLocation = FlagOps.HasFlags(
                flags, FileSearchFlags.DirectoryLocation, true);

            fileLocation = FlagOps.HasFlags(
                flags, FileSearchFlags.FileLocation, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Search(
            Interpreter interpreter,
            string path,
            FileSearchFlags flags
            )
        {
            int count = 0;

            return Search(interpreter, path, flags, ref count);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string Search(
            Interpreter interpreter, /* optional interpreter context to use. */
            string path,             /* [qualified?] file name to search for. */
            FileSearchFlags flags,   /* flags that control search behavior. */
            ref int count            /* in, out: how many names were checked? */
            )
        {
            bool specificPath;
            bool mapped;
            bool autoSourcePath;
            bool current;
            bool user;
            bool externals;
            bool application;
            bool applicationBase;
            bool vendor;
            bool strict;
            bool unix;
            bool directoryLocation;
            bool fileLocation;

            ExtractFileSearchFlags(flags,
                out specificPath, out mapped, out autoSourcePath, out current,
                out user, out externals, out application, out applicationBase,
                out vendor, out strict, out unix, out directoryLocation,
                out fileLocation);

            try
            {
                if (!String.IsNullOrEmpty(path))
                {
                    if (specificPath ||
                        mapped || autoSourcePath || current || user || application)
                    {
                        PathDictionary<object> dictionary = null;

                        //
                        // TODO: Should the IsPathRooted check always be done
                        //       here?  Maybe there should be a flag to disable
                        //       it?
                        //
                        if (specificPath && Path.IsPathRooted(path))
                            AddPathToDictionary(path, ref dictionary);

                        if (mapped)
                            /* IGNORED */
                            GetMappedPaths(interpreter, path, ref dictionary);

                        if (autoSourcePath)
                            /* IGNORED */
                            GetAutoSourcePaths(interpreter, ref dictionary);

                        if (current)
                        {
                            AddPathToDictionary(Directory.GetCurrentDirectory(),
                                ref dictionary);
                        }

                        if (user)
                        {
                            AddPathToDictionary(
                                GetHomeDirectory(), ref dictionary);

                            AddPathToDictionary(Environment.GetFolderPath(
                                Environment.SpecialFolder.MyDocuments),
                                ref dictionary);

#if NET_40
                            AddPathToDictionary(Environment.GetFolderPath(
                                Environment.SpecialFolder.UserProfile),
                                ref dictionary);

                            AddPathToDictionary(Environment.GetFolderPath(
                                Environment.SpecialFolder.CommonDocuments),
                                ref dictionary);
#else
                            AddPathToDictionary(GetUserProfileDirectory(),
                                ref dictionary);
#endif
                        }

                        if (externals)
                        {
                            AddPathToDictionary(GlobalState.GetExternalsPath(),
                                ref dictionary);
                        }

                        if (application)
                        {
                            AddPathToDictionary(Environment.GetFolderPath(
                                Environment.SpecialFolder.LocalApplicationData),
                                ref dictionary);

                            AddPathToDictionary(Environment.GetFolderPath(
                                Environment.SpecialFolder.ApplicationData),
                                ref dictionary);

                            AddPathToDictionary(Environment.GetFolderPath(
                                Environment.SpecialFolder.CommonApplicationData),
                                ref dictionary);

                            AddPathToDictionary(GlobalState.GetBinaryPath(),
                                ref dictionary);

                            AddPathToDictionary(GlobalState.GetAssemblyPath(),
                                ref dictionary);
                        }

                        if (user || application)
                        {
                            AddPathToDictionary(GetUserProfileDirectory(),
                                ref dictionary);
                        }

                        if (applicationBase)
                        {
                            AddPathToDictionary(AssemblyOps.GetAnchorPath(),
                                ref dictionary);

                            AddPathToDictionary(GlobalState.GetAppDomainBaseDirectory(),
                                ref dictionary);

                            AddPathToDictionary(GlobalState.GetBasePath(),
                                ref dictionary);

                            AddPathToDictionary(GlobalState.GetRawBasePath(),
                                ref dictionary);
                        }

                        if (dictionary != null)
                        {
                            //
                            // NOTE: Grab the vendor path in advance as it is used
                            //       for each loop iteration.
                            //
                            string vendorPath = vendor ? GetVendorPath() : null;

                            foreach (KeyValuePair<string, object> pair in dictionary)
                            {
                                //
                                // NOTE: Grab the location from the current pair.
                                //
                                string location = pair.Key;

                                //
                                // NOTE: Skip locations that are null or an empty
                                //       string.
                                //
                                if (String.IsNullOrEmpty(location))
                                    continue;

                                //
                                // NOTE: If the location entry is actually a file,
                                //       return it now if we are allowed to do so.
                                //
                                if (fileLocation)
                                {
                                    if (File.Exists(location))
                                    {
                                        count++;
                                        return GetNativePath(location);
                                    }

                                    count++;
                                }

                                //
                                // NOTE: If the location entry is not allowed to
                                //       be a directory -OR- the directory does
                                //       not exist, skip this location entry.
                                //
                                if (!directoryLocation ||
                                    !Directory.Exists(location))
                                {
                                    continue;
                                }

                                string fileName;
                                string fileNameOnly = Path.GetFileName(path);

                                if (!String.IsNullOrEmpty(vendorPath))
                                {
                                    fileName = CombinePath(unix, location,
                                        vendorPath, fileNameOnly);

                                    if (File.Exists(fileName))
                                    {
                                        count++;
                                        return GetNativePath(fileName);
                                    }

                                    count++;
                                }

                                fileName = CombinePath(unix, location,
                                    fileNameOnly);

                                if (File.Exists(fileName))
                                {
                                    count++;
                                    return GetNativePath(fileName);
                                }

                                count++;
                            }
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            //
            // NOTE: At this point, nothing was found.
            //
            if (strict)
            {
                //
                // NOTE: If we get here, we found nothing and that is
                //       considered an error (in strict mode).
                //
                return null;
            }
            else
            {
                //
                // NOTE: Otherwise, just return whatever input value
                //       we received.
                //
                return path;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetUserDirectory(
            bool strict
            )
        {
            string[] directories = {
                GetHomeDirectory(),
                GetUserProfileDirectory()
            };

            foreach (string directory in directories)
            {
                if (!String.IsNullOrEmpty(directory) &&
                    Directory.Exists(directory))
                {
                    return directory;
                }
            }

            //
            // NOTE: If we get here, we found nothing and that is
            //       considered an error.
            //
            return strict ? null : GetHomeDirectory();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetUserProfileDirectory()
        {
            return GlobalConfiguration.GetValue(
                EnvVars.UserProfile, ConfigurationFlags.PathOps);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetVendorPath()
        {
            //
            // NOTE: Return the vendor path "offset" to be appended to the
            //       search directory when looking for files.
            //
            return GlobalConfiguration.GetValue(
                EnvVars.VendorPath, ConfigurationFlags.PathOps);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetHomeDirectory()
        {
            return GlobalConfiguration.GetValue(
                EnvVars.Home, ConfigurationFlags.PathOps);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SetHomeDirectory(
            string value
            )
        {
            GlobalConfiguration.SetValue(
                EnvVars.Home, value, ConfigurationFlags.PathOps);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScrubPath(
            string basePath,
            string path
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                //
                // WINDOWS: File names are not case-sensitive.
                //
                if (!String.IsNullOrEmpty(basePath))
                {
                    //
                    // BUGFIX: *WINDOWS* Make sure both paths have
                    //         the same separators.
                    //
                    string path1 = GetNativePath(path);
                    string path2 = GetNativePath(basePath);

                    //
                    // NOTE: See if the specified path starts with
                    //       the base path.
                    //
                    if (String.Equals(path1, path2, ComparisonType))
                    {
                        //
                        // NOTE: The specified path is exactly the
                        //       same as the base path; just return
                        //       the "base directory" token.
                        //
                        return Vars.BaseDirectory;
                    }
                    else
                    {
                        //
                        // NOTE: Get the native directory separator
                        //       character.
                        //
                        char separator = NativeDirectorySeparatorChar;

                        if (path1.StartsWith(
                                path2 + separator, ComparisonType))
                        {
                            //
                            // NOTE: Replace the base path with a
                            //       "base directory" token.
                            //
                            return Vars.BaseDirectory +
                                path1.Substring(path2.Length);
                        }
                    }
                }

                return Path.GetFileName(path);
            }

            return path;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsUri(
            string value
            )
        {
            return IsUri(value, UriKind.Absolute);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsUri(
            string value,
            UriKind uriKind
            )
        {
            if (!String.IsNullOrEmpty(value))
            {
                Uri uri = null;
                UriKind localUriKind = UriKind.RelativeOrAbsolute;

                if (TryCreateUri(value, ref uri, ref localUriKind))
                    return (uriKind == UriKind.RelativeOrAbsolute) || (localUriKind == uriKind);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CombinePathsForUri(
            bool normalize,
            params string[] paths
            )
        {
            if (paths == null) // NOTE: Impossible?
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();

            foreach (string path in paths)
            {
                if (path == null)
                    continue;

                string localPath = path.Trim(DirectoryChars);

                if (!String.IsNullOrEmpty(localPath))
                {
                    if (builder.Length > 0)
                    {
                        //
                        // NOTE: URI path segments always use the Unix
                        //       path separator (i.e. forward slash).
                        //
                        builder.Append(Path.AltDirectorySeparatorChar);
                    }

                    if (normalize)
                    {
                        localPath = localPath.Replace(
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar);
                    }

                    builder.Append(localPath);
                }
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if WEB
        private static NameValueCollection ParseQueryString(
            string query,
            Encoding encoding
            )
        {
            if (query == null)
                return null;

            return (encoding != null) ?
                HttpUtility.ParseQueryString(query, encoding) :
                HttpUtility.ParseQueryString(query);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string UrlEncode(
            string value,
            Encoding encoding
            )
        {
            if (value == null)
                return null;

            return (encoding != null) ?
                HttpUtility.UrlEncode(value, encoding) :
                HttpUtility.UrlEncode(value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string CombineQueriesForUri(
            string query1,
            string query2,
            Encoding encoding
            )
        {
            NameValueCollection collection1 = ParseQueryString(
                query1, encoding);

            NameValueCollection collection2 = ParseQueryString(
                query2, encoding);

            if ((collection1 == null) && (collection2 == null))
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();
            NameValueCollection[] collections = { collection1, collection2 };

            foreach (NameValueCollection collection in collections)
            {
                if (collection == null)
                    continue;

                foreach (string key in collection.AllKeys)
                {
                    string[] values = collection.GetValues(key);

                    foreach (string value in values)
                    {
                        if (builder.Length > 0)
                            builder.Append(Characters.Ampersand);

                        builder.Append(UrlEncode(key, encoding));
                        builder.Append(Characters.EqualSign);
                        builder.Append(UrlEncode(value, encoding));
                    }
                }
            }

            return builder.ToString();
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Uri TryCombineUris(
            Uri baseUri,              /* in */
            string relativeUri,       /* in */
            Encoding encoding,        /* in */
            UriComponents components, /* in */
            UriFormat format,         /* in */
            UriFlags flags,           /* in */
            ref Result error          /* out */
            )
        {
            if (baseUri == null)
            {
                error = "invalid base uri";
                return null;
            }

            if (!baseUri.IsAbsoluteUri)
            {
                error = "uri is not absolute";
                return null;
            }

            //
            // NOTE: If no relative URI, just return the base URI as there
            //       is nothing else to combine it with.
            //
            if (String.IsNullOrEmpty(relativeUri))
                return baseUri;

            //
            // NOTE: Try to create an actual URI from the string of the
            //       relative URI.  If this fails, bail out now.
            //
            Uri localRelativeUri;

            if (!Uri.TryCreate(
                    DefaultBaseUri, relativeUri, out localRelativeUri))
            {
                error = String.Format(
                    "unable to create relative uri {0}",
                    FormatOps.WrapOrNull(relativeUri));

                return null;
            }

            //
            // NOTE: Use the URI format specified by the caller unless
            //       the right flag is not set.  In that case, use the
            //       default URI format.
            //
            if (!FlagOps.HasFlags(flags, UriFlags.UseFormat, true))
                format = DefaultUriFormat;

            //
            // NOTE: Grab components of the base URI that were requested
            //       by the caller, being careful to mask off those that
            //       are not applicable to the base portion of the URI.
            //
            string localBaseComponents = null;

            if (FlagOps.HasFlags(components, BaseUriComponents, false))
            {
                localBaseComponents = baseUri.GetComponents(
                    components & BaseUriComponents, format);
            }

            //
            // NOTE: Attempt to combine the paths from both URIs.  This
            //       should result in a combined string, delimted by the
            //       appropriate path separator, without leading and/or
            //       trailing path separators.
            //
            string localPath = null;

            if (FlagOps.HasFlags(components, UriComponents.Path, false))
            {
                localPath = CombinePathsForUri(
                    FlagOps.HasFlags(flags, UriFlags.Normalize, true),
                    baseUri.GetComponents(UriComponents.Path, format),
                    localRelativeUri.GetComponents(UriComponents.Path,
                    format));
            }

            //
            // NOTE: Attempt to combine all name/value pairs from both
            //       URIs.  This will only work when compiled with web
            //       support enabled (i.e. when we can make use of the
            //       System.Web assembly).
            //
            string localQuery = null;

#if WEB
            if (FlagOps.HasFlags(components, UriComponents.Query, false))
            {
                localQuery = CombineQueriesForUri(
                    baseUri.GetComponents(UriComponents.Query, format),
                    localRelativeUri.GetComponents(UriComponents.Query,
                    format), encoding);
            }
#endif

            //
            // NOTE: We cannot combine fragments to help form the final
            //       URI; therefore, consider the one from the relative
            //       URI first, if any.  Failing that, consider the one
            //       from the base URI.  Reverse this preference if the
            //       caller passes the right flag.
            //
            string localFragment = null;

            if (FlagOps.HasFlags(components, UriComponents.Fragment, false))
            {
                if (FlagOps.HasFlags(flags, UriFlags.PreferBaseUri, false))
                {
                    localFragment = baseUri.GetComponents(
                        UriComponents.Fragment, format);

                    if (String.IsNullOrEmpty(localFragment))
                    {
                        localFragment = localRelativeUri.GetComponents(
                            UriComponents.Fragment, format);
                    }
                }
                else
                {
                    localFragment = localRelativeUri.GetComponents(
                        UriComponents.Fragment, format);

                    if (String.IsNullOrEmpty(localFragment))
                    {
                        localFragment = baseUri.GetComponents(
                            UriComponents.Fragment, format);
                    }
                }
            }

            //
            // NOTE: Start building the final URI string, starting with
            //       the main components of the absolute base URI (e.g.
            //       scheme, user-info, server, port, etc), if any.
            //
            StringBuilder builder = StringOps.NewStringBuilder();

            if (!String.IsNullOrEmpty(localBaseComponents))
                builder.Append(localBaseComponents);

            //
            // NOTE: If there is a path, append it to the final URI
            //       string now.  If any component was added before it,
            //       append the appropriate path separator first.
            //
            if (!String.IsNullOrEmpty(localPath))
            {
                //
                // BUGBUG: Is this compliant with the RFC for a URI
                //         that starts with a path (assuming such a
                //         URI is actually legal to begin with)?
                //
                if (builder.Length > 0)
                    builder.Append(Path.AltDirectorySeparatorChar);

                builder.Append(localPath);
            }

            //
            // NOTE: If there is a query, append it to the final URI
            //       string now.  If any component was added before it,
            //       append the question mark first.
            //
            if (!String.IsNullOrEmpty(localQuery))
            {
                //
                // BUGBUG: Is this compliant with the RFC for a URI
                //         that starts with a query (assuming such a
                //         URI is actually legal to begin with)?
                //
                if (builder.Length > 0)
                    builder.Append(Characters.QuestionMark);

                builder.Append(localQuery);
            }

            //
            // NOTE: If there is a fragment, append it to the final URI
            //       string now.  If any component was added before it,
            //       append the number sign first.
            //
            if (!String.IsNullOrEmpty(localFragment))
            {
                //
                // BUGBUG: Is this compliant with the RFC for a URI
                //         that starts with a fragment (assuming such
                //         a URI is actually legal to begin with)?
                //
                if (builder.Length > 0)
                    builder.Append(Characters.NumberSign);

                builder.Append(localFragment);
            }

            //
            // NOTE: Grab the final (built) URI string now.  This will
            //       (potentially) be used for error reporting, should
            //       the actual URI creation fail.
            //
            string builderUri = builder.ToString();

            //
            // NOTE: Attempt to create the final URI object now, using
            //       the final built URI string.  If this fails, give
            //       an appropriate error message.
            //
            Uri uri;

            if (!Uri.TryCreate(builderUri, UriKind.Absolute, out uri))
            {
                error = String.Format(
                    "unable to create combined uri {0}",
                    FormatOps.WrapOrNull(builderUri));

                return null;
            }

            return uri;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TryCreateUri(
            string value,
            ref Uri uri,
            ref UriKind uriKind
            )
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out uri))
            {
                uriKind = UriKind.Absolute;

                return true;
            }
            else if (Uri.TryCreate(value, UriKind.Relative, out uri))
            {
                uriKind = UriKind.Relative;

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsWebUri(
            Uri uri,            /* in */
            ref UriFlags flags, /* in, out */
            ref string host,    /* out */
            ref Result error    /* out */
            )
        {
            if (uri == null)
            {
                error = "invalid uri";
                return false;
            }

            if (!uri.IsAbsoluteUri)
            {
                error = String.Format(
                    "uri {0} is not absolute",
                    FormatOps.WrapOrNull(uri));

                return false;
            }

            string scheme = uri.Scheme;

            if (String.IsNullOrEmpty(scheme))
            {
                error = String.Format(
                    "invalid scheme for uri {0}",
                    FormatOps.WrapOrNull(uri));

                return false;
            }

            bool allowHttps = FlagOps.HasFlags(
                flags, UriFlags.AllowHttps, true);

            bool allowHttp = FlagOps.HasFlags(
                flags, UriFlags.AllowHttp, true);

            bool allowFtp = FlagOps.HasFlags(
                flags, UriFlags.AllowFtp, true);

            bool allowFile = FlagOps.HasFlags(
                flags, UriFlags.AllowFile, true);

            bool wasHttps = false;
            bool wasHttp = false;
            bool wasFtp = false;
            bool wasFile = false;

            if ((allowHttps && (wasHttps = IsHttpsUriScheme(scheme))) ||
                (allowHttp && (wasHttp = IsHttpUriScheme(scheme))) ||
                (allowFtp && (wasFtp = IsFtpUriScheme(scheme))) ||
                (allowFile && (wasFile = IsFileUriScheme(scheme))))
            {
                bool noHost = FlagOps.HasFlags(
                    flags, UriFlags.NoHost, true);

                if (wasHttps)
                    flags |= UriFlags.WasHttps;

                if (wasHttp)
                    flags |= UriFlags.WasHttp;

                if (wasFtp)
                    flags |= UriFlags.WasFtp;

                if (wasFile)
                    flags |= UriFlags.WasFile;

                if (noHost)
                {
                    return true;
                }
                else
                {
                    try
                    {
                        host = uri.DnsSafeHost; /* throw */
                        return true;
                    }
                    catch (Exception e)
                    {
                        error = String.Format(
                            "failed to get host for uri {0}: {1}",
                            FormatOps.WrapOrNull(uri), e);
                    }

                    return false;
                }
            }

            error = String.Format(
                "unsupported uri scheme {0}",
                FormatOps.WrapOrNull(scheme));

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsHttpsUriScheme(
            string scheme
            )
        {
            return (String.Compare(scheme, Uri.UriSchemeHttps,
                StringOps.SystemNoCaseStringComparisonType) == 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsHttpUriScheme(
            string scheme
            )
        {
            return (String.Compare(scheme, Uri.UriSchemeHttp,
                StringOps.SystemNoCaseStringComparisonType) == 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsFtpUriScheme(
            string scheme
            )
        {
            return (String.Compare(scheme, Uri.UriSchemeFtp,
                StringOps.SystemNoCaseStringComparisonType) == 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsFileUriScheme(
            string scheme
            )
        {
            return (String.Compare(scheme, Uri.UriSchemeFile,
                StringOps.SystemNoCaseStringComparisonType) == 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsHttpsUriScheme(
            Uri uri
            )
        {
            return (uri != null) && IsHttpsUriScheme(uri.Scheme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsFileUriScheme(
            Uri uri
            )
        {
            return (uri != null) && IsFileUriScheme(uri.Scheme);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value
            )
        {
            Uri uri = null;

            return IsRemoteUri(value, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value,
            ref Uri uri
            )
        {
            uri = null;

            if (!String.IsNullOrEmpty(value))
            {
                //
                // WARNING: *SECURITY* The "UriKind" value here must be
                //          "Absolute", please do not change it.
                //
                if (Uri.TryCreate(value, UriKind.Absolute, out uri))
                    return !IsFileUriScheme(uri);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ScriptFileNameOnly(
            string path
            )
        {
            if (!HasDirectory(path))
                return path;

            return Path.GetFileName(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetDirectoryName(
            string path
            )
        {
            string result = path;

            try
            {
                if (!String.IsNullOrEmpty(result))
                {
                    if (IsRemoteUri(result))
                    {
                        //
                        // HACK: This is a horrible hack.
                        //
                        result = GetUnixPath(Path.GetDirectoryName(result));
                    }
                    else
                    {
                        result = Path.GetDirectoryName(result);
                    }
                }

                return result;
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsDriveLetterAndColon(
            string path
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            if (path.Length != 2)
                return false;

            if (!Char.IsLetter(path[0]))
                return false;

            if (path[1] != Characters.Colon)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SplitPathRaw(
            string path,
            string separator,
            out string directory,
            out string fileName
            )
        {
            directory = null;
            fileName = null;

            if (String.IsNullOrEmpty(path))
                return;

            int index = path.LastIndexOfAny(DirectoryChars);

            if (index != Index.Invalid)
            {
                directory = path.Substring(0, index);

                if (directory.Length == 0)
                {
                    if (separator != null)
                        directory += separator;
                    else
                        directory += DirectoryChars[index];

                    directory = Path.GetFullPath(directory);

                    if (separator != null)
                        directory = NormalizeSeparators(directory, separator);
                }
                else if (IsDriveLetterAndColon(directory))
                {
                    if (separator != null)
                        directory += separator;
                    else
                        directory += DirectoryChars[index];
                }

                fileName = path.Substring(index + 1);
            }
            else
            {
                fileName = path;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetFirstDirectorySeparator(
            string path,
            ref char separator
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                int[] indexes = {
                    path.IndexOf(Path.DirectorySeparatorChar),
                    path.IndexOf(Path.AltDirectorySeparatorChar)
                };

                int minimumIndex = Index.Invalid;

                foreach (int index in indexes)
                {
                    if (index == Index.Invalid)
                        continue;

                    if ((minimumIndex == Index.Invalid) ||
                        (index < minimumIndex))
                    {
                        minimumIndex = index;
                    }
                }

                if (minimumIndex != Index.Invalid)
                {
                    separator = path[minimumIndex];
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static char GetFirstDirectorySeparator(
            string path
            )
        {
            char separator = NativeDirectorySeparatorChar;

            /* IGNORED */
            GetFirstDirectorySeparator(path, ref separator);

            return separator;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void GetFirstDirectorySeparator(
            IList list,
            ref char separator
            )
        {
            if (list != null)
            {
                for (int index = 0; index < list.Count; index++)
                {
                    string path = StringOps.GetStringFromObject(list[index]);

                    if (!String.IsNullOrEmpty(path) &&
                        GetFirstDirectorySeparator(path, ref separator))
                    {
                        break;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MakeRelativePath(
            string path,
            bool separator /* NOTE: Also remove trailing separator? */
            )
        {
            if (String.IsNullOrEmpty(path))
                return path;

            if (path.Length <= 2) // NOTE: Do NOT return empty string.
                return path;

            if (!StringOps.CharIsAsciiAlpha(path[0]) ||
                (path[1] != Characters.Colon))
            {
                return path;
            }

            return separator ?
                path.Substring(2).TrimStart(DirectoryChars) :
                path.Substring(2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string AppendSeparator(
            string path
            )
        {
            if (String.IsNullOrEmpty(path))
                return path;

            if ((DirectoryChars == null) || (DirectoryChars.Length == 0))
                return path;

            foreach (char character in DirectoryChars)
            {
                int index = path.IndexOf(character);

                if (index != Index.Invalid)
                    return path + character;
            }

            return path + DirectoryChars[0];
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsUnderPath(
            Interpreter interpreter,
            string path1,
            string path2
            )
        {
            string newPath1;

            if (!String.IsNullOrEmpty(path1))
                newPath1 = ResolveFullPath(interpreter, path1);
            else
                newPath1 = GetNativePath(path1);

            string newPath2;

            if (!String.IsNullOrEmpty(path2))
                newPath2 = ResolveFullPath(interpreter, path2);
            else
                newPath2 = GetNativePath(path2);

#if MONO || MONO_HACKS
            //
            // HACK: *MONO* This method crashes on Mono 3.2.3 for Windows with the following
            //       stack trace:
            //
            //       System.TypeInitializationException: An exception was thrown by the type
            //           initializer for Eagle._Components.Public.InterpreterHelper --->
            //           System.TypeInitializationException: An exception was thrown by the
            //           type initializer for Eagle._Components.Private.GlobalState --->
            //           System.NullReferenceException: Object reference not set to an
            //           instance of an object
            //         at System.String.Compare (System.String strA, Int32 indexA,
            //           System.String strB, Int32 indexB, Int32 length, Boolean ignoreCase,
            //           System.Globalization.CultureInfo culture)
            //         at System.String.Compare (System.String strA, Int32 indexA,
            //           System.String strB, Int32 indexB, Int32 length, StringComparison
            //           comparisonType)
            //         at Eagle._Components.Private.PathOps.IsUnderPath
            //           (Eagle._Components.Public.Interpreter interpreter, System.String
            //           path1, System.String path2)
            //         at Eagle._Components.Private.AssemblyOps.GetPath
            //           (Eagle._Components.Public.Interpreter interpreter,
            //           System.Reflection.Assembly assembly)
            //         at Eagle._Components.Private.GlobalState..cctor ()
            //         --- End of inner exception stack trace ---
            //         at Eagle._Components.Public.InterpreterHelper..cctor ()
            //         --- End of inner exception stack trace ---
            //         at (wrapper managed-to-native)
            //           System.Reflection.MonoCMethod:InternalInvoke
            //           (System.Reflection.MonoCMethod,object,object[],System.Exception&)
            //         at System.Reflection.MonoCMethod.InternalInvoke (System.Object obj,
            //           System.Object[] parameters)
            //
            //       The above exception seems to be caused by an error in their code for
            //       the String.Compare method when a non-default application domain is
            //       used.
            //
            if ((newPath1 == null) || (newPath2 == null))
                return false;

            return String.Compare(newPath1, 0, newPath2, 0,
                newPath2.Length, ComparisonType) == 0;
#else
            return String.Compare(newPath1, 0, newPath2, 0,
                (newPath2 != null) ? newPath2.Length : 0,
                ComparisonType) == 0;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int CompareFileNames(
            string path1,
            string path2
            )
        {
            return String.Compare(
                GetNativePath(path1), GetNativePath(path2), ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsEqualFileName(
            string path1,
            string path2
            )
        {
            return String.Equals(
                GetNativePath(path1), GetNativePath(path2), ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsEqualFileName(
            string path1,
            string path2,
            int length
            )
        {
            return String.Compare(
                GetNativePath(path1), 0, GetNativePath(path2), 0, length,
                ComparisonType) == 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            Interpreter interpreter,
            string path1,
            string path2
            )
        {
#if NATIVE && WINDOWS
            UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation1;
            UnsafeNativeMethods.BY_HANDLE_FILE_INFORMATION fileInformation2;

            InitializeFileInformation(out fileInformation1);
            InitializeFileInformation(out fileInformation2);

            if ((GetPathInformation(path1, Directory.Exists(path1),
                    ref fileInformation1) == ReturnCode.Ok) &&
                (GetPathInformation(path2, Directory.Exists(path2),
                    ref fileInformation2) == ReturnCode.Ok))
            {
                return (fileInformation1.dwVolumeSerialNumber == fileInformation2.dwVolumeSerialNumber) &&
                    (fileInformation1.nFileIndexHigh == fileInformation2.nFileIndexHigh) &&
                    (fileInformation1.nFileIndexLow == fileInformation2.nFileIndexLow);
            }
            else
#endif
            {
                string newPath1;

                if (!String.IsNullOrEmpty(path1))
                    newPath1 = ResolveFullPath(interpreter, path1);
                else
                    newPath1 = path1;

                string newPath2;

                if (!String.IsNullOrEmpty(path2))
                    newPath2 = ResolveFullPath(interpreter, path2);
                else
                    newPath2 = path2;

                //
                // NOTE: If the normalized path strings are the same (or they
                //       are both null or empty string) then we match.
                //
                return IsEqualFileName(newPath1, newPath2);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsJustTilde(
            string path
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                if (path.Length == 1)
                {
                    return (path[0] == Characters.Tilde);
                }
                else
                {
                    string trimPath = TrimEndOfPath(path, null);

                    return ((trimPath.Length == 1) &&
                            (trimPath[0] == Characters.Tilde));
                }
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool MatchSuffix(
            string path1,
            string path2
            )
        {
            if ((path1 == null) || (path2 == null))
                return false;

            if (path1.EndsWith(path2, ComparisonType))
                return true;

            string nativePath1 = GetNativePath(path1);
            string nativePath2 = GetNativePath(path2);

            if ((nativePath1 != null) && (nativePath2 != null) &&
                nativePath1.EndsWith(nativePath2, ComparisonType))
            {
                return true;
            }

            string nonNativePath1 = GetNonNativePath(path1);
            string nonNativePath2 = GetNonNativePath(path2);

            if ((nonNativePath1 != null) && (nonNativePath2 != null) &&
                nonNativePath1.EndsWith(nonNativePath2, ComparisonType))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool PathExists(
            string path
            )
        {
            //
            // NOTE: Does the specified path exist as either an
            //       existing directory or a file?
            //
            return Directory.Exists(path) || File.Exists(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TranslatePath(
            string path,
            PathTranslationType translationType
            )
        {
            switch (translationType)
            {
                case PathTranslationType.Unix:
                    return GetUnixPath(path);
                case PathTranslationType.Windows:
                    return GetWindowsPath(path);
                case PathTranslationType.Native:
                    return GetNativePath(path);
                default:
                    return path;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetNativePath(
            string path
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return GetWindowsPath(path);
            else
                return GetUnixPath(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetNonNativePath(
            string path
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return GetUnixPath(path);
            else
                return GetWindowsPath(path);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetWindowsPath(
            string path
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
            {
                result = result.Replace(
                    Path.AltDirectorySeparatorChar,
                    Path.DirectorySeparatorChar);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetUnixPath(
            string path
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
            {
                result = result.Replace(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NormalizeSeparators(
            string path,
            string separator
            )
        {
            if (String.IsNullOrEmpty(path))
                return path;

            if (separator == null)
            {
                return path.Replace(
                    NonNativeDirectorySeparatorChar,
                    NativeDirectorySeparatorChar);
            }

            StringBuilder builder = StringOps.NewStringBuilder(path.Length);

            foreach (char character in path)
            {
                if (IsDirectoryChar(character))
                    builder.Append(separator);
                else
                    builder.Append(character);
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ValidatePathAsFile(
            string path,
            bool rooted,
            bool exists
            )
        {
            return !String.IsNullOrEmpty(path) &&
                (!rooted || Path.IsPathRooted(path)) &&
                (!exists || File.Exists(path));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ValidatePathAsDirectory(
            string path,
            bool rooted,
            bool exists
            )
        {
            return !String.IsNullOrEmpty(path) &&
                (!rooted || Path.IsPathRooted(path)) &&
                (!exists || Directory.Exists(path));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string TildeSubstitution(
            Interpreter interpreter,
            string path,
            bool noSearch,
            bool strict
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
            {
                //
                // NOTE: Unixism: convert leading tilde in the path to the
                //       "home" directory of the current user.
                //
                // NOTE: Also, search for files in the "home" directory in the
                //       various user/application profile directories as well.
                //
                if ((result.Length >= 2) && (result[0] == Characters.Tilde) &&
                    IsDirectoryChar(result[1]))
                {
                    //
                    // HACK: Attempt to search for the file in the standard
                    //       user/application profile locations; failing that,
                    //       just return the file name as though it existed in
                    //       the user directory.
                    //
                    if (!noSearch)
                    {
                        result = Search(interpreter,
                            result, FileSearchFlags.StandardAndStrict);
                    }

                    if (noSearch || (result == null))
                    {
                        result = CombinePath(
                            null, GetUserDirectory(false), path.Substring(1));
                    }
                }
                else if ((result.Length == 1) &&
                    (result[0] == Characters.Tilde))
                {
                    result = GetUserDirectory(false);
                }
                else if (strict &&
                    (result.Length >= 2) && (result[0] == Characters.Tilde))
                {
                    //
                    // NOTE: This is probably something like "~~" or "~foo", which
                    //       we do not support.
                    //
                    return null;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string BaseDirectorySubstitution(
            Interpreter interpreter,
            string path
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result) &&
                result.StartsWith(Vars.BaseDirectory, ComparisonType))
            {
                result = GlobalState.GetBasePath() +
                    result.Substring(Vars.BaseDirectory.Length);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string EnvironmentSubstitution(
            Interpreter interpreter, /* NOT USED */
            string path
            )
        {
            string result = path;

            if (!String.IsNullOrEmpty(result))
                result = CommonOps.Environment.ExpandVariables(result);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string SubstituteOrResolvePath(
            Interpreter interpreter,
            string path,
            bool resolve,
            ref bool remoteUri
            )
        {
            //
            // NOTE: Start with their original path value.
            //
            string result = path;

            //
            // NOTE: Did they pass null or an empty string?
            //
            bool isNullOrEmpty = String.IsNullOrEmpty(result);

            //
            // NOTE: Is this file name a remote URI?
            //
            remoteUri = !isNullOrEmpty ? IsRemoteUri(result) : false;

            //
            // NOTE: If they passed null or an empty string, there is no need
            //       to do anything else.
            //
            if (!isNullOrEmpty)
            {
                //
                // NOTE: Replace the base directory "token" with the actual
                //       base directory.
                //
                result = BaseDirectorySubstitution(interpreter, result);

                //
                // NOTE: Always perform environment substitution (even on
                //       remote URIs).
                //
                result = EnvironmentSubstitution(interpreter, result);

                //
                // NOTE: Only perform leading tilde substitution if the file
                //       name is local.
                //
                if (!remoteUri)
                {
                    //
                    // NOTE: Either resolve the path (skipping the environment
                    //       variables since they have already been done) -OR-
                    //       just perform tilde substitution on it.
                    //
                    result = resolve ?
                        ResolvePathNoEnvironment(interpreter, result) :
                        TildeSubstitution(interpreter, result, false, false);

                    //
                    // NOTE: When we are not fully resolving the file name, for
                    //       file names that do not represent a remote URI, we
                    //       normalize all directory separators in the result
                    //       to the native one for this operating system.
                    //
                    if (!resolve && !String.IsNullOrEmpty(result))
                    {
                        result = result.Replace(
                            NonNativeDirectorySeparatorChar,
                            NativeDirectorySeparatorChar);
                    }
                }
            }

            TraceOps.DebugTrace(String.Format(
                "SubstituteOrResolvePath: interpreter = {0}, " +
                "path = {1}, resolve = {2}, remoteUri = {3}, " +
                "result = {4}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(path), resolve, remoteUri,
                FormatOps.WrapOrNull(result)),
                typeof(PathOps).Name, TracePriority.PathDebug);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string ResolvePathNoEnvironment(
            Interpreter interpreter,
            string path
            )
        {
            return NormalizePath(
                interpreter, null, path, false, false, true, false, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ResolvePath(
            Interpreter interpreter,
            string path
            )
        {
            return NormalizePath(
                interpreter, null, path, false, true, true, false, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string ResolveFullPath(
            Interpreter interpreter,
            string path
            )
        {
            return NormalizePath(
                interpreter, null, path, false, true, true, true, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string MaybeToLib(
            string path,
            bool skipLibraryToLib,
            bool skipTestsToLib,
            bool relative
            )
        {
            string newPath; /* REUSED */
            bool done; /* REUSED */

            if (!skipLibraryToLib)
            {
                newPath = LibraryToLib(path, relative, out done);

                if (done)
                    return newPath;
            }

            if (!skipTestsToLib)
            {
                newPath = TestsToLib(path, relative, out done);

                if (done)
                    return newPath;
            }

            return path;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string TestsToLib(
            string path,
            bool relative,
            out bool done
            )
        {
            //
            // NOTE: This method is useless on null and empty strings, just
            //       return the input path verbatim.
            //
            if (String.IsNullOrEmpty(path))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Break the path into parts, based on the known directory
            //       separator characters.
            //
            StringList parts = new StringList(path.Split(DirectoryChars));

            //
            // NOTE: How many parts are there?
            //
            int count = parts.Count;

            //
            // NOTE: The minimum number of parts must be at least 2, to form
            //       "Tests/<fileName>".
            //
            if (count < 2)
            {
                done = false;
                return path;
            }

            //
            // NOTE: The final part, which is typically the file name, cannot
            //       be null or empty.
            //
            int offset = 1;

            if (String.IsNullOrEmpty(parts[count - offset]))
            {
                done = false;
                return path;
            }

            //
            // NOTE: The next part before that must be exactly "Tests".  On
            //       some systems, the case does not matter (e.g. Windows).
            //
            offset++;

            if (!String.Equals(
                    parts[count - offset], _Path.Tests, ComparisonType))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Insert "lib" just prior to "Tests".
            //
            parts.Insert(count - offset, TclVars.LibPath);

            //
            // NOTE: Since we just inserted an element, update the cached
            //       count.  Also, increment the offset because we are now
            //       interested in at least the final 3 elements.
            //
            count = parts.Count; offset++;

            //
            // NOTE: If we get to this point, this method is performing a
            //       real transformation on the provided path; therefore,
            //       set the output parameter accordingly.
            //
            done = true;

            //
            // NOTE: Are they wanting just the relative [matched] portion
            //       returned?
            //
            if (relative)
            {
                //
                // NOTE: *SPECIAL* Return only the final X parts, joined
                //       into one path, with the "lib" replacement made.
                //
                return String.Join(
                    Path.AltDirectorySeparatorChar.ToString(),
                    parts.ToArray(), count - offset, offset);
            }
            else
            {
                //
                // NOTE: Return all the parts, joined into one path, with
                //       the "lib" replacement made.
                //
                char separator = NativeDirectorySeparatorChar;
                GetFirstDirectorySeparator(path, ref separator);

                return String.Join(separator.ToString(), parts.ToArray());
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: If the last 3 parts of the path are "Library/Tests/<fileName>" -OR- the last 4
        //       parts of the path are "Library/Tests/<dirName>/<fileName>" with a "<dirName>"
        //       value of "data" or "tcl", (case-insensitive), then replace the "Library" part
        //       with "lib" and return the resulting path.  Also, if the "relative" parameter
        //       is non-zero, return only the final X parts of the path, separated by forward
        //       slashes (Unix-style), where X will be either 3 or 4 (i.e. X will only be 4 if
        //       a supported "<dirName>" part exists), .  The returned path may not actually
        //       exist on the file system -AND- that is perfectly OK.
        //
        private static string LibraryToLib(
            string path,
            bool relative,
            out bool done
            )
        {
            //
            // NOTE: This method is useless on null and empty strings, just
            //       return the input path verbatim.
            //
            if (String.IsNullOrEmpty(path))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Break the path into parts, based on the known directory
            //       separator characters.
            //
            string[] parts = path.Split(DirectoryChars);

            //
            // NOTE: How many parts are there?
            //
            int length = parts.Length;

            //
            // NOTE: The minimum number of parts must be at least 3, to form
            //       "Library/Tests/<fileName>".  Instead, there could be 4,
            //       where they may form "Library/Tests/<dirName>/<fileName>",
            //       where the "<dirName>" may be "data" or "tcl".  However,
            //       the absolute minimum number of parts here is still 3.
            //
            if (length < 3)
            {
                done = false;
                return path;
            }

            //
            // NOTE: The final part, which is typically the file name, cannot
            //       be null or empty.
            //
            int offset = 1;

            if (String.IsNullOrEmpty(parts[length - offset]))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Is there a "<dirName>" part equal to "data" or "tcl"?  If
            //       so, skip over it when considering if the remaining parts
            //       fit the supported pattern of "Library/Tests".
            //
            offset++;

            if ((length > 3) && (String.Equals(
                    parts[length - offset], _Path.Data, ComparisonType) ||
                String.Equals(
                    parts[length - offset], _Path.Tcl, ComparisonType)))
            {
                //
                // NOTE: At this point, we know there are at least 4 parts
                //       -AND- that the final part is "data" or "tcl".  So,
                //       skip to the previous part, which should be "Tests".
                //
                offset++;
            }

            //
            // NOTE: The next two parts before that must be exactly "Library"
            //       and "Tests".  On some systems, the case does not matter
            //       (e.g. Windows).
            //
            int nextOffset = offset + 1;

            if (!String.Equals(
                    parts[length - offset], _Path.Tests, ComparisonType) ||
                !String.Equals(
                    parts[length - nextOffset], _Path.Library, ComparisonType))
            {
                done = false;
                return path;
            }

            //
            // NOTE: Change the "Library" part into "lib".
            //
            parts[length - nextOffset] = TclVars.LibPath;

            //
            // NOTE: If we get to this point, this method is performing a
            //       real transformation on the provided path; therefore,
            //       set the output parameter accordingly.
            //
            done = true;

            //
            // NOTE: Are they wanting just the relative [matched] portion
            //       returned?
            //
            if (relative)
            {
                //
                // NOTE: *SPECIAL* Return only the final X parts, joined
                //       into one path, with the "lib" replacement made.
                //
                return String.Join(
                    Path.AltDirectorySeparatorChar.ToString(), parts,
                    length - nextOffset, nextOffset);
            }
            else
            {
                //
                // NOTE: Return all the parts, joined into one path, with
                //       the "lib" replacement made.
                //
                char separator = NativeDirectorySeparatorChar;
                GetFirstDirectorySeparator(path, ref separator);

                return String.Join(separator.ToString(), parts);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string NormalizePath(
            Interpreter interpreter,
            string rootPath,
            string path,
            bool unix,
            bool environment,
            bool tilde,
            bool full,
            bool noCase
            )
        {
            string newPath = null;
            Result error = null;

            if (NormalizePath(
                    interpreter, rootPath, path, unix, environment, tilde,
                    full, noCase, ref newPath, ref error) == ReturnCode.Ok)
            {
                return newPath;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode NormalizePath(
            Interpreter interpreter,
            string rootPath,
            string path,
            bool unix,
            bool environment,
            bool tilde,
            bool full,
            bool noCase,
            ref string newPath,
            ref Result error
            )
        {
            try
            {
                //
                // FIXME: I do not like this function.  It is too complex
                //        and tries to deal with too many corner cases.
                //        Also, some of the places where this function is
                //        called should probably be doing something simpler
                //        instead.
                //
                newPath = path;

                if (!String.IsNullOrEmpty(newPath))
                {
                    //
                    // NOTE: Perform environment substitution on the path?
                    //
                    if (environment)
                        newPath = EnvironmentSubstitution(interpreter, newPath);

                    //
                    // NOTE: Normalize implies clean.
                    //
                    newPath = CleanPath(newPath, false, null);

                    //
                    // NOTE: Collapse any extra trailing directory separator
                    //       characters into exactly one.
                    //
                    newPath = TrimEndOfPath(newPath, Path.DirectorySeparatorChar);

                    //
                    // NOTE: Perform leading tilde substitution?
                    //
                    if (tilde)
                        newPath = TildeSubstitution(interpreter, newPath, false, false);

                    //
                    // NOTE: Only resolve the full path if it will make sense
                    //       (we do not always want to to resolve relative to
                    //       the current directory).
                    //
                    if (!String.IsNullOrEmpty(newPath))
                    {
                        if (Path.IsPathRooted(newPath))
                        {
                            newPath = Path.GetFullPath(newPath); /* throw */
                        }
                        else if (full)
                        {
                            //
                            // NOTE: In this case, we fully resolve the entire
                            //       path, relative to the specified root path.
                            //
                            if (rootPath == null)
                                rootPath = Directory.GetCurrentDirectory();

                            if (!String.IsNullOrEmpty(newPath))
                                newPath = Path.GetFullPath(CombinePath(
                                    unix, rootPath, newPath)); /* throw */
                            else
                                newPath = rootPath;
                        }
                    }

                    //
                    // NOTE: When on Unix, use forward slashes; otherwise
                    //       (Windows), use backslashes.
                    //
                    newPath = unix ? GetUnixPath(newPath) : GetWindowsPath(newPath);

                    //
                    // NOTE: Does the result need to be normalized to lower
                    //       case?
                    //
                    if (noCase && !String.IsNullOrEmpty(newPath))
                    {
                        //
                        // NOTE: From the MSDN documentation at:
                        //
                        //       "ms-help://MS.NETDEVFX.v20.en/cpref7/html/
                        //              M_System_String_ToLowerInvariant.htm"
                        //
                        //       Security Considerations
                        //
                        //       If you need the lowercase or uppercase version of an
                        //       operating system identifier, such as a file name,
                        //       named pipe, or registry key, use the ToLowerInvariant
                        //       or ToUpperInvariant methods.
                        //
                        newPath = newPath.ToLowerInvariant();
                    }

                    //
                    // BUGFIX: Do not remove trailing slashes from the root path.
                    //
                    if (!IsEqualFileName(newPath, Path.GetPathRoot(newPath)))
                    {
                        //
                        // BUGFIX: Remove any trailing slashes.
                        //
                        newPath = TrimEndOfPath(newPath, null);
                    }
                }

                //
                // NOTE: If we get to this point, we have succeeded; however,
                //       this does not necessarily mean that we have a valid
                //       path in the result.
                //
                TraceOps.DebugTrace(String.Format(
                    "NormalizePath: interpreter = {0}, rootPath = {1}, " +
                    "path = {2}, unix = {3}, environment = {4}, tilde = {5}, " +
                    "full = {6}, noCase = {7}, newPath = {8}, code = {9}, " +
                    "error = {10}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(rootPath), FormatOps.WrapOrNull(path),
                    unix, environment, tilde, full, noCase,
                    FormatOps.WrapOrNull(newPath), ReturnCode.Ok,
                    FormatOps.WrapOrNull(true, true, error)),
                    typeof(PathOps).Name, TracePriority.PathDebug);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                //
                // NOTE: We encountered some kind of error while
                //       translating the path, return null to
                //       signal the error to the caller.
                //
                error = e;
            }

            TraceOps.DebugTrace(String.Format(
                "NormalizePath: interpreter = {0}, rootPath = {1}, " +
                "path = {2}, unix = {3}, environment = {4}, tilde = {5}, " +
                "full = {6}, noCase = {7}, newPath = {8}, code = {9}, " +
                "error = {10}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(rootPath), FormatOps.WrapOrNull(path),
                unix, environment, tilde, full, noCase,
                FormatOps.WrapOrNull(newPath), ReturnCode.Error,
                FormatOps.WrapOrNull(true, true, error)),
                typeof(PathOps).Name, TracePriority.PathError);

            return ReturnCode.Error;
        }
    }
}
