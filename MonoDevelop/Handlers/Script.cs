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

///////////////////////////////////////////////////////////////////////////////
//    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this code, it is a proof-of-concept only.  It is not
// production ready.
//
//    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Handlers
{
    #region Public Enumerations
    [ObjectId("02ed5449-bbb2-44ed-81f2-b5218b73c3e1")]
    public enum Commands
    {
        None,
        EvaluateExpression,
        EvaluateScript,
        EvaluateFile,
        SubstituteString,
        SubstituteFile
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////

    [ObjectId("eb1b3980-d45d-4e96-a243-8051878d2254")]
    internal class Script : CommandHandler, IDisposable
    {
        #region Private Constants
        //
        // NOTE: By default, no console.
        //
        private static readonly bool DefaultConsole = false;

        //
        // NOTE: By default:
        //
        //       1. We want to initialize the interpreter library.
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private TraceListener listener;
        private CreateFlags createFlags;
        private Interpreter interpreter;
        private bool replace; // replace selected text?
        private bool console;
        private bool exceptions;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Script()
            : base()
        {
            //
            // NOTE: Can we assume that the console host is available?
            //
            console = NeedConsole(DefaultConsole);

            //
            // NOTE: Get the effective interpreter creation flags from the
            //       environment, etc.
            //
            createFlags = Interpreter.GetStartupCreateFlags(
                null, DefaultCreateFlags, OptionOriginFlags.Standard,
                console, true);

            //
            // NOTE: By default, we do not want to allow "exceptional"
            //       (non-Ok) success return codes.
            //
            exceptions = false;

            //
            // NOTE: By default, replace the selected text (i.e. an Eagle
            //       script snippet) with the result of the evaluation or
            //       substitution.
            //
            replace = true;

            //
            // NOTE: Ok, attempt to create and initialize the interpreter
            //       now.
            //
            Initialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Helper Methods (Update)
        protected virtual void UpdateText(
            CommandInfo info
            )
        {
            IEditableTextBuffer buffer = GetEditableTextBuffer();

            info.Enabled = (buffer != null) &&
                !String.IsNullOrEmpty(buffer.SelectedText);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void UpdateDocument(
            CommandInfo info
            )
        {
            if (info != null)
                info.Enabled = !String.IsNullOrEmpty(
                    GetActiveDocumentFileName());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Helper Methods (Update / Run)
        protected virtual IEditableTextBuffer GetEditableTextBuffer()
        {
            Workbench workbench = IdeApp.Workbench;

            if (workbench != null)
            {
                Document document = workbench.ActiveDocument;

                if (document != null)
                    return document.GetContent<IEditableTextBuffer>();
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual FilePath GetActiveDocumentFileName()
        {
            Workbench workbench = IdeApp.Workbench;

            if (workbench != null)
            {
                Document document = workbench.ActiveDocument;

                if (document != null)
                    return document.FileName;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Engine Helper Methods (Run)
        protected virtual ReturnCode EvaluateExpression(
            string text,
            ref Result result
            )
        {
            if (interpreter != null)
                return interpreter.EvaluateExpression(text, ref result);
            else
                result = "invalid interpreter";

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateScript(
            string text,
            ref Result result
            )
        {
            if (interpreter != null)
                return interpreter.EvaluateScript(text, ref result);
            else
                result = "invalid interpreter";

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode EvaluateFile(
            string fileName,
            ref Result result
            )
        {
            if (interpreter != null)
                return interpreter.EvaluateFile(fileName, ref result);
            else
                result = "invalid interpreter";

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SubstituteString(
            string text,
            ref Result result
            )
        {
            if (interpreter != null)
                return interpreter.SubstituteString(text, ref result);
            else
                result = "invalid interpreter";

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode SubstituteFile(
            string fileName,
            ref Result result
            )
        {
            if (interpreter != null)
                return interpreter.SubstituteFile(fileName, ref result);
            else
                result = "invalid interpreter";

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Result Handling Methods (Run)
        protected virtual bool IsSuccess(
            ReturnCode code
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void HandleBufferResult(
            IEditableTextBuffer buffer,
            Result result
            )
        {
            if (buffer == null)
                return;

            buffer.BeginAtomicUndo();

            try
            {
                buffer.SelectedText = result;
            }
            finally
            {
                buffer.EndAtomicUndo();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual Document HandleDocumentResult(
            Type type,
            ReturnCode code,
            Result result
            )
        {
            Workbench workbench = IdeApp.Workbench;

            if (workbench != null)
            {
                return workbench.NewDocument(String.Format(
                    "Eagle {0}{1} #{2}",
                    (type != null) ?
                        String.Format("{0} ", type.Name) :
                        String.Empty,
                    IsSuccess(code) ? "Result" : "Error",
                    NextId()),
                ContentType.Text, result);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void HandleResult(
            Type type,
            IEditableTextBuffer buffer,
            ReturnCode code,
            Result result
            )
        {
            if (buffer == null)
                return;

            if (replace)
                HandleBufferResult(buffer, result);
            else
                HandleDocumentResult(type, code, result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private void Initialize()
        {
            //
            // NOTE: Create and initialize the interpreter.
            //
            ReturnCode code;
            Result result = null;

            interpreter = Interpreter.Create(
                null, createFlags, ref result); /* throw */

            if (interpreter != null)
            {
                code = Interpreter.ProcessStartupOptions(interpreter,
                    null, createFlags, OptionOriginFlags.Standard, console,
                    true, ref result);
            }
            else
            {
                code = ReturnCode.Error;
            }

            if (code != ReturnCode.Ok)
                throw new ScriptException(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        private long NextId()
        {
            return (interpreter != null) ? interpreter.NextId() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool NeedConsole(
            bool @default
            )
        {
            //
            // HACK: By default, assume that a console-based host is not
            //       available.  Then, attempt to check and see if the
            //       user believes that one is available.  We use this
            //       very clumsy method because MonoDevelop does not seem
            //       to expose an easy way for us to determine if we have
            //       a console-like host available to output diagnostic
            //       [and other] information to.
            //
            try
            {
                if (@default)
                {
                    if (Utility.GetEnvironmentVariable(
                            EnvVars.NoConsole, true, false) != null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (Utility.GetEnvironmentVariable(
                            EnvVars.Console, true, false) != null)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Trace Listener Helper Methods
        private ReturnCode SetupTraceListeners(
            bool setup,
            bool console,
            bool strict
            )
        {
            Result error = null;

            return SetupTraceListeners(setup, console, strict, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode SetupTraceListeners(
            bool setup,
            bool console,
            bool strict,
            ref Result error
            )
        {
            try
            {
                if (setup)
                {
                    //
                    // NOTE: Add our trace listener to the collections for
                    //       trace and debug output.
                    //
                    if (listener == null)
                    {
                        listener = Utility.NewDefaultTraceListener(console);

                        /* IGNORED */
                        Utility.AddTraceListener(listener, false);

                        /* IGNORED */
                        Utility.AddTraceListener(listener, true);

                        return ReturnCode.Ok; // NOTE: Success.
                    }
                    else if (strict)
                    {
                        error = "trace listeners already setup";
                    }
                    else
                    {
                        return ReturnCode.Ok; // NOTE: Fake success.
                    }
                }
                else
                {
                    //
                    // NOTE: Remove and dispose our trace listeners now.
                    //
                    if (listener != null)
                    {
                        /* IGNORED */
                        Utility.RemoveTraceListener(listener, true);

                        /* IGNORED */
                        Utility.RemoveTraceListener(listener, false);

                        listener.Dispose();
                        listener = null;

                        return ReturnCode.Ok; // NOTE: Success.
                    }
                    else if (strict)
                    {
                        error = "trace listeners not setup";
                    }
                    else
                    {
                        return ReturnCode.Ok; // NOTE: Fake success.
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(Script).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (interpreter != null)
                    {
                        interpreter.Dispose(); /* throw */
                        interpreter = null;
                    }

                    SetupTraceListeners(false, console, false);
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~Script()
        {
            Dispose(false);
        }
        #endregion
    }
}
