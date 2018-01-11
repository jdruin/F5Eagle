/*
 * Class0.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Commands = Eagle._Commands;

namespace Example
{
    /// <summary>
    /// Declare a "custom command" class that inherits default functionality
    /// and implements the appropriate interface(s).
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("ef4aec44-c64d-45a4-a751-58be49eadc30")]
    [CommandFlags(CommandFlags.Unsafe)]
    [ObjectGroup("example")]
    internal sealed class Class0 : _Commands.Default
    {
        #region Public Constructor (Required)
        /// <summary>
        /// Constructs an instance of this custom command class.
        /// </summary>
        /// <param name="commandData">
        /// An instance of the command data class containing the properties
        /// used to initialize the new instance of this custom command class.
        /// Typically, the only thing done with this parameter is to pass it
        /// along to the base class constructor.
        /// </param>
        public Class0(
            ICommandData commandData /* in */
            )
            : base(commandData)
        {
            //
            // NOTE: Typically, nothing much is done here.  Any non-trivial
            //       initialization should be done in IState.Initialize and
            //       cleaned up in IState.Terminate.
            //
            this.Flags |= Utility.GetCommandFlags(GetType().BaseType) |
                Utility.GetCommandFlags(this); /* HIGHLY RECOMMENDED */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members (Required)
        /// <summary>
        /// Execute the command and return the appropriate result and return
        /// code.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this command was initially created,
        /// if any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments supplied to this command by the script
        /// being evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain the result of the command.
        /// Upon failure, this must contain an appropriate error message.
        /// If no result is explicitly set by the command, a default result
        /// of null will be used by the engine.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.  Other
        /// return codes may be used to implement custom control structures.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: In truth, almost everything in this method is optional;
            //       however, there are certain conventions present in this
            //       example that custom commands are encouraged to follow.
            //
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                //
                // NOTE: We require a valid argument list.  Most custom
                //       commands will want to do this because it is a fairly
                //       standard safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count != 2)
            {
                //
                // NOTE: All commands and sub-commands (if applicable)
                //       should report the correct syntax as part of their
                //       syntax error messages (HIGHLY RECOMMENDED).
                //
                result = Utility.WrongNumberOfArguments(
                    this, 1, arguments, "string");

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //
            // NOTE: This is where the command should actually do something
            //       useful (i.e. now that all the method parameters have
            //       passed at least basic validity checks).  The requirements
            //       imposed by Eagle for custom command (IExecute)
            //       implementations are:
            //
            //       1. Upon success the Execute method must return the
            //          "ReturnCode.Ok" enumeration value.
            //
            //       2. Upon success, the result of the command, if there is
            //          any, should be stored in the "result" parameter,
            //          passed to this method by reference.  If the command
            //          does not produce a result that is meaningful to the
            //          caller, the value "String.Empty" should be used
            //          instead.
            //
            //       3. Upon failure, the Execute method must return the
            //          "ReturnCode.Error" enumeration value.
            //
            //       4. Upon failure, the error message or exception, if there
            //          is any, must be stored in the "result" parameter,
            //          passed to this method by reference.  Do not use a null
            //          value or the value "String.Empty" for the error
            //          message.
            //
            //       5. All the other "ReturnCode" enumeration values are
            //          reserved for use by custom control structure commands
            //          implemented in managed code (e.g. "Return", "Break",
            //          and "Continue").
            //
            //       The try block here is optional; however, custom commands
            //       are "officially" discouraged from raising exceptions,
            //       especially if they are allowed to escape the Execute
            //       method.
            //
            ///////////////////////////////////////////////////////////////////

            try
            {
                //
                // NOTE: Do useful work here.  This simulates doing "work" by
                //       simply returning the first argument (i.e. the one
                //       after the command name) verbatim.
                //
                result = arguments[1];

                //
                // NOTE: Indicate to the script engine that the command
                //       succeeded (HIGHLY RECOMMENDED).
                //
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                //
                // NOTE: If an actual exception is caught within the custom
                //       command implementation itself, the result should be
                //       set to the exception object (HIGHLY RECOMMENDED).
                //
                result = e;
            }

            //
            // NOTE: Indicate to the script engine that the command
            //       generated some kind of error (HIGHLY RECOMMENDED).
            //
            return ReturnCode.Error;
        }
        #endregion
    }
}
