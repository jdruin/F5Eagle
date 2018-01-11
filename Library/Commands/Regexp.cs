/*
 * Regexp.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("3b73b31e-24ef-4161-9213-ebf943c0a628")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Regexp : Core
    {
        public Regexp(
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
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 3)
                    {
                        OptionDictionary options = new OptionDictionary(
                            new IOption[] { 
                            new Option(null, OptionFlags.Unsupported, Index.Invalid, Index.Invalid, "-about", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-all", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-debug", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-ecma", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-compiled", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-explicit", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-reverse", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-expanded", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-indexes", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-indices", null), /* COMPAT: Tcl. */
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-inline", null),
                            new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-limit", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-line", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-lineanchor", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-linestop", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-nocase", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, "-noculture", null),
                            new Option(null, OptionFlags.MustHaveValue, Index.Invalid, Index.Invalid, "-start", null),
                            new Option(null, OptionFlags.MustHaveIntegerValue, Index.Invalid, Index.Invalid, "-length", null),
                            new Option(null, OptionFlags.None, Index.Invalid, Index.Invalid, Option.EndOfOptions, null)
                        });

                        int argumentIndex = Index.Invalid;
                        
                        code = interpreter.GetOptions(options, arguments, 0, 1, Index.Invalid, false, ref argumentIndex, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if ((argumentIndex != Index.Invalid) && ((argumentIndex + 1) < arguments.Count))
                            {
                                bool all = false;
                                bool debug = false;
                                bool indexes = false;
                                bool inline = false;

                                string pattern = arguments[argumentIndex];
                                string input = arguments[argumentIndex + 1];

                                RegexOptions regExOptions = StringOps.DefaultRegExOptions;

                                Variant value = null;
                                int limit = Count.Invalid;

                                if (options.IsPresent("-limit", ref value))
                                    limit = (int)value.Value;

                                int length = input.Length;

                                if (options.IsPresent("-length", ref value))
                                {
                                    length = (int)value.Value;

                                    if ((length < 0) || (length > input.Length))
                                        length = input.Length;
                                }

                                int startIndex = 0;

                                if (options.IsPresent("-start", ref value))
                                {
                                    //
                                    // NOTE: Handle "end-X", etc.
                                    //
                                    code = Value.GetIndex(
                                        value.ToString(), length,
                                        ValueFlags.AnyIndex,
                                        interpreter.CultureInfo,
                                        ref startIndex, ref result);

                                    if (code == ReturnCode.Ok)
                                    {
                                        if (startIndex < 0)
                                            startIndex = 0;

                                        if (startIndex > length)
                                            startIndex = length;
                                    }
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    if (options.IsPresent("-all"))
                                        all = true;

                                    if (options.IsPresent("-debug"))
                                        debug = true;

                                    if (options.IsPresent("-indexes") || options.IsPresent("-indices"))
                                        indexes = true;

                                    if (options.IsPresent("-inline"))
                                        inline = true;

                                    if (options.IsPresent("-ecma"))
                                        regExOptions |= RegexOptions.ECMAScript;

                                    if (options.IsPresent("-compiled"))
                                        regExOptions |= RegexOptions.Compiled;

                                    if (options.IsPresent("-explicit"))
                                        regExOptions |= RegexOptions.ExplicitCapture;

                                    if (options.IsPresent("-reverse"))
                                        regExOptions |= RegexOptions.RightToLeft;

                                    if (options.IsPresent("-expanded"))
                                        regExOptions |= RegexOptions.IgnorePatternWhitespace;

                                    if (options.IsPresent("-line"))
                                    {
                                        regExOptions &= ~RegexOptions.Singleline;
                                        regExOptions |= RegexOptions.Multiline;
                                    }

                                    if (options.IsPresent("-lineanchor"))
                                        regExOptions |= RegexOptions.Multiline;

                                    if (options.IsPresent("-linestop"))
                                        regExOptions &= ~RegexOptions.Singleline;

                                    if (options.IsPresent("-nocase"))
                                        regExOptions |= RegexOptions.IgnoreCase;

                                    if (options.IsPresent("-noculture"))
                                        regExOptions |= RegexOptions.CultureInvariant;

                                    if (!inline || ((argumentIndex + 2) >= arguments.Count))
                                    {
                                        Regex regEx = null;

                                        try
                                        {
                                            regEx = new Regex(pattern, regExOptions);
                                        }
                                        catch (Exception e)
                                        {
                                            Engine.SetExceptionErrorCode(interpreter, e);

                                            result = String.Format(
                                                "couldn't compile regular expression pattern: {0}",
                                                e.Message);

                                            code = ReturnCode.Error;
                                        }

                                        //
                                        // NOTE: If the result is still Ok, then we know that the regular 
                                        //       expression pattern was compiled and the regEx object was
                                        //       created successfully.
                                        //
                                        if (code == ReturnCode.Ok)
                                        {
                                            //
                                            // NOTE: Inline matches list, only created and populated when 
                                            //       we are operating in inline mode.
                                            //
                                            StringList matches = null;

                                            if (inline)
                                            {
                                                //
                                                // NOTE: Inline mode, create an empty inline matches 
                                                //       list.
                                                //
                                                matches = new StringList();
                                            }

                                            /*
                                             * The following loop is to handle multiple matches within the
                                             * same source string;  each iteration handles one match.  If "-all"
                                             * hasn't been specified then the loop body only gets executed once.
                                             * We terminate the loop when the starting offset is past the end of the
                                             * string.
                                             */

                                            int matchIndex = startIndex;
                                            int matchLength = length;
                                            int matchCount = 0;
                                            Match match = null;

                                            while (true)
                                            {
                                                //
                                                // NOTE: Perform the regular expresssion matching.  This cannot 
                                                //       raise an exception because we know the input argument 
                                                //       is valid.
                                                //
                                                if (matchLength < 0)
                                                    matchLength = 0;

                                                if ((matchIndex + matchLength) > input.Length)
                                                    matchLength = input.Length - matchIndex;

                                                if (debug)
                                                {
                                                    TraceOps.DebugWriteTo(interpreter, String.Format(
                                                        "{0}: Trying index {1}, length {2}", this.Name,
                                                        matchIndex, matchLength), false);
                                                }

                                                if (match != null)
                                                    match = match.NextMatch();
                                                else
                                                    match = regEx.Match(input, matchIndex, matchLength);

                                                //
                                                // NOTE: Did the overall match succeed?
                                                //
                                                if (match.Success)
                                                {
                                                    if (debug)
                                                    {
                                                        TraceOps.DebugWriteTo(interpreter, String.Format(
                                                            "{0}: Match success {1}", this.Name,
                                                            FormatOps.DisplayRegExMatch(match)), false);
                                                    }

                                                    //
                                                    // NOTE: We found another match.
                                                    //
                                                    matchCount++;

                                                    //
                                                    // NOTE: Check if we should return this match.
                                                    //
                                                    if ((limit < 0) || ((limit >= 0) && (matchCount <= limit)))
                                                    {
                                                        //
                                                        // NOTE: Advance the argument index just beyond the 
                                                        //       pattern and input arguments.
                                                        //
                                                        int nextArgumentIndex = argumentIndex + 2;

                                                        //
                                                        // NOTE: Process each match group and either set the
                                                        //       corresponding variable value or add it to the 
                                                        //       inline result.
                                                        //
                                                        foreach (Group group in match.Groups)
                                                        {
                                                            //
                                                            // NOTE: Having a null group should not happen; but,
                                                            //       just in case it does, skip over them.
                                                            //
                                                            if (group == null)
                                                                continue;

                                                            //
                                                            // NOTE: Set the value for this match group based on 
                                                            //       whether we are operating in indexes mode. 
                                                            //
                                                            string matchValue;

                                                            if (group.Success)
                                                            {
                                                                if (indexes)
                                                                {
                                                                    //
                                                                    // NOTE: Return the first and last indexes of 
                                                                    //       this match group.
                                                                    //
                                                                    matchValue = StringList.MakeList(
                                                                        group.Index, group.Index + group.Length - 1);
                                                                }
                                                                else
                                                                {
                                                                    //
                                                                    // NOTE: Return the value of this match group.
                                                                    //
                                                                    matchValue = group.Value;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                if (indexes)
                                                                {
                                                                    //
                                                                    // NOTE: Return invalid indexes for this match 
                                                                    //       group (we did not match this group).
                                                                    //
                                                                    matchValue = StringList.MakeList(
                                                                        Index.Invalid, Index.Invalid);
                                                                }
                                                                else
                                                                {
                                                                    //
                                                                    // NOTE: Return an empty value for this match
                                                                    //       group (we did not match this group).
                                                                    //
                                                                    matchValue = String.Empty;
                                                                }
                                                            }

                                                            //
                                                            // NOTE: Are we using inline mode?
                                                            //
                                                            if (inline)
                                                            {
                                                                //
                                                                // NOTE: Inline mode, add match value to inline 
                                                                //       matches list.
                                                                //
                                                                matches.Add(matchValue);
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: If they supplied a variable name for this match 
                                                                //       group, attempt to set the variable now.
                                                                //
                                                                if (nextArgumentIndex < arguments.Count)
                                                                {
                                                                    //
                                                                    // NOTE: Potentially re-entrant here due to variable 
                                                                    //       traces.
                                                                    //
                                                                    code = interpreter.SetVariableValue(VariableFlags.None,
                                                                        arguments[nextArgumentIndex], matchValue, null, ref result);

                                                                    if (code != ReturnCode.Ok)
                                                                    {
                                                                        result = String.Format("couldn't set variable \"{0}\"",
                                                                            arguments[nextArgumentIndex]);

                                                                        break;
                                                                    }

                                                                    //
                                                                    // NOTE: Advance to the next match variable name, if any.
                                                                    //
                                                                    nextArgumentIndex++;
                                                                }
                                                            }
                                                        }

                                                        //
                                                        // NOTE: If the inner loop failed to set a match variable, break 
                                                        //       out of the outer loop as well.
                                                        //
                                                        if (code != ReturnCode.Ok)
                                                            break;

                                                        //
                                                        // NOTE: If we are not in inline mode, fill in any remaining match variables 
                                                        //       with an empty string or "-1 -1" if we are in indexes mode.
                                                        //
                                                        if (!inline)
                                                        {
                                                            for (; nextArgumentIndex < arguments.Count; nextArgumentIndex++)
                                                            {
                                                                //
                                                                // NOTE: Potentially re-entrant here due to variable 
                                                                //       traces.
                                                                //
                                                                string matchValue;

                                                                if (indexes)
                                                                {
                                                                    //
                                                                    // NOTE: Return invalid indexes for this match 
                                                                    //       group (we did not match this group).
                                                                    //
                                                                    matchValue = StringList.MakeList(
                                                                        Index.Invalid, Index.Invalid);
                                                                }
                                                                else
                                                                {
                                                                    //
                                                                    // NOTE: Return an empty value for this match
                                                                    //       group (we did not match this group).
                                                                    //
                                                                    matchValue = String.Empty;
                                                                }

                                                                code = interpreter.SetVariableValue(VariableFlags.None,
                                                                    arguments[nextArgumentIndex], matchValue, null, ref result);

                                                                if (code != ReturnCode.Ok)
                                                                {
                                                                    result = String.Format("couldn't set variable \"{0}\"",
                                                                        arguments[nextArgumentIndex]);

                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    if (debug)
                                                    {
                                                        TraceOps.DebugWriteTo(interpreter, String.Format(
                                                            "{0}: Match failure", this.Name), false);
                                                    }

                                                    //
                                                    // NOTE: We are done matching.
                                                    //
                                                    break;
                                                }

                                                //
                                                // NOTE: Only keep going if we want all matches.
                                                //
                                                if (!all)
                                                    break;

                                                //
                                                // NOTE: Adjust the match index and length to remove what we have 
                                                //       already matched.
                                                //
                                                Group groupZero = RegExOps.GetMatchGroup(match, 0);

                                                if (groupZero == null)
                                                {
                                                    result = String.Format(
                                                        "cannot advance beyond {0} / {1}: group zero missing",
                                                        matchIndex, matchLength);

                                                    code = ReturnCode.Error;
                                                    break;
                                                }

                                                if ((groupZero.Index + groupZero.Length) == matchIndex)
                                                    matchIndex++;
                                                else
                                                    matchIndex = (groupZero.Index + groupZero.Length);

                                                //
                                                // NOTE: Did we run out of input string to match against?
                                                //
                                                if (matchIndex >= length)
                                                    break;
                                            }

                                            //
                                            // NOTE: If we did not encounter an error above (setting a match variable), 
                                            //       we will now set the overall command result based on whether or not
                                            //       we are using inline mode.
                                            //
                                            if (code == ReturnCode.Ok)
                                            {
                                                if (inline)
                                                    result = matches;
                                                else
                                                    result = (all ? matchCount : ConversionOps.ToInt(matchCount != 0));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        result = "regexp match variables not allowed when using -inline";

                                        code = ReturnCode.Error;
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
                                    result = "wrong # args: should be \"regexp ?switches? exp string ?matchVar? ?subMatchVar subMatchVar ...?\"";
                                }

                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"regexp ?switches? exp string ?matchVar? ?subMatchVar subMatchVar ...?\"";
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
