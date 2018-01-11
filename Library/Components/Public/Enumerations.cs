/*
 * Enumerations.cs --
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
using Eagle._Attributes;

namespace Eagle._Components.Public
{
    [Flags()]
    [ObjectId("17219a26-f14b-4643-82a0-b915c73dcd37")]
    public enum TracePriority
    {
        None = 0x0,    // do not use.
        Invalid = 0x1, // reserved, do not use.

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: It should be noted here that these are always
        //       (currently) treated as flags by this library;
        //       hence, their relative values have no meaning.
        //       Still, keeping them in order is makes it easy
        //       to change this later.
        //
        #region Core Priority Values
        Lowest = 0x2,
        Lower = 0x4,
        Low = 0x8,
        MediumLow = 0x10,
        Medium = 0x20,
        MediumHigh = 0x40,
        High = 0x80,
        Higher = 0x100,
        Highest = 0x200,
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Core Type Values
        Error = 0x400,                     // error / exception message.
        Debug = 0x800,                     // debug / diagnostic  message.
        Demand = 0x1000,                   // on-demand via script command, etc.
        External = 0x2000,                 // message external to library.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Application / User Values (External Use Only)
        User0 = 0x4000,                    // reserved for third-party use.
        User1 = 0x8000,                    // reserved for third-party use.
        User2 = 0x10000,                   // reserved for third-party use.
        User3 = 0x20000,                   // reserved for third-party use.
        User4 = 0x40000,                   // reserved for third-party use.
        User5 = 0x80000,                   // reserved for third-party use.
        User6 = 0x100000,                  // reserved for third-party use.
        User7 = 0x200000,                  // reserved for third-party use.
        User8 = 0x400000,                  // reserved for third-party use.
        User9 = 0x800000,                  // reserved for third-party use.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Error Handling Values
        LowError = Low | Error,                 // for external use only.
        MediumError = Medium | Error,           // for external use only.
        HighError = High | Error,               // for external use only.

        ///////////////////////////////////////////////////////////////////////////////////////////

        InternalError = Low | Error,            // miscellaneous unclassified

        HostError = MediumLow | Error,          // interpreter host, console, etc.

        PlatformError = Medium | Error,         // operating system call, etc.
        PathError = Medium | Error,             // path discovery and building
        FileSystemError = Medium | Error,       // file system error, etc.
        CallbackError = Medium | Error,         // user-defined callback issue
        EngineError = Medium | Error,           // script evaluation & support

        MarshalError = MediumHigh | Error,      // core marshaller, binder, etc.
        NativeError = MediumHigh | Error,       // native code and interop

        EventError = High | Error,              // event manager and processing
        HandleError = High | Error,             // handles, native and managed

        LockError = Higher | Error,             // unable to acquire lock
        ThreadError = Higher | Error,           // thread exceptions, timeout, etc.
        ScriptThreadError = Higher | Error,     // ScriptThread exceptions, etc.

        StartupError = Highest | Error,         // library / interpreter startup.
        ShellError = Highest | Error,           // interactive shell and loop.
        SecurityError = Highest | Error,        // signatures, certificates, etc.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Debugging Values
        LowDebug = Low | Debug,                 // for external use only.
        MediumDebug = Medium | Debug,           // for external use only.
        HighDebug = High | Debug,               // for external use only.

        ///////////////////////////////////////////////////////////////////////////////////////////

        PlatformDebug = Medium | Debug,         // operating system call, etc.
        PathDebug = Lowest | Debug,             // path discovery and building

        ShellDebug = Lower | Debug,             // interactive shell and loop.
        HostDebug = Lower | Debug,              // interpreter host, console, etc.
        GetScriptDebug = Lower | Debug,         // _Hosts.Core.GetScript(), et al.

        EventDebug = Low | Debug,               // event manager and processing
        EngineDebug = Low | Debug,              // script evaluation & support

        CleanupDebug = MediumLow | Debug,       // object disposal and cleanup.
        StartupDebug = MediumLow | Debug,       // library / interpreter startup.

        NativeDebug = Medium | Debug,           // native code and interop (summary)
        NativeDebug2 = MediumLow | Debug,       // native code and interop (details)
        MarshalDebug = Medium | Debug,          // core marshaller, binder, etc.

        ThreadDebug = MediumHigh | Debug,       // thread exceptions, timeout, etc.
        TestDebug = MediumHigh | Debug,         // test suite infrastructure, etc.
        ScriptThreadDebug = MediumHigh | Debug, // ScriptThread debugging, etc.

        NetworkDebug = Highest | Debug,         // data transfer over network, etc.
        SecurityDebug = Highest | Debug,        // signatures, certificates, etc.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Other Values
        Command = Medium | Demand,         // related to script command, etc.
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Conditional Values
        //
        // HACK: For the "Debug" build, enable those messages by default;
        //       otherwise, disable them by default.
        //
#if DEBUG
        MaybeDebug = Debug,
#else
        MaybeDebug = None,
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Useful Mask Values
        HigherAndUpMask = Higher | Highest,
        HighAndUpMask = High | HigherAndUpMask,

        MediumHighAndUpMask = MediumHigh | HighAndUpMask,
        MediumAndUpMask = Medium | MediumHighAndUpMask,
        MediumLowAndUpMask = MediumLow | MediumAndUpMask,

        LowAndUpMask = Low | MediumLowAndUpMask,
        LowerAndUpMask = Lower | LowAndUpMask,
        LowestAndUpMask = Lowest | LowerAndUpMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyPriorityMask = LowestAndUpMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        MaybeAnyCoreTypeMask = Error | MaybeDebug | Demand | External,

        AnyCoreTypeMask = Error | Debug | Demand | External,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyUserTypeMask = User0 | User1 | User2 |
                          User3 | User4 | User5 |
                          User6 | User7 | User8 |
                          User9,

        ///////////////////////////////////////////////////////////////////////////////////////////

        MaybeAnyTypeMask = MaybeAnyCoreTypeMask | AnyUserTypeMask,

        AnyTypeMask = AnyCoreTypeMask | AnyUserTypeMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyMask = AnyPriorityMask | AnyTypeMask,
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////

        #region Suggested Default Priority & Mask Values
        Default = Medium,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultDebug = Default | Debug,
        DefaultError = Default | Error,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultMask = MediumAndUpMask | MaybeAnyTypeMask
        #endregion
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if !CONSOLE
    [ObjectId("81605ffc-3647-472c-acd4-5d79b9434ea0")]
    public enum ConsoleColor /* COMPAT: .NET Framework. */
    {
        Black = 0,
        DarkBlue = 1,
        DarkGreen = 2,
        DarkCyan = 3,
        DarkRed = 4,
        DarkMagenta = 5,
        DarkYellow = 6,
        Gray = 7,
        DarkGray = 8,
        Blue = 9,
        Green = 10,
        Cyan = 11,
        Red = 12,
        Magenta = 13,
        Yellow = 14,
        White = 15
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && TCL_THREADS
    [ObjectId("581367c7-c7ee-44d1-8583-8fa3fb8e08a5")]
    internal enum TclThreadEvent
    {
        //
        // WARNING: The ordering of these values must match those
        //          in the ThreadStart() method of the TclThread
        //          class.
        //
        DoneEvent = 0x0,
        IdleEvent = 0x1,
        QueueEvent = 0x2
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("99b147d8-cb56-40dc-a7ca-bc6ded108bad")]
    public enum CreationFlagTypes
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CurrentCreateFlags = 0x10,
        CurrentInitializeFlags = 0x20,
        CurrentScriptFlags = 0x40,
        CurrentInterpreterFlags = 0x80,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultCreateFlags = 0x1000,
        DefaultInitializeFlags = 0x2000,
        DefaultScriptFlags = 0x4000,
        DefaultInterpreterFlags = 0x8000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FallbackCreateFlags = 0x100000,
        FallbackInitializeFlags = 0x200000,
        FallbackScriptFlags = 0x400000,
        FallbackInterpreterFlags = 0x800000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllCurrentFlags = CurrentCreateFlags | CurrentInitializeFlags |
                          CurrentScriptFlags | CurrentInterpreterFlags,

        AllDefaultFlags = DefaultCreateFlags | DefaultInitializeFlags |
                          DefaultScriptFlags | DefaultInterpreterFlags,

        AllFallbackFlags = FallbackCreateFlags | FallbackInitializeFlags |
                           FallbackScriptFlags | FallbackInterpreterFlags,

        Reserved1 = 0x10000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = AllDefaultFlags | Reserved1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
    [Flags()]
    [ObjectId("dc69c8a9-5970-4317-a5e3-fe980076cd22")]
    public enum TestResolveFlags
    {
        None = 0x0,                    /* No special handling. */
        Invalid = 0x1,                 /* Invalid, do not use. */
        AlwaysUseNamespaceFrame = 0x2, /* Always return the call frame
                                        * associated with the configured
                                        * namespace, if any. */
        NextUseNamespaceFrame = 0x4,   /* For the next call, return the call
                                        * frame associated with the configured
                                        * namespace, if any. */
        HandleGlobalOnly = 0x8,        /* Do not skip handling the call frame
                                        * lookup because the global-only flag
                                        * is set. */
        HandleAbsolute = 0x10,         /* Do not skip handling the call frame
                                        * lookup because the name is absolute.
                                        */
        HandleQualified = 0x20,        /* Do not skip handling the call frame
                                        * lookup because the name is qualified.
                                        */
        EnableLogging = 0x40,          /* Enable logging of all test IResolve
                                        * interface method calls. */

        Default = None
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9f719570-729f-4234-8282-0face8230536")]
    public enum TypeListFlags
    {
        None = 0x0,
        Invalid = 0x1,

        IntegerTypes = 0x2,
        FloatTypes = 0x4,
        StringTypes = 0x8,
        NumberTypes = 0x10,
        IntegralTypes = 0x20,
        NonIntegralTypes = 0x40,
        OtherTypes = 0x80,
        AllTypes = 0x100
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7a2e9b30-2671-4f43-806a-745d500713ac")]
    internal enum ConfigurationOperation
    {
        None = 0x0,
        Invalid = 0x1,

        Get = 0x2,
        Set = 0x4,
        Unset = 0x8
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("dbc4da96-c186-4a57-b0ec-8f4a6929f18c")]
    internal enum ConfigurationFlags
    {
        None = 0x0,                /* No special handling. */
        Invalid = 0x1,             /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Unprefixed = 0x2,          /* Check, modify, or remove values that are
                                    * NOT prefixed with the package name. */
        Prefixed = 0x4,            /* Check, modify, or remove values that are
                                    * prefixed with the package name. */
        Expand = 0x8,              /* Expand contained environment variables. */
        Verbose = 0x10,            /* Emit diagnostic messages. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Environment = 0x20,        /* Check, modify, or remove environment variables. */

#if CONFIGURATION
        AppSettings = 0x40,        /* Check, modify, or remove loaded AppSettings. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CONFIGURATION
        MaybeAppSettings = AppSettings,
#else
        MaybeAppSettings = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardMask = Unprefixed | Prefixed | Environment | MaybeAppSettings,
        UtilityMask = StandardMask & ~Prefixed,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        ForCacheConfiguration = 0x80, /* For use by the CacheConfiguration class. */
#endif

        ForGlobalState = 0x100,       /* For use by the GlobalState class. */
        ForInteractiveOps = 0x200,    /* For use by the InteractiveOps class. */
        ForInterpreter = 0x400,       /* For use by the Interpreter class. */

#if NATIVE && TCL && NATIVE_PACKAGE
        ForNativePackage = 0x800,     /* For use by the NativePackage class. */
#endif

#if NATIVE && NATIVE_UTILITY
        ForNativeUtility = 0x1000,    /* For use by the NativeUtility class. */
#endif

        ForPathOps = 0x2000,          /* For use by the PathOps class. */
        ForUtility = 0x4000,          /* For use by the Utility class. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        CacheConfiguration = StandardMask | ForCacheConfiguration,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        GlobalState = StandardMask | ForGlobalState,
        GlobalStateNoPrefix = GlobalState & ~Prefixed,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveOps = StandardMask | ForInteractiveOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Interpreter = StandardMask | ForInterpreter,
        InterpreterVerbose = Verbose | Interpreter,

        ///////////////////////////////////////////////////////////////////////////////////////////

        PathOps = StandardMask | ForPathOps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Utility = UtilityMask | ForUtility,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
        NativePackage = StandardMask | ForNativePackage,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && NATIVE_UTILITY
        NativeUtility = StandardMask | ForNativeUtility,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("27e1fa59-a2fc-44db-8c52-155c6b18fbed")]
    internal enum GarbageFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved1 = 0x2,
        Reserved2 = 0x4,
        Reserved3 = 0x8,

        ///////////////////////////////////////////////////////////////////////////////////////////

        NeverCollect = 0x10,
        AlwaysCollect = 0x20,
        MaybeCollect = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
        NeverCompact = 0x80,
        AlwaysCompact = 0x100,
        MaybeCompact = 0x200,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NeverWait = 0x400,
        AlwaysWait = 0x800,
        MaybeWait = 0x1000,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
        WhenPossibleCompact = AlwaysCompact,
        MaybeWhenPossibleCompact = MaybeCompact,
#else
        WhenPossibleCompact = None,
        MaybeWhenPossibleCompact = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForCommand = Reserved1 | AlwaysCollect |
                     WhenPossibleCompact | AlwaysWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForEngine = Reserved2 | AlwaysCollect |
                    WhenPossibleCompact | NeverWait,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Reserved3 | MaybeCollect |
                  MaybeWhenPossibleCompact | MaybeWait
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2fc84796-ec90-471c-ad91-6d5f218c3102")]
    internal enum DisposalPhase : ulong
    {
        None = 0x0,                     // No special handling.
        Invalid = 0x1,                  // Invalid, do not use.
        Native = 0x2,                   // Native plugin, command, etc.
        Managed = 0x4,                  // Managed plugin, command, etc.
        User = 0x8,                     // Non-system plugin, command, etc.
        System = 0x10,                  // System plugin, command, etc.

        Plugin = 0x20,                  // An IPlugin object.
        Command = 0x40,                 // An ICommand object.
        Function = 0x80,                // An IFunction object.
        Operator = 0x100,               // An IOperator object.
        Namespace = 0x200,              // An INamespace object.
        Resolver = 0x400,               // An IResolver object.
        Policy = 0x800,                 // An IPolicy object.
        Trace = 0x1000,                 // An ITrace object.
        EventManager = 0x2000,          // An IEventManager object.
        RandomNumberGenerator = 0x4000, // An RNG of some kind.
        Debugger = 0x8000,              // An IDebugger object.
        Scope = 0x10000,                // An ICallFrame object.
        Alias = 0x20000,                // An IAlias object.
        Database = 0x40000,             // An ADO.NET object.
        Channel = 0x80000,              // A channel or encoding object.
        Object = 0x100000,              // An IObject or related object.
        Trusted = 0x200000,             // A trusted type, URI, path, etc.
        Procedure = 0x400000,           // An IProcedure object.
        Execute = 0x800000,             // An IExecute object.
        Callback = 0x1000000,           // An ICallback object.
        Package = 0x2000000,            // An IPackage object.
        Thread = 0x4000000,             // System.Threading.Thread object.
        Interpreter = 0x8000000,        // An IInterpreter object.
        AppDomain = 0x10000000,         // System.AppDomain object.
        NativeLibrary = 0x20000000,     // A native delegate or module.
        NativeTcl = 0x40000000,         // Native Tcl integration subsystem.

        Phase1 = 0x80000000,
        Phase2 = 0x100000000,
        Phase3 = 0x200000000,
        Phase4 = 0x400000000,
        Phase5 = 0x800000000,

        VariableMask = Namespace | Resolver | Trace,

        NonBaseMask = Function | Operator | EventManager |
                      RandomNumberGenerator | Debugger |
                      Scope | Alias | Database | Channel |
                      Object | Trusted | Procedure | Execute |
                      Callback | Package | Thread | Interpreter |
                      AppDomain | NativeLibrary | NativeTcl,

        All = Plugin | Command | Function | Operator |
              Namespace | Resolver | Policy | Trace |
              EventManager | RandomNumberGenerator |
              Debugger | Scope | Alias | Database |
              Channel | Object | Trusted | Procedure |
              Execute | Callback | Package | Thread |
              Interpreter | AppDomain | NativeLibrary |
              NativeTcl,

        Phase1Mask = Phase1 | Native | User,
        Phase2Mask = Phase2 | Native | System,
        Phase3Mask = Phase3 | Managed | User,
        Phase4Mask = Phase4 | Managed | System
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
    //
    // WARNING: Reserved as a placeholder by the core library to represent all
    //          non-flags enumerated types defined by plugins loaded into
    //          isolated application domains.  Do not modify.
    //
    [ObjectId("9829bd1e-25bb-4445-bc00-5ae4dbcd8ab5")]
    internal enum StubEnum
    {
        //
        // HACK: Every enum type must have at least one value and zero is
        //       always implicitly allowed anyhow [by the CLR]; therefore,
        //       this just formalizes that behavior.
        //
        None = 0x0
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // WARNING: Reserved as a placeholder by the core library to represent all
    //          flags enumerated types defined by plugins loaded into isolated
    //          application domains.  Do not modify.
    //
    [Flags()]
    [ObjectId("a4e04c3a-bd77-426e-8149-9da823537be2")]
    internal enum StubFlagsEnum
    {
        //
        // HACK: Every enum type must have at least one value and zero is
        //       always implicitly allowed anyhow [by the CLR]; therefore,
        //       this just formalizes that behavior.
        //
        None = 0x0
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1bae8baa-330b-459a-a82f-97babf5c8c3f")]
    public enum WhiteSpaceFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Simple = 0x2,
        Unicode = 0x4,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Bell = 0x8,
        Backspace = 0x10,
        HorizontalTab = 0x20,
        LineFeed = 0x40,
        VerticalTab = 0x80,
        FormFeed = 0x100,
        CarriageReturn = 0x200,
        Space = 0x400,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForFormatted = 0x800,
        ForVariable = 0x1000,
        ForBox = 0x2000,
        ForTest = 0x4000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InvisibleCharactersMask = Bell | Backspace | HorizontalTab |
                                  LineFeed | VerticalTab | FormFeed |
                                  CarriageReturn,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Technically, the space character is visible.
        //
        AllCharactersMask = InvisibleCharactersMask | Space,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForMask = ForFormatted | ForTest | ForVariable | ForBox,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FormattedUse = InvisibleCharactersMask | ForFormatted,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VariableUse = Simple | InvisibleCharactersMask | ForVariable,

        ///////////////////////////////////////////////////////////////////////////////////////////

        BoxUse = Unicode | InvisibleCharactersMask | ForBox,

        ///////////////////////////////////////////////////////////////////////////////////////////

        TestUse = Unicode | AllCharactersMask | ForTest
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("37766464-c48b-4fbf-b00f-5239d86c0583")]
    public enum DetectFlags
    {
        None = 0x0,             /* Nothing. */
        Invalid = 0x1,          /* Invalid, do not use. */
        Assembly = 0x2,         /* Use the specified assembly as the basis of
                                 * the core script library detection. */
        Environment = 0x4,      /* Use the process environment variables as the
                                 * basis the core script library detection. */
        Setup = 0x8,            /* Use the installed instance as the basis of
                                 * the core script library detection. */
        BaseDirectory = 0x10,   /* Use the secondary name for the sub-directory
                                 * containing the "lib" sub-directory. */
        Directory = 0x20,       /* Use the primary name for the sub-directory
                                 * containing the "lib" sub-directory. */
        AssemblyVersion = 0x40, /* Use the assembly version number. */
        PackageVersion = 0x80,  /* Use the package version number. */
        NoVersion = 0x100,      /* Try without any version number. */

        All = Assembly | Environment | Setup |
              BaseDirectory | Directory | AssemblyVersion |
              PackageVersion | NoVersion,

        Default = All
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9cb44f96-4dfe-4852-8f61-6046940d1758")]
    public enum PackageType
    {
        None = 0x0,        /* No package. */
        Invalid = 0x1,     /* Invalid, do not use. */
        Library = 0x2,     /* The script library package. */
        Test = 0x4,        /* The test suite package. */
        Automatic = 0x8,   /* Attempt to automatically figure it out. */
        Default = 0x10,    /* Default package, for internal use only. */

        Mask = Library | Test | Automatic,

        Any = Library | Test /* Any known package type will work. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("dad4b809-342f-4da3-b533-7acdc54bae9e")]
    public enum EncodingType
    {
        None = 0x0,
        Invalid = 0x1,  /* Invalid, do not use. */
        System = 0x2,   /* Unicode */
        Default = 0x4,  /* UTF-8 */
        Binary = 0x8,   /* OneByte */
        Tcl = 0x10,     /* ISO-8859-1 */
        Channel = 0x20, /* ISO-8859-1 */
        Text = 0x40,    /* UTF-8 */
        Script = 0x80   /* UTF-8 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
    //
    // WARNING: Values may be added, modified, or removed from this enumeration
    //          at any time.
    //
    [Flags()]
    [ObjectId("7642aad2-c1d7-4ac5-80d0-f5e2d1db9bbf")]
    public enum CacheFlags : ulong
    {
        None = 0x0,           /* Nothing. */
        Invalid = 0x1,        /* Invalid, do not use. */
        Argument = 0x2,       /* Operate on the Argument object cache. */
        StringList = 0x4,     /* Operate on the StringList cache. */
        IParseState = 0x8,    /* Operate on the IParseState cache. */
        IExecute = 0x10,      /* Operate on the IExecute cache. */
        Type = 0x20,          /* Operate on the Type cache. */
        ComTypeList = 0x40,   /* Operate on the COM TypeList cache. */
        Miscellaneous = 0x80, /* Operate on other, internal caches. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForceTrim = 0x100,      /* Forcibly trim the specified caches if necessary,
                                 * ignorning any limits on how many items may be
                                 * trimmed at once. */
        Unlock = 0x200,         /* Unlock the specified caches. */
        Lock = 0x400,           /* Lock the specified caches. */
        DisableOnLock = 0x800,  /* When locking a cache, disable it as well. */
        Reset = 0x1000,         /* Create the specified caches if necessary -AND-
                                 * reset their settings back to their originally
                                 * configured values. */
        Clear = 0x2000,         /* Empty the specified caches. */
#if CACHE_DICTIONARY
        SetProperties = 0x4000, /* Configure various properties of the caches. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to enable more aggressive trimming
        //       of the associated cache, when appropriate (typically when
        //       under heavy load).
        //
        ForceTrimArgument = 0x8000,
        ForceTrimStringList = 0x10000,
        ForceTrimIParseState = 0x20000,
        ForceTrimIExecute = 0x40000, /* NOT YET IMPLEMENTED */
        ForceTrimType = 0x80000,
        ForceTrimComTypeList = 0x100000,
        ForceTrimMiscellaneous = 0x200000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to prevent the automatic enabling,
        //       disabling, or clearing of the associated cache, when it
        //       would have been appropriate (typically when under heavy
        //       load).
        //
        LockArgument = 0x400000,
        LockStringList = 0x800000,
        LockIParseState = 0x1000000,
        LockIExecute = 0x2000000, /* NOT YET IMPLEMENTED */
        LockType = 0x4000000,
        LockComTypeList = 0x8000000,
        LockMiscellaneous = 0x10000000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to automatically create the
        //       associated cache -AND- reset its settings back to
        //       the originally configured values when appropriate
        //       (typically when under heavy load).
        //
        ResetArgument = 0x20000000,
        ResetStringList = 0x40000000,
        ResetIParseState = 0x80000000,
        ResetIExecute = 0x100000000,
        ResetType = 0x200000000,
        ResetComTypeList = 0x400000000,
        ResetMiscellaneous = 0x800000000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are used to automatically clear the
        //       associated cache when appropriate (typically when
        //       under heavy load).
        //
        ClearArgument = 0x1000000000,
        ClearStringList = 0x2000000000,
        ClearIParseState = 0x4000000000,
        ClearIExecute = 0x8000000000,
        ClearType = 0x10000000000,
        ClearComTypeList = 0x20000000000,
        ClearMiscellaneous = 0x40000000000, /* NOT YET IMPLEMENTED */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_DICTIONARY
        MaybeSetProperties = SetProperties,
#else
        MaybeSetProperties = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ObjectMask = Argument | StringList | IParseState |
                     IExecute | Type | ComTypeList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagMask = ForceTrim | Unlock | Lock |
                   DisableOnLock | Reset | Clear |
                   MaybeSetProperties,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForceTrimMask = ForceTrimArgument | ForceTrimStringList | ForceTrimIParseState |
                        ForceTrimIExecute | ForceTrimType | ForceTrimComTypeList,

        LockMask = LockArgument | LockStringList | LockIParseState |
                   LockIExecute | LockType | LockComTypeList,

        ResetMask = ResetArgument | ResetStringList | ResetIParseState |
                    ResetIExecute | ResetType | ResetComTypeList,

        ClearMask = ClearArgument | ClearStringList | ClearIParseState |
                    ClearIExecute | ClearType | ClearComTypeList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DefaultForceTrimMask = ForceTrimArgument | ForceTrimStringList |
                               ForceTrimIParseState,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Do not automatically clear the StringList cache when under
        //       heavy load because it is very expensive to refill.
        //
        DefaultClearMask = ClearMask & ~ClearStringList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // TODO: Are these good defaults?
        //
        Default = ObjectMask | Miscellaneous | DefaultForceTrimMask |
                  ResetMask | DefaultClearMask | MaybeSetProperties
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1938facf-a73c-43d6-82d3-95a6d6cd0b63")]
    public enum HostStreamFlags
    {
        None = 0x0,                  /* No special handling. */
        Invalid = 0x1,               /* Invalid, do not use. */
        LoadedPlugins = 0x2,         /* Check loaded plugins for the stream. */
        CallingAssembly = 0x4,       /* NOT USED: Check the calling assembly for the stream. */
        EntryAssembly = 0x8,         /* Check the entry assembly for the stream. */
        ExecutingAssembly = 0x10,    /* Check the executing assembly for the stream. */
        ResolveFullPath = 0x20,      /* Resolve the file name to a fully qualified path. */
        AssemblyQualified = 0x40,    /* The returned resolved full path should include
                                      * the assembly location. */
        PreferFileSystem = 0x80,     /* Check the file system before checking any
                                      * assemblies. */
        SkipFileSystem = 0x100,      /* Skip checking the file system. */
        Script = 0x200,              /* From the script engine, etc. */
        Open = 0x400,                /* From the [open] command, etc. */
        FoundViaPlugin = 0x800,      /* Stream was opened from an assembly resource. */
        FoundViaAssembly = 0x1000,   /* Stream was opened from an assembly resource. */
        FoundViaFileSystem = 0x2000, /* Stream was opened from the file system. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        OptionMask = ResolveFullPath | AssemblyQualified | PreferFileSystem | SkipFileSystem,
        AssemblyMask = CallingAssembly | EntryAssembly | ExecutingAssembly,
        FoundMask = FoundViaPlugin | FoundViaAssembly | FoundViaFileSystem,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EngineScript = Default | ResolveFullPath | Script,
        OpenCommand = Default | Open
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("e24c641b-f0fe-4e52-b5f5-31a80acbbf52")]
    public enum ActionType
    {
        None = 0x0,             /* do nothing. */
        Invalid = 0x1,          /* invalid, do not use. */
        CheckForUpdate = 0x2,   /* check for an update; however, do not
                                 * download it. */
        FetchUpdate = 0x4,      /* check for an update and download it
                                 * if necessary. */
        RunUpdateAndExit = 0x8, /* run the external update tool and then
                                 * exit. */

        Default = CheckForUpdate
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("fd610430-d4f3-4525-b72a-e5746136884f")]
    public enum HostWriteType
    {
        None = 0,
        Invalid = 1,
        Normal = 2,
        Debug = 3,
        Error = 4,
        Flush = 5, /* Flush() only, no Write*(). */

        //
        // NOTE: External callers (i.e. those calling from outside of the core
        //       library) must NOT rely on the semantics of the "Default" value
        //       here.
        //
        Default = Normal
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("07486dfd-5b8b-41f1-ada1-264f908f0a3d")]
    public enum HostColor
    {
        Invalid = -1,
        None = Invalid
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("927338b0-c8d2-459d-9188-c85baf807990")]
    public enum Base26FormattingOption
    {
        None = 0x0,
        InsertLineBreaks = 0x1, // line break every 74 characters.
        InsertSpaces = 0x2,     // insert one space every 2 characters.

        Default = InsertLineBreaks | InsertSpaces
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6a65da98-d7ab-4ebc-a65b-a8db748340ce")]
    public enum InterpreterType
    {
        None = 0x0,
        Invalid = 0x1,
        Eagle = 0x2,
#if NATIVE && TCL
        Tcl = 0x4,
#if TCL_THREADS
        TclThread = 0x8,
#endif
#endif
        Master = 0x10,
        Slave = 0x20,
        Nested = 0x40,

        Default = Eagle | Master | Slave | Nested
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6b5ff78b-3562-4cb3-882b-2cdaf9963184")]
    public enum EventType
    {
        None = 0,                /* Do nothing. */
        Idle = 1,                /* Process any pending events. */
        Callback = 2,            /* The event represents an EventCallback
                                  * delegate. */

#if NATIVE && TCL
        Create = 3,              /* Create the interpreter. */
        Delete = 4,              /* Delete the interpreter. */
        Expression = 5,          /* Evaluate an expression (contained in the
                                  * ClientData). */
        Evaluate = 6,            /* Evaluate a script (in the triplet contained
                                  * in the ClientData). */
        SimpleEvaluate = 7,      /* Evaluate a script (contained in the
                                  * ClientData). */
        Substitute = 8,          /* Perform substitutions on a string (contained
                                  * in the ClientData). */
        Cancel = 9,              /* Cancel the script in progress (error message
                                  * contained in the ClientData). */
        Unwind = 10,             /* Unwind the script in progress (error message
                                  * contained in the ClientData). */
        ResetCancel = 11,        /* Reset the cancel and unwind flags for the Tcl
                                  * interpreter. */
        GetVariable = 12,        /* Get the value of a variable (name contained
                                  * in the ClientData). */
        SetVariable = 13,        /* Set the value of a variable (name/value pair
                                  * contained in the ClientData). */
        UnsetVariable = 14,      /* Unset a variable (name contained in the
                                  * ClientData). */
        AddCommand = 15,         /* Add an IExecute to the interpreter
                                  * (name/ICommand pair contained in the
                                  * ClientData). */
        AddStandardCommand = 16, /* Add a standard bridge between the Eagle
                                  * [eval] command and the Tcl [eagle] command. */
        RemoveCommand = 17,      /* Remove an IExecute from the interpreter
                                  * (name contained in the ClientData). */
        Dispose = 18             /* Fully dispose of all Tcl thread resources. */
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ab21fc28-02d8-4401-a88d-faafbedbd5bf")]
    public enum EngineMode
    {
        None = 0x0,
        Invalid = 0x1,
        EvaluateExpression = 0x2,
        EvaluateScript = 0x4,
        EvaluateFile = 0x8,
        SubstituteString = 0x10,
        SubstituteFile = 0x20
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7ea5444a-0ee3-4b33-bd80-be64722e86fb")]
    public enum ThreadFlags
    {
        None = 0x0,               /* No special handling. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Invalid = 0x1,            /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ThrowOnDisposed = 0x2,    /* Throw exceptions when attempting to access a
                                   * disposed interpreter. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Safe = 0x4,               /* Create interpreter as "safe". */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UserInterface = 0x8,      /* Thread must be able to use WinForms and/or WPF. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        IsBackground = 0x10,      /* Thread will not prevent the process from
                                   * terminating. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Quiet = 0x20,             /* The interpreter will be set to "quiet" mode. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoBackgroundError = 0x40, /* The background error handling for the created
                                   * interpreter will be disabled. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UseSelf = 0x80,           /* Create an aliased object named "thread" that
                                   * can be used to access the ScriptThread object
                                   * itself from inside the contained interpreter. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoCancel = 0x100,         /* Script cancellation will not cause the thread
                                   * to exit. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        StopOnError = 0x200,      /* Script errors will cause the thread to exit. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ErrorOnEmpty = 0x400,     /* An empty event queue will cause the thread to
                                   * exit. */

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        TclThread = 0x800,        /* Should native Tcl events be processed? */
        TclWaitEvent = 0x1000,    /* Wait for a Tcl event?  This should almost
                                   * never be used. */
        TclAllEvents = 0x2000,    /* Process all native Tcl events each time? */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoComplain = 0x4000,      /* Disable popup messages for errors that cannot
                                   * be reported any other way.  The errors will
                                   * still be logged to the active trace listners,
                                   * if any. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Verbose = 0x8000,         /* Enable more diagnostic output for key lifecycle
                                   * events (e.g. shutdown). */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Debug = 0x10000,          /* Enable debugging mode.  Currently, sets the
                                   * "EventFlags.Debug" flag for all events queued
                                   * via the interpreter event manager (i.e. not
                                   * those events handled directly by the engine). */

        ///////////////////////////////////////////////////////////////////////////////////////////

        UsePool = 0x20000,        /* Instead of creating a real managed thread,
                                   * use the thread pool.  This prevents various
                                   * other flags from doing anything, including the
                                   * "UserInterface", "IsBackground", and "Start"
                                   * flags. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Start = 0x40000,          /* Start the thread prior to returning from the
                                   * "Create" method instead of waiting until the
                                   * "Start" method is called. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoAbort = 0x80000,        /* Disable all use of the Thread.Abort() method. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Restricted = 0x100000,    /* Only permit use of the Send() and Queue() methods. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CommonUse = NoComplain | Start,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardUse = CommonUse | ThrowOnDisposed,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InterfaceUse = StandardUse | UserInterface,
        ServiceUse = StandardUse | NoCancel,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        TclInterfaceUse = StandardUse | UserInterface | TclThread | TclAllEvents,
        TclServiceUse = StandardUse | NoCancel | TclThread | TclAllEvents,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        TaskUse = StandardUse | UseSelf,
        SafeTaskUse = TaskUse | Safe,
        RestrictedTaskUse = TaskUse | Safe | Restricted,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AnyUse = InterfaceUse | ServiceUse | TaskUse,
        AnySafeUse = InterfaceUse | ServiceUse | TaskUse | Safe,
        AnyRestrictedUse = InterfaceUse | ServiceUse | TaskUse | Safe | Restricted,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = InterfaceUse // TODO: Good default?
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    // [Flags()]
    [ObjectId("66f29e4a-2adf-4641-956a-23505d82bbaa")]
    public enum EventPriority
    {
        Automatic = -1,         /* Automatically assign event priority based on flags. */
        None = 0x0,             /* Do not use. */
        Invalid = 0x1,          /* Do not use. */
        Highest = 0x2,          /* The highest possible event priority. */
        High = 0x4,
        Medium = 0x8,           /* The standard event priority. */
        Low = 0x10,
        Lowest = 0x20,          /* The lowest possible event priority. */

        Immediate = Highest,    /* The priority used by events that should execute
                                 * even if the event manager is not actively in a
                                 * wait operation (a.k.a. "engine events"). */
        Idle = Lowest,          /* The event priority used by [after idle]. */
        After = Medium,         /* The event priority used by [after XXXX <script>]. */
        Normal = Medium,        /* The default event priority used by QueueEvent in
                                 * auto-detection mode when none of the other event
                                 * flags match. */

        CheckEvents = Default,  /* The event priority used by by the Engine.CheckEvents
                                 * method. */
        Service = Default,      /* The event priority used by [interp service]. */
        Update = Default,       /* The event priority used by [update]. */
        WaitVariable = Default, /* The event priority used by [vwait]. */
        QueueEvent = Default,   /* The event priority used by the Interpreter.QueueEvent
                                 * method(s). */
        QueueScript = Default,  /* The event priority used by the Interpreter.QueueScript
                                 * method(s). */

        ScriptThread = Default, /* The event priority used by the ScriptThread class. */

#if NATIVE && TCL
        TclThread = Default,    /* The event priority used by the TclThread class. */
#endif

        Default = Automatic     /* The default event priority. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("39223dda-bcf9-4d91-af70-7e2c1c59d9b0")]
    public enum WatchdogOperation
    {
        None = 0x0,     /* No special handling. */
        Invalid = 0x1,  /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Check = 0x2,    /* Is the watchdog thread active? */
        Start = 0x4,    /* Start the watchdog thread. */
        Stop = 0x8,     /* Stop the watchdog thread. */
        Restart = 0x10, /* Restart the watchdog thread. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveStart = 0x200, /* Can the watchdog thread query the
                                   * interactive user? */
        ForceStart = 0x400,       /* Force watchdog thread to start even
                                   * when the timeout is infinite? */
        StrictStart = 0x800,      /* Fail starting the watchdog thread
                                   * if it is already running. */
        StrictStop = 0x1000,      /* Fail stopping the watchdog thread
                                   * if it is not running. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = InteractiveStart | ForceStart | StrictStart | StrictStop
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7344c9d6-999e-4730-9aad-0c7aee4502f7")]
    public enum EventFlags
    {
        None = 0x0,            /* No special handling. */
        Invalid = 0x1,         /* Invalid, do not use. */
        Queued = 0x2,          /* This event is being queued for execution later. */
        Direct = 0x4,          /* This event is being executed immediately. */
        UnknownThread = 0x8,   /* This event is being handled by an unknown thread (could be
                                * a worker thread, the main thread, or a thread-pool thread). */
        SameThread = 0x10,     /* This event is being handled by the same thread that
                                * created it. */
        InterThread = 0x20,    /* This event is being handled by a thread other than the
                                * one that created it. */
        Internal = 0x40,       /* This event is directed at the interpreter. */
        External = 0x80,       /* This event is not directed at the interpreter. */
        After = 0x100,         /* This event originated from the [after] command and must
                                * not be executed until a wait is initiated by a script. */
        Immediate = 0x200,     /* This event should be executed as soon as the interpreter
                                * can safely do so. */
        Idle = 0x400,          /* This event should be executed as soon as the interpreter
                                * can safely do so and is idle. */
        Synchronous = 0x800,   /* The code that created this event is blocking until it
                                * completes. */
        Asynchronous = 0x1000, /* The code that created this event queued it and continued
                                * executing. */
        Debug = 0x2000,        /* This event should produce debugging diagnostics. */
        Timing = 0x4000,       /* This event should produce timing diagnostics. */
        NoCallback = 0x8000,   /* Skip executing a callback if one exists. */
        NoIdle = 0x10000,      /* Skip processing idle events. */
        NoNotify = 0x20000,    /* Skip notifying the caller of event completion.  This is
                                * only used by the native Tcl integration subsystem. */
        IdleIfEmpty = 0x40000, /* Only process idle events if the queue is otherwise empty. */
        Interpreter = 0x80000, /* For queued scripts only, prefer the event flags from the
                                * interpreter instead of the ones provided with the script
                                * itself. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoBgError = 0x100000,     /* Skip invoking background error handler for this event. */
        FireAndForget = 0x200000, /* The IEvent object may be disposed after the event has
                                   * been serviced (i.e. the caller does NOT need to obtain
                                   * the result).  For now, this is for core library use
                                   * only.  Please do not use it. */
        WasDequeued = 0x400000,   /* The event was somehow removed from the event queue. */
        WasCompleted = 0x800000,  /* The event was executed.  This does not imply that it
                                   * was "successful". */
        WasCanceled = 0x1000000,  /* The event was canceled somehow.  This currently implies
                                   * that it MAY have been removed from the event queue as
                                   * well. */
        WasDiscarded = 0x2000000, /* The event was discarded somehow.  This currently implies
                                   * that it MAY have been removed from the event queue as
                                   * well. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        DequeueMask = After | Immediate | Idle, /* The flags that modify the event dequeuing
                                                 * behavior. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Engine = Immediate,          /* The types of events that are checked for and executed
                                      * during script evaluation and command execution (by the
                                      * engine).  This flag is designed for use by the engine
                                      * only (i.e. it is not for use by external applications
                                      * and plugins).  These events are NEVER guaranteed to be
                                      * serviced on any particular thread. */
        Service = Immediate,         /* The types of events that are checked for and executed
                                      * during ServiceEvents and related methods.  These events
                                      * are NEVER guaranteed to be serviced on any particular
                                      * thread. */
        Queue = Immediate,           /* The types of events that are queued, by default, via
                                      * QueueScript.  These events are NEVER guaranteed to be
                                      * serviced on any particular thread. */
        Wait = After | Immediate,    /* The types of events that are checked for and serviced
                                      * during the [vwait] and [update] commands.  This flag
                                      * is designed for use by [vwait] and [update] only (i.e.
                                      * it is not for use by external applications and
                                      * plugins).  Normally, [after] events are guaranteed to
                                      * be serviced on the primary thread for the interpreter;
                                      * however, that guarantee does NOT apply if external
                                      * applications and plugins use this flag. */
        All = After | Immediate,     /* Service all events.  Great care should be used with
                                      * this flag because scripts queued by [after] will not
                                      * necessarily be serviced at the next [vwait].  Also,
                                      * events are not guaranteed to be serviced on any
                                      * particular thread if this flag is used. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Engine
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("8d4d663a-6e43-4ccf-993c-238aebe4a617")]
    public enum CancelFlags
    {
        None = 0x0,                    /* Use default handling. */
        Invalid = 0x1,                 /* Invalid, do not use. */

#if NOTIFY
        Notify = 0x2,                  /* Enable notification callbacks. */
#endif

        Cancel = 0x4,                  /* Cancel the script being evaluated.
                                        * This is not currently used. */
        Unwind = 0x8,                  /* Completely unwind the script call
                                        * stack during script cancellation. */
        NeedResult = 0x10,             /* Update result parameter sent by the
                                        * caller to reflect the new interpreter
                                        * state. */
        IgnorePending = 0x20,          /* When resetting the script cancellation
                                        * state, ignore the number of pending
                                        * evaluations -OR- interactive loops. */
        FailPending = 0x40,            /* When resetting the script cancellation
                                        * state, treat pending evaluations -OR-
                                        * interactive loops as an error. */
        StopOnError = 0x80,            /* When attempting to cancel more than
                                        * one script evaluation, stop on the
                                        * first error encountered. */

#if DEBUGGER && DEBUGGER_ENGINE
        NoBreakpoint = 0x100,          /* Skip checking for any script
                                        * breakpoints. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ForDebug = 0x200,              /* For use with the [debug] command
                                        * only. */
        ForInterp = 0x400,             /* For use with the [interp] command
                                        * only. */
        ForTest2 = 0x800,              /* For use with the [test2] command
                                        * only. */
        ForTime = 0x1000,              /* For use with the [time] command
                                        * only. */
        ForTry = 0x2000,               /* For use with the [try] command
                                        * only. */
        ForCatch = 0x4000,             /* For use with the [catch] command
                                        * only. */
        ForEngine = 0x8000,            /* For use by the Engine class only. */
        ForReady = 0x10000,            /* For use by the Interpreter.Ready
                                        * method only. */
        ForInteractive = 0x20000,      /* For use by the interactive loop
                                        * only. */
        ForExternal = 0x40000,         /* For use by external (i.e. outside
                                        * of the core library) components
                                        * only. */
        ForBgError = 0x80000,          /* For use by the event manager only. */
        ForCommandCallback = 0x100000, /* For use by CommandCallback class
                                        * only. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CheckState = None,

#if NOTIFY
        ModifyState = Notify,
#else
        ModifyState = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        UnwindAndNotify = ModifyState | Unwind | ForExternal,
        IgnorePendingAndNotify = ModifyState | IgnorePending | ForExternal,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DebugHalt = ModifyState | ForDebug,
        InterpCancel = ModifyState | ForInterp,
        InterpResetCancel = ModifyState | ForInterp,
        Test2 = ModifyState | IgnorePending | ForTest2,
        Time = ModifyState | IgnorePending | ForTime,
        BgError = ModifyState | IgnorePending | ForBgError,
        CommandCallback = ModifyState | FailPending | ForCommandCallback,

        ///////////////////////////////////////////////////////////////////////////////////////////

        TryBlock = ModifyState | IgnorePending | ForTry,
        CatchBlock = ModifyState | IgnorePending | ForCatch,
        FinallyBlock = ModifyState | ForTry,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Engine = ModifyState | ForEngine,
        Ready = CheckState | NeedResult | ForReady,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveIsHalted = CheckState | NeedResult | ForInteractive,
        InteractiveManualHalt = ModifyState | ForInteractive,
        InteractiveAutomaticResetHalt = ModifyState | ForInteractive,
        InteractiveManualResetHalt = ModifyState | IgnorePending | ForInteractive,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("70cc84ac-f876-438f-a06e-fa2ef80e3bf8")]
    public enum TrustFlags
    {
        None = 0x0,            /* Use default handling. */
        Invalid = 0x1,         /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Shared = 0x2,          /* Allow other threads to use the interpreter
                                * while evaluating the script (DANGEROUS).  Use
                                * with great care, if ever. */
        WithEvents = 0x4,      /* Allow asynchronous events to be processed via
                                * the event manger while evaluating the script
                                * (DANGEROUS).  Use with great care, if ever. */
        MarkTrusted = 0x8,     /* Temporarily mark the interpreter as "trusted". */
        AllowUnsafe = 0x10,    /* Permit "trusted" evaluation even for "unsafe"
                                * interpreters (i.e. those that are already marked
                                * as "trusted"). */
        NoIgnoreHidden = 0x20, /* Do not enable execution of hidden commands. */
        ViaCoreLibrary = 0x40, /* For use by the core library only. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Temporarily mark the interpreter as "trusted" unless this is
        //       unnecessary because the interpreter is already "trusted".
        //
        MaybeMarkTrusted = MarkTrusted | AllowUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used when evaluating the Harpy / Badge security
        //       script fragments that are used to enable or disable script
        //       signing policies and core script certificates.  This is only
        //       done in response to the "-security" command line option -OR-
        //       by calling the ScriptOps.EnableOrDisableSecurity method.
        //
        SecurityPackage = MaybeMarkTrusted | ViaCoreLibrary,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None         /* WARNING: Do not change this value. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("15024598-4868-466e-a7cd-f6aa1069b4dc")]
    public enum Arity
    {
        Automatic = -3,      /* Use the value of the ArgumentsAttribute or the OperandsAttribute
                              * to determine the arity of the function or operator, respectively. */
        UnaryAndBinary = -2, /* This operator can accept one or two operands. */
        None = -1,           /* This function or operator can accept any number of arguments or
                              * operands. */
        Nullary = 0,
        Unary = 1,
        Binary = 2,
        Ternary = 3,
        Quaternary = 4,
        Quinary = 5,
        Senary = 6,
        Septenary = 7,
        Octary = 8,
        Nonary = 9
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e609aaf6-8e07-434b-ad07-758ad9e824ec")]
    public enum OperatorFlags
    {
        None = 0x0,
        Invalid = 0x1,             /* Invalid, do not use. */
        Core = 0x2,                /* This operator is part of the core
                                    * operator set. */
        Special = 0x4,             /* This operator requires special handling
                                    * by the expression engine. */
        Direct = 0x8,              /* The expression engine handles this
                                    * operator directly (i.e. without using an
                                    * IOperator class). */
        Breakpoint = 0x10,         /* Break into debugger before execution. */
        Disabled = 0x20,           /* The operator may not be executed. */
        Hidden = 0x40,             /* By default, the operator will not be
                                    * visible in the results of [info
                                    * operators]. */
        Standard = 0x80,           /* The operator is largely (or completely)
                                    * compatible with an identically named
                                    * operator from Tcl/Tk 8.4, 8.5, and/or
                                    * 8.6. */
        NonStandard = 0x100,       /* The operator is not present in Tcl/Tk
                                    * 8.4, 8.5, and/or 8.6 -OR- it is
                                    * completely incompatible with an
                                    * identically named operator in Tcl/Tk 8.4,
                                    * 8.5, and/or 8.6. */
        NoPopulate = 0x200,        /* The operator will not be returned by the
                                    * plugin manager. */
        NoTclMathOperator = 0x400, /* Disable adding the command to the
                                    * "tcl::mathop" namespace. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Arithmetic = 0x800,        /* Addition, subtraction, multiplication,
                                    * division, exponentiation, remainder. */
        Relational = 0x1000,       /* Equal to, not equal to, less than,
                                    * greater than, etc. */
        Conditional = 0x2000,      /* If-then-else, etc. */
        Logical = 0x4000,          /* Logical "and", "or", "not", "xor",
                                    * etc. */
        Bitwise = 0x8000,          /* Bitwise "and", "or", "not", "xor",
                                    * shift, rotate, etc. */
        String = 0x10000,          /* All the string-only operators. */
        List = 0x20000,            /* All the list-only operators. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        SubsetMask = Arithmetic | Relational | Conditional |
                     Logical | Bitwise | String | List,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are added to an operator when the parent interpreter
        //       is made "standard".
        //
        DisabledAndHidden = Disabled | Hidden
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("506fa37d-c5c2-4eb8-a4e0-27303cb669f0")]
    public enum MakeFlags
    {
        None = 0x0,              /* Use default handling. */
        Invalid = 0x1,           /* Invalid, do not use. */
        IncludeCommands = 0x2,   /* Disable and/or hide commands. */
        IncludeProcedures = 0x4, /* Disable and/or hide procedures. */
        IncludeFunctions = 0x8,  /* Disable and/or hide functions. */
        IncludeOperators = 0x10, /* Disable and/or hide operators. */
        IncludeVariables = 0x20, /* Disable and/or hide operators. */
        IncludeLibrary = 0x40,   /* Evaluate the associated library
                                  * script, if any. */
        ResetValue = 0x80,       /* Reset the value of all removed
                                  * variables. */

#if !MONO && NATIVE && WINDOWS
        ZeroString = 0x100,      /* Enable forcibly zeroing strings
                                  * that may contain "sensitive" data?
                                  * WARNING: THIS IS NOT GUARANTEED TO
                                  * WORK RELIABLY ON ALL PLATFORMS.
                                  * EXTREME CARE SHOULD BE EXERCISED
                                  * WHEN HANDLING ANY SENSITIVE DATA,
                                  * INCLUDING TESTING THAT THIS FLAG
                                  * WORKS WITHIN THE SPECIFIC TARGET
                                  * APPLICATION AND ON THE SPECIFIC
                                  * TARGET PLATFORM. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        ResetValueAndZeroString = ResetValue |
#if !MONO && NATIVE && WINDOWS
                                  ZeroString,
#else
                                  None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeLibrary = IncludeCommands | IncludeProcedures |
                      IncludeVariables | IncludeLibrary |
                      ResetValueAndZeroString,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeAll = SafeLibrary | IncludeFunctions,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardLibrary = IncludeCommands,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardAll = StandardLibrary | IncludeFunctions |
                      IncludeOperators,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeShell = SafeAll,

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardShell = StandardAll,

        ///////////////////////////////////////////////////////////////////////////////////////////

        SafeEvaluate = SafeAll & ~(IncludeVariables | IncludeLibrary | ResetValueAndZeroString),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("93f26486-9fd2-4705-9da6-172afd1b77e5")]
    public enum FunctionFlags
    {
        None = 0x0,
        Invalid = 0x1,              /* Invalid, do not use. */
        Core = 0x2,                 /* This function is part of the core
                                     * function set. */
        Special = 0x4,              /* This function requires special handling
                                     * by the expression engine. */
        NoPopulate = 0x8,           /* The function will not be returned by the
                                     * plugin manager. */
        ReadOnly = 0x10,            /* The function may not be modified nor
                                     * removed. */
        NoToken = 0x20,             /* Skip handling of the command token via
                                     * the associated plugin. */
        Breakpoint = 0x40,          /* Break into debugger before execution. */
        Disabled = 0x80,            /* The function may not be executed. */
        Hidden = 0x100,             /* By default, the function will not be
                                     * visible in the results of [info functions]. */
        Safe = 0x200,               /* Function is "safe" to execute for
                                     * partially trusted and/or untrusted
                                     * scripts. */
        Unsafe = 0x400,             /* Function is NOT "safe" to execute for
                                     * partially trusted and/or untrusted
                                     * scripts. */
        Standard = 0x800,           /* The function is largely (or completely)
                                     * compatible with an identically named
                                     * function from Tcl/Tk 8.4, 8.5, and/or
                                     * 8.6. */
        NonStandard = 0x1000,       /* The function is not present in Tcl/Tk
                                     * 8.4, 8.5, and/or 8.6 -OR- it is
                                     * completely incompatible with an
                                     * identically named function in Tcl/Tk
                                     * 8.4, 8.5, and/or 8.6. */
        Obsolete = 0x2000,          /* The function has been superseded and
                                     * should not be used for new development. */
        NoTclMathFunction = 0x4000, /* Disable adding the command to the
                                     * "tcl::mathfunc" namespace. */

        //
        // NOTE: This flag mask is only used for testing the core library.
        //
        ForTestUse = Obsolete | NoTclMathFunction,

        //
        // NOTE: These flags are added to a function when the parent interpreter
        //       is made "safe" or "standard".
        //
        DisabledAndHidden = Disabled | Hidden
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a227c466-4785-46ad-9000-9e3e8deff07f")]
    public enum OutputStyle
    {
        Invalid = -1,
        None = 0x0,
        ReversedText = 0x1,
        ReversedBorder = 0x2,
        Formatted = 0x4,
        Boxed = 0x8,
        Normal = 0x10,
        Debug = 0x20,
        Error = 0x40,

        FlagMask = ReversedText | ReversedBorder,
        BaseMask = Formatted | Boxed,
        TypeMask = Normal | Debug | Error,

        All = FlagMask | BaseMask | TypeMask,
        Default = Boxed | Normal
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e15a27e3-8f64-462c-95f5-21690ab6fcd1")]
    public enum PathTranslationType
    {
        None = 0x0,
        Unix = 0x1,
        Windows = 0x2,
        Native = 0x4,
        Default = Unix
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b0e68899-9337-4d3b-8691-a2f5588cc987")]
    public enum PathComparisonType
    {
        None = 0x0,
        String = 0x1,       /* Treat as plain strings with String.Compare. */
        DeepestFirst = 0x2, /* File names with more segments sort first.  Ties
                             * are broken with String.Compare. */
        DeepestLast = 0x4,  /* File names with more segments sort last.  Ties
                             * are broken with String.Compare. */
        Default = String
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("799013e1-6b95-4586-bde4-4bbfadbc986d")]
    public enum ChannelType
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Input = 0x2,
        Output = 0x4,
        Error = 0x8,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowExist = 0x10,
        AllowProxy = 0x20,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ErrorOnExist = 0x40,
        ErrorOnNull = 0x80,
        ErrorOnProxy = 0x100,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if CONSOLE
        Console = 0x200,

        FlagMask = AllowProxy | AllowExist | ErrorOnExist |
                   ErrorOnNull | ErrorOnProxy | Console,
#else
        FlagMask = AllowProxy | AllowExist | ErrorOnExist |
                   ErrorOnNull | ErrorOnProxy,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        StandardChannels = Input | Output | Error
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("de027927-2975-4c1d-98bb-53036202db09")]
    public enum TraceFlags
    {
        None = 0x0,     /* unspecified trace type, use default handling. */
        Invalid = 0x1,  /* invalid, do not use. */
        ReadOnly = 0x2, /* The trace cannot be removed. */
        Disabled = 0x4, /* The trace is disabled and will not be invoked. */
        NoToken = 0x8,  /* Skip handling of the trace token via the associated plugin. */
        Global = 0x10   /* The trace is interpreter-wide. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("734721a6-0f3d-4379-b80c-394616285bf5")]
    public enum PolicyType
    {
        None = 0x0,
        Invalid = 0x1,
        Script = 0x2,
        File = 0x4,
        Other = 0x8,
        Stream = 0x10,
        License = 0x20
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1eafc4d0-b42a-401c-a6a9-6c08940df3bf")]
    public enum PolicyFlags
    {
        None = 0x0,               /* unspecified policy type, use default handling. */
        Invalid = 0x1,            /* invalid, do not use. */
        ReadOnly = 0x2,           /* The policy cannot be removed. */
        Disabled = 0x4,           /* The policy is disabled and will not be invoked. */
        NoToken = 0x8,            /* Skip handling of the policy token via the associated plugin. */
        ForEngine = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        BeforePlugin = 0x20,      /* being invoked prior to loading a plugin assembly. */
        BeforeScript = 0x40,      /* being invoked prior to returning an IScript object
                                   * created from an external source. */
        BeforeFile = 0x80,        /* being invoked prior to reading a script file. */
        BeforeStream = 0x100,     /* being invoked prior to reading a script stream. */
        BeforeCommand = 0x200,    /* being invoked prior to the execution of a command. */
        BeforeSubCommand = 0x400, /* being invoked prior to the execution of a sub-command. */
        BeforeProcedure = 0x800,  /* being invoked prior to the execution of a procedure. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AfterFile = 0x1000,       /* being invoked after reading a script file. */
        AfterStream = 0x2000,     /* being invoked after reading a script stream. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        BeforeNoHash = 0x4000,    /* skip any hashing of the file and/or stream content. */
        AfterNoHash = 0x8000,     /* skip any hashing of the file and/or stream content. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Callback = 0x10000,       /* invokes a user callback. */
        Directory = 0x20000,      /* checks a directory against a list. */
        Script = 0x40000,         /* evaluates a user script. */
        SubCommand = 0x80000,     /* checks a sub-command against a list. */
        Type = 0x100000,          /* checks a type against a list. */
        Uri = 0x200000,           /* checks a uri against a list. */
        SplitList = 0x400000,     /* policy script is a list. */
        Arguments = 0x800000,     /* append command arguments to policy script prior to
                                   * evaluation. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        BeforeAny = BeforePlugin | BeforeScript | BeforeFile | BeforeStream |
                    BeforeCommand | BeforeSubCommand | BeforeProcedure,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AfterAny = AfterFile | AfterStream,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = BeforeNoHash | AfterNoHash,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EngineBeforePlugin = ForEngine | BeforePlugin,
        EngineBeforeScript = ForEngine | BeforeScript,
        EngineBeforeFile = ForEngine | BeforeFile | BeforeNoHash,
        EngineBeforeStream = ForEngine | BeforeStream | BeforeNoHash,
        EngineBeforeCommand = ForEngine | BeforeCommand,
        EngineBeforeSubCommand = ForEngine | BeforeSubCommand,
        EngineBeforeProcedure = ForEngine | BeforeProcedure,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EngineAfterFile = ForEngine | AfterFile,
        EngineAfterStream = ForEngine | AfterStream
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("4d68dbeb-d698-4b67-94dd-8e497b3e0e11")]
    public enum PolicyDecision
    {
        None = 0,
        Undecided = 1,
        Denied = 2,
        Approved = 3,
        Default = Denied
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e0b341df-fc31-40c9-954a-d84cb8a09217")]
    public enum UriFlags
    {
        None = 0x0,       /* No special handling. */
        Invalid = 0x1,    /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowFile = 0x2,  /* Allow URIs with the FILE scheme. */
        AllowHttp = 0x4,  /* Allow URIs with the HTTP scheme. */
        AllowHttps = 0x8, /* Allow URIs with the HTTPS scheme. */
        AllowFtp = 0x10,  /* Allow URIs with the FTP scheme. */
        NoHost = 0x20,    /* Do not query the host property. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        WasFile = 0x100,
        WasHttp = 0x200,
        WasHttps = 0x400,
        WasFtp = 0x800,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved1 = 0x1000,
        Reserved2 = 0x2000,
        Reserved3 = 0x4000,
        Reserved4 = 0x8000,
        Reserved5 = 0x10000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags only work with TryCombineUris.
        //
        UseFormat = 0x20000,     /* Use specified UriFormat. */
        Normalize = 0x40000,     /* Convert path backslash to slash. */
        PreferBaseUri = 0x80000, /* Prefer fragment from baseUri. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        LocalOnlyMask = Reserved1 | AllowFile,
        WebOnlyMask = Reserved2 | AllowHttp | AllowHttps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InsecureOnlyMask = Reserved3 | AllowHttp | AllowFtp,
        SecureOnlyMask = Reserved4 | AllowHttps,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Reserved5 | WebOnlyMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e68f81aa-f0b1-414a-8bab-1bf5626d0727")]
    public enum ExecutionPolicy : ulong
    {
        None = 0x0,                       /* Skip policy check, effectively the
                                           * same as allowing all. */
        Invalid = 0x1,                    /* Invalid, do not use. */
        Undefined = 0x2,                  /* The policy has not been explicilty
                                           * set and is not defined.  This value
                                           * should not be returned by a public
                                           * API.  Nor should it be included in
                                           * a value that is explicitly set. */
        /* Reserved2 = 0x4, */            /* Reserved for future use, do not
                                           * use. */
        /* Reserved3 = 0x8, */            /* Reserved for future use, do not
                                           * use. */
        AllowNone = 0x10,                 /* No files are allowed to be read
                                           * and/or evaluated. */
        AllowSignedOnly = 0x20,           /* Only files that are signed and
                                           * trusted are allowed to be read
                                           * and/or evaluated. */
        AllowAny = 0x40,                  /* All files are allowed to be read
                                           * and/or evaluated. */
        SkipExists = 0x80,                /* Bypass all file-exists checking
                                           * (i.e. for non-URIs). */
        ValidateXml = 0x100,              /* Validate the XML against the XSD
                                           * schema. */
        MatchSubject = 0x200,             /* Enforce the certificate subject
                                           * matching the X509 subject on the
                                           * associated assembly. */
        MatchSubjectPrefix = 0x400,       /* The X509 subject and simple
                                           * (subject) name should be matched
                                           * using prefix semantics as well. */
        MatchSubjectSimpleName = 0x800,   /* The X509 simple (subject) name
                                           * should also be checked. */
        CheckExpiry = 0x1000,             /* Enforce certificate expiration
                                           * dates. */
        CheckEntityType = 0x2000,         /* Enforce certificate entity types. */
        VerifyString = 0x4000,            /* Treat the content to be verified as
                                           * a string. */
        VerifyFile = 0x8000,              /* Treat the content to be verified as
                                           * a file. */
        CheckPublicKeyToken = 0x10000,    /* Make sure the public key tokens
                                           * match. */
        AllowAssemblyPublicKey = 0x20000, /* The public keys used to sign the
                                           * assembly may be used. */
        AllowEmbeddedPublicKey = 0x40000, /* Public keys embedded within the
                                           * assembly as resources may be used.
                                           */
        AllowRingPublicKey = 0x80000,     /* Public keys present on the trusted
                                           * key ring(s) may be used. */
        AllowAnyPublicKey = 0x100000,     /* Any public key present in the
                                           * assembly may be used; otherwise,
                                           * only the named public key may be
                                           * used. */
        TrustSignedOnly = 0x200000,       /* Files that are signed and trusted
                                           * are evaluated with full permissions,
                                           * even in a "safe" interpreter. */
        CheckDomains = 0x400000,          /* Enforce domain restrictions. */
        CheckQuantity = 0x800000,         /* Enforce certificate quantities. */
        ProtectQuantity = 0x1000000,      /* Protect certificate quantities. */
        PerMachine = 0x2000000,           /* Protect data on a per-machine basis.
                                           */
        AllowEmbedded = 0x4000000,        /* Permit the certificate to be
                                           * embedded within the data. */
        SkipFile = 0x8000000,             /* Do not check the native file system
                                           * for certificate data. */
        SkipHost = 0x10000000,            /* Do not check the interpreter host
                                           * for certificate data. */
        SkipHashName = 0x20000000,        /* Do not check the file name based on
                                           * the hash value of the contained data
                                           * for its certificate data. */
        SkipPlainName = 0x40000000,       /* Do not check the file name based on
                                           * the original file name for its
                                           * certificate data. */
        SaveApprovedData = 0x80000000,    /* Keep track of the data associated
                                           * with approved policy checks. */
        EnforceKeyUsage = 0x100000000,    /* Make sure that a key is only used
                                           * in compliance with its declared key
                                           * usage. */
        NoLoadKeyRings = 0x200000000,     /* Do not load key rings when running
                                           * the policy checks. */
        IgnoreKeyRingError = 0x400000000, /* Ignore all errors when loading key
                                           * rings. */
        ExplicitOnly = 0x800000000,       /* Do not consider any implicit policy
                                           * data. */
        PreferEmbedded = 0x1000000000,    /* Prefer embedded policy data over
                                           * external policy data. */
        SkipThisAssembly = 0x2000000000,  /* Disable special handling for the
                                           * license certificate associated with
                                           * the Harpy assembly itself. */
        SkipThisStream = 0x4000000000,    /* Disable use of the default stream
                                           * when searching for available license
                                           * certificates. */
        NoRenewKeyRings = 0x8000000000,   /* Disable loading trusted key rings
                                           * returned by the certificate renewal
                                           * server. */
        CheckRevocation = 0x10000000000,  /* Enforce checking of the revocation
                                           * list(s). */

        BasePolicyMask = AllowNone | AllowSignedOnly | AllowAny /* Mask for the
                                                                 * "base" policy
                                                                 * values. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("41dae319-8562-4466-bb7a-3f4ff0f5a620")]
    public enum CreateFlags : ulong
    {
        None = 0x0,                 /* No special creation behavior. */
        Invalid = 0x1,              /* Invalid, do not use. */
        Debug = 0x2,                /* Interpreter should run in "debug" mode? */
        Verbose = 0x4,              /* Verbose mode enabled. */
        BreakOnCreate = 0x8,        /* Break into the managed debugger immediately
                                     * on Create()? */
        Initialize = 0x10,          /* Initialize script library after creation? */
        ThrowOnDisposed = 0x20,     /* Throw exceptions when disposed objects are
                                     * accessed? */

#if DEBUGGER
        Debugger = 0x40,            /* Create script debugger? */
        DebuggerInterpreter = 0x80, /* Create isolated debugger interpreter? */
#endif

        NoGlobalNotify = 0x100,     /* Initially, disable "global" notifications. */
        NoHost = 0x200,             /* Do not create a default host? */
        NoConsole = 0x400,          /* Do not create a console host? */
        NoDisposeHost = 0x800,      /* Do not call Dispose on the host? */
        UseAttach = 0x1000,         /* Attempt to attach to existing console. */
        NoColor = 0x2000,           /* Limit colors to grayscale? */
        NoTitle = 0x4000,           /* Do not change the console title? */
        NoIcon = 0x8000,            /* Do not change the console icon? */
        NoProfile = 0x10000,        /* Do not load the host profile? */
        NoCancel = 0x20000,         /* Do not setup or teardown the script
                                     * cancellation user interface (i.e. keypress)? */
        NoUtility = 0x40000,        /* Do not load the native utility library? */
        ThrowOnError = 0x80000,     /* Throw an exception if initialization fails? */
        IgnoreOnError = 0x100000,   /* Just ignore any initialization errors? */
        StrictAutoPath = 0x200000,  /* Candidate directories for the auto_path must
                                     * exist? */
        ShowAutoPath = 0x400000,    /* Show the auto-path search information? */
        Startup = 0x800000,         /* Process startup options from the environment,
                                     * etc. */

#if TCL_WRAPPER
        TclWrapper = 0x1000000,     /* Enable Tcl wrapper mode.  Tcl interpreters
                                     * will be assumed to have been created on the
                                     * main thread. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* For embedders, more control over interpreter creation.
        //       These flags should not be combined with "Initialize" because using
        //       any of these flags will almost certainly cause script library
        //       initialization to fail.
        //

        Safe = 0x2000000,                   /* Include only "safe" commands and exclude all
                                             * "unsafe" commands from the interpreter. */
        HideUnsafe = 0x4000000,             /* Hide unsafe commands instead of excluding
                                             * them? */
        Standard = 0x8000000,               /* Include only commands that are part of the
                                             * "Tcl Standard". */
        HideNonStandard = 0x10000000,       /* Hide commands that are not part of the
                                             * "Tcl Standard"? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        NoChannels = 0x20000000,            /* Skip creating standard channels? */
        NoPlugins = 0x40000000,             /* Skip adding all standard plugins (i.e. no
                                             * commands, no object support)? */
        NoCorePlugin = 0x80000000,          /* Skip adding static core plugin (i.e. no
                                             * commands)? */

#if NOTIFY || NOTIFY_OBJECT
        NoObjectPlugin = 0x100000000,       /* Skip adding static object notify plugin
                                             * (i.e. no object reference counting)? */
#endif

#if TEST_PLUGIN || DEBUG
        NoTestPlugin = 0x200000000,         /* Skip adding static test plugin? */
#endif

#if NOTIFY && NOTIFY_ARGUMENTS
        NoTracePlugin = 0x400000000,        /* Skip adding static trace plugin? */
#endif

        NoVariables = 0x800000000,          /* Skip adding "most" standard variables? */
        NoPlatform = 0x1000000000,          /* Skip adding platform-related variables? */
        NoObjectIds = 0x2000000000,         /* Skip adding the eagle_platform(objectIds)
                                             * array element (it can be very large). */
        NoHome = 0x4000000000,              /* Skip adding env(HOME) variable (if needed)? */
        NoObjects = 0x8000000000,           /* Skip adding standard objects? */
        NoOperators = 0x10000000000,        /* Skip adding standard expression operators? */
        NoFunctions = 0x20000000000,        /* Skip adding standard math functions? */
        NoRandom = 0x40000000000,           /* Skip creating random number generator(s). */
        NoCorePolicies = 0x80000000000,     /* Skip adding built-in policies. */
        NoCoreTraces = 0x100000000000,      /* Skip adding built-in traces.  Please note that
                                             * setting this flag will disable opaque object
                                             * handle reference count tracking. */
#if NATIVE && TCL
        NoTclTransfer = 0x200000000000,     /* Skip transferring any "dead" native Tcl
                                             * resources to the new interpreter. */
#endif
        SetArguments = 0x400000000000,      /* Make sure the "argc" and "argv" variables
                                             * are set even if the supplied argument array
                                             * is null. */
        UseNamespaces = 0x800000000000,     /* Enable Tcl 8.4 compatible namespace support
                                             * for the interpreter. */
        NoLibrary = 0x1000000000000,        /* Skip core library initialization. */
        NoShellLibrary = 0x2000000000000,   /* Skip shell library initialization. */
        MeasureTime = 0x4000000000000,      /* Measure elapsed time for the Create()
                                             * method. */
        CloseConsole = 0x8000000000000,     /* First, close the console in the current
                                             * process. */
        OpenConsole = 0x10000000000000,     /* Next, open [or attach?] a console in the
                                             * current process. */
        ForceConsole = 0x20000000000000,    /* Finally, open [or attach?] a console even
                                             * if it appears to be open already. */
        AttachConsole = 0x40000000000000,   /* Allow an existing console in the parent
                                             * process to be attached. */
        CloneHost = 0x80000000000000,       /* Use a Clone(this) of the provided host,
                                             * NOT the host itself. */
        UseHostLibrary = 0x100000000000000, /* Always prefer host scripts over those on
                                             * the file system (i.e. when an embedded
                                             * script library is available). */
        TclReadOnly = 0x200000000000000,    /* Initially, put the native Tcl integration
                                             * subsystem into read-only mode. */

#if ISOLATED_PLUGINS
        IsolatePlugins = 0x400000000000000, /* Initially, force all plugins to be loaded
                                             * into isolated application domains. */
#endif

        NoPopulateOsExtra = 0x800000000000000, /* Skip asynchronously fully populating the
                                                * "tcl_platform(osExtra)" array element. */
        NoDefaultBinder = 0x1000000000000000,  /* Always skip using Type.DefaultBinder. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = 0x8000000000000000,  /* Reserved value, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for "safe" mode.
        //
        SafeAndHideUnsafe = Safe | HideUnsafe,

        //
        // NOTE: Typical flags for "standard" mode.
        //
        StandardAndHideNonStandard = Standard | HideNonStandard,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Flags to avoid changing host settings when running embedded
        //       within an existing application.
        //
        EmbeddedHostUse = NoTitle | NoIcon | NoCancel,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Common flags for embedding.
        //
        CommonUse = ThrowOnDisposed | StrictAutoPath |
#if TEST_PLUGIN && !DEBUG
                    NoTestPlugin |
#endif
#if NOTIFY && NOTIFY_ARGUMENTS
                    NoTracePlugin |
#endif
#if USE_NAMESPACES
                    UseNamespaces |
#endif
                    NoPopulateOsExtra |
                    NoDefaultBinder |
                    None,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for "DSL-style" embedding.
        //
        BareUse = CommonUse | ThrowOnError | NoCorePlugin,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        //
        // NOTE: Flags used when creating the isolated script debugger
        //       [interpreter].
        //
        DebuggerUse = Debugger,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Flags forbidden from being used for isolated script debugger
        //       interpreters.
        //
        NonDebuggerUse = Debugger | ThrowOnError,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for shell.
        //
        ShellUse = CommonUse |
#if DEBUGGER
                   DebuggerUse |
#endif
                   ThrowOnError,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL && NATIVE_PACKAGE
        //
        // NOTE: Standard flags for the interpreter used with the Eagle Package
        //       for Tcl.  Since we have no control over when, where, and which
        //       thread is used to shutdown Eagle, avoid throwing exceptions
        //       about objects already having been disposed.
        //
        NativeUse = (CommonUse & ~ThrowOnDisposed) |
#if DEBUGGER
                    DebuggerUse |
#endif
                    EmbeddedHostUse |
                    Initialize |
                    SetArguments,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for nested interps.
        //
        NestedUse = CommonUse |
#if NATIVE && TCL
                    NoTclTransfer |
#endif
                    Initialize,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        //
        // NOTE: For use with the ITclManager interface.
        //
        TclManagerUse = CommonUse | Initialize,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the ScriptOps.LoadApplicationSettings method only.
        //
        SettingsUse = CommonUse | Initialize,
        SafeSettingsUse = SettingsUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Standard flags for isolated test interps.
        //
        TestUse = CommonUse | Initialize,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for use by extensions (to other systems)
        //       that require an embedded interpreter.
        //
        EmbeddedUse = CommonUse | EmbeddedHostUse | Initialize |
                      ThrowOnError,

        SafeEmbeddedUse = EmbeddedUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for "single-use" interpreters
        //       created by the engine.
        //
        SingleUse = EmbeddedUse & ~ThrowOnError,
        SafeSingleUse = SingleUse | SafeAndHideUnsafe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for use by ScriptThread objects.
        //
        ScriptThreadUse = CommonUse |
#if DEBUGGER
                          DebuggerUse |
#endif
                          Initialize,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Default flags for all applications and plugins.
        //
        Default = ShellUse | Initialize
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("78754dfb-f802-4db2-9782-aaa3e69562a7")]
    public enum InitializeFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved = 0x2,
        Initialization = 0x4,
        Safe = 0x8,
        Test = 0x10,
        Embedding = 0x20,
        Vendor = 0x40,
        Security = 0x80,
        Shell = 0x100,
        Startup = 0x200,

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN
        MaybeEmbedding = None,
#else
        MaybeEmbedding = Embedding,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN
        MaybeVendor = None,
#else
        MaybeVendor = Vendor,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN
        MaybeSecurity = Security,
#else
        MaybeSecurity = None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Initialization | MaybeEmbedding | MaybeVendor |
                  MaybeSecurity | Shell | Startup
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("33e3eecb-1fe4-4d05-9642-fb5e187a5219")]
    public enum RuntimeOptionOperation
    {
        None = 0x0,
        Invalid = 0x1,
        Has = 0x2,
        Get = 0x4,
        Clear = 0x8,
        Add = 0x10,
        Remove = 0x20,
        Set = 0x40
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6271c3cb-0599-4c92-9d3e-916ba0a35536")]
    public enum EngineAttribute : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Name = 0x2,
        Culture = 0x4,
        Version = 0x8,
        PatchLevel = 0x10,
        Release = 0x20,
        SourceId = 0x40,
        SourceTimeStamp = 0x80,
        Configuration = 0x100,
        Tag = 0x200,
        Text = 0x400,
        TimeStamp = 0x800,
        CompileOptions = 0x1000,
        CSharpOptions = 0x2000,
        Uri = 0x4000,
        PublicKey = 0x8000,
        PublicKeyToken = 0x10000,
        ModuleVersionId = 0x20000,
        RuntimeOptions = 0x40000,
        ObjectIds = 0x80000,
        ImageRuntimeVersion = 0x100000,
        StrongName = 0x200000,
        StrongNameTag = 0x400000,
        Hash = 0x800000,
        Certificate = 0x1000000,
        UpdateBaseUri = 0x2000000,
        UpdatePathAndQuery = 0x4000000,
        DownloadBaseUri = 0x8000000,
        ScriptBaseUri = 0x10000000,
        AuxiliaryBaseUri = 0x20000000,
        TargetFramework = 0x40000000,
        NativeUtility = 0x80000000,
        InterpreterTimeStamp = 0x100000000,
        Vendor = 0x200000000,
        Default = Name,
        Reserved = 0x8000000000000000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("03316c9c-7871-4250-b28c-dc91412800ec")]
    public enum PairComparison
    {
        None = 0x0,
        Invalid = 0x1,
        LXRX = 0x2,
        LXRY = 0x4,
        LYRX = 0x8,
        LYRY = 0x10
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("2ace1c51-30ec-49a3-96a9-d453a1b2db1d")]
    public enum Priority
    {
        //
        // NOTE: All other positive integer values are also allowed in fields of this type.
        //
        Lowest = -2,
        None = -1,
        Highest = 0,
        Default = Highest
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("32619532-8777-4ddd-b430-7c867eba098e")]
    public enum Sequence
    {
        Invalid = -1,
        None = 0,

        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4,
        Fifth = 5,
        Sixth = 6,
        Seventh = 7,
        Eighth = 8,
        Nine = 9,
        Tenth = 10
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7a592738-e87b-4a8e-a0e5-730a71691f16")]
    public enum ObjectOptionType : ulong
    {
        None = 0x0,                    /* No options. */
        Invalid = 0x1,                 /* Invalid, do not use. */
        Alias = 0x2,                   /* For [object alias]. */
        Call = 0x4,                    /* For [library call]. */
        Callback = 0x8,                /* For ConversionOps.ToCommandCallback method. */
        Certificate = 0x10,            /* For [library certificate] and [object certificate]. */
        Cleanup = 0x20,                /* For [object cleanup]. */
        Create = 0x40,                 /* For [object create]. */
        Declare = 0x80,                /* For [object declare]. */
        Delegate = 0x100,              /* For the ObjectOps.InvokeDelegate method. */

#if CALLBACK_QUEUE
        Dequeue = 0x200,               /* For [callback dequeue]. */
#endif

#if XML && SERIALIZATION
        Deserialize = 0x400,           /* For [xml deserialize]. */
#endif

        Dispose = 0x800,               /* For [object dispose]. */

#if NATIVE && TCL
        Evaluate = 0x1000,             /* For [tcl eval]. */
#endif

#if PREVIOUS_RESULT
        Exception = 0x2000,            /* For [debug exception]. */
#endif

#if DATA
        Execute = 0x4000,              /* For [sql execute]. */
#endif

        FireCallback = 0x8000,         /* For the CommandCallback class. */
        FixupReturnValue = 0x10000,    /* For the MarshalOps.FixupReturnValue method(s). */
        ForEach = 0x20000,             /* For [object foreach]. */
        Get = 0x40000,                 /* For [object get]. */
        Import = 0x80000,              /* For [object import]. */
        Invoke = 0x100000,             /* For [object invoke]. */
        InvokeRaw = 0x200000,          /* For [object invokeraw]. */
        InvokeAll = 0x400000,          /* For [object invokeall]. */
        IsOfType = 0x800000,           /* For [object isoftype]. */
        Load = 0x1000000,              /* For [object load]. */
        Members = 0x2000000,           /* For [object members]. */
        Search = 0x4000000,            /* For [object search]. */

#if XML && SERIALIZATION
        Serialize = 0x8000000,         /* For [xml serialize]. */
#endif

        Type = 0x10000000,             /* For [object type]. */
        UnaliasNamespace = 0x20000000, /* For [object unaliasnamespace]. */
        Undeclare = 0x40000000,        /* For [object undeclare]. */
        Unimport = 0x80000000,         /* For [object unimport]. */
        Untype = 0x100000000,          /* For [object untype]. */

        //
        // NOTE: For the [object invoke*] sub-commands only.
        //
        ObjectInvokeOptionMask = Invoke | InvokeRaw | InvokeAll,

        //
        // NOTE: For the [library call] and [object invoke*]
        //       sub-commands only.
        //
        InvokeOptionMask = Call | ObjectInvokeOptionMask,

        //
        // NOTE: For all the [object] related sub-commands that are
        //       designed primarily to create object instances AS
        //       WELL AS their associated opaque object handles.
        //       The resulting opaque object handles may be disposed
        //       automatically (i.e. their IDispose.Dispose() method
        //       may be called just prior to their associated opaque
        //       object handles being removed from the containing
        //       interpreter context).  It should be noted here that
        //       using the "-create" flag to any of the [object]
        //       related sub-commands will trigger the automatic
        //       disposal behavior, even if that sub-command is not
        //       listed here.
        //
        CreateOptionMask = Create |
#if XML && SERIALIZATION
                           Deserialize |
#endif
                           Get |
                           Load,

        //
        // NOTE: For the [object] sub-commands only.
        //
        SubCommandMask = Alias | Certificate | Cleanup | Create |
                         Declare | Dispose | ForEach | Get |
                         Import | Invoke | InvokeRaw | InvokeAll |
                         IsOfType | Load | Members | Search |
                         Type | UnaliasNamespace | Undeclare | Unimport |
                         Untype,

        //
        // NOTE: All core marshaller related options.
        //
        ObjectMask = SubCommandMask | FixupReturnValue,

        //
        // NOTE: The default option type (i.e. [object invoke]).
        //
        Default = Invoke
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("242b38e9-3f23-4fbf-90a4-fbde0ad758af")]
    public enum OptionOriginFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Interactive = 0x2,
        CommandLine = 0x4,
        Environment = 0x8,
        Configuration = 0x10,
        Registry = 0x20,
        Default = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Override = 0x80,
        Remove = 0x100,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Any = Interactive | CommandLine | Environment |
              Configuration | Registry | Default,

        AnyOrOverride = Any | Override,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Shell = AnyOrOverride,
        Standard = AnyOrOverride,
        Plugin = AnyOrOverride,

#if NATIVE && TCL && NATIVE_PACKAGE
        NativePackage = AnyOrOverride | Remove,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
    [Flags()]
    [ObjectId("a3cceec2-cbd8-4a84-ae6c-fe2a8e0a334d")]
    public enum FindFlags : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        PreCallback = 0x2,
        PostCallback = 0x4,
        SpecificPath = 0x8,
        ScriptPath = 0x10,
        Environment = 0x20,
        AutoPath = 0x40,
        PackageBinaryPath = 0x80,
        PackageLibraryPath = 0x100,
        PackagePath = 0x200,
        EntryAssembly = 0x400,
        ExecutingAssembly = 0x800,
        BinaryPath = 0x1000,
        Registry = 0x2000,
        SearchPath = 0x4000,
        ExternalsPath = 0x8000,
        PeerPath = 0x10000,

#if UNIX
        LibraryPath = 0x20000,
        LocalLibraryPath = 0x40000,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        FindArchitecture = 0x80000,
        GetArchitecture = 0x100000,
        MatchArchitecture = 0x200000,
        RecursivePaths = 0x400000,
        ZeroComponents = 0x800000,
        RefreshAutoPath = 0x1000000,
        OverwriteBuilds = 0x2000000,
        TrustedOnly = 0x4000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ExtraNamePatternList = 0x8000000,
        PrimaryNamePatternList = 0x10000000,
        SecondaryNamePatternList = 0x20000000,
        OtherNamePatternList = 0x40000000,

        ExtraVersionPatternList = 0x80000000,
        PrimaryVersionPatternList = 0x100000000,
        SecondaryVersionPatternList = 0x200000000, /* NOT USED? */
        OtherVersionPatternList = 0x400000000, /* NOT USED? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Part0 = 0x800000000,
        Part1 = 0x1000000000,
        Part2 = 0x2000000000,
        Part3 = 0x4000000000,
        Part4 = 0x8000000000,
        Part5 = 0x10000000000,
        Part6 = 0x20000000000,
        Part7 = 0x40000000000,
        Part8 = 0x80000000000,
        Part9 = 0x100000000000,
        PartX = 0x200000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VerboseLooksLike = 0x400000000000,
        VerboseExtractBuild = 0x800000000000,
        VerboseRegistry = 0x1000000000000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        LocationMask = PreCallback | PostCallback | SpecificPath |
                       ScriptPath | Environment | AutoPath |
                       PackageBinaryPath | PackageLibraryPath | PackagePath |
                       EntryAssembly | ExecutingAssembly | BinaryPath |
                       Registry | SearchPath | ExternalsPath | PeerPath |
#if UNIX
                       LibraryPath | LocalLibraryPath |
#endif
                       None,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Architecture = FindArchitecture | GetArchitecture | MatchArchitecture,

        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = Architecture | RecursivePaths |
                    ZeroComponents | RefreshAutoPath |
                    OverwriteBuilds | TrustedOnly |
                    Part0 | Part1 | Part2 | Part3 |
                    Part4 | Part5 | Part6 | Part7 |
                    Part8 | Part9 | PartX,

        ///////////////////////////////////////////////////////////////////////////////////////////

        PatternMask = ExtraNamePatternList | PrimaryNamePatternList |
                      SecondaryNamePatternList | OtherNamePatternList |
                      ExtraVersionPatternList | PrimaryVersionPatternList |
                      SecondaryVersionPatternList | OtherVersionPatternList,

        ///////////////////////////////////////////////////////////////////////////////////////////

        VerboseMask = VerboseLooksLike | VerboseExtractBuild | VerboseRegistry,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Callback = PreCallback | PostCallback,

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = LocationMask | PatternMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Typical = All & ~(OtherNamePatternList | OtherVersionPatternList),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Standard = Typical | Architecture | TrustedOnly,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Typical
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("8b71bb91-aa6b-45e6-97ea-d9e702b3ce92")]
    public enum LoadFlags
    {
        None = 0x0,             /* no special unload flags. */
        Reserved = 0x1,         /* reserved, do not use. */
        SetDllDirectory = 0x2,  /* call SetDllDirectory prior to loading. */

#if TCL_THREADED
        IgnoreThreaded = 0x4,   /* allow non-threaded builds to be loaded. */
#endif

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("473819a9-4470-410c-b5d2-14b690e64c83")]
    public enum UnloadFlags
    {
        None = 0x0,           /* no special unload flags. */
        Reserved = 0x1,       /* reserved, do not use. */
        NoInterpThread = 0x2, /* skip Tcl interpreter thread validation. */
        NoInterpActive = 0x4, /* skip checking if the Tcl interpreter is
                               * active. */
        ReleaseModule = 0x8,  /* release the module reference for the Tcl
                               * library. */
        ExitHandler = 0x10,   /* delete the exit handler. */
        Finalize = 0x20,      /* call the Tcl_Finalize delegate if possible. */
        FreeLibrary = 0x40,   /* free the operating system module handle if the
                               * reference count reaches zero. */

        //
        // NOTE: The following composite flag values are used as
        //       specific points in the TclWrapper code.
        //
        FromExitHandler = ReleaseModule | ExitHandler,

        FromDoOneEvent = ExitHandler | Finalize | FreeLibrary,

        FromLoad = NoInterpActive | ReleaseModule | ExitHandler |
                   Finalize | FreeLibrary,

        FromThread = Default,

        //
        // NOTE: About 99.9% of all external callers should use
        //       this composite flag value [and no other values].
        //
        Default = ReleaseModule | ExitHandler | Finalize | FreeLibrary
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && LIBRARY
    [Flags()]
    [ObjectId("fd1eb082-ecbd-48c6-80d5-2ed2e06ce130")]
    public enum ModuleFlags
    {
        None = 0x0,
        NoUnload = 0x1, /* Do not call FreeLibrary when the reference
                         * count reaches zero. */
        NoRemove = 0x2  /* Do not allow the module to be removed from
                         * the interpreter. */
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("ba60001b-81d5-4e38-8dec-3fcb1b80e39d")]
    public enum LoadType /* Used with the [object load] -type option. */
    {
        Invalid = -1,         /* Reserved, do not use. */
        None = 0,             /* Does not actually load anything. */
        PartialName = 1,      /* Assembly.Load using a partial name. */
        FullName = 2,         /* Assembly.Load using the full name. */
        File = 3,             /* Assembly.LoadFrom using a path and file name. */
        Bytes = 4,            /* Assembly.Load using an array of bytes (base64 encoded). */
        Stream = 5,           /* Assembly.Load using an array of bytes (from stream). */
        Default = PartialName /* Default load type, currently by name. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("95df58aa-6b4b-4fbb-98bf-948b034f912e")]
    public enum PromptType
    {
        Invalid = -1, /* invalid, do not use. */
        None = 0,     /* no prompt will be displayed. */
        Start = 1,    /* this is the type for a normal prompt. */
        Continue = 2  /* this is the type for a continued prompt, when additional
                       * input is required to complete a script. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f76751ef-752b-4a0b-adf6-fe26384d6686")]
    public enum PromptFlags
    {
        None = 0x0,    /* no special handling, normal start or continue prompt. */
        Invalid = 0x1, /* invalid, do not use. */
        Debug = 0x8,   /* when set, this prompt is for the interactive debugger. */
        Queue = 0x10,  /* when set, this prompt is for queued (async) input mode. */
        Done = 0x20    /* when set, it means that the host successfully displayed a prompt. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("5a23c5d2-f882-4b9b-bb97-66171b9e7e7a")]
    public enum FrameResult
    {
        Invalid = -1,
        Default = 0,
        Specific = 1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("4f0a702e-5bc5-4a4f-9d4a-1f0f13fc2d23")]
    public enum TestResult
    {
        Invalid = -1,
        Unknown = 0,
        Skipped = 1,
        Failed = 2,
        Passed = 3
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("cb59bbb6-06b1-4d4a-82e1-93d1c0f54db9")]
    public enum UriEscapeType
    {
        Invalid = -1,
        None = 0,
        Uri = 1,
        Data = 2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
    [ObjectId("d1b2c836-5532-4af1-a5bd-879c3587727d")]
    public enum ControlEvent : uint /* COMPAT: Win32. */
    {
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT = 1,
        CTRL_CLOSE_EVENT = 2,
        CTRL_RESERVED1_EVENT = 3,
        CTRL_RESERVED2_EVENT = 4,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2156b4a5-731c-4eeb-99a6-796de4af31b4")]
    public enum FormatMessageFlags /* COMPAT: Win32. */
    {
        FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x100,
        FORMAT_MESSAGE_IGNORE_INSERTS = 0x200,
        FORMAT_MESSAGE_FROM_SYSTEM = 0x1000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("332c5315-7a93-44c7-9431-f4a915315a0d")]
    public enum FileAccessMask /* COMPAT: Win32. */
    {
        //
        //  Define the access mask as a longword sized structure divided up as
        //  follows:
        //
        //       3 3 2 2 2 2 2 2 2 2 2 2 1 1 1 1 1 1 1 1 1 1
        //       1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0
        //      +---------------+---------------+-------------------------------+
        //      |G|G|G|G|Res'd|A| StandardRights|         SpecificRights        |
        //      |R|W|E|A|     |S|               |                               |
        //      +-+-------------+---------------+-------------------------------+
        //
        //      typedef struct _ACCESS_MASK {
        //          WORD SpecificRights;
        //          BYTE StandardRights;
        //          BYTE AccessSystemAcl : 1;
        //          BYTE Reserved : 3;
        //          BYTE GenericAll : 1;
        //          BYTE GenericExecute : 1;
        //          BYTE GenericWrite : 1;
        //          BYTE GenericRead : 1;
        //      } ACCESS_MASK;
        //
        //  but to make life simple for programmer's we'll allow them to specify
        //  a desired access mask by simply OR'ing together mulitple single rights
        //  and treat an access mask as a DWORD.  For example
        //
        //      DesiredAccess = DELETE | READ_CONTROL
        //
        //  So we'll declare ACCESS_MASK as DWORD
        //

        NONE = 0x0,
        DELETE = 0x10000,
        READ_CONTROL = 0x20000,
        WRITE_DAC = 0x40000,
        WRITE_OWNER = 0x80000,
        SYNCHRONIZE = 0x100000,

        STANDARD_RIGHTS_REQUIRED = 0xF0000,
        STANDARD_RIGHTS_READ = READ_CONTROL,
        STANDARD_RIGHTS_WRITE = READ_CONTROL,
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL,
        STANDARD_RIGHTS_ALL = 0x1F0000,
        SPECIFIC_RIGHTS_ALL = 0xFFFF,

        //
        // AccessSystemAcl access type
        //

        ACCESS_SYSTEM_SECURITY = 0x1000000,

        //
        // MaximumAllowed access type
        //

        MAXIMUM_ALLOWED = 0x2000000,

        //
        //  These are the generic rights.
        //

        GENERIC_READ = unchecked((int)0x80000000),
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000,

        //
        // Define access rights to files and directories
        //

        FILE_NONE = 0x0,
        FILE_READ_DATA = 0x1,            // file & pipe
        FILE_LIST_DIRECTORY = 0x1,       // directory
        FILE_WRITE_DATA = 0x2,           // file & pipe
        FILE_ADD_FILE = 0x2,             // directory
        FILE_APPEND_DATA = 0x4,          // file
        FILE_ADD_SUBDIRECTORY = 0x4,     // directory
        FILE_CREATE_PIPE_INSTANCE = 0x4, // named pipe
        FILE_READ_EA = 0x8,              // file & directory
        FILE_WRITE_EA = 0x10,            // file & directory
        FILE_EXECUTE = 0x20,             // file
        FILE_TRAVERSE = 0x20,            // directory
        FILE_DELETE_CHILD = 0x40,        // directory
        FILE_READ_ATTRIBUTES = 0x80,     // all
        FILE_WRITE_ATTRIBUTES = 0x100,   // all

        FILE_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED |
                          SYNCHRONIZE |
                          0x1FF,

        FILE_GENERIC_READ = STANDARD_RIGHTS_READ |
                            FILE_READ_DATA |
                            FILE_READ_ATTRIBUTES |
                            FILE_READ_EA |
                            SYNCHRONIZE,

        FILE_GENERIC_WRITE = STANDARD_RIGHTS_WRITE |
                             FILE_WRITE_DATA |
                             FILE_WRITE_ATTRIBUTES |
                             FILE_WRITE_EA |
                             FILE_APPEND_DATA |
                             SYNCHRONIZE,

        FILE_GENERIC_EXECUTE = STANDARD_RIGHTS_EXECUTE |
                               FILE_READ_ATTRIBUTES |
                               FILE_EXECUTE |
                               SYNCHRONIZE
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6a35577d-d03a-4eb7-bda8-f876fd763a75")]
    public enum OperatingSystemId /* COMPAT: System.PlatformID. */
    {
        Win32s = PlatformID.Win32S,
        Windows9x = PlatformID.Win32Windows,
        WindowsNT = PlatformID.Win32NT,
        WindowsCE = PlatformID.WinCE,
        Unix = PlatformID.Unix,
#if NET_20_SP2 || NET_40
        Xbox = PlatformID.Xbox,     /* .NET 2.0 SP2+ only. */
        Darwin = PlatformID.MacOSX, /* .NET 2.0 SP2+ only. */
#else
        Xbox = 5,
        Darwin = 6,
#endif
        Mono_on_Unix = 128, // COMPAT: Mono.
        Unknown = unchecked((int)0xFFFFFFFF)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("caf88b71-e2b9-44ab-9807-4356759e95ef")]
    public enum ProcessorArchitecture : ushort /* COMPAT: Win32. */
    {
        Intel = 0,
        MIPS = 1,
        Alpha = 2,
        PowerPC = 3,
        SHx = 4,
        ARM = 5,
        IA64 = 6,
        Alpha64 = 7,
        MSIL = 8,
        AMD64 = 9,
        IA32_on_Win64 = 10,
        Neutral = 11,
        ARM64 = 12,
        Unknown = 0xFFFF
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("c12ed1e0-dc87-435b-8da8-a70d55341826")]
    public enum BoxCharacter
    {
        TopLeft,
        TopJunction,
        TopRight,
        Horizontal,
        Vertical,
        LeftJunction,
        CenterJunction,
        RightJunction,
        BottomLeft,
        BottomJunction,
        BottomRight,
        Space,
        Count /* Space + 1 */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if CACHE_STATISTICS
    [ObjectId("e87b3a0a-6559-4222-aad5-c2a4b6312392")]
    public enum CacheCountType
    {
        Invalid = -1,
        None = 0,
        Hit = 1,
        Miss = 2,
        Skip = 3,
        Collide = 4,
        Found = 5,
        NotFound = 6,
        Add = 7,
        Change = 8,
        Remove = 9,
        Clear = 10,
        Trim = 11,
        SizeOf = 12
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("cacfb749-6c82-44b7-bf4c-e98b86621291")]
    public enum TestInformationType
    {
        Interpreter = -12, // The parent interpreter of the one running the test.
        RepeatCount = -11, // The number of times a given test should be repeated.
        Verbose = -10,     // The test output flags (tcltest -verbose).
        Constraints = -9,  // The active test constraints.
#if DEBUGGER
        Breakpoints = -8,  // The active test breakpoints.
#endif
        Counts = -7,       // The number of times a given test has been run.
        PassedNames = -6,  // The names of the failing tests.
        SkippedNames = -5, // The names of the skipped tests.
        FailedNames = -4,  // The names of the failing tests.
        SkipNames = -3,    // The patterns of tests to skip.
        MatchNames = -2,   // The patterns of tests to run.
        Level = -1,        // The test nesting level.
        Total = 0,         // Total number of tests encountered.
        Skipped = 1,       // Total number of tests that were skipped.
        Passed = 2,        // Total number of tests that passed.
        Failed = 3,        // Total number of tests that failed.
        SizeOf = 4         // Total number of statistics that we are keeping track of.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a43b5860-1ad6-433f-940e-ef8d5cd399c2")]
    public enum HostSizeType
    {
        None = 0x0,     /* The host size type is unspecified. */
        Invalid = 0x1,  /* Invalid, do not use. */
        Any = 0x2,      /* The current -OR- maximum size of the host input/output
                         * buffer -OR- window, leaving these choices up to the
                         * underlying host.  All hosts that implement the
                         * ResetSize, GetSize, or SetSize methods should support
                         * this flag. */
        Buffer = 0x4,   /* The size of the host input/output buffer.  All hosts
                         * that implement the ResetSize, GetSize, or SetSize
                         * methods should support this flag. */
        Window = 0x8,   /* The size of the host input/output window, if any.
                         * Not all hosts will support this flag, even if they
                         * implement the ResetSize, GetSize or SetSize methods.
                         */
        Current = 0x10, /* The current size (i.e. as opposed to the maximum
                         * size) of the host input/output buffer -OR- window.
                         * All hosts that implement the ResetSize, GetSize, or
                         * SetSize methods should support this flag. */
        Maximum = 0x20, /* The maximum size (i.e. as opposed to the current
                         * size) of the host input/output buffer -OR- window.
                         * Not all hosts will support this flag, even if they
                         * implement the ResetSize, GetSize, or SetSize methods.
                         */

        BufferCurrent = Buffer | Current,
        BufferMaximum = Buffer | Maximum,

        WindowCurrent = Window | Current,
        WindowMaximum = Window | Maximum,

        Default = Any
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("3496ebd1-6e02-44b0-8527-28ae79ea403b")]
    public enum HostFlags : ulong
    {
        None = 0x0,                       /* The host has no flags set. */
        Invalid = 0x1,                    /* Invalid, do not use. */
#if ISOLATED_PLUGINS
        Isolated = 0x2,                   /* The host is apparently running in an isolated application
                                           * domain (i.e. it is not from the same application domain as
                                           * the parent interpreter). */
#endif
        Complain = 0x4,                   /* The host may be used by DebugOps.Complain(). */
        Debug = 0x8,                      /* The host may be used by DebugOps.Write(). */
        Test = 0x10,                      /* The host is operating in test mode (i.e. additional
                                           * diagnostic code may be executed during each method call). */
        Prompt = 0x20,                    /* The host supports the Prompt method. */
        ForcePrompt = 0x40,               /* The host wishes to display the prompt even when input has
                                           * been redirected. */
        Thread = 0x80,                    /* The host supports thread creation. */
        Exit = 0x100,                     /* The host supports the CanExit, CanForceExit, and Exiting
                                           * properties. */
        WorkItem = 0x200,                 /* The host supports the queueing of work items. */
        Stream = 0x400,                   /* The host supports the GetStream method. */
        Script = 0x800,                   /* The host supports the GetScript method. */
        Sleep = 0x1000,                   /* The host supports the Sleep method. */
        Yield = 0x2000,                   /* The host supports the Yield method. */
        CustomInfo = 0x4000,              /* The host supports the display of custom information via the
                                           * WriteCustomInfo method. */
        Resizable = 0x8000,               /* The host has a resizable input/output area. */
        HighLatency = 0x10000,            /* The host has a high latency (i.e. it may be performing
                                           * input/output over a poor quality remote connection). */
        LowBandwidth = 0x20000,           /* The host has low bandwidth (i.e. it may be performing
                                           * input/output over a poor quality remote connection). */
        Monochrome = 0x40000,             /* The host input/output area only supports two colors. */
        Color = 0x80000,                  /* The host input/output area supports at least the "standard"
                                           * colors (i.e. the ones from the ConsoleColor enumeration). */
        TrueColor = 0x100000,             /* The host input/output area supports at least 24-bits of
                                           * color. */
        ReversedColor = 0x200000,         /* The host input/output area supports reversing the foreground
                                           * and background colors. */
        Text = 0x400000,                  /* The host is text-based (i.e. hosted in a console window). */
        Graphical = 0x800000,             /* The host is graphical (i.e. hosted in a window that may or may
                                           * not look and behave like a console). */
        Virtual = 0x1000000,              /* The host is "virtual", meaning that it may not have any
                                           * interactive input/output area (i.e. all the input and output
                                           * may be simulated). */
        Sizing = 0x2000000,               /* The host supports getting and setting the size of its content
                                           * area.  This does not necessarily have anything to do with the
                                           * width and height of the host [window] itself. */
        Positioning = 0x4000000,          /* The host supports getting and setting positions within it. */
        Recording = 0x8000000,            /* The host supports recording commands, including interactive
                                           * commands (i.e. "demo" mode). */
        Playback = 0x10000000,            /* The host supports playing back commands, including interactive
                                           * commands (i.e. "demo" mode). */
        ZeroSize = 0x20000000,            /* There is no input/output area OR it is so small that is
                                           * unusable. */
        MinimumSize = 0x40000000,         /* The input/output area is minimal.  It may not be large enough
                                           * to display all of the "vital" header information at once. */
        CompactSize = 0x80000000,         /* The input/output area is at least large enough to display all
                                           * of the "vital" header information at once. */
        FullSize = 0x100000000,           /* The input/output area is at least large enough to display all
                                           * of the "standard" header information at once. */
        SuperFullSize = 0x200000000,      /* The input/output area is at least large enough to display all
                                           * of the "standard" header information at once. */
        JumboSize = 0x400000000,          /* The input/output area is at least large enough to display all
                                           * of the "standard" header information and some of the "extra"
                                           * header information at once. */
        SuperJumboSize = 0x800000000,     /* The input/output area is at least large enough to display all
                                           * of the "standard" header information and all of the "extra"
                                           * header information at once. */
        UnlimitedSize = 0x1000000000,     /* The input/output area should be considered to have an infinite
                                           * size. */
        MultipleLineInput = 0x2000000000, /* The input area supports multiple lines of input at the
                                           * same time. */
        AutoFlushHost = 0x4000000000,     /* Automatically flush from within WriteCore methods. */
        AutoFlushWriter = 0x8000000000,   /* Automatically flush created stream writers. */
        AutoFlushOutput = 0x10000000000,  /* Automatically flush the host output stream after [puts]. */
        AutoFlushError = 0x20000000000,   /* Automatically flush the host error stream after [puts]. */
        AdjustColor = 0x40000000000,      /* Adjust (i.e. "fine-tune") the foreground and background
                                           * colors. */
        QueryState = 0x80000000000,       /* The host supports the QueryState method. */
        ReadException = 0x100000000000,   /* An exception was thrown when reading. */
        WriteException = 0x200000000000,  /* An exception was thrown when writing. */
        NoColorNewLine = 0x400000000000,  /* Do not write a new line while the color is set to a
                                           * non-default value. */

        ExceptionMask = ReadException | WriteException,

        AllColors = Monochrome | Color | TrueColor,

        AllSizes = ZeroSize | MinimumSize | CompactSize |
                   FullSize | SuperFullSize | JumboSize |
                   SuperJumboSize | UnlimitedSize,

        Reserved = 0x8000000000000000 // NOTE: Reserved, do not use.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9f0e7e9d-6801-4fd3-a48b-ef062d8b2b8f")]
    public enum HostTestFlags
    {
        None = 0x0,          /* The host has no flags set. */
        Invalid = 0x1,       /* Invalid, do not use. */
        CustomInfo = 0x2     /* Test the custom information method. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d607b21a-e810-4d11-8a2a-f8a09aa178f7")]
    public enum HeaderFlags : ulong
    {
        None = 0x0,                    /* Displays nothing. */
        Invalid = 0x1,                 /* This value indicates that the header flags have not yet
                                        * been explicitly set by the user. */
        StopPrompt = 0x2,              /* Display debugger "[Stop]" prompts. */
        GoPrompt = 0x4,                /* Display debugger "[Go]" prompts. */
        AnnouncementInfo = 0x8,        /* Displays debugger banner (e.g. "Eagle Debugger"). */

#if DEBUGGER
        DebuggerInfo = 0x10,           /* Displays properties of the active debugger. */
#endif

        EngineInfo = 0x20,             /* Displays engine related properties of the active
                                        * interpreter. */
        ControlInfo = 0x40,            /* Displays control related properties of the active
                                        * interpreter. */
        EntityInfo = 0x80,             /* Displays per-type entity counts for the active
                                        * interpreter. */
        StackInfo = 0x100,             /* Displays native stack space information. */
        FlagInfo = 0x200,              /* Displays engine, substitution, notification, and other
                                        * flags for the active interpreter. */
        ArgumentInfo = 0x400,          /* Displays the "reason" for breaking into the debugger.
                                        * This is typically only used for breakpoints. */
        TokenInfo = 0x800,             /* Displays the specified token information.  This is
                                        * typically only used for token-based breakpoints. */
        TraceInfo = 0x1000,            /* Displays the specified variable trace information.
                                        * This is typically only used for variable watches and
                                        * traces. */
        InterpreterInfo = 0x2000,      /* Displays the state information for the active
                                        * interpreter that does not fit neatly into the other
                                        * categories. */
        CallStack = 0x4000,            /* Displays the call stack (i.e. the "evaluation stack")
                                        * for the active interpreter (current thread only). */
        CallStackInfo = 0x8000,        /* Displays the call stack (i.e. the "evaluation stack")
                                        * for the active interpreter (current thread only)
                                        * using the "boxed" style. */
        VariableInfo = 0x10000,        /* Displays properties of the specified variable. */
        ObjectInfo = 0x20000,          /* Displays properties of the specified object. */
        HostInfo = 0x40000,            /* Displays properties of the host for the active
                                        * interpreter. */
        TestInfo = 0x80000,            /* Displays test properties and statistics for the
                                        * active interpreter. */
        CallFrameInfo = 0x100000,      /* Displays properties of the specified call frame. */
        ResultInfo = 0x200000,         /* Displays the return code, string result, and error
                                        * line number for the current and previous results,
                                        * if any. */

#if PREVIOUS_RESULT
        PreviousResultInfo = 0x400000, /* Displays the return code, string result, and error
                                        * line number for the current and previous results,
                                        * if any. */
#endif

        ComplaintInfo = 0x800000,      /* Displays the previously stored "complaint" for the
                                        * active interpreter, if any. */

#if HISTORY
        HistoryInfo = 0x1000000,       /* Displays the command history for the active
                                        * interpreter, if any has been recorded. */
#endif

        OtherInfo = 0x2000000,         /* Reserved for future use. */
        CustomInfo = 0x4000000,        /* Displays custom information provided by the
                                        * underlying host implementation, if any. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        User = 0x8000000,              /* Indicates that the header flags have been explicitly
                                        * set by the user. */
        AutoSize = 0x10000000,         /* selectively displays information based on host style
                                        * (i.e. "Compact", "Full", or "Jumbo") as returned by
                                        * GetFlags(). */
        AutoRetry = 0x20000000,        /* If a call to WriteBox fails, retry it again inside
                                        * of a new section. */
        EmptySection = 0x40000000,     /* Display all the selected sections even if they
                                        * contain no meaningful content.  Typically, this flag
                                        * is only set when debugging IHost implementations. */
        EmptyContent = 0x80000000,     /* Display all the content in the selected sections,
                                        * even default and empty values. */
        Debug = 0x100000000,           /* The debugger is active in the parent interactive
                                        * loop. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        CallStackAllFrames = 0x200000000,  /* Display all call frames, not just those accessible
                                            * via scripts. */
        DebuggerBreakpoints = 0x400000000, /* Display debugger breakpoint information, if any. */
        EngineNative = 0x800000000,        /* Display [engine] native integration information, if
                                            * any. */
        HostDimensions = 0x1000000000,     /* Display interpreter host dimension information, if
                                            * any. */
        HostFormatting = 0x2000000000,     /* Display interpreter host formatting information, if
                                            * any. */
        HostColors = 0x4000000000,         /* Display interpreter host color information, if any.
                                            */
        HostNames = 0x8000000000,          /* Display interpreter host named theme information, if
                                            * any. */
        TraceCached = 0x10000000000,       /* Display interpreter trace information instead of the
                                            * specified trace information. */
        VariableLinks = 0x20000000000,     /* Display variable link information, if any. */
        VariableSearches = 0x40000000000,  /* Display variable search information, if any. */
        VariableElements = 0x80000000000,  /* Display variable array element information, if any.
                                            */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections are considered "vital" information when the
        //       script debugger calls the default host WriteHeader method.
        //
        Level1 = AnnouncementInfo |
#if DEBUGGER
                 DebuggerInfo |
#endif
                 ResultInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections are considered useful information when the
        //       script debugger calls the default host WriteHeader method.
        //
        Level2 = ControlInfo | OtherInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections may not actually be written by the default
        //       host WriteHeader method (e.g. if the associated data is
        //       null or otherwise
        //       unavailable from the current context.
        //
        Level3 = ArgumentInfo | TokenInfo | TraceInfo |
                 VariableInfo | ObjectInfo | CallFrameInfo |
#if PREVIOUS_RESULT
                 PreviousResultInfo |
#endif
                 ComplaintInfo | CustomInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections are written by the default host WriteHeader
        //       method, unless the interpreter is null -OR- from a different
        //       application domain.
        //
        Level4 = InterpreterInfo | CallStackInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These sections contain a lot of useful data; however, they
        //       are typically more useful for debugging Eagle itself rather
        //       than scripts.
        //
        Level5 = EngineInfo | FlagInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is specifically for use with the default host
        //       WriteHeader method when it is invoked due to the "#show"
        //       interactive command.
        //
        Show = Level1 | Level2 | Level3 | Level4 | Level5,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllPrompt = StopPrompt | GoPrompt,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllInfo = AnnouncementInfo |
#if DEBUGGER
                  DebuggerInfo |
#endif
                  EngineInfo | ControlInfo | EntityInfo |
                  StackInfo | FlagInfo | ArgumentInfo |
                  TokenInfo | TraceInfo | InterpreterInfo |
                  CallStackInfo | VariableInfo |
                  ObjectInfo | HostInfo | TestInfo |
                  CallFrameInfo | ResultInfo |
#if PREVIOUS_RESULT
                  PreviousResultInfo |
#endif
                  ComplaintInfo |
#if HISTORY
                  HistoryInfo |
#endif
                  OtherInfo | CustomInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllEmptyFlags = EmptySection | EmptyContent,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllHostFlags = HostDimensions | HostFormatting | HostColors |
                       HostNames,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllAutoFlags = AutoSize | AutoRetry,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllContentFlags = CallStackAllFrames | DebuggerBreakpoints | EngineNative |
                          AllHostFlags | TraceCached | VariableLinks |
                          VariableSearches | VariableElements,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllFlags = User | AllAutoFlags | AllEmptyFlags | Debug | AllContentFlags,

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = AllPrompt | CallStack | AllInfo | AllContentFlags | AllAutoFlags,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Breakpoint = AllPrompt | ArgumentInfo | TokenInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Watchpoint = AllPrompt | TraceInfo | VariableInfo,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Maximum = All & ~AutoSize | AutoRetry,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Full = Maximum & ~CallStack,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = AllAutoFlags | Level1 | Level3
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if HISTORY
    [Flags()]
    [ObjectId("21773a14-5704-467e-8df8-9137d5197404")]
    public enum HistoryFlags
    {
        None = 0x0,        /* no flags. */
        Invalid = 0x1,     /* invalid, do not use. */
        Engine = 0x2,      /* script command added via Engine. */
        Interactive = 0x4, /* interactive-only command added via REPL. */

        InstanceMask = Engine | Interactive
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
    [Flags()]
    [ObjectId("f70a8497-05dd-40a5-9b9d-93e2950aa095")]
    public enum FileShareMode /* COMPAT: Win32. */
    {
        FILE_SHARE_NONE = 0x0,
        FILE_SHARE_READ = 0x1,
        FILE_SHARE_WRITE = 0x2,
        FILE_SHARE_DELETE = 0x4,
        FILE_SHARE_READ_WRITE = FILE_SHARE_READ |
                                FILE_SHARE_WRITE,
        FILE_SHARE_ALL = FILE_SHARE_READ |
                         FILE_SHARE_WRITE |
                         FILE_SHARE_DELETE
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("ca9e0cd3-9bd3-47db-9952-79a3c132a53d")]
    public enum FileCreationDisposition /* COMPAT: Win32. */
    {
        NONE = 0,
        CREATE_NEW = 1,
        CREATE_ALWAYS = 2,
        OPEN_EXISTING = 3,
        OPEN_ALWAYS = 4,
        TRUNCATE_EXISTING = 5
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("5e06f9b4-0a39-452e-ac2d-0e6448f0863e")]
    public enum FileFlagsAndAttributes /* COMPAT: Win32. */
    {
        //
        // File attributes used with CreateFile
        //

        FILE_ATTRIBUTE_NONE = 0x0,
        FILE_ATTRIBUTE_READONLY = 0x1,
        FILE_ATTRIBUTE_HIDDEN = 0x2,
        FILE_ATTRIBUTE_SYSTEM = 0x4,
        FILE_ATTRIBUTE_DIRECTORY = 0x10,
        FILE_ATTRIBUTE_ARCHIVE = 0x20,
        FILE_ATTRIBUTE_DEVICE = 0x40,
        FILE_ATTRIBUTE_NORMAL = 0x80,
        FILE_ATTRIBUTE_TEMPORARY = 0x100,
        FILE_ATTRIBUTE_SPARSE_FILE = 0x200,
        FILE_ATTRIBUTE_REPARSE_POINT = 0x400,
        FILE_ATTRIBUTE_COMPRESSED = 0x800,
        FILE_ATTRIBUTE_OFFLINE = 0x1000,
        FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x2000,
        FILE_ATTRIBUTE_ENCRYPTED = 0x4000,

        //
        // Define the Security Quality of Service bits to be passed
        // into CreateFile
        //

        SECURITY_ANONYMOUS = 0x0,
        SECURITY_IDENTIFICATION = 0x10000,
        SECURITY_IMPERSONATION = 0x20000,
        SECURITY_DELEGATION = 0x30000,
        SECURITY_CONTEXT_TRACKING = 0x40000,
        SECURITY_EFFECTIVE_ONLY = 0x80000,
        SECURITY_SQOS_PRESENT = 0x100000,

        //
        // File creation flags must start at the high end since they
        // are combined with the attributes
        //

        FILE_FLAG_FIRST_PIPE_INSTANCE = 0x80000,
        FILE_FLAG_OPEN_NO_RECALL = 0x100000,
        FILE_FLAG_OPEN_REPARSE_POINT = 0x200000,
        FILE_FLAG_POSIX_SEMANTICS = 0x1000000,
        FILE_FLAG_BACKUP_SEMANTICS = 0x2000000,
        FILE_FLAG_DELETE_ON_CLOSE = 0x4000000,
        FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000,
        FILE_FLAG_RANDOM_ACCESS = 0x10000000,
        FILE_FLAG_NO_BUFFERING = 0x20000000,
        FILE_FLAG_OVERLAPPED = 0x40000000,
        FILE_FLAG_WRITE_THROUGH = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7f34cb91-54db-4bb1-9d11-63afd74bedb7")]
    public enum FileStatusModes /* COMPAT: POSIX. */
    {
        S_INONE = 0x0000,
        S_IEXEC = 0x0040,
        S_IWRITE = 0x0080,
        S_IREAD = 0x0100,
        S_IFDIR = 0x4000,
        S_IFREG = 0x8000,
        S_IFLNK = 0xA000,

        S_IRWX = S_IREAD | S_IWRITE | S_IEXEC
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("687b2526-63ad-4367-8a80-40441fe9396d")]
    public enum ExitCode
    {
        Success = 0,  /* COMPAT: Unix. */
        Failure = 1,  /* COMPAT: Unix. */
        Exception = 2
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9eb389ed-2d2a-4be1-823f-17717116e9cf")]
    public enum ScriptFlags : ulong
    {
        None = 0x0,                                        /* no special flags. */
        Invalid = 0x1,                                     /* invalid, do not use. */
        UseDefault = 0x2,                                  /* the engine should [also] use the default
                                                            * flags when searching for a script that
                                                            * cannot be found on the file system. */
        System = 0x4,                                      /* reserved, do not use */
        Core = 0x8,                                        /* script is part of the core language
                                                            * distribution. */
        Interactive = 0x10,                                /* script is only used by the interactive
                                                            * shell. */
        Package = 0x20,                                    /* script is part of a user package. */
        Application = 0x40,                                /* script is part of a user application. */
        Vendor = 0x80,                                     /* script is part of a vendor package. */
        User = 0x100,                                      /* script is user customizable. */
        SpecificPath = 0x200,                              /* the name should be checked verbatim if it is
                                                            * a fully qualified path. */
        Mapped = 0x400,                                    /* allow script location resolution via the
                                                            * eagle_paths script variable. */
        AutoSourcePath = 0x800,                            /* allow script location resolution via the
                                                            * auto_source_path script variable. */
        Library = 0x1000,                                  /* script has library semantics, including
                                                            * searching the package directory and the
                                                            * auto_path to find it. */
        Data = 0x2000,                                     /* script is really a data file (not strictly a
                                                            * script). */
        Test = 0x4000,                                     /* script is a test case. */
        Required = 0x8000,                                 /* script is required for proper operation. */
        Optional = 0x10000,                                /* script not required for proper operation. */
        File = 0x20000,                                    /* script resides in the specified file. */
        PreferFileSystem = 0x40000,                        /* check the file system before using the
                                                            * IHost.GetScript method for library scripts. */
        NoAutoPath = 0x80000,                              /* do not try to find the script along the
                                                            * auto_path. */
        NoHost = 0x100000,                                 /* the IHost.GetScript method should not be used
                                                            * for this request. */
        NoFileSystem = 0x200000,                           /* the core host should not look for the script
                                                            * on the native file system. */
        NoResources = 0x400000,                            /* the core host should not look for the script
                                                            * via resources, embedded or otherwise. */
        NoPlugins = 0x800000,                              /* the core host should not look for the script
                                                            * via plugin resource strings. */
        NoResourceManager = 0x1000000,                     /* the core host should not look for the script
                                                            * via the interpreter or host resource managers.
                                                            */
#if XML
        NoXml = 0x2000000,                                 /* the core host should not attempt to interpret
                                                            * the file as an XML script. */
#endif
        SkipQualified = 0x4000000,                         /* the core host should skip using the qualfied
                                                            * name as the basis for a resource name. */
        SkipNonQualified = 0x8000000,                      /* the core host should skip using the
                                                            * non-qualified name as the basis for a resource
                                                            * name. */
        SkipRelative = 0x10000000,                         /* the core host should skip using the relative
                                                            * name as the basis for a resource name. */
        SkipRawName = 0x20000000,                          /* the core host should skip treating the name
                                                            * as the raw resource name itself during the
                                                            * search. */
        SkipFileName = 0x40000000,                         /* the core host should skip treating the name
                                                            * as a file name during the search. */
        SkipFileNameOnly = 0x80000000,                     /* the core host should skip file names that
                                                            * ignore the package type during the search. */
        SkipNonFileNameOnly = 0x100000000,                 /* the core host should skip file names that
                                                            * do not ignore the package type during the
                                                            * search. */
        SkipLibraryToLib = 0x200000000,                    /* the core host should skip fixing up the
                                                            * "Library" path portion to "lib" during the
                                                            * search. */
        SkipTestsToLib = 0x400000000,                      /* the core host should skip fixing up the
                                                            * "Tests" path portion to "lib/Tests" during
                                                            * the search. */
        StrictGetFile = 0x800000000,                       /* null must be returned if the script file is
                                                            * not found on the file system */
        ErrorOnEmpty = 0x1000000000,                       /* forbid null and empty script values even upon
                                                            * success unless they are flagged as optional
                                                            * and NOT flagged as required. */
        FailOnException = 0x2000000000,                    /* unexpected exceptions should cause the
                                                            * remainder of the script search to be canceled
                                                            * and an error to be returned. */
        StopOnException = 0x4000000000,                    /* unexpected exceptions should cause the
                                                            * remainder of the script search to be canceled.
                                                            */
        FailOnError = 0x8000000000,                        /* unexpected errors should cause the remainder
                                                            * of the script search to be canceled and an
                                                            * error to be returned. */
        StopOnError = 0x10000000000,                       /* unexpected errors should cause the remainder
                                                            * of the script search to be canceled. */
        Silent = 0x20000000000,                            /* return minimum error information if a script
                                                            * cannot be found. */
        Quiet = 0x40000000000,                             /* return normal error information if a script
                                                            * cannot be found. */
        Verbose = 0x80000000000,                           /* return maximum error information if a script
                                                            * cannot be found. */
        PreferDeepFileNames = 0x100000000000,              /* prefer file names that are longer. */
        PreferDeepResourceNames = 0x200000000000,          /* prefer resource names that are longer. */
        SearchDirectory = 0x400000000000,                  /* when searching for a script on the file
                                                            * system, consider candidate locations that
                                                            * represent a directory. */
        SearchFile = 0x800000000000,                       /* when searching for a script on the file
                                                            * system, consider candidate locations that
                                                            * represent a file. */
        NoLibraryFile = 0x1000000000000,                   /* the file system should be disallowed when
                                                            * searching for the core script library. */
        ClientData = 0x2000000000000,                      /* the IClientData has been filled in with
                                                            * auxiliary data (e.g. the associated plugin
                                                            * file name). */
        LibraryPackage = 0x4000000000000,                  /* the script is part of the core script
                                                            * library. */
        TestPackage = 0x8000000000000,                     /* the script is part of the core script
                                                            * library test package. */
        AutomaticPackage = 0x10000000000000,               /* the script is part of the core script
                                                            * library -OR- test package. */
        FilterOnSuffixMatch = 0x20000000000000,            /* avoid checking resource names that do not
                                                            * fit within the suffix of the specified
                                                            * name. */
        NoTrace = 0x40000000000000,                        /* prevent the core host GetScriptTrace and
                                                            * FilterScriptResourceNamesTrace methods
                                                            * from emitting diagnostics. */
        NoPolicy = 0x80000000000000,                       /* Disable policy execution when looking up
                                                            * the script? */
        NoAssemblyManifest = 0x100000000000000,            /* the core host should not look for the
                                                            * script via manifest assembly resources.
                                                            */
        NoLibraryFileNameOnly = 0x200000000000000,         /* do not check the file system for just the
                                                            * file name portion of the requested library
                                                            * script. */
        NoPluginResourceName = 0x400000000000000,          /* skip calling the IPlugin.GetString method
                                                            * for the plugin-name decorated resource
                                                            * name. */
        NoRawResourceName = 0x800000000000000,             /* skip calling the IPlugin.GetString method
                                                            * for the raw, undecorated resource name. */
        NoHostResourceManager = 0x1000000000000000,        /* the core host should not look for the script
                                                            * via the host resource manager.
                                                            */
        NoApplicationResourceManager = 0x2000000000000000, /* the core host should not look for the script
                                                            * via the application resource manager.
                                                            */
        NoLibraryResourceManager = 0x4000000000000000,     /* the core host should not look for the script
                                                            * via the core library resource manager.
                                                            */
        NoPackagesResourceManager = 0x8000000000000000,    /* the core host should not look for the script
                                                            * via the core packages resource manager.
                                                            */

        //
        // NOTE: When using resource names, forbid all names that are
        //       not an exact match.
        //
        ExactNameOnly = SkipLibraryToLib | SkipTestsToLib | SkipFileName |
                        SkipRelative | SkipNonQualified,

        //
        // NOTE: Only consider library resources from the core library
        //       assembly.
        //
        CoreAssemblyOnly = NoFileSystem | NoPlugins | NoHostResourceManager |
                           NoApplicationResourceManager | NoLibraryFile |
                           NoAssemblyManifest | NoLibraryFileNameOnly,

        //
        // NOTE: If the "locked down" build configuration is in use, force
        //       all scripts to be fetched from the core library assembly.
        //
#if ENTERPRISE_LOCKDOWN
        MaybeCoreAssemblyOnly = CoreAssemblyOnly,
#else
        //
        // WARNING: This value cannot be the same as "None".  Do not modify
        //          or various things may break.
        //
        MaybeCoreAssemblyOnly = Invalid,
#endif

        //
        // NOTE: Even if this module has been built with an embedded
        //       script library, we still want to allow the application
        //       (or the user) to override the various embedded library
        //       scripts by placing the correct file(s) in the proper
        //       location(s) on the native file system.  This flag has
        //       no effect unless the "Library" flag is also specified.
        //
#if EMBEDDED_LIBRARY
        EmbeddedLibrary = PreferFileSystem | AutomaticPackage,
#else
        EmbeddedLibrary = AutomaticPackage,
#endif

        SearchAny = SpecificPath | SearchDirectory | SearchFile,

        RequiredFile = Required | StrictGetFile | SearchAny,
        OptionalFile = Optional | StrictGetFile | SearchAny,

        CoreRequiredFile = Core | RequiredFile | EmbeddedLibrary,
        CoreLibraryRequiredFile = CoreRequiredFile | Library | MaybeCoreAssemblyOnly,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CoreLibrarySecurityRequiredFile =
#if XML
            NoXml |
#endif
            CoreRequiredFile | Library |
#if EMBEDDED_LIBRARY
            CoreAssemblyOnly,
#else
            None,
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////

        CoreOptionalFile = Core | OptionalFile | EmbeddedLibrary,
        CoreLibraryOptionalFile = CoreOptionalFile | Library,

        PackageOptionalFile = Package | OptionalFile | EmbeddedLibrary,
        PackageLibraryOptionalFile = PackageOptionalFile | Library,

        PackageRequiredFile = Package | RequiredFile | EmbeddedLibrary,
        PackageLibraryRequiredFile = PackageRequiredFile | Library,

        ApplicationRequiredFile = Application | RequiredFile | EmbeddedLibrary,
        ApplicationLibraryRequiredFile = ApplicationRequiredFile | Library,

        ApplicationOptionalFile = Application | OptionalFile | EmbeddedLibrary,
        ApplicationLibraryOptionalFile = ApplicationOptionalFile | Library,

        VendorRequiredFile = Vendor | RequiredFile | EmbeddedLibrary,
        VendorLibraryRequiredFile = VendorRequiredFile | Library,

        VendorOptionalFile = Vendor | OptionalFile | EmbeddedLibrary,
        VendorLibraryOptionalFile = VendorOptionalFile | Library,

        UserRequiredFile = User | RequiredFile | EmbeddedLibrary,
        UserLibraryRequiredFile = UserRequiredFile | Library,

        UserOptionalFile = User | OptionalFile | EmbeddedLibrary,
        UserLibraryOptionalFile = UserOptionalFile | Library,

        UseDefaultGetScriptFile = ApplicationRequiredFile, // COMPAT: Eagle (legacy).

        Default = UseDefault
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7349287b-88d3-40d1-9c63-1100774aff17")]
    public enum ScriptBlockFlags
    {
        None = 0x0,            /* no special handling. */
        Invalid = 0x1,         /* invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        OkOrReturnOnly = 0x2,  /* only "Ok" And "Return" will be considered
                                * as successful return codes.
                                */
        AllowExceptions = 0x4, /* allow exceptional return codes.  this has
                                * no effect if the OkOrReturnOnly flag is
                                * used.
                                */
        TrimSpace = 0x8,       /* trim all surrounding whitespace from all
                                * successful script block results.
                                */
        EmitErrors = 0x10,     /* include any script block errors in the
                                * generated output.  these are errors from
                                * the script evaluation itself.
                                */
        StopOnError = 0x20,    /* stop further processing if a script block
                                * results in an error.
                                */
        EmitFailures = 0x40,   /* include any script block failures in the
                                * generated output.  these are errors that
                                * prevent the script block from actually
                                * being processed, e.g. parsing issues.
                                */
        StopOnFailure = 0x80,  /* stop further processing if a script block
                                * cannot be processed, e.g. due to a parse
                                * failure.
                                */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = unchecked((int)0x80000000), /* reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Under normal conditions, this is the combination of flags
        //       that should be used.
        //
        Standard = OkOrReturnOnly | AllowExceptions | TrimSpace |
                   EmitErrors | StopOnError | EmitFailures |
                   StopOnFailure,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the combination of flags that can be used to force
        //       processing to continue even if script evaluation errors or
        //       parsing failures are encountered.
        //
        Relaxed = Standard & ~(StopOnError | StopOnFailure),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("cb195945-70af-4c06-8e81-dc694ed4182a")]
    public enum Boolean : byte /* COMPAT: Tcl. */
    {
        False = 0,
        True = 1,
        No = 0,
        Yes = 1,
        Off = 0,
        On = 1,
        Disable = 0,
        Enable = 1,
        Disabled = 0,
        Enabled = 1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("435e1a60-3960-4512-9cb9-eecb0439c4da")]
    public enum TestOutputType
    {
        None = 0x0,            /* nothing. */
        Invalid = 0x1,         /* invalid, do not use. */
        AutomaticWrite = 0x2,  /* Interpreter: automatic handling of the test output
                                * to write. */
        AutomaticReturn = 0x4, /* Interpreter: automatic handling of the test output
                                * to return. */
        AutomaticLog = 0x8,    /* Interpreter: automatic handling of the test output
                                * to log. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Body = 0x10,           /* COMPAT: tcltest (body of failed tests). */
        B = Body,              /* Same as "Body". */
        Pass = 0x20,           /* COMPAT: tcltest (when a test passes). */
        P = Pass,              /* Same as "Pass". */
        Skip = 0x40,           /* COMPAT: tcltest (when a test is skipped). */
        S = Skip,              /* Same as "Skip". */
        Start = 0x80,          /* COMPAT: tcltest (when a test starts). */
        T = Start,             /* Same as "Start". */
        Error = 0x100,         /* COMPAT: tcltest (errorCode/errorInfo on
                                * failure). */
        E = Error,             /* Same as "Error". */
        Line = 0x200,          /* COMPAT: tcltest (source file line info on
                                * failure). */
        L = Line,              /* Same as "Line". */
        Fail = 0x400,          /* Eagle: when a test fails. */
        F = Fail,              /* Same as "Fail". */
        Reason = 0x800,        /* Eagle: failure mode and details (code
                                * mismatch, result mismatch, output mismatch,
                                * etc). */
        R = Reason,            /* Same as "Reason". */
        Time = 0x1000,         /* Eagle: setup/body/cleanup timing. */
        I = Time,              /* Same as "Time". */
        Exit = 0x2000,         /* Eagle: isolated process exit code detail. */
        X = Exit,              /* Same as "Exit". */
        StdOut = 0x4000,       /* Eagle: isolated process standard output. */
        O = StdOut,            /* Same as "StdOut". */
        StdErr = 0x8000,       /* Eagle: isolated process standard error. */
        D = StdErr,            /* Same as "StdErr". */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Automatic = AutomaticWrite | AutomaticReturn | AutomaticLog,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Legacy = Pass | Body | Skip |
                 Start | Error, /* pbste */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Standard = Pass | Body | Skip |
                   Start | Error | Line, /* pbstel */

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = Standard | Fail | Reason |
              Time | Exit | StdOut |
              StdErr,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = Automatic | All
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("da180459-c94a-4a04-9753-d6f1448964fa")]
    public enum TestPathType
    {
        None = 0x0,    /* unspecified. */
        Invalid = 0x1, /* invalid, do not use. */
        Library = 0x2, /* core library specific tests? */
        Plugins = 0x4, /* run plugin specific tests? */
        Tests = 0x8,   /* generic tests */

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("5f9a351b-67bb-48e1-ae17-15c4dd6e1c41")]
    public enum IsolationDetail
    {
        None = 0x0,
        Invalid = 0x1,
        Lowest = 0x2,
        Low = 0x4,
        Medium = 0x8,
        High = 0x10,
        Highest = 0x20,

        Default = Medium
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b60aaf1f-b159-4c6e-8e95-fe10eb40da0f")]
    public enum IsolationLevel
    {
        None = 0x0,
        Invalid = 0x1,
        Interpreter = 0x2,
        AppDomain = 0x4,
        AppDomainOrInterpreter = 0x8,
        Process = 0x10,
        Session = 0x20,
        Machine = 0x40,
#if ISOLATED_INTERPRETERS
        Maximum = AppDomain,
#else
        Maximum = AppDomainOrInterpreter,
#endif
        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("879a3ffc-59dc-43ae-9bad-8c58ada1206b")]
    public enum OptionBehaviorFlags
    {
        None = 0x0,                     /* No special option behavior. */
        Invalid = 0x1,                  /* Invalid, do not use. */
        ValidateLookups = 0x2,          /* Use the Validate flag for all entity
                                         * lookups. */
        ErrorOnEndOfOptions = 0x4,      /* Raise an error if an end-of-options
                                         * indicator is encountered when
                                         * unexpected. */
        StopOnEndOfOptions = 0x8,       /* Stop processing options if an
                                         * end-of-options indicator is encountered
                                         * when unexpected. */
        IgnoreOnEndOfOptions = 0x10,    /* Ignore end-of-options indicator if
                                         * encountered when unexpected. */
        SkipOnEndOfOptions = 0x20,      /* Skip to the next argument after an
                                         * unexpected end-of-options indicator
                                         * (i.e. assume a name/value pair).  This
                                         * flag has no effect unless the
                                         * IgnoreOnEndOfOptions flag is also set. */
        ErrorOnListOfOptions = 0x40,    /* Raise an error if an list-of-options
                                         * indicator is encountered when
                                         * unexpected. */
        StopOnListOfOptions = 0x80,     /* Stop processing options if an
                                         * list-of-options indicator is encountered
                                         * when unexpected. */
        IgnoreOnListOfOptions = 0x100,  /* Ignore list-of-options indicator if
                                         * encountered when unexpected. */
        SkipOnListOfOptions = 0x200,    /* Skip to the next argument after an
                                         * unexpected list-of-options indicator
                                         * (i.e. assume a name/value pair).  This
                                         * flag has no effect unless the
                                         * IgnoreOnListOfOptions flag is also set. */
        ErrorOnUnknownOption = 0x400,   /* Raise an error if an unknown option is
                                         * encountered. */
        StopOnUnknownOption = 0x800,    /* Stop processing options if an unknown
                                         * one is encountered. */
        IgnoreOnUnknownOption = 0x1000, /* Ignore unknown options if they are
                                         * encountered. */
        SkipOnUnknownOption = 0x2000,   /* Skip the next argument after an unknown
                                         * option (i.e. assume a name/value pair).
                                         * This flag has no effect unless the
                                         * IgnoreOnUnknown flag is also set. */
        ErrorOnNonOption = 0x4000,      /* Raise an error if a non-option is
                                         * encountered when unexpected (i.e. it
                                         * is not simply an option value). */
        StopOnNonOption = 0x8000,       /* Stop processing options if a non-option
                                         * is encountered when unexpected (i.e. it
                                         * is not simply an option value). */
        IgnoreOnNonOption = 0x10000,    /* Ignore the argument if a non-option
                                         * is encountered when unexpected (i.e. it
                                         * is not simply an option value). */
        SkipOnNonOption = 0x20000,      /* Skip to the next argument after a
                                         * non-option is encountered when
                                         * unexpected (i.e. it is not simply an
                                         * option value).  This flag has no effect
                                         * unless the IgnoreOnNonOption flag is
                                         * also set. */
        LastIsNonOption = 0x40000,      /* The last argument cannot be an option;
                                         * therefore, always stop if it is hit
                                         * prior to stopping for another reason. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the behavior needed by the CheckOptions method.
        //
        CheckOptions = StopOnEndOfOptions | IgnoreOnUnknownOption | IgnoreOnNonOption,

        //
        // NOTE: This is the value for the old "strict" mode behavior.
        //
        Strict = ErrorOnEndOfOptions | ErrorOnUnknownOption | StopOnNonOption,

        //
        // NOTE: This is the value for the old "non-strict" mode behavior.
        //
        Default = StopOnEndOfOptions | StopOnUnknownOption | StopOnNonOption
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("64fb0580-d2c6-4e7e-a6f4-300144472520")]
    public enum OptionFlags : ulong
    {
        None = 0x0,                              // Regular name-only option, no special handling.
        Invalid = 0x1,                           // Invalid, do not use.
        Present = 0x2,                           // Option was found while parsing arguments.
        Strict = 0x4,                            // Use strict option value processing semantics?
        Verbose = 0x8,                           // Error messages should contain more details?
        NoCase = 0x10,                           // Option is not case-sensitive.
        Unsafe = 0x20,                           // Option is not allowed in "safe" interpreters.
        System = 0x40,                           // Option was added automatically by the system.
        AllowInteger = 0x80,                     // Integers are allowed for an enumeration option.
        MustHaveValue = 0x100,                   // Value must have value after name (string, int, etc).
        MustBeBoolean = 0x200,                   // Value must convert to bool via GetBoolean.
        MustBeSignedByte = 0x400,                // Value must convert to int via GetSignedByte2.
        MustBeByte = 0x800,                      // Value must convert to int via GetByte2.
        MustBeNarrowInteger = 0x1000,            // Value must convert to int via GetInteger2.
        MustBeUnsignedNarrowInteger = 0x2000,    // Value must convert to int via GetUnsignedInteger2.
        MustBeInteger = 0x4000,                  // Value must convert to int via GetInteger2.
        MustBeUnsignedInteger = 0x8000,          // Value must convert to int via GetUnsignedInteger2.
        MustBeWideInteger = 0x10000,             // Value must convert to wideInt (long) via GetWideInteger2.
        MustBeUnsignedWideInteger = 0x20000,     // Value must convert to wideInt (long) via GetWideInteger2.
        MustBeIndex = 0x40000,                   // Value must be an int or end[<+|-><int>].
        MustBeLevel = 0x80000,                   // Value must be an int or #<int>.
        MustBeReturnCode = 0x100000,             // Value must be a ReturnCode or int.
        MustBeEnum = 0x200000,                   // Value must convert to the specified Enum type.
        MustBeEnumList = 0x400000,               // Value must be an EnumList object.
        MustBeGuid = 0x800000,                   // Value must convert to System.Guid.
        MustBeDateTime = 0x1000000,              // Value must convert to System.DateTime.
        MustBeTimeSpan = 0x2000000,              // Value must convert to System.DateTime.
        MustBeList = 0x4000000,                  // SplitList on value must succeed.
        MustBeDictionary = 0x8000000,            // Must have an even number of list elements.
        MustBeMatchMode = 0x10000000,            // Value must be "exact", "glob", or "regexp".
        MustBeValue = 0x20000000,                // Value must convert to some value via GetValue.
        MustBeObject = 0x40000000,               // Value must be an opaque object handle.
        MustBeInterpreter = 0x80000000,          // Value must be an opaque interpreter handle.
        MustBeType = 0x100000000,                // Value must be a System.Type object.
        MustBeTypeList = 0x200000000,            // Value must be a TypeList object.
        MustBeAbsoluteUri = 0x400000000,         // Value must be a System.Uri object.
        MustBeVersion = 0x800000000,             // Value must be a System.Version object.
        MustBeReturnCodeList = 0x1000000000,     // Value must be a ReturnCodeList object.
        MustBeAlias = 0x2000000000,              // Value must be an IAlias object.
        MustBeOption = 0x4000000000,             // Value must be an IOption object.
        MustBeAbsoluteNamespace = 0x8000000000,  // Value must be an INamespace object.
        MustBeRelativeNamespace = 0x10000000000, // Value must be an INamespace object.

#if NATIVE && TCL
        MustBeTclInterpreter = 0x20000000000,    // Value must be a Tcl interpreter.
#endif

        MustBeSecureString = 0x40000000000,      // Value must be a SecureString object.
        MustBeEncoding = 0x80000000000,          // Value must be an Encoding object.
        MustBePlugin = 0x100000000000,           // Value must be an IPlugin object.
        MustBeExecute = 0x200000000000,          // Value must be an IExecute object.

        //
        // NOTE: Special option flags.
        //

        EndOfOptions = 0x400000000000,           // This is the end-of-options marker, stop and do not process.
        ListOfOptions = 0x800000000000,          // This is the list-of-options marker, stop and show the
                                                 // available options, returning an error.

        ///////////////////////////////////////////////////////////////////////////////////////////

        MustBeEnumMask = MustBeEnum | MustBeEnumList,

        MustBeMask = MustBeBoolean | MustBeSignedByte | MustBeByte |
                     MustBeNarrowInteger | MustBeUnsignedNarrowInteger | MustBeInteger |
                     MustBeUnsignedInteger | MustBeWideInteger | MustBeUnsignedWideInteger |
                     MustBeIndex | MustBeLevel | MustBeReturnCode |
                     MustBeEnum | MustBeEnumList | MustBeGuid |
                     MustBeDateTime | MustBeTimeSpan | MustBeList |
                     MustBeDictionary | MustBeMatchMode | MustBeValue |
                     MustBeObject | MustBeInterpreter | MustBeType |
                     MustBeTypeList | MustBeAbsoluteUri | MustBeVersion |
                     MustBeReturnCodeList | MustBeAlias | MustBeOption |
                     MustBeAbsoluteNamespace | MustBeRelativeNamespace |
#if NATIVE && TCL
                     MustBeTclInterpreter |
#endif
                     MustBeSecureString | MustBeEncoding | MustBePlugin |
                     MustBeExecute,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Composite flags provided for shorthand.
        //

        MustHaveExecuteValue = MustHaveValue | MustBeExecute,
        MustHavePluginValue = MustHaveValue | MustBePlugin,
        MustHaveEncodingValue = MustHaveValue | MustBeEncoding,
        MustHaveSecureStringValue = MustHaveValue | MustBeSecureString,
        MustHaveBooleanValue = MustHaveValue | MustBeBoolean,
        MustHaveSignedByteValue = MustHaveValue | MustBeSignedByte,
        MustHaveByteValue = MustHaveValue | MustBeByte,
        MustHaveNarrowIntegerValue = MustHaveValue | MustBeNarrowInteger,
        MustHaveUnsignedNarrowIntegerValue = MustHaveValue | MustBeUnsignedNarrowInteger,
        MustHaveIntegerValue = MustHaveValue | MustBeInteger,
        MustHaveUnsignedIntegerValue = MustHaveValue | MustBeUnsignedInteger,
        MustHaveWideIntegerValue = MustHaveValue | MustBeWideInteger,
        MustHaveUnsignedWideIntegerValue = MustHaveValue | MustBeUnsignedWideInteger,
        MustHaveLevelValue = MustHaveValue | MustBeLevel,
        MustHaveReturnCodeValue = MustHaveValue | MustBeReturnCode,
        MustHaveDateTimeValue = MustHaveValue | MustBeDateTime,
        MustHaveTimeSpanValue = MustHaveValue | MustBeTimeSpan,
        MustHaveEnumValue = AllowInteger /* COMPAT: Eagle beta. */ | MustHaveValue | MustBeEnum,
        MustHaveEnumListValue = AllowInteger | MustHaveValue | MustBeEnumList,
        MustHaveListValue = MustHaveValue | MustBeList,
        MustHaveDictionaryValue = MustHaveValue | MustBeDictionary,
        MustHaveMatchModeValue = MustHaveValue | MustBeMatchMode,
        MustHaveAnyValue = MustHaveValue | MustBeValue,
        MustHaveObjectValue = MustHaveValue | MustBeObject,
        MustHaveInterpreterValue = MustHaveValue | MustBeInterpreter,
        MustHaveTypeValue = MustHaveValue | MustBeType,
        MustHaveTypeListValue = MustHaveValue | MustBeTypeList,
        MustHaveAbsoluteUriValue = MustHaveValue | MustBeAbsoluteUri,
        MustHaveVersionValue = MustHaveValue | MustBeVersion,
        MustHaveReturnCodeListValue = MustHaveValue | MustBeReturnCodeList,
        MustHaveAliasValue = MustHaveValue | MustBeAlias,
        MustHaveOptionValue = MustHaveValue | MustBeOption,
        MustHaveAbsoluteNamespaceValue = MustHaveValue | MustBeAbsoluteNamespace,
        MustHaveRelativeNamespaceValue = MustHaveValue | MustBeRelativeNamespace,

#if NATIVE && TCL
        MustHaveTclInterpreterValue = MustHaveValue | MustBeTclInterpreter,
#endif

        MatchOldValueType = 0x800000000000000,  // Old option value must be string or enum.
        Ignored = 0x1000000000000000,           // This option should not be processed.
        Disabled = 0x2000000000000000,          // This option is currently disabled (error).
        Unsupported = 0x4000000000000000,       // This option is not supported by this engine.
        Reserved = 0x8000000000000000           // Reserved, do not use.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("c0fea691-2825-4dc4-940e-adfd35aed8ce")]
    public enum InterruptType
    {
        None = 0x0,
        Invalid = 0x1,
        Canceled = 0x2,
        Unwound = 0x4,
        Halted = 0x8,
        Deleted = 0x10
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7067e9bb-8903-4fc0-b712-83eb26fa5a02")]
    public enum BreakpointType : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Reserved1 = 0x2,
        Reserved2 = 0x4,
        Reserved3 = 0x8,
        Reserved4 = 0x10,

        SingleStep = 0x20,
        MultipleStep = 0x40,

        Demand = 0x80,
        Token = 0x100,
        Identifier = 0x200,

        Cancel = 0x400, /* NOTE: Special due to Interpreter.Ready(). */
        Unwind = 0x800, /* NOTE: Special due to Interpreter.Ready(). */

        Error = 0x1000,
        Return = 0x2000,
        Test = 0x4000,
        Exit = 0x8000,

        Evaluate = 0x10000,   /* NOTE: Only used together with "Exit". */
        Substitute = 0x20000, /* NOTE: Only used together with "Exit". */

        BeforeText = 0x40000,
        AfterText = 0x80000,
        BeforeBackslash = 0x100000,
        AfterBackslash = 0x200000,
        BeforeUnknown = 0x400000,
        AfterUnknown = 0x800000,

        BeforeExpression = 0x1000000,
        AfterExpression = 0x2000000,
        BeforeIExecute = 0x4000000,
        AfterIExecute = 0x8000000,
        BeforeCommand = 0x10000000,
        AfterCommand = 0x20000000,
        BeforeSubCommand = 0x40000000,
        AfterSubCommand = 0x80000000,
        BeforeOperator = 0x100000000,
        AfterOperator = 0x200000000,
        BeforeFunction = 0x400000000,
        AfterFunction = 0x800000000,
        BeforeProcedure = 0x1000000000,
        AfterProcedure = 0x2000000000,
        BeforeProcedureBody = 0x4000000000,
        AfterProcedureBody = 0x8000000000,
        BeforeLambdaBody = 0x10000000000,
        AfterLambdaBody = 0x20000000000,
        BeforeVariableExist = 0x40000000000,         /* NOTE: Not yet implemented. */
        BeforeVariableGet = 0x80000000000,
        BeforeVariableSet = 0x100000000000,
        BeforeVariableReset = 0x200000000000,
        BeforeVariableUnset = 0x400000000000,
        BeforeVariableAdd = 0x800000000000,
        BeforeVariableArrayNames = 0x1000000000000,  /* NOTE: Not yet implemented. */
        BeforeVariableArrayValues = 0x2000000000000, /* NOTE: Not yet implemented. */

        BeforeVariableStandard = BeforeVariableGet | BeforeVariableSet | BeforeVariableUnset,

        BeforeVariable = BeforeVariableExist | BeforeVariableGet | BeforeVariableSet |
                         BeforeVariableReset | BeforeVariableUnset | BeforeVariableAdd |
                         BeforeVariableArrayNames | BeforeVariableArrayValues,

        EngineCancel = Reserved1 | Cancel | Unwind,
        EngineCode = Reserved2 | Error | Return,
        EngineTest = Reserved3 | Test,
        EngineExit = Reserved4 | Exit | Evaluate | Substitute,

        Text = BeforeText | AfterText,
        Backslash = BeforeBackslash | AfterBackslash,
        Unknown = BeforeUnknown | AfterUnknown,
        Expression = BeforeExpression | AfterExpression,
        IExecute = BeforeIExecute | AfterIExecute,
        Command = BeforeCommand | AfterCommand,
        Operator = BeforeOperator | AfterOperator,
        Function = BeforeFunction | AfterFunction,
        Procedure = BeforeProcedure | AfterProcedure,
        ProcedureBody = BeforeProcedureBody | AfterProcedureBody,
        Lambda = BeforeLambdaBody | AfterLambdaBody,

        BeforeStep = BeforeText | BeforeBackslash | BeforeUnknown |
                     BeforeExpression | BeforeIExecute | BeforeCommand |
                     BeforeSubCommand | BeforeOperator | BeforeFunction |
                     BeforeProcedure | BeforeLambdaBody,

        AfterStep = AfterText | AfterBackslash | AfterUnknown |
                    AfterExpression | AfterIExecute | AfterCommand |
                    AfterSubCommand | AfterOperator | AfterFunction |
                    AfterProcedure | AfterLambdaBody,

        VariableStep = BeforeVariableExist | BeforeVariableGet | BeforeVariableSet |
                       BeforeVariableReset | BeforeVariableUnset | BeforeVariableAdd |
                       BeforeVariableArrayNames | BeforeVariableArrayValues,

        Common = Demand | Identifier | EngineCancel |
                 EngineTest | EngineExit | BeforeStep |
                 VariableStep,

        Standard = Common | Token,

        Ready = Cancel | Unwind,

        //
        // NOTE: No tokens, no expressions (too noisy).
        //
        Express = Common & ~(Expression | Operator | Function),

        //
        // NOTE: The default breakpoint types.
        //
        Default = Express,

        //
        // NOTE: All possible breakpoint types.
        //
        All = SingleStep | MultipleStep | Demand |
              Token | Identifier | EngineCancel |
              EngineCode | EngineTest | EngineExit |
              BeforeStep | AfterStep | VariableStep
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("9b351955-a5dd-4306-b6c2-d510d332c353")]
    public enum MatchMode
    {
        None = 0x0,               // Do nothing.
        Invalid = 0x1,            // Invalid, do not use.

        Callback = 0x2,           // (application/plugin defined)
        Exact = 0x4,              // (e.g. [string equal], etc)
        SubString = 0x8,          // (e.g. "starts with", etc)
        Glob = 0x10,              // (e.g. [string match], etc)
        RegExp = 0x20,            // (e.g. [regexp], etc)
        Integer = 0x40,           // (e.g. [switch], etc)

        Substitute = 0x80,        // (e.g. [switch], etc)
        Expression = 0x100,       // (e.g. [test1], [test2], etc)

        NoCase = 0x200,           // Ignore case-sensitivity.
        ForceCase = 0x400,        // Force case-sensitivity.
        SubPattern = 0x800,       // (e.g. {a,b,c} syntax)
        EmptySubPattern = 0x1000, // Permit empty sub-patterns.

        ModeMask = Callback | Exact | SubString |
                   Glob | RegExp | Integer,

        FlagMask = Substitute | Expression | NoCase |
                   ForceCase | SubPattern | EmptySubPattern
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("040e4c44-a268-4d36-99d0-cc3c80373681")]
    public enum LevelFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Absolute = 0x2,  /* allow levels from the global frame inward. */
        Relative = 0x4,  /* allow levels from the current frame outward. */
        Invisible = 0x8, /* include otherwise invisible call frames. */

        //
        // NOTE: Tcl compatible call frame search semantics.
        //
        Default = Absolute | Relative,

        //
        // NOTE: Tcl compatible call frame search semantics
        //       with Eagle extensions (allows "invisible"
        //       call frames to be seen).
        //
        All = Absolute | Relative | Invisible
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
    [ObjectId("2d4b5503-ea8b-409f-bddd-a586c53ecb82")]
    public enum DbResultFormat
    {
        None = 0,
        RawArray = 1,
        RawList = 2,
        Array = 3,
        List = 4,
        Dictionary = 5,
        NestedList = 6,
        NestedDictionary = 7,
        DataReader = 8,
        Default = Array /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("8e169a10-f996-4ccf-add2-219e330ae1c4")]
    public enum DateTimeBehavior
    {
        None = 0,
        Ticks = 1,         /* Number of 100-nanosecond units since the start-of-time,
                            * 00:00:00.0000000 January 1st, 0001. */
        Seconds = 2,       /* Number of seconds since the standard Unix epoch,
                            * 00:00:00.0000000 January 1st, 1970. */
        ToString = 3,      /* Convert to a string using the specified or default
                            * format. */
        Default = ToString /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("21275a46-4292-4c42-ac70-9875dd6c2b2f")]
    public enum DbVariableFlags
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowNone = 0x2,
        AllowSelect = 0x4,
        AllowInsert = 0x8,
        AllowUpdate = 0x10,
        AllowDelete = 0x20,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowRead = AllowNone | AllowSelect,

        AllowWrite = AllowNone | AllowInsert | AllowUpdate |
                     AllowDelete,

        ///////////////////////////////////////////////////////////////////////////////////////////

        AllowAll = AllowRead | AllowWrite,
        Default = AllowAll
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("f517d99f-5f81-4663-8aec-0e071568d36d")]
    public enum DbExecuteType
    {
        None = 0,
        NonQuery = 1,
        Scalar = 2,
        Reader = 3,
        ReaderAndCount = 4,
        Default = NonQuery /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    //
    // TODO: Add support for more database backends here.
    //
    [ObjectId("5c7e322d-2044-42f0-9545-7f05b80f14dc")]
    public enum DbConnectionType
    {
        None = 0,
        Odbc = 1,
        OleDb = 2,
        Oracle = 3,
        Sql = 4,
        SqlCe = 5,
        SQLite = 6, /* COMPAT: Branding. */
        Other = 7,
        Default = Sql /* TODO: Good default? */
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("753458ab-2ef9-4934-83d7-f8a96effc9cc")]
    public enum IdentifierKind : ulong
    {
        None = 0x0,
        Invalid = 0x1,
        Interpreter = 0x2,
        PolicyData = 0x4,
        Policy = 0x8,
        TraceData = 0x10,
        Trace = 0x20,
        AnyIExecute = 0x40,
        CommandData = 0x80,
        Command = 0x100,
        HiddenCommand = 0x200,
        SubCommandData = 0x400,
        SubCommand = 0x800,
        ProcedureData = 0x1000,
        Procedure = 0x2000,
        HiddenProcedure = 0x4000,
        IExecute = 0x8000,
        HiddenIExecute = 0x10000,
        LambdaData = 0x20000,
        Lambda = 0x40000,
        OperatorData = 0x80000,
        Operator = 0x100000,
        FunctionData = 0x200000,
        Function = 0x400000,
        EnsembleData = 0x800000,
        Ensemble = 0x1000000,
        Variable = 0x2000000,
        CallFrame = 0x4000000,
        PackageData = 0x8000000,
        Package = 0x10000000,
        PluginData = 0x20000000,
        Plugin = 0x40000000,
        ObjectData = 0x80000000,
        Object = 0x100000000,
        ObjectTypeData = 0x200000000,
        ObjectType = 0x400000000,
        Option = 0x800000000,
        NativeModule = 0x1000000000,
        NativeDelegate = 0x2000000000,
        HostData = 0x4000000000,
        Host = 0x8000000000,
        AliasData = 0x10000000000,
        Alias = 0x20000000000,
        DelegateData = 0x40000000000,
        Delegate = 0x80000000000,
        Callback = 0x100000000000,
        Resolve = 0x200000000000,
        ResolveData = 0x400000000000,
        ClockData = 0x800000000000,
        Script = 0x1000000000000,
        ScriptBuilder = 0x2000000000000,
        NamespaceData = 0x4000000000000,
        Namespace = 0x8000000000000,
        InteractiveLoopData = 0x10000000000000,
        ShellCallbackData = 0x20000000000000,
        KeyPair = 0x40000000000000,
        Certificate = 0x80000000000000,
        KeyRing = 0x100000000000000
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a7a543b2-f7a1-408a-84ec-06bcf541c6c8")]
    public enum StreamFlags
    {
        None = 0x0,
        Invalid = 0x1,              /* Invalid, do not use. */
        PreventClose = 0x2,         /* When set, reject all Close and Dispose requests. */
        SawCarriageReturn = 0x4,    /* A carriage-return has been seen while processing input. */
        NeedLineFeed = 0x8,         /* A line-feed is needed while processing input. */
        UseAnyEndOfLineChar = 0x10, /* Any end-of-line character can terminate an input line. */
        Socket = 0x20,              /* The stream is a socket. */
        Client = 0x40,              /* The stream contains a client socket. */
        Server = 0x80,              /* The stream contains a server socket. */
        Listen = 0x100,             /* The stream contains a listen socket. */

        ListenSocket = Socket | Listen,
        ServerSocket = PreventClose | Socket | Server
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("0e36e577-a722-4a9b-bbe0-08340cab5bd3")]
    public enum FileSearchFlags
    {
        None = 0x0,                 /* no special handling. */
        Invalid = 0x1,              /* invalid, do not use. */
        SpecificPath = 0x2,         /* check the specified name verbatim (this only
                                     * applies if the name is fully qualified). */
        Mapped = 0x4,               /* search interpreter path map (first). */
        AutoSourcePath = 0x8,       /* search the auto_source_path (second). */
        Current = 0x10,             /* search the current directory (third). */
        User = 0x20,                /* search user-specific locations. */
        Externals = 0x40,           /* search the externals directory. */
        Application = 0x80,         /* search application-specific locations. */
        ApplicationBase = 0x100,    /* search application base directory locations. */
        Vendor = 0x200,             /* also search vendor locations. */
        Strict = 0x400,             /* return null if no existing file is found. */
        Unix = 0x800,               /* use Unix directory separators. */
        DirectoryLocation = 0x1000, /* allow candidate location to be a directory. */
        FileLocation = 0x2000,      /* allow candidate location to be a file. */

        Standard = SpecificPath | Mapped | AutoSourcePath |
                   User | Externals | Application |
                   ApplicationBase | Vendor | DirectoryLocation |
                   FileLocation,

        StandardAndStrict = Standard | Strict,

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("021d5251-a399-4275-98c8-14d543020237")]
    public enum StreamDirection
    {
        None = 0x0,
        Invalid = 0x1,
        Input = 0x2,
        Output = 0x4,
        Both = Input | Output
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("dcc9ce82-31c2-4366-bcbd-be8e049d0c47")]
    public enum StreamTranslation
    {
        //
        // NOTE: These names are referred to directly from scripts, please do not change.
        //
        binary = 0,
        lf = 1,
        cr = 2,
        crlf = 3,
        platform = 4,
        auto = 5,
        environment = 6,
        protocol = 7 /* cr/lf is required by numerous Internet protocols */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("721b5d85-d2de-42df-9f66-440f9efe2c97")]
    public enum NamespaceFlags
    {
        None = 0x0,
        Invalid = 0x1,

        Qualified = 0x2,
        Absolute = 0x4,
        Global = 0x8,

        Wildcard = 0x10,

        Command = 0x20,
        Variable = 0x40,

        QualifierMask = Qualified | Absolute | Global,
        PatternMask = Wildcard,
        EntityMask = Command | Variable,

        SplitNameMask = QualifierMask | PatternMask | EntityMask
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ba1fcf45-9ce8-4d7a-9158-fb5c4f345bd2")]
    public enum ScriptDataFlags
    {
        None = 0x0,               /* No special handling. */
        Invalid = 0x1,            /* Invalid, do not use. */
        UseSafeInterpreter = 0x2, /* Create a "safe" interpreter. */
        UseStaticOnly = 0x4,      /* Script only contains static data that uses
                                   * the [set] command. */
        CopyScalars = 0x8,        /* All scalar variables should be copied to
                                   * the resulting dictionary. */
        CopyArrays = 0x10,        /* All array variables should be copied to
                                   * the resulting dictionary. */
        ExistingOnly = 0x20,      /* When merging the settings, only consider
                                   * those that already existed. */
        ErrorOnScalar = 0x40,     /* When this flag is set and the associated
                                   * "copy" flag is not set, an error will be
                                   * returned if a scalar variable is found.
                                   */
        ErrorOnArray = 0x80,      /* When this flag is set and the associated
                                   * "copy" flag is not set, an error will be
                                   * returned if an array variable is found.
                                   */
        DisableSecurity = 0x100,  /* Prevent the "Security" flag from being
                                   * set in the interpreter initialization
                                   * flags. */
        UseTrustedUri = 0x200,    /* Make sure to trust the public key(s) that
                                   * are associated with the software update
                                   * SSL certificate(s) prior to attempting to
                                   * evaluate the settings file.  This will use
                                   * save/restore semantics on the associated
                                   * trust state.
                                   */

        Minimum = CopyScalars | CopyArrays | ExistingOnly,
        Medium = Minimum | UseSafeInterpreter,
        Maximum = Medium | UseStaticOnly
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /* [Flags()] */
    [ObjectId("747766b6-abe9-4c44-890e-94585ea3cbd2")]
    public enum ReturnCode : int /* COMPAT: Tcl. */
    {
        Invalid = -16, /* NOTE: This return code is reserved as "invalid",
                        *       please do not use it. */

#if false
        ConvertNoSpace = -4,   /* COMPAT: Tcl, these values can be returned by */
        ConvertUnknown = -3,   /*         Tcl_ExternalToUtf and Tcl_UtfToExternal in */
        ConvertSyntax = -2,    /*         addition to TCL_OK. */
        ConvertMultiByte = -1,
#endif

        Ok = 0,       /* COMPAT: Tcl, these five "standard" return code values */
        Error = 1,    /*         are straight from "generic/tcl.h" and are used */
        Return = 2,   /*         by the TclWrapper as well as script engine */
        Break = 3,    /*         itself, please do not change them. */
        Continue = 4,

        //
        // NOTE: If either of these bits are set, it indicates a custom
        //       return code is being provided by the user command.
        //
        CustomOk = 0x20000000,    /* COMPAT: HRESULT. */
        CustomError = 0x40000000, /* COMPAT: HRESULT. */

        //
        // NOTE: The high-bit is reserved, please do not use it.
        //
        Reserved = unchecked((int)0x80000000) /* COMPAT: HRESULT. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f185d6de-8e60-4c80-90e8-7569d5f02b23")]
    public enum FilePermission
    {
        None = 0x0,
        Execute = 0x1, /* COMPAT: Unix. */
        Write = 0x2,   /* COMPAT: Unix. */
        Read = 0x4,    /* COMPAT: Unix. */
        Invalid = 0x4000000,
        Exists = 0x8000000,
        NotExists = 0x10000000,
        Directory = 0x20000000,
        File = 0x40000000,
        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2752be1d-e3a3-42c7-992a-ea249b0161a6")]
    public enum MapOpenAccess /* COMPAT: C + POSIX */
    {
        //
        // NOTE: These names are referred to directly from scripts, please do not change.
        //
        Default = RdOnly,                 /* default handling */
        RdOnly = 0x0,                     /* open for reading only */
        WrOnly = 0x1,                     /* open for writing only */
        RdWr = 0x2,                       /* open for reading and writing */
        Append = 0x8,                     /* writes done at eof */
        SeekToEof = 0x10,                 /* seek to eof at open */
        Creat = 0x100,                    /* create and open file */
        Trunc = 0x200,                    /* open and truncate */
        Excl = 0x400,                     /* open only if file doesn't already exist */
        R = RdOnly,                       /* open for reading only; file must already exist; this is
                                           * the default */
        RPlus = RdWr,                     /* open for reading and writing; file must already exist */
        W = WrOnly | Creat | Trunc,       /* open for writing only; truncate if it exists, otherwise,
                                           * create new file */
        WPlus = RdWr | Creat | Trunc,     /* open for reading and writing; truncate if it exists;
                                           * otherwise, create new file */
        A = WrOnly | Creat | Append,      /* open for writing only; if it doesn't exist, create new
                                           * file, writes done at eof */
        APlus = RdWr | Creat | SeekToEof, /* open for reading and writing; if it doesn't exist, create
                                           * new file, initial position is eof */
        RdWrMask = RdOnly | WrOnly | RdWr /* mask of possible read/write modes */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6fad8db8-ecf0-4662-a6a1-c2930ff3acea")]
    public enum MapSeekOrigin
    {
        //
        // NOTE: These names are referred to directly from scripts, please do not change.
        //
        Begin = SeekOrigin.Begin,
        Start = SeekOrigin.Begin,
        Current = SeekOrigin.Current,
        End = SeekOrigin.End
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f83e7bd1-a0b2-4f5c-99d3-a5a315735031")]
    public enum SettingFlags
    {
        None = 0x0,          /* No special handling. */
        Invalid = 0x1,       /* Invalid, do not use. */
        CurrentUser = 0x2,   /* Enable reading and writing of per-user
                              * settings. */
        LocalMachine = 0x4,  /* Enable reading and writing of per-machine
                              * settings. */
        AnySecurity = 0x8,   /* Search all groups of settings, even when they
                              * may not be applicable to the current user. */
        UserSecurity = 0x10, /* Check the permissions of the current user to
                              * select which group of settings will be read
                              * or written by the current operation. */
        LowSecurity = 0x20,  /* Only read and/or write setting values within
                              * the group of settings that are writable by
                              * all users. */
        HighSecurity = 0x40, /* Only read and/or write setting values within
                              * the group of settings that are writable by
                              * users with administrator access. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = CurrentUser | LocalMachine | AnySecurity |
              UserSecurity | LowSecurity | HighSecurity,

        Legacy = CurrentUser | LocalMachine | HighSecurity,

        Default = Legacy
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("12692407-91a1-44ce-af30-5d29bdd8a265")]
    public enum ValueFlags : ulong
    {
        //
        // NOTE: The various flags in this enumeration
        //       control the behavior of the Value class
        //       when it attempts to interpret a given
        //       string as a number (or other supported
        //       type).
        //
        None = 0x0,
        Invalid = 0x1,

        //
        // NOTE: The available number bases.
        //
        BinaryRadix = 0x2,
        OctalRadix = 0x4,
        DecimalRadix = 0x8,
        HexadecimalRadix = 0x10,

        //
        // NOTE: The available integral types.
        //
        Boolean = 0x20,
        Byte = 0x40,
        Character = 0x80,
        String = 0x100,
        NarrowInteger = 0x200,
        Integer = 0x400,
        WideInteger = 0x800,

        //
        // NOTE: The available enumerated types.  These
        //       are considered to be distinct and are
        //       treated specially from the [integral]
        //       types above, even though they actually
        //       are integral types (primarily because
        //       the "fallback" semantics of the Value
        //       class functions are not designed to
        //       automatically convert a string to a
        //       value of an enumerated type, e.g. via
        //       GetNumber).
        //
        ReturnCode = 0x1000,
        MatchMode = 0x2000,

        //
        // NOTE: The available fixed-point types.
        //
        Decimal = 0x4000,

        //
        // NOTE: The available floating-point types.
        //
        Single = 0x8000,
        Double = 0x10000,

        //
        // NOTE: Some kind of basic numeric value.  This
        //       includes all booleans, integers (both signed
        //       and/or unsigned), fixed-point, and floating
        //       point.
        //
        Number = 0x20000,

        //
        // NOTE: The available miscellaneous types.
        //
        DateTime = 0x40000,
        DateTimeFormat = 0x80000, /* fixup DateTime format string */
        TimeSpan = 0x100000,
        Guid = 0x200000,
        Object = 0x400000, /* opaque object handle */

        //
        // NOTE: For use by Value.GetIndex only.
        //
        NamedIndex = 0x800000,
        WithOffset = 0x1000000,

        //
        // NOTE: For use with Value.GetNestedObject and Value.GetNestedMember only.
        //
        StopOnNullType = 0x2000000,
        StopOnNullObject = 0x4000000,
        StopOnNullMember = 0x8000000,

        StopOnNullMask = StopOnNullType | StopOnNullObject | StopOnNullMember,

        //
        // NOTE: Extra flags to control the type conversion
        //       behavior of various methods of the Value
        //       class.
        //
        Fast = 0x10000000,
        AllowInteger = 0x20000000,
        IgnoreLeading = 0x40000000, /* NOT USED */
        IgnoreTrailing = 0x80000000, /* NOT USED */
        Strict = 0x100000000,
        Verbose = 0x200000000,
        ShowName = 0x400000000,
        FullName = 0x800000000,
        NoCase = 0x1000000000,
        NoNested = 0x2000000000,
        NoNamespace = 0x4000000000,
        NoAssembly = 0x8000000000,
        NoException = 0x10000000000,
        NoComObject = 0x20000000000,
        AllowBooleanString = 0x40000000000,

        //
        // NOTE: Extra informational flags to indicate when a signed
        //       or unsigned integral number is being parsed.  These
        //       flags are not used in calls to parse the "default"
        //       signedness for a base integral type.
        //
        Signed = 0x100000000000,
        Unsigned = 0x200000000000,

        //
        // NOTE: Extra flags to control whether signed and/or
        //       unsigned variations are allowed when processing
        //       integral numbers in the decimal radix.
        //
        DefaultSignedness = 0x400000000000,
        NonDefaultSignedness = 0x800000000000,

        AllowSigned = 0x1000000000000,
        AllowUnsigned = 0x2000000000000,

        SignednessMask = DefaultSignedness | AllowSigned | AllowUnsigned,

        //
        // NOTE: The default value flags used by [object search]
        //       when calling GetType.
        //
        ObjectSearch = ShowName,

        //
        // NOTE: Either an integer or a wide integer.
        //
        IntegerOrWideInteger = Integer | WideInteger,

        //
        // NOTE: Used to mask off characters.
        //
        NonCharacterMask = ~Character,

        //
        // NOTE: Used to mask off things that are not to be
        //       considered as "real numbers".
        //
        NonRealMask = ~(Boolean | Character),

        //
        // NOTE: Allow booleans and their textual strings.
        //
        TclBoolean = Boolean | AllowBooleanString,

        //
        // NOTE: These are the types (more-or-less) handled
        //       by the GetNumeric method.
        //
        NumericMask = TclBoolean | Byte | NarrowInteger |
                      Integer | WideInteger | Decimal |
                      Single | Double | Number,

        //
        // NOTE: Useful combinations of the above flags,
        //       some are used by the engine and some are
        //       provided for convenience.
        //
        AnyRadix = BinaryRadix | OctalRadix | DecimalRadix |
                   HexadecimalRadix,

        AnySignedness = AllowSigned | AllowUnsigned,

        AnyIntegral = TclBoolean | Byte | Character |
                      NarrowInteger | Integer | WideInteger,

        AnyIntegralNonCharacter = AnyIntegral & NonCharacterMask,

        AnyBoolean = AnyIntegralAnyRadix,
        AnyStrictBoolean = AnyIntegralAnyRadix | Strict,

        AnyByte = AnyIntegralAnyRadix,

        AnyCharacter = AnyIntegralAnyRadix,

        AnyNarrowInteger = AnyIntegralAnyRadix,

        AnyInteger = AnyIntegralAnyRadix,

        AnyWideInteger = AnyIntegralAnyRadix,

        AnyFixedPoint = Decimal,

        AnyFloatingPoint = Single | Double,

        AnyReal = AnyNumber & NonRealMask,

        AnyNumber = AnyIntegral | AnyFixedPoint | AnyFloatingPoint | Number,

        AnyIntegralAnyRadix = AnyRadix | AnyIntegral | DefaultSignedness,
        AnyIntegralAnyRadixAnySignedness = AnyRadix | AnyIntegral | AnySignedness,

        AnyRealAnyRadix = AnyRadix | AnyReal | DefaultSignedness,
        AnyRealAnyRadixAnySignedness = AnyRadix | AnyReal | AnySignedness,

        AnyNumberAnyRadix = AnyRadix | AnyNumber | DefaultSignedness,
        AnyNumberAnyRadixAnySignedness = AnyRadix | AnyNumber | AnySignedness,

        AnyDateTime = AnyIntegralAnyRadix | DateTime | DateTimeFormat,
        AnyStrictDateTime = AnyDateTime | Strict,

        AnyTimeSpan = AnyIntegralAnyRadix | TimeSpan,
        AnyStrictTimeSpan = AnyTimeSpan | Strict,

        AnyReturnCode = AnyIntegralAnyRadix | ReturnCode,

        AnyMatchMode = AnyIntegralAnyRadix | MatchMode,
        AnyStrictMatchMode = AnyMatchMode | Strict,

        AnyVariant = AnyRadix | AnyNumber | DateTime |
                     DateTimeFormat | TimeSpan | Object |
                     DefaultSignedness,

        AnyVariantAnySignedness = AnyRadix | AnyNumber | DateTime |
                                  DateTimeFormat | TimeSpan | AnySignedness,

        AnyIndex = AnyRadix | TclBoolean | Integer |
                   NamedIndex | WithOffset | DefaultSignedness,

        AnyIndexAnySignedness = AnyRadix | TclBoolean | Integer |
                                NamedIndex | WithOffset | AnySignedness,

        AnyNonCharacter = Any & NonCharacterMask,

        Any = AnyVariant | String | Guid,
        AnyStrict = Any | Strict
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("19c16abe-025f-44cb-af23-10cb924f024d")]
    public enum ParseError
    {
        Success = 0,
        ExtraAfterCloseQuote = 1,
        ExtraAfterCloseBrace = 2,
        MissingBrace = 3,
        MissingBracket = 4,
        MissingParenthesis = 5,
        MissingQuote = 6,
        MissingVariableBrace = 7,
        Syntax = 8,
        BadNumber = 9
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("120473c9-ca35-4446-99a8-f7b5c1be6e31")]
    public enum TokenType
    {
        None = 0x0,
        Invalid = 0x1,
        Word = 0x2,
        SimpleWord = 0x4,
        Text = 0x8,
        Backslash = 0x10,
        Command = 0x20,
        Variable = 0x40,
        SubExpression = 0x80,
        Operator = 0x100,
        Function = 0x200,
        Separator = 0x400
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("4acc4d6d-ab07-498c-b7db-07781da4e427")]
    public enum TokenFlags
    {
        None = 0x0,
        Invalid = 0x1,

#if DEBUGGER
        Breakpoint = 0x2,
#endif

        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2ca81723-b85d-4bec-96f9-817c21e5cec0")]
    public enum TokenSyntaxType
    {
        None = 0x0,
        Invalid = 0x1,
        WhiteSpace = 0x2,       /* the token is whitespace.  not currently supported by the parser. */
        Comment = 0x4,          /* the token is a comment.  not currently supported by the parser. */
        CommandName = 0x8,      /* the token is the first argument to a command (i.e. the command name). */
        Argument = 0x10,        /* the token is an argument to a command. */
        Backslash = 0x20,       /* the token is a backslash substitution. */
        Command = 0x40,         /* the token is a nested command substitution. */
        Variable = 0x80,        /* the token is a variable substitution. */
        Block = 0x100,          /* the token is a block surrounded by braces. */
        StringLiteral = 0x200,  /* the token is a quoted string. */
        NumericLiteral = 0x400, /* the token is a numeric literal. */
        Expression = 0x800,     /* the token is an [expr] expression. */
        Operator = 0x1000,      /* the token is an [expr] operator. */
        Function = 0x2000       /* the token is an [expr] function. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("fa80583c-ff5a-487a-b874-f577ab6e4c53")]
    public enum ListElementFlags
    {
        None = 0x0,
        Invalid = 0x1,
        DontUseBraces = 0x2,   // prevents using braces for quoting list elements.
        UseBraces = 0x4,       // allows using braces for quoting list elements (unless DontUseBraces).
        BracesUnmatched = 0x8, // indicates that there are unmatched braces in the string to convert.
        DontQuoteHash = 0x10   // backward compatibility prior to fixing the quoting of '#' characters.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("0b70b6d4-8cff-4650-9fac-ba1fa68725b1")]
    public enum ReadyFlags
    {
        None = 0x0,                 /* No special handling, not
                                     * recommended. */
        Invalid = 0x1,              /* Invalid, do not use. */
        Reserved = 0x2,             /* Reserved, do not use. */

        ///////////////////////////////////////////////////////////////////////

        Disabled = 0x4,             /* Skip all interpreter readiness
                                     * checks. */
        NoFlags = 0x8,              /* Skip adding readiness flags from
                                     * interpreter. */
        Limited = 0x10,             /* Perform full check only every N
                                     * checks. */
        ExitedOk = 0x20,            /* Return 'Ok' even if interpreter
                                     * has exited. */
        DeletedOk = 0x40,           /* Return 'Ok' even if interpreter
                                     * is deleted. */
        NoStack = 0x80,             /* Skip native stack space checks. */
        NoPoolStack = 0x100,        /* Skip native stack space checks
                                     * for thread pool threads. */
        CheckStack = 0x200,         /* Check native stack space. */
        ForceStack = 0x400,         /* Force checking native stack
                                     * space. */
        ForcePoolStack = 0x800,     /* Check native stack space for
                                     * thread pool threads as well. */
        CheckLevels = 0x1000,       /* Consider the maximum levels
                                     * seen. */
        StackOnly = 0x2000,         /* Skip all checks except native
                                     * stack space. */
        NoCancel = 0x4000,          /* Skip script cancellation
                                     * checking. */
        NoHalt = 0x8000,            /* Skip halt checking. */

#if DEBUGGER
        NoBreakpoint = 0x10000,     /* Skip checking for any script
                                     * breakpoints. */
#endif

        ///////////////////////////////////////////////////////////////////////

        ForPublic = 0x20000,        /* Being called from the public
                                     * method. */
        ForParser = 0x40000,        /* Being called from the parser. */
        ForEngine = 0x80000,        /* Being called from the engine. */
        ForEventManager = 0x100000, /* Being called from the event
                                     * manager. */
        ForTclWrapper = 0x200000,   /* Being called from the Tcl
                                     * wrapper. */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default flags used to enable native stack
        //       space checking.
        //
        ForStack = CheckStack | ForcePoolStack | CheckLevels,

        ///////////////////////////////////////////////////////////////////////

        ViaPublic = Default | ForPublic | ForStack | ForceStack,
        ViaParser = Default | ForParser | ForStack | Limited,
        ViaEngine = Default | ForEngine | Limited,
        ViaEventManager = Default | ForEventManager,
        ViaTclWrapper = Default | ForTclWrapper | ForStack,

        ///////////////////////////////////////////////////////////////////////

        Unknown = None, /* This is the value used when the ready flags
                         * cannot be obtained from the interpreter for
                         * some reason (e.g. locking failure). */

        ///////////////////////////////////////////////////////////////////////

        Default = None  /* These are the flags that are always used by
                         * most callers (i.e. see the "Via*" values
                         * above). */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("85f9cb1e-c237-4a3d-8775-3b5a0700bc8c")]
    public enum InterpreterFlags : ulong
    {
        None = 0x0,                                 /* No flags. */
        Invalid = 0x1,                              /* Invalid, do not use. */
        Cleanup = 0x2,                              /* Interpreter is pending cleanup when
                                                     * the current evaluation stack is
                                                     * unwound (delete all commands,
                                                     * procedures, and global variables).
                                                     */
        Shared = 0x4,                               /* The interpreter is shared with an
                                                     * external component and must not be
                                                     * disposed. */
        NoBackgroundError = 0x8,                    /* Disable the default background error
                                                     * handling. */
        NoPolicies = 0x10,                          /* Skip all command and file policy
                                                     * checks? */
        NoTraces = 0x20,                            /* Skip all variable traces? */
        NoPostProcess = 0x40,                       /* Skip post-processing of returned
                                                     * variable values. */
        TraceInput = 0x80,                          /* Trace interactive input processing.
                                                     */
        TraceResult = 0x100,                        /* Trace engine result processing. */
        TraceToHost = 0x200,                        /* Redirect trace listener output to
                                                     * the interpreter host.
                                                     */
        TraceStack = 0x400,                         /* Attempt to emit a full stack trace
                                                     * if the DebugOps.Complain method is
                                                     * called.  In the future, other trace
                                                     * listener output may be impacted by
                                                     * this flag as well.
                                                     */
        TraceInteractiveCommand = 0x800,            /* Trace interactive commands. */
        TracePackageIndex = 0x1000,                 /* Trace package index file handling.
                                                     */
        ScriptLocation = 0x2000,                    /* Keep track of all script locations;
                                                     * if not set, only those pushed by
                                                     * [source] are tracked. */
        StrictScriptLocations = 0x4000,             /* Throw an exception if called upon
                                                     * to push or pop a script location
                                                     * when they are not available (i.e.
                                                     * null). */
        NoPackageIndexes = 0x8000,                  /* Skip searching for package indexes.
                                                     * This flag prevents a package index
                                                     * that modifies the auto-path from
                                                     * triggering a nested package index
                                                     * search. */
        WriteTestData = 0x10000,                    /* For [test1]/[test2], write the test
                                                     * data to the host. */
        NoReturnTestData = 0x20000,                 /* For [test1]/[test2], do not return
                                                     * the test data. */
        NoLogTestData = 0x40000,                    /* For [test1]/[test2], do not log
                                                     * the test data. */
        FinallyResetCancel = 0x80000,               /* Call Engine.ResetCancel prior to
                                                     * evaluating finally blocks in the
                                                     * [try] command. */
        FinallyRestoreCancel = 0x100000,            /* Call Engine.CancelEvaluate after
                                                     * evaluating the finally block in the
                                                     * [try] command, if necessary. */
        FinallyResetExit = 0x200000,                /* Save/reset the Exit property prior
                                                     * to evaluating finally blocks in the
                                                     * [try] command. */
        FinallyRestoreExit = 0x400000,              /* Restore the Exit property after
                                                     * evaluating the finally block in the
                                                     * [try] command, if necessary. */
        NoPackageFallback = 0x800000,               /* Skip using the configured package
                                                     * fallback delegate to locate any
                                                     * packages that fail to load using
                                                     * the standard "ifneeded" mechanism.
                                                     */
        NoPackageUnknown = 0x1000000,               /* Skip using the configured [package
                                                     * unknown] command to locate any
                                                     * packages that fail to load using
                                                     * the standard "ifneeded" mechanism.
                                                     * It should also be noted that using
                                                     * this flag, which is enabled by
                                                     * default, breaks automatic [package
                                                     * unknown] handling provided by the
                                                     * Eagle Package Repository Client.
                                                     */
#if !MONO && NATIVE && WINDOWS
        ZeroString = 0x2000000,                     /* Enable forcibly zeroing strings
                                                     * that may contain "sensitive" data?
                                                     * WARNING: THIS IS NOT GUARANTEED TO
                                                     * WORK RELIABLY ON ALL PLATFORMS.
                                                     * EXTREME CARE SHOULD BE EXERCISED
                                                     * WHEN HANDLING ANY SENSITIVE DATA,
                                                     * INCLUDING TESTING THAT THIS FLAG
                                                     * WORKS WITHIN THE SPECIFIC TARGET
                                                     * APPLICATION AND ON THE SPECIFIC
                                                     * TARGET PLATFORM. */
#endif
        NoInteractiveCommand = 0x4000000,           /* Disable all interactive commands.  This
                                                     * flag only applies to the interactive
                                                     * loop. */
        ReplaceEmptyListOk = 0x8000000,             /* The [lreplace] command should permit an
                                                     * empty list to be used, and ignore any
                                                     * indexes specified. */
        AllowProxyStream = 0x10000000,              /* The standard channel streams that are
                                                     * provided by the interpreter host are
                                                     * allowed to be transparent proxies even
                                                     * when the interpreter host itself is not
                                                     * a transparent proxy.  Use of this flag
                                                     * should be extremely rare, if ever. */
        IgnoreBgErrorFailure = 0x20000000,          /* Failures encountered when trying to
                                                     * handle a background error should be
                                                     * silently ignored. */
        BgErrorResetCancel = 0x40000000,            /* Call Engine.ResetCancel just before
                                                     * executing the configured background
                                                     * error handler. */
        CatchResetCancel = 0x80000000,              /* Call Engine.ResetCancel just after
                                                     * evaluating scripts for the [catch]
                                                     * command. */
        CatchResetExit = 0x100000000,               /* Reset the Exit property just after
                                                     * evaluating scripts for the [catch]
                                                     * command. */
        TestNullIsEmpty = 0x200000000,              /* Treat null results from the [test?]
                                                     * command bodies the same as an empty
                                                     * string. */
        InfoVarsMayHaveGlobal = 0x400000000,        /* Includes global variables in the
                                                     * list returned by [info vars] when
                                                     * executed in a namespace call frame. */
        ComplainViaTest = 0x800000000,              /* Send complaints to the test suite
                                                     * (log, etc). */
        ComplainViaTrace = 0x1000000000,            /* Send complaints to the diagnostic
                                                     * Trace/Debug listeners. */
        SecurityWasEnabled = 0x2000000000,          /* The ScriptOps.EnableOrDisableSecurity
                                                     * method successfully enabled security.
                                                     */
        ForceGlobalLibrary = 0x4000000000,          /* Make sure that all library scripts
                                                     * are evaluated in the global context.
                                                     */
        ForceGlobalStartup = 0x8000000000,          /* Make sure that all startup scripts
                                                     * are evaluated in the global context.
                                                     */
        AddTclMathOperators = 0x10000000000,        /* Enable adding a command for each
                                                     * operator into the "tcl::mathop"
                                                     * namespace. */
        AddTclMathFunctions = 0x20000000000,        /* Enable adding a command for each
                                                     * operator into the "tcl::mathfunc"
                                                     * namespace. */
#if DEBUGGER && BREAKPOINTS
        ArgumentLocation = 0x40000000000,           /* Keep track of Argument locations. */
#endif
        LegacyOctal = 0x80000000000,                /* Use legacy octal support (i.e. leading
                                                     * zero means octal). */
        NoCleanupObjectReferences = 0x100000000000, /* Skip cleaning up [temporary]
                                                     * object references when exiting
                                                     * the engine back to level zero. */
        StrictExpressionInteger = 0x200000000000,   /* When parsing an expression, failing to
                                                     * convert an integer-like string into an
                                                     * actual integer should result in a script
                                                     * error; otherwise, an attempt will be
                                                     * made to convert it into a floating point
                                                     * value. */
        NoInteractiveTimeout = 0x400000000000,      /* The interactive loop should not mess with
                                                     * the timeout thread. */

        AddTclMathMask = AddTclMathOperators | AddTclMathFunctions,

        //
        // NOTE: These are the default flags for newly created interpreters.
        //
        Default = FinallyResetCancel | FinallyRestoreCancel |
                  FinallyResetExit | FinallyRestoreExit |
                  NoPackageUnknown | ReplaceEmptyListOk |
                  TestNullIsEmpty | ComplainViaTest |
                  AddTclMathOperators | AddTclMathFunctions |
                  LegacyOctal | StrictExpressionInteger
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("e617a055-a7bd-468e-a5fb-052f57251323")]
    public enum CallbackFlags
    {
        None = 0x0,                    /* No special handling. */
        Invalid = 0x1,                 /* Invalid, do not use. */
        ReadOnly = 0x2,                /* The callback may not be modified nor removed. */

        //
        // NOTE: Callback argument handling.
        //

        Arguments = 0x4,               /* Automatically add argument objects. */
        Create = 0x8,                  /* We expect to create an object (e.g. System.Int32.Parse)? */
        Dispose = 0x10,                /* Dipose the object if it cannot be fully created/added? */
        Alias = 0x20,                  /* Create a command alias for the newly created object? */
        AliasRaw = 0x40,               /* The command alias refers to [object invokeall]. */
        AliasAll = 0x80,               /* The command alias refers to [object invokeall]. */
        AliasReference = 0x100,        /* The command alias holds an object reference. */
        ToString = 0x200,              /* Forcibly convert the object to a string and discard it? */
        ByRefStrict = 0x400,           /* Enforce strict type checking on ByRef argument values? */
        Complain = 0x800,              /* Complain about failures that occur when firing events? */
        CatchInterrupt = 0x1000,       /* Catch any ThreadInterruptedException within
                                        * the ThreadStart and ParameterizedThreadStart
                                        * methods and do not re-throw it; otherwise,
                                        * catch it, log it, and then re-throw it. */
        ReturnValue = 0x2000,          /* Automatically handle return values. */
        DefaultValue = 0x4000,         /* Used with ReturnValue in order to force the return of
                                        * the default value (e.g. 0, null) for the method return
                                        * type. */
        AddReference = 0x8000,         /* Add a reference to the callback return value. */
        RemoveReference = 0x10000,     /* Remove a reference from the callback return value. */
        DisposeThread = 0x20000,       /* Call MaybeDisposeThread for delegate types that could
                                        * be used as a thread entry point. */
        ThrowOnError = 0x40000,        /* Throw an exception if the evaluated script returns an
                                        * error? */
        External = 0x80000,            /* The command callback was created via the Utility class.
                                        * This flag is for core library use only. */
        UseOwner = 0x100000,           /* The callback script should be handled by the owner of
                                        * the interpreter being used instead of being directly
                                        * evaluated. */
        Asynchronous = 0x200000,       /* The callback script should be queued asynchronously to
                                        * the owner of the interpreter being used. */
        AsynchronousIfBusy = 0x400000, /* The callback script should be queued asynchronously to
                                        * the owner of the interpreter being used if the owner
                                        * is busy; otherwise, it should be sent synchronously. */
        ResetCancel = 0x800000,        /* Reset the script cancellation flag for the target
                                        * interpreter prior to evaluating the callback script. */
        FireAndForget = 0x1000000,     /* The callback should be cleaned up automatically after
                                        * it is invoked.  This flag is needed for asynchronous
                                        * callbacks. */

        //
        // NOTE: Default argument handling.
        //
        Default = Arguments | Create | Dispose |
                  Alias | Complain | ReturnValue |
                  AddReference | DisposeThread
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ad5ea8ab-53a1-4055-8db5-5c1bba309cfb")]
    public enum MarshalFlags : ulong
    {
        None = 0x0,                       /* No special handling. */
        Invalid = 0x1,                    /* Invalid, do not use. */
        Verbose = 0x2,                    /* Enable [very?] verbose error messages. */
        DefaultValue = 0x4,               /* Use the default value for a parameter when a
                                           * scalar variable does not exist. */
        StringList = 0x8,                 /* Enable aggressive automatic conversions to all
                                           * variants of the StringList type. */
        SkipNullSetupValue = 0x10,        /* For input or output array types, skip changing
                                           * the script variable into an array when the
                                           * object value is null. */
        NoSystemArray = 0x20,             /* Disable transparent detection and use of all
                                           * System.Array backed variables when marshalling
                                           * arguments to a method. */
        StrictMatchCount = 0x40,          /* MatchParameterTypes should strictly match the
                                           * parameter counts. */
        StrictMatchType = 0x80,           /* MatchParameterTypes should strictly match the
                                           * parameter types. */
        ForceParameterType = 0x100,       /* Force FindMethodsAndFixupArguments to use the
                                           * specified parameter type, not the one from the
                                           * method overload itself. */
        ReorderMatches = 0x200,           /* Reorder the matching methods based on the
                                           * criteria specified in the ReorderFlags. */
        NoDelegateCallback = 0x400,       /* Do not attempt to perform any conversions when
                                           * the target type is System.Delegate. */
        NoGenericCallback = 0x800,        /* Prevent use of the GenericCallback type when
                                           * creating a command callback. */
        DynamicCallback = 0x1000,         /* Enable use of the DynamicInvokeCallback when
                                           * creating a command callback. */
        NoChangeTypeThrow = 0x2000,       /* Skip throwing exceptions from within the
                                           * script binder ChangeType method. */
        NoBindToFieldThrow = 0x4000,      /* Skip throwing exceptions from within the
                                           * script binder BindToField method. */
        SkipChangeType = 0x8000,          /* Skip calling the ChangeType method of the
                                           * fallback (and/or default) binder. */
        SkipBindToField = 0x10000,        /* Skip calling the BindToField method of the
                                           * fallback (and/or default) binder. */
        SkipReferenceTypeCheck = 0x20000, /* Skip checking reference type equality when
                                           * coming back from the ChangeType method. */
        SkipValueTypeCheck = 0x40000,     /* Skip checking value type equality when coming
                                           * back from the ChangeType method. */
        NoCallbackOptions = 0x80000,      /* Skip parsing per-CommandCallback options. */
        IgnoreCallbackOptions = 0x100000, /* Ignore per-CommandCallback option values. */
        ThrowOnBindFailure = 0x200000,    /* Throw an exception on delegate binding
                                           * failures. */
        NoHandle = 0x400000,              /* Skip using opaque object handles. */
        UseInOnly = 0x800000,             /* When determining if a parameter is "input",
                                           * use only the ParameterInfo.IsIn property. */
        UseByRefOnly = 0x1000000,         /* When determining if a parameter is "output",
                                           * use only the Type.IsByRef property. */
        NoByRefArguments = 0x2000000,     /* Skip special handling for output parameters,
                                           * e.g. do not create ArgumentInfo objects. */
        NoScriptBinder = 0x4000000,       /* Do not assume that the binder implements
                                           * the full IScriptBinder semantics for the
                                           * passing of a MarshalClientData as the
                                           * value parameter. */
        TraceResults = 0x8000000,         /* Emit the final list of method overloads to
                                           * the trace listeners. */
        HandleByValue = 0x10000000,       /* Prior to looking up an opaque object handle
                                           * for any parameter, use its scalar variable
                                           * value as the opaque object handle name. */
        ByValHandleByValue = 0x20000000,  /* Prior to looking up an opaque object handle
                                           * for an "input" parameter, use its scalar
                                           * variable value as the opaque object handle
                                           * name. */
        ByRefHandleByValue = 0x40000000,  /* Prior to looking up an opaque object handle
                                           * for an "output" parameter, use its scalar
                                           * variable value as the opaque object handle
                                           * name. */
        ForceHandleByValue = 0x80000000,  /* Prior to looking up an opaque object handle
                                           * for any parameter, use its scalar variable
                                           * value as the opaque object handle name,
                                           * even if an opaque object handle is not
                                           * found using that value. */
        IsAssignableFrom = 0x100000000,   /* EXPERIMENTAL: Enable use of custom handling
                                           * for the Type.IsAssignableFrom method?  This
                                           * is mostly used to work around Mono issues.
                                           */
        SpecialValueType = 0x200000000,   /* EXPERIMENTAL: Enable special handling of
                                           * the ValueType type when determining if a
                                           * value can be used with a given type. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = StrictMatchCount | StrictMatchType |
                  ThrowOnBindFailure /* LEGACY */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("fcec8afc-2e5c-479b-bd55-d20621acbcc5")]
    public enum ReorderFlags
    {
        None = 0x0,                       /* No special handling. */
        Invalid = 0x1,                    /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        FewestParametersFirst = 0x2,      /* Prefer method overloads that accept
                                           * the fewest number of parameters. */
        MostParametersFirst = 0x4,        /* Prefer method overloads that accept
                                           * the greatest number of parameters. */
        ShallowestTypesFirst = 0x8,       /* Prefer argument types towards the
                                           * root of the hierarchy. */
        DeepestTypesFirst = 0x10,         /* Prefer argument types towards the
                                           * leaves of the hierarchy. */
        TypeDepthsFirst = 0x20,           /* Compare the type depths before
                                           * comparing the parameter counts. */
        TotalTypeDepths = 0x40,           /* When comparing type depths, instead
                                           * of returning a result at the first
                                           * non-equal type depth, sum all the
                                           * type depths to arrive at the result.
                                           * This is useful primarily when the
                                           * parameters do not conform to a
                                           * well-defined order between method
                                           * overloads. */
        UseParameterTypes = 0x80,         /* When calculating type depths, use
                                           * the formal parameter type instead
                                           * of the argument type, if doing so
                                           * would result in a more accurate
                                           * type depth. */
        SubTypeDepths = 0x100,            /* Consider Array<T> and Nullable<T> as
                                           * levels to be traversed when
                                           * calculating type depths. */
        ValueTypeDepths = 0x200,          /* Consider all reference types, except
                                           * "Object" and "ValueType", as levels
                                           * to be traversed when calculating
                                           * type depths. */
        ByRefTypeDepths = 0x400,          /* Also consider ByRef<T> as a level to
                                           * be traversed when calculating type
                                           * depths. */
        FallbackOkOnError = 0x800,        /* When an error is encountered (e.g.
                                           * parameter validation, exception,
                                           * etc), return "Ok" instead of
                                           * "Error".  This allows the default
                                           * method overload to be called instead
                                           * of failing the entire method call
                                           * operation. */
        UseArgumentCounts = 0x1000,       /* If necessary, take the supplied
                                           * arguments counts into account when
                                           * calculating parameter counts. */
        StrictParameterCounts = 0x2000,   /* Bail out on error while querying
                                           * parameter counts. */
        ContinueParameterCounts = 0x4000, /* Skip to next method overload on
                                           * error while querying parameter
                                           * counts. */
        StrictTypeDepths = 0x8000,        /* Bail out on error while calculating
                                           * parameter type depths. */
        ContinueTypeDepths = 0x10000,     /* Skip to next method overload on
                                           * error while calculating parameter
                                           * type depths. */
        StringTypePenalty = 0x20000,      /* Subtract one level for types that
                                           * are trivially convertible from a
                                           * string (e.g. System.String) when
                                           * calculating type depths. */
        StringTypeBonus = 0x40000,        /* Add one level for types that are
                                           * trivially convertible from a string
                                           * (e.g. System.String) when calculating
                                           * type depths. */
        TraceResults = 0x80000,           /* Emit the final sorted list of method
                                           * overloads to the trace listeners. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        ParameterCountMask = FewestParametersFirst | MostParametersFirst,
        ParameterTypeDepthMask = ShallowestTypesFirst | DeepestTypesFirst,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = FewestParametersFirst | DeepestTypesFirst |
                  SubTypeDepths | ValueTypeDepths |
                  ByRefTypeDepths /* TODO: Good default? */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("1e4879b1-f3db-4bda-8637-6f1495a51ec8")]
    public enum MethodFlags
    {
        None = 0x0,             /* No special handling. */
        Invalid = 0x1,          /* Invalid, do not use. */
        PluginPolicy = 0x2,     /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle plugin loading policies. */
        CommandPolicy = 0x4,    /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle command execution policies. */
        SubCommandPolicy = 0x8, /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle sub-command execution policies. */
        ProcedurePolicy = 0x10, /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle procedure execution policies. */
        ScriptPolicy = 0x20,    /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle script policies. */
        FilePolicy = 0x40,      /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle script file policies. */
        StreamPolicy = 0x80,    /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle script stream policies. */
        OtherPolicy = 0x100,    /* Method conforms to the ExecuteCallback delegate type and is
                                 * used to handle "other" policies. */
        VariableTrace = 0x200,  /* Method conforms to the TraceCallback delegate type and is
                                 * used to handle variable traces. */
        NoAdd = 0x400           /* The method will not be auto-loaded by the plugin manager. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("2000c391-277e-435f-8c29-f5f64f1c6e19")]
    public enum UsageType
    {
        None = 0x0,
        Count = 0x1
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("768b5bd7-cb31-465b-827e-94a12dc46dcd")]
    public enum LookupFlags
    {
        None = 0x0,       /* No special handling. */
        Invalid = 0x1,    /* Invalid, do not use. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Wrapper = 0x2,    /* Return the wrapper object, not the contained entity. */
        Validate = 0x4,   /* Validate that the returned object is not null.  If it
                           * is null, return an error. */
        Verbose = 0x8,    /* Return a verbose error message. */
        Visible = 0x10,   /* Consider "visible" (i.e. non-hidden) entities. */
        Invisible = 0x20, /* Consider "invisible" (i.e. hidden) entities. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are for use by the Does*Exist() methods of the
        //       Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
        Exists = Wrapper | Visible,
        ExistsAndValid = Exists | Validate,

        //
        // NOTE: These flags are for use by the Remove*() methods of the
        //       Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
        Remove = Default & ~Validate,
        RemoveNoVerbose = Remove & ~Verbose,

        //
        // NOTE: These flags are for use by the Unload*() methods of the
        //       Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
#if ISOLATED_PLUGINS
        Unload = Default,
#endif

        //
        // NOTE: These flags are for use by the Get*Interpreter() methods of
        //       the Interpreter class only.  These flags may NOT be used by
        //       external components.
        //
        Interpreter = Default & ~Wrapper,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags are for use by the Engine class only.  These flags
        //       may NOT be used by external components.
        //
        EngineDefault = Default,
        EngineNoVerbose = EngineDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the Expression class only.  These
        //       flags may NOT be used by external components.
        //
        ExpressionDefault = Default & ~Wrapper,
        ExpressionNoVerbose = ExpressionDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the MarshalOps class only.  These
        //       flags may NOT be used by external components.
        //
        MarshalDefault = Default & ~Verbose,
        MarshalNoVerbose = MarshalDefault,
        MarshalAlias = MarshalDefault & ~Validate,

        //
        // NOTE: These flags are for use by the HelpOps class only.  These flags
        //       may NOT be used by external components.
        //
        HelpNoVerbose = Default & ~Verbose,

        //
        // NOTE: These flags are for use by the Default host class only.  These
        //       flags may NOT be used by external components.
        //
        HostNoVerbose = Default & ~Verbose,

        //
        // NOTE: These flags are for use by the PolicyOps and RuntimeOps classes
        //       only.  These flags may NOT be used by external components.
        //
        PolicyDefault = Default | Invisible,
        PolicyNoVerbose = PolicyDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the GetInterpreterAliasTarget,
        //       HasInterpreterAlias, GetLibraryAliasTarget, GetObjectAliasTarget,
        //       and GetTclAliasTarget methods only.  These flags may NOT be used
        //       by external components.
        //
        AliasDefault = Default | Invisible,
        AliasNoVerbose = AliasDefault & ~Verbose,

        //
        // NOTE: These flags are for use by the Interpreter.GetOptions
        //       method only.  These flags may NOT be used by external
        //       components.
        //
        OptionDefault = NoValidate,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These flags should be used when the caller does not require a
        //       valid (non-null) entity be found (i.e. only that one exists
        //       for the given name or token).  These flags may be used by
        //       external components.
        //
        NoValidate = Wrapper | Verbose | Visible,

        //
        // NOTE: These flags should be used when the caller wants direct access
        //       to the entity itself, not any intermediate wrapper object that
        //       may contain it.  These flags may be used by external components.
        //
        NoWrapper = Validate | Verbose | Visible,

        //
        // NOTE: These flags should be used when the caller does not require an
        //       error message.  These flags may be used by external components.
        //
        NoVerbose = Wrapper | Validate | Visible,

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: These are the default flags for all entity lookups.  These
        //       flags may be used by external components.
        //
        Default = Wrapper | Validate | Verbose | Visible
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("bd01cdba-63db-4751-9c5e-45504fb336af")]
    public enum AliasFlags
    {
        None = 0x0,               /* No special handling. */
        MergeArguments = 0x1,     /* The alias will merge the current command arguments with the
                                   * arguments originally specified during alias creation,
                                   * maintaining the overall argument order and merging the options
                                   * together. */
        SkipTargetName = 0x2,     /* The merged arguments will not include the target command
                                   * name. */
        SkipSourceName = 0x4,     /* The merged arguments will not include the source command
                                   * name. */
        Evaluate = 0x8,           /* The target of the alias will be evaluated rather than executed
                                   * directly. */
        GlobalNamespace = 0x10,   /* The global namespace should be used as the context. */
        CrossCommand = 0x20,      /* The command alias is being used for [interp alias] support. */
        Object = 0x40,            /* The command alias refers to a managed object. */
        Reference = 0x80,         /* The command alias holds a reference to the managed object. */
        Namespace = 0x100,        /* The command alias is being used for [namespace] support. */
        CrossInterpreter = 0x200, /* The command alias is being used for [interp create] support. */

#if NATIVE && LIBRARY
        Library = 0x400,          /* The command alias refers to a native library delegate. */
#endif

#if NATIVE && TCL
        TclWrapper = 0x800,       /* The command alias is being used for [tcl eval] support. */
#endif

        CrossCommandAlias = GlobalNamespace | CrossCommand,         /* For [interp alias]
                                                                     * support. */
        CrossInterpreterAlias = GlobalNamespace | CrossInterpreter, /* For [interp create]
                                                                     * support. */
        NamespaceImport = SkipSourceName | Namespace                /* For [namespace import]
                                                                     * support. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b8e02964-4bde-47dc-a873-15cd5cb3633e")]
    public enum SubCommandFlags
    {
        None = 0x0,                  /* No special handling. */
        Invalid = 0x1,               /* Invalid, do not use. */
        Core = 0x2,                  /* The sub-command is handled by the core. */
        Safe = 0x4,                  /* Sub-command is "safe" to execute for partially
                                      * trusted and/or untrusted scripts. */
        Unsafe = 0x8,                /* Sub-command is NOT "safe" to execute for
                                      * partially trusted and/or untrusted scripts. */
        ForceQuery = 0x10,           /* Instead of modifying the sub-command, just
                                      * return it (i.e. even if the number of arguments
                                      * to the command would suggest otherwise). */
        ForceNew = 0x20,             /* The sub-command must be added, not modified. */
        ForceReset = 0x40,           /* The sub-command must be [re-]added during a
                                      * reset, if it does not already exist. */
        ForceDelete = 0x80,          /* The sub-command must be removed, not reset. */
        NoComplain = 0x100,          /* The query, add, reset, and remove operations
                                      * cannot generate an error just because the
                                      * sub-command may -OR- may not exist.  Queries
                                      * will return an empty string in this case. */
        UseExecuteArguments = 0x200, /* Append the IExecute.Execute arguments to the
                                      * configured script command before evaluating. */
        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ae31c0f5-e0a8-44b0-b784-bcdadfc76f0e")]
    public enum CommandFlags
    {
        None = 0x0,            /* No special handling. */
        Invalid = 0x1,         /* Invalid, do not use. */
        Core = 0x2,            /* This command is part of the core command set. */
        Delegate = 0x4,        /* This command wraps a delegate provided from an external
                                * source. */
#if ISOLATED_PLUGINS
        Isolated = 0x8,        /* The command has been loaded into an isolated AppDomain
                                * (most likely via its parent plugin). */
#endif
        Disabled = 0x10,       /* The command may not be executed. */
        Hidden = 0x20,         /* The command may only be executed if allowed by policy. */
        ReadOnly = 0x40,       /* The command may not be modified nor removed. */
        NativeCode = 0x80,     /* The command contains, calls, or refers to native code. */
        Breakpoint = 0x100,    /* Break into debugger before execution. */
        NoAdd = 0x200,         /* The command will not be auto-loaded by the plugin manager. */
        NoPopulate = 0x400,    /* The command will not be populated by the plugin manager. */
        Alias = 0x800,         /* The command is really an alias to another command. */
        Restore = 0x1000,      /* Skip over trying to add commands if they already exist
                                * (restoration mode). */
        Safe = 0x2000,         /* Command is "safe" to execute for partially trusted and/or
                                * untrusted scripts. */
        Unsafe = 0x4000,       /* Command is NOT "safe" to execute for partially trusted and/or
                                * untrusted scripts. */
        Standard = 0x8000,     /* The command is largely (or completely) compatible with an
                                * identically named command from Tcl/Tk 8.4, 8.5, and/or 8.6. */
        NonStandard = 0x10000, /* The command is not present in Tcl/Tk 8.4, 8.5, and/or 8.6
                                * -OR- it is completely incompatible with an identically named
                                * command in Tcl/Tk 8.4, 8.5, and/or 8.6. */
        NoToken = 0x20000,     /* Skip handling of the command token via the associated plugin. */
        NoRename = 0x40000,    /* Prevent the command from being renamed. */
        Obsolete = 0x80000,    /* The command has been superseded and should not be used for new
                                * development. */
        Diagnostic = 0x100000, /* The command is primarily intended to be used when debugging
                                * and/or testing the core library.  Also, the semantics of the
                                * contained sub-commands are subject to change, even in stable
                                * releases, and should not be relied upon by any production
                                * applications, plugins, or scripts. */
        Ensemble = 0x200000,   /* The command is an ensemble and may have special dispatch
                                * handling for its supported sub-commands and/or for unknown
                                * sub-commands. */
        SubCommand = 0x400000  /* The command is really a sub-command. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d2ee28ef-b37e-4f90-a88a-408d8610c33e")]
    public enum ByRefArgumentFlags
    {
        None = 0x0,             /* No special flags. */
        Invalid = 0x1,          /* Invalid, do not use. */
        Fast = 0x2,             /* Fast mode, skip traces, watches, notifications,
                                 * post-processing (primarily for speed), etc. */
        Direct = 0x4,           /* Direct mode, bypass all use of SetVariableValue
                                 * (primarily for speed).  Currently, this option
                                 * only applies to arrays. */
        Strict = 0x8,           /* Enable strict by-ref argument type handling. */
        Create = 0x10,          /* We expect to create an object (e.g. System.Int32.Parse)? */
        Dispose = 0x20,         /* Dipose the object if it cannot be fully created/added? */
        Alias = 0x40,           /* Create a command alias for the newly created object? */
        AliasRaw = 0x80,        /* The command alias refers to [object invokeraw]. */
        AliasAll = 0x100,       /* The command alias refers to [object invokeall]. */
        AliasReference = 0x200, /* The command alias holds an object reference. */
        ToString = 0x400,       /* Forcibly convert the object to a string and discard it? */
        ArrayAsValue = 0x800,   /* Use opaque object handles for managed arrays instead of
                                 * setting the script array element values. */
        ArrayAsLink = 0x1000,   /* Use a script variable linked to the underlying array object
                                 * instead of copying the data. */
        NoSetVariable = 0x2000, /* Skip setting variables for by-ref arguments. */

        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ae93bd6d-556e-4d73-bd51-2a1f4af7bca1")]
    public enum ObjectReferenceType
    {
        None = 0x0,    // None or unknown, do not use.
        Invalid = 0x1, // Invalid, do not use.
        Create = 0x2,  // Reference was added at handle creation.
        Demand = 0x4,  // Reference was added by script request.
        Trace = 0x8,   // Reference was added via [set] trace.
        Return = 0x10, // Reference was added via [return].
        Command = 0x20 // Reference was added for a command (alias?).
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("3aa35d84-3d18-4fb5-a6d1-d1d53108d76f")]
    public enum ObjectFlags : ulong
    {
        None = 0x0,                              /* No special handling.  Be very careful when
                                                  * using this.
                                                  * If the object does not actually belong to
                                                  * the caller, be sure to use the NoDispose
                                                  * flag instead. */
        Invalid = 0x1,                           /* Invalid, do not use. */
        Locked = 0x2,                            /* Automatic reference counting does not apply
                                                  * to this object. */
        Safe = 0x4,                              /* This object can be used by safe interpreters
                                                  * without risking security. */
        Assembly = 0x8,                          /* This is an assembly loaded by [object load].
                                                  */
        Runtime = 0x10,                          /* This is an object from this library. */
        Interpreter = 0x20,                      /* This is an Interpreter object. */
        EventManager = 0x40,                     /* This is an event manager object. */
        Debugger = 0x80,                         /* This is a script debugger object. */
        Host = 0x100,                            /* This is an IHost object of some type. */
        ContextManager = 0x200,                  /* This is a context manager object. */
        EngineContext = 0x400,                   /* This is an engine context object. */
        InteractiveContext = 0x800,              /* This is an interactive context object. */
        TestContext = 0x1000,                    /* This is a test context object. */
        VariableContext = 0x2000,                /* This is a variable context object. */
        Namespace = 0x4000,                      /* This is a namespace object. */
        CallFrame = 0x8000,                      /* This is a call frame object. */
        Wrapper = 0x10000,                       /* This is an entity wrapper object of some
                                                  * type. */
        CallStack = 0x20000,                     /* This is a call stack. */
        Alias = 0x40000,                         /* This object has a command alias. */
        NoDispose = 0x80000,                     /* This object cannot be disposed ([object
                                                  * dispose] is a NOP). */
        AllowExisting = 0x100000,                /* The handle for this object should not be
                                                  * created if one with the same value exists
                                                  * (even if the object name has been
                                                  * specified). */
        ForceNew = 0x200000,                     /* The handle for this object should always be
                                                  * created (even if one with the same value
                                                  * exists). */
        ForceDelete = 0x400000,                  /* This bridged Tcl command object should be
                                                  * forcibly deleted during dispose. */
        NoComplain = 0x800000,                   /* Errors should be ignored when trying to
                                                  * delete the Tcl command during bridge
                                                  * disposal. */
        NoBinder = 0x1000000,                    /* Skip trying to query the Binder property of
                                                  * the interpreter. */
        NoComObjectLookup = 0x2000000,           /* Skip all special type lookup handling for
                                                  * the COM object proxy type (i.e.
                                                  * "System.__ComObject"). */
        NoComObjectReturn = 0x4000000,           /* Skip all special return value handling for
                                                  * the COM object proxy type (i.e.
                                                  * "System.__ComObject"). */
        IgnoreAlias = 0x8000000,                 /* Ignore the Alias flag in FixupReturnValue
                                                  * when looking up existing objects to use for
                                                  * an opaque object handle. */
        NoAutoDispose = 0x10000000,              /* The object cannot be disposed automatically
                                                  * because we may not own it. */
        AutoDispose = 0x20000000,                /* The object should be disposed automatically.
                                                  */
        NoAttribute = 0x40000000,                /* Forbid using the ObjectFlagsAttribute when
                                                  * handling object return values. */
        ForceAutomaticName = 0x80000000,         /* Force a non-empty opaque object handle to be
                                                  * returned for null values when the name is
                                                  * automatically generated. */
        ForceManualName = 0x100000000,           /* Force a non-empty opaque object handle to be
                                                  * returned for null values when the name is
                                                  * manually specified. */
        NullObject = 0x200000000,                /* Reserved for use with the "null" opaque
                                                  * object handle only.  Do NOT use this for any
                                                  * other purpose. */
        SharedObject = 0x400000000,              /* Reserved for use by the AddSharedObject
                                                  * method.  Do NOT use this for any other
                                                  * purpose. */
        AddReference = 0x800000000,              /* Add an initial reference when creating a new
                                                  * opaque object handle. */
        StickAlias = 0x1000000000,               /* Create a command alias if the new object was
                                                  * created via an object with this flag set. */
        UnstickAlias = 0x2000000000,             /* Forbid creating a command alias just because
                                                  * the new object was created via an object with
                                                  * the StickAlias flag set. */
        NoReturnReference = 0x4000000000,        /* Skip adding / removing object references in
                                                  * response to [return]. */
        TemporaryReturnReference = 0x8000000000, /* Consider all object references added by
                                                  * [return] to be temporary.  Upon returning to
                                                  * the level 0 call frame, these references will
                                                  * be removed, which may result in the object
                                                  * being disposed (i.e. unless it has been saved
                                                  * into a variable). */
        NoRemoveComplain = 0x10000000000,        /* Do not complain if an attempt to remove this
                                                  * opaque object handle fails. */
        PreferMoreMembers = 0x20000000000,       /* When selecting a type from a list of
                                                  * candidates, prefer the one that has more
                                                  * members.  This flag only applies to COM
                                                  * interop objects. */
        PreferSimilarName = 0x40000000000,       /* When selecting a type from a list of
                                                  * candidates, prefer the one that has the name
                                                  * the most similar to the one originally
                                                  * specified.  This flag only applies to COM
                                                  * interop objects. */
        RejectDissimilarNames = 0x80000000000,   /* When selecting a type from a list of
                                                  * candidates, reject them all if none has a
                                                  * similar name.  This flag only applies to COM
                                                  * interop objects. */
        NoCase = 0x100000000000,                 /* Ignore case when comparing strings.  This
                                                  * flag only applies to COM interop objects. */
        AutoFlagsEnum = 0x200000000000,          /* When possible, attempt to automatically use
                                                  * the "flags" enumeration handling for fields
                                                  * set via [object invoke]. */
        Reserved1 = 0x400000000000,              /* This flag bit is reserved and will not be
                                                  * used outside of this class. */

        SelectTypeMask = PreferMoreMembers | PreferSimilarName | RejectDissimilarNames,

        ForNullObject = Locked | Safe | NullObject, /* This mask is for use when adding the
                                                     * "null" opaque object handle only. */

        NoComObject = NoComObjectLookup | NoComObjectReturn, /* Skip all special handling for
                                                              * the COM object proxy type
                                                              * (i.e. "System.__ComObject").
                                                              */

        Callback = NoDispose | Reserved1, /* Default flags used when creating a
                                           * CommandCallback object. */

        Default = ForceDelete | Reserved1 /* Default flags used by [object create], [object
                                           * foreach], [object get], [object invoke], [object
                                           * load], etc. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("a82bd131-a778-4856-a03e-d09e470a83c7")]
    public enum ProcedureFlags
    {
        None = 0x0,
        Invalid = 0x1,          /* Invalid, do not use. */
        Core = 0x2,             /* This procedure is included with the runtime. */
        Library = 0x4,          /* This procedure is part of the script library. */
        Interactive = 0x8,      /* This procedure is included with the interactive shell. */
        Disabled = 0x10,        /* The procedure may not be executed. */
        Hidden = 0x20,          /* The procedure may only be executed if allowed by policy. */
        ReadOnly = 0x40,        /* The procedure may not be modified nor removed. */
        Safe = 0x80,            /* Procedure is "safe" to execute for partially trusted and/or
                                 * untrusted scripts. */
        Unsafe = 0x100,         /* Procedure is NOT "safe" to execute for partially trusted and/or
                                 * untrusted scripts. */
        Breakpoint = 0x200,     /* Break into debugger upon entry and exit. */
        ScriptLocation = 0x400, /* Use the previously pushed script location when evaluating the
                                 * procedure body. */
        Private = 0x800,        /* The procedure may be executed only from within the file it was
                                 * defined in.  If the procedure was not defined in a file, it may
                                 * only be executed outside the context of a file. */
        NoReplace = 0x1000,     /* Attempts to replace the procedure are silently ignored. */
        NoRename = 0x2000,      /* Attempts to rename the procedure are silently ignored. */
        NoRemove = 0x4000,      /* Attempts to remove the procedure are silently ignored. */
        Fast = 0x8000,          /* Traces, watchpoints, and similar mechanisms are disabled by
                                 * default for all local variables. */
        Atomic = 0x10000,       /* The interpreter lock should be held while the procedure is
                                 * running. */
#if ARGUMENT_CACHE || PARSE_CACHE
        NonCaching = 0x20000,   /* Disable caching for the body of the procedure.  The exact
                                 * cache(s) that is/are disabled is unspecified and subject
                                 * to change in the future. */
#endif

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("b2e5ed96-4dac-4070-bbf5-51464b28c4d5")]
    public enum PluginFlags : ulong
    {
        None = 0x0,
        Invalid = 0x1,                  /* Invalid, do not use. */
        Primary = 0x2,                  /* The plugin is the primary one for the
                                         * containing assembly. */
        System = 0x4,                   /* The plugin is a system plugin (i.e.
                                         * part of the Eagle runtime). */
        Host = 0x8,                     /* The plugin contains a custom host. */
        Debugger = 0x10,                /* The plugin contains a script debugger
                                         * (reserved for future use). */
        User = 0x20,                    /* The plugin is a user plugin (i.e.
                                         * from a third-party vendor). */
        Commercial = 0x40,              /* The plugin is part of a commercial
                                         * product, this implies but does not
                                         * explicitly state that the plugin is
                                         * closed source and/or is not licensed
                                         * under a Tcl-style license. */
        Proprietary = 0x80,             /* The plugin contains proprietary code,
                                         * this implies but does not explicitly
                                         * state that the plugin is closed
                                         * source and/or is not licensed under a
                                         * Tcl-style license. */
        Command = 0x100,                /* The plugin contains one or more
                                         * custom commands. */
        Function = 0x200,               /* The plugin contains one or more
                                         * custom [expr] functions. */
        Trace = 0x400,                  /* The plugin traces variables
                                         * (interpreter-wide and/or specific
                                         * variables). */
        Notify = 0x800,                 /* The plugin listens for notifications.
                                         */
        Policy = 0x1000,                /* The plugin contains one or more
                                         * policies.  Setting this flag requires
                                         * the Primary flag to be set as well.
                                         */
        Resolver = 0x2000,              /* The plugin contains one or more
                                         * command and/or variable resolvers. */
        Static = 0x4000,                /* The plugin was provided "statically"
                                         * by the application. */
        Demand = 0x8000,                /* The plugin was loaded on-demand by
                                         * the [load] command. */
        UnsafeCode = 0x10000,           /* The plugin contains, calls, or refers
                                         * to unsafe code. */
        NativeCode = 0x20000,           /* The plugin contains, calls, or refers
                                         * to native code. */
        SafeCommands = 0x40000,         /* The plugin ONLY contains commands
                                         * which are "safe" when executed by
                                         * untrusted or marginally trusted
                                         * scripts.  Currently, this flag is
                                         * never set by Eagle itself; however,
                                         * plugin authors are encouraged to set
                                         * this flag for their plugins if all
                                         * their functionality is safe for use
                                         * by untrusted or marginally trusted
                                         * scripts.  In the future, Eagle may
                                         * make more extensive use of this flag.
                                         */
        MergeCommands = 0x80000,        /* When adding commands from the plugin,
                                         * ignore those that are already present.
                                         */
        MergeProcedures = 0x100000,     /* When adding procedures from the plugin,
                                         * ignore those that are already present.
                                         */
        MergePolicies = 0x200000,       /* When adding policies from the plugin,
                                         * ignore those that are already present.
                                         */
        Test = 0x400000,                /* The plugin is primarily for unit
                                         * testing purposes. */
        UserInterface = 0x800000,       /* The plugin contains a user interface.
                                         */
        NoInitialize = 0x1000000,       /* The default plugin should not perform
                                         * initialization logic on behalf of the
                                         * plugin. */
        NoTerminate = 0x2000000,        /* The default plugin should not perform
                                         * termination logic on behalf of the
                                         * plugin. */
        NoCommands = 0x4000000,         /* The default plugin should not add
                                         * commands on behalf of the plugin.
                                         */
        NoFunctions = 0x8000000,        /* The default plugin should not add
                                         * functions on behalf of the plugin.
                                         */
        NoPolicies = 0x10000000,        /* The default plugin should not add
                                         * policies on behalf of the plugin.
                                         */
        NoTraces = 0x20000000,          /* The default plugin should not add
                                         * traces on behalf of the plugin.
                                         */
        NoProvide = 0x40000000,         /* The notify plugin should not provide
                                         * the package on behalf of the plugin.
                                         */
        NoResources = 0x80000000,       /* The plugin does not have any
                                         * scripting resources and should not be
                                         * queried by the interpreter via any
                                         * resource manager it may contain. */
        NoAuxiliaryData = 0x100000000,  /* The plugin does not have any auxiliary
                                         * data and a dictionary should not be
                                         * created. */
        NoInitializeFlag = 0x200000000, /* The default plugin should not set or
                                         * reset the initialized flag. */
        NoResult = 0x400000000,         /* The default plugin should not set the
                                         * result from within the
                                         * IState.Initialize and IState.Terminate
                                         * methods. */
        NoGetString = 0x800000000,      /* The core host should not call the
                                         * GetString method for the plugin. */
        StrongName = 0x1000000000,      /* The plugin assembly has a StrongName
                                         * signature.  May not be 100% reliable.
                                         * WARNING: DO NOT SET THIS FLAG MANUALLY
                                         * AND DO NOT MAKE SECURITY DECISIONS
                                         * BASED ON THE PRESENCE OR ABSENCE OF
                                         * THIS FLAG. */
        Verified = 0x2000000000,        /* The StrongName signature has been
                                         * "verified" via the CLR native API.
                                         * May not be 100% reliable.  WARNING:
                                         * DO NOT SET THIS FLAG MANUALLY AND DO
                                         * NOT MAKE SECURITY DECISIONS BASED ON
                                         * THE PRESENCE OR ABSENCE OF THIS FLAG.
                                         */
        VerifiedOnly = 0x4000000000,    /* The StrongName signature must be
                                         * "verified" via the CLR native API
                                         * before any plugin file can be loaded.
                                         * This may not be 100% reliable. */
        SkipVerified = 0x8000000000,    /* The StrongName signature checking was
                                         * skipped. */
        Authenticode = 0x10000000000,   /* The plugin assembly has an
                                         * Authenticode signature.  May not be
                                         * 100% reliable.  WARNING: DO NOT SET
                                         * THIS FLAG MANUALLY AND DO NOT MAKE
                                         * SECURITY DECISIONS BASED ON THE
                                         * PRESENCE OR ABSENCE OF THIS FLAG. */
        Trusted = 0x20000000000,        /* The Authenticode signature and
                                         * certificate appear to be "trusted"
                                         * by the operating system.  May not be
                                         * 100% reliable.  WARNING: DO NOT SET
                                         * THIS FLAG MANUALLY AND DO NOT MAKE
                                         * SECURITY DECISIONS BASED ON THE
                                         * PRESENCE OR ABSENCE OF THIS FLAG. */
        TrustedOnly = 0x40000000000,    /* The Authenticode signature and
                                         * certificate must be "trusted" by the
                                         * operating system before any plugin
                                         * file can be loaded.  This may not be
                                         * 100% reliable. */
        SkipTrusted = 0x80000000000,    /* The Authenticode signature and
                                         * certificate checking was skipped. */
        SkipTerminate = 0x100000000000, /* The IState.Terminate method should be
                                         * skipped during the UnloadPlugin
                                         * method that accepts an IPlugin
                                         * instance. */
#if ISOLATED_PLUGINS
        Isolated = 0x200000000000,      /* The plugin assembly should be (or has
                                         * been) loaded into an isolated
                                         * AppDomain. */
#endif
        Verbose = 0x400000000000,       /* Enable verbose output during plugin
                                         * loading/unloading? */
        Licensed = 0x800000000000,      /* The plugin is a licensed component
                                         * and that license has been verified.
                                         */
        Custom1 = 0x1000000000000,      /* This flag is reserved for use by
                                         * third-party applications and plugins.
                                         */
        Custom2 = 0x2000000000000,      /* This flag is reserved for use by
                                         * third-party applications and plugins.
                                         */
        Custom3 = 0x4000000000000,      /* This flag is reserved for use by
                                         * third-party applications and plugins.
                                         */
        Reserved = 0x8000000000000      /* The flag is reserved for future use
                                         * and must not be set. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("eb0e3225-f87c-4f47-b30e-6ab38548cd7c")]
    public enum PackageIndexFlags
    {
        None = 0x0,             /* No special handling. */
        Invalid = 0x1,          /* Invalid, do not use. */
        PreferFileSystem = 0x2, /* Check the file system before checking the
                                 * interpreter host. */
        PreferHost = 0x4,       /* Check the interpreter host before checking
                                 * the file system. */
        Host = 0x8,             /* Use the interpreter host to find the
                                 * package index. */
        Normal = 0x10,          /* Use the external file system to find
                                 * the package index. */
        NoNormal = 0x20,        /* Forbid using the file system to find
                                 * the package index.  This flag is only
                                 * effective when using the interpreter
                                 * host to find the package index. */
        Recursive = 0x40,       /* Search all sub-directories as well? */
        Refresh = 0x80,         /* Force package index to be re-found
                                 * and re-evaluated. */
        Resolve = 0x100,        /* Resolve the fully qualified file name
                                 * for the package index script. */
        Trace = 0x200,          /* Enable tracing of key package index
                                 * operations. */
        Found = 0x400,          /* The package index script was found
                                 * and processed. */
        Locked = 0x800,         /* This flag is no longer used. */
        Safe = 0x1000,          /* Evaluate the package index script in
                                 * "safe" mode. */
        Evaluated = 0x2000,     /* The package index script was actually
                                 * evaluated. */
        NoComplain = 0x4000,    /* If a package index script fails, just
                                 * ignore the error. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the value used by the auto-path variable trace
        //       callback.
        //
        AutoPath = Host | Normal | NoNormal |
                   Recursive | Trace, /* TODO: Good default? */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used when finding and loading the Harpy / Badge
        //       package index scripts.  This is only done in response to the
        //       "-security" command line option -OR- by calling the ScriptOps
        //       EnableOrDisableSecurity method.
        //
        SecurityPackage = (AutoPath | Safe) & ~(Host | Recursive),

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("753ee5b0-e42b-4c69-b398-960f54ba75dd")]
    public enum PackageFlags
    {
        None = 0x0,            /* Nothing special. */
        Invalid = 0x1,         /* Invalid, do not use. */
        System = 0x2,          /* The package is a system package, do not use. */
        Loading = 0x4,         /* The package is currently being loaded via
                                * PackageRequire. */
        Static = 0x8,          /* The package was provided statically. */
        Core = 0x10,           /* The package is included with the runtime. */
        Plugin = 0x20,         /* The package was provided by a plugin. */
        Library = 0x40,        /* The package is part of the script library. */
        Interactive = 0x80,    /* The package is included with the interactive
                                * shell. */
        Automatic = 0x100,     /* The package was added to the interpreter
                                * automatically. */
        NoUpdate = 0x200,      /* Skip updating package flags upon provide. */
        NoProvide = 0x400,     /* The [package provide] sub-command should
                                * always do nothing. */
        AlwaysSatisfy = 0x800, /* The [package vsatisfies] sub-command should
                                * always return true. */

        ///////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This value is used when evaluating the Harpy / Badge package
        //       indexes prior to the interpreter being initialized.  This is
        //       only done in response to the "-security" command line option
        //       -OR- by calling the ScriptOps.EnableOrDisableSecurity method.
        //
        SecurityPackageMask = NoProvide | AlwaysSatisfy
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("864f7d71-9202-4e44-aa42-0d1c9d3468ff")]
    public enum EventWaitFlags
    {
        None = 0x0,
        Invalid = 0x1,
        NoBgError = 0x2,
        NoCancel = 0x4,
        StopOnError = 0x8,
        ErrorOnEmpty = 0x10,
        UserInterface = 0x20,
        NoUserInterface = 0x40,
        NoComplain = 0x80,
        StopOnComplain = 0x100,
        StopOnGlobalComplain = 0x200,

#if NATIVE && TCL
        TclDoOneEvent = 0x400,
        TclWaitEvent = 0x800, // NOTE: This flag should rarely, if ever, be used.
        TclAllEvents = 0x1000,
#endif

        StopOnAnyComplain = StopOnComplain | StopOnGlobalComplain,
        StopOnAny = StopOnError | StopOnComplain | StopOnGlobalComplain,

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("c5c70166-aa98-4f6f-8d08-1e9099b0aa11")]
    public enum VariableFlags : ulong
    {
        /* general flags (instanced) */

        None = 0x0,
        Invalid = 0x1,            /* Invalid, do not use. */
        Array = 0x2,              /* variable is an array */
        ReadOnly = 0x4,           /* cannot be modified by a script */
        WriteOnly = 0x8,          /* cannot be read by a script */
        Virtual = 0x10,           /* cannot be used with [array get], [array names],
                                   * [array values], [array set], [array size],
                                   * [array startsearch], or [array unset]. */
        System = 0x20,            /* pre-defined script library variable */
        Invariant = 0x40,         /* cannot be modified by a script (reserved) */
        Mutable = 0x80,           /* variable can be modified even in an immutable
                                   * interpreter. */
        Safe = 0x100,             /* variable can be used in a "safe" interpreter. */
        Unsafe = 0x200,           /* variable CANNOT be used in a "safe" interpreter. */
        Link = 0x400,             /* global or upvar alias */
        Undefined = 0x800,        /* declared via global or upvar but not yet set */
        Argument = 0x1000,        /* variable is a formal procedure argument */
        Global = 0x2000,          /* variable is global */
        Local = 0x4000,           /* variable is local to the current procedure */
        Wait = 0x8000,            /* variable is being waited on by the interpreter. */
        Dirty = 0x10000,          /* dirty bit, variable has changed (including unset) */
        NoWatchpoint = 0x20000,   /* disable watches for this variable. */
        NoTrace = 0x40000,        /* disable read/write traces for this variable. */
        NoNotify = 0x80000,       /* disable event notifications for this variabe. */
        NoPostProcess = 0x100000, /* disable post-processing of the variable value. */
        BreakOnGet = 0x200000,    /* break into debugger on get access */
        BreakOnSet = 0x400000,    /* break into debugger on set access */
        BreakOnUnset = 0x800000,  /* break into debugger on unset access */
        Substitute = 0x1000000,   /* subst the variable value prior to returning it */
        Evaluate = 0x2000000,     /* eval the variable value prior to returning it */

        /* "action" and/or "result" flags (non-instanced) */

        NotFound = 0x4000000,                 /* the variable was searched for, but was not found;
                                               * otherwise, the variable was not searched for
                                               * because the name was invalid */
        NoCreate = 0x8000000,                 /* do not create a new variable, only modify
                                               * existing an existing one */
        GlobalOnly = 0x10000000,              /* get or set variable only within the global call
                                               * frame */
        NoArray = 0x20000000,                 /* variable name cannot refer to an array */
        NoElement = 0x40000000,               /* variable name cannot be an element reference */
        NoComplain = 0x80000000,              /* unset without raising "does not exist" errors */
        AppendValue = 0x100000000,            /* append to the value instead of setting */
        AppendElement = 0x200000000,          /* append to the list instead of setting */
        NoFollowLink = 0x400000000,           /* operate on variable link, not the variable itself */
        ResetValue = 0x800000000,             /* reset value(s) to null when unset */

#if !MONO && NATIVE && WINDOWS
        ZeroString = 0x1000000000,            /* Upon [unset], enable forcibly zeroing strings
                                               * that may contain "sensitive" data?  WARNING: THIS
                                               * IS NOT GUARANTEED TO WORK RELIABLY ON ALL PLATFORMS.
                                               * EXTREME CARE SHOULD BE EXERCISED WHEN HANDLING
                                               * ANY SENSITIVE DATA, INCLUDING TESTING THAT THIS
                                               * FLAG WORKS WITHIN THE SPECIFIC TARGET APPLICATION
                                               * AND ON THE SPECIFIC TARGET PLATFORM. */
#endif

        Purge = 0x2000000000,                 /* purge deleted variables in call frame on unset */
        NoSplit = 0x4000000000,               /* skip splitting the variable name from the array
                                               * element index. */
        NoReady = 0x8000000000,               /* force skip of check if interpreter is ready */
        NoRemove = 0x10000000000,             /* do not remove variable from the call frame */
        NoGetArray = 0x20000000000,           /* do not validate the variable as an array or
                                               * non-array */
        NoLinkIndex = 0x40000000000,          /* variable cannot have a valid link index. */
        HasLinkIndex = 0x80000000000,         /* the variable has a valid link index and it should
                                               * not (i.e. we do not want an alias to an array
                                               * element). */
        Defined = 0x100000000000,             /* validate that the variable is not undefined */
        WaitFollowLink = 0x200000000000,      /* follow linked variable during wait. */
        WaitTrace = 0x400000000000,           /* enable tracing for variable wait operations. */
        NoObject = 0x800000000000,            /* skip opaque object handle processing. */
        SkipWatchpoint = 0x1000000000000,     /* skip all variable breakpoints for the duration of
                                               * this method call. */
        SkipTrace = 0x2000000000000,          /* skip all variable traces for the duration of this
                                               * method call. */
        SkipNotify = 0x4000000000000,         /* skip all event notifications for the duration of
                                               * this method call. */
        SkipPostProcess = 0x8000000000000,    /* skip post-processing the variable value for this
                                               * method call.  */
        ResolveNull = 0x10000000000000,       /* allow variable resolvers to return a null
                                               * variable along with a successful return code. */
        NonVirtual = 0x20000000000000,        /* disallow returning virtual variables. */
        WasVirtual = 0x40000000000000,        /* the variable was virtual and it should not be. */
        WasElement = 0x80000000000000,        /* the variable name refers to an array element. */
        NewTraceInfo = 0x100000000000000,     /* force the creation of a new TraceInfo object
                                               * instead of using the pre-allocated one for the
                                               * thread. */
        SkipToString = 0x200000000000000,     /* skip converting the variable value to be returned
                                               * to a string. */
        ForceToString = 0x400000000000000,    /* force conversion of the variable value to string
                                               * prior to being returned. */
        FallbackToString = 0x800000000000000, /* as a fallback, when used with SkipToString, allow
                                               * the variable value to be returned as a string if
                                               * it does not conform to a supported type. */

        /* reserved for integration usage (instanced) */

        Application = 0x1000000000000000,     /* application-defined flag */
        User = 0x2000000000000000,            /* user-defined flag */

        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved1 = 0x4000000000000000,       /* Reserved value, do not use. */
        Reserved2 = 0x8000000000000000,       /* Reserved value, do not use. */
        ReservedMask = Reserved1 | Reserved2,

        ///////////////////////////////////////////////////////////////////////////////////////////

        /* internal library use only */

        Library = System | GlobalOnly,

        /* virtual / system mask (instanced) */

        VirtualOrSystemMask = Virtual | System,

        /* watch flags mask (instanced) */

        BreakOnAny = BreakOnGet | BreakOnSet | BreakOnUnset,
        WatchpointMask = BreakOnAny | Mutable,

        /* no watch/trace/notify/post-process flags (instanced and non-instanced) */

        NonWatchpoint = NoWatchpoint | SkipWatchpoint,
        NonTrace = NoTrace | SkipTrace,
        NonNotify = NoNotify | SkipNotify,
        NonPostProcess = NoPostProcess | SkipPostProcess,

        /* "action" and/or "result" flags (non-instanced) */

        NonInstanceMask = NotFound | NoCreate | GlobalOnly | NoArray |
                          NoElement | NoComplain | AppendValue | AppendElement |
                          NoFollowLink | ResetValue |
#if !MONO && NATIVE && WINDOWS
                          ZeroString |
#endif
                          Purge | NoSplit | NoReady |
                          NoRemove | NoGetArray | NoLinkIndex | HasLinkIndex |
                          Defined | WaitFollowLink | WaitTrace | NoObject |
                          SkipWatchpoint | SkipTrace | SkipNotify | SkipPostProcess |
                          ResolveNull | NonVirtual | WasVirtual | WasElement |
                          NewTraceInfo | SkipToString | ForceToString | FallbackToString |
                          ReservedMask,

        /* flags not allowed when adding variables */

        NonAddMask = Link | Global | Local | Dirty |
                     NonInstanceMask,

        /* flags not allowed when setting variable values */

        NonSetMask = Array | Virtual | Undefined | NonAddMask,

        /* flags masked off when a variable is recycled */

        NonDefinedMask = ReadOnly | WriteOnly | System | Invariant |
                         Mutable | Safe | Unsafe | Argument |
                         Wait | Dirty | NoWatchpoint | NoTrace |
                         NoNotify | NoPostProcess | BreakOnGet | BreakOnSet |
                         BreakOnUnset | Substitute | Evaluate | NonSetMask,

        /* flags used when querying/setting a raw variable value */

        DirectValueMask = Reserved1 | None,
        DirectGetValueMask = DirectValueMask | SkipToString,
        DirectSetValueMask = DirectValueMask,

        /* flags when the variable is being get/set automatically via a
         * property setter method, the engine, or the interactive loop. */

        ViaProperty = NoReady,
        ViaShell = NoReady,
        ViaEngine = NoReady,
        ViaPrompt = GlobalOnly | NoReady,

        /* flags allowed when the interpreter is read-only and/or immutable */

        ReadOnlyMask = System,
        ImmutableMask = System | Mutable,

        /* used for [vwait], etc. */

        WaitVariableMask = WaitFollowLink | WaitTrace,

        /* used for [array], etc. */

        CommonCommandMask = Defined | NonVirtual,
        ArrayCommandMask = CommonCommandMask | NoElement | NoLinkIndex,

        /* used for GetVariable result flags checking (failure reason) */

        ArrayErrorMask = NotFound | HasLinkIndex,
        ErrorMask = ArrayErrorMask | WasVirtual,

        /* flags for maximum performance at the expense of everything else */

        FastMask = NoWatchpoint | NoTrace | NoNotify |
                   NoPostProcess,

        /* flags for use by [unset -zerostring] / [unset -maybezerostring] */

#if !MONO && NATIVE && WINDOWS
        ZeroStringMask = ResetValue | ZeroString,
#endif

        /* flags for use by [namespace which] */

        NamespaceWhichMask = NoElement | Defined,
        GlobalNamespaceWhichMask = GlobalOnly | NamespaceWhichMask,

        /* non-instanced flags for maximum performance */

        FastNonInstanceMask = NoSplit | SkipWatchpoint | SkipTrace |
                              SkipNotify | SkipPostProcess
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("8ed86944-4396-4106-ae22-6c0a0e95c7fc")]
    public enum CallFrameFlags : ulong
    {
        None = 0x0,
        Invalid = 0x1,              /* Invalid, do not use. */
        NoFree = 0x4,               /* This call frame should not be freed
                                     * via the Free() method unless the
                                     * "global" parameter is true.  This
                                     * flag may be set or unset by the
                                     * resolver. */
        Engine = 0x8,               /* Used to indicate a call frame pushed
                                     * by the engine. */
        Global = 0x10,              /* Used to indicate the outermost call
                                     * frame. */
        GlobalScope = 0x20,         /* Used for supporting the [scope global]
                                     * sub-command and its associated code. */
        Procedure = 0x40,           /* Used when a procedure body is being
                                     * evaluated. */
        After = 0x80,               /* Used for the [after] command. */
        Uplevel = 0x100,            /* Used for the [uplevel] command. */
        Downlevel = 0x200,          /* Used for the [downlevel] command. */
        Lambda = 0x400,             /* Used for the [apply] command. */
        Namespace = 0x800,          /* Used for the [namespace] command. */
        Scope = 0x1000,             /* Used for the [scope] command. */
        Evaluate = 0x2000,          /* Used for the [debug lockeval],
                                     * [eval], [interp eval], and
                                     * [namespace eval] commands. */
        InScope = 0x4000,           /* Used for the [namespace inscope]
                                     * command. */
        Source = 0x8000,            /* Used for the [source] command. */
        Substitute = 0x10000,       /* Used for the [subst] command. */
        Expression = 0x20000,       /* Used for the [expr] command. */
        BackgroundError = 0x40000,  /* Used for the [bgerror] command. */
        Try = 0x80000,              /* Used for the [try] command. */
        Catch = 0x100000,           /* Used for the [catch] command. */
        Interpreter = 0x200000,     /* Used for the [interp] command. */
        Test = 0x400000,            /* Used for the [test1] / [test2]
                                     * commands and the dedicated test
                                     * class. */
        Interactive = 0x800000,     /* Used for interactive extension
                                     * command execution. */
        Alias = 0x1000000,          /* Used for aliases. */
        Finally = 0x2000000,        /* Used for the finally block of a
                                     * [try] command. */
        Tcl = 0x4000000,            /* The script or file is being
                                     * evaluated via Tcl. */
        External = 0x8000000,       /* Used to indicate the frame is for
                                     * executing commands from external
                                     * components (i.e. those outside of
                                     * the engine). */
        Tracking = 0x10000000,      /* Used to indicate the frame is for
                                     * tracking purposes. */
        UseNamespace = 0x20000000,  /* Used to indicate the frame points to
                                     * a namespace with variables in the
                                     * call frame owned by that
                                     * namespace. */
        Invisible = 0x40000000,     /* Used to indicate the frame should be
                                     * skipped for [uplevel]. */
        NoInvisible = 0x80000000,   /* Used to indicate the frame should be
                                     * able to see other frames that have
                                     * been marked as invisible (e.g. via
                                     * [info level]). */
        Undefined = 0x100000000,    /* Used in call frame variable cleanup,
                                     * indicates that all variables
                                     * belonging to the call frame are now
                                     * undefined. */
        Automatic = 0x200000000,    /* This flag is used exclusively by the
                                     * engine code to detect call frames
                                     * that should be automatically popped
                                     * upon exiting from the core execution
                                     * routine. */
        Fast = 0x400000000,         /* This call frame disables variable
                                     * tracing and other things (e.g. debug
                                     * watches, etc) which can potentially
                                     * take a long time.*/
        SubCommand = 0x800000000,   /* Used for sub-command execution. */
        Debugger = 0x1000000000,    /* Used by the script debugger. */
        Restricted = 0x2000000000,  /* Special restrictions are applied to
                                     * evaluated scripts, some scripts may
                                     * not work. */
        Application = 0x4000000000, /* Reserved application-defined flag. */
        User = 0x8000000000,        /* Reserved user-defined flag. */

        /* these flags are toggled when marking/unmarking global scope frames */

        GlobalScopeMask = NoFree | GlobalScope,

        /* these frame types MAY "own" variables */

        Variables = Global | Procedure | Lambda | Namespace | Scope | GlobalScope,

        /* these frame types MAY be counted for [info level] */

        InfoLevel = Variables & ~(Global | GlobalScope | Lambda),

        /* these frame types MAY NOT "own" variables */

        NoVariables = Engine | Tracking
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("35136e42-f080-484a-9e4f-871888d49e2b")]
    public enum DetailFlags
    {
        None = 0x0,
        Invalid = 0x1,

        ///////////////////////////////////////////////////////////////////////////////////////////

        EmptySection = 0x2,
        EmptyContent = 0x4,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ICallFrameNameOnly = 0x8,
        ICallFrameToListAll = 0x10,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CallFrameLinked = 0x20,
        CallFrameSpecial = 0x40,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CallFrameVariables = 0x80,
        CallStackAllFrames = 0x100,
        DebuggerBreakpoints = 0x200,
        HostDimensions = 0x400,
        HostFormatting = 0x800,
        HostColors = 0x1000,
        HostNames = 0x2000,
        EngineNative = 0x4000,
        TraceCached = 0x8000,
        VariableLinks = 0x10000,
        VariableSearches = 0x20000,
        VariableElements = 0x40000,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ICallFrameMask = ICallFrameNameOnly | ICallFrameToListAll,

        CallFrameMask = CallFrameLinked | CallFrameSpecial,

        EmptyMask = EmptySection | EmptyContent,

        ContentMask = CallFrameVariables | CallStackAllFrames | DebuggerBreakpoints |
                      HostDimensions | HostFormatting | HostColors |
                      HostNames | EngineNative | TraceCached |
                      VariableLinks | VariableSearches | VariableElements,

        ///////////////////////////////////////////////////////////////////////////////////////////

        ScriptOnly = EmptyContent,

        ///////////////////////////////////////////////////////////////////////////////////////////

        DebugTrace = EmptyContent,

        ///////////////////////////////////////////////////////////////////////////////////////////

        InteractiveOnly = EmptyContent,
        InteractiveAll = EmptyContent | ContentMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        All = EmptyMask | ContentMask,

        ///////////////////////////////////////////////////////////////////////////////////////////

        Default = None
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("f3d19d7d-e026-435b-9a1b-3ac2dd969fc6")]
    public enum ToStringFlags
    {
        None = 0x0,
        Invalid = 0x1,
        NameAndValue = 0x2,
        Decorated = 0x4,
        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("87619eee-2103-4488-a2ce-59cc8d94059e")]
    public enum ResultFlags
    {
        None = 0x0,         // no extra flags.
        Invalid = 0x1,      // invalid, do not use.
        Global = 0x2,       // the result is globally scoped?
        Local = 0x4,        // the result is locally scoped?
        String = 0x8,       // result is a string (always true for now)
        Error = 0x10,       // the result has error information?
        Application = 0x20, // application-defined flag.
        User = 0x40,        // user-defined flag.
        Reserved = unchecked((int)0x80000000)
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("ca16c0e6-08b0-414d-9c1b-d09af3d98a7c")]
    public enum ArgumentFlags
    {
        None = 0x0,
        Invalid = 0x1,      // invalid, do not use.
        Expand = 0x2,       // NOT IMPLEMENTED
        HasDefault = 0x4,   // argument has a default value.
        ArgumentList = 0x8, // argument is part of an "args" argument list.
        Debug = 0x10,       // argument should use name and value when doing ToString().
        NameOnly = 0x20,    // argument should use name only when doing ToString().
        Application = 0x40, // application-defined flag.
        User = 0x80,        // user-defined flag.
        Reserved = unchecked((int)0x80000000),

        ToStringMask = Debug | NameOnly /* flags that impact ToString(). */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
    [Flags()]
    [ObjectId("0c443008-9aec-477d-ab7a-894ad71e4acb")]
    public enum NotifyType : ulong
    {
        None = 0x0,                    /* This is a reserved event value that represents a null
                                        * event. */
        Invalid = 0x1,                 /* The value that represents an invalid event. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Types
        ///////////////////////////////////////////////////////////////////////////////////////////

        Interpreter = 0x2,             /* The event pertains to an IInterpreter object. */
        CallFrame = 0x4,               /* The event pertains to an ICallFrame object. */
        Resolver = 0x8,                /* The event pertains to the resolver (abstract component). */
        Engine = 0x10,                 /* The event pertains to the engine (abstract component). */
        Stream = 0x20,                 /* The event pertains to a stream. */
        File = 0x40,                   /* The event pertains to a file. */

#if XML
        Xml = 0x80,                    /* The event pertains to XML integration. */
        XmlBlock = 0x100,              /* The event pertains to an XML script block. */
#endif

        Expression = 0x200,            /* The event pertains to an expression. */
        Script = 0x400,                /* The event pertains to a script. */
        String = 0x800,                /* The event pertains to a string. */

#if DEBUGGER
        Debugger = 0x1000,             /* The event pertains to the script debugger (abstract
                                        * component). */
#endif

        Variable = 0x2000,             /* The event pertains to an IVariable object. */
        Alias = 0x4000,                /* The event pertains to an IAlias object. */
        IExecute = 0x8000,             /* The event pertains to an IExecute object. */
        HiddenIExecute = 0x10000,      /* The event pertains to an IExecute object. */
        Procedure = 0x20000,           /* The event pertains to an IProcedure object. */
        HiddenProcedure = 0x40000,     /* The event pertains to an IProcedure object. */
        Command = 0x80000,             /* The event pertains to an ICommand object. */
        HiddenCommand = 0x100000,      /* The event pertains to an ICommand object. */
        SubCommand = 0x200000,         /* The event pertains to an ISubCommand object. */
        Operator = 0x400000,           /* The event pertains to an IOperator object. */
        Function = 0x800000,           /* The event pertains to an IFunction object. */
        Plugin = 0x1000000,            /* The event pertains to an IPlugin object. */
        Package = 0x2000000,           /* The event pertains to an IPackage object. */
        Resolve = 0x4000000,           /* The event pertains to an IResolve object. */

#if DATA
        Connection = 0x8000000,        /* The event pertains to an IDbConnection object. */
        Transaction = 0x10000000,      /* The event pertains to an IDbTransaction object. */
#endif

        Callback = 0x20000000,         /* The event pertains to an ICallback object. */

#if NATIVE && LIBRARY
        Module = 0x40000000,           /* The event pertains to an IModule object. */
        Delegate = 0x80000000,         /* The event pertains to an IDelegate object. */
#endif

        Idle = 0x100000000,            /* The event pertains to an idle IEvent object. */
        Event = 0x200000000,           /* The event pertains to an IEvent object. */
        Object = 0x400000000,          /* The event pertains to an IObject object. */

#if NATIVE && TCL
        Tcl = 0x800000000,             /* The event pertains to Tcl integration. */
#endif

#if HISTORY
        History = 0x1000000000,        /* The event pertains to command history. */
#endif

        Policy = 0x2000000000,         /* The event pertains to a policy. */
        Trace = 0x4000000000,          /* The event pertains to a trace. */

#if NATIVE && TCL && TCL_THREADS
        Thread = 0x8000000000,         /* The event pertains to a thread. */
#endif

#if APPDOMAINS
        AppDomain = 0x10000000000,     /* The event pertains to an AppDomain. */
#endif

        Library = 0x20000000000,       /* The event pertains to the script library. */

#if SHELL
        Shell = 0x40000000000,         /* The event pertains to the interactive shell. */
#endif

        RuntimeOption = 0x80000000000, /* The event pertains to a runtime option. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Masks
        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = Invalid,
        CheckMask = All,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // All
        ///////////////////////////////////////////////////////////////////////////////////////////

        All = Interpreter |
              CallFrame |
              Resolver |
              Engine |
              Stream |
              File |

#if XML
              Xml |
              XmlBlock |
#endif

              Expression |
              Script |
              String |

#if DEBUGGER
              Debugger |
#endif

              Variable |
              Alias |
              IExecute |
              HiddenIExecute |
              Procedure |
              HiddenProcedure |
              Command |
              HiddenCommand |
              SubCommand |
              Operator |
              Function |
              Plugin |
              Package |
              Resolve |

#if DATA
              Connection |
              Transaction |
#endif

              Callback |

#if NATIVE && LIBRARY
              Module |
              Delegate |
#endif

              Idle |
              Event |
              Object |

#if NATIVE && TCL
              Tcl |
#endif

#if HISTORY
              History |
#endif

              Policy |
              Trace |

#if NATIVE && TCL && TCL_THREADS
              Thread |
#endif

#if APPDOMAINS
              AppDomain |
#endif

              Library |

#if SHELL
              Shell |
#endif

              RuntimeOption,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Reserved
        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = 0x8000000000000000 /* Reserved value, do not use. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("17e7f251-9140-486f-83ad-5ef4d88c386f")]
    public enum NotifyFlags : ulong
    {
        None = 0x0,            /* This is a reserved event value that represents a null event. */
        Invalid = 0x1,         /* The value that represents an invalid event. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Flags
        ///////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        NoNotify = 0x2,        /* If this flag is set, ALL notifications are temporarily
                                * disabled. */
#endif

        Hidden = 0x4,          /* An entity is hidden. */
        Force = 0x8,           /* The operation on the named entity was forced by the caller. */
        Broadcast = 0x10,      /* The notification should be sent to all interpreters, regardless
                                * of their thread affinity. */
        Safe = 0x20,           /* The notification may be sent to "safe" interpreters. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Lifetime
        ///////////////////////////////////////////////////////////////////////////////////////////

        Reset = 0x40,          /* An entity or subsystem has been reset. */
        PreInitialized = 0x80, /* An entity has been initialized [interpreter]. */
        Setup = 0x100,         /* An entity has been setup [interpreter]. */
        Initialized = 0x200,   /* An entity has been initialized (IState?). */
        Terminated = 0x400,    /* An entity has been terminated (IState?). */
        Exit = 0x800,          /* The exit property of the interpreter was changed (we may
                                * be exiting). */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Engine Entity
        ///////////////////////////////////////////////////////////////////////////////////////////

        Matched = 0x1000,      /* A named entity has been located [by the resolver]. */
        NotFound = 0x2000,     /* A named entity could not be located [by the resolver]. */
        Executed = 0x4000,     /* A named entity has been executed.*/
        Exception = 0x8000,    /* An exception has been caught while executing a named entity. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Interpreter Entity
        ///////////////////////////////////////////////////////////////////////////////////////////

        Added = 0x10000,      /* A named entity has been added [to the interpreter or AppDomain]. */
        Copied = 0x20000,     /* A named entity has been copied [to the interpreter or AppDomain]. */
        Renamed = 0x40000,    /* A named entity has been renamed. */
        Updated = 0x80000,    /* A named entity has been updated. */
        Replaced = 0x100000,  /* A named entity has been replaced. */
        Removed = 0x200000,   /* A named entity has been removed [from the interpreter or AppDomain]. */
        Pushed = 0x400000,    /* A named entity has been pushed [Interpreter or CallFrame]. */
        Popped = 0x800000,    /* A named entity has been popped [Interpreter or callFrame]. */
        Deleted = 0x1000000,  /* A named entity has been deleted [CallFrame]. */
        Disposed = 0x2000000, /* A named entity has been disposed [object]. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Engine Core
        ///////////////////////////////////////////////////////////////////////////////////////////

        Substituted = 0x4000000,   /* A string has been substituted. */
        Evaluated = 0x8000000,     /* A script or expression has been evaluated. */
        Completed = 0x10000000,    /* A script or expression has been evaluated at the outermost level. */
        Canceled = 0x20000000,     /* A script or asynchronous event was canceled. */
        Unwound = 0x40000000,      /* A script or asynchronous event was unwound. */
        Halted = 0x80000000,       /* A script was halted. This is used primarily [by the interactive
                                    * user] to unwind nested instances of the interactive loop. */
        Interrupted = 0x100000000, /* A script was interrupted in some way. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Debugger
        ///////////////////////////////////////////////////////////////////////////////////////////

#if DEBUGGER
        Breakpoint = 0x200000000,  /* A breakpoint was triggered. */
        Watchpoint = 0x400000000,  /* A variable watch breakpoint was triggered. */
#endif

        Trace = 0x800000000,       /* A variable trace breakpoint was triggered. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Asynchronous Events
        ///////////////////////////////////////////////////////////////////////////////////////////

        Queued = 0x1000000000,     /* An asynchronous event has been queued to the interpreter. */
        Dequeued = 0x2000000000,   /* An asynchronous event has been dequeued from the interpreter. */
        Discarded = 0x4000000000,  /* An asynchronous event has been discarded by the interpreter. */
        Cleared = 0x8000000000,    /* All asynchronous events have been cleared for the interpreter. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Input/Output Events
        ///////////////////////////////////////////////////////////////////////////////////////////

        Read = 0x10000000000,      /* Data has been read. */
        Write = 0x20000000000,     /* Data has been written. */

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Tcl Variable Events
        ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        GetVariable = 0x40000000000,    /* The Tcl variable value was fetched. */
        SetVariable = 0x80000000000,    /* A Tcl variable was set. */
        UnsetVariable = 0x100000000000, /* A Tcl variable was unset. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Load/Unload Events
        ///////////////////////////////////////////////////////////////////////////////////////////

#if NOTIFY
        PreLoad = 0x200000000000,   /* A plugin or other loadable module is about to be loaded. */
        PreUnload = 0x400000000000, /* A plugin or other loadable module is about to be unloaded. */
        Load = 0x800000000000,      /* A plugin or other loadable module has been loaded. */
        Unload = 0x1000000000000,   /* A plugin or other loadable module has been unloaded. */
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Masks
        ///////////////////////////////////////////////////////////////////////////////////////////

        FlagsMask = Invalid |

#if NOTIFY || NOTIFY_OBJECT
                    NoNotify |
#endif

                    Hidden | Force | Broadcast | Safe,

        ///////////////////////////////////////////////////////////////////////////////////////////

        CheckMask = All,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // All
        ///////////////////////////////////////////////////////////////////////////////////////////

        All = Reset |
              PreInitialized |
              Setup |
              Initialized |
              Terminated |
              Exit |
              Matched |
              NotFound |
              Executed |
              Exception |
              Added |
              Copied |
              Renamed |
              Updated |
              Replaced |
              Removed |
              Pushed |
              Popped |
              Deleted |
              Disposed |
              Substituted |
              Evaluated |
              Completed |
              Canceled |
              Unwound |
              Halted |

#if DEBUGGER
              Breakpoint |
              Watchpoint |
#endif

              Trace |
              Queued |
              Dequeued |
              Discarded |
              Cleared |
              Read |
              Write |

#if NOTIFY
              PreLoad |
              PreUnload |
              Load |
              Unload |
#endif

              None,

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Reserved
        ///////////////////////////////////////////////////////////////////////////////////////////

        Reserved = 0x8000000000000000 /* Reserved value, do not use. */
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("6b6288ac-4c47-45e6-bea1-8a54b072aea3")]
    public enum PackagePreference
    {
        Invalid = -1,  /* invalid, do not use. */
        None = 0x0,    /* handling is unspecified, do not use. */
        Default = 0x1, /* no specific preference, use default handling. */
        Latest = 0x2,  /* always favor the VERY latest package version, even if alpha or beta. */
        Stable = 0x4   /* always favor the latest stable version. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

#if XML
    [ObjectId("7ef0e3e3-d336-4a9f-baf1-69c2553f69d3")]
    public enum XmlBlockType
    {
        Invalid = -1,
        None = 0,
        Automatic = 1,
        Text = 2,
        Base64 = 3,
        Uri = 4
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("acdb88e0-517b-4dd1-8b95-cb81ebf0dcae")]
    public enum SubstitutionFlags
    {
        None = 0x0,
        Invalid = 0x1,
        Backslashes = 0x2,
        Variables = 0x4,
        Commands = 0x8,

        All = Backslashes | Variables | Commands,
        Default = All
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("d9f761f8-bfd1-4102-819a-0e583609c45f")]
    public enum ExpressionFlags
    {
        None = 0x0,
        Invalid = 0x1,

#if EXPRESSION_FLAGS
        Backslashes = 0x2,
        Variables = 0x4,
        Commands = 0x8,
        Operators = 0x10,
        Functions = 0x20,
#endif

        BooleanToInteger = 0x40,

#if EXPRESSION_FLAGS
        Substitutions = Backslashes | Variables | Commands,
        Mathematics = Operators | Functions,
        All = Substitutions | Mathematics,
#else
        All = None,
#endif

        Default = All
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("7f46620d-7f45-469b-a4e7-6bbcd644aeee")]
    public enum EngineFlags : ulong
    {
        None = 0x0,                      /* Default flags (i.e. none). */
        Invalid = 0x1,                   /* Invalid, do not use. */
        BracketTerminator = 0x2,         /* Current command should be terminated by a close bracket. */
        UseIExecutes = 0x4,              /* Use IExecute objects during evaluation? */
        UseCommands = 0x8,               /* Use ICommand objects during evaluation? */
        UseProcedures = 0x10,            /* Use IProcedure objects during evaluation? */
        UseHidden = 0x20,                /* Use the list of hidden IExecute, ICommand, or IProcedure
                                          * entities. */
        ToExecute = 0x40,                /* We are looking up an entity to execute it (via the
                                          * IExecute interface).  This flag is for use by the Engine
                                          * class only. */
        ExactMatch = 0x80,               /* Do not match non-exact command and procedure names? */
        GetHidden = 0x100,               /* Allow fetching of hidden commands and/or procedures? */
        MatchHidden = 0x200,             /* Allow matching of hidden commands and/or procedures? */
        IgnoreHidden = 0x400,            /* Totally ignore the hidden command/procedure flags? */
        InvokeHidden = 0x800,            /* Allow execution of hidden commands and/or procedures? */
        CheckStack = 0x1000,             /* Enable stack space checking during interpreter readiness
                                          * checks? */
        ForceStack = 0x2000,             /* Force stack space checking during interpreter readiness
                                          * checks? */
        ForcePoolStack = 0x4000,         /* Check native stack space for thread pool threads as well. */
        ForceSoftEof = 0x8000,           /* The engine should never read past a "soft" end-of-file,
                                          * even with policy checking enabled. */
        NoUnknown = 0x10000,             /* Do not fallback on the "unknown" command/procedure for
                                          * unknown commands? */
        NoCancel = 0x20000,              /* Skip script cancellation checking during interpreter
                                          * readiness checks? */
        NoReady = 0x40000,               /* Disable interpreter readiness checks? */
        NoPolicy = 0x80000,              /* Disable all engine policy execution? */

#if DEBUGGER
        NoBreakpoint = 0x100000,         /* Disable debugger breakpoints? */
        NoWatchpoint = 0x200000,         /* Disable variable watches? */
#endif

        NoEvent = 0x400000,              /* Disable asynchronous event processing? */
        NoEvaluate = 0x800000,           /* Totally disable script evaluation? */
        NoSubstitute = 0x1000000,        /* Totally disable token substitution? */
        NoResetResult = 0x2000000,       /* Skip resetting the interpreter result? */
        NoResetError = 0x4000000,        /* Skip resetting error related flags? */
        ResetCancel = 0x8000000,         /* Used to determine if the cancel flag should be
                                          * forcibly reset. */
        ResetReturnCode = 0x10000000,    /* Used to determine if the last return code should
                                          * be forcibly reset. */
        EvaluateGlobal = 0x20000000,     /* Evaluate script in the global scope without regard
                                          * to the current scope? */
        ErrorInProgress = 0x40000000,    /* Error information is being logged as the stack
                                          * unwinds. */
        ErrorAlreadyLogged = 0x80000000, /* Error information has already been logged for the
                                          * current call. */
        ErrorCodeSet = 0x100000000,      /* Error code has been set for the current call. */

#if NOTIFY || NOTIFY_OBJECT
        NoNotify = 0x200000000,          /* Disable all notifications for the script? */
#endif

#if HISTORY
        NoHistory = 0x400000000,         /* Disable command history for the script? */
#endif

#if CALLBACK_QUEUE
        NoCallbackQueue = 0x800000000,   /* Disable the evaluation engine callback queue? */
#endif

        Interactive = 0x1000000000,      /* Script is being evaluated by an interactive user? */
        NoHost = 0x2000000000,           /* Do not fallback on evaluating a matching host script? */
        NoRemote = 0x4000000000,         /* Disallow evaluating remote script files? */

#if XML
        NoXml = 0x8000000000,            /* Disable auto-detection and evaluation of scripts
                                          * conforming to our XML schema? */
#endif

        NoInteractiveCommand = 0x10000000000, /* Disable interactive extension commands.  This
                                               * flag only applies to the interactive loop. */

        DeniedByPolicy = 0x20000000000,       /* The file or script has been denied by policy. */
        NoSafeFunction = 0x40000000000,       /* Allow all functions to be executed in safe
                                               * interpreters, include those that are NOT marked
                                               * as "safe". */
        ExternalExecution = 0x80000000000,    /* An external execution context is active and still
                                               * needs to be removed. */
        NoDebuggerArguments = 0x100000000000, /* Skip setting the command name and arguments for
                                               * use by the debugger. */
        NoCache = 0x200000000000,             /* Skip looking up executable entities in the cache
                                               * (e.g. commands, procedures, etc). */
        GlobalOnly = 0x400000000000,          /* Force command to be looked up in the global
                                               * namespace. */
        UseInterpreter = 0x800000000000,      /* When applicable, combine engine flags with the
                                               * ones from the provided interpreter. */
        ExternalScript = 0x1000000000000,     /* Force extra BeforeScript policy checks in the
                                               * ReadScriptStream and ReadScriptFile methods
                                               * due to being passed an external IScript. */
        FileCallFrame = 0x2000000000000,      /* The EvaluateFile and SubstituteFile methods of
                                               * the Engine should push and pop an "Engine" call
                                               * frame. */
        StreamCallFrame = 0x4000000000000,    /* The EvaluateStream and SubstituteStream methods
                                               * of the Engine should push and pop an "Engine"
                                               * call frame. */
        NoFileNameOnly = 0x8000000000000,     /* Disallow use of Path.GetFileName by the Engine
                                               * class. */
        NoRawName = 0x10000000000000,         /* Disallow use of raw names by the Engine
                                               * class. */
        AllErrors = 0x20000000000000,         /* Return all errors seen while trying to locate a
                                               * script [file?] to evaluate. */
        NoDefaultError = 0x40000000000000,    /* Return the detailed error (if possible) that is
                                               * seen while trying to locate a script [file?] to
                                               * evaluate. */
#if PARSE_CACHE
        NoCacheParseState = 0x80000000000000, /* EXPERIMENTAL: Prevent the engine from caching
                                               * ParseState object instances.  This flag may be
                                               * removed in later releases. */
#endif
#if ARGUMENT_CACHE
        NoCacheArgument = 0x100000000000000,  /* EXPERIMENTAL: Prevent the engine from caching
                                               * Argument object instances.  This flag may be
                                               * removed in later releases. */
#endif
        PostScriptBytes = 0x200000000000000,  /* When reading a script file, also read the
                                               * post-script bytes after the current "soft"
                                               * end-of-file character. */
        SeekSoftEof = 0x400000000000000,      /* The engine should seek to the next "soft"
                                               * end-of-file, prior to reading post-script
                                               * bytes from a stream. */

        //
        // NOTE: Stack checking related engine flags.
        //
        BaseStackMask = CheckStack | ForcePoolStack,
        FullStackMask = CheckStack | ForceStack | ForcePoolStack,

        //
        // NOTE: For use by the EvaluatePromptScript method only.
        //
        ForPrompt = NoCancel
#if DEBUGGER
            | NoBreakpoint
#endif
            ,

        //
        // NOTE: Engine flags allowed for use with [interp readorgetscriptfile]
        //       sub-command.
        //
        ReadOrGetScriptFileMask = ForceSoftEof | NoPolicy | NoNotify |
                                  NoHost | NoRemote |
#if XML
                                  NoXml |
#endif
                                  ExternalScript | NoFileNameOnly | NoRawName |
                                  AllErrors | NoDefaultError,

#if DEBUGGER
        //
        // NOTE: Used when evaluating an interpreter or shell startup script
        //       in the debugger.
        //
        DebuggerExecution = NoWatchpoint,
#endif

        //
        // NOTE: When looking up entities to execute, use all available entity
        //       types.
        //
        UseAll = UseCommands | UseProcedures | UseIExecutes,

        //
        // NOTE: All the error handling related flags.
        //
        ErrorMask = ErrorInProgress | ErrorAlreadyLogged | ErrorCodeSet,

        //
        // NOTE: All enabled/disabled related flags.
        //
        EnabledMask = NoEvaluate | NoSubstitute,

        //
        // NOTE: The high-bit is reserved, please do not use it.
        //
        Reserved = 0x8000000000000000  /* Reserved value, do not use. */
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [Flags()]
    [ObjectId("6572cb04-c5c9-48ff-b0e1-60ace408f8cb")]
    public enum CharacterType
    {
        None = 0x0,
        Invalid = 0x1,
        Normal = None,
        Space = 0x2,
        CommandTerminator = 0x4,
        Substitution = 0x8,
        Quote = 0x10,
        CloseParenthesis = 0x20,
        CloseBracket = 0x40,
        Brace = 0x80
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    [ObjectId("ce29fc68-cc0d-4eb1-98c6-7d02b6d0bbd5")]
    public enum Lexeme
    {
        /*
         * Basic lexemes:
         */

        Unknown,
        UnknownCharacter,
        Literal,
        FunctionName,
        OpenBracket,
        OpenBrace,
        OpenParenthesis,
        CloseParenthesis,
        DollarSign,
        QuotationMark,
        Comma,
        End,

        /*
         * Binary numeric operators:
         */

        Exponent,
        Multiply,
        Divide,
        Modulus,

        Plus,  // NOTE: Also unary.
        Minus, // NOTE: Also unary.

        LeftShift,
        RightShift,
        LeftRotate,
        RightRotate,

        LessThan,
        GreaterThan,
        LessThanOrEqualTo,
        GreaterThanOrEqualTo,

        Equal,
        NotEqual,

        BitwiseAnd,
        BitwiseXor,
        BitwiseOr,
        BitwiseEqv,
        BitwiseImp,

        LogicalAnd,
        LogicalXor,
        LogicalOr,
        LogicalEqv,
        LogicalImp,

        /*
         * The ternary "if" operator.
         */

        Question,
        Colon,

        /*
         * Unary operators. Unary minus and plus are represented by the (binary)
         * lexemes "Minus" and "Plus" (above).
         */

        LogicalNot,
        BitwiseNot,

        /*
         * Binary string operators:
         */

        StringGreaterThan,
        StringGreaterThanOrEqualTo,
        StringLessThan,
        StringLessThanOrEqualTo,

        StringEqual,
        StringNotEqual,
        StrStartsWith,
        StrEndsWith,
        StringContains,
        F5LogicalAnd,
        F5LogicalOr,
        F5LogicalNot,
        F5MatchesRegex,

        /*
         * Binary list operators:
         */

        ListIn,
        ListNotIn,

        /*
         * Operator value bounds:
         */

        Minimum = Unknown,
        Maximum = ListNotIn
    }
}
