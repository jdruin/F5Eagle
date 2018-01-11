/*
 * Default.cs --
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

#if DEBUGGER && DEBUGGER_ARGUMENTS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Public;

namespace Eagle._Objects
{
    [ObjectId("51d9a798-e19c-479f-b5c3-98459cb21415")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IObject
    {
        #region Public Constructors
        public Default(
            IObjectData objectData,
            object value,
            IClientData valueData
            )
        {
            kind = IdentifierKind.Object;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroup(this);

            if (objectData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, objectData.Group);

                name = objectData.Name;
                description = objectData.Description;
                clientData = objectData.ClientData;
                type = objectData.Type;
                objectFlags = objectData.ObjectFlags;
                referenceCount = objectData.ReferenceCount;
                temporaryReferenceCount = objectData.TemporaryReferenceCount;

#if NATIVE && TCL
                interpName = objectData.InterpName;
#endif

#if DEBUGGER && DEBUGGER_ARGUMENTS
                executeArguments = objectData.ExecuteArguments;
#endif

                token = objectData.Token;
            }

            this.value = value;
            this.valueData = valueData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IValue Members
        private object value;
        public virtual object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData valueData;
        public virtual IClientData ValueData
        {
            get { return valueData; }
            set { valueData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData extraData;
        public virtual IClientData ExtraData
        {
            get { return extraData; }
            set { extraData = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string String
        {
            get { return (value != null) ? value.ToString() : null; }
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual int Length
        {
            get { return (value != null) ? value.ToString().Length : 0; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveObjectFlags Members
        private ObjectFlags objectFlags;
        public virtual ObjectFlags ObjectFlags
        {
            get { return objectFlags; }
            set { objectFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectData Members
        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int referenceCount;
        public virtual int ReferenceCount
        {
            get { return referenceCount; }
            set { referenceCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int temporaryReferenceCount;
        public virtual int TemporaryReferenceCount
        {
            get { return temporaryReferenceCount; }
            set { temporaryReferenceCount = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        private string interpName;
        public virtual string InterpName
        {
            get { return interpName; }
            set { interpName = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_ARGUMENTS
        private ArgumentList executeArguments;
        public virtual ArgumentList ExecuteArguments
        {
            get { return executeArguments; }
            set { executeArguments = value; }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion
    }
}
