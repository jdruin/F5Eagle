/*
 * MutableAnyPair.cs --
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
    [ObjectId("68863146-12dd-45fa-b2a9-3f0f2dd0e67b")]
    public interface IMutableAnyPair<T1, T2> : IAnyPair<T1, T2>
    {
        bool Mutable { get; }

        new T1 X { get; [Throw(true)] set; }
        new T2 Y { get; [Throw(true)] set; }
    }
}
