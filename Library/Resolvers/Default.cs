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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Resolvers
{
    [ObjectId("fd02ce56-fef3-4932-9d1e-22e6115a362e")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IResolve
    {
        #region Public Constructors
        public Default(
            IResolveData resolveData
            )
        {
            kind = IdentifierKind.Resolve;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroup(this);

            if (resolveData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, resolveData.Group);

                name = resolveData.Name;
                description = resolveData.Description;
                clientData = resolveData.ClientData;
                interpreter = resolveData.Interpreter;
                token = resolveData.Token;
            }
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

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
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

        #region IResolve Members
        public virtual ReturnCode GetVariableFrame(
            ref ICallFrame frame,
            ref string varName,
            ref VariableFlags flags,
            ref Result error
            )
        {
            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetCurrentNamespace(
            ICallFrame frame,
            ref INamespace @namespace,
            ref Result error
            )
        {
            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetIExecute(
            ICallFrame frame,
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref long token,
            ref IExecute execute,
            ref Result error
            )
        {
            error = "not implemented";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual ReturnCode GetVariable(
            ICallFrame frame,
            string varName,
            string varIndex,
            ref VariableFlags flags,
            ref IVariable variable,
            ref Result error
            )
        {
            error = "not implemented";
            return ReturnCode.Error;
        }
        #endregion
    }
}
