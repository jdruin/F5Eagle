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
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Procedures
{
    [ObjectId("64ea360e-9474-4f70-9ae8-1e363c3a156e")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IProcedure
    {
        #region Public Constructors
        public Default(
            IProcedureData procedureData
            )
        {
            kind = IdentifierKind.Procedure;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroup(this);

            if (procedureData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, procedureData.Group);

                name = procedureData.Name;
                description = procedureData.Description;
                flags = procedureData.Flags;
                clientData = procedureData.ClientData;
                arguments = procedureData.Arguments;
                body = procedureData.Body;
                location = procedureData.Location;
                token = procedureData.Token;
            }

            callback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ?
                StringList.MakeList(FormatOps.TypeName(GetType()), name) :
                base.ToString();
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

        #region IUsageData Members
        private long usageCount;

        public virtual bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            if (type != UsageType.Count)
                return false;

            value = usageCount;
            usageCount = 0;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            if (type != UsageType.Count)
                return false;

            value = usageCount;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            if (type != UsageType.Count)
                return false;

            LogicOps.Swap(ref value, ref usageCount);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual bool AddUsage(
            UsageType type,
            long value
            )
        {
            if (type != UsageType.Count)
                return false;

            usageCount += value;

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        private ProcedureFlags flags;
        public virtual ProcedureFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string body;
        public virtual string Body
        {
            get { return body; }
            set { body = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IScriptLocation location;
        public virtual IScriptLocation Location
        {
            get { return location; }
            set { location = value; }
        }
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

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecute Members
        private ExecuteCallback callback;
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
