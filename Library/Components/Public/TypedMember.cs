/*
 * TypedMember.cs --
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
using Eagle._Attributes;

namespace Eagle._Components.Public
{
    [ObjectId("6939e5d2-3952-4c30-a4f0-9fe618243e24")]
    public sealed class TypedMember
    {
        public TypedMember(
            Type type,
            ObjectFlags flags,
            object @object,
            string memberName,
            string fullMemberName,
            MemberInfo[] memberInfo
            )
        {
            this.type = type;
            this.flags = flags;
            this.@object = @object;
            this.memberName = memberName;
            this.fullMemberName = fullMemberName;
            this.memberInfo = memberInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        private Type type;
        public Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ObjectFlags flags;
        public ObjectFlags Flags
        {
            get { return flags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object @object;
        public object Object
        {
            get { return @object; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string memberName;
        public string MemberName
        {
            get { return memberName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fullMemberName;
        public string FullMemberName
        {
            get { return fullMemberName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private MemberInfo[] memberInfo;
        public MemberInfo[] MemberInfo
        {
            get { return memberInfo; }
        }
    }
}
