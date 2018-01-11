using System;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    [ObjectId("DB81B0B7-BD67-4924-B797-72CC86CC10CB")]
    [OperatorFlags(OperatorFlags.NonStandard | OperatorFlags.String)]
    [Lexeme(Lexeme.F5MatchesRegex)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("equality")]
    [ObjectName(Operators.F5MatchesRegex)]
    internal sealed class F5MatchesRegex : Core
    {
        public F5MatchesRegex(IOperatorData operatorData) : base(operatorData)
        {}

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

                            //
                            // NOTE: Fine, try to treat the operands as strings.
                            //
                            code = Value.FixupStringVariants(
                                this, operand1, operand2, ref error);

                            if (code == ReturnCode.Ok)
                            {
                                if (operand1.IsString())
                                {
                                    string str1 = (string)operand1.Value;
                                    string str2 = (string)operand2.Value;
                                    var regex = new Regex(str2);

                                    value = regex.IsMatch(str1);
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
    }
}
