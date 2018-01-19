using System;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("EA4BF7E1-563B-45F0-A39B-9826C21A81C5")]
    /*
     * POLICY: We allow files in the script library directory to be sourced.
     */
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("engine")]
    internal sealed class When : Core
    {
        public When(ICommandData commandData) : base(commandData) { }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count < 2)
            {
                result = "wrong # args: should be \"when ?options? script\"";
                return ReturnCode.Error;
            }

            ReturnCode code = ReturnCode.Ok;

            OptionDictionary options = new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-withinfo", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });

            int argumentIndex = Index.Invalid;

            if (arguments.Count > 2)
            {
                if (interpreter.GetOptions(
                        options, arguments, 0, 1, Index.Invalid, false,
                        ref argumentIndex, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                argumentIndex = 1;
            }

            

            Variant value = null;
            Encoding encoding = null;

            if (options.IsPresent("-encoding", ref value))
                encoding = (Encoding)value.Value;

            bool withInfo = false;

            if (options.IsPresent("-withinfo", ref value))
                withInfo = (bool)value.Value;

            if (code == ReturnCode.Ok)
            {
                string name = StringList.MakeList(
                    "source", arguments[argumentIndex]);

                ICallFrame frame = interpreter.NewTrackingCallFrame(
                    name, CallFrameFlags.Source);

                interpreter.PushAutomaticCallFrame(frame);

                try
                {
#if ARGUMENT_CACHE
                    CacheFlags savedCacheFlags = CacheFlags.None;

                    if (withInfo)
                    {
                        interpreter.BeginNoArgumentCache(
                            ref savedCacheFlags);
                    }

                    try
                    {
#endif
#if DEBUGGER && BREAKPOINTS
                        InterpreterFlags savedInterpreterFlags =
                            InterpreterFlags.None;

                        if (withInfo)
                        {
                            interpreter.BeginArgumentLocation(
                                ref savedInterpreterFlags);
                        }

                        try
                        {
#endif
                            code = interpreter.EvaluateScript(arguments[arguments.Count - 1],ref result);

#if DEBUGGER && BREAKPOINTS
                        }
                        finally
                        {
                            if (withInfo)
                            {
                                interpreter.EndArgumentLocation(
                                    ref savedInterpreterFlags);
                            }
                        }
#endif
#if ARGUMENT_CACHE
                    }
                    finally
                    {
                        if (withInfo)
                        {
                            interpreter.EndNoArgumentCache(
                                ref savedCacheFlags);
                        }
                    }
#endif
                }
                finally
                {
                    //
                    // NOTE: Pop the original call frame that we pushed above
                    //       and any intervening scope call frames that may be
                    //       leftover (i.e. they were not explicitly closed).
                    //
                    /* IGNORED */
                    interpreter.PopScopeCallFramesAndOneMore();
                }
            }

            return code;
        }
        #endregion

    }
}
