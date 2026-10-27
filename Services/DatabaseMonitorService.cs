using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;

namespace FitnessCenterBlazor.Services
{
    public class DatabaseMonitorService : BackgroundService
    {
        private readonly string _connectionString =
        "Server=localhost;Port=3306;Database=FitnessCenter;User ID=root;Password=MiAr@0625Pals;";


    // Contains the latest row count for every table.
    public Dictionary<string, int> TableRowCounts { get; private set; } = new();

        // Runs automatically while the application is running.
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateTableRowCountsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database monitor error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2),stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // Gets all tables dynamically from MySQL.
        private async Task UpdateTableRowCountsAsync()
        {
            var result = new Dictionary<string, int>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Get every table from the current database.
            const string tableSql = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME;
        ";

            using var tableCommand = new MySqlCommand(tableSql, connection);
            using var reader = await tableCommand.ExecuteReaderAsync();
            var tableNames = new List<string>();

            while (await reader.ReadAsync())
            {
                tableNames.Add(reader["TABLE_NAME"]?.ToString() ?? "");
            }

            await reader.CloseAsync();

            // Count rows in every discovered table.
            foreach (var tableName in tableNames)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    continue;
                }

                string sql = $"SELECT COUNT(*) FROM `{tableName}`;";
                using var countCommand = new MySqlCommand(sql, connection);
                object? value = await countCommand.ExecuteScalarAsync();
                int count = Convert.ToInt32(value);
                result[tableName] = count;
            }

            // Replace the old information.
            TableRowCounts = result;
        }
    }


}
