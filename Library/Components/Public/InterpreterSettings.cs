/*
 * InterpreterSettings.cs --
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

#if XML
using System.IO;
using System.Xml;
#endif

#if XML && SERIALIZATION
using System.Xml.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;

#if XML
using Eagle._Constants;
#endif

using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("1d0263ae-929f-4ea1-a6c6-cd8b749d55bb")]
    public sealed class InterpreterSettings
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        : ScriptMarshalByRefObject
#endif
    {
        #region Private Data
        private IEnumerable<string> args;
        private string culture;
        private CreateFlags createFlags;
        private InitializeFlags initializeFlags;
        private ScriptFlags scriptFlags;
        private InterpreterFlags interpreterFlags;

#if SERIALIZATION
        [NonSerialized()]
#endif
        private AppDomain appDomain;

        private IHost host;
        private string profile;
        private object owner;
        private object applicationObject;
        private object policyObject;
        private object resolverObject;
        private object userObject;
        private ExecuteCallbackDictionary policies;
        private TraceList traces;
        private string text;
        private string libraryPath;
        private StringList autoPathList;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private InterpreterSettings()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static InterpreterSettings Create()
        {
            return new InterpreterSettings();
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        public static InterpreterSettings CreateFrom(
            string fileName,
            bool expand,
            ref Result error
            )
        {
            InterpreterSettings interpreterSettings = null;

            if (LoadFrom(fileName, expand, ref interpreterSettings,
                    ref error) == ReturnCode.Ok)
            {
                return interpreterSettings;
            }

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static string Expand(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            return CommonOps.Environment.ExpandVariables(value);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void Expand(
            InterpreterSettings interpreterSettings
            )
        {
            if (interpreterSettings != null)
            {
                IEnumerable<string> args = interpreterSettings.Args;

                if (args != null)
                {
                    StringList newArgs = new StringList();

                    foreach (string arg in args)
                        newArgs.Add(Expand(arg));

                    interpreterSettings.Args = newArgs;
                }

                interpreterSettings.Culture = Expand(
                    interpreterSettings.Culture);

                interpreterSettings.Profile = Expand(
                    interpreterSettings.Profile);

                interpreterSettings.Text = Expand(interpreterSettings.Text);

                interpreterSettings.LibraryPath = Expand(
                    interpreterSettings.LibraryPath);

                StringList autoPathList = interpreterSettings.AutoPathList;

                if (autoPathList != null)
                {
                    for (int index = 0; index < autoPathList.Count; index++)
                        autoPathList[index] = Expand(autoPathList[index]);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode FromInterpreter(
            Interpreter interpreter,
            InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (interpreter.PopulateInterpreterSettings(
                        ref interpreterSettings, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                interpreterSettings.CreateFlags = interpreter.CreateFlags;
                interpreterSettings.InitializeFlags = interpreter.InitializeFlags;
                interpreterSettings.ScriptFlags = interpreter.ScriptFlags;
                interpreterSettings.InterpreterFlags = interpreter.InterpreterFlags;
                interpreterSettings.Owner = interpreter.Owner;
                interpreterSettings.ApplicationObject = interpreter.ApplicationObject;
                interpreterSettings.PolicyObject = interpreter.PolicyObject;
                interpreterSettings.ResolverObject = interpreter.ResolverObject;
                interpreterSettings.UserObject = interpreter.UserObject;
                interpreterSettings.LibraryPath = interpreter.LibraryPath;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        private static ReturnCode FromDocument(
            XmlDocument document,
            InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            XmlElement documentElement = document.DocumentElement;

            if (documentElement == null)
            {
                error = "invalid xml document element";
                return ReturnCode.Error;
            }

            XmlNode node;

            node = documentElement.SelectSingleNode("Args");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                StringList list = null;

                if (Parser.SplitList(
                        null, node.InnerText, 0, Length.Invalid, false,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    interpreterSettings.Args = list;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            node = documentElement.SelectSingleNode("CreateFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                object enumValue = EnumOps.TryParseEnum(
                    typeof(CreateFlags), node.InnerText,
                    true, true, ref error);

                if (enumValue is CreateFlags)
                    interpreterSettings.CreateFlags = (CreateFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("InitializeFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                object enumValue = EnumOps.TryParseEnum(
                    typeof(InitializeFlags), node.InnerText,
                    true, true, ref error);

                if (enumValue is InitializeFlags)
                    interpreterSettings.InitializeFlags = (InitializeFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("ScriptFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                object enumValue = EnumOps.TryParseEnum(
                    typeof(ScriptFlags), node.InnerText,
                    true, true, ref error);

                if (enumValue is ScriptFlags)
                    interpreterSettings.ScriptFlags = (ScriptFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("InterpreterFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                object enumValue = EnumOps.TryParseEnum(
                    typeof(InterpreterFlags), node.InnerText,
                    true, true, ref error);

                if (enumValue is InterpreterFlags)
                    interpreterSettings.InterpreterFlags = (InterpreterFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("AutoPathList");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                StringList list = null;

                if (Parser.SplitList(
                        null, node.InnerText, 0, Length.Invalid, false,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    interpreterSettings.AutoPathList = list;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ToDocument(
            XmlDocument document,
            InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            XmlElement documentElement = document.DocumentElement;

            if (documentElement == null)
            {
                error = "invalid xml document element";
                return ReturnCode.Error;
            }

            XmlNode node;

            if (interpreterSettings.Args != null)
            {
                node = document.CreateElement("Args");

                node.InnerText = new StringList(
                    interpreterSettings.Args).ToString();

                documentElement.AppendChild(node);
            }

            node = document.CreateElement("CreateFlags");
            node.InnerText = interpreterSettings.CreateFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("InitializeFlags");
            node.InnerText = interpreterSettings.InitializeFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("ScriptFlags");
            node.InnerText = interpreterSettings.ScriptFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("InterpreterFlags");
            node.InnerText = interpreterSettings.InterpreterFlags.ToString();
            documentElement.AppendChild(node);

            if (interpreterSettings.AutoPathList != null)
            {
                node = document.CreateElement("AutoPathList");
                node.InnerText = interpreterSettings.AutoPathList.ToString();
                documentElement.AppendChild(node);
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Static Methods
        internal static ReturnCode UseStartupDefaults(
            InterpreterSettings interpreterSettings,
            CreateFlags createFlags,
            ref Result error
            )
        {
            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            //
            // NOTE: Use the creation flags specified by the caller,
            //       ignoring the creation flags in the interpreter
            //       settings.  If there are existing policies and/or
            //       traces, make sure the creation flags are modified
            //       to skip adding the default policies and/or traces
            //       during interpreter creation.
            //
            if (interpreterSettings.Policies != null)
                createFlags |= CreateFlags.NoCorePolicies;

            if (interpreterSettings.Traces != null)
                createFlags |= CreateFlags.NoCoreTraces;

            interpreterSettings.CreateFlags = createFlags;

            //
            // NOTE: The interpreter host may be disposed now -OR- may
            //       end up being disposed later, so avoid copying it.
            //
            interpreterSettings.Host = null;

            //
            // NOTE: Nulling these out is not necessary when the creation
            //       flags are modified to skip adding default policies
            //       and traces (above).
            //
            // interpreterSettings.Policies = null;
            // interpreterSettings.Traces = null;

            //
            // NOTE: These startup settings are reset by this method to
            //       avoid having their values used when the command line
            //       arguments have been "locked" by the interpreter host.
            //
            interpreterSettings.Text = null;
            interpreterSettings.LibraryPath = null;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static ReturnCode LoadFrom(
            Interpreter interpreter,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreterSettings != null)
            {
                error = "cannot overwrite valid interpreter settings";
                return ReturnCode.Error;
            }

            try
            {
                InterpreterSettings localInterpreterSettings = Create();

                if (FromInterpreter(
                        interpreter, localInterpreterSettings,
                        ref error) == ReturnCode.Ok)
                {
                    if (expand)
                        Expand(localInterpreterSettings);

                    interpreterSettings = localInterpreterSettings;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        public static ReturnCode LoadFrom(
            string fileName,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "cannot read \"{0}\": no such file",
                    fileName);

                return ReturnCode.Error;
            }

            if (interpreterSettings != null)
            {
                error = "cannot overwrite valid interpreter settings";
                return ReturnCode.Error;
            }

            try
            {
                XmlDocument document = new XmlDocument();
                document.Load(fileName);

                using (XmlNodeReader reader = new XmlNodeReader(document))
                {
                    object @object = null;

                    if (XmlOps.Deserialize(
                            typeof(InterpreterSettings), reader,
                            ref @object, ref error) == ReturnCode.Ok)
                    {
                        InterpreterSettings localInterpreterSettings =
                            @object as InterpreterSettings;

                        if (FromDocument(document, localInterpreterSettings,
                                ref error) == ReturnCode.Ok)
                        {
                            if (expand)
                                Expand(localInterpreterSettings);

                            interpreterSettings = localInterpreterSettings;
                            return ReturnCode.Ok;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SaveTo(
            string fileName,
            bool expand,
            InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (File.Exists(fileName))
            {
                error = String.Format(
                    "cannot write \"{0}\": file already exists",
                    fileName);

                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            try
            {
                using (Stream stream = new FileStream(fileName,
                        FileMode.CreateNew, FileAccess.Write)) /* EXEMPT */
                {
                    using (MemoryStream stream2 = new MemoryStream())
                    {
                        using (XmlTextWriter writer = new XmlTextWriter(
                                stream2, null))
                        {
                            if (expand)
                                Expand(interpreterSettings);

                            if (XmlOps.Serialize(
                                    interpreterSettings,
                                    typeof(InterpreterSettings), writer,
                                    null, ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            writer.Flush();

                            XmlDocument document;

                            using (MemoryStream stream3 = new MemoryStream(
                                    stream2.ToArray(), false))
                            {
                                writer.Close();

                                document = new XmlDocument();
                                document.Load(stream3);
                            }

                            if (ToDocument(
                                    document, interpreterSettings,
                                    ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            XmlWriterSettings writerSettings =
                                new XmlWriterSettings();

                            writerSettings.Indent = true;

                            using (XmlWriter writer2 = XmlWriter.Create(
                                    stream, writerSettings))
                            {
                                document.WriteTo(writer2);
                            }

                            return ReturnCode.Ok;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public IEnumerable<string> Args
        {
            get { return args; }
            set { args = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Culture
        {
            get { return culture; }
            set { culture = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public CreateFlags CreateFlags
        {
            get { return createFlags; }
            set { createFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public InitializeFlags InitializeFlags
        {
            get { return initializeFlags; }
            set { initializeFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public ScriptFlags ScriptFlags
        {
            get { return scriptFlags; }
            set { scriptFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public InterpreterFlags InterpreterFlags
        {
            get { return interpreterFlags; }
            set { interpreterFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public AppDomain AppDomain
        {
            get { return appDomain; }
            set { appDomain = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public IHost Host
        {
            get { return host; }
            set { host = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Profile
        {
            get { return profile; }
            set { profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object ApplicationObject
        {
            get { return applicationObject; }
            set { applicationObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object PolicyObject
        {
            get { return policyObject; }
            set { policyObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object ResolverObject
        {
            get { return resolverObject; }
            set { resolverObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object UserObject
        {
            get { return userObject; }
            set { userObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public ExecuteCallbackDictionary Policies
        {
            get { return policies; }
            set { policies = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public TraceList Traces
        {
            get { return traces; }
            set { traces = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string LibraryPath
        {
            get { return libraryPath; }
            set { libraryPath = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public StringList AutoPathList
        {
            get { return autoPathList; }
            set { autoPathList = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            StringList list = new StringList();

            list.Add("args");
            list.Add((args != null) ? args.ToString() : null);

            list.Add("culture");
            list.Add(culture);

            list.Add("createFlags");
            list.Add(createFlags.ToString());

            list.Add("initializeFlags");
            list.Add(initializeFlags.ToString());

            list.Add("scriptFlags");
            list.Add(scriptFlags.ToString());

            list.Add("interpreterFlags");
            list.Add(interpreterFlags.ToString());

            list.Add("appDomain");
            list.Add((appDomain != null) ? appDomain.ToString() : null);

            list.Add("host");
            list.Add((host != null) ? host.ToString() : null);

            list.Add("profile");
            list.Add(profile);

            list.Add("owner");
            list.Add((owner != null) ? owner.ToString() : null);

            list.Add("applicationObject");
            list.Add((applicationObject != null) ?
                applicationObject.ToString() : null);

            list.Add("policyObject");
            list.Add((policyObject != null) ?
                policyObject.ToString() : null);

            list.Add("resolverObject");
            list.Add((resolverObject != null) ?
                resolverObject.ToString() : null);

            list.Add("userObject");
            list.Add((userObject != null) ? userObject.ToString() : null);

            list.Add("policies");
            list.Add((policies != null) ? policies.ToString() : null);

            list.Add("traces");
            list.Add((traces != null) ? traces.ToString() : null);

            list.Add("text");
            list.Add(text);

            list.Add("libraryPath");
            list.Add(libraryPath);

            list.Add("autoPathList");
            list.Add((autoPathList != null) ? autoPathList.ToString() : null);

            return list.ToString();
        }
        #endregion
    }
}
