/*
 * NotEqual.cs --
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
    [ObjectId("ab93e922-b5e8-48b2-a5ec-6d576dac8ba8")]
    [OperatorFlags(OperatorFlags.Standard | OperatorFlags.Relational)]
    [Lexeme(Lexeme.NotEqual)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("inequation")]
    [ObjectName(Operators.NotEqual)]
    internal sealed class NotEqual : Core
    {
        public NotEqual(
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
                                    value = ((double)operand1.Value != (double)operand2.Value);
                                }
                                else if (operand1.IsDecimal())
                                {
                                    value = ((decimal)operand1.Value != (decimal)operand2.Value);
                                }
                                else if (operand1.IsWideInteger())
                                {
                                    value = ((long)operand1.Value != (long)operand2.Value);
                                }
                                else if (operand1.IsInteger())
                                {
                                    value = ((int)operand1.Value != (int)operand2.Value);
                                }
                                else if (operand1.IsBoolean())
                                {
                                    value = ((bool)operand1.Value != (bool)operand2.Value);
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
                                //
                                // NOTE: Fine, try to treat the operands as strings.
                                //
                                code = Value.FixupStringVariants(
                                    this, operand1, operand2, ref error);

                                if (code == ReturnCode.Ok)
                                {
                                    if (operand1.IsString())
                                    {
                                        value = (String.Compare(
                                            (string)operand1.Value, (string)operand2.Value,
                                            StringOps.UserStringComparisonType) != 0);
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

