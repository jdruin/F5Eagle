/*
 * InterpreterHelper.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("51eaee80-f84c-438b-b4b5-a5f9e6dc1bca")]
    // [ObjectFlags(ObjectFlags.AutoDispose)]
    public sealed class InterpreterHelper :
            ScriptMarshalByRefObject, IGetInterpreter, IDisposable
    {
        #region Private Constants
        private static readonly AssemblyName assemblyName =
            GlobalState.GetAssemblyName();

        ///////////////////////////////////////////////////////////////////////

        private static readonly string typeName =
            typeof(InterpreterHelper).FullName;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        //
        // NOTE: For use by the associated InterpreterHelper.Create method
        //       overload ONLY.
        //
        private InterpreterHelper(
            InterpreterSettings interpreterSettings,
            bool strict,
            ref Result result
            )
        {
            interpreter = Interpreter.Create(
                interpreterSettings, strict, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the associated InterpreterHelper.Create method
        //       overload ONLY.
        //
        private InterpreterHelper(
            IEnumerable<string> args,
            CreateFlags createFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            string text,
            string libraryPath,
            StringList autoPathList,
            ref Result result
            )
        {
            interpreter = Interpreter.Create(
                args, createFlags, initializeFlags, scriptFlags,
                interpreterFlags, text, libraryPath, autoPathList,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static InterpreterHelper Create(
            AppDomain appDomain,
            InterpreterSettings interpreterSettings,
            bool strict,
            ref Result result
            )
        {
            if (appDomain == null)
            {
                result = "invalid application domain";
                return null;
            }

            if (assemblyName == null)
            {
                result = "invalid assembly name";
                return null;
            }

            if (typeName == null)
            {
                result = "invalid type name";
                return null;
            }

            try
            {
                object[] ctorArgs = { interpreterSettings, strict, result };

                InterpreterHelper interpreterHelper =
                    (InterpreterHelper)appDomain.CreateInstanceAndUnwrap(
                        assemblyName.ToString(), typeName, false,
                        MarshalOps.PrivateCreateInstanceBindingFlags,
                        null, ctorArgs, null, null, null);

                //
                // NOTE: Grab the result as it may have been modified.
                //
                result = ctorArgs[ctorArgs.Length - 1] as Result;

                return interpreterHelper;
            }
            catch (Exception e)
            {
                result = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /* NOTE: For use by [test2] and CreateSlaveInterpreter ONLY. */
        internal static InterpreterHelper Create(
            AppDomain appDomain,
            IEnumerable<string> args,
            CreateFlags createFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            InterpreterFlags interpreterFlags,
            string text,
            string libraryPath,
            StringList autoPathList,
            ref Result result
            )
        {
            if (appDomain == null)
            {
                result = "invalid application domain";
                return null;
            }

            if (assemblyName == null)
            {
                result = "invalid assembly name";
                return null;
            }

            if (typeName == null)
            {
                result = "invalid type name";
                return null;
            }

            try
            {
                object[] ctorArgs = {
                    args, createFlags, initializeFlags, scriptFlags,
                    interpreterFlags, text, libraryPath, autoPathList,
                    result
                };

                InterpreterHelper interpreterHelper =
                    (InterpreterHelper)appDomain.CreateInstanceAndUnwrap(
                        assemblyName.ToString(), typeName, false,
                        MarshalOps.PrivateCreateInstanceBindingFlags,
                        null, ctorArgs, null, null, null);

                //
                // NOTE: Grab the result as it may have been modified.
                //
                result = ctorArgs[ctorArgs.Length - 1] as Result;

                return interpreterHelper;
            }
            catch (Exception e)
            {
                result = e;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Loop Methods
#if SHELL
        public ReturnCode InteractiveLoop(
            InteractiveLoopData loopData,
            ref Result result
            )
        {
            CheckDisposed();

            return Interpreter.InteractiveLoop(
                interpreter, loopData, ref result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(InterpreterHelper).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
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
                        interpreter.Dispose();
                        interpreter = null;
                    }
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
        ~InterpreterHelper()
        {
            Dispose(false);
        }
        #endregion
    }
}
