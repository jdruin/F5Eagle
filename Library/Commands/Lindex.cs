/*
 * Lindex.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("e60c0a62-397d-42cf-90d2-62be391062b3")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lindex : Core
    {
        public Lindex(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
                    if (arguments.Count >= 2)
                    {
                        int argumentIndex = 1;
                        string text = arguments[argumentIndex];
                        StringList inputList;

                        while (++argumentIndex < arguments.Count)
                        {
                            inputList = null;

                            code = Parser.SplitList(
                                interpreter, text, 0, Length.Invalid,
                                true, ref inputList, ref result);

                            if (code != ReturnCode.Ok)
                                break;

                            int index = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[argumentIndex], inputList.Count,
                                ValueFlags.AnyIndex, interpreter.CultureInfo,
                                ref index, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((index >= 0) && (index < inputList.Count))
                                    text = inputList[index];
                                else
                                    text = String.Empty;
                            }
                            else
                            {
                                IntList indexList = null;

                                code = Value.GetIndexList(
                                    interpreter, arguments[argumentIndex],
                                    inputList.Count, ValueFlags.AnyIndex,
                                    interpreter.CultureInfo, ref indexList);

                                if (code == ReturnCode.Ok)
                                {
                                    StringList outputList = new StringList();

                                    foreach (int index2 in indexList)
                                    {
                                        if ((index2 >= 0) && (index2 < inputList.Count))
                                            outputList.Add(inputList[index2]);
                                        else
                                            outputList.Add(String.Empty);
                                    }

                                    text = outputList.ToString();
                                }
                            }

                            if (code != ReturnCode.Ok)
                                break;
                        }

                        if (code == ReturnCode.Ok)
                            result = text;
                    }
                    else
                    {
                        result = "wrong # args: should be \"lindex list ?index ...?\"";
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
