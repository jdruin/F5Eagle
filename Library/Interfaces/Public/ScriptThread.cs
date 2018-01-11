/*
 * ScriptThread.cs --
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
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("a93e417d-5ad1-413e-bd26-feb58d04a0f0")]
    public interface IScriptThread : IGetInterpreter
    {
        ///////////////////////////////////////////////////////////////////////
        // OBJECT IDENTITY & AFFINITY
        ///////////////////////////////////////////////////////////////////////

        Thread Thread { get; }
        long Id { get; }
        string Name { get; }
        int ActiveCount { get; } // NOTE: How many in this AppDomain?

        ///////////////////////////////////////////////////////////////////////
        // THREAD CREATION & SETUP
        ///////////////////////////////////////////////////////////////////////

        ThreadFlags ThreadFlags { get; }
        int MaxStackSize { get; }
        int Timeout { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER CREATION & SETUP
        ///////////////////////////////////////////////////////////////////////

        IEnumerable<string> Args { get; }
        IHost Host { get; }
        CreateFlags CreateFlags { get; }
        InitializeFlags InitializeFlags { get; }
        ScriptFlags ScriptFlags { get; }
        InterpreterFlags InterpreterFlags { get; }
        IScript Script { get; }
        string VarName { get; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD CREATION READ-ONLY PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool UserInterface { get; }
        bool IsBackground { get; }
        bool UseSelf { get; }

        ///////////////////////////////////////////////////////////////////////
        // ERROR HANDLING READ-ONLY PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool Quiet { get; }
        bool NoBackgroundError { get; }

        ///////////////////////////////////////////////////////////////////////
        // EVENT HANDLING READ-ONLY PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        EventWaitFlags EventWaitFlags { get; }
        bool NoComplain { get; }

        ///////////////////////////////////////////////////////////////////////
        // DIAGNOSTIC READ-WRITE PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool Verbose { get; set; }
        bool Debug { get; set; }
        ReturnCode ReturnCode { get; set; }
        Result Result { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD STATE READ-ONLY PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        bool IsAlive { get; }
        bool IsBusy { get; }
        bool IsDisposed { get; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD STATE METHODS
        ///////////////////////////////////////////////////////////////////////

        bool Start();

        bool Stop();
        bool Stop(bool force);

        ///////////////////////////////////////////////////////////////////////
        // CLR OBJECT INTEGRATION METHODS
        ///////////////////////////////////////////////////////////////////////

        ReturnCode AddObject(object value);
        ReturnCode AddObject(object value, ref Result result);
        ReturnCode AddObject(object value, bool alias, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType,
            object value, bool alias, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType, object value,
            bool alias, bool aliasReference, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType,
            ObjectFlags objectFlags, object value, bool alias,
            bool aliasReference, ref Result result);

        ReturnCode AddObject(ObjectOptionType objectOptionType,
            string objectName, ObjectFlags objectFlags, object value,
            bool alias, bool aliasReference, ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONOUS WAIT METHODS
        ///////////////////////////////////////////////////////////////////////

        bool WaitForStart();
        bool WaitForStart(int milliseconds);
        bool WaitForStart(int milliseconds, bool strict);

        bool WaitForEnd();
        bool WaitForEnd(int milliseconds);
        bool WaitForEnd(int milliseconds, bool strict);

        bool WaitForEmpty();
        bool WaitForEmpty(int milliseconds);
        bool WaitForEmpty(int milliseconds, bool strict);
        bool WaitForEmpty(int milliseconds, bool idle, bool strict);

        bool WaitForEvent();
        bool WaitForEvent(int milliseconds);
        bool WaitForEvent(int milliseconds, bool strict);
        bool WaitForEvent(int milliseconds, bool idle, bool strict);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS QUEUEING METHODS (VIA EVENT MANAGER)
        ///////////////////////////////////////////////////////////////////////

        bool Queue(EventCallback callback, IClientData clientData);
        bool Queue(DateTime dateTime, EventCallback callback,
            IClientData clientData);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS QUEUEING METHODS (VIA ENGINE)
        ///////////////////////////////////////////////////////////////////////

        bool Queue(string text);
        bool Queue(DateTime dateTime, string text);
        bool Queue(string text, AsynchronousCallback callback,
            IClientData clientData);

        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONOUS QUEUEING METHODS (VIA EVENT MANAGER AND/OR ENGINE)
        ///////////////////////////////////////////////////////////////////////

        ReturnCode Send(string text, ref Result result);
        ReturnCode Send(string text, bool useEngine, ref Result result);
        ReturnCode Send(string text, int milliseconds, bool useEngine,
            ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SIGNALING METHODS
        ///////////////////////////////////////////////////////////////////////

        bool Signal(string value);
        bool WakeUp();

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SCRIPT CANCELLATION METHODS
        ///////////////////////////////////////////////////////////////////////

        bool Cancel(CancelFlags cancelFlags);
        bool Cancel(CancelFlags cancelFlags, ref Result error);

        bool ResetCancel(CancelFlags cancelFlags);
        bool ResetCancel(CancelFlags cancelFlags, ref Result error);

        ///////////////////////////////////////////////////////////////////////
        // CLEANUP METHODS (NON-PRIMARY THREAD CONTEXTS)
        ///////////////////////////////////////////////////////////////////////

        bool Cleanup();
    }
}
