/*
 * XmlOps.cs --
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
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;

#if SERIALIZATION
using System.Xml.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("dc088aa2-7481-4313-94dc-4809fb31eb1d")]
    internal static class XmlOps
    {
        #region Private Constants
        //
        // NOTE: This string is used to detect if a given string looks like
        //       the start of an XML document.  This is not designed to be a
        //       "perfect" detection mechanism; however, it will work well
        //       enough for our purposes.
        //
        private static readonly string DocumentStart = "<?xml ";

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        private static readonly string HelpXPath = "//help";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Generic Xml Handling Methods
        public static bool CouldBeDocument(
            string path
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            string extension = PathOps.GetExtension(path);

            if (String.IsNullOrEmpty(extension))
                return false;

            if (String.Equals(extension,
                    FileExtension.Markup, PathOps.ComparisonType))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeDocument(
            string text
            )
        {
            return (text != null) && text.StartsWith(
                DocumentStart, StringOps.SystemStringComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetEncoding(
            string fileName,
            Assembly assembly,
            string resourceName,
            bool validate,
            bool strict,
            ref Encoding encoding
            )
        {
            Result error = null;

            return GetEncoding(
                fileName, assembly, resourceName, validate, strict,
                ref encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetEncoding(
            string fileName,
            Assembly assembly,
            string resourceName,
            bool validate,
            bool strict,
            ref Encoding encoding,
            ref Result error
            )
        {
            XmlDocument document = null;

            if (LoadFile(
                    fileName, ref document,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (validate && (Validate(
                    assembly, resourceName, document,
                    ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            return GetEncoding(
                document, strict, ref encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetEncoding(
            XmlDocument document,
            bool strict,
            ref Encoding encoding,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            try
            {
                XmlDeclaration declaration =
                    document.FirstChild as XmlDeclaration;

                if (declaration == null)
                {
                    error = "invalid xml declaration";
                    return ReturnCode.Error;
                }

                string encodingName = declaration.Encoding;

                if (!String.IsNullOrEmpty(encodingName))
                {
                    encoding = Encoding.GetEncoding(encodingName); /* throw */
                    return ReturnCode.Ok;
                }
                else if (strict)
                {
                    error = "invalid encoding name";
                }
                else
                {
                    encoding = StringOps.GetEncoding(EncodingType.Default);
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

        public static ReturnCode LoadString(
            string xml,
            ref XmlDocument document,
            ref Result error
            )
        {
            if (xml == null)
            {
                error = "invalid xml";
                return ReturnCode.Error;
            }

            try
            {
                document = new XmlDocument();
                document.LoadXml(xml); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadFile(
            string fileName,
            ref XmlDocument document,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            bool remoteUri = PathOps.IsRemoteUri(fileName);

            if (!remoteUri && !File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't read file \"{0}\": no such file or directory",
                    fileName);

                return ReturnCode.Error;
            }

            try
            {
                document = new XmlDocument();
                document.Load(fileName); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Xml Script Block Methods
        #region Private Xml Script Block Methods
        private static ReturnCode GetSchemaStream(
            string schemaXml,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                Encoding encoding = StringOps.GetEncoding(
                    EncodingType.Default);

                if (encoding == null)
                {
                    error = "invalid encoding";
                    return ReturnCode.Error;
                }

                stream = new MemoryStream(encoding.GetBytes(schemaXml));

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetSchemaStream(
            Assembly assembly,
            string resourceName,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                if (assembly == null)
                    assembly = GlobalState.GetAssembly();

                if (resourceName == null)
                    resourceName = Xml.SchemaResourceName;

                stream = AssemblyOps.GetResourceStream(
                    assembly, resourceName, ref error);

                if (stream != null)
                    return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNamespaceManager(
            string namespaceName,
            Uri namespaceUri,
            XmlNameTable nameTable,
            ref XmlNamespaceManager namespaceManager,
            ref Result error
            )
        {
            if (nameTable == null)
            {
                error = "invalid xml name table";
                return ReturnCode.Error;
            }

            if (namespaceName == null)
            {
                error = "invalid xml namespace name";
                return ReturnCode.Error;
            }

            if (namespaceUri == null)
            {
                error = "invalid xml namespace uri";
                return ReturnCode.Error;
            }

            try
            {
                namespaceManager = new XmlNamespaceManager(nameTable);

                namespaceManager.AddNamespace(
                    namespaceName, namespaceUri.ToString());

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNamespaceManager(
            XmlNameTable nameTable,
            ref XmlNamespaceManager namespaceManager,
            ref Result error
            )
        {
            return GetNamespaceManager(
                Xml.NamespaceName, Xml.NamespaceUri, nameTable,
                ref namespaceManager, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode Validate(
            XmlSchema schema,
            XmlDocument document,
            ref Result error
            )
        {
            if (schema == null)
            {
                error = "invalid xml schema";
                return ReturnCode.Error;
            }

            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            try
            {
                document.Schemas.Add(schema); /* throw */
                document.Validate(null); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Xml Script Block Methods
        public static ReturnCode Validate(
            string schemaXml,
            XmlDocument document,
            ref Result error
            )
        {
            if (schemaXml == null)
            {
                error = "invalid schema xml";
                return ReturnCode.Error;
            }

            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            Stream stream = null;

            try
            {
                if (GetSchemaStream(
                        schemaXml, ref stream, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                try
                {
                    return Validate(
                        XmlSchema.Read(stream, null), document, ref error);
                }
                catch (Exception e)
                {
                    error = e;
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

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Validate(
            Assembly assembly,
            string resourceName,
            XmlDocument document,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            Stream stream = null;

            try
            {
                if (GetSchemaStream(
                        assembly, resourceName, ref stream,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                try
                {
                    return Validate(
                        XmlSchema.Read(stream, null), document,
                        ref error); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
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

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        public static ReturnCode GetHelp(
            XmlDocument document,
            ref string text,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            try
            {
                XmlNamespaceManager namespaceManager = null;

                if (GetNamespaceManager(
                        document.NameTable, ref namespaceManager,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                XmlNodeList nodeList = document.SelectNodes(
                    HelpXPath, namespaceManager);

                if ((nodeList == null) || (nodeList.Count == 0))
                {
                    error = "no help nodes found";
                    return ReturnCode.Error;
                }

                StringBuilder builder = StringOps.NewStringBuilder();

                foreach (XmlNode node in nodeList)
                {
                    if (node == null)
                        continue;

                    string nodeText = node.InnerText.Trim();

                    if (String.IsNullOrEmpty(nodeText))
                        continue;

                    if (builder.Length > 0)
                        builder.Append(Characters.Space);

                    builder.Append(nodeText);
                }

                if (builder.Length == 0)
                {
                    error = "no help text found";
                    return ReturnCode.Error;
                }

                text = builder.ToString();
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetNodeList(
            XmlDocument document,
            ref XmlNodeList nodeList,
            ref Result error
            )
        {
            return GetNodeList(
                document, Xml.XPathList, ref nodeList, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetNodeList(
            XmlDocument document,
            StringList xpaths,
            ref XmlNodeList nodeList,
            ref Result error
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (xpaths == null)
            {
                error = "invalid xpath list";
                return ReturnCode.Error;
            }

            try
            {
                XmlNamespaceManager namespaceManager = null;

                if (GetNamespaceManager(
                        document.NameTable, ref namespaceManager,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                foreach (string xpath in xpaths)
                {
                    if (xpath == null)
                        continue;

                    nodeList = document.SelectNodes(xpath, namespaceManager);

                    if ((nodeList == null) || (nodeList.Count == 0))
                        continue;

                    return ReturnCode.Ok;
                }

                error = String.Format(
                    "{0} xml nodes not found",
                    Xml.NamespaceName).TrimStart();
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Serialization Methods
#if SERIALIZATION
        public static ReturnCode Serialize(
            object @object,
            Type type,
            XmlSerializerNamespaces serializerNamespaces,
            ref byte[] bytes,
            ref Result error
            )
        {
            if (@object == null)
            {
                error = "invalid object";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (bytes != null)
            {
                error = "cannot overwrite valid byte array";
                return ReturnCode.Error;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    XmlSerializer serializer = new XmlSerializer(type);
                    serializer.Serialize(stream, @object, serializerNamespaces);
                    serializer = null;

                    bytes = stream.ToArray();
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Serialize(
            object @object,
            Type type,
            XmlWriter writer,
            XmlSerializerNamespaces serializerNamespaces,
            ref Result error
            )
        {
            if (@object == null)
            {
                error = "invalid object";
                return ReturnCode.Error;
            }

            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (writer == null)
            {
                error = "invalid xml writer";
                return ReturnCode.Error;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(type);
                serializer.Serialize(writer, @object, serializerNamespaces);
                serializer = null;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Deserialize(
            Type type,
            byte[] bytes,
            ref object @object,
            ref Result error
            )
        {
            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (bytes == null)
            {
                error = "invalid byte array";
                return ReturnCode.Error;
            }

            if (@object != null)
            {
                error = "cannot overwrite valid object";
                return ReturnCode.Error;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    XmlSerializer serializer = new XmlSerializer(type);
                    @object = serializer.Deserialize(stream);
                    serializer = null;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Deserialize(
            Type type,
            XmlReader reader,
            ref object @object,
            ref Result error
            )
        {
            if (type == null)
            {
                error = "invalid type";
                return ReturnCode.Error;
            }

            if (reader == null)
            {
                error = "invalid xml reader";
                return ReturnCode.Error;
            }

            if (@object != null)
            {
                error = "cannot overwrite valid object";
                return ReturnCode.Error;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(type);
                @object = serializer.Deserialize(reader);
                serializer = null;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion
    }
}
