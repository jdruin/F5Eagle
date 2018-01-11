/*
 * CommandCallbackWrapper.cs --
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
using System.Runtime.CompilerServices;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    //
    // WARNING: This class must be public for it to work correctly; however,
    //          it cannot be created and is NOT designed for use outside of
    //          the Eagle core library itself.  In the future, it may change
    //          in completely incompatible ways.  You have been warned.
    //
    [ObjectId("a6ec2541-13ec-4f07-ab59-70d5d8fd52b4")]
    public sealed class CommandCallbackWrapper
    {
        #region Private Constants
        //
        // NOTE: This is for use by CommandCallback.GetDynamicDelegate()
        //       only.
        //
        internal static readonly MethodInfo dynamicInvokeMethodInfo =
            typeof(CommandCallbackWrapper).GetMethod(
                "StaticFireDynamicInvokeCallback",
                MarshalOps.PublicStaticMethodBindingFlags);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // NOTE: This is used to synchronoize access to the static callback
        //       lookup dictionary (below).
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static readonly IDictionary<object, ICallback> callbacks =
            new Dictionary<object, ICallback>();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private CommandCallbackWrapper()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static object StaticFireDynamicInvokeCallback(
            object firstArgument,
            object[] args
            )
        {
            ICallback callback;

            lock (syncRoot)
            {
                if ((firstArgument == null) || (callbacks == null) ||
                    !callbacks.TryGetValue(firstArgument, out callback))
                {
                    throw new ScriptException(String.Format(
                        "{0} for object {1} with hash code {2} not found",
                        typeof(ICallback), FormatOps.WrapOrNull(
                        firstArgument), RuntimeHelpers.GetHashCode(
                        firstArgument)));
                }
            }

            //
            // NOTE: The "callback" variable could be null at this point.
            //
            return CommandCallback.StaticFireDynamicInvokeCallback(
                callback, args);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int Cleanup(
            ICallback callback
            )
        {
            IDictionary<object, ICallback> localCallbacks;

            lock (syncRoot)
            {
                if (callbacks == null)
                    return 0;

                localCallbacks = new Dictionary<object, ICallback>(callbacks);
            }

            int count = 0;

            foreach (KeyValuePair<object, ICallback> pair in localCallbacks)
            {
                if ((callback == null) ||
                    ObjectData.ReferenceEquals(pair.Value, callback))
                {
                    lock (syncRoot)
                    {
                        if ((callbacks != null) &&
                            callbacks.Remove(pair.Key))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static ReturnCode Create(
            object value,       /* in */
            ICallback callback, /* in */
            ref Result error    /* out */
            )
        {
            if (value == null)
            {
                error = "invalid object instance";
                return ReturnCode.Error;
            }

            if (callback == null)
            {
                error = "invalid command callback";
                return ReturnCode.Error;
            }

            lock (syncRoot)
            {
                if (callbacks == null)
                {
                    error = "command callbacks not available";
                    return ReturnCode.Error;
                }

                callbacks[value] = callback;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
