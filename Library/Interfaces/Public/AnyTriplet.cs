/*
 * AnyTriplet.cs --
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
    [ObjectId("8937ed3b-1e45-43a8-ad8e-c390fac435b7")]
    public interface IAnyTriplet<T1, T2, T3>
    {
        T1 X { get; }
        T2 Y { get; }
        T3 Z { get; }
    }
}
