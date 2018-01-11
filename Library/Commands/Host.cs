/*
 * Host.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Commands
{
    [ObjectId("861cf95e-54ea-41db-9be3-16908ab0ec25")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Host : Core
    {
        public Host(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "beep", "boxstyle", "cancel", "clear", "close",
            "color", "echo", "errchan", "exit", "flags",
            "inchan", "isopen", "mode", "namedcolor", "open",
            "outchan", "outputstyle", "pause", "position", "query",
            "readchar", "readkey", "readline", "redirected", "reset",
            "result", "size", "sleep", "title", "write", "writebox"
        });

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "beep":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-frequency", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-duration", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    IHost host = interpreter.Host;

                                                    if (host != null)
                                                    {
                                                        Variant value = null;
                                                        int frequency = _Hosts.Default.BeepFrequency;

                                                        if (options.IsPresent("-frequency", ref value))
                                                            frequency = (int)value.Value;

                                                        int duration = _Hosts.Default.BeepDuration;

                                                        if (options.IsPresent("-duration", ref value))
                                                            duration = (int)value.Value;

                                                        if (host.Beep(frequency, duration))
                                                        {
                                                            result = String.Empty;
                                                        }
                                                        else
                                                        {
                                                            result = "failed to send beep to host";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"host beep ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host beep ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "boxstyle":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                _Hosts.Default defaultHost = host as _Hosts.Default;

                                                if (defaultHost != null)
                                                {
                                                    StringList boxCharacterSets = defaultHost.BoxCharacterSets;

                                                    if (boxCharacterSets != null)
                                                    {
                                                        if (arguments.Count == 3)
                                                        {
                                                            int boxCharacterSet = 0;

                                                            code = Value.GetInteger2(
                                                                (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                                                interpreter.CultureInfo, ref boxCharacterSet,
                                                                ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                int count = boxCharacterSets.Count;

                                                                if ((boxCharacterSet < 0) || (boxCharacterSet >= count))
                                                                {
                                                                    if (count > 0)
                                                                    {
                                                                        result = String.Format(
                                                                            "box style must be between 0 and {0}",
                                                                            count - 1);
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "no box character sets found";
                                                                    }

                                                                    code = ReturnCode.Error;
                                                                }
                                                                else
                                                                {
                                                                    defaultHost.BoxCharacterSet = boxCharacterSet;
                                                                }
                                                            }
                                                        }

                                                        if (code == ReturnCode.Ok)
                                                            result = defaultHost.BoxCharacterSet;
                                                    }
                                                    else
                                                    {
                                                        result = "box character sets not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "interpreter host does not have box style support";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host boxstyle ?style?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "cancel":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool force = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref force,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                IDebugHost debugHost = interpreter.Host;

                                                if (debugHost != null)
                                                {
                                                    code = debugHost.Cancel(force, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "interpreter host not available";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host cancel ?force?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "clear":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                if (host.Clear())
                                                {
                                                    result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "failed to clear host";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host clear\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "close":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                code = host.Close(ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host close\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "color":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-bg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-background", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-fg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-foreground", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    IColorHost colorHost = interpreter.Host;

                                                    if (colorHost != null)
                                                    {
                                                        ConsoleColor foregroundColor = _ConsoleColor.None;
                                                        ConsoleColor backgroundColor = _ConsoleColor.None;

                                                        if (colorHost.GetColors(ref foregroundColor, ref backgroundColor))
                                                        {
                                                            ConsoleColor oldForegroundColor = foregroundColor;
                                                            ConsoleColor oldBackgroundColor = backgroundColor;

                                                            Variant value = null;
                                                            bool foreground = false;

                                                            if (options.IsPresent("-fg", ref value) ||
                                                                options.IsPresent("-foreground", ref value))
                                                            {
                                                                foregroundColor = (ConsoleColor)value.Value;
                                                                foreground = true;
                                                            }

                                                            bool background = false;

                                                            if (options.IsPresent("-bg", ref value) ||
                                                                options.IsPresent("-background", ref value))
                                                            {
                                                                backgroundColor = (ConsoleColor)value.Value;
                                                                background = true;
                                                            }

                                                            if ((!foreground && !background) ||
                                                                colorHost.SetColors(foreground, background,
                                                                    foregroundColor, backgroundColor))
                                                            {
                                                                if (foreground && background)
                                                                    result = StringList.MakeList(
                                                                        oldForegroundColor, oldBackgroundColor);
                                                                else if (foreground)
                                                                    result = oldForegroundColor;
                                                                else if (background)
                                                                    result = oldBackgroundColor;
                                                                else
                                                                    result = StringList.MakeList(
                                                                        oldForegroundColor, oldBackgroundColor);
                                                            }
                                                            else
                                                            {
                                                                result = "could not set interpreter host colors";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "could not get interpreter host colors";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"host color ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host color ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "echo":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    bool echo = false;

                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref echo,
                                                        ref result);

                                                    if (code == ReturnCode.Ok)
                                                        host.Echo = echo;
                                                }

                                                if (code == ReturnCode.Ok)
                                                    result = host.Echo;
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host echo ?enabled?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "errchan":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IStreamHost streamHost = interpreter.Host;

                                            if (streamHost != null)
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    string channelId = arguments[2];
                                                    Channel channel = interpreter.GetChannel(channelId, ref result);

                                                    if (channel != null)
                                                    {
                                                        /* IGNORED */
                                                        channel.GetBinaryWriter();

                                                        streamHost.Error = channel.GetInnerStream();
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    Stream stream = streamHost.Error;

                                                    result = (stream != null) ? stream.ToString() : String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host errchan ?channel?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "exit":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool force = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref force,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                IDebugHost debugHost = interpreter.Host;

                                                if (debugHost != null)
                                                {
                                                    code = debugHost.Exit(force, ref result);

                                                    if (code == ReturnCode.Ok)
                                                        result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "interpreter host not available";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host exit ?force?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "flags":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IInteractiveHost interactiveHost = interpreter.Host;

                                            if (interactiveHost != null)
                                            {
                                                result = interactiveHost.GetHostFlags();
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host flags\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "inchan":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IStreamHost streamHost = interpreter.Host;

                                            if (streamHost != null)
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    string channelId = arguments[2];
                                                    Channel channel = interpreter.GetChannel(channelId, ref result);

                                                    if (channel != null)
                                                    {
                                                        /* IGNORED */
                                                        channel.GetBinaryReader();

                                                        streamHost.In = channel.GetInnerStream();
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    Stream stream = streamHost.In;

                                                    result = (stream != null) ? stream.ToString() : String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host inchan ?channel?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isopen":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IInteractiveHost interactiveHost = interpreter.Host;

                                            if (interactiveHost != null)
                                            {
                                                result = interactiveHost.IsOpen();
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host isopen\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "mode":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(ChannelType), null, arguments[2],
                                                    interpreter.CultureInfo, true, true, true);

                                                if (enumValue is ChannelType)
                                                {
                                                    ChannelType channelType = (ChannelType)enumValue;

                                                    if (arguments.Count == 4)
                                                    {
                                                        int intValue = 0;

                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                            interpreter.CultureInfo, ref intValue, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            uint mode = ConversionOps.ToUInt(intValue);

                                                            code = host.SetMode(
                                                                channelType, mode, ref result);
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        uint mode = 0;

                                                        code = host.GetMode(
                                                            channelType, ref mode, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = mode;
                                                    }
                                                }
                                                else
                                                {
                                                    result = ScriptOps.BadValue(
                                                        null, "console channel", arguments[2],
                                                        Enum.GetNames(typeof(ChannelType)),
                                                        null, null);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host mode channel ?mode?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "namedcolor":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-theme", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-name", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-bg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-background", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-fg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-foreground", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    IColorHost colorHost = interpreter.Host;

                                                    if (colorHost != null)
                                                    {
                                                        Variant value = null;
                                                        string theme = null;

                                                        if (options.IsPresent("-theme", ref value))
                                                            theme = value.ToString();

                                                        string name = null;

                                                        if (options.IsPresent("-name", ref value))
                                                            name = value.ToString();

                                                        ConsoleColor foregroundColor = _ConsoleColor.None;
                                                        ConsoleColor backgroundColor = _ConsoleColor.None;

                                                        code = colorHost.GetColors(theme, name, true, true,
                                                            ref foregroundColor, ref backgroundColor, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            ConsoleColor oldForegroundColor = foregroundColor;
                                                            ConsoleColor oldBackgroundColor = backgroundColor;

                                                            bool foreground = false;

                                                            if (options.IsPresent("-fg", ref value) ||
                                                                options.IsPresent("-foreground", ref value))
                                                            {
                                                                foregroundColor = (ConsoleColor)value.Value;
                                                                foreground = true;
                                                            }

                                                            bool background = false;

                                                            if (options.IsPresent("-bg", ref value) ||
                                                                options.IsPresent("-background", ref value))
                                                            {
                                                                backgroundColor = (ConsoleColor)value.Value;
                                                                background = true;
                                                            }

                                                            if ((!foreground && !background) ||
                                                                (colorHost.SetColors(theme, name, foreground, background,
                                                                    foregroundColor, backgroundColor, ref result) == ReturnCode.Ok))
                                                            {
                                                                if (foreground && background)
                                                                    result = StringList.MakeList(
                                                                        oldForegroundColor, oldBackgroundColor);
                                                                else if (foreground)
                                                                    result = oldForegroundColor;
                                                                else if (background)
                                                                    result = oldBackgroundColor;
                                                                else
                                                                    result = StringList.MakeList(
                                                                        oldForegroundColor, oldBackgroundColor);
                                                            }
                                                            else
                                                            {
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"host namedcolor ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host namedcolor ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "open":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                code = host.Open(ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = String.Empty;
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host open\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "outchan":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IStreamHost streamHost = interpreter.Host;

                                            if (streamHost != null)
                                            {
                                                if (arguments.Count == 3)
                                                {
                                                    string channelId = arguments[2];
                                                    Channel channel = interpreter.GetChannel(channelId, ref result);

                                                    if (channel != null)
                                                    {
                                                        /* IGNORED */
                                                        channel.GetBinaryWriter();

                                                        streamHost.Out = channel.GetInnerStream();
                                                    }
                                                    else
                                                    {
                                                        code = ReturnCode.Error;
                                                    }
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    Stream stream = streamHost.Out;

                                                    result = (stream != null) ? stream.ToString() : String.Empty;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host outchan ?channel?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "outputstyle":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                _Hosts.Default defaultHost = host as _Hosts.Default;

                                                if (defaultHost != null)
                                                {
                                                    if (arguments.Count == 3)
                                                    {
                                                        object enumValue = EnumOps.TryParseFlagsEnum(
                                                            interpreter, typeof(OutputStyle),
                                                            defaultHost.OutputStyle.ToString(),
                                                            arguments[2], interpreter.CultureInfo,
                                                            true, true, true, ref result);

                                                        if (enumValue is OutputStyle)
                                                            defaultHost.OutputStyle = (OutputStyle)enumValue;
                                                        else
                                                            code = ReturnCode.Error;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                        result = defaultHost.OutputStyle;
                                                }
                                                else
                                                {
                                                    result = "interpreter host does not have output style support";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host outputstyle ?style?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "pause":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IInteractiveHost interactiveHost = interpreter.Host;

                                            if (interactiveHost != null)
                                            {
                                                if (interactiveHost.Pause())
                                                {
                                                    result = String.Empty;
                                                }
                                                else
                                                {
                                                    result = "failed to pause host";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host pause\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "position":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-x", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-relx", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-y", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-rely", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    IPositionHost positionHost = interpreter.Host;

                                                    if (positionHost != null)
                                                    {
                                                        int left = _Position.Invalid;
                                                        int top = _Position.Invalid;

                                                        if (positionHost.GetPosition(ref left, ref top))
                                                        {
                                                            bool setLeft = false;
                                                            bool setTop = false;
                                                            Variant value = null;

                                                            if (options.IsPresent("-x", ref value))
                                                            {
                                                                left = (int)value.Value;
                                                                setLeft = true;
                                                            }

                                                            if (options.IsPresent("-relx", ref value))
                                                            {
                                                                left += (int)value.Value;
                                                                setLeft = true;
                                                            }

                                                            if (options.IsPresent("-y", ref value))
                                                            {
                                                                top = (int)value.Value;
                                                                setTop = true;
                                                            }

                                                            if (options.IsPresent("-rely", ref value))
                                                            {
                                                                top += (int)value.Value;
                                                                setTop = true;
                                                            }

                                                            if ((!setLeft && !setTop) || positionHost.SetPosition(left, top))
                                                            {
                                                                if (setLeft && setTop)
                                                                    result = StringList.MakeList(left, top);
                                                                else if (setLeft)
                                                                    result = left;
                                                                else if (setTop)
                                                                    result = top;
                                                                else
                                                                    result = StringList.MakeList(left, top);
                                                            }
                                                            else
                                                            {
                                                                result = "could not set interpreter host position";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "could not get interpreter host position";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"host position ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host position ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "query":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IHost host = interpreter.Host;

                                            if (host != null)
                                            {
                                                if (FlagOps.HasFlags(
                                                        host.GetHostFlags(), HostFlags.QueryState, true))
                                                {
                                                    try
                                                    {
                                                        result = host.QueryState(DetailFlags.ScriptOnly);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        result = e;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "interpreter host does not have query support";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host query\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readchar":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            IReadHost readHost = interpreter.Host;

                                            if (readHost != null)
                                            {
                                                try
                                                {
                                                    int value = 0;

                                                    if (readHost.Read(ref value))
                                                    {
                                                        result = value;
                                                        code = ReturnCode.Ok;
                                                    }
                                                    else
                                                    {
                                                        result = "unable to read character from host";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    result = e;
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host readchar\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readkey":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool intercept = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref intercept,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                IReadHost readHost = interpreter.Host;

                                                if (readHost != null)
                                                {
                                                    try
                                                    {
#if CONSOLE
                                                        IClientData value = null;

                                                        if (readHost.ReadKey(intercept, ref value))
                                                        {
                                                            result = FormatOps.ConsoleKeyInfo(
                                                                (ConsoleKeyInfo)value.Data);
                                                        }
                                                        else
                                                        {
                                                            result = "unable to read key from host";
                                                            code = ReturnCode.Error;
                                                        }
#else
                                                        int value = 0;

                                                        if (readHost.Read(ref value))
                                                        {
                                                            result = new StringList("KeyChar",
                                                                ConversionOps.ToChar(value).ToString());
                                                        }
                                                        else
                                                        {
                                                            result = "unable to read character from host";
                                                            code = ReturnCode.Error;
                                                        }
#endif
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        result = e;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "interpreter host not available";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host readkey ?intercept?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "readline":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            bool nullOk = false;

                                            if (arguments.Count == 3)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[2], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref nullOk,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                IInteractiveHost interactiveHost = interpreter.Host;

                                                if (interactiveHost != null)
                                                {
                                                    try
                                                    {
                                                        string value = null;

                                                        if (interactiveHost.ReadLine(ref value))
                                                        {
                                                            if (nullOk || (value != null))
                                                            {
                                                                result = value;
                                                            }
                                                            else
                                                            {
                                                                result = "null line read from host";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "unable to read line from host";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        result = e;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "interpreter host not available";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host readline ?nullOk?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "redirected":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IStreamHost streamHost = interpreter.Host;

                                            if (streamHost != null)
                                            {
                                                object enumValue = EnumOps.TryParseFlagsEnum(
                                                    interpreter, typeof(ChannelType), null, arguments[2],
                                                    interpreter.CultureInfo, true, true, true);

                                                if (enumValue is ChannelType)
                                                {
                                                    ChannelType channelType = (ChannelType)enumValue;

                                                    if (channelType == ChannelType.Input)
                                                    {
                                                        result = streamHost.IsInputRedirected();
                                                    }
                                                    else if (channelType == ChannelType.Output)
                                                    {
                                                        result = streamHost.IsOutputRedirected();
                                                    }
                                                    else if (channelType == ChannelType.Error)
                                                    {
                                                        result = streamHost.IsErrorRedirected();
                                                    }
                                                    else
                                                    {
                                                        StringList list = new StringList();

                                                        if (FlagOps.HasFlags(channelType, ChannelType.Input, true))
                                                        {
                                                            list.Add(ChannelType.Input.ToString());
                                                            list.Add(streamHost.IsInputRedirected().ToString());
                                                        }

                                                        if (FlagOps.HasFlags(channelType, ChannelType.Output, true))
                                                        {
                                                            list.Add(ChannelType.Output.ToString());
                                                            list.Add(streamHost.IsOutputRedirected().ToString());
                                                        }

                                                        if (FlagOps.HasFlags(channelType, ChannelType.Error, true))
                                                        {
                                                            list.Add(ChannelType.Error.ToString());
                                                            list.Add(streamHost.IsErrorRedirected().ToString());
                                                        }

                                                        result = list;
                                                    }
                                                }
                                                else
                                                {
                                                    result = ScriptOps.BadValue(
                                                        null, "console channel", arguments[2],
                                                        Enum.GetNames(typeof(ChannelType)),
                                                        null, null);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host redirected channel\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "reset":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(HostSizeType), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-sizetype", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-channels", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-flags", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-interface", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-input", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-output", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-error", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-size", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-position", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-colors", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    IHost host = interpreter.Host;

                                                    if (host != null)
                                                    {
                                                        Variant value = null;
                                                        HostSizeType hostSizeType = HostSizeType.Default;

                                                        if (options.IsPresent("-sizetype", ref value))
                                                            hostSizeType = (HostSizeType)value.Value;

                                                        bool channels = false;

                                                        if (options.IsPresent("-channels"))
                                                            channels = true;

                                                        bool flags = false;

                                                        if (options.IsPresent("-flags"))
                                                            flags = true;

                                                        bool @interface = false;

                                                        if (options.IsPresent("-interface"))
                                                            @interface = true;

                                                        bool input = false;

                                                        if (options.IsPresent("-input"))
                                                            input = true;

                                                        bool output = false;

                                                        if (options.IsPresent("-output"))
                                                            output = true;

                                                        bool error = false;

                                                        if (options.IsPresent("-error"))
                                                            error = true;

                                                        bool size = false;

                                                        if (options.IsPresent("-size"))
                                                            size = true;

                                                        bool position = false;

                                                        if (options.IsPresent("-position"))
                                                            position = true;

                                                        bool colors = false;

                                                        if (options.IsPresent("-colors"))
                                                            colors = true;

                                                        bool all = false;

                                                        if (options.IsPresent("-all"))
                                                            all = true;

                                                        if ((!@interface && !all) ||
                                                            (host.Reset(ref result) == ReturnCode.Ok))
                                                        {
                                                            if ((!flags && !all) || host.ResetFlags())
                                                            {
                                                                if ((!input && !all) || host.ResetIn())
                                                                {
                                                                    if ((!output && !all) || host.ResetOut())
                                                                    {
                                                                        if ((!error && !all) || host.ResetError())
                                                                        {
                                                                            if ((!size && !all) || host.ResetSize(hostSizeType))
                                                                            {
                                                                                if ((!position && !all) || host.ResetPosition())
                                                                                {
                                                                                    if ((!colors && !all) || host.ResetColors())
                                                                                    {
                                                                                        if ((!channels && !all) ||
                                                                                            (interpreter.ResetStandardChannels(
                                                                                                host, ref result) == ReturnCode.Ok))
                                                                                        {
                                                                                            StringList list = new StringList();

                                                                                            if (all || @interface)
                                                                                                list.Add("interface");

                                                                                            if (all || flags)
                                                                                                list.Add("flags");

                                                                                            if (all || input)
                                                                                                list.Add("input");

                                                                                            if (all || output)
                                                                                                list.Add("output");

                                                                                            if (all || error)
                                                                                                list.Add("error");

                                                                                            if (all || size)
                                                                                                list.Add("size");

                                                                                            if (all || position)
                                                                                                list.Add("position");

                                                                                            if (all || colors)
                                                                                                list.Add("colors");

                                                                                            if (all || channels)
                                                                                                list.Add("channels");

                                                                                            result = GenericOps<string>.ListToEnglish(
                                                                                                list, ", ", Characters.Space.ToString(),
                                                                                                "and ");

                                                                                            if (!String.IsNullOrEmpty(result))
                                                                                                result += " reset";
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            result = "could not reset interpreter standard channels";
                                                                                            code = ReturnCode.Error;
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = "could not reset interpreter host colors";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = "could not reset interpreter host position";
                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                result = "could not reset interpreter host size";
                                                                                code = ReturnCode.Error;
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "could not reset interpreter host error stream";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "could not reset interpreter host output stream";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = "could not reset interpreter host input stream";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "could not reset interpreter host flags";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"host reset ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host reset ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "result":
                                    {
                                        if ((arguments.Count == 4) || (arguments.Count == 5))
                                        {
                                            ReturnCode returnCode = ReturnCode.Ok;

                                            code = Value.GetReturnCode2(
                                                arguments[2], ValueFlags.AnyReturnCode,
                                                interpreter.CultureInfo, ref returnCode,
                                                ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                IDebugHost debugHost = interpreter.Host;

                                                if (debugHost != null)
                                                {
                                                    int errorLine = 0;

                                                    if (arguments.Count == 5)
                                                    {
                                                        code = Value.GetInteger2(
                                                            (IGetValue)arguments[4], ValueFlags.AnyInteger,
                                                            interpreter.CultureInfo, ref errorLine,
                                                            ref result);
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        /* IGNORED */
                                                        debugHost.WriteResult(returnCode,
                                                            arguments[3], errorLine, true, false);

                                                        result = String.Empty;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "interpreter host not available";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host result code result ?errorLine?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "size":
                                    {
                                        if (arguments.Count >= 2)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(HostSizeType), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-sizetype", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-norestore", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-width", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-relwidth", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-height", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-relheight", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 2)
                                                code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    ISizeHost sizeHost = interpreter.Host;

                                                    if (sizeHost != null)
                                                    {
                                                        Variant value = null;
                                                        HostSizeType hostSizeType = HostSizeType.Default;

                                                        if (options.IsPresent("-sizetype", ref value))
                                                            hostSizeType = (HostSizeType)value.Value;

                                                        bool restore = true;

                                                        if (options.IsPresent("-norestore"))
                                                            restore = false;

                                                        int width = _Size.Invalid;
                                                        int height = _Size.Invalid;

                                                        if (sizeHost.GetSize(hostSizeType, ref width, ref height))
                                                        {
                                                            bool setWidth = false;
                                                            bool setHeight = false;

                                                            if (options.IsPresent("-width", ref value))
                                                            {
                                                                width = (int)value.Value;
                                                                setWidth = true;
                                                            }
                                                            else if (options.IsPresent("-relwidth", ref value))
                                                            {
                                                                width += (int)value.Value;
                                                                setWidth = true;
                                                            }

                                                            if (options.IsPresent("-height", ref value))
                                                            {
                                                                height = (int)value.Value;
                                                                setHeight = true;
                                                            }
                                                            else if (options.IsPresent("-relheight", ref value))
                                                            {
                                                                height += (int)value.Value;
                                                                setHeight = true;
                                                            }

                                                            if ((!setWidth && !setHeight) ||
                                                                sizeHost.SetSize(hostSizeType, width, height))
                                                            {
                                                                if (setWidth && setHeight)
                                                                    result = StringList.MakeList(width, height);
                                                                else if (setWidth)
                                                                    result = width;
                                                                else if (setHeight)
                                                                    result = height;
                                                                else
                                                                    result = StringList.MakeList(width, height);
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: We failed to set the new size.  For safety,
                                                                //       reset the original size unless we have been
                                                                //       instructed not to do so.
                                                                //
                                                                if (restore)
                                                                    /* IGNORED */
                                                                    sizeHost.ResetSize(hostSizeType);

                                                                result = "could not set interpreter host size";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "could not get interpreter host size";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"host size ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host size ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "sleep":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            IThreadHost threadHost = interpreter.Host;

                                            if (threadHost != null)
                                            {
                                                int milliseconds = 0;

                                                code = Value.GetInteger2(
                                                    (IGetValue)arguments[2], ValueFlags.AnyInteger,
                                                    interpreter.CultureInfo, ref milliseconds,
                                                    ref result);

                                                if (code == ReturnCode.Ok)
                                                {
                                                    if (FlagOps.HasFlags(threadHost.GetHostFlags(), HostFlags.Sleep, true))
                                                    {
                                                        if (threadHost.Sleep(milliseconds))
                                                        {
                                                            result = String.Empty;
                                                        }
                                                        else
                                                        {
                                                            result = "could not sleep";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host does not have sleep support";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host sleep milliseconds\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "title":
                                    {
                                        if ((arguments.Count == 2) || (arguments.Count == 3))
                                        {
                                            IInteractiveHost interactiveHost = interpreter.Host;

                                            if (interactiveHost != null)
                                            {
                                                if (arguments.Count == 3)
                                                    interactiveHost.Title = arguments[2];

                                                result = interactiveHost.Title;
                                            }
                                            else
                                            {
                                                result = "interpreter host not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host title ?title?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "write":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            bool newLine = false;

                                            if (arguments.Count == 4)
                                            {
                                                code = Value.GetBoolean2(
                                                    arguments[3], ValueFlags.AnyBoolean,
                                                    interpreter.CultureInfo, ref newLine,
                                                    ref result);
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                IInteractiveHost interactiveHost = interpreter.Host;

                                                if (interactiveHost != null)
                                                {
                                                    if (newLine)
                                                    {
                                                        if (interactiveHost.WriteLine(arguments[2]))
                                                        {
                                                            result = String.Empty;
                                                        }
                                                        else
                                                        {
                                                            result = "unable to write line to host";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (interactiveHost.Write(arguments[2]))
                                                        {
                                                            result = String.Empty;
                                                        }
                                                        else
                                                        {
                                                            result = "unable to write to host";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = "interpreter host not available";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host write value ?newLine?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "writebox":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-theme", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-name", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-x", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-relx", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-y", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-rely", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-bg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-background", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-fg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-foreground", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-boxbg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-boxbackground", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-boxfg", null),
                                                new Option(typeof(ConsoleColor), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-boxforeground", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-multiple", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noposition", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noboxcolors", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocolors", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-pairs", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-newline", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-separator", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-norestore", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, true, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) == arguments.Count))
                                                {
                                                    IDisplayHost displayHost = interpreter.Host;

                                                    if (displayHost != null)
                                                    {
                                                        bool noPosition = false;

                                                        if (options.IsPresent("-noposition"))
                                                            noPosition = true;

                                                        int left = noPosition ? 0 : _Position.Invalid;
                                                        int top = noPosition ? 0 : _Position.Invalid;

                                                        if (noPosition || displayHost.GetPosition(ref left, ref top))
                                                        {
                                                            Variant value = null;
                                                            string theme = null;

                                                            if (options.IsPresent("-theme", ref value))
                                                                theme = value.ToString();

                                                            bool noBoxColors = false;

                                                            if (options.IsPresent("-noboxcolors"))
                                                                noBoxColors = true;

                                                            ConsoleColor boxForegroundColor = _ConsoleColor.None;
                                                            ConsoleColor boxBackgroundColor = _ConsoleColor.None;

                                                            code = !noBoxColors ? displayHost.GetColors(
                                                                theme, _Hosts.Default.BoxColorPrefix, true, true,
                                                                ref boxForegroundColor, ref boxBackgroundColor,
                                                                ref result) : ReturnCode.Ok;

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                bool noColors = false;

                                                                if (options.IsPresent("-nocolors"))
                                                                    noColors = true;

                                                                ConsoleColor foregroundColor = _ConsoleColor.None;
                                                                ConsoleColor backgroundColor = _ConsoleColor.None;

                                                                if (noColors || displayHost.GetColors(
                                                                        ref foregroundColor, ref backgroundColor))
                                                                {
                                                                    string name = null;

                                                                    if (options.IsPresent("-name", ref value))
                                                                        name = value.ToString();

                                                                    if (options.IsPresent("-x", ref value))
                                                                        left = (int)value.Value;

                                                                    if (options.IsPresent("-relx", ref value))
                                                                        left += (int)value.Value;

                                                                    if (options.IsPresent("-y", ref value))
                                                                        top = (int)value.Value;

                                                                    if (options.IsPresent("-rely", ref value))
                                                                        top += (int)value.Value;

                                                                    if (options.IsPresent("-fg", ref value) ||
                                                                        options.IsPresent("-foreground", ref value))
                                                                    {
                                                                        foregroundColor = (ConsoleColor)value.Value;
                                                                    }

                                                                    if (options.IsPresent("-bg", ref value) ||
                                                                        options.IsPresent("-background", ref value))
                                                                    {
                                                                        backgroundColor = (ConsoleColor)value.Value;
                                                                    }

                                                                    if (options.IsPresent("-boxfg", ref value) ||
                                                                        options.IsPresent("-boxforeground", ref value))
                                                                    {
                                                                        boxForegroundColor = (ConsoleColor)value.Value;
                                                                    }

                                                                    if (options.IsPresent("-boxbg", ref value) ||
                                                                        options.IsPresent("-boxbackground", ref value))
                                                                    {
                                                                        boxBackgroundColor = (ConsoleColor)value.Value;
                                                                    }

                                                                    bool multiple = false;

                                                                    if (options.IsPresent("-multiple"))
                                                                        multiple = true;

                                                                    bool pairs = false;

                                                                    if (options.IsPresent("-pairs"))
                                                                        pairs = true;

                                                                    bool newLine = false;

                                                                    if (options.IsPresent("-newline"))
                                                                        newLine = true;

                                                                    bool restore = true;

                                                                    if (options.IsPresent("-norestore"))
                                                                        restore = false;

                                                                    bool separator = false;

                                                                    if (options.IsPresent("-separator"))
                                                                        separator = true;

                                                                    if (multiple)
                                                                    {
                                                                        StringList list = null;

                                                                        code = Parser.SplitList(
                                                                            interpreter, arguments[argumentIndex], 0,
                                                                            Length.Invalid, true, ref list, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            if (separator)
                                                                            {
                                                                                for (int index = 0; index < list.Count; index++)
                                                                                {
                                                                                    if (String.Compare(list[index], _Constants._String.Null,
                                                                                            StringOps.SystemStringComparisonType) == 0)
                                                                                    {
                                                                                        list[index] = null;
                                                                                    }
                                                                                }
                                                                            }

                                                                            StringPairList list2 = null;

                                                                            if (pairs)
                                                                            {
                                                                                if ((list.Count % 2) == 0)
                                                                                {
                                                                                    list2 = new StringPairList();

                                                                                    for (int index = 0; index < list.Count; index += 2)
                                                                                        list2.Add(new StringPair(list[index], list[index + 1]));
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = "pair list must have an even number of elements";
                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                list2 = new StringPairList(list);
                                                                            }

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                if (displayHost.WriteBox(
                                                                                        name, list2, null, newLine, restore, ref left, ref top,
                                                                                        foregroundColor, backgroundColor, boxForegroundColor,
                                                                                        boxBackgroundColor))
                                                                                {
                                                                                    result = StringList.MakeList(left, top);
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = "could not write box to interpreter host";
                                                                                    code = ReturnCode.Error;
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        if (displayHost.WriteBox(
                                                                                name, arguments[argumentIndex], null, newLine, restore,
                                                                                ref left, ref top, foregroundColor, backgroundColor,
                                                                                boxForegroundColor, boxBackgroundColor))
                                                                        {
                                                                            result = StringList.MakeList(left, top);
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "could not write box to interpreter host";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = "could not get interpreter host colors";
                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            result = "could not get interpreter host position";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = "interpreter host not available";
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    if ((argumentIndex != Index.Invalid) &&
                                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                                    {
                                                        result = OptionDictionary.BadOption(options, arguments[argumentIndex]);
                                                    }
                                                    else
                                                    {
                                                        result = "wrong # args: should be \"host writebox ?options? string\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"host writebox ?options? string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"host option ?arg ...?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
