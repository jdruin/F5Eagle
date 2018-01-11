/*
 * Version.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("b5813212-3372-416c-96ac-0b424a1465f3")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("introspection")]
    internal sealed class _Version : Core
    {
        public _Version(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return RuntimeOps.GetVersion(ref result);
        }
        #endregion
    }
}
