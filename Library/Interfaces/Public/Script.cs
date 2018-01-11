/*
 * Script.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if XML
using System;
#endif

using System.Collections;

#if CAS_POLICY
using System.Security.Cryptography;
using System.Security.Policy;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("15da79ef-3b2a-42fc-97bc-da5b0384e23e")]
    public interface IScript : IScriptLocation, ICollection, IIdentifier
    {
        string Type { get; }
        IList Parts { get; }
        string Text { get; }
        EngineMode EngineMode { get; }
        ScriptFlags ScriptFlags { get; }
        EngineFlags EngineFlags { get; }
        SubstitutionFlags SubstitutionFlags { get; }
        EventFlags EventFlags { get; }
        ExpressionFlags ExpressionFlags { get; }

#if XML
        XmlBlockType BlockType { get; }
        DateTime TimeStamp { get; }
        string PublicKeyToken { get; }
        byte[] Signature { get; }
#endif

#if CAS_POLICY
        Evidence Evidence { get; }
        byte[] HashValue { get; }
        HashAlgorithm HashAlgorithm { get; }
#endif
    }
}
