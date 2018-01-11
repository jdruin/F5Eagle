/*
 * ScriptException.cs --
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

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("30409d09-40ec-488c-be4c-92d769d150a6")]
    public class ScriptException : ApplicationException
    {
        #region Private Data
        private ReturnCode returnCode;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ScriptException()
            : base()
        {
            this.returnCode = ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptException(
            string message
            )
            : base(message)
        {
            this.returnCode = ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
            this.returnCode = ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptException(
            ReturnCode code,
            Result result
            )
            : this(result)
        {
            this.returnCode = code;
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptException(
            ReturnCode code,
            Result result,
            Exception innerException
            )
            : this(result, innerException)
        {
            this.returnCode = code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        protected ScriptException(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            returnCode = (ReturnCode)info.GetInt32("returnCode");
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        public virtual ReturnCode ReturnCode
        {
            get { return returnCode; }
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
            info.AddValue("returnCode", returnCode);

            base.GetObjectData(info, context);
        }
#endif
        #endregion
    }
}
