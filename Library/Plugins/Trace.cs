/*
 * Trace.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    [ObjectId("d8b3cd8d-fa09-41a4-b042-eff42dd9a193")]
    [PluginFlags(
        PluginFlags.System | PluginFlags.Notify |
        PluginFlags.Static | PluginFlags.NoCommands |
        PluginFlags.NoFunctions | PluginFlags.NoPolicies |
        PluginFlags.NoTraces)]
    [NotifyTypes(NotifyType.Engine)]
    [NotifyFlags(NotifyFlags.Executed)]
    internal sealed class Trace : Notify
    {
        #region Private Constants
        //
        // HACK: These are purposely not marked as read-only.
        //
        private static string DefaultNormalFormat = "Notify: {0} ==> {1}";
        private static string DefaultDirectFormat = "{0} ==> {1}";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not marked as read-only.
        //
        private static string DefaultCategory = typeof(Trace).Name;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not marked as read-only.
        //
        private static bool DefaultDisabled = true; // TODO: Good default?
        private static bool DefaultDirect = true; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not marked as read-only.
        //
        private static bool DefaultNormalize = true; // TODO: Good default?
        private static bool DefaultEllipsis = true; // TODO: Good default?
        private static bool DefaultQuote = false; // TODO: Good default?
        private static bool DefaultDisplay = true; // TODO: Good default?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private string normalFormat;
        private string directFormat;

        ///////////////////////////////////////////////////////////////////////

        private string normalCategory;
        private string directCategory;

        ///////////////////////////////////////////////////////////////////////

        private bool disabled;
        private bool direct;

        ///////////////////////////////////////////////////////////////////////

        private bool normalizeArguments;
        private bool normalizeResult;
        private bool ellipsisArguments;
        private bool ellipsisResult;
        private bool quoteArguments;
        private bool quoteResult;
        private bool displayArguments;
        private bool displayResult;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Trace(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);

            ///////////////////////////////////////////////////////////////////

            /* IGNORED */
            UseDefaultSettings();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        protected override PackageFlags GetPackageFlags()
        {
            //
            // NOTE: We know the package is a core package because this is
            //       the core library and this class is sealed.
            //
            return PackageFlags.Core | base.GetPackageFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private ReturnCode UseDefaultSettings()
        {
            normalFormat = DefaultNormalFormat;
            directFormat = DefaultDirectFormat;

            ///////////////////////////////////////////////////////////////////

            normalCategory = DefaultCategory;
            directCategory = DefaultCategory;

            ///////////////////////////////////////////////////////////////////

            disabled = DefaultDisabled;
            direct = DefaultDirect;

            ///////////////////////////////////////////////////////////////////

            normalizeArguments = DefaultNormalize;
            normalizeResult = DefaultNormalize;
            ellipsisArguments = DefaultEllipsis;
            ellipsisResult = DefaultEllipsis;
            quoteArguments = DefaultQuote;
            quoteResult = DefaultQuote;
            displayArguments = DefaultDisplay;
            displayResult = DefaultDisplay;

            ///////////////////////////////////////////////////////////////////

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetBooleanSetting(
            Interpreter interpreter,
            string text,
            ref bool value,
            ref Result error
            )
        {
            CultureInfo cultureInfo = null;

            if (interpreter != null)
            {
                //
                // BUGFIX: The interpreter may have been disposed and we do
                //         not want to throw an exception; therefore, wrap
                //         the interpreter property access in a try block.
                //
                bool locked = false;

                try
                {
                    interpreter.InternalTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked && !interpreter.Disposed)
                        cultureInfo = interpreter.CultureInfo; /* throw */
                }
                catch
                {
                    // do nothing.
                }
                finally
                {
                    interpreter.InternalExitLock(ref locked); /* TRANSACTIONAL */
                }
            }

            return Value.GetBoolean2(
                text, ValueFlags.AnyBoolean, cultureInfo, ref value,
                ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public override ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            //
            // HACK: Cleanup the trace plugin token in the interpreter
            //       state because this is the only place where we can
            //       be 100% sure it will get done.
            //
            if (interpreter != null)
                interpreter.InternalTracePluginToken = 0;

            return base.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INotify Members
        public override ReturnCode Notify(
            Interpreter interpreter,
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            //
            // NOTE: If we are disabled -OR- there are no event arguments -OR-
            //       this event does not match the kind we are interested in
            //       then just return "success" now.
            //
            if (disabled || (eventArgs == null) ||
                !FlagOps.HasFlags(
                    eventArgs.NotifyTypes, NotifyType.Engine, false) ||
                !FlagOps.HasFlags(
                    eventArgs.NotifyFlags, NotifyFlags.Executed, false))
            {
                return ReturnCode.Ok;
            }

            //
            // NOTE: In "direct" mode, skip [almost] all the tracing ceremony
            //       and just call into Trace.WriteLine().  Otherwise, use the
            //       TraceOps class and all its special handling.  Either way,
            //       figure out the String.Format() arguments ahead of time,
            //       based on our current "normalize" and "ellipsis" settings.
            //
            try
            {
                string arg0 = FormatOps.WrapTraceOrNull(
                    normalizeArguments, ellipsisArguments, quoteArguments,
                    displayArguments, eventArgs.Arguments);

                string arg1 = FormatOps.WrapTraceOrNull(
                    normalizeResult, ellipsisResult, quoteResult,
                    displayResult, eventArgs.Result);

                if (direct)
                {
                    //
                    // NOTE: This is just an extremely thin wrapper around
                    //       the Trace.WriteLine method.
                    //
                    DebugOps.TraceWriteLine(String.Format(
                        directFormat, arg0, arg1), directCategory);
                }
                else
                {
                    //
                    // NOTE: Use the tracing subsystem.
                    //
                    TraceOps.DebugTrace(String.Format(
                        normalFormat, arg0, arg1), normalCategory,
                        TracePriority.EngineDebug);
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Trace).Name,
                    TracePriority.EngineError);

                result = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteRequest Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            object request,
            ref object response,
            ref Result error
            )
        {
            //
            // NOTE: This method is not supposed to raise an error under
            //       normal conditions when faced with an unrecognized
            //       request.  It simply does nothing and lets the base
            //       plugin handle it.
            //
            if (request is string[])
            {
                string[] operation = (string[])request;
                int length = operation.Length;

                if ((length == 1) && String.Equals(
                        operation[0], "useDefaultSettings",
                        StringOps.SystemStringComparisonType))
                {
                    response = UseDefaultSettings();
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "normalFormat",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        //
                        // NOTE: Since the String.Format method does NOT
                        //       permit the format parameter to be null,
                        //       fallback to the default format string
                        //       for that case.
                        //
                        if (operation[1] != null)
                            normalFormat = operation[1];
                        else
                            normalFormat = DefaultNormalFormat;
                    }

                    response = normalFormat;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "directFormat",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        //
                        // NOTE: Since the String.Format method does NOT
                        //       permit the format parameter to be null,
                        //       fallback to the default format string
                        //       for that case.
                        //
                        if (operation[1] != null)
                            directFormat = operation[1];
                        else
                            directFormat = DefaultDirectFormat;
                    }

                    response = directFormat;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "normalCategory",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        //
                        // NOTE: Since the DebugOps.TraceWriteLine method
                        //       permits the category parameter to be null
                        //       (or any other string), there is no need
                        //       to check its value here.
                        //
                        normalCategory = operation[1];
                    }

                    response = normalCategory;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "directCategory",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        //
                        // NOTE: Since the DebugOps.TraceWriteLine method
                        //       permits the category parameter to be null
                        //       (or any other string), there is no need
                        //       to check its value here.
                        //
                        directCategory = operation[1];
                    }

                    response = directCategory;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "disabled",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        disabled = boolValue;
                    }

                    response = disabled;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "direct",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        direct = boolValue;
                    }

                    response = direct;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "normalizeArguments",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        normalizeArguments = boolValue;
                    }

                    response = normalizeArguments;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "normalizeResult",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        normalizeResult = boolValue;
                    }

                    response = normalizeResult;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "ellipsisArguments",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        ellipsisArguments = boolValue;
                    }

                    response = ellipsisArguments;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "ellipsisResult",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        ellipsisResult = boolValue;
                    }

                    response = ellipsisResult;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "quoteArguments",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        quoteArguments = boolValue;
                    }

                    response = quoteArguments;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "quoteResult",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        quoteResult = boolValue;
                    }

                    response = quoteResult;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "displayArguments",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        displayArguments = boolValue;
                    }

                    response = displayArguments;
                    return ReturnCode.Ok;
                }

                if ((length >= 1) && (length <= 2) && String.Equals(
                        operation[0], "displayResult",
                        StringOps.SystemStringComparisonType))
                {
                    if (length >= 2)
                    {
                        bool boolValue = false;

                        if (GetBooleanSetting(
                                interpreter, operation[1], ref boolValue,
                                ref error) != ReturnCode.Ok)
                        {
                            return ReturnCode.Error;
                        }

                        displayResult = boolValue;
                    }

                    response = displayResult;
                    return ReturnCode.Ok;
                }
            }

            //
            // NOTE: Call the base plugin and let it handle the request.
            //
            return base.Execute(
                interpreter, clientData, request, ref response, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = FormatOps.PluginAbout(this, false);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Options(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = new StringList(DefineConstants.OptionList, false);
            return ReturnCode.Ok;
        }
        #endregion
    }
}
