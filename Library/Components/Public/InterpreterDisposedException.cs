/*
 * InterpreterDisposedException.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if SERIALIZATION
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("59912fbe-86a1-4df9-bbf9-117bd0a8ff4d")]
    public class InterpreterDisposedException :
            ObjectDisposedException, IGetInterpreter
    {
        #region Private Data
#if SERIALIZATION
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public InterpreterDisposedException()
            : this((string)null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            string objectName
            )
            : base(objectName)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            string objectName,
            string message
            )
            : base(objectName, message)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            Interpreter interpreter,
            string objectName,
            string message
            )
            : this(objectName, message)
        {
            SetInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            Interpreter interpreter,
            string message,
            Exception innerException
            )
            : this(message, innerException)
        {
            SetInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            Type type
            )
            : this(null, type)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            Interpreter interpreter,
            Type type
            )
            : this(interpreter, (type != null) ? type.Name : null, (string)null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public InterpreterDisposedException(
            Interpreter interpreter,
            Type type,
            string message
            )
            : this(interpreter, (type != null) ? type.Name : null, message)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        protected InterpreterDisposedException(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        protected void SetInterpreter(
            Interpreter interpreter
            )
        {
            //
            // NOTE: If the provided interpreter has been disposed, use it.
            //
            if ((interpreter != null) && interpreter.Disposed)
            {
                this.interpreter = interpreter;
                return;
            }

            //
            // NOTE: Otherwise, grab the active interpreter and check if it
            //       has been disposed.  If so, use it.
            //
            interpreter = Interpreter.GetActive();

            if ((interpreter != null) && interpreter.Disposed)
            {
                this.interpreter = interpreter;
                return;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Runtime.Serialization.ISerializable Members
#if SERIALIZATION
        [SecurityPermission(
            SecurityAction.LinkDemand,
            Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context
            )
        {
            base.GetObjectData(info, context);
        }
#endif
        #endregion
    }
}
