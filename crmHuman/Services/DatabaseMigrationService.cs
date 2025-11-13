using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.SqlClient;

namespace crmHuman.Services
{
    /// <summary>
    /// Service để tự động chạy database migration khi ứng dụng khởi động
    /// </summary>
    public class DatabaseMigrationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseMigrationService> _logger;
        private const string ConnectionStringName = "stringConnect7";

        public DatabaseMigrationService(IConfiguration configuration, ILogger<DatabaseMigrationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Chạy migration tự động
        /// </summary>
        public async Task RunMigrationsAsync()
        {
            try
            {
                _logger.LogInformation("Bắt đầu chạy database migration...");

                var connectionString = _configuration.GetConnectionString(ConnectionStringName);
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Không tìm thấy connection string: {ConnectionStringName}", ConnectionStringName);
                    return;
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Chạy các migration
                await AddEmployeeColumnsAsync(connection);
                await AddTaxItemColumnsAsync(connection);

                _logger.LogInformation("Database migration hoàn thành thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chạy database migration");
                // Không throw exception để ứng dụng vẫn có thể khởi động
                // Nếu cần thiết, có thể throw để dừng ứng dụng
            }
        }

        /// <summary>
        /// Thêm các cột mới vào bảng Employees
        /// </summary>
        private async Task AddEmployeeColumnsAsync(IDbConnection connection)
        {
            // Kiểm tra tên bảng thực tế (có thể là Employees hoặc Employee)
            var tableName = await GetTableNameAsync(connection, new[] { "Employees", "Employee" }) ?? "Employees";

            var columns = new[]
            {
                new { Name = "Gender", Type = "NVARCHAR(50)", Description = "Giới tính" },
                new { Name = "PlaceOfBirth", Type = "NVARCHAR(255)", Description = "Nơi sinh" },
                new { Name = "Religion", Type = "NVARCHAR(100)", Description = "Tôn giáo" },
                new { Name = "PersonalEmail", Type = "NVARCHAR(255)", Description = "Email cá nhân" },
                new { Name = "BeneficiaryName", Type = "NVARCHAR(255)", Description = "Tên chủ tài khoản" }
            };

            foreach (var column in columns)
            {
                if (await ColumnExistsAsync(connection, tableName, column.Name))
                {
                    _logger.LogInformation("Cột {ColumnName} ({Description}) đã tồn tại trong bảng {TableName}", 
                        column.Name, column.Description, tableName);
                    continue;
                }

                try
                {
                    var sql = $@"ALTER TABLE [dbo].[{tableName}] ADD [{column.Name}] {column.Type} NULL;";
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    
                    // Sử dụng SqlCommand để có async methods
                    if (command is SqlCommand sqlCommand)
                    {
                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        command.ExecuteNonQuery();
                    }
                    
                    _logger.LogInformation("Đã thêm cột {ColumnName} ({Description}) vào bảng {TableName}", 
                        column.Name, column.Description, tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể thêm cột {ColumnName} vào bảng {TableName}. Có thể cột đã tồn tại.", 
                        column.Name, tableName);
                }
            }
        }

        /// <summary>
        /// Thêm các cột mới vào bảng TaxItem
        /// </summary>
        private async Task AddTaxItemColumnsAsync(IDbConnection connection)
        {
            var tableName = "TaxItem";

            var columns = new[]
            {
                new { Name = "PITDate", Type = "DATETIME", Description = "Ngày cấp mã số thuế" },
                new { Name = "EffectedFrom", Type = "DATETIME", Description = "Hiệu lực từ" }
            };

            foreach (var column in columns)
            {
                if (await ColumnExistsAsync(connection, tableName, column.Name))
                {
                    _logger.LogInformation("Cột {ColumnName} ({Description}) đã tồn tại trong bảng {TableName}", 
                        column.Name, column.Description, tableName);
                    continue;
                }

                try
                {
                    var sql = $@"ALTER TABLE [dbo].[{tableName}] ADD [{column.Name}] {column.Type} NULL;";
                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    
                    // Sử dụng SqlCommand để có async methods
                    if (command is SqlCommand sqlCommand)
                    {
                        await sqlCommand.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        command.ExecuteNonQuery();
                    }
                    
                    _logger.LogInformation("Đã thêm cột {ColumnName} ({Description}) vào bảng {TableName}", 
                        column.Name, column.Description, tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể thêm cột {ColumnName} vào bảng {TableName}. Có thể cột đã tồn tại.", 
                        column.Name, tableName);
                }
            }
        }

        /// <summary>
        /// Lấy tên bảng thực tế từ danh sách các tên có thể
        /// </summary>
        private async Task<string?> GetTableNameAsync(IDbConnection connection, string[] possibleNames)
        {
            try
            {
                foreach (var tableName in possibleNames)
                {
                    var sql = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_TYPE = 'BASE TABLE' 
                        AND TABLE_NAME = @TableName";

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    
                    var param = command.CreateParameter();
                    param.ParameterName = "@TableName";
                    param.Value = tableName;
                    command.Parameters.Add(param);

                    object? result;
                    // Sử dụng SqlCommand để có async methods
                    if (command is SqlCommand sqlCommand)
                    {
                        result = await sqlCommand.ExecuteScalarAsync();
                    }
                    else
                    {
                        result = command.ExecuteScalar();
                    }
                    
                    if (result != null && Convert.ToInt32(result) > 0)
                    {
                        return tableName;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi kiểm tra tên bảng");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra xem cột đã tồn tại chưa
        /// </summary>
        private async Task<bool> ColumnExistsAsync(IDbConnection connection, string tableName, string columnName)
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName 
                    AND COLUMN_NAME = @ColumnName";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                
                var tableParam = command.CreateParameter();
                tableParam.ParameterName = "@TableName";
                tableParam.Value = tableName;
                command.Parameters.Add(tableParam);
                
                var columnParam = command.CreateParameter();
                columnParam.ParameterName = "@ColumnName";
                columnParam.Value = columnName;
                command.Parameters.Add(columnParam);

                object? result;
                // Sử dụng SqlCommand để có async methods
                if (command is SqlCommand sqlCommand)
                {
                    result = await sqlCommand.ExecuteScalarAsync();
                }
                else
                {
                    result = command.ExecuteScalar();
                }
                
                return result != null && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi kiểm tra cột {ColumnName} trong bảng {TableName}", columnName, tableName);
                return false;
            }
        }
    }
}

