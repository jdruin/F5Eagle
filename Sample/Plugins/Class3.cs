/*
 * Class3.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Plugins = Eagle._Plugins;

namespace Sample
{
    /// <summary>
    /// Declare a "custom plugin" class that inherits default functionality and
    /// implements the appropriate interface(s).  This is the "primary" plugin
    /// for this assembly.  Only one plugin per assembly can be marked as the
    /// "primary" one.
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("c5e02acc-2297-4004-99e6-fbca0e6c812e")]
    [PluginFlags(
        PluginFlags.Primary | PluginFlags.User |
        PluginFlags.Command | PluginFlags.Function |
        PluginFlags.Trace | PluginFlags.Policy)]
    internal sealed class Class3 : _Plugins.Default
    {
        #region Private Data
        /// <summary>
        /// This field is used to store the token returned by the core library
        /// that represents the plugin instance loaded into the interpreter.
        /// </summary>
        private long functionToken;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructor (Required)
        /// <summary>
        /// Constructs an instance of this custom plugin class.
        /// </summary>
        /// <param name="pluginData">
        /// An instance of the plugin data class containing the properties
        /// used to initialize the new instance of this custom plugin class.
        /// Typically, the only thing done with this parameter is to pass it
        /// along to the base class constructor.
        /// </param>
        public Class3(
            IPluginData pluginData /* in */
            )
            : base(pluginData)
        {
            //
            // NOTE: Typically, nothing much is done here.  Any non-trivial
            //       initialization should be done in IState.Initialize and
            //       cleaned up in IState.Terminate.
            //
            this.Flags |= Utility.GetPluginFlags(GetType().BaseType) |
                Utility.GetPluginFlags(this); /* HIGHLY RECOMMENDED */

            //
            // HACK: For now, skip adding policies if we are being loaded into
            //       an isolated application domain.
            //
            // if (Utility.HasFlags(this.Flags, PluginFlags.Isolated, true))
            //     this.Flags |= PluginFlags.NoPolicies;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members (Optional)
        /// <summary>
        /// Initialize the plugin and/or setup any needed state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this plugin was initially created, if
        /// any.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Initialize(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most plugins
                //       will want to do this because it is a fairly standard
                //       safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            IFunction function = new Class8(new FunctionData(
                typeof(Class8).Name, null, null, clientData, null,
                1, null, Utility.GetFunctionFlags(typeof(Class8)),
                this, functionToken));

            if (interpreter.AddFunction(function, clientData,
                    ref functionToken, ref result) == ReturnCode.Ok)
            {
                return base.Initialize(interpreter, clientData, ref result);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Terminate the plugin and/or cleanup any state we setup during
        /// Initialize.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied when this plugin was initially created, if
        /// any.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Terminate(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                //
                // NOTE: We require a valid interpreter context.  Most plugins
                //       will want to do this because it is a fairly standard
                //       safety check (HIGHLY RECOMMENDED).
                //
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if ((interpreter == null) || (functionToken == 0) ||
                interpreter.RemoveFunction(functionToken, clientData,
                    ref result) == ReturnCode.Ok)
            {
                return base.Terminate(interpreter, clientData, ref result);
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members (optional)
        /// <summary>
        /// Returns information about the loaded plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode About(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            )
        {
            result = Utility.FormatPluginAbout(this, true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the compilation options used when compiling the loaded
        /// plugin as a list of strings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain a list of strings consisting of the
        /// compilation options used when compiling the loaded plugin.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Options(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            )
        {
            result = new StringList(DefineConstants.OptionList, false);
            return ReturnCode.Ok;
        }
        #endregion
    }
}
