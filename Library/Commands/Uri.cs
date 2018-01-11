/*
 * Uri.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NETWORK
using System.Collections.Specialized;
using System.Net.NetworkInformation;
#endif

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

namespace Eagle._Commands
{
    [ObjectId("ca27d807-1636-4d17-bbf2-ebbe91aed44f")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard)]
    [ObjectGroup("network")]
    internal sealed class _Uri : Core
    {
        public _Uri(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] {
            "compare", "create", "download", "escape", "host",
            "isvalid", "join", "parse", "ping", "scheme",
            "softwareupdates", "unescape", "upload"
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
            ReturnCode code;

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
                                case "compare":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(typeof(UriKind), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-kind", null),
                                                new Option(typeof(UriComponents), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-components",
                                                    new Variant(UriComponents.AbsoluteUri)),
                                                new Option(typeof(UriFormat), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid, "-format", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) && ((argumentIndex + 2) == arguments.Count))
                                                {
                                                    Variant value = null;
                                                    UriKind uriKind = UriKind.Absolute; // FIXME: Good default?

                                                    if (options.IsPresent("-kind", ref value))
                                                        uriKind = (UriKind)value.Value;

                                                    UriComponents uriComponents = UriComponents.AbsoluteUri; // FIXME: Good default?

                                                    if (options.IsPresent("-components", ref value))
                                                        uriComponents = (UriComponents)value.Value;

                                                    UriFormat uriFormat = UriFormat.UriEscaped; // FIXME: Good default?

                                                    if (options.IsPresent("-format", ref value))
                                                        uriFormat = (UriFormat)value.Value;

                                                    bool noCase = false;

                                                    if (options.IsPresent("-nocase"))
                                                        noCase = true;

                                                    Uri uri1 = null;

                                                    code = Value.GetUri(arguments[argumentIndex], uriKind, interpreter.CultureInfo, ref uri1, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        Uri uri2 = null;

                                                        code = Value.GetUri(arguments[argumentIndex + 1], uriKind, interpreter.CultureInfo, ref uri2, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            result = Uri.Compare(uri1, uri2, uriComponents, uriFormat,
                                                                noCase ? StringOps.UserNoCaseStringComparisonType : StringOps.UserStringComparisonType);
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
                                                        result = "wrong # args: should be \"uri compare ?options? uri1 uri2\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri compare ?options? uri1 uri2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "create":
                                    {
                                        if (arguments.Count >= 4)
                                        {
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-username", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-password", null),
                                                new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-port", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-path", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-query", null),
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-fragment", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            if (arguments.Count > 4)
                                                code = interpreter.GetOptions(options, arguments, 0, 4, Index.Invalid, true, ref argumentIndex, ref result);
                                            else
                                                code = ReturnCode.Ok;

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (argumentIndex == Index.Invalid)
                                                {
                                                    Variant value = null;
                                                    string userName = null;

                                                    if (options.IsPresent("-username", ref value))
                                                        userName = value.ToString();

                                                    string password = null;

                                                    if (options.IsPresent("-password", ref value))
                                                        password = value.ToString();

                                                    int port = Port.Invalid;

                                                    if (options.IsPresent("-port", ref value))
                                                        port = (int)value.Value;

                                                    string path = null;

                                                    if (options.IsPresent("-path", ref value))
                                                        path = value.ToString();

                                                    string query = null;

                                                    if (options.IsPresent("-query", ref value))
                                                        query = value.ToString();

                                                    string fragment = null;

                                                    if (options.IsPresent("-fragment", ref value))
                                                        fragment = value.ToString();

                                                    try
                                                    {
                                                        UriBuilder uriBuilder =
                                                            new UriBuilder(arguments[2], arguments[3]);

                                                        if (userName != null)
                                                            uriBuilder.UserName = userName;

                                                        if (password != null)
                                                            uriBuilder.Password = password;

                                                        if (port != Port.Invalid)
                                                            uriBuilder.Port = port;

                                                        if (path != null)
                                                            uriBuilder.Path = path;

                                                        if (query != null)
                                                            uriBuilder.Query = query;

                                                        if (fragment != null)
                                                            uriBuilder.Fragment = fragment;

                                                        result = uriBuilder.ToString();
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Engine.SetExceptionErrorCode(interpreter, e);

                                                        result = e;
                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = "wrong # args: should be \"uri create scheme host ?options?\"";
                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri create scheme host ?options?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "download":
                                    {
                                        if (arguments.Count >= 3)
                                        {
#if NETWORK
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-callback", null),
                                                new Option(typeof(CallbackFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid,
                                                    "-callbackflags", new Variant(CallbackFlags.Default)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-inline", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-trusted", null),
                                                new Option(null, OptionFlags.MustHaveEncodingValue, Index.Invalid, Index.Invalid, "-encoding", null),
                                                new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-webclientdata", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) <= arguments.Count) &&
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    Variant value = null;
                                                    IClientData localClientData = clientData;

                                                    if (options.IsPresent("-webclientdata", ref value))
                                                    {
                                                        IObject @object = (IObject)value.Value;

                                                        if (@object != null)
                                                        {
                                                            localClientData = _Public.ClientData.WrapOrReplace(
                                                                localClientData, @object.Value);
                                                        }
                                                        else
                                                        {
                                                            result = "option value has invalid data";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        StringList callbackArguments = null;

                                                        if (options.IsPresent("-callback", ref value))
                                                            callbackArguments = (StringList)value.Value;

                                                        CallbackFlags callbackFlags = CallbackFlags.Default;

                                                        if (options.IsPresent("-callbackflags", ref value))
                                                            callbackFlags = (CallbackFlags)value.Value;

                                                        bool inline = false;

                                                        if (options.IsPresent("-inline"))
                                                            inline = true;

                                                        bool trusted = false;

                                                        if (options.IsPresent("-trusted"))
                                                            trusted = true;

                                                        Encoding encoding = null;

                                                        if (options.IsPresent("-encoding", ref value))
                                                            encoding = (Encoding)value.Value;

                                                        Uri uri = null;

                                                        code = Value.GetUri(arguments[argumentIndex], UriKind.Absolute,
                                                            interpreter.CultureInfo, ref uri, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            string argument = null;

                                                            if ((argumentIndex + 2) == arguments.Count)
                                                                argument = PathOps.GetNativePath(arguments[argumentIndex + 1]);

                                                            if (inline)
                                                            {
                                                                //
                                                                // NOTE: Do nothing.
                                                                //
                                                            }
#if !MONO
                                                            else if (!CommonOps.Runtime.IsMono())
                                                            {
                                                                FilePermission permissions = FilePermission.Write |
                                                                    FilePermission.NotExists | FilePermission.File;

                                                                code = FileOps.VerifyPath(argument, permissions, ref result);
                                                            }
#endif
                                                            else if (String.IsNullOrEmpty(argument))
                                                            {
                                                                result = "invalid path";
                                                                code = ReturnCode.Error;
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (inline)
                                                                {
                                                                    //
                                                                    // NOTE: Is this an asynchronous request?
                                                                    //
                                                                    if (callbackArguments != null)
                                                                    {
                                                                        //
                                                                        // NOTE: The "-trusted" option is not supported for
                                                                        //       asynchronous downloads.  Instead, use the
                                                                        //       [uri softwareupdates] sub-command before
                                                                        //       and after (i.e. to allow for proper saving
                                                                        //       and restoring of the current trust setting).
                                                                        //
                                                                        if (!trusted)
                                                                        {
                                                                            code = WebOps.DownloadDataAsync(
                                                                                interpreter, localClientData, callbackArguments,
                                                                                callbackFlags, uri, ref result);
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "-trusted cannot be used with -callback option";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        byte[] responseBytes = null;

                                                                        code = WebOps.DownloadData(
                                                                            interpreter, localClientData, uri, trusted,
                                                                            ref responseBytes, ref result);

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            string stringValue = null;

                                                                            code = StringOps.GetString(
                                                                                encoding, responseBytes, EncodingType.Default,
                                                                                ref stringValue, ref result);

                                                                            if (code == ReturnCode.Ok)
                                                                                result = stringValue;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    //
                                                                    // NOTE: Is this an asynchronous request?
                                                                    //
                                                                    if (callbackArguments != null)
                                                                    {
                                                                        //
                                                                        // NOTE: The "-trusted" option is not supported for
                                                                        //       asynchronous downloads.  Instead, use the
                                                                        //       [uri softwareupdates] sub-command before
                                                                        //       and after (i.e. to allow for proper saving
                                                                        //       and restoring of the current trust setting).
                                                                        //
                                                                        if (!trusted)
                                                                        {
                                                                            code = WebOps.DownloadFileAsync(
                                                                                interpreter, localClientData, callbackArguments,
                                                                                callbackFlags, uri, argument,
                                                                                ref result);
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "-trusted cannot be used with -callback option";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        code = WebOps.DownloadFile(
                                                                            interpreter, localClientData, uri, argument, trusted,
                                                                            ref result);
                                                                    }
                                                                }
                                                            }
                                                        }
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
                                                        result = "wrong # args: should be \"uri download ?options? uri ?argument?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri download ?options? uri ?argument?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "escape":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            object enumValue = EnumOps.TryParseEnum(
                                                typeof(UriEscapeType), arguments[2],
                                                true, true);

                                            if (enumValue is UriEscapeType)
                                            {
                                                UriEscapeType type = (UriEscapeType)enumValue;

                                                if (type == UriEscapeType.None)
                                                {
                                                    //
                                                    // NOTE: Ok, do nothing.
                                                    //
                                                    result = arguments[3];
                                                    code = ReturnCode.Ok;
                                                }
                                                else if (type == UriEscapeType.Uri)
                                                {
                                                    //
                                                    // NOTE: Escape an entire URI.
                                                    //
                                                    result = Uri.EscapeUriString(arguments[3]);
                                                    code = ReturnCode.Ok;
                                                }
                                                else if (type == UriEscapeType.Data)
                                                {
                                                    //
                                                    // NOTE: Escape data for use inside a URI.
                                                    //
                                                    result = Uri.EscapeDataString(arguments[3]);
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    result = ScriptOps.BadValue(
                                                        null, "uri escape type", arguments[2],
                                                        Enum.GetNames(typeof(UriEscapeType)), null, null);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = ScriptOps.BadValue(
                                                    null, "uri escape type", arguments[2],
                                                    Enum.GetNames(typeof(UriEscapeType)), null, null);

                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri escape type string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "host":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = Uri.CheckHostName(arguments[2]);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri host uri\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isvalid":
                                    {
                                        if ((arguments.Count == 3) || (arguments.Count == 4))
                                        {
                                            UriKind uriKind = UriKind.Absolute; // FIXME: Good default?

                                            if (arguments.Count == 4)
                                            {
                                                object enumValue = EnumOps.TryParseEnum(
                                                    typeof(UriKind), arguments[3],
                                                    true, true);

                                                if (enumValue is UriKind)
                                                {
                                                    uriKind = (UriKind)enumValue;

                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    result = ScriptOps.BadValue(
                                                        null, "bad uri kind", arguments[3],
                                                        Enum.GetNames(typeof(UriKind)), null, null);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                code = ReturnCode.Ok;
                                            }

                                            //
                                            // NOTE: Only continue if the supplied UriKind was valid.
                                            //
                                            if (code == ReturnCode.Ok)
                                            {
#if MONO_LEGACY
                                                try
                                                {
#endif
                                                    result = Uri.IsWellFormedUriString(arguments[2], uriKind);
#if MONO_LEGACY
                                                }
                                                catch
                                                {
                                                    result = false;
                                                }
#endif
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri isvalid uri ?kind?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "join":
                                    {
                                        if (arguments.Count >= 3)
                                        {
                                            result = PathOps.CombinePath(
                                                true, arguments.GetRange(2, arguments.Count - 2));
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri join name ?name ...?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "parse":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            try
                                            {
                                                UriBuilder uriBuilder =
                                                    new UriBuilder(arguments[2]);

                                                result = StringList.MakeList(
                                                    "-scheme", uriBuilder.Scheme,
                                                    "-host", uriBuilder.Host,
                                                    "-port", uriBuilder.Port,
                                                    "-username", uriBuilder.UserName,
                                                    "-password", uriBuilder.Password,
                                                    "-path", uriBuilder.Path,
                                                    "-query", uriBuilder.Query,
                                                    "-fragment", uriBuilder.Fragment);

                                                code = ReturnCode.Ok;
                                            }
                                            catch (Exception e)
                                            {
                                                Engine.SetExceptionErrorCode(interpreter, e);

                                                result = e;
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri parse uri\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "ping":
                                    {
                                        if (arguments.Count == 4)
                                        {
#if NETWORK
                                            Uri uri = null;

                                            /* IGNORED */
                                            Value.GetUri(arguments[2], UriKind.Absolute,
                                                interpreter.CultureInfo, ref uri, ref result);

                                            string hostNameOrAddress = null;

                                            try
                                            {
                                                if (uri != null)
                                                    hostNameOrAddress = uri.DnsSafeHost; /* throw */
                                            }
                                            catch
                                            {
                                                // do nothing.
                                            }

                                            if (hostNameOrAddress == null)
                                                hostNameOrAddress = arguments[2]; // fallback on original argument.

                                            int timeout = _Timeout.None;

                                            code = Value.GetInteger2(
                                                (IGetValue)arguments[3], ValueFlags.AnyInteger,
                                                interpreter.CultureInfo, ref timeout, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                IPStatus status = IPStatus.Unknown;
                                                long roundtripTime = 0;

                                                code = SocketOps.Ping(hostNameOrAddress, timeout,
                                                    ref status, ref roundtripTime, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = StringList.MakeList(
                                                        status, roundtripTime, "milliseconds");
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri ping uri timeout\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "scheme":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = Uri.CheckSchemeName(arguments[2]);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri scheme name\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "softwareupdates":
                                    {
                                        if ((arguments.Count >= 2) && (arguments.Count <= 4))
                                        {
#if NETWORK
                                            if (arguments.Count == 2)
                                            {
                                                code = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                bool trusted = false;

                                                if (code == ReturnCode.Ok)
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[2], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref trusted,
                                                        ref result);
                                                }

                                                bool exclusive = false;

                                                if ((code == ReturnCode.Ok) && (arguments.Count >= 4))
                                                {
                                                    code = Value.GetBoolean2(
                                                        arguments[3], ValueFlags.AnyBoolean,
                                                        interpreter.CultureInfo, ref exclusive,
                                                        ref result);
                                                }

                                                if (code == ReturnCode.Ok)
                                                {
                                                    bool wasTrusted = UpdateOps.IsTrusted();
                                                    bool wasExclusive = UpdateOps.IsExclusive();

                                                    if ((trusted != wasTrusted) ||
                                                        (exclusive != wasExclusive))
                                                    {
                                                        if ((code == ReturnCode.Ok) &&
                                                            (trusted != wasTrusted))
                                                        {
                                                            code = UpdateOps.SetTrusted(
                                                                trusted, ref result);
                                                        }

                                                        if ((code == ReturnCode.Ok) &&
                                                            (exclusive != wasExclusive))
                                                        {
                                                            code = UpdateOps.SetExclusive(
                                                                exclusive, ref result);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "software update certificate is already {0}{1}",
                                                            wasTrusted ? "trusted" : "untrusted",
                                                            wasExclusive ? " exclusively" : String.Empty);

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                            }

                                            if (code == ReturnCode.Ok)
                                            {
                                                result = String.Format(
                                                    "software update certificate is {0}{1}",
                                                    UpdateOps.IsTrusted() ? "trusted" : "untrusted",
                                                    UpdateOps.IsExclusive() ? " exclusively" : String.Empty);
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri softwareupdates ?trusted? ?exclusive?\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "unescape":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            result = Uri.UnescapeDataString(arguments[2]);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri unescape string\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "upload":
                                    {
                                        if (arguments.Count >= 3)
                                        {
#if NETWORK
                                            OptionDictionary options = new OptionDictionary(
                                                new IOption[] {
                                                new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-method", null),
                                                new Option(null, OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-data", null),
                                                new Option(null, OptionFlags.MustHaveListValue, Index.Invalid, Index.Invalid, "-callback", null),
                                                new Option(typeof(CallbackFlags), OptionFlags.MustHaveEnumValue, Index.Invalid, Index.Invalid,
                                                    "-callbackflags", new Variant(CallbackFlags.Default)),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-inline", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-raw", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-trusted", null),
                                                new Option(null, OptionFlags.MustHaveEncodingValue, Index.Invalid, Index.Invalid, "-encoding", null),
                                                new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-webclientdata", null),
                                                new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                                            });

                                            int argumentIndex = Index.Invalid;

                                            code = interpreter.GetOptions(options, arguments, 0, 2, Index.Invalid, false, ref argumentIndex, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if ((argumentIndex != Index.Invalid) &&
                                                    ((argumentIndex + 1) <= arguments.Count) &&
                                                    ((argumentIndex + 2) >= arguments.Count))
                                                {
                                                    Variant value = null;
                                                    IClientData localClientData = clientData;

                                                    if (options.IsPresent("-webclientdata", ref value))
                                                    {
                                                        IObject @object = (IObject)value.Value;

                                                        if (@object != null)
                                                        {
                                                            localClientData = _Public.ClientData.WrapOrReplace(
                                                                localClientData, @object.Value);
                                                        }
                                                        else
                                                        {
                                                            result = "option value has invalid data";
                                                            code = ReturnCode.Error;
                                                        }
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        string method = null;

                                                        if (options.IsPresent("-method", ref value))
                                                            method = value.ToString();

                                                        StringList listData = null;

                                                        if (options.IsPresent("-data", ref value))
                                                            listData = (StringList)value.Value;

                                                        StringList callbackArguments = null;

                                                        if (options.IsPresent("-callback", ref value))
                                                            callbackArguments = (StringList)value.Value;

                                                        CallbackFlags callbackFlags = CallbackFlags.Default;

                                                        if (options.IsPresent("-callbackflags", ref value))
                                                            callbackFlags = (CallbackFlags)value.Value;

                                                        bool inline = false;

                                                        if (options.IsPresent("-inline"))
                                                            inline = true;

                                                        bool raw = false;

                                                        if (options.IsPresent("-raw"))
                                                            raw = true;

                                                        bool trusted = false;

                                                        if (options.IsPresent("-trusted"))
                                                            trusted = true;

                                                        Encoding encoding = null;

                                                        if (options.IsPresent("-encoding", ref value))
                                                            encoding = (Encoding)value.Value;

                                                        Uri uri = null;

                                                        code = Value.GetUri(arguments[argumentIndex], UriKind.Absolute,
                                                            interpreter.CultureInfo, ref uri, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            string argument = null;

                                                            if ((argumentIndex + 2) == arguments.Count)
                                                                argument = PathOps.GetNativePath(arguments[argumentIndex + 1]);

                                                            if (inline)
                                                            {
                                                                //
                                                                // NOTE: Do nothing.
                                                                //
                                                            }
#if !MONO
                                                            else if (!CommonOps.Runtime.IsMono())
                                                            {
                                                                FilePermission permissions = FilePermission.Read |
                                                                    FilePermission.Exists | FilePermission.File;

                                                                code = FileOps.VerifyPath(argument, permissions, ref result);
                                                            }
#endif
                                                            else if (String.IsNullOrEmpty(argument))
                                                            {
                                                                result = "invalid path";
                                                                code = ReturnCode.Error;
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (inline)
                                                                {
                                                                    //
                                                                    // NOTE: Is this an asynchronous request?
                                                                    //
                                                                    if (callbackArguments != null)
                                                                    {
                                                                        //
                                                                        // NOTE: The "-trusted" option is not supported for
                                                                        //       asynchronous uploads.  Instead, use the
                                                                        //       [uri softwareupdates] sub-command before
                                                                        //       and after (i.e. to allow for proper saving
                                                                        //       and restoring of the current trust setting).
                                                                        //
                                                                        if (!trusted)
                                                                        {
                                                                            if (raw)
                                                                            {
                                                                                byte[] requestBytes = null;

                                                                                code = ArrayOps.GetBytesFromList(
                                                                                    interpreter, listData, encoding,
                                                                                    ref requestBytes, ref result);

                                                                                if (code == ReturnCode.Ok)
                                                                                {
                                                                                    code = WebOps.UploadDataAsync(
                                                                                        interpreter, localClientData, callbackArguments,
                                                                                        callbackFlags, uri, method, requestBytes,
                                                                                        ref result);

                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                code = WebOps.UploadValuesAsync(
                                                                                    interpreter, localClientData, callbackArguments,
                                                                                    callbackFlags, uri, method,
                                                                                    ListOps.ToNameValueCollection(
                                                                                        listData, new NameValueCollection()),
                                                                                    ref result);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "-trusted cannot be used with -callback option";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        byte[] responseBytes = null;

                                                                        if (raw)
                                                                        {
                                                                            byte[] requestBytes = null;

                                                                            code = ArrayOps.GetBytesFromList(
                                                                                interpreter, listData, encoding,
                                                                                ref requestBytes, ref result);

                                                                            if (code == ReturnCode.Ok)
                                                                            {
                                                                                code = WebOps.UploadData(
                                                                                    interpreter, localClientData, uri, method,
                                                                                    requestBytes, trusted, ref responseBytes,
                                                                                    ref result);
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            code = WebOps.UploadValues(
                                                                                interpreter, localClientData, uri, method,
                                                                                ListOps.ToNameValueCollection(
                                                                                    listData, new NameValueCollection()),
                                                                                trusted, ref responseBytes, ref result);
                                                                        }

                                                                        if (code == ReturnCode.Ok)
                                                                        {
                                                                            string stringValue = null;

                                                                            code = StringOps.GetString(
                                                                                encoding, responseBytes, EncodingType.Default,
                                                                                ref stringValue, ref result);

                                                                            if (code == ReturnCode.Ok)
                                                                                result = stringValue;
                                                                        }
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    //
                                                                    // NOTE: Is this an asynchronous request?
                                                                    //
                                                                    if (callbackArguments != null)
                                                                    {
                                                                        //
                                                                        // NOTE: The "-trusted" option is not supported for
                                                                        //       asynchronous uploads.  Instead, use the
                                                                        //       [uri softwareupdates] sub-command before
                                                                        //       and after (i.e. to allow for proper saving
                                                                        //       and restoring of the current trust setting).
                                                                        //
                                                                        if (!trusted)
                                                                        {
                                                                            code = WebOps.UploadFileAsync(
                                                                                interpreter, localClientData, callbackArguments,
                                                                                callbackFlags, uri, method,
                                                                                argument, ref result);
                                                                        }
                                                                        else
                                                                        {
                                                                            result = "-trusted cannot be used with -callback option";
                                                                            code = ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        code = WebOps.UploadFile(
                                                                            interpreter, localClientData, uri, method, argument,
                                                                            trusted, ref result);
                                                                    }
                                                                }
                                                            }
                                                        }
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
                                                        result = "wrong # args: should be \"uri upload ?options? uri ?argument?\"";
                                                    }

                                                    code = ReturnCode.Error;
                                                }
                                            }
#else
                                            result = "not implemented";
                                            code = ReturnCode.Error;
#endif
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"uri upload ?options? uri ?argument?\"";
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
                        result = "wrong # args: should be \"uri option ?arg ...?\"";
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
