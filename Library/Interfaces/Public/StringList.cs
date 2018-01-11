/*
 * StringList.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("ce1b08f7-14d5-49ac-a89c-2aec29d80226")]
    public interface IStringList : ICloneable
    {
#if LIST_CACHE
        string CacheKey { get; set; }
#endif

        int Count { get; }

        string GetItem(int index);
        IPair<string> GetPair(int index);

        void Add(string item);
        void Add(string key, string value);
        void Add(StringBuilder item);

        void Add(IList list, int startIndex);
        void Add(IStringList list, int startIndex);
        void Add(IEnumerable<string> collection);
        void Add(IEnumerable<StringBuilder> collection);
        void Add(IDictionary<string, string> dictionary);
        void Add(IEnumerable<Argument> collection);
        void Add(IEnumerable<Result> collection);
        void Add(IEnumerable<IPair<string>> collection);

        void Add(StringTransformCallback callback, IEnumerable<string> collection);
        void Add(StringTransformCallback callback, IEnumerable<Argument> collection);
        void Add(StringTransformCallback callback, IEnumerable<Result> collection);

        bool MaybeAddNull();

        string ToString(bool empty);
        string ToString(string pattern, bool noCase);
        string ToString(string pattern, bool empty, bool noCase);
        string ToString(string separator, string pattern, bool noCase);
        string ToString(string separator, string pattern, bool empty, bool noCase);

        string ToRawString();
        string ToRawString(string separator);
        IStringList ToList();
        IStringList ToList(string pattern, bool noCase);
        IStringList ToList(string pattern, bool empty, bool noCase);
    }
}
