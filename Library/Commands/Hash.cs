/*
 * Hash.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("66a2a9aa-1024-4199-b6d9-097c2662acd7")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("string")]
    internal sealed class _Hash : Core
    {
        #region Private Static Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static readonly StringList normalHashAlgorithmNames =
            new StringList(
            new string[] { "MD5", "RIPEMD160", "SHA1", "SHA256", "SHA384",
                "SHA512"
        });

        ///////////////////////////////////////////////////////////////////////

        private static readonly StringList keyedHashAlgorithmNames =
            new StringList(
            new string[] { "MACTripleDES"
        });

        ///////////////////////////////////////////////////////////////////////

        private static readonly StringList macHashAlgorithmNames =
            new StringList(
            new string[] { "HMACMD5", "HMACRIPEMD160", "HMACSHA1",
                "HMACSHA256", "HMACSHA384", "HMACSHA512"
        });

        ///////////////////////////////////////////////////////////////////////

        private static MemberInfo memberInfo = null;

        ///////////////////////////////////////////////////////////////////////

        private static StringList defaultAlgorithms = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static bool IsHashAlgorithm(
            string typeName,
            ref string subTypeName
            )
        {
            if (String.IsNullOrEmpty(typeName))
                return false;

            return IsHashAlgorithm(Type.GetType(typeName), ref subTypeName);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsHashAlgorithm(
            Type type,
            ref string subTypeName
            )
        {
            if (type == null)
                return false;

            if (MarshalOps.IsAssignableFrom(typeof(HMAC), type))
            {
                subTypeName = "mac";
                return true;
            }

            if (MarshalOps.IsAssignableFrom(typeof(KeyedHashAlgorithm), type))
            {
                subTypeName = "keyed";
                return true;
            }

            if (MarshalOps.IsAssignableFrom(typeof(HashAlgorithm), type))
            {
                subTypeName = "normal";
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static MemberInfo GetAlgorithmsMemberInfo()
        {
            lock (syncRoot)
            {
                if (memberInfo != null)
                    return memberInfo;

                if (CommonOps.Runtime.IsMono())
                {
                    memberInfo = typeof(CryptoConfig).GetField(
                        "algorithms",
                        MarshalOps.PrivateStaticGetFieldBindingFlags);
                }
                else
                {
                    memberInfo = typeof(CryptoConfig).GetProperty(
                        "DefaultNameHT",
                        MarshalOps.PrivateStaticGetPropertyBindingFlags);
                }

                return memberInfo;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static StringList GetAlgorithms()
        {
            lock (syncRoot)
            {
                MemberInfo memberInfo = GetAlgorithmsMemberInfo();

                if (memberInfo == null)
                    return null;

                if (CommonOps.Runtime.IsMono())
                {
                    object value = ((FieldInfo)memberInfo).GetValue(null);

                    if (value is IDictionary<string, Type>) /* v3.x */
                    {
                        StringList list = new StringList();

                        foreach (KeyValuePair<string, Type> pair in
                                ((IDictionary<string, Type>)value))
                        {
                            if ((pair.Key == null) || (pair.Value == null))
                                continue;

                            string subTypeName = null;

                            if (!IsHashAlgorithm(pair.Value, ref subTypeName))
                                continue;

                            list.Add(StringList.MakeList(
                                subTypeName, pair.Key));
                        }

                        return list;
                    }
                    else if (value is Hashtable) /* v2.x */
                    {
                        StringList list = new StringList();

                        foreach (DictionaryEntry entry in ((Hashtable)value))
                        {
                            if (entry.Key == null)
                                continue;

                            string subTypeName = null;

                            if (!IsHashAlgorithm(
                                    entry.Value as string, ref subTypeName))
                            {
                                continue;
                            }

                            list.Add(StringList.MakeList(
                                subTypeName, entry.Key.ToString()));
                        }

                        return list;
                    }
                }
                else
                {
                    object value = ((PropertyInfo)memberInfo).GetValue(
                        null, null);

                    if (value is IDictionary<string, object>) /* v4.x */
                    {
                        StringList list = new StringList();

                        foreach (KeyValuePair<string, object> pair in
                                ((IDictionary<string, object>)value))
                        {
                            if (pair.Key == null)
                                continue;

                            string subTypeName = null;

                            if (!IsHashAlgorithm(
                                    pair.Value as Type, ref subTypeName))
                            {
                                continue;
                            }

                            list.Add(StringList.MakeList(
                                subTypeName, pair.Key));
                        }

                        return list;
                    }
                    else if (value is Hashtable) /* v2.x */
                    {
                        StringList list = new StringList();

                        foreach (DictionaryEntry entry in ((Hashtable)value))
                        {
                            if (entry.Key == null)
                                continue;

                            string subTypeName = null;

                            if (!IsHashAlgorithm(
                                    entry.Value as Type, ref subTypeName))
                            {
                                continue;
                            }

                            list.Add(StringList.MakeList(
                                subTypeName, entry.Key.ToString()));
                        }

                        return list;
                    }
                }

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public _Hash(
            ICommandData commandData
            )
            : base(commandData)
        {
            lock (syncRoot)
            {
                if (defaultAlgorithms == null)
                {
                    try
                    {
                        defaultAlgorithms = GetAlgorithms();
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(_Hash).Name,
                            TracePriority.InternalError);
                    }

                    //
                    // HACK: Prevent this block from being entered again for
                    //       this application domain.
                    //
                    if (defaultAlgorithms == null)
                        defaultAlgorithms = new StringList();
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands =
            new EnsembleDictionary(new string[] {
            "keyed", "list", "mac", "normal"
        });

        ///////////////////////////////////////////////////////////////////////

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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
                result = "wrong # args: should be \"hash option ?arg ...?\"";
                return ReturnCode.Error;
            }

            ReturnCode code;
            string subCommand = arguments[1];
            bool tried = false;

            code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, this, clientData, arguments, true,
                false, ref subCommand, ref tried, ref result);

            if ((code != ReturnCode.Ok) || tried)
                return code;

            //
            // NOTE: These algorithms are known to be supported by the
            //       framework.
            //
            //       Normal: MD5, RIPEMD160, SHA, SHA1, SHA256, SHA384, SHA512
            //
            //        Keyed: MACTripleDES
            //
            //         HMAC: HMACMD5, HMACRIPEMD160, HMACSHA1, HMACSHA256,
            //               HMACSHA384, HMACSHA512
            //
            switch (subCommand)
            {
                case "keyed":
                    {
                        if (arguments.Count >= 4)
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] {
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-raw",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-filename",
                                    null), /* COMPAT: Tcllib. */
                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                    Index.Invalid, Index.Invalid, "-encoding",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid,
                                    Option.EndOfOptions, null)
                            });

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(
                                options, arguments, 0, 2, Index.Invalid, false,
                                ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    ((argumentIndex + 2) <= arguments.Count) &&
                                    ((argumentIndex + 3) >= arguments.Count))
                                {
                                    Variant value = null;
                                    bool raw = false;

                                    if (options.IsPresent("-raw"))
                                        raw = true;

                                    bool isFileName = false;

                                    if (options.IsPresent("-filename", ref value))
                                        isFileName = true;

                                    Encoding encoding = null;

                                    if (options.IsPresent("-encoding", ref value))
                                        encoding = (Encoding)value.Value;

                                    if (code == ReturnCode.Ok)
                                    {
                                        try
                                        {
                                            using (KeyedHashAlgorithm algorithm =
                                                KeyedHashAlgorithm.Create(
                                                    arguments[argumentIndex]))
                                            {
                                                if (algorithm != null)
                                                {
                                                    algorithm.Initialize();

                                                    if ((argumentIndex + 3) == arguments.Count)
                                                    {
                                                        byte[] bytes = null;

                                                        code = StringOps.GetBytes(
                                                            encoding, arguments[argumentIndex + 2],
                                                            EncodingType.Binary, ref bytes, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            algorithm.Key = bytes;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (isFileName)
                                                        {
                                                            Stream stream = null;

                                                            try
                                                            {
                                                                code = RuntimeOps.NewStream(
                                                                    interpreter,
                                                                    arguments[argumentIndex + 1],
                                                                    FileMode.Open, FileAccess.Read,
                                                                    ref stream, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (raw)
                                                                    {
                                                                        result = new ByteList(
                                                                            algorithm.ComputeHash(stream));
                                                                    }
                                                                    else
                                                                    {
                                                                        result = FormatOps.Hash(
                                                                            algorithm.ComputeHash(stream));
                                                                    }
                                                                }
                                                            }
                                                            finally
                                                            {
                                                                if (stream != null)
                                                                {
                                                                    stream.Close();
                                                                    stream = null;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            byte[] bytes = null;

                                                            code = StringOps.GetBytes(
                                                                encoding, arguments[argumentIndex + 1],
                                                                EncodingType.Binary, ref bytes, ref result);

                                                            if (raw)
                                                            {
                                                                result = new ByteList(
                                                                    algorithm.ComputeHash(bytes));
                                                            }
                                                            else
                                                            {
                                                                result = FormatOps.Hash(
                                                                    algorithm.ComputeHash(bytes));
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "unsupported keyed hash algorithm \"{0}\"",
                                                        arguments[argumentIndex]);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Engine.SetExceptionErrorCode(interpreter, e);

                                            result = e;
                                            code = ReturnCode.Error;
                                        }
                                    }
                                }
                                else
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                    {
                                        result = OptionDictionary.BadOption(
                                            options, arguments[argumentIndex]);
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                            this.Name, subCommand);
                                    }

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                this.Name, subCommand);

                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "list":
                    {
                        if ((arguments.Count == 2) || (arguments.Count == 3))
                        {
                            string type = null;

                            if (arguments.Count == 3)
                                type = arguments[2];

                            switch (type)
                            {
                                case null:
                                case "all":
                                    {
                                        StringList list = new StringList();

                                        lock (syncRoot)
                                        {
                                            if (defaultAlgorithms != null)
                                                list.AddRange(defaultAlgorithms);
                                        }

                                        if (keyedHashAlgorithmNames != null)
                                            foreach (string hashAlgorithmName in keyedHashAlgorithmNames)
                                                list.Add(StringList.MakeList("keyed", hashAlgorithmName));

                                        if (macHashAlgorithmNames != null)
                                            foreach (string hashAlgorithmName in macHashAlgorithmNames)
                                                list.Add(StringList.MakeList("mac", hashAlgorithmName));

                                        if (normalHashAlgorithmNames != null)
                                            foreach (string hashAlgorithmName in normalHashAlgorithmNames)
                                                list.Add(StringList.MakeList("normal", hashAlgorithmName));

                                        result = list;
                                        break;
                                    }
                                case "default":
                                    {
                                        lock (syncRoot)
                                        {
                                            result = (defaultAlgorithms != null) ?
                                                new StringList(defaultAlgorithms) : null;
                                        }
                                        break;
                                    }
                                case "keyed":
                                    {
                                        result = (keyedHashAlgorithmNames != null) ?
                                            new StringList(keyedHashAlgorithmNames) : null;

                                        break;
                                    }
                                case "mac":
                                    {
                                        result = (macHashAlgorithmNames != null) ?
                                            new StringList(macHashAlgorithmNames) : null;

                                        break;
                                    }
                                case "normal":
                                    {
                                        result = (normalHashAlgorithmNames != null) ?
                                            new StringList(normalHashAlgorithmNames) : null;

                                        break;
                                    }
                                default:
                                    {
                                        result = "unknown algorithm list, must be: all, default, keyed, mac, or normal";
                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?type?\"",
                                this.Name, subCommand);

                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "mac":
                    {
                        if (arguments.Count >= 4)
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] {
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-raw",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-filename",
                                    null), /* COMPAT: Tcllib. */
                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                    Index.Invalid, Index.Invalid, "-encoding",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid,
                                    Option.EndOfOptions, null)
                            });

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(
                                options, arguments, 0, 2, Index.Invalid, false,
                                ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    ((argumentIndex + 2) <= arguments.Count) &&
                                    ((argumentIndex + 3) >= arguments.Count))
                                {
                                    Variant value = null;
                                    bool raw = false;

                                    if (options.IsPresent("-raw"))
                                        raw = true;

                                    bool isFileName = false;

                                    if (options.IsPresent("-filename", ref value))
                                        isFileName = true;

                                    Encoding encoding = null;

                                    if (options.IsPresent("-encoding", ref value))
                                        encoding = (Encoding)value.Value;

                                    if (code == ReturnCode.Ok)
                                    {
                                        try
                                        {
                                            using (HMAC algorithm = HMAC.Create(
                                                    arguments[argumentIndex]))
                                            {
                                                if (algorithm != null)
                                                {
                                                    algorithm.Initialize();

                                                    if ((argumentIndex + 3) == arguments.Count)
                                                    {
                                                        byte[] bytes = null;

                                                        code = StringOps.GetBytes(
                                                            encoding, arguments[argumentIndex + 2],
                                                            EncodingType.Binary, ref bytes, ref result);

                                                        if (code == ReturnCode.Ok)
                                                            algorithm.Key = bytes;
                                                    }

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        if (isFileName)
                                                        {
                                                            Stream stream = null;

                                                            try
                                                            {
                                                                code = RuntimeOps.NewStream(
                                                                    interpreter,
                                                                    arguments[argumentIndex + 1],
                                                                    FileMode.Open, FileAccess.Read,
                                                                    ref stream, ref result);

                                                                if (code == ReturnCode.Ok)
                                                                {
                                                                    if (raw)
                                                                    {
                                                                        result = new ByteList(
                                                                            algorithm.ComputeHash(stream));
                                                                    }
                                                                    else
                                                                    {
                                                                        result = FormatOps.Hash(
                                                                            algorithm.ComputeHash(stream));
                                                                    }
                                                                }
                                                            }
                                                            finally
                                                            {
                                                                if (stream != null)
                                                                {
                                                                    stream.Close();
                                                                    stream = null;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            byte[] bytes = null;

                                                            code = StringOps.GetBytes(
                                                                encoding, arguments[argumentIndex + 1],
                                                                EncodingType.Binary, ref bytes, ref result);

                                                            if (raw)
                                                            {
                                                                result = new ByteList(
                                                                    algorithm.ComputeHash(bytes));
                                                            }
                                                            else
                                                            {
                                                                result = FormatOps.Hash(
                                                                    algorithm.ComputeHash(bytes));
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "unsupported hmac algorithm \"{0}\"",
                                                        arguments[argumentIndex]);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Engine.SetExceptionErrorCode(interpreter, e);

                                            result = e;
                                            code = ReturnCode.Error;
                                        }
                                    }
                                }
                                else
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                    {
                                        result = OptionDictionary.BadOption(
                                            options, arguments[argumentIndex]);
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                            this.Name, subCommand);
                                    }

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?options? algorithm string ?key?\"",
                                this.Name, subCommand);

                            code = ReturnCode.Error;
                        }
                        break;
                    }
                case "normal":
                    {
                        if (arguments.Count >= 4)
                        {
                            OptionDictionary options = new OptionDictionary(
                                new IOption[] {
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-raw",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid, "-filename",
                                    null), /* COMPAT: Tcllib. */
                                new Option(null, OptionFlags.MustHaveEncodingValue,
                                    Index.Invalid, Index.Invalid, "-encoding",
                                    null),
                                new Option(null, OptionFlags.None,
                                    Index.Invalid, Index.Invalid,
                                    Option.EndOfOptions, null)
                            });

                            int argumentIndex = Index.Invalid;

                            code = interpreter.GetOptions(
                                options, arguments, 0, 2, Index.Invalid, false,
                                ref argumentIndex, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex != Index.Invalid) &&
                                    ((argumentIndex + 2) == arguments.Count))
                                {
                                    Variant value = null;
                                    bool raw = false;

                                    if (options.IsPresent("-raw"))
                                        raw = true;

                                    bool isFileName = false;

                                    if (options.IsPresent("-filename", ref value))
                                        isFileName = true;

                                    Encoding encoding = null;

                                    if (options.IsPresent("-encoding", ref value))
                                        encoding = (Encoding)value.Value;

                                    if (code == ReturnCode.Ok)
                                    {
                                        try
                                        {
                                            using (HashAlgorithm algorithm =
                                                HashAlgorithm.Create(
                                                    arguments[argumentIndex]))
                                            {
                                                if (algorithm != null)
                                                {
                                                    algorithm.Initialize();

                                                    if (isFileName)
                                                    {
                                                        Stream stream = null;

                                                        try
                                                        {
                                                            code = RuntimeOps.NewStream(
                                                                interpreter,
                                                                arguments[argumentIndex + 1],
                                                                FileMode.Open, FileAccess.Read,
                                                                ref stream, ref result);

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                if (raw)
                                                                {
                                                                    result = new ByteList(
                                                                        algorithm.ComputeHash(stream));
                                                                }
                                                                else
                                                                {
                                                                    result = FormatOps.Hash(
                                                                        algorithm.ComputeHash(stream));
                                                                }
                                                            }
                                                        }
                                                        finally
                                                        {
                                                            if (stream != null)
                                                            {
                                                                stream.Close();
                                                                stream = null;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        byte[] bytes = null;

                                                        code = StringOps.GetBytes(
                                                            encoding, arguments[argumentIndex + 1],
                                                            EncodingType.Binary, ref bytes, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (raw)
                                                            {
                                                                result = new ByteList(
                                                                    algorithm.ComputeHash(bytes));
                                                            }
                                                            else
                                                            {
                                                                result = FormatOps.Hash(
                                                                    algorithm.ComputeHash(bytes));
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "unsupported hash algorithm \"{0}\"",
                                                        arguments[argumentIndex]);

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Engine.SetExceptionErrorCode(interpreter, e);

                                            result = e;
                                            code = ReturnCode.Error;
                                        }
                                    }
                                }
                                else
                                {
                                    if ((argumentIndex != Index.Invalid) &&
                                        Option.LooksLikeOption(arguments[argumentIndex]))
                                    {
                                        result = OptionDictionary.BadOption(
                                            options, arguments[argumentIndex]);
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "wrong # args: should be \"{0} {1} ?options? algorithm string\"",
                                            this.Name, subCommand);
                                    }

                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "wrong # args: should be \"{0} {1} ?options? algorithm string\"",
                                this.Name, subCommand);

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

            return code;
        }
        #endregion
    }
}
