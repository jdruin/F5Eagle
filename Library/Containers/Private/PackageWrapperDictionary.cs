/*
 * PackageWrapperDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Containers.Private
{
    [ObjectId("cea47a6d-b9e1-4bdd-b1c7-1dc0be1967af")]
    internal sealed class PackageWrapperDictionary : WrapperDictionary<string, _Wrappers.Package>
    {
        public PackageWrapperDictionary()
            : base()
        {
            // do nothing.
        }
    }
}
