/*
 * VariableDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Public
{
    [ObjectId("c1e0a819-c899-4d10-92ff-fea8b14841df")]
    public sealed class VariableDictionary : Dictionary<string, IVariable>
    {
        #region Public Constructors
        public VariableDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public VariableDictionary(
            IDictionary<string, IVariable> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return GenericOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        internal int SetReadOnly(
            Interpreter interpreter,
            string pattern,
            bool readOnly
            )
        {
            int result = 0;

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (EntityOps.IsUndefined(variable))
                    continue;

                string name = variable.Name;

                if ((pattern == null) || StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    if (EntityOps.IsReadOnly(variable) == readOnly)
                        continue;

                    if (EntityOps.SetReadOnly(variable, readOnly))
                    {
                        result++;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal StringList GetDefined(
            Interpreter interpreter,
            string pattern
            )
        {
            StringList result = new StringList();

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (EntityOps.IsUndefined(variable))
                    continue;

                string name = variable.Name;

                if ((pattern == null) || StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal int SetUndefined(
            Interpreter interpreter,
            string pattern,
            bool undefined
            )
        {
            int result = 0;

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                //
                // NOTE: This method is EXEMPT from the normal requirement
                //       that all the variables operated on must be defined.
                //
                // if (EntityOps.IsUndefined(variable))
                //     continue;

                string name = variable.Name;

                if ((pattern == null) || StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    if (EntityOps.IsUndefined(variable) == undefined)
                        continue;

                    if (EntityOps.SetUndefined(variable, undefined))
                    {
                        result++;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal StringList GetLocals(
            Interpreter interpreter,
            string pattern
            )
        {
            if (pattern != null)
                pattern = ScriptOps.MakeVariableName(pattern);

            StringList result = new StringList();

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (EntityOps.IsUndefined(variable) ||
                    EntityOps.IsLink(variable))
                {
                    continue;
                }

                ICallFrame frame = CallFrameOps.FollowNext(variable.Frame);

                if (interpreter != null)
                {
                    if (interpreter.IsGlobalCallFrame(frame))
                        continue;

                    if (Interpreter.IsNamespaceCallFrame(frame))
                        continue;
                }

                string name = variable.Name;

                if ((pattern == null) || StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal StringList GetWatchpoints()
        {
            StringList result = new StringList();

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                VariableFlags flags = EntityOps.GetWatchpointFlags(
                    variable.Flags);

                if (flags != VariableFlags.None)
                {
                    //
                    // NOTE: Two element sub-list of name and watch types.
                    //
                    result.Add(new StringList(
                        variable.Name, flags.ToString()).ToString());
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
