using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ZTAMPZ_EMAIL_SWF.CoreUtil
{
    public class DatabaseHelper
    {
        public class DatabaseConnectionClassFullNames
        {
            public const string INFORMIX = "IBM.Data.Informix.IfxConnection";
            public const string ORACLE_BUNDLED = "System.Data.OracleClient.OracleConnection";
            public const string ORACLE_ODP = "Oracle.DataAccess.Client.OracleConnection";
            public const string POSTGRESQL_NPGSQL = "Npgsql.NpgsqlConnection";
            public const string MSSQL_BUNDLED = "System.Data.SqlClient.SqlConnection";
            public const string ODBC_BUNDLED = "System.Data.Odbc.OdbcConnection";
        }
        public static readonly string ODBC_DRIVER_SYBASE_ASE = "SYODASE.DLL";

        public static readonly string PROVIDER_INFORMIX = "IBM.Data.Informix";
        public static readonly string PROVIDER_MSSQL_BUNDLED = "System.Data.SqlClient";
        public static readonly string PROVIDER_POSTGRESQL_NPGSQL = "Npgsql";
        public static readonly string PROVIDER_MYSQL_CONNECTORNET = "MySql.Data.MySqlClient";
        public static readonly string PROVIDER_ORACLE_BUNDLED = "System.Data.OracleClient";
        public static readonly string PROVIDER_ORACLE_ODP_NET = "Oracle.DataAccess.Client";

        private static object connectionAccessLock = new object();
        //private static IDatabaseSpecificParameterSetter InformixParameterSetter = null;

        public static T ConvertFromDatabaseDataObject<T>(Object value)
        {
            Type tType = typeof(T);
            Type nullableUnderlyingType =
                tType.IsGenericType && tType.GetGenericTypeDefinition() == typeof(Nullable<>) ?
                tType.GetGenericArguments()[0] : null;
            if (value == DBNull.Value || value == null)
            {
                return (T)(object)null;
            }
            else
            {
                Type valueType = value.GetType();
                if (typeof(T).IsAssignableFrom(valueType))
                {
                    return (T)value;
                }
                else if (value is string && (tType == typeof(bool) || tType == typeof(bool?)))
                {
                    string s = (string)value;

                    if (s == "")
                    {
                        return (T)(object)null;
                    }
                    else if (s == "Y")
                    {
                        return (T)(object)true;
                    }
                    else if (s == "N")
                    {
                        return (T)(object)false;
                    }
                }

                return (T)Convert.ChangeType(value, nullableUnderlyingType != null ? nullableUnderlyingType : tType);
            }
        }

        public static IDbConnection GetAndOpenConnection(string id)
        {
            lock (connectionAccessLock)
            {
                var config = ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>().FirstOrDefault(d => d.Name == id);
                IDbConnection connection = null;
                if (config != null)
                {
                    connection = DbProviderFactories.GetFactory(config.ProviderName).CreateConnection();
                    connection.ConnectionString = config.ConnectionString;
                }
                else
                {
                    var connectionSettings = Helper.SplitString(ConfigurationManager.AppSettings["ADOCON_" + id], ',', '"');
                    connection = (IDbConnection)(Type.GetType(connectionSettings[0]).GetConstructor(new Type[0]).Invoke(null));
                    connection.ConnectionString = connectionSettings[1];
                }
                connection.Open();
                return connection;
            }
        }

        public static string GetProviderInvariantName(string id)
        {
            lock (connectionAccessLock)
            {
                var config = ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>().First(d => d.Name == id);
                return config.ProviderName;
            }
        }

        public static void SetParameter(IDbCommand command, string parameterName, Object value)
        {
            if (parameterName.StartsWith("{?") && parameterName.EndsWith("}"))
            {
                command.CommandText = command.CommandText.Replace("'" + parameterName + "'", DatabaseHelper.ConvertObjectToSQLRepresentation(command.Connection, value));
                command.CommandText = command.CommandText.Replace(parameterName, DatabaseHelper.ConvertObjectToSQLRepresentation(command.Connection, value));
            }
            else
            {
                //if (command.Connection.GetType().FullName == DatabaseHelper.DatabaseConnectionClassFullNames.INFORMIX)
                //{
                //    if (DatabaseHelper.InformixParameterSetter == null)
                //    {
                //        Assembly informixSpecificUtilAssembly = Assembly.LoadFile(Directory.GetCurrentDirectory() + "\\" + "InformixSpecificUtil.dll");
                //        DatabaseHelper.InformixParameterSetter = (IDatabaseSpecificParameterSetter)Activator.CreateInstance(informixSpecificUtilAssembly.GetType("ExtensibleSchedulerApplication.InformixSpecificUtil.InformixSpecificParameterSetter"));
                //    }

                //    DatabaseHelper.InformixParameterSetter.SetParameter(command, parameterName, value);
                //}
                //else
                //{
                    IDataParameter parameter = command.CreateParameter();
                    parameter.ParameterName = parameterName;
                    parameter.Value = value == null ? DBNull.Value : (value is bool ? ((bool)value ? "Y" : "N") : value);

                    if (value is DateTime && (((DateTime)value).Ticks % 10000000L) != 0L
                        && command.Connection.GetType().FullName == DatabaseHelper.DatabaseConnectionClassFullNames.ORACLE_BUNDLED)
                    {
                        OracleParameter dbPar = (OracleParameter)parameter;
                        dbPar.OracleType = OracleType.Timestamp;
                    }

                    if (command.Parameters.Contains(parameterName))
                    {
                        command.Parameters[parameterName] = parameter;
                    }
                    else
                    {
                        command.Parameters.Add(parameter);
                    }
                //}
            }
        }

        public static string StringListToSQLList(IEnumerable<string> stringList)
        {
            StringBuilder sqlList = new StringBuilder();
            if (stringList != null)
            {
                foreach (string str in stringList)
                {
                    sqlList.Append('\'').Append(str == null ? "" : str.Replace("'", "''")).Append("',");
                }
            }
            if (sqlList.Length > 0)
            {
                sqlList.Remove(sqlList.Length - 1, 1);
            }
            return "(" + sqlList.ToString() + ")";
        }

        [Obsolete("Please use GenerateParameterPlaceHolderSql(IDbConnection, string) instead", false)]
        public static string GenerateParameterPlaceHolderSql(string providerName, string parameterName)
        {
            // Becareful of Informix which only support ? as placeholder in SQL, but the DbCommand has parameters with names (not ?)

            return
                (providerName == DatabaseHelper.PROVIDER_INFORMIX ? "?" :
                providerName == DatabaseHelper.PROVIDER_ORACLE_BUNDLED || providerName == DatabaseHelper.PROVIDER_ORACLE_ODP_NET || providerName == DatabaseHelper.PROVIDER_POSTGRESQL_NPGSQL ?
                ":" + parameterName : "@" + parameterName);
        }

        public static string GenerateParameterPlaceHolderSql(IDbConnection connection, string parameterName)
        {
            // Becareful of Informix which only support ? as placeholder in SQL, but the DbCommand has parameters with names (not ?)
            string connectionTypeName = connection.GetType().FullName;

            return
                (connectionTypeName == DatabaseHelper.DatabaseConnectionClassFullNames.INFORMIX ? "?" :
                connectionTypeName == DatabaseHelper.DatabaseConnectionClassFullNames.ORACLE_BUNDLED || connectionTypeName == DatabaseHelper.DatabaseConnectionClassFullNames.POSTGRESQL_NPGSQL ?
                ":" + parameterName : "@" + parameterName);
        }

        public static bool IsMssql(string databaseId)
        {
            return GetProviderInvariantName(databaseId) == PROVIDER_MSSQL_BUNDLED;
        }

        public static bool IsOracle(string databaseId)
        {
            return GetProviderInvariantName(databaseId) == PROVIDER_ORACLE_BUNDLED ||
                GetProviderInvariantName(databaseId) == PROVIDER_ORACLE_ODP_NET;
        }

        public static string GenerateMultipleParameterPlaceHolderSql(IDbConnection connection, params string[] parameterNames)
        {
            return
                Helper.ConcatStringList(
                    parameterNames.Select(p => GenerateParameterPlaceHolderSql(connection, p)),
                    ",", ""
                );
        }

        public static IDbCommand GetCommand(IDbConnection connection)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandTimeout = 1000;
            return command;
        }

        public static IDbCommand GetCommand(IDbCommand command)
        {
            IDbCommand command2 = GetCommand(command.Connection);
            command2.Transaction = command.Transaction;
            return command2;
        }

        public static string ConvertObjectToSQLRepresentation(IDbConnection connection, object obj)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return "NULL";
            }

            string connectionClassName = connection.GetType().FullName;
            Type objType = obj.GetType();
            if (objType == typeof(string))
            {
                if (connectionClassName == DatabaseConnectionClassFullNames.MSSQL_BUNDLED)
                {
                    return "N'" + obj.ToString().Replace("'", "''") + "'";
                }
                return "'" + obj.ToString().Replace("'", "''") + "'";
            }
            else if (objType == typeof(int) || objType == typeof(long) || objType == typeof(decimal) || objType == typeof(float) || objType == typeof(double))
            {
                return (obj).ToString();
            }
            else
            {
                if (objType == typeof(DateTime))
                {
                    if (connectionClassName == DatabaseConnectionClassFullNames.ORACLE_BUNDLED || connectionClassName == DatabaseConnectionClassFullNames.ORACLE_ODP)
                    {
                        return "TO_TIMESTAMP('" + ((DateTime)obj).ToString("yyyy'-'MM'-'dd HH':'mm:ss'.'ffffff") + "', 'YYYY-MM-DD HH24:MI:SS.FF')";
                    }
                    else if (connectionClassName == DatabaseConnectionClassFullNames.INFORMIX)
                    {
                        return "DATETIME(" + ((DateTime)obj).ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'fffff") + ") YEAR TO FRACTION(5)";
                    }
                    else if (connectionClassName == DatabaseConnectionClassFullNames.MSSQL_BUNDLED)
                    {
                        return "{ts '" + ((DateTime)obj).ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'fff") + "'}";
                    }
                    else if (connectionClassName == DatabaseConnectionClassFullNames.ODBC_BUNDLED)
                    {
                        var odbcConn = (OdbcConnection)connection;
                        if (odbcConn.Driver.ToUpper() == DatabaseHelper.ODBC_DRIVER_SYBASE_ASE.ToUpper())
                        {
                            return "convert(datetime,'" + ((DateTime)obj).ToString("yyyy'-'MM'-'dd HH':'mm':'ss'.'fff") + "')";
                        }
                    }
                }
            }
            throw new ArgumentException("The specified obj (" + obj + ") is not of supported type (" + obj.GetType() + ")");
        }

        public static void LogError(IDbCommand command, Exception ex, string appUserId = "ZTAMPZEmailNotifications")
        {
            bool maintainTransaction = command.Transaction == null;

            try
            {
                if (maintainTransaction)
                {
                    command.Transaction = command.Connection.BeginTransaction();
                }

                int sequence;
                if (DatabaseHelper.IsOracle(command.Connection))
                {
                    command.CommandText = "SELECT SEQUENCE_NAME FROM USER_SEQUENCES WHERE SEQUENCE_NAME LIKE 'SEQ_Z63%'";
                    command.Parameters.Clear();
                    string sequenceName = DatabaseHelper.ConvertFromDatabaseDataObject<string>(command.ExecuteScalar());

                    command.CommandText = "SELECT " + sequenceName + ".NEXTVAL FROM DUAL";
                    command.Parameters.Clear();
                    sequence = DatabaseHelper.ConvertFromDatabaseDataObject<int>(command.ExecuteScalar());
                }
                else
                {
                    command.CommandText = "SELECT NEXT VALUE FOR SEQ_Z13BSEQ";
                    command.Parameters.Clear();
                    sequence = DatabaseHelper.ConvertFromDatabaseDataObject<int>(command.ExecuteScalar());
                }

                DateTime databaseDateTime = DatabaseHelper.GetCurrentDatabaseDateTime(command);

                string errorType = ex.GetType().Name;

                command.CommandText =
    @"INSERT INTO Z63 (Z63ID, Z63ERRTYP, Z63ERRMSG, Z63CREDTE, Z63CREBY) 
VALUES (" + DatabaseHelper.GenerateMultipleParameterPlaceHolderSql(command.Connection, "Z63ID", "Z63ERRTYP", "Z63ERRMSG", "Z63CREDTE", "Z63CREBY") + ")";
                command.Parameters.Clear();
                DatabaseHelper.SetParameter(command, "Z63ID", sequence);
                DatabaseHelper.SetParameter(command, "Z63ERRTYP", errorType != null && errorType.Length > 20 ? errorType.Substring(0, 20) : errorType);
                DatabaseHelper.SetParameter(command, "Z63ERRMSG", ex.ToString());
                DatabaseHelper.SetParameter(command, "Z63CREDTE", databaseDateTime);
                DatabaseHelper.SetParameter(command, "Z63CREBY", appUserId);
                command.ExecuteNonQuery();

                if (maintainTransaction)
                {
                    command.Transaction.Commit();
                    command.Transaction = null;
                }
            } catch (Exception e) {
                if (maintainTransaction)
                {
                    if (command.Transaction != null)
                    {
                        command.Transaction.Rollback();
                        command.Transaction = null;
                    }
                }
                else
                {
                    throw new Exception(e.Message, e);
                }
            }

        }

        public static string ObjectListToSQLList(IDbConnection connection, IEnumerable objectList)
        {
            StringBuilder sqlList = new StringBuilder();
            foreach (object o in objectList)
            {
                sqlList.Append(ConvertObjectToSQLRepresentation(connection, o)).Append(",");
            }
            if (sqlList.Length > 0)
            {
                sqlList.Remove(sqlList.Length - 1, 1);
            }
            return "(" + sqlList.ToString() + ")";
        }

        public static OracleLob CreateOracleLob(
            OracleCommand oracleCommand, OracleType oracleType)
        {
            string oracleTypeString;
            switch (oracleType)
            {
                case OracleType.Blob:
                    oracleTypeString = "BLOB";
                    break;
                case OracleType.Clob:
                    oracleTypeString = "CLOB";
                    break;
                case OracleType.NClob:
                    oracleTypeString = "NCLOB";
                    break;
                default:
                    throw new ArgumentException("OracleType " + oracleType + " is not supported.");
            }

            oracleCommand.CommandText = "DECLARE TEMP_LOB " + oracleTypeString + "; BEGIN DBMS_LOB.CREATETEMPORARY(TEMP_LOB, FALSE); :TEMPLOB := TEMP_LOB; END;";
            oracleCommand.Parameters.Clear();
            oracleCommand.Parameters.Add(new OracleParameter("TEMPLOB", oracleType)).Direction = ParameterDirection.Output;
            oracleCommand.ExecuteNonQuery();
            return (OracleLob)oracleCommand.Parameters[0].Value;
        }

        public static void WriteTextToClob(
          OracleLob clob, string str)
        {
            clob.BeginBatch(OracleLobOpenMode.ReadWrite);
            var buffer = Encoding.Unicode.GetBytes(str);
            clob.Write(buffer, 0, buffer.Length);
            clob.EndBatch();
        }

        public static bool CheckTableOrViewExists(IDbCommand command, string tableName, string schemaName = null, string catalogName = null)
        {
            string connectionClassName = command.Connection.GetType().FullName;

            if (connectionClassName == DatabaseConnectionClassFullNames.ORACLE_BUNDLED || connectionClassName == DatabaseConnectionClassFullNames.ORACLE_ODP)
            {
                command.CommandText =
                    "SELECT COUNT(*) FROM SYS." + (schemaName == null ? "USER_TABLES" : "ALL_TABLES") +
                    " WHERE TABLE_NAME = '" + tableName.Replace("'", "''") + "'" +
                    (schemaName == null ? "" : (" AND OWNER = '" + schemaName.Replace("'", "''") + "'"));
                command.Parameters.Clear();
            }
            else
            {
                command.CommandText =
@"SELECT COUNT(*)
FROM
    (SELECT TABLE_NAME, TABLE_SCHEMA, TABLE_CATALOG
    FROM INFORMATION_SCHEMA.TABLES
    UNION ALL
    SELECT TABLE_NAME, TABLE_SCHEMA, TABLE_CATALOG
    FROM INFORMATION_SCHEMA.VIEWS
    ) V
WHERE TABLE_NAME = " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_NAME") +
"   AND (TABLE_SCHEMA = " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_SCHEMA") +
"       OR " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_SCHEMA") + " IS NULL)" +
"   AND (TABLE_CATALOG = " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_CATALOG") +
"       OR " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_CATALOG") + " IS NULL) ";
                command.Parameters.Clear();
                DatabaseHelper.SetParameter(command, "TABLE_NAME", tableName);
                DatabaseHelper.SetParameter(command, "TABLE_SCHEMA", schemaName);
                DatabaseHelper.SetParameter(command, "TABLE_CATALOG", catalogName);
            }

            return DatabaseHelper.ConvertFromDatabaseDataObject<int>(command.ExecuteScalar()) == 1;
        }


        public static bool CheckColumnExists(IDbCommand command, string columnName, string tableName, string schemaName = null)
        {
            string connectionClassName = command.Connection.GetType().FullName;

            if (connectionClassName == DatabaseConnectionClassFullNames.ORACLE_BUNDLED || connectionClassName == DatabaseConnectionClassFullNames.ORACLE_ODP)
            {
                command.CommandText =
                    "SELECT COUNT(*) FROM SYS." + (schemaName == null ? "USER_TAB_COLUMNS" : "ALL_TAB_COLUMNS") +
                    " WHERE TABLE_NAME = :TABLE_NAME AND COLUMN_NAME = :COLUMN_NAME " +
                    (schemaName == null ? "" : (" AND OWNER = '" + schemaName.Replace("'", "''") + "'"));
                command.Parameters.Clear();
                DatabaseHelper.SetParameter(command, "TABLE_NAME", tableName);
                DatabaseHelper.SetParameter(command, "COLUMN_NAME", columnName);
            }
            else
            {
                command.CommandText =
@"SELECT COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_NAME") +
"   AND COLUMN_NAME = " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "COLUMN_NAME") +
"   AND (TABLE_SCHEMA = " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_SCHEMA") +
"       OR " + DatabaseHelper.GenerateParameterPlaceHolderSql(command.Connection, "TABLE_SCHEMA") + " IS NULL)";
                command.Parameters.Clear();
                DatabaseHelper.SetParameter(command, "TABLE_NAME", tableName);
                DatabaseHelper.SetParameter(command, "COLUMN_NAME", columnName);
                DatabaseHelper.SetParameter(command, "TABLE_SCHEMA", schemaName);
            }
            return DatabaseHelper.ConvertFromDatabaseDataObject<int>(command.ExecuteScalar()) == 1;
        }


        public static DateTime GetCurrentDatabaseDateTime(IDbCommand command)
        {
            switch (command.Connection.GetType().FullName)
            {
                case DatabaseConnectionClassFullNames.ORACLE_BUNDLED:
                    command.CommandText = "SELECT SYSDATE FROM DUAL";
                    break;
                case DatabaseConnectionClassFullNames.INFORMIX:
                    command.CommandText = "SELECT CURRENT FROM systables WHERE tabid = 1";
                    break;
                case DatabaseConnectionClassFullNames.MSSQL_BUNDLED:
                    command.CommandText = "SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()";
                    command.Parameters.Clear();
                    if (int.Parse(DatabaseHelper.ConvertFromDatabaseDataObject<string>(command.ExecuteScalar())) >= 100)
                    {
                        command.CommandText = "SELECT SYSDATETIME()";
                    }
                    else
                    {
                        command.CommandText = "SELECT CURRENT_TIMESTAMP";
                    }
                    break;
                default:
                    throw new ArgumentException();
            }

            command.Parameters.Clear();
            return DatabaseHelper.ConvertFromDatabaseDataObject<DateTime>(command.ExecuteScalar());
        }

        public static bool IsOracle(IDbConnection connection)
        {
            return
                connection.GetType().FullName == DatabaseHelper.DatabaseConnectionClassFullNames.ORACLE_BUNDLED ||
                connection.GetType().FullName == DatabaseHelper.DatabaseConnectionClassFullNames.ORACLE_ODP;
        }


        public static string getColumnName(string columnName, string databaseId)
        {
            string columnNameOnDb = "";
            switch (columnName)
            {
                case "Z71SCHRUNSECINL":
                    if (IsOracle(databaseId))
                    {
                        columnNameOnDb = "Z71SCHRUNDSINL";
                    }
                    else if (IsMssql(databaseId))
                    {
                        columnNameOnDb = "Z71SCHRUNSECINL";
                    }
                    break;
                default:
                    columnNameOnDb = columnName;
                    break;
            }
            return columnNameOnDb;
        }

        public static void PerformMultipleTransactedDatabaseOperations(IEnumerable<IDbCommand> databaseCommands, Action coreAction, Action<Exception> failAction, bool rollbackInsteadOfCommit = false)
        {
            foreach (var sourceCommand in databaseCommands)
            {
                if (sourceCommand.Transaction != null)
                {
                    throw new Exception("Transaction must not have begun.");
                }
            }

            try
            {
                foreach (var sourceCommand in databaseCommands)
                {
                    sourceCommand.Transaction = sourceCommand.Connection.BeginTransaction();
                }

                coreAction();

                foreach (var sourceCommand in databaseCommands)
                {
                    if (rollbackInsteadOfCommit)
                    {
                        sourceCommand.Transaction.Rollback();
                    }
                    else
                    {
                        sourceCommand.Transaction.Commit();
                    }
                    sourceCommand.Transaction = null;
                }
            }
            catch (Exception ex)
            {
                foreach (var sourceCommand in databaseCommands)
                {
                    if (sourceCommand.Transaction != null)
                    {
                        sourceCommand.Transaction.Rollback();
                        sourceCommand.Transaction = null;
                    }
                }
                failAction(ex);
            }
            finally
            {
                foreach (var sourceCommand in databaseCommands)
                {
                    var sourceConnection = sourceCommand.Connection;
                    sourceCommand.Dispose();
                    sourceConnection.Dispose();
                }
            }
        }

        public static List<Dictionary<string, object>> ReadToListOfDictionary(IDataReader reader)
        {
            var list = new List<Dictionary<string, object>>();
            while (reader.Read())
            {
                var dictionary = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    object value = reader[i];
                    dictionary[reader.GetName(i)] = value == DBNull.Value ? null : value;
                }
                list.Add(dictionary);
            }
            return list;
        }
    }
}
