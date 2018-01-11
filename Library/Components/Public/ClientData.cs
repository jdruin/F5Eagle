/*
 * ClientData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("149c6f50-7596-4f71-861c-aa1ac700aed7")]
    public class ClientData :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IClientData
    {
        #region Public Constants
        public static readonly IClientData Empty = new ClientData(null, true);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ClientData(
            object data
            )
            : this(data, false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ClientData(
            object data,
            bool readOnly
            )
        {
            this.data = data;
            this.readOnly = readOnly;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IClientData Members
        private object data;
        public virtual object Data
        {
            get { return data; }
            set
            {
                if (readOnly)
                    throw new ScriptException("data is read-only");

                data = value;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private bool readOnly;
        public virtual bool ReadOnly
        {
            get { return readOnly; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static IClientData WrapOrReplace(
            IClientData clientData,
            object data
            )
        {
            //
            // NOTE: If the original IClientData instance contains any data,
            //       wrap it, along with the new data, in an outer instance.
            //       Otherwise, simply create and return a new IClientData
            //       instance with the new data.
            //
            if (HasData(clientData))
            {
                return new ClientData(new AnyPair<IClientData, object>(
                    clientData, data), IsReadOnly(clientData));
            }
            else
            {
                return new ClientData(data, IsReadOnly(clientData));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static IClientData UnwrapOrReturn(
            IClientData clientData,
            ref object data
            )
        {
            object localData = null;

            //
            // NOTE: Does the IClientData instance have any data at all?
            //
            if (HasData(clientData, ref localData))
            {
                //
                // NOTE: Is it wrapping another IClientData instance?
                //
                IAnyPair<IClientData, object> anyPair =
                    localData as IAnyPair<IClientData, object>;

                if (anyPair != null)
                {
                    //
                    // NOTE: Return the wrapped data.  In this case, the
                    //       original data can still be used by the caller
                    //       if they extract it from the original (outer)
                    //       IClientData instance.
                    //
                    data = anyPair.Y;

                    //
                    // NOTE: Return the wrapped (inner) IClientData instance
                    //       to the caller.
                    //
                    return anyPair.X;
                }

                //
                // NOTE: Return the original contained data.
                //
                data = localData;
            }

            return clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static bool IsReadOnly(
            IClientData clientData
            )
        {
            //
            // HACK: We only know about the base ClientData class as far as
            //       detecting the read-only property goes (since it is not
            //       part of the formal IClientData interface).
            //
            ClientData localClientData = clientData as ClientData;

            if (localClientData == null)
                return false; /* NOTE: It cannot be read-only if null. */

            //
            // NOTE: Return the value of the read-only property for the
            //       IClientData instance.
            //
            return localClientData.ReadOnly;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasData(
            IClientData clientData
            )
        {
            object data = null;

            return HasData(clientData, ref data);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HasData(
            IClientData clientData,
            ref object data
            )
        {
            //
            // NOTE: If the IClientData instance is null or equals our reserved
            //       "empty" instance, then it contains no actual data.
            //
            if ((clientData == null) ||
                Object.ReferenceEquals(clientData, Empty))
            {
                return false;
            }

            //
            // NOTE: If this a "plain old" IClientData instance of the default
            //       type and it contains null data, we know there is no actual
            //       data in it.
            //
            object localData = clientData.Data;

            if ((clientData.GetType() == typeof(ClientData)) &&
                (localData == null))
            {
                return false;
            }

            //
            // NOTE: Otherwise, we must assume it contains actual data.
            //
            data = localData;
            return true;
        }
        #endregion
    }
}
