/*
 * ThreadContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("3cd21398-740b-4986-a86f-e7ad4738d322")]
    internal interface IThreadContext : IGetInterpreter
    {
        //
        // WARNING: This property may not throw exceptions.
        //
        [Throw(false)]
        bool Disposed { get; }

        //
        // WARNING: This property may not throw exceptions.
        //
        [Throw(false)]
        int ThreadId { get; }
    }
}
