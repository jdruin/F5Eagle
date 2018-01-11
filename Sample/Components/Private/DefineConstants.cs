/*
 * DefineConstants.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Sample
{
    [ObjectId("7a3fdc54-8976-4c3e-a5de-a88b92f83dbe")]
    internal static class DefineConstants
    {
        public static readonly StringList OptionList = new StringList(new string[] {
#if ASSEMBLY_DATETIME
            "ASSEMBLY_DATETIME",
#endif

#if ASSEMBLY_RELEASE
            "ASSEMBLY_RELEASE",
#endif

#if ASSEMBLY_STRONG_NAME_TAG
            "ASSEMBLY_STRONG_NAME_TAG",
#endif

#if ASSEMBLY_TEXT
            "ASSEMBLY_TEXT",
#endif

#if CONSOLE
            "CONSOLE",
#endif

#if DEBUG
            "DEBUG",
#endif

#if NET_20_ONLY
            "NET_20_ONLY",
#endif

#if NOTIFY
            "NOTIFY",
#endif

#if NOTIFY_OBJECT
            "NOTIFY_OBJECT",
#endif

#if OFFICIAL
            "OFFICIAL",
#endif

#if PATCHLEVEL
            "PATCHLEVEL",
#endif

#if SAMPLE
            "SAMPLE",
#endif

#if SOURCE_ID
            "SOURCE_ID",
#endif

#if SOURCE_TIMESTAMP
            "SOURCE_TIMESTAMP",
#endif

#if STABLE
            "STABLE",
#endif

#if THROW_ON_DISPOSED
            "THROW_ON_DISPOSED",
#endif

#if TCL
            "TCL",
#endif

#if TRACE
            "TRACE",
#endif

            null
        });
    }
}
