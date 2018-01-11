/*
 * Test.cs --
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
using System.IO;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    [ObjectId("f5813bfc-7fae-45bb-ab05-9e2d9f1ef49f")]
    [PluginFlags(
        PluginFlags.System | PluginFlags.Command |
        PluginFlags.Static | PluginFlags.MergeCommands |
        PluginFlags.Test)]
    internal sealed class Test : Default
    {
        #region Public Constructors
        public Test(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= GetConstructorPluginFlags();
            this.ExtraFlags = GetDefaultExtraFlags();
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

        #region Public Properties
        private bool enableExecute;
        public bool EnableExecute
        {
            get { return enableExecute; }
            set { enableExecute = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool enableGetStream;
        public bool EnableGetStream
        {
            get { return enableGetStream; }
            set { enableGetStream = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool enableGetString;
        public bool EnableGetString
        {
            get { return enableGetString; }
            set { enableGetString = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private PluginFlags extraFlags;
        public PluginFlags ExtraFlags
        {
            get { return extraFlags; }
            set { extraFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static PluginFlags GetDefaultExtraFlags()
        {
            return PluginFlags.NoCommands | PluginFlags.NoFunctions |
                   PluginFlags.NoPolicies | PluginFlags.NoTraces |
                   PluginFlags.NoProvide | PluginFlags.NoResult;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private PluginFlags GetConstructorPluginFlags()
        {
            return AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);
        }

        ///////////////////////////////////////////////////////////////////////

        private PluginFlags GetIStatePluginFlags()
        {
            return this.Flags | this.ExtraFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        private ICommand CreateCommand(
            IClientData clientData
            )
        {
            return new _Commands.Nop(new CommandData(
                FormatOps.PluginCommand(this.Assembly, this.Name,
                typeof(_Commands.Nop), null), null, null, clientData,
                typeof(_Commands.Nop).FullName, CommandFlags.None,
                this, 0));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public override ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            //
            // NOTE: This method cannot rely on automatic command handling
            //       provided by the default plugin because it does not own
            //       the core command set.  This is very useful for testing
            //       "custom" plugin handling that does not involve relying
            //       on the default plugin.
            //
            if (interpreter != null)
            {
                //
                // NOTE: The test plugin command is "non-standard".  Create
                //       and add it only if the interpreter matches.
                //
                ReturnCode code = interpreter.IsStandard() ? ReturnCode.Ok :
                    interpreter.AddCommand(CreateCommand(clientData), null,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    Version version = this.Version;

                    code = interpreter.PkgProvide(
                        this.GetType().FullName, version, GetPackageFlags(),
                        ref result);

                    if (code == ReturnCode.Ok)
                        result = StringList.MakeList(this.Name, version);
                }
            }

            ///////////////////////////////////////////////////////////////////

            this.Flags = GetIStatePluginFlags();

            return base.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            //
            // NOTE: This method cannot rely on automatic command handling
            //       provided by the default plugin because it does not own
            //       the core command set.  This is very useful for testing
            //       "custom" plugin handling that does not involve relying
            //       on the default plugin.
            //
            if (interpreter != null)
            {
                //
                // NOTE: Attempt to remove all commands owned by this plugin
                //       now.  This is harmless if no commands are found to
                //       be owned by this plugin.
                //
                ReturnCode code = interpreter.RemoveCommands(
                    this, clientData, CommandFlags.None, ref result);

                if (code == ReturnCode.Ok)
                {
                    Version version = this.Version;

                    code = interpreter.WithdrawPackage(
                        this.GetType().FullName, version, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // HACK: Cleanup the test plugin token in the
                        //       interpreter state because this is the
                        //       only place where we can be 100% sure
                        //       it will get done.
                        //
                        interpreter.InternalTestPluginToken = 0;

                        result = StringList.MakeList(this.Name, version);
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            this.Flags = GetIStatePluginFlags();

            return base.Terminate(interpreter, clientData, ref result);
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
            if (!enableExecute)
            {
                return base.Execute(
                    interpreter, clientData, request, ref response,
                    ref error);
            }

            if (clientData != null)
            {
                response = new object[] {
                    interpreter, clientData.Data, request
                };

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid clientData";
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        public override Stream GetStream(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            if (!enableGetStream)
                return base.GetStream(interpreter, name, ref error);

            if (String.IsNullOrEmpty(name))
            {
                error = "invalid stream name";
                return null;
            }

            Assembly assembly = this.Assembly;

            if (assembly == null)
            {
                error = "plugin assembly not available";
                return null;
            }

            try
            {
                Stream stream = assembly.GetManifestResourceStream(name);

                if (stream != null)
                    return stream;

                stream = assembly.GetManifestResourceStream(
                    PathOps.MakeRelativePath(name, true));

                if (stream != null)
                    return stream;

                string prefix = GlobalState.GetBasePath();

                if (name.StartsWith(prefix, PathOps.ComparisonType))
                {
                    stream = assembly.GetManifestResourceStream(
                        name.Substring(prefix.Length));

                    if (stream != null)
                        return stream;
                }

                stream = assembly.GetManifestResourceStream(name);

                if (stream != null)
                    return stream;

                stream = assembly.GetManifestResourceStream(
                    Path.GetFileName(name));

                if (stream != null)
                    return stream;

                error = "stream not found";
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public override string GetString(
            Interpreter interpreter,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (!enableGetString)
            {
                return base.GetString(
                    interpreter, name, cultureInfo, ref error);
            }

            if (name != null)
            {
                return String.Format(
                    "interpreter: {0}, name: {1}, cultureInfo: {2}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(cultureInfo));
            }
            else
            {
                error = "invalid string name";
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
