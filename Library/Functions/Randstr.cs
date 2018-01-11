/*
 * Randstr.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("b9168098-2447-4a77-825a-7661eaeefbb6")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.IntegerTypes)]
    [ObjectGroup("random")]
    internal sealed class Randstr : Core
    {
        public Randstr(
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
                        int intValue = 0;

                        code = Value.GetInteger2(
                            (IGetValue)arguments[1], ValueFlags.AnyInteger,
                            interpreter.CultureInfo, ref intValue, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            if (intValue > 0)
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
                                        byte[] bytes = new byte[intValue];

                                        rng.GetBytes(bytes);

                                        string stringValue = null;

                                        code = StringOps.GetString(
                                            null, bytes, EncodingType.Binary,
                                            ref stringValue, ref error);

                                        if (code == ReturnCode.Ok)
                                            value = stringValue;
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
                                error = "number of bytes must be greater than zero";
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
