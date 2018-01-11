/*
 * Interpreter.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("eb34a7f9-199e-4bbb-90b9-7ab5334bcc38")]
    public interface IInterpreter
    {
        ///////////////////////////////////////////////////////////////////////
        // OBJECT IDENTITY & AFFINITY
        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: THESE PROPERTIES ARE NOT GUARANTEED TO BE ACCURATE OR USEFUL.
        //
        long Id { get; }
        bool Disposed { get; }
        int ThreadId { get; }
        Thread Thread { get; }

#if SHELL
        Thread InteractiveThread { get; }
#endif

        EventWaitHandle VariableEvent { get; }

        ///////////////////////////////////////////////////////////////////////

        [Throw(false)]
        long IdNoThrow { get; }   /* INTERNAL USE ONLY. */

        [Throw(false)]
        int GetHashCodeNoThrow(); /* INTERNAL USE ONLY. */

        ///////////////////////////////////////////////////////////////////////

        AppDomain GetAppDomain();
        bool IsSameAppDomain(AppDomain appDomain);

        ///////////////////////////////////////////////////////////////////////

        StrongName GetStrongName();
        Hash GetHash();
        X509Certificate GetCertificate();

        ///////////////////////////////////////////////////////////////////////

        void DemandStrongName();
        void DemandStrongName(ref StrongName strongName);

        void DemandCertificate();
        void DemandCertificate(ref X509Certificate certificate);

        ///////////////////////////////////////////////////////////////////////
        //
        // NOTE: This method is used to generated "opaque" handle names for a
        //       variety of things.
        //
        long NextId();

        ///////////////////////////////////////////////////////////////////////

        ReturnCode GetContext(ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // HOST & SCRIPT ENVIRONMENT
        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default script flags used by the engine (i.e. in the
        //       EvaluateFile method) when requesting a script from the
        //       interpreter host.
        //
        ScriptFlags ScriptFlags { get; set; }

        //
        // NOTE: The host could be almost anything, minimally it must be an
        //       IInteractiveHost implementation of some kind.
        //
        IHost Host { get; set; }

        //
        // NOTE: Normally also a System.Reflection.Binder and an implementation
        //       of IScriptBinder.
        //
        IBinder Binder { get; set; }

        CultureInfo CultureInfo { get; set; }
        bool Quiet { get; set; }

#if POLICY_TRACE
        bool PolicyTrace { get; set; }
#endif

        MatchCallback MatchCallback { get; set; }

#if NETWORK
        NewWebClientCallback NewWebClientCallback { get; set; }
#endif

        string BackgroundError { get; set; }
        string Unknown { get; set; }
        string GlobalUnknown { get; set; }
        string NamespaceUnknown { get; set; }

        PackageCallback PackageFallback { get; set; }
        string PackageUnknown { get; set; }

        //
        // NOTE: These properties are, in both theory and practice, very
        //       closely tied to the precise implementation semantics of the
        //       IHost implementation in use; therefore, they are considered to
        //       be part of the "host environment".
        //
        bool Exit { get; set; }
        ExitCode ExitCode { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // DATA TYPE CONVERSION
        ///////////////////////////////////////////////////////////////////////

        string DateTimeFormat { get; set; }
        DateTimeKind DateTimeKind { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // EXPRESSION PRECISION
        ///////////////////////////////////////////////////////////////////////

        int Precision { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // EXECUTION LIMITS
        ///////////////////////////////////////////////////////////////////////

        bool Enabled { get; set; }
        bool ReadOnly { get; set; }
        bool Immutable { get; set; }
        int ReadyLimit { get; set; }
        int RecursionLimit { get; set; }
        int Timeout { get; set; }
        int FinallyTimeout { get; set; }
        int ThreadStackSize { get; set; }
        int ExtraStackSpace { get; set; }

#if RESULT_LIMITS
        int ExecuteResultLimit { get; set; }
        int NestedResultLimit { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // ENGINE SUPPORT
        ///////////////////////////////////////////////////////////////////////

        bool IsBusy { get; }

        int Levels { get; } // WARNING: NOT GUARANTEED TO BE ACCURATE OR USEFUL.

        ///////////////////////////////////////////////////////////////////////
        // XML DATA HANDLING
        ///////////////////////////////////////////////////////////////////////

#if XML
        bool ValidateXml { get; set; }
        bool AllXml { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // WATCHDOG SUPPORT
        ///////////////////////////////////////////////////////////////////////

        ReturnCode WatchdogControl(
            WatchdogOperation operation,
            IClientData clientData,
            int? timeout,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // EVENT QUEUE MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        int SleepTime { get; set; }
        IEventManager EventManager { get; }
        EventFlags ServiceEventFlags { get; }

        ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            ref Result error
            );

        ReturnCode QueueScript(
            DateTime dateTime,
            string text,
            ref IEvent @event,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // ENTITY MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        IEntityManager EntityManager { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        IInterpreterManager InterpreterManager { get; }
    }
}
