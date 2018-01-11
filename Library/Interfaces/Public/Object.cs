/*
 * Object.cs --
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
    [ObjectId("0a0f31fa-bb82-4cbe-9ef2-0c0718ac9c3d")]
    public interface IObject : IObjectData, IValue, IValueData
    {
        // nothing.
    }
}
