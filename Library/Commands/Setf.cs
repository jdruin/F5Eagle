/*
 * Setf.cs --
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

namespace Eagle._Commands
{
    [ObjectId("7c8c73c9-41f9-496f-b1a5-1b4a9aa421c4")]
    [Obsolete()]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard | CommandFlags.Obsolete | CommandFlags.Diagnostic)]
    [ObjectGroup("variable")]
    internal sealed class Setf : Core
    {
        #region Public Constructors
        public Setf(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if ((arguments.Count == 3) || (arguments.Count == 4))
                    {
                        VariableFlags flags = VariableFlags.None;

                        object enumValue = EnumOps.TryParseFlagsEnum(
                            interpreter, typeof(VariableFlags),
                            flags.ToString(), arguments[1],
                            interpreter.CultureInfo, true, true,
                            true, ref result);

                        if (enumValue is VariableFlags)
                        {
                            flags = (VariableFlags)enumValue;

                            if (arguments.Count == 3)
                            {
                                code = interpreter.GetVariableValue(
                                    flags, arguments[2], ref result, ref result);
                            }
                            else if (arguments.Count == 4)
                            {
                                code = interpreter.SetVariableValue(
                                    flags, arguments[2], arguments[3], null, ref result);

                                if (code == ReturnCode.Ok)
                                    //
                                    // NOTE: Maybe it was append mode?  Re-get the value now.
                                    //
                                    code = interpreter.GetVariableValue(
                                        flags, arguments[2], ref result, ref result);
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"setf varFlags varName ?newValue?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
