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

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("5336d290-4d0c-45a0-a956-f56f3384e0cb")]
    public interface IVariable : IIdentifier
    {
        ICallFrame Frame { get; set; }
        VariableFlags Flags { get; set; }
        string QualifiedName { get; set; }
        IVariable Link { get; set; }
        string LinkIndex { get; set; }
        object Value { get; set; }
        ElementDictionary ArrayValue { get; set; }
        TraceList Traces { get; set; }

        void Reset(EventWaitHandle @event);

        void SetupValue(
            object newValue, bool union, bool array, bool clear,
            bool flag);

        bool HasFlags(VariableFlags hasFlags, bool all);
        VariableFlags SetFlags(VariableFlags flags, bool set);

        bool HasTraces();
        void ClearTraces();
        int AddTraces(TraceList traces);

        //
        // TODO: Change this to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode FireTraces(
            BreakpointType breakpointType, Interpreter interpreter,
            ITraceInfo traceInfo, ref Result result);
    }
}
