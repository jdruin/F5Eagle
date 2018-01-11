/*
 * AttributeOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;

#if NET_40
using System.Runtime.Versioning;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("846102ad-f175-4611-b35c-1c32bbdcc227")]
    internal static class AttributeOps
    {
        #region Private Assembly Attribute Methods
        public static string GetAssemblyConfiguration(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyConfigurationAttribute), false))
                    {
                        AssemblyConfigurationAttribute configuration =
                            (AssemblyConfigurationAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyConfigurationAttribute),
                                false)[0];

                        return configuration.Configuration;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTargetFramework(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
#if NET_40
                try
                {
                    if (assembly.IsDefined(
                            typeof(TargetFrameworkAttribute), false))
                    {
                        TargetFrameworkAttribute targetFramework =
                            (TargetFrameworkAttribute)
                            assembly.GetCustomAttributes(
                                typeof(TargetFrameworkAttribute), false)[0];

                        return targetFramework.FrameworkName;
                    }
                }
                catch
                {
                    // do nothing.
                }
#elif NET_35
                return ".NETFramework,Version=v3.5";
#elif NET_20
                return ".NETFramework,Version=v2.0";
#endif
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static string GetAssemblyCopyright(
            Assembly assembly,
            bool noUnicode
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyCopyrightAttribute), false))
                    {
                        AssemblyCopyrightAttribute copyright =
                            (AssemblyCopyrightAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyCopyrightAttribute), false)[0];

                        string result = copyright.Copyright;

                        if (noUnicode && !String.IsNullOrEmpty(result))
                        {
                            result = result.Replace(
                                Characters.Copyright.ToString(),
                                Characters.CopyrightAnsi);
                        }

                        return result;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static DateTime GetAssemblyDateTime(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyDateTimeAttribute), false))
                    {
                        AssemblyDateTimeAttribute dateTime =
                            (AssemblyDateTimeAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyDateTimeAttribute), false)[0];

                        return dateTime.DateTime;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return DateTime.MinValue;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static string GetAssemblyLicense(
            Assembly assembly,
            bool summary
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyLicenseAttribute), false))
                    {
                        AssemblyLicenseAttribute license =
                            (AssemblyLicenseAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyLicenseAttribute), false)[0];

                        if (summary)
                            return license.Summary;
                        else
                            return license.Text;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyDescription(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyDescriptionAttribute), false))
                    {
                        AssemblyDescriptionAttribute description =
                            (AssemblyDescriptionAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyDescriptionAttribute), false)[0];

                        return description.Description;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region MemberInfo (Mostly Type) Attribute Methods
        public static int GetArguments(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ArgumentsAttribute), false))
                    {
                        ArgumentsAttribute arguments =
                            (ArgumentsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ArgumentsAttribute), false)[0];

                        return arguments.Arguments;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return (int)Arity.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static int GetArguments(
            object @object
            )
        {
            if (@object != null)
                return GetArguments(@object.GetType());
            else
                return (int)Arity.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static CommandFlags GetCommandFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(CommandFlagsAttribute), false))
                    {
                        CommandFlagsAttribute flags =
                            (CommandFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(CommandFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return CommandFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static CommandFlags GetCommandFlags(
            object @object
            )
        {
            if (@object != null)
                return GetCommandFlags(@object.GetType());
            else
                return CommandFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static FunctionFlags GetFunctionFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(FunctionFlagsAttribute), false))
                    {
                        FunctionFlagsAttribute flags =
                            (FunctionFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(FunctionFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return FunctionFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static FunctionFlags GetFunctionFlags(
            object @object
            )
        {
            if (@object != null)
                return GetFunctionFlags(@object.GetType());
            else
                return FunctionFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static OperatorFlags GetOperatorFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(OperatorFlagsAttribute), false))
                    {
                        OperatorFlagsAttribute flags =
                            (OperatorFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(OperatorFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return OperatorFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static OperatorFlags GetOperatorFlags(
            object @object
            )
        {
            if (@object != null)
                return GetOperatorFlags(@object.GetType());
            else
                return OperatorFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Lexeme GetLexeme(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(LexemeAttribute), false))
                    {
                        LexemeAttribute flags =
                            (LexemeAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(LexemeAttribute), false)[0];

                        return flags.Lexeme;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return Lexeme.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static Lexeme GetLexeme(
            object @object
            )
        {
            if (@object != null)
                return GetLexeme(@object.GetType());
            else
                return Lexeme.Unknown;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static TypeListFlags GetTypeListFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(TypeListFlagsAttribute), false))
                    {
                        TypeListFlagsAttribute flags =
                            (TypeListFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(TypeListFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return TypeListFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static TypeListFlags GetTypeListFlags(
            object @object
            )
        {
            if (@object != null)
                return GetTypeListFlags(@object.GetType());
            else
                return TypeListFlags.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static MethodFlags GetMethodFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(MethodFlagsAttribute), false))
                    {
                        MethodFlagsAttribute flags =
                            (MethodFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(MethodFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return MethodFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static MethodFlags GetMethodFlags(
            object @object
            )
        {
            if (@object != null)
                return GetMethodFlags(@object.GetType());
            else
                return MethodFlags.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Notifier Attribute Methods
#if NOTIFY || NOTIFY_OBJECT
        public static NotifyFlags GetNotifyFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(NotifyFlagsAttribute), false))
                    {
                        NotifyFlagsAttribute flags =
                            (NotifyFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(NotifyFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static NotifyFlags GetNotifyFlags(
            object @object
            )
        {
            if (@object != null)
                return GetNotifyFlags(@object.GetType());
            else
                return NotifyFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static NotifyType GetNotifyTypes(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(NotifyTypesAttribute), false))
                    {
                        NotifyTypesAttribute types =
                            (NotifyTypesAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(NotifyTypesAttribute), false)[0];

                        return types.Types;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return NotifyType.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static NotifyType GetNotifyTypes(
            object @object
            )
        {
            if (@object != null)
                return GetNotifyTypes(@object.GetType());
            else
                return NotifyType.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static ObjectFlags GetObjectFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectFlagsAttribute), false))
                    {
                        ObjectFlagsAttribute flags =
                            (ObjectFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ObjectFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return ObjectFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static ObjectFlags GetObjectFlags(
            object @object
            )
        {
            if (@object != null)
                return GetObjectFlags(@object.GetType());
            else
                return ObjectFlags.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static string GetObjectGroups(
            MemberInfo memberInfo,
            bool inherit,
            bool primaryOnly
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectGroupAttribute), inherit))
                    {
                        object[] attributes = memberInfo.GetCustomAttributes(
                            typeof(ObjectGroupAttribute), inherit);

                        if (attributes != null)
                        {
                            StringList list = null;

                            foreach (object attribute in attributes)
                            {
                                ObjectGroupAttribute group =
                                    attribute as ObjectGroupAttribute;

                                if (group != null)
                                {
                                    string value = group.Group;

                                    if (value != null)
                                    {
                                        if (list == null)
                                            list = new StringList();

                                        list.Add(value);

                                        if (primaryOnly)
                                            break;
                                    }
                                }
                            }

                            if (list != null)
                                return list.ToString();
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetObjectGroup(
            object @object
            )
        {
            if (@object != null)
            {
                return GetObjectGroups(@object.GetType(), true, false);
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            MemberInfo memberInfo
            )
        {
            bool defined = false;

            return GetObjectId(memberInfo, ref defined);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            MemberInfo memberInfo,
            ref bool defined
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectIdAttribute), false))
                    {
                        defined = true;

                        ObjectIdAttribute id =
                            (ObjectIdAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ObjectIdAttribute), false)[0];

                        return id.Id;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            object @object
            )
        {
            if (@object != null)
                return GetObjectId(@object.GetType());
            else
                return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetObjectName(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(ObjectNameAttribute), false))
                    {
                        ObjectNameAttribute name =
                            (ObjectNameAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(ObjectNameAttribute), false)[0];

                        return name.Name;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static string GetObjectName(
            object @object
            )
        {
            if (@object != null)
                return GetObjectName(@object.GetType());
            else
                return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static int GetOperands(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(OperandsAttribute), false))
                    {
                        OperandsAttribute operands =
                            (OperandsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(OperandsAttribute), false)[0];

                        return operands.Operands;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return (int)Arity.None;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static int GetOperands(
            object @object
            )
        {
            if (@object != null)
                return GetOperands(@object.GetType());
            else
                return (int)Arity.None;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetPluginFlags(
            MemberInfo memberInfo
            )
        {
            if (memberInfo != null)
            {
                try
                {
                    if (memberInfo.IsDefined(
                            typeof(PluginFlagsAttribute), false))
                    {
                        PluginFlagsAttribute flags =
                            (PluginFlagsAttribute)
                            memberInfo.GetCustomAttributes(
                                typeof(PluginFlagsAttribute), false)[0];

                        return flags.Flags;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return PluginFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetPluginFlags(
            object @object
            )
        {
            if (@object != null)
                return GetPluginFlags(@object.GetType());
            else
                return PluginFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ObjectId Attribute Methods
        #region Dead Code
#if DEAD_CODE
        public static StringPairList GetObjectIds(
            AppDomain appDomain,
            bool all
            )
        {
            Result error = null;

            return GetObjectIds(appDomain, all, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static StringPairList GetObjectIds(
            AppDomain appDomain,
            bool all,
            ref Result error
            )
        {
            if (appDomain != null)
            {
                try
                {
                    StringPairList list = new StringPairList();

                    foreach (Assembly assembly in appDomain.GetAssemblies())
                    {
                        if (assembly != null)
                        {
                            StringPairList list2 = GetObjectIds(
                                assembly, all, ref error);

                            if (list2 == null)
                                return null;

                            list.AddRange(list2);
                        }
                    }

                    return list;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid application domain";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringPairList GetObjectIds(
            Assembly assembly,
            bool all
            )
        {
            Result error = null;

            return GetObjectIds(assembly, all, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringPairList GetObjectIds(
            Assembly assembly,
            bool all,
            ref Result error
            )
        {
            if (assembly != null)
            {
                try
                {
                    StringPairList list = new StringPairList();

                    foreach (Type type in assembly.GetTypes()) /* throw */
                    {
                        bool defined = false;

                        Guid id = GetObjectId(type, ref defined);

                        if (all || defined || !id.Equals(Guid.Empty))
                            list.Add(id.ToString(), type.FullName);
                    }

                    return list;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid assembly";
            }

            return null;
        }
        #endregion
    }
}
