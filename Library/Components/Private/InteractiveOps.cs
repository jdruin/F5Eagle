/*
 * InteractiveOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SHELL && INTERACTIVE_COMMANDS
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
#endif

using Eagle._Attributes;

#if SHELL && INTERACTIVE_COMMANDS
using Eagle._Components.Public;
using Eagle._Components.Shared;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
#endif

#if HISTORY || (SHELL && INTERACTIVE_COMMANDS)
using Eagle._Interfaces.Public;
#endif

#if SHELL && INTERACTIVE_COMMANDS
using _Public = Eagle._Components.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("3d994484-cb72-4c34-acbb-f74fd0509f14")]
    internal static class InteractiveOps
    {
        #region Private Constants
#if SHELL && INTERACTIVE_COMMANDS
#if NATIVE && TCL
        //
        // NOTE: Used by the interpreter host to set its title based on the
        //       currently selected evaluation mode.
        //
        private static readonly string TclInteractiveMode = "native Tcl mode";
        private static readonly string EagleInteractiveMode = null;
#endif

        ///////////////////////////////////////////////////////////////////////////

        //
        // NOTE: When one of the IInformationHost.Write* methods fails (i.e. it
        //       returns false), it is typically because there is not enough
        //       space left to write the complete output using the currently
        //       selected style (e.g. the internal call to WriteBox failed).
        //
        private static readonly string HostWriteInfoError =
            "failed to write formatted information to host " +
            "(perhaps there is no space left?), please use " +
            "the [host clear] command and try again";

        ///////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default script variable name used (i.e. in the current
        //       scope) to hold the result of the last interactive command.  It is
        //       used by the interactive "#sresult" command.
        //
        private static readonly string DefaultResultVarName = "__result";
#endif

        ///////////////////////////////////////////////////////////////////////////

#if HISTORY && SHELL && INTERACTIVE_COMMANDS
        private static readonly string DefaultHistoryFileName =
            "history" + FileExtension.Script;

        ///////////////////////////////////////////////////////////////////////////

        private static readonly IHistoryData DefaultHistoryLoadData = null;
        private static readonly IHistoryData DefaultHistorySaveData = null;

        ///////////////////////////////////////////////////////////////////////////

        private static readonly IHistoryFilter DefaultHistoryLoadFilter = null;
        private static readonly IHistoryFilter DefaultHistorySaveFilter = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Interactive Command Dispatch Method
#if SHELL && INTERACTIVE_COMMANDS
        //
        // TODO: Yes, this method has 44 parameters, a lot of which are "ref".
        //       Yes, it's too long, and uses a bunch of "if/else" statements
        //       for something that should be a lookup table.  This needs to
        //       be refactored.
        //
        public static bool Dispatch(
            Interpreter interpreter,                      /* in */
            InteractiveLoopData loopData,                 /* in, out */
            bool noInteractive,                           /* in */
            bool trace,                                   /* in */
            ref IInteractiveHost interactiveHost,         /* in, out */
            ref string text,                              /* in, out */
            ref string savedText,                         /* in, out */
            ref bool tclsh,                               /* in, out */
            ref bool? savedTclsh,                         /* out */
            ref EngineFlags localEngineFlags,             /* in, out */
            ref SubstitutionFlags localSubstitutionFlags, /* in, out */
            ref EventFlags localEventFlags,               /* in, out */
            ref ExpressionFlags localExpressionFlags,     /* in, out */
            ref HeaderFlags localHeaderFlags,             /* in, out */
            ref bool exact,                               /* in, out */
            ref bool canceled,                            /* out */
            ref bool notReady,                            /* out */
            ref Result parseError,                        /* out */
            ref int localErrorLine,                       /* in, out */
            ref bool haveErrorLine,                       /* out */
            ref bool startedGcThread,                     /* out */
            ref string tclInterpName,                     /* in, out */
            ref bool done,                                /* out */
            ref bool previous,                            /* in, out */
            ref bool show,                                /* out */
            ref bool forceCancel,                         /* in, out */
            ref bool forceHalt,                           /* in, out */
            ref ReturnCode localCode,                     /* in, out */
            ref Result localResult,                       /* in, out */
            ref Result result                             /* in, out */
            )
        {
            #region Parameter Check
            if (loopData == null)
            {
                localResult = "invalid interactive loop data";
                localCode = ReturnCode.Error;

                return true; /* COMMAND PROCESSED */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Interactive Command Local Variables
            //
            // NOTE: Setup the local interactive command processing variables
            //       used when resolving and dispatching interactive commands.
            //
            bool debugVerbose = true;
            ArgumentList debugArguments = null;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Interactive Commands
            #region "Special" Interactive Commands
            //
            // NOTE: This first interactive command check is designed to
            //       allow any overridden interactive commands to be executed
            //       properly (e.g. "#show") even if they do not actually
            //       represent a built-in interactive command (e.g. "#foo").
            //       This call modifies the local return code and result.
            //       The result of the interactive command execution will be
            //       displayed using the normal mechanisms, after this huge
            //       "if/else" block (below).
            //
            if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, null, loopData.ClientData,
                    false, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                #region Interactive Extension Command
                //
                // NOTE: Do nothing.
                //
                return true; /* COMMAND PROCESSED */
                #endregion
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "nop", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                #region Interactive Nop Command
                //
                // NOTE: This built-in interactive command is specifically
                //       designed to do nothing.
                //
                return true; /* COMMAND PROCESSED */
                #endregion
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region "Normal" Interactive Commands
            #region Previously "Special" Interactive Commands
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "go", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.go(
                    loopData.Debug, ref done, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "run", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.run(
                    interpreter, loopData.Debug, ref done,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "break", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands._break(
                    interpreter, debugArguments, loopData.Token,
                    loopData.TraceInfo, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, localHeaderFlags,
                    loopData.ClientData, loopData.Arguments,
                    ref done, ref localCode, ref localResult,
                    ref loopData.code, ref result);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "halt", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.halt(
                    interpreter, loopData.Debug, ref done,
                    ref localCode, ref localResult,
                    ref loopData.code, ref result);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "done", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands._done(
                    debugArguments, ref done, ref localCode,
                    ref localResult, ref loopData.code,
                    ref result);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "exact", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands._exact(
                    ref exact, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "exit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.exit(
                    ref loopData.exit, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Other "Normal" Interactive Commands
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cmd", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult) ||
                Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cmd.exe", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult) ||
                Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cmd", loopData.ClientData,
                    false, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult) ||
                Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cmd.exe", loopData.ClientData,
                    false, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.cmd(
                    interpreter, debugArguments, localEventFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "intsec", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.intsec(
                    interpreter, interactiveHost as IDebugHost,
                    debugArguments, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "tclshrc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.tclshrc(
                    interpreter, interactiveHost as IFileSystemHost,
                    debugArguments, localEventFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "website", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.website(
                    interpreter, localEventFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "reset", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.reset(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "useattach", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.useattach(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "color", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.color(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "exceptions", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.exceptions(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "testgc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.testgc(
                    interpreter, debugArguments, ref startedGcThread,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "hcancel", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.hcancel(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "hexit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.hexit(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "stable", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.stable(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "check", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                localErrorLine = 0;

                Commands.check(
                    interpreter, debugArguments, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, ref localCode,
                    ref localResult, ref localErrorLine);

                haveErrorLine = true;
                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "eval", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.eval(
                    text, debugArguments, ref savedText,
                    ref tclsh, ref savedTclsh,
                    ref show, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "again", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.again(
                    interpreter, ref previous, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "help", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.help(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "usage", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.usage(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "version", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.version(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "args", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.args(
                    loopData.Args, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "ainfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.ainfo(
                    interpreter, interactiveHost, loopData.Code,
                    loopData.BreakpointType, loopData.BreakpointName,
                    loopData.Arguments, result, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "npinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.npinfo(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "clearq", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.clearq(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "oinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.oinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "vinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.vinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "complaint", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.complaint(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cuinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.cuinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "dinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.dinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "testinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.testinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "toinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.toinfo(
                    interpreter, interactiveHost, loopData.Token,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "tcancel", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.tcancel(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "tcode", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.tcode(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "toldvalue", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.toldvalue(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "tnewvalue", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.tnewvalue(
                    interpreter, debugArguments, loopData.TraceInfo,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "tinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.tinfo(
                    interpreter, interactiveHost, debugArguments,
                    loopData.TraceInfo, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "stack", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.stack(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "finfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.finfo(
                    interpreter, interactiveHost, loopData.EngineFlags,
                    loopData.SubstitutionFlags, loopData.EventFlags,
                    loopData.ExpressionFlags, loopData.HeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "lfinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.lfinfo(
                    interpreter, interactiveHost, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, localHeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "frinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.frinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "einfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.einfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.cinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "eninfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.eninfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "sinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.sinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "histfile", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.histfile(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "histinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.histinfo(
                    interpreter, interactiveHost, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "histclear", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.histclear(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "histload", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.histload(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "histsave", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.histsave(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "hinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.hinfo(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "iinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.iinfo(
                    interpreter, interactiveHost, result,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "fresc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.fresc(
                    ref forceCancel, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "fresh", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.fresh(
                    ref forceHalt, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "resc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.resc(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "resh", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.resh(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "rehash", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.rehash(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "deval", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                localErrorLine = 0;

                Commands.deval(
                    interpreter, debugArguments, ref localCode,
                    ref localResult, ref localErrorLine);

                haveErrorLine = true;
                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "dsubst", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.dsubst(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "suspend", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.suspend(
                    interpreter, loopData.Debug, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "resume", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.resume(
                    interpreter, loopData.Debug, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "about", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.about(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "chans", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.chans(
                    interpreter, interactiveHost as IStreamHost,
                    debugArguments, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "init", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.init(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "dpath", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.dpath(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cancel", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.cancel(
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "test", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult) ||
                Interpreter.CheckInteractiveCommand(
                    interpreter, text, "ptest", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                localErrorLine = 0;

                Commands.test(
                    interpreter, debugArguments, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, ref localCode,
                    ref localResult, ref localErrorLine);

                haveErrorLine = true;
                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "trustclr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.trustclr(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "trustdir", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.trustdir(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "testdir", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.testdir(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "purge", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.purge(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "restc", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.restc(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "restt", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.restt(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "restv", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.restv(
                    interpreter, loopData.Args, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "vout", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.vout(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "relimit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.relimit(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "rlimit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.rlimit(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "ntypes", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.ntypes(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "nflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.nflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "hflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.hflags(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Debug, ref localHeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "lhflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.lhflags(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Debug, ref localHeaderFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "cflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.cflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "dcflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.dcflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "scflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.scflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "dscflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.dscflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "iflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.iflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "diflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.diflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "paflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.paflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "prflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.prflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "pflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.pflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "ceflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.ceflags(
                    interpreter, debugArguments,
                    ref localEngineFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "seflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.seflags(
                    interpreter, debugArguments,
                    ref localEngineFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "evflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.evflags(
                    interpreter, debugArguments,
                    ref localEventFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "exflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.exflags(
                    interpreter, debugArguments,
                    ref localExpressionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "ieflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.ieflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "ievflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.ievflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "iexflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.iexflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "leflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.leflags(
                    interpreter, debugArguments,
                    ref localEngineFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "levflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.levflags(
                    interpreter, debugArguments, ref localEventFlags,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "lexflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.lexflags(
                    interpreter, debugArguments,
                    ref localExpressionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "sflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.sflags(
                    interpreter, debugArguments,
                    ref localSubstitutionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "isflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.isflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "izflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.izflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "dizflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.dizflags(
                    interpreter, debugArguments, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "lsflags", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.lsflags(
                    interpreter, debugArguments,
                    ref localSubstitutionFlags, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "step", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.step(
                    interpreter, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "style", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.style(
                    interpreter, interactiveHost, debugArguments,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "canexit", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.canexit(
                    interactiveHost, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "show", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.show(
                    interpreter, interactiveHost, debugArguments,
                    loopData, result, localHeaderFlags, localCode,
                    localResult, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "overr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.overr(
                    interpreter, interactiveHost, debugArguments,
                    ref show, ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "prevr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.prevr(
                    interpreter, interactiveHost, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "nextr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.nextr(
                    interpreter, interactiveHost, localCode,
                    localResult, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "fresr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.fresr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult, ref loopData.code,
                    ref result);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "resr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.resr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult, ref loopData.code,
                    ref result);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "clearr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.clearr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "nullr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.nullr(
                    interactiveHost, ref show, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "copyr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.copyr(
                    interactiveHost, loopData.Code, result, ref show,
                    ref localCode, ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "setr", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.setr(
                    interactiveHost, localCode, localResult,
                    ref show, ref loopData.code, ref result);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "mover", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.mover(
                    interactiveHost, ref show, ref localCode,
                    ref localResult, ref loopData.code,
                    ref result);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "lrinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.lrinfo(
                    interactiveHost, localCode, localResult,
                    localErrorLine, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "mrinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.mrinfo(
                    interpreter, interactiveHost, debugArguments,
                    loopData.Code, result, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "rinfo", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.rinfo(
                    interpreter, interactiveHost, localCode,
                    localResult, localErrorLine, loopData.Code, result,
                    ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "sresult", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.sresult(
                    interpreter, interactiveHost, debugArguments,
                    localCode, localResult, localErrorLine,
                    loopData.Code, result, ref show);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "tclsh", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.tclsh(interpreter,
                    interactiveHost, ref tclsh, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "tclinterp", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.tclinterp(
                    debugArguments, ref tclInterpName, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            else if (Interpreter.CheckInteractiveCommand(
                    interpreter, text, "queue", loopData.ClientData,
                    true, exact, ref debugVerbose, ref debugArguments,
                    ref localCode, ref localResult))
            {
                Commands.queue(
                    interpreter, noInteractive, trace,
                    loopData.Debug, localEngineFlags,
                    localSubstitutionFlags, localEventFlags,
                    localExpressionFlags, loopData.ClientData,
                    forceCancel, forceHalt, ref interactiveHost,
                    ref savedText, ref loopData.exit, ref done,
                    ref previous, ref canceled, ref text,
                    ref notReady, ref parseError, ref localCode,
                    ref localResult);

                return true; /* COMMAND PROCESSED */
            }
            #endregion
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////

            return false; /* COMMAND NOT PROCESSED */
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////

        #region Interactive Command Implementation Class
#if SHELL && INTERACTIVE_COMMANDS
        [ObjectId("bc7c0ee9-8677-4416-b1c4-e47437b209cb")]
        internal static class Commands
        {
            #region Public Interactive Command Methods
            #region Special Interactive Command Methods
            public static void go(
                bool debug,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: In debug mode, we simply break out of
                //       the loop; otherwise, an error message
                //       is displayed.
                //
                if (debug)
                {
                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;

                    //
                    // NOTE: Set the "done" flag for this
                    //       nested interactive loop now.
                    //       This code used to simply use
                    //       a C# "break" statement here;
                    //       however, this is much cleaner
                    //       because it permits the extra
                    //       tasks performed at the bottom
                    //       of this loop to be completed.
                    //       Also, it allows the code for
                    //       this interactive command to
                    //       reside outside of the main
                    //       InteractiveLoop method.
                    //
                    done = true;
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}go\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void run(
                Interpreter interpreter,
                bool debug,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In debug mode, we simply disable further
                //       stepping and break out of the loop;
                //       otherwise, an error message is displayed.
                //
                if (debug)
                {
                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, false,
                            ref localDebugger, ref localResult))
                    {
                        //
                        // FIXME: Yes, this is somewhat confusing.
                        //        Why does the "#run" command call
                        //        the IDebugger.Reset method?
                        //
                        //        From the perspective of the
                        //        IDebugger interface itself, the
                        //        Reset method clears all the
                        //        internal debugging state
                        //        (basically resetting it to null
                        //        and zero).
                        //
                        //        However, from the perspective of
                        //        the interactive loop, this command
                        //        (which is named "#run") is used to
                        //        disable all debugging features and
                        //        run the script being evaluated at
                        //        full speed.
                        //
                        localCode = localDebugger.Reset(
                            ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            //
                            // NOTE: Set the "done" flag for this
                            //       nested interactive loop now.
                            //       This code used to simply use
                            //       a C# "break" statement here;
                            //       however, this is much cleaner
                            //       because it permits the extra
                            //       tasks performed at the bottom
                            //       of this loop to be completed.
                            //       Also, it allows the code for
                            //       this interactive command to
                            //       reside outside of the main
                            //       InteractiveLoop method.
                            //
                            done = true;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}run\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void _break(
                Interpreter interpreter,
                ArgumentList debugArguments,
                IToken token,
                ITraceInfo traceInfo,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                HeaderFlags localHeaderFlags,
                IClientData clientData,
                ArgumentList arguments,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
#if DEBUGGER
                IDebugger localDebugger = null;

                if (Engine.CheckDebugger(interpreter, false,
                        ref localDebugger, ref localResult))
                {
#if PREVIOUS_RESULT
                    //
                    // NOTE: At this point, the result of the
                    //       previous command may still be
                    //       untouched and will be displayed
                    //       verbatim upon entry into the
                    //       interactive loop.
                    //
                    localResult = Result.Copy(
                        Interpreter.GetPreviousResult(interpreter),
                        true); /* COPY */
#endif

                    //
                    // NOTE: Break into the debugger by
                    //       starting a nested interactive
                    //       loop.
                    //
                    localCode = DebuggerOps.Breakpoint(
                        localDebugger, interpreter,
                        new InteractiveLoopData(localCode,
                        BreakpointType.Demand, debugArguments[0],
                        token, traceInfo, localEngineFlags,
                        localSubstitutionFlags, localEventFlags,
                        localExpressionFlags, localHeaderFlags,
                        clientData, arguments), ref localResult);

                    //
                    // FIXME: If there were no other failures in
                    //        the interactive loop, perhaps we
                    //        should reflect the previous result?
                    //        Better logic here may be needed.
                    //
                    if ((localCode == ReturnCode.Ok) &&
                        (localResult != null))
                    {
                        localCode = localResult.ReturnCode;
                    }
                    else if (interpreter.ActiveInteractiveLoops > 1)
                    {
                        //
                        // BUGFIX: If the interpreter has been
                        //         halted then we need to break
                        //         out of this loop and any
                        //         nested interactive loops
                        //         (except the outermost one).
                        //
                        result = localResult;
                        code = localCode;

                        //
                        // NOTE: Set the "done" flag for this
                        //       nested interactive loop now.
                        //       This code used to simply use
                        //       a C# "break" statement here;
                        //       however, this is much cleaner
                        //       because it permits the extra
                        //       tasks performed at the bottom
                        //       of this loop to be completed.
                        //       Also, it allows the code for
                        //       this interactive command to
                        //       reside outside of the main
                        //       InteractiveLoop method.
                        //
                        done = true;
                    }
                }
                else
                {
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void halt(
                Interpreter interpreter,
                bool debug,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: In debug mode, we simply break out of
                //       the loop and return failure to the
                //       caller; otherwise, an error message
                //       is displayed.
                //
                if (debug)
                {
                    //
                    // NOTE: Prevent further trips through the
                    //       interpreter and the interactive
                    //       loop(s).
                    //
                    localResult = "INTERPRETER HALTED";

                    localCode = Engine.HaltEvaluate(
                        interpreter, localResult,
                        CancelFlags.InteractiveManualHalt,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        result = localResult; /* TRANSFER */
                        code = ReturnCode.Error;

                        //
                        // NOTE: Set the "done" flag for this
                        //       nested interactive loop now.
                        //       This code used to simply use
                        //       a C# "break" statement here;
                        //       however, this is much cleaner
                        //       because it permits the extra
                        //       tasks performed at the bottom
                        //       of this loop to be completed.
                        //       Also, it allows the code for
                        //       this interactive command to
                        //       reside outside of the main
                        //       InteractiveLoop method.
                        //
                        done = true;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}halt\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void _done(
                ArgumentList debugArguments,
                ref bool done,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: We simply break out of the loop and return
                //       the specified (or current) return code and
                //       result to the caller.
                //
                localCode = ReturnCode.Ok;

                //
                // NOTE: Check for the optional argument containing
                //       the return code.
                //
                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseEnum(
                        typeof(ReturnCode), debugArguments[1],
                        true, true);

                    //
                    // NOTE: Was the argument a valid return code?
                    //
                    if (enumValue is ReturnCode)
                    {
                        code = (ReturnCode)enumValue;
                    }
                    else
                    {
                        localResult = ScriptOps.BadValue(null,
                            "return code value", debugArguments[1],
                            Enum.GetNames(typeof(ReturnCode)), null,
                            null);

                        localCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Check for the optional argument containing
                //       the new result.
                //
                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3))
                {
                    result = debugArguments[2];
                }

                //
                // NOTE: If we succeeded in all the previous steps,
                //       bail out of this nested interactive loop.
                //
                if (localCode == ReturnCode.Ok)
                {
                    //
                    // NOTE: Set the "done" flag for this
                    //       nested interactive loop now.
                    //       This code used to simply use
                    //       a C# "break" statement here;
                    //       however, this is much cleaner
                    //       because it permits the extra
                    //       tasks performed at the bottom
                    //       of this loop to be completed.
                    //       Also, it allows the code for
                    //       this interactive command to
                    //       reside outside of the main
                    //       InteractiveLoop method.
                    //
                    done = true;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void _exact(
                ref bool exact,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                exact = !exact;

                localResult = String.Format(
                    "exact matching {0}", exact ? "enabled" : "disabled");

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void exit(
                ref bool exit,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Exit the hard way.
                //
                exit = true;

                localResult = "interactive exit";
                localCode = ReturnCode.Ok;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////

            #region Normal Interactive Command Methods
            public static void cmd(
                Interpreter interpreter,
                ArgumentList debugArguments,
                EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Launch a child "cmd.exe" (really ComSpec) shell for
                //       debugging.
                //
                string fileName = GlobalConfiguration.GetValue(
                    EnvVars.ComSpec, ConfigurationFlags.InteractiveOps);

                if (!String.IsNullOrEmpty(fileName))
                {
                    //
                    // HACK: Create a command line that can be understood by the
                    //       operating system command processor, using its quoting
                    //       rules, not Tcl's.  Unfortunately, all the underlying
                    //       arguments for this "special" interactive command have
                    //       already been parsed from the raw input string using
                    //       Tcl's standard list quoting rules; therefore, a great
                    //       deal of care must be taken by the interactive user to
                    //       construct a command line that can survive both sets of
                    //       quoting rules.  Also, we must be sure to skip the
                    //       interactive command name itself (i.e. there was a long
                    //       standing bug here becase we were not doing that).
                    //
                    // EXAMPLE:
                    //
                    //       #cmd /c [info nameofexecutable] -eval "set x 2; puts $x"
                    //       (this requires "#isflags +Commands")
                    //
                    string execArguments = RuntimeOps.BuildCommandLine(
                        ArgumentList.GetRangeAsStringList(debugArguments, 1), false);

                    localCode = ProcessOps.ExecuteProcess(interpreter, fileName,
                        execArguments, null, localEventFlags, ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = String.Empty;
                }
                else
                {
                    localResult = String.Format(
                        "cannot execute shell, environment variable \"{0}\" not set",
                        EnvVars.ComSpec);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void intsec(
                Interpreter interpreter,
                IDebugHost debugHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    localCode = ReturnCode.Ok;
                    localResult = null;

                    bool savedSecurity = interpreter.HasSecurity();
                    bool security = !savedSecurity; /* TOGGLE */

                    if (debugArguments.Count >= 2)
                    {
                        localCode = Value.GetBoolean2(
                            debugArguments[1], ValueFlags.AnyBoolean,
                            interpreter.CultureInfo, ref security,
                            ref localResult);
                    }

                    if (localCode == ReturnCode.Ok)
                    {
                        localCode = ScriptOps.EnableOrDisableSecurity(
                            interpreter, security, ref localResult);

                        if ((localCode == ReturnCode.Ok) && (debugHost != null))
                        {
                            debugHost.WriteResult(localCode, String.Format(
                                "Interpreter {0} security {1}{2} while {3}.",
                                FormatOps.InterpreterNoThrow(interpreter),
                                security == savedSecurity ? "still " : "now ",
                                security ? "enabled" : "disabled",
                                interpreter.IsSafe() ?
                                    "\"safe\"" : "\"unsafe\""), true);
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void tclshrc(
                Interpreter interpreter,
                IFileSystemHost fileSystemHost,
                ArgumentList debugArguments,
                EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Launch a text editor (e.g. "notepad.exe") for editing the
                //       shell startup file (e.g. "~/tclshrc.tcl").
                //
                string fileName = GlobalConfiguration.GetValue(
                    EnvVars.Editor, ConfigurationFlags.InteractiveOps);

                //
                // NOTE: The editor environment variable is not set.  On Windows,
                //       just default to using "notepad[.exe]".
                //
                if (String.IsNullOrEmpty(fileName) &&
                    PlatformOps.IsWindowsOperatingSystem())
                {
                    fileName = EnvVars.EditorValue;
                }

                if (!String.IsNullOrEmpty(fileName))
                {
                    string name = TclVars.RunCommandsFileName;

                    ScriptFlags scriptFlags = ScriptFlags.Interactive |
                        ScriptFlags.ApplicationOptionalFile |
                        ScriptFlags.UserOptionalFile;

                    IClientData clientData = ClientData.Empty;
                    ResultList errors = null;

                    localCode = ScriptOps.GetStartup(
                            interpreter, fileSystemHost, name, ref scriptFlags,
                            ref clientData, ref localResult, ref errors);

                    if (localCode == ReturnCode.Ok)
                    {
                        string text = localResult;

                        if (!String.IsNullOrEmpty(text))
                        {
                            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.File, true))
                            {
                                if (PathOps.IsRemoteUri(text) || File.Exists(text))
                                {
                                    if (debugArguments.Count > 1)
                                        debugArguments.Insert(1, text);
                                    else
                                        debugArguments.Add(text);

                                    string execArguments = RuntimeOps.BuildCommandLine(
                                        ArgumentList.GetRangeAsStringList(debugArguments, 1),
                                        false);

                                    localCode = ProcessOps.ExecuteProcess(
                                        interpreter, fileName, execArguments, null,
                                        localEventFlags, true, ref localResult);

                                    if (localCode == ReturnCode.Ok)
                                        localResult = String.Empty;
                                }
                                else
                                {
                                    localResult = String.Format(
                                        "the provided \"{0}\" script file \"{1}\" is not " +
                                        "a valid remote uri and does not exist locally",
                                        name, text);

                                    localCode = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                localResult = String.Format(
                                    "the \"{0}\" script is not a file",
                                    name);

                                localCode = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            localResult = String.Format(
                                "the \"{0}\" script is invalid or has no content",
                                name);

                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = errors;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot execute editor, environment variable \"{0}\" not set",
                        EnvVars.Editor);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void website(
                Interpreter interpreter,
                EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                Uri uri = SharedAttributeOps.GetAssemblyUri(
                    GlobalState.GetAssembly());

                if (uri != null)
                {
                    localCode = ProcessOps.ShellExecuteProcess(
                        interpreter, uri.ToString(), null, null,
                        localEventFlags, ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = String.Empty;
                }
                else
                {
                    localResult = "uri not available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void reset(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                IDebugger localDebugger = null;

                if (Engine.CheckDebugger(
                        interpreter, true, ref localDebugger, ref localResult))
                {
                    //
                    // FIXME: Yes, this is somewhat confusing.  Why not call the
                    //        IDebugger.Reset method here?
                    //
                    //        From the perspective of the IDebugger interface itself,
                    //        the Initialize method sets the internal debugging state
                    //        to its initial defaults and the Reset method clears all
                    //        the internal debugging state (basically resetting it
                    //        to null and zero).
                    //
                    //        However, from the perspective of the interactive loop,
                    //        this command (which is named "#reset") is used to reset
                    //        the internal debugging state of the IDebugger interface
                    //        to its initial default state.  Without this command, it
                    //        would be very difficult to re-enable debugging features
                    //        after using the "#suspend" command followed by the "#go"
                    //        command.
                    //
                    localCode = localDebugger.Initialize(ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = "debugger reset";
                }
                else
                {
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void useattach(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Default defaultHost = interactiveHost as _Hosts.Default;

                if (defaultHost != null)
                {
                    //
                    // NOTE: Get the current attach setting and then toggle it.
                    //
                    defaultHost.UseAttach = !defaultHost.UseAttach;

                    localResult = String.Format(
                        "attach {0}",
                        ConversionOps.ToEnabled(defaultHost.UseAttach));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void color(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IColorHost colorHost = interactiveHost as IColorHost;

                if (colorHost != null)
                {
                    //
                    // NOTE: Get the current color setting and then toggle it.
                    //
                    colorHost.NoColor = !colorHost.NoColor;

                    localResult = String.Format(
                        "color {0}",
                        ConversionOps.ToEnabled(!colorHost.NoColor));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void exceptions(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Default defaultHost = interactiveHost as _Hosts.Default;

                if (defaultHost != null)
                {
                    //
                    // NOTE: Get the current exceptions setting and then toggle it.
                    //
                    defaultHost.Exceptions = !defaultHost.Exceptions;

                    localResult = String.Format(
                        "exceptions {0}",
                        ConversionOps.ToEnabled(defaultHost.Exceptions));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void testgc(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref bool startedGcThread,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    bool start = false;

                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref start,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                        {
                            if (start)
                            {
                                localCode = TestOps.StartGcThread(
                                    interpreter, ref localResult);

                                if (localCode == ReturnCode.Ok)
                                    startedGcThread = true;
                            }
                            else
                            {
                                localCode = TestOps.InterruptGcThread(
                                    interpreter, true, ref localResult);

                                if (localCode == ReturnCode.Ok)
                                    startedGcThread = false;
                            }
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}testgc start\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void hcancel(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                try
                {
                    IAnyPair<int, bool> anyPair = new AnyPair<int, bool>(
                        TestOps.hostWorkItemDelay, TestOps.hostWorkItemForce);

                    if (Engine.QueueWorkItem(
                            interpreter, TestOps.HostCancelThreadStart, anyPair))
                    {
                        localResult = "queued host cancel work item";
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = "failed to queue host cancel work item";
                        localCode = ReturnCode.Error;
                    }
                }
                catch (Exception e)
                {
                    localResult = e;
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void hexit(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                try
                {
                    IAnyPair<int, bool> anyPair = new AnyPair<int, bool>(
                        TestOps.hostWorkItemDelay, TestOps.hostWorkItemForce);

                    if (Engine.QueueWorkItem(
                            interpreter, TestOps.HostExitThreadStart, anyPair))
                    {
                        localResult = "queued host exit work item";
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = "failed to queue host exit work item";
                        localCode = ReturnCode.Error;
                    }
                }
                catch (Exception e)
                {
                    localResult = e;
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void stable(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    bool stable = false;

                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref stable,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        string localValue = RuntimeOps.GetUpdatePathAndQuery(
                            GlobalState.GetAssemblyUpdateVersion(), stable,
                            null);

                        Result localError = null;

                        localCode = interpreter.SetVariableValue2(
                            VariableFlags.GlobalOnly, Vars.Platform.Name,
                            Vars.Platform.UpdatePathAndQueryName, localValue,
                            ref localError);

                        if (localCode == ReturnCode.Ok)
                            localResult = localValue;
                        else
                            localResult = localError;
                    }
                }
                else
                {
                    Result localValue = null;
                    Result localError = null;

                    localCode = interpreter.GetVariableValue2(
                        VariableFlags.GlobalOnly, Vars.Platform.Name,
                        Vars.Platform.UpdatePathAndQueryName,
                        ref localValue, ref localError);

                    if (localCode == ReturnCode.Ok)
                        localResult = localValue;
                    else
                        localResult = localError;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void check(
                Interpreter interpreter,
                ArgumentList debugArguments,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult,
                ref int localErrorLine
                )
            {
                localCode = ReturnCode.Ok;

                bool wantScripts = false; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref wantScripts,
                        ref localResult);
                }

                bool quiet = true; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref quiet,
                        ref localResult);
                }

                bool prompt = true; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[3], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref prompt,
                        ref localResult);
                }

                bool automatic = true; // TODO: Good default?

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 5) &&
                    !String.IsNullOrEmpty(debugArguments[4]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[4], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref automatic,
                        ref localResult);
                }

                //
                // NOTE: Default to checking for new releases only.
                //
                ActionType actionType = ActionType.Default;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 6) &&
                    !String.IsNullOrEmpty(debugArguments[5]))
                {
                    object enumValue = EnumOps.TryParseEnum(
                        typeof(ActionType), debugArguments[5],
                        true, true);

                    if (enumValue is ActionType)
                    {
                        actionType = (ActionType)enumValue;
                    }
                    else
                    {
                        localResult = ScriptOps.BadValue(
                            null, "action type value", debugArguments[5],
                            Enum.GetNames(typeof(ActionType)), null, null);

                        localCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Default to the Windows setup packages.
                //
                ReleaseType releaseType = ReleaseType.Setup;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 7) &&
                    !String.IsNullOrEmpty(debugArguments[6]))
                {
                    object enumValue = EnumOps.TryParseEnum(
                        typeof(ReleaseType), debugArguments[6],
                        true, true);

                    if (enumValue is ReleaseType)
                    {
                        releaseType = (ReleaseType)enumValue;
                    }
                    else
                    {
                        localResult = ScriptOps.BadValue(
                            null, "release type value", debugArguments[6],
                            Enum.GetNames(typeof(ReleaseType)), null, null);

                        localCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: Evaluate the script we use to check for updates to the
                //       script engine.  If the proc has been redefined, this may
                //       not actually do anything.
                //
                if (localCode == ReturnCode.Ok)
                {
                    localCode = ShellOps.CheckForUpdate(
                        interpreter, actionType, releaseType, wantScripts, quiet,
                        prompt, automatic, localEngineFlags, localSubstitutionFlags,
                        localEventFlags, localExpressionFlags, ref localErrorLine,
                        ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void eval(
                string text,
                ArgumentList debugArguments,
                ref string savedText,
                ref bool tclsh,
                ref bool? savedTclsh,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    //
                    // NOTE: Make sure that the command to be evaluated is not an
                    //       interactive one.  This restriction may seem somewhat
                    //       arbitrary; however, it does prevent an endless input
                    //       loop involving *this* interactive command.
                    //
                    Argument debugArgument = debugArguments[1];

                    if (!ShellOps.LooksLikeAnyInteractiveCommand(debugArgument))
                    {
                        //
                        // NOTE: Try to strip the leading "#eval " command from
                        //       the original input text.
                        //
                        string localText = ShellOps.StripInteractiveCommand(text);

                        if (!String.IsNullOrEmpty(localText))
                        {
                            //
                            // NOTE: We are not actually doing anything, do not
                            //       display the result.
                            //
                            show = false;

                            //
                            // NOTE: Stuff the [now modified] command into the
                            //       saved input text for use during the next
                            //       iteration of the primary loop.
                            //
                            savedText = localText;

                            //
                            // NOTE: Make sure that the saved command text is not
                            //       evaluated using the "tclsh emulation mode"
                            //       by saving the associated flag and then
                            //       forcing it to false.  The flag will be
                            //       restored after the saved command text is
                            //       evaluated.
                            //
                            savedTclsh = tclsh;
                            tclsh = false;
                        }
                        else
                        {
                            localResult = "nothing to evaluate";
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = "evaluation of interactive commands is disabled";
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}eval arg ?arg ...?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void again(
                Interpreter interpreter,
                ref bool previous,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                if (previous)
                {
                    //
                    // NOTE: Do not record this command (i.e. "#again") as the
                    //       previous interactive input.
                    //
                    previous = false;

                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, false,
                            ref localDebugger, ref localResult))
                    {
                        string localText = interpreter.PreviousInteractiveInput;

                        if (localText != null)
                            localText = localText.Trim();

                        if (!String.IsNullOrEmpty(localText))
                        {
                            //
                            // NOTE: We are not actually doing anything, do not
                            //       display the result.
                            //
                            show = false;

                            //
                            // NOTE: Set the debugger command to the previously
                            //       entered interactive command.
                            //
                            localDebugger.Command = localText;
                        }
                        else
                        {
                            localResult = "no previous interactive input exists";
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = "playback of previous interactive input is disabled";
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void help(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Invoke the interactive command help.
                //
                localCode = HelpOps.WriteInteractiveHelp(
                    interpreter, debugArguments, ref localResult);
            }

            ///////////////////////////////////////////////////////////////////////

            public static void usage(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool showBanner = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showBanner,
                        ref localResult);
                }

                bool showLegalese = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showLegalese,
                        ref localResult);
                }

                bool showOptions = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[3], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showOptions,
                        ref localResult);
                }

                bool showEnvironment = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 5) &&
                    !String.IsNullOrEmpty(debugArguments[4]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[4], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showEnvironment,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    //
                    // NOTE: Show the command line syntax.
                    //
                    localCode = HelpOps.WriteUsage(
                        interpreter, null, showBanner, showLegalese, showOptions,
                        showEnvironment, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void version(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool showBanner = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showBanner,
                        ref localResult);
                }

                bool showLegalese = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showLegalese,
                        ref localResult);
                }

                bool showSource = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 4) &&
                    !String.IsNullOrEmpty(debugArguments[3]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[3], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showSource,
                        ref localResult);
                }

                bool showUpdate = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 5) &&
                    !String.IsNullOrEmpty(debugArguments[4]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[4], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showUpdate,
                        ref localResult);
                }

                bool showPlugins = true;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 6) &&
                    !String.IsNullOrEmpty(debugArguments[5]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[5], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showPlugins,
                        ref localResult);
                }

                bool showOptions = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 7) &&
                    !String.IsNullOrEmpty(debugArguments[6]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[6], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref showOptions,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    //
                    // NOTE: Show the detailed library version.
                    //
                    localCode = HelpOps.WriteVersion(
                        interpreter, showBanner, showLegalese, showSource,
                        showUpdate, showPlugins, showOptions, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void args(
                IEnumerable<string> args,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Show the command line arguments, if any.
                //
                if (args != null)
                {
                    localResult = StringList.MakeList(args);
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = "no shell arguments available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void ainfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ReturnCode code,
                BreakpointType breakpointType,
                string breakpointName,
                ArgumentList arguments,
                Result result,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteArgumentInfo(
                            interpreter, code, breakpointType,
                            breakpointName, arguments, result,
                            DetailFlags.InteractiveAll, true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void npinfo(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NATIVE && TCL && NATIVE_PACKAGE
                NativePackage.DebugTclInterpreters(interpreter, null, true);

                localResult = String.Empty;
                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void clearq(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if CALLBACK_QUEUE
                localCode = interpreter.ClearCallbackQueue(ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "callback queue cleared";
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void oinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    IObject @object = null;

                    localCode = interpreter.GetObject(
                        debugArguments[1], LookupFlags.Default,
                        ref @object, ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        IInformationHost informationHost =
                            interactiveHost as IInformationHost;

                        if ((informationHost != null) &&
                            !AppDomainOps.IsTransparentProxy(informationHost))
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteObjectInfo(
                                    interpreter, @object,
                                    DetailFlags.InteractiveAll, true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                HostOps.NoFeatureError,
                                typeof(IInformationHost).Name);

                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}oinfo name\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void vinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    if (AppDomainOps.IsSame(interpreter))
                    {
                        //
                        // BUGFIX: We do not want to be prevented from examining
                        //         the variable due to script cancellation, etc.
                        //
                        VariableFlags flags = VariableFlags.NoElement |
                            VariableFlags.NoReady;

                        IVariable variable = null;

                        localCode = interpreter.GetVariableViaResolversWithSplit(
                            debugArguments[1], ref flags, ref variable,
                            ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                            if ((localCode == ReturnCode.Ok) &&
                                (debugArguments.Count >= 3) &&
                                !String.IsNullOrEmpty(debugArguments[2]))
                            {
                                object enumValue = EnumOps.TryParseFlagsEnum(
                                    interpreter, typeof(DetailFlags),
                                    detailFlags.ToString(), debugArguments[2],
                                    interpreter.CultureInfo, true, true, true,
                                    ref localResult);

                                if (enumValue is DetailFlags)
                                {
                                    detailFlags = (DetailFlags)enumValue;

                                    localCode = ReturnCode.Ok;
                                }
                                else
                                {
                                    localCode = ReturnCode.Error;
                                }
                            }

                            if (localCode == ReturnCode.Ok)
                            {
                                IInformationHost informationHost =
                                    interactiveHost as IInformationHost;

                                if ((informationHost != null) &&
                                    !AppDomainOps.IsTransparentProxy(informationHost))
                                {
                                    informationHost.SavePosition();

                                    if (!informationHost.WriteVariableInfo(
                                            interpreter, variable, detailFlags,
                                            true))
                                    {
                                        informationHost.WriteResultLine(
                                            ReturnCode.Error, HostWriteInfoError);
                                    }

                                    informationHost.RestorePosition(true);

                                    localResult = String.Empty;
                                    localCode = ReturnCode.Ok;
                                }
                                else
                                {
                                    localResult = String.Format(
                                        HostOps.NoFeatureError,
                                        typeof(IInformationHost).Name);

                                    localCode = ReturnCode.Error;
                                }
                            }
                        }
                    }
                    else
                    {
                        localResult = "wrong application domain";
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}vinfo name ?flags?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void complaint(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteComplaintInfo(
                            interpreter, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void cuinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteCustomInfo(
                            interpreter, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void dinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                localCode = ReturnCode.Ok;

                DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(DetailFlags),
                        detailFlags.ToString(), debugArguments[1],
                        interpreter.CultureInfo, true, true, true,
                        ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        detailFlags = (DetailFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }

                if (localCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        informationHost.SavePosition();

                        if (!informationHost.WriteDebuggerInfo(
                                interpreter, detailFlags, true))
                        {
                            informationHost.WriteResultLine(
                                ReturnCode.Error, HostWriteInfoError);
                        }

                        informationHost.RestorePosition(true);

                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void testinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteTestInfo(
                            interpreter, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void toinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                IToken token,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Show the token, if any.
                //
                if (token != null)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if ((informationHost != null) &&
                        !AppDomainOps.IsTransparentProxy(informationHost))
                    {
                        informationHost.SavePosition();

                        if (!informationHost.WriteTokenInfo(
                                interpreter, token,
                                DetailFlags.InteractiveAll, true))
                        {
                            informationHost.WriteResultLine(
                                ReturnCode.Error, HostWriteInfoError);
                        }

                        informationHost.RestorePosition(true);

                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = "no token information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void tcancel(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        bool cancel = false;

                        localCode = Value.GetBoolean2(
                            debugArguments[1], ValueFlags.AnyBoolean,
                            interpreter.CultureInfo, ref cancel,
                            ref localResult);

                        if (localCode == ReturnCode.Ok)
                            traceInfo.Cancel = cancel;
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                        localResult = traceInfo.Cancel;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void tcode(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        object enumValue = EnumOps.TryParseEnum(
                            typeof(ReturnCode), debugArguments[1],
                            true, true);

                        if (enumValue is ReturnCode)
                        {
                            traceInfo.ReturnCode = (ReturnCode)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = ScriptOps.BadValue(
                                null, "return code value", debugArguments[1],
                                Enum.GetNames(typeof(ReturnCode)), null, null);

                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                        localResult = traceInfo.ReturnCode;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void toldvalue(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if (debugArguments.Count >= 2)
                        traceInfo.OldValue = debugArguments[1];

                    localResult = StringOps.GetResultFromObject(
                        traceInfo.OldValue);

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void tnewvalue(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (traceInfo != null)
                {
                    if (debugArguments.Count >= 2)
                        traceInfo.NewValue = debugArguments[1];

                    localResult = StringOps.GetResultFromObject(
                        traceInfo.NewValue);

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = "no trace information available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void tinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ITraceInfo traceInfo,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(DetailFlags),
                        detailFlags.ToString(), debugArguments[1],
                        interpreter.CultureInfo, true, true, true,
                        ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        detailFlags = (DetailFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }

                if (localCode == ReturnCode.Ok)
                {
                    if (FlagOps.HasFlags(
                            detailFlags, DetailFlags.TraceCached, true) ||
                        (traceInfo != null))
                    {
                        IInformationHost informationHost =
                            interactiveHost as IInformationHost;

                        if ((informationHost != null) &&
                            !AppDomainOps.IsTransparentProxy(informationHost))
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteTraceInfo(
                                    interpreter, traceInfo, detailFlags,
                                    true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                HostOps.NoFeatureError,
                                typeof(IInformationHost).Name);

                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = "no trace information available";
                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void stack(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                CallStack callStack = interpreter.CallStack;

                if (callStack != null)
                {
                    localCode = ReturnCode.Ok;

                    int limit = 0;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        localCode = Value.GetInteger2(
                            (IGetValue)debugArguments[1], ValueFlags.AnyInteger,
                            interpreter.CultureInfo, ref limit, ref localResult);
                    }

                    DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 3) &&
                        !String.IsNullOrEmpty(debugArguments[2]))
                    {
                        object enumValue = EnumOps.TryParseFlagsEnum(
                            interpreter, typeof(DetailFlags),
                            detailFlags.ToString(), debugArguments[2],
                            interpreter.CultureInfo, true, true, true,
                            ref localResult);

                        if (enumValue is DetailFlags)
                        {
                            detailFlags = (DetailFlags)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localCode = ReturnCode.Error;
                        }
                    }

                    bool info = false;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 4) &&
                        !String.IsNullOrEmpty(debugArguments[3]))
                    {
                        localCode = Value.GetBoolean2(
                            debugArguments[3], ValueFlags.AnyBoolean,
                            interpreter.CultureInfo, ref info,
                            ref localResult);
                    }

                    if (localCode == ReturnCode.Ok)
                    {
                        IInformationHost informationHost =
                            interactiveHost as IInformationHost;

                        if ((informationHost != null) &&
                            !AppDomainOps.IsTransparentProxy(informationHost))
                        {
                            if (info)
                            {
                                informationHost.SavePosition();

                                if (!informationHost.WriteCallStackInfo(
                                        interpreter, callStack, limit,
                                        detailFlags, true))
                                {
                                    informationHost.WriteResultLine(
                                        ReturnCode.Error, HostWriteInfoError);
                                }

                                informationHost.RestorePosition(true);
                            }
                            else
                            {
                                // informationHost.SavePosition();

                                if (!informationHost.WriteCallStack(
                                        interpreter, callStack, limit,
                                        detailFlags, true))
                                {
                                    informationHost.WriteResultLine(
                                        ReturnCode.Error, HostWriteInfoError);
                                }

                                // informationHost.RestorePosition(true);
                            }

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                HostOps.NoFeatureError,
                                typeof(IInformationHost).Name);

                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = "no call stack available";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void finfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                EngineFlags engineFlags,
                SubstitutionFlags substitutionFlags,
                EventFlags eventFlags,
                ExpressionFlags expressionFlags,
                HeaderFlags headerFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteFlagInfo(
                            interpreter, engineFlags,
                            substitutionFlags, eventFlags,
                            expressionFlags, headerFlags,
                            DetailFlags.InteractiveAll, true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void lfinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                HeaderFlags localHeaderFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteFlagInfo(
                            interpreter, localEngineFlags,
                            localSubstitutionFlags, localEventFlags,
                            localExpressionFlags, localHeaderFlags,
                            DetailFlags.InteractiveAll, true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void frinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    localCode = ReturnCode.Ok;

                    DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                    if ((localCode == ReturnCode.Ok) &&
                        (debugArguments.Count >= 3) &&
                        !String.IsNullOrEmpty(debugArguments[2]))
                    {
                        object enumValue = EnumOps.TryParseFlagsEnum(
                            interpreter, typeof(DetailFlags),
                            detailFlags.ToString(), debugArguments[2],
                            interpreter.CultureInfo, true, true, true,
                            ref localResult);

                        if (enumValue is DetailFlags)
                        {
                            detailFlags = (DetailFlags)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localCode = ReturnCode.Error;
                        }
                    }

                    if (localCode == ReturnCode.Ok)
                    {
                        CallStack callStack = interpreter.CallStack;

                        if (callStack != null)
                        {
                            ICallFrame frame = null;

                            if (FlagOps.HasFlags(detailFlags,
                                    DetailFlags.CallStackAllFrames, true))
                            {
                                int index = 0;

                                localCode = Value.GetInteger2(
                                    (IGetValue)debugArguments[1],
                                    ValueFlags.AnyInteger,
                                    interpreter.CultureInfo, ref index,
                                    ref localResult);

                                if (localCode == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Verify that the index is within the
                                    //        bounds of the call stack.
                                    //
                                    int count = callStack.Count;

                                    if ((index >= 0) && (index < count))
                                    {
                                        frame = callStack[index];
                                    }
                                    else
                                    {
                                        localResult = String.Format("invalid " +
                                            "call frame index (there {0} {1} {2})",
                                            (count == 1) ? "is" : "are", count,
                                            (count == 1) ? "frame" : "frames");

                                        localCode = ReturnCode.Error;
                                    }
                                }
                            }
                            else
                            {
                                FrameResult frameResult = interpreter.GetCallFrame(
                                    debugArguments[1], ref frame, ref localResult);

                                if (frameResult != FrameResult.Invalid)
                                    localCode = ReturnCode.Ok;
                                else
                                    localCode = ReturnCode.Error;
                            }

                            if (localCode == ReturnCode.Ok)
                            {
                                IInformationHost informationHost =
                                    interactiveHost as IInformationHost;

                                if ((informationHost != null) &&
                                    !AppDomainOps.IsTransparentProxy(informationHost))
                                {
                                    informationHost.SavePosition();

                                    if (!informationHost.WriteCallFrameInfo(
                                            interpreter, frame, detailFlags,
                                            true))
                                    {
                                        informationHost.WriteResultLine(
                                            ReturnCode.Error, HostWriteInfoError);
                                    }

                                    informationHost.RestorePosition(true);

                                    localResult = String.Empty;
                                    localCode = ReturnCode.Ok;
                                }
                                else
                                {
                                    localResult = String.Format(
                                        HostOps.NoFeatureError,
                                        typeof(IInformationHost).Name);

                                    localCode = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            localResult = "no call stack available";
                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}frinfo level ?flags?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void einfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteEngineInfo(
                            interpreter, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void cinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteControlInfo(
                            interpreter, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void eninfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteEntityInfo(
                            interpreter, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void sinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool refresh = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref refresh,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        if (refresh)
                        {
#if NATIVE && WINDOWS
                            localCode = RuntimeOps.CheckForStackSpace(
                                interpreter, Engine.GetExtraStackSpace());

                            if (localCode != ReturnCode.Ok)
                                localResult = "check for stack space failed";
#else
                            localResult = "not implemented";
                            localCode = ReturnCode.Error;
#endif
                        }

                        if (localCode == ReturnCode.Ok)
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteStackInfo(
                                    interpreter, DetailFlags.InteractiveAll,
                                    true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);

                            localResult = String.Empty;
                            localCode = ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void histfile(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                if (debugArguments.Count >= 2)
                    interpreter.HistoryFileName = debugArguments[1];

                localResult = interpreter.HistoryFileName;
                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void histinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    IHistoryFilter historyFilter = interpreter.HistoryInfoFilter;

                    if (historyFilter == null)
                        historyFilter = HistoryOps.DefaultInfoFilter;

                    if (!informationHost.WriteHistoryInfo(
                            interpreter, historyFilter,
                            DetailFlags.InteractiveAll, true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void histclear(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                localCode = interpreter.ClearHistory(null, ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "history cleared";
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void histload(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                string fileName = interpreter.HistoryFileName;

                if (fileName == null)
                    fileName = DefaultHistoryFileName;

                if (debugArguments.Count >= 2)
                    fileName = debugArguments[1];

                IHistoryData historyData = interpreter.HistoryLoadData;

                if (historyData == null)
                    historyData = DefaultHistoryLoadData;

                IHistoryFilter historyFilter = interpreter.HistoryLoadFilter;

                if (historyFilter == null)
                    historyFilter = DefaultHistoryLoadFilter;

                localCode = interpreter.LoadHistory(
                    null, fileName, historyData, historyFilter, false,
                    ref localResult);

                if (localCode == ReturnCode.Ok)
                {
                    localResult = String.Format(
                        "history loaded from \"{0}\"",
                        fileName);
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void histsave(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if HISTORY
                string fileName = interpreter.HistoryFileName;

                if (fileName == null)
                    fileName = DefaultHistoryFileName;

                if (debugArguments.Count >= 2)
                    fileName = debugArguments[1];

                IHistoryData historyData = interpreter.HistorySaveData;

                if (historyData == null)
                    historyData = DefaultHistorySaveData;

                IHistoryFilter historyFilter = interpreter.HistorySaveFilter;

                if (historyFilter == null)
                    historyFilter = DefaultHistorySaveFilter;

                localCode = interpreter.SaveHistory(
                    null, fileName, historyData, historyFilter, false,
                    ref localResult);

                if (localCode == ReturnCode.Ok)
                {
                    localResult = String.Format(
                        "history saved to \"{0}\"",
                        fileName);
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void hinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                DetailFlags detailFlags = DetailFlags.InteractiveOnly;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(DetailFlags),
                        detailFlags.ToString(), debugArguments[1],
                        interpreter.CultureInfo, true, true, true,
                        ref localResult);

                    if (enumValue is DetailFlags)
                    {
                        detailFlags = (DetailFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }

                if (localCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        informationHost.SavePosition();

                        if (!informationHost.WriteHostInfo(
                                interpreter, detailFlags, true))
                        {
                            informationHost.WriteResultLine(
                                ReturnCode.Error, HostWriteInfoError);
                        }

                        informationHost.RestorePosition(true);

                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void iinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                Result result,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (localResult != null)
                    localResult.Flags |= ResultFlags.Local;

                if (result != null)
                    result.Flags |= ResultFlags.Global;

                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteInterpreterInfo(
                            interpreter, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);

                    localResult = String.Empty;
                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void fresc(
                ref bool forceCancel,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                forceCancel = !forceCancel; // NOTE: TOGGLE.

                localResult = String.Format(
                    "force reset cancel {0}",
                    ConversionOps.ToEnabled(forceCancel));

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void fresh(
                ref bool forceHalt,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                forceHalt = !forceHalt; // NOTE: TOGGLE.

                localResult = String.Format(
                    "force reset halt {0}",
                    ConversionOps.ToEnabled(forceHalt));

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void resc(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                bool reset = false;

                localCode = Engine.ResetCancel(
                    interpreter, CancelFlags.InteractiveManualResetHalt, ref reset,
                    ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = String.Format(
                        "cancel flags {0}", reset ? "reset" : "not reset");
            }

            ///////////////////////////////////////////////////////////////////////

            public static void resh(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                bool reset = false;

                localCode = Engine.ResetHalt(
                    interpreter, CancelFlags.InteractiveManualResetHalt, ref reset,
                    ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = String.Format(
                        "halt flags {0}", reset ? "reset" : "not reset");
            }

            ///////////////////////////////////////////////////////////////////////

            public static void rehash(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Core coreHost = interactiveHost as _Hosts.Core;

                if (coreHost != null)
                {
                    //
                    // BUGFIX: Force the currently loaded color settings
                    //         to be re-initialized based on the NoColor
                    //         setting.
                    //
                    coreHost.InitializeColors();

                    //
                    // NOTE: Reset the profile name if necessary.
                    //
                    if (debugArguments.Count >= 2)
                        coreHost.Profile = debugArguments[1];

                    //
                    // NOTE: Figure out the encoding that should be
                    //       used when reading the profile file.
                    //
                    string encodingName = null;

                    if (debugArguments.Count >= 3)
                        encodingName = debugArguments[2];

                    Encoding encoding = null;

                    if (encodingName != null)
                    {
                        localCode = interpreter.GetEncoding(
                            encodingName, LookupFlags.Default, ref encoding,
                            ref localResult);
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Reload any user-specific host profile here.
                        //
                        string fileName = coreHost.HostProfileFileName;

                        //
                        // NOTE: Using this interactive command overrides the
                        //       "NoProfile" option, if set, and forces the
                        //       profile to be loaded.
                        //
                        if (_Hosts.Core.LoadHostProfile(
                                interpreter, coreHost, coreHost.GetType(),
                                encoding, fileName, false, true,
                                ref localResult))
                        {
                            localResult = String.Format(
                                "host profile \"{0}\" reloaded",
                                fileName);

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = String.Format(
                                "colors initialized to defaults; " +
                                "failed to reload host profile: {0}",
                                localResult);

                            localCode = ReturnCode.Error;
                        }
                    }
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Core).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void deval(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult,
                ref int localErrorLine
                )
            {
#if DEBUGGER
                if (debugArguments.Count >= 2)
                {
                    Interpreter debugInterpreter = null;

                    if (Engine.CheckDebuggerInterpreter(
                            interpreter, false, ref debugInterpreter,
                            ref localResult))
                    {
                        localErrorLine = 0;

                        if (debugArguments.Count == 2)
                            localCode = debugInterpreter.EvaluateScript(
                                debugArguments[1], ref localResult,
                                ref localErrorLine);
                        else
                            localCode = debugInterpreter.EvaluateScript(
                                debugArguments, 1, ref localResult,
                                ref localErrorLine);
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}deval arg ?arg ...?\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void dsubst(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                if (debugArguments.Count >= 2)
                {
                    Interpreter debugInterpreter = null;

                    if (Engine.CheckDebuggerInterpreter(interpreter, false,
                            ref debugInterpreter, ref localResult))
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.None, Index.Invalid,
                                    Index.Invalid, "-nobackslashes", null),
                            new Option(null, OptionFlags.None, Index.Invalid,
                                    Index.Invalid, "-nocommands", null),
                            new Option(null, OptionFlags.None, Index.Invalid,
                                    Index.Invalid, "-novariables", null),
                            new Option(null, OptionFlags.None, Index.Invalid,
                                    Index.Invalid, Option.EndOfOptions, null)
                        });

                        int argumentIndex = Index.Invalid;

                        localCode = interpreter.GetOptions(
                            options, debugArguments, 0, 1, Index.Invalid, false,
                            ref argumentIndex, ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) &&
                                ((argumentIndex + 1) == debugArguments.Count))
                            {
                                SubstitutionFlags debugSubstitutionFlags =
                                    SubstitutionFlags.Default;

                                if (options.IsPresent("-nobackslashes"))
                                    debugSubstitutionFlags &= ~SubstitutionFlags.Backslashes;

                                if (options.IsPresent("-nocommands"))
                                    debugSubstitutionFlags &= ~SubstitutionFlags.Commands;

                                if (options.IsPresent("-novariables"))
                                    debugSubstitutionFlags &= ~SubstitutionFlags.Variables;

                                localCode = debugInterpreter.SubstituteString(
                                    debugArguments[argumentIndex],
                                    debugSubstitutionFlags, ref localResult);
                            }
                            else
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    Option.LooksLikeOption(debugArguments[argumentIndex]))
                                {
                                    localResult = OptionDictionary.BadOption(
                                        options, debugArguments[argumentIndex]);
                                }
                                else
                                {
                                    localResult = String.Format(
                                        "wrong # args: should be \"{0}dsubst " +
                                        "?-nobackslashes? ?-nocommands? " +
                                        "?-novariables? string\"",
                                        ShellOps.InteractiveCommandPrefix);
                                }

                                localCode = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "wrong # args: should be \"{0}dsubst " +
                        "?-nobackslashes? ?-nocommands? ?-novariables? string\"",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void suspend(
                Interpreter interpreter,
                bool debug,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In debug mode, we simply suspend debugging (stepping);
                //       otherwise, an error message is displayed.
                //
                if (debug)
                {
                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, true,
                            ref localDebugger, ref localResult))
                    {
                        localCode = localDebugger.Suspend(ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localResult = "debugger suspended";
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}suspend\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void resume(
                Interpreter interpreter,
                bool debug,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In debug mode, we simply resume debugging (stepping);
                //       otherwise, an error message is displayed.
                //
                if (debug)
                {
                    IDebugger localDebugger = null;

                    if (Engine.CheckDebugger(interpreter, true,
                            ref localDebugger, ref localResult))
                    {
                        localCode = localDebugger.Resume(ref localResult);

                        if (localCode == ReturnCode.Ok)
                            localResult = "debugger resumed";
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        "cannot \"{0}resume\" when not debugging",
                        ShellOps.InteractiveCommandPrefix);

                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void about(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (HelpOps.WriteBanner(
                        interpreter, false, false, false, false, false, false,
                        false, false) &&
                    (interactiveHost != null) && interactiveHost.WriteLine() &&
                    HelpOps.WriteLegalese(interpreter, false))
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        ReturnCode localCommandCode;
                        Result localCommandResult = null;

                        bool stable = false;

                        localCommandCode = Value.GetBoolean2(
                            debugArguments[1], ValueFlags.AnyBoolean,
                            interpreter.CultureInfo, ref stable,
                            ref localCommandResult);

                        string suffix = null;

                        if ((debugArguments.Count >= 3) &&
                            !String.IsNullOrEmpty(debugArguments[2]))
                        {
                            suffix = debugArguments[2];
                        }

                        if ((localCommandCode == ReturnCode.Ok) && stable)
                        {
                            string localValue = RuntimeOps.GetUpdatePathAndQuery(
                                GlobalState.GetAssemblyUpdateVersion(), null,
                                suffix);

                            localCommandCode = interpreter.SetVariableValue2(
                                VariableFlags.GlobalOnly, Vars.Platform.Name,
                                Vars.Platform.UpdatePathAndQueryName, localValue,
                                ref localCommandResult);

                            if (localCommandCode == ReturnCode.Ok)
                                localResult = String.Empty;
                            else
                                localResult = localCommandResult;

                            localCode = localCommandCode;
                        }
                        else
                        {
                            ResultList errors = new ResultList();

                            errors.Add("invalid, unstable argument");

                            if (localCommandResult != null)
                                errors.Add(localCommandResult);

                            localResult = errors;
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                }
                else
                {
                    localResult = "failed to display banner and/or license";
                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void chans(
                Interpreter interpreter,
                IStreamHost streamHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool replace = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref replace,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    ChannelType channelType = ChannelType.StandardChannels;

                    if (replace)
                        channelType |= ChannelType.AllowExist;

                    localCode = interpreter.ModifyStandardChannels(
                        streamHost, null, channelType, ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    localResult = String.Format(
                        "standard channels {0}",
                        replace ? "restored" : "present");
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void init(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool shell = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref shell,
                        ref localResult);
                }

                bool force = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref force,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    if (shell)
                    {
                        localCode = interpreter.InitializeShell(
                            force, ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            localResult = String.Format(
                                "shell {0}initialized",
                                force ? "force " : String.Empty);
                        }
                    }
                    else
                    {
                        localCode = interpreter.Initialize(
                            force, ref localResult);

                        if (localCode == ReturnCode.Ok)
                        {
                            localResult = String.Format(
                                "{0}initialized",
                                force ? "force " : String.Empty);
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void dpath(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool all = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref all,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    GlobalState.DisplayPaths(interpreter, all);
                    localResult = String.Empty;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void cancel(
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if CONSOLE
                try
                {
                    //
                    // NOTE: Simulate a Ctrl-C on the console.
                    //
                    Interpreter.ConsoleCancelEventHandler(null, null);

                    localResult = "cancellation initiated";
                    localCode = ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    localResult = e;
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void test(
                Interpreter interpreter,
                ArgumentList debugArguments,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult,
                ref int localErrorLine
                )
            {
                string pattern = null;

                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    pattern = debugArguments[1];
                }

                localCode = ReturnCode.Ok;

                bool all = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref all,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                {
                    localErrorLine = 0;

                    localCode = TestOps.ShellMain(
                        interpreter, pattern, localEngineFlags,
                        localSubstitutionFlags, localEventFlags,
                        localExpressionFlags, String.Compare(
                            debugArguments[0], String.Format("{0}ptest",
                                ShellOps.InteractiveCommandPrefix),
                            StringOps.SystemStringComparisonType) == 0 ?
                                TestPathType.Plugins : TestPathType.Default,
                        all, ref localResult, ref localErrorLine);
                }

                if (localCode == ReturnCode.Ok)
                    localResult = String.Empty;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void trustclr(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    StringList trustedPaths = interpreter.TrustedPaths;

                    if (trustedPaths != null)
                    {
                        trustedPaths.Clear();

                        localResult = "trusted directory list cleared";
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = "trusted directory list not available";
                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void trustdir(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    StringList trustedPaths = interpreter.TrustedPaths;

                    if (trustedPaths != null)
                    {
                        if (debugArguments.Count >= 2)
                            trustedPaths.Add(debugArguments[1]);

                        localResult = trustedPaths;
                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = "trusted directory list not available";
                        localCode = ReturnCode.Error;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void testdir(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                    interpreter.TestPath = debugArguments[1];

                localResult = StringList.MakeList(
                    String.Format("manual test path is \"{0}\"",
                        interpreter.TestPath),
                    String.Format("effective base test path is \"{0}\"",
                        TestOps.GetPath(interpreter, TestPathType.None)),
                    String.Format("effective library test path is \"{0}\"",
                        TestOps.GetPath(interpreter, TestPathType.Library)),
                    String.Format("effective plugin test path is \"{0}\"",
                        TestOps.GetPath(interpreter, TestPathType.Plugins)));

                localCode = ReturnCode.Ok;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void purge(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = CallFrameOps.Purge(interpreter, ref localResult);
            }

            ///////////////////////////////////////////////////////////////////////

            public static void restc(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                localCode = ReturnCode.Ok;

                bool strict = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref strict,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                    localCode = interpreter.RestoreCorePlugin(
                        strict, ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "core plugin restored";
            }

            ///////////////////////////////////////////////////////////////////////

            public static void restt(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NOTIFY && NOTIFY_ARGUMENTS
                localCode = ReturnCode.Ok;

                bool strict = false;

                if ((localCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref strict,
                        ref localResult);
                }

                if (localCode == ReturnCode.Ok)
                    localCode = interpreter.RestoreTracePlugin(
                        strict, ref localResult);

                if (localCode == ReturnCode.Ok)
                    localResult = "trace plugin restored";
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void restv(
                Interpreter interpreter,
                IEnumerable<string> args,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                StringList autoPathList = GlobalState.GetAutoPathList(
                    interpreter, false);

                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    localCode = interpreter.SetupVariables(
                        interpreter.CreateFlags, args, autoPathList, false,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localCode = interpreter.SetupPlatform(
                            interpreter.CreateFlags, false, ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localResult = "core variables restored";
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void vout(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                string channelId = StandardChannel.Output;

                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    channelId = debugArguments[1];
                }

                if ((debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    bool enabled = false;

                    localCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref enabled,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        localCode = interpreter.SetChannelVirtualOutput(
                            channelId, enabled, ref localResult);
                }
                else
                {
                    localCode = interpreter.GetChannelVirtualOutput(
                        channelId, ref localResult);
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void relimit(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    int readyLimit = 0;

                    localCode = Value.GetInteger2(
                        (IGetValue)debugArguments[1], ValueFlags.AnyInteger,
                        interpreter.CultureInfo, ref readyLimit,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        interpreter.ReadyLimit = readyLimit;
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ReadyLimit;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void rlimit(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if (debugArguments.Count >= 2)
                {
                    int recursionLimit = 0;

                    localCode = Value.GetInteger2(
                        (IGetValue)debugArguments[1], ValueFlags.AnyInteger,
                        interpreter.CultureInfo, ref recursionLimit,
                        ref localResult);

                    if (localCode == ReturnCode.Ok)
                        interpreter.RecursionLimit = recursionLimit;
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.RecursionLimit;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void ntypes(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NOTIFY || NOTIFY_OBJECT
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(NotifyType),
                        interpreter.NotifyTypes.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is NotifyFlags)
                    {
                        interpreter.NotifyTypes = (NotifyType)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.NotifyTypes;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void nflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NOTIFY || NOTIFY_OBJECT
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(NotifyFlags),
                        interpreter.NotifyFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is NotifyFlags)
                    {
                        interpreter.NotifyFlags = (NotifyFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.NotifyFlags;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void hflags(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                bool debug,
                ref HeaderFlags localHeaderFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(HeaderFlags),
                        DebuggerOps.GetHeaderFlags(interactiveHost,
                            interpreter.HeaderFlags | HeaderFlags.User,
                            debug, false, false, true).ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is HeaderFlags)
                    {
                        localHeaderFlags = (HeaderFlags)enumValue;
                        interpreter.HeaderFlags = localHeaderFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.HeaderFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void lhflags(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                bool debug,
                ref HeaderFlags localHeaderFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(HeaderFlags),
                        DebuggerOps.GetHeaderFlags(interactiveHost,
                            localHeaderFlags, debug, false, false,
                            true).ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is HeaderFlags)
                    {
                        localHeaderFlags = (HeaderFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localHeaderFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void cflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(CreateFlags),
                        interpreter.CreateFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is CreateFlags)
                    {
                        interpreter.CreateFlags = (CreateFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.CreateFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void dcflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(CreateFlags),
                        interpreter.DefaultCreateFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is CreateFlags)
                    {
                        interpreter.DefaultCreateFlags = (CreateFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultCreateFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void scflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(ScriptFlags),
                        interpreter.ScriptFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ScriptFlags)
                    {
                        interpreter.ScriptFlags = (ScriptFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ScriptFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void dscflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(ScriptFlags),
                        interpreter.DefaultScriptFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ScriptFlags)
                    {
                        interpreter.DefaultScriptFlags = (ScriptFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultScriptFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void iflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(InterpreterFlags),
                        interpreter.InterpreterFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InterpreterFlags)
                    {
                        interpreter.InterpreterFlags = (InterpreterFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InterpreterFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void diflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(InterpreterFlags),
                        interpreter.DefaultInterpreterFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InterpreterFlags)
                    {
                        interpreter.DefaultInterpreterFlags = (InterpreterFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultInterpreterFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void paflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(PackageFlags),
                        interpreter.PackageFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is PackageFlags)
                    {
                        interpreter.PackageFlags = (PackageFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.PackageFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void prflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(ProcedureFlags),
                        interpreter.ProcedureFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ProcedureFlags)
                    {
                        interpreter.ProcedureFlags = (ProcedureFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ProcedureFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void pflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(PluginFlags),
                        interpreter.PluginFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is PluginFlags)
                    {
                        interpreter.PluginFlags = (PluginFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.PluginFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void ceflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EngineFlags localEngineFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(EngineFlags),
                        interpreter.ContextEngineFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        localEngineFlags = (EngineFlags)enumValue;
                        interpreter.ContextEngineFlags = localEngineFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ContextEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void seflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EngineFlags localEngineFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(EngineFlags),
                        interpreter.SharedEngineFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        localEngineFlags = (EngineFlags)enumValue;
                        interpreter.SharedEngineFlags = localEngineFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.SharedEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void evflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(EventFlags),
                        interpreter.EngineEventFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EventFlags)
                    {
                        localEventFlags = (EventFlags)enumValue;
                        interpreter.EngineEventFlags = localEventFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.EngineEventFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void exflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(ExpressionFlags),
                        interpreter.ExpressionFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ExpressionFlags)
                    {
                        localExpressionFlags = (ExpressionFlags)enumValue;
                        interpreter.ExpressionFlags = localExpressionFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.ExpressionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void ieflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(EngineFlags),
                        interpreter.InteractiveEngineFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        interpreter.InteractiveEngineFlags =
                            (EngineFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void ievflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(EventFlags),
                        interpreter.InteractiveEventFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EventFlags)
                    {
                        interpreter.InteractiveEventFlags = (EventFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveEventFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void iexflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(ExpressionFlags),
                        interpreter.InteractiveExpressionFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ExpressionFlags)
                    {
                        interpreter.InteractiveExpressionFlags =
                            (ExpressionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveExpressionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void leflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EngineFlags localEngineFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(EngineFlags),
                        localEngineFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EngineFlags)
                    {
                        localEngineFlags = (EngineFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localEngineFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void levflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref EventFlags localEventFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(EventFlags),
                        localEventFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is EventFlags)
                    {
                        localEventFlags = (EventFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localEventFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void lexflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ExpressionFlags localExpressionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(ExpressionFlags),
                        localExpressionFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is ExpressionFlags)
                    {
                        localExpressionFlags = (ExpressionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localExpressionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void sflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref SubstitutionFlags localSubstitutionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(SubstitutionFlags),
                        interpreter.SubstitutionFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is SubstitutionFlags)
                    {
                        localSubstitutionFlags = (SubstitutionFlags)enumValue;
                        interpreter.SubstitutionFlags = localSubstitutionFlags;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.SubstitutionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void isflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(SubstitutionFlags),
                        interpreter.InteractiveSubstitutionFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is SubstitutionFlags)
                    {
                        interpreter.InteractiveSubstitutionFlags =
                            (SubstitutionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InteractiveSubstitutionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void izflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(InitializeFlags),
                        interpreter.InitializeFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InitializeFlags)
                    {
                        interpreter.InitializeFlags =
                            (InitializeFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.InitializeFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void dizflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(InitializeFlags),
                        interpreter.DefaultInitializeFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is InitializeFlags)
                    {
                        interpreter.DefaultInitializeFlags =
                            (InitializeFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = interpreter.DefaultInitializeFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void lsflags(
                Interpreter interpreter,
                ArgumentList debugArguments,
                ref SubstitutionFlags localSubstitutionFlags,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                if ((debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    object enumValue = EnumOps.TryParseFlagsEnum(
                        interpreter, typeof(SubstitutionFlags),
                        localSubstitutionFlags.ToString(),
                        debugArguments[1], interpreter.CultureInfo,
                        true, true, true, ref localResult);

                    if (enumValue is SubstitutionFlags)
                    {
                        localSubstitutionFlags = (SubstitutionFlags)enumValue;

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localCode = ReturnCode.Ok;
                }

                if (localCode == ReturnCode.Ok)
                    localResult = localSubstitutionFlags;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void step(
                Interpreter interpreter,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if DEBUGGER
                //
                // NOTE: In any mode, toggle the debug single step flag.
                //
                IDebugger localDebugger = null;

                if (Engine.CheckDebugger(interpreter, false,
                        ref localDebugger, ref localResult))
                {
                    localDebugger.SingleStep =
                        !localDebugger.SingleStep; // NOTE: TOGGLE.

                    localResult = String.Format("single step {0}",
                        ConversionOps.ToEnabled(localDebugger.SingleStep));

                    localCode = ReturnCode.Ok;
                }
                else
                {
                    localCode = ReturnCode.Error;
                }
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void style(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                _Hosts.Default defaultHost = interactiveHost as _Hosts.Default;

                if (defaultHost != null)
                {
                    if ((debugArguments.Count >= 2) &&
                        !String.IsNullOrEmpty(debugArguments[1]))
                    {
                        object enumValue = EnumOps.TryParseFlagsEnum(
                            interpreter, typeof(OutputStyle),
                            defaultHost.OutputStyle.ToString(), debugArguments[1],
                            interpreter.CultureInfo, true, true, true,
                            ref localResult);

                        if (enumValue is OutputStyle)
                        {
                            defaultHost.OutputStyle = (OutputStyle)enumValue;

                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localCode = ReturnCode.Ok;
                    }

                    if (localCode == ReturnCode.Ok)
                        localResult = defaultHost.OutputStyle;
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(_Hosts.Default).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void canexit(
                IInteractiveHost interactiveHost,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                IProcessHost processHost = interactiveHost as IProcessHost;

                if (processHost != null)
                {
                    if (FlagOps.HasFlags(
                            processHost.GetHostFlags(), HostFlags.Exit, true))
                    {
                        processHost.CanExit = !processHost.CanExit;

                        localResult = String.Format(
                            "exit {0}",
                            ConversionOps.ToEnabled(processHost.CanExit));

                        localCode = ReturnCode.Ok;
                    }
                    else
                    {
                        localResult = String.Format(
                            HostOps.NoFeatureError, HostFlags.Exit);

                        localCode = ReturnCode.Error;
                    }
                }
                else
                {
                    localResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IProcessHost).Name);

                    localCode = ReturnCode.Error;
                }
            }

            ///////////////////////////////////////////////////////////////////////

            public static void show(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                InteractiveLoopData loopData,
                Result result,
                HeaderFlags localHeaderFlags,
                ReturnCode localCode,
                Result localResult,
                ref bool show
                )
            {
                //
                // NOTE: Since one of the primary features of this interactive
                //       command is to display the local or master return code and
                //       result, we do not want to make any significant changes to
                //       it, ever; therefore, use new local variables to hold the
                //       results of this operation.
                //
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                bool local = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref local,
                        ref localCommandResult);
                }

                bool empty = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref empty,
                        ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    if (localResult != null)
                        localResult.Flags |= ResultFlags.Local;

                    if (result != null)
                        result.Flags |= ResultFlags.Global;

                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    bool proxy = AppDomainOps.IsTransparentProxy(informationHost);

                    if (informationHost != null)
                    {
                        //
                        // NOTE: Display the current debug information (even
                        //       when not in debug mode).
                        //
                        informationHost.WriteHeader(
                            interpreter, new InteractiveLoopData(
                            loopData, local || (loopData == null) ? localCode :
                            loopData.Code, !proxy && (loopData != null) ?
                            loopData.Token : null, !proxy &&
                            (loopData != null) ? loopData.TraceInfo : null,
                            DebuggerOps.GetHeaderFlags(
                                interactiveHost, localHeaderFlags,
                                (loopData != null) ? loopData.Debug : false,
                                true, empty, true) & ~HeaderFlags.AllPrompt),
                            local ? localResult : result);
                    }
                    else
                    {
                        localCommandResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCommandCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void overr(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                OptionDictionary options = new OptionDictionary(
                    new IOption[] {
                    new Option(null, OptionFlags.MustHaveReturnCodeValue,
                            Index.Invalid, Index.Invalid, "-code", null),
                    new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                            Index.Invalid, "-result", null),
                    new Option(null, OptionFlags.None, Index.Invalid,
                            Index.Invalid, Option.EndOfOptions, null)
                });

                int argumentIndex = Index.Invalid;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2))
                {
                    localCommandCode = interpreter.GetOptions(options,
                        debugArguments, 0, 1, Index.Invalid, false,
                        ref argumentIndex, ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    if (argumentIndex == Index.Invalid)
                    {
                        StringList list = new StringList();
                        Variant value = null;

                        if (options.IsPresent("-code", ref value))
                        {
                            localCode = (ReturnCode)value.Value;

                            list.Add(String.Format("return code set to {0}",
                                localCode));
                        }
                        else
                        {
                            list.Add("return code unchanged");
                        }

                        if (options.IsPresent("-result", ref value))
                        {
                            localResult = value.ToString();

                            list.Add(String.Format("result set to {0}",
                                FormatOps.WrapOrNull(true, true, localResult)));
                        }
                        else
                        {
                            list.Add("result unchanged");
                        }

                        localCommandResult = list;
                        localCommandCode = ReturnCode.Ok;
                    }
                    else
                    {
                        if ((argumentIndex != Index.Invalid) &&
                            Option.LooksLikeOption(debugArguments[argumentIndex]))
                        {
                            localCommandResult = OptionDictionary.BadOption(
                                options, debugArguments[argumentIndex]);
                        }
                        else
                        {
                            localCommandResult = String.Format(
                                "wrong # args: should be \"{0}overr ?options?\"",
                                ShellOps.InteractiveCommandPrefix);
                        }

                        localCommandCode = ReturnCode.Error;
                    }
                }

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       set it (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void prevr(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if PREVIOUS_RESULT
                Result previousResult = Interpreter.GetPreviousResult(
                    interpreter);

                if (previousResult != null)
                {
                    //
                    // NOTE: Set the local result equal to the previous result.
                    //
                    localResult = Result.Copy(previousResult, true); /* COPY */
                    localCode = previousResult.ReturnCode;

                    if (interactiveHost != null)
                    {
                        interactiveHost.WriteResultLine(
                            ReturnCode.Ok, "result rewound");
                    }
                }
                else if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(ReturnCode.Error,
                        "no previous result");
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       set it (i.e. they already know what it is).
                //
                show = false;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void nextr(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                ref bool show
                )
            {
#if PREVIOUS_RESULT
                if (localResult != null)
                {
                    Result previousResult =
                        Result.Copy(localCode, localResult, true); /* COPY */

                    //
                    // NOTE: Set the previous result equal to the local result.
                    //
                    Interpreter.SetPreviousResult(interpreter, previousResult);

                    if (interactiveHost != null)
                    {
                        interactiveHost.WriteResultLine(
                            ReturnCode.Ok, "previous result set");
                    }
                }
                else if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(ReturnCode.Error,
                        "no result");
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       set it (i.e. they already know what it is).
                //
                show = false;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void fresr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Fully reset the local and master results.
                //
                if (localResult == null)
                    localResult = String.Empty; /* SET */

                localResult.Reset(true); /* RESET */
                localCode = ReturnCode.Ok;

                if (result == null)
                    result = String.Empty; /* SET */

                result.Reset(true); /* RESET */
                code = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result reset (in-place)");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void resr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Reset the local and master results.
                //
                localResult = String.Empty;
                localCode = ReturnCode.Ok;

                result = Result.Copy(localResult, true); /* COPY */
                code = localCode;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result reset");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void clearr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Clears the local result.
                //
                localResult = String.Empty;
                localCode = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result cleared");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void nullr(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Nulls the local result.
                //
                localResult = null;
                localCode = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result nulled");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void copyr(
                IInteractiveHost interactiveHost,
                ReturnCode code,
                Result result,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Set the local result equal to the master result.
                //
                localResult = Result.Copy(result, true); /* COPY */
                localCode = code;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result copied");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void setr(
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                ref bool show,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Set the master result equal to the local result.
                //
                result = Result.Copy(localResult, true); /* COPY */
                code = localCode;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result set");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void mover(
                IInteractiveHost interactiveHost,
                ref bool show,
                ref ReturnCode localCode,
                ref Result localResult,
                ref ReturnCode code,
                ref Result result
                )
            {
                //
                // NOTE: Set the master result equal to the local result
                //       and then reset the local result.
                //
                result = localResult; /* MOVE */
                code = localCode;

                localResult = String.Empty;
                localCode = ReturnCode.Ok;

                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        ReturnCode.Ok, "result transferred");
                }

                //
                // NOTE: Skip displaying the local result since we just set it
                //       (i.e. they already know what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void lrinfo(
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                int localErrorLine,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                IInformationHost informationHost =
                    interactiveHost as IInformationHost;

                if (informationHost != null)
                {
                    informationHost.SavePosition();

                    if (!informationHost.WriteResultInfo(
                            "LocalResultInfo", localCode, localResult,
                            localErrorLine, DetailFlags.InteractiveAll,
                            true))
                    {
                        informationHost.WriteResultLine(
                            ReturnCode.Error, HostWriteInfoError);
                    }

                    informationHost.RestorePosition(true);
                }
                else
                {
                    localCommandResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IInformationHost).Name);

                    localCommandCode = ReturnCode.Error;
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void mrinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ReturnCode code,
                Result result,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                bool localPrevious = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2) &&
                    !String.IsNullOrEmpty(debugArguments[1]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[1], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref localPrevious,
                        ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    IInformationHost informationHost =
                        interactiveHost as IInformationHost;

                    if (informationHost != null)
                    {
                        if (localPrevious)
                        {
                            // informationHost.SavePosition();

                            if (!informationHost.WriteAllResultInfo(
                                    code, result,
                                    Interpreter.GetErrorLine(interpreter),
#if PREVIOUS_RESULT
                                    Interpreter.GetPreviousResult(interpreter),
#else
                                    null,
#endif
                                    DetailFlags.InteractiveAll, true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            // informationHost.RestorePosition(true);
                        }
                        else
                        {
                            informationHost.SavePosition();

                            if (!informationHost.WriteResultInfo(
                                    "MasterResultInfo", code, result,
                                    Interpreter.GetErrorLine(interpreter),
                                    DetailFlags.InteractiveAll, true))
                            {
                                informationHost.WriteResultLine(
                                    ReturnCode.Error, HostWriteInfoError);
                            }

                            informationHost.RestorePosition(true);
                        }

                        //
                        // NOTE: Skip displaying the local result since we may have
                        //       just shown it.
                        //
                        show = false;
                    }
                    else
                    {
                        localCommandResult = String.Format(
                            HostOps.NoFeatureError,
                            typeof(IInformationHost).Name);

                        localCommandCode = ReturnCode.Error;
                    }
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void rinfo(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ReturnCode localCode,
                Result localResult,
                int localErrorLine,
                ReturnCode code,
                Result result,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                IDebugHost debugHost = interactiveHost as IDebugHost;

                if (debugHost != null)
                {
                    // debugHost.SavePosition();

                    if (!debugHost.WriteResult("master result: ", code,
                            result, Interpreter.GetErrorLine(interpreter),
                            true))
                    {
                        debugHost.WriteResultLine(ReturnCode.Ok,
                            "no master result available");
                    }

                    if (!debugHost.WriteResult("local result: ", localCode,
                            localResult, localErrorLine, true))
                    {
                        debugHost.WriteResultLine(ReturnCode.Ok,
                            "no local result available");
                    }

                    // debugHost.RestorePosition(true);
                }
                else
                {
                    localCommandResult = String.Format(
                        HostOps.NoFeatureError,
                        typeof(IDebugHost).Name);

                    localCommandCode = ReturnCode.Error;
                }

                //
                // NOTE: If the above interactive command failed, display the
                //       reason why.
                //
                if ((localCommandCode != ReturnCode.Ok) &&
                    (interactiveHost != null))
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we may have just
                //       shown it.
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void sresult(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ArgumentList debugArguments,
                ReturnCode localCode,
                Result localResult,
                int localErrorLine,
                ReturnCode code,
                Result result,
                ref bool show
                )
            {
                ReturnCode localCommandCode = ReturnCode.Ok;
                Result localCommandResult = null;

                string varName = DefaultResultVarName;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 2))
                {
                    varName = debugArguments[1];
                }

                bool master = false;

                if ((localCommandCode == ReturnCode.Ok) &&
                    (debugArguments.Count >= 3) &&
                    !String.IsNullOrEmpty(debugArguments[2]))
                {
                    localCommandCode = Value.GetBoolean2(
                        debugArguments[2], ValueFlags.AnyBoolean,
                        interpreter.CultureInfo, ref master,
                        ref localCommandResult);
                }

                if (localCommandCode == ReturnCode.Ok)
                {
                    string varValue;

                    if (master)
                    {
                        varValue = (code == ReturnCode.Ok) ?
                            (string)result : ResultOps.Format(
                                code, result, Interpreter.GetErrorLine(
                                interpreter));
                    }
                    else
                    {
                        varValue = (localCode == ReturnCode.Ok) ?
                            (string)localResult : ResultOps.Format(
                                localCode, localResult, localErrorLine);
                    }

                    localCommandCode = interpreter.SetVariableValue(
                        VariableFlags.None, varName, varValue,
                        ref localCommandResult);
                }

                //
                // BUGFIX: We do not show the result value; however, we do need
                //         to show that *something* was just done.
                //
                if (interactiveHost != null)
                {
                    interactiveHost.WriteResultLine(
                        localCommandCode, (localCommandCode == ReturnCode.Ok) ?
                        (Result)String.Format("{0} result stored to variable \"{1}\"",
                        master ? "master" : "local", varName) : localCommandResult);
                }

                //
                // NOTE: Skip displaying the local result since we just stored it
                //       into a script variable (i.e. they can easily determine
                //       what it is).
                //
                show = false;
            }

            ///////////////////////////////////////////////////////////////////////

            public static void tclsh(
                Interpreter interpreter,
                IInteractiveHost interactiveHost,
                ref bool tclsh,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NATIVE && TCL
                tclsh = !tclsh; // NOTE: TOGGLE.

                interpreter.InteractiveMode = tclsh ?
                    TclInteractiveMode : EagleInteractiveMode;

                if (interactiveHost != null)
                    interactiveHost.RefreshTitle();

                localResult = String.Format(
                    "native tcl evaluation mode {0}",
                    ConversionOps.ToEnabled(tclsh));

                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void tclinterp(
                ArgumentList debugArguments,
                ref string tclInterpName,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
#if NATIVE && TCL
                if (debugArguments.Count >= 2)
                {
                    tclInterpName = debugArguments[1];

                    //
                    // NOTE: In this context an empty string means
                    //       "use any master Tcl interpreter" (i.e.
                    //       the old default behavior).
                    //
                    if (tclInterpName.Length == 0)
                        tclInterpName = null;
                }

                localResult = tclInterpName;
                localCode = ReturnCode.Ok;
#else
                localResult = "not implemented";
                localCode = ReturnCode.Error;
#endif
            }

            ///////////////////////////////////////////////////////////////////////

            public static void queue(
                Interpreter interpreter,
                bool noInteractive,
                bool trace,
                bool debug,
                EngineFlags localEngineFlags,
                SubstitutionFlags localSubstitutionFlags,
                EventFlags localEventFlags,
                ExpressionFlags localExpressionFlags,
                IClientData clientData,
                bool forceCancel,
                bool forceHalt,
                ref IInteractiveHost interactiveHost,
                ref string savedText,
                ref bool exit,
                ref bool done,
                ref bool previous,
                ref bool canceled,
                ref string text,
                ref bool notReady,
                ref Result parseError,
                ref ReturnCode localCode,
                ref Result localResult
                )
            {
                //
                // NOTE: Invoke the method directly responsible for getting a
                //       complete [logical] line of interactive input.
                //
                Interpreter.GetInteractiveInput(
                    interpreter, noInteractive, trace, debug, true,
                    localEngineFlags, localSubstitutionFlags, clientData,
                    forceCancel, forceHalt, ref interactiveHost,
                    ref savedText, ref exit, ref done, ref previous,
                    out canceled, out text, out notReady, out parseError);

                //
                // NOTE: Do they still want to queue up an event?
                //
                if (!done && !canceled && !notReady)
                {
                    if (!String.IsNullOrEmpty(text) &&
                        (text.Trim().Length > 0))
                    {
                        string name = FormatOps.Id(String.Format(
                            "loop{0}", interpreter.ActiveInteractiveLoops),
                            null, interpreter.NextId());

                        IScript script = Script.Create(
                            name, null, null, ScriptTypes.Queue, text,
                            TimeOps.GetUtcNow(), EngineMode.EvaluateScript,
                            ScriptFlags.None, localEngineFlags,
                            localSubstitutionFlags, localEventFlags,
                            localExpressionFlags, ClientData.Empty);

                        Thread queueThread = Engine.CreateThread(interpreter,
                            _Public.EventManager.QueueEventThreadStart, 0,
                            true, false);

                        if (queueThread != null)
                        {
                            queueThread.Name = String.Format(
                                "queueThread: {0}", interpreter);

                            queueThread.Start(new AnyPair<Interpreter, IScript>(
                                interpreter, script));

                            localResult = "queue thread started";
                            localCode = ReturnCode.Ok;
                        }
                        else
                        {
                            localResult = "could not create queue thread";
                            localCode = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        localResult = String.Empty;
                        localCode = ReturnCode.Ok;
                    }
                }
                else
                {
                    localResult = "queue event canceled";
                    localCode = ReturnCode.Error;
                }
            }
            #endregion
            #endregion
        }
#endif
        #endregion
    }
}
