/*
 * Fmod.cs --
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
    [ObjectId("cc4ac906-03b1-4521-868f-78a7fb5a86a5")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Binary)]
    [TypeListFlags(TypeListFlags.FloatTypes)]
    [ObjectGroup("congruence")]
    internal sealed class Fmod : Core
    {
        public Fmod(
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
                        double[] doubleValue = { 0.0, 0.0 };

                        code = Value.GetDouble(
                            (IGetValue)arguments[1], interpreter.CultureInfo,
                            ref doubleValue[0], ref error);

                        if (code == ReturnCode.Ok)
                        {
                            code = Value.GetDouble(
                                (IGetValue)arguments[2], interpreter.CultureInfo,
                                ref doubleValue[1], ref error);
                        }

                        if (code == ReturnCode.Ok)
                        {
                            try
                            {
                                value = Interpreter.FixIntermediatePrecision(
                                    Math.IEEERemainder(doubleValue[0], doubleValue[1]));
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