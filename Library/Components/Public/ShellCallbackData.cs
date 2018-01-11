/*
 * ShellCallbackData.cs --
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
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("725d7253-4202-43e0-b9d4-c78193b447f8")]
    public sealed class ShellCallbackData : IIdentifier, IShellManager
    {
        #region Private Constructors
        private ShellCallbackData()
        {
            this.kind = IdentifierKind.ShellCallbackData;
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

        private ShellCallbackData(
            ShellCallbackData callbackData
            )
            : this()
        {
            Copy(callbackData, this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ShellCallbackData Create()
        {
            return new ShellCallbackData();
        }

        ///////////////////////////////////////////////////////////////////////

        internal static ShellCallbackData Create(
            ShellCallbackData callbackData
            )
        {
            return new ShellCallbackData(callbackData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IShellManager Members
        private ArgumentCallback argumentCallback;
        public ArgumentCallback ArgumentCallback
        {
            get { return argumentCallback; }
            set { argumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateScriptCallback evaluateScriptCallback;
        public EvaluateScriptCallback EvaluateScriptCallback
        {
            get { return evaluateScriptCallback; }
            set { evaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateFileCallback evaluateFileCallback;
        public EvaluateFileCallback EvaluateFileCallback
        {
            get { return evaluateFileCallback; }
            set { evaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EvaluateEncodedFileCallback evaluateEncodedFileCallback;
        public EvaluateEncodedFileCallback EvaluateEncodedFileCallback
        {
            get { return evaluateEncodedFileCallback; }
            set { evaluateEncodedFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private InteractiveLoopCallback interactiveLoopCallback;
        public InteractiveLoopCallback InteractiveLoopCallback
        {
            get { return interactiveLoopCallback; }
            set { interactiveLoopCallback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Static Methods
        internal static void Copy(
            ShellCallbackData sourceCallbackData,
            ShellCallbackData targetCallbackData
            )
        {
            if ((sourceCallbackData == null) || (targetCallbackData == null))
                return;

            targetCallbackData.ArgumentCallback =
                sourceCallbackData.ArgumentCallback;

            targetCallbackData.EvaluateScriptCallback =
                sourceCallbackData.EvaluateScriptCallback;

            targetCallbackData.EvaluateFileCallback =
                sourceCallbackData.EvaluateFileCallback;

            targetCallbackData.EvaluateEncodedFileCallback =
                sourceCallbackData.EvaluateEncodedFileCallback;

            targetCallbackData.InteractiveLoopCallback =
                sourceCallbackData.InteractiveLoopCallback;

            targetCallbackData.Initialized = sourceCallbackData.Initialized;

            targetCallbackData.HadArgumentCallback =
                sourceCallbackData.HadArgumentCallback;

            targetCallbackData.HadEvaluateScriptCallback =
                sourceCallbackData.HadEvaluateScriptCallback;

            targetCallbackData.HadEvaluateFileCallback =
                sourceCallbackData.HadEvaluateFileCallback;

            targetCallbackData.HadEvaluateEncodedFileCallback =
                sourceCallbackData.HadEvaluateEncodedFileCallback;

            targetCallbackData.HadInteractiveLoopCallback =
                sourceCallbackData.HadInteractiveLoopCallback;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Properties
        private bool initialized;
        internal bool Initialized
        {
            get { return initialized; }
            private set { initialized = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadArgumentCallback;
        internal bool HadArgumentCallback
        {
            get { return hadArgumentCallback; }
            private set { hadArgumentCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadEvaluateScriptCallback;
        internal bool HadEvaluateScriptCallback
        {
            get { return hadEvaluateScriptCallback; }
            private set { hadEvaluateScriptCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadEvaluateFileCallback;
        internal bool HadEvaluateFileCallback
        {
            get { return hadEvaluateFileCallback; }
            private set { hadEvaluateFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadEvaluateEncodedFileCallback;
        internal bool HadEvaluateEncodedFileCallback
        {
            get { return hadEvaluateEncodedFileCallback; }
            private set { hadEvaluateEncodedFileCallback = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hadInteractiveLoopCallback;
        internal bool HadInteractiveLoopCallback
        {
            get { return hadInteractiveLoopCallback; }
            private set { hadInteractiveLoopCallback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Methods
        internal void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            hadArgumentCallback = (argumentCallback != null);
            hadEvaluateScriptCallback = (evaluateScriptCallback != null);
            hadEvaluateFileCallback = (evaluateFileCallback != null);

            hadEvaluateEncodedFileCallback =
                (evaluateEncodedFileCallback != null);

            hadInteractiveLoopCallback = (interactiveLoopCallback != null);
        }

        ///////////////////////////////////////////////////////////////////////

        internal void Refresh(
            ArgumentCallback argumentCallback,
            EvaluateScriptCallback evaluateScriptCallback,
            EvaluateFileCallback evaluateFileCallback,
            EvaluateEncodedFileCallback evaluateEncodedFileCallback,
            InteractiveLoopCallback interactiveLoopCallback
            )
        {
            if (!hadArgumentCallback)
                this.argumentCallback = argumentCallback;

            if (!hadEvaluateScriptCallback)
                this.evaluateScriptCallback = evaluateScriptCallback;

            if (!hadEvaluateFileCallback)
                this.evaluateFileCallback = evaluateFileCallback;

            if (!hadEvaluateEncodedFileCallback)
                this.evaluateEncodedFileCallback = evaluateEncodedFileCallback;

            if (!hadInteractiveLoopCallback)
                this.interactiveLoopCallback = interactiveLoopCallback;
        }

        ///////////////////////////////////////////////////////////////////////

        internal string ToTraceString()
        {
            IStringList list = new StringPairList();

            list.Add("ArgumentCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(argumentCallback)));

            list.Add("EvaluateScriptCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(evaluateScriptCallback)));

            list.Add("EvaluateFileCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(evaluateFileCallback)));

            list.Add("EvaluateEncodedFileCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(evaluateEncodedFileCallback)));

            list.Add("InteractiveLoopCallback", FormatOps.WrapOrNull(
                FormatOps.DelegateName(interactiveLoopCallback)));

            list.Add("Initialized", initialized.ToString());
            list.Add("HadArgumentCallback", hadArgumentCallback.ToString());

            list.Add("HadEvaluateScriptCallback",
                hadEvaluateScriptCallback.ToString());

            list.Add("HadEvaluateFileCallback",
                hadEvaluateFileCallback.ToString());

            list.Add("HadEvaluateEncodedFileCallback",
                hadEvaluateEncodedFileCallback.ToString());

            list.Add("HadInteractiveLoopCallback",
                hadInteractiveLoopCallback.ToString());

            return list.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
