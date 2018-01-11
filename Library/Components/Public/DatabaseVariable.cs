/*
 * DatabaseVariable.cs --
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
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("3d4f0e30-9aaf-485e-8d5a-c2e2325ecfef")]
    public sealed class DatabaseVariable :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IDisposable
    {
        #region Private Constants
        #region Table/Column/Parameter Name Validation Regular Expression
        //
        // HACK: This is hard-coded for now.  Maybe make this configurable at
        //       some point.
        //
        // HACK: This is purposely not read-only.
        //
        private static Regex identifierRegEx = new Regex(
            "^[$A-Z_][$0-9A-Z_]*$", RegexOptions.IgnoreCase);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDbDataParameter Names
        private static readonly string RowIdParameterName = "@rowId";
        private static readonly string NameParameterName = "@name";
        private static readonly string ValueParameterName = "@value";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Database Column Names
        //
        // NOTE: This is the primary column name for the row identifier used
        //       by Oracle.
        //
        private static readonly string OracleRowIdColumnName = "ROWID";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the column name for the row identifier used by SQL
        //       Server.
        //
        private static readonly string SqlRowIdColumnName = "$IDENTITY";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the primary column name for the row identifier used
        //       by SQLite.
        //
        private static readonly string SQLiteRowIdColumnName = "rowid";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region SQL DML Statements
        //
        // NOTE: This is used to return a count of variables.  It must work
        //       with any SQL database.
        //
        private static readonly string SelectCountCommandText =
            "SELECT COUNT(*) FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a list of variable names.  It must
        //       work with any SQL database.
        //
        private static readonly string SelectOneForAllCommandText =
            "SELECT {1} FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a list of variable names and their
        //       values.  It must work with any SQL database.
        //
        private static readonly string SelectTwoForAllCommandText =
            "SELECT {1}, {2} FROM {0};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a single column value for a matching
        //       row.  It must work with any SQL database.
        //
        private static readonly string SelectCommandText =
            "SELECT {0} FROM {1} WHERE {2} = {3};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to return a single column value for a matching
        //       row.  It must work with SQLite.
        //
        private static readonly string SelectWhereCastCommandText =
            "SELECT {0} FROM {1} WHERE CAST({2} AS TEXT) = {3};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to check if a single matching row exists.  It
        //       must work with any SQL database.
        //
        private static readonly string SelectExistCommandText =
            "SELECT 1 FROM {0} WHERE {1} = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to check if a single matching row exists.  It
        //       must work with SQLite.
        //
        private static readonly string SelectExistWhereCastCommandText =
            "SELECT 1 FROM {0} WHERE CAST({1} AS TEXT) = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to insert a single row with two columns, one
        //       for the new variable name and one for the new variable value.
        //       It must work with any SQL database.
        //
        private static readonly string InsertCommandText =
            "INSERT INTO {0} ({1}, {2}) VALUES ({3}, {4});";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to insert a single row with two columns, one
        //       for the new variable name and one for the new variable value.
        //       It must work with SQLite.
        //
        private static readonly string InsertWhereCastCommandText =
            "INSERT INTO {0} ({1}, {2}) VALUES ({3}, CAST({4} AS TEXT));";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to update a single row with two columns, one
        //       for the existing variable name and one for the new variable
        //       value.  It must work with any SQL database.
        //
        private static readonly string UpdateCommandText =
            "UPDATE {0} SET {1} = {3} WHERE {2} = {4};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to update a single row with two columns, one
        //       for the existing variable name and one for the new variable
        //       value.  It must work with SQLite.
        //
        private static readonly string UpdateWhereCastCommandText =
            "UPDATE {0} SET {1} = {3} WHERE CAST({2} AS TEXT) = {4};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to delete a single row with at least one column,
        //       the existing variable name.  It must work with any SQL
        //       database.
        //
        private static readonly string DeleteCommandText =
            "DELETE FROM {0} WHERE {1} = {2};";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is used to delete a single row with at least one column,
        //       the existing variable name [to be matched against].  It must
        //       work with SQLite.
        //
        private static readonly string DeleteWhereCastCommandText =
            "DELETE FROM {0} WHERE CAST({1} AS TEXT) = {2};";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private DatabaseVariable(
            DbVariableFlags dbVariableFlags,
            DbConnectionType dbConnectionType,
            string typeName,
            string connectionString,
            string tableName,
            string nameColumnName,
            string valueColumnName,
            BreakpointType permissions,
            bool useRowId
            )
        {
            this.dbVariableFlags = dbVariableFlags;
            this.dbConnectionType = dbConnectionType;
            this.typeName = typeName;
            this.connectionString = connectionString;
            this.tableName = tableName;
            this.nameColumnName = nameColumnName;
            this.valueColumnName = valueColumnName;
            this.permissions = permissions;
            this.useRowId = useRowId;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static DatabaseVariable Create(
            DbVariableFlags dbVariableFlags,
            DbConnectionType dbConnectionType,
            string typeName,
            string connectionString,
            string tableName,
            string nameColumnName,
            string valueColumnName,
            BreakpointType permissions,
            bool useRowId
            )
        {
            return new DatabaseVariable(
                dbVariableFlags, dbConnectionType, typeName, connectionString,
                tableName, nameColumnName, valueColumnName, permissions,
                useRowId);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Members
        #region Public Properties
        private DbVariableFlags dbVariableFlags;
        public DbVariableFlags DbVariableFlags
        {
            get { CheckDisposed(); return dbVariableFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DbConnectionType dbConnectionType;
        public DbConnectionType DbConnectionType
        {
            get { CheckDisposed(); return dbConnectionType; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string typeName;
        public string TypeName
        {
            get { CheckDisposed(); return typeName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string connectionString;
        public string ConnectionString
        {
            get { CheckDisposed(); return connectionString; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string tableName;
        public string TableName
        {
            get { CheckDisposed(); return tableName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string nameColumnName;
        public string NameColumnName
        {
            get { CheckDisposed(); return nameColumnName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string valueColumnName;
        public string ValueColumnName
        {
            get { CheckDisposed(); return valueColumnName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BreakpointType permissions;
        public BreakpointType Permissions
        {
            get { CheckDisposed(); return permissions; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool useRowId;
        public bool UseRowId
        {
            get { CheckDisposed(); return useRowId; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string rowIdColumnName;
        public string RowIdColumnName
        {
            get { CheckDisposed(); return rowIdColumnName; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Command Helper Methods
        public bool DoesExist(
            Interpreter interpreter,
            string name
            )
        {
            CheckDisposed();

            return DoesExistViaSelect(interpreter, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public long? GetCount(
            Interpreter interpreter,
            ref Result error
            )
        {
            CheckDisposed();

            long count = 0;

            if (GetCountViaSelect(
                    interpreter, ref count, ref error) == ReturnCode.Ok)
            {
                return count;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectDictionary GetList(
            Interpreter interpreter,
            bool names,
            bool values,
            ref Result error
            )
        {
            CheckDisposed();

            ObjectDictionary dictionary = null;

            if (GetListViaSelect(
                    interpreter, names, values, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                return dictionary;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysToString(
            Interpreter interpreter,
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions,
            ref Result error
            )
        {
            CheckDisposed();

            ObjectDictionary dictionary = null;

            if (GetListViaSelect(
                    interpreter, true, false, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, false, mode, pattern, null, null,
                    null, null, noCase, regExOptions) as StringList;

                return GenericOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), null, false);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            Interpreter interpreter,
            string pattern,
            bool noCase,
            ref Result error
            )
        {
            CheckDisposed();

            ObjectDictionary dictionary = null;

            if (GetListViaSelect(
                    interpreter, true, true, ref dictionary,
                    ref error) == ReturnCode.Ok)
            {
                StringList list = GenericOps<string, object>.KeysAndValues(
                    dictionary, false, true, true, StringOps.DefaultMatchMode,
                    pattern, null, null, null, null, noCase, RegexOptions.None)
                    as StringList;

                return GenericOps<string>.ListToString(
                    list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                    Characters.Space.ToString(), null, false);
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Method
        [MethodFlags(MethodFlags.VariableTrace | MethodFlags.NoAdd)]
        public ReturnCode TraceCallback(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (traceInfo == null)
            {
                result = "invalid trace";
                return ReturnCode.Error;
            }

            IVariable variable = traceInfo.Variable;

            if (variable == null)
            {
                result = "invalid variable";
                return ReturnCode.Error;
            }

            //
            // NOTE: *SPECIAL* Ignore the index when we initially add the
            //       variable since we do not perform any trace actions during
            //       add anyhow.
            //
            if (breakpointType == BreakpointType.BeforeVariableAdd)
                return traceInfo.ReturnCode;

            //
            // NOTE: Check if we support the requested operation at all.
            //
            if ((breakpointType != BreakpointType.BeforeVariableGet) &&
                (breakpointType != BreakpointType.BeforeVariableSet) &&
                (breakpointType != BreakpointType.BeforeVariableUnset))
            {
                result = "unsupported operation";
                return ReturnCode.Error;
            }

            //
            // NOTE: *WARNING* Empty array element names are allowed, please do
            //       not change this to "!String.IsNullOrEmpty".
            //
            if (traceInfo.Index != null)
            {
                //
                // NOTE: Check if we are allowing this type of operation.  This
                //       does not apply if the entire variable is being removed
                //       from the interpreter (i.e. for "unset" operations when
                //       the index is null).
                //
                if (!HasFlags(breakpointType, true))
                {
                    result = "permission denied";
                    return ReturnCode.Error;
                }

                try
                {
                    using (IDbConnection connection = CreateDbConnection(
                            interpreter, ref result))
                    {
                        if (connection == null)
                            return ReturnCode.Error;

                        connection.Open();

                        using (IDbCommand command = connection.CreateCommand())
                        {
                            if (command == null)
                            {
                                result = "could not create command";
                                return ReturnCode.Error;
                            }

                            switch (breakpointType)
                            {
                                case BreakpointType.BeforeVariableGet:
                                    {
                                        string commandText;
                                        string whereColumnName;
                                        DbType? whereParameterDbType;
                                        string whereParameterName;
                                        object whereParameterValue;

                                        GetCommandTextAndValues(
                                            interpreter, breakpointType, traceInfo.Index,
                                            out commandText, out whereColumnName,
                                            out whereParameterDbType, out whereParameterName,
                                            out whereParameterValue);

                                        CheckIdentifier("ValueColumnName", valueColumnName);
                                        CheckIdentifier("TableName", tableName);

                                        command.CommandText = FormatCommandText(commandText,
                                            1, valueColumnName, tableName, whereColumnName,
                                            whereParameterName);

                                        IDbDataParameter whereParameter = command.CreateParameter();

                                        if (whereParameter == null)
                                        {
                                            result = "could not create where parameter";
                                            return ReturnCode.Error;
                                        }

                                        if (whereParameterDbType != null)
                                            whereParameter.DbType = (DbType)whereParameterDbType;

                                        whereParameter.ParameterName = whereParameterName;
                                        whereParameter.Value = whereParameterValue;

                                        command.Parameters.Add(whereParameter);

                                        using (IDataReader reader = command.ExecuteReader())
                                        {
                                            if (reader == null)
                                            {
                                                result = "could not execute command";
                                                return ReturnCode.Error;
                                            }

                                            if (reader.Read())
                                            {
                                                result = StringOps.GetResultFromObject(
                                                    reader.GetValue(0));

                                                traceInfo.ReturnCode = ReturnCode.Ok;
                                            }
                                            else
                                            {
                                                result = String.Format(
                                                    "can't read {0}: no such element in array",
                                                    FormatOps.ErrorVariableName(
                                                        variable.Name, traceInfo.Index));

                                                traceInfo.ReturnCode = ReturnCode.Error;
                                            }
                                        }

                                        traceInfo.Cancel = true;
                                        break;
                                    }
                                case BreakpointType.BeforeVariableSet:
                                    {
                                        string commandText;
                                        string whereColumnName;
                                        DbType? whereParameterDbType;
                                        string whereParameterName;
                                        object whereParameterValue;

                                        GetCommandTextAndValues(
                                            interpreter, breakpointType, traceInfo.Index,
                                            out commandText, out whereColumnName,
                                            out whereParameterDbType, out whereParameterName,
                                            out whereParameterValue);

                                        CheckIdentifier("TableName", tableName);
                                        CheckIdentifier("ValueColumnName", valueColumnName);

                                        command.CommandText = FormatCommandText(commandText,
                                            2, tableName, valueColumnName, whereColumnName,
                                            ValueParameterName, whereParameterName);

                                        IDbDataParameter valueParameter = command.CreateParameter();

                                        if (valueParameter == null)
                                        {
                                            result = "could not create value parameter";
                                            return ReturnCode.Error;
                                        }

                                        object valueParameterValue = traceInfo.NewValue;

                                        valueParameter.ParameterName = ValueParameterName;
                                        valueParameter.Value = valueParameterValue;

                                        IDbDataParameter whereParameter = command.CreateParameter();

                                        if (whereParameter == null)
                                        {
                                            result = "could not create where parameter";
                                            return ReturnCode.Error;
                                        }

                                        if (whereParameterDbType != null)
                                            whereParameter.DbType = (DbType)whereParameterDbType;

                                        whereParameter.ParameterName = whereParameterName;
                                        whereParameter.Value = whereParameterValue;

                                        command.Parameters.Add(valueParameter);
                                        command.Parameters.Add(whereParameter);

                                        if (command.ExecuteNonQuery() > 0) /* Did we do anything? */
                                        {
                                            result = StringOps.GetResultFromObject(
                                                valueParameterValue);

                                            EntityOps.SetUndefined(variable, false);
                                            EntityOps.SetDirty(variable, true);

                                            traceInfo.ReturnCode = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "can't set {0}: no such element in array",
                                                FormatOps.ErrorVariableName(
                                                    variable.Name, traceInfo.Index));

                                            traceInfo.ReturnCode = ReturnCode.Error;
                                        }

                                        traceInfo.Cancel = true;
                                        break;
                                    }
                                case BreakpointType.BeforeVariableUnset:
                                    {
                                        string commandText;
                                        string whereColumnName;
                                        DbType? whereParameterDbType;
                                        string whereParameterName;
                                        object whereParameterValue;

                                        GetCommandTextAndValues(
                                            interpreter, breakpointType, traceInfo.Index,
                                            out commandText, out whereColumnName,
                                            out whereParameterDbType, out whereParameterName,
                                            out whereParameterValue);

                                        CheckIdentifier("TableName", tableName);

                                        command.CommandText = FormatCommandText(commandText,
                                            1, tableName, whereColumnName, whereParameterName);

                                        IDbDataParameter whereParameter = command.CreateParameter();

                                        if (whereParameter == null)
                                        {
                                            result = "could not create where parameter";
                                            return ReturnCode.Error;
                                        }

                                        if (whereParameterDbType != null)
                                            whereParameter.DbType = (DbType)whereParameterDbType;

                                        whereParameter.ParameterName = whereParameterName;
                                        whereParameter.Value = whereParameterValue;

                                        command.Parameters.Add(whereParameter);

                                        if (command.ExecuteNonQuery() > 0) /* Did we do anything? */
                                        {
                                            result = String.Empty;

                                            EntityOps.SetDirty(variable, true);

                                            traceInfo.ReturnCode = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "can't unset {0}: no such element in array",
                                                FormatOps.ErrorVariableName(
                                                    variable.Name, traceInfo.Index));

                                            traceInfo.ReturnCode = ReturnCode.Error;
                                        }

                                        traceInfo.Cancel = true;
                                        break;
                                    }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Engine.SetExceptionErrorCode(interpreter, e);

                    result = e;
                    traceInfo.ReturnCode = ReturnCode.Error;
                }

                return traceInfo.ReturnCode;
            }
            else if (breakpointType == BreakpointType.BeforeVariableUnset)
            {
                //
                // NOTE: They want to unset the entire env array.  I guess
                //       this should be allowed, it is in Tcl.  Also, make
                //       sure it is purged from the call frame so that it
                //       cannot be magically restored with this trace
                //       callback in place.
                //
                traceInfo.Flags &= ~VariableFlags.NoRemove;

                //
                // NOTE: Ok, allow the variable removal.
                //
                return ReturnCode.Ok;
            }
            else
            {
                //
                // NOTE: We (this trace procedure) expect the variable
                //       to always be an array.
                //
                result = String.Format(
                    "can't {0} {1}: variable is array",
                    FormatOps.Breakpoint(breakpointType),
                    FormatOps.ErrorVariableName(variable.Name, null));

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Helper Methods
        public ReturnCode AddVariable(
            Interpreter interpreter,
            string name,
            ref Result error
            )
        {
            CheckDisposed();

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            return interpreter.AddVariable(VariableFlags.Array, name,
                new TraceList(new TraceCallback[] { TraceCallback }),
                true, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Helper Methods
        public StringPairList ToList()
        {
            CheckDisposed();

            StringPairList list = new StringPairList();

            list.Add("dbConnectionType", dbConnectionType.ToString());

            if (typeName != null)
                list.Add("typeName", typeName);

            if (connectionString != null)
                list.Add("connectionString", connectionString);

            if (tableName != null)
                list.Add("tableName", tableName);

            if (nameColumnName != null)
                list.Add("nameColumnName", nameColumnName);

            if (valueColumnName != null)
                list.Add("valueColumnName", valueColumnName);

            list.Add("permissions", permissions.ToString());
            list.Add("useRowId", useRowId.ToString());

            return list;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            CheckDisposed();

            return ToList().ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Members
        #region Connection Helper Methods
        private IDbConnection CreateDbConnection(
            Interpreter interpreter
            )
        {
            Result error = null;

            return CreateDbConnection(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private IDbConnection CreateDbConnection(
            Interpreter interpreter,
            ref Result error
            )
        {
            IDbConnection connection = null;

            if (DataOps.CreateDbConnection(
                    interpreter, dbConnectionType, connectionString,
                    typeName, typeName, ValueFlags.None, ref connection,
                    ref error) == ReturnCode.Ok)
            {
                return connection;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Column Name Helper Methods
        private void GetRowIdColumnName()
        {
            //
            // HACK: For now, we only know how to do this for SQL Server and
            //       SQLite.
            //
            // TODO: Add support for more database backends here.
            //
            //
            switch (dbConnectionType)
            {
                case DbConnectionType.Oracle:
                    {
                        rowIdColumnName = OracleRowIdColumnName;
                        break;
                    }
                case DbConnectionType.Sql:
                    {
                        rowIdColumnName = SqlRowIdColumnName;
                        break;
                    }
                case DbConnectionType.SQLite:
                    {
                        rowIdColumnName = SQLiteRowIdColumnName;
                        break;
                    }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Command Text Helper Methods
        private static string GetVariableCountCommandText()
        {
            return SelectCountCommandText;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetVariableListCommandText(
            bool names,
            bool values
            )
        {
            if (names || values)
            {
                if (names && values)
                    return SelectTwoForAllCommandText;
                else
                    return SelectOneForAllCommandText;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Text Helper Methods
        private string GetSelectCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
                return SelectWhereCastCommandText;
            else
                return SelectCommandText;
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableExistCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
                return SelectExistWhereCastCommandText;
            else
                return SelectExistCommandText;
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableGetCommandText()
        {
            return GetSelectCommandText();
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableSetCommandText(
            bool exists
            )
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
            {
                return exists ?
                    UpdateWhereCastCommandText : InsertWhereCastCommandText;
            }
            else
            {
                return exists ? UpdateCommandText : InsertCommandText;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private string GetVariableUnsetCommandText()
        {
            //
            // TODO: Add support for more database backends here.
            //
            if (dbConnectionType == DbConnectionType.SQLite)
                return DeleteWhereCastCommandText;
            else
                return DeleteCommandText;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Flags Helper Methods
        private bool HasFlags(
            DbVariableFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(dbVariableFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool HasFlags(
            BreakpointType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(permissions, hasFlags, all);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Query Helper Methods
        private bool DoesExistViaSelect(
            Interpreter interpreter,
            string name
            )
        {
            using (IDbConnection connection = CreateDbConnection(
                    interpreter))
            {
                if (connection == null)
                    return false;

                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    if (command == null)
                        return false;

                    string commandText = GetVariableExistCommandText();

                    CheckIdentifier("TableName", tableName);
                    CheckIdentifier("NameColumnName", nameColumnName);

                    command.CommandText = FormatCommandText(
                        commandText, 1, tableName, nameColumnName,
                        NameParameterName);

                    IDbDataParameter whereParameter =
                        command.CreateParameter();

                    if (whereParameter == null)
                        return false;

                    whereParameter.ParameterName = NameParameterName;
                    whereParameter.Value = name;

                    command.Parameters.Add(whereParameter);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader == null)
                            return false;

                        return reader.Read();
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool GetRowIdViaSelect(
            Interpreter interpreter,
            string rowIdColumnName,
            string name,
            ref object rowId
            ) /* throw */
        {
            if (rowIdColumnName == null)
                return false;

            using (IDbConnection connection = CreateDbConnection(
                    interpreter))
            {
                if (connection == null)
                    return false;

                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    if (command == null)
                        return false;

                    string commandText = GetSelectCommandText();

                    CheckIdentifier("RowIdColumnName", rowIdColumnName);
                    CheckIdentifier("TableName", tableName);
                    CheckIdentifier("NameColumnName", nameColumnName);

                    command.CommandText = FormatCommandText(
                        commandText, 1, rowIdColumnName, tableName,
                        nameColumnName, NameParameterName);

                    IDbDataParameter whereParameter =
                        command.CreateParameter();

                    if (whereParameter == null)
                        return false;

                    whereParameter.ParameterName = NameParameterName;
                    whereParameter.Value = name;

                    command.Parameters.Add(whereParameter);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader == null)
                            return false;

                        bool exists = reader.Read();

                        if (exists)
                            rowId = reader.GetValue(0);

                        return exists;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetCountViaSelect(
            Interpreter interpreter,
            ref long count,
            ref Result error
            )
        {
            using (IDbConnection connection = CreateDbConnection(
                    interpreter, ref error))
            {
                if (connection == null)
                    return ReturnCode.Error;

                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    if (command == null)
                    {
                        error = "could not create command";
                        return ReturnCode.Error;
                    }

                    string commandText = GetVariableCountCommandText();

                    CheckIdentifier("TableName", tableName);

                    command.CommandText = FormatCommandText(commandText,
                        0, tableName);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader == null)
                        {
                            error = "could not execute command";
                            return ReturnCode.Error;
                        }

                        if (reader.Read())
                            count = reader.GetInt64(0);
                        else
                            count = 0;
                    }
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private ReturnCode GetListViaSelect(
            Interpreter interpreter,
            bool names,
            bool values,
            ref ObjectDictionary dictionary,
            ref Result error
            )
        {
            if (dictionary == null)
                dictionary = new ObjectDictionary();

            if (!names && !values)
                return ReturnCode.Ok;

            using (IDbConnection connection = CreateDbConnection(
                    interpreter, ref error))
            {
                if (connection == null)
                    return ReturnCode.Error;

                connection.Open();

                using (IDbCommand command = connection.CreateCommand())
                {
                    if (command == null)
                    {
                        error = "could not create command";
                        return ReturnCode.Error;
                    }

                    string commandText = GetVariableListCommandText(
                        names, values);

                    CheckIdentifier("TableName", tableName);
                    CheckIdentifier("NameColumnName", nameColumnName);
                    CheckIdentifier("ValueColumnName", valueColumnName);

                    command.CommandText = FormatCommandText(commandText,
                        0, tableName, nameColumnName, valueColumnName);

                    using (IDataReader reader = command.ExecuteReader())
                    {
                        if (reader == null)
                        {
                            error = "could not execute command";
                            return ReturnCode.Error;
                        }

                        while (reader.Read())
                        {
                            string name = reader.GetString(0);

                            if (name == null)
                                continue;

                            object value = null;

                            if (reader.FieldCount >= 2)
                                value = reader.GetValue(1);

                            dictionary[name] = value;
                        }
                    }
                }
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Static Helper Methods
        private static void CheckIdentifier(
            string propertyName,
            string propertyValue
            ) /* throw */
        {
            CheckIdentifier(propertyName, propertyValue, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void CheckIdentifier(
            string propertyName,
            string propertyValue,
            bool isParameterName
            ) /* throw */
        {
            if (propertyValue == null)
                throw new ArgumentNullException(propertyName);

            //
            // HACK: A parameter name is exempt from the regular expression
            //       check in this method as long as it matches one of the
            //       "well-known" (constant) parameter names.
            //
            if (isParameterName && (String.Equals(
                    propertyValue, RowIdParameterName) || String.Equals(
                    propertyValue, NameParameterName) || String.Equals(
                    propertyValue, ValueParameterName)))
            {
                return;
            }

            if (identifierRegEx != null)
            {
                Match match = identifierRegEx.Match(propertyValue);

                if ((match == null) || !match.Success)
                {
                    throw new ArgumentException(String.Format(
                        "value {0} is not a valid database identifier, " +
                        "pattern {1}", FormatOps.WrapOrNull(propertyValue),
                        FormatOps.WrapOrNull(identifierRegEx)),
                        propertyName);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method is used to format the command text for execution
        //       against the target database.  It performs some "last resort"
        //       checks for valid identifiers.  Since all callers should have
        //       already checked their identifier names, this method should
        //       never throw any exceptions.
        //
        // NOTE: The caller is expected to know (and pass) the number of
        //       parameter names that occur as the final (X) parameters.
        //       These parameter names must be valid identifiers unless
        //       they are one of the "well-known" (constant) parameter
        //       names.
        //
        private static string FormatCommandText(
            string format,
            int parameterCount,
            params string[] names
            ) /* throw */
        {
            if (names == null)
                throw new ArgumentNullException("names");

            int length = names.Length;
            int lastIndex = length - 1;

            for (int index = 0; index < length; index++)
            {
                //
                // HACK: This assumes that all parameter names only occur
                //       at the end of the parameter list.  This class is
                //       designed to conform with this assumption.
                //
                bool isParameterName = (parameterCount > 0) &&
                    (index >= (lastIndex - parameterCount));

                //
                // NOTE: The property name is unknown at this point.  That
                //       does not matter because they are not used in the
                //       actual command text.
                //
                CheckIdentifier(null, names[index], isParameterName);
            }

            return String.Format(format, names);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Callback Helper Methods
        private void CheckTraceAccess(
            BreakpointType breakpointType
            ) /* throw */
        {
            CheckTraceAccess(breakpointType, null);
        }

        ///////////////////////////////////////////////////////////////////////

        private void CheckTraceAccess(
            BreakpointType breakpointType,
            bool? exists
            ) /* throw */
        {
            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableGet:
                    {
                        if (!HasFlags(DbVariableFlags.AllowSelect, true))
                            throw new ScriptException("SELECT forbidden");

                        break;
                    }
                case BreakpointType.BeforeVariableSet:
                    {
                        if (((exists == null) || (!(bool)exists)) &&
                            !HasFlags(DbVariableFlags.AllowInsert, true))
                        {
                            throw new ScriptException("INSERT forbidden");
                        }

                        if (((exists == null) || ((bool)exists)) &&
                            !HasFlags(DbVariableFlags.AllowUpdate, true))
                        {
                            throw new ScriptException("UPDATE forbidden");
                        }

                        break;
                    }
                case BreakpointType.BeforeVariableUnset:
                    {
                        if (!HasFlags(DbVariableFlags.AllowDelete, true))
                            throw new ScriptException("DELETE forbidden");

                        break;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void GetCommandTextAndValues(
            Interpreter interpreter,
            BreakpointType breakpointType,
            string name,
            out string commandText,
            out string whereColumnName,
            out DbType? whereParameterDbType,
            out string whereParameterName,
            out object whereParameterValue
            ) /* throw */
        {
            commandText = null;
            whereColumnName = null;
            whereParameterDbType = null;
            whereParameterName = null;
            whereParameterValue = null;

            switch (breakpointType)
            {
                case BreakpointType.BeforeVariableGet:
                    {
                        if (useRowId)
                        {
                            if (rowIdColumnName == null)
                                GetRowIdColumnName();

                            object rowId = null;

                            GetRowIdViaSelect(
                                interpreter, rowIdColumnName, name, ref rowId);

                            if ((rowIdColumnName != null) && (rowId != null))
                            {
                                CheckIdentifier(
                                    "RowIdColumnName", rowIdColumnName);

                                commandText = SelectCommandText;
                                whereColumnName = rowIdColumnName;
                                whereParameterDbType = DbType.Object;
                                whereParameterName = RowIdParameterName;
                                whereParameterValue = rowId;

                                return;
                            }
                        }

                        CheckTraceAccess(breakpointType);
                        CheckIdentifier("NameColumnName", nameColumnName);

                        commandText = GetVariableGetCommandText();
                        whereColumnName = nameColumnName;
                        whereParameterName = NameParameterName;
                        whereParameterValue = name;
                        break;
                    }
                case BreakpointType.BeforeVariableSet:
                    {
                        bool exists;

                        if (useRowId)
                        {
                            if (rowIdColumnName == null)
                                GetRowIdColumnName();

                            object rowId = null;

                            if (rowIdColumnName != null)
                            {
                                exists = GetRowIdViaSelect(
                                    interpreter, rowIdColumnName, name,
                                    ref rowId);
                            }
                            else
                            {
                                exists = DoesExistViaSelect(interpreter, name);
                            }

                            if ((rowIdColumnName != null) && (rowId != null))
                            {
                                CheckIdentifier(
                                    "RowIdColumnName", rowIdColumnName);

                                commandText = UpdateCommandText;
                                whereColumnName = rowIdColumnName;
                                whereParameterDbType = DbType.Object;
                                whereParameterName = RowIdParameterName;
                                whereParameterValue = rowId;

                                return;
                            }
                        }
                        else
                        {
                            exists = DoesExistViaSelect(interpreter, name);
                        }

                        CheckTraceAccess(breakpointType, exists);
                        CheckIdentifier("NameColumnName", nameColumnName);

                        commandText = GetVariableSetCommandText(exists);
                        whereColumnName = nameColumnName;
                        whereParameterName = NameParameterName;
                        whereParameterValue = name;
                        break;
                    }
                case BreakpointType.BeforeVariableUnset:
                    {
                        if (useRowId)
                        {
                            if (rowIdColumnName == null)
                                GetRowIdColumnName();

                            object rowId = null;

                            GetRowIdViaSelect(
                                interpreter, rowIdColumnName, name, ref rowId);

                            if ((rowIdColumnName != null) && (rowId != null))
                            {
                                CheckIdentifier(
                                    "RowIdColumnName", rowIdColumnName);

                                commandText = DeleteCommandText;
                                whereColumnName = rowIdColumnName;
                                whereParameterDbType = DbType.Object;
                                whereParameterName = RowIdParameterName;
                                whereParameterValue = rowId;

                                return;
                            }
                        }

                        CheckTraceAccess(breakpointType);
                        CheckIdentifier("NameColumnName", nameColumnName);

                        commandText = GetVariableUnsetCommandText();
                        whereColumnName = nameColumnName;
                        whereParameterName = NameParameterName;
                        whereParameterValue = name;
                        break;
                    }
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
            {
                throw new ObjectDisposedException(
                    typeof(DatabaseVariable).Name);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //}

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~DatabaseVariable()
        {
            Dispose(false);
        }
        #endregion
    }
}
