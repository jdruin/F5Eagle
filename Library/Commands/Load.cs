/*
 * Load.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if CAS_POLICY
using System.Configuration.Assemblies;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

namespace Eagle._Commands
{
    [ObjectId("eba460e1-048f-409a-a18c-70c5dc6aad6b")]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.Standard)]
    [ObjectGroup("managedEnvironment")]
    internal sealed class Load : Core
    {
        public Load(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

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
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] {
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nocommands", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nofunctions", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-nopolicies", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-notraces", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noprovide", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noresources", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-verifiedonly", null),
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-trustedonly", null),
#if ISOLATED_PLUGINS
                            new Option(null, OptionFlags.Unsafe, Index.Invalid, Index.Invalid, "-noisolated", null),
#else
                            new Option(null, OptionFlags.Unsafe | OptionFlags.Unsupported, Index.Invalid, Index.Invalid,
                                "-noisolated", null),
#endif
                            new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-clientdata", null),
                            new Option(null, OptionFlags.MustHaveObjectValue, Index.Invalid, Index.Invalid, "-data", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                        });

                        int argumentIndex = Index.Invalid;

                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            //
                            // NOTE: There should be a minimum of one and a maximum
                            //       of three arguments after the final option.
                            //
                            if ((argumentIndex != Index.Invalid) &&
                                ((argumentIndex + 3) >= arguments.Count))
                            {
                                string path = ((argumentIndex + 2) < arguments.Count) ?
                                    (string)arguments[argumentIndex + 2] : String.Empty;

                                Interpreter slaveInterpreter = null;

                                code = interpreter.GetNestedSlaveInterpreter(
                                    path, LookupFlags.Interpreter, false,
                                    ref slaveInterpreter, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    Variant value = null;
                                    IClientData localClientData = clientData;

                                    if (options.IsPresent("-clientdata", ref value))
                                    {
                                        IObject @object = (IObject)value.Value;

                                        if ((@object.Value == null) ||
                                            (@object.Value is IClientData))
                                        {
                                            localClientData = (IClientData)@object.Value;
                                        }
                                        else
                                        {
                                            result = "option value has invalid clientData";
                                            code = ReturnCode.Error;
                                        }
                                    }

                                    if (code == ReturnCode.Ok)
                                    {
                                        if (options.IsPresent("-data", ref value))
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
                                            //
                                            // NOTE: All plugins loaded by this command are considered
                                            //       as having been loaded "on demand".
                                            //
                                            PluginFlags pluginFlags = PluginFlags.Demand;

                                            //
                                            // NOTE: Add the plugin flags for the target interpreter.
                                            //
                                            pluginFlags |= slaveInterpreter.PluginFlags;

#if ISOLATED_PLUGINS
                                            //
                                            // NOTE: Disable loading this plugin into an isolated
                                            //       application domain (i.e. load it into the default
                                            //       application domain for the target interpreter).
                                            //
                                            if (options.IsPresent("-noisolated"))
                                                pluginFlags &= ~PluginFlags.Isolated;
#endif

                                            if (options.IsPresent("-nocommands"))
                                                pluginFlags |= PluginFlags.NoCommands;

                                            if (options.IsPresent("-nofunctions"))
                                                pluginFlags |= PluginFlags.NoFunctions;

                                            if (options.IsPresent("-nopolicies"))
                                                pluginFlags |= PluginFlags.NoPolicies;

                                            if (options.IsPresent("-notraces"))
                                                pluginFlags |= PluginFlags.NoTraces;

                                            if (options.IsPresent("-noprovide"))
                                                pluginFlags |= PluginFlags.NoProvide;

                                            if (options.IsPresent("-noresources"))
                                                pluginFlags |= PluginFlags.NoResources;

                                            if (options.IsPresent("-verifiedonly"))
                                                pluginFlags |= PluginFlags.VerifiedOnly;

                                            if (options.IsPresent("-trustedonly"))
                                                pluginFlags |= PluginFlags.TrustedOnly;

                                            string fileName = PathOps.ResolveFullPath(
                                                interpreter, arguments[argumentIndex]);

                                            if (!String.IsNullOrEmpty(fileName))
                                            {
                                                string typeName = null;

                                                if ((argumentIndex + 1) < arguments.Count)
                                                    typeName = arguments[argumentIndex + 1];

                                                IPlugin plugin = null;
                                                long token = 0;

                                                try
                                                {
                                                    code = slaveInterpreter.LoadPlugin(
                                                        fileName,
#if CAS_POLICY
                                                        null, null, AssemblyHashAlgorithm.None,
#endif
                                                        typeName, localClientData, pluginFlags,
                                                        ref plugin, ref result);

                                                    if (code == ReturnCode.Ok)
                                                    {
                                                        code = slaveInterpreter.AddPlugin(
                                                            plugin, localClientData, ref token,
                                                            ref result);
                                                    }
                                                }
                                                finally
                                                {
                                                    if (code != ReturnCode.Ok)
                                                    {
                                                        if (token != 0)
                                                        {
                                                            //
                                                            // NOTE: Terminate and remove the plugin now.
                                                            //       This does not unload the associated
                                                            //       AppDomain, if any.
                                                            //
                                                            ReturnCode removeCode;
                                                            Result removeResult = null;

                                                            removeCode = slaveInterpreter.RemovePlugin(
                                                                token, localClientData, ref removeResult);

                                                            if (removeCode != ReturnCode.Ok)
                                                            {
                                                                DebugOps.Complain(
                                                                    slaveInterpreter, removeCode,
                                                                    removeResult);
                                                            }
                                                        }

                                                        if (plugin != null)
                                                        {
                                                            //
                                                            // NOTE: Unload the plugin.  This basically does
                                                            //       "nothing" unless the plugin was isolated.
                                                            //       In that case, it unloads the associated
                                                            //       AppDomain.
                                                            //
                                                            ReturnCode unloadCode;
                                                            Result unloadResult = null;

                                                            unloadCode = slaveInterpreter.UnloadPlugin(
                                                                plugin, localClientData, pluginFlags |
                                                                PluginFlags.SkipTerminate, ref unloadResult);

                                                            if (unloadCode != ReturnCode.Ok)
                                                            {
                                                                DebugOps.Complain(
                                                                    slaveInterpreter, unloadCode,
                                                                    unloadResult);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                result = "invalid file name";
                                                code = ReturnCode.Error;
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
                                    result = "wrong # args: should be \"load ?options? fileName ?packageName? ?interp?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"load ?options? fileName ?packageName? ?interp?\"";
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
