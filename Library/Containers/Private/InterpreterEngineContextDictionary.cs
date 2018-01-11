/*
 * InterpreterEngineContextDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
    [ObjectId("bd183024-d512-42c4-8364-d31f2503d781")]
    internal sealed class InterpreterEngineContextDictionary
            : Dictionary<IInterpreter, IEngineContext>
    {
        public InterpreterEngineContextDictionary()
            : base(new _Comparers._Interpreter())
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public bool RemoveAndReturn(
            IInterpreter key,
            out IEngineContext value
            )
        {
            /* IGNORED */
            base.TryGetValue(key, out value);

            return base.Remove(key);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            InterpreterList list = new InterpreterList(this.Keys);

            return GenericOps<IInterpreter>.ListToString(
                list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(),
                pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
