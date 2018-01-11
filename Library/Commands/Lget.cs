/*
 * Lget.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("55e4e145-6aa0-47d8-8a1c-c1c50d9d459e")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("list")]
    internal sealed class Lget : Core
    {
        public Lget(
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
                        lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                        {
                            StringList list = null;

                            code = interpreter.GetListVariableValue(
                                VariableFlags.None, arguments[1], false, false, true, ref list,
                                ref result);

                            if (code == ReturnCode.Ok)
                            {
                                Result value = list;
                                int argumentIndex = 2; // start at first index.
                                int listIndex = argumentIndex - 2;

                                if (arguments.Count > 2)
                                {
                                    StringList[] lists = new StringList[arguments.Count - 2]; // only count index args.
                                    int[] index = new int[arguments.Count - 2];

                                    do
                                    {
                                        if ((value != null) && (value.Value is StringList))
                                        {
                                            lists[listIndex] = (StringList)value.Value;
                                        }
                                        else
                                        {
                                            lists[listIndex] = null;

                                            //
                                            // WARNING: Cannot cache list representation here, the list
                                            //          is modified below.
                                            //
                                            code = Parser.SplitList(
                                                interpreter, value, 0, Length.Invalid,
                                                false, ref lists[listIndex], ref result);

                                            if (code != ReturnCode.Ok)
                                                break;
                                        }

                                        index[listIndex] = Index.Invalid;

                                        code = Value.GetIndex(
                                            arguments[argumentIndex], lists[listIndex].Count,
                                            ValueFlags.AnyIndex, interpreter.CultureInfo,
                                            ref index[listIndex], ref result);

                                        if (code != ReturnCode.Ok)
                                            break;

                                        if ((index[listIndex] < 0) ||
                                            (index[listIndex] >= lists[listIndex].Count))
                                        {
                                            result = "list index out of range";
                                            code = ReturnCode.Error;
                                            break;
                                        }

                                        value = lists[listIndex][index[listIndex]];
                                        argumentIndex++;

                                        if (argumentIndex >= arguments.Count)
                                            break;

                                        //
                                        // NOTE: Advance to handling the next nested list.
                                        //
                                        listIndex++;
                                    }
                                    while (true);
                                }

                                if (code == ReturnCode.Ok)
                                    result = value;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lget varName ?index ...?\"";
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
