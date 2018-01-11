/*
 * Command.cs --
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
using _Public = Eagle._Components.Public;

namespace Eagle._SubCommands
{
    [ObjectId("20783832-9aa4-4b72-8959-b2f6a7fcc6a4")]
    /*
     * NOTE: This command is "safe" because it does not accomplish anything by
     *       itself; instead, it just evaluates the configured script command.
     *       If the interpreter is marked as "safe", using this class will not
     *       permit the evaluated script to escape those restrictions.
     */
    [CommandFlags(CommandFlags.Safe | CommandFlags.SubCommand)]
    [ObjectGroup("engine")]
    internal sealed class Command : Default
    {
        #region Private Data
        //
        // NOTE: The script command to evaluate when this sub-command instance
        //       is executed.
        //
        private StringList scriptCommand;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Command(
            ISubCommandData subCommandData
            )
            : base(subCommandData)
        {
            SetupForSubCommandExecute(this.ClientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void SetupForSubCommandExecute(
            IClientData clientData
            )
        {
            object data = null;

            clientData = _Public.ClientData.UnwrapOrReturn(
                clientData, ref data);

            scriptCommand = data as StringList;
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList GetArgumentsForExecute(
            ArgumentList arguments
            )
        {
            SubCommandFlags subCommandFlags = this.Flags;

            if (FlagOps.HasFlags(subCommandFlags,
                    SubCommandFlags.UseExecuteArguments, true))
            {
                return arguments;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} option ?arg ...?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            string subCommand = arguments[1];

            if (!String.Equals(
                    subCommand, this.Name,
                    StringOps.SystemStringComparisonType))
            {
                result = ScriptOps.BadSubCommand(
                    interpreter, null, null, subCommand, this, null, null);

                return ReturnCode.Error;
            }

            //
            // NOTE: Evaluate the configured script command, maybe
            //       adding all the local arguments, and return the
            //       results verbatim.
            //
            string name = StringList.MakeList(this.Name);

            ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                CallFrameFlags.Evaluate | CallFrameFlags.SubCommand);

            interpreter.PushAutomaticCallFrame(frame);

            ReturnCode code = interpreter.EvaluateScript(
                ScriptOps.GetArgumentsForExecute(this, scriptCommand,
                GetArgumentsForExecute(arguments), 0), 0, ref result);

            if (code == ReturnCode.Error)
            {
                Engine.AddErrorInformation(interpreter, result,
                    String.Format("{0}    (\"{1}\" body line {2})",
                        Environment.NewLine, ScriptOps.GetNameForExecute(
                        arguments[0], this), Interpreter.GetErrorLine(
                        interpreter)));
            }

            //
            // NOTE: Pop the original call frame that we pushed above and
            //       any intervening scope call frames that may be leftover
            //       (i.e. they were not explicitly closed).
            //
            /* IGNORED */
            interpreter.PopScopeCallFramesAndOneMore();
            return code;
        }
        #endregion
    }
}
