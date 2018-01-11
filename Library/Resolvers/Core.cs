/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Resolvers
{
    [ObjectId("2465f7d5-091b-4466-aebb-61caa1fe00da")]
    public class Core : Default
    {
        #region Public Constructors
        public Core(
            IResolveData resolveData
            )
            : base(resolveData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IResolve Members
        public override ReturnCode GetVariableFrame(
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags flags,
            ref Result error
            )
        {
            Interpreter interpreter = base.Interpreter;

            if (interpreter != null)
            {
                //
                // NOTE: This is used for legacy compatibility
                //       with the Eagle beta releases.
                //
                frame = interpreter.GetVariableFrame(
                    frame, ref varName, ref flags); /* EXEMPT */

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode GetCurrentNamespace(
            ICallFrame frame,          /* NOT USED */
            ref INamespace @namespace,
            ref Result error
            )
        {
            Interpreter interpreter = base.Interpreter;

            if (interpreter != null)
            {
                @namespace = interpreter.GlobalNamespace;
                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode GetIExecute(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments, /* NOT USED */
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IExecute execute,
            ref Result error
            )
        {
            Interpreter interpreter = base.Interpreter;

            if (interpreter != null)
            {
                //
                // NOTE: Lookup the command or procedure to execute.  We
                //       use inexact (unique prefix) matching here unless
                //       we are forbidden from doing so; in that case, we
                //       use exact matching.
                //
                if (Engine.HasExactMatch(engineFlags))
                {
                    return interpreter.GetAnyIExecute(
                        frame, engineFlags | EngineFlags.GetHidden,
                        name, lookupFlags, ref token, ref execute,
                        ref error);
                }
                else
                {
                    //
                    // NOTE: Include hidden commands in the resolution
                    //       phase here because the policy decisions about
                    //       whether or not to execute them are not made
                    //       here.
                    //
                    return interpreter.MatchAnyIExecute(
                        frame, engineFlags | EngineFlags.MatchHidden,
                        name, lookupFlags, ref ambiguous, ref token,
                        ref execute, ref error);
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode GetVariable(
            ICallFrame frame,
            string varName,
            string varIndex,
            ref VariableFlags flags,
            ref IVariable variable,
            ref Result error
            )
        {
            Interpreter interpreter = base.Interpreter;

            if (interpreter != null)
            {
                //
                // NOTE: Lookup the variable using the default semantics
                //       provided by the interpreter.  This resolver does
                //       not support namespaces.  At some point in the
                //       future, alternative resolvers may be used to
                //       support namespaces.
                //
                return interpreter.GetVariable(
                    frame, varName, varIndex, ref flags,
                    ref variable, ref error);
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
