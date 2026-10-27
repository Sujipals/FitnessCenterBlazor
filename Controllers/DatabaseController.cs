using Microsoft.AspNetCore.Mvc;
using FitnessCenterBlazor.Services;
using System.Data;

namespace FitnessCenterBlazor.Controllers
{
    [ApiController]
    [Route("api/database")]
    public class DatabaseController : ControllerBase
    {
        private readonly FitnessCenterDbService _dbService;

        public DatabaseController(FitnessCenterDbService dbService)
        {
            _dbService = dbService;
        }

        // ============================================================
        // GET ALL DATABASE TABLES
        // GET: /api/database/tables
        // ============================================================

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables()
        {
            var tables = await _dbService.GetTableNamesAsync();

            return Ok(tables);
        }

        // ============================================================
        // GET DATA FROM ANY TABLE
        // GET: /api/database/{tableName}
        // ============================================================

        [HttpGet("{tableName}")]
        public async Task<IActionResult> GetTable(string tableName)
        {
            try
            {
                // Get the selected table from MySQL
                var dataTable =
                    await _dbService.GetTableAsync(tableName);

                // Create a JSON-friendly list
                var result = new List<Dictionary<string, object?>>();

                // Go through every row
                foreach (DataRow row in dataTable.Rows)
                {
                    var item = new Dictionary<string, object?>();

                    // Go through every column
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        item[column.ColumnName] =
                            row[column] == DBNull.Value
                                ? null
                                : row[column];
                    }

                    result.Add(item);
                }

                // Return clean JSON
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ============================================================
        // GET COLUMNS FOR ANY TABLE
        // GET: /api/database/{tableName}/columns
        // ============================================================

        [HttpGet("{tableName}/columns")]
        public async Task<IActionResult> GetColumns(string tableName)
        {
            try
            {
                var columns =
                    await _dbService.GetColumnsAsync(tableName);

                return Ok(columns);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    $"Database error: {ex.Message}");
            }
        }


        // ============================================================
        // CREATE RECORD IN ANY TABLE
        // POST: /api/database/{tableName}
        // ============================================================

        [HttpPost("{tableName}")]
        public async Task<IActionResult> Create(
            string tableName,
            [FromBody] Dictionary<string, object?> values)
        {
            try
            {
                // Insert the values into the selected table
                await _dbService.InsertDynamicAsync(
                    tableName,
                    values);

                return Ok(new
                {
                    message = "Record created successfully.",
                    table = tableName
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    $"Database error: {ex.Message}");
            }
        }

        // ============================================================
        // UPDATE RECORD IN ANY TABLE
        // PUT: /api/database/{tableName}/{id}
        // ============================================================

        [HttpPut("{tableName}/{id}")]
        public async Task<IActionResult> Update(
            string tableName,
            string id,
            [FromBody] Dictionary<string, object?> values)
        {
            try
            {
                // Get the columns of the selected table
                var columns =
                    await _dbService.GetColumnsAsync(tableName);

                // Find the primary key
                var primaryKey =
                    columns.FirstOrDefault(c => c.IsPrimaryKey);

                if (primaryKey == null)
                {
                    return BadRequest(
                        $"Table '{tableName}' has no primary key.");
                }

                // Convert the ID to the correct type
                object primaryKeyValue = id;

                if (primaryKey.DataType.Equals(
                        "int",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(id, out var intId))
                    {
                        return BadRequest("Invalid ID.");
                    }

                    primaryKeyValue = intId;
                }

                // Do not allow the primary key to be changed
                values.Remove(primaryKey.Name);

                // Update the record
                await _dbService.UpdateDynamicAsync(
                    tableName,
                    primaryKeyValue,
                    values);

                return Ok(new
                {
                    message = "Record updated successfully.",
                    table = tableName,
                    id = primaryKeyValue
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    $"Database error: {ex.Message}");
            }
        }


        // ============================================================
        // DELETE RECORD FROM ANY TABLE
        // DELETE: /api/database/{tableName}/{id}
        // ============================================================

        [HttpDelete("{tableName}/{id}")]
        public async Task<IActionResult> Delete(
            string tableName,
            string id)
        {
            try
            {
                // Get the table structure
                var columns =
                    await _dbService.GetColumnsAsync(tableName);

                // Find the primary key
                var primaryKey =
                    columns.FirstOrDefault(c => c.IsPrimaryKey);

                if (primaryKey == null)
                {
                    return BadRequest(
                        $"Table '{tableName}' has no primary key.");
                }

                // Convert the ID to the correct type
                object primaryKeyValue = id;

                if (primaryKey.DataType.ToLower() == "int")
                {
                    if (!int.TryParse(id, out var intId))
                    {
                        return BadRequest("Invalid ID.");
                    }

                    primaryKeyValue = intId;
                }

                // Delete the record
                await _dbService.DeleteDynamicAsync(
                    tableName,
                    primaryKeyValue);

                return Ok(new
                {
                    message = "Record deleted successfully.",
                    table = tableName,
                    id = primaryKeyValue
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    $"Database error: {ex.Message}");
            }
        }

    }
}