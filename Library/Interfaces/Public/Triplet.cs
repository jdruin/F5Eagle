/*
 * Triplet.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("23f69942-2259-403c-8474-53c3b740423c")]
    public interface ITriplet<T> : IAnyTriplet<T, T, T>
    {
        // nothing.
    }
}
