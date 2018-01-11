/*
 * AttributeOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;

#if !EAGLE
using System.Runtime.InteropServices;
#endif

using Eagle._Attributes;

namespace Eagle._Components.Shared
{
#if EAGLE
    [ObjectId("b7db31a5-539b-4457-9123-6cdacd4f930c")]
#else
    [Guid("b7db31a5-539b-4457-9123-6cdacd4f930c")]
#endif
    internal static class AttributeOps
    {
        #region Private Constants
        private static readonly string UpdateUriName = "update";
        private static readonly string DownloadUriName = "download";
        private static readonly string ScriptUriName = "script";
        private static readonly string AuxiliaryUriName = "auxiliary";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Assembly Attribute Methods
        public static string GetAssemblyRelease(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyReleaseAttribute), false))
                    {
                        AssemblyReleaseAttribute release =
                            (AssemblyReleaseAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyReleaseAttribute), false)[0];

                        return release.Release;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblySourceId(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblySourceIdAttribute), false))
                    {
                        AssemblySourceIdAttribute sourceId =
                            (AssemblySourceIdAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblySourceIdAttribute), false)[0];

                        return sourceId.SourceId;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblySourceTimeStamp(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblySourceTimeStampAttribute), false))
                    {
                        AssemblySourceTimeStampAttribute sourceTimeStamp =
                            (AssemblySourceTimeStampAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblySourceTimeStampAttribute),
                                false)[0];

                        return sourceTimeStamp.SourceTimeStamp;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyStrongNameTag(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyStrongNameTagAttribute), false))
                    {
                        AssemblyStrongNameTagAttribute strongNameTag =
                            (AssemblyStrongNameTagAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyStrongNameTagAttribute),
                                false)[0];

                        return strongNameTag.StrongNameTag;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTag(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyTagAttribute), false))
                    {
                        AssemblyTagAttribute tag =
                            (AssemblyTagAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyTagAttribute), false)[0];

                        return tag.Tag;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyText(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyTextAttribute), false))
                    {
                        AssemblyTextAttribute text =
                            (AssemblyTextAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyTextAttribute), false)[0];

                        return text.Text;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTitle(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyTitleAttribute), false))
                    {
                        AssemblyTitleAttribute title =
                            (AssemblyTitleAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyTitleAttribute), false)[0];

                        return title.Title;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri(
            Assembly assembly
            )
        {
            return GetAssemblyUri(assembly, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri(
            Assembly assembly,
            string name
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyUriAttribute), false))
                    {
                        object[] attributes = assembly.GetCustomAttributes(
                            typeof(AssemblyUriAttribute), false);

                        if (attributes != null)
                        {
                            foreach (object attribute in attributes)
                            {
                                AssemblyUriAttribute uri =
                                    attribute as AssemblyUriAttribute;

                                if ((uri != null) &&
                                    String.Equals(uri.Name, name))
                                {
                                    return uri.Uri;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUpdateBaseUri(
            Assembly assembly
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyUpdateBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, UpdateUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyDownloadBaseUri(
            Assembly assembly
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyDownloadBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, DownloadUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyScriptBaseUri(
            Assembly assembly
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyScriptBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, ScriptUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyAuxiliaryBaseUri(
            Assembly assembly
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyAuxiliaryBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, AuxiliaryUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }
        #endregion
    }
}
