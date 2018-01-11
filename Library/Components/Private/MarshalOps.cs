/*
 * MarshalOps.cs --
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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

#if NATIVE
using System.Security;
#endif

#if NATIVE && !NET_40
using System.Security.Permissions;
#endif

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;
using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

namespace Eagle._Components.Private
{
#if NATIVE
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("a6ba3c2e-1324-4e38-8b8b-eb563a71f2cc")]
    internal static class MarshalOps
    {
        private const char ParameterDelimiter = Characters.OpenBracket;
        private static readonly string TypeDelimiterString = Type.Delimiter.ToString();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal const BindingFlags CreateInstanceBindingFlags =
            /* Instance | Public | */ BindingFlags.CreateInstance;

        internal const BindingFlags PrivateCreateInstanceBindingFlags =
            BindingFlags.Instance | BindingFlags.NonPublic |
            BindingFlags.CreateInstance;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal const BindingFlags DefaultBindingFlags =
            BindingFlags.Default;

        internal const BindingFlags EnumFieldBindingFlags =
            BindingFlags.Static | BindingFlags.Public;

        internal const MemberTypes UnsafeObjectMemberTypes =
            MemberTypes.Constructor | MemberTypes.Event |
            MemberTypes.TypeInfo | MemberTypes.Custom |
            MemberTypes.NestedType;

        internal const BindingFlags UnsafeObjectBindingFlags =
            BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        internal const BindingFlags NestedObjectBindingFlags =
            BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod |
            BindingFlags.GetField | BindingFlags.GetProperty;

        internal const BindingFlags LooseMethodBindingFlags =
            BindingFlags.IgnoreCase | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.Public |
            BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod;

        internal const BindingFlags HostInfoBindingFlags =
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod |
            BindingFlags.GetField | BindingFlags.GetProperty;

#if !MONO
        private const string RuntimeAssemblyTypeName = "System.Reflection.RuntimeAssembly";
        private const string EnumerateCacheMethodName = "EnumerateCache";
#endif

#if !NET_40 && !MONO && NET_20_FAST_ENUM
        //
        // NOTE: This is used only by the "EnumOps.GetEnumNamesAndValuesFast" method.
        //
        internal const string GetEnumValuesMethodName = "InternalGetEnumValues";
#endif

        private const string ParameterNamePrefix = "parameter";

        internal const string ImplicitOperatorMethodName = "op_Implicit";
        internal const string ExplicitOperatorMethodName = "op_Explicit";

        private static readonly StringList AccessorPrefixes = new StringList(new string[] {
            "get_", "set_"
        });

        private const int MaximumTypeLevels = 10;

        private static readonly Regex ObjectHandleIdRegEx =
            new Regex(Characters.NumberSign + "\\d+");

        private static readonly char[] TypeAndHandleDelimiters = {
            Type.Delimiter, Characters.NumberSign
        };

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal const BindingFlags PrivateInstanceBindingFlags = BindingFlags.Instance |
            BindingFlags.NonPublic;

        internal const BindingFlags PrivateStaticBindingFlags = BindingFlags.Static |
            BindingFlags.NonPublic;

        internal const BindingFlags PublicStaticMethodBindingFlags = BindingFlags.Static |
            BindingFlags.Public | BindingFlags.InvokeMethod;

        internal const BindingFlags PrivateStaticGetFieldBindingFlags = BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.GetField;

        internal const BindingFlags PrivateStaticSetFieldBindingFlags = BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.SetField;

        internal const BindingFlags PrivateStaticGetPropertyBindingFlags = BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.GetProperty;

#if CONSOLE
        internal const BindingFlags PublicStaticGetPropertyBindingFlags = BindingFlags.Static |
            BindingFlags.Public | BindingFlags.GetProperty;
#endif

        #region Dead Code
#if DEAD_CODE
        internal const BindingFlags PrivateStaticSetPropertyBindingFlags = BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.SetProperty;
#endif
        #endregion

        internal const BindingFlags PrivateStaticMethodBindingFlags = BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.InvokeMethod;

        internal const BindingFlags PrivateInstanceGetFieldBindingFlags = BindingFlags.Instance |
            BindingFlags.NonPublic | BindingFlags.GetField;

#if TEST
        internal const BindingFlags PublicInstanceGetFieldBindingFlags = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.GetField;
#endif

#if WINFORMS
        internal const BindingFlags PrivateInstanceGetPropertyBindingFlags = BindingFlags.Instance |
            BindingFlags.NonPublic | BindingFlags.GetProperty;
#endif

#if TEST
        internal const BindingFlags PublicInstanceGetPropertyBindingFlags = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.GetProperty;
#endif

        internal const BindingFlags PublicInstanceMethodBindingFlags = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.InvokeMethod;

#if (CONSOLE && !MONO) || (NATIVE && WINDOWS)
        internal const BindingFlags PrivateInstanceMethodBindingFlags = BindingFlags.Instance |
            BindingFlags.NonPublic | BindingFlags.InvokeMethod;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /* IMMUTABLE */
        private static readonly IClientData ObjectValueData = ClientData.Empty;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Type ComObjectType = Type.GetType("System.__ComObject");

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Type ByRefObjectType = typeof(object).MakeByRefType();
        private static readonly Type ByRefValueType = typeof(ValueType).MakeByRefType();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Type ByRefStringType = typeof(string).MakeByRefType();
        private static readonly Type ByRefStringPairType = typeof(StringPair).MakeByRefType();
        private static readonly Type ByRefStringListType = typeof(StringList).MakeByRefType();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static Result DefaultObjectName = null;
        private static int StringTypePenalty = -1;
        private static int StringTypeBonus = 1;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static MarshalFlags IsSameTypeMarshalFlags = MarshalFlags.SpecialValueType;
        private static MarshalFlags ConvertValueToStringMarshalFlags = MarshalFlags.None;
        private static MarshalFlags IsAssignableFromMarshalFlags = MarshalFlags.None;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly Type RuntimeType = CommonOps.Runtime.IsMono() ?
            Type.GetType("System.MonoType") : Type.GetType("System.RuntimeType");

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
        //
        // HACK: This is the string required to format a Single (float)
        //       value, including MinValue and MaxValue, in order to be
        //       able to successfully parse it again.  This is purposely
        //       not read-only to allow for customization.  This is only
        //       used when handling data reader values to be returned by
        //       the [sql execute] command -AND- then only if both the
        //       SingleDataFormat is null and NeedSingleFormat() returns
        //       true for the value.
        //
        private static string DefaultSingleDataFormat = "E8";

        //
        // HACK: This is the string required to format a Double (double)
        //       value, including MinValue and MaxValue, in order to be
        //       able to successfully parse it again.  This is purposely
        //       not read-only to allow for customization.  This is only
        //       used when handling data reader values to be returned by
        //       the [sql execute] command -AND- then only if both the
        //       DoubleDataFormat is null and NeedDoubleFormat() returns
        //       true for the value.
        //
        private static string DefaultDoubleDataFormat = "E16";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is the format string to use when formatting a value
        //       of the Single (float) data type.  This is purposely not
        //       read-only to allow for customization.  This is only used
        //       when handling data reader values to be returned from the
        //       [sql execute] command.
        //
        private static string SingleDataFormat = null;

        //
        // HACK: This is the format string to use when formatting a value
        //       of the Double (double) data type.  This is purposely not
        //       read-only to allow for customization.  This is only used
        //       when handling data reader values to be returned from the
        //       [sql execute] command.
        //
        private static string DoubleDataFormat = null;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("90b718a7-90f2-4fba-ba0d-caaf2c25b5ad")]
        internal static class UnsafeNativeMethods
        {
            [ComImport()]
            [Guid("b196b283-bab4-101a-b69c-00aa00341d07")]
            [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
            [ObjectId("031c7925-e195-4c67-b5fd-b60c8bbc757d")]
            public interface IProvideClassInfo
            {
                [return: MarshalAs(UnmanagedType.Interface)]
                ITypeInfo GetClassInfo();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            [ComImport()]
            [Guid("00020400-0000-0000-c000-000000000046")]
            [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
            [ObjectId("2adc2a0c-f981-4800-8a1f-a34731420a9f")]
            public interface IDispatch
            {
                uint GetTypeInfoCount();

                [return: MarshalAs(UnmanagedType.Interface)]
                ITypeInfo GetTypeInfo(
                    [In()]
                    uint typeInfoId,
                    [In(), MarshalAs(UnmanagedType.U4)]
                    int localeId
                    );

                [PreserveSig()]
                int GetIDsOfNames(
                    [In()]
                    ref Guid iid,
                    [In(), MarshalAs(UnmanagedType.LPArray)]
                    string[] names,
                    [In()]
                    uint count,
                    [In(), MarshalAs(UnmanagedType.U4)]
                    int localeId,
                    [Out(), MarshalAs(UnmanagedType.LPArray)]
                    int[] dispatchIds
                    );

                [PreserveSig()]
                int Invoke(
                    [In()]
                    int dispatchId,
                    [In()]
                    ref Guid iid,
                    [In(), MarshalAs(UnmanagedType.U4)]
                    int localeId,
                    [In()]
                    uint flags,
                    [In(), Out()]
                    ref DISPPARAMS dispParams,
                    [Out()]
                    out object result,
                    [In(), Out()]
                    ref EXCEPINFO excepInfo,
                    [Out()]
                    out uint argumentError
                    );
            }
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer Helper Classes
        [ObjectId("d519117f-c9d6-4cdd-9d0a-f7523f5c7f3d")]
        private sealed class ParameterDataTriplet :
                AnyTriplet<IPair<int>, IPair<int>, IntList>
        {
            #region Public Constructors
            public ParameterDataTriplet(
                IPair<int> x,
                IPair<int> y,
                IntList z
                )
                : base(x, y, z)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                IPair<int> x = this.X;
                IPair<int> y = this.Y;
                IntList z = this.Z;

                return String.Format(
                    "x = {0}, y = {1}, z = {2}",
                    (x != null) ? x.ToString() : FormatOps.DisplayNull,
                    (y != null) ? y.ToString() : FormatOps.DisplayNull,
                    (z != null) ? z.ToString() : FormatOps.DisplayNull);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Static Methods
            public static string ToString(
                ParameterDataTriplet triplet
                )
            {
                if (triplet == null)
                    return FormatOps.DisplayNull;

                return triplet.ToString();
            }
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [ObjectId("2be7ddf5-2fe2-4e77-acb9-d039cad9c2a1")]
        private sealed class ParameterDataComparer :
                IComparer<ParameterDataTriplet>
        {
            #region Private Data
            private ReorderFlags reorderFlags;
            private bool useParameterCounts;
            private bool useTypeDepths;
            private bool typeDepthsFirst;
            private IComparer<int> intComparer;
            private IComparer<IPair<int>> countComparer;
            private IComparer<IntList> depthComparer;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ParameterDataComparer(
                ReorderFlags reorderFlags
                )
            {
                this.reorderFlags = reorderFlags;

                SetupFlags();
                SetupComparers();
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private void SetupFlags()
            {
                useParameterCounts = FlagOps.HasFlags(
                    reorderFlags, ReorderFlags.ParameterCountMask, false);

                useTypeDepths = FlagOps.HasFlags(
                    reorderFlags, ReorderFlags.ParameterTypeDepthMask, false);

                typeDepthsFirst = FlagOps.HasFlags(
                    reorderFlags, ReorderFlags.TypeDepthsFirst, true);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private void SetupComparers()
            {
                intComparer = Comparer<int>.Default;

                countComparer = useParameterCounts ?
                    CreateParameterCountComparer() : null;

                depthComparer = useTypeDepths ?
                    CreateParameterTypeDepthComparer() : null;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private IComparer<IPair<int>> CreateParameterCountComparer()
            {
                bool ascending = false;
                bool maximum = false;

                if (FlagOps.HasFlags(
                        reorderFlags, ReorderFlags.FewestParametersFirst, true))
                {
                    ascending = true;
                    maximum = false;
                }

                if (FlagOps.HasFlags(
                        reorderFlags, ReorderFlags.MostParametersFirst, true))
                {
                    ascending = false;
                    maximum = true;
                }

                return new ParameterCountComparer(
                    intComparer, ascending, maximum);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private IComparer<IntList> CreateParameterTypeDepthComparer()
            {
                bool ascending = false;
                bool maximum = false;
                bool total = false;

                if (FlagOps.HasFlags(
                        reorderFlags, ReorderFlags.ShallowestTypesFirst, true))
                {
                    ascending = true;
                    maximum = false;
                }

                if (FlagOps.HasFlags(
                        reorderFlags, ReorderFlags.DeepestTypesFirst, true))
                {
                    ascending = false;
                    maximum = true;
                }

                if (FlagOps.HasFlags(
                        reorderFlags, ReorderFlags.TotalTypeDepths, true))
                {
                    total = true;
                }

                return new ParameterTypeDepthComparer(
                    intComparer, ascending, maximum, total);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IComparer<ParameterDataTriplet> Members
            public int Compare(
                ParameterDataTriplet left,
                ParameterDataTriplet right
                )
            {
                if ((left == null) && (right == null))
                {
                    return 0;
                }
                else if (left == null)
                {
                    return -1;
                }
                else if (right == null)
                {
                    return 1;
                }
                else
                {
                    if (useParameterCounts && useTypeDepths)
                    {
                        int result;

                        if (typeDepthsFirst)
                        {
                            result = depthComparer.Compare(left.Z, right.Z);

                            if (result != 0)
                                return result;

                            result = countComparer.Compare(left.Y, right.Y);
                        }
                        else
                        {
                            result = countComparer.Compare(left.Y, right.Y);

                            if (result != 0)
                                return result;

                            result = depthComparer.Compare(left.Z, right.Z);
                        }

                        return result;
                    }
                    else if (useParameterCounts)
                    {
                        return countComparer.Compare(left.Y, right.Y);
                    }
                    else if (useTypeDepths)
                    {
                        return depthComparer.Compare(left.Z, right.Z);
                    }
                    else /* NOTE: This else should not be reached. */
                    {
                        //
                        // HACK: *FALLBACK* Just compare the relative method
                        //       index values.  This should never be reached.
                        //
                        IPair<int> leftPair = left.X;
                        IPair<int> rightPair = right.X;

                        if ((leftPair == null) && (rightPair == null))
                        {
                            return 0;
                        }
                        else if (leftPair == null)
                        {
                            return -1;
                        }
                        else if (rightPair == null)
                        {
                            return 1;
                        }
                        else
                        {
                            int result = intComparer.Compare(
                                leftPair.X, rightPair.X);

                            if (result != 0)
                                return result;

                            return intComparer.Compare(
                                leftPair.Y, rightPair.Y);
                        }
                    }
                }
            }
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Parameter Counts
        [ObjectId("a63ad040-72ca-414e-993d-c1febc4109b7")]
        private sealed class ParameterCountComparer : IComparer<IPair<int>>
        {
            #region Private Data
            private IComparer<int> comparer;
            private bool ascending;
            private bool maximum;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ParameterCountComparer(
                IComparer<int> comparer,
                bool ascending,
                bool maximum
                )
            {
                this.comparer = comparer;
                this.ascending = ascending;
                this.maximum = maximum;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Static Methods
            private static int GetValueForCompare(
                IPair<int> value,
                bool maximum
                )
            {
                int result;

                if (maximum)
                {
                    result = value.Y;

                    if (result == Count.Invalid)
                        result = int.MaxValue;
                }
                else
                {
                    result = value.X;

                    if (result == Count.Invalid)
                        result = int.MinValue;
                }

                return result;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private int GetPrimaryValueForCompare(
                IPair<int> value
                )
            {
                return GetValueForCompare(value, maximum);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int GetSecondaryValueForCompare(
                IPair<int> value
                )
            {
                return GetValueForCompare(value, !maximum);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int Compare(
                int left,
                int right
                )
            {
                if ((left == Count.Invalid) && (right == Count.Invalid))
                {
                    return 0;
                }
                else if (left == Count.Invalid)
                {
                    return maximum ? -1 : 1;
                }
                else if (right == Count.Invalid)
                {
                    return maximum ? 1 : -1;
                }
                else
                {
                    return comparer.Compare(left, right);
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int AdjustResultForCompare(
                int result
                )
            {
                return ascending ? result : -result;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IComparer<IPair<int>> Members
            public int Compare(
                IPair<int> left,
                IPair<int> right
                )
            {
                if ((left == null) && (right == null))
                {
                    return 0;
                }
                else if (left == null)
                {
                    return ascending ? -1 : 1;
                }
                else if (right == null)
                {
                    return ascending ? 1 : -1;
                }
                else
                {
                    //
                    // NOTE: At this point, we need to compare the minimum -OR-
                    //       maximum parameter counts.  However, there are
                    //       several complications:
                    //
                    //       1. Either of the parameter counts may be -1, which
                    //          means it can accept any number of arguments.
                    //
                    //       2. We must take into account whether the results
                    //          need to be in ascending order or descending.
                    //
                    //       3. If there is a tie, we need to take into account
                    //          the other parameter count.
                    //
                    int result = AdjustResultForCompare(Compare(
                        GetPrimaryValueForCompare(left),
                        GetPrimaryValueForCompare(right)));

                    if (result != 0)
                        return result;

                    return AdjustResultForCompare(Compare(
                        GetSecondaryValueForCompare(left),
                        GetSecondaryValueForCompare(right)));
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Parameter Type Depths
        [ObjectId("c932e6b7-cdce-4757-829a-331c158bef43")]
        private sealed class ParameterTypeDepthComparer : IComparer<IntList>
        {
            #region Private Data
            IComparer<int> comparer;
            private bool ascending;
            private bool maximum;
            private bool total;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Public Constructors
            public ParameterTypeDepthComparer(
                IComparer<int> comparer,
                bool ascending,
                bool maximum,
                bool total
                )
            {
                this.comparer = comparer;
                this.ascending = ascending;
                this.maximum = maximum;
                this.total = total;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Private Methods
            private int PrivateCompare(
                IntList left,
                IntList right
                )
            {
                if ((left == null) && (right == null))
                {
                    return 0;
                }
                else if (left == null)
                {
                    return maximum ? -1 : 1;
                }
                else if (right == null)
                {
                    return maximum ? 1 : -1;
                }
                else
                {
                    int result = left.Count - right.Count;

                    if (result == 0)
                    {
                        for (int index = 0; index < left.Count; index++)
                        {
                            int compareResult = comparer.Compare(
                                left[index], right[index]);

                            if (total)
                                result += compareResult;
                            else if (compareResult != 0)
                                return compareResult;
                        }
                    }

                    return result;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

            private int AdjustResultForCompare(
                int result
                )
            {
                return ascending ? result : -result;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region IComparer<IntList> Members
            public int Compare(
                IntList left,
                IntList right
                )
            {
                if ((left == null) && (right == null))
                {
                    return 0;
                }
                else if (left == null)
                {
                    return ascending ? -1 : 1;
                }
                else if (right == null)
                {
                    return ascending ? 1 : -1;
                }
                else
                {
                    return AdjustResultForCompare(PrivateCompare(left, right));
                }
            }
            #endregion
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool IsObjectHandle(
            Result result
            )
        {
            return (result != null) &&
                Object.ReferenceEquals(result.ValueData, ObjectValueData);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static MarshalFlagsList GetParameterMarshalFlags(
            EnumList list
            )
        {
            if (list == null)
                return null;

            MarshalFlagsList result = new MarshalFlagsList(
                list.Count);

            foreach (Enum element in list)
                if (element is MarshalFlags)
                    result.Add((MarshalFlags)element);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSystemComObjectType(
            Type type
            )
        {
            return ((type != null) && Object.ReferenceEquals(type, ComObjectType));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsNamespaceQualifiedTypeName(
            string typeName
            )
        {
            //
            // NOTE: If the type name is null or does not contain any periods,
            //       it must be a simple type name.
            //
            if ((typeName == null) ||
                typeName.IndexOf(Type.Delimiter) == Index.Invalid)
            {
                return false;
            }

            //
            // BUGFIX: This method is only used within the context of searching
            //         for a fully qualified type name and returning false from
            //         this method simply allows the search to include imported
            //         namespaces; therefore, we err on the side of caution and
            //         return true only if the type name could not possibly be
            //         a simple generic type name that happens to contain some
            //         generic type parameters that may be fully qualified type
            //         names.
            //
            int index = typeName.IndexOf(ParameterDelimiter);

            if (index != Index.Invalid)
            {
                return typeName.Substring(
                    0, index).IndexOf(Type.Delimiter) != Index.Invalid;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsAssemblyQualifiedTypeName(
            string typeName
            )
        {
            //
            // BUGFIX: Must take into account generic types with more than one
            //         generic type parameter (i.e. that will contain commas).
            //
            if (!String.IsNullOrEmpty(typeName))
            {
                //
                // NOTE: Simply try to find a comma that is NOT within square
                //       brackets.  If we find one, return true.
                //
                int count = 0;

                for (int index = 0; index < typeName.Length; index++)
                {
                    char character = typeName[index];

                    switch (character)
                    {
                        case Characters.Comma:
                            {
                                if (count == 0)
                                    return true;
                                else
                                    break;
                            }
                        case Characters.OpenBracket:
                            {
                                count++;
                                break;
                            }
                        case Characters.CloseBracket:
                            {
                                count--;
                                break;
                            }
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsOneDimensionalArrayType(
            Type type
            )
        {
            Type elementType = null;

            return IsOneDimensionalArrayType(type, ref elementType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsOneDimensionalArrayType(
            Type type,
            ref Type elementType
            )
        {
            int rank = 0;

            if (IsArrayType(type, ref rank, ref elementType) &&
                (rank == 1) && (elementType != null))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetVariableValueAsString(
            Interpreter interpreter,
            object value,
            bool ignoreAlias,
            ref Result result
            )
        {
            ObjectFlags objectFlags = ObjectOps.GetDefaultObjectFlags();

            if (ignoreAlias)
                objectFlags |= ObjectFlags.IgnoreAlias;

            return FixupReturnValue(
                interpreter, null, objectFlags, null,
                ObjectOps.GetDefaultObjectOptionType(), null,
                value, false, false, false, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetArrayIndexes(
            CultureInfo cultureInfo,
            string index,
            ref int[] indexes
            )
        {
            Result error = null;

            return GetArrayIndexes(
                cultureInfo, index, ref indexes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetArrayIndexes(
            CultureInfo cultureInfo,
            string index,
            ref int[] indexes,
            ref Result error
            )
        {
            if (index == null)
            {
                error = "invalid index";
                return ReturnCode.Error;
            }

            string[] subIndexes = index.Split(Characters.Comma);

            if (subIndexes == null)
            {
                error = String.Format(
                    "could not parse array index {0}",
                    FormatOps.WrapOrNull(index));

                return ReturnCode.Error;
            }

            int rank = subIndexes.Length;

            if (rank <= 0)
            {
                error = String.Format(
                    "invalid array rank {0}",
                    rank);

                return ReturnCode.Error;
            }

            int[] localIndexes = new int[rank];

            for (int rankIndex = 0; rankIndex < rank; rankIndex++)
            {
                if (Value.GetInteger2(
                        subIndexes[rankIndex], ValueFlags.AnyInteger,
                        cultureInfo, ref localIndexes[rankIndex],
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            indexes = localIndexes;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetArrayElementValue(
            IBinder binder,
            CultureInfo cultureInfo,
            Array array,
            int[] indexes,
            ref object value
            )
        {
            Result error = null;

            return GetArrayElementValue(
                binder, cultureInfo, array, indexes, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetArrayElementValue(
            IBinder binder,
            CultureInfo cultureInfo,
            Array array,
            int[] indexes,
            ref object value,
            ref Result error
            )
        {
            if (array == null)
            {
                error = "invalid managed array";
                return ReturnCode.Error;
            }

            try
            {
                value = array.GetValue(indexes);
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SetArrayElementValue(
            IBinder binder,
            CultureInfo cultureInfo,
            Array array,
            int[] indexes,
            object value,
            ref Result error
            )
        {
            if (array == null)
            {
                error = "invalid managed array";
                return ReturnCode.Error;
            }

            Type type = array.GetType();

            if (type == null)
            {
                error = "managed array has an invalid type";
                return ReturnCode.Error;
            }

            Type elementType = null;

            if (!IsArrayType(type, ref elementType))
            {
                error = String.Format(
                    "unknown element type for managed array type {0}",
                    FormatOps.TypeName(type));

                return ReturnCode.Error;
            }

            if (binder == null)
            {
                error = "invalid binder";
                return ReturnCode.Error;
            }

            Type valueType = GetValueTypeOrObjectType(value);

            object localValue = null;
            bool changeType = true;

            if (IsSameType(valueType, elementType))
            {
                localValue = value;
                changeType = false;
            }
            else if (IsSimpleTypeForToString(binder, valueType))
            {
                Result result = null;

                if (ConvertValueToString(binder as ScriptBinder,
                        cultureInfo, valueType, null, value,
                        false, ref result) == ReturnCode.Ok)
                {
                    localValue = StringOps.GetStringFromObject(result);
                }
                else
                {
                    return ReturnCode.Error;
                }
            }
            else
            {
                localValue = StringOps.GetStringFromObject(value);
            }

            try
            {
                array.SetValue(changeType ?
                    binder.ChangeType(
                        localValue, elementType, cultureInfo) :
                    localValue, indexes);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetArrayElementKeys(
            Interpreter interpreter,
            Array array,
            MatchMode mode,
            string pattern,
            bool noCase,
            ref StringList keys,
            ref Result error
            )
        {
            Result result = null;

            if (GetArrayElementKeysAndOrValues(
                    interpreter, array, true, false, mode, pattern,
                    null, noCase, ref result) == ReturnCode.Ok)
            {
                if (result != null)
                {
                    StringList list = result.Value as StringList;

                    if (list != null)
                    {
                        keys = list;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "no {0} within result for {1} conversion",
                            FormatOps.TypeName(typeof(StringList)),
                            FormatOps.TypeName(typeof(Array)));
                    }
                }
                else
                {
                    error = String.Format(
                        "no result for {0} to {1} conversion",
                        FormatOps.TypeName(typeof(Array)),
                        FormatOps.TypeName(typeof(StringList)));
                }
            }
            else
            {
                error = result;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetArrayElementValues(
            Interpreter interpreter,
            Array array,
            MatchMode mode,
            string pattern,
            bool noCase,
            ref StringList values,
            ref Result error
            )
        {
            Result result = null;

            if (GetArrayElementKeysAndOrValues(
                    interpreter, array, false, true, mode, null,
                    pattern, noCase, ref result) == ReturnCode.Ok)
            {
                if (result != null)
                {
                    StringList list = result.Value as StringList;

                    if (list != null)
                    {
                        values = list;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "no {0} within result for {1} conversion",
                            FormatOps.TypeName(typeof(StringList)),
                            FormatOps.TypeName(typeof(Array)));
                    }
                }
                else
                {
                    error = String.Format(
                        "no result for {0} to {1} conversion",
                        FormatOps.TypeName(typeof(Array)),
                        FormatOps.TypeName(typeof(StringList)));
                }
            }
            else
            {
                error = result;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetArrayElementKeysAndValues(
            Interpreter interpreter,
            Array array,
            MatchMode mode,
            string keyPattern,
            string valuePattern,
            bool noCase,
            ref StringDictionary keysAndValues,
            ref Result error
            )
        {
            Result result = null;

            if (GetArrayElementKeysAndOrValues(
                    interpreter, array, true, true, mode, keyPattern,
                    valuePattern, noCase, ref result) == ReturnCode.Ok)
            {
                if (result != null)
                {
                    StringDictionary dictionary =
                        result.Value as StringDictionary;

                    if (dictionary != null)
                    {
                        keysAndValues = dictionary;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "no {0} within result for {1} conversion",
                            FormatOps.TypeName(typeof(StringDictionary)),
                            FormatOps.TypeName(typeof(Array)));
                    }
                }
                else
                {
                    error = String.Format(
                        "no result for {0} to {1} conversion",
                        FormatOps.TypeName(typeof(Array)),
                        FormatOps.TypeName(typeof(StringDictionary)));
                }
            }
            else
            {
                error = result;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetArrayElementKeysAndValues(
            Interpreter interpreter,
            Array array,
            MatchMode mode,
            string pattern,
            bool noCase,
            bool matchKey,
            bool matchValue,
            ref StringDictionary keysAndValues,
            ref Result error
            )
        {
            Result result = null;

            if (GetArrayElementKeysAndOrValues(
                    interpreter, array, true, true, mode, pattern, noCase,
                    matchKey, matchValue, ref result) == ReturnCode.Ok)
            {
                if (result != null)
                {
                    StringDictionary dictionary =
                        result.Value as StringDictionary;

                    if (dictionary != null)
                    {
                        keysAndValues = dictionary;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "no {0} within result for {1} conversion",
                            FormatOps.TypeName(typeof(StringDictionary)),
                            FormatOps.TypeName(typeof(Array)));
                    }
                }
                else
                {
                    error = String.Format(
                        "no result for {0} to {1} conversion",
                        FormatOps.TypeName(typeof(Array)),
                        FormatOps.TypeName(typeof(StringDictionary)));
                }
            }
            else
            {
                error = result;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetArrayElementKeysAndOrValues(
            Interpreter interpreter,
            Array array,
            bool keys,
            bool values,
            MatchMode mode,
            string keyPattern,
            string valuePattern,
            bool noCase,
            ref Result result
            )
        {
            if (array == null)
            {
                result = String.Format(
                    "invalid {0} variable",
                    FormatOps.TypeName(typeof(Array)));

                return ReturnCode.Error;
            }

            if (keys || values)
            {
                StringDictionary dictionary = (keys && values) ?
                    new StringDictionary() : null;

                StringList list = (dictionary == null) ?
                    new StringList() : null;

                //
                // NOTE: Obtain all the bounds for the array.
                //
                int rank = 0;
                int[] lowerBounds = null;
                int[] lengths = null;
                int[] indexes = null;

                if (!ArrayOps.GetBounds(
                        array, ref rank, ref lowerBounds, ref lengths,
                        ref indexes))
                {
                    result = "unable to obtain bounds for array";
                    return ReturnCode.Error;
                }

                //
                // NOTE: Iterate over the entire array, capturing the names
                //       (i.e. index strings) and/or values as we go.
                //
                int length = array.Length;

                for (int unused = 0; unused < length; unused++)
                {
                    string index = GenericOps<int>.ListToString(
                        indexes, 0, Index.Invalid, ToStringFlags.None,
                        Characters.Comma.ToString(), null, false);

                    string value = StringOps.GetStringFromObject(
                        array.GetValue(indexes));

                    if (((keyPattern == null) ||
                        StringOps.Match(null, mode, index, keyPattern, noCase)) &&
                        ((valuePattern == null) ||
                        StringOps.Match(null, mode, value, valuePattern, noCase)))
                    {
                        if (dictionary != null)
                        {
                            dictionary.Add(index, value);
                        }
                        else if (list != null)
                        {
                            if (keys)
                                list.Add(index);

                            if (values)
                                list.Add(value);
                        }
                    }

                    ArrayOps.IncrementIndexes(
                        rank, lowerBounds, lengths, indexes);
                }

                if (dictionary != null)
                    result = dictionary;
                else
                    result = list;
            }
            else
            {
                result = String.Empty;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetArrayElementKeysAndOrValues(
            Interpreter interpreter,
            Array array,
            bool keys,
            bool values,
            MatchMode mode,
            string pattern,
            bool noCase,
            bool matchKey,
            bool matchValue,
            ref Result result
            )
        {
            if (array == null)
            {
                result = String.Format(
                    "invalid {0} variable",
                    FormatOps.TypeName(typeof(Array)));

                return ReturnCode.Error;
            }

            if (keys || values)
            {
                StringDictionary dictionary = (keys && values) ?
                    new StringDictionary() : null;

                StringList list = (dictionary == null) ?
                    new StringList() : null;

                //
                // NOTE: Obtain all the bounds for the array.
                //
                int rank = 0;
                int[] lowerBounds = null;
                int[] lengths = null;
                int[] indexes = null;

                if (!ArrayOps.GetBounds(
                        array, ref rank, ref lowerBounds, ref lengths,
                        ref indexes))
                {
                    result = "unable to obtain bounds for array";
                    return ReturnCode.Error;
                }

                //
                // NOTE: Iterate over the entire array, capturing the names
                //       (i.e. index strings) and/or values as we go.
                //
                int length = array.Length;

                for (int unused = 0; unused < length; unused++)
                {
                    string index = GenericOps<int>.ListToString(
                        indexes, 0, Index.Invalid, ToStringFlags.None,
                        Characters.Comma.ToString(), null, false);

                    string value = StringOps.GetStringFromObject(
                        array.GetValue(indexes));

                    string text;

                    if (matchKey)
                    {
                        if (matchValue)
                            text = StringList.MakeList(index, value);
                        else
                            text = index;
                    }
                    else if (matchValue)
                    {
                        text = value;
                    }
                    else
                    {
                        //
                        // NOTE: This will never match a valid pattern.
                        //
                        text = null;
                    }

                    if ((pattern == null) ||
                        StringOps.Match(null, mode, text, pattern, noCase))
                    {
                        if (dictionary != null)
                        {
                            dictionary.Add(index, value);
                        }
                        else if (list != null)
                        {
                            if (keys)
                                list.Add(index);

                            if (values)
                                list.Add(value);
                        }
                    }

                    ArrayOps.IncrementIndexes(
                        rank, lowerBounds, lengths, indexes);
                }

                if (dictionary != null)
                    result = dictionary;
                else
                    result = list;
            }
            else
            {
                result = String.Empty;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool DoesArrayElementExist(
            IBinder binder,
            CultureInfo cultureInfo,
            IVariable variable,
            string index
            )
        {
            Array array = EntityOps.GetSystemArray(variable);

            if (array == null)
                return false;

            int[] indexes = null;

            if (GetArrayIndexes(
                    cultureInfo, index, ref indexes) != ReturnCode.Ok)
            {
                return false;
            }

            object value = null; /* NOT USED */

            if (GetArrayElementValue(
                    binder, cultureInfo, array, indexes,
                    ref value) != ReturnCode.Ok)
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HasElementType(
            Type type,
            bool isArray,
            ref Type elementType
            )
        {
            if ((type != null) && type.HasElementType)
            {
                elementType = type.GetElementType();

                return (type.IsArray == isArray);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsObjectType(
            Type type,
            bool output
            )
        {
            Type objectType = typeof(object);

            if (type == objectType)
                return true;

            if (!output)
                return false;

            if (type == ByRefObjectType)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsStringType(
            Type type,
            bool output
            )
        {
            Type stringType = typeof(string);

            if (type == stringType)
                return true;

            if (!output)
                return false;

            if (type == ByRefStringType)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsArrayValue(
            object value,
            ref Array array
            )
        {
            if (value == null)
                return false;

            array = value as Array;
            return (array != null);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsArrayType(
            IVariable variable,  /* in */
            ref int rank,        /* in, out */
            ref Type elementType /* in, out */
            )
        {
            if (variable == null)
                return false;

            ElementDictionary arrayValue = null;

            if (!EntityOps.IsArray(variable, ref arrayValue))
                return false;

            //
            // NOTE: An empty array has a rank of one, not zero.
            //
            int maximumRank = 1;

            //
            // NOTE: This loop is O(N) in the pathological case where
            //       all but the last array index are an empty string.
            //
            foreach (KeyValuePair<string, object> pair in arrayValue)
            {
                string index = pair.Key;

                if (String.IsNullOrEmpty(index))
                    continue; /* NOTE: Skip invalid array index. */

                string[] subIndexes = index.Split(Characters.Comma);

                if (subIndexes == null)
                    return false;

                int subIndexesLength = subIndexes.Length;

                if (subIndexesLength > maximumRank)
                    maximumRank = subIndexesLength;

                break;
            }

            if ((rank == 0) || (maximumRank > rank))
                rank = maximumRank;

            if (elementType == null)
                elementType = typeof(Variant);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsArrayType(
            Type type
            )
        {
            Type elementType = null;

            return IsArrayType(type, ref elementType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsArrayType(
            Type type,
            ref Type elementType
            )
        {
            int rank = 0;

            return IsArrayType(type, ref rank, ref elementType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsArrayType(
            Type type,
            ref int rank,
            ref Type elementType
            )
        {
            if (type != null)
            {
                if (type.IsArray)
                {
                    rank = type.GetArrayRank();
                    elementType = type.GetElementType();

                    return true;
                }
                else if (type.IsByRef)
                {
                    Type byRefElementType = type.GetElementType();

                    if ((byRefElementType != null) && byRefElementType.IsArray)
                    {
                        rank = byRefElementType.GetArrayRank();
                        elementType = byRefElementType.GetElementType();

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string JoinTypeName(
            string[] parts,
            int lastIndex
            )
        {
            return JoinTypeName(parts, 0, lastIndex);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string JoinTypeName(
            string[] parts,
            int startIndex,
            int lastIndex
            )
        {
            if (parts == null)
                return null;

            int length = parts.Length;

            if ((startIndex < 0) || (startIndex >= length))
                return null;

            if ((lastIndex < 0) || (lastIndex >= length))
                return null;

            if (startIndex > lastIndex)
                return null;

            return String.Join(
                TypeDelimiterString, parts, startIndex,
                lastIndex + 1);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string[] SplitTypeName(
            string typeName
            )
        {
            if (String.IsNullOrEmpty(typeName))
                return null;

            string[] typeParts = typeName.Split(Characters.GraveAccent);

            if ((typeParts == null) || (typeParts.Length == 0))
                return new string[] { typeName };

            string[] baseTypeParts = typeParts[0].Split(Type.Delimiter);

            if ((baseTypeParts == null) || (baseTypeParts.Length == 0))
                return new string[] { typeName };

            return baseTypeParts;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static VariableFlags GetArrayVariableFlags(
            bool input
            )
        {
            return VariableFlags.NoElement | VariableFlags.NoLinkIndex |
                (input ? VariableFlags.Defined : VariableFlags.None) |
                VariableFlags.NonVirtual;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Type GetArrayElementType(
            Type elementType
            )
        {
            return (elementType == typeof(Variant)) ?
                typeof(object) : elementType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MaybeUseScalarForHandle(
            MarshalFlags marshalFlags,
            bool input,
            bool output
            )
        {
            if (FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.HandleByValue, true))
            {
                return true;
            }

            if (input && FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.ByValHandleByValue, true))
            {
                return true;
            }

            if (output && FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.ByRefHandleByValue, true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetObject(
            Interpreter interpreter,
            string text,
            MarshalFlags marshalFlags,
            bool input,
            bool output,
            ref object value
            )
        {
            if (MaybeUseScalarForHandle(marshalFlags, input, output))
            {
                Result result = null;

                if (interpreter.GetVariableValue(VariableFlags.None,
                        text, ref result) == ReturnCode.Ok)
                {
                    if (FlagOps.HasFlags(marshalFlags,
                            MarshalFlags.ForceHandleByValue, true))
                    {
                        text = result;
                    }
                    else
                    {
                        string newText = result;

                        if (Value.GetObject(
                                interpreter, newText,
                                ref value) == ReturnCode.Ok)
                        {
                            return ReturnCode.Ok;
                        }
                    }
                }
            }

            return Value.GetObject(interpreter, text, ref value);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsScalarWithNullOrEmptyValue(
            Interpreter interpreter,
            IVariable variable
            )
        {
            //
            // NOTE: Neither a null variable nor an array value qualify for
            //       this checking.
            //
            if ((variable == null) || EntityOps.IsArray2(variable))
                return false;

            object value = variable.Value; /* SCALAR */

            if (value == null)
                return true; /* NOTE: Physical null value. */

            if (!(value is string))
                return false;

            string stringValue = (string)value;

            if (stringValue.Length == 0)
                return true; /* NOTE: Physical empty value. */

            //
            // NOTE: Next, see if the variable actually represents an opaque
            //       object handle, where the object value itself is null or
            //       an empty string.
            //
            IObject @object = null;

            if ((interpreter != null) && (interpreter.GetObject(
                    stringValue, LookupFlags.MarshalNoVerbose,
                    ref @object) == ReturnCode.Ok))
            {
                //
                // NOTE: Technically, the value of "@object" should never
                //       be null at this point, given the lookup flags.
                //
                if (@object == null)
                    return true;

                if (@object.Value == null)
                    return true;

                if (@object.Value is string)
                {
                    /* REUSED */
                    stringValue = (string)@object.Value;

                    if (stringValue.Length == 0)
                        return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool DoesImplementAnyInterface(
            Type type,         /* in */
            Type interfaceType /* in */
            )
        {
            if ((type == null) || (interfaceType == null))
                return false;

            if (!interfaceType.IsInterface)
                return false;

            Type localType = type;

            while (localType != null)
            {
                Type[] interfaceTypes = localType.GetInterfaces();

                if (interfaceTypes == null)
                    continue;

                for (int index = 0; index < interfaceTypes.Length; index++)
                {
                    Type @interface = interfaceTypes[index];

                    if (@interface == null)
                        continue;

                    if (@interface == interfaceType)
                        return true;

                    if (DoesImplementAnyInterface(
                            @interface, interfaceType)) /* RECURSIVE */
                    {
                        return true;
                    }
                }

                localType = localType.BaseType;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsAssignableFromGenericParameterConstraint(
            Type type1,               /* in */
            Type type2,               /* in */
            MarshalFlags marshalFlags /* in */
            )
        {
            if ((type1 == null) || (type2 == null))
                return false;

            Type[] constraintTypes = type1.GetGenericParameterConstraints();

            for (int index = 0; index < constraintTypes.Length; index++)
            {
                Type constraintType = constraintTypes[index];

                if ((constraintType == null) || !IsAssignableFrom(
                        constraintType, type2, marshalFlags)) /* RECURSIVE */
                {
                    return false;
                }
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the Eagle._Commands._Hash.IsHashAlgorithm method
        //       only.
        //
        public static bool IsAssignableFrom(
            Type type1, /* in */
            Type type2  /* in */
            )
        {
            return IsAssignableFrom(type1, type2, IsAssignableFromMarshalFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsAssignableFrom(
            Type type1,               /* in */
            Type type2,               /* in */
            MarshalFlags marshalFlags /* in */
            )
        {
            //
            // TODO: Maybe this flag should be enabled by default for Mono,
            //       perhaps in the caller?
            //
            if (FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.IsAssignableFrom, true))
            {
                //
                // NOTE: This algorithm is mostly a "stolen" (and modified)
                //       variant of the .NET Framework "Reference Source"
                //       implementation of its Type.IsAssignableFrom method;
                //       hopefully, this will work on Mono, since it seems
                //       their Type.IsAssignableFrom method is broken for
                //       some ByRef types, e.g. the "System.IO.Stream&" and
                //       "System.Byte[]&" types.
                //
                if ((type1 == null) || (type2 == null))
                    return false;

                if (type1 == type2)
                    return true;

                if (type2.IsSubclassOf(type1))
                    return true;

                if (type1.IsInterface)
                {
                    return DoesImplementAnyInterface(type2, type1);
                }
                else if (type1.IsGenericParameter)
                {
                    return IsAssignableFromGenericParameterConstraint(
                        type1, type2, marshalFlags);
                }

                return false;
            }
            else
            {
                if ((type1 == null) || (type2 == null))
                    return false;

                return type1.IsAssignableFrom(type2);
            }
        }
 
        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsArray(
            Interpreter interpreter,     /* in */
            VariableFlags variableFlags, /* in */
            string name,                 /* in */
            ref IVariable variable       /* in, out */
            )
        {
            if (interpreter == null)
                return false;

            if ((variable == null) && interpreter.DoesVariableExist(
                    variableFlags, name, ref variable) != ReturnCode.Ok)
            {
                return false;
            }

            return EntityOps.IsArray2(variable);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsOfType(
            IObject @object,
            Type type,
            MarshalFlags marshalFlags,
            bool assignable
            )
        {
            if (@object == null)
                return false;

            return IsOfType(@object.Value, type, marshalFlags, assignable);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsOfType(
            object value,
            Type type,
            MarshalFlags marshalFlags,
            bool assignable
            )
        {
            if (type == null)
                return false;

            if (value == null)
            {
                //
                // NOTE: All reference types support being assigned
                //       a value of null.
                //
                return assignable ?
                    !type.IsValueType : IsObjectType(type, false);
            }

            Type valueType = value.GetType();

            if (valueType == null)
                return false;

            if (IsSameType(valueType, type, marshalFlags))
                return true;

            if (!assignable)
                return false;

            if (IsAssignableFrom(type, valueType, marshalFlags))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MaybeExtractObjectHandleTypeName(
            string objectName
            )
        {
            if (String.IsNullOrEmpty(objectName))
                return objectName;

            if (ObjectHandleIdRegEx == null)
                return objectName;

            return ObjectHandleIdRegEx.Replace(objectName, String.Empty);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int CompareSimilarTypeNames(
            string typeName1,
            string typeName2,
            StringComparison comparisonType
            )
        {
            int result = 0;

            typeName1 = MaybeExtractObjectHandleTypeName(typeName1);
            typeName2 = MaybeExtractObjectHandleTypeName(typeName2);

            if (typeName1 != null)
            {
                if (typeName2 != null)
                {
                    string[] parts1 = typeName1.Split(
                        TypeAndHandleDelimiters);

                    if (parts1 == null)
                        return 0;

                    string[] parts2 = typeName2.Split(
                        TypeAndHandleDelimiters);

                    if (parts2 == null)
                        return 0;

                    int length1 = parts1.Length;
                    int length2 = parts2.Length;

                    if ((length1 == 0) || (length2 == 0))
                        return ConversionOps.ToInt(length1 == length2);

                    int index1 = length1 - 1;
                    int index2 = length2 - 1;

                    while (true)
                    {
                        string part1 = parts1[index1];
                        string part2 = parts2[index2];

                        if (String.Equals(
                                part1, part2, comparisonType))
                        {
                            result++;
                            goto next;
                        }

                        if ((part1 != null) != (part2 != null))
                            goto next;

                        if (part1 == null)
                            goto next;

                        if (part1.StartsWith(part2, comparisonType) ||
                            part1.EndsWith(part2, comparisonType) ||
                            part2.StartsWith(part1, comparisonType) ||
                            part2.EndsWith(part1, comparisonType))
                        {
                            result++;
                            goto next;
                        }

                    next:

                        if (--index1 < 0)
                            break;

                        if (--index2 < 0)
                            break;
                    }
                }
            }
            else if (typeName2 == null)
            {
                result++;
            }

#if DEBUG
            TraceOps.DebugTrace(String.Format(
                "CompareSimilarTypeNames: comparison between {0} and {1} using {2} is {3}",
                FormatOps.WrapOrNull(typeName1), FormatOps.WrapOrNull(typeName2),
                FormatOps.WrapOrNull(comparisonType), result), typeof(MarshalOps).Name,
                TracePriority.MarshalDebug);
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSameType(
            Type type1,
            Type type2
            )
        {
            return IsSameType(type1, type2, IsSameTypeMarshalFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSameType(
            Type type1,
            Type type2,
            MarshalFlags marshalFlags
            )
        {
            return IsValueType(type1) || IsValueType(type2) ?
                IsSameValueType(type1, type2, true, FlagOps.HasFlags(
                marshalFlags, MarshalFlags.SpecialValueType, true)) :
                IsSameReferenceType(type1, type2, marshalFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSameValueType(
            Type type1,
            Type type2
            )
        {
            return IsSameValueType(type1, type2, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSameValueType(
            Type type1,
            Type type2,
            bool output
            )
        {
            return IsSameValueType(type1, type2, output, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSameValueType(
            Type type1,
            Type type2,
            bool output,
            bool special
            )
        {
            return IsSameValueType(type1, type2, true, output, special);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSameValueType(
            Type type1,
            Type type2,
            bool nullable,
            bool output,
            bool special
            )
        {
            Type valueType = null;

            if (type1 == type2)
                return true;
            else if (nullable && IsNullableType(type1, output, ref valueType) && (type2 == valueType))
                return true;
            else if (nullable && IsNullableType(type2, output, ref valueType) && (type1 == valueType))
                return true;
            else if (output && !type1.IsByRef && (type1.MakeByRefType() == type2))
                return true;
            else if (output && !type2.IsByRef && (type2.MakeByRefType() == type1))
                return true;
            else if (special && IsSpecialValueType(type1, type2, nullable, output))
                return true;
            else
                return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSpecialValueType(
            Type type1,
            Type type2,
            bool output
            )
        {
            if ((type1 == typeof(ValueType)) && type2.IsSubclassOf(typeof(ValueType)))
                return true;
            else if ((type2 == typeof(ValueType)) && type1.IsSubclassOf(typeof(ValueType)))
                return true;
            else if (output && (type1 == ByRefValueType) && type2.IsSubclassOf(typeof(ValueType)))
                return true;
            else if (output && (type2 == ByRefValueType) && type1.IsSubclassOf(typeof(ValueType)))
                return true;
            else
                return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSpecialValueType(
            Type type1,
            Type type2,
            bool nullable,
            bool output
            )
        {
            Type valueType = null;

            if (IsSpecialValueType(type1, type2, output))
            {
                return true;
            }
            else if (nullable && IsNullableType(type1, output, ref valueType) &&
                    IsSpecialValueType(type2, valueType, output))
            {
                return true;
            }
            else if (nullable && IsNullableType(type2, output, ref valueType) &&
                    IsSpecialValueType(type1, valueType, output))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsSameReferenceType(
            Type type1,
            Type type2,
            MarshalFlags marshalFlags
            )
        {
            return IsSameReferenceType(type1, type2, marshalFlags, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSameReferenceType(
            Type type1,
            Type type2,
            MarshalFlags marshalFlags,
            bool output
            )
        {
            if ((type1 == type2) ||
                IsAssignableFrom(type2, type1, marshalFlags))
            {
                return true;
            }
            else if (output && !type1.IsByRef && IsAssignableFrom(
                    type2, type1.MakeByRefType(), marshalFlags))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsValueType(
            Type type
            )
        {
            return IsValueType(type, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsValueType(
            Type type,
            bool output
            )
        {
            Type elementType = null;

            return IsValueType(type, output, ref elementType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSpecialValueType(
            Type type,
            bool output
            )
        {
            Type valueType = typeof(ValueType);

            if (type == valueType)
                return true;

            if (!output)
                return false;

            if (type == ByRefValueType)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsValueType(
            Type type,
            bool output,
            ref Type elementType
            )
        {
            if (type != null)
            {
                if (type.IsValueType || IsSpecialValueType(type, output))
                {
                    elementType = type;

                    return true;
                }
                else if (output && type.IsByRef)
                {
                    Type byRefElementType = type.GetElementType();

                    if ((byRefElementType != null) && byRefElementType.IsValueType)
                    {
                        elementType = byRefElementType;

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsNullableType(
            Type type
            )
        {
            return IsNullableType(type, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsNullableType(
            Type type,
            bool output
            )
        {
            Type valueType = null;

            return IsNullableType(type, output, ref valueType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsNullableType(
            Type type,
            bool output,
            ref Type valueType
            )
        {
            if (type != null)
            {
                if (type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    Type[] genericArguments = type.GetGenericArguments();

                    if ((genericArguments != null) && (genericArguments.Length > 0))
                        valueType = genericArguments[0];

                    return true;
                }
                else if (output && type.IsByRef)
                {
                    Type byRefElementType = type.GetElementType();

                    if ((byRefElementType != null) &&
                        byRefElementType.IsGenericType &&
                        (byRefElementType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        Type[] genericArguments = byRefElementType.GetGenericArguments();

                        if ((genericArguments != null) && (genericArguments.Length > 0))
                            valueType = genericArguments[0];

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MatchParameterTypes(
            TypeList parameterTypes,       /* in */
            ParameterInfo[] parameterInfo, /* in */
            MarshalFlags marshalFlags,     /* in */
            bool strictCount,              /* in */
            bool strictType,               /* in */
            ref Result error               /* out */
            )
        {
            if (parameterTypes != null)
            {
                if (parameterInfo != null)
                {
                    int parameterLength = parameterInfo.Length;

                    if (!strictCount ||
                        (parameterLength == parameterTypes.Count))
                    {
                        int minimumLength = Math.Min(
                            parameterLength, parameterTypes.Count);

                        for (int parameterIndex = 0;
                                parameterIndex < minimumLength;
                                parameterIndex++)
                        {
                            Type parameterType = parameterTypes[parameterIndex];

                            if (parameterType == null)
                                continue;

                            Type parameterInfoType =
                                parameterInfo[parameterIndex].ParameterType;

                            string parameterInfoName =
                                parameterInfo[parameterIndex].Name;

                            if (strictType)
                            {
                                if (parameterType != parameterInfoType)
                                {
                                    error = String.Format(
                                        "parameter {0} type mismatch, type {1} is not " +
                                        "equal to type {2}", FormatOps.ArgumentName(
                                        parameterIndex, parameterInfoName),
                                        GetErrorTypeName(parameterInfoType),
                                        GetErrorTypeName(parameterType));

                                    return false;
                                }
                            }
                            else
                            {
                                if ((parameterInfoType == null) || !IsAssignableFrom(
                                        parameterInfoType, parameterType, marshalFlags))
                                {
                                    error = String.Format(
                                        "parameter {0} type mismatch, type {1} is not " +
                                        "assignable from type {2}", FormatOps.ArgumentName(
                                        parameterIndex, parameterInfoName),
                                        GetErrorTypeName(parameterInfoType),
                                        GetErrorTypeName(parameterType));

                                    return false;
                                }
                            }
                        }

                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "paramter count {0} is not equal to type count {1}",
                            parameterLength, parameterTypes.Count);
                    }
                }
                else
                {
                    error = "invalid parameter info";
                }
            }
            else
            {
                error = "invalid parameter types";
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !MONO
        private static ReturnCode ResolveAssemblyClean(
            string name,
            ref Result result
            )
        {
            if (!String.IsNullOrEmpty(name))
            {
                int retry = 0;

                //
                // NOTE: If we cannot resolve the assembly using the clean method,
                //       we will fallback to attempting to load it.
                //
            fallback:
                if (retry == 0)
                {
                    MethodInfo methodInfo = null;

                    if (CommonOps.Runtime.IsFramework20()) // NOTE: Assumes !IsMono.
                    {
                        //
                        // NOTE: The .NET Framework 2.0 and 3.5 define the method
                        //       we need on the Assembly type; therefore, we will
                        //       try there (and only there).
                        //
                        methodInfo = typeof(Assembly).GetMethod(
                            EnumerateCacheMethodName, PrivateStaticMethodBindingFlags);
                    }
                    else if (CommonOps.Runtime.IsFramework40()) // NOTE: Assumes !IsMono.
                    {
                        //
                        // NOTE: The .NET Framework 4.0 defines the method we need
                        //       on the RuntimeAssembly type; therefore, we will
                        //       try there (and only there).
                        //
                        Type type = Type.GetType(RuntimeAssemblyTypeName);

                        if (type != null)
                            methodInfo = type.GetMethod(
                                EnumerateCacheMethodName, PrivateStaticMethodBindingFlags);
                    }

                    if (methodInfo != null)
                    {
                        try
                        {
                            //
                            // NOTE: Create an assembly name object using the string
                            //       provided by the script (verbatim).
                            //
                            AssemblyName oldAssemblyName = new AssemblyName(name);

                            //
                            // HACK: This method is private in the .NET Framework and we
                            //       need to use it; therefore, we use reflection to
                            //       invoke it.  This method requires the "UnmanagedCode"
                            //       permission.
                            //
                            AssemblyName newAssemblyName = methodInfo.Invoke(
                                null, new object[] { oldAssemblyName }) as AssemblyName;

                            //
                            // NOTE: Return non-zero if the assembly name was found in the
                            //       GAC.
                            //
                            if (newAssemblyName != null)
                            {
                                //
                                // NOTE: Return the newly resolved assembly name string.
                                //
                                result = newAssemblyName.ToString();

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                result = String.Format(
                                    "could not resolve assembly based on partial name {0}",
                                    FormatOps.WrapOrNull(name));
                            }
                        }
                        catch (Exception e)
                        {
                            result = e;
                        }
                    }
                    else
                    {
                        //
                        // NOTE: We cannot use the preferred method to resolve the
                        //       assembly; fallback to attempting to actually load
                        //       it.
                        //
                        retry++;

                        goto fallback;
                    }
                }
                else
                {
                    //
                    // NOTE: *FALLBACK* In case the clean method does not work.
                    //
                    return ResolveAssemblyDefault(name, ref result);
                }
            }
            else
            {
                result = "invalid assembly name";
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ResolveAssemblyDefault(
            string name,
            ref Result result
            )
        {
            if (!String.IsNullOrEmpty(name))
            {
                try
                {
                    Assembly assembly = Assembly.LoadWithPartialName(name);

                    if (assembly != null)
                    {
                        //
                        // NOTE: Return the newly resolved assembly name string.
                        //
                        result = assembly.FullName;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        result = String.Format(
                            "could not resolve assembly based on partial name {0}",
                            FormatOps.WrapOrNull(name));
                    }
                }
                catch (Exception e)
                {
                    result = e;
                }
            }
            else
            {
                result = "invalid assembly name";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ResolveAssembly(
            string name,
            ref Result result
            )
        {
#if !MONO
            if (!CommonOps.Runtime.IsMono())
                return ResolveAssemblyClean(name, ref result);
            else
#endif
                return ResolveAssemblyDefault(name, ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int SelectMethodIndex(
            Interpreter interpreter,                /* in */
            IBinder binder,                         /* in */
            CultureInfo cultureInfo,                /* in */
            Type type,                              /* in */
            MethodBase[] methods,                   /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in */
            object[] args,                          /* in */
            IntList methodIndexList,                /* in */
            ObjectArrayList argsList,               /* in */
            MarshalFlags marshalFlags,              /* in */
            ReorderFlags reorderFlags               /* in, NOT USED */
            )
        {
            if ((methodIndexList == null) || (methodIndexList.Count < 1))
                return Index.Invalid;

            int defaultMethodIndex = methodIndexList[0];
            int methodIndex = defaultMethodIndex;
            IScriptBinder scriptBinder = binder as IScriptBinder;

            if (scriptBinder != null)
            {
                ReturnCode code;
                Result error = null;

                code = scriptBinder.SelectMethodIndex(
                    type, cultureInfo, parameterTypes, parameterMarshalFlags,
                    methods, args, methodIndexList, argsList, ref methodIndex,
                    ref error);

                if (code == ReturnCode.Ok)
                    return methodIndex;
                else if (code == ReturnCode.Break)
                    return Index.Invalid;
                else if (code == ReturnCode.Continue)
                    return defaultMethodIndex;

                if (FlagOps.HasFlags(marshalFlags, MarshalFlags.Verbose, true))
                    DebugOps.Complain(interpreter, code, error);
            }

            return methodIndex;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ArgumentInfo CreateArgumentInfo(
            int parameterIndex,
            Type parameterType,
            bool output
            )
        {
            //
            // TODO: All parameters are treated as either input -OR-
            //       input/output, which should be what conforms to
            //       COM.  Verify that this will work in all cases.
            //
            return CreateArgumentInfo(
                parameterIndex, parameterType, true, output);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ArgumentInfo CreateArgumentInfo(
            int parameterIndex,
            Type parameterType,
            bool input,
            bool output
            )
        {
            return ArgumentInfo.Create(parameterIndex, parameterType,
                GetParameterName(parameterIndex, parameterType),
                input, output);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FixupArguments(
            Interpreter interpreter,                /* in */
            IBinder binder,                         /* in */
            OptionDictionary options,               /* in */
            CultureInfo cultureInfo,                /* in */
            string objectName,                      /* in */
            string methodName,                      /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in */
            MarshalFlags marshalFlags,              /* in */
            object[] args,                          /* in, out */
            bool strict,                            /* in */
            ref Result error                        /* out */
            )
        {
            if (parameterTypes == null)
            {
                error = "invalid parameter type list";
                return ReturnCode.Error;
            }

            if (args == null)
            {
                error = "invalid argument array";
                return ReturnCode.Error;
            }

            int argumentLength = args.Length;
            int parameterCount = parameterTypes.Count;

            if (strict && (argumentLength != parameterCount))
            {
                error = String.Format(
                    "method {0} requires exactly {1} {2} and {3} {4} supplied",
                    FormatOps.MethodOverload(Index.Invalid, objectName, methodName),
                    parameterCount, (parameterCount != 1) ? "arguments" : "argument",
                    argumentLength, (argumentLength != 1) ? "were" : "was");

                return ReturnCode.Error;
            }

            object[] newArgs = new object[argumentLength];

            for (int argumentIndex = 0;
                    argumentIndex < argumentLength;
                    argumentIndex++)
            {
                object arg = args[argumentIndex];

                Type parameterType = (argumentIndex < parameterCount) ?
                    parameterTypes[argumentIndex] : null;

                if (parameterType != null)
                {
                    bool output = parameterType.IsByRef;

                    if (FixupArgument(
                            interpreter, binder, options, cultureInfo,
                            parameterType, CreateArgumentInfo(
                                argumentIndex, parameterType, output),
                            marshalFlags, true, output, ref arg,
                            ref error) == ReturnCode.Ok)
                    {
                        newArgs[argumentIndex] = arg;
                    }
                    else
                    {
                        return ReturnCode.Error;
                    }
                }
                else
                {
                    newArgs[argumentIndex] = arg;
                }
            }

            //
            // NOTE: Success, commit changes to caller's original arguments.
            //
            Array.Copy(newArgs, args, argumentLength);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void SetupTemporaryByRefVariableNames(
            Interpreter interpreter,
            ArgumentInfoList argumentInfoList
            )
        {
            if ((interpreter == null) || (argumentInfoList == null))
                return;

            ulong randomNumber = interpreter.GetRandomNumber();

            foreach (ArgumentInfo argumentInfo in argumentInfoList)
            {
                if (argumentInfo == null)
                    continue;

                argumentInfo.SetName(String.Format(
                    "{0}byref_{1}_{2}", Vars.VarsPrefix,
                    FormatOps.Hexadecimal(randomNumber, true),
                    argumentInfo.Index));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetByRefArgumentInfo(
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in: NOT USED */
            MarshalFlags marshalFlags,              /* in: NOT USED */
            ref ArgumentInfoList argumentInfoList,  /* out */
            ref Result error                        /* out */
            )
        {
            if (parameterTypes == null)
            {
                error = "invalid parameter type list";
                return ReturnCode.Error;
            }

            for (int parameterIndex = 0;
                    parameterIndex < parameterTypes.Count;
                    parameterIndex++)
            {
                Type parameterType = parameterTypes[parameterIndex];

                if (parameterType == null)
                    continue;

                bool output = parameterType.IsByRef;

                if (output)
                {
                    if (argumentInfoList == null)
                        argumentInfoList = new ArgumentInfoList();

                    argumentInfoList.Add(CreateArgumentInfo(
                        parameterIndex, parameterType, output));
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Type MaybeGenericType(
            Type type,            /* in */
            TypeList objectTypes, /* in */
            ValueFlags flags,     /* in */
            ref ResultList errors /* out */
            )
        {
            if (objectTypes != null)
            {
                try
                {
                    if ((type != null) && type.ContainsGenericParameters)
                    {
                        Type[] genericArguments = type.GetGenericArguments();

                        if ((genericArguments != null) &&
                            (genericArguments.Length > 0))
                        {
                            return type.MakeGenericType(objectTypes.ToArray());
                        }
                    }
                }
                catch (Exception e)
                {
                    if (!FlagOps.HasFlags(flags, ValueFlags.NoException, true))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }
            }

            return type;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static MethodBase MaybeGenericMethod(
            MethodBase[] methods,  /* in, out */
            int matchIndex,        /* in */
            TypeList methodTypes,  /* in */
            bool verbose,          /* in */
            ref ResultList errors  /* out */
            )
        {
            MethodBase method = null;

            if ((methods != null) &&
                (matchIndex >= 0) && (matchIndex < methods.Length))
            {
                //
                // NOTE: Ok, the specified method is in bounds, grab it.
                //
                method = methods[matchIndex];

                //
                // NOTE: Further handling is only needed when the caller has
                //       specifically requested it.
                //
                if (methodTypes != null)
                {
                    try
                    {
                        //
                        // NOTE: Prior to checking for generic arguments, we
                        //       need an actual MethodInfo, so attempt a cast
                        //       to that type.  Failing that, just return the
                        //       existing MethodBase.
                        //
                        MethodInfo methodInfo = method as MethodInfo;

                        if ((methodInfo != null) &&
                            methodInfo.ContainsGenericParameters)
                        {
                            Type[] genericArguments =
                                methodInfo.GetGenericArguments();

                            if ((genericArguments != null) &&
                                (genericArguments.Length > 0))
                            {
                                MethodInfo newMethodInfo =
                                    methodInfo.MakeGenericMethod(
                                        methodTypes.ToArray());

                                //
                                // HACK: Replace the original method in the
                                //       array.  This is necessary because we
                                //       need it in order to be able to invoke
                                //       it and further up the call stack our
                                //       various callers expect to be able to
                                //       fetch it directly from the methods
                                //       array (at the corresponding index).
                                //
                                methods[matchIndex] = newMethodInfo;

                                //
                                // NOTE: Also return the new MethodInfo for
                                //       ease-of-use by our direct caller.
                                //
                                return newMethodInfo;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        if (verbose || (errors.Count == 0))
                            errors.Add(e);
                    }
                }
            }

            return method;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsByRefType(
            Type type,
            bool output,
            ref Type byValType
            )
        {
            if (type != null)
            {
                if (output && type.IsByRef)
                {
                    Type byRefElementType = type.GetElementType();

                    if (byRefElementType != null)
                    {
                        byValType = byRefElementType;
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int CountSubTypes(
            Type type,
            bool output
            )
        {
            Type subType = null;

            return CountSubTypes(type, output, ref subType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int CountSubTypes(
            Type type,
            bool output,
            ref Type subType
            )
        {
            int result = 0;

            if (type != null)
            {
                Type localType = type;

                if (IsByRefType(localType, output, ref localType))
                    result++;

                if (IsArrayType(localType, ref localType))
                    result++;

                if (IsNullableType(localType, output, ref localType))
                    result++;

                subType = localType;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsBaseOfValueType(
            Type type,
            bool output
            )
        {
            return IsObjectType(type, output) || IsSpecialValueType(type, output);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void AjustTypeDepthForStringTypes(
            Type type,
            int stringDepth, /* -1 <= x <= 1 */
            bool output,
            ref int totalDepth
            )
        {
            //
            // HACK: *SPECIAL* Maybe apply a "penalty" or "bonus" to any
            //       types that are trivially convertible from a string
            //       (e.g. System.String).
            //
            if ((stringDepth != 0) && IsSimpleTypeForToString(type, output))
                totalDepth += stringDepth; /* NOTE: Plus/minus one. */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int CalculateTypeDepth(
            Type type,
            int stringDepth, /* -1 <= x <= 1 */
            bool subTypes,
            bool valueTypes,
            bool output
            )
        {
            int totalDepth = 0;

            while (type != null)
            {
                AjustTypeDepthForStringTypes(
                    type, stringDepth, output, ref totalDepth);

                //
                // NOTE: *SPECIAL* When specially tracking value types,
                //       make sure not to count the special "ValueType"
                //       type twice.
                //
                if (!valueTypes || !IsSpecialValueType(type, output))
                    totalDepth++;

                if (subTypes || valueTypes)
                {
                    int subTypeCount = 0;
                    Type subType = type;

                    subTypeCount = CountSubTypes(
                        subType, output, ref subType);

                    if (subTypes)
                        totalDepth += subTypeCount;

                    if (!Object.ReferenceEquals(subType, type))
                    {
                        if (valueTypes &&
                            !IsBaseOfValueType(subType, output) &&
                            !IsValueType(subType, output))
                        {
                            totalDepth++;
                        }

                        if (subType == null)
                            break;

                        type = subType;

                        AjustTypeDepthForStringTypes(
                            type, stringDepth, output, ref totalDepth);
                    }
                }

                type = type.BaseType;
            }

            return totalDepth;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode CalculateTypeDepths(
            ParameterInfo[] parameterInfo,
            object[] args,
            int stringDepth, /* -1 <= x <= 1 */
            bool parameterTypes,
            bool subTypes,
            bool valueTypes,
            bool output,
            ref IntList depths,
            ref Result error
            )
        {
            try
            {
                if (parameterInfo == null)
                {
                    error = "invalid parameter info";
                    return ReturnCode.Error;
                }

                if (args == null)
                {
                    error = "invalid argument array";
                    return ReturnCode.Error;
                }

                if (depths == null)
                    depths = new IntList(args.Length);

                for (int index = 0; index < args.Length; index++)
                {
                    object arg = args[index];
                    Type type = (arg != null) ? arg.GetType() : null;

                    if (parameterTypes && (index < parameterInfo.Length))
                    {
                        if ((type == null) || IsStringType(type, output))
                        {
                            Type parameterType =
                                parameterInfo[index].ParameterType;

                            if ((parameterType != null) &&
                                !IsStringType(parameterType, output))
                            {
                                type = parameterType;
                            }
                        }
                    }

                    depths.Add(CalculateTypeDepth(
                        type, stringDepth, subTypes, valueTypes,
                        output));
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetParameterCounts(
            ParameterInfo[] parameterInfo,
            object[] args,
            bool argumentCounts,
            ref int minimumCount,
            ref int maximumCount,
            ref Result error
            )
        {
            if (GetParameterCounts(
                    parameterInfo, ref minimumCount, ref maximumCount,
                    ref error) == ReturnCode.Ok)
            {
                //
                // BUGFIX: If the minimum or maximum parameter count is not
                //         valid, that means the method takes an arbitrary
                //         number of parameters.  Since our caller uses the
                //         counts to rank methods, convert an invalid count
                //         into the actual parameter count, which should be
                //         zero for the minimum and the number of supplied
                //         arguments for the maximum.
                //
                if (argumentCounts && (args != null))
                {
                    if (minimumCount == Count.Invalid)
                        minimumCount = 0;

                    if (maximumCount == Count.Invalid)
                        maximumCount = args.Length;
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TraceMethodOverloads(
            IBinder binder,                         /* in */
            CultureInfo cultureInfo,                /* in */
            MethodBase[] methods,                   /* in */
            MarshalFlagsList parameterMarshalFlags, /* in */
            ObjectArrayList argsList,               /* in */
            IntList methodIndexList,                /* in */
            MarshalFlags marshalFlags               /* in */
            )
        {
            TraceMethodOverloads(
                binder, cultureInfo, methods, parameterMarshalFlags, argsList,
                methodIndexList, marshalFlags, ReorderFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TraceMethodOverloads(
            IBinder binder,                         /* in */
            CultureInfo cultureInfo,                /* in */
            MethodBase[] methods,                   /* in */
            MarshalFlagsList parameterMarshalFlags, /* in */
            ObjectArrayList argsList,               /* in */
            IntList methodIndexList,                /* in */
            MarshalFlags marshalFlags,              /* in */
            ReorderFlags reorderFlags               /* in */
            )
        {
            if (methods == null)
            {
                TraceOps.DebugTrace(
                    "TraceMethodOverloads: missing method array",
                    typeof(MarshalOps).Name, TracePriority.MarshalError);

                return;
            }

            if (argsList == null)
            {
                TraceOps.DebugTrace(
                    "TraceMethodOverloads: missing argument lists",
                    typeof(MarshalOps).Name, TracePriority.MarshalError);

                return;
            }

            if (methodIndexList == null)
            {
                TraceOps.DebugTrace(
                    "TraceMethodOverloads: missing method index list",
                    typeof(MarshalOps).Name, TracePriority.MarshalError);

                return;
            }

            int argsIndex = 0;

            foreach (int methodIndex in methodIndexList)
            {
                MethodBase method = null;

                if ((methodIndex >= 0) && (methodIndex < methods.Length))
                {
                    method = methods[methodIndex];
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "TraceMethodOverloads: out-of-bounds method index {0}",
                        methodIndex), typeof(MarshalOps).Name,
                        TracePriority.MarshalError);
                }

                object[] args = null;

                if ((argsIndex >= 0) && (argsIndex < argsList.Count))
                {
                    args = argsList[argsIndex];
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "TraceMethodOverloads: out-of-bounds argument list index {0}",
                        argsIndex), typeof(MarshalOps).Name,
                        TracePriority.MarshalError);
                }

                TraceOps.DebugTrace(String.Format(
                    "TraceMethodOverloads: marshalFlags = {0}, reorderFlags = {1}, " +
                    "methodIndex = {2}, argsIndex = {3}, method = {4}, " +
                    "parameterMarshalFlags = {5}, args = {6}",
                    FormatOps.WrapOrNull(marshalFlags), FormatOps.WrapOrNull(
                    reorderFlags), methodIndex, argsIndex, (method != null) ?
                    method.ToString() : FormatOps.DisplayNull, FormatOps.DisplayList(
                    parameterMarshalFlags), FormatOps.MethodArguments(binder,
                    cultureInfo, args, true)), typeof(MarshalOps).Name,
                    TracePriority.MarshalDebug);

                argsIndex++;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void TraceReorderedMethods(
            IBinder binder,                                   /* in */
            CultureInfo cultureInfo,                          /* in */
            MethodBase[] methods,                             /* in */
            ObjectArrayList argsList,                         /* in */
            List<ParameterDataTriplet> parameterDataTriplets, /* in */
            MarshalFlags marshalFlags,                        /* in */
            ReorderFlags reorderFlags                         /* in */
            )
        {
            if (methods == null)
            {
                TraceOps.DebugTrace(
                    "TraceReorderedMethods: missing method array",
                    typeof(MarshalOps).Name, TracePriority.MarshalError);

                return;
            }

            if (argsList == null)
            {
                TraceOps.DebugTrace(
                    "TraceReorderedMethods: missing argument lists",
                    typeof(MarshalOps).Name, TracePriority.MarshalError);

                return;
            }

            if (parameterDataTriplets == null)
            {
                TraceOps.DebugTrace(
                    "TraceReorderedMethods: missing parameter data",
                    typeof(MarshalOps).Name, TracePriority.MarshalError);

                return;
            }

            foreach (ParameterDataTriplet triplet in parameterDataTriplets)
            {
                if (triplet == null)
                    continue;

                IPair<int> pair = triplet.X;

                if (pair == null)
                    continue;

                MethodBase method = null;
                int methodIndex = pair.X;

                if ((methodIndex >= 0) && (methodIndex < methods.Length))
                {
                    method = methods[methodIndex];
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "TraceReorderedMethods: out-of-bounds method index {0}",
                        methodIndex), typeof(MarshalOps).Name,
                        TracePriority.MarshalError);
                }

                object[] args = null;
                int argsIndex = pair.Y;

                if ((argsIndex >= 0) && (argsIndex < argsList.Count))
                {
                    args = argsList[argsIndex];
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "TraceReorderedMethods: out-of-bounds argument list index {0}",
                        argsIndex), typeof(MarshalOps).Name,
                        TracePriority.MarshalError);
                }

                TraceOps.DebugTrace(String.Format(
                    "TraceReorderedMethods: reordering done, marshalFlags = {0}, " +
                    "reorderFlags = {1}, triplet = {2}, methodIndex = {3}, " +
                    "argsIndex = {4}, method = {5}, args = {6}",
                    FormatOps.WrapOrNull(marshalFlags), FormatOps.WrapOrNull(
                    reorderFlags), ParameterDataTriplet.ToString(triplet),
                    methodIndex, argsIndex, (method != null) ? method.ToString() :
                    FormatOps.DisplayNull, FormatOps.MethodArguments(binder,
                    cultureInfo, args, true)), typeof(MarshalOps).Name,
                    TracePriority.MarshalDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Permit reordering of the method index list by taking into
        //       account the least or greatest number of total parameters
        //       optionally in combination with the relative positions of
        //       the parameter types in the overall type hierarchy.  This
        //       method will be used in combination with and/or by the
        //       callers of the FindMethodsAndFixupArguments method.
        //
        public static ReturnCode ReorderMethodIndexes(
            IBinder binder,               /* in */
            CultureInfo cultureInfo,      /* in */
            MethodBase[] methods,         /* in */
            MarshalFlags marshalFlags,    /* in */
            ReorderFlags reorderFlags,    /* in */
            ref IntList methodIndexList,  /* in, out */
            ref ObjectArrayList argsList, /* in, out */
            ref ResultList errors         /* in, out */
            )
        {
            bool verbose = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.Verbose, true);

            bool fallbackOkOnError = FlagOps.HasFlags(
                reorderFlags, ReorderFlags.FallbackOkOnError, true);

            if (methods == null)
            {
                if (errors == null)
                    errors = new ResultList();

                if (verbose || (errors.Count == 0))
                    errors.Add("invalid method array");

                if (fallbackOkOnError)
                    return ReturnCode.Ok;
                else
                    return ReturnCode.Error;
            }

            if (methodIndexList == null)
            {
                if (errors == null)
                    errors = new ResultList();

                if (verbose || (errors.Count == 0))
                    errors.Add("invalid method index list");

                if (fallbackOkOnError)
                    return ReturnCode.Ok;
                else
                    return ReturnCode.Error;
            }

            if (argsList == null)
            {
                if (errors == null)
                    errors = new ResultList();

                if (verbose || (errors.Count == 0))
                    errors.Add("invalid argument array list");

                if (fallbackOkOnError)
                    return ReturnCode.Ok;
                else
                    return ReturnCode.Error;
            }

            //
            // NOTE: If there are not at least two method overloads, do
            //       nothing and return success, as there is no actual
            //       reordering work to do.
            //
            int methodIndexCount = methodIndexList.Count;

            if (methodIndexCount < 2)
                return ReturnCode.Ok; /* NOTE: Nothing to do. */

            //
            // NOTE: At this point, verify that our assumption about the
            //       number of method indexes matching exactly with the
            //       number of argument lists is actually true.  If not,
            //       we cannot continue.
            //
            int argsListCount = argsList.Count;

            if (methodIndexCount != argsListCount)
            {
                if (errors == null)
                    errors = new ResultList();

                if (verbose || (errors.Count == 0))
                {
                    errors.Add(String.Format(
                        "argument array list has {0} elements and needs {1}",
                        argsListCount, methodIndexCount));
                }

                if (fallbackOkOnError)
                    return ReturnCode.Ok;
                else
                    return ReturnCode.Error;
            }

            bool useParameterCounts = FlagOps.HasFlags(
                reorderFlags, ReorderFlags.ParameterCountMask, false);

            bool useTypeDepths = FlagOps.HasFlags(
                reorderFlags, ReorderFlags.ParameterTypeDepthMask, false);

            bool traceResults = FlagOps.HasFlags(
                reorderFlags, ReorderFlags.TraceResults, true);

            if (!useParameterCounts && !useTypeDepths)
            {
                if (traceResults)
                {
                    TraceMethodOverloads(
                        binder, cultureInfo, methods, null, argsList,
                        methodIndexList, marshalFlags, reorderFlags);
                }

                return ReturnCode.Ok; /* NOTE: Nothing else to do. */
            }

            //
            // NOTE: By default, when calculating type depths, no special
            //       consideration is given for a type that happens to be
            //       trivially convertible from a string (e.g. the types
            //       System.String, StringList, etc); however, there may
            //       be certain special cases where it may be desirable to
            //       either encourage or discourage selection of a method
            //       overload that uses one of these types.  Therefore,
            //       two flags are provided:
            //
            //       1. StringTypePenalty: This flag causes one "level"
            //          to be subtracted from the calculated type depth.
            //
            //       2. StringTypeBonus: This flag causes one "level" to
            //          be added to the calculated type depth.
            //
            //       If both of the above flags are used together, they
            //       will cancel each other out; this is by design.
            //
            int stringDepth = 0;

            if (FlagOps.HasFlags(
                    reorderFlags, ReorderFlags.StringTypePenalty, true))
            {
                stringDepth += StringTypePenalty;
            }

            if (FlagOps.HasFlags(
                    reorderFlags, ReorderFlags.StringTypeBonus, true))
            {
                stringDepth += StringTypeBonus;
            }

            bool useArgumentCounts = useParameterCounts ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.UseArgumentCounts, true) : false;

            bool strictParameterCounts = useParameterCounts ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.StrictParameterCounts, true) : false;

            bool continueParameterCounts = useParameterCounts ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.ContinueParameterCounts, true) : false;

            bool strictTypeDepths = useTypeDepths ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.StrictTypeDepths, true) : false;

            bool continueTypeDepths = useTypeDepths ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.ContinueTypeDepths, true) : false;

            bool useParameterTypes = useTypeDepths ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.UseParameterTypes, true) : false;

            bool useSubTypeDepths = useTypeDepths ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.SubTypeDepths, true) : false;

            bool useValueTypeDepths = useTypeDepths ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.ValueTypeDepths, true) : false;

            bool useByRefTypeDepths = useTypeDepths ? FlagOps.HasFlags(
                reorderFlags, ReorderFlags.ByRefTypeDepths, true) : false;

            List<ParameterDataTriplet> parameterDataTriplets =
                new List<ParameterDataTriplet>(methodIndexCount);

            int argsIndex = 0;

            foreach (int methodIndex in methodIndexList)
            {
                ParameterInfo[] parameterInfo;
                MethodBase method = methods[methodIndex];

                if (method != null)
                    parameterInfo = method.GetParameters();
                else
                    parameterInfo = null;

                object[] args = argsList[argsIndex];
                int minimumCount = 0;
                int maximumCount = 0;
                Result error = null; /* REUSED */

                if (useParameterCounts && GetParameterCounts(
                        parameterInfo, args, useArgumentCounts,
                        ref minimumCount, ref maximumCount,
                        ref error) != ReturnCode.Ok)
                {
                    if (errors == null)
                        errors = new ResultList();

                    if ((error != null) && (verbose || (errors.Count == 0)))
                        errors.Add(error);

                    if (strictParameterCounts)
                    {
                        //
                        // NOTE: Failed to query the minimum and/or maximum
                        //       parameter counts for this method overload.
                        //       Return the error to the caller verbatim.
                        //
                        return ReturnCode.Error;
                    }
                    else if (continueParameterCounts)
                    {
                        //
                        // NOTE: Ok, just skip this method overload when
                        //       operating in "continue" mode.
                        //
                        argsIndex++; // Advance to next arguments list.
                        continue;
                    }
                    else
                    {
                        //
                        // NOTE: Otherwise, we cannot continue because the
                        //       constructed parameter data triplet would
                        //       contain undefined (zero) parmaeter counts
                        //       (i.e. even though they were specifically
                        //       requested by the caller).
                        //
                        if (traceResults)
                        {
                            TraceReorderedMethods(
                                binder, cultureInfo, methods, argsList,
                                parameterDataTriplets, marshalFlags,
                                reorderFlags);
                        }

                        return ReturnCode.Ok;
                    }
                }

                IntList depths = null;

                error = null;

                if (useTypeDepths && CalculateTypeDepths(
                        parameterInfo, args, stringDepth,
                        useParameterTypes, useSubTypeDepths,
                        useValueTypeDepths, useByRefTypeDepths,
                        ref depths, ref error) != ReturnCode.Ok)
                {
                    if (errors == null)
                        errors = new ResultList();

                    if ((error != null) && (verbose || (errors.Count == 0)))
                        errors.Add(error);

                    if (strictTypeDepths)
                    {
                        //
                        // NOTE: Failed to calculate the type depths for
                        //       this method overload.  Return the error
                        //       to the caller verbatim.
                        //
                        return ReturnCode.Error;
                    }
                    else if (continueTypeDepths)
                    {
                        //
                        // NOTE: Ok, just skip this method overload when
                        //       operating in "continue" mode.
                        //
                        argsIndex++; // Advance to next arguments list.
                        continue;
                    }
                    else
                    {
                        //
                        // NOTE: Otherwise, we cannot continue because the
                        //       constructed ParameterDataTriplet would not
                        //       contain any type depths (i.e. even though
                        //       they were specifically requested by the
                        //       caller).
                        //
                        if (traceResults)
                        {
                            TraceReorderedMethods(
                                binder, cultureInfo, methods, argsList,
                                parameterDataTriplets, marshalFlags,
                                reorderFlags);
                        }

                        return ReturnCode.Ok;
                    }
                }

                //
                // NOTE: Keep track of the parameter counts and type depths
                //       for this method overload.
                //
                ParameterDataTriplet triplet = new ParameterDataTriplet(
                    new Pair<int>(methodIndex, argsIndex),
                    new Pair<int>(minimumCount, maximumCount),
                    depths);

                parameterDataTriplets.Add(triplet);
                argsIndex++; // Advance to next arguments list.
            }

            //
            // NOTE: Next, try to sort the method indexes into "priority"
            //       order, based on the criteria (via the ReorderFlags)
            //       specified by the caller.  If this ends up throwing
            //       an exception, just translate it into an error and
            //       return failure.
            //
            try
            {
                parameterDataTriplets.Sort(new ParameterDataComparer(
                    reorderFlags)); /* throw */
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                if (verbose || (errors.Count == 0))
                    errors.Add(e);

                if (fallbackOkOnError)
                    return ReturnCode.Ok;
                else
                    return ReturnCode.Error;
            }

            //
            // NOTE: If requested, trace the resulting list of sorted method
            //       overloads.
            //
            if (traceResults)
            {
                TraceReorderedMethods(
                    binder, cultureInfo, methods, argsList,
                    parameterDataTriplets, marshalFlags,
                    reorderFlags);
            }

            //
            // NOTE: Finally, build the new lists for the caller and then
            //       replace their existing lists.
            //
            IntList localMethodIndexList = new IntList(methodIndexCount);
            ObjectArrayList localArgsList = new ObjectArrayList(methodIndexCount);

            foreach (ParameterDataTriplet triplet in parameterDataTriplets)
            {
                IPair<int> pair = triplet.X;

                if (pair == null)
                    continue;

                localMethodIndexList.Add(pair.X);
                localArgsList.Add(argsList[pair.Y]);
            }

            methodIndexList = localMethodIndexList;
            argsList = localArgsList;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool TryGetArgumentInfoList(
            IntArgumentInfoListDictionary argumentInfoListDictionary, /* in */
            int methodIndex,                                          /* in */
            out ArgumentInfoList argumentInfoList                     /* out */
            )
        {
            if ((argumentInfoListDictionary == null) ||
                (methodIndex == Index.Invalid))
            {
                argumentInfoList = null;
                return false;
            }

            return argumentInfoListDictionary.TryGetValue(
                methodIndex, out argumentInfoList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsInput(
            Type parameterType, /* NOT USED */
            ParameterInfo parameterInfo,
            MarshalFlags marshalFlags
            )
        {
            if (parameterInfo == null)
                return false;

            if (parameterInfo.IsIn)
                return true;

            if (FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.UseInOnly, true))
            {
                return false;
            }

            if (!parameterInfo.IsOut)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsOutput(
            Type parameterType,
            ParameterInfo parameterInfo,
            MarshalFlags marshalFlags
            )
        {
            if (parameterType == null)
                return false;

            if (parameterType.IsByRef)
                return true;

            if (FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.UseByRefOnly, true))
            {
                return false;
            }

            if (parameterInfo.IsOut)
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FindMethodsAndFixupArguments( /* METHOD OVERLOAD RESOLUTION ENGINE */
            Interpreter interpreter,                                      /* in */
            IBinder binder,                                               /* in */
            OptionDictionary options,                                     /* in */
            CultureInfo cultureInfo,                                      /* in */
            Type type,                                                    /* in */
            string objectName,                                            /* in, NOT USED, RESERVED */
            string fullObjectName,                                        /* in */
            string methodName,                                            /* in */
            string fullMethodName,                                        /* in */
            MemberTypes memberTypes,                                      /* in */
            BindingFlags bindingFlags,                                    /* in */
            MethodBase[] methods,                                         /* in */
            TypeList methodTypes,                                         /* in */
            TypeList parameterTypes,                                      /* in */
            MarshalFlagsList parameterMarshalFlags,                       /* in */
            object[] args,                                                /* in */
            int limit,                                                    /* in */
            MarshalFlags marshalFlags,                                    /* in */
            ref IntList methodIndexList,                                  /* in, out */
            ref ObjectArrayList argsList,                                 /* in, out */
            ref IntArgumentInfoListDictionary argumentInfoListDictionary, /* in, out */
            ref ResultList errors                                         /* in, out */
            )
        {
            bool verbose = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.Verbose, true);

            if (methods != null)
            {
                bool strictMatchCount = FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.StrictMatchCount, true);

                bool strictMatchType = FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.StrictMatchType, true);

                bool forceParameterType = FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.ForceParameterType, true);

                bool noByRefArguments = FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.NoByRefArguments, true);

                bool traceResults = FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.TraceResults, true);

                //
                // NOTE: What method name, if any, did we match to?
                //
                string matchMethodName = null;

                //
                // NOTE: Process each method info and attempt to match it against
                //       the supplied arguments taking into account the number and
                //       coerced types of the arguments, the (in, out) attributes,
                //       and the params argument, if any.
                //
                for (int matchIndex = 0; matchIndex < methods.Length; matchIndex++)
                {
                    //
                    // NOTE: Get the method for the current method, which may be a
                    //       generic method needing type information to resolve it.
                    //
                    MethodBase method = MaybeGenericMethod(
                        methods, matchIndex, methodTypes, verbose, ref errors);

                    //
                    // NOTE: Skip methods that are obviously invalid.
                    //
                    if (method == null)
                        continue;

                    //
                    // NOTE: We need the name of the method to check.
                    //
                    string checkMethodName = method.Name;

                    //
                    // NOTE: If the method name appears to be invalid, skip it.
                    //
                    if (String.IsNullOrEmpty(checkMethodName))
                        continue;

                    //
                    // NOTE: The method name must be matched first since we
                    //       have likely been passed all the methods for this
                    //       type.
                    //
                    if (MatchMethodName(
                            checkMethodName, methodName, memberTypes,
                            bindingFlags))
                    {
                        //
                        // NOTE: Set the name match index so that even if we
                        //       ultimately do not find a matching method at
                        //       least the error handling code knows that we
                        //       got this far.
                        //
                        matchMethodName = checkMethodName;

                        //
                        // NOTE: What are the output arguments for the current
                        //       method overload?
                        //
                        ArgumentInfoList methodArgumentInfoList = null;

                        //
                        // NOTE: Does the caller want to attempt to convert
                        //       arguments and filter on them?
                        //
                        if (args != null)
                        {
                            //
                            // NOTE: We need the parameters of the method to check.
                            //
                            ParameterInfo[] checkParameterInfo = method.GetParameters();

                            //
                            // NOTE: If the parameters appear to be invalid, skip them.
                            //
                            if (checkParameterInfo != null)
                            {
                                int minimumCount = 0;
                                int maximumCount = 0;
                                Result error = null;

                                if (GetParameterCounts(
                                        checkParameterInfo, ref minimumCount,
                                        ref maximumCount, ref error) == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Get the number of arguments supplied by the caller.
                                    //
                                    int argumentLength = args.Length;

                                    //
                                    // NOTE: Does the number of arguments supplied by the caller fit
                                    //       within the minimum and maximum number of arguments for
                                    //       this method?
                                    //
                                    if (((minimumCount == Count.Invalid) || (argumentLength >= minimumCount)) &&
                                        ((maximumCount == Count.Invalid) || (argumentLength <= maximumCount)))
                                    {
                                        //
                                        // NOTE: Check for any parameter type hints provided by the caller.
                                        //       If they were supplied then they must match up with the
                                        //       parameter types for this method.
                                        //
                                        if ((parameterTypes == null) || MatchParameterTypes(
                                                parameterTypes, checkParameterInfo, marshalFlags,
                                                strictMatchCount, strictMatchType, ref error))
                                        {
                                            //
                                            // NOTE: Get the number of formal arguments for the method to
                                            //       check.
                                            //
                                            int checkArgumentLength = checkParameterInfo.Length;

                                            //
                                            // NOTE: Create a temporary holding area for attempted argument
                                            //       type conversions because in the case we do not fully
                                            //       match the method we do not want to fiddle with the
                                            //       caller's original arguments array.
                                            //
                                            object[] newArgs = new object[checkArgumentLength];

                                            //
                                            // NOTE: Start consuming the original arguments at the first
                                            //       argument.
                                            //
                                            int argumentIndex = 0;

                                            //
                                            // NOTE: Reset the value of the argument being processed.
                                            //
                                            object matchArgumentValue = null;

                                            //
                                            // NOTE: Process each formal argument, checking the various
                                            //       constraints imposed by it upon the supplied argument
                                            //       value.
                                            //
                                            for (int parameterIndex = 0; parameterIndex < checkArgumentLength; parameterIndex++)
                                            {
                                                //
                                                // NOTE: Check argument attributes to see if they are ref (in, out) or out (out).
                                                //       Enforce argument being an existing script variable name for ref.
                                                //       Enforce argument being a script variable name for out.
                                                //
                                                //       Also, arrays need to be handled in concert with (in, out).  Script arrays
                                                //       will be used and the indexes will always be in the form (x) or (x,y,z)
                                                //       where x is an integer index into a single or multi-dimensional array, and
                                                //       y and z, etc are integer indexes of a multi-dimensional array.
                                                //
                                                ParameterInfo parameterInfo = checkParameterInfo[parameterIndex];

                                                if (parameterInfo == null)
                                                {
                                                    if (errors == null)
                                                        errors = new ResultList();

                                                    if (verbose || (errors.Count == 0))
                                                    {
                                                        error = String.Format(
                                                            "could not check method {0} parameter {1}: " +
                                                            "parameter information unavailable",
                                                            FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                            parameterIndex);

                                                        errors.Add(error);
                                                    }
                                                    break;
                                                }

                                                Type parameterType;

                                                if (forceParameterType)
                                                {
                                                    if ((parameterTypes != null) &&
                                                        (parameterIndex < parameterTypes.Count))
                                                    {
                                                        parameterType = parameterTypes[parameterIndex];
                                                    }
                                                    else
                                                    {
                                                        parameterType = parameterInfo.ParameterType;
                                                    }
                                                }
                                                else
                                                {
                                                    parameterType = parameterInfo.ParameterType;
                                                }

                                                MarshalFlags perParameterMarshalFlags = MarshalFlags.None;

                                                if ((parameterMarshalFlags != null) &&
                                                    (parameterIndex < parameterMarshalFlags.Count))
                                                {
                                                    perParameterMarshalFlags = parameterMarshalFlags[parameterIndex];
                                                }

                                                int parameterPosition = parameterInfo.Position;
                                                string parameterName = parameterInfo.Name;

                                                //
                                                // NOTE: Check if this parameter is of a pointer type.
                                                //
                                                if (parameterType.IsPointer)
                                                {
                                                    //
                                                    // NOTE: We do not allow and cannot process pointer types.
                                                    //
                                                    if (errors == null)
                                                        errors = new ResultList();

                                                    if (verbose || (errors.Count == 0))
                                                    {
                                                        error = String.Format(
                                                            "could not convert method {0} argument {1} " +
                                                            "to type {2}: pointers types are forbidden",
                                                            FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                            FormatOps.ArgumentName(parameterPosition, parameterName),
                                                            GetErrorTypeName(parameterType));

                                                        errors.Add(error);
                                                    }
                                                    break;
                                                }
                                                else if (parameterInfo.IsDefined(typeof(ParamArrayAttribute), false))
                                                {
                                                    //
                                                    // NOTE: Inside the loop, we need the type of the elements in the params array.
                                                    //
                                                    Type elementType = parameterType.GetElementType();

                                                    //
                                                    // NOTE: For simplicity, we will add each supplied argument to the params list
                                                    //       and convert the whole thing to an array at the end of the loop (when
                                                    //       we place the params array into the new arguments array at the current
                                                    //       argument index).
                                                    //
                                                    ObjectList @params = new ObjectList();

                                                    //
                                                    // NOTE: Save off the starting argument index because that is where we need to
                                                    //       place the params array.
                                                    //
                                                    int paramsIndex = argumentIndex; // save for below.

                                                    //
                                                    // NOTE: Use all the remaining arguments for this params array.  None of these
                                                    //       are allowed to be ByRef, therefore, we just need to perform type
                                                    //       coercion on the remaining argument strings (which will all end up being
                                                    //       of the same type).
                                                    //
                                                    for (; argumentIndex < argumentLength; argumentIndex++)
                                                    {
                                                        //
                                                        // NOTE: Start with their original argument value.
                                                        //
                                                        matchArgumentValue = args[argumentIndex];

                                                        if (FixupArgument(
                                                                interpreter, binder, options, cultureInfo,
                                                                elementType, ArgumentInfo.Create(argumentIndex,
                                                                    elementType, parameterName, true, false),
                                                                marshalFlags | perParameterMarshalFlags,
                                                                true, false, ref matchArgumentValue,
                                                                ref error) == ReturnCode.Ok)
                                                        {
                                                            @params.Add(matchArgumentValue);
                                                        }
                                                        else
                                                        {
                                                            if (errors == null)
                                                                errors = new ResultList();

                                                            if (verbose || (errors.Count == 0))
                                                            {
                                                                error = String.Format(
                                                                    "could not convert method {0} argument {1} with " +
                                                                    "value {2} to type {3}: {4}",
                                                                    FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                                    FormatOps.ArgumentName(parameterPosition, parameterName),
                                                                    FormatOps.WrapOrNull(matchArgumentValue),
                                                                    GetErrorTypeName(elementType), error);

                                                                errors.Add(error);
                                                            }
                                                            break;
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did we successfully complete the argument processing
                                                    //       loop above?  If so, we need to resize (expand or truncate)
                                                    //       the new arguments array because there may have been 0
                                                    //       supplied arguments or we may have just collapsed N supplied
                                                    //       arguments into 1 params argument.
                                                    //
                                                    if (argumentIndex >= argumentLength)
                                                    {
                                                        //
                                                        // NOTE: Make sure that the params array argument is the final
                                                        //       argument.  This includes forcing the outer for loop to
                                                        //       recognize that we have matched this method and are
                                                        //       totally done processing the supplied arguments by
                                                        //       reducing the supplied argument count and setting the
                                                        //       argument index to match.
                                                        //
                                                        argumentLength = paramsIndex + 1;
                                                        argumentIndex = argumentLength;

                                                        //
                                                        // NOTE: Physically resize the new arguments array to exactly
                                                        //       match the argument count; whether it is larger or
                                                        //       smaller does not matter.
                                                        //
                                                        Array.Resize(ref newArgs, argumentLength);

                                                        //
                                                        // NOTE: Store the params list into the corresponding new
                                                        //       argument slot.
                                                        //
                                                        newArgs[paramsIndex] = Array.CreateInstance(elementType,
                                                            @params.Count);

                                                        Array.Copy(@params.ToArray(), (Array)newArgs[paramsIndex],
                                                            @params.Count);
                                                    }

                                                    //
                                                    // NOTE: We just used up the all supplied arguments (or
                                                    //       we encountered an error that prevents us from
                                                    //       continuing to process this method as a match).
                                                    //
                                                    break;
                                                }
                                                else
                                                {
                                                    //
                                                    // FIXME: This is not 100% correct.  The .NET Framework and C#
                                                    //        are not consistent when it comes to setting the
                                                    //        parameter input and output properties.  More work
                                                    //        needs to be done to properly figure out the semantics
                                                    //        used by C#.
                                                    //
                                                    bool output = IsOutput(
                                                        parameterType, parameterInfo, marshalFlags |
                                                        perParameterMarshalFlags);

                                                    bool input = IsInput(
                                                        parameterType, parameterInfo, marshalFlags |
                                                        perParameterMarshalFlags);

                                                    bool optional = parameterInfo.IsOptional;
                                                    bool missing;

                                                    //
                                                    // NOTE: Set the value of this argument.
                                                    //
                                                    if (argumentIndex < argumentLength)
                                                    {
                                                        //
                                                        // NOTE: Use the supplied value.
                                                        //
                                                        matchArgumentValue = args[argumentIndex];

                                                        missing = false;
                                                    }
                                                    else if (optional)
                                                    {
                                                        //
                                                        // NOTE: Try to use the default value (optional argument).
                                                        //       This will be forbidden if the parameter type is
                                                        //       a value type and the default value is null.
                                                        //
                                                        matchArgumentValue = GetDefaultValue(
                                                            parameterInfo, parameterType);

                                                        missing = true;
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: Use the default value, which may be null.  If we get
                                                        //       to this point, that should mean that non-optional
                                                        //       arguments are present after optional ones; otherwise,
                                                        //       we would have enough of the supplied arguments to
                                                        //       satisfy the formal arguments.
                                                        //
                                                        matchArgumentValue = GetDefaultValue(
                                                            parameterType);

                                                        missing = true;
                                                    }

                                                    //
                                                    // NOTE: Set the argument value to convert.
                                                    //
                                                    newArgs[argumentIndex] = matchArgumentValue;

                                                    //
                                                    // NOTE: Attempt to convert the argument to the expected type.
                                                    //
                                                    if (FixupArgument(
                                                            interpreter, binder, options, cultureInfo,
                                                            parameterType, ArgumentInfo.Create(argumentIndex,
                                                                parameterType, parameterName, input, output),
                                                            marshalFlags | perParameterMarshalFlags, input,
                                                            output, ref newArgs[argumentIndex],
                                                            ref error) == ReturnCode.Ok)
                                                    {
                                                        //
                                                        // BUGFIX: If an optional output parameter is not present
                                                        //         (i.e. there is no variable name), the output
                                                        //         handling for it must be skipped.
                                                        //
                                                        if (!noByRefArguments && !missing && output)
                                                        {
                                                            if (matchArgumentValue is string)
                                                            {
                                                                if (methodArgumentInfoList == null)
                                                                    methodArgumentInfoList = new ArgumentInfoList();

                                                                //
                                                                // NOTE: Original args, not newArgs, because we need
                                                                //       the variable name, not the value.
                                                                //
                                                                methodArgumentInfoList.Add(ArgumentInfo.Create(
                                                                    argumentIndex, parameterType,
                                                                    (string)matchArgumentValue, input, output));
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: This is an output parameter and the argument
                                                                //       value provided is not a string (i.e. it cannot
                                                                //       be a variable name).
                                                                //
                                                                if (errors == null)
                                                                    errors = new ResultList();

                                                                if (verbose || (errors.Count == 0))
                                                                {
                                                                    error = String.Format(
                                                                        "could not convert method {0} argument {1} with " +
                                                                        "value {2} to type {3}: output variable " +
                                                                        "name must be a string", FormatOps.MethodOverload(
                                                                            matchIndex, fullObjectName, fullMethodName),
                                                                        FormatOps.ArgumentName(parameterPosition,
                                                                            parameterName),
                                                                        FormatOps.WrapOrNull(newArgs[argumentIndex]),
                                                                        GetErrorTypeName(parameterType));

                                                                    errors.Add(error);
                                                                }
                                                                break;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //
                                                        // NOTE: We could not convert the supplied argument to
                                                        //       the required type for this method.
                                                        //
                                                        if (errors == null)
                                                            errors = new ResultList();

                                                        if (verbose || (errors.Count == 0))
                                                        {
                                                            error = String.Format(
                                                                "could not convert method {0} argument {1} with " +
                                                                "value {2} to type {3}: {4}",
                                                                FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                                FormatOps.ArgumentName(parameterPosition, parameterName),
                                                                FormatOps.WrapOrNull(newArgs[argumentIndex]),
                                                                GetErrorTypeName(parameterType), error);

                                                            errors.Add(error);
                                                        }
                                                        break;
                                                    }

                                                    argumentIndex++;
                                                }
                                            }

                                            //
                                            // NOTE: Did we match up all arguments with their formal parameters?
                                            //
                                            if (argumentIndex == checkArgumentLength)
                                            {
                                                if (methodIndexList == null)
                                                    methodIndexList = new IntList();

                                                methodIndexList.Add(matchIndex);

                                                if (argsList == null)
                                                    argsList = new ObjectArrayList();

                                                argsList.Add(newArgs);

                                                if (methodArgumentInfoList != null)
                                                {
                                                    if (argumentInfoListDictionary == null)
                                                    {
                                                        argumentInfoListDictionary =
                                                            new IntArgumentInfoListDictionary();
                                                    }

                                                    argumentInfoListDictionary[matchIndex] =
                                                        methodArgumentInfoList;
                                                }

                                                if ((limit > 0) &&
                                                    (methodIndexList.Count >= limit))
                                                {
                                                    if (traceResults)
                                                    {
                                                        TraceMethodOverloads(
                                                            binder, cultureInfo, methods,
                                                            parameterMarshalFlags, argsList,
                                                            methodIndexList, marshalFlags);
                                                    }

                                                    return ReturnCode.Ok;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (errors == null)
                                                errors = new ResultList();

                                            if (verbose || (errors.Count == 0))
                                            {
                                                error = String.Format(
                                                    "cannot match parameter types with method {0}: {1}",
                                                    FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                    error);

                                                errors.Add(error);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        if (verbose || (errors.Count == 0))
                                        {
                                            //
                                            // NOTE: Create an appropriate error message based on how
                                            //       exactly the parameter do not match.
                                            //
                                            if (minimumCount > 0)
                                            {
                                                if (maximumCount != Count.Invalid)
                                                {
                                                    if (maximumCount != minimumCount)
                                                    {
                                                        error = String.Format(
                                                            "method {0} requires between {1} and {2} " +
                                                            "arguments and {3} {4} supplied",
                                                            FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                            minimumCount, maximumCount, argumentLength,
                                                            (argumentLength != 1) ? "were" : "was");
                                                    }
                                                    else
                                                    {
                                                        error = String.Format(
                                                            "method {0} requires exactly {1} " +
                                                            "{2} and {3} {4} supplied",
                                                            FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                            maximumCount, (maximumCount != 1) ? "arguments" : "argument",
                                                            argumentLength, (argumentLength != 1) ? "were" : "was");
                                                    }
                                                }
                                                else
                                                {
                                                    error = String.Format(
                                                        "method {0} requires at least {1} " +
                                                        "{2} and {3} {4} supplied",
                                                        FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                        minimumCount, (minimumCount != 1) ? "arguments" : "argument",
                                                        argumentLength, (argumentLength != 1) ? "were" : "was");
                                                }
                                            }
                                            else
                                            {
                                                if (maximumCount != Count.Invalid)
                                                {
                                                    error = String.Format(
                                                        "method {0} requires at most {1} " +
                                                        "{2} and {3} {4} supplied",
                                                        FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                        maximumCount, (maximumCount != 1) ? "arguments" : "argument",
                                                        argumentLength, (argumentLength != 1) ? "were" : "was");
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: We should not really get here.  That
                                                    //       being said, we still try to provide
                                                    //       a meaningful error message.
                                                    //
                                                    error = String.Format(
                                                        "method {0} does not take {1} {2}",
                                                        FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                                        argumentLength, (argumentLength != 1) ? "arguments" : "argument");
                                                }
                                            }

                                            errors.Add(error);
                                        }
                                    }
                                }
                                else
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    if (verbose || (errors.Count == 0))
                                    {
                                        error = String.Format(
                                            "cannot get parameter counts for method {0}: {1}",
                                            FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName),
                                            error);

                                        errors.Add(error);
                                    }
                                }
                            }
                            else
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                if (verbose || (errors.Count == 0))
                                {
                                    errors.Add(String.Format(
                                        "method {0} has no parameters",
                                        FormatOps.MethodOverload(matchIndex, fullObjectName, fullMethodName)));
                                }
                            }
                        }
                        else
                        {
                            if (methodIndexList == null)
                                methodIndexList = new IntList();

                            methodIndexList.Add(matchIndex);

                            if (argsList == null)
                                argsList = new ObjectArrayList();

                            argsList.Add(args);

                            if ((limit > 0) &&
                                (methodIndexList.Count >= limit))
                            {
                                if (traceResults)
                                {
                                    TraceMethodOverloads(
                                        binder, cultureInfo, methods,
                                        parameterMarshalFlags, argsList,
                                        methodIndexList, marshalFlags);
                                }

                                return ReturnCode.Ok;
                            }
                        }
                    }
                }

                //
                // NOTE: Did we find any matches?  If one or more matching
                //       methods were found, this is a success.  We can get
                //       here if we did not exceed the method limit supplied
                //       by the caller (or if there is no limit).
                //
                if ((methodIndexList != null) && (methodIndexList.Count > 0))
                {
                    if (traceResults)
                    {
                        TraceMethodOverloads(
                            binder, cultureInfo, methods,
                            parameterMarshalFlags, argsList,
                            methodIndexList, marshalFlags);
                    }

                    return ReturnCode.Ok;
                }

                //
                // NOTE: Did we even find a method with a matching name?
                //       This logic is designed purely to produce a slightly
                //       better error message.
                //
                if (matchMethodName == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    if (verbose || (errors.Count == 0))
                    {
                        errors.Add(String.Format(
                            "method {0} not found",
                            FormatOps.MethodOverload(Index.Invalid, fullObjectName, fullMethodName)));
                    }
                }
            }
            else
            {
                if (errors == null)
                    errors = new ResultList();

                if (verbose || (errors.Count == 0))
                    errors.Add("invalid method array");
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        [Obsolete()]
        private static ReturnCode OldFindMethodAndFixupArguments( /* NOT USED */
            Interpreter interpreter,                /* in */
            IBinder binder,                         /* in */
            OptionDictionary options,               /* in */
            CultureInfo cultureInfo,                /* in */
            Type type,                              /* in */
            string objectName,                      /* in */
            string fullObjectName,                  /* in */
            string methodName,                      /* in */
            string fullMethodName,                  /* in */
            MemberTypes memberTypes,                /* in */
            BindingFlags bindingFlags,              /* in */
            MethodBase[] methods,                   /* in */
            TypeList methodTypes,                   /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in */
            MarshalFlags marshalFlags,              /* in */
            ref object[] args,                      /* in, out */
            ref int methodIndex,                    /* out */
            ref ArgumentInfoList argumentInfoList,  /* out */
            ref Result error                        /* out */
            )
        {
            IntList methodIndexList = null;
            ObjectArrayList argsList = null;
            IntArgumentInfoListDictionary argumentInfoListDictionary = null;
            ResultList errors = null;

            if (FindMethodsAndFixupArguments(
                    interpreter, binder, options, cultureInfo, type, objectName,
                    fullObjectName, methodName, fullMethodName, memberTypes,
                    bindingFlags, methods, methodTypes, parameterTypes,
                    parameterMarshalFlags, args, 1 /* limit */, marshalFlags,
                    ref methodIndexList, ref argsList,
                    ref argumentInfoListDictionary, ref errors) == ReturnCode.Ok)
            {
                if ((methodIndexList != null) && (methodIndexList.Count > 0) &&
                    (argsList != null) && (argsList.Count > 0))
                {
                    //
                    // FIXME: Select the first method that matches.  Later,
                    //        more sophisticated logic may need to be added
                    //        here.
                    //
                    int localMethodIndex = methodIndexList[0];
                    object[] localArgs = argsList[0];
                    ArgumentInfoList localArgumentInfoList;

                    if (TryGetArgumentInfoList(
                            argumentInfoListDictionary, localMethodIndex,
                            out localArgumentInfoList))
                    {
                        methodIndex = localMethodIndex;
                        args = localArgs;
                        argumentInfoList = localArgumentInfoList;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add("missing first method byref arguments");
                    }
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("missing first method index or arguments in list");
                }
            }

            if (errors != null)
                error = errors;

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        [Obsolete()]
        private static ReturnCode AncientFindMethodAndFixupArguments( /* LEGACY */ /* NOT USED */
            Interpreter interpreter,                /* in */
            IBinder binder,                         /* in */
            OptionDictionary options,               /* in */
            CultureInfo cultureInfo,                /* in */
            Type type,                              /* in */
            string objectName,                      /* in */
            string methodName,                      /* in */
            MemberTypes memberTypes,                /* in */
            BindingFlags bindingFlags,              /* in */
            MethodBase[] methods,                   /* in */
            TypeList parameterTypes,                /* in */
            MarshalFlagsList parameterMarshalFlags, /* in */
            MarshalFlags marshalFlags,              /* in */
            ref object[] args,                      /* in, out */
            ref int methodIndex,                    /* out */
            ref ArgumentInfoList argumentInfoList,  /* in, out */
            ref Result error                        /* out */
            )
        {
            if (methods != null)
            {
                //
                // NOTE: At what method index, if any, did the method name and
                //       parameter info match (these are used to construct a more
                //       informative error message)?
                //
                string matchMethodName = null;

                //
                // NOTE: These variables are not always 100% accurate due to how
                //       we search among the overloads of a method.  They are
                //       provided to assist in constructing an error message on a
                //       "best effort" basis.  Another alternative is to keep a
                //       list of all errors encountered during each attempted
                //       "binding" to a particular overload of a method.
                //
                ParameterInfo[] matchParameterInfo = null; /* WARNING: Not 100% accurate. */
                int matchParameterIndex = Index.Invalid;   /* WARNING: Not 100% accurate. */
                int matchArgumentIndex = Index.Invalid;    /* WARNING: Not 100% accurate. */
                object matchArgumentValue = null;          /* WARNING: Not 100% accurate. */

                //
                // NOTE: Process each method info and attempt to match it against
                //       the supplied arguments taking into account the number and
                //       coerced types of the arguments, the (in, out) attributes,
                //       and the params argument, if any.
                //
                for (int matchIndex = 0; matchIndex < methods.Length; matchIndex++)
                {
                    //
                    // NOTE: Reset these error message helpers here because they
                    //       will no longer be accurate.
                    //
                    matchParameterInfo = null;

                    //
                    // NOTE: Get the method for the current method.
                    //
                    MethodBase method = methods[matchIndex];

                    if (method != null)
                    {
                        //
                        // NOTE: We need the name of the method to check.
                        //
                        string checkMethodName = methods[matchIndex].Name;

                        //
                        // NOTE: If the method name appears to be invalid, just skip it.
                        //
                        if (!String.IsNullOrEmpty(checkMethodName))
                        {
                            //
                            // NOTE: The method name must be matched first since we have
                            //       likely been passed all the methods for this type.
                            //
                            if (MatchMethodName(checkMethodName, methodName, memberTypes, bindingFlags))
                            {
                                //
                                // NOTE: Does the caller want to attempt to convert
                                //       arguments and filter on them?
                                //
                                if (args != null)
                                {
                                    //
                                    // NOTE: Set the name match index so that even if we ultimately
                                    //       do not find a matching method at least the error handling
                                    //       code knows that we got this far.
                                    //
                                    matchMethodName = checkMethodName;

                                    //
                                    // NOTE: We need the parameters of the method to check.
                                    //
                                    ParameterInfo[] checkParameterInfo = methods[matchIndex].GetParameters();

                                    //
                                    // NOTE: If the parameters appear to be invalid, just skip them.
                                    //
                                    if (checkParameterInfo != null)
                                    {
                                        int minimumCount = 0;
                                        int maximumCount = 0;

                                        if (GetParameterCounts(checkParameterInfo,
                                                ref minimumCount, ref maximumCount, ref error) == ReturnCode.Ok)
                                        {
                                            //
                                            // NOTE: Get the number of arguments supplied by the caller.
                                            //
                                            int argumentLength = args.Length;

                                            //
                                            // NOTE: Does the number of arguments supplied by the caller fit
                                            //       within the minimum and maximum number of arguments for
                                            //       this method?
                                            //
                                            if (((minimumCount == Count.Invalid) || (argumentLength >= minimumCount)) &&
                                                ((maximumCount == Count.Invalid) || (argumentLength <= maximumCount)))
                                            {
                                                //
                                                // NOTE: Check for any parameter type hints provided by the caller.
                                                //       If they were supplied then they must match up with the
                                                //       parameter types for this method.
                                                //
                                                if ((parameterTypes == null) || MatchParameterTypes(
                                                        parameterTypes, checkParameterInfo, marshalFlags,
                                                        true, true, ref error))
                                                {
                                                    //
                                                    // NOTE: Set the matching parameter info so that even if we
                                                    //       ultimately do not find a matching method at least the
                                                    //       error handling code knows that we got this far.
                                                    //
                                                    matchParameterInfo = checkParameterInfo;

                                                    //
                                                    // NOTE: Get the number of formal arguments for the method to
                                                    //       check.
                                                    //
                                                    int checkArgumentLength = checkParameterInfo.Length;

                                                    //
                                                    // NOTE: Create a temporary holding area for attempted argument
                                                    //       type conversions because in the case we do not fully
                                                    //       match the method we do not want to fiddle with the
                                                    //       caller's original arguments array.
                                                    //
                                                    object[] newArgs = new object[checkArgumentLength];

                                                    //
                                                    // NOTE: Start consuming the original arguments at the first
                                                    //       argument.
                                                    //
                                                    int argumentIndex = 0;

                                                    //
                                                    // NOTE: Reset these error message helpers here because they
                                                    //       will no longer be accurate.
                                                    //
                                                    matchParameterIndex = Index.Invalid;
                                                    matchArgumentIndex = Index.Invalid;
                                                    matchArgumentValue = null;

                                                    //
                                                    // NOTE: Process each formal argument, checking the various
                                                    //       constraints imposed by it upon the supplied argument
                                                    //       value.
                                                    //
                                                    for (int parameterIndex = 0; parameterIndex < checkArgumentLength; parameterIndex++)
                                                    {
                                                        //
                                                        // NOTE: Set the matching parameter and argument indexes so that
                                                        //       even if we ultimately do not find a matching method at
                                                        //       least the error handling code knows that we got this far.
                                                        //
                                                        matchParameterIndex = parameterIndex;
                                                        matchArgumentIndex = argumentIndex;

                                                        //
                                                        // NOTE: Check argument attributes to see if they are ref (in, out) or out (out).
                                                        //       Enforce argument being an existing script variable name for ref.
                                                        //       Enforce argument being a script variable name for out.
                                                        //
                                                        //       Also, arrays need to be handled in concert with (in, out).  Script arrays
                                                        //       will be used and the indexes will always be in the form (x) or (x,y,z)
                                                        //       where x is an integer index into a single or multi-dimensional array, and
                                                        //       y and z, etc are integer indexes of a multi-dimensional array.
                                                        //
                                                        ParameterInfo parameterInfo = checkParameterInfo[parameterIndex];

                                                        if (parameterInfo == null)
                                                            break; // BUGBUG: Better error message here.

                                                        Type parameterType = parameterInfo.ParameterType;
                                                        string parameterName = parameterInfo.Name;

                                                        //
                                                        // NOTE: Check if this parameter is of a pointer type.
                                                        //
                                                        if (parameterType.IsPointer)
                                                        {
                                                            //
                                                            // NOTE: We do not allow and cannot process pointer types.
                                                            //
                                                            break; // BUGBUG: Better error message here.
                                                        }
                                                        else if (parameterInfo.IsDefined(typeof(ParamArrayAttribute), false))
                                                        {
                                                            //
                                                            // NOTE: Inside the loop, we need the type of the elements in the params array.
                                                            //
                                                            Type elementType = parameterType.GetElementType();

                                                            //
                                                            // NOTE: For simplicity, we will add each supplied argument to the params list
                                                            //       and convert the whole thing to an array at the end of the loop (when
                                                            //       we place the params array into the new arguments array at the current
                                                            //       argument index).
                                                            //
                                                            ObjectList @params = new ObjectList();

                                                            //
                                                            // NOTE: Save off the starting argument index because that is where we need to
                                                            //       place the params array.
                                                            //
                                                            int paramsIndex = argumentIndex; // save for below.

                                                            //
                                                            // NOTE: Use all the remaining arguments for this params array.  None of these
                                                            //       are allowed to be ByRef, therefore, we just need to perform type
                                                            //       coercion on the remaining argument strings (which will all end up being
                                                            //       of the same type).
                                                            //
                                                            for (; argumentIndex < argumentLength; argumentIndex++)
                                                            {
                                                                //
                                                                // NOTE: Start with their original argument value.
                                                                //
                                                                matchArgumentValue = args[argumentIndex];

                                                                if (FixupArgument(
                                                                        interpreter, binder, options, cultureInfo,
                                                                        elementType, ArgumentInfo.Create(argumentIndex,
                                                                            elementType, parameterName, true, false),
                                                                        marshalFlags, true, false, ref matchArgumentValue,
                                                                        ref error) == ReturnCode.Ok)
                                                                {
                                                                    @params.Add(matchArgumentValue);
                                                                }
                                                                else
                                                                {
                                                                    break;
                                                                }
                                                            }

                                                            //
                                                            // NOTE: Did we successfully complete the argument processing
                                                            //       loop above?  If so, we need to resize (expand or truncate)
                                                            //       the new arguments array because there may have been 0
                                                            //       supplied arguments or we may have just collapsed N supplied
                                                            //       arguments into 1 params argument.
                                                            //
                                                            if (argumentIndex >= argumentLength)
                                                            {
                                                                //
                                                                // NOTE: Make sure that the params array argument is the final
                                                                //       argument.  This includes forcing the outer for loop to
                                                                //       recognize that we have matched this method and are
                                                                //       totally done processing the supplied arguments by
                                                                //       reducing the supplied argument count and setting the
                                                                //       argument index to match.
                                                                //
                                                                argumentLength = paramsIndex + 1;
                                                                argumentIndex = argumentLength;

                                                                //
                                                                // NOTE: Physically resize the new arguments array to exactly
                                                                //       match the argument count; whether it is larger or
                                                                //       smaller does not matter.
                                                                //
                                                                Array.Resize(ref newArgs, argumentLength);

                                                                //
                                                                // NOTE: Store the params list into the corresponding new
                                                                //       argument slot.
                                                                //
                                                                newArgs[paramsIndex] = Array.CreateInstance(elementType,
                                                                    @params.Count);

                                                                Array.Copy(@params.ToArray(), (Array)newArgs[paramsIndex],
                                                                    @params.Count);
                                                            }

                                                            //
                                                            // NOTE: We just used up the all supplied arguments (or
                                                            //       we encountered an error that prevents us from
                                                            //       continuing to process this method as a match).
                                                            //
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            //
                                                            // FIXME: This is not 100% correct.  The .NET Framework and C#
                                                            //        are not consistent when it comes to setting the
                                                            //        parameter input and output properties.  More work
                                                            //        needs to be done to properly figure out the semantics
                                                            //        used by C#.
                                                            //
                                                            bool output = IsOutput(parameterType, parameterInfo, marshalFlags);
                                                            bool input = IsInput(parameterType, parameterInfo, marshalFlags);
                                                            bool optional = parameterInfo.IsOptional;
                                                            bool missing;

                                                            //
                                                            // NOTE: Set the value of this argument.
                                                            //
                                                            if (argumentIndex < argumentLength)
                                                            {
                                                                //
                                                                // NOTE: Use the supplied value.
                                                                //
                                                                matchArgumentValue = args[argumentIndex];

                                                                missing = false;
                                                            }
                                                            else if (optional)
                                                            {
                                                                //
                                                                // NOTE: Try to use the default value (optional argument).
                                                                //       This will be forbidden if the parameter type is
                                                                //       a value type and the default value is null.
                                                                //
                                                                matchArgumentValue = GetDefaultValue(
                                                                    parameterInfo, parameterType);

                                                                missing = true;
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: Use the default value, which may be null.  If we get
                                                                //       to this point, that should mean that non-optional
                                                                //       arguments are present after optional ones; otherwise,
                                                                //       we would have enough of the supplied arguments to
                                                                //       satisfy the formal arguments.
                                                                //
                                                                matchArgumentValue = GetDefaultValue(
                                                                    parameterType);

                                                                missing = true;
                                                            }

                                                            //
                                                            // NOTE: Set the argument value to convert.
                                                            //
                                                            newArgs[argumentIndex] = matchArgumentValue;

                                                            //
                                                            // NOTE: Attempt to convert the argument to the expected type.
                                                            //
                                                            if (FixupArgument(
                                                                    interpreter, binder, options, cultureInfo,
                                                                    parameterType, ArgumentInfo.Create(argumentIndex,
                                                                        parameterType, parameterName, input, output),
                                                                    marshalFlags, input, output, ref newArgs[argumentIndex],
                                                                    ref error) == ReturnCode.Ok)
                                                            {
                                                                //
                                                                // BUGFIX: If an optional output parameter is not present
                                                                //         (i.e. there is no variable name), the output
                                                                //         handling for it must be skipped.
                                                                //
                                                                if (!missing && output)
                                                                {
                                                                    if (matchArgumentValue is string)
                                                                    {
                                                                        if (argumentInfoList == null)
                                                                            argumentInfoList = new ArgumentInfoList();

                                                                        //
                                                                        // NOTE: Original args, not newArgs, because we need the
                                                                        //       variable name, not the value.
                                                                        //
                                                                        argumentInfoList.Add(ArgumentInfo.Create(
                                                                            argumentIndex, parameterType,
                                                                            (string)matchArgumentValue, input, output));
                                                                    }
                                                                    else
                                                                    {
                                                                        //
                                                                        // NOTE: This is an output parameter and the argument
                                                                        //       value provided is not a string (i.e. it cannot
                                                                        //       be a variable name).
                                                                        //
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                //
                                                                // NOTE: We could not convert the supplied argument to
                                                                //       the required type for this method.
                                                                //
                                                                break;
                                                            }

                                                            argumentIndex++;
                                                        }
                                                    }

                                                    //
                                                    // NOTE: Did we match up all arguments with their formal parameters?
                                                    //
                                                    if (argumentIndex == checkArgumentLength)
                                                    {
                                                        //
                                                        // NOTE: Commit changes to caller's original arguments.
                                                        //
                                                        Array.Resize(ref args, checkArgumentLength); /* NOTE: Might be NOP. */
                                                        Array.Copy(newArgs, args, checkArgumentLength);

                                                        methodIndex = matchIndex;
                                                        return ReturnCode.Ok;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //
                                    // FIXME: Select the first method that matches the name (and
                                    //        the binding flags because the caller should have
                                    //        already performed that filtering step).  More
                                    //        sophisticated logic may need to be added here later.
                                    //
                                    methodIndex = matchIndex;
                                    return ReturnCode.Ok;
                                }
                            }
                        }
                    }
                }

                //
                // NOTE: Did we even find a method with a matching name
                //       and argument count?
                //
                if ((matchParameterInfo != null) &&
                    (matchParameterIndex != Index.Invalid) &&
                    (matchArgumentIndex != Index.Invalid))
                {
                    //
                    // WARNING: The argument value and parameter type are produced on a
                    //          "best effort" basis here because during our search we may
                    //          have overwritten them when we should not have (i.e. for a
                    //          subsequent method overload that had a matching number of
                    //          arguments).
                    //
                    error = String.Format(
                        "could not convert method {0} argument value {1} to type {2}: {3}",
                        FormatOps.MethodOverload(Index.Invalid, objectName, methodName),
                        FormatOps.WrapOrNull(matchArgumentValue),
                        GetErrorTypeName(matchParameterInfo[matchParameterIndex].ParameterType),
                        error);
                }
                else if (matchMethodName != null)
                {
                    error = String.Format(
                        "no method {0} matches arguments: {1}",
                        FormatOps.MethodOverload(Index.Invalid, objectName, methodName),
                        error);
                }
                else
                {
                    error = String.Format(
                        "method {0} not found",
                        FormatOps.MethodOverload(Index.Invalid, objectName, methodName));
                }
            }
            else
            {
                error = "invalid method array";
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetTypeListFromParameterInfo(
            ParameterInfo[] parameterInfo,
            bool strict,
            ref TypeList types,
            ref Result error
            )
        {
            if (parameterInfo != null)
            {
                int parameterLength = parameterInfo.Length;

                if (!strict || (parameterLength > 0))
                {
                    if (types == null)
                        types = new TypeList();

                    //
                    // NOTE: Iterate over all the parameters, adding the type
                    //       of the parameter to the resulting list.
                    //
                    for (int parameterIndex = 0; parameterIndex < parameterLength; parameterIndex++)
                    {
                        //
                        // NOTE: Just skip over invalid array entries.
                        //
                        if (parameterInfo[parameterIndex] != null)
                            types.Add(parameterInfo[parameterIndex].ParameterType);
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "no parameters to process";
                }
            }
            else
            {
                error = "invalid parameter info";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool ComSucceeded(
            int hResult
            )
        {
            return (hResult >= 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static EventAttributes? GetEventAttributes(
            MemberInfo memberInfo
            )
        {
            return (memberInfo is EventInfo) ?
                ((EventInfo)memberInfo).Attributes : (EventAttributes?)null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static FieldAttributes? GetFieldAttributes(
            MemberInfo memberInfo
            )
        {
            return (memberInfo is FieldInfo) ?
                ((FieldInfo)memberInfo).Attributes : (FieldAttributes?)null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetParameterName(
            int index,
            Type type
            )
        {
            return (type != null) ?
                String.Format("{0}{1}_{2}", ParameterNamePrefix, index, type.Name) :
                String.Format("{0}{1}", ParameterNamePrefix, index);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object GetDefaultValue(
            Type type
            )
        {
            return ((type != null) && type.IsValueType) ?
                Activator.CreateInstance(type) : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static object GetDefaultValue(
            ParameterInfo parameterInfo,
            Type parameterType
            )
        {
            object result;

            if (parameterInfo != null)
            {
                //
                // NOTE: First, try to use the declared default value for
                //       the parameter.
                //
                result = parameterInfo.DefaultValue;

                //
                // BUGFIX: If the parameter type is a value type, forbid
                //         using null as the default value.  There are
                //         some assemblies "in the wild" that appear to
                //         use null as the default parameter value for
                //         some of their value type parameters.  This is
                //         clearly not allowed by the spec (?).
                //
                if ((result == null) &&
                    (parameterType != null) && parameterType.IsValueType)
                {
                    result = GetDefaultValue(parameterType);
                }
            }
            else
            {
                result = GetDefaultValue(parameterType);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool DoesComObjectSupportInterface(
            IntPtr unknown,
            Type type
            )
        {
            if ((unknown != IntPtr.Zero) && (type != null))
            {
                IntPtr @interface = IntPtr.Zero;

                try
                {
                    Guid iid = type.GUID;

                    int result = Marshal.QueryInterface(unknown, ref iid,
                        out @interface);

                    if (ComSucceeded(result) &&
                        (@interface != IntPtr.Zero) && iid.Equals(type.GUID))
                    {
#if DEBUG && VERBOSE
                        TraceOps.DebugTrace(String.Format(
                            "DoesComObjectSupportInterface: succeeded, " +
                            "unknown = {0}, iid = {1}, type = {2}, result = " +
                            "{3}", unknown, iid, FormatOps.WrapOrNull(type),
                            result), typeof(MarshalOps).Name,
                            TracePriority.MarshalDebug);
#endif

                        return true;
                    }
                }
                finally
                {
                    if (@interface != IntPtr.Zero)
                    {
                        Marshal.Release(@interface);
                        @interface = IntPtr.Zero;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetTypeListFromComObject(
            Interpreter interpreter,
            string text,
            string part,
            object @object,
            TypePairDictionary<string, long> interfaces,
            CultureInfo cultureInfo,
            ref TypeList types,
            ref Result error
            )
        {
            if (interpreter != null)
            {
                if (@object != null)
                {
                    if (interfaces != null)
                    {
#if NATIVE
                        string typeName1;
                        string typeName2;

                        /* IGNORED */
                        GetTypeNamesFromComObject(
                            @object, cultureInfo, out typeName1, out typeName2);
#endif

                        IntPtr unknown = IntPtr.Zero;

                        try
                        {
                            unknown = Marshal.GetIUnknownForObject(@object);

                            if (unknown != IntPtr.Zero)
                            {
#if COM_TYPE_CACHE
                                TypeList cacheTypes = null;

                                if (interpreter.GetCachedComTypeList(
                                        unknown, ref cacheTypes))
                                {
                                    foreach (Type cacheType in cacheTypes)
                                    {
                                        if (!interfaces.ContainsKey(cacheType) ||
                                            !DoesComObjectSupportInterface(unknown, cacheType))
                                        {
                                            //
                                            // NOTE: The cached type list is probably bogus,
                                            //       remove it now and continue with the
                                            //       normal lookup method.
                                            //
                                            /* IGNORED */
                                            interpreter.RemoveCachedComTypeList(unknown);

                                            goto notCached;
                                        }
                                    }

                                    types = cacheTypes;
                                    return ReturnCode.Ok;
                                }

                            notCached:
#endif

                                SortedDictionary<long, Type> sortedTypes =
                                    new SortedDictionary<long, Type>();

                                foreach (KeyValuePair<Type, IAnyPair<string, long>> pair
                                        in interfaces)
                                {
                                    Type type = pair.Key;

                                    if ((type != null) && type.IsInterface)
                                    {
                                        IAnyPair<string, long> anyPair = pair.Value;

                                        if (anyPair != null)
                                        {
                                            //
                                            // NOTE: Check if the COM object actually supports
                                            //       this interface.  Also, if possible, compare
                                            //       the short name of the type against the one
                                            //       returned by the object itself via
                                            //       IProvideClassInfo or IDispatch.  This seems
                                            //       to improve the matching when a COM object
                                            //       implements more than one interface.
                                            //
                                            if (DoesComObjectSupportInterface(unknown, type) &&
#if NATIVE
                                                (((typeName1 == null) ||
                                                String.Equals(type.Name, typeName1,
                                                    StringOps.SystemStringComparisonType)) ||
                                                ((typeName2 == null) ||
                                                String.Equals(type.Name, typeName2,
                                                    StringOps.SystemStringComparisonType)))
#else
                                                true
#endif
                                                )
                                            {
                                                sortedTypes.Add(anyPair.Y, type);
                                            }
                                        }
                                    }
                                }

                                if (sortedTypes.Count > 0)
                                {
                                    if (types == null)
                                        types = new TypeList();

                                    types.AddRange(sortedTypes.Values);

#if COM_TYPE_CACHE
                                    /* IGNORED */
                                    interpreter.AddCachedComTypeList(unknown, types);
#endif

                                    return ReturnCode.Ok;
                                }

                                TraceOps.DebugTrace(String.Format(
                                    "GetTypeListFromComObject: interface type for " +
                                    "COM object {0} part {1} ({2}) not found",
                                    FormatOps.WrapOrNull(text),
                                    FormatOps.WrapOrNull(part),
                                    unknown), typeof(MarshalOps).Name,
                                    TracePriority.MarshalError);

                                error = "type not found";
                            }
                            else
                            {
                                error = "invalid COM object";
                            }
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                        finally
                        {
                            if (unknown != IntPtr.Zero)
                            {
                                Marshal.Release(unknown);
                                unknown = IntPtr.Zero;
                            }
                        }
                    }
                    else
                    {
                        error = "invalid interface list";
                    }
                }
                else
                {
                    error = "invalid object";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        private static bool GetTypeInfoFromComObject(
            object @object,
            CultureInfo cultureInfo,
            out ITypeInfo typeInfo1,
            out ITypeInfo typeInfo2
            )
        {
            typeInfo1 = null;
            typeInfo2 = null;

            if (@object != null)
            {
                try
                {
                    UnsafeNativeMethods.IProvideClassInfo provideClassInfo =
                        @object as UnsafeNativeMethods.IProvideClassInfo;

                    if (provideClassInfo != null)
                        typeInfo1 = provideClassInfo.GetClassInfo();
                }
                catch
                {
                    // do nothing.
                }

                try
                {
                    UnsafeNativeMethods.IDispatch dispatch =
                        @object as UnsafeNativeMethods.IDispatch;

                    if (dispatch != null)
                        typeInfo2 = dispatch.GetTypeInfo(0,
                            (cultureInfo != null) ? cultureInfo.LCID : 0);
                }
                catch
                {
                    // do nothing.
                }

                return (typeInfo1 != null) || (typeInfo2 != null);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetTypeNamesFromComObject(
            object @object,
            CultureInfo cultureInfo,
            out string typeName1,
            out string typeName2
            )
        {
            typeName1 = null;
            typeName2 = null;

            if (@object != null)
            {
                ITypeInfo typeInfo1 = null;
                ITypeInfo typeInfo2 = null;

                try
                {
                    GetTypeInfoFromComObject(
                        @object, cultureInfo, out typeInfo1, out typeInfo2);

                    if (typeInfo1 != null)
                    {
                        try
                        {
                            typeName1 = Marshal.GetTypeInfoName(typeInfo1);
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }

                    if (typeInfo2 != null)
                    {
                        try
                        {
                            typeName2 = Marshal.GetTypeInfoName(typeInfo2);
                        }
                        catch
                        {
                            // do nothing.
                        }
                    }

#if DEBUG && VERBOSE
                    TraceOps.DebugTrace(String.Format(
                        "GetTypeNamesFromComObject: object = {0}, " +
                        "cultureInfo = {1}, typeName1 = {2}, typeName2 = {3}",
                        FormatOps.WrapOrNull(@object),
                        FormatOps.WrapOrNull(cultureInfo),
                        FormatOps.WrapOrNull(typeName1),
                        FormatOps.WrapOrNull(typeName2)),
                        typeof(MarshalOps).Name, TracePriority.MarshalDebug);
#endif

                    return (typeName1 != null) || (typeName2 != null);
                }
                finally
                {
                    if (typeInfo2 != null)
                    {
                        Marshal.ReleaseComObject(typeInfo2);
                        typeInfo2 = null;
                    }

                    if (typeInfo1 != null)
                    {
                        Marshal.ReleaseComObject(typeInfo1);
                        typeInfo1 = null;
                    }
                }
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Type GetTypeFromComObject(
            Interpreter interpreter,
            string text,
            string part,
            object @object,
            TypePairDictionary<string, long> interfaces,
            IBinder binder,
            CultureInfo cultureInfo,
            ObjectFlags objectFlags,
            ref Result error
            )
        {
            TypeList types = null;

            if (GetTypeListFromComObject(
                    interpreter, text, part, @object, interfaces,
                    cultureInfo, ref types, ref error) == ReturnCode.Ok)
            {
                if (types != null)
                {
                    if (types.Count > 0)
                    {
                        bool ambiguous = false;

                        if (types.Count > 1)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "GetTypeFromComObject: interface type for COM " +
                                "object {0} part {1} is ambiguous between: {2}",
                                FormatOps.WrapOrNull(text),
                                FormatOps.WrapOrNull(part),
                                FormatOps.WrapOrNull(types)),
                                typeof(MarshalOps).Name,
                                TracePriority.MarshalError);

                            ambiguous = true;
                        }

                        Type type = types[0];

                        if (binder != null)
                        {
                            IScriptBinder scriptBinder = binder as IScriptBinder;

                            if (scriptBinder != null)
                            {
                                ReturnCode code = scriptBinder.SelectType(
                                    interpreter, text, @object, types,
                                    cultureInfo, objectFlags, ref type,
                                    ref error);

                                //
                                // NOTE: *HACK*: In future, break out of
                                //       the type selection loop, should
                                //       there be one.
                                //
                                if (code == ReturnCode.Break)
                                    code = ReturnCode.Ok;

                                if (code == ReturnCode.Ok)
                                    return type;

                                if (code == ReturnCode.Error)
                                    return null;
                            }
                        }

                        if (ambiguous)
                        {
                            TraceOps.DebugTrace(String.Format(
                                "GetTypeFromComObject: interface type for COM " +
                                "object {0} part {1} was resolved to: {2}",
                                FormatOps.WrapOrNull(text),
                                FormatOps.WrapOrNull(part),
                                FormatOps.WrapOrNull(type)),
                                typeof(MarshalOps).Name,
                                TracePriority.MarshalDebug);
                        }

                        return type;
                    }
                    else
                    {
                        error = "type not found";
                    }
                }
                else
                {
                    error = "invalid type list";
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static MethodAttributes? GetMethodAttributes(
            MemberInfo memberInfo
            )
        {
            return (memberInfo is MethodBase) ?
                ((MethodBase)memberInfo).Attributes : (MethodAttributes?)null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static PropertyAttributes? GetPropertyAttributes(
            MemberInfo memberInfo
            )
        {
            return (memberInfo is PropertyInfo) ?
                ((PropertyInfo)memberInfo).Attributes : (PropertyAttributes?)null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static TypeAttributes? GetTypeAttributes(
            MemberInfo memberInfo
            )
        {
            return (memberInfo is Type) ?
                ((Type)memberInfo).Attributes : (TypeAttributes?)null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ParameterInfo GetReturnParameterInfo(
            MethodBase methodBase
            )
        {
            return (methodBase is MethodInfo) ? ((MethodInfo)methodBase).ReturnParameter : null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMethodBaseFromMemberInfo(
            MemberInfo[] memberInfo,
            BindingFlags bindingFlags,
            ref MethodBase[] methodBase,
            ref Result error
            )
        {
            if (memberInfo != null)
            {
                int memberLength = memberInfo.Length;

                if (memberLength > 0)
                {
                    //
                    // NOTE: We have no idea how many methods we will find,
                    //       allocate an empty list and keep adding to it.
                    //
                    MethodBaseList methodBaseList = new MethodBaseList();

                    //
                    // NOTE: Iterate over all the members, adding the methods
                    //       we find verbatim and flattening the get and set
                    //       accessor methods for properties into the list
                    //       as well.
                    //
                    for (int memberIndex = 0; memberIndex < memberLength; memberIndex++)
                    {
                        //
                        // NOTE: Just skip over invalid array entries.
                        //
                        if (memberInfo[memberIndex] != null)
                        {
                            //
                            // NOTE: Is this member a method base (a ConstructorInfo,
                            //       MethodInfo, etc)?
                            //
                            if (memberInfo[memberIndex] is MethodBase)
                            {
                                methodBaseList.Add((MethodBase)memberInfo[memberIndex]);
                            }
                            //
                            // NOTE: Is this member an event?  If so, lookup all the
                            //       methods associated with it.
                            //
                            else if (memberInfo[memberIndex] is EventInfo)
                            {
                                MethodInfo[] methodInfo = null;

                                if (GetMethodInfoFromEventInfo(
                                        new EventInfo[] { (EventInfo)memberInfo[memberIndex] },
                                        bindingFlags, ref methodInfo, ref error) == ReturnCode.Ok)
                                {
                                    int methodLength = methodInfo.Length;

                                    for (int methodIndex = 0; methodIndex < methodLength; methodIndex++)
                                        if (methodInfo[methodIndex] != null)
                                            methodBaseList.Add(methodInfo[methodIndex]);
                                }
                                else
                                {
                                    return ReturnCode.Error;
                                }
                            }
                            //
                            // NOTE: Is this member a property?  If so, lookup the
                            //       accessor methods.
                            //
                            else if (memberInfo[memberIndex] is PropertyInfo)
                            {
                                MethodInfo[] methodInfo = null;

                                if (GetMethodInfoFromPropertyInfo(
                                        new PropertyInfo[] { (PropertyInfo)memberInfo[memberIndex] },
                                        bindingFlags, ref methodInfo, ref error) == ReturnCode.Ok)
                                {
                                    int methodLength = methodInfo.Length;

                                    for (int methodIndex = 0; methodIndex < methodLength; methodIndex++)
                                        if (methodInfo[methodIndex] != null)
                                            methodBaseList.Add(methodInfo[methodIndex]);
                                }
                                else
                                {
                                    return ReturnCode.Error;
                                }
                            }
                        }
                    }

                    //
                    // NOTE: Now that we have totally succeeded, place the
                    //       result into the caller's array.
                    //
                    methodBase = methodBaseList.ToArray();

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "no members to process";
                }
            }
            else
            {
                error = "invalid member info";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMethodInfoFromEventInfo(
            EventInfo[] eventInfo,
            BindingFlags bindingFlags,
            ref MethodInfo[] methodInfo,
            ref Result error
            )
        {
            if (eventInfo != null)
            {
                int eventLength = eventInfo.Length;

                if (eventLength > 0)
                {
                    //
                    // NOTE: Do we want to include non-public methods?
                    //
                    bool nonPublic =
                        FlagOps.HasFlags(bindingFlags, BindingFlags.NonPublic, true);

                    //
                    // NOTE: We have no idea how many methods we will find,
                    //       allocate an empty list and keep adding to it.
                    //
                    MethodInfoList methodInfoList = new MethodInfoList();

                    //
                    // NOTE: Iterate over all the properties, flattening the get and
                    //       set accessor methods into one method array.
                    //
                    for (int eventIndex = 0; eventIndex < eventLength; eventIndex++)
                    {
                        //
                        // NOTE: Just skip over invalid array entries.
                        //
                        if (eventInfo[eventIndex] != null)
                        {
                            //
                            // NOTE: Add the method used to add an event handler
                            //       delegate to this event source.
                            //
                            methodInfoList.Add(
                                eventInfo[eventIndex].GetAddMethod(nonPublic));

                            //
                            // NOTE: Add the methods that were associated with this
                            //       event in MSIL using the .other directive.
                            //
                            methodInfoList.AddRange(
                                eventInfo[eventIndex].GetOtherMethods(nonPublic));

                            //
                            // NOTE: Add the method that is called when this event is
                            //       raised.
                            //
                            methodInfoList.Add(
                                eventInfo[eventIndex].GetRaiseMethod(nonPublic));

                            //
                            // NOTE: Add the method used to remove an event handler
                            //       delegate from this event source.
                            //
                            methodInfoList.Add(
                                eventInfo[eventIndex].GetRemoveMethod(nonPublic));
                        }
                    }

                    //
                    // NOTE: Now that we have totally succeeded, place the
                    //       result into the caller's array.
                    //
                    methodInfo = methodInfoList.ToArray();

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "no events to process";
                }
            }
            else
            {
                error = "invalid event info";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMethodInfoFromPropertyInfo(
            PropertyInfo[] propertyInfo,
            BindingFlags bindingFlags,
            ref MethodInfo[] methodInfo,
            ref Result error
            )
        {
            if (propertyInfo != null)
            {
                int propertyLength = propertyInfo.Length;

                if (propertyLength > 0)
                {
                    //
                    // NOTE: Do we want to include non-public methods?
                    //
                    bool nonPublic =
                        FlagOps.HasFlags(bindingFlags, BindingFlags.NonPublic, true);

                    //
                    // NOTE: Allocate enough space to hold get and set methods
                    //       for every property.
                    //
                    methodInfo = new MethodInfo[propertyLength * 2];

                    //
                    // NOTE: Iterate over all the properties, flattening the get and
                    //       set accessor methods into one method array.
                    //
                    for (int propertyIndex = 0; propertyIndex < propertyLength; propertyIndex++)
                    {
                        //
                        // NOTE: Just skip over invalid array entries.
                        //
                        if (propertyInfo[propertyIndex] != null)
                        {
                            //
                            // NOTE: Populate the get method for this property, this
                            //       may end up being null; however, we do not care
                            //       about that at this point.
                            //
                            methodInfo[propertyIndex * 2] =
                                propertyInfo[propertyIndex].GetGetMethod(nonPublic);

                            //
                            // NOTE: Populate the set method for this property, this
                            //       may end up being null; however, we do not care
                            //       about that at this point.
                            //
                            methodInfo[(propertyIndex * 2) + 1] =
                                propertyInfo[propertyIndex].GetSetMethod(nonPublic);
                        }
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "no properties to process";
                }
            }
            else
            {
                error = "invalid property info";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool MatchMethodName(
            string checkMethodName,
            string methodName,
            MemberTypes memberTypes,
            BindingFlags bindingFlags
            )
        {
            if (String.IsNullOrEmpty(methodName))
            {
                //
                // NOTE: Match anything if they are checking for a null
                //       or empty member name.
                //
                return true;
            }
            else
            {
                if (!String.IsNullOrEmpty(checkMethodName))
                {
                    //
                    // NOTE: If the binding flags say to ignore case, then we will.
                    //
                    StringComparison comparisonType =
                        FlagOps.HasFlags(bindingFlags, BindingFlags.IgnoreCase, true) ?
                            StringOps.UserNoCaseStringComparisonType :
                            StringOps.UserStringComparisonType;

                    //
                    // NOTE: Attempt an exact match first.
                    //
                    if (String.Compare(checkMethodName, methodName, comparisonType) == 0)
                    {
                        //
                        // NOTE: Exact match of the member name.
                        //
                        return true;
                    }
                    else if (FlagOps.HasFlags(memberTypes, MemberTypes.Property, true) &&
                        (AccessorPrefixes != null))
                    {
                        //
                        // HACK: For properties only, we allow the removal of the leading
                        //       "get_" and "set_" from the member name so that we can
                        //       properly match the accessors (without knowing in advance
                        //       whether we actually want the get or set accessor).
                        //
                        // BUGFIX: We want to be sure and use the correct comparison
                        //         length here.
                        //
                        foreach (string accessorPrefix in AccessorPrefixes)
                        {
                            if (accessorPrefix == null)
                                continue;

                            if (!checkMethodName.StartsWith(
                                    accessorPrefix, comparisonType))
                            {
                                continue;
                            }

                            int accessorPrefixLength = accessorPrefix.Length;

                            int maximumLength = Math.Max(
                                checkMethodName.Length - accessorPrefixLength,
                                methodName.Length);

                            if (String.Compare(checkMethodName, accessorPrefixLength,
                                    methodName, 0, maximumLength, comparisonType) == 0)
                            {
                                //
                                // NOTE: Exact match of the member name without the
                                //       get/set accessor prefix.
                                //
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode GetParameterCounts(
            ParameterInfo[] parameterInfo,
            ref int minimumCount,
            ref int maximumCount,
            ref Result error
            )
        {
            if (parameterInfo == null)
            {
                error = "invalid parameter info";
                return ReturnCode.Error;
            }

            minimumCount = 0;
            maximumCount = 0;

            for (int parameterIndex = 0;
                    parameterIndex < parameterInfo.Length;
                    parameterIndex++)
            {
                //
                // NOTE: Just skip over invalid array entries.
                //
                if (parameterInfo[parameterIndex] == null)
                    continue;

                if (parameterInfo[parameterIndex].IsDefined(
                        typeof(ParamArrayAttribute), false))
                {
                    //
                    // NOTE: The minimum [required] number of parameters is
                    //       unchanged and the maximum [allowed] number of
                    //       parameters is now "infinite".
                    //
                    maximumCount = Count.Invalid;
                }
                else
                {
                    //
                    // NOTE: If this parameter is optional then the minimum
                    //       [required] number of parameters is unchanged.
                    //
                    if (!parameterInfo[parameterIndex].IsOptional)
                    {
                        //
                        // NOTE: We hit another required parameter, increase
                        //       the minimum [required] number of parameters.
                        //
                        minimumCount++;
                    }

                    //
                    // NOTE: If the maximum [allowed] number of parameters has
                    //       not already been set to "infinite", increase it.
                    //
                    if (maximumCount != Count.Invalid)
                        maximumCount++;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsPrimitiveType(
            Type type,
            bool output
            )
        {
            Type elementType = null;

            return IsPrimitiveType(type, output, ref elementType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsPrimitiveType(
            Type type,
            bool output,
            ref Type elementType
            )
        {
            if (type != null)
            {
                if (type.IsPrimitive)
                {
                    elementType = type;

                    return true;
                }
                else if (output && type.IsByRef)
                {
                    Type byRefElementType = type.GetElementType();

                    if ((byRefElementType != null) &&
                        byRefElementType.IsPrimitive)
                    {
                        elementType = byRefElementType;

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsEnumType(
            Type type,
            bool nullable,
            bool output
            )
        {
            Type elementType = null;

            return IsEnumType(type, nullable, output, ref elementType);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsEnumType(
            Type type,
            bool nullable,
            bool output,
            ref Type elementType
            )
        {
            if (type != null)
            {
                if (type.IsEnum)
                {
                    elementType = type;

                    return true;
                }
                else
                {
                    Type valueType = null;

                    if (nullable &&
                        IsNullableType(type, output, ref valueType) &&
                        (valueType != null) && valueType.IsEnum)
                    {
                        elementType = valueType;

                        return true;
                    }
                    else if (output && type.IsByRef)
                    {
                        Type byRefElementType = type.GetElementType();

                        if ((byRefElementType != null) &&
                            byRefElementType.IsEnum)
                        {
                            elementType = byRefElementType;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSimpleTypeForToString(
            IBinder binder,
            Type type
            )
        {
            if (type != null)
            {
                if (type.IsPrimitive || type.IsEnum)
                {
                    return true;
                }
                else
                {
                    IScriptBinder scriptBinder = binder as IScriptBinder;

                    if ((scriptBinder != null) &&
                        scriptBinder.HasChangeTypeCallback(type, true))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method returns non-zero for all the types that can be
        //       converted by the default script binder to a StringList object.
        //
        private static bool IsStringListForChangeType(
            IBinder binder,
            Type type
            )
        {
            if (binder != null)
            {
                IScriptBinder scriptBinder = binder as IScriptBinder;

                if (scriptBinder != null)
                {
                    ChangeTypeCallback callback = null;

                    if (scriptBinder.HasChangeTypeCallback(
                            type, false, ref callback) &&
                        scriptBinder.IsCoreStringListChangeTypeCallback(
                            callback))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSimpleTypeForToString(
            Type type,
            bool output
            )
        {
            if (type == null)
                return false;

            if ((type == typeof(string)) ||
                (type == typeof(StringPair)) ||
                (type == typeof(StringList)))
            {
                return true;
            }

            if (output)
            {
                if ((type == ByRefStringType) ||
                    (type == ByRefStringPairType) ||
                    (type == ByRefStringListType))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsGenericListType(
            IBinder binder, /* NOT USED */
            Type type,
            bool output
            )
        {
            while ((type != null) && (type != typeof(object)))
            {
                if (output && type.IsByRef)
                {
                    Type byRefElementType = type.GetElementType();

                    if (byRefElementType == null)
                        return false;

                    type = byRefElementType;
                }

                if (type.IsGenericType &&
                    (type.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Type GetRuntimeType()
        {
            return RuntimeType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool GetGenericTypeInfo(
            Type type,
            int levels,
            Dictionary<Type, int> types,
            ref Type genericType,
            ref Type[] genericArguments
            )
        {
            do
            {
                //
                // NOTE: Return false if the type is invalid, is at the very
                //       root of the type hierarchy, or contains any generic
                //       parameters.
                //
                if ((type == null) || (type == typeof(object) ||
                    type.ContainsGenericParameters))
                {
                    return false;
                }

                //
                // NOTE: Keep track of the fact that we have seen this type
                //       [before we change it to the base type].
                //
                if (!types.ContainsKey(type))
                    types.Add(type, levels);

                //
                // NOTE: Return true if the type is generic.  Also, fetch the
                //       generic type definition and generic arguments into the
                //       parameters provided by the caller.
                //
                if (type.IsGenericType)
                {
                    genericType = type.GetGenericTypeDefinition();
                    genericArguments = type.GetGenericArguments();

                    //
                    // NOTE: Only return true if the generic type definition
                    //       and generic arguments are actually valid.
                    //
                    if ((genericType != null) && (genericArguments != null))
                        return true;
                    else
                        return false;
                }

                type = type.BaseType;
            }
            while (true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSimpleGenericTypeForToString(
            IBinder binder,
            Type type,
            bool output
            )
        {
            int levels = 0;
            Dictionary<Type, int> types = null;

            return IsSimpleGenericTypeForToString(
                binder, type, output, ref levels, ref types);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsSimpleGenericTypeForToString(
            IBinder binder,
            Type type,
            bool output,
            ref int levels,
            ref Dictionary<Type, int> types
            )
        {
            if ((type != null) && !type.ContainsGenericParameters)
            {
                //
                // NOTE: If the number of type nesting levels exceeds
                //       the arbitrary limit we have set then this is
                //       not a "simple" generic type; therefore, return
                //       false.
                //
                if (levels++ > MaximumTypeLevels)
                    return false;

                if (types == null)
                    types = new Dictionary<Type, int>();

                Type genericType = null;
                Type[] genericArguments = null;

                if (GetGenericTypeInfo(type, levels, types,
                        ref genericType, ref genericArguments))
                {
                    foreach (Type genericArgument in genericArguments)
                    {
                        //
                        // NOTE: If the generic argument is null,
                        //       something is really wrong.  In this
                        //       case, just return false.
                        //
                        if (genericArgument == null)
                            return false;

                        //
                        // BUGFIX: If we have already seen this type,
                        //         then it is certainly not a "simple"
                        //         generic type (i.e. it is nested
                        //         and contains one or more circular
                        //         type references).
                        //
                        if (types.ContainsKey(genericArgument))
                            return false;

                        //
                        // NOTE: Keep track of the fact that we have
                        //       seen this type.
                        //
                        types.Add(genericArgument, levels);

                        //
                        // NOTE: Make sure the generic argument is a simple
                        //       string type, primitive type (e.g. int), or
                        //       a simple generic type (e.g. List<int>);
                        //       otherwise, return false.
                        //
                        if (!IsSimpleTypeForToString(genericArgument, output) &&
                            !IsSimpleTypeForToString(binder, genericArgument) &&
                            !IsSimpleGenericTypeForToString(binder, genericArgument,
                                output, ref levels, ref types))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ConvertValueToString(
            IScriptBinder scriptBinder, /* in */
            CultureInfo cultureInfo,    /* in */
            Type type,                  /* in */
            OptionDictionary options,   /* in */
            object value,               /* in */
            bool create,                /* in */
            ref Result result           /* out */
            )
        {
            if (!create)
            {
                if ((scriptBinder != null) &&
                    scriptBinder.HasToStringCallback(type, false))
                {
                    IChangeTypeData changeTypeData = new ChangeTypeData(
                        "MarshalOps.ConvertValueToString", type, value,
                        options, cultureInfo, null, ConvertValueToStringMarshalFlags);

                    ReturnCode code = scriptBinder.ToString(
                        changeTypeData, ref result);

                    if (code == ReturnCode.Ok)
                        result = changeTypeData.NewValue as string;

                    return code;
                }

                result = String.Format(
                    "binder ToString conversion not available for type {0}",
                    GetErrorTypeName(type));
            }
            else
            {
                result = "binder ToString conversion bypassed by caller";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetObjectHandleName(
            Interpreter interpreter,
            string objectName,
            Assembly assembly
            )
        {
            return (objectName != null) ? objectName : FormatOps.AssemblyName(
                assembly, (interpreter != null) ? interpreter.NextId() :
                GlobalState.NextId() /* FALLBACK */, false, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetObjectHandleName(
            Interpreter interpreter,
            string objectName,
            Type valueType,
            bool runtime
            )
        {
            return (objectName != null) ? objectName : FormatOps.ObjectHandle(
                null, FormatOps.ObjectHandleTypeName(valueType, !runtime),
                (interpreter != null) ? interpreter.NextId() :
                GlobalState.NextId() /* FALLBACK */);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void CheckReturnValueType(
            Interpreter interpreter,
            string objectName,
            object value,
            Type valueType,
            bool runtime,
            ref ObjectFlags objectFlags,
            ref string localObjectName
            )
        {
            if (value is Assembly)
            {
                //
                // NOTE: Specially format the string handle so that it can be
                //       easily recognized as an assembly name.
                //
                // BUGBUG: When we format the name here for a given assembly it
                //         is always the same.  This can lead to problems if a
                //         script removes the assembly via [object dispose] and
                //         then tries to re-load it into the same variable as
                //         before (unless the variable has been unset in the
                //         interim) because the added reference is canceled out
                //         since the old and new values are the same.  An easy
                //         "fix" for this problem would be to include a unique
                //         Id at the end of the opaque object handle for
                //         assemblies.
                //
                // HACK: The "fix" mentioned above is now in place; however, it
                //       is ugly.
                //
                // BUGFIX: If an object name has been explicitly specified by the
                //         caller, we must use it.
                //
                if (objectName != null)
                {
                    localObjectName = objectName;
                }
                else
                {
                    localObjectName = GetObjectHandleName(
                        interpreter, objectName, (Assembly)value);
                }

                return;
            }

            ///////////////////////////////////////////////////////////////////

            if (value is IInterpreter)
            {
                //
                // NOTE: Set the flags that indicate that we know this is an
                //       Interpreter object (informational).
                //
                objectFlags |= ObjectFlags.Interpreter;

                //
                // NOTE: If this object represents the active interpreter, it
                //       cannot be disposed from a script without causing
                //       serious problems.  Set the flag accordingly.
                //
                if (Object.ReferenceEquals(value, interpreter))
                    objectFlags |= ObjectFlags.NoDispose;
            }
            else if (value is IHost)
            {
                //
                // NOTE: Set the flags that indicate that we know this is a
                //       Host object (informational).
                //
                objectFlags |= ObjectFlags.Host;

                //
                // NOTE: If this object represents the active interpreter
                //       host, it cannot be disposed from a script without
                //       causing serious problems.  Set the flags accordingly.
                //
                if (Object.ReferenceEquals(value, interpreter.Host))
                    objectFlags |= ObjectFlags.NoDispose;
            }
#if DEBUGGER
            else if (value is IDebugger)
            {
                //
                // NOTE: Set the flags that indicate that we know this is a
                //       Debugger object (informational).
                //
                objectFlags |= ObjectFlags.Debugger;

                //
                // NOTE: If this object represents the active script debugger,
                //       it cannot be disposed from a script without causing
                //       serious problems.  Set the flag accordingly.
                //
                if (Object.ReferenceEquals(value, interpreter.Debugger))
                    objectFlags |= ObjectFlags.NoDispose;
            }
#endif
            else if (value is IEventManager)
            {
                //
                // NOTE: Set the flags that indicate that we know this is an
                //       EventManager object (informational).
                //
                objectFlags |= ObjectFlags.EventManager;

                //
                // NOTE: If this object represents the active event manager,
                //       it cannot be disposed from a script without causing
                //       serious problems.  Set the flag accordingly.
                //
                if (Object.ReferenceEquals(value, interpreter.EventManager))
                    objectFlags |= ObjectFlags.NoDispose;
            }
#if THREADING
            else if (value is IContextManager)
            {
                //
                // NOTE: Set the flags that indicate that we know this is a
                //       ContextManager object (informational).
                //
                objectFlags |= ObjectFlags.ContextManager;

                //
                // NOTE: If this object represents the active context manager,
                //       it cannot be disposed from a script without causing
                //       serious problems.  Set the flag accordingly.
                //
                if (Object.ReferenceEquals(
                        value, interpreter.InternalContextManager))
                {
                    objectFlags |= ObjectFlags.NoDispose;
                }
            }
            else if (value is IEngineContext)
            {
                //
                // NOTE: Set the flags that indicate that we know this is an
                //       EngineContext object (informational).
                //
                objectFlags |= ObjectFlags.EngineContext;

                //
                // NOTE: If this object represents an active engine context,
                //       it cannot be disposed from a script without causing
                //       serious problems.  However, there is no easy way to
                //       check if it belongs to this interpreter.  Set the
                //       flag anyhow.
                //
                objectFlags |= ObjectFlags.NoDispose;
            }
            else if (value is IInteractiveContext)
            {
                //
                // NOTE: Set the flags that indicate that we know this is an
                //       InteractiveContext object (informational).
                //
                objectFlags |= ObjectFlags.InteractiveContext;

                //
                // NOTE: If this object represents an active interactive
                //       context, it cannot be disposed from a script without
                //       causing serious problems.  However, there is no easy
                //       way to check if it belongs to this interpreter.  Set
                //       the flag anyhow.
                //
                objectFlags |= ObjectFlags.NoDispose;
            }
            else if (value is ITestContext)
            {
                //
                // NOTE: Set the flags that indicate that we know this is an
                //       TestContext object (informational).
                //
                objectFlags |= ObjectFlags.TestContext;

                //
                // NOTE: If this object represents an active test context,
                //       it cannot be disposed from a script without causing
                //       serious problems.  However, there is no easy way to
                //       check if it belongs to this interpreter.  Set the
                //       flag anyhow.
                //
                objectFlags |= ObjectFlags.NoDispose;
            }
            else if (value is IVariableContext)
            {
                //
                // NOTE: Set the flags that indicate that we know this is an
                //       VariableContext object (informational).
                //
                objectFlags |= ObjectFlags.VariableContext;

                //
                // NOTE: If this object represents an active variable context,
                //       it cannot be disposed from a script without causing
                //       serious problems.  However, there is no easy way to
                //       check if it belongs to this interpreter.  Set the
                //       flag anyhow.
                //
                objectFlags |= ObjectFlags.NoDispose;
            }
#endif
            else if (value is INamespace)
            {
                //
                // NOTE: Set the flags that indicate that we know this is a
                //       Namespace object (informational).
                //
                objectFlags |= ObjectFlags.Namespace;

                //
                // NOTE: If this object represents an active namespace, it
                //       cannot be disposed from a script without causing
                //       serious problems.  Set the flag accordingly.
                //
                if (Object.ReferenceEquals(
                        ((INamespace)value).Interpreter, interpreter))
                {
                    objectFlags |= ObjectFlags.NoDispose;
                }
            }
            else if (value is ICallFrame)
            {
                //
                // NOTE: Set the flags that indicate that we know this is a
                //       CallFrame object (informational).
                //
                objectFlags |= ObjectFlags.CallFrame;

                //
                // NOTE: If this object represents the active call frame, it
                //       cannot be disposed from a script without causing
                //       serious problems.  However, there is no easy way to
                //       check if it belongs to this interpreter.  Set the
                //       flag anyhow.
                //
                objectFlags |= ObjectFlags.NoDispose;
            }
            else if (value is IWrapper)
            {
                //
                // NOTE: Set the flags that indicate that we know this is a
                //       Wrapper object (informational).
                //
                objectFlags |= ObjectFlags.Wrapper;

                //
                // NOTE: If this object represents the active entity, it
                //       cannot be disposed from a script without causing
                //       problems.  However, there is no easy way to check
                //       if it belongs to this interpreter.  Set the flag
                //       anyhow.
                //
                objectFlags |= ObjectFlags.NoDispose;
            }
            else if (value is CallStack)
            {
                //
                // NOTE: Set the flags that indicate that we know this is a
                //       CallStack object (informational).
                //
                objectFlags |= ObjectFlags.CallStack;

                //
                // NOTE: If this object represents the active call stack, it
                //       cannot be disposed from a script without causing
                //       serious problems.  However, there is no easy way to
                //       check if it belongs to this interpreter.  Set the
                //       flag anyhow.
                //
                objectFlags |= ObjectFlags.NoDispose;
            }

            //
            // NOTE: If necessary, set the flags that indicate that we know
            //       this is a runtime object from this library.
            //
            if (runtime)
                objectFlags |= ObjectFlags.Runtime;

            //
            // BUGFIX: If an object name has been explicitly specified by the
            //         caller, we must use it.
            //
            if (objectName != null)
            {
                //
                // NOTE: Return the specified name for the object.  If the
                //       value is null, only do this if the appropriate flag
                //       is set.
                //
                if ((value != null) || FlagOps.HasFlags(
                        objectFlags, ObjectFlags.ForceManualName, true))
                {
                    localObjectName = objectName;
                }
                else
                {
                    localObjectName = DefaultObjectName;
                }
            }
            else
            {
                //
                // NOTE: Return the [full] name of the object (i.e. include
                //       the namespace if the object is not from this runtime
                //       library).  If the value is null, only do this if the
                //       appropriate flag is set.
                //
                if ((value != null) || FlagOps.HasFlags(
                        objectFlags, ObjectFlags.ForceAutomaticName, true))
                {
                    localObjectName = GetObjectHandleName(
                        interpreter, objectName, valueType, runtime);
                }
                else
                {
                    localObjectName = DefaultObjectName;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void CheckForStickyAlias(
            IHaveObjectFlags haveObjectFlags, /* in */
            ref ObjectFlags objectFlags,      /* in, out */
            ref bool alias                    /* in, out */
            )
        {
            if (haveObjectFlags == null)
                return;

            if (FlagOps.HasFlags(objectFlags, ObjectFlags.UnstickAlias, true) ||
                FlagOps.HasFlags(haveObjectFlags.ObjectFlags, ObjectFlags.UnstickAlias, true))
            {
                return;
            }

            //
            // NOTE: This purposely does not check for the StickAlias flag in
            //       the "objectFlags" parameter.  This allows a sticky alias
            //       to be created [later] from a non-aliased opaque object
            //       handle.  Furthermore, having the StickAlias flag imply
            //       that a command alias should be created right now would
            //       somewhat defeat the purpose of having a distinct -alias
            //       option.
            //
            if (FlagOps.HasFlags(haveObjectFlags.ObjectFlags, ObjectFlags.StickAlias, true))
            {
                objectFlags |= ObjectFlags.StickAlias;
                alias = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,           /* in */
            Type type,                         /* in */
            ObjectFlags objectFlags,           /* in */
            OptionDictionary options,          /* in */
            ObjectOptionType objectOptionType, /* in */
            string objectName,                 /* in */
            object value,                      /* in */
            bool create,                       /* in */
            bool alias,                        /* in */
            bool aliasReference,               /* in */
            ref Result result                  /* out */
            )
        {
            IBinder binder = null;
            CultureInfo cultureInfo = null;

            if (interpreter != null)
            {
                if (!FlagOps.HasFlags(objectFlags, ObjectFlags.NoBinder, true))
                    binder = interpreter.Binder;

                cultureInfo = interpreter.CultureInfo;
            }

            OptionDictionary localOptions;

            if (options != null)
                localOptions = options;
            else if (alias)
                localOptions = ObjectOps.GetInvokeOptions(objectOptionType);
            else
                localOptions = null;

            return FixupReturnValue(
                interpreter, binder, cultureInfo, type, objectFlags,
                localOptions, objectOptionType, objectName, null,
                value, create, true, alias, aliasReference, false,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,           /* in */
            IBinder binder,                    /* in */
            CultureInfo cultureInfo,           /* in */
            Type type,                         /* in */
            ObjectFlags objectFlags,           /* in */
            OptionDictionary options,          /* in */
            ObjectOptionType objectOptionType, /* in */
            string objectName,                 /* in */
            string interpName,                 /* in */
            object value,                      /* in */
            bool create,                       /* in */
            bool dispose,                      /* in */
            bool alias,                        /* in */
            bool aliasReference,               /* in */
            bool toString,                     /* in */
            ref Result result                  /* out */
            )
        {
            ReturnCode code;
            string localObjectName = null;
            string aliasName = null;
            long token = 0;
            IAlias newAlias = null;

            if (interpreter != null)
            {
                if (!create && (value == null))
                {
                    result = DefaultObjectName;
                    code = ReturnCode.Ok;
                }
                else if (!create && (value is string))
                {
                    result = (string)value;
                    code = ReturnCode.Ok;
                }
                else
                {
                    //
                    // BUGFIX: The type of null is "object".
                    //
                    Type valueType = GetValueTypeOrObjectType(value);

                    //
                    // NOTE: Fetch the object flags from the attribute on the
                    //       associated type, if any.
                    //
                    if (!FlagOps.HasFlags(
                            objectFlags, ObjectFlags.NoAttribute, true))
                    {
                        objectFlags |= AttributeOps.GetObjectFlags(valueType);
                    }

                    //
                    // HACK: Make COM Interop objects work [slightly better].
                    //
                    if (FlagOps.HasFlags(
                            objectFlags, ObjectFlags.NoComObjectReturn, true) ||
                        !IsSystemComObjectType(valueType) ||
                        ((valueType = GetTypeFromComObject(
                            interpreter, null, null, value, interpreter.ObjectInterfaces,
                            binder, cultureInfo, objectFlags, ref result)) != null))
                    {
                        IScriptBinder scriptBinder = binder as IScriptBinder;
                        ReturnCode localCode;
                        Result localResult = null;

                        if (!create && IsEnumType(valueType, true, true))
                        {
                            result = ToStringOrDefaultReturnValue(value);
                            code = ReturnCode.Ok;
                        }
                        else if (!create && IsSimpleTypeForToString(valueType, true))
                        {
                            result = ToStringOrDefaultReturnValue(value);
                            code = ReturnCode.Ok;
                        }
                        else if (!create && IsSimpleTypeForToString(binder, valueType))
                        {
                            localCode = ConvertValueToString(scriptBinder, cultureInfo,
                                valueType, options, value, create, ref localResult);

                            if (localCode == ReturnCode.Ok)
                                result = localResult;
                            else
                                result = ToStringOrDefaultReturnValue(value);

                            code = ReturnCode.Ok;
                        }
                        else if (!create &&
                            IsSimpleGenericTypeForToString(binder, valueType, true) &&
                            IsGenericListType(binder, valueType, true))
                        {
                            result = ToStringOrDefaultReturnValue(value);
                            code = ReturnCode.Ok;
                        }
                        else if ((valueType != null) && valueType.IsPointer)
                        {
                            result = "pointer types are not supported";
                            code = ReturnCode.Error;
                        }
                        else
                        {
                            Type elementType = null;

                            if (!create &&
                                (IsOneDimensionalArrayType(valueType, ref elementType) ||
                                    HasElementType(valueType, false, ref elementType)) &&
                                (IsSimpleTypeForToString(elementType, true) ||
                                    IsSimpleTypeForToString(binder, elementType) ||
                                    (IsSimpleGenericTypeForToString(binder, elementType, true) &&
                                        IsGenericListType(binder, elementType, true))))
                            {
                                //
                                // NOTE: Convert all one-dimensional arrays of objects that have
                                //       a primitive, yet meaningful, string representation to a
                                //       simple list of strings.
                                //
                                result = new StringList(value);
                                code = ReturnCode.Ok;
                            }
                            else
                            {
                                //
                                // NOTE: Make sure that we use any existing opaque object handle for this
                                //       object value if possible; otherwise, we end up with a bunch of
                                //       opaque object handles pointing to the exact same object.  We
                                //       cannot reuse the opaque object value if the we need an alias and
                                //       it does not include one.
                                //
                                ObjectFlags hasObjectFlags = ObjectFlags.None;
                                ObjectFlags notHasObjectFlags = ObjectFlags.None;

                                //
                                // NOTE: See if the caller has forbidden us from checking the alias flag
                                //       on potential opaque object handles to reuse.  If so, only the
                                //       object reference itself will be considered.
                                //
                                bool ignoreAlias = FlagOps.HasFlags(
                                    objectFlags, ObjectFlags.IgnoreAlias, true);

                                //
                                // NOTE: *SPECIAL* For the null value, the alias flag is not necessary;
                                //       we always want to reuse the "null" opaque object handle unless
                                //       we are actually forcibly forbidden from doing it.
                                //
                                if (value != null)
                                {
                                    if (!ignoreAlias)
                                    {
                                        if (alias)
                                            hasObjectFlags |= ObjectFlags.Alias;
                                        else
                                            notHasObjectFlags |= ObjectFlags.Alias;
                                    }
                                }
                                else
                                {
                                    //
                                    // NOTE: *SPECIAL* The "null" opaque object handle may or may not
                                    //       have the alias flag; however, it always has this flag.
                                    //
                                    hasObjectFlags |= ObjectFlags.NullObject;
                                }

                                IObject @object = null;

                                if (!FlagOps.HasFlags(objectFlags, ObjectFlags.ForceNew, true) &&
                                    ((objectName == null) || /* NOTE: Only if not a specific name. */
                                    FlagOps.HasFlags(objectFlags, ObjectFlags.AllowExisting, true)) &&
                                    interpreter.GetObject(value, alias ?
                                        LookupFlags.MarshalAlias : LookupFlags.MarshalDefault,
                                        hasObjectFlags, notHasObjectFlags, true, true,
                                        ref localObjectName, ref @object) == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Does the caller want to override the standard semantics
                                    //       of returning the object handle from the interpreter?  In
                                    //       that case, we will return the result of the ToString method
                                    //       on the object.
                                    //
                                    if (toString)
                                    {
                                        result = ToStringOrDefaultReturnValue(value);
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: At this point, we cannot use the objectName passed by
                                        //       the caller, even if it is non-null because the opaque
                                        //       object handle may not be known by that name.
                                        //
                                        // NOTE: *SPECIAL* When dealing with the "null" opaque object
                                        //       handle, return an empty string for the existing handle
                                        //       name.  This is necessary to allow scripts to check the
                                        //       length of the handle name against zero.  The only way
                                        //       to prevent this handle is with the new ForceName flag.
                                        //
                                        if ((value != null) || FlagOps.HasFlags(
                                                objectFlags, ObjectFlags.ForceAutomaticName, true))
                                        {
                                            result = localObjectName;
                                        }
                                        else
                                        {
                                            result = DefaultObjectName;
                                        }
                                    }

                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    localCode = ConvertValueToString(scriptBinder, cultureInfo,
                                        valueType, options, value, create, ref localResult);

                                    if (localCode == ReturnCode.Ok)
                                    {
                                        result = localResult;
                                        code = ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: Ok, we really haven't seen this object yet.  Add a new
                                        //       opaque object handle for this object value and return
                                        //       it.
                                        //
                                        bool runtime = (valueType != null) &&
                                            GlobalState.IsAssembly(valueType.Assembly);

                                        //
                                        // NOTE: If the caller requested that an alias be created for
                                        //       this opaque object handle, add the flag.  Do this now
                                        //       to allow the CheckReturnValueType method to possibly
                                        //       override it.  Currently, the CheckReturnValueType method
                                        //       never overrides this flag; however, that is subject to
                                        //       change.
                                        //
                                        if (alias)
                                            objectFlags |= ObjectFlags.Alias;

                                        //
                                        // NOTE: Based on the type of the value to be returned, determine
                                        //       the opaque object handle name and the associated object
                                        //       flags.
                                        //
                                        CheckReturnValueType(
                                            interpreter, objectName, value, valueType, runtime,
                                            ref objectFlags, ref localObjectName);

                                        //
                                        // NOTE: Does the caller want to override the standard semantics
                                        //       of adding an object to the interpreter?  In that case,
                                        //       we will return the result of the ToString method on the
                                        //       object and then dispose of the object, since it represents
                                        //       a transient value at this point (the marshaller "owns" it
                                        //       and we can be relatively certain no other outside references
                                        //       to it exist).
                                        //
                                        if (toString)
                                        {
                                            result = ToStringOrDefaultReturnValue(value);
                                            code = ReturnCode.Ok;

                                            goto disposeObject;
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: At this point, if the opaque object handle name is
                                            //       null, bail out now.  This must be done because the
                                            //       call to AddObject (below) cannot succeed when the
                                            //       name is null.  This means the CheckReturnValueType
                                            //       method "rejected" the request to add the object.
                                            //
                                            if (localObjectName == null)
                                            {
                                                result = DefaultObjectName;
                                                return ReturnCode.Ok;
                                            }

                                            //
                                            // NOTE: If the caller is not intentionally creating an opaque
                                            //       object handle, prevent it from being automatically
                                            //       disposed.
                                            //
                                            if (!create && !FlagOps.HasFlags(
                                                    objectFlags, ObjectFlags.AutoDispose, true) &&
                                                !FlagOps.HasFlags(objectOptionType,
                                                    ObjectOptionType.CreateOptionMask, false))
                                            {
                                                objectFlags |= ObjectFlags.NoAutoDispose;
                                            }

                                            //
                                            // NOTE: Normally, use a starting reference count of zero;
                                            //       however, this can be increased to one by using an
                                            //       object flag.
                                            //
                                            int referenceCount = 0;

                                            if (FlagOps.HasFlags(
                                                    objectFlags, ObjectFlags.AddReference, true))
                                            {
                                                referenceCount++;
                                            }

                                            //
                                            // NOTE: Add the opaque object handle to the interpreter.  It
                                            //       is possible for this call to fail (e.g. because the
                                            //       opaque object handle name already exists, etc).
                                            //
                                            code = interpreter.AddObject(
                                                localObjectName, type, objectFlags, ClientData.Empty,
                                                referenceCount,
#if NATIVE && TCL
                                                interpName,
#endif
#if DEBUGGER && DEBUGGER_ARGUMENTS
                                                Engine.GetDebuggerExecuteArguments(interpreter),
#endif
                                                value, ref token, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                if (alias)
                                                {
                                                    Type aliasType = AppDomainOps.IsSame(interpreter) ?
                                                        valueType : typeof(object);

                                                    aliasName = interpreter.GetObjectAliasName(
                                                        aliasType, localObjectName, null);

                                                    //
                                                    // HACK: Avoid trying to add an aliased command with
                                                    //       an empty name when namespaces are enabled,
                                                    //       because it is illegal and not very useful.
                                                    //
                                                    if (interpreter.CanAddAliasName(
                                                            localObjectName, aliasName))
                                                    {
                                                        code = interpreter.AddObjectAlias(
                                                            localObjectName, aliasName, options,
                                                            objectOptionType, aliasReference,
                                                            ref newAlias, ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
                                                            if (interpName != null)
                                                            {
#if NATIVE && TCL
                                                                code = interpreter.AddTclBridge(
                                                                    newAlias as IExecute, interpName,
                                                                    localObjectName, ClientData.Empty,
                                                                    FlagOps.HasFlags(objectFlags,
                                                                        ObjectFlags.ForceDelete, true),
                                                                    FlagOps.HasFlags(objectFlags,
                                                                        ObjectFlags.NoComplain, true),
                                                                    ref result);
#else
                                                                result = "option \"-tcl\" not supported for this platform";
                                                                code = ReturnCode.Error;
#endif
                                                            }

                                                            if (code == ReturnCode.Ok)
                                                            {
                                                                //
                                                                // NOTE: The object, alias, and bridged Tcl
                                                                //       command (if any) were successfully
                                                                //       added to the interpreter.
                                                                //
                                                                result = localObjectName;

                                                                if (result != null)
                                                                    result.ValueData = ObjectValueData;
                                                            }
                                                            else
                                                            {
                                                                goto removeAlias;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            goto removeObject;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = localObjectName;

                                                        if (result != null)
                                                            result.ValueData = ObjectValueData;
                                                    }
                                                }
                                                else
                                                {
                                                    result = localObjectName;

                                                    if (result != null)
                                                        result.ValueData = ObjectValueData;
                                                }
                                            }
                                            else
                                            {
                                                goto disposeObject;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            goto done;

        removeAlias:
            //
            // NOTE: If we get here, we failed to add the bridged Tcl command
            //       for the added object; therefore, undo our previously
            //       successful actions by removing the alias, removing the
            //       object, and then disposing it.
            //
            if (newAlias != null)
            {
                ReturnCode removeCode;
                Result removeResult = null;

                removeCode = interpreter.RemoveAliasAndCommand(
                    (aliasName != null) ? aliasName : localObjectName,
                    null, false, ref removeResult);

                if (removeCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, removeCode, removeResult);
            }

        removeObject:
            //
            // NOTE: If we get here, we failed to add an alias for the added
            //       object; therefore, undo our previously successful actions
            //       by removing the object and then disposing it.
            //
            if (token != 0)
            {
                ReturnCode removeCode;
                Result removeResult = null;

                removeCode = interpreter.RemoveObject(
                    token, null, ObjectOps.GetDefaultSynchronous(),
                    ref dispose, ref removeResult);

                if (removeCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, removeCode, removeResult);
            }

            goto done;

        disposeObject:
            //
            // NOTE: Do they want us to try to dispose of their object upon
            //       failure AND are we actually allowed to do so?
            //
            if (dispose &&
                !FlagOps.HasFlags(objectFlags, ObjectFlags.NoDispose, true))
            {
                //
                // NOTE: We failed to add the object to the interpreter, try
                //       to dispose the object since it will otherwise be lost.
                //
                ReturnCode disposeCode;
                Result disposeError = null;

                disposeCode = ObjectOps.TryDispose(value, ref disposeError);

                if (disposeCode != ReturnCode.Ok)
                    DebugOps.Complain(interpreter, disposeCode, disposeError);
            }

            goto done;

        done:
            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FixupByRefArray(
            Interpreter interpreter,               /* in */
            IBinder binder,                        /* in */
            CultureInfo cultureInfo,               /* in */
            ObjectFlags objectFlags,               /* in */
            OptionDictionary options,              /* in */
            ObjectOptionType objectOptionType,     /* in */
            string interpName,                     /* in */
            Array array,                           /* in */
            string name,                           /* in */
            MarshalFlags marshalFlags,             /* in: NOT USED */
            ByRefArgumentFlags byRefArgumentFlags, /* in */
            bool create,                           /* in */
            bool dispose,                          /* in */
            bool alias,                            /* in */
            bool aliasReference,                   /* in */
            bool toString,                         /* in */
            ref Result error                       /* out */
            )
        {
            if (interpreter != null)
            {
                int rank;

                if ((array != null) && ((rank = array.Rank) > 0))
                {
                    //
                    // NOTE: In "fast" mode, skip all the extra variable processing.
                    //
                    bool fast = FlagOps.HasFlags(
                        byRefArgumentFlags, ByRefArgumentFlags.Fast, true);

                    //
                    // NOTE: In "direct" mode, bypass all use of SetVariableValue
                    //       (primarily for speed reasons).
                    //
                    bool direct = FlagOps.HasFlags(
                        byRefArgumentFlags, ByRefArgumentFlags.Direct, true);

                    VariableFlags variableFlags = VariableFlags.None;

                    if (fast)
                        variableFlags |= VariableFlags.FastNonInstanceMask;

                    lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                    {
                        //
                        // NOTE: For direct mode, allocate the new array value now.
                        //
                        IVariable variable = null;
                        ElementDictionary arrayValue = null;

                        if (direct)
                        {
                            //
                            // NOTE: Forbid the use of virtual variables in direct
                            //       mode.
                            //
                            variableFlags |= VariableFlags.NonVirtual;

                            if (interpreter.GetVariableViaResolversWithSplit(
                                    name, ref variableFlags, ref variable,
                                    ref error) == ReturnCode.Ok)
                            {
                                if (EntityOps.IsUndefined(variable) ||
                                    EntityOps.IsArray2(variable))
                                {
                                    arrayValue = variable.ArrayValue;

                                    if (arrayValue == null)
                                    {
                                        arrayValue = new ElementDictionary(
                                            interpreter.VariableEvent);

                                        variable.ArrayValue = arrayValue;
                                    }
                                }
                                else
                                {
                                    error = String.Format(
                                        "{0} isn't an array",
                                        FormatOps.WrapOrNull(name));

                                    return ReturnCode.Error;
                                }
                            }
                            else
                            {
                                return ReturnCode.Error;
                            }
                        }

                        //
                        // NOTE: Create space to hold the lower bounds, lengths,
                        //       and indexes for each rank of the array.
                        //
                        int[] lowerBounds = null;
                        int[] lengths = null;
                        int[] indexes = null;

                        //
                        // NOTE: Obtain all the bounds for the array.
                        //
                        if (!ArrayOps.GetBounds(
                                array, ref rank, ref lowerBounds, ref lengths,
                                ref indexes))
                        {
                            error = String.Format(
                                "unable to obtain bounds for array {0}",
                                FormatOps.WrapOrNull(name));

                            return ReturnCode.Error;
                        }

                        //
                        // NOTE: Iterate over the array N number of times.  For our
                        //       purposes, the actual value of the loop index does
                        //       not matter as it is not used.
                        //
                        ObjectOptionType savedObjectOptionType = objectOptionType;

                        objectOptionType = ObjectOps.GetByRefOptionType(
                            objectOptionType, byRefArgumentFlags);

                        int length = array.Length;

                        for (int unused = 0; unused < length; unused++)
                        {
                            string index = GenericOps<int>.ListToString(
                                indexes, 0, Index.Invalid, ToStringFlags.None,
                                Characters.Comma.ToString(), null, false);

                            object value = array.GetValue(indexes);

                            if (value != null)
                            {
                                Type elementType = value.GetType();

                                if (IsArrayType(elementType))
                                {
                                    string nestedName = FormatOps.NestedArrayName(name, index);

                                    if (FixupByRefArray(
                                            interpreter, binder, cultureInfo, objectFlags,
                                            options, savedObjectOptionType, interpName,
                                            (Array)value, nestedName, marshalFlags,
                                            byRefArgumentFlags, create, dispose, alias,
                                            aliasReference, toString, ref error) != ReturnCode.Ok)
                                    {
                                        return ReturnCode.Error;
                                    }

                                    //
                                    // NOTE: Now, we need to update the reference in the parent array
                                    //       (below) to point to the nested array.
                                    //
                                    value = nestedName;
                                }

                                //
                                // NOTE: Translate the raw output argument value to a string result
                                //       (possibly a new opaque object handle).
                                //
                                Result result = null;

                                if (FixupReturnValue(
                                        interpreter, binder, cultureInfo, null, objectFlags,
                                        options, objectOptionType, null, interpName, value,
                                        create, dispose, alias, aliasReference, toString,
                                        ref result) != ReturnCode.Ok)
                                {
                                    return ReturnCode.Error;
                                }

                                //
                                // NOTE: Use direct mode?  If so, completely bypass the use of
                                //       SetVariableValue (primarily for speed reasons).
                                //
                                if (direct && (arrayValue != null))
                                {
                                    //
                                    // NOTE: We want the string value here, not the Result
                                    //       object.
                                    //
                                    arrayValue[index] = result.String;
                                }
                                else
                                {
                                    if (!FlagOps.HasFlags(byRefArgumentFlags,
                                            ByRefArgumentFlags.NoSetVariable, true))
                                    {
                                        //
                                        // NOTE: This cannot be guaranteed to succeed:
                                        //
                                        //       1. The variable may be read-only.
                                        //
                                        //       2. If the variable has traces, they may opt to
                                        //          override, cancel, or abort setting of the new
                                        //          value.
                                        //
                                        if (interpreter.SetVariableValue2(
                                                variableFlags, name, index, result, null,
                                                ref error) != ReturnCode.Ok)
                                        {
                                            return ReturnCode.Error;
                                        }
                                    }
                                }
                            }

                            //
                            // NOTE: Increment the indexes by one element for the next
                            //       time around.
                            //
                            ArrayOps.IncrementIndexes(
                                rank, lowerBounds, lengths, indexes);
                        }

                        //
                        // NOTE: In "direct" mode, mark the variable as defined and
                        //       dirty now.
                        //
                        if (direct)
                        {
                            EntityOps.SetUndefined(variable, false);
                            EntityOps.SignalDirty(variable, null);
                        }
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "array is invalid or has an invalid rank";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FixupByRefArguments(
            Interpreter interpreter,               /* in */
            IBinder binder,                        /* in */
            CultureInfo cultureInfo,               /* in */
            ArgumentInfoList argumentInfoList,     /* in */
            ObjectFlags objectFlags,               /* in */
            OptionDictionary options,              /* in */
            ObjectOptionType objectOptionType,     /* in */
            string interpName,                     /* in */
            object[] args,                         /* in */
            MarshalFlags marshalFlags,             /* in */
            ByRefArgumentFlags byRefArgumentFlags, /* in */
            bool strict,                           /* in */
            bool create,                           /* in */
            bool dispose,                          /* in */
            bool alias,                            /* in */
            bool aliasReference,                   /* in */
            bool toString,                         /* in */
            bool arrayAsValue,                     /* in */
            bool arrayAsLink,                      /* in */
            ref Result error                       /* out */
            )
        {
            if (interpreter != null)
            {
                if (argumentInfoList != null)
                {
                    if (args != null)
                    {
                        //
                        // NOTE: In "fast" mode, skip all the extra variable processing.
                        //
                        VariableFlags variableFlags = VariableFlags.None;

                        if (FlagOps.HasFlags(byRefArgumentFlags, ByRefArgumentFlags.Fast, true))
                            variableFlags |= VariableFlags.FastNonInstanceMask;

                        ObjectOptionType savedObjectOptionType = objectOptionType;

                        objectOptionType = ObjectOps.GetByRefOptionType(
                            objectOptionType, byRefArgumentFlags);

                        int argumentLength = args.Length;

                        foreach (ArgumentInfo argumentInfo in argumentInfoList)
                        {
                            if (argumentInfo == null)
                                continue;

                            int parameterIndex = argumentInfo.Index;
                            string parameterName = argumentInfo.Name;

                            if ((parameterIndex < 0) || (parameterIndex >= argumentLength))
                            {
                                error = String.Format(
                                    "output parameter {0} out-of-bounds, index {1} " +
                                    "must be between 0 and {2}", FormatOps.ArgumentName(
                                    parameterIndex, parameterName), parameterIndex,
                                    argumentLength);

                                return ReturnCode.Error;
                            }

                            object arg = args[parameterIndex];

                            if (arg != null)
                            {
#if DEBUG && MONO
                                //
                                // HACK: *MONO* Mono does not like the use of nullable types here.
                                //       https://bugzilla.novell.com/show_bug.cgi?id=471259
                                //
                                Type argType = null;

                                try
                                {
                                    argType = arg.GetType();
                                }
                                catch
                                {
                                    // do nothing.
                                }
#else
                                Type argType = arg.GetType();
#endif

                                ///////////////////////////////////////////////////////////////////////

#if DEBUG && MONO
                                Type parameterType = null;

                                try
                                {
                                    //
                                    // BUGBUG: I'm not sure if this will cause a problem in
                                    //         Mono 2.0; however, better safe than sorry.
                                    //
                                    parameterType = argumentInfo.Type; // TODO: Test in Mono 2.0.
                                }
                                catch
                                {
                                    // do nothing.
                                }
#else
                                Type parameterType = argumentInfo.Type;
#endif

                                ///////////////////////////////////////////////////////////////////////

                                //
                                // NOTE: Are we operating in strict type checking mode?
                                //
                                if (strict ||
                                    FlagOps.HasFlags(byRefArgumentFlags, ByRefArgumentFlags.Strict, true))
                                {
                                    //
                                    // NOTE: In strict mode, we check the type of the argument
                                    //       object against the formal parameter type we actually
                                    //       expect for this parameter.
                                    //
                                    if (!IsSameValueType(argType, parameterType) &&
                                        !IsSameReferenceType(argType, parameterType, marshalFlags))
                                    {
                                        error = String.Format(
                                            "output parameter {0} type {1} does not match " +
                                            "expected type {2}", FormatOps.ArgumentName(
                                            parameterIndex, parameterName),
                                            GetErrorTypeName(argType), GetErrorTypeName(parameterType));

                                        return ReturnCode.Error;
                                    }
                                }

                                ///////////////////////////////////////////////////////////////////////

                                //
                                // NOTE: Check for array types.  Arrays are handled specially.
                                //
                                Array array = null;
                                bool isObject = false;

                                if (IsArrayValue(arg, ref array) &&
                                    ((isObject = IsObjectType(parameterType, true) ||
                                    IsArrayType(parameterType))))
                                {
                                    if (arrayAsValue || FlagOps.HasFlags(byRefArgumentFlags,
                                            ByRefArgumentFlags.ArrayAsValue, true))
                                    {
                                        goto fixupReturnValue;
                                    }

                                    ///////////////////////////////////////////////////////////////////

                                    if (arrayAsLink || FlagOps.HasFlags(byRefArgumentFlags,
                                            ByRefArgumentFlags.ArrayAsLink, true))
                                    {
                                        if (interpreter.SetVariableSystemArray(
                                                variableFlags, parameterName,
                                                array, ref error) == ReturnCode.Ok)
                                        {
                                            continue;
                                        }
                                        else
                                        {
                                            return ReturnCode.Error;
                                        }
                                    }

                                    ///////////////////////////////////////////////////////////////////

                                    //
                                    // BUGFIX: When dealing with the "System.Object" type only,
                                    //         allow array values to override the pre-existing
                                    //         scalar variable value as long as the existing
                                    //         value within the scalar is logically -OR-
                                    //         physically a null value or an empty string.
                                    //
                                    if (isObject)
                                    {
                                        IVariable variable = null;

                                        if ((interpreter.DoesVariableExist(
                                                VariableFlags.NoElement | VariableFlags.Defined,
                                                parameterName, ref variable) == ReturnCode.Ok) &&
                                            IsScalarWithNullOrEmptyValue(interpreter, variable))
                                        {
                                            if (interpreter.UnsetVariable(
                                                    VariableFlags.None, parameterName,
                                                    ref error) != ReturnCode.Ok)
                                            {
                                                return ReturnCode.Error;
                                            }
                                        }
                                    }

                                    ///////////////////////////////////////////////////////////////////

                                    if (FixupByRefArray(
                                            interpreter, binder, cultureInfo, objectFlags, options,
                                            savedObjectOptionType, interpName, array, parameterName,
                                            marshalFlags, byRefArgumentFlags,
                                            create || FlagOps.HasFlags(byRefArgumentFlags,
                                                ByRefArgumentFlags.Create, true),
                                            dispose || FlagOps.HasFlags(byRefArgumentFlags,
                                                ByRefArgumentFlags.Dispose, true),
                                            alias || FlagOps.HasFlags(byRefArgumentFlags,
                                                ByRefArgumentFlags.Alias, true),
                                            aliasReference || FlagOps.HasFlags(byRefArgumentFlags,
                                                ByRefArgumentFlags.AliasReference, true),
                                            toString || FlagOps.HasFlags(byRefArgumentFlags,
                                                ByRefArgumentFlags.ToString, true),
                                            ref error) != ReturnCode.Ok)
                                    {
                                        return ReturnCode.Error;
                                    }

                                    continue;
                                }
                            }

                            ///////////////////////////////////////////////////////////////////////////

                        fixupReturnValue:

                            //
                            // NOTE: Translate the raw output argument value to a string
                            //       result (possibly a new opaque object handle).
                            //
                            Result result = null;

                            if (FixupReturnValue(
                                    interpreter, binder, cultureInfo, null, objectFlags,
                                    options, objectOptionType, null, interpName, arg,
                                    create || FlagOps.HasFlags(byRefArgumentFlags,
                                        ByRefArgumentFlags.Create, true),
                                    dispose || FlagOps.HasFlags(byRefArgumentFlags,
                                        ByRefArgumentFlags.Dispose, true),
                                    alias || FlagOps.HasFlags(byRefArgumentFlags,
                                        ByRefArgumentFlags.Alias, true),
                                    aliasReference || FlagOps.HasFlags(byRefArgumentFlags,
                                        ByRefArgumentFlags.AliasReference, true),
                                    toString || FlagOps.HasFlags(byRefArgumentFlags,
                                        ByRefArgumentFlags.ToString, true),
                                    ref result) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            ///////////////////////////////////////////////////////////////////////////

                            string value = result; // TODO: *REVIEW* Maybe use GetStringFromObject?

                            if (value == null)
                            {
                                //
                                // BUGFIX: If we were responsible for setting up an empty array
                                //         value and the output parameter ends up being null,
                                //         undo the value setup now, if possible.
                                //
                                VariableFlags localVariableFlags = variableFlags;

                                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                {
                                    IVariable variable = null;

                                    if (interpreter.GetVariableViaResolversWithSplit(
                                            (string)parameterName, ref localVariableFlags,
                                            ref variable) == ReturnCode.Ok)
                                    {
                                        if (EntityOps.IsLink(variable))
                                        {
                                            variable = EntityOps.FollowLinks(
                                                variable, localVariableFlags);
                                        }

                                        if (!EntityOps.IsUndefined(variable) &&
                                            EntityOps.IsArray2(variable))
                                        {
                                            ElementDictionary arrayValue = variable.ArrayValue;

                                            if ((arrayValue == null) ||
                                                (arrayValue.Count == 0))
                                            {
                                                //
                                                // NOTE: Remove the empty array value and make
                                                //       the variable into a scalar now.
                                                //
                                                variable.SetupValue(
                                                    null, true, false, true, true);
                                            }
                                        }
                                    }
                                }
                            }

                            ///////////////////////////////////////////////////////////////////////////

                            if (!FlagOps.HasFlags(
                                    byRefArgumentFlags, ByRefArgumentFlags.NoSetVariable, true))
                            {
                                //
                                // NOTE: This cannot be guaranteed to succeed:
                                //
                                //       1. The variable may be read-only.
                                //
                                //       2. If the variable has traces, they may opt to
                                //          override, cancel, or abort setting of the new
                                //          value.
                                //
                                //       3. The variable may be an array (i.e. and we are
                                //          trying to treat it as a scalar here).
                                //
                                if (interpreter.SetVariableValue(
                                        variableFlags, parameterName, value, null,
                                        ref error) != ReturnCode.Ok)
                                {
                                    return ReturnCode.Error;
                                }
                            }
                        }

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "invalid arguments";
                    }
                }
                else
                {
                    error = "invalid argument info";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FixupScalar(
            Interpreter interpreter,   /* in */
            IBinder binder,            /* in */
            OptionDictionary options,  /* in */
            CultureInfo cultureInfo,   /* in */
            Type type,                 /* in */
            ArgumentInfo argumentInfo, /* in */
            MarshalFlags marshalFlags, /* in */
            bool input,                /* in */
            bool output,               /* in */
            ref object arg,            /* in, out */
            ref Result error           /* out */
            )
        {
            if (interpreter != null)
            {
                if ((type != null) && !IsArrayType(type))
                {
                    if (arg != null)
                    {
                        object newArg = arg;

                        if (newArg is string)
                        {
                            Result value = null;
                            Result localError = null;

                            if (interpreter.GetVariableValue(
                                    VariableFlags.None, (string)newArg, ref value,
                                    ref localError) == ReturnCode.Ok)
                            {
                                /* need String, not Result */
                                newArg = value.String;

                                if (FixupValue(
                                        interpreter, binder, options, cultureInfo,
                                        type, argumentInfo, marshalFlags, input,
                                        output, ref newArg, ref error) == ReturnCode.Ok)
                                {
                                    arg = newArg;

                                    return ReturnCode.Ok;
                                }
                            }
                            else if (FlagOps.HasFlags(
                                    marshalFlags, MarshalFlags.DefaultValue, true))
                            {
                                //
                                // NOTE: The variable does not exist and we have been
                                //       told by the caller to use the default value
                                //       in that case.
                                //
                                arg = GetDefaultValue(type);

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = localError;
                            }
                        }
                        else
                        {
                            error = "variable name references must be of type string";
                        }
                    }
                    else
                    {
                        error = "variable name references cannot be null";
                    }
                }
                else
                {
                    if (type != null)
                        error = String.Format(
                            "type {0} cannot be an array type",
                            GetErrorTypeName(type));
                    else
                        error = "invalid type";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FixupArray(
            Interpreter interpreter,   /* in */
            IBinder binder,            /* in */
            OptionDictionary options,  /* in */
            CultureInfo cultureInfo,   /* in */
            Type type,                 /* in */
            Type elementType,          /* in */
            IVariable variable,        /* in */
            ArgumentInfo argumentInfo, /* in */
            MarshalFlags marshalFlags, /* in */
            bool input,                /* in */
            bool output,               /* in */
            ref object arg,            /* in, out */
            ref Result error           /* out */
            )
        {
            if (interpreter != null)
            {
                int rank = 0;

                if ((type != null) &&
                    (((variable != null) && IsArrayType(variable, ref rank, ref elementType)) ||
                    ((variable == null) && IsArrayType(type, ref rank, ref elementType))) &&
                    (rank > 0) && (elementType != null))
                {
                    if (input || output)
                    {
                        if (arg != null)
                        {
                            object newArg = arg;

                            if (newArg is string)
                            {
                                //
                                // FIXME: PRI 4: Variable traces will not be fired here because we are
                                //        accessing the array elements via the ArrayValues property and
                                //        not through the GetVariableValue method.
                                //
                                // BUGFIX: Make sure we do not end up with an alias to an array element.
                                //         At this point, there cannot be an array element name (via the
                                //         linked variable).  If there is one, that is always an error.
                                //
                                // BUGFIX: Make sure the variable is actually defined *IF* this is an
                                //         input argument.
                                //
                                VariableFlags variableFlags = GetArrayVariableFlags(input);

                                lock (interpreter.SyncRoot) /* TRANSACTIONAL */
                                {
                                    Result localError = null;

                                    if ((variable != null) || interpreter.GetVariableViaResolversWithSplit(
                                            (string)newArg, ref variableFlags,
                                            ref variable, ref localError) == ReturnCode.Ok)
                                    {
                                        if (EntityOps.IsLink(variable))
                                            variable = EntityOps.FollowLinks(variable, variableFlags);

                                        //
                                        // BUGFIX: If the variable is currently undefined AND we are going to
                                        //         allow it to be used anyhow (i.e. because it is output only),
                                        //         then make sure it has a valid value [if necessary].
                                        //
                                        if (!EntityOps.IsArray2(variable))
                                        {
                                            //
                                            // BUGFIX: Try to interpret the value of the variable as a well-known
                                            //         opaque object handle (which can only be a string). If the
                                            //         interpreter is null then the this step will have no effect.
                                            //         This should only be done if the handle has not already been
                                            //         looked up for this value.  If the resulting object is null,
                                            //         allow the variable name to be used.  Otherwise, forbid the
                                            //         variable name from being used and issue an appropriate
                                            //         error message.
                                            //
                                            // BUGFIX: Also, allow the variable value itself to be null here (i.e.
                                            //         not just the string value of "null", which refers to the
                                            //         opaque object handle that contains null).
                                            //
                                            object value = variable.Value;

                                            if ((value == null) || (!FlagOps.HasFlags(
                                                    marshalFlags, MarshalFlags.NoHandle, true) &&
                                                (value is string) &&
                                                (ArgumentInfo.QueryCount(argumentInfo, 0) <= 0)))
                                            {
                                                //
                                                // NOTE: The value of the variable may be null -OR- an
                                                //       opaque object handle that refers to null.  In
                                                //       either case, the variable is automatically fixed
                                                //       up to be an array.  Non-null variable values are
                                                //       not allowed for managed arrays.
                                                //
                                                if ((value == null) || GetObject(
                                                        interpreter, (string)value, marshalFlags,
                                                        input, output, ref value) == ReturnCode.Ok)
                                                {
                                                    //
                                                    // NOTE: We have now successfully looked up this object
                                                    //       handle.  Keep track of this fact so this task
                                                    //       is not repeated.
                                                    //
                                                    /* IGNORED */
                                                    ArgumentInfo.IncrementCount(argumentInfo, 0);

                                                    if (value == null)
                                                    {
                                                        //
                                                        // NOTE: *SPECIAL* The array itself is allowed to be
                                                        //       null for both input and output arguments.
                                                        //       Just return now.  Also, if this is an output
                                                        //       parameter, convert the variable into an array
                                                        //       now so that it can be populated later.
                                                        //
                                                        if (output && !FlagOps.HasFlags(marshalFlags,
                                                                MarshalFlags.SkipNullSetupValue, true))
                                                        {
                                                            variable.SetupValue(null, true, true, true, true);
                                                        }

                                                        arg = value;

                                                        return ReturnCode.Ok;
                                                    }
                                                    else if (!output)
                                                    {
                                                        if (IsSameReferenceType(
                                                                value.GetType(), type, marshalFlags))
                                                        {
                                                            //
                                                            // NOTE: *SPECIAL* If the argument is input only,
                                                            //       allow the opaque object handle to refer to
                                                            //       an array of the proper type and just use it
                                                            //       verbatim without any additional processing.
                                                            //
                                                            arg = value;

                                                            return ReturnCode.Ok;
                                                        }
                                                        else
                                                        {
                                                            error = String.Format(
                                                                "cannot convert from type {0} to type {1}",
                                                                GetErrorValueTypeName(value), GetErrorTypeName(type));

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        error = String.Format(
                                                            "cannot convert scalar variable containing object " +
                                                            "of type {0} to output array of type {1}",
                                                            GetErrorValueTypeName(value), GetErrorTypeName(type));

                                                        return ReturnCode.Error;
                                                    }
                                                }
                                            }
                                        }

                                        //
                                        // BUGFIX: Make sure the variable is actually defined *IF* this is an
                                        //         input argument.
                                        //
                                        if (EntityOps.IsArray(variable))
                                        {
                                            //
                                            // BUGFIX: If the variable is currently undefined AND we are going
                                            //         to allow it to be used anyhow (i.e. because it is output
                                            //         only), then make sure it has a valid value [if necessary].
                                            //
                                            if (EntityOps.IsUndefined(variable))
                                            {
                                                variable.SetupValue(null, true, true, true, false);
                                            }
                                            //
                                            // HACK: Unless forbidden from doing so, attempt to automatically
                                            //       detect and use an existing System.Array value within the
                                            //       linked variable.
                                            //
                                            else if (!FlagOps.HasFlags(
                                                    marshalFlags, MarshalFlags.NoSystemArray, true) &&
                                                interpreter.IsSystemArrayVariable(variable))
                                            {
                                                arg = EntityOps.GetSystemArray(variable);

                                                return ReturnCode.Ok;
                                            }

                                            int?[] lowerBounds = new int?[rank]; // NOTE: Populated in first pass.
                                            int?[] lengths = new int?[rank]; // NOTE: Populated in first pass.
                                            Array array = null; // NOTE: Created and populated in second pass.

                                            //
                                            // NOTE: The .NET Framework forces us to make multiple passes here
                                            //       because it lacks support for sparse arrays (specifically,
                                            //       we must know the "lengths" of each rank prior to creating
                                            //       or populating the array); therefore, to share the somewhat
                                            //       verbose index parsing and processing code, we artificially
                                            //       divide the work into two passes and identify the work to be
                                            //       done in the body based on the current pass.
                                            //
                                            // BUGFIX: Skip second pass for output-only parameters.
                                            //
                                            for (int pass = 0; pass < (input ? 2 : 1); pass++)
                                            {
                                                //
                                                // NOTE: We must examine and process each element of the script
                                                //       array variable to determine the maximum lengths of each
                                                //       rank (in the first pass) and the actual values to place
                                                //       into the managed array (in the second pass).
                                                //
                                                ElementDictionary arrayValue = variable.ArrayValue;

                                                foreach (string index in arrayValue.Keys)
                                                {
                                                    //
                                                    // NOTE: Ignore index expressions that are null or empty so
                                                    //       that scripts may place out-of-band data about the
                                                    //       array there without causing an error.
                                                    //
                                                    if (!String.IsNullOrEmpty(index))
                                                    {
                                                        //
                                                        // FIXME: Currently, we hard-code this to look for comma
                                                        //        separated indexes.  This is low-priority because
                                                        //        this is considered to be the de-facto standard
                                                        //        for "multi-dimensional" script array indexing.
                                                        //
                                                        string[] subIndexes = index.Split(Characters.Comma);

                                                        if (subIndexes != null)
                                                        {
                                                            int subIndexesLength = subIndexes.Length;

                                                            if (subIndexesLength == rank)
                                                            {
                                                                int[] indexes = new int[rank];

                                                                for (int rankIndex = 0; rankIndex < rank; rankIndex++)
                                                                {
                                                                    if (Value.GetInteger2(
                                                                            subIndexes[rankIndex], ValueFlags.AnyInteger,
                                                                            cultureInfo, ref indexes[rankIndex],
                                                                            ref error) == ReturnCode.Ok)
                                                                    {
                                                                        //
                                                                        // NOTE: Setup lower bounds and lengths
                                                                        //       on first pass only.
                                                                        //
                                                                        if (pass == 0)
                                                                        {
                                                                            int rankLowerBound = indexes[rankIndex];

                                                                            if ((lowerBounds[rankIndex] == null) ||
                                                                                (rankLowerBound < lowerBounds[rankIndex]))
                                                                            {
                                                                                lowerBounds[rankIndex] = rankLowerBound;
                                                                            }

                                                                            int rankLength = indexes[rankIndex] -
                                                                                (int)lowerBounds[rankIndex] + 1;

                                                                            if ((lengths[rankIndex] == null) ||
                                                                                (rankLength > lengths[rankIndex]))
                                                                            {
                                                                                lengths[rankIndex] = rankLength;
                                                                            }
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        return ReturnCode.Error;
                                                                    }
                                                                }

                                                                if (pass == 1)
                                                                {
                                                                    object value = arrayValue[index];

                                                                    if (elementType.IsArray)
                                                                    {
                                                                        Type subElementType = elementType.GetElementType();

                                                                        if (FixupArray(
                                                                                interpreter, binder, options, cultureInfo, elementType,
                                                                                subElementType, null, argumentInfo, marshalFlags,
                                                                                input, output, ref value, ref error) != ReturnCode.Ok)
                                                                        {
                                                                            return ReturnCode.Error;
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        /* IGNORED */
                                                                        ArgumentInfo.ResetCount(argumentInfo, 0);

                                                                        if (FixupValue(
                                                                                interpreter, binder, options, cultureInfo, elementType,
                                                                                argumentInfo, marshalFlags, input, output, ref value,
                                                                                ref error) != ReturnCode.Ok)
                                                                        {
                                                                            return ReturnCode.Error;
                                                                        }
                                                                    }

#if DEBUG && MONO
                                                                    //
                                                                    // FIXME: Remove this when Mono fixes this bug.
                                                                    //
                                                                    IntList indexList = new IntList(indexes);

                                                                    TraceOps.DebugTrace(String.Format(
                                                                        "FixupArray: index list is {0}",
                                                                        FormatOps.WrapOrNull(indexList)),
                                                                        typeof(MarshalOps).Name,
                                                                        TracePriority.MarshalDebug);
#endif

                                                                    //
                                                                    // NOTE: Finally, set the array element.
                                                                    //
                                                                    array.SetValue(value, indexes);
                                                                }
                                                            }
                                                            else
                                                            {
                                                                error = String.Format(
                                                                    "array index {0} appears to be of rank {1} " +
                                                                    "and array is of rank {2}",
                                                                    FormatOps.WrapOrNull(index), subIndexesLength,
                                                                    rank);

                                                                return ReturnCode.Error;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            error = String.Format(
                                                                "could not parse array index {0}",
                                                                FormatOps.WrapOrNull(index));

                                                            return ReturnCode.Error;
                                                        }
                                                    }
                                                }

                                                //
                                                // NOTE: After the first pass is complete, validate the lengths
                                                //       and create the array (now that we know the final lengths).
                                                //
                                                if (pass == 0)
                                                {
                                                    for (int rankIndex = 0; rankIndex < rank; rankIndex++)
                                                    {
                                                        if (lengths[rankIndex] < 0)
                                                        {
                                                            error = String.Format(
                                                                "invalid length {0} for array rank {1}",
                                                                lengths[rankIndex], rankIndex + 1);

                                                            return ReturnCode.Error;
                                                        }
                                                    }

                                                    array = Array.CreateInstance(
                                                        GetArrayElementType(elementType),
                                                        ArrayOps.ToNonNullable<int>(lengths, 0),
                                                        ArrayOps.ToNonNullable<int>(lowerBounds, 0));
                                                }
                                            }

                                            //
                                            // NOTE: If we get here, the array should be fully populated.
                                            //
                                            arg = array;

                                            return ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            error = String.Format(
                                                "can't read {0}: variable isn't array",
                                                FormatOps.ErrorVariableName(
                                                    variable, null, (string)newArg, null));
                                        }
                                    }
                                    //
                                    // NOTE: *SPECIAL CASE* Convert a string to an one-dimensional
                                    //       array of characters?
                                    //
                                    else if (FlagOps.HasFlags(variableFlags, VariableFlags.NotFound, true) &&
                                        (rank == 1) &&
                                        (elementType == typeof(char)))
                                    {
                                        arg = ((string)newArg).ToCharArray();

                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        error = localError;
                                    }
                                }
                            }
                            //
                            // NOTE: *SPECIAL CASE* Convert a list of strings into a one-dimensional
                            //       array of the given type.
                            //
                            else if (newArg is StringList)
                            {
                                if (rank == 1)
                                {
                                    StringList list = (StringList)newArg;

                                    Array array = Array.CreateInstance(
                                        GetArrayElementType(elementType),
                                        list.Count);

                                    for (int index = 0; index < list.Count; index++)
                                    {
                                        object value = list[index];

                                        if (elementType.IsArray)
                                        {
                                            Type subElementType = elementType.GetElementType();

                                            if (FixupArray(
                                                    interpreter, binder, options, cultureInfo, elementType,
                                                    subElementType, null, argumentInfo, marshalFlags,
                                                    input, output, ref value, ref error) != ReturnCode.Ok)
                                            {
                                                return ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            /* IGNORED */
                                            ArgumentInfo.ResetCount(argumentInfo, 0);

                                            if (FixupValue(
                                                    interpreter, binder, options, cultureInfo, elementType,
                                                    argumentInfo, marshalFlags, input, output, ref value,
                                                    ref error) != ReturnCode.Ok)
                                            {
                                                return ReturnCode.Error;
                                            }
                                        }

                                        //
                                        // NOTE: We were able to convert the element to the requested
                                        //       type.  Place it in the array at the same index.
                                        //
                                        array.SetValue(value, index);
                                    }

                                    //
                                    // NOTE: If we get here, the array should be fully populated.
                                    //
                                    arg = array;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "cannot convert list to an array of rank {0}",
                                        rank);
                                }
                            }
                            else
                            {
                                error = "variable name references must be of type string";
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Arrays arguments are allowed to be null.
                            //
                            return ReturnCode.Ok;
                        }
                    }
                    else
                    {
                        error = "array must be input and/or output";
                    }
                }
                else
                {
                    if (type != null)
                        error = String.Format(
                            "type {0} is not an array type or has an invalid rank",
                            GetErrorTypeName(type));
                    else
                        error = "invalid type";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
        private static bool NeedSingleFormat(
            float value
            )
        {
            return (value == float.MinValue) || (value == float.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool NeedDoubleFormat(
            double value
            )
        {
            return (value == double.MinValue) || (value == double.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string FixupDataValue(
            object value,
            DateTimeBehavior dateTimeBehavior,
            DateTimeKind dateTimeKind,
            string dateTimeFormat,
            string @default
            )
        {
            if (value is string)
            {
                return (string)value;
            }
            else if (value is byte[])
            {
                return new ByteList((byte[])value).ToString();
            }
            else if (value is DateTime)
            {
                DateTime dateTime = (DateTime)value;

                if ((dateTimeKind != DateTimeKind.Unspecified) &&
                    (dateTime.Kind != DateTimeKind.Unspecified) &&
                    (dateTime.Kind != dateTimeKind))
                {
                    if (dateTimeKind == DateTimeKind.Utc)
                        dateTime = dateTime.ToUniversalTime();
                    else if (dateTimeKind == DateTimeKind.Local)
                        dateTime = dateTime.ToLocalTime();
                }

                switch (dateTimeBehavior)
                {
                    case DateTimeBehavior.Ticks:
                        {
                            return dateTime.Ticks.ToString();
                        }
                    case DateTimeBehavior.Seconds:
                        {
                            long seconds = 0;

                            if (TimeOps.DateTimeToSeconds(
                                    ref seconds, dateTime,
                                    TimeOps.UnixEpoch))
                            {
                                return seconds.ToString();
                            }
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "FixupDataValue: failed to get " +
                                    "seconds from the epoch {0} to {1}",
                                    TimeOps.UnixEpoch, dateTime),
                                    typeof(MarshalOps).Name,
                                    TracePriority.MarshalError);
                            }
                            break;
                        }
                    case DateTimeBehavior.ToString:
                        {
                            return (dateTimeFormat != null) ?
                                dateTime.ToString(dateTimeFormat) :
                                dateTime.ToString();
                        }
                }
            }
            else if (value is float)
            {
                float floatValue = (float)value;

                if (SingleDataFormat != null)
                {
                    return floatValue.ToString(SingleDataFormat);
                }
                else if ((DefaultSingleDataFormat != null) &&
                    NeedSingleFormat(floatValue))
                {
                    return floatValue.ToString(DefaultSingleDataFormat);
                }

                return floatValue.ToString();
            }
            else if (value is double)
            {
                double doubleValue = (double)value;

                if (DoubleDataFormat != null)
                {
                    return doubleValue.ToString(DoubleDataFormat);
                }
                else if ((DefaultDoubleDataFormat != null) &&
                    NeedDoubleFormat(doubleValue))
                {
                    return doubleValue.ToString(DefaultDoubleDataFormat);
                }

                return doubleValue.ToString();
            }
            else if ((value != null) && (value != DBNull.Value))
            {
                return value.ToString();
            }

            return @default;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Result ToStringOrDefaultReturnValue(
            object value
            )
        {
            if (value == null)
                return DefaultObjectName;

            return value.ToString();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetErrorTypeName(
            object value
            )
        {
            if (value == null)
                return FormatOps.DisplayNull;

            return GetErrorTypeName(value.GetType());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetErrorTypeName(
            Type type
            )
        {
            if (type == null)
                return FormatOps.DisplayNull;

            return FormatOps.WrapOrNull(type.FullName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetErrorValueTypeName(
            object value
            )
        {
            if (value == null)
                return FormatOps.DisplayNull;

            return GetErrorTypeName(value.GetType());
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Type GetValueTypeOrObjectType(
            object value
            )
        {
            if (value == null)
                return typeof(object);

            return value.GetType();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FixupValue(
            Interpreter interpreter,   /* in */
            IBinder binder,            /* in */
            OptionDictionary options,  /* in */
            CultureInfo cultureInfo,   /* in */
            Type type,                 /* in */
            ArgumentInfo argumentInfo, /* in */
            MarshalFlags marshalFlags, /* in */
            bool input,                /* in */
            bool output,               /* in */
            ref object arg,            /* in, out */
            ref Result error           /* out */
            )
        {
            if (interpreter != null)
            {
                if ((type != null) && !IsArrayType(type))
                {
                    if (arg != null)
                    {
                        object newArg = arg;

                        //
                        // NOTE: First, try to interpret the string as a well-known
                        //       opaque object handle (which can only be a string).
                        //       If the interpreter is null then the this step will
                        //       have no effect.  This should only be done if the
                        //       handle has not already been looked up for this
                        //       value.
                        //
                        if (!FlagOps.HasFlags(
                                marshalFlags, MarshalFlags.NoHandle, true) &&
                            (newArg is string) &&
                            (ArgumentInfo.QueryCount(argumentInfo, 0) <= 0))
                        {
                            if (GetObject(
                                    interpreter, (string)newArg, marshalFlags,
                                    input, output, ref newArg) == ReturnCode.Ok)
                            {
                                //
                                // NOTE: We have now successfully looked up this
                                //       object handle.  Keep track of this fact
                                //       so this task is not repeated.
                                //
                                /* IGNORED */
                                ArgumentInfo.IncrementCount(argumentInfo, 0);
                            }
                        }

                        //
                        // NOTE: Next, try to interpet our [possibly new] argument
                        //       string as one of the "primitive" types that can
                        //       parse and understand, in ascending order of size.
                        //
                        if (newArg != null)
                        {
                            if (IsValueType(type, output))
                            {
                                //
                                // NOTE: Special case for value types that are baked
                                //       into an object reference, we do not want to
                                //       attempt any further unnecessary conversions
                                //       on them (e.g. System.IntPtr, etc).  Also, we
                                //       explicitly allow nullable types here even
                                //       though in this case it does not matter since
                                //       we know the value is not null.
                                //
                                if (IsSameValueType(newArg.GetType(), type, output))
                                {
                                    arg = newArg;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    if (binder != null)
                                    {
                                        //
                                        // NOTE: Ok, it does not appear to be convertible to one of our
                                        //       built-in value types; however, it may still be valid.
                                        //       Here, we just pass it off to the ScriptBinder.
                                        //
                                        try
                                        {
                                            //
                                            // NOTE: First, try to use their selected binder to convert the
                                            //       value.
                                            //
                                            // HACK: Currently, this basically assumes that their
                                            //       "selected binder" is really the ScriptBinder that can
                                            //       only actually convert values from strings to their
                                            //       appropriate type.
                                            //
                                            // NOTE: In the future, we may not want to unconditionally call
                                            //       ToString here on the candidate value, especially for
                                            //       reference types.  In that case, the ScriptBinder would
                                            //       need to be adapted to handle conversions that are not
                                            //       always from a string.
                                            //
                                            MarshalClientData marshalClientData;
                                            object value;

                                            if ((binder is IScriptBinder) && !FlagOps.HasFlags(
                                                    marshalFlags, MarshalFlags.NoScriptBinder, true))
                                            {
                                                marshalClientData = new MarshalClientData(
                                                    newArg.ToString(), options, marshalFlags, ReturnCode.Ok,
                                                    error);

                                                value = marshalClientData;
                                            }
                                            else
                                            {
                                                marshalClientData = null;
                                                value = newArg.ToString();
                                            }

                                            newArg = binder.ChangeType(value, type, cultureInfo);

                                            if (marshalClientData != null)
                                                marshalFlags = marshalClientData.MarshalFlags;

                                            //
                                            // HACK: If the selected binder did not actually manage to
                                            //       convert the value to the appropriate type and it did
                                            //       not throw an exception (i.e. the ScriptBinder), throw
                                            //       one on its behalf.
                                            //
                                            if (!FlagOps.HasFlags(marshalFlags,
                                                    MarshalFlags.SkipValueTypeCheck, true) &&
                                                ((newArg == null) ||
                                                !IsSameValueType(newArg.GetType(), type, output)))
                                            {
                                                throw new ScriptException(String.Format(
                                                    "value type mismatch, type {0} is not " +
                                                    "compatible with type {1}",
                                                    GetErrorValueTypeName(newArg), GetErrorTypeName(type)));
                                            }

                                            //
                                            // NOTE: Make sure that the marshal client data return code, if
                                            //       any, is still Ok; if not, we have a problem.
                                            //
                                            ReturnCode code;

                                            if (marshalClientData != null)
                                            {
                                                code = marshalClientData.ReturnCode;

                                                if (code == ReturnCode.Ok)
                                                    arg = newArg;
                                                else
                                                    error = marshalClientData.Result;
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: With binders that are not IScriptBinder compatible,
                                                //       if we get to this point, we MUST have succeeded.
                                                //
                                                code = ReturnCode.Ok;
                                            }

                                            return code;
                                        }
                                        catch
                                        {
                                            // do nothing.
                                        }
                                    }

                                    //
                                    // HACK: With the new modifications to the ScriptBinder to handle
                                    //       reference and nullable types, we should no longer have
                                    //       any real reasons to reach this point; however, we can
                                    //       abuse the built-in ChangeType to generate an exception
                                    //       explaining why the value cannot be converted.
                                    //
                                    try
                                    {
                                        //
                                        // NOTE: Next, fallback to the default handling of using the
                                        //       built-in ChangeType (typically used for primitive
                                        //       value conversions or trivial reference conversions
                                        //       only).
                                        //
                                        arg = Convert.ChangeType(newArg, type);

                                        return ReturnCode.Ok;
                                    }
                                    catch (Exception e)
                                    {
                                        error = e;
                                    }
                                }
                            }
                            else if (IsSameReferenceType(
                                    newArg.GetType(), type, marshalFlags, output))
                            {
                                arg = newArg;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                //
                                // NOTE: Don't bother trying to do anything else at this point if
                                //       the type to convert to is string because we implicitly
                                //       "convert" everything to string prior to passing it into
                                //       the selected binder (i.e. the ScriptBinder) as it only
                                //       "knows" how to convert to other types starting from a
                                //       string.
                                //
                                if ((binder != null) && (type != typeof(string)) &&
                                    (FlagOps.HasFlags(marshalFlags, MarshalFlags.StringList, true) ||
                                    !IsStringListForChangeType(binder, type)))
                                {
                                    try
                                    {
                                        //
                                        // NOTE: First, try to use their selected binder to convert the
                                        //       value to whatever this reference type is.
                                        //
                                        // HACK: Currently, this basically assumes that their
                                        //       "selected binder" is really the ScriptBinder that can
                                        //       only actually convert values from strings to their
                                        //       appropriate type.
                                        //
                                        // NOTE: In the future, we may not want to unconditionally call
                                        //       ToString here on the candidate value, especially for
                                        //       reference types.  In that case, the ScriptBinder would
                                        //       need to be adapted to handle conversions that are not
                                        //       always from a string.
                                        //
                                        MarshalClientData marshalClientData;
                                        object value;

                                        if ((binder is IScriptBinder) && !FlagOps.HasFlags(
                                                marshalFlags, MarshalFlags.NoScriptBinder, true))
                                        {
                                            marshalClientData = new MarshalClientData(
                                                newArg.ToString(), options, marshalFlags, ReturnCode.Ok,
                                                error);

                                            value = marshalClientData;
                                        }
                                        else
                                        {
                                            marshalClientData = null;
                                            value = newArg.ToString();
                                        }

                                        newArg = binder.ChangeType(value, type, cultureInfo);

                                        if (marshalClientData != null)
                                            marshalFlags = marshalClientData.MarshalFlags;

                                        //
                                        // HACK: If the selected binder did not actually manage to
                                        //       convert the value to the appropriate type and it did
                                        //       not throw an exception (i.e. the ScriptBinder), throw
                                        //       one on its behalf.
                                        //
                                        if (!FlagOps.HasFlags(marshalFlags,
                                                MarshalFlags.SkipReferenceTypeCheck, true) &&
                                            (newArg != null) && !IsSameReferenceType(
                                                newArg.GetType(), type, marshalFlags, output))
                                        {
                                            throw new ScriptException(String.Format(
                                                "reference type mismatch, type {0} is not " +
                                                "compatible with type {1}",
                                                GetErrorValueTypeName(newArg), GetErrorTypeName(type)));
                                        }

                                        //
                                        // NOTE: Make sure that the marshal client data return code, if
                                        //       any, is still Ok; if not, we have a problem.
                                        //
                                        ReturnCode code;

                                        if (marshalClientData != null)
                                        {
                                            code = marshalClientData.ReturnCode;

                                            if (code == ReturnCode.Ok)
                                                arg = newArg;
                                            else
                                                error = marshalClientData.Result;
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: With binders that are not IScriptBinder compatible,
                                            //       if we get to this point, we MUST have succeeded.
                                            //
                                            code = ReturnCode.Ok;
                                        }

                                        return code;
                                    }
                                    catch (Exception e)
                                    {
                                        error = e;
                                    }
                                }
                                else
                                {
                                    error = String.Format(
                                        "cannot convert from type {0} to type {1}",
                                        GetErrorValueTypeName(newArg), GetErrorTypeName(type));
                                }
                            }
                        }
                        else
                        {
                            if (IsValueType(type) && !IsNullableType(type, output))
                            {
                                error = String.Format(
                                    "argument of value type {0} cannot be null",
                                    GetErrorTypeName(type));
                            }
                            else
                            {
                                arg = newArg;

                                return ReturnCode.Ok;
                            }
                        }
                    }
                    else
                    {
                        if (IsValueType(type) && !IsNullableType(type, output))
                        {
                            error = String.Format(
                                "argument of value type {0} cannot be null",
                                GetErrorTypeName(type));
                        }
                        else
                        {
                            return ReturnCode.Ok;
                        }
                    }
                }
                else
                {
                    if (type != null)
                        error = String.Format(
                            "type {0} cannot be an array type",
                            GetErrorTypeName(type));
                    else
                        error = "invalid type";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode FixupArgument(
            Interpreter interpreter,   /* in */
            IBinder binder,            /* in */
            OptionDictionary options,  /* in */
            CultureInfo cultureInfo,   /* in */
            Type type,                 /* in */
            ArgumentInfo argumentInfo, /* in */
            MarshalFlags marshalFlags, /* in */
            bool input,                /* in */
            bool output,               /* in */
            ref object arg,            /* in, out */
            ref Result error           /* out */
            )
        {
            if (interpreter != null)
            {
                Type elementType = null;

                if ((type != null) &&
                    (!IsArrayType(type, ref elementType) || (elementType != null)))
                {
                    if (input || output)
                    {
                        if (arg != null)
                        {
                            object newArg = arg;

                            //
                            // NOTE: First, try to interpret the string as a well-known
                            //       opaque object handle (which can only be a string).
                            //       If the interpreter is null then the this step will
                            //       have no effect.  This should only be done if the
                            //       handle has not already been looked up for this
                            //       value.
                            //
                            if (!FlagOps.HasFlags(
                                    marshalFlags, MarshalFlags.NoHandle, true) &&
                                (newArg is string) &&
                                (ArgumentInfo.QueryCount(argumentInfo, 0) <= 0))
                            {
                                if (GetObject(
                                        interpreter, (string)newArg, marshalFlags,
                                        input, output, ref newArg) == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: We have now successfully looked up this
                                    //       object handle.  Keep track of this fact
                                    //       so this task is not repeated.
                                    //
                                    /* IGNORED */
                                    ArgumentInfo.IncrementCount(argumentInfo, 0);
                                }
                            }

                            //
                            // NOTE: Next, try to interpet our [possibly new] argument
                            //       string as one of the "primitive" types that can
                            //       parse and understand, in ascending order of size.
                            //
                            if (newArg != null)
                            {
                                if (output)
                                {
                                    //
                                    // NOTE: The argument value must be a script variable name;
                                    //       therefore, it must be a string.
                                    //
                                    if (newArg is string)
                                    {
                                        //
                                        // NOTE: If the argument is used for input and output (ref),
                                        //       we need to make sure the argument refers to an existing
                                        //       script variable.
                                        //
                                        if (input)
                                        {
                                            //
                                            // NOTE: Argument is input-output.
                                            //
                                            if (IsArrayType(type))
                                            {
                                                //
                                                // NOTE: The variable name must refer to an existing script
                                                //       array variable.
                                                //
                                                if (FixupArray(
                                                        interpreter, binder, options, cultureInfo, type,
                                                        elementType, null, argumentInfo, marshalFlags,
                                                        input, output, ref newArg, ref error) == ReturnCode.Ok)
                                                {
                                                    arg = newArg;

                                                    return ReturnCode.Ok;
                                                }
                                            }
                                            else
                                            {
                                                IVariable variable = null;

                                                if (IsObjectType(type, true) && IsArray(
                                                        interpreter, GetArrayVariableFlags(input),
                                                        (string)newArg, ref variable))
                                                {
                                                    //
                                                    // NOTE: The variable name must refer to an existing script
                                                    //       array variable.
                                                    //
                                                    if (FixupArray(
                                                            interpreter, binder, options, cultureInfo, type,
                                                            elementType, variable, argumentInfo, marshalFlags,
                                                            input, output, ref newArg, ref error) == ReturnCode.Ok)
                                                    {
                                                        arg = newArg;

                                                        return ReturnCode.Ok;
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: The variable name must refer to an existing script
                                                    //       scalar variable.
                                                    //
                                                    if (FixupScalar(
                                                            interpreter, binder, options, cultureInfo,
                                                            type, argumentInfo, marshalFlags, input,
                                                            output, ref newArg, ref error) == ReturnCode.Ok)
                                                    {
                                                        arg = newArg;

                                                        return ReturnCode.Ok;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: Argument is output-only.
                                            //
                                            if (IsArrayType(type))
                                            {
                                                //
                                                // NOTE: If the variable name exists, it must not refer to
                                                //       a script scalar variable.
                                                //
                                                if (interpreter.DoesVariableExist(VariableFlags.NoElement,
                                                        (string)newArg) == ReturnCode.Ok)
                                                {
                                                    if (FixupArray(
                                                            interpreter, binder, options, cultureInfo,
                                                            type, elementType, null, argumentInfo,
                                                            marshalFlags, input, output, ref newArg,
                                                            ref error) == ReturnCode.Ok)
                                                    {
                                                        arg = newArg;

                                                        return ReturnCode.Ok;
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: The variable does not exist.  This is allowed
                                                    //       because it is output-only.  We simply clear out
                                                    //       the argument value and return.
                                                    //
                                                    arg = GetDefaultValue(type);

                                                    return ReturnCode.Ok;
                                                }
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: If the variable name exists, it must not refer to
                                                //       a script array variable.
                                                //
                                                if (interpreter.DoesVariableExist(VariableFlags.None,
                                                        (string)newArg) == ReturnCode.Ok)
                                                {
                                                    if (FixupScalar(
                                                            interpreter, binder, options, cultureInfo,
                                                            type, argumentInfo, marshalFlags, input,
                                                            output, ref newArg, ref error) == ReturnCode.Ok)
                                                    {
                                                        arg = newArg;

                                                        return ReturnCode.Ok;
                                                    }
                                                }
                                                else
                                                {
                                                    //
                                                    // NOTE: The variable does not exist.  This is allowed
                                                    //       because it is output-only.  We simply clear out
                                                    //       the argument value and return.
                                                    //
                                                    arg = GetDefaultValue(type);

                                                    return ReturnCode.Ok;
                                                }
                                            }
                                        }
                                    }
                                    else if (IsSameReferenceType(
                                            newArg.GetType(), type, marshalFlags, output))
                                    {
                                        //
                                        // NOTE: The argument is already of the correct [array] type.
                                        //
                                        arg = newArg;

                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: This is an error because output arguments must refer
                                        //       to a script variable by name (which is always a string).
                                        //
                                        error = "variable name references must be of type string";
                                    }
                                }
                                else
                                {
                                    //
                                    // NOTE: Argument is input-only.
                                    //
                                    if (IsArrayType(type))
                                    {
                                        //
                                        // NOTE: We do not support passing arrays by value; therefore,
                                        //       this argument value must be an existing variable name
                                        //       that refers to a script array variable with elements
                                        //       that can be coerced to the appropriate type.
                                        //
                                        if (newArg is string)
                                        {
                                            //
                                            // BUGFIX: *SPECIAL* When resolving member arguments, we do
                                            //         NOT want to try to interpret the argument value
                                            //         as an array variable name is they provided us an
                                            //         actual string and the destination type is a
                                            //         character array.
                                            //
                                            if (type == typeof(char[]))
                                            {
                                                arg = newArg.ToString().ToCharArray();

                                                return ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                //
                                                // NOTE: If the variable name exists, it must not refer to
                                                //       a script scalar variable.
                                                //
                                                if (interpreter.DoesVariableExist(VariableFlags.NoElement,
                                                        (string)newArg) == ReturnCode.Ok)
                                                {
                                                    if (FixupArray(
                                                            interpreter, binder, options, cultureInfo,
                                                            type, elementType, null, argumentInfo,
                                                            marshalFlags, input, output, ref newArg,
                                                            ref error) == ReturnCode.Ok)
                                                    {
                                                        arg = newArg;

                                                        return ReturnCode.Ok;
                                                    }
                                                }
                                                else
                                                {
                                                    StringList list = null;

                                                    if (Parser.SplitList(
                                                            interpreter, (string)newArg, 0,
                                                            Length.Invalid, true, ref list,
                                                            ref error) == ReturnCode.Ok)
                                                    {
                                                        newArg = list;

                                                        if (FixupArray(
                                                                interpreter, binder, options, cultureInfo,
                                                                type, elementType, null, argumentInfo,
                                                                marshalFlags, input, output, ref newArg,
                                                                ref error) == ReturnCode.Ok)
                                                        {
                                                            arg = newArg;

                                                            return ReturnCode.Ok;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else if (IsSameReferenceType(
                                                newArg.GetType(), type, marshalFlags, output))
                                        {
                                            //
                                            // NOTE: The argument is already of the correct [array] type.
                                            //
                                            arg = newArg;

                                            return ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            //
                                            // NOTE: This is an error because output arguments must refer
                                            //       to a script variable by name (which is always a string).
                                            //
                                            error = "variable name references must be of type string";
                                        }
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: Argument is input-only, non-array.
                                        //
                                        if (FixupValue(
                                                interpreter, binder, options, cultureInfo,
                                                type, argumentInfo, marshalFlags, input,
                                                output, ref newArg, ref error) == ReturnCode.Ok)
                                        {
                                            arg = newArg;

                                            return ReturnCode.Ok;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (IsValueType(type) && !IsNullableType(type, output))
                                {
                                    error = String.Format(
                                        "argument of value type {0} cannot be null",
                                        GetErrorTypeName(type));
                                }
                                else
                                {
                                    arg = newArg;

                                    return ReturnCode.Ok;
                                }
                            }
                        }
                        else
                        {
                            if (IsValueType(type) && !IsNullableType(type, output))
                            {
                                error = String.Format(
                                    "argument of value type {0} cannot be null",
                                    GetErrorTypeName(type));
                            }
                            else
                            {
                                return ReturnCode.Ok;
                            }
                        }
                    }
                    else
                    {
                        error = "argument must be input and/or output";
                    }
                }
                else
                {
                    if (type != null)
                        error = String.Format(
                            "type {0} is an array type with missing element type",
                            GetErrorTypeName(type));
                    else
                        error = "invalid type";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
    }
}
