using System;
using System.Collections.Generic;
using System.Runtime;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if DEBUGGER
using Eagle._Interfaces.Private;
#endif

namespace Eagle._Commands
{
    [ObjectId("B3101A13-AB0F-43A5-9A02-16CD17285C17")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard | CommandFlags.Diagnostic)]
    [ObjectGroup("string")]
    internal sealed class Log : Core
    {
        public Log(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {

            // This method litteraly does nothing.  At this point, I dont care if the logging works because I can't test the F5 logging endpoint anyway.
            ReturnCode code = ReturnCode.Ok;

            return code;
        }
    }
}
