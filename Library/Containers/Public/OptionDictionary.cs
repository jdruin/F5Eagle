/*
 * OptionDictionary.cs --
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
using System.Globalization;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Public
{
    //
    // TODO: Centralize ALL options using ArgumentListOptionDictionary
    //       from a ranged ArgumentList to OptionDictionary.
    //
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("755e6bb2-b7e3-42df-bc4e-81610901e093")]
    public sealed class OptionDictionary : Dictionary<string, IOption>
    {
        #region Private Constructors
        private OptionDictionary(
            bool system
            )
            : base()
        {
            if (system)
                AddSystemOptions(false, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public OptionDictionary()
            : this(true)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public OptionDictionary(
            IEnumerable<IOption> collection
            )
            : this()
        {
            foreach (IOption item in collection)
                MaybeAdd(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public OptionDictionary(
            IEnumerable<IOption> collection1,
            IEnumerable<IOption> collection2
            )
            : this(collection1)
        {
            foreach (IOption item in collection2)
                MaybeAdd(item);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private OptionDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        private static IEnumerable<IOption> CreateSystemOptions(
            )
        {
            OptionFlags flags = OptionFlags.System;

            return new IOption[] {
                new Option(null, flags | OptionFlags.ListOfOptions,
                Index.Invalid, Index.Invalid, Option.ListOfOptions, null),
            };
        }

        ///////////////////////////////////////////////////////////////////////

        public static OptionDictionary FromString(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            bool allowInteger,
            bool strict,
            bool verbose,
            bool noCase,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            StringList list = null;

            if (Parser.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                OptionDictionary options = new OptionDictionary();

                foreach (string element in list)
                {
                    IOption option = Option.FromString(
                        interpreter, element, appDomain,
                        allowInteger, strict, verbose,
                        noCase, cultureInfo, ref error);

                    if (option == null)
                        return null;

                    if (options.Has(option))
                    {
                        error = String.Format(
                            "duplicate option name {0}",
                            FormatOps.WrapOrNull(option.Name));

                        return null;
                    }

                    options.Add(option);
                }

                return options;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static OptionDictionary FromString(
            Interpreter interpreter,
            string text,
            AppDomain appDomain,
            ValueFlags valueFlags,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            StringList list = null;

            if (Parser.SplitList(
                    interpreter, text, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                OptionDictionary options = new OptionDictionary();

                foreach (string element in list)
                {
                    IOption option = Option.FromString(
                        interpreter, element, appDomain,
                        valueFlags, cultureInfo, ref error);

                    if (option == null)
                        return null;

                    if (options.Has(option))
                    {
                        error = String.Format(
                            "duplicate option name {0}",
                            FormatOps.WrapOrNull(option.Name));

                        return null;
                    }

                    options.Add(option);
                }

                return options;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Add Methods
        public void Add(
            IOption item
            )
        {
            this.Add(item.Name, item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void MaybeAdd(
            IOption item
            )
        {
            if (!Has(item)) Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        private void AddSystemOptions(
            bool force,
            bool strict
            )
        {
            IEnumerable<IOption> collection = CreateSystemOptions();

            if (collection != null)
            {
                foreach (IOption item in collection)
                {
                    if (item == null)
                        continue;

                    if (force)
                        Replace(item);
                    else if (strict)
                        Add(item);
                    else
                        MaybeAdd(item);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Replace Methods
        public void Replace(
            IOption item
            )
        {
            this[item.Name] = item;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Has (Is Available) Methods
        public bool Has(
            IIdentifierBase item
            )
        {
            return (item != null) ? Has(item.Name) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Has(
            string name
            )
        {
            return Has(this, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Has(
            string name,
            ref IOption option
            )
        {
            return Has(this, name, ref option);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Has(
            OptionDictionary options,
            string name
            )
        {
            IOption option = null;

            return Has(options, name, ref option);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool Has(
            OptionDictionary options,
            string name,
            ref IOption option
            )
        {
            Result error = null;

            return TryResolveSimple(
                options, name, false, ref option, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option CanBePresent (Is Usable) Methods
        public bool CanBePresent(
            string name,
            ref Result error
            )
        {
            return CanBePresent(this, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanBePresent(
            OptionDictionary options,
            string name,
            ref Result error
            )
        {
            IOption option = null;

            if (!TryResolveSimple(options, name, true, ref option, ref error))
                return false;

            return option.CanBePresent(options, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option IsPresent (Is Set) Methods
        public bool IsPresent(
            string name
            )
        {
            return IsPresent(name, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsPresent(
            string name,
            ref Variant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(name, ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsPresent(
            string name,
            ref Variant value,
            ref int index
            )
        {
            return IsPresent(name, false, ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsPresent(
            string name,
            bool noCase
            )
        {
            Variant value = null;

            return IsPresent(name, noCase, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsPresent(
            string name,
            bool noCase,
            ref Variant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(name, noCase, ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsPresent(
            string name,
            bool noCase,
            ref Variant value,
            ref int index
            )
        {
            return IsPresent(this, name, noCase, ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPresent(
            OptionDictionary options,
            string name,
            bool noCase
            )
        {
            Variant value = null;

            return IsPresent(options, name, noCase, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPresent(
            OptionDictionary options,
            string name,
            bool noCase,
            ref Variant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(options, name, noCase, ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPresent(
            OptionDictionary options,
            string name,
            bool noCase,
            ref Variant value,
            ref int index
            )
        {
            return IsPresent(
                options, name, noCase, true, ref value, ref index);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Internal
        internal bool CheckPresent(
            string name
            )
        {
            Variant value = null;

            return CheckPresent(name, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        internal bool CheckPresent(
            string name,
            ref Variant value
            )
        {
            int index = Index.Invalid;

            return IsPresent(this, name, false, false, ref value, ref index);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private
        private static bool IsPresent(
            OptionDictionary options,
            string name,
            bool noCase,
            bool strict,
            ref Variant value,
            ref int index
            )
        {
            if (options != null)
            {
                if (name != null)
                {
                    if (noCase)
                    {
                        //
                        // HACK: Perform a linear search of the options.  We
                        //       should not need to do this since the options
                        //       are in a dictionary; however, we want to
                        //       preserve the "case-sensitive" semantics unless
                        //       otherwise requested by the caller.
                        //
                        bool found = false;

                        foreach (KeyValuePair<string, IOption> pair in options)
                        {
                            if (String.Compare(pair.Key, 0, name, 0, name.Length,
                                    StringOps.SystemNoCaseStringComparisonType) == 0)
                            {
                                found = true;

                                IOption option = pair.Value;

                                if ((option != null) &&
                                    option.IsPresent(options, ref value))
                                {
                                    index = option.Index;
                                    return true;
                                }
                            }
                        }

                        if (strict && !found)
                        {
                            //
                            // NOTE: This should not really happen, issue a
                            //       debug trace.
                            //
                            TraceOps.DebugTrace(String.Format(
                                "IsPresent: {0}",
                                BadOption(options, name)),
                                typeof(OptionDictionary).Name,
                                TracePriority.Command);
                        }
                    }
                    else
                    {
                        IOption option;

                        if (options.TryGetValue(name, out option))
                        {
                            if ((option != null) &&
                                option.IsPresent(options, ref value))
                            {
                                index = option.Index;
                                return true;
                            }
                        }
                        else if (strict)
                        {
                            //
                            // NOTE: This should not really happen, issue a
                            //       debug trace.
                            //
                            TraceOps.DebugTrace(String.Format(
                                "IsPresent: {0}",
                                BadOption(options, name)),
                                typeof(OptionDictionary).Name,
                                TracePriority.Command);
                        }
                    }
                }
            }

            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Setting Methods
        public bool SetPresent(
            string name,
            bool present,
            int index,
            Variant value
            )
        {
            return SetPresent(this, name, present, index, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetPresent(
            OptionDictionary options,
            string name,
            bool present,
            int index,
            Variant value
            )
        {
            if (options != null)
            {
                if (name != null)
                {
                    IOption option;

                    if (options.TryGetValue(name, out option))
                    {
                        if (option != null)
                        {
                            option.SetPresent(options, present, index, value);

                            return true;
                        }
                    }
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Lookup Methods
        private static bool TryResolveSimple(
            OptionDictionary options,
            string name,
            bool verbose,
            ref IOption option,
            ref Result error
            )
        {
            if (options == null)
            {
                error = "invalid options";
                return false;
            }

            if (name == null)
            {
                error = "invalid option name";
                return false;
            }

            if (!options.TryGetValue(name, out option))
            {
                error = BadOption(verbose ? options : null, name);
                return false;
            }

            if (option == null)
            {
                error = String.Format("invalid option \"{0}\"", name);
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode TryResolve(
            string name,
            bool strict,
            bool noCase,
            ref IOption option,
            ref Result error
            )
        {
            return TryResolve(
                this, name, strict, noCase, ref option, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryResolve(
            OptionDictionary options,
            string name,
            bool strict,
            bool noCase,
            ref IOption option,
            ref Result error
            )
        {
            if (options != null)
            {
                if (name != null)
                {
                    string exactName = null;
                    StringList list = new StringList();

                    foreach (KeyValuePair<string, IOption> pair in options)
                    {
                        string key = pair.Key;
                        IOption value = pair.Value;
                        bool match;

                        if (noCase || ((value != null) && value.IsNoCase(options)))
                        {
                            match = (String.Compare(key, 0, name, 0, name.Length,
                                StringOps.SystemNoCaseStringComparisonType) == 0);
                        }
                        else
                        {
                            match = (String.Compare(key, 0, name, 0, name.Length,
                                StringOps.SystemStringComparisonType) == 0);
                        }

                        if (match)
                        {
                            //
                            // NOTE: Was the key valid (this should always succeed).
                            //
                            if (key != null)
                            {
                                //
                                // NOTE: It was a match; however, was it an exact match?
                                //
                                if (key.Length == name.Length)
                                    //
                                    // NOTE: Preserve match, it may differ in case.
                                    //
                                    exactName = key;

                                //
                                // NOTE: Was it an exact match or did we match at least one 
                                //       character in a partial match?
                                //
                                if ((key.Length == name.Length) || (name.Length > 0))
                                    //
                                    // NOTE: Store the exact or partial match in the results 
                                    //       dictionary.
                                    //
                                    list.Add(key);
                            }
                        }
                    }

                    //
                    // NOTE: If there was an exact match, just use it.
                    //
                    if (exactName != null)
                    {
                        //
                        // NOTE: Normal case, an exact option match was found.
                        //
                        option = options[exactName];

                        return ReturnCode.Ok;
                    }
                    else if (list.Count == 1)
                    {
                        //
                        // NOTE: Normal case, exactly one option partially matched.
                        //
                        option = options[list[0]];

                        return ReturnCode.Ok;
                    }
                    else if (list.Count > 1)
                    {
                        //
                        // NOTE: They specified an ambiguous option.
                        //
                        error = AmbiguousOption(options, name, list);
                    }
                    else if (strict)
                    {
                        //
                        // NOTE: They specified a non-existent option.
                        //
                        error = BadOption(options, name);
                    }
                    else
                    {
                        //
                        // NOTE: Non-strict mode, leave the original option value 
                        //       unchanged and let the caller deal with it.
                        //
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    error = "invalid option name";
                }
            }
            else
            {
                error = "invalid options";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ArgumentList Building Methods
        public ReturnCode ToArgumentList(
            ref ArgumentList arguments,
            ref Result error
            )
        {
            return ToArgumentList(this, ref arguments, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ToArgumentList(
            OptionDictionary options,
            ref ArgumentList arguments,
            ref Result error
            )
        {
            if (options != null)
            {
                if (options.Count > 0)
                {
                    if (arguments == null)
                        arguments = new ArgumentList();

                    IOption endOption = null;

                    foreach (KeyValuePair<string, IOption> pair in options)
                    {
                        IOption option = pair.Value;

                        if (option == null)
                            continue;

                        if (option.IsIgnored(options))
                            continue;

                        //
                        // TODO: Is this a good idea (i.e. simply ignoring
                        //       the list-of-options flag instead of raising
                        //       an error)?
                        //
                        if (option.HasFlags(OptionFlags.ListOfOptions, true))
                            continue;

                        if (!option.HasFlags(OptionFlags.EndOfOptions, true))
                        {
                            Variant value = null;

                            if (!option.IsPresent(options, ref value))
                                continue;

                            if (!option.CanBePresent(options, ref error))
                                return ReturnCode.Error;

                            arguments.Add(Argument.InternalCreate(option.Name));

                            if (option.MustHaveValue(options))
                                arguments.Add(Argument.InternalCreate(value));
                        }
                        else
                        {
                            //
                            // NOTE: This option must be processed last; however,
                            //       we still need to keep track of it now until
                            //       that time.
                            //
                            endOption = option;
                        }
                    }

                    if ((endOption != null) && !endOption.IsIgnored(options))
                    {
                        Variant value = null;

                        if (endOption.IsPresent(options, ref value))
                        {
                            if (!endOption.CanBePresent(options, ref error))
                                return ReturnCode.Error;

                            arguments.Add(Argument.InternalCreate(endOption.Name));

                            if (endOption.MustHaveValue(options))
                                arguments.Add(Argument.InternalCreate(value));
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            else
            {
                error = "invalid options";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Message Methods
        public static string ToEnglish(
            IDictionary<string, string> dictionary
            )
        {
            return GenericOps<string>.ListToEnglish(
                dictionary, ", ", Characters.Space.ToString(), "or ");
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result ListOptions(
            OptionDictionary options,
            bool safe
            )
        {
            if (options == null)
                return "there are no available options";

            IDictionary<string, string> dictionary;

            if (safe)
            {
                dictionary = new StringSortedList();

                foreach (KeyValuePair<string, IOption> pair in options)
                {
                    IOption option = pair.Value;

                    if ((option == null) || option.IsUnsafe(options))
                        continue;

                    string name = pair.Key;

                    if (name == null)
                        continue;

                    dictionary.Add(name, null);
                }
            }
            else
            {
                dictionary = new StringSortedList(options.Keys);
            }

            return String.Format(
                "available options are {0}", ToEnglish(dictionary));
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result AmbiguousOption(
            OptionDictionary options, /* NOT REALLY USED */
            string name,
            StringList list
            )
        {
            if ((options == null) || (list == null))
            {
                return String.Format(
                    "ambiguous option \"{0}\"", name); // FIXME: Fallback here?
            }

            return String.Format(
                "ambiguous option \"{0}\": must be {1}", name,
                // ToEnglish(new StringSortedList(options.Keys)));
                ToEnglish(new StringSortedList(list)));
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadOption(
            OptionDictionary options,
            string name
            )
        {
            if (options == null)
            {
                return String.Format(
                    "bad option \"{0}\"", name); // FIXME: Fallback here?
            }

            return String.Format(
                "bad option \"{0}\": must be {1}", name,
                ToEnglish(new StringSortedList(options.Keys)));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return GenericOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
