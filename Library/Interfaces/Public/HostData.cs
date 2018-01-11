/*
 * HostData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Resources;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("35cfe935-a23e-48ce-a395-2dab9c268c2f")]
    public interface IHostData : IIdentifier, IHaveInterpreter
    {
        string TypeName { get; set; }
        ResourceManager ResourceManager { get; set; }
        string Profile { get; set; }
        bool UseAttach { get; set; }
        bool NoColor { get; set; }
        bool NoTitle { get; set; }
        bool NoIcon { get; set; }
        bool NoProfile { get; set; }
        bool NoCancel { get; set; }
    }
}
