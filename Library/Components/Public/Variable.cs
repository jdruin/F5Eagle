/*
 * Variable.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("ec135739-c556-422f-b396-314d27a556a9")]
    public sealed class Variable : IVariable
    {
        #region Private Constants
        private static readonly string DefaultValue = String.Empty;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private EventWaitHandle @event;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Constructors
        internal Variable(
            ICallFrame frame,
            string name,
            string qualifiedName,
            IVariable link,
            string linkIndex,
            EventWaitHandle @event
            )
            : this(frame, name, VariableFlags.None, qualifiedName,
                   link, linkIndex, @event)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        internal Variable(
            ICallFrame frame,
            string name,
            VariableFlags flags,
            string qualifiedName,
            TraceList traces,
            EventWaitHandle @event
            )
            : this(frame, name, flags, qualifiedName, (string)null, @event)
        {
            this.traces = traces;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private Variable(
            ICallFrame frame,
            string name,
            VariableFlags flags,
            string qualifiedName,
            IVariable link,
            string linkIndex,
            EventWaitHandle @event
            )
            : this(frame, name, flags, qualifiedName, (string)null, @event)
        {
            this.link = link;
            this.linkIndex = linkIndex;
        }

        ///////////////////////////////////////////////////////////////////////

        private Variable(
            ICallFrame frame,
            string name,
            VariableFlags flags,
            string qualifiedName,
            object value,
            EventWaitHandle @event
            )
        {
            this.kind = IdentifierKind.Variable;
            this.id = Guid.Empty;
            this.name = name;
            this.frame = frame;
            this.flags = flags & ~VariableFlags.NonInstanceMask;
            this.qualifiedName = qualifiedName;
            this.link = null;
            this.linkIndex = null;
            this.value = value;
            this.arrayValue = null; // TODO: For arrays, create this?
            this.traces = null;
            this.@event = @event;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        internal void ResetValue(
            Interpreter interpreter,
            bool zero
            )
        {
#if !MONO && NATIVE && WINDOWS
            if (zero && (interpreter != null) && interpreter.HasZeroString())
            {
                if (value is string)
                {
                    ReturnCode zeroCode;
                    Result zeroError = null;

                    zeroCode = StringOps.ZeroString(
                        (string)value, ref zeroError);

                    if (zeroCode != ReturnCode.Ok)
                        DebugOps.Complain(interpreter, zeroCode, zeroError);
                }
                else if (value is Argument)
                {
                    ((Argument)value).ResetValue(interpreter, zero);
                }
                else if (value is Result)
                {
                    ((Result)value).ResetValue(interpreter, zero);
                }
            }
#endif

            value = null;

            if (arrayValue != null)
            {
                arrayValue.ResetValue(interpreter, zero);
                arrayValue = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private TraceList ResetTraces(
            bool initialize,
            bool clear
            )
        {
            if (traces != null)
            {
                if (clear)
                {
                    TraceList oldTraces = new TraceList(traces);

                    traces.Clear();

                    return oldTraces;
                }
            }
            else if (initialize)
            {
                traces = new TraceList();
            }

            return null;
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

        #region IVariable Members
        private ICallFrame frame;
        public ICallFrame Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private VariableFlags flags;
        public VariableFlags Flags
        {
            get { return flags; }
            set
            {
                //
                // NOTE: Save the old variable flags.
                //
                VariableFlags oldFlags = flags;

                //
                // NOTE: Set the new variable flags.
                //
                flags = value;

                //
                // NOTE: Call our internal event handler,
                //       passing the old and new flags.
                //
                /* IGNORED */
                EntityOps.OnFlagsChanged(@event, oldFlags, flags);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private string qualifiedName;
        public string QualifiedName
        {
            get { return qualifiedName; }
            set { qualifiedName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IVariable link;
        public IVariable Link
        {
            get { return link; }
            set { link = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string linkIndex;
        public string LinkIndex
        {
            get { return linkIndex; }
            set { linkIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object value;
        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ElementDictionary arrayValue;
        public ElementDictionary ArrayValue
        {
            get { return arrayValue; }
            set { arrayValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TraceList traces;
        public TraceList Traces
        {
            get { return traces; }
            set { traces = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void Reset(
            EventWaitHandle @event
            )
        {
            flags = VariableFlags.None;
            qualifiedName = null;
            link = null;
            linkIndex = null;
            value = null;
            arrayValue = null;
            traces = null; // BUGBUG: Is this correct (i.e. does Tcl do this)?
            this.@event = @event;
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetupValue(
            object newValue,
            bool union,
            bool array,
            bool clear,
            bool flag
            )
        {
            if (array)
            {
                //
                // NOTE: An array variable cannot have a scalar value unless
                //       the caller specifically asks for one.
                //
                if (union && (value != null))
                    value = null;
                else if (newValue != null)
                    value = newValue;

                //
                // NOTE: An array variable must have an array value (element
                //       dictionary).  Only clear it if requested by the
                //       caller.
                //
                if (arrayValue == null)
                    arrayValue = new ElementDictionary(@event);
                else if (clear)
                    arrayValue.Clear();

                //
                // NOTE: Set the array flag?
                //
                if (flag)
                    flags |= VariableFlags.Array;
            }
            else
            {
                //
                // NOTE: A scalar variable cannot have an array value (element
                //       dictionary).
                //
                if (union && (arrayValue != null))
                    arrayValue = null;

                //
                // NOTE: A scalar variable can have a scalar value.  Only clear
                //       it if requested by the caller.  Otherwise, set the new
                //       value if requested by the caller.
                //
                if (clear)
                    value = null;
                else if (newValue != null)
                    value = newValue;

                //
                // NOTE: Unset the array flag?
                //
                if (flag)
                    flags &= ~VariableFlags.Array;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasFlags(
            VariableFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public VariableFlags SetFlags(
            VariableFlags flags,
            bool set
            )
        {
            if (set)
                return (this.flags |= flags);
            else
                return (this.flags &= ~flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool HasTraces()
        {
            return ((traces != null) && (traces.Count > 0));
        }

        ///////////////////////////////////////////////////////////////////////

        public void ClearTraces()
        {
            /* IGNORED */
            ResetTraces(true, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public int AddTraces(
            TraceList traces
            )
        {
            int result = 0;

            /* IGNORED */
            ResetTraces(true, false);

            if (this.traces != null)
            {
                foreach (ITrace trace in traces)
                {
                    if (trace == null)
                        continue;

                    this.traces.Add(trace);

                    result++;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode FireTraces(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (traces != null)
            {
                //
                // NOTE: Save the current variable flags.
                //
                VariableFlags savedFlags = flags;

                //
                // NOTE: Prevent endless trace recursion.
                //
                flags |= VariableFlags.NoTrace;

                try
                {
                    //
                    // NOTE: Process each trace (as long as they all continue
                    //       to succeed).
                    //
                    foreach (ITrace trace in traces)
                    {
                        if ((trace != null) && !EntityOps.IsDisabled(trace))
                        {
                            //
                            // NOTE: If possible, set the Trace property of the
                            //       TraceInfo to the one we are about to execute.
                            //
                            if (traceInfo != null)
                                traceInfo.Trace = trace;

                            //
                            // NOTE: Since variable traces can basically do anything
                            //       they want, we wrap them in a try block to prevent
                            //       exceptions from escaping.
                            //
                            interpreter.EnterTraceLevel();

                            try
                            {
                                code = trace.Execute(
                                    breakpointType, interpreter, traceInfo, ref result);
                            }
                            catch (Exception e)
                            {
                                //
                                // NOTE: Translate exceptions to a failure return.
                                //
                                result = String.Format(
                                    "caught exception while firing variable trace: {0}",
                                    e);

                                code = ReturnCode.Error;
                            }
                            finally
                            {
                                interpreter.ExitTraceLevel();
                            }

                            //
                            // NOTE: Check for exception results specially because we
                            //       treat "Break" different from other return codes.
                            //
                            if (code == ReturnCode.Break)
                            {
                                //
                                // NOTE: Success; however, skip processing further
                                //       traces for this variable operation.
                                //
                                code = ReturnCode.Ok;
                                break;
                            }
                            else if (code != ReturnCode.Ok)
                            {
                                //
                                // NOTE: Some type of failure (or exception), stop
                                //       processing for this variable operation.
                                //
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    //
                    // NOTE: Restore the saved variable flags.
                    //
                    flags = savedFlags;
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringOps.GetStringFromObject(
                value, DefaultValue, !(value is Variable));
        }
        #endregion
    }
}
