/*
 * LambdaWrapperDictionary.cs --
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

namespace Eagle._Containers.Private
{
    [ObjectId("b1b72fd7-6519-43e4-81ec-e989965a3a18")]
    internal sealed class LambdaWrapperDictionary :
            WrapperDictionary<string, _Wrappers.Lambda>
    {
        public LambdaWrapperDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode ToList(
            ProcedureFlags hasFlags,
            ProcedureFlags notHasFlags,
            bool hasAll,
            bool notHasAll,
            string pattern,
            bool noCase,
            ref StringList list,
            ref Result error
            )
        {
            StringList inputList;

            //
            // NOTE: If no flags were supplied, we do not bother filtering on
            //       them.
            //
            if ((hasFlags == ProcedureFlags.None) &&
                (notHasFlags == ProcedureFlags.None))
            {
                inputList = new StringList(this.Keys);
            }
            else
            {
                inputList = new StringList();

                foreach (KeyValuePair<string, _Wrappers.Lambda> pair in this)
                {
                    if (pair.Value != null)
                    {
                        if (((hasFlags == ProcedureFlags.None) ||
                                FlagOps.HasFlags(
                                    pair.Value.Flags, hasFlags, hasAll)) &&
                            ((notHasFlags == ProcedureFlags.None) ||
                                !FlagOps.HasFlags(
                                    pair.Value.Flags, notHasFlags, notHasAll)))
                        {
                            inputList.Add(pair.Key);
                        }
                    }
                }
            }

            if (list == null)
                list = new StringList();

            return GenericOps<string>.FilterList(
                inputList, list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, pattern, noCase, ref error);
        }
    }
}
