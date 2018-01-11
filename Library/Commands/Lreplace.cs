/*
 * Lreplace.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("5525590c-6517-4ea5-bb16-520ec34a0c4d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lreplace : Core
    {
        public Lreplace(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

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
                    if (arguments.Count >= 4)
                    {
                        StringList list = null;

                        //
                        // WARNING: Cannot cache list representation here, the list
                        //          is modified below.
                        //
                        code = Parser.SplitList(
                            interpreter, arguments[1], 0,
                            Length.Invalid, false, ref list,
                            ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int firstIndex = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[2], list.Count, ValueFlags.AnyIndex,
                                interpreter.CultureInfo, ref firstIndex,
                                ref result);

                            if (code == ReturnCode.Ok)
                            {
                                int lastIndex = Index.Invalid;

                                code = Value.GetIndex(
                                    arguments[3], list.Count, ValueFlags.AnyIndex,
                                    interpreter.CultureInfo, ref lastIndex,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if (firstIndex < 0)
                                        firstIndex = 0;

                                    if ((firstIndex < list.Count) || (list.Count == 0))
                                    {
                                        if (list.Count > 0)
                                        {
                                            if (lastIndex >= list.Count)
                                                lastIndex = list.Count - 1;

                                            int numToDelete;

                                            if (firstIndex <= lastIndex)
                                                numToDelete = (lastIndex - firstIndex + 1);
                                            else
                                                numToDelete = 0;

                                            list.RemoveRange(firstIndex, numToDelete);

                                            if (arguments.Count >= 5)
                                                list.InsertRange(firstIndex, arguments, 4);

                                            result = list;
                                        }
                                        else if (ScriptOps.HasFlags(interpreter,
                                                InterpreterFlags.ReplaceEmptyListOk, true))
                                        {
                                            result = list;
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "list doesn't contain element {0}", arguments[2]);

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "list doesn't contain element {0}", arguments[2]);

                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lreplace list first last ?value ...?\"";
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
