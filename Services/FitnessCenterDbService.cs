using FitnessCenterBlazor.Models;
using MySql.Data.MySqlClient;
using System.Data;

namespace FitnessCenterBlazor.Services
{
    public class FitnessCenterDbService
    {
        // Database connection string.
        // IMPORTANT:
        // For a real project, move the password to configuration/User Secrets.
        private readonly string _connectionString =
            "Server=localhost;Port=3306;Database=FitnessCenter;User ID=root;Password=MiAr@0625Pals;";

        // ============================================================
        // GET ALL TABLE NAMES
        // ============================================================

        public async Task<List<string>> GetTableNamesAsync()
        {
            var tables = new List<string>();

            using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync();

            string sql = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                ORDER BY TABLE_NAME;
            ";

            using var command =
                new MySqlCommand(sql, connection);

            using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString("TABLE_NAME"));
            }

            return tables;
        }

        // ============================================================
        // GET CURRENT DATABASE
        // ============================================================

        public async Task<string> GetCurrentDatabaseAsync()
        {
            using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command =
                new MySqlCommand(
                    "SELECT DATABASE();",
                    connection);

            var result =
                await command.ExecuteScalarAsync();

            return result?.ToString() ?? "";
        }

        // ============================================================
        // GET DATA FROM SELECTED TABLE
        // ============================================================

        public async Task<DataTable> GetTableAsync(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(
                    "Table name cannot be empty.");
            }

            // Get valid table names from the database.
            var tableNames =
                await GetTableNamesAsync();

            // Find the real table name.
            var actualTableName =
                tableNames.FirstOrDefault(
                    t => string.Equals(
                        t,
                        tableName,
                        StringComparison.OrdinalIgnoreCase));

            // Prevent an invalid table name.
            if (actualTableName == null)
            {
                throw new ArgumentException(
                    $"Invalid table name: '{tableName}'.");
            }

            using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync();

            // The table name has already been checked
            // against INFORMATION_SCHEMA.
            string sql =
                $"SELECT * FROM `{actualTableName}`;";

            using var command =
                new MySqlCommand(sql, connection);

            using var reader =
                await command.ExecuteReaderAsync();

            var table = new DataTable();

            table.Load(reader);

            return table;
        }

        // ============================================================
        // GET COLUMN INFORMATION
        // ============================================================

        public async Task<List<ColumnInfo>> GetColumnsAsync(
            string tableName)
        {
            // Get valid table names.
            var tableNames =
                await GetTableNamesAsync();

            // Find the actual table name.
            var actualTableName =
                tableNames.FirstOrDefault(
                    t => string.Equals(
                        t,
                        tableName,
                        StringComparison.OrdinalIgnoreCase));

            if (actualTableName == null)
            {
                throw new ArgumentException(
                    $"Invalid table name: {tableName}");
            }

            var columns = new List<ColumnInfo>();

            using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync();

            string sql = @"
                SELECT
                    COLUMN_NAME,
                    DATA_TYPE,
                    COLUMN_KEY,
                    EXTRA,
                    IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                AND TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION;
            ";

            using var command =
                new MySqlCommand(sql, connection);

            // Parameter protects this value.
            command.Parameters.AddWithValue(
                "@TableName",
                actualTableName);

            using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                columns.Add(
                    new ColumnInfo
                    {
                        Name =
                            reader["COLUMN_NAME"]
                                .ToString() ?? "",

                        DataType =
                            reader["DATA_TYPE"]
                                .ToString() ?? "",

                        IsPrimaryKey =
                            reader["COLUMN_KEY"]
                                .ToString() == "PRI",

                        IsAutoIncrement =
                            reader["EXTRA"]
                                .ToString()
                                ?.Contains(
                                    "auto_increment",
                                    StringComparison.OrdinalIgnoreCase)
                                == true,

                        IsNullable =
                            reader["IS_NULLABLE"]
                                .ToString() == "YES"
                    });
            }

            return columns;
        }

        // ============================================================
        // DYNAMIC INSERT
        // ============================================================

        public async Task InsertDynamicAsync(
            string tableName,
            Dictionary<string, object?> values)
        {
            // Get information about the table columns.
            var columns =
                await GetColumnsAsync(tableName);

            // Only use columns that:
            // 1. Are not auto-generated.
            // 2. Exist in the values dictionary.
            var insertColumns =
                columns
                    .Where(c =>
                        !c.IsAutoIncrement &&
                        values.ContainsKey(c.Name))
                    .ToList();

            if (insertColumns.Count == 0)
            {
                throw new ArgumentException(
                    "There are no values to insert.");
            }

            // Example:
            // FirstName, LastName, Email
            string columnList =
                string.Join(
                    ", ",
                    insertColumns.Select(
                        c => $"`{c.Name}`"));

            // Example:
            // @FirstName, @LastName, @Email
            string parameterList =
                string.Join(
                    ", ",
                    insertColumns.Select(
                        c => $"@{c.Name}"));

            string sql = $@"
                INSERT INTO `{tableName}`
                ({columnList})
                VALUES
                ({parameterList});
            ";

            using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command =
                new MySqlCommand(sql, connection);

            // Add parameter values.
            foreach (var column in insertColumns)
            {
                command.Parameters.AddWithValue(
                    $"@{column.Name}",
                    values[column.Name] ?? DBNull.Value);
            }

            await command.ExecuteNonQueryAsync();
        }

        // ============================================================
        // DYNAMIC UPDATE
        // ============================================================

        public async Task UpdateDynamicAsync(
            string tableName,
            object primaryKeyValue,
            Dictionary<string, object?> values)
        {
            var columns =
                await GetColumnsAsync(tableName);

            // Find primary key.
            var primaryKey =
                columns.FirstOrDefault(
                    c => c.IsPrimaryKey);

            if (primaryKey == null)
            {
                throw new ArgumentException(
                    $"Table '{tableName}' has no primary key.");
            }

            // Do not update the primary key.
            var updateColumns =
                columns
                    .Where(c =>
                        !c.IsPrimaryKey &&
                        values.ContainsKey(c.Name))
                    .ToList();

            if (updateColumns.Count == 0)
            {
                throw new ArgumentException(
                    "There are no values to update.");
            }

            // Build:
            // Name = @Name,
            // Price = @Price
            string setClause =
                string.Join(
                    ", ",
                    updateColumns.Select(
                        c =>
                            $"`{c.Name}` = @{c.Name}"));

            string sql = $@"
                UPDATE `{tableName}`
                SET {setClause}
                WHERE `{primaryKey.Name}` = @PrimaryKey;
            ";

            using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command =
                new MySqlCommand(sql, connection);

            // Add update parameters.
            foreach (var column in updateColumns)
            {
                command.Parameters.AddWithValue(
                    $"@{column.Name}",
                    values[column.Name] ?? DBNull.Value);
            }

            // Add primary key parameter.
            command.Parameters.AddWithValue(
                "@PrimaryKey",
                primaryKeyValue);

            await command.ExecuteNonQueryAsync();
        }

        // ============================================================
        // DYNAMIC DELETE
        // ============================================================

        public async Task DeleteDynamicAsync(
            string tableName,
            object primaryKeyValue)
        {
            var columns =
                await GetColumnsAsync(tableName);

            // Find primary key automatically.
            var primaryKey =
                columns.FirstOrDefault(
                    c => c.IsPrimaryKey);

            if (primaryKey == null)
            {
                throw new ArgumentException(
                    $"Table '{tableName}' has no primary key.");
            }

            string sql = $@"
                DELETE FROM `{tableName}`
                WHERE `{primaryKey.Name}` = @PrimaryKey;
            ";

            using var connection =
                new MySqlConnection(_connectionString);

            await connection.OpenAsync();

            using var command =
                new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@PrimaryKey",
                primaryKeyValue);

            await command.ExecuteNonQueryAsync();
        }
    }
}