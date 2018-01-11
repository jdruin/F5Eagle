/*
 * LogicalNot.cs --
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
    [ObjectId("A575C774-0A72-4EB2-934B-4C70D367FC47")]
    [OperatorFlags(OperatorFlags.NonStandard | OperatorFlags.Logical)]
    [Lexeme(Lexeme.F5LogicalNot)]
    [Operands(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("logical")]
    [ObjectName(Operators.F5LogicalNot)]
    internal sealed class F5LogicalNot : Core
    {
        public F5LogicalNot(
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
                            if (operand1 != null)
                            {
                                if (operand1.ConvertTo(typeof(bool)))
                                {
                                    if (operand1.IsBoolean())
                                    {
                                        value = LogicOps.Not((bool)operand1.Value);
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "unsupported operand type for operator {0}",
                                            FormatOps.OperatorName(name, this.Lexeme));

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = String.Format(
                                        "failed to convert operand to type \"{0}\"",
                                        typeof(bool));

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "operand for operator {0} is invalid",
                                    FormatOps.OperatorName(name, this.Lexeme));

                                code = ReturnCode.Error;
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

