/*
 * PolicyOps.cs --
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
using System.IO;
using System.Reflection;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("ab00e89a-8a1f-404b-91fd-32d10d0f44ba")]
    internal static class PolicyOps
    {
        #region Private (Internal) Data
        #region Default Sub-Command Policy Lists
        //
        // NOTE: This is the default list of [file] sub-commands that
        //       are ALLOWED to be used by scripts running in a "safe"
        //       interpreter.
        //
        internal static readonly StringDictionary DefaultAllowedFileSubCommandNames =
            new StringDictionary(new string[] {
            "channels", "dirname", "join", "split"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default list of [info] sub-commands that
        //       are ALLOWED to be used by scripts running in a "safe"
        //       interpreter.
        //
        internal static readonly StringDictionary DefaultAllowedInfoSubCommandNames =
            new StringDictionary(new string[] {
            "appdomain", "args", "body", "commands",
            "complete", "context", "default", "engine",
            "ensembles", "exists", "functions", "globals",
            "level", "library", "locals", "objects",
            "operands", "operators", "patchlevel", "procs",
            "script", "subcommands", "tclversion", "vars"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default list of [interp] sub-commands that
        //       are ALLOWED to be used by scripts running in a "safe"
        //       interpreter.
        //
        internal static readonly StringDictionary DefaultAllowedInterpSubCommandNames =
            new StringDictionary(new string[] {
            "alias", "aliases", "cancel", "exists",
            "issafe", "slaves"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default list of [object] sub-commands that
        //       are ALLOWED to be used by scripts running in a "safe"
        //       interpreter.
        //
        internal static readonly StringDictionary DefaultAllowedObjectSubCommandNames =
            new StringDictionary(new string[] {
            "dispose", "invoke", "invokeall", "isoftype"
        }, true, false);

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the default list of [package] sub-commands that
        //       are NOT allowed to be used by scripts running in a "safe"
        //       interpreter.
        //
        internal static readonly StringDictionary DefaultDisallowedPackageSubCommandNames =
            new StringDictionary(new string[] {
            "indexes", "reset", "scan", "vloaded"
        }, true, false);
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This list defines the default policies that are added to every
        //       interpreter.
        //
        internal static ExecuteCallbackDictionary DefaultCallbacks =
            new ExecuteCallbackDictionary(new ExecuteCallback[] {
            FilePolicyCallback, InfoPolicyCallback, InterpPolicyCallback,
            ObjectPolicyCallback, PackagePolicyCallback, SourcePolicyCallback
        });
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Helper Methods
        private static ReturnCode EvaluatePolicyScript(
            Interpreter interpreter,
            string text,
            ArgumentList arguments,
            PolicyFlags policyFlags,
            ref Result result
            ) /* THREAD-SAFE, RE-ENTRANT */
        {
            if (interpreter != null)
            {
                //
                // NOTE: Interpreter is valid, so far, so good.
                //
                ReturnCode code = ReturnCode.Ok;

                //
                // NOTE: Does the caller provide an argument list to
                //       append to the script prior to evaluating it?
                //
                bool appendArguments = FlagOps.HasFlags(
                    policyFlags, PolicyFlags.Arguments, true);

                //
                // NOTE: Does the caller want us to attempt to split
                //       the script into a list?
                //
                if (FlagOps.HasFlags(
                        policyFlags, PolicyFlags.SplitList, true))
                {
                    //
                    // NOTE: We are in "list" mode.  Attempt to parse
                    //       the script as a list and then append the
                    //       supplied argument list before converting
                    //       it back to a string.
                    //
                    StringList list = null;

                    code = Parser.SplitList(
                        interpreter, text, 0, Length.Invalid, false,
                        ref list, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        if (appendArguments && (arguments != null))
                            list.Add(arguments);

                        text = list.ToString();
                    }
                }
                else if (appendArguments && (arguments != null))
                {
                    //
                    // NOTE: Arguments were supplied; however, we are
                    //       not operating in "list" mode.  Append the
                    //       arguments as a single string to the script
                    //       (separated by a single intervening space).
                    //
                    StringBuilder builder = StringOps.NewStringBuilder(
                        text);

                    builder.Append(Characters.Space);
                    builder.Append(arguments);

                    text = builder.ToString();
                }

                //
                // NOTE: Did the list parsing code above succeed, if it
                //       was requested?
                //
                if (code == ReturnCode.Ok)
                {
                    code = interpreter.EvaluateScript(
                        text, ref result); /* EXEMPT */
                }

                return code;
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Sub-Command Support Methods
        private static EnsembleDictionary GetSubCommands(
            IEnsemble ensemble,
            bool allowed
            )
        {
            EnsembleDictionary subCommands = null;

            if (ensemble != null)
            {
                IPolicyEnsemble policyEnsemble = ensemble as IPolicyEnsemble;

                if (policyEnsemble != null)
                {
                    EnsembleDictionary disallowedSubCommands = policyEnsemble.DisallowedSubCommands;

                    if (allowed)
                    {
                        EnsembleDictionary possibleSubCommands = policyEnsemble.AllowedSubCommands;

                        if (possibleSubCommands == null)
                            possibleSubCommands = ensemble.SubCommands;

                        if (possibleSubCommands != null)
                        {
                            if (disallowedSubCommands != null)
                            {
                                subCommands = new EnsembleDictionary();

                                foreach (KeyValuePair<string, ISubCommand> pair in possibleSubCommands)
                                {
                                    string subCommandName = pair.Key;

                                    if (!disallowedSubCommands.ContainsKey(subCommandName))
                                        subCommands.Add(subCommandName, pair.Value);
                                }
                            }
                            else
                            {
                                subCommands = possibleSubCommands;
                            }
                        }
                    }
                    else
                    {
                        subCommands = disallowedSubCommands;
                    }
                }
                else
                {
                    subCommands = ensemble.SubCommands;
                }
            }

            return subCommands;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static StringDictionary GetSubCommandNames(
            IEnsemble ensemble,
            bool allowed
            )
        {
            EnsembleDictionary subCommands = GetSubCommands(ensemble, allowed);

            if (subCommands == null)
                return null;

            StringDictionary subCommandNames = subCommands.CachedNames;

            if ((subCommandNames == null) ||
                (subCommandNames.Count != subCommands.Count))
            {
                subCommands.CachedNames = subCommandNames = new StringDictionary();

                foreach (KeyValuePair<string, ISubCommand> pair in subCommands)
                    subCommandNames.Add(pair.Key, null);
            }

            return subCommandNames;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static EnsembleDictionary GetSubCommandsUnsafe(
            IEnsemble ensemble
            )
        {
            return (ensemble != null) ? ensemble.SubCommands : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static EnsembleDictionary GetSubCommandsSafe(
            Interpreter interpreter,
            IEnsemble ensemble
            )
        {
            if (interpreter == null)
                return null;

            lock (interpreter.SyncRoot) /* TRANSACTIONAL */
            {
                if (ensemble == null)
                    return null;

                if (!interpreter.IsSafe())
                    return ensemble.SubCommands;

                return GetSubCommands(ensemble, true);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used as a callback to filter an arbitrary list of
        //       matched sub-commands based on the ones allowed for an ensemble.
        //
        /* Eagle._Components.Private.Delegates.SubCommandFilterCallback */
        public static IEnumerable<KeyValuePair<string, ISubCommand>>
                OnlyAllowedSubCommands(
            Interpreter interpreter,
            IEnsemble ensemble,
            IEnumerable<KeyValuePair<string, ISubCommand>> subCommands,
            ref Result error
            )
        {
            if (subCommands == null)
            {
                error = "invalid sub-commands";
                return null;
            }

            EnsembleDictionary allowedSubCommands = GetSubCommandsSafe(
                interpreter, ensemble);

            if (allowedSubCommands == null)
            {
                error = "invalid allowed sub-commands";
                return null;
            }

            IList<KeyValuePair<string, ISubCommand>> filteredSubCommands =
                new List<KeyValuePair<string, ISubCommand>>();

            foreach (KeyValuePair<string, ISubCommand> pair in subCommands)
                if (allowedSubCommands.ContainsKey(pair.Key))
                    filteredSubCommands.Add(pair);

            return filteredSubCommands;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Policy Support Methods
        public static IPolicy NewCorePolicy(
            ExecuteCallback callback,
            IClientData clientData,
            IPlugin plugin,
            ref Result error
            )
        {
            if (callback != null)
            {
                MethodInfo methodInfo = callback.Method;

                if (methodInfo != null)
                {
                    Type type = methodInfo.DeclaringType;

                    if (type != null)
                    {
                        _Policies.Core policy = new _Policies.Core(new PolicyData(
                            FormatOps.PolicyDelegateName(callback), null, null,
                            clientData, type.FullName, methodInfo.Name,
                            RuntimeOps.DelegateBindingFlags,
                            AttributeOps.GetMethodFlags(methodInfo),
                            PolicyFlags.None, plugin, 0));

                        policy.Callback = callback;
                        return policy;
                    }
                    else
                    {
                        error = "invalid policy callback method type";
                    }
                }
                else
                {
                    error = "invalid policy callback method";
                }
            }
            else
            {
                error = "invalid policy callback";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsTrustedObject(
            Interpreter interpreter,
            string text,
            ObjectFlags flags,
            object @object,    /* NOT USED */
            ref Result error
            )
        {
            if (interpreter != null)
            {
                if (FlagOps.HasFlags(flags, ObjectFlags.Safe, true))
                    return true;

                error = String.Format(
                    "permission denied: safe interpreter cannot " +
                    "use object from \"{0}\"", text);

                return false;
            }
            else
            {
                error = "invalid interpreter";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsTrustedType(
            Interpreter interpreter,
            string text,
            Type type,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                if (type != null)
                {
                    string name = type.FullName;

                    if (name != null)
                    {
                        ObjectDictionary trustedTypes =
                            new ObjectDictionary();

                        AddTrustedTypes(interpreter, trustedTypes);

                        if ((trustedTypes != null) &&
                            trustedTypes.ContainsKey(name))
                        {
                            return true;
                        }

                        error = String.Format(
                            "permission denied: safe interpreter cannot " +
                            "use type from \"{0}\"", text);
                    }
                    else
                    {
                        error = "invalid type name";
                    }
                }
                else
                {
                    error = "invalid type";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSuccess(
            PolicyDecision decision
            )
        {
            //
            // NOTE: If the policy decision is "None" or "Approved", that is
            //       considered to be a success.
            //
            if (PolicyContext.IsNone(decision) ||
                PolicyContext.IsApproved(decision))
            {
                return true;
            }

            //
            // NOTE: Any other decision is considered to be a failure.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSuccess(
            ReturnCode code,
            PolicyDecision decision
            )
        {
            //
            // NOTE: Anytime a policy callback returns something other than
            //       "Ok", it is a failure.
            //
            if (code != ReturnCode.Ok)
                return false;

            //
            // NOTE: When the return code is "Ok", success is based on the
            //       formal policy decision itself.
            //
            return IsSuccess(decision);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddTrustedDirectories(
            Interpreter interpreter,
            PathDictionary<object> directories
            )
        {
            if ((interpreter != null) && (directories != null))
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    //
                    // BUGFIX: Cannot add null directory to the dictionary;
                    //         therefore, if the initialized path is invalid,
                    //         just skip it.
                    //
                    string initializedPath = interpreter.InternalInitializedPath;

                    if ((initializedPath != null) &&
                        !directories.ContainsKey(initializedPath))
                    {
                        directories.Add(initializedPath);
                    }

                    //
                    // NOTE: Add the paths trusted by the interpreter.
                    //
                    StringList trustedPaths = interpreter.TrustedPaths;

                    if (trustedPaths != null)
                    {
                        foreach (string trustedPath in trustedPaths)
                        {
                            if ((trustedPath != null) &&
                                !directories.ContainsKey(trustedPath))
                            {
                                directories.Add(trustedPath);
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddTrustedTypes(
            Interpreter interpreter,
            ObjectDictionary types
            )
        {
            if ((interpreter != null) && (types != null))
            {
                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Add the types trusted by the interpreter.
                    //
                    ObjectDictionary trustedTypes = interpreter.TrustedTypes;

                    if (trustedTypes != null)
                    {
                        foreach (KeyValuePair<string, object> pair in trustedTypes)
                        {
                            string trustedType = pair.Key;

                            if ((trustedType != null) &&
                                !types.ContainsKey(trustedType))
                            {
                                types.Add(trustedType, pair.Value);
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool CanBeTrustedUri(
            Interpreter interpreter, /* NOT USED */
            Uri uri
            )
        {
            if (uri == null)
                return false;

            //
            // TODO: Can a "trusted" URI really ever be anything other
            //       than HTTPS?
            //
            return PathOps.IsHttpsUriScheme(uri);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AddTrustedUris(
            Interpreter interpreter,
            UriDictionary<object> uris
            )
        {
            if (uris != null)
            {
                Uri uri; /* REUSED */

                //
                // NOTE: Add the URI for this assembly, if any; however,
                //       make sure it is secure (HTTPS).
                //
                uri = GlobalState.GetAssemblyUri();

                if ((uri != null) && CanBeTrustedUri(interpreter, uri))
                {
                    if (!uris.ContainsKey(uri))
                    {
                        //
                        // NOTE: For now, the value null is always used
                        //       here.
                        //
                        uris.Add(uri, null);
                    }
                }

                //
                // NOTE: Add the URIs trusted by the interpreter, if any;
                //       however, make sure they are secure (HTTPS).
                //
                if (interpreter != null)
                {
                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                    {
                        UriDictionary<object> trustedUris = interpreter.TrustedUris;

                        if (trustedUris != null)
                        {
                            foreach (KeyValuePair<Uri, object> pair in trustedUris)
                            {
                                uri = pair.Key;

                                if ((uri != null) && CanBeTrustedUri(interpreter, uri))
                                {
                                    if (!uris.ContainsKey(uri))
                                    {
                                        //
                                        // NOTE: For now, the "pair.Value"
                                        //       value is purposely ignored
                                        //       here.
                                        //
                                        uris.Add(uri, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndPlugin( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref IPlugin plugin,
            ref Result error
            )
        {
            if (clientData != null)
            {
                if (policyContext == null)
                    policyContext = clientData.Data as IPolicyContext;

                if (policyContext != null)
                {
                    plugin = policyContext.Plugin;

                    if (plugin != null)
                        return ReturnCode.Ok;
                    else
                        error = "invalid plugin";
                }
                else
                {
                    error = "policy clientData is not a policyContext object";
                }
            }
            else
            {
                error = "invalid policy clientData";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndScript( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref IScript script,
            ref Result error
            )
        {
            if (clientData != null)
            {
                if (policyContext == null)
                    policyContext = clientData.Data as IPolicyContext;

                if (policyContext != null)
                {
                    script = policyContext.Script;

                    if (script != null)
                        return ReturnCode.Ok;
                    else
                        error = "invalid script";
                }
                else
                {
                    error = "policy clientData is not a policyContext object";
                }
            }
            else
            {
                error = "invalid policy clientData";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndFileName( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string fileName,
            ref Result error
            )
        {
            if (clientData != null)
            {
                if (policyContext == null)
                    policyContext = clientData.Data as IPolicyContext;

                if (policyContext != null)
                {
                    fileName = policyContext.FileName;

                    if (!String.IsNullOrEmpty(fileName))
                        return ReturnCode.Ok;
                    else
                        error = "invalid file name";
                }
                else
                {
                    error = "policy clientData is not a policyContext object";
                }
            }
            else
            {
                error = "invalid policy clientData";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndText( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string text,
            ref Result error
            )
        {
            if (clientData != null)
            {
                if (policyContext == null)
                    policyContext = clientData.Data as IPolicyContext;

                if (policyContext != null)
                {
                    text = policyContext.Text;
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "policy clientData is not a policyContext object";
                }
            }
            else
            {
                error = "invalid policy clientData";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndTextAndBytes( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string text,
            ref ByteList bytes,
            ref Result error
            )
        {
            if (clientData != null)
            {
                if (policyContext == null)
                    policyContext = clientData.Data as IPolicyContext;

                if (policyContext != null)
                {
                    text = policyContext.Text;

                    IClientData policyClientData = policyContext.ClientData;

                    if (policyClientData != null)
                    {
                        ReadScriptClientData readScriptClientData =
                            policyClientData as ReadScriptClientData;

                        if (readScriptClientData != null)
                        {
                            ByteList localBytes = readScriptClientData.Bytes;

                            if (localBytes != null)
                                bytes = new ByteList(localBytes); /* COPY */
                            else
                                bytes = null;
                        }
                        else
                        {
                            bytes = null;
                        }
                    }
                    else
                    {
                        bytes = null;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "policy clientData is not a policyContext object";
                }
            }
            else
            {
                error = "invalid policy clientData";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        private static ReturnCode LookupPluginCommandType( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IPlugin plugin,
            Type commandType,
            ref ICommand command
            )
        {
            Result error = null;

            return LookupPluginCommandType(
                interpreter, plugin, commandType, ref command, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode LookupPluginCommandType( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IPlugin plugin,
            Type commandType,
            ref ICommand command,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                if (plugin != null)
                {
                    if (commandType != null)
                    {
                        List<ICommand> commands = null;

                        if (interpreter.GetPluginCommands(plugin,
                                ref commands, ref error) == ReturnCode.Ok)
                        {
                            foreach (ICommand localCommand in commands)
                            {
                                IWrapper wrapper = localCommand as IWrapper;

                                if (wrapper != null)
                                {
                                    object @object = wrapper.Object;

                                    if (@object != null)
                                    {
                                        if (Object.ReferenceEquals(
                                                @object.GetType(), commandType))
                                        {
                                            command = localCommand;
                                            return ReturnCode.Ok;
                                        }
                                    }
                                }
                                else
                                {
                                    error = "invalid command wrapper";
                                    return ReturnCode.Error;
                                }
                            }
                        }
                    }
                    else
                    {
                        error = "invalid command type";
                    }
                }
                else
                {
                    error = "invalid plugin";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndCommand( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IClientData clientData,
            Type commandType,
            long commandToken,
            ref IPolicyContext policyContext,
            ref bool match,
            ref Result error
            )
        {
            ICommand command = null;

            return ExtractPolicyContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref command, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExtractPolicyContextAndCommand( /* POLICY HELPER METHOD */
            Interpreter interpreter,
            IClientData clientData,
            Type commandType,
            long commandToken,
            ref IPolicyContext policyContext,
            ref ICommand command,
            ref bool match,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                if (clientData != null)
                {
                    if (policyContext == null)
                        policyContext = clientData.Data as IPolicyContext;

                    if (policyContext != null)
                    {
                        IExecute execute = policyContext.Execute;

                        if (execute != null)
                        {
                            if (commandType != null)
                            {
#if ISOLATED_PLUGINS
                                IPlugin plugin = policyContext.Plugin;

                                if (AppDomainOps.IsIsolated(plugin))
                                {
                                    command = null;

                                    if (((commandToken == 0) &&
                                        (LookupPluginCommandType(interpreter, plugin,
                                            commandType, ref command) == ReturnCode.Ok)) ||
                                        ((commandToken != 0) &&
                                        (interpreter.GetCommand(commandToken,
                                            LookupFlags.PolicyNoVerbose,
                                            ref command) == ReturnCode.Ok)))
                                    {
                                        match = Object.ReferenceEquals(execute, command);
                                    }
                                    else
                                    {
                                        match = false;
                                    }
                                }
                                else
#endif
                                {
                                    //
                                    // BUGBUG: This method call is a serious problem for
                                    //         isolated plugins.  The command type cannot be
                                    //         sent cleanly across the AppDomain boundry.
                                    //
                                    command = null;

                                    if (((commandToken == 0) &&
                                        (interpreter.GetCommand(commandType,
                                            LookupFlags.PolicyNoVerbose,
                                            ref command) == ReturnCode.Ok)) ||
                                        ((commandToken != 0) &&
                                        (interpreter.GetCommand(commandToken,
                                            LookupFlags.PolicyNoVerbose,
                                            ref command) == ReturnCode.Ok)))
                                    {
                                        match = Object.ReferenceEquals(execute, command);
                                    }
                                    else
                                    {
                                        match = false;
                                    }
                                }
                            }
                            else
                            {
                                //
                                // NOTE: Command type is null, skip type matching against
                                //       it (i.e. just extract it and return).
                                //
                                command = execute as ICommand;

                                match = (command != null);
                            }

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "policyContext does not contain an executable object";
                        }
                    }
                    else
                    {
                        error = "policy clientData is not a policyContext object";
                    }
                }
                else
                {
                    error = "invalid policy clientData";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Policy Implementations
        #region Trusted Sub-Command Policy Implementation
        public static ReturnCode SubCommandPolicy( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            StringDictionary subCommandNames,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IPolicyContext policyContext = null;
            ICommand command = null;
            bool match = false;

            if (ExtractPolicyContextAndCommand(
                    interpreter, clientData, commandType, commandToken,
                    ref policyContext, ref command, ref match,
                    ref result) == ReturnCode.Ok)
            {
                if (arguments != null)
                {
                    if (match)
                    {
                        string subCommandName = null;

                        if (arguments.Count >= 2) /* ENSEMBLE */
                            subCommandName = arguments[1];

                        if (!String.IsNullOrEmpty(subCommandName))
                        {
                            if (ScriptOps.SubCommandFromEnsemble(
                                    interpreter, command,
                                    OnlyAllowedSubCommands, true,
                                    false, ref subCommandName,
                                    ref result) == ReturnCode.Ok)
                            {
                                if (subCommandNames == null)
                                {
                                    subCommandNames = GetSubCommandNames(
                                        command, allowed);
                                }

                                if (allowed)
                                {
                                    if ((subCommandNames != null) &&
                                        subCommandNames.ContainsKey(subCommandName))
                                    {
                                        //
                                        // NOTE: The sub-command is in the
                                        //       "allowed" list, vote to
                                        //       allow the command to be
                                        //       executed.
                                        //
                                        policyContext.Approved();

                                        //
                                        // NOTE: Return the sub-command as
                                        //       the result.
                                        //
                                        policyContext.Result = subCommandName;
                                    }
                                }
                                else
                                {
                                    if ((subCommandNames != null) &&
                                        !subCommandNames.ContainsKey(subCommandName))
                                    {
                                        //
                                        // NOTE: The sub-command is not in
                                        //       the "denied" list, vote to
                                        //       allow the command to be
                                        //       executed.
                                        //
                                        policyContext.Approved();

                                        //
                                        // NOTE: Return the sub-command as
                                        //       the result.
                                        //
                                        policyContext.Result = subCommandName;
                                    }
                                }
                            }
                        }
                    }

                    //
                    // NOTE: The policy checking has been successful;
                    //       however, this does not necessarily mean
                    //       that we allow the command to be executed.
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    result = "invalid argument list";
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Trusted URI Policy Implementation
        public static ReturnCode UriPolicy( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Uri uri,
            UriDictionary<object> uris,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IPolicyContext policyContext = null;
            bool match = false;

            if (ExtractPolicyContextAndCommand(
                    interpreter, clientData, commandType,
                    commandToken, ref policyContext, ref match,
                    ref result) == ReturnCode.Ok)
            {
                if (match)
                {
                    if (allowed)
                    {
                        if ((uris != null) &&
                            uris.ContainsSchemeAndServer(uri))
                        {
                            //
                            // NOTE: The URI is in the "allowed"
                            //       list, vote to allow the
                            //       command to be executed.
                            //
                            policyContext.Approved();
                        }
                    }
                    else
                    {
                        if ((uris != null) &&
                            !uris.ContainsSchemeAndServer(uri))
                        {
                            //
                            // NOTE: The URI is not in the
                            //       "denied" list, vote to
                            //       allow the command to be
                            //       executed.
                            //
                            policyContext.Approved();
                        }
                    }
                }

                //
                // NOTE: The policy checking has been successful;
                //       however, this does not necessarily mean
                //       that we allow the command to be executed.
                //
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Trusted Directory Policy Implementation
        public static ReturnCode DirectoryPolicy( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            string fileName,
            PathDictionary<object> directories,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IPolicyContext policyContext = null;
            bool match = false;

            if (ExtractPolicyContextAndCommand(
                    interpreter, clientData, commandType,
                    commandToken, ref policyContext, ref match,
                    ref result) == ReturnCode.Ok)
            {
                if (match)
                {
                    string directory = null;

                    try
                    {
                        /* throw */
                        directory = Path.GetDirectoryName(
                            PathOps.BaseDirectorySubstitution(
                                interpreter, fileName));
                    }
                    catch
                    {
                        // do nothing.
                    }

                    if (!String.IsNullOrEmpty(directory))
                    {
                        if (allowed)
                        {
                            if ((directories != null) &&
                                directories.Contains(directory))
                            {
                                //
                                // NOTE: The directory is in the
                                //       "allowed" list, vote to
                                //       allow the command to be
                                //       executed.
                                //
                                policyContext.Approved();
                            }
                        }
                        else
                        {
                            if ((directories != null) &&
                                !directories.Contains(directory))
                            {
                                //
                                // NOTE: The directory is not in
                                //       the "denied" list, vote
                                //       to allow the command to
                                //       be executed.
                                //
                                policyContext.Approved();
                            }
                        }
                    }
                }

                //
                // NOTE: The policy checking has been successful;
                //       however, this does not necessarily mean
                //       that we allow the command to be executed.
                //
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Trusted Object Type Policy Implementation
        public static ReturnCode TypePolicy( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Type objectType,
            TypeList types,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IPolicyContext policyContext = null;
            bool match = false;

            if (ExtractPolicyContextAndCommand(
                    interpreter, clientData, commandType,
                    commandToken, ref policyContext, ref match,
                    ref result) == ReturnCode.Ok)
            {
                if (match)
                {
                    if (objectType != null)
                    {
                        if (allowed)
                        {
                            if ((types != null) &&
                                types.Contains(objectType))
                            {
                                //
                                // NOTE: The type is in the
                                //       "allowed" list, vote to
                                //       allow the command to be
                                //       executed.
                                //
                                policyContext.Approved();
                            }
                        }
                        else
                        {
                            if ((types != null) &&
                                !types.Contains(objectType))
                            {
                                //
                                // NOTE: The type is not in the
                                //       "denied" list, vote to
                                //       allow the command to be
                                //       executed.
                                //
                                policyContext.Approved();
                            }
                        }
                    }
                }

                //
                // NOTE: The policy checking has been successful;
                //       however, this does not necessarily mean
                //       that we allow the command to be executed.
                //
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dynamic User Managed Callback Policy Implementation
        public static ReturnCode CallbackPolicy( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            ICallback callback,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IPolicyContext policyContext = null;
            bool match = false;

            if (ExtractPolicyContextAndCommand(
                    interpreter, clientData, commandType,
                    commandToken, ref policyContext, ref match,
                    ref result) == ReturnCode.Ok)
            {
                if (arguments != null)
                {
                    if (match)
                    {
                        if (callback != null)
                        {
                            ReturnCode localCode;
                            Result localResult = null;

                            localCode = callback.Invoke(
                                new StringList(arguments), ref localResult);

                            switch (localCode)
                            {
                                case ReturnCode.Ok:
                                    {
                                        //
                                        // NOTE: The callback was executed
                                        //       successfully, vote to allow
                                        //       the command to be executed.
                                        //
                                        policyContext.Approved();
                                        break;
                                    }
                                case ReturnCode.Break:
                                    {
                                        //
                                        // NOTE: No vote is made for this
                                        //       return code.
                                        //
                                        break;
                                    }
                                case ReturnCode.Continue:
                                    {
                                        //
                                        // NOTE: This return code represents
                                        //       an official "undecided" vote.
                                        //
                                        policyContext.Undecided();
                                        break;
                                    }
                                case ReturnCode.Error:
                                case ReturnCode.Return: // NOTE: Invalid policy return code.
                                default:
                                    {
                                        //
                                        // NOTE: An error or any other
                                        //       return code is interpreted
                                        //       as a "denied" vote.
                                        //
                                        policyContext.Denied();
                                        break;
                                    }
                            }

                            //
                            // NOTE: Set the informational policy result to
                            //       the result of the callback execution.
                            //
                            policyContext.Result = Result.Copy(
                                localCode, localResult, true); /* COPY */
                        }
                    }

                    //
                    // NOTE: The policy checking has been successful;
                    //       however, this does not necessarily mean
                    //       that we allow the command to be executed.
                    //
                    return ReturnCode.Ok;
                }
                else
                {
                    result = "invalid argument list";
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dynamic User Script Evaluation Policy Implementation
        public static ReturnCode ScriptPolicy( /* POLICY IMPLEMENTATION */
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Interpreter policyInterpreter,
            string text,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            IPolicyContext policyContext = null;
            bool match = false;

            if (ExtractPolicyContextAndCommand(
                    interpreter, clientData, commandType,
                    commandToken, ref policyContext,
                    ref match, ref result) == ReturnCode.Ok)
            {
                if (match)
                {
                    //
                    // NOTE: *WARNING* Empty scripts are allowed, please
                    //        do not change this to "!String.IsNullOrEmpty".
                    //
                    if ((policyInterpreter != null) && (text != null))
                    {
                        ReturnCode localCode;
                        Result localResult = null;

                        localCode = EvaluatePolicyScript(
                            policyInterpreter, text, arguments, flags,
                            ref localResult);

                        switch (localCode)
                        {
                            case ReturnCode.Ok:
                                {
                                    //
                                    // NOTE: The callback was executed
                                    //       successfully, vote to allow
                                    //       the command to be executed.
                                    //
                                    policyContext.Approved();
                                    break;
                                }
                            case ReturnCode.Break:
                                {
                                    //
                                    // NOTE: No vote is made for this
                                    //       return code.
                                    //
                                    break;
                                }
                            case ReturnCode.Continue:
                                {
                                    //
                                    // NOTE: This return code represents
                                    //       an official "undecided" vote.
                                    //
                                    policyContext.Undecided();
                                    break;
                                }
                            case ReturnCode.Error:
                            case ReturnCode.Return: // NOTE: Not allowed.
                            default:
                                {
                                    //
                                    // NOTE: An error or any other return
                                    //       code is interpreted as a
                                    //       "denied" vote.
                                    //
                                    policyContext.Denied();
                                    break;
                                }
                        }

                        //
                        // NOTE: Set the informational policy result to
                        //       the result of the script evaluation.
                        //
                        policyContext.Result = Result.Copy(
                            localCode, localResult, true); /* COPY */
                    }
                }

                //
                // NOTE: The policy checking has been successful;
                //       however, this does not necessarily mean
                //       that we allow the command to be executed.
                //
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Default Core Command Policies
        #region The Default [file] Command Policy
        [MethodFlags(MethodFlags.CommandPolicy | MethodFlags.NoAdd)]
        private static ReturnCode FilePolicyCallback( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return SubCommandPolicy(
                PolicyFlags.SubCommand, typeof(_Commands._File),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region The Default [info] Command Policy
        [MethodFlags(MethodFlags.CommandPolicy | MethodFlags.NoAdd)]
        private static ReturnCode InfoPolicyCallback( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return SubCommandPolicy(
                PolicyFlags.SubCommand, typeof(_Commands.Info),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region The Default [interp] Command Policy
        [MethodFlags(MethodFlags.CommandPolicy | MethodFlags.NoAdd)]
        private static ReturnCode InterpPolicyCallback( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return SubCommandPolicy(
                PolicyFlags.SubCommand, typeof(_Commands.Interp),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region The Default [object] Command Policy
        [MethodFlags(MethodFlags.CommandPolicy | MethodFlags.NoAdd)]
        private static ReturnCode ObjectPolicyCallback( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return SubCommandPolicy(
                PolicyFlags.SubCommand, typeof(_Commands.Object),
                0, null, true, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region The Default [package] Command Policy
        [MethodFlags(MethodFlags.CommandPolicy | MethodFlags.NoAdd)]
        private static ReturnCode PackagePolicyCallback( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return SubCommandPolicy(
                PolicyFlags.SubCommand, typeof(_Commands.Package),
                0, null, false, interpreter, clientData, arguments,
                ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region The Default [source] Command Policy
        [MethodFlags(MethodFlags.CommandPolicy | MethodFlags.NoAdd)]
        private static ReturnCode SourcePolicyCallback( /* POLICY */
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            //
            // HACK: There is a small problem with this policy.  We need to
            //       examine the first argument to determine which policy
            //       checking method we would like to use; however, we do
            //       not actually know if the command being executed is
            //       [source].  In practice, this does not matter because
            //       the policy methods themselves double-check the command
            //       type.  Therefore, the argument checking here will be
            //       pointless (but harmless) if the command is not [source].
            //
            string fileName = null;

            if ((arguments != null) && (arguments.Count >= 2))
                fileName = arguments[arguments.Count - 1];

            //
            // NOTE: If the file name represents a remote URI, use slightly
            //       different policy handling.
            //
            Uri uri = null;

            if (PathOps.IsRemoteUri(fileName, ref uri))
            {
                //
                // NOTE: Only allow remote sites that we know, trust, and have
                //       100% positive control over.
                //
                UriDictionary<object> trustedUris = new UriDictionary<object>();

                AddTrustedUris(interpreter, trustedUris);

                return UriPolicy(
                    PolicyFlags.Uri, typeof(_Commands.Source), 0, uri,
                    trustedUris, true, interpreter, clientData, arguments,
                    ref result);
            }
            else
            {
                PathDictionary<object> directories = new PathDictionary<object>();

                AddTrustedDirectories(interpreter, directories);

                return DirectoryPolicy(
                    PolicyFlags.Directory, typeof(_Commands.Source), 0,
                    fileName, directories, true, interpreter, clientData,
                    arguments, ref result);
            }
        }
        #endregion
        #endregion
    }
}
