/*
 * Rand.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("c3c083a9-bab0-4153-8223-51ae7bc16953")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Nullary)]
    [ObjectGroup("random")]
    internal sealed class Rand : Core
    {
        public Rand(
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
                            Random random;

                            lock (interpreter.SyncRoot)
                            {
                                random = interpreter.Random;
                            }

                            if (random != null)
                            {
                                value = Interpreter.FixIntermediatePrecision(
                                    random.NextDouble());
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