/*
 * Script.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Tasks
{
    [ObjectId("557579fe-436f-461c-9d82-4cec1bc09e97")]
    public abstract class Script : Task
    {
        #region Private Constants
        //
        // NOTE: By default:
        //
        //       1. We want to initialize the script library.
        //       2. We want to throw an exception if disposed objects
        //          are accessed.
        //       3. We do not want to change the console title.
        //       4. We do not want to change the console icon.
        //       5. We do not want to intercept the Ctrl-C keypress.
        //       6. We want to throw an exception if interpreter
        //          creation fails.
        //       7. We want to have only directories that actually
        //          exist in the auto-path.
        //
        private static readonly CreateFlags DefaultCreateFlags =
            CreateFlags.EmbeddedUse;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly string codeResultErrorLineAndInfoFormat =
            "{0}, line {2}: {1}" + Environment.NewLine + "{3}";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private string text;
        private CreateFlags createFlags;
        private EngineFlags engineFlags;
        private SubstitutionFlags substitutionFlags;
        private EventFlags eventFlags;
        private ExpressionFlags expressionFlags;
        private bool exceptions;
        private bool showStackTrace;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Data
        protected ReturnCode code;
        protected string result;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        //
        // NOTE: Sets up default values for the properties we use.  The MSBuild 
        //       documentation is not entirely clear about whether or not having 
        //       constructors is allowed; however, it does not appear to forbid 
        //       them.
        //
        protected Script()
        {
            //
            // NOTE: Get the effective interpreter creation flags from the
            //       environment, etc.
            //
            createFlags = Interpreter.GetStartupCreateFlags(null,
                DefaultCreateFlags, OptionOriginFlags.Standard, true, true);

            //
            // NOTE: By default, we do not want any special evaluation flags.
            //
            engineFlags = EngineFlags.None;

            //
            // NOTE: By default, we want all the substitution flags.
            //
            substitutionFlags = SubstitutionFlags.Default;

            //
            // NOTE: By default, we want to handle events targeted at the
            //       engine.
            //
            eventFlags = EventFlags.Default;

            //
            // NOTE: By default, we want all the expression flags.
            //
            expressionFlags = ExpressionFlags.Default;

            //
            // NOTE: By default, we do not want to allow "exceptional" (non-Ok) 
            //       success return codes.
            //
            exceptions = false;

            //
            // NOTE: By default, we want to show the exception stack trace.
            //
            showStackTrace = true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Task Parameters
        [Required()]
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public CreateFlags CreateFlags
        {
            get { return createFlags; }
            set { createFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
            set { engineFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
            set { substitutionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public EventFlags EventFlags 
        {
            get { return eventFlags; }
            set { eventFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
            set { expressionFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public bool Exceptions
        {
            get { return exceptions; }
            set { exceptions = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* [Optional()] */
        public bool ShowStackTrace
        {
            get { return showStackTrace; }
            set { showStackTrace = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Output()]
        public ReturnCode Code
        {
            get { return code; }
            set { code = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Output()]
        public string Result
        {
            get { return result; }
            set { result = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Helper Methods
        #region Interpreter Creation Helper Methods (Execute)
        protected virtual Interpreter CreateInterpreter(
            ref Result result
            )
        {
            return Interpreter.Create(null, createFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode PostCreateInterpreter(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Interpreter.ProcessStartupOptions(interpreter, null,
                createFlags, OptionOriginFlags.Standard, true, true, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Postcondition Helper Methods (Execute)
        protected virtual bool IsSuccess(
            ReturnCode code
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Logging Helper Methods (Execute)
        protected virtual void LogError(
            ReturnCode code,
            Result result
            )
        {
            if (result == null)
                return;

            Log.LogError(null, result.ErrorCode, null, null, result.ErrorLine,
                0, result.ErrorLine, 0, codeResultErrorLineAndInfoFormat, code,
                result, result.ErrorLine, result.ErrorInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void LogErrorFromException(
            Exception exception
            )
        {
            Log.LogErrorFromException(exception, showStackTrace);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Engine Helper Methods (Execute)
        protected virtual ReturnCode EvaluateExpression(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateExpression(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateScript(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateScript(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateFile(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.EvaluateFile(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SubstituteString(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.SubstituteString(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SubstituteFile(
            Interpreter interpreter,
            ref Result result
            )
        {
            return Engine.SubstituteFile(interpreter, text, engineFlags,
                substitutionFlags, eventFlags, expressionFlags, ref result);
        }
        #endregion
        #endregion
    }
}
