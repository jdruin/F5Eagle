/*
 * AnyPair.cs --
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
    [ObjectId("97655866-7ec2-4254-9997-ba51d60b6c62")]
    public interface IAnyPair<T1, T2>
    {
        T1 X { get; }
        T2 Y { get; }
    }
}
