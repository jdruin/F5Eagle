/*
 * LeftShift.cs --
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
    [ObjectId("13823c03-46f6-483c-bb2f-075f0f76724e")]
    [OperatorFlags(OperatorFlags.Standard | OperatorFlags.Bitwise)]
    [Lexeme(Lexeme.LeftShift)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.IntegralTypes)]
    [ObjectGroup("bitwise")]
    [ObjectName(Operators.LeftShift)]
    internal sealed class LeftShift : Core
    {
        public LeftShift(
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
                                this, operand1, operand2, null, null, false, true,
                                ref error);

                            if (code == ReturnCode.Ok)
                            {
                                if (operand1.IsWideInteger())
                                {
                                    value = MathOps.LeftShift((long)operand1.Value, (int)operand2.Value);
                                }
                                else if (operand1.IsInteger())
                                {
                                    value = MathOps.LeftShift((int)operand1.Value, (int)operand2.Value);
                                }
                                else if (operand1.IsBoolean())
                                {
                                    value = MathOps.LeftShift(ConversionOps.ToInt((bool)operand1.Value),
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
