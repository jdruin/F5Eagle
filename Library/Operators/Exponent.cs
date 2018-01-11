/*
 * Exponent.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    [ObjectId("3a0efea2-4c30-43b2-b63a-b3c7d0f0dbc9")]
    [OperatorFlags(OperatorFlags.Standard | OperatorFlags.Arithmetic)]
    [Lexeme(Lexeme.Exponent)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("exponential")]
    [ObjectName(Operators.Exponent)]
    internal sealed class Exponent : Core
    {
        public Exponent(
            IOperatorData operatorData
            )
            : base(operatorData)
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
                    try
                    {
                        string name = null;
                        Variant operand1 = null;
                        Variant operand2 = null;

                        code = Value.GetOperandsFromArguments(interpreter,
                            this, arguments, ValueFlags.AnyVariant,
                            interpreter.CultureInfo, ref name,
                            ref operand1, ref operand2, ref error);

                        if (code == ReturnCode.Ok)
                        {
                            code = Value.FixupVariants(
                                this, operand1, operand2, null, null, false, false,
                                ref error);

                            if (code == ReturnCode.Ok)
                            {
                                if (operand1.IsDouble())
                                {
                                    value = Interpreter.FixIntermediatePrecision(
                                        Math.Pow((double)operand1.Value, (double)operand2.Value));
                                }
                                else if (operand1.IsDecimal())
                                {
                                    if (operand1.ConvertTo(typeof(double)))
                                    {
                                        if (operand2.ConvertTo(typeof(double)))
                                        {
                                            value = Interpreter.FixIntermediatePrecision(
                                                Math.Pow((double)operand1.Value, (double)operand2.Value));
                                        }
                                        else
                                        {
                                            error = String.Format(
                                                "could not convert \"{0}\" to double",
                                                operand2.Value);

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "could not convert \"{0}\" to double",
                                            operand1.Value);

                                        code = ReturnCode.Error;
                                    }
                                }
                                else if (operand1.IsWideInteger())
                                {
                                    value = MathOps.Pow((long)operand1.Value, (long)operand2.Value);
                                }
                                else if (operand1.IsInteger())
                                {
                                    value = MathOps.Pow((int)operand1.Value, (int)operand2.Value);
                                }
                                else if (operand1.IsBoolean())
                                {
                                    value = MathOps.Pow(ConversionOps.ToInt((bool)operand1.Value),
                                        ConversionOps.ToInt((bool)operand2.Value));
                                }
                                else
                                {
                                    error = String.Format(
                                        "unsupported operand type for operator {0}",
                                        FormatOps.OperatorName(name, this.Lexeme));

                                    code = ReturnCode.Error;
                                }
                            }
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

