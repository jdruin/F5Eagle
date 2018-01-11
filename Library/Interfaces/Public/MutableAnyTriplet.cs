/*
 * MutableAnyTriplet.cs --
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
    [ObjectId("db0bf1e1-3c6c-42ad-abbc-97617352d13a")]
    public interface IMutableAnyTriplet<T1, T2, T3> : IAnyTriplet<T1, T2, T3>
    {
        bool Mutable { get; }

        new T1 X { get; [Throw(true)] set; }
        new T2 Y { get; [Throw(true)] set; }
        new T3 Z { get; [Throw(true)] set; }
    }
}
