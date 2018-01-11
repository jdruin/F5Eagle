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
using System.Collections;

#if CAS_POLICY
using System.Security.Cryptography;
using System.Security.Policy;
#endif

#if XML
using System.Xml;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("b2975958-ed3b-4d1d-8540-0ff4c297110d")]
    public sealed class Script :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IScript /* THREAD-SAFE */
    {
        #region Private Code Access Security Constants
#if CAS_POLICY
        private static readonly Evidence DefaultEvidence = null;
        private static readonly byte[] DefaultHashValue = null;
        private static readonly HashAlgorithm DefaultHashAlgorithm = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Script(
            Guid id,
            string name,
            string group,
            string description,
            string type,
            string text,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
#if XML
            XmlBlockType blockType,
            DateTime timeStamp,
            string publicKeyToken,
            byte[] signature,
#endif
#if CAS_POLICY
            Evidence evidence,
            byte[] hashValue,
            HashAlgorithm hashAlgorithm,
#endif
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData
            )
        {
            this.kind = IdentifierKind.Script;
            this.id = id;
            this.name = name;
            this.group = group;
            this.description = description;
            this.type = type;
            this.parts = null; /* TODO: If we ever support scripts with multiple
                                *       parts, change this to be a formal argument
                                *       to this constructor and remove text? */
            this.text = text;
            this.fileName = fileName;
            this.startLine = startLine;
            this.endLine = endLine;
            this.viaSource = viaSource;

#if XML
            this.blockType = blockType;
            this.timeStamp = timeStamp;
            this.publicKeyToken = publicKeyToken;
            this.signature = signature;
#endif

#if CAS_POLICY
            this.evidence = evidence;
            this.hashValue = hashValue;
            this.hashAlgorithm = hashAlgorithm;
#endif

            this.engineMode = engineMode;
            this.scriptFlags = scriptFlags;
            this.engineFlags = engineFlags;
            this.substitutionFlags = substitutionFlags;
            this.expressionFlags = expressionFlags;
            this.eventFlags = eventFlags;
            this.clientData = clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Members
        public static IScript Create(
            string text
            )
        {
            return Create(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string text,
            IClientData clientData
            )
        {
            return Create(
                ScriptTypes.Invalid, text, TimeOps.GetUtcNow(), clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string type,
            string text,
            DateTime timeStamp,
            IClientData clientData
            )
        {
            return Create(
                null, null, null, type, text, timeStamp,
                EngineMode.EvaluateScript, ScriptFlags.None, EngineFlags.None,
                SubstitutionFlags.Default, EventFlags.None, ExpressionFlags.Default,
                clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        /* INTERNAL STATIC OK */
        internal static IScript CreateForPolicy(
            string name,
            string type,
            string text,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags
            )
        {
            return Create(
                name, null, null, type, text, TimeOps.GetUtcNow(),
                EngineMode.EvaluateScript, ScriptFlags.None,
                engineFlags, substitutionFlags, eventFlags,
                expressionFlags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string name,
            string group,
            string description,
            string type,
            string text,
            DateTime timeStamp,
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData
            )
        {
            return Create(
                name, group, description, type, text, null, Parser.UnknownLine,
                Parser.UnknownLine, false, timeStamp, engineMode, scriptFlags,
                engineFlags, substitutionFlags, eventFlags, expressionFlags,
                clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IScript Create(
            string name,
            string group,
            string description,
            string type,
            string text,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
            DateTime timeStamp,
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData
            )
        {
            return PrivateCreate(
                Guid.Empty, name, group, description, type, text, fileName,
                startLine, endLine, viaSource,
#if XML
                XmlBlockType.None, timeStamp, null, null,
#endif
                engineMode, scriptFlags, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        /* INTERNAL STATIC OK */
        internal static IScript CreateFromXmlNode( /* NOTE: Engine use only. */
            string type,
            XmlNode node,
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData,
            ref Result error
            )
        {
            try
            {
                XmlElement element = node as XmlElement;

                if (element != null)
                {
                    foreach (string attribute in _XmlAttribute.RequiredList)
                    {
                        if ((attribute != null) && !element.HasAttribute(attribute))
                        {
                            error = String.Format(
                                "missing required attribute \"{0}\"",
                                attribute);

                            return null;
                        }
                    }

                    /* REQUIRED */
                    Guid id = element.HasAttribute(_XmlAttribute.Id) ?
                        new Guid(element.GetAttribute(_XmlAttribute.Id)) : Guid.Empty;

                    /* REQUIRED */
                    XmlBlockType blockType = element.HasAttribute(_XmlAttribute.Type) ?
                        (XmlBlockType)Enum.Parse(typeof(XmlBlockType),
                            element.GetAttribute(_XmlAttribute.Type), true) :
                            XmlBlockType.None;

                    /* REQUIRED */
                    string text = element.InnerText;

                    /* OPTIONAL */
                    string name = element.HasAttribute(_XmlAttribute.Name) ?
                        element.GetAttribute(_XmlAttribute.Name) : null;

                    /* OPTIONAL */
                    string group = element.HasAttribute(_XmlAttribute.Group) ?
                        element.GetAttribute(_XmlAttribute.Group) : null;

                    /* OPTIONAL */
                    string description = element.HasAttribute(_XmlAttribute.Description) ?
                        element.GetAttribute(_XmlAttribute.Description) : null;

                    /* OPTIONAL */
                    DateTime timeStamp = element.HasAttribute(_XmlAttribute.TimeStamp) ?
                        DateTime.Parse(element.GetAttribute(_XmlAttribute.TimeStamp)).ToUniversalTime() :
                        DateTime.MinValue;

                    /* OPTIONAL */
                    string publicKeyToken = element.HasAttribute(_XmlAttribute.PublicKeyToken) ?
                        element.GetAttribute(_XmlAttribute.PublicKeyToken) : null;

                    /* OPTIONAL */
                    byte[] signature = element.HasAttribute(_XmlAttribute.Signature) ?
                        Convert.FromBase64String(
                            element.GetAttribute(_XmlAttribute.Signature)) : null;

                    //
                    // NOTE: Create the script using the values extracted from the XML element.
                    //
                    return PrivateCreate(
                        id, name, group, description, type, text, null, Parser.UnknownLine,
                        Parser.UnknownLine, false, blockType, timeStamp, publicKeyToken,
                        signature, engineMode, scriptFlags, engineFlags, substitutionFlags,
                        eventFlags, expressionFlags, clientData);
                }
                else
                {
                    error = "xml node is not an element";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static IScript PrivateCreate(
            Guid id,
            string name,
            string group,
            string description,
            string type,
            string text,
            string fileName,
            int startLine,
            int endLine,
            bool viaSource,
#if XML
            XmlBlockType blockType,
            DateTime timeStamp,
            string publicKeyToken,
            byte[] signature,
#endif
            EngineMode engineMode,
            ScriptFlags scriptFlags,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            IClientData clientData
            )
        {
            return new Script(
                id, name, group, description, type, text, fileName,
                startLine, endLine, viaSource,
#if XML
                blockType, timeStamp, publicKeyToken, signature,
#endif
#if CAS_POLICY
                DefaultEvidence,
                DefaultHashValue,
                DefaultHashAlgorithm,
#endif
                engineMode, scriptFlags, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, clientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerator Class
        [ObjectId("10883ee1-ca0c-44d0-89f9-2cdf26517ca1")]
        private sealed class ScriptEnumerator : IEnumerator
        {
            #region Private Data
            private IScript script;
            private int position;
            #endregion

            ///////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ScriptEnumerator(
                IScript script
                )
            {
                if (script == null)
                    throw new ArgumentNullException("script");

                this.script = script;

                Reset();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////

            #region IEnumerator Members
            public object Current
            {
                get
                {
                    if (script != null)
                    {
                        lock (script.SyncRoot)
                        {
                            //
                            // TODO: If we ever support scripts with multiple
                            //       parts, change this to do proper indexing.
                            //
                            if (position < script.Count)
                                return script.Text; /* Immutable, Deep Copy */
                            else
                                throw new InvalidOperationException();
                        }
                    }
                    else
                        throw new InvalidOperationException();
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public bool MoveNext()
            {
                position++;

                if (script != null)
                {
                    lock (script.SyncRoot)
                    {
                        return position < script.Count;
                    }
                }
                else
                    return false;
            }

            ///////////////////////////////////////////////////////////////////////

            public void Reset()
            {
                position = Index.Invalid;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnumerable Members
        public IEnumerator GetEnumerator()
        {
            return new ScriptEnumerator(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICollection Members
        public int Count
        {
            //
            // TODO: If we ever support scripts with multiple
            //       parts, change this to return the proper
            //       count.
            //
            get { return 1; } // A collection of one.
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSynchronized
        {
            get { return false; } // must lock manually.
        }

        ///////////////////////////////////////////////////////////////////////

        private readonly object syncRoot = new object();
        public object SyncRoot
        {
            get { return syncRoot; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void CopyTo(
            Array array,
            int index
            )
        {
            if (array == null)
                throw new ArgumentNullException();

            if (index < 0)
                throw new ArgumentOutOfRangeException();

            if (array.Rank != 1)
                throw new ArgumentException();

            int length = array.Length;

            if (index >= length)
                throw new ArgumentException();

            int count = this.Count;

            if ((index + count) > length)
                throw new ArgumentException();

            array.SetValue(text, index);
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

        #region IScriptLocation Members
        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int startLine;
        public int StartLine
        {
            get { return startLine; }
            set { startLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int endLine;
        public int EndLine
        {
            get { return endLine; }
            set { endLine = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool viaSource;
        public bool ViaSource
        {
            get { return viaSource; }
            set { viaSource = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList ToList()
        {
            return ToList(false);
        }

        ///////////////////////////////////////////////////////////////////////

        public StringPairList ToList(
            bool scrub
            )
        {
            StringPairList list = new StringPairList();

            list.Add("type", type);
            list.Add("text", text);

            list.Add("fileName", scrub ? PathOps.ScrubPath(
                GlobalState.GetBasePath(), fileName) : fileName);

            list.Add("startLine", startLine.ToString());
            list.Add("endLine", endLine.ToString());
            list.Add("viaSource", viaSource.ToString());

            list.Add("engineMode", engineMode.ToString());
            list.Add("scriptFlags", scriptFlags.ToString());
            list.Add("engineFlags", engineFlags.ToString());
            list.Add("substitutionFlags", substitutionFlags.ToString());
            list.Add("eventFlags", eventFlags.ToString());
            list.Add("expressionFlags", expressionFlags.ToString());

#if XML
            list.Add("blockType", blockType.ToString());
            list.Add("timeStamp", timeStamp.ToString());
            list.Add("publicKeyToken", publicKeyToken);
            list.Add("signature", ArrayOps.ToHexadecimalString(signature));
#endif

#if CAS_POLICY
            if (!scrub)
            {
                list.Add("evidence", (evidence != null) ?
                    evidence.ToString() : null);

                list.Add("hashValue", ArrayOps.ToHexadecimalString(hashValue));

                list.Add("hashAlgorithm", (hashAlgorithm != null) ?
                    hashAlgorithm.ToString() : null);
            }
#endif

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IScript Members
        private string type;
        public string Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IList parts;
        public IList Parts
        {
            get { return parts; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            //
            // TODO: If we ever support scripts with multiple
            //       parts, change this to combine all the
            //       parts into one piece of text?
            //
            get { return text; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        private XmlBlockType blockType;
        public XmlBlockType BlockType
        {
            get { return blockType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime timeStamp;
        public DateTime TimeStamp
        {
            get { return timeStamp; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string publicKeyToken;
        public string PublicKeyToken
        {
            get { return publicKeyToken; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] signature;
        public byte[] Signature
        {
            get { return signature; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        private Evidence evidence;
        public Evidence Evidence
        {
            get { return evidence; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] hashValue;
        public byte[] HashValue
        {
            get { return hashValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        private HashAlgorithm hashAlgorithm;
        public HashAlgorithm HashAlgorithm
        {
            get { return hashAlgorithm; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private EngineMode engineMode;
        public EngineMode EngineMode
        {
            get { return engineMode; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptFlags scriptFlags;
        public ScriptFlags ScriptFlags
        {
            get { return scriptFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EngineFlags engineFlags;
        public EngineFlags EngineFlags
        {
            get { return engineFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SubstitutionFlags substitutionFlags;
        public SubstitutionFlags SubstitutionFlags
        {
            get { return substitutionFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private EventFlags eventFlags;
        public EventFlags EventFlags
        {
            get { return eventFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ExpressionFlags expressionFlags;
        public ExpressionFlags ExpressionFlags
        {
            get { return expressionFlags; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList(text, type);
        }
        #endregion
    }
}
