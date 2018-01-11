/*
 * ScriptClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("02de061d-be8f-494a-89c1-5c80dc037c6e")]
    public class ScriptClientData : ClientData, IClientData
    {
        #region Public Constructors
        public ScriptClientData(
            object data
            )
            : this(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptClientData(
            object data,
            bool readOnly
            )
            : this(new StringDictionary(), data, readOnly)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ScriptClientData(
            StringDictionary dictionary,
            object data,
            bool readOnly
            )
            : base(data, readOnly)
        {
            this.dictionary = dictionary;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private StringDictionary dictionary;
        public virtual StringDictionary Dictionary
        {
            get { return dictionary; }
            set
            {
                if (base.ReadOnly)
                    throw new ScriptException("dictionary is read-only");

                dictionary = value;
            }
        }
        #endregion
    }
}
