/*
 * Random.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Security.Cryptography;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("1497187e-2051-473e-b55c-179b4c74d71d")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Nullary)]
    [ObjectGroup("random")]
    internal sealed class _Random : Core
    {
        public _Random(
            IFunctionData functionData
            )
            : base(functionData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == (this.Arguments + 1))
                    {
                        try
                        {
                            RandomNumberGenerator rng;

                            lock (interpreter.SyncRoot)
                            {
                                rng = interpreter.RandomNumberGenerator;
                            }

                            if (rng != null)
                            {
                                byte[] bytes = new byte[sizeof(long)];

                                rng.GetBytes(bytes);

                                value = BitConverter.ToInt64(bytes, 0);
                            }
                            else
                            {
                                error = "random number generator not available";
                                code = ReturnCode.Error;
                            }
                        }
                        catch (Exception e)
                        {
                            Engine.SetExceptionErrorCode(interpreter, e);

                            error = String.Format(
                                "caught math exception: {0}",
                                e);

                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (arguments.Count > (this.Arguments + 1))
                            error = String.Format(
                                "too many arguments for math function \"{0}\"",
                                base.Name);
                        else
                            error = String.Format(
                                "too few arguments for math function \"{0}\"",
                                base.Name);

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}