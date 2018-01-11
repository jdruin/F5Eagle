/*
 * DataOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Data;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("2e72f5b2-15df-4d65-98ec-fa01f3300ac8")]
    internal static class DataOps
    {
        #region Synchronization Objects
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static DbConnectionTypeStringDictionary DbConnectionTypeFullNames;
        private static DbConnectionTypeStringDictionary DbConnectionTypeNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Data Support Methods
        public static DbConnectionTypeStringDictionary
                GetDbConnectionTypeNames()
        {
            DbConnectionTypeStringDictionary result =
                new DbConnectionTypeStringDictionary();

            result.Add(DbConnectionType.None,
                typeof(object).AssemblyQualifiedName);

            result.Add(DbConnectionType.Odbc,
                typeof(OdbcConnection).AssemblyQualifiedName);

            result.Add(DbConnectionType.OleDb,
                typeof(OleDbConnection).AssemblyQualifiedName);

            result.Add(DbConnectionType.Sql,
                typeof(SqlConnection).AssemblyQualifiedName);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static DbConnectionTypeStringDictionary
                GetOtherDbConnectionTypeNames(bool full, bool copy)
        {
            lock (syncRoot)
            {
                //
                // NOTE: One-time initialization, these are not per-interpreter
                //       datums and never change.
                //
                if (DbConnectionTypeFullNames == null)
                {
                    DbConnectionTypeFullNames =
                        new DbConnectionTypeStringDictionary();

                    //
                    // NOTE: This type name is optional because it requires the
                    //       System.Data.OracleClient assembly to be loaded.
                    //
                    DbConnectionTypeFullNames.Add(DbConnectionType.Oracle,
                        "System.Data.OracleClient.OracleConnection, " +
                        "System.Data.OracleClient, Version=2.0.0.0, " +
                        "Culture=neutral, PublicKeyToken=b77a5c561934e089");

                    //
                    // NOTE: This type name is optional because it requires the
                    //       .NET Framework v3.5 (SP1 or higher?) to be installed.
                    //
                    DbConnectionTypeFullNames.Add(DbConnectionType.SqlCe,
                        "System.Data.SqlServerCe.SqlCeConnection, " +
                        "System.Data.SqlServerCe, Version=3.5.1.0, " +
                        "Culture=neutral, PublicKeyToken=89845dcd8080cc91");

                    //
                    // NOTE: This type name is optional because it requires
                    //       the System.Data.SQLite assembly to be loaded
                    //       (i.e. from "https://system.data.sqlite.org/" OR
                    //       "https://sf.net/projects/sqlite-dotnet2/").
                    //
                    DbConnectionTypeFullNames.Add(DbConnectionType.SQLite,
                        "System.Data.SQLite.SQLiteConnection, " +
                        "System.Data.SQLite, Version=1.0, " +
                        "Culture=neutral, PublicKeyToken=db937bc2d44ff139");
                }

                if (DbConnectionTypeNames == null)
                {
                    DbConnectionTypeNames =
                        new DbConnectionTypeStringDictionary();

                    //
                    // NOTE: This type name is optional because it requires
                    //       the System.Data.SQLite assembly to be loaded
                    //       (i.e. from "https://system.data.sqlite.org/" OR
                    //       "https://sf.net/projects/sqlite-dotnet2/").
                    //
                    DbConnectionTypeNames.Add(DbConnectionType.SQLite,
                        "System.Data.SQLite.SQLiteConnection");
                }

                if (copy)
                {
                    return new DbConnectionTypeStringDictionary(full ?
                        DbConnectionTypeFullNames : DbConnectionTypeNames);
                }
                else
                {
                    return full ?
                        DbConnectionTypeFullNames : DbConnectionTypeNames;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode CreateOtherDbConnection(
            Interpreter interpreter,
            DbConnectionType dbConnectionType,
            string connectionString,
            string typeFullName,
            string typeName,
            ValueFlags valueFlags,
            ref IDbConnection connection,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(typeFullName) &&
                String.IsNullOrEmpty(typeName))
            {
                error = String.Format(
                    "invalid type name for database connection type \"{0}\"",
                    dbConnectionType);

                return ReturnCode.Error;
            }

            ResultList errors = null;

            foreach (string thisTypeName in new string[] {
                    typeFullName, typeName })
            {
                if (!String.IsNullOrEmpty(thisTypeName))
                {
                    try
                    {
                        Type type = null;

                        if (Value.GetType(
                                interpreter, thisTypeName, null,
                                interpreter.GetAppDomain(), valueFlags,
                                interpreter.CultureInfo, ref type,
                                ref errors) == ReturnCode.Ok)
                        {
                            connection = (IDbConnection)Activator.CreateInstance(
                                type, new object[] { connectionString });

                            return ReturnCode.Ok;
                        }
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }
            }

            error = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,
            DbConnectionType dbConnectionType,
            string connectionString,
            string typeFullName,
            string typeName,
            ValueFlags valueFlags,
            ref IDbConnection connection,
            ref Result error
            )
        {
            return CreateDbConnection(interpreter, dbConnectionType,
                connectionString, typeFullName, typeName, valueFlags,
                GetOtherDbConnectionTypeNames(true, false),
                GetOtherDbConnectionTypeNames(false, false),
                ref connection, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,
            DbConnectionType dbConnectionType,
            string connectionString,
            string typeFullName,
            string typeName,
            ValueFlags valueFlags,
            DbConnectionTypeStringDictionary dbConnectionTypeFullNames,
            DbConnectionTypeStringDictionary dbConnectionTypeNames,
            ref IDbConnection connection,
            ref Result error
            )
        {
            try
            {
                switch (dbConnectionType)
                {
                    case DbConnectionType.None:
                        {
                            //
                            // NOTE: The caller explicitly requested an invalid
                            //       database connection; therefore, return one.
                            //
                            connection = null;
                            return ReturnCode.Ok;
                        }
                    case DbConnectionType.Odbc:
                        {
                            connection = new OdbcConnection(connectionString);
                            return ReturnCode.Ok;
                        }
                    case DbConnectionType.OleDb:
                        {
                            connection = new OleDbConnection(connectionString);
                            return ReturnCode.Ok;
                        }
                    case DbConnectionType.Sql:
                        {
                            connection = new SqlConnection(connectionString);
                            return ReturnCode.Ok;
                        }
                    case DbConnectionType.Other:
                        {
                            return CreateOtherDbConnection(
                                interpreter, dbConnectionType, connectionString,
                                typeFullName, typeName, valueFlags, ref connection,
                                ref error);
                        }
                    default:
                        {
                            //
                            // NOTE: Lookup the type name and/or full name and
                            //       then go to the "other" case (for database
                            //       connection types that are not "built-in").
                            //
                            bool found = false;

                            if ((dbConnectionTypeFullNames != null) &&
                                dbConnectionTypeFullNames.TryGetValue(
                                    dbConnectionType, out typeFullName))
                            {
                                found = true;
                            }

                            if ((dbConnectionTypeNames != null) &&
                                dbConnectionTypeNames.TryGetValue(
                                    dbConnectionType, out typeName))
                            {
                                found = true;
                            }

                            if (found)
                                goto case DbConnectionType.Other;

                            error = String.Format(
                                "unsupported database connection type \"{0}\"",
                                dbConnectionType);

                            break;
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

        private static void GetDataReaderFieldNames(
            IDataReader reader,
            bool clear,
            ref StringList list
            )
        {
            if (reader == null)
                return;

            int fieldCount = reader.FieldCount;

            if (clear || (list == null))
                list = new StringList();

            for (int index = 0; index < fieldCount; index++)
                list.Add(reader.GetName(index));
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetDataReaderFieldValues(
            IDataReader reader,
            DateTimeBehavior dateTimeBehavior,
            DateTimeKind dateTimeKind,
            string dateTimeFormat,
            string nullValue,
            bool clear,
            bool allowNull,
            bool pairs,
            bool names,
            ref StringList list
            )
        {
            if (reader == null)
                return;

            int fieldCount = reader.FieldCount;

            if (clear || (list == null))
                list = new StringList();

            for (int index = 0; index < fieldCount; index++)
            {
                object value = reader.GetValue(index);

                if (allowNull ||
                    ((value != null) && (value != DBNull.Value)))
                {
                    if (pairs)
                    {
                        StringList element = new StringList();

                        if (names)
                            element.Add(reader.GetName(index));

                        element.Add(MarshalOps.FixupDataValue(
                            value, dateTimeBehavior, dateTimeKind,
                            dateTimeFormat, nullValue));

                        list.Add(element.ToString());
                    }
                    else
                    {
                        if (names)
                            list.Add(reader.GetName(index));

                        list.Add(MarshalOps.FixupDataValue(
                            value, dateTimeBehavior, dateTimeKind,
                            dateTimeFormat, nullValue));
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DataReaderToList(
            Interpreter interpreter,
            IDataReader reader,
            DateTimeBehavior dateTimeBehavior,
            DateTimeKind dateTimeKind,
            string dateTimeFormat,
            string nullValue,
            int limit,
            bool nested,
            bool allowNull,
            bool pairs,
            bool names,
            ref StringList list,
            ref int count,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (reader == null)
            {
                error = "invalid data reader";
                return ReturnCode.Error;
            }

            //
            // NOTE: Now, read the rows of data and populate the
            //       rows array.
            //
            int localCount = 0;

            while (reader.Read())
            {
                localCount++;

                if (nested)
                {
                    //
                    // NOTE: Add the column values for this row to a new list
                    //       for this row only and then add list that entire
                    //       row list to the result list as one item.
                    //
                    StringList row = null;

                    GetDataReaderFieldValues(
                        reader, dateTimeBehavior, dateTimeKind, dateTimeFormat,
                        nullValue, false, allowNull, pairs, names, ref row);

                    if (list == null)
                        list = new StringList();

                    list.Add(row.ToString());
                }
                else
                {
                    //
                    // NOTE: Add the column values for this row directly
                    //       to the result list.
                    //
                    GetDataReaderFieldValues(
                        reader, dateTimeBehavior, dateTimeKind, dateTimeFormat,
                        nullValue, false, allowNull, pairs, names, ref list);
                }

                //
                // NOTE: Have we reached the limit for the number of rows we
                //       want to return, if any?
                //
                if ((limit != 0) && (--limit == 0))
                    break;
            }

            count = localCount;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DataReaderToArray(
            Interpreter interpreter,
            IDataReader reader,
            string varName,
            DateTimeBehavior dateTimeBehavior,
            DateTimeKind dateTimeKind,
            string dateTimeFormat,
            string nullValue,
            int limit,
            bool allowNull,
            bool pairs,
            bool names,
            ref int count,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (reader == null)
            {
                error = "invalid data reader";
                return ReturnCode.Error;
            }

            ReturnCode code;

            //
            // NOTE: Make sure to reset the rows variable if it already exists.
            //
            bool badRequest = false;
            Result localError = null;

            if (((interpreter.DoesVariableExist(
                    VariableFlags.NoElement, null, varName, ref badRequest,
                    ref localError) != ReturnCode.Ok) && !badRequest) ||
                (!badRequest && interpreter.ResetVariable(
                    VariableFlags.NoElement, varName,
                    ref error) == ReturnCode.Ok))
            {
                //
                // NOTE: Get the column names.
                //
                StringList nameList = null;

                GetDataReaderFieldNames(reader, false, ref nameList);

                //
                // NOTE: Create the script array variable by setting the
                //       columns names.
                //
                code = interpreter.SetVariableValue2(
                    VariableFlags.None, varName, Vars.ResultSet.Names,
                    nameList.ToString(), ref error);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: This is incremented inside the loop prior to be
                    //       used as an array element index; therefore, we will
                    //       never have a row zero that contains valid data.
                    //
                    int rowIndex = 0;

                    //
                    // NOTE: Now, read the rows of data and populate the
                    //       rows array.
                    //
                    while (reader.Read())
                    {
                        //
                        // NOTE: This list will contain the column names and
                        //       values for this row.  Including the column
                        //       names here as well as in the header row helps
                        //       us to detect when a given column value is null
                        //       (missing) for a row and retains semantic
                        //       compatibility with the upcoming TDBC standard.
                        //
                        StringList nameValueList = null;

                        GetDataReaderFieldValues(
                            reader, dateTimeBehavior, dateTimeKind,
                            dateTimeFormat, nullValue, false, allowNull, pairs,
                            names, ref nameValueList);

                        rowIndex++;

                        code = interpreter.SetVariableValue2(
                            VariableFlags.None, varName, rowIndex.ToString(),
                            nameValueList.ToString(), ref error);

                        if (code != ReturnCode.Ok)
                            break;

                        //
                        // NOTE: Have we reached the limit for the number of
                        //       rows we want to return, if any?
                        //
                        if ((limit != 0) && (--limit == 0))
                            break;
                    }

                    //
                    // NOTE: If we succeeded, set the overall row count now
                    //       that we know what it is.
                    //
                    if (code == ReturnCode.Ok)
                    {
                        code = interpreter.SetVariableValue2(
                            VariableFlags.None, varName,
                            Vars.ResultSet.Count, rowIndex.ToString(),
                            ref error);

                        if (code == ReturnCode.Ok)
                            count = rowIndex;
                    }
                }
            }
            else
            {
                //
                // NOTE: The rows variable name is invalid (i.e. it refers
                //       to an array element) OR it already exists and we
                //       failed to reset it.
                //
                if (badRequest)
                    error = localError;

                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
