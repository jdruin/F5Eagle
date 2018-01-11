/*
 * Update.cs --
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
    [ObjectId("9ddc097b-4635-4504-9493-98c25f0baf83")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("event")]
    internal sealed class Update : Core
    {
        public Update(
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
                    if ((arguments.Count == 1) || (arguments.Count == 2))
                    {
                        bool idleTasks = false;

                        if (arguments.Count == 2)
                        {
                            if (String.Compare(arguments[1], "idletasks", StringOps.SystemStringComparisonType) == 0)
                            {
                                idleTasks = true;
                            }
                            else
                            {
                                result = String.Format(
                                    "bad option \"{0}\": must be idletasks", 
                                    arguments[1]);

                                code = ReturnCode.Error;
                            }
                        }

                        if ((code == ReturnCode.Ok) && idleTasks)
                            code = EventOps.Wait(interpreter, 0, true, false, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            code = EventOps.ProcessEvents(
                                interpreter, interpreter.UpdateEventFlags,
                                EventPriority.Update, 0, true, false, ref result);
                        }

                        if ((code == ReturnCode.Ok) && !idleTasks)
                            code = EventOps.Wait(interpreter, 0, true, false, ref result);
                    }
                    else
                    {
                        result = "wrong # args: should be \"update ?idletasks?\"";
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
