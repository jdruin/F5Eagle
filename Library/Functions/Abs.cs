/*
 * Abs.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("6331715b-4d54-43cd-b155-7862da954759")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("component")]
    internal sealed class Abs : Core
    {
        public Abs(
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
                        Variant variant1 = null;

                        code = Value.GetVariant(interpreter,
                            (IGetValue)arguments[1], ValueFlags.AnyVariant,
                            interpreter.CultureInfo, ref variant1, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            try
                            {
                                if (variant1.IsDouble())
                                {
                                    value = Interpreter.FixIntermediatePrecision(
                                        Math.Abs((double)variant1.Value));
                                }
                                else if (variant1.IsDecimal())
                                {
                                    value = Interpreter.FixIntermediatePrecision(
                                        Math.Abs((decimal)variant1.Value));
                                }
                                else if (variant1.IsWideInteger())
                                {
                                    value = Math.Abs((long)variant1.Value);
                                }
                                else if (variant1.IsInteger())
                                {
                                    value = Math.Abs((int)variant1.Value);
                                }
                                else if (variant1.IsBoolean())
                                {
                                    value = Math.Abs(ConversionOps.ToInt((bool)variant1.Value));
                                }
                                else
                                {
                                    error = String.Format(
                                        "unsupported variant type for function \"{0}\"",
                                        base.Name);

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
