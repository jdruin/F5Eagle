/*
 * ObjectOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if DATA
using System.Data;
#endif

using System.Reflection;

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
using System.Runtime;
#endif

using System.Security.Cryptography.X509Certificates;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("21953933-d364-453c-b848-01e348a8f8ac")]
    internal static class ObjectOps
    {
        #region Private Data
        #region Type Conversion
        //
        // NOTE: *WARNING* Changes to this assembly name are considered
        //        to be a "breaking change".
        //
        private static readonly string systemSimpleName =
            AssemblyOps.GetFullName(typeof(object)); /* mscorlib */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this list are considered to be a
        //       "breaking change".
        //
        private static readonly StringList systemNamespaces = new StringList(
            new string[] {
            /* System */
            typeof(object).Namespace,
        });

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this assembly name are considered
        //        to be a "breaking change".
        //
        private static readonly string assemblySimpleName =
            GlobalState.GetAssemblyFullName(); /* Eagle */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this list are considered to be a
        //       "breaking change".
        //
        private static readonly StringList assemblyNamespaces = new StringList(
            new string[] {
            /* Eagle._Attributes */
            // typeof(AssemblyDateTimeAttribute).Namespace,

            /* Eagle._Components.Public */
            typeof(Engine).Namespace,

            /* Eagle._Containers.Public */
            typeof(ArgumentList).Namespace,

            /* Eagle._Encodings */
            // typeof(_Encodings.OneByteEncoding).Namespace,

            /* Eagle._Interfaces.Public */
            // typeof(IClientData).Namespace,
        });
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System Defaults
        #region DateTime Format & Kind
        internal static string DefaultDateTimeFormat = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Default to "unspecified" for DateTime values.  Perhaps this
        //       should be "UTC" instead?
        //
        internal static DateTimeKind DefaultDateTimeKind =
            DateTimeKind.Unspecified;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Pattern-Related Flags
        private static MatchMode DefaultMatchMode = MatchMode.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Creation
        //
        // NOTE: The default behavior for the -create / -nocreate options
        //       is controlled by these fields.
        //
        private static bool DefaultCreate = false;
        private static bool DefaultNoCreate = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reflection-Related Flags
        private static MemberTypes DefaultMemberTypes =
            MemberTypes.Field | MemberTypes.Method | MemberTypes.Property;

        ///////////////////////////////////////////////////////////////////////

        private static BindingFlags DefaultBindingFlags =
            BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.FlattenHierarchy;

        ///////////////////////////////////////////////////////////////////////

        private static BindingFlags InvokeRawBindingFlags =
            BindingFlags.InvokeMethod;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal-Related Flags
        private static LoadType DefaultLoadType = LoadType.Default;

        ///////////////////////////////////////////////////////////////////////

        private static MarshalFlags DefaultMarshalFlags =
            MarshalFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static MarshalFlags DefaultParameterMarshalFlags =
            MarshalFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static ReorderFlags DefaultReorderFlags =
            ReorderFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ByRefArgumentFlags DefaultByRefArgumentFlags =
            ByRefArgumentFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static ObjectFlags DefaultObjectFlags =
            ObjectFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ObjectFlags DefaultByRefObjectFlags =
            ObjectFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static CallbackFlags DefaultCallbackFlags =
            CallbackFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ObjectOptionType DefaultObjectOptionType =
            ObjectOptionType.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ValueFlags DefaultObjectValueFlags = ValueFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static ValueFlags DefaultMemberValueFlags = ValueFlags.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Disposal
        //
        // NOTE: Non-zero means an object should be disposed prior to it
        //       being removed fro the interpreter.
        //
        private static bool DefaultDispose = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region GC Settings
        //
        // NOTE: The default behavior was to run garbage collection after
        //       removing a managed object from the interpreter; however,
        //       that did have negative performance implications.
        //
        private static bool DefaultSynchronous = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default behavior is not to wait for pending finalizers
        //       to finish.
        //
        private static bool DefaultWaitForGC = false;

        ///////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
        //
        // NOTE: The default behavior is not to compact the large object
        //       heap; however, compacting it can be useful if many large
        //       objects are being created and finalized.
        //
        private static bool DefaultCompactLargeObjectHeap = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Flags
        //
        // NOTE: Any changes to these default option flag values will be
        //       library-wide.
        //
        private static OptionFlags AliasOptionFlags = OptionFlags.None;
        private static OptionFlags CreateOptionFlags = OptionFlags.None;
        private static OptionFlags NoCreateOptionFlags = OptionFlags.None;
        private static OptionFlags NoDisposeOptionFlags = OptionFlags.None;
        private static OptionFlags SynchronousOptionFlags = OptionFlags.None;
        private static OptionFlags TraceOptionFlags = OptionFlags.None;
        private static OptionFlags VerboseOptionFlags = OptionFlags.None;
        private static OptionFlags ArrayAsLinkOptionFlags = OptionFlags.None;
        private static OptionFlags ArrayAsValueOptionFlags = OptionFlags.None;
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region DateTime Default Settings Support Methods
        public static string GetDefaultDateTimeFormat()
        {
            return DefaultDateTimeFormat;
        }

        ///////////////////////////////////////////////////////////////////////

        public static DateTimeKind GetDefaultDateTimeKind()
        {
            return DefaultDateTimeKind;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Settings Support Methods
        public static bool GetDefaultDispose()
        {
            return DefaultDispose;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetDefaultSynchronous()
        {
            return DefaultSynchronous;
        }

        ///////////////////////////////////////////////////////////////////////

        public static BindingFlags GetDefaultBindingFlags()
        {
            return DefaultBindingFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        public static BindingFlags GetInvokeRawBindingFlags()
        {
            return InvokeRawBindingFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectFlags GetDefaultObjectFlags()
        {
            return DefaultObjectFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectOptionType GetDefaultObjectOptionType()
        {
            return DefaultObjectOptionType;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Type Support Methods
        public static IClientData GetClientData(
            object @object
            )
        {
            IGetClientData getClientData = @object as IGetClientData;

            if (getClientData != null)
                return getClientData.ClientData;
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AddSystemObjectNamespaces(
            ref StringDictionary namespaces,
            ref Result error
            )
        {
            if (namespaces == null)
                namespaces = new StringDictionary();

            if (systemNamespaces == null)
            {
                error = "system namespaces not available";
                return ReturnCode.Error;
            }

            foreach (string systemNamespace in systemNamespaces)
                if (!namespaces.ContainsKey(systemNamespace))
                    namespaces.Add(systemNamespace, systemSimpleName);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AddAssemblyObjectNamespaces(
            ref StringDictionary namespaces,
            ref Result error
            )
        {
            if (namespaces == null)
                namespaces = new StringDictionary();

            if (assemblyNamespaces == null)
            {
                error = "assembly namespaces not available";
                return ReturnCode.Error;
            }

            foreach (string assemblyNamespace in assemblyNamespaces)
                if (!namespaces.ContainsKey(assemblyNamespace))
                    namespaces.Add(assemblyNamespace, assemblySimpleName);

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Garbage Collection Support Methods
        private static bool ShouldGC()
        {
            //
            // NOTE: If this environment variable is set, the Eagle library
            //       will never manually call into the GC to have it collect
            //       garbage; otherwise, manual calls into the GC will be
            //       enabled at certain strategic points in the code where
            //       it makes sense.
            //
            if (CommonOps.Environment.DoesVariableExist(EnvVars.NeverGC))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
        private static bool ShouldCompactForGC()
        {
            //
            // NOTE: If this environment variable is set, the Eagle library
            //       will never compact the (large object?) heap; otherwise,
            //       it may be compacted when the memory load is high.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.NeverCompactForGC))
            {
                return false;
            }

#if (ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE) && NATIVE
            return CacheConfiguration.IsCompactMemoryLoadOk(CacheFlags.None);
#else
            return true;
#endif
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldWaitForGC()
        {
            //
            // NOTE: If this environment variable is set, always wait for
            //       the GC to finish the pending finalizers; otherwise,
            //       we will only wait if this is the default application
            //       domain to prevent a subtle deadlock that can seemingly
            //       occur in applications that contain a user-interface
            //       that may be running in an isolated application domain
            //       (see below).  Otherwise, if the "opposite" environment
            //       variable is set, never wait for the GC to finish the
            //       pending finalizers.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.AlwaysWaitForGC))
            {
                return true;
            }
            else if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.NeverWaitForGC))
            {
                return false;
            }
            else
            {
                //
                // BUGBUG: Only wait for pending finalizers in the default
                //         application domain (due to potential deadlocks?).
                //         This seems to be related to the cross-AppDomain
                //         marshalling in .NET wanting to obtain a lock on
                //         the GC from two threads at the same time, which
                //         results in a deadlock.  This issue was observed
                //         in a WPF application loaded into an isolated
                //         application domain; therefore, this issue may be
                //         limited to applications that contain some kind
                //         of user-interface thread.
                //
                if (AppDomainOps.IsCurrentDefault())
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void CollectGarbage(
            int generation,
            GCCollectionMode collectionMode,
            bool compact
            ) /* throw */
        {
#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            GCLargeObjectHeapCompactionMode savedLOHCompactionMode =
                GCSettings.LargeObjectHeapCompactionMode;

            if (compact)
            {
                GCSettings.LargeObjectHeapCompactionMode =
                    GCLargeObjectHeapCompactionMode.CompactOnce;
            }

            try
            {
#endif
                if (generation == -1)
                    GC.Collect();
                else
                    GC.Collect(generation, collectionMode);
#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            }
            finally
            {
                if (compact)
                {
                    GCSettings.LargeObjectHeapCompactionMode =
                        savedLOHCompactionMode;
                }
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CollectGarbage() /* throw */
        {
            CollectGarbage(GarbageFlags.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CollectGarbage(
            GarbageFlags flags
            ) /* throw */
        {
            CollectGarbage(-1, GCCollectionMode.Default, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void CollectGarbage(
            int generation,
            GCCollectionMode collectionMode,
            GarbageFlags flags
            ) /* throw */
        {
            if (FlagOps.HasFlags(flags, GarbageFlags.AlwaysCollect, true))
            {
                //
                // NOTE: Do nothing.  The garbage will be collected below.
                //
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.NeverCollect, true))
            {
                //
                // NOTE: Garbage collection has been disabled by the caller,
                //       just return now.
                //
                return;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.MaybeCollect, true))
            {
                //
                // NOTE: Attempt to automatically detect whether or not we
                //       should actually collect any garbage.
                //
                if (!ShouldGC())
                    return;
            }

            ///////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            bool reallyCompact;

            if (FlagOps.HasFlags(flags, GarbageFlags.AlwaysCompact, true))
            {
                //
                // NOTE: Yes, we should compact the (large object?) heap.
                //
                reallyCompact = true;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.NeverCompact, true))
            {
                //
                // NOTE: No, we should not compact the (large object?) heap.
                //
                reallyCompact = false;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.MaybeCompact, true))
            {
                //
                // NOTE: Attempt to automatically detect whether or not we
                //       should compact the (large object?) heap.
                //
                reallyCompact = ShouldCompactForGC();
            }
            else
            {
                //
                // NOTE: Fallback to the value configured as the default for
                //       this class.
                //
                reallyCompact = DefaultCompactLargeObjectHeap;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            bool reallyWait;

            if (FlagOps.HasFlags(flags, GarbageFlags.AlwaysWait, true))
            {
                //
                // NOTE: Yes, we should wait for all pending finalizers.
                //
                reallyWait = true;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.NeverWait, true))
            {
                //
                // NOTE: No, we should not wait for all pending finalizers.
                //
                reallyWait = false;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.MaybeWait, true))
            {
                //
                // NOTE: Attempt to automatically detect whether or not we
                //       should wait for the pending finalizers to finish.
                //
                reallyWait = ShouldWaitForGC();
            }
            else
            {
                //
                // NOTE: Fallback to the value configured as the default for
                //       this class.
                //
                reallyWait = DefaultWaitForGC;
            }

            ///////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            CollectGarbage(generation, collectionMode, reallyCompact);
#else
            CollectGarbage(generation, collectionMode, false);
#endif

            if (reallyWait)
            {
                GC.WaitForPendingFinalizers();

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
                CollectGarbage(generation, collectionMode, reallyCompact);
#else
                CollectGarbage(generation, collectionMode, false);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static void GetTotalMemory(
            bool collect,
            ref long beforeBytes,
            ref long afterBytes
            )
        {
            beforeBytes = GC.GetTotalMemory(false);

            if (collect)
                afterBytes = GC.GetTotalMemory(true);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Option Support Methods
        #region Object Option Translation Methods
        public static ObjectOptionType GetOptionType(
            bool raw,
            bool all
            )
        {
            if (all)
                return ObjectOptionType.InvokeAll;

            if (raw)
                return ObjectOptionType.InvokeRaw;

            return ObjectOptionType.Invoke;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use with the MarshalOps.FixupReturnValue and
        //       MarshalOps.FixupByRefArguments methods.
        //
        public static OptionDictionary GetInvokeOptions(
            ObjectOptionType objectOptionType
            )
        {
            //
            // NOTE: Enforce the logical union of alias option types here,
            //       via all return paths.  In this case, if more than one
            //       invoke option type is specified, the return value will
            //       be null.
            //
            objectOptionType = objectOptionType &
                ObjectOptionType.InvokeOptionMask;

            if ((objectOptionType == ObjectOptionType.Call) ||
                (objectOptionType == ObjectOptionType.Invoke) ||
                (objectOptionType == ObjectOptionType.InvokeRaw) ||
                (objectOptionType == ObjectOptionType.InvokeAll))
            {
                return GetObjectOptions(objectOptionType);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectOptionType GetByRefOptionType(
            ObjectOptionType objectOptionType,
            ByRefArgumentFlags byRefArgumentFlags
            )
        {
            //
            // NOTE: Enforce the logical union of alias option types here,
            //       via all return paths.
            //
            if (FlagOps.HasFlags(
                    byRefArgumentFlags, ByRefArgumentFlags.AliasAll, true))
            {
                return (objectOptionType &
                    ~ObjectOptionType.InvokeOptionMask) |
                    ObjectOptionType.InvokeAll;
            }
            else if (FlagOps.HasFlags(
                    byRefArgumentFlags, ByRefArgumentFlags.AliasRaw, true))
            {
                return (objectOptionType &
                    ~ObjectOptionType.InvokeOptionMask) |
                    ObjectOptionType.InvokeRaw;
            }

            return (objectOptionType &
                ~ObjectOptionType.InvokeOptionMask) | ObjectOptionType.Invoke;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Option "Factory" Methods
        //
        // NOTE: This is for the [object alias] sub-command.
        //
        public static OptionDictionary GetAliasOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is primarily for the [library call] sub-command.
        //
        public static OptionDictionary GetCallOptions()
        {
            //
            // NOTE: These options are used by both the InvokeDelegate method
            //       (below) and the code for the [library call] command.
            //       Normally, this method would simply call into a static
            //       method exported from the _Commands.Library class; however,
            //       that class is only available when the library has been
            //       compiled with native code enabled; therefore, we define
            //       the actual options here and both the _Commands.Library
            //       class and the InvokeDelegate method can simply call us.
            //
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimekind",
                    new Variant(DefaultDateTimeKind)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-datetimeformat",
                    new Variant(DefaultDateTimeFormat)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinvoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noargs", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, ArrayAsValueOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayasvalue", null),
                new Option(null, ArrayAsLinkOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayaslink", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-default", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictargs", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, TraceOptionFlags, Index.Invalid,
                    Index.Invalid, "-trace", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobyref", null),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-byrefobjectflags",
                    new Variant(DefaultByRefObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the ToCommandCallback method.
        //
        public static OptionDictionary GetCallbackOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-returntype", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumListValue, Index.Invalid,
                    Index.Invalid, "-parametermarshalflags",
                    new Variant(DefaultParameterMarshalFlags)),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-objectflags", new Variant(DefaultObjectFlags)),
                new Option(typeof(CallbackFlags), OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-callbackflags", new Variant(DefaultCallbackFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [library certificate] and
        //       [object certificate] sub-commands.
        //
        public static OptionDictionary GetCertificateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-chain", null),
                new Option(typeof(X509VerificationFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-verificationflags",
                    new Variant(CertificateOps.DefaultVerificationFlags)),
                new Option(typeof(X509RevocationMode),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-revocationmode",
                    new Variant(CertificateOps.DefaultRevocationMode)),
                new Option(typeof(X509RevocationFlag),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-revocationflag",
                    new Variant(CertificateOps.DefaultRevocationFlag)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object cleanup] sub-command.
        //
        public static OptionDictionary GetCleanupOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-references", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noremove", null),
                new Option(null, SynchronousOptionFlags, Index.Invalid,
                    Index.Invalid, "-synchronous", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object create] sub-command.
        //
        public static OptionDictionary GetCreateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue,
                    Index.Invalid, Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-methodtypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumListValue, Index.Invalid,
                    Index.Invalid, "-parametermarshalflags",
                    new Variant(DefaultParameterMarshalFlags)),
                new Option(null, TraceOptionFlags, Index.Invalid,
                    Index.Invalid, "-trace", null),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(typeof(ReorderFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-reorderflags",
                    new Variant(DefaultReorderFlags)),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinvoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noargs", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, ArrayAsValueOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayasvalue", null),
                new Option(null, ArrayAsLinkOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayaslink", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictargs", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-default", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobyref", null),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-byrefobjectflags",
                    new Variant(DefaultByRefObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object declare] sub-command.
        //
        public static OptionDictionary GetDeclareOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenonpublic", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-declaremode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-declarepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if CALLBACK_QUEUE
        //
        // NOTE: This is for the [callback dequeue] sub-command.
        //
        public static OptionDictionary GetDequeueOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        //
        // NOTE: This is for the [xml deserialize] sub-command.
        //
        public static OptionDictionary GetDeserializeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object dispose] sub-command.
        //
        public static OptionDictionary GetDisposeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, SynchronousOptionFlags, Index.Invalid,
                    Index.Invalid, "-synchronous", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        //
        // NOTE: This is for the [tcl eval] sub-command.
        //
        public static OptionDictionary GetEvaluateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-exceptions", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if PREVIOUS_RESULT
        //
        // NOTE: This is for the [debug exception] sub-command.
        //
        public static OptionDictionary GetExceptionOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DATA
        //
        // NOTE: This is for the [sql execute] sub-command.
        //
        public static OptionDictionary GetExecuteOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbatim", null),
                new Option(typeof(DateTimeBehavior),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimebehavior",
                    new Variant(DateTimeBehavior.Default)),
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimekind",
                    new Variant(DefaultDateTimeKind)),
                new Option(typeof(ValueFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-valueflags",
                    new Variant(ValueFlags.AnyNonCharacter)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-valueformat", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-datetimeformat",
                    new Variant(DefaultDateTimeFormat)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-transaction", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-rowsvar", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-nested", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-allownull", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-nullvalue", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-pairs", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-names", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-timevar", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(typeof(CommandType), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-commandtype", null),
                new Option(typeof(DbResultFormat),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-format", null),
                new Option(typeof(DbExecuteType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-execute", null),
                new Option(typeof(CommandBehavior),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-behavior",
                    new Variant(CommandBehavior.Default)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-objecttype", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use with the MarshalOps.FixupReturnValue and/or
        //       Utility.FixupReturnValue methods.
        //
        public static OptionDictionary GetFixupReturnValueOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-returntype", null),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-objectflags", new Variant(DefaultObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object foreach] sub-command.
        //
        public static OptionDictionary GetForEachOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, SynchronousOptionFlags, Index.Invalid,
                    Index.Invalid, "-synchronous", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-collect", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object get] sub-command.
        //
        public static OptionDictionary GetGetOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object import] sub-command.
        //
        public static OptionDictionary GetImportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-system", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-assembly", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-container", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-importmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-importpattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invoke] sub-command.
        //
        public static OptionDictionary GetInvokeOptions()
        {
            //
            // NOTE: The reason this is defined here is because it must be
            //       used anywhere that can create an alias that refers to
            //       this command (i.e. an "object alias").  Currently, this
            //       includes the [library] command, the [debug] command,
            //       and the custom event handling mechanism as well as the
            //       [object] command itself, specifically the "load",
            //       "create", and "invoke" sub-commands.
            //
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimekind",
                    new Variant(DefaultDateTimeKind)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-datetimeformat",
                    new Variant(DefaultDateTimeFormat)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-type", null),
                new Option(null, OptionFlags.MustHaveTypeValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-returntype", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-methodtypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumListValue, Index.Invalid,
                    Index.Invalid, "-parametermarshalflags",
                    new Variant(DefaultParameterMarshalFlags)),
                new Option(null, TraceOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-trace", null),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(typeof(ReorderFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-reorderflags",
                    new Variant(DefaultReorderFlags)),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinvoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noargs", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-invoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-invokeraw", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-chained", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-lastresult", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-keepresults", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-invokeall", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, ArrayAsValueOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayasvalue", null),
                new Option(null, ArrayAsLinkOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayaslink", null),
                /* FIXME: Unsafe? */
                new Option(null, VerboseOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid,Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-default", null),
                new Option(typeof(ValueFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectvalueflags",
                    new Variant(DefaultObjectValueFlags)),
                new Option(typeof(ValueFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-membervalueflags",
                    new Variant(DefaultMemberValueFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonestedobject", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonestedmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictargs", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobyref", null),
                new Option(typeof(MemberTypes), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-membertypes",
                    new Variant(DefaultMemberTypes)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-objectflags", new Variant(DefaultObjectFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-byrefobjectflags", new Variant(DefaultByRefObjectFlags)),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-identity", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-typeidentity", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invokeall] sub-command.
        //
        public static OptionDictionary GetInvokeAllOptions()
        {
            OptionDictionary options = GetInvokeOptions();

            options["-invoke"].Flags &= ~OptionFlags.Ignored;
            options["-invokeraw"].Flags &= ~OptionFlags.Ignored;
            options["-invokeall"].Flags |= OptionFlags.Ignored;

            options["-chained"].Flags &= ~OptionFlags.Ignored;
            options["-lastresult"].Flags &= ~OptionFlags.Ignored;
            options["-keepresults"].Flags &= ~OptionFlags.Ignored;
            options["-nocomplain"].Flags &= ~OptionFlags.Ignored;

            return options;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invokeraw] sub-command.
        //
        public static OptionDictionary GetInvokeRawOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimekind",
                    new Variant(DefaultDateTimeKind)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-datetimeformat",
                    new Variant(DefaultDateTimeFormat)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-type", null),
                new Option(null, OptionFlags.MustHaveTypeValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-returntype", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-methodtypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumListValue, Index.Invalid,
                    Index.Invalid, "-parametermarshalflags",
                    new Variant(DefaultParameterMarshalFlags)),
                new Option(null, TraceOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-trace", null),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinvoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noargs", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-invoke", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-invokeraw", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-chained", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-lastresult", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-keepresults", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-invokeall", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, ArrayAsValueOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayasvalue", null),
                new Option(null, ArrayAsLinkOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayaslink", null),
                /* FIXME: Unsafe? */
                new Option(null, VerboseOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid,Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-default", null),
                new Option(typeof(ValueFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectvalueflags",
                    new Variant(DefaultObjectValueFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonestedobject", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobyref", null),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-objectflags", new Variant(DefaultObjectFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-byrefobjectflags", new Variant(DefaultByRefObjectFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object isoftype] sub-command.
        //
        public static OptionDictionary GetIsOfTypeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-assignable", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object load] sub-command.
        //
        public static OptionDictionary GetLoadOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveRelativeNamespaceValue,
                    Index.Invalid, Index.Invalid, "-namespace", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-import", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnonpublic", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-importmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-importpattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declare", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenonpublic", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-declaremode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-declarepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenocase", null),
                new Option(typeof(LoadType), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-loadtype",
                    new Variant(DefaultLoadType)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags | ObjectFlags.Assembly)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object members] sub-command.
        //
        public static OptionDictionary GetMembersOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-mode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-pattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-attributes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-signatures", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-qualified", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-matchnameonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nameonly", null),
                new Option(typeof(MemberTypes), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-membertypes",
                    new Variant(DefaultMemberTypes)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object search] sub-command.
        //
        public static OptionDictionary GetSearchOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noshowname", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonamespace", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noassembly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noexception", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-fullname", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        //
        // NOTE: This is for the [xml serialize] sub-command.
        //
        public static OptionDictionary GetSerializeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object type] and [object untype]
        //       sub-commands.
        //
        public static OptionDictionary GetTypeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-typemode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-typepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-typenocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object unaliasnamespace] sub-command.
        //
        public static OptionDictionary GetUnaliasNamespaceOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bycontainer", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-aliasmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-aliaspattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasnocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object undeclare] sub-command.
        //
        public static OptionDictionary GetUndeclareOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bycontainer", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-declaremode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-declarepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object unimport] sub-command.
        //
        public static OptionDictionary GetUnimportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bycontainer", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-importmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-importpattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, Option.EndOfOptions, null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        public static OptionDictionary GetObjectOptions(
            ObjectOptionType objectOptionType
            )
        {
            switch (objectOptionType)
            {
                case ObjectOptionType.Alias:             // [object alias]
                    return GetAliasOptions();            //
                case ObjectOptionType.Call:              // [library call]
                    return GetCallOptions();             //
                case ObjectOptionType.Callback:          // ToCommandCallback
                    return GetCallbackOptions();         //
                case ObjectOptionType.Certificate:       // [object certificate]
                    return GetCertificateOptions();      //
                case ObjectOptionType.Cleanup:           // [object cleanup]
                    return GetCleanupOptions();          //
                case ObjectOptionType.Create:            // [object create]
                    return GetCreateOptions();           //
                case ObjectOptionType.Declare:           // [object declare]
                    return GetDeclareOptions();          //
#if CALLBACK_QUEUE                                       //
                case ObjectOptionType.Dequeue:           // [callback dequeue]
                    return GetDequeueOptions();          //
#endif                                                   //
#if XML && SERIALIZATION                                 //
                case ObjectOptionType.Deserialize:       // [xml deserialize]
                    return GetDeserializeOptions();      //
#endif                                                   //
                case ObjectOptionType.Dispose:           // [object dispose]
                    return GetDisposeOptions();          //
#if NATIVE && TCL
                case ObjectOptionType.Evaluate:          // [tcl eval]
                    return GetEvaluateOptions();         //
#endif                                                   //
#if PREVIOUS_RESULT                                      //
                case ObjectOptionType.Exception:         // [debug exception]
                    return GetExceptionOptions();        //
#endif                                                   //
#if DATA                                                 //
                case ObjectOptionType.Execute:           // [sql execute]
                    return GetExecuteOptions();          //
#endif                                                   //
                case ObjectOptionType.FireCallback:      // CommandCallback
                    return null;                         // N/A
                case ObjectOptionType.FixupReturnValue:  // MarshalOps
                    return GetFixupReturnValueOptions(); //
                case ObjectOptionType.ForEach:           // [object foreach]
                    return GetForEachOptions();          //
                case ObjectOptionType.Get:               // [object get]
                    return GetGetOptions();              //
                case ObjectOptionType.Import:            // [object import]
                    return GetImportOptions();           //
                case ObjectOptionType.Invoke:            // [object invoke]
                    return GetInvokeOptions();           //
                case ObjectOptionType.InvokeAll:         // [object invokeall]
                    return GetInvokeAllOptions();        //
                case ObjectOptionType.InvokeRaw:         // [object invokeraw]
                    return GetInvokeRawOptions();        //
                case ObjectOptionType.IsOfType:          // [object isoftype]
                    return GetIsOfTypeOptions();         //
                case ObjectOptionType.Load:              // [object load]
                    return GetLoadOptions();             //
                case ObjectOptionType.Members:           // [object members]
                    return GetMembersOptions();          //
                case ObjectOptionType.Search:            // [object search]
                    return GetSearchOptions();           //
#if XML && SERIALIZATION                                 //
                case ObjectOptionType.Serialize:         // [xml serialize]
                    return GetSerializeOptions();        //
#endif                                                   //
                case ObjectOptionType.Type:              // [object type]
                    return GetTypeOptions();             //
                case ObjectOptionType.UnaliasNamespace:  // [object unaliasnamespace]
                    return GetUnaliasNamespaceOptions(); //
                case ObjectOptionType.Undeclare:         // [object undeclare]
                    return GetUndeclareOptions();        //
                case ObjectOptionType.Unimport:          // [object unimport]
                    return GetUnimportOptions();         //
                case ObjectOptionType.Untype:            // [object untype]
                    return GetTypeOptions();             //
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Option Processing Methods
        //
        // NOTE: For use by the ConversionOps.Dynamic._ToString.FromDateTime
        //       method only.
        //
        public static void ProcessDateTimeOptions(
            Interpreter interpreter,
            OptionDictionary options,
            string defaultDateTimeFormat,
            out string dateTimeFormat
            )
        {
            DateTimeKind dateTimeKind;

            ProcessDateTimeOptions(
                interpreter, options, null, defaultDateTimeFormat,
                out dateTimeKind, out dateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessDateTimeOptions(
            Interpreter interpreter,
            OptionDictionary options,
            DateTimeKind? defaultDateTimeKind,
            string defaultDateTimeFormat,
            out DateTimeKind dateTimeKind,
            out string dateTimeFormat
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            dateTimeKind = (defaultDateTimeKind != null) ?
                (DateTimeKind)defaultDateTimeKind : DefaultDateTimeKind;

            if ((options != null) &&
                options.CheckPresent("-datetimekind", ref value))
            {
                dateTimeKind = (DateTimeKind)value.Value;
            }
            else if (interpreter != null)
            {
                dateTimeKind = interpreter.DateTimeKind;
            }

            ///////////////////////////////////////////////////////////////////

            dateTimeFormat = (defaultDateTimeFormat != null) ?
                defaultDateTimeFormat : DefaultDateTimeFormat;

            if ((options != null) &&
                options.CheckPresent("-datetimeformat", ref value))
            {
                dateTimeFormat = value.ToString();
            }
            else if (interpreter != null)
            {
                dateTimeFormat = interpreter.DateTimeFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessCallbackOptions(
            Interpreter interpreter,
            OptionDictionary options,
            MarshalFlags? defaultMarshalFlags,
            ObjectFlags? defaultObjectFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            CallbackFlags? defaultCallbackFlags,
            out Type returnType,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out MarshalFlags marshalFlags,
            out ObjectFlags objectFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out CallbackFlags callbackFlags
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            returnType = null;

            if ((options != null) &&
                options.CheckPresent("-returntype", ref value))
            {
                returnType = (Type)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterTypes = null;

            if ((options != null) &&
                options.CheckPresent("-parametertypes", ref value))
            {
                parameterTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterMarshalFlags = null;

            if ((options != null) &&
                options.CheckPresent("-parametermarshalflags", ref value))
            {
                parameterMarshalFlags = MarshalOps.GetParameterMarshalFlags(
                    (EnumList)value.Value);
            }

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if ((options != null) &&
                options.CheckPresent("-marshalflags", ref value))
            {
                marshalFlags = (MarshalFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectFlags = (defaultObjectFlags != null) ?
                (ObjectFlags)defaultObjectFlags : DefaultObjectFlags;

            if ((options != null) &&
                options.CheckPresent("-objectflags", ref value))
            {
                objectFlags = (ObjectFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            byRefArgumentFlags = (defaultByRefArgumentFlags != null) ?
                (ByRefArgumentFlags)defaultByRefArgumentFlags :
                DefaultByRefArgumentFlags;

            if ((options != null) &&
                options.CheckPresent("-argumentflags", ref value))
            {
                byRefArgumentFlags = (ByRefArgumentFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            callbackFlags = (defaultCallbackFlags != null) ?
                (CallbackFlags)defaultCallbackFlags : DefaultCallbackFlags;

            if ((options != null) &&
                options.CheckPresent("-callbackflags", ref value))
            {
                callbackFlags = (CallbackFlags)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool strictMember,
            out bool strictArgs,
            out bool invoke,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            TypeList objectTypes;
            TypeList methodTypes;
            TypeList parameterTypes;
            MarshalFlagsList parameterMarshalFlags;
            ValueFlags objectValueTypes;
            ValueFlags memberValueTypes;
            MemberTypes memberTypes;
            bool strictType;
            bool identity;
            bool typeIdentity;
            bool noNestedObject;
            bool noNestedMember;
            bool noCase;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, null, null, null, defaultBindingFlags,
                defaultMarshalFlags, defaultReorderFlags,
                defaultByRefArgumentFlags, out objectTypes, out methodTypes,
                out parameterTypes, out parameterMarshalFlags,
                out objectValueTypes, out memberValueTypes, out memberTypes,
                out bindingFlags, out marshalFlags, out reorderFlags,
                out byRefArgumentFlags, out limit, out index, out noByRef,
                out strictType, out strictMember, out strictArgs,
                out identity, out typeIdentity, out noNestedObject,
                out noNestedMember, out noCase, out invoke, out noArgs,
                out arrayAsValue, out arrayAsLink, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool strictType,
            out bool strictMember,
            out bool strictArgs,
            out bool noCase,
            out bool invoke,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            ValueFlags objectValueTypes;
            ValueFlags memberValueTypes;
            MemberTypes memberTypes;
            bool identity;
            bool typeIdentity;
            bool noNestedObject;
            bool noNestedMember;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, null, null, null, defaultBindingFlags,
                defaultMarshalFlags, defaultReorderFlags,
                defaultByRefArgumentFlags, out objectTypes, out methodTypes,
                out parameterTypes, out parameterMarshalFlags,
                out objectValueTypes, out memberValueTypes, out memberTypes,
                out bindingFlags, out marshalFlags, out reorderFlags,
                out byRefArgumentFlags, out limit, out index, out noByRef,
                out strictType, out strictMember, out strictArgs, out identity,
                out typeIdentity, out noNestedObject, out noNestedMember,
                out noCase, out invoke, out noArgs, out arrayAsValue,
                out arrayAsLink, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool strictType,
            out bool strictMember,
            out bool strictArgs,
            out bool identity,
            out bool typeIdentity,
            out bool noNestedObject,
            out bool noNestedMember,
            out bool noCase,
            out bool invoke,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            bool verbose;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, defaultObjectValueFlags,
                defaultMemberValueFlags, defaultMemberTypes,
                defaultBindingFlags, defaultMarshalFlags, defaultReorderFlags,
                defaultByRefArgumentFlags, out objectTypes, out methodTypes,
                out parameterTypes, out parameterMarshalFlags,
                out objectValueFlags, out memberValueFlags, out memberTypes,
                out bindingFlags, out marshalFlags, out reorderFlags,
                out byRefArgumentFlags, out limit, out index, out noByRef,
                out verbose, out strictType, out strictMember, out strictArgs,
                out identity, out typeIdentity, out noNestedObject,
                out noNestedMember, out noCase, out invoke, out noArgs,
                out arrayAsValue, out arrayAsLink, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool verbose,
            out bool strictType,
            out bool strictMember,
            out bool strictArgs,
            out bool identity,
            out bool typeIdentity,
            out bool noNestedObject,
            out bool noNestedMember,
            out bool noCase,
            out bool invoke,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            noByRef = false;

            if ((options != null) && options.CheckPresent("-nobyref"))
                noByRef = true;

            ///////////////////////////////////////////////////////////////////

            verbose = false;

            if ((options != null) && options.CheckPresent("-verbose"))
                verbose = true;

            ///////////////////////////////////////////////////////////////////

            strictType = false;

            if ((options != null) && options.CheckPresent("-stricttype"))
                strictType = true;

            ///////////////////////////////////////////////////////////////////

            strictMember = false;

            if ((options != null) && options.CheckPresent("-strictmember"))
                strictMember = true;

            ///////////////////////////////////////////////////////////////////

            strictArgs = false;

            if ((options != null) && options.CheckPresent("-strictargs"))
                strictArgs = true;

            ///////////////////////////////////////////////////////////////////

            identity = false;

            if ((options != null) && options.CheckPresent("-identity"))
                identity = true;

            ///////////////////////////////////////////////////////////////////

            typeIdentity = false;

            if ((options != null) && options.CheckPresent("-typeidentity"))
                typeIdentity = true;

            ///////////////////////////////////////////////////////////////////

            noNestedObject = false;

            if ((options != null) && options.CheckPresent("-nonestedobject"))
                noNestedObject = true;

            ///////////////////////////////////////////////////////////////////

            noNestedMember = false;

            if ((options != null) && options.CheckPresent("-nonestedmember"))
                noNestedMember = true;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Now check for and use the -nocase value.  It is also
            //       important to note here that a specifying the binding
            //       flags does not override this setting.
            //
            noCase = false;

            if ((options != null) && options.CheckPresent("-nocase"))
                noCase = true;

            ///////////////////////////////////////////////////////////////////

            invoke = true;

            if ((options != null) && options.CheckPresent("-noinvoke"))
                invoke = false;

            ///////////////////////////////////////////////////////////////////

            noArgs = false;

            if ((options != null) && options.CheckPresent("-noargs"))
                noArgs = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsValue = false;

            if ((options != null) && options.CheckPresent("-arrayasvalue"))
                arrayAsValue = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsLink = false;

            if ((options != null) && options.CheckPresent("-arrayaslink"))
                arrayAsLink = true;

            ///////////////////////////////////////////////////////////////////

            trace = false;

            if ((options != null) && options.CheckPresent("-trace"))
                trace = true;

            ///////////////////////////////////////////////////////////////////

            objectValueFlags = (defaultObjectValueFlags != null) ?
                (ValueFlags)defaultObjectValueFlags : DefaultObjectValueFlags;

            if ((options != null) &&
                options.CheckPresent("-objectvalueflags", ref value))
            {
                objectValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            memberValueFlags = (defaultMemberValueFlags != null) ?
                (ValueFlags)defaultMemberValueFlags : DefaultMemberValueFlags;

            if ((options != null) &&
                options.CheckPresent("-membervalueflags", ref value))
            {
                memberValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            ProcessReflectionOptions(
                options, defaultMemberTypes, defaultBindingFlags,
                out memberTypes, out bindingFlags);

            //
            // NOTE: Now check for and use the -nocase value.  It is also
            //       important to note here that a specifying the binding
            //       flags does not override this setting.
            //
            if (noCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            //
            // NOTE: Now check for and use the -identity and -typeidentity
            //       values.  It is also important to note here that a
            //       specifying the binding flags does not override this
            //       setting.
            //
            if (identity || typeIdentity)
            {
                //
                // NOTE: These flags are needed because of the precise
                //       signature of the "HandleOps.Identity" method.
                //
                bindingFlags |= MarshalOps.PublicStaticMethodBindingFlags;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: For "safe" interpreters, prevent the use of "unsafe"
            //       member types and binding flags (e.g. NonPublic, etc).
            //
            if ((interpreter != null) && interpreter.IsSafe())
            {
                memberTypes &= ~MarshalOps.UnsafeObjectMemberTypes;
                bindingFlags &= ~MarshalOps.UnsafeObjectBindingFlags;
            }

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if (options != null)
            {
                if (options.CheckPresent("-marshalflags", ref value))
                    marshalFlags = (MarshalFlags)value.Value;

                if (options.CheckPresent("-default"))
                    marshalFlags |= MarshalFlags.DefaultValue;
            }

            if (noByRef)
                marshalFlags |= MarshalFlags.NoByRefArguments;

            if (verbose)
                marshalFlags |= MarshalFlags.Verbose;

            if (arrayAsValue)
                marshalFlags |= MarshalFlags.SkipNullSetupValue;

            ///////////////////////////////////////////////////////////////////

            reorderFlags = (defaultReorderFlags != null) ?
                (ReorderFlags)defaultReorderFlags : DefaultReorderFlags;

            if ((options != null) &&
                options.CheckPresent("-reorderflags", ref value))
            {
                reorderFlags = (ReorderFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            byRefArgumentFlags = (defaultByRefArgumentFlags != null) ?
                (ByRefArgumentFlags)defaultByRefArgumentFlags :
                DefaultByRefArgumentFlags;

            if ((options != null) &&
                options.CheckPresent("-argumentflags", ref value))
            {
                byRefArgumentFlags = (ByRefArgumentFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectTypes = null;

            if ((options != null) &&
                options.CheckPresent("-objecttypes", ref value))
            {
                objectTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            methodTypes = null;

            if ((options != null) &&
                options.CheckPresent("-methodtypes", ref value))
            {
                methodTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterTypes = null;

            if ((options != null) &&
                options.CheckPresent("-parametertypes", ref value))
            {
                parameterTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterMarshalFlags = null;

            if ((options != null) &&
                options.CheckPresent("-parametermarshalflags", ref value))
            {
                parameterMarshalFlags = MarshalOps.GetParameterMarshalFlags(
                    (EnumList)value.Value);
            }

            ///////////////////////////////////////////////////////////////////

            limit = invoke ? 1 : 0;

            if ((options != null) && options.CheckPresent("-limit", ref value))
                limit = (int)value.Value;

            ///////////////////////////////////////////////////////////////////

            index = Index.Invalid;

            if ((options != null) && options.CheckPresent("-index", ref value))
                index = (int)value.Value;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference
            )
        {
            ObjectFlags byRefObjectFlags;
            Type returnType;
            bool create;
            bool dispose;
            bool toString;

            ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, null, out returnType,
                out objectFlags, out byRefObjectFlags, out objectName,
                out interpName, out create, out dispose, out alias,
                out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            ObjectFlags byRefObjectFlags;

            ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, null, out returnType,
                out objectFlags, out byRefObjectFlags, out objectName,
                out interpName, out create, out dispose, out alias,
                out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            ObjectFlags? defaultByRefObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out ObjectFlags byRefObjectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            returnType = null;

            if (options != null)
            {
                if (options.Has("-objecttype"))
                {
                    //
                    // NOTE: For example, [sql execute]...
                    //
                    if (options.CheckPresent("-objecttype", ref value))
                        returnType = (Type)value.Value;
                }
                else if (options.Has("-returntype"))
                {
                    //
                    // NOTE: For example, [object invoke]...
                    //
                    if (options.CheckPresent("-returntype", ref value))
                        returnType = (Type)value.Value;
                }
                else
                {
                    //
                    // NOTE: For example, [callback dequeue],
                    //       InvokeDelegate()...
                    //
                    if (options.CheckPresent("-type", ref value))
                        returnType = (Type)value.Value;
                }
            }

            ///////////////////////////////////////////////////////////////////

            objectFlags = (defaultObjectFlags != null) ?
                (ObjectFlags)defaultObjectFlags : DefaultObjectFlags;

            if (options != null)
            {
                if (options.CheckPresent("-objectflags", ref value))
                    objectFlags = (ObjectFlags)value.Value;

                if (options.CheckPresent("-noforcedelete"))
                    objectFlags &= ~ObjectFlags.ForceDelete;
            }

            ///////////////////////////////////////////////////////////////////

            byRefObjectFlags = (defaultByRefObjectFlags != null) ?
                (ObjectFlags)defaultByRefObjectFlags : DefaultByRefObjectFlags;

            if (options != null)
            {
                if (options.CheckPresent("-byrefobjectflags", ref value))
                    byRefObjectFlags = (ObjectFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectName = null;

            if ((options != null) &&
                options.CheckPresent("-objectname", ref value))
            {
                objectName = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            interpName = null;

#if NATIVE && TCL
            if ((options != null) && options.CheckPresent("-tcl", ref value))
                interpName = value.ToString();
#endif

            ///////////////////////////////////////////////////////////////////

            if (options != null)
            {
                if (options.Has("-nocreate"))
                {
                    create = DefaultNoCreate;

                    if (options.CheckPresent("-nocreate"))
                        create = false;
                }
                else
                {
                    create = DefaultCreate;

                    if (options.CheckPresent("-create"))
                        create = true;
                }
            }
            else
            {
                create = DefaultCreate;
            }

            ///////////////////////////////////////////////////////////////////

            dispose = true;

            if ((options != null) && options.CheckPresent("-nodispose"))
                dispose = false;

            ///////////////////////////////////////////////////////////////////

            alias = false;

            if ((options != null) && options.CheckPresent("-alias"))
                alias = true;

            ///////////////////////////////////////////////////////////////////

            aliasRaw = false;

            if ((options != null) && options.CheckPresent("-aliasraw"))
                aliasRaw = true;

            ///////////////////////////////////////////////////////////////////

            aliasAll = false;

            if ((options != null) && options.CheckPresent("-aliasall"))
                aliasAll = true;

            ///////////////////////////////////////////////////////////////////

            aliasReference = false;

            if ((options != null) && options.CheckPresent("-aliasreference"))
                aliasReference = true;

            ///////////////////////////////////////////////////////////////////

            toString = false;

            if ((options != null) && options.CheckPresent("-tostring"))
                toString = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessGetTypeOptions(
            OptionDictionary options,
            out bool verbose,
            out bool strictType,
            out bool noCase
            )
        {
            TypeList objectTypes;

            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType,
                out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessGetTypeOptions(
            OptionDictionary options,
            out TypeList objectTypes,
            out bool verbose,
            out bool strictType
            )
        {
            bool noCase;

            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType,
                out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessGetTypeOptions(
            OptionDictionary options,
            out TypeList objectTypes,
            out bool verbose,
            out bool strictType,
            out bool noCase
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            verbose = false;

            if ((options != null) && options.CheckPresent("-verbose"))
                verbose = true;

            ///////////////////////////////////////////////////////////////////

            strictType = false;

            if ((options != null) && options.CheckPresent("-stricttype"))
                strictType = true;

            ///////////////////////////////////////////////////////////////////

            noCase = false;

            if ((options != null) && options.CheckPresent("-nocase"))
                noCase = true;

            ///////////////////////////////////////////////////////////////////

            objectTypes = null;

            if ((options != null) &&
                options.CheckPresent("-objecttypes", ref value))
            {
                objectTypes = (TypeList)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessMarshalOptions(
            OptionDictionary options,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out Type type,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out bool noByRef,
            out bool verbose,
            out bool strictType,
            out bool strictArgs,
            out bool noNestedObject,
            out bool noCase,
            out bool invoke,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            noByRef = false;

            if ((options != null) && options.CheckPresent("-nobyref"))
                noByRef = true;

            ///////////////////////////////////////////////////////////////////

            verbose = false;

            if ((options != null) && options.CheckPresent("-verbose"))
                verbose = true;

            ///////////////////////////////////////////////////////////////////

            strictType = false;

            if ((options != null) && options.CheckPresent("-stricttype"))
                strictType = true;

            ///////////////////////////////////////////////////////////////////

            strictArgs = false;

            if ((options != null) && options.CheckPresent("-strictargs"))
                strictArgs = true;

            ///////////////////////////////////////////////////////////////////

            noNestedObject = false;

            if ((options != null) && options.CheckPresent("-nonestedobject"))
                noNestedObject = true;

            ///////////////////////////////////////////////////////////////////

            noCase = false;

            if ((options != null) && options.CheckPresent("-nocase"))
                noCase = true;

            ///////////////////////////////////////////////////////////////////

            invoke = true;

            if ((options != null) && options.CheckPresent("-noinvoke"))
                invoke = false;

            ///////////////////////////////////////////////////////////////////

            noArgs = false;

            if ((options != null) && options.CheckPresent("-noargs"))
                noArgs = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsValue = false;

            if ((options != null) && options.CheckPresent("-arrayasvalue"))
                arrayAsValue = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsLink = false;

            if ((options != null) && options.CheckPresent("-arrayaslink"))
                arrayAsLink = true;

            ///////////////////////////////////////////////////////////////////

            trace = false;

            if ((options != null) && options.CheckPresent("-trace"))
                trace = true;

            ///////////////////////////////////////////////////////////////////

            objectValueFlags = (defaultObjectValueFlags != null) ?
                (ValueFlags)defaultObjectValueFlags : DefaultObjectValueFlags;

            if ((options != null) &&
                options.CheckPresent("-objectvalueflags", ref value))
            {
                objectValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            memberValueFlags = (defaultMemberValueFlags != null) ?
                (ValueFlags)defaultMemberValueFlags : DefaultMemberValueFlags;

            if ((options != null) &&
                options.CheckPresent("-membervalueflags", ref value))
            {
                memberValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            ProcessReflectionOptions(
                options, defaultMemberTypes, defaultBindingFlags,
                out memberTypes, out bindingFlags);

            //
            // NOTE: Now check for and use the -nocase value.  It is also
            //       important to note here that a specifying the binding
            //       flags does not override this setting.
            //
            if (noCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            ///////////////////////////////////////////////////////////////////

            byRefArgumentFlags = (defaultByRefArgumentFlags != null) ?
                (ByRefArgumentFlags)defaultByRefArgumentFlags :
                DefaultByRefArgumentFlags;

            if ((options != null) &&
                options.CheckPresent("-argumentflags", ref value))
            {
                byRefArgumentFlags = (ByRefArgumentFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            reorderFlags = (defaultReorderFlags != null) ?
                (ReorderFlags)defaultReorderFlags : DefaultReorderFlags;

            if ((options != null) &&
                options.CheckPresent("-reorderflags", ref value))
            {
                reorderFlags = (ReorderFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if (options != null)
            {
                if (options.CheckPresent("-marshalflags", ref value))
                    marshalFlags = (MarshalFlags)value.Value;

                if (options.CheckPresent("-default"))
                    marshalFlags |= MarshalFlags.DefaultValue;
            }

            if (noByRef)
                marshalFlags |= MarshalFlags.NoByRefArguments;

            if (verbose)
                marshalFlags |= MarshalFlags.Verbose;

            if (arrayAsValue)
                marshalFlags |= MarshalFlags.SkipNullSetupValue;

            ///////////////////////////////////////////////////////////////////

            type = null;

            if ((options != null) && options.CheckPresent("-type", ref value))
                type = (Type)value.Value;

            ///////////////////////////////////////////////////////////////////

            objectTypes = null;

            if ((options != null) &&
                options.CheckPresent("-objecttypes", ref value))
            {
                objectTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            methodTypes = null;

            if ((options != null) &&
                options.CheckPresent("-methodtypes", ref value))
            {
                methodTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterTypes = null;

            if ((options != null) &&
                options.CheckPresent("-parametertypes", ref value))
            {
                parameterTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterMarshalFlags = null;

            if ((options != null) &&
                options.CheckPresent("-parametermarshalflags", ref value))
            {
                parameterMarshalFlags = MarshalOps.GetParameterMarshalFlags(
                    (EnumList)value.Value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectAliasOptions(
            OptionDictionary options,
            out TypeList objectTypes,
            out bool verbose,
            out bool strictType,
            out bool noCase,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference
            )
        {
            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType,
                out noCase);

            ///////////////////////////////////////////////////////////////////

            aliasRaw = false;

            if ((options != null) && options.CheckPresent("-aliasraw"))
                aliasRaw = true;

            ///////////////////////////////////////////////////////////////////

            aliasAll = false;

            if ((options != null) && options.CheckPresent("-aliasall"))
                aliasAll = true;

            ///////////////////////////////////////////////////////////////////

            aliasReference = false;

            if ((options != null) && options.CheckPresent("-aliasreference"))
                aliasReference = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectCertificateOptions(
            OptionDictionary options,
            X509VerificationFlags? defaultX509VerificationFlags,
            X509RevocationMode? defaultX509RevocationMode,
            X509RevocationFlag? defaultX509RevocationFlag,
            out X509VerificationFlags x509VerificationFlags,
            out X509RevocationMode x509RevocationMode,
            out X509RevocationFlag x509RevocationFlag,
            out bool chain
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            x509VerificationFlags = (defaultX509VerificationFlags != null) ?
                (X509VerificationFlags)defaultX509VerificationFlags :
                CertificateOps.DefaultVerificationFlags;

            if ((options != null) &&
                options.CheckPresent("-verificationflags", ref value))
            {
                x509VerificationFlags = (X509VerificationFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            x509RevocationMode = (defaultX509RevocationMode != null) ?
                (X509RevocationMode)defaultX509RevocationMode :
                CertificateOps.DefaultRevocationMode;

            if ((options != null) &&
                options.CheckPresent("-revocationmode", ref value))
            {
                x509RevocationMode = (X509RevocationMode)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            x509RevocationFlag = (defaultX509RevocationFlag != null) ?
                (X509RevocationFlag)defaultX509RevocationFlag :
                CertificateOps.DefaultRevocationFlag;

            if ((options != null) &&
                options.CheckPresent("-revocationflag", ref value))
            {
                x509RevocationFlag = (X509RevocationFlag)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            chain = false;

            if ((options != null) && options.CheckPresent("-chain"))
                chain = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectDeclareOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool verbose,
            out bool strictType,
            out bool nonPublic,
            out bool noCase
            )
        {
            TypeList objectTypes;

            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType);

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-declaremode", "-declarepattern", "-declarenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            nonPublic = false;

            if ((options != null) && options.CheckPresent("-declarenonpublic"))
                nonPublic = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectImportOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string container,
            out string pattern,
            out bool assembly,
            out bool system,
            out bool noCase
            )
        {
            bool nonPublic;

            ProcessObjectImportOptions(
                options, defaultMatchMode, out matchMode, out container,
                out pattern, out assembly, out system, out nonPublic,
                out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectImportOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string container,
            out string pattern,
            out bool assembly,
            out bool system,
            out bool nonPublic,
            out bool noCase
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-importmode", "-importpattern", "-importnocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            container = null;

            if ((options != null) &&
                options.CheckPresent("-container", ref value))
            {
                container = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            nonPublic = false;

            if ((options != null) && options.CheckPresent("-importnonpublic"))
                nonPublic = true;

            ///////////////////////////////////////////////////////////////////

            assembly = false;

            if ((options != null) && options.CheckPresent("-assembly"))
                assembly = true;

            ///////////////////////////////////////////////////////////////////

            system = false;

            if ((options != null) && options.CheckPresent("-system"))
                system = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectInvokeRawOptions(
            OptionDictionary options,
            ValueFlags? defaultObjectValueFlags,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out Type type,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out bool noByRef,
            out bool strictType,
            out bool strictArgs,
            out bool noNestedObject,
            out bool noCase,
            out bool invoke,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            ValueFlags memberValueFlags;
            MemberTypes memberTypes;
            ReorderFlags reorderFlags;
            bool verbose;

            ProcessMarshalOptions(
                options, null, null, null, defaultBindingFlags,
                defaultMarshalFlags, null, defaultByRefArgumentFlags,
                out type, out objectTypes, out methodTypes, out parameterTypes,
                out parameterMarshalFlags, out objectValueFlags,
                out memberValueFlags, out memberTypes, out bindingFlags,
                out marshalFlags, out reorderFlags, out byRefArgumentFlags,
                out noByRef, out verbose, out strictType, out strictArgs,
                out noNestedObject, out noCase, out invoke, out noArgs,
                out arrayAsValue, out arrayAsLink, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectIsOfTypeOptions(
            OptionDictionary options,
            MarshalFlags? defaultMarshalFlags,
            out TypeList objectTypes,
            out MarshalFlags marshalFlags,
            out bool verbose,
            out bool strictType,
            out bool noCase,
            out bool noComplain,
            out bool assignable
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType,
                out noCase);

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if ((options != null) &&
                options.CheckPresent("-marshalflags", ref value))
            {
                marshalFlags = (MarshalFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            noComplain = false;

            if ((options != null) && options.CheckPresent("-nocomplain"))
                noComplain = true;

            ///////////////////////////////////////////////////////////////////

            assignable = false;

            if ((options != null) && options.CheckPresent("-assignable"))
                assignable = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectLoadOptions(
            OptionDictionary options,
            LoadType? defaultLoadType,
            MatchMode? defaultMatchMode,
            out INamespace @namespace,
            out LoadType loadType,
            out MatchMode declareMatchMode,
            out MatchMode importMatchMode,
            out string declarePattern,
            out string importPattern,
            out bool declare,
            out bool import,
            out bool declareNonPublic,
            out bool declareNoCase,
            out bool importNonPublic,
            out bool importNoCase
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-declaremode", "-declarepattern", "-declarenocase",
                defaultMatchMode, out declareMatchMode, out declarePattern,
                out declareNoCase);

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-importmode", "-importpattern", "-importnocase",
                defaultMatchMode, out importMatchMode, out importPattern,
                out importNoCase);

            ///////////////////////////////////////////////////////////////////

            @namespace = null;

            if ((options != null) &&
                options.CheckPresent("-namespace", ref value))
            {
                @namespace = (INamespace)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            loadType = (defaultLoadType != null) ?
                (LoadType)defaultLoadType : DefaultLoadType;

            if ((options != null) &&
                options.CheckPresent("-loadtype", ref value))
            {
                loadType = (LoadType)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            declare = false;

            if ((options != null) && options.CheckPresent("-declare"))
                declare = true;

            ///////////////////////////////////////////////////////////////////

            import = false;

            if ((options != null) && options.CheckPresent("-import"))
                import = true;

            ///////////////////////////////////////////////////////////////////

            declareNonPublic = false;

            if ((options != null) && options.CheckPresent("-declarenonpublic"))
                declareNonPublic = true;

            ///////////////////////////////////////////////////////////////////

            importNonPublic = false;

            if ((options != null) && options.CheckPresent("-importnonpublic"))
                importNonPublic = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectMembersOptions(
            OptionDictionary options,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            MatchMode? defaultMatchMode,
            out TypeList objectTypes,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out MatchMode matchMode,
            out string pattern,
            out bool verbose,
            out bool strictType,
            out bool noCase,
            out bool attributes,
            out bool matchNameOnly,
            out bool nameOnly,
            out bool signatures,
            out bool qualified
            )
        {
            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType,
                out noCase);

            ///////////////////////////////////////////////////////////////////

            ProcessReflectionOptions(
                options, defaultMemberTypes, defaultBindingFlags,
                out memberTypes, out bindingFlags);

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-mode", "-pattern", defaultMatchMode,
                out matchMode, out pattern);

            ///////////////////////////////////////////////////////////////////

            attributes = false;

            if ((options != null) && options.CheckPresent("-attributes"))
                attributes = true;

            ///////////////////////////////////////////////////////////////////

            matchNameOnly = false;

            if ((options != null) && options.CheckPresent("-matchnameonly"))
                matchNameOnly = true;

            ///////////////////////////////////////////////////////////////////

            nameOnly = false;

            if ((options != null) && options.CheckPresent("-nameonly"))
                nameOnly = true;

            ///////////////////////////////////////////////////////////////////

            signatures = false;

            if ((options != null) && options.CheckPresent("-signatures"))
                signatures = true;

            ///////////////////////////////////////////////////////////////////

            qualified = false;

            if ((options != null) && options.CheckPresent("-qualified"))
                qualified = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectTypeOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase
            )
        {
            ProcessPatternMatchingOptions(
                options, "-typemode", "-typepattern", "-typenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectUnaliasNamespaceOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase,
            out bool values
            )
        {
            ProcessPatternMatchingOptions(
                options, "-aliasmode", "-aliaspattern", "-aliasnocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            values = false;

            if ((options != null) && options.CheckPresent("-bycontainer"))
                values = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectUndeclareOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase,
            out bool values
            )
        {
            ProcessPatternMatchingOptions(
                options, "-declaremode", "-declarepattern", "-declarenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            values = false;

            if ((options != null) && options.CheckPresent("-bycontainer"))
                values = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectUnimportOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase,
            out bool values
            )
        {
            ProcessPatternMatchingOptions(
                options, "-importmode", "-importpattern", "-importnocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            values = false;

            if ((options != null) && options.CheckPresent("-bycontainer"))
                values = true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectUntypeOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase
            )
        {
            ProcessPatternMatchingOptions(
                options, "-typemode", "-typepattern", "-typenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessPatternMatchingOptions(
            OptionDictionary options,
            string matchModeOptionName,
            string patternOptionName,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern
            )
        {
            bool noCase;

            ProcessPatternMatchingOptions(
                options, matchModeOptionName, patternOptionName, null,
                defaultMatchMode, out matchMode, out pattern, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessPatternMatchingOptions(
            OptionDictionary options,
            string matchModeOptionName,
            string patternOptionName,
            string noCaseOptionName,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            matchMode = (defaultMatchMode != null) ?
                (MatchMode)defaultMatchMode : DefaultMatchMode;

            if ((options != null) &&
                (matchModeOptionName != null) &&
                options.CheckPresent(matchModeOptionName, ref value))
            {
                matchMode = (MatchMode)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            pattern = null;

            if ((options != null) &&
                (patternOptionName != null) &&
                options.CheckPresent(patternOptionName, ref value))
            {
                pattern = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            noCase = false;

            if ((options != null) &&
                (noCaseOptionName != null) &&
                options.CheckPresent(noCaseOptionName))
            {
                noCase = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ProcessReflectionOptions(
            OptionDictionary options,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags
            )
        {
            Variant value = null;

            ///////////////////////////////////////////////////////////////////

            memberTypes = (defaultMemberTypes != null) ?
                (MemberTypes)defaultMemberTypes : DefaultMemberTypes;

            if ((options != null) &&
                options.CheckPresent("-membertypes", ref value))
            {
                memberTypes = (MemberTypes)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            bindingFlags = (defaultBindingFlags != null) ?
                (BindingFlags)defaultBindingFlags : DefaultBindingFlags;

            //
            // TODO: Is this a really bad option name?
            //
            bool hadFlags = (options != null) &&
                options.CheckPresent("-flags", ref value);

            if (hadFlags)
                bindingFlags = (BindingFlags)value.Value;

            if ((options != null) &&
                options.CheckPresent("-bindingflags", ref value))
            {
                if (hadFlags)
                    bindingFlags |= (BindingFlags)value.Value;
                else
                    bindingFlags = (BindingFlags)value.Value;
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Invocation Support Methods
        public static ReturnCode InvokeDelegate(
            Interpreter interpreter,
            Delegate @delegate,
            ArgumentList arguments,
            ref Result result
            )
        {
            ///////////////////////////////////////////////////////////////////
            //                       ARGUMENT VALIDATION
            ///////////////////////////////////////////////////////////////////

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (@delegate == null)
            {
                result = "invalid delegate";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid arguments";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //                        OPTION PROCESSING
            ///////////////////////////////////////////////////////////////////

            ReturnCode code = ReturnCode.Ok;
            OptionDictionary options = GetCallOptions();
            int argumentIndex = Index.Invalid;

            if (arguments.Count > 1)
            {
                code = interpreter.GetOptions(
                    options, arguments, 0, 1, Index.Invalid, true,
                    ref argumentIndex, ref result);

                if (code != ReturnCode.Ok)
                    return code;
            }

            ///////////////////////////////////////////////////////////////////

            BindingFlags bindingFlags;
            MarshalFlags marshalFlags;
            ReorderFlags reorderFlags;
            ByRefArgumentFlags byRefArgumentFlags;
            int limit;
            int index;
            bool noByRef;
            bool strictMember;
            bool strictArgs;
            bool invoke;
            bool noArgs;
            bool arrayAsValue;
            bool arrayAsLink;
            bool trace;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, null, null, null, null, out bindingFlags,
                out marshalFlags, out reorderFlags, out byRefArgumentFlags,
                out limit, out index, out noByRef, out strictMember,
                out strictArgs, out invoke, out noArgs, out arrayAsValue,
                out arrayAsLink, out trace);

            ///////////////////////////////////////////////////////////////////

            Type returnType;
            ObjectFlags objectFlags;
            ObjectFlags byRefObjectFlags;
            string objectName;
            string interpName;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;

            ProcessFixupReturnValueOptions(
                options, null, null, out returnType, out objectFlags,
                out byRefObjectFlags, out objectName, out interpName,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString);

            ///////////////////////////////////////////////////////////////////
            //                    METHOD ARGUMENT BUILDING
            ///////////////////////////////////////////////////////////////////

            object[] args = null;
            int argumentCount = 0;

            if ((argumentIndex != Index.Invalid) &&
                (argumentIndex < arguments.Count))
            {
                //
                // NOTE: How many arguments were supplied?
                //
                argumentCount = (arguments.Count - argumentIndex);

                //
                // NOTE: Create and populate the array of arguments for the
                //       invocation.
                //
                args = new object[argumentCount];

                for (int index2 = argumentIndex; index2 < arguments.Count;
                        index2++)
                {
                    /* need String, not Argument */
                    args[index2 - argumentIndex] = arguments[index2].String;
                }
            }
            else if (invoke || !noArgs)
            {
                //
                // FIXME: When no arguments are specified, we actually need an
                //        array of zero arguments for the parameter to argument
                //        matching code to work correctly.
                //
                args = new object[0];
            }

            //
            // HACK: We want to use the existing marshalling code; therefore,
            //       we pre-bake some of the required arguments here (i.e. we
            //       KNOW what method we are going to call, however we want
            //       magical bi-directional type coercion, etc).
            //
            object delegateTarget = @delegate.Target;
            MethodInfo delegateMethodInfo = @delegate.Method;

            if (delegateMethodInfo == null)
            {
                result = "cannot invoke delegate, no method";
                return ReturnCode.Error;
            }

            string newObjectName = (delegateTarget != null) ?
                delegateTarget.GetType().FullName : null;

            string newMemberName = delegateMethodInfo.Name;
            MethodInfo[] methodInfo = new MethodInfo[] { delegateMethodInfo };

            if (methodInfo == null) // NOTE: Redundant [for now].
            {
                result = String.Format(
                    "delegate \"{0}\" has no methods matching \"{1}\"",
                    newObjectName, bindingFlags);

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //                    METHOD ARGUMENT CONVERSION
            ///////////////////////////////////////////////////////////////////

            IntList methodIndexList = null;
            ObjectArrayList argsList = null;
            IntArgumentInfoListDictionary argumentInfoListDictionary = null;
            ResultList errors = null;

            //
            // NOTE: Attempt to convert the argument strings to something
            //       potentially more meaningful and find the corresponding
            //       method.
            //
            code = MarshalOps.FindMethodsAndFixupArguments(
                interpreter, interpreter.Binder, options,
                interpreter.CultureInfo, @delegate.GetType(),
                newObjectName, newObjectName, newMemberName,
                newMemberName, MemberTypes.Method, bindingFlags,
                methodInfo, null, null, null, args, limit,
                marshalFlags, ref methodIndexList, ref argsList,
                ref argumentInfoListDictionary, ref errors);

            if (code != ReturnCode.Ok)
            {
                result = errors;
                return code;
            }

            ///////////////////////////////////////////////////////////////////
            //                   METHOD OVERLOAD REORDERING
            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.ReorderMatches, true))
            {
                IntList savedMethodIndexList = new IntList(methodIndexList);

                code = MarshalOps.ReorderMethodIndexes(
                    interpreter.Binder, interpreter.CultureInfo,
                    methodInfo, marshalFlags, reorderFlags,
                    ref methodIndexList, ref argsList, ref errors);

                if (code == ReturnCode.Ok)
                {
                    if (trace)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "InvokeDelegate: savedMethodIndexList = {0}, " +
                            "methodIndexList = {1}", savedMethodIndexList,
                            methodIndexList), typeof(ObjectOps).Name,
                            TracePriority.MarshalDebug);
                    }
                }
                else
                {
                    result = errors;
                    return code;
                }
            }

            ///////////////////////////////////////////////////////////////////
            //                   METHOD OVERLOAD VALIDATION
            ///////////////////////////////////////////////////////////////////

            if ((methodIndexList == null) || (methodIndexList.Count == 0) ||
                (argsList == null) || (argsList.Count == 0))
            {
                result = String.Format(
                    "method \"{0}\" of delegate \"{1}\" not found",
                    newMemberName, newObjectName);

                return ReturnCode.Error;
            }

            if ((index != Index.Invalid) &&
                ((index < 0) || (index >= methodIndexList.Count) ||
                (index >= argsList.Count)))
            {
                result = String.Format(
                    "method \"{0}\" of delegate \"{1}\" not found, " +
                    "invalid method index {2}, must be {3}",
                    newMemberName, newObjectName, index,
                    FormatOps.BetweenOrExact(0, methodIndexList.Count - 1));

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //                      OPTION TYPE SELECTION
            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Figure out which type of options are needed for created
            //       aliases.
            //
            ObjectOptionType objectOptionType = ObjectOptionType.Delegate |
                GetOptionType(aliasRaw, aliasAll);

            //
            // NOTE: Are we actually going to invoke the method or are we simply
            //       returning the list of matching method overloads?  For this
            //       method, the list of method overloads should always have
            //       exactly one result (i.e. it is somewhat redundant; however,
            //       it is designed to match the semantics of [object invoke]).
            //
            if (invoke)
            {
                ///////////////////////////////////////////////////////////////
                //                  METHOD OVERLOAD SELECTION
                ///////////////////////////////////////////////////////////////

                if (strictMember && (methodIndexList.Count != 1))
                {
                    result = String.Format(
                        "matched {0} method overloads of \"{1}\" on delegate " +
                        "\"{2}\", need exactly 1", methodIndexList.Count,
                        newMemberName, newObjectName);

                    return ReturnCode.Error;
                }

                //
                // FIXME: Select the first method that matches.  More
                //        sophisticated logic may need to be added here later.
                //
                int methodIndex = (index != Index.Invalid) ?
                    methodIndexList[index] : methodIndexList[0];

                if (methodIndex == Index.Invalid)
                {
                    result = String.Format(
                        "method \"{0}\" of delegate \"{1}\" not found",
                        newMemberName, newObjectName);

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////
                //               METHOD ARGUMENT ARRAY SELECTION
                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Get the arguments we are going to use to perform
                //       the actual method call.
                //
                args = (index != Index.Invalid) ? argsList[index] : argsList[0];

                //
                // NOTE: Lookup the output argument list associated with the
                //       method to be invoked.  This may end up being null.
                //       In that case, no output argument handling will be
                //       done.
                //
                ArgumentInfoList argumentInfoList;

                /* IGNORED */
                MarshalOps.TryGetArgumentInfoList(argumentInfoListDictionary,
                    methodIndex, out argumentInfoList);

                ///////////////////////////////////////////////////////////////
                //                       METHOD TRACING
                ///////////////////////////////////////////////////////////////

                if (trace)
                {
                    TraceOps.DebugTrace(String.Format(
                        "InvokeDelegate: methodIndex = {0}, delegate = {1}, " +
                        "args = {2}, argumentInfoList = {3}", methodIndex,
                        FormatOps.WrapOrNull(@delegate), FormatOps.WrapOrNull(
                        new StringList(args)), FormatOps.WrapOrNull(
                        argumentInfoList)), typeof(ObjectOps).Name,
                        TracePriority.MarshalDebug);
                }

                ///////////////////////////////////////////////////////////////
                //                      METHOD INVOCATION
                ///////////////////////////////////////////////////////////////

                object returnValue = null;

                code = Engine.ExecuteDelegate(
                    @delegate, args, ref returnValue, ref result);

                ///////////////////////////////////////////////////////////////
                //                   BYREF ARGUMENT HANDLING
                ///////////////////////////////////////////////////////////////

                if ((code == ReturnCode.Ok) &&
                    !noByRef && (argumentInfoList != null))
                {
                    code = MarshalOps.FixupByRefArguments(
                        interpreter, interpreter.Binder,
                        interpreter.CultureInfo, argumentInfoList,
                        objectFlags | byRefObjectFlags, GetInvokeOptions(
                        objectOptionType), objectOptionType, interpName,
                        args, marshalFlags, byRefArgumentFlags, strictArgs,
                        create, dispose, alias, aliasReference, toString,
                        arrayAsValue, arrayAsLink, ref result);
                }

                ///////////////////////////////////////////////////////////////
                //                    RETURN VALUE HANDLING
                ///////////////////////////////////////////////////////////////

                if (code == ReturnCode.Ok)
                {
                    code = MarshalOps.FixupReturnValue(
                        interpreter, interpreter.Binder,
                        interpreter.CultureInfo, returnType, objectFlags,
                        options, objectOptionType, objectName, interpName,
                        returnValue, create, dispose, alias, aliasReference,
                        toString, ref result);
                }
            }
            else
            {
                ///////////////////////////////////////////////////////////////
                //                 METHOD OVERLOAD DIAGNOSTICS
                ///////////////////////////////////////////////////////////////

                MethodInfoList methodInfoList = new MethodInfoList();

                if (index != Index.Invalid)
                {
                    methodInfoList.Add(methodInfo[methodIndexList[index]]);
                }
                else
                {
                    foreach (int methodIndex in methodIndexList)
                        methodInfoList.Add(methodInfo[methodIndex]);
                }

                ///////////////////////////////////////////////////////////////
                //                    RETURN VALUE HANDLING
                ///////////////////////////////////////////////////////////////

                code = MarshalOps.FixupReturnValue(
                    interpreter, interpreter.Binder, interpreter.CultureInfo,
                    returnType, objectFlags, options, objectOptionType,
                    objectName, interpName, methodInfoList, create, dispose,
                    alias, aliasReference, toString, ref result);
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Reference Support Methods
        public static bool RemoveTemporaryReferences(
            IObjectData objectData
            )
        {
            if (objectData == null)
                return false;

            int referenceCount = objectData.TemporaryReferenceCount;

            if (referenceCount > 0)
            {
                ObjectFlags flags = objectData.ObjectFlags;

                if (!FlagOps.HasFlags(flags, ObjectFlags.Locked, true))
                    objectData.ReferenceCount -= referenceCount;

                objectData.TemporaryReferenceCount = 0;

                flags &= ~ObjectFlags.TemporaryReturnReference;
                objectData.ObjectFlags = flags;

                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Disposal Support Methods
        public static ReturnCode TryDispose(
            object @object,  /* in */
            ref Result error /* out */
            )
        {
            bool dispose = DefaultDispose;

            return TryDispose(@object, ref dispose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryDispose(
            object @object,   /* in */
            ref bool dispose, /* in, out */
            ref Result error  /* out */
            )
        {
            Exception exception = null;

            return TryDispose(
                @object, ref dispose, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryDispose(
            object @object,         /* in */
            ref bool dispose,       /* in, out: No, not really. */
            ref Result error,       /* out */
            ref Exception exception /* out */
            )
        {
            if (@object != null)
            {
                try
                {
                    IDisposable disposable = @object as IDisposable;

                    if (disposable != null)
                    {
                        try
                        {
                            disposable.Dispose(); /* throw */
                            dispose = true; /* disposed */

                            return ReturnCode.Ok; /* success */
                        }
                        catch (Exception e)
                        {
                            //
                            // NOTE: The object threw an exception during its
                            //       disposal.  This is technically allowed;
                            //       however, it is typically discouraged.
                            //       Save the information and report it to our
                            //       caller.
                            //
                            error = e;
                            exception = e;

                            return ReturnCode.Error; /* failure */
                        }
                        finally
                        {
                            disposable = null;
                        }
                    }
                    else
                    {
                        //
                        // NOTE: Object does not implement IDisposable.
                        //
                        dispose = false; /* not disposable */

                        return ReturnCode.Ok; /* success */
                    }
                }
                finally
                {
                    @object = null;
                }
            }
            else
            {
                //
                // NOTE: Object is not valid.
                //
                dispose = false; /* invalid object */

                return ReturnCode.Ok;
            }
        }
        #endregion
    }
}
